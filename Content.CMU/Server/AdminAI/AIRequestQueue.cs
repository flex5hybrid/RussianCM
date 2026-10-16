using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Content.Server.CMU14.AdminAI;

public enum AIRequestClass : byte { LightManual, HeavyManual, Background }
public sealed record AIQueueOptions(int Capacity = 8, int TtlSeconds = 30, int TimeoutSeconds = 45,
    int MaxInputChars = 4096, int DegradedInputChars = 1024, int DegradedOutputTokens = 64)
{
    public bool IsValid() => Capacity is >= 1 and <= 32 && TtlSeconds is >= 1 and <= 120
        && TimeoutSeconds is >= 1 and <= 60 && MaxInputChars is >= 1 and <= 16384
        && DegradedInputChars >= 1 && DegradedInputChars <= MaxInputChars && DegradedOutputTokens is >= 1 and <= 128;
}
public sealed record AIQueueSnapshot(int Pending, bool Running, long Accepted, long Completed, long Rejected,
    AIResourceState State, AIWorkerHealth Health);

/// <summary>Bounded waiting queue, one physical inference slot, in-flight GUID deduplication, cancellation and TTL.</summary>
public sealed class AIRequestQueue : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly List<Job> _pending = new();
    private readonly IAIInferenceClient _client;
    private readonly AIQueueOptions _options;
    private readonly Func<TimeSpan> _now;
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _wake = new(0, 1);
    private readonly Task _consumer;
    private readonly Action<Guid, AIInferenceStatus>? _audit;
    private Job? _active;
    private AIResourceState _state = AIResourceState.Paused;
    private AIWorkerHealth _health = AIWorkerHealth.Unavailable;
    private bool _disabled;
    private Task? _disposeTask;
    private readonly Channel<(Guid Id, AIInferenceStatus Status)> _events = Channel.CreateBounded<(Guid, AIInferenceStatus)>(
        new BoundedChannelOptions(64) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
    private long _accepted, _completed, _rejected;

    public AIRequestQueue(IAIInferenceClient client, AIQueueOptions options, Func<TimeSpan>? now = null,
        Action<Guid, AIInferenceStatus>? audit = null)
    {
        if (!options.IsValid()) throw new ArgumentException("Invalid AI queue limits.", nameof(options));
        _client = client; _options = options; _audit = audit;
        var started = Stopwatch.GetTimestamp();
        _now = now ?? (() => Stopwatch.GetElapsedTime(started));
        _consumer = Task.Run(ConsumeAsync);
    }

    public AIQueueSnapshot Snapshot()
    {
        lock (_gate) return new(_pending.Count, _active != null, _accepted, _completed, _rejected, _state, _health);
    }

    public Task<AIInferenceResult> EnqueueAsync(AIInferenceRequest request, AIRequestClass kind, CancellationToken token)
    {
        lock (_gate)
        {
            Purge();
            AIInferenceStatus? refusal = token.IsCancellationRequested ? AIInferenceStatus.Cancelled
                : _disabled ? AIInferenceStatus.Disabled
                : request.CorrelationId == Guid.Empty || !request.IsInputValid(_options.MaxInputChars) || !Enum.IsDefined(kind)
                    || request.OutputTokenLimit is < 1 or > 512 ? AIInferenceStatus.InvalidRequest
                : _health != AIWorkerHealth.Ready ? AIInferenceStatus.Unavailable
                : !Allowed(kind, (int)request.InputChars) ? AIInferenceStatus.Paused
                : _active?.Id == request.CorrelationId || _pending.Exists(j => j.Id == request.CorrelationId) ? AIInferenceStatus.Duplicate
                : _pending.Count >= _options.Capacity ? AIInferenceStatus.QueueFull : null;
            if (refusal != null)
            {
                _rejected++; _events.Writer.TryWrite((request.CorrelationId, refusal.Value)); Wake();
                return Task.FromResult(new AIInferenceResult(request.CorrelationId, refusal.Value));
            }
            var job = new Job(request, kind, token, _now());
            _pending.Add(job); _accepted++;
            // Registration never takes the queue lock or waits on the game thread.
            job.Registration = token.Register(() => { job.Done.TrySetResult(new(job.Id, AIInferenceStatus.Cancelled)); Wake(); });
            Wake();
            return job.Done.Task;
        }
    }

    public void SetAvailability(AIResourceState state, AIWorkerHealth health)
    {
        CancellationTokenSource? cancel = null;
        lock (_gate)
        {
            var previousState = _state;
            _state = state; _health = health;
            if (_active != null && !_active.Finished && (_disabled || health != AIWorkerHealth.Ready || !Allowed(_active.Kind, _active.InputLength)
                || previousState == AIResourceState.Normal && state == AIResourceState.Degraded))
            {
                Finish(_active, new(_active.Id, _disabled ? AIInferenceStatus.Disabled
                    : health != AIWorkerHealth.Ready ? AIInferenceStatus.Unavailable : AIInferenceStatus.Paused));
                cancel = _active.Execution;
            }
            Purge();
        }
        // Cancellation callbacks belong to the transport and must not hold up a game tick.
        if (cancel != null) _ = Task.Run(() => SafeCancel(cancel));
        Wake();
    }

    private bool Allowed(AIRequestClass kind, int length) => _state == AIResourceState.Normal
        || _state == AIResourceState.Degraded && kind == AIRequestClass.LightManual && length <= _options.DegradedInputChars;

    private void Purge()
    {
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            var job = _pending[i];
            var status = job.Token.IsCancellationRequested ? AIInferenceStatus.Cancelled
                : _disabled ? AIInferenceStatus.Disabled
                : _now() - job.Created >= TimeSpan.FromSeconds(_options.TtlSeconds) ? AIInferenceStatus.Expired
                : _health != AIWorkerHealth.Ready ? AIInferenceStatus.Unavailable
                : !Allowed(job.Kind, job.InputLength) ? AIInferenceStatus.Paused : (AIInferenceStatus?)null;
            if (status == null) continue;
            _pending.RemoveAt(i); Finish(job, new(job.Id, status.Value));
        }
    }

    private void Finish(Job job, AIInferenceResult result)
    {
        if (job.Finished) return;
        if (job.Token.IsCancellationRequested) result = new(job.Id, AIInferenceStatus.Cancelled);
        job.Finished = true; job.Request = null;
        job.Registration.Unregister();
        job.Done.TrySetResult(result); _completed++;
        _events.Writer.TryWrite((job.Id, result.Status));
    }

    private async Task ConsumeAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                DrainAudit();
                Job? job = null;
                lock (_gate)
                {
                    Purge();
                    if (_pending.Count > 0)
                    {
                        var index = 0;
                        for (var i = 1; i < _pending.Count; i++) if (_pending[i].Kind < _pending[index].Kind) index = i;
                        job = _pending[index]; _pending.RemoveAt(index); _active = job;
                        job.Execution = CancellationTokenSource.CreateLinkedTokenSource(job.Token, _stop.Token);
                    }
                }
                if (job == null) { await _wake.WaitAsync(250, _stop.Token).ConfigureAwait(false); continue; }
                await ExecuteAsync(job).ConfigureAwait(false);
                lock (_gate) { _active = null; }
                job.Execution?.Dispose();
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        finally { DrainAudit(); }
    }

    private async Task ExecuteAsync(Job job)
    {
        Task<AIInferenceResult> operation;
        AIInferenceRequest request;
        lock (_gate)
        {
            if (job.Finished || job.Request == null) return;
            request = _state == AIResourceState.Degraded
                ? job.Request with { OutputTokenLimit = Math.Min(job.Request.OutputTokenLimit ?? 512, _options.DegradedOutputTokens) }
                : job.Request;
        }
        if (job.Execution!.IsCancellationRequested) { lock (_gate) Finish(job, new(job.Id, AIInferenceStatus.Cancelled)); return; }
        try { operation = _client.InferAsync(request, job.Execution.Token); }
        catch (Exception) { lock (_gate) Finish(job, new(job.Id, AIInferenceStatus.Unavailable)); return; }
        AIInferenceResult result;
        try
        {
            var started = Stopwatch.GetTimestamp();
            while (!operation.IsCompleted)
            {
                await Task.WhenAny(operation, Task.Delay(250, job.Execution.Token)).ConfigureAwait(false);
                lock (_gate) Purge();
                DrainAudit();
                job.Execution.Token.ThrowIfCancellationRequested();
                if (Stopwatch.GetElapsedTime(started).TotalSeconds >= _options.TimeoutSeconds) throw new TimeoutException();
            }
            job.Execution.Token.ThrowIfCancellationRequested();
            result = await operation.ConfigureAwait(false);
            if (result.CorrelationId != job.Id) result = new(job.Id, AIInferenceStatus.InvalidResponse);
            if (result.Status != AIInferenceStatus.Success) result = result with { UntrustedText = null };
        }
        catch (TimeoutException) { result = new(job.Id, AIInferenceStatus.TimedOut); SafeCancel(job.Execution); }
        catch (OperationCanceledException) { result = new(job.Id, AIInferenceStatus.Cancelled); }
        catch (Exception) { result = new(job.Id, AIInferenceStatus.Unavailable); }
        lock (_gate) Finish(job, result);
        // A client that ignores cancellation cannot free the physical slot and start a parallel inference.
        while (!operation.IsCompleted && !_stop.IsCancellationRequested)
        {
            lock (_gate) Purge();
            DrainAudit();
            await Task.WhenAny(operation, Task.Delay(250, _stop.Token)).ConfigureAwait(false);
        }
        _ = operation.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void Wake() { try { _wake.Release(); } catch (SemaphoreFullException) { } catch (ObjectDisposedException) { } }
    private static void SafeCancel(CancellationTokenSource? source)
    { try { source?.Cancel(); } catch (ObjectDisposedException) { } catch (AggregateException) { } }

    private void DrainAudit()
    {
        while (_events.Reader.TryRead(out var entry))
        {
            try { _audit?.Invoke(entry.Id, entry.Status); } catch (Exception) { /* Diagnostics cannot stop dispatch. */ }
        }
    }

    public ValueTask DisposeAsync()
    {
        Disable();
        lock (_gate) return new(_disposeTask ??= Task.Run(DisposeCore));
    }

    private async Task DisposeCore()
    {
        Disable();
        SafeCancel(_stop);
        if (_client is IDisposable disposable) { try { disposable.Dispose(); } catch (Exception) { } }
        await _consumer.ConfigureAwait(false);
        _wake.Dispose(); _stop.Dispose();
    }

    public void Disable()
    {
        lock (_gate) _disabled = true;
        SetAvailability(AIResourceState.Paused, AIWorkerHealth.Disabled);
    }

    private sealed class Job(AIInferenceRequest request, AIRequestClass kind, CancellationToken token, TimeSpan created)
    {
        public readonly Guid Id = request.CorrelationId;
        public readonly int InputLength = (int)request.InputChars;
        public AIInferenceRequest? Request = request;
        public readonly AIRequestClass Kind = kind;
        public readonly CancellationToken Token = token;
        public readonly TimeSpan Created = created;
        public readonly TaskCompletionSource<AIInferenceResult> Done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationTokenRegistration Registration;
        public CancellationTokenSource? Execution;
        public bool Finished;
    }
}

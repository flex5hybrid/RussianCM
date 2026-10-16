using System;
using System.Globalization;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Server.CMU14.AdminAI;

public sealed record AdminAISnapshot(bool Enabled, AIResourceState State, AIWorkerHealth Health,
    string Reason, AIQueueSnapshot? Queue, AILoadSample? Load);

public interface IAdminAIService
{
    void Initialize();
    void ObservePhase(ModUpdateLevel level);
    void Shutdown();
    AdminAISnapshot Snapshot();
    Task<AIInferenceResult> EnqueueAsync(AIInferenceRequest request, AIRequestClass kind, CancellationToken token);
}

/// <summary>Optional infrastructure only. No ahelp subscription, generation trigger, admin action or process control.</summary>
public sealed class AdminAIService : IAdminAIService
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ILogManager _logs = default!;
    [Dependency] private IMeterFactory _meterFactory = default!;
    private Meter? _meter;
    private readonly object _gate = new();
    private ISawmill _log = default!;
    private AIRequestQueue? _queue;
    private CancellationTokenSource? _lifetime;
    private Task _stopping = Task.CompletedTask;
    private Task _poll = Task.CompletedTask, _healthPoll = Task.CompletedTask;
    private AIResourceGovernor? _governor;
    private AILoadSample? _load;
    private AIResourceState _state = AIResourceState.Paused;
    private AIWorkerHealth _health = AIWorkerHealth.Disabled;
    private string _reason = "disabled";
    private bool _dirty = true, _initialized, _shutdown, _restart;
    private bool _governorEnabled;
    private int _maxSampleAgeSeconds;
    private long _lastLoadStamp, _nextStaleCheck;
    private long _tickStarted, _maxSimulationMicroseconds, _tickBudgetMicroseconds, _lastTickStamp;
    private readonly long _started = Stopwatch.GetTimestamp();
    private Action<bool> _boolChanged = default!;
    private Action<int> _intChanged = default!;
    private Action<float> _floatChanged = default!;
    private Action<string> _stringChanged = default!;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true; _log = _logs.GetSawmill("cmu.admin_ai");
        _meter = _meterFactory.Create("CMU.AdminAI");
        _meter.CreateObservableGauge("cmu_admin_ai_enabled", () => Snapshot().Enabled ? 1 : 0);
        _meter.CreateObservableGauge("cmu_admin_ai_state", () => (int)Snapshot().State);
        _meter.CreateObservableGauge("cmu_admin_ai_worker_ready", () => Snapshot().Health == AIWorkerHealth.Ready ? 1 : 0);
        _meter.CreateObservableGauge("cmu_admin_ai_queue_pending", () => Snapshot().Queue?.Pending ?? 0);
        _meter.CreateObservableGauge("cmu_admin_ai_queue_running", () => Snapshot().Queue?.Running == true ? 1 : 0);
        _meter.CreateObservableGauge("cmu_admin_ai_accepted_since_reload", () => Snapshot().Queue?.Accepted ?? 0);
        _meter.CreateObservableGauge("cmu_admin_ai_completed_since_reload", () => Snapshot().Queue?.Completed ?? 0);
        _meter.CreateObservableGauge("cmu_admin_ai_rejected_since_reload", () => Snapshot().Queue?.Rejected ?? 0);
        _meter.CreateObservableGauge("cmu_admin_ai_cpu_psi_avg10", () => Snapshot().Load?.CpuSomeAvg10 ?? double.NaN);
        _meter.CreateObservableGauge("cmu_admin_ai_memory_psi_avg10", () => Snapshot().Load?.MemoryFullAvg10 ?? double.NaN);
        _meter.CreateObservableGauge("cmu_admin_ai_io_psi_avg10", () => Snapshot().Load?.IoFullAvg10 ?? double.NaN);
        _meter.CreateObservableGauge("cmu_admin_ai_tick_boundary_budget_ratio", () => Snapshot().Load?.SimulationBudgetRatio ?? double.NaN);
        _boolChanged = _ => Volatile.Write(ref _dirty, true); _intChanged = _ => Volatile.Write(ref _dirty, true);
        _floatChanged = _ => Volatile.Write(ref _dirty, true); _stringChanged = _ => Volatile.Write(ref _dirty, true);
        foreach (var cvar in AdminAICVars.InfrastructureBools) _config.OnValueChanged(cvar, _boolChanged);
        foreach (var cvar in AdminAICVars.InfrastructureInts) _config.OnValueChanged(cvar, _intChanged);
        foreach (var cvar in AdminAICVars.RuntimeFloats) _config.OnValueChanged(cvar, _floatChanged);
        foreach (var cvar in AdminAICVars.RuntimeStrings) _config.OnValueChanged(cvar, _stringChanged);
    }

    public void ObservePhase(ModUpdateLevel level)
    {
        if (!_initialized || _shutdown) return;
        if (level == ModUpdateLevel.InputPostEngine)
        {
            if (Volatile.Read(ref _dirty)) { Volatile.Write(ref _dirty, false); Stop(); _restart = true; }
            if (_restart && _stopping.IsCompleted) { _restart = false; Start(); }
            if (_queue != null && Stopwatch.GetTimestamp() >= _nextStaleCheck)
            {
                _nextStaleCheck = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
                lock (_gate) CheckFreshness();
            }
        }
        if (_queue == null) return;
        // Next tick or frame boundary follows state sending in this engine. Includes inter-tick overhead,
        // conservatively, but never frame sleep. No profiler or engine changes are required.
        if (level is not (ModUpdateLevel.PreEngine or ModUpdateLevel.FramePreEngine)) return;
        if (_tickStarted == 0)
        {
            if (level == ModUpdateLevel.PreEngine) _tickStarted = Stopwatch.GetTimestamp();
            return;
        }
        var elapsed = (long)(Stopwatch.GetElapsedTime(_tickStarted).TotalMilliseconds * 1000);
        _tickStarted = level == ModUpdateLevel.PreEngine ? Stopwatch.GetTimestamp() : 0;
        long previous;
        do { previous = Interlocked.Read(ref _maxSimulationMicroseconds); if (previous >= elapsed) break; }
        while (Interlocked.CompareExchange(ref _maxSimulationMicroseconds, elapsed, previous) != previous);
        Interlocked.Exchange(ref _tickBudgetMicroseconds, Math.Max(1, (long)(_timing.TickPeriod.TotalMilliseconds * 1000)));
        Interlocked.Exchange(ref _lastTickStamp, Stopwatch.GetTimestamp());
    }

    private void Start()
    {
        var inference = AdminAICVars.Read(_config);
        var queueOptions = AdminAICVars.ReadQueue(_config);
        var governorOptions = AdminAICVars.ReadGovernor(_config);
        var groups = _config.GetCVar(AdminAICVars.CgroupPaths).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!inference.Enabled || !inference.IsValid() || !queueOptions.IsValid() || !governorOptions.IsValid()
            || _config.GetCVar(AdminAICVars.MaxConcurrentRequests) != 1 || groups.Length > 4
            || Array.Exists(groups, p => !LinuxAIPressureReader.IsValidCgroupPath(p)))
        { lock (_gate) _reason = _config.GetCVar(AdminAICVars.Enabled) ? "invalid-configuration" : "disabled"; return; }
        _lifetime = new();
        _governor = new(governorOptions);
        _governorEnabled = governorOptions.Enabled; _maxSampleAgeSeconds = governorOptions.MaxSampleAgeSeconds;
        Interlocked.Exchange(ref _lastLoadStamp, 0);
        var queue = new AIRequestQueue(new UdsAIInferenceClient(inference), queueOptions, audit: Audit);
        lock (_gate) { _queue = queue; _health = AIWorkerHealth.Unavailable; _reason = "metrics-unavailable"; }
        var token = _lifetime.Token;
        _poll = Task.Run(() => PollLoad(queue, governorOptions, groups, token));
        _healthPoll = Task.Run(() => PollHealth(queue, inference, token));
        _log.Info("AI infrastructure enabled; model output is private, automatic public FAQ requires separate opt-in.");
    }

    private async Task PollLoad(AIRequestQueue queue, AIGovernorOptions options, string[] groups, CancellationToken token)
    {
        var reader = new LinuxAIPressureReader();
        try
        {
            while (!token.IsCancellationRequested)
            {
                var now = Stopwatch.GetElapsedTime(_started);
                var tickStamp = Interlocked.Read(ref _lastTickStamp);
                var budget = Interlocked.Read(ref _tickBudgetMicroseconds);
                var busy = Interlocked.Exchange(ref _maxSimulationMicroseconds, 0);
                double? ratio = tickStamp == 0 || budget <= 0 || Stopwatch.GetElapsedTime(tickStamp).TotalSeconds > options.MaxSampleAgeSeconds
                    ? null : (double)busy / budget;
                var load = await reader.ReadAsync(groups, now, ratio, token).ConfigureAwait(false);
                if (token.IsCancellationRequested) return;
                string? transition = null;
                lock (_gate)
                {
                    var governor = _governor!;
                    _load = load; var state = governor.Observe(load, Stopwatch.GetElapsedTime(_started));
                    if (state != _state)
                    {
                        var details = load == null ? "load=unavailable" : string.Create(CultureInfo.InvariantCulture,
                            $"cpu_psi={load.CpuSomeAvg10:F2} memory_psi={load.MemoryFullAvg10:F2} io_psi={load.IoFullAvg10:F2} tick_max_ratio={load.SimulationBudgetRatio:F3}");
                        transition = $"AI governor: {_state} -> {state}, reason={governor.Reason}, {details}";
                    }
                    _state = state; _reason = governor.Reason;
                    Interlocked.Exchange(ref _lastLoadStamp, load == null ? 0 : Stopwatch.GetTimestamp());
                    queue.SetAvailability(state, _health);
                }
                if (transition != null) _log.Info(transition);
                await Task.Delay(TimeSpan.FromSeconds(options.PollSeconds), token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception)
        {
            if (token.IsCancellationRequested) return;
            lock (_gate) { _state = AIResourceState.Paused; _reason = "metrics-failed"; queue.SetAvailability(_state, AIWorkerHealth.Unavailable); }
            _log.Warning("AI metrics task failed; inference paused.");
        }
    }

    private async Task PollHealth(AIRequestQueue queue, AIInferenceOptions options, CancellationToken token)
    {
        using var client = new UdsAIInferenceClient(options with { TimeoutSeconds = Math.Min(options.TimeoutSeconds, 3) });
        try
        {
            while (!token.IsCancellationRequested)
            {
                var health = await client.CheckHealthAsync(token).ConfigureAwait(false);
                if (token.IsCancellationRequested) return;
                string? transition = null;
                lock (_gate)
                {
                    if (_health != health) transition = $"AI worker health: {_health} -> {health}";
                    _health = health; CheckFreshness(); queue.SetAvailability(_state, health);
                }
                if (transition != null) _log.Info(transition);
                await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception)
        {
            if (token.IsCancellationRequested) return;
            lock (_gate) { _health = AIWorkerHealth.Unavailable; queue.SetAvailability(AIResourceState.Paused, _health); }
            _log.Warning("AI health task failed; inference unavailable.");
        }
    }

    public AdminAISnapshot Snapshot()
    {
        AIRequestQueue? queue; AIResourceState state; AIWorkerHealth health; string reason; AILoadSample? load;
        lock (_gate) { queue = _queue; state = _state; health = _health; reason = _reason; load = _load; }
        return new(queue != null, state, health, reason, queue?.Snapshot(), load);
    }

    public Task<AIInferenceResult> EnqueueAsync(AIInferenceRequest request, AIRequestClass kind, CancellationToken token)
    {
        AIRequestQueue? queue;
        lock (_gate) { CheckFreshness(); queue = _queue; }
        return queue?.EnqueueAsync(request, kind, token) ?? Task.FromResult(new AIInferenceResult(request.CorrelationId, AIInferenceStatus.Disabled));
    }

    private void Audit(Guid id, AIInferenceStatus status) => _log.Info($"AI request correlation={id} result={status}");

    private void CheckFreshness()
    {
        if (_queue == null || !_governorEnabled) return;
        var stamp = Interlocked.Read(ref _lastLoadStamp);
        if (stamp != 0 && Stopwatch.GetElapsedTime(stamp).TotalSeconds <= _maxSampleAgeSeconds) return;
        if (_state != AIResourceState.Paused) _ = Task.Run(() => _log.Warning("AI governor -> Paused: metrics-stale"));
        _state = _governor!.Observe(null, Stopwatch.GetElapsedTime(_started));
        _reason = "metrics-stale";
        _queue.SetAvailability(_state, _health);
    }

    private void Stop()
    {
        var lifetime = _lifetime; _lifetime = null;
        AIRequestQueue? queue;
        lock (_gate)
        {
            queue = _queue; _queue = null; _load = null;
            _state = AIResourceState.Paused; _health = AIWorkerHealth.Disabled; _reason = "disabled";
        }
        if (lifetime == null || queue == null) return;
        _tickStarted = 0;
        Interlocked.Exchange(ref _maxSimulationMicroseconds, 0);
        Interlocked.Exchange(ref _lastTickStamp, 0);
        // First stop new generation synchronously; all waits/disposal happen away from the game thread.
        queue.Disable();
        var poll = _poll; var health = _healthPoll;
        _stopping = Task.Run(async () =>
        {
            try { lifetime.Cancel(); await Task.WhenAll(queue.DisposeAsync().AsTask(), poll, health); }
            catch (Exception) { _log.Warning("AI shutdown completed with a background error."); }
            finally { lifetime.Dispose(); }
        });
    }

    public void Shutdown()
    {
        if (!_initialized || _shutdown) return;
        _shutdown = true;
        foreach (var cvar in AdminAICVars.InfrastructureBools) _config.UnsubValueChanged(cvar, _boolChanged);
        foreach (var cvar in AdminAICVars.InfrastructureInts) _config.UnsubValueChanged(cvar, _intChanged);
        foreach (var cvar in AdminAICVars.RuntimeFloats) _config.UnsubValueChanged(cvar, _floatChanged);
        foreach (var cvar in AdminAICVars.RuntimeStrings) _config.UnsubValueChanged(cvar, _stringChanged);
        Stop();
        _meter?.Dispose();
    }
}

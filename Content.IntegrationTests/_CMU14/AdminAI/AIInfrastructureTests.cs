using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.CMU14.AdminAI;
using NUnit.Framework;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture]
public sealed class AIInfrastructureTests
{
    private static AIInferenceRequest Request(string text = "synthetic") => new(Guid.NewGuid(), text);
    private static AILoadSample Load(int second, double cpu = 0, double memory = 0, double io = 0, double tick = 0.1)
        => new(TimeSpan.FromSeconds(second), cpu, memory, io, tick);

    [Test]
    public void GovernorProtectsImmediatelyAndRecoversInTwoStableWindows()
    {
        var governor = new AIResourceGovernor(new(RecoverySeconds: 5));
        Assert.That(governor.Observe(Load(0), TimeSpan.Zero), Is.EqualTo(AIResourceState.Paused));
        Assert.That(governor.Observe(Load(5), TimeSpan.FromSeconds(5)), Is.EqualTo(AIResourceState.Degraded));
        Assert.That(governor.Observe(Load(10), TimeSpan.FromSeconds(10)), Is.EqualTo(AIResourceState.Normal));
        Assert.That(governor.Observe(Load(11, cpu: 10), TimeSpan.FromSeconds(11)), Is.EqualTo(AIResourceState.Degraded));
        Assert.That(governor.Observe(Load(12, cpu: 25), TimeSpan.FromSeconds(12)), Is.EqualTo(AIResourceState.Paused));
        Assert.That(governor.Observe(Load(13, cpu: 10), TimeSpan.FromSeconds(13)), Is.EqualTo(AIResourceState.Paused));
        Assert.That(governor.Observe(Load(14), TimeSpan.FromSeconds(14)), Is.EqualTo(AIResourceState.Paused));
        Assert.That(governor.Observe(Load(18, tick: 0.65), TimeSpan.FromSeconds(18)), Is.EqualTo(AIResourceState.Paused));
        Assert.That(governor.Observe(Load(19), TimeSpan.FromSeconds(19)), Is.EqualTo(AIResourceState.Paused));
        Assert.That(governor.Observe(Load(24), TimeSpan.FromSeconds(24)), Is.EqualTo(AIResourceState.Degraded));
    }

    [TestCase(25, 0, 0, 0.1)]
    [TestCase(0, 5, 0, 0.1)]
    [TestCase(0, 0, 15, 0.1)]
    [TestCase(0, 0, 0, 1.2)]
    [TestCase(double.NaN, 0, 0, 0.1)]
    [TestCase(0, double.PositiveInfinity, 0, 0.1)]
    [TestCase(-1, 0, 0, 0.1)]
    [TestCase(0, 0, 0, -1)]
    public void PressureOrInvalidMeasurementsPause(double cpu, double memory, double io, double tick)
    {
        var governor = new AIResourceGovernor(new(RecoverySeconds: 5));
        governor.Observe(Load(0), TimeSpan.Zero);
        governor.Observe(Load(5), TimeSpan.FromSeconds(5));
        governor.Observe(Load(10), TimeSpan.FromSeconds(10));
        Assert.That(governor.Observe(new(TimeSpan.FromSeconds(11), cpu, memory, io, tick), TimeSpan.FromSeconds(11)),
            Is.EqualTo(AIResourceState.Paused));
    }

    [Test]
    public void MissingStaleAndFutureMetricsPauseUnlessOperatorDisablesGovernor()
    {
        var governor = new AIResourceGovernor(new());
        Assert.That(governor.Observe(null, TimeSpan.Zero), Is.EqualTo(AIResourceState.Paused));
        Assert.That(governor.Observe(Load(0), TimeSpan.FromSeconds(6)), Is.EqualTo(AIResourceState.Paused));
        Assert.That(governor.Observe(Load(2), TimeSpan.FromSeconds(1)), Is.EqualTo(AIResourceState.Paused));
        Assert.That(new AIResourceGovernor(new(Enabled: false)).Observe(null, TimeSpan.Zero), Is.EqualTo(AIResourceState.Normal));
        Assert.That(new AIResourceGovernor(new(CpuPaused: 1)).Observe(Load(0), TimeSpan.Zero), Is.EqualTo(AIResourceState.Paused));
    }

    [TestCase("some avg10=12.34 avg60=1.0 total=100\nfull avg10=0.25 avg60=0.0 total=2\n", "some", true, 12.34)]
    [TestCase("some avg10=12.34\nfull avg10=0.25\n", "full", true, 0.25)]
    [TestCase("some avg10=1.5\n", "full", false, 0)]
    [TestCase("some avg10=NaN\n", "some", false, 0)]
    [TestCase("some avg10=101\n", "some", false, 0)]
    [TestCase("some avg10=-1\n", "some", false, 0)]
    [TestCase("some avg10=1,5\n", "some", false, 0)]
    public void PsiUsesSpecifiedCategoryAndInvariantFinitePercentage(string text, string category, bool valid, double expected)
    {
        var parsed = LinuxAIPressureReader.TryParseAvg10(text, category, out var value);
        Assert.That(parsed, Is.EqualTo(valid));
        if (valid) Assert.That(value, Is.EqualTo(expected));
    }

    [TestCase("/sys/fs/cgroup/system.slice/rucmu.service", true)]
    [TestCase("/sys/fs/cgroup/../etc", false)]
    [TestCase("/tmp/test", false)]
    [TestCase("/sys/fs/cgroup/", false)]
    [TestCase("/sys/fs/cgroup/a\tb", false)]
    public void CgroupPathsStayInsideFixedTree(string path, bool valid)
        => Assert.That(LinuxAIPressureReader.IsValidCgroupPath(path), Is.EqualTo(valid));

    [Test]
    public void LimitsRejectUnsafeOrInconsistentConfiguration()
    {
        Assert.That(new AIQueueOptions(Capacity: 33).IsValid(), Is.False);
        Assert.That(new AIQueueOptions(DegradedInputChars: 5000).IsValid(), Is.False);
        Assert.That(new AIQueueOptions(TtlSeconds: 0).IsValid(), Is.False);
        Assert.That(new AIGovernorOptions(PollSeconds: 3, MaxSampleAgeSeconds: 5).IsValid(), Is.False);
        Assert.That(new AIGovernorOptions(CpuDegraded: double.NaN).IsValid(), Is.False);
    }

    [Test]
    public async Task QueueIsBoundedDeduplicatedAndPrioritized()
    {
        var client = new ControlledClient();
        await using var queue = Ready(client, new(Capacity: 3));
        var first = Request(); var firstTask = queue.EnqueueAsync(first, AIRequestClass.Background, default);
        await client.Next();
        Assert.That((await queue.EnqueueAsync(first, AIRequestClass.LightManual, default)).Status, Is.EqualTo(AIInferenceStatus.Duplicate));
        var background = queue.EnqueueAsync(Request(), AIRequestClass.Background, default);
        var heavy = queue.EnqueueAsync(Request(), AIRequestClass.HeavyManual, default);
        var light = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default);
        Assert.That((await queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default)).Status, Is.EqualTo(AIInferenceStatus.QueueFull));
        Assert.That(queue.Snapshot().Pending, Is.EqualTo(3));
        client.Release(); await firstTask.WaitAsync(TimeSpan.FromSeconds(3));
        await client.Next(); client.Release(); await light.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.That(heavy.IsCompleted || background.IsCompleted, Is.False);
        await client.Next(); client.Release(); await heavy.WaitAsync(TimeSpan.FromSeconds(3));
        await client.Next(); client.Release(); await background.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.That(client.MaxConcurrent, Is.EqualTo(1));
    }

    [Test]
    public async Task PendingTtlAndCancellationReclaimCapacity()
    {
        long seconds = 0;
        var client = new ControlledClient();
        await using var queue = Ready(client, new(Capacity: 2, TtlSeconds: 1), () => TimeSpan.FromSeconds(Interlocked.Read(ref seconds)));
        var active = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default); await client.Next();
        var expired = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default);
        using var token = new CancellationTokenSource();
        var cancelled = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, token.Token);
        token.Cancel();
        Assert.That((await cancelled).Status, Is.EqualTo(AIInferenceStatus.Cancelled));
        Interlocked.Exchange(ref seconds, 2);
        Assert.That((await expired.WaitAsync(TimeSpan.FromSeconds(3))).Status, Is.EqualTo(AIInferenceStatus.Expired));
        Assert.That(queue.Snapshot().Pending, Is.Zero);
        client.Release(); await active;
    }

    [Test]
    public async Task PauseClearsPendingCancelsActiveAndDiscardsLateOutput()
    {
        var client = new ControlledClient(ignoreCancellation: true);
        await using var queue = Ready(client, new());
        var active = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default); await client.Next();
        var pending = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default);
        queue.SetAvailability(AIResourceState.Paused, AIWorkerHealth.Ready);
        Assert.That((await active).Status, Is.EqualTo(AIInferenceStatus.Paused));
        Assert.That((await pending).Status, Is.EqualTo(AIInferenceStatus.Paused));
        Assert.That((await queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default)).Status, Is.EqualTo(AIInferenceStatus.Paused));
        client.Release();
        Assert.That((await active).UntrustedText, Is.Null);
    }

    [Test]
    public async Task IgnoredCancellationKeepsPhysicalSlotWhileWaitingTasksExpire()
    {
        long seconds = 0;
        var client = new ControlledClient(ignoreCancellation: true);
        await using var queue = Ready(client, new(TtlSeconds: 1), () => TimeSpan.FromSeconds(Interlocked.Read(ref seconds)));
        using var token = new CancellationTokenSource();
        var active = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, token.Token); await client.Next();
        var pending = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default);
        token.Cancel(); Assert.That((await active).Status, Is.EqualTo(AIInferenceStatus.Cancelled));
        Interlocked.Exchange(ref seconds, 2);
        Assert.That((await pending.WaitAsync(TimeSpan.FromSeconds(3))).Status, Is.EqualTo(AIInferenceStatus.Expired));
        Assert.That(client.Calls, Is.EqualTo(1));
        client.Release();
    }

    [Test]
    public async Task TimeoutDoesNotStartAnotherPhysicalRequestUntilClientCompletes()
    {
        var client = new ControlledClient(ignoreCancellation: true);
        await using var queue = Ready(client, new(TimeoutSeconds: 1));
        var active = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default); await client.Next();
        var pending = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default);
        Assert.That((await active.WaitAsync(TimeSpan.FromSeconds(3))).Status, Is.EqualTo(AIInferenceStatus.TimedOut));
        Assert.That(client.Calls, Is.EqualTo(1)); Assert.That(pending.IsCompleted, Is.False);
        client.Release(); await client.Next(); client.Release(); await pending;
        Assert.That(client.MaxConcurrent, Is.EqualTo(1));
    }

    [Test]
    public async Task DegradedAllowsOnlySmallManualRequestsWithReducedBudget()
    {
        var client = new ControlledClient();
        await using var queue = Ready(client, new(DegradedInputChars: 16, DegradedOutputTokens: 32));
        queue.SetAvailability(AIResourceState.Degraded, AIWorkerHealth.Ready);
        foreach (var kind in new[] { AIRequestClass.HeavyManual, AIRequestClass.Background })
            Assert.That((await queue.EnqueueAsync(Request(), kind, default)).Status, Is.EqualTo(AIInferenceStatus.Paused));
        Assert.That((await queue.EnqueueAsync(Request(new string('x', 17)), AIRequestClass.LightManual, default)).Status, Is.EqualTo(AIInferenceStatus.Paused));
        var task = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default);
        var request = await client.Next();
        Assert.That(request.OutputTokenLimit, Is.EqualTo(32)); client.Release(); await task;
    }

    [Test]
    public async Task DialogueCannotBypassQueueAdmissionOrDegradedBudget()
    {
        var client = new ControlledClient();
        await using var queue = Ready(client, new(MaxInputChars: 32, DegradedInputChars: 16));
        var request = Request("sources") with { Dialogue = [new(false, new string('x', 26))] };
        Assert.That((await queue.EnqueueAsync(request, AIRequestClass.LightManual, default)).Status,
            Is.EqualTo(AIInferenceStatus.InvalidRequest));
        queue.SetAvailability(AIResourceState.Degraded, AIWorkerHealth.Ready);
        request = request with { Dialogue = [new(false, new string('x', 10))] };
        Assert.That((await queue.EnqueueAsync(request, AIRequestClass.LightManual, default)).Status,
            Is.EqualTo(AIInferenceStatus.Paused));
        Assert.That(client.Calls, Is.Zero);
    }

    [Test]
    public async Task DisableIsPermanentEvenIfStaleHealthPollReportsReady()
    {
        var client = new ControlledClient(); var queue = Ready(client, new());
        var active = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default); await client.Next();
        queue.Disable(); queue.SetAvailability(AIResourceState.Normal, AIWorkerHealth.Ready);
        Assert.That((await active).Status, Is.EqualTo(AIInferenceStatus.Disabled));
        Assert.That((await queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default)).Status, Is.EqualTo(AIInferenceStatus.Disabled));
        await queue.DisposeAsync(); await queue.DisposeAsync();
    }

    [TestCase(true, AIInferenceStatus.Unavailable)]
    [TestCase(false, AIInferenceStatus.InvalidResponse)]
    public async Task ClientFailureOrWrongCorrelationCannotPublishDraft(bool throws, AIInferenceStatus expected)
    {
        await using var queue = Ready(new ImmediateClient(throws), new());
        var result = await queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default).WaitAsync(TimeSpan.FromSeconds(3));
        Assert.That(result.Status, Is.EqualTo(expected)); Assert.That(result.UntrustedText, Is.Null);
    }

    [Test]
    public async Task ArtificialQueueSmokeEnforcesOneSlotAndBoundedWaiting()
    {
        var client = new ControlledClient();
        await using var queue = Ready(client, new());
        var first = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default); await client.Next();
        var tasks = Enumerable.Range(0, 1000).Select(_ => queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default)).ToArray();
        Assert.That(queue.Snapshot().Pending, Is.EqualTo(8));
        var rejected = await Task.WhenAll(tasks.Where(t => t.IsCompleted));
        Assert.That(rejected.Count(result => result.Status == AIInferenceStatus.QueueFull), Is.EqualTo(992));
        client.Release(); await first;
        for (var i = 0; i < 8; i++) { await client.Next(); client.Release(); }
        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(results.Count(r => r.Status == AIInferenceStatus.Success), Is.EqualTo(8));
        Assert.That(client.MaxConcurrent, Is.EqualTo(1));
        Assert.That(queue.Snapshot().Accepted, Is.EqualTo(9));
        TestContext.Out.WriteLine("Queue smoke: submitted=1001, accepted=9, rejected=992, max_pending=8, max_inference=1");
    }

    private static AIRequestQueue Ready(IAIInferenceClient client, AIQueueOptions options, Func<TimeSpan>? now = null)
    {
        var queue = new AIRequestQueue(client, options, now);
        queue.SetAvailability(AIResourceState.Normal, AIWorkerHealth.Ready); return queue;
    }

    [Test]
    public async Task AdmissionRejectsInvalidCancelledAndUnavailableWithoutCallingClient()
    {
        var client = new ControlledClient();
        await using var queue = Ready(client, new());
        foreach (var request in new[] { new AIInferenceRequest(Guid.Empty, "test"), Request(" "), Request(new string('x', 4097)), Request() with { OutputTokenLimit = 0 } })
            Assert.That((await queue.EnqueueAsync(request, AIRequestClass.LightManual, default)).Status, Is.EqualTo(AIInferenceStatus.InvalidRequest));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.That((await queue.EnqueueAsync(Request(), AIRequestClass.LightManual, cancellation.Token)).Status, Is.EqualTo(AIInferenceStatus.Cancelled));
        queue.SetAvailability(AIResourceState.Normal, AIWorkerHealth.Unavailable);
        Assert.That((await queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default)).Status, Is.EqualTo(AIInferenceStatus.Unavailable));
        Assert.That(client.Calls, Is.Zero);
    }

    [TestCase(AIResourceState.Degraded, AIWorkerHealth.Ready, AIInferenceStatus.Paused)]
    [TestCase(AIResourceState.Normal, AIWorkerHealth.Unavailable, AIInferenceStatus.Unavailable)]
    public async Task LoadOrHealthChangeCancelsRunningRequest(AIResourceState state, AIWorkerHealth health, AIInferenceStatus status)
    {
        var client = new ControlledClient(ignoreCancellation: true);
        await using var queue = Ready(client, new());
        var task = queue.EnqueueAsync(Request(), AIRequestClass.LightManual, default); await client.Next();
        queue.SetAvailability(state, health);
        Assert.That((await task).Status, Is.EqualTo(status)); Assert.That((await task).UntrustedText, Is.Null);
        client.Release();
    }

    private sealed class ControlledClient(bool ignoreCancellation = false) : IAIInferenceClient
    {
        private readonly ConcurrentQueue<(AIInferenceRequest Request, TaskCompletionSource<AIInferenceResult> Done)> _operations = new();
        private readonly SemaphoreSlim _entered = new(0);
        private int _running, _calls, _max;
        public int Calls => Volatile.Read(ref _calls);
        public int MaxConcurrent => Volatile.Read(ref _max);
        public Task<AIWorkerHealth> CheckHealthAsync(CancellationToken token) => Task.FromResult(AIWorkerHealth.Ready);
        public async Task<AIInferenceResult> InferAsync(AIInferenceRequest request, CancellationToken token)
        {
            var running = Interlocked.Increment(ref _running); Interlocked.Increment(ref _calls);
            _max = Math.Max(_max, running);
            var done = new TaskCompletionSource<AIInferenceResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _operations.Enqueue((request, done)); _entered.Release();
            try { return ignoreCancellation ? await done.Task : await done.Task.WaitAsync(token); }
            finally { Interlocked.Decrement(ref _running); }
        }
        public async Task<AIInferenceRequest> Next()
        {
            Assert.That(await _entered.WaitAsync(TimeSpan.FromSeconds(3)), Is.True, "Inference did not start");
            Assert.That(_operations.TryPeek(out var operation), Is.True); return operation.Request;
        }
        public void Release()
        {
            Assert.That(_operations.TryDequeue(out var operation), Is.True);
            operation.Done.TrySetResult(new(operation.Request.CorrelationId, AIInferenceStatus.Success, "synthetic draft"));
        }
    }

    private sealed class ImmediateClient(bool throws) : IAIInferenceClient
    {
        public Task<AIWorkerHealth> CheckHealthAsync(CancellationToken token) => Task.FromResult(AIWorkerHealth.Ready);
        public Task<AIInferenceResult> InferAsync(AIInferenceRequest request, CancellationToken token) => throws
            ? Task.FromException<AIInferenceResult>(new InvalidOperationException("synthetic failure"))
            : Task.FromResult(new AIInferenceResult(Guid.NewGuid(), AIInferenceStatus.Success, "wrong request"));
    }
}

using Content.Server.CMU14.AdminAI;
using Content.Tools.AdminAI.Spike;
using NUnit.Framework;

namespace Content.Tools.AdminAI.Tests;

[TestFixture, NonParallelizable]
public sealed class UdsSpikeTests
{
    private static string GetSocketPath()
    {
        var path = Environment.GetEnvironmentVariable("RUCMU_AI_TEST_SOCKET");
        if (string.IsNullOrWhiteSpace(path))
            Assert.Ignore("Set RUCMU_AI_TEST_SOCKET to a short, writable absolute .sock path. IPC gate remains unverified.");
        return path!;
    }

    [Test]
    public async Task DisabledClientDoesNotConnectToSocket()
    {
        using var client = new UdsAIInferenceClient("/run/rucmu-ai/nonexistent.sock", new());
        Assert.That(await client.CheckHealthAsync(CancellationToken.None), Is.EqualTo(AIWorkerHealth.Disabled));
        Assert.That((await client.InferAsync(new(Guid.NewGuid(), "synthetic"), CancellationToken.None)).Status,
            Is.EqualTo(AIInferenceStatus.Disabled));
    }

    [Test]
    public async Task MissingWorkerAndCancelledRequestFailWithoutDraft()
    {
        using var client = new UdsAIInferenceClient("/run/rucmu-ai/nonexistent.sock", new(Enabled: true, TimeoutSeconds: 1));
        var result = await client.InferAsync(new(Guid.NewGuid(), "synthetic"), CancellationToken.None);
        Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Unavailable));
        Assert.That(result.UntrustedText, Is.Null);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        result = await client.InferAsync(new(Guid.NewGuid(), "synthetic"), cancellation.Token);
        Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Cancelled));
    }

    [Test, Category("LocalIPC")]
    public async Task ActualHttpOverUnixSocketRoundTrip()
    {
        var path = GetSocketPath();
        using var worker = new MockWorker(path);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var workerTask = worker.RunAsync(cancellation.Token);
        try
        {
            using var client = new UdsAIInferenceClient(path, new(Enabled: true, TimeoutSeconds: 2));
            Assert.That(await client.CheckHealthAsync(cancellation.Token), Is.EqualTo(AIWorkerHealth.Ready));
            var result = await client.InferAsync(new(Guid.NewGuid(), "synthetic probe"), cancellation.Token);
            Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Success));
            Assert.That(result.UntrustedText, Does.StartWith("[MOCK WORKER: NO MODEL]"));
        }
        finally
        {
            cancellation.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }

    [TestCase(MockWorkerFault.StallHeaders), TestCase(MockWorkerFault.StallBody), Category("LocalIPC")]
    public async Task InFlightRequestTimesOutAndWorkerCanServeAgain(MockWorkerFault fault)
    {
        var path = GetSocketPath();
        using var worker = new MockWorker(path, fault);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var workerTask = worker.RunAsync(lifetime.Token);
        try
        {
            using var client = new UdsAIInferenceClient(path, new(Enabled: true, TimeoutSeconds: 1));
            var pending = client.InferAsync(new(Guid.NewGuid(), "synthetic timeout test"), lifetime.Token);
            await worker.RequestReceived.WaitAsync(TimeSpan.FromSeconds(3));
            var result = await pending.WaitAsync(TimeSpan.FromSeconds(4));
            Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.TimedOut));
            Assert.That(result.UntrustedText, Is.Null);
            worker.ReleaseStall();
            Assert.That(await client.CheckHealthAsync(lifetime.Token), Is.EqualTo(AIWorkerHealth.Ready));
        }
        finally
        {
            lifetime.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }

    [TestCase(MockWorkerFault.StallHeaders), TestCase(MockWorkerFault.StallBody), Category("LocalIPC")]
    public async Task CancellationAfterWorkerReceivesRequestDoesNotReturnDraft(MockWorkerFault fault)
    {
        var path = GetSocketPath();
        using var worker = new MockWorker(path, fault);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var requestCancellation = new CancellationTokenSource();
        var workerTask = worker.RunAsync(lifetime.Token);
        try
        {
            using var client = new UdsAIInferenceClient(path, new(Enabled: true, TimeoutSeconds: 5));
            var pending = client.InferAsync(new(Guid.NewGuid(), "synthetic cancellation test"), requestCancellation.Token);
            await worker.RequestReceived.WaitAsync(TimeSpan.FromSeconds(3));
            requestCancellation.Cancel();
            var result = await pending.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Cancelled));
            Assert.That(result.UntrustedText, Is.Null);
            worker.ReleaseStall();
            Assert.That(await client.CheckHealthAsync(lifetime.Token), Is.EqualTo(AIWorkerHealth.Ready));
        }
        finally
        {
            lifetime.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }

    [TestCase(MockWorkerFault.Disconnect, AIInferenceStatus.Unavailable)]
    [TestCase(MockWorkerFault.TruncatedBody, AIInferenceStatus.Unavailable)]
    [TestCase(MockWorkerFault.MalformedJson, AIInferenceStatus.InvalidResponse)]
    [TestCase(MockWorkerFault.EmptyBody, AIInferenceStatus.InvalidResponse)]
    [TestCase(MockWorkerFault.OversizedBodyWithoutLength, AIInferenceStatus.InvalidResponse)]
    [TestCase(MockWorkerFault.Redirect, AIInferenceStatus.Unavailable)]
    [Category("LocalIPC")]
    public async Task FailedWorkerResponseIsRejectedWithoutDraft(MockWorkerFault fault, AIInferenceStatus expected)
    {
        var path = GetSocketPath();
        using var worker = new MockWorker(path, fault);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var workerTask = worker.RunAsync(lifetime.Token);
        try
        {
            using var client = new UdsAIInferenceClient(path, new(Enabled: true, TimeoutSeconds: 2));
            var result = await client.InferAsync(new(Guid.NewGuid(), "synthetic fault test"), lifetime.Token);
            Assert.That(result.Status, Is.EqualTo(expected));
            Assert.That(result.UntrustedText, Is.Null);
            Assert.That(worker.RequestCount, Is.EqualTo(1), "The client must not retry or follow redirects.");
            Assert.That(await client.CheckHealthAsync(lifetime.Token), Is.EqualTo(AIWorkerHealth.Ready));
        }
        finally
        {
            lifetime.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }

    [Test, Category("LocalIPC")]
    public async Task ExistingWorkerSocketIsNotDeletedByFailedSecondBind()
    {
        var path = GetSocketPath();
        using var worker = new MockWorker(path);
        Assert.Throws<System.Net.Sockets.SocketException>(() => { using var duplicate = new MockWorker(path); });
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var workerTask = worker.RunAsync(lifetime.Token);
        try
        {
            using var client = new UdsAIInferenceClient(path, new(Enabled: true, TimeoutSeconds: 2));
            Assert.That(await client.CheckHealthAsync(lifetime.Token), Is.EqualTo(AIWorkerHealth.Ready));
        }
        finally
        {
            lifetime.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }
}

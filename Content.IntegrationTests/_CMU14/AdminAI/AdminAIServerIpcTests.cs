using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.CMU14.AdminAI;
using Content.Tools.AdminAI.Spike;
using NUnit.Framework;
using Robust.Server;
using Robust.Shared;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.UnitTesting;

namespace Content.IntegrationTests.CMU14.AdminAI;

/// <summary>Actual Content.Server transport on an isolated engine server thread, without game prototypes or players.</summary>
[TestFixture, NonParallelizable, Category("LocalIPC")]
public sealed class AdminAIServerIpcTests
{
    [Test]
    public async Task DisabledOrInvalidServiceNeverStartsQueueAndReloadsSafely()
    {
        var paths = Paths();
        using var server = StartServer(paths.Resources);
        await server.WaitIdleAsync();
        AdminAIService? service = null;
        try
        {
            await server.WaitAssertion(() =>
            {
                var config = server.ResolveDependency<IConfigurationManager>();
                foreach (var cvar in AdminAICVars.RuntimeBools) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeInts) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeFloats) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeStrings) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                service = new AdminAIService();
                server.InstanceDependencyCollection.InjectDependencies(service);
                service.Initialize(); service.ObservePhase(ModUpdateLevel.InputPostEngine);
                Assert.That(service.Snapshot().Enabled, Is.False);
                Assert.That(service.Snapshot().Queue, Is.Null);
                Assert.That(service.Snapshot().Health, Is.EqualTo(AIWorkerHealth.Disabled));
                config.SetCVar(AdminAICVars.MaxConcurrentRequests, 2);
                config.SetCVar(AdminAICVars.Enabled, true);
                service.ObservePhase(ModUpdateLevel.InputPostEngine);
                Assert.That(service.Snapshot().Enabled, Is.False);
                Assert.That(service.Snapshot().Reason, Is.EqualTo("invalid-configuration"));
                config.SetCVar(AdminAICVars.Enabled, false);
                service.ObservePhase(ModUpdateLevel.InputPostEngine);
                Assert.That(service.Snapshot().Reason, Is.EqualTo("disabled"));
                service.Shutdown(); service.Shutdown();
                config.SetCVar(AdminAICVars.Enabled, true);
                service.ObservePhase(ModUpdateLevel.InputPostEngine);
                Assert.That(service.Snapshot().Enabled, Is.False);
            });
        }
        finally { if (service != null) await server.WaitPost(service.Shutdown); }
    }

    [Test]
    public async Task CompiledQueueExpiresWaitingTasksAndPausesWithoutBlockingServerThread()
    {
        var paths = Paths();
        using var worker = new MockWorker(paths.Socket, MockWorkerFault.StallHeaders);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var workerTask = worker.RunAsync(lifetime.Token);
        AIRequestQueue? queue = null;
        try
        {
            using var server = StartServer(paths.Resources);
            await server.WaitIdleAsync();
            Task<AIInferenceResult>? active = null, pending = null;
            await server.WaitPost(() =>
            {
                queue = new(new UdsAIInferenceClient(paths.Socket, new(Enabled: true, TimeoutSeconds: 5)), new(TtlSeconds: 1));
                queue.SetAvailability(AIResourceState.Normal, AIWorkerHealth.Ready);
                active = queue.EnqueueAsync(new(Guid.NewGuid(), "synthetic queue smoke"), AIRequestClass.LightManual, default);
            });
            await worker.RequestReceived.WaitAsync(TimeSpan.FromSeconds(3));
            await server.WaitPost(() => pending = queue!.EnqueueAsync(new(Guid.NewGuid(), "synthetic pending"), AIRequestClass.LightManual, default));
            Assert.That((await pending!.WaitAsync(TimeSpan.FromSeconds(3))).Status, Is.EqualTo(AIInferenceStatus.Expired));
            await server.WaitAssertion(() =>
            {
                Assert.That(server.IsAlive, Is.True);
                Assert.That(active!.IsCompleted, Is.False);
                queue!.SetAvailability(AIResourceState.Paused, AIWorkerHealth.Ready);
            });
            var result = await active!.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Paused)); Assert.That(result.UntrustedText, Is.Null);
            await server.WaitAssertion(() => Assert.That(server.IsAlive, Is.True));
        }
        finally
        {
            if (queue != null) await queue.DisposeAsync();
            lifetime.Cancel();
            try { await workerTask; } catch (OperationCanceledException) { }
        }
    }

    private static (string Socket, string Resources) Paths()
    {
        var socket = Environment.GetEnvironmentVariable("RUCMU_AI_TEST_SOCKET");
        var resources = Environment.GetEnvironmentVariable("RUCMU_AI_ENGINE_RESOURCES");
        if (string.IsNullOrWhiteSpace(socket) || string.IsNullOrWhiteSpace(resources))
            Assert.Ignore("Set RUCMU_AI_TEST_SOCKET and RUCMU_AI_ENGINE_RESOURCES for the real server-thread IPC spike.");
        return (socket!, resources!);
    }

    private static RobustIntegrationTest.ServerIntegrationInstance StartServer(string resources)
    {
        return new(new RobustIntegrationTest.ServerIntegrationOptions
        {
            ContentStart = false,
            ContentAssemblies = [],
            Options = new ServerOptions
            {
                LoadConfigAndUserData = false,
                LoadContentResources = false,
                PrototypeDirectory = new ResPath("/AdminAIStageAEmptyPrototypes"),
                MountOptions = new MountOptions(dirMounts: [resources], zipMounts: []),
            },
        });
    }

    [Test]
    public async Task CompiledContentServerCanCheckWorkerFromServerThread()
    {
        var paths = Paths();
        using var worker = new MockWorker(paths.Socket);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var workerTask = worker.RunAsync(lifetime.Token);
        UdsAIInferenceClient? client = null;
        try
        {
            using var server = StartServer(paths.Resources);
            await server.WaitIdleAsync();
            Task<AIWorkerHealth>? pending = null;
            await server.WaitPost(() =>
            {
                client = new UdsAIInferenceClient(paths.Socket, new(Enabled: true, TimeoutSeconds: 2));
                pending = client.CheckHealthAsync(lifetime.Token);
            });
            Assert.That(await pending!, Is.EqualTo(AIWorkerHealth.Ready));
            await server.WaitAssertion(() => Assert.That(server.IsAlive, Is.True));
        }
        finally
        {
            client?.Dispose();
            lifetime.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }

    [Test]
    public async Task ServerThreadContinuesWhileWorkerRequestIsPending()
    {
        var paths = Paths();
        using var worker = new MockWorker(paths.Socket, MockWorkerFault.StallHeaders);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var requestCancellation = new CancellationTokenSource();
        var workerTask = worker.RunAsync(lifetime.Token);
        UdsAIInferenceClient? client = null;
        try
        {
            using var server = StartServer(paths.Resources);
            await server.WaitIdleAsync();
            Task<AIInferenceResult>? pending = null;
            await server.WaitPost(() =>
            {
                client = new UdsAIInferenceClient(paths.Socket, new(Enabled: true, TimeoutSeconds: 5));
                pending = client.InferAsync(new(Guid.NewGuid(), "synthetic server-thread spike"), requestCancellation.Token);
            });
            await worker.RequestReceived.WaitAsync(TimeSpan.FromSeconds(3));
            await server.WaitAssertion(() =>
            {
                Assert.That(server.IsAlive, Is.True);
                Assert.That(pending!.IsCompleted, Is.False, "The server must process posted actions while IPC awaits its worker.");
            });
            requestCancellation.Cancel();
            var result = await pending!.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.That(result.Status, Is.EqualTo(AIInferenceStatus.Cancelled));
            Assert.That(result.UntrustedText, Is.Null);
            await server.WaitAssertion(() => Assert.That(server.IsAlive, Is.True));
        }
        finally
        {
            requestCancellation.Cancel();
            client?.Dispose();
            lifetime.Cancel();
            try { await workerTask; }
            catch (OperationCanceledException) { }
        }
    }
}

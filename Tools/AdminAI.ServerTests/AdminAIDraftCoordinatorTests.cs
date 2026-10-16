using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.CMU14.AdminAI;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminAI;
using Content.Shared.CCVar;
using Content.Shared.Guidebook;
using NSubstitute;
using NUnit.Framework;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture, NonParallelizable, Category("LocalIPC")]
public sealed class AdminAIDraftCoordinatorTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task ActualCoordinatorPreparesOnlyForAuthorizedAdminAndDeliversPrivately(bool revokeBeforeCompletion)
    {
        var engineResources = Environment.GetEnvironmentVariable("RUCMU_AI_ENGINE_RESOURCES")!;
        var contentResources = Environment.GetEnvironmentVariable("RUCMU_AI_CONTENT_RESOURCES")!;
        Assert.That(engineResources, Is.Not.Null); Assert.That(contentResources, Is.Not.Null);
        using var server = new RobustIntegrationTest.ServerIntegrationInstance(new()
        {
            ContentStart = false, ContentAssemblies = [],
            Options = new ServerOptions
            {
                LoadConfigAndUserData = false, LoadContentResources = false,
                PrototypeDirectory = new ResPath("/AdminAIEmptyPrototypes"),
                MountOptions = new MountOptions(dirMounts: [engineResources, contentResources], zipMounts: []),
            },
        });
        await server.WaitIdleAsync();
        var service = new ControlledService();
        var session = Substitute.For<ICommonSession>(); var player = new NetUserId(Guid.NewGuid());
        session.UserId.Returns(new NetUserId(Guid.NewGuid())); session.Status.Returns(SessionStatus.InGame);
        var admin = new AdminData { Active = false, Flags = AdminFlags.Adminhelp };
        var responses = new List<AdminAIDraftState>(); var resets = 0;
        AdminAIDraftSystem? coordinator = null; AdminAIDraftState? opened = null;
        try
        {
            await server.WaitAssertion(() =>
            {
                var deps = server.InstanceDependencyCollection.FromParent(server.InstanceDependencyCollection);
                var config = server.ResolveDependency<IConfigurationManager>();
                foreach (var cvar in AdminAICVars.RuntimeBools) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeInts) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeFloats) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeStrings) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                config.RegisterCVar(CCVars.AhelpRateLimitPeriod.Name, 2f, CCVars.AhelpRateLimitPeriod.Flags);
                config.RegisterCVar(CCVars.AhelpRateLimitCount.Name, 100, CCVars.AhelpRateLimitCount.Flags);
                config.SetCVar(AdminAICVars.Enabled, true);
                var admins = Substitute.For<IAdminManager>();
                admins.HasAdminFlag(session, AdminFlags.Adminhelp).Returns(_ => admin.HasFlag(AdminFlags.Adminhelp));
                var players = Substitute.For<IPlayerManager>();
                players.TryGetSessionById(Arg.Any<NetUserId>(), out Arg.Any<ICommonSession?>()).Returns(call =>
                { call[1] = session; return (NetUserId)call[0] == session.UserId; });
                deps.RegisterInstance<IAdminManager>(admins, true); deps.RegisterInstance<IPlayerManager>(players, true);
                deps.RegisterInstance<IAdminAIService>(service); deps.RegisterInstance<IAdminAIRoundContext>(new TestRound());
                deps.RegisterInstance<IAdminLogManager>(Substitute.For<IAdminLogManager>());
                var prototypes = Substitute.For<IPrototypeManager>();
                prototypes.EnumeratePrototypes<GuideEntryPrototype>().Returns(new[]
                {
                    new GuideEntryPrototype { Id = "CMUGuideMedicalInfantry", Name = "Первая помощь пехотинца",
                        Text = new ResPath("/ServerInfo/Guidebook/CMU14/Medical/CMUMedicalInfantry.xml") },
                });
                deps.RegisterInstance<IPrototypeManager>(prototypes);
                deps.BuildGraph();
                var rateLimit = new PlayerRateLimitManager(); deps.InjectDependencies(rateLimit); deps.RegisterInstance<PlayerRateLimitManager>(rateLimit);
                deps.BuildGraph();
                coordinator = new AdminAIDraftSystem(); deps.InjectDependencies(coordinator);
                coordinator.StateDelivery = (target, state) => { Assert.That(target, Is.SameAs(session)); responses.Add(state); };
                coordinator.ResetDelivery = target => { Assert.That(target, Is.SameAs(session)); resets++; };
                coordinator.InitializeCoordinator();
                coordinator.Accepted(new(player.UserId, 0, false, "Как вылечить морпеха?", false));
                coordinator.Accepted(new(player.UserId, 0, true, "Какой вид травмы?", false));
                coordinator.Accepted(new(player.UserId, 0, false, "Я не знаю, я новичок", false));
                coordinator.HandleRequest(new(player, Guid.NewGuid(), Guid.Empty, AdminAIDraftAction.Open), session);
                Assert.That(responses, Is.Empty); Assert.That(resets, Is.EqualTo(1)); Assert.That(service.Calls, Is.Zero);
                admin.Active = true;
                coordinator.HandleRequest(new(new(Guid.NewGuid()), Guid.NewGuid(), Guid.Empty, AdminAIDraftAction.Open), session);
                Assert.That(responses.Last().Status, Is.EqualTo(AdminAIDraftStatus.NoConversation)); Assert.That(service.Calls, Is.Zero);
                coordinator.HandleRequest(new(player, Guid.NewGuid(), Guid.Empty, AdminAIDraftAction.Open), session);
                opened = responses.Last(); Assert.That(opened.ViewId, Is.Not.EqualTo(Guid.Empty));
                coordinator.HandleRequest(new(new(Guid.NewGuid()), opened.SelectionId, opened.ViewId, AdminAIDraftAction.Prepare), session);
                Assert.That(service.Calls, Is.Zero, "Forged channel cannot infer");
            });
            // Drain the real engine task manager while the published document is indexed in the background.
            for (var i = 0; i < 40 && !service.Entered.Task.IsCompleted; i++)
            {
                await Task.Delay(25);
                await server.WaitRunTicks(1);
                await server.WaitPost(() =>
                {
                    coordinator!.HandleRequest(new(player, opened!.SelectionId, opened.ViewId, AdminAIDraftAction.Prepare), session);
                });
            }
            var request = await service.Entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.That(request.Dialogue.Select(turn => turn.FromAdmin), Is.EqualTo(new[] { false, true, false }));
            Assert.That(request.Dialogue[^1].Text, Is.EqualTo("Я не знаю, я новичок"));
            Assert.That(request.Input, Does.Contain("Осмотреть ранения"));
            if (revokeBeforeCompletion) await server.WaitPost(() => admin.Active = false);
            service.Completed.TrySetResult(new(request.CorrelationId, AIInferenceStatus.Success, "[bold]Проверьте правила[/bold] https://evil.example"));
            for (var i = 0; i < 30; i++)
            {
                await Task.Delay(20); await server.WaitRunTicks(1);
                if (responses.Any(state => state.Status == AdminAIDraftStatus.Draft) || resets > 1) break;
            }
            await server.WaitAssertion(() =>
            {
                Assert.That(service.Calls, Is.EqualTo(1));
                if (revokeBeforeCompletion) { Assert.That(responses.Any(state => state.Status == AdminAIDraftStatus.Draft), Is.False); Assert.That(resets, Is.GreaterThan(1)); }
                else
                {
                    var draft = responses.Last(state => state.Status == AdminAIDraftStatus.Draft);
                    Assert.That(draft.Channel, Is.EqualTo(player)); Assert.That(draft.Draft, Does.Not.Contain("https://"));
                    Assert.That(draft.Sources, Is.Not.Empty); Assert.That(draft.Sources.All(source => source.Version.Length == 64), Is.True);
                }
            });
        }
        finally { if (coordinator != null) await server.WaitPost(coordinator.Shutdown); }
    }

    private sealed class ControlledService : IAdminAIService
    {
        public int Calls;
        public readonly TaskCompletionSource<AIInferenceRequest> Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<AIInferenceResult> Completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Initialize() { }
        public void ObservePhase(Robust.Shared.ContentPack.ModUpdateLevel level) { }
        public void Shutdown() { }
        public AdminAISnapshot Snapshot() => new(true, AIResourceState.Normal, AIWorkerHealth.Ready, "test", null, null);
        public Task<AIInferenceResult> EnqueueAsync(AIInferenceRequest request, AIRequestClass kind, CancellationToken token)
        { Interlocked.Increment(ref Calls); Entered.TrySetResult(request); return Completed.Task.WaitAsync(token); }
    }
    private sealed class TestRound : IAdminAIRoundContext { public int CurrentRoundId => 0; }
}

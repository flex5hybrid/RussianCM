using System.Collections.Concurrent;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.CMU14.AdminAI;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminAI;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Guidebook;
using NSubstitute;
using NUnit.Framework;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture, NonParallelizable, Category("LocalIPC")]
public sealed class AdminAICaseCoordinatorTests
{
    [TestCase("success")]
    [TestCase("no-logs")]
    [TestCase("no-adminhelp")]
    [TestCase("wrong-round")]
    [TestCase("wrong-view")]
    [TestCase("wide-window")]
    [TestCase("null-participants")]
    [TestCase("revoked")]
    [TestCase("new-input")]
    [TestCase("evicted")]
    [TestCase("pressure")]
    [TestCase("timeout")]
    public async Task CompiledEvidenceHandlerBoundsDatabaseAndRechecksPrivateDelivery(string scenario)
    {
        var engine = Environment.GetEnvironmentVariable("RUCMU_AI_ENGINE_RESOURCES")!;
        var content = Environment.GetEnvironmentVariable("RUCMU_AI_CONTENT_RESOURCES")!;
        using var server = new RobustIntegrationTest.ServerIntegrationInstance(new()
        {
            ContentStart = false, ContentAssemblies = [],
            Options = new ServerOptions { LoadConfigAndUserData = false, LoadContentResources = false,
                PrototypeDirectory = new ResPath("/AdminAIEmptyPrototypes"),
                MountOptions = new MountOptions(dirMounts: [engine, content], zipMounts: []) },
        });
        await server.WaitIdleAsync();
        var session = Substitute.For<ICommonSession>(); session.UserId.Returns(new NetUserId(Guid.NewGuid()));
        session.Status.Returns(SessionStatus.InGame);
        var channel = new NetUserId(Guid.NewGuid());
        var flags = AdminFlags.Adminhelp | AdminFlags.Logs;
        var logs = Substitute.For<IAdminLogManager>();
        var entered = new TaskCompletionSource<LogFilter>(TaskCreationOptions.RunContinuationsAsynchronously);
        var rows = new TaskCompletionSource<List<SharedAdminLog>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        logs.All(Arg.Any<LogFilter>()).Returns(call => { Interlocked.Increment(ref calls); entered.TrySetResult(call.Arg<LogFilter>()); return rows.Task; });
        var callbacks = new ConcurrentQueue<Action>(); var states = new List<AdminAIDraftState>(); var reports = new List<AdminAICaseState>(); var resets = 0;
        var service = new NoModel(); AdminAIDraftSystem? coordinator = null; AdminAIDraftState? opened = null;
        try
        {
            await server.WaitAssertion(() =>
            {
                var config = server.ResolveDependency<IConfigurationManager>();
                foreach (var cvar in AdminAICVars.RuntimeBools) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeInts) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeFloats) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                foreach (var cvar in AdminAICVars.RuntimeStrings) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                config.RegisterCVar(CCVars.AhelpRateLimitPeriod.Name, 2f, CCVars.AhelpRateLimitPeriod.Flags);
                config.RegisterCVar(CCVars.AhelpRateLimitCount.Name, 100, CCVars.AhelpRateLimitCount.Flags);
                config.SetCVar(AdminAICVars.Enabled, true); config.SetCVar(AdminAICVars.Investigation, true);
                if (scenario == "evicted") config.SetCVar(AdminAICVars.MaxConversations, 1);
                var deps = server.InstanceDependencyCollection.FromParent(server.InstanceDependencyCollection);
                var admins = Substitute.For<IAdminManager>();
                admins.HasAdminFlag(session, Arg.Any<AdminFlags>()).Returns(call => (flags & call.Arg<AdminFlags>()) == call.Arg<AdminFlags>());
                var players = Substitute.For<IPlayerManager>();
                players.TryGetSessionById(Arg.Any<NetUserId>(), out Arg.Any<ICommonSession?>()).Returns(call =>
                { call[1] = session; return (NetUserId)call[0] == session.UserId; });
                var tasks = Substitute.For<ITaskManager>();
                tasks.When(manager => manager.RunOnMainThread(Arg.Any<Action>())).Do(call => callbacks.Enqueue(call.Arg<Action>()));
                var prototypes = Substitute.For<IPrototypeManager>();
                prototypes.EnumeratePrototypes<GuideEntryPrototype>().Returns(new[] { new GuideEntryPrototype {
                    Id = "AU14RulesGeneral", Name = "Rules", Text = new ResPath("/ServerInfo/Guidebook/CMU14/Rules/AU14GeneralRules.xml") } });
                deps.RegisterInstance<IAdminManager>(admins, true); deps.RegisterInstance<IPlayerManager>(players, true);
                deps.RegisterInstance<ITaskManager>(tasks, true); deps.RegisterInstance<IPrototypeManager>(prototypes, true);
                deps.RegisterInstance<IAdminLogManager>(logs); deps.RegisterInstance<IAdminAIService>(service);
                deps.RegisterInstance<IAdminAIRoundContext>(new Round()); deps.BuildGraph();
                var rate = new PlayerRateLimitManager(); deps.InjectDependencies(rate); deps.RegisterInstance<PlayerRateLimitManager>(rate); deps.BuildGraph();
                coordinator = new AdminAIDraftSystem(); deps.InjectDependencies(coordinator);
                coordinator.StateDelivery = (target, state) => { Assert.That(target, Is.SameAs(session)); states.Add(state); };
                coordinator.ResetDelivery = _ => resets++;
                coordinator.CaseDelivery = (target, state) => { Assert.That(target, Is.SameAs(session)); reports.Add(state); };
                coordinator.InitializeCoordinator(); coordinator.Accepted(new(channel.UserId, 0, false, "Меня убили", false));
                coordinator.HandleRequest(new(channel, Guid.NewGuid(), Guid.Empty, AdminAIDraftAction.Open), session);
                opened = states.Last(); Assert.That(opened.CanInvestigate, Is.True);
            });
            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(10); var ready = false;
                await server.WaitPost(() => { while (callbacks.TryDequeue(out var cb)) cb(); ready = coordinator!.GetKnowledge() != null; });
                if (ready) break;
            }
            await server.WaitPost(() =>
            {
                if (scenario == "no-logs") flags = AdminFlags.Adminhelp;
                if (scenario == "no-adminhelp") flags = AdminFlags.Logs;
                coordinator!.HandleContext(new(channel, opened!.SelectionId,
                    scenario == "wrong-view" ? Guid.NewGuid() : opened.ViewId,
                    scenario == "wrong-round" ? 1 : 0, scenario == "wide-window" ? 30 : 5, 2,
                    scenario == "null-participants" ? null! : []), session);
            });
            if (scenario is "no-logs" or "no-adminhelp" or "wrong-round" or "wrong-view" or "wide-window" or "null-participants")
            {
                await Task.Delay(100);
                Assert.That(calls, Is.Zero);
                if (scenario == "wide-window") Assert.That(reports.Single().Status, Is.EqualTo("invalid"));
                else Assert.That(reports, Is.Empty);
                return;
            }
            var filter = await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.That(filter.Round, Is.EqualTo(0)); Assert.That(filter.Limit, Is.EqualTo(65));
            Assert.That(filter.AnyPlayers, Is.EqualTo(new[] { channel.UserId })); Assert.That(filter.IncludeNonPlayers, Is.False);
            Assert.That(filter.Before - filter.After, Is.EqualTo(TimeSpan.FromMinutes(7)));
            if (scenario == "timeout")
            {
                await Task.Delay(5200);
                await server.WaitPost(() => { while (callbacks.TryDequeue(out var cb)) cb(); });
                Assert.That(reports.Last().Status, Is.EqualTo("expired"));
                await server.WaitPost(() => coordinator!.HandleContext(new(channel, opened!.SelectionId, opened.ViewId, 0, 5, 2, []), session));
                Assert.That(calls, Is.EqualTo(1), "Cancellation-ignoring backend must retain the sole physical database slot.");
            }
            await server.WaitPost(() =>
            {
                if (scenario == "revoked") flags = AdminFlags.Adminhelp;
                if (scenario == "new-input") coordinator!.Accepted(new(channel.UserId, 0, false, "Новое обращение", false));
                if (scenario == "evicted") coordinator!.Accepted(new(Guid.NewGuid(), 0, false, "Другое обращение", false));
                if (scenario == "pressure") service.Paused = true;
            });
            rows.TrySetResult([new(42, LogType.Chat, LogImpact.Low, DateTime.UtcNow, "actual log row", [channel.UserId])]);
            for (var i = 0; i < 50; i++)
            {
                await Task.Delay(10);
                await server.WaitPost(() => { while (callbacks.TryDequeue(out var cb)) cb(); });
                if (i > 5 && callbacks.IsEmpty) break;
            }
            await server.WaitAssertion(() =>
            {
                var delivered = reports.Where(report => report.Status == "ready").ToArray();
                if (scenario == "success")
                { Assert.That(delivered, Has.Length.EqualTo(1)); Assert.That(delivered[0].Evidence.Single().LogId, Is.EqualTo(42)); }
                else Assert.That(delivered, Is.Empty, "Late private evidence must not survive revoked permission, stale input, pressure or timeout.");
                if (scenario == "revoked") Assert.That(resets, Is.GreaterThan(0), "Revoked privileges clear all private client state.");
                if (scenario == "evicted") Assert.That(states.Any(state => state.Status == AdminAIDraftStatus.Stale
                    && state.Channel == channel && state.ViewId == Guid.Empty && state.SelectionId == opened!.SelectionId), Is.True,
                    "Eviction must clear only the matching old client selection, not a freshly opened channel.");
                Assert.That(calls, Is.EqualTo(1));
            });
        }
        finally { rows.TrySetResult([]); if (coordinator != null) await server.WaitPost(coordinator.Shutdown); }
    }
    private sealed class Round : IAdminAIRoundContext { public int CurrentRoundId => 0; }
    private sealed class NoModel : IAdminAIService
    {
        public bool Paused;
        public void Initialize() { } public void Shutdown() { } public void ObservePhase(ModUpdateLevel level) { }
        public AdminAISnapshot Snapshot() => new(true, Paused ? AIResourceState.Paused : AIResourceState.Normal, AIWorkerHealth.Ready, "test", null, null);
        public Task<AIInferenceResult> EnqueueAsync(AIInferenceRequest request, AIRequestClass kind, CancellationToken token)
            => throw new InvalidOperationException("Evidence report must use authoritative rows, not generated evidence.");
    }
}

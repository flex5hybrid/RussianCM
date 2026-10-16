using System.Collections.Concurrent;
using Content.Server.Administration.Managers;
using Content.Server.CMU14.AdminAI;
using Content.Shared.GameTicking;
using NSubstitute;
using NUnit.Framework;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture, NonParallelizable, Category("LocalIPC")]
public sealed class AdminAISafeFaqCoordinatorTests
{
    [TestCase("normal")]
    [TestCase("map")]
    [TestCase("disabled")]
    [TestCase("admin-present")]
    [TestCase("admin-arrives")]
    [TestCase("new-input")]
    [TestCase("reconnected")]
    [TestCase("config-disabled")]
    [TestCase("paused")]
    [TestCase("worker-down")]
    [TestCase("dispute")]
    [TestCase("no-source")]
    [TestCase("admin-only")]
    [TestCase("clarification")]
    [TestCase("clarification-admin-arrives")]
    public async Task CompiledHandlerOnlyDeliversEligiblePublicFaqWithoutCallingModel(string scenario)
    {
        var engine = Environment.GetEnvironmentVariable("RUCMU_AI_ENGINE_RESOURCES")!;
        var content = Environment.GetEnvironmentVariable("RUCMU_AI_CONTENT_RESOURCES")!;
        Assert.That(engine, Is.Not.Null); Assert.That(content, Is.Not.Null);
        using var server = new RobustIntegrationTest.ServerIntegrationInstance(new()
        {
            ContentStart = false, ContentAssemblies = [],
            Options = new ServerOptions
            {
                LoadConfigAndUserData = false, LoadContentResources = false,
                PrototypeDirectory = new ResPath("/AdminAIEmptyPrototypes"),
                MountOptions = new MountOptions(dirMounts: [engine, content], zipMounts: []),
            },
        });
        await server.WaitIdleAsync();
        var session = Substitute.For<ICommonSession>(); var replacement = Substitute.For<ICommonSession>();
        session.UserId.Returns(new NetUserId(Guid.NewGuid())); session.Status.Returns(SessionStatus.InGame);
        replacement.UserId.Returns(session.UserId); replacement.Status.Returns(SessionStatus.InGame);
        var current = session; var adminPresent = scenario == "admin-present";
        var callbacks = new ConcurrentQueue<Action>(); var replies = new List<string>();
        var service = new NoInferenceService(scenario);
        var knowledge = new AIRuleKnowledgeBase([new("AU14CommsHeadsets", "Radio", "/ServerInfo/Guidebook/CMU14/Comms/AU14CommsHeadsets.xml",
            File.ReadAllBytes(Path.Combine(content, "ServerInfo/Guidebook/CMU14/Comms/AU14CommsHeadsets.xml")))]);
        AdminAISafeFaqSystem? coordinator = null;
        IConfigurationManager? config = null;
        try
        {
            await server.WaitAssertion(() =>
            {
                var loc = server.ResolveDependency<ILocalizationManager>();
                var culture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU", predefinedOnly: false);
                loc.LoadCulture(culture); loc.DefaultCulture = culture;
                var deps = server.InstanceDependencyCollection.FromParent(server.InstanceDependencyCollection);
                config = server.ResolveDependency<IConfigurationManager>();
                foreach (var cvar in AdminAICVars.RuntimeBools) config.RegisterCVar(cvar.Name, cvar.DefaultValue, cvar.Flags);
                config.SetCVar(AdminAICVars.Enabled, true);
                config.SetCVar(AdminAICVars.SafeFAQ, scenario != "disabled");
                config.SetCVar(AdminAICVars.Clarification, scenario.StartsWith("clarification"));
                var admins = Substitute.For<IAdminManager>();
                admins.AllAdmins.Returns(_ => adminPresent ? new[] { session } : Array.Empty<ICommonSession>());
                var players = Substitute.For<IPlayerManager>();
                players.TryGetSessionById(Arg.Any<NetUserId>(), out Arg.Any<ICommonSession?>()).Returns(call =>
                { call[1] = current; return (NetUserId)call[0] == session.UserId; });
                var tasks = Substitute.For<ITaskManager>();
                tasks.When(manager => manager.RunOnMainThread(Arg.Any<Action>())).Do(call => callbacks.Enqueue(call.Arg<Action>()));
                deps.RegisterInstance<IAdminManager>(admins, true); deps.RegisterInstance<IPlayerManager>(players, true);
                deps.RegisterInstance<ITaskManager>(tasks, true); deps.RegisterInstance<IAdminAIService>(service);
                deps.RegisterInstance<IPrototypeManager>(Substitute.For<IPrototypeManager>(), true);
                deps.RegisterInstance<IAdminAIRoundContext>(new PublicRound()); deps.BuildGraph();
                coordinator = new AdminAISafeFaqSystem(); deps.InjectDependencies(coordinator);
                coordinator.KnowledgeLookup = () => knowledge;
                coordinator.CaseLookup = _ => Guid.Parse("8ce00000-0000-0000-0000-000000000007");
                coordinator.ReplyDelivery = (target, text) => { Assert.That(target, Is.SameAs(session)); replies.Add(text); };
                coordinator.InitializeCoordinator();
                var question = scenario.StartsWith("clarification") ? "Меня убили" : scenario == "map" ? "На какой карте мы сейчас играем?" : scenario == "dispute"
                    ? "Как пользоваться радио? Меня убили" : scenario == "no-source" ? "Как зарядить квантовый вомбат?" : "Как пользоваться радио?";
                coordinator.Accepted(new(session.UserId.UserId, 7, false, question, scenario == "admin-only"));
            });
            // Hold actual completion callbacks while trusted server state changes.
            for (var index = 0; index < 40 && callbacks.IsEmpty; index++) await Task.Delay(10);
            await server.WaitAssertion(() =>
            {
                if (scenario is "admin-arrives" or "clarification-admin-arrives") adminPresent = true;
                if (scenario == "reconnected") current = replacement;
                if (scenario == "config-disabled") config!.SetCVar(AdminAICVars.SafeFAQ, false);
                if (scenario == "new-input") coordinator!.Accepted(new(session.UserId.UserId, 7, false, "Новое обращение", false));
                while (callbacks.TryDequeue(out var callback)) callback();
                if (scenario is "normal" or "map" or "clarification")
                {
                    Assert.That(replies, Has.Count.EqualTo(1));
                    Assert.That(replies[0], scenario == "map" ? Does.Contain("LV-624") : scenario == "clarification" ? Does.Contain("место") : Does.Contain(":h"));
                }
                else Assert.That(replies, Is.Empty, scenario);
                Assert.That(service.Calls, Is.Zero, "Automatic replies must never publish unchecked model output.");
            });
        }
        finally { if (coordinator != null) await server.WaitPost(coordinator.Shutdown); }
    }

    private sealed class PublicRound : IAdminAIRoundContext
    {
        public int CurrentRoundId => 7;
        public string PublicMapDescription => "Проверочный публичный контекст: LV-624.";
    }
    private sealed class NoInferenceService(string scenario) : IAdminAIService
    {
        public int Calls;
        public void Initialize() { }
        public void ObservePhase(ModUpdateLevel level) { }
        public void Shutdown() { }
        public AdminAISnapshot Snapshot() => new(true, scenario == "paused" ? AIResourceState.Paused : AIResourceState.Normal,
            scenario == "worker-down" ? AIWorkerHealth.Unavailable : AIWorkerHealth.Ready, "test", null, null);
        public Task<AIInferenceResult> EnqueueAsync(AIInferenceRequest request, AIRequestClass kind, CancellationToken token)
        { Calls++; throw new InvalidOperationException("Automatic FAQ must not infer unchecked text."); }
    }
}

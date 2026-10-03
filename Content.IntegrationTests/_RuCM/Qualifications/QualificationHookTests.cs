using System;
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Qualifications;
using Content.Server.GameTicking.Events;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Station.Events;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.IntegrationTests._RuCM.Qualifications;

public sealed class QualificationHookTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task TypedWireContractsRoundtripPrivateHistoryAndManagementPayloads()
    {
        var target = Guid.NewGuid(); var actor = Guid.NewGuid();
        var at = DateTimeOffset.Parse("2026-10-03T12:34:56+03:00");
        var context = new TrainingContext(actor, "instructor", "AU14JobGOVFORPlatCo", 42, "test", at);
        var view = new QualificationView { Target = target, Viewer = actor,
            Preview = new() { Counts = new() { ["medical"] = 2 }, Records = 2 } };
        view.Store.Players[target] = new() { Player = target, FirstTrainingAt = at,
            Grants = new() { ["medical"] = new("medical", QualificationStatus.Active, 3, new[] { "practice" }, context, "test") },
            Progress = new() { ["medical"] = new() { ["practice"] = new("medical", "practice", context, "note") } } };
        view.Store.Notes.Add(new(Guid.NewGuid(), target, context, "private note"));
        view.Store.Suspensions.Add(new() { Target = target, Initiator = context, ResolvedAt = at });
        byte[] bytes = null;
        await Server.WaitAssertion(() =>
        {
            using var stream = new System.IO.MemoryStream();
            Server.ResolveDependency<IRobustSerializer>().Serialize(stream, new QualificationBoundView(view));
            bytes = stream.ToArray();
        });
        await Client.WaitAssertion(() =>
        {
            using var stream = new System.IO.MemoryStream(bytes);
            var decoded = Client.ResolveDependency<IRobustSerializer>().Deserialize<QualificationBoundView>(stream).View;
            Assert.That(decoded.Store.Players[target].FirstTrainingAt, Is.EqualTo(at));
            Assert.That(decoded.Store.Notes.Single().Author.At.Offset, Is.EqualTo(at.Offset));
            Assert.That(decoded.Store.Players[target].Grants["medical"].RequiredItems, Is.EqualTo(new[] { "practice" }));
            Assert.That(decoded.Store.Players[target].Progress["medical"]["practice"].By.Actor, Is.EqualTo(actor));
            Assert.That(decoded.Store.Suspensions.Single().ResolvedAt, Is.EqualTo(at));
            Assert.That(decoded.Preview.Counts["medical"], Is.EqualTo(2));
            using var output = new System.IO.MemoryStream();
            var request = new QualificationRequest { Target = target, Configuration = new()
            {
                Role = new() { JobId = "AU14JobGOVFORSquadRifleman", MinimumLevel = MilitaryLevel.Enlisted },
                Instructor = new(true, true, false, new() { "medical" }, actor, at, actor, at),
                MigrationGroups = new() { ["medical"] = new() { "job" } }, TrackerAliases = new() { ["old"] = "new" }
            } };
            Client.ResolveDependency<IRobustSerializer>().Serialize(output, new QualificationEuiRequest(QualificationAction.SaveRole, request));
            bytes = output.ToArray();
        });
        await Server.WaitAssertion(() =>
        {
            using var stream = new System.IO.MemoryStream(bytes);
            var request = Server.ResolveDependency<IRobustSerializer>().Deserialize<QualificationEuiRequest>(stream).Request;
            Assert.That(request.Configuration.Role.MinimumLevel, Is.EqualTo(MilitaryLevel.Enlisted));
            Assert.That(request.Configuration.Instructor.Professional, Does.Contain("medical"));
            Assert.That(request.Configuration.TrackerAliases["old"], Is.EqualTo("new"));
        });
    }

    [Test]
    public async Task ActualPublicEventsBlockRoundstartAndExplicitLatejoinAndDisabledIsTransparent()
    {
        const string job = "AU14JobGOVFORSquadRifleman";
        await Server.WaitAssertion(() =>
        {
            var entityManager = Server.ResolveDependency<IEntityManager>();
            var system = entityManager.System<QualificationSystem>();
            var seed = new QualificationStore();
            seed.Definitions["enlisted"] = new() { Id = "enlisted" };
            seed.Roles[job] = new() { JobId = job, MinimumLevel = MilitaryLevel.Enlisted };
            system.ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(5);
        await Server.WaitAssertion(() =>
        {
            var ent = Server.ResolveDependency<IEntityManager>(); var cfg = Server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(QualificationCVars.Enabled, true); cfg.SetCVar(QualificationCVars.Enforce, true);
            var player = ServerSession;
            Assert.That(player, Is.Not.Null);
            var candidates = new StationJobsGetCandidatesEvent(player.UserId, new List<ProtoId<JobPrototype>> { job });
            ent.EventBus.RaiseEvent(EventSource.Local, ref candidates);
            Assert.That(candidates.Jobs, Is.Empty);
            var disallowed = new GetDisallowedJobsEvent(player, new()); ent.EventBus.RaiseEvent(EventSource.Local, ref disallowed);
            Assert.That(disallowed.Jobs, Does.Contain(new ProtoId<JobPrototype>(job)));
            var allowed = new IsRoleAllowedEvent(player, new() { job }, null); ent.EventBus.RaiseEvent(EventSource.Local, ref allowed);
            Assert.That(allowed.Cancelled, Is.True);
            foreach (var late in new[] { false, true })
            {
                var spawn = new PlayerBeforeSpawnEvent(player, HumanoidCharacterProfile.DefaultWithSpecies(), job, late, EntityUid.Invalid);
                ent.EventBus.RaiseEvent(EventSource.Local, spawn); Assert.That(spawn.JobId, Is.Null);
            }
            cfg.SetCVar(QualificationCVars.Enabled, false);
            var unchanged = new StationJobsGetCandidatesEvent(player.UserId, new List<ProtoId<JobPrototype>> { job }); ent.EventBus.RaiseEvent(EventSource.Local, ref unchanged);
            Assert.That(unchanged.Jobs, Does.Contain(new ProtoId<JobPrototype>(job)));
            var noGate = new PlayerBeforeSpawnEvent(player, HumanoidCharacterProfile.DefaultWithSpecies(), job, true, EntityUid.Invalid);
            ent.EventBus.RaiseEvent(EventSource.Local, noGate); Assert.That(noGate.JobId, Is.EqualTo(job));
        });
    }

    [Test]
    public async Task ForgedEuiRequestsCannotGrantSelfOfficerOrConfigureRoles()
    {
        await Server.WaitAssertion(() =>
        {
            var system = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>();
            var admins = Server.ResolveDependency<IAdminManager>();
            if (admins.GetAdminData(ServerSession) != null) admins.DeAdmin(ServerSession);
            var seed = new QualificationStore(); seed.Definitions["officer"] = new() { Id = "officer" };
            system.ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(5);
        foreach (var action in new[] { QualificationAction.Grant, QualificationAction.Complete, QualificationAction.Restore, QualificationAction.SaveInstructor, QualificationAction.SaveRole })
        {
            await Task.Delay(250);
            string result = null;
            await Server.WaitAssertion(() =>
            {
                var system = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>();
                var request = new QualificationRequest { Target = ServerSession.UserId, Qualification = "officer", Item = "test", Payload = "{}", Reason = "forged" };
                system.Submit(ServerSession, action, System.Text.Json.JsonSerializer.Serialize(request), error => result = error);
            });
            await Pair.RunTicksSync(30);
            await Server.WaitAssertion(() =>
            {
                Assert.That(result, Is.EqualTo("permission"));
                Assert.That(Server.ResolveDependency<IEntityManager>().System<QualificationSystem>().Service.Snapshot().Audit, Is.Empty);
            });
        }
    }

    [Test]
    public async Task ForgedBuiMessagesAreRejectedAndPrivateDataIsNotReplicatedInComponentStates()
    {
        var map = await Pair.CreateTestMap();
        EntityUid actor = default;
        NetEntity netActor = default;
        await Server.WaitAssertion(() =>
        {
            var ent = Server.ResolveDependency<IEntityManager>(); var system = ent.System<QualificationSystem>();
            var admins = Server.ResolveDependency<IAdminManager>();
            if (admins.GetAdminData(ServerSession) != null) admins.DeAdmin(ServerSession);
            var seed = new QualificationStore(); seed.Definitions["officer"] = new() { Id = "officer" };
            system.ConfigureRepository(new MemoryQualificationRepository(), seed);
            actor = ent.SpawnEntity("CMMobHuman", map.GridCoords);
            Server.PlayerMan.SetAttachedEntity(ServerSession, actor); netActor = ent.GetNetEntity(actor);
        });
        await Pair.RunTicksSync(5);
        await Server.WaitPost(() => Server.ResolveDependency<IEntityManager>().System<QualificationSystem>().Open(ServerSession, true));
        await RunUntilSynced();
        foreach (var action in new[] { QualificationAction.Grant, QualificationAction.Complete, QualificationAction.Restore, QualificationAction.SaveInstructor, QualificationAction.SaveRole })
        {
            await Task.Delay(250);
            var request = new QualificationRequest { Target = ServerSession.UserId, Qualification = "officer", Item = "test", Payload = "{}", Reason = "forged" };
            await Client.WaitPost(() => Client.EntMan.System<SharedUserInterfaceSystem>().ClientSendUiMessage(Client.EntMan.GetEntity(netActor), QualificationUiKey.Main,
                new QualificationBoundRequest(action, request)));
            await RunUntilSynced();
            await Server.WaitAssertion(() =>
            {
                var ent = Server.ResolveDependency<IEntityManager>();
                Assert.That(ent.System<QualificationSystem>().Service.Snapshot().Audit, Is.Empty);
                Assert.That(ent.GetComponent<UserInterfaceComponent>(actor).States, Does.Not.ContainKey(QualificationUiKey.Main));
            });
        }
        await Server.WaitAssertion(() =>
        {
            var ent = Server.ResolveDependency<IEntityManager>(); var other = ent.SpawnEntity("CMMobHuman", map.GridCoords);
            var attempt = new BoundUserInterfaceMessageAttempt(other, actor, QualificationUiKey.Main, new OpenBoundInterfaceMessage());
            ent.EventBus.RaiseLocalEvent(actor, attempt); Assert.That(attempt.Cancelled, Is.True);
        });
    }

    [Test]
    public async Task ManagementEuiEditsConfigurationAndRevokedAclImmediatelyRedactsPrivateView()
    {
        const string job = "AU14JobGOVFORSquadRifleman";
        Content.Server._RuCM.Qualifications.QualificationEui eui = null;
        var selected = Guid.NewGuid(); var unrelated = Guid.NewGuid();
        await Server.WaitAssertion(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>(); if (admins.GetAdminData(ServerSession) != null) admins.DeAdmin(ServerSession);
            var system = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>();
            var seed = new QualificationStore();
            foreach (var id in QualificationRules.Levels) seed.Definitions[id] = new() { Id = id, Name = "rucm-qualifications-definition-" + id };
            seed.Management.Add(ServerSession.UserId); seed.Roles[job] = new() { JobId = job, Enabled = false };
            seed.Players[selected] = new() { Player = selected };
            seed.Players[unrelated] = new() { Player = unrelated };
            var context = new TrainingContext(ServerSession.UserId, "instructor", "", 1, "test", DateTimeOffset.UtcNow);
            seed.Notes.Add(new(Guid.NewGuid(), selected, context, "selected note"));
            seed.Notes.Add(new(Guid.NewGuid(), unrelated, context, "unrelated private note"));
            system.ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(5);
        await Server.WaitPost(() =>
        {
            var system = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>();
            eui = new(system); Server.ResolveDependency<EuiManager>().OpenEui(eui, ServerSession);
        });
        await RunUntilSynced();
        await Task.Delay(250);
        await Server.WaitPost(() =>
        {
            var system = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>();
            var req = new QualificationRequest { Revision = system.Service.Snapshot().Revision, Reason = "test role requirement change",
                Configuration = new() { Role = new RoleRequirement { JobId = job, MinimumLevel = MilitaryLevel.Enlisted } } };
            eui.HandleMessage(new QualificationEuiRequest(QualificationAction.SaveRole, req));
        });
        await Pair.RunTicksSync(10); await RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            var system = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>();
            Assert.That(system.Service.CanTakeJob(ServerSession.UserId, job).Allowed, Is.False);
            Assert.That(system.Service.Snapshot().Audit.Last().Action, Is.EqualTo("SaveRole"));
            var view = system.View(ServerSession, selected);
            Assert.That(view.Store.Players.Keys, Is.EquivalentTo(new[] { selected }));
            Assert.That(view.Store.Notes.Select(n => n.Target), Is.EquivalentTo(new[] { selected }));
            Assert.That(system.Service.Snapshot().Players.Count, Is.EqualTo(2), "View redaction must not mutate cache");
        });
        await Task.Delay(250);
        await Server.WaitPost(() =>
        {
            var system = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>();
            var req = new QualificationRequest { Revision = system.Service.Snapshot().Revision, Reason = "remove management ACL", Configuration = new() { Management = new() } };
            eui.HandleMessage(new QualificationEuiRequest(QualificationAction.SaveManagement, req));
        });
        await Pair.RunTicksSync(10); await RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            var view = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>().View(ServerSession, Guid.NewGuid());
            Assert.That(view.Management, Is.False); Assert.That(view.Target, Is.EqualTo((Guid) ServerSession.UserId));
            Assert.That(view.Store.Audit, Is.Empty); Assert.That(view.Store.Management, Is.Empty);
            eui.Close();
        });
        await RunUntilSynced();
    }
}

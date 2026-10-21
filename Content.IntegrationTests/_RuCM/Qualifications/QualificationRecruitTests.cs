using System;
using System.Linq;
using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Qualifications;
using Content.Server.Antag;
using Content.Shared.Antag;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Database;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Network;

namespace Content.IntegrationTests._RuCM.Qualifications;

public sealed class QualificationRecruitTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Destructive = true };

    private async Task PrepareInstructor()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>();
            if (admins.GetAdminData(ServerSession) != null) admins.DeAdmin(ServerSession);
            var entities = Server.ResolveDependency<IEntityManager>();
            var seed = new QualificationStore();
            seed.Definitions["enlisted"] = new() { Id = "enlisted", Name = "rucm-qualifications-definition-enlisted",
                Items = new() { new() { Id = "practice", Name = "practice" } } };
            var now = DateTimeOffset.UtcNow;
            seed.Instructors[ServerSession.UserId] = new(true, true, false, new(), ServerSession.UserId, now, ServerSession.UserId, now);
            entities.System<QualificationSystem>().ConfigureRepository(new MemoryQualificationRepository(), seed);
            // A minimal round fixture: actual mind/session authority without loading an unrelated station map.
            typeof(GameTicker).GetField("_runLevel", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(entities.System<GameTicker>(), GameRunLevel.InRound);
            var body = entities.SpawnEntity("CMMobHuman", map.GridCoords);
            var minds = entities.System<SharedMindSystem>();
            var mind = minds.GetOrCreateMind(ServerSession.UserId);
            minds.TransferTo(mind, body);
            Server.PlayerMan.SetAttachedEntity(ServerSession, body);
        });
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() => Assert.That(Server.System<QualificationSystem>().Authority(ServerSession).CurrentParticipant, Is.True));
    }

    [Test]
    public async Task CurrentRecruitJobRejectsMidRoundAntagonistSelection()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var entities = Server.EntMan;
            var body = entities.SpawnEntity("CMMobHuman", map.GridCoords);
            var minds = entities.System<SharedMindSystem>();
            var mind = minds.GetOrCreateMind(ServerSession.UserId);
            minds.TransferTo(mind, body);
            entities.System<SharedRoleSystem>().MindAddJobRole(mind, jobPrototype: GOVFORRecruitJob.Id);
            Server.PlayerMan.SetAttachedEntity(ServerSession, body);

            var prototypes = Server.ResolveDependency<IPrototypeManager>();
            Assert.That(prototypes.Index<JobPrototype>(GOVFORRecruitJob.Id).CanBeAntag, Is.False);
            var antag = prototypes.Index<AntagSpecifierPrototype>("Thief");
            var method = typeof(AntagSelectionSystem).GetMethod("IsMindValid",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.Invoke(entities.System<AntagSelectionSystem>(), new object?[] { mind, antag }), Is.False,
                "Mid-round antag selection must reject the Recruit job regardless of antagonist job filters");
        });
    }

    [Test]
    public async Task InstructorEuiSelectsAnotherOnlineAccountWithoutCharacterAndTrainsOnlyThatRecruit()
    {
        await PrepareInstructor();
        var recruit = await Server.AddDummySession("RecruitNick");
        Content.Server._RuCM.Qualifications.QualificationEui eui = null;
        await Pair.RunTicksSync(5);
        await Server.WaitAssertion(() =>
        {
            var system = Server.System<QualificationSystem>();
            var view = system.View(ServerSession, ServerSession.UserId);
            Assert.That(view.OnlinePlayers[recruit.UserId], Is.EqualTo("RecruitNick"));
            Assert.That(recruit.AttachedEntity, Is.Null, "Lobby accounts must be selectable without a spawned character");
            eui = new(system); Server.ResolveDependency<EuiManager>().OpenEui(eui, ServerSession);
            eui.HandleMessage(new QualificationEuiRequest(QualificationAction.View, new QualificationRequest { Target = recruit.UserId }));
        });
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() =>
        {
            var view = ((QualificationEuiState) eui.GetNewState()).View;
            Assert.That(view.Target, Is.EqualTo((Guid) recruit.UserId));
            Assert.That(view.AccountNames[recruit.UserId], Is.EqualTo("RecruitNick"));
            Assert.That(view.TargetOnline, Is.True);
        });
        foreach (var action in new[] { QualificationAction.Complete, QualificationAction.Certify, QualificationAction.Note })
        {
            await Task.Delay(250);
            await Server.WaitPost(() => eui.HandleMessage(new QualificationEuiRequest(action, new()
            { Target = recruit.UserId, Qualification = "enlisted", Item = "practice", Reason = "Skills checked in person",
                Revision = Server.System<QualificationSystem>().Service.Snapshot().Revision })));
            await Pair.RunTicksSync(10);
            await Server.WaitAssertion(() => Assert.That(((QualificationEuiState) eui.GetNewState()).View.Error, Is.Empty));
        }
        await Server.WaitAssertion(() =>
        {
            var snapshot = Server.System<QualificationSystem>().Service.Snapshot();
            Assert.That(snapshot.Players[recruit.UserId].Grants["enlisted"].Status, Is.EqualTo(QualificationStatus.Active));
            Assert.That(snapshot.Notes.Single().Target, Is.EqualTo((Guid) recruit.UserId));
            Assert.That(snapshot.Players.ContainsKey(ServerSession.UserId), Is.False, "Never mutate the instructor's own record");
            eui.Close();
        });
    }

    [TestCase(QualificationAction.Complete)]
    [TestCase(QualificationAction.Certify)]
    [TestCase(QualificationAction.Note)]
    public async Task RecruitDisconnectingAfterSubmissionIsRejectedBeforeMutation(QualificationAction action)
    {
        await PrepareInstructor();
        var recruit = await Server.AddDummySession("LeavingRecruit");
        await Pair.RunTicksSync(5);
        string error = null;
        await Server.WaitPost(() =>
        {
            var system = Server.System<QualificationSystem>();
            system.Submit(ServerSession, action, new QualificationRequest { Target = recruit.UserId, Qualification = "enlisted", Item = "practice", Reason = "forged stale window" }, result => error = result);
            // Disconnect after queueing; leave the stale session registered to exercise the status check too.
            recruit.GetType().GetMethod("SetStatus")!.Invoke(recruit, new object[] { SessionStatus.Disconnected });
        });
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() =>
        {
            var system = Server.System<QualificationSystem>();
            Assert.That(error, Is.EqualTo("target_offline"));
            Assert.That(system.Service.Snapshot().Audit, Is.Empty);
            Assert.That(system.Service.Snapshot().Players, Is.Empty);
            Assert.That(system.View(ServerSession, recruit.UserId).TargetOnline, Is.False);
        });
    }

    [Test]
    public async Task AccreditedInstructorCanSelectRecruitBeforeSpawningButCannotMutate()
    {
        await PrepareInstructor();
        var recruit = await Server.AddDummySession("LobbyRecruit");
        await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, null));
        string error = null;
        Guid selected = Guid.Empty;
        await Server.WaitPost(() => Server.System<QualificationSystem>().Submit(ServerSession, QualificationAction.View,
            new QualificationRequest { Target = recruit.UserId }, result => error = result, target => selected = target));
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() =>
        {
            var view = Server.System<QualificationSystem>().View(ServerSession, selected);
            Assert.That(error, Is.Empty); Assert.That(selected, Is.EqualTo((Guid) recruit.UserId));
            Assert.That(view.Instructor, Is.True); Assert.That(view.InstructorOnDuty, Is.False);
        });
        await Task.Delay(250);
        await Server.WaitPost(() => Server.System<QualificationSystem>().Submit(ServerSession, QualificationAction.Complete,
            new QualificationRequest { Target = recruit.UserId, Qualification = "enlisted", Item = "practice", Reason = "outside round" }, result => error = result));
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() => Assert.That(error, Is.EqualTo("permission")));
    }

    [Test]
    public async Task OrdinaryPlayerCannotSelectRecruitByNicknameOrReadTheirPrivateRecord()
    {
        var recruit = await Server.AddDummySession("PrivateRecruit");
        await Server.WaitPost(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>(); if (admins.GetAdminData(ServerSession) != null) admins.DeAdmin(ServerSession);
            var seed = new QualificationStore(); seed.Players[recruit.UserId] = new() { Player = recruit.UserId };
            Server.System<QualificationSystem>().ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(5);
        string error = null; var selected = Guid.Empty;
        await Server.WaitPost(() => Server.System<QualificationSystem>().Submit(ServerSession, QualificationAction.View,
            new QualificationRequest { TargetName = "PrivateRecruit" }, result => error = result, target => selected = target));
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() =>
        {
            var view = Server.System<QualificationSystem>().View(ServerSession, recruit.UserId);
            Assert.That(error, Is.EqualTo("permission")); Assert.That(selected, Is.EqualTo(Guid.Empty));
            Assert.That(view.Target, Is.EqualTo((Guid) ServerSession.UserId));
            Assert.That(view.Store.Players, Is.Empty); Assert.That(view.AccountNames.ContainsKey(recruit.UserId), Is.False);
            Assert.That(view.AccountNames[ServerSession.UserId], Is.EqualTo(ServerSession.Name));
        });
    }

    [Test]
    public async Task ManagementOpensOfflinePlayerByNicknameAndFailedLookupKeepsPreviousSelection()
    {
        var offline = new NetUserId(Guid.NewGuid());
        Task recordWrite = null;
        await Server.WaitPost(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>(); if (admins.GetAdminData(ServerSession) != null) admins.DeAdmin(ServerSession);
            var seed = new QualificationStore(); seed.Management.Add(ServerSession.UserId);
            Server.System<QualificationSystem>().ConfigureRepository(new MemoryQualificationRepository(), seed);
            recordWrite = Server.ResolveDependency<IServerDbManager>().UpdatePlayerRecordAsync(offline, "OfflineRecruit", System.Net.IPAddress.Loopback, null);
        });
        await recordWrite; await Pair.RunTicksSync(10);
        Content.Server._RuCM.Qualifications.QualificationEui eui = null;
        var requestId = Guid.NewGuid();
        await Server.WaitPost(() =>
        {
            eui = new(Server.System<QualificationSystem>()); Server.ResolveDependency<EuiManager>().OpenEui(eui, ServerSession);
            eui.HandleMessage(new QualificationEuiRequest(QualificationAction.View, new QualificationRequest { TargetName = "OfflineRecruit", RequestId = requestId }));
        });
        await Pair.RunTicksSync(30);
        await Server.WaitAssertion(() =>
        {
            var view = ((QualificationEuiState) eui.GetNewState()).View;
            Assert.That(view.Target, Is.EqualTo((Guid) offline)); Assert.That(view.TargetOnline, Is.False);
            Assert.That(view.AccountNames[offline], Is.EqualTo("OfflineRecruit")); Assert.That(view.ResponseId, Is.EqualTo(requestId));
        });
        await Task.Delay(250);
        await Server.WaitPost(() => eui.HandleMessage(new QualificationEuiRequest(QualificationAction.View, new QualificationRequest { TargetName = "NoSuchAccount", RequestId = Guid.NewGuid() })));
        await Pair.RunTicksSync(30);
        await Server.WaitAssertion(() =>
        {
            var view = ((QualificationEuiState) eui.GetNewState()).View;
            Assert.That(view.Target, Is.EqualTo((Guid) offline)); Assert.That(view.Error, Is.EqualTo("account_not_found"));
            eui.Close();
        });
    }

    [Test]
    public async Task CharacterContextVerbOpensSelectedRecruitAndRechecksDisconnection()
    {
        await PrepareInstructor();
        var recruit = await Server.AddDummySession("ContextRecruit");
        EntityUid body = default;
        Content.Shared.Verbs.Verb verb = null;
        await Server.WaitAssertion(() =>
        {
            var ent = Server.EntMan;
            body = ent.SpawnEntity("CMMobHuman", ent.GetComponent<Robust.Shared.GameObjects.TransformComponent>(ServerSession.AttachedEntity.Value).Coordinates);
            Server.PlayerMan.SetAttachedEntity(recruit, body);
            var verbs = new Content.Shared.Verbs.GetVerbsEvent<Content.Shared.Verbs.Verb>(ServerSession.AttachedEntity.Value, body, null, null, true, true, true, new());
            ent.EventBus.RaiseEvent(EventSource.Local, verbs);
            verb = verbs.Verbs.Single(v => v.Text == Server.ResolveDependency<Robust.Shared.Localization.ILocalizationManager>().GetString("rucm-qualifications-open-record-verb"));
            verb.Act();
        });
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<Robust.Client.UserInterface.IUserInterfaceManager>();
            var window = Content.Client._RuCM.Qualifications.QualificationEntrySystem.Descendants(ui.WindowRoot)
                .OfType<Content.Client._RuCM.Qualifications.QualificationWindow>().Single();
            var text = string.Join(" ", Content.Client._RuCM.Qualifications.QualificationEntrySystem.Descendants(window)
                .OfType<Robust.Client.UserInterface.Controls.RichTextLabel>().Select(l => l.GetMessage().ToString()));
            Assert.That(text, Does.Contain("ContextRecruit"));
            window.Close();
        });
        await RunUntilSynced();
        await Server.WaitPost(() =>
        {
            recruit.GetType().GetMethod("SetStatus").Invoke(recruit, new object[] { SessionStatus.Disconnected });
            verb.Act();
        });
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<Robust.Client.UserInterface.IUserInterfaceManager>();
            Assert.That(Content.Client._RuCM.Qualifications.QualificationEntrySystem.Descendants(ui.WindowRoot)
                .OfType<Content.Client._RuCM.Qualifications.QualificationWindow>(), Is.Empty);
        });
    }
}

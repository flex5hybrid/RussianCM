using System.Collections.Generic;
using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.CMU14.Round;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Station.Systems;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Antag;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.CMU14.Qualifications;

[TestFixture]
public sealed class CMURecruitAntagTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Destructive = true };

    [TestPrototypes]
    private const string Prototypes = """
        - type: antagSpecifier
          id: CMUTestRecruitAntag
          allowNonHumans: true

        - type: entity
          parent: BaseGameRule
          id: CMUTestRecruitAntagRule
          components:
          - type: AntagSelection
            selectionTime: Never
            antags:
            - !type:FixedAntagCount
              proto: CMUTestRecruitAntag
              count: 1
              playerRatio: 1
        """;

    [TestCase(true)]
    [TestCase(false)]
    public async Task CurrentRecruitCannotBecomeAntagEvenWithoutPreferenceCheck(bool checkPreferences)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var entities = Server.EntMan;
            var selection = Server.System<AntagSelectionSystem>();
            var body = entities.SpawnEntity("CMMobHuman", map.GridCoords);
            var minds = Server.System<SharedMindSystem>();
            var mind = minds.GetOrCreateMind(ServerSession.UserId);
            minds.TransferTo(mind, body);
            Server.PlayerMan.SetAttachedEntity(ServerSession, body);
            var roles = Server.System<SharedRoleSystem>();
            roles.MindAddJobRole(mind, jobPrototype: GOVFORRecruitJob.Id);
            var ruleUid = entities.SpawnEntity("CMUTestRecruitAntagRule", map.GridCoords);
            Entity<AntagSelectionComponent> rule = (ruleUid, entities.GetComponent<AntagSelectionComponent>(ruleUid));
            var definition = Server.ProtoMan.Index<AntagSpecifierPrototype>("CMUTestRecruitAntag");

            Assert.That(Server.ProtoMan.Index<JobPrototype>(GOVFORRecruitJob.Id).CanBeAntag, Is.False);
            Assert.That(selection.IsSessionValid(ServerSession, rule, definition), Is.False);
            Assert.That(selection.IsMindValid(ServerSession, definition), Is.False);
            Assert.That(selection.IsEntityValid(body, definition), Is.False);
            Assert.That(selection.CanBeAntag(ServerSession, rule, definition, checkPreferences), Is.False);
            Assert.That(selection.TryMakeAntag(rule, definition, ServerSession, checkPreferences), Is.False);
            Assert.That(rule.Comp.AssignedMinds, Is.Empty);

            // An attached-entity shortcut must not turn a temporarily detached recruit into an antag.
            Server.PlayerMan.SetAttachedEntity(ServerSession, null);
            Assert.That(selection.IsSessionValid(ServerSession, rule, definition), Is.False);
            Server.PlayerMan.SetAttachedEntity(ServerSession, body);

            // The restriction belongs to this profession, not this account or its future characters.
            roles.MindAddJobRole(mind, jobPrototype: "AU14JobGOVFORSquadRifleman");
            Assert.That(selection.CanBeAntag(ServerSession, rule, definition, false), Is.True);
            Assert.That(selection.TryMakeAntag(rule, definition, ServerSession, false), Is.True);
            Assert.That(selection.GetAssignedAntagCount(rule, definition), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task RoundstartPreselectionExcludesRecruitAndDelayedAssignmentRechecksTheCurrentJob()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var entities = Server.EntMan;
            typeof(GameTicker).GetField("_runLevel", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Server.System<GameTicker>(), GameRunLevel.PreRoundLobby);
            Server.System<AuRoundSystem>().SetPreset(Server.ProtoMan.Index<GamePresetPrototype>("Insurgency"));
            var selection = Server.System<AntagSelectionSystem>();
            var body = entities.SpawnEntity("CMMobHuman", map.GridCoords);
            var minds = Server.System<SharedMindSystem>();
            var mind = minds.GetOrCreateMind(ServerSession.UserId);
            minds.TransferTo(mind, body);
            Server.PlayerMan.SetAttachedEntity(ServerSession, body);
            var roles = Server.System<SharedRoleSystem>();
            var ruleUid = entities.SpawnEntity("CMUTestRecruitAntagRule", map.GridCoords);
            Entity<AntagSelectionComponent> rule = (ruleUid, entities.GetComponent<AntagSelectionComponent>(ruleUid));
            var definition = Server.ProtoMan.Index<AntagSpecifierPrototype>("CMUTestRecruitAntag");

            var profile = HumanoidCharacterProfile.DefaultWithSpecies().WithAge(30).WithJobPriorities(
                new Dictionary<ProtoId<JobPrototype>, JobPriority> { [GOVFORRecruitJob.Id] = JobPriority.High });
            var getCandidates = typeof(StationJobsSystem).GetMethod("GetJobCandidates", BindingFlags.Instance | BindingFlags.NonPublic)!;
            object[] arguments = { new Dictionary<NetUserId, HumanoidCharacterProfile> { [ServerSession.UserId] = profile } };
            var ordinaryCandidates = (Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>>)getCandidates.Invoke(
                Server.System<StationJobsSystem>(), arguments)!;
            Assert.That(ordinaryCandidates.ContainsKey(GOVFORRecruitJob.Id), Is.True, "The recruit job is otherwise available");
            typeof(AntagSelectionSystem).GetMethod("PreSelectSession", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(selection, new object[] { rule, new ProtoId<AntagSpecifierPrototype>(definition.ID), ServerSession });
            var candidates = (Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>>)getCandidates.Invoke(
                Server.System<StationJobsSystem>(), arguments)!;
            Assert.That(selection.GetPreSelectedAntagSessions(), Does.Contain(ServerSession));
            Assert.That(candidates.ContainsKey(GOVFORRecruitJob.Id), Is.False,
                "The real roundstart allocator must not give an antag the recruit profession");

            roles.MindAddJobRole(mind, jobPrototype: GOVFORRecruitJob.Id);
            var initialize = typeof(AntagSelectionSystem).GetMethod("TryInitializeAntag", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.That(initialize.Invoke(selection, new object[] { rule, definition, ServerSession }), Is.False);
            Assert.That(rule.Comp.PreSelectedSessions[definition.ID], Is.Empty);
            Assert.That(rule.Comp.AssignedMinds, Is.Empty);
            Assert.That(ServerSession.AttachedEntity, Is.EqualTo(body), "Reject before replacing the recruit's body");
        });
    }
}

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Qualifications;
using Content.Server.Administration.Managers;
using Content.Server.CMU14.Qualifications.Training;
using Content.Server.CMU14.Round;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Alert;
using Content.Shared.CMU14.Qualifications.Training;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
namespace Content.IntegrationTests.CMU14.Qualifications;

[TestFixture, NonParallelizable]
public sealed class CMUGovforTrainingTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Destructive = true };
    private EntityUid _teacherA, _teacherB, _recruit;
    private ICommonSession _other = default!, _student = default!;
    private const string Firearms = "CMUBasicFirearmsTraining";
    private const string Engineering = "CMUBasicEngineeringTraining";

    private async Task Prepare()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitPost(() =>
            typeof(GameTicker).GetMethod("IncrementRoundNumber", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(Server.System<GameTicker>(), null));
        _other = await Server.AddDummySession("InstructorB");
        _student = await Server.AddDummySession("RecruitA");
        await Server.WaitPost(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>();
            foreach (var session in new[] { _other, _student })
                if (admins.GetAdminData(session) != null) admins.DeAdmin(session);
            var seed = new QualificationStore();
            foreach (var id in new[] { "enlisted", "field_engineering", "medical" })
                seed.Definitions[id] = new()
                {
                    Id = id,
                    Name = id,
                    Items = new() { new() { Id = "firearms", Name = "firearms" },
                        new() { Id = "first_aid", Name = "first_aid" }, new() { Id = "engineering_basics", Name = "engineering_basics" },
                        new() { Id = "practice", Name = "practice" } }
                };
            var now = DateTimeOffset.UtcNow;
            foreach (var session in new[] { ServerSession, _other })
            {
                var context = new TrainingContext(session.UserId, session.Name, "AU14JobGOVFORadvisor", 1, "test", now);
                seed.Instructors[session.UserId] = new(true, true, true, new() { "field_engineering", "medical" },
                    session.UserId, now, session.UserId, now);
                seed.Players[session.UserId] = new()
                {
                    Player = session.UserId,
                    Grants = new() { ["sergeant"] = new("sergeant", QualificationStatus.Active, 1, Array.Empty<string>(), context, "test") }
                };
            }
            Server.System<QualificationSystem>().ConfigureRepository(new MemoryQualificationRepository(), seed);
            typeof(GameTicker).GetField("_runLevel", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(Server.System<GameTicker>(), GameRunLevel.InRound);
            Server.System<AuRoundSystem>().SetPreset(Server.ProtoMan.Index<GamePresetPrototype>("Insurgency"));
            _teacherA = Spawn(ServerSession, "AU14JobGOVFORadvisor");
            _teacherB = Spawn(_other, "AU14JobGOVFORadvisor");
            _recruit = Spawn(_student, GOVFORRecruitJob.Id);

            EntityUid Spawn(ICommonSession player, string job)
            {
                var body = Server.EntMan.SpawnEntity("CMMobHuman", map.GridCoords);
                var minds = Server.System<SharedMindSystem>();
                var mind = minds.GetOrCreateMind(player.UserId);
                minds.TransferTo(mind, body);
                Server.System<SharedRoleSystem>().MindAddJobRole(mind, jobPrototype: job);
                Server.PlayerMan.SetAttachedEntity(player, body);
                return body;
            }
        });
        await Pair.RunTicksSync(15);
        await Server.WaitAssertion(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>();
            foreach (var session in new[] { _other, _student })
                if (admins.GetAdminData(session) != null) admins.DeAdmin(session);
            Assert.That(Server.System<QualificationSystem>().Authority(ServerSession).CurrentParticipant, Is.True);
            Assert.That(Server.System<QualificationSystem>().View(_other, _student.UserId).Management, Is.False);
            Assert.That(Server.System<CMUGovforTrainingSystem>().IsRecruit(_recruit), Is.True);
            // Exercise the real job skills rather than a separate, all-zero human fixture.
            var job = Server.ProtoMan.Index<JobPrototype>(GOVFORRecruitJob.Id);
            var skillSystem = Server.System<SkillsSystem>();
            foreach (var (skill, level) in ((SkillsComponent)job.RoundComponents["Skills"].Component).Skills)
                skillSystem.SetSkill(_recruit, skill, level);
        });
    }

    [Test]
    public async Task AssignmentOwnershipAndForgedTopicAreRejectedAtServerBoundary()
    {
        await Prepare();
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Assign(_teacherB, _recruit), Is.EqualTo("cmu-training-assigned"));
            Assert.That(training.Apply(_other, QualificationAction.TrainingStart,
                new() { Target = _student.UserId, Qualification = Firearms }), Is.EqualTo("permission"));
            Assert.That(training.Start(_teacherB, _recruit, Firearms), Is.Not.Empty);
            Assert.That(training.Start(_teacherA, _recruit, "RMCSkillEngineer"), Is.Not.Empty);
            Assert.That(training.ActiveTopic(_student.UserId), Is.Null);
            Assert.That(training.AssignedInstructor(_student.UserId), Is.EqualTo((Guid)ServerSession.UserId));
            Assert.That(Server.System<QualificationSystem>().View(_other, _student.UserId).TrainingRecruits, Is.Empty,
                "Another teacher cannot see someone else's recruit in their training roster");
        });
        string? response = null;
        await Server.WaitPost(() => Server.System<QualificationSystem>().Submit(_other,
            QualificationAction.TrainingStart,
            new QualificationRequest { Target = _student.UserId, Instructor = ServerSession.UserId, Qualification = Firearms },
            error => response = error));
        await Pair.RunTicksSync(15);
        await Server.WaitAssertion(() =>
        {
            Assert.That(response, Is.EqualTo("permission"), "The BUI/EUI queue must reject a forged instructor identity");
            Assert.That(Server.System<CMUGovforTrainingSystem>().ActiveTopic(_student.UserId), Is.Null);
        });
    }

    [Test]
    public async Task TemporarySkillsUseNativeQueriesAndNeverOverwriteNormalRoleSkills()
    {
        await Prepare();
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            var skills = Server.System<SkillsSystem>();
            Assert.That(skills.GetSkill(_recruit, "RMCSkillFirearms"), Is.EqualTo(1));
            Assert.That(skills.GetSkill(_recruit, "RMCSkillConstruction"), Is.EqualTo(1));
            Assert.That(skills.GetSkill(_recruit, "RMCSkillMedical"), Is.Zero);
            skills.SetSkill(_recruit, "RMCSkillEngineer", 1);
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherA, _recruit, Engineering), Is.Empty);
            Assert.That(skills.GetSkill(_recruit, "RMCSkillEngineer"), Is.EqualTo(2));
            Assert.That(skills.HasSkill(_recruit, "RMCSkillConstruction", 2), Is.True);
            Assert.That(skills.HasAllSkills(_recruit, new System.Collections.Generic.Dictionary<EntProtoId<SkillDefinitionComponent>, int>
            { ["RMCSkillEngineer"] = 2, ["RMCSkillConstruction"] = 2 }), Is.True);
            Assert.That(skills.HasAllSkills(_recruit, new System.Collections.Generic.List<Skill> { new("RMCSkillEngineer", 2) }), Is.True);
            Assert.That(training.Start(_teacherA, _recruit, Firearms), Is.Empty);
            Assert.That(skills.GetSkill(_recruit, "RMCSkillEngineer"), Is.EqualTo(1),
                "Changing topics removes the previous topic's access");
            Assert.That(skills.HasSkill(_recruit, "RMCSkillConstruction", 2), Is.False);
            Assert.That(skills.GetSkill(_recruit, "RMCSkillConstruction"), Is.EqualTo(1));
            Assert.That(training.Start(_teacherA, _recruit, Engineering), Is.Empty);
            skills.SetSkill(_recruit, "RMCSkillEngineer", 3); // Real role skill changes survive session removal.
            Assert.That(skills.GetSkill(_recruit, "RMCSkillEngineer"), Is.EqualTo(3));
            training.Finish(_student.UserId, "test");
            Assert.That(Server.EntMan.HasComponent<CMUTrainingSkillsComponent>(_recruit), Is.False);
            Assert.That(skills.GetSkill(_recruit, "RMCSkillEngineer"), Is.EqualTo(3));
            Assert.That(skills.HasSkill(_recruit, "RMCSkillConstruction", 2), Is.False);
            Assert.That(skills.GetSkill(_recruit, "RMCSkillConstruction"), Is.EqualTo(1));
            Assert.That(skills.GetSkill(_recruit, "RMCSkillFirearms"), Is.EqualTo(1));
            Assert.That(skills.GetSkill(_recruit, "RMCSkillMedical"), Is.Zero);
        });
    }

    [Test]
    public async Task ReassignmentEndsLessonAndImmediatelyChangesNavigation()
    {
        await Prepare();
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherA, _recruit, Engineering), Is.Empty);
            Assert.That(training.Apply(ServerSession, QualificationAction.TrainingTrack, new() { Target = _student.UserId }), Is.Empty);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingNavigationComponent>(_teacherA), Is.True);
            Assert.That(training.Apply(ServerSession, QualificationAction.TrainingAssign,
                new() { Target = _student.UserId, Instructor = _other.UserId }), Is.Empty);
            Assert.That(training.AssignedInstructor(_student.UserId), Is.EqualTo((Guid)_other.UserId));
            Assert.That(training.ActiveTopic(_student.UserId), Is.Null);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingSkillsComponent>(_recruit), Is.False);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingNavigationComponent>(_teacherA), Is.False);
            Assert.That(Server.EntMan.GetComponent<CMUTrainingNavigationComponent>(_recruit).TargetName,
                Is.EqualTo(Server.EntMan.GetComponent<MetaDataComponent>(_teacherB).EntityName));
        });
    }

    [Test]
    public async Task InstructorDetachAndSyntheticConversionCleanAllRoundState()
    {
        await Prepare();
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherA, _recruit, Engineering), Is.Empty);
            Server.PlayerMan.SetAttachedEntity(ServerSession, null);
            Assert.That(training.AssignedInstructor(_student.UserId), Is.Null);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingSkillsComponent>(_recruit), Is.False);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingNavigationComponent>(_recruit), Is.False);
            Server.PlayerMan.SetAttachedEntity(ServerSession, _teacherA);
            Server.EntMan.EnsureComponent<SynthComponent>(_recruit);
            Assert.That(training.Assign(_teacherA, _recruit), Is.Not.Empty);
            Assert.That(Server.System<QualificationSystem>().View(ServerSession, _student.UserId).TrainingRecruits, Is.Empty);
        });
    }

    [Test]
    public async Task DisconnectInstructorImmediatelyRevokesSkillsAndAssignment()
    {
        await Prepare();
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Assert.That(training.Assign(_teacherB, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherB, _recruit, Engineering), Is.Empty);
            Server.PlayerMan.SetStatus(_other, Robust.Shared.Enums.SessionStatus.Disconnected);
            Assert.That(training.AssignedInstructor(_student.UserId), Is.Null);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingSkillsComponent>(_recruit), Is.False);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingNavigationComponent>(_recruit), Is.False);
        });
    }

    [Test]
    public async Task CompletedChecklistPersistsProgressWithoutTemporarySkills()
    {
        await Prepare();
        string? response = null;
        await Server.WaitPost(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherA, _recruit, Engineering), Is.Empty);
            Assert.That(training.Apply(ServerSession, QualificationAction.TrainingTrack,
                new() { Target = _student.UserId }), Is.Empty);
        });
        await Pair.RunTicksSync(3);
        await Server.WaitPost(() =>
        {
            var recruitAlerts = Server.EntMan.GetComponent<AlertsComponent>(_recruit).Alerts;
            var instructorAlerts = Server.EntMan.GetComponent<AlertsComponent>(_teacherA).Alerts;
            Assert.That(recruitAlerts[AlertKey.ForCategory("SquadTracker")].Type.ToString(), Is.EqualTo("CMUTrainingInstructor"));
            Assert.That(instructorAlerts[AlertKey.ForCategory("SquadTracker")].Type.ToString(), Is.EqualTo("CMUTrainingRecruit"));
            Assert.That(Server.EntMan.GetComponent<CMUTrainingNavigationComponent>(_recruit).SendOnlyToOwner, Is.True);
            var qualification = Server.System<QualificationSystem>();
            qualification.Submit(ServerSession, QualificationAction.Complete,
                new QualificationRequest
                {
                    Target = _student.UserId,
                    Qualification = "enlisted",
                    Item = "engineering_basics",
                    Reason = "Personally verified engineering practice",
                    Revision = qualification.Service.Revision
                },
                error => response = error);
        });
        await Pair.RunTicksSync(15);
        await Server.WaitAssertion(() =>
        {
            Assert.That(response, Is.Empty);
            Assert.That(Server.System<QualificationSystem>().Service.PlayerSnapshot(_student.UserId)!
                .Progress["enlisted"].ContainsKey("engineering_basics"), Is.True);
            Assert.That(Server.System<CMUGovforTrainingSystem>().ActiveTopic(_student.UserId), Is.Null);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingSkillsComponent>(_recruit), Is.False);
            Assert.That(Server.System<SkillsSystem>().HasSkill(_recruit, "RMCSkillConstruction", 2), Is.False);
        });
    }

    [Test]
    public async Task RoleLossSyntheticConversionDeletionAndRevokedAccreditationDenyAccess()
    {
        await Prepare();
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherA, _recruit, Engineering), Is.Empty);
            var mind = Server.System<SharedMindSystem>().GetOrCreateMind(_student.UserId);
            Server.System<SharedRoleSystem>().MindAddJobRole(mind, jobPrototype: "AU14JobGOVFORadvisor");
            Assert.That(Server.System<SkillsSystem>().HasSkill(_recruit, "RMCSkillConstruction", 2), Is.False,
                "Role loss invalidates effective skills before the periodic cleanup");
        });
        await Pair.RunTicksSync(20);
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Assert.That(training.AssignedInstructor(_student.UserId), Is.Null);
            var mind = Server.System<SharedMindSystem>().GetOrCreateMind(_student.UserId);
            Server.System<SharedRoleSystem>().MindAddJobRole(mind, jobPrototype: GOVFORRecruitJob.Id);
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherA, _recruit, Engineering), Is.Empty);
            Server.EntMan.EnsureComponent<SynthComponent>(_recruit);
            Assert.That(Server.System<SkillsSystem>().HasSkill(_recruit, "RMCSkillConstruction", 2), Is.False);
        });
        await Pair.RunTicksSync(20);
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Assert.That(training.AssignedInstructor(_student.UserId), Is.Null);
            Server.EntMan.RemoveComponent<SynthComponent>(_recruit);
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherA, _recruit, Engineering), Is.Empty);
            Server.EntMan.DeleteEntity(_teacherA);
            Assert.That(training.AssignedInstructor(_student.UserId), Is.Null);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingSkillsComponent>(_recruit), Is.False);
            Assert.That(training.Assign(_teacherB, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherB, _recruit, Engineering), Is.Empty);
            var qualification = Server.System<QualificationSystem>();
            var seed = qualification.Service.Snapshot();
            seed.Instructors.Remove(_other.UserId);
            qualification.ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(20);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.System<CMUGovforTrainingSystem>().AssignedInstructor(_student.UserId), Is.Null);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingSkillsComponent>(_recruit), Is.False);
        });
    }

    [Test]
    public async Task RecruitLimitAndRoundCleanupAreEnforced()
    {
        await Prepare();
        await Server.WaitAssertion(() =>
        {
            var training = Server.System<CMUGovforTrainingSystem>();
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CMUTrainingCVars.MaxRecruits, 0);
            Assert.That(training.Assign(_teacherA, _recruit), Is.EqualTo("cmu-training-limit"));
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CMUTrainingCVars.MaxRecruits, 4);
            Assert.That(training.Assign(_teacherA, _recruit), Is.Empty);
            Assert.That(training.Start(_teacherA, _recruit, Firearms), Is.Empty);
            Server.EntMan.EventBus.RaiseEvent(EventSource.Local, new Content.Shared.GameTicking.RoundRestartCleanupEvent());
            Assert.That(training.AssignedInstructor(_student.UserId), Is.Null);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingSkillsComponent>(_recruit), Is.False);
            Assert.That(Server.EntMan.HasComponent<CMUTrainingNavigationComponent>(_recruit), Is.False);
        });
    }
}

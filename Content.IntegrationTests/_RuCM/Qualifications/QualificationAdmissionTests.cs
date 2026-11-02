using System;
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Qualifications;
using Content.Server.CMU14.Round;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Presets;
using Content.Server.Players.JobWhitelist;
using Content.Server.Station.Events;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.CCVar;
using Content.Shared.CMU14.Round.Roles;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._RuCM.Qualifications;

public sealed class QualificationAdmissionTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Destructive = true };
    private static readonly string[] InstructorJobs =
        { "AU14JobGOVFORadvisor", "AU14JobGOVFORadvisorRMC", "AU14JobGOVFORadvisorUPP" };
    private static QualificationStore Seed()
    {
        var seed = new QualificationStore();
        foreach (var id in QualificationRules.Levels.Concat(new[] { "medical" }))
            seed.Definitions[id] = new() { Id = id, Name = "rucm-qualifications-definition-" + id,
                Items = new() { new() { Id = "practice", Name = "practice" } } };
        foreach (var job in InstructorJobs) seed.Roles[job] = new() { JobId = job, MinimumLevel = MilitaryLevel.Sergeant };
        return seed;
    }
    private async Task Configure(QualificationStore seed)
    {
        await Server.WaitPost(() => Server.System<QualificationSystem>().ConfigureRepository(new MemoryQualificationRepository(), seed));
        await Pair.RunTicksSync(10);
        await Server.WaitPost(() =>
        {
            var cfg = Server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(QualificationCVars.Enabled, true); cfg.SetCVar(QualificationCVars.Enforce, true);
            cfg.SetCVar(CCVars.GameRoleWhitelist, true);
            Server.System<AuRoundSystem>().SetPreset(Server.ResolveDependency<IPrototypeManager>().Index<GamePresetPrototype>("Insurgency"));
            Server.System<QualificationSystem>().SynchronizeRolePolicy();
        });
    }

    [TestCase(true, true, "Insurgency")]
    [TestCase(true, false, "Insurgency")]
    [TestCase(false, false, "Insurgency")]
    [TestCase(true, true, "ForceOnForce")]
    [TestCase(true, true, "DistressSignal")]
    public async Task InstructorAdmissionAndRevocationAreEnforcedAtTheActualSpawnBoundary(bool enabled, bool enforce, string preset)
    {
        var seed = Seed();
        var now = DateTimeOffset.UtcNow;
        var context = new TrainingContext(Guid.NewGuid(), "manager", "", 1, "test", now);
        seed.Players[ServerSession.UserId] = new() { Player = ServerSession.UserId,
            Grants = new() { ["sergeant"] = new("sergeant", QualificationStatus.Active, 1, Array.Empty<string>(), context, "test") } };
        await Configure(seed);
        await Server.WaitPost(() =>
        {
            var cfg = Server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(QualificationCVars.Enabled, enabled);
            cfg.SetCVar(QualificationCVars.Enforce, enforce);
            Server.System<AuRoundSystem>().SetPreset(Server.ProtoMan.Index<GamePresetPrototype>(preset));
            Server.System<QualificationSystem>().SynchronizeRolePolicy();
        });
        await Server.WaitAssertion(() =>
        {
            Server.ResolveDependency<IConfigurationManager>().SetCVar(QualificationCVars.FailOpen, true);
            foreach (var id in InstructorJobs) Assert.That(Server.System<QualificationSystem>().CanTakeJob(ServerSession.UserId, id), Is.False);
            Assert.That(Server.System<QualificationSystem>().Service.Snapshot().Instructors, Is.Empty);
        });
        foreach (var active in new[] { true, false })
        {
            Task mutation = null;
            await Server.WaitPost(() =>
            {
                var service = Server.System<QualificationSystem>().Service;
                mutation = service.Apply(new(context, true, false, false, false, false), QualificationAction.SaveInstructor, new()
                {
                    Target = ServerSession.UserId, Revision = service.Revision, Reason = "accreditation change",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new InstructorAccreditation(active, true, true, new(),
                        context.Actor, now, context.Actor, now))
                });
            });
            await mutation;
            await Server.WaitPost(() => Server.System<QualificationSystem>().Update(1));
            await RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var policy = Client.System<QualificationRolePolicy>();
                foreach (var id in InstructorJobs)
                {
                    Assert.That(policy.Eligibility[id], Is.EqualTo(active), "Lobby/latejoin eligibility follows accreditation changes");
                    Assert.That(Client.ProtoMan.Index<JobPrototype>(id).Whitelisted, Is.False);
                    var requirement = new QualificationAccessRequirement { Job = id };
                    Assert.That(requirement.Check(Client.EntMan, Client.ProtoMan, HumanoidCharacterProfile.DefaultWithSpecies(),
                        new Dictionary<string, TimeSpan>(), out _), Is.EqualTo(active));
                }
            });
            await Server.WaitAssertion(() =>
            {
                foreach (var id in InstructorJobs)
                {
                    Assert.That(Server.System<QualificationSystem>().CanTakeJob(ServerSession.UserId, id), Is.EqualTo(active));
                    Assert.That(Server.ResolveDependency<JobWhitelistManager>().IsAllowed(ServerSession, id), Is.True,
                        "An instructor never needs a second job whitelist");
                    if (!active)
                    {
                        var candidates = new StationJobsGetCandidatesEvent(ServerSession.UserId, new() { id }, HumanoidCharacterProfile.DefaultWithSpecies());
                        Server.EntMan.EventBus.RaiseEvent(EventSource.Local, ref candidates);
                        Assert.That(candidates.Jobs, Is.Empty);
                        var disallowed = new GetDisallowedJobsEvent(ServerSession, new());
                        Server.EntMan.EventBus.RaiseEvent(EventSource.Local, ref disallowed);
                        Assert.That(disallowed.Jobs, Does.Contain(new ProtoId<JobPrototype>(id)));
                        var allowed = new IsRoleAllowedEvent(ServerSession, new() { id }, null);
                        Server.EntMan.EventBus.RaiseEvent(EventSource.Local, ref allowed);
                        Assert.That(allowed.Cancelled, Is.True);
                    }
                    foreach (var late in new[] { false, true })
                    {
                        var spawn = new PlayerBeforeSpawnEvent(ServerSession, HumanoidCharacterProfile.DefaultWithSpecies(), id, late, EntityUid.Invalid);
                        Server.EntMan.EventBus.RaiseEvent(EventSource.Local, spawn);
                        Assert.That(spawn.JobId, Is.EqualTo(active ? id : null));
                    }
                }
            });
        }
    }

    [Test]
    public async Task InstructorHasNoExtraWhitelistAndPreservesOtherNonTimeRequirements()
    {
        Dictionary<string, bool> whitelists = null;
        Dictionary<string, HashSet<JobRequirement>> requirements = null;
        await Server.WaitAssertion(() =>
        {
            var prototypes = Server.ResolveDependency<IPrototypeManager>(); var roles = Server.System<SharedRoleSystem>();
            whitelists = prototypes.EnumeratePrototypes<JobPrototype>().ToDictionary(j => j.ID, j => j.Whitelisted);
            requirements = InstructorJobs.ToDictionary(id => id, id => new HashSet<JobRequirement>(roles.GetRoleRequirements(prototypes.Index<JobPrototype>(id))));
            foreach (var job in InstructorJobs) Assert.That(whitelists[job], Is.False);
        });
        await Configure(Seed());
        await Server.WaitAssertion(() =>
        {
            var prototypes = Server.ResolveDependency<IPrototypeManager>(); var roles = Server.System<SharedRoleSystem>();
            foreach (var job in prototypes.EnumeratePrototypes<JobPrototype>())
                Assert.That(job.Whitelisted, Is.EqualTo(InstructorJobs.Contains(job.ID) ? false : whitelists[job.ID]), job.ID);
            foreach (var id in InstructorJobs)
            {
                Assert.That(Server.ResolveDependency<JobWhitelistManager>().IsAllowed(ServerSession, id), Is.True);
                var actual = roles.GetRoleRequirements(prototypes.Index<JobPrototype>(id));
                foreach (var requirement in requirements[id].Where(r => r is not OverallPlaytimeRequirement and not RoleTimeRequirement and not DepartmentTimeRequirement and not QualificationAccessRequirement))
                    Assert.That(actual, Does.Contain(requirement), id);
                Assert.That(actual.OfType<QualificationAccessRequirement>().Count(), Is.EqualTo(1));
            }
            Server.ResolveDependency<IConfigurationManager>().SetCVar(QualificationCVars.Enabled, false);
            Server.System<QualificationSystem>().SynchronizeRolePolicy();
            foreach (var id in InstructorJobs)
            {
                Assert.That(prototypes.Index<JobPrototype>(id).Whitelisted, Is.False);
                Assert.That(Server.System<QualificationSystem>().CanTakeJob(ServerSession.UserId, id), Is.False,
                    "Instructor accreditation is required even if general qualification enforcement is disabled");
            }
        });
    }

    [Test]
    public async Task AllRealGovforSyntheticJobsKeepWhitelistAndSkipGateSpawnParticipationAndMigration()
    {
        var map = await Pair.CreateTestMap(); string[] jobs = null;
        await Server.WaitAssertion(() =>
        {
            jobs = Server.ResolveDependency<IPrototypeManager>().EnumeratePrototypes<JobPrototype>()
                .Where(j => j.RoundSide == RoundJobSide.Govfor && j.IsSynthetic).Select(j => j.ID).ToArray();
            foreach (var expected in new[] { "AU14JobGOVFORAuxSupportSynth", "AU14JobGOVFORAuxSupportSynthRMC", "AU14JobGOVFORAuxSupportSynthUPP" })
                Assert.That(jobs, Does.Contain(expected));
        });
        var seed = Seed();
        foreach (var id in jobs) seed.Roles[id] = new() { JobId = id, MinimumLevel = MilitaryLevel.Officer, Professional = new() { "medical" } };
        seed.MigrationGroups["medical"] = jobs.ToHashSet();
        await Configure(seed);
        await Server.WaitAssertion(() =>
        {
            var system = Server.System<QualificationSystem>(); var ent = Server.EntMan;
            var whitelist = Server.ResolveDependency<JobWhitelistManager>();
            var body = ent.SpawnEntity("CMMobHuman", map.GridCoords);
            foreach (var id in jobs)
            {
                var job = Server.ResolveDependency<IPrototypeManager>().Index<JobPrototype>(id);
                Assert.That(job.Whitelisted, Is.True, id);
                Assert.That(job.WhitelistParent?.Id, Is.EqualTo("RuCMWhitelistMedium"));
                Assert.That(whitelist.IsAllowed(ServerSession, id), Is.False, "No synthetic WL is granted by qualifications");
                Assert.That(system.CanTakeJob(ServerSession.UserId, id), Is.True);
                Assert.That(system.Service.CanTakeJob(ServerSession.UserId, id).Allowed, Is.True);
                Assert.That(system.Service.EnabledJobIds(), Does.Not.Contain(id));
                foreach (var late in new[] { false, true })
                {
                    var ev = new PlayerBeforeSpawnEvent(ServerSession, HumanoidCharacterProfile.DefaultWithSpecies(), id, late, EntityUid.Invalid);
                    ent.EventBus.RaiseEvent(EventSource.Local, ev); Assert.That(ev.JobId, Is.EqualTo(id));
                }
                ent.EventBus.RaiseEvent(EventSource.Local, new PlayerSpawnCompleteEvent(body, ServerSession, id, false, true, 1, EntityUid.Invalid,
                    HumanoidCharacterProfile.DefaultWithSpecies()));
            }
            // Permit the separate synthetic whitelist, then verify both admission layers together.
            var field = typeof(JobWhitelistManager).GetField("_whitelists", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var data = (Dictionary<Robust.Shared.Network.NetUserId, HashSet<string>>) field.GetValue(whitelist);
            data[ServerSession.UserId] = new() { "RuCMWhitelistMedium" };
            foreach (var id in jobs)
            {
                Assert.That(whitelist.IsAllowed(ServerSession, id), Is.True);
                Assert.That(system.CanTakeJob(ServerSession.UserId, id), Is.True);
            }
        });
        await Pair.RunTicksSync(15);
        await Server.WaitAssertion(() =>
        {
            var service = Server.System<QualificationSystem>().Service;
            Assert.That(service.Snapshot().Participation, Is.Empty);
            Assert.That(service.Snapshot().Players, Is.Empty);
            var hours = jobs.Select(j => Server.ResolveDependency<IPrototypeManager>().Index<JobPrototype>(j).PlayTimeTracker.Id)
                .Distinct().ToDictionary(t => t, _ => 100.0);
            Assert.That(service.MigrationDryRun(new[] { new MigrationCandidate(ServerSession.UserId, hours) }, DateTimeOffset.UtcNow).Grants, Is.Empty);
        });
    }

    [TestCase(QualificationAction.Complete)]
    [TestCase(QualificationAction.Certify)]
    [TestCase(QualificationAction.ResetRecruit)]
    public async Task SyntheticNeverDisplaysRecruitAndQueuedHumanTrainingIsRejected(QualificationAction action)
    {
        var map = await Pair.CreateTestMap(); var student = await Server.AddDummySession("SyntheticStudent");
        var seed = Seed(); seed.Management.Add(ServerSession.UserId);
        await Configure(seed);
        string error = null; QualificationView view = null;
        await Server.WaitPost(() =>
        {
            var system = Server.System<QualificationSystem>();
            system.Submit(ServerSession, action, new QualificationRequest { Target = student.UserId,
                Qualification = "enlisted", Item = "practice", Reason = "stale human training window" }, result => error = result);
            // Switch to a synthetic job after submission to exercise the authoritative dequeue check.
            var body = Server.EntMan.SpawnEntity("CMMobHuman", map.GridCoords);
            var minds = Server.System<SharedMindSystem>(); var mind = minds.GetOrCreateMind(student.UserId);
            minds.TransferTo(mind, body);
            Server.System<SharedRoleSystem>().MindAddJobRole(mind, jobPrototype: "AU14JobGOVFORAuxSupportSynth");
            Server.PlayerMan.SetAttachedEntity(student, body);
        });
        await Pair.RunTicksSync(15);
        await Server.WaitAssertion(() =>
        {
            var system = Server.System<QualificationSystem>();
            Assert.That(error, Is.EqualTo("synthetic_excluded"));
            Assert.That(system.Service.Snapshot().Players, Is.Empty);
            view = system.View(ServerSession, student.UserId);
            Assert.That(view.TargetSynthetic, Is.True);
        });
        await Client.WaitAssertion(() =>
        {
            using var window = new Content.Client._RuCM.Qualifications.QualificationWindow((_, _) => { });
            // Render the personal dossier rather than management's default training page.
            view.Management = false; view.Instructor = false; view.Officer = false; view.CommandingOfficer = false;
            window.Update(view);
            var text = string.Join(" ", Content.Client._RuCM.Qualifications.QualificationEntrySystem.Descendants(window)
                .OfType<RichTextLabel>().Select(l => l.GetMessage().ToString()));
            var loc = Client.ResolveDependency<ILocalizationManager>();
            Assert.That(text, Does.Contain(loc.GetString("rucm-qualifications-synthetic-excluded")));
            Assert.That(text, Does.Not.Contain(loc.GetString("rucm-qualifications-level-none")));
        });
    }
}

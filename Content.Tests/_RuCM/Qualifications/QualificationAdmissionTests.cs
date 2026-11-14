using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server._RuCM.Qualifications;
using Content.Shared._RuCM.Qualifications;
using NUnit.Framework;

namespace Content.Tests._RuCM.Qualifications;

[TestFixture]
public sealed class QualificationAdmissionTests
{
    private static readonly string[] InstructorJobs =
        { "AU14JobGOVFORadvisor", "AU14JobGOVFORadvisorRMC", "AU14JobGOVFORadvisorUPP" };
    private readonly Guid _player = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private TrainingContext Context => new(_manager, "manager", "", 1, "test", DateTimeOffset.UtcNow);
    private QualificationAuthority Manager => new(Context, true, false, false, false, false);
    private static QualificationStore Seed()
    {
        var seed = new QualificationStore();
        foreach (var id in QualificationRules.Levels.Concat(new[] { "medical" }))
            seed.Definitions[id] = new() { Id = id, Items = new() { new() { Id = "practice" } } };
        foreach (var job in InstructorJobs)
            seed.Roles[job] = new() { JobId = job, MinimumLevel = MilitaryLevel.Sergeant, Govfor = true };
        return seed;
    }
    private async Task<QualificationService> Create(QualificationStore seed = null, MemoryQualificationRepository repository = null)
    {
        var service = new QualificationService(repository ?? new());
        await service.Initialize(seed ?? Seed());
        return service;
    }
    private async Task Accredit(QualificationService service, bool active)
    {
        var context = Context;
        await service.Apply(Manager, QualificationAction.SaveInstructor, new()
        {
            Target = _player, Revision = service.Revision, Reason = "test accreditation",
            Payload = JsonSerializer.Serialize(new InstructorAccreditation(active, InstructorRank.Instructor,
                _manager, context.At, _manager, context.At))
        });
    }
    private async Task Grant(QualificationService service, string qualification) =>
        await service.Apply(Manager, QualificationAction.Grant,
            new() { Target = _player, Qualification = qualification, Reason = "test grant" });

    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public async Task AllInstructorVariantsRequireBothSergeantAndActiveAccreditation(bool sergeant, bool accredited, bool allowed)
    {
        var service = await Create();
        if (sergeant) await Grant(service, "sergeant");
        if (accredited) await Accredit(service, true);
        foreach (var job in InstructorJobs) Assert.That(service.CanTakeJob(_player, job).Allowed, Is.EqualTo(allowed), job);
        if (!accredited) Assert.That(service.Snapshot().Instructors.ContainsKey(_player), Is.False);
    }

    [Test]
    public async Task AccreditationRevocationImmediatelyClosesAllInstructorVariantsAndSurvivesRefresh()
    {
        var service = await Create(); await Grant(service, "sergeant"); await Accredit(service, true);
        Assert.That(InstructorJobs.All(j => service.CanTakeJob(_player, j).Allowed), Is.True);
        await Accredit(service, false);
        await service.Refresh();
        Assert.That(InstructorJobs.All(j => !service.CanTakeJob(_player, j).Allowed), Is.True);
    }

    [Test]
    public async Task DisabledOrLoweredInstructorRoleCannotBypassMandatoryAdmission()
    {
        var seed = Seed();
        foreach (var role in seed.Roles.Values) { role.Enabled = false; role.MinimumLevel = MilitaryLevel.None; }
        var service = await Create(seed); await Accredit(service, true);
        foreach (var job in InstructorJobs)
        {
            Assert.That(service.IsRoleEnabled(job), Is.True);
            Assert.That(service.CanTakeJob(_player, job).Allowed, Is.False);
        }
    }

    [Test]
    public async Task MissingStoredInstructorRoleCannotBypassMandatoryAdmission()
    {
        var service = await Create(new QualificationStore());
        foreach (var job in InstructorJobs) Assert.That(service.CanTakeJob(_player, job).Allowed, Is.False);
    }

    [Test]
    public async Task SyntheticCannotRequireHumanQualificationsOrRecordParticipation()
    {
        var seed = Seed();
        seed.Roles["synth"] = new() { JobId = "synth", Govfor = true, Synthetic = true,
            MinimumLevel = MilitaryLevel.Officer, Professional = new() { "medical" }, Tracker = "synth_tracker" };
        var service = await Create(seed);
        Assert.That(service.CanTakeJob(_player, "synth").Allowed, Is.True);
        Assert.That(service.IsRoleEnabled("synth"), Is.False);
        Assert.That(service.EnabledJobIds(), Does.Not.Contain("synth"));
        await service.RecordParticipation(new(_player, "synth", DateTimeOffset.UtcNow, 1, "test"));
        Assert.That(service.Snapshot().Participation, Is.Empty);
        Assert.That(service.Snapshot().Players, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SyntheticActivityAndPlaytimeNeverMigrateEvenWithExplicitGroupsAndAliases(bool alias)
    {
        var now = DateTimeOffset.UtcNow; var seed = Seed();
        seed.Roles["synth"] = new() { JobId = "synth", Govfor = true, Synthetic = true,
            MinimumLevel = MilitaryLevel.Officer, Professional = new() { "medical" }, Tracker = "synth_tracker" };
        seed.Roles["human"] = new() { JobId = "human", Govfor = true, MinimumLevel = MilitaryLevel.Officer,
            Professional = new() { "medical" }, Tracker = "human_tracker" };
        foreach (var group in new[] { "sergeant", "officer", "medical" }) seed.MigrationGroups[group] = new() { "synth", "human" };
        if (alias) seed.TrackerAliases["synth_tracker"] = "human_tracker";
        // Historical rows written by the previous version must not grant Enlisted either.
        seed.Participation.Add(new(_player, "synth", now, 1, "test"));
        var service = await Create(seed);
        var plan = service.MigrationDryRun(new[] { new MigrationCandidate(_player, new() { ["synth_tracker"] = 100 }) }, now);
        Assert.That(plan.Grants, Is.Empty);
        if (!alias)
        {
            var human = service.MigrationDryRun(new[] { new MigrationCandidate(_player, new() { ["human_tracker"] = 10 }) }, now);
            Assert.That(human.Grants[_player], Does.Contain("officer"));
            Assert.That(human.Grants[_player], Does.Contain("medical"));
        }
    }

    [Test]
    public async Task OldPersistedSyntheticClassificationAndManagementEditsCannotOverridePrototypeFacts()
    {
        var old = Seed(); old.Roles["synth"] = new() { JobId = "synth", Govfor = true, Tracker = "synth_tracker" };
        var repository = new MemoryQualificationRepository(); await repository.Save(old, 0);
        var seed = old.Clone(); seed.Roles["synth"].Synthetic = true;
        var service = await Create(seed, repository);
        foreach (var refresh in new[] { false, true })
        {
            if (refresh) await service.Refresh();
            Assert.That(service.Snapshot().Roles["synth"].Synthetic, Is.True);
            await service.Apply(Manager, QualificationAction.SaveRole, new() { Target = _player, Revision = service.Revision,
                Reason = "try changing runtime facts", Payload = JsonSerializer.Serialize(old.Roles["synth"]) });
            Assert.That(service.Snapshot().Roles["synth"].Synthetic, Is.True);
        }
    }
}

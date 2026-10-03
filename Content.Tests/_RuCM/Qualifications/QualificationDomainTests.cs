using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._RuCM.Qualifications;
using Content.Shared._RuCM.Qualifications;
using NUnit.Framework;

namespace Content.Tests._RuCM.Qualifications;

[TestFixture]
public sealed class QualificationDomainTests
{
    private readonly Guid _manager = Guid.NewGuid();
    private readonly Guid _student = Guid.NewGuid();
    private static TrainingContext Context(Guid actor) => new(actor, "character", "job", 42, "test", DateTimeOffset.UtcNow);
    private QualificationAuthority Manager => new(Context(_manager), true, false, false, false, false);
    private static QualificationStore Seed()
    {
        var s = new QualificationStore();
        foreach (var id in QualificationRules.Levels.Concat(new[] { "medical", "aviation" }))
            s.Definitions[id] = new() { Id = id, Name = id, Items = new() { new() { Id = "required" }, new() { Id = "optional", Required = false }, new() { Id = "disabled", Enabled = false } } };
        s.Roles["medical_job"] = new() { JobId = "medical_job", MinimumLevel = MilitaryLevel.Enlisted, Professional = new() { "medical" }, Tracker = "medical_tracker" };
        s.Roles["sergeant_job"] = new() { JobId = "sergeant_job", MinimumLevel = MilitaryLevel.Sergeant, Tracker = "sergeant_tracker" };
        s.Roles["officer_job"] = new() { JobId = "officer_job", MinimumLevel = MilitaryLevel.Officer, Tracker = "officer_tracker" };
        return s;
    }
    private async Task<QualificationService> Service(IRuCMQualificationRepository repository = null)
    { var service = new QualificationService(repository ?? new MemoryQualificationRepository()); await service.Initialize(Seed()); return service; }
    private QualificationRequest Request(string id, string item = "") => new() { Target = _student, Qualification = id, Item = item, Reason = "test reason" };
    private async Task Grant(QualificationService s, string id) => await s.Apply(Manager, QualificationAction.Grant, Request(id));

    [Test]
    public void DeepCopyKeepsCacheIndependentOfEditableNestedValues()
    {
        var original = Seed(); var context = Context(_manager);
        original.Players[_student] = new() { Player = _student, FirstTrainingAt = context.At,
            Grants = new() { ["medical"] = new("medical", QualificationStatus.Active, 1, new[] { "required" }, context, "test") },
            Progress = new() { ["medical"] = new() { ["required"] = new("medical", "required", context, "note") } } };
        original.Instructors[_manager] = new(true, true, false, new() { "medical" }, _manager, context.At, _manager, context.At);
        original.Suspensions.Add(new() { Target = _student, Qualification = "medical", Initiator = context });
        original.MigrationGroups["medical"] = new() { "medical_job" };
        original.TrackerAliases["old"] = "medical_tracker";
        original.Notes.Add(new(Guid.NewGuid(), _student, context, "private note"));
        original.Audit.Add(new(Guid.NewGuid(), "test", _manager, _student, context.At, 42, "test", "", "", "test", ""));
        original.Participation.Add(new(_student, "medical_job", context.At, 42, "test"));
        var before = System.Text.Json.JsonSerializer.Serialize(original);
        var copy = original.Clone();
        Assert.That(System.Text.Json.JsonSerializer.Serialize(copy), Is.EqualTo(before), "Every persisted field must survive copying");
        copy.Definitions["medical"].Items[0].Name = "changed";
        copy.Roles["medical_job"].Professional.Clear();
        copy.Players[_student].Grants["medical"].RequiredItems[0] = "changed";
        copy.Players[_student].Progress["medical"].Clear();
        copy.Instructors[_manager].Professional.Clear();
        copy.Suspensions[0].Reason = "changed";
        copy.MigrationGroups["medical"].Clear(); copy.TrackerAliases.Clear();
        copy.Notes.Clear(); copy.Audit.Clear(); copy.Participation.Clear();
        copy.Management.Add(_student); copy.OfficerJobs.Add("changed"); copy.CommandingOfficerJobs.Add("changed");
        copy.Migrations.Add("changed"); copy.Players.Clear();
        Assert.That(System.Text.Json.JsonSerializer.Serialize(original), Is.EqualTo(before), "Editing a snapshot cannot change the authoritative cache");
    }

    [TestCase("officer", MilitaryLevel.Sergeant)]
    [TestCase("sergeant", MilitaryLevel.Enlisted)]
    public async Task HigherLevelSatisfiesLower(string grant, MilitaryLevel required)
    { var s = await Service(); await Grant(s, grant); Assert.That(QualificationRules.CanTakeJob(s.GetPlayerTrainingState(_student), new() { MinimumLevel = required }).Allowed, Is.True); }

    [TestCase("sergeant", MilitaryLevel.Enlisted)]
    [TestCase("enlisted", MilitaryLevel.None)]
    public async Task SuspensionInterruptsInheritance(string id, MilitaryLevel expected)
    { var s = await Service(); await Grant(s, "officer"); await s.Apply(Manager, QualificationAction.Suspend, Request(id)); Assert.That(QualificationRules.EffectiveLevel(s.GetPlayerTrainingState(_student)), Is.EqualTo(expected)); }

    [Test]
    public async Task ProfessionalSuspensionOnlyBlocksMatchingRequirements()
    {
        var s = await Service(); await Grant(s, "sergeant"); await Grant(s, "medical");
        await s.Apply(Manager, QualificationAction.Suspend, Request("medical"));
        Assert.That(s.CanTakeJob(_student, "medical_job").Allowed, Is.False);
        Assert.That(s.CanTakeJob(_student, "sergeant_job").Allowed, Is.True);
    }
    [Test]
    public async Task MultipleQualificationsRequireAll()
    { var s = await Service(); await Grant(s, "medical"); Assert.That(QualificationRules.CanTakeJob(s.GetPlayerTrainingState(_student), new() { Professional = new() { "medical", "aviation" } }).Allowed, Is.False); }

    [TestCase(QualificationAction.Grant, "officer")]
    [TestCase(QualificationAction.Complete, "enlisted")]
    [TestCase(QualificationAction.Restore, "enlisted")]
    [TestCase(QualificationAction.SaveInstructor, "enlisted")]
    [TestCase(QualificationAction.SaveRole, "enlisted")]
    [TestCase(QualificationAction.SaveDefinition, "enlisted")]
    [TestCase(QualificationAction.MigrationExecute, "enlisted")]
    public async Task ForgedClientActionsNeverAuthorize(QualificationAction action, string id)
    {
        var s = await Service(); var actor = new QualificationAuthority(Context(_student), false, false, false, false, true);
        var req = Request(id, "required"); req.Payload = "{}";
        if (action == QualificationAction.MigrationExecute)
            Assert.ThrowsAsync<QualificationPermissionException>(() => s.ExecuteMigration(actor, s.MigrationDryRun(Array.Empty<MigrationCandidate>(), DateTimeOffset.UtcNow)));
        else Assert.ThrowsAsync<QualificationPermissionException>(() => s.Apply(actor, action, req));
        Assert.That(s.Snapshot().Audit, Is.Empty);
    }
    [Test]
    public async Task AccreditationDoesNotFollowFromMilitaryRank()
    { var s = await Service(); await Grant(s, "officer"); Assert.That(QualificationRules.CanTrain(s.Snapshot().Instructors.GetValueOrDefault(_student), "enlisted"), Is.False); }
    [TestCase("sergeant")]
    [TestCase("officer")]
    [TestCase("medical")]
    public void EnlistedInstructorCannotTrainUnauthorizedChecklist(string id)
    { Assert.That(QualificationRules.CanTrain(new(true, true, false, new(), _manager, default, _manager, default), id), Is.False); }

    [Test]
    public async Task ChecklistRequiredOptionalAndDisabledRules()
    {
        var s = await Service();
        Assert.ThrowsAsync<QualificationValidationException>(() => s.Apply(Manager, QualificationAction.Certify, Request("enlisted")));
        await s.Apply(Manager, QualificationAction.Complete, Request("enlisted", "required"));
        await s.Apply(Manager, QualificationAction.Certify, Request("enlisted"));
        Assert.That(s.GetPlayerTrainingState(_student).Grants.ContainsKey("enlisted"), Is.True);
    }
    [Test]
    public async Task CompletionIsIdempotentUnderConcurrency()
    { var s = await Service(); await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => s.Apply(Manager, QualificationAction.Complete, Request("enlisted", "required")))); Assert.That(s.Snapshot().Audit.Count, Is.EqualTo(1)); }
    [Test]
    public async Task ExistingGrantSurvivesChecklistVersionEdit()
    {
        var s = await Service(); await Grant(s, "medical"); var old = s.GetPlayerTrainingState(_student).Grants["medical"];
        var definition = s.Snapshot().Definitions["medical"]; definition.Items.Add(new() { Id = "new_required" });
        var req = Request("medical"); req.Revision = s.Snapshot().Revision; req.Payload = System.Text.Json.JsonSerializer.Serialize(definition);
        await s.Apply(Manager, QualificationAction.SaveDefinition, req);
        Assert.That(System.Text.Json.JsonSerializer.Serialize(s.GetPlayerTrainingState(_student).Grants["medical"]), Is.EqualTo(System.Text.Json.JsonSerializer.Serialize(old)));
        Assert.That(s.Snapshot().Definitions["medical"].Version, Is.EqualTo(2));
    }
    [Test]
    public async Task TwoOfficersRequireDistinctAccountsAndCurrentInitiator()
    {
        var s = await Service(); await Grant(s, "enlisted"); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var officer = new QualificationAuthority(Context(first), false, false, true, false, true);
        await s.Apply(officer, QualificationAction.Suspend, Request("enlisted"));
        var pending = s.Snapshot().Suspensions.Single(); var req = new QualificationRequest { Suspension = pending.Id };
        Assert.That(s.GetPlayerTrainingState(_student).Grants["enlisted"].Status, Is.EqualTo(QualificationStatus.Active));
        Assert.ThrowsAsync<QualificationPermissionException>(() => s.Apply(officer with { VerifiedInitiator = first }, QualificationAction.ConfirmSuspension, req));
        var confirmer = new QualificationAuthority(Context(second), false, false, true, false, true);
        Assert.ThrowsAsync<QualificationPermissionException>(() => s.Apply(confirmer, QualificationAction.ConfirmSuspension, req));
        await s.Apply(confirmer with { VerifiedInitiator = first }, QualificationAction.ConfirmSuspension, req);
        Assert.That(s.GetPlayerTrainingState(_student).Grants["enlisted"].Status, Is.EqualTo(QualificationStatus.Suspended));
    }
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task AdminAndCoSuspendWithoutSecond(bool admin, bool co)
    { var s = await Service(); await Grant(s, "enlisted"); await s.Apply(new(Context(Guid.NewGuid()), admin, false, false, co, true), QualificationAction.Suspend, Request("enlisted")); Assert.That(s.Snapshot().Suspensions.Single().Status, Is.EqualTo("active")); }

    [Test]
    public async Task PersistenceAuditAndRestorationSurviveServiceRestart()
    {
        var repository = new MemoryQualificationRepository(); var s = await Service(repository); await Grant(s, "enlisted");
        await s.Apply(Manager, QualificationAction.Suspend, Request("enlisted")); await s.Apply(Manager, QualificationAction.Restore, Request("enlisted"));
        var restarted = await Service(repository);
        Assert.That(restarted.Snapshot().Audit.Count, Is.EqualTo(3));
        Assert.That(restarted.Snapshot().Suspensions.Single().Status, Is.EqualTo("resolved"));
        Assert.That(restarted.CanTakeJob(_student, "medical_job").Allowed, Is.False);
        Assert.That(QualificationRules.EffectiveLevel(restarted.GetPlayerTrainingState(_student)), Is.EqualTo(MilitaryLevel.Enlisted));
    }
    [Test]
    public async Task DryRunDoesNotWriteAndExecuteIsIdempotent()
    {
        var s = await Service(); var before = s.Snapshot().Revision;
        var plan = s.MigrationDryRun(new[] { new MigrationCandidate(_student, new() { ["officer_tracker"] = 10, ["medical_tracker"] = 5 }) }, DateTimeOffset.UtcNow);
        Assert.That(s.Snapshot().Revision, Is.EqualTo(before)); Assert.That(plan.Grants[_student], Does.Contain("officer"));
        await s.ExecuteMigration(Manager, plan); var audits = s.Snapshot().Audit.Count;
        await s.ExecuteMigration(Manager, plan); Assert.That(s.Snapshot().Audit.Count, Is.EqualTo(audits));
    }
    [Test]
    public async Task RoleConfigurationImmediatelyChangesCachedEligibility()
    {
        var s = await Service(); var req = Request(""); req.Revision = s.Snapshot().Revision;
        req.Payload = System.Text.Json.JsonSerializer.Serialize(new RoleRequirement { JobId = "medical_job", Enabled = false });
        Assert.That(s.CanTakeJob(_student, "medical_job").Allowed, Is.False);
        await s.Apply(Manager, QualificationAction.SaveRole, req); Assert.That(s.CanTakeJob(_student, "medical_job").Allowed, Is.True);
    }

    [Test]
    public async Task OfficerDoesNotProvideIndependentCommandingOfficerAdmission()
    {
        var s = await Service(); await Grant(s, "officer");
        Assert.That(QualificationRules.CanTakeJob(s.GetPlayerTrainingState(_student), new() { Professional = new() { "commanding_officer" } }).Allowed, Is.False);
        Assert.That(QualificationRules.CanTrain(new(true, true, true, new() { "commanding_officer" }, _manager, default, _manager, default), "commanding_officer"), Is.False);
    }

    [Test]
    public async Task MigrationRecentParticipationIsJobSpecificAndNeverRestoresSuspensions()
    {
        var seed = Seed(); seed.Roles["medical_job"].Govfor = true;
        var now = DateTimeOffset.UtcNow;
        seed.Participation.Add(new(_student, "medical_job", now.AddDays(-13), 1, "test"));
        var old = Guid.NewGuid(); seed.Participation.Add(new(old, "medical_job", now.AddDays(-15), 2, "test"));
        var outsider = Guid.NewGuid(); seed.Participation.Add(new(outsider, "sergeant_job", now, 3, "test"));
        var s = new QualificationService(new MemoryQualificationRepository()); await s.Initialize(seed);
        var plan = s.MigrationDryRun(new[] { new MigrationCandidate(_student, new()), new(old, new()), new(outsider, new()) }, now);
        Assert.That(plan.Grants[_student], Does.Contain("enlisted")); Assert.That(plan.Grants.ContainsKey(old), Is.False); Assert.That(plan.Grants.ContainsKey(outsider), Is.False);
        await Grant(s, "medical"); await s.Apply(Manager, QualificationAction.Suspend, Request("medical"));
        var migration = s.MigrationDryRun(new[] { new MigrationCandidate(_student, new() { ["medical_tracker"] = 5 }) }, now);
        Assert.That(migration.Grants.GetValueOrDefault(_student) ?? new(), Does.Not.Contain("medical"));
    }

    [Test]
    public async Task MigrationAliasesAndGroupsArePersistentAndConfigurable()
    {
        var s = await Service(); var request = Request(""); request.Revision = s.Snapshot().Revision;
        request.Payload = System.Text.Json.JsonSerializer.Serialize(new QualificationMigrationSettings(
            new() { ["medical"] = new() { "medical_job" } }, new() { ["legacy_medical"] = "medical_tracker" }));
        await s.Apply(Manager, QualificationAction.SaveMigrationSettings, request);
        var plan = s.MigrationDryRun(new[] { new MigrationCandidate(_student, new() { ["legacy_medical"] = 5 }) }, DateTimeOffset.UtcNow);
        Assert.That(plan.Grants[_student], Does.Contain("medical"));
    }

    private sealed class FailingRepository : IRuCMQualificationRepository
    {
        public bool Fail;
        public readonly MemoryQualificationRepository Inner = new();
        public Task<QualificationStore> Load(System.Threading.CancellationToken cancel = default) => Fail ? throw new InvalidOperationException("simulated outage") : Inner.Load(cancel);
        public Task Save(QualificationStore store, long revision, System.Threading.CancellationToken cancel = default) => Fail ? throw new InvalidOperationException("simulated outage") : Inner.Save(store, revision, cancel);
    }
    [Test]
    public async Task FailedWriteDoesNotPublishUncommittedCacheAndRecoveryReloadsLastKnownState()
    {
        var repo = new FailingRepository(); var s = await Service(repo); await Grant(s, "enlisted"); var revision = s.Snapshot().Revision;
        repo.Fail = true;
        Assert.ThrowsAsync<InvalidOperationException>(() => s.Apply(Manager, QualificationAction.Grant, Request("medical")));
        Assert.That(s.Snapshot().Revision, Is.EqualTo(revision)); Assert.That(s.GetPlayerTrainingState(_student).Grants.ContainsKey("medical"), Is.False);
        Assert.That(s.Available, Is.False); Assert.That(QualificationRules.EffectiveLevel(s.GetPlayerTrainingState(_student)), Is.EqualTo(MilitaryLevel.Enlisted));
        repo.Fail = false; await s.Refresh(); Assert.That(s.Available, Is.True);
        await Grant(s, "medical"); Assert.That(s.GetPlayerTrainingState(_student).Grants.ContainsKey("medical"), Is.True);
    }

    [Test]
    public async Task StaleConfigurationCannotOverwriteAnotherManagementChange()
    {
        var s = await Service(); var request = Request(""); request.Revision = s.Snapshot().Revision;
        request.Payload = System.Text.Json.JsonSerializer.Serialize(new RoleRequirement { JobId = "medical_job", Enabled = false });
        await s.Apply(Manager, QualificationAction.SaveRole, request);
        Assert.ThrowsAsync<QualificationConflictException>(() => s.Apply(Manager, QualificationAction.SaveRole, request));
        Assert.That(s.CanTakeJob(_student, "medical_job").Allowed, Is.True);
    }
}

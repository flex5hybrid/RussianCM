using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._RuCM.Qualifications;
using Content.Shared._RuCM.Qualifications;
using NUnit.Framework;

namespace Content.Tests._RuCM.Qualifications;

[TestFixture]
public sealed class QualificationResetTests
{
    private static QualificationAuthority Actor(Guid id, bool manager = false, bool officer = false, int round = 1, Guid? initiator = null) =>
        new(new(id, "name", "job", round, "server", DateTimeOffset.UtcNow), manager, false, officer, false, officer, initiator);
    private static async Task<QualificationService> Prepare(Guid target, Guid manager)
    {
        var context = Actor(manager).Context;
        var seed = new QualificationStore();
        foreach (var id in QualificationRules.Levels.Concat(new[] { "medical", "commanding_officer" }))
            seed.Definitions[id] = new() { Id = id, Items = new() { new() { Id = "practice" } } };
        seed.Players[target] = new() { Player = target };
        foreach (var id in seed.Definitions.Keys)
            seed.Players[target].Grants[id] = new(id, QualificationStatus.Active, 1, new[] { "practice" }, context, "original");
        seed.Players[target].Progress["enlisted"] = new() { ["practice"] = new("enlisted", "practice", context, "original") };
        seed.Instructors[target] = new(true, InstructorRank.SeniorInstructor, manager, context.At, manager, context.At);
        seed.Notes.Add(new(Guid.NewGuid(), target, context, "retain historical note"));
        var service = new QualificationService(new MemoryQualificationRepository()); await service.Initialize(seed); return service;
    }
    [Test]
    public async Task ResetRevokesInheritedAndProfessionalAccessAndAllowsFreshCertification()
    {
        var manager = Guid.NewGuid(); var target = Guid.NewGuid(); var service = await Prepare(target, manager);
        var reason = "Return to basic training";
        await service.Apply(Actor(manager, manager: true), QualificationAction.ResetRecruit, new() { Target = target, Reason = reason });
        var state = service.Snapshot(); var recruit = state.Players[target];
        Assert.Multiple(() => {
            Assert.That(QualificationRules.EffectiveLevel(recruit), Is.EqualTo(MilitaryLevel.None));
            Assert.That(recruit.Grants.Values.All(g => g.Status == QualificationStatus.Revoked), Is.True);
            Assert.That(recruit.Progress, Is.Empty); Assert.That(state.Instructors[target].Active, Is.False);
            Assert.That(state.Notes.Single().Text, Is.EqualTo("retain historical note"));
            Assert.That(state.Audit.Single().OldState, Does.Contain("practice"));
            Assert.That(state.Audit.Single().Reason, Is.EqualTo(reason));
        });
        await service.Apply(Actor(manager, manager: true), QualificationAction.Complete, new() { Target = target, Qualification = "enlisted", Item = "practice", Reason = "new practice" });
        await service.Apply(Actor(manager, manager: true), QualificationAction.Certify, new() { Target = target, Qualification = "enlisted", Reason = "new certification" });
        Assert.That(QualificationRules.EffectiveLevel(service.Snapshot().Players[target]), Is.EqualTo(MilitaryLevel.Enlisted));
    }
    [Test]
    public async Task TwoDifferentCurrentRoundOfficersMustApproveReset()
    {
        var manager = Guid.NewGuid(); var target = Guid.NewGuid(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var service = await Prepare(target, manager);
        await service.Apply(Actor(first, officer: true), QualificationAction.ResetRecruit, new() { Target = target, Reason = "Retraining required" });
        var pending = service.Snapshot().Suspensions.Single();
        Assert.That(pending.RecruitReset, Is.True);
        Assert.That(QualificationRules.EffectiveLevel(service.Snapshot().Players[target]), Is.EqualTo(MilitaryLevel.Officer));
        Assert.ThrowsAsync<QualificationPermissionException>(async () => await service.Apply(Actor(first, officer: true, initiator: first), QualificationAction.ConfirmSuspension, new() { Suspension = pending.Id }));
        Assert.ThrowsAsync<QualificationPermissionException>(async () => await service.Apply(Actor(second, officer: true, round: 2, initiator: first), QualificationAction.ConfirmSuspension, new() { Suspension = pending.Id }));
        Assert.ThrowsAsync<QualificationPermissionException>(async () => await service.Apply(Actor(second, officer: true), QualificationAction.ConfirmSuspension, new() { Suspension = pending.Id }));
        await service.Apply(Actor(second, officer: true, initiator: first), QualificationAction.ConfirmSuspension, new() { Suspension = pending.Id });
        Assert.That(QualificationRules.EffectiveLevel(service.Snapshot().Players[target]), Is.EqualTo(MilitaryLevel.None));
        Assert.That(service.Snapshot().Suspensions.Single().Second.Actor, Is.EqualTo(second));
    }
    [Test]
    public async Task InstructorCannotResetAndReasonAndOtherTargetAreMandatory()
    {
        var manager = Guid.NewGuid(); var target = Guid.NewGuid(); var service = await Prepare(target, manager);
        Assert.ThrowsAsync<QualificationPermissionException>(async () => await service.Apply(Actor(target), QualificationAction.ResetRecruit, new() { Target = manager, Reason = "forged" }));
        Assert.ThrowsAsync<QualificationPermissionException>(async () => await service.Apply(Actor(manager, manager: true), QualificationAction.ResetRecruit, new() { Target = manager, Reason = "self" }));
        Assert.ThrowsAsync<QualificationValidationException>(async () => await service.Apply(Actor(manager, manager: true), QualificationAction.ResetRecruit, new() { Target = target }));
        Assert.That(service.Snapshot().Audit, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task InstructorDefaultUpgradeIsIdempotentAndRetainsExplicitRoleConfiguration(bool configured)
    {
        const string job = "AU14JobGOVFORadvisor";
        var manager = Guid.NewGuid(); var store = new QualificationStore { Revision = 1 };
        store.Management.Add(manager); store.Roles[job] = new() { JobId = job, Enabled = false };
        if (configured) store.Audit.Add(new(Guid.NewGuid(), "SaveRole", manager, null, DateTimeOffset.UtcNow, 1, "server", "", "", "custom settings", "{\"JobId\":\"" + job + "\"}"));
        var repository = new MemoryQualificationRepository(); await repository.Save(store, 0);
        var seed = new QualificationStore(); seed.Roles[job] = new() { JobId = job, MinimumLevel = MilitaryLevel.Sergeant };
        var service = new QualificationService(repository); await service.Initialize(seed);
        Assert.That(service.Snapshot().Roles[job].Enabled, Is.EqualTo(!configured));
        Assert.That(service.Snapshot().Management, Does.Contain(manager));
        Assert.That(service.Snapshot().Migrations, Does.Contain("drill-instructor-defaults-v1"));
        var revision = service.Revision; var another = new QualificationService(repository); await another.Initialize(seed);
        Assert.That(another.Revision, Is.EqualTo(revision));
        Assert.That(another.Snapshot().Audit.Count(a => a.Action == "InstructorRoleDefaults"), Is.EqualTo(1));
    }
}

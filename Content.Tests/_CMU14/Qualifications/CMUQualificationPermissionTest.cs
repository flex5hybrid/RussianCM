using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server._RuCM.Qualifications;
using Content.Shared._RuCM.Qualifications;
using NUnit.Framework;

namespace Content.Tests.CMU14.Qualifications;

[TestFixture]
public sealed class CMUQualificationPermissionTest
{
    [TestCase(InstructorRank.Instructor, false, "enlisted", true)]
    [TestCase(InstructorRank.Instructor, false, "squad_leader", true)]
    [TestCase(InstructorRank.Instructor, false, "sergeant", false)]
    [TestCase(InstructorRank.Instructor, false, "field_engineering", true)]
    [TestCase(InstructorRank.Instructor, false, "officer", false)]
    [TestCase(InstructorRank.Instructor, false, "commanding_officer", false)]
    [TestCase(InstructorRank.SeniorInstructor, false, "officer", true)]
    [TestCase(InstructorRank.SeniorInstructor, false, "medical", true)]
    [TestCase(InstructorRank.SeniorInstructor, false, "commanding_officer", false)]
    [TestCase(null, true, "commanding_officer", true)]
    [TestCase(null, false, "enlisted", false)]
    public async Task AccreditationRestrictsChecklistAndCertification(InstructorRank? rank, bool management, string id, bool allowed)
    {
        var context = new TrainingContext(Guid.NewGuid(), "teacher", "officer", 42, "test", DateTimeOffset.UtcNow);
        var actor = new QualificationAuthority(context, false, false, true, false, !management);
        var seed = new QualificationStore();
        foreach (var level in QualificationRules.Levels)
            seed.Definitions[level] = new() { Id = level, Items = [new() { Id = "practice" }] };
        seed.Definitions.TryAdd(id, new() { Id = id, Items = [new() { Id = "practice" }] });
        if (rank is { } accreditation)
            seed.Instructors[context.Actor] = new(true, accreditation, context.Actor, context.At, context.Actor, context.At);
        if (management)
            seed.Management.Add(context.Actor);

        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(seed);
        var request = new QualificationRequest { Target = Guid.NewGuid(), Qualification = id, Item = "practice", Reason = "Verified" };
        if (!allowed)
        {
            Assert.ThrowsAsync<QualificationPermissionException>(() => service.Apply(actor, QualificationAction.Complete, request));
            Assert.ThrowsAsync<QualificationPermissionException>(() => service.Apply(actor, QualificationAction.Certify, request));
            Assert.That(service.PlayerSnapshot(request.Target), Is.Null);
            return;
        }

        await service.Apply(actor, QualificationAction.Complete, request);
        Assert.ThrowsAsync<QualificationPermissionException>(() => service.Apply(actor, QualificationAction.Certify,
            new() { Target = context.Actor, Qualification = id }));
        await service.Apply(actor, QualificationAction.Certify, request);
        Assert.That(service.PlayerSnapshot(request.Target)?.Grants[id].Status, Is.EqualTo(QualificationStatus.Active));
        if (rank == InstructorRank.SeniorInstructor)
        {
            Assert.ThrowsAsync<QualificationPermissionException>(() => service.Apply(actor, QualificationAction.Grant, request));
            Assert.ThrowsAsync<QualificationPermissionException>(() => service.Apply(actor, QualificationAction.SaveInstructor, request));
        }
    }

    [Test]
    public async Task ManagementAssignsSeniorRankWithoutDelegatingChiefAccess()
    {
        var chief = Guid.NewGuid();
        var teacher = Guid.NewGuid();
        var context = new TrainingContext(chief, "chief", "", 42, "test", DateTimeOffset.UtcNow);
        var seed = new QualificationStore { Management = [chief] };
        var legacy = JsonSerializer.Deserialize<InstructorAccreditation>("""
            {"Active":true,"Enlisted":true,"Sergeant":true,"Professional":["medical"]}
            """)!;
        seed.Instructors[teacher] = legacy;
        Assert.That(QualificationRules.GetInstructorRank(false, legacy), Is.EqualTo(InstructorRank.Instructor));
        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(seed);
        var authority = service.GetAuthority(context, false, false);
        var request = new QualificationRequest { Target = teacher, Revision = service.Revision, Reason = "Update accreditation" };
        request.Payload = JsonSerializer.Serialize(legacy with { Rank = InstructorRank.ChiefInstructor });
        Assert.ThrowsAsync<QualificationValidationException>(() => service.Apply(authority, QualificationAction.SaveInstructor, request));
        request.Payload = JsonSerializer.Serialize(legacy with { Rank = InstructorRank.SeniorInstructor });
        await service.Apply(authority, QualificationAction.SaveInstructor, request);
        Assert.That(service.Snapshot().Instructors[teacher].Rank, Is.EqualTo(InstructorRank.SeniorInstructor));
    }

    [Test]
    public async Task ExistingStoreKeepsTrainingHistoryAndUsesShortenedChecklist()
    {
        const string squadLeader = "AU14JobGOVFORSquadSergeant";
        const string platoonSergeant = "AU14JobGOVFORSectionSergeant";
        var context = new TrainingContext(Guid.NewGuid(), "chief", "", 42, "test", DateTimeOffset.UtcNow);
        var authority = new QualificationAuthority(context, true, false, false, false, false);
        var rookie = Guid.NewGuid();
        var veteran = Guid.NewGuid();
        var oldSeed = new QualificationStore();
        var qualification = oldSeed.Definitions["enlisted"] = new()
        {
            Id = "enlisted",
            Items = [new() { Id = "drill" }, new() { Id = "military_regulations" }, new() { Id = "firearms" }, new() { Id = "first_aid" }],
        };
        oldSeed.Definitions["sergeant"] = new() { Id = "sergeant" };
        oldSeed.Roles[squadLeader] = new() { JobId = squadLeader, MinimumLevel = MilitaryLevel.Sergeant };
        oldSeed.Roles[platoonSergeant] = new() { JobId = platoonSergeant, MinimumLevel = MilitaryLevel.Sergeant };

        var repository = new MemoryQualificationRepository();
        var oldService = new QualificationService(repository);
        await oldService.Initialize(oldSeed);
        foreach (var item in qualification.Items.Where(item => item.Id != "drill"))
            await oldService.Apply(authority, QualificationAction.Complete, new() { Target = rookie, Qualification = "enlisted", Item = item.Id });
        await oldService.Apply(authority, QualificationAction.Grant, new() { Target = veteran, Qualification = "sergeant", Reason = "Verified" });

        var seed = oldSeed.Clone();
        seed.Definitions["enlisted"].Items.Single(item => item.Id == "first_aid").Enabled = false;
        seed.Definitions["squad_leader"] = new() { Id = "squad_leader", Items = [new() { Id = "unit_command" }] };
        seed.Roles[squadLeader].MinimumLevel = MilitaryLevel.Enlisted;
        seed.Roles[squadLeader].Professional.Add("squad_leader");
        var service = new QualificationService(repository);
        await service.Initialize(seed);

        Assert.That(service.Snapshot().Definitions["enlisted"].Version, Is.EqualTo(2));
        Assert.That(service.PlayerSnapshot(rookie)!.Progress["enlisted"].Keys, Does.Contain("first_aid"));
        Assert.That(service.PlayerSnapshot(veteran)!.Grants["enlisted"].RequiredItems, Does.Contain("first_aid"));
        Assert.That(service.CanTakeJob(veteran, squadLeader).Allowed, Is.True);
        await service.Apply(authority, QualificationAction.Complete, new() { Target = rookie, Qualification = "enlisted", Item = "drill" });
        await service.Apply(authority, QualificationAction.Certify, new() { Target = rookie, Qualification = "enlisted" });
        Assert.That(service.CanTakeJob(rookie, squadLeader).Allowed, Is.False);
        await service.Apply(authority, QualificationAction.Grant, new() { Target = rookie, Qualification = "squad_leader", Reason = "Verified" });
        Assert.That(service.CanTakeJob(rookie, squadLeader).Allowed, Is.True);
        Assert.That(service.CanTakeJob(rookie, platoonSergeant).Allowed, Is.False);

        var restarted = new QualificationService(repository);
        await restarted.Initialize(seed);
        Assert.That(restarted.Revision, Is.EqualTo(service.Revision));
    }

    [Test, Combinatorial]
    public async Task AuthorityPreservesPermissionAndParticipationRules(
        [Values] bool administrator,
        [Values] bool management,
        [Values] bool participant,
        [Values("crew", "officer", "co", "both")] string job)
    {
        var player = Guid.NewGuid();
        var verified = Guid.NewGuid();
        var context = new TrainingContext(player, "character", job, 42, "test", DateTimeOffset.UtcNow);
        var seed = new QualificationStore
        {
            OfficerJobs = new() { "officer", "both" },
            CommandingOfficerJobs = new() { "co", "both" },
        };
        if (management)
            seed.Management.Add(player);

        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(seed);
        var authority = service.GetAuthority(context, administrator, participant, verified);

        Assert.Multiple(() =>
        {
            Assert.That(authority.Context, Is.EqualTo(context));
            Assert.That(authority.Administrator, Is.EqualTo(administrator));
            Assert.That(authority.Management, Is.EqualTo(management));
            Assert.That(authority.CurrentParticipant, Is.EqualTo(participant));
            Assert.That(authority.CurrentOfficer, Is.EqualTo(participant && job is "officer" or "both"));
            Assert.That(authority.CurrentCo, Is.EqualTo(participant && job is "co" or "both"));
            Assert.That(authority.VerifiedInitiator, Is.EqualTo(verified));
        });
    }

    [Test]
    public async Task RefreshedPermissionsImmediatelyReplacePreviousAuthority()
    {
        var player = Guid.NewGuid();
        var context = new TrainingContext(player, "character", "command", 42, "test", DateTimeOffset.UtcNow);
        var seed = new QualificationStore
        {
            Management = new() { player },
            OfficerJobs = new() { "command" },
            CommandingOfficerJobs = new() { "command" },
            Instructors = new() { [player] = new(true, InstructorRank.Instructor, player, context.At, player, context.At) },
        };
        var repository = new MemoryQualificationRepository();
        var service = new QualificationService(repository);
        await service.Initialize(seed);

        Assert.That(service.GetAuthority(context, false, true),
            Is.EqualTo(new QualificationAuthority(context, false, true, true, true, true)));
        Assert.That(service.IsActiveInstructor(player), Is.True);

        var next = service.Snapshot();
        next.Management.Clear();
        next.OfficerJobs.Clear();
        next.CommandingOfficerJobs.Clear();
        next.Instructors.Clear();
        next.Revision++;
        await repository.Save(next, service.Revision);
        await service.Refresh();

        Assert.That(service.GetAuthority(context, false, true),
            Is.EqualTo(new QualificationAuthority(context, false, false, false, false, true)));
        Assert.That(service.IsActiveInstructor(player), Is.False);
    }

    [Test]
    public async Task InstructorMutationImmediatelyUpdatesReadAccess()
    {
        var player = Guid.NewGuid();
        var context = new TrainingContext(Guid.NewGuid(), "admin", "", 42, "test", DateTimeOffset.UtcNow);
        var administrator = new QualificationAuthority(context, true, false, false, false, false);
        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(new QualificationStore());
        Assert.That(service.IsActiveInstructor(player), Is.False);

        foreach (var active in new[] { true, false })
        {
            var accreditation = new InstructorAccreditation(active, InstructorRank.Instructor,
                context.Actor, context.At, context.Actor, context.At);
            await service.Apply(administrator, QualificationAction.SaveInstructor, new()
            {
                Target = player,
                Revision = service.Revision,
                Reason = "Update accreditation",
                Payload = JsonSerializer.Serialize(accreditation),
            });
            Assert.That(service.IsActiveInstructor(player), Is.EqualTo(active));
        }
    }
}

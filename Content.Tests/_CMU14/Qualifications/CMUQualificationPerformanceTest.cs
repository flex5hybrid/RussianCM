using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._RuCM.Qualifications;
using Content.Shared._RuCM.Qualifications;
using NUnit.Framework;

namespace Content.Tests.CMU14.Qualifications;

[TestFixture]
public sealed class CMUQualificationPerformanceTest
{
    private static QualificationAuthority Actor(Guid id, bool manager = false, bool officer = false,
        bool co = false, bool drill = false) =>
        new(new(id, "test", drill ? "AU14JobGOVFORadvisor" : "crew", 1, "test", DateTimeOffset.UtcNow),
            manager, false, officer, co, drill);

    private static QualificationStore Seed(Guid viewer, Guid target, Guid unrelated)
    {
        var actor = Actor(viewer).Context;
        var seed = new QualificationStore();
        seed.Definitions["medical"] = new() { Id = "medical", Name = "medical", Items = new() { new() { Id = "practice" } } };
        seed.Roles["medic"] = new() { JobId = "medic", Govfor = true, Professional = new() { "medical" } };
        seed.Management.Add(viewer);
        seed.OfficerJobs.Add("officer");
        seed.CommandingOfficerJobs.Add("co");
        seed.Migrations.Add("history");
        seed.MigrationGroups["medical"] = new() { "medic" };
        seed.TrackerAliases["old"] = "new";
        foreach (var id in new[] { viewer, target, unrelated })
        {
            seed.Players[id] = new()
            {
                Player = id,
                Grants = new() { ["medical"] = new("medical", QualificationStatus.Active, 1, new[] { "practice" }, actor, "test") },
                Progress = new() { ["medical"] = new() { ["practice"] = new("medical", "practice", actor, "test") } },
            };
            seed.Instructors[id] = new(true, InstructorRank.SeniorInstructor, viewer, actor.At, viewer, actor.At);
            seed.Notes.Add(new(Guid.NewGuid(), id, actor, "private"));
            seed.Suspensions.Add(new() { Target = id, Initiator = actor, Status = "active" });
            seed.Suspensions.Add(new() { Target = id, Initiator = actor, Status = "pending" });
            seed.Participation.Add(new(id, "medic", actor.At, 1, "test"));
        }
        for (var i = 0; i < 250; i++)
            seed.Audit.Add(new(Guid.NewGuid(), "test", viewer, i % 3 == 0 ? null : i % 3 == 1 ? target : unrelated,
                actor.At, 1, "test", "", "", "", ""));
        return seed;
    }

    [TestCase("manager")]
    [TestCase("instructor")]
    [TestCase("drill")]
    [TestCase("officer")]
    [TestCase("co")]
    [TestCase("ordinary")]
    public async Task FilteredSnapshotsPreserveRecordPrivacyAndDeepCopyIsolation(string role)
    {
        var viewer = Guid.NewGuid();
        var target = Guid.NewGuid();
        var unrelated = Guid.NewGuid();
        var seed = Seed(viewer, target, unrelated);
        if (role != "manager") seed.Management.Clear();
        if (role != "instructor") seed.Instructors.Remove(viewer);
        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(seed);
        var actor = Actor(viewer, role == "manager", role == "officer", role == "co", role == "drill");
        var before = JsonSerializer.Serialize(service.Snapshot());
        var selected = target;
        var view = service.RecordSnapshot(actor, ref selected);
        var expected = role == "ordinary" ? viewer : target;
        Assert.That(selected, Is.EqualTo(expected));
        Assert.That(view.Players.Keys, Is.EquivalentTo(new[] { expected }));
        Assert.That(view.Participation, Is.Empty);
        Assert.That(view.Notes.Count, Is.EqualTo(role is "manager" or "instructor" or "drill" ? 1 : 0));
        Assert.That(view.Suspensions.All(s => s.Target == expected ||
            role is "manager" or "officer" or "co" && s.Status == "pending"), Is.True);
        if (role == "manager")
        {
            Assert.That(view.Audit, Is.EqualTo(seed.Audit.Where(a => a.Target == target || a.Target == null).TakeLast(100)));
            Assert.That(view.Management, Does.Contain(viewer));
            Assert.That(view.Instructors.Count, Is.EqualTo(seed.Instructors.Count));
        }
        else
        {
            Assert.That(view.Audit, Is.Empty);
            Assert.That(view.Management, Is.Empty);
            Assert.That(view.OfficerJobs, Is.Empty);
            Assert.That(view.Migrations, Is.Empty);
            Assert.That(view.Instructors.Keys, Is.EquivalentTo(role == "instructor" ? new[] { target, viewer } :
                seed.Instructors.ContainsKey(expected) ? new[] { expected } : Array.Empty<Guid>()));
        }
        view.Definitions["medical"].Items[0].Name = "changed";
        view.Roles["medic"].Professional.Clear();
        view.Players[expected].Grants["medical"].RequiredItems[0] = "changed";
        view.Players[expected].Progress["medical"].Clear();
        view.Instructors.Clear();
        foreach (var suspension in view.Suspensions) suspension.Status = "changed";
        view.Management.Clear();
        view.Migrations.Clear();
        view.MigrationGroups.Clear();
        view.TrackerAliases.Clear();
        Assert.That(JsonSerializer.Serialize(service.Snapshot()), Is.EqualTo(before));
    }

    [Test]
    public async Task RecordCopiesDoNotAllocateUnrelatedHistory()
    {
        var viewer = Guid.NewGuid();
        var target = Guid.NewGuid();
        var seed = Seed(viewer, target, Guid.NewGuid());
        seed.Management.Clear();
        seed.Instructors.Remove(viewer);
        for (var i = 0; i < 5000; i++)
        {
            var id = Guid.NewGuid();
            seed.Players[id] = new() { Player = id };
        }
        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(seed);
        var actor = Actor(viewer);
        for (var i = 0; i < 5; i++) { var selected = target; service.RecordSnapshot(actor, ref selected); }
        var timer = new Stopwatch();
        var before = GC.GetAllocatedBytesForCurrentThread();
        timer.Start();
        for (var i = 0; i < 100; i++) { var selected = target; service.RecordSnapshot(actor, ref selected); }
        timer.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Progress.WriteLine($"100 filtered record copies: {allocated:N0} bytes, {timer.Elapsed.TotalMilliseconds:F3} ms.");
        Assert.That(allocated, Is.LessThan(2 * 1024 * 1024));

        before = GC.GetAllocatedBytesForCurrentThread();
        timer.Restart();
        for (var i = 0; i < 100; i++) service.Snapshot();
        timer.Stop();
        var legacy = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Progress.WriteLine($"100 legacy full-store copies: {legacy:N0} bytes, {timer.Elapsed.TotalMilliseconds:F3} ms.");
        Assert.That(legacy, Is.GreaterThan(allocated * 100));
        var fork = typeof(QualificationService).GetMethod("CloneForMutation", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<QualificationStore, QualificationStore>>();
        var source = ReadCache(service);
        for (var i = 0; i < 5; i++) fork(source);
        before = GC.GetAllocatedBytesForCurrentThread();
        timer.Restart();
        for (var i = 0; i < 100; i++) fork(source);
        timer.Stop();
        var mutationCopies = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Progress.WriteLine($"100 mutation drafts with shared untouched histories: {mutationCopies:N0} bytes, {timer.Elapsed.TotalMilliseconds:F3} ms.");
        Assert.That(mutationCopies, Is.LessThan(legacy / 4));
        var own = service.GetPlayerTrainingState(viewer);
        own!.Progress["medical"].Clear();
        Assert.That(service.GetPlayerTrainingState(viewer)!.Progress["medical"], Is.Not.Empty);
    }

    private static QualificationStore ReadCache(QualificationService service) =>
        (QualificationStore) typeof(QualificationService).GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;

    [TestCase(QualificationAction.Complete)]
    [TestCase(QualificationAction.Certify)]
    [TestCase(QualificationAction.Grant)]
    [TestCase(QualificationAction.Revoke)]
    [TestCase(QualificationAction.Restore)]
    [TestCase(QualificationAction.Suspend)]
    [TestCase(QualificationAction.ConfirmSuspension)]
    [TestCase(QualificationAction.ResetRecruit)]
    [TestCase(QualificationAction.CorrectProgress)]
    [TestCase(QualificationAction.SaveDefinition)]
    public async Task FailedChangesCannotMutatePreviouslyPublishedHistories(QualificationAction action)
    {
        var viewer = Guid.NewGuid();
        var target = Guid.NewGuid();
        var seed = Seed(viewer, target, Guid.NewGuid());
        foreach (var id in QualificationRules.Levels) seed.Definitions[id] = new() { Id = id };
        seed.Definitions["medical"].Items.Add(new() { Id = "second", Required = false });
        if (action == QualificationAction.Certify) seed.Players[target].Grants.Remove("medical");
        var repository = new RejectingRepository();
        var service = new QualificationService(repository);
        await service.Initialize(seed);
        repository.Reject = true;
        var published = ReadCache(service);
        var before = JsonSerializer.Serialize(published);
        var actor = Actor(viewer, true);
        var request = new QualificationRequest
        {
            Target = target, Qualification = action == QualificationAction.Grant ? "enlisted" : "medical",
            Item = action == QualificationAction.Complete ? "second" : "practice",
            Reason = "test rollback", Revision = service.Revision,
        };
        if (action == QualificationAction.SaveDefinition)
            request.Payload = JsonSerializer.Serialize(new QualificationDefinition
                { Id = "medical", Items = new() { new() { Id = "new" } } });
        if (action == QualificationAction.ConfirmSuspension)
        {
            // Prepare a real pending request in a separate fresh service.
            seed.Suspensions.Single(s => s.Target == target && s.Status == "pending").Qualification = "medical";
            repository.Reject = false;
            repository.Store = null;
            service = new(repository);
            await service.Initialize(seed);
            repository.Reject = true;
            published = ReadCache(service);
            before = JsonSerializer.Serialize(published);
            var context = new TrainingContext(Guid.NewGuid(), "second officer", "officer", 1, "test", actor.Context.At);
            actor = new(context, false, false, true, false, true, viewer);
            request.Suspension = seed.Suspensions.Single(s => s.Target == target && s.Status == "pending").Id;
            request.Target = Guid.Empty;
        }
        Assert.ThrowsAsync<InvalidOperationException>(() => service.Apply(actor, action, request));
        Assert.That(JsonSerializer.Serialize(published), Is.EqualTo(before), "a failed save must not mutate the old publication");
        Assert.That(JsonSerializer.Serialize(service.Snapshot()), Is.EqualTo(before), "uncommitted histories must not leak into readers");
    }

    private sealed class RejectingRepository : IRuCMQualificationRepository
    {
        public QualificationStore Store;
        public bool Reject;
        public Task<QualificationStore?> Load(CancellationToken cancel = default) => Task.FromResult(Store?.Clone());
        public Task Save(QualificationStore store, long expectedRevision, CancellationToken cancel = default)
        {
            if (Reject) throw new InvalidOperationException("test save failure");
            Store = store.Clone();
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task MetricsAreIndependentAndRefreshAfterMutation()
    {
        var id = Guid.NewGuid();
        var seed = Seed(id, Guid.NewGuid(), Guid.NewGuid());
        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(seed);
        var first = service.Metrics();
        Assert.That(first["started_training"], Is.EqualTo(3));
        first.Clear();
        Assert.That(service.Metrics()["started_training"], Is.EqualTo(3));
        await service.Apply(Actor(id, true), QualificationAction.Complete,
            new() { Target = Guid.NewGuid(), Qualification = "medical", Item = "practice", Reason = "test" });
        Assert.That(service.Metrics()["started_training"], Is.EqualTo(4));
    }

    [Test]
    public async Task JobPollingAndStartingMutationDoNotAllocateTheHistoricalStoreOnTheCaller()
    {
        var id = Guid.NewGuid();
        var target = Guid.NewGuid();
        var seed = Seed(id, target, Guid.NewGuid());
        for (var i = 0; i < 5000; i++)
        {
            var historical = Guid.NewGuid();
            seed.Players[historical] = new() { Player = historical };
        }
        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(seed);
        for (var i = 0; i < 5; i++) service.IsJobAllowed(target, "medic");
        var allowed = true;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++) allowed &= service.IsJobAllowed(target, "medic");
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Progress.WriteLine($"10000 boolean admission checks: {allocated:N0} bytes.");
        Assert.That(allowed, Is.True);
        Assert.That(allocated, Is.LessThan(4096));

        var actor = Actor(id, true);
        var request = new QualificationRequest { Target = target, Qualification = "medical", Reason = "test suspension" };
        before = GC.GetAllocatedBytesForCurrentThread();
        var mutation = service.Apply(actor, QualificationAction.Suspend, request);
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestContext.Progress.WriteLine($"Starting a mutation with 5000 stored players allocated {allocated:N0} bytes on the caller.");
        Assert.That(allocated, Is.LessThan(64 * 1024));
        await mutation;
        Assert.That(service.IsJobAllowed(target, "medic"), Is.False);
        Assert.That(service.CanTakeJob(target, "medic").Missing, Does.Contain("medical"));
    }

    [Test]
    public async Task UnchangedRevisionRetainsCacheAndStorageRecoveryStillRevokesPermissions()
    {
        var id = Guid.NewGuid();
        var repository = new VersionedRepository();
        var service = new QualificationService(repository);
        await service.Initialize(Seed(id, Guid.NewGuid(), Guid.NewGuid()));
        Assert.That(service.GetAuthority(Actor(id).Context, false, false).Management, Is.True);
        repository.Fail = true;
        await service.Refresh();
        Assert.That(service.Available, Is.False);
        repository.Fail = false;
        await service.Refresh();
        Assert.That(service.Available, Is.True);
        Assert.That(repository.FullLoads, Is.EqualTo(1), "ordinary refreshes must not request the entire store");
        repository.Store.Management.Clear();
        repository.Store.Revision++;
        await service.Refresh();
        Assert.That(service.GetAuthority(Actor(id).Context, false, false).Management, Is.False);
    }

    private sealed class VersionedRepository : IRuCMQualificationRepository, ICMUQualificationRefreshRepository
    {
        public QualificationStore Store;
        public bool Fail;
        public int FullLoads;
        public Task<QualificationStore?> Load(CancellationToken cancel = default)
        { FullLoads++; return Task.FromResult(Store?.Clone()); }
        public Task<QualificationStore?> LoadIfChanged(long revision, CancellationToken cancel = default)
        {
            if (Fail) throw new InvalidOperationException("test outage");
            return Task.FromResult(Store.Revision == revision ? null : Store.Clone());
        }
        public Task Save(QualificationStore store, long expectedRevision, CancellationToken cancel = default)
        { Store = store.Clone(); return Task.CompletedTask; }
    }
}

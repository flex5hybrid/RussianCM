using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Content.Server._RuCM.Qualifications;
using Content.Server.Database;
using Content.Shared._RuCM.Qualifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Content.IntegrationTests._RuCM.Qualifications;

[TestFixture, FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class QualificationSqliteTests
{
    private DirectoryInfo _directory;
    private string _gameConnection;
    private string _qualificationConnection;
    private Guid _manager;
    private Guid _student;
    private QualificationAuthority Manager => new(new(_manager, "instructor", "test job", 71, "sqlite-test", DateTimeOffset.UtcNow), true, false, false, false, true);
    private DbContextOptions<SqliteServerDbContext> GameOptions => new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(_gameConnection).Options;

    [SetUp]
    public void Setup()
    {
        _directory = Directory.CreateTempSubdirectory("rucm-qualification-game-");
        var connection = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory.FullName, "preferences.db"), Pooling = false };
        _gameConnection = connection.ConnectionString;
        connection.Mode = SqliteOpenMode.ReadWrite;
        connection.DefaultTimeout = 5;
        _qualificationConnection = connection.ConnectionString;
        _manager = Guid.NewGuid(); _student = Guid.NewGuid();
        // Matches the game's native provider setup on USE_SYSTEM_SQLITE hosts, without opening a file.
        using var providerSetup = new SqliteServerDbContext(GameOptions);
    }

    [TearDown]
    public void Cleanup()
    {
        var root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        Assert.That(_directory.Parent.FullName.TrimEnd(Path.DirectorySeparatorChar), Is.EqualTo(root).IgnoreCase);
        Assert.That(_directory.Name, Does.StartWith("rucm-qualification-game-"));
        // Only files in our uniquely created directory; no recursive deletion or gameplay DB access.
        foreach (var file in _directory.GetFiles()) file.Delete();
        _directory.Delete();
    }

    private static QualificationStore Seed()
    {
        var seed = new QualificationStore();
        foreach (var id in QualificationRules.Levels.Concat(new[] { "medical" }))
            seed.Definitions[id] = new() { Id = id, Name = id, Items = new() { new() { Id = "practice" } } };
        seed.Roles["medical_job"] = new() { JobId = "medical_job", Govfor = true, MinimumLevel = MilitaryLevel.Enlisted, Professional = new() { "medical" }, Tracker = "medical_tracker" };
        seed.Roles["officer_job"] = new() { JobId = "officer_job", Govfor = true, MinimumLevel = MilitaryLevel.Officer, Tracker = "officer_tracker" };
        return seed;
    }

    private QualificationRequest Request(string qualification, string item = "") => new()
    { Target = _student, Qualification = qualification, Item = item, Reason = "observed O'Brien; [font size=99] procedure" };

    private async Task<QualificationService> CreateService()
    {
        await using (var game = new SqliteServerDbContext(GameOptions))
            await game.Database.MigrateAsync();
        var service = new QualificationService(new SqliteQualificationRepository(_qualificationConnection));
        await service.Initialize(Seed());
        Assert.That(service.Available, Is.True);
        return service;
    }

    [Test]
    public async Task ExistingGameFilePreservesGameDataProgressAccreditationSuspensionsAndMigration()
    {
        // Game migration runs before qualification table creation, as in production initialization.
        await using var game = new SqliteServerDbContext(GameOptions);
        await game.Database.MigrateAsync();
        game.Preference.Add(new() { UserId = _student, SelectedCharacterSlot = 0, AdminOOCColor = "#123456" });
        game.PlayTime.Add(new() { PlayerId = _student, Tracker = "medical_tracker", TimeSpent = TimeSpan.FromHours(7) });
        await game.SaveChangesAsync();
        // CMU14: real EF SQLite UUID/TimeSpan encoding must round-trip through the read-only full scanner.
        var candidates = await new CMUHistoricalQualificationScanner(_gameConnection, true).Scan();
        Assert.That(candidates.Single(c => c.Player == _student).TrackerHours["medical_tracker"], Is.EqualTo(7));
        var before = await GameSchemaFingerprint();
        var service = new QualificationService(new SqliteQualificationRepository(_qualificationConnection));
        await service.Initialize(Seed()); Assert.That(service.Available, Is.True);
        var complete = Request("medical", "practice");
        await service.Apply(Manager, QualificationAction.Complete, complete);
        var correction = Request("medical", "practice"); correction.Revision = service.Snapshot().Revision;
        await service.Apply(Manager, QualificationAction.CorrectProgress, correction);
        await using (var connection = new SqliteConnection(_gameConnection))
        {
            await connection.OpenAsync();
            await using var progress = new SqliteCommand("SELECT count(*) FROM rucm_training_record WHERE kind='progress'", connection);
            Assert.That((long) await progress.ExecuteScalarAsync(), Is.Zero, "Corrected progress cannot survive in projections");
        }
        await service.Apply(Manager, QualificationAction.Complete, complete);
        await service.Apply(Manager, QualificationAction.Certify, Request("medical"));
        await service.Apply(Manager, QualificationAction.Grant, Request("enlisted"));
        await service.Apply(Manager, QualificationAction.Note, Request(""));
        await service.Apply(Manager, QualificationAction.Suspend, Request("medical"));
        var instructor = Request(""); instructor.Revision = service.Snapshot().Revision;
        instructor.Payload = JsonSerializer.Serialize(new InstructorAccreditation(true, InstructorRank.SeniorInstructor, _manager, default, _manager, default));
        await service.Apply(Manager, QualificationAction.SaveInstructor, instructor);
        var acl = Request(""); acl.Revision = service.Snapshot().Revision;
        acl.Payload = JsonSerializer.Serialize(new[] { _manager });
        await service.Apply(Manager, QualificationAction.SaveManagement, acl);
        await service.RecordParticipation(new(_student, "medical_job", Manager.Context.At, 71, "sqlite-test"));

        var imported = Guid.NewGuid();
        var plan = service.MigrationDryRun(new[] { new MigrationCandidate(imported, new() { ["officer_tracker"] = 10 }) }, Manager.Context.At);
        var revision = service.Snapshot().Revision;
        Assert.That((await new SqliteQualificationRepository(_qualificationConnection).Load()).Revision, Is.EqualTo(revision));
        await service.ExecuteMigration(Manager, plan);
        var written = JsonSerializer.Serialize(service.Snapshot());
        await service.ExecuteMigration(Manager, plan);
        Assert.That(JsonSerializer.Serialize(service.Snapshot()), Is.EqualTo(written));

        var restarted = new QualificationService(new SqliteQualificationRepository(_qualificationConnection));
        await restarted.Initialize(Seed()); Assert.That(restarted.Available, Is.True);
        Assert.That(JsonSerializer.Serialize(restarted.Snapshot()), Is.EqualTo(written), "Every persisted field must survive a repository/service restart");
        Assert.That(restarted.CanTakeJob(_student, "medical_job").Allowed, Is.False);
        Assert.That(restarted.Snapshot().Instructors[_student].Rank, Is.EqualTo(InstructorRank.SeniorInstructor));
        Assert.That(restarted.Snapshot().Notes.Single().Text, Is.EqualTo(Request("").Reason));
        Assert.That(QualificationRules.EffectiveLevel(restarted.GetPlayerTrainingState(imported)), Is.EqualTo(MilitaryLevel.Officer));
        await restarted.Apply(Manager, QualificationAction.Restore, Request("medical"));
        Assert.That(restarted.CanTakeJob(_student, "medical_job").Allowed, Is.True);
        Assert.That(restarted.Snapshot().Suspensions.Single().Status, Is.EqualTo("resolved"));

        game.ChangeTracker.Clear();
        var preference = await game.Preference.SingleAsync(p => p.UserId == _student);
        Assert.That(preference.AdminOOCColor, Is.EqualTo("#123456"));
        Assert.That((await game.PlayTime.SingleAsync(p => p.PlayerId == _student)).TimeSpent, Is.EqualTo(TimeSpan.FromHours(7)));
        Assert.That(await GameSchemaFingerprint(), Is.EqualTo(before), "Upstream game schema and EF migration history must stay unchanged");
        preference.AdminOOCColor = "#abcdef";
        await game.SaveChangesAsync();
        await using var verify = new SqliteConnection(_gameConnection);
        await verify.OpenAsync();
        await using var tables = new SqliteCommand("SELECT count(*) FROM sqlite_master WHERE type='table' AND name LIKE 'rucm_training_%'", verify);
        Assert.That((long) await tables.ExecuteScalarAsync(), Is.EqualTo(4));
        Assert.That(_directory.GetFiles("*.db").Select(f => f.Name), Is.EqualTo(new[] { "preferences.db" }), "No separate qualification database file");
    }

    [Test]
    public async Task ConcurrentWritersUseAtomicCasAndPreserveTheWinner()
    {
        var service = await CreateService();
        var current = service.Snapshot();
        var first = current.Clone(); first.Revision++; first.Management.Add(_manager);
        var second = current.Clone(); second.Revision++; second.Management.Add(_student);
        var writers = await Task.WhenAll(Commit(first), Commit(second));
        Assert.That(writers.Count(w => w), Is.EqualTo(1));
        var loaded = await new SqliteQualificationRepository(_qualificationConnection).Load();
        Assert.That(JsonSerializer.Serialize(loaded), Is.EqualTo(JsonSerializer.Serialize(writers[0] ? first : second)));
        async Task<bool> Commit(QualificationStore next)
        {
            try { await new SqliteQualificationRepository(_qualificationConnection).Save(next, current.Revision); return true; }
            catch (QualificationConflictException) { return false; }
        }
    }

    [Test]
    public async Task FailedProjectionRollsBackStateAuditAndCacheThenRecovers()
    {
        var service = await CreateService();
        var before = JsonSerializer.Serialize(service.Snapshot());
        await using var connection = new SqliteConnection(_gameConnection); await connection.OpenAsync();
        await using (var failure = new SqliteCommand("CREATE TRIGGER simulate_projection_failure BEFORE INSERT ON rucm_training_record WHEN NEW.kind='training_note' BEGIN SELECT RAISE(ABORT,'simulated write failure'); END", connection))
            await failure.ExecuteNonQueryAsync();
        Assert.ThrowsAsync<SqliteException>(() => service.Apply(Manager, QualificationAction.Note, Request("")));
        Assert.That(service.Available, Is.False);
        Assert.That(JsonSerializer.Serialize(service.Snapshot()), Is.EqualTo(before));
        Assert.That(JsonSerializer.Serialize(await new SqliteQualificationRepository(_qualificationConnection).Load()), Is.EqualTo(before));
        await using (var recover = new SqliteCommand("DROP TRIGGER simulate_projection_failure", connection)) await recover.ExecuteNonQueryAsync();
        await service.Refresh();
        await service.Apply(Manager, QualificationAction.Note, Request(""));
        Assert.That(service.Available, Is.True); Assert.That(service.Snapshot().Notes, Has.Count.EqualTo(1));
    }

    [TestCase("UPDATE rucm_training_audit SET action='forged'")]
    [TestCase("DELETE FROM rucm_training_audit")]
    [TestCase("INSERT OR REPLACE INTO rucm_training_audit SELECT * FROM rucm_training_audit")]
    public async Task AuditRejectsUpdateDeleteAndImplicitReplace(string sql)
    {
        var service = await CreateService();
        await service.Apply(Manager, QualificationAction.Grant, Request("enlisted"));
        await using var connection = new SqliteConnection(_gameConnection); await connection.OpenAsync();
        await using var attack = new SqliteCommand(sql, connection);
        Assert.ThrowsAsync<SqliteException>(async () => await attack.ExecuteNonQueryAsync());
        await service.Apply(Manager, QualificationAction.Grant, Request("medical"));
        await using var audits = new SqliteCommand("SELECT action FROM rucm_training_audit ORDER BY at", connection);
        await using var reader = await audits.ExecuteReaderAsync();
        var actions = new System.Collections.Generic.List<string>();
        while (await reader.ReadAsync()) actions.Add(reader.GetString(0));
        Assert.That(actions, Is.EqualTo(new[] { "Grant", "Grant" }));
    }

    [Test]
    public async Task QualificationStorageNeverCreatesAMissingGameDatabase()
    {
        Assert.ThrowsAsync<SqliteException>(() => new SqliteQualificationRepository(_qualificationConnection).Load());
        Assert.That(_directory.GetFiles(), Is.Empty);
    }

    [Test]
    public async Task FileLockWaitRunsOffTheSimulationThread()
    {
        var service = await CreateService();
        await using var connection = new SqliteConnection(_gameConnection); await connection.OpenAsync();
        using var writer = connection.BeginTransaction(deferred: false);
        var read = new SqliteQualificationRepository(_qualificationConnection).Load();
        // The call returns a pending task instead of synchronously waiting for the writer lock.
        Assert.That(read.IsCompleted, Is.False);
        writer.Rollback();
        Assert.That((await read).Revision, Is.EqualTo(service.Snapshot().Revision));
    }

    private async Task<string> GameSchemaFingerprint()
    {
        await using var connection = new SqliteConnection(_gameConnection); await connection.OpenAsync();
        await using var schema = new SqliteCommand("SELECT type,name,tbl_name,sql FROM sqlite_master WHERE tbl_name NOT LIKE 'rucm_training_%' ORDER BY type,name", connection);
        await using var reader = await schema.ExecuteReaderAsync();
        var rows = new System.Collections.Generic.List<string>();
        while (await reader.ReadAsync()) rows.Add(string.Join("|", Enumerable.Range(0, 4).Select(i => reader.GetValue(i).ToString())));
        await reader.DisposeAsync();
        await using var history = new SqliteCommand("SELECT MigrationId,ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId", connection);
        await using var historyReader = await history.ExecuteReaderAsync();
        while (await historyReader.ReadAsync()) rows.Add(historyReader.GetString(0) + "|" + historyReader.GetString(1));
        return string.Join("\n", rows);
    }
}

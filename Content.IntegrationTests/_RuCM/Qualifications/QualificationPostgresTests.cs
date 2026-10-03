using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server._RuCM.Qualifications;
using Content.Shared._RuCM.Qualifications;
using Npgsql;

namespace Content.IntegrationTests._RuCM.Qualifications;

[TestFixture]
public sealed class QualificationPostgresTests
{
    [Test]
    public async Task PostgreSqlRoundtripAuditImmutabilityMigrationAndConcurrentWriterConflict()
    {
        var connectionString = Environment.GetEnvironmentVariable("RUCM_QUALIFICATIONS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) Assert.Ignore("Set RUCM_QUALIFICATIONS_TEST_CONNECTION to an isolated test database.");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.That(builder.Database, Does.StartWith("rucm_qualifications_test"), "Never run against a gameplay database");
        var fixtureDatabase = "rucm_qualifications_test_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(connectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand("CREATE DATABASE " + fixtureDatabase, admin);
            await create.ExecuteNonQueryAsync();
        }
        builder.Database = fixtureDatabase; builder.Pooling = false;
        var fixtureConnection = builder.ConnectionString;
        try
        {
        connectionString = fixtureConnection;
        var repository = new PostgresQualificationRepository(connectionString);
        var seed = new QualificationStore();
        foreach (var id in QualificationRules.Levels.Concat(new[] { "medical" })) seed.Definitions[id] = new() { Id = id, Name = id, Items = new() { new() { Id = "practice" } } };
        seed.Roles["test_officer"] = new() { JobId = "test_officer", MinimumLevel = MilitaryLevel.Officer, Tracker = "test_officer_tracker" };
        var s = new QualificationService(repository); await s.Initialize(seed);
        Assert.That(s.Available, Is.True);
        var actorId = Guid.NewGuid(); var target = Guid.NewGuid();
        var context = new TrainingContext(actorId, "test actor", "test role", 71, "test", DateTimeOffset.UtcNow);
        var authority = new QualificationAuthority(context, true, false, false, false, false);
        var req = new QualificationRequest { Target = target, Qualification = "medical", Item = "practice", Reason = "test" };
        await s.Apply(authority, QualificationAction.Complete, req);
        await s.Apply(authority, QualificationAction.Certify, req);
        await s.Apply(authority, QualificationAction.Suspend, req);
        var count = s.Snapshot().Audit.Count;
        var restarted = new QualificationService(new PostgresQualificationRepository(connectionString));
        await restarted.Initialize(seed);
        Assert.That(restarted.GetPlayerTrainingState(target).Progress["medical"].ContainsKey("practice"), Is.True);
        Assert.That(restarted.GetPlayerTrainingState(target).Grants["medical"].Status, Is.EqualTo(QualificationStatus.Suspended));
        Assert.That(restarted.Snapshot().Audit.Count, Is.EqualTo(count));
        Assert.That(restarted.Snapshot().Suspensions.Single(x => x.Target == target).Initiator.Character, Is.EqualTo("test actor"));

        // Immutable database audit, not merely a read-only UI.
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("UPDATE rucm_training.audit SET action='forged' WHERE target=@target", connection);
            command.Parameters.AddWithValue("target", target);
            Assert.ThrowsAsync<PostgresException>(async () => await command.ExecuteNonQueryAsync());
        }
        var snapshot = restarted.Snapshot();
        var stale = snapshot.Clone(); stale.Revision++;
        var winning = snapshot.Clone(); winning.Revision++;
        await repository.Save(winning, snapshot.Revision);
        Assert.ThrowsAsync<QualificationConflictException>(() => repository.Save(stale, snapshot.Revision));

        var migrationService = new QualificationService(repository); await migrationService.Initialize(seed);
        var migrationTarget = Guid.NewGuid(); var before = migrationService.Snapshot().Revision;
        var plan = migrationService.MigrationDryRun(new[] { new MigrationCandidate(migrationTarget, new() { ["test_officer_tracker"] = 10 }) }, context.At);
        Assert.That((await repository.Load()).Revision, Is.EqualTo(before));
        await migrationService.ExecuteMigration(authority, plan);
        var migrated = (await repository.Load()).Audit.Count;
        await migrationService.ExecuteMigration(authority, plan);
        Assert.That((await repository.Load()).Audit.Count, Is.EqualTo(migrated));
        var afterRestart = new QualificationService(repository); await afterRestart.Initialize(seed);
        Assert.That(QualificationRules.EffectiveLevel(afterRestart.GetPlayerTrainingState(migrationTarget)), Is.EqualTo(MilitaryLevel.Officer));
        }
        finally
        {
            var adminBuilder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("RUCM_QUALIFICATIONS_TEST_CONNECTION"));
            await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand("DROP DATABASE " + fixtureDatabase, admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}

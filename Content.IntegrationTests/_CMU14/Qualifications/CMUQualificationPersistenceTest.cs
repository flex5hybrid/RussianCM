using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Content.Server._RuCM.Qualifications;
using Content.Server.Database;
using Content.Shared._RuCM.Qualifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Content.IntegrationTests.CMU14.Qualifications;

[TestFixture, NonParallelizable]
public sealed class CMUQualificationPersistenceTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task SelectiveWritesPreserveOtherPlayersRollbackConflictsResetAndMigration(bool postgres)
    {
        var file = Path.Combine(Path.GetTempPath(), $"cmu-qualification-writes-{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder { DataSource = file, Pooling = false }.ConnectionString;
        SqliteServerDbContext? provider = null;
        if (postgres)
        {
            connectionString = Environment.GetEnvironmentVariable("RUCM_QUALIFICATIONS_TEST_CONNECTION")!;
            if (string.IsNullOrWhiteSpace(connectionString)) Assert.Ignore("An isolated PostgreSQL test database is required.");
            var options = new NpgsqlConnectionStringBuilder(connectionString);
            Assert.That(options.Database, Does.StartWith("rucm_qualifications_test"));
            options.Pooling = false;
            connectionString = options.ConnectionString;
        }
        else
            provider = new(new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(connectionString).Options);
        await using DbConnection connection = postgres ? new NpgsqlConnection(connectionString) : new SqliteConnection(connectionString);
        await connection.OpenAsync();
        if (postgres)
            Assert.That(await Scalar("SELECT to_regnamespace('rucm_training') IS NULL"), Is.True, "Refuse existing qualification data.");
        var record = postgres ? "rucm_training.record" : "rucm_training_record";
        var audit = postgres ? "rucm_training.audit" : "rucm_training_audit";
        IRuCMQualificationRepository repository = postgres
            ? new PostgresQualificationRepository(connectionString)
            : new SqliteQualificationRepository(connectionString);
        try
        {
            Assert.That(await repository.Load(), Is.Null);
            var context = new TrainingContext(Guid.NewGuid(), "manager", "test", 71, "test", DateTimeOffset.UtcNow);
            var manager = new QualificationAuthority(context, true, false, false, false, true);
            var student = Guid.NewGuid();
            var imported = Guid.NewGuid();
            var seed = new QualificationStore { Revision = 1 };
            foreach (var id in QualificationRules.Levels.Concat(new[] { "medical" }))
                seed.Definitions[id] = new() { Id = id, Name = id, Items = new() { new() { Id = "practice" } } };
            seed.Roles["officer_job"] = new() { JobId = "officer_job", Govfor = true, MinimumLevel = MilitaryLevel.Officer, Tracker = "officer_tracker" };
            seed.Players[student] = new() { Player = student };
            for (var i = 0; i < 5000; i++)
            {
                var id = Guid.NewGuid();
                seed.Players[id] = new() { Player = id, Progress = new() { ["medical"] = new() { ["practice"] = new("medical", "practice", context, "historical") } } };
                seed.Notes.Add(new(Guid.NewGuid(), id, context, "historical"));
                seed.Audit.Add(new(Guid.NewGuid(), "Historical", context.Actor, id, context.At, 71, "test", "", "", "historical", ""));
            }
            var timer = Stopwatch.StartNew();
            await repository.Save(seed, 0);
            timer.Stop();
            TestContext.Progress.WriteLine($"{(postgres ? "PostgreSQL" : "SQLite")} full bootstrap, 5000 histories: {timer.Elapsed.TotalMilliseconds:F3} ms.");
            var historical = seed.Players.Keys.First(id => id != student);
            var historicalRows = await Scalar($"SELECT count(*) FROM {record} WHERE player='{historical}'");
            var service = new QualificationService(repository);
            await service.Initialize(seed);
            Assert.That(service.Available, Is.True);

            // Fail even BEFORE an upsert of another player's row: unchanged projections must never be written.
            if (postgres)
            {
                await Sql($"""
                    CREATE FUNCTION rucm_training.protect_history() RETURNS trigger LANGUAGE plpgsql AS $$
                    BEGIN
                        IF COALESCE(NEW.player,OLD.player) IS NOT NULL
                           AND COALESCE(NEW.player,OLD.player) NOT IN ('{student}'::uuid,'{imported}'::uuid)
                        THEN RAISE EXCEPTION 'Unrelated history write'; END IF;
                        RETURN COALESCE(NEW,OLD);
                    END; $$;
                    CREATE TRIGGER protect_history BEFORE INSERT OR UPDATE OR DELETE ON {record}
                    FOR EACH ROW EXECUTE FUNCTION rucm_training.protect_history();
                    CREATE FUNCTION rucm_training.protect_old_audit() RETURNS trigger LANGUAGE plpgsql AS $$
                    BEGIN IF EXISTS(SELECT 1 FROM {audit} WHERE id=NEW.id) THEN RAISE EXCEPTION 'Old audit replay'; END IF;
                    RETURN NEW; END; $$;
                    CREATE TRIGGER protect_old_audit BEFORE INSERT ON {audit}
                    FOR EACH ROW EXECUTE FUNCTION rucm_training.protect_old_audit();
                    """);
            }
            else
            {
                foreach (var operation in new[] { "INSERT", "UPDATE", "DELETE" })
                {
                    var row = operation == "DELETE" ? "OLD" : "NEW";
                    await Sql($"""
                        CREATE TRIGGER protect_history_{operation} BEFORE {operation} ON {record}
                        WHEN {row}.player IS NOT NULL AND {row}.player NOT IN ('{student}','{imported}')
                        BEGIN SELECT RAISE(ABORT,'Unrelated history write'); END;
                        """);
                }
            }
            QualificationRequest Request(string qualification = "", string item = "") => new()
            { Target = student, Qualification = qualification, Item = item, Reason = "test" };

            timer.Restart();
            await service.Apply(manager, QualificationAction.Complete, Request("medical", "practice"));
            timer.Stop();
            TestContext.Progress.WriteLine($"{(postgres ? "PostgreSQL" : "SQLite")} selective completion, same histories: {timer.Elapsed.TotalMilliseconds:F3} ms.");
            var correction = Request("medical", "practice");
            correction.Revision = service.Revision;
            await service.Apply(manager, QualificationAction.CorrectProgress, correction);
            Assert.That(Convert.ToInt64(await Scalar($"SELECT count(*) FROM {record} WHERE kind='progress' AND player='{student}'")), Is.Zero);
            Assert.That(Convert.ToInt64(await Scalar($"SELECT count(*) FROM {record} WHERE kind='progress' AND player<>'{student}'")), Is.EqualTo(5000));
            await service.Apply(manager, QualificationAction.Complete, Request("medical", "practice"));
            await service.Apply(manager, QualificationAction.Certify, Request("medical"));
            await service.Apply(manager, QualificationAction.Grant, Request("enlisted"));
            await service.Apply(manager, QualificationAction.Suspend, Request("medical"));
            var accreditation = Request();
            accreditation.Revision = service.Revision;
            accreditation.Payload = JsonSerializer.Serialize(new InstructorAccreditation(true, InstructorRank.SeniorInstructor, context.Actor, context.At, context.Actor, context.At));
            await service.Apply(manager, QualificationAction.SaveInstructor, accreditation);

            var beforeFailure = JsonSerializer.Serialize(service.Snapshot());
            var notesBefore = await Scalar($"SELECT count(*) FROM {record} WHERE kind='training_note'");
            if (postgres)
                await Sql($"""
                    CREATE FUNCTION rucm_training.fail_note() RETURNS trigger LANGUAGE plpgsql AS $$
                    BEGIN IF NEW.kind='training_note' THEN RAISE EXCEPTION 'Injected failure'; END IF; RETURN NEW; END; $$;
                    CREATE TRIGGER fail_note BEFORE INSERT ON {record} FOR EACH ROW EXECUTE FUNCTION rucm_training.fail_note();
                    """);
            else
                await Sql($"CREATE TRIGGER fail_note BEFORE INSERT ON {record} WHEN NEW.kind='training_note' BEGIN SELECT RAISE(ABORT,'Injected failure'); END;");
            Assert.CatchAsync<DbException>(() => service.Apply(manager, QualificationAction.Note, Request()));
            Assert.That(JsonSerializer.Serialize(service.Snapshot()), Is.EqualTo(beforeFailure));
            Assert.That(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(await repository.Load())), JsonNode.Parse(beforeFailure)), Is.True);
            Assert.That(await Scalar($"SELECT count(*) FROM {record} WHERE kind='training_note'"), Is.EqualTo(notesBefore));
            Assert.That(Convert.ToInt64(await Scalar($"SELECT count(*) FROM {audit}")), Is.EqualTo(service.Snapshot().Audit.Count));
            await Sql(postgres ? $"DROP TRIGGER fail_note ON {record}" : "DROP TRIGGER fail_note");
            await service.Apply(manager, QualificationAction.Note, Request());
            Assert.That(service.Available, Is.True);

            var stale = new QualificationService(repository);
            await stale.Initialize(seed);
            await service.Apply(manager, QualificationAction.Note, Request());
            Assert.ThrowsAsync<QualificationConflictException>(() => stale.Apply(manager, QualificationAction.Note, Request()));
            Assert.That(stale.Revision, Is.EqualTo(service.Revision));
            await stale.Apply(manager, QualificationAction.Note, Request());
            await service.Refresh();
            await service.Apply(manager, QualificationAction.ResetRecruit, Request());
            var state = await repository.Load();
            Assert.That(state!.Instructors[student].Active, Is.False);
            Assert.That(state.Suspensions.Single().Status, Is.EqualTo("resolved"));
            Assert.That(state.Players[student].Progress, Is.Empty);
            Assert.That(state.Players[student].Grants.Values.All(g => g.Status == QualificationStatus.Revoked), Is.True);
            Assert.That(Convert.ToInt64(await Scalar($"SELECT count(*) FROM {record} WHERE kind='progress' AND player='{student}'")), Is.Zero);
            Assert.That((string)(await Scalar($"SELECT body FROM {record} WHERE kind='instructor' AND player='{student}'"))!, Does.Contain("\"Active\": false").Or.Contain("\"Active\":false"));

            await service.RecordParticipation(new(student, "officer_job", context.At, 71, "test"));
            var plan = service.MigrationDryRun(new[] { new MigrationCandidate(imported, new() { ["officer_tracker"] = 10 }) }, context.At);
            await service.ExecuteMigration(manager, plan);
            var beforeRestart = JsonSerializer.Serialize(service.Snapshot());
            var restarted = new QualificationService(repository);
            await restarted.Initialize(seed);
            Assert.That(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(restarted.Snapshot())), JsonNode.Parse(beforeRestart)), Is.True);
            Assert.That(restarted.Snapshot().Participation.Count, Is.EqualTo(1));
            Assert.That(QualificationRules.EffectiveLevel(restarted.GetPlayerTrainingState(imported)), Is.EqualTo(MilitaryLevel.Officer));
            Assert.That(await Scalar($"SELECT count(*) FROM {record} WHERE player='{historical}'"), Is.EqualTo(historicalRows));
            Assert.That(Convert.ToInt64(await Scalar($"SELECT count(*) FROM {audit}")), Is.EqualTo(restarted.Snapshot().Audit.Count));

            // Measure a subsequent full write on the same populated store after removing fixture guards.
            if (postgres)
                await Sql($"DROP TRIGGER protect_history ON {record}; DROP TRIGGER protect_old_audit ON {audit}");
            else
                foreach (var operation in new[] { "INSERT", "UPDATE", "DELETE" }) await Sql($"DROP TRIGGER protect_history_{operation}");
            var full = restarted.Snapshot();
            var revision = full.Revision++;
            timer.Restart();
            await repository.Save(full, revision);
            timer.Stop();
            TestContext.Progress.WriteLine($"{(postgres ? "PostgreSQL" : "SQLite")} full repeat write, same histories: {timer.Elapsed.TotalMilliseconds:F3} ms.");
        }
        finally
        {
            if (postgres) await Sql("DROP SCHEMA rucm_training CASCADE");
            await connection.CloseAsync();
            provider?.Dispose();
            if (!postgres)
                foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" }) File.Delete(file + suffix);
        }

        async Task<object?> Scalar(string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            return await command.ExecuteScalarAsync();
        }
        async Task Sql(string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }
}

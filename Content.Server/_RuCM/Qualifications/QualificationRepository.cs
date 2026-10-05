using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.CCVar;
using Npgsql;
using Robust.Shared.Configuration;

namespace Content.Server._RuCM.Qualifications;

public interface IRuCMQualificationRepository
{
    Task<QualificationStore?> Load(CancellationToken cancel = default);
    Task Save(QualificationStore store, long expectedRevision, CancellationToken cancel = default);
    Task<MigrationScan> ScanMigrationCandidates(CancellationToken cancel = default) =>
        Task.FromResult(new MigrationScan(0, Array.Empty<MigrationCandidate>()));
}

/// <summary>Own tables in the game's PostgreSQL database, transactional CAS and append-only audit.</summary>
public sealed class PostgresQualificationRepository : IRuCMQualificationRepository
{
    private readonly string _connection;
    public PostgresQualificationRepository(string connection) { _connection = connection; }

    /// <summary>
    /// Uses the same public database CVars as ServerDbManager.CreatePostgresOptions.
    /// There is deliberately no qualification-specific database or credential override.
    /// SQLite is selected independently by QualificationRepositoryFactory using the game's SQLite file.
    /// </summary>
    public static string? GameConnectionString(IConfigurationManager configuration)
    {
        if (!string.Equals(configuration.GetCVar(CCVars.DatabaseEngine), "postgres", StringComparison.OrdinalIgnoreCase))
            return null;

        return new NpgsqlConnectionStringBuilder
        {
            Host = configuration.GetCVar(CCVars.DatabasePgHost),
            Port = configuration.GetCVar(CCVars.DatabasePgPort),
            Database = configuration.GetCVar(CCVars.DatabasePgDatabase),
            Username = configuration.GetCVar(CCVars.DatabasePgUsername),
            Password = configuration.GetCVar(CCVars.DatabasePgPassword)
        }.ConnectionString;
    }

    public const string Schema = """
        CREATE SCHEMA IF NOT EXISTS rucm_training;
        CREATE TABLE IF NOT EXISTS rucm_training.schema_migration(version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
        CREATE TABLE IF NOT EXISTS rucm_training.state(id integer PRIMARY KEY CHECK(id=1), revision bigint NOT NULL, body jsonb NOT NULL);
        CREATE TABLE IF NOT EXISTS rucm_training.record(kind text NOT NULL, key text NOT NULL, player uuid NULL, body jsonb NOT NULL, PRIMARY KEY(kind,key));
        CREATE INDEX IF NOT EXISTS rucm_training_record_player ON rucm_training.record(player,kind);
        CREATE TABLE IF NOT EXISTS rucm_training.audit(id uuid PRIMARY KEY, actor uuid NOT NULL, target uuid NULL, at timestamptz NOT NULL, action text NOT NULL, body jsonb NOT NULL);
        CREATE INDEX IF NOT EXISTS rucm_training_audit_target ON rucm_training.audit(target,at);
        CREATE OR REPLACE FUNCTION rucm_training.immutable_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Qualification audit is append-only'; END; $$;
        DROP TRIGGER IF EXISTS immutable_audit ON rucm_training.audit;
        CREATE TRIGGER immutable_audit BEFORE UPDATE OR DELETE ON rucm_training.audit FOR EACH ROW EXECUTE FUNCTION rucm_training.immutable_audit();
        INSERT INTO rucm_training.schema_migration(version) VALUES(1) ON CONFLICT DO NOTHING;
        """;

    public async Task<MigrationScan> ScanMigrationCandidates(CancellationToken cancel = default)
    {
        await using var connection = new NpgsqlConnection(_connection);
        await connection.OpenAsync(cancel);

        var accountsScanned = 0;
        await using (var count = new NpgsqlCommand("SELECT COUNT(*) FROM player", connection))
            accountsScanned = Convert.ToInt32(await count.ExecuteScalarAsync(cancel));

        var candidates = new Dictionary<Guid, Dictionary<string, double>>();
        await using var command = new NpgsqlCommand("SELECT player_id, tracker, time_spent FROM play_time", connection);
        await using var reader = await command.ExecuteReaderAsync(cancel);
        while (await reader.ReadAsync(cancel))
        {
            var player = reader.GetGuid(0);
            var tracker = reader.GetString(1);
            var hours = reader.GetFieldValue<TimeSpan>(2).TotalHours;
            if (!candidates.TryGetValue(player, out var trackers))
                candidates[player] = trackers = new();
            trackers[tracker] = trackers.GetValueOrDefault(tracker) + hours;
        }

        return new(accountsScanned, candidates.Select(p => new MigrationCandidate(p.Key, p.Value)).ToArray());
    }

    public async Task<QualificationStore?> Load(CancellationToken cancel = default)
    {
        await using var connection = new NpgsqlConnection(_connection);
        await connection.OpenAsync(cancel);
        await using (var transaction = await connection.BeginTransactionAsync(cancel))
        {
            // Serializes independent schema migrations across concurrent server starts.
            await using (var setup = new NpgsqlCommand("SELECT pg_advisory_xact_lock(71714501); CREATE SCHEMA IF NOT EXISTS rucm_training; CREATE TABLE IF NOT EXISTS rucm_training.schema_migration(version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());", connection, transaction))
                await setup.ExecuteNonQueryAsync(cancel);
            await using var version = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM rucm_training.schema_migration WHERE version=1)", connection, transaction);
            if (await version.ExecuteScalarAsync(cancel) is not true)
            {
                await using var schema = new NpgsqlCommand(Schema, connection, transaction);
                await schema.ExecuteNonQueryAsync(cancel);
            }
            await transaction.CommitAsync(cancel);
        }
        await using var command = new NpgsqlCommand("SELECT body::text FROM rucm_training.state WHERE id=1", connection);
        var json = await command.ExecuteScalarAsync(cancel) as string;
        return json == null ? null : JsonSerializer.Deserialize<QualificationStore>(json);
    }

    public async Task Save(QualificationStore store, long expectedRevision, CancellationToken cancel = default)
    {
        await using var connection = new NpgsqlConnection(_connection);
        await connection.OpenAsync(cancel);
        await using var transaction = await connection.BeginTransactionAsync(cancel);
        await using (var command = new NpgsqlCommand("""
            INSERT INTO rucm_training.state(id,revision,body)
            SELECT 1,@revision,CAST(@body AS jsonb) WHERE @expected=0 OR EXISTS(SELECT 1 FROM rucm_training.state WHERE id=1)
            ON CONFLICT(id) DO UPDATE SET revision=EXCLUDED.revision,body=EXCLUDED.body
            WHERE rucm_training.state.revision=@expected
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("revision", store.Revision);
            command.Parameters.AddWithValue("expected", expectedRevision);
            command.Parameters.AddWithValue("body", JsonSerializer.Serialize(store));
            if (await command.ExecuteNonQueryAsync(cancel) != 1)
                throw new QualificationConflictException();
        }

        // Queryable projections. Unique(kind,key) also enforces stable progress/qualification identity.
        async Task Record(string kind, string key, Guid? player, object value)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO rucm_training.record(kind,key,player,body) VALUES(@kind,@key,@player,CAST(@body AS jsonb))
                ON CONFLICT(kind,key) DO UPDATE SET player=EXCLUDED.player,body=EXCLUDED.body
                """, connection, transaction);
            command.Parameters.AddWithValue("kind", kind);
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("player", NpgsqlTypes.NpgsqlDbType.Uuid, (object?) player ?? DBNull.Value);
            command.Parameters.AddWithValue("body", JsonSerializer.Serialize(value));
            await command.ExecuteNonQueryAsync(cancel);
        }
        foreach (var (id, definition) in store.Definitions) await Record("qualification_definition", id, null, definition);
        foreach (var (id, role) in store.Roles) await Record("role_requirement", id, null, role);
        var progressKeys = new System.Collections.Generic.List<string>();
        foreach (var (id, player) in store.Players)
        {
            await Record("player", id.ToString(), id, player);
            foreach (var (qualification, grant) in player.Grants)
                await Record("grant", JsonSerializer.Serialize(new[] { id.ToString(), qualification }), id, grant);
            foreach (var (qualification, progress) in player.Progress)
            foreach (var (item, completion) in progress)
            {
                var key = JsonSerializer.Serialize(new[] { id.ToString(), qualification, item });
                progressKeys.Add(key); await Record("progress", key, id, completion);
            }
        }
        await using (var obsolete = new NpgsqlCommand("DELETE FROM rucm_training.record WHERE kind='progress' AND NOT(key=ANY(@keys))", connection, transaction))
        { obsolete.Parameters.AddWithValue("keys", progressKeys.ToArray()); await obsolete.ExecuteNonQueryAsync(cancel); }
        foreach (var (id, accreditation) in store.Instructors) await Record("instructor", id.ToString(), id, accreditation);
        foreach (var note in store.Notes) await Record("training_note", note.Id.ToString(), note.Target, note);
        foreach (var suspension in store.Suspensions) await Record("suspension", suspension.Id.ToString(), suspension.Target, suspension);
        await Record("system_setting", "management", null, store.Management);
        await Record("system_setting", "officer_jobs", null, store.OfficerJobs);
        await Record("system_setting", "co_jobs", null, store.CommandingOfficerJobs);
        await Record("system_setting", "migration_groups", null, store.MigrationGroups);
        await Record("system_setting", "tracker_aliases", null, store.TrackerAliases);
        foreach (var key in store.Migrations) await Record("migration_state", key, null, new { Key = key });
        foreach (var audit in store.Audit)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO rucm_training.audit(id,actor,target,at,action,body)
                VALUES(@id,@actor,@target,@at,@action,CAST(@body AS jsonb)) ON CONFLICT(id) DO NOTHING
                """, connection, transaction);
            command.Parameters.AddWithValue("id", audit.Id);
            command.Parameters.AddWithValue("actor", audit.Actor);
            command.Parameters.AddWithValue("target", NpgsqlTypes.NpgsqlDbType.Uuid, (object?) audit.Target ?? DBNull.Value);
            command.Parameters.AddWithValue("at", audit.At.ToUniversalTime());
            command.Parameters.AddWithValue("action", audit.Action);
            command.Parameters.AddWithValue("body", JsonSerializer.Serialize(audit));
            await command.ExecuteNonQueryAsync(cancel);
        }
        await transaction.CommitAsync(cancel);
    }
}

public sealed class QualificationConflictException : Exception;

/// <summary>Explicit test fixture storage, never selected by the production integration.</summary>
public sealed class MemoryQualificationRepository : IRuCMQualificationRepository
{
    private QualificationStore? _store;
    public Task<QualificationStore?> Load(CancellationToken cancel = default)
    { lock (this) return Task.FromResult(_store?.Clone()); }
    public Task Save(QualificationStore store, long expectedRevision, CancellationToken cancel = default)
    {
        lock (this)
        {
            if ((_store?.Revision ?? 0) != expectedRevision) throw new QualificationConflictException();
            _store = store.Clone();
        }
        return Task.CompletedTask;
    }
}

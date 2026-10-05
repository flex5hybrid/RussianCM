using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.CCVar;
using Microsoft.Data.Sqlite;
using Robust.Shared.Configuration;

namespace Content.Server._RuCM.Qualifications;

/// <summary>Own prefixed tables in the existing game SQLite file. No ATTACH or separate file.</summary>
public sealed class SqliteQualificationRepository : IRuCMQualificationRepository
{
    private readonly string _connection;
    public SqliteQualificationRepository(string connection) { _connection = connection; }

    public static string? GameConnectionString(IConfigurationManager configuration, string? userDataRoot)
    {
        if (!string.Equals(configuration.GetCVar(CCVars.DatabaseEngine), "sqlite", StringComparison.OrdinalIgnoreCase) || userDataRoot == null)
            return null;

        return new SqliteConnectionStringBuilder
        {
            // Same path resolution as ServerDbManager.SetupSqlite, including rooted custom paths.
            DataSource = Path.Combine(userDataRoot, configuration.GetCVar(CCVars.DatabaseSqliteDbPath)),
            Mode = SqliteOpenMode.ReadWrite, // Only the game creates the database file.
            Pooling = false,
            DefaultTimeout = 5
        }.ConnectionString;
    }

    public const string Schema = """
        CREATE TABLE IF NOT EXISTS rucm_training_schema_migration(version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
        CREATE TABLE IF NOT EXISTS rucm_training_state(id INTEGER PRIMARY KEY CHECK(id=1), revision INTEGER NOT NULL, body TEXT NOT NULL CHECK(json_valid(body)));
        CREATE TABLE IF NOT EXISTS rucm_training_record(kind TEXT NOT NULL, key TEXT NOT NULL, player TEXT NULL, body TEXT NOT NULL CHECK(json_valid(body)), PRIMARY KEY(kind,key));
        CREATE INDEX IF NOT EXISTS rucm_training_record_player ON rucm_training_record(player,kind);
        CREATE TABLE IF NOT EXISTS rucm_training_audit(id TEXT PRIMARY KEY NOT NULL, actor TEXT NOT NULL, target TEXT NULL, at TEXT NOT NULL, action TEXT NOT NULL, body TEXT NOT NULL CHECK(json_valid(body)));
        CREATE INDEX IF NOT EXISTS rucm_training_audit_target ON rucm_training_audit(target,at);
        CREATE TRIGGER IF NOT EXISTS rucm_training_audit_no_update BEFORE UPDATE ON rucm_training_audit BEGIN SELECT RAISE(ABORT,'Qualification audit is append-only'); END;
        CREATE TRIGGER IF NOT EXISTS rucm_training_audit_no_delete BEFORE DELETE ON rucm_training_audit BEGIN SELECT RAISE(ABORT,'Qualification audit is append-only'); END;
        CREATE TRIGGER IF NOT EXISTS rucm_training_audit_no_replace BEFORE INSERT ON rucm_training_audit WHEN EXISTS(SELECT 1 FROM rucm_training_audit WHERE id=NEW.id) BEGIN SELECT RAISE(ABORT,'Qualification audit is append-only'); END;
        INSERT INTO rucm_training_schema_migration(version) VALUES(1) ON CONFLICT DO NOTHING;
        """;

    // Microsoft.Data.Sqlite performs synchronous I/O even through its async APIs. Never wait for
    // a file lock or run a large transaction on the simulation thread.
    public Task<QualificationStore?> Load(CancellationToken cancel = default) => Task.Run(() => LoadCore(cancel), cancel);
    public Task Save(QualificationStore store, long expectedRevision, CancellationToken cancel = default) =>
        Task.Run(() => SaveCore(store, expectedRevision, cancel), cancel);
    public Task<MigrationScan> ScanMigrationCandidates(CancellationToken cancel = default) =>
        Task.Run(() => ScanMigrationCandidatesCore(cancel), cancel);

    private MigrationScan ScanMigrationCandidatesCore(CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        using var connection = new SqliteConnection(_connection);
        connection.Open();

        var accountsScanned = 0;
        using (var count = new SqliteCommand("SELECT COUNT(*) FROM player", connection))
            accountsScanned = Convert.ToInt32(count.ExecuteScalar(), CultureInfo.InvariantCulture);

        var candidates = new Dictionary<Guid, Dictionary<string, double>>();
        using var command = new SqliteCommand("SELECT player_id, tracker, time_spent FROM play_time", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cancel.ThrowIfCancellationRequested();
            if (!Guid.TryParse(reader.GetString(0), out var player))
                continue;
            var tracker = reader.GetString(1);
            if (!TimeSpan.TryParse(reader.GetString(2), CultureInfo.InvariantCulture, out var spent))
                continue;
            if (!candidates.TryGetValue(player, out var trackers))
                candidates[player] = trackers = new();
            trackers[tracker] = trackers.GetValueOrDefault(tracker) + spent.TotalHours;
        }

        return new(accountsScanned, candidates.Select(p => new MigrationCandidate(p.Key, p.Value)).ToArray());
    }

    private QualificationStore? LoadCore(CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        using var connection = new SqliteConnection(_connection);
        connection.Open();
        // BEGIN IMMEDIATE serializes migrations/transactions across processes before any reads.
        using var transaction = connection.BeginTransaction(deferred: false);
        using (var setup = Command(connection, transaction, "CREATE TABLE IF NOT EXISTS rucm_training_schema_migration(version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP)"))
            setup.ExecuteNonQuery();
        using (var version = Command(connection, transaction, "SELECT EXISTS(SELECT 1 FROM rucm_training_schema_migration WHERE version=1)"))
        {
            if (Convert.ToInt64(version.ExecuteScalar()) == 0)
            {
                using var schema = Command(connection, transaction, Schema);
                schema.ExecuteNonQuery();
            }
        }
        cancel.ThrowIfCancellationRequested();
        using var read = Command(connection, transaction, "SELECT body FROM rucm_training_state WHERE id=1");
        var json = read.ExecuteScalar() as string;
        var store = json == null ? null : JsonSerializer.Deserialize<QualificationStore>(json);
        transaction.Commit();
        return store;
    }

    private void SaveCore(QualificationStore store, long expectedRevision, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        var body = JsonSerializer.Serialize(store);
        using var connection = new SqliteConnection(_connection);
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        using (var state = Command(connection, transaction, """
            INSERT INTO rucm_training_state(id,revision,body)
            SELECT 1,@revision,@body WHERE @expected=0 OR EXISTS(SELECT 1 FROM rucm_training_state WHERE id=1)
            ON CONFLICT(id) DO UPDATE SET revision=excluded.revision,body=excluded.body
            WHERE rucm_training_state.revision=@expected
            """))
        {
            state.Parameters.AddWithValue("revision", store.Revision);
            state.Parameters.AddWithValue("expected", expectedRevision);
            state.Parameters.AddWithValue("body", body);
            if (state.ExecuteNonQuery() != 1) throw new QualificationConflictException();
        }

        // Rebuild only current progress within the same transaction. This also handles removals
        // without a variable-length IN list exceeding SQLite's parameter limit.
        using (var obsolete = Command(connection, transaction, "DELETE FROM rucm_training_record WHERE kind='progress'"))
            obsolete.ExecuteNonQuery();
        using var record = Command(connection, transaction, """
            INSERT INTO rucm_training_record(kind,key,player,body) VALUES(@kind,@key,@player,@body)
            ON CONFLICT(kind,key) DO UPDATE SET player=excluded.player,body=excluded.body
            """);
        var kindParameter = record.Parameters.Add("kind", SqliteType.Text);
        var keyParameter = record.Parameters.Add("key", SqliteType.Text);
        var playerParameter = record.Parameters.Add("player", SqliteType.Text);
        var bodyParameter = record.Parameters.Add("body", SqliteType.Text);
        void Record(string kind, string key, Guid? player, object value)
        {
            cancel.ThrowIfCancellationRequested();
            kindParameter.Value = kind;
            keyParameter.Value = key;
            playerParameter.Value = player is { } id ? id.ToString() : DBNull.Value;
            bodyParameter.Value = JsonSerializer.Serialize(value);
            record.ExecuteNonQuery();
        }
        foreach (var (id, definition) in store.Definitions) Record("qualification_definition", id, null, definition);
        foreach (var (id, role) in store.Roles) Record("role_requirement", id, null, role);
        foreach (var (id, player) in store.Players)
        {
            Record("player", id.ToString(), id, player);
            foreach (var (qualification, grant) in player.Grants)
                Record("grant", JsonSerializer.Serialize(new[] { id.ToString(), qualification }), id, grant);
            foreach (var (qualification, progress) in player.Progress)
            foreach (var (item, completion) in progress)
                Record("progress", JsonSerializer.Serialize(new[] { id.ToString(), qualification, item }), id, completion);
        }
        foreach (var (id, accreditation) in store.Instructors) Record("instructor", id.ToString(), id, accreditation);
        foreach (var note in store.Notes) Record("training_note", note.Id.ToString(), note.Target, note);
        foreach (var suspension in store.Suspensions) Record("suspension", suspension.Id.ToString(), suspension.Target, suspension);
        Record("system_setting", "management", null, store.Management);
        Record("system_setting", "officer_jobs", null, store.OfficerJobs);
        Record("system_setting", "co_jobs", null, store.CommandingOfficerJobs);
        Record("system_setting", "migration_groups", null, store.MigrationGroups);
        Record("system_setting", "tracker_aliases", null, store.TrackerAliases);
        foreach (var key in store.Migrations) Record("migration_state", key, null, new { Key = key });
        foreach (var entry in store.Audit)
        {
            cancel.ThrowIfCancellationRequested();
            // Skip existing IDs before INSERT so the duplicate-ID trigger can also reject
            // INSERT OR REPLACE, whose implicit deletes bypass ordinary delete triggers.
            using var audit = Command(connection, transaction, """
                INSERT INTO rucm_training_audit(id,actor,target,at,action,body)
                SELECT @id,@actor,@target,@at,@action,@body
                WHERE NOT EXISTS(SELECT 1 FROM rucm_training_audit WHERE id=@id)
                """);
            audit.Parameters.AddWithValue("id", entry.Id.ToString());
            audit.Parameters.AddWithValue("actor", entry.Actor.ToString());
            audit.Parameters.AddWithValue("target", entry.Target is { } target ? target.ToString() : DBNull.Value);
            audit.Parameters.AddWithValue("at", entry.At.ToUniversalTime().ToString("O"));
            audit.Parameters.AddWithValue("action", entry.Action);
            audit.Parameters.AddWithValue("body", JsonSerializer.Serialize(entry));
            audit.ExecuteNonQuery();
        }
        cancel.ThrowIfCancellationRequested();
        transaction.Commit();
    }

    private static SqliteCommand Command(SqliteConnection connection, SqliteTransaction transaction, string sql) =>
        new(sql, connection, transaction);
}

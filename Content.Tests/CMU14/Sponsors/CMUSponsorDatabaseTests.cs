using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CMU14.Sponsors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;

namespace Content.Tests.CMU14.Sponsors;

[TestFixture]
public sealed class CMUSponsorDatabaseTests
{
    [Test]
    public async Task CosmeticPreferencesSurviveSubscriptionDeletionAndReconnection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(connection).Options;
        var user = Guid.NewGuid();
        var choices = new CMUSponsorSettings("frost", "Home", "branch", "16", "red", "skull", "A memento", false);
        await using (var db = new SqliteServerDbContext(options))
        {
            await db.Database.MigrateAsync();
            db.Player.Add(new Player
            {
                UserId = user, FirstSeenTime = DateTime.UtcNow, LastSeenTime = DateTime.UtcNow,
                LastSeenUserName = "sponsor", LastSeenAddress = IPAddress.Loopback,
            });
            var tier = new RMCPatronTier { Name = "test", Priority = 1, DiscordRole = 1 };
            db.RMCPatronTiers.Add(tier);
            db.RMCPatrons.Add(new RMCPatron { PlayerId = user, Tier = tier });
            db.CMUSponsorPreferences.Add(new CMUSponsorPreferences
            {
                PlayerId = user, Settings = JsonSerializer.Serialize(choices), ApprovedFigurineDescription = "A memento",
            });
            await db.SaveChangesAsync();
            db.RMCPatrons.Remove(await db.RMCPatrons.SingleAsync());
            await db.SaveChangesAsync();
        }
        // A fresh context simulates reconnecting rather than reading EF's tracked objects.
        await using var reconnected = new SqliteServerDbContext(options);
        var row = await reconnected.CMUSponsorPreferences.SingleAsync();
        Assert.That(JsonSerializer.Deserialize<CMUSponsorSettings>(row.Settings), Is.EqualTo(choices));
        Assert.That(row.ApprovedFigurineDescription, Is.EqualTo("A memento"));
        Assert.That(await reconnected.RMCPatrons.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task MigrationPreservesPublishedLobbyMessages()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(connection).Options;
        await using var db = new SqliteServerDbContext(options);
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.FindIndex(migrations, m => m.EndsWith("_CMUSponsorPreferences"));
        Assert.That(index, Is.GreaterThan(0));
        await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
        var user = Guid.NewGuid();
        db.Player.Add(new Player
        {
            UserId = user, FirstSeenTime = DateTime.UtcNow, LastSeenTime = DateTime.UtcNow,
            LastSeenUserName = "legacy-sponsor", LastSeenAddress = IPAddress.Loopback,
        });
        var tier = new RMCPatronTier { Name = "legacy", Priority = 3, DiscordRole = 2 };
        db.RMCPatrons.Add(new RMCPatron { PlayerId = user, Tier = tier });
        await db.SaveChangesAsync();
        // Existing published messages are retained when installing the moderation gate.
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO rmc_patron_lobby_messages (patron_id,message) VALUES ($user,'Already published');";
        command.Parameters.AddWithValue("$user", user);
        await command.ExecuteNonQueryAsync();
        await db.GetService<IMigrator>().MigrateAsync();
        command.CommandText = "SELECT approved FROM rmc_patron_lobby_messages WHERE message='Already published';";
        Assert.That(Convert.ToInt32(await command.ExecuteScalarAsync()), Is.EqualTo(1));
    }

    [Test]
    public void BothDatabaseModelsMatchTheirMigrationSnapshots()
    {
        using var sqlite = new SqliteServerDbContext(new DbContextOptionsBuilder<SqliteServerDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        using var postgres = new PostgresServerDbContext(new DbContextOptionsBuilder<PostgresServerDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        Assert.Multiple(() =>
        {
            Assert.That(sqlite.Database.HasPendingModelChanges(), Is.False);
            Assert.That(postgres.Database.HasPendingModelChanges(), Is.False);
            var script = postgres.GetService<IMigrator>().GenerateScript();
            Assert.That(script, Does.Contain("cmu_sponsor_preferences"));
            Assert.That(script, Does.Contain("UPDATE rmc_patron_lobby_messages SET approved = TRUE"));
        });
    }
}

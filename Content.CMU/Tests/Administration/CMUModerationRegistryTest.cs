using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.CMU14.Administration.ModerationRegistry;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.CMU14.Administration;

[TestFixture]
public sealed class CMUModerationRegistryTest
{
    private const string WebhookUrl = "https://discord.com/api/webhooks/1234567890/test_token";

    [Test]
    public async Task RegistersSanctionsTransactionallyAndRetriesWithoutLosingRecords()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        var options = new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(connection).Options;
        await using var context = new SqliteServerDbContext(options);
        await connection.OpenAsync();
        var config = new Mock<IConfigurationManager>();
        config.Setup(c => c.GetCVar(CMUModerationRegistryCVars.DiscordWebhook)).Returns(WebhookUrl);
        var db = new ServerDbSqlite(() => options, true, config.Object, true, Mock.Of<ISawmill>(),
            Mock.Of<ISerializationManager>(), false);

        var target = new NetUserId(Guid.NewGuid());
        var admin = new NetUserId(Guid.NewGuid());
        var time = DateTime.UtcNow;
        await db.UpdatePlayerRecord(target, "Player", IPAddress.Loopback, null);
        await db.UpdatePlayerRecord(admin, "Moderator", IPAddress.Loopback, null);
        var warning = new AdminMessage
        {
            PlayerUserId = target.UserId,
            CreatedById = admin.UserId,
            CreatedAt = time,
            LastEditedAt = time,
            Message = "warning",
        };

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER reject_moderation_record BEFORE INSERT ON cmu_moderation_outbox
            BEGIN SELECT RAISE(ABORT, 'test rollback'); END
            """);
        Assert.That(async () => await db.AddAdminMessage(warning), Throws.TypeOf<DbUpdateException>());
        Assert.That(await context.AdminMessages.CountAsync(), Is.Zero);
        Assert.That(await context.Set<CMUModerationOutbox>().CountAsync(), Is.Zero);
        await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_moderation_record");

        await db.AddAdminMessage(warning);
        await db.AddAdminNote(new AdminNote
        {
            PlayerUserId = target.UserId,
            CreatedById = admin.UserId,
            CreatedAt = time,
            LastEditedAt = time,
            Message = "visible note",
        });
        await db.AddAdminNote(new AdminNote
        {
            PlayerUserId = target.UserId,
            CreatedAt = time,
            LastEditedAt = time,
            Message = "private note",
            Secret = true,
        });
        await db.AddBanAsync(new BanDef(null, BanType.Server, [], [(IPAddress.Loopback, 32)], [], time, null,
            [], TimeSpan.Zero, "server ban", NoteSeverity.High, admin, null));
        await db.AddBanAsync(new BanDef(null, BanType.Role, [target], [], [], time, time.AddHours(1),
            [], TimeSpan.Zero, "role ban", NoteSeverity.Medium, admin, null, roles: [new("Job", "TestJob")]));
        await db.AddChatBan(null, target, (IPAddress.Loopback, 32), null, TimeSpan.FromHours(1),
            ChatType.Ooc, admin, "chat ban");

        var queued = await context.Set<CMUModerationOutbox>().AsNoTracking().ToListAsync();
        var records = queued.Select(entry => JsonSerializer.Deserialize<CMUModerationRecord>(entry.Payload)!).ToList();
        Assert.That(records.Select(record => record.Kind), Is.EquivalentTo(new[]
        {
            CMUModerationKind.Warning, CMUModerationKind.Warning, CMUModerationKind.ServerBan,
            CMUModerationKind.RoleBan, CMUModerationKind.ChatBan,
        }));
        Assert.That(records.Single(record => record.Kind == CMUModerationKind.RoleBan).Scope, Is.EqualTo("TestJob"));
        Assert.That(queued.Select(entry => entry.Payload), Has.None.Contains("127.0.0.1"));
        Assert.That(await db.ClaimModerationRecord("98765"), Is.Null);

        var delivery = (await db.ClaimModerationRecord("1234567890"))!;
        await db.CompleteModerationRecord(delivery, TimeSpan.FromMinutes(1));
        for (var i = 0; i < 4; i++)
        {
            var next = (await db.ClaimModerationRecord("1234567890"))!;
            Assert.That(next.Id, Is.Not.EqualTo(delivery.Id));
            await db.CompleteModerationRecord(next, null);
        }

        Assert.That(await db.ClaimModerationRecord("1234567890"), Is.Null);
        Assert.That(await context.Set<CMUModerationOutbox>().Select(entry => entry.Id).SingleAsync(), Is.EqualTo(delivery.Id));
    }

    [Test]
    public void FormatsLongReasonsAndDisablesMentions()
    {
        var reason = "@everyone **жирный** " + new string('я', 4096);
        var payload = CMUModerationEmbed.Build(new CMUModerationRecord(
            CMUModerationKind.Warning, [], null, reason, DateTime.UtcNow, null, []), Guid.Empty, new System.Collections.Generic.Dictionary<Guid, string>(), "RuCM");
        var embed = payload.Embeds!.Single();
        Assert.That(payload.AllowedMentions.Parse, Is.Empty);
        Assert.That(embed.Title, Is.EqualTo("Предупреждение"));
        Assert.That(embed.Description, Does.Contain(@"\*\*жирный\*\*").And.EndsWith("…"));
        Assert.That(embed.Description.Length, Is.LessThanOrEqualTo(4096));
    }
}

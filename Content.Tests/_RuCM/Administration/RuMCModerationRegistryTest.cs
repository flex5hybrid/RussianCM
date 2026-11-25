using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server._RuCM.Administration.ModerationRegistry;
using Content.Server.Database;
using Content.Server._RuCM.Discord;
using Content.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Tests._RuCM.Administration;

[TestFixture]
public sealed class RuMCModerationRegistryTest : ContentUnitTest
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
        config.Setup(c => c.GetCVar(RuMCModerationRegistryCVars.Webhook)).Returns(WebhookUrl);
        var db = new ServerDbSqlite(() => options, true, config.Object, true, Mock.Of<ISawmill>(),
            Mock.Of<ISerializationManager>(), false);

        var target = new NetUserId(Guid.NewGuid());
        var admin = new NetUserId(Guid.NewGuid());
        var time = DateTime.UtcNow;
        await db.UpdatePlayerRecord(target, "Player", IPAddress.Loopback, null);
        await db.UpdatePlayerRecord(admin, "Moderator", IPAddress.Loopback, null);
        const ulong discordId = 123456789012345678;
        context.RMCDiscordAccounts.Add(new RMCDiscordAccount { Id = discordId });
        context.RMCLinkedAccounts.Add(new RMCLinkedAccount { PlayerId = target.UserId, DiscordId = discordId });
        await context.SaveChangesAsync();

        var missing = Guid.NewGuid();
        var accounts = await db.GetModerationAccounts([target.UserId, admin.UserId, missing]);
        Assert.That(accounts[target.UserId], Is.EqualTo(new RuMCModerationAccount("Player", discordId)));
        Assert.That(accounts[admin.UserId], Is.EqualTo(new RuMCModerationAccount("Moderator", null)));
        Assert.That(accounts.ContainsKey(missing), Is.False);
        var warning = new AdminMessage
        {
            PlayerUserId = target.UserId,
            CreatedById = admin.UserId,
            CreatedAt = time,
            LastEditedAt = time,
            Message = "warning",
        };

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER reject_moderation_record BEFORE INSERT ON rumc_moderation_outbox
            BEGIN SELECT RAISE(ABORT, 'test rollback'); END
            """);
        Assert.That(async () => await db.AddAdminMessage(warning), Throws.TypeOf<DbUpdateException>());
        Assert.That(await context.AdminMessages.CountAsync(), Is.Zero);
        Assert.That(await context.Set<RuMCModerationOutbox>().CountAsync(), Is.Zero);
        Assert.That(db.ModerationQueueVersion, Is.Zero);
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

        Assert.That(db.ModerationQueueVersion, Is.EqualTo(5));
        var queued = await context.Set<RuMCModerationOutbox>().AsNoTracking().ToListAsync();
        var records = queued.Select(entry => JsonSerializer.Deserialize<RuMCModerationRecord>(entry.Payload)!).ToList();
        Assert.That(records.Select(record => record.Kind), Is.EquivalentTo(new[]
        {
            RuMCModerationKind.Warning, RuMCModerationKind.Warning, RuMCModerationKind.ServerBan,
            RuMCModerationKind.RoleBan, RuMCModerationKind.ChatBan,
        }));
        Assert.That(records.Single(record => record.Kind == RuMCModerationKind.RoleBan).Scope, Is.EqualTo("TestJob"));
        Assert.That(queued.Select(entry => entry.Payload), Has.None.Contains("127.0.0.1"));
        Assert.That(await db.ClaimModerationRecord("98765"), Is.Null);

        var delivery = (await db.ClaimModerationRecord("1234567890"))!;
        await db.CompleteModerationRecord(delivery, TimeSpan.FromMinutes(1));
        var failed = (await db.ClaimModerationRecord("1234567890"))!;
        await db.RejectModerationRecord(failed);
        for (var i = 0; i < 3; i++)
        {
            var next = (await db.ClaimModerationRecord("1234567890"))!;
            Assert.That(next.Id, Is.Not.EqualTo(delivery.Id));
            await db.CompleteModerationRecord(next, null);
        }

        Assert.That(await db.ClaimModerationRecord("1234567890"), Is.Null);
        var stranded = await context.Set<RuMCModerationOutbox>().AsNoTracking().ToListAsync();
        Assert.That(stranded.Single(entry => entry.FailedAt == null).Id, Is.EqualTo(delivery.Id));
        Assert.That(stranded.Single(entry => entry.FailedAt != null).Id, Is.EqualTo(failed.Id));
    }

    [TestCase("en-US", "Chat ban", "Channels", "Dead, OOC", "Discord profile", "Discord account not linked")]
    [TestCase("ru-RU", "Блокировка чата", "Каналы", "Мёртвые, ООС", "Профиль Discord", "Discord не привязан")]
    public void FormatsLocalizedChatBansWithSafeReasons(
        string language, string title, string channelsLabel, string channels, string profileLabel, string unlinkedLabel)
    {
        var folder = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (folder != null && !Directory.Exists(Path.Combine(folder.FullName, "Content.RUMC", "Resources", "Locale")))
            folder = folder.Parent;

        Assert.That(folder, Is.Not.Null);
        var root = new MemoryContentRoot();
        var registry = $"Locale/{language}/_RuMC14/moderation/registry.ftl";
        var chatBans = $"Locale/{language}/_RMC14/chat-bans.ftl";
        root.AddOrUpdateFile(new ResPath(registry), File.ReadAllBytes(Path.Combine(folder!.FullName, "Content.RUMC", "Resources", registry)));
        root.AddOrUpdateFile(new ResPath(chatBans), File.ReadAllBytes(Path.Combine(folder.FullName, "Resources", chatBans)));
        IoCManager.Resolve<IResourceManager>().AddRoot(new ResPath("/"), root);
        var loc = IoCManager.Resolve<ILocalizationManager>();
        var culture = new CultureInfo(language, false);
        loc.Initialize();
        loc.LoadCulture(culture);
        loc.SetCulture(culture);

        var reason = "@everyone **reason** " + new string('x', 4096);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var unlinked = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var accounts = new Dictionary<Guid, RuMCModerationAccount>
        {
            [first] = new("Pilot", 123456789012345678),
            [second] = new("Scout", 234567890123456789),
            [unlinked] = new("Marine", null),
            [admin] = new("Moderator", 345678901234567890),
        };
        var payload = RuMCModerationEmbed.Build(new RuMCModerationRecord(
            RuMCModerationKind.ChatBan, [first, second, unlinked], admin, reason, DateTime.UtcNow, null, [],
            ChatChannels: ChatType.Dead | ChatType.Ooc), Guid.Empty, accounts, "RuCM", loc);
        var embed = payload.Embeds!.Single();

        Assert.That(payload.AllowedMentions.Parse, Is.Empty);
        Assert.That(embed.Title, Is.EqualTo(title));
        Assert.That(embed.Fields.Single(field => field.Name == channelsLabel).Value, Is.EqualTo(channels));
        Assert.That(embed.Fields[0].Value.Split('\n'), Is.EqualTo(new[]
        {
            $"Pilot ({first}) · [{profileLabel}](https://discord.com/users/123456789012345678)",
            $"Scout ({second}) · [{profileLabel}](https://discord.com/users/234567890123456789)",
            $"Marine ({unlinked}) · {unlinkedLabel}",
        }));
        Assert.That(embed.Fields[1].Value, Is.EqualTo($"Moderator ({admin})"));
        Assert.That(embed.Description, Does.Contain(@"\*\*reason\*\*").And.EndsWith("…"));
        Assert.That(embed.Description.Length, Is.LessThanOrEqualTo(4096));
    }

    [TestCase(1023, true)]
    [TestCase(1024, false)]
    public void PlayerFieldStaysWithinDiscordLimitWithoutCuttingProfileLinks(int fullLength, bool overflow)
    {
        const int playerCount = 4;
        const ulong discordId = 123456789012345678;
        const string label = "Discord profile";
        var localization = new Mock<ILocalizationManager>();
        localization.Setup(loc => loc.GetString(It.IsAny<string>())).Returns((string key) => key);
        localization.Setup(loc => loc.GetString("rumc-moderation-registry-discord-profile")).Returns(label);

        var players = Enumerable.Range(0, playerCount).Select(_ => Guid.NewGuid()).ToArray();
        var link = $"[{label}](https://discord.com/users/{discordId})";
        var rowWithoutName = $" ({Guid.Empty}) · {link}".Length;
        var nameLength = fullLength - (playerCount - 1) - playerCount * rowWithoutName;
        var accounts = players.Select((player, index) => new
        {
            Player = player,
            Name = new string('x', nameLength / playerCount + (index < nameLength % playerCount ? 1 : 0)),
        }).ToDictionary(entry => entry.Player, entry => new RuMCModerationAccount(entry.Name, discordId));

        var targets = overflow ? [..players, Guid.NewGuid()] : players;
        var record = new RuMCModerationRecord(
            RuMCModerationKind.ServerBan, targets, null, "reason", DateTime.UtcNow, null, []);
        var field = RuMCModerationEmbed.Build(record, Guid.Empty, accounts, "RuCM", localization.Object)
            .Embeds!.Single().Fields[0].Value;

        var rows = field.Split('\n');
        Assert.That(field.Length, Is.LessThanOrEqualTo(1024));
        Assert.That(rows, Has.Length.EqualTo(playerCount));
        foreach (var row in rows.Take(overflow ? playerCount - 1 : playerCount))
            Assert.That(row, Does.EndWith(link));

        if (overflow)
            Assert.That(rows[^1], Is.EqualTo("…"));
        else
            Assert.That(field.Length, Is.EqualTo(fullLength));
    }

    [TestCase("https://discord.com/api/webhooks/1234567890/test_token", true)]
    [TestCase("https://discordapp.com/api/v10/webhooks/1234567890/test_token", true)]
    [TestCase("https://discordapp.com.example.org/api/webhooks/1234567890/test_token", false)]
    [TestCase("https://discord.com/api/webhooks/1234567890/test_token?thread_id=123", false)]
    public void AcceptsOnlyDiscordWebhookDestinations(string url, bool valid)
    {
        Assert.That(RuMCModerationWebhook.TryParse(url, out var webhook), Is.EqualTo(valid));
        if (valid)
            Assert.That(webhook.Id, Is.EqualTo("1234567890"));
    }
}

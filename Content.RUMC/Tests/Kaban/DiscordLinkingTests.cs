using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace Content.IntegrationTests._RuCM.Kaban;

public sealed partial class KabanEconomyTests
{
    private async Task<Guid> DiscordCode(Guid? player = null, DateTime? created = null)
    {
        var code = Guid.NewGuid();
        await using var db = Open();
        db.RMCLinkingCodes.Add(new RMCLinkingCodes
            { PlayerId = player ?? _user, Code = code, CreationTime = created ?? Now });
        await db.SaveChangesAsync();
        return code;
    }

    private async Task<RMCDiscordLinkResult> LinkDiscord(Guid code, ulong discord = 12345)
    {
        await using var db = Open();
        return await DiscordAccountLinking.Complete(db, code, discord, Now);
    }

    [Test]
    public async Task DiscordLinkConsumesCodeAndCreatesPersistentLinkAndAuditOnce()
    {
        var code = await DiscordCode();
        Assert.That((await LinkDiscord(code)).Status, Is.EqualTo(RMCDiscordLinkStatus.Success));
        Assert.That((await LinkDiscord(code)).Status, Is.EqualTo(RMCDiscordLinkStatus.InvalidCode));
        await using var db = Open();
        Assert.That((await db.RMCLinkedAccounts.SingleAsync()).PlayerId, Is.EqualTo(_user));
        Assert.That((await db.RMCLinkedAccounts.SingleAsync()).DiscordId, Is.EqualTo(12345UL));
        Assert.That(await db.RMCLinkingCodes.CountAsync(), Is.Zero);
        Assert.That(await db.RMCLinkedAccountLogs.CountAsync(), Is.EqualTo(1));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(1000));
    }

    [TestCase(-24)]
    [TestCase(-25)]
    [TestCase(1)]
    public async Task DiscordLinkRejectsExpiredAndFutureCodesWithoutMutatingAccounts(int hours)
    {
        var code = await DiscordCode(created: Now.AddHours(hours));
        Assert.That((await LinkDiscord(code)).Status, Is.EqualTo(RMCDiscordLinkStatus.Expired));
        await using var db = Open();
        Assert.That(await db.RMCLinkedAccounts.CountAsync(), Is.Zero);
        Assert.That(await db.RMCDiscordAccounts.CountAsync(), Is.Zero);
        Assert.That(await db.RMCLinkedAccountLogs.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task DiscordLinkRejectsUnknownAndEmptyCodesAndZeroDiscordId()
    {
        var code = await DiscordCode();
        Assert.That((await LinkDiscord(Guid.Empty)).Status, Is.EqualTo(RMCDiscordLinkStatus.InvalidCode));
        Assert.That((await LinkDiscord(Guid.NewGuid())).Status, Is.EqualTo(RMCDiscordLinkStatus.InvalidCode));
        Assert.That((await LinkDiscord(code, 0)).Status, Is.EqualTo(RMCDiscordLinkStatus.InvalidCode));
        await using var db = Open();
        Assert.That(await db.RMCLinkingCodes.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.RMCLinkedAccounts.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task DiscordLinkPreservesBothAccountsWhenRelinkingWouldDisplaceAnotherOwner()
    {
        var code = await DiscordCode();
        await LinkDiscord(code);
        var second = await DiscordCode(_manager);
        Assert.That((await LinkDiscord(second)).Status, Is.EqualTo(RMCDiscordLinkStatus.Conflict));
        var original = await DiscordCode();
        Assert.That((await LinkDiscord(original, 67890)).Status, Is.EqualTo(RMCDiscordLinkStatus.Conflict));
        await using var db = Open();
        Assert.That((await db.RMCLinkedAccounts.SingleAsync()).PlayerId, Is.EqualTo(_user));
        Assert.That((await db.RMCLinkedAccounts.SingleAsync()).DiscordId, Is.EqualTo(12345UL));
        Assert.That(await db.RMCLinkedAccountLogs.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.RMCLinkingCodes.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task DiscordLinkAcceptsSamePairWithoutDuplicatingAuditOrRemovingPatron()
    {
        await LinkDiscord(await DiscordCode());
        await using (var db = Open())
        {
            db.RMCPatronTiers.Add(new RMCPatronTier { Id = 900, Name = "Existing", DiscordRole = 900 });
            db.RMCPatrons.Add(new RMCPatron { PlayerId = _user, TierId = 900 });
            await db.SaveChangesAsync();
        }
        Assert.That((await LinkDiscord(await DiscordCode())).Status, Is.EqualTo(RMCDiscordLinkStatus.AlreadyLinked));
        await using var check = Open();
        Assert.That(await check.RMCLinkedAccountLogs.CountAsync(), Is.EqualTo(1));
        Assert.That((await check.RMCPatrons.SingleAsync()).TierId, Is.EqualTo(900));
        Assert.That(await check.RMCLinkingCodes.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task DiscordLinkSameCodeCannotBeClaimedByTwoDiscordUsers()
    {
        var code = await DiscordCode();
        var results = await Task.WhenAll(Task.Run(() => LinkDiscord(code)), Task.Run(() => LinkDiscord(code, 67890)));
        Assert.That(results.Count(r => r.Status == RMCDiscordLinkStatus.Success), Is.EqualTo(1));
        Assert.That(results.Count(r => r.Status == RMCDiscordLinkStatus.InvalidCode), Is.EqualTo(1));
        await using var db = Open();
        Assert.That(await db.RMCLinkedAccounts.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.RMCLinkedAccountLogs.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task DiscordLinkTwoGameAccountsCannotClaimOneDiscordUserConcurrently()
    {
        var a = await DiscordCode();
        var b = await DiscordCode(_manager);
        var results = await Task.WhenAll(Task.Run(() => LinkDiscord(a)), Task.Run(() => LinkDiscord(b)));
        Assert.That(results.Count(r => r.Status == RMCDiscordLinkStatus.Success), Is.EqualTo(1));
        Assert.That(results.Count(r => r.Status == RMCDiscordLinkStatus.Conflict), Is.EqualTo(1));
        await using var db = Open();
        Assert.That(await db.RMCLinkedAccounts.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.RMCLinkedAccountLogs.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.RMCLinkingCodes.CountAsync(), Is.EqualTo(1));
    }
}

public sealed class DiscordLinkBotTests
{
    [Test]
    public void AccountLinkBotNeedsNoPrivilegedIntentsWhileChatRelayRetainsMessageContent()
    {
        var minimal = Content.Server._RMC14.Discord.RMCDiscordManager.GameBotIntents(false);
        Assert.That(minimal, Is.EqualTo(GatewayIntents.Guilds));
        var relay = Content.Server._RMC14.Discord.RMCDiscordManager.GameBotIntents(true);
        Assert.That(relay.HasFlag(GatewayIntents.GuildMessages | GatewayIntents.MessageContent), Is.True);
        Assert.That(relay.HasFlag(GatewayIntents.GuildMembers), Is.False);
        Assert.That(relay.HasFlag(GatewayIntents.GuildPresences), Is.False);
    }
}

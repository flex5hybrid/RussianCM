using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._RuCM.Administration.ModerationRegistry;
using Content.Server._RuCM.Discord;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static readonly TimeSpan ModerationLeaseDuration = TimeSpan.FromMinutes(5);
    private long _moderationQueueVersion;

    internal long ModerationQueueVersion => Interlocked.Read(ref _moderationQueueVersion);

    protected void QueueModerationBan(ServerDbContext db, BanDef ban)
    {
        var kind = ban.Type == BanType.Role ? RuMCModerationKind.RoleBan : RuMCModerationKind.ServerBan;
        var scope = ban.Roles is { } roles ? string.Join(", ", roles.Select(role => role.RoleId)) : null;
        QueueModerationRecord(db, new(
            kind, ban.UserIds.Select(user => user.UserId).ToArray(), ban.BanningAdmin?.UserId, ban.Reason,
            ban.BanTime.UtcDateTime, ban.ExpirationTime?.UtcDateTime, [..ban.RoundIds], ban.Severity, scope));
    }

    private void QueueModerationWarning(ServerDbContext db, IAdminRemarksCommon warning, Guid? admin, NoteSeverity? severity = null)
    {
        QueueModerationRecord(db, new(
            RuMCModerationKind.Warning, warning.PlayerUserId is { } player ? [player] : [], admin,
            warning.Message, warning.CreatedAt, warning.ExpirationTime,
            warning.RoundId is { } round ? [round] : [], severity));
    }

    private void QueueModerationRecord(ServerDbContext db, RuMCModerationRecord record)
    {
        if (!RuMCModerationWebhook.TryParse(_configuration.GetCVar(RuMCModerationRegistryCVars.Webhook), out var webhook))
            return;

        db.Set<RuMCModerationOutbox>().Add(new RuMCModerationOutbox
        {
            Id = Guid.NewGuid(),
            WebhookId = webhook.Id,
            Payload = JsonSerializer.Serialize(record),
            AvailableAt = DateTime.UtcNow,
        });

        // Only wake the local dispatcher after the sanction and outbox row have committed.
        db.SavedChanges += (_, _) => Interlocked.Increment(ref _moderationQueueVersion);
    }

    internal async Task<RuMCModerationOutbox?> ClaimModerationRecord(string webhookId)
    {
        await using var db = await GetDb();
        var now = DateTime.UtcNow;
        var available = db.DbContext.Set<RuMCModerationOutbox>()
            .Where(entry => entry.WebhookId == webhookId && entry.FailedAt == null && entry.AvailableAt <= now);
        var next = await available.OrderBy(entry => entry.AvailableAt).ThenBy(entry => entry.Id).FirstOrDefaultAsync();
        if (next == null)
            return null;

        // The conditional update lets multiple game servers share an outbox without sending the same row.
        var lease = Guid.NewGuid();
        var changed = await available.Where(entry => entry.Id == next.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entry => entry.AvailableAt, now + ModerationLeaseDuration)
                .SetProperty(entry => entry.LeaseId, lease));

        next.LeaseId = lease;
        return changed == 1 ? next : null;
    }

    internal async Task CompleteModerationRecord(RuMCModerationOutbox entry, TimeSpan? retryAfter)
    {
        await using var db = await GetDb();
        var claimed = GetModerationLease(db.DbContext, entry);

        if (retryAfter is { } delay)
        {
            var availableAt = DateTime.UtcNow + delay;
            await claimed.ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.AvailableAt, availableAt)
                .SetProperty(row => row.LeaseId, (Guid?) null));
            return;
        }

        await claimed.ExecuteDeleteAsync();
    }

    internal async Task RejectModerationRecord(RuMCModerationOutbox entry)
    {
        await using var db = await GetDb();
        var failedAt = DateTime.UtcNow;
        await GetModerationLease(db.DbContext, entry).ExecuteUpdateAsync(setters => setters
            .SetProperty(row => row.FailedAt, failedAt)
            .SetProperty(row => row.LeaseId, (Guid?) null));
    }

    private static IQueryable<RuMCModerationOutbox> GetModerationLease(ServerDbContext db, RuMCModerationOutbox entry) =>
        db.Set<RuMCModerationOutbox>().Where(row => row.Id == entry.Id && row.LeaseId == entry.LeaseId);

    internal async Task<Dictionary<Guid, RuMCModerationAccount>> GetModerationAccounts(Guid[] ids)
    {
        await using var db = await GetDb();
        return await db.DbContext.Set<Player>().Where(player => ids.Contains(player.UserId))
            .Select(player => new
            {
                player.UserId,
                player.LastSeenUserName,
                DiscordId = player.LinkedAccount == null ? (ulong?) null : player.LinkedAccount.DiscordId,
            })
            .ToDictionaryAsync(player => player.UserId, player => new RuMCModerationAccount(player.LastSeenUserName, player.DiscordId));
    }
}

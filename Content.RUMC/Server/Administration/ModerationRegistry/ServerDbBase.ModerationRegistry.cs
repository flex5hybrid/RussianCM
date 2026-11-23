using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.CMU14.Administration.ModerationRegistry;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private readonly IConfigurationManager _configuration;

    protected void QueueModerationBan(ServerDbContext db, BanDef ban)
    {
        var kind = ban.Type == BanType.Role ? CMUModerationKind.RoleBan : CMUModerationKind.ServerBan;
        var scope = ban.Roles is { } roles ? string.Join(", ", roles.Select(role => role.RoleId)) : null;
        QueueModerationRecord(db, new(
            kind, ban.UserIds.Select(user => user.UserId).ToArray(), ban.BanningAdmin?.UserId, ban.Reason,
            ban.BanTime.UtcDateTime, ban.ExpirationTime?.UtcDateTime, [..ban.RoundIds], ban.Severity, scope));
    }

    private void QueueModerationWarning(ServerDbContext db, IAdminRemarksCommon warning, Guid? admin, NoteSeverity? severity = null)
    {
        QueueModerationRecord(db, new(
            CMUModerationKind.Warning, warning.PlayerUserId is { } player ? [player] : [], admin,
            warning.Message, warning.CreatedAt, warning.ExpirationTime,
            warning.RoundId is { } round ? [round] : [], severity));
    }

    private void QueueModerationRecord(ServerDbContext db, CMUModerationRecord record)
    {
        if (!CMUModerationRecord.TryGetWebhook(_configuration.GetCVar(CMUModerationRegistryCVars.DiscordWebhook), out var webhook))
            return;

        db.Set<CMUModerationOutbox>().Add(new CMUModerationOutbox
        {
            Id = Guid.NewGuid(),
            WebhookId = webhook.Id,
            Payload = JsonSerializer.Serialize(record),
            AvailableAt = DateTime.UtcNow,
        });
    }

    internal async Task<CMUModerationOutbox?> ClaimModerationRecord(string webhookId)
    {
        await using var db = await GetDb();
        var now = DateTime.UtcNow;
        var available = db.DbContext.Set<CMUModerationOutbox>()
            .Where(entry => entry.WebhookId == webhookId && entry.AvailableAt <= now);
        var next = await available.OrderBy(entry => entry.AvailableAt).ThenBy(entry => entry.Id).FirstOrDefaultAsync();
        if (next == null)
            return null;

        // The conditional update lets multiple game servers share an outbox without sending the same row.
        var lease = Guid.NewGuid();
        var changed = await available.Where(entry => entry.Id == next.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entry => entry.AvailableAt, now.AddMinutes(5))
                .SetProperty(entry => entry.LeaseId, lease));

        next.LeaseId = lease;
        return changed == 1 ? next : null;
    }

    internal async Task CompleteModerationRecord(CMUModerationOutbox entry, TimeSpan? retryAfter)
    {
        await using var db = await GetDb();
        var claimed = db.DbContext.Set<CMUModerationOutbox>()
            .Where(row => row.Id == entry.Id && row.LeaseId == entry.LeaseId);

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

    internal async Task<Dictionary<Guid, string>> GetModerationNames(Guid[] ids)
    {
        await using var db = await GetDb();
        return await db.DbContext.Set<Player>().Where(player => ids.Contains(player.UserId))
            .ToDictionaryAsync(player => player.UserId, player => player.LastSeenUserName);
    }
}

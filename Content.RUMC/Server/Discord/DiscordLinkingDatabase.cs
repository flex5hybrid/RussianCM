using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Content.Server.Database;

public enum RMCDiscordLinkStatus { Success, AlreadyLinked, InvalidCode, Expired, Conflict }
public sealed record RMCDiscordLinkResult(RMCDiscordLinkStatus Status, Guid Player = default);

public partial interface IServerDbManager
{
    Task<RMCDiscordLinkResult> CompleteDiscordLink(Guid code, ulong discord, DateTime now);
}

public sealed partial class ServerDbManager
{
    public Task<RMCDiscordLinkResult> CompleteDiscordLink(Guid code, ulong discord, DateTime now)
        => RunDbCommand(() => _db.CompleteDiscordLink(code, discord, now));
}

public abstract partial class ServerDbBase
{
    public async Task<RMCDiscordLinkResult> CompleteDiscordLink(Guid code, ulong discord, DateTime now)
    {
        await using var guard = await GetDb();
        return await DiscordAccountLinking.Complete(guard.DbContext, code, discord, now);
    }
}

/// <summary>Uses the game's configured SQLite/PostgreSQL database and existing account tables.</summary>
public static class DiscordAccountLinking
{
    public static async Task<RMCDiscordLinkResult> Complete(ServerDbContext db, Guid code, ulong discord, DateTime now)
    {
        if (code == Guid.Empty || discord == 0)
            return new(RMCDiscordLinkStatus.InvalidCode);
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Acquire the code row before reading. SQLite also acquires its writer lock here.
        var matches = await db.RMCLinkingCodes.Where(c => c.Code == code)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Code, code));
        if (matches != 1)
            return new(RMCDiscordLinkStatus.InvalidCode);
        var linking = await db.RMCLinkingCodes.SingleAsync(c => c.Code == code);
        if (linking.CreationTime <= now.AddDays(-1) || linking.CreationTime > now)
            return new(RMCDiscordLinkStatus.Expired);
        var existing = await db.RMCLinkedAccounts
            .Where(l => l.PlayerId == linking.PlayerId || l.DiscordId == discord).ToArrayAsync();
        // Never silently detach somebody else's account or remove their patron privileges.
        if (existing.Any(l => l.PlayerId != linking.PlayerId || l.DiscordId != discord))
            return new(RMCDiscordLinkStatus.Conflict);
        var alreadyLinked = existing.Length != 0;
        if (!alreadyLinked)
        {
            if (!await db.RMCDiscordAccounts.AnyAsync(a => a.Id == discord))
                db.RMCDiscordAccounts.Add(new RMCDiscordAccount { Id = discord });
            db.RMCLinkedAccounts.Add(new RMCLinkedAccount { PlayerId = linking.PlayerId, DiscordId = discord });
            db.RMCLinkedAccountLogs.Add(new RMCLinkedAccountLogs
                { PlayerId = linking.PlayerId, DiscordId = discord, At = now });
        }
        db.RMCLinkingCodes.Remove(linking);
        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" }
            or SqliteException { SqliteExtendedErrorCode: 1555 or 2067 })
        {
            // Two distinct codes racing for one Discord account: the unique index decides once.
            await transaction.RollbackAsync();
            return new(RMCDiscordLinkStatus.Conflict);
        }
        return new(alreadyLinked ? RMCDiscordLinkStatus.AlreadyLinked : RMCDiscordLinkStatus.Success, linking.PlayerId);
    }
}

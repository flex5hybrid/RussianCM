using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

// ReSharper disable CheckNamespace
namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<CMUSponsorPreferences?> GetCMUSponsorPreferences(Guid player);
    Task<List<CMUSponsorPreferences>> GetAllCMUSponsorPreferences();
    Task SetCMUSponsorSettings(Guid player, string settings);
    Task<bool> ApproveCMUSponsorFigurine(Guid player, string settingsSnapshot, string description);
    Task<bool> ApproveCMUSponsorLobby(Guid player, string message);
    Task SetCMUSponsorCustomItem(Guid player, string prototype);
}

public sealed partial class ServerDbManager
{
    public Task<CMUSponsorPreferences?> GetCMUSponsorPreferences(Guid player) =>
        RunDbCommand(() => _db.GetCMUSponsorPreferences(player));
    public Task<List<CMUSponsorPreferences>> GetAllCMUSponsorPreferences() =>
        RunDbCommand(() => _db.GetAllCMUSponsorPreferences());
    public Task SetCMUSponsorSettings(Guid player, string settings) =>
        RunDbCommand(() => _db.SetCMUSponsorSettings(player, settings));
    public Task<bool> ApproveCMUSponsorFigurine(Guid player, string settingsSnapshot, string description) =>
        RunDbCommand(() => _db.ApproveCMUSponsorFigurine(player, settingsSnapshot, description));
    public Task<bool> ApproveCMUSponsorLobby(Guid player, string message) =>
        RunDbCommand(() => _db.ApproveCMUSponsorLobby(player, message));
    public Task SetCMUSponsorCustomItem(Guid player, string prototype) =>
        RunDbCommand(() => _db.SetCMUSponsorCustomItem(player, prototype));
}

public abstract partial class ServerDbBase
{
    public async Task<CMUSponsorPreferences?> GetCMUSponsorPreferences(Guid player)
    {
        await using var db = await GetDb();
        return await db.DbContext.CMUSponsorPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == player);
    }

    public async Task<List<CMUSponsorPreferences>> GetAllCMUSponsorPreferences()
    {
        await using var db = await GetDb();
        return await db.DbContext.CMUSponsorPreferences.AsNoTracking().ToListAsync();
    }

    private static async Task<CMUSponsorPreferences> SponsorRow(ServerDbContext db, Guid player)
    {
        var row = await db.CMUSponsorPreferences.FirstOrDefaultAsync(p => p.PlayerId == player);
        if (row != null)
            return row;
        row = new CMUSponsorPreferences { PlayerId = player };
        db.CMUSponsorPreferences.Add(row);
        return row;
    }

    public async Task SetCMUSponsorSettings(Guid player, string settings)
    {
        using var guard = await LockPreferencesAsync(new NetUserId(player));
        await using var db = await GetDb();
        var row = await SponsorRow(db.DbContext, player);
        row.Settings = settings;
        await db.DbContext.SaveChangesAsync();
    }

    public async Task<bool> ApproveCMUSponsorFigurine(Guid player, string settingsSnapshot, string description)
    {
        using var guard = await LockPreferencesAsync(new NetUserId(player));
        await using var db = await GetDb();
        var row = await SponsorRow(db.DbContext, player);
        // Approval applies only to the draft that the moderator actually reviewed.
        if (row.Settings != settingsSnapshot)
            return false;
        row.ApprovedFigurineDescription = description;
        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApproveCMUSponsorLobby(Guid player, string message)
    {
        using var guard = await LockPreferencesAsync(new NetUserId(player));
        await using var db = await GetDb();
        var row = await db.DbContext.RMCPatronLobbyMessages.FirstOrDefaultAsync(p => p.PatronId == player);
        if (row == null || row.Message != message)
            return false;
        row.Approved = true;
        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task SetCMUSponsorCustomItem(Guid player, string prototype)
    {
        using var guard = await LockPreferencesAsync(new NetUserId(player));
        await using var db = await GetDb();
        var row = await SponsorRow(db.DbContext, player);
        row.CustomItem = prototype;
        await db.DbContext.SaveChangesAsync();
    }
}

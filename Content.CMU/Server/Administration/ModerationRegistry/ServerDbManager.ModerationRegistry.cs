using System.Threading.Tasks;

namespace Content.Server.Database;

public sealed partial class ServerDbManager
{
    Task<CMUModerationOutbox?> IServerDbManager.ClaimModerationRecord(string webhookId) =>
        RunDbCommand(() => _db.ClaimModerationRecord(webhookId));

    Task IServerDbManager.CompleteModerationRecord(CMUModerationOutbox entry, TimeSpan? retryAfter) =>
        RunDbCommand(() => _db.CompleteModerationRecord(entry, retryAfter));

    Task<Dictionary<Guid, string>> IServerDbManager.GetModerationNames(Guid[] ids) =>
        RunDbCommand(() => _db.GetModerationNames(ids));
}

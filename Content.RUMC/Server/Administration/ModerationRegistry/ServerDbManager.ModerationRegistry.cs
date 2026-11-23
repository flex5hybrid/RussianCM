using System.Threading.Tasks;
using Content.Server._RuCM.Administration.ModerationRegistry;

namespace Content.Server.Database;

public sealed partial class ServerDbManager : IRuMCModerationOutbox
{
    long IRuMCModerationOutbox.QueueVersion => _db.ModerationQueueVersion;

    Task<RuMCModerationOutbox?> IRuMCModerationOutbox.ClaimModerationRecord(string webhookId) =>
        RunDbCommand(() => _db.ClaimModerationRecord(webhookId));

    Task IRuMCModerationOutbox.CompleteModerationRecord(RuMCModerationOutbox entry, TimeSpan? retryAfter) =>
        RunDbCommand(() => _db.CompleteModerationRecord(entry, retryAfter));

    Task IRuMCModerationOutbox.RejectModerationRecord(RuMCModerationOutbox entry) =>
        RunDbCommand(() => _db.RejectModerationRecord(entry));

    Task<Dictionary<Guid, string>> IRuMCModerationOutbox.GetModerationNames(Guid[] ids) =>
        RunDbCommand(() => _db.GetModerationNames(ids));
}

using System.Threading.Tasks;
using Content.Server.Database;

namespace Content.Server._RuCM.Administration.ModerationRegistry;

public interface IRuMCModerationOutbox
{
    long QueueVersion { get; }
    Task<RuMCModerationOutbox?> ClaimModerationRecord(string webhookId);
    Task CompleteModerationRecord(RuMCModerationOutbox entry, TimeSpan? retryAfter);
    Task RejectModerationRecord(RuMCModerationOutbox entry);
    Task<Dictionary<Guid, string>> GetModerationNames(Guid[] ids);
}

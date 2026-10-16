using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.AdminAI;

[Serializable, NetSerializable]
public sealed class AdminAIContextRequest : EntityEventArgs
{
    public readonly NetUserId Channel;
    public readonly Guid SelectionId, ViewId;
    public readonly int Round, BeforeMinutes, AfterMinutes;
    public readonly Guid[] AdditionalPlayers;
    public AdminAIContextRequest(NetUserId channel, Guid selectionId, Guid viewId, int round,
        int beforeMinutes, int afterMinutes, Guid[] additionalPlayers)
    { Channel = channel; SelectionId = selectionId; ViewId = viewId; Round = round; BeforeMinutes = beforeMinutes; AfterMinutes = afterMinutes; AdditionalPlayers = additionalPlayers; }
}

[Serializable, NetSerializable]
public sealed class AdminAIEvidence
{
    public readonly int Round, LogId;
    public readonly long UtcTicks;
    public readonly string Type, Message;
    public AdminAIEvidence(int round, int logId, long utcTicks, string type, string message)
    { Round = round; LogId = logId; UtcTicks = utcTicks; Type = type; Message = message; }
}

[Serializable, NetSerializable]
public sealed class AdminAICaseState : EntityEventArgs
{
    public readonly NetUserId Channel;
    public readonly Guid SelectionId, ViewId, ReportId;
    public readonly string Status, Report;
    public readonly AdminAIEvidence[] Evidence;
    public readonly int Seconds;
    public AdminAICaseState(NetUserId channel, Guid selectionId, Guid viewId, Guid reportId, string status,
        string report, AdminAIEvidence[] evidence, int seconds = 0)
    { Channel = channel; SelectionId = selectionId; ViewId = viewId; ReportId = reportId; Status = status; Report = report; Evidence = evidence; Seconds = seconds; }
}

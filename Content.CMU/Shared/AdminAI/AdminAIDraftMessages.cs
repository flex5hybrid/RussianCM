using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.AdminAI;

[Serializable, NetSerializable]
public enum AdminAIDraftAction : byte { Open, Prepare, Cancel, Refresh, Close }

[Serializable, NetSerializable]
public enum AdminAIDraftStatus : byte
{
    Disabled, Unavailable, Paused, NoConversation, Ready, Working, Draft, Cancelled,
    Expired, Denied, InvalidResponse, RateLimited, QueueFull, Stale, KnowledgeLoading,
}

/// <summary>The sender identity is always taken from the authenticated session, never this packet.</summary>
[Serializable, NetSerializable]
public sealed class AdminAIDraftRequest : EntityEventArgs
{
    public readonly NetUserId Channel;
    public readonly Guid SelectionId;
    public readonly Guid ViewId;
    public readonly AdminAIDraftAction Action;
    public AdminAIDraftRequest(NetUserId channel, Guid selectionId, Guid viewId, AdminAIDraftAction action)
    { Channel = channel; SelectionId = selectionId; ViewId = viewId; Action = action; }
}

[Serializable, NetSerializable]
public sealed class AdminAISource
{
    public readonly string GuideId;
    public readonly string Title;
    public readonly string Version;
    public readonly string Fragment;
    public AdminAISource(string guideId, string title, string version, string fragment)
    { GuideId = guideId; Title = title; Version = version; Fragment = fragment; }
}

/// <summary>Private to one authorized administrator. Never a BwoinkTextMessage or broadcast.</summary>
[Serializable, NetSerializable]
public sealed class AdminAIDraftState : EntityEventArgs
{
    public readonly NetUserId Channel;
    public readonly Guid SelectionId;
    public readonly Guid ViewId;
    public readonly Guid CaseId;
    public readonly Guid RequestId;
    public readonly AdminAIDraftStatus Status;
    public readonly string Draft;
    public readonly AdminAISource[] Sources;
    public readonly string WorkerState;
    public readonly int Pending;
    public readonly int DraftSeconds;
    public readonly bool CanInvestigate;
    public readonly int Round;
    public AdminAIDraftState(NetUserId channel, Guid selectionId, Guid viewId, Guid caseId, Guid requestId,
        AdminAIDraftStatus status, string draft, AdminAISource[] sources, string workerState, int pending, int draftSeconds = 0,
        bool canInvestigate = false, int round = 0)
    {
        Channel = channel; SelectionId = selectionId; ViewId = viewId; CaseId = caseId; RequestId = requestId;
        Status = status; Draft = draft; Sources = sources; WorkerState = workerState; Pending = pending;
        DraftSeconds = draftSeconds;
        CanInvestigate = canInvestigate; Round = round;
    }
}

[Serializable, NetSerializable]
public sealed class AdminAIDraftReset : EntityEventArgs { }

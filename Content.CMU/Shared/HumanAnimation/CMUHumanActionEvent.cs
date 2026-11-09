using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.HumanAnimation;

[Serializable, NetSerializable]
public enum CMUHumanAction : byte
{
    None,
    Reach,
    Melee,
    Shoot,
    Fall,
    Rise,
}

/// <summary>A confirmed action for the optional client-side human pose layer.</summary>
[Serializable, NetSerializable]
public sealed class CMUHumanActionEvent(NetEntity actor, CMUHumanAction action) : EntityEventArgs
{
    public readonly NetEntity Actor = actor;
    public readonly CMUHumanAction Action = action;
}

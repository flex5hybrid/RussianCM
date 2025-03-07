using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.TargetOverlay.Events;


[Serializable, NetSerializable]
public sealed class RemoveTargetOverlayEvent : EntityEventArgs
{
    public NetEntity Player;
}

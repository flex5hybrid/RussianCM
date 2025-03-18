using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.TargetOverlay.Events;


[Serializable, NetSerializable]
public sealed class TargetOverlayShootEvent : EntityEventArgs
{
    public NetEntity Performer;

    public NetEntity? Sender;

    public List<(MapCoordinates CursorPosition, NetEntity? Target)> Targets = new();
}

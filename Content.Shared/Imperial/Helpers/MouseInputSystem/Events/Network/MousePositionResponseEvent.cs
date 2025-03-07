using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.MouseInput.Events;


[Serializable, NetSerializable]
public sealed class MousePositionResponseEvent : EntityEventArgs
{
    public NetCoordinates Coordinates;

    public NetEntity Performer;
}

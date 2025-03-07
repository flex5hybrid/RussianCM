using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.MouseInput.Events;


[Serializable, NetSerializable]
public sealed class MousePositionRequestEvent : EntityEventArgs
{
    public NetEntity Performer;

    public NetEntity Player;
}

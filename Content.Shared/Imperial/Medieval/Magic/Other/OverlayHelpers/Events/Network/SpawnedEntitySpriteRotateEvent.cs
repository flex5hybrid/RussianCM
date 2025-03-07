using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic.Overlays;


[Serializable, NetSerializable]
public sealed partial class SpawnedEntitySpriteRotateEvent : EntityEventArgs
{
    public Angle Rotation;

    public NetEntity Action;
}

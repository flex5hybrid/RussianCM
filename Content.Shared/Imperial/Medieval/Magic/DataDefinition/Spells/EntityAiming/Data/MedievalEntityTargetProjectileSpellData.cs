using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


[Serializable, NetSerializable, DataDefinition]
public sealed partial class MedievalEntityTargetProjectileSpellData : MedievalEntityAimingSpellData
{
    [DataField(required: true)]
    public EntProtoId ProjectilePrototype;

    [DataField]
    public float ProjectileSpeed = 20f;
}

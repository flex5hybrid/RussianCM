using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


[Serializable, NetSerializable, DataDefinition]
public sealed partial class MedievalHomingProjectilesSpellData : MedievalEntityAimingSpellData
{
    [DataField(required: true)]
    public EntProtoId ProjectilePrototype;

    [DataField]
    public float ProjectileSpeed = 20f;

    [DataField]
    public Angle Spread = Angle.FromDegrees(180);

    [DataField]
    public float LinearVelocityIntensy = 1.0f;

    [DataField]
    public Angle RelativeAngle = Angle.Zero;
}

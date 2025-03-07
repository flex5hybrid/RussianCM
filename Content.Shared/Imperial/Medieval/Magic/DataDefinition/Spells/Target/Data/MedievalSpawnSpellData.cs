using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// DoAfter parameters that are required to cast the spell
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class MedievalSpawnSpellData : MedievalTargetSpellData
{
    [DataField]
    public EntProtoId SpawnedEntityPrototype;

    [DataField]
    public SpawnSpellType SpawnSpellType;

    [DataField]
    public Vector2 RelativePosition = new Vector2(0, 0);

    [DataField]
    public Angle Angle;
}

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


[Serializable, NetSerializable, DataDefinition]
public sealed partial class MedievalSpawnInHandSpellData : MedievalInstantSpellData
{
    [DataField]
    public EntProtoId SpawnedEntityPrototype;
}

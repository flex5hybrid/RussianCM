using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// DoAfter parameters that are required to cast the spell
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class MedievalTeleportSpellData : MedievalTargetSpellData;

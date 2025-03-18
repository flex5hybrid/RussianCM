using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class MedievalTargetSpellData : MedievalSpellData
{
    /// <summary>
    /// Selects where to cast spells.
    /// Can be useful for example for a fireball, to calculate where it will fly while aiming
    /// </summary>
    [DataField]
    public TargetSpellType TargetSpellType = TargetSpellType.TargetOnLastMousePosition;

    [DataField]
    public NetCoordinates Coordinates;
}

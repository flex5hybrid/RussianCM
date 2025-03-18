using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public abstract partial class MedievalSpellData
{
    [DataField]
    public NetEntity Performer;

    [DataField]
    public NetEntity Action;

    [DataField]
    public float CastSpeedModifier;
}

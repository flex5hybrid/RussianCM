using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// An event that is called at the end of a spell cast with DoAfter
/// </summary>
[Serializable, NetSerializable]
public abstract partial class MedievalSpellDoAfterEvent : SimpleDoAfterEvent
{
    public MedievalSpellData SpellData;
}

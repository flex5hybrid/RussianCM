using Content.Shared.Actions;

namespace Content.Shared.Imperial.Medieval.Magic;


public abstract partial class MedievalTargetSpellEvent : WorldTargetActionEvent, IMedievalDoAfterSpell, IMedievalSpeakSpell
{
    /// <summary>
    /// Do After args for spells
    /// </summary>
    [DataField]
    public MedievalSpellDoAfterArgs? SpellCastDoAfter { get; private set; }

    /// <summary>
    /// Localized string spoken by the caster when casting this spell.
    /// </summary>
    [DataField]
    public Dictionary<TimeSpan, MedievalSpellSpeech>? SpeechPoints { get; private set; }

    /// <summary>
    /// Selects where to cast spells.
    /// Can be useful for example for a fireball, to calculate where it will fly while aiming
    /// </summary>
    [DataField]
    public TargetSpellType TargetSpellType = TargetSpellType.TargetOnLastMousePosition;

}

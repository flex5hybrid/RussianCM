using Content.Shared.Actions;

namespace Content.Shared.Imperial.Medieval.Magic;


public abstract partial class MedievalInstantSpellEvent : InstantActionEvent, IMedievalDoAfterSpell, IMedievalSpeakSpell
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
}

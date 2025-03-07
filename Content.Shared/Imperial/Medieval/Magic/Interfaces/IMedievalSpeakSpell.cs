namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Interface for spoken events
/// </summary>
public interface IMedievalSpeakSpell
{
    /// <summary>
    /// Localized string spoken by the caster when casting this spell.
    /// </summary>
    public Dictionary<TimeSpan, MedievalSpellSpeech>? SpeechPoints { get; }
}

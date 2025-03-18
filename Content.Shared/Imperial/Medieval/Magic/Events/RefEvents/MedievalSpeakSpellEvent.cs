namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Raised when the caster casts a spell
/// </summary>
[ByRefEvent]
public struct MedievalSpeakSpellEvent(EntityUid performer, string speech)
{
    public readonly EntityUid Performer = performer;

    public readonly string Speech = speech;

    public bool Cancelled = false;
}

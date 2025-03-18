namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Raised on fail spell cast
/// </summary>
public sealed class MedievalFailCastSpellEvent : EntityEventArgs
{
    public EntityUid Performer;

    public EntityUid Action;
}

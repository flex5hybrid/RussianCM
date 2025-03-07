namespace Content.Shared.Imperial.Medieval.Magic;


public sealed class MedievalSpellRelayEvent<TEvent>(TEvent args) : EntityEventArgs
{
    public TEvent Args = args;
}

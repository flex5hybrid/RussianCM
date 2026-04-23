namespace Content.Shared.Corvax.TTS;

public sealed class RMCAnnouncementMadeEvent : EntityEventArgs
{
    public readonly EntityUid? Source;
    public readonly string RawMessage;

    public RMCAnnouncementMadeEvent(EntityUid? source, string rawMessage)
    {
        Source = source;
        RawMessage = rawMessage;
    }
}

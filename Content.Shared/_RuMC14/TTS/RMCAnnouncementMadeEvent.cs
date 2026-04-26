using Robust.Shared.Player;

namespace Content.Shared.Corvax.TTS;

public sealed class RMCAnnouncementMadeEvent : EntityEventArgs
{
    public readonly EntityUid? Source;
    public readonly string RawMessage;
    public readonly Filter? Filter;
    public readonly bool ExcludeSurvivors;
    public readonly string? Faction;

    public RMCAnnouncementMadeEvent(
        EntityUid? source,
        string rawMessage,
        Filter? filter = null,
        bool excludeSurvivors = true,
        string? faction = null)
    {
        Source = source;
        RawMessage = rawMessage;
        Filter = filter;
        ExcludeSurvivors = excludeSurvivors;
        Faction = faction;
    }
}

using Content.Shared.GameTicking;
using Robust.Shared.Localization;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    /// <summary>Same public map labels as the lobby/escape screen; never exposes hidden map entities.</summary>
    public string GetPublicAIMapDescription() => Loc.GetString(
        RunLevel == GameRunLevel.InRound ? "cmu-admin-ai-current-map" : "cmu-admin-ai-selected-map",
        ("planet", GetPlanetMapName()), ("ships", GetShipMapName()));
}

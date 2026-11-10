namespace Content.Server.GameTicking.Presets;

// CMU14: exclusions belong to the mode, preserving platoon assets and other modes.
public sealed partial class GamePresetPrototype
{
    [DataField]
    public List<string> ExcludedPlatoons = new();
}

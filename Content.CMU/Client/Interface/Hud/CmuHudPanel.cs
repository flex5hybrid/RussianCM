namespace Content.Client.CMU14.Interface.Hud;

/// <summary>
/// A HUD surface sharing the display simulation and UI Setup preferences.
/// Chrome, text and icons share one live CRT image at native capture resolution.
/// </summary>
public sealed class CmuHudPanel : CmuCrtDisplayPanel
{
    public bool RasterEnabled { get => EffectEnabled; set => EffectEnabled = value; }
}

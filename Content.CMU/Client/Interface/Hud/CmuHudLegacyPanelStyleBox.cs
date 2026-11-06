using Content.Client.Stylesheets;
using Robust.Client.Graphics;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Palette-aware backing for panels that historically set an explicit grey override.</summary>
public sealed class CmuHudLegacyPanelStyleBox : StyleBoxFlat
{
    private readonly Color _legacy;
    public CmuHudLegacyPanelStyleBox(Color legacy) => _legacy = legacy;

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        BackgroundColor = StyleNano.CrtUiEnabled ? CmuHudStyle.InsetColor : _legacy;
        base.DoDraw(handle, box, uiScale);
    }
}

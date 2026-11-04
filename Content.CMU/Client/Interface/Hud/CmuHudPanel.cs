using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>
/// A terminal surface using the existing CRT raster pass and UI setup intensity/pitch settings.
/// Content remains live: no subtree capture, input remapping or duplicated message buffers.
/// </summary>
public sealed class CmuHudPanel : PanelContainer
{
    private readonly CrtScanlineOverlay _raster;
    public bool RasterEnabled { get => _raster.Visible; set => _raster.Visible = value; }
    public bool ChatSurface
    {
        get => _raster.ChatSurface;
        set
        {
            _raster.ChatSurface = value;
            // The tall communication column is a reading surface, not a rounded prop screen.
            _raster.Glass = !value;
        }
    }
    public bool MenuSurface { get => _raster.MenuSurface; set => _raster.MenuSurface = value; }

    public CmuHudPanel()
    {
        AddChild(_raster = new CrtScanlineOverlay { Glass = true });
    }

    protected override void ChildAdded(Control child)
    {
        base.ChildAdded(child);
        // XAML adds content after the constructor. Keep the non-interactive raster above it.
        foreach (var existing in Children)
        {
            if (existing is not CrtScanlineOverlay raster || raster == child)
                continue;
            raster.SetPositionInParent(ChildCount - 1);
            break;
        }
    }
}

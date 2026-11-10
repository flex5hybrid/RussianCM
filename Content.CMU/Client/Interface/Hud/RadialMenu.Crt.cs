using System.Numerics;
using Content.Client.CMU14.Interface;
using Content.Client.CMU14.Interface.Hud;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface.Controls;

public partial class RadialMenuButtonWithSector
{
    private bool CMUHighlighted => DrawMode is DrawModeEnum.Hover or DrawModeEnum.Pressed;

    // Annulus primitives expect sRGB, unlike the screen handle's lines and circles.
    private Color CMUSectorFill(Color fallback) => !StyleNano.CrtUiEnabled ? fallback : Color.ToSrgb(
        (CMUHighlighted ? CmuHudStyle.HoverColor : CmuHudStyle.SurfaceColor)
        .WithAlpha(CMUHighlighted ? 0.96f : 0.88f));

    private Color CMUSectorEdge(Color fallback) => !StyleNano.CrtUiEnabled ? fallback : Color.ToSrgb(
        CMUHighlighted ? CmuHudStyle.SelectionColor : CmuHudStyle.EdgeColor.WithAlpha(0.8f));

    private Color CMUSectorSeparator(Color fallback) => !StyleNano.CrtUiEnabled
        ? fallback
        : CmuHudStyle.EdgeColor.WithAlpha(0.65f);
}

public sealed partial class RadialMenuContextualCentralTextureButton
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        var back = HasStyleClass("RadialMenuBackButton");
        if (!StyleNano.CrtUiEnabled || !back && !HasStyleClass("RadialMenuCloseButton"))
        {
            base.Draw(handle);
            return;
        }

        var center = (Vector2) PixelSize / 2;
        var radius = MathF.Max(1, MathF.Min(PixelSize.X, PixelSize.Y) / 2f - 2 * UIScale);
        var highlighted = DrawMode is DrawModeEnum.Hover or DrawModeEnum.Pressed;
        CmuCrtWindowsUIController.DrawShape(handle, () =>
            handle.DrawCircle(center, radius, highlighted ? CmuHudStyle.HoverColor : CmuHudStyle.SurfaceColor));
        handle.DrawCircle(center, radius, highlighted ? CmuHudStyle.SelectionColor : CmuHudStyle.EdgeColor, false);

        var glyph = radius * 0.32f;
        var color = back ? CmuHudStyle.TextColor : CrtTerminalPalette.Alert;
        if (back)
        {
            var tip = center - new Vector2(glyph, 0);
            handle.DrawLine(tip, center + new Vector2(glyph, 0), color);
            handle.DrawLine(tip, center + new Vector2(0, -glyph), color);
            handle.DrawLine(tip, center + new Vector2(0, glyph), color);
        }
        else
        {
            handle.DrawLine(center - new Vector2(glyph), center + new Vector2(glyph), color);
            handle.DrawLine(center + new Vector2(-glyph, glyph), center + new Vector2(glyph, -glyph), color);
        }
    }
}

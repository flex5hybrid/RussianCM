using System.Numerics;
using Content.Client.CMU14.Interface;
using Content.Client.CMU14.Interface.Hud;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Storage;

/// <summary>Keep native storage texture measurements, replacing only their CRT presentation.</summary>
public sealed class CmuStorageTexture : TextureRect
{
    public bool Sidebar { get; init; }
    public bool Blocked { get; init; }
    public bool Opaque { get; init; }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!StyleNano.CrtUiEnabled || Theme.ID != "SS14DefaultTheme")
        {
            base.Draw(handle);
            return;
        }

        var box = PixelSizeBox;
        var line = MathF.Max(1, UIScale);
        var fill = Sidebar ? CmuHudStyle.SurfaceColor : CmuHudStyle.InsetColor;
        CmuCrtWindowsUIController.DrawFill(handle, box, fill.WithAlpha(Opaque || Sidebar ? 1f : 0.92f));
        var edge = CmuHudStyle.EdgeColor.WithAlpha(Sidebar ? 0.8f : 0.4f);
        handle.DrawRect(new UIBox2(box.Right - line, box.Top, box.Right, box.Bottom), edge);
        if (Sidebar)
            return;

        handle.DrawRect(new UIBox2(box.Left, box.Bottom - line, box.Right, box.Bottom), edge);
        if (!Blocked)
            return;

        // Unusable cells remain visibly distinct without the old solid grey tile.
        var inset = MathF.Min(box.Width, box.Height) / 3;
        handle.DrawLine(box.TopLeft + new Vector2(inset), box.BottomRight - new Vector2(inset), edge);
    }
}

/// <summary>Native close/back controls retain their textures for measurement and theme fallback.</summary>
public sealed class CmuStorageButton : TextureButton
{
    public bool Back { get; init; }
    private static readonly CmuHudStyleBox Normal = CmuHudStyle.Box(CmuHudTone.Inset);
    private static readonly CmuHudStyleBox Hover = CmuHudStyle.Box(CmuHudTone.Hover);
    private static readonly CmuHudStyleBox PressedBox = CmuHudStyle.Box(selected: true);

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!StyleNano.CrtUiEnabled || Theme.ID != "SS14DefaultTheme")
        {
            base.Draw(handle);
            return;
        }

        var box = PixelSizeBox;
        (DrawMode == DrawModeEnum.Pressed ? PressedBox : DrawMode == DrawModeEnum.Hover ? Hover : Normal)
            .Draw(handle, box, UIScale);
        var color = Back ? CmuHudStyle.TextColor : CrtTerminalPalette.Alert;
        if (DrawMode == DrawModeEnum.Disabled)
            color = color.WithAlpha(0.4f);
        var inset = box.Size * 0.3f;
        var a = box.TopLeft + inset;
        var b = box.BottomRight - inset;
        if (Back)
        {
            var middle = (a.Y + b.Y) / 2;
            handle.DrawLine(new Vector2(a.X, middle), new Vector2(b.X, middle), color);
            handle.DrawLine(new Vector2(a.X, middle), new Vector2((a.X + b.X) / 2, a.Y), color);
            handle.DrawLine(new Vector2(a.X, middle), new Vector2((a.X + b.X) / 2, b.Y), color);
        }
        else
        {
            handle.DrawLine(a, b, color);
            handle.DrawLine(new Vector2(a.X, b.Y), new Vector2(b.X, a.Y), color);
        }
    }
}

/// <summary>Preserve the native two-pixel inset while drawing a palette-aware hairline.</summary>
public sealed class CmuStorageFrame : PanelContainer
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!StyleNano.CrtUiEnabled || Theme.ID != "SS14DefaultTheme")
        {
            base.Draw(handle);
            return;
        }

        handle.DrawRect(PixelSizeBox, CmuHudStyle.EdgeColor, filled: false);
    }
}

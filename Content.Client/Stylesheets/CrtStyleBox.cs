using System;
using Content.Client.CMU14.Interface.Hud; // CMU14: CRT material belongs to fills, below text.
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client.Stylesheets;

/// <summary>
///     A flat panel with a border and optional corner brackets, for the CRT theme.
/// </summary>
/// <remarks>
/// CMU14: fills and borders use native drawing. The enclosing display pass captures the
/// complete window, including its text, so nested panels do not receive duplicate effects.
/// </remarks>
public sealed class CrtStyleBox : StyleBox
{
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color CornerColor { get; set; } = StyleNano.CrtGreen.WithAlpha(0.55f);

    public Thickness BorderThickness { get; set; }

    /// <summary>
    ///     L-shaped brackets inset from each corner. Kept where the scanlines and pixelation were
    ///     not: this is deliberate framing at a known position, not scattered noise.
    /// </summary>
    public bool DrawCornerTicks { get; set; } = true;

    public float CornerLength { get; set; } = 14f;

    public CrtStyleBox()
    {
    }

    public CrtStyleBox(CrtStyleBox other) : base(other)
    {
        BackgroundColor = other.BackgroundColor;
        BorderColor = other.BorderColor;
        CornerColor = other.CornerColor;
        BorderThickness = other.BorderThickness;
        DrawCornerTicks = other.DrawCornerTicks;
        CornerLength = other.CornerLength;
    }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var thickness = BorderThickness.Scale(uiScale);
        var inner = thickness.Deflate(box);

        CmuCrtWindowsUIController.DrawFill(handle, inner, BackgroundColor); // CMU14
        DrawBorder(handle, box, thickness);

        // Corner brackets are a CRT-theme flourish; the base theme gets a plain bordered panel.
        if (StyleNano.CrtUiEnabled && DrawCornerTicks)
            DrawCorners(handle, inner, uiScale);
    }

    protected override float GetDefaultContentMargin(Margin margin)
    {
        return margin switch
        {
            Margin.Top => BorderThickness.Top,
            Margin.Bottom => BorderThickness.Bottom,
            Margin.Right => BorderThickness.Right,
            Margin.Left => BorderThickness.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(margin), margin, null)
        };
    }

    private void DrawBorder(DrawingHandleScreen handle, UIBox2 box, Thickness thickness)
    {
        var (left, top, right, bottom) = thickness;

        if (left > 0)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Top, box.Left + left, box.Bottom), BorderColor);

        if (top > 0)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Top, box.Right, box.Top + top), BorderColor);

        if (right > 0)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Right - right, box.Top, box.Right, box.Bottom), BorderColor);

        if (bottom > 0)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Bottom - bottom, box.Right, box.Bottom), BorderColor);
    }

    private void DrawCorners(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var inset = MathF.Max(2f, uiScale * 2f);
        var line = MathF.Max(1f, uiScale);
        var length = MathF.Max(5f, CornerLength * uiScale);
        var left = box.Left + inset;
        var right = box.Right - inset;
        var top = box.Top + inset;
        var bottom = box.Bottom - inset;

        DrawCorner(handle, left, top, length, line, 1, 1);
        DrawCorner(handle, right, top, length, line, -1, 1);
        DrawCorner(handle, left, bottom, length, line, 1, -1);
        DrawCorner(handle, right, bottom, length, line, -1, -1);
    }

    private void DrawCorner(
        DrawingHandleScreen handle,
        float x,
        float y,
        float length,
        float line,
        int xDirection,
        int yDirection)
    {
        var horizontal = new UIBox2(
            MathF.Min(x, x + length * xDirection),
            MathF.Min(y, y + line * yDirection),
            MathF.Max(x, x + length * xDirection),
            MathF.Max(y, y + line * yDirection));

        var vertical = new UIBox2(
            MathF.Min(x, x + line * xDirection),
            MathF.Min(y, y + length * yDirection),
            MathF.Max(x, x + line * xDirection),
            MathF.Max(y, y + length * yDirection));

        CmuCrtWindowsUIController.DrawFill(handle, horizontal, CornerColor);
        CmuCrtWindowsUIController.DrawFill(handle, vertical, CornerColor);
    }
}

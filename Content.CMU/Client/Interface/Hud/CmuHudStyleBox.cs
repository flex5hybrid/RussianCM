using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Content.Client.Stylesheets;

namespace Content.Client.CMU14.Interface.Hud;

public enum CmuHudTone { Surface, Inset, Hover, Selected }

/// <summary>Live phosphor surface, shared by slots, controls and terminal panels.</summary>
public sealed class CmuHudStyleBox : StyleBox
{
    public CmuHudTone Tone { get; set; }
    public float Opacity { get; set; } = 1f;
    public bool Selected { get; set; }
    public bool OutlineOnly { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        if (box.Width <= 0 || box.Height <= 0)
            return;
        var line = MathF.Max(1, uiScale);
        if (!OutlineOnly)
        {
            var fill = Tone switch
            {
                CmuHudTone.Inset => CmuHudStyle.InsetColor,
                CmuHudTone.Hover => CmuHudStyle.HoverColor,
                CmuHudTone.Selected => CrtTerminalPalette.Surface3,
                _ => CmuHudStyle.SurfaceColor,
            };
            handle.DrawRect(box, fill.WithAlpha(Opacity));
        }

        var edge = Selected ? CmuHudStyle.SelectionColor : CmuHudStyle.EdgeColor;
        handle.DrawRect(new UIBox2(box.Left, box.Bottom - line, box.Right, box.Bottom), edge);
        if (StyleNano.CrtUiEnabled)
        {
            // Open corners leave the glass readable without a heavy frame around every slot.
            var length = MathF.Min(7 * uiScale, MathF.Min(box.Width, box.Height) / 3);
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + length, box.Top + line), edge);
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + line, box.Top + length), edge);
            handle.DrawRect(new UIBox2(box.Right - length, box.Top, box.Right, box.Top + line), edge);
            handle.DrawRect(new UIBox2(box.Right - line, box.Top, box.Right, box.Top + length), edge);
        }
        if (!Selected)
            return;

        handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + line, box.Bottom), edge);
        handle.DrawRect(new UIBox2(box.Right - line, box.Top, box.Right, box.Bottom), edge);
        var inset = MathF.Min(10 * uiScale, box.Width / 4);
        handle.DrawRect(new UIBox2(box.Left + inset, box.Bottom - 2 * line, box.Right - inset, box.Bottom), edge);
    }

    protected override float GetDefaultContentMargin(Margin margin) => 0;
}

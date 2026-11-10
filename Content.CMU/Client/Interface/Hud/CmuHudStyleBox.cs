using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client.CMU14.Interface.Hud;

public enum CmuHudTone { Surface, Inset, Hover, Selected, Header }

/// <summary>Thin terminal rules and reverse-video selection, following the live UI setup palette.</summary>
public sealed class CmuHudStyleBox : StyleBox
{
    public CmuHudTone Tone { get; set; }
    public float Opacity { get; set; } = 1f;
    public bool ExamineSurface { get; set; }
    public bool Selected { get; set; }
    public bool OutlineOnly { get; set; }
    public bool Inverse { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        if (!StyleNano.CrtUiEnabled)
            return;
        if (box.Width <= 0 || box.Height <= 0)
            return;
        var line = MathF.Min(MathF.Max(1, MathF.Round(uiScale)), MathF.Min(box.Width, box.Height) / 2);
        if (!OutlineOnly)
        {
            var fill = Inverse && Selected && StyleNano.CrtUiEnabled
                ? CmuHudStyle.SelectionColor
                : Tone switch
                {
                    CmuHudTone.Inset => CmuHudStyle.InsetColor,
                    CmuHudTone.Hover => CmuHudStyle.HoverColor,
                    CmuHudTone.Selected => StyleNano.CrtUiEnabled
                        ? CmuHudStyle.HoverColor : CrtTerminalPalette.Surface3,
                    CmuHudTone.Header => CmuHudStyle.HeaderColor,
                    _ => CmuHudStyle.SurfaceColor,
                };
            CmuCrtWindowsUIController.DrawFill(handle, box, fill.WithAlpha(ExamineSurface
                ? Math.Clamp(IoCManager.Resolve<Robust.Shared.Configuration.IConfigurationManager>().GetCVar(Content.Shared.CCVar.CCVars.CMUExamineOpacity), 0, 1)
                : Opacity));
        }

        var edge = Selected ? CmuHudStyle.SelectionColor : CmuHudStyle.EdgeColor;
        // Resting HUD cells retain their transparent fill and a quiet, complete perimeter.
        if (OutlineOnly && !Selected)
            edge = edge.WithAlpha(0.35f);

        // Whole rectangular rules describe table/window boundaries without decorative brackets.
        CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Top, box.Right, box.Top + line), edge);
        CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Bottom - line, box.Right, box.Bottom), edge);
        CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Top + line, box.Left + line, box.Bottom - line), edge);
        CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Right - line, box.Top + line, box.Right, box.Bottom - line), edge);
        if (Selected && OutlineOnly)
            CmuCrtWindowsUIController.DrawFill(handle, new UIBox2(box.Left, box.Top, box.Left + 2 * line, box.Bottom), edge);
    }

    protected override float GetDefaultContentMargin(Margin margin) => 0;
}

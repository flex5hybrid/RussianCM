using Content.Client.CMU14.Interface;
using Content.Client.CMU14.Interface.Hud;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface.Controls;

public partial class RadialMenuButton
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (!StyleNano.CrtUiEnabled || this is RadialMenuButtonWithSector || !HasStyleClass("RadialMenuButton"))
            return;
        var highlighted = DrawMode is DrawModeEnum.Hover or DrawModeEnum.Pressed;
        var box = PixelSizeBox;
        handle.DrawRect(box, highlighted ? CrtTerminalPalette.Surface3 : CmuHudStyle.InsetColor.WithAlpha(0.85f));
        handle.DrawRect(box, highlighted ? CmuHudStyle.SelectionColor : CmuHudStyle.EdgeColor.WithAlpha(0.6f), false);
        if (highlighted)
        {
            var line = MathF.Max(1, UIScale);
            handle.DrawRect(new UIBox2(box.Left, box.Bottom - 2 * line, box.Right, box.Bottom), CmuHudStyle.SelectionColor);
        }
    }
}

using System.Numerics;
using Content.Client._RMC14.Storage;
using Content.Client.CMU14.Storage;
using Content.Client.CMU14.Interface;
using Content.Client.CMU14.Interface.Hud;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class ItemGridPiece
{
    private CMUIconLabelPresentation? _cmuIconLabel;

    private static void CMUDrawPieceCell(DrawingHandleScreen handle, UIBox2 box,
        bool top, bool bottom, bool left, bool right, bool hovering, float uiScale)
    {
        // Draw only the exposed shape edges: multi-cell items remain one silhouette.
        CmuCrtWindowsUIController.DrawFill(handle, box,
            hovering ? CmuHudStyle.HoverColor : CrtTerminalPalette.Surface1);
        var color = hovering ? CmuHudStyle.SelectionColor : CmuHudStyle.EdgeColor;
        var line = MathF.Max(1, uiScale);
        if (top)
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Right, box.Top + line), color);
        if (bottom)
            handle.DrawRect(new UIBox2(box.Left, box.Bottom - line, box.Right, box.Bottom), color);
        if (left)
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + line, box.Bottom), color);
        if (right)
            handle.DrawRect(new UIBox2(box.Right - line, box.Top, box.Right, box.Bottom), color);
    }

    private void CMUDrawIconLabel(Vector2 position, DrawingHandleScreen handle)
    {
        _entityManager.System<RMCIconLabelSystem>().DrawStorage(Entity, UIScale, position, handle, ref _cmuIconLabel);
    }
}

using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>One resize-driven layout policy shared by overlay and separated-chat game screens.</summary>
public sealed class CmuHudScreenCoordinator
{
    private readonly LayoutContainer _viewport;
    private readonly ActionsBar _actions;
    private readonly InventoryGui _inventory;
    private readonly HotbarGui _hotbar;
    private readonly AlertsUI _alerts;
    private readonly GhostGui _ghost;
    private bool _applying;
    public bool StatusOnLeft { get; set; }
    public float RightInset { get; set; }
    public float LeftInset { get; set; }
    /// <summary>Chat bounds in the overlay HUD's layout parent; separated chat leaves this unset.</summary>
    public UIBox2? OverlayChatBounds { get; set; }

    public CmuHudScreenCoordinator(LayoutContainer viewport, ActionsBar actions, InventoryGui inventory,
        HotbarGui hotbar, AlertsUI alerts, GhostGui ghost)
    {
        _viewport = viewport;
        _actions = actions;
        _inventory = inventory;
        _hotbar = hotbar;
        _alerts = alerts;
        _ghost = ghost;
        viewport.OnResized += Apply;
        inventory.OnResized += Apply;
        hotbar.OnResized += Apply;
        hotbar.DockLayoutChanged += Apply;
        alerts.OnResized += Apply;
        ghost.OnResized += Apply;
    }

    public void Apply()
    {
        if (_applying || _viewport.Width <= 0 || _viewport.Height <= 0)
            return;
        _applying = true;
        try
        {
            var footer = _hotbar.FooterHeight;
            var bottom = footer + 24;
            var footerRight = RightInset;
            var statusRight = RightInset;
            var actionsRight = RightInset;
            var railTop = MathF.Max(12, _actions.Height + 24);
            var railBottom = MathF.Max(railTop + 48, _viewport.Height - bottom - 12);
            if (OverlayChatBounds is { } chat)
            {
                var chatInset = MathF.Max(0, _viewport.Width - chat.Left + 12);
                actionsRight = MathF.Max(actionsRight, chatInset);
                // A short floating chat does not occupy the footer or the whole right edge.
                if (chat.Bottom > _viewport.Height - bottom - 12)
                    footerRight = MathF.Max(footerRight, chatInset);
                if (!StatusOnLeft)
                {
                    var desiredRail = MathF.Max(48, MathF.Min(_alerts.DesiredSize.Y, _viewport.Height * 0.45f));
                    if (railBottom - chat.Bottom - 12 >= desiredRail)
                        railTop = MathF.Max(railTop, chat.Bottom + 12);
                    else
                        statusRight = MathF.Max(statusRight, chatInset);
                }
            }
            var width = MathF.Max(200, _viewport.Width - footerRight - LeftInset);
            LayoutContainer.SetMarginLeft(_hotbar, 84 + LeftInset);
            LayoutContainer.SetMarginRight(_hotbar, -12 - footerRight);
            LayoutContainer.SetMarginBottom(_hotbar, -5);
            LayoutContainer.SetMarginTop(_hotbar, -5);
            LayoutContainer.SetMarginLeft(_ghost, 12 + LeftInset);
            LayoutContainer.SetMarginRight(_ghost, -12 - footerRight);
            LayoutContainer.SetGrowHorizontal(_ghost, LayoutContainer.GrowDirection.Constrain);
            LayoutContainer.SetGrowVertical(_ghost, LayoutContainer.GrowDirection.Begin);
            _ghost.SetAvailableWidth(width - 24);
            _inventory.SetAvailableHeight(_viewport.Height - bottom - _actions.Height - 20);
            _inventory.SetDockHeight(_hotbar.InventoryClearanceHeight);
            var equipmentLeft = MathF.Max(12 + LeftInset, _hotbar.Position.X + _hotbar.DockContentLeft - 72);
            LayoutContainer.SetMarginLeft(_inventory, equipmentLeft);
            LayoutContainer.SetMarginBottom(_inventory, -5);
            LayoutContainer.SetMarginTop(_inventory, -5);
            // A narrow instrument rail belongs to the side, independent of the equipment drawer.
            var railHeight = MathF.Max(48, railBottom - railTop);
            LayoutContainer.SetAnchorPreset(_alerts,
                StatusOnLeft ? LayoutContainer.LayoutPreset.CenterLeft : LayoutContainer.LayoutPreset.CenterRight);
            LayoutContainer.SetGrowHorizontal(_alerts,
                StatusOnLeft ? LayoutContainer.GrowDirection.End : LayoutContainer.GrowDirection.Begin);
            var railCenter = (railTop + railBottom) / 2 - _viewport.Height / 2;
            LayoutContainer.SetMarginBottom(_alerts, railCenter);
            LayoutContainer.SetMarginTop(_alerts, railCenter);
            if (!StatusOnLeft)
            {
                LayoutContainer.SetMarginRight(_alerts, -12 - statusRight);
                LayoutContainer.SetMarginLeft(_alerts, -12 - statusRight);
            }
            else
            {
                var left = 8 + LeftInset;
                LayoutContainer.SetMarginLeft(_alerts, left);
                LayoutContainer.SetMarginRight(_alerts, left);
            }
            LayoutContainer.SetGrowVertical(_alerts, LayoutContainer.GrowDirection.Both);
            _alerts.SetHudSpace(MathF.Min(railHeight, _viewport.Height * 0.45f), true);
            var actionsWidth = MathF.Max(64, _viewport.Width - actionsRight - LeftInset - 24);
            _actions.ActionsContainer.MaxGridWidth = actionsWidth;
            _actions.ActionsContainer.MaxWidth = actionsWidth;
            // Ghost actions keep their original lower-centre position. They do not use the
            // living character's item/storage height, which can be zero while spectating.
            LayoutContainer.SetMarginBottom(_ghost, -80);
            LayoutContainer.SetMarginTop(_ghost, -80);
        }
        finally
        {
            _applying = false;
        }
    }
}

using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Viewport anchors independent of inline storage size and equipment visibility.</summary>
public sealed class CmuHudScreenCoordinator
{
    private readonly LayoutContainer _viewport;
    private readonly ActionsBar _actions;
    private readonly GameTopMenuBar _menu;
    private readonly InventoryGui _inventory;
    private readonly HotbarGui _hotbar;
    private readonly AlertsUI _alerts;
    private readonly GhostGui _ghost;
    private bool _applying;
    public bool StatusOnLeft { get; set; }
    public float RightInset { get; set; }
    public float LeftInset { get; set; }
    public UIBox2? OverlayChatBounds { get; set; }

    public CmuHudScreenCoordinator(LayoutContainer viewport, ActionsBar actions, InventoryGui inventory,
        HotbarGui hotbar, AlertsUI alerts, GhostGui ghost, GameTopMenuBar menu)
    {
        _viewport = viewport;
        _actions = actions;
        _inventory = inventory;
        _hotbar = hotbar;
        _alerts = alerts;
        _ghost = ghost;
        _menu = menu;
        viewport.OnResized += Apply;
        menu.OnResized += Apply;
        actions.OnResized += Apply;
        alerts.OnResized += Apply;
        hotbar.DockLayoutChanged += Apply;
        // Both game screens live for the UI manager's lifetime, including their coordinators.
        IoCManager.Resolve<IStylesheetManager>().CrtThemeChanged += Apply;
    }

    public void Apply()
    {
        if (_applying || _viewport.Width <= 0 || _viewport.Height <= 0)
            return;
        _applying = true;
        try
        {
            var width = MathF.Max(64, _viewport.Width - RightInset - 24);
            var menuWidth = OverlayChatBounds is { } bounds ? MathF.Max(64, bounds.Left - 24) : width;
            var menuLimit = StyleNano.CrtUiEnabled ? 456 : 650;
            _menu.SetWidth = MathF.Min(menuLimit, menuWidth);
            _menu.SetAvailableWidth(MathF.Min(menuLimit, menuWidth));
            var top = MathF.Max(64, _menu.Height) + 18;
            var footer = MathF.Max(64, _hotbar.FooterHeight);
            // Keep the targeting doll above the equipment toggle at the foot of the left rail.
            var targetingSpace = StatusOnLeft ? 112 : 0;
            var statusHeight = MathF.Max(64, MathF.Min(420, _alerts.HudContentHeight));
            var availableActions = MathF.Max(68, _viewport.Height - top - footer - 24
                - (StatusOnLeft ? statusHeight + targetingSpace + 16 : 0));
            var count = _actions.ActionsContainer.ChildCount;
            var preferredRows = count <= 6 ? count : Math.Min(7, (count + 1) / 2);
            var actionRows = Math.Clamp(preferredRows, 1, Math.Max(1, (int) (availableActions / 68)));
            _actions.ActionsContainer.Rows = StyleNano.CrtUiEnabled ? actionRows : 1;
            _inventory.SetAvailableHeight(_viewport.Height - top - 80);
            LayoutContainer.SetMarginLeft(_inventory, 12);
            LayoutContainer.SetMarginTop(_inventory, -8);
            LayoutContainer.SetMarginBottom(_inventory, -8);

            // Control.Margin clamps its deflated rectangle. Fixed LayoutContainer margins with
            // GrowDirection.Constrain can instead create a negative width before resize callbacks run.
            LayoutContainer.SetMarginLeft(_hotbar, 0);
            LayoutContainer.SetMarginRight(_hotbar, 0);
            var dockInset = MathF.Min(StyleNano.CrtUiEnabled ? 84 : 5, _viewport.Width / 4);
            _hotbar.Margin = new Thickness(dockInset, 0, dockInset + MathF.Max(0, RightInset), 0);
            LayoutContainer.SetMarginTop(_hotbar, -8);
            LayoutContainer.SetMarginBottom(_hotbar, -8);
            LayoutContainer.SetMarginLeft(_ghost, 0);
            LayoutContainer.SetMarginRight(_ghost, 0);
            _ghost.Margin = new Thickness(12, 0, 12 + MathF.Max(0, RightInset), 0);
            LayoutContainer.SetGrowHorizontal(_ghost, LayoutContainer.GrowDirection.Constrain);
            LayoutContainer.SetGrowVertical(_ghost, LayoutContainer.GrowDirection.Begin);
            _ghost.SetAvailableWidth(width);
            LayoutContainer.SetMarginTop(_ghost, -80);
            LayoutContainer.SetMarginBottom(_ghost, -80);

            // A fixed top anchor prevents additional storage rows from moving status icons.
            var railTop = StatusOnLeft
                ? top + MathF.Max(_actions.Height, actionRows * 68) + 12
                : OverlayChatBounds is { } chat ? chat.Bottom + 12 : MathF.Max(top, _viewport.Height * 0.45f);
            var railHeight = MathF.Max(64, _viewport.Height - railTop - footer - targetingSpace - 24);
            LayoutContainer.SetAnchorPreset(_alerts, StatusOnLeft
                ? LayoutContainer.LayoutPreset.TopLeft : LayoutContainer.LayoutPreset.TopRight);
            LayoutContainer.SetGrowHorizontal(_alerts, StatusOnLeft
                ? LayoutContainer.GrowDirection.End : LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetGrowVertical(_alerts, LayoutContainer.GrowDirection.End);
            var side = StatusOnLeft ? 8 : -8 - RightInset;
            LayoutContainer.SetMarginLeft(_alerts, side);
            LayoutContainer.SetMarginRight(_alerts, side);
            LayoutContainer.SetMarginTop(_alerts, railTop);
            LayoutContainer.SetMarginBottom(_alerts, railTop);
            _alerts.SetHudSpace(railHeight, true);
        }
        finally
        {
            _applying = false;
        }
    }
}

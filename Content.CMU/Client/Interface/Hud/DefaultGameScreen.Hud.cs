using System.Numerics;
using Content.Client.CMU14.Interface.Hud;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Screens;

public sealed partial class DefaultGameScreen
{
    private CmuHudScreenCoordinator _cmuHud = default!;
    private Vector2? _cmuPendingChatSize;
    private Vector2? _cmuExpandedChatSize;
    private bool _cmuChatSized;
    private bool _cmuChatBottomPinned = true;
    private bool _cmuExpandedChatBottomPinned;
    private bool _cmuApplyingLayout;

    private void CMUInitializeHud()
    {
        SetAnchorAndMarginPreset(TopBar, LayoutPreset.TopRight, margin: 10);
        SetGrowHorizontal(TopBar, GrowDirection.Begin);
        // Default HUD widgets are siblings of ViewportContainer, so their coordinate space is this screen.
        _cmuHud = new CmuHudScreenCoordinator(this, Actions, Inventory, Hotbar, Alerts, Ghost);
        Chat.EnableHudWorkspace();
        SetGrowHorizontal(Chat, GrowDirection.Begin);
        SetGrowVertical(Chat, GrowDirection.End);
        Chat.HudCompactModeChanged += CMUOnChatCompact;
        TopBar.OnResized += CMUUpdateHudLayout;
        OnResized += CMUOnScreenResized;
    }

    private void CMUSetChatSize(Vector2 size)
    {
        // The chat controller can restore its CVar before this screen has been laid out.
        _cmuPendingChatSize = size;
        CMUUpdateHudLayout();
    }

    private void CMURememberChatSizing()
    {
        _cmuChatBottomPinned = Chat.GetValue<float>(MarginBottomProperty) >= Height - 16;
    }

    private void CMUOnScreenResized()
    {
        if (_cmuChatSized && _cmuChatBottomPinned && _cmuPendingChatSize == null)
            _cmuPendingChatSize = new Vector2(Height - 12, Chat.GetValue<float>(MarginLeftProperty));
        CMUUpdateHudLayout();
    }

    private void CMUOnChatCompact(bool compact)
    {
        if (compact)
        {
            _cmuExpandedChatBottomPinned = _cmuChatBottomPinned;
            _cmuExpandedChatSize = new Vector2(Chat.GetValue<float>(MarginBottomProperty),
                Chat.GetValue<float>(MarginLeftProperty));
            CMUSetChatSize(new Vector2(MathF.Min(Height - 12, 330), -270));
        }
        else
        {
            var expanded = _cmuExpandedChatSize ?? new Vector2(Height - 12, -370);
            if (_cmuExpandedChatBottomPinned)
                expanded.X = Height - 12;
            CMUSetChatSize(expanded);
        }
    }

    private void CMUUpdateHudLayout()
    {
        if (_cmuApplyingLayout || _cmuHud == null || Width <= 0 || Height <= 0)
            return;
        _cmuApplyingLayout = true;
        try
        {
            var compact = Chat.HudCompactMode;
            var pending = _cmuPendingChatSize;
            var requested = pending ?? (_cmuChatSized
                ? new Vector2(Chat.GetValue<float>(MarginBottomProperty), Chat.GetValue<float>(MarginLeftProperty))
                : new Vector2(Height - 12, -Math.Clamp(Width * 0.29f, 320, 420) - 10));
            _cmuPendingChatSize = null;

            var maxWidth = MathF.Max(1, Width - 20);
            var minWidth = MathF.Min(compact ? 240 : 320, maxWidth);
            maxWidth = MathF.Max(minWidth, MathF.Min(maxWidth, Width * 0.55f));
            var width = float.IsFinite(requested.Y) && requested.Y < -10 ? -requested.Y - 10 : 360;
            width = Math.Clamp(width, minWidth, maxWidth);
            // TopRight margins must describe the new width as well: SetWidth alone leaves
            // the old left edge fixed and pushes the strip beyond the screen's right edge.
            SetMarginLeft(TopBar, -width - 10);
            SetMarginRight(TopBar, -10);
            TopBar.SetWidth = width;

            var top = MathF.Max(48, TopBar.Height) + 18;
            top = MathF.Min(top, MathF.Max(0, Height - 60));
            var maxBottom = MathF.Max(top + 1, Height - 12);
            var minHeight = MathF.Min(compact ? 220 : 420, maxBottom - top);
            Chat.MinSize = new Vector2(minWidth, minHeight);
            var bottom = float.IsFinite(requested.X) && requested.X > 0 ? requested.X : maxBottom;
            bottom = Math.Clamp(bottom, top + minHeight, maxBottom);
            SetMarginTop(Chat, top);
            SetMarginLeft(Chat, -width - 10);
            SetMarginRight(Chat, -10);
            SetMarginBottom(Chat, bottom);
            if (pending != null || !_cmuChatSized)
                _cmuChatBottomPinned = bottom >= maxBottom - 4;
            _cmuChatSized = true;

            _cmuHud.OverlayChatBounds = new UIBox2(Width - width - 10, top, Width - 10, bottom);
            _cmuHud.Apply();
        }
        finally
        {
            _cmuApplyingLayout = false;
        }
    }
}

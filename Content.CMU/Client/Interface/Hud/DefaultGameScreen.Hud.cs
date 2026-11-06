using System.Numerics;
using Content.Client.CMU14.Interface.Hud;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Screens;

public sealed partial class DefaultGameScreen
{
    private CmuHudScreenCoordinator _cmuHud = default!;
    private Vector2? _cmuPendingChatSize;
    private bool _cmuChatSized;
    private bool _cmuApplyingLayout;

    private void CMUInitializeHud()
    {
        _cmuHud = new CmuHudScreenCoordinator(this, Actions, Inventory, Hotbar, Alerts, Ghost, TopBar)
        {
            StatusOnLeft = true,
        };
        Chat.EnableHudWorkspace();
        SetGrowHorizontal(Chat, GrowDirection.Begin);
        SetGrowVertical(Chat, GrowDirection.End);
        OnResized += CMUUpdateHudLayout;
    }

    private void CMUSetChatSize(Vector2 size)
    {
        _cmuPendingChatSize = size;
        CMUUpdateHudLayout();
    }

    private void CMUUpdateHudLayout()
    {
        if (_cmuApplyingLayout || _cmuHud == null || Width <= 0 || Height <= 0)
            return;
        _cmuApplyingLayout = true;
        try
        {
            var requested = _cmuPendingChatSize ?? (_cmuChatSized
                ? new Vector2(Chat.GetValue<float>(MarginBottomProperty), Chat.GetValue<float>(MarginLeftProperty))
                : new Vector2(Height * 0.52f, -Math.Clamp(Width * 0.29f, 320, 420) - 10));
            _cmuPendingChatSize = null;
            var maxWidth = MathF.Max(1, Width - 100);
            var minWidth = MathF.Min(320, maxWidth);
            maxWidth = MathF.Max(minWidth, MathF.Min(maxWidth, Width * 0.55f));
            var width = float.IsFinite(requested.Y) && requested.Y < -10 ? -requested.Y - 10 : 360;
            width = Math.Clamp(width, minWidth, maxWidth);
            // Keep the lower part of the game view open beneath the floating chat.
            var maxBottom = MathF.Max(11, Height - 288);
            var minHeight = MathF.Min(220, maxBottom - 10);
            Chat.MinSize = new Vector2(minWidth, minHeight);
            var bottom = float.IsFinite(requested.X) && requested.X > 0 ? requested.X : Height * 0.52f;
            bottom = Math.Clamp(bottom, 10 + minHeight, maxBottom);
            SetMarginTop(Chat, 10);
            SetMarginLeft(Chat, -width - 10);
            SetMarginRight(Chat, -10);
            SetMarginBottom(Chat, bottom);
            _cmuChatSized = true;
            _cmuHud.OverlayChatBounds = new UIBox2(Width - width - 10, 10, Width - 10, bottom);
            _cmuHud.Apply();
        }
        finally
        {
            _cmuApplyingLayout = false;
        }
    }
}

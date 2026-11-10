using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Chat;

/// <summary>Legacy chat uses immediate wheel steps, without the native delayed scroll target.</summary>
public sealed class CmuChatOutputPanel : OutputPanel
{
    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        if (args.Delta.Y == 0)
            return;

        var bar = FindControl<VScrollBar>("_v_scroll");
        // OutputPanel's scrollbar works in physical pixels; its value event also updates following.
        bar.Value = bar.ValueTarget - args.Delta.Y * 96f * UIScale;
        args.Handle();
    }
}

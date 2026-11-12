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

        // RuMC edit start: FindControl can't see code-created children
        VScrollBar? bar = null;
        foreach (var child in Children)
        {
            if (child is VScrollBar found)
            {
                bar = found;
                break;
            }
        }

        if (bar == null)
        {
            base.MouseWheel(args);
            return;
        }
        // RuMC edit end

        // OutputPanel's scrollbar works in physical pixels; its value event also updates following.
        bar.Value = bar.ValueTarget - args.Delta.Y * 96f * UIScale;
        args.Handle();
    }
}

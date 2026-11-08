using System.Numerics;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Screens;

/// <summary>
///     Screens that are considered to be 'in-game'.
/// </summary>
public abstract class InGameScreen : UIScreen
{
    // CMU14: custom placement is the final layout pass, preserving native input and ownership.
    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var controller = UserInterfaceManager.GetUIController<Content.Client.CMU14.Interface.Hud.CmuHudLayoutController>();
        controller.RestoreNatural(this);
        var result = base.ArrangeOverride(finalSize);
        controller.Arrange(this, finalSize);
        return result;
    }

    public Action<Vector2>? OnChatResized;

    public abstract ChatBox ChatBox { get; }

    public abstract void SetChatSize(Vector2 size);
}

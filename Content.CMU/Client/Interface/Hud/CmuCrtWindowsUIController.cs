using Content.Client.ContextMenu.UI;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Ordinary windows need a CRT pass as well as the terminal stylesheet.</summary>
[UsedImplicitly]
public sealed class CmuCrtWindowsUIController : UIController
{
    public override void Initialize()
    {
        base.Initialize();
        // UI controllers live for the UI manager's lifetime, like the close-window controller.
        WatchRoot(UIManager.WindowRoot);
        WatchRoot(UIManager.ModalRoot);
        WatchRoot(UIManager.PopupRoot);
    }

    private static void WatchRoot(Control root)
    {
        root.OnChildAdded += AttachEffect;
        foreach (var child in root.Children)
            AttachEffect(child);
    }

    private static void AttachEffect(Control control)
    {
        // Radials deliberately have transparent space outside their sectors. A rectangular
        // overlay there would darken the game world, including the centre's cancel target.
        if (control is RadialMenu || control is not BaseWindow && control is not ContextMenuPopup)
            return;
        if (HasSurfaceEffect(control))
            return;

        control.AddChild(new CrtScanlineOverlay
        {
            Name = "CmuWindowCrtEffect",
            MenuSurface = true,
            Glass = true,
        });
    }

    private static bool HasSurfaceEffect(Control control)
    {
        if (control is CrtScreenControl or CrtScanlineOverlay)
            return true;
        foreach (var child in control.Children)
        {
            if (HasSurfaceEffect(child))
                return true;
        }
        return false;
    }
}

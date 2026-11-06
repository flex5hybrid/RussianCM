using System.Linq;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Give native windows one CRT pass over their complete chrome and content.</summary>
[UsedImplicitly]
public sealed class CmuCrtWindowsUIController : UIController
{
    private readonly HashSet<Control> _pending = new();

    public override void Initialize()
    {
        Watch(UIManager.WindowRoot);
        Watch(UIManager.ModalRoot);
        Watch(UIManager.PopupRoot);
    }

    private void Watch(Control root)
    {
        root.OnChildAdded += Queue;
        root.OnChildRemoved += Removed;
        foreach (var child in root.Children)
            Queue(child);
    }

    private void Queue(Control control)
    {
        if (control is DefaultWindow or FancyWindow)
            _pending.Add(control);
    }

    private void Removed(Control control) => _pending.Remove(control);

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_pending.Count == 0)
            return;
        var pending = _pending.ToArray();
        _pending.Clear();
        foreach (var window in pending)
        {
            if (window.Parent == null || window.Children.Any(child => child is CmuCrtWindowSurface))
                continue;

            // These native window types use Control's overlay measure/arrange. The neutral
            // surface preserves that layout; the BaseWindow keeps its parent and bounds.
            // Install once outside rendering, after construction, not on each settings change.
            var surface = new CmuCrtWindowSurface();
            foreach (var child in window.Children.ToArray())
            {
                window.RemoveChild(child);
                surface.AddChild(child);
            }
            window.AddChild(surface);
        }
    }

    // Chrome renders normally into its enclosing capture, never through a second material.
    public static void DrawFill(DrawingHandleScreen handle, UIBox2 box, Color colour)
    {
        if (box.Width > 0 && box.Height > 0 && colour.A > 0)
            handle.DrawRect(box, colour);
    }

    public static void DrawShape(DrawingHandleScreen handle, Action draw) => draw();
}

/// <summary>Control's native overlay layout, with a full-subtree display pass.</summary>
internal sealed class CmuCrtWindowSurface : Control
{
    private readonly CmuCrtCapture _capture = new();

    public CmuCrtWindowSurface()
    {
        MouseFilter = MouseFilterMode.Pass;
    }

    protected override void Draw(IRenderHandle handle)
    {
        if (!_capture.Draw(this, handle, true, false, 1))
            base.Draw(handle);
    }

    protected override void RenderChildOverride(ref ControlRenderArguments args, int childIndex, Vector2i position)
    {
        if (CmuCrtCapture.Capturing || !_capture.Drawn)
            base.RenderChildOverride(ref args, childIndex, position);
    }

    protected override void ExitedTree()
    {
        _capture.Release();
        base.ExitedTree();
    }
}

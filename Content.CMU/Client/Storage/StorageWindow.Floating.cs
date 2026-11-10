using System.Numerics;
using Content.Shared.Storage;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class StorageWindow
{
    private Control? _cmuFloatingRoot;
    private bool _cmuClamping;

    /// <summary>Populate the grid before BaseWindow measures it for initial placement.</summary>
    public void CMUPrepareForOpen(Entity<StorageComponent> storage)
    {
        UpdateContainer(storage);
        BuildItemPieces();
        _isDirty = false;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        if (Parent is not { } root || root != UserInterfaceManager.WindowRoot)
            return;

        _cmuFloatingRoot = root;
        root.OnResized += CMUConstrainFloating;
        LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.TopLeft);
        LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.End);
        LayoutContainer.SetGrowVertical(this, LayoutContainer.GrowDirection.End);
        RectClipContent = true;
        CMUConstrainFloating();
    }

    protected override void ExitedTree()
    {
        _cmuCapture.Release();
        if (_cmuFloatingRoot != null)
            _cmuFloatingRoot.OnResized -= CMUConstrainFloating;
        _cmuFloatingRoot = null;
        base.ExitedTree();
    }

    protected override void Resized()
    {
        base.Resized();
        CMUConstrainFloating();
    }

    private void CMUConstrainFloating()
    {
        if (_cmuClamping || _cmuFloatingRoot is not { } root || root.Size.X <= 0 || root.Size.Y <= 0)
            return;
        _cmuClamping = true;
        try
        {
            if (MaxSize != root.Size)
                MaxSize = root.Size;
            var extent = Vector2.Min(Vector2.Max(Size, DesiredSize), root.Size);
            var maximum = Vector2.Max(Vector2.Zero, root.Size - extent);
            var position = Vector2.Clamp(Position, Vector2.Zero, maximum);
            if (Position != position)
                LayoutContainer.SetPosition(this, position);
        }
        finally
        {
            _cmuClamping = false;
        }
    }
}

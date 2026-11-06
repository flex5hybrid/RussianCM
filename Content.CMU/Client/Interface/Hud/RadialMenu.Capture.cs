using Content.Client.CMU14.Interface;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface.Controls;

public partial class RadialMenu
{
    private readonly CmuCrtCapture _cmuCapture = new();

    protected override void Draw(IRenderHandle handle)
    {
        if (!_cmuCapture.Draw(this, handle, true, false, 1))
            base.Draw(handle);
    }

    protected override void RenderChildOverride(ref ControlRenderArguments args, int childIndex, Vector2i position)
    {
        if (CmuCrtCapture.Capturing || !_cmuCapture.Drawn)
            base.RenderChildOverride(ref args, childIndex, position);
    }

    protected override void ExitedTree()
    {
        _cmuCapture.Release();
        base.ExitedTree();
    }
}

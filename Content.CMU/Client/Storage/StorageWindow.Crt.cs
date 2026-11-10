using Content.Client.CMU14.Interface;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class StorageWindow
{
    private readonly CmuCrtCapture _cmuCapture = new();

    protected override void Draw(IRenderHandle handle)
    {
        if (!_cmuCapture.Draw(this, handle, false, false, 1))
            base.Draw(handle);
    }

    protected override void RenderChildOverride(ref ControlRenderArguments args, int childIndex, Vector2i position)
    {
        if (CmuCrtCapture.Capturing || !_cmuCapture.Drawn)
            base.RenderChildOverride(ref args, childIndex, position);
    }
}

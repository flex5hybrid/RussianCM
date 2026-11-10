using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface;

/// <summary>Draws the complete live panel, including text, through the CRT display shader.</summary>
[Virtual]
public class CmuCrtDisplayPanel : PanelContainer
{
    private readonly CmuCrtCapture _capture = new();
    public bool EffectEnabled { get; set; } = true;
    public bool ChatSurface { get; set; }
    public bool MenuSurface { get; set; }
    public bool Raster { get; set; } = true;
    public float IntensityScale { get; set; } = 1f;

    protected override void Draw(IRenderHandle handle)
    {
        if (!_capture.Draw(this, handle, EffectEnabled && Raster, ChatSurface, IntensityScale))
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

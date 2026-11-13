using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.DonorCapes;

public sealed class DonorCapePreviewControl : Control
{
    private readonly SpriteSystem _spriteSystem;
    private IRsiStateLike? _state;
    private int _frameStart;
    private int _frameCount;
    private int _frame;
    private float _frameTime;
    private RsiDirection _direction;

    public TextureRect DisplayRect { get; }

    public DonorCapePreviewControl(SpriteSystem spriteSystem)
    {
        _spriteSystem = spriteSystem;
        DisplayRect = new TextureRect();
        AddChild(DisplayRect);
    }

    public void SetFromSpriteSpecifier(SpriteSpecifier specifier)
    {
        _state = _spriteSystem.RsiStateLike(specifier);
        _direction = _state.RsiDirections == RsiDirectionType.Dir1
            ? RsiDirection.South
            : RsiDirection.North;
        var range = DonorCapePreviewAnimation.GetBackViewFrameRange(
            _state.AnimationFrameCount,
            _state.RsiDirections);
        _frameStart = range.Start;
        _frameCount = range.Count;
        _frame = _frameStart;
        _frameTime = _state.GetDelay(_frame);
        DisplayRect.Texture = _state.GetFrame(_direction, _frame);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (!VisibleInTree || _state == null || _frameCount <= 1)
            return;

        var oldFrame = _frame;
        _frameTime -= args.DeltaSeconds;
        while (_frameTime < _state.GetDelay(_frame))
        {
            _frame = _frameStart + (_frame - _frameStart + 1) % _frameCount;
            _frameTime += _state.GetDelay(_frame);
        }

        if (_frame != oldFrame)
            DisplayRect.Texture = _state.GetFrame(_direction, _frame);
    }
}

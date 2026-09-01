using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.PhysicsSystem.Controllers;

public sealed partial class MoverController
{
    private const float MouseLookSensitivity = 0.0025f;
    private const float LookSendInterval = 1f / 60f;

    [Dependency] private IClyde _firstPersonClyde = default!;
    [Dependency] private IInputManager _firstPersonInput = default!;
    [Dependency] private World3DGridRenderingSystem _world3D = default!;

    private bool _mouseLookCaptured;
    private bool _lookYawDirty;
    private float _lookSendAccumulator;
    private float _lookPitch = World3DGridRenderingSystem.DefaultFirstPersonPitch;

    private void InitializeFirstPersonMouseLook()
    {
        _firstPersonClyde.MouseMove += OnFirstPersonMouseMove;
        _firstPersonClyde.OnWindowFocused += OnFirstPersonWindowFocused;
        _firstPersonInput.FirstChanceOnKeyEvent += OnFirstPersonKeyEvent;
    }

    private void ShutdownFirstPersonMouseLook()
    {
        SetMouseLookCaptured(false);
        _firstPersonClyde.MouseMove -= OnFirstPersonMouseMove;
        _firstPersonClyde.OnWindowFocused -= OnFirstPersonWindowFocused;
        _firstPersonInput.FirstChanceOnKeyEvent -= OnFirstPersonKeyEvent;
    }

    private void OnFirstPersonPlayerAttached(
        Entity<InputMoverComponent> entity,
        ref LocalPlayerAttachedEvent args)
    {
        _lookPitch = World3DGridRenderingSystem.DefaultFirstPersonPitch;
        _world3D.SetFirstPersonPitch(_lookPitch);
        SetMouseLookCaptured(true);
    }

    private void OnFirstPersonPlayerDetached(
        Entity<InputMoverComponent> entity,
        ref LocalPlayerDetachedEvent args)
    {
        SetMouseLookCaptured(false);
        _lookYawDirty = false;
    }

    private void OnFirstPersonMouseMove(MouseMoveEventArgs args)
    {
        if (!_mouseLookCaptured || _playerManager.LocalEntity is not { Valid: true } player)
            return;

        // SDL reports positive X to the right and positive Y down. Keep both axes in the
        // conventional FPS orientation: right turns right, up looks up.
        var yawDelta = new Angle(args.Relative.X * MouseLookSensitivity);
        RotateCamera(player, yawDelta);

        _lookPitch = Math.Clamp(
            _lookPitch - args.Relative.Y * MouseLookSensitivity,
            -1.35f,
            1.35f);
        _world3D.SetFirstPersonPitch(_lookPitch);
        _lookYawDirty = true;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_lookYawDirty || _playerManager.LocalEntity is not { Valid: true } player)
            return;

        _lookSendAccumulator += frameTime;
        if (_lookSendAccumulator < LookSendInterval || !TryComp(player, out InputMoverComponent? mover))
            return;

        _lookSendAccumulator = 0f;
        _lookYawDirty = false;
        RaisePredictiveEvent(new RequestFirstPersonLookEvent
        {
            Yaw = mover.TargetRelativeRotation,
        });
    }

    private void OnFirstPersonKeyEvent(KeyEventArgs args, KeyEventType type)
    {
        if (type != KeyEventType.Down)
            return;

        if (args.Key == Keyboard.Key.Escape && _mouseLookCaptured)
        {
            SetMouseLookCaptured(false);
            return;
        }

        if (args.Key != Keyboard.Key.F8 || _playerManager.LocalEntity is not { Valid: true })
            return;

        SetMouseLookCaptured(!_mouseLookCaptured);
        args.Handle();
    }

    private void OnFirstPersonWindowFocused(WindowFocusedEventArgs args)
    {
        if (args.Window == _firstPersonClyde.MainWindow && !args.Focused)
            SetMouseLookCaptured(false);
    }

    private void SetMouseLookCaptured(bool captured)
    {
        if (_mouseLookCaptured == captured)
            return;

        _mouseLookCaptured = captured;
        _firstPersonClyde.MainWindow.SetRelativeMouseMode(captured);
    }
}

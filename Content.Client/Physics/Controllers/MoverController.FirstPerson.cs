using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.PhysicsSystem.Controllers;

public sealed partial class MoverController
{
    private const float MouseLookSensitivity = 0.0025f;
    private const float LookSendInterval = 1f / 30f;

    [Dependency] private IClyde _firstPersonClyde = default!;
    [Dependency] private IInputManager _firstPersonInput = default!;
    [Dependency] private World3DGridRenderingSystem _world3D = default!;
    [Dependency] private FirstPersonLookClientSystem _firstPersonLookNet = default!;
    [Dependency] private SharedTransformSystem _firstPersonTransform = default!;

    private bool _mouseLookCaptured;
    private bool _lookYawDirty;
    private float _lookSendAccumulator;
    private Angle _lookYaw;
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
        _lookYaw = (entity.Comp.FirstPersonMode
            ? entity.Comp.FirstPersonYaw
            : GetWorldYawFromLegacyMover(entity.Comp)).Reduced();
        _lookPitch = World3DGridRenderingSystem.DefaultFirstPersonPitch;
        _lookYawDirty = false;
        _lookSendAccumulator = 0f;

        ApplyFirstPersonYaw(entity.Owner);
        _world3D.SetFirstPersonPitch(_lookPitch);
        _firstPersonLookNet.Send((float) _lookYaw.Theta);
        SetMouseLookCaptured(true);
    }

    private void OnFirstPersonPlayerDetached(
        Entity<InputMoverComponent> entity,
        ref LocalPlayerDetachedEvent args)
    {
        entity.Comp.FirstPersonMode = false;
        SetMouseLookCaptured(false);
        _lookYawDirty = false;
        _lookSendAccumulator = 0f;
    }

    private void OnFirstPersonMouseMove(MouseMoveEventArgs args)
    {
        if (!_mouseLookCaptured || _playerManager.LocalEntity is not { Valid: true } player)
            return;

        // SDL reports positive X to the right and positive Y down. Yaw is now an independent
        // first-person value; it no longer goes through RotateCamera/TargetRelativeRotation lerping.
        _lookYaw = (_lookYaw + new Angle(args.Relative.X * MouseLookSensitivity)).Reduced();
        ApplyFirstPersonYaw(player);

        _lookPitch = Math.Clamp(
            _lookPitch - args.Relative.Y * MouseLookSensitivity,
            -1.35f,
            1.35f);
        _world3D.SetFirstPersonPitch(_lookPitch);
        _lookYawDirty = true;
    }

    private void ApplyFirstPersonYaw()
    {
        if (_playerManager.LocalEntity is { Valid: true } player)
            ApplyFirstPersonYaw(player);
    }

    private void ApplyFirstPersonYaw(EntityUid player)
    {
        if (!TryComp(player, out InputMoverComponent? mover))
            return;

        var yaw = _lookYaw.Reduced();
        mover.FirstPersonMode = true;
        mover.FirstPersonYaw = yaw;

        // Temporary 2D-physics adapter only. The legacy mover adds the parent grid's world
        // rotation, so store yaw relative to that parent. This keeps W aligned with the 3D
        // camera even on rotated grids while still bypassing LerpRotation entirely.
        var adapterYaw = yaw - GetMoverParentWorldRotation(mover);
        adapterYaw = adapterYaw.Reduced();
        mover.RelativeRotation = adapterYaw;
        mover.TargetRelativeRotation = adapterYaw;
        mover.LerpTarget = TimeSpan.Zero;
    }

    private Angle GetWorldYawFromLegacyMover(InputMoverComponent mover)
    {
        return (GetMoverParentWorldRotation(mover) + mover.RelativeRotation).Reduced();
    }

    private Angle GetMoverParentWorldRotation(InputMoverComponent mover)
    {
        if (mover.RelativeEntity is { } relative &&
            TryComp(relative, out TransformComponent? relativeXform))
        {
            return _firstPersonTransform.GetWorldRotation(relativeXform);
        }

        return Angle.Zero;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        ApplyFirstPersonYaw();

        if (!_lookYawDirty || _playerManager.LocalEntity is not { Valid: true })
            return;

        _lookSendAccumulator += frameTime;
        if (_lookSendAccumulator < LookSendInterval)
            return;

        _lookSendAccumulator = 0f;
        _lookYawDirty = false;
        _firstPersonLookNet.Send((float) _lookYaw.Theta);
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

        if (captured &&
            _playerManager.LocalEntity is { Valid: true } player &&
            TryComp(player, out InputMoverComponent? mover))
        {
            _lookYaw = (mover.FirstPersonMode
                ? mover.FirstPersonYaw
                : GetWorldYawFromLegacyMover(mover)).Reduced();
            ApplyFirstPersonYaw(player);
        }

        _mouseLookCaptured = captured;
        _firstPersonClyde.MainWindow.SetRelativeMouseMode(captured);
    }
}

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
    private const float LookSendInterval = 1f / 30f;

    [Dependency] private IClyde _firstPersonClyde = default!;
    [Dependency] private IInputManager _firstPersonInput = default!;
    [Dependency] private World3DGridRenderingSystem _world3D = default!;

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
        _lookYaw = entity.Comp.RelativeRotation.Reduced();
        _lookYawDirty = false;
        _lookSendAccumulator = 0f;
        SetFirstPersonCameraRotation(entity.Owner, _lookYaw);

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
        _lookSendAccumulator = 0f;
    }

    private void OnFirstPersonMouseMove(MouseMoveEventArgs args)
    {
        if (!_mouseLookCaptured || _playerManager.LocalEntity is not { Valid: true } player)
            return;

        // SDL reports positive X to the right and positive Y down. Convert both axes to the
        // conventional FPS orientation and apply yaw immediately rather than through the
        // legacy 2D camera interpolation path.
        _lookYaw = (_lookYaw + new Angle(args.Relative.X * MouseLookSensitivity)).Reduced();
        SetFirstPersonCameraRotation(player, _lookYaw);

        _lookPitch = Math.Clamp(
            _lookPitch - args.Relative.Y * MouseLookSensitivity,
            -1.35f,
            1.35f);
        _world3D.SetFirstPersonPitch(_lookPitch);
        _lookYawDirty = true;
    }

    private void ApplyFirstPersonYaw()
    {
        if (!_mouseLookCaptured || _playerManager.LocalEntity is not { Valid: true } player)
            return;

        // Authoritative snapshots can briefly contain an older yaw. Re-apply the local camera
        // yaw before movement/rendering so WASD and the 3D camera always use the same angle.
        SetFirstPersonCameraRotation(player, _lookYaw);
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

        // Mouse motion is a frame/input event, not a predicted simulation command. Coalesce it
        // to 30 Hz and send the latest yaw as a normal network event to avoid late predicted ticks.
        RaiseNetworkEvent(new FirstPersonLookSyncEvent
        {
            Yaw = _lookYaw,
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

        if (captured &&
            _playerManager.LocalEntity is { Valid: true } player &&
            TryComp(player, out InputMoverComponent? mover))
        {
            _lookYaw = mover.RelativeRotation.Reduced();
            SetFirstPersonCameraRotation(player, _lookYaw);
        }

        _mouseLookCaptured = captured;
        _firstPersonClyde.MainWindow.SetRelativeMouseMode(captured);
    }
}

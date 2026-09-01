using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// Receives the first-person yaw independently of entity-event prediction.
/// The legacy relative-rotation fields are updated immediately only as a temporary adapter
/// for the existing 2D movement solver.
/// </summary>
public sealed class FirstPersonLookServerSystem : EntitySystem
{
    [Dependency] private IServerNetManager _net = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        _net.RegisterNetMessage<MsgFirstPersonLook>(OnLook);
    }

    private void OnLook(MsgFirstPersonLook message)
    {
        if (!float.IsFinite(message.Yaw) ||
            !_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            session.AttachedEntity is not { } uid ||
            !TryComp(uid, out InputMoverComponent? mover))
        {
            return;
        }

        var yaw = new Angle(message.Yaw).Reduced();
        mover.FirstPersonMode = true;
        mover.FirstPersonYaw = yaw;

        // The 3D camera and legacy 2D mover use opposite angular handedness for their
        // forward vectors. Camera yaw theta looks along (sin(theta), cos(theta)), while
        // legacy MoveUp rotated by A points (-sin(A), cos(A)), so A must be -theta.
        //
        // GetParentGridAngle later adds the parent grid rotation, therefore keep the
        // adapter relative to the parent grid so authoritative movement matches the
        // first-person camera on every quadrant and on rotated grids.
        var parentRotation = Angle.Zero;
        if (mover.RelativeEntity is { } relative &&
            TryComp(relative, out TransformComponent? relativeXform))
        {
            parentRotation = _transform.GetWorldRotation(relativeXform);
        }

        var movementWorldYaw = new Angle(-yaw.Theta);
        var adapterYaw = (movementWorldYaw - parentRotation).Reduced();
        mover.RelativeRotation = adapterYaw;
        mover.TargetRelativeRotation = adapterYaw;
        mover.LerpTarget = TimeSpan.Zero;
        Dirty(uid, mover);
    }
}

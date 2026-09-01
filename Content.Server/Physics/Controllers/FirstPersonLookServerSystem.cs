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

        // Temporary bridge into the authoritative 2D movement solver. Keeping both values equal
        // bypasses the old camera lerp/ShortestDistance path completely.
        mover.RelativeRotation = yaw;
        mover.TargetRelativeRotation = yaw;
        mover.LerpTarget = TimeSpan.Zero;
        Dirty(uid, mover);
    }
}

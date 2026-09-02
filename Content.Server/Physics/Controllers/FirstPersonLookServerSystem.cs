using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Physics3D;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// Receives the first-person yaw independently of entity-event prediction.
/// Look state is authoritative input for native 3D movement and interaction. It no longer mutates legacy 2D
/// relative-rotation fields.
/// </summary>
public sealed class FirstPersonLookServerSystem : EntitySystem
{
    [Dependency] private IServerNetManager _net = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedPhysics3DSystem _physics3D = default!;

    public override void Initialize()
    {
        base.Initialize();
        _net.RegisterNetMessage<MsgFirstPersonLook>(OnLook);
        _net.RegisterNetMessage<MsgFirstPersonJump>(OnJump);
    }

    private void OnLook(MsgFirstPersonLook message)
    {
        if (!float.IsFinite(message.Yaw) ||
            !float.IsFinite(message.Pitch) ||
            !_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            session.AttachedEntity is not { } uid ||
            !TryComp(uid, out InputMoverComponent? mover))
        {
            return;
        }

        var yaw = new Angle(message.Yaw).Reduced();
        mover.FirstPersonMode = true;
        mover.FirstPersonYaw = yaw;
        mover.FirstPersonPitch = Math.Clamp(message.Pitch, -1.35f, 1.35f);
        Dirty(uid, mover);
    }

    private void OnJump(MsgFirstPersonJump message)
    {
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            session.AttachedEntity is not { } uid ||
            !HasComp<CharacterController3DComponent>(uid))
        {
            return;
        }

        _physics3D.RequestCharacterJump(uid);
    }
}

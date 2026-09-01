using Content.Shared.Movement.Systems;
using Robust.Shared.Network;

namespace Content.Client.PhysicsSystem.Controllers;

/// <summary>
/// Registers and sends the lightweight first-person look message.
/// Keeping this separate from EntityEvent networking avoids tick-stamped late MsgEntity warnings.
/// </summary>
public sealed class FirstPersonLookClientSystem : EntitySystem
{
    [Dependency] private IClientNetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        _net.RegisterNetMessage<MsgFirstPersonLook>();
    }

    public void Send(float yaw)
    {
        if (!float.IsFinite(yaw))
            return;

        _net.ClientSendMessage(new MsgFirstPersonLook
        {
            Yaw = yaw,
        });
    }
}

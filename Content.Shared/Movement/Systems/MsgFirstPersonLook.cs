using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Lightweight transport for the experimental first-person yaw.
/// This intentionally bypasses entity-event tick stamping so mouse motion does not create late MsgEntity traffic.
/// </summary>
public sealed class MsgFirstPersonLook : NetMessage
{
    public float Yaw;

    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.UnreliableSequenced;

    // Content-owned sequence channel. Robust reserves channels 16 and higher internally.
    public override int SequenceChannel => 15;

    public override int EstimateBufferSize() => sizeof(float);

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Yaw = buffer.ReadFloat();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Yaw);
    }
}

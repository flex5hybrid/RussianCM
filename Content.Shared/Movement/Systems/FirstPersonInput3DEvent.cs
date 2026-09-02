using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Sequenced first-person input sampled independently from digital movement keys. Predictive system messages are
/// acknowledged by server snapshots and replayed at their source tick during reconciliation.
/// </summary>
[Serializable, NetSerializable]
public sealed class FirstPersonInput3DEvent : EntityEventArgs
{
    public float Yaw;
    public float Pitch;
    public bool Jump;
}

using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Interaction;

[Serializable, NetSerializable]
public enum InteractionAction3D : byte
{
    Use,
    Activate,
    AltActivate,
    Pull,
}

/// <summary>
/// A centre-screen world action. No client coordinate or target is accepted: both are reconstructed from the
/// authoritative character pose and sequenced first-person look state on the receiving simulation.
/// </summary>
[Serializable, NetSerializable]
public sealed class Interaction3DRequestEvent(InteractionAction3D action) : EntityEventArgs
{
    public InteractionAction3D Action = action;
}

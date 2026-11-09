using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Movement;

[Serializable, NetSerializable]
public enum CMUHumanMovementPace : byte
{
    Walk,
    Quiet,
    Tactical,
}

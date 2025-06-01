using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Map;
using System.Collections.Generic;

namespace Content.Server.Vehicles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TankMovementComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxSpeed")]
    public float MaxSpeed = 5f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("acceleration")]
    public float Acceleration = 2f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("rotationSpeed")]
    public float RotationSpeed = 1.5f;

    [ViewVariables]
    public float CurrentSpeed = 0f;

    [ViewVariables]
    public bool MovingForward = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanMove = true;

    // Добавлено для хранения активных направлений движения
    [ViewVariables]
    public HashSet<Direction> ActiveDirections = new();
}

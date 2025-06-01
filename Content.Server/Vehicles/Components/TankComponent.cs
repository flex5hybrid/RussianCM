using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server.Vehicles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TankComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxHealth")]
    public float MaxHealth = 1000f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("health")]
    public float Health = 1000f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("armor")]
    public float Armor = 500f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxFuel")]
    public float MaxFuel = 200f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fuel")]
    public float Fuel = 200f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fuelConsumptionRate")]
    public float FuelConsumptionRate = 0.5f; // Потребление топлива в секунду
}

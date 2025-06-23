using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.RuMC.Vehicles.Components;

/// <summary>
/// Атрибуты танка
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TankComponent : Component
{
    [DataField]
    public float MaxHealth = 1000f;

    [DataField]
    public float Health = 1000f;

    [DataField]
    public float MaxFuel = 200f;

    /// <summary>
    /// The slot the pilot is stored in.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot PilotSlot = null!;

    [ViewVariables]
    public readonly string PilotSlotId = "mech-pilot-slot";

    /// <summary>
    /// How long it takes to enter the mech.
    /// </summary>
    [DataField]
    public float EntryDelay = 1;
}

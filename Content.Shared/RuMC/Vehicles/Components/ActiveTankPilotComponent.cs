using Content.Shared.RuMC.Vehicles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.RuMC.Vehicles.Components;

/// <summary>
/// Компонент активного водителя танка
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTankSystem))]
public sealed partial class ActiveTankPilotComponent : Component
{
    /// <summary>
    /// Привязанная техника к пилоту
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid Mech;
}

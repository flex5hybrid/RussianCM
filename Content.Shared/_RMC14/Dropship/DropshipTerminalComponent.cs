using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipTerminalComponent : Component
{
    /// <summary>
    ///     The faction this terminal belongs to. If set, only users of the same faction can use it.
    ///     On ship grids, this is auto-inherited from ShipFactionComponent on MapInit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Faction;
}

using Robust.Shared.GameStates;

namespace Content.Server.Vehicles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TankControllerComponent : Component
{
    [ViewVariables]
    public EntityUid? Controller;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanMove = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanShoot = true;
}

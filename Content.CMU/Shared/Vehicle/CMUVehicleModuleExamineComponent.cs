using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.Vehicle;

// CMU14 class
// A vehicle module that adds a line to the vehicle's examine text while it is fitted, so modules without a sprite on
// the vehicle can still be seen.
[RegisterComponent, NetworkedComponent]
public sealed partial class CMUVehicleModuleExamineComponent : Component
{
    [DataField(required: true)]
    public LocId Examine;
}

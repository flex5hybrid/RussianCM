using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.CMU14.Vehicle;

// CMU14 class
// A front module (the spiked bullbar): while it is fitted and working, a creature the vehicle runs into takes this damage
// instead of the usual collision damage.
[RegisterComponent, NetworkedComponent]
public sealed partial class CMUVehicleRamDamageComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();
}

// CMU14 class
// A module (the heavy-fuel engine) that cancels this share of the slowdown the vehicle's other modules give it. It does
// nothing for a vehicle that is not slowed.
[RegisterComponent, NetworkedComponent]
public sealed partial class CMUVehicleSlowdownCompensationComponent : Component
{
    [DataField]
    public float Fraction = 0.5f;
}

// CMU14 class
// A turret module that needs a gunner. While it is fitted, the first passenger seat of the vehicle's interior is replaced by
// this gunner seat; when it is taken off, the passenger seat comes back.
[RegisterComponent]
public sealed partial class CMUVehicleTurretCrewComponent : Component
{
    [DataField(required: true)]
    public EntProtoId GunnerSeat;
}

// CMU14 class
// On a vehicle that takes a homemade turret: where on its sprite the turret's rotating overlay sits, in pixels from the
// vehicle's middle (y up), for each way the vehicle faces. One turret fits several vehicles with roofs in different
// places, so these are set on the turret when it is fitted (see CMUVehicleTurretCrewSystem).
[RegisterComponent]
public sealed partial class CMUVehicleTurretMountComponent : Component
{
    [DataField]
    public Vector2 North;

    [DataField]
    public Vector2 East;

    [DataField]
    public Vector2 South;

    [DataField]
    public Vector2 West;
}

// CMU14 class
// On a vehicle whose passenger seat a turret replaced: the gunner seat, and what to put back when the turret is removed.
[RegisterComponent]
public sealed partial class CMUVehicleGunnerSeatStateComponent : Component
{
    [ViewVariables]
    public EntityUid? GunnerSeat;

    [ViewVariables]
    public EntProtoId? OriginalPrototype;

    [ViewVariables]
    public EntityCoordinates Coordinates;

    [ViewVariables]
    public Angle Rotation;
}

// CMU14 class
// A seat in a vehicle's interior that loads the magazine of the vehicle's mounted gun: use a magazine on it, and the
// empty one comes back to the hand.
[RegisterComponent]
public sealed partial class CMUVehicleMagazineLoaderComponent : Component
{
    [DataField]
    public string Slot = "gun_magazine";
}

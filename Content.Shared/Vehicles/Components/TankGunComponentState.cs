using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Vehicles.Components;

[Serializable, NetSerializable]
public sealed class TankGunComponentState : ComponentState
{
    public int Ammo { get; }
    public TimeSpan NextFire { get; }
    public bool CanShoot { get; }

    public TankGunComponentState(int ammo, TimeSpan nextFire, bool canShoot)
    {
        Ammo = ammo;
        NextFire = nextFire;
        CanShoot = canShoot;
    }
}

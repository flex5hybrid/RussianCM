using System;
using Content.Server.Vehicles.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Vehicles.Systems;

public sealed class TankSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TankComponent, ComponentInit>(OnTankInit);
    }

    private void OnTankInit(EntityUid uid, TankComponent component, ComponentInit args)
    {
        component.Health = component.MaxHealth;
        component.Fuel = component.MaxFuel;
    }

    public void TakeDamage(EntityUid uid, float damage, TankComponent? tank = null)
    {
        if (!Resolve(uid, ref tank))
            return;

        tank.Health -= damage;
        if (tank.Health <= 0)
            QueueDel(uid);
    }
}

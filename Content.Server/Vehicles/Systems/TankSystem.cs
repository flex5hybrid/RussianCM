using Content.Server.Vehicles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Vehicles.Systems;

public sealed class TankSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

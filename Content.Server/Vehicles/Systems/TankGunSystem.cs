using Content.Server.Projectiles;
using Content.Server.Vehicles.Components;
using Content.Shared.Projectiles;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using System;
using Robust.Shared.Serialization;

public sealed class TankGunSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TankGunComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, TankGunComponent component, ref ComponentGetState args)
    {
        args.State = new TankGunComponentState(component.Ammo, component.NextFire, component.CanShoot);
    }

    public void TryFire(EntityUid uid, EntityUid user, TankGunComponent gun)
    {
        if (gun.Ammo <= 0 || !gun.CanShoot || gun.NextFire > _gameTiming.CurTime)
            return;

        gun.Ammo--;
        gun.NextFire = _gameTiming.CurTime + TimeSpan.FromSeconds(gun.Cooldown);

        var direction = _transform.GetWorldRotation(uid).ToWorldVec();
        var spawnPos = _transform.GetMapCoordinates(uid).Offset(direction * 1.5f);

        // Создаем снаряд
        var projectile = EntityManager.SpawnEntity(gun.ProjectilePrototype, spawnPos);

        // Устанавливаем скорость снаряда
        if (TryComp<PhysicsComponent>(projectile, out var physics))
        {
            _physics.ApplyLinearImpulse(projectile, direction * gun.ProjectileSpeed, body: physics);
        }

        // Устанавливаем стрелка (shooter)
        if (TryComp<ProjectileComponent>(projectile, out var projectileComp))
        {
            projectileComp.Shooter = user;
        }
    }
}

[NetworkedComponent]
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

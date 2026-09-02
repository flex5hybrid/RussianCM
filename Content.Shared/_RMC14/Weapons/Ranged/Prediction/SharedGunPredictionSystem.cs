using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics3D;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Weapons.Ranged.Prediction;

public abstract partial class SharedGunPredictionSystem : EntitySystem
{
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedTransform3DSystem _transform3D = default!;
    [Dependency] private SharedPhysics3DSystem _physics3D = default!;
    [Dependency] private VehicleRideSurfaceSystem _rideSurface = default!;

    public bool GunPrediction { get; private set; }

    public override void Initialize()
    {
        Subs.CVar(_config, RMCCVars.RMCGunPrediction, v => GunPrediction = v, true);
    }

    public List<EntityUid>? ShootRequested(NetEntity netGun, NetCoordinates coordinates, NetEntity? target, List<int>? projectiles, ICommonSession session, bool rearmSemiAuto = false)
    {
        var user = session.AttachedEntity;

        if (user == null ||
            !_combatMode.IsInCombatMode(user) ||
            !_gun.TryGetGun(user.Value, out var ent, out var gun))
        {
            return null;
        }

        if (ent != GetEntity(netGun))
            return null;

        var firstPerson3D = TryComp(user.Value, out InputMoverComponent? mover) && mover.FirstPersonMode;
        if (!firstPerson3D)
        {
            gun.ShootOrigin3D = null;
            gun.ShootDirection3D = null;
        }

        var shootCoordinates = firstPerson3D
            ? ReconstructFirstPersonAim(user.Value, mover!, gun, out target)
            : GetCoordinates(coordinates);
        var shootMapCoordinates = _transform.ToMapCoordinates(shootCoordinates);
        if (!IsSameMap(ent, shootMapCoordinates))
            return null;

        var targetUid = GetEntity(target);
        if (targetUid is { } clickedTarget)
        {
            if (_rideSurface.TryGetRiderAtCoordinates(clickedTarget, shootMapCoordinates, out var rider))
                targetUid = rider;

            if (targetUid is { } resolvedTarget &&
                !IsSameMap(resolvedTarget, shootMapCoordinates))
            {
                targetUid = null;
            }
        }

#pragma warning disable RA0002
        gun.ShootCoordinates = shootCoordinates;
        gun.Target = targetUid;
#pragma warning restore RA0002
        if (rearmSemiAuto)
            _gun.ResetShotCounter(ent, gun);

        return _gun.AttemptShoot(user.Value, ent, gun, projectiles, session);
    }

    private EntityCoordinates ReconstructFirstPersonAim(
        EntityUid user,
        InputMoverComponent mover,
        GunComponent gun,
        out NetEntity? ignoredClientTarget)
    {
        ignoredClientTarget = null;
        if (!TryComp(user, out TransformComponent? userTransform))
            return EntityCoordinates.Invalid;

        const float maxRange = 1000f;
        var origin = _transform3D.GetWorldPosition3D(user, userTransform) + Vector3.UnitZ * 1.58f;
        var horizontal = MathF.Cos(mover.FirstPersonPitch);
        var yaw = (float) mover.FirstPersonYaw.Theta;
        var direction = Vector3.Normalize(new Vector3(
            MathF.Sin(yaw) * horizontal,
            MathF.Cos(yaw) * horizontal,
            MathF.Sin(mover.FirstPersonPitch)));

        gun.ShootOrigin3D = origin;
        gun.ShootDirection3D = direction;

        var hitPoint = origin + direction * maxRange;
        if (_physics3D.TryRayCast(
                userTransform.MapID,
                new Ray3D(origin, direction),
                maxRange,
                int.MaxValue,
                user,
                false,
                out var hit))
        {
            hitPoint = hit.Position;
            ignoredClientTarget = GetNetEntity(hit.Entity);
        }

        // Temporary adapter for the legacy gun event ecosystem. Spatial authority remains the reconstructed 3D ray.
        var mapPoint = new MapCoordinates(new Vector2(hitPoint.X, hitPoint.Y), userTransform.MapID);
        return _transform.ToCoordinates(userTransform.ParentUid, mapPoint);
    }

    protected bool IsSameMap(EntityUid entity, EntityUid other)
    {
        return TryGetMapId(entity, out var mapId) &&
               TryGetMapId(other, out var otherMapId) &&
               mapId == otherMapId;
    }

    protected bool IsSameMap(EntityUid entity, MapCoordinates coordinates)
    {
        return coordinates.MapId != MapId.Nullspace &&
               TryGetMapId(entity, out var mapId) &&
               mapId == coordinates.MapId;
    }

    protected bool IsSameMap(MapCoordinates coordinates, MapCoordinates other)
    {
        return coordinates.MapId != MapId.Nullspace &&
               coordinates.MapId == other.MapId;
    }

    private bool TryGetMapId(EntityUid entity, out MapId mapId)
    {
        mapId = MapId.Nullspace;

        if (!TryComp(entity, out TransformComponent? xform))
            return false;

        mapId = xform.MapID;
        return mapId != MapId.Nullspace;
    }
}

using System.Numerics;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.Medieval.Magic.MedievalHomingProjectile;


public sealed partial class MedievalHomingProjectileSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly PhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<MedievalHomingProjectileComponent, ProjectileComponent, PhysicsComponent>();

        while (enumerator.MoveNext(out var uid, out var component, out var _, out var physicsComponent))
        {
            if (component.NextUpdate >= _timing.CurTime) return;

            component.NextUpdate = _timing.CurTime + component.UpdateRate;

            if (component.MapTarget == null && component.EntityTarget == null) continue;
            if (GetVectorSigns(physicsComponent.LinearVelocity) == component.Signs * -1)
            {
                component.Signs = GetVectorSigns(component.TargetCoords - _transformSystem.GetWorldPosition(uid));
                component.TargetCoords = component.EntityTarget.HasValue
                    ? GetPeakDistance(component.EntityTarget.Value, uid)
                    : GetPeakDistance(component.MapTarget!.Value, uid);
            }

            var grid = _transformSystem.GetGrid(uid);
            var gridRotation = grid.HasValue ? _transformSystem.GetWorldRotation(grid.Value) : Angle.Zero;

            var targetPosition = GetTargetCoords(component);
            var projectilePosition = _transformSystem.GetWorldPosition(uid);

            var direction = component.TargetCoords - projectilePosition;
            var rotation = component.RotateToTarget
                ? (targetPosition - projectilePosition).ToAngle()
                : physicsComponent.LinearVelocity.ToAngle();

            _transformSystem.SetLocalRotation(uid, rotation - component.RelativeAngle - gridRotation);
            _physicsSystem.ApplyLinearImpulse(uid, direction * component.LinearVelocityIntensy, body: physicsComponent);
        }
    }

    #region Public API

    public void SetTarget(EntityUid projectile, EntityUid target)
    {
        SetTarget(projectile, target, 1.0f);
    }

    public void SetTarget(EntityUid projectile, EntityUid target, float linearVelocityIntensy)
    {
        SetTarget(projectile, target, linearVelocityIntensy, Angle.Zero);
    }

    public void SetTarget(EntityUid projectile, EntityUid target, float linearVelocityIntensy, Angle relativeAngle)
    {
        SetTarget(projectile, target, linearVelocityIntensy, relativeAngle, false);
    }

    public void SetTarget(EntityUid projectile, EntityUid target, float linearVelocityIntensy, Angle relativeAngle, bool rotateToTarget)
    {
        var component = EnsureComp<MedievalHomingProjectileComponent>(projectile);

        component.EntityTarget = target;
        component.MapTarget = null;
        component.LinearVelocityIntensy = linearVelocityIntensy;
        component.RelativeAngle = relativeAngle;
        component.RotateToTarget = rotateToTarget;

        component.TargetCoords = GetPeakDistance(target, projectile);
        component.Signs = GetVectorSigns(component.TargetCoords - _transformSystem.GetWorldPosition(projectile));
    }
    public void SetTarget(EntityUid projectile, MapCoordinates target)
    {
        SetTarget(projectile, target, 1.0f);
    }

    public void SetTarget(EntityUid projectile, MapCoordinates target, float linearVelocityIntensy)
    {
        SetTarget(projectile, target, linearVelocityIntensy, Angle.Zero);
    }

    public void SetTarget(EntityUid projectile, MapCoordinates target, float linearVelocityIntensy, Angle relativeAngle)
    {
        SetTarget(projectile, target, linearVelocityIntensy, relativeAngle, false);
    }

    public void SetTarget(EntityUid projectile, MapCoordinates target, float linearVelocityIntensy, Angle relativeAngle, bool rotateToTarget)
    {
        var component = EnsureComp<MedievalHomingProjectileComponent>(projectile);

        component.EntityTarget = null;
        component.MapTarget = target;
        component.LinearVelocityIntensy = linearVelocityIntensy;
        component.RelativeAngle = relativeAngle;
        component.RotateToTarget = rotateToTarget;
        component.TargetCoords = GetPeakDistance(target, projectile);
        component.Signs = GetVectorSigns(component.TargetCoords - _transformSystem.GetWorldPosition(projectile));
    }

    #endregion

    #region Helpers

    private Vector2 GetPeakDistance(EntityUid target, EntityUid projectile)
    {
        if (!TryComp<PhysicsComponent>(target, out var physicsComponent)) return Vector2.Zero;

        var targetPosition = _transformSystem.GetMapCoordinates(target).Position + (physicsComponent?.LinearVelocity ?? Vector2.Zero);
        var projectilePosition = _transformSystem.GetWorldPosition(projectile);

        return (targetPosition + projectilePosition) / 2;
    }

    private Vector2 GetPeakDistance(MapCoordinates target, EntityUid projectile)
    {
        var projectilePosition = _transformSystem.GetWorldPosition(projectile);

        return (target.Position + projectilePosition) / 2f;
    }

    private Vector2 GetTargetCoords(MedievalHomingProjectileComponent component)
    {
        if (component.EntityTarget == null) return component.MapTarget!.Value.Position;
        if (!HasComp<TransformComponent>(component.EntityTarget.Value)) return Vector2.Zero;

        return _transformSystem.GetMapCoordinates(component.EntityTarget.Value).Position;
    }

    private Vector2i GetVectorSigns(Vector2 vec) => new Vector2i(Math.Sign(vec.X), Math.Sign(vec.Y));

    #endregion
}

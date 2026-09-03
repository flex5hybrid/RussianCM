using System;
using System.Numerics;
using Content.Server.Fluids.Components;
using Content.Server.Maps.Components;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics3D;

namespace Content.Server.Maps;

/// <summary>
/// Promotes loaded and newly spawned collidable content to BEPU bodies. Existing fixture geometry is extruded
/// into compound 3D boxes, preserving fixture filters, sensors, friction, restitution, mass and initial velocity.
/// The old physics component is retained as read-only compatibility data but removed from the planar solver.
/// </summary>
public sealed class Native3DEntityMigrationSystem : EntitySystem
{
    [Dependency] private SharedTransform3DSystem _transforms3D = default!;
    [Dependency] private SharedPhysics3DSystem _physics3D = default!;
    [Dependency] private SharedPhysicsSystem _physics2D = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapGrid3DComponent, MapInitEvent>(OnGridMapInit);
        SubscribeLocalEvent<PhysicsComponent, MapInitEvent>(OnPhysicsMapInit);
        SubscribeLocalEvent<PhysicsComponent, EntParentChangedMessage>(OnPhysicsParentChanged);
        SubscribeLocalEvent<Native3DMigratedEntityComponent, FixturesChangedEvent>(OnFixturesChanged);
        SubscribeLocalEvent<ProjectileComponent, ComponentStartup>(OnProjectile3DHandlerAdded);
        SubscribeLocalEvent<ThrownItemComponent, ComponentStartup>(OnThrown3DHandlerAdded);
        SubscribeLocalEvent<FluidCell3DComponent, ComponentStartup>(OnFluid3DHandlerAdded);
    }

    private void OnProjectile3DHandlerAdded(Entity<ProjectileComponent> entity, ref ComponentStartup args) =>
        DisableLegacyCollisionEvents(entity.Owner);

    private void OnThrown3DHandlerAdded(Entity<ThrownItemComponent> entity, ref ComponentStartup args) =>
        DisableLegacyCollisionEvents(entity.Owner);

    private void OnFluid3DHandlerAdded(Entity<FluidCell3DComponent> entity, ref ComponentStartup args) =>
        DisableLegacyCollisionEvents(entity.Owner);

    private void DisableLegacyCollisionEvents(EntityUid uid)
    {
        if (TryComp(uid, out LegacyPhysics3DBridgeComponent? bridge))
            bridge.RaiseLegacyEvents = false;
    }

    private void OnGridMapInit(Entity<MapGrid3DComponent> grid, ref MapInitEvent args)
    {
        PromoteGrid(grid.Owner);
    }

    public void PromoteGrid(EntityUid grid)
    {
        var query = EntityQueryEnumerator<PhysicsComponent, FixturesComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var body, out var fixtures, out var transform))
        {
            if (transform.GridUid == grid)
                TryPromote((uid, body, fixtures, transform));
        }
    }

    private void OnPhysicsMapInit(Entity<PhysicsComponent> entity, ref MapInitEvent args)
    {
        if (TryComp(entity.Owner, out FixturesComponent? fixtures) &&
            TryComp(entity.Owner, out TransformComponent? transform))
            TryPromote((entity.Owner, entity.Comp, fixtures, transform));
    }

    private void OnPhysicsParentChanged(Entity<PhysicsComponent> entity, ref EntParentChangedMessage args)
    {
        if (TryComp(entity.Owner, out FixturesComponent? fixtures))
            TryPromote((entity.Owner, entity.Comp, fixtures, args.Transform));
    }

    private void OnFixturesChanged(
        Entity<Native3DMigratedEntityComponent> entity,
        ref FixturesChangedEvent args)
    {
        if (!TryComp(entity.Owner, out Collider3DComponent? collider3D) ||
            !TryComp(entity.Owner, out PhysicsBody3DComponent? body3D) ||
            !TryComp(entity.Owner, out LegacyPhysics3DBridgeComponent? bridge))
            return;

        var body2D = args.Entity.Comp1;
        var fixtures = args.Entity.Comp2;
        var isCharacter = (body2D.BodyType & BodyType.KinematicController) != 0;
        var height = InferHeight(entity.Owner, fixtures, isCharacter);
        entity.Comp.Height = height;
        body3D.BodyType = ConvertBodyType(body2D.BodyType, isCharacter);
        body3D.Mass = MathF.Max(body2D.Mass, 1f);
        body3D.GravityScale = body2D.IgnoreGravity ? 0f : 1f;
        body3D.LinearDamping = body2D.LinearDamping;
        body3D.AngularDamping = body2D.AngularDamping;
        body3D.SleepingAllowed = body2D.SleepingAllowed;

        collider3D.Shapes.Clear();
        bridge.ShapeFixtureIds.Clear();
        if (isCharacter)
            AddCharacterShape(collider3D, bridge, fixtures, height);
        else
            AddExtrudedFixtureShapes(collider3D, bridge, fixtures, height);

        body3D.CanCollide = bridge.RequestedCanCollide && collider3D.Shapes.Count > 0;

        Dirty(entity.Owner, body3D);
        Dirty(entity.Owner, collider3D);
        _physics3D.RefreshBody(entity.Owner);
    }

    private void TryPromote(Entity<PhysicsComponent, FixturesComponent, TransformComponent> entity)
    {
        if (HasComp<Native3DMigratedEntityComponent>(entity.Owner) ||
            HasComp<MapGridComponent>(entity.Owner) ||
            !entity.Comp1.CanCollide ||
            entity.Comp2.FixtureCount == 0 ||
            entity.Comp3.GridUid is not { } grid ||
            !HasComp<MapGrid3DComponent>(grid))
            return;

        var isCharacter = (entity.Comp1.BodyType & BodyType.KinematicController) != 0;
        var height = InferHeight(entity.Owner, entity.Comp2, isCharacter);
        if (height <= 0f)
            return;

        _physics2D.SetCanCollide(entity.Owner, false, body: entity.Comp1);
        _transforms3D.SetAuthoritative(entity.Owner, true, entity.Comp3);
        var body3D = EnsureComp<PhysicsBody3DComponent>(entity.Owner);
        body3D.BodyType = ConvertBodyType(entity.Comp1.BodyType, isCharacter);
        body3D.Mass = MathF.Max(entity.Comp1.Mass, 1f);
        body3D.LinearVelocity = new Vector3(entity.Comp1.LinearVelocity, 0f);
        body3D.AngularVelocity = new Vector3(0f, 0f, entity.Comp1.AngularVelocity);
        body3D.GravityScale = entity.Comp1.IgnoreGravity ? 0f : 1f;
        body3D.LinearDamping = entity.Comp1.LinearDamping;
        body3D.AngularDamping = entity.Comp1.AngularDamping;
        body3D.SleepingAllowed = entity.Comp1.SleepingAllowed;
        body3D.CanCollide = true;

        var collider3D = EnsureComp<Collider3DComponent>(entity.Owner);
        collider3D.Shapes.Clear();
        var bridge = EnsureComp<LegacyPhysics3DBridgeComponent>(entity.Owner);
        bridge.ShapeFixtureIds.Clear();
        bridge.RequestedCanCollide = true;
        bridge.RaiseLegacyEvents = !HasComp<ProjectileComponent>(entity.Owner) &&
                                   !HasComp<ThrownItemComponent>(entity.Owner) &&
                                   !HasComp<FluidCell3DComponent>(entity.Owner);
        if (isCharacter)
            AddCharacterShape(collider3D, bridge, entity.Comp2, height);
        else
            AddExtrudedFixtureShapes(collider3D, bridge, entity.Comp2, height);

        if (collider3D.Shapes.Count == 0)
        {
            RemCompDeferred<PhysicsBody3DComponent>(entity.Owner);
            RemCompDeferred<Collider3DComponent>(entity.Owner);
            RemCompDeferred<LegacyPhysics3DBridgeComponent>(entity.Owner);
            return;
        }

        if (isCharacter)
            EnsureComp<CharacterController3DComponent>(entity.Owner);
        var marker = EnsureComp<Native3DMigratedEntityComponent>(entity.Owner);
        marker.Height = height;
        Dirty(entity.Owner, body3D);
        Dirty(entity.Owner, collider3D);
        _physics3D.RefreshBody(entity.Owner);
    }

    private static void AddCharacterShape(
        Collider3DComponent collider,
        LegacyPhysics3DBridgeComponent bridge,
        FixturesComponent fixtures,
        float height)
    {
        var bounds = GetFixtureBounds(fixtures);
        var radius = Math.Clamp(MathF.Max(bounds.Width, bounds.Height) * 0.45f, 0.22f, 0.42f);
        collider.Shapes.Add(new CapsuleShape3D
        {
            Radius = radius,
            Length = MathF.Max(height - radius * 2f, 0.1f),
            Offset = new Vector3(bounds.Center, height * 0.5f),
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI * 0.5f),
            CollisionLayer = GetCollisionLayer(fixtures),
            CollisionMask = GetCollisionMask(fixtures),
            Friction = 0f,
        });
        foreach (var fixture in fixtures.Fixtures)
        {
            bridge.ShapeFixtureIds.Add(fixture.Key);
            break;
        }
    }

    private static void AddExtrudedFixtureShapes(
        Collider3DComponent collider,
        LegacyPhysics3DBridgeComponent bridge,
        FixturesComponent fixtures,
        float height)
    {
        foreach (var (fixtureId, fixture) in fixtures.Fixtures)
        {
            for (var child = 0; child < fixture.Shape.ChildCount; child++)
            {
                var bounds = fixture.Shape.ComputeAABB(Robust.Shared.Physics.Transform.Empty, child);
                if (bounds.Width <= 0.001f || bounds.Height <= 0.001f)
                    continue;
                collider.Shapes.Add(new BoxShape3D
                {
                    Size = new Vector3(bounds.Width, bounds.Height, height),
                    Offset = new Vector3(bounds.Center, height * 0.5f),
                    Sensor = !fixture.Hard,
                    CollisionLayer = fixture.CollisionLayer,
                    CollisionMask = fixture.CollisionMask,
                    Friction = fixture.Friction,
                    Restitution = fixture.Restitution,
                });
                bridge.ShapeFixtureIds.Add(fixtureId);
            }
        }
    }

    private float InferHeight(EntityUid uid, FixturesComponent fixtures, bool character)
    {
        if (character)
            return 1.7f;
        var bounds = GetFixtureBounds(fixtures);
        if (HasComp<OccluderComponent>(uid))
            return 2.6f;
        return Math.Clamp(bounds.MaxDimension * 0.85f, 0.12f, 1.4f);
    }

    private static Box2 GetFixtureBounds(FixturesComponent fixtures)
    {
        var bounds = default(Box2);
        var found = false;
        foreach (var fixture in fixtures.Fixtures.Values)
        for (var child = 0; child < fixture.Shape.ChildCount; child++)
        {
            var childBounds = fixture.Shape.ComputeAABB(Robust.Shared.Physics.Transform.Empty, child);
            bounds = found ? bounds.Union(childBounds) : childBounds;
            found = true;
        }
        return found ? bounds : new Box2(-0.25f, -0.25f, 0.25f, 0.25f);
    }

    private static int GetCollisionLayer(FixturesComponent fixtures)
    {
        var result = 0;
        foreach (var fixture in fixtures.Fixtures.Values)
            result |= fixture.CollisionLayer;
        return result;
    }

    private static int GetCollisionMask(FixturesComponent fixtures)
    {
        var result = 0;
        foreach (var fixture in fixtures.Fixtures.Values)
            result |= fixture.CollisionMask;
        return result;
    }

    private static PhysicsBodyType3D ConvertBodyType(BodyType bodyType, bool character)
    {
        if (character)
            return PhysicsBodyType3D.Character;
        if ((bodyType & BodyType.Dynamic) != 0)
            return PhysicsBodyType3D.Dynamic;
        if ((bodyType & BodyType.Static) != 0)
            return PhysicsBodyType3D.Static;
        return PhysicsBodyType3D.Kinematic;
    }
}

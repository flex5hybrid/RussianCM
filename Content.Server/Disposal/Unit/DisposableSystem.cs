using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Disposal.Components;
using Content.Shared.Item;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics3D;
using Robust.Shared.Maths;

namespace Content.Server.Disposal.Unit
{
    public sealed partial class DisposableSystem : EntitySystem
    {
        [Dependency] private ThrowingSystem _throwing = default!;
        [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private DamageableSystem _damageable = default!;
        [Dependency] private DisposalUnitSystem _disposalUnitSystem = default!;
        [Dependency] private DisposalTubeSystem _disposalTubeSystem = default!;
        [Dependency] private SharedAudioSystem _audio = default!;
        [Dependency] private SharedContainerSystem _containerSystem = default!;
        [Dependency] private SharedMapSystem _maps = default!;
        [Dependency] private SharedPhysicsSystem _physicsSystem = default!;
        [Dependency] private SharedTransformSystem _xformSystem = default!;
        [Dependency] private SharedTransform3DSystem _xform3D = default!;
        [Dependency] private SharedPhysics3DSystem _physics3D = default!;

        private EntityQuery<DisposalTubeComponent> _disposalTubeQuery;
        private EntityQuery<DisposalUnitComponent> _disposalUnitQuery;
        private EntityQuery<MetaDataComponent> _metaQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;
        private EntityQuery<TransformComponent> _xformQuery;

        public override void Initialize()
        {
            base.Initialize();

            _disposalTubeQuery = GetEntityQuery<DisposalTubeComponent>();
            _disposalUnitQuery = GetEntityQuery<DisposalUnitComponent>();
            _metaQuery = GetEntityQuery<MetaDataComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();

            SubscribeLocalEvent<DisposalHolderComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<DisposalHolderComponent, ContainerIsInsertingAttemptEvent>(CanInsert);
            SubscribeLocalEvent<DisposalHolderComponent, EntInsertedIntoContainerMessage>(OnInsert);
        }

        private void OnComponentStartup(EntityUid uid, DisposalHolderComponent holder, ComponentStartup args)
        {
            holder.Container = _containerSystem.EnsureContainer<Container>(uid, nameof(DisposalHolderComponent));
        }

        private void CanInsert(Entity<DisposalHolderComponent> ent, ref ContainerIsInsertingAttemptEvent args)
        {
            if (!HasComp<ItemComponent>(args.EntityUid) && !HasComp<BodyComponent>(args.EntityUid))
                args.Cancel();
        }

        private void OnInsert(Entity<DisposalHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            if (_physicsQuery.TryGetComponent(args.Entity, out var physBody))
                _physicsSystem.SetCanCollide(args.Entity, false, body: physBody);

            if (TryComp(args.Entity, out PhysicsBody3DComponent? body3D))
            {
                body3D.CanCollide = false;
                body3D.Dirty(EntityManager);
                _physics3D.RefreshBody(args.Entity);
            }
        }

        public void ExitDisposals(EntityUid uid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null)
        {
            if (Terminating(uid))
                return;

            if (!Resolve(uid, ref holder, ref holderTransform))
                return;
            if (holder.IsExitingDisposals)
            {
                Log.Error("Tried exiting disposals twice. This should never happen.");
                return;
            }
            holder.IsExitingDisposals = true;

            // Check for a disposal unit to throw them into and then eject them from it.
            // *This ejection also makes the target not collide with the unit.*
            // *This is on purpose.*

            EntityUid? disposalId = null;
            DisposalUnitComponent? duc = null;
            var gridUid = holderTransform.GridUid;
            if (_xform3D.IsAuthoritative(uid))
            {
                var holderPosition = _xform3D.GetWorldPosition3D(uid, holderTransform);
                var units3D = EntityQueryEnumerator<DisposalUnitComponent, TransformComponent, Transform3DComponent>();
                while (units3D.MoveNext(out var unitUid, out var unit, out var unitTransform, out var unitTransform3D))
                {
                    if (!unitTransform3D.IsAuthoritative ||
                        unitTransform.MapID != holderTransform.MapID ||
                        Vector3.DistanceSquared(holderPosition, _xform3D.GetWorldPosition3D(unitUid, unitTransform)) > 0.36f)
                        continue;

                    disposalId = unitUid;
                    duc = unit;
                    break;
                }
            }

            if (TryComp<MapGridComponent>(gridUid, out var grid))
            {
                foreach (var contentUid in _maps.GetLocal(gridUid.Value, grid, holderTransform.Coordinates))
                {
                    if (_disposalUnitQuery.TryGetComponent(contentUid, out duc))
                    {
                        disposalId = contentUid;
                        break;
                    }
                }
            }

            // We're purposely iterating over all the holder's children
            // because the holder might have something teleported into it,
            // outside the usual container insertion logic.
            var children = holderTransform.ChildEnumerator;
            while (children.MoveNext(out var entity))
            {
                RemComp<BeingDisposedComponent>(entity);

                var meta = _metaQuery.GetComponent(entity);
                if (holder.Container.Contains(entity))
                    _containerSystem.Remove((entity, null, meta), holder.Container, reparent: false, force: true);

                var xform = _xformQuery.GetComponent(entity);
                if (xform.ParentUid != uid)
                    continue;

                if (duc != null)
                    _containerSystem.Insert((entity, xform, meta), duc.Container);
                else
                {
                    _xformSystem.AttachToGridOrMap(entity, xform);
                    if (_xform3D.IsAuthoritative(uid))
                    {
                        var position3D = _xform3D.GetWorldPosition3D(uid, holderTransform);
                        _xform3D.SetAuthoritative(entity, true, xform);
                        _xform3D.SetWorldPosition3D(entity, position3D, xform);
                        var body3D = EnsureComp<PhysicsBody3DComponent>(entity);
                        body3D.BodyType = PhysicsBodyType3D.Dynamic;
                        body3D.CanCollide = true;
                        body3D.ContinuousDetection = ContinuousDetectionMode3D.Continuous;
                        var collider3D = EnsureComp<Collider3DComponent>(entity);
                        if (collider3D.Shapes.Count == 0)
                        {
                            collider3D.Shapes.Add(new SphereShape3D
                            {
                                Radius = 0.24f,
                                CollisionLayer = 1,
                                CollisionMask = int.MaxValue,
                            });
                        }

                        body3D.Dirty(EntityManager);
                        collider3D.Dirty(EntityManager);
                        _physics3D.RefreshBody(entity);
                        var direction3D = holder.CurrentDirection3D != Vector3i.Zero
                            ? holder.CurrentDirection3D
                            : holder.PreviousDirection3D;
                        var velocity = _disposalTubeSystem.GetDisposalDirectionWorld3D(
                            holder.CurrentTube ?? holder.PreviousTube ?? uid,
                            direction3D);
                        body3D.LinearVelocity = velocity * 10f;
                        body3D.Dirty(EntityManager);
                        continue;
                    }

                    var direction = holder.CurrentDirection == Direction.Invalid ? holder.PreviousDirection : holder.CurrentDirection;

                    if (direction != Direction.Invalid && _xformQuery.TryGetComponent(gridUid, out var gridXform))
                    {
                        var directionAngle = direction.ToAngle();
                        directionAngle += _xformSystem.GetWorldRotation(gridXform);
                        _throwing.TryThrow(entity, directionAngle.ToWorldVec() * 3f, 10f);
                    }
                }
            }

            if (disposalId != null && duc != null)
            {
                _disposalUnitSystem.TryEjectContents(disposalId.Value, duc);
            }

            if (_atmosphereSystem.GetContainingMixture(uid, false, true) is { } environment)
            {
                _atmosphereSystem.Merge(environment, holder.Air);
                holder.Air.Clear();
            }

            Del(uid);
        }

        // Note: This function will cause an ExitDisposals on any failure that does not make an ExitDisposals impossible.
        public bool EnterTube(EntityUid holderUid, EntityUid toUid, DisposalHolderComponent? holder = null, TransformComponent? holderTransform = null, DisposalTubeComponent? to = null, TransformComponent? toTransform = null)
        {
            if (!Resolve(holderUid, ref holder, ref holderTransform))
                return false;
            if (holder.IsExitingDisposals)
            {
                Log.Error("Tried entering tube after exiting disposals. This should never happen.");
                return false;
            }
            if (!Resolve(toUid, ref to, ref toTransform))
            {
                ExitDisposals(holderUid, holder, holderTransform);
                return false;
            }

            foreach (var ent in holder.Container.ContainedEntities)
            {
                var comp = EnsureComp<BeingDisposedComponent>(ent);
                comp.Holder = holderUid;
            }

            // Insert into next tube
            if (!_containerSystem.Insert(holderUid, to.Contents))
            {
                ExitDisposals(holderUid, holder, holderTransform);
                return false;
            }

            if (holder.CurrentTube != null)
            {
                holder.PreviousTube = holder.CurrentTube;
                holder.PreviousDirection = holder.CurrentDirection;
                holder.PreviousDirection3D = holder.CurrentDirection3D;
            }
            holder.CurrentTube = toUid;
            if (_disposalTubeSystem.TryGetNextDirection3D(toUid, holder, out var direction3D))
            {
                _xform3D.SetAuthoritative(holderUid, true, holderTransform);
                _xform3D.SetWorldPosition3D(holderUid, _xform3D.GetWorldPosition3D(toUid, toTransform), holderTransform);
                holder.CurrentDirection = Direction.Invalid;
                holder.CurrentDirection3D = direction3D;
                holder.StartingTime = 0.1f;
                holder.TimeLeft = 0.1f;
                if (direction3D == Vector3i.Zero)
                {
                    ExitDisposals(holderUid, holder, holderTransform);
                    return false;
                }

                if (holder.CurrentDirection3D != holder.PreviousDirection3D)
                {
                    foreach (var ent in holder.Container.ContainedEntities)
                        _damageable.TryChangeDamage(ent, to.DamageOnTurn);
                    _audio.PlayPvs(to.ClangSound, toUid);
                }

                return true;
            }

            holder.CurrentDirection3D = Vector3i.Zero;
            var ev = new GetDisposalsNextDirectionEvent(holder);
            RaiseLocalEvent(toUid, ref ev);
            holder.CurrentDirection = ev.Next;
            holder.StartingTime = 0.1f;
            holder.TimeLeft = 0.1f;
            // Logger.GetSawmill("c.s.disposal.holder").Info( $"Disposals dir {holder.CurrentDirection}");

            // Invalid direction = exit now!
            if (holder.CurrentDirection == Direction.Invalid)
            {
                ExitDisposals(holderUid, holder, holderTransform);
                return false;
            }

            // damage entities on turns and play sound
            if (holder.CurrentDirection != holder.PreviousDirection)
            {
                foreach (var ent in holder.Container.ContainedEntities)
                {
                    _damageable.TryChangeDamage(ent, to.DamageOnTurn);
                }
                _audio.PlayPvs(to.ClangSound, toUid);
            }

            return true;
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<DisposalHolderComponent>();
            while (query.MoveNext(out var uid, out var holder))
            {
                UpdateComp(uid, holder, frameTime);
            }
        }

        private void UpdateComp(EntityUid uid, DisposalHolderComponent holder, float frameTime)
        {
            while (frameTime > 0)
            {
                var time = frameTime;
                if (time > holder.TimeLeft)
                {
                    time = holder.TimeLeft;
                }

                holder.TimeLeft -= time;
                frameTime -= time;

                if (!Exists(holder.CurrentTube))
                {
                    ExitDisposals(uid, holder);
                    break;
                }

                var currentTube = holder.CurrentTube!.Value;
                if (holder.TimeLeft > 0)
                {
                    var progress = 1 - holder.TimeLeft / holder.StartingTime;
                    if (holder.CurrentDirection3D != Vector3i.Zero && _xform3D.IsAuthoritative(currentTube))
                    {
                        var origin3D = _xform3D.GetWorldPosition3D(currentTube);
                        var direction3D = _disposalTubeSystem.GetDisposalDirectionWorld3D(currentTube, holder.CurrentDirection3D);
                        _xform3D.SetWorldPosition3D(uid, origin3D + direction3D * progress);
                        continue;
                    }

                    var origin = _xformQuery.GetComponent(currentTube).Coordinates;
                    var destination = holder.CurrentDirection.ToVec();
                    var newPosition = destination * progress;

                    // This is some supreme shit code.
                    _xformSystem.SetCoordinates(uid, _xformSystem.WithEntityId(origin.Offset(newPosition), currentTube));
                    continue;
                }

                // Past this point, we are performing inter-tube transfer!
                // Remove current tube content
                _containerSystem.Remove(uid, _disposalTubeQuery.GetComponent(currentTube).Contents, reparent: false, force: true);

                // Find next tube
                var nextTube = holder.CurrentDirection3D != Vector3i.Zero
                    ? _disposalTubeSystem.NextTubeFor3D(currentTube, holder.CurrentDirection3D)
                    : _disposalTubeSystem.NextTubeFor(currentTube, holder.CurrentDirection);
                if (!Exists(nextTube))
                {
                    ExitDisposals(uid, holder);
                    break;
                }

                // Perform remainder of entry process
                if (!EnterTube(uid, nextTube!.Value, holder))
                {
                    break;
                }
            }
        }
    }
}

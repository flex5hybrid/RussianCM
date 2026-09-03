using System.Numerics;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics3D;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private static readonly Vector3i[] FluidHorizontalDirections3D =
    {
        Vector3i.East,
        Vector3i.West,
        Vector3i.North,
        Vector3i.South,
    };

    private float _fluid3DAccumulator;

    private void InitializeFluids3D()
    {
        SubscribeLocalEvent<FluidCell3DComponent, StartCollide3DEvent>(OnFluidContact3D);
    }

    private bool TrySpillAt3D(
        EntityUid source,
        TransformComponent transform,
        Solution solution,
        out EntityUid puddleUid,
        bool sound)
    {
        puddleUid = EntityUid.Invalid;
        if (!_transform3D.IsAuthoritative(source) || solution.Volume <= FixedPoint2.Zero)
            return false;

        var root = transform.GridUid ?? transform.MapUid;
        if (root is not { } rootUid || !TryComp(rootUid, out MapGrid3DComponent? grid))
            return false;

        var world = _transform3D.GetWorldPosition3D(source, transform);
        GetGravityDirections3D(rootUid, source, out _, out var worldDown);
        if (_physics3D.TryRayCast(
                transform.MapID,
                new Ray3D(world - worldDown * 0.05f, worldDown),
                64f,
                int.MaxValue,
                source,
                false,
                out var hit))
        {
            world = hit.Position + hit.Normal * 0.02f;
        }

        var cell = _mapGrid3D.WorldToCell((rootUid, grid), world);
        if (!HasGroundBelow3D(rootUid, grid, cell, transform.MapID, source))
        {
            solution.RemoveSolution(solution.Volume);
            return true; // The liquid was released into open space and is consumed by the spill.
        }

        puddleUid = GetOrCreateFluidCell3D(rootUid, grid, cell, transform.MapID, solution);
        if (!puddleUid.IsValid())
        {
            solution.RemoveSolution(solution.Volume);
            return true;
        }

        TryAddSolution(puddleUid, solution, sound);
        return true;
    }

    private EntityUid GetOrCreateFluidCell3D(
        EntityUid root,
        MapGrid3DComponent grid,
        Vector3i cell,
        MapId mapId,
        Solution sample)
    {
        var query = EntityQueryEnumerator<FluidCell3DComponent>();
        while (query.MoveNext(out var uid, out var fluid))
        {
            if (fluid.Root == root && fluid.Cell == cell)
                return uid;
        }

        if (IsFluidCellBlocked3D((root, grid), cell))
            return EntityUid.Invalid;

        var world = _mapGrid3D.CellToWorld((root, grid), cell);
        var uidNew = Spawn(GetPuddlePrototype(sample), new MapCoordinates(new Vector2(world.X, world.Y), mapId));
        var fluidCell = EnsureComp<FluidCell3DComponent>(uidNew);
        fluidCell.Root = root;
        fluidCell.Cell = cell;

        _transform3D.SetAuthoritative(uidNew, true);
        _transform3D.SetWorldPosition3D(uidNew, world);

        var body = EnsureComp<PhysicsBody3DComponent>(uidNew);
        body.BodyType = PhysicsBodyType3D.Static;
        body.CanCollide = true;
        var collider = EnsureComp<Collider3DComponent>(uidNew);
        collider.Shapes.Clear();
        collider.Shapes.Add(new BoxShape3D
        {
            Size = new Vector3(MathF.Max(grid.CellSize * 0.96f, 0.02f), MathF.Max(grid.CellSize * 0.96f, 0.02f), 0.04f),
            Sensor = true,
            CollisionLayer = 1,
            CollisionMask = int.MaxValue,
        });
        EnsureComp<Primitive3DComponent>(uidNew);
        body.Dirty(EntityManager);
        collider.Dirty(EntityManager);
        _physics3D.RefreshBody(uidNew);
        return uidNew;
    }

    private void UpdateFluids3D(float frameTime)
    {
        _fluid3DAccumulator += frameTime;
        if (_fluid3DAccumulator < 0.25f)
            return;

        _fluid3DAccumulator %= 0.25f;
        var fluids = new List<EntityUid>();
        var query = EntityQueryEnumerator<FluidCell3DComponent, PuddleComponent>();
        while (query.MoveNext(out var uid, out _, out _))
            fluids.Add(uid);

        foreach (var uid in fluids)
            ProcessFluidCell3D(uid);
    }

    private void ProcessFluidCell3D(EntityUid uid)
    {
        if (!TryComp(uid, out FluidCell3DComponent? fluid) ||
            !TryComp(uid, out PuddleComponent? puddle) ||
            !TryComp(fluid.Root, out MapGrid3DComponent? grid) ||
            !_solutionContainerSystem.ResolveSolution(uid, puddle.SolutionName, ref puddle.Solution, out var solution))
            return;

        if (solution.Volume <= FixedPoint2.Zero)
            return;

        var mapId = Transform(uid).MapID;
        GetGravityDirections3D(fluid.Root, uid, out var cellDown, out _);
        var below = fluid.Cell + cellDown;
        if (!HasImmediateSupport3D(fluid.Root, grid, fluid.Cell, mapId, uid))
        {
            if (!HasGroundBelow3D(fluid.Root, grid, fluid.Cell, mapId, uid))
            {
                _solutionContainerSystem.SplitSolution(puddle.Solution!.Value, solution.Volume);
                QueueDel(uid);
                return;
            }

            TransferFluid3D(uid, puddle, solution, fluid.Root, grid, below, mapId, solution.Volume);
            return;
        }

        var overflow = solution.Volume - puddle.OverflowVolume;
        if (overflow <= FixedPoint2.Zero)
        {
            UpdateFluidVisual3D((uid, fluid), puddle, solution, grid);
            return;
        }

        var destinations = new List<Vector3i>(4);
        foreach (var direction in FluidHorizontalDirections3D)
        {
            var cell = fluid.Cell + direction;
            if (!IsFluidCellBlocked3D((fluid.Root, grid), cell))
                destinations.Add(cell);
        }

        if (destinations.Count == 0)
            return;

        var amount = overflow / destinations.Count;
        foreach (var destination in destinations)
        {
            if (amount <= FixedPoint2.Zero || solution.Volume <= puddle.OverflowVolume)
                break;
            TransferFluid3D(uid, puddle, solution, fluid.Root, grid, destination, mapId, amount);
        }

        UpdateFluidVisual3D((uid, fluid), puddle, solution, grid);
    }

    private void TransferFluid3D(
        EntityUid sourceUid,
        PuddleComponent sourcePuddle,
        Solution source,
        EntityUid root,
        MapGrid3DComponent grid,
        Vector3i destination,
        MapId mapId,
        FixedPoint2 amount)
    {
        var split = _solutionContainerSystem.SplitSolution(sourcePuddle.Solution!.Value, FixedPoint2.Min(amount, source.Volume));
        if (split.Volume <= FixedPoint2.Zero)
            return;

        var target = GetOrCreateFluidCell3D(root, grid, destination, mapId, split);
        if (!target.IsValid() || !TryAddSolution(target, split, sound: false))
            _solutionContainerSystem.TryAddSolution(sourcePuddle.Solution.Value, split);
    }

    private bool HasImmediateSupport3D(
        EntityUid root,
        MapGrid3DComponent grid,
        Vector3i cell,
        MapId mapId,
        EntityUid ignored)
    {
        GetGravityDirections3D(root, ignored, out var cellDown, out var worldDown);
        if (IsFluidCellBlocked3D((root, grid), cell + cellDown))
            return true;

        var center = _mapGrid3D.CellToWorld((root, grid), cell);
        return _physics3D.TryRayCast(
            mapId,
            new Ray3D(center, worldDown),
            MathF.Max(grid.CellSize * 0.6f, 0.1f),
            int.MaxValue,
            ignored,
            false,
            out _);
    }

    private bool HasGroundBelow3D(
        EntityUid root,
        MapGrid3DComponent grid,
        Vector3i cell,
        MapId mapId,
        EntityUid ignored)
    {
        var center = _mapGrid3D.CellToWorld((root, grid), cell);
        GetGravityDirections3D(root, ignored, out _, out var worldDown);
        return _physics3D.TryRayCast(
            mapId,
            new Ray3D(center, worldDown),
            64f,
            int.MaxValue,
            ignored,
            false,
            out _);
    }

    private bool IsFluidCellBlocked3D(Entity<MapGrid3DComponent> grid, Vector3i cell)
    {
        var voxel = _mapGrid3D.GetVoxel(grid, cell);
        return (voxel.Flags & VoxelFlags3D.Solid) != 0;
    }

    private void UpdateFluidVisual3D(
        Entity<FluidCell3DComponent> entity,
        PuddleComponent puddle,
        Solution solution,
        MapGrid3DComponent grid)
    {
        var fill = Math.Clamp(solution.Volume.Float() / MathF.Max(puddle.OverflowVolume.Float(), 0.01f), 0.02f, 1f);
        var height = MathF.Max(grid.CellSize * fill, 0.02f);
        var cellCenter = _mapGrid3D.CellToWorld((entity.Comp.Root, grid), entity.Comp.Cell);
        GetGravityDirections3D(entity.Comp.Root, entity.Owner, out var cellDown, out _);
        var belowCenter = _mapGrid3D.CellToWorld((entity.Comp.Root, grid), entity.Comp.Cell + cellDown);
        var up = cellCenter - belowCenter;
        up = up.LengthSquared() > 1e-6f ? Vector3.Normalize(up) : Vector3.UnitZ;
        var bottom = cellCenter - up * grid.CellSize * 0.5f;
        _transform3D.SetWorldPosition3D(entity.Owner, bottom + up * height * 0.5f);

        var size = new Vector3(grid.CellSize * 0.96f, grid.CellSize * 0.96f, height);
        var primitive = EnsureComp<Primitive3DComponent>(entity.Owner);
        primitive.Size = size;
        primitive.Color = solution.GetColor(_prototypeManager).WithAlpha(0.68f);
        primitive.Dirty(EntityManager);

        if (TryComp(entity.Owner, out Collider3DComponent? collider) && collider.Shapes.FirstOrDefault() is BoxShape3D box)
        {
            box.Size = size;
            collider.Dirty(EntityManager);
            _physics3D.RefreshBody(entity.Owner);
        }
    }

    private void OnFluidContact3D(Entity<FluidCell3DComponent> entity, ref StartCollide3DEvent args)
    {
        if (!TryComp(entity.Owner, out PuddleComponent? puddle) ||
            !_solutionContainerSystem.ResolveSolution(entity.Owner, puddle.SolutionName, ref puddle.Solution, out var solution))
            return;

        var viscosity = 0f;
        foreach (var (reagent, _) in solution.Contents)
            viscosity = MathF.Max(viscosity, _reagent.Index(reagent.Prototype).Viscosity);

        if (viscosity > 0f && _physics3D.TryGetVelocity(args.OtherEntity, out var linear, out var angular))
        {
            var drag = Math.Clamp(1f - viscosity * 0.25f, 0.2f, 1f);
            _physics3D.SetVelocity(args.OtherEntity, new Vector3(linear.X * drag, linear.Y * drag, linear.Z), angular);
        }

        if (HasComp<ReactiveComponent>(args.OtherEntity) && solution.Volume > FixedPoint2.Zero)
        {
            var touch = _solutionContainerSystem.SplitSolution(
                puddle.Solution!.Value,
                FixedPoint2.Min(solution.Volume, FixedPoint2.New(1)));
            _reactive.DoEntityReaction(args.OtherEntity, touch, ReactionMethod.Touch);
        }
    }

    private void GetGravityDirections3D(
        EntityUid root,
        EntityUid subject,
        out Vector3i cellDown,
        out Vector3 worldDown)
    {
        var gravity = _physics3D.GetGravity(subject);
        worldDown = gravity.LengthSquared() > 1e-8f ? Vector3.Normalize(gravity) : -Vector3.UnitZ;
        var localDown = Vector3.Transform(worldDown, Quaternion.Inverse(_transform3D.GetWorldRotation3D(root)));
        var absolute = Vector3.Abs(localDown);
        if (absolute.X >= absolute.Y && absolute.X >= absolute.Z)
            cellDown = localDown.X >= 0f ? Vector3i.East : Vector3i.West;
        else if (absolute.Y >= absolute.Z)
            cellDown = localDown.Y >= 0f ? Vector3i.North : Vector3i.South;
        else
            cellDown = localDown.Z >= 0f ? Vector3i.Up : Vector3i.Down;
    }
}

using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics3D;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private static readonly Vector3i[] PositiveAtmosphereDirections3D =
    {
        Vector3i.East,
        Vector3i.North,
        Vector3i.Up,
    };

    private EntityQuery<GridAtmosphere3DComponent> _atmosphere3DQuery;
    private EntityQuery<MapGrid3DComponent> _mapGrid3DQuery;

    private void InitializeAtmosphere3D()
    {
        _atmosphere3DQuery = GetEntityQuery<GridAtmosphere3DComponent>();
        _mapGrid3DQuery = GetEntityQuery<MapGrid3DComponent>();

        SubscribeLocalEvent<GridAtmosphere3DComponent, ComponentStartup>(OnAtmosphere3DStartup);
        SubscribeLocalEvent<GridAtmosphere3DComponent, VoxelChanged3DEvent>(OnAtmosphere3DVoxelChanged);
    }

    private void OnAtmosphere3DStartup(Entity<GridAtmosphere3DComponent> entity, ref ComponentStartup args)
    {
        SeedAtmosphere3D(entity);
    }

    private void SeedAtmosphere3D(Entity<GridAtmosphere3DComponent> entity)
    {
        if (!_mapGrid3DQuery.TryGetComponent(entity.Owner, out var grid))
            return;

        foreach (var region in entity.Comp.Regions)
            SeedAtmosphereRegion3D(entity, grid, region);
    }

    private void SeedAtmosphereRegion3D(
        Entity<GridAtmosphere3DComponent> entity,
        MapGrid3DComponent grid,
        AtmosphereRegion3D region)
    {
        var min = Vector3i.ComponentMin(region.Min, region.Max);
        var max = Vector3i.ComponentMax(region.Min, region.Max);
        for (var z = min.Z; z <= max.Z; z++)
        for (var y = min.Y; y <= max.Y; y++)
        for (var x = min.X; x <= max.X; x++)
        {
            var indices = new Vector3i(x, y, z);
            if (IsAtmosphereBlocked3D((entity.Owner, grid), indices))
                continue;

            entity.Comp.Cells.TryAdd(indices, CreateAtmosphere3DMixture(entity.Comp, region.Mixture));
        }
    }

    /// <summary>
    /// Adds and immediately seeds an inclusive volumetric atmosphere region on a native map root.
    /// </summary>
    public void AddAtmosphereRegion3D(
        EntityUid root,
        Vector3i min,
        Vector3i max,
        GasMixture? mixture = null,
        bool sealedBoundary = false)
    {
        var grid = EnsureComp<MapGrid3DComponent>(root);
        var atmosphere = EnsureComp<GridAtmosphere3DComponent>(root);
        var region = new AtmosphereRegion3D
        {
            Min = min,
            Max = max,
            Mixture = mixture,
            SealedBoundary = sealedBoundary,
        };
        atmosphere.Regions.Add(region);
        SeedAtmosphereRegion3D((root, atmosphere), grid, region);
    }

    private GasMixture CreateAtmosphere3DMixture(GridAtmosphere3DComponent component, GasMixture? source = null)
    {
        if (source is not null)
        {
            var mixture = source.Clone();
            mixture.Volume = component.CellVolume;
            return mixture;
        }

        var air = new GasMixture(component.CellVolume) { Temperature = Atmospherics.T20C };
        var volumeRatio = component.CellVolume / Atmospherics.CellVolume;
        air.SetMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard * volumeRatio);
        air.SetMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard * volumeRatio);
        return air;
    }

    private void OnAtmosphere3DVoxelChanged(Entity<GridAtmosphere3DComponent> entity, ref VoxelChanged3DEvent args)
    {
        if (!_mapGrid3DQuery.TryGetComponent(entity.Owner, out var grid))
            return;

        if (IsAtmosphereBlocked3D((entity.Owner, grid), args.Indices))
        {
            entity.Comp.Cells.Remove(args.Indices);
            return;
        }

        if (!ContainsAtmosphereRegion3D(entity.Comp, args.Indices) || entity.Comp.Cells.ContainsKey(args.Indices))
            return;

        // Opening a structural cell fills it from its neighbouring room rather than manufacturing gas.
        foreach (var direction in SharedMapGrid3DSystem.CardinalNeighbors)
        {
            if (!entity.Comp.Cells.TryGetValue(args.Indices + direction, out var neighbour))
                continue;

            var opened = neighbour.RemoveRatio(0.5f);
            opened.Volume = entity.Comp.CellVolume;
            entity.Comp.Cells.Add(args.Indices, opened);
            return;
        }

        entity.Comp.Cells.Add(args.Indices, new GasMixture(entity.Comp.CellVolume)
        {
            Temperature = Atmospherics.TCMB,
        });
    }

    private static bool ContainsAtmosphereRegion3D(GridAtmosphere3DComponent component, Vector3i indices)
    {
        foreach (var region in component.Regions)
        {
            var min = Vector3i.ComponentMin(region.Min, region.Max);
            var max = Vector3i.ComponentMax(region.Min, region.Max);
            if (indices.X >= min.X && indices.X <= max.X &&
                indices.Y >= min.Y && indices.Y <= max.Y &&
                indices.Z >= min.Z && indices.Z <= max.Z)
                return true;
        }

        return false;
    }

    private bool IsAtmosphereBlocked3D(Entity<MapGrid3DComponent> grid, Vector3i indices)
    {
        var voxel = _mapGrid3D.GetVoxel(grid, indices);
        return (voxel.Flags & (VoxelFlags3D.Solid | VoxelFlags3D.Airtight)) != 0;
    }

    private void UpdateAtmosphere3D(float frameTime)
    {
        var query = EntityQueryEnumerator<GridAtmosphere3DComponent, MapGrid3DComponent>();
        while (query.MoveNext(out var uid, out var atmosphere, out var grid))
        {
            if (!atmosphere.Simulated || atmosphere.Cells.Count == 0)
                continue;

            atmosphere.Accumulator += frameTime;
            var interval = MathF.Max(atmosphere.UpdateInterval, 0.01f);
            if (atmosphere.Accumulator < interval)
                continue;

            atmosphere.Accumulator %= interval;
            ProcessAtmosphere3D((uid, atmosphere), grid);
        }
    }

    private void ProcessAtmosphere3D(Entity<GridAtmosphere3DComponent> entity, MapGrid3DComponent grid)
    {
        var coordinates = entity.Comp.Cells.Keys.ToArray();
        Array.Sort(coordinates, CompareAtmosphereCoordinates3D);

        foreach (var current in coordinates)
        {
            if (!entity.Comp.Cells.TryGetValue(current, out var currentAir))
                continue;

            foreach (var direction in PositiveAtmosphereDirections3D)
            {
                var adjacent = current + direction;
                if (IsAtmosphereBlocked3D((entity.Owner, grid), current) ||
                    IsAtmosphereBlocked3D((entity.Owner, grid), adjacent))
                    continue;

                if (entity.Comp.Cells.TryGetValue(adjacent, out var adjacentAir))
                {
                    var pressureDelta = currentAir.Pressure - adjacentAir.Pressure;
                    if (EqualizeAtmosphereFace3D(currentAir, adjacentAir, entity.Comp.Conductance))
                    {
                        var flowDirection = pressureDelta > 0f ? direction : -direction;
                        var sourceCell = pressureDelta > 0f ? current : adjacent;
                        ApplyAtmosphericImpulse3D(entity.Owner, grid, sourceCell, flowDirection, MathF.Abs(pressureDelta));
                    }
                }
            }

            // Every absent, non-airtight face outside the declared volume is a real breach to map atmosphere.
            foreach (var direction in SharedMapGrid3DSystem.CardinalNeighbors)
            {
                var adjacent = current + direction;
                if (!entity.Comp.Cells.ContainsKey(adjacent) &&
                    IsAtmosphereBoundaryOpen3D(entity.Comp, current, adjacent) &&
                    !IsAtmosphereBlocked3D((entity.Owner, grid), adjacent))
                {
                    var pressure = currentAir.Pressure;
                    VentAtmosphereFace3D(currentAir, entity.Comp.Conductance);
                    ApplyAtmosphericImpulse3D(entity.Owner, grid, current, direction, pressure);
                }
            }

            React(currentAir, null);
        }
    }

    /// <summary>
    /// Deposits heat into the volumetric cell occupied by an authoritative 3D ignition source.
    /// Returns false when the source still belongs to the compatibility atmosphere.
    /// </summary>
    public bool HotspotExpose3D(EntityUid source, float exposedTemperature, float exposedVolume)
    {
        if (!_transform3D.IsAuthoritative(source))
            return false;

        var transform = Transform(source);
        if (!TryGetContainingMixture3D((source, transform), out var mixture))
            return false;

        if (mixture is null || mixture.Immutable)
            return true;

        var ratio = Math.Clamp(exposedVolume / MathF.Max(mixture.Volume, 0.01f), 0f, 1f);
        if (exposedTemperature > mixture.Temperature)
            mixture.Temperature += (exposedTemperature - mixture.Temperature) * ratio;

        React(mixture, null);
        return true;
    }

    private bool EqualizeAtmosphereFace3D(GasMixture first, GasMixture second, float conductance)
    {
        var pressureDelta = first.Pressure - second.Pressure;
        if (MathF.Abs(pressureDelta) < 0.01f)
            return false;

        var source = pressureDelta > 0f ? first : second;
        var destination = pressureDelta > 0f ? second : first;
        if (source.TotalMoles <= Atmospherics.GasMinMoles)
            return false;

        var ratio = Math.Clamp(
            MathF.Abs(pressureDelta) / MathF.Max(source.Pressure, 0.01f) * conductance,
            0f,
            0.5f);
        if (ratio < Atmospherics.MinimumAirRatioToMove)
            return false;

        Merge(destination, source.RemoveRatio(ratio));
        return true;
    }

    private void ApplyAtmosphericImpulse3D(
        EntityUid root,
        MapGrid3DComponent grid,
        Vector3i sourceCell,
        Vector3i flowDirection,
        float pressureDifference)
    {
        if (!SpaceWind || pressureDifference <= 0f)
            return;

        var center = _mapGrid3D.CellToWorld((root, grid), sourceCell);
        var adjacentCenter = _mapGrid3D.CellToWorld((root, grid), sourceCell + flowDirection);
        var direction = adjacentCenter - center;
        if (direction.LengthSquared() < 1e-6f)
            return;

        direction = Vector3.Normalize(direction);
        var halfExtent = new Vector3(MathF.Max(grid.CellSize * 0.5f, 0.01f));
        var overlaps = new List<PhysicsOverlap3D>();
        _physics3D.GetAabbOverlaps(
            Transform(root).MapID,
            new Box3(center - halfExtent, center + halfExtent),
            int.MaxValue,
            null,
            false,
            overlaps);

        var handled = new HashSet<EntityUid>();
        foreach (var overlap in overlaps)
        {
            if (!handled.Add(overlap.Entity) ||
                !TryComp(overlap.Entity, out MovedByPressureComponent? moved) ||
                !moved.Enabled ||
                !TryComp(overlap.Entity, out PhysicsBody3DComponent? body) ||
                body.BodyType is PhysicsBodyType3D.Static or PhysicsBodyType3D.Kinematic ||
                float.IsPositiveInfinity(moved.MoveResist))
                continue;

            var force = MathF.Sqrt(pressureDifference) * 2.25f;
            if (force < moved.MoveResist * MovedByPressureComponent.MoveForcePushRatio)
                continue;

            var impulse = MathF.Min(force / MathF.Max(SpaceWindPressureForceDivisorPush, 0.01f),
                MathF.Max(body.Mass, 0.01f) * SpaceWindMaxVelocity);
            _physics3D.ApplyLinearImpulse(overlap.Entity, direction * impulse);
        }
    }

    private static void VentAtmosphereFace3D(GasMixture mixture, float conductance)
    {
        if (mixture.TotalMoles <= Atmospherics.GasMinMoles)
            return;

        mixture.RemoveRatio(Math.Clamp(conductance, 0f, 0.5f));
    }

    private static bool IsAtmosphereBoundaryOpen3D(
        GridAtmosphere3DComponent component,
        Vector3i current,
        Vector3i adjacent)
    {
        var containsCurrent = false;
        var sealedBoundary = false;
        foreach (var region in component.Regions)
        {
            var min = Vector3i.ComponentMin(region.Min, region.Max);
            var max = Vector3i.ComponentMax(region.Min, region.Max);
            var currentInside = current.X >= min.X && current.X <= max.X &&
                                current.Y >= min.Y && current.Y <= max.Y &&
                                current.Z >= min.Z && current.Z <= max.Z;
            if (!currentInside)
                continue;

            containsCurrent = true;
            if (adjacent.X >= min.X && adjacent.X <= max.X &&
                adjacent.Y >= min.Y && adjacent.Y <= max.Y &&
                adjacent.Z >= min.Z && adjacent.Z <= max.Z)
                return false;

            sealedBoundary |= region.SealedBoundary;
        }

        return containsCurrent && !sealedBoundary;
    }

    private static int CompareAtmosphereCoordinates3D(Vector3i first, Vector3i second)
    {
        var z = first.Z.CompareTo(second.Z);
        if (z != 0)
            return z;
        var y = first.Y.CompareTo(second.Y);
        return y != 0 ? y : first.X.CompareTo(second.X);
    }

    private bool TryGetContainingMixture3D(Entity<TransformComponent?> entity, out GasMixture? mixture)
    {
        mixture = null;
        if (!_transform3D.IsAuthoritative(entity.Owner) || !Resolve(entity, ref entity.Comp))
            return false;

        var root = entity.Comp.GridUid ?? entity.Comp.MapUid;
        if (root is not { } rootUid ||
            !_atmosphere3DQuery.TryGetComponent(rootUid, out var atmosphere) ||
            !_mapGrid3DQuery.TryGetComponent(rootUid, out var grid))
            return false;

        var worldPosition = _transform3D.GetWorldPosition3D(entity.Owner, entity.Comp);
        var cell = _mapGrid3D.WorldToCell((rootUid, grid), worldPosition);
        if (atmosphere.Cells.TryGetValue(cell, out mixture))
            return true;

        if (entity.Comp.MapUid is { } mapUid && _mapAtmosQuery.TryGetComponent(mapUid, out var mapAtmosphere))
            mixture = mapAtmosphere.Mixture;
        else
            mixture = GasMixture.SpaceGas;

        return true;
    }
}

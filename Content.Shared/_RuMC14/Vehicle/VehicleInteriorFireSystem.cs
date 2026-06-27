using Content.Shared._RMC14.Vehicle;
using Content.Shared._RuMC14.Vehicle;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Vehicle;

/// <summary>
/// When a vehicle is destroyed (all top-level hardpoints reach 0), spawns fire on every tile
/// of the cabin interior, provided the interior map has already been loaded.
/// </summary>
public sealed class VehicleInteriorFireSystem : EntitySystem
{
    private static readonly EntProtoId FireProto = "RMCTileFireNapalm";

    [Dependency] private VehicleSystem _vehicleSystem = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HardpointIntegrityComponent, VehicleFrameDestroyedEvent>(
            OnVehicleFrameDestroyed);
    }

    private void OnVehicleFrameDestroyed(
        Entity<HardpointIntegrityComponent> ent,
        ref VehicleFrameDestroyedEvent args)
    {
        SpawnInteriorFire(args.Vehicle);
    }

    private void SpawnInteriorFire(EntityUid vehicle)
    {
        if (!_vehicleSystem.TryGetInteriorMapId(vehicle, out var mapId))
            return;

        EntityUid? interiorGrid = null;
        var gridQuery = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
        while (gridQuery.MoveNext(out var gridUid, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            interiorGrid = gridUid;
            break;
        }

        if (interiorGrid is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? gridComp))
        {
            return;
        }

        foreach (var tile in _mapSystem.GetAllTiles(gridId, gridComp))
        {
            var tileDef = _turf.GetContentTileDefinition(tile.Tile);

            if (tileDef.MapAtmosphere || tileDef.ID == "RMCVoid")
                continue;

            var coords = _mapSystem.GridTileToLocal(gridId, gridComp, tile.GridIndices);
            Spawn(FireProto, coords);
        }
    }
}

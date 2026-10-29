using System.Globalization;
using System.Numerics;
using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Clothing.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.CMU14.Lighting;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Light.Components;
using Content.Shared._RMC14.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.CMU14.Light;

/// <summary>Explicit admin-only art review: an industrial set and a copy of the real CMU Almayer.</summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed partial class CMUCinematicDemoCommand : LocalizedEntityCommands
{
    private static readonly ProtoId<ReagentPrototype> Water = "Water";
    private static readonly ResPath Almayer = new("/Maps/CMU14/Ships/Legacy/almayer_rucm.yml");
    private static readonly ResPath ReferenceHall = new("/Maps/CMU14/Planets/StableGarrison/StableGarrisonMultiZ0.yml");
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private PointLightSystem _lights = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private PuddleSystem _puddles = default!;
    [Dependency] private ISharedPlayerManager _players = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IConsoleHost _console = default!;

    public override string Command => "cmu_cinematic_demo";
    public override string Description => Loc.GetString("cmu-cinematic-demo-description");
    public override string Help => Loc.GetString("cmu-cinematic-demo-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 4)
        {
            shell.WriteError(Help);
            return;
        }
        var session = shell.Player;
        if (args.Length > 0 && !_players.TryGetSessionByUsername(args[0], out session))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }
        var mode = args.Length >= 2 ? args[1] : "industrial";
        if (mode is "reload" or "watch" && session != null)
        {
            _console.RemoteExecuteCommand(session, mode == "watch" ? "rldshader +watch" : "rldshader");
            return;
        }
        if (mode == "capture" && session != null)
        {
            var seconds = 6;
            if (args.Length > 3 || args.Length == 3
                && (!int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds)
                    || seconds is < 3 or > 60))
            {
                shell.WriteError(Help);
                return;
            }
            if (session.AttachedEntity is { } viewer
                && EntityManager.GetComponent<TransformComponent>(viewer).MapUid is { } reviewMap
                && EntityManager.HasComponent<CMUCinematicReviewSceneComponent>(reviewMap))
                _map.SetPaused(EntityManager.GetComponent<TransformComponent>(viewer).MapID, true);
            _console.RemoteExecuteCommand(session, $"cmu_cinematic_capture {seconds}");
            return;
        }
        if (mode == "view" && args.Length == 4 && session?.AttachedEntity is { } actor
            && float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            && float.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            var mapUid = EntityManager.GetComponent<TransformComponent>(actor).MapUid!.Value;
            _transform.SetCoordinates(actor, new EntityCoordinates(mapUid, new Vector2(x, y)));
            return;
        }
        if (mode is "almayer" or "reference")
        {
            LoadReviewMap(shell, session, mode == "reference" ? ReferenceHall : Almayer,
                mode == "reference" ? new Vector2(225.5f, -145.5f) : new Vector2(-108.5f, 90.5f));
            return;
        }
        if (mode is not ("industrial" or "industrial-static"))
        {
            shell.WriteError(Help);
            return;
        }

        var map = _map.CreateMap(out var mapId, runMapInit: true);
        EntityManager.AddComponent<CMUCinematicReviewSceneComponent>(map);
        _map.SetAmbientLight(mapId, Color.FromHex("#25303B"));
        AddAir(map);
        var grid = _map.CreateGridEntity(mapId);
        EntityManager.AddComponent<CMUCinematicAtmosphereComponent>(grid).Density = 0.8f;
        for (var tx = -10; tx <= 10; tx++)
        for (var ty = -6; ty <= 6; ty++)
        {
            var tile = tx >= 2 ? "CMFloorPlate" : tx < -3 ? "CMFloorSteel" : "CMFloorWhiteSterile";
            _map.SetTile(grid.Owner, grid.Comp, new Vector2i(tx, ty), new Tile(_tiles[tile].TileId));
            if (tx is -10 or 10 || ty is -6 or 6)
                Spawn("CMWallMetalAlmayer", tx, ty);
        }

        // Partial partitions and columns leave the main sightline open, while retaining
        // convincing industrial scale, contact shadows and a real occlusion test.
        foreach (var tx in new[] { -8, 8 })
            Spawn("CMWallMetalAlmayer", tx, 2);
        Spawn("CMWindowReinforcedAlmayer", -7, 2);
        Spawn("CMWindowReinforcedAlmayer", 7, 2);
        foreach (var tx in new[] { -7, -6, -5, 3, 4, 5 })
            Spawn("CMTableAlmayer", tx, 3);
        Spawn("RMCPropCommunicationsConsole", -6, 3);
        Spawn("RMCPersonalDesktop", 4, 3);
        Spawn("RMCPersonalDesktop", 5, 3);
        Spawn("CMRack", -9, -3);
        Spawn("CMRack", -9, -2);
        Spawn("CMCrateSecureSurgery", -8, -3);
        Spawn("CMVendorCola", -4, 5);
        Spawn("CMVendorSnack", -3, 5);
        Spawn("RMCBenchLeft", -3, -4);
        Spawn("RMCBenchRight", -2, -4);
        Spawn("CMTable", 7, -4);
        Spawn("RMCPersonalDesktop", 7, -4);
        foreach (var tx in new[] { -5, -4 })
        {
            Spawn("CMTableAlmayer", tx, 0);
            Spawn("RMCPropCommunicationsConsole", tx, 0);
        }
        Spawn("CMCrateSecureSurgery", -7, -4);
        Spawn("CMCrateSecureSurgery", 8, 0);
        Spawn("CMRack", 8, 1);
        Spawn("RMCPropComputerLarge", 2, 4);
        Spawn("RMCPropComputerMap", -8, 4);
        for (var tx = -9; tx <= 9; tx++)
        {
            var top = Spawn("CMOverheadPipe", tx, 5);
            var bottom = Spawn("CMOverheadPipe", tx, -5);
            _transform.SetLocalRotation(top, Angle.FromDegrees(90));
            _transform.SetLocalRotation(bottom, Angle.FromDegrees(90));
        }
        AddFixture(-7, 5, "#EDB578", "CMUIndustrial");
        AddFixture(-1, 5, "#E5D5AE", "CMUFixture");
        var alarm = AddFixture(7, 5, "#A8D9F0", "CMUFixture");
        AddFixture(-6, -5, "#F1B679", "CMUIndustrial");
        AddFixture(5, -5, "#ACD8F0", "CMUFixture");
        var beacon = EntityManager.SpawnEntity("CMUWarshipAlertBeaconRed", new EntityCoordinates(alarm, Vector2.Zero));
        if (mode == "industrial-static")
        {
            EntityManager.RemoveComponent<RotatingLightComponent>(beacon);
            _transform.SetLocalRotation(beacon, Angle.FromDegrees(30));
        }
        Spawn("RMCSmallFloodlight", -6, -2);
        Spawn("RMCTallFloodlight", 8, -2);
        for (var px = 4; px <= 6; px++)
        for (var py = -1; py <= 1; py++)
        {
            var water = new Solution();
            water.AddReagent(Water.Id, FixedPoint2.New(60));
            _puddles.TrySpillAt(new EntityCoordinates(grid.Owner, new Vector2(px + 0.5f, py + 0.5f)),
                water, out _, sound: false);
        }
        PowerForReview(mapId);
        PlacePlayer(session, new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));
        shell.WriteLine(Loc.GetString("cmu-cinematic-demo-created", ("map", mapId),
            ("grid", EntityManager.GetNetEntity(grid.Owner))));
        return;

        EntityUid Spawn(string prototype, int sx, int sy) => EntityManager.SpawnEntity(prototype,
            new EntityCoordinates(grid.Owner, new Vector2(sx + 0.5f, sy + 0.5f)));

        EntityUid AddFixture(int sx, int sy, string color, string profile)
        {
            var uid = Spawn("RMCLightFixtureAlwaysPowered", sx, sy);
            if (sy < 0)
                _transform.SetLocalRotation(uid, Angle.FromDegrees(180));
            _lights.SetColor(uid, Color.FromHex(color));
            _lights.SetRadius(uid, 7f);
            var emitter = EntityManager.GetComponent<CMUCinematicEmitterComponent>(uid);
            emitter.Profile = profile;
            EntityManager.Dirty(uid, emitter);
            return uid;
        }
    }

    private void LoadReviewMap(IConsoleShell shell, ICommonSession? session, ResPath path, Vector2 position)
    {
        if (!_loader.TryLoadMap(path, out var loaded, out var grids,
                new DeserializationOptions { InitializeMaps = true }))
        {
            shell.WriteError(Loc.GetString("cmu-cinematic-demo-load-failed"));
            return;
        }
        var mapId = loaded.Value.Comp.MapId;
        EntityManager.AddComponent<CMUCinematicReviewSceneComponent>(loaded.Value.Owner);
        _map.SetPaused(mapId, false);
        // Keep the map's own ambient and all authored geometry/fixtures. The local review copy
        // has fixed power in both Off and Cinematic, rather than requiring a fully started round.
        PowerForReview(mapId);
        // Open dining / recreation room: industrial perimeter, vendors, terminals and furniture.
        PlacePlayer(session, new EntityCoordinates(loaded.Value.Owner, position));
        shell.WriteLine(Loc.GetString("cmu-cinematic-demo-map-loaded", ("path", path.ToString()),
            ("map", mapId), ("x", position.X), ("y", position.Y)));
    }

    private void PowerForReview(MapId map)
    {
        var power = EntityManager.System<PowerReceiverSystem>();
        var query = EntityManager.EntityQueryEnumerator<ApcPowerReceiverComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var receiver, out var xform))
        {
            if (xform.MapID != map)
                continue;
            // The RMC area-power updater skips generic receivers and refuses to update
            // NeedsPower=false. The isolated review copy uses the normal always-powered path.
            EntityManager.RemoveComponent<RMCPowerReceiverComponent>(uid);
            power.SetPowerDisabled(uid, false, receiver);
            power.SetNeedsPower(uid, false, receiver);
        }
    }

    private void PlacePlayer(ICommonSession? session, EntityCoordinates coordinates)
    {
        if (session == null)
            return;
        if (session.AttachedEntity is { } player)
            _transform.SetCoordinates(player, coordinates);
        else
        {
            var viewer = EntityManager.SpawnEntity("CMMobHuman", coordinates);
            EntityManager.AddComponent<GodmodeComponent>(viewer);
            EntityManager.System<OutfitSystem>().SetOutfit(viewer, "CMGearRiflemanEquipped");
            EntityManager.System<MindSystem>().ControlMob(session.UserId, viewer);
        }
        EntityManager.System<GameTicker>().PlayerJoinGame(session, silent: true);
    }

    private void AddAir(EntityUid map)
    {
        var gas = new GasMixture(2500) { Temperature = 293.15f };
        gas.SetMoles(Gas.Oxygen, 21.824879f);
        gas.SetMoles(Gas.Nitrogen, 82.10312f);
        _atmosphere.SetMapAtmosphere(map, false, gas);
    }
}

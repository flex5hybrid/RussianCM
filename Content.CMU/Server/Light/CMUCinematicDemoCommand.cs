using System.Numerics;
using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.CMU14.Lighting;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Server.CMU14.Light;

/// <summary>Explicit admin-only, isolated reproducible scene. Never runs as part of a normal round.</summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed partial class CMUCinematicDemoCommand : LocalizedEntityCommands
{
    private static readonly ProtoId<ReagentPrototype> Water = "Water";
    private static readonly Vector2 ViewingPosition = new(0.5f, 0.5f);
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private PointLightSystem _lights = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private PuddleSystem _puddles = default!;
    [Dependency] private ISharedPlayerManager _players = default!;

    public override string Command => "cmu_cinematic_demo";
    public override string Description => Loc.GetString("cmu-cinematic-demo-description");
    public override string Help => Loc.GetString("cmu-cinematic-demo-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Help);
            return;
        }

        var session = shell.Player;
        if (args.Length == 1 && !_players.TryGetSessionByUsername(args[0], out session))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        var map = _map.CreateMap(out var mapId, runMapInit: true);
        _map.SetAmbientLight(mapId, Color.FromHex("#25303B"));
        var gas = new GasMixture(2500) { Temperature = 293.15f };
        gas.SetMoles(Gas.Oxygen, 21.824879f);
        gas.SetMoles(Gas.Nitrogen, 82.10312f);
        _atmosphere.SetMapAtmosphere(map, false, gas);
        var grid = _map.CreateGridEntity(mapId);
        EntityManager.AddComponent<CMUCinematicAtmosphereComponent>(grid).Density = 0.4f;

        for (var x = -12; x <= 12; x++)
        for (var y = -7; y <= 7; y++)
        {
            var tile = x > 0 ? y >= 2 ? "CMFloorWhiteSterile" : "CMFloorPlate" : "CMFloorSteel";
            _map.SetTile(grid.Owner, grid.Comp, new Vector2i(x, y), new Tile(_tiles[tile].TileId));
            if (x is -12 or 12 || y is -7 or 7 || x == 0 && y is not (0 or 1 or 4))
                Spawn("CMWallMetalAlmayer", x, y);
        }

        Spawn("CMWindowReinforcedAlmayer", 0, 4);
        AddFixture(-9, "#DAB580", "CMUIndustrial");
        AddFixture(-4, "#F1EBDD", "CMUFixture");
        AddFixture(4, "#A6D7E6", "CMUFixture");
        var emergency = AddFixture(9, "#EE3528", "CMUFixture");
        EntityManager.SpawnEntity("CMUWarshipAlertBeaconRed", new EntityCoordinates(emergency, Vector2.Zero));
        Spawn("RMCPropCommunicationsConsole", -8, 3);
        Spawn("RMCPersonalDesktop", 7, 3);
        Spawn("RMCSmallFloodlight", -5, -4);
        Spawn("RMCTallFloodlight", 7, -4);
        var water = new Solution();
        water.AddReagent(Water.Id, FixedPoint2.New(40));
        _puddles.TrySpillAt(new EntityCoordinates(grid.Owner, new Vector2(5.5f, -2.5f)), water, out _, sound: false);

        if (session?.AttachedEntity is { } player)
            _transform.SetCoordinates(player, new EntityCoordinates(grid.Owner, ViewingPosition));
        else if (session != null)
        {
            var viewer = EntityManager.SpawnEntity("MobHuman",
                new EntityCoordinates(grid.Owner, ViewingPosition));
            EntityManager.System<MindSystem>().ControlMob(session.UserId, viewer);
            EntityManager.System<GameTicker>().PlayerJoinGame(session, silent: true);
        }
        shell.WriteLine(Loc.GetString("cmu-cinematic-demo-created", ("map", mapId),
            ("grid", EntityManager.GetNetEntity(grid.Owner))));
        return;

        EntityUid Spawn(string prototype, int x, int y) => EntityManager.SpawnEntity(prototype,
            new EntityCoordinates(grid.Owner, new Vector2(x + 0.5f, y + 0.5f)));

        EntityUid AddFixture(int x, string color, string profile)
        {
            // Keep the real native light point on the room side of the perimeter occluder.
            var uid = Spawn("RMCLightFixtureAlwaysPowered", x, 6);
            _lights.SetColor(uid, Color.FromHex(color));
            _lights.SetRadius(uid, 8f);
            var emitter = EntityManager.GetComponent<CMUCinematicEmitterComponent>(uid);
            emitter.Profile = profile;
            EntityManager.Dirty(uid, emitter);
            return uid;
        }
    }
}

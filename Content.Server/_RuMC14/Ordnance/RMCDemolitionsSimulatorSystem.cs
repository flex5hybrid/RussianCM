using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.Ordnance;

/// <summary>
///     Runs the handheld demolitions simulator by replaying a casing inside a disposable test chamber.
/// </summary>
public sealed class RMCDemolitionsSimulatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDemolitionsSimulatorComponent, MapInitEvent>(OnMapInit);

        Subs.BuiEvents<RMCDemolitionsSimulatorComponent>(RMCDemolitionsSimulatorUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBuiOpened);
            subs.Event<BoundUIClosedEvent>(OnBuiClosed);
            subs.Event<RMCDemolitionsSimulatorSimulateMsg>(OnSimulateMsg);
        });
    }

    private void OnMapInit(Entity<RMCDemolitionsSimulatorComponent> ent, ref MapInitEvent args)
    {
        CreateChamber(ent);
    }

    private void OnBuiOpened(Entity<RMCDemolitionsSimulatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        ToggleViewer(ent.Comp.ChamberCameraEnt, args.Actor, true);
        UpdateUiState(ent);
    }

    private void OnBuiClosed(Entity<RMCDemolitionsSimulatorComponent> ent, ref BoundUIClosedEvent args)
    {
        ToggleViewer(ent.Comp.ChamberCameraEnt, args.Actor, false);
    }

    private void OnSimulateMsg(Entity<RMCDemolitionsSimulatorComponent> ent, ref RMCDemolitionsSimulatorSimulateMsg args)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.CooldownEnd)
        {
            UpdateUiState(ent);
            return;
        }

        if (!_hands.TryGetActiveItem(args.Actor, out var item) || item == null)
        {
            UpdateUiState(ent);
            return;
        }

        if (!TryComp<RMCChembombCasingComponent>(item.Value, out var casing))
        {
            UpdateUiState(ent);
            return;
        }

        RunSimulation(ent, item.Value, casing);
        ent.Comp.CooldownEnd = now + RMCDemolitionsSimulatorComponent.Cooldown;

        // Every run gets a clean chamber so the replay always starts from a known state.
        CreateChamber(ent);
        ToggleViewer(ent.Comp.ChamberCameraEnt, args.Actor, true);

        ent.Comp.PendingExplosion = true;
        ent.Comp.ExplosionAt = now + TimeSpan.FromSeconds(1.5);

        Dirty(ent);
        UpdateUiState(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCDemolitionsSimulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.PendingExplosion || _timing.CurTime < comp.ExplosionAt)
                continue;

            comp.PendingExplosion = false;
            Dirty(uid, comp);

            if (!EntityManager.EntityExists(comp.ChamberCameraEnt))
                continue;

            var xform = Transform(comp.ChamberCameraEnt);
            var center = new MapCoordinates(xform.WorldPosition, xform.MapID);
            _explosion.QueueExplosion(
                center,
                "RMC",
                comp.PendingTotalIntensity,
                comp.PendingIntensitySlope,
                comp.PendingMaxIntensity,
                null,
                addLog: false);
        }
    }

    private void CreateChamber(Entity<RMCDemolitionsSimulatorComponent> ent)
    {
        if (ent.Comp.ChamberMapEnt != EntityUid.Invalid && EntityManager.EntityExists(ent.Comp.ChamberMapEnt))
            QueueDel(ent.Comp.ChamberMapEnt);

        var mapEnt = _mapSystem.CreateMap(out var mapId);
        ent.Comp.ChamberMapEnt = mapEnt;

        var gridEnt = _mapManager.CreateGridEntity(mapId);
        var gridComp = Comp<MapGridComponent>(gridEnt);

        var floorTile = new Tile(_tileDefinitionManager["FloorSteel"].TileId);
        for (var x = -5; x <= 5; x++)
        {
            for (var y = -5; y <= 5; y++)
            {
                _mapSystem.SetTile(gridEnt, gridComp, new Vector2i(x, y), floorTile);
            }
        }

        for (var index = 1; index <= 4; index++)
        {
            Spawn("RMCTrainingDummy", new EntityCoordinates(gridEnt, new Vector2(0, index)));
        }

        var camera = Spawn("RMCDemolitionsCamera", new EntityCoordinates(gridEnt, Vector2.Zero));
        ent.Comp.ChamberCameraEnt = camera;
        ent.Comp.ChamberCamera = GetNetEntity(camera);

        Dirty(ent);
    }

    private void RunSimulation(Entity<RMCDemolitionsSimulatorComponent> simEnt, EntityUid casingUid, RMCChembombCasingComponent casing)
    {
        _solution.TryGetSolution(casingUid, casing.ChemicalSolution, out _, out var solution);
        var estimate = RMCOrdnanceYieldEstimator.Estimate(solution, _prototype, RMCOrdnanceYieldEstimator.FromCasing(casing));

        simEnt.Comp.LastCasingName = MetaData(casingUid).EntityName;
        simEnt.Comp.LastCurrentVolume = estimate.CurrentVolume;
        simEnt.Comp.LastMaxVolume = (float) casing.MaxVolume;
        simEnt.Comp.LastHasExplosion = estimate.HasExplosion;
        simEnt.Comp.LastTotalIntensity = estimate.TotalIntensity;
        simEnt.Comp.LastIntensitySlope = estimate.IntensitySlope;
        simEnt.Comp.LastMaxIntensity = estimate.MaxIntensity;
        simEnt.Comp.LastHasFire = estimate.HasFire;
        simEnt.Comp.LastFireIntensity = estimate.FireIntensity;
        simEnt.Comp.LastFireRadius = estimate.FireRadius;
        simEnt.Comp.LastFireDuration = estimate.FireDuration;

        simEnt.Comp.PendingTotalIntensity = estimate.TotalIntensity;
        simEnt.Comp.PendingIntensitySlope = estimate.IntensitySlope;
        simEnt.Comp.PendingMaxIntensity = estimate.MaxIntensity;

        Dirty(simEnt);
    }

    private void UpdateUiState(Entity<RMCDemolitionsSimulatorComponent> ent)
    {
        var now = _timing.CurTime;
        var onCooldown = now < ent.Comp.CooldownEnd;
        var secondsLeft = onCooldown
            ? Math.Max(0, (int) Math.Ceiling((ent.Comp.CooldownEnd - now).TotalSeconds))
            : 0;

        var state = new RMCDemolitionsSimulatorBuiState(
            ent.Comp.LastCasingName,
            ent.Comp.LastCurrentVolume,
            ent.Comp.LastMaxVolume,
            ent.Comp.LastHasExplosion,
            ent.Comp.LastTotalIntensity,
            ent.Comp.LastIntensitySlope,
            ent.Comp.LastMaxIntensity,
            ent.Comp.LastHasFire,
            ent.Comp.LastFireIntensity,
            ent.Comp.LastFireRadius,
            ent.Comp.LastFireDuration,
            onCooldown,
            secondsLeft,
            ent.Comp.ChamberCamera);

        _ui.SetUiState(ent.Owner, RMCDemolitionsSimulatorUiKey.Key, state);
    }

    private void ToggleViewer(EntityUid camera, EntityUid actor, bool subscribed)
    {
        if (camera == EntityUid.Invalid || !_playerManager.TryGetSessionByEntity(actor, out var session))
            return;

        if (subscribed)
            _viewSubscriber.AddViewSubscriber(camera, session);
        else
            _viewSubscriber.RemoveViewSubscriber(camera, session);
    }
}

using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.Ordnance;

/// <summary>
///     Runs the large console-based explosion simulator by sampling a beaker and replaying the predicted blast
///     against a target formation inside a temporary chamber map.
/// </summary>
public sealed class RMCExplosionSimulatorSystem : EntitySystem
{
    private static readonly SoundPathSpecifier BeepSound = new("/Audio/Machines/twobeep.ogg");
    private const string BeakerSlotId = "beakerSlot";

    private static readonly Dictionary<RMCExplosionSimulatorTarget, Vector2[]> TargetFormations = new()
    {
        [RMCExplosionSimulatorTarget.Marines] =
        [
            new Vector2(2, 1),
            new Vector2(2, -1),
            new Vector2(3, 2),
            new Vector2(3, 0),
            new Vector2(3, -2),
            new Vector2(4, 1),
            new Vector2(4, -1),
        ],
        [RMCExplosionSimulatorTarget.SpecialForces] =
        [
            new Vector2(2, 0),
            new Vector2(2, 1),
            new Vector2(2, -1),
            new Vector2(3, 0),
            new Vector2(3, 1),
        ],
        [RMCExplosionSimulatorTarget.Xenomorphs] =
        [
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(1, -1),
            new Vector2(2, 0),
            new Vector2(2, 1),
            new Vector2(2, -1),
            new Vector2(3, 0),
            new Vector2(3, 1),
            new Vector2(3, -1),
        ],
    };

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly EyeSystem _eye = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCExplosionSimulatorWatchingComponent, MoveInputEvent>(OnWatchingMoveInput);

        Subs.BuiEvents<RMCExplosionSimulatorComponent>(RMCExplosionSimulatorUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBuiOpened);
            subs.Event<BoundUIClosedEvent>(OnBuiClosed);
            subs.Event<RMCExplosionSimulatorSimulateMsg>(OnSimulateMsg);
            subs.Event<RMCExplosionSimulatorTargetMsg>(OnTargetMsg);
            subs.Event<RMCExplosionSimulatorReplayMsg>(OnReplayMsg);
        });
    }

    private void OnBuiOpened(Entity<RMCExplosionSimulatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        ToggleViewer(ent.Comp.ChamberCameraEnt, args.Actor, true);
        UpdateUiState(ent);
    }

    private void OnBuiClosed(Entity<RMCExplosionSimulatorComponent> ent, ref BoundUIClosedEvent args)
    {
        ToggleViewer(ent.Comp.ChamberCameraEnt, args.Actor, false);
    }

    private void OnSimulateMsg(Entity<RMCExplosionSimulatorComponent> ent, ref RMCExplosionSimulatorSimulateMsg args)
    {
        if (ent.Comp.IsProcessing)
        {
            UpdateUiState(ent);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent, BeakerSlotId, out var itemSlot) || itemSlot.Item is not { } beaker)
        {
            UpdateUiState(ent);
            return;
        }

        RunSimulation(ent, beaker);

        ent.Comp.IsProcessing = true;
        ent.Comp.SimulationReady = false;
        ent.Comp.ProcessingEnd = _timing.CurTime + RMCExplosionSimulatorComponent.ProcessingDuration;
        ent.Comp.ProcessingActor = args.Actor;

        Dirty(ent);
        UpdateUiState(ent);
    }

    private void OnTargetMsg(Entity<RMCExplosionSimulatorComponent> ent, ref RMCExplosionSimulatorTargetMsg args)
    {
        ent.Comp.SelectedTarget = args.Target;
        Dirty(ent);
        UpdateUiState(ent);
    }

    private void OnReplayMsg(Entity<RMCExplosionSimulatorComponent> ent, ref RMCExplosionSimulatorReplayMsg args)
    {
        if (!ent.Comp.SimulationReady)
            return;

        CreateChamberAndReplay(ent, args.Actor);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCExplosionSimulatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IsProcessing && _timing.CurTime >= comp.ProcessingEnd)
            {
                comp.IsProcessing = false;
                comp.SimulationReady = true;
                Dirty(uid, comp);

                _audio.PlayPvs(BeepSound, uid);

                if (comp.ProcessingActor != EntityUid.Invalid)
                    _popup.PopupEntity("Simulation complete.", uid, comp.ProcessingActor);

                UpdateUiState((uid, comp));
            }

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

    private void RunSimulation(Entity<RMCExplosionSimulatorComponent> simEnt, EntityUid beaker)
    {
        _solution.TryGetFitsInDispenser(beaker, out _, out var solution);
        var estimate = RMCOrdnanceYieldEstimator.Estimate(
            solution,
            _prototype,
            RMCOrdnanceYieldEstimator.ExplosionSimulationProfile);

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

    private void CreateChamberAndReplay(Entity<RMCExplosionSimulatorComponent> ent, EntityUid actor)
    {
        ToggleViewer(ent.Comp.ChamberCameraEnt, actor, false);

        if (ent.Comp.ChamberMapEnt != EntityUid.Invalid && EntityManager.EntityExists(ent.Comp.ChamberMapEnt))
            QueueDel(ent.Comp.ChamberMapEnt);

        var mapEnt = _mapSystem.CreateMap(out var mapId);
        ent.Comp.ChamberMapEnt = mapEnt;

        var gridEnt = _mapManager.CreateGridEntity(mapId);
        var gridComp = Comp<MapGridComponent>(gridEnt);
        var floorTile = new Tile(_tileDefinitionManager["FloorSteel"].TileId);

        for (var x = -8; x <= 8; x++)
        {
            for (var y = -8; y <= 8; y++)
            {
                _mapSystem.SetTile(gridEnt, gridComp, new Vector2i(x, y), floorTile);
            }
        }

        SpawnTargetEntities(ent.Comp.SelectedTarget, gridEnt);

        var camera = Spawn("RMCDemolitionsCamera", new EntityCoordinates(gridEnt, Vector2.Zero));
        ent.Comp.ChamberCameraEnt = camera;
        ent.Comp.ChamberCamera = GetNetEntity(camera);

        ToggleViewer(camera, actor, true);

        _eye.SetTarget(actor, camera);
        _eye.SetDrawLight(actor, false);
        EnsureComp<RMCExplosionSimulatorWatchingComponent>(actor).Watching = camera;

        ent.Comp.PendingExplosion = true;
        ent.Comp.ExplosionAt = _timing.CurTime + TimeSpan.FromSeconds(2);

        Dirty(ent);
        UpdateUiState(ent);
    }

    private void SpawnTargetEntities(RMCExplosionSimulatorTarget target, EntityUid gridEnt)
    {
        if (!TargetFormations.TryGetValue(target, out var positions))
            return;

        foreach (var position in positions)
        {
            Spawn("RMCTrainingDummy", new EntityCoordinates(gridEnt, position));
        }
    }

    private void UpdateUiState(Entity<RMCExplosionSimulatorComponent> ent)
    {
        var hasBeaker = _itemSlots.TryGetSlot(ent, BeakerSlotId, out var slot) && slot.Item.HasValue;
        var secondsLeft = ent.Comp.IsProcessing
            ? Math.Max(0, (int) Math.Ceiling((ent.Comp.ProcessingEnd - _timing.CurTime).TotalSeconds))
            : 0;

        var state = new RMCExplosionSimulatorBuiState(
            hasBeaker,
            ent.Comp.SelectedTarget,
            ent.Comp.IsProcessing,
            secondsLeft,
            ent.Comp.SimulationReady,
            ent.Comp.LastHasExplosion,
            ent.Comp.LastTotalIntensity,
            ent.Comp.LastIntensitySlope,
            ent.Comp.LastMaxIntensity,
            ent.Comp.LastHasFire,
            ent.Comp.LastFireIntensity,
            ent.Comp.LastFireRadius,
            ent.Comp.LastFireDuration,
            ent.Comp.ChamberCamera);

        _ui.SetUiState(ent.Owner, RMCExplosionSimulatorUiKey.Key, state);
    }

    /// <summary>
    ///     Any movement input exits the detached camera view and returns the actor to their normal eye target.
    /// </summary>
    private void OnWatchingMoveInput(Entity<RMCExplosionSimulatorWatchingComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement || !HasComp<ActorComponent>(ent))
            return;

        _eye.SetTarget(ent.Owner, null);
        _eye.SetDrawLight(ent.Owner, true);

        if (ent.Comp.Watching is { } watching)
            ToggleViewer(watching, ent.Owner, false);

        ent.Comp.Watching = EntityUid.Invalid;
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

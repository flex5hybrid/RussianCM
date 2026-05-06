using System.Numerics;
using System.Linq;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.Ordnance;

/// <summary>
///     Runs the handheld demolitions simulator by replaying the held ordnance sample inside a disposable chamber.
/// </summary>
public sealed class RMCDemolitionsSimulatorSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RMCOrdnanceSampleResolverSystem _resolver = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
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

        if (!_hands.TryGetActiveItem(args.Actor, out var item) ||
            item == null ||
            !RunSimulation(ent, item.Value))
        {
            UpdateUiState(ent);
            return;
        }

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

            if (!comp.PendingFire)
                continue;

            var tile = xform.Coordinates.SnapToGrid(EntityManager, _mapManager);
            _flammable.SpawnFireDiamond(
                comp.PendingFireEntity,
                tile,
                (int) comp.PendingFireRadius,
                (int) comp.PendingFireIntensity,
                (int) comp.PendingFireDuration);
        }
    }

    private void CreateChamber(Entity<RMCDemolitionsSimulatorComponent> ent)
    {
        if (ent.Comp.ChamberMapEnt != EntityUid.Invalid && EntityManager.EntityExists(ent.Comp.ChamberMapEnt))
            QueueDel(ent.Comp.ChamberMapEnt);

        var options = DeserializationOptions.Default with { InitializeMaps = true };
        if (!_mapLoader.TryLoadMap(ent.Comp.ChamberMap, out var map, out var grids, options) ||
            map is not { } chamberMap ||
            grids == null ||
            grids.Count == 0)
            return;

        var chamberGrid = grids.First();
        ent.Comp.ChamberMapEnt = chamberMap.Owner;

        for (var index = 1; index <= 4; index++)
        {
            Spawn("RMCTrainingDummy", new EntityCoordinates(chamberGrid.Owner, new Vector2(0, index)));
        }

        var camera = Spawn("RMCDemolitionsCamera", new EntityCoordinates(chamberGrid.Owner, Vector2.Zero));
        ent.Comp.ChamberCameraEnt = camera;
        ent.Comp.ChamberCamera = GetNetEntity(camera);

        Dirty(ent);
    }

    private bool RunSimulation(Entity<RMCDemolitionsSimulatorComponent> simEnt, EntityUid sampleUid)
    {
        if (!_resolver.TryResolveSample(sampleUid, out var sample))
            return false;

        simEnt.Comp.LastCasingName = sample.Name;
        simEnt.Comp.LastCurrentVolume = sample.Estimate.CurrentVolume;
        simEnt.Comp.LastMaxVolume = sample.MaxVolume;
        ApplyEstimate(simEnt.Comp, sample.Estimate);
        Dirty(simEnt);
        return true;
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

    private static void ApplyEstimate(RMCDemolitionsSimulatorComponent comp, in RMCOrdnanceYieldEstimate estimate)
    {
        comp.LastHasExplosion = estimate.HasExplosion;
        comp.LastTotalIntensity = estimate.TotalIntensity;
        comp.LastIntensitySlope = estimate.IntensitySlope;
        comp.LastMaxIntensity = estimate.MaxIntensity;
        comp.LastHasFire = estimate.HasFire;
        comp.LastFireIntensity = estimate.FireIntensity;
        comp.LastFireRadius = estimate.FireRadius;
        comp.LastFireDuration = estimate.FireDuration;
        comp.PendingTotalIntensity = estimate.TotalIntensity;
        comp.PendingIntensitySlope = estimate.IntensitySlope;
        comp.PendingMaxIntensity = estimate.MaxIntensity;
        comp.PendingFire = estimate.HasFire;
        comp.PendingFireIntensity = estimate.FireIntensity;
        comp.PendingFireRadius = estimate.FireRadius;
        comp.PendingFireDuration = estimate.FireDuration;
        comp.PendingFireEntity = estimate.FireEntity;
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

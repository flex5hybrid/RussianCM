using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Explosion.Components;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Payload.Components;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Ordnance;

public sealed class RMCChembombSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCChembombCasingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCChembombCasingComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<RMCChembombCasingComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCChembombCasingComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCChembombCasingComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<RMCChembombCasingComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
        SubscribeLocalEvent<RMCChembombCasingComponent, RMCCasingScrewDoAfterEvent>(OnScrewDoAfter);
        SubscribeLocalEvent<RMCChembombCasingComponent, RMCCasingCutDoAfterEvent>(OnCutDoAfter);
        SubscribeLocalEvent<RMCDemolitionsScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
    }

    private void OnUseInHand(Entity<RMCChembombCasingComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (ent.Comp.Stage == RMCCasingAssemblyStage.Armed)
            return;

        _popup.PopupEntity(Loc.GetString("rmc-chembomb-not-armed"), ent, args.User, PopupType.SmallCaution);
        args.Handled = true;
    }

    private void OnScrewDoAfter(Entity<RMCChembombCasingComponent> ent, ref RMCCasingScrewDoAfterEvent args)
    {
        if (args.Cancelled || _net.IsClient)
            return;

        if (ent.Comp.Stage == RMCCasingAssemblyStage.Open)
        {
            ent.Comp.Stage = RMCCasingAssemblyStage.Sealed;
            _itemSlots.SetLock(ent, "detonator", true);
            _popup.PopupEntity(Loc.GetString("rmc-chembomb-sealed"), ent, args.User);
        }
        else if (ent.Comp.Stage == RMCCasingAssemblyStage.Sealed)
        {
            ent.Comp.Stage = RMCCasingAssemblyStage.Open;
            _itemSlots.SetLock(ent, "detonator", false);
            _popup.PopupEntity(Loc.GetString("rmc-chembomb-unsealed"), ent, args.User);
        }

        Dirty(ent);
    }

    private void OnCutDoAfter(Entity<RMCChembombCasingComponent> ent, ref RMCCasingCutDoAfterEvent args)
    {
        if (args.Cancelled || _net.IsClient)
            return;

        if (ent.Comp.Stage == RMCCasingAssemblyStage.Sealed)
        {
            ent.Comp.Stage = RMCCasingAssemblyStage.Armed;
            _popup.PopupEntity(Loc.GetString("rmc-chembomb-armed"), ent, args.User);
        }
        else if (ent.Comp.Stage == RMCCasingAssemblyStage.Armed)
        {
            ent.Comp.Stage = RMCCasingAssemblyStage.Sealed;
            _popup.PopupEntity(Loc.GetString("rmc-chembomb-disarmed"), ent, args.User);
        }

        Dirty(ent);
    }

    private void OnScannerAfterInteract(Entity<RMCDemolitionsScannerComponent> scanner, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<RMCChembombCasingComponent>(target, out var casing))
            return;

        if (_net.IsClient)
            return;

        args.Handled = true;

        float powerMod = 0f, falloffMod = 0f, intensityMod = 0f, radiusMod = 0f, durationMod = 0f;
        bool hasExplosive = false, hasIncendiary = false;

        if (_solution.TryGetSolution(target, casing.ChemicalSolution, out _, out var solution))
        {
            foreach (var reagent in solution)
            {
                if (!_prototype.TryIndexReagent(reagent.Reagent.Prototype, out var proto))
                    continue;

                var qty = (float) reagent.Quantity;
                powerMod += qty * (float) proto!.PowerMod;
                falloffMod += qty * (float) proto.FalloffMod;
                intensityMod += qty * (float) proto.IntensityMod;
                radiusMod += qty * (float) proto.RadiusMod;
                durationMod += qty * (float) proto.DurationMod;

                if (proto.PowerMod > FixedPoint2.Zero) hasExplosive = true;
                if (proto.IntensityMod > FixedPoint2.Zero || proto.Intensity > 0) hasIncendiary = true;
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Loc.GetString("rmc-demolitions-sim-header", ("name", MetaData(target).EntityName)));

        FixedPoint2 currentVol = FixedPoint2.Zero;
        if (_solution.TryGetSolution(target, casing.ChemicalSolution, out _, out var chem))
            currentVol = chem.Volume;
        sb.AppendLine(Loc.GetString("rmc-demolitions-sim-volume",
            ("current", (int)(float) currentVol), ("max", (int)(float) casing.MaxVolume)));

        if (!hasExplosive && !hasIncendiary)
        {
            sb.AppendLine(Loc.GetString("rmc-demolitions-sim-empty"));
        }
        else
        {
            if (hasExplosive || powerMod > 0)
            {
                float power = casing.BasePower + powerMod;
                float falloff = MathF.Max(casing.MinFalloff, casing.BaseFalloff + falloffMod);
                float approxRadius = MathF.Sqrt(MathF.Max(1f, power) / MathF.Max(1.5f, falloff / 14f));
                sb.AppendLine(Loc.GetString("rmc-demolitions-sim-explosion",
                    ("power", (int) power), ("falloff", (int) falloff), ("radius", approxRadius.ToString("F1"))));
            }
            if (hasIncendiary)
            {
                float intensity = Math.Clamp(casing.MinFireIntensity + intensityMod, casing.MinFireIntensity, casing.MaxFireIntensity);
                float radius = Math.Clamp(casing.MinFireRadius + radiusMod, casing.MinFireRadius, casing.MaxFireRadius);
                float duration = Math.Clamp(casing.MinFireDuration + durationMod, casing.MinFireDuration, casing.MaxFireDuration);
                sb.AppendLine(Loc.GetString("rmc-demolitions-sim-fire",
                    ("intensity", (int) intensity), ("radius", (int) radius), ("duration", (int) duration)));
            }
        }

        _popup.PopupEntity(sb.ToString(), target, args.User, PopupType.Large);
    }

    private void OnTrigger(Entity<RMCChembombCasingComponent> ent, ref TriggerEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Stage != RMCCasingAssemblyStage.Armed)
        {
            QueueDel(ent);
            args.Handled = true;
            return;
        }

        var comp = ent.Comp;

        if (!_solution.TryGetSolution(ent.Owner, comp.ChemicalSolution, out _, out var solution))
        {
            QueueDel(ent);
            args.Handled = true;
            return;
        }

        float powerMod = 0f;
        float falloffMod = 0f;
        float intensityMod = 0f;
        float radiusMod = 0f;
        float durationMod = 0f;
        EntProtoId fireEntity = comp.DefaultFireEntity;
        bool hasExplosive = false;
        bool hasIncendiary = false;

        foreach (var reagent in solution)
        {
            if (!_prototype.TryIndexReagent(reagent.Reagent.Prototype, out var proto))
                continue;

            var qty = (float) reagent.Quantity;

            powerMod += qty * (float) proto!.PowerMod;
            falloffMod += qty * (float) proto.FalloffMod;
            intensityMod += qty * (float) proto.IntensityMod;
            radiusMod += qty * (float) proto.RadiusMod;
            durationMod += qty * (float) proto.DurationMod;

            if (proto.PowerMod > FixedPoint2.Zero)
                hasExplosive = true;

            if (proto.IntensityMod > FixedPoint2.Zero || proto.Intensity > 0)
            {
                hasIncendiary = true;
                if (proto.Intensity > 0)
                    fireEntity = proto.FireEntity;
            }
        }

        var coords = _transform.GetMoverCoordinates(ent.Owner);

        if (hasExplosive || powerMod > 0)
        {
            float power = comp.BasePower + powerMod;
            float falloff = MathF.Max(comp.MinFalloff, comp.BaseFalloff + falloffMod);

            float totalIntensity = MathF.Max(1f, power);
            float intensitySlope = MathF.Max(1.5f, falloff / 14f);
            float maxIntensity = MathF.Max(5f, power / 15f);

            _explosion.QueueExplosion(
                _transform.GetMapCoordinates(ent.Owner),
                "RMC",
                totalIntensity,
                intensitySlope,
                maxIntensity,
                ent.Owner);
        }

        if (hasIncendiary)
        {
            float intensity = Math.Clamp(comp.MinFireIntensity + intensityMod, comp.MinFireIntensity, comp.MaxFireIntensity);
            float radius = Math.Clamp(comp.MinFireRadius + radiusMod, comp.MinFireRadius, comp.MaxFireRadius);
            float duration = Math.Clamp(comp.MinFireDuration + durationMod, comp.MinFireDuration, comp.MaxFireDuration);

            var tile = coords.SnapToGrid(EntityManager, _map);
            _flammable.SpawnFireDiamond(fireEntity, tile, (int) radius, (int) intensity, (int) duration);
        }

        args.Handled = true;
        QueueDel(ent.Owner);
    }

    private void OnInteractUsing(Entity<RMCChembombCasingComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        // Screwdriver: seal (Open→Sealed) or unseal (Sealed→Open)
        if (_tool.HasQuality(args.Used, "Screwing"))
        {
            if (ent.Comp.Stage == RMCCasingAssemblyStage.Armed)
            {
                _popup.PopupEntity(Loc.GetString("rmc-chembomb-seal-disarm-first"), ent, args.User, PopupType.SmallCaution);
                args.Handled = true;
                return;
            }

            if (ent.Comp.Stage == RMCCasingAssemblyStage.Open && !ent.Comp.HasActiveDetonator)
            {
                _popup.PopupEntity(Loc.GetString("rmc-chembomb-seal-no-detonator"), ent, args.User, PopupType.SmallCaution);
                args.Handled = true;
                return;
            }

            StartToolDoAfter<RMCCasingScrewDoAfterEvent>(ent, args.User, args.Used, 2f);
            args.Handled = true;
            return;
        }

        // Wirecutters: arm (Sealed→Armed) or disarm (Armed→Sealed)
        if (_tool.HasQuality(args.Used, "Cutting"))
        {
            if (ent.Comp.Stage == RMCCasingAssemblyStage.Open)
            {
                _popup.PopupEntity(Loc.GetString("rmc-chembomb-arm-seal-first"), ent, args.User, PopupType.SmallCaution);
                args.Handled = true;
                return;
            }

            StartToolDoAfter<RMCCasingCutDoAfterEvent>(ent, args.User, args.Used, 2f);
            args.Handled = true;
            return;
        }

        // Beaker: fill with chemicals (only when Open)
        if (ent.Comp.Stage != RMCCasingAssemblyStage.Open)
            return;

        var used = args.Used;
        if (!HasComp<FitsInDispenserComponent>(used))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.ChemicalSolution, out var casingSoln, out var casingChem))
            return;

        var remaining = casingChem.AvailableVolume;
        if (remaining <= FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString("rmc-chembomb-full"), ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        if (!_solution.TryGetFitsInDispenser(used, out var beakerSoln, out var beakerChem))
            return;

        if (beakerChem.Volume <= FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString("rmc-chembomb-beaker-empty"), ent.Owner, args.User, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        var toTransfer = FixedPoint2.Min(remaining, beakerChem.Volume);
        var transferred = _solution.SplitSolution(beakerSoln.Value, toTransfer);
        _solution.TryAddSolution(casingSoln.Value, transferred);

        _popup.PopupEntity(
            Loc.GetString("rmc-chembomb-fill", ("amount", toTransfer), ("total", casingChem.Volume + toTransfer), ("max", ent.Comp.MaxVolume)),
            ent.Owner, args.User);

        args.Handled = true;
    }

    private void OnExamined(Entity<RMCChembombCasingComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(RMCChembombCasingComponent)))
        {
            FixedPoint2 currentVol = FixedPoint2.Zero;
            if (_solution.TryGetSolution(ent.Owner, ent.Comp.ChemicalSolution, out _, out var chem))
                currentVol = chem.Volume;

            args.PushMarkup(Loc.GetString("rmc-chembomb-examine-volume",
                ("current", currentVol),
                ("max", ent.Comp.MaxVolume)));

            if (ent.Comp.HasActiveDetonator)
                args.PushMarkup(Loc.GetString("rmc-chembomb-examine-detonator"));
            else
                args.PushMarkup(Loc.GetString("rmc-chembomb-examine-no-detonator"));

            args.PushMarkup(ent.Comp.Stage switch
            {
                RMCCasingAssemblyStage.Sealed => Loc.GetString("rmc-chembomb-examine-sealed"),
                RMCCasingAssemblyStage.Armed  => Loc.GetString("rmc-chembomb-examine-locked"),
                _                             => Loc.GetString("rmc-chembomb-examine-open"),
            });
        }
    }

    private void OnItemInserted(Entity<RMCChembombCasingComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // Старая система детонаторов
        if (TryComp<RMCDetonatorAssemblyComponent>(args.Entity, out var oldComp))
        {
            if (!oldComp.Ready)
                return;
            ent.Comp.HasActiveDetonator = true;
            Dirty(ent);
            return;
        }

        // Новая система: RMCOrdnanceAssembly должна быть закрыта отвёрткой
        if (!TryComp<RMCOrdnanceAssemblyComponent>(args.Entity, out var assembly) || !assembly.IsLocked)
            return;

        ent.Comp.HasActiveDetonator = true;
        Dirty(ent);

        // Добавляем компонент триггера на корпус в зависимости от типа сенсора
        var sensor = GetSensorType(assembly);
        switch (sensor)
        {
            case null: // DoubleIgniter — мгновенная детонация
            {
                var trigger = EnsureComp<OnUseTimerTriggerComponent>(ent);
                trigger.Delay = 1f;
                trigger.DoPopup = false;
                trigger.InitialBeepDelay = 0f;
                trigger.BeepInterval = 99999f;
                break;
            }
            case RMCOrdnancePartType.RMCOrdnanceTimer:
            {
                var trigger = EnsureComp<OnUseTimerTriggerComponent>(ent);
                trigger.Delay = assembly.TimerDelay;
                trigger.DelayOptions = new List<float> { 3f, 5f, 10f, 30f };
                trigger.DoPopup = true;
                trigger.InitialBeepDelay = 0f;
                trigger.BeepInterval = 5f;
                break;
            }
            // Proximity и Signaler: триггер реализуется отдельно
        }
    }

    private void OnItemRemoved(Entity<RMCChembombCasingComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Старая система
        if (HasComp<RMCDetonatorAssemblyComponent>(args.Entity))
        {
            ent.Comp.HasActiveDetonator = false;
            Dirty(ent);
            return;
        }

        // Новая система
        if (!HasComp<RMCOrdnanceAssemblyComponent>(args.Entity))
            return;

        RemCompDeferred<OnUseTimerTriggerComponent>(ent);
        ent.Comp.HasActiveDetonator = false;
        Dirty(ent);
    }

    /// <summary>
    /// Возвращает тип сенсора сборки (не-igniter часть), или null если обе части — igniters.
    /// </summary>
    private static RMCOrdnancePartType? GetSensorType(RMCOrdnanceAssemblyComponent comp)
    {
        if (comp.LeftPartType != null && comp.LeftPartType != RMCOrdnancePartType.RMCOrdnanceIgniter)
            return comp.LeftPartType;
        if (comp.RightPartType != null && comp.RightPartType != RMCOrdnancePartType.RMCOrdnanceIgniter)
            return comp.RightPartType;
        return null;
    }

    private void StartToolDoAfter<T>(Entity<RMCChembombCasingComponent> ent, EntityUid user, EntityUid tool, float delay)
        where T : SimpleDoAfterEvent, new()
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new T(), ent, ent, tool)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }
}

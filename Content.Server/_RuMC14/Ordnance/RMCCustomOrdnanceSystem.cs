using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Mortar;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Ordnance;

/// <summary>
/// Сборка кастомных снарядов (84мм ракета и 80мм миномётный снаряд):
/// труба/корпус + боеголовка → готовый снаряд.
/// Параметры взрыва вычисляются из реагентов боеголовки при сборке.
/// Для ракеты: копирование параметров с картриджа на проджектайл в момент выстрела.
/// Для миномётного снаряда: детонация по событию <see cref="MortarShellLandEvent"/>.
/// </summary>
public sealed class RMCCustomOrdnanceSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RMCReagentSystem _reagents = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly EntProtoId AssembledRocket = "RMCCustomRocket84mm";
    private static readonly EntProtoId AssembledMortarShell = "RMCCustomMortarShell80mm";

    // Временное хранилище: gun UID → список снарядов из AmmoShotEvent.
    // Очищается в GunShotEvent того же тика.
    private readonly Dictionary<EntityUid, List<EntityUid>> _pendingProjectiles = new();

    public override void Initialize()
    {
        // Сборка ракеты: труба — target, боеголовка — used (или наоборот)
        SubscribeLocalEvent<RuMCRocketTubeComponent, InteractUsingEvent>(OnTubeInteractUsing);
        SubscribeLocalEvent<RuMCRocketWarheadComponent, InteractUsingEvent>(OnWarheadInteractUsing);

        // Сборка миномётного снаряда: корпус — target, боеголовка — used
        SubscribeLocalEvent<RuMCMortarShellCasingComponent, InteractUsingEvent>(OnMortarCasingInteractUsing);

        // DoAfter
        SubscribeLocalEvent<RuMCRocketTubeComponent, RuMCOrdnanceAssembleDoAfterEvent>(OnRocketAssembleDoAfter);
        SubscribeLocalEvent<RuMCMortarShellCasingComponent, RuMCOrdnanceAssembleDoAfterEvent>(OnMortarAssembleDoAfter);

        // Передача параметров взрыва с картриджа на проджектайл при выстреле
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GunShotEvent>(OnGunShot);

        // Детонация ракеты при столкновении (TriggerOnCollide → TriggerEvent)
        SubscribeLocalEvent<RuMCCustomOrdnancePayloadComponent, TriggerEvent>(OnRocketTrigger);

        // Детонация миномётного снаряда при приземлении
        SubscribeLocalEvent<RuMCCustomOrdnancePayloadComponent, MortarShellLandEvent>(OnMortarShellLand);
    }

    // ─── Сборка: 84мм ракета ───────────────────────────────────────────────────

    private void OnTubeInteractUsing(Entity<RuMCRocketTubeComponent> tube, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!HasComp<RuMCRocketWarheadComponent>(args.Used))
            return;

        TryStartRocketAssembly(tube.Owner, tube.Comp.RequiredFuel, tube.Comp.FuelSolution,
            args.Used, args.User, ref args.Handled);
    }

    private void OnWarheadInteractUsing(Entity<RuMCRocketWarheadComponent> warhead, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryComp<RuMCRocketTubeComponent>(args.Used, out var tubeComp))
            return;

        TryStartRocketAssembly(args.Used, tubeComp.RequiredFuel, tubeComp.FuelSolution,
            warhead.Owner, args.User, ref args.Handled);
    }

    private void TryStartRocketAssembly(EntityUid tubeUid, float requiredFuel, string fuelSolution,
        EntityUid warheadUid, EntityUid user, ref bool handled)
    {
        if (!ValidateWarhead(warheadUid, tubeUid, user,
            "rmc-rocket-warhead-not-armed", "rmc-rocket-warhead-no-detonator"))
        {
            handled = true;
            return;
        }

        if (!ValidateFuel(tubeUid, fuelSolution, requiredFuel, user,
            "rmc-rocket-tube-no-fuel", "rmc-rocket-tube-insufficient-fuel"))
        {
            handled = true;
            return;
        }

        StartAssemblyDoAfter(tubeUid, warheadUid, user, "rmc-rocket-assembling");
        handled = true;
    }

    private void OnRocketAssembleDoAfter(Entity<RuMCRocketTubeComponent> tube, ref RuMCOrdnanceAssembleDoAfterEvent args)
    {
        if (args.Cancelled || _net.IsClient || args.Target is not { } warheadUid)
            return;

        if (!TryComp<RMCChembombCasingComponent>(warheadUid, out var casing) ||
            casing.Stage != RMCCasingAssemblyStage.Armed || !casing.HasActiveDetonator)
        {
            _popup.PopupEntity(Loc.GetString("rmc-rocket-assembly-failed"), tube, args.User, PopupType.SmallCaution);
            return;
        }

        SpawnAssembledOrdnance(AssembledRocket, warheadUid, casing, args.User);
        QueueDel(tube.Owner);
        QueueDel(warheadUid);
        _popup.PopupEntity(Loc.GetString("rmc-rocket-assembled"), args.User, args.User);
    }

    // ─── Сборка: 80мм миномётный снаряд ───────────────────────────────────────

    private void OnMortarCasingInteractUsing(Entity<RuMCMortarShellCasingComponent> casing, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        // Боеголовка — это RMCChembombCasing без маркера RuMCMortarShellCasing
        if (!HasComp<RMCChembombCasingComponent>(args.Used) || HasComp<RuMCMortarShellCasingComponent>(args.Used))
            return;

        TryStartMortarAssembly(casing.Owner, casing.Comp.RequiredFuel, casing.Comp.FuelSolution,
            args.Used, args.User, ref args.Handled);
    }

    private void TryStartMortarAssembly(EntityUid casingUid, float requiredFuel, string fuelSolution,
        EntityUid warheadUid, EntityUid user, ref bool handled)
    {
        if (!ValidateWarhead(warheadUid, casingUid, user,
            "rmc-mortar-shell-warhead-not-armed", "rmc-mortar-shell-warhead-no-detonator"))
        {
            handled = true;
            return;
        }

        if (!ValidateFuel(casingUid, fuelSolution, requiredFuel, user,
            "rmc-mortar-shell-no-fuel", "rmc-mortar-shell-insufficient-fuel"))
        {
            handled = true;
            return;
        }

        StartAssemblyDoAfter(casingUid, warheadUid, user, "rmc-mortar-shell-assembling");
        handled = true;
    }

    private void OnMortarAssembleDoAfter(Entity<RuMCMortarShellCasingComponent> casing, ref RuMCOrdnanceAssembleDoAfterEvent args)
    {
        if (args.Cancelled || _net.IsClient || args.Target is not { } warheadUid)
            return;

        if (!TryComp<RMCChembombCasingComponent>(warheadUid, out var warheadCasing) ||
            warheadCasing.Stage != RMCCasingAssemblyStage.Armed || !warheadCasing.HasActiveDetonator)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-shell-assembly-failed"), casing, args.User, PopupType.SmallCaution);
            return;
        }

        SpawnAssembledOrdnance(AssembledMortarShell, warheadUid, warheadCasing, args.User);
        QueueDel(casing.Owner);
        QueueDel(warheadUid);
        _popup.PopupEntity(Loc.GetString("rmc-mortar-shell-assembled"), args.User, args.User);
    }

    // ─── Общая логика сборки ──────────────────────────────────────────────────

    private bool ValidateWarhead(EntityUid warheadUid, EntityUid messageTarget, EntityUid user,
        string notArmedKey, string noDetonatorKey)
    {
        if (!TryComp<RMCChembombCasingComponent>(warheadUid, out var casing))
            return false;

        if (casing.Stage != RMCCasingAssemblyStage.Armed)
        {
            _popup.PopupEntity(Loc.GetString(notArmedKey), messageTarget, user, PopupType.SmallCaution);
            return false;
        }

        if (!casing.HasActiveDetonator)
        {
            _popup.PopupEntity(Loc.GetString(noDetonatorKey), messageTarget, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private bool ValidateFuel(EntityUid tubeUid, string fuelSolution, float required, EntityUid user,
        string noFuelKey, string insufficientFuelKey)
    {
        if (!_solution.TryGetSolution(tubeUid, fuelSolution, out _, out var fuel))
        {
            _popup.PopupEntity(Loc.GetString(noFuelKey), tubeUid, user, PopupType.SmallCaution);
            return false;
        }

        if ((float) fuel.Volume < required)
        {
            _popup.PopupEntity(Loc.GetString(insufficientFuelKey,
                ("required", (int) required),
                ("current", (int)(float) fuel.Volume)),
                tubeUid, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void StartAssemblyDoAfter(EntityUid sourceUid, EntityUid warheadUid, EntityUid user, string startMsgKey)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, 3f,
            new RuMCOrdnanceAssembleDoAfterEvent(), sourceUid, warheadUid)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            _popup.PopupEntity(Loc.GetString(startMsgKey), sourceUid, user);
    }

    private void SpawnAssembledOrdnance(EntProtoId proto, EntityUid warheadUid,
        RMCChembombCasingComponent warheadCasing, EntityUid user)
    {
        var ordnanceEnt = Spawn(proto, _transform.GetMapCoordinates(user));
        var comp = EnsureComp<RuMCCustomOrdnancePayloadComponent>(ordnanceEnt);
        BakePayloadIntoComponent(warheadUid, warheadCasing, comp);
        Dirty(ordnanceEnt, comp);
    }

    /// <summary>
    /// Вычисляет и записывает параметры взрыва/пожара из реагентов боеголовки в компонент.
    /// Формула идентична <see cref="RMCChembombSystem.OnTrigger"/> для паритета с SSCM13.
    /// </summary>
    private void BakePayloadIntoComponent(EntityUid warheadUid, RMCChembombCasingComponent casing,
        RuMCCustomOrdnancePayloadComponent comp)
    {
        float powerMod = 0f, falloffMod = 0f, intensityMod = 0f, radiusMod = 0f, durationMod = 0f;
        bool hasExplosion = false, hasIncendiary = false;
        var fireEntity = casing.DefaultFireEntity;

        if (_solution.TryGetSolution(warheadUid, casing.ChemicalSolution, out _, out var chemicals))
        {
            foreach (var reagent in chemicals)
            {
                if (!_reagents.TryIndex(reagent.Reagent, out var proto))
                    continue;

                var qty = (float) reagent.Quantity;
                powerMod += qty * (float) proto.PowerMod;
                falloffMod += qty * (float) proto.FalloffMod;
                intensityMod += qty * (float) proto.IntensityMod;
                radiusMod += qty * (float) proto.RadiusMod;
                durationMod += qty * (float) proto.DurationMod;

                if (proto.PowerMod > FixedPoint2.Zero)
                    hasExplosion = true;

                if (proto.IntensityMod > FixedPoint2.Zero || proto.Intensity > 0)
                {
                    hasIncendiary = true;
                    if (proto.Intensity > 0)
                        fireEntity = proto.FireEntity;
                }
            }
        }

        if (hasExplosion || powerMod > 0f)
        {
            var power = casing.BasePower + powerMod;
            var falloff = MathF.Max(casing.MinFalloff, casing.BaseFalloff + falloffMod);
            comp.HasExplosion = true;
            comp.TotalIntensity = MathF.Max(1f, power);
            comp.IntensitySlope = MathF.Max(1.5f, falloff / 14f);
            comp.MaxIntensity = MathF.Max(5f, power / 15f);
        }

        if (hasIncendiary)
        {
            comp.HasFire = true;
            comp.FireIntensity = Math.Clamp(casing.MinFireIntensity + intensityMod,
                casing.MinFireIntensity, casing.MaxFireIntensity);
            comp.FireRadius = Math.Clamp(casing.MinFireRadius + radiusMod,
                casing.MinFireRadius, casing.MaxFireRadius);
            comp.FireDuration = Math.Clamp(casing.MinFireDuration + durationMod,
                casing.MinFireDuration, casing.MaxFireDuration);
            comp.FireEntity = fireEntity;
        }
    }

    // ─── Передача параметров с картриджа на проджектайл (ракета) ──────────────

    private void OnAmmoShot(Entity<BallisticAmmoProviderComponent> gun, ref AmmoShotEvent args)
    {
        if (args.FiredProjectiles.Count == 0)
            return;

        _pendingProjectiles[gun.Owner] = new List<EntityUid>(args.FiredProjectiles);
    }

    private void OnGunShot(Entity<BallisticAmmoProviderComponent> gun, ref GunShotEvent args)
    {
        if (!_pendingProjectiles.TryGetValue(gun.Owner, out var projectiles))
            return;

        _pendingProjectiles.Remove(gun.Owner);

        foreach (var (ammoUid, _) in args.Ammo)
        {
            if (ammoUid == null)
                continue;

            if (!TryComp<RuMCCustomOrdnancePayloadComponent>(ammoUid.Value, out var cartridgeComp))
                continue;

            // Копируем параметры с картриджа на все спаунутые проджектайлы
            foreach (var projectileUid in projectiles)
            {
                var projComp = EnsureComp<RuMCCustomOrdnancePayloadComponent>(projectileUid);
                projComp.HasExplosion = cartridgeComp.HasExplosion;
                projComp.TotalIntensity = cartridgeComp.TotalIntensity;
                projComp.IntensitySlope = cartridgeComp.IntensitySlope;
                projComp.MaxIntensity = cartridgeComp.MaxIntensity;
                projComp.HasFire = cartridgeComp.HasFire;
                projComp.FireIntensity = cartridgeComp.FireIntensity;
                projComp.FireRadius = cartridgeComp.FireRadius;
                projComp.FireDuration = cartridgeComp.FireDuration;
                projComp.FireEntity = cartridgeComp.FireEntity;
                Dirty(projectileUid, projComp);
            }

            break; // Один снаряд за выстрел
        }
    }

    // ─── Детонация ─────────────────────────────────────────────────────────────

    private void OnRocketTrigger(Entity<RuMCCustomOrdnancePayloadComponent> ent, ref TriggerEvent args)
    {
        if (_net.IsClient)
            return;

        // Миномётные снаряды детонируют через OnMortarShellLand, а не здесь
        if (HasComp<MortarShellComponent>(ent))
            return;

        ExplodePayload(ent.Comp,
            _transform.GetMapCoordinates(ent.Owner),
            _transform.GetMoverCoordinates(ent.Owner),
            ent.Owner);

        args.Handled = true;
    }

    private void OnMortarShellLand(Entity<RuMCCustomOrdnancePayloadComponent> ent, ref MortarShellLandEvent args)
    {
        if (_net.IsClient)
            return;

        ExplodePayload(ent.Comp,
            _transform.ToMapCoordinates(args.Coordinates),
            args.Coordinates,
            ent.Owner);
    }

    private void ExplodePayload(RuMCCustomOrdnancePayloadComponent comp, MapCoordinates mapCoords,
        EntityCoordinates coords, EntityUid source)
    {
        if (comp.HasExplosion)
        {
            _explosion.QueueExplosion(mapCoords, "RMC",
                comp.TotalIntensity, comp.IntensitySlope, comp.MaxIntensity, source);
        }

        if (comp.HasFire)
        {
            var tile = coords.SnapToGrid(EntityManager, _map);
            _flammable.SpawnFireDiamond(comp.FireEntity, tile,
                (int) comp.FireRadius, (int) comp.FireIntensity, (int) comp.FireDuration);
        }
    }
}

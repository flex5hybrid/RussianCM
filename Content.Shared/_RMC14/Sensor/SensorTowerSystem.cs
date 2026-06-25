using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Content.Shared.Administration.Logs;
using Content.Shared._RMC14.Weapons.Ranged.IFF;

namespace Content.Shared._RMC14.Sensor;

public sealed partial class SensorTowerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SkillsSystem _skills = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private GunIFFSystem _gunIFF = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapIncludeXenosEvent>(OnTacticalMapIncludeXenos);

        SubscribeLocalEvent<SensorTowerComponent, MapInitEvent>(OnSensorTowerMapInit);
        SubscribeLocalEvent<SensorTowerComponent, InteractUsingEvent>(OnSensorTowerInteractUsing);
        SubscribeLocalEvent<SensorTowerComponent, InteractHandEvent>(OnSensorTowerInteractHand);
        SubscribeLocalEvent<SensorTowerComponent, ExaminedEvent>(OnSensorTowerExamined);
        SubscribeLocalEvent<SensorTowerComponent, SensorTowerRepairDoAfterEvent>(OnSensorTowerRepairDoAfter);
        SubscribeLocalEvent<SensorTowerComponent, SensorTowerDestroyDoAfterEvent>(OnSensorTowerDestroyDoAfter);
        // Allow claiming/wiping faction like communications towers
        SubscribeLocalEvent<SensorTowerComponent, DialogChosenEvent>(OnSensorTowerDialogChosen);
        SubscribeLocalEvent<SensorTowerComponent, SensorTowerWipeDoAfterEvent>(OnSensorTowerDialogWipeDoAfter);
        SubscribeLocalEvent<SensorTowerComponent, SensorTowerAddDoAfterEvent>(OnSensorTowerDialogAddDoAfter);
    }

    private void OnSensorTowerDialogChosen(Entity<SensorTowerComponent> ent, ref DialogChosenEvent args)
    {
        DoAfterEvent ev;
        var delay = TimeSpan.Zero;
        if (args.Index == 0)
            ev = new SensorTowerWipeDoAfterEvent();
        else
        {
            ev = new SensorTowerAddDoAfterEvent();
            delay = TimeSpan.FromSeconds(1);
        }

        var doAfter = new DoAfterArgs(EntityManager, args.Actor, delay, ev, ent)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnSensorTowerDialogWipeDoAfter(Entity<SensorTowerComponent> ent, ref SensorTowerWipeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (ent.Comp.State == SensorTowerState.Weld)
            return;

        args.Handled = true;
        ent.Comp.Faction = string.Empty;
        Dirty(ent);
        var msg = Loc.GetString("rmc-sensor-tower-wipe", ("name", Name(ent))); // RuMC edit
        _popup.PopupClient(msg, ent, args.User, PopupType.Medium);
        // Notify tactical map system that sensor ownership changed so the canvas updates immediately
        RaiseLocalEvent(ent.Owner, new SensorTowerStateChangedEvent(ent.Owner));
    }

    private void OnSensorTowerDialogAddDoAfter(Entity<SensorTowerComponent> ent, ref SensorTowerAddDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (ent.Comp.State == SensorTowerState.Weld)
            return;

        if (_gunIFF.TryGetFaction(args.User, out var faction))
        {
            // Save faction as string for compatibility with tactical map checks
            ent.Comp.Faction = faction.ToString();
            Dirty(ent);
            args.Handled = true;
            var msg = Loc.GetString("rmc-sensor-tower-configure", ("name", Name(ent)), ("faction", faction)); // RuMC edit
            _popup.PopupClient(msg, ent, args.User, PopupType.Medium);
            // Notify tactical map system that sensor ownership changed so the canvas updates immediately
            RaiseLocalEvent(ent.Owner, new SensorTowerStateChangedEvent(ent.Owner));
        }
    }

    private void OnTacticalMapIncludeXenos(ref TacticalMapIncludeXenosEvent ev)
    {
        var towers = EntityQueryEnumerator<SensorTowerComponent>();
        while (towers.MoveNext(out var tower))
        {
            if (tower.State == SensorTowerState.On)
            {
                ev.Include = true;
                return;
            }
        }
    }

    private void OnSensorTowerMapInit(Entity<SensorTowerComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnSensorTowerInteractUsing(Entity<SensorTowerComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            var msg = Loc.GetString("rmc-skills-no-training", ("target", ent));
            _popup.PopupClient(msg, ent, user, PopupType.SmallCaution);
            return;
        }

        var used = args.Used;

        // If using a multitool, claim the tower for your faction immediately (wipe/overwrite previous owner).
        if (HasComp<MultitoolComponent>(used))
        {
            // Do not allow claiming if the tower is destroyed/welded
            if (ent.Comp.State == SensorTowerState.Weld)
            {
                var msg = Loc.GetString("rmc-sensor-tower-too-damaged"); // RuMC edit
                _popup.PopupClient(msg, ent, args.User, PopupType.SmallCaution);
                args.Handled = true;
                return;
            }

            // Try to get the user's faction via IFF system. If present, assign it; otherwise wipe.
            if (_gunIFF.TryGetFaction(args.User, out var faction))
            {
                ent.Comp.Faction = faction.ToString();
                Dirty(ent);
                var msg = Loc.GetString("rmc-sensor-tower-configure", ("name", Name(ent)), ("faction", faction)); // RuMC edit
                _popup.PopupClient(msg, ent, args.User, PopupType.Medium);
                _adminLog.Add(LogType.RMCCommunicationsTower, $"{ToPrettyString(args.User)} set {ToPrettyString(ent)} to faction {faction}.");
            }
            else
            {
                // No faction found on user - wipe the tower
                ent.Comp.Faction = string.Empty;
                Dirty(ent);
                var msg = Loc.GetString("rmc-sensor-tower-wipe", ("name", Name(ent))); // RuMC edit
                _popup.PopupClient(msg, ent, args.User, PopupType.Medium);
                _adminLog.Add(LogType.RMCCommunicationsTower, $"{ToPrettyString(args.User)} wiped faction settings from {ToPrettyString(ent)}.");
            }

            // Notify tactical map system that sensor ownership changed so the canvas updates immediately
            RaiseLocalEvent(ent.Owner, new SensorTowerStateChangedEvent(ent.Owner));

            args.Handled = true;
            return;
        }


        if (TryComp<RMCDeviceBreakerComponent>(args.Used, out var breaker) && ent.Comp.State != SensorTowerState.Weld)
        {
            var doafter = new DoAfterArgs(EntityManager, args.User, breaker.DoAfterTime, new RMCDeviceBreakerDoAfterEvent(), args.Used, args.Target, args.Used)
            {
                BreakOnMove = true,
                RequireCanInteract = true,
                BreakOnHandChange = true,
                DuplicateCondition = DuplicateConditions.SameTool
            };

            args.Handled = true;
            _doAfter.TryStartDoAfter(doafter);
            return;
        }

        var correctQuality = ent.Comp.State switch
        {
            SensorTowerState.Weld => ent.Comp.WeldingQuality,
            SensorTowerState.Wire => ent.Comp.CuttingQuality,
            SensorTowerState.Wrench => ent.Comp.WrenchQuality,
            _ => throw new ArgumentOutOfRangeException(),
        };

        args.Handled = true;

        if (_tool.HasQuality(used, correctQuality))
            TryRepair(ent, user, used, ent.Comp.State);
    }

    private void OnSensorTowerInteractHand(Entity<SensorTowerComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (HasComp<XenoComponent>(user))
        {
            if (!HasComp<HandsComponent>(user))
                return;

            Destroy(ent, user);
            return;
        }

        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
        {
            _popup.PopupClient(Loc.GetString("rmc-sensor-tower-no-skill"), ent, user, PopupType.SmallCaution); // RuMC edit
            return;
        }

        ref var state = ref ent.Comp.State;
        var popup = state switch
        {
            // RuMC edit start
            SensorTowerState.Weld => Loc.GetString("rmc-sensor-tower-interact-weld"),
            SensorTowerState.Wire => Loc.GetString("rmc-sensor-tower-interact-wire"),
            SensorTowerState.Wrench => Loc.GetString("rmc-sensor-tower-interact-wrench"),
            SensorTowerState.Off => Loc.GetString("rmc-sensor-tower-interact-off", ("name", Name(ent))),
            SensorTowerState.On => Loc.GetString("rmc-sensor-tower-interact-on", ("name", Name(ent))),
            // RuMC edit end
            _ => throw new ArgumentOutOfRangeException(),
        };
        _popup.PopupClient(popup, ent, user, PopupType.Medium);

        if (state < SensorTowerState.Off)
            return;

        if (state == SensorTowerState.Off)
            state = SensorTowerState.On;
        else if (state == SensorTowerState.On)
            state = SensorTowerState.Off;

        _adminLog.Add(LogType.RMCCommunicationsTower, $"{ToPrettyString(args.User)} turned {ToPrettyString(ent)} {state}.");
        ChangeState(ent, state);
    }

    private void OnSensorTowerExamined(Entity<SensorTowerComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(SensorTowerComponent)))
        {
            var text = ent.Comp.State switch
            {
                // RuMC edit start
                SensorTowerState.Weld => Loc.GetString("rmc-sensor-tower-examine-weld"),
                SensorTowerState.Wire => Loc.GetString("rmc-sensor-tower-examine-wire"),
                SensorTowerState.Wrench => Loc.GetString("rmc-sensor-tower-examine-wrench"),
                SensorTowerState.Off => Loc.GetString("rmc-sensor-tower-examine-offline"),
                SensorTowerState.On => Loc.GetString("rmc-sensor-tower-examine-online"),
                // RuMC edit end
                _ => throw new ArgumentOutOfRangeException(),
            };
            args.PushMarkup(text); // RuMC edit

            if (ent.Comp.State < SensorTowerState.Off)
            {
                // RuMC edit start
                var repairMsg = ent.Comp.State switch
                {
                    SensorTowerState.Wrench => Loc.GetString("rmc-sensor-tower-repair-wrench"),
                    SensorTowerState.Wire => Loc.GetString("rmc-sensor-tower-repair-wirecutters"),
                    SensorTowerState.Weld => Loc.GetString("rmc-sensor-tower-repair-welder"),
                    // RuMC edit end
                    _ => throw new ArgumentOutOfRangeException(),
                };
                args.PushMarkup(repairMsg);
            }
        }
    }

    private void OnSensorTowerRepairDoAfter(Entity<SensorTowerComponent> ent, ref SensorTowerRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.State != args.State)
            return;

        ent.Comp.State = args.State switch
        {
            SensorTowerState.Weld => SensorTowerState.Wire,
            SensorTowerState.Wire => SensorTowerState.Wrench,
            SensorTowerState.Wrench => SensorTowerState.Off,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnSensorTowerDestroyDoAfter(Entity<SensorTowerComponent> ent, ref SensorTowerDestroyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        FullyDestroy(ent);
    }

    public void FullyDestroy(Entity<SensorTowerComponent> ent)
    {
        ent.Comp.State = SensorTowerState.Weld;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    public void SensorTowerIncrementalDestroy(Entity<SensorTowerComponent> ent)
    {
        ent.Comp.State = ent.Comp.State switch
        {
            SensorTowerState.On => SensorTowerState.Wrench,
            SensorTowerState.Off => SensorTowerState.Wrench,
            SensorTowerState.Wrench => SensorTowerState.Wire,
            SensorTowerState.Wire => SensorTowerState.Weld,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void TryRepair(Entity<SensorTowerComponent> tower, EntityUid user, EntityUid used, SensorTowerState state)
    {
        var quality = state switch
        {
            SensorTowerState.Weld => tower.Comp.WeldingQuality,
            SensorTowerState.Wire => tower.Comp.CuttingQuality,
            SensorTowerState.Wrench => tower.Comp.WrenchQuality,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

        var delay = state switch
        {
            SensorTowerState.Weld => tower.Comp.WeldingDelay,
            SensorTowerState.Wire => tower.Comp.CuttingDelay,
            SensorTowerState.Wrench => tower.Comp.WrenchDelay,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        _tool.UseTool(
            used,
            user,
            tower,
            (float)delay.TotalSeconds,
            quality,
            new SensorTowerRepairDoAfterEvent(state),
            tower.Comp.WeldingCost
        );
    }

    private void UpdateAppearance(Entity<SensorTowerComponent> tower)
    {
        _appearance.SetData(tower, SensorTowerLayers.Layer, tower.Comp.State);
    }

    private void Destroy(Entity<SensorTowerComponent> tower, EntityUid user)
    {
        if (tower.Comp.State == SensorTowerState.Weld)
        {
            _popup.PopupClient(Loc.GetString("rmc-sensor-tower-destroy-clueless"), user, user, PopupType.SmallCaution); // RuMC edit
            return;
        }

        var ev = new SensorTowerDestroyDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, tower.Comp.DestroyDelay, ev, tower, tower, user)
        {
            ForceVisible = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient(Loc.GetString("rmc-sensor-tower-destroy-start", ("name", Name(tower))), tower, user, PopupType.Medium); // RuMC edit
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<SensorTowerComponent>();
        while (query.MoveNext(out var uid, out var tower))
        {
            if (tower.State != SensorTowerState.On)
                continue;

            if (time < tower.NextBreakAt)
                continue;

            if (!_random.Prob(tower.BreakChance))
            {
                tower.NextBreakAt = time + tower.BreakEvery;
                Dirty(uid, tower);
                continue;
            }

            if (_random.Prob(0.75f))
            {
                _popup.PopupEntity(Loc.GetString("rmc-sensor-tower-break-wrench", ("name", Name(uid))), uid, uid, PopupType.LargeCaution); // RuMC edit
                tower.State = SensorTowerState.Wrench;
                ChangeState((uid, tower), SensorTowerState.Wrench);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("rmc-sensor-tower-break-wire", ("name", Name(uid))), uid, uid, PopupType.LargeCaution); // RuMC edit
                ChangeState((uid, tower), SensorTowerState.Wire);
            }

            UpdateAppearance((uid, tower));
        }
    }

    private void ChangeState(Entity<SensorTowerComponent> tower, SensorTowerState newState)
    {
        tower.Comp.State = newState;
        Dirty(tower);

        var ev = new SensorTowerStateChangedEvent(tower.Owner);
        // Raise local event on the tower entity; event payload carries the EntityUid only (not Entity<T>)
        RaiseLocalEvent(tower.Owner, ev);
        UpdateAppearance(tower);
    }


    public bool HasOnlineSensorForFaction(string? faction)
    {
        if (string.IsNullOrWhiteSpace(faction))
            return false;

        var comps = EntityQueryEnumerator<SensorTowerComponent>();
        while (comps.MoveNext(out _, out var comp))
        {
            if (comp.State != SensorTowerState.On)
                continue;

            if (string.Equals(comp.Faction, faction, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

// Event types for DoAfter claiming/wiping sensor towers (use SimpleDoAfterEvent like communications)
[Serializable, NetSerializable]
public sealed partial class SensorTowerWipeDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SensorTowerAddDoAfterEvent : SimpleDoAfterEvent;

// Local event for sensor tower state changes. Not net-serializable to avoid serializing Entity<T>.
public sealed partial class SensorTowerStateChangedEvent : EntityEventArgs
{
    public EntityUid TowerUid;
    public SensorTowerStateChangedEvent(EntityUid towerUid) { TowerUid = towerUid; }
}

using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.CMU14.Fighter;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;

namespace Content.Shared._RuMC14.ManpadLowAltitude;

/// <summary>Raise, lower and trigger of the low altitude mode.</summary>
public sealed partial class RuMCManpadLowAltitudeSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private FighterManpadSystem _fighterManpad = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FighterManpadComponent, FighterManpadAimStartedEvent>(OnFighterAimStarted);
        SubscribeLocalEvent<RuMCManpadLowAltitudeComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<RuMCManpadLowAltitudeComponent, RuMCManpadLowAltitudeAimActionEvent>(OnAim);
        SubscribeLocalEvent<RuMCManpadLowAltitudeComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<RuMCManpadLowAltitudeComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<RuMCManpadLowAltitudeComponent, GotUnequippedHandEvent>(OnUnequipped);
        SubscribeLocalEvent<RuMCManpadLowAltitudeComponent, HandDeselectedEvent>(OnDeselected);
        SubscribeLocalEvent<RuMCManpadLowAltitudeComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGetActions(Entity<RuMCManpadLowAltitudeComponent> ent, ref GetItemActionsEvent args)
    {
        if (!args.InHands)
            return;

        args.AddAction(ref ent.Comp.AimAction, ent.Comp.AimActionId);
        Dirty(ent);
    }

    private void OnAim(Entity<RuMCManpadLowAltitudeComponent> ent, ref RuMCManpadLowAltitudeAimActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.AimingUser == args.Performer)
        {
            StopAiming(ent);
            _popup.PopupEntity(Loc.GetString("rumc-manpad-low-altitude-lowered"), args.Performer, args.Performer);
            return;
        }

        if (!CanAim(ent, args.Performer))
        {
            _popup.PopupEntity(Loc.GetString("rumc-manpad-low-altitude-requires-wield"), args.Performer, args.Performer);
            return;
        }

        if (!HasAmmo(ent))
        {
            _popup.PopupEntity(Loc.GetString("rumc-manpad-low-altitude-empty"), args.Performer, args.Performer);
            return;
        }

        // One mode at a time.
        if (TryComp(ent, out FighterManpadComponent? fighter))
            _fighterManpad.StopAiming((ent, fighter));

        ent.Comp.AimingUser = args.Performer;
        _actions.SetToggled(ent.Comp.AimAction, true);
        Dirty(ent);

        var started = new RuMCManpadLowAltitudeAimStartedEvent(args.Performer);
        RaiseLocalEvent(ent, ref started);
        _popup.PopupEntity(Loc.GetString("rumc-manpad-low-altitude-raised"), args.Performer, args.Performer);
    }

    private void OnFighterAimStarted(Entity<FighterManpadComponent> ent, ref FighterManpadAimStartedEvent args)
    {
        if (TryComp(ent, out RuMCManpadLowAltitudeComponent? lowAltitude))
            StopAiming((ent, lowAltitude));
    }

    private void OnAttemptShoot(Entity<RuMCManpadLowAltitudeComponent> ent, ref AttemptShootEvent args)
    {
        args.Cancelled = true;

        if (ent.Comp.AimingUser != args.User)
            return;

        if (ent.Comp.LockState != RuMCManpadLowAltitudeLockState.Locked)
        {
            args.Message = Loc.GetString("rumc-manpad-low-altitude-no-lock");
            return;
        }

        var launch = new RuMCManpadLowAltitudeLaunchRequestEvent(args.User);
        RaiseLocalEvent(ent, ref launch);
    }

    public bool CanAim(Entity<RuMCManpadLowAltitudeComponent> ent, EntityUid user)
    {
        return !TerminatingOrDeleted(user)
               && _hands.GetActiveItem(user) == ent.Owner
               && TryComp(ent, out WieldableComponent? wieldable)
               && wieldable.Wielded
               && !_containers.IsEntityOrParentInContainer(user)
               && _blocker.CanInteract(user, ent)
               && _blocker.CanUseHeldEntity(user, ent);
    }

    public bool IsAiming(Entity<RuMCManpadLowAltitudeComponent> ent)
    {
        return ent.Comp.AimingUser is { } user && CanAim(ent, user) && HasAmmo(ent);
    }

    public bool HasAmmo(EntityUid launcher)
    {
        var ammo = new GetAmmoCountEvent();
        RaiseLocalEvent(launcher, ref ammo);
        return ammo.Count > 0;
    }

    public void StopAiming(Entity<RuMCManpadLowAltitudeComponent> ent)
    {
        if (ent.Comp.AimingUser == null)
            return;

        ent.Comp.AimingUser = null;
        ent.Comp.LockState = RuMCManpadLowAltitudeLockState.None;
        ent.Comp.TargetKind = RuMCManpadLowAltitudeTargetKind.None;
        _actions.SetToggled(ent.Comp.AimAction, false);
        Dirty(ent);

        var stopped = new RuMCManpadLowAltitudeAimStoppedEvent();
        RaiseLocalEvent(ent, ref stopped);
    }

    public void SetLock(
        Entity<RuMCManpadLowAltitudeComponent> ent,
        RuMCManpadLowAltitudeLockState state,
        RuMCManpadLowAltitudeTargetKind kind)
    {
        if (ent.Comp.LockState == state && ent.Comp.TargetKind == kind)
            return;

        ent.Comp.LockState = state;
        ent.Comp.TargetKind = kind;
        Dirty(ent);
    }

    /// <summary>Localized name of the aircraft kind shown to the operator.</summary>
    public string GetTargetName(RuMCManpadLowAltitudeTargetKind kind)
    {
        return Loc.GetString(kind switch
        {
            RuMCManpadLowAltitudeTargetKind.Gunship => "rumc-manpad-low-altitude-target-gunship",
            RuMCManpadLowAltitudeTargetKind.Blackfoot => "rumc-manpad-low-altitude-target-blackfoot",
            _ => "rumc-manpad-low-altitude-target-none",
        });
    }

    private void OnUnwielded(Entity<RuMCManpadLowAltitudeComponent> ent, ref ItemUnwieldedEvent args)
        => StopAiming(ent);

    private void OnUnequipped(Entity<RuMCManpadLowAltitudeComponent> ent, ref GotUnequippedHandEvent args)
        => StopAiming(ent);

    private void OnDeselected(Entity<RuMCManpadLowAltitudeComponent> ent, ref HandDeselectedEvent args)
        => StopAiming(ent);

    private void OnExamined(Entity<RuMCManpadLowAltitudeComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.AimingUser == null)
            return;

        var key = ent.Comp.LockState switch
        {
            RuMCManpadLowAltitudeLockState.Locking => "rumc-manpad-low-altitude-examine-locking",
            RuMCManpadLowAltitudeLockState.Locked => "rumc-manpad-low-altitude-examine-locked",
            _ => "rumc-manpad-low-altitude-examine-aimed",
        };
        args.PushMarkup(Loc.GetString(key, ("target", GetTargetName(ent.Comp.TargetKind))));
    }
}

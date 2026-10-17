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

namespace Content.Shared._RuMC14.ManpadGunship;

/// <summary>Raise, lower and trigger of the gunship mode.</summary>
public sealed partial class RuMCManpadGunshipSystem : EntitySystem
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
        SubscribeLocalEvent<RuMCManpadGunshipComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<RuMCManpadGunshipComponent, RuMCManpadGunshipAimActionEvent>(OnAim);
        SubscribeLocalEvent<RuMCManpadGunshipComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<RuMCManpadGunshipComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<RuMCManpadGunshipComponent, GotUnequippedHandEvent>(OnUnequipped);
        SubscribeLocalEvent<RuMCManpadGunshipComponent, HandDeselectedEvent>(OnDeselected);
        SubscribeLocalEvent<RuMCManpadGunshipComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGetActions(Entity<RuMCManpadGunshipComponent> ent, ref GetItemActionsEvent args)
    {
        if (!args.InHands)
            return;

        args.AddAction(ref ent.Comp.AimAction, ent.Comp.AimActionId);
        Dirty(ent);
    }

    private void OnAim(Entity<RuMCManpadGunshipComponent> ent, ref RuMCManpadGunshipAimActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.AimingUser == args.Performer)
        {
            StopAiming(ent);
            _popup.PopupEntity(Loc.GetString("rumc-manpad-gunship-lowered"), args.Performer, args.Performer);
            return;
        }

        if (!CanAim(ent, args.Performer))
        {
            _popup.PopupEntity(Loc.GetString("rumc-manpad-gunship-requires-wield"), args.Performer, args.Performer);
            return;
        }

        if (!HasAmmo(ent))
        {
            _popup.PopupEntity(Loc.GetString("rumc-manpad-gunship-empty"), args.Performer, args.Performer);
            return;
        }

        // One mode at a time.
        if (TryComp(ent, out FighterManpadComponent? fighter))
            _fighterManpad.StopAiming((ent, fighter));

        ent.Comp.AimingUser = args.Performer;
        _actions.SetToggled(ent.Comp.AimAction, true);
        Dirty(ent);

        var started = new RuMCManpadGunshipAimStartedEvent(args.Performer);
        RaiseLocalEvent(ent, ref started);
        _popup.PopupEntity(Loc.GetString("rumc-manpad-gunship-raised"), args.Performer, args.Performer);
    }

    private void OnFighterAimStarted(Entity<FighterManpadComponent> ent, ref FighterManpadAimStartedEvent args)
    {
        if (TryComp(ent, out RuMCManpadGunshipComponent? gunship))
            StopAiming((ent, gunship));
    }

    private void OnAttemptShoot(Entity<RuMCManpadGunshipComponent> ent, ref AttemptShootEvent args)
    {
        args.Cancelled = true;

        if (ent.Comp.AimingUser != args.User)
            return;

        if (ent.Comp.LockState != RuMCManpadGunshipLockState.Locked)
        {
            args.Message = Loc.GetString("rumc-manpad-gunship-no-lock");
            return;
        }

        var launch = new RuMCManpadGunshipLaunchRequestEvent(args.User);
        RaiseLocalEvent(ent, ref launch);
    }

    public bool CanAim(Entity<RuMCManpadGunshipComponent> ent, EntityUid user)
    {
        return !TerminatingOrDeleted(user)
               && _hands.GetActiveItem(user) == ent.Owner
               && TryComp(ent, out WieldableComponent? wieldable)
               && wieldable.Wielded
               && !_containers.IsEntityOrParentInContainer(user)
               && _blocker.CanInteract(user, ent)
               && _blocker.CanUseHeldEntity(user, ent);
    }

    public bool IsAiming(Entity<RuMCManpadGunshipComponent> ent)
    {
        return ent.Comp.AimingUser is { } user && CanAim(ent, user) && HasAmmo(ent);
    }

    public bool HasAmmo(EntityUid launcher)
    {
        var ammo = new GetAmmoCountEvent();
        RaiseLocalEvent(launcher, ref ammo);
        return ammo.Count > 0;
    }

    public void StopAiming(Entity<RuMCManpadGunshipComponent> ent)
    {
        if (ent.Comp.AimingUser == null)
            return;

        ent.Comp.AimingUser = null;
        ent.Comp.LockState = RuMCManpadGunshipLockState.None;
        _actions.SetToggled(ent.Comp.AimAction, false);
        Dirty(ent);

        var stopped = new RuMCManpadGunshipAimStoppedEvent();
        RaiseLocalEvent(ent, ref stopped);
    }

    public void SetLockState(Entity<RuMCManpadGunshipComponent> ent, RuMCManpadGunshipLockState state)
    {
        if (ent.Comp.LockState == state)
            return;

        ent.Comp.LockState = state;
        Dirty(ent);
    }

    private void OnUnwielded(Entity<RuMCManpadGunshipComponent> ent, ref ItemUnwieldedEvent args)
        => StopAiming(ent);

    private void OnUnequipped(Entity<RuMCManpadGunshipComponent> ent, ref GotUnequippedHandEvent args)
        => StopAiming(ent);

    private void OnDeselected(Entity<RuMCManpadGunshipComponent> ent, ref HandDeselectedEvent args)
        => StopAiming(ent);

    private void OnExamined(Entity<RuMCManpadGunshipComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.AimingUser == null)
            return;

        var key = ent.Comp.LockState switch
        {
            RuMCManpadGunshipLockState.Locking => "rumc-manpad-gunship-examine-locking",
            RuMCManpadGunshipLockState.Locked => "rumc-manpad-gunship-examine-locked",
            _ => "rumc-manpad-gunship-examine-aimed",
        };
        args.PushMarkup(Loc.GetString(key));
    }
}

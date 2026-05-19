using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Projectile;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RuMC14.Explosion;

public abstract partial class SharedRuMCButterflyMineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CollisionWakeSystem _collisionWake = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RuMCButterflyMineComponent, ButterflyMineDeployDoafterEvent>(OnDeploy);
        SubscribeLocalEvent<RuMCButterflyMineComponent, ButterflyMineDisarmDoafterEvent>(OnDisarm);
        SubscribeLocalEvent<RuMCButterflyMineComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RuMCButterflyMineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RuMCButterflyMineComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<RuMCButterflyMineComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<RuMCButterflyMineComponent, CombatModeShouldHandInteractEvent>(OnShouldInteract);
    }

    private void OnDeploy(Entity<RuMCButterflyMineComponent> ent, ref ButterflyMineDeployDoafterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanDeployPopup(ent, args.User, out var coordinates, out var rotation))
            return;

        var xform = Transform(ent);
        _transform.SetCoordinates(ent, xform, coordinates, rotation);
        _transform.AnchorEntity(ent, xform);
        _collisionWake.SetEnabled(ent, false);
        _physics.SetBodyType(ent, BodyType.Static);

        ent.Comp.Installer = args.User;
        ent.Comp.InstallerImmunityUntil = Timing.CurTime + ent.Comp.InstallerImmunityDuration;
        ent.Comp.Armed = true;
        Dirty(ent);

        UpdateAppearance(ent);
        _audio.PlayPredicted(ent.Comp.DeploySound, ent, args.User);
    }

    private void OnDisarm(Entity<RuMCButterflyMineComponent> ent, ref ButterflyMineDisarmDoafterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        _transform.Unanchor(ent);
        _collisionWake.SetEnabled(ent, true);
        ent.Comp.Armed = false;
        ent.Comp.Installer = null;
        _physics.SetBodyType(ent, BodyType.Dynamic);
        Dirty(ent);

        if (TryComp(args.User, out HandsComponent? hands))
            _hands.TryPickupAnyHand(args.User, ent, handsComp: hands);

        UpdateAppearance(ent);
    }

    private void OnUseInHand(Entity<RuMCButterflyMineComponent> ent, ref UseInHandEvent args)
    {
        if (!CanDeployPopup(ent, args.User, out _, out _))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.PlacementDelay,
            new ButterflyMineDeployDoafterEvent(),
            ent,
            ent,
            args.User)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnInteractUsing(Entity<RuMCButterflyMineComponent> ent, ref InteractUsingEvent args)
    {
        if (!ent.Comp.Armed)
            return;

        if (!_tool.HasQuality(args.Used, ent.Comp.DisarmTool))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DisarmDelay,
            new ButterflyMineDisarmDoafterEvent(),
            ent,
            ent,
            args.User)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnPreventCollide(Entity<RuMCButterflyMineComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.Armed
            && !HasComp<XenoProjectileComponent>(args.OtherEntity)
            && !HasComp<MobStateComponent>(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }

    private void OnBeforeDamageChanged(Entity<RuMCButterflyMineComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (HasComp<ProjectileComponent>(args.Source))
            args.Cancelled = true;

        if (!ent.Comp.Armed)
            args.Cancelled = true;
    }

    private void OnShouldInteract(Entity<RuMCButterflyMineComponent> ent, ref CombatModeShouldHandInteractEvent args)
    {
        if (HasComp<XenoComponent>(args.User))
            args.Cancelled = true;
    }

    private bool CanDeployPopup(Entity<RuMCButterflyMineComponent> ent,
        EntityUid user,
        out EntityCoordinates coordinates,
        out Angle rotation)
    {
        coordinates = _transform.GetMoverCoordinates(user, Transform(user)).SnapToGrid();
        rotation = Angle.Zero;

        if (_container.IsEntityInContainer(user))
        {
            var msg = Loc.GetString("rmc-explosive-deploy-container", ("explosive", ent));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        var query = _rmcMap.GetAnchoredEntitiesEnumerator(coordinates);
        while (query.MoveNext(out var anchoredUid))
        {
            if (!HasComp<RuMCButterflyMineComponent>(anchoredUid)
                && !HasComp<RMCLandmineComponent>(anchoredUid))
                continue;

            var msg = Loc.GetString("rmc-mine-deploy-fail-occupied");
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void UpdateAppearance(Entity<RuMCButterflyMineComponent> ent)
    {
        _appearance.SetData(ent, ToggleableVisuals.Enabled, ent.Comp.Armed);
    }
}

[Serializable, NetSerializable]
public sealed partial class ButterflyMineDeployDoafterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class ButterflyMineDisarmDoafterEvent : SimpleDoAfterEvent { }

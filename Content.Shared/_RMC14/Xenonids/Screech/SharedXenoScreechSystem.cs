using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Stunnable; // CMU14: restore RMC queen control
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Containers;
using Content.Shared.Coordinates;
using Content.Shared.Jittering;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands; // CMU14
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Screech;

public sealed partial class XenoScreechSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!; // CMU14
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private ExamineSystemShared _examineSystem = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedDeafnessSystem _deaf = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private XenoSystem _xeno = default!;
    [Dependency] private RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private RMCSlowSystem _slow = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();
    private readonly HashSet<Entity<MobStateComponent>> _closeMobs = new(); // CMU14
    private readonly HashSet<Entity<XenoParasiteComponent>> _parasites = new(); // CMU14

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoScreechComponent, XenoScreechActionEvent>(OnXenoScreechAction);
        SubscribeLocalEvent<ScreechScatterComponent, GunRefreshModifiersEvent>(OnScreechScatterRefresh);
        SubscribeLocalEvent<ScreechBlindComponent, ComponentStartup>(OnScreechBlindStartup);
        SubscribeLocalEvent<ScreechBlindComponent, ComponentShutdown>(OnScreechBlindShutdown);
        // CMU14: the penalty follows the affected holder when guns change hands.
        SubscribeLocalEvent<ScreechBlindComponent, DidEquipHandEvent>(OnScreechEquipHand);
        SubscribeLocalEvent<ScreechBlindComponent, DidUnequipHandEvent>(OnScreechUnequipHand);
    }

    // CMU14 Begin: RMC queen screech; retain the public honor-roar helpers below.
    private void OnXenoScreechAction(Entity<XenoScreechComponent> xeno, ref XenoScreechActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoScreechAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        if (!TryComp(xeno, out TransformComponent? xform))
            return;

        args.Handled = true;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        _closeMobs.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParalyzeRange, _closeMobs);

        foreach (var receiver in _closeMobs)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, receiver))
                continue;

            if (!Stun(xeno, receiver, xeno.Comp.ParalyzeTime, false))
                continue;

            _cameraShake.ShakeCamera(receiver, xeno.Comp.CloseScreenShakeShakes, xeno.Comp.CloseScreenShakeStrength);
            Deafen(xeno, receiver, xeno.Comp.CloseDeafTime);
        }

        _mobs.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.StunRange, _mobs);

        foreach (var receiver in _mobs)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, receiver))
                continue;

            if (_closeMobs.Contains(receiver))
                continue;

            if (!Stun(xeno, receiver, xeno.Comp.StunTime, true))
                continue;

            _cameraShake.ShakeCamera(receiver, xeno.Comp.FarScreenShakeShakes, xeno.Comp.FarScreenShakeStrength);
            Deafen(xeno, receiver, xeno.Comp.FarDeafTime);
        }

        _parasites.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParasiteStunRange, _parasites);

        foreach (var receiver in _parasites)
        {
            if (!Stun(xeno, receiver, xeno.Comp.ParasiteStunTime, true, false))
                continue;

            _cameraShake.ShakeCamera(receiver, xeno.Comp.CloseScreenShakeShakes, xeno.Comp.CloseScreenShakeStrength);
        }

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, xeno.Owner.ToCoordinates());
    }

    private bool Stun(EntityUid xeno, EntityUid receiver, TimeSpan time, bool stun, bool occlusionCheck = true)
    {
        if (_mobState.IsDead(receiver))
            return false;

        if (occlusionCheck && !_examineSystem.InRangeUnOccluded(xeno, receiver))
            return false;

        if (stun)
            return _stun.TryStun(receiver, time, false);

        return _stun.TryParalyze(receiver, time, false);
    }

    // CMU14 End

    // CMU Related Change
    public bool ApplyScreechEffects(EntityUid xeno, EntityUid receiver, TimeSpan slowTime, TimeSpan blindTime)
    {
        if (_mobState.IsDead(receiver))
            return false;

        if (!_examineSystem.InRangeUnOccluded(xeno, receiver))
            return false;

        _slow.TrySuperSlowdown(receiver, slowTime, ignoreDurationModifier: true);
        _jitter.DoJitter(receiver, slowTime, true, 3f, 8f);
        // CMU14: zero disables blindness/scatter, including the first simulation tick.
        if (blindTime > TimeSpan.Zero)
        {
            var blind = EnsureComp<ScreechBlindComponent>(receiver);
            blind.EndsAt = _timing.CurTime + blindTime;
            Dirty(receiver, blind);
        }
        return true;
    }

    // CMU Related Change
    public void Deafen(EntityUid xeno, EntityUid receiver, TimeSpan time)
    {
        if (_mobState.IsDead(receiver))
            return;

        if (!_examineSystem.InRangeUnOccluded(xeno, receiver))
            return;

        _deaf.TryDeafen(receiver, time, false);
    }

    private void OnScreechScatterRefresh(Entity<ScreechScatterComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.MinAngle += ent.Comp.AngleIncrease;
        args.MaxAngle += ent.Comp.AngleIncrease;
    }

    private void OnScreechBlindStartup(Entity<ScreechBlindComponent> ent, ref ComponentStartup args)
    {
        AddScatterToTrackedGuns(ent.Owner, ent.Comp);
    }

    private void OnScreechBlindShutdown(Entity<ScreechBlindComponent> ent, ref ComponentShutdown args)
    {
        RemoveScatterFromTrackedGuns(ent.Comp);
    }

    // CMU14 Begin: update ownership immediately so one holder cannot clear another's penalty later.
    private void OnScreechEquipHand(Entity<ScreechBlindComponent> ent, ref DidEquipHandEvent args)
    {
        AddScatterToTrackedGuns(ent.Owner, ent.Comp);
    }

    private void OnScreechUnequipHand(Entity<ScreechBlindComponent> ent, ref DidUnequipHandEvent args)
    {
        if (ent.Comp.ModifiedGuns.Remove(args.Unequipped) && RemComp<ScreechScatterComponent>(args.Unequipped))
            _gun.RefreshModifiers(args.Unequipped);
    }
    // CMU14 End

    private void AddScatterToTrackedGuns(EntityUid user, ScreechBlindComponent blind)
    {
        if (!TryComp<HandsComponent>(user, out var hands))
            return;

        foreach (var name in hands.Hands.Keys)
        {
            if (!_container.TryGetContainer(user, name, out var container))
                continue;

            foreach (var held in container.ContainedEntities)
            {
                if (!HasComp<GunComponent>(held))
                    continue;

                if (!HasComp<ScreechScatterComponent>(held))
                {
                    EnsureComp<ScreechScatterComponent>(held);
                    blind.ModifiedGuns.Add(held);
                }

                _gun.RefreshModifiers(held);
            }
        }
    }

    private void RemoveScatterFromTrackedGuns(
        ScreechBlindComponent blind)
    {
        foreach (var gun in blind.ModifiedGuns)
        {
            if (TerminatingOrDeleted(gun))
                continue;

            if (RemComp<ScreechScatterComponent>(gun))
                _gun.RefreshModifiers(gun);
        }

        blind.ModifiedGuns.Clear();
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<ScreechBlindComponent>();

        while (query.MoveNext(out var uid, out var blind))
        {
            if (blind.EndsAt != null && time < blind.EndsAt)
                continue;

            RemCompDeferred<ScreechBlindComponent>(uid);
        }
    }
}

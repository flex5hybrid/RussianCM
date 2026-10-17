using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.CMU14.Dropship.Integrity;
using Content.Shared._RuMC14.ManpadGunship;
using Content.Shared.CMU14.Dropship.Integrity;
using Content.Shared.CMU14.Dropship.TacticalLand;
using Content.Shared.CMU14.Fighter;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.ManpadGunship;

/// <summary>Launches the missile and resolves the hit.</summary>
public sealed partial class RuMCManpadGunshipMissileSystem : EntitySystem
{
    private static readonly EntProtoId LaunchVisual = "CMUFighterManpadLaunch";

    private const float MinFlightDistance = 2f;

    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private FighterAudioSystem _fighterAudio = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private DropshipIntegritySystem _integrity = default!;
    [Dependency] private RuMCManpadGunshipLockSystem _lock = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private RuMCManpadGunshipTargetingSystem _targeting = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private EntityQuery<DropshipTacticalHoverComponent> _hoverQuery;

    public override void Initialize()
    {
        _hoverQuery = GetEntityQuery<DropshipTacticalHoverComponent>();
        SubscribeLocalEvent<RuMCManpadGunshipComponent, RuMCManpadGunshipLaunchRequestEvent>(OnLaunchRequested);
    }

    private void OnLaunchRequested(
        Entity<RuMCManpadGunshipComponent> launcher,
        ref RuMCManpadGunshipLaunchRequestEvent args)
    {
        var xform = Transform(launcher);
        if (xform.MapUid is not { } map
            || !TryComp(launcher, out RuMCManpadGunshipAimingComponent? aiming)
            || aiming.Target is not { } target
            || !_targeting.CanTrack(launcher, target)
            || !TryConsumeMissile(launcher, args.User))
        {
            return;
        }

        var origin = _transform.GetWorldPosition(xform);
        var launchSite = new EntityCoordinates(map, origin + GetMuzzleOffset(args.User));
        var flight = SpawnLaunchVisual(launcher.Comp, args.User, target, launchSite, origin);
        AddMissile(flight, launcher.Comp, args.User, target);

        _fighterAudio.PlayGround(launcher.Comp.LaunchSound, launchSite, launcher.Comp.SoundRange, 0);
        _audio.PlayPvs(launcher.Comp.IncomingSound, target, AudioParams.Default.WithVolume(-6));

        _adminLogger.Add(
            LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(args.User):user} launched a missile from {ToPrettyString(launcher):launcher} " +
            $"at gunship {ToPrettyString(target):target}");

        _lock.ResetLock(launcher, aiming);
    }

    private EntityUid SpawnLaunchVisual(
        RuMCManpadGunshipComponent launcher,
        EntityUid user,
        EntityUid target,
        EntityCoordinates launchSite,
        Vector2 origin)
    {
        var now = _timing.CurTime;
        var hullCenter = _targeting.GetHullCenter(target);
        var toTarget = hullCenter - origin;
        var flightSeconds = (float) launcher.MissileFlightTime.TotalSeconds;

        var flight = Spawn(LaunchVisual, launchSite);
        var visual = Comp<FighterManpadVisualComponent>(flight);
        visual.Launched = true;
        visual.Direction = toTarget.LengthSquared() > 0.001f ? Vector2.Normalize(toTarget) : Vector2.UnitY;
        visual.TubeDirection = _transform.GetWorldRotation(user).GetCardinalDir().ToVec();
        visual.StartedAt = now;
        visual.FlightSeconds = flightSeconds;
        if (toTarget.LengthSquared() >= MinFlightDistance * MinFlightDistance)
        {
            visual.Destination = hullCenter;
            visual.Target = target;
        }

        var lifetime = MathF.Max(FighterManpadVisualComponent.Lifetime, flightSeconds + 1f);
        visual.ExpiresAt = now + TimeSpan.FromSeconds(lifetime);
        Comp<TimedDespawnComponent>(flight).Lifetime = lifetime;
        Dirty(flight, visual);
        return flight;
    }

    private void AddMissile(
        EntityUid flight,
        RuMCManpadGunshipComponent launcher,
        EntityUid user,
        EntityUid target)
    {
        var missile = AddComp<RuMCManpadGunshipMissileComponent>(flight);
        missile.Shooter = user;
        missile.Target = target;
        missile.ImpactAt = _timing.CurTime + launcher.MissileFlightTime;
        missile.Damage = launcher.Damage;
        missile.HitEffect = launcher.HitEffect;
        missile.HitEffectCount = launcher.HitEffectCount;
        missile.HitEffectSpread = launcher.HitEffectSpread;
        missile.ImpactSound = launcher.ImpactSound;
        missile.SoundRange = launcher.SoundRange;
    }

    private bool TryConsumeMissile(Entity<RuMCManpadGunshipComponent> launcher, EntityUid user)
    {
        var take = new TakeAmmoEvent(1, [], Transform(launcher).Coordinates, user);
        RaiseLocalEvent(launcher, take);
        if (take.Ammo.Count == 0)
            return false;

        foreach (var (cartridge, _) in take.Ammo)
        {
            if (cartridge is { } round)
                Del(round);
        }

        _gun.UpdateAmmoCount(launcher);
        return true;
    }

    private Vector2 GetMuzzleOffset(EntityUid user)
    {
        var leftHand = _hands.TryGetHand(user, _hands.GetActiveHand(user), out var hand)
                       && hand.Value.Location == HandLocation.Left;
        return _transform.GetWorldRotation(user).GetCardinalDir() switch
        {
            Direction.South => new Vector2(leftHand ? .16f : -.2f, .35f),
            Direction.North => new Vector2(leftHand ? -.2f : .16f, .35f),
            Direction.East => new Vector2(.48f, .16f),
            _ => new Vector2(-.48f, .16f),
        };
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RuMCManpadGunshipMissileComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var missile, out var xform))
        {
            if (now < missile.ImpactAt)
                continue;

            Impact(missile, xform.MapUid);

            RemCompDeferred<RuMCManpadGunshipMissileComponent>(uid);
        }
    }

    private void Impact(RuMCManpadGunshipMissileComponent missile, EntityUid? shooterMap)
    {
        if (!TryComp(missile.Target, out DropshipIntegrityComponent? integrity)
            || !_hoverQuery.HasComp(missile.Target)
            || integrity.Crashing
            || integrity.Wrecked)
        {
            return;
        }

        var shipXform = Transform(missile.Target);
        if (shipXform.MapUid is not { } shipMap)
            return;

        _integrity.DamageIntegrity((missile.Target, integrity), missile.Damage);
        _adminLogger.Add(
            LogType.Damaged,
            LogImpact.High,
            $"{ToPrettyString(missile.Shooter):user}'s missile hit gunship {ToPrettyString(missile.Target):target} " +
            $"for {missile.Damage} hull damage");

        var position = _targeting.GetHullCenter(missile.Target);
        for (var i = 0; i < missile.HitEffectCount; i++)
        {
            var offset = new Vector2(
                _random.NextFloat(-missile.HitEffectSpread, missile.HitEffectSpread),
                _random.NextFloat(-missile.HitEffectSpread, missile.HitEffectSpread));
            Spawn(missile.HitEffect, new EntityCoordinates(shipMap, position + offset));
        }

        if (missile.ImpactSound is { } sound)
        {
            _fighterAudio.PlayGround(sound, new EntityCoordinates(shipMap, position), missile.SoundRange, 0);
            if (shooterMap is { } groundMap)
                _fighterAudio.PlayGround(sound, new EntityCoordinates(groundMap, position), missile.SoundRange, 0);
        }

        _audio.PlayPvs(integrity.ImpactSound, missile.Target);
    }
}

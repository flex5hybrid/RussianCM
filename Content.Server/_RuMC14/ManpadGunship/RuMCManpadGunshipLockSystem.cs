using Content.Shared._RuMC14.ManpadGunship;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.ManpadGunship;

/// <summary>Acquires a gunship for raised MANPADs, beeps while locking, drops the lock on loss of sight.</summary>
public sealed partial class RuMCManpadGunshipLockSystem : EntitySystem
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMilliseconds(100);

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private RuMCManpadGunshipSystem _manpadGunship = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private RuMCManpadGunshipTargetingSystem _targeting = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly List<Entity<RuMCManpadGunshipComponent>> _stopQueue = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RuMCManpadGunshipComponent, RuMCManpadGunshipAimStartedEvent>(OnAimStarted);
        SubscribeLocalEvent<RuMCManpadGunshipComponent, RuMCManpadGunshipAimStoppedEvent>(OnAimStopped);
    }

    private void OnAimStarted(Entity<RuMCManpadGunshipComponent> ent, ref RuMCManpadGunshipAimStartedEvent args)
    {
        EnsureComp<RuMCManpadGunshipAimingComponent>(ent);
    }

    private void OnAimStopped(Entity<RuMCManpadGunshipComponent> ent, ref RuMCManpadGunshipAimStoppedEvent args)
    {
        RemComp<RuMCManpadGunshipAimingComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RuMCManpadGunshipAimingComponent, RuMCManpadGunshipComponent>();
        while (query.MoveNext(out var uid, out var aiming, out var manpad))
        {
            var launcher = new Entity<RuMCManpadGunshipComponent>(uid, manpad);
            if (manpad.LockState != RuMCManpadGunshipLockState.None && now >= aiming.NextBeep)
                Beep(launcher, aiming, now);

            if (now < aiming.NextScan)
                continue;

            aiming.NextScan = now + ScanInterval;

            if (!_manpadGunship.IsAiming(launcher))
            {
                _stopQueue.Add(launcher);
                continue;
            }

            switch (manpad.LockState)
            {
                case RuMCManpadGunshipLockState.None:
                    if (now >= aiming.NextAcquire && _targeting.TryFindTarget(launcher, out var target))
                        BeginLock(launcher, aiming, target, now);
                    break;
                case RuMCManpadGunshipLockState.Locking:
                    if (!IsTargetTrackable(launcher, aiming))
                        LoseLock(launcher, aiming, now);
                    else if (now >= aiming.LockCompleteAt)
                        CompleteLock(launcher, aiming, now);
                    break;
                case RuMCManpadGunshipLockState.Locked:
                    if (!IsTargetTrackable(launcher, aiming))
                        LoseLock(launcher, aiming, now);
                    break;
            }
        }

        foreach (var launcher in _stopQueue)
        {
            _manpadGunship.StopAiming(launcher);
            RemComp<RuMCManpadGunshipAimingComponent>(launcher);
        }

        _stopQueue.Clear();
    }

    /// <summary>Back to scanning.</summary>
    public void ResetLock(Entity<RuMCManpadGunshipComponent> launcher, RuMCManpadGunshipAimingComponent aiming)
    {
        aiming.Target = null;
        _manpadGunship.SetLockState(launcher, RuMCManpadGunshipLockState.None);
    }

    private bool IsTargetTrackable(Entity<RuMCManpadGunshipComponent> launcher, RuMCManpadGunshipAimingComponent aiming)
    {
        return aiming.Target is { } target && _targeting.CanTrack(launcher, target);
    }

    private void Beep(
        Entity<RuMCManpadGunshipComponent> launcher,
        RuMCManpadGunshipAimingComponent aiming,
        TimeSpan now)
    {
        if (launcher.Comp.AimingUser is not { } user)
            return;

        var comp = launcher.Comp;
        var locked = comp.LockState == RuMCManpadGunshipLockState.Locked;
        _audio.PlayGlobal(locked ? comp.LockedSound : comp.LockingSound, user);

        var interval = comp.BeepIntervalEnd;
        if (!locked)
        {
            var progress = comp.LockTime > TimeSpan.Zero
                ? Math.Clamp((now - aiming.LockStartedAt) / comp.LockTime, 0, 1)
                : 1;
            interval = comp.BeepIntervalStart + (comp.BeepIntervalEnd - comp.BeepIntervalStart) * progress;
        }

        aiming.NextBeep = now + interval;
    }

    private void BeginLock(
        Entity<RuMCManpadGunshipComponent> launcher,
        RuMCManpadGunshipAimingComponent aiming,
        EntityUid target,
        TimeSpan now)
    {
        if (launcher.Comp.AimingUser is not { } user)
            return;

        aiming.Target = target;
        aiming.LockStartedAt = now;
        aiming.LockCompleteAt = now + launcher.Comp.LockTime;
        aiming.NextBeep = now;
        _manpadGunship.SetLockState(launcher, RuMCManpadGunshipLockState.Locking);
        _popup.PopupEntity(Loc.GetString("rumc-manpad-gunship-locking"), user, user);
    }

    private void CompleteLock(
        Entity<RuMCManpadGunshipComponent> launcher,
        RuMCManpadGunshipAimingComponent aiming,
        TimeSpan now)
    {
        if (launcher.Comp.AimingUser is not { } user)
            return;

        aiming.NextBeep = now;
        _manpadGunship.SetLockState(launcher, RuMCManpadGunshipLockState.Locked);
        _popup.PopupEntity(Loc.GetString("rumc-manpad-gunship-locked"), user, user, PopupType.Medium);
    }

    private void LoseLock(
        Entity<RuMCManpadGunshipComponent> launcher,
        RuMCManpadGunshipAimingComponent aiming,
        TimeSpan now)
    {
        if (launcher.Comp.AimingUser is { } user)
            _popup.PopupEntity(Loc.GetString("rumc-manpad-gunship-lock-lost"), user, user, PopupType.SmallCaution);

        aiming.NextAcquire = now + launcher.Comp.LockDelay;
        ResetLock(launcher, aiming);
    }
}

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.ManpadGunship;

/// <summary>Gunship aiming mode of a MANPAD.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RuMCManpadGunshipComponent : Component
{
    [DataField]
    public EntProtoId AimActionId = "RuMCActionManpadGunshipAim";

    [DataField, AutoNetworkedField]
    public EntityUid? AimAction;

    [AutoNetworkedField]
    public EntityUid? AimingUser;

    [AutoNetworkedField]
    public RuMCManpadGunshipLockState LockState;

    /// <summary>Time a gunship must be tracked before firing.</summary>
    [DataField]
    public TimeSpan LockTime = TimeSpan.FromSeconds(3);

    /// <summary>Pause after a lost lock.</summary>
    [DataField]
    public TimeSpan LockDelay = TimeSpan.FromSeconds(1.5);

    /// <summary>Horizontal range in tiles.</summary>
    [DataField]
    public float LockRange = 15f;

    /// <summary>Launch to impact.</summary>
    [DataField]
    public TimeSpan MissileFlightTime = TimeSpan.FromSeconds(3);

    /// <summary>Hull integrity removed on impact.</summary>
    [DataField]
    public float Damage = 800f;

    [DataField]
    public SoundSpecifier LaunchSound = new SoundPathSpecifier("/Audio/CMU14/Fighter/manpad-launch.wav");

    /// <summary>Warning aboard the gunship.</summary>
    [DataField]
    public SoundSpecifier IncomingSound = new SoundPathSpecifier("/Audio/CMU14/Fighter/manpad-incoming.wav");

    /// <summary>Played on the gunship's and the shooter's level.</summary>
    [DataField]
    public SoundSpecifier ImpactSound = new SoundPathSpecifier("/Audio/CMU14/Fighter/missile-impact.ogg");

    /// <summary>Spawned on the gunship's level.</summary>
    [DataField]
    public EntProtoId HitEffect = "RuMCManpadGunshipHitEffect";

    /// <summary>Fireballs per impact.</summary>
    [DataField]
    public int HitEffectCount = 3;

    /// <summary>Fireball scatter in tiles.</summary>
    [DataField]
    public float HitEffectSpread = 2.5f;

    /// <summary>Sound range in tiles.</summary>
    [DataField]
    public float SoundRange = 65f;

    /// <summary>Beep while locking.</summary>
    [DataField]
    public SoundSpecifier LockingSound = new SoundPathSpecifier(
        "/Audio/Machines/quickbeep.ogg",
        AudioParams.Default.WithVolume(-4));

    /// <summary>Beep once locked.</summary>
    [DataField]
    public SoundSpecifier LockedSound = new SoundPathSpecifier(
        "/Audio/Machines/quickbeep.ogg",
        AudioParams.Default.WithVolume(-4).WithPitchScale(1.6f));

    /// <summary>Beep gap at lock start.</summary>
    [DataField]
    public TimeSpan BeepIntervalStart = TimeSpan.FromSeconds(0.7);

    /// <summary>Beep gap at the end of the lock and while locked.</summary>
    [DataField]
    public TimeSpan BeepIntervalEnd = TimeSpan.FromSeconds(0.2);
}

[Serializable, NetSerializable]
public enum RuMCManpadGunshipLockState : byte
{
    None,
    Locking,
    Locked,
}

public sealed partial class RuMCManpadGunshipAimActionEvent : InstantActionEvent;

/// <summary>Raised when the operator raises the launcher against gunships.</summary>
[ByRefEvent]
public readonly record struct RuMCManpadGunshipAimStartedEvent(EntityUid User);

/// <summary>Raised when the mode stops: lowered, dropped, unwielded, empty or switched.</summary>
[ByRefEvent]
public readonly record struct RuMCManpadGunshipAimStoppedEvent;

/// <summary>Raised when the trigger is pulled with a lock.</summary>
[ByRefEvent]
public readonly record struct RuMCManpadGunshipLaunchRequestEvent(EntityUid User);

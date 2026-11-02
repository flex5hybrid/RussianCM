using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.ManpadLowAltitude;

/// <summary>Low altitude aiming mode of a MANPAD: engages gunships and Blackfoots.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RuMCManpadLowAltitudeComponent : Component
{
    [DataField]
    public EntProtoId AimActionId = "RuMCActionManpadLowAltitudeAim";

    [DataField, AutoNetworkedField]
    public EntityUid? AimAction;

    [AutoNetworkedField]
    public EntityUid? AimingUser;

    [AutoNetworkedField]
    public RuMCManpadLowAltitudeLockState LockState;

    /// <summary>What the current lock is on, for the operator's readout.</summary>
    [AutoNetworkedField]
    public RuMCManpadLowAltitudeTargetKind TargetKind;

    /// <summary>Time a target must be tracked before firing.</summary>
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

    [DataField]
    public SoundSpecifier LaunchSound = new SoundPathSpecifier("/Audio/CMU14/Fighter/manpad-launch.wav");

    /// <summary>Warning aboard the target.</summary>
    [DataField]
    public SoundSpecifier IncomingSound = new SoundPathSpecifier("/Audio/CMU14/Fighter/manpad-incoming.wav");

    /// <summary>Played on the target's and the shooter's level.</summary>
    [DataField]
    public SoundSpecifier ImpactSound = new SoundPathSpecifier("/Audio/CMU14/Fighter/missile-impact.ogg");

    /// <summary>What a hit does to a gunship.</summary>
    [DataField]
    public RuMCManpadLowAltitudeHitProfile GunshipHit = new()
    {
        Damage = 800f,
        Effect = "RuMCManpadGunshipHitEffect",
        EffectCount = 3,
        EffectSpread = 2.5f,
    };

    /// <summary>What a hit does to a Blackfoot: a smaller blast that matches its size.</summary>
    [DataField]
    public RuMCManpadLowAltitudeHitProfile BlackfootHit = new()
    {
        Damage = 400f,
        Effect = "RuMCManpadBlackfootHitEffect",
        EffectCount = 2,
        EffectSpread = 0.8f,
    };

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
public enum RuMCManpadLowAltitudeLockState : byte
{
    None,
    Locking,
    Locked,
}

/// <summary>Damage and fireballs of a missile hit on one kind of aircraft.</summary>
[DataDefinition]
public sealed partial class RuMCManpadLowAltitudeHitProfile
{
    /// <summary>Hull integrity removed on impact.</summary>
    [DataField]
    public float Damage;

    /// <summary>Spawned on the target's level.</summary>
    [DataField]
    public EntProtoId Effect;

    /// <summary>Fireballs per impact.</summary>
    [DataField]
    public int EffectCount;

    /// <summary>Fireball scatter in tiles.</summary>
    [DataField]
    public float EffectSpread;
}

/// <summary>The kind of aircraft the MANPAD is locking on.</summary>
[Serializable, NetSerializable]
public enum RuMCManpadLowAltitudeTargetKind : byte
{
    None,
    Gunship,
    Blackfoot,
}

public sealed partial class RuMCManpadLowAltitudeAimActionEvent : InstantActionEvent;

/// <summary>Raised when the operator raises the launcher for the low altitude scan.</summary>
[ByRefEvent]
public readonly record struct RuMCManpadLowAltitudeAimStartedEvent(EntityUid User);

/// <summary>Raised when the mode stops: lowered, dropped, unwielded, empty or switched.</summary>
[ByRefEvent]
public readonly record struct RuMCManpadLowAltitudeAimStoppedEvent;

/// <summary>Raised when the trigger is pulled with a lock.</summary>
[ByRefEvent]
public readonly record struct RuMCManpadLowAltitudeLaunchRequestEvent(EntityUid User);

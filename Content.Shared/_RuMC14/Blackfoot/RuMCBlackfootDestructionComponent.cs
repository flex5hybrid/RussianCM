using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RuMC14.Blackfoot;

/// <summary>
/// How a Blackfoot that lost its thrusters in the air is destroyed: it spins out of control
/// while it falls, then crashes, throws its crew out on fire and starts a blaze.
/// </summary>
[RegisterComponent]
public sealed partial class RuMCBlackfootDestructionComponent : Component
{
    /// <summary>Time between the hull failing and the crash.</summary>
    [DataField]
    public TimeSpan FallDelay = TimeSpan.FromSeconds(4);

    /// <summary>Degrees per second gained every second while falling.</summary>
    [DataField]
    public float SpinAcceleration = 120f;

    [DataField]
    public EntProtoId CrashEffect = "RuMCBlackfootCrashEffect";

    [DataField]
    public int CrashEffectCount = 4;

    [DataField]
    public float CrashEffectSpread = 1.8f;

    [DataField]
    public SoundSpecifier CrashSound = new SoundPathSpecifier("/Audio/_RMC14/Explosion/bigboom3.ogg");

    [DataField]
    public float SoundRange = 35f;

    /// <summary>Players within this many tiles feel the crash.</summary>
    [DataField]
    public float CameraShakeRange = 14f;

    [DataField]
    public string ExplosionType = "RMC";

    [DataField]
    public float ExplosionTotalIntensity = 100f;

    [DataField]
    public float ExplosionSlope = 10f;

    [DataField]
    public float ExplosionMaxTileIntensity = 12f;

    [DataField]
    public EntProtoId FireProto = "RMCTileFire";

    /// <summary>Tiles around the wreck that burn.</summary>
    [DataField]
    public int FireRange = 2;

    /// <summary>How far the crew is thrown from the wreck, as with a tank cook-off.</summary>
    [DataField]
    public float EjectionDistance = 4f;

    [DataField]
    public float EjectionSpeed = 8f;

    [DataField]
    public int CrewFireIntensity = 30;

    [DataField]
    public int CrewFireDuration = 40;
}

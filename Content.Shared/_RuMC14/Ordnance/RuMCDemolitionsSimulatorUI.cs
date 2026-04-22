using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Ordnance;

/// <summary>
///     UI channel for the handheld demolitions simulator.
/// </summary>
[Serializable, NetSerializable]
public enum RMCDemolitionsSimulatorUiKey
{
    Key,
}

/// <summary>
///     Requests a new simulated detonation for the casing held in the active hand.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCDemolitionsSimulatorSimulateMsg : BoundUserInterfaceMessage;

/// <summary>
///     Sent from server to client when simulation results are ready.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCDemolitionsSimulatorBuiState(
    string casingName,
    float currentVolume,
    float maxVolume,
    bool hasExplosion,
    float totalIntensity,
    float intensitySlope,
    float maxIntensity,
    bool hasFire,
    float fireIntensity,
    float fireRadius,
    float fireDuration,
    bool onCooldown,
    int cooldownSecsLeft,
    NetEntity? cameraNetId) : BoundUserInterfaceState
{
    public readonly string CasingName = casingName;
    public readonly float CurrentVolume = currentVolume;
    public readonly float MaxVolume = maxVolume;

    public readonly bool HasExplosion = hasExplosion;
    public readonly float TotalIntensity = totalIntensity;
    public readonly float IntensitySlope = intensitySlope;
    public readonly float MaxIntensity = maxIntensity;

    public readonly bool HasFire = hasFire;
    public readonly float FireIntensity = fireIntensity;
    public readonly float FireRadius = fireRadius;
    public readonly float FireDuration = fireDuration;

    public readonly bool OnCooldown = onCooldown;
    public readonly int CooldownSecsLeft = cooldownSecsLeft;

    public readonly NetEntity? CameraNetId = cameraNetId;
}

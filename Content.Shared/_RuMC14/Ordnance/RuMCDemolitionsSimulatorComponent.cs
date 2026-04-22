using Robust.Shared.GameStates;

namespace Content.Shared._RuMC14.Ordnance;

/// <summary>
///     Server-owned state for the handheld demolitions simulator.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCDemolitionsSimulatorComponent : Component
{
    /// <summary>
    ///     Minimum time between simulation runs so the test chamber is not spammed.
    /// </summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Absolute time when the simulator leaves cooldown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CooldownEnd;

    /// <summary>
    ///     Chamber camera network entity exposed to the client for replay viewing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? ChamberCamera;

    /// <summary>
    ///     Last simulated casing label shown in the UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string LastCasingName = string.Empty;

    [DataField, AutoNetworkedField]
    public float LastCurrentVolume;

    [DataField, AutoNetworkedField]
    public float LastMaxVolume;

    [DataField, AutoNetworkedField]
    public bool LastHasExplosion;

    [DataField, AutoNetworkedField]
    public float LastTotalIntensity;

    [DataField, AutoNetworkedField]
    public float LastIntensitySlope;

    [DataField, AutoNetworkedField]
    public float LastMaxIntensity;

    [DataField, AutoNetworkedField]
    public bool LastHasFire;

    [DataField, AutoNetworkedField]
    public float LastFireIntensity;

    [DataField, AutoNetworkedField]
    public float LastFireRadius;

    [DataField, AutoNetworkedField]
    public float LastFireDuration;

    /// <summary>
    ///     Server-only replay chamber map used to stage the simulated blast.
    /// </summary>
    public EntityUid ChamberMapEnt = EntityUid.Invalid;

    /// <summary>
    ///     Server-side entity for the active replay camera.
    /// </summary>
    public EntityUid ChamberCameraEnt = EntityUid.Invalid;

    /// <summary>
    ///     Whether the next delayed replay explosion is armed.
    /// </summary>
    public bool PendingExplosion;

    public TimeSpan ExplosionAt;
    public float PendingTotalIntensity;
    public float PendingIntensitySlope;
    public float PendingMaxIntensity;
}

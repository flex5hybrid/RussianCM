using System.Collections.Generic;
using Content.Shared._CMU14.Medical.Bones;
using Content.Shared._CMU14.Medical.Organs;
using Content.Shared._CMU14.Medical.Wounds;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Scanner;

/// <summary>
///     Pure scan state — no BUI dependency. Used by body scanner snapshots, stored medical
///     records, and as the payload inside <see cref="HealthScannerBuiState"/>.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public readonly record struct HealthScanState(
    NetEntity Target,
    FixedPoint2 Blood,
    FixedPoint2 MaxBlood,
    float? Temperature,
    string Pulse,
    Solution? Chemicals,
    bool Bleeding,
    HealthScanDetailLevel DetailLevel);

/// <summary>
///     Thin BUI wrapper around <see cref="HealthScanState"/> for the health analyzer live-update path.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthScannerBuiState(HealthScanState scanState) : BoundUserInterfaceState
{
    public readonly NetEntity Target = scanState.Target;
    public readonly FixedPoint2 Blood = scanState.Blood;
    public readonly FixedPoint2 MaxBlood = scanState.MaxBlood;
    public readonly float? Temperature = scanState.Temperature;
    public readonly string Pulse = scanState.Pulse;
    public readonly Solution? Chemicals = scanState.Chemicals;
    public readonly bool Bleeding = scanState.Bleeding;
    public readonly HealthScanDetailLevel DetailLevel = scanState.DetailLevel;
    public Dictionary<BodyPartType, CMUBodyPartReadout>? CMUParts;
    public List<CMUOrganReadout>? CMUOrgans;
    public List<CMUFractureReadout>? CMUFractures;
    public List<CMUInternalBleedReadout>? CMUInternalBleeds;
    public int? CMUHeartBpm;
    public bool? CMUHeartStopped;
    public CMUPainShockRisk? CMUPainShockRisk;
    public bool CMUPainShockSuppressed;
    public bool CMUExternalBleeding;
    public bool CMUSyntheticPhysiology;
    public HealthScannerDamageReadout Damage = new();
    public HealthScannerAdviceReadout Advice = new();
    public MobState MobState;
    public bool HasIncapThreshold;
    public FixedPoint2 IncapThreshold;
    public bool HasDeadThreshold;
    public FixedPoint2 DeadThreshold;
    public bool PermaDead;
    public bool VictimBurst;
    public bool VictimInfected;
    public bool HolocardXeno;
}

[Serializable, NetSerializable]
public sealed class HealthScannerDamageReadout
{
    public FixedPoint2 Brute;
    public FixedPoint2 Burn;
    public FixedPoint2 Toxin;
    public FixedPoint2 Airloss;
    public FixedPoint2 Genetic;
    public FixedPoint2 Total;
    public bool UntreatedBruteWounds;
    public bool UntreatedBurnWounds;
}

[Serializable, NetSerializable]
public sealed class HealthScannerAdviceReadout
{
    public bool NeedsEpinephrine;
    public bool ShowRepeatedDefibWarning;
    public bool ShowDefib;
    public bool ShowCpr;
    public bool ShowLarvaBursted;
    public bool ShowLarvaSurgery;
    public bool ShowBruteWounds;
    public bool ShowBurnWounds;
    public bool ShowBloodPack;
    public bool ShowFood;
    public bool ShowCprCrit;
    public bool ShowDexalin;
    public bool ShowBicaridine;
    public bool ShowKelotane;
    public bool ShowDylovene;
}

[Serializable, NetSerializable]
public readonly record struct CMUBodyPartReadout(
    BodyPartType Type,
    BodyPartSymmetry Symmetry,
    FixedPoint2 Current,
    FixedPoint2 Max,
    WoundSize? WoundDescriptor,
    bool Eschar,
    bool Splinted,
    bool Cast,
    bool Tourniquet);

[Serializable, NetSerializable]
public readonly record struct CMUOrganReadout(
    string OrganName,
    OrganDamageStage Stage,
    FixedPoint2 Current,
    FixedPoint2 Max,
    bool Removed);

[Serializable, NetSerializable]
public readonly record struct CMUFractureReadout(
    BodyPartType Part,
    BodyPartSymmetry Symmetry,
    FractureSeverity Severity,
    bool ExactSeverity,
    bool Suppressed);

[Serializable, NetSerializable]
public readonly record struct CMUInternalBleedReadout(
    BodyPartType Part,
    BodyPartSymmetry Symmetry,
    bool ExactLocationKnown,
    float BloodlossPerSecond);

[Serializable, NetSerializable]
public enum CMUPainShockRisk : byte
{
    Low,
    Elevated,
    High,
    Imminent,
    Active,
}

[Serializable, NetSerializable]
public enum HealthScannerUIKey
{
    Key
}

[Serializable, NetSerializable]
public enum HealthScanDetailLevel : byte
{
    HealthAnalyzer = 0,
    BodyScan = 1,
    Full = 2,
}

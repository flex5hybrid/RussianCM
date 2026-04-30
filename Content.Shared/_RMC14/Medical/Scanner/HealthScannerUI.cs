using System.Collections.Generic;
using Content.Shared._CMU14.Medical.Bones;
using Content.Shared._CMU14.Medical.Organs;
using Content.Shared._CMU14.Medical.StatusEffects;
using Content.Shared._CMU14.Medical.Wounds;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
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
    public readonly NetEntity Target = target;
    public readonly FixedPoint2 Blood = blood;
    public readonly FixedPoint2 MaxBlood = maxBlood;
    public readonly float? Temperature = temperature;
    public readonly Solution? Chemicals = chemicals;
    public readonly bool Bleeding = bleeding;
    public Dictionary<BodyPartType, CMUBodyPartReadout>? CMUParts;
    public List<CMUOrganReadout>? CMUOrgans;
    public List<CMUFractureReadout>? CMUFractures;
    public List<CMUInternalBleedReadout>? CMUInternalBleeds;
    public int? CMUHeartBpm;
    public bool? CMUHeartStopped;
    public PainTier? CMUPainTier;
    public bool CMUExternalBleeding;
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

using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Ordnance;

/// <summary>
///     Shared ordnance chemistry estimator used by scanners and simulators.
///     Keeping the numbers in one place helps the custom ordnance pipeline stay in parity.
/// </summary>
public static class RMCOrdnanceYieldEstimator
{
    /// <summary>
    ///     Default simulator profile used when we only have a reagent beaker and no physical casing.
    /// </summary>
    public static readonly RMCOrdnanceYieldProfile ExplosionSimulationProfile = new(
        180f,
        80f,
        25f,
        3f,
        25f,
        1f,
        5f,
        3f,
        24f,
        "RMCTileFire");

    /// <summary>
    ///     Builds a reusable yield profile from a live custom casing.
    /// </summary>
    public static RMCOrdnanceYieldProfile FromCasing(RMCChembombCasingComponent casing)
    {
        return new RMCOrdnanceYieldProfile(
            casing.BasePower,
            casing.BaseFalloff,
            casing.MinFalloff,
            casing.MinFireIntensity,
            casing.MaxFireIntensity,
            casing.MinFireRadius,
            casing.MaxFireRadius,
            casing.MinFireDuration,
            casing.MaxFireDuration,
            casing.DefaultFireEntity);
    }

    /// <summary>
    ///     Estimates blast and incendiary output for a chemical payload.
    /// </summary>
    public static RMCOrdnanceYieldEstimate Estimate(
        Solution? solution,
        IPrototypeManager prototype,
        in RMCOrdnanceYieldProfile profile)
    {
        var powerMod = 0f;
        var falloffMod = 0f;
        var intensityMod = 0f;
        var radiusMod = 0f;
        var durationMod = 0f;
        var hasExplosive = false;
        var hasIncendiary = false;
        var fireEntity = profile.DefaultFireEntity;
        var currentVolume = solution?.Volume.Float() ?? 0f;

        if (solution != null)
        {
            foreach (var reagent in solution)
            {
                if (!prototype.TryIndexReagent(reagent.Reagent.Prototype, out ReagentPrototype? proto))
                    continue;

                var quantity = (float) reagent.Quantity;
                powerMod += quantity * (float) proto.PowerMod;
                falloffMod += quantity * (float) proto.FalloffMod;
                intensityMod += quantity * (float) proto.IntensityMod;
                radiusMod += quantity * (float) proto.RadiusMod;
                durationMod += quantity * (float) proto.DurationMod;

                if (proto.PowerMod > FixedPoint2.Zero)
                    hasExplosive = true;

                if (proto.IntensityMod > FixedPoint2.Zero || proto.Intensity > 0)
                {
                    hasIncendiary = true;

                    if (proto.Intensity > 0)
                        fireEntity = proto.FireEntity;
                }
            }
        }

        var hasExplosion = hasExplosive || powerMod > 0f;
        var blastPower = hasExplosion
            ? MathF.Max(1f, profile.BasePower + powerMod)
            : 0f;
        var blastFalloff = hasExplosion
            ? MathF.Max(profile.MinFalloff, profile.BaseFalloff + falloffMod)
            : 0f;
        var intensitySlope = hasExplosion
            ? MathF.Max(1.5f, blastFalloff / 14f)
            : 0f;
        var maxIntensity = hasExplosion
            ? MathF.Max(5f, blastPower / 15f)
            : 0f;

        var fireIntensity = hasIncendiary
            ? Math.Clamp(profile.MinFireIntensity + intensityMod, profile.MinFireIntensity, profile.MaxFireIntensity)
            : 0f;
        var fireRadius = hasIncendiary
            ? Math.Clamp(profile.MinFireRadius + radiusMod, profile.MinFireRadius, profile.MaxFireRadius)
            : 0f;
        var fireDuration = hasIncendiary
            ? Math.Clamp(profile.MinFireDuration + durationMod, profile.MinFireDuration, profile.MaxFireDuration)
            : 0f;

        return new RMCOrdnanceYieldEstimate(
            currentVolume,
            hasExplosion,
            blastPower,
            blastFalloff,
            intensitySlope,
            maxIntensity,
            hasIncendiary,
            fireIntensity,
            fireRadius,
            fireDuration,
            fireEntity);
    }
}

/// <summary>
///     Envelope that defines how a casing converts reagent modifiers into final blast and fire output.
/// </summary>
public readonly record struct RMCOrdnanceYieldProfile(
    float BasePower,
    float BaseFalloff,
    float MinFalloff,
    float MinFireIntensity,
    float MaxFireIntensity,
    float MinFireRadius,
    float MaxFireRadius,
    float MinFireDuration,
    float MaxFireDuration,
    EntProtoId DefaultFireEntity);

/// <summary>
///     Final yield estimate returned by <see cref="RMCOrdnanceYieldEstimator"/>.
/// </summary>
public readonly record struct RMCOrdnanceYieldEstimate(
    float CurrentVolume,
    bool HasExplosion,
    float TotalIntensity,
    float BlastFalloff,
    float IntensitySlope,
    float MaxIntensity,
    bool HasFire,
    float FireIntensity,
    float FireRadius,
    float FireDuration,
    EntProtoId FireEntity)
{
    public float ApproxBlastRadius => HasExplosion
        ? MathF.Sqrt(MathF.Max(1f, TotalIntensity) / MathF.Max(1.5f, IntensitySlope))
        : 0f;
}

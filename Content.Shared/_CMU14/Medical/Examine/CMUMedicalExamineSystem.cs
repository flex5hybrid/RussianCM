using System;
using System.Collections.Generic;
using Content.Shared._CMU14.Medical;
using Content.Shared._CMU14.Medical.Bones;
using Content.Shared._CMU14.Medical.Items;
using Content.Shared._CMU14.Medical.Wounds;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Examine;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;

namespace Content.Shared._CMU14.Medical.Examine;

public sealed partial class CMUMedicalExamineSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    private const string UntreatedWoundColor = "#ff4d4d";
    private const string TreatedWoundColor = "#7bd88f";
    private const string FractureColor = "#dca94c";
    private const string SeveredColor = "#ff4d4d";
    private const string DetailedPartColor = "#9fc7ff";
    private const string DetailedWoundColor = "#ffb86c";
    private const string DetailedBurnColor = "#ff704d";
    private const string DetailedBleedColor = "#ff5f5f";
    private const string DetailedUntreatedColor = "#ffd166";
    private const string DetailedAdequateColor = "#f0c85a";
    private const string DetailedOptimalColor = "#7bd88f";
    private const string DetailedCleanupColor = "#d987ff";
    private const string DetailedHintColor = "#83c9ff";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMUHumanMedicalComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<CMUHumanMedicalComponent> ent, ref ExaminedEvent args)
    {
        if (!_cfg.GetCVar(CMUMedicalCCVars.Enabled))
            return;

        using (args.PushGroup(nameof(CMUMedicalExamineSystem), -1))
        {
            AddBodyPartLines(
                ent,
                args,
                _cfg.GetCVar(CMUMedicalCCVars.WoundsEnabled),
                _cfg.GetCVar(CMUMedicalCCVars.BoneEnabled),
                _cfg.GetCVar(CMUMedicalCCVars.BodyPartEnabled));
        }
    }

    private void AddBodyPartLines(
        EntityUid body,
        ExaminedEvent args,
        bool includeWounds,
        bool includeFractures,
        bool includeMissingParts)
    {
        var partSummaries = new List<BodyPartExamineSummary>();

        foreach (var (partUid, part) in _body.GetBodyChildren(body))
        {
            var sections = new List<string>();

            if (includeWounds)
            {
                var untreated = new List<string>();
                var treated = new List<string>();
                if (TryComp<BodyPartWoundComponent>(partUid, out var wounds))
                {
                    for (var i = 0; i < wounds.Wounds.Count; i++)
                    {
                        if (IsWoundTreatedForExamine(wounds, i))
                            treated.Add(DescribeVisibleWound(wounds, i));
                        else
                            untreated.Add(DescribeVisibleWound(wounds, i));
                    }

                    if (wounds.ExternalBleeding != ExternalBleedTier.None)
                        untreated.Add(Loc.GetString("cmu-medical-examine-wound-bleeding-active"));
                }

                if (HasComp<CMUEscharComponent>(partUid))
                    untreated.Add(Loc.GetString("cmu-medical-examine-eschar"));

                if (untreated.Count > 0)
                    sections.Add($"[color={UntreatedWoundColor}]{ToSentence(untreated)}[/color]");

                if (treated.Count > 0)
                    sections.Add($"[color={TreatedWoundColor}]{ToSentence(treated)}[/color]");
            }

            if (includeFractures
                && TryComp<FractureComponent>(partUid, out var fracture)
                && fracture.Severity is FractureSeverity.Compound or FractureSeverity.Comminuted)
            {
                var stabilized = HasComp<CMUSplintedComponent>(partUid) || HasComp<CMUCastComponent>(partUid);
                sections.Add($"[color={FractureColor}]{DescribeVisibleFracture(fracture.Severity, stabilized)}[/color]");
            }

            if (sections.Count == 0)
                continue;

            partSummaries.Add(new BodyPartExamineSummary(
                BodyPartSortOrder(part.PartType, part.Symmetry),
                FormatPartName(part.PartType, part.Symmetry),
                ToSemicolonList(sections)));
        }

        if (includeMissingParts)
        {
            foreach (var (type, symmetry) in GetMissingPartSlots(body))
            {
                partSummaries.Add(new BodyPartExamineSummary(
                    BodyPartSortOrder(type, symmetry),
                    FormatPartName(type, symmetry),
                    $"[color={SeveredColor}]{Loc.GetString("cmu-medical-examine-part-severed")}[/color]"));
            }
        }

        partSummaries.Sort((a, b) => a.Order.CompareTo(b.Order));

        foreach (var summary in partSummaries)
        {
            args.PushMarkup(Loc.GetString(
                "cmu-medical-examine-body-part-line",
                ("part", summary.Part),
                ("conditions", summary.Conditions)));
        }
    }

    public string GetDetailedExamineText(EntityUid body)
    {
        var partSummaries = new List<BodyPartExamineSummary>();

        foreach (var (partUid, part) in _body.GetBodyChildren(body))
        {
            var sections = new List<string>();

            if (TryComp<BodyPartWoundComponent>(partUid, out var wounds))
            {
                for (var i = 0; i < wounds.Wounds.Count; i++)
                {
                    if (IsOptimallyTreatedForDetailedExamine(wounds, i))
                        continue;

                    sections.Add(DescribeDetailedWound(wounds, i));
                }

                if (wounds.ExternalBleeding != ExternalBleedTier.None)
                    sections.Add(Color($"external bleeding: {DescribeBleedTier(wounds.ExternalBleeding)}", DetailedBleedColor));
            }

            if (HasComp<CMUEscharComponent>(partUid))
                sections.Add(Color("burn eschar: charred tissue", DetailedBurnColor));

            if (sections.Count == 0)
                continue;

            partSummaries.Add(new BodyPartExamineSummary(
                BodyPartSortOrder(part.PartType, part.Symmetry),
                PartHeader(part.PartType, part.Symmetry),
                ToDetailedLines(sections)));
        }

        foreach (var (type, symmetry) in GetMissingPartSlots(body))
        {
            partSummaries.Add(new BodyPartExamineSummary(
                BodyPartSortOrder(type, symmetry),
                PartHeader(type, symmetry),
                Color("severed", SeveredColor)));
        }

        if (partSummaries.Count == 0)
            return Loc.GetString("cmu-medical-detailed-examine-none");

        partSummaries.Sort((a, b) => a.Order.CompareTo(b.Order));

        var lines = new List<string>(partSummaries.Count);
        foreach (var summary in partSummaries)
        {
            lines.Add($"{summary.Part}:\n  {summary.Conditions}");
        }

        return string.Join('\n', lines);
    }

    private static bool IsOptimallyTreatedForDetailedExamine(BodyPartWoundComponent wounds, int index)
    {
        var cleanup = index < wounds.Cleanup.Count ? wounds.Cleanup[index] : WoundCleanupFlags.None;
        return GetTreatmentQuality(wounds, index) == WoundTreatmentQuality.Optimal &&
               cleanup == WoundCleanupFlags.None;
    }

    private List<(BodyPartType Type, BodyPartSymmetry Symmetry)> GetMissingPartSlots(EntityUid body)
    {
        var missing = new List<(BodyPartType Type, BodyPartSymmetry Symmetry)>();
        if (!TryComp<BodyComponent>(body, out var bodyComp))
            return missing;

        if (_body.GetRootPartOrNull(body, bodyComp) is not { } root)
            return missing;

        AddMissingChildSlots(root.Entity, root.BodyPart, missing);

        foreach (var (partUid, part) in _body.GetBodyChildren(body, bodyComp))
        {
            if (partUid == root.Entity)
                continue;

            AddMissingChildSlots(partUid, part, missing);
        }

        return missing;
    }

    private void AddMissingChildSlots(
        EntityUid parent,
        BodyPartComponent parentPart,
        List<(BodyPartType Type, BodyPartSymmetry Symmetry)> missing)
    {
        foreach (var (slotId, slot) in parentPart.Children)
        {
            if (!IsReportableMissingPart(slot.Type))
                continue;

            var containerId = SharedBodySystem.GetPartSlotContainerId(slotId);
            if (_containers.TryGetContainer(parent, containerId, out var container) &&
                container.ContainedEntities.Count > 0)
            {
                continue;
            }

            if (TryGetPartSymmetry(slotId, parentPart.Symmetry, out var symmetry))
                missing.Add((slot.Type, symmetry));
        }
    }

    private static bool IsReportableMissingPart(BodyPartType type)
    {
        return type is BodyPartType.Arm
            or BodyPartType.Hand
            or BodyPartType.Leg
            or BodyPartType.Foot;
    }

    private static bool TryGetPartSymmetry(string slotId, BodyPartSymmetry parentSymmetry, out BodyPartSymmetry symmetry)
    {
        if (slotId.Contains("left", StringComparison.OrdinalIgnoreCase))
        {
            symmetry = BodyPartSymmetry.Left;
            return true;
        }

        if (slotId.Contains("right", StringComparison.OrdinalIgnoreCase))
        {
            symmetry = BodyPartSymmetry.Right;
            return true;
        }

        if (parentSymmetry is BodyPartSymmetry.Left or BodyPartSymmetry.Right)
        {
            symmetry = parentSymmetry;
            return true;
        }

        symmetry = BodyPartSymmetry.None;
        return false;
    }

    private string DescribeVisibleWound(BodyPartWoundComponent wounds, int index) // RuMC edit
    {
        var wound = wounds.Wounds[index];
        var size = index < wounds.Sizes.Count ? wounds.Sizes[index] : WoundSize.Deep;
        var sizeText = size switch
        {
            WoundSize.Small => Loc.GetString("cmu-medical-examine-wound-size-small"),
            WoundSize.Deep => Loc.GetString("cmu-medical-examine-wound-size-deep-visible"),
            WoundSize.Gaping => Loc.GetString("cmu-medical-examine-wound-size-gaping-visible"),
            WoundSize.Massive => Loc.GetString("cmu-medical-examine-wound-size-massive"),
            _ => Loc.GetString("cmu-medical-examine-wound-size-deep-visible"),
        };

        var kind = wound.Type switch
        {
            WoundType.Burn => Loc.GetString("cmu-medical-examine-wound-type-burn"),
            WoundType.Surgery => Loc.GetString("cmu-medical-examine-wound-type-wound"),
            _ => GetVisibleWoundKind(wounds, index),
        };

        return Loc.GetString(
            "cmu-medical-examine-wound-visible",
            ("treated", IsWoundTreatedForExamine(wounds, index) ? "true" : "false"),
            ("size", sizeText),
            ("type", kind));
    }

    private string GetVisibleWoundKind(BodyPartWoundComponent wounds, int index) // RuMC edit
    {
        if (index < wounds.Mechanisms.Count && wounds.Mechanisms[index] == WoundMechanism.Burn)
            return Loc.GetString("cmu-medical-examine-wound-type-burn");

        return Loc.GetString("cmu-medical-examine-wound-type-wound");
    }

    private string DescribeVisibleFracture(FractureSeverity severity, bool stabilized) // RuMC edit
    {
        var prefix = stabilized ? "stabilized " : string.Empty;
        var key = severity switch
        {
            FractureSeverity.Compound => "cmu-medical-examine-fracture-compound",
            FractureSeverity.Comminuted => "cmu-medical-examine-fracture-comminuted",
            _ => "cmu-medical-examine-fracture-simple",
        };
        return Loc.GetString(key, ("stabilized", stabilized ? "true" : "false"));
    }

    private static string DescribeDetailedWound(BodyPartWoundComponent wounds, int index)
    {
        var wound = wounds.Wounds[index];
        var size = index < wounds.Sizes.Count ? wounds.Sizes[index] : WoundSize.Deep;
        var mechanism = index < wounds.Mechanisms.Count ? wounds.Mechanisms[index] : LegacyMechanismFor(wound.Type);
        var quality = GetTreatmentQuality(wounds, index);
        var cleanup = index < wounds.Cleanup.Count ? wounds.Cleanup[index] : WoundCleanupFlags.None;

        var details = new List<string>
        {
            Color($"{DescribeDetailedSize(size)} {DescribeMechanism(mechanism, wound.Type)}", WoundColorFor(mechanism, wound.Type)),
            Color(
                DescribeTreatment(quality, wound.Treated),
                TreatmentColorFor(quality, wound.Treated)),
        };

        var cleanupText = quality == WoundTreatmentQuality.Adequate
            ? DescribeCleanup(cleanup)
            : string.Empty;
        if (cleanupText.Length > 0)
            details.Add(Color(cleanupText, DetailedCleanupColor));

        var optimalHint = DescribeOptimalHint(mechanism, wound.Type, cleanup);
        if (quality != WoundTreatmentQuality.Optimal && optimalHint.Length > 0)
            details.Add(Color($"optimal: {optimalHint}", DetailedHintColor));

        return ToDetailedLines(details);
    }

    private static bool IsWoundTreatedForExamine(BodyPartWoundComponent wounds, int index)
    {
        return wounds.Wounds[index].Treated || GetTreatmentQuality(wounds, index) != WoundTreatmentQuality.Untreated;
    }

    private static WoundTreatmentQuality GetTreatmentQuality(BodyPartWoundComponent wounds, int index)
    {
        return index < wounds.TreatmentQualities.Count
            ? wounds.TreatmentQualities[index]
            : WoundTreatmentQuality.Untreated;
    }

    private static string ToDetailedLines(List<string> sections)
    {
        return string.Join("\n  ", sections);
    }

    private static string PartHeader(BodyPartType type, BodyPartSymmetry symmetry)
    {
        return $"[bold]{Color(FormatPartName(type, symmetry), DetailedPartColor)}[/bold]";
    }

    private static string Color(string text, string color)
    {
        return $"[color={color}]{text}[/color]";
    }

    private static string WoundColorFor(WoundMechanism mechanism, WoundType type)
    {
        if (mechanism == WoundMechanism.Burn || type == WoundType.Burn)
            return DetailedBurnColor;

        return DetailedWoundColor;
    }

    private static string TreatmentColorFor(WoundTreatmentQuality quality, bool treated)
    {
        return quality switch
        {
            WoundTreatmentQuality.Optimal => DetailedOptimalColor,
            WoundTreatmentQuality.Adequate => DetailedAdequateColor,
            _ => treated ? TreatedWoundColor : DetailedUntreatedColor,
        };
    }

    private static string DescribeDetailedFracture(FractureSeverity severity, bool stabilized)
    {
        var prefix = stabilized ? "stabilized " : string.Empty;
        return severity switch
        {
            FractureSeverity.Hairline => $"{prefix}hairline fracture",
            FractureSeverity.Simple => $"{prefix}simple fracture",
            FractureSeverity.Compound => $"{prefix}compound fracture",
            FractureSeverity.Comminuted => $"{prefix}comminuted fracture",
            _ => "fracture",
        };
    }

    private static string DescribeDetailedSize(WoundSize size) => size switch
    {
        WoundSize.Small => "small",
        WoundSize.Deep => "deep",
        WoundSize.Gaping => "gaping",
        WoundSize.Massive => "massive",
        _ => "deep",
    };

    private static string DescribeMechanism(WoundMechanism mechanism, WoundType type) => mechanism switch
    {
        WoundMechanism.Bullet => "bullet wound",
        WoundMechanism.Stab => "stab wound",
        WoundMechanism.Slash => "slash wound",
        WoundMechanism.Crush => "crush wound",
        WoundMechanism.Burn => "burn",
        WoundMechanism.Blast => "blast wound",
        WoundMechanism.Fragment => "fragment wound",
        WoundMechanism.Surgical => "surgical wound",
        _ => type == WoundType.Burn ? "burn" : "wound",
    };

    private static string DescribeTreatment(WoundTreatmentQuality quality, bool treated) => quality switch
    {
        WoundTreatmentQuality.Optimal => "optimal treatment",
        WoundTreatmentQuality.Adequate => "adequate treatment",
        _ => treated ? "treated" : "untreated",
    };

    private static string DescribeCleanup(WoundCleanupFlags cleanup)
    {
        if (cleanup == WoundCleanupFlags.None)
            return string.Empty;

        var entries = new List<string>(4);
        if ((cleanup & WoundCleanupFlags.RetainedFragment) != WoundCleanupFlags.None)
            entries.Add("retained fragments");
        if ((cleanup & WoundCleanupFlags.PoorClosure) != WoundCleanupFlags.None)
            entries.Add("poor closure");
        if ((cleanup & WoundCleanupFlags.CharredTissue) != WoundCleanupFlags.None)
            entries.Add("charred tissue");
        if ((cleanup & WoundCleanupFlags.CrushDebris) != WoundCleanupFlags.None)
            entries.Add("crush debris");
        if ((cleanup & WoundCleanupFlags.DirtyDressing) != WoundCleanupFlags.None)
            entries.Add("dirty dressing");

        return $"cleanup needed: {ToSentence(entries)}";
    }

    private static string DescribeOptimalHint(WoundMechanism mechanism, WoundType type, WoundCleanupFlags cleanup)
    {
        if ((cleanup & WoundCleanupFlags.RetainedFragment) != WoundCleanupFlags.None)
            return "remove shrapnel";
        if ((cleanup & WoundCleanupFlags.PoorClosure) != WoundCleanupFlags.None)
            return "sealing dressing";
        if ((cleanup & WoundCleanupFlags.CharredTissue) != WoundCleanupFlags.None)
            return "burn gel dressing";
        if ((cleanup & WoundCleanupFlags.CrushDebris) != WoundCleanupFlags.None)
            return "compression dressing";

        return mechanism switch
        {
            WoundMechanism.Bullet or WoundMechanism.Stab or WoundMechanism.Fragment => "hemostatic dressing",
            WoundMechanism.Slash or WoundMechanism.Surgical => "sealing dressing",
            WoundMechanism.Crush or WoundMechanism.Blast => "compression dressing",
            WoundMechanism.Burn => "burn gel dressing",
            _ when type == WoundType.Burn => "burn gel dressing",
            _ when (cleanup & WoundCleanupFlags.DirtyDressing) != WoundCleanupFlags.None => "antiseptic dressing",
            _ => string.Empty,
        };
    }

    private static string DescribeBleedTier(ExternalBleedTier tier) => tier switch
    {
        ExternalBleedTier.Minor => "minor",
        ExternalBleedTier.Moderate => "moderate",
        ExternalBleedTier.Severe => "severe",
        ExternalBleedTier.Arterial => "arterial",
        _ => "none",
    };

    private static WoundMechanism LegacyMechanismFor(WoundType type) => type switch
    {
        WoundType.Burn => WoundMechanism.Burn,
        WoundType.Surgery => WoundMechanism.Surgical,
        _ => WoundMechanism.Generic,
    };

    private string FormatPartName(BodyPartType type, BodyPartSymmetry symmetry) // RuMC edit
    {
        var key = (type, symmetry) switch
        {
            (BodyPartType.Head, _) => "cmu-medical-examine-part-head",
            (BodyPartType.Torso, _) => "cmu-medical-examine-part-torso",
            (BodyPartType.Arm, BodyPartSymmetry.Left) => "cmu-medical-examine-part-arm-left",
            (BodyPartType.Arm, BodyPartSymmetry.Right) => "cmu-medical-examine-part-arm-right",
            (BodyPartType.Hand, BodyPartSymmetry.Left) => "cmu-medical-examine-part-hand-left",
            (BodyPartType.Hand, BodyPartSymmetry.Right) => "cmu-medical-examine-part-hand-right",
            (BodyPartType.Leg, BodyPartSymmetry.Left) => "cmu-medical-examine-part-leg-left",
            (BodyPartType.Leg, BodyPartSymmetry.Right) => "cmu-medical-examine-part-leg-right",
            (BodyPartType.Foot, BodyPartSymmetry.Left) => "cmu-medical-examine-part-foot-left",
            (BodyPartType.Foot, BodyPartSymmetry.Right) => "cmu-medical-examine-part-foot-right",
            _ => null,
        };

        return key != null ? Loc.GetString(key) : type.ToString();
    }

    private static int BodyPartSortOrder(BodyPartType type, BodyPartSymmetry symmetry)
    {
        return type switch
        {
            BodyPartType.Head => 0,
            BodyPartType.Torso => 10,
            BodyPartType.Arm when symmetry == BodyPartSymmetry.Left => 20,
            BodyPartType.Hand when symmetry == BodyPartSymmetry.Left => 21,
            BodyPartType.Arm when symmetry == BodyPartSymmetry.Right => 30,
            BodyPartType.Hand when symmetry == BodyPartSymmetry.Right => 31,
            BodyPartType.Leg when symmetry == BodyPartSymmetry.Left => 40,
            BodyPartType.Foot when symmetry == BodyPartSymmetry.Left => 41,
            BodyPartType.Leg when symmetry == BodyPartSymmetry.Right => 50,
            BodyPartType.Foot when symmetry == BodyPartSymmetry.Right => 51,
            _ => 100 + ((int) type * 10) + SymmetrySortOrder(symmetry),
        };
    }

    private static int SymmetrySortOrder(BodyPartSymmetry symmetry)
    {
        return symmetry switch
        {
            BodyPartSymmetry.Left => 0,
            BodyPartSymmetry.None => 1,
            BodyPartSymmetry.Right => 2,
            _ => 3,
        };
    }

    private string ToSentence(List<string> parts) // RuMC edit
    {
        return parts.Count switch
        {
            0 => string.Empty,
            1 => parts[0],
            2 => $"{parts[0]} {Loc.GetString("cmu-medical-examine-list-and")} {parts[1]}",
            _ => Loc.GetString(
                "cmu-medical-examine-list-comma-and",
                ("list", string.Join(", ", parts.GetRange(0, parts.Count - 1))),
                ("last", parts[^1])),
        };
    }

    private static string ToSemicolonList(List<string> parts)
    {
        return string.Join("; ", parts);
    }

    private readonly record struct BodyPartExamineSummary(int Order, string Part, string Conditions);
}

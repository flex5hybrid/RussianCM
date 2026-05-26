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
using Robust.Shared.Timing;
using Robust.Shared.Localization; // RuMC edit

namespace Content.Shared._CMU14.Medical.Examine;

public sealed partial class CMUMedicalExamineSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    private const string UntreatedWoundColor = "#ff4d4d";
    private const string TreatedWoundColor = "#7bd88f";
    private const string FractureColor = "#dca94c";
    private const string SeveredColor = "#ff4d4d";

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
        var now = _timing.CurTime;
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
                        var wound = wounds.Wounds[i];
                        var size = i < wounds.Sizes.Count ? wounds.Sizes[i] : WoundSize.Deep;
                        if (wound.Treated)
                            treated.Add(DescribeWound(wound, size, now));
                        else
                            untreated.Add(DescribeWound(wound, size, now));
                    }
                }

                if (HasComp<CMUEscharComponent>(partUid))
                    untreated.Add(Loc.GetString("cmu-medical-examine-eschar")); // RuMC edit

                if (untreated.Count > 0)
                    sections.Add($"[color={UntreatedWoundColor}]{ToSentence(untreated)}[/color]");

                if (treated.Count > 0)
                    sections.Add($"[color={TreatedWoundColor}]{ToSentence(treated)}[/color]");
            }

            if (includeFractures
                && TryComp<FractureComponent>(partUid, out var fracture)
                && fracture.Severity != FractureSeverity.None)
            {
                var stabilized = HasComp<CMUSplintedComponent>(partUid) || HasComp<CMUCastComponent>(partUid);
                sections.Add($"[color={FractureColor}]{DescribeFracture(fracture.Severity, stabilized)}[/color]");
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

    private string DescribeWound(Wound wound, WoundSize size, TimeSpan now)  // RuMC edit
    {
        var sizeKey = size switch
        {
            WoundSize.Small   => "cmu-medical-examine-wound-size-small",
            WoundSize.Deep    => "cmu-medical-examine-wound-size-deep",
            WoundSize.Gaping  => "cmu-medical-examine-wound-size-gaping",
            WoundSize.Massive => "cmu-medical-examine-wound-size-massive",
            _                 => "cmu-medical-examine-wound-size-deep",
        };

        var typeKey = wound.Type switch
        {
            WoundType.Burn    => "cmu-medical-examine-wound-type-burn",
            WoundType.Surgery => "cmu-medical-examine-wound-type-surgery",
            _                 => "cmu-medical-examine-wound-type-trauma",
        };

        var treated = wound.Treated
            ? Loc.GetString("cmu-medical-examine-wound-treated-prefix") + " "
            : string.Empty;

        var bleeding = !wound.Treated
            && wound.Bloodloss > 0f
            && (wound.StopBleedAt is null || now < wound.StopBleedAt.Value)
                ? " " + Loc.GetString("cmu-medical-examine-wound-bleeding-suffix")
                : string.Empty;

        return $"{treated}{Loc.GetString(sizeKey)} {Loc.GetString(typeKey)}{bleeding}";
    }

    private string DescribeFracture(FractureSeverity severity, bool stabilized) // RuMC edit
    {
        var key = severity switch
        {
            FractureSeverity.Hairline   => "cmu-medical-examine-fracture-hairline",
            FractureSeverity.Simple     => "cmu-medical-examine-fracture-simple",
            FractureSeverity.Compound   => "cmu-medical-examine-fracture-compound",
            FractureSeverity.Comminuted => "cmu-medical-examine-fracture-comminuted",
            _                           => "cmu-medical-examine-fracture-simple",
        };
        return Loc.GetString(key, ("stabilized", stabilized ? "true" : "false"));
    }

    private string FormatPartName(BodyPartType type, BodyPartSymmetry symmetry) // RuMC edit
    {
        var key = (type, symmetry) switch
        {
            (BodyPartType.Head,  _)                        => "cmu-medical-examine-part-head",
            (BodyPartType.Torso, _)                        => "cmu-medical-examine-part-torso",
            (BodyPartType.Arm,   BodyPartSymmetry.Left)    => "cmu-medical-examine-part-arm-left",
            (BodyPartType.Arm,   BodyPartSymmetry.Right)   => "cmu-medical-examine-part-arm-right",
            (BodyPartType.Hand,  BodyPartSymmetry.Left)    => "cmu-medical-examine-part-hand-left",
            (BodyPartType.Hand,  BodyPartSymmetry.Right)   => "cmu-medical-examine-part-hand-right",
            (BodyPartType.Leg,   BodyPartSymmetry.Left)    => "cmu-medical-examine-part-leg-left",
            (BodyPartType.Leg,   BodyPartSymmetry.Right)   => "cmu-medical-examine-part-leg-right",
            (BodyPartType.Foot,  BodyPartSymmetry.Left)    => "cmu-medical-examine-part-foot-left",
            (BodyPartType.Foot,  BodyPartSymmetry.Right)   => "cmu-medical-examine-part-foot-right",
            _                                              => null,
        };

        return key != null ? Loc.GetString(key) : type.ToString();
    }

    private static int BodyPartSortOrder(BodyPartType type, BodyPartSymmetry symmetry)
    {
        return type switch
        {
            BodyPartType.Head => 0,
            BodyPartType.Arm when symmetry == BodyPartSymmetry.Left => 10,
            BodyPartType.Hand when symmetry == BodyPartSymmetry.Left => 11,
            BodyPartType.Torso => 20,
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
            _ => $"{string.Join(", ", parts.GetRange(0, parts.Count - 1))} {Loc.GetString("cmu-medical-examine-list-and")} {parts[parts.Count - 1]}",
        };
    }

    private static string ToSemicolonList(List<string> parts)
    {
        return string.Join("; ", parts);
    }

    private readonly record struct BodyPartExamineSummary(int Order, string Part, string Conditions);
}

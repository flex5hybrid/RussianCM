using System;
using System.Collections.Generic;
using Content.Shared._CMU14.Medical;
using Content.Shared._CMU14.Medical.Bones;
using Content.Shared._CMU14.Medical.Items;
using Content.Shared._CMU14.Medical.Wounds;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Examine;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared._CMU14.Medical.Examine;

public sealed class CMUMedicalExamineSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    private const string UntreatedWoundColor = "#ff4d4d";
    private const string TreatedWoundColor = "#7bd88f";
    private const string FractureColor = "#dca94c";

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
                _cfg.GetCVar(CMUMedicalCCVars.BoneEnabled));
        }
    }

    private void AddBodyPartLines(EntityUid body, ExaminedEvent args, bool includeWounds, bool includeFractures)
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
                    untreated.Add(Loc.GetString("cmu-medical-examine-eschar")); // RuCM edit localization key

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

        partSummaries.Sort((a, b) => a.Order.CompareTo(b.Order));

        foreach (var summary in partSummaries)
        {
            args.PushMarkup(Loc.GetString(
                "cmu-medical-examine-body-part-line",
                ("part", summary.Part),
                ("conditions", summary.Conditions)));
        }
    }

    private string DescribeWound(Wound wound, WoundSize size, TimeSpan now) // RuCM edit localization keys
    {
        var sizeKey = size switch
        {
            WoundSize.Small => "small",
            WoundSize.Deep => "deep",
            WoundSize.Gaping => "gaping",
            WoundSize.Massive => "massive",
            _ => "deep",
        };

        var kindKey = wound.Type switch
        {
            WoundType.Burn => "burn",
            WoundType.Surgery => "surgery",
            _ => "trauma",
        };

        var isBleeding = !wound.Treated
            && wound.Bloodloss > 0f
            && (wound.StopBleedAt is null || now < wound.StopBleedAt.Value);

        return Loc.GetString("cmu-medical-examine-wound-describe",
            ("size", sizeKey),
            ("kind", kindKey),
            ("treated", wound.Treated),
            ("bleeding", isBleeding));
    }

    private string DescribeFracture(FractureSeverity severity, bool stabilized) // RuCM edit localization keys
    {
        var severityKey = severity switch
        {
            FractureSeverity.Hairline => "hairline",
            FractureSeverity.Simple => "simple",
            FractureSeverity.Compound => "compound",
            FractureSeverity.Comminuted => "comminuted",
            _ => "simple",
        };

        return Loc.GetString("cmu-medical-examine-fracture-describe",
            ("severity", severityKey),
            ("stabilized", stabilized));
    }

    private string FormatPartName(BodyPartType type, BodyPartSymmetry symmetry) // RuCM edit localization keys
    {
        var typeKey = type.ToString().ToLowerInvariant();
        var symmetryKey = symmetry switch
        {
            BodyPartSymmetry.Left => "left",
            BodyPartSymmetry.Right => "right",
            _ => "none",
        };

        return Loc.GetString("cmu-medical-examine-part-name",
            ("type", typeKey),
            ("symmetry", symmetryKey));
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

    private string ToSentence(List<string> parts) // RuCM edit localization keys
    {
        return parts.Count switch
        {
            0 => string.Empty,
            1 => parts[0],
            2 => Loc.GetString("cmu-medical-examine-sentence-two",
                    ("a", parts[0]), ("b", parts[1])),
            _ => Loc.GetString("cmu-medical-examine-sentence-many",
                    ("list", string.Join(", ", parts.GetRange(0, parts.Count - 1))),
                    ("last", parts[parts.Count - 1])),
        };
    }

    private static string ToSemicolonList(List<string> parts)
    {
        return string.Join("; ", parts);
    }

    private readonly record struct BodyPartExamineSummary(int Order, string Part, string Conditions);
}

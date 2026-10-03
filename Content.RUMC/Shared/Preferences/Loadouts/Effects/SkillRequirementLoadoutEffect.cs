using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

// A loadout with this effect can only be chosen if the character has the skill at this level.
public sealed partial class SkillRequirementLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public EntProtoId<SkillDefinitionComponent> Skill;

    [DataField(required: true)]
    public int MinLevel;

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var prototypeManager = collection.Resolve<IPrototypeManager>();

        var sources = profile.Loadouts.Values.Append(loadout);

        var level = sources
            .SelectMany(source => source.SelectedLoadouts.Values)
            .SelectMany(selected => selected)
            .Where(selected => prototypeManager.TryIndex(selected.Prototype, out _))
            .SelectMany(selected => prototypeManager.Index(selected.Prototype).Effects.OfType<SetSkillLoadoutEffect>())
            .Where(effect => effect.Skill == Skill)
            .Select(effect => effect.Level)
            .DefaultIfEmpty(0)
            .Max();

        if (level >= MinLevel)
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromUnformatted(Loc.GetString("loadouts-skill-requirement-not-met",
            ("skill", prototypeManager.TryIndex(Skill, out var skillProto) ? skillProto.Name : Skill.Id),
            ("level", MinLevel)));
        return false;
    }
}

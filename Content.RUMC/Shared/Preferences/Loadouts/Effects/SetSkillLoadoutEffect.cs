using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

// Gives the character a skill at a fixed level when they spawn.
public sealed partial class SetSkillLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public EntProtoId<SkillDefinitionComponent> Skill;

    [DataField(required: true)]
    public int Level;

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var prototypeManager = collection.Resolve<IPrototypeManager>();
        if (prototypeManager.TryIndex(Skill, out var skillPrototype) &&
            skillPrototype.TryComp<SkillDefinitionComponent>(out _, collection.Resolve<IComponentFactory>()))
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromUnformatted(Loc.GetString("loadouts-skill-upgrade-invalid"));
        return false;
    }

    public override void ApplyToEntity(EntityUid entity, IEntityManager entityManager, IPrototypeManager prototypeManager)
    {
        entityManager.System<SkillsSystem>().SetSkill(entity, Skill, Level);
    }
}

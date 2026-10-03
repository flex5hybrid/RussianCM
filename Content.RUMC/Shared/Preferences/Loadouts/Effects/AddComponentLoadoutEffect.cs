using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

public sealed partial class AddComponentLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        return true;
    }

    public override void ApplyToEntity(EntityUid entity, IEntityManager entityManager, IPrototypeManager prototypeManager)
    {
        entityManager.AddComponents(entity, Components, removeExisting: false);
    }
}

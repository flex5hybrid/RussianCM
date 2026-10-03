using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

// Marks a loadout as a free clothing choice for one inventory slot.
public sealed partial class CustomClothingLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public string Slot = string.Empty;

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
}

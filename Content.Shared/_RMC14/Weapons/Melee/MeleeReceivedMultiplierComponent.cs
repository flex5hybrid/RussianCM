using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCMeleeWeaponSystem))]
public sealed partial class MeleeReceivedMultiplierComponent : Component
{
    /// <summary>
    /// When set, xeno melee damage is *replaced* by this spec (legacy behaviour, used by
    /// resin walls etc.). When null, the incoming damage is scaled by <see cref="XenoMultiplier"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? XenoDamage; // TODO RMC14 other hives

    /// <summary>
    /// Multiplier applied to xeno melee damage when <see cref="XenoDamage"/> is null.
    /// Lets targets scale xeno damage (e.g. "tank takes 3x damage from xenos") without
    /// hard-coding per-type numbers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 XenoMultiplier = FixedPoint2.New(1);

    [DataField, AutoNetworkedField]
    public FixedPoint2 OtherMultiplier = FixedPoint2.New(1);
}

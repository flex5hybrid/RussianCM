using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Medieval.Magic.Mana;


[RegisterComponent, NetworkedComponent]
public sealed partial class ManaDrainSpellComponent : Component
{
    [DataField(required: true)]
    public float ManaDrain;

    [DataField]
    public bool CanUseWithoutMana = false;

    [DataField]
    public LocId ManaLowMessage = "medieval-mana-not-enough-mana";

    [DataField]
    public DamageSpecifier DamageOnUseWithoutMana = new();
}

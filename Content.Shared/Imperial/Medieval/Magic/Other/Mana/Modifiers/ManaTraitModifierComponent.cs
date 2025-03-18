namespace Content.Shared.Imperial.Medieval.Magic.Mana;


[RegisterComponent]
public sealed partial class ManaTraitModifierComponent : Component
{
    [DataField]
    public float RegenTraitModifier = 1f;

    [DataField]
    public float MaxManaTraitModifier = 1f;
}

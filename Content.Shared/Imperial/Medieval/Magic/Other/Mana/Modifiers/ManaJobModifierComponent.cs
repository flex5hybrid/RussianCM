namespace Content.Shared.Imperial.Medieval.Magic.Mana;


[RegisterComponent]
public sealed partial class ManaJobModifierComponent : Component
{
    [DataField]
    public float RegenJobModifier = 1f;
    [DataField]
    public float MaxManaJobModifier = 1f;
}

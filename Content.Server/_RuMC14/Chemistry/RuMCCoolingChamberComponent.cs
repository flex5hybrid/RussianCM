namespace Content.Server._RuMC14.Chemistry;

[RegisterComponent]
public sealed partial class RuMCCoolingChamberComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CoolPerSecond = 160f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TargetTemperature = 277.15f;
}

[RegisterComponent]
public sealed partial class ActiveRuMCCoolingChamberComponent : Component
{
}

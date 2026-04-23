namespace Content.Server._RuMC14.Chemistry;

[RegisterComponent]
public sealed partial class RuMCCoolingChamberComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CoolPerSecond = 160f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TargetTemperature = 0.15f;

    // Пропускаем один тик после закрытия, чтобы PVS успел обработать переход сущностей в контейнер
    public bool SkipNextTick;
}

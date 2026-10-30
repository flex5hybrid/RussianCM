using Robust.Shared.GameStates;

namespace Content.Shared._RuMC14.BatteryDrain;

/// <summary>Drains the item's own battery while it is active.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RuMCBatteryDrainComponent : Component
{
    /// <summary>Fraction of the maximum charge needed to turn the item on.</summary>
    [DataField]
    public float MinActivationCharge = 0.1f;

    [DataField]
    public LocId NoChargeMessage = "rumc-battery-drain-no-charge";
}

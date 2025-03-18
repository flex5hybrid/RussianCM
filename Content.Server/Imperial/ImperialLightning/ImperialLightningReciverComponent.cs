namespace Content.Server.Imperial.ImperialLightning;


[RegisterComponent]
public sealed partial class ImperialLightningReciverComponent : Component
{
    /// <summary>
    /// Entities with <see cref="ImperialLightningSenderComponent"/> and the same <see cref="ImperialLightningSenderComponent.SendingFrequency" /> field will be connected by lightning
    /// </summary>
    [DataField(required: true)]
    public string ReceiptFrequency = "";
}

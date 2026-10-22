namespace Content.Shared.CMU14.Marines.Orders;

/// <summary>
/// Grants access to the silence order in the shared military-order selector.
/// </summary>
[RegisterComponent]
public sealed partial class AU14SilenceOrderAbilityComponent : Component
{
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long targets remain whisper-only.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Radius of the effect in tiles.
    /// </summary>
    [DataField]
    public float Range = 7f;

}

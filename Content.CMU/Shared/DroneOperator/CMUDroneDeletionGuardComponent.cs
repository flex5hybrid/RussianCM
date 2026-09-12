namespace Content.Shared.CMU14.DroneOperator;

/// <summary>
/// Marks drone entities whose normal deletion must not be treated as a gameplay death.
/// </summary>
[RegisterComponent]
public sealed partial class CMUDroneDeletionGuardComponent : Component;

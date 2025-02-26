using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialStore;


/// <summary>
///     Used when the refund button is pressed
/// </summary>
[Serializable, NetSerializable]
public sealed class ImperialStoreRequestRefundMessage : BoundUserInterfaceMessage;

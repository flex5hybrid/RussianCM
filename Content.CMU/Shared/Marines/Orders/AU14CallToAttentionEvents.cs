using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Marines.Orders;

public sealed partial class AU14CallToAttentionActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum AU14MilitaryOrder : byte
{
    Silence,
    Salute,
    Attention,
}

[Serializable, NetSerializable]
public enum AU14MilitaryOrderUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class AU14MilitaryOrderSelectedBuiMsg(AU14MilitaryOrder order) : BoundUserInterfaceMessage
{
    public readonly AU14MilitaryOrder Order = order;
}

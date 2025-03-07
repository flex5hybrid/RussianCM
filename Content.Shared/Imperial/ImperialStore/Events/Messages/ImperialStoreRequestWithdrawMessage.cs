using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialStore;


[Serializable, NetSerializable]
public sealed class ImperialStoreRequestWithdrawMessage(string currency, int amount) : BoundUserInterfaceMessage
{
    public string Currency = currency;

    public int Amount = amount;
}

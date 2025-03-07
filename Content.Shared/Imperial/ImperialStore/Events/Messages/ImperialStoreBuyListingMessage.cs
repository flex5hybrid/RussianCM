using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialStore;


[Serializable, NetSerializable]
public sealed class ImperialStoreBuyListingMessage(ImperialListingData listing) : BoundUserInterfaceMessage
{
    public ImperialListingData Listing = listing;
}

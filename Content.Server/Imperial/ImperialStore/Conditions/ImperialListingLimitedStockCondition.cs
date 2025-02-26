using Content.Shared.Imperial.ImperialStore;

namespace Content.Server.Imperial.ImperialStore;

/// <summary>
/// Only allows a listing to be purchased a certain amount of times.
/// </summary>
public sealed partial class ImperialListingLimitedStockCondition : ImperialListingCondition
{
    /// <summary>
    /// The amount of times this listing can be purchased.
    /// </summary>
    [DataField("stock", required: true)]
    public int Stock;

    public override bool Condition(ImperialListingConditionArgs args)
    {
        return args.Listing.PurchaseAmount < Stock;
    }
}

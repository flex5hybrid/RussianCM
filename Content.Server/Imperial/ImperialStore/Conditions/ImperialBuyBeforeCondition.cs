using Content.Shared.Imperial.ImperialStore;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.ImperialStore;

public sealed partial class ImperialBuyBeforeCondition : ImperialListingCondition
{
    /// <summary>
    ///     Required listing(s) needed to purchase before this listing is available
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ImperialListingPrototype>>? Whitelist;

    /// <summary>
    ///     Listing(s) that if bought, block this purchase, if any.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ImperialListingPrototype>>? Blacklist;

    /// <summary>
    /// Unjustified types of purchases that the user must have before opening this purchase
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ImperialListingPrototype>>? RequiredPurchases;

    public override bool Condition(ImperialListingConditionArgs args)
    {
        if (!args.EntityManager.TryGetComponent<ImperialStoreComponent>(args.StoreEntity, out var storeComp))
            return false;

        var allListings = storeComp.Listings;

        if (!CheckBlacklist(allListings)) return false;
        if (!CheckWhitelist(allListings)) return false;
        if (!CheckRequiredPurchases(allListings)) return false;

        return true;
    }

    #region Helpers

    private bool CheckBlacklist(HashSet<ImperialListingData> alllListings)
    {
        if (Blacklist == null) return true;

        foreach (var blacklistListing in Blacklist)
        {
            foreach (var listing in alllListings)
            {
                if (listing.ID == blacklistListing.Id && listing.PurchaseAmount > 0)
                    return false;
            }
        }

        return true;
    }

    private bool CheckWhitelist(HashSet<ImperialListingData> alllListings)
    {
        if (Whitelist == null) return true;

        foreach (var requiredListing in Whitelist)
        {
            foreach (var listing in alllListings)
            {
                if (listing.ID == requiredListing.Id && listing.PurchaseAmount > 0)
                    return true;
            }
        }

        return false;
    }

    private bool CheckRequiredPurchases(HashSet<ImperialListingData> alllListings)
    {
        if (RequiredPurchases == null) return true;

        var foundedPurchasesCount = 0;

        foreach (var requiredListing in RequiredPurchases)
        {
            foreach (var listing in alllListings)
            {
                if (listing.ID == requiredListing.Id && listing.PurchaseAmount > 0)
                    foundedPurchasesCount += 1;
            }
        }

        return foundedPurchasesCount == RequiredPurchases.Count;
    }

    #endregion
}

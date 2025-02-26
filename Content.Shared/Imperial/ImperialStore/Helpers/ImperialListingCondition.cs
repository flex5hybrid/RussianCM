using JetBrains.Annotations;

namespace Content.Shared.Imperial.ImperialStore;

/// <summary>
/// Used to define a complicated condition that requires C#
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class ImperialListingCondition
{
    /// <summary>
    /// Determines whether or not a certain entity can purchase a listing.
    /// </summary>
    /// <returns>Whether or not the listing can be purchased</returns>
    public abstract bool Condition(ImperialListingConditionArgs args);
}

/// <param name="Buyer">Either the account owner, user, or an inanimate object (e.g., surplus bundle)</param>
/// <param name="Listing">The listing itself</param>
/// <param name="EntityManager">An entitymanager for sane coding</param>
public readonly record struct ImperialListingConditionArgs(EntityUid Buyer, EntityUid? StoreEntity, ImperialListingData Listing, IEntityManager EntityManager);

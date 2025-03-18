namespace Content.Server.Imperial.ImperialStore;

/// <summary>
///     Keeps track of entities bought from stores for refunds, especially useful if entities get deleted before they can be refunded.
/// </summary>
[RegisterComponent, Access(typeof(ImperialStoreSystem))]
public sealed partial class ImperialStoreRefundComponent : Component
{
    [ViewVariables, DataField]
    public EntityUid? StoreEntity;
}

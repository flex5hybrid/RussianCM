namespace Content.Shared.Imperial.ImperialStore;


/// <summary>
///     Broadcast when an Entity with the <see cref="ImperialStoreRefundComponent"/> is deleted
/// </summary>
[ByRefEvent]
public readonly struct ImperialRefundEntityDeletedEvent
{
    public EntityUid Uid { get; }

    public ImperialRefundEntityDeletedEvent(EntityUid uid)
    {
        Uid = uid;
    }
}

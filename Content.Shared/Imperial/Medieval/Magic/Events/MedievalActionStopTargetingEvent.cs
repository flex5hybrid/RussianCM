namespace Content.Shared.Imperial.Medieval.Magic.Overlays;


[Serializable]
public sealed partial class MedievalActionStopTargetingEvent : EntityEventArgs
{
    public EntityUid ActionOwner;

    public EntityUid Action;
}

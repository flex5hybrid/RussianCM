using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic.Overlays;


[Serializable]
public sealed partial class MedievalActionStartTargetingEvent : EntityEventArgs
{
    public EntityUid ActionOwner;

    public EntityUid Action;
}

using System.ComponentModel;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.TargetOverlay.Events;


[Serializable, NetSerializable]
public sealed class AddTargetOverlayEvent : EntityEventArgs
{
    public NetEntity Player;

    public NetEntity? Sender;

    public HashSet<string> WhiteListComponents = new();

    public HashSet<string> BlackListComponents = new();

    public int MaxTargetCount = 1;
}

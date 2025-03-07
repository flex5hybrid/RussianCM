using Robust.Shared.Map;

namespace Content.Client.Imperial.TargetOverlay;


[RegisterComponent]
public sealed partial class TargetOverlayComponent : Component
{
    [DataField]
    public int MaxTargetCount = 1;


    [DataField]
    public HashSet<Type> WhiteListComponents = new();

    [DataField]
    public HashSet<Type> BlackListComponents = new();

    [ViewVariables]
    public EntityUid? Sender;

}

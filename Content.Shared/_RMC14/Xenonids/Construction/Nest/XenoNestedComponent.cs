using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Construction.Nest;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoNestSystem))]
public sealed partial class XenoNestedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Nest;

    [DataField, AutoNetworkedField]
    public bool Detached;

    [DataField]
    public float IncubationMultiplier = 1.5f;

    [DataField, AutoNetworkedField]
    public NetUserId? GhostedId;

    [DataField]
    public TimeSpan BreakoutTime = TimeSpan.FromMinutes(7);

    [DataField]
    public TimeSpan StruggleMessageInterval = TimeSpan.FromSeconds(30);

    [DataField]
    public DoAfterId? BreakoutDoAfter;

    [DataField]
    public TimeSpan NextStruggleMessage;
}

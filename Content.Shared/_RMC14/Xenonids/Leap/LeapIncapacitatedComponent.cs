using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LeapIncapacitatedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan RecoverAt;
}

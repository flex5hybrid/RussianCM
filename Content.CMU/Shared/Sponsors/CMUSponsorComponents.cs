using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.Sponsors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CMUSponsorGhostComponent : Component
{
    [DataField, AutoNetworkedField]
    public string State = "animated";
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CMUSponsorCapeComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Trim = "";

    [DataField, AutoNetworkedField]
    public string Emblem = "";
}

[RegisterComponent]
public sealed partial class CMUSponsorEngravingComponent : Component
{
    [DataField]
    public string Text = "";
}

using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.Cassette;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUDynamicCassetteComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? OwnerName;

    [DataField, AutoNetworkedField]
    public List<Guid> Tracks = new();

    [DataField, AutoNetworkedField]
    public List<string> Titles = new();

    [DataField]
    public int MaxTracks = CMUDynamicCassetteLimits.MaxTracksPerCassette;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CMUPublicCassettePlayerComponent : Component;

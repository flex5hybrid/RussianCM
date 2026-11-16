using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.Cassette;

/// <summary>
/// Помечает кассету как поддерживающую загруженные в текущем раунде треки. /
/// Marks a cassette as able to receive tracks uploaded during the current round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUDynamicCassetteComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<Guid> Tracks = new();

    [DataField, AutoNetworkedField]
    public List<string> Titles = new();

    [DataField]
    public int MaxTracks = CMUDynamicCassetteLimits.MaxTracksPerCassette;
}

/// <summary>
/// Включает пространственное воспроизведение кассетного плеера для находящихся рядом клиентов. /
/// Makes a cassette player emit spatial audio to nearby clients.
/// Плееры без этого компонента сохраняют приватный режим наушников. /
/// Players without this component retain private headphone playback.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CMUPublicCassettePlayerComponent : Component;

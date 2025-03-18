using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.Minigames;


/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Serializable]
public sealed partial class InMinigameComponent : MinigameComponent
{
    [ViewVariables]
    public MinigameData? ActiveMinigame;

    [ViewVariables]
    public List<EntityUid> OtherPlayers = new();

    [ViewVariables]
    public TimeSpan FirstMinigameStartTime = TimeSpan.Zero;

    [ViewVariables]
    public float MinigameWonPrecent = 0.0f;
}

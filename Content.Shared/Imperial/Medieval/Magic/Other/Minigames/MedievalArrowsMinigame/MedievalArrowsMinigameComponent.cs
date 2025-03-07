using Content.Shared.Imperial.Minigames;
using Robust.Shared.GameStates;


namespace Content.Shared.Imperial.Medieval.Magic.Minigames;


[RegisterComponent, NetworkedComponent, Serializable, AutoGenerateComponentState]
public sealed partial class MedievalArrowsMinigameComponent : MinigameComponent
{
    [DataField, AutoNetworkedField]
    public List<ArrowsTypes> Combination = new();

    [DataField]
    public int BaseArrowsCount = 5;

    [DataField]
    public float ArrowPerDifficulty = 0.1f;

    [DataField]
    public TimeSpan ProbDelay = TimeSpan.FromSeconds(0.5f);


    [ViewVariables]
    public List<ArrowsTypes> PlayerCombination = new();

    [ViewVariables]
    public TimeSpan NextProbTIme = TimeSpan.Zero;

    [ViewVariables]
    public MinigameData CurrentMinigame = default!;
}

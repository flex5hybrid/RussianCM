namespace Content.Shared.Imperial.Minigames;


/// <summary>
///
/// </summary>
[Serializable]
public abstract partial class MinigameComponent : Component
{
    [DataField]
    public float Difficulty = 1.0f;
}

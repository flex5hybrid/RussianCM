namespace Content.Shared.Imperial.Minigames;


public abstract class SharedMinigamesSystem : EntitySystem
{
    #region Public API | Server Only

    public virtual bool TryStartMinigame(EntityUid player, string minigameId)
    {
        return false;
    }
    public virtual bool TryStartMinigame(EntityUid player, MinigamePrototype minigamePrototype)
    {
        return false;
    }

    public virtual bool TryStartMinigameBetween(EntityUid player, EntityUid player2, string minigameId)
    {
        return false;
    }

    public virtual bool TryStartMinigameBetween(EntityUid player, EntityUid player2, MinigamePrototype minigamePrototype)
    {
        return false;
    }

    public virtual bool TryStartMinigameBetween(List<EntityUid> players, string minigameId)
    {
        return false;
    }

    public virtual bool TryStartMinigameBetween(List<EntityUid> players, MinigamePrototype minigamePrototype)
    {
        return false;
    }

    public virtual void AddWonMinigame<T>(EntityUid player, MinigameData minigame, InMinigameComponent? component = null) where T : MinigameComponent
    {
    }

    #endregion
}

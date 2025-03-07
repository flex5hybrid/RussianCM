namespace Content.Shared.Imperial.Minigames.Events;


public sealed partial class AfterMinigameAddedEvent : EventArgs
{
    public MinigamePrototype Minigame = new();

    public EntityUid NewPlayer;
}

using System.ComponentModel;

namespace Content.Shared.Imperial.Minigames.Events;


public sealed partial class BeforeMinigameAddedEvent : CancelEventArgs
{
    public MinigamePrototype Minigame = new();

    public EntityUid NewPlayer;
}

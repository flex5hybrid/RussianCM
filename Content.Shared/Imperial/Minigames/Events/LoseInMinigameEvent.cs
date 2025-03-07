using System.ComponentModel;

namespace Content.Shared.Imperial.Minigames.Events;


public sealed partial class LoseInMinigameEvent : CancelEventArgs
{
    public MinigameData Minigame = new();
}

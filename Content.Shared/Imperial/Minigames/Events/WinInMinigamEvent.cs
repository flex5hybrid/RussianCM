using System.ComponentModel;

namespace Content.Shared.Imperial.Minigames.Events;


public sealed partial class WinInMinigamEvent : CancelEventArgs
{
    public MinigameData Minigame = new();
}

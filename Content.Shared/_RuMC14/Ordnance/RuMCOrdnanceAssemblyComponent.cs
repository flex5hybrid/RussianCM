using Robust.Shared.GameStates;

namespace Content.Shared._RuMC14.Ordnance;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOrdnanceAssemblyComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCOrdnancePartType? LeftPartType;

    [DataField, AutoNetworkedField]
    public RMCOrdnancePartType? RightPartType;

    /// <summary>
    /// Закрыта ли сборка отвёрткой. Только закрытая сборка может быть вставлена в корпус.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsLocked;

    /// <summary>
    /// Задержка в секундах для сенсора Timer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TimerDelay = 5f;
}

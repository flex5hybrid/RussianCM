using Content.Server.Imperial.Revolutionary.UI;

namespace Content.Server.Imperial.Revolutionary.Components;

[RegisterComponent]
public sealed partial class ConsentRevolutionaryComponent : Component
{
    /// <summary>
    /// Другой участник запроса на обращение. Если null, сущность не находится в процессе запроса.
    /// </summary>
    [DataField] public EntityUid? OtherMember;

    /// <summary>
    /// Является ли сущность инициатором обращения.
    /// Если false, значит сущность запрашивает преобразование.
    /// </summary>
    [DataField] public bool IsConverter = false;

    /// <summary>
    /// Окно интерфейса для подтверждения обращения
    /// </summary>
    public ConsentRequestedEui? Window;

    /// <summary>
    /// Время последнего запроса на превращение в революционера
    /// </summary>
    [DataField] public TimeSpan? RequestStartTime;

    /// <summary>
    /// Таймаут для ответа на запрос обращения
    /// </summary>
    [DataField] public TimeSpan ResponseTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Период блокировки повторных запросов после отказа
    /// </summary>
    [DataField] public TimeSpan RequestBlockTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Период блокировки новых запросов после успешного обращения
    /// </summary>
    [DataField] public TimeSpan ConversionBlockTime = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Максимальная дистанция для взаимодействия при запросе
    /// </summary>
    [DataField] public float MaxDistance = 3f;
}

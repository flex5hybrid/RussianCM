namespace Content.Server.Imperial.Revolutionary.Components;

/// <summary>
/// Блокирует возможность преобразования сущности в революционера.
/// Используется для постоянной защиты от конверсии через революционную механику.
/// </summary>
[RegisterComponent]
public sealed partial class ConsentRevolutionaryDenyComponent : Component
{
    /// <summary>
    /// Локализованный ключ сообщения, которое увидит инициатор
    /// при попытке обращения защищенной сущности
    /// </summary>
    [DataField]
    public string OnConversionAttemptText = "rev-consent-convert-failed-convert-block";
}

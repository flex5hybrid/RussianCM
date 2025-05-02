namespace Content.Server.Imperial.Revolutionary.Components;

/// <summary>
/// Временный блокирующий компонент, предотвращающий повторное преобразование сущности в революционера.
/// Добавляется после попытки обращения для ограничения спама и злоупотреблений.
/// </summary>
[RegisterComponent]
public sealed partial class ConsentRevolutionaryCooldownComponent : Component;

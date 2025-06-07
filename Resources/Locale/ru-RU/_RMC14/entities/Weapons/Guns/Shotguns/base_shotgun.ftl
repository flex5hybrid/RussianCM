### Базовые классы ###
ent-RMCBaseWeaponShotgun = Базовый дробовик
ent-RMCBaseWeaponShotgun-desc = Базовый класс для всех дробовиков в игре.

ent-RMCBaseBreechloader = Дробовик с переломным механизмом
ent-RMCBaseBreechloader-desc = Базовый класс для дробовиков с переломным механизмом заряжания.

### Общие характеристики ###
shotgun-features =
    • Высокая остановочная сила
    • Низкая скорость перезарядки
    • Сильная отдача

### Слоты ###
breech-load-slot = Патронник

### Действия ###
gun-breech-open = Затвор открыт
gun-breech-close = Затвор закрыт
gun-wield-delay = Готовность к стрельбе: 0.6с
gun-heavy-penalty = Замедление при ношении: -33.4%

### Технические термины ###
ammo-type-shotgun = Дробь
ammo-count-shotgun = { $count ->
    [one] { $count } патрон
    [few] { $count } патрона
   *[many] { $count } патронов
}

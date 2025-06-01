### Базовые классы ###
ent-CMPelletShotgunBase = Базовый снаряд дробовика
ent-CMPelletShotgunBase-desc = Абстрактный базовый класс для снарядов дробовика.

### Типы снарядов ###
ent-CMPelletShotgunBuckshot = Дробь
ent-CMPelletShotgunBuckshot-desc = Стандартные дробинки (65 урона, 5 бронепробития). 4 дробины, разлёт 15°.

ent-CMPelletShotgunSlug = Пуля
ent-CMPelletShotgunSlug-desc = Пулевой снаряд (70 урона, 20 бронепробития). Оглушает на 1 секунду.

ent-CMPelletShotgunIncendiary = Зажигательная пуля
ent-CMPelletShotgunIncendiary-desc = Поджигающий снаряд (55 теплового урона, 5 бронепробития).

ent-CMPelletShotgunBeanbag = Травматический снаряд
ent-CMPelletShotgunBeanbag-desc = Менее-летальный снаряд (45 урона выносливости). Не наносит урона здоровью.

ent-CMPelletShotgunFlechette = Флешетты
ent-CMPelletShotgunFlechette-desc = Бронебойные элементы (30 урона, 35 бронепробития). 4 элемента, разлёт 6°.

ent-CMPelletShotgunIncendiaryBuckshot = Зажигательная дробь
ent-CMPelletShotgunIncendiaryBuckshot-desc = Дробинки с эффектом поджига. 4 дробины, разлёт 15°.

ent-RMCPelletShotgunBreaching = Штурмовой снаряд
ent-RMCPelletShotgunBreaching-desc = Снаряд для пробивания препятствий (55 урона, 5 бронепробития). 4 элемента, разлёт 8°.

### Характеристики снарядов ###
pellet-stats = 
    • Дробь: 65 урона ×4 (15°)
    • Пуля: 70 урона + оглушение
    • Зажигательная: 55 теплового урона + поджиг
    • Травматическая: 45 урона выносливости
    • Флешетты: 30 урона ×4 (35 бронепробития)
    • Штурмовая: 55 урона ×4 (8°)

### Спецэффекты ###
pellet-effects =
    • Оглушение (пулевой снаряд)
    • Поджиг (зажигательные)
    • Бронепробитие (флешетты)
    • Урон выносливости (травматические)

### Технические параметры ###
pellet-spread-angle = Угол разлёта: { $degrees }°
pellet-count = Количество элементов: { $count }
pellet-penetration = Бронепробитие: { $amount }
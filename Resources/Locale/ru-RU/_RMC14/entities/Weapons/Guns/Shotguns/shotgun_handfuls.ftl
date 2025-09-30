### Базовые классы ###

### Типы патронов ###
ent-CMShellShotgunBuckshot = Горсть дробовых патронов
ent-CMShellShotgunBuckshot-desc = Дробовые патроны с широким разбросом. Максимум 5 в стопке.

ent-RMCShellShotgunBreaching = Горсть штурмовых патронов
ent-RMCShellShotgunBreaching-desc = Патроны для пробивания препятствий. Максимум 6 в стопке.

ent-CMShellShotgunSlugs = Горсть пулевых патронов
ent-CMShellShotgunSlugs-desc = Тяжёлые пулевые патроны. Максимум 5 в стопке.

ent-CMShellShotgunIncendiary = Горсть зажигательных патронов
ent-CMShellShotgunIncendiary-desc = Самовоспламеняющиеся патроны. Максимум 5 в стопке.

ent-CMShellShotgunBeanbag = Горсть травматических патронов
ent-CMShellShotgunBeanbag-desc = Менее-летальные патроны для контроля толпы. Максимум 5 в стопке.

ent-CMShellShotgunFlechette = Горсть патронов с флешеттами
ent-CMShellShotgunFlechette-desc = Патроны с поражающими элементами. Максимум 5 в стопке.

ent-CMShellShotgunIncendiaryBuckshot = Горсть зажигательных дробовых патронов
ent-CMShellShotgunIncendiaryBuckshot-desc = Дробовые патроны с зажигательным эффектом. Максимум 5 в стопке.

### Стеки патронов ###
stack-RMCShellShotgunBuckshot = Стопка дробовых патронов
stack-RMCShellShotgunBreaching = Стопка штурмовых патронов
stack-RMCShellShotgunSlug = Стопка пулевых патронов
stack-RMCShellShotgunIncendiary = Стопка зажигательных патронов
stack-RMCShellShotgunBeanbag = Стопка травматических патронов
stack-RMCShellShotgunFlechette = Стопка патронов с флешеттами
stack-RMCShellShotgunIncendiaryBuckshot = Стопка зажигательных дробовых патронов

### Характеристики ###
shotgun-shell-stats = 
    • Дробовые: широкий разброс
    • Пулевые: высокая пробивная способность
    • Зажигательные: поджигают цель
    • Травматические: меньше-летальные
    • Флешетты: повышенная точность
    • Штурмовые: для пробивания преград

### Действия ###
shell-reload = Патрон заряжен
shell-eject = Патрон извлечён
shell-stack-count = { $count ->
    [one] 1 патрон
    [few] { $count } патрона
   *[many] { $count } патронов
} в стопке
# Chembomb casing interaction strings
rmc-chembomb-full = Корпус уже полон!
rmc-chembomb-beaker-empty = Контейнер пуст.
rmc-chembomb-fill = Перелито {$amount}ед. ({$total}/{$max}ед.)
rmc-chembomb-examine-volume = Химический заряд: {$current}/{$max}ед.
rmc-chembomb-examine-detonator = [color=green]Детонаторная сборка установлена.[/color]
rmc-chembomb-examine-no-detonator = [color=red]Детонаторная сборка отсутствует.[/color]
rmc-chembomb-examine-locked = [color=orange]ВЗВЕДЕНО[/color]

# Slot labels
rmc-chembomb-detonator-slot = Детонаторная сборка

# Explosive reagents
reagent-name-rmc-cyclonite = Циклонит (RDX)
reagent-desc-rmc-cyclonite = Белое кристаллическое твёрдое вещество, применяемое как взрывчатка. Одно из самых мощных военных взрывчатых веществ.

reagent-name-rmc-anfo = АНФО
reagent-desc-rmc-anfo = Смесь нитрата аммония и топливного масла. Широко используемая промышленная взрывчатка.

reagent-name-rmc-nitroglycerin = Нитроглицерин
reagent-desc-rmc-nitroglycerin = Чрезвычайно чувствительная маслянистая жидкая взрывчатка. Обращайтесь осторожно.

reagent-name-rmc-octogen = Октоген (HMX)
reagent-desc-rmc-octogen = Мощное взрывчатое соединение, более стабильное чем RDX, но с более высокими характеристиками.

reagent-name-rmc-ammonium-nitrate = Нитрат аммония
reagent-desc-rmc-ammonium-nitrate = Белое кристаллическое твёрдое вещество, используемое как удобрение и прекурсор взрывчатки.

reagent-name-rmc-potassium-hydroxide = Гидроксид калия
reagent-desc-rmc-potassium-hydroxide = Сильная щёлочь, используемая в различных химических процессах.

reagent-name-rmc-hexamine = Гексамин
reagent-desc-rmc-hexamine = Органическое соединение, используемое как таблетированное топливо и прекурсор взрывчатки. Горит чистым горячим пламенем.

reagent-name-rmc-potassium-chloride = Хлорид калия
reagent-desc-rmc-potassium-chloride = Соль металла-галида, применяемая в медицине и промышленности.

reagent-name-rmc-sodium-chloride = Хлорид натрия
reagent-desc-rmc-sodium-chloride = Обычная соль. Применяется везде — от кулинарии до химического синтеза.

# Demolitions simulator
rmc-demolitions-sim-no-casing = В активной руке нет корпуса.
rmc-demolitions-sim-not-casing = Удерживаемый предмет не является корпусом химбомбы.
rmc-demolitions-sim-header = [bold]Симуляция: {$name}[/bold]
rmc-demolitions-sim-volume = Загружено химикатов: {$current}/{$max}ед.
rmc-demolitions-sim-empty = [color=gray]Взрывчатые или зажигательные химикаты не обнаружены.[/color]
rmc-demolitions-sim-explosion = [bold]Взрыв:[/bold] Мощность {$power}, Затухание {$falloff}, Прим. радиус ~{$radius} тайл.
rmc-demolitions-sim-fire = [bold]Огонь:[/bold] Интенсивность {$intensity}, Радиус {$radius} тайл., Длительность {$duration}с

# Assembly tool steps
rmc-chembomb-seal-no-detonator = Сначала вставьте детонаторную сборку.
rmc-chembomb-seal-disarm-first = Сначала обезвредьте корпус.
rmc-chembomb-arm-seal-first = Сначала закройте корпус отвёрткой.
rmc-chembomb-sealed = Вы закручиваете корпус.
rmc-chembomb-unsealed = Вы откручиваете корпус.
rmc-chembomb-armed = Вы перерезаете провода взрывателя. Корпус взведён.
rmc-chembomb-disarmed = Вы перерезаете провод взведения. Корпус обезврежен.
rmc-chembomb-not-armed = Корпус не готов. Сначала завершите сборку.

# Examine stage
rmc-chembomb-examine-open = Не закрыт.
rmc-chembomb-examine-sealed = [color=yellow]Закрыт.[/color] Взведите кусачками.

# Mine casing deployment
rmc-mine-no-detonator = Детонаторная сборка не установлена.
rmc-mine-deploy-fail-occupied = Здесь уже есть мина!
rmc-mine-planted = Мина установлена.

# Ordnance part assembly (combine igniter/timer → detonator assembly)
rmc-ordnance-assembly-incompatible = Эти части несовместимы.
rmc-ordnance-assembly-combined = Вы соединяете части в {$result}.

# 84мм ракета: сборка
rmc-rocket-warhead-not-armed = Боеголовка не взведена. Сначала закройте её отвёрткой и примените кусачки.
rmc-rocket-warhead-no-detonator = В боеголовку не установлена детонаторная сборка.
rmc-rocket-tube-no-fuel = В трубе ракеты нет топлива. Заправьте 60 ед. метана или водорода.
rmc-rocket-tube-insufficient-fuel = Недостаточно топлива. Нужно {$required}ед., есть {$current}ед.
rmc-rocket-assembling = Собираю ракету...
rmc-rocket-assembled = Ракета собрана. Загрузите её в пусковую установку M5-ATL.
rmc-rocket-assembly-failed = Сборка не удалась. Проверьте состояние боеголовки.

# 80мм миномётный снаряд: сборка
rmc-mortar-shell-warhead-not-armed = Боеголовка миномётного снаряда не взведена. Завершите сборку сначала.
rmc-mortar-shell-warhead-no-detonator = В боеголовку снаряда не установлена детонаторная сборка.
rmc-mortar-shell-no-fuel = В корпусе снаряда нет топлива. Заправьте 60 ед. водорода.
rmc-mortar-shell-insufficient-fuel = Недостаточно топлива в корпусе. Нужно {$required}ед., есть {$current}ед.
rmc-mortar-shell-assembling = Собираю миномётный снаряд...
rmc-mortar-shell-assembled = Миномётный снаряд собран. Загрузите его в миномёт.
rmc-mortar-shell-assembly-failed = Сборка не удалась. Проверьте состояние боеголовки.

# Химический взрыв (паритет SSCM13)
rmc-chemical-explosion-warning = Взрывчатая смесь детонирует!

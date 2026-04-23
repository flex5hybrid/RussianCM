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
reagent-name-rmc-nitric-acid = Азотная кислота
reagent-desc-rmc-nitric-acid = Дымящая сильная кислота. Ключевой прекурсор для синтеза взрывчатых соединений. Хранить в холоде.

reagent-name-rmc-glycerol = Глицерол
reagent-desc-rmc-glycerol = Бесцветная вязкая жидкость без запаха. Реагирует с азотной кислотой при охлаждении, образуя нитроглицерин.

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

# Ordnance assembly overrides
rmc-chembomb-examine-sealed = [color=yellow]Закрыт.[/color]
rmc-ordnance-assembly-pry-locked = Сначала разблокируйте сборку отвёрткой.
rmc-ordnance-assembly-locked = Вы фиксируете сборку. Теперь её можно установить в корпус.
rmc-ordnance-assembly-unlocked = Вы разблокируете сборку. Её параметры снова можно менять.
rmc-ordnance-assembly-disassembled = Вы разбираете сборку обратно на части.
rmc-ordnance-payload-not-ready = Боеголовка ещё не готова к установке.
rmc-ordnance-payload-no-fuel = В двигательном корпусе нет топлива.
rmc-ordnance-payload-wrong-fuel = Двигательный корпус должен быть заправлен {$fuel}.
rmc-ordnance-payload-no-chemicals = В боеголовке нет химической смеси.
rmc-ordnance-payload-assembled = Боеприпас полностью собран.
rmc-ordnance-assembly-combined = Вы соединяете части в detonator assembly.
rmc-ordnance-timer-set = {$time} секунд
rmc-ordnance-timer-current = {$time} секунд (текущее)
rmc-ordnance-timer-popup = Таймер установлен на {$time} секунд.
rmc-ordnance-frequency-configure = Настроить частоту
rmc-ordnance-frequency-set = {$frequency}
rmc-ordnance-frequency-current = {$frequency} (текущая)
rmc-ordnance-frequency-popup = Частота установлена на {$frequency}.
rmc-ordnance-proximity-set = Радиус {$range} тайла
rmc-ordnance-proximity-current = Радиус {$range} тайла (текущий)
rmc-ordnance-proximity-popup = Радиус срабатывания установлен на {$range} тайла.

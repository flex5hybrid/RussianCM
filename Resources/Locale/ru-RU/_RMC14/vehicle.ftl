# Общие события транспорта
rmc-vehicle-wheel-repaired = Колесо отремонтировано.
rmc-vehicle-crash-immobile = Двигатель глохнет от удара!
rmc-vehicle-crash-immobile-try-again = Двигатель ещё не восстановился после удара.
rmc-vehicle-crash-immobile-recovered = Двигатель снова заводится.

# Посадка / высадка
rmc-vehicle-ride-climb = Забраться на
rmc-vehicle-ride-climb-self = Вы забираетесь на {$vehicle}.
rmc-vehicle-ride-climb-others = {$user} забирается на {$vehicle}.
rmc-vehicle-ride-climb-down = Слезть
rmc-vehicle-ride-climb-down-self = Вы слезаете с {$vehicle}.
rmc-vehicle-ride-climb-down-others = {$user} слезает с {$vehicle}.

# Точки крепления — общее
rmc-hardpoint-remove-verb = Удалить {$slot}
rmc-hardpoint-repaired = Точка крепления отремонтирована.
rmc-hardpoint-intact = Точка крепления уже цела.
rmc-hardpoint-remove-blocked = Эта точка крепления зафиксирована на месте.

# Точки крепления — осмотр
rmc-hardpoint-integrity-examine = Целостность: [color={$color}]{$current}/{$max} ({$percent}%)[/color]
rmc-hardpoint-armor-modifiers-examine = Модификаторы урона: кислотный {$acid}, рубящий {$slash}, колющий {$bullet}, взрывной {$explosive}, дробящий {$blunt}.
rmc-hardpoint-condition-pristine = Она в идеальном состоянии.
rmc-hardpoint-condition-good = Она в хорошем состоянии.
rmc-hardpoint-condition-worn = На ней видны следы износа.
rmc-hardpoint-condition-bad = Она в плохом состоянии.
rmc-hardpoint-condition-critical = Она еле держится.

# Точки крепления — интерфейс
rmc-hardpoint-ui-title = Точки крепления
rmc-hardpoint-ui-empty-slot = Пусто
rmc-hardpoint-ui-integrity = {$current}/{$max} ({$percent}%)
rmc-hardpoint-ui-no-integrity = Нет данных о целостности
rmc-hardpoint-ui-remove = Удалить
rmc-hardpoint-ui-removing = Удаление...

# Загрузчик боеприпасов — сообщения
rmc-vehicle-ammo-loader-no-vehicle = Загрузчик не подключён к транспортному средству.
rmc-vehicle-ammo-loader-no-hardpoint = Совместимая точка крепления не установлена.
rmc-vehicle-ammo-loader-wrong-ammo = Эти боеприпасы не подходят к данному загрузчику.
rmc-vehicle-ammo-loader-full = {$target} уже заполнен.
rmc-vehicle-ammo-loader-empty = {$box} пуст.
rmc-vehicle-ammo-loader-loaded = Загружено {$amount} патронов в {$target}.
rmc-vehicle-ammo-loader-unloaded = Извлечено {$amount} патронов из {$target}.
rmc-vehicle-ammo-loader-box-full = {$box} заполнен.
rmc-vehicle-ammo-loader-in-use = Загрузчик уже используется.
rmc-vehicle-ammo-loader-hold-ammo = Возьмите коробку с боеприпасами в руки, чтобы загрузить её.
rmc-vehicle-ammo-loader-not-enough = В коробке недостаточно патронов для снаряжения магазина.

# Загрузчик боеприпасов — интерфейс
rmc-vehicle-ammo-loader-ui-ammo = Боеприпасы: {$current}/{$max}
rmc-vehicle-ammo-loader-ui-no-hardpoints = Нет совместимых точек крепления.
rmc-vehicle-ammo-loader-ui-slot = Слот: {$slot} ({$type})
rmc-vehicle-ammo-loader-ui-chambered = В патроннике: {$current}/{$max}
rmc-vehicle-ammo-loader-ui-stored = В запасе: {$current}/{$max}
rmc-vehicle-ammo-loader-ui-load = Загрузить
rmc-vehicle-ammo-loader-ui-full = Заполнен
rmc-vehicle-ammo-loader-ui-no-ammo = Нет патронов
rmc-vehicle-ammo-loader-ui-ready-slot = 1 орудие
rmc-vehicle-ammo-loader-ui-slot-tooltip = {$current}/{$max} патронов

# Оружие транспорта — интерфейс
rmc-vehicle-weapons-ui-title = Оружие транспорта
rmc-vehicle-weapons-ui-empty-slot = Пусто
rmc-vehicle-weapons-ui-select = Выбрать
rmc-vehicle-weapons-ui-selected = Выбрано
rmc-vehicle-weapons-ui-unavailable = Недоступно
rmc-vehicle-weapons-ui-ammo = Боеприпасы: {$current}/{$max}
rmc-vehicle-weapons-ui-ammo-none = Боеприпасы: --
rmc-vehicle-weapons-ui-chambered = В патроннике: {$current}/{$max}
rmc-vehicle-weapons-ui-stored = В запасе: {$current}/{$max}
rmc-vehicle-weapons-ui-operator = Оператор: {$name}
rmc-vehicle-weapons-ui-operator-self = Оператор: Вы
rmc-vehicle-weapons-ui-in-use = Используется
rmc-vehicle-weapons-ui-slot = Слот: {$slot}
rmc-vehicle-weapons-ui-turret-slot = Слот турели: {$slot}
rmc-vehicle-weapons-ui-mounted-to = Установлено на: {$slot}
rmc-vehicle-weapons-ui-hardpoint-in-use = {$operator} уже управляет этой точкой крепления.
rmc-vehicle-weapons-ui-auto-on = Автотурель: Вкл.
rmc-vehicle-weapons-ui-auto-off = Автотурель: Выкл.
rmc-vehicle-weapons-ui-stabilization-on = Стабилизация: Вкл.
rmc-vehicle-weapons-ui-stabilization-off = Стабилизация: Выкл.
rmc-vehicle-weapons-ui-none-selected = Точка крепления не выбрана
rmc-vehicle-weapons-ui-integrity = Целостность: {$current}/{$max} ({$percent}%)
rmc-vehicle-weapons-ui-no-integrity = Целостность: --
rmc-vehicle-weapons-ui-cooldown-ready = ГОТОВО
rmc-vehicle-weapons-ui-cooldown-recharging = Перезарядка: {$seconds}с

# Бортовое орудие
rmc-vehicle-portgun-need-seat = Вам нужно занять место у бортового орудия.
rmc-vehicle-portgun-no-vehicle = Бортовое орудие не подключено к транспортному средству.
rmc-vehicle-portgun-no-gun = Бортовое орудие не установлено.
rmc-vehicle-portgun-in-use = {$operator} уже управляет бортовым орудием.
rmc-vehicle-portgun-active = Вы уже управляете бортовым орудием.
rmc-vehicle-portgun-examine-ammo = Боеприпасы: {$current}/{$max}
rmc-vehicle-portgun-eject = Извлечь магазин

# Турель
rmc-vehicle-turret-no-base = Совместимая турель не установлена.

# Развёртывание / свёртывание
rmc-vehicle-deploy-not-driver = Для развёртывания необходимо находиться на месте водителя.
rmc-vehicle-deploy-requires-turret = Для развёртывания необходимо установить турель.
rmc-vehicle-deploy-start = Развёртывание началось.
rmc-vehicle-undeploy-start = Свёртывание началось.
rmc-vehicle-deploy-finish = Транспортное средство развёрнуто.
rmc-vehicle-undeploy-finish = Транспортное средство свёрнуто.
rmc-vehicle-deploy-action-name-deploy = Развернуть
rmc-vehicle-deploy-action-desc-deploy = Развернуть транспортное средство.
rmc-vehicle-deploy-action-name-undeploy = Свернуть
rmc-vehicle-deploy-action-desc-undeploy = Свернуть транспортное средство.
rmc-vehicle-deploy-action-name-deploying = Развёртывание...
rmc-vehicle-deploy-action-desc-deploying = Выполняется развёртывание.
rmc-vehicle-deploy-action-name-undeploying = Свёртывание...
rmc-vehicle-deploy-action-desc-undeploying = Выполняется свёртывание.

# Вход / выход
rmc-vehicle-enter-locked = Транспортное средство заблокировано.
rmc-vehicle-enter-use-doorway = Для входа необходимо использовать дверной проём.
rmc-vehicle-enter-busy = Туда уже кто-то входит.
rmc-vehicle-enter-xeno-full = Внутри больше нет места для ксеносов.
rmc-vehicle-enter-passenger-full = Внутри нет места для пассажиров.
rmc-vehicle-hull-destroyed = Корпус транспортного средства разрушен.
rmc-vehicle-exit-busy = Кто-то уже использует этот выход.
rmc-vehicle-exit-blocked = Выход заблокирован.
rmc-vehicle-look-inside = Заглянуть внутрь

# Замок
rmc-vehicle-lock-not-driver = Чтобы запереть или отпереть транспортное средство, необходимо находиться на месте водителя.
rmc-vehicle-lock-broken = Замок транспортного средства сломан.
rmc-vehicle-lock-broken-attempt = Транспортное средство невозможно запереть, пока сломанный замок не отремонтирован.
rmc-vehicle-lock-set-locked = Двери транспортного средства заблокированы.
rmc-vehicle-lock-set-unlocked = Двери транспортного средства разблокированы.
rmc-vehicle-lock-too-damaged = Замок слишком повреждён, чтобы его можно было задействовать.
rmc-vehicle-lock-broken-open = От повреждений замок транспортного средства ломается!
rmc-vehicle-lock-operational-again = Замок транспортного средства снова работает.
rmc-vehicle-lock-broken-success = Вы взламываете замок транспортного средства.
rmc-vehicle-lock-repaired = Вы ремонтируете замок транспортного средства.

# Ключ
rmc-vehicle-key-name = ключ от транспортного средства
rmc-vehicle-key-name-copy = дубликат ключа от транспортного средства
rmc-vehicle-key-name-specific = ключ от {$vehicle}
rmc-vehicle-key-name-copy-specific = дубликат ключа от {$vehicle}
rmc-vehicle-key-bind-success = Вы привязываете ключ к транспортному средству.
rmc-vehicle-key-copy-success = Вы копируете ключ от транспортного средства.
rmc-vehicle-key-copy-invalid = Этот ключ невозможно скопировать.
rmc-vehicle-key-copy-requires-source = Сначала необходимо скопировать существующий ключ от транспортного средства.
rmc-vehicle-key-unbound = Ключ не привязан ни к какому транспортному средству.
rmc-vehicle-key-invalid = Ключ не подходит к этому транспортному средству.
rmc-vehicle-key-examine-blank = [color=lightblue]Этот чистый ключ можно привязать к транспортному средству, используя его на нём.[/color]
rmc-vehicle-key-examine-duplicator = [color=lightblue]Этот чистый ключ позволяет скопировать существующий ключ, используя его на том ключе.[/color]
rmc-vehicle-key-examine-bound = [color=lightblue]Этот ключ привязан к замку транспортного средства.[/color]

# Названия поломок (короткие, для строк статуса)
rmc-hardpoint-failure-name-armor-compromised = пробоина в броне
rmc-hardpoint-failure-name-feed-jam = заклинивание системы подачи
rmc-hardpoint-failure-name-runaway-trigger = самопроизвольный спуск
rmc-hardpoint-failure-name-turret-traverse = повреждение кольца поворота башни
rmc-hardpoint-failure-name-engine-misfire = перебои в двигателе
rmc-hardpoint-failure-name-transmission-slip = пробуксовка трансмиссии
rmc-hardpoint-failure-name-warped-frame = деформация рамы
rmc-hardpoint-failure-name-damaged-mount = повреждение крепления
rmc-hardpoint-failure-name-tire-blowout = разрыв шины
rmc-hardpoint-failure-name-thrown-tread = слетевшая гусеница
rmc-hardpoint-failure-name-engine-overheat = перегрев двигателя
rmc-hardpoint-failure-name-electrical-short = короткое замыкание
rmc-hardpoint-failure-name-fuel-leak = утечка топлива
rmc-hardpoint-failure-name-default = неисправность точки крепления

# Названия поломок в алертах и уведомлениях
rmc-hardpoint-failure-alert-armor-compromised = Пробоина в броне
rmc-hardpoint-failure-alert-feed-jam = Заклинивание системы подачи
rmc-hardpoint-failure-alert-runaway-trigger = Самопроизвольный спуск
rmc-hardpoint-failure-alert-turret-traverse = Повреждение поворота башни
rmc-hardpoint-failure-alert-engine-misfire = Перебои в двигателе
rmc-hardpoint-failure-alert-transmission-slip = Пробуксовка трансмиссии
rmc-hardpoint-failure-alert-warped-frame = Деформация рамы
rmc-hardpoint-failure-alert-damaged-mount = Повреждение крепления
rmc-hardpoint-failure-alert-tire-blowout = Разрыв шины
rmc-hardpoint-failure-alert-thrown-tread = Слетевшая гусеница
rmc-hardpoint-failure-alert-engine-overheat = Перегрев двигателя
rmc-hardpoint-failure-alert-electrical-short = Короткое замыкание
rmc-hardpoint-failure-alert-fuel-leak = Утечка топлива
rmc-hardpoint-failure-alert-default = Неисправность точки крепления

# Описания эффектов поломок
rmc-hardpoint-failure-effect-armor-compromised = Броневая защита этой точки крепления отключена.
rmc-hardpoint-failure-effect-feed-jam = Это оружие может случайно заклинить или дать осечку.
rmc-hardpoint-failure-effect-runaway-trigger = Это оружие может самопроизвольно выстрелить в установленном состоянии.
rmc-hardpoint-failure-effect-turret-traverse = Скорость поворота башни значительно снижена.
rmc-hardpoint-failure-effect-engine-misfire = Ускорение и максимальная скорость транспортного средства снижены.
rmc-hardpoint-failure-effect-transmission-slip = Ускорение, скорость заднего хода и максимальная скорость снижены.
rmc-hardpoint-failure-effect-warped-frame = Деформированная рама тормозит движение и снижает ходовые характеристики.
rmc-hardpoint-failure-effect-damaged-mount = Мощность этой точки крепления снижена до переустановки крепления.
rmc-hardpoint-failure-effect-tire-blowout = Транспортное средство теряет скорость и сцепление из-за повреждённой шины.
rmc-hardpoint-failure-effect-thrown-tread = Транспортное средство едва движется до переустановки гусеницы.
rmc-hardpoint-failure-effect-engine-overheat = Двигатель захлёбывается, ускорение резко снижено.
rmc-hardpoint-failure-effect-electrical-short = Электрическая мощность этой точки крепления нестабильна и снижена.
rmc-hardpoint-failure-effect-fuel-leak = Blackfoot теряет топливо со временем до ремонта.
rmc-hardpoint-failure-effect-default = Точка крепления неисправна.

# Формат-строки диагностики поломок
rmc-hardpoint-failure-header = Неисправности транспорта
rmc-hardpoint-failure-hardpoint-header = Неисправности точки крепления
rmc-hardpoint-failure-on-label = { $name } на { $location }
rmc-hardpoint-failure-effect-label = Эффект: { $effect }
rmc-hardpoint-failure-repair-label = Ремонт: шаг { $step }/{ $total } — { $instruction } Используйте { $tool }.
rmc-hardpoint-failure-hull-label = Корпус: { $statuses }
rmc-hardpoint-failure-slot-summary = { $slot }: { $statuses }
rmc-hardpoint-failure-status = { $name } (шаг { $step }/{ $total }: { $tool })
rmc-hardpoint-failure-diagnostic = { $name } — { $effect }
rmc-hardpoint-failure-detected = { $name } обнаружено.
rmc-hardpoint-failure-detected-on = { $name } обнаружено на { $location }.

# Инструкции по ремонту
rmc-hardpoint-repair-armor-compromised-1 = Затяните крепёж брони и зафиксируйте пластину.
rmc-hardpoint-repair-armor-compromised-2 = Заварите и залатайте пробоины в броне.
rmc-hardpoint-repair-feed-jam-1 = Откройте крышку подачи и устраните погнутые звенья ленты.
rmc-hardpoint-repair-feed-jam-2 = Прокрутите привод подачи мультитулом.
rmc-hardpoint-repair-runaway-trigger-1 = Откройте корпус спускового механизма и изолируйте изношенную тягу шептала.
rmc-hardpoint-repair-runaway-trigger-2 = Сбросьте реле управления огнём мультитулом.
rmc-hardpoint-repair-runaway-trigger-3 = Установите и затяните тягу спускового механизма.
rmc-hardpoint-repair-turret-traverse-1 = Затяните и переиндексируйте кольцо поворота башни.
rmc-hardpoint-repair-turret-traverse-2 = Поднимите подшипник башни домкратом и установите кольцо на место.
rmc-hardpoint-repair-engine-misfire-1 = Откройте панель доступа к двигателю.
rmc-hardpoint-repair-engine-misfire-2 = Импульсируйте цепь управления зажиганием мультитулом.
rmc-hardpoint-repair-engine-misfire-3 = Затяните крепления двигателя после стабилизации цепи.
rmc-hardpoint-repair-transmission-slip-1 = Поднимите трансмиссию домкратом и установите на место.
rmc-hardpoint-repair-transmission-slip-2 = Затяните болты корпуса трансмиссии.
rmc-hardpoint-repair-warped-frame-1 = Поднимите раму домкратом и снимите нагрузку с деформированного участка.
rmc-hardpoint-repair-warped-frame-2 = Нагрейте и выправьте деформированные элементы рамы сварочным аппаратом.
rmc-hardpoint-repair-warped-frame-3 = Затяните распорки рамы.
rmc-hardpoint-repair-damaged-mount-1 = Поднимите точку крепления домкратом, освободив повреждённое крепление.
rmc-hardpoint-repair-damaged-mount-2 = Установите и затяните фиксирующую арматуру крепления.
rmc-hardpoint-repair-tire-blowout-1 = Снимите разорванный корпус шины с обода.
rmc-hardpoint-repair-tire-blowout-2 = Поднимите ступицу домкратом и установите новый колёсный узел.
rmc-hardpoint-repair-tire-blowout-3 = Затяните колёсные гайки в последовательности.
rmc-hardpoint-repair-thrown-tread-1 = Поднимите ходовую часть домкратом и снимите натяжение с гусеницы.
rmc-hardpoint-repair-thrown-tread-2 = Наденьте слетевшие звенья гусеницы обратно на катки.
rmc-hardpoint-repair-thrown-tread-3 = Зафиксируйте натяжитель и затяните пальцы гусеницы.
rmc-hardpoint-repair-engine-overheat-1 = Откройте кожух двигателя и выпустите накопившееся тепло.
rmc-hardpoint-repair-engine-overheat-2 = Снимите деформированный кожух вентилятора с радиатора.
rmc-hardpoint-repair-engine-overheat-3 = Импульсируйте контроллер насоса охлаждающей жидкости до стабилизации потока.
rmc-hardpoint-repair-electrical-short-1 = Срежьте сгоревшую проводку с жгута точки крепления.
rmc-hardpoint-repair-electrical-short-2 = Проследите и сбросьте управляющую цепь мультитулом.
rmc-hardpoint-repair-electrical-short-3 = Закройте панель доступа и закрепите новый жгут.
rmc-hardpoint-repair-fuel-leak-1 = Откройте сервисную панель топливной системы и изолируйте разорванный топливный шланг.
rmc-hardpoint-repair-fuel-leak-2 = Залатайте протекающий топливный шланг.
rmc-hardpoint-repair-fuel-leak-3 = Затяните муфту топливного шланга.

# Интерфейс консоли снабжения техникой
rmc-vehicle-supply-ui-stored-vehicles = Хранящиеся ТС
rmc-vehicle-supply-ui-loadout = Снаряжение ТС
rmc-vehicle-supply-ui-preview-title = Транспорт
rmc-vehicle-supply-ui-preview-empty = Предпросмотр ТС

rmc-vehicle-supply-ui-raise = Поднять
rmc-vehicle-supply-ui-lower = Опустить

rmc-vehicle-supply-ui-status = Подъёмник: {$mode} | Статус: {$status} | Активно: {$active}
rmc-vehicle-supply-ui-no-lift = нет подъёмника

rmc-vehicle-supply-ui-none = нет
rmc-vehicle-supply-ui-busy = занят
rmc-vehicle-supply-ui-idle = ожидание

rmc-vehicle-supply-ui-copies-expanded = Копии v
rmc-vehicle-supply-ui-copies-collapsed = Копии >

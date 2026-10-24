# Локализация интерфейса
cmu-ship-weapons-title = Вооружение корабельных орудий
cmu-ship-weapons-instructions = Выберите от 1 до {$max} орудий. Все выбранные орудия используют общий запас огневых задач: {$uses}. Каждый выстрел выжигает батарею, из которой он произведён, пока её не отремонтируют.
cmu-ship-weapons-unarmed = Орудия не подготовлены. Подтверждение для этой операции необратимо.
cmu-ship-weapons-arm = Подготовить выбранные орудия
cmu-ship-weapons-confirm = Навсегда зафиксировать эту конфигурацию?
cmu-ship-weapons-locked = Конфигурация орудий этого корабля навсегда зафиксирована.
cmu-ship-weapons-no-ship = Терминал должен быть установлен на корабельной сетке или палубе.
cmu-ship-weapons-access-denied = Требуется доступ командования.
cmu-ship-weapons-invalid-selection = Выберите от 1 до {$max} разных орудий из доступного списка.
cmu-ship-weapons-not-armed = Это орудие не подготовлено на этом корабле.
cmu-ship-weapons-depleted = Этот корабль израсходовал все общие огневые задачи.
cmu-ship-weapons-cooldown = Корабельные батареи поддержки перезаряжаются.
cmu-ship-weapons-invalid-target = Неверная орбитальная цель: вне границ, под землёй или защищена.
cmu-ship-weapons-launched = {$weapon}: принято. Удар через 24 секунды.
cmu-ship-weapons-incoming = ВНИМАНИЕ! Приближается удар корабельного оружия! Покиньте район цели!
cmu-ship-weapons-directional = ВНИМАНИЕ! Цель орбитального удара к {$direction} от вас! Покиньте район!
cmu-ship-weapons-directional-above = ВНИМАНИЕ! Орбитальный удар нацелен на вашу позицию! Немедленно уходите!
cmu-ship-weapons-squad-warning = ВНИМАНИЕ! Приближается {$weapon}! Уходите из зоны опасного сближения!
cmu-ship-weapons-select = Орбитальное орудие
cmu-ship-weapons-armed-hint = Использует одну из общих огневых задач этого корабля.
cmu-ship-weapons-remaining = {$weapon} (осталось общих задач: {$uses}/{$max})
cmu-ship-weapons-bearing = Направление шагающего заградительного огня
cmu-ship-weapons-bearing-north = северу
cmu-ship-weapons-bearing-east = востоку
cmu-ship-weapons-bearing-south = югу
cmu-ship-weapons-bearing-west = западу
cmu-ship-weapons-gatling = Заградительный огонь «Гатлинг»
cmu-ship-weapons-walking = Шагающий заградительный огонь
cmu-ship-weapons-120mm = 120-мм ОФ заградительный огонь
cmu-ship-weapons-380mm = 380-мм ОФ заградительный огонь
cmu-ship-weapons-gas = Газовый удар
cmu-ship-weapons-ems = ЭМИ-удар
cmu-ship-weapons-smoke = Дымовой удар
cmu-ship-weapons-airburst = Удар с воздушным подрывом
cmu-ship-weapons-precision = Точный удар
cmu-ship-weapons-railcannon = Удар рельсовой пушки
cmu-ship-weapons-missing-gun = Чтобы подготовить это орудие, на корабле должна быть установлена совместимая орбитальная батарея.
cmu-ship-weapons-no-loaded-gun = Все батареи на этом корабле выгорели. Отремонтируйте одну, прежде чем стрелять снова.
cmu-ship-weapons-crt-heading = ОРБИТАЛЬНОЕ УПРАВЛЕНИЕ ОГНЁМ
cmu-ship-weapons-crt-subheading = КОРАБЕЛЬНАЯ БАТАРЕЯ ПОДДЕРЖКИ // КОНСОЛЬ ПОДГОТОВКИ ЗАДАЧ
cmu-ship-weapons-crt-status-open = КОНФИГУРАЦИЯ ОТКРЫТА
cmu-ship-weapons-crt-status-locked = КОНФИГУРАЦИЯ ЗАФИКСИРОВАНА
cmu-ship-weapons-crt-status-offline = НЕТ СВЯЗИ С КОРАБЛЁМ
cmu-ship-weapons-crt-selected = ВЫБРАНО {$count}/{$max}
cmu-ship-weapons-crt-armed-count = ПОДГОТОВЛЕНО: {$count} {$count ->
    [one] ЗАДАЧА
    [few] ЗАДАЧИ
    [many] ЗАДАЧ
   *[other] ЗАДАЧИ
}
cmu-ship-weapons-crt-pool = ОСТАЛОСЬ ОГНЕВЫХ ЗАДАЧ: {$uses}/{$max}
cmu-ship-weapons-crt-pool-pending = ОБЩИХ ОГНЕВЫХ ЗАДАЧ ПРИ ФИКСАЦИИ: {$uses}
cmu-ship-weapons-crt-tag-ready = > ГОТОВО К ПОДГОТОВКЕ
cmu-ship-weapons-crt-tag-selected = > ВЫБРАНО
cmu-ship-weapons-crt-tag-no-battery = ! БАТАРЕЯ НЕ УСТАНОВЛЕНА
cmu-ship-weapons-crt-tag-unavailable = ! НЕДОСТУПНО
cmu-ship-weapons-crt-tag-armed = > ПОДГОТОВЛЕНО // ЗАДАЧ: {$uses}/{$max}
cmu-ship-weapons-crt-tag-standby = - ОЖИДАНИЕ
cmu-ship-weapons-crt-detail-barrage = ВЫСТРЕЛОВ: {$shots} // РАДИУС: {$blast} кл. // РАЗБРОС: {$spread} кл.
cmu-ship-weapons-crt-detail-single = {$shots ->
    [one] ОДИН ВЫСТРЕЛ
    [few] {$shots} ВЫСТРЕЛА
    [many] {$shots} ВЫСТРЕЛОВ
   *[other] {$shots} ВЫСТРЕЛА
} // РАДИУС: {$blast} кл. // БЕЗ РАЗБРОСА
cmu-ship-weapons-crt-detail-walking = ВЫСТРЕЛОВ: {$shots} // РАДИУС: {$blast} кл. // ШАГ: {$distance} кл.
cmu-ship-weapons-crt-detail-cloud = ОБЛАКО: {$radius} кл. // 30 СЕК
cmu-ship-weapons-crt-detail-ems = ПОЛЕ ОГЛУШЕНИЯ: {$radius} кл. // ВСЕ ЦЕЛИ
cmu-ship-weapons-crt-detail-rail = УДАР РЕЛЬСЫ 5x5 // ВСЕ ПАЛУБЫ
cmu-ship-weapons-gun-ready = [color=green]Батарея готова к выстрелу.[/color]
cmu-ship-weapons-gun-needs-repair = [color=yellow]Выгорела. Шаг ремонта {$step}/{$steps}: {$hint}.[/color]
cmu-ship-weapons-repair-complete = Батарея снова оживает. Готова к выстрелу.
cmu-ship-weapons-repair-hatch-hint = откройте затворную крышку ломом
cmu-ship-weapons-repair-hatch-done = Вы открываете затворную крышку.
cmu-ship-weapons-repair-housing-hint = открутите кожух катушки
cmu-ship-weapons-repair-housing-done = Вы откручиваете кожух катушки.
cmu-ship-weapons-repair-unbolt-hint = отвинтите выгоревший блок катушки гаечным ключом
cmu-ship-weapons-repair-unbolt-done = Вы отвинчиваете выгоревший блок катушки.
cmu-ship-weapons-repair-wiring-hint = срежьте оплавленную проводку
cmu-ship-weapons-repair-wiring-done = Вы срезаете оплавленную проводку.
cmu-ship-weapons-repair-weld-hint = заварите швы заменяющих рельсов
cmu-ship-weapons-repair-weld-done = Вы завариваете швы заменяющих рельсов.
cmu-ship-weapons-repair-bolt-hint = привинтите блок катушки обратно гаечным ключом
cmu-ship-weapons-repair-bolt-done = Вы привинчиваете блок катушки обратно.
cmu-ship-weapons-repair-calibrate-hint = откалибруйте ретранслятор наведения мультитулом
cmu-ship-weapons-repair-calibrate-done = Вы калибруете ретранслятор наведения.
cmu-ship-weapons-repair-secure-hint = закрутите кожух
cmu-ship-weapons-repair-secure-done = Вы закручиваете кожух.

# Локализация связанных энтити

## Терминалы активации
ent-CMUShipWeaponsTerminal = терминал активации корабельного вооружения
    .desc = Выбор орбитального вспомогательного вооружения для этого корабля. На терминале отображается ограничение выбора и общие огневые задачи. Подтверждённые варианты выбора не могут быть изменены во время операции.

ent-CMUShipWeaponsTerminalGovfor = { ent-CMUShipWeaponsTerminal }
    .desc = { ent-CMUShipWeaponsTerminal.desc }
    .suffix = ГОВФОР

ent-CMUShipWeaponsTerminalOpfor = { ent-CMUShipWeaponsTerminal }
    .desc = { ent-CMUShipWeaponsTerminal.desc }
    .suffix = ОПФОР

## Снаряды и эффекты
ent-CMUShipWeaponSmoke = орбитальный дым
ent-CMUShipWeaponGas = орбитальный токсичный газ
ent-CMUShipWeaponEmsCloud = электромагнитные помехи
ent-CMUShipWeaponEmsStun = ЭМИ-паралич

## Орбитальные батареи
ent-CMUShipWeaponGunBase = орбитальная батарея
    .desc = Корабельная орбитальная батарея поддержки. Каждая огневая задача выжигает её, и её нужно пошагово отремонтировать, прежде чем она снова сможет стрелять. Осмотрите её, чтобы увидеть следующий шаг.

ent-CMUShipWeaponGun = орбитальная батарея
    .desc = Корабельная орбитальная батарея поддержки. Каждая огневая задача выжигает её, и её нужно пошагово отремонтировать, прежде чем она снова сможет стрелять. Осмотрите её, чтобы увидеть следующий шаг.
    .suffix = CMU

ent-CMUShipWeaponGunFlipped = орбитальная батарея
    .desc = Корабельная орбитальная батарея поддержки. Каждая огневая задача выжигает её, и её нужно пошагово отремонтировать, прежде чем она снова сможет стрелять. Осмотрите её, чтобы увидеть следующий шаг.
    .suffix = CMU, перевёрнутая

## Спавнеры корабельных ударов
ent-CMUShipWeaponStrikeSpawnerGatlingBarrage = корабельный удар заградительным огнём «Гатлинг»
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerGatlingBarrageInstant = корабельный удар заградительным огнём «Гатлинг»
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerWalkingBarrage = корабельный удар шагающим заградительным огнём
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerWalkingBarrageInstant = корабельный удар шагающим заградительным огнём
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawner120mmHEBarrage = корабельный удар 120-мм ОФ заградительным огнём
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawner120mmHEBarrageInstant = корабельный удар 120-мм ОФ заградительным огнём
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawner380mmHEBarrage = корабельный удар 380-мм ОФ заградительным огнём
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawner380mmHEBarrageInstant = корабельный удар 380-мм ОФ заградительным огнём
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerGasStrike = корабельный газовый удар
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerGasStrikeInstant = корабельный газовый удар
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerEMSStrike = корабельный ЭМИ-удар
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerEMSStrikeInstant = корабельный ЭМИ-удар
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerSmokeStrike = корабельный дымовой удар
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerSmokeStrikeInstant = корабельный дымовой удар
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerAirburstStrike = корабельный удар с воздушным подрывом
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerAirburstStrikeInstant = корабельный удар с воздушным подрывом
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerPrecisionStrike = корабельный точный удар
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerPrecisionStrikeInstant = корабельный точный удар
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerRailcannonStrike = корабельный удар рельсовой пушки
    .suffix = CMU, ЗАПУСКАЕТ КОРАБЕЛЬНЫЙ УДАР!!!

ent-CMUShipWeaponStrikeSpawnerRailcannonStrikeInstant = корабельный удар рельсовой пушки
    .suffix = CMU, МГНОВЕННЫЙ КОРАБЕЛЬНЫЙ УДАР!!!
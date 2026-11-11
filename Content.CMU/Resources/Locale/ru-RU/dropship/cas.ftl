## Заклинивание оружия

cmu-dropship-weapon-jammed = ЗАКЛИНИВАНИЕ ОРУЖИЯ: {$reason}. Почините установку с помощью мультитула.
cmu-dropship-fault-reason-jam-feed = заклинило подачу
cmu-dropship-fault-reason-jam-double = двойное заклинивание
cmu-dropship-fault-reason-jam-gremlin = в затворе что-то гремит/болтается

## Чек-лист наземной команды

cmu-dropship-checklist-header = [color=cyan]ЧЕК-ЛИСТ НАЗЕМНОЙ КОМАНДЫ[/color]
cmu-dropship-checklist-mounted = Установлено: {$weapon}
cmu-dropship-checklist-ammo = Загружено: {$ammo} ({$rounds}/{$max})
cmu-dropship-checklist-no-ammo = Загружено: [color=orange]НЕТ[/color]
cmu-dropship-checklist-faulted = Состояние: [color=red]НЕИСПРАВНО[/color] ({$reason})
cmu-dropship-checklist-serviceable = Состояние: [color=lime]ИСПРАВНО[/color]
cmu-dropship-checklist-wear = Износ: {$uses} боёв, {$landings} посадок с момента обслуживания. Шанс отказа при посадке {$landing}%, шанс заклинивания {$jam}%.
cmu-dropship-checklist-ok = [color=lime]{$text}[/color]
cmu-dropship-checklist-bad = [color=orange]{$text}[/color]

## Сводки на консоли пилота

cmu-dropship-loadout-title = ЗАГРУЗКА
cmu-dropship-loadout-line = {$location} {$weapon}: {$ammo}
cmu-dropship-loadout-ammo = {$ammo} {$rounds}/{$max}
cmu-dropship-loadout-empty = ПУСТО
cmu-dropship-loadout-none = ОРУЖИЕ НЕ УСТАНОВЛЕНО
cmu-dropship-preflight-title = ПРЕДПОЛЁТНАЯ ПРОВЕРКА
cmu-dropship-preflight-booting = ЗАГРУЗКА СИСТЕМЫ УПРАВЛЕНИЯ ОГНЁМ
cmu-dropship-preflight-fault = НЕИСПРАВНОСТЬ {$location}: {$reason}
cmu-dropship-preflight-empty = ПУСТАЯ УСТАНОВКА {$location}
cmu-dropship-preflight-dry = НЕТ БОЕПРИПАСОВ {$location}
cmu-dropship-preflight-ok = ВСЕ СИСТЕМЫ В НОРМЕ

## Нагрев M90

## Админ-команды

cmu-cas-admin-verb-category = КАС (Админ)
cmu-cas-admin-clear-fault = Сбросить неисправность и износ
cmu-cas-admin-force-fault = Принудительно вызвать неисправность
cmu-cas-admin-refill = Пополнить боеприпасы
cmu-cas-admin-skip-boot = Пропустить загрузку системы управления огнём
cmu-cas-admin-landing-reboot = Принудительный бросок на перезагрузку при посадке
cmu-cas-admin-refill-all = Пополнить все установки
cmu-cas-admin-clear-all = Сбросить все неисправности и восстановить M90

## Последствия ракет
cmu-cas-fallout-designator-obscured = Дым рассеивает лазер. Цель невозможно подсветить сквозь него.
## Запросы поддержки с воздуха от RTO

cmu-cas-requests-title = ЗАПРОСЫ КАС
cmu-cas-requests-none = НЕТ ОТКРЫТЫХ ЗАПРОСОВ
cmu-cas-request-card = #{$id} {$callsign} | {$status}
    ЦЕЛЬ: {$target} @ {$location}
    ОПИСАНИЕ: {$description}
    СОЮЗНИКИ: {$friendlies}
    БОЕПРИПАС: {$munition}{$danger}
cmu-cas-request-card-danger = {" "}| ОПАСНО БЛИЗКО
cmu-cas-request-accept = ПРИНЯТЬ
cmu-cas-request-deny = ОТКЛОНИТЬ
cmu-cas-request-complete = ОТРАБОТКА ЗАВЕРШЕНА
cmu-cas-status-pending = ОЖИДАЕТ
cmu-cas-status-accepted = ПРИНЯТ
cmu-cas-status-denied = ОТКЛОНЁН
cmu-cas-status-clearedhot = РАЗРЕШЕНИЕ НА ОГОНЬ
cmu-cas-status-complete = ЗАВЕРШЁН
cmu-cas-request-duplicate = У вас уже есть открытый запрос КАСа по этой цели.
cmu-cas-request-full = Слишком много открытых запросов КАСа. Сначала закройте или отмените существующий.
cmu-cas-request-no-target = Цель не выбрана. Выберите видимую метку или используйте «СВОЯ ПОЗИЦИЯ» в CAS-аплинке.
cmu-cas-request-sent = Запрос КАС #{$id} передан.
cmu-cas-request-not-rto = Для запроса поддержки с воздуха нужна ранцевая радиостанция дальнего радиуса.

cmu-cas-verb-category = Запросить КАС
cmu-cas-verb-request = {$munition}
cmu-cas-verb-clear-hot = Разрешить огонь: #{$id} {$target}
cmu-cas-verb-abort = Отменить: #{$id} {$target}

cmu-cas-munition-any = Любые доступные
cmu-cas-munition-guns = Пушки
cmu-cas-munition-rockets = Ракеты
cmu-cas-munition-bombs = Бомбы
cmu-cas-munition-lasers = Лазерное оружие
cmu-cas-munition-gau = Пушечный подвес GAU-21
cmu-cas-munition-bofors = 40-мм BOFORS
cmu-cas-munition-howitzer = 105-мм гаубица
cmu-cas-munition-missiles = Управляемые ракеты
cmu-cas-munition-chingun = Подбородочная пушка M90

cmu-cas-effect-any = Любой эффект
cmu-cas-effect-highexplosive = Фугасный
cmu-cas-effect-armorpiercing = Бронебойный
cmu-cas-effect-incendiary = Зажигательный
cmu-cas-effect-fragmentation = Осколочно-фугасный / противопехотный
cmu-cas-effect-smoke = Дымовая завеса
cmu-cas-effect-illumination = Осветительный

cmu-cas-mark-flare = сигнальная ракета
cmu-cas-mark-laser = лазерное целеуказание
cmu-cas-mark-radio = радиометка, без ракеты
cmu-cas-radio-mark-name = радиометка {$id}
cmu-cas-request-out-of-range = Эта метка за пределами дальности вызова аплинка ({$range} клеток).
cmu-cas-request-no-los = Для вызова этой цели нужна прямая видимость. Стены блокируют обзор; окна — нет.
cmu-cas-cancel-rto-range = РТО покинул 45-клеточную зону контроля цели или стал недоступен.
cmu-cas-radio-request-remarks = Примечания к запросу {$id}: метка — {$mark}, эффект — {$effect}. {$remarks}

cmu-cas-request-card-effect = ЭФФЕКТ: {$effect}
cmu-cas-request-card-radio-mark = МЕТКА: РАДИО, БЕЗ РАКЕТЫ
cmu-cas-request-card-laser = МЕТКА: ЛАЗЕР (+25% ТОЧНОСТИ)
cmu-cas-request-card-remarks = ПРИМЕЧАНИЯ: {$remarks}

cmu-cas-carry-undecided = АВТО-ОТРАБОТКА ЧЕРЕЗ {$seconds} с
cmu-cas-carry-auto = АВТО-ОТРАБОТКА: {$volleys} залпов
cmu-cas-carry-manual = РУЧНАЯ ОТРАБОТКА
cmu-cas-carry-auto-button = АВТО
cmu-cas-carry-manual-button = ВРУЧНУЮ
cmu-cas-radio-carry-auto = {$aircraft}, управление огнём приняло запрос {$id} для {$callsign}. Автоматический удар бортовыми боеприпасами.
cmu-cas-radio-carry-manual = {$aircraft} выполняет запрос {$id} для {$callsign} вручную.

cmu-cas-uplink-no-faction = Аплинк не может аутентифицировать вас ни в одной командной сети.
cmu-cas-call-title = КАС-АПЛИНК — ЗАПРОС ПО 9-ЛИНЕЙНОЙ ФОРМЕ
cmu-cas-call-header = ПОЗЫВНОЙ {$callsign} | ДАЛЬНОСТЬ ВЫЗОВА {$range} КЛЕТОК
cmu-cas-relay-position = ПЕРЕДАТЬ ПОЗИЦИЮ ВМЕСТО JTAC
cmu-cas-relay-position-help = Передаёт ваши текущие координаты X/Y с ручного GPS. Не создаёт запрос КАС и не применяет смещения или отклонения.
cmu-cas-radio-position = {$callsign}: моя позиция GPS X {$x}, Y {$y}, {$area}.
cmu-cas-position-radio-off = Включите радиостанцию перед передачей позиции.
cmu-cas-position-unavailable = Ваша текущая позиция GPS недоступна.
cmu-cas-call-targets = ВИДИМЫЕ МЕТКИ В ЗОНЕ ДОСЯГАЕМОСТИ
cmu-cas-call-no-marks = В зоне досягаемости нет видимых ракет или лазерных пятен.
cmu-cas-call-own-position = СВОЯ ПОЗИЦИЯ (вызов со смещением)
cmu-cas-call-offset-x = Восток (+) / Запад (−)
cmu-cas-call-offset-y = Север (+) / Юг (−)
cmu-cas-call-position-deviation = Начало координат показано выше. Смещения считаются относительно вашей позиции на момент отправки; радиодальность добавляет до {$deviation} клеток случайного отклонения. Видимый противник не требуется.
cmu-cas-position-invalid = Смещения позиции должны быть от −{$limit} до +{$limit} клеток.
cmu-cas-position-mark-name = Радиопозиция {$id}
cmu-cas-call-target-none = {$name} (в зоне досягаемости ничего не видно)
cmu-cas-call-target-entry = {$name} — {$distance} м{$danger}{$laser}
cmu-cas-call-danger-tag = {" "}[ОПАСНО БЛИЗКО]
cmu-cas-call-laser-tag = {" "}[ЛАЗЕР]
cmu-cas-call-preview = ПРЕДПРОСМОТР ПЕРЕДАЧИ
cmu-cas-call-pick = Выберите метку для предпросмотра запроса.
cmu-cas-call-line-mark = МЕТКА: {$value}
cmu-cas-call-line-target = ЦЕЛЬ: {$value}
cmu-cas-call-line-location = МЕСТОПОЛОЖЕНИЕ: {$value}
cmu-cas-call-line-description = ОПИСАНИЕ: {$value}
cmu-cas-call-line-friendlies = СВОИ: {$value}
cmu-cas-call-line-distance = РАССТОЯНИЕ ОТ ВАС: {$value} клеток
cmu-cas-call-line-danger = ВНИМАНИЕ: {$value}
cmu-cas-call-line-laser = {$value}
cmu-cas-call-laser-bonus = Лазерное целеуказание: разброс удара уменьшен на 25%.
cmu-cas-call-munition = Система вооружения
cmu-cas-call-effect = Желаемый эффект
cmu-cas-call-remarks = Примечания
cmu-cas-call-remarks-placeholder = Необязательные примечания для экипажа
cmu-cas-call-send = ПЕРЕДАТЬ ЗАПРОС
cmu-cas-call-requests = ВАШИ ЗАПРОСЫ
cmu-cas-call-request-entry = #{$id} {$target} | {$munition} / {$effect} | {$status} | {$aircraft} {$carry}
cmu-cas-call-clear-hot = РАЗРЕШЕНИЕ НА ОГОНЬ
cmu-cas-call-abort = ОТМЕНИТЬ

cmu-cas-target-none = противник не наблюдается
cmu-cas-target-hostiles = {$count ->
    [one] один противник на открытом месте
   *[other] {$count} противников на открытом месте
}
cmu-cas-friendlies-none = в радиусе пятнадцати клеток никого
cmu-cas-friendlies-nearest = ближайший {$distance} клеток {$direction}

cmu-cas-radio-request = {$callsign}, запрос КАС {$id}. Метка: {$target}, {$location}. Цель: {$description}. Свои: {$friendlies}. Боеприпасы: {$munition}.{$danger} Ожидаю подтверждения.
cmu-cas-radio-danger-close = {" "}ОПАСНО БЛИЗКО.
cmu-cas-radio-cleared = {$callsign}: {$aircraft}, вы РАЗРЕШЕНЫ НА ОГОНЬ по {$target}.
cmu-cas-radio-abort = {$callsign}: ОТМЕНА, ОТМЕНА, ОТМЕНА запроса {$id}. Огонь не открывать.
cmu-cas-radio-accepted = {$aircraft}: принял {$callsign}, запрос {$id} по {$target} принят. Иду на цель, ожидайте разрешения на огонь.
cmu-cas-radio-denied = {$aircraft}: {$callsign}, не могу выполнить запрос {$id}. Поддержка в данный момент невозможна.
cmu-cas-radio-complete = {$aircraft}: {$callsign}, отработка по {$target} завершена. {$bda}
cmu-cas-bda = Запрашиваю BDA: {$down} поражено, {$standing} ещё стоят.
cmu-cas-bda-unknown = Запрашиваю BDA.

cmu-cas-danger-close-warning = ОПАСНО БЛИЗКО: {$count ->
    [one] один свой
   *[other] {$count} своих
} рядом с точкой удара. Для подтверждения откройте огонь повторно в течение {$seconds} секунд.

cmu-cas-clearance-required = Требуется разрешение РТО. Дождитесь РАЗРЕШЕНИЯ НА ОГОНЬ перед выполнением этой задачи.
cmu-cas-cancel-target-lost = По указанной позиции противник не остался.
cmu-cas-cancel-no-weapon = Запрошенное оружие недоступно для автоматической отработки.
cmu-cas-cancel-unable = Запрошенное оружие не смогло выстрелить. Повторите запрос, проверив загрузку и режим полёта.
cmu-cas-radio-cancelled = Запрос КАС #{$id} отменён: {$reason}
cmu-cas-invalid-gps = Этот наземный доклад недоступен или больше не авторизован.
cmu-cas-ground-report = {$observer}: {$map}, GPS X {$x}, Y {$y}
cmu-cas-gps-mark-name = наземный доклад {$id}
cmu-cas-call-gps-help = Наземные доклады предоставляются авторизованной службой наземных докладов. Доступность перепроверяется при передаче.
# V2 хирургия — строки интерфейса и названия операций.

# ---- Окно ------------------------------------------------------------

cmu-medical-surgery-window-title = Хирургическая операция
cmu-medical-surgery-window-hint = Выберите часть тела, выберите операцию, затем нажмите на пациента нужным инструментом.
cmu-medical-surgery-no-eligible = Здесь нет доступных операций.
cmu-medical-surgery-section-parts = Части тела
cmu-medical-surgery-section-surgeries = Операции
cmu-medical-surgery-section-surgeries-on = Операции на: { $part }
cmu-medical-surgery-arm-button = Начать операцию
cmu-medical-surgery-cancel-armed = Отменить операцию
cmu-medical-surgery-step-hint = Шаг { $step }/{ $total } — { $label } ({ $tool })
cmu-medical-surgery-step-hint-prereq = Подготовительный шаг { $step }/{ $total } — { $label } ({ $tool })
cmu-medical-surgery-armed-heading = ПОДГОТОВЛЕНО

# ---- Блок текущей операции ------------------------------------------

cmu-medical-surgery-in-progress-heading = ОПЕРАЦИЯ ИДЁТ
cmu-medical-surgery-in-progress-subtitle = { $surgery } · { $part }
cmu-medical-surgery-in-progress-credit = Начал: { $surgeon } · { $elapsed } назад
cmu-medical-surgery-step-now = Шаг { $step } из { $total }: { $label }
cmu-medical-surgery-action-hint = Нажмите на { $part }, держа в руке { $tool }.
cmu-medical-surgery-action-hint-no-tool = Нажмите на { $part }, чтобы продолжить.
cmu-medical-surgery-continue-button = Продолжить операцию
cmu-medical-surgery-abandon-button = Бросить операцию

# ---- Статусы частей тела --------------------------------------------

cmu-medical-surgery-part-heading = { $part }
cmu-medical-surgery-part-condition-healthy = Здорова
cmu-medical-surgery-part-condition-locked = На { $other } уже идёт другая операция — сначала завершите или бросьте её
cmu-medical-surgery-part-condition-no-eligible = Нет доступных операций

cmu-medical-surgery-condition-incision-open = Разрез открыт
cmu-medical-surgery-condition-ribcage-open = Грудная клетка раскрыта
cmu-medical-surgery-condition-fracture = { $severity } перелом
cmu-medical-surgery-condition-internal-bleed = Внутреннее кровотечение
cmu-medical-surgery-condition-in-progress = Операция в процессе
cmu-medical-surgery-condition-missing = Отсечена

# ---- Категории в BUI ------------------------------------------------

cmu-medical-surgery-category-fracture = Переломы
cmu-medical-surgery-category-bleed = Внутренние кровотечения
cmu-medical-surgery-category-burn = Ожоги
cmu-medical-surgery-category-remove_organ = Извлечение органов
cmu-medical-surgery-category-transplant = Пересадка органов
cmu-medical-surgery-category-suture = Ушивание органов
cmu-medical-surgery-category-head_organ = Операции на голове
cmu-medical-surgery-category-amputation = Удаление конечности
cmu-medical-surgery-category-reattach = Пришивание конечности
cmu-medical-surgery-category-parasite = Удаление паразита
cmu-medical-surgery-category-close_up = Закрытие
cmu-medical-surgery-category-general = Прочее

# ---- Осмотр ---------------------------------------------------------

cmu-medical-surgery-examine-patient-in-progress = [color=#dca94c]Идёт операция «{ $surgery }» (хирург: { $surgeon }) — далее: { $next }.[/color]
cmu-medical-surgery-examine-part-in-progress = [color=#dca94c]На этой части тела идёт операция «{ $surgery }» (хирург: { $surgeon }) — далее: { $next }.[/color]
cmu-medical-surgery-examine-part-abandoned = [color=#888888]Открытая рана — операция не выполняется.[/color]

# ---- Закрывающие шаги -----------------------------------------------

cmu-medical-surgery-step-close-incision-label = Закрыть разрез
cmu-medical-surgery-step-mend-ribcage-label = Восстановить грудную клетку
cmu-medical-surgery-step-close-bones-label = Закрыть кости

# ---- Вооружённый шаг ------------------------------------------------

cmu-medical-surgery-armed-none = (операция не выбрана)
cmu-medical-surgery-armed-step = Подготовлено: { $surgery } — шаг { $step } ({ $tool })
cmu-medical-surgery-armed-cancelled = Операция отменена.
cmu-medical-surgery-armed-expired = Выбор операции истёк.

# ---- Всплывающие сообщения ------------------------------------------

cmu-medical-surgery-wrong-part = Это не та часть тела, для которой была выбрана операция.
cmu-medical-surgery-wrong-tool = Для этого шага нужен другой инструмент.
cmu-medical-surgery-wrong-tool-damage = Вы соскальзываете с инструментом { $tool }!
cmu-medical-surgery-no-tool = Для этого шага нужен хирургический инструмент.
cmu-medical-surgery-wrong-limb = Эта конечность не подходит ни к одному пустому слоту пациента.

# ---- Категории инструментов -----------------------------------------

cmu-medical-surgery-tool-category-scalpel = Скальпель
cmu-medical-surgery-tool-category-hemostat = Гемостат
cmu-medical-surgery-tool-category-retractor = Ретрактор
cmu-medical-surgery-tool-category-cautery = Прижигатель
cmu-medical-surgery-tool-category-bone_saw = Костная пила
cmu-medical-surgery-tool-category-bone_setter = Костный фиксатор
cmu-medical-surgery-tool-category-bone_gel = Костный гель
cmu-medical-surgery-tool-category-bone_graft = Костный трансплантат
cmu-medical-surgery-tool-category-organ_clamp = Зажим для органов
cmu-medical-surgery-tool-category-scalpel_or_burn_kit = Скальпель или набор для ожогов

# ---- Названия шагов -------------------------------------------------

cmu-medical-surgery-step-realign-simple-label = Сопоставить простой перелом
cmu-medical-surgery-step-realign-compound-label = Сопоставить сложный перелом
cmu-medical-surgery-step-realign-comminuted-label = Сопоставить оскольчатый перелом
cmu-medical-surgery-step-apply-gel-label = Нанести костный гель
cmu-medical-surgery-step-apply-gel-second-label = Нанести костный гель (второй слой)
cmu-medical-surgery-step-insert-graft-label = Установить костный трансплантат
cmu-medical-surgery-step-cauterize-bleed-label = Пережать внутреннее кровотечение
cmu-medical-surgery-step-clamp-liver-label = Пережать сосуды печени
cmu-medical-surgery-step-clamp-lungs-label = Пережать сосуды лёгких
cmu-medical-surgery-step-clamp-kidneys-label = Пережать сосуды почек
cmu-medical-surgery-step-clamp-heart-label = Пережать сосуды сердца
cmu-medical-surgery-step-clamp-stomach-label = Пережать сосуды желудка
cmu-medical-surgery-step-extract-liver-label = Извлечь печень
cmu-medical-surgery-step-extract-lungs-label = Извлечь лёгкие
cmu-medical-surgery-step-extract-kidneys-label = Извлечь почки
cmu-medical-surgery-step-extract-heart-label = Извлечь сердце
cmu-medical-surgery-step-extract-stomach-label = Извлечь желудок
cmu-medical-surgery-step-reinsert-liver-label = Установить новую печень
cmu-medical-surgery-step-reinsert-lungs-label = Установить новые лёгкие
cmu-medical-surgery-step-reinsert-kidneys-label = Установить новые почки
cmu-medical-surgery-step-reinsert-stomach-label = Установить новый желудок
cmu-medical-surgery-step-transplant-heart-label = Пересадить донорское сердце
cmu-medical-surgery-step-suture-liver-label = Ушить печень
cmu-medical-surgery-step-suture-lungs-label = Ушить лёгкие
cmu-medical-surgery-step-suture-kidneys-label = Ушить почки
cmu-medical-surgery-step-suture-heart-label = Ушить сердце
cmu-medical-surgery-step-suture-stomach-label = Ушить желудок
cmu-medical-surgery-step-amputate-limb-label = Ампутировать конечность
cmu-medical-surgery-step-reattach-limb-label = Пришить отсечённую конечность
cmu-medical-surgery-step-trim-necrotic-stump-label = Обработать некротическую культю
cmu-medical-surgery-step-prep-reattach-socket-label = Подготовить место пришивания
cmu-medical-surgery-step-debride-eschar-label = Удалить некроз

# ---- Названия операций ----------------------------------------------

cmu-medical-surgery-name-set-fracture = Вправление перелома
cmu-medical-surgery-name-stop-internal-bleeding = Остановить внутреннее кровотечение
cmu-medical-surgery-name-remove-liver = Удаление печени
cmu-medical-surgery-name-remove-lungs = Удаление лёгких
cmu-medical-surgery-name-remove-kidneys = Удаление почек
cmu-medical-surgery-name-remove-heart = Удаление сердца
cmu-medical-surgery-name-remove-stomach = Удаление желудка
cmu-medical-surgery-name-replace-liver = Замена печени
cmu-medical-surgery-name-replace-lungs = Замена лёгких
cmu-medical-surgery-name-replace-kidneys = Замена почек
cmu-medical-surgery-name-transplant-heart = Пересадка сердца
cmu-medical-surgery-name-replace-stomach = Замена желудка
cmu-medical-surgery-name-suture-liver = Ушивание печени
cmu-medical-surgery-name-suture-lungs = Ушивание лёгких
cmu-medical-surgery-name-suture-kidneys = Ушивание почек
cmu-medical-surgery-name-suture-heart = Ушивание сердца
cmu-medical-surgery-name-suture-stomach = Ушивание желудка
cmu-medical-surgery-name-repair-brain = Восстановление мозга
cmu-medical-surgery-name-repair-eyes = Восстановление глаз
cmu-medical-surgery-name-repair-ears = Восстановление ушей
cmu-medical-surgery-name-remove-limb = Удаление конечности
cmu-medical-surgery-name-reattach-limb = Пришивание конечности
cmu-medical-surgery-name-remove-larva = Удаление личинки
cmu-medical-surgery-name-debride-eschar = Удаление струпа

# Missing entries synced from en-US

cmu-medical-surgery-section-patient = Пациент

cmu-medical-surgery-section-workflow = Рабочий процесс

cmu-medical-surgery-workflow-ready = Активная процедура не выбрана.

cmu-medical-surgery-workflow-active = { $surgery } активен на { $part }.

cmu-medical-surgery-no-part-selected = Выберите часть тела.

cmu-medical-surgery-procedure-detail = { $step } / { $tool }

cmu-medical-surgery-choose-next-heading = Выбрать следующую операцию

cmu-medical-surgery-choose-next-hint = Продолжите еще один ремонт этой открытой части или закройте ее.

cmu-medical-surgery-continue-with-button = Продолжить с { $surgery }

cmu-medical-surgery-close-up-button = Закрыть

cmu-medical-surgery-actions-heading = Действия

# ---- Per-part section labels -----------------------------------------

cmu-medical-surgery-condition-skull-open = Открытый череп

cmu-medical-surgery-condition-bones-open = Открытые кости

cmu-medical-surgery-condition-eschar = Эшар

cmu-medical-surgery-step-mend-skull-label = Починить череп

cmu-medical-surgery-step-mend-bones-label = Починить кости

cmu-medical-surgery-auto-armed = Выбран { $surgery }.

cmu-medical-surgery-auto-continue = Продолжаем { $surgery }.

cmu-medical-surgery-choose-repair-or-close = Выберите ремонт органов или закройте их.

# ---- Click-target popups ---------------------------------------------

cmu-medical-surgery-improvised-mishap = Импровизированный { $tool } соскальзывает и причиняет дополнительную травму.

cmu-medical-surgery-step-failed = Операция соскальзывает и приводит к травме.

cmu-medical-surgery-step-failed-with-tool = { $tool } соскальзывает и вызывает хирургическую травму.

cmu-medical-surgery-missing-skills = Вы не знаете, как выполнить этот шаг.

cmu-medical-surgery-cannot-start = Эта операция больше не доступна.

cmu-medical-surgery-needs-operating-table = Сначала перенесите их на операционный стол.

cmu-medical-surgery-remove-helmet = Сначала снимите шлем.

cmu-medical-surgery-remove-armor = Сначала снимите препятствующую броню.

cmu-medical-surgery-welder-not-lit = Сначала зажгите инструмент.

cmu-medical-surgery-patient-not-lying = Пациент должен лежать или быть привязанным к операционному столу.

cmu-medical-surgery-patient-not-controlled = Перед операцией пациенту необходима анестезия, сильные обезболивающие или средства фиксации.

cmu-medical-surgery-self-pain-control = Самостоятельная операция требует в первую очередь сильных обезболивающих.

cmu-medical-surgery-self-not-secured = Прежде чем приступить к самостоятельной операции, пристегнитесь к стулу, кровати или ролику.

cmu-medical-surgery-self-not-allowed = Вы не можете сделать такую операцию себе.

cmu-medical-surgery-step-pain-interrupted = Боль пациента прерывает хирургический этап.

cmu-medical-amputation-success = Конечность удаляется.

# ---- Tool category names (used in the BUI button + armed line) -------

cmu-medical-surgery-tool-category-severed_limb = Соответствующая конечность

cmu-medical-surgery-tool-category-blowtorch = Горящий сварщик

cmu-medical-surgery-tool-category-cable_coil = Кабельная катушка

# ---- Per-step labels -------------------------------------------------

cmu-autodoc-window-title = Автодок

cmu-autodoc-no-patient = Нет пациента

cmu-autodoc-status-no-pod = Поблизости нет подключенных модулей автодоков.

cmu-autodoc-status-empty = Связанный модуль пуст.

cmu-autodoc-status-ready = Готов поставить в очередь автоматизированные процедуры.

cmu-autodoc-status-running = Выполнение процедур в очереди.

cmu-autodoc-current-idle = Текущая процедура: простой

cmu-autodoc-current-step = Текущая процедура: { $step }.

cmu-autodoc-current-step-timed = Текущая процедура: { $step } (остался { $time })

cmu-autodoc-current-step-detail = { $surgery } / { $part } / { $step }

cmu-autodoc-start-button = Старт

cmu-autodoc-stop-button = Стоп

cmu-autodoc-clear-button = Очистить

cmu-autodoc-eject-button = Извлечь пациента

cmu-autodoc-remove-button = Удалить

cmu-autodoc-queue-button = Очередь

cmu-autodoc-queue-heading = Очередь

cmu-autodoc-parts-heading = Части

cmu-autodoc-surgeries-heading = Операции

cmu-autodoc-queue-empty = Никаких процедур в очереди.

cmu-autodoc-queue-summary = { $count } процедуры в очереди

cmu-autodoc-available-procedures = { $count } доступные процедуры

cmu-autodoc-part-procedures = { $count } процедуры

cmu-autodoc-surgery2-required = Для постановки в очередь шагов autodoc требуется обучение хирургии 2.

cmu-autodoc-no-surgeries = Операций здесь нет.

cmu-autodoc-queue-row = #{ $index } { $surgery } on { $part } - { $step }

cmu-autodoc-surgery-row = { $surgery } - { $step }

cmu-autodoc-automated-step-label = Автоматизированный цикл ремонта

cmu-autodoc-automated-step-note = Автодок ремонтирует эту цель с помощью машинного таймера.

cmu-autodoc-repair-wounds-surgery = Ремонт ран/ожогов

cmu-autodoc-procedure-time-note = { $time } автоматизированная процедура.

cmu-autodoc-minutes = { $minutes } мин.

# ---- Body scanner ----------------------------------------------------

cmu-body-scanner-window-title = Сканер тела

cmu-body-scanner-no-patient = Нет пациента

cmu-body-scanner-status-no-pod = Поблизости нет модуля сканера тела.

cmu-body-scanner-status-empty = Модуль связанного сканера пуст.

cmu-body-scanner-status-ready = Сканирование пациента готово.

cmu-body-scanner-status-no-skill = Для выполнения сканирования требуется обучение хирургии 1.

cmu-body-scanner-boost-active = Хирургическая помощь откалибрована: осталось { $time }.

cmu-body-scanner-boost-inactive = Хирургическая помощь не откалибрована.

cmu-body-scanner-scan-heading = Сканировать

cmu-body-scanner-terms-heading = Слои срезов

cmu-body-scanner-targets-heading = Активные показания срезов

cmu-body-scanner-start-button = Начать калибровку

cmu-body-scanner-reset-button = Сброс калибровки

cmu-body-scanner-eject-button = Извлечь пациента

cmu-body-scanner-surgery1-required = Для сканирования тела требуется обучение хирургии 1.

cmu-body-scanner-no-scan-lines = Нет данных сканирования.

cmu-body-scanner-diagnostic-summary = Диагностические линии { $count }

cmu-body-scanner-match-summary = { $matched }/{ $required } заблокирован, { $time } остался

cmu-body-scanner-match-summary-idle = { $matched }/{ $required } заблокирован, не запускается

cmu-body-scanner-calibrated-summary = Откалибровано, осталось ассистентов { $time }

cmu-body-scanner-calibrated-badge = КАЛИБРОВАННЫЙ { $time }

cmu-body-scanner-calibration-ready = 2:00

cmu-body-scanner-lockout-summary = Активный фрагмент заблокирован, остался { $time }

cmu-body-scanner-lockout-status = Активный фрагмент заблокирован: осталось { $time }.

cmu-body-scanner-lockout-detail = Калибровка не удалась. Подождите, пока блокировка исчезнет.

cmu-body-scanner-no-surgical-targets = Цели не обнаружены.

cmu-body-scanner-no-surgical-targets-detail = Никакого повышения не присуждается.

cmu-body-scanner-calibration-heading = Сканирование анатомических срезов

cmu-body-scanner-sweep-title = Многоуровневое сканирование сканера

cmu-body-scanner-sweep-detail = Для начала настройте фрагмент.

cmu-body-scanner-layer-selected = Настроенный срез — { $locked }/{ $total } заблокирован

cmu-body-scanner-layer-ready = { $locked }/{ $total } заблокирован

cmu-body-scanner-layer-empty = Никаких аномальных показаний

cmu-body-scanner-signal-locked = Сигнал заблокирован

cmu-body-scanner-signal-ready = { $detail } - блокировка на циане

cmu-body-scanner-start-status = Запустите калибровку, чтобы начать сканирование срезов.

cmu-body-scanner-ready-status = Настройте срез, затем зафиксируйте аномальные показания, пока развертка имеет голубой цвет.

cmu-body-scanner-armed-status = Срез настроен: { $layer }. Зафиксируйте показания, когда развертка станет голубым.

cmu-body-scanner-penalty-status = Неудачное время или фрагмент: -{ $seconds }s.

cmu-body-scanner-feedback-correct = Сигнал заблокирован.

cmu-body-scanner-feedback-wrong-timing = Свип пропустил полосу захвата: -{ $seconds }s.

cmu-body-scanner-feedback-wrong-layer = Интерференция слоев: -{ $seconds }s.

cmu-body-scanner-expired-status = Время истекло. Сбросьте калибровку и повторите попытку.

cmu-body-scanner-complete-status = Все показания заблокированы. Калиброванная хирургическая помощь.

cmu-body-scanner-timer-active = АКТИВНЫЙ ТАЙМЕР НАРЕЗКИ

cmu-body-scanner-timer-expired = ТАЙМЕР ИСТЕК

cmu-body-scanner-timer-locked = СЛАЙС ЗАБЛОКИРОВАН

cmu-body-scanner-timer-detail = Зафиксируйте показания до закрытия окна сканирования.

cmu-body-scanner-no-layer-signals = Никаких аномальных показаний на { $layer } нет.

cmu-body-scanner-interference-title = Неразрешенное чтение

cmu-body-scanner-interference-detail = Помехи на { $layer }

cmu-body-scanner-decoy-ready = { $detail } — шумное эхо

cmu-body-scanner-decoy-vitals-1 = Сердечный эхо-спайк

cmu-body-scanner-decoy-vitals-2 = Мерцание кислорода в крови

cmu-body-scanner-decoy-detail-vitals = преходящий жизненно важный артефакт

cmu-body-scanner-decoy-skeleton-1 = Тень от кости

cmu-body-scanner-decoy-skeleton-2 = Призрак выравнивания суставов

cmu-body-scanner-decoy-detail-skeleton = нестабильный силуэт кости

cmu-body-scanner-decoy-organs-1 = Мягкое цветение органов

cmu-body-scanner-decoy-organs-2 = Отражение плотности

cmu-body-scanner-decoy-detail-organs = непостоянная плотность органов

cmu-body-scanner-decoy-tissue-1 = Вспышка поверхностных тканей

cmu-body-scanner-decoy-tissue-2 = Диапазон сосудистого шума

cmu-body-scanner-decoy-detail-tissue = шумный возврат мягких тканей

cmu-body-scanner-triage-stable = Стабильные показания

cmu-body-scanner-triage-serious = Серьезные выводы

cmu-body-scanner-triage-critical = Критические выводы

cmu-body-scanner-triage-clear = Никаких немедленных аномальных результатов.

cmu-body-scanner-health-stable = Стабильный

cmu-body-scanner-health-damaged = Поврежденный

cmu-body-scanner-health-critical = Критический

cmu-body-scanner-section-vitals = Жизненно важные органы

cmu-body-scanner-section-body = Тело

cmu-body-scanner-section-organs = Органы

cmu-body-scanner-term-assigned = { $term } -> { $target }

cmu-body-scanner-target-filled = { $target }: { $term }

cmu-body-scanner-line-state = Состояние: { $state }

cmu-body-scanner-line-damage = Урон: общий { $total } (грубить { $brute }, сжечь { $burn })

cmu-body-scanner-line-blood = Кровь: { $blood } / { $max }

cmu-body-scanner-heart-stopped = Сердце: активность не обнаружена

cmu-body-scanner-heart-active = Сердце: { $bpm } ударов в минуту.

cmu-body-scanner-line-no-data = Диагностических данных нет.

cmu-body-scanner-line-part = { $part }: { $details }

cmu-body-scanner-part-health = HP { $current } / { $max }

cmu-body-scanner-part-wounds = { $count } необработанная рана(и)

cmu-body-scanner-part-fracture = { $severity } перелом

cmu-body-scanner-part-bleed = внутреннее кровотечение { $rate }/с

cmu-body-scanner-part-eschar = струп

cmu-body-scanner-part-splinted = шинированный

cmu-body-scanner-part-cast = отлитый

cmu-body-scanner-part-tourniquet = жгут

cmu-body-scanner-part-missing-limb = отсутствующая / оторванная конечность

cmu-body-scanner-line-organ = { $organ }: { $stage } ({ $current } / { $max })

cmu-body-scanner-line-missing-organ = Отсутствует { $organ } в { $part }

cmu-body-scanner-signal-heart-stopped = Сердце: активность не обнаружена

cmu-body-scanner-signal-organ-damage = { $organ }: повреждение органов { $stage }.

cmu-body-scanner-signal-low-blood = Низкий объем крови: { $blood } / { $max }

cmu-body-scanner-signal-internal-bleed = { $part }: внутренний выпуск { $rate }/с

cmu-body-scanner-signal-fracture = { $part }: перелом { $severity }

cmu-body-scanner-signal-wounds = { $part }: { $count } необработанная рана(и)

cmu-body-scanner-signal-trauma = { $part }: травма тканей { $current }/{ $max }

cmu-body-scanner-signal-missing-organ = Отсутствует { $organ } в { $part }

cmu-body-scanner-signal-missing-limb = { $part }: отсутствующая / оторванная конечность

cmu-body-scanner-slice-detail-cardiac = сердечный ритм

cmu-body-scanner-slice-detail-organ = плотность органов

cmu-body-scanner-slice-detail-blood = объем крови

cmu-body-scanner-slice-detail-bleed = тканевый поток

cmu-body-scanner-slice-detail-fracture = выравнивание костей

cmu-body-scanner-slice-detail-wound = разрушение тканей

cmu-body-scanner-slice-detail-trauma = плотность мягких тканей

cmu-body-scanner-slice-detail-missing-organ = орган силуэт

cmu-body-scanner-slice-detail-missing-limb = силуэт конечности

cmu-limb-printer-window-title = Принтер конечностей

cmu-limb-printer-header = Изготовление конечностей

cmu-limb-printer-matrix-heading = Матрица синтеза

cmu-limb-printer-blood-heading = Шаблон крови

cmu-limb-printer-no-beaker = Матричный стакан не вставлен.

cmu-limb-printer-no-syringe = Шприц для крови не вставлен.

cmu-limb-printer-fluid-amount = { $current } / { $max }u

cmu-limb-printer-matrix-cost = Матрица { $cost }u на отпечаток

cmu-limb-printer-blood-cost = { $cost }u кровь на отпечаток

cmu-limb-printer-remove-beaker = Удалить стакан

cmu-limb-printer-remove-syringe = Удалить шприц

cmu-limb-printer-left-heading = Левый

cmu-limb-printer-right-heading = Правильно

cmu-limb-printer-print-ready = Готов к печати

cmu-limb-printer-status-ready = Готов синтезировать.

cmu-limb-printer-missing-beaker = Вставьте стакан биогенной матрицы.

cmu-limb-printer-missing-matrix = Биогенная матрица слишком низка.

cmu-limb-printer-missing-syringe = Вставьте шприц с кровью пациента.

cmu-limb-printer-missing-blood = Образец крови пациента слишком мал.

cmu-limb-printer-printed = Распечатал { $limb }.

cmu-limb-printer-left-arm = Левая рука

cmu-limb-printer-left-leg = Левая нога

cmu-limb-printer-right-arm = Правая рука

cmu-limb-printer-right-leg = Правая нога

cmu-limb-printer-slot-beaker = матричный стакан

cmu-limb-printer-slot-syringe = шприц для крови

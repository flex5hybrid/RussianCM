# Строки интерфейса хирургии V2-β.
# - Заголовок окна / подсказка
# - Строка состояния выбранного этапа
# - Сообщения о неверном инструменте / части тела / отсутствии инструмента
# - Названия категорий инструментов (категории резолвера из SharedCMUSurgeryFlowSystem)
# - Названия этапов для всех 19 операций V1 CMU

# ---- Элементы окна ---------------------------------------------------

cmu-medical-surgery-window-title = Хирургическая процедура
cmu-medical-surgery-window-hint = Выберите часть тела, выберите операцию, затем нажмите на пациента требуемым инструментом.
cmu-medical-surgery-no-eligible = Здесь нет доступных операций.
cmu-medical-surgery-section-patient = Пациент
cmu-medical-surgery-section-workflow = Ход операции
cmu-medical-surgery-workflow-ready = Активная процедура не выбрана.
cmu-medical-surgery-workflow-active = { $surgery } выполняется на { $part }.
cmu-medical-surgery-section-parts = Части тела
cmu-medical-surgery-section-surgeries = Операции
cmu-medical-surgery-section-surgeries-on = Операции на { $part }
cmu-medical-surgery-no-part-selected = Выберите часть тела.
cmu-medical-surgery-procedure-detail = { $step } / { $tool }
cmu-medical-surgery-arm-button = Начать операцию
cmu-medical-surgery-cancel-armed = Отменить операцию
cmu-medical-surgery-step-hint = Этап { $step }/{ $total } — { $label } ({ $tool })
cmu-medical-surgery-step-hint-prereq = Предварительный этап { $step }/{ $total } — { $label } ({ $tool })
cmu-medical-surgery-armed-heading = ВЫБРАНО

# ---- Панель выполняемой операции ------------------------------------

cmu-medical-surgery-in-progress-heading = ВЫПОЛНЯЕТСЯ
cmu-medical-surgery-in-progress-subtitle = { $surgery } · { $part }
cmu-medical-surgery-in-progress-credit = Последний этап выполнил { $surgeon } · начато { $elapsed } назад
cmu-medical-surgery-step-now = Этап { $step }: { $label }
cmu-medical-surgery-action-hint = Нажмите на { $part } с помощью { $tool }.
cmu-medical-surgery-action-hint-no-tool = Нажмите на { $part }, чтобы продолжить.
cmu-medical-surgery-choose-next-heading = Выберите следующую операцию
cmu-medical-surgery-choose-next-hint = Выполните ещё одну процедуру на этой открытой части тела или закройте операционное поле.
cmu-medical-surgery-continue-with-button = Продолжить: { $surgery }
cmu-medical-surgery-close-up-button = Закрыть операционное поле
cmu-medical-surgery-continue-button = Продолжить операцию
cmu-medical-surgery-abandon-button = Прервать операцию
cmu-medical-surgery-actions-heading = Действия

# ---- Состояния частей тела -------------------------------------------

cmu-medical-surgery-part-heading = { $part }
cmu-medical-surgery-part-condition-healthy = Здорова
cmu-medical-surgery-part-condition-locked = На { $other } выполняется другая операция — сначала завершите или прервите её
cmu-medical-surgery-part-condition-no-eligible = Нет доступных операций

cmu-medical-surgery-condition-incision-open = Открытый разрез
cmu-medical-surgery-condition-ribcage-open = Грудная клетка вскрыта
cmu-medical-surgery-condition-skull-open = Череп вскрыт
cmu-medical-surgery-condition-bones-open = Кости вскрыты
cmu-medical-surgery-condition-fracture = Перелом: { $severity }
cmu-medical-surgery-condition-internal-bleed = Внутреннее кровотечение
cmu-medical-surgery-condition-eschar = Струп
cmu-medical-surgery-condition-wounds = Раны
cmu-medical-surgery-condition-damaged = Повреждено
cmu-medical-surgery-condition-vascular-tear = Разрыв сосуда
cmu-medical-surgery-condition-embedded-foreign-body = Инородное тело
cmu-medical-surgery-condition-compartment-pressure = Компартмент-синдром
cmu-medical-surgery-condition-contaminated-wound = Загрязнённая рана
cmu-medical-surgery-condition-bone-splinters = Костные осколки
cmu-medical-surgery-condition-organ-adhesion = Спайки органа
cmu-medical-surgery-condition-organ-hemorrhage = Кровотечение органа
cmu-medical-surgery-condition-in-progress = Выполняется операция
cmu-medical-surgery-condition-missing = Отсечена

# ---- Заголовки категорий BUI -----------------------------------------

cmu-medical-surgery-category-fracture = Перелом
cmu-medical-surgery-category-bleed = Внутреннее кровотечение
cmu-medical-surgery-category-burn = Ожоги
cmu-medical-surgery-category-remove_organ = Удаление органа
cmu-medical-surgery-category-transplant = Пересадка органа
cmu-medical-surgery-category-suture = Ушивание органа
cmu-medical-surgery-category-head_organ = Операция на голове
cmu-medical-surgery-category-amputation = Ампутация конечности
cmu-medical-surgery-category-reattach = Пришивание конечности
cmu-medical-surgery-category-parasite = Удаление паразита
cmu-medical-surgery-category-close_up = Закрытие операционного поля
cmu-medical-surgery-category-general = Прочее

# ---- Осмотр (CMUSurgeryStateExamineSystem) ---------------------------

cmu-medical-surgery-examine-patient-in-progress = [color=#dca94c]Выполняется { $surgery } (последний этап выполнил { $surgeon }) — далее: { $next }.[/color]
cmu-medical-surgery-examine-part-in-progress = [color=#dca94c]Выполняется { $surgery } (последний этап выполнил { $surgeon }) — далее: { $next }.[/color]
cmu-medical-surgery-examine-part-abandoned = [color=#888888]Открытая рана — операция не выполняется.[/color]

cmu-medical-surgery-examine-incision = [color=#888888]На { $part } имеется хирургический разрез.[/color]
cmu-medical-surgery-examine-site-details = [color=#dca94c]{ $part }: { $access }; { $hemostasis }; текущий этап: { $step }.[/color]
cmu-medical-surgery-examine-no-active-step = активная процедура отсутствует
cmu-medical-surgery-access-closed = закрыто
cmu-medical-surgery-access-incised = только разрез
cmu-medical-surgery-access-shallow = поверхностный доступ
cmu-medical-surgery-access-bone-cut = кость распилена, но ещё не раскрыта
cmu-medical-surgery-access-deep = глубокий доступ
cmu-medical-surgery-hemostasis-none = хирургическое кровотечение отсутствует
cmu-medical-surgery-hemostasis-uncontrolled = неконтролируемое хирургическое кровотечение
cmu-medical-surgery-hemostasis-clamped = кровоточащие сосуды пережаты

# ---- Этапы закрытия (резервные ключи RMC) ----------------------------

cmu-medical-surgery-step-close-incision-label = Закрыть разрез
cmu-medical-surgery-step-mend-ribcage-label = Восстановить грудную клетку
cmu-medical-surgery-step-mend-skull-label = Восстановить череп
cmu-medical-surgery-step-mend-bones-label = Восстановить кости
cmu-medical-surgery-step-close-bones-label = Закрыть кости

# ---- Состояние выбранного этапа --------------------------------------

cmu-medical-surgery-armed-none = (операция не выбрана)
cmu-medical-surgery-armed-step = Выбрано: { $surgery } — этап { $step } ({ $tool })
cmu-medical-surgery-armed-cancelled = Операция отменена.
cmu-medical-surgery-armed-expired = Время выбора операции истекло.
cmu-medical-surgery-auto-armed = Выбрано: { $surgery }.
cmu-medical-surgery-ui-less-select-part = Выберите часть тела перед использованием хирургического инструмента.
cmu-medical-surgery-ui-less-no-action = Для этого инструмента нет подходящего действия на выбранном операционном поле.
cmu-medical-surgery-unclamped-closure = Разрез закрыт при неконтролируемом кровотечении, что приводит к внутреннему кровотечению.
cmu-medical-surgery-amputation-cancelled = Вы тампонируете разрез и отменяете запланированную ампутацию.
cmu-medical-surgery-auto-continue = Продолжается: { $surgery }.
cmu-medical-surgery-choose-repair-or-close = Выберите восстановление органа или закройте операционное поле.

# ---- Всплывающие сообщения при нажатии -------------------------------

cmu-medical-surgery-wrong-part = Это не та часть тела, для которой была выбрана операция.
cmu-medical-surgery-wrong-tool = Для этого этапа нужен другой инструмент.
cmu-medical-surgery-wrong-tool-damage = { $tool } соскальзывает у вас в руках!
cmu-medical-surgery-improvised-mishap = Импровизированный { $tool } соскальзывает и наносит дополнительную травму.
cmu-medical-surgery-step-failed = Во время операции происходит ошибка, вызывающая травму.
cmu-medical-surgery-step-failed-with-tool = { $tool } соскальзывает и наносит хирургическую травму.
cmu-medical-surgery-no-tool = Для выполнения этого этапа нужен хирургический инструмент.
cmu-medical-surgery-missing-skills = Вы не знаете, как выполнить этот этап.
cmu-medical-surgery-cannot-start = Эта операция больше недоступна.
cmu-medical-surgery-step-busy = На этом пациенте уже выполняется другое хирургическое действие.
cmu-medical-surgery-needs-operating-table = Сначала переместите пациента на операционный стол.
cmu-medical-surgery-remove-helmet = Сначала снимите с пациента шлем.
cmu-medical-surgery-remove-armor = Сначала снимите мешающую броню.
cmu-medical-surgery-wrong-limb = Эта конечность не соответствует ни одному свободному месту у пациента.
cmu-medical-surgery-welder-not-lit = Сначала включите инструмент.
cmu-medical-surgery-patient-not-lying = Пациент должен лежать или быть зафиксирован на операционном столе.
cmu-medical-surgery-patient-not-controlled = Перед операцией пациенту требуется анестезия, сильное обезболивающее или фиксация.
cmu-medical-surgery-self-pain-control = Для операции на себе сначала требуется сильное обезболивающее.
cmu-medical-surgery-self-not-secured = Перед операцией на себе пристегнитесь к стулу, кровати или каталке.
cmu-medical-surgery-self-not-allowed = Вы не можете провести эту операцию на себе.
cmu-medical-surgery-step-pain-uncontrolled = Пациент испытывает слишком сильную боль для продолжения операции. Используйте анестезию или сильное обезболивающее перед следующей попыткой.
cmu-medical-amputation-success = Конечность удалена.
cmu-medical-amputation-cured-infection = Инфицированная плоть удалена вместе с конечностью.

# ---- Категории инструментов (используются в кнопке BUI и строке выбора) -------

cmu-medical-surgery-tool-category-scalpel = Скальпель
cmu-medical-surgery-tool-category-hemostat = Гемостат
cmu-medical-surgery-tool-category-retractor = Ретрактор
cmu-medical-surgery-tool-category-cautery = Прижигатель
cmu-medical-surgery-tool-category-bone_saw = Костная пила
cmu-medical-surgery-tool-category-bone_setter = Костоправ
cmu-medical-surgery-tool-category-bone_gel = Костный гель
cmu-medical-surgery-tool-category-bone_graft = Костный трансплантат
cmu-medical-surgery-tool-category-fix_o_vein = Фиксатор вен
cmu-medical-surgery-tool-category-organ_clamp = Зажим для органов
cmu-medical-surgery-tool-category-scalpel_or_burn_kit = Скальпель или набор для лечения ожогов
cmu-medical-surgery-tool-category-severed_limb = Подходящая конечность
cmu-medical-surgery-tool-category-blowtorch = Включённый сварочный аппарат
cmu-medical-surgery-tool-category-cable_coil = Моток кабеля

# ---- Названия этапов -------------------------------------------------

cmu-medical-surgery-step-realign-simple-label = Вправить простой перелом
cmu-medical-surgery-step-realign-compound-label = Вправить открытый перелом
cmu-medical-surgery-step-realign-shattered-label = Вправить оскольчатый перелом
cmu-medical-surgery-step-apply-gel-label = Нанести костный гель
cmu-medical-surgery-step-apply-gel-second-label = Нанести костный гель (второй слой)
cmu-medical-surgery-step-insert-graft-label = Установить костный трансплантат
cmu-medical-surgery-step-cauterize-bleed-label = Устранить внутреннее кровотечение
cmu-medical-surgery-step-tie-vessel-label = Перевязать разорванный сосуд
cmu-medical-surgery-step-extract-foreign-body-label = Извлечь инородное тело
cmu-medical-surgery-step-relieve-pressure-label = Снизить внутритканевое давление
cmu-medical-surgery-step-debride-contamination-label = Удалить загрязнённые ткани
cmu-medical-surgery-step-remove-bone-fragments-label = Удалить костные осколки
cmu-medical-surgery-step-free-organ-adhesions-label = Рассечь спайки органа
cmu-medical-surgery-step-pack-organ-bleed-label = Тампонировать кровотечение органа
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
cmu-medical-surgery-step-reinsert-liver-label = Установить донорскую печень
cmu-medical-surgery-step-reinsert-lungs-label = Установить донорские лёгкие
cmu-medical-surgery-step-reinsert-kidneys-label = Установить донорские почки
cmu-medical-surgery-step-reinsert-stomach-label = Установить донорский желудок
cmu-medical-surgery-step-transplant-heart-label = Пересадить донорское сердце
cmu-medical-surgery-step-suture-liver-label = Ушить печень
cmu-medical-surgery-step-suture-lungs-label = Ушить лёгкие
cmu-medical-surgery-step-suture-kidneys-label = Ушить почки
cmu-medical-surgery-step-suture-heart-label = Ушить сердце
cmu-medical-surgery-step-suture-stomach-label = Ушить желудок
cmu-medical-surgery-step-amputate-limb-label = Ампутировать конечность
cmu-medical-surgery-step-reattach-limb-label = Пришить отсечённую конечность

# ---- Автодок ---------------------------------------------------------

cmu-autodoc-window-title = Автодок
cmu-autodoc-no-patient = Пациент отсутствует
cmu-autodoc-status-no-pod = Поблизости нет подключённой капсулы автодока.
cmu-autodoc-status-empty = Подключённая капсула пуста.
cmu-autodoc-status-ready = Готов к добавлению автоматических процедур в очередь.
cmu-autodoc-status-running = Выполняются процедуры из очереди.
cmu-autodoc-current-idle = Текущая процедура: ожидание
cmu-autodoc-current-step = Текущая процедура: { $step }
cmu-autodoc-current-step-timed = Текущая процедура: { $step } (осталось { $time })
cmu-autodoc-current-step-detail = { $surgery } / { $part } / { $step }
cmu-autodoc-start-button = Запустить
cmu-autodoc-stop-button = Остановить
cmu-autodoc-clear-button = Очистить
cmu-autodoc-eject-button = Извлечь пациента
cmu-autodoc-remove-button = Удалить
cmu-autodoc-queue-button = В очередь
cmu-autodoc-queue-heading = Очередь
cmu-autodoc-parts-heading = Части тела
cmu-autodoc-surgeries-heading = Операции
cmu-autodoc-queue-empty = В очереди нет процедур.
cmu-autodoc-queue-summary = Процедур в очереди: { $count }
cmu-autodoc-available-procedures = Доступно процедур: { $count }
cmu-autodoc-part-procedures = Процедур: { $count }
cmu-autodoc-surgery2-required = Для добавления этапов автодока в очередь требуется навык «Хирургия 2».
cmu-autodoc-no-surgeries = Здесь нет доступных операций.
cmu-autodoc-queue-row = #{ $index } { $surgery } на { $part } - { $step }
cmu-autodoc-surgery-row = { $surgery } - { $step }
cmu-autodoc-automated-step-label = Автоматический цикл восстановления
cmu-autodoc-automated-step-note = Автодок восстанавливает выбранную область по машинному таймеру.
cmu-autodoc-repair-wounds-surgery = Лечение ран / ожогов
cmu-autodoc-procedure-time-note = Автоматическая процедура: { $time }.
cmu-autodoc-minutes = { $minutes } мин
cmu-autodoc-seconds = { $seconds } с
cmu-autodoc-minutes-seconds = { $minutes } мин { $seconds } с

# ---- Сканер тела -----------------------------------------------------

cmu-body-scanner-window-title = Сканер тела
cmu-body-scanner-no-patient = Пациент отсутствует
cmu-body-scanner-status-no-pod = Поблизости нет подключённой капсулы сканера тела.
cmu-body-scanner-status-empty = Подключённая капсула сканера пуста.
cmu-body-scanner-status-ready = Сканирование пациента готово.
cmu-body-scanner-status-no-skill = Для завершения сканирования требуется навык «Хирургия 1».
cmu-body-scanner-boost-active = Хирургическая поддержка откалибрована: осталось { $time }.
cmu-body-scanner-boost-inactive = Хирургическая поддержка не откалибрована.
cmu-body-scanner-scan-heading = Сканирование
cmu-body-scanner-terms-heading = Слои среза
cmu-body-scanner-targets-heading = Активные показания среза
cmu-body-scanner-start-button = Начать калибровку
cmu-body-scanner-reset-button = Сбросить калибровку
cmu-body-scanner-eject-button = Извлечь пациента
cmu-body-scanner-surgery1-required = Для сканирования тела требуется навык «Хирургия 1».
cmu-body-scanner-no-scan-lines = Данные сканирования отсутствуют.
cmu-body-scanner-diagnostic-summary = Диагностических записей: { $count }
cmu-body-scanner-match-summary = Зафиксировано { $matched }/{ $required }, осталось { $time }
cmu-body-scanner-match-summary-idle = Зафиксировано { $matched }/{ $required }, не запущено
cmu-body-scanner-calibrated-summary = Откалибровано, хирургическая поддержка: { $time }
cmu-body-scanner-calibrated-badge = ОТКАЛИБРОВАНО { $time }
cmu-body-scanner-calibration-ready = 2:00
cmu-body-scanner-lockout-summary = Активный срез заблокирован, осталось { $time }
cmu-body-scanner-lockout-status = Активный срез заблокирован: осталось { $time }.
cmu-body-scanner-lockout-detail = Калибровка не удалась. Дождитесь окончания блокировки.
cmu-body-scanner-no-surgical-targets = Цели не обнаружены.
cmu-body-scanner-no-surgical-targets-detail = Бонус не получен.
cmu-body-scanner-calibration-heading = Сканирование анатомических срезов
cmu-body-scanner-sweep-title = Послойная развёртка сканера
cmu-body-scanner-sweep-detail = Настройтесь на срез, чтобы начать.
cmu-body-scanner-layer-selected = Срез настроен — зафиксировано { $locked }/{ $total }
cmu-body-scanner-layer-ready = Зафиксировано { $locked }/{ $total }
cmu-body-scanner-layer-empty = Аномальных показаний нет
cmu-body-scanner-signal-locked = Сигнал зафиксирован
cmu-body-scanner-signal-ready = { $detail } — зафиксируйте на голубом
cmu-body-scanner-start-status = Запустите калибровку, чтобы начать сканирование срезов.
cmu-body-scanner-ready-status = Настройтесь на срез, затем фиксируйте аномальные показания, когда развёртка станет голубой.
cmu-body-scanner-armed-status = Выбран срез: { $layer }. Фиксируйте показания, когда развёртка войдёт в голубую зону.
cmu-body-scanner-penalty-status = Неверный момент или срез: -{ $seconds } с.
cmu-body-scanner-feedback-correct = Сигнал зафиксирован.
cmu-body-scanner-feedback-wrong-timing = Развёртка вышла за зону захвата: -{ $seconds } с.
cmu-body-scanner-feedback-wrong-layer = Помехи слоя: -{ $seconds } с.
cmu-body-scanner-expired-status = Время истекло. Сбросьте калибровку для повторной попытки.
cmu-body-scanner-complete-status = Все показания зафиксированы. Хирургическая поддержка откалибрована.
cmu-body-scanner-timer-active = ТАЙМЕР АКТИВНОГО СРЕЗА
cmu-body-scanner-timer-expired = ВРЕМЯ ИСТЕКЛО
cmu-body-scanner-timer-locked = СРЕЗ ЗАБЛОКИРОВАН
cmu-body-scanner-timer-detail = Зафиксируйте показания до закрытия окна сканирования.
cmu-body-scanner-no-layer-signals = На слое { $layer } нет аномальных показаний.
cmu-body-scanner-interference-title = Нераспознанное показание
cmu-body-scanner-interference-detail = Помехи на слое { $layer }
cmu-body-scanner-decoy-ready = { $detail } — шумное эхо
cmu-body-scanner-decoy-vitals-1 = Всплеск сердечного эха
cmu-body-scanner-decoy-vitals-2 = Колебание насыщения крови кислородом
cmu-body-scanner-decoy-detail-vitals = кратковременный артефакт жизненных показателей
cmu-body-scanner-decoy-skeleton-1 = Тонкая тень на кости
cmu-body-scanner-decoy-skeleton-2 = Призрачное смещение сустава
cmu-body-scanner-decoy-detail-skeleton = нестабильный силуэт кости
cmu-body-scanner-decoy-organs-1 = Размытый контур органа
cmu-body-scanner-decoy-organs-2 = Отражение плотности
cmu-body-scanner-decoy-detail-organs = непостоянная плотность органа
cmu-body-scanner-decoy-tissue-1 = Всплеск поверхностных тканей
cmu-body-scanner-decoy-tissue-2 = Полоса сосудистого шума
cmu-body-scanner-decoy-detail-tissue = зашумлённый отклик мягких тканей
cmu-body-scanner-triage-stable = Стабильные показатели
cmu-body-scanner-triage-serious = Серьёзные нарушения
cmu-body-scanner-triage-critical = Критические нарушения
cmu-body-scanner-triage-clear = Немедленных патологических изменений не обнаружено.
cmu-body-scanner-health-stable = Стабильное
cmu-body-scanner-health-damaged = Повреждено
cmu-body-scanner-health-critical = Критическое
cmu-body-scanner-section-vitals = Жизненные показатели
cmu-body-scanner-section-body = Тело
cmu-body-scanner-section-organs = Органы
cmu-body-scanner-term-assigned = { $term } -> { $target }
cmu-body-scanner-target-filled = { $target }: { $term }
cmu-body-scanner-line-state = Состояние: { $state }
cmu-body-scanner-line-damage = Повреждения: всего { $total } (механические { $brute }, ожоги { $burn })
cmu-body-scanner-line-blood = Кровь: { $blood } / { $max }
cmu-body-scanner-heart-stopped = Сердце: активность не обнаружена
cmu-body-scanner-heart-active = Сердце: { $bpm } уд/мин
cmu-body-scanner-line-no-data = Диагностические данные отсутствуют.
cmu-body-scanner-line-part = { $part }: { $details }
cmu-body-scanner-part-health = ОЗ { $current } / { $max }
cmu-body-scanner-part-wounds = Необработанных ран: { $count }
cmu-body-scanner-part-fracture = Перелом: { $severity }
cmu-body-scanner-part-bleed = внутреннее кровотечение { $rate }/с
cmu-body-scanner-part-eschar = струп
cmu-body-scanner-part-splinted = наложена шина
cmu-body-scanner-part-cast = наложен гипс
cmu-body-scanner-part-tourniquet = наложен жгут
cmu-body-scanner-part-flesh-infection = локализованная инфекция
cmu-body-scanner-part-missing-limb = конечность отсутствует / отсечена
cmu-body-scanner-line-organ = { $organ }: { $stage } ({ $current } / { $max })
cmu-body-scanner-line-missing-organ = { $organ } отсутствует в { $part }
cmu-body-scanner-title-state = Состояние
cmu-body-scanner-title-damage = Повреждения
cmu-body-scanner-title-blood = Кровь
cmu-body-scanner-title-heart = Сердце
cmu-body-scanner-title-flesh-infection = Инфекция
cmu-body-scanner-title-no-data = Диагностика
cmu-body-scanner-title-missing-organ = Отсутствует: { $organ }
cmu-body-scanner-detail-damage = всего { $total } (механические { $brute }, ожоги { $burn })
cmu-body-scanner-detail-blood = { $blood } / { $max }
cmu-body-scanner-detail-heart-stopped = активность не обнаружена
cmu-body-scanner-detail-flesh-infection-systemic = системная
cmu-body-scanner-detail-heart-active = { $bpm } уд/мин
cmu-body-scanner-detail-no-data = Диагностические данные отсутствуют.
cmu-body-scanner-detail-organ = { $stage } ({ $current } / { $max })
cmu-body-scanner-detail-missing-organ = в { $part }
cmu-body-scanner-signal-heart-stopped = Сердце: активность не обнаружена
cmu-body-scanner-signal-organ-damage = { $organ }: повреждение органа — { $stage }
cmu-body-scanner-signal-low-blood = Низкий объём крови: { $blood } / { $max }
cmu-body-scanner-signal-internal-bleed = { $part }: внутреннее кровотечение { $rate }/с
cmu-body-scanner-signal-fracture = { $part }: перелом — { $severity }
cmu-body-scanner-signal-wounds = { $part }: необработанных ран — { $count }
cmu-body-scanner-signal-trauma = { $part }: травма тканей { $current } / { $max }
cmu-body-scanner-signal-missing-organ = { $organ } отсутствует в { $part }
cmu-body-scanner-signal-missing-limb = { $part }: конечность отсутствует / отсечена
cmu-body-scanner-slice-detail-cardiac = сердечный ритм
cmu-body-scanner-slice-detail-organ = плотность органа
cmu-body-scanner-slice-detail-blood = объём крови
cmu-body-scanner-slice-detail-bleed = кровоток тканей
cmu-body-scanner-slice-detail-fracture = положение костей
cmu-body-scanner-slice-detail-wound = повреждение тканей
cmu-body-scanner-slice-detail-trauma = плотность мягких тканей
cmu-body-scanner-slice-detail-missing-organ = силуэт органа
cmu-body-scanner-slice-detail-missing-limb = силуэт конечности

cmu-limb-printer-window-title = Принтер конечностей
cmu-limb-printer-header = Создание конечностей
cmu-limb-printer-matrix-heading = Матрица синтеза
cmu-limb-printer-blood-heading = Образец крови
cmu-limb-printer-metal-heading = Материал роботизированного каркаса
cmu-limb-printer-metal-type = Металлические листы
cmu-limb-printer-no-beaker = Мензурка с матрицей не установлена.
cmu-limb-printer-no-syringe = Шприц с кровью не установлен.
cmu-limb-printer-no-metal = Металлические листы не загружены.
cmu-limb-printer-fluid-amount = { $current } / { $max } ед.
cmu-limb-printer-stack-amount = { $current } / { $max }
cmu-limb-printer-matrix-cost = { $cost } ед. матрицы на печать
cmu-limb-printer-blood-cost = { $cost } ед. крови на печать
cmu-limb-printer-metal-cost = { $cost } листов на печать роботизированной конечности
cmu-limb-printer-remove-beaker = Извлечь мензурку
cmu-limb-printer-remove-syringe = Извлечь шприц
cmu-limb-printer-remove-metal = Извлечь металл
cmu-limb-printer-left-heading = Левая
cmu-limb-printer-right-heading = Правая
cmu-limb-printer-print-ready = Готово к печати
cmu-limb-printer-status-ready = Готово к синтезу.
cmu-limb-printer-missing-beaker = Вставьте мензурку с биогенной матрицей.
cmu-limb-printer-missing-matrix = Недостаточно биогенной матрицы.
cmu-limb-printer-missing-syringe = Вставьте шприц с кровью пациента.
cmu-limb-printer-missing-blood = Недостаточно крови пациента.
cmu-limb-printer-missing-metal-slot = Загрузите металлические листы.
cmu-limb-printer-missing-metal = Недостаточно металлических листов.
cmu-limb-printer-wrong-metal = Загрузите металлические листы, а не обычную сталь.
cmu-limb-printer-printed = Напечатано: { $limb }.
cmu-limb-printer-left-arm = Левая рука
cmu-limb-printer-left-hand = Левая кисть
cmu-limb-printer-left-leg = Левая нога
cmu-limb-printer-left-foot = Левая ступня
cmu-limb-printer-right-arm = Правая рука
cmu-limb-printer-right-hand = Правая кисть
cmu-limb-printer-right-leg = Правая нога
cmu-limb-printer-right-foot = Правая ступня
cmu-limb-printer-left-robotic-arm = Левая роботизированная рука
cmu-limb-printer-left-robotic-hand = Левая роботизированная кисть
cmu-limb-printer-left-robotic-leg = Левая роботизированная нога
cmu-limb-printer-left-robotic-foot = Левая роботизированная ступня
cmu-limb-printer-right-robotic-arm = Правая роботизированная рука
cmu-limb-printer-right-robotic-hand = Правая роботизированная кисть
cmu-limb-printer-right-robotic-leg = Правая роботизированная нога
cmu-limb-printer-right-robotic-foot = Правая роботизированная ступня
cmu-limb-printer-slot-beaker = мензурка с матрицей
cmu-limb-printer-slot-syringe = шприц с кровью
cmu-limb-printer-slot-metal = металлические листы
cmu-autodoc-regenerate-limb-surgery = Регенерация конечности

cmu-body-scanner-calibration-elsewhere = Активная калибровка относится к другому сеансу сканирования. Обратный отсчёт продолжается.

cmu-medical-surgery-category-yautja_medicomp = Медикомп яутжа
cmu-medical-surgery-tool-category-yautja_medicomp_stabilizer = Стабилизирующий гель
cmu-medical-surgery-tool-category-yautja_medicomp_healing_gun = Лечебная пушка
cmu-medical-surgery-tool-category-yautja_medicomp_clamp = Зажим для ран
cmu-medical-surgery-step-trim-necrotic-stump-label = Обрезать некротическую культю
cmu-medical-surgery-step-prep-reattach-socket-label = Подготовить место пришивания
cmu-medical-surgery-step-debride-eschar-label = Удалить струп
cmu-medical-surgery-name-set-fracture = Стабилизация перелома
cmu-medical-surgery-name-stop-internal-bleeding = Остановка внутреннего кровотечения
cmu-medical-surgery-name-remove-liver = Удаление печени
cmu-medical-surgery-name-remove-lungs = Удаление лёгких
cmu-medical-surgery-name-remove-kidneys = Удаление почек
cmu-medical-surgery-name-remove-heart = Удаление сердца
cmu-medical-surgery-name-remove-stomach = Удаление желудка
cmu-medical-surgery-name-replace-liver = Замена печени
cmu-medical-surgery-name-replace-lungs = Замена лёгких
cmu-medical-surgery-name-replace-kidneys = Замена почек
cmu-medical-surgery-name-transplant-heart = Трансплантация сердца
cmu-medical-surgery-name-replace-stomach = Замена желудка
cmu-medical-surgery-name-suture-liver = Ушивание печени
cmu-medical-surgery-name-suture-lungs = Ушивание лёгких
cmu-medical-surgery-name-suture-kidneys = Ушивание почек
cmu-medical-surgery-name-suture-heart = Ушивание сердца
cmu-medical-surgery-name-suture-stomach = Ушивание желудка
cmu-medical-surgery-name-repair-brain = Восстановление мозга
cmu-medical-surgery-name-repair-eyes = Восстановление глаз
cmu-medical-surgery-name-repair-ears = Восстановление ушей
cmu-medical-surgery-name-remove-limb = Ампутация конечности
cmu-medical-surgery-name-reattach-limb = Пришивание конечности
cmu-medical-surgery-name-remove-larva = Удаление личинки
cmu-medical-surgery-name-debride-eschar = Удаление струпа
cmu-autodoc-dialysis-start = Начать диализ
cmu-autodoc-dialysis-stop = Остановить диализ
cmu-autodoc-chemicals-heading = Химикаты капсулы сна
cmu-autodoc-no-chemicals = Химикаты не настроены.
cmu-autodoc-dose-small = 5 ед.
cmu-autodoc-dose-large = 10 ед.

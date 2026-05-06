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
cmu-medical-surgery-category-remove_organ = Извлечение органов
cmu-medical-surgery-category-transplant = Пересадка органов
cmu-medical-surgery-category-suture = Ушивание органов
cmu-medical-surgery-category-head_organ = Операции на голове
cmu-medical-surgery-category-reattach = Пришивание конечности
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
cmu-medical-surgery-step-reattach-limb-label = Пришить отсечённую конечность

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
cmu-medical-surgery-name-repair-brain = Восстановление мозга
cmu-medical-surgery-name-repair-eyes = Восстановление глаз
cmu-medical-surgery-name-repair-ears = Восстановление ушей
cmu-medical-surgery-name-reattach-limb = Пришивание конечности

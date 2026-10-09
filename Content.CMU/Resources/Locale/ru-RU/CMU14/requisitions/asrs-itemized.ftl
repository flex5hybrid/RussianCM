cmu-asrs-window-title = Автоматизированная система хранения и выдачи
cmu-asrs-tab-items = ПРЕДМЕТЫ
cmu-asrs-tab-bundles = УСТАРЕВШИЕ КОМПЛЕКТЫ
cmu-asrs-return-items = КАТАЛОГ ПРЕДМЕТОВ
cmu-asrs-mode-itemized = ПОШТУЧНАЯ ЗАКУПКА
cmu-asrs-categories = КАТЕГОРИИ
cmu-asrs-search-items = Поиск отдельных предметов...
cmu-asrs-catalog-heading = ДОСТУПНЫЕ ПРЕДМЕТЫ
cmu-asrs-cart-heading = КОРЗИНА ПОСТАВКИ
cmu-asrs-cart-empty = Корзина пуста. Добавьте предметы из каталога.
cmu-asrs-checkout = ОФОРМИТЬ ПОСТАВКУ
cmu-asrs-category-all = ВСЕ ПРЕДМЕТЫ
cmu-asrs-category-favorites = ИЗБРАННОЕ
cmu-asrs-category-recent = НЕДАВНИЕ ЗАКАЗЫ
cmu-asrs-results = РЕЗУЛЬТАТОВ: { $count }
cmu-asrs-no-results = Ни один предмет не соответствует текущим фильтрам.
cmu-asrs-budget = БЮДЖЕТ: ${ $balance }
cmu-asrs-platform-status = ПЛАТФОРМА: { $state } | СВОБОДНО МЕСТ: { $slots }
cmu-asrs-platform-none = НЕ ПОДКЛЮЧЕНА
cmu-asrs-platform-busy = ЗАНЯТА
cmu-asrs-platform-lowered = ОПУЩЕНА
cmu-asrs-platform-raised = ПОДНЯТА
cmu-asrs-platform-lowering = ОПУСКАЕТСЯ
cmu-asrs-platform-raising = ПОДНИМАЕТСЯ
cmu-asrs-button-busy = АСХВ ЗАНЯТА
cmu-asrs-button-raise = ПОДНЯТЬ
cmu-asrs-button-lower = ОПУСТИТЬ
cmu-asrs-button-wait = ПОДОЖДИТЕ
cmu-asrs-stock-unlimited = НЕОГРАНИЧЕННО
cmu-asrs-stock-count = В НАЛИЧИИ { $current }/{ $max }
cmu-asrs-stock-count-refill = В НАЛИЧИИ { $current }/{ $max } | +{ $time }
cmu-asrs-cart-cost = ИТОГО: ${ $cost }
cmu-asrs-cart-weight = ГРУЗ: { $weight } ЕД. ВЕСА
cmu-asrs-cart-crates = ПОСТАВКА: МЕСТ НА ПЛАТФОРМЕ — { $crates }
cmu-asrs-cart-remove = Убрать один
cmu-asrs-cart-add = Добавить один
cmu-asrs-favorite-toggle = Добавить/убрать из избранного
cmu-asrs-projected-budget = ПОСЛЕ ЗАКАЗА: ${ $balance }
cmu-asrs-cart-capacity = ТЕКУЩИЙ ЯЩИК: СВОБОДНО { $remaining } ЕД. ВЕСА
cmu-asrs-cart-state-idle = ОЖИДАНИЕ
cmu-asrs-cart-state-packing = ПЛАН УПАКОВКИ
cmu-asrs-crate-title = ЯЩИК { $number }
cmu-asrs-crate-packing = УПАКОВКА
cmu-asrs-crate-sealed = ЗАПОЛНЕН
cmu-asrs-loose-title = БЕЗ ЯЩИКА { $number }
cmu-asrs-loose-state = ОТПРАВЛЯЕТСЯ ОТДЕЛЬНО
cmu-asrs-hint-empty = ВЫБЕРИТЕ ПРЕДМЕТ, ЧТОБЫ НАЧАТЬ ФОРМИРОВАНИЕ ПЛАНА УПАКОВКИ.
cmu-asrs-hint-fit = В ЯЩИКЕ { $crate } ОСТАЛОСЬ { $remaining } ЕД. ВЕСА.
cmu-asrs-hint-loose = ПРЕДМЕТОВ БЕЗ ЯЩИКА: { $amount }.
cmu-asrs-hint-funds = ЗАКАЗ ПРЕВЫШАЕТ БЮДЖЕТ НА ${ $amount }.
cmu-asrs-hint-slots = ДЛЯ ЗАКАЗА ТРЕБУЕТСЯ ЕЩЁ МЕСТ НА ПЛАТФОРМЕ: { $amount }.
cmu-asrs-checkout-pending = Передача заказа...
cmu-asrs-checkout-success = Заказ принят. Поставка добавлена в очередь.
cmu-asrs-checkout-invalid = Заказ отклонён: некорректная корзина.
cmu-asrs-checkout-funds = Заказ отклонён: недостаточно средств.
cmu-asrs-checkout-stock = Заказ отклонён: запасы изменились. Проверьте корзину.
cmu-asrs-checkout-platform = Заказ отклонён: платформа АСХВ не подключена.
cmu-asrs-checkout-full = Заказ отклонён: на платформе недостаточно свободных мест.
cmu-asrs-phase-verifying = ПРОВЕРКА ЗАПАСОВ...
cmu-asrs-phase-packing = ФОРМИРОВАНИЕ ГРУЗОВОГО МАНИФЕСТА...
cmu-asrs-phase-sealing = ЗАПЕЧАТЫВАНИЕ ЯЩИКОВ...
cmu-asrs-phase-dispatching = ОТПРАВКА НА ПЛАТФОРМУ...
cmu-asrs-phase-complete = ПОСТАВКА ПРИНЯТА
cmu-asrs-receipt-title = КВИТАНЦИЯ ОТПРАВКИ АСХВ
cmu-asrs-receipt-summary = СПИСАНО: ${ $cost }
    ГРУЗ: { $weight } ЕД. ВЕСА
    ПОСТАВОК: { $crates }
cmu-asrs-receipt-dismiss = ВЕРНУТЬСЯ В КАТАЛОГ
cmu-asrs-preview-crate = МАРШРУТ: ЯЩИК { $crate } // { $weight }/{ $limit } ЕД. ВЕСА
cmu-asrs-preview-loose = МАРШРУТ: ГРУЗ БЕЗ ЯЩИКА // { $weight } ЕД. ВЕСА
cmu-asrs-slot-filled = Это место на платформе занято поставкой
cmu-asrs-slot-free = Место на платформе свободно
cmu-asrs-slot-overflow = Поставка превышает доступную вместимость платформы
cmu-asrs-boot-bus = АСХВ/88 УПРАВЛЕНИЕ ПОГРУЗКОЙ
    {"["}01] ОПРОС ШИНЫ ХРАНИЛИЩА...
    {"["}02] ОЖИДАНИЕ ТЕЛЕМЕТРИИ ПОГРУЗОЧНОГО ОТСЕКА
cmu-asrs-boot-cranes = АСХВ/88 УПРАВЛЕНИЕ ПОГРУЗКОЙ
    {"["}OK] ШИНА ХРАНИЛИЩА
    {"["}03] КАЛИБРОВКА СЕРВОПРИВОДОВ КРАНА...
cmu-asrs-boot-scale = АСХВ/88 УПРАВЛЕНИЕ ПОГРУЗКОЙ
    {"["}OK] НУЛЕВАЯ ПОЗИЦИЯ КРАНА
    {"["}04] ОБНУЛЕНИЕ ГРУЗОВЫХ ВЕСОВ...
cmu-asrs-boot-manifest = АСХВ/88 УПРАВЛЕНИЕ ПОГРУЗКОЙ
    {"["}OK] ВЕСОВЫЕ ДАТЧИКИ
    {"["}05] ПОДКЛЮЧЕНИЕ ТОМА МАНИФЕСТА...
cmu-asrs-boot-ready = АСХВ/88 УПРАВЛЕНИЕ ПОГРУЗКОЙ
    ВСЕ СИСТЕМЫ В НОРМЕ // ПОГРУЗОЧНЫЙ ОТСЕК ГОТОВ
cmu-asrs-idle = ПОГРУЗОЧНЫЙ ОТСЕК АСХВ // ОЖИДАНИЕ
    ═══════════════════════════════════
    ПОЗИЦИЯ КРАНА { $position } // ОЖИДАНИЕ МАНИФЕСТА
    ШИНА ХРАНИЛИЩА БЕЗ АКТИВНОСТИ // МОНИТОРИНГ ПЛАТФОРМЫ АКТИВЕН
cmu-asrs-load-bay = ПОГРУЗОЧНЫЙ ОТСЕК //
cmu-asrs-load-control = ПРОМЫШЛЕННОЕ УПРАВЛЕНИЕ ПОГРУЗКОЙ
cmu-asrs-catalog-index = УКАЗАТЕЛЬ КАТАЛОГА
cmu-asrs-holographic-inspection = ГОЛОГРАФИЧЕСКИЙ ОСМОТР
cmu-asrs-feed-rack = ПОДАЮЩАЯ СТОЙКА
cmu-asrs-physical-manifest = ФИЗИЧЕСКИЙ ГРУЗОВОЙ МАНИФЕСТ
cmu-asrs-platform-slots = МЕСТА НА ПЛАТФОРМЕ
cmu-asrs-line-printer = СТРОЧНЫЙ ПРИНТЕР // КВИТАНЦИЯ ОТПРАВКИ
cmu-asrs-seal-dispatch = ЗАПЕЧАТАТЬ И ОТПРАВИТЬ
cmu-asrs-budget-prefix = БЮДЖЕТ:
cmu-asrs-after-prefix = ПОСЛЕ:
cmu-asrs-conveyor-crate = ▥  ЯЩИК { $number }  //  ЗАПЕЧАТАН

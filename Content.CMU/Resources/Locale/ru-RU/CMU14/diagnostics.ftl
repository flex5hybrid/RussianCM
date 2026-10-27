cmu-cmd-client-memory-desc = Печатает сущность клиента, компонент, прототип и количество карт.

cmu-cmd-client-memory-help = Использование:
    cmu client memory snapshot [top=15]
    cmu client memory [top=15]
    cmu client memory diff [top=15]

    Используйте базовый уровень, подождите, пока счет растет, затем дифф.

cmu-cmd-client-memory-baseline = Базовый уровень клиентской памяти, захваченный тиком { $tick }.

cmu-cmd-client-memory-unknown-mode = Неизвестный режим{ $mode }'.

cmu-cmd-client-memory-title = == CMU Счета клиентов ==

cmu-cmd-client-memory-tick = Тик: { $tick }

cmu-cmd-client-memory-sandbox-note = Счетчики времени выполнения, GC, процесс и ThreadPool опускаются в песочнице контента.

cmu-cmd-client-memory-counts = Организации: { $entities }{ $entityDelta } Компоненты: { $components }{ $componentDelta }

cmu-cmd-client-memory-top-components = Лучшие компоненты

cmu-cmd-client-memory-top-prototypes = Лучшие прототипы

cmu-cmd-client-memory-maps = Карты

cmu-cmd-client-memory-section = == { $title } ==

cmu-cmd-client-memory-none =   нет

cmu-cmd-client-memory-row = { $index }. { $key } считать{ $count }{ $delta }

cmu-cmd-client-perf-desc = Захватывает подробный клиентский FPS, распределение, рендеринг и диагностику прогнозирования в локальный файл журнала.

cmu-cmd-client-perf-help = Использование: cmu client perf [start [seconds=120] [spike_ms=33.333]] | stop | report | status | open | help
    Бегите без аргументов, закройте консоль и воспроизведите падение FPS. Захват останавливается автоматически.
    Используйте остановку, чтобы закончить раньше, сообщить о подробной контрольной точке и открыть, чтобы найти файл .log для совместного использования.
    Продолжительность: 5–1800sПороговый порог: 1–10000ms (десятичная точка). Профилирование добавляет накладные расходы при включении.

cmu-cmd-client-perf-join-server = Присоединяйтесь к серверу, прежде чем фиксировать производительность клиента. Используйте cmu client perf, чтобы найти более ранние записи.

cmu-cmd-client-perf-failed = Диагностика эффективности клиентов не удалась: { $error }

cmu-cmd-server-perf-desc = Показ или ручной захват CMU Автоматическая диагностика производительности сервера.

cmu-cmd-server-perf-help = Использование: cmuperf status | report | pvs | Physics | reset

cmu-cmd-server-perf-report-written = Подробно CMU Отчет о производительности, написанный на лесопилку cmu.server-performance.

cmu-cmd-server-perf-report-unavailable = Диагностика производительности отключена или не выпустила свой первый образец.

cmu-cmd-server-perf-reset = CMU Окна производительности и здоровые базовые линии были сброшены.

cmu-cmd-server-perf-reset-unavailable = Диагностика производительности отключена или не инициализирована.

cmu-cmd-medical-perf-desc = Сообщения поблизости CMU Наглядность медицинского клиента и наложение кандидата имеет значение.

cmu-cmd-medical-perf-help = Использование: cmu medical perf [range=12]

cmu-cmd-medical-perf-no-entity = Вы должны быть привязаны к сущности.

cmu-cmd-medical-perf-nullspace = Привязанная сущность находится в нулевом пространстве.

cmu-cmd-medical-perf-heading = CMU Медицинский фейерверк вокруг { $range }м:

cmu-cmd-medical-perf-toggles =   Локальные переключатели: statusIcons{ $statusIcons }Морской Оверлей ={ $marineOverlay }

cmu-cmd-medical-perf-hud =   Местные HUD Comps: HealthBars{ $healthBars }Иконы здоровья ={ $healthIcons }Морские иконы ={ $marineIcons }

cmu-cmd-medical-perf-nearby =   Видимые объекты поблизости: { $count }

cmu-cmd-medical-perf-bodies =   поблизости CMU тела: { $count }

cmu-cmd-medical-perf-internals =   видимый прикрепленный CMU Внутренние: { $count }

cmu-cmd-medical-perf-status-candidates =   Кандидаты на статус иконки: { $count }

cmu-cmd-medical-perf-health-candidates =   Кандидаты в Health Bar: { $count }

cmu-cmd-medical-perf-marine-candidates =   Кандидаты на морские иконы: { $count }

cmu-cmd-server-perf-pvs-written = Сценическое окно PVS, написанное на лесопилку cmu.server-performance.

cmu-cmd-server-perf-physics-written = Физика контроллера окна и перепись тела написаны на cmu.server-производительность лесопилки.

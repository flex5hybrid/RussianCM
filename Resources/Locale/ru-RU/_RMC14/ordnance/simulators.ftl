ent-CMDemolitionsScanner = сапёрный сканер
    .desc = Портативный сканер для анализа самодельных боеприпасов. Используйте его на корпусе химбомбы, чтобы получить сводку по предполагаемому взрывному и зажигательному эффекту.

ent-RMCExplosionSimulator = компьютер симуляции взрыва
    .desc = Мощный симуляционный компьютер для анализа готовых взрывных боеприпасов по разным типам целей. Вставьте собранную гранату, мину, ракету или другой боеприпас, выберите профиль цели и запустите двухминутную симуляцию.

ent-RMCDemolitionsCamera = симуляционная камера
    .desc = Одноразовая камера полигона, используемая симуляторами подрывного дела.

rmc-explosion-sim-slot-sample = Слот образца

rmc-demolitions-simulator-ui-title = Симулятор подрывника
rmc-demolitions-simulator-ui-blast-zone = Зона взрыва
rmc-demolitions-simulator-ui-target = Цель:
rmc-demolitions-simulator-ui-target-xenomorph-drone = Ксеноморф-дрон
rmc-demolitions-simulator-ui-target-xenomorph-warrior = Ксеноморф-воин
rmc-demolitions-simulator-ui-target-xenomorph-crusher = Ксеноморф-крушитель
rmc-demolitions-simulator-ui-target-marine-light = Морпех (без брони)
rmc-demolitions-simulator-ui-target-marine-heavy = Морпех (в броне)
rmc-demolitions-simulator-ui-target-metal-wall = Металлическая стена
rmc-demolitions-simulator-ui-no-casing = Корпус не выбран.
rmc-demolitions-simulator-ui-blast-parameters = Параметры взрыва
rmc-demolitions-simulator-ui-fire-parameters = Параметры огня
rmc-demolitions-simulator-ui-damage-estimates = Оценка урона
rmc-demolitions-simulator-ui-simulate = Симулировать
rmc-demolitions-simulator-ui-status = Держите корпус химбомбы в активной руке и нажмите «Симулировать».
rmc-demolitions-simulator-ui-cooldown = Охлаждение... {$seconds}с
rmc-demolitions-simulator-ui-damage-idle = [color=gray]Держите корпус химбомбы и нажмите «Симулировать».[/color]
rmc-demolitions-simulator-ui-simulating = Симуляция: {$name}
rmc-demolitions-simulator-ui-volume = Химикаты: {$current}/{$max} ед.
rmc-demolitions-simulator-ui-blast-stats = Мощность {$power}   Затухание {$falloff}   Радиус ~{$radius} тайла
rmc-demolitions-simulator-ui-blast-none = Взрывчатые химикаты не обнаружены.
rmc-demolitions-simulator-ui-fire-stats = Интенсивность {$intensity}   Радиус {$radius} тайлов   Длительность {$duration}с
rmc-demolitions-simulator-ui-fire-none = Зажигательные химикаты не обнаружены.
rmc-demolitions-simulator-ui-damage-header = [bold][color=#88dd88]{$target}[/color][/bold] - HP: {$hp}  Броня: {$armor}%
rmc-demolitions-simulator-ui-status-lethal = [color=#ff2222]СМЕРТЕЛЬНО[/color]
rmc-demolitions-simulator-ui-status-critical = [color=#ff8800]Критично[/color]
rmc-demolitions-simulator-ui-status-wounded = [color=#ffdd00]Ранен[/color]
rmc-demolitions-simulator-ui-status-alive = [color=#88ff88]Жив[/color]
rmc-demolitions-simulator-ui-range-epicenter = 0 (эпицентр)
rmc-demolitions-simulator-ui-range-tiles = {$count} {$count ->
        [one] тайл
        [few] тайла
       *[other] тайлов
    }
rmc-demolitions-simulator-ui-damage-row = [color=#aaaaaa]{$label}[/color] {$blast}+{$fire} = [bold]{$total}[/bold] ур. -> {$status}

rmc-explosion-sim-ui-title = Компьютер симуляции взрыва
rmc-explosion-sim-ui-subtitle = Двухминутный анализ самодельных взрывчатых смесей.
rmc-explosion-sim-ui-analysis-cycle = Цикл анализа
rmc-explosion-sim-ui-standby = Ожидание
rmc-explosion-sim-ui-target-profile = Профиль цели
rmc-explosion-sim-ui-target-help = Выберите построение для воспроизведения.
rmc-explosion-sim-ui-target-marines = Морпехи USCM
rmc-explosion-sim-ui-target-special-forces = Спецназ
rmc-explosion-sim-ui-target-xenomorphs = Улей ксеноморфов
rmc-explosion-sim-ui-threat-notes = Тактические заметки
rmc-explosion-sim-ui-sample-status = Состояние образца
rmc-explosion-sim-ui-no-sample = Образец боеприпаса не вставлен.
rmc-explosion-sim-ui-blast-projection = Прогноз взрыва
rmc-explosion-sim-ui-incendiary-projection = Прогноз возгорания
rmc-explosion-sim-ui-summary = Сводка симуляции
rmc-explosion-sim-ui-run-analysis = Запустить анализ
rmc-explosion-sim-ui-replay = Воспроизвести
rmc-explosion-sim-ui-footer-left = Комплекс анализа взрывов
rmc-explosion-sim-ui-footer-right = Исследовательский отсек OT
rmc-explosion-sim-ui-header-idle = Компьютер симуляции взрыва
rmc-explosion-sim-ui-header-processing = Идёт химический анализ
rmc-explosion-sim-ui-header-ready = Пакет симуляции готов
rmc-explosion-sim-ui-processing-remaining = Осталось {$seconds} с
rmc-explosion-sim-ui-processing-complete = Анализ завершён
rmc-explosion-sim-ui-sample-loaded = Образец загружен: {$name}
rmc-explosion-sim-ui-readiness-no-sample-line1 = Вставьте готовый боеприпас,
rmc-explosion-sim-ui-readiness-no-sample-line2 = чтобы начать анализ.
rmc-explosion-sim-ui-readiness-processing-line1 = Образец запечатан.
rmc-explosion-sim-ui-readiness-processing-line2 = Анализ по профилю «{$target}».
rmc-explosion-sim-ui-readiness-ready-line1 = Пакет раствора сохранён.
rmc-explosion-sim-ui-readiness-ready-line2 = Воспроизведение готово.
rmc-explosion-sim-ui-readiness-staged-line1 = Образец подготовлен.
rmc-explosion-sim-ui-readiness-staged-line2 = Запустите анализ для «{$target}».
rmc-explosion-sim-ui-blast-none-line1 = Взрывной выход
rmc-explosion-sim-ui-blast-none-line2 = не прогнозируется.
rmc-explosion-sim-ui-blast-present-line1 = Мощность {$power}
rmc-explosion-sim-ui-blast-present-line2 = Затухание {$falloff}
rmc-explosion-sim-ui-blast-present-line3 = Пик {$peak}
rmc-explosion-sim-ui-blast-present-line4 = Радиус около {$radius} тайла
rmc-explosion-sim-ui-fire-none-line1 = Устойчивое горение
rmc-explosion-sim-ui-fire-none-line2 = не прогнозируется.
rmc-explosion-sim-ui-fire-present-line1 = Интенсивность {$intensity}
rmc-explosion-sim-ui-fire-present-line2 = Радиус {$radius} тайлов
rmc-explosion-sim-ui-fire-present-line3 = Длительность {$duration} с
rmc-explosion-sim-ui-status-processing-line1 = Загруженный образец анализируется по профилю «{$target}».
rmc-explosion-sim-ui-status-processing-line2 = Данные для воспроизведения будут готовы примерно через {$seconds} с.
rmc-explosion-sim-ui-status-idle-line1 = Загрузите готовый боеприпас, выберите профиль цели
rmc-explosion-sim-ui-status-idle-line2 = и запустите двухминутный цикл симуляции.
rmc-explosion-sim-ui-status-ready-line1 = Симуляция по профилю «{$target}» завершена.
rmc-explosion-sim-ui-status-ready-blast = Взрывная волна достигает примерно {$radius} тайла от эпицентра.
rmc-explosion-sim-ui-status-ready-no-blast = Смесь не создаёт значимой взрывной волны.
rmc-explosion-sim-ui-status-ready-fire = Зона возгорания прогнозируется до {$radius} тайлов примерно на {$duration} с.
rmc-explosion-sim-ui-status-ready-no-fire = Устойчивого распространения огня не ожидается.
rmc-explosion-sim-ui-status-ready-replay = Используйте камеру воспроизведения, чтобы заспавнить полигон и посмотреть прогнозируемый подрыв.
rmc-explosion-sim-ui-target-summary-marines = отделение морпехов USCM
rmc-explosion-sim-ui-target-summary-special-forces = штурмовая группа спецназа
rmc-explosion-sim-ui-target-summary-xenomorphs = волна штурма ксеноморфов
rmc-explosion-sim-ui-target-summary-unknown = неизвестный профиль
rmc-explosion-sim-ui-target-notes-marines-1 = Стандартное пехотное построение.
rmc-explosion-sim-ui-target-notes-marines-2 = Семь манекенов расставлены,
rmc-explosion-sim-ui-target-notes-marines-3 = чтобы показать спад поражения.
rmc-explosion-sim-ui-target-notes-special-forces-1 = Плотная бронированная группа.
rmc-explosion-sim-ui-target-notes-special-forces-2 = Пять манекенов стоят теснее
rmc-explosion-sim-ui-target-notes-special-forces-3 = для stress-test у эпицентра.
rmc-explosion-sim-ui-target-notes-xenomorphs-1 = Плотный органический натиск.
rmc-explosion-sim-ui-target-notes-xenomorphs-2 = Девять манекенов сдвинуты вперёд,
rmc-explosion-sim-ui-target-notes-xenomorphs-3 = чтобы показать насыщение
rmc-explosion-sim-ui-target-notes-xenomorphs-4 = и распространение огня.
rmc-explosion-sim-ui-target-notes-unknown = Профиль цели не загружен.

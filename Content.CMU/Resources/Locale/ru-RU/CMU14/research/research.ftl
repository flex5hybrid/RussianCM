# этот файл — настоящий бардак, и я прошу за это прощения - MACMAN2003

research-database-details = [bold]Сведения из базы данных:[/bold]
cmu-paper-header-wy =[italic][bold]Официальный документ «Вейланд-Ютани»[/bold][/italic]
# по какой-то совершенно безумной причине для переноса строки необходим {"\u000a"}
cmu-paper-subheader-xrf-analysis = [italic]Автоматизированный отчёт A-XRF[/italic]{"\u000a"}[head=2]Анализ {$NAME}[/head]{"\u000a"}[bold]Результаты для образца:[/bold]#{$NUMBER}
cmu-paper-subheader-research-xrf-fail = [italic]Распечатка анализа реагента[/italic]{"\u000a"}[head=2]ОШИБКА анализа[/head]

cmu-paper-research-fail-reason = [bold]Причина ошибки:[/bold]{"\u000a"}[italic]{$REASON}[/italic]

cmu-paper-xrf-footer = [italic]Этот отчёт был автоматически сгенерирован сканером A-XRF.[/italic]

research-report-reaction-header = Химическое вещество имеет следующие показатели реакции:

research-report-overdose = Передозировка наступает при {$OD} ед.
research-report-crit-overdose = Критическая передозировка наступает при {$COD} ед.
research-report-metab-mult = Стандартный множитель продолжительности действия: {$MULT}x.

research-report-clearance-insuf = ЗАСЕКРЕЧЕНО:[italic] Для чтения записи базы данных требуется уровень допуска {$CLEAR}.[/italic]
research-report-x-needed = ЗАСЕКРЕЧЕНО:[italic] Для чтения записи базы данных требуется уровень допуска [bold]X[/bold].[/italic]

research-report-no-data = [italic]В базе данных не найдено никаких сведений об этом реагенте.[/italic]

research-report-spectrum-saved = [italic]Спектр излучения {$NAME} сохранён в базе данных.[/italic]

research-report-composition-details = [bold]Сведения о составе:[/bold]

research-report-unknown-emission = [italic]- Неизвестный спектр излучения.[/italic]

research-report-ingredient = [italic] - {$AMOUNT} {$NAME}[/italic]

research-report-catalyst-details = Для реакции потребуются следующие катализаторы:

research-report-element = [italic] - {$NAME}[/italic]

research-report-unable-analyze = [italic]ОШИБКА: Не удалось проанализировать спектр излучения образца.[/italic]

xrf-report-error = Анализ ОШИБКА

research-report-analysis-name = Анализ {$NAME1}{$NAME2}

research-report-simulation-name = Результат симуляции для {$ID}

cmu-paper-header-wy-sim = [italic][bold]Официальный документ Компании[/bold]{"\u000a"}Отчёт о моделировании синтеза[/italic]{"\u000a"}[head=2]Результат для {$ID}[/head]

cmu-paper-sim-footer = [italic]Этот отчёт был автоматически напечатан Симулятором синтеза.[/italic]

cmu-paper-header-experiment = [italic][bold]Официальный документ «Вейланд-Ютани»[/bold][/italic]
cmu-paper-subheader-experiment = [head=2]Контракт на химический синтез: {$NAME}[/head]
cmu-paper-contract-experiment = [bold]Эксперимент {$EXP}[/bold] требует синтеза [bold]{$NAME}[/bold] из следующих реагентов:
research-chem-catalyst = [bold]Катализаторы:[/bold]
cmu-paper-contract-footer = [italic]Этот контракт был автоматически сгенерирован Терминалом исследовательских данных.[/italic]

cmu-paper-ciph-hint-header = [italic][bold]Официальный документ Компании[/bold]{"\u000a"}Заметки по эксперименту и разрешение на испытания[/italic]
cmu-paper-ciph-hint-subheader = [head=3][color=#517087]Отдел биологического оружия «Вейланд-Ютани»[/head][/color]
cmu-paper-ciph-hint = В ходе испытаний было установлено, что предполагаемый компонент [bold]{$CIPH}[/bold] состоит из [bold]{$A}[/bold] и [bold]{$B}[/bold]. Недавнее открытие позволяет предположить, что последним компонентом является [bold]{$C}[/bold].
cmu-paper-xeno-knowledge = Предварительное исследование позволило выдвинуть гипотезу, что [bold]Зашифрованное[/bold] каким-то образом связано с видом ксенофауны ОБОЗНАЧЕНИЕ_ОЖИДАЕТСЯ.{"\u000a"} Согласно имеющимся данным, ОБОЗНАЧЕНИЕ_ОЖИДАЕТСЯ представляет собой эусоциальную облигатно-паразитоидную форму жизни.{"\u000a"} Данные, полученные во время спасательной операции 2122 года и операции КМП СА в 2179 году, указывают на чрезвычайно высокий интеллект и смертоносность этих организмов. {"\u000a"} При проведении испытаний обеспечьте усиленную изоляцию и держите группы безопасности в готовности с автоматическим и бронебойным вооружением.
cmu-paper-xeno-sample-deliv =  {"\u000a"} Мы санкционировали доставку образца ОБОЗНАЧЕНИЕ_ОЖИДАЕТСЯ к ближайшему лифту АСХВ. {"\u000a"} Nota bene: образцы ОБОЗНАЧЕНИЕ_ОЖИДАЕТСЯ встречаются крайне редко. [bold]Не[/bold] потеряйте его.
cmu-paper-ciph-hint-footer = - [italic]Вейланд-Ютани[/italic]

research-chem-terminal-update = Химические контракты обновлены!

research-data-ui-clearance = [color=#ffbf00][head=2]Уровень допуска {$NUM}[/head][/color]
research-data-ui-credits = [color=#ffbf00][head=2]Доступно кредитов: {$NUM}[/head][/color]

research-data-ui-manage = Управление исследованиями
research-data-ui-view = Просмотр химикатов

research-data-ui-chem-name = [color=#ffbf00][head=3]{$NAME}[/head][/color]

research-data-ui-diff-hard = Сложно
research-data-ui-diff-inter = Средне
research-data-ui-diff-easy = Легко

research-data-ui-chem-difficulty = [color=#ffbf00][head=3]Сложность: {$DIFF}[/head][/color]

research-data-ui-chem-desc = [color=#ffbf00]Предварительная оценка показывает, что одной из частей рецепта является {$RECIHINT}{"\u000a"}Предварительные испытания указывают на свойство {$PROPHINT}[/color]

research-data-ui-time-left = [color=#ffbf00]Обновление контрактов через: {$TIME}[/color]

research-data-ui-chem-take = [color=#ffbf00][head=3]Взять контракт[/head][/color]

research-data-synthesis-name = Синтез {$NAME}

research-data-contract-name = Контракт на {$NAME}

research-data-ui-analysis-scan = [color=#ffbf00][bold]Анализ[/bold][/color]
research-data-ui-analysis-sim = [color=#ffbf00][bold]Симуляция[/bold][/color]
research-data-ui-compound-idx = [color=#ffbf00][bold]{$NAME}[/bold][/color]

research-data-ui-scan-time = [color=#ffbf00][bold]Время сканирования[/bold][/color]
research-data-ui-vc-type = [color=#ffbf00][bold]Тип[/bold][/color]
research-data-ui-compound = [color=#ffbf00][bold]Соединение[/bold][/color]
research-data-ui-actions = [color=#ffbf00][bold]Действия[/bold][/color]

research-data-ui-reprint = [color=#ffbf00][head=3]Повторно распечатать последний контракт[/head][/color]
research-data-ui-contracts = [color=#ffbf00][head=3]Химические контракты[/head][/color]
research-data-ui-scan-time-idx = [color=#ffbf00][bold]{$TIME}[/bold][/color]
research-data-ui-improve = [color=#ffbf00][head=3]Улучшить: {$NUM} CR[/head][/color]
ui-research-data-terminal-name = Терминал исследовательских данных

research-data-ui-read = [color=#ffbf00][bold]Прочитать[/bold][/color]
research-data-ui-print = [color=#ffbf00][bold]Распечатать[/bold][/color]

ui-chem-simulator-window-name = Химический симулятор

research-sim-ui-credits = [bold]ИССЛЕДОВАТЕЛЬСКИЕ КРЕДИТЫ: {$NUM}[/bold]
research-sim-ui-cost-null = РАСЧЁТНАЯ СТОИМОСТЬ СИМУЛЯЦИИ: NULL
research-sim-ui-cost = РАСЧЁТНАЯ СТОИМОСТЬ СИМУЛЯЦИИ: {$NUM}
research-sim-ui-target-name = ЦЕЛЕВОЕ ВЕЩЕСТВО: {$NAME}
research-sim-ui-ref-name = ЭТАЛОННОЕ ВЕЩЕСТВО: {$NAME}
research-sim-ui-no-targ-chem = ЦЕЛЕВОЕ ВЕЩЕСТВО: ХИМИЧЕСКИЕ ДАННЫЕ НЕ ВВЕДЕНЫ
research-sim-ui-no-ref-chem = ЭТАЛОННОЕ ВЕЩЕСТВО: ХИМИЧЕСКИЕ ДАННЫЕ НЕ ВВЕДЕНЫ
research-sim-ui-overdose = УРОВЕНЬ ПЕРЕДОЗИРОВКИ ПОСЛЕ СИМУЛЯЦИИ: {$NUM}
research-sim-ui-no-overdose = УРОВЕНЬ ПЕРЕДОЗИРОВКИ ПОСЛЕ СИМУЛЯЦИИ:

research-sim-ui-simulate = СИМУЛИРОВАТЬ
research-sim-ui-eject-targ = ИЗВЛЕЧЬ ЦЕЛЕВОЙ ОБРАЗЕЦ
research-sim-ui-eject-ref = ИЗВЛЕЧЬ ЭТАЛОН
research-sim-ui-override = ПЕРЕОПРЕДЕЛИТЬ
research-sim-ui-override-tooltip = Отключить защиту от объединения конфликтующих свойств.
research-sim-ui-amplify = УСИЛИТЬ
research-sim-ui-amplify-tooltip = Повысить выбранное свойство на один уровень. Эта операция снижает порог передозировки.
research-sim-ui-suppress = ПОДАВИТЬ
research-sim-ui-suppress-tooltip = Понизить выбранное свойство на один уровень. Эта операция снижает порог передозировки.
research-sim-ui-relate = ЗАМЕНИТЬ
research-sim-ui-relate-tooltip = Использовать эталонное вещество для замены одного выбранного свойства в целевом веществе. Уровни выбранного свойства у целевого и эталонного вещества должны совпадать. Эта операция снижает порог передозировки.
research-sim-ui-add = ДОБАВИТЬ
research-sim-ui-add-tooltip = Использовать свойство эталонного вещества, чтобы добавить его в целевое вещество без отрицательных последствий для целевого вещества. Однако операция повреждает химическую структуру эталонного вещества, делая любые дальнейшие модификации невозможными.

research-sim-ui-no-data = [color=black][bold]Данные не введены![/bold][/color]

research-sim-ui-target-data = [head=3]Данные целевого вещества[/head]
research-sim-ui-reference-data = [head=3]Данные эталонного вещества[/head]
research-sim-ui-price = [bold]Стоимость операции: {$COST}[/bold]

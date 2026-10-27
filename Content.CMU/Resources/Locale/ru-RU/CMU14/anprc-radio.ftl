anprc-window-title = Тактическая радиостанция AN/PRC-117G

anprc-transmit-hint-header = ПЕРЕДАЧА
anprc-transmit-hint-active = :r передаёт по активной предустановке

anprc-power-off-button = ВЫКЛЮЧИТЬ
anprc-power-on-button = ВКЛЮЧИТЬ

anprc-status-equipped = НАДЕТА
anprc-status-unequipped = НЕ НАДЕТА
anprc-status-on = ВКЛ.
anprc-status-off = ВЫКЛ.

anprc-slot-empty-display = НЕТ КАНАЛА


anprc-radio-off = Радиостанция не издаёт ни звука. Она выключена.
anprc-not-authorized = Радиостанция щёлкает. Вы не обучены работе с этим оборудованием.
anprc-no-active-slot = Ни одна предустановка не активна. Сначала добавьте радиосеть.
anprc-slot-empty = Предустановке { $slot } не назначен канал.
anprc-no-tower = Помехи. Для { $channel } требуется вышка связи или квалифицированный ретранслятор RTO.
anprc-not-rto-warning = Вы не обучены работе с этой радиостанцией. Она не будет ретранслировать радиосети у вас за спиной, и вы не сможете передавать.
anprc-verb-open = Открыть панель радиостанции
anprc-out-of-range = Помехи. В радиусе действия нет ретранслятора для { $channel }.

anprc-frequency-invalid = Неверный формат частоты. Введите число, например 1606 или 1.606.
anprc-frequency-out-of-band = На этой частоте радиосеть работать не может. Прямые частоты находятся в диапазоне 1.000–2.999 МГц или в softwave-диапазоне 30.000–87.999 МГц.
anprc-frequency-not-found = На частоте { $freq } канал не найден.
anprc-frequency-set = [{ $slot }] настроена на { $freq } МГц.
anprc-frequency-set-net = [{ $slot }] настроена на { $freq } МГц — радиосеть { $channel }.
anprc-frequency-set-unknown = [{ $slot }] настроена на { $freq } МГц — неопознанная радиосеть. Радиообмен будет записываться.
anprc-frequency-set-dynamic = [{ $slot }] установлена на { $freq } МГц (прямая частота) — радиосеть не назначена. Для передачи используйте :r.

anprc-frequency-card-fallback =
    {"["}head=2]ИНСТРУКЦИЯ ПО ОРГАНИЗАЦИИ СВЯЗИ[/head]
    {"["}head=3]НАЗНАЧЕНИЕ ЧАСТОТ РАДИОСЕТЕЙ AN/PRC-117G[/head]

    {"["}bold]Командование GOVFOR[/bold] - 2.592 МГц
    {"["}bold]GOVFOR Альфа[/bold]      - 2.502 МГц
    {"["}bold]GOVFOR Браво[/bold]      - 1.606 МГц
    {"["}bold]GOVFOR Чарли[/bold]      - 1.607 МГц
    {"["}bold]Разведка GOVFOR[/bold]   - 1.605 МГц
    {"["}bold]JTAC GOVFOR[/bold]       - 2.598 МГц
    {"["}bold]MILP GOVFOR[/bold]       - 2.595 МГц

    Введите частоту во вкладке FREQ панели радиостанции (с точкой или без неё: 2592 и 2.592 означают одно и то же), чтобы назначить соответствующую радиосеть ячейке предустановки.

    {"["}italic]УВЕДОМЛЕНИЕ COMSEC: Эта карточка является документом ограниченного доступа. Уничтожить перед угрозой захвата. Частоты действуют на всём театре военных действий; исходите из того, что противник располагает собственной копией.[/italic]

anprc-slot-max-reached = Достигнуто максимальное количество предустановок (4). Сначала удалите одну из них.

anprc-monitor-no-transmit = МОНИТОРИНГ АКТИВЕН — радиостанция работает только на приём. Отключите MON, чтобы передавать.

anprc-ct-mode-no-fill = РЕЖИМ CT — для передачи требуется криптографический ключ. Сначала загрузите ключ COMSEC.

anprc-scan-switched = СКАНИРОВАНИЕ — радиообмен на [{ $label }] (P{ $slot } · { $channel }). Активная радиосеть переключена.

anprc-squelch-suppressed = *шумоподавление*

anprc-crypto-not-equipped = Для загрузки криптографического ключа радиостанция должна быть надета.
anprc-crypto-already-loaded = Уже загружено: { $designation }. Сначала выполните обнуление.
anprc-crypto-loaded = { $designation } загружено. Передачи зашифрованы.
anprc-crypto-zeroized = { $designation } обнулено. Передачи не защищены.
anprc-crypto-destroyed = { $designation } физически уничтожено. Ключ невозможно восстановить.
anprc-crypto-no-card = Криптографический ключ не загружен.
anprc-crypto-wrong-faction = Это устройство загрузки ключей несовместимо с вашей радиостанцией.
anprc-crypto-examine-empty = Криптографический ключ не загружен. Передачи не защищены.
anprc-crypto-examine-loaded = { $designation } загружено ({ $faction }).
anprc-crypto-examine-stale = { $designation } загружено ({ $faction }) — УСТАРЕЛО, больше не защищает радиообмен.

anprc-comsec-unsecured = ПРЕДУПРЕЖДЕНИЕ COMSEC: Передача по { $channel } ({ $faction }) без криптографического ключа. Радиообмен доступен для чтения всем сторонам.

anprc-recrypto-no-card = Действующая карта ключа не загружена. Сначала вставьте карту ключа своей фракции.
anprc-recrypto-stale-card = Эта карта уже была признана устаревшей. Для смены ключей вставьте актуальную карту.
anprc-recrypto-foreign-card = СМЕНА КЛЮЧЕЙ ОТКЛОНЕНА — загруженный ключ не соответствует органу, выдавшему эту радиостанцию.
anprc-recrypto-ordered = СМЕНА КЛЮЧЕЙ COMSEC ПРИКАЗАНА: все карты ключей { $faction }, выданные до этого приказа, теперь считаются устаревшими. Запросите замену через обычный канал снабжения.
anprc-recrypto-not-authorized = СМЕНА КЛЮЧЕЙ ОТКЛОНЕНА — для смены ключей требуются командные полномочия COMSEC.
anprc-recrypto-button = ПРИКАЗАТЬ СМЕНУ КЛЮЧЕЙ — АННУЛИРОВАТЬ КЛЮЧИ ФРАКЦИИ
anprc-recrypto-button-confirm = НАЖМИТЕ ЕЩЁ РАЗ ДЛЯ ПОДТВЕРЖДЕНИЯ — АННУЛИРУЕТ ВСЕ КЛЮЧИ ФРАКЦИИ
anprc-recrypto-superseded-notice = СМЕНА КЛЮЧЕЙ COMSEC — загруженный вами ключ устарел. Запросите замену.

anprc-battery-depleted = Радиостанция разряжена. Вставьте батарею.
anprc-battery-empty = AN/PRC-117G выключается: батарея разряжена.
anprc-battery-insufficient = Недостаточно заряда батареи для передачи.

anprc-unknown-station = НЕИЗВЕСТНАЯ СТАНЦИЯ
anprc-radio-check-call = ВСЕМ СТАНЦИЯМ, Я { $station }, ПРОВЕРКА СВЯЗИ, ПРИЁМ.
anprc-radio-check-report = ОТВЕТЫ НА ПРОВЕРКУ СВЯЗИ — ПРИЁМ РАЗБОРЧИВЫЙ: { $clear } | ПРИЁМ С ПОМЕХАМИ: { $degraded }
anprc-radio-check-nothing-heard = ОТВЕТА НЕТ
anprc-radio-check-interference = ПОМЕХИ В РАДИОСЕТИ — азимут сильнейшего источника { $bearing }.

anprc-verb-plant = Развернуть ретранслятор
anprc-verb-packup = Свернуть радиостанцию
anprc-retrans-planted = Радиостанция закреплена на месте и начинает работать как автономный ретранслятор.
anprc-retrans-packed = Ретрансляционная станция свёрнута обратно в переносную радиостанцию.
anprc-retrans-pickup-blocked = Она закреплена как ретрансляционная станция. Сначала сверните её.

anprc-verb-handset = Взять трубку
anprc-verb-handset-release = Повесить трубку
anprc-handset-taken = Вы снимаете проводную трубку с { $radio }.
anprc-handset-released = Вы возвращаете трубку на { $radio }.
anprc-handset-in-use = Кто-то уже использует эту трубку.
anprc-handset-hands-full = Чтобы взять трубку, нужна свободная рука.
anprc-handset-cord = Провод вырывает трубку из вашей руки, когда вы отходите слишком далеко.
anprc-handset-radio-gone = Трубка отключается.
anprc-handset-hint = Пока вы держите трубку, обычная речь передаётся по активной радиосети станции. Говорите шёпотом, чтобы не выходить в эфир.

# Поисковый приёмник
anprc-sweep-started = Радиостанция покидает радиосеть и начинает сканировать диапазон. Пока вы не остановите поиск, вы ничего не услышите и не сможете передавать.
anprc-sweep-needs-online = Для поиска радиостанция должна быть включена и надета.
anprc-sweep-aborted = Поиск прекращается из-за отключения радиостанции.
anprc-sweep-aborted-power = Батарея разряжается, и поиск прекращается.
anprc-sweep-tx-blocked = Радиостанция сканирует диапазон. Остановите поиск перед передачей.
anprc-sweep-contact = Поиск сужается. Что-то передаёт на частоте { $freq } МГц.
anprc-sweep-resolved = ОБНАРУЖЕНО: { $freq } МГц — { $net }. Передачи носимых станций в этой сети теперь дают приблизительный пеленг, а каждое сообщение добавляет попытку подбора ключа на панели.
anprc-sweep-unknown-net = НЕОПОЗНАННАЯ РАДИОСЕТЬ

# Журнал радиосети на бумаге
anprc-log-print-empty = В журнале нет ничего, что стоило бы записывать.
anprc-log-printed = Вы переписываете на бумагу { $count } записей журнала.

anprc-log-frequency-unknown = ЧАСТОТА НЕИЗВ.

# Языки, которые невозможно передавать по радио
anprc-language-no-radio = { $language } невозможно передать по радиосети.

anprc-chat-intercept-line = [color={ $color }]\[INT\] "{ $message }"[/color]

anprc-fixed-net-bearing = ПЕЛЕНГ { $net }: направление на говорящего — { $bearing }, { $band ->
    [near] близко.
    [mid] на некотором расстоянии.
   *[far] далеко.
}

anprc-chat-label = 117G

anprc-chat-intercept-label = ИНТ

anprc-chat-intercept-verb = включён

anprc-key-untrained = Вы не знаете, как взломать ключ. Это требует обучения радиооператоров.

anprc-key-offline = Набор должен быть включен и либо носиться, либо ставиться на работу с ключом.

anprc-key-no-fix = Сначала почините сеть этой фракции. У вас пока нет трафика для анализа.

anprc-key-bad-trial = Судебный процесс должен быть { $length } символы из { $symbols }.

anprc-key-no-depth = Никаких судебных разбирательств. Ждите трафика в сети, которую вы починили.

anprc-key-trial-result = судебный { $trial }: { $hits } из { $length } на месте.

anprc-key-broken = Ключ сломан. Этот набор читает { $faction } Трафик до тех пор, пока они не перекодируются. Они не могут передавать информацию по сети.

anprc-quick-setup-loaded = Стандартные загруженные сети: { $nets }.

anprc-quick-setup-nothing = Стандартные сети уже загружены.

anprc-quick-setup-full = Нет свободной памяти для: { $nets }Сначала удалите или опустошите память.

anprc-call-says = { $name } говорит: "{ $message }"

anprc-call-no-fill = Нет заполнения COMSEC. Набор не имеет фракционной сети для вызова.

anprc-call-no-link = Нет связи. Набор выключен, уложен, заклинило, из власти или поиска.

anprc-call-no-link-target = Нет связи. Эта станция вне досягаемости - между вами нет релейного пути или прямой связи.

anprc-call-incoming = Твой AN/PRC-117G Звонит: { $caller }Возьми трубку, чтобы ответить.

anprc-call-incoming-connected = Входящий звонок от { $caller } - подключен через телефон.

anprc-call-ended = Повесь трубку.

anprc-call-far-end-hung-up = Линия мертва. Другой конец повис.

anprc-call-link-lost = Звонок падает. Линк потерялся.

anprc-call-net-blocked = Телефон звонит. Подождите, прежде чем снова использовать радио.

anprc-verb-phone = Используйте телефон

anprc-emcon-on = ЭМКОН: Сет молчит. Он не будет передавать, ретранслировать или звонить и использует очень мало энергии. Он может получать трафик.

anprc-emcon-off = ЭМКОН поднялся. Набор может передавать и ретранслировать снова.

anprc-emcon-no-transmit = Набор находится в EMCON и не может передавать. Отключите EMCON на странице OPT.

anprc-retrans-needs-staked = Ретранс работает только тогда, когда набор поставлен на карту.

anprc-retrans-needs-nets = Выберите два разных воспоминания, каждое из которых содержит названную сеть.

anprc-retrans-bridged = Retrans Active: трафик { $a } повторяется на { $b }и наоборот.

anprc-retrans-relayed = [RXMT] { $speaker }: { $message }

anprc-return-to-auto-done = Съёмочная площадка вернулась на АВТО.

anprc-peak-needs-staked = Выберите настройки и включите их перед пиком антенны.

anprc-peak-already = Антенна уже установлена.

anprc-peak-start = Вы начинаете прицеливаться и настраивать антенну.

anprc-peak-done = Антенна достигла пика. Диапазон реле увеличивается до тех пор, пока набор не будет упакован.

anprc-otar-needs-current-fill = Вам нужен текущий ключ вашей фракции, прежде чем отправлять его по воздуху.

anprc-otar-cooldown = Набор все еще ездит на велосипеде после последнего толчка ключа.

anprc-otar-none = Ни один доступный набор не имеет устаревшего ключа.

anprc-otar-sent = Ключ, посланный по воздуху { $count ->
    [one] один набор
   *[other] { $count } набор
}.

anprc-otar-received = Ключ COMSEC вашего набора был обновлен по воздуху { $station }.

anprc-dwell-needs-search = Начните поиск группы перед парковкой головы поиска.

anprc-dwell-on = Голова поиска припаркована на контакте. Остальная часть группы игнорируется, пока вы не выпустите ее.

anprc-dwell-lost = Этот контакт исчез или уже был урегулирован.

anprc-jammer-none = Никакие помехи не влияют на набор.

anprc-jammer-bearing-first = Жаммерный подшипник { $bearing }Двигайся хотя бы { $baseline } Плитку и взять второй подшипник, чтобы зафиксировать ее положение.

anprc-jammer-baseline-short = Жаммерный подшипник { $bearing }Но вы только переехали { $moved } из { $baseline } Плитка с первого положения. Двигайтесь дальше, прежде чем брать второй подшипник.

anprc-jammer-fixed = FIX: подшипники пересекаются { $bearing }примерно { $distance } Плитки прочь. Джаммер отметил на карте вашей фракции { $minutes } минуты.

anprc-antenna-none = НЕТ

anprc-log-report-title = NET LOG

anprc-log-report-title-intercepts = МЕЖДУНАРОДНЫЙ ЛОГ

anprc-log-report-station = Начало:

anprc-log-report-entries = ВЕДОМОСТИ:

anprc-log-report-intercept = (Интерцепт)

anprc-log-report-footer = Переписано с одного AN/PRC-117G сетевой журнал. Время использует установленные часы, а не местное время.

anprc-bearing-n = N

anprc-bearing-ne = Нэ

anprc-bearing-e = Е

anprc-bearing-se = Шеф.

anprc-bearing-s = S

anprc-bearing-sw = SW

anprc-bearing-w = WWW

anprc-bearing-nw = NW

anprc-antenna-whip = Свист

anprc-antenna-wire = Жить

anprc-antenna-mast = мачта

# Callsign controls; station identities are supplied in the server language.
anprc-ui-callsign = ПОЗЫВНОЙ
anprc-ui-callsign-not-set = НЕ НАЗНАЧЕН
anprc-ui-callsign-placeholder = До 16 символов: САБЛЯ 01
anprc-ui-callsign-auto = { $callsign } (АВТО)
anprc-ui-set-button = ЗАДАТЬ
anprc-ui-callsign-help = Позывной станции. Пустое поле — позывной носителя. Нажмите «ЗАДАТЬ», чтобы применить.
anprc-ui-callsign-directory = СПРАВОЧНИК ПОЗЫВНЫХ
anprc-ui-callsign-presets = ШАБЛОНЫ
anprc-ui-radio-check = ПРОВЕРКА СВЯЗИ

anprc-op-issue-plain-detail = Вас может прочитать кто угодно, помехи действуют сильнее, а вражеский пеленгатор может определить направление на вас. Для экстренных случаев.

anprc-op-set-mode-fh = Перестройка частоты. Защищает от помех и позволяет передавать в зоне подавителей. Расход аккумулятора x1.5; противник, обнаруживший сеть, может получить пеленг.

anprc-op-set-mode-sc = Один канал. Защищённый и экономичный режим (расход аккумулятора x0.75). При шифровании вражеский пеленгатор не получает пеленга. Защиты от помех нет.

anprc-op-set-mode-pt = Открытый текст. Вас может прочитать кто угодно, помехи действуют сильнее, а противник может получить пеленг. Расход аккумулятора вдвое ниже. Для экстренных случаев.

anprc-op-set-power-lo = Низкая. Дальность ретрансляции x0.6, расход аккумулятора вдвое ниже. Вражеским поисковым приёмникам нужно подойти ближе, чтобы услышать вас.

anprc-op-set-power-med = Средняя. Обычная дальность и расход аккумулятора.

anprc-op-set-power-hi = Высокая. Дальность ретрансляции x1.5, расход аккумулятора вдвое выше. Вражеские поисковые приёмники слышат вас с большего расстояния.

anprc-op-view-guided = ВИДЕО: ОБРАЗОВАНО

anprc-op-view-expert = ВИУ: 117G Панель

anprc-op-view-tooltip = Переключение между направляемой панелью и AN/PRC-117G Лицевая пластина. Лицевая панель работает как реальный набор: клавиатура, переключатель функций и экранные страницы. Оба контролируют одно и то же радио.

anprc-op-help = Помогите.

anprc-op-help-tooltip = Покажите оператору брифинг еще раз.

anprc-op-power-on = Власть на

anprc-op-power-off = ПИТАНИЕ ОТКЛЮЧЕНО

anprc-op-radio-check = Радио Чек

anprc-op-radio-check-tooltip = Отправляет радио-чек в активную сеть и показывает, кто вас слышит.

anprc-op-radio-check-unavailable = Радио-проверка нуждается в наборе, ношении или установке с выбранной сетью.

anprc-op-footer-worn = работать

anprc-op-footer-planted = PLANTED (RETRANS)

anprc-op-footer-stowed = Не работать

anprc-op-more-below = БОЛЬШЕ ПОЛУЧШЕНИЙ - СКРОЛЛ ИЛИ ЗДЕСЬ

anprc-op-more-below-tooltip = Эта страница длиннее окна. Прокрутите колесо мыши или нажмите, чтобы спрыгнуть вниз.

anprc-op-tab-nets = NETS

anprc-op-tab-nets-tooltip = Ваши сетевые воспоминания: что вы передаете и что передаете.

anprc-op-tab-log = LOG

anprc-op-tab-log-tooltip = Все, что было слышно, включая перехват вражеского трафика.

anprc-op-tab-security = Безопасность

anprc-op-tab-security-tooltip = Шифрование: ваша карта заполнения и ее элементы управления.

anprc-op-tab-search = Поиск

anprc-op-tab-search-tooltip = Ищите вражеские сети.

anprc-op-tab-settings = Сеттинги

anprc-op-tab-settings-tooltip = Как работает набор, монитор и сканирование, ваш позывной, телефон - и Вернуться в Авто.

anprc-op-tab-badge = { $tab } ({ $count })

anprc-op-tab-alert = { $tab } !

anprc-op-lcd-off = ВЫКЛ

anprc-op-lcd-secure = { $mode } Секунда

anprc-op-lcd-unsecured = { $mode } Не трогательно

anprc-op-lcd-clear = { $mode } В чистом виде

anprc-op-lcd-searching = Обсуждение BAND SEARCH - OFF ALL NETS

anprc-op-lcd-direct = Прямая частота

anprc-op-lcd-no-memories = Нет сетей в памяти

anprc-op-lcd-no-net = Нет выделенных

anprc-op-lcd-talk = TALK: тип :r и ваше сообщение

anprc-op-lcd-talk-off = Убрать

anprc-op-lcd-talk-monitor = Слушать онлайн - Can't Transmit

anprc-op-lcd-talk-searching = Оригинальное название: Can't Transmit

anprc-op-lcd-talk-stowed = Не работает - не передаётся

anprc-op-lcd-talk-no-net = Никаких разговоров.

anprc-op-lcd-signal = СИГ { $bars }

anprc-op-lcd-signal-direct = СИГ ДИРЕКТ

anprc-op-lcd-battery = Бэт { $bars }

anprc-op-lcd-no-battery = Никакой еды.

anprc-op-lcd-relay = ОТВЕТ { $count } { $count ->
    [one] NET
   *[other] NETS
} / { $range }ТТ

anprc-op-lcd-relay-none = Не рассчитывая

anprc-op-lcd-station = Св. { $callsign }

anprc-op-ready-title = Оригинальное название: R Talks on { $label }

anprc-op-ready-detail = Type :r followed by your message to transmit. Click another net on the NETS page to switch.

anprc-op-ready-detail-relaying = You're relaying { $count } { $count ->
    [one] net
   *[other] nets
} to headsets within { $range } tiles. Type :r followed by your message to transmit.

anprc-op-also = Также: { $issue }

anprc-op-issue-no-battery = Нет батареи

anprc-op-issue-no-battery-detail = Набор не будет работать без камеры. Положите заряженный силовой элемент в пакет.

anprc-op-issue-off = Радио отключено

anprc-op-issue-off-detail = Ничто не передается, и вы не можете передавать, пока набор выключен.

anprc-op-issue-stowed = Упаковка не на твоей спине

anprc-op-issue-stowed-detail = Вы можете запрограммировать его сейчас, но он передает и ретранслирует только во время ношения в заднем слоте или в качестве станции ретрансляции.

anprc-op-issue-untrained = Носитель не обученный оператор

anprc-op-issue-untrained-detail = Изношенный пакет ретранслируется только для обученного радиооператора. Ни на ком другом он не будет обеспечивать ретрансляцию.

anprc-op-issue-searching = Поисковый приемник работает

anprc-op-issue-searching-detail = Ты выронил все сети. Вы не можете слышать или передавать, пока поиск не прекратится.

anprc-op-issue-squad-missing = Ваша сеть отрядов{ $net }) не загружается

anprc-op-issue-standard-missing = Стандартные сети отсутствуют: { $nets }

anprc-op-issue-standard-missing-detail = Боевые гарнитуры нуждаются в соседней реле, несущей их сеть. Ваш пакет передает каждую дружественную сеть в памяти, поэтому они должны быть загружены.

anprc-op-issue-no-nets = Нет сетей в памяти

anprc-op-issue-no-nets-detail = Добавьте память и выберите сеть для нее на странице NETS.

anprc-op-issue-no-active = Нет сети, выбранной для разговора

anprc-op-issue-no-active-detail = Ваши воспоминания загружены, но ни один не выбран для :r.

anprc-op-issue-monitor = Слушайте только на

anprc-op-issue-monitor-detail = Вы слышите каждую сеть в памяти, но :r не может передавать.

anprc-op-issue-ct-no-fill = Режим КТ без заполнения карты

anprc-op-issue-ct-no-fill-detail = Цифровой текст нуждается в заполнении шифрования. Пока один не загружен, набор не будет передавать.

anprc-op-issue-no-fill = Не зашифрованный

anprc-op-issue-no-fill-detail = Противник может читать то, что вы посылаете. Загрузите свою карточку в пакет.

anprc-op-issue-stale-fill = Заполнение карты заменено

anprc-op-issue-stale-fill-detail = Командование выпустило новый ключ. Ваша карта устарела и больше не обеспечивает трафик. Получите текущую карту.

anprc-op-issue-plain = Передача в чистом виде (ПТ)

anprc-op-issue-weak-link = Слабые ссылки в этой сети

anprc-op-issue-no-link = Никакая эстафета не покрывает эту сеть здесь

anprc-op-issue-link-detail = Вы находитесь на краю или снаружи ретрансляции для этой сети. Подойдите ближе к реле или загрузите сеть в свой собственный пакет.

anprc-op-issue-low-battery = Низкая батарея

anprc-op-issue-low-battery-detail = Замените камеру в ближайшее время. Сила SC и LO может продлить оставшийся заряд.

anprc-op-action-power-on = Повернись

anprc-op-action-quick-setup = Повернитесь и наклоните стационарные сети

anprc-op-action-stop-search = Хватит искать.

anprc-op-action-load-nets = Груз { $nets }

anprc-op-action-open-nets = Открытые сети

anprc-op-action-use-net = Говори. { $label }

anprc-op-action-monitor-off = Повернись, слушай только

anprc-op-action-mode-fh = Щелкните на FH

anprc-op-action-open-security = Открытая безопасность

anprc-op-nets-intro = Каждая память содержит одну сеть. Каждая дружественная боевая сеть в памяти передается на близлежащие гарнитуры, поэтому держите свою команду и командные сети заряженными.

anprc-op-nets-empty = Никаких воспоминаний. Используйте стандартную кнопку «Сети» выше или добавьте память и выберите сеть для нее.

anprc-op-quick-setup-title = Стандартные сети

anprc-op-quick-setup-text = грузы { $nets } в свободные воспоминания и выбирает сеть для передачи. Ничто из того, что вы уже настроили, не перезаписано.

anprc-op-quick-setup-button = LOAD STANDARD NETS

anprc-op-quick-setup-button-off = Повернитесь и наклоните стационарные сети

anprc-op-quick-setup-tooltip = Обрабатывает нормальную настройку: включает установку, загружает отсутствующие стандартные сети и выбирает одну для передачи. Режим, шифрование и мощность остаются неизменными.

anprc-op-add-memory = + память

anprc-op-add-memory-free = + ДОБРОЧНАЯ ПАМЯТЬ ({ $free } из { $max } бесплатно

anprc-op-add-memory-full = Все воспоминания в использовании

anprc-op-add-memory-tooltip = Добавьте пустую память. Назовите его сейчас, затем выберите его сеть с EDIT.

anprc-op-add-memory-placeholder = Имя, вплоть до 8 буквы (факультативно)

anprc-op-add = ДОБАВИТЬ

anprc-op-cancel = ОТМЕНА

anprc-op-done = Сделано

anprc-op-net-use = использовать

anprc-op-net-use-tooltip = Поговорите об этом в сети. :r будет передаваться здесь.

anprc-op-net-active = В воздухе

anprc-op-net-active-tooltip = Вы транслируете через эту сеть.

anprc-op-net-edit = Редактировать

anprc-op-net-edit-tooltip = Измените сеть, введите частоту, переименуйте или удалите эту память.

anprc-op-net-direct = Прямая частота

anprc-op-net-unknown = Неопознанная сеть (перехват)

anprc-op-net-status-empty = Empty - нажмите EDIT, чтобы выбрать сеть

anprc-op-net-status-talking = Ты говоришь здесь.

anprc-op-net-status-listening = Выбрано (только для прослушивания)

anprc-op-net-status-hearing = Слышал

anprc-op-net-status-relayed = Передано в близлежащие гарнитуры

anprc-op-net-status-not-relayed = Не передано прямо сейчас

anprc-op-net-status-foreign = Enemy Net — слушайте только, никогда не передавайте

anprc-op-net-status-direct = Сырая частота - не ретранслируется

anprc-op-editor-title = Редактировать { $label }

anprc-op-editor-pick-net = Выберите сеть:

anprc-op-editor-net-entry = { $frequency }  { $net }  { $tag }

anprc-op-editor-tag-squad = [YOUR SQUAD]

anprc-op-editor-tag-standard = [STANDARD]

anprc-op-editor-tag-intercept = [INTERCEPT]

anprc-op-editor-tag-in-memory = [IN MEMORY]

anprc-op-editor-intercept-tooltip = Вражеская сеть, установленная вашим поисковым приемником. Вы можете слушать и записывать его, но он никогда не передается.

anprc-op-editor-no-nets = Нет доступных сетей. Вместо этого введите частоту.

anprc-op-editor-key-frequency = Ключевая частота:

anprc-op-editor-frequency-placeholder = Например. 250.2 или 2502

anprc-op-editor-tune = Тюн

anprc-op-editor-frequency-help = Частота, соответствующая одной из ваших известных сетей, загружает эту сеть. Все остальное становится прямой частотой: нераспространенной и понятной каждому, кто ее найдет.

anprc-op-editor-rename = Переименовать:

anprc-op-editor-rename-button = Возвращение

anprc-op-editor-empty = Поспешите

anprc-op-editor-empty-tooltip = Очистите сеть от этой памяти, но сохраните слот памяти.

anprc-op-editor-delete = Удалить память

anprc-op-editor-delete-confirm = Нажмите снова, чтобы удалить

anprc-op-editor-delete-tooltip = Удалите эту память полностью. Требуется второй клик.

anprc-op-log-intro = Набор записывает все, что слышит, сначала новейшее. Вражеский трафик отмечен Интерцептом. Печать журнала, когда вам нужно передать его.

anprc-op-log-search-placeholder = Отправитель поиска или сообщение

anprc-op-log-intercepts-only = Интерцепты

anprc-op-log-intercepts-only-tooltip = Показывать только вражеский трафик.

anprc-op-log-all-nets = Все сети

anprc-op-log-print = ПРИНТ ЛОГ

anprc-op-log-print-tooltip = Распечатайте полный журнал на бумаге.

anprc-op-log-print-intercepts = ПРИНСТОВЫЕ ИНТЕРЕКТЫ

anprc-op-log-print-intercepts-tooltip = Печать только перехватывала вражеский трафик.

anprc-op-log-count = { $count } из { $max } линии,

anprc-op-log-count-filtered = Показывать { $shown } из { $total } линии

anprc-op-log-empty = Пока ничего не слышно.

anprc-op-log-no-match = Ничто не соответствует фильтру.

anprc-op-log-header = [{ $time }] { $sender } - { $net }

anprc-op-log-header-intercept = [{ $time }] { $sender } - { $net } - Интерцепт

anprc-op-sec-status-secure = запечатлённый

anprc-op-sec-status-none = Не запечатлённый

anprc-op-sec-status-stale = ПОЛНЫЙ ПОДПИСАНИЕ

anprc-op-sec-status-plain = In The Clear (альбом)

anprc-op-sec-detail-secure = Ваш трафик зашифрован. Противник перехватчика слышит статическое.

anprc-op-sec-detail-none = Заряженной карты нет. Противники могут читать все, что вы отправляете.

anprc-op-sec-detail-stale = Командование изменило ключ. Ваша карта устарела, поэтому ваш трафик доступен для чтения. Получите текущую карту.

anprc-op-sec-detail-plain = Режим PT передает в чистоте даже при загруженном заполнении. Переключитесь на FH, SC или CT в SETTINGS для шифрования.

anprc-op-sec-fill = Заполнить: { $designation } ({ $faction })

anprc-op-sec-fill-none = Заполнение: нет

anprc-op-sec-how-to = Чтобы загрузить шифрование, используйте выданную карту заполнения на упаковке.

anprc-op-sec-actions = Заполните действия

anprc-op-sec-actions-help = Эти действия требуют подтверждения. Уничтожить или уничтожить заливку до захвата набора.

anprc-op-sec-zeroize = ЗЕРОИЗ

anprc-op-sec-zeroize-confirm = Нажмите снова на норойзе

anprc-op-sec-zeroize-tooltip = Удалите заполнение из набора и выбросьте карту.

anprc-op-sec-destroy = Уничтожение

anprc-op-sec-destroy-confirm = Нажмите снова, чтобы уничтожить

anprc-op-sec-destroy-tooltip = Снимите заполнение и уничтожьте карту, чтобы ее нельзя было захватить.

anprc-op-sec-recrypto = Порядок РЕКРИПТО

anprc-op-sec-recrypto-confirm = Нажмите снова на Recypto

anprc-op-sec-recrypto-tooltip = Заменяет каждую карточку, которую держит ваша фракция. Используйте это после того, как карта или набор скомпрометированы. Требуется командная власть.

anprc-op-search-intro = Поисковый приемник сканирует группу на сети других фракций. Каждая передача, которую он ловит, улучшает исправление. После решения сеть может быть загружена в память и контролироваться.

anprc-op-search-cost = Поиск снимает вас с каждой загруженной сети. Вы не можете услышать их или передать, пока идет поиск.

anprc-op-search-running-warning = Поиск - вы не в каждой сети и не можете передавать.

anprc-op-search-offline = Набор должен быть включен и изношен или посажен для поиска.

anprc-op-search-start = Начать поиск

anprc-op-search-stop = Хватит искать

anprc-op-search-head = голова { $frequency }

anprc-op-search-idle = ОЖИДАНИЕ

anprc-op-search-contacts = Контакты

anprc-op-search-no-contacts = Контактов пока нет. Занятые сети легче найти.

anprc-op-search-partial = ~{ $frequency }  исправлять { $tier }/{ $max }

anprc-op-search-partial-help = Частичное исправление. Продолжайте поиск, пока сеть активна, чтобы решить все остальное.

anprc-op-search-own = { $frequency }  { $net } (ваш)

anprc-op-search-fixed = { $frequency }  { $net }

anprc-op-search-tune-into = Настроить на:

anprc-op-search-tune-tooltip = Загрузите эту сеть в память. Предыдущее содержание заменяется.

anprc-op-search-no-memory = Сначала добавьте память

anprc-op-set-mode = ВАВЕФОРМА

anprc-op-set-mode-ct = Цифровой текст. Скрытые от перехватчиков и ДФ, но только другие 117G Наборы с соответствующим заполнением могут его прочитать. Обычные гарнитуры слышат статично. Требуется заполнить карту.

anprc-op-set-power = Внешняя сила

anprc-op-set-squelch = Сквелч

anprc-op-set-squelch-0 = 0Открыто. Услышьте все, включая плохо деградировавший трафик.

anprc-op-set-squelch-1 = 1Мьеты тяжелые статические и трафик вы не можете расшифровать.

anprc-op-set-squelch-2 = 2Также приглушает умеренно заторможенный трафик.

anprc-op-set-squelch-3 = 3 (по умолчанию): также приглушает слегка забитый и сильно деградировавший пограничный трафик.

anprc-op-set-squelch-4 = 4Максимум. Даже слегка деградировавший дальний трафик приглушен. Тихие, но слабые станции могут исчезнуть.

anprc-op-set-receive = Получение

anprc-op-set-monitor-off = Только слушай:

anprc-op-set-monitor-on = Только слушай:

anprc-op-set-monitor-help = Услышьте каждую сеть в памяти сразу. Вы не можете передавать, пока это включено.

anprc-op-set-scan-off = СКАН: вылет

anprc-op-set-scan-on = СКАН: ОН

anprc-op-set-scan-help = Услышьте каждую сеть в памяти и переместите свою активную сеть разговоров в ту, которая в последний раз получала трафик.

anprc-op-set-station = Начало звонка

anprc-op-set-callsign-manual = { $callsign } (устанавливается вручную)

anprc-op-set-callsign-from-wearer = { $callsign } (знак звонка)

anprc-op-set-callsign-unknown = НЕИЗВЕСТНАЯ СТАНЦИЯ

anprc-op-set-callsign-help = Все отправленное через пакет использует этот позывной. По умолчанию он использует ваш. Установите его вручную, когда радиостанция идентифицируется как стационарная, например, PLT MAIN.

anprc-op-set-callsign-placeholder = Звонок со станции

anprc-op-set-callsign-set = УСТАНОВИТЬ

anprc-op-set-callsign-auto = Используйте мой собственный звонок

anprc-op-set-callsign-auto-tooltip = Очистите позывной фиксированной станции и вернитесь к себе.

anprc-op-set-roster = Розтер

anprc-op-set-roster-tooltip = Обычная станция вызывает вашу фракцию.

anprc-op-set-directory = Открытая сетевая директива

anprc-op-set-directory-tooltip = Показывает, кто держит каждую позывную в сетях вашей фракции.

anprc-op-set-phone = телефон

anprc-op-set-phone-tooltip = Позвоните по другому радио или фиксированному телефону с помощью телефона. Также отвечает на звонок или вешает активный звонок.

anprc-op-intro-title = Вы - РАДИО-оператор

anprc-op-intro-lead = Твоя стая поддерживает связь с отрядом. Панель говорит вам, что требует внимания и что нажимать, так что вы можете узнать остальное, как вы используете его.

anprc-op-intro-point-1-title = 1Ты и есть надежда.

anprc-op-intro-point-1 = Боевые гарнитуры работают только вблизи реле, несущего их сеть. Ваша упаковка передает каждую дружественную сеть в памяти, поэтому ваша сеть должна быть загружена.

anprc-op-intro-point-2-title = 2Возвращайся в воздух.

anprc-op-intro-point-2 = Наденьте пакет на спину и нажмите LOAD STANDARD NETS. Это включает его, загружает ваш отряд и командные сети и выбирает сеть вашего отряда.

anprc-op-intro-point-3-title = 3Говори.

anprc-op-intro-point-3 = Тип :r, за которым следует ваше сообщение для передачи в сети с пометкой ON AIR. Нажмите USE на другую память для переключения сетей.

anprc-op-intro-point-4-title = 4Посмотреть Баннер

anprc-op-intro-point-4 = Полоса под экраном показывает, что нужно исправить дальше, и обычно дает вам кнопку, чтобы исправить это. Зеленый означает, что набор готов.

anprc-op-intro-point-5-title = 5Иди глубже, когда будешь готов.

anprc-op-intro-point-5 = Загрузите свою карту заполнения через SECURITY для шифрования. Панель управления поддерживает нормальные настройки на AUTO. Для более низкой мощности, режимов экономии батареи, поиска и других расширенных элементов управления используйте VIEW для открытия 117G Лицевая пластина.

anprc-op-intro-reopen = Пресса? Помогите в верхней части панели увидеть это снова.

anprc-op-intro-dismiss = Понял.

anprc-op-intro-guide = Откройте полное руководство

anprc-op-intro-guide-tooltip = Откройте страницы путеводителя для входа в сеть и использования AN/PRC-117G.

anprc-op-set-auto-heading = Как устроено место

anprc-op-set-auto-on = АВТО. Стандартная форма волны, выход и подавление, используя свой собственный позывной. Больше нечего устанавливать - выберите сеть и поговорите.

anprc-op-set-auto-off = Кто-то изменил этот набор с лицевой панели:

anprc-op-set-changed-mode = форма волны { $mode }

anprc-op-set-changed-power = выход { $power }

anprc-op-set-changed-squelch = удавка { $level }

anprc-op-set-changed-callsign = позывной станции { $callsign }

anprc-op-set-changed-burst = взрывная передача

anprc-op-set-changed-power-save = энергосбережение

anprc-op-set-changed-priority = Приоритетные часы { $slot }

anprc-op-set-changed-emcon = EMCON - набор не будет передавать или ретранслировать

anprc-op-set-changed-retrans = Мост Ретранс

anprc-op-set-changed-search = Поиск по полосе - каждая сеть отключена

anprc-op-set-return-to-auto = Вернуться в АВТО

anprc-op-set-return-to-auto-tooltip = Восстановите все настройки на панели до стандарта. Ваши воспоминания и наполнение остаются неизменными.

anprc-op-set-expert-hint = Выходная мощность, форма волны, сигналы фиксированной станции, приемник поиска, прямые частоты и передовые методы оператора контролируются с точки зрения EXPERT. Смотрите путеводитель для деталей.

anprc-op-set-callsign-guided-help = Набор обычно использует назначенный вами позывной. Звонки фиксированной станции настроены с точки зрения EXPERT.

anprc-op-editor-frequency-expert = Прямые частоты вводятся с просмотра EXPERT. Дружественные сети и любые сети, разрешенные поисковым приемником, появляются в списке выше.

anprc-op-std-net-entry = { $label } ({ $net })

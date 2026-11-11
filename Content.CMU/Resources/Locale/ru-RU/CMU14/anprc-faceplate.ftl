# Экспертная панель AN/PRC-117G. Все, что можно увидеть на стекле, лампах и 
# гравировках. Стекло представляет собой дисплей с символами фиксированной ширины: старайтесь, чтобы переводы 
# были короткими и набраны заглавными буквами, как и английский текст, иначе они будут обрезаны по краям.
# Буквенные группы на клавиатуре (ABC1, DEF2, ...) расположены не случайно — на них 
# вводятся позывные и названия.

## Общие слова
anprc-fp-on = ВКЛ
anprc-fp-off = ВЫКЛ
anprc-fp-auto = АВТО
anprc-fp-none = НЕТ
anprc-fp-unknown = НЕИЗВЕСТНО
anprc-fp-send = ПЕРЕДАТЬ
anprc-fp-stowed = УБРАН
anprc-fp-deployed = РАЗВЁРНУТ
anprc-fp-standby = ОЖИДАНИЕ
anprc-fp-no-net = НЕТ СЕТИ
anprc-fp-handset = ТРУБКА
anprc-fp-headset = ГАРНИТУРА
anprc-fp-callsign-auto = { $callsign } АВТО
anprc-fp-unavailable-off = СТАНЦИЯ ВЫКЛ - ИСПОЛЬЗУЙТЕ 6 PWR ИЛИ ПЕРЕКЛЮЧАТЕЛЬ
anprc-fp-note-wear-or-stake = НАДЕНЬТЕ ИЛИ ЗАКРЕПИТЕ СТАНЦИЮ, ЧТОБЫ РАБОТАТЬ В СЕТИ

## Режим, мощность, диапазоны
anprc-fp-mode-short-fh = FH
anprc-fp-mode-short-sc = SC
anprc-fp-mode-short-ct = CT
anprc-fp-mode-short-pt = PT
anprc-fp-mode-fh = ПЕРЕСКОК ЧАСТОТ
anprc-fp-mode-sc = ОДИН КАНАЛ
anprc-fp-mode-ct = ШИФР
anprc-fp-mode-pt = ОТКРЫТО
anprc-fp-power-lo = НИЗ
anprc-fp-power-med = СРЕД
anprc-fp-power-hi = ВЫС
anprc-fp-band-lf = LF
anprc-fp-band-hf = HF
anprc-fp-band-vhf = VHF
anprc-fp-band-uhf = UHF
anprc-fp-band-shf = SHF

## Содержимое памяти
anprc-fp-slot-net = { $freq } МГц  { $net }
anprc-fp-slot-direct = { $freq } МГц  ПРЯМОЙ
anprc-fp-slot-unknown-net = { $freq } МГц  НЕИЗВ. СЕТЬ
anprc-fp-slot-empty = --- ПУСТО ---

## Жкран OPT
anprc-fp-opt-title = OPT  НАСТРОЙКА
anprc-fp-opt-mode = РЕЖИМ
anprc-fp-opt-monitor = КОНТРОЛЬ
anprc-fp-opt-scan = СКАНИРОВАНИЕ
anprc-fp-opt-squelch = ШУМОДАВ
anprc-fp-opt-output = ВЫХОД
anprc-fp-opt-backlight = ПОДСВЕТКА
anprc-fp-opt-backlight-value = LT
anprc-fp-opt-techniques = ПРИЁМЫ
anprc-fp-opt-burst = ПАКЕТНАЯ ПЕРЕДАЧА
anprc-fp-opt-power-save = ЭНЕРГОСБЕРЕЖЕНИЕ
anprc-fp-opt-priority = ПРИОРИТЕТНЫЙ КОНТРОЛЬ
anprc-fp-opt-emcon = EMCON
anprc-fp-opt-emcon-silent = МОЛЧАНИЕ
anprc-fp-opt-staked = ЗАКРЕПЛЁННАЯ СТАНЦИЯ
anprc-fp-opt-station = СТАНЦИЯ
anprc-fp-opt-callsign = ПОЗЫВНОЙ
anprc-fp-opt-clear-override = СБРОС ОТКРЫТОГО РЕЖИМА
anprc-fp-opt-roster = СПИСОК
anprc-fp-opt-directory = СПРАВОЧНИК СЕТЕЙ
anprc-fp-opt-phone = ТЕЛЕФОН
anprc-fp-opt-radio-check = ПРОВЕРКА СВЯЗИ
anprc-fp-opt-status = СОСТОЯНИЕ
anprc-fp-opt-role = РОЛЬ
anprc-fp-role-retrans = РЕТРАНСЛЯЦИЯ
anprc-fp-role-manpack = НОСИМАЯ
anprc-fp-opt-antenna = АНТЕННА
anprc-fp-opt-relay = РЕЛЕЙ
anprc-fp-opt-relay-value = { $count } СЕТЕЙ { $full }/{ $partial }T
anprc-fp-opt-operator = ОПЕРАТОР
anprc-fp-opt-operator-untrained = НЕОБУЧЕН - БЕЗ РЕЛЕЯ
anprc-fp-opt-audio = ЗВУК
anprc-fp-opt-link = КАНАЛ
anprc-fp-opt-link-quality = КАЧЕСТВО
anprc-fp-opt-link-via = ЧЕРЕЗ
anprc-fp-opt-link-via-none = РЕЛЕ ВНЕ ДОСЯГАЕМОСТИ
anprc-fp-opt-link-via-value = { $name } { $bearing } { $distance }T
anprc-fp-opt-draw = ПОТРЕБЛЕНИЕ
anprc-fp-opt-draw-value = { $draw }/С
anprc-fp-opt-endurance = РЕСУРС
anprc-fp-opt-endurance-value = ~{ $minutes } МИН

## Экран OPT / ЗАКРЕПЛЁННАЯ
anprc-fp-staked-title = OPT  ЗАКРЕПЛЁННАЯ СТАНЦИЯ
anprc-fp-staked-needs-stake = ЗАКРЕПИТЕ СТАНЦИЮ ДЛЯ РЕТРАНСЛЯЦИИ И ПИКИРОВАНИЯ
anprc-fp-staked-retrans = РЕТРАНСЛЯЦИЯ
anprc-fp-staked-bridge = МОСТ
anprc-fp-staked-bridge-value = { $a } <> { $b }
anprc-fp-staked-break = РАЗОРВАТЬ МОСТ
anprc-fp-staked-side-a = СТОРОНА A
anprc-fp-staked-side-b = СТОРОНА B
anprc-fp-staked-make-bridge = МОСТ A <> B
anprc-fp-staked-retrans-note = ТРАФИК ОДНОЙ СТОРОНЫ ПОВТОРЯЕТСЯ НА ДРУГОЙ
anprc-fp-staked-antenna = АНТЕННА
anprc-fp-staked-peak = НАСТРОИТЬ АНТЕННУ
anprc-fp-staked-peaked = НАСТРОЕНА
anprc-fp-staked-unpeaked = НЕ НАСТРОЕНА
anprc-fp-staked-peak-note = +25% ДАЛЬНОСТИ ДО СБОРКИ СТАНЦИИ

## Список
anprc-fp-roster-title = OPT  СПИСОК
anprc-fp-roster-none = СПИСОК ПУСТ

## Подсказки ввода с клавиатуры
anprc-fp-entry-station = СТАНЦИЯ
anprc-fp-entry-label = МЕТКА
anprc-fp-entry-frequency = ЧАСТ [{ $slot }]

## Подтверждения на экране
anprc-fp-ack-mode = РЕЖИМ { $mode }
anprc-fp-ack-monitor = КОНТРОЛЬ { $state }
anprc-fp-ack-scan = СКАНИРОВАНИЕ { $state }
anprc-fp-ack-squelch = ШУМОДАВ { $level }
anprc-fp-ack-output = ВЫХОД { $power }
anprc-fp-ack-burst = ПАКЕТ { $state }
anprc-fp-ack-power-save = ЭНЕРГОСБЕРЕЖЕНИЕ { $state }
anprc-fp-ack-priority = КОНТРОЛЬ { $slot }
anprc-fp-ack-priority-off = ПРИОРИТЕТНЫЙ КОНТРОЛЬ ВЫКЛ
anprc-fp-ack-emcon-on = EMCON - СТАНЦИЯ МОЛЧИТ
anprc-fp-ack-emcon-off = EMCON СНЯТ
anprc-fp-ack-station = СТАНЦИЯ { $callsign }
anprc-fp-ack-station-cleared = СТАНЦИЯ СБРОШЕНА
anprc-fp-ack-radio-check = ПРОВЕРКА СВЯЗИ ОТПРАВЛЕНА
anprc-fp-ack-retrans-on = МОСТ ПОДНЯТ
anprc-fp-ack-retrans-off = МОСТ РАЗОРВАН
anprc-fp-ack-peak = НАСТРОЙКА - НЕ ДВИГАЙТЕСЬ

## Экраны PGM
anprc-fp-pgm-title = PGM  ПАМЯТЬ СЕТЕЙ
anprc-fp-pgm-load-std = ЗАГРУЗИТЬ СТАНД. СЕТИ
anprc-fp-pgm-no-nets = В ПАМЯТИ НЕТ СЕТЕЙ
anprc-fp-pgm-add = ДОБАВИТЬ СЕТЬ
anprc-fp-pgm-note-ent = ENT ОТКРЫВАЕТ ПАМЯТЬ ПОД КУРСОРОМ
anprc-fp-pgm-note-working = *  РАБОЧАЯ СЕТЬ. :r ПЕРЕДАЁТ НА НЕЙ
anprc-fp-memory-title = PGM  ПАМЯТЬ
anprc-fp-memory-loaded = ЗАГРУЖЕНО
anprc-fp-memory-work = СДЕЛАТЬ СЕТЬ РАБОЧЕЙ
anprc-fp-memory-active = АКТИВНА
anprc-fp-memory-tune = НАСТРОИТЬСЯ НА СЕТЬ
anprc-fp-memory-key-freq = ВВЕСТИ ЧАСТОТУ
anprc-fp-memory-empty = ОЧИСТИТЬ ПАМЯТЬ
anprc-fp-memory-delete = УДАЛИТЬ ПАМЯТЬ
anprc-fp-memory-rename = ПЕРЕИМЕНОВАТЬ
anprc-fp-netlist-title = PGM  СПИСОК СЕТЕЙ
anprc-fp-netlist-intercept = { $net } (ПЕР)
anprc-fp-netlist-none = НЕТ СЕТЕЙ - ВВЕДИТЕ ЧАСТОТУ
anprc-fp-ack-std-loading = ЗАГРУЗКА СТАНД. СЕТЕЙ
anprc-fp-ack-memory-added = ПАМЯТЬ { $label } ДОБАВЛЕНА
anprc-fp-ack-net-selected = СЕТЬ { $label } ВЫБРАНА
anprc-fp-ack-memory-emptied = ПАМЯТЬ ОЧИЩЕНА
anprc-fp-ack-memory-deleted = ПАМЯТЬ УДАЛЕНА
anprc-fp-ack-memory-renamed = ПАМЯТЬ { $label }
anprc-fp-ack-net-loaded = СЕТЬ ЗАГРУЖЕНА

## Экран SEC
anprc-fp-sec-title = SEC  COMSEC
anprc-fp-sec-secured = ЗАЩИЩЕНО
anprc-fp-sec-superseded = УСТАРЕЛО
anprc-fp-sec-unsecured = НЕ ЗАЩИЩЕНО
anprc-fp-sec-switch = ПЕРЕКЛЮЧАТЕЛЬ
anprc-fp-sec-switch-load = LD - ЗАГРУЗИТЬ КЛЮЧ
anprc-fp-sec-switch-zeroize = Z - ГОТОВНОСТЬ К СТИРАНИЮ
anprc-fp-sec-switch-operating = { $position } - РАБОТА
anprc-fp-sec-fill = КЛЮЧ
anprc-fp-sec-issued-to = ВЫДАН
anprc-fp-sec-note-superseded = УСТАРЕЛО - НУЖЕН НОВЫЙ КЛЮЧ
anprc-fp-sec-note-insert = ВСТАВЬТЕ КАРТУ КЛЮЧА ДЛЯ ЗАГРУЗКИ
anprc-fp-sec-traffic = ТРАФИК
anprc-fp-sec-encrypted = ЗАШИФРОВАН
anprc-fp-sec-in-clear = ОТКРЫТО
anprc-fp-sec-fill-actions = ДЕЙСТВИЯ С КЛЮЧОМ
anprc-fp-sec-zeroize = СТЕРЕТЬ
anprc-fp-sec-confirm-wipe = ПОДТВЕРДИТЬ СТИРАНИЕ
anprc-fp-sec-eject = ИЗВЛЕЧЬ
anprc-fp-sec-no-fill = НЕТ КЛЮЧА
anprc-fp-sec-destroy = УНИЧТОЖИТЬ
anprc-fp-sec-confirm-burn = ПОДТВЕРДИТЬ СЖИГАНИЕ
anprc-fp-sec-burn = СЖЕЧЬ
anprc-fp-sec-recrypto = ПЕРЕШИФРОВКА
anprc-fp-sec-confirm-recrypto = ПОДТВЕРДИТЬ ПЕРЕШИФРОВКУ
anprc-fp-sec-supersede = ЗАМЕНИТЬ
anprc-fp-sec-needs-fill = НУЖЕН КЛЮЧ
anprc-fp-sec-note-authority = ПЕРЕШИФРОВКА ТРЕБУЕТ РАЗРЕШЕНИЯ КОМАНДОВАНИЯ
anprc-fp-sec-note-supersedes = ОНА АННУЛИРУЕТ ВСЕ СТАРЫЕ КАРТЫ ФРАКЦИИ
anprc-fp-sec-otar = ПЕРЕЗАГРУЗКА КЛЮЧА ПО РАДИО
anprc-fp-sec-otar-send = ОТПРАВИТЬ КЛЮЧ ПО РАДИО
anprc-fp-sec-confirm-otar = ПОДТВЕРДИТЬ ОТПРАВКУ КЛЮЧА
anprc-fp-sec-otar-targets = { $count } СТАНЦИЙ УСТАРЕЛИ
anprc-fp-sec-note-otar = ШЛЁТ ТЕКУЩИЙ КЛЮЧ УСТАРЕВШИМ СТАНЦИЯМ В ДОСЯГАЕМОСТИ
anprc-fp-sec-note-otar-risk = ЗАХВАЧЕННЫЕ СТАНЦИИ С ВАШЕЙ КАРТОЙ ТОЖЕ ПОЛУЧАТ КЛЮЧ
anprc-fp-ack-zeroize-armed = СТИРАНИЕ ВЗВЕДЕНО - ENT ЕЩЁ РАЗ ДЛЯ ОЧИСТКИ
anprc-fp-ack-zeroized = КЛЮЧ СТЁРТ
anprc-fp-ack-destroy-armed = УНИЧТОЖЕНИЕ ВЗВЕДЕНО - ENT ЕЩЁ РАЗ ДЛЯ СЖИГАНИЯ
anprc-fp-ack-destroyed = КЛЮЧ УНИЧТОЖЕН
anprc-fp-ack-recrypto-armed = ПЕРЕШИФРОВКА ВЗВЕДЕНА - ENT ЕЩЁ РАЗ ДЛЯ ПРИКАЗА
anprc-fp-ack-recrypto = ПЕРЕШИФРОВКА ЗАКАЗАНА
anprc-fp-ack-otar-armed = ОТПРАВКА КЛЮЧА ВЗВЕДЕНА - ENT ЕЩЁ РАЗ ДЛЯ ОТПРАВКИ
anprc-fp-ack-otar = КЛЮЧ ОТПРАВЛЕН ПО РАДИО

## Экран SRCH
anprc-fp-srch-title = SRCH  ПРИЁМНИК
anprc-fp-srch-searching = ПОИСК
anprc-fp-srch-dwelling = УДЕРЖАНИЕ
anprc-fp-srch-idle = ПРОСТОЙ
anprc-fp-srch-start = НАЧАТЬ ПОИСК
anprc-fp-srch-stop = ОСТАНОВИТЬ ПОИСК
anprc-fp-srch-running = РАБОТАЕТ
anprc-fp-srch-ready = ГОТОВО
anprc-fp-srch-head = ГОЛОВКА
anprc-fp-srch-release = ОТПУСТИТЬ ГОЛОВКУ
anprc-fp-srch-state = СОСТОЯНИЕ
anprc-fp-srch-state-inhibited = СЕТИ СБРОШЕНЫ, ПЕРЕДАЧА ЗАПРЕЩЕНА
anprc-fp-srch-note-drops = ПОИСК СБРАСЫВАЕТ ВСЕ СЕТИ
anprc-fp-srch-note-deploy = НАДЕНЬТЕ ИЛИ ЗАКРЕПИТЕ СТАНЦИЮ ДЛЯ ПОИСКА ПО ДИАПАЗОНУ
anprc-fp-srch-contacts = КОНТАКТЫ
anprc-fp-srch-no-contacts = НЕТ КОНТАКТОВ
anprc-fp-srch-contact-partial = ЧАСТИЧНО { $tier }/{ $max }
anprc-fp-srch-contact-dwell = УДЕРЖ. { $tier }/{ $max }
anprc-fp-srch-contact-own = { $net } (СВОЯ)
anprc-fp-srch-contact-no-mem = { $net } (НЕТ ПАМ)
anprc-fp-srch-note-select-memory = ВЫБЕРИТЕ ПАМЯТЬ НА PGM, ЧТОБЫ НАСТРОИТЬ ФИКС
anprc-fp-srch-note-dwell = ENT НА ЧАСТИЧНОМ КОНТАКТЕ СТАВИТ ГОЛОВКУ НА НЕГО
anprc-fp-srch-jammer = ПЕЛЕНГ ГЛУШИЛКИ
anprc-fp-srch-jammer-clear = ГЛУШЕНИЕ НЕ СЛЫШНО
anprc-fp-srch-jammer-first = ПЕРВЫЙ ПЕЛЕНГ
anprc-fp-srch-jammer-baseline = БАЗА
anprc-fp-srch-jammer-baseline-value = { $moved }/{ $needed }T
anprc-fp-srch-jammer-take = ВЗЯТЬ ПЕЛЕНГ
anprc-fp-srch-jammer-second = ВЗЯТЬ ВТОРОЙ ПЕЛЕНГ
anprc-fp-srch-jammer-note = ДВА РАЗНЕСЁННЫХ ПЕЛЕНГА ДАЮТ ТОЧКУ НА КАРТЕ
anprc-fp-ack-search-running = ПОИСК РАБОТАЕТ
anprc-fp-ack-search-stopped = ПОИСК ОСТАНОВЛЕН
anprc-fp-srch-keys = АНАЛИЗ КЛЮЧА
anprc-fp-srch-keys-none = ЗАФИКСИРУЙТЕ ВРАЖЕСКУЮ СЕТЬ ДЛЯ АНАЛИЗА КЛЮЧА

## Экран KEY
anprc-fp-key-title = KEY  { $faction }
anprc-fp-key-state = КЛЮЧ
anprc-fp-key-broken = ВЗЛОМАН
anprc-fp-key-broken-note = ЭТА СТАНЦИЯ ЧИТАЕТ ИХ ТРАФИК ДО ПЕРЕШИФРОВКИ. ПЕРЕДАЧА НА ИХ СЕТЯХ ЗАПРЕЩЕНА
anprc-fp-key-depth = { $depth }/{ $max } В ЗАПАСЕ
anprc-fp-key-test = ПРОВЕРИТЬ ПРОБНЫЙ КЛЮЧ
anprc-fp-key-prompt = КЛЮЧ A-H x6
anprc-fp-key-rules = 6 СИМВОЛОВ A-H, БЕЗ ПОВТОРОВ. СОВПАДЕНИЯ = СИМВОЛЫ НА СВОЁМ МЕСТЕ
anprc-fp-key-no-depth = НЕТ ПРОБ В ЗАПАСЕ. КАЖДАЯ СТРОКА НА ФИКС. СЕТИ ДАЁТ ОДНУ
anprc-fp-key-trials = ПРОБЫ
anprc-fp-key-no-trials = ПРОБ ЕЩЁ НЕТ
anprc-fp-key-hits = { $hits }/{ $length }
anprc-fp-key-gone = НЕТ КЛЮЧА. СНАЧАЛА ЗАФИКСИРУЙТЕ ИХ СЕТЬ
anprc-fp-ack-key-sent = ПРОБА ОТПРАВЛЕНА
anprc-fp-ack-key-bad = 6 БУКВ A-H
anprc-fp-ack-dwell-on = ГОЛОВКА ЗАФИКСИРОВАНА
anprc-fp-ack-dwell-off = ГОЛОВКА СКАНИРУЕТ
anprc-fp-ack-contact-tuned = КОНТАКТ НАСТРОЕН
anprc-fp-ack-bearing = ПЕЛЕНГ ВЗЯТ

## Карточка клавиш
anprc-fp-help-title = ?  КАРТОЧКА КЛАВИШ
anprc-fp-help-off = СТАНЦИЯ ВЫКЛ
anprc-fp-help-online = В СЕТИ
anprc-fp-help-screen = РАБОТА С ЭКРАНОМ
anprc-fp-help-pre = +/- PRE
anprc-fp-help-pre-what = ДВИГАТЬ КУРСОР
anprc-fp-help-ent = ENT
anprc-fp-help-ent-what = ВЫБРАТЬ СТРОКУ ПОД НИМ
anprc-fp-help-clr = CLR
anprc-fp-help-clr-what = ВЫЙТИ НА ЭКРАН НАЗАД
anprc-fp-help-pg = +/- PG
anprc-fp-help-pg-what = СМЕНИТЬ СТРАНИЦУ
anprc-fp-help-soft-keys = МЯГКИЕ КЛАВИШИ ВНИЗУ ПЕРЕХОДЯТ НА СТРАНИЦЫ
anprc-fp-help-keypad = КЛАВИАТУРА
anprc-fp-help-key-1 = 1 CALL
anprc-fp-help-key-1-what = ПРОВЕРКА СВЯЗИ
anprc-fp-help-key-2 = 2 LT
anprc-fp-help-key-2-what = ПОДСВЕТКА
anprc-fp-help-key-3 = 3 MODE
anprc-fp-help-key-3-what = РЕЖИМ (ВОЛНА)
anprc-fp-help-key-4 = 4 SQL
anprc-fp-help-key-4-what = ШУМОДАВ
anprc-fp-help-key-5 = 5 ZERO
anprc-fp-help-key-5-what = COMSEC, СТИРАНИЕ ВЗВЕДЕНО
anprc-fp-help-key-6 = 6 PWR
anprc-fp-help-key-6-what = ВКЛ И ВЫКЛ СТАНЦИИ
anprc-fp-help-key-pages = 7 8 9 0
anprc-fp-help-key-pages-what = OPT PGM SEC LOG
anprc-fp-help-keying = ВВОД ЦИФР И ТЕКСТА
anprc-fp-help-keying-1 = ОТКРЫТИЕ ПОЛЯ АКТИВИРУЕТ КЛАВИАТУРУ.
anprc-fp-help-keying-2 = ЧАСТОТЫ В ФОРМАТЕ ___.___ МГЦ. СНАЧАЛА
anprc-fp-help-keying-3 = ВВОДИТЕ МГЦ. НЕИСПОЛЬЗОВАННЫЕ ЦИФРЫ = НОЛЬ.
anprc-fp-help-keying-4 = ТЕКСТ — БУКВЫ НАД КЛАВИШАМИ,
anprc-fp-help-keying-5 = НАЖМИТЕ ЕЩЁ РАЗ ДЛЯ A B C. ЗАГЛАВНЫЕ
anprc-fp-help-keying-6 = ПОКАЗЫВАЮТ ГРУППУ КЛАВИШИ ПРИ ВВОДЕ.
anprc-fp-help-keying-7 = 0 ВВОДИТ ПРОБЕЛ, ЗАТЕМ ДЕФИС.
anprc-fp-help-keying-8 = CLR УДАЛЯЕТ, ENT ПОДТВЕРЖДАЕТ.
anprc-fp-help-switch = ПЕРЕКЛЮЧАТЕЛЬ ФУНКЦИЙ
anprc-fp-help-switch-off = ВЫКЛ
anprc-fp-help-switch-off-what = СТАНЦИЯ ВЫКЛ
anprc-fp-help-switch-ct = CT
anprc-fp-help-switch-ct-what = ЗАЩИЩЕНО - СНОВА ЦИКЛ FH SC CT
anprc-fp-help-switch-pt = PT
anprc-fp-help-switch-pt-what = ОТКРЫТО - ЛЮБОЙ МОЖЕТ ЧИТАТЬ
anprc-fp-help-switch-ld = LD
anprc-fp-help-switch-ld-what = СТРАНИЦА КЛЮЧА - ПРУЖИНА
anprc-fp-help-switch-z = Z
anprc-fp-help-switch-z-what = ВЗВОДИТ СТИРАНИЕ - ПРУЖИНА
anprc-fp-help-switch-1 = LD И Z — ПРУЖИННЫЕ ПОЛОЖЕНИЯ. ПЕРЕКЛЮЧАТЕЛЬ
anprc-fp-help-switch-2 = ВОЗВРАЩАЕТСЯ САМ. РЕЖИМ ОСТАЁТСЯ
anprc-fp-help-switch-3 = ПРЕЖНИМ.
anprc-fp-help-techniques = ПРИЁМЫ (ТОЛЬКО НА ПАНЕЛИ)
anprc-fp-help-techniques-1 = OPT: ПАКЕТ НЕ ДАЁТ ПЕЛЕНГА, ЭНЕРГО-
anprc-fp-help-techniques-2 = СБЕРЕЖЕНИЕ ТЯНЕТ БАТАРЕЮ, ПРИОРИТЕТНЫЙ
anprc-fp-help-techniques-3 = КОНТРОЛЬ СЛУШАЕТ 2-Ю СЕТЬ, EMCON МОЛЧИТ.
anprc-fp-help-techniques-4 = OPT > ЗАКРЕПЛЁННАЯ: РЕТРАНСЛЯЦИЯ МОСТИТ ДВЕ
anprc-fp-help-techniques-5 = ПАМЯТИ, ПИКИРОВАНИЕ +25% ДАЛЬНОСТИ.
anprc-fp-help-techniques-6 = SEC: ОТПРАВЬТЕ КЛЮЧ ПО РАДИО ПОСЛЕ ПЕРЕШИФРОВКИ.
anprc-fp-help-techniques-7 = SRCH: ENT НА ЧАСТИЧНОМ КОНТАКТЕ УДЕРЖИВАЕТ,
anprc-fp-help-techniques-8 = ДВА ПЕЛЕНГА ГЛУШИЛКИ ДАЮТ ТОЧКУ НА КАРТЕ.
anprc-fp-help-procedure = ПОРЯДОК РАБОТЫ
anprc-fp-help-procedure-1 = ПАНЕЛЬ РАБОТАЕТ НА ЛЮБОЙ ВКЛЮЧЁННОЙ СТАНЦИИ,
anprc-fp-help-procedure-2 = В РУКЕ ИЛИ НА ЗЕМЛЕ. НАДЕНЬТЕ ИЛИ
anprc-fp-help-procedure-3 = ЗАКРЕПИТЕ ЕЁ, ЧТОБЫ РАБОТАТЬ В СЕТИ.
anprc-fp-help-procedure-4 = ДОБАВЬТЕ ПАМЯТЬ НА PGM, НАСТРОЙТЕ, ВЫБЕРИТЕ.
anprc-fp-help-procedure-5 = ИСПОЛЬЗУЙТЕ :r НА РАБОЧЕЙ ПАМЯТИ ДЛЯ ПЕРЕДАЧИ.
anprc-fp-help-procedure-6 = БЕЗ КЛЮЧА ВАШ ТРАФИК ИДЁТ ОТКРЫТО.

## Экран LOG
anprc-fp-log-title = LOG  ЖУРНАЛ СЕТЕЙ
anprc-fp-log-filter = ФИЛЬТР СЕТЕЙ
anprc-fp-log-all-nets = ВСЕ СЕТИ
anprc-fp-log-intercepts-only = ТОЛЬКО ПЕРЕХВАТЫ
anprc-fp-log-print = ПЕЧАТЬ ЖУРНАЛА
anprc-fp-log-print-intercepts = ПЕЧАТЬ ПЕРЕХВАТОВ
anprc-fp-log-paper = БУМАГА
anprc-fp-log-traffic = ТРАФИК
anprc-fp-log-empty = ЖУРНАЛ ПУСТ
anprc-fp-log-intercept = { $net } ПЕР
anprc-fp-log-no-match = НЕТ ТРАФИКА ПОД ФИЛЬТР
anprc-fp-ack-log-printed = ЖУРНАЛ НАПЕЧАТАН
anprc-fp-ack-intercepts-printed = ПЕРЕХВАТЫ НАПЕЧАТАНЫ

## Строка состояния сверху
anprc-fp-lamp-rx = RX
anprc-fp-lamp-tx = TX
anprc-fp-rd-sec = SEC
anprc-fp-rd-unsec = UNSEC
anprc-fp-rd-clear = ОТКР
anprc-fp-rd-sig = СИГ { $bars }
anprc-fp-rd-bat = БАТ { $bars }
anprc-fp-rd-no-net-loaded = СЕТЬ НЕ ЗАГРУЖЕНА
anprc-fp-rd-no-net-memory = НЕТ СЕТИ В ПАМЯТИ
anprc-fp-rd-slot-empty = СЛОТ ПУСТ
anprc-fp-rd-search-slot = SR
anprc-fp-rd-search = ПОИСК ПО ДИАПАЗОНУ - СЕТИ СБРОШЕНЫ
anprc-fp-rd-dwell = УДЕРЖАНИЕ КОНТАКТА - СЕТИ СБРОШЕНЫ
anprc-fp-rd-search-band = ПОИСК
anprc-fp-rd-direct = ПРЯМАЯ ЧАСТОТА
anprc-fp-rd-band-sat = { $band } SAT
anprc-fp-rd-band-los = { $band } LOS
anprc-fp-rd-settings = { $mode }  SQL { $squelch }  PWR { $power }
anprc-fp-rd-flag-mon = MON
anprc-fp-rd-flag-scan = SCAN
anprc-fp-rd-flag-burst = BURST
anprc-fp-rd-flag-ps = PS
anprc-fp-rd-flag-pri = PRI
anprc-fp-rd-flag-emcon = EMCON
anprc-fp-rd-flag-rxmt = RXMT
anprc-fp-rd-stn = STN { $station }
anprc-fp-rd-bit = BIT { $result }
anprc-fp-rd-bit-no-cell = NO CELL
anprc-fp-rd-bit-search = SRCH
anprc-fp-rd-bit-pass = PASS

## Гравировки, мягкие клавиши и строка состояния панели
anprc-fp-eng-function = ФУНКЦИЯ
anprc-fp-eng-sprung = LD / Z ПРУЖИНА
anprc-fp-tab-pgm = PGM
anprc-fp-tab-pgm-key = КЛАВИША 8
anprc-fp-tab-sec = SEC
anprc-fp-tab-sec-key = КЛАВИША 9
anprc-fp-tab-srch = SRCH
anprc-fp-tab-srch-key = ДИАПАЗОН
anprc-fp-tab-log = LOG
anprc-fp-tab-log-key = КЛАВИША 0
anprc-fp-tab-opt = OPT
anprc-fp-tab-opt-key = КЛАВИША 7
anprc-fp-tab-help-key = КЛАВИШИ
anprc-fp-no-data = НЕТ ДАННЫХ ОТ СТАНЦИИ
anprc-fp-worn = НАДЕТА
anprc-fp-status = { $deployment }  { $power }  { $net }
anprc-fp-status-direct = ПРЯМАЯ { $freq }
anprc-fp-status-no-net = нет сети

## Строка подсказки
anprc-fp-prompt-ld-held = LD УДЕРЖИВАЕТСЯ - СТРАНИЦА КЛЮЧА, ПЕРЕКЛЮЧАТЕЛЬ ВОЗВРАЩАЕТСЯ
anprc-fp-prompt-z-armed = Z УДЕРЖИВАЕТСЯ - СТИРАНИЕ ВЗВЕДЕНО, ПЕРЕКЛЮЧАТЕЛЬ ВОЗВРАЩАЕТСЯ
anprc-fp-prompt-z-nothing = Z УДЕРЖИВАЕТСЯ - НЕЧЕГО СТИРАТЬ
anprc-fp-prompt-no-cell = НЕТ ЭЛЕМЕНТА - ВСТАВЬТЕ БАТАРЕЮ
anprc-fp-prompt-off = СТАНЦИЯ ВЫКЛ - ПЕРЕКЛЮЧАТЕЛЬ ИЛИ 6 PWR
anprc-fp-prompt-stowed = УБРАН - НАСТРОЙКИ В ПОРЯДКЕ, НАДЕНЬТЕ ДЛЯ РАБОТЫ В СЕТИ
anprc-fp-prompt-no-net = НЕТ СЕТИ - 8 PGM, ЧТОБЫ ЗАГРУЗИТЬ

## Подтверждения клавиатуры и переключателя
anprc-fp-ack-backlight = ПОДСВЕТКА { $level }
anprc-fp-ack-entry-abandoned = ВВОД ОТМЕНЁН
anprc-fp-ack-three-digits = ВВЕДИТЕ ХОТЯ БЫ 3 ЦИФРЫ
anprc-fp-ack-check-stowed = СТАНЦИЯ УБРАНА - НАДЕНЬТЕ ИЛИ ЗАКРЕПИТЕ ДЛЯ ПРОВЕРКИ
anprc-fp-ack-check-searching = ИДЁТ ПОИСК - ПЕРЕДАЧА ЗАПРЕЩЕНА
anprc-fp-ack-check-no-net = НЕТ СЕТИ ДЛЯ ПРОВЕРКИ
anprc-fp-ack-nothing-to-wipe = НЕЧЕГО СТИРАТЬ
anprc-fp-ack-no-cell = НЕТ ЭЛЕМЕНТА - PWR ОТКАЗАНО
anprc-fp-ack-set-on = СТАНЦИЯ ВКЛ
anprc-fp-ack-set-off = СТАНЦИЯ ВЫКЛ
anprc-fp-ack-mode-plain = РЕЖИМ PLAIN - ТРАФИК ОТКРЫТО
anprc-fp-ack-set-on-comsec = СТАНЦИЯ ВКЛ - COMSEC ПОДНЯТ, ПЕРЕКЛЮЧАТЕЛЬ ВОЗВРАЩАЕТСЯ
anprc-fp-ack-ld-comsec = LD - COMSEC ПОДНЯТ, ПЕРЕКЛЮЧАТЕЛЬ ВОЗВРАЩАЕТСЯ
anprc-fp-ack-z-armed = Z - СТИРАНИЕ ВЗВЕДЕНО, ПОДТВЕРДИТЕ ENT. ПЕРЕКЛЮЧАТЕЛЬ ВОЗВРАЩАЕТСЯ
anprc-fp-ack-z-nothing = Z - НЕЧЕГО СТИРАТЬ
anprc-fp-ack-sprung-back = ПЕРЕКЛЮЧАТЕЛЬ ВЕРНУЛСЯ В { $position } - РЕЖИМ НЕ ИЗМЕНЁН

## Положения переключателя функций
anprc-fp-knob-off = ВЫКЛ
anprc-fp-knob-ct = CT
anprc-fp-knob-pt = PT
anprc-fp-knob-ld = LD
anprc-fp-knob-z = Z

## Подсветка
anprc-fp-backlight-day = ДЕНЬ
anprc-fp-backlight-night = НОЧЬ

## Панель разъёмов
anprc-fp-port-ant = ANT
anprc-fp-port-audio = AUDIO
anprc-fp-port-fill = KDU/FILL
anprc-fp-port-power = PWR/GPS

## Гравировки клавиатуры
anprc-fp-key-clr = CLR
anprc-fp-key-ent = ENT
anprc-fp-key-fn-scrn = SCRN
anprc-fp-key-fn-curs = CURS
anprc-fp-key-fn-back = BACK
anprc-fp-key-fn-work = WORK
anprc-fp-key-fn-call = CALL
anprc-fp-key-fn-lt = LT
anprc-fp-key-fn-mode = MODE
anprc-fp-key-fn-sql = SQL
anprc-fp-key-fn-zero = ZERO
anprc-fp-key-fn-pwr = PWR
anprc-fp-key-fn-opt = OPT
anprc-fp-key-fn-pgm = PGM
anprc-fp-key-fn-sec = SEC
anprc-fp-key-fn-log = LOG
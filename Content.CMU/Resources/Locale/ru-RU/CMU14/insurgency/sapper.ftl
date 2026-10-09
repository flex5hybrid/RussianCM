# Роль сапёра КОФ.
au14-job-name-clfsapper = Сапёр КОФ
au14-job-description-clfsapper = Партизан, обученный полевой подготовке и подрывному делу. Устанавливайте и маскируйте ловушки, обращая саму территорию колонии против правительственных сил.
# Префикс рации НЕ должен выдавать сапёра: все скрытые роли КОФ отображаются в связи как обычные гражданские
# (тот же префикс "CIV", что и у au14-job-prefix-civiliancolonist / партизана КОФ).
au14-job-prefix-clfsapper = CIV

# Сообщения об установке / обезвреживании ловушек сапёра.
insfor-sapper-trap-deployed = Вы устанавливаете заряд, скрывая его от посторонних глаз.
insfor-sapper-trap-disarmed = Вы перерезаете растяжку и убираете заряд.
insfor-sapper-trap-deploy-container = Здесь это установить нельзя.
insfor-sapper-trap-deploy-occupied = На этом тайле уже установлена ловушка.
insfor-sapper-trap-unskilled = Вы возитесь с устройством, но понятия не имеете, как его установить.

# Составная растяжка.
insfor-sapper-tripwire-attached = Вы прикрепляете взрывчатку к заряду растяжки.
insfor-sapper-tripwire-full = Здесь больше нельзя закрепить взрывчатку.
insfor-sapper-tripwire-need-explosive = Прежде чем устанавливать растяжку, к ней необходимо прикрепить взрывчатку.
insfor-sapper-tripwire-place-other-end = Вы устанавливаете заряд. Теперь протяните провод туда, где хотите закрепить его второй конец — не далее { $range } тайлов, по прямой и в пределах видимости — и используйте его там.
insfor-sapper-tripwire-strung = Растяжка натянута и взведена.
insfor-sapper-tripwire-charge-gone = Заряда, к которому был протянут этот провод, больше нет.
insfor-sapper-tripwire-bad-spot = Здесь нельзя протянуть растяжку.
insfor-sapper-tripwire-too-close = Вы стоите прямо на заряде.
insfor-sapper-tripwire-not-straight = Провод должен идти от заряда по прямой.
insfor-sapper-tripwire-too-far = Это слишком далеко от заряда.
insfor-sapper-tripwire-no-los = Между этой точкой и зарядом нет прямой видимости.
insfor-sapper-tripwire-eject-verb = Снять взрывчатку
insfor-sapper-tripwire-ejected = Вы снимаете взрывчатку с заряда.

# Звуковая ловушка раннего предупреждения.
insfor-sapper-audio-name-title = Звуковая ловушка
insfor-sapper-audio-name-prompt = Назовите эту ловушку
insfor-sapper-audio-default-name = Безымянная
insfor-sapper-audio-location-unknown = местоположение неизвестно
insfor-sapper-audio-radio-alert = Сработала звуковая ловушка «{$name}». Местоположение: {$location}.

# Верстак сапёра.
insfor-sapper-workbench-deployed = Вы раскладываете верстак и фиксируете его ножки.
insfor-sapper-workbench-need-materials = На верстаке не хватает материалов или отдельных компонентов (положите необходимые предметы на верстак или рядом с ним).
insfor-sapper-workbench-crafted = Вы изготавливаете {$item}.

# Чип автоспуска «Switch».
au14-switch-on = Вы переключаете тумблер. Ударно-спусковой механизм перестаёт соблюдать ограничения.
au14-switch-off = Вы возвращаете тумблер в исходное положение.
au14-switch-jammed = Механизм заклинивает — оружие заело!
au14-switch-exploded = Оружие разрывает на части прямо у вас в руках!
au14-switch-jammed-shoot = Оружие заклинило! Сначала передёрните затвор.
au14-switch-rack-verb = Передёрнуть затвор (устранить задержку)
au14-switch-rack-fail = Вы передёргиваете затвор, но гильза остаётся зажатой внутри.
au14-switch-rack-success = Искорёженная гильза вылетает наружу. Оружие снова может стрелять.

# Оружейные работы на верстаке (принудительная установка/снятие модулей).
insfor-sapper-workbench-weapon-placed = Вы кладёте оружие на верстак.
insfor-sapper-workbench-weapon-occupied = На верстаке уже лежит оружие.
insfor-sapper-workbench-no-weapon = Сначала положите оружие на верстак.
insfor-sapper-workbench-slots-full = Все слоты этого оружия уже заняты.
insfor-sapper-workbench-attached = Вы силой устанавливаете модуль на место ({$slot}).
insfor-sapper-workbench-wrong-slot = Этот модуль не подходит ни к одному слоту данного оружия.
insfor-sapper-workbench-hold-attachment = Сначала возьмите модуль в руку.
insfor-sapper-workbench-take-weapon = Забрать оружие
insfor-sapper-workbench-detach = Снять: {$name}

# Взлом банкоматов.
insfor-sapper-atm-already-hacked = Этот банкомат уже полностью обчистили.
insfor-sapper-atm-hacked = Банкомат содрогается и выплёвывает наличные на сумму {$amount}.
insfor-sapper-atm-malfunction = ОШИБКА: УСТРОЙСТВО НЕИСПРАВНО. ОБРАТИТЕСЬ К АДМИНИСТРАТОРУ.
insfor-sapper-console-drained = Средства с консоли выводятся наружу — выпадает {$amount} наличными.
insfor-sapper-asrs-drained = Средства со счёта АСХВ оказываются у вас в руках — {$amount} наличными.
insfor-sapper-asrs-empty = На этом терминале нет средств для вывода.

# Сеть шпионских камер.
device-frequency-prototype-name-surveillance-camera-clf = Шпионские камеры КОФ

# Петлевая ловушка.
insfor-sapper-snare-caught = Петля резко затягивается вокруг вас, связывая руки и переворачивая вас вверх ногами!
insfor-sapper-snare-struggled-free = Вы с усилием освобождаетесь из петли.
insfor-sapper-snare-cutting = Вы начинаете разрезать петлю, освобождая попавшего в неё.
insfor-sapper-snare-cut-free = Петля перерезана, и вы падаете на землю.
insfor-sapper-atm-message-title = Устройство для выкачивания средств
insfor-sapper-atm-message-prompt = Оставьте сообщение на экране банкомата до завершения его самовосстановления (необязательно, до {$max} символов)

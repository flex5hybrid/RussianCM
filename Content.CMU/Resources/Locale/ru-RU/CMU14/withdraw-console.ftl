# Всплывающие сообщения
withdraw-console-locked = Консоль заблокирована. Проведите двумя авторизованными ID-картами, чтобы разблокировать её.
withdraw-console-id-unbound = У этой ID-карты нет зарегистрированного владельца.
withdraw-console-id-wrong-faction = Доступ запрещён. Эта ID-карта не принадлежит авторизованному члену фракции.
withdraw-console-id-already-swiped = Удостоверение этого человека уже было принято.
withdraw-console-id-accepted = ID-карта принята ({ $count }/2). Проведите ещё одной авторизованной ID-картой.
withdraw-console-unlocked = Консоль разблокирована. Протокол вывода сил теперь доступен.
withdraw-console-dropdown-blocked = Вывод сил уже идёт: спуски десантных челноков заблокированы. Вы всё ещё можете вернуться на корабль.
withdraw-console-faction-not-allowed = Вывод сил этой фракции в текущей операции не разрешён.

# Оповещение в середине вывода сил (срабатывает по истечении половины общей продолжительности)
withdraw-console-announcement = УВЕДОМЛЕНИЕ О ВЫВОДЕ СИЛ { $faction }: Начат официальный вывод сил. Вывод будет завершён через { $minutes } минут. Всему персоналу подготовиться к эвакуации.

# Этап блокировки захвата (~1/3 времени до завершения)
withdraw-console-announcement-hijack-lock = ВЫВОД СИЛ { $faction }: осталось { $minutes } минут.

# Этап блокировки спуска (~1/6 времени до завершения)
withdraw-console-announcement-dropdown-lock = ВЫВОД СИЛ { $faction }: осталось { $minutes } минут. Все спуски на поверхность теперь заблокированы.

# Подписи интерфейса
withdraw-console-ui-status-locked = Консоль заблокирована. Проведите двумя авторизованными ID-картами.
withdraw-console-ui-status-ready = Консоль разблокирована. Протокол вывода сил доступен.
withdraw-console-ui-status-active = ВЫВОД СИЛ В ПРОЦЕССЕ
withdraw-console-ui-round-ended = Операция завершена.
withdraw-console-ui-unlocked = Консоль разблокирована (проверено ID-карт: 2/2)
withdraw-console-ui-swipe-count = Проверено ID-карт: { $count }/2
withdraw-console-ui-timer = Осталось { $minutes } мин { $seconds } с
withdraw-console-ui-stalemate-toggle = Объявить патовую ситуацию
withdraw-console-ui-stalemate-cancel = Отменить патовую ситуацию

# Сообщения об окончании раунда
withdraw-console-round-end-withdrawn = { $faction } официально вывела свои силы из операции. Столкновение завершается незначительным поражением для { $faction }.
withdraw-console-round-end-stalemate = Обе фракции согласились признать патовую ситуацию. Столкновение завершается ничьей.

# Для колонии
withdraw-console-colony-evac-enabled = Вывод сил колонии подтверждён. Системы заправки спасательных шлюпок активированы. Эвакуация будет разрешена на отметке в 10 минут.

withdraw-console-gamemode-disabled = Withdrawal is not possible when responding to a distress signal.

withdraw-console-ui-title = Withdraw Console

withdraw-console-ui-initiate = Initiate Withdraw

withdraw-console-ui-cancel = Cancel Withdrawal

withdraw-console-ui-confirm = CONFIRM

withdraw-console-ui-crt-heading = WITHDRAWAL CONTROL

withdraw-console-ui-crt-heading-faction = WITHDRAWAL CONTROL // { $faction }

withdraw-console-ui-crt-subheading = OPERATION TERMINATION PROTOCOL // DUAL AUTHORIZATION

withdraw-console-ui-crt-auth = COMMAND AUTHORIZATION

withdraw-console-ui-crt-timer = TIME TO WITHDRAWAL

withdraw-console-ui-crt-stalemate-proposed = STALEMATE PROPOSED

withdraw-console-ui-crt-status-locked = LOCKED

withdraw-console-ui-crt-status-ready = ARMED

withdraw-console-ui-crt-status-active = WITHDRAWING

withdraw-console-ui-crt-status-concluded = CONCLUDED

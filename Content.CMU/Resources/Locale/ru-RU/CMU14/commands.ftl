cmd-mghost-desc = Это делает тебя призраком-наставником.

cmd-mghost-help = хозяин

cmu-cmd-clfcomms-desc = Переключает AU14 Система связи через CLFСети INSFOR. Выключение означает, что каналы сотовой связи работают как стоковое радио: нет требований к покрытию, нет статики, нет позывных.

cmu-cmd-clfcomms-help = Использование: clfcomms [on|off]Без аргументов сообщает нынешнее состояние.

cmu-cmd-clfcomms-status-on = CLF Включаю. Сети ячейки привязаны к якорю и работают под полной системой связи.

cmu-cmd-clfcomms-status-off = CLF Коммс: Офф. Сети ячейки работают как стоковое радио.

cmu-cmd-clfcomms-master-off = Примечание: главный переключатель (au14.new comms system) выключен, поэтому сейчас это ничего не делает.

cmu-cmd-clfcomms-invalid-state = Не мог прочитать{ $value }Включай или нет.

cmu-cmd-clfcomms-already = CLF Телефоны уже есть { $state }.

cmu-cmd-clfcomms-state-on = включён

cmu-cmd-clfcomms-state-off = выключен

cmu-cmd-clfcomms-server = Сервер

cmu-cmd-clfcomms-admin-on = { $user } Включите систему обратно для CLFИНСФОР.

cmu-cmd-clfcomms-admin-off = { $user } Выключил систему связи для CLF/INSFOR - их сети теперь стоковое радио.

cmu-cmd-clfcomms-result-on = CLF Включи связь. Ячейка вернулась в соответствии с правилами покрытия, статической и позывной.

cmu-cmd-clfcomms-result-off = CLF Отключите связь. Сети ячейки теперь доходят куда угодно, разоблачены и не зажаты.

cmu-cmd-clfcomms-hint = выключать

cmu-cmd-savebuild-desc = Сохраните созданные игроком объекты в коробке вокруг вас в общий файл.

cmu-cmd-savebuild-help = строить <name> [radius 0-5]

cmu-cmd-savebuild-player-only = Эта команда может управляться только игроком.

cmu-cmd-savebuild-radius-number = Радиус должен быть числом.

cmu-cmd-buildsave-desc = Откройте панель выбора сохранения сборки.

cmu-cmd-buildsave-help = спасать

cmu-cmd-digup-desc = Выкопайте один z-уровень, всплыв на вашем текущем горизонтальном положении.

cmu-cmd-digup-help = au digup

cmu-cmd-digdown-desc = Копайте прямо вниз, создавая / спускаясь в каменный уровень Z под вами.

cmu-cmd-digdown-help = au digdown

cmu-cmd-zdig-player-only = Эта команда должна управляться игроком в игре.

cmu-cmd-digup-success = Поднимите уровень.

cmu-cmd-digup-failed = Не удалось выкопать здесь (ничего выше, стена блокирует пятно выше, или функция отключена).

cmu-cmd-digdown-success = Понижай уровень.

cmu-cmd-digdown-failed = Не могу копаться здесь (карта выключена, функция отключена или уровень авторизации уже ниже).

cmu-cmd-multiz-desc = Список карт с их AU14 Статус Multi Z-Level (вертикальное здание) или переключать его на карту / глобально.

cmu-cmd-multiz-help = au multiz (список) <mapId> <on|off>  Оригинальное название: Au multiz Global <on|off>

cmu-cmd-multiz-enabled = Включен

cmu-cmd-multiz-disabled = несоответствующий

cmu-cmd-multiz-yes = Да

cmu-cmd-multiz-no = Нет

cmu-cmd-multiz-global-list = Глобально AU14 z-строительство: { $state }  (перенаправлено с «Au multiz global on |off»)

cmu-cmd-multiz-map-list =   Карта { $mapId } { $map } - Multi Z-Level: { $enabled }

cmu-cmd-multiz-usage = Использование: au multiz <mapId|global> <on|off>

cmu-cmd-multiz-invalid-state = Второй аргумент должен быть «включен» или «выключен».

cmu-cmd-multiz-global-changed = Глобально AU14 z-строительство сейчас { $state }.

cmu-cmd-multiz-invalid-map = Аргумент карты должен быть числовым MapId (запустите «au multiz», чтобы перечислить их) или «глобальным».

cmu-cmd-multiz-map-missing = Нет карты с MapId { $mapId }.

cmu-cmd-multiz-map-changed = Карта { $mapId } Multi Z-Level будет { $enabled }Игроки { $permission } строить AU14 z-уровень лестницы / этажи здесь.

cmu-cmd-multiz-can-build = теперь может

cmu-cmd-multiz-cannot-build = больше не может

cmd-znetwork-weather-desc = Устанавливает погоду для всех карт в zNetwork.

cmd-znetwork-weather-help = znetwork-weather <zNetwork entity> <weather|null> [duration seconds]

cmu-cmd-znetwork-weather-entity-missing = Невозможность найти сущность { $entity }.

cmu-cmd-znetwork-weather-component-missing = Целевая организация не имеет компонента CMUZLevelsNetwork: { $entity }.

cmu-cmd-znetwork-weather-network-hint = zNetwork net entity

cmu-cmd-znetwork-weather-duration-hint = Продолжительность в секундах

cmu-cmd-toolperm-desc = Предоставить, отозвать или перечислить разрешения редактора для каждого инструмента ckey.

cmu-cmd-toolperm-help = Использование: toolperm add <ckey> <tool> Удаление инструмента <ckey> <tool> Список инструментов
    Инструменты: { $tools }

cmu-cmd-toolperm-no-grants = Никаких грантов на инструменты.

cmu-cmd-toolperm-unknown-tool = Неизвестный инструмент{ $tool }Инструменты: { $tools }

cmu-cmd-toolperm-granted = Предоставлено{ $tool }- { $ckey }.

cmu-cmd-toolperm-revoked = Отозвано.{ $tool }от { $ckey }.

cmu-cmd-toolperm-no-change = Ничего не изменилось.

cmu-cmd-nuke-decals-desc = Удалите наклейки из каждой загруженной сетки.

cmu-cmd-nuke-decals-help = Обсуждение Nuke: Decals [all (true/false, default: false)] [decalId...] - Удаляет наклейки с каждой загруженной сетки.
    По умолчанию это будет удалять только чистые наклейки (например, кровь / грязь и т. Д.), Чтобы сэкономить детали карты.
    Чтобы удалить все наклейки (включая детали, размещенные картографом), передайте «правду» в качестве первого аргумента.

cmu-cmd-nuke-decals-scope-all = все декады

cmu-cmd-nuke-decals-scope-cleanable = только чистый

cmu-cmd-nuke-decals-summary = Удалён { $removed } наклейки{ $scope }) из { $grids } Решетки.

cmu-cmd-nuke-decals-summary-filtered = Удалён { $removed } совпадающие декали { $filterCount } ids{ $scope }) из { $grids } Решетки.

cmu-cmd-nuke-decals-skipped = [nuke:decals] { $count } Соответствующие наклейки были найдены, но пропущены, потому что они отключили дефолт Cleanable (чистый уборщик).

cmu-cmd-nuke-decals-retry = Чтобы удалить их, запустите команду снова, начиная с «истинно» (nuke:decals true). { $ids }').

cmu-cmd-nuke-decals-hint-first = [all (default: false)] или [decalId]

cmu-cmd-nuke-decals-hint-id = [decalId...]

cmu-cmd-nuke-lights-desc = Удалите огни в радиусе вокруг вас; используйте «Ядерная помощь: огни» для получения дополнительной информации.

cmu-cmd-nuke-lights-help = Оригинальное название: Nuke: Lights [radius=80] [energy=80] [duration=4] [x y mapId] [color=Orange]

cmu-cmd-nuke-lights-positive = Радиус, энергия и продолжительность должны быть больше нуля.

cmu-cmd-nuke-lights-no-attached = Никакого присоединения. Предоставьте x y mapId в явном виде.

cmu-cmd-nuke-lights-color-error = Не удалось разобрать цвет{ $color }Используйте имя Orange или шестнадцатеричное значение #ff8a00.

cmu-cmd-nuke-lights-map-missing = Карта { $mapId } не существует.

cmu-cmd-nuke-lights-spawned = Распыленный ядерный свет { $uid } в { $position } на карте { $mapId } для { $duration }С.

cmu-cmd-nuke-lights-parse-value = Не удалось разобрать { $name } '{ $value }'.

cmu-cmd-nuke-lights-parse-coordinates = Не удалось определить координаты{ $x }' '{ $y }'.

cmu-cmd-nuke-lights-parse-map-id = Не удалось разобрать ID карты{ $mapId }'.

cmu-cmd-nuke-lights-hint-radius = радиус

cmu-cmd-nuke-lights-hint-energy = энергия

cmu-cmd-nuke-lights-hint-duration = продолжительность

cmu-cmd-nuke-lights-hint-x = х

cmu-cmd-nuke-lights-hint-y = y

cmu-cmd-nuke-lights-hint-map-id = карта

cmu-cmd-nuke-lights-hint-color = цвет

cmd-cmu-expedition-desc = Создавайте и проверяйте пользовательские карты экспедиций Govfor.

cmd-cmu-expedition-visit-desc = Введите готовую экспедицию в зону посадки для осмотра.

cmd-cmu-expedition-visit-help = экспедиционный визит <map ID> [connected username]

cmu-expedition-visit-no-player = Требуется подключенный игрок. С серверной консоли включите их имя пользователя.

cmu-expedition-visited = {$player} находится в зоне высадки на карте экспедиции {$map}.

cmd-cmu-expedition-help = сценарии cmu-экспедиции, сценарии cmu-экспедиции <scenario ID> <seed>Cmu-экспедиция генерирует <Woodland|Swamp|Tundra|Beach|Mountain|SwampJungle|BurnedWoodland> <seed> [RiverValley|LakeCountry|Ridgeline|Wetlands|Coast|Archipelago|Caldera|Fjord|Delta|Highlands] [CrashRecovery|SurveyCamp|BrokenConvoy|LostRelay]КМУ-экспедиция <status|open|briefing> <map ID>или время экспедиций <map ID> час <0..23.99>

cmu-expedition-invalid-scenario = Неизвестная экспедиционная история или пропавший сюжетный реквизит. Используйте сценарии cmu-expedition для перечня предустановок.

cmu-expedition-no-scenario = Это экспедиция только по местности. Создайте именованный сценарий, чтобы включить историю восстановления.

cmu-expedition-objective-location = Последнее известное местоположение груза: {$x}, {$y}.

cmu-expedition-sector-map-name = {$sector} - Экспедиция {$seed}

cmu-expedition-sector-lz-name = {$sector} - LZ {$seed}

cmu-expedition-invalid-profile = Недействительный профиль экспедиции, палитра, размер или вариант. Проверьте прототипы сервера.

cmu-expedition-no-layout = Это семя не могло поместиться на соединенных участках и безопасной зоне посадки. Попробуйте другое семя или форму земли.

cmu-expedition-busy = Уже создается экспедиция. Подожди, пока все закончится.

cmu-expedition-limit = Уже существуют три карты экспедиций. Удалите неиспользованную карту экспедиции, прежде чем создавать другую.

cmu-expedition-failed = Поколение экспедиции провалилось. Смотрите журнал сервера; неполная карта будет удалена.

cmu-expedition-started = Создание карты экспедиции {$map} семя семя {$seed}Его Govfor LZ откроется автоматически при завершении генерации.

cmu-expedition-not-map = Эта карта не является созданной экспедицией.

cmu-expedition-not-ready = Экспедиция продолжается. Зона посадки недоступна.

cmu-expedition-ready = Экспедиция готова. Командные миссии автоматически публикуют Govfor LZ.

cmu-expedition-opened = Зона посадки экспедиции Говфор открыта.

cmu-expedition-details = семя {$seed}: {$biome} / {$landform} / {$story}Центр посадки: {$x}, {$y}.

cmu-expedition-map-name = экспедиция {$seed}

cmu-expedition-lz-name = экспедиция {$seed} - Зона посадки

ent-CMUExpeditionBoulder1 = выветрившийся валун
    .desc = Местный камень носился гладко под ветром и водой.

ent-CMUExpeditionBoulder2 = выветрившийся валун
    .desc = Местный камень носился гладко под ветром и водой.

ent-CMUExpeditionBoulder3 = выветрившийся валун
    .desc = Местный камень носился гладко под ветром и водой.

ent-CMUExpeditionSupply = Заброшенное оборудование
    .desc = Поврежденный случай, оставленный экспедицией.

ent-CMUExpeditionRelay = бесшумная полевая эстафета
    .desc = Заброшенная эстафета на краю обследованной территории.

ent-CMUExpeditionMiner = Солнечный экспериментальный майнер
    .desc = Компактная пробная установка для извлечения в усиленной транспортной раме. Его спаренный регулятор калибровки запечатан рядом с корпусом резца. Машина выключена.

cmu-expedition-airspace = экспедиция {$seed} - высотный уровень {$level}

cmu-expedition-time-set = Время освещения экспедиции обновляется на каждом уровне.

cmu-expedition-announcement = Приоритетное черное заявление. Восстановление экспериментального шахтера в {$sector}LZ {$lz} Открыт. GOVFORРазвернуть немедленно.

cmu-expedition-fly = Начать с {$sector}

cmd-cmu-expedition-orders-desc = Установите движение отряда экспедиции, работу охраны, тактику и правила фракции.

cmd-cmu-expedition-orders-help = приказы об экспедициях <map ID|here> <squad ID> <move|assault|guard|patrol-add> [x y, omit with here] [guard only: auto|north|east|south|west]; или патрульный запуск |patrol-stop |patrol-clear; или стиль <Steady|Aggressive|Cautious>; или дружественная цель <comma-separated NPC factions|default>

cmu-expedition-unknown-faction = Неизвестная фракция NPC: {$faction}.

cmu-expedition-orders-applied = Приказы, применяемые к {$count} Стражи экспедиции.

cmu-expedition-ai-squad = Отряд {$squad} созданный. Используйте cmu-expedition-приказы, чтобы управлять им.

cmu-cmd-znetwork-entity-hint = zNetwork net entity

cmu-cmd-znetwork-entity-missing = Невозможность найти сущность { $entity }.

cmu-cmd-znetwork-component-missing = Целевая организация не имеет компонента CMUZLevelsNetwork: { $entity }.

cmd-znetwork-delete-desc = Удалить все карты в выбранной zNetwork и самой zNetwork.

cmd-znetwork-delete-help = znetwork-delete <zNetwork entity>

cmu-cmd-znetwork-delete-success = ZNetwork и все его карты удалены.

cmd-znetwork-combine-desc = Соединяет несколько карт в общую сеть z-уровня. Если какая-либо карта уже является частью сети z-уровня, она не работает.

cmd-znetwork-combine-help = znetwork-combine <mapId> <mapId> [...]

cmu-cmd-znetwork-combine-map-hint = Карта ID в порядке от земли до неба

cmu-cmd-znetwork-combine-not-enough = Недостаточно карт для формирования сети уровней.

cmu-cmd-znetwork-combine-parse-map = Не могу разобрать{ $value }В карточку.

cmu-cmd-znetwork-combine-nullspace = Nullspace не может быть добавлен в ZNetwork.

cmu-cmd-znetwork-combine-map-missing = Карта { $mapId } не существует.

cmu-cmd-znetwork-combine-duplicate = Дублированная карта: { $mapId }.

cmu-cmd-znetwork-combine-name = Комбинированная сеть zNetwork: { $id }

cmu-cmd-znetwork-combine-success = Создана сеть Z-уровня! Компания Z-Network: { $network }

cmu-cmd-znetwork-combine-partial-failure = Создана сеть z-level { $network }Но что-то пошло не так!

cmd-znetwork-mapping-desc = Загружает существующий прототип игровой карты в виде zNetwork для картографирования.

cmd-znetwork-mapping-help = картирование сети <gameMapPrototype>

cmu-cmd-znetwork-mapping-hint = GameMapPrototype с поддержкой CMU Набор карт уровня Z

cmu-cmd-znetwork-mapping-unknown-prototype = Неизвестный прототип GameMap { $prototype }.

cmu-cmd-znetwork-mapping-network-name = Картографирование zNetwork: { $map }

cmu-cmd-znetwork-mapping-cleanup = Выгрузили все карты, созданные неудавшейся загрузкой zNetwork.

cmu-cmd-znetwork-mapping-default-load-failed = Не удалось загрузить карту zNetwork по умолчанию: { $path }!

cmu-cmd-znetwork-mapping-map-name = картографирование { $map }

cmu-cmd-znetwork-mapping-map-depth-name = картографирование { $map } [{ $depth }]

cmu-cmd-znetwork-mapping-depth-load-failed = Не удалось загрузить карту zNetwork (глубина) { $depth }): { $path }!

cmu-cmd-znetwork-mapping-map-disappeared = По какой-то причине после загрузки карты не существует! MapId: { $mapId }

cmu-cmd-znetwork-mapping-network-failed = Не удалось создать zNetwork с загруженных карт!

cmd-znetwork-initialize-desc = Инициализирует все карты в zNetwork. Это относится не ко всем компонентам прототипа карты.

cmd-znetwork-initialize-help = znetwork-инициализировать <zNetwork entity>

cmu-cmd-znetwork-initialize-map-component-missing = Структура карты { $map } У него нет компонента карты.

cmu-cmd-znetwork-initialize-map-missing = Карта с ID { $mapId } не существует.

cmu-cmd-znetwork-initialize-already = Карта с ID { $mapId } Они уже инициализированы.

cmu-cmd-znetwork-initialize-success = Карта с ID { $mapId } Он был инициализирован.

cmd-znetwork-variantize-desc = Применяет случайные вариации плитки на всех картах в zNetwork.

cmd-znetwork-variantize-help = znetwork-вариантизация <zNetwork entity>

cmu-cmd-znetwork-variantize-grid-missing = Сущность{ $entity }Не существует или не является сеткой.

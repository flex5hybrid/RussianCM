# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 wray-git
# SPDX-License-Identifier: AGPL-3.0-only
## In-game construction menu editor (world right-click > Construction)

verb-categories-construction = Строительство

construction-category-au14-custom = Пользовательское

construction-menu-verb-add = Добавить в меню строительства
construction-menu-verb-add-message = Навсегда добавить этот предмет в меню строительства (применится после следующего перезапуска).
construction-menu-verb-remove = Удалить из меню строительства
construction-menu-verb-remove-message = Удалить этот предмет из меню строительства (применится после следующего перезапуска).
construction-menu-verb-change-recipe = Изменить рецепт
construction-menu-verb-change-recipe-message = Изменить список появления, категорию или рецепт этого пункта меню (применится после следующего перезапуска).
construction-menu-verb-change-recipe-disabled = Этого предмета нет в меню строительства. Сначала добавьте его.

## Add / Change dialogs

construction-menu-dialog-add-title = Добавить { $item } в меню строительства
construction-menu-dialog-change-title = Изменить рецепт - { $item }
construction-menu-dialog-spawnlist = Список появления (по умолчанию: { $default })
construction-menu-dialog-category = Категория (по умолчанию: { $default })
construction-menu-dialog-recipe = Рецепт, например { $example }  (Материал:Количество, шаги разделяются >, инструменты: weld/wrench/screw/pry/cut)
construction-menu-dialog-spawnlist-current = Список появления (текущий: { $current })
construction-menu-dialog-category-current = Категория (текущая: { $current })
construction-menu-dialog-recipe-current = Рецепт (текущий: { $current })

## Result popups

construction-menu-verb-added = { $item } добавлен в "{ $category }". Рецепт: { $recipe }. Применится после следующего перезапуска.
construction-menu-verb-recipe-changed = { $item } обновлен. Рецепт: { $recipe }. Применится после следующего перезапуска.
construction-menu-verb-removed = { $item } удален из меню строительства. Применится после следующего перезапуска.

## Editor window

construction-editor-title = Редактор меню строительства
construction-editor-title-add = Добавить в меню строительства
construction-editor-title-edit = Изменить рецепт
construction-editor-spawnlist = Список появления
construction-editor-category = Категория
construction-editor-new-spawnlist = Название нового списка...
construction-editor-new-category = Название новой категории...
construction-editor-add-new = Добавить новый...
construction-editor-confirm = Подтвердить
construction-editor-material-custom = Другое...
construction-editor-material-notfound = Материал "{ $material }" не найден - выберите существующий.
construction-editor-steps = Шаги рецепта
construction-editor-material = ID пользовательского стака (например Steel)
construction-editor-amount = Кол-во
construction-editor-doafter = Сек.
construction-editor-add-material = + Материал
construction-editor-add-tool = + Инструмент
construction-editor-remove-step = Удалить последний
construction-editor-clear-steps = Очистить
construction-editor-ok = Сохранить (след. перезапуск)
construction-editor-cancel = Отмена
construction-editor-health = Прочность
construction-editor-health-placeholder = пусто = наследовать
construction-editor-danger = Опасная зона - массовое удаление
construction-editor-remove-include-all = Включить ВСЕ сущности в этом списке/категории
construction-editor-remove-group = Удалить список/категорию
construction-editor-remove-confirm = Подтвердить удаление
construction-editor-remove-need-check = Для подтверждения этого разрушительного действия сначала отметьте "Включить все сущности".
construction-editor-remove-warning = ВНИМАНИЕ: навсегда удаляет ВСЕ рецепты в { $spawnlist } / { $category }. Подождите 3 секунды...
construction-editor-remove-ready = Готово - нажмите "Подтвердить", чтобы навсегда удалить все рецепты в { $spawnlist } / { $category }.
construction-menu-group-removed = Удалено рецептов: { $count } из { $spawnlist } / { $category }. Применится после следующего перезапуска.
construction-editor-step-material = { $amount } x { $material }  ({ $sec }с)
construction-editor-step-tool = Инструмент: { $tool }  ({ $sec }с)

## Deconstruction steps (structures only)

construction-editor-deconstruct-steps = Шаги разборки (только структуры, по умолчанию: лом)
construction-editor-add-deconstruct-tool = + Инструмент
construction-editor-pick-deconstruct-entity-tool = + Пользовательский инструмент...
construction-editor-remove-deconstruct-step = Удалить последний
construction-editor-clear-deconstruct-steps = Очистить

construction-menu-verb-add-failed = Не удалось добавить предмет в меню строительства.
construction-menu-verb-remove-failed = Не удалось удалить предмет из меню строительства.
construction-menu-verb-bad-recipe = Не удалось разобрать рецепт. Пример: "Steel:4 > weld > Steel:2".

construction-menu-verb-invalid = Нельзя сохранить рецепт: { $reason }
construction-menu-invalid-no-steps = рецепту нужен хотя бы один шаг с материалом.
construction-menu-invalid-tool = шаги с инструментами ("{ $tool }") пока не поддерживаются - используйте только шаги с материалами. (Путь строительства не может проверять инструменты без падений.)
construction-menu-invalid-tool-item = шаги с инструментами ("{ $tool }") не поддерживаются для предметов в руке - они работают только для структур. Уберите шаг с инструментом или выберите структуру.
construction-menu-invalid-material = материал "{ $material }" нельзя использовать для строительства. Используйте CM-материал (например CMSteel, CMPlasteel, CMGlass, CMGlassReinforced, RMCWood, RMCPlastic).
construction-menu-invalid-entity = сущности "{ $entity }" не существует. Выберите реальный прототип из списка.
construction-menu-invalid-deconstruct-material = шаги разборки могут быть только инструментами (например лом, сварка) - нельзя подавать материалы, чтобы что-то разобрать. Уберите шаг с материалом.

## Custom material/tool selector + editor additions

construction-editor-pick-entity-material = + Пользовательский материал...
construction-editor-pick-entity-tool = + Пользовательский инструмент (не расходуется)...
construction-editor-step-entity-material = { $amount } x { $entity }  ({ $sec }с)
construction-editor-step-entity-tool = Инструмент (остается): { $entity }  ({ $sec }с)
construction-selector-title = Выбор сущности
construction-selector-search = Поиск сущностей...
construction-selector-select = Выбрать

## Utilities -> Admin Tools

gmod-construction-menu-admin-tools = Инструменты администратора
gmod-construction-menu-items-editor = Редактор предметов строительства
gmod-construction-menu-tiles-editor = Редактор тайлов
gmod-construction-menu-lathe-editor = Редактор станков
gmod-construction-menu-zlevel-toggles = Переключатели Z-уровней
construction-menu-editor-not-admin = Вы не администратор - редактор не откроется.

## Utilities -> INSFOR

gmod-construction-menu-insfor = INSFOR
gmod-construction-menu-insfor-editor = Редактор INSFOR
gmod-construction-menu-insfor-custom-editor = Пользовательский редактор INSFOR

## In-menu detail panel: Change Recipe / Remove Item (admins; works for vanilla recipes too)

gmod-construction-menu-change-recipe = Изменить рецепт
gmod-construction-menu-remove-item = Удалить предмет
construction-menu-recipe-hidden = "{ $recipe }" удален из меню строительства. Полностью применится после следующего перезапуска.
construction-menu-recipe-already-hidden = "{ $recipe }" уже удален из меню строительства.
construction-menu-recipe-hide-failed = Не удалось удалить этот рецепт из меню строительства.

## Recipe chooser (entity already has recipes)

construction-chooser-title = Рецепты этого предмета
construction-chooser-entry = { $spawnlist } / { $category }
construction-chooser-change = Изменить
construction-chooser-remove = Удалить
construction-chooser-add-new = Добавить новый рецепт
construction-menu-verb-no-resources = Нельзя редактировать меню строительства: не найдена доступная для записи папка Resources.

## Tiles editor

construction-tile-editor-title = Добавить тайл в меню строительства
construction-tile-editor-tile = Тайл
construction-tile-editor-search = Поиск тайлов...
construction-tile-editor-main-category = Главная категория
construction-tile-editor-page-zlevel = Z-уровень (экспериментально)
construction-tile-editor-page-spawnlists = Списки появления
construction-tile-editor-spawnlist = Список появления (только страница списков)
construction-tile-editor-category = Категория
construction-tile-editor-material = Материал
construction-tile-editor-amount = Стоимость (листы)
construction-tile-editor-selected = Выбранный тайл: { $tile }
construction-tile-editor-none = (тайл не выбран)
construction-tile-editor-save = Сохранить (след. перезапуск)
construction-tile-editor-cancel = Отмена
construction-menu-tile-invalid-tile = Тайл "{ $tile }" недействителен. Выберите его из списка.
construction-menu-tile-added = Тайл { $tile } добавлен в "{ $category }". Применится после следующего перезапуска.

## Lathe editor

construction-lathe-editor-title = Добавить рецепт станка
construction-lathe-editor-lathe = Станок
construction-lathe-editor-autolathe = Автолат
construction-lathe-editor-armylathe = Армейский лат
construction-lathe-editor-pick-item = Выберите предмет для печати...
construction-lathe-editor-selected = Предмет: { $item }
construction-lathe-editor-none = (предмет не выбран)
construction-lathe-editor-steel = Стоимость в стали
construction-lathe-editor-glass = Стоимость в стекле
construction-lathe-editor-plastic = Стоимость в пластике
construction-lathe-editor-time = Время печати (с)
construction-lathe-editor-save = Сохранить (след. перезапуск)
construction-lathe-editor-cancel = Отмена
construction-menu-lathe-invalid-cost = Укажите стоимость хотя бы одного материала (сталь / стекло / пластик).
construction-menu-lathe-added = { $item } добавлен в { $lathe }. Применится после следующего перезапуска.
construction-menu-lathe-removed = Рецепт станка { $recipe } удален. Применится после следующего перезапуска.
construction-lathe-editor-existing = Уже добавленные рецепты (нажмите, чтобы удалить)
construction-lathe-editor-remove = Удалить

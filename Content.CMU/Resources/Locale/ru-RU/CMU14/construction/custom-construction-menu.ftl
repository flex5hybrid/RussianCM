# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 wray-git
# SPDX-License-Identifier: AGPL-3.0-only
## Внутриигровой редактор меню строительства (ПКМ по миру > Строительство)

verb-categories-construction = Строительство

construction-category-au14-custom = Пользовательское

construction-menu-verb-add = Добавить в меню строительства
construction-menu-verb-add-message = Навсегда добавить этот предмет в меню строительства (применится после следующего перезапуска).
construction-menu-verb-remove = Удалить из меню строительства
construction-menu-verb-remove-message = Удалить этот предмет из меню строительства (применится после следующего перезапуска).
construction-menu-verb-change-recipe = Изменить рецепт
construction-menu-verb-change-recipe-message = Изменить список спавна, категорию или рецепт для этого пункта меню (применится после следующего перезапуска).
construction-menu-verb-change-recipe-disabled = Этого предмета нет в меню строительства. Сначала добавьте его.

## Диалоги добавления / изменения

construction-menu-dialog-add-title = Добавить { $item } в меню строительства
construction-menu-dialog-change-title = Изменить рецепт — { $item }
construction-menu-dialog-spawnlist = Список спавна (по умолчанию: { $default })
construction-menu-dialog-category = Категория (по умолчанию: { $default })
construction-menu-dialog-recipe = Рецепт, например { $example }  (Материал:Количество, разделяйте этапы через >, инструменты: weld/wrench/screw/pry/cut)
construction-menu-dialog-spawnlist-current = Список спавна (текущий: { $current })
construction-menu-dialog-category-current = Категория (текущая: { $current })
construction-menu-dialog-recipe-current = Рецепт (текущий: { $current })

## Всплывающие сообщения о результате

construction-menu-verb-added = { $item } добавлен в «{ $category }». Рецепт: { $recipe }. Применится после следующего перезапуска.
construction-menu-verb-recipe-changed = { $item } обновлён. Рецепт: { $recipe }. Применится после следующего перезапуска.
construction-menu-verb-removed = { $item } удалён из меню строительства. Применится после следующего перезапуска.

## Окно редактора

construction-editor-title = Редактор меню строительства
construction-editor-title-add = Добавить в меню строительства
construction-editor-title-edit = Изменить рецепт
construction-editor-spawnlist = Список спавна
construction-editor-category = Категория
construction-editor-new-spawnlist = Название нового списка спавна…
construction-editor-new-category = Название новой категории…
construction-editor-add-new = Добавить новое…
construction-editor-confirm = Подтвердить
construction-editor-material-custom = Пользовательский…
construction-editor-material-notfound = Материал «{ $material }» не найден — выберите существующий.
construction-editor-steps = Этапы рецепта
construction-editor-material = Пользовательский ID стака (например, Steel)
construction-editor-amount = Кол-во
construction-editor-doafter = Сек
construction-editor-add-material = + Материал
construction-editor-add-tool = + Инструмент
construction-editor-remove-step = Удалить последний
construction-editor-clear-steps = Очистить
construction-editor-ok = Сохранить (после перезапуска)
construction-editor-cancel = Отмена
construction-editor-health = Прочность
construction-editor-health-placeholder = пусто = наследовать
construction-editor-danger = Опасная зона — массовое удаление
construction-editor-remove-include-all = Включить ВСЕ сущности из этого списка спавна/категории
construction-editor-remove-group = Удалить список спавна/категорию
construction-editor-remove-confirm = Подтвердить удаление
construction-editor-remove-need-check = Сначала отметьте «Включить все сущности», чтобы подтвердить это необратимое действие.
construction-editor-remove-warning = ВНИМАНИЕ: навсегда удаляет КАЖДЫЙ рецепт в { $spawnlist } / { $category }. Подождите 3 секунды...
construction-editor-remove-ready = Готово — нажмите «Подтвердить», чтобы навсегда удалить все рецепты в { $spawnlist } / { $category }.
construction-menu-group-removed = Удалено рецептов: { $count } из { $spawnlist } / { $category }. Применится после следующего перезапуска.
construction-editor-step-material = { $amount } x { $material }  ({ $sec } с)
construction-editor-step-tool = Инструмент: { $tool }  ({ $sec } с)

## Этапы разборки (только конструкции)

construction-editor-deconstruct-steps = Этапы разборки (конструкции, по умолчанию: лом)
construction-editor-add-deconstruct-tool = + Инструмент
construction-editor-pick-deconstruct-entity-tool = + Пользовательский инструмент…
construction-editor-remove-deconstruct-step = Удалить последний
construction-editor-clear-deconstruct-steps = Очистить

construction-menu-verb-add-failed = Не удалось добавить предмет в меню строительства.
construction-menu-verb-remove-failed = Не удалось удалить предмет из меню строительства.
construction-menu-verb-bad-recipe = Не удалось разобрать рецепт. Используйте, например: "Steel:4 > weld > Steel:2".

construction-menu-verb-invalid = Невозможно сохранить рецепт: { $reason }
construction-menu-invalid-no-steps = рецепт должен содержать хотя бы один этап с материалом.
construction-menu-invalid-tool = этапы с инструментами («{ $tool }») пока не поддерживаются — используйте только этапы с материалами. (Система строительства не может требовать инструменты без сбоя.)
construction-menu-invalid-tool-item = этапы с инструментами («{ $tool }») не поддерживаются для предметов, создаваемых в руках — они работают только с конструкциями. Удалите этап с инструментом или выберите конструкцию.
construction-menu-invalid-material = материал «{ $material }» нельзя использовать для строительства. Используйте материал CM (например, CMSteel, CMPlasteel, CMGlass, CMGlassReinforced, RMCWood, RMCPlastic).
construction-menu-invalid-entity = сущность «{ $entity }» не существует. Выберите существующий прототип из списка.
construction-menu-invalid-deconstruct-material = этапы разборки могут содержать только инструменты (например, лом или сварочный аппарат) — материалы нельзя использовать для разборки. Удалите этап с материалом.

## Выбор пользовательских материалов/инструментов + дополнения редактора

construction-editor-pick-entity-material = + Пользовательский материал…
construction-editor-pick-entity-tool = + Пользовательский инструмент (не расходуется)…
construction-editor-step-entity-material = { $amount } x { $entity }  ({ $sec } с)
construction-editor-step-entity-tool = Инструмент (сохраняется): { $entity }  ({ $sec } с)
construction-selector-title = Выберите сущность
construction-selector-search = Поиск сущностей…
construction-selector-select = Выбрать

## Утилиты → Инструменты администратора

gmod-construction-menu-admin-tools = Инструменты администратора
gmod-construction-menu-items-editor = Редактор предметов строительства
gmod-construction-menu-tiles-editor = Редактор тайлов
gmod-construction-menu-lathe-editor = Редактор станков
gmod-construction-menu-zlevel-toggles = Переключатели Z-уровней
gmod-construction-menu-spawnlist-delete = Удалить список спавна
construction-menu-editor-not-admin = Вы не администратор — редактор не откроется.

## Инструмент удаления списка спавна (Инструменты администратора → Удалить список спавна)

construction-spawnlist-delete-title = Удаление списка спавна
construction-spawnlist-delete-pick = Список спавна для удаления (с количеством рецептов):
construction-spawnlist-delete-option = { $spawnlist } ({ $count } рецептов)
construction-spawnlist-delete-none = Нет списков спавна со сгенерированными рецептами.
construction-spawnlist-delete-arm = Удалить…
construction-spawnlist-delete-confirm = ПОДТВЕРДИТЬ УДАЛЕНИЕ
construction-spawnlist-delete-warning = Это удалит ВСЕ сгенерированные рецепты в «{ $spawnlist }». Подтверждение станет доступно через несколько секунд…
construction-spawnlist-delete-ready = Готово — подтверждение удалит «{ $spawnlist }» и все его рецепты.
construction-menu-spawnlist-deleted = Список спавна «{ $spawnlist }» удалён (удалено рецептов: { $count }).
construction-spawnlist-delete-pick-category = Область удаления (одна категория или весь список спавна):
construction-spawnlist-delete-category-all = Весь список спавна (все категории)
construction-spawnlist-delete-category-option = { $category } ({ $count } рецептов)
construction-spawnlist-delete-category-warning = Это удалит ВСЕ сгенерированные рецепты категории «{ $category }» из «{ $spawnlist }». Подтверждение станет доступно через несколько секунд…
construction-spawnlist-delete-category-ready = Готово — подтверждение удалит категорию «{ $category }» из «{ $spawnlist }» и все её рецепты.
construction-menu-spawnlist-category-deleted = Категория «{ $category }» списка спавна «{ $spawnlist }» удалена (удалено рецептов: { $count }).

## Предпросмотр сохранения в БД (подтверждение пользователем перед записью в файлы/строки базы данных)

construction-db-preview-title = Подтверждение сохранения — ожидающие изменения
construction-db-preview-summary = { $kind }: запланировано записей: { $planned }, отклонено: { $rejected }. Проверьте изменения и подтвердите.
construction-db-preview-confirm = Подтвердить сохранение
construction-db-preview-cancel = Отмена
construction-db-preview-kind-entry = Запись строительства
construction-db-preview-kind-mass = Массовые сущности
construction-db-preview-kind-mass-tiles = Массовые тайлы

## Утилиты → INSFOR

gmod-construction-menu-insfor = INSFOR
gmod-construction-menu-insfor-editor = Редактор INSFOR
gmod-construction-menu-insfor-custom-editor = Пользовательский редактор INSFOR

## Панель сведений в меню: изменение рецепта / удаление предмета (для администраторов; работает и с обычными рецептами)

gmod-construction-menu-change-recipe = Изменить рецепт
gmod-construction-menu-remove-item = Удалить предмет
construction-menu-recipe-hidden = Рецепт «{ $recipe }» удалён из меню строительства. Полностью применится после следующего перезапуска.
construction-menu-recipe-already-hidden = Рецепт «{ $recipe }» уже удалён из меню строительства.
construction-menu-recipe-hide-failed = Не удалось удалить этот рецепт из меню строительства.

## Выбор рецепта (если у сущности уже есть рецепты)

construction-chooser-title = Рецепты этого предмета
construction-chooser-entry = { $spawnlist } / { $category }
construction-chooser-change = Изменить
construction-chooser-remove = Удалить
construction-chooser-add-new = Добавить новый рецепт
construction-menu-verb-no-resources = Невозможно изменить меню строительства: доступный для записи каталог Resources не найден.

## Редактор тайлов

construction-tile-editor-title = Добавить тайл в меню строительства
construction-tile-editor-tile = Тайл
construction-tile-editor-search = Поиск тайлов...
construction-tile-editor-main-category = Основная категория
construction-tile-editor-page-zlevel = Z-уровень (экспериментально)
construction-tile-editor-page-spawnlists = Списки спавна
construction-tile-editor-spawnlist = Список спавна (только страница списков спавна)
construction-tile-editor-category = Категория
construction-tile-editor-material = Материал
construction-tile-editor-amount = Стоимость (листов)
construction-tile-editor-selected = Выбранный тайл: { $tile }
construction-tile-editor-none = (тайл не выбран)
construction-tile-editor-save = Сохранить (после перезапуска)
construction-tile-editor-cancel = Отмена
construction-menu-tile-invalid-tile = «{ $tile }» не является допустимым тайлом. Выберите тайл из списка.
construction-menu-tile-added = Тайл { $tile } добавлен в «{ $category }». Применится после следующего перезапуска.

## Редактор станков

construction-lathe-editor-title = Добавить рецепт станка
construction-lathe-editor-lathe = Станок
construction-lathe-editor-autolathe = Автолат
construction-lathe-editor-armylathe = Военный станок
construction-lathe-editor-pick-item = Выберите предмет для печати...
construction-lathe-editor-selected = Предмет: { $item }
construction-lathe-editor-none = (предмет не выбран)
construction-lathe-editor-steel = Стоимость в стали
construction-lathe-editor-glass = Стоимость в стекле
construction-lathe-editor-plastic = Стоимость в пластике
construction-lathe-editor-time = Время печати (с)
construction-lathe-editor-save = Сохранить (после перезапуска)
construction-lathe-editor-cancel = Отмена
construction-menu-lathe-invalid-cost = Укажите стоимость хотя бы одного материала (сталь / стекло / пластик).
construction-menu-lathe-added = { $item } добавлен в { $lathe }. Применится после следующего перезапуска.
construction-menu-lathe-removed = Рецепт станка { $recipe } удалён. Применится после следующего перезапуска.
construction-lathe-editor-existing = Существующие добавленные рецепты (нажмите для удаления)
construction-lathe-editor-remove = Удалить

# Массовый редактор сущностей (Инструменты администратора): пакетное добавление множества сущностей в один рецепт
gmod-construction-menu-mass-editor = Массовый редактор сущностей
construction-mass-selector-title = Массовый редактор сущностей — выбор сущностей
construction-mass-selector-parent-search = Поиск родительских прототипов...
construction-mass-selector-parent-all = Все родители
construction-mass-selector-select-all = Выбрать все показанные
construction-mass-selector-clear = Очистить
construction-mass-selector-confirm = Продолжить
construction-mass-selector-count = Выбрано: {$count}
construction-menu-mass-item-name = Выбрано сущностей: {$count}
construction-menu-mass-none = Ни одна из выбранных сущностей не подходит.
construction-menu-mass-added = Добавлено предметов: {$added} в {$category} ({$recipe}).
construction-menu-mass-partial = Добавлено предметов: {$added}, ошибок: {$failed} ({$reason}).

# Массовый редактор сущностей — режим тайлов
construction-mass-selector-tiles = Тайлы
construction-mass-tiles-title = Массовый рецепт тайлов ({$count} тайлов)
construction-menu-mass-tiles-added = Добавлено тайлов: {$added} в {$category}.

# Списки Z-синхронизации (Инструменты администратора): какие стены отражаются между Z-уровнями в качестве границ карты
gmod-construction-menu-zsync-lists = Списки Z-синхронизации
au-zsync-title = Списки Z-синхронизации — отражение границ между уровнями
au-zsync-browser-header = Все сущности (выберите, затем добавьте в список)
au-zsync-lists-header = Текущие списки
au-zsync-whitelist = Белый список (отражаются между Z-уровнями)
au-zsync-blacklist = Чёрный список (никогда не отражаются, имеет приоритет над белым списком)
au-zsync-add-whitelist = Добавить в белый список
au-zsync-add-blacklist = Добавить в чёрный список
au-zsync-pick-whitelist = Выбрать сущность → белый список
au-zsync-pick-blacklist = Выбрать сущность → чёрный список
au-zsync-remove-selected = Удалить выбранные
au-zsync-changed = Z-синхронизация {$list} обновлена (изменено: {$count}).
au-zsync-picked = {$proto} добавлен в список Z-синхронизации {$list}.
au-zsync-pick-instruction = Нажмите на сущность в текущем раунде, чтобы добавить её прототип в выбранный список Z-синхронизации. ПКМ для отмены.
au-zsync-pick-no-entity = Под курсором нет сущности.
au-zsync-pick-cancelled = Выбор сущности для Z-синхронизации отменён.
au-zsync-scope-button = Область: {$scope}
au-zsync-scope-global = Глобально (все карты)
au-zsync-scope-maps = Карт: {$count}
au-zsync-scope-header = Изменить область
au-zsync-scope-global-button = Глобально
au-zsync-scope-info = Редактируется Z-синхронизация для: {$scope}
au-zsync-move-to-whitelist = Переместить выбранные в белый список
au-zsync-move-to-blacklist = Переместить выбранные в чёрный список
au-zsync-conflict-title = Уже находится в противоположном списке
au-zsync-conflict-text = Эти сущности уже находятся в списке {$list}. «Подтвердить» переместит их, «Игнорировать» добавит только остальные.
au-zsync-confirm = Подтвердить
au-zsync-ignore = Игнорировать

# Разрешения инструментов (Инструменты администратора): выдаваемый только хостом доступ к редакторам по ckey
gmod-construction-menu-tool-permissions = Разрешения инструментов
au14-toolperm-title = Разрешения инструментов — доступ к редакторам по ckey
au14-toolperm-grant-header = Выдать доступ к инструменту (ckey работает, даже если игрок не в сети)
au14-toolperm-ckey-placeholder = ckey...
au14-toolperm-grant = Выдать
au14-toolperm-users-header = Пользователи с разрешениями (нажмите на ckey, чтобы раскрыть)
au14-toolperm-none = Разрешения на инструменты пока никому не выданы.
au14-toolperm-remove = Удалить
au14-toolperm-tool-construction = Редактор предметов строительства
au14-toolperm-tool-mass = Массовый редактор сущностей
au14-toolperm-tool-tiles = Редактор тайлов
au14-toolperm-tool-lathe = Редактор станков
au14-toolperm-tool-zleveltoggles = Переключатели Z-уровней
au14-toolperm-tool-zsync = Списки Z-синхронизации
au14-toolperm-tool-insfor = Редактор INSFOR
au14-toolperm-tool-spawnlistdelete = Удаление списка спавна

au14-toolperm-user-header = { $ckey } ({ $count })

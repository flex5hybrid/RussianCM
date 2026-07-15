# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 wray-git
# SPDX-License-Identifier: AGPL-3.0-only
# Examine line shown on any entity a player constructed.
construction-player-built-examine = Построил: [color=cyan]{ $name }[/color].

# Build-partner verbs (right-click another player).
build-partner-add-verb = Добавить в партнеры строительства
build-partner-remove-verb = Убрать партнера строительства
build-partner-added = { $name } теперь может включать ваши постройки в свои сохранения.
build-partner-removed = { $name } больше не может включать ваши постройки в свои сохранения.

# Saving builds.
saved-build-success = Постройка "{ $name }" сохранена ({ $count } сущностей).
saved-build-error-no-name = Сначала укажите название постройки.
saved-build-error-empty = В выделении нет ничего, что построили вы или ваш партнер.
saved-build-error-serialize = Не удалось сериализовать эту постройку.
saved-build-error-write = Не удалось записать файл постройки.

# Build-save selection panel (client).
saved-build-window-title = Сохранить постройку
saved-build-window-range = Радиус
saved-build-window-size = Выделение: { $size }x{ $size } тайлов
saved-build-window-append = Добавить радиус
saved-build-window-clear = Очистить
saved-build-window-selected = Подсвечено: { $count }
saved-build-window-name = Название постройки...
saved-build-window-save = Сохранить постройку
saved-build-window-open-folder = Открыть папку сохраненных построек

# Saved Builds spawnlist in the construction menu.
gmod-construction-menu-saved-builds = Сохраненные постройки
saved-build-card = { $name }  ({ $author } · { $count })
saved-build-detail-desc = Автор: { $author }
    { $count } сущностей · { $source }
saved-build-none = Сохраненных построек пока нет. Используйте инструмент build-save, чтобы создать первую.
saved-build-place-button = Разместить постройку
saved-build-placed = Постройка размещена ({ $count } частей).
saved-build-error-load = Не удалось загрузить эту постройку.
saved-build-error-nogrid = Постройку можно размещать только на гриде.
saved-build-error-noorigin = Исходное место этой постройки больше не существует.
saved-build-error-notadmin = Только администраторы могут размещать постройку мгновенно. Постройте ее через строительные призраки.
saved-build-place-original-button = Разместить на исходном месте
saved-build-ghosts-placed = Размещено строительных призраков: { $count } - достройте их материалами.

# Saved-build management (delete + open folder).
gmod-construction-menu-delete-build = Удалить постройку
gmod-construction-menu-open-build-folder = Открыть папку построек
saved-build-deleted = Эта сохраненная постройка удалена.
saved-build-error-delete = Не удалось удалить эту сохраненную постройку.
saved-build-error-delete-notyours = Можно удалять только свои сохраненные постройки. (Администраторы могут удалять любые.)

# Build-mode dropdown at the top of the construction menu.
gmod-construction-menu-mode-admin = Строительство: админ (мгновенно)
gmod-construction-menu-mode-player = Строительство: игрок (призраки)
gmod-construction-menu-mode-mapper = Строительство: маппер (любая сущность)

# Build partners window (the "Partners" button).
build-partner-window-title = Партнеры строительства
build-partner-window-desc = Добавьте игрока, чтобы он мог включать ВАШИ построенные предметы в свои сохраненные постройки.
build-partner-window-empty = Других игроков онлайн нет.
build-partner-window-add = Добавить
build-partner-window-remove = Убрать
build-partner-window-clear-all = Очистить всех партнеров
build-partner-granted-to-you = { $name } добавил вас в партнеры строительства - теперь вы можете сохранять его постройки.
build-partner-revoked-from-you = { $name } убрал вас из партнеров строительства.

# Saved-build window extra option (mapper mode) + detail-panel rename/delete.
saved-build-window-include-loose = Включать незакрепленные предметы
gmod-construction-menu-rename-build = Переименовать
gmod-construction-menu-delete-build-confirm = Подтвердить удаление?

# Placement controls hint (top-left).
saved-build-controls-mode-admin = Режим: админ (мгновенно, бесплатно)
saved-build-controls-mode-player = Режим: строительство (призраки + материалы)
saved-build-controls-gridalign = Alt (переключить): по сетке ({ $state })
saved-build-controls-state-on = вкл.
saved-build-controls-state-off = выкл.
saved-build-controls-rotate = { $key }: повернуть
saved-build-controls-place = ЛКМ: разместить
saved-build-controls-cancel = ПКМ: отмена

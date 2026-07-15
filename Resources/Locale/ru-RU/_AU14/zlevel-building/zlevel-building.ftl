# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 wray-git
# SPDX-License-Identifier: AGPL-3.0-only
# Building overhaul (z-level) - Phase 1: structural support graph
au-zsupport-unsupported = Эта секция больше не имеет опоры!
au-zsupport-admin-alert = Структура на Z-уровне обрушилась (потеря опоры) - вероятная причина: { $culprit }.

# Building overhaul (z-level) - underground cave-ins
au-cavein-warning = Потолок здесь стонет и трещит - сейчас будет обвал!
au-cavein-admin-alert = Подземный обвал ({ $count } тайлов) - вероятная причина: { $culprit }.

# Building overhaul (z-level) - structural scanner
au-scanner-on = Вы включаете структурный сканер.
au-scanner-off = Вы выключаете структурный сканер.

# Building overhaul (z-level) - mapper opt-out condition
construction-step-condition-au14-zbuild-allowed = На этой карте должно быть разрешено вертикальное строительство.

# Building overhaul (z-level) - construction menu entries
au14-construction-tile-plating = настил
au14-construction-tile-plating-desc = Уложить металлический настил. Можно размещать над пустотой, чтобы строить полы в воздухе.
au14-construction-tile-steel = стальной пол
au14-construction-tile-steel-desc = Уложить стальной пол. Можно размещать над пустотой, чтобы строить полы в воздухе.
au14-construction-tile-dirt = земля
au14-construction-tile-dirt-desc = Насыпать участок земли. Можно размещать над пустотой, чтобы строить полы в воздухе.

au14-construction-z-stairs-up = лестница z-уровней (вверх)
au14-construction-z-stairs-up-desc = Лестница, ведущая на один z-уровень выше; создает стоячую площадку уровнем выше и ставит здесь опорную балку.
au14-construction-z-stairs-down = лестница z-уровней (вниз)
au14-construction-z-stairs-down-desc = Лестница, ведущая на один z-уровень ниже; отражает опорную балку на уровне ниже.

au14-construction-support-beam-wood = деревянная опорная балка
au14-construction-support-beam-wood-desc = Деревянная опорная балка. Ставьте ее под полом верхнего уровня, чтобы удерживать его: дешево, но перекрывает малое расстояние.
au14-construction-support-beam-metal = металлическая опорная балка
au14-construction-support-beam-metal-desc = Стальная опорная балка. Ставьте ее под полом верхнего уровня, чтобы удерживать его: надежный универсальный пролет.
au14-construction-support-beam-plasteel = пласталевая опорная балка
au14-construction-support-beam-plasteel-desc = Пласталевая опорная балка. Ставьте ее под полом верхнего уровня, чтобы удерживать его: дорого, зато перекрывает самую широкую платформу.

## Z-Level Toggles admin tool (construction menu > Tools)
au-zlevel-toggles-title = Переключатели Z-уровней
au-zlevel-toggles-search = Поиск карт...
au-zlevel-toggles-hint = Да = игроки могут строить по Z-уровням на этой карте. Сохраняется между раундами.
au-zlevel-toggles-yes = Да
au-zlevel-toggles-no = Нет
au-zlevel-toggles-map-loaded = {$map} (загружена)
au-zlevel-toggle-enabled = Строительство по Z-уровням РАЗРЕШЕНО на {$map}.
au-zlevel-toggle-disabled = Строительство по Z-уровням ЗАПРЕЩЕНО на {$map}.

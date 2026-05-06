## Upstream sync 2026-04-30

- Ветка для синхронизации: `sync-upstream/300426`.
- Актуальный `upstream/master` содержит большой медицинский апдейт `CMU14`, который затрагивает UI health scanner, shared state и множество прототипов/ресурсов.
- При merge конфликтовали в основном:
  - `HealthScanner*` файлы
  - несколько `_RMC14` прототипов (`defib`, `lathe`, `chem`, `radio_channels`)
  - `Resources/migration.yml`
  - GitHub workflows
- Практические решения по конфликтам:
  - медсканер приведён к новой схеме апстрима с `HealthScannerBuildStateEvent` и расширяемым `HealthScannerBuiState`
  - локализованный `MarineIntel` keycode в `radio_channels.yml` сохранён
  - удаление `.github/workflows/rsi-diff.yml` сохранено как решение форка
  - в `chem.yml` сохранены наши structural-компоненты диспенсера и добавлены новые CMU-реагенты
  - в `lathe.yml` сохранены локальные рецепты хирургии/орданса
  - в `migration.yml` сохранены локальные migration-строки и исправлена дата блока CMU на `2026-04-23`
- После ручного разрешения конфликтов `dotnet build SpaceStation14.sln -c Debug -v minimal` проходит успешно.

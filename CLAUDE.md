# CLAUDE.md — Правила проекта RussianCM

Этот репозиторий является форком **RMC-14** (CM-SS14) на движке **RobustToolbox / Space Station 14**.
Проект называется **RuMC14** — русскоязычный форк Colonial Marines SS14.

При изучении кода записывай ключевые выводы в файлы в `.claude/memory/`. Используй эти файлы при каждой новой сессии.

---

## СТРУКТУРА РЕПОЗИТОРИЯ

```
RussianCM/
├── Content.Server/          # Серверная логика (авторитетная)
├── Content.Shared/          # Общие компоненты и контракты
├── Content.Client/          # Клиентская часть (UI, визуализация)
├── Content.Server.Database/ # Модели БД и миграции
├── Content.IntegrationTests/# Интеграционные тесты
├── Resources/               # Контент (YAML-прототипы, звуки, текстуры, локализация)
│   ├── Prototypes/
│   │   ├── _RMC14/         # Прототипы апстрима RMC14 (956+ YAML-файлов)
│   │   └── _RuMC/          # Кастомные прототипы RuMC14
│   └── Locale/
│       ├── en-US/           # Английская локализация
│       ├── ru-RU/           # Русская локализация
│       └── nl-NL/           # Нидерландская локализация
├── RobustToolbox/           # Движок (git submodule, отслеживает upstream master)
├── RSI.NET/                 # RSI-утилиты (git submodule)
└── SpaceStation14.sln       # Solution файл
```

---

## КАСТОМНЫЙ КОД: ВАЖНОЕ РАЗГРАНИЧЕНИЕ

В проекте существуют **два разных** префикса директорий:

| Директория | Назначение |
|---|---|
| `_RMC14` | **Апстрим** из родительского проекта RMC-14 (не трогать без крайней нужды) |
| `_RuMC14` | **Кастомный код RuMC14** — здесь ведётся разработка |
| `_RuMC` | Кастомные прототипы/ресурсы в `Resources/` |

### Весь кастомный код размещать только в:
- `Content.Server/_RuMC14/`
- `Content.Shared/_RuMC14/`
- `Content.Client/_RuMC14/`
- `Resources/Prototypes/_RuMC/`
- `Resources/Locale/ru-RU/_RuMC/`

**Не размещать кастомный код в `_RMC14`, `Content.*` (корень) или других апстрим-директориях.**

---

## КАСТОМНЫЕ СИСТЕМЫ (_RuMC14)

### Серверные системы (`Content.Server/_RuMC14/`)
| Система | Файл | Назначение |
|---|---|---|
| `TTSSystem` | `TTS/TTSSystem.cs` + partial-классы | Text-to-speech: озвучка игроков |
| `TTSManager` | `TTS/TTSManager.cs` | Управление TTS-очередями и rate-limit |
| `VoiceMaskSystem.TTS` | `TTS/VoiceMaskSystem.TTS.cs` | Маскировка голоса с TTS |
| `RMCOrdnanceAssemblySystem` | `Ordnance/` | Сборка боеприпасов |
| `RMCExplosionSimulatorSystem` | `Ordnance/` | Симулятор взрывов |
| `RMCDemolitionsSimulatorSystem` | `Ordnance/` | Симулятор подрывных работ |
| `RMCMineCasingSystem` | `Ordnance/` | Система мин |
| `RMCChembombSystem` | `Ordnance/` | Химические бомбы |
| `PlaytimeApi` | `Playtime_API/` | API для трекинга игрового времени |

### Общие компоненты (`Content.Shared/_RuMC14/`)
| Компонент/Система | Назначение |
|---|---|
| `TTSComponent`, `TTSVoicePrototype` | Конфигурация TTS для сущностей |
| `PlayTTSEvent`, `RequestPreviewTTSEvent` | События TTS |
| `GridAmbienceComponent`, `SharedGridAmbienceSystem` | Амбиентная атмосфера на гридах |
| `RMCOrdnanceAssemblyComponent` и 10+ Ordnance-компонентов | Данные о боеприпасах |

### Клиентские системы (`Content.Client/_RuMC14/`)
| Класс | Назначение |
|---|---|
| `RuMCDemolitionsSimulatorBui` | BUI окно симулятора подрыва |
| `RuMCExplosionSimulatorBui` | BUI окно симулятора взрыва |
| `RuMCOrdnanceAssemblyVisualsSystem` | Визуализация сборки боеприпасов |
| `GridAmbienceSystem` | Клиентская система амбиентности |

---

## АРХИТЕКТУРА (ECS)

Проект использует Entity-Component-System:

- **Entity** — контейнер компонентов (идентификатор сущности)
- **Component** — только данные, без логики
- **System** — вся игровая логика

### Слои проекта

| Слой | Роль |
|---|---|
| `Content.Server` | Авторитетная игровая логика, симуляция, создание/удаление сущностей |
| `Content.Shared` | Компоненты, сетевые сообщения, enum, константы |
| `Content.Client` | UI, визуализация, обработка ввода |

---

## ПРАВИЛА СЕРВЕРА И КЛИЕНТА

### Сервер:
- Полностью управляет состоянием игры
- Выполняет симуляции и проверки
- Создаёт и удаляет сущности
- Является единственным источником истины

### Клиент:
- Отвечает за UI и визуализацию
- Обрабатывает ввод игрока
- **Никогда не принимает решений о геймплее**
- Изменяет только спрайты и визуальные эффекты

### Shared:
- Компоненты
- Сетевые сообщения
- Общие enum и константы

---

## СЕТЕВОЕ ВЗАИМОДЕЙСТВИЕ

- Использовать networked components только для синхронизации состояния
- Минимизировать объём передаваемых данных
- Не передавать сущности целиком
- Предпочитать событийную модель вместо постоянного опроса (Update-polling)

---

## UI ПРАВИЛА

- Использовать **Bound User Interface (BUI)** — файлы `*Bui.cs`
- UI не должен изменять игровое состояние напрямую
- Поток данных: Клиент → Сервер (сообщения) → Клиент (состояние UI)
- Логика UI отделена от игровой логики

---

## КОМПОНЕНТЫ

- Компоненты содержат **только данные**
- Никакой логики внутри компонентов
- Компоненты должны быть маленькими и специализированными
- Предпочительна **композиция**, а не наследование

---

## СИСТЕМЫ

- Системы содержат всю игровую логику
- По возможности системы должны быть **без состояния** (stateless)
- Избегать жёсткой связности между системами
- Использовать событийную модель вместо Update-поллинга

---

## ВИЗУАЛИЗАЦИЯ

- Изменение спрайтов — только на клиенте
- Использовать `Appearance` / `Visualizer` системы
- Сервер **не должен** напрямую изменять `SpriteComponent`
- Сервер задаёт состояние → клиент отображает

---

## БАЗА ДАННЫХ

БД поддерживает **SQLite** (разработка) и **PostgreSQL** (продакшн).

Схема: `Content.Server.Database/` — `Model.cs`, `ModelSqlite.cs`, `ModelPostgres.cs`

### Кастомные миграции RMC14 (26 пар Sqlite + Postgres):
- `RMCPatrons` — система патронов и их привилегий
- `RMCNamedItems` — именные предметы
- `RMCLinkedAccountLogs` — логи привязанных аккаунтов
- `RMCSquadPreference` — предпочтения отряда
- `RMCCommendations` — система наград
- `RMCChatBans` — бан в чате
- `RMCPlayerStats*` — статистика игроков

При добавлении новых колонок/таблиц — создавать **пару** миграций (Sqlite + Postgres).

---

## ЛОКАЛИЗАЦИЯ

| Язык | Директория | Объём |
|---|---|---|
| Русский | `Resources/Locale/ru-RU/` | ~3931 .ftl файлов |
| Английский | `Resources/Locale/en-US/` | основной |
| Нидерландский | `Resources/Locale/nl-NL/` | частичный |

Кастомные строки: `Resources/Locale/ru-RU/_RuMC/`

---

## ПРОТОТИПЫ И YAML

Кастомные прототипы (`Resources/Prototypes/_RuMC/`):
- `Effects/radio.yml`
- `Entities/Objects/Ordnance/` — C4, детонаторы, гранаты, мины, миномёты, ракеты, баки
- `Entities/Structures/Machines/dropship_spawners.yml`
- `Entities/areas_corsat.yml`
- `SoundCollections/warcry.yml`
- `tts.yml` — голоса TTS

Кастомные гайдбуки (`Resources/Prototypes/_RuMC14/`):
- `marine_law.yml`, `rumc_guidebook.yml`, `rumc_rules.yml`

---

## СРЕДА РАЗРАБОТКИ И СБОРКА

### Версия .NET:
```
SDK: 9.0.100 (rollForward: latestFeature)
```

### Первый запуск:
```bash
python3 RUN_THIS.py   # инициализация submodule-ов и загрузка движка
```

### Запуск сервера/клиента:
```bash
./runserver.sh        # dotnet run --project Content.Server
./runclient.sh        # dotnet run --project Content.Client
```

### С инструментами:
```bash
./runserver-Tools.sh
./runclient-Tools.sh
```

### RobustToolbox:
- Является **git submodule** (отслеживает upstream master RobustToolbox)
- Второй submodule: `RSI.NET` (утилиты для RSI-спрайтов)
- **Не обновлять submodule-ы без явного согласования**

---

## ТЕСТИРОВАНИЕ

Тесты находятся в `Content.IntegrationTests/`.

### Кастомные тесты _RMC14:
- `PlanetMapLoadTests.cs` — загрузка карт планет
- `RMCLagCompensationTest.cs` — тест компенсации лага
- `RMCTestExtensions.cs` — вспомогательные методы тестов
- `RMCUpstreamTileAndPlatingCheck.cs` — проверка совместимости тайлов с апстримом

### Запуск тестов:
```bash
dotnet test Content.IntegrationTests/
dotnet test Content.Tests/
```

---

## CI/CD (GitHub Actions)

23 автоматических workflow в `.github/workflows/`:

| Workflow | Назначение |
|---|---|
| `build-test-debug.yml` | Основной CI (сборка + тесты) |
| `publish.yml` | Релизная публикация |
| `yaml-linter.yml` | Валидация YAML-прототипов |
| `validate-rsis.yml` | Валидация RSI-спрайтов |
| `validate-rgas.yml` | Валидация RGA-файлов |
| `rmc-mapchecker.yml` | Проверка карт |
| `check-crlf.yml` | Контроль окончаний строк (LF only) |
| `no-submodule-update.yml` | Блокировка случайного обновления submodule |
| `labeler-*.yml` (×10) | Автоматическая расстановка меток PR |

Требования bors для мерджа: Build & Test (Debug + Release), Test Packaging, YAML Linter, RSI + RGA валидация.

---

## СТИЛЬ КОДА

- Следовать **существующему стилю** проекта
- Избегать лишней абстракции
- Приоритет — читаемость
- Изменения должны быть **точечными**
- **Окончания строк: LF** (не CRLF)

---

## КОММЕНТАРИИ В КОДЕ

- Все комментарии писать на **русском языке**
- Комментарии объясняют "зачем", а не "что делает код"
- Избегать очевидных комментариев

---

## АПСТРИМ И ПОДДЕРЖКА

- Минимизировать изменения в официальном коде (`_RMC14`, `Content.*` корень)
- Логика реализуется через расширения, компоненты или системы
- Изменения базового кода допускаются **только при крайней необходимости**
- Проект должен оставаться совместимым с апстримом RMC-14 и SS14

---

## ПОВЕДЕНЧЕСКАЯ МОДЕЛЬ РАЗРАБОТКИ

При реализации задач:

1. Сначала анализируй существующий код в `_RuMC14`
2. Затем ищи аналог в `_RMC14` (апстрим)
3. Используй уже существующие системы
4. Расширяй, а не переписывай
5. Избегай глобальных изменений в апстрим-директориях
6. Сохраняй серверную авторитетность
7. Клиент оставляй только для отображения

---

## ЕСЛИ НЕ УВЕРЕН

- Ищи аналог в `Content.Server/_RMC14/` или `Content.Shared/_RMC14/`
- Придерживайся текущих паттернов проекта
- Выбирай самое простое корректное решение
- Не вводи новую архитектуру без необходимости

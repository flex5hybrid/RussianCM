# RuCMU AI Admin Assistant — инфраструктура этапа B

> Исторический протокол этапа. Актуальная поставка и ограничения: [инструкция релиза](rucmu-admin-ai-release.md).

Рабочая ветка: `codex/rucmu-admin-ai-stage-b`, продолжение этапа A
(`e4a0ecabe90`). Пользователь поручил продолжить разработку; Linux-приёмка A
остаётся открытой. Здесь подготовлены код и шаблоны, а не production-развёртывание.
На машине разработки Windows, без Linux/systemd и без модели GGUF.

## Поведение сервера

`IAdminAIService` регистрируется в серверном IoC и получает существующие
фазы EntryPoint. При `cmu.admin_ai.enabled=false` сервис не создаёт transport,
очередь, фоновые задачи или PSI reader. Игровой сервер запускается без worker.
`ahelp`, его маршрутизация и UI не изменены. Триггеры генерации появятся в C
после проверки прав и контекста. Нет исполнения команд модели или наказаний.

При включении сервис создаёт один UDS inference client и отдельный health client.
Проверка health каждые 5 секунд ограничена 3 секундами; ошибки/loading дают
Unavailable. Никаких запуска/перезапуска systemd из игры или запасного TCP/API.
При недоступности worker ожидающие запросы завершаются Unavailable, текущий
отменяется; новый worker обнаруживается последующим health poll.

Очередь ограничена ожидающими задачами (8 по умолчанию) плюс одной выполняемой.
LightManual имеет приоритет перед HeavyManual и Background, внутри класса FIFO.
GUID дедуплицируется только среди выполняемых и ожидающих. Ожидание имеет TTL,
отмена удаляет ожидание; после завершения ссылка очереди на исходный текст очищается.
Timeout/отмена клиента, проигнорировавшего cancellation, **не освобождают физический
слот** до завершения его операции. Следующие задачи в это время истекают по TTL.
Отмена UDS — best-effort: остановку вычислений в конкретной сборке llama нужно
проверять отдельно. Worker имеет собственный `--parallel 1` и cgroup-ограничения.
При переходе Normal → Degraded текущая генерация отменяется; новые лёгкие запросы
получают уменьшенный token budget. Paused очищает ожидание и отменяет выполнение.

Все transport/PSI операции, ожидания и диагностика запросов выполняются вне
игрового потока. Очередь содержит короткие критические секции, не вызывает transport
или внешние audit callbacks под lock. Диагностика отказов/завершений использует
ограниченный канал 64 событий; при переполнении старые события вытесняются.
Это техническая диагностика B, не полный аудит обращений/инициаторов будущего C.
Тексты промптов, ответов, пути сокета и данные игроков в неё не пишутся.

## Governor и метрики

Начальное состояние Paused. На Linux фоновые задачи читают `/proc/pressure` и,
если явно заданы, до четырёх cgroup v2 директорий игровых служб. Используются
CPU `some avg10`, memory/io `full avg10`, максимум global/configured cgroups.
Отсутствие, повреждение или устаревание любого требуемого источника даёт Paused.
Каждый файл ограничен 4096 байтами; нет обхода игровых данных. Семантика PSI:
[документация ядра](https://docs.kernel.org/accounting/psi.html).

Длительность измеряется существующими границами PreEngine → следующий PreEngine
либо FramePreEngine. В исследованном `BaseServer.Update` после PostEngine ещё
выполняется SendGameStateUpdate, а в `GameLoop` FrameUpdate идёт после catch-up
тиков. Поэтому выбранный интервал включает отправку состояния и небольшой
межтиковый overhead, но не ожидание следующего кадра. Это консервативное измерение
границ, а не точный движковый профиль отдельного тика. Максимум интервала за окно
poll делится на TickPeriod. RobustToolbox не изменён и profiler не включается.
Для production baseline TPS/tick p95/p99 нужны отдельные штатные измерения.

Перегрузка переводит в защиту сразу. Восстановление требует всех метрик ниже
75% порогов Degraded непрерывно 30 секунд: Paused → Degraded, затем ещё 30 секунд
до Normal. В Degraded разрешён только LightManual с коротким входом и 64 tokens.
Stale watchdog проверяет свежесть на tick/admission и блокирует очередь даже
при зависшем фоновом чтении. На Windows с governor по умолчанию генерация Paused.
Отключение governor — явное действие оператора; системные лимиты всё равно нужны.

Новые CVar (все `SERVERONLY | ARCHIVE`, без репликации):

| Имя после `cmu.admin_ai.` | Default | Допустимые пределы |
| --- | --- | --- |
| max_concurrent_requests | 1 | Только 1 |
| queue_capacity / queue_ttl_seconds | 8 / 30 | 1..32 / 1..120 |
| degraded_input_chars / degraded_output_tokens | 1024 / 64 | 1..max_input_chars / 1..128 |
| governor_enabled | true | bool |
| poll_seconds / recovery_seconds | 2 / 30 | 1..10 / 5..300 |
| max_sample_age_seconds | 5 | 2×poll..60 |
| cpu_psi_degraded / cpu_psi_paused | 10 / 25 | 0 < low < high ≤ 100 |
| memory_psi_degraded / memory_psi_paused | 1 / 5 | То же |
| io_psi_degraded / io_psi_paused | 5 / 15 | То же |
| tick_budget_degraded / tick_budget_paused | 0.8 / 1.2 | 0 < low < high ≤ 10 |
| cgroup_paths | пусто | До 4 абсолютных `/sys/fs/cgroup/...`, разделённых `;` |

Настройки этапа A сохранены. Любое изменение подписанных CVar сначала навсегда
блокирует старую очередь, отменяет её и освобождает фоновые задачи; новая очередь
создаётся только после их завершения. При ошибочных настройках inference выключен,
диагностика показывает invalid-configuration. `cmu.admin_ai.enabled=false` отменяет
генерацию без остановки игрового сервера. При зависшем завершении старого сервиса
новый не стартует. Тексты старых задач не переносятся в новую конфигурацию.

`admin_ai_status` доступен локальной консоли и администраторам с Debug и показывает
только cached state/health/reason и счётчики очереди. `admin_ai_probe` сохранён.
Meter `CMU.AdminAI` использует штатный `IMeterFactory` и имена `cmu_admin_ai_*`:
enabled, state (Normal=0, Degraded=1, Paused=2), worker_ready, queue_pending,
queue_running, accepted/completed/rejected_since_reload, CPU/memory/io PSI avg10,
tick_boundary_budget_ratio. Отсутствующие нагрузки — NaN, не фиктивный ноль.
Счётчики since_reload — gauges текущей очереди, сбрасываются при reload. Нет
labels по игрокам, GUID или обращениям. Экспорт использует существующие серверные
настройки metrics; новый listener не создаётся.

## Локальное развёртывание (только администратор Linux-хоста)

1. Сначала запустить `bash deploy/linux/audit-game-placement.sh` с **реальными**
   именами службы RuCMU и трёх Squad. Сохранить отчёт. Скрипт только читает:
   cgroup v2, lscpu NUMA/SMT, affinity основного процесса и наборы affinity потоков,
   CPU quota, память и PSI. Названия служб/доступ к хосту в этом задании не даны.
   Не менять affinity Squad. Если есть свободные ядра, обсуждать cpuset AI по
   реальному отчёту, избегая SMT-соседей занятых игровых ядер и чужих NUMA nodes.
2. Выбрать локальную CPU-only сборку llama-server с UDS и локальный GGUF в пределах
   памяти. Зафиксировать commit/build, SHA256 бинарника, зависимости/их hashes,
   SHA256/название/квантизацию модели и её лицензию. Никаких выдуманных pin, загрузок
   из worker или автоматического выбора модели. Права на распространение проверяет
   оператор по выбранной лицензии. [Официальный интерфейс llama-server](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md)
   описывает UDS `--host ...sock`, offline, parallel и отключение web UI/agent.
   Закреплённая локальная версия должна подтвердить эти возможности через --help.
3. Подготовить пользователя `rucmu-ai` без login, группу `rucmu-ai-socket`;
   доступ к группе давать только игровому сервису RuCMU и worker. Бинарник и
   `worker-preflight.sh` размещаются в `/opt/rucmu-ai/bin/`, модель и LICENSE —
   в `/opt/rucmu-ai/models/`. Всё root-owned, worker не может менять файлы/каталоги.
   Модель доступна ему только на чтение. Не давать worker доступ к игровым группам.
4. По `worker.env.example` создать root-owned `/etc/rucmu-ai/worker.env`; заполнить
   реальные pins. Это systemd EnvironmentFile, не shell script; не `source` его.
   Не добавлять LLAMA_ARG_*, токены, LD_PRELOAD, LD_LIBRARY_PATH или команды.
   Preflight отвергает placeholders, hash/version mismatch, отсутствие GGUF/license,
   запись в артефакты, отсутствие требуемых CLI flags. Он не подтверждает лицензию.
5. Проверить `rucmu-ai-worker.service` на **целевом systemd**. В InaccessiblePaths
   добавить фактические игровые/секретные директории из вашего развёртывания,
   если они отличаются от /srv, /var/lib/rucmu, /var/lib/squad, /etc/rucmu, /etc/squad.
   Без этого, pins, базового отчёта и baseline шаблон не считается принятым.
   Read-only /opt/rucmu-ai; запись разрешена в /run/rucmu-ai и приватные временные каталоги;
   worker stderr/stdout отключены, промпты в journal не сохраняются.
6. Шаблон начинает с CPUQuota=100% (не более одного CPU по времени), threads=2,
   CPUWeight/IOWeight=1, Nice=15; MemoryHigh=3G, MemoryMax=4G, MemorySwapMax=0.
   Это стартовые предохранители, а не гарантия отсутствия конкуренции. Если модель
   не помещается — выбрать меньшую, не повышать лимиты автоматически. Максимум
   первоначальной CPUQuota по ТЗ 200%; любое повышение — после измерений.
   AllowedCPUs задавать только отдельным drop-in по проверенному placement.
7. `systemd-analyze verify` и `systemd-analyze security` на хосте должны подтвердить
   поддержку директив. Проверить effective cgroup `cpu.max`, `memory.high`,
   `memory.max`, `memory.swap.max`, cpuset; свойства через `systemctl show`.
   [Ресурсные ограничения systemd](https://github.com/systemd/systemd/blob/main/man/systemd.resource-control.xml),
   [изоляция процесса](https://github.com/systemd/systemd/blob/main/man/systemd.exec.xml).
   PrivateNetwork + RestrictAddressFamilies=AF_UNIX запрещают TCP/UDP; GPU скрыт
   PrivateDevices, n-gpu-layers=0 и no-kv-offload. Никаких cloud API/tools/MCP.
   Unix socket должен иметь группу rucmu-ai-socket и права 0660, каталог 0770.
   Проверить `stat` и отсутствие других listening sockets; недопустимые права
   исправлять в шаблоне/закреплённой сборке, не через chmod 777.
8. В отдельном тестовом окружении проверить отсутствие записи в игровые пути,
   недоступность внешней сети и GPU, UDS health и bounded response, timeout/
   отмену, смерть/OOM/restart worker. systemd restart не чаще 120 секунд, максимум
   два старта за 600 секунд; при crash-loop остановится до ручного reset-failed.
   Content.Server сам не пытается запустить worker. Не тестировать OOM на production.
9. Только затем baseline всех четырёх серверов и сравнение с ручной очередью AI:
   одинаковая карта/нагрузка, TPS, tick p95/p99, global/per-unit PSI, CPU/RAM,
   latency каждого Squad. Сохранить длительность, нагрузку и конкретные лимиты.
   При ухудшении отключить AI CVar и снизить квоты/выбрать меньшую модель.
   Автоматические ответы в B отсутствуют. К C не относится разрешение их включить.

Rollback: `cmu.admin_ai.enabled=false` на RuCMU, проверить Disabled и пустую очередь;
администратор останавливает только rucmu-ai-worker. Удаление шаблона/группы не
требует изменений ahelp или базы данных. Не удалять group пока RuCMU использует её.

## Проверки разработки и открытая приёмка

Изменённые файлы относительно этапа A (25 файлов):

- `Content.CMU/Server/AdminAI/`: новые `AIRequestQueue.cs`, `AIResourceGovernor.cs`,
  `LinuxAIPressureReader.cs`, `AdminAIService.cs`, `AdminAIStatusCommand.cs`;
  изменены `AdminAICVars.cs`, `IAIInferenceClient.cs`, `AIInferenceOptions.cs`,
  `AIInferenceProtocol.cs`, `UdsAIInferenceClient.cs`.
- `Content.Server/Entry/EntryPoint.cs`, `Content.Server/IoC/ServerContentIoC.cs`:
  lifecycle и регистрация сервиса.
- `Content.CMU/Resources/Locale/{en-US,ru-RU}/CMU14/admin-ai.ftl`: status command.
- `Content.IntegrationTests/_CMU14/AdminAI/`: новые `AIInfrastructureTests.cs`;
  изменены `AIInferenceTests.cs`, `AdminAIConfigurationTests.cs`,
  `AdminAIServerIpcTests.cs`; links в `Tools/AdminAI.Tests/AdminAI.Tests.csproj`.
- `deploy/linux/`: `rucmu-ai-worker.service`, `worker.env.example`,
  `worker-preflight.sh`, `audit-game-placement.sh`.
- `Docs/rucmu-admin-ai-stage-a.md`, `Docs/rucmu-admin-ai-stage-b.md`.

Проверено 2026-10-13 на Windows/.NET 10, с сохранёнными engine binaries:

| Проверка | Фактический результат |
| --- | --- |
| Content.Server, `--no-restore`, `BuildProjectReferences=false`, frozen references helper из A | 0 errors, 66 warnings; 38.38 s |
| Tools/AdminAI.Spike build | 0 errors, 0 warnings |
| Tools/AdminAI.Tests build с опциональными engine CVar bindings | 0 errors, 2 NU1900 (NuGet audit недоступен в tool sandbox) |
| Boundary/governor/queue/PSI NUnit | 73 passed, 0 failed, 0 skipped |
| Actual UDS transport NUnit | 12 passed, 0 failed, 0 skipped |
| Tools/AdminAI.ServerTests build | 0 errors, 0 warnings |
| Actual compiled Content.Server в изолированном engine server thread | 4 passed, 0 failed, 0 skipped |
| Отдельные процессы mock-worker/probe, остановка и перезапуск | Ready/Success → Unavailable (exit 2) → Ready/Success |
| bash -n для двух deploy скриптов и linux-smoke.sh (Git Bash) | Синтаксис корректен |
| Статическое сравнение en-US/ru-RU Fluent | 9 одинаковых ключей, переменные совпадают |
| RobustToolbox/RSI.NET status, engine DLL SHA256 | Подмодули чистые; DLL Robust.Shared/Server/Shared.Maths совпадают с исходными |

Итого **89 тестов**. Очередь под искусственной нагрузкой: submitted=1001,
accepted=9 (один активный + восемь ожидающих), rejected=992; max_pending=8,
max_inference=1. Проверены приоритет, TTL при активной генерации, дедупликация,
отмена, игнорирование cancellation клиентом, таймаут, reload invalid/disabled,
разные источники перегрузки, старые/NaN метрики и восстановление с гистерезисом.

В процессе тестов исправлена задержка очистки TTL: sweep теперь выполняется и
при обычном pending inference, не только после timeout. Изолированный серверный
тест проверяет expiration ожидающего запроса и обработку серверных действий
при stalled UDS; Paused возвращает результат без черновика. Это не игровой load test.

Логи текущего запуска: `.codex-build/admin-ai/stage-b/` (ignored): server-build.log,
tests-build.log, boundary-tests.log/boundary-nunit.xml, ipc-tests.log,
server-tests-build.log, server-tests.log/server-nunit.xml, initial/worker-stopped/
restarted-probe.log. Полный список изменённых файлов доступен в diff ветки.
Warnings сборки сервера относятся к существующим deprecated API и NuGet audit
ImageSharp; они не исправлялись в AI-коммите. Чистая сборка всего dependency graph
не выполнялась; Content.Server собирался против существующих зависимостей,
как в A, без пересборки движка и без отключения анализаторов/генераторов.

Команды NUnit (из корня рабочей ветки, после сборки):

```powershell
dotnet Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll boundary
$env:RUCMU_AI_TEST_SOCKET = Join-Path $env:TEMP ('rucmu-ai-' + [guid]::NewGuid().ToString('N').Substring(0,8) + '.sock')
dotnet Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll ipc
$env:RUCMU_AI_ENGINE_RESOURCES = Join-Path (Get-Location) 'RobustToolbox/Resources'
dotnet Tools/AdminAI.ServerTests/bin/Debug/net10.0/Content.IntegrationTests.dll ipc
```

Оба IPC runner используют одинаковое имя отчёта `.codex-build/admin-ai/ipc-nunit.xml`;
сохранять его отдельно после каждого запуска. Socket должен быть уникальным,
абсолютным и короче 108 UTF-8 bytes. Это test-only endpoint, production путь
остаётся `/run/rucmu-ai/llama.sock`. При запрете tool sandbox использовано ранее
данное пользователем разрешение на локальный IPC; policy RobustToolbox не менялась.

Linux runtime, effective systemd/cgroup права, real llama/GGUF и нагрузка четырёх
серверов здесь **не проверены**. Подготовлены воспроизводимые Linux smoke этапа A
и read-only placement audit; Git Bash syntax check не заменяет их запуск на Linux.
Полная игровая integration suite и UI/ahelp E2E также не выполнялись. Этап C
потребует собственных проверок прав, отзыва доступа, смены раунда и черновиков.

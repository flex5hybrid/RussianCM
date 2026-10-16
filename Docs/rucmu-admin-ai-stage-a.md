# RuCMU AI Admin Assistant: аудит и этап A

> Исторический протокол этапа. Актуальная поставка и ограничения: [инструкция релиза](rucmu-admin-ai-release.md).

Дата проверки: 2026-10-13. Репозиторий `flex5hybrid/RussianCM`, база
`origin/master` = `70bb8f196b1`, ветка `codex/rucmu-admin-ai-stage-a`.

**Статус: IPC из настоящего собранного Content.Server подтверждён на изолированном
серверном потоке Windows. Content.Shared и Content.Server собраны против
существующих бинарников зависимостей без пересборки RobustToolbox. Этап A ещё
не принят: проверка на целевом Linux остаётся неподтверждённой. Интеграция с ahelp
не начата, к этапу B не переходить.** Диагностический mock-worker не является
ИИ и не доказывает работоспособность llama-server или качество модели.

## Проверенные точки интеграции

| Код | Реальное поведение и граница будущей интеграции |
| --- | --- |
| `Content.Server/Administration/Systems/BwoinkSystem.cs:699` | `OnBwoinkTextMessage` проверяет свою переписку / `AdminFlags.Adminhelp`, запрещает чужой `AdminOnly` без прав, затем вызывает `PlayerRateLimitManager.CountAction`. Триггер AI допустим только после обоих проверок и не должен задерживать штатную доставку. |
| Там же, строки 725–778 | Получатели выбираются через `GetTargetAdmins`, сообщение форматируется отдельно для администратора и игрока. Затем сохраняется Discord relay; при отсутствии админов и webhook отправляется штатное уведомление. Весь маршрут оставлен без изменений. |
| `Content.Shared/Administration/SharedBwoinkSystem.cs` | `BwoinkTextMessage` содержит `UserId`, `TrueSender`, `Text`, `SentAt`, `PlaySound`, `AdminOnly`. Уникального ID сообщения нет, сервер игнорирует переданный клиентом `TrueSender`. Нужен отдельный серверный correlation/case ID, а не доверие сетевому пакету. |
| `Content.Client/Administration/Systems/BwoinkSystem.cs` | `Send` посылает штатное сообщение, сервер устанавливает настоящего отправителя. UI получает события через `OnBwoinkTextMessageRecieved`. |
| `Content.Client/Administration/UI/Bwoink/` | `BwoinkControl` управляет `AdminOnly` и звуком; `BwoinkPanel.SenderLineEdit` отправляет/очищает поле; `TextOutput` использует `FormattedMessage` и разрешённые теги. Будущий черновик требует безопасного отображения и вставки в поле без вызова отправки. |
| `Content.Server/Administration/Logs/IAdminLogManager.cs` | `All(LogFilter? filter = null, Func<List<SharedAdminLog>>? listProvider = null)` и `CurrentRoundLogs(LogFilter? filter = null)` возвращают `Task<List<SharedAdminLog>>`. |
| `LogFilter.cs` | Есть `Round`, `After`, `Before`, `AnyPlayers`, `AllPlayers`, `Limit`, `CancellationToken`, `Types`, `Impacts`, `DateOrder`. Лимиты и доступ должны проверяться перед запросом. |
| `Content.Shared/Administration/Logs/SharedAdminLog.cs` | Поля: `Id`, `Type`, `Impact`, `Date`, `Message`, `Players`; **нет RoundId**. Привязку `roundId:logId` надо сохранять из параметров сбора контекста. |
| `AdminLogsEui.cs` | Проверяет `AdminFlags.Logs` при запросах и изменении прав; использует отмену предыдущей выборки. Доступ к ahelp не означает доступ к логам. |
| `IAdminNotesManager.cs`, `AdminNotesManager.cs:58` | `CanView(ICommonSession)` проверяет `AdminFlags.ViewNotes`. `GetAllAdminRemarks(Guid)` возвращает в том числе секретные заметки; сам метод не заменяет проверку прав инициатора. |
| `Content.Server/Program.cs`, `RobustToolbox/Robust.Server/ServerOptions.cs` | Штатный сервер не переопределяет `ServerOptions.Sandboxing`, по умолчанию `false`. Эту настройку не изменяли. Клиентский whitelist не разрешает `Socket`, `NetworkStream`, `UnixDomainSocketEndPoint`, `SocketsHttpHandler`. IPC нельзя переносить в Shared/Client. |
| `Content.Server/Content.Server.csproj` | Автоматически компилирует `Content.CMU/Server/**/*.cs`; модуль размещён в `Content.CMU/Server/AdminAI`, namespace `Content.Server.CMU14.AdminAI`. Не требуется менять проекты движка. |
| `Content.IntegrationTests/Content.IntegrationTests.csproj` | NUnit 4.4.0, NUnit3TestAdapter 5.2.0, Microsoft.NET.Test.Sdk 18.0.1; файлы тестов включаются автоматически. |

У существующего обработчика `_activeConversations` обновляется ещё до проверки
прав. Это наблюдение аудита, а не точка запуска AI; исправление не включено
в этот этап.

## Что реализовано

- Серверный `IAIInferenceClient`: health, асинхронный запрос с `CancellationToken`,
  correlation ID, явные результаты Disabled/Unavailable/Cancelled/TimedOut/
  InvalidResponse/Success. Интерфейс не предоставляет игровых действий,
  команд, доступа к БД или инструментов модели.
- `UnavailableAIInferenceClient`: явно отключённый/недоступный клиент для
  заблокированного этапа. Не имитирует успешную генерацию, нигде не регистрируется
  и не подключается к ahelp.
- Неизменяемые ограничения `AIInferenceOptions`: проверка входа, длины вывода,
  токенов, таймаута и production-пути `/run/rucmu-ai/<имя>.sock`.
- `AIInferenceProtocol`: JSON-сериализация фиксированной system-политики и
  недоверенного user-текста в разных ролях, без tools; строгий разбор одного
  завершённого ответа assistant. Пустые, повреждённые, чрезмерно вложенные,
  слишком большие, обрезанные ответы и tool/function calls отвергаются.
- Server-only CVar, по умолчанию `enabled=false`. `AdminAICVars.Read` читает
  свежий snapshot и отключает его при неверных значениях. **Потребитель теперь
  только ручной health-probe: установка enabled=true не запускает никакого
  процесса/генерации.**
- Отдельный диагностический `Tools/AdminAI.Spike`: настоящий HTTP/JSON по UDS,
  proxy/redirect/cookies отключены, только HTTP/1.1, жёсткий лимит body,
  таймаут и отмена. URI `http://localhost/` используется только для HTTP framing;
  каждый `ConnectCallback` открывает `AddressFamily.Unix`, DNS/TCP fallback нет.
  После smoke-тестов транспорт перенесён в `Content.CMU/Server/AdminAI/UdsAIInferenceClient.cs`
  и компилируется в Content.Server. Инструменты используют тот же исходник,
  а серверные интеграционные тесты — настоящий собранный Content.Server.dll.
- Mock-worker как отдельный console process для проверки протокола.
  Ответ помечен `[MOCK WORKER: NO MODEL]`; GGUF не скачивается/не загружается.
  Существующий endpoint не удаляется перед bind; процесс живёт максимум 5 минут.
- NUnit-тесты с in-memory worker и отдельный реальный UDS smoke-test.
  Один и тот же серверный код и основные тесты компилируются обычным
  Content.IntegrationTests и небольшим standalone проектом.

Число токенов не гарантирует длину в символах или качество ответа. Parser
возвращает `UntrustedText`, который пока **никуда не отображается**. Экранирование
разметки/ссылок, права, TTL, раунды, отмена устаревших результатов и ручное
одобрение будут проверяться в этапе C. System prompt и корректный JSON сами
по себе не доказывают защиту модели от prompt injection.

## CVar и допустимые значения

Все определения находятся только в Content.Server, имеют `SERVERONLY | ARCHIVE`
без `REPLICATED`/`NOTIFY`. Параметры следующих этапов не добавлены как фиктивные
переключатели несуществующих функций.

| CVar | Default | Проверка при чтении snapshot |
| --- | --- | --- |
| `cmu.admin_ai.enabled` | `false` | Разрешает только ручной health-probe без генерации/отправки сообщений |
| `cmu.admin_ai.socket_path` | `/run/rucmu-ai/llama.sock` | Каталог `/run/rucmu-ai/`, ASCII имя, `.sock`, без traversal; до 107 UTF-8 bytes |
| `cmu.admin_ai.request_timeout_seconds` | `45` | 1–60 |
| `cmu.admin_ai.max_input_chars` | `4096` | 1–16384 |
| `cmu.admin_ai.max_output_tokens` | `256` | 1–512 |
| `cmu.admin_ai.max_output_chars` | `4096` | 1–8192 |

Жёсткий максимум JSON response: 32768 bytes; JSON depth: 16.
Предполагается одна модель без router/tool API. Очередь, resource governor и
квоты worker ещё не реализованы: диагностический client нельзя использовать
как production coordinator или запускать массовый инференс на игровой машине.

## Первоначальный IPC blocker внутри sandbox инструмента

На этой Windows-среде разрешено создать `Socket(AddressFamily.Unix, ...)`,
но настоящий `Bind(new UnixDomainSocketEndPoint(path))` завершается ошибкой:

```text
IPC_BLOCKED: SocketError=AccessDenied, native=10013:
Сделана попытка доступа к сокету методом, запрещенным правами доступа.
```

Это ошибка sandbox среды исполнения инструментов; она **не доказывает**, что
UDS невозможен на целевом Linux или в штатном Content.Server. На Linux здесь
тест не запускался. Первоначальный слишком длинный Windows temp-path заменён
на короткий путь внутри разрешённого workspace; после этого воспроизведён 10013.
При первоначальной проверке разрешения среды/движка не менялись для IPC,
запуск вне sandbox инструмента для сокетов не предпринимался.
Git-операции ветки и проверка подмодулей использовали
штатное разрешение инструмента на доступ к Git metadata, отдельно от IPC.

Из корня worktree:

```powershell
dotnet build Tools/AdminAI.Spike/AdminAI.Spike.csproj
dotnet Tools/AdminAI.Spike/bin/Debug/net10.0/AdminAI.Spike.dll selftest C:\Users\ha13r\GitHub\ColonialMarinesUniverse\admin-ai-spike.sock
```

Для независимых процессов на разрешённом Linux-стенде (после сборки):

```sh
# Только mock, без модели и игроков. Использовать свободный, доступный текущему пользователю путь.
dotnet Tools/AdminAI.Spike/bin/Debug/net10.0/AdminAI.Spike.dll worker /tmp/rucmu-ai-spike.sock
# В другом терминале:
dotnet Tools/AdminAI.Spike/bin/Debug/net10.0/AdminAI.Spike.dll probe /tmp/rucmu-ai-spike.sock
```

Успешная проверка должна показать `UDS health: Ready` и
`UDS inference protocol: Success`. Это проверит IPC с mock, но после этого
ещё требуется проверка **из Content.Server** на целевом Linux и с закреплённой
версией реального llama-server. До неё не считать этап A принятым.

Поддержка `.sock` в `--host` описана в
[официальной документации llama-server](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md).
Использован стандартный
[SocketsHttpHandler.ConnectCallback](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.connectcallback).
Установленного llama-server и GGUF в этом этапе нет, конкретные версия,
лицензия и hash не заявляются; их нужно зафиксировать при локальном развёртывании.

## Первоначальные проверки до разрешения запуска IPC вне sandbox

SDK: .NET 10.0.300. Все команды ниже выполнены из worktree этой ветки.

| Команда/проверка | Фактический результат |
| --- | --- |
| `dotnet build Tools/AdminAI.Spike/AdminAI.Spike.csproj --no-restore` | Успех, 0 warnings, 0 errors |
| `dotnet test Tools/AdminAI.Tests/AdminAI.Tests.csproj --filter 'TestCategory!=LocalIPC' --logger 'trx;LogFileName=boundary.trx' --results-directory .artifacts/admin-ai` | Сборка состоялась, запуск VSTest прерван: testhost connection timeout 90 секунд; это не успешный прогон тестов |
| `dotnet build Tools/AdminAI.Tests/AdminAI.Tests.csproj --no-restore -p:AdminAIEngineAssemblyDirectory='C:\Users\ha13r\GitHub\ColonialMarinesUniverse\bin\Content.IntegrationTests'` | Успех; компилирует все 5 новых серверных файлов против **существующего** Robust.Shared.dll, не пересобирая движок. 2 NU1900: источники vulnerability audit NuGet недоступны |
| `dotnet Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll boundary` | **34 passed, 0 failed, 0 skipped**. Штатный NUnit framework runner внутри одного процесса; без testhost IPC |
| `$env:RUCMU_AI_TEST_SOCKET='C:\Users\ha13r\GitHub\ColonialMarinesUniverse\admin-ai-nunit.sock'; dotnet Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll ipc` | **0 passed, 1 failed, 0 skipped**, `Socket.Bind`: AccessDenied/10013 |
| `dotnet build Content.Server/Content.Server.csproj --verbosity quiet` и diagnostic повтор | Неуспех на restore graph, до C# compilation; итоговый счётчик errors=0 не означает успех |
| `dotnet build Content.Server/Content.Server.csproj --no-restore --verbosity minimal` | `NETSDK1004`: нет `Content.Server/obj/project.assets.json` |
| `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter 'FullyQualifiedName~Content.IntegrationTests.CMU14.AdminAI' --verbosity minimal` | Остановлен после нескольких минут без прогресса на restore; интеграционные тесты не выполнены |
| `git diff --check --ignore-submodules=all` | Без whitespace ошибок |
| `git -C RobustToolbox status --short`, `git -C RSI.NET status --short` | Чисто. Gitlinks остались `1eee1d78e4f9b091f533191639606d1d34deb42a` и `3eead16bbade2214f4e0c4dd07a845051c24b075` |

Diagnostic build log содержит MSB4276 от DefaultSdkResolver, после которого
штатный WorkloadSdkResolver успешно разрешает соответствующие SDK. Поэтому
этот MSB4276 **не объявляется установленной причиной** неуспеха restore graph.
Изменять движок, отключать его проверки или чинить зависимости других задач
ради этого этапа не предпринималось.

XML результатов и журналы сохранены локально в `.codex-build/admin-ai/`
(ignored, не входят в коммит). Для тестов только BCL-части можно опустить
`AdminAIEngineAssemblyDirectory`: тогда CVar-тест не включается, проходят 33
проверки. Указание существующих бинарников — focused validation, а не полная
сборка проекта, игровой integration test или подтверждение Linux runtime.

## Smoke-тест после явного разрешения пользователя

2026-10-13 пользователь разрешил запуск IPC-теста вне ограниченного sandbox
инструмента. Настройки sandbox RobustToolbox, исходники и сборки движка
не изменялись. Использованы уже собранные диагностические DLL, синтетический
запрос и mock-worker без модели. Внешние AI API не вызываются.

| Сценарий | Результат | Измеренное время |
| --- | --- | --- |
| `AdminAI.Spike.dll selftest <короткий абсолютный .sock путь>` | `UDS health: Ready`, `UDS inference protocol: Success`, exit 0; сокет удалён при dispose | 122 ms |
| NUnit `ipc` внутри одного процесса | 1 passed, 0 failed, 0 skipped; реальный bind/connect/HTTP/JSON, socket cleanup подтверждён | 180 ms |
| Worker и probe в отдельных процессах, запуск 1 | `Ready`, `Success`, exit 0 | 131 ms |
| Probe после остановки тестового worker | `Unavailable`, ожидаемый exit 2; необработанного исключения нет | 84 ms |
| Новый worker в отдельном процессе на том же пути | `Ready`, `Success`, exit 0 | 128 ms |
| Повторный NUnit `boundary` | 34 passed, 0 failed, 0 skipped | — |
| Обычный VSTest со всеми тестами прототипа | **35 passed, 0 failed, 0 skipped**, exit 0 | 101 ms test duration |

Время CLI smoke-сценариев включает запуск dotnet-процесса и обработку mock;
это не задержка реального LLM и не измерение игровых тиков. Длительность
VSTest — значение отчёта test runner, без полного времени старта runner.

Для проверки между процессами запускались два собственных тестовых worker
по очереди (`Start-Process -WindowStyle Hidden`). После каждого теста остановлен
только созданный worker, оставшийся сокет удалён по проверенному абсолютному
пути внутри workspace. Игровые процессы не запускались и не останавливались.

Финальный обычный прогон уже собранных DLL:

```powershell
$env:RUCMU_AI_TEST_SOCKET = 'C:\Users\ha13r\GitHub\ColonialMarinesUniverse\ai-vstest-<unique>.sock'
dotnet test Tools/AdminAI.Tests/AdminAI.Tests.csproj --no-build --no-restore --logger 'trx;LogFileName=smoke-vstest.trx' --results-directory .codex-build/admin-ai/smoke-authorized-20261013
```

Локальные доказательства: `.codex-build/admin-ai/smoke-authorized-20261013/`
содержит `results.json`, `separate-process-results.json`, stdout/stderr worker,
результаты probe, `ipc-before-authorization.xml`, `ipc-after-authorization.xml`
и `smoke-vstest.trx`. Прежняя ошибка AccessDenied внутри sandbox сохранена;
успех относится к разрешённому запуску вне этого sandbox.

Не проверены реальные timeout во время генерации, OOM, Linux, llama-server/GGUF,
игровой Content.Server и нагрузка RuCMU + 3 Squad. Успех smoke не снимает
оставшиеся критерии приёмки A и не разрешает автоматически этап E.

## Продолжение A: серверный транспорт и реальные отказы IPC

После команды пользователя «Работаем дальше»:

- `UdsAIInferenceClient` перенесён в серверный модуль. Production-конструктор
  принимает проверенный `AIInferenceOptions`; отдельный internal-конструктор
  позволяет тестам выбрать собственный локальный endpoint, не меняя production CVar.
- Добавлен `admin_ai_probe`, только для локальной серверной консоли или
  администратора с Debug. Он делает только `GET /health`, не вызывает генерацию.
  По умолчанию отключён. На время одного запроса повторный запуск отвергается;
  IPC выполняется вне игрового потока, права и snapshot CVar перепроверяются
  перед выдачей статуса. Абсолютный путь сокета и модельный текст не выводятся.
- Добавлены английские/русские строки команды.
- Mock-worker поддерживает тестовые отказы. Реальный UDS проверен при зависании
  до заголовков/во время чтения body, активной отмене, обрыве, недостающем body,
  повреждённом/пустом JSON и превышении лимита body без Content-Length.
  Redirect отвергается без повторного запроса; неуспешный второй bind не удаляет
  сокет работающего первого worker. Эти режимы существуют только в тестовом mock.
- Два теста `AdminAIServerIpcTests` используют **настоящий собранный Content.Server.dll**
  на потоке `RobustIntegrationTest.ServerIntegrationInstance`, без игровых
  прототипов/игроков/production-конфига. Первый проверяет health; второй проверяет
  обработку серверных действий при зависшем IPC и отмену без возвращения черновика.

| Проверка | Фактический результат |
| --- | --- |
| `dotnet restore Content.Server/Content.Server.csproj --disable-parallel --verbosity minimal` вне sandbox инструмента | Успешно; прежний blocker restore снят |
| Content.Shared с `BuildProjectReferences=false` и существующими бинарниками зависимостей | 0 errors, 18 warnings, 3:16.62 |
| Content.Server с текущим Content.Shared, сохранёнными анализаторами/генераторами и существующими engine binaries | **0 errors, 64 warnings**, 40.83 s |
| Tools/AdminAI.Spike | 0 errors, 0 warnings |
| Tools/AdminAI.Tests | 0 errors; 2 NU1900 из недоступного NuGet audit при sandbox build |
| `dotnet test Tools/AdminAI.Tests/AdminAI.Tests.csproj --no-build --no-restore` с реальным UDS | **46 passed, 0 failed, 0 skipped**, около 2 s |
| Tools/AdminAI.ServerTests build | 0 errors, 0 warnings |
| `dotnet test Tools/AdminAI.ServerTests/AdminAI.ServerTests.csproj --no-build --no-restore` | **2 passed, 0 failed, 0 skipped**, 611 ms |
| `bash -n Tools/AdminAI.Spike/linux-smoke.sh` через Git Bash | Синтаксис корректен; это не Linux runtime test |

Сборка всего C#-кода Content.Shared/Content.Server выполнена без пересборки
RobustToolbox, через локальный ignored MSBuild helper
`.codex-build/admin-ai/stage-a-followup/frozen-references.targets`.
Он использует уже собранные зависимости/анализаторы и их runtime support files;
исходники движка не меняет, проверки анализаторов не отключает. Это **сборка
Content-проектов против существующих зависимостей**, не чистая сборка всего
графа с нуля. Полный набор Content.IntegrationTests не запускался.
Основные warnings относятся к существующим устаревшим API и NuGet audit
ImageSharp; обновление сторонних модулей в AI-коммит не включено.

Точные команды сборки Content-проектов в этом окружении:

```powershell
$frozen = 'C:\Users\ha13r\GitHub\ColonialMarinesUniverse\.codex-worktrees\rucmu-admin-ai-stage-a\.codex-build\admin-ai\stage-a-followup\frozen-references.targets'
$worktree = 'C:\Users\ha13r\GitHub\ColonialMarinesUniverse\.codex-worktrees\rucmu-admin-ai-stage-a'
$existing = 'C:\Users\ha13r\GitHub\ColonialMarinesUniverse'
dotnet build Content.Shared/Content.Shared.csproj --no-restore -p:BuildProjectReferences=false "-p:CustomAfterMicrosoftCommonTargets=$frozen" "-p:AdminAIWorktreeRoot=$worktree" "-p:AdminAIExistingRoot=$existing"
dotnet build Content.Server/Content.Server.csproj --no-restore -p:BuildProjectReferences=false "-p:CustomAfterMicrosoftCommonTargets=$frozen" "-p:AdminAIWorktreeRoot=$worktree" "-p:AdminAIExistingRoot=$existing"
```

Tools/AdminAI.ServerTests принимает пути `AdminAIContentServerDirectory`,
`AdminAIEngineAssemblyDirectory`, `AdminAIEngineResourceDirectory`.
Тестовые пути задаются через `RUCMU_AI_TEST_SOCKET` (короткий свободный .sock)
и `RUCMU_AI_ENGINE_RESOURCES` (настоящий RobustToolbox/Resources).
Настоящие ресурсы копируются в стандартный test output layout. Конфигурация
sandbox движка не меняется; игровые прототипы в этих двух focused-тестах
не загружаются. Reflection для обхода проверок не применяется.
Результаты/журналы: `.codex-build/admin-ai/stage-a-followup/`.

На этой машине WSL не установлен и Docker не найден; Linux/SSH-стенд не указан.
Для Linux подготовлен скрипт, использующий **уже собранные** переносимые артефакты:

```sh
bash Tools/AdminAI.Spike/linux-smoke.sh
```

Он требует Linux и .NET 10 runtime; ничего не скачивает. Проверяет selftest,
46 тестов протокола/IPC, отдельные процессы, отказ и повторный запуск worker,
а также серверный поток из AdminAI.ServerTests при наличии его артефакта.
Отсутствие артефакта/пропущенные проверки дают неуспех, а не фиктивную приёмку.
Runtime output с реальными ресурсами должен сохранять layout Tools/*/bin.
Отчёт содержит OS/runtime и результаты в `.codex-build/admin-ai/linux-smoke-*`.
Скрипт здесь не исполнялся на Linux; systemd/PSI/cgroups и реальный GGUF не проверены.

Для ручной health-проверки на отдельном Linux-стенде с worker и подготовленным
сокетом `/run/rucmu-ai/llama.sock`, из серверной консоли:

```text
cvar cmu.admin_ai.enabled true
admin_ai_probe
cvar cmu.admin_ai.enabled false
```

Это не отправляет ahelp и не запускает модель/systemd. При выключении CVar
до завершения запроса результат не показывается. Автоматического health polling
или inference coordinator на этапе A нет.

Изменения продолжения: серверный клиент/health-команда, 2 locale-файла,
серверные IPC-тесты, включение их mock-fixture в Content.IntegrationTests,
отдельный project AdminAI.ServerTests, новые fault-сценарии и Linux smoke-скрипт.
Прежний `Tools/AdminAI.Spike/UdsSpikeClient.cs` заменён серверным файлом.

## Файлы первоначального коммита этапа A

Все файлы новые; существующие системы ahelp, webhook, UI, SharedBwoinkSystem,
RobustToolbox и upstream-подмодули не редактировались.

```text
Content.CMU/Server/AdminAI/IAIInferenceClient.cs
Content.CMU/Server/AdminAI/AIInferenceOptions.cs
Content.CMU/Server/AdminAI/AIInferenceProtocol.cs
Content.CMU/Server/AdminAI/UnavailableAIInferenceClient.cs
Content.CMU/Server/AdminAI/AdminAICVars.cs
Content.IntegrationTests/_CMU14/AdminAI/AIInferenceTests.cs
Content.IntegrationTests/_CMU14/AdminAI/AdminAIConfigurationTests.cs
Tools/AdminAI.Spike/AdminAI.Spike.csproj
Tools/AdminAI.Spike/Directory.Packages.props
Tools/AdminAI.Spike/Program.cs
Tools/AdminAI.Spike/MockWorker.cs
Tools/AdminAI.Spike/UdsSpikeClient.cs
Tools/AdminAI.Tests/AdminAI.Tests.csproj
Tools/AdminAI.Tests/Directory.Packages.props
Tools/AdminAI.Tests/Program.cs
Tools/AdminAI.Tests/UdsSpikeTests.cs
Docs/rucmu-admin-ai-stage-a.md
```

## Что ещё требуется для Linux-приёмки A

1. На Linux проверить сборку с штатно подготовленными зависимостями. На Windows
   Content.Shared и Content.Server уже собраны с сохранением бинарников движка;
   46 протокольных/IPC тестов и 2 сфокусированных серверных теста прошли.
2. Выполнить реальный UDS round-trip на Linux без изменения разрешений sandbox
   и подтвердить допустимость запроса из Content.Server. При запрете engine
   sandbox зафиксировать ошибку и остановиться; не отключать его проверку.
3. Повторить подтверждённые на Windows отказ/таймаут/отмену/обрыв и перезапуск
   mock-worker на Linux. Скрипт `Tools/AdminAI.Spike/linux-smoke.sh` подготовлен.
4. По последующей команде пользователя продолжена подготовка B в отдельной
   ветке `codex/rucmu-admin-ai-stage-b`: см. `Docs/rucmu-admin-ai-stage-b.md`.
   Это не подтверждает Linux-приёмку A или возможность production-развёртывания.
   C (DraftOnly и права), D (логи и доказательства) остаются следующими этапами.
   Этап E требует отдельного ручного принятия предыдущих.

Нагрузка RuCMU + 3 Squad, Linux PSI/cgroups, лицензия/hash GGUF, качество
реальной модели, права UI и поведение при изменении раунда **не проверены**.
Production RuCMU и Squad не запускались/не перенастраивались, модель
не загружалась. Не заявляется production readiness или отсутствие влияния
на производительность на основании этих локальных тестов.

Откат: убрать этот отдельный коммит. Миграций БД, запущенных служб и изменений
production-конфигурации нет.

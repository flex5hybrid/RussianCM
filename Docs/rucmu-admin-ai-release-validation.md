# RuCMU AI Admin Assistant: протокол проверки перед PR

2026-10-14, `codex/rucmu-admin-ai-safe-faq`, база `origin/master` = `02084d038c9`.
Основной checkout и его несвязанные изменения не включены. Проверки выполнялись
в `.codex-worktrees/rucmu-admin-ai-stage-a` и отдельном Linux-снимке.

## Результат

| Проверка | Фактический результат |
| --- | --- |
| Boundary, конфигурация, knowledge, доказательства, политика, гонки | 119 passed / 0 failed / 0 skipped |
| Mock UDS: timeout, cancellation, worker faults, ограниченный парсинг | 12 / 0 / 0 |
| Compiled Content.Server: handler permissions, private delivery, FAQ, clarification, bounded DB | 33 / 0 / 0 |
| Те же compiled серверные тесты с Linux DLL, Ubuntu | 33 / 0 / 0 |
| Compiled Client UI: вставка, expiry, новая выборка, readonly evidence, sandbox | 14 / 0 / 0 |
| Повторный прогон штатного Content.IntegrationTests по namespace AdminAI | 135 / 0 / 0, 4 min 21 s; пересекается с focused тестами |
| Content.Shared, Windows | 0 errors / 18 warnings |
| Content.Server, Windows | 0 errors / 67 warnings |
| Content.Client, Windows, штатная XAML-компиляция | 0 errors / 53 warnings |
| Content.Shared, TargetOS Linux, отдельный снимок | 0 errors / 18 warnings |
| Content.Server, TargetOS Linux, отдельный снимок | 0 errors / 67 warnings |
| Новый обычный headless Windows-клиент → обновлённый WSL Linux-сервер | InGame, serializer/transfer handshake completed |
| Штатный sandbox этого настоящего клиента | Client, Shared, Shared.Database verified |
| Реальный сервер после подключения | 259 документов / 5728 фрагментов, worker Ready, governor Normal |
| Настоящая модель, итоговый worker, production UDS-клиент | Ready / Success, первый запрос 12,07 s, прогретый 4,89 s |
| Worker при этом запросе | CPU quota 1 CPU, RAM max 4 GiB, AF_UNIX only, PrivateNetwork yes, NRestarts 0 |
| Memory events до/после итогового запроса | high/max/oom/oom_kill = 0; наблюдаемая память 2,42 GB |
| Source diff и status подмодулей | RobustToolbox и RSI.NET без изменений |

**178 различных автоматических тестов**, 33 серверных дополнительно повторены
на Linux. Игровая логика в обычном headless-клиенте и сервере загружалась полностью;
соединение реальное, но сообщения ахелпа в этом smoke не отправлялись. Новые
автоответы/отчёты проверены compiled fixtures с управляемыми sessions/services/DB,
не полным игровым сетевым E2E. Ручной обычный DraftOnly ранее принят пользователем.

Дополнение после пользовательского timeout на «Привет помогите»: 3 новые
регрессии поиска, boundary теперь 122/0/0; прежние 33 compiled server tests
повторены успешно. Всего 181 различных тестов с учётом неизменённых ранее
проверенных наборов. Windows/Linux Content.Server пересобраны: 0 errors / 67 warnings.
GPU-генерация реальной модели на итоговом q8/512: две серии по 30/30 Success
с двумя клиентами InGame, p95 0,60/0,62 s, max 0,61/0,66 s. Третья серия
30/30 с почти максимальным штатным входом (3975/4096 символов): p95 0,69 s,
max 0,80 s. Измерение модели + UDS не включает игровой handler/очередь/сеть/UI;
ручной E2E нового GPU-профиля пока не подтверждён. Прежний FP16 с конкурирующей
нагрузкой давал timeout 45 s и ответ 26,68 s; этот отрицательный результат
сохранён. Условия и ограничения —
[отдельный протокол](rucmu-admin-ai-gpu-wsl.md).

Warnings включают существующие API, предупреждения анализаторов о будущих
требованиях к Dependency-классам и уязвимости закреплённого ImageSharp. Версии
зависимостей и проверки sandbox не менялись. При подключении тестового клиента
обнаружен существующий `TTS API token is empty`; TTS выключен только в локальном
тестовом запуске, настройки production и код TTS не изменены.

После первой проверки CI исправлены отсутствующие явные импорты `System.IO`/
`System.Threading` в linked mock fixture и синхронный доступ к `Task.Result` в
smoke очереди. Штатный Content.IntegrationTests собран с теми же frozen DLL:
0 errors / 89 warnings; 119 boundary-тестов повторены успешно. Зависимости
восстановлены штатным restore; исходники движка не менялись.

Дополнительный тест worker после перезапуска выявил 24,79 s и рост `memory.high`
с 5981 до 67856. Ограничение batch/ubatch до 128 и q8 KV-кеш сохранили 4096 context,
один CPU и лимиты 3/4 GiB, снизили наблюдаемую память с 3,22 до 2,42 GB. Итоговые
первый/повторный запросы: 12,07/4,89 s, без high/max/oom событий. Это отдельные
синтетические замеры; качество произвольных моделей и p95/p99 не установлены.

## Команды и артефакты

### Контекст и CPU: 2026-10-15

После передачи истории отдельными ролями и исправления поиска продолжений:
131 boundary / 12 mock UDS / 33 compiled server tests — 0 failed, 0 skipped.
Это 9 новых boundary-регрессий; неизменённые 14 client tests относятся к прежней
проверке, а не к повторному прогону этого изменения.

CPU-кандидаты 0.6B–1.7B не приняты: наиболее быстрый Qwen3 0.6B Q4 дал max
2.43 s, но ошибался по смыслу; итоговый эксперимент Qwen3 1.7B Q4 на 2 CPU дал
max 4.61 s и потерю контекста. Дообучения/новых весов нет. Модели не заменили
игровой worker. Полные условия, hashes, ограничения и воспроизводимый инструмент:
[CPU и контекст](rucmu-admin-ai-cpu-context.md).

Итоговый `AdminAI.DialogueBench` проверен на прежнем GPU 4B worker: 21/21
полных ответов, p95 0.43 s, max 0.51 s. При ручном чтении обнаружены ненужные
уточнения вместо шага осмотра и утверждение отсутствия неизвестной механики
без источника. Это успешная проверка транспорта/времени, **не принятие качества**.
Полный сетевой F1 и совместная нагрузка четырёх серверов не проверены.

Required Checks на head `e06d83848b7` завершился failure; Build и Asset Validation
прошли. Ранее сравнивавшийся baseline описан ниже, не все новые игровые группы
сравнены с ним. CI следующего commit должен проверяться отдельно.

Контент собран в Debug с уже существующими соответствующими платформе DLL движка,
всеми анализаторами и штатным XAML task. Для Windows использовались параметры:

```text
dotnet build Content.Shared/Content.Shared.csproj --no-restore
dotnet build Content.Server/Content.Server.csproj --no-restore
dotnet build Content.Client/Content.Client.csproj --no-restore
```

К каждой команде добавлены `BuildProjectReferences=false`, локальный
`CustomAfterMicrosoftCommonTargets=.../stage-c/frozen-references.targets`,
`AdminAIWorktreeRoot`, `AdminAIExistingRoot`, `_RobustForceInternalMSBuild=true`
и `-nr:false`. Локальный targets перенаправляет отсутствующие reference/analyzer
paths на уже построенные DLL; не исключает анализаторы и не изменяет исходники
движка. Для Client указан штатный `CompileRobustXamlTaskAssemblyFile`.
Обычная воспроизводимая сборка при наличии SDK/зависимостей — `dotnet build`
из checkout; локальные frozen-параметры не входят в продукт.

Linux content собирался аналогично из отдельного снимка с `TargetOS=Linux`.
Его движок ранее собран обычным способом по отдельному разрешению пользователя,
без правок исходников, sandbox или версии. Linux runner исполнялся настоящим
Linux .NET в Ubuntu; Windows engine DLL для него не подставлялись.

Focused runner команды и параметры DLL/resource paths описаны в
[инструкции](rucmu-admin-ai-release.md). Перед каждым runner записывался его
отдельный NUnit XML, чтобы общий путь `ipc-nunit.xml` не потерял результат.
Файлы локального доказательства:

```text
.codex-build/admin-ai/release-boundary-{build,tests}.log
.codex-build/admin-ai/release-boundary-nunit.xml
.codex-build/admin-ai/release-mock-{tests.log,nunit.xml}
.codex-build/admin-ai/release-server-{build,tests,tests-build}.log
.codex-build/admin-ai/release-server-nunit.xml
.codex-build/admin-ai/release-client-{build,tests,tests-build}.log
.codex-build/admin-ai/release-client-nunit.xml
.codex-build/admin-ai/release-full-integration-tests.log
.codex-build/admin-ai/full-integration-results/admin-ai-release.trx
.codex-build/admin-ai/release-linux-{shared,server}-build.log
.codex-build/admin-ai/stage-wsl/server-linux-{tests.log,nunit.xml}
.codex-build/admin-ai/release-real-model-smoke.log
.codex-build/admin-ai/release-worker-{limits,memory-before,memory-after}.log
.codex-build/admin-ai/release-benchmark-results.log
```

Performance comparison: прежний код из `4cb8e5896f4` и новый код, один корпус,
одна Release-сборка измерителя, 100 warmup + 3000 запросов на режим. Полнотекстовый
p95 0,7025 → 0,0165 ms; типовые FAQ p95 0,1181 → 0,0179 ms. Количество полученных
фрагментов совпало. Это синтетические lookup-замеры, не latency сети/модели и
не точность ответов на весь спектр механик.

Проверены регрессии: адресная очистка вытесненного кейса без сброса новой выборки,
сохранение полного ID/SHA256 источника после очистки разметки, обязательная оговорка
о недостатке данных при минимальном бюджете и ограниченная высота вкладки контекста.

## Границы приёмки

- Production-хост RuCMU и три Squad не предоставлены. CPU placement, реальные
  tick/TPS/p95/p99 и поведение под совместной нагрузкой не измерены.
- Работоспособность и скорость всех механик не подтверждены: возвращаются только
  найденные публичные источники, неизвестное не выдаётся за факт.
- Исторические кейсы, заметки, внутренние инструкции и модельные выводы о мотивах
  не реализованы и не маскируются сгенерированными данными. Текущий bounded report
  показывает фактическую выборку и недостаток информации.
- Все режимы отключены по умолчанию. PR не развёртывает систему в production;
  включение требует описанной эксплуатационной приёмки.

## Проверенные версии подмодулей

```text
RobustToolbox 1eee1d78e4f9b091f533191639606d1d34deb42a
RSI.NET      3eead16bbade2214f4e0c4dd07a845051c24b075
```

Полный список изменённых файлов — ниже; бинарные артефакты, модель, локальная БД,
игровые обращения и несвязанные `BuildChecker` каталоги исключены.

- `Content.CMU/Client/AdminAI/AdminAIDraftControl.Cases.cs`
- `Content.CMU/Client/AdminAI/AdminAIDraftControl.cs`
- `Content.CMU/Resources/Locale/en-US/CMU14/admin-ai.ftl`
- `Content.CMU/Resources/Locale/ru-RU/CMU14/admin-ai.ftl`
- `Content.CMU/Resources/ServerInfo/Guidebook/CMU14/Comms/AU14CommsVoiceProcedure.xml`
- `Content.CMU/Server/AdminAI/AICaseAnalyzer.cs`
- `Content.CMU/Server/AdminAI/AIContextCollector.cs`
- `Content.CMU/Server/AdminAI/AIDraftSessionStore.cs`
- `Content.CMU/Server/AdminAI/AIInferenceOptions.cs`
- `Content.CMU/Server/AdminAI/AIInferenceProtocol.cs`
- `Content.CMU/Server/AdminAI/AIRequestQueue.cs`
- `Content.CMU/Server/AdminAI/AIResourceGovernor.cs`
- `Content.CMU/Server/AdminAI/AIResponsePolicy.cs`
- `Content.CMU/Server/AdminAI/AIRuleKnowledgeBase.cs`
- `Content.CMU/Server/AdminAI/AISafeFaqSessionStore.cs`
- `Content.CMU/Server/AdminAI/AdminAICVars.cs`
- `Content.CMU/Server/AdminAI/AdminAIDraftSystem.Cases.cs`
- `Content.CMU/Server/AdminAI/AdminAIDraftSystem.cs`
- `Content.CMU/Server/AdminAI/AdminAIProbeCommand.cs`
- `Content.CMU/Server/AdminAI/AdminAIRoundContext.cs`
- `Content.CMU/Server/AdminAI/AdminAISafeFaqSystem.cs`
- `Content.CMU/Server/AdminAI/AdminAIService.cs`
- `Content.CMU/Server/AdminAI/AdminAIStatusCommand.cs`
- `Content.CMU/Server/AdminAI/GameTicker.AdminAI.cs`
- `Content.CMU/Server/AdminAI/IAIInferenceClient.cs`
- `Content.CMU/Server/AdminAI/LinuxAIPressureReader.cs`
- `Content.CMU/Server/AdminAI/UdsAIInferenceClient.cs`
- `Content.CMU/Server/AdminAI/UnavailableAIInferenceClient.cs`
- `Content.CMU/Shared/AdminAI/AdminAICaseMessages.cs`
- `Content.CMU/Shared/AdminAI/AdminAIDraftMessages.cs`
- `Content.CMU/Shared/AdminAI/AdminAIDraftText.cs`
- `Content.Client/Administration/Systems/BwoinkSystem.cs`
- `Content.Client/Administration/UI/Bwoink/BwoinkControl.xaml`
- `Content.Client/Administration/UI/Bwoink/BwoinkControl.xaml.cs`
- `Content.Client/Administration/UI/Bwoink/BwoinkPanel.xaml.cs`
- `Content.Client/UserInterface/Systems/Bwoink/AHelpUIController.cs`
- `Content.IntegrationTests/Content.IntegrationTests.csproj`
- `Content.IntegrationTests/_CMU14/AdminAI/AICaseEvidenceTests.cs`
- `Content.IntegrationTests/_CMU14/AdminAI/AIDraftSecurityTests.cs`
- `Content.IntegrationTests/_CMU14/AdminAI/AIInferenceTests.cs`
- `Content.IntegrationTests/_CMU14/AdminAI/AIInfrastructureTests.cs`
- `Content.IntegrationTests/_CMU14/AdminAI/AISafeFaqTests.cs`
- `Content.IntegrationTests/_CMU14/AdminAI/AdminAIClientUiTests.cs`
- `Content.IntegrationTests/_CMU14/AdminAI/AdminAIConfigurationTests.cs`
- `Content.IntegrationTests/_CMU14/AdminAI/AdminAIServerIpcTests.cs`
- `Content.IntegrationTests/_CMU14/AdminAI/AdminAITestPaths.cs`
- `Content.Server/Administration/Systems/BwoinkSystem.cs`
- `Content.Server/Entry/EntryPoint.cs`
- `Content.Server/IoC/ServerContentIoC.cs`
- `Docs/rucmu-admin-ai-gpu-wsl.md`
- `Docs/rucmu-admin-ai-local-wsl.md`
- `Docs/rucmu-admin-ai-release-validation.md`
- `Docs/rucmu-admin-ai-release.md`
- `Docs/rucmu-admin-ai-safe-faq.md`
- `Docs/rucmu-admin-ai-stage-a.md`
- `Docs/rucmu-admin-ai-stage-b.md`
- `Docs/rucmu-admin-ai-stage-c.md`
- `Tools/AdminAI.ClientTests/AdminAI.ClientTests.csproj`
- `Tools/AdminAI.ClientTests/Directory.Packages.props`
- `Tools/AdminAI.Local/Start-TestClient.ps1`
- `Tools/AdminAI.ServerTests/AdminAI.ServerTests.csproj`
- `Tools/AdminAI.ServerTests/AdminAICaseCoordinatorTests.cs`
- `Tools/AdminAI.ServerTests/AdminAIDraftCoordinatorTests.cs`
- `Tools/AdminAI.ServerTests/AdminAISafeFaqCoordinatorTests.cs`
- `Tools/AdminAI.ServerTests/Directory.Packages.props`
- `Tools/AdminAI.Spike/AdminAI.Spike.csproj`
- `Tools/AdminAI.Spike/Directory.Packages.props`
- `Tools/AdminAI.Spike/MockWorker.cs`
- `Tools/AdminAI.Spike/Program.cs`
- `Tools/AdminAI.Spike/linux-smoke.sh`
- `Tools/AdminAI.Tests/AdminAI.Tests.csproj`
- `Tools/AdminAI.Tests/Directory.Packages.props`
- `Tools/AdminAI.Tests/Program.cs`
- `Tools/AdminAI.Tests/UdsSpikeTests.cs`
- `deploy/linux/admin-ai.toml.example`
- `deploy/linux/audit-game-placement.sh`
- `deploy/linux/rucmu-ai-worker-cuda-wsl-test.service.example`
- `deploy/linux/rucmu-ai-worker.service`
- `deploy/linux/run-local-ai-test.sh`
- `deploy/linux/worker-cuda-wsl-test-preflight.sh`
- `deploy/linux/worker-cuda-wsl-test.env.example`
- `deploy/linux/worker-preflight.sh`
- `deploy/linux/worker.env.example`

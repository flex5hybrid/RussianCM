# RuCMU AI Admin Assistant — DraftOnly в ahelp

> Исторический протокол этапа. Актуальная поставка и ограничения: [инструкция релиза](rucmu-admin-ai-release.md).

Ветка `codex/rucmu-admin-ai-stage-c`, база — этап B `747481a6667`.
Реализован ручной приватный помощник в существующем административном ahelp.
При первоначальной проверке этапа C реальная модель и Linux-хост ещё не были
предоставлены; приведённые ниже UI/mock результаты не являются production-приёмкой.
Позднее по разрешению пользователя подготовлен локальный WSL2 с настоящим GGUF:
[артефакты, Linux smoke и ручной сценарий](rucmu-admin-ai-local-wsl.md).

## Как пользоваться

1. На целевом Linux-хосте подготовить локальный llama-server/GGUF по
   [инструкции B](rucmu-admin-ai-stage-b.md): отдельный процесс, UDS,
   cgroup-лимиты, CPU-only, без сети. Сначала проверить Linux acceptance A/B
   и placement/baseline RuCMU + трёх Squad. Бинарник и модель не скачиваются игрой.
2. В конфигурации RuCMU включить `cmu.admin_ai.enabled=true`; socket по умолчанию
   `/run/rucmu-ai/llama.sock`. Governor оставить включённым. Начальное Paused
   требует достоверных PSI/tick метрик и двух устойчивых окон восстановления
   (по умолчанию 60 секунд суммарно). Проверить `admin_ai_status`/`admin_ai_probe`.
3. Получить новое сообщение игрока в ahelp **после включения** помощника.
   Переписка до включения не импортируется из клиентского окна или Discord:
   сервер не доверяет присланной клиентом истории и не восстанавливает её из
   закрытых административных данных.
4. Администратор с Adminhelp выбирает игрока и разворачивает
   «ИИ-помощник — приватный черновик». Нажимает «Подготовить»; при первой индексации
   опубликованных правил может понадобиться подождать и повторить запрос.
5. Прочитать и отредактировать черновик. «Источники» показывает реальные фрагменты,
   SHA256 и кнопку открытия соответствующего guideEntry. Наличие источника
   не подтверждает факт произошедшего и не гарантирует правильность ответа модели.
6. «Вставить» добавляет текст в штатное поле ahelp, сохраняя набранное оператором.
   Само сообщение не отправляется: отправка остаётся обычным действием администратора
   (в текущем интерфейсе — Enter). «Заново» запускает новый запрос, «Отмена» отменяет
   текущий. Закрытие окна, переключение игрока и отзыв прав очищают приватный результат.

## Серверная граница и безопасность

- В BwoinkSystem добавлен **локальный серверный** accepted event после успешных
  штатных authorization/rate-limit checks и до асинхронного форматирования.
  Исходная доставка игроку, администраторам, звуки, admin-only, webhook и поведение
  без админов сохранены. Observer только записывает ограниченный контекст;
  получение обычного сообщения не запускает генерацию.
- Новый case ID создаётся сервером только для принятого публичного сообщения
  игрока. Каждый принятый текст получает отдельный ID; GUID клиента/TrueSender
  не используются как доказательство принадлежности обращения. Новое неизвестное
  обращение нельзя создать для ИИ одним админским сообщением.
- Каждый Open/Prepare/Refresh/Cancel/Close проверяет Adminhelp на сервере,
  authenticated session, известный текущий канал и выданный этому администратору
  view ID. Request не содержит owner ID, свободный prompt, логи или заметки.
  AI RPC используют отдельную rate-limit bucket и не расходуют обычную ahelp квоту.
- Контекст содержит только публичные сообщения текущего обращения и локальные
  опубликованные Rules/SOP. Admin-only не передаются модели; логи, заметки и БД
  в C не читаются. Поэтому Adminhelp без Logs/ViewNotes не получает привилегированных
  данных. «Собрать контекст» и case reports остаются этапом D; фиктивной кнопки нет.
- Перед выдачей ответа заново проверяются права, тот же объект session, round ID,
  case ID, view ID, request ID, revision и epoch. Поздние ответы после нового
  сообщения/выбора/закрытия/смены раунда/config reload не выдаются. Revoke/de-admin
  также очищают клиентскую приватную область. `IAdminAIRoundContext` даёт только
  номер раунда, не управление раундом.
- Ответ уходит **одному** администратору через отдельное сетевое событие, никогда
  через BwoinkTextMessage, broadcast или webhook. Вставка не вызывает Send/команду.
  Нет ban/kick/warn/mute/freeze/respawn, закрытия жалобы или изменения игровых сущностей.
- Результат — недоверенный текст в обычном TextEdit. Разметочные скобки заменяются,
  HTTP/WWW ссылки убираются, управляющие/bidi символы фильтруются, длина ограничена.
  Кнопки источников строятся только из серверного индекса. Истечение draft TTL
  запрещает вставку и очищает editor; периодический серверный статус не перезаписывает
  правки оператора, если исходный черновик не изменился.
- Prompt фиксирует русский язык, отсутствие решений о виновности/наказаниях и
  запрет выдуманных фактов. Для совместимых шаблонов llama передаётся
  `chat_template_kwargs.enable_thinking=false`; `<think>` в content отвергается.
  [Официальный формат llama-server](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md).
  Это инструкция модели и защита структуры, не гарантия семантического качества.
  Клиент/сервер не предоставляют модели tools, файловые или игровые API.

## Пределы контекста и знаний

Все новые CVar server-only, archived, без репликации. Изменение настроек очищает
view/context и отменяет запросы; выключение не требует остановки игры.

| CVar после `cmu.admin_ai.` | Default | Допустимые пределы |
| --- | --- | --- |
| max_conversations | 128 | 1..512 |
| max_private_views | 32 | 1..64, один view на администратора |
| context_messages | 8 | 1..16 |
| context_message_chars | 512 | 64..1024 |
| context_ttl_seconds | 300 | 30..600 |
| draft_ttl_seconds | 120 | 15..300 |
| draft_cooldown_seconds | 5 | 2..60, сохраняется при смене/закрытии view |

При заполнении контекстов самый старый вытесняется с отменой привязанных запросов.
Ссылки на тексты и sources очищаются при expiry/round reset/close/disable/revoke.
Приватная область UI создана один раз на окно, а не на каждого игрока в roster.
В базе данных новая история не хранится.

Индекс неизменяемый и строится в фоновой задаче из реальных guideEntry prototypes:
до 64 XML только в `/ServerInfo/Guidebook/CMU14/Rules/` и `/SOP/`, до 65536 байт
на документ и 2048 фрагментов. DTD/внешние XML entities запрещены. Версия — SHA256
исходных байтов; выдаётся до 3 фрагментов по 480 символов. Поиск лексический,
без embeddings/внешних сервисов/выдуманного процента уверенности. При отсутствии
релевантного источника ручной черновик может предложить уточнение; SafeFAQ не включён.
Prototype reload сбрасывает индекс и текущие views.

Технический аудит содержит actor ID, case/round/request ID, mode, статус, актуальность
и IDs найденных источников; без полного prompt/ответа/приватной переписки. Инициатор
берётся из server session. Полный аудит санкций и доказательств не реализуется этим MVP.

## Проверки 2026-10-13

| Проверка | Результат |
| --- | --- |
| Content.Shared build с frozen dependencies | 0 errors, 18 warnings |
| Content.Server build с текущим Shared | 0 errors, 67 warnings |
| Content.Client build + штатная XAML-компиляция | 0 errors, 53 warnings |
| Boundary, очередь/governor, DraftOnly безопасность, знания/санитизация | 93 passed, 0 failed, 0 skipped |
| Реальные UDS/fault tests | 12 passed, 0 failed, 0 skipped |
| Настоящий Content.Server: транспорт/очередь + coordinator с тестовыми sessions/inference | 6 passed, 0 failed, 0 skipped |
| Настоящий headless Content.Client UI, XAML структура и неизменённый engine sandbox | 8 passed, 0 failed, 0 skipped |

Итого **119 тестов**. UI проверен на ширине chat pane 360/460, RU/EN; вставка
сохраняет operator text и не отправляет ahelp/RPC. Проверены старый selection,
expired draft, forged channel/owner/view, смена раунда/revoke/close, cancel, TTL,
cooldown и приватность. Coordinator тесты используют настоящий handler и настоящий
файл общих правил, scoped IoC с mock sessions/prototype metadata и fake inference;
интернет/модель/production не участвуют. Исходники этих тестов доступны в Tools.

На первом sandbox запуске компилятор создал запрещённый `CollectionsMarshal.SetCount`
из короткого `OpenHelp([source.GuideId])`. Заменён только наш callback обычным
List initializer; проверка всего Shared/Client через штатный CheckSandboxed прошла.
Никакого изменения whitelist, skipVerify или отключения sandbox.

Использованы существующие DLL/анализаторы/генераторы RobustToolbox. Для XAML
указан существующий `CompileRobustXamlTaskAssemblyFile` из основного checkout и
предусмотренный `_RobustForceInternalMSBuild=true`; XAML-компиляция не отключалась.
Focused UI harness переиспользует matching native files (freetype6/zlib и др.) игры.
Это Content сборка против existing dependencies, не чистая сборка всего графа.
Warnings относятся к существующим deprecated API, dependency annotations и
NuGet audit ImageSharp. Full game integration suite и реальные четыре сервера
не запускались; существующий ahelp/Discord relay требует дополнительного live E2E.

Результаты и команды текущей проверки сохранены в ignored `.codex-build/admin-ai/stage-c/`.
После сборки focused tools запуск:

```powershell
dotnet Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll boundary
$env:RUCMU_AI_TEST_SOCKET = Join-Path $env:TEMP ('rucmu-ai-' + [guid]::NewGuid().ToString('N').Substring(0,8) + '.sock')
dotnet Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll ipc
$env:RUCMU_AI_ENGINE_RESOURCES = Join-Path (Get-Location) 'RobustToolbox/Resources'
$env:RUCMU_AI_CONTENT_RESOURCES = Join-Path (Get-Location) 'Content.CMU/Resources'
dotnet Tools/AdminAI.ServerTests/bin/Debug/net10.0/Content.IntegrationTests.dll ipc
dotnet Tools/AdminAI.ClientTests/bin/Debug/net10.0/Content.IntegrationTests.dll ipc
```

Разные IPC runners пишут один `.codex-build/admin-ai/ipc-nunit.xml`; сохранять его
после каждого запуска. Native directory для ClientTests соответствует платформе
существующих engine binaries. На Linux повторить A/B acceptance и эти тесты с
правильными native/runtime артефактами. Запуск реального GGUF, качество ответов,
системные лимиты и сравнение TPS/tick p95/p99/latency Squad остаются **непроверенными**.
Без этого нельзя объявлять систему полностью принятой для production.

Следующий функциональный этап D — ограниченные log queries, case report и доказательства
с отдельными Logs permissions. E (автоматические FAQ/уточнения) требует ручной приёмки
предыдущих этапов и отдельного включения; текущий ИИ ничего не отправляет самостоятельно.

## Изменённые файлы

- `Content.CMU/Server/AdminAI/`: `AdminAIDraftSystem.cs`, `AIDraftSessionStore.cs`,
  `AIRuleKnowledgeBase.cs`, `AdminAIRoundContext.cs`, `AdminAICVars.cs`, `AIInferenceProtocol.cs`.
- `Content.Shared/Administration/AdminAI/`: `AdminAIDraftMessages.cs`, `AdminAIDraftText.cs`.
- `Content.CMU/Client/AdminAI/AdminAIDraftControl.cs`; client `BwoinkSystem.cs`,
  `BwoinkControl.xaml/.cs`, `BwoinkPanel.xaml.cs`, `AHelpUIController.cs`.
- Server `Administration/Systems/BwoinkSystem.cs`, `IoC/ServerContentIoC.cs`.
- `Content.CMU/Resources/Locale/{en-US,ru-RU}/CMU14/admin-ai.ftl`.
- Integration `AIInferenceTests.cs`, `AIDraftSecurityTests.cs`, `AdminAIClientUiTests.cs`;
  `Tools/AdminAI.Tests/AdminAI.Tests.csproj`, `Tools/AdminAI.ServerTests/` coordinator
  tests/csproj и новый focused `Tools/AdminAI.ClientTests/`.
- `Docs/rucmu-admin-ai-stage-c.md`.

RobustToolbox и RSI.NET чистые; SHA256 Robust.Shared/Server/Client/Shared.Maths DLL
совпадают с исходными. Изменения игры сохранены отдельно от посторонних файлов.

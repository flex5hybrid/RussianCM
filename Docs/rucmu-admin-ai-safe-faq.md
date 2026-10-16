# RuCMU AI: быстрые знания и SafeFAQ без администрации

> Исторический протокол этапа. Актуальная поставка и ограничения: [инструкция релиза](rucmu-admin-ai-release.md).

Проверка 2026-10-14. Ветка `codex/rucmu-admin-ai-safe-faq`, база `ecb6ceb64d1`.
Пользователь отдельно запросил ускорение, знания по механикам и автоответы без
администраторов после ручного принятия DraftOnly. Поэтому SafeFAQ выполнен сейчас;
этап D (анализ логов/доказательства) и автоматические уточнения не реализованы.

## Поведение

- Изначально `cmu.admin_ai.enabled=false`, `cmu.admin_ai.safe_faq=false`.
- SafeFAQ включается отдельно. Автоматическая справка возможна только без любого
  подключённого администратора. `AllAdmins` включает скрытых, AFK и de-adminned staff;
  их имена/статусы игроку не сообщаются.
- Требуются исправная инфраструктура и governor Normal. Пороги, гистерезис,
  один слот модели, CPU/RAM quotas и Unix-сокет не ослаблены.
- Быстрая справка берёт реальные выдержки из документов; непроверенный сгенерированный
  текст автоматически не отправляется. Это поиск/извлечение, а не дообучение GGUF.
- Неизвестная механика, спор/санкции/инъекция/закрытые данные остаются в обычном ahelp
  без решения от помощника. Обращение не закрывается. Выдержки не разрешают конкретную
  боевую ситуацию и не заменяют приказы командования.
- Ответ явно помечен как автоматическая справка и показывает название руководства
  либо публичное состояние раунда. Ссылки/разметка не берутся из модели или игрока.
- FAQ запускается только после штатной доставки принятого ahelp. Исходная авторизация,
  rate limit, admin-only и подготовка существующего Discord relay сохранены.
- Перед отправкой повторяются проверки режима, governor, worker, отсутствия staff,
  того же объекта session, раунда, текущего request ID и TTL. Новое сообщение,
  администратор, переподключение, смена раунда/config/prototypes отменяют старый ответ.
- Ограничения FAQ: максимум 8 физических фоновых задач, 128 записей, один запрос на
  игрока за 5 секунд, минимум 500 ms между запросами сервера, TTL результата 5 секунд.
  Поиск/парсинг вне игрового тика; главный поток только проверяет состояние и доставляет.

## Источники и скорость

Whitelist расширен с Rules/SOP до опубликованных CMU14 и `_RuCM/GOVFOR` guideEntry.
Документы с заметками/логами/произвольными путями не импортируются. Не читаются БД,
секретные игровые сущности, произвольные файлы или интернет. Индекс обновляется при
prototype reload, строится в фоне: максимум 384 документов, 65536 bytes на файл,
8192 фрагментов по 600 символов. DTD/внешние entities запрещены.

Проверены **259 документов, 5728 фрагментов**, включая медицину, связь, инженерию,
исследования, снабжение/учебные документы, правила и SOP. Русские окончания и небольшие
словарные алиасы влияют только на поиск. Ранжирование учитывает частоту термина
и раздел документа. Совпадение одного общего слова не является процентом уверенности.
FAQ требует содержательного совпадения; неизвестное свойство предмета нельзя подтвердить
совпадением только его названия. Общие вопросы имеют явные маршруты к реальным документам.

| Вопрос пользователя | Действительный источник |
| --- | --- |
| Как вылечить морпеха? | CMUGuideMedicalInfantry: осмотр ран, гемостатическая марля, мазь |
| Как пользоваться радио? | AU14CommsHeadsets: `:h`, установленный ключ, необходимость ретранслятора |
| Какие правила открытия огня? | AU14SOPRulesofEngagement: опубликованное применение силы вместе с оговоркой об изменении СОП командованием |
| На какой карте мы сейчас играем? | Read-only данные GameTicker, те же подписи планеты/кораблей, что в публичном UI. В лобби прямо говорится, что раунд ещё не идёт |

Индекс: 147.99 ms. На 300 фиксированных синтетических запросах по трём вопросам
lookup p95 **0.266 ms**, максимум **0.334 ms**. Это время поиска в готовом индексе,
не сетевой latency, не генерация модели и не benchmark четырёх игровых серверов.
Во время параллельной сборки замеры достигали p95 8 ms / max 20.5 ms.

Ручные черновики используют тот же быстрый маршрут по открытым источникам и публичной
карте, с прежними приватными permission/session/view/revision/TTL проверками.
Остальные черновики идут в настоящую модель: prompt до 2300 символов, история до 800,
поиск по последнему вопросу игрока, без служебных SHA256 в prompt. Новый server-only
`cmu.admin_ai.draft_output_tokens=128`, допустимо 32..256; Degraded ограничивает вывод
прежним бюджетом. Более сложные генерации по-прежнему могут достигать timeout.

При импорте найдена существующая ошибка `AU14CommsVoiceProcedure.xml`: отсутствовал
открывающий `<Table>` перед закрывающим тегом. Восстановлены обёртка и заголовки;
сами определения процедурных слов сохранены. XML теперь проходит строгий парсер,
ни один из 259 документов не отвергнут. Таблицы сохраняют связь заголовок/значение,
textlink сохраняет видимую подпись, GuideEntityEmbed — подпись предмета.

## Проверки

| Проверка | Результат |
| --- | --- |
| Boundary/security/knowledge, реальный corpus, классификация и races | 112 passed, 0 failed, 0 skipped |
| Реальные mock UDS/fault tests | 12 passed, 0 failed, 0 skipped |
| Compiled Content.Server, Windows, включая 13 новых FAQ сценариев | 19 passed, 0 failed, 0 skipped |
| Те же compiled Content.Server тесты, Linux | 19 passed, 0 failed, 0 skipped |
| Неизменённый Client UI, вставка/expiry/retry, штатный engine sandbox | 10 passed, 0 failed, 0 skipped |
| Windows Content.Server против существующих engine DLL | 0 errors, 68 warnings |
| Linux Content.Server, отдельный снимок, существующие Linux engine DLL | 0 errors, 68 warnings |

**153 различных теста**; 19 серверных дополнительно повторены на Linux. Нативный
сервер запущен в разрешённом локальном WSL. Сам сервер подтвердил загрузку 259/5728,
worker Ready и governor Normal. Это не подтверждение production-нагрузки RuCMU/Squad.
Ручная проверка автоматических ответов в обычном клиенте пока ожидает пользователя.

Warnings относятся к существующим API/analyzers и ImageSharp audit. Исходники,
sandbox/API и версии RobustToolbox/RSI.NET не изменены. Бинарные артефакты, тестовая БД,
логи и модель не включаются в коммит. Нет новых AI API, загрузок при build/start/game,
полномочий на санкции или новых сообщений внешним сервисам.

## Воспроизводимые проверки

Из корня рабочей ветки:

```powershell
dotnet build Tools/AdminAI.Tests/AdminAI.Tests.csproj --no-restore -p:AdminAIEngineAssemblyDirectory=C:\Users\ha13r\GitHub\ColonialMarinesUniverse\bin\Content.IntegrationTests
dotnet Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll boundary
$env:RUCMU_AI_TEST_SOCKET = Join-Path $env:TEMP ('rucmu-ai-' + [guid]::NewGuid().ToString('N').Substring(0,8) + '.sock')
dotnet Tools/AdminAI.Tests/bin/Debug/net10.0/AdminAI.Tests.dll ipc
$env:RUCMU_AI_ENGINE_RESOURCES = Join-Path $PWD 'RobustToolbox/Resources'
$env:RUCMU_AI_CONTENT_RESOURCES = Join-Path $PWD 'Content.CMU/Resources'
dotnet Tools/AdminAI.ServerTests/bin/Debug/net10.0/Content.IntegrationTests.dll ipc
dotnet Tools/AdminAI.ClientTests/bin/Debug/net10.0/Content.IntegrationTests.dll ipc
```

ServerTests предварительно собирается с `AdminAIContentServerDirectory`,
`AdminAIEngineAssemblyDirectory` и `AdminAIEngineResourceDirectory`, указывающими
на соответствующие платформе DLL/ресурсы. Linux runner использует отдельные Linux DLL.
Команды Content build и фактические журналы/NUnit XML сохранены в ignored
`.codex-build/admin-ai/safe-faq-*`; Linux runner — `stage-wsl/server-linux-*`.

## Локальная ручная проверка

`run-local-ai-test.sh` явно включает SafeFAQ только на локальном loopback сервере.
Production defaults остаются выключенными. Запущен обычный AIPlayerTest, админ-клиент
для первой проверки закрыт. Отправить четыре вопроса по одному с интервалом от 5 секунд;
проверить автоматическую пометку, источник и правильное указание лобби/карты.
Затем подключить администратора с правами и повторить вопрос: автоматического ответа
быть не должно; ручной DraftOnly должен продолжить работать. Повторить неизвестный
вопрос/жалобу: обычный ahelp сохраняется, помощник не принимает решение.

## Изменённые файлы

- `Content.CMU/Server/AdminAI/AIRuleKnowledgeBase.cs`, `AIResponsePolicy.cs`: публичный индекс и политика выдачи.
- `AISafeFaqSessionStore.cs`, `AdminAISafeFaqSystem.cs`: ограниченные запросы и серверная доставка FAQ.
- `AIDraftSessionStore.cs`, `AdminAIDraftSystem.cs`: последний вопрос и быстрый приватный черновик.
- `AdminAICVars.cs`, `AdminAIService.cs`: opt-in и бюджет вывода/диагностика режима.
- `AdminAIRoundContext.cs`, `Content.Server/GameTicking/GameTicker.AdminAI.cs`: только публичные подписи карты.
- `Content.Server/Administration/Systems/BwoinkSystem.cs`: событие после штатной доставки.
- `Content.CMU/Resources/Locale/{en-US,ru-RU}/CMU14/admin-ai.ftl`: автоматическая пометка и контекст карты.
- `Content.CMU/Resources/ServerInfo/Guidebook/CMU14/Comms/AU14CommsVoiceProcedure.xml`: исправление структуры таблицы.
- `Content.IntegrationTests/_CMU14/AdminAI/{AISafeFaqTests,AdminAIConfigurationTests}.cs`: источники, классификация, races, default-off.
- `Tools/AdminAI.Tests/AdminAI.Tests.csproj`, `Tools/AdminAI.ServerTests/AdminAISafeFaqCoordinatorTests.cs`: focused harness и настоящий handler.
- `deploy/linux/run-local-ai-test.sh`: явный тестовый opt-in.
- Этот отчёт.

# Локальный тест RuCMU Admin AI в WSL2

> Исторический протокол этапа. Актуальная поставка и ограничения: [инструкция релиза](rucmu-admin-ai-release.md).

Проверки 2026-10-13–14, ветка `codex/rucmu-admin-ai-stage-c`.
Это отдельная локальная Ubuntu, не production-хост RuCMU/Squad.
Модель скачана отдельным административным шагом по прямому разрешению пользователя.
Сборка, запуск игры и worker не скачивают модели и не вызывают облачные AI API.

## Зафиксированные артефакты

| Артефакт | Версия / SHA256 |
| --- | --- |
| llama.cpp CPU release | `b11429`, commit `d81235049384534c167caea52b85a694f6103d14` |
| Архив Ubuntu x64 | `f6d25dde8f51133143d1453da4fd5f73b145127177612a283bf7995957af3392` |
| Установленный llama-server | `5faf410bbbc00a1b94b09ee01623699f255365628dfa73b1ee552d9e52a2231d` |
| Qwen3-4B-Instruct-2507 Q4_K_M GGUF | 2 497 280 736 bytes, `2fde00ce69dd4899c70d020845e2638353015bba0fdf161b3eb965f2bca4464e` |
| GGUF repository revision | `ae44f08e1392f39c0e474af10c3ff8355c8b6688` |
| Исходная модель Qwen revision | `cdbee75f17c01a7cc42f958dc650907174af0554` |
| Лицензия исходной модели | Apache-2.0, SHA256 файла `832dd9e00a68dd83b3c3fb9f5588dad7dcf337a0db50f7d9483f310cd292e92e` |

Источники: [официальный релиз llama.cpp](https://github.com/ggml-org/llama.cpp/releases/tag/b11429),
[GGUF от автора квантования bartowski](https://huggingface.co/bartowski/Qwen_Qwen3-4B-Instruct-2507-GGUF/tree/ae44f08e1392f39c0e474af10c3ff8355c8b6688),
[исходная модель Qwen](https://huggingface.co/Qwen/Qwen3-4B-Instruct-2507/tree/cdbee75f17c01a7cc42f958dc650907174af0554).
GGUF подготовлен bartowski, а не опубликован непосредственно организацией Qwen.

Ubuntu 26.04.1, WSL2 kernel `6.18.54.1-microsoft-standard-WSL2`, systemd 259.
Доступны global PSI и cgroup v2 CPU/memory/io/pids. .NET SDK 10.0.401 установлен
отдельно в `/home/rucmu/rucmu-ai-test/dotnet`; ICU/OpenMP взяты из официального Ubuntu
репозитория с проверкой SHA256. Сеть WSL при настройке была недоступна: установочные
артефакты переданы через Windows. Настройки firewall/sandbox не менялись.

## Worker

Установлен подготовленный `deploy/linux/rucmu-ai-worker.service`, без автозапуска.
Модель и бинарник находятся в root-owned `/opt/rucmu-ai`, доступны worker только для чтения.
Пользователь worker: `rucmu-ai`, группа сокета: `rucmu-ai-socket`; тестовый пользователь
`rucmu` включён в эту группу. `/run/rucmu-ai` и сокет имеют права 0770.

Проверены реальные значения cgroup: `cpu.max = 100000 100000`, memory high 3 GiB,
max 4 GiB, swap max 0, pids max 64. CPUWeight/IOWeight 1, Nice 15,
`PrivateNetwork=yes`, `RestrictAddressFamilies=AF_UNIX`, `NoNewPrivileges=yes`.
На systemd 259 устаревший `CPUAccounting=yes` выдаёт предупреждение и игнорируется;
CPU quota при этом проверена непосредственно в cgroup. Этот шаблон нужно повторно
проверять на целевом production systemd.

В Ubuntu:

```sh
sudo systemctl start rucmu-ai-worker
sudo systemctl status rucmu-ai-worker --no-pager
sudo systemctl stop rucmu-ai-worker
```

Реальный ограниченный smoke-запрос через production-клиент:

```sh
/home/rucmu/rucmu-ai-test/dotnet/dotnet \
  /mnt/c/Users/ha13r/GitHub/ColonialMarinesUniverse/.codex-worktrees/rucmu-admin-ai-stage-a/Tools/AdminAI.Spike/bin/Debug/net10.0/AdminAI.Spike.dll \
  model-smoke /run/rucmu-ai/llama.sock
```

`model-smoke` не запускает worker. Использует только фиксированный синтетический
вопрос, production endpoint validator и timeout 45 секунд. Только этот режим печатает
образец ответа: реальные обращения игроков не принимаются как аргументы.
Обычные `probe`/`selftest` по-прежнему не печатают тексты.

## Проверки и ограничения

| Проверка | Результат |
| --- | --- |
| Сборка обновлённого spike | 0 errors, 0 warnings |
| Boundary/security тесты в Ubuntu | 93 passed, 0 failed, 0 skipped |
| Mock UDS тесты в Ubuntu | 12 passed, 0 failed, 0 skipped |
| Настоящая модель, production UDS-клиент | Ready / Success, 12.04 s |
| Worker остановлен | Unavailable, exit 2 |
| Ручной restart, настоящая модель | Ready / Success, 11.66 s |
| Пик памяти worker после restart smoke | 2 529 845 248 bytes; OOM/OOM kill 0 |
| Использование Windows engine DLL на Linux | Непригодно: 2 failed, 4 skipped в серверном runner |
| Отдельная обычная Linux-сборка игры/движка | 0 errors, 137 warnings, 4 min 32 s |
| Focused server runner с Linux DLL | 6 passed, 0 failed, 0 skipped |
| Focused server runner с Windows DLL | Те же 6 passed, 0 failed, 0 skipped |
| Клиентский UI, включая ручной повтор после Paused/Unavailable | 10 passed, 0 failed, 0 skipped |
| Один поток, настоящая модель и запущенная игра | Ready / Success, 12.15 s; governor Normal |
| CPU PSI avg10 за 50 секунд однопоточного smoke | Максимум 0.18%; вместо 12.92–26.74% при прежнем ahelp-запросе |
| Worker после однопоточного smoke | MemoryPeak 3 221 852 160 bytes; OOM/OOM kill 0, NRestarts 0 |
| Настоящий ahelp после настройки | Success/current=True; пользователь подтвердил черновик, вставку и ручную отправку |
| Более долгий повтор из ahelp | TimedOut; governor продолжал работать в Normal, автоматического ответа нет |
| Подключение Windows-клиентов | AIAdminTest и AIPlayerTest подключены через loopback; InGame |
| Штатный sandbox настоящего Windows-клиента | Shared/Client/Shared.Database IL verification passed |

Ответ на синтетический запрос: «Просьба предоставить время и место события для
дальнейшего рассмотрения». Это образец черновика, а не доказательство общего качества
модели, безопасности всех её ответов или влияния на четыре игровых сервера.

Имеющийся `Robust.Shared.dll` содержит compile-time `ResPath.SystemSeparator = '\\'`:
его Windows-сборку нельзя переносить в Linux как готовую Linux-сборку игры.
Пользователь отдельно разрешил обычную Linux-сборку неизменённого движка в отдельной
тестовой папке. Исходники, sandbox/API и версии upstream-подмодулей менять запрещено.
Linux-сборка сделана в отдельном снимке с `TargetOS=Linux`, прежние Windows DLL
сохранены. Версии RobustToolbox `1eee1d78e4f9b091f533191639606d1d34deb42a` и RSI.NET
`3eead16bbade2214f4e0c4dd07a845051c24b075` не менялись. NuGet audit сообщил о
существующих уязвимостях ImageSharp 3.1.12; версии зависимостей в этой задаче не менялись.
В focused runner явные ссылки Shared.Testing/Client и пакет Moq фиксируют реальный
runtime manifest; отсутствие рядом случайных DLL больше не определяет успешность теста.

Сеть WSL NAT перехватывал существующий маршрут AmneziaVPN к `172.17.112.0/20`.
Пользователь отдельно разрешил создать `%USERPROFILE%/.wslconfig`:

```ini
[wsl2]
networkingMode=mirrored
```

После перезапуска `wslinfo --networking-mode` подтвердил mirrored. VPN и firewall
не менялись. Игра слушает только `127.0.0.1:1213`, AI — только UDS, внешняя
публикация сервера отключена. Описание режима:
[Microsoft WSL networking](https://learn.microsoft.com/en-us/windows/wsl/networking).

## Ручной тест ahelp

Worker и оба клиента во время подготовки запущены. Администратор AIAdminTest получил
права штатной локальной консольной командой `promotehost AIAdminTest`. AIPlayerTest
остаётся обычным игроком. Воссоздать окружение после закрытия:

```sh
sudo systemctl start rucmu-ai-worker
bash /home/rucmu/rucmu-ai-test/game/deploy/linux/run-local-ai-test.sh
```

В двух PowerShell-окнах из корня **рабочей ветки**, соответственно:

```powershell
& ./Tools/AdminAI.Local/Start-TestClient.ps1 -Role Admin
& ./Tools/AdminAI.Local/Start-TestClient.ps1 -Role Player
```

Каждая команда держит свой графический клиент, поэтому запускать их нужно отдельно.
После подключения в консоли игры: `listplayers`, затем `promotehost` с фактическим
именем администратора. При уже занятом имени клиент получает суффикс: в текущем
тесте права выданы `AIAdminTest_2`, обычный игрок — `AIPlayerTest_2`.

Development preset устанавливает собственный режим раунда после загрузки обычных
настроек. Для лёгкого лобби helper использует штатные `config.preset_development=false`
и `config.preset_debug=false`, без изменения preset-файлов или сборки движка.
Governor остаётся включённым. Пока сервер пуст
и автоматически приостановлен, AI закономерно Paused/metrics-stale. После подключения
игроков и устранения стартовых пиков зафиксирован переход Paused → Degraded → Normal
без отключения защиты. Загрузка карты/передача первого состояния дала существующие
сетевые предупреждения send-buffer would-block/dropped; оба клиента затем вошли InGame.

1. В окне AIPlayerTest открыть ahelp (F1) и отправить новый тестовый вопрос.
2. В AIAdminTest выбрать этого игрока и раскрыть «ИИ-помощник — приватный черновик».
3. Нажать «Подготовить», проверить текст и реальные источники.
4. Нажать «Вставить», убедиться, что игрок ещё не получил ответ; затем вручную отправить.

Первая ручная подготовка из настоящего ahelp была принята, но governor отменил её
при переходе Normal → Degraded; результат Paused корректно доставлен в приватный UI.
Устранён выявленный UI-дефект: Paused/Unavailable больше не блокируют ручной повтор
навсегда. Повтор не автоматический, сервер заново проверяет admission/права/контекст.
Добавлена диагностика реальных PSI и tick boundary ratio в `admin_ai_status` и
фоновых логах переходов; тексты prompt/response туда не пишутся. Пороги защиты не менялись.

При повторе в ahelp измерен CPU PSI 12.92% при Normal → Degraded и 26.74% при
Degraded → Paused. Memory/IO PSI были 0, максимум tick boundary / budget —
0.061–0.065. Worker использовал два потока при CPU quota в одно ядро. Для локальной
проверки установлен systemd drop-in `local-test.conf`, меняющий только `ExecStart`
на `--threads 1 --threads-batch 1`; квоты, ограничения доступа и governor сохранены.
Шаблон службы теперь тоже использует один поток под `CPUQuota=100%`.
Синтетический запрос при работающей игре завершился за 12.15 s, CPU PSI avg10
не превысил 0.18% за 50 секунд, governor остался Normal. За сам запрос cgroup
throttled_usec вырос только на 18 099 микросекунд. Это подтверждает эффективность
настройки в локальном тесте. После настройки настоящий ручной запрос из ahelp
завершился `Success`, сервер подтвердил `current=True` и привязал реальные источники.
Во время следующего запроса состояние оставалось Normal/Ready (CPU PSI 0.00%,
tick boundary / budget 0.049). Пользователь подтвердил появление черновика, вставку
в поле и ручную отправку. Один последующий запрос завершился TimedOut, пока governor
оставался Normal: один CPU достаточен для короткого smoke, но не гарантирует
завершение любого контекста в timeout. Ограничение времени не увеличивалось.
MemoryHigh срабатывал (high 3874), жёсткий лимит не достигнут, OOM отсутствует;
результат нельзя переносить на production или три Squad без отдельного baseline.

При выключении тестового сервера обнаружен отдельный crash существующего клиента
при переходе в главное меню: unregistered `Content.Client.Corvax.TTS.TTSSystem`.
Код TTS не менялся в этой задаче. WSL без активных пользовательских процессов может
завершиться вместе со своими systemd-службами: это не crash/OOM модели. Для повторного
запуска вручную стартовать worker и держать игровой сервер/Ubuntu открытыми.

Генерация и доставка актуального черновика из настоящего ahelp подтверждены сервером.
Вставка и ручная отправка подтверждены пользователем в интерфейсе 2026-10-14.
Всего пройдены 121 различных focused теста: 93 boundary/security, 12 mock IPC,
6 серверных, 10 клиентских; серверные 6 дополнительно повторены на Windows.
Принятие D/E и baseline четырёх игровых серверов остаются отдельными задачами.

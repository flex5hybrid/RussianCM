# Оружейный Техник (Ordnance Technician) — состояние порта

## Ключевые файлы

### C# код
| Файл | Назначение |
|---|---|
| `Content.Server/_RuMC14/Ordnance/RMCOrdnanceAssemblySystem.cs` | Комбинирование деталей → сборка, разборка ломом, блокировка отвёрткой |
| `Content.Server/_RuMC14/Ordnance/RMCChembombSystem.cs` | Основная логика: заполнение химикатами, запечатывание, взведение, детонация |
| `Content.Server/_RuMC14/Ordnance/RMCMineCasingSystem.cs` | Установка мины: DoAfter → anchor, StepTrigger, IFF |
| `Content.Server/_RuMC14/Ordnance/RMCExplosionSimulatorSystem.cs` | Симулятор взрывов (BUI) — создаёт отдельный чамбер-мэп |
| `Content.Server/_RuMC14/Ordnance/RMCDemolitionsSimulatorSystem.cs` | Стационарный демолишн-симулятор (BUI) |
| `Content.Shared/_RuMC14/Ordnance/RuMCOrdnanceAssemblyComponent.cs` | Данные сборки: LeftPartType, RightPartType, IsLocked, TimerDelay |
| `Content.Shared/_RuMC14/Ordnance/RuMCChembombCasingComponent.cs` | Данные корпуса: MaxVolume, BasePower, Stage, HasActiveDetonator и т.д. |
| `Content.Shared/_RuMC14/Ordnance/RuMCOrdnancePartComponent.cs` | Тип детали: RMCOrdnancePartType enum |
| `Content.Client/_RuMC14/Ordnance/RuMCOrdnanceAssemblyVisualsSystem.cs` | Динамический спрайт сборки из двух RSI-слоёв |

### Прототипы
| Файл | Назначение |
|---|---|
| `Resources/Prototypes/_RuMC/Entities/Objects/Ordnance/detonator_assemblies.yml` | Все детали (Igniter, Timer, ProxSensor, Signaler) и готовые сборки |
| `Resources/Prototypes/_RuMC/Entities/Objects/Ordnance/base_casing.yml` | Абстрактный базовый корпус `RMCCasingBase` |
| `Resources/Prototypes/_RuMC/Entities/Objects/Ordnance/mines_custom.yml` | M20 мина и M40 граната |
| `Resources/Prototypes/_RuMC/Entities/Objects/Ordnance/grenade_custom.yml` | M15 граната |
| `Resources/Prototypes/_RuMC/Entities/Objects/Ordnance/C4_custom.yml` | C4 и 80мм миномётный варгед/корпус |
| `Resources/Prototypes/_RuMC/Entities/Objects/Ordnance/mortar_shell_custom.yml` | 80мм корпус мины, камера-варгед + construction graph |
| `Resources/Prototypes/_RuMC/Entities/Objects/Ordnance/rockets_custom.yml` | 84мм труба и варгед |
| `Resources/Prototypes/_RuMC/Recipes/Construction/Graphs/mortar_shell.yml` | Construction graph `RMC80mmMortarCasingCustomGraph` |

### Спрайты
Все в `Resources/Textures/_RuMC14/Ordnance/`:
- `igniter.rsi` — igniter, igniter_left, igniter_right
- `timer.rsi` — timer, timer_left, timer_right, timer_timing_l, timer_timing_r
- `prox.rsi` — prox, prox_left, prox_right + scanning/timing states
- `signaller.rsi` — signaller, signaller_left, signaller_right
- `voice.rsi` — voice, voice_left, voice_right (СПРАЙТЫ ГОТОВЫ — реализации нет)
- `motion.rsi` — motion, motion_active (СПРАЙТЫ ГОТОВЫ — реализации нет)

### Локализация
`Resources/Locale/en-US/_RMC14/ordnance/chembombs.ftl` и `ru-RU/_RMC14/ordnance/chembombs.ftl` — все строки есть (rmc-chembomb-*, rmc-mine-*, rmc-demolitions-sim-*, rmc-ordnance-assembly-*)

## Два поколения системы детонаторов

**Старая система** (RMCDetonatorAssemblyComponent):
- Сущности: RMCTimerDetonatorAssembly, RMCDoubleIgniterAssembly, RMCProximitySensorAssembly, RMCSignalerAssembly
- Используют PayloadTrigger + OnUseTimerTriggerComponent в YAML
- Определены в `detonator_assemblies.yml` (нижняя часть "# Ниже старая хуйня")
- Есть рецепты в Armylathe upstream

**Новая система** (RMCOrdnanceAssemblyComponent):
- Детали: RMCOrdnanceIgniter, RMCOrdnanceTimer, RMCOrdnanceProximitySensor, RMCOrdnanceSignaler
- Комбинируются через RMCOrdnanceAssemblySystem
- Закрываются отвёрткой (IsLocked) → принимаются корпусом через тег `RMCOrdnanceAssemblyLocked`
- Триггер назначается в RMCChembombSystem.OnItemInserted

## Сборочный процесс (как должен работать цикл)

```
[Armylathe] → детали (Igniter + Timer/Prox/Signaler)
   ↓ use-in-hand друг на друга
[RMCOrdnanceAssembly] (IsLocked=false)
   ↓ отвёртка
[RMCOrdnanceAssembly] (IsLocked=true, тег RMCOrdnanceAssemblyLocked)
   ↓ вставить в корпус
[Корпус] (HasActiveDetonator=true) + нужный триггер
   ↓ заполнить беакером
[Корпус] (с химикатами)
   ↓ отвёртка → Sealed → кусачки → Armed
[Корпус ARMED] → готов к применению
```

## Стадии корпуса (RMCCasingAssemblyStage)

```
Open → (screwdriver) → Sealed → (wirecutters) → Armed
Armed → (wirecutters) → Sealed → (screwdriver) → Open
```

Вставить детонатор можно только в Open.  
Заполнить химикатами можно только в Open.

## Известные баги и нерешённые задачи

### ИСПРАВЛЕНО
- ~~Опечатка `RMCOrdnanceSignaller` в GetPartProto~~ → исправлено на `RMCOrdnanceSignaler`
- ~~Отсутствующий construction graph `RMC80mmMortarCasingCustomGraph`~~ → создан минимальный граф
- ~~ItemSlots закомментированы у RMC80mmMortarWarhead~~ → раскомментированы и исправлены

### НЕРЕШЁННЫЕ ФИЧИ (приоритет)
1. **Signaler trigger** — OnItemInserted в RMCChembombSystem не назначает триггер для Signaler типа (TODO-комментарий)
2. **Shrapnel** — BaseShards/ShrapnelProto в компоненте есть, OnTrigger не спаунит осколки  
3. **Сборка ракеты** — RMC84mmRocketTube + RMC84mmRocketWarhead не объединяются
4. **Locked visual** — IsLocked не меняет спрайт assembly
5. **Валидация комбинаций** — любые 2 детали принимаются, нужна таблица допустимых пар
6. **Armylathe рецепты** — RMCOrdnanceProximitySensor и RMCOrdnanceSignaler (NEW) не в лате
7. **Voice Detonator** — спрайты готовы, реализации нет
8. **Motion Sensor** — спрайты готовы (inc. active state), реализации нет

### ТЕХНИЧЕСКИЙ ДОЛГ
- Удалить старую систему RMCDetonatorAssemblyComponent после проверки новой
- ExplosionSimulator: Unwatch при закрытии BUI (иначе игрок остаётся в темноте)
- mortar_shell_custom.yml: propellingCharge slot (shell+warhead combo) не реализован
- RMC80mmMortarCasingCustomGraph — только минимальный граф, нет перехода к assembled state

## Armylathe — что уже есть в upstream

В `Resources/Prototypes/_RMC14/Entities/Structures/Machines/Lathe/armylathe.yml` уже есть:
- RMCOrdnanceIgniter, RMCOrdnanceTimer (новые детали)
- RMCProximitySensorAssembly, RMCSignalerAssembly (СТАРЫЕ сборки)
- RMCM40GrenadeCasing, RMCM15GrenadeCasing, RMCM20MineCasing, RMCC4PlasticCasing
- RMC84mmRocketTube, RMC84mmRocketWarhead
- RMC80mmMortarShell, RMC80mmMortarWarhead, RMC80mmMortarCameraWarhead

НЕТ в лате: RMCOrdnanceProximitySensor и RMCOrdnanceSignaler (новые детали)

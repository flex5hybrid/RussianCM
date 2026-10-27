ent-CMUExpeditionScavenger = вооруженный падальщик
    .desc = Выживший, держащий затонувший периметр. Их винтовка очень реальна.

cmu-expedition-scavenger-identity = вооруженный падальщик

cmd-cmu-expedition-ai-desc = Посейте отряд падальщиков здесь на любой карте или на объекте экспедиции.

cmd-cmu-expedition-ai-help = сму-экспедиция-ай <map ID|here> [count: 1-12] [variant] [outfit]Используйте Tab для просмотра ролей, состава отряда и фракционной одежды. Отступники не меняют товарищеские отношения, цели или МКФ.

cmu-expedition-here-no-player = Опция «здесь» требует вашего прикрепленного игрока или наблюдателя на карте с землей.

cmu-expedition-ai-use-here = Эта карта не имеет цели экспедиции. Встаньте или привидение над желаемой землей и используйте cmu-expedition-ai здесь.

cmu-expedition-ai-no-space = Безопасного грунта поблизости не обнаружено. Отойдите от скал, границ твердой воды, огня или занятой земли.

cmu-expedition-ai-deployed = развернутый {$count}/{$requested} {$variant} падальщики в отряде {$squad} на карте {$map}Используйте приказы об экспедициях, чтобы направлять их.

cmu-expedition-orders-none = Ни один охранник не принял приказ. Проверь удостоверение и чистую землю. Патрули нужны 2-8 Погибшие или контролируемые игроком охранники не могут получать приказы о движении.

cmu-expedition-ai-orders-status = Заказы: пункт назначения {$destination}патруль {$patrolling}точка опоры {$waypoint}/{$points}заблокированный {$blocked} (автоматический повтор).

ent-CMUExpeditionScavengerPoor = мусорщик
    .desc = Голодный крушитель в залатанном пальто. Избыточный пистолет и один запасной журнал - все, что они могут себе позволить.

ent-CMUExpeditionScavengerRich = Спасательный барон
    .desc = Успешный спасатель покупает керамические пластины и украденную импульсную винтовку. Этот платил, чтобы выжить в следующем споре.

ent-CMUExpeditionScavengerScout = Скаутский след
    .desc = Надзиратель, несущий короткий карабин и дым для поспешного побега. Они выявляют проблемы перед остальным экипажем.

cmu-expedition-poor-identity = мусорщик

cmu-expedition-rich-identity = Спасательный барон

cmu-expedition-scout-identity = Скаутский след

cmu-expedition-ai-already-spawned = Эта экспедиция уже получила свой превью-отряд.

cmu-expedition-ai-spawned = развернутый {$count} Вооруженные падальщики возле крушения.

ent-CMUExpeditionFieldDressing = Экспедиционная полевая одежда
    .desc = Компактная аварийная повязка для стабилизации незначительных боевых повреждений. Три заявки на пачку.

cmu-expedition-field-dressing-stack = полевые повязки

cmd-cmu-expedition-ai-status-desc = Показать действия пехоты экспедиции, покрыть выбор и расходы на поиск.

cmd-cmu-expedition-ai-status-help = cmu-expedition-ai-status <map ID|here>

cmu-expedition-ai-status-line = {$entity}: {$disposition}/{$emotion}Стресс {$stress}Инициатива {$initiative} Отряд {$squad}, {$goal}/{$action}, {$state} в {$position} патроны {$ammo}повреждения {$damage}подавленный {$suppressed}Пожарная проверка {$fireCheck} убежище {$anchor}Смотри. {$peek} Поиск {$cells} клетки {$milliseconds} ms (max) {$maxSearchMs}маршрут {$routeCells} / {$routeMs} ms | перезагрузка {$reloads}гранаты {$grenades}Спасатели {$rescues}Фланги {$flanks}доклады {$reports}, неудачи {$failures}

ent-CMUExpeditionScavengerAggressive = охотник за мусором
    .desc = Смелый стрелок, который нажимает на ясное отверстие, но все еще может быть потрепан входящий огонь.

ent-CMUExpeditionScavengerCautious = караульщик
    .desc = Защитник пациента, который предпочитает расстояние, измеренные выстрелы и безопасный путь назад.

cmu-expedition-raider-identity = охотник за мусором

cmu-expedition-sentry-identity = караульщик

cmu-expedition-contact-report = Контакт в моей последней наблюдаемой позиции. Прикрой подход.

cmu-expedition-hint-outfit = Одежда: scavenger, uscm, rmcTWE), upp (UPP), pmc, clf, cmb, lacn, ccaf, uacg или prodigy

cmu-expedition-outfit-applied = Запрошенный {$outfit} Одежда. Команда и МФФ остаются неизменными; устанавливают дружелюбие и цели с помощью команд-экспедиций. Проверьте ai-статус для снаряжения каждого члена.

cmu-expedition-sniper-identity = снайперский падальщик

cmu-expedition-variant-sniper = Снайпер с ан M42AРодные прицельные выстрелы и запасной пистолет

cmu-expedition-ai-vision-status = Видение ={ $sight } Вспышки ={ $flares } FlashShots ={ $flashShots } БиподПопытки={ $bipods }

cmu-expedition-ai-perception-status = Восприятие: проверка зрения { $checks }/{ $limit }Контактные записи { $contacts }В поисках { $searching }Поиск остается { $remaining }s

cmu-expedition-ai-logistics-status = Outfit={ $outfit } поставки ={ $decision } общий{ $shared } получено ={ $received } Открытые ящики ={ $crates }

cmu-expedition-contact-steady-1 = Контакт. Держитесь подальше и следите за своими полосами.

cmu-expedition-contact-steady-2 = Глаза враждебные. Держите это под контролем.

cmu-expedition-contact-steady-3 = Новый контакт. Проверьте стороны, прежде чем двигаться.

cmu-expedition-contact-bold-1 = Вот они где. Держите давление!

cmu-expedition-contact-bold-2 = На них есть глаза. Сделайте свои выстрелы.

cmu-expedition-contact-bold-3 = Контакт! Не дай им пройти мимо нас.

cmu-expedition-contact-careful-1 = Движение. Враждебность подтверждена. Следи за пробелами.

cmu-expedition-contact-careful-2 = Контакт. Полегче. Держите путь эвакуации.

cmu-expedition-contact-careful-3 = Глаза на одного. Не преследуйте одну и ту же цель.

cmu-expedition-contact-shaken-1 = Больше контактов. Держите его вместе, держите его вместе.

cmu-expedition-contact-shaken-2 = Враждебное пятно. Мне нужна помощь!

cmu-expedition-contact-shaken-3 = Контакт! Следи за сторонами. Пожалуйста, следите за сторонами.

cmu-expedition-contact-hurt-1 = Контакт. Мне больно, но я все еще смотрю на них.

cmu-expedition-contact-hurt-2 = Еще один враг. Мне больно. Нужно, чтобы кто-то следил за мной.

cmu-expedition-contact-hurt-3 = Глаза враждебные. Не очень хорошо здесь.

cmu-expedition-contact-rush-1 = Близкий контакт! Освободите место!

cmu-expedition-contact-rush-2 = Скоро приедет. Держи дистанцию!

cmu-expedition-contact-rush-3 = Прямо на нас! Распространяйтесь!

cmu-expedition-contact-medic-1 = Контакт. Оставайтесь там, где я могу добраться до вас, если вы спуститесь.

cmu-expedition-contact-medic-2 = Враждебное пятно. Держите полосу открытой для жертв.

cmu-expedition-contact-medic-3 = Контакт. Звоните, если вас ударили, не убегайте в одиночку.

cmu-expedition-ai-fallback-status = Вызовы ={ $callouts } убранство ={ $scavenged } ближний{ $strikes } Восстановление ={ $item }

cmu-expedition-ai-radio-status = Радио: {$received} получено/ {$accepted} принятый, {$decision}Действуя по радио {$radioContact}подход {$destination}.

cmu-expedition-ai-survival-status = Выживание: оружие {$weapon}восстановленный {$recovered}расстояние {$spacing} то {$destination}Отвергнутое покрытие {$cover}подготовка работы {$preparing}Работает {$working}.

cmu-expedition-ai-threat-status = Угрозы: {$enemies} видимый через {$sectors} направления, {$shooters} Недавние стрелки, перекрестный огонь {$crossfire}переизбранный {$moves}Фланковые ответы {$responses}Другие отряды, участвующие в этой цели {$assigned}.

cmu-expedition-ai-equipment-status = Оборудование: {$weapon}Переключатели оружия {$switches}Стреляли ракеты {$rockets}Решение о гранате {$grenade}Выброшенный дым {$smokes}выстрелы во время путешествия {$movingShots}.

cmu-expedition-assault-identity = штурмовик-пожиратель

cmu-expedition-support-identity = стрелок поддержки падальщика

cmu-expedition-marksman-identity = стрелок-пожиратель

cmu-expedition-rocketeer-identity = ракетоносец

cmu-expedition-medic-identity = Полевой врач

cmu-expedition-ai-medical-status = Медицинский: {$decision}фаза {$phase}Пациент {$patient}Прикрывая огонь {$covered}лечение {$doses}шоки {$shocks}Возрождение {$revivals}Извлечение {$extractions}.

ent-CMUExpeditionScavengerMedic = Полевой врач
    .desc = Медик спасателей с повязками, стабилизирующим инжектором и дефибриллятором.

ent-CMUExpeditionDefibrillator = полевой дефибриллятор
    .desc = Стандартный дефибриллятор с батарейным питанием, переносимый врачом.

ent-CMUExpeditionStabilizer = инжектор стабилизации поля
    .desc = Конечный запас стандартного коктейля для восстановления трикордазина.

ent-CMUExpeditionScavengerAssault = штурмовик-пожиратель
    .desc = Быстроходный рейдер с компактным пистолетом-пулеметом и запасным пистолетом.

ent-CMUExpeditionScavengerSupport = стрелок поддержки падальщика
    .desc = Тяжелый наводчик спасателей, несущий украденную винтовку.

ent-CMUExpeditionScavengerMarksman = стрелок-пожиратель
    .desc = Терпеливый стрелок с винтовкой стрелка и пистолетом для близких неприятностей.

ent-CMUExpeditionScavengerRocketeer = ракетоносец
    .desc = Налетчик с пистолетом-пулеметом и одной одноразовой фугасной ракетой.

ent-CMUExpeditionFieldPack = сумка с полем для мусора
    .desc = Расширенная спасательная сумка, упакованная боеприпасами, полевыми припасами и запасным оружием.

cmu-expedition-radio-channel = Полевой отряд

ent-CMUExpeditionHeadset = полевая гарнитура
    .desc = Радио отряда малой дальности. Он работает без башни в пределах сорока метров.

ent-CMUExpeditionEncryptionKey = ключ шифрования полевой команды
    .desc = Ключ от полевого радио.

ent-CMUExpeditionBlastGrenade = граната HEDP
    .desc = Стандартная четырехсекундная граната, перевозимая в полевых припасах мусорщика.

ent-CMUExpeditionSmokeGrenade = дымовая граната
    .desc = Дымовая граната, перевозимая для покрытия изъятия или восстановления пострадавших.

cmu-expedition-ai-coordination-status = Отряд: {$role}, {$decision}покрытый {$shooter}покрытие {$mover}; покрытые ходы {$moves}Прерванный {$interruptions}; трафик {$traffic}.

ent-CMUExpeditionScavengerBreacher = дробовик
    .desc = Боец ближнего боя с типом 23 Нагружен тяжелыми слизняками, запасными снарядами и запасным пистолетом.

ent-CMUExpeditionScavengerSkirmisher = стычка с мусорщиком
    .desc = Легко оборудованный MP5 рейдер, который предпочитает прикрытые фланги и реагирует на атаки со стороны.

ent-CMUExpeditionScavengerMachinegunner = пулеметчик
    .desc = Ан M60 Наводчик, который держит полосы стрельбы, в то время как остальная часть спасателей движется.

ent-CMUExpeditionScavengerVeteran = ветеран падальщиков
    .desc = Опытный спасатель со старым AR10Более тяжелая броня и предпочтение устойчивого огня средней дальности.

cmu-expedition-breacher-identity = дробовик

cmu-expedition-skirmisher-identity = стычка с мусорщиком

cmu-expedition-machinegunner-identity = пулеметчик

cmu-expedition-veteran-identity = ветеран падальщиков

ent-CMUExpeditionBreacherShotgun = Тип мусорщика 23 ружье
    .desc = Стандартный тяжелый дробовик, перевозимый спасательным истребителем ближнего боя экипажа.

ent-CMUExpeditionSkirmisherSMG = мусорщик MP5
    .desc = Стандарт MP5 Перевозится подвижной фланговой охраной спасателей.

ent-CMUExpeditionMachineGun = мусорщик M60
    .desc = Стандартный ленточный корм M60 Используется для прикрытия движений спасателей.

ent-CMUExpeditionVeteranRifle = мусорщик AR10
    .desc = Старый AR10 Поддерживается опытным спасателем.

cmu-expedition-hint-here = Ваше текущее местоположение; работает на обычных картах

cmu-expedition-hint-map = Карта ID

cmu-expedition-hint-count = Количество падальщиков (1-12)

cmu-expedition-hint-variant = Индивидуальная роль или смешанный состав отряда

cmu-expedition-hint-squad = ID отряда на выбранной карте

cmu-expedition-hint-squad-members = {$count} Живые члены AI

cmu-expedition-hint-order = Приказ отряда

cmu-expedition-hint-style = Боевое расположение

cmu-expedition-hint-factions = Фракции NPC, разделенные запятыми или по умолчанию для устранения переопределений

cmu-expedition-hint-x = Карта X координаты

cmu-expedition-hint-y = Карта Y координаты

cmu-expedition-hint-scenario = Сценарий экспедиции

cmu-expedition-hint-seed = Семя целого поколения

cmu-expedition-hint-hour = Час назад 0 то 23.99

cmu-expedition-variant-regular = MAR40 стрелок; охранник общего назначения

cmu-expedition-variant-poor = Легко оборудованный пистолетный падальщик

cmu-expedition-variant-rich = Бронированный патронник

cmu-expedition-variant-scout = Карбинный разведчик; более широкое обнаружение и фланговое предпочтение

cmu-expedition-variant-assault = M63 штурмовик; залпы ближнего боя

cmu-expedition-variant-support = Поддерживает импульсную винтовку; удерживает полосы движения

cmu-expedition-variant-marksman = M4SPR стрелок; огонь большей дальности

cmu-expedition-variant-rocketeer = SMG и одна одноразовая ракета; проверка взрывов и бэкбластов

cmu-expedition-variant-medic = Полевой медик; лечение, восстановление пострадавших и соответствующие возрождения

cmu-expedition-variant-breacher = Тип 23 дробовик и пистолет; ближний огонь

cmu-expedition-variant-skirmisher = MP5; прикрытые фланги и ответ фланга

cmu-expedition-variant-machinegunner = M60; более длинные залпы и предпочтение прикрытия огня

cmu-expedition-variant-veteran = AR10Устойчивый огонь средней дальности и более тяжелая броня

cmu-expedition-variant-mixed = Разнообразный спасательный экипаж с винтовкой, поддержкой, флангом и медицинскими ролями

cmu-expedition-variant-specialists = Поддержка, штурм, стрелок, ракета, медик и дробовик

cmu-expedition-variant-medical = Медик с поддержкой и винтовочным сопровождением

cmu-expedition-variant-raiders = Дробовик, MP5Нападение, поддержка и медик

cmu-expedition-variant-fireteam = AR10 ветеран, M60 наводчик, MP5 стрелок, медик и стрелок

cmu-expedition-order-move = Путешествие в точку, возобновление после контакта

cmu-expedition-order-assault = Сражайтесь в точке с короткими авансами и прикрывая огонь.

cmu-expedition-order-guard = Путешествие в точку и укрепление, когда безопасно

cmu-expedition-hint-guard-facing = Охрана лицом (карта кардинального направления) или авто для выбора открытого подхода

cmu-expedition-ai-fieldcraft-status = Полевое строительство ={ $construction }лицом ={ $facing }Припасы разграблены ={ $supplies }Опасность ={ $hazard }, Dodges={ $dodges }Местные объезды ={ $detours }Сбои в заказе ={ $failures }

cmu-expedition-order-patrol-add = Добавить точку доступа (до восьми)

cmu-expedition-order-patrol-start = Начните зацикливаться по крайней мере на двух точках

cmu-expedition-order-patrol-stop = Прекратить патрулирование

cmu-expedition-order-patrol-clear = Остановить и очистить все пути

cmu-expedition-order-style = Измените агрессию, смелость и предпочтительный диапазон

cmu-expedition-order-friendly = Установить дружественную фракцию превышения

cmu-expedition-order-target = Установите целевые фракции; AllHostile привлекает всех недружественных комбатантов

cmu-expedition-ai-door-status = Двери: { $decision }В ожидании { $door }Открытый { $opened }Неудачный { $failures }.

ent-CMUExpeditionRocketPack = экспедиционный пусковой носитель
    .desc = Полевой пакет с внешними нарезками для одной резервной одноразовой пусковой установки наряду с обычными поставками.

cmu-expedition-ai-status-budget = Все карты, последний тик: {$processed} решения выполнены, {$deferred} отсроченный, {$milliseconds} Мисс Решающая. Огнеметные чеки проходят каждый тик.

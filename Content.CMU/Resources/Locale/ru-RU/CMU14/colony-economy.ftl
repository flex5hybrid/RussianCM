cmu-atm-card-slot-occupied = В банкомате уже есть карта.
cmu-atm-no-card = У вас нет ID-карты, которую можно вставить.
cmu-atm-take-card-verb = Забрать карту
cmu-atm-take-cash-verb = Забрать наличные
cmu-atm-take-card-start = Вы начинаете вытаскивать карту из банкомата...
cmu-atm-take-card-start-others = {CAPITALIZE(THE($user))} начинает вытаскивать карту из банкомата!
cmu-atm-boot-title = КОЛОНИАЛЬНЫЕ ФИНАНСОВЫЕ СИСТЕМЫ W-Y
cmu-atm-boot-bios = BIOS 2.7 (C) 2179 W-Y CORP.
cmu-atm-boot-memory = ПАМЯТЬ 640K
cmu-atm-boot-keypad = КЛАВИАТУРА
cmu-atm-boot-reader = СЧИТЫВАТЕЛЬ КАРТ
cmu-atm-boot-dispenser = ВЫДАЧА НАЛИЧНЫХ
cmu-atm-boot-uplink = СВЯЗЬ С КАЗНАЧЕЙСТВОМ ООН
cmu-atm-boot-ok = ОК
cmu-atm-boot-loading = ЗАГРУЗКА ТЕРМИНАЛА...
cmu-atm-out-of-order = НЕ РАБОТАЕТ
cmu-atm-out-of-order-sorry = ВОСПОЛЬЗУЙТЕСЬ ДРУГИМ БАНКОМАТОМ
cmu-atm-hint-confirm = OK = подтвердить   X = назад
cmu-atm-hint-continue = Нажмите OK для продолжения.
cmu-atm-nav-title = Банкомат колонии
cmu-atm-nav-pin = Ваша карта №{ $account } — PIN { $pin }
cmu-atm-nav-no-card = У вас нет собственной карты
cmu-atm-nav-pin-unknown = Чтение вашей карты...
cmu-atm-nav-pop-out = Отдельное окно

# ATM screens and transaction messages
cmu-atm-busy = Кто-то уже использует этот банкомат.
cmu-atm-tampered = Внимание: обнаружены следы вмешательства в электронику.
cmu-atm-normal = Банкомат работает исправно.
cmu-atm-unknown = Неизвестно
cmu-atm-invalid-amount = Введите корректную сумму.
cmu-atm-insufficient-funds = Недостаточно средств.
cmu-atm-insufficient-hand = Недостаточно наличных в руках.
cmu-atm-invalid-account = Введите корректный пятизначный номер счёта.
cmu-atm-account-missing = Счёт не найден.
cmu-atm-insufficient-cash = Недостаточно наличных.
cmu-atm-own-account = Нельзя перевести деньги на собственный счёт.
cmu-atm-timeout = Время сеанса истекло.
cmu-atm-take-cash-first = Сначала заберите наличные.
cmu-atm-invalid-pin = Неверный формат PIN. Введите все { $digits } цифры.
cmu-atm-incorrect-pin = Неверный PIN. Попытка { $attempt }/{ $maximum }.
cmu-atm-withdraw-confirm = Снять ${ $amount }? После налога вы получите ${ $net }.
cmu-atm-dispensed = Выдано ${ $amount }. Баланс: ${ $balance }.
cmu-atm-deposited = Внесено ${ $amount }. Баланс: ${ $balance }.
cmu-atm-recipient = Получатель: { $name }. Введите сумму:
cmu-atm-remote-confirm = Внести ${ $amount } на счёт №{ $account }?
cmu-atm-remote-done = Внесено ${ $amount } на счёт №{ $account }.
cmu-atm-transfer-confirm = Перевести ${ $amount } на счёт №{ $account }?
cmu-atm-transfer-done = Переведено ${ $amount }. Баланс: ${ $balance }.
cmu-atm-welcome =
    ФИНАНСОВЫЙ ТЕРМИНАЛ КОЛОНИИ
    v2.7  КАЗНА ООН
    { "" }
    1) ВНЕСЕНИЕ НА ЧУЖОЙ СЧЁТ
    { "" }
    Вставьте ID-карту для доступа к счёту.
cmu-atm-pin-prompt = ВВЕДИТЕ PIN:
cmu-atm-locked =
    { "** КАРТА ЗАБЛОКИРОВАНА **" }
    Слишком много неверных попыток. Попробуйте позже.
cmu-atm-main-menu =
    Здравствуйте, { $name }.
    { "" }
    1) СНЯТИЕ
    2) ВНЕСЕНИЕ
    3) ПЕРЕВОД
    4) ВНЕСЕНИЕ НА ЧУЖОЙ СЧЁТ
    5) ИСТОРИЯ
    6) ВЫХОД
cmu-atm-withdraw-prompt =
    СНЯТИЕ
    Введите сумму:
cmu-atm-deposit-prompt =
    ВНЕСЕНИЕ
    Введите сумму:
cmu-atm-remote-prompt =
    ВНЕСЕНИЕ НА ЧУЖОЙ СЧЁТ
    Номер счёта получателя:
cmu-atm-transfer-prompt =
    ПЕРЕВОД
    Номер счёта получателя:
cmu-atm-history-title = ИСТОРИЯ СЧЁТА
cmu-atm-history-empty = Операций пока нет.
cmu-atm-history-back = OK = назад
cmu-atm-history-withdrawal = -${ $amount } СНЯТИЕ
cmu-atm-history-deposit = +${ $amount } ВНЕСЕНИЕ
cmu-atm-history-cash = +${ $amount } НАЛИЧНЫЕ
cmu-atm-history-out = -${ $amount } НА №{ $account }
cmu-atm-history-in = +${ $amount } ОТ №{ $account }
cmu-atm-history-returned = +${ $amount } ВОЗВРАТ
cmu-atm-history-purchase = -${ $amount } ПОКУПКА
cmu-atm-fault-fatal = СБОЙ { $byte }: ОШИБКА CRC РЕЕСТРА
cmu-atm-fault-segv = SEGV ПО АДРЕСУ { $address } В TXN_CORE
cmu-atm-fault-halted = TXN_CORE ОСТАНОВЛЕНО
cmu-atm-fault-watchdog = СБРОС WATCHDOG ... НЕ УДАЛСЯ
cmu-atm-fault-uplink = СВЯЗЬ ПОТЕРЯНА, ПОВТОР { $attempt }/3
cmu-atm-fault-dispenser = ОШИБКА ШИНЫ ВЫДАЧИ { $byte }
cmu-atm-fault-auth = ТАБЛИЦА ДОСТУПА ПОВРЕЖДЕНА: { $address }
cmu-atm-fault-kernel = ПАНИКА ЯДРА: НЕТ СИНХРОНИЗАЦИИ
cmu-atm-fault-stack = СТЕК ПОВРЕЖДЁН, ПРЕРЫВАНИЕ
cmu-atm-fault-trap = НЕОБРАБОТАННОЕ ИСКЛЮЧЕНИЕ { $byte }: { $address }
cmu-atm-fault-garbled = ?? ???? ?????? ??? ??

# RUMC: налог на посылки, проданные через торговые платформы
admin-console-trade-tax-section = -- Налог на продажу посылок --
admin-console-current-trade-tax = Текущий налог на продажу посылок: { $percent }%
admin-console-trade-tax-description = Берётся из суммы, которую дрон платит за посылку, проданную на торговой платформе. Эта доля уходит в бюджет колонии, остальное получает продавец.
admin-console-trade-tax-announcement = Налог колонии на продажу посылок установлен в { $percent }%. Эта доля от каждой посылки, проданной на торговой платформе, уходит колонии.

colony-economy-sales-tax = Налог с продаж: { $percent }%

colony-economy-income-tax = Подоходный налог: { $percent }%

colony-economy-transit-tariff = Транзитный тариф: { $percent }%

colony-economy-active-embargoes = Активные эмбарго: { $factions }

colony-economy-no-embargoes = Эмбарго: нет

colony-economy-active-trade-pacts = Активные торговые пакты: { $factions }

colony-economy-no-trade-pacts = Торговые соглашения: нет

colony-economy-overview = - Обзор экономики колонии -

colony-economy-third-party-support = Сторонняя поддержка -

colony-economy-open-third-party-menu = Открытое стороннее меню

colony-economy-unknown-faction = Неизвестная фракция

colony-economy-apply = Применять

admin-console-title = Консоль администрации

admin-console-colony-budget = Бюджет колонии: ${ $amount }

admin-console-sales-tax-section = - Налог с продаж -

admin-console-current-sales-tax = Текущий налог с продаж: { $percent }%

admin-console-sales-tax-description = Применяется к денежным поставщикам и корпоративным заказам ASRS. Налоговые поступления идут в колониальный бюджет.

admin-console-new-tax = Новый налог0–50%):

admin-console-tax-placeholder = Например. 10

admin-console-income-tax-section = - Налог на прибыль -

admin-console-current-income-tax = Текущий подоходный налог: { $percent }%

admin-console-income-tax-description = Вычитается из выплат заработной платы и корпоративных денежных средств. Доходы идут в колониальный бюджет.

admin-console-announcement-sender = Администрация

admin-console-sales-tax-announcement = Налог с продаж колоний установлен { $percent }%.

admin-console-income-tax-announcement = Налог на доходы колонии установлен { $percent }Это влияет на выплаты заработной платы и корпоративные изъятия.

corporate-console-title = Консоль по корпоративным вопросам

corporate-console-budget = Корпоративный бюджет: ${ $amount }

corporate-console-withdraw-section = - Снять наличные -

corporate-console-transit-tariff-section = - Транзитный тариф -

corporate-console-current-transit-tariff = Текущий транзитный тариф: { $percent }%

corporate-console-transit-tariff-description = Процент от всех поданных на хранение выплат, который идет в корпоративный бюджет вместо колонии.

corporate-console-new-tariff = Новый тариф0–50%):

corporate-console-tariff-placeholder = Например. 15

corporate-console-withdraw-tax-note = Примечание: Выводы подлежат { $percent }% подоходный налог.

corporate-console-withdraw-no-tax = Нет подоходного налога на снятие средств.

corporate-console-announcement-sender = Корпоративные вопросы

corporate-console-tariff-announcement = Корпоративный транзитный тариф установлен { $percent }Выплаты в колонию были скорректированы.

budget-console-title = Бюджетная консоль

budget-console-current-budget = Текущий бюджет: { $amount }

budget-console-withdraw-cash = Вывод денежных средств:

budget-console-dispense-salaries = Раздавать все зарплаты

budget-console-transfer-department = Перевод в отдел:

budget-console-amount-placeholder = Сумма

budget-console-department-entry = { $department } (Бюджет: $){ $amount })

budget-console-transfer = передача

budget-console-no-departments = Никаких отделов не найдено.

cash-vendor-credit = Кредит:

cash-vendor-amount = ${ $amount }

cash-vendor-scan-id = Идентификатор сканера

cash-vendor-clear-department = Чистый Депт

cash-vendor-return-change = Вернуться назад Изменение

cash-vendor-department-budget = Бюджет госбюджета:

cash-vendor-department-budget-value = ${ $amount } ({ $department })

cash-vendor-search-placeholder = Поиск...

cash-vendor-footer-hint = Вставьте наличные или носите свой идентификатор, затем выберите пункт.

cash-vendor-prices-include-tax = Цены, в т.ч. налог

cash-vendor-id-account = ID аккаунт:

cash-vendor-insufficient-cash = Не хватает наличных, и нет удостоверения личности, чтобы взимать с остальных.

cash-vendor-insufficient-funds = Недостаток средств на ID-карте.

cash-vendor-card-locked = Ваша ID-карта заблокирована.

cash-vendor-card-charged = ${ $amount } Списан с вашей ID-карты.

cash-vendor-sales-tax = Налог с продаж: { $percent }%

cash-vendor-no-sales-tax = Нет налога с продаж

cash-vendor-buy = Купить

cash-vendor-no-items = Никаких предметов.

cmu-economy-sales-tax = Налог с продаж

cmu-economy-income-tax = Подоходный налог

cmu-economy-transit-tariff = Транзитный тариф

cmu-economy-embargoes = Стыд

cmu-economy-trade-pacts = Торговые пакты

cmu-economy-none = Нет

cmu-corporate-heading = Корпоративные продукты

cmu-corporate-subheading = COLONY LIAISON OFFICE // Транзит и поддержка

cmu-corporate-treasury = Корпоративное Казначейство

cmu-corporate-withdraw-tax = Вывод средств { $percent }% подоходный налог колонии.

cmu-corporate-withdraw-no-tax = Выводы в настоящее время не облагаются налогом.

cmu-corporate-tariff = Транзитный тариф

cmu-corporate-current-tariff = Текущая ставка

cmu-corporate-exact = Точная ставка0-{ $max }%)

cmu-corporate-apply = ПРИМЕЧАНИЕ

cmu-corporate-tariff-announced = Каждое изменение тарифа объявляется колониальным.

cmu-corporate-support = Корпоративная поддержка

cmu-corporate-support-hint = Звонок по корпоративной безопасности, синтетикам или подрядчикам, оплачивается из корпоративного бюджета.

cmu-corporate-footer = Тарифный доход уплачивается в корпоративный бюджет с каждого представления.

cmu-budget-heading = Колонский буджет

cmu-budget-subheading = Колонная траектория // АДМИНИСТРАЦИЯ

cmu-budget-treasury = Минфин

cmu-budget-payroll = Оплата труда

cmu-budget-payroll-hint = Платит сотрудникам каждого отдела, из собственного бюджета каждого департамента.

cmu-budget-dispense = ДИСПЕНСА ВСЕХ САЛАРИЙ

cmu-budget-dispense-confirm = КОНФИРМ ПЕЙДЕЙ

cmu-budget-departments = Департаменты

cmu-budget-department-count = { $count ->
    [one] 1 ДЕПАРТМЕНТ
   *[other] { $count } ДЕПАРТМЕНТЫ
}

cmu-budget-transfer-amount = Земельный переход

cmu-budget-transfer = Трансфер

cmu-budget-department-budget = Бюджет { $amount }

cmu-budget-total = Ведомые департаментами

cmu-budget-footer = Переводы переводят деньги из бюджета колонии в бюджет департамента.

cmu-cash-vendor-credit = КРЕДИТ

cmu-cash-vendor-other = другой

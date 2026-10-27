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

colony-economy-sales-tax = Sales Tax: { $percent }%

colony-economy-income-tax = Income Tax: { $percent }%

colony-economy-transit-tariff = Transit Tariff: { $percent }%

colony-economy-active-embargoes = Active Embargoes: { $factions }

colony-economy-no-embargoes = Embargoes: None

colony-economy-active-trade-pacts = Active Trade Pacts: { $factions }

colony-economy-no-trade-pacts = Trade Pacts: None

colony-economy-overview = -- Colony Economy Overview --

colony-economy-third-party-support = -- Third Party Support --

colony-economy-open-third-party-menu = Open Third Party Menu

colony-economy-unknown-faction = Unknown Faction

colony-economy-apply = Apply

admin-console-title = Administration Console

admin-console-colony-budget = Colony Budget: ${ $amount }

admin-console-sales-tax-section = -- Sales Tax --

admin-console-current-sales-tax = Current Sales Tax: { $percent }%

admin-console-sales-tax-description = Applies to cash vendors and corporate ASRS orders. Tax revenue goes to the colony budget.

admin-console-new-tax = New Tax (0–50%):

admin-console-tax-placeholder = e.g. 10

admin-console-income-tax-section = -- Income Tax --

admin-console-current-income-tax = Current Income Tax: { $percent }%

admin-console-income-tax-description = Deducted from salary payouts and corporate cash withdrawals. Revenue goes to the colony budget.

admin-console-announcement-sender = Administration

admin-console-sales-tax-announcement = Colony sales tax has been set to { $percent }%.

admin-console-income-tax-announcement = Colony income tax has been set to { $percent }%. This affects salary payouts and corporate withdrawals.

corporate-console-title = Corporate Affairs Console

corporate-console-budget = Corporate Budget: ${ $amount }

corporate-console-withdraw-section = -- Withdraw Cash --

corporate-console-transit-tariff-section = -- Transit Tariff --

corporate-console-current-transit-tariff = Current Transit Tariff: { $percent }%

corporate-console-transit-tariff-description = A percentage of all submission storage payouts that goes to the corporate budget instead of the colony.

corporate-console-new-tariff = New Tariff (0–50%):

corporate-console-tariff-placeholder = e.g. 15

corporate-console-withdraw-tax-note = Note: Withdrawals are subject to { $percent }% income tax.

corporate-console-withdraw-no-tax = No income tax on withdrawals.

corporate-console-announcement-sender = Corporate Affairs

corporate-console-tariff-announcement = Corporate transit tariff has been set to { $percent }%. Submission payouts to the colony have been adjusted.

budget-console-title = Budget Console

budget-console-current-budget = Current Budget: { $amount }

budget-console-withdraw-cash = Withdraw Cash:

budget-console-dispense-salaries = Dispense All Salaries

budget-console-transfer-department = Transfer to Department:

budget-console-amount-placeholder = Amount

budget-console-department-entry = { $department } (Budget: ${ $amount })

budget-console-transfer = Transfer

budget-console-no-departments = No departments found.

cash-vendor-credit = Credit:

cash-vendor-amount = ${ $amount }

cash-vendor-scan-id = Scan ID

cash-vendor-clear-department = Clear Dept

cash-vendor-return-change = Return Change

cash-vendor-department-budget = Dept Budget:

cash-vendor-department-budget-value = ${ $amount } ({ $department })

cash-vendor-search-placeholder = Search...

cash-vendor-footer-hint = Insert cash or carry your ID, then select item.

cash-vendor-prices-include-tax = Prices incl. tax

cash-vendor-id-account = ID Account:

cash-vendor-insufficient-cash = Not enough cash, and no ID card to charge the rest to.

cash-vendor-insufficient-funds = Insufficient funds on your ID card.

cash-vendor-card-locked = Your ID card is locked.

cash-vendor-card-charged = ${ $amount } charged to your ID card.

cash-vendor-sales-tax = Sales Tax: { $percent }%

cash-vendor-no-sales-tax = No sales tax

cash-vendor-buy = Buy

cash-vendor-no-items = No items available.

cmu-economy-sales-tax = Sales tax

cmu-economy-income-tax = Income tax

cmu-economy-transit-tariff = Transit tariff

cmu-economy-embargoes = Embargoes

cmu-economy-trade-pacts = Trade pacts

cmu-economy-none = None

cmu-corporate-heading = CORPORATE AFFAIRS

cmu-corporate-subheading = COLONY LIAISON OFFICE // TRANSIT & SUPPORT

cmu-corporate-treasury = Corporate Treasury

cmu-corporate-withdraw-tax = Withdrawals pay { $percent }% colony income tax.

cmu-corporate-withdraw-no-tax = Withdrawals are currently untaxed.

cmu-corporate-tariff = Transit Tariff

cmu-corporate-current-tariff = Current rate

cmu-corporate-exact = EXACT RATE (0-{ $max }%)

cmu-corporate-apply = APPLY

cmu-corporate-tariff-announced = Every tariff change is announced colony-wide.

cmu-corporate-support = Corporate Support

cmu-corporate-support-hint = Call in corporate security, synthetics or contractors, paid from the corporate budget.

cmu-corporate-footer = Tariff income is paid into the corporate budget from every submission.

cmu-budget-heading = COLONY BUDGET

cmu-budget-subheading = COLONY TREASURY // ADMINISTRATION

cmu-budget-treasury = Treasury

cmu-budget-payroll = Payroll

cmu-budget-payroll-hint = Pays every department's staff now, from each department's own budget.

cmu-budget-dispense = DISPENSE ALL SALARIES

cmu-budget-dispense-confirm = CONFIRM PAYDAY

cmu-budget-departments = Departments

cmu-budget-department-count = { $count ->
    [one] 1 DEPARTMENT
   *[other] { $count } DEPARTMENTS
}

cmu-budget-transfer-amount = TRANSFER AMOUNT

cmu-budget-transfer = TRANSFER

cmu-budget-department-budget = BUDGET { $amount }

cmu-budget-total = Held by departments

cmu-budget-footer = Transfers move money from the colony budget into a department's budget.

cmu-cash-vendor-credit = CREDIT

cmu-cash-vendor-other = Other

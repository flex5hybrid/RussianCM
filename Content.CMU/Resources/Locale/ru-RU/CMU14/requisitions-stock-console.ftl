cmu-req-stock-console-order-add = +

cmu-req-stock-console-order-remove = -

cmu-req-stock-console-order-heading = БЛАНК ЗАКАЗА

cmu-req-stock-console-order-summary = {$count}/{$max}

cmu-req-stock-console-order-empty = Нажимайте + у бесплатных предметов, чтобы добавить их в бланк.

cmu-req-stock-console-order-hint = Распечатайте бланк и получите печать «ОДОБРЕНО» в отделе снабжения.

cmu-req-stock-console-order-full = Бланк заполнен.

cmu-req-stock-console-order-add-tooltip = Добавить одну единицу в бланк заказа

cmu-req-stock-console-order-remove-tooltip = Убрать одну единицу из бланка заказа

cmu-req-stock-console-order-print = Печать бланка

cmu-req-stock-console-order-clear = Очистить

cmu-req-slip-line = {$count}x {$item}

cmu-req-slip-content = [head=2]ЗАКАЗ СНАБЖЕНИЯ[/head]
    Заказчик: {$requester}

    {$items}
    Всего предметов: {$total}

    {"["}italic]Требуется печать «ОДОБРЕНО» от отдела снабжения. Примените заверенный бланк к автомату снабжения.[/italic]

cmu-req-slip-printed = {CAPITALIZE(THE($console))} стрекочет и выдаёт свежий бланк заказа.

cmu-req-slip-not-approved = Автомат не выполнит заказ без печати «ОДОБРЕНО».

cmu-req-slip-no-access = У вас нет доступа к этому автомату.

cmu-req-slip-already-filled = Этот заказ уже выполнен.

cmu-req-slip-none-in-stock = На складе нет ни одного предмета из этого заказа.

cmu-req-slip-filled = {CAPITALIZE(THE($vendor))} считывает бланк, выдаёт {$count} {$count ->
    [one] предмет
    [few] предмета
   *[other] предметов
} и отмечает заказ выполненным.

cmu-req-slip-partly-filled = Автомат выдаёт {$count} {$count ->
    [one] предмет
    [few] предмета
   *[other] предметов
}; {$left} {$left ->
    [one] предмет всё ещё отсутствует
    [few] предмета всё ещё отсутствуют
   *[other] предметов всё ещё отсутствуют
} на складе.

cmu-req-slip-completed-content = [head=2]ЗАКАЗ СНАБЖЕНИЯ — ВЫПОЛНЕН[/head]
    Заказчик: {$requester}
    Исполнитель: {$filler}

    {$items}
    Всего предметов: {$total}

    {$pays ->
        [yes] {"["}italic]Отправьте этот бланк через подъёмник АСИП, чтобы закрыть заказ.[/italic]
       *[other] {"["}italic]Копия для учёта. Заказчик {$requester} уже получил оплату за заказ в эту смену.[/italic]
    }

ent-CMUPaperRequisitionSlip = бланк заказа снабжения
    .desc = Список предметов, запрошенных в отделе снабжения. Автомат выполнит заказ только после печати «ОДОБРЕНО». После выполнения отправьте бланк через подъёмник АСИП.

ent-CMUTableReinforcedRequisitionDispenseBase = укреплённый стол выдачи снабжения
    .desc = Квадратная металлическая поверхность на массивном металлическом основании. Здесь выкладывают выполненные заказы снабжения.

ent-CMUTableReinforcedRequisitionDispenseNorth =
    .suffix = Выдача заказов, север
    .desc = { ent-CMUTableReinforcedRequisitionDispenseBase.desc }

ent-CMUTableReinforcedRequisitionDispenseSouth =
    .suffix = Выдача заказов, юг
    .desc = { ent-CMUTableReinforcedRequisitionDispenseBase.desc }

cmu-req-stock-console-window-title = Запасы реквизиций

cmu-req-stock-console-refresh-button = Обновить

cmu-req-stock-console-summary = {$vendors} {$vendors ->
    [one] продавец
   *[other] поставщики
}, {$items} предметы, {$empty} вне запасов

cmu-req-stock-console-no-vendors = Автоматических реквизиций на этом корабле не обнаружено.

cmu-req-stock-console-unlimited = ограниченный

cmu-req-stock-console-amount = {$amount} левый

cmu-req-stock-console-amount-max = {$amount} / {$max}

cmu-req-stock-console-search = Поиск

cmu-req-stock-console-of-max = из {$max}

cmu-req-stock-console-heading = Рекомендации СТОК

cmu-req-stock-console-search-placeholder = Поиск предметов...

cmu-req-stock-console-low-only = Лишь застрял

cmu-req-stock-console-footer = Живой запас поставщиков реквизиций этого корабля. Предметы на четверть или менее их начальных запасов показывают низкий уровень.

cmu-req-stock-console-tag-out = { $count } ВЫХОД

cmu-req-stock-console-tag-low = { $count } Низкий

cmu-req-stock-console-tag-ok = Все застряли

cmu-req-stock-console-status-ok = Застрять

cmu-req-stock-console-status-low = Низкий

cmu-req-stock-console-status-out = ВЫХОД

# Requisition Computer
requisition-paperwork-receiver = Логистический филиал
requisition-paperwork-reward-message = Подтверждение получено!
# Requisition Invoice
requisition-paper-print = {$name} счет-фактура
requisition-paper-print-manifest = [голова=2]
    {$containerName}[/head][bold]{$content}[/bold][head= 2]
    WT. {$weight} LBS
    LOT {$lot}
    S/N {$serialNumber}[/head]
requisition-paper-print-content = - {$count} {$item}

# Missing entries synced from en-US

requisition-paperwork-receiver-name = Логистический филиал

requisition-paper-print-name = {$name} счет-фактура

ui-supply-drop-consle-name = Консоль сброса припасов

ui-supply-drop-console-name-bolded = [bold]ПОДКЛЮЧЕНИЕ ПИТАНИЯ[/bold]

ui-supply-drop-console-longitude = Долгота:

ui-supply-drop-console-latitude = Широта:

ui-supply-drop-pad-status = [bold]Состояние панели питания[/bold]

ui-supply-drop-console-update = Обновить

ui-supply-drop-console-ready = Готов стрелять!

ui-supply-drop-console-launch = ЗАПУСК ПОСТАВКИ

ui-supply-drop-console-launch-confirmation = Подтвердить прекращение поставок?

ui-supply-drop-console-cooldown = {$time} секунд до следующего запуска

ui-supply-drop-crate-status =
    { $hasCrate ->
        [true] Supply Pad Status: crate loaded.
       *[false] No crate loaded.
    }

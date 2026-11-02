colony-rotation-announcement-sender = Колониальное управление транспортом
colony-rotation-pad-unknown = посадочная площадка колонии

colony-rotation-dispatched = Смена окончена. Шаттл для смены экипажа отправлен. Место посадки: {$pad}. Прибытие через {$minutes} {$minutes ->
    [one] минуту
    [few] минуты
    [many] минут
   *[other] минуты
}. Всем, кто уходит со смены, следует собрать вещи и направиться туда.
colony-rotation-called = Администрация колонии запросила досрочную смену экипажа. Место посадки: {$pad}. Прибытие через {$minutes} {$minutes ->
    [one] минуту
    [few] минуты
    [many] минут
   *[other] минуты
}. Всем, кто уходит со смены, следует собрать вещи и направиться туда.
colony-rotation-recalled = Транспорт для смены экипажа отозван. Следующая смена запланирована через {$minutes} {$minutes ->
    [one] минуту
    [few] минуты
    [many] минут
   *[other] минуты
}.
colony-rotation-landed = Транспорт для смены экипажа приземлился. Место посадки: {$pad}. Он улетит через {$minutes} {$minutes ->
    [one] минуту
    [few] минуты
    [many] минут
   *[other] минуты
}. До этого загрузите на борт товары для квартальной отправки.
colony-rotation-departing-minutes = Транспорт для смены экипажа улетает через {$minutes} {$minutes ->
    [one] минуту
    [few] минуты
    [many] минут
   *[other] минуты
}. Поднимайтесь на борт, если уходите со смены.
colony-rotation-departing-seconds = Транспорт для смены экипажа улетает через {$seconds} {$seconds ->
    [one] секунду
    [few] секунды
    [many] секунд
   *[other] секунды
}. Последний вызов на посадку.
colony-rotation-departed = Транспорт для смены экипажа поднялся в воздух. Смена окончена.
colony-rotation-round-end = Транспорт для смены экипажа покинул колонию.

colony-rotation-call-too-early = Транспортное управление не отправит транспорт так рано в начале смены. Попробуйте снова через {$minutes} {$minutes ->
    [one] минуту
    [few] минуты
    [many] минут
   *[other] минуты
}.
colony-rotation-call-cooldown = Транспортное управление всё ещё обрабатывает предыдущий запрос.
colony-rotation-recall-cooldown = Транспортное управление всё ещё обрабатывает предыдущий запрос.
colony-rotation-recall-committed = Транспорт уже снижается и не может быть отозван.
colony-rotation-access-denied = Доступ запрещён. Только администрация колонии может организовать смену экипажа.

colony-rotation-console-header = Транспорт смены экипажа
colony-rotation-console-call = Запросить досрочную смену
colony-rotation-console-recall = Отозвать транспорт
colony-rotation-console-scheduled = Запланировано к отправке через {$time}
colony-rotation-console-inbound = На подходе, посадка через {$time}
colony-rotation-console-approaching = На финальном заходе на посадку
colony-rotation-console-docked = На площадке, улетит через {$time}
colony-rotation-console-departing = Взлетает

colony-rotation-summary-rotated = Покинули колонию на транспорте: {$count} {$count ->
    [one] колонист
    [few] колониста
    [many] колонистов
   *[other] колониста
}:
colony-rotation-summary-stayed = В колонии остались: {$count} {$count ->
    [one] колонист
    [few] колониста
    [many] колонистов
   *[other] колониста
}:
colony-rotation-summary-no-shipment = Для квартальной отправки ничего не было загружено.
colony-rotation-summary-shipment = Квартальная отправка оценена в ${$value}:
colony-rotation-summary-shipment-line = {$count} × {$item}

colony-rotation-command-call-description = Немедленно отправляет транспорт смены экипажа, пропуская минимальную длительность смены.
colony-rotation-command-call-help = Использование: rotationcall [минут до посадки]
colony-rotation-command-recall-description = Отзывает приближающийся транспорт смены экипажа.
colony-rotation-command-recall-help = Использование: rotationrecall
colony-rotation-command-minutes-hint = <минут до посадки>
colony-rotation-command-called = Транспорт смены экипажа отправлен.
colony-rotation-command-recalled = Транспорт смены экипажа отозван.
colony-rotation-command-no-rule = В этом раунде нет транспорта смены экипажа. Он используется только в режиме «Колониальная смена».
colony-rotation-command-already-called = Транспорт смены экипажа уже в пути.
colony-rotation-command-nothing-inbound = Нет приближающегося транспорта смены экипажа для отзыва.
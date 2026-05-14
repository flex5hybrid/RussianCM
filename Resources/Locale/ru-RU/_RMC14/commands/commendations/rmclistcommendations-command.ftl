# List Commendations Command
cmd-rmclistcommendations-desc = Список похвал по раундам, игрокам, идентификаторам или недавним записям.
cmd-rmclistcommendations-help = Использование:
  rmclistcommendations last <count> [type]
    - Lists the most recent commendations
    - count: number of most recent commendations to show
    - type: type commendation filter (all default)
  
  rmclistcommendations round <roundId> [type]
    - Lists all commendations for a specific round
    - type: type commendation filter (all default)

  rmclistcommendations id <commendationId>
    - Lists a single commendation by id
  
  rmclistcommendations player giver <usernameOrId> <count> [type]
    - Lists commendations given by a player
    - count: number of most recent commendations to show
    - type: type commendation filter (all default)
  
  rmclistcommendations player receiver <usernameOrId> <count> [type]
    - Lists commendations received by a player
    - count: number of most recent commendations to show
    - type: type commendation filter (all default)
  
  Examples:
    rmclistcommendations last 10
    rmclistcommendations last 5 jelly
    rmclistcommendations round 42
    rmclistcommendations round 42 medal
    rmclistcommendations id 128
    rmclistcommendations player giver PlayerName 10
    rmclistcommendations player receiver PlayerName 5 jelly

# Errors
cmd-rmclistcommendations-invalid-arguments = Неверные аргументы!
cmd-rmclistcommendations-invalid-round-id = Неверный идентификатор раунда!
cmd-rmclistcommendations-invalid-id = Неверный идентификатор благодарности!
cmd-rmclistcommendations-invalid-type = Неверный тип «{ $type }»!
cmd-rmclistcommendations-invalid-player-mode = Неверный режим игрока! Должно быть «дающий» или «получатель».
cmd-rmclistcommendations-invalid-count = Неверный счетчик! Должно быть положительное число.
cmd-rmclistcommendations-player-not-found = Игрок «{ $player }» не найден.
cmd-rmclistcommendations-no-results = Похвалы не найдены.

# Headers
cmd-rmclistcommendations-last-header = Показаны последние похвалы { $count } (запрос: { $total }):
cmd-rmclistcommendations-round-header = Благодарности за раунд { $round } (всего { $count }):
cmd-rmclistcommendations-id-header = Благодарность { $id }:
cmd-rmclistcommendations-giver-header = Показаны последние полученные благодарности { $count } (запрос: { $total }):
cmd-rmclistcommendations-receiver-header = Показаны последние полученные благодарности { $count } (запрос: { $total }):

# Format
cmd-rmclistcommendations-format = id [{ $id }] { $type }: { $name } - { $giverUserName } ({ $giver }) → { $receiverUserName } ({ $receiver }) Раунд { $round }: { $text }

# Completion hints
cmd-rmclistcommendations-hint-mode = Режим (последний, раунд, идентификатор или игрок)
cmd-rmclistcommendations-hint-mode-last = Перечислите последние похвалы
cmd-rmclistcommendations-hint-mode-round = Список наград по раундам
cmd-rmclistcommendations-hint-mode-id = Перечислить похвалы по идентификатору
cmd-rmclistcommendations-hint-mode-player = Список похвал от игрока
cmd-rmclistcommendations-hint-round-id = Идентификатор раунда
cmd-rmclistcommendations-hint-commendation-id = Идентификатор рекомендации
cmd-rmclistcommendations-hint-player-mode = Режим игрока (дающий или получающий)
cmd-rmclistcommendations-hint-player-giver = Благодарности от игрока
cmd-rmclistcommendations-hint-player-receiver = Благодарности, полученные игроком
cmd-rmclistcommendations-hint-player = Имя пользователя или UserId игрока
cmd-rmclistcommendations-hint-count = Количество похвал для отображения
cmd-rmclistcommendations-hint-type = Тип рекомендательного фильтра

cmd-rmcdeletecommendations-desc = Удаляет похвалы по раунду, дарителю, получателю или идентификатору.
cmd-rmcdeletecommendations-help = Использование:
  rmcdeletecommendations id <commendationId>
    - Deletes a single commendation by id

  rmcdeletecommendations round <roundId> <type>
    - Deletes all commendations for a specific round and type
    - type: type commendation filter

  rmcdeletecommendations round <roundId> <type> giver <usernameOrId>
    - Deletes commendations in a round and type given by a player
    - type: type commendation filter

  rmcdeletecommendations round <roundId> <type> receiver <usernameOrId>
    - Deletes commendations in a round and type received by a player
    - type: type commendation filter

  Examples:
    rmcdeletecommendations id 128
    rmcdeletecommendations round 42 medal
    rmcdeletecommendations round 42 jelly giver PlayerName
    rmcdeletecommendations round 42 medal receiver PlayerName

cmd-rmcdeletecommendations-invalid-arguments = Неверные аргументы!
cmd-rmcdeletecommendations-invalid-round-id = Неверный идентификатор раунда!
cmd-rmcdeletecommendations-invalid-id = Неверный идентификатор благодарности!
cmd-rmcdeletecommendations-invalid-type = Неверный тип «{ $type }»!
cmd-rmcdeletecommendations-invalid-player-mode = Неверный режим игрока! Должно быть «дающий» или «получатель».
cmd-rmcdeletecommendations-player-not-found = Игрок «{ $player }» не найден.
cmd-rmcdeletecommendations-no-results = Похвалы не найдены.

cmd-rmcdeletecommendations-id-header = Удалена похвала { $id }:
cmd-rmcdeletecommendations-round-header = Удалены похвалы за Раунд { $round } (всего { $count }):
cmd-rmcdeletecommendations-format = id [{ $id }] { $type }: { $name } - { $giverUserName } ({ $giver }) → { $receiverUserName } ({ $receiver }) Раунд { $round }: { $text }
cmd-rmcdeletecommendations-admin-announcement = { $admin } удалил рекомендации с идентификатором: { $ids }
cmd-rmcdeletecommendations-admin-announcement-round = { $admin } удалил рекомендации за Раунд { $round } с ID: { $ids }

cmd-rmcdeletecommendations-hint-mode = Режим (id или раунд)
cmd-rmcdeletecommendations-hint-mode-id = Удалить похвалу по id
cmd-rmcdeletecommendations-hint-mode-round = Удаление похвал по раундам
cmd-rmcdeletecommendations-hint-round-id = Идентификатор раунда
cmd-rmcdeletecommendations-hint-commendation-id = Идентификатор рекомендации
cmd-rmcdeletecommendations-hint-type = Тип рекомендации
cmd-rmcdeletecommendations-hint-player-mode = Режим игрока (дающий или получающий)
cmd-rmcdeletecommendations-hint-player-giver = Благодарности от игрока
cmd-rmcdeletecommendations-hint-player-receiver = Благодарности, полученные игроком
cmd-rmcdeletecommendations-hint-player = Имя пользователя или UserId игрока

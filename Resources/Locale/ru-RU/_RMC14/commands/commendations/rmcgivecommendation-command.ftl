# Команда выдачи награды
cmd-rmcgivecommendation-desc = Выдаёт медаль или желе игроку
cmd-rmcgivecommendation-help = Использование: rmcgivecommendation <имя_выдающего> <получатель> <имя_получателя> <тип> <тип_награды> <цитата> [id_раунда]
  Аргументы:
  giverName: кто IC выдаёт награду (ОБЯЗАТЕЛЬНО использовать кавычки, если есть пробелы)
  receiver: имя игрока или UserId
  receiverName: имя персонажа (ОБЯЗАТЕЛЬНО использовать кавычки, если есть пробелы)
  type: medal или jelly
  commendationType: число (используйте автодополнение для просмотра доступных типов)
  citation: причина награды (ОБЯЗАТЕЛЬНО в кавычках)
  roundId: номер раунда, по умолчанию текущий (необязательно)
  
  Примеры:
    rmcgivecommendation "UNMC High Command" PlayerName "John Doe" medal 1 "За исключительную храбрость"
    rmcgivecommendation "The Queen Mother" XenoPlayer "XX-Alpha" jelly 2 "За защиту улья"
    rmcgivecommendation "UNMC High Command" PlayerName "John Doe" medal 1 "За исключительную храбрость" 42

# Ошибки
cmd-rmcgivecommendation-invalid-arguments = Неверное количество аргументов!
cmd-rmcgivecommendation-invalid-type = Неверный тип! Должно быть 'medal' или 'jelly'.
cmd-rmcgivecommendation-invalid-award-type = Неверный тип '{ $type }'! Должно быть 1-{ $max }.
cmd-rmcgivecommendation-empty-citation = Цитата не может быть пустой!
cmd-rmcgivecommendation-player-not-found = Игрок '{ $player }' не найден.

# Успех
cmd-rmcgivecommendation-success = Награда { $award } выдана { $player }!
cmd-rmcgivecommendation-admin-announcement = { $admin } выдал { $type } "{ $award }" игроку { $receiver } (персонаж: { $character }) в раунде { $round }

# Подсказки автодополнения
cmd-rmcgivecommendation-hint-giver = Имя IC выдающего (будьте внимательны при вводе IC имени)
cmd-rmcgivecommendation-hint-giver-highcommand = Стандартный выдающий для медалей морпехов
cmd-rmcgivecommendation-hint-giver-queen-mother = Стандартный выдающий для ксеножеле
cmd-rmcgivecommendation-hint-receiver = Имя игрока или UserId
cmd-rmcgivecommendation-hint-receiver-name = Имя персонажа получателя (будьте внимательны при вводе IC имени)
cmd-rmcgivecommendation-hint-type = Тип (medal или jelly)
cmd-rmcgivecommendation-hint-type-medal = Выдать медаль морпеху
cmd-rmcgivecommendation-hint-type-jelly = Выдать королевское желе ксеноморфу
cmd-rmcgivecommendation-hint-medal-type = Тип медали (1-{ $count })
cmd-rmcgivecommendation-hint-jelly-type = Тип желе (1-{ $count })
cmd-rmcgivecommendation-hint-invalid-type = Тип должен быть 'medal' или 'jelly'
cmd-rmcgivecommendation-hint-citation = Текст цитаты (будьте внимательны при вводе IC причины)
cmd-rmcgivecommendation-hint-round = ID раунда (необязательно)
cmd-rmcgivecommendation-hint-round-current = Текущий раунд

markings-search = Поиск
-markings-selection = { $selectable ->
    [0] Вы больше не можете выбрать черту.
    [one] Вы можете выбрать еще одну черту.
    *[other] Вы можете выбрать ещё { $selectable } черты.
}
markings-limits = { $required ->
    [true] { $count ->
            [-1] Выберите хотя бы одну черту.
            [0] Вы не можете выбрать ещё черту, но как-то, должны? Это баг.
            [one] Выберите одну черту.
            *[other] Выберите хотя бы одну черту и до { $count }. { -markings-selection(selectable: $selectable) }
        }
    *[false] { $count ->
            [-1] Выберите любое количество черт.
            [0] Вы больше не можете выбрать черту.
            [one] Выберите до одной черты.
            *[other] Выберите до { $count } черт. { -markings-selection(selectable: $selectable) }
        }
}
markings-reorder = Выбранные черты

humanoid-marking-modifier-respect-limits = Учитывать ограничения
humanoid-marking-modifier-respect-group-sex = Учитывать ограничение расы и пола
humanoid-marking-modifier-base-layers = Базовый слой
humanoid-marking-modifier-enable = Включить
humanoid-marking-modifier-prototype-id = ID прототипа:

# Categories

markings-organ-Torso = Туловище
markings-organ-Head = Голова
markings-organ-ArmLeft = Левая рука
markings-organ-ArmRight = Правая рука
markings-organ-HandRight = Правая кисть
markings-organ-HandLeft = Левая кисть
markings-organ-LegLeft = Левая нога
markings-organ-LegRight = Правая нога
markings-organ-FootLeft = Левая стопа
markings-organ-FootRight = Правая стопа
markings-organ-Eyes = Глаза

markings-category-Special = Особое
markings-category-Hair = Волосы
markings-category-HeadTop = Голова (Верх)
markings-category-HeadSide = Голова (Бок)
markings-category-Snout = Нос
markings-category-UndergarmentTop = Нижняя рубашка
markings-category-UndergarmentBottom = Трусы
markings-category-Chest = Туловище
markings-category-Arms = Руки
markings-category-Legs = Ноги
markings-category-Tail = Хвост
markings-category-Overlay = Наложение

markings-category-FacialHair = Растительность на лице

markings-used = Используемые метки
markings-unused = Доступные метки
markings-add = Добавить метку
markings-remove = Удалить метку
markings-rank-up = Вверх
markings-rank-down = Вниз
marking-points-remaining = Осталось меток: { $points }
marking-used = { $marking-name }
marking-used-forced = { $marking-name } (Принудительно)
marking-slot-add = Добавить
marking-slot-remove = Удалить
marking-slot = Слот { $number }

# Missing entries synced from en-US

markings-category-Head = Глава

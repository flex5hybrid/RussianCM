rmc-bioscan-ares-announcement = [color=white][font size=16][bold]ARES v3.2 Статус биосканирования[/bold][/font][/color][color=red][font size=14][bold]
    {$message}[/bold][/font][/color]
rmc-bioscan-ares = Биосканирование завершено.
  Сенсоры обнаружили { $shipUncontained ->
    [0] отсутствие
    *[other] {$shipUncontained}
  } неизвестных { $shipUncontained ->
    [0] биосигнатур
    [1] биосигнатуры
    *[other] биосигнатур
  } на корабле{ $shipLocation ->
    [none] {""}
    *[other], включая одну в {$shipLocation},
  } и { $onPlanet ->
    [0] отсутствие
    *[other] примерно {$onPlanet}
  } { $onPlanet ->
    [0] сигнатур
    [1] сигнатуру
    *[other] сигнатур
  } в других локациях{ $planetLocation ->
    [none].
    *[other], включая одну в {$planetLocation}
  }
rmc-bioscan-xeno-announcement = [color=#318850][font size=14][bold]Королева-Мать простирает свой разум через миры.
  {$message}[/bold][/font][/color]
rmc-bioscan-xeno = Детям моим и их Королеве: Чую { $onShip ->
  [0] отсутствие носителей
  [1] примерно 1 носителя
  *[other] примерно {$onShip} носителей
} в металлическом улье{ $shipLocation ->
  [none] {""}
  *[other], включая одного в {$shipLocation},
} и { $onPlanet ->
  [0] никого
  *[other] {$onPlanet}
} в других местах{ $planetLocation ->
  [none].
  *[other], включая одного в {$planetLocation}
}

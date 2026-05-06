rmc-bioscan-ares-announcement = [color=white][font size=16][bold]ARES v3.2 Статус биосканирования[/bold][/font][/color][color=red][font size=14][bold]
    {$message}[/bold][/font][/color]

rmc-bioscan-ares = Биосканирование завершено.

  Сенсоры показывают { $shipUncontained ->
    [0] отсутствие
    *[other] {$shipUncontained}
  } неизвестных форм жизни { $shipUncontained ->
    [0] сигнатур
    [1] сигнатуру
    *[other] сигнатур
  } на корабле{ $shipLocation ->
    [none] {""}
    *[other], включая одну в {$shipLocation},
  } и { $onPlanet ->
    [0] отсутствие
    *[other] приблизительно {$onPlanet}
  } { $onPlanet ->
    [0] сигнатур
    [1] сигнатуру
    *[other] сигнатур
  } обнаружено вне корабля{ $planetLocation ->
    [none].
    *[other], включая одну в {$planetLocation}
  }

rmc-bioscan-xeno-announcement = [color=#318850][font size=14][bold]Мысли Императрицы попадают в ваш разум из далёких миров.
  {$message}[/bold][/font][/color]

rmc-bioscan-xeno = Моим детям и их Королеве: я ощущаю { $onShip ->
  [0] отсутствие носителей
  [1] приблизительно 1 носителя
  *[other] приблизительно {$onShip} носителей
} в металлическом улье{ $shipLocation ->
  [none] {""}
  *[other], включая одного в {$shipLocation},
} и {$onPlanet ->
  [0] отсутствие
  *[other] {$onPlanet}
} разбросанных в других местах{$planetLocation ->
  [none].
  *[other], включая одного в {$planetLocation}
}
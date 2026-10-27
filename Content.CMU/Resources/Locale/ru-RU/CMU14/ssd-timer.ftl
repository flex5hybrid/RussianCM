cmu-ssd-timer-examined = [color=yellow]{ CAPITALIZE(SUBJECT($ent)) } { CONJUGATE-HAVE($ent) } Он спал для { $time ->
    [0] меньше минуты
    [one] { $time } минута
   *[other] { $time } минуты
}.[/color]

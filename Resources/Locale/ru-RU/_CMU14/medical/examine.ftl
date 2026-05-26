cmu-medical-examine-wound-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $wounds } на { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-fracture-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $fracture } в { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-wounds-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } раны: { $parts }.[/color]
cmu-medical-examine-fractures-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } переломы: { $parts }.[/color]
cmu-medical-examine-body-part-line = { $part }: { $conditions }.

cmu-medical-examine-wound-size-small = небольшая
cmu-medical-examine-wound-size-deep = глубокая
cmu-medical-examine-wound-size-gaping = зияющая
cmu-medical-examine-wound-size-massive = массивная

cmu-medical-examine-wound-type-burn = ожоговая рана
cmu-medical-examine-wound-type-surgery = хирургическая рана
cmu-medical-examine-wound-type-trauma = травматическая рана

cmu-medical-examine-wound-treated-prefix = обработанная
cmu-medical-examine-wound-bleeding-suffix = (кровоточит)

cmu-medical-examine-wound-full = { $treated }{ $size } { $type }{ $bleeding }

cmu-medical-examine-fracture-hairline = { $stabilized ->
    [true]  стабилизированная трещина кости
   *[other] трещина кости
}
cmu-medical-examine-fracture-simple = { $stabilized ->
    [true]  стабилизированный перелом кости
   *[other] перелом кости
}
cmu-medical-examine-fracture-compound = { $stabilized ->
    [true]  стабилизированный открытый перелом
   *[other] открытый перелом
}
cmu-medical-examine-fracture-comminuted = { $stabilized ->
    [true]  стабилизированный оскольчатый перелом
   *[other] оскольчатый перелом
}

cmu-medical-examine-eschar = обугленная ткань

cmu-medical-examine-part-head = Голова
cmu-medical-examine-part-torso = Торс
cmu-medical-examine-part-arm-left = Левая рука
cmu-medical-examine-part-arm-right = Правая рука
cmu-medical-examine-part-hand-left = Левая кисть
cmu-medical-examine-part-hand-right = Правая кисть
cmu-medical-examine-part-leg-left = Левая нога
cmu-medical-examine-part-leg-right = Правая нога
cmu-medical-examine-part-foot-left = Левая стопа
cmu-medical-examine-part-foot-right = Правая стопа
cmu-medical-examine-part-severed = ОТСЕЧЕНА

cmu-medical-examine-list-and =  и
cmu-medical-examine-list-comma-and = { $list } и { $last }

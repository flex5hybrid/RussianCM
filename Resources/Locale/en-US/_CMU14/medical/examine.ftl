cmu-medical-examine-wound-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $wounds } on { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-fracture-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $fracture } in { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-wounds-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } wounds: { $parts }.[/color]
cmu-medical-examine-fractures-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } fractures: { $parts }.[/color]
cmu-medical-examine-body-part-line = { $part }: { $conditions }.

cmu-medical-examine-wound-size-small = small
cmu-medical-examine-wound-size-deep = deep
cmu-medical-examine-wound-size-gaping = gaping
cmu-medical-examine-wound-size-massive = massive

cmu-medical-examine-wound-type-burn = burn
cmu-medical-examine-wound-type-surgery = surgical wound
cmu-medical-examine-wound-type-trauma = trauma wound

cmu-medical-examine-wound-treated-prefix = treated
cmu-medical-examine-wound-bleeding-suffix = (bleeding)

cmu-medical-examine-wound-full = a { $treated }{ $size } { $type }{ $bleeding }

cmu-medical-examine-fracture-hairline = { $stabilized ->
    [true]  a stabilized hairline fracture
   *[other] a hairline fracture
}
cmu-medical-examine-fracture-simple = { $stabilized ->
    [true]  a stabilized broken bone
   *[other] a broken bone
}
cmu-medical-examine-fracture-compound = { $stabilized ->
    [true]  a stabilized compound fracture
   *[other] a compound fracture
}
cmu-medical-examine-fracture-comminuted = { $stabilized ->
    [true]  a stabilized shattered bone
   *[other] a shattered bone
}

cmu-medical-examine-eschar = charred burn tissue

cmu-medical-examine-part-head = Head
cmu-medical-examine-part-torso = Torso
cmu-medical-examine-part-arm-left = Left arm
cmu-medical-examine-part-arm-right = Right arm
cmu-medical-examine-part-hand-left = Left hand
cmu-medical-examine-part-hand-right = Right hand
cmu-medical-examine-part-leg-left = Left leg
cmu-medical-examine-part-leg-right = Right leg
cmu-medical-examine-part-foot-left = Left foot
cmu-medical-examine-part-foot-right = Right foot
cmu-medical-examine-part-severed = SEVERED

cmu-medical-examine-list-and =  and
cmu-medical-examine-list-comma-and = { $list }, and { $last }

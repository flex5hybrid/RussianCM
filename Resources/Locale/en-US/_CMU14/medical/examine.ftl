cmu-medical-examine-wound-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $wounds } on { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-fracture-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $fracture } in { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-wounds-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } wounds: { $parts }.[/color]
cmu-medical-examine-fractures-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } fractures: { $parts }.[/color]
cmu-medical-examine-body-part-line = { $part }: { $conditions }.

# Wound description
cmu-medical-examine-wound-describe =
    { $treated ->
        [true] a treated
       *[false] a
    } { $size ->
        [small] small
        [deep] deep
        [gaping] gaping
       *[massive] massive
    } { $kind ->
        [burn] burn
        [surgery] surgical wound
       *[trauma] trauma wound
    }{ $bleeding ->
        [true]  (bleeding)
       *[false] {""}
    }

# Fracture description
cmu-medical-examine-fracture-describe =
    { $stabilized ->
        [true] a stabilized
       *[false] a
    } { $severity ->
        [hairline] hairline fracture
        [compound] compound fracture
        [comminuted] shattered bone
       *[simple] broken bone
    }

# Eschar
cmu-medical-examine-eschar = charred burn tissue

# Part names
cmu-medical-examine-part-name =
    { $symmetry ->
        [left] Left { $type }
        [right] Right { $type }
       *[none] { $type ->
            [head] Head
            [torso] Torso
           *[other] { $type }
        }
    }

# Sentence connectors
cmu-medical-examine-sentence-two = { $a } and { $b }
cmu-medical-examine-sentence-many = { $list }, and { $last }

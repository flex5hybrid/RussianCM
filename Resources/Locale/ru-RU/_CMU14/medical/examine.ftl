# Описание ран
cmu-medical-examine-wound-describe =
    { $treated ->
        [true] обработанная
       *[false] {""}
    } { $size ->
        [small] небольшая
        [deep] глубокая
        [gaping] зияющая
       *[massive] массивная
    } { $kind ->
        [burn] ожоговая рана
        [surgery] хирургическая рана
       *[trauma] механическая рана
    }{ $bleeding ->
        [true]  (кровотечение)
       *[false] {""}
    }

# Описание переломов
cmu-medical-examine-fracture-describe =
    { $stabilized ->
        [true] { $severity ->
            [hairline] зафиксированная трещина кости
            [compound] зафиксированный оскольчатый перелом
            [comminuted] зафиксированная раздробленная кость
           *[simple] зафиксированный перелом кости
        }
       *[false] { $severity ->
            [hairline] трещина кости
            [compound] оскольчатый перелом
            [comminuted] раздробленная кость
           *[simple] перелом кости
        }
    }

# Некроз(эщар)
cmu-medical-examine-eschar = обугленная ткань

# Названия частей тела
cmu-medical-examine-part-name =
    { $symmetry ->
        [left] Левая { $type ->
            [arm] рука
            [hand] кисть
            [leg] нога
            [foot] стопа
           *[other] { $type }
        }
        [right] Правая { $type ->
            [arm] рука
            [hand] кисть
            [leg] нога
            [foot] стопа
           *[other] { $type }
        }
       *[none] { $type ->
            [head] Голова
            [torso] Торс
           *[other] { $type }
        }
    }

# Соединители в перечислениях
cmu-medical-examine-sentence-two = { $a } и { $b }
cmu-medical-examine-sentence-many = { $list } и { $last }

# Шаблон строки осмотра
cmu-medical-examine-body-part-line = { $part }: { $conditions }.

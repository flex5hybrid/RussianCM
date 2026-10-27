cmu-medical-examine-wound-line = [color=red]У { POSS-ADJ($target) } { $part } { $wounds }.[/color]
cmu-medical-examine-fracture-line = [color=#dca94c]У { POSS-ADJ($target) } { $part } { $fracture }.[/color]
cmu-medical-examine-wounds-line = [color=red]Раны: { $parts }.[/color]
cmu-medical-examine-fractures-line = [color=#dca94c]Переломы: { $parts }.[/color]
cmu-medical-examine-body-part-line = { $part }: { $conditions }.
cmu-medical-detailed-examine-verb = Осмотреть травмы
cmu-medical-detailed-examine-verb-message = Внимательно осмотреть полученные травмы.
cmu-medical-detailed-examine-start = Вы начинаете осматривать { THE($target) } на наличие травм.
cmu-medical-detailed-examine-none = Очевидных травм не обнаружено.
cmu-medical-detailed-examine-window-title = Травмы — { $target }
cmu-medical-detailed-examine-window-heading = Отчёт о травмах
cmu-medical-detailed-examine-window-bleeding = Кровотечение: { $tier }

cmu-robotic-limb-material-synthetic = синтетический
cmu-robotic-limb-examine-state = протез
cmu-robotic-limb-examine-brute = вмятины на обшивке
cmu-robotic-limb-examine-burn = обгоревшая проводка
cmu-robotic-limb-detailed-state = протез
cmu-robotic-limb-detailed-brute = механические повреждения: вмятины на обшивке
cmu-robotic-limb-detailed-burn = термические повреждения: обгоревшая проводка
cmu-robotic-limb-inspect-header = Повреждения роботизированной конечности

cmu-medical-examine-and = и
cmu-medical-examine-active-bleeding = активное кровотечение
cmu-medical-examine-charred-burn-tissue = обугленная ожоговая ткань
cmu-medical-examine-severed = ОТОРВАНО
cmu-medical-detailed-examine-severed = оторвано
cmu-medical-examine-stump-tourniquet = жгут на культе
cmu-medical-examine-stump-bleeding = культя фонтанирует кровью
cmu-medical-examine-external-bleeding-detailed = внешнее кровотечение: { $tier }
cmu-medical-examine-burn-eschar-detailed = ожоговый струп: обугленная ткань
cmu-medical-examine-burn-eschar-header = Ожоговый струп
cmu-medical-examine-charred-tissue = обугленная ткань
cmu-medical-examine-arterial-bleeding-header = Артериальное кровотечение

cmu-medical-examine-wound-indef = { $stage }
cmu-medical-examine-treated-wounds = { $count ->
    [one] рана обработана
    [few] раны обработаны
   *[other] ран обработано
}

cmu-medical-examine-fracture-simple = простой перелом
cmu-medical-examine-fracture-simple-stabilized = стабилизированный простой перелом
cmu-medical-examine-fracture-compound = открытый перелом
cmu-medical-examine-fracture-compound-stabilized = стабилизированный открытый перелом
cmu-medical-examine-fracture-shattered = раздробленная кость
cmu-medical-examine-fracture-shattered-stabilized = стабилизированная раздробленная кость
cmu-medical-examine-fracture-default = сломанная кость
cmu-medical-examine-fracture-hairline = трещина кости
cmu-medical-examine-fracture-hairline-stabilized = стабилизированная трещина кости

cmu-medical-examine-wound-title-bullet = Пулевые ранения
cmu-medical-examine-wound-title-stab = Колотые ранения
cmu-medical-examine-wound-title-slash = Резаные ранения
cmu-medical-examine-wound-title-crush = Раздробленные ранения
cmu-medical-examine-wound-title-burn = Ожоги
cmu-medical-examine-wound-title-blast = Взрывные ранения
cmu-medical-examine-wound-title-fragment = Осколочные ранения
cmu-medical-examine-wound-title-surgical = Хирургические ранения
cmu-medical-examine-wound-title-default = Ранения

cmu-medical-examine-severity-minor = Лёгкая
cmu-medical-examine-severity-moderate = Умеренная
cmu-medical-examine-severity-severe = Тяжёлая
cmu-medical-examine-severity-massive = Критическая

cmu-medical-examine-treated = обработано
cmu-medical-examine-untreated = не обработано
cmu-medical-examine-bleed-tier-none = нет

cmu-medical-examine-part-head = Голова
cmu-medical-examine-part-head-left = Левая голова
cmu-medical-examine-part-head-right = Правая голова
cmu-medical-examine-part-torso = Туловище
cmu-medical-examine-part-torso-left = Левое туловище
cmu-medical-examine-part-torso-right = Правое туловище
cmu-medical-examine-part-arm = Рука
cmu-medical-examine-part-arm-left = Левая рука
cmu-medical-examine-part-arm-right = Правая рука
cmu-medical-examine-part-hand = Кисть
cmu-medical-examine-part-hand-left = Левая кисть
cmu-medical-examine-part-hand-right = Правая кисть
cmu-medical-examine-part-leg = Нога
cmu-medical-examine-part-leg-left = Левая нога
cmu-medical-examine-part-leg-right = Правая нога
cmu-medical-examine-part-foot = Ступня
cmu-medical-examine-part-foot-left = Левая ступня
cmu-medical-examine-part-foot-right = Правая ступня
cmu-medical-examine-part-tail = Хвост
cmu-medical-examine-part-tail-left = Левый хвост
cmu-medical-examine-part-tail-right = Правый хвост
cmu-medical-examine-part-other = Другое
cmu-medical-examine-part-other-left = Левая часть
cmu-medical-examine-part-other-right = Правая часть

cmu-medical-examine-bleed-tier-minor = minor

cmu-medical-examine-bleed-tier-moderate = moderate

cmu-medical-examine-bleed-tier-severe = severe

cmu-medical-examine-bleed-tier-arterial = arterial

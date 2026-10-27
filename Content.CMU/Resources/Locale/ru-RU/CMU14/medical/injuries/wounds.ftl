# V2 — ζ.M2 / ζ.M3 / ζ.M4 механики ран.

# ζ.M2 — Струп / хирургическое удаление некротических тканей.
cmu-medical-eschar-name = струп
cmu-medical-eschar-debride-success = Струп удалён.

# ζ.M3 — Многоступенчатые описания ран.

# ζ.M4 — Наложение / снятие жгута / некроз.
cmu-medical-tourniquet-applying = Наложение жгута...
cmu-medical-tourniquet-removing = Снятие жгута...

cmu-medical-bandage-no-wounds = Нет необработанных ран, которые можно перевязать.
cmu-medical-bandage-no-wounds-on-body-part = На выбранной части тела нет необработанных ран.
cmu-medical-bandage-synth-requires-repair-tools = Для ремонта механических повреждений синтетикам требуется сварочный аппарат, а для устранения ожогов — мотки кабеля.
cmu-medical-tourniquet-applied = Жгут наложен.
cmu-medical-tourniquet-removed = Жгут снят.
cmu-medical-tourniquet-already-on = На этой конечности уже наложен жгут.
cmu-medical-tourniquet-no-target = Нет конечности, на которую можно наложить жгут.
cmu-medical-tourniquet-verb-remove = Снять жгут
cmu-medical-tourniquet-necrosis = В конечности развился некроз.

cmu-medical-cast-needed = Наложите гипс, чтобы кость не срослась неправильно.
cmu-medical-cast-verb-remove = Снять гипс
cmu-medical-cast-removing = Снятие гипса...
cmu-medical-cast-removed = Гипс снят.
cmu-medical-cast-ready-remove = Гипс уже можно снять.
cmu-medical-cast-broke = Гипс раскалывается, когда кость ломается снова.
cmu-medical-cast-malunion = Кость срослась неправильно.

cmu-robotic-limb-repair-brute-start-self = Вы начинаете устранять вмятины на своей { $limb }.
cmu-robotic-limb-repair-brute-start-user = Вы начинаете устранять вмятины на { $limb } у { THE($target) }.
cmu-robotic-limb-repair-brute-start-others = { THE($user) } начинает устранять вмятины на { $limb } у { THE($target) }.
cmu-robotic-limb-repair-burn-start-self = Вы начинаете ремонтировать обгоревшую проводку в своей { $limb }.
cmu-robotic-limb-repair-burn-start-user = Вы начинаете ремонтировать обгоревшую проводку в { $limb } у { THE($target) }.
cmu-robotic-limb-repair-burn-start-others = { THE($user) } начинает ремонтировать обгоревшую проводку в { $limb } у { THE($target) }.
cmu-robotic-limb-repair-brute-finish-self = Вы устраняете вмятины на своей { $limb } с помощью { THE($tool) }.
cmu-robotic-limb-repair-brute-finish-user = Вы устраняете вмятины на { $limb } у { THE($target) } с помощью { THE($tool) }.
cmu-robotic-limb-repair-brute-finish-others = { THE($user) } устраняет вмятины на { $limb } у { THE($target) } с помощью { THE($tool) }.
cmu-robotic-limb-repair-burn-finish-self = Вы ремонтируете обгоревшую проводку в своей { $limb } с помощью { THE($tool) }.
cmu-robotic-limb-repair-burn-finish-user = Вы ремонтируете обгоревшую проводку в { $limb } у { THE($target) } с помощью { THE($tool) }.
cmu-robotic-limb-repair-burn-finish-others = { THE($user) } ремонтирует обгоревшую проводку в { $limb } у { THE($target) } с помощью { THE($tool) }.
cmu-medical-tourniquet-applied-stump = Жгут наложен на культю.
cmu-medical-tourniquet-verb-remove-stump = Снять жгут с культи

cmu-medical-tourniquet-no-selected-limb = Нет ни руки, ни ноги, чтобы нанести жгут туда, куда вы нацеливаетесь.

cmu-medical-cast-removed-early = Литье снимается до того, как кость закончит установку.

cmu-medical-splint-verb-remove = Удалить шину

cmu-medical-splint-removing = Удаление шины...

cmu-medical-splint-removed = Шпилька удаляется.

cmu-medical-body-part-picker-header = Выберите часть для перевязки

cmu-medical-body-part-picker-empty = Никаких ран от повязки.

cmu-medical-body-part-picker-entry = { $part } — { $count } { $count ->
    [one] рана
   *[other] раны
}

cmu-medical-body-part-sided = { $side } { $type }

cmu-medical-body-part-side-left = Левый

cmu-medical-body-part-side-right = Правильно.

cmu-medical-body-part-type-other = другой

cmu-medical-body-part-type-torso = Туловище

cmu-medical-body-part-type-head = Голова

cmu-medical-body-part-type-arm = Рука

cmu-medical-body-part-type-hand = Кисть

cmu-medical-body-part-type-leg = Нога

cmu-medical-body-part-type-foot = Ступня

cmu-medical-body-part-type-tail = Хвост

cmu-wound-arterial-spray = Кровь выплескивается из тебя { $part } В горячих рывках!

cmu-wound-arterial-spray-stump = Кровь брызгает из пня, где ты { $limb } Раньше было!

cmu-wound-stump-limb = { $symmetry ->
    [left] левый{" "}
    [right] правильно{" "}
   *[other] {""}
}{ $type ->
    [arm] рука
    [hand] рука
    [leg] нога
    [foot] нога
   *[other] конечность
}

cmu-internal-bleed-pain = Вы чувствуете колющую боль в своем {$part}!

cmu-shrapnel-move-pain = Вы чувствуете, как осколки копаются в вашем {$part} Когда ты двигаешься.

cmu-body-part-name = { $side ->
        [left] левый { $type }
        [right] правильно { $type }
       *[none] { $type ->
            [torso] грудь
           *[other] { $type }
        }
    }

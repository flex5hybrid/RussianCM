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

cmu-medical-tourniquet-no-selected-limb = There's no arm or leg to put a tourniquet on where you're aiming.

cmu-medical-cast-removed-early = The cast comes off before the bone has finished setting.

cmu-medical-splint-verb-remove = Remove splint

cmu-medical-splint-removing = Removing splint...

cmu-medical-splint-removed = The splint is removed.

cmu-medical-body-part-picker-header = Pick a part to bandage

cmu-medical-body-part-picker-empty = No wounds to bandage.

cmu-medical-body-part-picker-entry = { $part } — { $count } { $count ->
    [one] wound
   *[other] wounds
}

cmu-medical-body-part-sided = { $side } { $type }

cmu-medical-body-part-side-left = Left

cmu-medical-body-part-side-right = Right

cmu-medical-body-part-type-other = Other

cmu-medical-body-part-type-torso = Torso

cmu-medical-body-part-type-head = Head

cmu-medical-body-part-type-arm = Arm

cmu-medical-body-part-type-hand = Hand

cmu-medical-body-part-type-leg = Leg

cmu-medical-body-part-type-foot = Foot

cmu-medical-body-part-type-tail = Tail

cmu-wound-arterial-spray = Blood squirts out of your { $part } in hot spurts!

cmu-wound-arterial-spray-stump = Blood squirts out of the stump where your { $limb } used to be!

cmu-wound-stump-limb = { $symmetry ->
    [left] left{" "}
    [right] right{" "}
   *[other] {""}
}{ $type ->
    [arm] arm
    [hand] hand
    [leg] leg
    [foot] foot
   *[other] limb
}

cmu-internal-bleed-pain = You feel a stabbing pain in your {$part}!

cmu-shrapnel-move-pain = You feel shrapnel digging into your {$part} as you move.

cmu-body-part-name = { $side ->
        [left] left { $type }
        [right] right { $type }
       *[none] { $type ->
            [torso] chest
           *[other] { $type }
        }
    }

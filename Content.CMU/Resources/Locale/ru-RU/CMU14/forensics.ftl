cmu-forensic-tool-signature = след инструмента {$signature}

cmu-forensic-pry-marks = следы взлома, соответствующие следу инструмента {$signature}

cmu-forensic-weld-marks = следы сварки, соответствующие следу инструмента {$signature}

cmu-forensics-floor-nothing = Здесь нет крови для сканирования.

cmu-forensics-blood-stain = пятно крови

cmu-forensics-degraded-blood = старая кровь, слишком разрушенная для определения группы

cmu-forensic-scanner-copy = Копия

cmu-forensic-scanner-copy-tooltip = Копируйте в буфер обмена, затем вставьте его в поиск криминальных записей.

cmu-forensic-scanner-time-since-death = Время после смерти

cmu-forensic-scanner-matches = Рекордные матчи

cmu-forensic-scanner-no-matches = Никаких совпадений.

cmu-forensics-gunshot-residue = остатки огнестрельного оружия

cmu-forensics-death-recent = less than a minute ago

cmu-forensics-death-minutes = about {$minutes} {$minutes ->
    [one] minute
   *[other] minutes
} ago

cmu-forensics-match = Матч: {$names}

cmu-forensics-no-match = Никаких совпадений.

cmu-forensic-scanner-card-death = Смерть

cmu-forensic-scanner-card-injuries = травмы

cmu-forensic-scanner-card-evidence = Доказательства

cmu-forensic-scanner-cause-of-death = Причина смерти:

cmu-forensic-scanner-none = Не найдено

cmu-forensic-scanner-severity-minor = МИНОР

cmu-forensic-scanner-severity-moderate = современный

cmu-forensic-scanner-severity-severe = Свенер

cmu-forensic-scanner-severity-critical = КРИТИЧЕСКОЕ

cmu-forensic-scanner-severity-missing = промах

cmu-forensics-cause-decapitation = Обезглавливание

cmu-forensics-cause-blood-loss = Потеря крови

cmu-forensics-cause-gunshot = огнестрельные ранения

cmu-forensics-cause-explosive = Взрывная травма

cmu-forensics-cause-stab = ножевые ранения

cmu-forensics-cause-laceration = разрывы

cmu-forensics-cause-blunt = Травма тупой силы

cmu-forensics-cause-piercing = Проникающая травма

cmu-forensics-cause-burns = Ожоги

cmu-forensics-cause-hypothermia = гипотермия

cmu-forensics-cause-electrocution = Электрошок

cmu-forensics-cause-chemical-burns = Химические ожоги

cmu-forensics-cause-asphyxiation = удушье

cmu-forensics-cause-poisoning = отравление

cmu-forensics-cause-radiation = Радиационное отравление

cmu-forensics-cause-cellular = повреждение клеток

cmu-forensics-cause-unknown = неопределенный

cmu-forensics-wound-gunshot = огнестрельное ранение

cmu-forensics-wound-stab = ножевое ранение

cmu-forensics-wound-laceration = разрыв

cmu-forensics-wound-blunt = тупая травма

cmu-forensics-wound-burn = ожог

cmu-forensics-wound-blast = взрывное ранение

cmu-forensics-wound-shrapnel = осколочное ранение

cmu-forensics-wound-surgical = хирургический разрез

cmu-forensics-wound-bruise = синяк

cmu-forensics-wound-stump = культя

cmu-forensics-wound-generic = рана

cmu-forensics-injury-wound = { $count ->
    [one] { $kind }
   *[other] { $count } { $kind }s
} (worst: { $worst }){ $treated ->
    [yes] лечится
   *[other] {""}
}

cmu-forensics-injury-bleeding = { $tier } наружное кровотечение

cmu-forensics-injury-internal-bleeding = внутреннее кровотечение

cmu-forensics-injury-fracture = { $severity } fracture

cmu-forensics-injury-shrapnel = { $count } { $count ->
    [one] fragment
   *[other] fragments
} of shrapnel lodged

cmu-forensics-injury-missing = пропавший

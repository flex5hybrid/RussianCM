cmu-mutiny-recruit-sent = Запрос на вступление в мятеж отправлен. Получатель — { $target }.
cmu-mutiny-recruit-sworn = { $target } станет мятежником, когда начнётся мятеж.
cmu-mutiny-recruit-joined = { $target } теперь в рядах мятежников!
cmu-mutiny-recruit-declined = { $target } отклоняет предложение вступить в мятеж.
cmu-mutiny-recruit-fail-self = Вы не можете завербовать себя!
cmu-mutiny-recruit-fail-faction = Вы не можете завербовать { $target }! Цель на другой стороне.
cmu-mutiny-recruit-fail-already = { $target } уже завербован.
cmu-mutiny-recruit-fail-other-leader = { $target } уже завербован другим лидером.
cmu-mutiny-recruit-fail-synth = Вы не можете завербовать { $target }!
cmu-mutiny-recruit-fail-loyalist = Вы не можете завербовать { $target }!
cmu-mutiny-recruit-fail-unresponsive = Сейчас вы не можете завербовать { $target }.
cmu-mutiny-recruit-fail-pending = Ответ от { $target } ещё не получен.
cmu-mutiny-recruit-fail-declined = { $target } отклоняет предложение. Подождите, прежде чем повторить его.

cmu-mutiny-sworn = [color=#E0524A]Вы станете мятежником, когда начнётся мятеж. Подготовьтесь и никому не причиняйте вреда до его начала.[/color]
cmu-mutiny-sworn-released = [color=#E0524A]Вашего лидера больше нет. Вы не станете мятежником.[/color]
cmu-mutiny-invite-void = [color=#E0524A]Вы больше не можете присоединиться к этому мятежу.[/color]
cmu-mutiny-side-loyalist = [color=#5F8CC4][bold]Вы на стороне лоялистов![/bold][/color] Прочитайте правила участия в мятеже.
cmu-mutiny-side-loyalist-forced = { cmu-mutiny-side-loyalist }
cmu-mutiny-side-noncombatant = [color=#A0A0A0][bold]Вы не участвуете в бою![/bold][/color] Вы можете лечить обе стороны. Вы не должны вступать в бой, и вас не должны атаковать.

cmu-mutiny-announcement-sender = Командование
cmu-mutiny-announcement = ВНИМАНИЕ! По полученным сообщениям, начался мятеж. Код — «Задержать, арестовать, защищать».

cmu-mutiny-window-title = Мятеж
cmu-mutiny-rules = Ознакомьтесь с правилами проведения мятежей и бунтов в справочнике.
cmu-mutiny-timer = Осталось { $seconds } с. Затем — { $fallback }

cmu-mutiny-invite-heading = Вступление в мятеж
cmu-mutiny-invite-from = { $leader } предлагает вам присоединиться к мятежу.
cmu-mutiny-invite-from-job = { $leader } ({ $job }) предлагает вам присоединиться к мятежу.
cmu-mutiny-invite-body = Вы хотите стать мятежником?
cmu-mutiny-invite-sworn-note = Вы станете мятежником, когда начнётся мятеж. До его начала никому не причиняйте вреда.
cmu-mutiny-invite-open-note = Мятеж уже начался. Если вы согласитесь, вы сразу станете мятежником.
cmu-mutiny-invite-accept = Да
cmu-mutiny-invite-accept-open = Да
cmu-mutiny-invite-decline = Нет

cmu-mutiny-begin-heading = Начать мятеж?
cmu-mutiny-begin-body = Вы уверены, что хотите начать мятеж?
cmu-mutiny-begin-roster = Завербованные ({ $count })
cmu-mutiny-begin-roster-empty = Пока никто не завербован.
cmu-mutiny-begin-dead = погиб
cmu-mutiny-begin-warning = Все завербованные станут мятежниками. Остальным на вашей стороне предстоит сделать выбор. Военная полиция и командование останутся лоялистами.
cmu-mutiny-begin-confirm = Да
cmu-mutiny-begin-cancel = Нет

cmu-mutiny-side-heading = Выберите сторону
cmu-mutiny-side-from = { $leader } начинает мятеж.
cmu-mutiny-side-from-job = { $leader } ({ $job }) начинает мятеж.
cmu-mutiny-side-body = Начался мятеж. Кого вы поддержите?
cmu-mutiny-side-mutineer = Мятежники
cmu-mutiny-side-mutineer-desc = Присоединиться к мятежу.
cmu-mutiny-side-mutineer-synth = Синтетики не могут вступить в мятеж.
cmu-mutiny-side-loyalist-name = Лоялисты
cmu-mutiny-side-loyalist-desc = Выступить против мятежников.
cmu-mutiny-side-noncombatant-name = Отказаться от участия в бою
cmu-mutiny-side-noncombatant-desc = Вы можете лечить обе стороны. Вы не должны вступать в бой, и вас не должны атаковать.

ent-CMUActionMutinyBegin = Начать мятеж
    .desc = Объявить начало мятежа.

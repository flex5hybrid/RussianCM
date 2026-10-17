ent-CMUFighterManpad = ПЗРК EMBLR
    .desc = Переносной зенитный ракетный комплекс. Зарядите ракету EMBLR 70мм, возьмите оружие двумя руками и используйте действие «Поиск целей на большой высоте». Если опустить оружие или выронить его, захват цели снимается. Для поиска штурмовика используйте «Поиск целей на малой высоте».
    .suffix = IFF стрелка

ent-CMUFighterManpadGovfor = {ent-CMUFighterManpad}
    .desc = {ent-CMUFighterManpad.desc}
    .suffix = ГОВФОР

ent-CMUFighterManpadOpfor = {ent-CMUFighterManpad}
    .desc = {ent-CMUFighterManpad.desc}
    .suffix = ОПФОР

ent-CMUFighterManpadNoIFF = {ent-CMUFighterManpad}
    .desc = Переносной зенитный ракетный комплекс с отключённой блокировкой «свой-чужой». Пока оружие держат двумя руками и наведено на цель, оно автоматически атакует ЛЮБОЙ пилотируемый летательный аппарат в вашем секторе, включая дружественные. Использует боеприпасы EMBLR 70мм.
    .suffix = Без IFF

ent-CMUActionManpadAim = Поиск целей на большой высоте
    .desc = Поднять ПЗРК и искать вражескую авиацию в вашем секторе. Пуск произойдёт автоматически. Нажмите ещё раз, чтобы опустить ПЗРК. Нельзя включить вместе с поиском на малой высоте.
cmu-manpad-requires-wield = Возьмите ПЗРК в активную руку и сначала удерживайте его обеими руками.
cmu-manpad-aiming = ПЗРК поднят. Наблюдение за сектором на предмет вражеской авиации.
cmu-manpad-lowered = ПЗРК опущен. Захват отменён.
cmu-manpad-examine-safe = [color=gray]Опущен. Возьмите его обеими руками и используйте «Поиск целей на большой высоте», чтобы прикрыть ваш сектор.[/color]
cmu-manpad-examine-aimed = [color=orange]Направлен в небо. Прикрывает текущий сектор оператора.[/color]
cmu-manpad-iff-unknown = Не удаётся опознать вашу фракцию. ПЗРК остаётся на предохранителе.
cmu-manpad-empty = ПЗРК пуст. Зарядите ракету EMBLR 70 мм перед прицеливанием.
cmu-manpad-aiming-all = ПЗРК поднят. IFF ОТКЛЮЧЕНА: поражаются любые самолёты в вашем секторе, включая союзные.
cmu-manpad-examine-no-iff = [color=red]IFF обойдена. Этот комплекс может стрелять по союзным самолётам.[/color]
cmu-manpad-radar-title = ПЗРК / ВОЗДУШНОЕ ПРОСТРАНСТВО
cmu-manpad-radar-iff = IFF ВКЛ · ТОЛЬКО ВРАЖЕСКИЕ
cmu-manpad-radar-no-iff = IFF ВЫКЛ · ВСЕ САМОЛЁТЫ
cmu-manpad-radar-sector = ВАШ СЕКТОР { $sector } · целей: { $count }
cmu-manpad-radar-outside = ВНЕ ЗОНЫ ОПЕРАЦИИ · целей: { $count }
cmu-manpad-radar-empty = ПУСТО · зарядите ракету EMBLR 70 мм
cmu-manpad-radar-cycling = ПЕРЕЗАРЯДКА ПУСКОВОЙ · { $seconds } с
cmu-manpad-radar-lowered = ОПУЩЕН · используйте «Поиск целей на большой высоте» для поражения целей
cmu-manpad-radar-lock = ЗАХВАТ · пуск через { $seconds } с
cmu-manpad-radar-scanning = НАВЕДЕН · наблюдение за вашим сектором
cmu-manpad-radar-legend = Зелёный: союзник · красный: цель · жёлтый: неопознан
    Голубой: вы / ваш сектор · кольцо: захват
cmu-manpad-radar-height = { $height } м
cmu-swarm-no-core = Для призыва малого роя нужно живое ядро улья.

cmu-swarm-cooldown = Улей ещё восстанавливается после предыдущего призыва малого роя.

cmu-swarm-summoned = У ядра улья появилось особей малого роя: {$count}.

cmu-swarm-menu-title = Малый рой

cmu-swarm-menu-summon = Призвать 15 особей (перезарядка 15 минут)

cmu-swarm-menu-guard = Охранять позицию

cmu-swarm-menu-escort = Сопровождать ксеноморфа (или себя)

cmu-swarm-menu-cooldown = До готовности призыва: {$seconds} с

cmu-swarm-menu-rules = Поведение и союзники улья

cmu-swarm-enable-attacks = Включить автоматическую атаку

cmu-swarm-disable-attacks = Пассивный режим: никого не атаковать

cmu-swarm-rules-passive = Пассивный режим: особи роя перемещаются и сопровождают, не атакуя. Союзы действуют на весь ваш улей, включая будущие призывы.

cmu-swarm-rules-automatic = Автоматический режим: особи роя атакуют врагов поблизости и защищают союзников улья. Союзы действуют на весь ваш улей, включая будущие призывы.

cmu-swarm-add-ally = Заключить союз: {$faction}

cmu-swarm-remove-ally = Расторгнуть союз: {$faction}

ent-CMUActionSwarmRules = Поведение роя и союзники
    .desc = Настройте пассивное перемещение или автоматическую атаку и управляйте союзами своего улья.

ent-CMUActionSwarmMenu = Малый рой
    .desc = Откройте меню приказов малому рою: призыв, охрана позиции или сопровождение ксеноморфа.

ent-CMUActionSummonSwarm = Призвать малый рой
    .desc = Призовите пятнадцать малых особей под управлением ИИ у ядра улья. Выжившие особи сохраняются. Доступно раз в пятнадцать минут.

ent-CMUActionGuardSwarm = Метка охраны роя
    .desc = Прикажите малым особям переместиться к метке, рассредоточиться и защищать её. После боя с ближайшими врагами они вернутся на позицию.

ent-CMUActionFollowSwarm = Сопровождение роем
    .desc = Прикажите малым особям сопровождать живого ксеноморфа вашего улья под управлением игрока, включая вас, и атаковать врагов поблизости.

ent-CMUSwarmLesser = трутень малого роя
    .desc = Небольшой расходный трутень, подчиняющийся своей королеве.

ent-CMUSwarmGuardMarker = метка охраны роя
    .desc = Королева приказала малому рою защищать эту позицию.

cmu-swarm-disabled = В этом режиме игры приказы рою недоступны для этой королевы.

cmu-swarm-full = Hive swarm is full ({$limit} living lessers).

cmu-swarm-ovi-note = Ovi command link. Issued orders continue after detaching.

cmu-swarm-flag-limit = The hive has 64 flags. Replace old orders or clear player markers first.

cmu-swarm-ungrouped = Ungrouped

cmu-swarm-squad = Squad {$number}

cmu-swarm-select-squad = Select squad

cmu-swarm-assign-squad = Assign selected

cmu-swarm-select-all = Select all

cmu-swarm-select-none = Clear selection

cmu-swarm-selection-count = {$count} selected

cmu-swarm-place-order = Place order

cmu-swarm-release = Release to roam

cmu-swarm-append = Add patrol point (up to 8)

cmu-swarm-apply-aggression = Apply to selected

cmu-swarm-roam-note = Roam: 21 × 15 tiles, pauses, then rest and wake cycles.

cmu-swarm-work-note = Build / weed: one site per flag; requires plasma and a valid site.

cmu-swarm-markers-heading = Hive player markers: yellow rally, red attack, cyan defend

cmu-swarm-place-marker = Place marker

cmu-swarm-clear-markers = Clear player markers

cmu-swarm-selection-hint = Select individuals or a squad. Orders affect only selected lessers.

cmu-swarm-target-hint = Click a visible world tile or escort target; right-click cancels targeting.

cmu-swarm-member = Lesser {$number} · squad {$squad} · {$order} · {$aggression}

cmu-swarm-member-resting = {" "}(resting)

cmu-swarm-member-working = {" "}(building)

cmu-swarm-order-idle = Free roaming

cmu-swarm-order-guard = Guard

cmu-swarm-order-follow = Escort xeno

cmu-swarm-order-roam = Roam area

cmu-swarm-order-patrol = Patrol route

cmu-swarm-order-build = Build resin

cmu-swarm-order-weed = Plant weeds

cmu-swarm-order-rally = Rally

cmu-swarm-order-attack = Attack

cmu-swarm-order-defend = Defend

cmu-swarm-aggression-passive = Passive (never attack)

cmu-swarm-aggression-defensive = Defensive (engage within 3 tiles)

cmu-swarm-aggression-aggressive = Aggressive (pursue within order leash)

cmu-swarm-aggression-short-passive = Passive

cmu-swarm-aggression-short-defensive = Defensive

cmu-swarm-aggression-short-aggressive = Aggressive

cmu-swarm-structure-wall = Resin wall

cmu-swarm-structure-membrane = Resin membrane

cmu-swarm-structure-door = Resin door

ent-CMUActionPlaceSwarmOrder = Place swarm order flag
    .desc = Place the order or tactical marker prepared in the ovi swarm panel.

cmu-swarm-patrol-limit = This route already has eight points. Uncheck Add patrol point to start a new route.

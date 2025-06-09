### Основное оружие ###
ent-RMCWeaponRifleType71 = Штурмовая винтовка Type 71
ent-RMCWeaponRifleType71-desc = Основная служебная винтовка космических сил SPP. Type 71 - эргономичная и лёгкая штурмовая винтовка под патрон 5.45x39мм. В соответствии с доктриной превосходства и подавления, обладает высокой скорострельностью и коробчатым магазином большой ёмкости. Несмотря на посредственную точность, встроенный механизм снижения отдачи делает оружие удивительно управляемым при стрельбе очередями.

ent-RMCWeaponRifleType71Flamer = Штурмовая винтовка Type 71-F
ent-RMCWeaponRifleType71Flamer-desc = Менее распространённый вариант Type 71 со встроенным огнемётом повышенной мощности.

### Магазины ###
ent-RMCMagazineRifleType71 = Магазин Type 71 (5.45x39мм)
ent-RMCMagazineRifleType71-desc = Коробчатый магазин на 60 патронов 5.45x39мм.

ent-RMCMagazineRifleType71AP = Магазин Type 71 с ББП (5.45x39мм)
ent-RMCMagazineRifleType71AP-desc = Магазин с бронебойными патронами (60 патронов 5.45x39мм).

ent-RMCMagazineRifleType71HEAP = Магазин Type 71 с БОПП (5.45x39мм)
ent-RMCMagazineRifleType71HEAP-desc = Магазин с бронебойно-осколочными патронами (60 патронов 5.45x39мм).

### Патроны ###
ent-RMCCartridgeRifle545x39mm = Патрон (5.45x39мм)
ent-RMCCartridgeRifle545x39mm-desc = Стандартный патрон 5.45x39мм.

ent-RMCCartridgeRifle545x39mmAP = Патрон (5.45x39мм ББП)
ent-RMCCartridgeRifle545x39mmAP-desc = Бронебойный патрон 5.45x39мм.

ent-RMCCartridgeRifle545x39mmHEAP = Патрон (5.45x39мм БОПП)
ent-RMCCartridgeRifle545x39mmHEAP-desc = Бронебойно-осколочный патрон 5.45x39мм.

### Слоты и компоненты ###
gun-magazine-name = Магазин
rmc-aslot-barrel-name = Дульная насадка
rmc-aslot-rail-name = Рельса
rmc-aslot-stock-name = Приклад
rmc-aslot-underbarrel-name = Подствольник

### Режимы стрельбы ###
gun-mode-SemiAuto = Одиночный
gun-mode-Burst = Очередь (4 выстр.)
gun-mode-FullAuto = Автоматический

### Обвесы ###
rmc-attachment-type71-stock = Стандартный приклад Type 71
rmc-attachment-mini-flamethrower = Компактный огнемёт
rmc-attachment-burst-fire = Узел стрельбы очередями

### Действия ###
gun-reload-insert = Магазин вставлен
gun-reload-eject = Магазин извлечён
gun-fire-mode-select = Режим изменён на { $mode }
attachment-locked-underbarrel = Подствольный модуль заблокирован

### Технические термины ###
ammo-type-545x39mm = 5.45x39мм
ammo-count-type71 = { $count ->
    [one] { $count } патрон 5.45x39мм
    [few] { $count } патрона 5.45x39мм
   *[many] { $count } патронов 5.45x39мм
}
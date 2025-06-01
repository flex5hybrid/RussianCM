### Основное оружие ###
ent-RMCWeaponRifleXM40 = Штурмовая винтовка XM40
ent-RMCWeaponRifleXM40-desc = Экспериментальный прототип, предшественник линейки M54C, который так и не поступил в массовое производство. Используется только элитными подразделениями морской пехоты. Единственная винтовка в арсенале UNMC со встроенным глушителем. Совместима с магазинами M54C MK2, но имеет и собственную систему боепитания. Особенно эффективна в режиме стрельбы очередями.

### Магазины ###
ent-RMCMagazineRifleXM40AP = Магазин XM40 с ББП (10x24мм)
ent-RMCMagazineRifleXM40AP-desc = Магазин с бронебойными патронами (60 патронов 10x24мм).

ent-RMCMagazineRifleXM40HEAP = Магазин XM40 с БОПП (10x24мм)
ent-RMCMagazineRifleXM40HEAP-desc = Магазин с бронебойно-осколочными патронами (60 патронов 10x24мм).

### Слоты и компоненты ###
gun-magazine-name = Магазин
rmc-aslot-barrel-name = Дульная насадка
rmc-aslot-underbarrel-name = Подствольник

### Режимы стрельбы ###
gun-mode-SemiAuto = Одиночный
gun-mode-Burst = Очередь
gun-mode-FullAuto = Автоматический

### Особенности ###
xm40-integrated-suppressor = Встроенный глушитель
xm40-burstfire-lethality = Повышенная эффективность в режиме очереди
xm40-elite-only = Только для элитных подразделений

### Действия ###
gun-reload-insert = Магазин вставлен
gun-reload-eject = Магазин извлечён
gun-fire-mode-select = Режим изменён на { $mode }
attachment-locked-barrel = Глушитель зафиксирован

### Технические термины ###
ammo-type-10x24mm = 10x24мм
ammo-count-xm40 = { $count ->
    [one] { $count } патрон 10x24мм
    [few] { $count } патрона 10x24мм
   *[many] { $count } патронов 10x24мм
}
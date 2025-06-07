### Основное оружие ###
ent-WeaponRifleSSG45 = Штурмовая винтовка SSG-45
ent-WeaponRifleSSG45-desc = Разработана Weston-Yamada как современная замена устаревшей платформе M5. Специальная штурмовая винтовка (SpezialSturmGewehr) создана для точного боя на средних и дальних дистанциях. Использует безгильзовые патроны 7x62mm, обеспечивая повышенную останавливающую силу и эффективность боеприпасов по сравнению с M54C MK2, сохраняя при этом сопоставимые характеристики в ближнем бою. Частные военные компании Weston-Yamada сочли винтовки UNMC M54C недостаточно эффективными для своих операций, особенно в миссиях на большой дистанции, где критичны точность и огневая мощь. Это привело к разработке SSG, оптимизированной под специфические задачи оперативников.

### Магазины ###
ent-RMCMagazineRifleSSG45 = Магазин SSG45 (7x62мм)
ent-RMCMagazineRifleSSG45-desc = Стандартный магазин на 30 патронов 7x62мм для винтовки SSG-45.

ent-RMCMagazineRifleSSG45Extended = Удлинённый магазин SSG45 (7x62мм)
ent-RMCMagazineRifleSSG45Extended-desc = Увеличенный магазин на 45 патронов 7x62мм.

ent-RMCMagazineRifleSSG45AP = Магазин SSG45 с ББП (7x62мм)
ent-RMCMagazineRifleSSG45AP-desc = Магазин с бронебойными патронами (30 патронов 7x62мм).

ent-RMCMagazineRifleSSG45HEAP = Магазин SSG45 с БОПП (7x62мм)
ent-RMCMagazineRifleSSG45HEAP-desc = Магазин с бронебойно-осколочными патронами (30 патронов 7x62мм).

ent-RMCMagazineRifleSSG45Incend = Магазин SSG45 с зажигательными (7x62мм)
ent-RMCMagazineRifleSSG45Incend-desc = Магазин с зажигательными патронами (30 патронов 7x62мм).

### Обвесы ###
rmc-attachment-barrel-charger = Дульный ускоритель
rmc-attachment-extended-barrel = Удлинённый ствол
rmc-attachment-ssg45-stock = Стандартный приклад SSG45

### Действия ###

attachment-locked-stock = Приклад зафиксирован и не может быть заменён

### Технические термины ###
ammo-type-7x62mm = 7x62мм безгильзовый
ammo-count-ssg45 = { $count ->
    [one] { $count } патрон 7x62мм
    [few] { $count } патрона 7x62мм
   *[many] { $count } патронов 7x62мм
}

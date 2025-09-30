### Основное оружие ###
ent-WeaponRifleXM88 = Тяжёлая винтовка XM88
ent-WeaponRifleXM88-desc = Экспериментальная переносная противо-материальная винтовка под патрон .458 SOCOM. Требует ручного перезаряжения после каждого выстрела.

### Боеприпасы ###

ent-RMCBox458SOCOM = Ящик патронов .458 SOCOM
ent-RMCBox458SOCOM-desc = Ящик с патронами .458 SOCOM для тяжёлой винтовки XM88.

### Характеристики ###
xm88-features = 
    • Ручное перезаряжение
    • Мощный противо-материальный патрон
    • Ограниченная ёмкость (9 патронов)

### Слоты и компоненты ###
gun-magazine-name = Магазин
rmc-aslot-barrel-name = Дульная насадка
rmc-aslot-rail-name = Рельса
rmc-aslot-underbarrel-name = Подствольник
rmc-aslot-stock-name = Приклад

### Режимы стрельбы ###
gun-mode-SemiAuto = Одиночный

### Камуфляж ###
camouflage-variation-Jungle = Джунгли
camouflage-variation-Snow = Снег
camouflage-variation-Desert = Пустыня
camouflage-variation-Urban = Город
camouflage-variation-Classic = Классика

### Действия ###
gun-reload-insert = Патрон заряжен
gun-pump-action = Затвор перезаряжен
gun-wield-delay = Готовность к стрельбе: 0.4с
gun-heavy-penalty = Замедление при ношении: -27.5%

### Технические термины ###
ammo-type-458socom = .458 SOCOM
ammo-count-xm88 = { $count ->
    [one] { $count } патрон
    [few] { $count } патрона
   *[many] { $count } патронов
} .458 SOCOM
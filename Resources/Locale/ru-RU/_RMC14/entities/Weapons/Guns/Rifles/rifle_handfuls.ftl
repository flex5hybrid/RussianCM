### Патроны .458 SOCOM ###
ent-RMCCartridge458SOCOM = Горсть патронов .458 SOCOM
ent-RMCCartridge458SOCOM-desc = Несколько крупнокалиберных патронов .458 SOCOM, готовых к использованию.

### Формы множественного числа ###
ammo-count-458socom = { $count ->
    [one] { $count } патрон .458 SOCOM
    [few] { $count } патрона .458 SOCOM
   *[many] { $count } патронов .458 SOCOM
}

stack-458socom-count = { $count ->
    [one] 1 патрон
    [few] { $count } патрона
   *[many] { $count } патронов
}
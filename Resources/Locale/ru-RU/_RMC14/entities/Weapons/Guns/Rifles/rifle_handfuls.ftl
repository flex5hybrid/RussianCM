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

## Strings for the "grant_connect_bypass" command.

cmd-grant_connect_bypass-desc = Временно разрешить пользователю обходить регулярные проверки соединения.
cmd-grant_connect_bypass-help = Использование:grant_connect_bypass <user> [duration minutes]
    Temporarily grants a user the ability to bypass regular connections restrictions.
    The bypass only applies to this game server and will expire after (by default) 1 hour.
    They will be able to join regardless of whitelist, panic bunker, or player cap.

cmd-grant_connect_bypass-arg-user = <user>
cmd-grant_connect_bypass-arg-duration = [duration minutes]

cmd-grant_connect_bypass-invalid-args = Ожидается 1 или 2 аргумента
cmd-grant_connect_bypass-unknown-user = Не удалось найти пользователя «{ $user }»
cmd-grant_connect_bypass-invalid-duration = Неверная продолжительность «{ $duration }».

cmd-grant_connect_bypass-success = Успешно добавлен обход для пользователя «{ $user }».

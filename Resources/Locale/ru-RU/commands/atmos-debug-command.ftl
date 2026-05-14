cmd-atvrange-desc = Устанавливает диапазон отладки Atmos (в виде двух чисел с плавающей запятой: начинается [red] и заканчивается [blue]).
cmd-atvrange-help = Использование: { $command } <start> <end>
cmd-atvrange-error-start = Плохой поплавок СТАРТ
cmd-atvrange-error-end = Плохой поплавок КОНЕЦ
cmd-atvrange-error-zero = Масштаб не может быть нулевым, так как это приведет к делению на ноль в AtmosDebugOverlay.

cmd-atvmode-desc = Устанавливает режим отладки Atmos. Это автоматически сбросит весы.
cmd-atvmode-help = Использование: { $command } <TotalMoles/GasMoles/Temperature> [<gas ID (for GasMoles)>]
cmd-atvmode-error-invalid = Неверный режим
cmd-atvmode-error-target-gas = Для этого режима должен быть предусмотрен целевой газ.
cmd-atvmode-error-out-of-range = Идентификатор газа не поддается анализу или находится вне диапазона.
cmd-atvmode-error-info = Для этого режима не требуется никакой дополнительной информации.

cmd-atvcbm-desc = Меняется с красного/зеленого/синего на оттенки серого.
cmd-atvcbm-help = Использование: { $command } <true/false>
cmd-atvcbm-error = Неверный флаг

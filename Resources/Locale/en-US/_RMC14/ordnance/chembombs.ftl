# Chembomb casing interaction strings
rmc-chembomb-full = The casing is already full!
rmc-chembomb-beaker-empty = The container is empty.
rmc-chembomb-fill = Transferred {$amount}u. ({$total}/{$max}u)
rmc-chembomb-examine-volume = Chemical fill: {$current}/{$max}u
rmc-chembomb-examine-detonator = [color=green]Detonator assembly installed.[/color]
rmc-chembomb-examine-no-detonator = [color=red]No detonator assembly.[/color]
rmc-chembomb-examine-locked = [color=orange]ARMED[/color]

# Slot labels
rmc-chembomb-detonator-slot = Detonator Assembly

# Explosive reagents
reagent-name-rmc-cyclonite = Cyclonite (RDX)
reagent-desc-rmc-cyclonite = A white crystalline solid used as an explosive. One of the most powerful military explosives known.

reagent-name-rmc-anfo = ANFO
reagent-desc-rmc-anfo = Ammonium Nitrate/Fuel Oil mixture. A commonly used bulk industrial explosive.

reagent-name-rmc-nitroglycerin = Nitroglycerin
reagent-desc-rmc-nitroglycerin = An extremely sensitive oily liquid explosive. Handle with care.

reagent-name-rmc-octogen = Octogen (HMX)
reagent-desc-rmc-octogen = A powerful explosive compound, more stable than RDX but with higher performance.

reagent-name-rmc-ammonium-nitrate = Ammonium Nitrate
reagent-desc-rmc-ammonium-nitrate = A white crystalline solid used as a fertilizer and explosive precursor.

reagent-name-rmc-potassium-hydroxide = Potassium Hydroxide
reagent-desc-rmc-potassium-hydroxide = A strong alkali used in various chemical processes.

reagent-name-rmc-hexamine = Hexamine
reagent-desc-rmc-hexamine = An organic compound used as a fuel tablet and explosive precursor. Burns with a clean, hot flame.

reagent-name-rmc-potassium-chloride = Potassium Chloride
reagent-desc-rmc-potassium-chloride = A metal halide salt used in medicine and industry.

reagent-name-rmc-sodium-chloride = Sodium Chloride
reagent-desc-rmc-sodium-chloride = Common salt. Used in everything from cooking to chemical synthesis.

# Demolitions simulator
rmc-demolitions-sim-no-casing = No casing found in active hand.
rmc-demolitions-sim-not-casing = The held item is not a chembomb casing.
rmc-demolitions-sim-header = [bold]Simulation: {$name}[/bold]
rmc-demolitions-sim-volume = Chemicals loaded: {$current}/{$max}u
rmc-demolitions-sim-empty = [color=gray]No explosive or incendiary chemicals detected.[/color]
rmc-demolitions-sim-explosion = [bold]Blast:[/bold] Power {$power}, Falloff {$falloff}, Est. radius ~{$radius} tiles
rmc-demolitions-sim-fire = [bold]Fire:[/bold] Intensity {$intensity}, Radius {$radius} tiles, Duration {$duration}s

# Assembly tool steps
rmc-chembomb-seal-no-detonator = Insert a detonator assembly first.
rmc-chembomb-seal-disarm-first = Disarm the casing before unsealing.
rmc-chembomb-arm-seal-first = Seal the casing with a screwdriver first.
rmc-chembomb-sealed = You screw the casing shut.
rmc-chembomb-unsealed = You unscrew the casing.
rmc-chembomb-armed = You prime the detonator. The casing is armed.
rmc-chembomb-disarmed = You cut the arming wire. The casing is safe.
rmc-chembomb-not-armed = The casing is not ready. Complete the assembly first.

# Examine stage
rmc-chembomb-examine-open = Unsealed.
rmc-chembomb-examine-sealed = [color=yellow]Sealed.[/color] Prime with wirecutters.

# Mine casing deployment
rmc-mine-no-detonator = No detonator assembly installed.
rmc-mine-deploy-fail-occupied = There is already a mine here!
rmc-mine-planted = Mine planted.

# Ordnance part assembly (combine igniter/timer → detonator assembly)
rmc-ordnance-assembly-incompatible = These parts are not compatible.
rmc-ordnance-assembly-combined = You combine the parts into a {$result}.

# 84mm rocket assembly
rmc-rocket-warhead-not-armed = The warhead is not armed. Seal it with a screwdriver and prime with wirecutters first.
rmc-rocket-warhead-no-detonator = No detonator assembly installed in the warhead.
rmc-rocket-tube-no-fuel = The rocket tube has no fuel. Fill it with 60u of methane or hydrogen first.
rmc-rocket-tube-insufficient-fuel = Not enough fuel. Need {$required}u, have {$current}u.
rmc-rocket-assembling = Assembling rocket...
rmc-rocket-assembled = Rocket assembled. Load it into an M5-ATL launcher.
rmc-rocket-assembly-failed = Assembly failed. Check the warhead status.

# 80mm mortar shell assembly
rmc-mortar-shell-warhead-not-armed = The mortar warhead is not armed. Complete the assembly process first.
rmc-mortar-shell-warhead-no-detonator = No detonator assembly installed in the mortar warhead.
rmc-mortar-shell-no-fuel = The mortar shell casing has no fuel. Fill it with 60u of hydrogen first.
rmc-mortar-shell-insufficient-fuel = Not enough fuel in shell casing. Need {$required}u, have {$current}u.
rmc-mortar-shell-assembling = Assembling mortar shell...
rmc-mortar-shell-assembled = Mortar shell assembled. Load it into the mortar.
rmc-mortar-shell-assembly-failed = Assembly failed. Check the warhead status.

# Chemical explosion warning (SSCM13 parity)
rmc-chemical-explosion-warning = The explosive mixture detonates!

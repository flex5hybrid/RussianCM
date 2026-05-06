# Ordnance Simulators And Yield Flow

## Purpose

The `_RuMC14` ordnance tools now share a single yield estimation path so the following features stay numerically aligned:

- `RMCChembombSystem` live detonation logic
- `RMCDemolitionsSimulatorSystem` handheld replay simulator
- `RMCExplosionSimulatorSystem` console-based beaker simulator
- `RMCDemolitionsScanner` projected casing readout

## Shared Yield Estimator

`Content.Server/_RuMC14/Ordnance/RMCOrdnanceYieldEstimator.cs` is the single source of truth for custom ordnance chemistry output.

It converts a reagent solution plus a yield profile into:

- explosive output
  - `TotalIntensity`
  - `BlastFalloff`
  - `IntensitySlope`
  - `MaxIntensity`
- incendiary output
  - `FireIntensity`
  - `FireRadius`
  - `FireDuration`
  - `FireEntity`

`RMCOrdnanceYieldProfile` is built either from a live `RMCChembombCasingComponent` or from the fixed console profile used by the explosion simulator computer.

## Explosion Simulator Computer

Files:

- `Content.Server/_RuMC14/Ordnance/RMCExplosionSimulatorSystem.cs`
- `Content.Client/_RuMC14/Ordnance/RuMCExplosionSimulatorBui.cs`
- `Content.Client/_RuMC14/Ordnance/RuMCExplosionSimulatorWindow.xaml`
- `Content.Client/_RuMC14/Ordnance/RuMCExplosionSimulatorWindow.xaml.cs`

Flow:

1. A beaker is inserted into the machine item slot.
2. The operator chooses a target formation.
3. The server samples the beaker with the shared yield estimator.
4. The UI shows a two-minute analysis cycle.
5. Replay spawns a disposable chamber map, target dummies, and a detached camera.
6. After a short delay the queued explosion is replayed in the chamber.

Movement input exits the detached replay camera and returns the operator to their normal eye target.

## Handheld Demolitions Simulator

Files:

- `Content.Server/_RuMC14/Ordnance/RMCDemolitionsSimulatorSystem.cs`
- `Content.Shared/_RuMC14/Ordnance/RuMCDemolitionsSimulatorComponent.cs`

Flow:

1. The user holds a custom casing in their active hand.
2. The simulator estimates the blast and fire profile with the shared estimator.
3. A fresh disposable chamber is rebuilt for the replay.
4. A delayed explosion is queued so the viewer sees the room before detonation.

## Demolitions Scanner

Files:

- `Content.Server/_RuMC14/Ordnance/RMCChembombSystem.cs`
- `Content.Shared/_RuMC14/Ordnance/RuMCDemolitionsScannerUI.cs`
- `Content.Client/_RuMC14/Ordnance/RuMCDemolitionsScannerWindow.xaml`

The scanner does not detonate anything. It uses the same estimator to present:

- casing stage
- detonator presence
- chemical volume
- projected blast envelope
- projected incendiary spread

Because it shares the same estimator as live detonation and both simulators, scanner numbers should match gameplay instead of drifting.

## Payload Assembly Notes

Files:

- `Content.Server/_RuMC14/Ordnance/RMCOrdnancePayloadAssemblySystem.cs`
- `Content.Shared/_RuMC14/Ordnance/RuMCOrdnancePayloadAssemblyComponent.cs`

Rocket and mortar payload assembly is data-driven:

- the shell or rocket body defines the fuel solution and optional required fuel reagent
- the armed chemical casing defines the payload chemistry
- the payload assembly system maps the inserted casing prototype to the final assembled ammo prototype

Only armed casings with an active detonator and the correct fueled body are allowed to assemble.

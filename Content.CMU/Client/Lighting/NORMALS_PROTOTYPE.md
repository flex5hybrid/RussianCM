# Normal-aware lighting: test implementation

This is an **opt-in experimental renderer** for CMU Cinematic Lighting V4, not a global replacement for Robust lighting. Native LightRenderTarget, game authority, and FOV remain unchanged.

## What is actually implemented

1. A networked, opt-in CMUCinematicNormal component selects which entities may receive extra per-pixel highlight.
2. The client finds a matching generated RSI using the path transformation:
   /Textures/CMU14/Items/laptop.rsi -> /Textures/_GeneratedNormalMaps/CMU14/Items/laptop.rsi.
3. It samples the current normal RSI state, animation frame, and direction and draws each enabled sprite layer into a **world-oriented** RGBA normal target. Rotated / mirrored sprite axes are transformed into a common world basis.
4. A separate high-dynamic-range target accumulates a *positive directional excess* from up to eight existing cinematic lights using the same visibility polygon and light masks as the V4 spill renderer.
5. The final V4 composite adds this weak extra response to scene RGB only where native FOV / non-space stencil allows it. Native lighting is neither modified nor subtracted. Quality Off and the CVar Off retain the original path.

The experiment is deliberately conservative: the system handles up to 64 opt-in sprites, skips containers / special visibility masks, and does not try to relight all normal game sprites. Some occlusion between overlapping sprites is not yet modeled in the normal buffer.

## Run a test

Use the branch of PR #576 or checkout the changes after PR #575. Start the normal game client and local test server, and find a room with at least one **CMU cinematic emitter**. This effect operates on V4's cinematic light list, not every native PointLight.

In the **client console**:

~~~text
cvar cmu.lighting.cinematic_quality 2
cvar cmu.lighting.cinematic_normals true
~~~

With a mapping/admin spawn command, spawn prototype CMUCinematicNormalDemoLaptop near the lamp. The generated four-state normal RSI for the laptop is included in this PR.

Inspect data:

~~~text
cmu_cinematic_debug Normals
cmu_cinematic_debug NormalLight
cmu_cinematic_debug final
~~~

Move the laptop or change the relative light position. Compare to:

~~~text
cvar cmu.lighting.cinematic_normals false
~~~

The normal-related render targets and their shaders are disposed when the quality or normal CVar is disabled. The default is disabled.

## Designed boundaries / known limitations

- This is **not** full PBR or global illumination, and it deliberately does not cast new shadows from fake height maps.
- The normal-aware effect is a weak additive orientation highlight, not a physically conserved re-lighting model.
- Only opt-in sprites can participate. For complex overlapping sprite composites the normal buffer may not exactly reproduce native sprite depth order; do not opt into live entities until validated.
- The source is an approximate height-from-image normal generator. Its output is not true geometric ground truth.
- Local fixture height is a constant approximation in normal-light.swsl. Accurate per-object and per-lamp heights are future work.
- World-space basis transforms and sRGB decoding need visual checking with real image/video captures, especially mirrored sprites and different GPU drivers.
- The example wire prototype does not have its generated RSI committed; generate it explicitly using the script in PR #575 before using the wire demo.

Before merge, run the C# client compile workflow, asset validation and a manual A/B visual test in a real local client. Avoid enabling by default in production.

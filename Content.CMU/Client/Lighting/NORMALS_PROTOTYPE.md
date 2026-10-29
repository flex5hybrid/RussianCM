# Normal-aware lighting: test implementation

This is an **opt-in experimental renderer** for CMU Cinematic Lighting V4, not a global replacement for Robust lighting. Native LightRenderTarget, game authority, and FOV remain unchanged.

## What is actually implemented

1. A networked, opt-in CMUCinematicNormal component selects which entities may receive extra per-pixel highlight.
2. The client finds a matching generated RSI using the path transformation:
   /Textures/CMU14/Items/laptop.rsi -> /Textures/_GeneratedNormalMaps/CMU14/Items/laptop.rsi.
3. It samples the current normal RSI state, animation frame, and direction and draws each enabled sprite layer into a **world-oriented** RGBA normal target. Rotated / mirrored sprite axes are transformed into a common world basis.
4. A separate high-dynamic-range target accumulates tunable directional highlights from up to eight cinematic lights in explicit mode or 16 in mass mode, using the same visibility polygon and light masks as the V4 spill renderer.
5. The final V4 composite adds this configurable extra response to scene RGB only where native FOV / non-space stencil allows it. Native lighting is neither modified nor subtracted. Quality Off and the CVar Off retain the original path.

The renderer supports **two client-only modes**, both off by default:

- **Explicit mode** (normal CVar enabled, mass CVar disabled): at most 64 entities with `CMUCinematicNormal`, 8 cinematic lights.
- **Mass-test mode** (both CVars enabled): spatially query visible `SpriteComponent` entities, automatically match existing generated normal RSI states, prefer explicit opt-in objects, then select the nearest eligible objects deterministically. The budget is 256 sprites (up to four layers each) and 16 cinematic lights per viewport.

Mass mode still excludes hidden/container-occluded/special-visibility entities; does not enumerate all entities in a map; caches missing normal RSI paths to avoid repeated resource lookup; and only samples already-generated assets. It never generates textures at runtime. Sorting follows draw-depth and render-order, then approximate Y-sorting; perfect native ordering of all overlapping sprites is not yet guaranteed. The normal buffer is computed every frame to follow animations.

**Important:** the test budget selects 256 nearest *eligible* objects, not literally every sprite on screen. Generated normals must already exist under `_GeneratedNormalMaps` in either texture root.

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

## Mass-test all supported sprites (local client only)

Install the generator's dependencies and generate a representative subset before launching the game:

~~~powershell
python -m pip install -r Tools/normal_maps/requirements.txt
python Tools/normal_maps/generate.py --paths Resources/Textures/_RMC14/Objects Resources/Textures/_RMC14/Structures Content.CMU/Resources/Textures/CMU14
~~~

For a full-repository experiment, `python Tools/normal_maps/generate.py --all` is available but can generate **thousands** of PNGs and grow the checkout considerably. Use a separate worktree / experimental branch. Remember: generated assets must be included in the packaged resources used by the client; do not deploy them to production without review.

Start the local game with the compiled client and server, find a region with CMU cinematic lamps, then enter these **client** console commands:

~~~text
cvar cmu.lighting.cinematic_quality 2
cvar cmu.lighting.cinematic_normals true
cvar cmu.lighting.cinematic_normals_all true
cmu_cinematic_debug Normals
cmu_cinematic_debug NormalLight
cmu_cinematic_debug final
cmu_cinematic_stats
~~~

`cmu_cinematic_stats` reports `selected`, `rendered`, `layers` for the normal pass. If rendered is zero, either no normal RSI matches the scene or there are no visible eligible sprites. Test moving the camera and moving lamps independently, comparing to `cvar cmu.lighting.cinematic_normals_all false` (explicit only) and `cvar cmu.lighting.cinematic_normals false` (fully disabled).

### Real-time normal-light tuning

The normal renderer exposes five client-only CVars. They are read each light pass, so changing them in the client console takes effect without a C# rebuild:

~~~text
cvar cmu.lighting.cinematic_normals_gain 1.25
cvar cmu.lighting.cinematic_normals_height 0.45
cvar cmu.lighting.cinematic_normals_contrast 3.0
cvar cmu.lighting.cinematic_normals_flatness 0.7
cvar cmu.lighting.cinematic_normals_max_boost 1.0
~~~

Defaults are gain 1.0, height 0.45, contrast 3.0, flatness 0.7, max boost 1.0. For a *strict* per-pixel directional difference, set flatness to 1.0. Lower flatness intentionally introduces a baseline glow; using it as a relief diagnostic can be misleading. Compare `cmu_cinematic_debug NormalLight` and `final` against `cvar cmu.lighting.cinematic_normals false`; leave normals disabled by default on production clients.

For a stronger but bounded experiment, try gain 1.5 / contrast 4.0. If the result becomes noisy or washed out, return to gain 0.8 / contrast 2.0; reduce height towards 0.3 to emphasize the side-facing response. Values are clamped client-side.

**Performance:** start with High (2), compare against Off at the same camera position; profile GPU time independently because `cmu_cinematic_stats` CPU time is not a GPU timer. Do not enable the bulk setting on live rounds before measuring scene-correctness and performance.

## Designed boundaries / known limitations

- This is **not** full PBR or global illumination, and it deliberately does not cast new shadows from fake height maps.
- The normal-aware effect is an additive orientation highlight, not a physically conserved re-lighting model. High gain/contrast or flatness below 1 may brighten entire sprite faces instead of producing only relief; tune and compare in final mode.
- Only opt-in sprites participate unless the mass CVar is enabled. The mass test is capped at 256 sprites / 16 lights and may not exactly reproduce native sprite depth order for composites.
- The source is an approximate height-from-image normal generator. Its output is not true geometric ground truth.
- Local fixture height is a tunable approximation (CVar). Accurate per-object and per-lamp heights are future work.
- World-space basis transforms and sRGB decoding need visual checking with real image/video captures, especially mirrored sprites and different GPU drivers.
- The example wire prototype does not have its generated RSI committed; generate it explicitly using the script in PR #575 before using the wire demo.

Before merge, run the C# client compile workflow, asset validation and a manual A/B visual test in a real local client. Avoid enabling by default in production.

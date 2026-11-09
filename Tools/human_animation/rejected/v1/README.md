# Human 2D animation draft

This first pass adds twelve procedural presentations to `AppearanceCMUHuman`. It uses the existing
32-pixel human artwork, rather than replacing character customization with a pre-rendered actor.
The client option is **off by default** pending live art, equipment and frame-time review.

Enable locally in the client console:

```text
cvar cmu.human_animations true
```

Use `false` to disable it. Disabling removes only this system's post effect.

| Clip | Trigger |
| --- | --- |
| Idle | Conscious, standing, stationary human; breathing is staggered between entities. |
| Walk | Actual walking displacement together with directional movement input. |
| Run | Actual displacement with sprint input. |
| HeavyWalk | Two-handed item or the existing reduced sprint-speed modifier. |
| CombatWalk | Combat mode, or a wielded gun; no free arm swing. |
| Limp | Public missing-leg state, before the other standing gait choices. No private pain or diagnosis data is read. |
| Crawl | Conscious, prone, actively moving human. |
| Melee | Existing client lunge/attack presentation, including attacks with zero lunge displacement. |
| Shoot | Successful client shooting presentation, after cancellation checks; empty clicks do not trigger it. |
| Interact | Uncancelled, unfinished do-after requiring hands. |
| Fall | Standing state becomes prone; brief compression alongside the existing rotation visualizer. |
| GetUp | Prone state becomes standing; brief straightening alongside the existing rotation visualizer. |

Posture transitions take priority over combat impulses. Dead/unconscious and stationary prone bodies
settle to their original artwork. Buckled riders, surrender and active exercise keep their existing
presentations. Reparenting, large teleports and streaming gaps do not advance the stride clock.
Walking cadence follows actual distance, so running into a wall does not produce walking-in-place.

The shader deforms the already composed, lit sprite in source pixel increments. Clothing, gender
displacement, markings, tints and in-hands are composed normally first. Only this named post effect
is installed/removed; no limb layers, equipment, sprite rotation/offset or gameplay state are replaced.
The pass runs before water, stealth and interaction outlines. Its body coordinate basis is projected
per viewport and follows sprite scale, camera rotation and prone rotation.

## Review artifacts

From the repository root, using .NET 10 and Python with Pillow and numpy:

```powershell
dotnet run --project Tools/human_animation/ReviewExport -- Tools/human_animation/generated/poses.json
python Tools/human_animation/render_review.py
```

`generated/human-animations.gif` shows all twelve clips, all four directions, a male marine in armor,
and a female human in a uniform. The combat/shot examples include a rifle. Samples are exported by
compiling the production C# sampler directly. The Python renderer is a CPU reference of the shader's
pixel warp; the fall/get-up/prone rotation represents the existing game visualizer.
**This is an offline art study, not a recording of live gameplay.**

Targeted rules tests:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --filter FullyQualifiedName~CMUHumanAnimationTest
```

To check the production SWSL parser and GPU compiler on Windows after building the client:

```powershell
pwsh -File Tools/three_d/parse_native_shader.ps1 -Shader Content.CMU/Resources/Textures/CMU14/Shaders/human_animation.swsl > Tools/human_animation/generated/native-shader.json
python Tools/human_animation/validate_shader.py
```

The last step compiles/links four actual engine-generated GLSL variants using a hidden SDL/OpenGL
context. It does not launch the game. These checks establish code/clip integrity, not live acceptance.

Validation on 2026-11-09: the client build completed with zero errors; all eight targeted tests
passed; all four GLSL variants compiled and linked. The standard test run initially could not
connect to its worker in the restricted environment; rerunning the same tests with local process
access passed. The game was not launched.

## Remaining art and runtime work

- This is a pixel warp, not a hand-drawn articulated walk cycle. Profile leg overlap, arm/shoulder
  joins and unusual silhouettes need art review; dedicated frames may be preferable for some poses.
- Clothing is transformed with the actor, but every uniform/armor/in-hand combination is not certified.
  Long weapons, skirts, shields, hair, amputations and custom-sized sprite layers need live checks.
- Reloading, eating, treatment and construction currently share a generic hand-work loop when their
  existing do-after requires hands. They do not have separate authored action-specific clips.
- Limping currently follows missing legs; it does not disclose or infer fractures from private state.
- Fall/get-up retain the engine's current horizontal/vertical rotation, rather than adding new gameplay poses.
- No live multiplayer, rotated-grid, remote-shot or frame-time acceptance is claimed. Post processing
  adds render-target work per visible actor. Measure busy rounds before enabling this by default.

The original RSI art and its licensing metadata remain unchanged. The preview derives from those
source assets and retains their licensing obligations; it is a review sheet, not replacement art.

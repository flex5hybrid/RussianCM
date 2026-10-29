# CMU generated normal maps (v1)

Generates deterministic **alpha-silhouette normal maps** offline for plain PNGs and regular RSI folders, and **commits them to Git**. The client does not regenerate maps during gameplay. Original assets are never modified. No RobustToolbox changes required.

## Install and test

From the repository root:

~~~sh
python -m pip install -r Tools/normal_maps/requirements.txt
python -m unittest discover -s Tools/normal_maps -p 'test_*.py'
~~~

## Generate and commit

Regenerate one RSI or individual PNG:

~~~sh
python Tools/normal_maps/generate.py --paths Content.CMU/Resources/Textures/CMU14/Items/laptop.rsi
~~~

Or update only changed textures (including deletion and rename cleanup):

~~~sh
python Tools/normal_maps/generate.py --changed-from origin/master
~~~

Review and commit BOTH the modified source and its generated output. CI checks changed source textures using:

~~~sh
python Tools/normal_maps/generate.py --check --changed-from origin/master
~~~

For a **separate, intentional rollout** of the entire preexisting asset collection, use --all. **Do not run it casually**: it may create thousands of files. Full --all --check additionally detects orphaned outputs, while changed-file CI checks only the sources modified by that PR.

## File structure

A normal RSI is mirrored under the same resource root's _GeneratedNormalMaps directory:

~~~text
Content.CMU/Resources/Textures/CMU14/Items/laptop.rsi/
  meta.json
  laptop_off.png

Content.CMU/Resources/Textures/_GeneratedNormalMaps/CMU14/Items/laptop.rsi/
  meta.json
  laptop_off.png
~~~

Identical rules apply under Resources/Textures. Mirrored RSI folders preserve the original metadata, state names, source PNG sheet layout, animation frames, directions, dimensions, licensing, and alpha. The game can later access generated RSI states via its ordinary RSI resource loading. Standalone PNGs are mirrored as standalone PNGs.

Interface, UI, HUD, icon, font, cursor, and logo folders are excluded by default. The first version supports regular .rsi directories and .png, **not .rsic**.

## What is generated

Each RSI frame is processed separately so one frame cannot leak gradients into its neighbours. The generator blurs the **alpha silhouette**, estimates its X/Y gradient, converts it into a tangent-space unit normal and preserves the original alpha channel. A flat interior normally has RGB(128,128,255). For tuning:

- --bevel 1.25 controls how far the silhouette bevel extends.
- --strength 5 controls how steeply the edge normal tilts.

This is intentionally conservative: image brightness, painted shadows, highlights, and colours are not interpreted as object depth. The generator cannot recover real 3D geometry. More advanced authored normals and exclusions can be added later.

## Limitations and next stage

- This PR **does not yet make lighting read the normals**. A normal buffer, height model, and normal-aware lighting shader are future work.
- Existing asset normals are **not bulk-created** in this PR; only subsequently changed/selected files are handled.
- For RSI assets, meta.json uses the real Robust format of a frame size object with x/y coordinates.
- Generated results are source-code artifacts: changes to the algorithm may require intentional regeneration.
- The generated data texture must be sampled as normal-vector data rather than colour (ensure suitable colour-space handling in the future renderer).
- Automatically generated assets are derived works: original licensing and attribution still apply; the original meta.json is kept unmodified.

## Contributor workflow

Add or update textures -> run the generator for the changed path -> inspect its PNGs -> commit both source and generated RSI/PNG -> CI verifies exact output. Missing/stale generated files fail CI. If an RSI is deleted, the generator removes its mirror.

# CMU generated normal maps — v2

Offline, deterministic normal-map **asset pipeline** for SS14/CMU. Developers add a PNG or RSI, run this tool, inspect its generated map and commit it next to the source in Git. The game client does **not** need to generate normals at runtime. No RobustToolbox changes are required for this tool.

> The generated maps are approximations of painted 2D surface geometry. RGB pixels alone cannot reliably reconstruct true 3D shapes. In particular, painted highlights and dark lines are not guaranteed to be actual bumps or recesses.

## Installation

From the repository root (Python 3.12 recommended):

~~~shell
python -m pip install -r Tools/normal_maps/requirements.txt
python -m unittest discover -s Tools/normal_maps -p 'test_*.py'
~~~

## Generate and visually inspect one RSI

~~~shell
python Tools/normal_maps/generate.py \
  --paths Content.CMU/Resources/Textures/CMU14/Items/laptop.rsi \
  --mode hybrid \
  --profile equipment \
  --preview-dir /tmp/cmu-normal-preview
~~~

Each preview includes the **original sprite**, **generated normal map**, and **two directional lighting previews from opposite sides**. This makes it much easier to spot inconsistent or useless normal maps. Only the first frame of up to 24 processed PNG sheets is previewed. The preview is diagnostic: it is **not the CMU light shader**.

Normal maps are created in parallel resource directories:

~~~text
Content.CMU/Resources/Textures/CMU14/Items/laptop.rsi/
    meta.json
    laptop_off.png
Content.CMU/Resources/Textures/_GeneratedNormalMaps/CMU14/Items/laptop.rsi/
    meta.json
    laptop_off.png
~~~

This also works for ordinary PNG files and textures in the upstream Resources/Textures root. RSI frame size, state names, directions, animation sheets and metadata are preserved. A normal sheet is computed **independently for each RSI frame** to prevent interpolation across neighbouring sprite frames.

## Generation modes

| Mode | Explanation |
|---|---|
| flat | Constant (128, 128, 255) for each visible pixel; preserves the original alpha |
| bevel | Silhouette-only normal map from smoothed alpha (previous v1 behaviour) |
| height | Internal relief estimated from multiscale local luminance and weak chromatic seam cues |
| hybrid (default) | Alpha bevel **plus** internal detail and colour-boundary seams, moderated by the material profile |

The internal-relief estimator uses a small blur minus a broad blur to suppress baked *large-scale* lighting gradients, dead-zones low-contrast noise, and adds low-amplitude inset seams at stronger colour transitions. It is intentionally much weaker than full 'brightness=height'. This approach makes opaque sprite interiors less featureless; however, **it can still produce false relief** from painted patterns, and there is no single correct reconstruction.

## Material profiles

--profile auto (default) guesses a safe detail multiplier by directory:
- floor/tiles/terrain: very low detail and no silhouette bevel;
- Mobs/Characters/Clothing: reduced interior detail;
- Objects/Items/Structures/Doors/Vehicles: standard equipment detail;
- Wire, barbed wire, coils, cables, chains and fences: **wire** profile, with strongly reduced internal relief, no colour-seam carving and a strict normal-tilt limit;
- other directories: generic detail.

For a specific item, force --profile equipment, --profile character, --profile floor, --profile generic or --profile wire. In auto mode, wire-related names are recognized **before** broader category directories (e.g. _RMC14/Objects). Profile selection does not alter the source image. Bevel mode on a fully opaque solid rectangle may still be flat if the source provides no alpha silhouette.

## Tunable parameters

~~~shell
python Tools/normal_maps/generate.py \
  --paths Content.CMU/Resources/Textures/CMU14/Items/laptop.rsi \
  --mode hybrid --profile equipment \
  --bevel 1.25 --strength 5 \
  --detail-strength 2.2 --detail-radius 2.4 --edge-strength 0.16 \
  --preview-dir /tmp/cmu-normal-preview
~~~

- --bevel: blur radius for alpha silhouette (0..16)
- --strength: slope strength at alpha silhouette (0..100)
- --detail-strength: amount of inferred inner relief (0..20)
- --detail-radius: spatial scale for removing broad painted shading (0.5..16)
- --edge-strength: amount of small recessed seams at chromatic edges (0..2)

For noisy textures start by lowering detail-strength and edge-strength. **For barbed_wire.rsi use --profile auto (wire selected automatically);** this prevents the hand-painted bright strands from turning into exaggerated cyan/magenta normals. For flat floor tiles force --profile floor. For assets where colour must **never** imply geometry use --mode bevel or --mode flat.

## Incremental Git workflow / CI

After editing your texture, update only the assets modified since the base branch:

~~~shell
python Tools/normal_maps/generate.py --changed-from origin/master
python Tools/normal_maps/generate.py --check --changed-from origin/master
~~~

Commit BOTH source and generated assets. CI checks reproducibility at the decoded pixel level (rather than PNG compression bytes), and fails on missing or outdated normal maps for changed source textures. The workflow is read-only; it does not silently write commits into contributor branches.

To process a particular directory use --paths with the directory; to intentionally generate the entire historical asset collection use --all. **Do not run --all casually**: it can add thousands of files. It also reconciles removed generated files. Use --check to verify without writing.

Generated maps are stored inside _GeneratedNormalMaps under each of the existing texture roots. Interface, UI, HUD, icon, font, cursor and logo folders are excluded. Only regular .rsi and .png are supported in v2, not .rsic. Existing original files are never modified.

## Future rendering integration

This PR generates **assets**, not the lighting system that consumes them. To render them in CMU, the client must:
1. resolve a normal RSI frame matching the source RSI state/direction/animation;
2. ensure correct per-layer sorting, sprite flip/rotation, and linear normal data sampling;
3. provide a normal buffer aligned with world-space pixels and an approximate light/surface height;
4. incorporate normals into a directional/area-light shader and preserve hard FOV.

**Do not enable every automatically generated normal map without reviewing representative lighting results.** Maps are a useful starting point, not authored 3D geometry.

Art licensing and attributions remain those of the source asset; metadata is copied unchanged.

"""Compare native captures numerically; never rewrite their PNGs."""
import argparse
import json
import runpy
from pathlib import Path
import numpy as np
from PIL import Image


def pixels(path):
    return np.asarray(Image.open(path).convert('RGB'), dtype=np.int16)


def delta(a, b):
    d = np.abs(a - b)
    return {'changed_pixels': int(np.any(d, axis=2).sum()),
            'mean_absolute_channel_change_8bit': float(d.mean()),
            'maximum_channel_change_8bit': int(d.max())}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('before', type=Path)
    parser.add_argument('after', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    off = pixels(args.after / 'quality-0.png')
    # Reuse the frozen regression definition: border-connected exterior only.
    # Black texels inside visible sprites are not a hidden-space/FOV mask.
    checks = runpy.run_path(str(Path(__file__).resolve().parents[5]
                              / 'Tools/CMU14/Lighting/check-comparison.py'))
    deep = checks['exterior'](off)
    rows = []
    for quality in range(4):
        a = pixels(args.before / f'quality-{quality}.png')
        b = pixels(args.after / f'quality-{quality}.png')
        assert a.shape == b.shape == (960, 1344, 3)
        rows.append({'quality': quality, 'whole_frame': delta(a, b),
                     'statue_water_xyxy': [758, 500, 972, 722],
                     'statue_water': delta(a[500:722, 758:972], b[500:722, 758:972]),
                     'central_actor_xyxy': [645, 445, 695, 512],
                     'central_actor': delta(a[445:512, 645:695], b[445:512, 645:695]),
                     'deep_hidden_pixels': int(deep.sum()),
                     'nonblack_in_deep_hidden': int(np.any(b[deep] != 0, axis=1).sum())})
    args.output.write_text(json.dumps({'before': str(args.before), 'after': str(args.after),
                                      'presets': rows}, indent=2) + '\n', encoding='utf-8', newline='\n')

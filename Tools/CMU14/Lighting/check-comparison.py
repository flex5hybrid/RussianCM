"""Native PNG regression analysis. Never changes an input image; thresholds live in quality-regions.json."""
import argparse
import json
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


def pixels(path):
    return np.asarray(Image.open(path).convert('RGB'), dtype=np.float64) / 255.0


def linear(rgb):
    return np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)


def luminance(rgb):
    return rgb @ np.array([.2126, .7152, .0722])


def exterior(original):
    h, w = original.shape[:2]
    black = np.all(original == 0, axis=2)
    connected = np.zeros((h, w), dtype=np.uint8)
    q = deque([(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)]
              + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)])
    while q:
        x, y = q.popleft()
        if connected[y, x] or not black[y, x]:
            continue
        connected[y, x] = 255
        if x: q.append((x - 1, y))
        if y: q.append((x, y - 1))
        if x + 1 < w: q.append((x + 1, y))
        if y + 1 < h: q.append((x, y + 1))
    return np.asarray(Image.fromarray(connected).filter(ImageFilter.MinFilter(7))) > 0


def run_length(values):
    best = current = 0
    for v in values:
        current = current + 1 if v else 0
        best = max(best, current)
    return best


def analyze(root, config, source_off=None, reference=True, swatches=False):
    original = pixels(root / 'quality-0.png')
    h, w = original.shape[:2]
    deep = exterior(original)
    old = linear(original)
    old_y = luminance(old)
    visible = np.any(original > 0, axis=2)
    regions = config['regions'] if reference else {'whole_stand': [0, 0, w, h]}
    if swatches:
        regions = {name: [round(x0 * w / 1344), round(135 * h / 960),
                          round(x1 * w / 1344), round(300 * h / 960)]
                   for name, x0, x1 in [('matte', 210, 405), ('polished', 465, 660),
                                        ('tile', 720, 915), ('wet', 975, 1170)]}
    thresholds = config['thresholds']
    results = []
    for quality in (1, 2, 3):
        rgb = pixels(root / f'quality-{quality}.png')
        if rgb.shape != original.shape:
            raise ValueError('Viewport dimensions changed')
        values = linear(rgb)
        y = luminance(values)
        changed = np.any(rgb != original, axis=2)
        white = np.all(rgb > .985, axis=2) & visible
        row = {'quality': quality, 'size': [w, h], 'deep_exterior_pixels': int(deep.sum()),
               'changed_deep_exterior_pixels': int((changed & deep).sum()),
               'white_visible_fraction': float(white.sum() / max(1, visible.sum())), 'regions': {}}
        checks = {'fov': row['changed_deep_exterior_pixels'] == 0,
                  'white_coverage': row['white_visible_fraction'] <= thresholds['maximum_white_fraction']}
        for name, bounds in regions.items():
            x0, y0, x1, y1 = bounds
            if reference:
                x0, x1 = (round(v * w / config['reference_size'][0]) for v in (x0, x1))
                y0, y1 = (round(v * h / config['reference_size'][1]) for v in (y0, y1))
            mask = visible[y0:y1, x0:x1]
            region = values[y0:y1, x0:x1][mask]
            before = old[y0:y1, x0:x1][mask]
            if not len(region): continue
            lum = luminance(region)
            base = luminance(before)
            row['regions'][name] = {
                'mean_luminance': float(lum.mean()), 'peak_luminance': float(lum.max()),
                'p99_luminance': float(np.percentile(lum, 99)),
                'rms_contrast': float(lum.std() / max(lum.mean(), .001)),
                'contrast_ratio_to_off': float((lum.std() / max(lum.mean(), .001))
                    / max(base.std() / max(base.mean(), .001), .001)),
                'mean_rgb': region.mean(axis=0).tolist(), 'delta_rgb': (region - before).mean(axis=0).tolist()}
        if reference:
            r = row['regions']
            red = lambda name: r[name]['delta_rgb'][0] - sum(r[name]['delta_rgb'][1:]) * .5
            cyan = lambda name: sum(r[name]['delta_rgb'][1:]) * .5 - r[name]['delta_rgb'][0]
            warm = lambda name: sum(r[name]['delta_rgb'][:2]) * .5 - r[name]['delta_rgb'][2]
            row['locality'] = {'red_near': red('red_near'), 'red_far': red('red_far'),
                               'cyan_near': cyan('cyan_near'), 'cyan_far': cyan('cyan_far'),
                               'warm_near': warm('warm_near'), 'warm_far': warm('warm_far')}
            if quality >= 2:
                checks['red_locality'] = red('red_near') >= thresholds['minimum_local_color_bias'] and (
                    red('red_near') - red('red_far') >= thresholds['minimum_color_locality_difference'])
                checks['cyan_locality'] = cyan('cyan_near') >= thresholds['minimum_local_color_bias'] and (
                    cyan('cyan_near') - cyan('cyan_far') >= thresholds['minimum_color_locality_difference'])
                checks['warm_locality'] = warm('warm_near') >= thresholds['minimum_local_color_bias'] and (
                    warm('warm_near') - warm('warm_far') >= thresholds['minimum_color_locality_difference'])
            checks['actor_contrast'] = r['actor']['contrast_ratio_to_off'] >= thresholds['minimum_actor_contrast_ratio']
        # Original texture edges are excluded before looking for new long gradients.
        # These are candidates, not hard failures: furniture and physical material transitions
        # can legitimately add straight boundaries and must be inspected with the targets.
        delta = y - old_y
        padded = np.pad(delta, 1, mode='edge')
        smooth_delta = (delta * 4 + padded[:-2, 1:-1] + padded[2:, 1:-1]
                        + padded[1:-1, :-2] + padded[1:-1, 2:]) / 8
        old_dy, old_dx = np.gradient(old_y)
        excluded = np.asarray(Image.fromarray(np.uint8((np.hypot(old_dx, old_dy) > thresholds['original_edge_gradient']) * 255))
                              .filter(ImageFilter.MaxFilter(9))) > 0
        dy, dx = np.gradient(smooth_delta)
        allowed = visible & ~excluded
        if (root / 'view.json').exists():
            view = json.loads((root / 'view.json').read_text(encoding='utf-8-sig'))
            ux, uy = np.meshgrid((np.arange(w) + .5) / w, 1 - (np.arange(h) + .5) / h)
            world_x = view['origin'][0] + view['x'][0] * ux + view['y'][0] * uy
            world_y = view['origin'][1] + view['x'][1] * ux + view['y'][1] * uy
            tile_edges = (np.minimum(world_x % 1, 1 - world_x % 1) < .07) | (np.minimum(world_y % 1, 1 - world_y % 1) < .07)
            allowed &= ~tile_edges
        edge = allowed & (np.maximum(np.abs(dx), np.abs(dy)) > thresholds['straight_gradient'])
        candidates = []
        for orientation, scan in [('horizontal', edge), ('vertical', edge.T)]:
            for line, values_on_line in enumerate(scan):
                starts = np.flatnonzero(np.diff(np.r_[False, values_on_line, False].astype(np.int8)) == 1)
                ends = np.flatnonzero(np.diff(np.r_[False, values_on_line, False].astype(np.int8)) == -1)
                for start, end in zip(starts, ends):
                    if end - start >= thresholds['straight_run_pixels']:
                        candidates.append({'orientation': orientation, 'line': int(line),
                                           'start': int(start), 'end_exclusive': int(end), 'pixels': int(end-start)})
        longest = max(max(map(run_length, edge)), max(map(run_length, edge.T)))
        row['straight_boundary_candidates'] = {'longest_run_pixels': longest,
            'needs_visual_review': longest >= thresholds['straight_run_pixels'],
            'segments': sorted(candidates, key=lambda v: -v['pixels'])[:16]}
        if source_off is not None and quality >= 2:
            # Use native captures of the reflections debug view, whose HDR is tone mapped by
            # composite.swsl. Do not measure wrapped 8-bit copies of a raw float render target.
            off_path = source_off / f'quality-{quality}.png'
            off_values = linear(pixels(off_path))
            difference = np.maximum(0, luminance(values - off_values))
            row['source_off'] = {'mean_positive_change': float(difference[visible].mean()),
                'peak_positive_change': float(difference.max()), 'changed_pixels': int((difference > .001).sum())}
            checks['source_off_response'] = difference.max() >= thresholds['minimum_source_off_change']
        row['checks'] = {k: bool(v) for k, v in checks.items()}
        results.append(row)
    return {'config_version': config['version'], 'thresholds': thresholds,
            'all_checks_pass': all(all(x['checks'].values()) for x in results), 'presets': results}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--config', type=Path, default=Path(__file__).with_name('quality-regions.json'))
    parser.add_argument('--stand', action='store_true')
    parser.add_argument('--swatches', action='store_true', help='Report the four fixed material samples; implies --stand.')
    parser.add_argument('--source-off', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    result = analyze(args.directory, json.loads(args.config.read_text(encoding='utf-8-sig')),
                     args.source_off, not (args.stand or args.swatches), args.swatches)
    output = json.dumps(result, indent=2, allow_nan=False) + '\n'
    if args.output: args.output.write_text(output, encoding='utf-8', newline='\n')
    else: print(output, end='')
    raise SystemExit(0 if result['all_checks_pass'] else 1)

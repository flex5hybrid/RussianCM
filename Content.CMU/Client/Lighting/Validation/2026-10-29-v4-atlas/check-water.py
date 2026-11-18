"""Measure real water/control/actor pixels across captures; do not edit images."""
import argparse
import csv
import json
import runpy
from pathlib import Path
import numpy as np


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('on', type=Path)
    parser.add_argument('reflections_disabled', type=Path)
    parser.add_argument('lamp_off', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[5]
    api = runpy.run_path(str(repo / 'Tools/CMU14/Lighting/check-comparison.py'))
    config = json.loads((repo / 'Tools/CMU14/Lighting/quality-regions.json').read_text())
    threshold = config['thresholds']['minimum_source_off_change']
    with (args.on / 'sources.csv').open() as f:
        sources = {row['entity']: row for row in csv.DictReader(f)}
    with (args.lamp_off / 'sources.csv').open() as f:
        surviving = {row['entity'] for row in csv.DictReader(f)}
    missing = [source for uid, source in sources.items() if uid not in surviving]
    assert len(missing) == 1, 'The native source manifest must identify exactly one disabled lamp.'
    selected = 'left_water' if float(missing[0]['x']) < 0 else 'right_water'
    control = 'right_water' if selected == 'left_water' else 'left_water'
    boxes = {'left_water': [512, 128, 576, 320], 'right_water': [768, 128, 832, 320],
             'opaque_head_inner': [538, 224, 550, 234], 'dry_actor': [655, 450, 688, 509]}
    rows = []
    for quality in (0, 1, 2, 3):
        images = [api['pixels'](p / f'quality-{quality}.png')
                  for p in (args.on, args.reflections_disabled, args.lamp_off)]
        assert all(image.shape == (960, 1344, 3) for image in images)
        row = {'quality': quality, 'regions': {}}
        for name, (x0, y0, x1, y1) in boxes.items():
            on, disabled, lamp_off = [image[y0:y1, x0:x1] for image in images]
            isolated = np.abs(api['linear'](on) - api['linear'](disabled))
            source = np.maximum(0, api['luminance'](api['linear'](on) - api['linear'](lamp_off)))
            row['regions'][name] = {
                'xyxy': boxes[name], 'reflection_changed_pixels': int(np.any(on != disabled, axis=2).sum()),
                'reflection_mean_absolute_linear_channel_change': float(isolated.mean()),
                'reflection_maximum_absolute_linear_channel_change': float(isolated.max()),
                'lamp_off_mean_positive_linear_luminance_change': float(source.mean()),
                'lamp_off_maximum_positive_linear_luminance_change': float(source.max())}
        r = row['regions']
        row['checks'] = {'opaque_head_unchanged_by_reflections': r['opaque_head_inner']['reflection_changed_pixels'] == 0,
                         'dry_actor_unchanged_by_reflections': r['dry_actor']['reflection_changed_pixels'] == 0}
        if quality >= 2:
            row['checks'].update({
                'left_water_reflects': r['left_water']['reflection_maximum_absolute_linear_channel_change'] >= threshold,
                'right_water_reflects': r['right_water']['reflection_maximum_absolute_linear_channel_change'] >= threshold,
                'selected_water_source_off': r[selected]['lamp_off_maximum_positive_linear_luminance_change'] >= threshold,
                'source_off_local_to_selected_water': r[selected]['lamp_off_mean_positive_linear_luminance_change']
                    > r[control]['lamp_off_mean_positive_linear_luminance_change']})
        rows.append(row)
    result = {'threshold_from_frozen_quality_regions': threshold,
              'disabled_source_from_native_csv': missing[0], 'selected_water_region': selected,
              'all_checks_pass': all(all(row['checks'].values()) for row in rows), 'presets': rows,
              'scope': 'Final water response and opaque actor cores. The lamp-off comparison also includes direct/native changes; it is not isolated specular. Native submerged-body alpha/depth remain authoritative.'}
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8', newline='\n')
    raise SystemExit(0 if result['all_checks_pass'] else 1)

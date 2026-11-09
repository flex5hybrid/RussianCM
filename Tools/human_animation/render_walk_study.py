"""Articulated, editable eight-pose walk study. No live game assets or code are installed.

Unlike the rejected global pixel warp, every leg has its own knee and foot, the arms have
shoulder/elbow/wrist pivots, and front/back draw order is explicit. Occluded profile limbs
are reconstructed from their visible counterpart; this remains an art study, not final art.
"""
from pathlib import Path
import json
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from render_review import ROOT, OUT, frame, person, deform

SPEC = json.loads((Path(__file__).parent / "studies/walk-v2.json").read_text())
Y, X = np.mgrid[:40, :40]
POINTS = np.stack((X - 4 + 0.5, Y - 4 + 0.5), axis=-1)


def tint(image, color):
    data = np.array(image)
    data[:, :, :3] = data[:, :, :3] * np.array(color)
    return Image.fromarray(data)


def clipped(image, mask):
    data = np.array(image).copy()
    data[:, :, 3] = np.where(mask, data[:, :, 3], 0)
    return Image.fromarray(data)


def source_layers(direction, female, armor):
    yy, xx = np.mgrid[:32, :32]
    def skin(state):
        image = np.array(frame("Mobs/Species/Human/parts.rsi", state, direction))
        # Detached organ artwork includes red cut surfaces. In the body these joints are hidden;
        # a reconstructed sleeve/hip must not expose the detached-organ cut palette.
        rgb = image[:, :, :3].astype(float)
        cut = (rgb[:, :, 0] > rgb[:, :, 1] * 1.6) & (rgb[:, :, 0] > rgb[:, :, 2] * 1.6)
        for channel in (1, 2):
            image[:, :, channel][cut] = image[:, :, 0][cut]
        return tint(Image.fromarray(image), (.84, .69, .57))
    clothes = frame("_RMC14/Objects/Clothing/Uniforms/Marine/standard.rsi", "equipped-INNERCLOTHING", direction)
    if female:
        d = np.array(frame("Mobs/Species/Human/displacement.rsi", "jumpsuit-female", direction))
        sx = np.clip(xx + d[:, :, 0].astype(int) - 128, 0, 31)
        sy = np.clip(yy - d[:, :, 1].astype(int) + 128, 0, 31)
        data = np.array(clothes)[sy, sx].copy()
        data[:, :, 3] = data[:, :, 3].astype(float) * d[:, :, 3] / 255
        clothes = Image.fromarray(data)
    boots = frame("_RMC14/Objects/Clothing/Shoes/Boots/black.rsi", "equipped-FEET", direction)
    armor_image = (frame("_RMC14/Objects/Clothing/OuterClothing/Armor/m3/standard.rsi", "equipped-OUTERCLOTHING", direction)
                   if armor else Image.new("RGBA", (32, 32)))
    profile = direction in (2, 3)
    # Torso/head remain rigid. No breathing stretch, resampling of the face, or whole-body shear.
    root = Image.new("RGBA", (32, 32))
    root.alpha_composite(skin("torso_f" if female else "torso_m"))
    root.alpha_composite(skin("head_f" if female else "head_m"))
    central = ((xx >= (12 if direction == 2 else 11)) & (xx <= (20 if direction == 2 else 19)) if profile
               else (xx >= 11) & (xx <= 19))
    root.alpha_composite(clipped(clothes, (central | (yy < 14)) & (yy < 24)))
    root.alpha_composite(clipped(armor_image, (central | (yy < 14)) & (yy < 24)))
    if armor:
        root.alpha_composite(frame("_RMC14/Objects/Clothing/Head/Helmets/m10/standard.rsi", "equipped-HELMET", direction))
    else:
        root.alpha_composite(tint(frame("Mobs/Customization/human_hair.rsi", "ponytail", direction), (.32, .20, .13)))

    legs, arms = [], []
    for side in range(2):
        name = "l" if side == 0 else "r"
        leg_skin = clipped(skin(name + "_leg"), yy >= 21)
        bbox = leg_skin.getbbox()
        center_x = (bbox[0] + bbox[2]) / 2
        leg_mask = (yy >= 21) & ((xx >= 16) if bbox[0] >= 16 else (xx < 16))
        if profile:
            leg_mask = yy >= 22
        leg = leg_skin.copy()
        leg.alpha_composite(clipped(clothes, leg_mask))
        foot = skin(name + "_foot")
        foot_mask = np.ones((32, 32), dtype=bool) if profile else ((xx >= 16) if bbox[0] >= 16 else (xx < 16))
        foot.alpha_composite(clipped(boots, foot_mask))
        legs.append((leg, foot, center_x))

        # A side-facing source contains only the near sleeve. Reconstruct the far sleeve explicitly.
        arm_name = ("r" if direction == 2 else "l") if profile else name
        arm = clipped(skin(arm_name + "_arm"), yy >= 12)
        arm.alpha_composite(skin(arm_name + "_hand"))
        arm_box = skin(arm_name + "_arm").getbbox()
        arm_x = (arm_box[0] + arm_box[2]) / 2
        arm_mask = (xx >= arm_box[0]) & (xx < arm_box[2]) & (yy >= 10) & (yy < 23)
        arm.alpha_composite(clipped(clothes, arm_mask))
        arm.alpha_composite(clipped(armor_image, arm_mask & (yy < 18)))
        arms.append((arm, arm_x))
    return root, legs, arms


def bone(source, start, end, target_start, target_end, t_min=0, t_max=1):
    """Inverse-map ONE rigid bone segment. Separate segments may bend around their joint."""
    start, end, a, b = (np.array(p, dtype=float) for p in (start, end, target_start, target_end))
    vector = b - a
    length = float(np.linalg.norm(vector))
    along = vector / max(length, .001)
    normal = np.array([along[1], -along[0]])
    local = POINTS - a
    distance = np.sum(local * along, axis=-1)
    cross = np.sum(local * normal, axis=-1)
    source_vector = end - start
    source_length = float(np.linalg.norm(source_vector))
    source_normal = np.array([source_vector[1], -source_vector[0]]) / source_length
    fraction = distance / max(length, .001)
    uv = start + fraction[:, :, None] * source_vector + cross[:, :, None] * source_normal
    sx, sy = np.floor(uv[:, :, 0]).astype(int), np.floor(uv[:, :, 1]).astype(int)
    valid = (fraction >= t_min) & (fraction < t_max) & (sx >= 0) & (sx < 32) & (sy >= 0) & (sy < 32)
    result = np.array(source)[np.clip(sy, 0, 31), np.clip(sx, 0, 31)].copy()
    result[~valid] = 0
    return Image.fromarray(result)


def joint(hip, ankle, bend):
    hip, ankle = np.array(hip, float), np.array(ankle, float)
    delta = ankle - hip
    distance = float(np.linalg.norm(delta))
    # Equal-length thigh/calf. IK keeps each planted ankle fixed throughout the stance.
    half = distance * .5
    radius = max(4.35, half + .001)
    offset = min(2, math.sqrt(max(0, radius * radius - half * half)))
    return (hip + ankle) * .5 + np.array([delta[1], -delta[0]]) / distance * offset * bend


def paste_at(canvas, image, dx=0, dy=0):
    canvas.alpha_composite(image, (4 + round(dx), 4 + round(dy)))


def actor(direction, index, female=False, armor=True):
    root, legs, arms = source_layers(direction, female, armor)
    profile = direction in (2, 3)
    facing = 1 if direction == 2 else -1
    pose = SPEC["poses"][index]
    bob = pose["body_down"]
    legs_drawn, arms_drawn = [], []
    for side in range(2):
        leg, foot, lx = legs[side]
        arm, ax = arms[side]
        step = SPEC["poses"][(index + side * 4) % 8]
        dx = step["foot_x"] * facing if profile else 0
        # Front/back use depth and foreshortening; do not swing the legs sideways.
        depth = 0 if profile else round(step["foot_x"] * (1 if direction == 0 else -1) * .5)
        lift = step["foot_lift"]
        foot_dy = -lift + min(0, depth)
        hip = (lx, 21 + bob)
        ankle = (lx + dx, 29 + foot_dy)
        knee = joint(hip, ankle, facing if profile else (0.18 if side == 0 else -.18))
        layer = Image.new("RGBA", (40, 40))
        layer.alpha_composite(bone(leg, (lx, 21), (lx, 25), hip, knee, -.4, 1.15))
        layer.alpha_composite(bone(leg, (lx, 25), (lx, 29), knee, ankle, -.15, 1.12))
        paste_at(layer, foot, dx, foot_dy)
        legs_drawn.append(layer)

        far = profile and side == 1
        target_x = ax + (2 * facing if far else 0)
        swing = -step["foot_x"] * .8
        shoulder = (target_x, 11.5 + bob)
        wrist = (target_x + swing * facing if profile else target_x,
                 20.5 + bob - (0 if profile else swing * .45))
        elbow = (np.array(shoulder) + wrist) / 2 + np.array([0.4 * facing if profile else 0, -.2])
        hand_layer = Image.new("RGBA", (40, 40))
        hand_layer.alpha_composite(bone(arm, (ax, 11.5), (ax, 16), shoulder, elbow, -.4, 1.15))
        hand_layer.alpha_composite(bone(arm, (ax, 16), (ax, 20.5), elbow, wrist, -.15, 1.4))
        arms_drawn.append(hand_layer)

    canvas = Image.new("RGBA", (40, 40))
    if profile:
        canvas.alpha_composite(tint(arms_drawn[1], (.82, .82, .82)))
        canvas.alpha_composite(tint(legs_drawn[1], (.82, .82, .82)))
        canvas.alpha_composite(legs_drawn[0])
        paste_at(canvas, root, 0, bob)
        canvas.alpha_composite(arms_drawn[0])
    else:
        canvas.alpha_composite(arms_drawn[0])
        canvas.alpha_composite(arms_drawn[1])
        for side in sorted(range(2), key=lambda s: SPEC["poses"][(index + s * 4) % 8]["foot_x"]):
            canvas.alpha_composite(legs_drawn[side])
        paste_at(canvas, root, 0, bob)
    return canvas


def main():
    OUT.mkdir(exist_ok=True)
    fonts = "C:/Windows/Fonts/segoeui.ttf"
    title = ImageFont.truetype(fonts, 25)
    label = ImageFont.truetype(fonts, 17)
    small = ImageFont.truetype(fonts, 13)
    old = json.loads((OUT / "poses.json").read_text())
    old_walk = next(clip for clip in old if clip["name"] == "Walk")
    frames = []
    for index in range(8):
        page = Image.new("RGB", (900, 670), "#111820")
        draw = ImageDraw.Draw(page)
        draw.text((24, 16), "Ходьба · сравнение подходов", font=title, fill="#edf3f7")
        draw.text((24, 52), "V2: отдельные конечности, колени и стопы · 8 явных поз · офлайн-образец", font=small, fill="#a3b6c4")
        for d, direction in enumerate(("Юг", "Север", "Восток", "Запад")):
            x = 170 + d * 180
            draw.text((x + 50, 83), direction, font=label, fill="#c3d5e0")
            # Compare one complete walk cycle at the same playback cadence, without speeding up V1.
            cases = [deform(person(d, armored=True), old_walk["frames"][round(index * 12 / (8 * 1.15))], d),
                     actor(d, index), actor(d, index, female=True, armor=False)]
            for row, image in enumerate(cases):
                y = 108 + row * 180
                draw.rounded_rectangle((x, y, x + 164, y + 164), 8, fill="#202d39")
                page.paste(image.resize((160, 160), Image.Resampling.NEAREST), (x + 2, y + 2), image.resize((160,160), Image.Resampling.NEAREST))
        for row, lines in enumerate((("V1", "Отклонённый", "общий изгиб"), ("V2", "Отдельные", "части тела"), ("V2", "В форме", "без брони"))):
            for line, text in enumerate(lines):
                draw.text((24, 155 + row * 180 + line * 24), text, font=label if line == 0 else small,
                          fill="#f1c898" if line == 0 else "#9bb1c3")
        draw.text((24, 650), "Это образец движения, не запись из игры. В игровой код новый вариант пока не подключён.", font=small, fill="#8ea5b6")
        frames.append(page)
    frames[2].save(OUT / "walk-v2.png")
    frames[0].save(OUT / "walk-v2.gif", save_all=True, append_images=frames[1:], duration=SPEC["frame_ms"], loop=0)
    # Editable contact sheet at an integer pixel scale, with all authored poses visible.
    sheet = Image.new("RGB", (1280, 340), "#111820")
    draw = ImageDraw.Draw(sheet)
    for d in range(2):
        for i in range(8):
            image = actor(2 if d == 0 else 0, i).resize((160, 160), Image.Resampling.NEAREST)
            sheet.paste(image, (i * 160, d * 170), image)
            draw.text((i * 160 + 8, d * 170 + 148), f"{i+1}: {SPEC['poses'][i]['pose']}", font=small, fill="#adbfcd")
    sheet.save(OUT / "walk-v2-poses.png")
    # Source-resolution sheets are review assets only. Their characters are baked composites,
    # not replacements for a customizable player's body or inventory layers.
    for name, female, armor in (("marine", False, True), ("uniform", True, False)):
        atlas = Image.new("RGBA", (8 * 40, 4 * 40))
        for direction in range(4):
            for index in range(8):
                atlas.alpha_composite(actor(direction, index, female, armor), (index * 40, direction * 40))
        atlas.save(OUT / f"walk-v2-{name}.png")
    print(f"Rendered articulated 8-pose walk study to {OUT / 'walk-v2.gif'}")


if __name__ == "__main__":
    main()

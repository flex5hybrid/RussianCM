"""Build per-layer V2 pose lookups, retaining each live character's original artwork."""
import json
import numpy as np
from PIL import Image
from render_walk_study import ROOT, SPEC, frame, clipped, bone, joint, paste_at

DEST = ROOT / "Content.CMU/Resources/Textures/CMU14/Effects/human_walk_v2.rsi"
RUN = json.loads((ROOT / "Tools/human_animation/studies/run-v2.json").read_text())
PARTS = "Mobs/Species/Human/parts.rsi"
yy, xx = np.mgrid[:32, :32]
encoded = np.zeros((32, 32, 4), dtype=np.uint8)
encoded[:, :, 0] = xx + 1
encoded[:, :, 1] = yy + 1
encoded[:, :, 3] = 255
SOURCE = Image.fromarray(encoded)
ROLES = ["root", "leg-left", "leg-right", "foot-left", "foot-right",
         "arm-left", "arm-right",
         "clothes-back-arm", "clothes-back-leg", "clothes-front-leg", "clothes-body", "clothes-front-arm", "clothes-fill",
         "glove-left", "glove-right", "shoe-left", "shoe-right"]

def gait(stride, lift, bob, **options):
    return {**options, "poses": [
        {**step, "foot_x": step["foot_x"] * stride,
         "foot_lift": round(step["foot_lift"] * lift), "body_down": round(step["body_down"] * bob)}
        for step in SPEC["poses"]
    ]}


def gesture(down, reach=(0,) * 8, advance=(0,) * 8):
    return {"stationary": True, "poses": [
        {"body_down": y, "foot_x": 0, "foot_lift": 0, "reach": hand, "advance": x}
        for y, hand, x in zip(down, reach, advance)
    ]}


QUIET = gait(.65, .55, 0)
TACTICAL = gait(.8, .7, .55)
HEAVY = gait(.8, .6, 1, body_forward=1, arm_swing=.55)
LIMP_LEFT = gait(.85, .75, 1, sore_leg=0)
LIMP_RIGHT = gait(.85, .75, 1, sore_leg=1)
CRAWL = gait(.75, .6, 0, arm_swing=1.55, crawl=True)
IDLE = gesture((0, 0, 0, -1, -1, -1, 0, 0))
REACH = gesture((0, 0, 1, 1, 1, 0, 0, 0), (0, 1, 3, 4, 4, 3, 1, 0), (0, 0, 0, 1, 1, 0, 0, 0))
MELEE = gesture((0, 1, 1, 0, 0, 1, 0, 0), (0, -2, -1, 5, 5, 3, 1, 0), (0, -1, 0, 1, 2, 1, 0, 0))
SHOOT = gesture((0, 1, 1, 0, 0, 0, 0, 0), advance=(-2, -2, -1, -1, 0, 0, 0, 0))
FALL = gesture((0, 1, 2, 3, 2, 1, 0, 0))
RISE = gesture((3, 3, 2, 2, 1, 1, 0, 0))

CLIPS = (
    ("walk", False, SPEC), ("held", True, SPEC), ("run", False, RUN), ("run-held", True, RUN),
    ("quiet", False, QUIET), ("quiet-held", True, QUIET), ("tactical", True, TACTICAL),
    ("heavy", False, HEAVY), ("heavy-held", True, HEAVY),
    ("limp-left", False, LIMP_LEFT), ("limp-left-held", True, LIMP_LEFT),
    ("limp-right", False, LIMP_RIGHT), ("limp-right-held", True, LIMP_RIGHT),
    ("crawl", False, CRAWL), ("crawl-held", True, CRAWL),
    ("idle", True, IDLE), ("reach", False, REACH), ("reach-held", True, REACH),
    ("melee", False, MELEE), ("melee-held", True, MELEE), ("shoot", True, SHOOT),
    ("fall", True, FALL), ("rise", True, RISE),
)


def root_offset(direction, index, spec):
    step = spec["poses"][index]
    bob = step["body_down"]
    if direction >= 2:
        facing = 1 if direction == 2 else -1
        return (spec.get("body_forward", 0) + step.get("advance", 0)) * facing, bob
    if spec.get("stationary") or spec.get("crawl"):
        return 0, bob
    # Shift the hips over the supporting foot during the loading pose.
    sway = 0
    if index % 4 == 1:
        side = "l" if index < 4 else "r"
        leg = frame(PARTS, side + "_leg", direction).getbbox()
        sway = -1 if leg[0] < 16 else 1
    return sway, bob - 1


def pose(direction, index, held, spec):
    forward, bob = root_offset(direction, index, spec)
    profile = direction >= 2
    facing = 1 if direction == 2 else -1
    stationary = spec.get("stationary", False)
    central = ((xx >= (12 if direction == 2 else 11)) & (xx <= (20 if direction == 2 else 19))
               if profile else (xx >= 11) & (xx <= 19))
    front_side = 1 if direction == 2 or direction == 0 and stationary else 0
    back_side = 1 - front_side
    near_glove_side = xx < 18 if direction == 2 else xx >= 14
    blank = Image.new("RGBA", (40, 40))
    torso_fill = blank.copy()
    root = Image.new("RGBA", (40, 40))
    paste_at(root, SOURCE, forward, bob)
    leg_maps, foot_maps, arm_maps = [], [], []
    cloth_legs, cloth_arms, shoe_maps, glove_maps, ankle_rows = [], [], [], [], []
    owned = np.zeros((32, 32), dtype=bool)
    sleeve = np.zeros((32, 32), dtype=bool)
    for side, name in enumerate(("l", "r")):
        step = spec["poses"][(index + side * 4) % 8]
        bounds = frame(PARTS, name + "_leg", direction).getbbox()
        lx = (bounds[0] + bounds[2]) / 2
        stride = step["foot_x"]
        lift = step["foot_lift"]
        if spec.get("sore_leg") == side:
            stride *= .55
            lift = min(lift, 1)
        dx, dy = round(stride * facing), -lift
        sole = SOURCE
        if not profile:
            depth = round(stride * (1 if direction == 0 else -1) * .6)
            dx = 0
            # Reserve pixels below the neutral sole for the forward step. The far foot recedes
            # independently of knee lift, which contributes less when looking along the stride.
            dy = 0 if stationary else -2 + depth - round(lift * .35)
            if not stationary:
                # Front and back source feet have a toe that sticks out by a pixel. Compress
                # the outer half of the forefoot, keeping its outline and the ankle in place.
                outward = -1 if lx < 16 else 1
                outer_toe = (yy >= 30) & ((xx + .5 - lx) * outward > 0)
                source_x = xx + np.where(outer_toe, outward, 0)
                straight = encoded.copy()
                straight[:, :, 0] = np.clip(source_x + 1, 1, 32)
                straight[:, :, 3] = np.where((source_x >= 0) & (source_x < 32), 255, 0)
                sole = Image.fromarray(straight)
        ankle_rows.append(dy)
        hip, ankle = (lx + forward, 21 + bob), (lx + dx, 29 + dy)
        bend = facing if profile and not stationary else 0
        knee = joint(hip, ankle, bend)

        def leg_image(source):
            if stationary and forward == bob == 0:
                image = blank.copy()
                paste_at(image, source)
                return image
            image = bone(source, (lx, 21), (lx, 25), hip, knee, -.4, 1.15)
            image.alpha_composite(bone(source, (lx, 25), (lx, 29), knee, ankle, -.15, 1.0))
            # The last ankle row belongs to the rigid foot; boots cover it at the same offset.
            paste_at(image, clipped(source, yy == 29), dx, dy)
            return image

        leg_maps.append(leg_image(clipped(SOURCE, yy >= 21)))
        foot_map = Image.new("RGBA", (40, 40))
        paste_at(foot_map, sole, dx, dy)
        foot_maps.append(foot_map)
        leg_mask = (yy >= 21) & ((xx >= 16) if bounds[0] >= 16 else (xx < 16)) & (xx >= 9) & (xx < 22)
        if profile:
            leg_mask = (yy >= 21) & (xx >= 11) & (xx < 22)
        owned |= leg_mask
        cloth_legs.append(leg_image(clipped(SOURCE, leg_mask)))
        shoe_mask = (np.ones((32, 32), bool) if profile else ((xx >= 16) if bounds[0] >= 16 else (xx < 16)))
        shoe = leg_image(clipped(SOURCE, shoe_mask & (yy >= 24) & (yy < 29)))
        paste_at(shoe, clipped(sole, shoe_mask & (yy >= 29)), dx, dy)
        shoe_maps.append(shoe)

        if held:
            arm_maps.append(root)
            cloth_arms.append(blank)
            glove_maps.append(root if side == front_side else blank)
            continue

        # In profile the far arm only contributes a sliver of hand in the source art.
        # Keep that hand with the body instead of borrowing the visible sleeve or glove.
        if profile and side == back_side:
            arm_maps.append(root)
            cloth_arms.append(blank)
            far_glove = Image.new("RGBA", (40, 40))
            paste_at(far_glove, clipped(SOURCE, ~near_glove_side), forward, bob)
            glove_maps.append(far_glove)
            continue

        arm = frame(PARTS, name + "_arm", direction)
        arm_bounds = arm.getbbox()
        ax = (arm_bounds[0] + arm_bounds[2]) / 2
        target_x = ax + forward
        swing = -step["foot_x"] * .8 * spec.get("arm_swing", 1)
        reach = step.get("reach", 0) if profile or side == 1 else 0
        shoulder = (target_x, 11.5 + bob)
        if spec.get("bent_arms", False):
            if profile:
                elbow = (target_x + (swing * .55 - 1.25) * facing, 16 + bob)
                wrist = (elbow[0] + 3.5 * facing, 15.5 + bob - swing * .3)
            else:
                outward = -1 if ax < 16 else 1
                elbow = (target_x + outward, 16 + bob)
                wrist = (target_x - outward, 17.5 + bob - swing * .35)
        elif stationary or spec.get("crawl"):
            reach_x = (reach + swing) * facing if profile else reach * (.28 if ax < 16 else -.28)
            wrist = (target_x + reach_x,
                     20.5 + bob - (abs(reach) * .9 if stationary else swing * .5))
            elbow = (np.array(shoulder) + wrist) / 2 + np.array([-.6 * facing if profile else 0, 0])
        else:
            wrist = (target_x + swing * facing if profile else target_x,
                     20.5 + bob - (0 if profile else swing * .45))
            elbow = (np.array(shoulder) + wrist) / 2 + np.array([.4 * facing if profile else 0, -.2])

        def arm_image(source):
            image = bone(source, (ax, 11.5), (ax, 16), shoulder, elbow, -.4, 1.15)
            image.alpha_composite(bone(source, (ax, 16), (ax, 20.5), elbow, wrist, -.15, 1.4))
            return image

        arm_mask = (xx >= arm_bounds[0]) & (xx < arm_bounds[2]) & (yy >= 12) & (yy < 23)
        owned |= arm_mask
        sleeve |= arm_mask & (yy >= 17)
        cloth_arms.append(arm_image(clipped(SOURCE, arm_mask)))
        arm_maps.append(arm_image(clipped(SOURCE, yy >= 12)))
        glove_side = near_glove_side if profile else (xx < 16 if ax < 16 else xx >= 16)
        glove_maps.append(arm_image(clipped(SOURCE, glove_side & (yy >= 12))))
        if profile:
            # Stay within the torso under the resting sleeve and sample from inside the chest.
            chest = np.array(frame(PARTS, "torso_m", direction))[:, :, 3] > 0
            under_sleeve = (xx >= arm_bounds[0] - (facing < 0)) & (xx < arm_bounds[2] + (facing > 0))
            donor = encoded.copy()
            donor[:, :, 0] = 18 if facing > 0 else 15
            paste_at(torso_fill, clipped(Image.fromarray(donor), chest & under_sleeve & (yy >= 12) & (yy <= 20)),
                     forward, bob)
            arm.alpha_composite(frame(PARTS, name + "_hand", direction))
            # Leave the actual moving arm and hand visible on short-sleeved outfits.
            arm_coordinates = np.array(arm_maps[-1])
            arm_x = np.maximum(arm_coordinates[:, :, 0], 1) - 1
            arm_y = np.maximum(arm_coordinates[:, :, 1], 1) - 1
            moving_arm = (arm_coordinates[:, :, 3] != 0) & (np.array(arm)[arm_y, arm_x, 3] != 0)
            torso_fill = clipped(torso_fill, ~moving_arm)

    above_legs = yy < (23 if profile else 24)
    root_mask = ~owned | (central & above_legs) | (yy < 14)
    if profile:
        root_mask &= ~sleeve
    near_leg = front_side if profile else int(ankle_rows[1] > ankle_rows[0])
    far_leg = 1 - near_leg
    shade = 112 if profile else min(80, (ankle_rows[near_leg] - ankle_rows[far_leg]) * 19)
    if shade and not stationary:
        # Distinguish the two silhouette depths even where the source art shares a leg profile.
        for maps in (leg_maps, foot_maps, cloth_legs, shoe_maps):
            rear = np.array(maps[far_leg])
            rear[:, :, 2] = shade
            maps[far_leg] = Image.fromarray(rear)
    if held:
        root_mask |= above_legs
    torso = Image.new("RGBA", (40, 40))
    paste_at(torso, clipped(SOURCE, root_mask), forward, bob)
    # Keep candidates separate: combine the SAMPLED clothing colors in the shader.
    # A lookup's alpha says that a coordinate exists, not that the garment is opaque there.
    # Equipment composites right then left. Feed it far then near in both profiles.
    return {
        "root": root,
        "leg-left": leg_maps[0], "leg-right": leg_maps[1],
        "foot-left": foot_maps[0], "foot-right": foot_maps[1],
        "arm-left": arm_maps[0], "arm-right": arm_maps[1],
        "clothes-back-arm": cloth_arms[back_side],
        "clothes-back-leg": cloth_legs[far_leg],
        "clothes-front-leg": cloth_legs[near_leg],
        "clothes-body": torso,
        "clothes-front-arm": cloth_arms[front_side],
        "clothes-fill": torso_fill,
        "glove-left": glove_maps[front_side], "glove-right": glove_maps[back_side],
        "shoe-left": shoe_maps[near_leg], "shoe-right": shoe_maps[far_leg],
    }


def encode_lookup(result):
    data = np.array(result)[4:36, 4:36]
    lookup = np.zeros((32, 32, 4), dtype=np.uint8)
    lookup[:, :, 0] = np.clip(128 + data[:, :, 0].astype(int) - 1 - xx, 0, 255)
    # Positive green samples down the source image: the shader negates it in bottom-up texture UVs.
    lookup[:, :, 1] = np.clip(128 + data[:, :, 1].astype(int) - 1 - yy, 0, 255)
    lookup[:, :, 2] = data[:, :, 2]  # Attenuation of the sampled artwork on the far side.
    lookup[:, :, 3] = data[:, :, 3]
    return Image.fromarray(lookup)


def pack_state(atlas):
    """Use one RSI frame for consecutive identical poses in a direction."""
    frames = []
    delays = []
    for direction in range(4):
        previous = None
        durations = []
        for index in range(8):
            x, y = index * 32, direction * 32
            image = atlas.crop((x, y, x + 32, y + 32))
            pixels = image.tobytes()
            if pixels == previous:
                durations[-1] += 1
            else:
                frames.append(image)
                durations.append(1)
                previous = pixels
        delays.append([ticks / 10 for ticks in durations])

    columns = min(8, len(frames))
    packed = Image.new("RGBA", (columns * 32, ((len(frames) + columns - 1) // columns) * 32))
    for index, image in enumerate(frames):
        packed.paste(image, (index % columns * 32, index // columns * 32))
    return packed, delays


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    states = []
    for prefix, held, spec in CLIPS:
        atlases = {role: Image.new("RGBA", (256, 128)) for role in ROLES}
        for direction in range(4):
            for index in range(8):
                for role, result in pose(direction, index, held, spec).items():
                    atlases[role].alpha_composite(encode_lookup(result), (index * 32, direction * 32))
        for role, atlas in atlases.items():
            name = prefix + "-" + role
            packed, delays = pack_state(atlas)
            packed.save(DEST / (name + ".png"))
            states.append({"name": name, "directions": 4, "delays": delays})
    (DEST / "meta.json").write_text(json.dumps({"version": 1, "license": "MIT",
        "copyright": "CMU V2 procedural joint-coordinate lookup data; contains no source character pixels.",
        # Lookup bytes are coordinates, not sRGB colors. 128 must reach the shader unchanged.
        "load": {"srgb": False}, "size": {"x": 32, "y": 32}, "states": states}, indent=2) + "\n",
        encoding="utf-8", newline="\n")
    stored = sum(len(delays) for state in states for delays in state["delays"])
    print(f"Built {len(states)} states, {stored} stored frames for {len(states) * 32} joint poses.")


if __name__ == "__main__":
    main()

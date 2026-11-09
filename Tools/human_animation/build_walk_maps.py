"""Build per-layer V2 pose lookups, retaining each live character's original artwork."""
import json
import numpy as np
from PIL import Image
from render_walk_study import ROOT, SPEC, frame, clipped, bone, joint, paste_at

DEST = ROOT / "Content.CMU/Resources/Textures/CMU14/Effects/human_walk_v2.rsi"
RUN = json.loads((ROOT / "Tools/human_animation/studies/run-v2.json").read_text())
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


def pose(direction, index, held, spec):
    bob = spec["poses"][index]["body_down"]
    profile = direction >= 2
    facing = 1 if direction == 2 else -1
    forward = spec.get("body_forward", 0) * facing if profile else 0
    central = ((xx >= (12 if direction == 2 else 11)) & (xx <= (20 if direction == 2 else 19))
               if profile else (xx >= 11) & (xx <= 19))
    front_side = 1 if direction == 2 else 0
    back_side = 1 - front_side
    near_glove_side = xx < 18 if direction == 2 else xx >= 14
    blank = Image.new("RGBA", (40, 40))
    torso_fill = blank.copy()
    root = Image.new("RGBA", (40, 40))
    paste_at(root, SOURCE, forward, bob)
    leg_maps, foot_maps, arm_maps = [], [], []
    cloth_legs, cloth_arms, shoe_maps, glove_maps = [], [], [], []
    owned = np.zeros((32, 32), dtype=bool)
    sleeve = np.zeros((32, 32), dtype=bool)
    for side, name in enumerate(("l", "r")):
        step = spec["poses"][(index + side * 4) % 8]
        bounds = frame("Mobs/Species/Human/parts.rsi", name + "_leg", direction).getbbox()
        lx = (bounds[0] + bounds[2]) / 2
        dx = round(step["foot_x"] * facing) if profile else 0
        depth = 0 if profile else round(step["foot_x"] * (1 if direction == 0 else -1) * .5)
        dy = -step["foot_lift"] + min(0, depth)
        hip, ankle = (lx + forward, 21 + bob), (lx + dx, 29 + dy)
        knee = joint(hip, ankle, facing if profile else (.18 if side == 0 else -.18))

        def leg_image(source):
            image = bone(source, (lx, 21), (lx, 25), hip, knee, -.4, 1.15)
            image.alpha_composite(bone(source, (lx, 25), (lx, 29), knee, ankle, -.15, 1.0))
            # The last ankle row belongs to the rigid foot; boots cover it at the same offset.
            paste_at(image, clipped(source, yy == 29), dx, dy)
            return image

        leg_maps.append(leg_image(clipped(SOURCE, yy >= 21)))
        foot_map = Image.new("RGBA", (40, 40))
        paste_at(foot_map, SOURCE, dx, dy)
        foot_maps.append(foot_map)
        leg_mask = (yy >= 21) & ((xx >= 16) if bounds[0] >= 16 else (xx < 16)) & (xx >= 9) & (xx < 22)
        if profile:
            leg_mask = (yy >= 21) & (xx >= 11) & (xx < 22)
        owned |= leg_mask
        cloth_legs.append(leg_image(clipped(SOURCE, leg_mask)))
        shoe_mask = (np.ones((32, 32), bool) if profile else ((xx >= 16) if bounds[0] >= 16 else (xx < 16)))
        shoe = leg_image(clipped(SOURCE, shoe_mask & (yy >= 24) & (yy < 29)))
        paste_at(shoe, clipped(SOURCE, shoe_mask & (yy >= 29)), dx, dy)
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

        arm = frame("Mobs/Species/Human/parts.rsi", name + "_arm", direction)
        arm_bounds = arm.getbbox()
        ax = (arm_bounds[0] + arm_bounds[2]) / 2
        target_x = ax + forward
        swing = -step["foot_x"] * .8
        shoulder = (target_x, 11.5 + bob)
        if spec.get("bent_arms", False):
            if profile:
                elbow = (target_x + (swing * .55 - 1.25) * facing, 16 + bob)
                wrist = (elbow[0] + 3.5 * facing, 15.5 + bob - swing * .3)
            else:
                outward = -1 if ax < 16 else 1
                elbow = (target_x + outward, 16 + bob)
                wrist = (target_x - outward, 17.5 + bob - swing * .35)
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
            chest = np.array(frame("Mobs/Species/Human/parts.rsi", "torso_m", direction))[:, :, 3] > 0
            under_sleeve = (xx >= arm_bounds[0] - (facing < 0)) & (xx < arm_bounds[2] + (facing > 0))
            donor = encoded.copy()
            donor[:, :, 0] = 18 if facing > 0 else 15
            paste_at(torso_fill, clipped(Image.fromarray(donor), chest & under_sleeve & (yy >= 12) & (yy <= 20)),
                     forward, bob)
            arm.alpha_composite(frame("Mobs/Species/Human/parts.rsi", name + "_hand", direction))
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
        # Tone down the rear leg and boot, which share the near leg's profile artwork.
        for maps in (leg_maps, foot_maps, cloth_legs, shoe_maps):
            rear = np.array(maps[back_side])
            rear[:, :, 2] = 112
            maps[back_side] = Image.fromarray(rear)
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
        "clothes-back-leg": cloth_legs[back_side],
        "clothes-front-leg": cloth_legs[front_side],
        "clothes-body": torso,
        "clothes-front-arm": cloth_arms[front_side],
        "clothes-fill": torso_fill,
        "glove-left": glove_maps[front_side], "glove-right": glove_maps[back_side],
        "shoe-left": shoe_maps[front_side], "shoe-right": shoe_maps[back_side],
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


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    states = []
    for prefix, held, spec in (("walk-", False, SPEC), ("held-", True, SPEC),
                               ("run-", False, RUN), ("run-held-", True, RUN)):
        atlases = {role: Image.new("RGBA", (256, 128)) for role in ROLES}
        for direction in range(4):
            for index in range(8):
                for role, result in pose(direction, index, held, spec).items():
                    atlases[role].alpha_composite(encode_lookup(result), (index * 32, direction * 32))
        for role, atlas in atlases.items():
            name = prefix + role
            atlas.save(DEST / (name + ".png"))
            states.append({"name": name, "directions": 4, "delays": [[.1] * 8 for _ in range(4)]})
    (DEST / "meta.json").write_text(json.dumps({"version": 1, "license": "MIT",
        "copyright": "CMU V2 procedural joint-coordinate lookup data; contains no source character pixels.",
        # Lookup bytes are coordinates, not sRGB colors. 128 must reach the shader unchanged.
        "load": {"srgb": False}, "size": {"x": 32, "y": 32}, "states": states}, indent=2) + "\n")
    print(f"Built {len(states)} states x 4 directions x 8 joint poses.")


if __name__ == "__main__":
    main()

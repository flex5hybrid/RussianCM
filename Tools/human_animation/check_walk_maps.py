"""Read-only checks of the shipped coordinate lookup assets; no build, engine or GPU context."""
import json
from pathlib import Path

import numpy as np
from PIL import Image
from build_walk_maps import CLIPS, ROLES, root_offset
from render_review import frame

ROOT = Path(__file__).resolve().parents[2]
RSI = ROOT / "Content.CMU/Resources/Textures/CMU14/Effects/human_walk_v2.rsi"
Y, X = np.mgrid[:32, :32]
PARTS = "Mobs/Species/Human/parts.rsi"
UNIFORM = "_RMC14/Objects/Clothing/Uniforms/Marine/standard.rsi"
BOOTS = "_RMC14/Objects/Clothing/Shoes/Boots/black.rsi"
ARMOR = "_RMC14/Objects/Clothing/OuterClothing/Armor/m3/standard.rsi"
SLEEVELESS = "_RMC14/Objects/Clothing/Uniforms/Marine/standard/classic.rsi"
CLOTH_COMPONENTS = ("clothes-back-arm", "clothes-back-leg", "clothes-front-leg", "clothes-body", "clothes-front-arm")


def sample(mask, sheet, direction, pose):
    data = sheet[direction*32:(direction+1)*32, pose*32:(pose+1)*32].astype(int)
    source_x = np.clip(X + data[:, :, 0] - 128, 0, 31)
    source_y = np.clip(Y + data[:, :, 1] - 128, 0, 31)
    return (data[:, :, 3] != 0) & mask[source_y, source_x]


def check_sprint_arms(sheets):
    for direction, visible_arms in enumerate(("lr", "lr", "r", "l")):
        for letter in visible_arms:
            role = "arm-left" if letter == "l" else "arm-right"
            hand = np.array(frame(PARTS, f"{letter}_hand", direction))[:, :, 3] > 0
            for pose in range(8):
                raised = sample(hand, sheets["run-" + role], direction, pose)
                held = sample(hand, sheets["run-held-" + role], direction, pose)
                assert raised.any() and Y[raised].mean() < Y[held].mean() - 1, (direction, role, pose)


def check_depth_stride(sheets):
    for mode in ("walk", "held", "run", "run-held"):
        for direction in (0, 1):
            feet = [
                (side, np.array(frame(PARTS, f"{letter}_foot", direction))[:, :, 3] > 0)
                for side, letter in (("left", "l"), ("right", "r"))
            ]
            contacts = []
            for index in (0, 4):
                rows = []
                for side, mask in feet:
                    posed = sample(mask, sheets[f"{mode}-foot-{side}"], direction, index)
                    assert posed.sum() == mask.sum(), (mode, direction, side, index, "clipped foot")
                    rows.append(Y[posed].mean())
                contacts.append(rows[0] - rows[1])
            # A contact alternates which foot projects nearer the camera in both views.
            assert min(contacts) < -1 and max(contacts) > 1, (mode, direction, contacts)


def check_stationary_actions(sheets):
    for mode in ("idle", "reach", "reach-held", "melee", "melee-held", "shoot", "fall", "rise"):
        for side in ("left", "right"):
            foot = sheets[f"{mode}-foot-{side}"]
            assert np.all(foot[:, :, :2] == 128), (mode, side, "stationary gesture moves a foot")
            if mode not in ("reach", "melee"):
                assert np.array_equal(sheets[f"{mode}-arm-{side}"], sheets[f"{mode}-root"]), (mode, side)


def check_uniform_coverage(sheets):
    for direction in range(4):
        clothes = np.array(frame(UNIFORM, "equipped-INNERCLOTHING", direction))[:, :, 3] > 0
        boots = np.array(frame(BOOTS, "equipped-FEET", direction))[:, :, 3] > 0
        limbs = [
            (f"{'arm' if part == 'hand' else part}-{side}",
             np.array(frame(PARTS, f"{letter}_{part}", direction))[:, :, 3] > 0)
            for letter, side in (("l", "left"), ("r", "right"))
            for part in ("arm", "hand", "leg", "foot")
        ]
        for mode, _, _ in CLIPS:
            for index in range(8):
                covered = np.logical_or.reduce([
                    *(sample(clothes, sheets[f"{mode}-{role}"], direction, index) for role in CLOTH_COMPONENTS),
                    *(sample(boots, sheets[f"{mode}-{role}"], direction, index) for role in ("shoe-left", "shoe-right")),
                ])
                for role, skin in limbs:
                    posed = sample(skin & (clothes | boots), sheets[f"{mode}-{role}"], direction, index)
                    assert not np.any(posed & ~covered), (mode, role, direction, index)


def expand_state(state):
    assert state["directions"] == len(state["delays"]) == 4
    packed = np.array(Image.open(RSI / (state["name"] + ".png")).convert("RGBA"))
    height, width = packed.shape[:2]
    assert width % 32 == height % 32 == 0
    columns = width // 32
    expanded = np.empty((128, 256, 4), dtype=np.uint8)
    frame_index = 0
    for direction, delays in enumerate(state["delays"]):
        ticks = [round(delay * 10) for delay in delays]
        assert all(tick > 0 and tick / 10 == delay for tick, delay in zip(ticks, delays))
        assert sum(ticks) == 8
        pose_index = 0
        for tick in ticks:
            x, y = frame_index % columns * 32, frame_index // columns * 32
            data = packed[y:y + 32, x:x + 32]
            for _ in range(tick):
                expanded[direction*32:(direction+1)*32, pose_index*32:(pose_index+1)*32] = data
                pose_index += 1
            frame_index += 1
    return expanded


def fit_uniform(mask, direction):
    displace = np.array(frame("Mobs/Species/Human/displacement.rsi", "jumpsuit-female", direction)).astype(int)
    dx = X + displace[:, :, 0] - 128
    dy = Y + displace[:, :, 1] - 128
    valid = (displace[:, :, 3] != 0) & (dx >= 0) & (dx < 32) & (dy >= 0) & (dy < 32)
    return valid & mask[np.clip(dy, 0, 31), np.clip(dx, 0, 31)]


def check_torso_fill(sheets):
    for mode in ("held-", "run-held-"):
        assert not sheets[mode + "clothes-fill"][:, :, 3].any()
    for mode in ("walk-", "run-"):
        assert not sheets[mode + "clothes-fill"][:64, :, 3].any()
    cases = 0
    for direction, side, letter in ((2, "right", "r"), (3, "left", "l")):
        chest = np.array(frame(PARTS, "torso_m", direction))[:, :, 3] > 0
        arm = np.array(frame(PARTS, letter + "_arm", direction))[:, :, 3] > 0
        hand = np.array(frame(PARTS, letter + "_hand", direction))[:, :, 3] > 0
        under_sleeve = (X >= 11) & (X <= 16) if direction == 2 else (X >= 15) & (X <= 20)
        torso = chest & under_sleeve & (Y >= 12) & (Y <= 20)
        for path, state, fitted, has_top in (
            (UNIFORM, "equipped-INNERCLOTHING", False, True),
            (UNIFORM, "equipped-INNERCLOTHING", True, True),
            (ARMOR, "equipped-OUTERCLOTHING", False, True),
            (SLEEVELESS, "sleeveless-equipped-INNERCLOTHING", False, True),
            (UNIFORM, "jacket-equipped-INNERCLOTHING", False, False),
            (UNIFORM, "jacket-equipped-INNERCLOTHING", True, False),
        ):
            clothes = np.array(frame(path, state, direction))[:, :, 3] > 0
            if fitted:
                clothes = fit_uniform(clothes, direction)
            # The shader requires fabric in the selected row and the row above it.
            donors = clothes & clothes[np.maximum(0, Y - 1), X]
            repaired = 0
            for mode in ("walk-", "run-"):
                for pose in range(8):
                    patch = sample(donors, sheets[mode + "clothes-fill"], direction, pose)
                    exposed = (sample(torso, sheets[mode + "root"], direction, pose) &
                               ~sample(arm | hand, sheets[mode + "arm-" + side], direction, pose))
                    assert not np.any(patch & ~exposed), (state, direction, mode, pose)
                    covered = np.logical_or.reduce([
                        sample(clothes, sheets[mode + role], direction, pose) for role in CLOTH_COMPONENTS
                    ])
                    gap = exposed & ~covered
                    if has_top:
                        assert not np.any(gap & ~patch), (state, direction, fitted, mode, pose)
                    else:
                        assert not patch.any(), (state, direction, fitted, mode, pose)
                    repaired += np.count_nonzero(gap & patch)
                    cases += 1
            assert (repaired > 0) == has_top, (state, direction, fitted)
    return cases


def check_profile_coverage(sheets):
    """Previously clothed limb pixels must stay covered in both profile directions."""
    cases = 0
    for direction in (2, 3):
        rows = slice(direction * 32, (direction + 1) * 32)
        for mode in ("walk-", "held-", "run-", "run-held-"):
            assert not sheets[mode + "clothes-back-arm"][rows, :, 3].any(), (direction, mode)
            for rear, near in (("clothes-back-leg", "clothes-front-leg"), ("shoe-right", "shoe-left")):
                back = sheets[mode + rear][rows]
                assert np.all(back[:, :, 2][back[:, :, 3] != 0] > 0), (direction, mode, rear)
                assert not sheets[mode + near][rows, :, 2].any(), (direction, mode, near)
            if mode in ("walk-", "run-"):
                # The far glove may sample the tiny original far hand but never the near side.
                near_side = X < 18 if direction == 2 else X >= 14
                for pose in range(8):
                    assert not sample(near_side, sheets[mode + "glove-right"], direction, pose).any()
        boots = np.array(frame(BOOTS, "equipped-FEET", direction))[:, :, 3] > 0
        for outfit in ("equipped-INNERCLOTHING", "jacket-equipped-INNERCLOTHING"):
            uniform = np.array(frame(UNIFORM, outfit, direction))[:, :, 3] > 0
            for fitted in (False, True):
                clothes = fit_uniform(uniform, direction) if fitted else uniform
                original_coverage = clothes | boots
                for mode in ("walk-", "held-", "run-", "run-held-"):
                    for pose in range(8):
                        coverage = np.logical_or.reduce([
                            *(sample(clothes, sheets[mode + role], direction, pose) for role in CLOTH_COMPONENTS),
                            *(sample(boots, sheets[mode + role], direction, pose) for role in ("shoe-left", "shoe-right")),
                        ])
                        for letter, side in (("l", "left"), ("r", "right")):
                            for part in ("arm", "hand", "leg", "foot"):
                                skin = np.array(frame(PARTS, f"{letter}_{part}", direction))[:, :, 3] > 0
                                role = f"{'arm' if part == 'hand' else part}-{side}"
                                posed = sample(skin & original_coverage, sheets[mode + role], direction, pose)
                                assert not np.any(posed & ~coverage), (direction, outfit, fitted, mode, pose, role)
                        cases += 1
    return cases


def main():
    meta = json.loads((RSI / "meta.json").read_text())
    assert meta.get("load", {}).get("srgb") is False, "Coordinate data must bypass sRGB conversion"
    assert meta["size"] == {"x": 32, "y": 32}
    studies = Path(__file__).parent / "studies"
    run = json.loads((studies / "run-v2.json").read_text())
    assert run["poses"][0]["foot_lift"] == 0 and run["poses"][4]["foot_lift"] > 0
    assert run["poses"][3]["foot_lift"] > 0 and run["poses"][7]["foot_lift"] > 0
    clips = {name: spec for name, _, spec in CLIPS}
    assert {state["name"] for state in meta["states"]} == {f"{name}-{role}" for name in clips for role in ROLES}
    yy, xx = np.mgrid[:32, :32]
    poses = pixels = 0
    sheets = {}
    for state in meta["states"]:
        sheet = expand_state(state)
        sheets[state["name"]] = sheet
        for direction in range(4):
            for index in range(8):
                data = sheet[direction*32:(direction+1)*32, index*32:(index+1)*32].astype(int)
                visible = data[:, :, 3] != 0
                assert np.all((data[:, :, 3] == 0) | (data[:, :, 3] == 255)), "Lookups must not interpolate pixels"
                source_x = xx + data[:, :, 0] - 128
                # WorldTextureBoundsToUV uses bottom-up UVs; the shader negates positive green.
                source_y = yy + data[:, :, 1] - 128
                assert np.all((source_x[visible] >= 0) & (source_x[visible] < 32)), state["name"]
                assert np.all((source_y[visible] >= 0) & (source_y[visible] < 32)), state["name"]
                if state["name"].endswith("-root"):
                    forward, down = root_offset(direction, index, clips[state["name"][:-5]])
                    assert np.all(source_x[visible] == xx[visible] - forward), "Rigid root must preserve horizontal pixels"
                    assert np.all(source_y[visible] == yy[visible] - down), "Root must move down, not up"
                    bounds = (xx - forward >= 0) & (xx - forward < 32) & (yy - down >= 0) & (yy - down < 32)
                    assert np.array_equal(visible, bounds), "Root must preserve every in-frame source pixel"
                poses += 1
                pixels += np.count_nonzero(visible)
    shaders = ROOT / "Content.CMU/Resources/Textures/CMU14/Shaders"
    lit = (shaders / "human_walk_v2.swsl").read_text(encoding="utf-8-sig").strip()
    unlit = (shaders / "human_walk_v2_unshaded.swsl").read_text(encoding="utf-8-sig").strip()
    assert unlit == "light_mode unshaded;\n" + lit, "Lit/unlit lookup math must match"
    check_sprint_arms(sheets)
    check_depth_stride(sheets)
    check_stationary_actions(sheets)
    check_uniform_coverage(sheets)
    outfits = check_profile_coverage(sheets)
    torso_cases = check_torso_fill(sheets)
    print(f"OK: linear lookup loading; {poses} poses; {pixels} visible coordinate samples stay inside their 32x32 frame; rigid-root direction; lit/unlit parity.")
    print(f"OK: no newly exposed sleeve, trouser or boot pixels in {outfits} profile outfit/pose cases.")
    print("OK: raised sprint wrists; no copied profile sleeve or glove; rear leg and boot shading.")
    print(f"OK: alternating front/rear contacts in walk and sprint, and uniform coverage in all {len(clips)} clips.")
    print(f"OK: repaired clothing gaps and preserved bare arms and open tops in {torso_cases} profile outfit/pose cases.")


if __name__ == "__main__":
    main()

"""Generates the overlays of the civilian vehicle tuning modules on the colonist vehicles: the pickup (civtruck), the blue
box truck, the car and the hippie van. The pizza van has its own script (pizza_van.py), which this one follows.

Run from the repository root:  python -I Content.CMU/Design/Vehicles/TuningSprites/vehicles.py [preview-dir]

Each vehicle is described by where things are on its sprite, read off its base frames: the edges of the body and the
windows from above (south with the front down, north with the front up), and from the side (east, front to the right)
the panels that may be plated, which leave the doors free, the windows, the front wing, the wheels and the roof. West
is east mirrored and shifted to where the vehicle's own west frame sits. With a directory argument it also writes a 3x
preview of every overlay on each vehicle.
"""
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import png  # noqa: E402
from generate import (Canvas, S0, S1, S2, S3, S4, S5, R1, R2, W1, W2, T1, T2, T3, T4, L1, L2, F1, F2)  # noqa: E402

ROOT = os.getcwd()
OUT_DIR = os.path.join(ROOT, "Content.CMU", "Resources", "Textures", "CMU14", "Structures", "vehicles")
SOUTH, NORTH, EAST = 0, 1, 2

# bl, br: the left and right edges of the body; span: the rows the side armour covers; win: the window to guard
# (x0, x1, y0, y1); wheels: the rows of each wheel at the sides; bar: the bullbar (x0, x1, y, outward: +1 down, -1 up);
# hood, trunk: the rows of the bonnet and of the back end, plated (cut to the body) by standard and heavy armour.
# East: sides, the panels to plate (light and standard; heavy has its own); wins, the windows; hood and trunk, the
# boxes the front and back end are plated in, cut to the body;
# wheels (x0, x1) and the row of their tops; bar (x, y0, y1). The turret is drawn by turret_overlay.py.
VEHICLES = {
    "civ_truck": {
        "base": ("Content.CMU/Resources/Textures/CMU14/Structures/vehicles/hybrisavehicles/van.rsi", "truck_base", "wheels_1"),
        "size": 96, "west_shift": 0,
        SOUTH: dict(bl=26, br=69, span=(10, 86), win=(33, 61, 71, 76), wheels=[(15, 27), (44, 55), (79, 89)],
                    bar=(28, 67, 90, 1), hood=(78, 88), trunk=(8, 12)),
        NORTH: dict(bl=26, br=69, span=(6, 90), win=(32, 62, 25, 31), wheels=[(3, 12), (28, 44), (71, 88)],
                    bar=(28, 67, 1, -1), hood=(4, 12), trunk=(86, 90)),
        EAST: dict(sides=[(2, 60, 39, 57)], heavy=[(1, 60, 44, 58)], wins=[(63, 73, 46, 51)],
                   hood=(81, 94, 33, 53), trunk=(1, 5, 30, 58),
                   wheels=[(7, 23), (35, 53), (78, 89)], wheel_top=59, bar=(91, 44, 66)),
    },
    "blue_truck": {
        "base": ("Content.CMU/Resources/Textures/CMU14/Structures/vehicles/hybrisavehicles/small_truck_blue.rsi",
                 "small_truck_blue", "wheels_0"),
        "size": 96, "west_shift": 0,
        SOUTH: dict(bl=27, br=68, span=(14, 78), win=(34, 62, 57, 62), wheels=[(13, 27), (35, 48), (66, 78)],
                    bar=(29, 66, 81, 1), hood=(64, 77), trunk=(11, 15)),
        NORTH: dict(bl=27, br=68, span=(16, 82), win=(41, 56, 30, 34), wheels=[(17, 30), (49, 62), (69, 83)],
                    bar=(29, 66, 10, -1), hood=None, trunk=(75, 83)),
        EAST: dict(sides=[(18, 56, 40, 58)], heavy=[(17, 57, 43, 59)], wins=[(63, 72, 44, 49)],
                   hood=(77, 84, 36, 60), trunk=(15, 19, 25, 60),
                   wheels=[(17, 31), (36, 49), (64, 78)], wheel_top=61, bar=(85, 40, 64)),
    },
    "car": {
        "base": ("Content.RUMC/Resources/Textures/CMU14/Structures/vehicles/car.rsi", "car_base", "wheels_1"),
        "size": 96, "west_shift": -2,
        SOUTH: dict(bl=33, br=62, span=(20, 70), win=(36, 59, 47, 54), wheels=[(24, 33), (63, 72)],
                    bar=(31, 64, 72, 1), hood=(56, 68), trunk=(18, 28)),
        NORTH: dict(bl=33, br=62, span=(22, 72), win=(36, 59, 49, 56), wheels=[(23, 32), (62, 71)],
                    bar=(31, 64, 15, -1), hood=(18, 30), trunk=(58, 70)),
        EAST: dict(sides=[(24, 58, 57, 59)], heavy=[(24, 58, 56, 60)],
                   wins=[(26, 43, 43, 50), (46, 58, 43, 50)], hood=(61, 82, 30, 58), trunk=(9, 24, 30, 60),
                   wheels=[(17, 28), (64, 75)], wheel_top=56, bar=(82, 36, 60)),
    },
    "hippie_van": {
        "base": ("Content.RUMC/Resources/Textures/CMU14/Structures/vehicles/hippie_van.rsi", "hippie_van_base", "wheels_1"),
        "size": 96, "west_shift": -3,
        SOUTH: dict(bl=33, br=62, span=(20, 72), win=(36, 59, 52, 60), wheels=[(24, 34), (61, 70)],
                    bar=(31, 64, 74, 1), hood=(61, 73), trunk=(18, 22)),
        NORTH: dict(bl=33, br=62, span=(20, 72), win=(36, 59, 52, 60), wheels=[(22, 31), (58, 68)],
                    bar=(31, 64, 15, -1), hood=(18, 22), trunk=(61, 73)),
        EAST: dict(sides=[(9, 41, 53, 62), (58, 71, 53, 62)], heavy=[(8, 41, 50, 63), (58, 72, 50, 63)],
                   wins=[(14, 25, 39, 50), (29, 41, 39, 50), (44, 56, 39, 50), (59, 71, 39, 50)],
                   hood=(72, 82, 50, 63), trunk=(7, 13, 50, 63),
                   wheels=[(16, 29), (64, 76)], wheel_top=58, bar=(82, 38, 64)),
    },
}


def bolts_v(c, x, y0, y1, step=6):
    for y in range(y0, y1, step):
        c.bolt(x, y)


def bolts_h(c, x0, x1, y, step=6):
    for x in range(x0, x1, step):
        c.bolt(x, y)


def seams(c, x0, x1, y0, y1, color, every=14):
    for x in range(x0 + every, x1 - 3, every):
        c.vline(x, y0 + 1, y1 - 1, color)


MASKS = {}


def body(g, d):
    """The pixels of the vehicle in a direction, to cut plates to."""
    key = (g["base"], d)
    if key not in MASKS:
        rsi, state, _ = g["base"]
        MASKS[key] = set(base_frames(rsi, state, g["size"])[d])
    return MASKS[key]


def cut_plate(c, mask, x0, x1, y0, y1, fill, light, shade, inset=1, bolts=True):
    """A plate over the box, cut to the body less an inset: lit where it starts, shaded where it ends."""
    inner = {(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1)
             if all((x + dx, y + dy) in mask for dx in range(-inset, inset + 1) for dy in range(-inset, inset + 1))}
    for x, y in inner:
        if (x, y - 1) not in inner or (x - 1, y) not in inner:
            c.set(x, y, light)
        elif (x, y + 1) not in inner or (x + 1, y) not in inner:
            c.set(x, y, shade)
        else:
            c.set(x, y, fill)
    if bolts and inner:
        xs = sorted({x for x, _ in inner})
        ys = sorted({y for _, y in inner})
        # A tall plate is two sheets: a seam across the middle with a row of bolts along it.
        if ys[-1] - ys[0] >= 12:
            seam = (ys[0] + ys[-1]) // 2
            for x in xs:
                if (x - 1, seam) in inner and (x + 1, seam) in inner:
                    c.set(x, seam, shade)
            for x in range(xs[0] + 2, xs[-1] - 1, 5):
                if (x, seam + 2) in inner and (x, seam + 3) in inner:
                    c.bolt(x, seam + 2)
        for bx in (xs[0] + 2, xs[-1] - 2):
            for by in (ys[0] + 2, ys[-1] - 3):
                if (bx, by) in inner and (bx, by + 1) in inner:
                    c.bolt(bx, by)
    return inner


def top_ends(c, g, mask, heavy):
    """The bonnet and the back end from above."""
    for rows in (g.get("hood"), g.get("trunk")):
        if not rows:
            continue
        y0, y1 = rows
        if heavy:
            cut_plate(c, mask, g["bl"], g["br"], y0, y1, W1, W2, S0, bolts=False)
            cut_plate(c, mask, g["bl"], g["br"], y0, y1, S2, S3, S0, inset=2)
        else:
            cut_plate(c, mask, g["bl"], g["br"], y0, y1, S3, S4, S1)


def side_ends(c, g, mask, heavy, light=False):
    """The bonnet and the back end from the side."""
    for x0, x1, y0, y1 in (g["hood"], g["trunk"]):
        if light:
            cut_plate(c, mask, x0, x1, max(y0, y1 - 7), y1, T2, T3, T1, bolts=False)
        elif heavy:
            cut_plate(c, mask, x0, x1, y0, y1, W1, W2, S0, bolts=False)
            cut_plate(c, mask, x0, x1, y0, y1, S2, S3, S0, inset=2)
        else:
            cut_plate(c, mask, x0, x1, y0, y1, S3, S4, S1)


# From above.

def top_armour_light(c, g):
    y0, y1 = g["span"]
    for x in (g["bl"] - 1, g["br"]):
        c.rect(x, y0 + 2, x + 1, y1 - 2, T2)
        c.vline(x, y0 + 2, y1 - 2, T3)
        bolts_v(c, x + 1, y0 + 5, y1 - 3)


def top_armour(c, g, d, mask):
    y0, y1 = g["span"]
    top_ends(c, g, mask, heavy=False)
    for x in (g["bl"] - 2, g["br"]):
        c.plate(x, y0, x + 2, y1, S3, S4, S1, S5)
        for y in range(y0 + 15, y1 - 4, 15):
            c.hline(x, x + 2, y, S1)
        bolts_v(c, x + 1, y0 + 3, y1 - 2)
    wx0, wx1, wy0, wy1 = g["win"]
    for y in range(wy0, wy1 + 1, 2):
        c.hline(wx0, wx1, y, S2)
    for x in (wx0 + (wx1 - wx0) // 4, wx1 - (wx1 - wx0) // 4):
        c.vline(x, wy0, wy1, S1)


def top_armour_heavy(c, g, mask):
    y0, y1 = g["span"]
    mid = (y0 + y1) // 2
    top_ends(c, g, mask, heavy=True)
    for x in (g["bl"] - 3, g["br"]):
        c.plate(x, y0 - 2, x + 3, y1 + 2, W1, W2, S0)
        c.plate(x + 1, y0 + 3, x + 2, mid - 2, L1, L2, S0)
        c.plate(x + 1, mid + 2, x + 2, y1 - 3, S2, S3, S0)
        c.weld(x, mid, x + 3, mid)
        bolts_v(c, x + 1, y0, y1, 8)
    wx0, wx1, wy0, wy1 = g["win"]
    c.plate(wx0 - 1, wy0 - 1, wx1 + 1, wy1 + 1, S2, S3, S0, S4)
    my = (wy0 + wy1) // 2
    c.hline(wx0 + 4, wx1 - 4, my, R1)
    c.hline(wx0 + 4, wx1 - 4, my + 1, R2)
    c.weld(wx0 - 1, wy1 + 1, wx1 + 1, wy1 + 1)


def top_wheels(c, g):
    for y0, y1 in g["wheels"]:
        for x in (g["bl"] - 4, g["br"] + 1):
            c.plate(x, y0, x + 3, y1, S3, S4, S1)
            c.bolt(x + 1, y0 + 2)
            if y1 - y0 > 6:
                c.bolt(x + 1, y1 - 3)


def spike(c, x, y, dx, dy, length=3):
    for i in range(length):
        c.set(x + dx * i, y + dy * i, S5 if i == length - 1 else S3)
        if i == 0:
            c.set(x + dx * i + dy, y + dy * i + dx, S2)


def top_bullbar(c, g):
    x0, x1, y, out = g["bar"]
    c.rect(x0, y, x1, y + 2, S2)
    c.hline(x0, x1, y, S4)
    tip = y + 3 if out > 0 else y - 1
    for x in range(x0 + 1, x1, 4):
        spike(c, x, tip, 0, out, 3)
    if out > 0:
        for x in (x0 + 6, x1 - 7):
            c.rect(x, y - 6, x + 1, y + 2, S3)


# From the side (east).

def side_armour_light(c, g, mask):
    side_ends(c, g, mask, heavy=False, light=True)
    for x0, x1, y0, y1 in g["sides"]:
        top = max(y0, y1 - 6)
        c.plate(x0, top, x1, y1, T2, T3, T1, T4)
        bolts_h(c, x0 + 2, x1, top + 2)


def side_armour(c, g, mask):
    side_ends(c, g, mask, heavy=False)
    for x0, x1, y0, y1 in g["sides"]:
        c.plate(x0, y0, x1, y1, S3, S4, S1, S5)
        seams(c, x0, x1, y0, y1, S1)
        if y1 - y0 >= 6:
            bolts_h(c, x0 + 2, x1, y0 + 2)
            bolts_h(c, x0 + 2, x1, y1 - 3)
    for x0, x1, y0, y1 in g["wins"]:
        for x in range(x0 + 1, x1, 2):
            c.vline(x, y0 + 1, y1 - 1, S2)
        c.hline(x0, x1, y0, S1)


def side_armour_heavy(c, g, mask):
    side_ends(c, g, mask, heavy=True)
    for x0, x1, y0, y1 in g["heavy"]:
        c.plate(x0, y0, x1, y1, W1, W2, S0)
        if y1 - y0 >= 18:
            mid = (y0 + y1) // 2
            c.plate(x0 + 2, y0 + 2, x1 - 2, mid - 1, L1, L2, S0)
            c.plate(x0 + 2, mid + 2, x1 - 2, y1 - 2, S2, S3, S0, S4)
            c.weld(x0, mid, x1, mid)
            bolts_h(c, x0 + 4, x1 - 2, mid + 4, 7)
        else:
            c.plate(x0 + 1, y0 + 1, x1 - 1, y1 - 1, S2, S3, S0)
    for x0, x1, y0, y1 in g["wins"]:
        c.plate(x0, y0, x1, y1, S2, S3, S0)
        my = (y0 + y1) // 2
        c.hline(x0 + 2, x1 - 2, my, R1)
        c.hline(x0 + 2, x1 - 2, my + 1, R2)


def side_wheels(c, g):
    top = g["wheel_top"]
    for x0, x1 in g["wheels"]:
        c.plate(x0 - 1, top - 3, x1 + 1, top + 3, S3, S4, S1, S5)
        c.hline(x0 - 1, x1 + 1, top, S2)
        bolts_h(c, x0 + 1, x1, top - 2, 5)
        c.weld(x0 - 1, top + 3, x1 + 1, top + 3)


def side_bullbar(c, g):
    x, y0, y1 = g["bar"]
    c.rect(x, y0, x + 2, y1, S2)
    c.vline(x, y0, y1, S4)
    for y in (y0 + 6, y1 - 6):
        c.rect(x - 4, y, x + 2, y + 1, S3)
    for y in range(y0 + 1, y1, 4):
        spike(c, x + 3, y, 1, 0, 3)


def build(name, g, d):
    """One direction of one state on one vehicle."""
    c = Canvas(g["size"], g["size"])
    if d in (SOUTH, NORTH):
        top = g[d]
        {
            "armour_light": lambda: top_armour_light(c, top),
            "armour": lambda: top_armour(c, top, d, body(g, d)),
            "armour_heavy": lambda: top_armour_heavy(c, top, body(g, d)),
            "wheels_armoured": lambda: top_wheels(c, top),
            "bullbar_spiked": lambda: top_bullbar(c, top),
        }[name]()
    else:
        side = g[EAST]
        {
            "armour_light": lambda: side_armour_light(c, side, body(g, EAST)),
            "armour": lambda: side_armour(c, side, body(g, EAST)),
            "armour_heavy": lambda: side_armour_heavy(c, side, body(g, EAST)),
            "wheels_armoured": lambda: side_wheels(c, side),
            "bullbar_spiked": lambda: side_bullbar(c, side),
        }[name]()
    c.outline()
    return c


STATES = ["armour_light", "armour", "armour_heavy", "wheels_armoured", "bullbar_spiked"]


def directions(name, g):
    size = g["size"]
    east = build(name, g, EAST)
    west = Canvas(size, size)
    for (x, y), color in east.px.items():
        west.set(size - 1 - x + g["west_shift"], y, color)
    return [build(name, g, SOUTH), build(name, g, NORTH), east, west]


def save(path, size, states, source):
    os.makedirs(path, exist_ok=True)
    meta_states = []
    for name, dirs in states.items():
        rows = [[(0, 0, 0, 0)] * (size * 2) for _ in range(size * 2)]
        for i, c in enumerate(dirs):
            ox, oy = (i % 2) * size, (i // 2) * size
            for (x, y), color in c.px.items():
                rows[oy + y][ox + x] = color
        png.write(os.path.join(path, name + ".png"), size * 2, size * 2, rows)
        meta_states.append({"name": name, "directions": 4})
    meta = {"version": 1, "license": "CC-BY-SA-3.0",
            "copyright": "Generated by Content.CMU/Design/Vehicles/TuningSprites/vehicles.py for CMU, fitted to "
                         f"{source}, palette after the RMC14 vehicle hardpoint icons",
            "size": {"x": size, "y": size}, "states": meta_states}
    with open(os.path.join(path, "meta.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(meta, f, indent=2)
        f.write("\n")


def base_frames(rsi, state, size):
    path = os.path.join(ROOT, rsi)
    meta = json.load(open(os.path.join(path, "meta.json"), encoding="utf-8-sig"))
    entry = next(s for s in meta["states"] if s["name"] == state)
    per = len(entry["delays"][0]) if "delays" in entry else 1
    width, _, px = png.read(os.path.join(path, state + ".png"))
    cols = width // size
    out = []
    for d in range(4):
        i = d * per
        cx, cy = (i % cols) * size, (i // cols) * size
        out.append({(x, y): px[cy + y][cx + x] for y in range(size) for x in range(size) if px[cy + y][cx + x][3]})
    return out


def preview(path, g, states, scale=3):
    size = g["size"]
    rsi, base_state, wheel_state = g["base"]
    base, wheels = base_frames(rsi, base_state, size), base_frames(rsi, wheel_state, size)
    sets = [[]] + list(states.values())
    width, height = 4 * size * scale, len(sets) * size * scale
    rows = [[(200, 200, 200, 255) if ((x // 12 + y // 12) % 2) else (225, 225, 225, 255) for x in range(width)]
            for y in range(height)]
    for r, dirs in enumerate(sets):
        for d in range(4):
            for layer in [base[d], wheels[d]] + ([dirs[d].px] if dirs else []):
                for (x, y), color in layer.items():
                    for dy in range(scale):
                        for dx in range(scale):
                            rows[r * size * scale + y * scale + dy][d * size * scale + x * scale + dx] = color
    png.write(path, width, height, rows)


def main():
    for vehicle, g in VEHICLES.items():
        states = {name: directions(name, g) for name in STATES}
        save(os.path.join(OUT_DIR, f"tuning_{vehicle}.rsi"), g["size"], states, os.path.basename(g["base"][0]))
        if len(sys.argv) > 1:
            preview(os.path.join(sys.argv[1], f"preview_{vehicle}.png"), g, states)


if __name__ == "__main__":
    main()

"""Generates the overlays of the civilian vehicle tuning modules on the pizza van (_RMC14/Structures/Vehicles/pizza_van.rsi).

Run from the repository root:  python -I Content.CMU/Design/Vehicles/TuningSprites/pizza_van.py [preview.png]

Every state has the four directions of the van (south, north, east, west) at 64x64 and is drawn over its base sprite as
a layer of its own (see the pizza van in Resources/Prototypes/_RMC14/Vehicles/Van/vehicles.yml). West is east mirrored.
With a path argument it also writes a 6x preview of every overlay on the van.
"""
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import png  # noqa: E402
from generate import (Canvas, OUT, S0, S1, S2, S3, S4, S5, R1, R2, W1, W2, W3, T1, T2, T3, T4, L1, L2,  # noqa: E402
                      F1, F2)

ROOT = os.getcwd()
VAN_RSI = os.path.join(ROOT, "Resources", "Textures", "_RMC14", "Structures", "Vehicles", "pizza_van.rsi")
OUT_RSI = os.path.join(ROOT, "Content.CMU", "Resources", "Textures", "CMU14", "Structures", "vehicles",
                       "tuning_pizza_van.rsi")
SIZE = 64
SOUTH, NORTH, EAST = 0, 1, 2
# The bonnet: rows from above (south only: from the north the back doors are there) and the box from the side.
HOOD_SOUTH = (16, 46, 46, 55)
HOOD_EAST = (57, 63, 35, 52)
_MASKS = {}


def body(d):
    """The pixels of the van in a direction, to cut plates to."""
    if d not in _MASKS:
        _MASKS[d] = set(van_frames("van_base", 2)[d])
    return _MASKS[d]


def hood(c, d, heavy):
    """The bonnet plated, cut to the body: from above (south) and from the side."""
    from vehicles import cut_plate
    if d == NORTH:
        return
    x0, x1, y0, y1 = HOOD_SOUTH if d == SOUTH else HOOD_EAST
    if heavy:
        cut_plate(c, body(d), x0, x1, y0, y1, W1, W2, S0, bolts=False)
        cut_plate(c, body(d), x0, x1, y0, y1, S2, S3, S0, inset=2)
    else:
        cut_plate(c, body(d), x0, x1, y0, y1, S3, S4, S1)


def mirror(c):
    m = Canvas(SIZE, SIZE)
    for (x, y), color in c.px.items():
        m.set(SIZE - 1 - x, y, color)
    return m


def bolts_v(c, x, y0, y1, step=6):
    for y in range(y0, y1, step):
        c.bolt(x, y)


def bolts_h(c, x0, x1, y, step=6):
    for x in range(x0, x1, step):
        c.bolt(x, y)


# Armour. From above (south, north) it hangs along both sides of the body and guards the windscreen; from the side
# (east) it covers the panels between the wheels.

def armour_light(d):
    c = Canvas(SIZE, SIZE)
    if d in (SOUTH, NORTH):
        for x in (14, 48):
            c.rect(x, 8, x + 1, 54, T2)
            c.vline(x, 8, 54, T3)
            bolts_v(c, x + 1, 11, 52)
    else:
        c.plate(21, 40, 39, 46, T2, T3, T1, T4)
        c.plate(4, 40, 9, 46, T2, T3, T1)
        bolts_h(c, 23, 39, 42)
        c.hline(21, 39, 43, T1)
    c.outline()
    return c


def armour(d):
    c = Canvas(SIZE, SIZE)
    if d in (SOUTH, NORTH):
        hood(c, d, heavy=False)
        for x in (13, 47):
            c.plate(x, 6, x + 2, 56, S3, S4, S1, S5)
            for y in (20, 36):
                c.hline(x, x + 2, y, S1)
            bolts_v(c, x + 1, 9, 54)
        # Bars over the windscreen (south) or the rear window (north).
        y0 = 39 if d == SOUTH else 40
        for y in range(y0, y0 + 6, 2):
            c.hline(19, 43, y, S2)
        c.vline(25, y0, y0 + 5, S1)
        c.vline(37, y0, y0 + 5, S1)
    else:
        # Side plates from the back to the door (x 43-56 stays free), and a plate on the front wing.
        c.plate(3, 32, 41, 46, S3, S4, S1, S5)
        for x in (16, 29):
            c.vline(x, 33, 45, S1)
        bolts_h(c, 5, 41, 34)
        bolts_h(c, 5, 41, 43)
        hood(c, d, heavy=False)
        # Bars over the door window.
        for x in range(45, 55, 2):
            c.vline(x, 30, 35, S2)
        c.hline(44, 55, 29, S1)
    c.outline()
    return c


def armour_heavy(d):
    c = Canvas(SIZE, SIZE)
    if d in (SOUTH, NORTH):
        hood(c, d, heavy=True)
        for x in (12, 46):
            c.plate(x, 3, x + 3, 58, W1, W2, S0)
            c.plate(x + 1, 10, x + 2, 28, L1, L2, S0)
            c.plate(x + 1, 34, x + 2, 52, S2, S3, S0)
            c.weld(x, 30, x + 3, 30)
            bolts_v(c, x + 1, 6, 56, 8)
        # The window plated over, a slit left to see through.
        y0 = 38 if d == SOUTH else 39
        c.plate(18, y0, 44, y0 + 7, S2, S3, S0, S4)
        c.hline(22, 40, y0 + 3, R1)
        c.hline(22, 40, y0 + 4, R2)
        c.weld(18, y0 + 7, 44, y0 + 7)
        # Plates welded along the edges of the roof.
        c.plate(18, 2 if d == SOUTH else 1, 44, 5 if d == SOUTH else 4, S2, S3, S0)
    else:
        # A welded shell over the side up to the door, which stays free; the door window is plated over to a
        # slit, and the front wing gets a plate of its own.
        c.plate(2, 29, 41, 47, W1, W2, S0)
        c.plate(4, 31, 21, 38, L1, L2, S0)
        c.plate(23, 31, 39, 38, S2, S3, S0)
        c.plate(4, 41, 39, 45, S2, S3, S0, S4)
        c.weld(2, 39, 41, 39)
        c.weld(22, 31, 22, 38)
        bolts_h(c, 6, 39, 43, 7)
        c.plate(44, 29, 55, 36, S2, S3, S0)
        c.hline(46, 53, 32, R1)
        c.hline(46, 53, 33, R2)
        hood(c, d, heavy=True)
    c.outline()
    return c


def wheels_armoured(d):
    c = Canvas(SIZE, SIZE)
    if d in (SOUTH, NORTH):
        for x in (12, 48):
            for y0, y1 in ((11, 25), (43, 57)):
                c.plate(x, y0, x + 3, y1, S3, S4, S1)
                c.bolt(x + 1, y0 + 2)
                c.bolt(x + 1, y1 - 3)
    else:
        # Guards over the top of each wheel, down to the hub.
        for x0, x1 in ((5, 22), (38, 56)):
            c.plate(x0, 44, x1, 50, S3, S4, S1, S5)
            c.hline(x0, x1, 47, S2)
            bolts_h(c, x0 + 2, x1, 45, 5)
            c.weld(x0, 50, x1, 50)
    c.outline()
    return c


def spike(c, x, y, dx, dy, length=3):
    for i in range(length):
        c.set(x + dx * i, y + dy * i, S5 if i == length - 1 else S3)
        if i == 0:
            c.set(x + dx * i + dy, y + dy * i + dx, S2)


def bullbar_spiked(d):
    c = Canvas(SIZE, SIZE)
    if d == SOUTH:
        c.rect(14, 56, 48, 58, S2)
        c.hline(14, 48, 56, S4)
        for x in (20, 42):
            c.rect(x, 50, x + 1, 58, S3)
        for x in range(15, 48, 4):
            spike(c, x, 59, 0, 1, 4)
    elif d == NORTH:
        c.rect(15, 2, 47, 4, S2)
        c.hline(15, 47, 2, S4)
        for x in range(16, 47, 4):
            spike(c, x, 1, 0, -1, 2)
    else:
        c.rect(58, 30, 60, 56, S2)
        c.vline(58, 30, 56, S4)
        for y in (36, 50):
            c.rect(54, y, 60, y + 1, S3)
        for y in range(31, 56, 4):
            spike(c, 61, y, 1, 0, 3)
    c.outline()
    return c


STATES = {
    "armour_light": armour_light,
    "armour": armour,
    "armour_heavy": armour_heavy,
    "wheels_armoured": wheels_armoured,
    "bullbar_spiked": bullbar_spiked,
}


def directions(build):
    east = build(EAST)
    return [build(SOUTH), build(NORTH), east, mirror(east)]


def save(states):
    os.makedirs(OUT_RSI, exist_ok=True)
    meta_states = []
    for name, dirs in states.items():
        rows = [[(0, 0, 0, 0)] * (SIZE * 2) for _ in range(SIZE * 2)]
        for i, c in enumerate(dirs):
            ox, oy = (i % 2) * SIZE, (i // 2) * SIZE
            for (x, y), color in c.px.items():
                rows[oy + y][ox + x] = color
        png.write(os.path.join(OUT_RSI, name + ".png"), SIZE * 2, SIZE * 2, rows)
        meta_states.append({"name": name, "directions": 4})
    meta = {"version": 1, "license": "CC-BY-SA-3.0",
            "copyright": "Generated by Content.CMU/Design/Vehicles/TuningSprites/pizza_van.py for CMU, fitted to "
                         "pizza_van.rsi (cmss13), palette after the RMC14 vehicle hardpoint icons",
            "size": {"x": SIZE, "y": SIZE}, "states": meta_states}
    with open(os.path.join(OUT_RSI, "meta.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(meta, f, indent=2)
        f.write("\n")


def van_frames(name, frames_per_dir):
    width, _, px = png.read(os.path.join(VAN_RSI, name + ".png"))
    cols = width // SIZE
    out = []
    for d in range(4):
        i = d * frames_per_dir
        cx, cy = (i % cols) * SIZE, (i // cols) * SIZE
        out.append({(x, y): px[cy + y][cx + x] for y in range(SIZE) for x in range(SIZE) if px[cy + y][cx + x][3]})
    return out


def preview(path, states, scale=4):
    base, wheels = van_frames("van_base", 2), van_frames("wheels_1", 1)
    sets = [[]] + list(states.values())
    width, height = 4 * SIZE * scale, len(sets) * SIZE * scale
    rows = [[(200, 200, 200, 255) if ((x // 16 + y // 16) % 2) else (225, 225, 225, 255) for x in range(width)]
            for y in range(height)]
    for r, dirs in enumerate(sets):
        for d in range(4):
            layers = [base[d], wheels[d]] + ([dirs[d].px] if dirs else [])
            for layer in layers:
                for (x, y), color in layer.items():
                    for dy in range(scale):
                        for dx in range(scale):
                            rows[r * SIZE * scale + y * scale + dy][d * SIZE * scale + x * scale + dx] = color
    png.write(path, width, height, rows)


def main():
    states = {name: directions(build) for name, build in STATES.items()}
    save(states)
    if len(sys.argv) > 1:
        preview(sys.argv[1], states)


if __name__ == "__main__":
    main()

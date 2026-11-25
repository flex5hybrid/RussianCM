"""Generates the sprites of the civilian vehicle tuning modules (Content.CMU/Resources/Prototypes/CMU14/Vehicles/Tuning).

Run from the repository root:  python -I Content.CMU/Design/Vehicles/TuningSprites/generate.py [preview.png]

Pure standard library. The palette is taken from the RMC14 vehicle hardpoint icons (humvee.rsi, van.rsi) so the
modules sit next to them. With a path argument it also writes an 8x preview sheet of every sprite.
"""
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import png  # noqa: E402

ROOT = os.getcwd()
TEXTURES = os.path.join(ROOT, "Content.CMU", "Resources", "Textures", "CMU14")
MODULES_RSI = os.path.join(TEXTURES, "Objects", "Vehicles", "tuning_modules.rsi")
MOLD_RSI = os.path.join(TEXTURES, "Objects", "Smelting", "armour_plate_shape.rsi")
WORKSHOP_RSI = os.path.join(TEXTURES, "Production", "Electric", "vehicle_workshop.rsi")
BLANK_SHAPE = os.path.join(TEXTURES, "Objects", "Smelting", "shapes.rsi", "blank_shape.png")
FABRICATOR = os.path.join(TEXTURES, "Production", "Electric", "fabricator.rsi")


def hex_color(value, alpha=255):
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha)


# Steel, from the Humvee hardpoints.
OUT = hex_color("1a2022")
S0 = hex_color("292f31")
S1 = hex_color("394040")
S2 = hex_color("575e5d")
S3 = hex_color("676b6a")
S4 = hex_color("7d8583")
S5 = hex_color("abadad")
# Rubber, from the van wheel.
R0 = hex_color("101111")
R1 = hex_color("222323")
R2 = hex_color("393b3a")
R3 = hex_color("4a4c4b")
# Weld beads and rust.
W1 = hex_color("615044")
W2 = hex_color("8a6a46")
W3 = hex_color("b08a52")
# Titanium.
T1 = hex_color("4a5866")
T2 = hex_color("66788a")
T3 = hex_color("8a9cad")
T4 = hex_color("b8c6d2")
# Lead.
L1 = hex_color("4b4f5e")
L2 = hex_color("646a7c")
# Gun furniture, heavy fuel.
F1 = hex_color("3d3226")
F2 = hex_color("5a4733")
H1 = hex_color("6e4f1c")
H2 = hex_color("a37a2c")


class Canvas:
    def __init__(self, width=32, height=32):
        self.width, self.height = width, height
        self.px = {}

    def set(self, x, y, color):
        if 0 <= x < self.width and 0 <= y < self.height:
            self.px[(x, y)] = color

    def get(self, x, y):
        return self.px.get((x, y))

    def rect(self, x0, y0, x1, y1, color):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.set(x, y, color)

    def hline(self, x0, x1, y, color):
        self.rect(x0, y, x1, y, color)

    def vline(self, x, y0, y1, color):
        self.rect(x, y0, x, y1, color)

    def disc(self, cx, cy, r, color, r_inner=-1.0):
        for y in range(self.height):
            for x in range(self.width):
                d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
                if r_inner < d <= r:
                    self.set(x, y, color)

    def plate(self, x0, y0, x1, y1, fill, light, shade, highlight=None):
        """A bevelled plate: lit top and left edges, shaded bottom and right."""
        self.rect(x0, y0, x1, y1, fill)
        self.hline(x0, x1, y0, light)
        self.vline(x0, y0, y1, light)
        self.hline(x0, x1, y1, shade)
        self.vline(x1, y0 + 1, y1, shade)
        if highlight:
            self.set(x0, y0, highlight)

    def bolt(self, x, y):
        self.set(x, y, S5)
        self.set(x, y + 1, S0)

    def weld(self, x0, y0, x1, y1):
        """A bead of weld along a straight run, alternating its two tones."""
        steps = max(abs(x1 - x0), abs(y1 - y0))
        for i in range(steps + 1):
            x = x0 + round((x1 - x0) * i / max(steps, 1))
            y = y0 + round((y1 - y0) * i / max(steps, 1))
            self.set(x, y, W3 if i % 2 == 0 else W1)

    def outline(self, color=OUT):
        """A one-pixel outline around everything drawn, like the RMC hardpoint icons."""
        edge = []
        for y in range(self.height):
            for x in range(self.width):
                if (x, y) in self.px:
                    continue
                if any((x + dx, y + dy) in self.px for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    edge.append((x, y))
        for x, y in edge:
            self.px[(x, y)] = color

    def rows(self):
        return [[self.px.get((x, y), (0, 0, 0, 0)) for x in range(self.width)] for y in range(self.height)]


def armour_light():
    c = Canvas()
    for x0, y0 in ((4, 8), (8, 15)):
        c.plate(x0, y0, x0 + 19, y0 + 5, T2, T3, T1, T4)
        c.hline(x0 + 1, x0 + 18, y0 + 2, T3)
        for bx in (x0 + 2, x0 + 17):
            c.set(bx, y0 + 3, T4)
    c.outline()
    return c


def armour():
    c = Canvas()
    for x0, y0 in ((4, 5), (7, 10), (10, 15)):
        c.plate(x0, y0, x0 + 17, y0 + 8, S2, S3, S1, S4)
        c.hline(x0 + 1, x0 + 16, y0 + 1, S3)
        for bx, by in ((x0 + 2, y0 + 2), (x0 + 15, y0 + 2), (x0 + 2, y0 + 5), (x0 + 15, y0 + 5)):
            c.bolt(bx, by)
    c.outline()
    return c


def armour_heavy():
    c = Canvas()
    c.plate(3, 7, 28, 25, S1, S2, S0, S3)
    c.plate(4, 8, 15, 16, S2, S3, S1, S4)
    c.plate(16, 8, 27, 16, S2, S3, S1, S4)
    c.plate(4, 17, 27, 24, S1, S2, S0)
    c.plate(17, 18, 24, 23, L1, L2, S0)
    c.weld(4, 16, 27, 16)
    c.weld(15, 8, 15, 16)
    c.weld(16, 17, 16, 24)
    for bx, by in ((6, 10), (13, 10), (18, 10), (25, 10), (6, 20), (12, 20)):
        c.bolt(bx, by)
    c.outline()
    return c


def wheel_armoured():
    c = Canvas()
    c.disc(16, 16, 13, R2)
    c.disc(16, 16, 13, R1, 11.5)
    for i in range(16):
        a = i * math.pi / 8
        c.set(round(16 + 12.3 * math.cos(a) - 0.5), round(16 + 12.3 * math.sin(a) - 0.5), R3)
    c.disc(16, 16, 7, S1)
    c.disc(16, 16, 6, S2)
    c.disc(16, 16, 3, S3)
    c.disc(16, 16, 1.5, S4)
    for i in range(5):
        a = i * 2 * math.pi / 5 - math.pi / 2
        c.set(round(16 + 4.6 * math.cos(a) - 0.5), round(16 + 4.6 * math.sin(a) - 0.5), S0)
    # Armour shoes welded over the tread.
    c.plate(12, 1, 19, 5, S2, S3, S1, S4)
    c.plate(12, 26, 19, 30, S2, S3, S1, S4)
    c.plate(1, 12, 5, 19, S2, S3, S1, S4)
    c.plate(26, 12, 30, 19, S2, S3, S1, S4)
    c.weld(12, 6, 19, 6)
    c.weld(12, 25, 19, 25)
    c.weld(6, 12, 6, 19)
    c.weld(25, 12, 25, 19)
    c.outline()
    return c


def bullbar_spiked():
    c = Canvas()
    # Spikes first, so the bars sit over their roots.
    for sx in (8, 13, 18, 23):
        for h in range(5):
            c.hline(sx - (4 - h) // 2, sx + (4 - h) // 2, 5 + h, S4 if h < 4 else S3)
        c.set(sx, 5, S5)
        c.set(sx, 4, S5)
    for y in (10, 21):
        c.hline(4, 27, y, S3)
        c.hline(4, 27, y + 1, S2)
        c.hline(4, 27, y + 2, S1)
    for x in (4, 26):
        c.rect(x, 8, x + 1, 26, S2)
        c.vline(x, 8, 26, S3)
    for x in (11, 20):
        c.rect(x, 13, x + 1, 20, S1)
        c.vline(x, 13, 20, S2)
    for sx in (8, 23):
        c.weld(sx - 1, 13, sx + 1, 13)
    c.weld(11, 13, 12, 13)
    c.weld(20, 13, 21, 13)
    c.outline()
    return c


def engine_heavy_fuel():
    c = Canvas()
    c.plate(5, 13, 24, 26, S1, S2, S0, S3)
    c.plate(7, 7, 22, 12, S2, S3, S1, S4)
    for x in range(8, 22, 2):
        c.vline(x, 8, 11, S4)
    # Exhaust stack and the belt pulley.
    c.rect(25, 4, 27, 22, S1)
    c.vline(25, 4, 22, S2)
    c.hline(25, 27, 4, S0)
    c.disc(9, 22, 3.2, R1)
    c.disc(9, 22, 1.6, S3)
    # Heavy fuel filler and its tag.
    c.rect(15, 16, 22, 20, H1)
    c.hline(15, 22, 16, H2)
    c.rect(19, 4, 21, 6, S0)
    c.hline(19, 21, 4, S3)
    for bx in (7, 22):
        c.bolt(bx, 14)
    c.outline()
    return c


def turret_homemade():
    c = Canvas()
    c.disc(16, 17, 12.5, S1)
    c.disc(16, 17, 10.5, S2)
    c.disc(16, 17, 12.5, S0, 11.6)
    # Scrap patches welded over the ring.
    c.plate(8, 14, 13, 19, S3, S4, S1)
    c.plate(18, 18, 24, 23, W1, W2, S0)
    c.weld(8, 20, 13, 20)
    # The gun cradle: two rails out to the front.
    for x in (13, 18):
        c.rect(x, 2, x + 1, 16, S3)
        c.vline(x, 2, 16, S4)
    c.hline(13, 19, 10, S1)
    c.hline(13, 19, 15, S1)
    for i in range(8):
        a = i * math.pi / 4
        c.set(round(16 + 11.3 * math.cos(a) - 0.5), round(17 + 11.3 * math.sin(a) - 0.5), S5)
    c.outline()
    return c


def gun_pkp():
    """A PKP light machine gun on a pivot mount: long barrel with its flash hider and bipod, the 200-round box on the
    receiver's side, the skeleton stock."""
    green1, green2 = hex_color("3b4230"), hex_color("525a41")
    c = Canvas()
    # Mount: base plate, pivot post and the yoke the gun sits in.
    c.plate(16, 27, 28, 29, S1, S2, S0)
    c.rect(22, 20, 23, 26, S2)
    c.vline(22, 20, 26, S3)
    c.hline(18, 26, 19, S2)
    c.vline(18, 16, 19, S2)
    c.vline(26, 16, 19, S2)
    # Barrel with the gas tube under it and the flash hider; the bipod folded under the barrel.
    c.hline(2, 15, 12, S3)
    c.hline(2, 15, 13, S1)
    c.hline(6, 15, 14, S2)
    c.rect(0, 11, 2, 14, S0)
    c.set(1, 12, S4)
    c.hline(7, 13, 15, S0)
    # Receiver and feed cover.
    c.plate(15, 10, 24, 15, S1, S2, S0, S3)
    c.hline(16, 23, 11, S0)
    # The skeleton stock and the pistol grip.
    c.hline(24, 31, 11, S1)
    c.hline(24, 31, 15, S1)
    c.vline(31, 11, 15, S1)
    c.rect(22, 16, 23, 18, F1)
    # The 200-round box hung under the receiver.
    c.plate(15, 16, 20, 21, green1, green2, S0)
    c.hline(16, 19, 18, green1)
    c.outline()
    return c


def mold_armour_plate():
    """The blank mold with a deep armour plate hollow and its bolt holes cut into it."""
    width, height, rows = png.read(BLANK_SHAPE)
    c = Canvas(width, height)
    for y, row in enumerate(rows):
        for x, color in enumerate(row):
            if color[3]:
                c.set(x, y, color)
    x0, y0, x1, y1 = 4, 4, width - 5, height - 5
    c.rect(x0, y0, x1, y1, S0)
    c.hline(x0, x1, y0, OUT)
    c.vline(x0, y0, y1, OUT)
    c.hline(x0 + 1, x1, y1, S2)
    c.vline(x1, y0 + 1, y1, S2)
    c.rect(x0 + 2, y0 + 2, x1 - 2, y1 - 2, S1)
    for bx, by in ((x0 + 2, y0 + 2), (x1 - 2, y0 + 2), (x0 + 2, y1 - 2), (x1 - 2, y1 - 2)):
        c.set(bx, by, OUT)
    return c


def workshop_states():
    """The complex assembly bench with a wheel-and-wrench plate on its front, off and working."""
    states = {}
    for state in ("off", "on"):
        width, height, rows = png.read(os.path.join(FABRICATOR, state + ".png"))
        frames = width // 32
        sheet = Canvas(width, height)
        for y, row in enumerate(rows):
            for x, color in enumerate(row):
                if color[3]:
                    sheet.set(x, y, color)
        for f in range(frames):
            ox = f * 32
            sheet.rect(ox + 6, 20, ox + 12, 26, OUT)
            sheet.rect(ox + 7, 21, ox + 11, 25, S2)
            for y in range(21, 26):
                for x in range(7, 12):
                    d = math.hypot(x + 0.5 - 9.5, y + 0.5 - 23.5)
                    if d <= 2.3:
                        sheet.set(ox + x, y, R1 if d > 1.0 else S4)
            sheet.set(ox + 11, 21, W3)
        states[state] = (sheet, frames)
    return states


MODULES = {
    "armour_light": armour_light,
    "armour": armour,
    "armour_heavy": armour_heavy,
    "wheel_armoured": wheel_armoured,
    "bullbar_spiked": bullbar_spiked,
    "engine_heavy_fuel": engine_heavy_fuel,
    "turret_homemade": turret_homemade,
    "gun_pkp": gun_pkp,
}

COPYRIGHT = ("Generated by Content.CMU/Design/Vehicles/TuningSprites/generate.py for CMU, "
             "palette after the RMC14 vehicle hardpoint icons")


def save_rsi(path, size, states, copyright_text):
    os.makedirs(path, exist_ok=True)
    meta_states = []
    for name, canvas, frames in states:
        png.write(os.path.join(path, name + ".png"), canvas.width, canvas.height, canvas.rows())
        entry = {"name": name}
        if frames > 1:
            entry["delays"] = [[0.25] * frames]
        meta_states.append(entry)
    meta = {"version": 1, "license": "CC-BY-SA-3.0", "copyright": copyright_text,
            "size": {"x": size[0], "y": size[1]}, "states": meta_states}
    with open(os.path.join(path, "meta.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(meta, f, indent=2)
        f.write("\n")


def preview(path, canvases, scale=8, pad=2):
    cell_w = max(c.width for c in canvases) * scale + pad * 2
    cell_h = max(c.height for c in canvases) * scale + pad * 2
    width, height = cell_w * len(canvases), cell_h
    rows = [[(200, 200, 200, 255) if ((x // 16 + y // 16) % 2) else (225, 225, 225, 255)
             for x in range(width)] for y in range(height)]
    for i, c in enumerate(canvases):
        for (x, y), color in c.px.items():
            for dy in range(scale):
                for dx in range(scale):
                    rows[pad + y * scale + dy][i * cell_w + pad + x * scale + dx] = color
    png.write(path, width, height, rows)


def main():
    modules = [(name, build(), 1) for name, build in MODULES.items()]
    save_rsi(MODULES_RSI, (32, 32), modules, COPYRIGHT)
    mold = mold_armour_plate()
    save_rsi(MOLD_RSI, (mold.width, mold.height), [("armour_plate_shape", mold, 1)],
             COPYRIGHT + "; mold blank from shapes.rsi (Created for Colonial Marines Universe)")
    workshop = workshop_states()
    save_rsi(WORKSHOP_RSI, (32, 32), [(name, canvas, frames) for name, (canvas, frames) in workshop.items()],
             "Complex assembly bench sprite from the sheet provided by the project author (placeholder), "
             "with a wheel plate added by Content.CMU/Design/Vehicles/TuningSprites/generate.py")
    if len(sys.argv) > 1:
        frames = [c for _, c, _ in modules] + [mold]
        for canvas, count in workshop.values():
            for f in range(count):
                crop = Canvas(32, 32)
                for (x, y), color in canvas.px.items():
                    if f * 32 <= x < f * 32 + 32:
                        crop.set(x - f * 32, y, color)
                frames.append(crop)
        preview(sys.argv[1], frames)


if __name__ == "__main__":
    main()

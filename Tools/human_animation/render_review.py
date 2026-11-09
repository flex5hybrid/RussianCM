"""Offline art study. Samples come from the production C# sampler; this is not a game capture.

python Tools/human_animation/render_review.py
Requires Pillow and numpy. Run ReviewExport first (see README).
"""
from pathlib import Path
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Tools/human_animation/generated"
TEXTURES = ROOT / "Resources/Textures"
FONT = Path("C:/Windows/Fonts/segoeui.ttf")
LABELS = {
    "Idle": ("Ожидание", "Спокойное дыхание"),
    "Walk": ("Ходьба", "Обычный шаг"),
    "Run": ("Бег", "Шире шаг, заметнее работа корпуса"),
    "HeavyWalk": ("Тяжёлая походка", "Короткий шаг под нагрузкой"),
    "CombatWalk": ("Боевой шаг", "Руки удерживают оружие"),
    "Limp": ("Хромота", "Неравномерный перенос веса"),
    "Crawl": ("Ползание", "Движение в лежачем положении"),
    "Melee": ("Удар", "Короткий вынос руки и корпуса"),
    "Shoot": ("Выстрел", "Отдача верхней части тела"),
    "Interact": ("Взаимодействие", "Общий цикл работы руками"),
    "Fall": ("Падение", "Сжатие корпуса при падении"),
    "GetUp": ("Подъём", "Выпрямление после подъёма"),
}


def frame(rsi, state, direction):
    sheet = Image.open(TEXTURES / rsi / (state + ".png")).convert("RGBA")
    columns = sheet.width // 32
    x, y = direction % columns * 32, direction // columns * 32
    return sheet.crop((x, y, x + 32, y + 32))


def person(direction, female=False, armored=False):
    body = Image.new("RGBA", (32, 32))
    for state in ["r_arm", "l_arm", "r_leg", "l_leg", "r_foot", "l_foot",
                  "torso_f" if female else "torso_m", "r_hand", "l_hand",
                  "head_f" if female else "head_m"]:
        part = np.array(frame("Mobs/Species/Human/parts.rsi", state, direction))
        part[:, :, :3] = part[:, :, :3] * np.array([0.84, 0.69, 0.57])
        body.alpha_composite(Image.fromarray(part))
    uniform = frame("_RMC14/Objects/Clothing/Uniforms/Marine/standard.rsi", "equipped-INNERCLOTHING", direction)
    if female:
        # The real shader runs AFTER the inventory's source-owned female displacement.
        displacement = np.array(frame("Mobs/Species/Human/displacement.rsi", "jumpsuit-female", direction))
        yy, xx = np.mgrid[:32, :32]
        sx = np.clip(xx + displacement[:, :, 0].astype(int) - 128, 0, 31)
        sy = np.clip(yy - displacement[:, :, 1].astype(int) + 128, 0, 31)
        data = np.array(uniform)[sy, sx].copy()
        data[:, :, 3] = data[:, :, 3].astype(float) * displacement[:, :, 3] / 255
        uniform = Image.fromarray(data)
    body.alpha_composite(uniform)
    if armored:
        body.alpha_composite(frame("_RMC14/Objects/Clothing/OuterClothing/Armor/m3/standard.rsi", "equipped-OUTERCLOTHING", direction))
        body.alpha_composite(frame("_RMC14/Objects/Clothing/Head/Helmets/m10/standard.rsi", "equipped-HELMET", direction))
    else:
        hair = TEXTURES / "Mobs/Customization/human_hair.rsi/ponytail.png"
        if hair.exists():
            hair_data = np.array(frame("Mobs/Customization/human_hair.rsi", "ponytail", direction))
            hair_data[:, :, :3] = hair_data[:, :, :3] * np.array([0.32, 0.20, 0.13])
            body.alpha_composite(Image.fromarray(hair_data))
    body.alpha_composite(frame("_RMC14/Objects/Clothing/Shoes/Boots/black.rsi", "equipped-FEET", direction))
    return body


def deform(body, sample, direction):
    """CPU reference of human_animation.swsl in a padded 40px render target."""
    source = Image.new("RGBA", (40, 40))
    source.alpha_composite(body, (4, 4))
    pixels = np.array(source)
    yy, xx = np.mgrid[:40, :40]
    px, py = xx - 4 + 0.5, yy - 4 + 0.5
    upper = np.clip((25 - py) / 15, 0, 1)
    leg = np.clip((py - 21) / 8, 0, 1)
    side = np.where(px < 16, -1, 1)
    facing = [0, 0, 1, -1][direction]
    profile = abs(facing)
    arm = (abs(px - 16) >= 6) * np.clip((py - 11) / 8, 0, 1) * (py <= 24)
    g, a = sample["gait"], sample["action"]
    dx = g[0] * side * leg * (0.25 + 0.75 * profile)
    dy = -np.maximum(0, g[0] * side) * leg * 0.45 - g[1] * leg * 0.25
    dx += facing * (g[3] - a[1]) * upper
    dy += (-g[2] - a[3] + a[2]) * upper
    dy += [1, -1, 0, 0][direction] * (g[3] - a[1]) * upper * 0.7
    dx += a[0] * arm * (side * (1 - profile) + facing * profile)
    dy -= a[0] * arm * (1 - profile) * 0.5
    sx, sy = xx - np.floor(dx + 0.5).astype(int), yy - np.floor(dy + 0.5).astype(int)
    inside = (sx >= 0) & (sx < 40) & (sy >= 0) & (sy < 40)
    result = pixels[np.clip(sy, 0, 39), np.clip(sx, 0, 39)].copy()
    result[~inside] = 0
    return Image.fromarray(result)


def main():
    clips = json.loads((OUT / "poses.json").read_text())
    people = [[person(d, female=row == 1, armored=row == 0) for d in range(4)] for row in range(2)]
    font = ImageFont.truetype(str(FONT), 19)
    small = ImageFont.truetype(str(FONT), 12)
    title = ImageFont.truetype(str(FONT), 26)
    frames = []
    for index in range(48):
        page = Image.new("RGB", (1328, 868), "#10161d")
        draw = ImageDraw.Draw(page)
        draw.text((24, 14), "CMU · Анимации людей · первый эскиз", font=title, fill="#e9f0f5")
        draw.text((24, 51), "Игровые спрайты · 4 направления · броня и форма · 12 кадров/с", font=small, fill="#8da6b5")
        for cell, clip in enumerate(clips):
            x, y = cell % 4 * 332 + 4, cell // 4 * 254 + 80
            draw.rounded_rectangle((x, y, x + 324, y + 244), 10, fill="#1b2631", outline="#304354")
            label, detail = LABELS[clip["name"]]
            draw.text((x + 12, y + 8), label, font=font, fill="#f0d2a4")
            draw.text((x + 12, y + 36), detail, font=small, fill="#9bb1c1")
            for row in range(2):
                for direction in range(4):
                    body = people[row][direction].copy()
                    if clip["name"] in ("CombatWalk", "Shoot"):
                        body.alpha_composite(frame("_RMC14/Objects/Weapons/Guns/Rifles/m54c2.rsi", "wielded-inhand-right", direction))
                    image = deform(body, clip["frames"][index], direction)
                    # Prone rotation is owned by the game's existing RotationVisualizer, not the new rig.
                    angle = 0
                    if clip["name"] == "Crawl":
                        angle = -90
                    elif clip["name"] == "Fall":
                        angle = -90 * clip["frames"][index]["progress"]
                    elif clip["name"] == "GetUp":
                        angle = -90 * (1 - clip["frames"][index]["progress"])
                    if angle:
                        image = image.rotate(angle, resample=Image.Resampling.NEAREST)
                    image = image.resize((80, 80), Image.Resampling.NEAREST)
                    page.paste(image, (x + 2 + direction * 80, y + 54 + row * 80), image)
            for direction, name in enumerate(["Юг", "Север", "Восток", "Запад"]):
                draw.text((x + 27 + direction * 80, y + 224), name, font=small, fill="#718d9f")
        draw.text((24, 847), "Офлайн-превью поз. Не запись из игры; совместимость всего снаряжения и производительность ещё требуют проверки.", font=small, fill="#8da6b5")
        frames.append(page)
    frames[5].save(OUT / "human-animations.png")
    frames[0].save(OUT / "human-animations.gif", save_all=True, append_images=frames[1:],
                   duration=[80, 80, 90] * 16, loop=0, optimize=True)
    print(f"Rendered 12 clips x 4 directions x 2 outfits x 48 frames to {OUT}")


if __name__ == "__main__":
    main()

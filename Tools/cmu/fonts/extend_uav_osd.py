"""Generate CMU OSD Cyrillic from the bundled UAV OSD Sans Mono.

Requires fonttools. Original glyphs and advances are retained. New outlines use
the original 585-unit stroke, 292-unit chamfers, 4096-unit cap and 3586 advance.
Run from any directory: python Tools/cmu/fonts/extend_uav_osd.py
"""

from copy import deepcopy
from pathlib import Path

from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont


ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Resources/Fonts/UAVOSD/UAV-OSD-Sans-Mono.ttf"
OUTPUT = ROOT / "Content.CMU/Resources/Fonts/UAVOSD/CMU-OSD-Cyrillic.ttf"


def generate():
    font = TTFont(SOURCE, recalcTimestamp=False)
    original = font.getBestCmap().copy()
    cmap = original.copy()
    glyf = font["glyf"]
    order = font.getGlyphOrder().copy()
    advance = font["hmtx"][original[ord("A")]][0]

    def contours(char):
        glyph = glyf[original[ord(char)]]
        points, ends, flags = glyph.getCoordinates(glyf)
        assert all(flag & 1 for flag in flags), "The source must have polygonal contours"
        result, start = [], 0
        for end in ends:
            result.append(list(points[start:end + 1]))
            start = end + 1
        return result

    def polygon(points, hole=False):
        points = list(points)
        area = sum(x * ny - nx * y for (x, y), (nx, ny) in zip(points, points[1:] + points[:1]))
        if (area > 0) != hole:
            points.reverse()
        return points

    def rect(left, bottom, right, top):
        return polygon([(left, bottom), (left, top), (right, top), (right, bottom)])

    def mirrored(paths, axis=2925):
        # Reverse winding after reflection, preserving source counters and overlapping strokes.
        return [[(axis - x, y) for x, y in reversed(path)] for path in paths]

    def reflected_y(paths):
        return [[(x, 4096 - y) for x, y in reversed(path)] for path in paths]

    def add(char, paths):
        codepoint = ord(char)
        if codepoint in original:
            return original[codepoint]
        name = f"uni{codepoint:04X}"
        pen = TTGlyphPen(None)
        for path in paths:
            pen.moveTo(path[0])
            for point in path[1:]:
                pen.lineTo(point)
            pen.closePath()
        glyph = pen.glyph()
        glyf[name] = glyph
        glyph.recalcBounds(glyf)
        # Inform rasterizers that joined strokes may overlap; all outer paths wind clockwise.
        if glyph.numberOfContours > 0:
            glyph.flags[0] |= 0x40
        font["hmtx"][name] = (advance, glyph.xMin)
        order.append(name)
        cmap[codepoint] = name
        return name

    # Matching Latin forms are reused exactly, including the source's angular corners.
    for russian, latin in zip("АВЕКМНОРСТХ", "ABEKMHOPCTX"):
        cmap[ord(russian)] = original[ord(latin)]
    cmap[ord("З")] = original[ord("3")]

    soft = reflected_y(contours("P"))
    add("Ь", soft)
    add("Б", deepcopy(soft) + [rect(0, 3510, 2925, 4096)])
    add("Г", [polygon([(0, 0), (0, 4096), (2925, 4096), (2925, 3510), (585, 3510), (585, 0)])])
    add("Д", [
        polygon([(0, -585), (0, 585), (292, 585), (877, 4096), (2340, 4096),
                 (2633, 585), (2925, 585), (2925, -585), (2340, -585),
                 (2340, 0), (585, 0), (585, -585)]),
        polygon([(877, 585), (2048, 585), (1755, 3510), (1170, 3510)], hole=True),
    ])
    add("Ж", contours("X") + [rect(1170, 0, 1755, 4096)])
    inverted_n = mirrored(contours("N"), axis=3217)
    add("И", inverted_n)
    # Diacritics fit inside the original cap/ascent. They cannot increase line height or clip menus.
    short_i = [[(x, round(y * 3218 / 4096)) for x, y in path] for path in inverted_n]
    breve = polygon([(585, 4096), (1170, 4096), (1170, 3803), (1755, 3803),
                     (1755, 4096), (2340, 4096), (2340, 3803), (2048, 3510),
                     (877, 3510), (585, 3803)])
    add("Й", short_i + [breve])
    add("Ё", [rect(0, 0, 585, 3218), rect(585, 0, 2925, 585),
              rect(585, 1462, 1755, 2048), rect(585, 2633, 2925, 3218),
              rect(585, 3510, 1170, 4096), rect(1755, 3510, 2340, 4096)])
    add("Л", [polygon([(0, 0), (0, 585), (292, 585), (877, 4096), (2048, 4096),
                       (2925, 0), (2340, 0), (1755, 3510), (1170, 3510), (877, 0)])])
    add("П", [polygon([(0, 0), (0, 4096), (2925, 4096), (2925, 0),
                       (2340, 0), (2340, 3510), (585, 3510), (585, 0)])])
    add("У", [polygon([(0, 4096), (585, 4096), (1462, 2340), (2340, 4096),
                       (2925, 4096), (2925, 3803), (1170, 292), (585, 0),
                       (0, 0), (0, 585), (585, 585), (1170, 1755), (0, 3803)])])
    add("Ф", [
        polygon([(0, 1170), (0, 2925), (585, 3510), (2340, 3510),
                 (2925, 2925), (2925, 1170), (2340, 585), (585, 585)]),
        polygon([(585, 1462), (877, 1170), (2048, 1170), (2340, 1462),
                 (2340, 2633), (2048, 2925), (877, 2925), (585, 2633)], hole=True),
        rect(1170, 0, 1755, 4096),
    ])
    add("Ц", [polygon([(0, 0), (0, 4096), (585, 4096), (585, 585), (2340, 585),
                       (2340, 4096), (2925, 4096), (2925, -585), (2340, -585), (2340, 0)])])
    add("Ч", [polygon([(2340, 0), (2340, 1755), (585, 1755), (0, 2340), (0, 4096),
                       (585, 4096), (585, 2633), (877, 2340), (2340, 2340),
                       (2340, 4096), (2925, 4096), (2925, 0)])])
    sha = [rect(0, 0, 585, 4096), rect(1170, 0, 1755, 4096),
           rect(2340, 0, 2925, 4096), rect(585, 0, 2340, 585)]
    add("Ш", sha)
    add("Щ", deepcopy(sha) + [rect(2340, -585, 2925, 0)])
    add("Ъ", [
        polygon([(585, 0), (585, 3510), (0, 3510), (0, 4096), (1170, 4096),
                 (1170, 2340), (2340, 2340), (2925, 1755), (2925, 585), (2340, 0)]),
        polygon([(1170, 585), (2048, 585), (2340, 877), (2340, 1462),
                 (2048, 1755), (1170, 1755)], hole=True),
    ])
    add("Ы", [
        polygon([(0, 0), (0, 4096), (585, 4096), (585, 2340), (1462, 2340),
                 (2048, 1755), (2048, 585), (1462, 0)]),
        polygon([(585, 585), (1170, 585), (1462, 877), (1462, 1462),
                 (1170, 1755), (585, 1755)], hole=True),
        rect(2340, 0, 2925, 4096),
    ])
    add("Э", mirrored(contours("C")) + [rect(1170, 1755, 2925, 2340)])
    add("Ю", [
        rect(0, 0, 585, 4096), rect(585, 1755, 1170, 2340),
        polygon([(1170, 585), (1170, 3510), (1755, 4096), (2340, 4096),
                 (2925, 3510), (2925, 585), (2340, 0), (1755, 0)]),
        polygon([(2048, 585), (2340, 877), (2340, 3218), (2048, 3510),
                 (1755, 3218), (1755, 877)], hole=True),
    ])
    add("Я", mirrored(contours("R")))

    # The source maps Latin lower case to capitals. Cyrillic follows the same intentional OSD style.
    for codepoint in range(0x410, 0x430):
        cmap[codepoint + 0x20] = cmap[codepoint]
    cmap[ord("ё")] = cmap[ord("Ё")]
    cmap.setdefault(0xA0, original[ord(" ")])
    chevrons = [polygon([(1170, 585), (0, 1755), (0, 2340), (1170, 3510),
                         (1755, 3510), (1755, 3218), (585, 2048), (1755, 877), (1755, 585)])]
    chevrons.append([(x + 1170, y) for x, y in chevrons[0]])
    add("«", chevrons)
    add("»", mirrored(chevrons))
    for char in "–—":
        add(char, [rect(0, 1755, 2925, 2340)])
    add("…", [rect(x, 0, x + 585, 585) for x in (0, 1170, 2340)])
    for char, source in [("‘", "'"), ("’", "'"), ("“", '"'), ("”", '"')]:
        cmap.setdefault(ord(char), original[ord(source)])
    small_n = [[(round(x * 0.52), y) for x, y in path] for path in contours("N")]
    add("№", small_n + [rect(2048, 1462, 3218, 1755),
        polygon([(2048, 2340), (2048, 3218), (2340, 3510), (2925, 3510),
                 (3218, 3218), (3218, 2340), (2925, 2048), (2340, 2048)]),
        polygon([(2340, 2340), (2925, 2340), (2925, 3218), (2340, 3218)], hole=True)])

    font.setGlyphOrder(order)
    for table in font["cmap"].tables:
        if table.isUnicode():
            table.cmap.update(cmap)
    font["maxp"].numGlyphs = len(order)
    font["OS/2"].recalcUnicodeRanges(font)
    # Advertise Cyrillic (Windows-1251) without changing original line metrics or font sizing.
    font["OS/2"].ulCodePageRange1 |= 1 << 2
    font["OS/2"].usLastCharIndex = max(cmap)
    names = {
        1: "CMU OSD Cyrillic", 2: "Regular", 3: "CMU OSD Cyrillic 1.001",
        4: "CMU OSD Cyrillic", 5: "Version 1.001", 6: "CMU-OSD-Cyrillic",
        16: "CMU OSD Cyrillic", 17: "Regular",
        10: "UAV OSD Sans Mono by Nicholas Kruse; Cyrillic extension by CMU contributors.",
    }
    for name_id, value in names.items():
        for platform, encoding, language in [(3, 1, 0x409), (1, 0, 0)]:
            font["name"].setName(value, name_id, platform, encoding, language)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    font.save(OUTPUT)
    print(f"Wrote {OUTPUT.relative_to(ROOT)}: {len(cmap)} characters, {len(order)} glyphs")


if __name__ == "__main__":
    generate()

from PIL import Image

name = "hunter_tile_1"
path = "Resources/Textures/_RuMC14/Tiles/hunter/hunter_floors.rsi"
img = Image.open(f"{path}/{name}.png")
tiles = []

for y in range(0, 64, 32):
    for x in range(0, 64, 32):
        tiles.append(img.crop((x, y, x+32, y+32)))

out = Image.new("RGBA", (32 * len(tiles), 32))

for i, tile in enumerate(tiles):
    out.paste(tile, (i * 32, 0))

out.save(f"{path}/{name}.png")

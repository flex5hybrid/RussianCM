from PIL import Image, ImageChops, ImageFilter
from collections import deque
import pathlib, json, sys
root = pathlib.Path(sys.argv[1])
original = Image.open(root / 'quality-0.png').convert('RGB')
width, height = original.size
raw = original.tobytes()
black = bytearray(1 if raw[i] == raw[i+1] == raw[i+2] == 0 else 0 for i in range(0,len(raw),3))
connected = bytearray(width*height)
queue = deque()
for x in range(width):
    queue.extend((x,(height-1)*width+x))
for y in range(height):
    queue.extend((y*width,y*width+width-1))
while queue:
    index = queue.popleft()
    if connected[index] or not black[index]: continue
    connected[index] = 255
    x, y = index % width, index // width
    if x: queue.append(index-1)
    if x < width-1: queue.append(index+1)
    if y: queue.append(index-width)
    if y < height-1: queue.append(index+width)
mask = Image.frombytes('L',(width,height),bytes(connected)).filter(ImageFilter.MinFilter(7))
rows=[]
for quality in [2,3]:
    current=Image.open(root / ('quality-{}.png'.format(quality))).convert('RGB')
    if current.size != original.size: raise RuntimeError('Viewport dimensions changed')
    changed = ImageChops.difference(original,current).point(lambda p: 255 if p else 0).convert('L').point(lambda p:255 if p else 0)
    leaked=ImageChops.multiply(changed,mask)
    points=[(x,y,current.getpixel((x,y))) for y in range(height) for x in range(width) if leaked.getpixel((x,y))]
    if points:
        print('changed black bounds/sample:', leaked.getbbox(), points[:8], file=sys.stderr)
    rows.append({'quality':quality,'width':width,'height':height,'changed_pixels':sum(changed.histogram()[1:]),'deep_exterior_black_pixels':mask.histogram()[255],'changed_deep_exterior_pixels':sum(leaked.histogram()[1:])})
print(json.dumps(rows,indent=2))

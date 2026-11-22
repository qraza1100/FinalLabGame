"""Generates the game's 2D textures (glyphs, UI sprites, FX) with Pillow.
Usage: python make_textures.py <output_dir>"""
import sys, os, math
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
W = (255, 255, 255, 255)

def save(img, name):
    img.save(os.path.join(OUT, name)); print("wrote", name)

def ss(size, scale=4):
    return Image.new("RGBA", (size[0] * scale, size[1] * scale), (0, 0, 0, 0)), scale

def down(img, size):
    return img.resize(size, Image.LANCZOS)

def glow(img, radius=6, strength=0.55):
    a = img.split()[3].filter(ImageFilter.GaussianBlur(radius))
    a = a.point(lambda v: int(v * strength))
    base = Image.new("RGBA", img.size, (255, 255, 255, 0))
    base.putalpha(a)
    return Image.alpha_composite(base, img)

# ---- glyphs (white, used as emissive masks and UI icons) 256x256
def glyph(kind):
    img, s = ss((256, 256)); d = ImageDraw.Draw(img); c = 128 * s
    if kind == "moon":
        r = 78 * s
        d.ellipse([c - r, c - r, c + r, c + r], fill=W)
        r2 = 64 * s; ox, oy = c + 38 * s, c - 26 * s
        d.ellipse([ox - r2, oy - r2, ox + r2, oy + r2], fill=(0, 0, 0, 0))
    elif kind == "sun":
        r = 40 * s
        d.ellipse([c - r, c - r, c + r, c + r], fill=W)
        for k in range(8):
            a = k * math.pi / 4
            x1, y1 = c + 62 * s * math.cos(a), c + 62 * s * math.sin(a)
            x2, y2 = c + 96 * s * math.cos(a), c + 96 * s * math.sin(a)
            d.line([x1, y1, x2, y2], fill=W, width=16 * s)
            d.ellipse([x2 - 8 * s, y2 - 8 * s, x2 + 8 * s, y2 + 8 * s], fill=W)
    elif kind == "star":
        pts = []
        for k in range(10):
            a = -math.pi / 2 + k * math.pi / 5
            rr = (96 if k % 2 == 0 else 40) * s
            pts.append((c + rr * math.cos(a), c + 8 * s + rr * math.sin(a)))
        d.polygon(pts, fill=W)
    elif kind == "heart":
        pts = []
        for i in range(120):
            t = 2 * math.pi * i / 120
            x = 16 * math.sin(t) ** 3
            y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
            pts.append((c + x * 5.6 * s, c - (y + 2) * 5.6 * s))
        d.polygon(pts, fill=W)
    elif kind == "pearl":
        r = 70 * s
        d.ellipse([c - r, c - r, c + r, c + r], fill=W)
    img = down(img, (256, 256))
    return glow(img, 8, 0.5)

for k in ("moon", "sun", "star", "heart", "pearl"):
    save(glyph(k), f"glyph_{k}.png")

# ---- UI sprites (white; tinted by Image.color)
def rounded(size, radius, outline=0):
    img, s = ss(size); d = ImageDraw.Draw(img)
    box = [0, 0, size[0] * s - 1, size[1] * s - 1]
    if outline:
        d.rounded_rectangle(box, radius * s, outline=W, width=outline * s)
    else:
        d.rounded_rectangle(box, radius * s, fill=W)
    return down(img, size)

save(rounded((96, 96), 22), "ui_panel.png")
save(rounded((96, 96), 22, outline=3), "ui_panel_outline.png")
save(rounded((64, 64), 31), "ui_pill.png")
save(rounded((32, 32), 7), "ui_bar.png")
img, s = ss((64, 64)); d = ImageDraw.Draw(img); d.ellipse([0, 0, 64 * s - 1, 64 * s - 1], fill=W); save(down(img, (64, 64)), "ui_circle.png")
# diamond bullet
img, s = ss((64, 64)); d = ImageDraw.Draw(img); d.polygon([(32 * s, 2 * s), (62 * s, 32 * s), (32 * s, 62 * s), (2 * s, 32 * s)], fill=W); save(down(img, (64, 64)), "ui_diamond.png")
# vignette (alpha grows to edges)
vg = Image.new("RGBA", (256, 256), (255, 255, 255, 0)); px = vg.load()
for y in range(256):
    for x in range(256):
        dx, dy = (x - 127.5) / 127.5, (y - 127.5) / 127.5
        r = math.sqrt(dx * dx + dy * dy)
        a = max(0.0, min(1.0, (r - 0.45) / 0.65)) ** 1.6
        px[x, y] = (255, 255, 255, int(255 * a))
save(vg, "ui_vignette.png")

# ---- FX
dot = Image.new("RGBA", (64, 64), (255, 255, 255, 0)); px = dot.load()
for y in range(64):
    for x in range(64):
        r = math.hypot(x - 31.5, y - 31.5) / 31.5
        px[x, y] = (255, 255, 255, int(255 * max(0, 1 - r) ** 2))
save(dot, "fx_dot.png")
img, s = ss((256, 256)); d = ImageDraw.Draw(img)
d.ellipse([20 * s, 20 * s, 236 * s, 236 * s], outline=W, width=10 * s)
save(glow(down(img, (256, 256)), 6, 0.7), "fx_ring.png")
# arrow pointing to +V (top of image = forward)
img, s = ss((128, 256)); d = ImageDraw.Draw(img)
d.polygon([(64 * s, 8 * s), (120 * s, 100 * s), (82 * s, 100 * s), (82 * s, 240 * s), (46 * s, 240 * s), (46 * s, 100 * s), (8 * s, 100 * s)], fill=W)
save(glow(down(img, (128, 256)), 6, 0.6), "fx_arrow.png")
# chevrons (tile vertically, pointing to +V)
img, s = ss((128, 128)); d = ImageDraw.Draw(img)
for k in range(2):
    y0 = (16 + k * 64) * s
    d.line([(20 * s, y0 + 36 * s), (64 * s, y0), (108 * s, y0 + 36 * s)], fill=W, width=12 * s, joint="curve")
save(down(img, (128, 128)), "fx_chevrons.png")
# cracks
cr = Image.new("RGBA", (256, 256), (0, 0, 0, 0)); d = ImageDraw.Draw(cr)
import random
random.seed(4)
for _ in range(7):
    x, y = 128, 128
    a = random.uniform(0, 2 * math.pi)
    for _ in range(9):
        a += random.uniform(-0.7, 0.7)
        nx, ny = x + 16 * math.cos(a), y + 16 * math.sin(a)
        d.line([x, y, nx, ny], fill=W, width=4)
        x, y = nx, ny
save(glow(cr, 4, 0.8), "fx_cracks.png")

# left-to-right fade (menu readability overlay)
fade = Image.new("RGBA", (512, 16), (255, 255, 255, 0)); px = fade.load()
for x in range(512):
    a = int(255 * max(0.0, 1.0 - x / 511.0) ** 1.3)
    for y in range(16):
        px[x, y] = (255, 255, 255, a)
save(fade, "ui_fade_left.png")

# pill outline (transparent centre) for 9-sliced outlines
save(rounded((64, 64), 31, outline=3), "ui_pill_outline.png")

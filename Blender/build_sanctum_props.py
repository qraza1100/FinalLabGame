"""
The Drowned Sanctum - custom props, modelled procedurally in Blender.

Run from a terminal:
  blender -b -P build_sanctum_props.py -- <fbx_output_dir> <blend_output_path>

Creates: Sub (Hull + Lamp + Propeller), HeartOfEternity, SigilTablet, Pearl,
Urchin, AirVent, Seaweed, Coral, Altar. Each is exported to its own FBX for
Unity (1 unit = 1 metre, Y up) and all are saved together in one .blend file.
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
FBX_DIR = argv[0] if len(argv) > 0 else os.path.join(os.path.dirname(__file__), "fbx")
BLEND_PATH = argv[1] if len(argv) > 1 else os.path.join(os.path.dirname(__file__), "SanctumProps.blend")
os.makedirs(FBX_DIR, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
coll = scene.collection

# ---------------------------------------------------------------- helpers
MATS = {}
def mat(name, col, emit=None, metal=0.0, rough=0.5):
    if name in MATS:
        return MATS[name]
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    m.diffuse_color = (col[0], col[1], col[2], 1.0)
    nt = getattr(m, "node_tree", None)
    if nt:
        p = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if p:
            def setin(key, val):
                if key in p.inputs:
                    p.inputs[key].default_value = val
            setin('Base Color', (col[0], col[1], col[2], 1.0))
            setin('Metallic', metal)
            setin('Roughness', rough)
            if emit:
                setin('Emission Color', (emit[0], emit[1], emit[2], 1.0))
                setin('Emission Strength', 2.0)
    MATS[name] = m
    return m

def deselect():
    for o in bpy.context.view_layer.objects:
        o.select_set(False)

def activate(o):
    deselect()
    o.select_set(True)
    bpy.context.view_layer.objects.active = o

def prim(kind, name, material, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), smooth=False, **kw):
    getattr(bpy.ops.mesh, "primitive_" + kind + "_add")(location=loc, rotation=rot, **kw)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    activate(o)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    o.data.materials.append(material)
    if smooth:
        bpy.ops.object.shade_smooth()
    return o

def bevel(o, width=0.04, segments=2):
    activate(o)
    md = o.modifiers.new("Bevel", 'BEVEL')
    md.width = width
    md.segments = segments
    bpy.ops.object.modifier_apply(modifier=md.name)

def join(objs, name):
    deselect()
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    o.data.name = name
    return o

def mesh_from_bmesh(bm, name, material):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    me.materials.append(material)
    return o

def export(root, filename):
    deselect()
    root.select_set(True)
    for c in root.children_recursive:
        c.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(FBX_DIR, filename),
        use_selection=True,
        object_types={'MESH', 'EMPTY'},
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,
        axis_forward='-Z', axis_up='Y',
        mesh_smooth_type='FACE',
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False)
    print("EXPORTED", filename)

# ---------------------------------------------------------------- materials
BRASS = mat("Brass", (0.90, 0.55, 0.15), metal=0.6, rough=0.35)
BRASS_DK = mat("BrassDark", (0.45, 0.25, 0.06), metal=0.5, rough=0.45)
GLASS = mat("Glass", (0.20, 0.95, 0.90), emit=(0.2, 0.95, 0.9))
LAMP = mat("Lamp", (1.0, 0.95, 0.75), emit=(1.0, 0.95, 0.75))
GEM = mat("HeartGem", (1.0, 0.62, 0.12), emit=(1.0, 0.55, 0.1), rough=0.15)
STONE = mat("Stone", (0.42, 0.46, 0.40), rough=0.85)
PEARL = mat("Pearl", (0.95, 0.93, 0.88), rough=0.2)
URCHIN = mat("UrchinBody", (0.30, 0.08, 0.22), rough=0.6)
SPIKE = mat("UrchinSpike", (0.18, 0.05, 0.14), rough=0.5)
VENTGLOW = mat("VentGlow", (0.20, 0.95, 0.90), emit=(0.2, 0.95, 0.9))
DARK = mat("Dark", (0.05, 0.06, 0.07), rough=0.7)
WEED = mat("Seaweed", (0.10, 0.50, 0.35), rough=0.7)
CORAL = mat("Coral", (0.95, 0.40, 0.35), rough=0.7)
GLYPH = mat("Glyph", (0.20, 0.95, 0.90), emit=(0.2, 0.95, 0.9))

roots = []

# ---------------------------------------------------------------- 1. Explorer sub (nose +Y)
body = prim("uv_sphere", "Body", BRASS, scale=(0.42, 0.9, 0.40), segments=24, ring_count=12, smooth=True)
tower = prim("cylinder", "Tower", BRASS_DK, loc=(0, 0.12, 0.40), scale=(0.2, 0.32, 0.18), vertices=16)
bevel(tower, 0.03, 2)
scope = prim("cylinder", "Scope", BRASS_DK, loc=(0, 0.18, 0.66), scale=(0.025, 0.025, 0.12), vertices=8)
ports = []
for i, (y, r) in enumerate([(0.28, 0.11), (-0.02, 0.085), (-0.30, 0.065)]):
    for sx in (1, -1):
        rim = prim("cylinder", "PortRim%d%d" % (i, sx), BRASS_DK, loc=(sx * 0.36, y, 0.06), rot=(0, math.radians(90), 0),
                   scale=(r + 0.025, r + 0.025, 0.05), vertices=16)
        gl = prim("cylinder", "Port%d%d" % (i, sx), GLASS, loc=(sx * 0.385, y, 0.06), rot=(0, math.radians(90), 0),
                  scale=(r, r, 0.04), vertices=16)
        ports += [rim, gl]
vfin = prim("cube", "VFin", BRASS_DK, loc=(0, -0.78, 0.2), scale=(0.025, 0.16, 0.16))
hfin = prim("cube", "HFin", BRASS_DK, loc=(0, -0.78, 0.0), scale=(0.34, 0.12, 0.022))
sfinL = prim("cube", "SideFinL", BRASS_DK, loc=(0.44, 0.15, -0.16), rot=(0, math.radians(-20), 0), scale=(0.12, 0.16, 0.02))
sfinR = prim("cube", "SideFinR", BRASS_DK, loc=(-0.44, 0.15, -0.16), rot=(0, math.radians(20), 0), scale=(0.12, 0.16, 0.02))
lampHousing = prim("cylinder", "LampHousing", BRASS_DK, loc=(0, 0.86, -0.06), rot=(math.radians(90), 0, 0), scale=(0.12, 0.12, 0.08), vertices=16)
hull = join([body, tower, scope, vfin, hfin, sfinL, sfinR, lampHousing] + ports, "Hull")

lamp = prim("cylinder", "Lamp", LAMP, loc=(0, 0.93, -0.06), rot=(math.radians(90), 0, 0), scale=(0.09, 0.09, 0.03), vertices=16)
hub = prim("cylinder", "Propeller", BRASS_DK, loc=(0, -0.98, 0), rot=(math.radians(90), 0, 0), scale=(0.06, 0.06, 0.08), vertices=12)
blades = []
for k in range(3):
    a = k * 2 * math.pi / 3
    b = prim("cube", "Blade%d" % k, BRASS, loc=(0.13 * math.cos(a), -0.98, 0.13 * math.sin(a)),
             rot=(0, -a, math.radians(25)), scale=(0.12, 0.012, 0.045))
    blades.append(b)
prop = join([hub] + blades, "Propeller")
lamp.parent = hull
prop.parent = hull
roots.append((hull, "Sub.fbx"))

# ---------------------------------------------------------------- 2. Heart of Eternity (faceted crystal, faces -Y)
bm = bmesh.new()
N, d = 36, 0.09
pts = []
for i in range(N):
    t = 2 * math.pi * i / N
    x = 16 * math.sin(t) ** 3
    z = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
    pts.append((x / 38.0, (z + 3) / 38.0))
front = [bm.verts.new((x, -d, z)) for x, z in pts]
back = [bm.verts.new((x, d, z)) for x, z in pts]
cf = bm.verts.new((0, -d * 2.6, -0.02))
cb = bm.verts.new((0, d * 2.6, -0.02))
for i in range(N):
    j = (i + 1) % N
    bm.faces.new((front[j], front[i], cf))
    bm.faces.new((back[i], back[j], cb))
    bm.faces.new((front[i], front[j], back[j], back[i]))
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
heart = mesh_from_bmesh(bm, "HeartOfEternity", GEM)
roots.append((heart, "HeartOfEternity.fbx"))

# ---------------------------------------------------------------- 3. Sigil tablet (front -Y)
tab = prim("cube", "SigilTablet", STONE, scale=(0.42, 0.10, 0.56))
bevel(tab, 0.07, 3)
rim = prim("cube", "TabletInset", DARK, loc=(0, -0.085, 0), scale=(0.32, 0.03, 0.44))
bevel(rim, 0.03, 2)
tab = join([tab, rim], "SigilTablet")
roots.append((tab, "SigilTablet.fbx"))

# ---------------------------------------------------------------- 4. Pearl
pearl = prim("uv_sphere", "Pearl", PEARL, radius=0.18, segments=20, ring_count=12, smooth=True)
roots.append((pearl, "Pearl.fbx"))

# ---------------------------------------------------------------- 5. Urchin
core = prim("ico_sphere", "Urchin", URCHIN, radius=0.32, subdivisions=2, smooth=True)
spikes = []
dirs = [v.co.normalized() for v in core.data.vertices]
for i, n in enumerate(dirs):
    q = n.to_track_quat('Z', 'Y')
    c = prim("cone", "Spike%d" % i, SPIKE, loc=tuple(n * 0.42), rot=tuple(q.to_euler()),
             radius1=0.045, radius2=0.0, depth=0.38, vertices=6)
    spikes.append(c)
urchin = join([core] + spikes, "Urchin")
roots.append((urchin, "Urchin.fbx"))

# ---------------------------------------------------------------- 6. Air vent
ring = prim("torus", "AirVent", STONE, major_radius=0.62, minor_radius=0.16, major_segments=24, minor_segments=8, smooth=True)
base = prim("cylinder", "VentBase", STONE, loc=(0, 0, -0.08), scale=(0.66, 0.66, 0.06), vertices=24)
glow = prim("cylinder", "VentGlowDisc", VENTGLOW, loc=(0, 0, 0.0), scale=(0.5, 0.5, 0.03), vertices=24)
bars = [prim("cube", "Grate%d" % k, DARK, loc=(0, 0, 0.05), rot=(0, 0, math.radians(k * 45)), scale=(0.5, 0.025, 0.025)) for k in range(4)]
vent = join([ring, base, glow] + bars, "AirVent")
roots.append((vent, "AirVent.fbx"))

# ---------------------------------------------------------------- 7. Seaweed (3 swaying blades)
bm = bmesh.new()
for b in range(3):
    ang = b * 2 * math.pi / 3 + 0.3
    tx, ty = math.cos(ang), math.sin(ang)
    nx, ny = -ty, tx
    prev = None
    H, SEG = 1.7 - b * 0.25, 10
    for i in range(SEG + 1):
        z = H * i / SEG
        sway = 0.14 * math.sin(i * 0.8 + b)
        w = 0.11 * (1.0 - i / (SEG + 2))
        cx, cy = 0.12 * tx + sway * nx, 0.12 * ty + sway * ny
        v1 = bm.verts.new((cx - w * tx, cy - w * ty, z))
        v2 = bm.verts.new((cx + w * tx, cy + w * ty, z))
        if prev:
            bm.faces.new((prev[0], prev[1], v2, v1))
        prev = (v1, v2)
weed = mesh_from_bmesh(bm, "Seaweed", WEED)
roots.append((weed, "Seaweed.fbx"))

# ---------------------------------------------------------------- 8. Coral
parts = [prim("cylinder", "Coral", CORAL, loc=(0, 0, 0.22), scale=(0.06, 0.06, 0.22), vertices=8)]
for k, (ax, ay, h) in enumerate([(35, 0, 0.32), (-30, 20, 0.28), (10, -40, 0.30), (-15, 45, 0.24), (40, 60, 0.2)]):
    rx, ry = math.radians(ax), math.radians(ay)
    dirv = Vector((math.sin(ry), -math.sin(rx), math.cos(rx) * math.cos(ry))).normalized()
    start = Vector((0, 0, 0.30 + 0.04 * k))
    mid = start + dirv * (h / 2)
    q = dirv.to_track_quat('Z', 'Y')
    parts.append(prim("cylinder", "Branch%d" % k, CORAL, loc=tuple(mid), rot=tuple(q.to_euler()), scale=(0.04, 0.04, h / 2), vertices=8))
    parts.append(prim("uv_sphere", "Tip%d" % k, CORAL, loc=tuple(start + dirv * h), radius=0.06, segments=8, ring_count=6, smooth=True))
coral = join(parts, "Coral")
roots.append((coral, "Coral.fbx"))

# ---------------------------------------------------------------- 9. Altar (3 tiers + glowing glyph band)
t1 = prim("cube", "Altar", STONE, loc=(0, 0, 0.15), scale=(0.9, 0.9, 0.15)); bevel(t1, 0.04)
t2 = prim("cube", "Tier2", STONE, loc=(0, 0, 0.42), scale=(0.68, 0.68, 0.12)); bevel(t2, 0.04)
t3 = prim("cube", "Tier3", STONE, loc=(0, 0, 0.64), scale=(0.46, 0.46, 0.10)); bevel(t3, 0.03)
band = prim("cube", "GlyphBand", GLYPH, loc=(0, 0, 0.42), scale=(0.70, 0.70, 0.025))
altar = join([t1, t2, t3, band], "Altar")
roots.append((altar, "Altar.fbx"))

# ---------------------------------------------------------------- export + lay out for the .blend
for i, (root, fname) in enumerate(roots):
    export(root, fname)
for i, (root, fname) in enumerate(roots):
    root.location = ((i % 5) * 2.5, (i // 5) * 2.5, 0)

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
print("SAVED", BLEND_PATH)

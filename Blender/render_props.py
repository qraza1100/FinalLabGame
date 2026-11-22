"""Renders a labelled overview of the custom props in SanctumProps.blend.
blender -b SanctumProps.blend -P render_props.py -- <output.png>"""
import bpy, sys, math
from mathutils import Vector
out = sys.argv[sys.argv.index("--") + 1]
scene = bpy.context.scene
labels = {"Hull": "Explorer Sub", "HeartOfEternity": "Heart of Eternity", "SigilTablet": "Sigil Tablet", "Pearl": "Pearl",
          "Urchin": "Urchin", "AirVent": "Air Vent", "Seaweed": "Seaweed", "Coral": "Coral", "Altar": "Altar"}
white = bpy.data.materials.new("LabelWhite"); white.diffuse_color = (0.85, 0.95, 0.95, 1)
for name, text in labels.items():
    o = bpy.data.objects.get(name)
    if not o: continue
    bpy.ops.object.text_add(location=(o.location.x, o.location.y - 0.9, 0.02), rotation=(math.radians(55), 0, 0))
    t = bpy.context.active_object; t.data.body = text; t.data.size = 0.26; t.data.align_x = 'CENTER'
    t.data.materials.append(white)
# ground plane
bpy.ops.mesh.primitive_plane_add(size=40, location=(5, 1.25, -0.02))
g = bpy.context.active_object; gm = bpy.data.materials.new("Ground"); gm.diffuse_color = (0.05, 0.13, 0.16, 1); g.data.materials.append(gm)
cam_data = bpy.data.cameras.new("Cam"); cam_data.lens = 30
cam = bpy.data.objects.new("Cam", cam_data); scene.collection.objects.link(cam)
cam.location = (5.0, -10.0, 8.0)
direction = Vector((5.0, 1.3, 0.2)) - cam.location
cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
scene.camera = cam
w = bpy.data.worlds.new("W"); w.color = (0.02, 0.06, 0.09); scene.world = w
scene.render.engine = 'BLENDER_WORKBENCH'
sh = scene.display.shading
sh.light = 'STUDIO'; sh.color_type = 'MATERIAL'; sh.show_cavity = True; sh.show_shadows = True
try: sh.cavity_type = 'BOTH'
except Exception: pass
scene.render.resolution_x = 1800; scene.render.resolution_y = 1000; scene.render.resolution_percentage = 100
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print("RENDERED", out)

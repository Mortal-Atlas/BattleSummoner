"""
Render 6 High-Resolution Beauty Showcases of CircularContinent_MacroLevel1
"""
import bpy
import mathutils
import math
import os

project_root = os.getcwd()
blend_file = os.path.join(project_root, "Assets", "Models", "Environments", "CircularContinent_MacroLevel1.blend")
output_dir = os.path.join(project_root, "Assets", "Screenshots")
os.makedirs(output_dir, exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=blend_file)
scene = bpy.context.scene

# Configure Render Settings
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1920
scene.render.resolution_y = 1080
scene.render.film_transparent = False

# Bright atmospheric sky
if not scene.world:
    scene.world = bpy.data.worlds.new("World")
scene.world.use_nodes = True
bg_node = scene.world.node_tree.nodes.get("Background")
if bg_node:
    bg_node.inputs['Color'].default_value = (0.58, 0.76, 0.94, 1.0) # Sky blue
    bg_node.inputs['Strength'].default_value = 1.1

def orient_camera_look_at(cam_obj, target_pos):
    direction = target_pos - cam_obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam_obj.rotation_euler = rot_quat.to_euler()

# 6 Dedicated Camera Setups: (Name, Cam_Pos, Target_Pos, Lens)
shots = [
    (
        "continent_macro_overview.png",
        mathutils.Vector((0.0, -820.0, 680.0)),
        mathutils.Vector((0.0, 30.0, 15.0)),
        38.0
    ),
    (
        "continent_citadel_closeup.png",
        mathutils.Vector((0.0, -210.0, 140.0)),
        mathutils.Vector((0.0, 0.0, 32.0)),
        45.0
    ),
    (
        "continent_volcano_boss_arena.png",
        mathutils.Vector((-260.0, 160.0, 160.0)),
        mathutils.Vector((-260.0, 270.0, 85.0)),
        32.0
    ),
    (
        "continent_dockcity_offshore_island.png",
        mathutils.Vector((160.0, -310.0, 95.0)),
        mathutils.Vector((310.0, -180.0, 12.0)),
        42.0
    ),
    (
        "continent_oasis_desolation.png",
        mathutils.Vector((-230.0, -130.0, 38.0)),
        mathutils.Vector((-300.0, -70.0, 5.0)),
        40.0
    ),
    (
        "continent_mini_boss_arena.png",
        mathutils.Vector((145.0, 295.0, 52.0)),
        mathutils.Vector((180.0, 330.0, 42.0)),
        42.0
    )
]

# Create camera
cam_data = bpy.data.cameras.new("RenderCam")
cam_obj = bpy.data.objects.new("RenderCam", cam_data)
scene.collection.objects.link(cam_obj)
scene.camera = cam_obj

for filename, loc, target, lens in shots:
    cam_obj.location = loc
    orient_camera_look_at(cam_obj, target)
    cam_data.lens = lens
    cam_data.clip_end = 7000.0
    
    out_path = os.path.join(output_dir, filename)
    scene.render.filepath = out_path
    print(f"Rendering {filename} to {out_path}...")
    bpy.ops.render.render(write_still=True)
    print(f"Rendered {filename} successfully.")

print("All 6 showcases rendered successfully.")

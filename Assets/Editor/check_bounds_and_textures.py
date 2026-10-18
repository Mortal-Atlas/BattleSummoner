import bpy
import os

fbx_in = r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Meshy\Character_MeshyHero.fbx"
output_folder = r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Meshy"
fbx_rigged_out = os.path.join(output_folder, "Character_MeshyHero_Rigged.fbx")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx_in)

mesh_obj = None
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        mesh_obj = obj
        break

if not mesh_obj:
    print("Error: No mesh found!")
    sys.exit(1)

import mathutils

# Check bounding box
bbox = [mesh_obj.matrix_world @ mathutils.Vector(corner) for corner in mesh_obj.bound_box]
min_x = min(v.x for v in bbox)
max_x = max(v.x for v in bbox)
min_y = min(v.y for v in bbox)
max_y = max(v.y for v in bbox)
min_z = min(v.z for v in bbox)
max_z = max(v.z for v in bbox)

height = max_z - min_z
center_x = (min_x + max_x) * 0.5
center_y = (min_y + max_y) * 0.5

print(f"Bounds: X({min_x:.3f}, {max_x:.3f}) Y({min_y:.3f}, {max_y:.3f}) Z({min_z:.3f}, {max_z:.3f}) Height={height:.3f}")

# Check textures and unpack if any
for img in bpy.data.images:
    print(f"Image found: {img.name} size: {img.size}")
    if img.packed_file:
        tex_save_path = os.path.join(output_folder, f"{img.name}.png")
        img.filepath_raw = tex_save_path
        img.file_format = 'PNG'
        img.save()
        print(f"Unpacked and saved texture: {tex_save_path}")

print("Inspection completed.")

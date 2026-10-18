import bpy
import os
import mathutils

models_dir = r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\OverworldPieces"
tex_dir = os.path.join(models_dir, "Textures")
os.makedirs(tex_dir, exist_ok=True)

fbx_files = [f for f in os.listdir(models_dir) if f.endswith(".fbx")]
print(f"Processing {len(fbx_files)} FBX files...")

for fbx_name in fbx_files:
    fbx_path = os.path.join(models_dir, fbx_name)
    base_name = os.path.splitext(fbx_name)[0]
    
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        bpy.ops.import_scene.fbx(filepath=fbx_path)
    except Exception as e:
        print(f"Failed to import {fbx_name}: {e}")
        continue
        
    print(f"\n--- {base_name} ---")
    img_idx = 0
    for img in bpy.data.images:
        if img.name != 'Render Result':
            print(f"  Image: {img.name}, size: {img.size}")
            if img.packed_file or img.has_data:
                tex_filename = f"tex_{base_name}_{img_idx}.png"
                out_path = os.path.join(tex_dir, tex_filename)
                img.filepath_raw = out_path
                img.file_format = 'PNG'
                try:
                    img.save()
                    print(f"  Saved texture: {tex_filename}")
                except Exception as e:
                    print(f"  Texture save error: {e}")
                img_idx += 1
                
    for obj in bpy.data.objects:
        if obj.type == 'MESH':
            bbox = [obj.matrix_world @ mathutils.Vector(c) for c in obj.bound_box]
            dims = obj.dimensions
            print(f"  Mesh: {obj.name} | Dims: ({dims.x:.2f}, {dims.y:.2f}, {dims.z:.2f})")

print("\nAll overworld piece textures extracted!")

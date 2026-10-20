import bpy
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
models = [
    "Assets/Models/Characters/Starter/femalestarter.glb",
    "Assets/Models/Characters/Starter/MaleStarter.glb",
]

for rel_path in models:
    full_path = os.path.join(project_root, rel_path)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=full_path)
    
    mesh = [o for o in bpy.data.objects if o.type == 'MESH'][0]
    verts = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
    max_z = max(v.z for v in verts)
    min_z = min(v.z for v in verts)
    
    head_verts = [v for v in verts if v.z > max_z - 0.2]
    feet_verts = [v for v in verts if v.z < min_z + 0.15]
    
    avg_head_y = sum(v.y for v in head_verts)/len(head_verts)
    min_head_y = min(v.y for v in head_verts)
    max_head_y = max(v.y for v in head_verts)
    
    avg_feet_y = sum(v.y for v in feet_verts)/len(feet_verts)
    min_feet_y = min(v.y for v in feet_verts)
    max_feet_y = max(v.y for v in feet_verts)
    
    print(f"\nModel: {rel_path}")
    print(f"  Head Z > {max_z - 0.2:.2f}: Y range=[{min_head_y:.3f}, {max_head_y:.3f}], avg_Y={avg_head_y:.3f}")
    print(f"  Feet Z < {min_z + 0.15:.2f}: Y range=[{min_feet_y:.3f}, {max_feet_y:.3f}], avg_Y={avg_feet_y:.3f}")

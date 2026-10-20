import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
fbx_path = os.path.join(project_root, "Assets/Models/Characters/Character_Modular_Base.fbx")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx_path)

print("--- Inspecting Character_Modular_Base.fbx ---")
for obj in bpy.data.objects:
    if obj.type == 'ARMATURE':
        print(f"Armature: {obj.name}")
        for bname in ['Hips', 'Spine', 'Head', 'LeftFoot', 'RightFoot']:
            b = obj.data.bones.get(bname)
            if b:
                print(f"  Bone '{b.name}': head={b.head_local}, tail={b.tail_local}, vec={b.vector}")
    elif obj.type == 'MESH' and 'Head' in obj.name:
        print(f"Mesh: {obj.name}")
        # check vertices
        verts = [obj.matrix_world @ v.co for v in obj.data.vertices]
        min_y = min(v.y for v in verts)
        max_y = max(v.y for v in verts)
        print(f"  Head bounds: Y=[{min_y:.4f}, {max_y:.4f}]")

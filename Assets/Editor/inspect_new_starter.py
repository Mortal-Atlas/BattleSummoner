import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
fbx_path = os.path.join(project_root, "Assets/Models/Characters/Starter/Starter_Female_Rigged.fbx")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx_path)

mesh = [o for o in bpy.data.objects if o.type == 'MESH'][0]
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]

print("\n--- Inspecting Starter_Female_Rigged.fbx in Blender ---")
print(f"Mesh name: {mesh.name}, rot: {[math.degrees(a) for a in mesh.rotation_euler]}")
print(f"Arm name: {arm.name}, rot: {[math.degrees(a) for a in arm.rotation_euler]}")

# Bones
for bname in ['Hips', 'Head', 'LeftFoot', 'RightFoot']:
    b = arm.data.bones.get(bname)
    if b:
        print(f"Bone {bname}: head={b.head_local}, tail={b.tail_local}, vec={b.vector}")

# Let's check mesh vertices in head region (Z > 0.7)
head_verts = [mesh.matrix_world @ v.co for v in mesh.data.vertices if (mesh.matrix_world @ v.co).z > 0.7]
print(f"Head verts: {len(head_verts)}")
min_y = min(v.y for v in head_verts)
max_y = max(v.y for v in head_verts)
avg_y = sum(v.y for v in head_verts)/len(head_verts)
min_x = min(v.x for v in head_verts)
max_x = max(v.x for v in head_verts)
print(f"Head bounds in Blender: X=[{min_x:.3f}, {max_x:.3f}], Y=[{min_y:.3f}, {max_y:.3f}], avg_Y={avg_y:.3f}")

# Center head vertices (|X| < 0.02)
c_verts = [v for v in head_verts if abs(v.x) < 0.02]
c_min_y = min(v.y for v in c_verts)
c_max_y = max(v.y for v in c_verts)
print(f"Center Head bounds: Y=[{c_min_y:.3f}, {c_max_y:.3f}]")

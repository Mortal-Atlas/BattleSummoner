import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
blend_path = os.path.join(project_root, "Assets/Models/Characters/Character_Base_Male_MouthRigged.blend")

bpy.ops.wm.open_mainfile(filepath=blend_path)
mesh = bpy.data.objects.get("Character_Mesh")
arm = bpy.data.objects.get("Character_Armature")

print("\n--- Cross section along centerline (|X| < 0.005) ---")
centerline_verts = []
for v in mesh.data.vertices:
    w_co = mesh.matrix_world @ v.co
    if abs(w_co.x) < 0.005:
        centerline_verts.append((w_co, v))

# Sort by Z descending
centerline_verts.sort(key=lambda item: item[0].z, reverse=True)

print(f"Total centerline verts: {len(centerline_verts)}")
print(f"Top 30 centerline verts (Z descending):")
for w_co, v in centerline_verts[:30]:
    print(f"  Z={w_co.z:.4f}, Y={w_co.y:.4f} (local: x={v.co.x:.4f}, y={v.co.y:.4f}, z={v.co.z:.4f})")

# Look at head region between Z = 1.45 and Z = 1.75
head_centerline = [item for item in centerline_verts if 1.45 <= item[0].z <= 1.75]
print(f"\nHead centerline verts (1.45 <= Z <= 1.75): {len(head_centerline)}")
for w_co, v in head_centerline:
    print(f"  Z={w_co.z:.4f}, Y={w_co.y:.4f}")

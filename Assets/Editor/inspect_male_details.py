import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
blend_path = os.path.join(project_root, "Assets/Models/Characters/Character_Base_Male_MouthRigged.blend")

bpy.ops.wm.open_mainfile(filepath=blend_path)
mesh = bpy.data.objects.get("Character_Mesh")
arm = bpy.data.objects.get("Character_Armature")

print("\n--- Inspecting Character_Base_Male_MouthRigged.blend ---")
print("Mesh matrix_world:")
print(mesh.matrix_world)

# Check world coordinates of all vertices in head region (world Z > 1.5)
head_verts = []
for v in mesh.data.vertices:
    w_co = mesh.matrix_world @ v.co
    if w_co.z > 1.5:
        head_verts.append((w_co, v))

print(f"Total head verts (Z > 1.5): {len(head_verts)}")
# Find vertex with min Y and max Y in head region
min_y_v = min(head_verts, key=lambda item: item[0].y)
max_y_v = max(head_verts, key=lambda item: item[0].y)

print(f"Min Y vertex (world): co={min_y_v[0]}, local={min_y_v[1].co}")
print(f"Max Y vertex (world): co={max_y_v[0]}, local={max_y_v[1].co}")

# Check vertex groups for Lip_Upper_Mid, Nose, Eyes
for vg_name in ['Lip_Upper_Mid', 'Head', 'Nose', 'Eye_Left', 'Eye_Right']:
    vg = mesh.vertex_groups.get(vg_name)
    if vg:
        vg_verts = [mesh.matrix_world @ v.co for v in mesh.data.vertices for g in v.groups if g.group == vg.index and g.weight > 0.5]
        if vg_verts:
            avg_x = sum(c.x for c in vg_verts)/len(vg_verts)
            avg_y = sum(c.y for c in vg_verts)/len(vg_verts)
            avg_z = sum(c.z for c in vg_verts)/len(vg_verts)
            print(f"VG '{vg_name}': count={len(vg_verts)}, world avg=({avg_x:.4f}, {avg_y:.4f}, {avg_z:.4f})")

# Check bones in armature
print("\nArmature bones (world coords):")
for b in arm.data.bones:
    if any(k in b.name for k in ['Hips', 'Spine', 'Head', 'Eye', 'Lip', 'Face', 'Jaw', 'Foot']):
        head_w = arm.matrix_world @ b.head_local
        tail_w = arm.matrix_world @ b.tail_local
        print(f"  Bone '{b.name}': head=({head_w.x:.3f}, {head_w.y:.3f}, {head_w.z:.3f}), tail=({tail_w.x:.3f}, {tail_w.y:.3f}, {tail_w.z:.3f})")

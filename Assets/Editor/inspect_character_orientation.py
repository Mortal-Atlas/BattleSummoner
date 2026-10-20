import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
models_dir = os.path.join(project_root, "Assets/Models/Characters")

files_to_check = [
    "Character_Base_Female_MouthRigged.blend",
    "Character_Base_Male_MouthRigged.blend",
]

for fname in files_to_check:
    fpath = os.path.join(models_dir, fname)
    if not os.path.exists(fpath):
        print(f"File {fpath} does not exist!")
        continue
    
    print(f"\n==========================================")
    print(f"INSPECTING: {fname}")
    print(f"==========================================")
    bpy.ops.wm.open_mainfile(filepath=fpath)
    
    for obj in bpy.data.objects:
        print(f"Object: {obj.name}, type: {obj.type}, rotation_euler: {[math.degrees(a) for a in obj.rotation_euler]}")
        if obj.type == 'ARMATURE':
            arm = obj
            print(f"  Armature bones count: {len(arm.data.bones)}")
            for bname in ['Hips', 'hips', 'Spine', 'Head', 'head', 'Eye_Target_Master', 'Face_Rig_Master']:
                b = arm.data.bones.get(bname)
                if b:
                    print(f"    Bone '{bname}': head={b.head_local}, tail={b.tail_local}, vector={b.vector}")
        elif obj.type == 'MESH':
            mesh = obj
            # Check bounding box
            bbox = [list(corner) for corner in mesh.bound_box]
            min_y = min(c[1] for c in bbox)
            max_y = max(c[1] for c in bbox)
            min_z = min(c[2] for c in bbox)
            max_z = max(c[2] for c in bbox)
            print(f"  Mesh bounds: Y=[{min_y:.3f}, {max_y:.3f}], Z=[{min_z:.3f}, {max_z:.3f}]")
            
            # Find face vs back of head: check nose / eyes / chin vertices vs back of head vertices
            # Vertices with Z > 1.4 (head region)
            head_verts = [v.co for v in mesh.data.vertices if v.co.z > 1.4]
            if head_verts:
                head_min_y = min(v.y for v in head_verts)
                head_max_y = max(v.y for v in head_verts)
                print(f"  Head vertices Y range: min_y={head_min_y:.4f}, max_y={head_max_y:.4f}")
                
            # Check nose/mouth position if vertex groups exist
            for vg in mesh.vertex_groups:
                if 'mouth' in vg.name.lower() or 'lip' in vg.name.lower() or 'nose' in vg.name.lower() or 'eye' in vg.name.lower():
                    # get average position of vertices in this group
                    v_coords = [mesh.data.vertices[v.index].co for v in mesh.data.vertices for g in v.groups if g.group == vg.index and g.weight > 0.5]
                    if v_coords:
                        avg_y = sum(c.y for c in v_coords) / len(v_coords)
                        avg_z = sum(c.z for c in v_coords) / len(v_coords)
                        print(f"    VG '{vg.name}': {len(v_coords)} verts, avg Y={avg_y:.4f}, Z={avg_z:.4f}")

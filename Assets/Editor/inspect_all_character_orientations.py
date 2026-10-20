import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
models_dir = os.path.join(project_root, "Assets/Models/Characters")

files_to_check = [
    ("BLEND", os.path.join(models_dir, "Character_Base_Male_MouthRigged.blend")),
    ("BLEND", os.path.join(models_dir, "Character_Base_Female_MouthRigged.blend")),
    ("FBX", os.path.join(models_dir, "Character_Modular_Base.fbx")),
    ("FBX", os.path.join(models_dir, "Character_Base_Male_Genshin_Rigged.fbx")),
    ("FBX", os.path.join(models_dir, "Character_Base_Female_Genshin_Rigged.fbx")),
    ("FBX", os.path.join(models_dir, "Starter/Starter_Male_Rigged.fbx")),
    ("FBX", os.path.join(models_dir, "Starter/Starter_Female_Rigged.fbx")),
]

for ftype, fpath in files_to_check:
    if not os.path.exists(fpath):
        print(f"File {fpath} does not exist!")
        continue
    
    print(f"\n==================================================")
    print(f"INSPECTING: {os.path.basename(fpath)} ({ftype})")
    print(f"==================================================")
    
    # Reset scene
    bpy.ops.wm.read_factory_settings(use_empty=True)
    
    if ftype == "BLEND":
        bpy.ops.wm.open_mainfile(filepath=fpath)
    else:
        bpy.ops.import_scene.fbx(filepath=fpath)
        
    armatures = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    
    print(f"Armatures ({len(armatures)}): {[a.name for a in armatures]}")
    print(f"Meshes ({len(meshes)}): {[m.name for m in meshes]}")
    
    for arm in armatures:
        print(f"\n Armature: {arm.name}, rot_euler={[round(math.degrees(a), 2) for a in arm.rotation_euler]}")
        # Check bone positions in world / object space
        for bname in ['Hips', 'hips', 'Spine', 'spine', 'Head', 'head', 'Nose', 'Eye_Target_Master', 'Lip_Upper_Mid']:
            b = arm.data.bones.get(bname)
            if b:
                # bone vector
                v = b.vector
                print(f"   Bone '{b.name}': head=({b.head_local.x:.3f}, {b.head_local.y:.3f}, {b.head_local.z:.3f}), tail=({b.tail_local.x:.3f}, {b.tail_local.y:.3f}, {b.tail_local.z:.3f}), vec=({v.x:.3f}, {v.y:.3f}, {v.z:.3f})")
            
        # Check forward direction of hips and feet/toes
        for toe_name in ['LeftFoot', 'left_foot', 'RightFoot', 'right_foot', 'LeftToeBase', 'RightToeBase']:
            b = arm.data.bones.get(toe_name)
            if b:
                print(f"   Foot/Toe Bone '{b.name}': head=({b.head_local.x:.3f}, {b.head_local.y:.3f}, {b.head_local.z:.3f}), tail=({b.tail_local.x:.3f}, {b.tail_local.y:.3f}, {b.tail_local.z:.3f})")

    for mesh in meshes:
        print(f"\n Mesh: {mesh.name}, rot_euler={[round(math.degrees(a), 2) for a in mesh.rotation_euler]}")
        mat = mesh.matrix_world
        # Inspect vertices in head area (world Z > 1.3 or local Z/Y)
        verts_world = [mat @ v.co for v in mesh.data.vertices]
        if not verts_world:
            continue
        min_x = min(v.x for v in verts_world)
        max_x = max(v.x for v in verts_world)
        min_y = min(v.y for v in verts_world)
        max_y = max(v.y for v in verts_world)
        min_z = min(v.z for v in verts_world)
        max_z = max(v.z for v in verts_world)
        print(f"   World bounds: X=[{min_x:.3f}, {max_x:.3f}], Y=[{min_y:.3f}, {max_y:.3f}], Z=[{min_z:.3f}, {max_z:.3f}]")
        
        # Check vertices with highest Z (head level)
        head_verts = [v for v in verts_world if v.z > (max_z - 0.25)]
        if head_verts:
            head_min_y = min(v.y for v in head_verts)
            head_max_y = max(v.y for v in head_verts)
            head_avg_y = sum(v.y for v in head_verts) / len(head_verts)
            print(f"   Head level (Z > {max_z - 0.25:.2f}): Y bounds=[{head_min_y:.3f}, {head_max_y:.3f}], avg_Y={head_avg_y:.3f}")
            
            # Find the most extreme vertex along Y in head level: is face at +Y or -Y?
            # Usually the nose/face sticks out further than the back of the neck/head.
            # Also check vertices near center X (|X| < 0.03) at head level
            center_head_verts = [v for v in head_verts if abs(v.x) < 0.03]
            if center_head_verts:
                c_min_y = min(v.y for v in center_head_verts)
                c_max_y = max(v.y for v in center_head_verts)
                print(f"   Center Head (Z > {max_z - 0.25:.2f}, |X|<0.03): min_Y={c_min_y:.3f}, max_Y={c_max_y:.3f}")
                
        # Check feet level
        feet_verts = [v for v in verts_world if v.z < (min_z + 0.15)]
        if feet_verts:
            f_min_y = min(v.y for v in feet_verts)
            f_max_y = max(v.y for v in feet_verts)
            print(f"   Feet level (Z < {min_z + 0.15:.2f}): Y bounds=[{f_min_y:.3f}, {f_max_y:.3f}] (toes stick out in forward direction)")

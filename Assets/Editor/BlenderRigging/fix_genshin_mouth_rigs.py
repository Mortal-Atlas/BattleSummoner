import bpy
import numpy as np
from mathutils import Matrix, Vector
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"

def dist_point_to_segment(p, a, b):
    ab = b - a
    ab_len_sq = np.sum(ab * ab)
    if ab_len_sq < 1e-6:
        return np.linalg.norm(p - a, axis=1)
    t = np.sum((p - a) * ab, axis=1) / ab_len_sq
    t = np.clip(t, 0.0, 1.0)
    projection = a + t[:, np.newaxis] * ab
    return np.linalg.norm(p - projection, axis=1)

def skin_mesh_to_armature(mesh_obj, arm_obj, deform_bone_names, is_female):
    n_verts = len(mesh_obj.data.vertices)
    coords = np.empty(n_verts * 3, dtype=np.float32)
    mesh_obj.data.vertices.foreach_get('co', coords)
    coords = coords.reshape((n_verts, 3))
    
    bones = arm_obj.data.bones
    n_bones = len(deform_bone_names)
    
    print(f"Calculating segment distances for {n_verts} vertices to {n_bones} deform bones...")
    distances = np.zeros((n_verts, n_bones), dtype=np.float32)
    for i, name in enumerate(deform_bone_names):
        bone = bones[name]
        a = np.array(bone.head_local, dtype=np.float32)
        b = np.array(bone.tail_local, dtype=np.float32)
        distances[:, i] = dist_point_to_segment(coords, a, b)
    
    eps = 0.015
    raw_weights = 1.0 / (np.maximum(distances, 0.001) + eps) ** 2.5
    
    # Facial landmarks (on -Y front face in Blender -> maps to +Z in Unity)
    if is_female:
        mouth_z = 1.475; mouth_y = -0.065
        eye_z = 1.555; eye_y = -0.075; eye_x = 0.038
        brow_z = 1.590; brow_y = -0.082
        chin_z = 1.425; chin_y = -0.050
    else:
        mouth_z = 1.562; mouth_y = -0.070
        eye_z = 1.642; eye_y = -0.082; eye_x = 0.042
        brow_z = 1.675; brow_y = -0.092
        chin_z = 1.510; chin_y = -0.058

    bone_landmarks = {
        "Jaw_Master": ((0.0, chin_y + 0.02, chin_z + 0.01), 0.055),
        "Lip_Upper_Mid": ((0.0, mouth_y - 0.005, mouth_z + 0.008), 0.025),
        "Lip_Upper_Left": ((0.018, mouth_y - 0.003, mouth_z + 0.006), 0.022),
        "Lip_Upper_Right": ((-0.018, mouth_y - 0.003, mouth_z + 0.006), 0.022),
        "Lip_Lower_Mid": ((0.0, mouth_y - 0.005, mouth_z - 0.008), 0.025),
        "Lip_Lower_Left": ((0.016, mouth_y - 0.003, mouth_z - 0.006), 0.022),
        "Lip_Lower_Right": ((-0.016, mouth_y - 0.003, mouth_z - 0.006), 0.022),
        "Mouth_Corner_Left": ((0.028, mouth_y, mouth_z), 0.024),
        "Mouth_Corner_Right": ((-0.028, mouth_y, mouth_z), 0.024),
        "Tongue_Tip": ((0.0, mouth_y + 0.015, mouth_z - 0.004), 0.020),
        "Brow_Left": ((eye_x, brow_y, brow_z), 0.038),
        "Brow_Right": ((-eye_x, brow_y, brow_z), 0.038),
        "Eye_Left": ((eye_x, eye_y, eye_z), 0.028),
        "Eye_Right": ((-eye_x, eye_y, eye_z), 0.028),
        "Eyelid_Upper_Left": ((eye_x, eye_y - 0.005, eye_z + 0.010), 0.022),
        "Eyelid_Upper_Right": ((-eye_x, eye_y - 0.005, eye_z + 0.010), 0.022),
        "Eyelid_Lower_Left": ((eye_x, eye_y - 0.005, eye_z - 0.010), 0.022),
        "Eyelid_Lower_Right": ((-eye_x, eye_y - 0.005, eye_z - 0.010), 0.022),
    }

    for bname, (center, radius) in bone_landmarks.items():
        if bname in deform_bone_names:
            b_idx = deform_bone_names.index(bname)
            cx, cy, cz = center
            d_center = np.sqrt((coords[:, 0] - cx)**2 + (coords[:, 1] - cy)**2 + (coords[:, 2] - cz)**2)
            inside = d_center < radius
            boost = np.zeros(n_verts, dtype=np.float32)
            boost[inside] = 0.5 * (1.0 + np.cos(np.pi * d_center[inside] / radius)) * 50000.0
            raw_weights[:, b_idx] += boost

    top_indices = np.argsort(-raw_weights, axis=1)[:, :4]
    
    mesh_obj.vertex_groups.clear()
    vgroups = {name: mesh_obj.vertex_groups.new(name=name) for name in deform_bone_names}
    
    print("Writing normalized weights to vertex groups...")
    vert_group_weights = {name: ([], []) for name in deform_bone_names}
    for v_idx in range(n_verts):
        b_idx_top = top_indices[v_idx]
        w_top = raw_weights[v_idx, b_idx_top]
        w_sum = np.sum(w_top)
        if w_sum > 1e-6:
            w_norm = w_top / w_sum
            for bi, w in zip(b_idx_top, w_norm):
                if w > 0.01:
                    b_name = deform_bone_names[bi]
                    vert_group_weights[b_name][0].append(v_idx)
                    vert_group_weights[b_name][1].append(float(w))
    
    for name, (v_list, w_list) in vert_group_weights.items():
        if v_list:
            vg = vgroups[name]
            for vi, w in zip(v_list, w_list):
                vg.add([vi], w, 'REPLACE')
                
    mesh_obj.parent = arm_obj
    arm_mod = mesh_obj.modifiers.new(name="Armature", type='ARMATURE')
    arm_mod.object = arm_obj
    print(f"✅ Skinned {mesh_obj.name} cleanly to {arm_obj.name} across all {n_bones} bones. 0 unweighted verts!")

def rig_character(gender="male"):
    is_female = (gender.lower() == "female")
    print(f"\n==================================================")
    print(f"RIGGING GENSHIN CHARACTER: {gender.upper()} (FORWARD ALIGNED)")
    print(f"==================================================")
    
    bpy.ops.wm.read_factory_settings(use_empty=True)
    
    glb_path = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_Apose.glb")
    blend_path = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_MouthRigged.blend")
    tex_path = os.path.join(project_root, f"Assets/Models/Characters/Textures/Character_Base_{'Female' if is_female else 'Male'}_Genshin_BaseColor.png")
    fbx_mouth = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_BlenderMouthRigged.fbx")
    fbx_genshin = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_Genshin_Rigged.fbx")
    
    # 1. Import pristine mesh
    bpy.ops.import_scene.gltf(filepath=glb_path)
    mesh_obj = [o for o in bpy.data.objects if o.type == 'MESH'][0]
    mesh_obj.name = "Character_Mesh"
    
    bpy.context.view_layer.objects.active = mesh_obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    
    # Ground shift and scale
    v_co = [v.co for v in mesh_obj.data.vertices]
    min_z = min(v.z for v in v_co)
    max_z = max(v.z for v in v_co)
    height = max_z - min_z
    
    target_height = 1.78 if is_female else 1.82
    scale_factor = target_height / height
    
    for v in mesh_obj.data.vertices:
        v.co.z = (v.co.z - min_z) * scale_factor
        v.co.x = v.co.x * scale_factor
        v.co.y = v.co.y * scale_factor
    mesh_obj.data.update()
    
    # We want the face and chest to be at -Y in Blender (so it maps directly to +Z in Unity)
    # For Male: GLB has face at +Y, so rotate 180 to face -Y.
    # For Female: GLB already has face at -Y, so keep as is.
    if not is_female:
        mesh_obj.data.transform(Matrix.Rotation(math.pi, 4, 'Z'))
        mesh_obj.data.update()
    
    print(f"Mesh prepped: height={target_height:.3f}m, verts={len(mesh_obj.data.vertices)}, face at -Y (maps to +Z in Unity).")
    
    # 2. Build complete 73-bone Armature facing -Y (maps to +Z in Unity)
    bpy.ops.object.armature_add(location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = "Character_Armature"
    
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_obj.data.edit_bones
    eb.remove(eb[0])
    
    def add_bone(name, parent, head_pos, tail_pos, roll=0.0):
        b = eb.new(name)
        b.parent = parent
        b.head = Vector(head_pos)
        b.tail = Vector(tail_pos)
        b.roll = roll
        return b

    if is_female:
        z_hips = 0.86
        z_spine = 0.98
        z_chest = 1.14
        z_uchest = 1.28
        z_neck = 1.38
        z_head = 1.45
        z_top = 1.76
        
        x_leg = 0.095
        z_knee = 0.48
        z_ankle = 0.13
        z_toes = 0.05
        
        x_sh = 0.14
        z_sh = 1.32
        x_elbow = 0.28
        z_elbow = 1.10
        x_wrist = 0.40
        z_wrist = 0.88
        x_hand = 0.45
        z_hand = 0.76
        
        mouth_z = 1.475; mouth_y = -0.065
        eye_z = 1.555; eye_y = -0.075; eye_x = 0.038
        brow_z = 1.590; brow_y = -0.082
        chin_z = 1.425; chin_y = -0.050
        jaw_pivot_z = 1.460; jaw_pivot_y = -0.010
    else:
        z_hips = 0.936
        z_spine = 1.044
        z_chest = 1.206
        z_uchest = 1.386
        z_neck = 1.476
        z_head = 1.548
        z_top = 1.820
        
        x_leg = 0.110
        z_knee = 0.504
        z_ankle = 0.144
        z_toes = 0.054
        
        x_sh = 0.180
        z_sh = 1.422
        x_elbow = 0.320
        z_elbow = 1.188
        x_wrist = 0.440
        z_wrist = 0.936
        x_hand = 0.497
        z_hand = 0.799
        
        mouth_z = 1.562; mouth_y = -0.070
        eye_z = 1.642; eye_y = -0.082; eye_x = 0.042
        brow_z = 1.675; brow_y = -0.092
        chin_z = 1.510; chin_y = -0.058
        jaw_pivot_z = 1.550; jaw_pivot_y = -0.010

    # Spine chain (Head is up +Z, roll=0 ensures bone front faces -Y in Blender -> +Z in Unity)
    hips = add_bone('Hips', None, (0, 0, z_hips), (0, 0, z_hips + 0.10))
    spine = add_bone('Spine', hips, hips.tail, (0, 0, z_chest))
    chest = add_bone('Chest', spine, spine.tail, (0, 0, z_uchest))
    uchest = add_bone('UpperChest', chest, chest.tail, (0, 0, z_neck))
    neck = add_bone('Neck', uchest, uchest.tail, (0, 0, z_head))
    head = add_bone('Head', neck, neck.tail, (0, 0, z_top - 0.05))

    # Facial Roots
    face_root = add_bone("Face_Rig_Master", head, (0.0, mouth_y + 0.02, mouth_z), (0.0, mouth_y, mouth_z))
    jaw_master = add_bone("Jaw_Master", head, (0.0, jaw_pivot_y, jaw_pivot_z), (0.0, chin_y, chin_z))

    # Mouth Bones (facing -Y)
    add_bone("Lip_Upper_Mid", face_root, (0.0, mouth_y - 0.005, mouth_z + 0.008), (0.0, mouth_y - 0.015, mouth_z + 0.008))
    add_bone("Lip_Upper_Left", face_root, (0.018, mouth_y - 0.003, mouth_z + 0.006), (0.022, mouth_y - 0.012, mouth_z + 0.006))
    add_bone("Lip_Upper_Right", face_root, (-0.018, mouth_y - 0.003, mouth_z + 0.006), (-0.022, mouth_y - 0.012, mouth_z + 0.006))
    add_bone("Lip_Lower_Mid", jaw_master, (0.0, mouth_y - 0.005, mouth_z - 0.008), (0.0, mouth_y - 0.015, mouth_z - 0.008))
    add_bone("Lip_Lower_Left", jaw_master, (0.016, mouth_y - 0.003, mouth_z - 0.006), (0.020, mouth_y - 0.012, mouth_z - 0.006))
    add_bone("Lip_Lower_Right", jaw_master, (-0.016, mouth_y - 0.003, mouth_z - 0.006), (-0.020, mouth_y - 0.012, mouth_z - 0.006))
    add_bone("Mouth_Corner_Left", face_root, (0.028, mouth_y, mouth_z), (0.035, mouth_y - 0.008, mouth_z))
    add_bone("Mouth_Corner_Right", face_root, (-0.028, mouth_y, mouth_z), (-0.035, mouth_y - 0.008, mouth_z))
    tongue_root = add_bone("Tongue_Root", jaw_master, (0.0, mouth_y + 0.040, mouth_z - 0.010), (0.0, mouth_y + 0.022, mouth_z - 0.006))
    add_bone("Tongue_Tip", tongue_root, (0.0, mouth_y + 0.022, mouth_z - 0.006), (0.0, mouth_y + 0.005, mouth_z - 0.002))

    # Eyes & Brows (facing -Y)
    eye_target = add_bone("Eye_Target_Master", head, (0.0, eye_y - 0.35, eye_z), (0.0, eye_y - 0.45, eye_z))
    add_bone("Eye_Target_Left", eye_target, (eye_x, eye_y - 0.35, eye_z), (eye_x, eye_y - 0.45, eye_z))
    add_bone("Eye_Target_Right", eye_target, (-eye_x, eye_y - 0.35, eye_z), (-eye_x, eye_y - 0.45, eye_z))

    add_bone("Eye_Left", head, (eye_x, eye_y + 0.015, eye_z), (eye_x, eye_y - 0.020, eye_z))
    add_bone("Eye_Right", head, (-eye_x, eye_y + 0.015, eye_z), (-eye_x, eye_y - 0.020, eye_z))

    add_bone("Eyelid_Upper_Left", head, (eye_x, eye_y, eye_z + 0.014), (eye_x + 0.008, eye_y - 0.008, eye_z + 0.014))
    add_bone("Eyelid_Upper_Right", head, (-eye_x, eye_y, eye_z + 0.014), (-eye_x - 0.008, eye_y - 0.008, eye_z + 0.014))
    add_bone("Eyelid_Lower_Left", head, (eye_x, eye_y, eye_z - 0.014), (eye_x + 0.008, eye_y - 0.008, eye_z - 0.014))
    add_bone("Eyelid_Lower_Right", head, (-eye_x, eye_y, eye_z - 0.014), (-eye_x - 0.008, eye_y - 0.008, eye_z - 0.014))

    add_bone("Brow_Left", head, (eye_x, brow_y, brow_z), (eye_x + 0.020, brow_y + 0.005, brow_z + 0.005))
    add_bone("Brow_Right", head, (-eye_x, brow_y, brow_z), (-eye_x - 0.020, brow_y + 0.005, brow_z + 0.005))

    # Arms (+X is Left, -X is Right in Blender -> maps to -X Left, +X Right in Unity)
    l_sh = add_bone('LeftShoulder', uchest, (0.04, 0.0, z_uchest), (x_sh, 0.0, z_sh))
    l_arm = add_bone('LeftArm', l_sh, l_sh.tail, (x_elbow, 0.015, z_elbow))
    l_fa = add_bone('LeftForeArm', l_arm, l_arm.tail, (x_wrist, 0.010, z_wrist))
    l_h = add_bone('LeftHand', l_fa, l_fa.tail, (x_hand, 0.010, z_hand))
    
    # Left Fingers
    add_bone('LeftHandThumb1', l_h, (x_hand - 0.005, 0.00, z_hand + 0.01), (x_hand + 0.005, -0.01, z_hand + 0.00))
    add_bone('LeftHandThumb2', eb['LeftHandThumb1'], eb['LeftHandThumb1'].tail, (x_hand + 0.015, -0.02, z_hand - 0.01))
    add_bone('LeftHandIndex1', l_h, (x_hand + 0.010, 0.00, z_hand - 0.01), (x_hand + 0.020, 0.00, z_hand - 0.03))
    add_bone('LeftHandIndex2', eb['LeftHandIndex1'], eb['LeftHandIndex1'].tail, (x_hand + 0.027, 0.00, z_hand - 0.05))
    add_bone('LeftHandMiddle1', l_h, (x_hand + 0.005, 0.00, z_hand - 0.01), (x_hand + 0.005, 0.00, z_hand - 0.035))
    add_bone('LeftHandMiddle2', eb['LeftHandMiddle1'], eb['LeftHandMiddle1'].tail, (x_hand + 0.005, 0.00, z_hand - 0.060))
    add_bone('LeftHandRing1', l_h, (x_hand, 0.00, z_hand - 0.01), (x_hand, 0.00, z_hand - 0.03))
    add_bone('LeftHandRing2', eb['LeftHandRing1'], eb['LeftHandRing1'].tail, (x_hand - 0.006, 0.00, z_hand - 0.05))
    add_bone('LeftHandPinky1', l_h, (x_hand - 0.005, 0.00, z_hand - 0.01), (x_hand - 0.005, 0.00, z_hand - 0.025))
    add_bone('LeftHandPinky2', eb['LeftHandPinky1'], eb['LeftHandPinky1'].tail, (x_hand - 0.010, 0.00, z_hand - 0.04))

    # Right Arm (-X)
    r_sh = add_bone('RightShoulder', uchest, (-0.04, 0.0, z_uchest), (-x_sh, 0.0, z_sh))
    r_arm = add_bone('RightArm', r_sh, r_sh.tail, (-x_elbow, 0.015, z_elbow))
    r_fa = add_bone('RightForeArm', r_arm, r_arm.tail, (-x_wrist, 0.010, z_wrist))
    r_h = add_bone('RightHand', r_fa, r_fa.tail, (-x_hand, 0.010, z_hand))

    # Right Fingers
    add_bone('RightHandThumb1', r_h, (-x_hand + 0.005, 0.00, z_hand + 0.01), (-x_hand - 0.005, -0.01, z_hand + 0.00))
    add_bone('RightHandThumb2', eb['RightHandThumb1'], eb['RightHandThumb1'].tail, (-x_hand - 0.015, -0.02, z_hand - 0.01))
    add_bone('RightHandIndex1', r_h, (-x_hand - 0.010, 0.00, z_hand - 0.01), (-x_hand - 0.020, 0.00, z_hand - 0.03))
    add_bone('RightHandIndex2', eb['RightHandIndex1'], eb['RightHandIndex1'].tail, (-x_hand - 0.027, 0.00, z_hand - 0.05))
    add_bone('RightHandMiddle1', r_h, (-x_hand - 0.005, 0.00, z_hand - 0.01), (-x_hand - 0.005, 0.00, z_hand - 0.035))
    add_bone('RightHandMiddle2', eb['RightHandMiddle1'], eb['RightHandMiddle1'].tail, (-x_hand - 0.005, 0.00, z_hand - 0.060))
    add_bone('RightHandRing1', r_h, (-x_hand, 0.00, z_hand - 0.01), (-x_hand, 0.00, z_hand - 0.03))
    add_bone('RightHandRing2', eb['RightHandRing1'], eb['RightHandRing1'].tail, (-x_hand + 0.006, 0.00, z_hand - 0.05))
    add_bone('RightHandPinky1', r_h, (-x_hand + 0.005, 0.00, z_hand - 0.01), (-x_hand + 0.005, 0.00, z_hand - 0.025))
    add_bone('RightHandPinky2', eb['RightHandPinky1'], eb['RightHandPinky1'].tail, (-x_hand + 0.010, 0.00, z_hand - 0.04))

    # Legs (+X is Left, -X is Right. Knees and toes point to -Y in Blender -> +Z in Unity)
    l_uleg = add_bone('LeftUpLeg', hips, (x_leg, 0.0, z_hips), (x_leg, -0.02, z_knee))
    l_leg = add_bone('LeftLeg', l_uleg, l_uleg.tail, (x_leg, 0.0, z_ankle))
    l_foot = add_bone('LeftFoot', l_leg, l_leg.tail, (x_leg, -0.10, z_toes))
    l_toes = add_bone('LeftToes', l_foot, l_foot.tail, (x_leg, -0.17, z_toes - 0.02))

    r_uleg = add_bone('RightUpLeg', hips, (-x_leg, 0.0, z_hips), (-x_leg, -0.02, z_knee))
    r_leg = add_bone('RightLeg', r_uleg, r_uleg.tail, (-x_leg, 0.0, z_ankle))
    r_foot = add_bone('RightFoot', r_leg, r_leg.tail, (-x_leg, -0.10, z_toes))
    r_toes = add_bone('RightToes', r_foot, r_foot.tail, (-x_leg, -0.17, z_toes - 0.02))

    # Equipment Sockets
    add_bone('Socket_Head_Helmet', head, (0, 0, z_top - 0.05), (0, 0, z_top + 0.05))
    add_bone('Socket_Chest_Armor', chest, (0, -0.08, z_chest), (0, -0.22, z_chest))
    add_bone('Socket_Back_TwoHanded', chest, (0, 0.08, z_chest), (0, 0.22, z_chest))
    add_bone('Socket_Legs_Armor', hips, (0, 0, z_hips), (0, 0, z_hips - 0.10))
    add_bone('Socket_Feet_Shoes_Left', l_foot, (x_leg, -0.05, z_toes), (x_leg, -0.12, z_toes))
    add_bone('Socket_Feet_Shoes_Right', r_foot, (-x_leg, -0.05, z_toes), (-x_leg, -0.12, z_toes))
    add_bone('Socket_Weapon_LeftHand', l_h, (x_wrist + 0.06, 0.02, z_wrist - 0.10), (x_wrist + 0.06, 0.02, z_wrist - 0.25))
    add_bone('Socket_Weapon_RightHand', r_h, (-x_wrist - 0.06, 0.02, z_wrist - 0.10), (-x_wrist - 0.06, 0.02, z_wrist - 0.25))

    bpy.ops.object.mode_set(mode='OBJECT')
    print(f"Armature built with {len(arm_obj.data.bones)} bones facing -Y (Forward in Unity)!")

    # Mark non-deform bones
    deform_bones = []
    for b in arm_obj.data.bones:
        if 'Socket' in b.name or 'Target' in b.name or b.name == 'Face_Rig_Master':
            b.use_deform = False
        else:
            b.use_deform = True
            deform_bones.append(b.name)

    print(f"Total deform bones: {len(deform_bones)}")

    # 3. Robust skinning via segment-distance falloff
    skin_mesh_to_armature(mesh_obj, arm_obj, deform_bones, is_female)

    # 4. Setup Material
    mat = bpy.data.materials.new(name=f"M_{gender.capitalize()}_Base_Genshin")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf and os.path.exists(tex_path):
        tex_node = mat.node_tree.nodes.new('ShaderNodeTexImage')
        tex_node.image = bpy.data.images.load(tex_path)
        mat.node_tree.links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])
    mesh_obj.data.materials.clear()
    mesh_obj.data.materials.append(mat)

    # 5. Save Blend File
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved blend file to: {blend_path}")

    # 6. Export FBX (using bake_space_transform=True, matching Starter heroes!)
    bpy.ops.object.select_all(action='DESELECT')
    mesh_obj.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj

    for out_fbx in [fbx_mouth, fbx_genshin]:
        bpy.ops.export_scene.fbx(
            filepath=out_fbx,
            use_selection=True,
            global_scale=1.0,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_ALL',
            axis_forward='-Z',
            axis_up='Y',
            bake_space_transform=True,
            object_types={'ARMATURE', 'MESH'},
            mesh_smooth_type='FACE',
            add_leaf_bones=False,
            primary_bone_axis='Y',
            secondary_bone_axis='X',
            armature_nodetype='NULL'
        )
        print(f"Exported FBX to: {out_fbx}")

if __name__ == "__main__":
    rig_character("male")
    rig_character("female")

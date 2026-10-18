import bpy
import sys
import os
import math

def run_rigging(gender="male"):
    is_female = (gender.lower() == "female")
    project_root = r"d:/Games/BattleSummoner/BattleSummoner"
    
    in_fbx = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_Genshin_Rigged.fbx")
    out_fbx = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_BlenderMouthRigged.fbx")
    out_blend = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_MouthRigged.blend")

    print(f"\n==================================================")
    print(f"✦ STARTING BLENDER FACIAL & MOUTH RIGGING: {gender.upper()} ✦")
    print(f"Source FBX: {in_fbx}")
    print(f"==================================================")

    # 1. Clean Scene
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

    # 2. Import Base FBX
    bpy.ops.import_scene.fbx(filepath=in_fbx)

    armature = None
    mesh_obj = None
    for o in bpy.data.objects:
        if o.type == 'ARMATURE': armature = o
        elif o.type == 'MESH': mesh_obj = o

    if not armature or not mesh_obj:
        raise RuntimeError("Could not find Armature or Mesh in imported FBX!")

    print(f"Loaded Armature '{armature.name}' ({len(armature.data.bones)} bones)")
    print(f"Loaded Mesh '{mesh_obj.name}' ({len(mesh_obj.data.vertices)} vertices)")

    # 3. Calculate Character Landmarks
    # Male: Mouth center Y=-0.105, Z=1.575
    # Female: Mouth center Y=-0.119, Z=1.531
    mouth_z = 1.531 if is_female else 1.575
    mouth_y = -0.119 if is_female else -0.105
    jaw_pivot_z = mouth_z - 0.015
    jaw_pivot_y = mouth_y + 0.065 # slightly back near ear
    chin_z = mouth_z - 0.055
    chin_y = mouth_y - 0.015
    brow_z = 1.635 if is_female else 1.675
    brow_y = mouth_y - 0.035
    eye_z = 1.585 if is_female else 1.625
    eye_y = mouth_y - 0.025

    # 4. Add Professional Armature Bones in Edit Mode
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode='EDIT')
    eb = armature.data.edit_bones
    head_eb = eb.get('Head') or eb.get('head')
    if not head_eb:
        raise RuntimeError("Head bone not found in armature!")

    def add_bone(name, parent, head_pos, tail_pos):
        existing = eb.get(name)
        if existing: eb.remove(existing)
        b = eb.new(name)
        b.parent = parent
        b.head = head_pos
        b.tail = tail_pos
        return b

    # Facial Root Master
    face_root = add_bone("Face_Rig_Master", head_eb, (0.0, mouth_y + 0.02, mouth_z), (0.0, mouth_y, mouth_z))

    # Jaw Master (Hinges at jaw pivot, points down to chin)
    jaw_master = add_bone("Jaw_Master", head_eb, (0.0, jaw_pivot_y, jaw_pivot_z), (0.0, chin_y, chin_z))

    # Upper Lip Bones (Children of Face_Rig_Master)
    lip_up_mid = add_bone("Lip_Upper_Mid", face_root, (0.0, mouth_y - 0.005, mouth_z + 0.008), (0.0, mouth_y - 0.015, mouth_z + 0.008))
    lip_up_l = add_bone("Lip_Upper_Left", face_root, (0.018, mouth_y - 0.003, mouth_z + 0.006), (0.022, mouth_y - 0.012, mouth_z + 0.006))
    lip_up_r = add_bone("Lip_Upper_Right", face_root, (-0.018, mouth_y - 0.003, mouth_z + 0.006), (-0.022, mouth_y - 0.012, mouth_z + 0.006))

    # Lower Lip Bones (Children of Jaw_Master so they follow jaw drops automatically)
    lip_low_mid = add_bone("Lip_Lower_Mid", jaw_master, (0.0, mouth_y - 0.005, mouth_z - 0.008), (0.0, mouth_y - 0.015, mouth_z - 0.008))
    lip_low_l = add_bone("Lip_Lower_Left", jaw_master, (0.016, mouth_y - 0.003, mouth_z - 0.006), (0.020, mouth_y - 0.012, mouth_z - 0.006))
    lip_low_r = add_bone("Lip_Lower_Right", jaw_master, (-0.016, mouth_y - 0.003, mouth_z - 0.006), (-0.020, mouth_y - 0.012, mouth_z - 0.006))

    # Mouth Corners
    corner_l = add_bone("Mouth_Corner_Left", face_root, (0.028, mouth_y, mouth_z), (0.035, mouth_y - 0.008, mouth_z))
    corner_r = add_bone("Mouth_Corner_Right", face_root, (-0.028, mouth_y, mouth_z), (-0.035, mouth_y - 0.008, mouth_z))

    # Tongue Chain (Inside Oral Cavity, child of Jaw)
    tongue_root = add_bone("Tongue_Root", jaw_master, (0.0, mouth_y + 0.040, mouth_z - 0.010), (0.0, mouth_y + 0.022, mouth_z - 0.006))
    tongue_tip = add_bone("Tongue_Tip", tongue_root, (0.0, mouth_y + 0.022, mouth_z - 0.006), (0.0, mouth_y + 0.005, mouth_z - 0.002))

    # Eyebrow Bones
    brow_l = add_bone("Brow_Left", head_eb, (0.035, brow_y, brow_z), (0.055, brow_y + 0.005, brow_z + 0.005))
    brow_r = add_bone("Brow_Right", head_eb, (-0.035, brow_y, brow_z), (-0.055, brow_y + 0.005, brow_z + 0.005))

    # Eyelids
    eyelid_up_l = add_bone("Eyelid_Upper_Left", head_eb, (0.038, eye_y, eye_z + 0.012), (0.048, eye_y, eye_z + 0.012))
    eyelid_up_r = add_bone("Eyelid_Upper_Right", head_eb, (-0.038, eye_y, eye_z + 0.012), (-0.048, eye_y, eye_z + 0.012))

    bpy.ops.object.mode_set(mode='OBJECT')
    print(f"Successfully added 15 professional facial/mouth bones to {armature.name}!")

    # 5. Paint Smooth Vertex Weights (Vertex Groups) on Mesh
    bpy.context.view_layer.objects.active = mesh_obj
    mat_world = mesh_obj.matrix_world
    verts = mesh_obj.data.vertices

    # Define Bone Landmark Radii & Centers in World Space
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
        "Brow_Left": ((0.038, brow_y, brow_z), 0.038),
        "Brow_Right": ((-0.038, brow_y, brow_z), 0.038),
        "Eyelid_Upper_Left": ((0.040, eye_y, eye_z + 0.008), 0.028),
        "Eyelid_Upper_Right": ((-0.040, eye_y, eye_z + 0.008), 0.028),
    }

    for bname, (center, radius) in bone_landmarks.items():
        vg = mesh_obj.vertex_groups.get(bname)
        if not vg: vg = mesh_obj.vertex_groups.new(name=bname)
        cx, cy, cz = center
        r_sq = radius * radius
        for i, v in enumerate(verts):
            w_co = mat_world @ v.co
            dx = w_co.x - cx
            dy = w_co.y - cy
            dz = w_co.z - cz
            dist_sq = dx*dx + dy*dy + dz*dz
            if dist_sq < r_sq:
                dist = math.sqrt(dist_sq)
                w = 0.5 * (1.0 + math.cos(math.pi * dist / radius))
                vg.add([i], w, 'REPLACE')

    print(f"Baked smooth vertex groups for all 14 facial landmarks!")

    # 6. Build Professional Shape Keys (Morph Targets)
    # Clear existing shape keys if any
    if mesh_obj.data.shape_keys:
        bpy.ops.object.shape_key_clear()

    sk_basis = mesh_obj.shape_key_add(name="Basis", from_mix=False)
    
    # Target shape key configurations
    # Local mouth center: (0.0, Y_local, Z_local)
    # In mesh local space: Y = World Z, Z = -World Y
    loc_mouth_y = mouth_z
    loc_mouth_z = -mouth_y
    loc_brow_y = brow_z
    loc_brow_z = -brow_y
    loc_eye_y = eye_z
    loc_eye_z = -eye_y

    shape_key_defs = [
        "Vowel_A", "Vowel_I", "Vowel_U", "Vowel_E", "Vowel_O",
        "Mouth_Smile", "Mouth_Frown", "Mouth_Open", "Mouth_Pout",
        "Mouth_Smirk_L", "Mouth_Smirk_R",
        "Brow_Raise_L", "Brow_Raise_R", "Brow_Angry_L", "Brow_Angry_R", "Brow_Sad_L", "Brow_Sad_R",
        "Eye_Blink_L", "Eye_Blink_R", "Eye_Wide_L", "Eye_Wide_R"
    ]

    r_mouth = 0.055
    r_brow = 0.045
    r_eye = 0.038

    for sk_name in shape_key_defs:
        sk = mesh_obj.shape_key_add(name=sk_name, from_mix=False)
        for i, v in enumerate(verts):
            lx, ly, lz = v.co.x, v.co.y, v.co.z
            
            # Mouth calculations
            dm = math.sqrt(lx*lx + (ly - loc_mouth_y)**2 + (lz - loc_mouth_z)**2)
            if dm < r_mouth and any(k in sk_name for k in ["Vowel", "Mouth"]):
                w = 0.5 * (1.0 + math.cos(math.pi * dm / r_mouth))
                sign_x = 1.0 if lx >= 0 else -1.0
                rel_y = ly - loc_mouth_y

                dx, dy, dz = 0.0, 0.0, 0.0

                if sk_name == "Vowel_A":
                    # Open Jaw drop
                    if rel_y <= 0.004:
                        dy = -0.012 * w
                        dz = -0.003 * w
                    else:
                        dy = 0.002 * w
                elif sk_name == "Vowel_I":
                    # Wide horizontal smile
                    dx = sign_x * 0.010 * w
                    dy = 0.003 * w if rel_y >= 0 else -0.002 * w
                elif sk_name == "Vowel_U":
                    # Pursed rounded protrusion
                    dz = 0.008 * w
                    dx = -sign_x * 0.006 * w
                    dy = -0.003 * w if rel_y <= 0 else 0.0
                elif sk_name == "Vowel_E":
                    # Relaxed wide opening
                    dx = sign_x * 0.006 * w
                    dy = -0.007 * w if rel_y <= 0 else 0.001 * w
                elif sk_name == "Vowel_O":
                    # Tall vertical oval
                    dy = -0.014 * w if rel_y <= 0 else 0.002 * w
                    dx = -sign_x * 0.005 * w
                    dz = 0.004 * w
                elif sk_name == "Mouth_Smile":
                    # Zygomatic pull
                    dx = sign_x * 0.007 * w
                    dy = 0.008 * w
                elif sk_name == "Mouth_Frown":
                    dy = -0.008 * w
                elif sk_name == "Mouth_Open":
                    dy = -0.016 * w if rel_y <= 0 else 0.0
                elif sk_name == "Mouth_Pout":
                    if rel_y <= 0:
                        dz = 0.007 * w
                        dy = 0.003 * w
                elif sk_name == "Mouth_Smirk_L":
                    if lx > 0:
                        dx = 0.008 * w
                        dy = 0.009 * w
                elif sk_name == "Mouth_Smirk_R":
                    if lx < 0:
                        dx = -0.008 * w
                        dy = 0.009 * w

                if dx != 0.0 or dy != 0.0 or dz != 0.0:
                    sk.data[i].co = (lx + dx, ly + dy, lz + dz)

            # Eyebrow calculations
            db_l = math.sqrt((lx - 0.038)**2 + (ly - loc_brow_y)**2 + (lz - loc_brow_z)**2)
            db_r = math.sqrt((lx + 0.038)**2 + (ly - loc_brow_y)**2 + (lz - loc_brow_z)**2)

            if "Brow" in sk_name:
                if "L" in sk_name and db_l < r_brow:
                    wb = 0.5 * (1.0 + math.cos(math.pi * db_l / r_brow))
                    if sk_name == "Brow_Raise_L":
                        sk.data[i].co.y += 0.012 * wb
                    elif sk_name == "Brow_Angry_L":
                        sk.data[i].co.y -= 0.008 * wb
                        sk.data[i].co.x -= 0.005 * wb
                    elif sk_name == "Brow_Sad_L":
                        sk.data[i].co.y += 0.009 * wb
                        sk.data[i].co.x += 0.003 * wb
                elif "R" in sk_name and db_r < r_brow:
                    wb = 0.5 * (1.0 + math.cos(math.pi * db_r / r_brow))
                    if sk_name == "Brow_Raise_R":
                        sk.data[i].co.y += 0.012 * wb
                    elif sk_name == "Brow_Angry_R":
                        sk.data[i].co.y -= 0.008 * wb
                        sk.data[i].co.x += 0.005 * wb
                    elif sk_name == "Brow_Sad_R":
                        sk.data[i].co.y += 0.009 * wb
                        sk.data[i].co.x -= 0.003 * wb

            # Eye calculations
            de_l = math.sqrt((lx - 0.040)**2 + (ly - loc_eye_y)**2 + (lz - loc_eye_z)**2)
            de_r = math.sqrt((lx + 0.040)**2 + (ly - loc_eye_y)**2 + (lz - loc_eye_z)**2)
            if "Eye" in sk_name:
                if "L" in sk_name and de_l < r_eye:
                    we = 0.5 * (1.0 + math.cos(math.pi * de_l / r_eye))
                    if sk_name == "Eye_Blink_L" and ly >= loc_eye_y:
                        sk.data[i].co.y -= 0.012 * we
                    elif sk_name == "Eye_Wide_L":
                        sk.data[i].co.y += 0.005 * we
                elif "R" in sk_name and de_r < r_eye:
                    we = 0.5 * (1.0 + math.cos(math.pi * de_r / r_eye))
                    if sk_name == "Eye_Blink_R" and ly >= loc_eye_y:
                        sk.data[i].co.y -= 0.012 * we
                    elif sk_name == "Eye_Wide_R":
                        sk.data[i].co.y += 0.005 * we

    print(f"Baked {len(mesh_obj.data.shape_keys.key_blocks)} professional shape keys into mesh!")

    # 7. Create Professional Baked Animation Actions
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode='POSE')
    armature.animation_data_create()

    # Bone references
    pb_jaw = armature.pose.bones['Jaw_Master']
    pb_jaw.rotation_mode = 'XYZ'
    pb_lip_up = armature.pose.bones['Lip_Upper_Mid']
    pb_lip_low = armature.pose.bones['Lip_Lower_Mid']
    pb_c_l = armature.pose.bones['Mouth_Corner_Left']
    pb_c_r = armature.pose.bones['Mouth_Corner_Right']
    pb_brow_l = armature.pose.bones['Brow_Left']
    pb_brow_r = armature.pose.bones['Brow_Right']

    def reset_pose():
        for pb in armature.pose.bones:
            pb.location = (0, 0, 0)
            pb.rotation_euler = (0, 0, 0)
            pb.scale = (1, 1, 1)

    def bake_clip(name, num_frames, keyframe_fn):
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        armature.animation_data.action = act
        reset_pose()

        for f in range(1, num_frames + 1):
            bpy.context.scene.frame_set(f)
            t = (f - 1) / float(max(1, num_frames - 1))
            keyframe_fn(f, t)
            pb_jaw.keyframe_insert(data_path="rotation_euler", frame=f)
            pb_jaw.keyframe_insert(data_path="location", frame=f)
            pb_lip_up.keyframe_insert(data_path="location", frame=f)
            pb_lip_low.keyframe_insert(data_path="location", frame=f)
            pb_c_l.keyframe_insert(data_path="location", frame=f)
            pb_c_r.keyframe_insert(data_path="location", frame=f)
            pb_brow_l.keyframe_insert(data_path="location", frame=f)
            pb_brow_r.keyframe_insert(data_path="location", frame=f)

        track = armature.animation_data.nla_tracks.new()
        track.name = name
        track.strips.new(name, 1, act)
        print(f"Baked NLA Animation Clip '{name}' ({num_frames} frames)")

    # CLIP 1: Action_Phonetic_Vowels_Cycle (60 frames @ 30fps)
    # A (F12) -> I (F24) -> U (F36) -> E (F48) -> O (F56) -> Sil (F60)
    def key_vowels_cycle(f, t):
        reset_pose()
        if f <= 12: # Sil -> A
            p = (f - 1) / 11.0
            pb_jaw.rotation_euler = (0.22 * p, 0, 0)
            pb_lip_low.location = (0, 0, -0.008 * p)
        elif f <= 24: # A -> I
            p = (f - 12) / 12.0
            pb_jaw.rotation_euler = (math.radians(12.6) * (1 - p) + math.radians(4.5) * p, 0, 0)
            pb_c_l.location = (0.008 * p, 0, 0.004 * p)
            pb_c_r.location = (-0.008 * p, 0, 0.004 * p)
        elif f <= 36: # I -> U
            p = (f - 24) / 12.0
            pb_jaw.rotation_euler = (math.radians(4.5) * (1 - p) + math.radians(5.1) * p, 0, 0)
            pb_c_l.location = (0.008 * (1 - p) - 0.005 * p, 0, 0.004 * (1 - p))
            pb_c_r.location = (-0.008 * (1 - p) + 0.005 * p, 0, 0.004 * (1 - p))
            pb_lip_up.location = (0, -0.008 * p, 0)
            pb_lip_low.location = (0, -0.008 * p, 0)
        elif f <= 48: # U -> E
            p = (f - 36) / 12.0
            pb_jaw.rotation_euler = (math.radians(5.1) * (1 - p) + math.radians(8.0) * p, 0, 0)
            pb_c_l.location = (-0.005 * (1 - p) + 0.005 * p, 0, 0)
            pb_c_r.location = (0.005 * (1 - p) - 0.005 * p, 0, 0)
            pb_lip_up.location = (0, -0.008 * (1 - p), 0)
            pb_lip_low.location = (0, -0.008 * (1 - p) - 0.004 * p, 0)
        elif f <= 56: # E -> O
            p = (f - 48) / 8.0
            pb_jaw.rotation_euler = (math.radians(8.0) * (1 - p) + math.radians(11.5) * p, 0, 0)
            pb_c_l.location = (0.005 * (1 - p) - 0.004 * p, 0, 0)
            pb_c_r.location = (-0.005 * (1 - p) + 0.004 * p, 0, 0)
            pb_lip_low.location = (0, -0.005 * p, -0.004 * (1 - p) - 0.008 * p)
        else: # O -> Sil
            p = (f - 56) / 4.0
            pb_jaw.rotation_euler = (math.radians(11.5) * (1 - p), 0, 0)
            pb_c_l.location = (-0.004 * (1 - p), 0, 0)
            pb_c_r.location = (0.004 * (1 - p), 0, 0)
            pb_lip_low.location = (0, -0.005 * (1 - p), -0.008 * (1 - p))

    bake_clip("Action_Phonetic_Vowels_Cycle", 60, key_vowels_cycle)

    # CLIP 2: Action_Dialogue_Intro_WhereAmI (84 frames = 2.8 sec)
    # Spoken: "System link established... Where... am I?"
    def key_dialogue_intro(f, t):
        reset_pose()
        # Wonder Eyebrows
        wonder_w = min(1.0, f / 18.0)
        pb_brow_l.location = (0, 0, 0.009 * wonder_w)
        pb_brow_r.location = (0, 0, 0.009 * wonder_w)

        # Phonetic Syllables
        if 8 <= f <= 16: # "Sys-" (Vowel I)
            p = math.sin((f - 8) / 8.0 * math.pi)
            pb_jaw.rotation_euler = (0.06 * p, 0, 0)
            pb_c_l.location = (0.006 * p, 0, 0.003 * p)
            pb_c_r.location = (-0.006 * p, 0, 0.003 * p)
        elif 16 < f <= 24: # "-tem" (Vowel E)
            p = math.sin((f - 16) / 8.0 * math.pi)
            pb_jaw.rotation_euler = (0.12 * p, 0, 0)
            pb_c_l.location = (0.004 * p, 0, 0)
            pb_c_r.location = (-0.004 * p, 0, 0)
        elif 24 < f <= 34: # "link" (Vowel I)
            p = math.sin((f - 24) / 10.0 * math.pi)
            pb_jaw.rotation_euler = (0.08 * p, 0, 0)
            pb_c_l.location = (0.007 * p, 0, 0.002 * p)
            pb_c_r.location = (-0.007 * p, 0, 0.002 * p)
        elif 34 < f <= 46: # "es-tab-lished" (E-A-I cadence)
            p = math.sin((f - 34) / 12.0 * math.pi)
            pb_jaw.rotation_euler = (0.15 * p, 0, 0)
        elif 46 < f <= 56: # [Dramatic Pause]
            pb_jaw.rotation_euler = (0.02, 0, 0)
        elif 56 < f <= 66: # "Where..." (U -> E)
            p = math.sin((f - 56) / 10.0 * math.pi)
            pb_jaw.rotation_euler = (0.16 * p, 0, 0)
            pb_lip_up.location = (0, -0.006 * p, 0)
        elif 66 < f <= 76: # "...am I?" (A -> I)
            p = math.sin((f - 66) / 10.0 * math.pi)
            pb_jaw.rotation_euler = (0.19 * p, 0, 0)
            pb_c_l.location = (0.005 * p, 0, 0.004 * p)
            pb_c_r.location = (-0.005 * p, 0, 0.004 * p)
        else:
            pb_jaw.rotation_euler = (0.01, 0, 0)

    bake_clip("Action_Dialogue_Intro_WhereAmI", 84, key_dialogue_intro)

    # CLIP 3: Action_Dialogue_Thinking_Spire (78 frames = 2.6 sec)
    # Spoken: "Is this the gateway of the Hundredfold Spire?"
    def key_dialogue_thinking(f, t):
        reset_pose()
        # Asymmetrical thinking eyebrows
        think_p = min(1.0, f / 15.0)
        pb_brow_l.location = (0, 0, 0.012 * think_p) # high cocked
        pb_brow_r.location = (0, 0, -0.005 * think_p) # furrowed

        # Syllables
        pulse = abs(math.sin(f * 0.35)) * 0.14
        pb_jaw.rotation_euler = (pulse, 0, 0)
        pb_c_l.location = (0.005 * think_p, 0, 0.003 * think_p) # slight smirk

    bake_clip("Action_Dialogue_Thinking_Spire", 78, key_dialogue_thinking)

    # CLIP 4: Action_Dialogue_Determined_Conquer (75 frames = 2.5 sec)
    # Spoken: "I'm ready. Let's conquer this world!"
    def key_dialogue_determined(f, t):
        reset_pose()
        # Confident focused brows
        pb_brow_l.location = (-0.003, 0, -0.004)
        pb_brow_r.location = (0.003, 0, -0.004)

        if f <= 55:
            pulse = abs(math.sin(f * 0.40)) * 0.16
            pb_jaw.rotation_euler = (pulse, 0, 0)
        else: # Wide determined smile finish!
            p = min(1.0, (f - 55) / 15.0)
            pb_jaw.rotation_euler = (0.04 * p, 0, 0)
            pb_c_l.location = (0.008 * p, 0, 0.006 * p)
            pb_c_r.location = (-0.008 * p, 0, 0.006 * p)

    bake_clip("Action_Dialogue_Determined_Conquer", 75, key_dialogue_determined)

    # CLIP 5: Action_Expression_Joy_Smile (30 frames loop)
    def key_joy_smile(f, t):
        reset_pose()
        breath = math.sin(t * math.pi * 2) * 0.001
        pb_brow_l.location = (0, 0, 0.005 + breath)
        pb_brow_r.location = (0, 0, 0.005 + breath)
        pb_jaw.rotation_euler = (0.03, 0, 0)
        pb_c_l.location = (0.008, 0, 0.006 + breath)
        pb_c_r.location = (-0.008, 0, 0.006 + breath)

    bake_clip("Action_Expression_Joy_Smile", 30, key_joy_smile)

    # 8. Save Blend File for inspection
    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print(f"✦ Saved full Blender project file to: {out_blend}")

    # 9. Export to FBX with all actions and shape keys
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.export_scene.fbx(
        filepath=out_fbx,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=True,
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X'
    )
    print(f"✦ Successfully exported FBX to: {out_fbx}")
    print(f"==================================================")

if __name__ == "__main__":
    gender = "male"
    if "--gender" in sys.argv:
        idx = sys.argv.index("--gender")
        if idx + 1 < len(sys.argv):
            gender = sys.argv[idx + 1]
    run_rigging(gender)

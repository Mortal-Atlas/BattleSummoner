import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix

def build_pristine_rigged_character(gender="female"):
    is_female = (gender.lower() == "female")
    project_root = r"d:/Games/BattleSummoner/BattleSummoner"
    
    in_orig_fbx = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_Genshin.fbx")
    base_blend = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_MouthRigged.blend")
    out_blend = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_MouthRigged.blend")
    out_fbx = os.path.join(project_root, f"Assets/Models/Characters/Character_Base_{'Female' if is_female else 'Male'}_BlenderMouthRigged.fbx")

    print(f"\n==================================================")
    print(f"✦ BUILDING PRISTINE EYE & MOUTH RIG FOR {gender.upper()} ✦")
    print(f"==================================================")

    # 1. Open current blend file
    bpy.ops.wm.open_mainfile(filepath=base_blend)
    
    armature = bpy.data.objects.get('Character_Armature')
    mesh_obj = bpy.data.objects.get('Character_Mesh')
    
    if not armature or not mesh_obj:
        raise RuntimeError("Armature or Mesh not found in base blend!")

    # Head is already cleanly spliced and welded in Character_Base_Female_MouthRigged.blend!
    print(f"✦ Using clean anime mesh with {len(mesh_obj.data.vertices)} vertices.")

    # 2. Add Professional Armature Bones: Eye Rig + Mouth Rig
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode='EDIT')
    eb = armature.data.edit_bones
    head_eb = eb.get('Head') or eb.get('head')
    if not head_eb:
        raise RuntimeError("Head bone not found!")

    # In armature coordinates:
    # Male: Mouth center Y=-0.105, Z=1.575, Eyes Y=-0.130, Z=1.625
    # Female: Mouth center Y=-0.119, Z=1.531, Eyes Y=-0.144, Z=1.585
    mouth_z = 1.531 if is_female else 1.575
    mouth_y = -0.119 if is_female else -0.105
    jaw_pivot_z = mouth_z - 0.015
    jaw_pivot_y = mouth_y + 0.065
    chin_z = mouth_z - 0.055
    chin_y = mouth_y - 0.015
    brow_z = 1.635 if is_female else 1.675
    brow_y = mouth_y - 0.035
    eye_z = 1.585 if is_female else 1.625
    eye_y = mouth_y - 0.025
    eye_x = 0.038

    def add_bone(name, parent, head_pos, tail_pos):
        existing = eb.get(name)
        if existing: eb.remove(existing)
        b = eb.new(name)
        b.parent = parent
        b.head = head_pos
        b.tail = tail_pos
        return b

    # Facial Roots
    face_root = add_bone("Face_Rig_Master", head_eb, (0.0, mouth_y + 0.02, mouth_z), (0.0, mouth_y, mouth_z))
    jaw_master = add_bone("Jaw_Master", head_eb, (0.0, jaw_pivot_y, jaw_pivot_z), (0.0, chin_y, chin_z))

    # Mouth Bones
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

    # Dedicated Eye Rig Bones
    # Eye aim target master bone (floating ahead in front of face)
    eye_target = add_bone("Eye_Target_Master", head_eb, (0.0, eye_y - 0.35, eye_z), (0.0, eye_y - 0.45, eye_z))
    add_bone("Eye_Target_Left", eye_target, (eye_x, eye_y - 0.35, eye_z), (eye_x, eye_y - 0.45, eye_z))
    add_bone("Eye_Target_Right", eye_target, (-eye_x, eye_y - 0.35, eye_z), (-eye_x, eye_y - 0.45, eye_z))

    # Left & Right Eye pivot bones
    add_bone("Eye_Left", head_eb, (eye_x, eye_y + 0.015, eye_z), (eye_x, eye_y - 0.020, eye_z))
    add_bone("Eye_Right", head_eb, (-eye_x, eye_y + 0.015, eye_z), (-eye_x, eye_y - 0.020, eye_z))

    # Eyelid Bones (Upper & Lower)
    add_bone("Eyelid_Upper_Left", head_eb, (eye_x, eye_y, eye_z + 0.014), (eye_x + 0.008, eye_y - 0.008, eye_z + 0.014))
    add_bone("Eyelid_Upper_Right", head_eb, (-eye_x, eye_y, eye_z + 0.014), (-eye_x - 0.008, eye_y - 0.008, eye_z + 0.014))
    add_bone("Eyelid_Lower_Left", head_eb, (eye_x, eye_y, eye_z - 0.014), (eye_x + 0.008, eye_y - 0.008, eye_z - 0.014))
    add_bone("Eyelid_Lower_Right", head_eb, (-eye_x, eye_y, eye_z - 0.014), (-eye_x - 0.008, eye_y - 0.008, eye_z - 0.014))

    # Eyebrows
    add_bone("Brow_Left", head_eb, (eye_x, brow_y, brow_z), (eye_x + 0.020, brow_y + 0.005, brow_z + 0.005))
    add_bone("Brow_Right", head_eb, (-eye_x, brow_y, brow_z), (-eye_x - 0.020, brow_y + 0.005, brow_z + 0.005))

    bpy.ops.object.mode_set(mode='OBJECT')
    print(f"✦ Added full Eye & Mouth armature bones (22 facial bones total)!")

    # 3. Paint smooth vertex weights for facial & eye bones
    bpy.context.view_layer.objects.active = mesh_obj
    mat_world = mesh_obj.matrix_world
    verts = mesh_obj.data.vertices

    # Ensure all head vertices belong to Head group
    vg_head = mesh_obj.vertex_groups.get('Head')
    if not vg_head: vg_head = mesh_obj.vertex_groups.new(name='Head')
    head_indices = [i for i, v in enumerate(verts) if v.co.y > 1.390]
    vg_head.add(head_indices, 1.0, 'ADD')

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
        vg = mesh_obj.vertex_groups.get(bname)
        if not vg: vg = mesh_obj.vertex_groups.new(name=bname)
        cx, cy, cz = center
        r_sq = radius * radius
        for i in head_indices:
            v = verts[i]
            w_co = mat_world @ v.co
            dx = w_co.x - cx
            dy = w_co.y - cy
            dz = w_co.z - cz
            dist_sq = dx*dx + dy*dy + dz*dz
            if dist_sq < r_sq:
                dist = math.sqrt(dist_sq)
                w = 0.5 * (1.0 + math.cos(math.pi * dist / radius))
                vg.add([i], w, 'REPLACE')

    print("✦ Baked smooth vertex groups for all 18 facial & eye landmarks!")

    # 4. Build Professional Animation Standard Shape Keys
    if mesh_obj.data.shape_keys:
        mesh_obj.shape_key_clear()

    sk_basis = mesh_obj.shape_key_add(name="Basis", from_mix=False)
    
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
        "Eye_Blink_L", "Eye_Blink_R", "Eye_Wide_L", "Eye_Wide_R",
        "Eye_Look_L", "Eye_Look_R", "Eye_Look_Up", "Eye_Look_Down",
        "Eye_Squint_L", "Eye_Squint_R",
        "Brow_Raise_L", "Brow_Raise_R", "Brow_Angry_L", "Brow_Angry_R", "Brow_Sad_L", "Brow_Sad_R"
    ]

    r_mouth = 0.055
    r_brow = 0.045
    r_eye = 0.038

    for sk_name in shape_key_defs:
        sk = mesh_obj.shape_key_add(name=sk_name, from_mix=False)
        for i in head_indices:
            v = verts[i]
            lx, ly, lz = v.co.x, v.co.y, v.co.z
            
            # Mouth calculations
            dm = math.sqrt(lx*lx + (ly - loc_mouth_y)**2 + (lz - loc_mouth_z)**2)
            if dm < r_mouth and any(k in sk_name for k in ["Vowel", "Mouth"]):
                w = 0.5 * (1.0 + math.cos(math.pi * dm / r_mouth))
                sign_x = 1.0 if lx >= 0 else -1.0
                rel_y = ly - loc_mouth_y

                dx, dy, dz = 0.0, 0.0, 0.0
                if sk_name == "Vowel_A":
                    dy = -0.014 * w if rel_y <= 0.004 else 0.002 * w
                    dz = -0.004 * w
                elif sk_name == "Vowel_I":
                    dx = sign_x * 0.012 * w
                    dy = 0.004 * w if rel_y >= 0 else -0.002 * w
                elif sk_name == "Vowel_U":
                    dz = 0.010 * w
                    dx = -sign_x * 0.007 * w
                elif sk_name == "Vowel_E":
                    dx = sign_x * 0.008 * w
                    dy = -0.008 * w if rel_y <= 0 else 0.002 * w
                elif sk_name == "Vowel_O":
                    dy = -0.016 * w if rel_y <= 0 else 0.002 * w
                    dx = -sign_x * 0.006 * w
                    dz = 0.005 * w
                elif sk_name == "Mouth_Smile":
                    dx = sign_x * 0.008 * w
                    dy = 0.009 * w
                elif sk_name == "Mouth_Frown":
                    dy = -0.009 * w
                elif sk_name == "Mouth_Open":
                    dy = -0.018 * w if rel_y <= 0 else 0.0
                elif sk_name == "Mouth_Pout":
                    if rel_y <= 0: dz = 0.008 * w; dy = 0.004 * w
                elif sk_name == "Mouth_Smirk_L":
                    if lx > 0: dx = 0.009 * w; dy = 0.010 * w
                elif sk_name == "Mouth_Smirk_R":
                    if lx < 0: dx = -0.009 * w; dy = 0.010 * w

                if dx != 0.0 or dy != 0.0 or dz != 0.0:
                    sk.data[i].co = (lx + dx, ly + dy, lz + dz)

            # Eye calculations
            de_l = math.sqrt((lx - eye_x)**2 + (ly - loc_eye_y)**2 + (lz - loc_eye_z)**2)
            de_r = math.sqrt((lx + eye_x)**2 + (ly - loc_eye_y)**2 + (lz - loc_eye_z)**2)
            if "Eye" in sk_name:
                if "L" in sk_name and de_l < r_eye:
                    we = 0.5 * (1.0 + math.cos(math.pi * de_l / r_eye))
                    if sk_name == "Eye_Blink_L" and ly >= loc_eye_y - 0.005:
                        sk.data[i].co.y -= 0.014 * we
                    elif sk_name == "Eye_Wide_L":
                        sk.data[i].co.y += 0.006 * we
                    elif sk_name == "Eye_Squint_L":
                        sk.data[i].co.y -= 0.007 * we
                    elif sk_name == "Eye_Look_L":
                        sk.data[i].co.x += 0.006 * we
                    elif sk_name == "Eye_Look_R":
                        sk.data[i].co.x -= 0.006 * we
                    elif sk_name == "Eye_Look_Up":
                        sk.data[i].co.y += 0.005 * we
                    elif sk_name == "Eye_Look_Down":
                        sk.data[i].co.y -= 0.005 * we
                elif "R" in sk_name and de_r < r_eye:
                    we = 0.5 * (1.0 + math.cos(math.pi * de_r / r_eye))
                    if sk_name == "Eye_Blink_R" and ly >= loc_eye_y - 0.005:
                        sk.data[i].co.y -= 0.014 * we
                    elif sk_name == "Eye_Wide_R":
                        sk.data[i].co.y += 0.006 * we
                    elif sk_name == "Eye_Squint_R":
                        sk.data[i].co.y -= 0.007 * we
                    elif sk_name == "Eye_Look_L":
                        sk.data[i].co.x += 0.006 * we
                    elif sk_name == "Eye_Look_R":
                        sk.data[i].co.x -= 0.006 * we
                    elif sk_name == "Eye_Look_Up":
                        sk.data[i].co.y += 0.005 * we
                    elif sk_name == "Eye_Look_Down":
                        sk.data[i].co.y -= 0.005 * we

            # Brow calculations
            db_l = math.sqrt((lx - eye_x)**2 + (ly - loc_brow_y)**2 + (lz - loc_brow_z)**2)
            db_r = math.sqrt((lx + eye_x)**2 + (ly - loc_brow_y)**2 + (lz - loc_brow_z)**2)
            if "Brow" in sk_name:
                if "L" in sk_name and db_l < r_brow:
                    wb = 0.5 * (1.0 + math.cos(math.pi * db_l / r_brow))
                    if sk_name == "Brow_Raise_L": sk.data[i].co.y += 0.014 * wb
                    elif sk_name == "Brow_Angry_L": sk.data[i].co.y -= 0.009 * wb; sk.data[i].co.x -= 0.006 * wb
                    elif sk_name == "Brow_Sad_L": sk.data[i].co.y += 0.010 * wb; sk.data[i].co.x += 0.004 * wb
                elif "R" in sk_name and db_r < r_brow:
                    wb = 0.5 * (1.0 + math.cos(math.pi * db_r / r_brow))
                    if sk_name == "Brow_Raise_R": sk.data[i].co.y += 0.014 * wb
                    elif sk_name == "Brow_Angry_R": sk.data[i].co.y -= 0.009 * wb; sk.data[i].co.x += 0.006 * wb
                    elif sk_name == "Brow_Sad_R": sk.data[i].co.y += 0.010 * wb; sk.data[i].co.x -= 0.004 * wb

    print(f"✦ Baked {len(mesh_obj.data.shape_keys.key_blocks)} industry standard shape keys!")

    # 5. Create Pose Constraints & NLA Baked Animation Clips
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode='POSE')
    armature.animation_data_create()

    # Bone references
    pb_jaw = armature.pose.bones.get('Jaw_Master')
    pb_lip_up = armature.pose.bones.get('Lip_Upper_Mid')
    pb_lip_low = armature.pose.bones.get('Lip_Lower_Mid')
    pb_c_l = armature.pose.bones.get('Mouth_Corner_Left')
    pb_c_r = armature.pose.bones.get('Mouth_Corner_Right')
    pb_brow_l = armature.pose.bones.get('Brow_Left')
    pb_brow_r = armature.pose.bones.get('Brow_Right')
    pb_eye_l = armature.pose.bones.get('Eye_Left')
    pb_eye_r = armature.pose.bones.get('Eye_Right')
    pb_eye_target = armature.pose.bones.get('Eye_Target_Master')

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
            for pb in [pb_jaw, pb_lip_up, pb_lip_low, pb_c_l, pb_c_r, pb_brow_l, pb_brow_r, pb_eye_l, pb_eye_r, pb_eye_target]:
                if pb:
                    pb.keyframe_insert(data_path="rotation_euler", frame=f)
                    pb.keyframe_insert(data_path="location", frame=f)

        track = armature.animation_data.nla_tracks.new()
        track.name = name
        track.strips.new(name, 1, act)
        print(f"Baked NLA Clip '{name}' ({num_frames} frames)")

    # CLIP 1: Phonetic Vowels Cycle
    def key_vowels(f, t):
        reset_pose()
        if f <= 12:
            p = (f - 1) / 11.0
            pb_jaw.rotation_euler = (0.22 * p, 0, 0)
            pb_lip_low.location = (0, 0, -0.008 * p)
        elif f <= 24:
            p = (f - 12) / 12.0
            pb_jaw.rotation_euler = (math.radians(12.6)*(1-p) + math.radians(4.5)*p, 0, 0)
            pb_c_l.location = (0.008*p, 0, 0.004*p)
            pb_c_r.location = (-0.008*p, 0, 0.004*p)
        elif f <= 36:
            p = (f - 24) / 12.0
            pb_jaw.rotation_euler = (math.radians(4.5)*(1-p) + math.radians(5.1)*p, 0, 0)
            pb_c_l.location = (0.008*(1-p) - 0.005*p, 0, 0.004*(1-p))
            pb_c_r.location = (-0.008*(1-p) + 0.005*p, 0, 0.004*(1-p))
            pb_lip_up.location = (0, -0.008*p, 0)
        elif f <= 48:
            p = (f - 36) / 12.0
            pb_jaw.rotation_euler = (math.radians(5.1)*(1-p) + math.radians(8.0)*p, 0, 0)
            pb_c_l.location = (-0.005*(1-p) + 0.005*p, 0, 0)
            pb_c_r.location = (0.005*(1-p) - 0.005*p, 0, 0)
        elif f <= 56:
            p = (f - 48) / 8.0
            pb_jaw.rotation_euler = (math.radians(8.0)*(1-p) + math.radians(11.5)*p, 0, 0)
            pb_c_l.location = (0.005*(1-p) - 0.004*p, 0, 0)
            pb_c_r.location = (-0.005*(1-p) + 0.004*p, 0, 0)
        else:
            p = (f - 56) / 4.0
            pb_jaw.rotation_euler = (math.radians(11.5)*(1-p), 0, 0)
    bake_clip("Action_Phonetic_Vowels_Cycle", 60, key_vowels)

    # CLIP 2: Eye Look & Natural Darting Cycle (60 frames loop)
    def key_eyes_darting(f, t):
        reset_pose()
        if f <= 20: # Look left
            p = min(1.0, f / 12.0)
            pb_eye_l.rotation_euler = (0, 0, 0.25 * p)
            pb_eye_r.rotation_euler = (0, 0, 0.25 * p)
        elif f <= 40: # Dart right
            p = (f - 20) / 15.0
            rot = 0.25 * (1.0 - p) - 0.25 * p
            pb_eye_l.rotation_euler = (0, 0, rot)
            pb_eye_r.rotation_euler = (0, 0, rot)
        else: # Return center
            p = (f - 40) / 20.0
            rot = -0.25 * (1.0 - p)
            pb_eye_l.rotation_euler = (0, 0, rot)
            pb_eye_r.rotation_euler = (0, 0, rot)
    bake_clip("Action_Facial_EyeBlink_LookAround", 60, key_eyes_darting)

    # CLIP 3: Dialogue Intro Spoken
    def key_dialogue_intro(f, t):
        reset_pose()
        wonder_w = min(1.0, f / 18.0)
        pb_brow_l.location = (0, 0, 0.009 * wonder_w)
        pb_brow_r.location = (0, 0, 0.009 * wonder_w)
        pulse = abs(math.sin(f * 0.35)) * 0.14
        pb_jaw.rotation_euler = (pulse, 0, 0)
        pb_c_l.location = (0.004 * wonder_w, 0, 0.002 * wonder_w)
        pb_c_r.location = (-0.004 * wonder_w, 0, 0.002 * wonder_w)
    bake_clip("Action_Dialogue_Intro_WhereAmI", 84, key_dialogue_intro)

    # CLIP 4: Joy Smile
    def key_joy_smile(f, t):
        reset_pose()
        breath = math.sin(t * math.pi * 2) * 0.001
        pb_brow_l.location = (0, 0, 0.005 + breath)
        pb_brow_r.location = (0, 0, 0.005 + breath)
        pb_jaw.rotation_euler = (0.03, 0, 0)
        pb_c_l.location = (0.008, 0, 0.006 + breath)
        pb_c_r.location = (-0.008, 0, 0.006 + breath)
    bake_clip("Action_Expression_Joy_Smile", 30, key_joy_smile)

    # 6. Save Blend File
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print(f"✦ Saved full Blender project file to: {out_blend}")

    # 7. Export FBX
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
    print(f"✦ Successfully exported clean FBX to: {out_fbx}")
    print(f"==================================================")

if __name__ == "__main__":
    import sys
    gender = "female"
    if "--gender" in sys.argv:
        idx = sys.argv.index("--gender")
        if idx + 1 < len(sys.argv):
            gender = sys.argv[idx + 1]
    build_pristine_rigged_character(gender)

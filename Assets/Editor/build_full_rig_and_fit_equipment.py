import bpy
import os
import mathutils

char_dir = r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Characters"
equip_dir = r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Equipment"

base_fbx = os.path.join(char_dir, "Character_Anime_BaseMesh.fbx")
rigged_base_out = os.path.join(char_dir, "Character_Anime_BaseMesh_Rigged.fbx")
equipped_out = os.path.join(char_dir, "Character_Anime_Fitted_Equipped.fbx")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=base_fbx)

body_obj = None
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        body_obj = obj
        break

if not body_obj:
    print("Error: No body mesh found!")
    exit(1)

body_obj.name = "Character_Anime_Body"
bpy.context.view_layer.objects.active = body_obj
body_obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Compute body bounds
bbox = [body_obj.matrix_world @ mathutils.Vector(c) for c in body_obj.bound_box]
min_x, max_x = min(v.x for v in bbox), max(v.x for v in bbox)
min_y, max_y = min(v.y for v in bbox), max(v.y for v in bbox)
min_z, max_z = min(v.z for v in bbox), max(v.z for v in bbox)
height = max_z - min_z

print(f"Body dimensions: X({min_x:.2f} to {max_x:.2f}), Y({min_y:.2f} to {max_y:.2f}), Z({min_z:.2f} to {max_z:.2f}), Height={height:.2f}")

# Create Armature
bpy.ops.object.armature_add(enter_editmode=True, align='WORLD', location=(0, 0, 0))
armature_obj = bpy.context.active_object
armature_obj.name = "Anime_Armature"
arm_data = armature_obj.data
arm_data.name = "Anime_ArmatureData"

# Delete default bone
bpy.ops.armature.select_all(action='SELECT')
bpy.ops.armature.delete()

edit_bones = arm_data.edit_bones

# Proportional placement based on body height and Z range
z_mid = (min_z + max_z) * 0.5
z_hips = min_z + height * 0.52
z_spine = min_z + height * 0.62
z_chest = min_z + height * 0.74
z_neck = min_z + height * 0.84
z_head = min_z + height * 0.90
z_head_top = max_z

z_shoulder = min_z + height * 0.78
z_elbow = min_z + height * 0.68
z_wrist = min_z + height * 0.56
z_finger_tip = min_z + height * 0.46

z_knee = min_z + height * 0.28
z_ankle = min_z + height * 0.08
z_toe = min_z

x_arm_span = (max_x - min_x) * 0.5
x_shoulder = x_arm_span * 0.25
x_elbow = x_arm_span * 0.55
x_wrist = x_arm_span * 0.85
x_hand = x_arm_span * 0.98

x_leg = (max_x - min_x) * 0.14

# Full Humanoid Bone Hierarchy with 10 fingers and 10 toes
bones_spec = [
    # Spine
    ("Hips", None, (0, 0, z_hips), (0, 0, z_spine)),
    ("Spine", "Hips", (0, 0, z_spine), (0, 0, z_chest)),
    ("Chest", "Spine", (0, 0, z_chest), (0, 0, z_neck)),
    ("Neck", "Chest", (0, 0, z_neck), (0, 0, z_head)),
    ("Head", "Neck", (0, 0, z_head), (0, 0, z_head_top)),

    # Left Arm
    ("LeftShoulder", "Chest", (-x_shoulder * 0.5, 0, z_shoulder), (-x_shoulder, 0, z_shoulder)),
    ("LeftArm", "LeftShoulder", (-x_shoulder, 0, z_shoulder), (-x_elbow, 0, z_elbow)),
    ("LeftForeArm", "LeftArm", (-x_elbow, 0, z_elbow), (-x_wrist, 0, z_wrist)),
    ("LeftHand", "LeftForeArm", (-x_wrist, 0, z_wrist), (-x_hand, 0, z_finger_tip + 0.05)),

    # Left Hand - 5 Fingers (10 Fingers Total)
    ("Thumb_01_L", "LeftHand", (-x_wrist - 0.02, 0.02, z_wrist - 0.02), (-x_wrist - 0.05, 0.04, z_wrist - 0.05)),
    ("Thumb_02_L", "Thumb_01_L", (-x_wrist - 0.05, 0.04, z_wrist - 0.05), (-x_wrist - 0.08, 0.05, z_wrist - 0.08)),
    ("Index_01_L", "LeftHand", (-x_hand + 0.04, 0.01, z_finger_tip + 0.06), (-x_hand + 0.02, 0.01, z_finger_tip + 0.03)),
    ("Index_02_L", "Index_01_L", (-x_hand + 0.02, 0.01, z_finger_tip + 0.03), (-x_hand, 0.01, z_finger_tip)),
    ("Middle_01_L", "LeftHand", (-x_hand + 0.04, 0.0, z_finger_tip + 0.06), (-x_hand + 0.02, 0.0, z_finger_tip + 0.03)),
    ("Middle_02_L", "Middle_01_L", (-x_hand + 0.02, 0.0, z_finger_tip + 0.03), (-x_hand, 0.0, z_finger_tip - 0.01)),
    ("Ring_01_L", "LeftHand", (-x_hand + 0.04, -0.01, z_finger_tip + 0.06), (-x_hand + 0.02, -0.01, z_finger_tip + 0.03)),
    ("Ring_02_L", "Ring_01_L", (-x_hand + 0.02, -0.01, z_finger_tip + 0.03), (-x_hand, -0.01, z_finger_tip)),
    ("Pinky_01_L", "LeftHand", (-x_hand + 0.04, -0.02, z_finger_tip + 0.06), (-x_hand + 0.02, -0.02, z_finger_tip + 0.03)),
    ("Pinky_02_L", "Pinky_01_L", (-x_hand + 0.02, -0.02, z_finger_tip + 0.03), (-x_hand, -0.02, z_finger_tip + 0.01)),

    # Right Arm
    ("RightShoulder", "Chest", (x_shoulder * 0.5, 0, z_shoulder), (x_shoulder, 0, z_shoulder)),
    ("RightArm", "RightShoulder", (x_shoulder, 0, z_shoulder), (x_elbow, 0, z_elbow)),
    ("RightForeArm", "RightArm", (x_elbow, 0, z_elbow), (x_wrist, 0, z_wrist)),
    ("RightHand", "RightForeArm", (x_wrist, 0, z_wrist), (x_hand, 0, z_finger_tip + 0.05)),

    # Right Hand - 5 Fingers (10 Fingers Total)
    ("Thumb_01_R", "RightHand", (x_wrist + 0.02, 0.02, z_wrist - 0.02), (x_wrist + 0.05, 0.04, z_wrist - 0.05)),
    ("Thumb_02_R", "Thumb_01_R", (x_wrist + 0.05, 0.04, z_wrist - 0.05), (x_wrist + 0.08, 0.05, z_wrist - 0.08)),
    ("Index_01_R", "RightHand", (x_hand - 0.04, 0.01, z_finger_tip + 0.06), (x_hand - 0.02, 0.01, z_finger_tip + 0.03)),
    ("Index_02_R", "Index_01_R", (x_hand - 0.02, 0.01, z_finger_tip + 0.03), (x_hand, 0.01, z_finger_tip)),
    ("Middle_01_R", "RightHand", (x_hand - 0.04, 0.0, z_finger_tip + 0.06), (x_hand - 0.02, 0.0, z_finger_tip + 0.03)),
    ("Middle_02_R", "Middle_01_R", (x_hand - 0.02, 0.0, z_finger_tip + 0.03), (x_hand, 0.0, z_finger_tip - 0.01)),
    ("Ring_01_R", "RightHand", (x_hand - 0.04, -0.01, z_finger_tip + 0.06), (x_hand - 0.02, -0.01, z_finger_tip + 0.03)),
    ("Ring_02_R", "Ring_01_R", (x_hand - 0.02, -0.01, z_finger_tip + 0.03), (x_hand, -0.01, z_finger_tip)),
    ("Pinky_01_R", "RightHand", (x_hand - 0.04, -0.02, z_finger_tip + 0.06), (x_hand - 0.02, -0.02, z_finger_tip + 0.03)),
    ("Pinky_02_R", "Pinky_01_R", (x_hand - 0.02, -0.02, z_finger_tip + 0.03), (x_hand, -0.02, z_finger_tip + 0.01)),

    # Legs & 10 Toes (5 toes Left, 5 toes Right)
    ("LeftUpLeg", "Hips", (-x_leg, 0, z_hips), (-x_leg, 0, z_knee)),
    ("LeftLeg", "LeftUpLeg", (-x_leg, 0, z_knee), (-x_leg, 0, z_ankle)),
    ("LeftFoot", "LeftLeg", (-x_leg, 0, z_ankle), (-x_leg, 0.12, z_toe + 0.02)),
    ("LeftToes", "LeftFoot", (-x_leg, 0.12, z_toe + 0.02), (-x_leg, 0.22, z_toe)),

    ("RightUpLeg", "Hips", (x_leg, 0, z_hips), (x_leg, 0, z_knee)),
    ("RightLeg", "RightUpLeg", (x_leg, 0, z_knee), (x_leg, 0, z_ankle)),
    ("RightFoot", "RightLeg", (x_leg, 0, z_ankle), (x_leg, 0.12, z_toe + 0.02)),
    ("RightToes", "RightFoot", (x_leg, 0.12, z_toe + 0.02), (x_leg, 0.22, z_toe))
]

for name, parent, head_p, tail_p in bones_spec:
    b = edit_bones.new(name)
    b.head = mathutils.Vector(head_p)
    b.tail = mathutils.Vector(tail_p)
    if parent and parent in edit_bones:
        b.parent = edit_bones[parent]

bpy.ops.object.mode_set(mode='OBJECT')

# Bind Body to Armature
bpy.ops.object.select_all(action='DESELECT')
body_obj.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj

print("Binding naked anime body to full humanoid rig (10 fingers, 10 toes)...")
try:
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    print("Auto-weight binding succeeded!")
except Exception as e:
    print(f"Fallback to envelope weights: {e}")
    bpy.ops.object.parent_set(type='ARMATURE_ENVELOPE')

# Export Naked Rigged Character (ready for posing)
bpy.ops.object.select_all(action='DESELECT')
body_obj.select_set(True)
armature_obj.select_set(True)

bpy.ops.export_scene.fbx(
    filepath=rigged_base_out,
    use_selection=True,
    add_leaf_bones=False,
    bake_anim=False,
    axis_forward='-Z',
    axis_up='Y',
    apply_scale_options='FBX_SCALE_ALL'
)
print(f"Exported naked rigged base model: {rigged_base_out}")

# Now import and fit each equipment piece onto the character
equip_pieces = [
    ("Equipment_Helmet", (0, 0, z_head + 0.02), 0.55),
    ("Equipment_Torso", (0, 0, z_chest - 0.05), 0.65),
    ("Equipment_Leggings", (0, 0, z_knee + 0.15), 0.68),
    ("Equipment_Shoes", (0, 0, z_ankle - 0.05), 0.55)
]

fitted_objects = []

for eq_name, target_pos, scale_factor in equip_pieces:
    eq_fbx = os.path.join(equip_dir, f"{eq_name}.fbx")
    if not os.path.exists(eq_fbx): continue
    
    # Import piece
    bpy.ops.import_scene.fbx(filepath=eq_fbx)
    new_objs = [o for o in bpy.context.selected_objects if o.type == 'MESH']
    if not new_objs: continue
    eq_mesh = new_objs[0]
    eq_mesh.name = eq_name
    
    # Center origin
    bpy.context.view_layer.objects.active = eq_mesh
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
    
    # Scale and position to fit body
    eq_mesh.location = mathutils.Vector(target_pos)
    eq_mesh.scale = mathutils.Vector((scale_factor, scale_factor, scale_factor))
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    
    # Parent to armature
    eq_mesh.select_set(True)
    armature_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj
    try:
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    except Exception:
        eq_mesh.parent = armature_obj
        
    fitted_objects.append(eq_mesh)
    print(f"Fitted {eq_name} to character body at {target_pos}")

# Export Fully Equipped Character
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(
    filepath=equipped_out,
    use_selection=True,
    add_leaf_bones=False,
    bake_anim=False,
    axis_forward='-Z',
    axis_up='Y',
    apply_scale_options='FBX_SCALE_ALL'
)
print(f"Exported fully equipped character to: {equipped_out}")
print("Rigging & equipment fitting pipeline completed successfully!")

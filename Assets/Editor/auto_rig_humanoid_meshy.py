import bpy
import os
import mathutils

fbx_in = r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Meshy\Character_MeshyHero.fbx"
output_folder = r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Meshy"
fbx_rigged_out = os.path.join(output_folder, "Character_MeshyHero_Rigged.fbx")

# Reset factory settings
bpy.ops.wm.read_factory_settings(use_empty=True)

# Import FBX
bpy.ops.import_scene.fbx(filepath=fbx_in)

mesh_obj = None
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        mesh_obj = obj
        break

if not mesh_obj:
    print("Error: No mesh found in FBX!")
    exit(1)

# Ensure transforms are applied
bpy.context.view_layer.objects.active = mesh_obj
mesh_obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Create Armature
bpy.ops.object.armature_add(enter_editmode=True, align='WORLD', location=(0, 0, 0))
armature_obj = bpy.context.active_object
armature_obj.name = "Character_Armature"
arm_data = armature_obj.data
arm_data.name = "Character_ArmatureData"

# Clear default bone
bpy.ops.armature.select_all(action='SELECT')
bpy.ops.armature.delete()

# Define standard Humanoid / Mixamo bone layout
bone_definitions = [
    # name, parent, head (x,y,z), tail (x,y,z)
    ("Hips", None, (0.0, 0.0, -0.05), (0.0, 0.0, 0.18)),
    ("Spine", "Hips", (0.0, 0.0, 0.18), (0.0, 0.0, 0.38)),
    ("Spine1", "Spine", (0.0, 0.0, 0.38), (0.0, 0.0, 0.58)),
    ("Neck", "Spine1", (0.0, 0.0, 0.58), (0.0, 0.0, 0.72)),
    ("Head", "Neck", (0.0, 0.0, 0.72), (0.0, 0.0, 0.95)),

    # Left Arm
    ("LeftShoulder", "Spine1", (-0.08, 0.0, 0.55), (-0.22, 0.0, 0.50)),
    ("LeftArm", "LeftShoulder", (-0.22, 0.0, 0.50), (-0.46, 0.0, 0.44)),
    ("LeftForeArm", "LeftArm", (-0.46, 0.0, 0.44), (-0.68, 0.0, 0.38)),
    ("LeftHand", "LeftForeArm", (-0.68, 0.0, 0.38), (-0.84, 0.0, 0.34)),

    # Right Arm
    ("RightShoulder", "Spine1", (0.08, 0.0, 0.55), (0.22, 0.0, 0.50)),
    ("RightArm", "RightShoulder", (0.22, 0.0, 0.50), (0.46, 0.0, 0.44)),
    ("RightForeArm", "RightArm", (0.46, 0.0, 0.44), (0.68, 0.0, 0.38)),
    ("RightHand", "RightForeArm", (0.68, 0.0, 0.38), (0.84, 0.0, 0.34)),

    # Left Leg
    ("LeftUpLeg", "Hips", (-0.18, 0.0, -0.05), (-0.18, 0.0, -0.48)),
    ("LeftLeg", "LeftUpLeg", (-0.18, 0.0, -0.48), (-0.18, 0.0, -0.85)),
    ("LeftFoot", "LeftLeg", (-0.18, 0.0, -0.85), (-0.18, 0.20, -0.95)),

    # Right Leg
    ("RightUpLeg", "Hips", (0.18, 0.0, -0.05), (0.18, 0.0, -0.48)),
    ("RightLeg", "RightUpLeg", (0.18, 0.0, -0.48), (0.18, 0.0, -0.85)),
    ("RightFoot", "RightLeg", (0.18, 0.0, -0.85), (0.18, 0.20, -0.95)),
]

edit_bones = arm_data.edit_bones

for name, parent_name, head_pos, tail_pos in bone_definitions:
    bone = edit_bones.new(name)
    bone.head = mathutils.Vector(head_pos)
    bone.tail = mathutils.Vector(tail_pos)
    if parent_name and parent_name in edit_bones:
        bone.parent = edit_bones[parent_name]

# Return to object mode
bpy.ops.object.mode_set(mode='OBJECT')

# Bind Mesh to Armature with Automatic Weights
bpy.ops.object.select_all(action='DESELECT')
mesh_obj.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj

print("Binding mesh to humanoid armature with automatic weights...")
try:
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    print("Automatic weight binding succeeded!")
except Exception as e:
    print(f"Automatic weighting warning: {e}. Falling back to envelope weights...")
    bpy.ops.object.parent_set(type='ARMATURE_ENVELOPE')

# Ensure material and texture linking
if mesh_obj.data.materials:
    mat = mesh_obj.data.materials[0]
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    tex_path = os.path.join(output_folder, "Image_0.png")
    if bsdf and os.path.exists(tex_path):
        img_node = mat.node_tree.nodes.new('ShaderNodeTexImage')
        img_node.image = bpy.data.images.load(tex_path)
        mat.node_tree.links.new(img_node.outputs['Color'], bsdf.inputs['Base Color'])

# Export rigged FBX
bpy.ops.object.select_all(action='DESELECT')
mesh_obj.select_set(True)
armature_obj.select_set(True)

print(f"Exporting rigged character to {fbx_rigged_out}...")
bpy.ops.export_scene.fbx(
    filepath=fbx_rigged_out,
    use_selection=True,
    add_leaf_bones=False,
    bake_anim=False,
    axis_forward='-Z',
    axis_up='Y',
    apply_scale_options='FBX_SCALE_ALL'
)

print("Rigged character FBX export complete!")

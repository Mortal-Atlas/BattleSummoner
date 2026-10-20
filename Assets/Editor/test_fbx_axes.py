import bpy
from mathutils import Matrix
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
glb_path = f"{project_root}/Assets/Models/Characters/Starter/femalestarter.glb"
out_test_z = f"{project_root}/Assets/Models/Characters/Starter/test_forward_Z.fbx"
out_test_neg_z = f"{project_root}/Assets/Models/Characters/Starter/test_forward_negZ.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=glb_path)
mesh = [o for o in bpy.data.objects if o.type == 'MESH'][0]

# glb_path mesh natively faces +Y.
# Let's export with axis_forward='Z', axis_up='Y'
bpy.ops.export_scene.fbx(filepath=out_test_z, axis_forward='Z', axis_up='Y')

# And export with axis_forward='-Z', axis_up='Y'
bpy.ops.export_scene.fbx(filepath=out_test_neg_z, axis_forward='-Z', axis_up='Y')

print("Exported both test FBXs.")

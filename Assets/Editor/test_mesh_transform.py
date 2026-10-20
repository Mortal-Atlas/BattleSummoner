import bpy
import math
from mathutils import Matrix

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
glb_path = "Assets/Models/Characters/Starter/femalestarter.glb"
full_path = f"{project_root}/{glb_path}"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=full_path)

mesh = [o for o in bpy.data.objects if o.type == 'MESH'][0]

print("BEFORE rotation:")
verts_head = [v.co for v in mesh.data.vertices if v.co.z > 0.75]
print(f"Y range: min={min(v.y for v in verts_head):.4f}, max={max(v.y for v in verts_head):.4f}, avg={sum(v.y for v in verts_head)/len(verts_head):.4f}")

# Rotate directly using Matrix
rot_180_z = Matrix.Rotation(math.pi, 4, 'Z')
mesh.data.transform(rot_180_z)

print("AFTER mesh.data.transform(rot_180_z):")
verts_head_after = [v.co for v in mesh.data.vertices if v.co.z > 0.75]
print(f"Y range: min={min(v.y for v in verts_head_after):.4f}, max={max(v.y for v in verts_head_after):.4f}, avg={sum(v.y for v in verts_head_after)/len(verts_head_after):.4f}")

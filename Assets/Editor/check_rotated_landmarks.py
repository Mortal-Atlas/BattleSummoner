import bpy
from mathutils import Matrix, Vector
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
models = [
    ("Male", "Assets/Models/Characters/Character_Base_Male_MouthRigged.blend", -0.105, 1.575),
    ("Female", "Assets/Models/Characters/Character_Base_Female_MouthRigged.blend", -0.119, 1.531)
]

for gender, rel_path, mouth_y, mouth_z in models:
    full_path = os.path.join(project_root, rel_path)
    bpy.ops.wm.open_mainfile(filepath=full_path)
    mesh = bpy.data.objects.get("Character_Mesh")
    
    print(f"\n==========================================")
    print(f"CHECKING {gender.upper()} WITH 180 DEG MESH ROTATION:")
    print(f"==========================================")
    
    # Invert mesh in world space by 180 around Z: (x, y) -> (-x, -y)
    # The mouth landmark in build_pristine_character_rigs.py was at (0, mouth_y, mouth_z)
    # If the mesh vertices are rotated 180 around Z:
    # A vertex originally at (x, y, z) becomes (-x, -y, z).
    # Since original mouth was at avg Y = +0.0558 (Male) or +0.0272 (Female),
    # after rotating 180 around Z, it will be at avg Y = -0.0558 or -0.0272!
    
    verts = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
    # Rotate them 180 around Z
    verts_rot = [Vector((-v.x, -v.y, v.z)) for v in verts]
    
    # Check nearest vertices to mouth bone (0, mouth_y, mouth_z)
    b_mouth = Vector((0.0, mouth_y, mouth_z))
    dists = [(v - b_mouth).length for v in verts_rot]
    min_dist = min(dists)
    print(f"Distance from mouth landmark {b_mouth} to nearest rotated mesh vertex: {min_dist:.4f} m ({min_dist*100:.2f} cm)")
    
    # Check nearest vertices to eye landmark (0.038, mouth_y - 0.025, mouth_z + 0.050)
    eye_y = mouth_y - 0.025
    eye_z = mouth_z + 0.050
    b_eye = Vector((0.038, eye_y, eye_z))
    dists_eye = [(v - b_eye).length for v in verts_rot]
    min_dist_eye = min(dists_eye)
    print(f"Distance from eye landmark {b_eye} to nearest rotated mesh vertex: {min_dist_eye:.4f} m ({min_dist_eye*100:.2f} cm)")

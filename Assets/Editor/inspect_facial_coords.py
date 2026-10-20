import bpy
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
models = [
    ("Male", "Assets/Models/Characters/Character_Base_Male_MouthRigged.blend"),
    ("Female", "Assets/Models/Characters/Character_Base_Female_MouthRigged.blend")
]

for gender, rel_path in models:
    full_path = os.path.join(project_root, rel_path)
    bpy.ops.wm.open_mainfile(filepath=full_path)
    mesh = bpy.data.objects.get("Character_Mesh")
    arm = bpy.data.objects.get("Character_Armature")
    
    print(f"\n==========================================")
    print(f"COORDINATES FOR {gender.upper()}:")
    print(f"==========================================")
    
    # Mesh vertices in world space
    mat = mesh.matrix_world
    verts = [mat @ v.co for v in mesh.data.vertices]
    
    # Let's find vertices in mouth area (centerline, Z around 1.5-1.6 for male, 1.45-1.55 for female)
    z_mouth_approx = 1.58 if gender == "Male" else 1.53
    mouth_verts = [v for v in verts if abs(v.x) < 0.02 and abs(v.z - z_mouth_approx) < 0.04 and v.y > 0.0]
    if mouth_verts:
        avg_x = sum(v.x for v in mouth_verts)/len(mouth_verts)
        avg_y = sum(v.y for v in mouth_verts)/len(mouth_verts)
        avg_z = sum(v.z for v in mouth_verts)/len(mouth_verts)
        print(f"Mouth center vertices: count={len(mouth_verts)}, avg=({avg_x:.4f}, {avg_y:.4f}, {avg_z:.4f})")
        
    z_eye_approx = 1.63 if gender == "Male" else 1.58
    eye_verts = [v for v in verts if 0.02 < v.x < 0.06 and abs(v.z - z_eye_approx) < 0.04 and v.y > 0.0]
    if eye_verts:
        avg_x = sum(v.x for v in eye_verts)/len(eye_verts)
        avg_y = sum(v.y for v in eye_verts)/len(eye_verts)
        avg_z = sum(v.z for v in eye_verts)/len(eye_verts)
        print(f"Left eye area: count={len(eye_verts)}, avg=({avg_x:.4f}, {avg_y:.4f}, {avg_z:.4f})")
        
    # Check feet in world space
    min_z = min(v.z for v in verts)
    feet_verts = [v for v in verts if v.z < min_z + 0.1]
    if feet_verts:
        print(f"Feet bounds in world space: Y=[{min(v.y for v in feet_verts):.3f}, {max(v.y for v in feet_verts):.3f}] (positive Y is toes, negative Y is heel!)")

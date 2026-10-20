import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
fbx_path = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Metropolis\Building_CathedralGothic_OpenDoors.fbx"
bpy.ops.import_scene.fbx(filepath=fbx_path)

print("=== IMPORTED CATHEDRAL OBJECTS ===")
for obj in bpy.context.scene.objects:
    print(f"Object: {obj.name}, type={obj.type}, dim={obj.dimensions}, loc={obj.location}, rot={obj.rotation_euler}")
    if obj.type == 'MESH':
        print(f"  Materials: {[m.name for m in obj.data.materials if m]}")
        print(f"  Verts: {len(obj.data.vertices)}")

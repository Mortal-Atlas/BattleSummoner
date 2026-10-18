import bpy
import sys

fbx_path = r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Meshy\Character_MeshyHero.fbx"

# Clear existing objects
bpy.ops.wm.read_factory_settings(use_empty=True)

# Import FBX
bpy.ops.import_scene.fbx(filepath=fbx_path)

print("\n=== MESHY MODEL INSPECTION ===")
for obj in bpy.data.objects:
    print(f"Object: {obj.name} | Type: {obj.type} | Loc: {obj.location} | Dimensions: {obj.dimensions}")
    if obj.type == 'MESH':
        print(f"  Vertices: {len(obj.data.vertices)} | Polygons: {len(obj.data.polygons)}")
        print(f"  Materials: {[m.name for m in obj.data.materials if m]}")
        print(f"  Vertex groups: {[vg.name for vg in obj.vertex_groups]}")
    elif obj.type == 'ARMATURE':
        print(f"  Bones ({len(obj.data.bones)}): {[b.name for b in obj.data.bones]}")

print("=== END INSPECTION ===\n")

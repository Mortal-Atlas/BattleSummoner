import bpy
import mathutils
from mathutils.bvhtree import BVHTree
import math

terrain_obj = bpy.data.objects.get("Circular_Continent_Terrain")
depsgraph = bpy.context.evaluated_depsgraph_get()
eval_terrain = terrain_obj.evaluated_get(depsgraph)
bvh = BVHTree.FromObject(eval_terrain, depsgraph)

def get_terrain_z(x, y):
    hit = bvh.ray_cast(mathutils.Vector((x, y, 280.0)), mathutils.Vector((0, 0, -1)))
    return hit[0].z if hit[0] else None

print("=== CHECKING GLACIAL GORGE PROFILE (Y = 180 to 260) ===")
for test_y in [180, 200, 220, 240]:
    print(f"\nProfile at Y = {test_y}:")
    for test_x in [-25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25]:
        tz = get_terrain_z(test_x, test_y)
        print(f"  x={test_x:3d}: z={tz:.2f}" if tz else f"  x={test_x:3d}: None")

print("\n=== CHECKING SEAWARD CANAL PROFILE ===")
for test_x in [170, 180, 190, 200, 210, 220, 230, 240, 250]:
    tz1 = get_terrain_z(test_x, -60)
    tz2 = get_terrain_z(test_x, -85)
    print(f"x={test_x:3d} | Y=-60: z={tz1:.2f} | Y=-85: z={tz2:.2f}")

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

print("=== DETAILED TERRAIN HEIGHT PROFILE FOR BRIDGES ===")

bridges = [obj for obj in bpy.data.objects if "bridge" in obj.name.lower()]
for b in bridges:
    mw = b.matrix_world
    loc, rot, sc = mw.decompose()
    dim = b.dimensions
    print(f"\n=============================")
    print(f"Bridge: {b.name}")
    print(f"Current World Pos: ({loc.x:.2f}, {loc.y:.2f}, {loc.z:.2f})")
    print(f"Dimensions: length={dim.x:.2f}, width={dim.y:.2f}, height={dim.z:.2f}")
    
    # Check terrain around the bridge in a cross section
    center_z = get_terrain_z(loc.x, loc.y)
    print(f"Terrain directly at center: {center_z}")
    
    # Sample points along the bridge line
    rot_mat = rot.to_matrix()
    axis = mathutils.Vector((1, 0, 0)) if dim.x > dim.y else mathutils.Vector((0, 1, 0))
    dir_vec = rot_mat @ axis
    dir_vec.z = 0
    dir_vec.normalize()
    
    length = max(dim.x, dim.y)
    print(f"Alignment vector: ({dir_vec.x:.3f}, {dir_vec.y:.3f})")
    for t_step in [-1.0, -0.75, -0.5, -0.25, 0.0, 0.25, 0.5, 0.75, 1.0]:
        dist = t_step * (length * 0.5)
        px = loc.x + dir_vec.x * dist
        py = loc.y + dir_vec.y * dist
        tz = get_terrain_z(px, py)
        print(f"  t={t_step:+.2f} ({px:6.1f}, {py:6.1f}): terrain_z={tz}")

print("\n=== CITADEL GATES AND RADIAL VIADUCTS ===")
for i in range(1, 7):
    gh = bpy.data.objects.get(f"Citadel_Gatehouse_0{i}")
    vb = bpy.data.objects.get(f"Citadel_Viaduct_Bridge_0{i}")
    if gh and vb:
        gh_w = gh.matrix_world.translation
        vb_w = vb.matrix_world.translation
        print(f"Gate {i}: pos=({gh_w.x:.1f}, {gh_w.y:.1f}, {gh_w.z:.1f}) | Viaduct pos=({vb_w.x:.1f}, {vb_w.y:.1f}, {vb_w.z:.1f})")

import bpy
import mathutils
from mathutils.bvhtree import BVHTree

# Load the blend
terrain_obj = bpy.data.objects.get("Circular_Continent_Terrain")
print("Terrain Obj:", terrain_obj)

# Create a BVH tree for raycasting onto the terrain
if terrain_obj:
    # Ensure mesh has normals/eval
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eval_terrain = terrain_obj.evaluated_get(depsgraph)
    bvh = BVHTree.FromObject(eval_terrain, depsgraph)
    print("BVHTree built successfully for terrain.")
else:
    bvh = None

bridges = [obj for obj in bpy.data.objects if "bridge" in obj.name.lower()]
print(f"\n--- CHECKING {len(bridges)} BRIDGES AGAINST TERRAIN ---")
for b in bridges:
    loc = b.location
    dim = b.dimensions
    rot = b.rotation_euler
    print(f"\nBridge: {b.name}")
    print(f"  Transform: loc=({loc.x:.1f}, {loc.y:.1f}, {loc.z:.1f}), dim=({dim.x:.1f}, {dim.y:.1f}, {dim.z:.1f}), rot_z={rot.z:.2f}")
    
    if bvh:
        # Check raycast at center, start, and end
        # We can test rays straight down from z=200
        center_ray = bvh.ray_cast(mathutils.Vector((loc.x, loc.y, 250.0)), mathutils.Vector((0, 0, -1)))
        if center_ray[0]:
            print(f"  Terrain under bridge center: z={center_ray[0].z:.2f} (bridge z={loc.z:.2f}, diff={loc.z - center_ray[0].z:.2f})")
        else:
            print("  No terrain directly under center!")

        # Calculate rough endpoints along the bridge's primary axis
        # Assuming dimension x or y is length
        length = max(dim.x, dim.y)
        axis = mathutils.Vector((1, 0, 0)) if dim.x > dim.y else mathutils.Vector((0, 1, 0))
        rot_mat = rot.to_matrix()
        world_axis = rot_mat @ axis
        world_axis.normalize()

        p1 = loc + world_axis * (length * 0.45)
        p2 = loc - world_axis * (length * 0.45)

        ray1 = bvh.ray_cast(mathutils.Vector((p1.x, p1.y, 250.0)), mathutils.Vector((0, 0, -1)))
        ray2 = bvh.ray_cast(mathutils.Vector((p2.x, p2.y, 250.0)), mathutils.Vector((0, 0, -1)))

        t1_z = ray1[0].z if ray1[0] else "None"
        t2_z = ray2[0].z if ray2[0] else "None"
        print(f"  End 1: pos=({p1.x:.1f}, {p1.y:.1f}) -> terrain_z={t1_z}")
        print(f"  End 2: pos=({p2.x:.1f}, {p2.y:.1f}) -> terrain_z={t2_z}")

print("\n--- CHECKING CENTRAL MOAT & CITADEL ELEVATION ---")
# Raycast citadel center and moat ring (radius ~70)
center_hit = bvh.ray_cast(mathutils.Vector((0, 0, 250.0)), mathutils.Vector((0, 0, -1)))
print("Citadel center terrain z:", center_hit[0].z if center_hit[0] else "None")

for angle_deg in [0, 60, 120, 180, 240, 300]:
    rad = mathutils.math.radians(angle_deg)
    rx = 70.0 * mathutils.math.cos(rad)
    ry = 70.0 * mathutils.math.sin(rad)
    hit = bvh.ray_cast(mathutils.Vector((rx, ry, 250.0)), mathutils.Vector((0, 0, -1)))
    print(f"Moat r=70 @ {angle_deg} deg ({rx:.1f}, {ry:.1f}): terrain_z={hit[0].z if hit[0] else 'None'}")

for angle_deg in [0, 60, 120, 180, 240, 300]:
    rad = mathutils.math.radians(angle_deg)
    rx = 110.0 * mathutils.math.cos(rad)
    ry = 110.0 * mathutils.math.sin(rad)
    hit = bvh.ray_cast(mathutils.Vector((rx, ry, 250.0)), mathutils.Vector((0, 0, -1)))
    print(f"Outer Bank r=110 @ {angle_deg} deg ({rx:.1f}, {ry:.1f}): terrain_z={hit[0].z if hit[0] else 'None'}")

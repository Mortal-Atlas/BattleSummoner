"""
Comprehensive Blender Script: Refine Circular Continent
1. Replaces center building with Meshy Gothic Cathedral (Building_CathedralGothic_OpenDoors.fbx).
2. Fixes all bridge placements (elevations, spans, alignments, viaduct pillars, and ascending ramps).
3. Populates detailed townhouses, shops, and district architecture across concentric citadel tiers & Seaward Dockcity.
4. Removes old placeholder tree trunks/canopies (Foliage_Canopy_Root).
5. Ensures volcano vertex colors are 100% rock/obsidian (0% grass) and refines splat map.
6. Saves .blend file and exports updated .fbx.
"""

import bpy
import bmesh
import math
import mathutils
from mathutils.bvhtree import BVHTree
import os

print("=== STARTING BLENDER CONTINENT REFINEMENT ===")

# 1. Setup BVH for raycasting on current terrain
terrain_obj = bpy.data.objects.get("Circular_Continent_Terrain")
if not terrain_obj:
    raise Exception("Circular_Continent_Terrain not found!")

depsgraph = bpy.context.evaluated_depsgraph_get()
eval_terrain = terrain_obj.evaluated_get(depsgraph)
bvh = BVHTree.FromObject(eval_terrain, depsgraph)

def get_terrain_z(x, y, default_z=16.0):
    hit = bvh.ray_cast(mathutils.Vector((x, y, 320.0)), mathutils.Vector((0, 0, -1)))
    return hit[0].z if hit[0] else default_z

# 2. Materials
m_citadel_stone = bpy.data.materials.get("M_Citadel_Stone")
m_citadel_roof = bpy.data.materials.get("M_Citadel_Roof")
m_wood_timber = bpy.data.materials.get("M_Wood_Timber")
m_road_cobble = bpy.data.materials.get("M_Road_Cobble")

# 3. Clean up old placeholder trees and old citadel blocks
print("Cleaning up old placeholder foliage and old citadel center objects...")
old_foliage_root = bpy.data.objects.get("Foliage_Canopy_Root")
if old_foliage_root:
    for child in list(old_foliage_root.children):
        bpy.data.objects.remove(child, do_unlink=True)
    bpy.data.objects.remove(old_foliage_root, do_unlink=True)

# Also remove any stray Tree_ objects
stray_trees = [o for o in bpy.data.objects if o.name.startswith("Tree_South") or o.name.startswith("Tree_Temperate") or o.name.startswith("Tree_Glacial")]
for t in stray_trees:
    bpy.data.objects.remove(t, do_unlink=True)

# Remove old center keep and spires
old_center_objs = [
    "Citadel_Palace_GrandKeep",
    "Citadel_Gothic_Master_Spire",
    "Citadel_Turret_Spire_1",
    "Citadel_Turret_Spire_2",
    "Citadel_Turret_Spire_3",
    "Citadel_Turret_Spire_4"
]
for name in old_center_objs:
    obj = bpy.data.objects.get(name)
    if obj:
        bpy.data.objects.remove(obj, do_unlink=True)

# Remove old basic citadel houses (Citadel_House_*) to make way for detailed gabled architectures
old_houses = [o for o in bpy.data.objects if o.name.startswith("Citadel_House_")]
for h in old_houses:
    bpy.data.objects.remove(h, do_unlink=True)

citadel_root = bpy.data.objects.get("Concentric_Citadel_Root")
if not citadel_root:
    citadel_root = bpy.data.objects.new("Concentric_Citadel_Root", None)
    bpy.context.collection.objects.link(citadel_root)

# 4. Import the Meshy Cathedral
cathedral_fbx_path = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Metropolis\Building_CathedralGothic_OpenDoors.fbx"
print(f"Importing Meshy Cathedral from: {cathedral_fbx_path}")
if os.path.exists(cathedral_fbx_path):
    # Import into a temporary collection
    bpy.ops.import_scene.fbx(filepath=cathedral_fbx_path)
    # The imported object is usually named Cathedral_Mesh or similar
    imported_objs = [o for o in bpy.context.selected_objects]
    cathedral_mesh_obj = None
    for o in imported_objs:
        if "Cathedral" in o.name or "Mesh" in o.name:
            cathedral_mesh_obj = o
            break
    if not cathedral_mesh_obj and imported_objs:
        cathedral_mesh_obj = imported_objs[0]

    if cathedral_mesh_obj:
        cathedral_mesh_obj.name = "Citadel_Meshy_GrandCathedral"
        cathedral_mesh_obj.parent = citadel_root
        # Center in citadel plaza: Citadel floor is at z = 16.0
        # Check mesh bounds to sit flush on z = 16.0
        min_z = min(v.co.z for v in cathedral_mesh_obj.data.vertices)
        cathedral_mesh_obj.location = (0.0, 0.0, 16.0 - min_z)
        cathedral_mesh_obj.rotation_euler = (0.0, 0.0, 0.0) # Face south promenade
        cathedral_mesh_obj.scale = (1.0, 1.0, 1.0)
        print(f"Cathedral placed at location {cathedral_mesh_obj.location} with dimensions {cathedral_mesh_obj.dimensions}")
else:
    print(f"WARNING: Cathedral FBX not found at {cathedral_fbx_path}")

# 5. Fix All Bridge Placements
print("\n--- FIXING ALL BRIDGES ---")

# A. Citadel Viaduct Bridges 01..06
# Gatehouses are at radius 68.0, deck elevation = 16.5m
# Outer moat rim is at radius 104.0..108.0m
viaduct_angles = [90.0, 150.0, 210.0, 270.0, 330.0, 30.0]
for idx, ang_d in enumerate(viaduct_angles):
    b_name = f"Citadel_Viaduct_Bridge_0{idx+1}"
    v_bridge = bpy.data.objects.get(b_name)
    rad = math.radians(ang_d)
    ca, sa = math.cos(rad), math.sin(rad)
    
    # Gate inner point (r = 68)
    p_inner = mathutils.Vector((68.0 * ca, 68.0 * sa, 16.5))
    # Outer bank landing point (r = 108)
    p_outer_xy = mathutils.Vector((108.0 * ca, 108.0 * sa))
    outer_tz = get_terrain_z(p_outer_xy.x, p_outer_xy.y)
    
    # For Viaduct 03 (SW into Highland Terraces), the terrain is elevated (28m+)
    # We ramp it smoothly from 16.5 to outer_tz
    p_outer = mathutils.Vector((p_outer_xy.x, p_outer_xy.y, max(14.0, outer_tz)))
    
    center_pos = (p_inner + p_outer) * 0.5
    span_len = (p_outer - p_inner).length
    incline_angle = math.atan2(p_outer.z - p_inner.z, math.hypot(p_outer.x - p_inner.x, p_outer.y - p_inner.y))
    
    if not v_bridge:
        bpy.ops.mesh.primitive_cube_add(size=1.0)
        v_bridge = bpy.context.active_object
        v_bridge.name = b_name
        v_bridge.parent = citadel_root
        if m_citadel_stone:
            v_bridge.data.materials.append(m_citadel_stone)
            
    # Set bridge mesh transform
    v_bridge.location = center_pos
    v_bridge.scale = (span_len + 4.0, 8.5, 3.5) # generous length to embed into gate & bank
    v_bridge.rotation_euler = (0.0, -incline_angle, rad)
    print(f"Fixed {b_name}: center={center_pos}, span={span_len:.1f}, incline={math.degrees(incline_angle):.1f} deg")

    # Update or create 3 viaduct support pillars reaching the moat bed (Z ~ 4.5m)
    for frac, r_sub in [(0.25, 78.0), (0.50, 88.0), (0.75, 98.0)]:
        pil_name = f"Viaduct_Pillar_{idx+1:02d}_{int(r_sub)}"
        pillar = bpy.data.objects.get(pil_name)
        px, py = r_sub * ca, r_sub * sa
        top_z = p_inner.z + (p_outer.z - p_inner.z) * frac
        bed_z = min(top_z - 4.0, get_terrain_z(px, py, 4.5))
        p_height = top_z - bed_z + 1.0
        p_center_z = bed_z + p_height * 0.5 - 0.5
        
        if not pillar:
            bpy.ops.mesh.primitive_cube_add(size=1.0)
            pillar = bpy.context.active_object
            pillar.name = pil_name
            pillar.parent = citadel_root
            if m_citadel_stone:
                pillar.data.materials.append(m_citadel_stone)
                
        pillar.location = (px, py, p_center_z)
        pillar.scale = (5.5, 5.5, p_height)
        pillar.rotation_euler = (0, 0, rad)

# B. Bridge_Coastal_HighArch_Dockcity
b_dock = bpy.data.objects.get("Bridge_Coastal_HighArch_Dockcity")
if b_dock:
    # High arch spanning across the coastal channel from (272, -114) to (338, -146)
    p1 = mathutils.Vector((272.0, -114.0, 15.5))
    p2 = mathutils.Vector((338.0, -146.0, 19.2))
    b_center = (p1 + p2) * 0.5
    b_len = (p2 - p1).length
    b_ang = math.atan2(p2.y - p1.y, p2.x - p1.x)
    incline = math.atan2(p2.z - p1.z, math.hypot(p2.x - p1.x, p2.y - p1.y))
    
    b_dock.location = b_center
    b_dock.scale = (b_len + 4.0, 8.0, 4.5)
    b_dock.rotation_euler = (0.0, -incline, b_ang)
    print(f"Fixed Bridge_Coastal_HighArch_Dockcity: loc={b_center}, len={b_len:.1f}")

# C. Bridge_Southern_Gorge
b_sg = bpy.data.objects.get("Bridge_Southern_Gorge")
if b_sg:
    # Gorge banks: Left bank at x=-4 (z=27), Right bank at x=24 (z=21), canyon bottom z=9
    p1 = mathutils.Vector((-5.0, -220.0, 27.5))
    p2 = mathutils.Vector((25.0, -220.0, 21.0))
    b_center = (p1 + p2) * 0.5
    b_len = (p2 - p1).length
    incline = math.atan2(p2.z - p1.z, p2.x - p1.x)
    
    b_sg.location = b_center
    b_sg.scale = (b_len + 4.0, 7.5, 3.5)
    b_sg.rotation_euler = (0.0, -incline, 0.0)
    print(f"Fixed Bridge_Southern_Gorge: loc={b_center}, len={b_len:.1f}")

# D. Bridge_Glacial_Gorge
b_gg = bpy.data.objects.get("Bridge_Glacial_Gorge")
if b_gg:
    # At Y = 175, gorge channel runs north-south, river bed z ≈ 0, west bank z=14.5, east bank z=12.5
    p1 = mathutils.Vector((-16.0, 175.0, 15.0))
    p2 = mathutils.Vector((16.0, 175.0, 13.0))
    b_center = (p1 + p2) * 0.5
    b_len = (p2 - p1).length
    incline = math.atan2(p2.z - p1.z, p2.x - p1.x)
    
    b_gg.location = b_center
    b_gg.scale = (b_len + 4.0, 7.0, 3.5)
    b_gg.rotation_euler = (0.0, -incline, 0.0)
    print(f"Fixed Bridge_Glacial_Gorge: loc={b_center}, len={b_len:.1f}")

# E. Seaward Canal Bridges
b_scw = bpy.data.objects.get("Bridge_Seaward_Canal_West")
if b_scw:
    # Spans canal at y = -60 from x = 178 (z=8) to x = 206 (z=7)
    b_scw.location = (192.0, -60.0, 8.5)
    b_scw.scale = (30.0, 7.0, 2.5)
    b_scw.rotation_euler = (0.0, 0.0, 0.0)
    print("Fixed Bridge_Seaward_Canal_West")

b_sce = bpy.data.objects.get("Bridge_Seaward_Canal_East")
if b_sce:
    # Spans canal at y = -85 from x = 196 (z=11) to x = 224 (z=13)
    b_sce.location = (210.0, -85.0, 12.8)
    b_sce.scale = (30.0, 7.5, 2.5)
    b_sce.rotation_euler = (0.0, 0.0, 0.0)
    print("Fixed Bridge_Seaward_Canal_East")


# 6. Populate Houses and Architecture in Blender
print("\n--- POPULATING HOUSES AND ARCHITECTURE IN BLENDER ---")

def create_gabled_house(name, width, depth, height, roof_height, parent, loc, rot_z, mats):
    bm = bmesh.new()
    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    obj = bpy.data.objects.new(name, mesh)
    obj.parent = parent
    bpy.context.collection.objects.link(obj)
    
    hw, hd = width * 0.5, depth * 0.5
    # Base box
    v0 = bm.verts.new((-hw, -hd, 0))
    v1 = bm.verts.new((hw, -hd, 0))
    v2 = bm.verts.new((hw, hd, 0))
    v3 = bm.verts.new((-hw, hd, 0))
    
    v4 = bm.verts.new((-hw, -hd, height))
    v5 = bm.verts.new((hw, -hd, height))
    v6 = bm.verts.new((hw, hd, height))
    v7 = bm.verts.new((-hw, hd, height))
    
    # Roof ridge
    v8 = bm.verts.new((0, -hd * 1.05, height + roof_height))
    v9 = bm.verts.new((0, hd * 1.05, height + roof_height))
    
    # Walls
    bm.faces.new((v0, v1, v5, v4)) # Front
    bm.faces.new((v2, v3, v7, v6)) # Back
    bm.faces.new((v1, v2, v6, v5)) # Right
    bm.faces.new((v3, v0, v4, v7)) # Left
    bm.faces.new((v0, v3, v2, v1)) # Floor
    
    # Gables
    bm.faces.new((v4, v5, v8))
    bm.faces.new((v6, v7, v9))
    
    # Roof slopes
    bm.faces.new((v5, v6, v9, v8))
    bm.faces.new((v7, v4, v8, v9))
    
    bm.to_mesh(mesh)
    bm.free()
    
    if m_citadel_stone:
        obj.data.materials.append(m_citadel_stone)
    if m_citadel_roof:
        obj.data.materials.append(m_citadel_roof)
        
    obj.location = loc
    obj.rotation_euler = (0, 0, rot_z)
    return obj

# Populate Concentric Citadel Rings
# Tier 3 (Radius 30m - 40m): Mages' Quarter, High-End Commercial, Residential
# Avoid the 6 radial avenues (30, 90, 150, 210, 270, 330 deg)
house_idx = 1
t3_angles = []
for deg in range(0, 360, 12):
    # Check if close to an avenue (within 10 deg)
    if any(abs(deg - a) < 9 or abs(deg - a) > 351 for a in [30, 90, 150, 210, 270, 330]):
        continue
    t3_angles.append(deg)

for deg in t3_angles:
    rad = math.radians(deg)
    ca, sa = math.cos(rad), math.sin(rad)
    r = 34.0
    h_x = r * ca
    h_y = r * sa
    h_z = 16.0
    tangent_angle = rad + math.pi * 0.5
    w = 6.5
    d = 9.0
    h = 8.5
    rh = 4.5
    create_gabled_house(f"Citadel_Tier3_House_{house_idx:02d}", w, d, h, rh, citadel_root, (h_x, h_y, h_z), tangent_angle, None)
    house_idx += 1

# Tier 2 (Radius 48m - 62m): Commercial Trade Shops, Bazaars, Residential Districts
t2_angles = []
for deg in range(0, 360, 9):
    if any(abs(deg - a) < 7 or abs(deg - a) > 353 for a in [30, 90, 150, 210, 270, 330]):
        continue
    t2_angles.append(deg)

for deg in t2_angles:
    rad = math.radians(deg)
    ca, sa = math.cos(rad), math.sin(rad)
    # Inner and outer rows
    for r_offset, prefix in [(50.0, "Inner"), (58.5, "Outer")]:
        h_x = r_offset * ca
        h_y = r_offset * sa
        h_z = 16.0
        tangent_angle = rad + math.pi * 0.5
        w = 6.0
        d = 7.5
        h = 7.5
        rh = 4.0
        create_gabled_house(f"Citadel_Tier2_{prefix}_{house_idx:03d}", w, d, h, rh, citadel_root, (h_x, h_y, h_z), tangent_angle, None)
        house_idx += 1

print(f"Created {house_idx} detailed gabled buildings in Citadel!")

# Populate Seaward Dockcity with coastal houses and trade lodges
dockcity_root = bpy.data.objects.get("Seaward_Dockcity_Root")
if not dockcity_root:
    dockcity_root = bpy.data.objects.new("Seaward_Dockcity_Root", None)
    bpy.context.collection.objects.link(dockcity_root)

dock_house_idx = 1
# Dockcity grid along harbor streets
for gx in range(235, 285, 11):
    for gy in range(-185, -125, 12):
        tz = get_terrain_z(gx, gy)
        if tz > 2.0 and tz < 15.0: # On town plateau, not in sea
            w = 7.0
            d = 9.0
            h = 7.0
            rh = 3.8
            rot = (gx * 0.1 + gy * 0.15) % 1.5
            create_gabled_house(f"Dockcity_House_{dock_house_idx:02d}", w, d, h, rh, dockcity_root, (gx, gy, tz), rot, None)
            dock_house_idx += 1

print(f"Created {dock_house_idx} gabled houses in Seaward Dockcity!")

# 7. Refine Splat Vertex Colors - STRICT VOLCANO EXCLUSION
print("\n--- REFINING SPLAT VERTEX COLORS & VOLCANO EXCLUSION ---")
color_layer = terrain_obj.data.color_attributes.get("Col_BiomeSplat")
if color_layer:
    volcano_center_x = -260.0
    volcano_center_y = 270.0
    
    # We will modify vertex colors via bmesh
    bm = bmesh.new()
    bm.from_mesh(terrain_obj.data)
    bm.verts.ensure_lookup_table()
    
    color_layer = bm.loops.layers.color.get("Col_BiomeSplat")
    if color_layer:
        modified_count = 0
        for face in bm.faces:
            for loop in face.loops:
                vert = loop.vert
                x, y, z = vert.co.x, vert.co.y, vert.co.z
                dx = x - volcano_center_x
                dy = y - volcano_center_y
                dist_v = math.hypot(dx, dy)
                
                # Volcano zone: 0% Grass! 100% Rock/Obsidian
                if dist_v < 185.0:
                    # Pure Rock / Obsidian
                    loop[color_layer] = (0.0, 1.0, 0.0, 0.0) # (Grass=0, Rock=1, Sand=0, Snow=0)
                    modified_count += 1
                elif dist_v < 210.0:
                    # Transition falloff
                    t = (dist_v - 185.0) / 25.0
                    old_c = loop[color_layer]
                    loop[color_layer] = (old_c[0] * t, max(old_c[1], 1.0 - t), old_c[2], old_c[3])
                    modified_count += 1
                    
        print(f"Updated {modified_count} vertex color loops in Volcano region for 0% plant/grass!")
        bm.to_mesh(terrain_obj.data)
    bm.free()

# 8. Save updated .blend file
blend_save_path = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\CircularContinent_MacroLevel1.blend"
print(f"\nSaving updated blend file to: {blend_save_path}")
bpy.ops.wm.save_as_mainfile(filepath=blend_save_path)

# 9. Export updated FBX
fbx_export_path = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\CircularContinent_MacroLevel1.fbx"
print(f"Exporting updated FBX to: {fbx_export_path}...")
bpy.ops.export_scene.fbx(
    filepath=fbx_export_path,
    use_selection=False,
    use_active_collection=False,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',
    bake_space_transform=True,
    object_types={'MESH', 'EMPTY'},
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    add_leaf_bones=False,
    primary_bone_axis='Y',
    secondary_bone_axis='X',
    axis_forward='-Z',
    axis_up='Y'
)
print("FBX EXPORT COMPLETED SUCCESSFULLY!")

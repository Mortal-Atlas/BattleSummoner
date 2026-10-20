import bpy
import bmesh
import math
import random
import os
from mathutils import Vector, noise

# ==============================================================================
# BLENDER MCP: HIGH-RES SUBDIVIDED TERRAIN, ERODED MOUNTAINS, MEANDERING RIVER
# & WALKABLE MOUNTAIN SWITCHBACK TRAILS
# ==============================================================================

def create_pbr_mat(name, rgba, roughness):
    if name in bpy.data.materials:
        return bpy.data.materials[name]
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = roughness
    return mat

def link_to_collection(obj, target_col):
    if obj.name not in target_col.objects:
        target_col.objects.link(obj)
    if obj.name in bpy.context.scene.collection.objects:
        bpy.context.scene.collection.objects.unlink(obj)

def rebuild_highres_world_and_traversable_mountains():
    print("[Blender MCP] Starting High-Res World, Ridgeline Mountains & Meandering River Generation...")

    # 1. Purge floating rock artifacts and old coarse terrain
    for obj in list(bpy.data.objects):
        n = obj.name.lower()
        if any(k in n for k in ["macrocontinent", "macroterrain", "floating", "rock_cluster", "river_trench"]):
            bpy.data.objects.remove(obj, do_unlink=True)

    # 2. Create High-Resolution 512x512 Base Mesh (4x higher vertex density)
    WORLD_SIZE = 6400.0
    SUBDIVS = 512
    bpy.ops.mesh.primitive_grid_add(
        x_subdivisions=SUBDIVS,
        y_subdivisions=SUBDIVS,
        size=WORLD_SIZE,
        location=(0, 0, 0)
    )
    terrain = bpy.context.active_object
    terrain.name = "Floor01_MacroContinent_Terrain"
    mesh = terrain.data

    # Ensure vertex color attribute exists
    col_attr = None
    if hasattr(mesh, "color_attributes"):
        col_attr = mesh.color_attributes.new(name="Biome_Splat_RGBA", type='BYTE_COLOR', domain='POINT')

    # Define Meandering North River Control Points (S-Curve from North Glacier to Moat)
    def get_river_center_x(vy):
        return math.sin(vy * 0.0032) * 145.0 + math.cos(vy * 0.0075) * 55.0

    # Define Walkable Mountain Switchback Trail (Connects North Gate -> Alpine Pass -> Peak)
    def get_mountain_trail_distance_and_height(vx, vy):
        if vy < 540.0 or vy > 2350.0:
            return 9999.0, 0.0
        t = (vy - 540.0) / (2350.0 - 540.0)
        switchback_x = -260.0 + math.sin(t * math.pi * 4.5) * 290.0
        dist = abs(vx - switchback_x)
        trail_z = 20.0 + t * 168.0
        return dist, trail_z

    # Volcanic cone center at NW (-1450, 1550)
    volc_cx = -1450.0
    volc_cy = 1550.0

    print("[Blender MCP] Sculpting 262,144 vertices with multi-octave ridged noise, river canyon and trail...")
    for i, v in enumerate(mesh.vertices):
        vx, vy = v.co.x, v.co.y
        r = math.hypot(vx, vy)

        # A. Keep Terrain DEPRESSED inside R < 452m so it NEVER buries City Tier buildings!
        if r < 452.0:
            v.co.z = 14.0
            if col_attr:
                col_attr.data[i].color = (0.72, 0.70, 0.66, 1.0)
            continue

        # B. Concentric Moat Basin (452m <= R <= 535m)
        if r <= 535.0:
            moat_t = math.sin(((r - 452.0) / 83.0) * math.pi)
            v.co.z = 18.0 - (moat_t * 14.0)
            if col_attr:
                col_attr.data[i].color = (0.45, 0.42, 0.36, 1.0)
            continue

        # C. Base Rolling Meadow with Multi-Octave Detail (Not flat/blocky)
        p = Vector((vx * 0.0009, vy * 0.0009, 0.0))
        base_z = 20.0 + noise.multi_fractal(p, 1.0, 2.0, 4) * 9.5
        splat = [0.26, 0.54, 0.18, 1.0] # Lush Anime Grass

        # D. North & NW Ridged Multifractal Mountain Range (Replaces smooth Play-Doh blobs!)
        if vy > 620.0:
            m_mask = min(1.0, max(0.0, (vy - 620.0) / 550.0))
            m_mask = m_mask * m_mask * (3.0 - 2.0 * m_mask) # Smoothstep

            # Ridged Multifractal for sharp anime alpine ridges, gullies & crags
            rp = Vector((vx * 0.0014, vy * 0.0014, 2.5))
            ridge_1 = 1.0 - abs(noise.noise(rp))
            ridge_2 = 1.0 - abs(noise.noise(rp * 2.35)) * 0.5
            ridge_3 = 1.0 - abs(noise.noise(rp * 5.10)) * 0.25
            ridged_val = pow(max(0.0, (ridge_1 + ridge_2 + ridge_3) / 1.75), 1.65)

            mountain_z = ridged_val * 285.0 * m_mask
            base_z += mountain_z

            # Slope & Altitude Splatting (Rock on steep ridges, Snow above Z=135m)
            if base_z > 135.0 and vx > -650.0:
                snow_t = min(1.0, (base_z - 135.0) / 55.0)
                splat = [0.92 * snow_t + 0.38 * (1.0 - snow_t),
                         0.95 * snow_t + 0.41 * (1.0 - snow_t),
                         1.00 * snow_t + 0.46 * (1.0 - snow_t), 1.0]
            elif mountain_z > 32.0:
                splat = [0.38, 0.39, 0.43, 1.0] # Stratified Alpine Cliff Rock

        # D2. NW Volcanic Caldera ((-1450, 1550)): Dark basalt cone (Y = +340m) with glowing purple boss crater
        v_dist = math.hypot(vx - volc_cx, vy - volc_cy)
        if v_dist < 800.0:
            v_blend = max(0.0, 1.0 - (v_dist / 800.0))
            v_blend = v_blend * v_blend * (3.0 - 2.0 * v_blend)
            volc_h = (v_blend ** 1.35) * 320.0

            # Sunken purple crater in center
            if v_dist < 200.0:
                crater_t = max(0.0, 1.0 - (v_dist / 200.0))
                crater_t = crater_t * crater_t * (3.0 - 2.0 * crater_t)
                volc_h -= crater_t * 140.0
                splat = [0.48 * crater_t + 0.16 * (1.0 - crater_t),
                         0.14 * crater_t + 0.14 * (1.0 - crater_t),
                         0.72 * crater_t + 0.18 * (1.0 - crater_t), 1.0]
            else:
                splat = [0.18, 0.16, 0.20, 1.0] # Dark Basalt

            base_z = max(base_z, 20.0 + volc_h)

        # E. Carve Natural Meandering River Gorge Through North Mountains into Moat
        if vy > 500.0 and vy < 2450.0:
            rc_x = get_river_center_x(vy)
            r_dist = abs(vx - rc_x)
            river_half_width = 36.0 + noise.noise(Vector((vx * 0.01, vy * 0.01, 0.0))) * 10.0
            bank_feather = 68.0

            if r_dist < (river_half_width + bank_feather):
                # Smooth terraced riverbank canyon (No straight V-trench!)
                t_riv = max(0.0, min(1.0, r_dist / (river_half_width + bank_feather)))
                canyon_blend = t_riv * t_riv * (3.0 - 2.0 * t_riv)
                river_bed_z = 8.0 + ((vy - 500.0) / 1950.0) * 14.0
                base_z = river_bed_z * (1.0 - canyon_blend) + base_z * canyon_blend
                if r_dist < river_half_width + 14.0:
                    splat = [0.58, 0.50, 0.38, 1.0] # Riverbed cobble & sand

        # F. Carve Walkable 14m-Wide Switchback Mountain Trail into the Slope
        trail_d, trail_z = get_mountain_trail_distance_and_height(vx, vy)
        if trail_d < 22.0:
            t_path = max(0.0, min(1.0, (trail_d - 7.0) / 15.0))
            path_blend = t_path * t_path * (3.0 - 2.0 * t_path)
            base_z = trail_z * (1.0 - path_blend) + base_z * path_blend
            if trail_d < 8.5:
                splat = [0.55, 0.43, 0.28, 1.0] # Walkable Packed Dirt Trail

        # G. Outer Coastline Falloff at R = 2850m
        if r > 2700.0:
            coast_t = min(1.0, (r - 2700.0) / 250.0)
            base_z = base_z * (1.0 - coast_t) - 14.0 * coast_t

        v.co.z = base_z
        if col_attr:
            col_attr.data[i].color = splat

    mesh.update()
    bpy.ops.object.shade_smooth()
    print("[Blender MCP] High-Res 512x512 Terrain, Ridged Mountains, Meandering River & Switchback Trails Built.")

    # 3. Add Switchback Trail Flanking Overhangs
    build_mountain_switchback_overhangs(terrain)

def build_mountain_switchback_overhangs(terrain):
    col_name = "07_Mountain_Switchback_Cliffs_And_Pines"
    if col_name in bpy.data.collections:
        mtn_col = bpy.data.collections[col_name]
    else:
        mtn_col = bpy.data.collections.new(col_name)
        bpy.context.scene.collection.children.link(mtn_col)

    for obj in list(mtn_col.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

    mat_cliff = create_pbr_mat("MAT_Alpine_StratifiedCliff", (0.34, 0.36, 0.40, 1.0), 0.88)

    def get_switchback_x_and_z(vy):
        t = max(0.0, min(1.0, (vy - 540.0) / (2350.0 - 540.0)))
        sx = -260.0 + math.sin(t * math.pi * 4.5) * 290.0
        sz = 20.0 + t * 168.0
        return sx, sz

    rng = random.Random(20261019)
    num_segments = 48
    for s_idx in range(num_segments):
        t = s_idx / float(num_segments - 1)
        vy = 560.0 + t * (2280.0 - 560.0)
        sx, sz = get_switchback_x_and_z(vy)

        t_next = min(1.0, t + 0.01)
        vy_next = 560.0 + t_next * (2280.0 - 560.0)
        sx_next, _ = get_switchback_x_and_z(vy_next)
        tangent = Vector((sx_next - sx, vy_next - vy, 0.0)).normalized()
        perp = Vector((-tangent.y, tangent.x, 0.0))

        for side in [-1.0, 1.0]:
            flank_offset = rng.uniform(13.5, 22.0)
            ox = sx + perp.x * side * flank_offset
            oy = vy + perp.y * side * flank_offset
            oz = sz + rng.uniform(2.0, 6.5)

            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=(ox, oy, oz))
            overhang = bpy.context.active_object
            overhang.name = f"Mountain_CliffOverhang_{s_idx:02d}_{'L' if side < 0 else 'R'}"
            overhang.scale = (rng.uniform(7.0, 13.0), rng.uniform(8.0, 15.0), rng.uniform(4.0, 8.0))

            yaw = math.atan2(perp.y * (-side), perp.x * (-side))
            overhang.rotation_euler = (math.radians(rng.uniform(-10, 10)), math.radians(22.0), yaw)
            overhang.data.materials.append(mat_cliff)
            link_to_collection(overhang, mtn_col)

    print("[Blender MCP] Built 96 Stratified Cliff Overhangs flanking the mountain switchbacks.")

if __name__ == "__main__":
    rebuild_highres_world_and_traversable_mountains()
    # Save the active blend file
    bpy.ops.wm.save_mainfile()
    print("[Blender MCP] Saved main blend file successfully!")

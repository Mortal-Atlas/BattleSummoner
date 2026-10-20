import bpy
import bmesh
import math
import os
from mathutils import Vector

BLEND_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\CircularContinent_MacroLevel1.blend"
FBX_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\CircularContinent_MacroLevel1.fbx"

def s_curve(t):
    return t * t * (3.0 - 2.0 * t)

def pseudo_noise(x, y):
    n = math.sin(x * 12.9898 + y * 78.233) * 43758.5453
    return n - math.floor(n)

def smooth_noise(x, y):
    ix, iy = math.floor(x), math.floor(y)
    fx, fy = x - ix, y - iy
    fx = s_curve(fx)
    fy = s_curve(fy)
    
    n00 = pseudo_noise(ix, iy)
    n10 = pseudo_noise(ix + 1, iy)
    n01 = pseudo_noise(ix, iy + 1)
    n11 = pseudo_noise(ix + 1, iy + 1)
    
    nx0 = n00 * (1.0 - fx) + n10 * fx
    nx1 = n01 * (1.0 - fx) + n11 * fx
    return nx0 * (1.0 - fy) + nx1 * fy

def fbm(x, y, octaves=4):
    val = 0.0
    amp = 1.0
    freq = 1.0
    tot = 0.0
    for _ in range(octaves):
        val += smooth_noise(x * freq, y * freq) * amp
        tot += amp
        amp *= 0.5
        freq *= 2.0
    return val / tot

def clay_strips_vein_profile(x, y, base_z):
    """
    Simulates the clay strips sculpting tool:
    Applies stratified, directional vein-like ridges with sharp-edged stepped crests.
    """
    # Angle of vein direction with subtle domain warp
    warp = (smooth_noise(x * 0.0012, y * 0.0012) - 0.5) * 45.0
    u = (x * 0.7071 + y * 0.7071) + warp
    v = (-x * 0.7071 + y * 0.7071)
    
    # Primary clay strip ridges (small size, high frequency: ~35m to 70m strip width)
    strip_phase = (u * 0.022) % 1.0
    # Sculpted clay strip shape: flat-topped ridge with steep bevel edges
    strip_shape = math.sin(strip_phase * math.pi)
    strip_shape = math.copysign(abs(strip_shape) ** 0.65, strip_shape)
    
    # Secondary micro-vein striation (fine texture)
    micro_phase = (v * 0.045 + u * 0.015) % 1.0
    micro_shape = math.sin(micro_phase * math.pi)
    
    # Vein intensity modulated by elevation (high on mountains/volcano, zero on city/water)
    return (strip_shape * 0.75 + micro_shape * 0.25)

def evaluate_highres_world_point(wx, wz):
    """
    Evaluates continent elevation, flat terrace buffers, biomes, and clay-strips pass.
    wx: West-East (-4096m to +4096m)
    wz: South-North (-4096m to +4096m)
    """
    # 1. Domain warp for organic boundaries
    warp_x = (smooth_noise((wx + 3000.0) * 0.0011, (wz + 3000.0) * 0.0011) - 0.5) * 320.0
    warp_z = (smooth_noise((wx + 7000.0) * 0.0011, (wz + 7000.0) * 0.0011) - 0.5) * 320.0
    nx = wx + warp_x
    nz = wz + warp_z

    r_true = math.hypot(wx, wz)
    r_warped = math.hypot(nx, nz)
    angle_rad = math.atan2(nz, nx)
    angle_deg = (math.degrees(angle_rad) + 360.0) % 360.0

    # Biome Colors
    col_lush_grass    = (0.27, 0.54, 0.18, 1.0) # Anime meadow
    col_forest_grass  = (0.18, 0.42, 0.14, 1.0) # Deep woodland
    col_desert_sand   = (0.86, 0.65, 0.35, 1.0) # Warm golden sand
    col_desert_mesa   = (0.74, 0.42, 0.24, 1.0) # Rust-ochre mesa
    col_alpine_snow   = (0.92, 0.96, 1.00, 1.0) # Crisp glacier snow
    col_volcano_rock  = (0.16, 0.14, 0.19, 1.0) # Dark basalt/obsidian
    col_purple_crater = (0.52, 0.18, 0.78, 1.0) # Glowing purple caldera
    col_city_stone    = (0.75, 0.74, 0.70, 1.0) # Kingdom masonry

    # 1. CENTRAL MAGIC CITY WITH SOLID BUFFER BIT OF WALKABLE LAND
    # Tier 4: R <= 102m (Base buildings R <= 95m, +7m buffer) -> 80m
    # Tier 3: 102m < R <= 208m (Base buildings R <= 195m, +13m buffer) -> 58m
    # Tier 2: 208m < R <= 324m (Base buildings R <= 310m, +14m buffer) -> 38m
    # Tier 1: 324m < R <= 468m (Base buildings R <= 450m, +18m buffer) -> 22m
    # Moat: 468m < R <= 535m (Bed at +4m, Water at +18m)
    if r_true <= 468.0:
        if r_true <= 102.0:
            h = 80.0
        elif r_true <= 208.0:
            h = 58.0
        elif r_true <= 324.0:
            h = 38.0
        else:
            h = 22.0
        return h, col_city_stone, 0.0, 0.0, 0.0, 1.0

    if r_true <= 535.0:
        moat_t = math.sin(((r_true - 468.0) / 67.0) * math.pi)
        h = 22.0 - (moat_t * 18.0)
        return h, col_city_stone, 0.0, 0.0, 0.0, 1.0

    # 2. CONTINENT COASTLINE & SE SHIPPING BAY / OFFSHORE ISLAND
    max_coast_r = 2650.0 + (smooth_noise(wx * 0.0008 + 10.0, wz * 0.0008 + 10.0) - 0.5) * 480.0
    se_bay_dist = math.hypot(nx - 1180.0, nz - (-820.0))
    se_island_dist = math.hypot(nx - 1680.0, nz - (-1180.0))

    if se_island_dist < 440.0:
        falloff = max(0.0, 1.0 - (se_island_dist / 440.0))
        falloff = s_curve(falloff)
        h = 3.0 + falloff * 24.0
        col = (
            col_desert_sand[0] * (1.0 - falloff) + col_lush_grass[0] * falloff,
            col_desert_sand[1] * (1.0 - falloff) + col_lush_grass[1] * falloff,
            col_desert_sand[2] * (1.0 - falloff) + col_lush_grass[2] * falloff,
            1.0
        )
        return h, col, falloff, 1.0 - falloff, 0.0, 0.0

    if r_true > max_coast_r or (se_bay_dist < 360.0 and se_island_dist >= 440.0):
        return -14.0, col_desert_sand, 0.0, 1.0, 0.0, 0.0

    # Base Rolling Meadow Elevation
    coast_fade = max(0.0, min(1.0, (max_coast_r - r_true) / 350.0))
    base_h = 20.0 + (smooth_noise((wx + 1000.0) * 0.0018, (wz + 1000.0) * 0.0018) * 16.0)
    vert_col = col_lush_grass
    w_grass = 1.0
    w_sand = 0.0
    w_snow = 0.0
    w_rock = 0.0

    # 3. TOP-LEFT (NW): MAGIC VOLCANIC VOLCANIA & PURPLE CALDERA (NEVER WHITE SNOW!)
    volc_center = (-1250.0, 1350.0)
    volc_dist = math.hypot(nx - volc_center[0], nz - volc_center[1])
    if volc_dist < 850.0:
        v_blend = max(0.0, min(1.0, 1.0 - (volc_dist / 850.0)))
        v_blend = s_curve(v_blend)
        ridge = abs(smooth_noise(wx * 0.0045, wz * 0.0045) - 0.5) * 2.0
        cone_h = (v_blend ** 1.45) * 315.0 + (ridge * 60.0 * v_blend)

        true_volc_dist = math.hypot(wx - volc_center[0], wz - volc_center[1])
        crater_blend = 0.0
        if true_volc_dist < 220.0:
            crater_blend = max(0.0, min(1.0, 1.0 - (true_volc_dist / 220.0)))
            crater_blend = s_curve(crater_blend)
            cone_h -= crater_blend * 160.0

        base_h += cone_h
        # Dark Obsidian/Basalt blended to glowing Purple in crater center
        target_vcol = (
            col_volcano_rock[0] * (1.0 - crater_blend) + col_purple_crater[0] * crater_blend,
            col_volcano_rock[1] * (1.0 - crater_blend) + col_purple_crater[1] * crater_blend,
            col_volcano_rock[2] * (1.0 - crater_blend) + col_purple_crater[2] * crater_blend,
            1.0
        )
        col_mix = min(1.0, v_blend * 1.6)
        vert_col = (
            vert_col[0] * (1.0 - col_mix) + target_vcol[0] * col_mix,
            vert_col[1] * (1.0 - col_mix) + target_vcol[1] * col_mix,
            vert_col[2] * (1.0 - col_mix) + target_vcol[2] * col_mix,
            1.0
        )
        w_rock = min(1.0, v_blend * 1.5)
        w_grass = max(0.0, 1.0 - w_rock)

    # 4. TOP-CENTER (N): FROSTHOLD WASTES (SNOW & ALPINE GLACIERS)
    # Angle ~52 deg to ~124 deg, R > 850m -> Crisp White Snow & Glacier!
    north_mask = 0.0
    if 45.0 <= angle_deg <= 130.0 and r_warped > 850.0:
        a_mid = 88.0
        a_half = 40.0
        a_diff = abs(angle_deg - a_mid)
        a_weight = max(0.0, 1.0 - (a_diff / a_half))
        r_weight = max(0.0, min(1.0, (r_warped - 850.0) / 450.0))
        north_mask = s_curve(a_weight) * s_curve(r_weight)
        
    if north_mask > 0.01:
        alpine_peaks = (north_mask ** 1.3) * 240.0 * smooth_noise(wx * 0.0028 + 50.0, wz * 0.0028 + 50.0)
        base_h += alpine_peaks
        vert_col = (
            vert_col[0] * (1.0 - north_mask) + col_alpine_snow[0] * north_mask,
            vert_col[1] * (1.0 - north_mask) + col_alpine_snow[1] * north_mask,
            vert_col[2] * (1.0 - north_mask) + col_alpine_snow[2] * north_mask,
            1.0
        )
        w_snow = north_mask
        w_grass = max(0.0, 1.0 - w_snow - w_rock)

    # 5. LEFT CRESCENT (W / SW): THE RUSTING DESOLATION (GOLDEN SAND & MESAS)
    # Angle 150 deg to 245 deg, R > 620m (NO STRAIGHT VERTICAL SEAM!)
    west_mask = 0.0
    if 148.0 <= angle_deg <= 248.0 and r_warped > 620.0:
        a_mid = 198.0
        a_half = 50.0
        a_diff = abs(angle_deg - a_mid)
        a_weight = max(0.0, 1.0 - (a_diff / a_half))
        r_weight = max(0.0, min(1.0, (r_warped - 620.0) / 400.0))
        west_mask = s_curve(a_weight) * s_curve(r_weight)

    if west_mask > 0.01:
        dunes = math.sin(wx * 0.022 + smooth_noise(wx * 0.004, wz * 0.004) * 4.0) * 14.0
        mesa_n = smooth_noise((wx + 400.0) * 0.0022, (wz - 200.0) * 0.0022)
        mesa_boost = s_curve(max(0.0, (mesa_n - 0.60) / 0.15)) * 44.0 if mesa_n > 0.60 else 0.0
        desert_h = 16.0 + abs(dunes) + mesa_boost
        base_h = base_h * (1.0 - west_mask) + desert_h * west_mask
        
        mesa_fac = min(1.0, mesa_boost / 44.0)
        sand_col = (
            col_desert_sand[0] * (1.0 - mesa_fac) + col_desert_mesa[0] * mesa_fac,
            col_desert_sand[1] * (1.0 - mesa_fac) + col_desert_mesa[1] * mesa_fac,
            col_desert_sand[2] * (1.0 - mesa_fac) + col_desert_mesa[2] * mesa_fac,
            1.0
        )
        vert_col = (
            vert_col[0] * (1.0 - west_mask) + sand_col[0] * west_mask,
            vert_col[1] * (1.0 - west_mask) + sand_col[1] * west_mask,
            vert_col[2] * (1.0 - west_mask) + sand_col[2] * west_mask,
            1.0
        )
        w_sand = west_mask
        w_grass = max(0.0, 1.0 - w_sand - w_snow - w_rock)

    # 6. STEPPED HIGHLANDS (NO CONCENTRIC BULLSEYE TARGET RINGS)
    plateaus = [
        ((-820.0, -680.0), 380.0, 64.0),
        ((-380.0, -1520.0), 340.0, 52.0),
        ((1680.0, -180.0), 390.0, 58.0)
    ]
    for p_center, p_rad, p_height in plateaus:
        pd = math.hypot(nx - p_center[0], nz - p_center[1])
        if pd < p_rad:
            norm = 1.0 - (pd / p_rad)
            smooth_terrace = (math.floor(norm * 3.0) + s_curve((norm * 3.0) % 1.0)) / 3.0
            base_h += smooth_terrace * p_height

    final_h = base_h * coast_fade

    # 7. CARVE RIVERS & LAKES ORGANICALLY
    # Inland Forest Lake ("Brestuoil Lake" at (1480, 520))
    ne_lake_dist = math.hypot(nx - 1480.0, nz - 520.0)
    if ne_lake_dist < 220.0:
        lake_depth = s_curve(1.0 - (ne_lake_dist / 220.0)) * 18.0
        final_h -= lake_depth
        vert_col = (
            vert_col[0] * 0.4 + col_desert_sand[0] * 0.6,
            vert_col[1] * 0.4 + col_desert_sand[1] * 0.6,
            vert_col[2] * 0.4 + col_desert_sand[2] * 0.6,
            1.0
        )

    # River Segments
    river_segments = [
        ((0.0, 2200.0), (40.0, 1200.0), 45.0),
        ((40.0, 1200.0), (0.0, 520.0), 48.0),
        ((-520.0, 80.0), (-1150.0, 60.0), 44.0),
        ((-1150.0, 60.0), (-2150.0, -380.0), 38.0),
        ((-1150.0, 60.0), (-1650.0, -620.0), 40.0),
        ((-1650.0, -620.0), (-2100.0, -1250.0), 38.0),
        ((-1650.0, -620.0), (-1350.0, -1550.0), 36.0),
        ((-60.0, -520.0), (-120.0, -1350.0), 42.0),
        ((-120.0, -1350.0), (60.0, -2350.0), 48.0),
        ((520.0, -60.0), (1050.0, -40.0), 44.0),
        ((1050.0, -40.0), (1480.0, 520.0), 36.0),
        ((1050.0, -40.0), (1180.0, -820.0), 65.0)
    ]
    meander = (smooth_noise(wx * 0.005, wz * 0.005) - 0.5) * 55.0
    px, pz = wx + meander, wz + meander

    for (ax, az), (bx, bz), width in river_segments:
        abx, abz = bx - ax, bz - az
        mag_sq = abx * abx + abz * abz
        if mag_sq > 0.001:
            t = max(0.0, min(1.0, ((px - ax) * abx + (pz - az) * abz) / mag_sq))
            proj_x = ax + t * abx
            proj_z = az + t * abz
            dist_seg = math.hypot(px - proj_x, pz - proj_z)
            if dist_seg < width:
                carve = math.cos((dist_seg / width) * math.pi * 0.5)
                final_h = min(final_h, 10.0 - carve * 14.0)
                vert_col = (
                    vert_col[0] * (1.0 - carve * 0.75) + col_desert_sand[0] * (carve * 0.75),
                    vert_col[1] * (1.0 - carve * 0.75) + col_desert_sand[1] * (carve * 0.75),
                    vert_col[2] * (1.0 - carve * 0.75) + col_desert_sand[2] * (carve * 0.75),
                    1.0
                )

    # 8. CLAY STRIPS SCULPTING PASS (SMALL SIZE, MEDIUM-TO-HIGH INTENSITY)
    # Outside city area (R > 520m), add layered organic vein-like ridges
    if r_true > 520.0 and final_h > 2.0:
        city_fade = max(0.0, min(1.0, (r_true - 520.0) / 180.0))
        vein_val = clay_strips_vein_profile(wx, wz, final_h)
        # Intensity scales with elevation: higher on ridges and peaks (up to 7.5m), moderate on meadows (2.2m)
        elev_intensity = 2.2 + min(1.0, final_h / 150.0) * 5.3
        final_h += vein_val * elev_intensity * city_fade * coast_fade

    return final_h, vert_col, w_grass, w_sand, w_snow, w_rock

def process_blend_continent():
    print(f"Opening Blend file: {BLEND_PATH}")
    bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)
    
    terrain_obj = bpy.data.objects.get("Floor01_MacroContinent_Terrain")
    if not terrain_obj:
        print("Creating new Floor01_MacroContinent_Terrain object in blend file...")
        mesh = bpy.data.meshes.new("Floor01_MacroContinent_Terrain")
        terrain_obj = bpy.data.objects.new("Floor01_MacroContinent_Terrain", mesh)
        bpy.context.scene.collection.objects.link(terrain_obj)
    else:
        print("Found Floor01_MacroContinent_Terrain. Re-generating high-resolution sculpted mesh...")
        mesh = terrain_obj.data
        mesh.clear_geometry()

    terrain_obj.location = (0.0, 0.0, 0.0)
    terrain_obj.rotation_euler = (0.0, 0.0, 0.0)
    terrain_obj.scale = (1.0, 1.0, 1.0)
    
    # We will build a clean, high-density 401x401 vertex grid (~20.5m grid spacing, 160,801 vertices)
    # Perfectly fits Unity 32-bit mesh buffers while giving 4x the density of the previous blocky terrain!
    grid_res = 401
    grid_extent = 4096.0 # -4096m to +4096m
    step = (grid_extent * 2.0) / (grid_res - 1)

    bm = bmesh.new()
    verts_grid = []
    
    print(f"Creating {grid_res}x{grid_res} ({grid_res*grid_res} vertices) subdivided terrain grid...")
    for iy in range(grid_res):
        row = []
        wz = -grid_extent + iy * step
        for ix in range(grid_res):
            wx = -grid_extent + ix * step
            h, col, wg, ws, wsn, wr = evaluate_highres_world_point(wx, wz)
            v = bm.verts.new((wx, wz, h))
            row.append((v, col, (wg, ws, wsn, wr)))
        verts_grid.append(row)

    bm.verts.ensure_lookup_table()
    print("Generating quad faces and UV coordinates...")
    
    for iy in range(grid_res - 1):
        for ix in range(grid_res - 1):
            v0 = verts_grid[iy][ix][0]
            v1 = verts_grid[iy][ix + 1][0]
            v2 = verts_grid[iy + 1][ix + 1][0]
            v3 = verts_grid[iy + 1][ix][0]
            bm.faces.new((v0, v1, v2, v3))

    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        f.smooth = True

    # Color Attribute layer for Biome Splatting
    color_layer = bm.loops.layers.color.get("Biome_Splat_RGBA")
    if not color_layer:
        color_layer = bm.loops.layers.color.new("Biome_Splat_RGBA")

    # UV Layer for Seamless Continental Mapping
    uv_layer = bm.loops.layers.uv.get("UVMap")
    if not uv_layer:
        uv_layer = bm.loops.layers.uv.new("UVMap")

    # Assign UVs and Vertex Colors
    for face in bm.faces:
        for loop in face.loops:
            vx, vy, vz = loop.vert.co.x, loop.vert.co.y, loop.vert.co.z
            # Conformal UV coordinates [0..1]
            u = (vx + grid_extent) / (grid_extent * 2.0)
            v = (vy + grid_extent) / (grid_extent * 2.0)
            loop[uv_layer].uv = (u, v)
            
            # Evaluate exact vertex color
            _, col, _, _, _, _ = evaluate_highres_world_point(vx, vy)
            loop[color_layer] = col

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    
    mesh.update()
    print(f"✅ Mesh updated with {len(mesh.vertices)} vertices and {len(mesh.polygons)} polygons.")

    # Save Blend file
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print(f"✅ Saved updated blend file to: {BLEND_PATH}")

    # Export FBX
    print(f"Exporting high-resolution FBX to: {FBX_PATH}...")
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        obj.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=False,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'EMPTY', 'CAMERA', 'LIGHT', 'MESH'},
        mesh_smooth_type='OFF',
        add_leaf_bones=False,
        bake_anim=False
    )
    print(f"🎉 Successfully exported high-resolution sculpted FBX to: {FBX_PATH}!")

if __name__ == "__main__":
    process_blend_continent()

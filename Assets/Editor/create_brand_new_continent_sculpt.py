import bpy
import bmesh
import math
import os
from mathutils import Vector

NEW_BLEND_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\Continent_Map_Sculpt.blend"
NEW_FBX_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\Continent_Map_Sculpt.fbx"
COMPAT_FBX_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\CircularContinent_MacroLevel1.fbx"

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

def clay_strips_vein_profile(x, y, base_z):
    warp = (smooth_noise(x * 0.0012, y * 0.0012) - 0.5) * 45.0
    u = (x * 0.7071 + y * 0.7071) + warp
    v = (-x * 0.7071 + y * 0.7071)
    
    strip_phase = (u * 0.022) % 1.0
    strip_shape = math.sin(strip_phase * math.pi)
    strip_shape = math.copysign(abs(strip_shape) ** 0.65, strip_shape)
    
    micro_phase = (v * 0.045 + u * 0.015) % 1.0
    micro_shape = math.sin(micro_phase * math.pi)
    
    return (strip_shape * 0.75 + micro_shape * 0.25)

def evaluate_highres_world_point(wx, wz):
    warp_x = (smooth_noise((wx + 3000.0) * 0.0011, (wz + 3000.0) * 0.0011) - 0.5) * 320.0
    warp_z = (smooth_noise((wx + 7000.0) * 0.0011, (wz + 7000.0) * 0.0011) - 0.5) * 320.0
    nx = wx + warp_x
    nz = wz + warp_z

    r_true = math.hypot(wx, wz)
    r_warped = math.hypot(nx, nz)
    angle_rad = math.atan2(nz, nx)
    angle_deg = (math.degrees(angle_rad) + 360.0) % 360.0

    col_lush_grass    = (0.27, 0.54, 0.18, 1.0)
    col_forest_grass  = (0.18, 0.42, 0.14, 1.0)
    col_desert_sand   = (0.86, 0.65, 0.35, 1.0)
    col_desert_mesa   = (0.74, 0.42, 0.24, 1.0)
    col_alpine_snow   = (0.92, 0.96, 1.00, 1.0)
    col_volcano_rock  = (0.16, 0.14, 0.19, 1.0)
    col_purple_crater = (0.52, 0.18, 0.78, 1.0)
    col_city_stone    = (0.75, 0.74, 0.70, 1.0)

    # 1. CENTRAL CITY PLATFORMS AND MOAT
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

    # 2. CONTINENT COASTLINE & OFFSHORE
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

    coast_fade = max(0.0, min(1.0, (max_coast_r - r_true) / 350.0))
    base_h = 20.0 + (smooth_noise((wx + 1000.0) * 0.0018, (wz + 1000.0) * 0.0018) * 16.0)
    vert_col = col_lush_grass
    w_grass = 1.0
    w_sand = 0.0
    w_snow = 0.0
    w_rock = 0.0

    # 3. NW VOLCANO & CRATER
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

    # 4. N FROSTHOLD SNOW PEAKS
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

    # 5. W/SW DESERT & MESAS
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

    # 6. STEPPED HIGHLANDS
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

    # 7. RIVERS & LAKE
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

    # 8. ORGANIC RIDGE/VEIN FOUNDATION
    if r_true > 520.0 and final_h > 2.0:
        city_fade = max(0.0, min(1.0, (r_true - 520.0) / 180.0))
        vein_val = clay_strips_vein_profile(wx, wz, final_h)
        elev_intensity = 2.2 + min(1.0, final_h / 150.0) * 5.3
        final_h += vein_val * elev_intensity * city_fade * coast_fade

    return final_h, vert_col, w_grass, w_sand, w_snow, w_rock

def build_brand_new_sculpt_file():
    print("🧹 Creating completely clean Blender file...")
    bpy.ops.wm.read_factory_settings(use_empty=True)

    # Scene & Metric Units
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = 'METERS'

    # Master Collection
    col_terrain = bpy.data.collections.new("Landscape_Terrain")
    scene.collection.children.link(col_terrain)

    col_reference = bpy.data.collections.new("City_Boundary_Guides")
    scene.collection.children.link(col_reference)

    # --- 1. CREATE HIGH-RESOLUTION BASE TERRAIN MESH ---
    # 801x801 vertices = 641,601 vertices, 640,000 quads
    # Grid extent: -4096m to +4096m (8192m wide) -> 10.24m per quad
    grid_res = 801
    grid_extent = 4096.0
    step = (grid_extent * 2.0) / (grid_res - 1)

    print(f"Generating high-resolution base terrain: {grid_res}x{grid_res} ({grid_res*grid_res} vertices)...")
    mesh = bpy.data.meshes.new("Floor01_MacroContinent_Terrain")
    terrain_obj = bpy.data.objects.new("Floor01_MacroContinent_Terrain", mesh)
    col_terrain.objects.link(terrain_obj)

    bm = bmesh.new()
    verts_grid = []
    
    for iy in range(grid_res):
        row = []
        wz = -grid_extent + iy * step
        for ix in range(grid_res):
            wx = -grid_extent + ix * step
            h, col, wg, ws, wsn, wr = evaluate_highres_world_point(wx, wz)
            v = bm.verts.new((wx, wz, h))
            row.append((v, col))
        verts_grid.append(row)

    bm.verts.ensure_lookup_table()
    print("Constructing quad mesh faces...")
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

    # Biome Vertex Color Attribute
    color_layer = bm.loops.layers.color.new("Biome_Splat_RGBA")
    uv_layer = bm.loops.layers.uv.new("UVMap")

    print("Mapping UVs and Biome Colors...")
    for face in bm.faces:
        for loop in face.loops:
            vx, vy, vz = loop.vert.co.x, loop.vert.co.y, loop.vert.co.z
            u = (vx + grid_extent) / (grid_extent * 2.0)
            v = (vy + grid_extent) / (grid_extent * 2.0)
            loop[uv_layer].uv = (u, v)
            _, col, _, _, _, _ = evaluate_highres_world_point(vx, vy)
            loop[color_layer] = col

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    print(f"✅ Base terrain built with {len(mesh.vertices):,} vertices and {len(mesh.polygons):,} quads.")

    # Create Material for Terrain
    mat = bpy.data.materials.new("M_Continent_Sculpt")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Roughness'].default_value = 0.85
    terrain_obj.data.materials.append(mat)

    # --- 2. CREATE TOWN BOUNDARY REFERENCE GUIDE ---
    # Concentric rings showing Tier 1-4 and Moat so the user clearly sees city boundaries
    guide_mesh = bpy.data.meshes.new("Town_Boundary_Reference_Mesh")
    guide_obj = bpy.data.objects.new("Town_Boundary_Reference", guide_mesh)
    col_reference.objects.link(guide_obj)

    bm_guide = bmesh.new()
    rings = [
        ("Tier 4 Castle Keep (R=102m, H=80m)", 102.0, 80.0),
        ("Tier 3 High District (R=208m, H=58m)", 208.0, 58.0),
        ("Tier 2 Mid District (R=324m, H=38m)", 324.0, 38.0),
        ("Tier 1 Low District (R=468m, H=22m)", 468.0, 22.0),
        ("Moat Waterline (R=535m, H=18m)", 535.0, 18.0)
    ]
    ring_segs = 64
    for label, radius, z_elev in rings:
        prev_v = None
        first_v = None
        for i in range(ring_segs):
            theta = (i / ring_segs) * 2.0 * math.pi
            gx = math.cos(theta) * radius
            gy = math.sin(theta) * radius
            v = bm_guide.verts.new((gx, gy, z_elev + 0.5))
            if prev_v:
                bm_guide.edges.new((prev_v, v))
            else:
                first_v = v
            prev_v = v
        if prev_v and first_v:
            bm_guide.edges.new((prev_v, first_v))

    bm_guide.to_mesh(guide_mesh)
    bm_guide.free()
    guide_mesh.update()

    # Lock reference so user can never accidentally select or sculpt it
    guide_obj.hide_select = True
    guide_obj.display_type = 'WIRE'
    guide_obj.color = (1.0, 0.85, 0.1, 1.0)
    guide_obj.show_name = True

    # --- 3. SUN LIGHTING & VIEWPORT CAMERA ---
    sun_data = bpy.data.lights.new(name="Sun_KeyLight", type='SUN')
    sun_data.energy = 4.0
    sun_data.color = (1.0, 0.96, 0.90)
    sun_obj = bpy.data.objects.new("Sun_KeyLight", sun_data)
    sun_obj.rotation_euler = (math.radians(50.0), math.radians(15.0), math.radians(-35.0))
    scene.collection.objects.link(sun_obj)

    cam_data = bpy.data.cameras.new("Overview_Camera")
    cam_data.clip_start = 1.0
    cam_data.clip_end = 50000.0
    cam_obj = bpy.data.objects.new("Overview_Camera", cam_data)
    cam_obj.location = (0.0, -3200.0, 2200.0)
    cam_obj.rotation_euler = (math.radians(58.0), 0.0, 0.0)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    # --- 4. CONFIGURE 3D VIEWPORT (CLIPPING & MATCAP CAVITY) ---
    for window in bpy.context.window_manager.windows:
        screen = window.screen
        for area in screen.areas:
            if area.type == 'VIEW_3D':
                for space in area.spaces:
                    if space.type == 'VIEW_3D':
                        space.clip_start = 1.0
                        space.clip_end = 50000.0
                        space.shading.type = 'SOLID'
                        space.shading.light = 'MATCAP'
                        space.shading.show_cavity = True
                        space.shading.cavity_type = 'BOTH'
                        space.shading.cavity_ridge_factor = 2.0
                        space.shading.cavity_valley_factor = 2.0

    # --- 5. CONFIGURE SCULPT BRUSHES & SCENE-METRIC SIZING ---
    # Make Floor01_MacroContinent_Terrain the active object
    bpy.context.view_layer.objects.active = terrain_obj
    terrain_obj.select_set(True)

    # Set SCULPT mode
    bpy.ops.object.mode_set(mode='SCULPT')

    sculpt = scene.tool_settings.sculpt
    ups = sculpt.unified_paint_settings
    
    # Crucial Fix: Set brush size unit to 'SCENE' (World Meters) so pressing F scales in meters
    # and zooming in/out NEVER breaks brush coverage!
    ups.use_unified_size = True
    ups.use_locked_size = 'SCENE'
    ups.unprojected_size = 120.0 # 120 meters diameter
    ups.size = 150 # 150px fallback
    ups.use_unified_strength = True
    ups.strength = 0.5

    # Configure each sculpt brush so switching tools stays in SCENE meter mode
    brush_names = ["Clay Strips", "Clay", "Draw", "Smooth", "Grab", "Flatten", "Scrape", "Inflate", "Pinch", "Crease"]
    for b_name in brush_names:
        b = bpy.data.brushes.get(b_name)
        if b:
            b.use_locked_size = 'SCENE'
            b.unprojected_size = 120.0
            b.strength = 0.5

    # Set active brush to Clay Strips
    clay_strips = bpy.data.brushes.get("Clay Strips")
    if clay_strips:
        sculpt.brush = clay_strips

    # Save to the brand new Blend file
    os.makedirs(os.path.dirname(NEW_BLEND_PATH), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=NEW_BLEND_PATH)
    print(f"🎉 Brand new clean Blender file created and saved to:\n  -> {NEW_BLEND_PATH}")

    # Export clean initial FBX files
    print(f"Exporting initial FBX to: {NEW_FBX_PATH}...")
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    terrain_obj.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=NEW_FBX_PATH,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'MESH'},
        mesh_smooth_type='OFF',
        add_leaf_bones=False,
        bake_anim=False
    )
    print(f"🎉 Initial sculpted FBX exported to: {NEW_FBX_PATH}")

    # Also update CircularContinent_MacroLevel1.fbx for instant Unity compatibility
    import shutil
    shutil.copyfile(NEW_FBX_PATH, COMPAT_FBX_PATH)
    print(f"🎉 Synced compatibility FBX to: {COMPAT_FBX_PATH}")

    # Return to SCULPT mode for the user
    bpy.context.view_layer.objects.active = terrain_obj
    bpy.ops.object.mode_set(mode='SCULPT')
    print("✨ Scene is now in SCULPT mode with Clay Strips ready!")

if __name__ == "__main__":
    build_brand_new_sculpt_file()

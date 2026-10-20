import bpy
import numpy as np
import math
import os
import time

OUTPUT_DIR = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Textures\Environments\Continent"
ALBEDO_PATH = os.path.join(OUTPUT_DIR, "T_Continent_MacroAlbedo.png")
NORMAL_PATH = os.path.join(OUTPUT_DIR, "T_Continent_MacroNormal.png")
SPLAT_PATH  = os.path.join(OUTPUT_DIR, "T_Continent_BiomeMask.png")
WATER_PATH  = os.path.join(OUTPUT_DIR, "T_Continent_WaterMask.png")

def s_curve(t):
    return t * t * (3.0 - 2.0 * t)

def generate_textures_from_user_sculpt(res=2048):
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    t0 = time.time()
    print(f"🎨 Generating {res}x{res} Textures from User's Sculpted Model ({bpy.data.filepath})...")

    terrain = bpy.data.objects.get("Floor01_MacroContinent_Terrain")
    if not terrain:
        raise RuntimeError("Floor01_MacroContinent_Terrain not found in Blender scene!")

    mesh = terrain.data
    num_verts = len(mesh.vertices)
    grid_res = int(math.isqrt(num_verts))
    if grid_res * grid_res != num_verts:
        raise RuntimeError(f"Expected square grid of vertices, got {num_verts}")

    print(f"Reading {num_verts:,} sculpted vertex coordinates ({grid_res}x{grid_res})...")
    raw_coords = np.empty(num_verts * 3, dtype=np.float32)
    mesh.vertices.foreach_get('co', raw_coords)
    raw_coords = raw_coords.reshape((num_verts, 3))

    # Grid elevations: [iy, ix] where iy is south->north (Y), ix is west->east (X)
    z_src = raw_coords[:, 2].reshape((grid_res, grid_res))
    z_min, z_max = z_src.min(), z_src.max()
    print(f"Sculpted elevation bounds: Min={z_min:.2f}m, Max={z_max:.2f}m")

    # Interpolate to target resolution (res x res) using fast separable bilinear interpolation
    print(f"Resampling sculpted heightfield from {grid_res}x{grid_res} to {res}x{res}...")
    x_indices = np.linspace(0, grid_res - 1, res, dtype=np.float32)
    y_indices = np.linspace(0, grid_res - 1, res, dtype=np.float32)

    x0 = np.floor(x_indices).astype(np.int32)
    x1 = np.clip(x0 + 1, 0, grid_res - 1)
    fx = x_indices - x0

    y0 = np.floor(y_indices).astype(np.int32)
    y1 = np.clip(y0 + 1, 0, grid_res - 1)
    fy = y_indices - y0

    # Separable bilinear: horizontal then vertical
    temp = z_src[:, x0] * (1.0 - fx)[None, :] + z_src[:, x1] * fx[None, :]
    z_grid = temp[y0, :] * (1.0 - fy)[:, None] + temp[y1, :] * fy[:, None]

    # Coordinate grids in world meters (-4096m to +4096m)
    extent = 4096.0
    u_coords = np.linspace(-extent, extent, res, dtype=np.float32)
    v_coords = np.linspace(-extent, extent, res, dtype=np.float32)
    wx_grid, wz_grid = np.meshgrid(u_coords, v_coords)

    # Polar coordinates
    r_grid = np.sqrt(wx_grid**2 + wz_grid**2)
    angle_rad = np.arctan2(wz_grid, wx_grid)
    angle_deg = (np.degrees(angle_rad) + 360.0) % 360.0

    # Slopes and gradients for cliff detection and normals
    dx = (extent * 2.0) / (res - 1)
    dz_dx = np.gradient(z_grid, dx, axis=1)
    dz_dy = np.gradient(z_grid, dx, axis=0)
    slope = np.sqrt(dz_dx**2 + dz_dy**2)

    print("Computing Biome Masks tailored to user's peaks and gorges...")

    # 1. Central City Mask (R <= 468m) and Moat (468m < R <= 535m)
    city_tier4 = np.clip((105.0 - r_grid) / 5.0, 0.0, 1.0)
    city_tier3 = np.clip((210.0 - r_grid) / 6.0, 0.0, 1.0) * (1.0 - city_tier4)
    city_tier2 = np.clip((326.0 - r_grid) / 6.0, 0.0, 1.0) * (1.0 - city_tier4 - city_tier3)
    city_tier1 = np.clip((468.0 - r_grid) / 8.0, 0.0, 1.0) * (1.0 - city_tier4 - city_tier3 - city_tier2)
    city_total = np.clip((468.0 - r_grid) / 10.0, 0.0, 1.0)

    moat_mask = np.clip(1.0 - np.abs(r_grid - 501.5) / 33.5, 0.0, 1.0)
    moat_mask = np.sin(moat_mask * (math.pi * 0.5))

    # 2. NW Volcanic Spire & Caldera (Centered around user's peak coordinates)
    peak_vert_idx = np.argmax(raw_coords[:, 2])
    peak_x = float(raw_coords[peak_vert_idx, 0])
    peak_y = float(raw_coords[peak_vert_idx, 1])
    peak_z = float(raw_coords[peak_vert_idx, 2])
    print(f"🌋 Detected Summit: Height {peak_z:.2f}m at X={peak_x:.2f}m, Y={peak_y:.2f}m")

    volc_dist = np.sqrt((wx_grid - peak_x)**2 + (wz_grid - peak_y)**2)
    volc_mask = np.clip(1.0 - (volc_dist / 1050.0), 0.0, 1.0)
    volc_mask = s_curve(volc_mask)

    # Caldera / Crater interior (deep crater sculpted by user near summit)
    caldera_mask = np.clip(1.0 - (volc_dist / 280.0), 0.0, 1.0)
    caldera_mask = s_curve(caldera_mask)

    # 3. North Alpine Snow & Glaciers (peaks in north or high elevations)
    north_angle_weight = np.clip(1.0 - np.abs(angle_deg - 88.0) / 45.0, 0.0, 1.0)
    north_dist_weight  = np.clip((r_grid - 800.0) / 450.0, 0.0, 1.0)
    snow_mask = s_curve(north_angle_weight) * s_curve(north_dist_weight)
    
    # Snow on high mountain ridges (Z > 240m, outside volcano)
    high_peak_snow = np.clip((z_grid - 220.0) / 150.0, 0.0, 1.0) * (1.0 - volc_mask * 0.85)
    snow_mask = np.clip(snow_mask + high_peak_snow, 0.0, 1.0)

    # 4. West / Southwest Desert & Mesas
    west_angle_weight = np.clip(1.0 - np.abs(angle_deg - 198.0) / 50.0, 0.0, 1.0)
    west_dist_weight  = np.clip((r_grid - 620.0) / 400.0, 0.0, 1.0)
    desert_mask = s_curve(west_angle_weight) * s_curve(west_dist_weight) * (1.0 - city_total)

    # 5. Cliffs / Steep Gorges (everywhere outside city where slope is steep)
    cliff_mask = np.clip((slope - 0.38) / 0.28, 0.0, 1.0)
    cliff_mask = s_curve(cliff_mask) * (1.0 - city_total)

    # 6. Water, Deep Ravines & Coastline
    water_mask = np.clip((0.0 - z_grid) / 8.0, 0.0, 1.0)
    shore_mask = np.clip(1.0 - np.abs(z_grid - 2.0) / 5.0, 0.0, 1.0)

    # 7. Meadows / Grass (Default continental fill)
    grass_mask = np.clip(1.0 - desert_mask - snow_mask - volc_mask - city_total - water_mask, 0.0, 1.0)

    # --- COLOR DEFINITIONS ---
    col_grass_light   = np.array([0.38, 0.70, 0.24], dtype=np.float32)
    col_grass_rich    = np.array([0.27, 0.55, 0.18], dtype=np.float32)
    col_grass_forest  = np.array([0.17, 0.40, 0.13], dtype=np.float32)

    col_sand_gold     = np.array([0.88, 0.72, 0.42], dtype=np.float32)
    col_desert_mesa   = np.array([0.72, 0.38, 0.22], dtype=np.float32)

    col_snow_crisp    = np.array([0.94, 0.97, 1.00], dtype=np.float32)
    col_snow_ice      = np.array([0.62, 0.82, 0.92], dtype=np.float32)
    col_mountain_slate= np.array([0.34, 0.36, 0.42], dtype=np.float32)

    col_volc_basalt   = np.array([0.14, 0.12, 0.16], dtype=np.float32)
    col_volc_crater   = np.array([0.72, 0.22, 0.98], dtype=np.float32) # Radiant glowing violet caldera

    col_city_tier4    = np.array([0.88, 0.86, 0.82], dtype=np.float32)
    col_city_tier3    = np.array([0.80, 0.78, 0.74], dtype=np.float32)
    col_city_tier2    = np.array([0.74, 0.72, 0.68], dtype=np.float32)
    col_city_tier1    = np.array([0.66, 0.64, 0.60], dtype=np.float32)

    col_cliff_rock    = np.array([0.28, 0.26, 0.30], dtype=np.float32)
    col_water_shallow = np.array([0.22, 0.58, 0.68], dtype=np.float32)
    col_water_deep    = np.array([0.10, 0.20, 0.30], dtype=np.float32)

    # Multi-frequency organic texture modulation
    wave_noise = (
        np.sin(wx_grid * 0.035 + np.cos(wz_grid * 0.035) * 2.0) * 0.25 +
        np.sin(wz_grid * 0.075 + np.sin(wx_grid * 0.055) * 2.0) * 0.15
    )

    albedo = np.zeros((res, res, 3), dtype=np.float32)

    # 1. Grass
    grass_hue = col_grass_rich[None, None, :] + (wave_noise[:, :, None] * 0.10)
    grass_elev_var = np.clip(z_grid / 150.0, 0.0, 1.0)[:, :, None]
    grass_col = grass_hue * (1.0 - grass_elev_var * 0.25) + col_grass_light[None, None, :] * (grass_elev_var * 0.25)
    albedo += grass_col * grass_mask[:, :, None]

    # 2. Desert
    desert_col = col_sand_gold[None, None, :] + (np.sin(wx_grid * 0.08) * 0.08)[:, :, None]
    mesa_boost = np.clip((z_grid - 25.0) / 40.0, 0.0, 1.0)[:, :, None]
    desert_col = desert_col * (1.0 - mesa_boost * 0.55) + col_desert_mesa[None, None, :] * (mesa_boost * 0.55)
    albedo += desert_col * desert_mask[:, :, None]

    # 3. Snow
    snow_col = col_snow_crisp[None, None, :]
    ice_shadow = np.clip(cliff_mask * 0.6, 0.0, 1.0)[:, :, None]
    snow_col = snow_col * (1.0 - ice_shadow) + col_snow_ice[None, None, :] * ice_shadow
    albedo += snow_col * snow_mask[:, :, None]

    # 4. Volcano with basalt and glowing purple caldera
    volc_col = col_volc_basalt[None, None, :] * (1.0 - caldera_mask[:, :, None]) + col_volc_crater[None, None, :] * caldera_mask[:, :, None]
    albedo += volc_col * volc_mask[:, :, None]

    # 5. City
    city_col = (
        col_city_tier4[None, None, :] * city_tier4[:, :, None] +
        col_city_tier3[None, None, :] * city_tier3[:, :, None] +
        col_city_tier2[None, None, :] * city_tier2[:, :, None] +
        col_city_tier1[None, None, :] * city_tier1[:, :, None]
    )
    pave_rings = np.sin(r_grid * 0.15) * 0.06
    city_col = np.clip(city_col + pave_rings[:, :, None], 0.0, 1.0)
    albedo = albedo * (1.0 - city_total[:, :, None]) + city_col * city_total[:, :, None]

    # 6. Cliff rock overlay
    albedo = albedo * (1.0 - cliff_mask[:, :, None]) + col_cliff_rock[None, None, :] * cliff_mask[:, :, None]

    # 7. Water & Coastline
    water_depth = np.clip(-z_grid / 20.0, 0.0, 1.0)[:, :, None]
    water_col = col_water_shallow[None, None, :] * (1.0 - water_depth) + col_water_deep[None, None, :] * water_depth
    albedo = albedo * (1.0 - water_mask[:, :, None]) + water_col * water_mask[:, :, None]
    albedo = albedo * (1.0 - shore_mask[:, :, None] * 0.45) + col_sand_gold[None, None, :] * (shore_mask[:, :, None] * 0.45)

    # Ambient Occlusion in user's deep ravines and volcanic gorges
    gorge_depth = np.clip((slope * 0.15) + (np.clip(100.0 - z_grid, 0.0, 150.0) / 300.0), 0.0, 0.35)
    ao = np.clip(1.0 - gorge_depth, 0.65, 1.0)
    albedo = albedo * ao[:, :, None]
    albedo = np.clip(albedo, 0.0, 1.0)

    # --- NORMAL MAP ---
    print("Computing Tangent-Space Normal Map for user's sculpted features...")
    norm_x = -dz_dx * 2.2
    norm_y = -dz_dy * 2.2
    norm_z = np.ones((res, res), dtype=np.float32)

    # Painterly cell perturbation
    facet_scale = 0.025
    cell_fx = np.sin(wx_grid * facet_scale * 3.14159) * 0.15
    cell_fy = np.cos(wz_grid * facet_scale * 3.14159) * 0.15
    norm_x += cell_fx * (1.0 - city_total)
    norm_y += cell_fy * (1.0 - city_total)

    norm_mag = np.sqrt(norm_x**2 + norm_y**2 + norm_z**2)
    norm_x /= norm_mag
    norm_y /= norm_mag
    norm_z /= norm_mag

    normal_map = np.zeros((res, res, 4), dtype=np.float32)
    normal_map[:, :, 0] = norm_x * 0.5 + 0.5
    normal_map[:, :, 1] = norm_y * 0.5 + 0.5
    normal_map[:, :, 2] = norm_z * 0.5 + 0.5
    normal_map[:, :, 3] = 1.0

    albedo_rgba = np.ones((res, res, 4), dtype=np.float32)
    albedo_rgba[:, :, :3] = albedo

    # --- SPLAT MASK ---
    splat_mask = np.zeros((res, res, 4), dtype=np.float32)
    splat_mask[:, :, 0] = grass_mask
    splat_mask[:, :, 1] = np.clip(desert_mask + shore_mask * 0.7, 0.0, 1.0)
    splat_mask[:, :, 2] = snow_mask
    splat_mask[:, :, 3] = np.clip(cliff_mask + volc_mask + city_total, 0.0, 1.0)
    splat_sum = np.maximum(0.001, np.sum(splat_mask, axis=2, keepdims=True))
    splat_mask /= splat_sum

    # --- WATER MASK ---
    water_out = np.zeros((res, res, 4), dtype=np.float32)
    water_out[:, :, 0] = water_mask
    water_out[:, :, 1] = moat_mask
    water_out[:, :, 2] = shore_mask
    water_out[:, :, 3] = 1.0

    save_texture(ALBEDO_PATH, albedo_rgba, "T_Continent_MacroAlbedo", res)
    save_texture(NORMAL_PATH, normal_map, "T_Continent_MacroNormal", res)
    save_texture(SPLAT_PATH, splat_mask, "T_Continent_BiomeMask", res)
    save_texture(WATER_PATH, water_out, "T_Continent_WaterMask", res)

    print(f"🎉 Successfully generated textures from user's sculpted map in {time.time() - t0:.2f}s!")

def save_texture(filepath, data_rgba, img_name, res):
    out_data = np.ascontiguousarray(data_rgba, dtype=np.float32)
    img = bpy.data.images.get(img_name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(img_name, width=res, height=res, alpha=True, float_buffer=False)
    img.pixels.foreach_set(out_data.flatten())
    img.filepath_raw = filepath
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)
    print(f"  Saved: {os.path.basename(filepath)} ({res}x{res})")

if __name__ == "__main__":
    generate_textures_from_user_sculpt(res=2048)

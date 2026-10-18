import bpy  # type: ignore
import numpy as np  # type: ignore
import math
import os

# =============================================================================
# ISLAND TERRAIN GENERATOR (Blender 5.1 Python)
# Generates:
# 1. Island_Overworld_Terrain.fbx (sculpted multi-biome island with rivers, flat kingdom center)
# 2. 7 PBR and Splat Textures (Grass, Desert Sand, Volcanic Rock, Beach, Cliff, Master Map, Splat Map)
# =============================================================================

PROJECT_DIR = r"D:\Games\BattleSummoner\BattleSummoner"
MODELS_DIR = os.path.join(PROJECT_DIR, "Assets", "Models", "Environment")
TEXTURES_DIR = os.path.join(PROJECT_DIR, "Assets", "Textures", "Environment")

os.makedirs(MODELS_DIR, exist_ok=True)
os.makedirs(TEXTURES_DIR, exist_ok=True)

print(">>> Starting Island Terrain Generation in Blender...")

# -----------------------------------------------------------------------------
# 1. Clear existing objects
# -----------------------------------------------------------------------------
bpy.ops.wm.read_factory_settings(use_empty=True)

# -----------------------------------------------------------------------------
# 2. Grid Geometry Specification
# -----------------------------------------------------------------------------
GRID_SIZE = 1700.0  # meters (-850 to +850)
RES = 280          # 280 x 280 vertices = 78,400 verts, 155,682 tris
KINGDOM_R = 348.0  # Center kingdom radius (flat foundation & moat canal up to 348m)
KINGDOM_BLEND = 35.0 # Blend to terrain
CALDERA_X, CALDERA_Z = 380.0, 380.0 # NE Caldera center (r ~ 537m, angle 45 deg)

xs = np.linspace(-GRID_SIZE * 0.5, GRID_SIZE * 0.5, RES, dtype=np.float32)
zs = np.linspace(-GRID_SIZE * 0.5, GRID_SIZE * 0.5, RES, dtype=np.float32)
X, Z = np.meshgrid(xs, zs)

R = np.sqrt(X**2 + Z**2)
Theta = np.arctan2(Z, X) # -pi to pi

# Organic Coastline radius R_coast(theta)
R_coast = 780.0 + 45.0 * np.sin(3.0 * Theta + 0.6) + 32.0 * np.cos(5.0 * Theta - 0.3) + 18.0 * np.sin(7.0 * Theta) + 12.0 * np.cos(9.0 * Theta)

# -----------------------------------------------------------------------------
# 3. 8 Meandering River Channels
# -----------------------------------------------------------------------------
# Radiating outward at 8 bridge angles (0, 45, 90, 135, 180, 225, 270, 315 deg)
bridge_angles = np.array([k * (math.pi / 4.0) for k in range(8)], dtype=np.float32)

dist_to_rivers = np.full_like(R, 9999.0)
for k in range(8):
    ang_k = bridge_angles[k]
    # Organic meander offset
    r_from_moat = np.maximum(0.0, R - KINGDOM_R)
    meander = 0.08 * np.sin(r_from_moat * 0.018) + 0.04 * np.cos(r_from_moat * 0.035)
    river_ang = ang_k + meander
    
    # Angular difference
    d_theta = np.abs(np.arctan2(np.sin(Theta - river_ang), np.cos(Theta - river_ang)))
    arc_dist = R * d_theta
    dist_to_rivers = np.minimum(dist_to_rivers, arc_dist)

# River channel mask: 0 in center of river bed, 1 on normal land
river_half_width = 10.0 # meters
river_bank_width = 24.0 # meters
w_river = np.clip((dist_to_rivers - river_half_width) / (river_bank_width - river_half_width), 0.0, 1.0)
w_river = w_river * w_river * (3.0 - 2.0 * w_river) # smoothstep

# -----------------------------------------------------------------------------
# 4. Organic Biome Zones (Noisy angular partitioning)
# -----------------------------------------------------------------------------
# Spatial distortion for organic non-square overlapping borders
theta_noisy = Theta + 0.38 * np.sin(X * 0.005 + Z * 0.004) + 0.20 * np.cos(X * 0.010 - Z * 0.008)
# Normalize to [-pi, pi]
theta_noisy = np.arctan2(np.sin(theta_noisy), np.cos(theta_noisy))

# Biome Centers & Envelopes:
# 1. NE Volcanic: centered at +pi/4 (45 deg) -> range [0, pi/2]
# 2. SW Desert: centered at -3pi/4 (-135 deg) -> range [-pi, -pi/2]
# 3. NW Mistwood Forest: centered at +3pi/4 (135 deg) -> range [pi/2, pi]
# 4. SE Terraced Meadows: centered at -pi/4 (-45 deg) -> range [-pi/2, 0]

def calc_biome_weight(theta_arr, target_ang, width=math.pi * 0.5, blend=0.45):
    diff = np.abs(np.arctan2(np.sin(theta_arr - target_ang), np.cos(theta_arr - target_ang)))
    w = np.clip(1.0 - (diff - (width * 0.5 - blend)) / blend, 0.0, 1.0)
    return w * w * (3.0 - 2.0 * w)

w_volc = calc_biome_weight(theta_noisy, math.pi * 0.25)
w_desert = calc_biome_weight(theta_noisy, -math.pi * 0.75)
w_forest = calc_biome_weight(theta_noisy, math.pi * 0.75)
w_meadow = calc_biome_weight(theta_noisy, -math.pi * 0.25)

# Total weight normalization
w_total = w_volc + w_desert + w_forest + w_meadow + 1e-5
w_volc /= w_total
w_desert /= w_total
w_forest /= w_total
w_meadow /= w_total

# -----------------------------------------------------------------------------
# 5. Biome Elevation Functions
# -----------------------------------------------------------------------------
# A. NE Volcanic Range & Caldera
h_volc_ridges = 65.0 * (np.sin(X * 0.014) * np.cos(Z * 0.014))**2 + 35.0 * np.abs(np.sin(X * 0.028 + Z * 0.022)) + 15.0 * np.sin(X * 0.05)
dist_to_caldera = np.sqrt((X - CALDERA_X)**2 + (Z - CALDERA_Z)**2)
caldera_depress = np.clip(1.0 - dist_to_caldera / 95.0, 0.0, 1.0)
caldera_rim = np.exp(-((dist_to_caldera - 95.0) / 32.0)**2) * 55.0
h_volc = h_volc_ridges * (1.0 - caldera_depress * 0.85) + caldera_rim + 18.0

# B. SW Desert Dunes & Mesas (User: "i want a desert too on the bottomleft section of the map")
dune_ripples = 12.0 * np.sin(X * 0.025 + Z * 0.015 + 0.6 * np.sin(X * 0.01)) + 10.0 * np.cos(X * 0.018 - Z * 0.022)
mesa_steps = np.floor(np.clip(np.sin(X * 0.008) * np.cos(Z * 0.008) * 3.5, 0.0, 3.0)) * 9.0
h_desert = dune_ripples + mesa_steps + 8.0

# C. NW Mistwood Forest (Rolling green canopy hills)
h_forest = 24.0 * (np.sin(X * 0.011) + np.cos(Z * 0.013)) + 14.0 * np.sin(X * 0.022 + Z * 0.018) + 12.0

# D. SE Verdant Terraced Meadows & Coastal Steppes
h_meadow = 18.0 * np.sin(X * 0.012) * np.cos(Z * 0.011) + 10.0 * np.cos(X * 0.020 + Z * 0.020) + 8.0

# Combined Raw Land Elevation
h_raw_land = w_volc * h_volc + w_desert * h_desert + w_forest * h_forest + w_meadow * h_meadow

# -----------------------------------------------------------------------------
# 6. Combined Island Height with Kingdom Flat Core, Moat Trench & Ocean Shelf
# -----------------------------------------------------------------------------
# Kingdom City Core: R <= 290m -> -0.5m
# Inner Wall Drop: 290m to 298m -> smooth drop to -3.8m canal bed
# Moat Canal: 298m to 348m -> -3.8m (water level is -0.75m, moat depth ~ 3.05m)
# Outer Embankment: 348m to 375m -> smooth ramp from -3.8m to 0.0m
# Outer Buffer: 375m to 420m -> smooth blend into raw biome terrain
# Outer Island: R > 420m -> h_raw_land with river carving and ocean shelf (-14m)

# River carving on raw land
h_river_carved = w_river * h_raw_land + (1.0 - w_river) * (-3.2)

# Coastline falloff to ocean floor (-14m)
w_coast = np.clip((R - (R_coast - 45.0)) / 75.0, 0.0, 1.0)
s_coast = w_coast * w_coast * (3.0 - 2.0 * w_coast)
h_outer_island = (1.0 - s_coast) * h_river_carved + s_coast * (-14.0)

# Smooth blend from buffer zone (375m..420m) into outer terrain
w_outer = np.clip((R - 375.0) / 45.0, 0.0, 1.0)
s_outer = w_outer * w_outer * (3.0 - 2.0 * w_outer)
h_post_moat = (1.0 - s_outer) * 0.0 + s_outer * h_outer_island

# Outer moat embankment ramp (348m..375m: -3.8m up to 0.0m)
w_ramp = np.clip((R - 348.0) / 27.0, 0.0, 1.0)
s_ramp = w_ramp * w_ramp * (3.0 - 2.0 * w_ramp)
h_embankment = -3.8 + s_ramp * 3.8

# Inner city drop (290m..298m: -0.5m down to -3.8m)
w_inner = np.clip((R - 290.0) / 8.0, 0.0, 1.0)
s_inner = w_inner * w_inner * (3.0 - 2.0 * w_inner)
h_city_drop = (1.0 - s_inner) * (-0.5) + s_inner * (-3.8)

# Final Height Composition
Y = np.where(R <= 290.0, -0.5,
    np.where(R <= 298.0, h_city_drop,
    np.where(R <= 348.0, -3.8,
    np.where(R <= 375.0, h_embankment,
             h_post_moat))))

# -----------------------------------------------------------------------------
# 7. Build Blender Mesh
# -----------------------------------------------------------------------------
mesh = bpy.data.meshes.new("Mesh_Island_Overworld_Terrain")
obj = bpy.data.objects.new("Island_Overworld_Terrain", mesh)
bpy.context.collection.objects.link(obj)

verts = []
for j in range(RES):
    for i in range(RES):
        verts.append((float(X[j, i]), float(Y[j, i]), float(Z[j, i])))

# Explicit triangles mathematically guaranteed to face UP (+Y in Unity):
# Tri 1: (v0, v2, v1) -> Cross(v2-v0, v1-v0).y = +dz * dx > 0
# Tri 2: (v0, v3, v2) -> Cross(v3-v0, v2-v0).y = +dz * dx > 0
faces = []
for j in range(RES - 1):
    for i in range(RES - 1):
        v0 = j * RES + i
        v1 = j * RES + (i + 1)
        v2 = (j + 1) * RES + (i + 1)
        v3 = (j + 1) * RES + i
        faces.append((v0, v2, v1))
        faces.append((v0, v3, v2))

mesh.from_pydata(verts, [], faces)
mesh.update()

# Recalculate normals to be 100% outward/upward consistent
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.shade_smooth()

# -----------------------------------------------------------------------------
# 8. UV Coordinates (UV0: global [0,1], UV1: tiled [0, 32])
# -----------------------------------------------------------------------------
uv_layer_global = mesh.uv_layers.new(name="UVMap_Global")
uv_layer_tiled = mesh.uv_layers.new(name="UVMap_Tiled")

loop_idx = 0
for poly in mesh.polygons:
    for v_idx in poly.vertices:
        gx = (verts[v_idx][0] + GRID_SIZE * 0.5) / GRID_SIZE
        gz = (verts[v_idx][2] + GRID_SIZE * 0.5) / GRID_SIZE
        uv_layer_global.data[loop_idx].uv = (gx, gz)
        uv_layer_tiled.data[loop_idx].uv = (gx * 32.0, gz * 32.0)
        loop_idx += 1

# -----------------------------------------------------------------------------
# 9. Vertex Colors (Biome Splat Weights)
# R: Forest / Meadow Grass
# G: Desert Sand
# B: Volcanic Basalt
# A: Beach Sand & Riverbanks
# -----------------------------------------------------------------------------
color_layer = mesh.color_attributes.new(name="BiomeSplat", type='FLOAT_COLOR', domain='POINT')

# Compute per-vertex RGBA
beach_mask = np.clip((R - (R_coast - 65.0)) / 55.0, 0.0, 1.0) * (1.0 - np.clip((R - R_coast) / 25.0, 0.0, 1.0))
river_bank_mask = (1.0 - w_river)

v_col_A = np.clip(beach_mask + river_bank_mask * 0.85, 0.0, 1.0)
v_col_R = (w_forest + w_meadow) * (1.0 - v_col_A)
v_col_G = w_desert * (1.0 - v_col_A)
v_col_B = w_volc * (1.0 - v_col_A)

for j in range(RES):
    for i in range(RES):
        v_idx = j * RES + i
        r_c = float(v_col_R[j, i])
        g_c = float(v_col_G[j, i])
        b_c = float(v_col_B[j, i])
        a_c = float(v_col_A[j, i])
        color_layer.data[v_idx].color = (r_c, g_c, b_c, a_c)

print(">>> Terrain mesh successfully created with", len(verts), "vertices and", len(faces), "polygons.")

# -----------------------------------------------------------------------------
# 10. Export FBX to Unity Assets with true 1:1 Unit Scale (1700m span)
# -----------------------------------------------------------------------------
fbx_path = os.path.join(MODELS_DIR, "Island_Overworld_Terrain.fbx")
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    check_existing=False,
    use_selection=True,
    global_scale=1.0,
    apply_unit_scale=False,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    bake_space_transform=False,
    mesh_smooth_type='FACE',
    colors_type='LINEAR'
)
print(">>> Exported FBX to:", fbx_path)

# =============================================================================
# 11. GENERATE PROCEDURAL HIGH-RESOLUTION TEXTURES
# =============================================================================
TEX_SIZE = 1024

def save_image_from_array(img_name, rgba_array, filename):
    img = bpy.data.images.new(img_name, rgba_array.shape[1], rgba_array.shape[0])
    img.pixels = rgba_array.flatten().tolist()
    img.filepath_raw = os.path.join(TEXTURES_DIR, filename)
    img.file_format = 'PNG'
    img.save()
    print(f"Saved texture: {filename}")

# A. Seamless Lush Grass (Meadow & Forest)
tx = np.linspace(0, 8 * math.pi, TEX_SIZE)
ty = np.linspace(0, 8 * math.pi, TEX_SIZE)
TX, TY = np.meshgrid(tx, ty)
noise_grass1 = 0.5 + 0.3 * np.sin(TX * 2.0) * np.cos(TY * 2.0) + 0.2 * np.sin(TX * 5.0 + TY * 3.0)
noise_grass2 = 0.5 + 0.3 * np.cos(TX * 4.0 - TY * 2.0) + 0.2 * np.sin(TX * 7.0)

grass_R = 0.28 + 0.15 * noise_grass1
grass_G = 0.68 + 0.22 * noise_grass1 - 0.08 * noise_grass2
grass_B = 0.18 + 0.10 * noise_grass2
grass_A = np.ones_like(grass_R)
grass_tex = np.dstack((grass_R, grass_G, grass_B, grass_A)).astype(np.float32)
save_image_from_array("T_Terrain_LushGrass", grass_tex, "T_Terrain_LushGrass.png")

# B. Seamless Desert Sand (Wind Ripples)
dune_lines = np.sin(TX * 3.0 + 0.5 * np.cos(TY * 1.5)) * 0.5 + 0.5
sand_noise = 0.5 + 0.3 * np.sin(TX * 8.0) * np.cos(TY * 8.0)
sand_R = 0.88 + 0.08 * dune_lines + 0.04 * sand_noise
sand_G = 0.65 + 0.12 * dune_lines - 0.03 * sand_noise
sand_B = 0.38 + 0.08 * dune_lines
sand_A = np.ones_like(sand_R)
sand_tex = np.clip(np.dstack((sand_R, sand_G, sand_B, sand_A)), 0.0, 1.0).astype(np.float32)
save_image_from_array("T_Terrain_DesertSand", sand_tex, "T_Terrain_DesertSand.png")

# C. Seamless Volcanic Basalt / Obsidian Rock
rock_crags = np.abs(np.sin(TX * 2.5) * np.cos(TY * 2.5))
rock_noise = 0.5 + 0.3 * np.sin(TX * 6.0 + TY * 4.0) + 0.2 * np.cos(TX * 9.0)
volc_R = 0.18 + 0.14 * rock_crags + 0.06 * rock_noise
volc_G = 0.17 + 0.11 * rock_crags + 0.04 * rock_noise
volc_B = 0.24 + 0.16 * rock_crags + 0.08 * rock_noise # Obsidian bluish/purplish tint
volc_A = np.ones_like(volc_R)
volc_tex = np.clip(np.dstack((volc_R, volc_G, volc_B, volc_A)), 0.0, 1.0).astype(np.float32)
save_image_from_array("T_Terrain_VolcanicRock", volc_tex, "T_Terrain_VolcanicRock.png")

# D. Seamless Beach Sand
beach_R = 0.82 + 0.08 * sand_noise
beach_G = 0.74 + 0.07 * sand_noise
beach_B = 0.55 + 0.06 * sand_noise
beach_A = np.ones_like(beach_R)
beach_tex = np.clip(np.dstack((beach_R, beach_G, beach_B, beach_A)), 0.0, 1.0).astype(np.float32)
save_image_from_array("T_Terrain_BeachSand", beach_tex, "T_Terrain_BeachSand.png")

# E. Seamless Rock Cliff (Limestone & Slate Strata)
cliff_strata = np.sin(TY * 6.0 + 0.3 * np.sin(TX * 3.0)) * 0.5 + 0.5
cliff_R = 0.42 + 0.18 * cliff_strata
cliff_G = 0.40 + 0.16 * cliff_strata
cliff_B = 0.38 + 0.15 * cliff_strata
cliff_A = np.ones_like(cliff_R)
cliff_tex = np.clip(np.dstack((cliff_R, cliff_G, cliff_B, cliff_A)), 0.0, 1.0).astype(np.float32)
save_image_from_array("T_Terrain_RockCliff", cliff_tex, "T_Terrain_RockCliff.png")

# F. Full Island Master Satellite Color Map (2048 x 2048 matching user diagram)
MAP_SIZE = 2048
m_xs = np.linspace(-GRID_SIZE * 0.5, GRID_SIZE * 0.5, MAP_SIZE, dtype=np.float32)
m_zs = np.linspace(-GRID_SIZE * 0.5, GRID_SIZE * 0.5, MAP_SIZE, dtype=np.float32)
m_X, m_Z = np.meshgrid(m_xs, m_zs)
m_R = np.sqrt(m_X**2 + m_Z**2)
m_Theta = np.arctan2(m_Z, m_X)
m_R_coast = 780.0 + 45.0 * np.sin(3.0 * m_Theta + 0.6) + 32.0 * np.cos(5.0 * m_Theta - 0.3)

m_theta_noisy = m_Theta + 0.38 * np.sin(m_X * 0.005 + m_Z * 0.004) + 0.20 * np.cos(m_X * 0.010 - m_Z * 0.008)
m_theta_noisy = np.arctan2(np.sin(m_theta_noisy), np.cos(m_theta_noisy))

m_w_volc = calc_biome_weight(m_theta_noisy, math.pi * 0.25)
m_w_desert = calc_biome_weight(m_theta_noisy, -math.pi * 0.75)
m_w_forest = calc_biome_weight(m_theta_noisy, math.pi * 0.75)
m_w_meadow = calc_biome_weight(m_theta_noisy, -math.pi * 0.25)
m_w_tot = m_w_volc + m_w_desert + m_w_forest + m_w_meadow + 1e-5
m_w_volc /= m_w_tot
m_w_desert /= m_w_tot
m_w_forest /= m_w_tot
m_w_meadow /= m_w_tot

# Base RGB colors per biome
# Forest: Rich dark green (0.16, 0.48, 0.12)
# Meadow: Vibrant yellow-green (0.28, 0.62, 0.18)
# Desert: Warm golden sand (0.86, 0.64, 0.35)
# Volcanic: Charcoal obsidian (0.20, 0.19, 0.26)
# Caldera rift center: glowing violet (0.55, 0.15, 0.75)
# Beach: Golden sand (0.82, 0.74, 0.50)
# Ocean: Deep azure blue (0.08, 0.32, 0.72)
# River: Turquoise river (0.15, 0.65, 0.85)
# Kingdom core: Royal amber cobblestone (0.75, 0.62, 0.42)

m_dist_to_rivers = np.full_like(m_R, 9999.0)
for k in range(8):
    ang_k = bridge_angles[k]
    r_f = np.maximum(0.0, m_R - KINGDOM_R)
    meander = 0.08 * np.sin(r_f * 0.018) + 0.04 * np.cos(r_f * 0.035)
    river_ang = ang_k + meander
    d_th = np.abs(np.arctan2(np.sin(m_Theta - river_ang), np.cos(m_Theta - river_ang)))
    m_dist_to_rivers = np.minimum(m_dist_to_rivers, m_R * d_th)

m_w_river = np.clip((m_dist_to_rivers - 8.0) / 14.0, 0.0, 1.0)
m_w_river = m_w_river * m_w_river * (3.0 - 2.0 * m_w_river)

m_dist_caldera = np.sqrt((m_X - CALDERA_X)**2 + (m_Z - CALDERA_Z)**2)
m_caldera_core = np.clip(1.0 - m_dist_caldera / 45.0, 0.0, 1.0)

# Land RGB
land_R = m_w_forest * 0.16 + m_w_meadow * 0.28 + m_w_desert * 0.86 + m_w_volc * 0.20
land_G = m_w_forest * 0.48 + m_w_meadow * 0.62 + m_w_desert * 0.64 + m_w_volc * 0.19
land_B = m_w_forest * 0.12 + m_w_meadow * 0.18 + m_w_desert * 0.35 + m_w_volc * 0.26

# Purple caldera glow
land_R = land_R * (1.0 - m_caldera_core) + 0.55 * m_caldera_core
land_G = land_G * (1.0 - m_caldera_core) + 0.15 * m_caldera_core
land_B = land_B * (1.0 - m_caldera_core) + 0.75 * m_caldera_core

# Apply beach along shoreline
m_beach_mask = np.clip((m_R - (m_R_coast - 45.0)) / 40.0, 0.0, 1.0) * (1.0 - np.clip((m_R - m_R_coast) / 10.0, 0.0, 1.0))
land_R = land_R * (1.0 - m_beach_mask) + 0.82 * m_beach_mask
land_G = land_G * (1.0 - m_beach_mask) + 0.74 * m_beach_mask
land_B = land_B * (1.0 - m_beach_mask) + 0.50 * m_beach_mask

# Apply river turquoise water
land_R = land_R * m_w_river + (1.0 - m_w_river) * 0.15
land_G = land_G * m_w_river + (1.0 - m_w_river) * 0.65
land_B = land_B * m_w_river + (1.0 - m_w_river) * 0.85

# Apply Kingdom center flat circle
m_kingdom_mask = np.clip((m_R - 325.0) / 20.0, 0.0, 1.0)
land_R = land_R * m_kingdom_mask + (1.0 - m_kingdom_mask) * 0.75
land_G = land_G * m_kingdom_mask + (1.0 - m_kingdom_mask) * 0.62
land_B = land_B * m_kingdom_mask + (1.0 - m_kingdom_mask) * 0.42

# Ocean mask
m_ocean_mask = np.clip((m_R - m_R_coast) / 25.0, 0.0, 1.0)
m_ocean_mask = m_ocean_mask * m_ocean_mask * (3.0 - 2.0 * m_ocean_mask)

map_R = land_R * (1.0 - m_ocean_mask) + 0.08 * m_ocean_mask
map_G = land_G * (1.0 - m_ocean_mask) + 0.32 * m_ocean_mask
map_B = land_B * (1.0 - m_ocean_mask) + 0.72 * m_ocean_mask
map_A = np.ones_like(map_R)

master_map_tex = np.clip(np.dstack((map_R, map_G, map_B, map_A)), 0.0, 1.0).astype(np.float32)
save_image_from_array("T_Terrain_MasterColorMap", master_map_tex, "T_Terrain_MasterColorMap.png")

# G. Biome Splat Map (4 channels: R=Grass/Forest, G=Desert, B=Volcanic, A=Beach/River)
splat_R = (m_w_forest + m_w_meadow) * (1.0 - m_beach_mask) * m_w_river
splat_G = m_w_desert * (1.0 - m_beach_mask) * m_w_river
splat_B = m_w_volc * (1.0 - m_beach_mask) * m_w_river
splat_A = np.clip(m_beach_mask + (1.0 - m_w_river), 0.0, 1.0)

splat_tex = np.clip(np.dstack((splat_R, splat_G, splat_B, splat_A)), 0.0, 1.0).astype(np.float32)
save_image_from_array("T_Terrain_BiomeSplatMap", splat_tex, "T_Terrain_BiomeSplatMap.png")

print(">>> ALL ASSETS GENERATED SUCCESSFULLY!")

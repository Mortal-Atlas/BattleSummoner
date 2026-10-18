import bpy
import numpy as np
import os
import math

OUTPUT_DIR = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Textures\Metropolis"
os.makedirs(OUTPUT_DIR, exist_ok=True)

def save_image_numpy(arr_rgba, filename):
    """
    arr_rgba: numpy float array shape (H, W, 4) in range [0, 1]
    """
    h, w, c = arr_rgba.shape
    img_name = os.path.splitext(filename)[0]
    img = bpy.data.images.new(img_name, width=w, height=h, alpha=True, float_buffer=False)
    # Blender expects flattened 1D array row by row
    img.pixels = arr_rgba.flatten().tolist()
    out_path = os.path.join(OUTPUT_DIR, filename)
    img.filepath_raw = out_path
    img.file_format = 'PNG'
    img.save()
    print(f"Saved texture: {out_path}")

# 1. Seamless Flagstone / Limestone Paving (Ashlar Pavers)
def generate_flagstone_paving(size=512):
    arr = np.zeros((size, size, 4), dtype=np.float32)
    # Grid of stones: 8 rows, staggered
    rows = 8
    cols = 8
    row_h = size // rows
    col_w = size // cols
    
    # Base stone color (warm light limestone)
    base_color = np.array([0.76, 0.74, 0.70, 1.0])
    mortar_color = np.array([0.38, 0.36, 0.34, 1.0])
    
    y_coords, x_coords = np.mgrid[0:size, 0:size]
    row_idx = y_coords // row_h
    
    # Stagger alternate rows by half a column width
    stagger = (row_idx % 2) * (col_w // 2)
    shifted_x = (x_coords + stagger) % size
    col_idx = shifted_x // col_w
    
    # Distance to mortar edges
    y_in_row = y_coords % row_h
    x_in_col = shifted_x % col_w
    
    edge_y = np.minimum(y_in_row, row_h - y_in_row)
    edge_x = np.minimum(x_in_col, col_w - x_in_col)
    min_dist_to_edge = np.minimum(edge_y, edge_x)
    
    # Mortar groove threshold
    mortar_width = 4.0
    mortar_mask = np.clip((min_dist_to_edge) / mortar_width, 0.0, 1.0)
    
    # Bevel effect near edge
    bevel = np.clip(min_dist_to_edge / (mortar_width * 3.5), 0.0, 1.0)
    
    # Random variation per stone
    stone_seed = ((row_idx * 17 + col_idx * 31) % 100) / 100.0
    stone_shade = 0.88 + stone_seed * 0.22
    
    # Subtle noise
    noise = np.random.uniform(0.96, 1.04, (size, size))
    
    for c in range(3):
        stone_c = base_color[c] * stone_shade * bevel * noise
        arr[:, :, c] = stone_c * mortar_mask + mortar_color[c] * (1.0 - mortar_mask)
    arr[:, :, 3] = 1.0
    
    save_image_numpy(arr, "tex_metropolis_flagstone_paving.png")

# 2. Seamless Linear Cobblestone (Clean rows, NO circles/fans)
def generate_linear_cobblestone(size=512):
    arr = np.zeros((size, size, 4), dtype=np.float32)
    rows = 16
    cols = 16
    row_h = size // rows
    col_w = size // cols
    
    base_color = np.array([0.62, 0.60, 0.58, 1.0])
    mortar_color = np.array([0.28, 0.27, 0.26, 1.0])
    
    y_coords, x_coords = np.mgrid[0:size, 0:size]
    row_idx = y_coords // row_h
    
    stagger = (row_idx % 2) * (col_w // 2)
    shifted_x = (x_coords + stagger) % size
    col_idx = shifted_x // col_w
    
    y_in_row = y_coords % row_h
    x_in_col = shifted_x % col_w
    
    edge_y = np.minimum(y_in_row, row_h - y_in_row)
    edge_x = np.minimum(x_in_col, col_w - x_in_col)
    dist_edge = np.minimum(edge_y, edge_x)
    
    mortar_w = 3.0
    mortar_mask = np.clip(dist_edge / mortar_w, 0.0, 1.0)
    
    # Rounded cobble dome effect
    norm_x = (x_in_col - col_w * 0.5) / (col_w * 0.5)
    norm_y = (y_in_row - row_h * 0.5) / (row_h * 0.5)
    dome = np.clip(1.0 - (norm_x**2 + norm_y**2) * 0.45, 0.5, 1.0)
    
    stone_seed = ((row_idx * 29 + col_idx * 43) % 100) / 100.0
    stone_shade = 0.85 + stone_seed * 0.3
    noise = np.random.uniform(0.95, 1.05, (size, size))
    
    for c in range(3):
        val = base_color[c] * stone_shade * dome * noise
        arr[:, :, c] = val * mortar_mask + mortar_color[c] * (1.0 - mortar_mask)
    arr[:, :, 3] = 1.0
    
    save_image_numpy(arr, "tex_metropolis_linear_cobble.png")

# 3. Glowing Anti-Gravity Flight Lane / Arcane Conduit
def generate_antigravity_lane(size=512):
    arr = np.zeros((size, size, 4), dtype=np.float32)
    y_coords, x_coords = np.mgrid[0:size, 0:size]
    
    # Base dark road
    arr[:, :, 0] = 0.22
    arr[:, :, 1] = 0.24
    arr[:, :, 2] = 0.28
    arr[:, :, 3] = 1.0
    
    # Centerline glowing cyan conduit (width ~24 pixels)
    center_dist = np.abs(x_coords - size * 0.5)
    center_beam = np.clip(1.0 - center_dist / 14.0, 0.0, 1.0) ** 2
    
    # Dual flanking guide rails
    rail_dist1 = np.abs(x_coords - (size * 0.5 - 60))
    rail_dist2 = np.abs(x_coords - (size * 0.5 + 60))
    rails = (np.clip(1.0 - rail_dist1 / 8.0, 0.0, 1.0) + np.clip(1.0 - rail_dist2 / 8.0, 0.0, 1.0)) ** 2
    
    # Repeating forward chevron runes along Y (period 64)
    y_mod = y_coords % 64
    chevron_v = np.abs((x_coords - size * 0.5) * 0.8)
    chevron_mask = np.clip(1.0 - np.abs(y_mod - chevron_v - 16) / 5.0, 0.0, 1.0)
    chevron_mask *= np.clip(1.0 - center_dist / 70.0, 0.0, 1.0)
    
    glow_cyan = np.clip(center_beam * 1.5 + rails * 0.9 + chevron_mask * 1.2, 0.0, 2.0)
    
    # Apply cyan glow
    arr[:, :, 0] += glow_cyan * 0.1
    arr[:, :, 1] += glow_cyan * 0.85
    arr[:, :, 2] += glow_cyan * 1.0
    arr = np.clip(arr, 0.0, 1.0)
    
    save_image_numpy(arr, "tex_metropolis_antigravity_lane.png")

# 4. Farmland / Agri Plots (Tilled soil furrows and crop sprouts)
def generate_farmland(size=512):
    arr = np.zeros((size, size, 4), dtype=np.float32)
    y_coords, x_coords = np.mgrid[0:size, 0:size]
    
    # Furrow wave along X
    furrow = np.sin(x_coords * (2.0 * math.pi / 32.0)) * 0.5 + 0.5
    soil_dark = np.array([0.28, 0.18, 0.11, 1.0])
    soil_light = np.array([0.42, 0.28, 0.18, 1.0])
    crop_green = np.array([0.48, 0.65, 0.22, 1.0])
    
    # Crop patches along ridge of furrow
    crop_spots = (furrow > 0.75).astype(np.float32) * (np.sin(y_coords * (2.0 * math.pi / 24.0)) > 0.0).astype(np.float32)
    
    for c in range(3):
        soil_c = soil_dark[c] + (soil_light[c] - soil_dark[c]) * furrow
        arr[:, :, c] = soil_c * (1.0 - crop_spots * 0.7) + crop_green[c] * (crop_spots * 0.7)
    arr[:, :, 3] = 1.0
    
    save_image_numpy(arr, "tex_metropolis_farmland.png")

# 5. Volcanic Basalt with Purple Rift Energy (NE Sector)
def generate_volcanic_basalt(size=512):
    arr = np.zeros((size, size, 4), dtype=np.float32)
    y_coords, x_coords = np.mgrid[0:size, 0:size]
    
    # Dark basalt rock
    noise1 = np.random.uniform(0.12, 0.20, (size, size))
    arr[:, :, 0] = noise1 * 0.95
    arr[:, :, 1] = noise1 * 0.92
    arr[:, :, 2] = noise1 * 1.05
    arr[:, :, 3] = 1.0
    
    # Crack network (Voronoi/perlin approx with sine waves)
    fissures = np.abs(np.sin(x_coords * 0.04 + np.cos(y_coords * 0.03) * 2.5) * np.sin(y_coords * 0.04 + np.sin(x_coords * 0.03) * 2.5))
    fissure_mask = np.clip((0.08 - fissures) / 0.08, 0.0, 1.0) ** 2
    
    arr[:, :, 0] += fissure_mask * 0.75
    arr[:, :, 1] += fissure_mask * 0.15
    arr[:, :, 2] += fissure_mask * 0.95
    arr = np.clip(arr, 0.0, 1.0)
    
    save_image_numpy(arr, "tex_volcanic_basalt_rift.png")

print("Starting generation of seamless metropolis textures...")
generate_flagstone_paving()
generate_linear_cobblestone()
generate_antigravity_lane()
generate_farmland()
generate_volcanic_basalt()
print("All seamless textures created successfully!")

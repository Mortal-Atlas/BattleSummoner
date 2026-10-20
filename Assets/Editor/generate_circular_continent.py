"""
Master Procedural Generator: Level 1 Macro-Environment ("The Circular Continent")
Enhanced with organic branching hydrology, multiple connected lakes,
architectural lake bridges, and professional topological landscaping.
"""

import bpy
import bmesh
import math
import mathutils
import os
import random

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if "Scene" not in bpy.data.scenes:
        bpy.data.scenes.new("Scene")

def create_pbr_material(name, base_color, roughness=0.6, metallic=0.0, emission=None, emission_strength=1.0, is_water=False):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    out = nodes.new(type='ShaderNodeOutputMaterial')
    out.location = (400, 0)
    
    bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    bsdf.location = (0, 0)
    
    bsdf.inputs['Base Color'].default_value = (*base_color[:3], 1.0)
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    
    if is_water:
        bsdf.inputs['Roughness'].default_value = 0.05
        bsdf.inputs['Metallic'].default_value = 0.05
        bsdf.inputs['IOR'].default_value = 1.333
        
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*emission[:3], 1.0)
        bsdf.inputs['Emission Strength'].default_value = emission_strength
        
    links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return mat

def create_terrain_splat_material():
    mat = bpy.data.materials.new(name="M_Terrain_Macro_Splat")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    out = nodes.new(type='ShaderNodeOutputMaterial')
    out.location = (850, 0)
    
    bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    bsdf.location = (550, 0)
    bsdf.inputs['Roughness'].default_value = 0.80
    
    attr = nodes.new(type='ShaderNodeAttribute')
    attr.location = (-650, 200)
    attr.attribute_name = "Col_BiomeSplat"
    
    sep = nodes.new(type='ShaderNodeSeparateColor')
    sep.location = (-420, 200)
    links.new(attr.outputs['Color'], sep.inputs['Color'])
    
    tex_coord = nodes.new(type='ShaderNodeTexCoord')
    tex_coord.location = (-950, -200)
    
    noise_grass = nodes.new(type='ShaderNodeTexNoise')
    noise_grass.location = (-700, -150)
    noise_grass.inputs['Scale'].default_value = 0.04
    noise_grass.inputs['Detail'].default_value = 3.0
    links.new(tex_coord.outputs['Object'], noise_grass.inputs['Vector'])
    
    mix_grass = nodes.new(type='ShaderNodeMix')
    mix_grass.data_type = 'RGBA'
    mix_grass.location = (-450, -150)
    mix_grass.inputs[6].default_value = (0.16, 0.42, 0.12, 1.0) # Forest Emerald
    mix_grass.inputs[7].default_value = (0.36, 0.48, 0.18, 1.0) # Olive Highland
    links.new(noise_grass.outputs['Fac'], mix_grass.inputs[0])
    
    wave_sand = nodes.new(type='ShaderNodeTexWave')
    wave_sand.location = (-700, -400)
    wave_sand.inputs['Scale'].default_value = 0.12
    wave_sand.inputs['Distortion'].default_value = 2.0
    links.new(tex_coord.outputs['Object'], wave_sand.inputs['Vector'])
    
    mix_sand = nodes.new(type='ShaderNodeMix')
    mix_sand.data_type = 'RGBA'
    mix_sand.location = (-450, -400)
    mix_sand.inputs[6].default_value = (0.84, 0.68, 0.40, 1.0)
    mix_sand.inputs[7].default_value = (0.70, 0.52, 0.30, 1.0)
    links.new(wave_sand.outputs['Fac'], mix_sand.inputs[0])
    
    mix1 = nodes.new(type='ShaderNodeMix')
    mix1.data_type = 'RGBA'
    mix1.location = (-150, 150)
    mix1.inputs[7].default_value = (0.26, 0.25, 0.24, 1.0)
    links.new(mix_grass.outputs[2], mix1.inputs[6])
    links.new(sep.outputs['Green'], mix1.inputs[0])
    
    mix2 = nodes.new(type='ShaderNodeMix')
    mix2.data_type = 'RGBA'
    mix2.location = (80, 100)
    links.new(mix1.outputs[2], mix2.inputs[6])
    links.new(mix_sand.outputs[2], mix2.inputs[7])
    links.new(sep.outputs['Blue'], mix2.inputs[0])
    
    mix3 = nodes.new(type='ShaderNodeMix')
    mix3.data_type = 'RGBA'
    mix3.location = (300, 50)
    mix3.inputs[7].default_value = (0.94, 0.96, 0.99, 1.0)
    links.new(mix2.outputs[2], mix3.inputs[6])
    links.new(attr.outputs['Alpha'], mix3.inputs[0])
    
    links.new(mix3.outputs[2], bsdf.inputs['Base Color'])
    links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return mat

def create_all_materials():
    mats = {}
    mats['Terrain_Splat'] = create_terrain_splat_material()
    mats['Citadel_Stone'] = create_pbr_material('M_Citadel_Stone', (0.58, 0.56, 0.54), roughness=0.68)
    mats['Citadel_Roof'] = create_pbr_material('M_Citadel_Roof', (0.16, 0.20, 0.28), roughness=0.45)
    mats['Wood_Timber'] = create_pbr_material('M_Wood_Timber', (0.36, 0.24, 0.15), roughness=0.80)
    mats['Water_Ocean'] = create_pbr_material('M_Water_Ocean', (0.04, 0.22, 0.44), roughness=0.04, is_water=True)
    mats['Water_River'] = create_pbr_material('M_Water_River', (0.10, 0.48, 0.65), roughness=0.06, is_water=True)
    mats['Water_Ice'] = create_pbr_material('M_Water_Ice', (0.65, 0.85, 0.95), roughness=0.15)
    mats['Volcanic_Obsidian'] = create_pbr_material('M_Volcanic_Obsidian', (0.07, 0.06, 0.07), roughness=0.35, metallic=0.3)
    mats['Foliage_Pine'] = create_pbr_material('M_Foliage_Pine', (0.10, 0.26, 0.12), roughness=0.85)
    mats['Foliage_Oak'] = create_pbr_material('M_Foliage_Oak', (0.18, 0.46, 0.15), roughness=0.78)
    mats['Foliage_Palm'] = create_pbr_material('M_Foliage_Palm', (0.30, 0.52, 0.14), roughness=0.72)
    mats['Boss_Purple'] = create_pbr_material('M_Boss_Purple', (0.54, 0.17, 0.89), roughness=0.2, emission=(0.68, 0.14, 1.0), emission_strength=26.0)
    mats['MiniBoss_Red'] = create_pbr_material('M_MiniBoss_Red', (0.95, 0.10, 0.15), roughness=0.2, emission=(1.0, 0.04, 0.08), emission_strength=20.0)
    mats['Road_Cobble'] = create_pbr_material('M_Road_Cobble', (0.42, 0.40, 0.38), roughness=0.72)
    mats['Farmland_Soil'] = create_pbr_material('M_Farmland_Soil', (0.34, 0.26, 0.16), roughness=0.92)
    return mats

# =========================================================================
# SPLINE & RIVER UTILITIES
# =========================================================================
def sample_bezier_polyline(p0, p1, p2, p3, steps=16, z0=0.0, z1=0.0, w0=18.0, w1=18.0):
    pts = []
    for i in range(steps):
        t = i / float(steps - 1)
        it = 1.0 - t
        x = (it**3)*p0[0] + 3*(it**2)*t*p1[0] + 3*it*(t**2)*p2[0] + (t**3)*p3[0]
        y = (it**3)*p0[1] + 3*(it**2)*t*p1[1] + 3*it*(t**2)*p2[1] + (t**3)*p3[1]
        z = z0 + (z1 - z0) * t
        w = w0 + (w1 - w0) * t
        pts.append((x, y, z, w))
    return pts

def dist_to_segment(px, py, x1, y1, x2, y2):
    dx, dy = x2 - x1, y2 - y1
    l2 = dx*dx + dy*dy
    if l2 < 1e-6:
        return math.hypot(px - x1, py - y1), 0.0
    t = max(0.0, min(1.0, ((px - x1)*dx + (py - y1)*dy) / l2))
    proj_x = x1 + t * dx
    proj_y = y1 + t * dy
    return math.hypot(px - proj_x, py - proj_y), t

def eval_river_corridor(px, py, polyline):
    # Quick bounding box
    min_x = min(p[0] for p in polyline) - 35.0
    max_x = max(p[0] for p in polyline) + 35.0
    min_y = min(p[1] for p in polyline) - 35.0
    max_y = max(p[1] for p in polyline) + 35.0
    if px < min_x or px > max_x or py < min_y or py > max_y:
        return None, None
        
    best_dist = 1e9
    best_z = 0.0
    best_width = 18.0
    for i in range(len(polyline) - 1):
        pA, pB = polyline[i], polyline[i+1]
        d, t = dist_to_segment(px, py, pA[0], pA[1], pB[0], pB[1])
        if d < best_dist:
            best_dist = d
            best_z = pA[2] + (pB[2] - pA[2]) * t
            best_width = pA[3] + (pB[3] - pA[3]) * t
            
    half_w = best_width * 0.5
    if best_dist < half_w:
        # Inside riverbed
        norm_d = best_dist / half_w
        bed_h = best_z + 3.8 * (norm_d ** 2)
        return bed_h, norm_d
    return None, None

# Pre-generate river splines
RIVERS = {
    # 1. Glacial Gorge Upper (Glacial lake down to fork)
    'Glacial_Upper': [
        (-20.0, 360.0, 35.0, 24.0),
        (-12.0, 320.0, 29.0, 22.0),
        (-5.0, 280.0, 24.0, 20.0),
        (0.0, 240.0, 19.5, 20.0),
    ],
    # 2. Fork A: Glacial Main continuing to Central Moat
    'Glacial_To_Moat': [
        (0.0, 240.0, 19.5, 20.0),
        (0.0, 190.0, 15.0, 18.0),
        (0.0, 140.0, 11.5, 18.0),
        (0.0, 104.0, 8.0, 18.0),
    ],
    # 3. Fork B: Glacial to Highland Tarn Lake
    'Glacial_To_Highland': sample_bezier_polyline(
        (0.0, 240.0), (-35.0, 245.0), (-75.0, 242.0), (-110.0, 235.0),
        steps=18, z0=19.5, z1=14.5, w0=18.0, w1=18.0
    ),
    # 4. Highland Tarn Cascade (mountain brook outflow)
    'Highland_Cascade': sample_bezier_polyline(
        (-110.0, 235.0), (-135.0, 215.0), (-155.0, 195.0), (-180.0, 175.0),
        steps=16, z0=14.5, z1=9.5, w0=16.0, w1=16.0
    ),
    # 5. Lake Brestuoil to Emerald Lily Lake
    'Brestuoil_To_Emerald': sample_bezier_polyline(
        (295.0, 255.0), (265.0, 275.0), (240.0, 295.0), (210.0, 310.0),
        steps=18, z0=17.5, z1=14.0, w0=20.0, w1=20.0
    ),
    # 6. Lake Brestuoil Canal to Moat
    'Brestuoil_Canal': sample_bezier_polyline(
        (280.0, 220.0), (230.0, 150.0), (160.0, 70.0), (95.0, 20.0),
        steps=22, z0=17.5, z1=8.5, w0=18.0, w1=18.0
    ),
    # 7. Southern River Upper (Moat to Southern Fork)
    'Southern_Upper': [
        (0.0, -104.0, 8.0, 20.0),
        (3.0, -135.0, 8.5, 20.0),
        (6.0, -165.0, 9.0, 20.0),
        (10.0, -190.0, 9.2, 22.0),
    ],
    # 8. Southern Fork A: To Silvermirror Lake
    'Southern_To_Silvermirror': sample_bezier_polyline(
        (10.0, -190.0), (35.0, -205.0), (60.0, -220.0), (85.0, -235.0),
        steps=18, z0=9.2, z1=8.8, w0=22.0, w1=24.0
    ),
    # 9. Silvermirror Lake Outlet Brook
    'Silvermirror_Outlet': sample_bezier_polyline(
        (85.0, -235.0), (115.0, -255.0), (140.0, -275.0), (170.0, -295.0),
        steps=18, z0=8.8, z1=4.0, w0=18.0, w1=16.0
    ),
    # 10. Southern Fork B: Delta Gorge into Ocean
    'Southern_Delta_Gorge': [
        (10.0 + 25.0 * math.sin(y * 0.02), y, 9.2 - ((abs(y) - 190.0) / 240.0) * 8.5, 22.0)
        for y in [float(v) for v in range(-190, -435, -15)]
    ]
}

def evaluate_continent_height(x, y):
    r = math.hypot(x, y)
    theta = math.atan2(y, x)
    deg = (math.degrees(theta) + 360.0) % 360.0
    
    # 1. SOUTHEAST OFFSHORE BASTION ISLAND (Aligned with Harbor Watchtower)
    dx_isl, dy_isl = x - 370.0, y - (-180.0)
    r_isl = math.hypot(dx_isl, dy_isl)
    if r_isl < 78.0:
        isl_falloff = 1.0 - (r_isl / 78.0)
        return max(0.5, 17.5 * (isl_falloff ** 1.35))
        
    # Main continent coastline
    r_coast = 480.0 + 26.0 * math.sin(5.0 * theta) + 18.0 * math.cos(8.0 * theta)
    if 290.0 < deg < 350.0:
        r_coast = 350.0 + 25.0 * math.cos(3.0 * theta)
        
    dist_coast = r_coast - r
    if dist_coast <= 0:
        return max(-35.0, dist_coast * 0.45)
        
    coast_blend = min(1.0, dist_coast / 45.0)
    
    # CENTRAL CITADEL & MOAT
    if r < 72.0:
        return 16.0 - (r / 72.0) * 1.5
    elif r < 104.0:
        dist_m = abs(r - 88.0) / 16.0
        return 4.5 + 9.5 * (dist_m ** 2)
    elif r < 118.0:
        return 15.5 - ((r - 104.0) / 14.0) * 1.5
        
    z = 13.0
    
    # NORTHWEST: MAGIO VOLTANIC VOLCANIA
    dx_v, dy_v = x - (-260.0), y - 270.0
    r_v = math.hypot(dx_v, dy_v)
    if r_v < 180.0:
        v_prof = max(0.0, 1.0 - (r_v / 180.0))
        cone = 14.0 + 115.0 * (v_prof ** 1.35)
        cone += 9.0 * math.sin(7.0 * math.atan2(dy_v, dx_v)) * v_prof
        caldera_r = 46.0
        if r_v < caldera_r:
            cf = (r_v / caldera_r) ** 2
            cone = 78.0 + 40.0 * cf
        z = max(z, cone)
        
    # NORTH: FROSTHOLD WASTES
    if y > 200.0 and -140.0 < x < 140.0:
        ny = (y - 200.0) / 300.0
        glacier = 14.0 + 95.0 * ny * (1.0 + 0.28 * math.sin(x * 0.08))
        d_glake = math.hypot(x - (-20.0), y - 380.0)
        if d_glake < 60.0:
            glacier = 38.0 + 22.0 * (d_glake / 60.0)
        z = max(z, glacier)
        
    # WEST & SOUTHWEST: THE RUSTING DESOLATION
    if x < -90.0 and -320.0 < y < 150.0:
        nx = abs(x) / 500.0
        dunes = 7.0 * math.sin(x * 0.045 + y * 0.025) + 4.0 * math.cos(x * 0.09)
        desert = 8.0 + 18.0 * nx + max(0.0, dunes)
        d_o1 = math.hypot(x - (-360.0), y - 30.0)
        if d_o1 < 40.0:
            desert = 3.0 if d_o1 < 26.0 else 3.0 + 8.5 * ((d_o1 - 26.0) / 14.0)
        d_o2 = math.hypot(x - (-300.0), y - (-70.0))
        if d_o2 < 36.0:
            desert = 3.0 if d_o2 < 24.0 else 3.0 + 8.0 * ((d_o2 - 24.0) / 12.0)
        d_o3 = math.hypot(x - (-380.0), y - (-160.0))
        if d_o3 < 38.0:
            desert = 2.5 if d_o3 < 24.0 else 2.5 + 8.5 * ((d_o3 - 24.0) / 14.0)
        trans_d = min(1.0, (-x - 90.0) / 30.0)
        z = z * (1.0 - trans_d) + desert * trans_d
        
    # HIGHLAND TERRACES (SW of Citadel)
    dx_t, dy_t = x - (-120.0), y - (-240.0)
    r_t = math.hypot(dx_t, dy_t)
    if r_t < 160.0 and x > -220.0 and y < -120.0:
        t_prof = 1.0 - (r_t / 160.0)
        raw_t = 12.0 + 54.0 * t_prof
        step_h = 14.0
        quant_t = math.floor(raw_t / step_h) * step_h + min(step_h, (raw_t % step_h) * 0.22)
        z = max(z, quant_t)
        
    # NORTHEAST: TEMPERATE FOREST & BRESTUOIL LAKE
    if x > 90.0 and y > 60.0:
        f_hills = 14.0 + 28.0 * math.sin(x * 0.02 + y * 0.015) + 12.0 * math.cos(y * 0.03)
        z = max(z, f_hills)
        
    # SOUTHEAST: SEAWARD PORT TOWN & HARBOR BASIN
    if 150.0 < x < 310.0 and -250.0 < y < -75.0:
        h_dx = x - 248.0
        h_dy = (y - (-160.0)) * 0.75
        h_dist = math.hypot(h_dx, h_dy)
        if h_dist < 92.0:
            if x <= 195.0:
                town_h = 9.2 + 3.8 * ((195.0 - x) / 45.0)
            elif x <= 232.0:
                town_h = 5.0 + 4.2 * ((232.0 - x) / 37.0)
            elif x <= 256.0:
                town_h = 2.5 + 2.5 * ((256.0 - x) / 24.0)
            else:
                town_h = 2.5 - 6.5 * min(1.3, (x - 256.0) / 28.0)
            y_cup = 0.75 * ((h_dy / 65.0) ** 2)
            port_z = town_h + y_cup
            h_blend = min(1.0, (92.0 - h_dist) / 18.0)
            z = z * (1.0 - h_blend) + port_z * h_blend
        
    # SOUTH FOREST & DELTA
    if 20.0 < x < 260.0 and y < -180.0 and x < 180.0:
        s_forest = 12.0 + 40.0 * (1.0 - (math.hypot(x - 140.0, y - (-310.0)) / 150.0))
        z = max(z, s_forest)
        
    # =========================================================================
    # LAKES CARVING
    # =========================================================================
    # 1. Lake Brestuoil Dual Basin (with central narrows for bridge)
    d_b1 = math.hypot(x - 335.0, y - 275.0)
    d_b2 = math.hypot(x - 295.0, y - 240.0)
    d_bn = math.hypot(x - 315.0, y - 258.0)
    if d_b1 < 42.0:
        lake_bed = 16.5 if d_b1 < 30.0 else 16.5 + 4.5 * ((d_b1 - 30.0) / 12.0)
        z = min(z, lake_bed)
    if d_b2 < 38.0:
        lake_bed = 16.5 if d_b2 < 28.0 else 16.5 + 4.5 * ((d_b2 - 28.0) / 10.0)
        z = min(z, lake_bed)
    if d_bn < 24.0:
        # Isthmus neck channel between basins (spanned by Lake Brestuoil Bridge)
        lake_bed = 16.8 + 3.8 * ((d_bn / 24.0) ** 2)
        z = min(z, lake_bed)
        
    # 2. Highland Tarn Lake (North-East Highland Basin)
    d_ht = math.hypot(x - (-110.0), y - 235.0)
    if d_ht < 36.0:
        tarn_bed = 13.5 if d_ht < 24.0 else 13.5 + 5.0 * ((d_ht - 24.0) / 12.0)
        z = min(z, tarn_bed)
        
    # 3. Emerald Lily Lake (North-East Lily Wetland Lake)
    d_em = math.hypot(x - 210.0, y - 310.0)
    if d_em < 38.0:
        lily_bed = 13.5 if d_em < 26.0 else 13.5 + 4.5 * ((d_em - 26.0) / 12.0)
        z = min(z, lily_bed)
        
    # 4. Silvermirror Lake (South-West Broad Lake)
    d_sm = math.hypot(x - 85.0, y - (-235.0))
    if d_sm < 46.0:
        sm_bed = 8.5 if d_sm < 34.0 else 8.5 + 4.0 * ((d_sm - 34.0) / 12.0)
        z = min(z, sm_bed)
        
    # =========================================================================
    # BRANCHING RIVER CANYONS & BED CARVING
    # =========================================================================
    for r_name, poly in RIVERS.items():
        bed_h, _ = eval_river_corridor(x, y, poly)
        if bed_h is not None:
            z = min(z, bed_h)
            
    # Western Desert Runoff River
    if x < -95.0 and 40.0 < y < 220.0:
        r_line = 110.0 - 0.45 * (x - (-95.0))
        d_wr = abs(y - r_line)
        if d_wr < 22.0:
            z = min(z, 7.5 + 4.5 * (d_wr / 22.0))
            
    return max(0.5, z) * coast_blend

def build_smooth_terrain(mats):
    print("Building high-resolution smooth macro-terrain with organic hydrology...")
    res = 280
    extent = 540.0
    
    mesh = bpy.data.meshes.new("Mesh_Circular_Continent")
    obj = bpy.data.objects.new("Circular_Continent_Terrain", mesh)
    bpy.context.collection.objects.link(obj)
    
    bm = bmesh.new()
    step = (extent * 2.0) / (res - 1)
    grid_verts = []
    
    for iy in range(res):
        row = []
        y = -extent + iy * step
        for ix in range(res):
            x = -extent + ix * step
            z = evaluate_continent_height(x, y)
            row.append(bm.verts.new((x, y, z)))
        grid_verts.append(row)
        
    bm.verts.ensure_lookup_table()
    for iy in range(res - 1):
        for ix in range(res - 1):
            v0 = grid_verts[iy][ix]
            v1 = grid_verts[iy][ix + 1]
            v2 = grid_verts[iy + 1][ix + 1]
            v3 = grid_verts[iy + 1][ix]
            bm.faces.new((v0, v1, v2, v3))
            
    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        f.smooth = True
        
    bm.normal_update()
    
    color_layer = bm.loops.layers.color.new("Col_BiomeSplat")
    for face in bm.faces:
        for loop in face.loops:
            v = loop.vert
            x, y, z = v.co.x, v.co.y, v.co.z
            r = math.hypot(x, y)
            
            w_grass = 1.0
            w_rock = 0.0
            w_sand = 0.0
            w_snow = 0.0
            
            # Slope cliff rock
            nz = loop.vert.normal.z
            if nz < 0.65:
                w_rock = max(w_rock, (0.65 - nz) / 0.35)
                
            # Volcanic Sector
            d_volcano = math.hypot(x - (-260.0), y - 270.0)
            if d_volcano < 170.0:
                v_fac = 1.0 - (d_volcano / 170.0)
                w_rock = max(w_rock, min(1.0, 0.45 + 0.65 * v_fac))
                
            # Frosthold Snow
            if y > 180.0 and -150.0 < x < 150.0:
                s_fac = min(1.0, (y - 180.0) / 140.0)
                if z > 25.0:
                    w_snow = max(w_snow, s_fac)
                    
            # Desert Sand
            if x < -80.0 and -340.0 < y < 140.0:
                d_fac = min(1.0, (-x - 80.0) / 60.0)
                w_sand = max(w_sand, d_fac)
                
            # Sandy / Gravel Shores along lakes and rivers
            if 0.5 <= z < 4.0 and r > 320.0:
                beach_wt = (4.0 - z) / 3.5
                w_sand = max(w_sand, min(1.0, beach_wt))
                
            # Lake Shorelines Sandy Fringe
            lake_dists = [
                (math.hypot(x - 335.0, y - 275.0), 44.0), # Brestuoil A
                (math.hypot(x - 295.0, y - 240.0), 40.0), # Brestuoil B
                (math.hypot(x - (-110.0), y - 235.0), 38.0), # Highland Tarn
                (math.hypot(x - 210.0, y - 310.0), 40.0), # Emerald Lily
                (math.hypot(x - 85.0, y - (-235.0)), 48.0), # Silvermirror
            ]
            # Seaward Port Town Harbor & Waterfront Paving/Sand
            if 170.0 < x < 285.0 and -230.0 < y < -90.0:
                h_dx = x - 248.0
                h_dy = (y - (-160.0)) * 0.75
                h_dist = math.hypot(h_dx, h_dy)
                if h_dist < 80.0:
                    w_sand = max(w_sand, min(1.0, (80.0 - h_dist) / 36.0))
                    
            if d_volcano < 170.0:
                w_sand = 0.0
                
            w_rock = min(1.0, w_rock)
            w_snow = min(1.0 - w_rock, w_snow)
            w_sand = min(1.0 - (w_rock + w_snow), w_sand)
            w_grass = max(0.0, 1.0 - (w_rock + w_snow + w_sand))
            
            loop[color_layer] = (w_grass, w_rock, w_sand, w_snow)
            
    bm.to_mesh(mesh)
    bm.free()
    
    mesh.materials.append(mats['Terrain_Splat'])
    return obj

def build_river_mesh_ribbon(name, polyline, mats, parent_obj):
    bm = bmesh.new()
    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    obj = bpy.data.objects.new(name, mesh)
    obj.parent = parent_obj
    bpy.context.collection.objects.link(obj)
    
    for i in range(len(polyline)):
        px, py, pz, pw = polyline[i]
        # Calculate normal vector in 2D
        if i < len(polyline) - 1:
            nx = -(polyline[i+1][1] - py)
            ny = polyline[i+1][0] - px
        else:
            nx = -(py - polyline[i-1][1])
            ny = px - polyline[i-1][0]
        l = math.hypot(nx, ny)
        if l > 1e-5:
            nx /= l
            ny /= l
        else:
            nx, ny = 1.0, 0.0
            
        half_w = pw * 0.5
        v_l = bm.verts.new((px - nx * half_w, py - ny * half_w, pz))
        v_r = bm.verts.new((px + nx * half_w, py + ny * half_w, pz))
        
    bm.verts.ensure_lookup_table()
    for i in range(len(polyline) - 1):
        v0 = bm.verts[i * 2]
        v1 = bm.verts[i * 2 + 1]
        v2 = bm.verts[(i + 1) * 2 + 1]
        v3 = bm.verts[(i + 1) * 2]
        bm.faces.new((v0, v1, v2, v3))
        
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(mats['Water_River'])
    return obj

def build_hydrology(mats):
    print("Building Expanded Branching Hydrology Network...")
    hydro_root = bpy.data.objects.new("Hydrology_Network_Root", None)
    hydro_root.location = (0.0, 0.0, 0.0)
    bpy.context.collection.objects.link(hydro_root)
    
    # 1. Circular Moat Water Ring (r: 72 to 104, Z = 10.0m)
    bm = bmesh.new()
    segs = 96
    moat_mesh = bpy.data.meshes.new("Mesh_Water_Moat")
    moat_obj = bpy.data.objects.new("Water_Central_Moat", moat_mesh)
    moat_obj.parent = hydro_root
    bpy.context.collection.objects.link(moat_obj)
    
    r_in, r_out, zw = 72.0, 104.0, 10.0
    iv, ov = [], []
    for i in range(segs):
        ang = (2.0 * math.pi * i) / segs
        ca, sa = math.cos(ang), math.sin(ang)
        iv.append(bm.verts.new((r_in * ca, r_in * sa, zw)))
        ov.append(bm.verts.new((r_out * ca, r_out * sa, zw)))
    for i in range(segs):
        inext = (i + 1) % segs
        bm.faces.new((iv[i], ov[i], ov[inext], iv[inext]))
    bm.to_mesh(moat_mesh)
    bm.free()
    moat_mesh.materials.append(mats['Water_River'])
    
    # 2. Lake Brestuoil Dual Basin (Z = 19.5m)
    bpy.ops.mesh.primitive_cylinder_add(radius=42.0, depth=0.8, location=(335.0, 275.0, 19.5))
    lake_b1 = bpy.context.active_object
    lake_b1.name = "Water_Lake_Brestuoil_NorthBasin"
    lake_b1.parent = hydro_root
    lake_b1.data.materials.append(mats['Water_River'])
    
    bpy.ops.mesh.primitive_cylinder_add(radius=38.0, depth=0.8, location=(295.0, 240.0, 19.5))
    lake_b2 = bpy.context.active_object
    lake_b2.name = "Water_Lake_Brestuoil_SouthBasin"
    lake_b2.parent = hydro_root
    lake_b2.data.materials.append(mats['Water_River'])
    
    # Channel connecting Brestuoil basins under the Stone Arch Bridge
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(315.0, 258.0, 19.5))
    lake_bn = bpy.context.active_object
    lake_bn.name = "Water_Lake_Brestuoil_Narrows"
    lake_bn.scale = (28.0, 32.0, 0.8)
    lake_bn.rotation_euler = (0, 0, 0.72)
    lake_bn.parent = hydro_root
    lake_bn.data.materials.append(mats['Water_River'])
    
    # 3. Highland Tarn Lake (Z = 16.5m)
    bpy.ops.mesh.primitive_cylinder_add(radius=34.0, depth=0.8, location=(-110.0, 235.0, 16.5))
    lake_ht = bpy.context.active_object
    lake_ht.name = "Water_Highland_Tarn"
    lake_ht.parent = hydro_root
    lake_ht.data.materials.append(mats['Water_River'])
    
    # 4. Emerald Lily Lake (Z = 16.0m)
    bpy.ops.mesh.primitive_cylinder_add(radius=36.0, depth=0.8, location=(210.0, 310.0, 16.0))
    lake_em = bpy.context.active_object
    lake_em.name = "Water_Emerald_Lily_Lake"
    lake_em.parent = hydro_root
    lake_em.data.materials.append(mats['Water_River'])
    
    # 5. Silvermirror Lake (Z = 9.2m)
    bpy.ops.mesh.primitive_cylinder_add(radius=44.0, depth=0.8, location=(85.0, -235.0, 9.2))
    lake_sm = bpy.context.active_object
    lake_sm.name = "Water_Silvermirror_Lake"
    lake_sm.parent = hydro_root
    lake_sm.data.materials.append(mats['Water_River'])
    
    # 6. Northern Glacial Headwater (Z = 38.0m)
    bpy.ops.mesh.primitive_cylinder_add(radius=48.0, depth=0.8, location=(-20.0, 380.0, 38.0))
    lake_g = bpy.context.active_object
    lake_g.name = "Water_Glacial_Headwater"
    lake_g.parent = hydro_root
    lake_g.data.materials.append(mats['Water_Ice'])
    
    # 7. Desert Oases
    oases = [
        ("Water_Oasis_North", (-360.0, 30.0, 5.5), 26.0),
        ("Water_Oasis_Central", (-300.0, -70.0, 5.0), 24.0),
        ("Water_Oasis_South", (-380.0, -160.0, 4.2), 24.0),
    ]
    for o_name, loc, rad in oases:
        bpy.ops.mesh.primitive_cylinder_add(radius=rad, depth=0.8, location=loc)
        o_obj = bpy.context.active_object
        o_obj.name = o_name
        o_obj.parent = hydro_root
        o_obj.data.materials.append(mats['Water_River'])
        
    # 8. All Branching River Mesh Ribbons
    for r_name, poly in RIVERS.items():
        build_river_mesh_ribbon(f"Water_River_{r_name}", poly, mats, hydro_root)
        
    # 9. Infinite Ocean Plane
    bpy.ops.mesh.primitive_plane_add(size=2600.0, location=(0.0, 0.0, 0.0))
    ocean = bpy.context.active_object
    ocean.name = "Water_Infinite_Ocean"
    ocean.parent = hydro_root
    ocean.data.materials.append(mats['Water_Ocean'])
    
    return hydro_root

def build_hollow_curtain_wall(name, radius, height, thickness, segments, mats, z_base=16.0):
    bm = bmesh.new()
    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    obj = bpy.data.objects.new(name, mesh)
    
    r_inner = radius - thickness / 2.0
    r_outer = radius + thickness / 2.0
    
    b_in, b_out = [], []
    t_in, t_out = [], []
    
    for i in range(segments):
        ang = (2.0 * math.pi * i) / segments
        ca, sa = math.cos(ang), math.sin(ang)
        b_in.append(bm.verts.new((r_inner * ca, r_inner * sa, z_base)))
        b_out.append(bm.verts.new((r_outer * ca, r_outer * sa, z_base)))
        t_in.append(bm.verts.new((r_inner * ca, r_inner * sa, z_base + height)))
        t_out.append(bm.verts.new((r_outer * ca, r_outer * sa, z_base + height)))
        
    for i in range(segments):
        inext = (i + 1) % segments
        bm.faces.new((b_in[i], t_in[i], t_in[inext], b_in[inext]))
        bm.faces.new((b_out[i], b_out[inext], t_out[inext], t_out[i]))
        bm.faces.new((t_in[i], t_out[i], t_out[inext], t_in[inext]))
        
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(mats['Citadel_Stone'])
    return obj

def build_citadel(mats):
    print("Building Concentric Citadel...")
    citadel = bpy.data.objects.new("Concentric_Citadel_Root", None)
    citadel.location = (0.0, 0.0, 0.0)
    bpy.context.collection.objects.link(citadel)
    
    wall_out = build_hollow_curtain_wall("Citadel_Outer_Curtain_Wall", 68.0, 14.0, 3.5, 72, mats, z_base=16.0)
    wall_out.parent = citadel
    bpy.context.collection.objects.link(wall_out)
    
    wall_in = build_hollow_curtain_wall("Citadel_Inner_Curtain_Wall", 42.0, 18.0, 3.5, 48, mats, z_base=16.0)
    wall_in.parent = citadel
    bpy.context.collection.objects.link(wall_in)
    
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 32.0))
    keep = bpy.context.active_object
    keep.name = "Citadel_Palace_GrandKeep"
    keep.scale = (32.0, 26.0, 32.0)
    keep.parent = citadel
    keep.data.materials.append(mats['Citadel_Stone'])
    
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=7.0, radius2=0.2, depth=42.0, location=(0, 0, 68.0))
    spire = bpy.context.active_object
    spire.name = "Citadel_Gothic_Master_Spire"
    spire.parent = citadel
    spire.data.materials.append(mats['Citadel_Roof'])
    
    turrets = [(12, 10), (-12, 10), (12, -10), (-12, -10)]
    for idx, (tx, ty) in enumerate(turrets):
        bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=3.8, depth=24.0, location=(tx, ty, 58.0))
        t = bpy.context.active_object
        t.name = f"Citadel_Turret_Spire_{idx+1}"
        t.parent = citadel
        t.data.materials.append(mats['Citadel_Roof'])
        
    angles = [90.0, 150.0, 210.0, 270.0, 330.0, 30.0]
    for idx, ang_d in enumerate(angles):
        rad = math.radians(ang_d)
        ca, sa = math.cos(rad), math.sin(rad)
        mid_r = 87.0
        b_len = 44.0
        
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(mid_r * ca, mid_r * sa, 15.2))
        viaduct = bpy.context.active_object
        viaduct.name = f"Citadel_Viaduct_Bridge_0{idx+1}"
        viaduct.scale = (b_len, 8.0, 3.8)
        viaduct.rotation_euler = (0, 0, rad)
        viaduct.parent = citadel
        viaduct.data.materials.append(mats['Citadel_Stone'])
        
        for pr in [76.0, 88.0, 100.0]:
            bpy.ops.mesh.primitive_cylinder_add(radius=2.6, depth=11.0, location=(pr * ca, pr * sa, 9.5))
            p = bpy.context.active_object
            p.name = f"Viaduct_Pillar_0{idx+1}_{int(pr)}"
            p.parent = citadel
            p.data.materials.append(mats['Citadel_Stone'])
            
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(68.0 * ca, 68.0 * sa, 23.5))
        gate = bpy.context.active_object
        gate.name = f"Citadel_Gatehouse_0{idx+1}"
        gate.scale = (8.5, 13.0, 17.0)
        gate.rotation_euler = (0, 0, rad)
        gate.parent = citadel
        gate.data.materials.append(mats['Citadel_Stone'])
        
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(68.0 * ca, 68.0 * sa, 32.5))
        gbat = bpy.context.active_object
        gbat.name = f"Gatehouse_Roof_{idx+1}"
        gbat.scale = (9.2, 13.6, 1.2)
        gbat.rotation_euler = (0, 0, rad)
        gbat.parent = citadel
        gbat.data.materials.append(mats['Citadel_Roof'])
        
    for i in range(16):
        hang = (2.0 * math.pi * i) / 16
        hr = 30.0
        hx, hy = hr * math.cos(hang), hr * math.sin(hang)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, hy, 20.5))
        h = bpy.context.active_object
        h.name = f"Citadel_Inner_Estate_{i+1}"
        h.scale = (5.5, 7.5, 9.0)
        h.rotation_euler = (0, 0, hang + math.pi/2)
        h.parent = citadel
        h.data.materials.append(mats['Citadel_Stone'])
        
    for i in range(36):
        hang = (2.0 * math.pi * i) / 36
        hdeg = (math.degrees(hang) + 360.0) % 360.0
        if any(abs(hdeg - a) < 16.0 or abs(hdeg - a - 360) < 16.0 for a in angles):
            continue
        hr = 55.0
        hx, hy = hr * math.cos(hang), hr * math.sin(hang)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, hy, 20.0))
        h = bpy.context.active_object
        h.name = f"Citadel_House_{i+1}"
        h.scale = (6.0, 8.5, 8.0)
        h.rotation_euler = (0, 0, hang + math.pi/2)
        h.parent = citadel
        h.data.materials.append(mats['Wood_Timber'])
        
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=5.2, depth=3.5, location=(hx, hy, 25.5))
        rf = bpy.context.active_object
        rf.name = f"House_Roof_{i+1}"
        rf.rotation_euler = (0, 0, hang + math.pi/4)
        rf.parent = citadel
        rf.data.materials.append(mats['Citadel_Roof'])
        
    return citadel

def build_dockcity(mats):
    print("Building Seaward Dockcity Harbor Foundation & Offshore Bastion...")
    root = bpy.data.objects.new("Seaward_Dockcity_Root", None)
    root.location = (0.0, 0.0, 0.0)
    bpy.context.collection.objects.link(root)
    
    # Offshore Island Harbor Bastion Tower (Strategic coastal landmark)
    bpy.ops.mesh.primitive_cylinder_add(radius=8.5, depth=28.0, location=(370.0, -180.0, 21.0))
    bt = bpy.context.active_object
    bt.name = "Island_Harbor_Bastion_Tower"
    bt.parent = root
    bt.data.materials.append(mats['Citadel_Stone'])
    
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=9.2, depth=8.0, location=(370.0, -180.0, 38.0))
    btr = bpy.context.active_object
    btr.name = "Bastion_Roof"
    btr.parent = root
    btr.data.materials.append(mats['Citadel_Roof'])
    
    return root

def build_encounters(mats):
    print("Building Combat Encounters & Beacons...")
    root = bpy.data.objects.new("Combat_Encounters_Root", None)
    root.location = (0.0, 0.0, 0.0)
    bpy.context.collection.objects.link(root)
    
    boss_loc = (-260.0, 270.0, 78.0)
    boss = bpy.data.objects.new("BOSS_ARENA_FLOOR_BOSS", None)
    boss.location = boss_loc
    boss.parent = root
    bpy.context.collection.objects.link(boss)
    
    bpy.ops.mesh.primitive_cylinder_add(radius=40.0, depth=3.5, location=boss_loc)
    dais = bpy.context.active_object
    dais.name = "Boss_Arena_Obsidian_Dais"
    dais.parent = boss
    dais.data.materials.append(mats['Volcanic_Obsidian'])
    
    bpy.ops.mesh.primitive_cylinder_add(radius=9.0, depth=120.0, location=(boss_loc[0], boss_loc[1], boss_loc[2] + 60.0))
    bcol = bpy.context.active_object
    bcol.name = "Boss_Energy_Singularity_Pillar"
    bcol.parent = boss
    bcol.data.materials.append(mats['Boss_Purple'])
    
    miniboss_spawns = [
        ("MINIBOSS_ARENA_01_NW_Foothills", (-160.0, 160.0, 22.0)),
        ("MINIBOSS_ARENA_02_NE_Forest_A", (160.0, 220.0, 18.0)),
        ("MINIBOSS_ARENA_03_NE_Forest_B", (250.0, 140.0, 16.0)),
        ("MINIBOSS_ARENA_04_NE_Forest_C", (200.0, 260.0, 19.0)),
        ("MINIBOSS_ARENA_05_NE_Forest_D", (120.0, 180.0, 15.0)),
        ("MINIBOSS_ARENA_06_South_Citadel_Ruin", (140.0, -120.0, 14.0)),
    ]
    for mb_name, loc in miniboss_spawns:
        mb = bpy.data.objects.new(mb_name, None)
        mb.location = loc
        mb.parent = root
        bpy.context.collection.objects.link(mb)
        
        bpy.ops.mesh.primitive_cylinder_add(radius=16.0, depth=1.8, location=loc)
        plat = bpy.context.active_object
        plat.name = f"{mb_name}_Stone_Platform"
        plat.parent = mb
        plat.data.materials.append(mats['Citadel_Stone'])
        
        bpy.ops.mesh.primitive_cylinder_add(radius=4.5, depth=45.0, location=(loc[0], loc[1], loc[2] + 22.5))
        mcol = bpy.context.active_object
        mcol.name = f"{mb_name}_Beacon_Pillar"
        mcol.parent = mb
        mcol.data.materials.append(mats['MiniBoss_Red'])
        
    return root

def build_vegetation_canopy(mats):
    print("Building Procedural Foliage Canopy...")
    veg_root = bpy.data.objects.new("Foliage_Canopy_Root", None)
    veg_root.location = (0.0, 0.0, 0.0)
    bpy.context.collection.objects.link(veg_root)
    
    random.seed(42)
    # Temperate Oaks
    for idx in range(120):
        ox = random.uniform(105.0, 360.0)
        oy = random.uniform(60.0, 300.0)
        d_moat = math.hypot(ox, oy)
        if d_moat < 118.0:
            continue
        d_lake = math.hypot(ox - 320.0, oy - 260.0)
        if d_lake < 52.0:
            continue
        oz = evaluate_continent_height(ox, oy)
        if oz < 2.0 or oz > 50.0:
            continue
            
        bpy.ops.mesh.primitive_cylinder_add(radius=0.7, depth=5.5, location=(ox, oy, oz + 2.75))
        trunk = bpy.context.active_object
        trunk.name = f"Oak_Trunk_{idx}"
        trunk.parent = veg_root
        trunk.data.materials.append(mats['Wood_Timber'])
        
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=4.2, location=(ox, oy, oz + 6.8))
        fol = bpy.context.active_object
        fol.name = f"Oak_Canopy_{idx}"
        fol.parent = veg_root
        fol.data.materials.append(mats['Foliage_Oak'])
        
    # Northern Pines
    for idx in range(80):
        px = random.uniform(-130.0, 130.0)
        py = random.uniform(190.0, 360.0)
        if abs(px) < 32.0 and py > 104.0:
            continue
        pz = evaluate_continent_height(px, py)
        if pz < 10.0 or pz > 70.0:
            continue
            
        bpy.ops.mesh.primitive_cylinder_add(radius=0.55, depth=6.5, location=(px, py, pz + 3.25))
        trunk = bpy.context.active_object
        trunk.name = f"Pine_Trunk_{idx}"
        trunk.parent = veg_root
        trunk.data.materials.append(mats['Wood_Timber'])
        
        for l in range(3):
            lz = pz + 5.0 + l * 2.4
            rad = 4.0 - l * 0.9
            bpy.ops.mesh.primitive_cone_add(vertices=7, radius1=rad, depth=3.6, location=(px, py, lz))
            tier = bpy.context.active_object
            tier.name = f"Pine_Tier_{idx}_{l}"
            tier.parent = veg_root
            tier.data.materials.append(mats['Foliage_Pine'])
            
    # Desert Palms around Oases
    palm_idx = 0
    oases_locs = [(-360.0, 30.0, 5.5), (-300.0, -70.0, 5.0), (-380.0, -160.0, 4.2)]
    for ocx, ocy, ocz in oases_locs:
        for _ in range(14):
            palm_idx += 1
            ang = random.uniform(0, 2.0 * math.pi)
            dist = random.uniform(26.0, 38.0)
            px = ocx + dist * math.cos(ang)
            py = ocy + dist * math.sin(ang)
            pz = evaluate_continent_height(px, py)
            
            bpy.ops.mesh.primitive_cylinder_add(radius=0.42, depth=7.0, location=(px, py, pz + 3.5))
            trunk = bpy.context.active_object
            trunk.name = f"Palm_Trunk_{palm_idx}"
            trunk.rotation_euler = (random.uniform(-0.15, 0.15), random.uniform(-0.15, 0.15), 0)
            trunk.parent = veg_root
            trunk.data.materials.append(mats['Wood_Timber'])
            
            bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=3.8, depth=1.6, location=(px, py, pz + 7.2))
            fronds = bpy.context.active_object
            fronds.name = f"Palm_Fronds_{palm_idx}"
            fronds.rotation_euler = (math.pi, 0, 0)
            fronds.parent = veg_root
            fronds.data.materials.append(mats['Foliage_Palm'])
            
    return veg_root

def build_civil_infrastructure(mats):
    print("Building Roads, Farmland, and Architectural Bridges...")
    root = bpy.data.objects.new("Civil_Infrastructure_Root", None)
    root.location = (0.0, 0.0, 0.0)
    bpy.context.collection.objects.link(root)
    
    # 1. Perimeter Ring Highway
    bm = bmesh.new()
    segs = 96
    rmesh = bpy.data.meshes.new("Mesh_Road_Ring_Highway")
    robj = bpy.data.objects.new("Road_Perimeter_Highway", rmesh)
    robj.parent = root
    bpy.context.collection.objects.link(robj)
    
    rin, rout = 108.0, 116.0
    iv, ov = [], []
    for i in range(segs):
        ang = (2.0 * math.pi * i) / segs
        ca, sa = math.cos(ang), math.sin(ang)
        iv.append(bm.verts.new((rin * ca, rin * sa, 15.6)))
        ov.append(bm.verts.new((rout * ca, rout * sa, 15.6)))
    for i in range(segs):
        inext = (i + 1) % segs
        bm.faces.new((iv[i], ov[i], ov[inext], iv[inext]))
    bm.to_mesh(rmesh)
    bm.free()
    rmesh.materials.append(mats['Road_Cobble'])
    
    # 2. Farmland Gridded Plots (3x3 plots)
    for gx in range(3):
        for gy in range(3):
            fx = 175.0 + gx * 24.0
            fy = 80.0 + gy * 22.0
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(fx, fy, 15.2))
            plot = bpy.context.active_object
            plot.name = f"Farmland_Plot_{gx}_{gy}"
            plot.scale = (20.0, 18.0, 0.4)
            plot.parent = root
            plot.data.materials.append(mats['Farmland_Soil'])
            
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(fx, fy, 15.5))
            bdr = bpy.context.active_object
            bdr.name = f"Farmland_Border_{gx}_{gy}"
            bdr.scale = (21.0, 19.0, 0.3)
            bdr.parent = root
            bdr.data.materials.append(mats['Citadel_Stone'])
            
    return root

def setup_lighting_and_camera():
    bpy.ops.object.light_add(type='SUN', location=(300.0, -400.0, 500.0))
    sun = bpy.context.active_object
    sun.name = "Sun_GoldenHour"
    sun.data.energy = 4.8
    sun.data.color = (1.0, 0.94, 0.82)
    sun.rotation_euler = (math.radians(50.0), math.radians(16.0), math.radians(-35.0))
    
    bpy.ops.object.camera_add(location=(0.0, -820.0, 680.0))
    cam = bpy.context.active_object
    cam.name = "Camera_Macro_Continent_Master"
    cam.rotation_euler = (math.radians(50.0), 0.0, 0.0)
    cam.data.lens = 38.0
    cam.data.clip_end = 6000.0
    bpy.context.scene.camera = cam

def export_all():
    project_root = os.getcwd()
    out_blend = os.path.join(project_root, "Assets", "Models", "Environments", "CircularContinent_MacroLevel1.blend")
    out_fbx = os.path.join(project_root, "Assets", "Models", "Environments", "CircularContinent_MacroLevel1.fbx")
    
    bpy.ops.wm.save_as_mainfile(filepath=out_blend)
    print(f"✅ Saved Master Blend File to: {out_blend}")
    
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(
        filepath=out_fbx,
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
    print(f"🎉 Exported Full Level 1 Macro-Environment FBX to: {out_fbx}")

def main():
    print("=================================================================")
    print("STARTING FULL LEVEL 1 MACRO-ENVIRONMENT FABRICATION")
    print("=================================================================")
    clear_scene()
    mats = create_all_materials()
    build_smooth_terrain(mats)
    build_hydrology(mats)
    build_citadel(mats)
    build_dockcity(mats)
    build_encounters(mats)
    build_vegetation_canopy(mats)
    build_civil_infrastructure(mats)
    setup_lighting_and_camera()
    export_all()
    print("=================================================================")
    print("LEVEL 1 MACRO-ENVIRONMENT GENERATION SUCCESSFUL")
    print("=================================================================")

if __name__ == "__main__":
    main()

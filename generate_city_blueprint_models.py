"""
Full Procedural Generation of Aethelburg Metropolis Blueprint Models
Using Blender 5.1 Python API.
Produces 14 high-quality FBX exterior models for:
- Houses (Residential townhouse, Low-district cottage)
- Blacksmith with open forge patio, chimney & anvil
- Bakery with open shopfront, bread counter & baskets
- Guildhall with grand columns, heraldic banners & open great hall
- Bazaar stands with striped awnings, crates & barrels
- Magic item store with mystical crests & crystal pedestals
- Enchanter workshop with runic stone walls & enchanting altar
- Tavern with 2 stories, signpost, open taproom & bar
- Inn with covered porch & open entrance
- Arcane Spire for Mages' Quarter
- Hover Pads with floating rune rings
- Mana-Shield Generator Towers
- Bastion Gates with open arch & glowing mana portal curtain

ALL BUILDINGS FEATURE OPEN DOORWAYS (NO DOORS) - ZERO OBSTRUCTION FOR WALK-IN!
"""

import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix, Euler

OUTPUT_DIR = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Metropolis"
os.makedirs(OUTPUT_DIR, exist_ok=True)

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def get_or_create_material(name, diffuse_color=(0.8, 0.8, 0.8, 1.0), roughness=0.5, metallic=0.0, emission_color=(0,0,0,1), emission_strength=0.0):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        bsdf = nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = diffuse_color
            bsdf.inputs["Roughness"].default_value = roughness
            bsdf.inputs["Metallic"].default_value = metallic
            if emission_strength > 0:
                bsdf.inputs["Emission Color"].default_value = emission_color
                bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat

def add_box_to_bmesh(bm, min_pt, max_pt, mat_idx):
    verts = [
        bm.verts.new((min_pt[0], min_pt[1], min_pt[2])),
        bm.verts.new((max_pt[0], min_pt[1], min_pt[2])),
        bm.verts.new((max_pt[0], max_pt[1], min_pt[2])),
        bm.verts.new((min_pt[0], max_pt[1], min_pt[2])),
        bm.verts.new((min_pt[0], min_pt[1], max_pt[2])),
        bm.verts.new((max_pt[0], min_pt[1], max_pt[2])),
        bm.verts.new((max_pt[0], max_pt[1], max_pt[2])),
        bm.verts.new((min_pt[0], max_pt[1], max_pt[2])),
    ]
    faces = [
        bm.faces.new((verts[0], verts[3], verts[2], verts[1])), # bottom (-Z)
        bm.faces.new((verts[4], verts[5], verts[6], verts[7])), # top (+Z)
        bm.faces.new((verts[0], verts[1], verts[5], verts[4])), # front (-Y)
        bm.faces.new((verts[2], verts[3], verts[7], verts[6])), # back (+Y)
        bm.faces.new((verts[0], verts[4], verts[7], verts[3])), # left (-X)
        bm.faces.new((verts[1], verts[2], verts[6], verts[5])), # right (+X)
    ]
    for f in faces:
        f.material_index = mat_idx
    return verts, faces

def add_cylinder_to_bmesh(bm, center, radius, height, segments=12, mat_idx=0):
    half_h = height * 0.5
    top_verts = []
    bot_verts = []
    angle_step = (math.pi * 2.0) / segments
    for i in range(segments):
        ang = i * angle_step
        x = center[0] + math.sin(ang) * radius
        y = center[1] + math.cos(ang) * radius
        bot_verts.append(bm.verts.new((x, y, center[2] - half_h)))
        top_verts.append(bm.verts.new((x, y, center[2] + half_h)))
    
    # Cap faces
    bm.faces.new(reversed(bot_verts)).material_index = mat_idx
    bm.faces.new(top_verts).material_index = mat_idx
    
    # Side faces
    for i in range(segments):
        next_i = (i + 1) % segments
        f = bm.faces.new((bot_verts[i], bot_verts[next_i], top_verts[next_i], top_verts[i]))
        f.material_index = mat_idx

def add_gable_roof(bm, width, depth, base_z, peak_z, overhang=0.4, mat_roof=3, mat_gable=2):
    hw = width * 0.5 + overhang
    hd = depth * 0.5 + overhang
    
    # Slopes along Y (pitch peaks at X=0)
    v1 = bm.verts.new((-hw, -hd, base_z))
    v2 = bm.verts.new((0.0, -hd, peak_z))
    v3 = bm.verts.new((0.0,  hd, peak_z))
    v4 = bm.verts.new((-hw,  hd, base_z))
    f1 = bm.faces.new((v1, v2, v3, v4))
    f1.material_index = mat_roof
    
    v5 = bm.verts.new((0.0, -hd, peak_z))
    v6 = bm.verts.new(( hw, -hd, base_z))
    v7 = bm.verts.new(( hw,  hd, base_z))
    v8 = bm.verts.new((0.0,  hd, peak_z))
    f2 = bm.faces.new((v5, v6, v7, v8))
    f2.material_index = mat_roof
    
    # Gable ends
    g1 = bm.faces.new((v1, v6, v2))
    g1.material_index = mat_gable
    g2 = bm.faces.new((v7, v4, v3))
    g2.material_index = mat_gable

def add_open_hollow_shell(bm, width, depth, wall_h, door_w, door_h, wall_t=0.35, mat_floor=0, mat_wall=1):
    hw = width * 0.5
    hd = depth * 0.5
    hdw = door_w * 0.5
    
    # Floor slab
    fv = [
        bm.verts.new((-hw + wall_t, -hd + wall_t, 0.02)),
        bm.verts.new(( hw - wall_t, -hd + wall_t, 0.02)),
        bm.verts.new(( hw - wall_t,  hd - wall_t, 0.02)),
        bm.verts.new((-hw + wall_t,  hd - wall_t, 0.02))
    ]
    bm.faces.new(fv).material_index = mat_floor
    
    # Back wall (+Y)
    add_box_to_bmesh(bm, (-hw, hd - wall_t, 0), (hw, hd, wall_h), mat_wall)
    # Left wall (-X)
    add_box_to_bmesh(bm, (-hw, -hd, 0), (-hw + wall_t, hd - wall_t, wall_h), mat_wall)
    # Right wall (+X)
    add_box_to_bmesh(bm, (hw - wall_t, -hd, 0), (hw, hd - wall_t, wall_h), mat_wall)
    # Front wall (-Y): left side of door, right side of door, lintel above door
    add_box_to_bmesh(bm, (-hw, -hd, 0), (-hdw, -hd + wall_t, wall_h), mat_wall)
    add_box_to_bmesh(bm, (hdw, -hd, 0), (hw, -hd + wall_t, wall_h), mat_wall)
    add_box_to_bmesh(bm, (-hdw, -hd, door_h), (hdw, -hd + wall_t, wall_h), mat_wall)
    # Note: Door opening between -hdw and hdw from Z=0 to door_h is 100% EMPTY (NO DOOR).

def export_current_mesh(mesh_name, materials, filename):
    obj = bpy.context.active_object
    obj.name = mesh_name
    for m in materials:
        obj.data.materials.append(m)
    
    # Recalculate normals to outside & UV Unwrap
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    
    out_path = os.path.join(OUTPUT_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=out_path,
        use_selection=True,
        bake_space_transform=True,
        apply_scale_options='FBX_SCALE_ALL'
    )
    print(f"Exported: {filename}")

# =========================================================================
# 1. BUILDING: RESIDENTIAL HOUSE (2-Story Half-Timber, Open Doorway)
# =========================================================================
def generate_house_residential():
    reset_scene()
    m_floor = get_or_create_material("M_Metro_WoodFloor", (0.35, 0.22, 0.12, 1), 0.6)
    m_stone = get_or_create_material("M_Metro_StoneBase", (0.55, 0.53, 0.50, 1), 0.7)
    m_timber = get_or_create_material("M_Metro_PlasterTimber", (0.82, 0.78, 0.70, 1), 0.65)
    m_slate = get_or_create_material("M_Metro_RoofSlate", (0.22, 0.25, 0.28, 1), 0.4)
    m_trim = get_or_create_material("M_Metro_TimberWood", (0.28, 0.17, 0.08, 1), 0.5)

    bm = bmesh.new()
    w, d, g_h, tot_h = 8.0, 10.0, 3.8, 7.2
    door_w, door_h = 2.0, 2.8
    add_open_hollow_shell(bm, w, d, g_h, door_w, door_h, 0.35, 0, 1)
    
    # Ground floor door frame trim (NO door mesh!)
    hw, hd, hdw = w*0.5, d*0.5, door_w*0.5
    add_box_to_bmesh(bm, (-hdw - 0.15, -hd - 0.08, 0), (-hdw, -hd + 0.05, door_h + 0.15), 4)
    add_box_to_bmesh(bm, (hdw, -hd - 0.08, 0), (hdw + 0.15, -hd + 0.05, door_h + 0.15), 4)
    add_box_to_bmesh(bm, (-hdw - 0.15, -hd - 0.08, door_h), (hdw + 0.15, -hd + 0.05, door_h + 0.15), 4)
    
    # Upper floor with overhang
    ow, od = w + 0.5, d + 0.5
    add_box_to_bmesh(bm, (-ow*0.5, -od*0.5, g_h), (ow*0.5, od*0.5, tot_h), 2)
    
    # Corner timber beams
    tb = 0.25
    add_box_to_bmesh(bm, (-ow*0.5, -od*0.5, g_h), (-ow*0.5 + tb, -od*0.5 + tb, tot_h), 4)
    add_box_to_bmesh(bm, ( ow*0.5 - tb, -od*0.5, g_h), ( ow*0.5, -od*0.5 + tb, tot_h), 4)
    add_box_to_bmesh(bm, (-ow*0.5,  od*0.5 - tb, g_h), (-ow*0.5 + tb,  od*0.5, tot_h), 4)
    add_box_to_bmesh(bm, ( ow*0.5 - tb,  od*0.5 - tb, g_h), ( ow*0.5,  od*0.5, tot_h), 4)

    # Roof & Chimney
    add_gable_roof(bm, ow, od, tot_h, tot_h + 3.2, 0.5, 3, 2)
    add_box_to_bmesh(bm, (ow*0.25, od*0.1, tot_h + 0.5), (ow*0.25 + 0.9, od*0.1 + 0.9, tot_h + 4.2), 1)

    mesh = bpy.data.meshes.new("Building_House_Residential")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_House_Residential", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_House_Residential", [m_floor, m_stone, m_timber, m_slate, m_trim], "Building_House_Residential.fbx")

# =========================================================================
# 2. BUILDING: LOW-DISTRICT COTTAGE (Open Doorway, Rustic Thatched/Wood)
# =========================================================================
def generate_house_low_district():
    reset_scene()
    m_floor = get_or_create_material("M_Metro_WoodFloor", (0.35, 0.22, 0.12, 1), 0.6)
    m_stone = get_or_create_material("M_Metro_StoneBase", (0.48, 0.45, 0.42, 1), 0.75)
    m_timber = get_or_create_material("M_Metro_TimberWood", (0.32, 0.20, 0.10, 1), 0.6)
    m_thatch = get_or_create_material("M_Metro_RoofSlate", (0.38, 0.32, 0.22, 1), 0.8)

    bm = bmesh.new()
    w, d, g_h = 6.8, 8.2, 3.4
    door_w, door_h = 1.8, 2.5
    add_open_hollow_shell(bm, w, d, g_h, door_w, door_h, 0.35, 0, 1)

    # Hearth inside back wall
    add_box_to_bmesh(bm, (-1.2, d*0.5 - 1.2, 0), (1.2, d*0.5 - 0.35, 2.2), 1)
    
    # Roof
    add_gable_roof(bm, w, d, g_h, g_h + 2.4, 0.4, 3, 2)
    # Chimney
    add_box_to_bmesh(bm, (-0.6, d*0.5 - 1.0, g_h + 1.0), (0.6, d*0.5 - 0.2, g_h + 3.4), 1)

    mesh = bpy.data.meshes.new("Building_House_LowDistrict")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_House_LowDistrict", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_House_LowDistrict", [m_floor, m_stone, m_timber, m_thatch], "Building_House_LowDistrict.fbx")

# =========================================================================
# 3. BUILDING: BLACKSMITH WORKSHOP (Open Forge Patio, Chimney, Anvil)
# =========================================================================
def generate_blacksmith():
    reset_scene()
    m_stone = get_or_create_material("M_Metro_StoneBase", (0.42, 0.40, 0.38, 1), 0.8)
    m_timber = get_or_create_material("M_Metro_TimberWood", (0.28, 0.17, 0.08, 1), 0.6)
    m_iron = get_or_create_material("M_Metro_RustedIron", (0.18, 0.18, 0.20, 1), 0.35, 0.8)
    m_fire = get_or_create_material("M_Metro_ForgeFire", (1.0, 0.4, 0.05, 1), 0.2, 0.0, (1.0, 0.4, 0.05, 1), 4.0)
    m_slate = get_or_create_material("M_Metro_RoofSlate", (0.24, 0.26, 0.28, 1), 0.5)

    bm = bmesh.new()
    w, d, g_h = 11.5, 10.5, 4.2
    # Wide open front workshop opening (NO doors, 4.2m wide x 3.4m high)
    door_w, door_h = 4.2, 3.4
    add_open_hollow_shell(bm, w, d, g_h, door_w, door_h, 0.4, 0, 0)

    # Massive Forge Chimney on East exterior wall
    add_box_to_bmesh(bm, (w*0.5 - 0.4, -1.5, 0), (w*0.5 + 2.2, 1.5, 6.8), 0)
    # Glowing Fire Hearth inside forge
    add_box_to_bmesh(bm, (w*0.5 - 0.3, -1.0, 0.8), (w*0.5 + 1.8, 1.0, 1.2), 3)

    # Anvil in front of open archway (Z=0.0 to 1.1)
    add_box_to_bmesh(bm, (0.0, -d*0.5 + 2.0, 0), (0.8, -d*0.5 + 2.8, 0.7), 1) # wood stump
    add_box_to_bmesh(bm, (0.1, -d*0.5 + 2.1, 0.7), (0.7, -d*0.5 + 2.7, 1.1), 2) # iron anvil

    # Water cooling trough
    add_box_to_bmesh(bm, (w*0.5 - 2.8, -d*0.5 + 1.2, 0), (w*0.5 - 1.2, -d*0.5 + 2.2, 0.8), 1)

    # Roof
    add_gable_roof(bm, w, d, g_h, g_h + 3.0, 0.6, 4, 1)

    mesh = bpy.data.meshes.new("Building_Blacksmith_OpenForge")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_Blacksmith_OpenForge", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_Blacksmith_OpenForge", [m_stone, m_timber, m_iron, m_fire, m_slate], "Building_Blacksmith_OpenForge.fbx")

# =========================================================================
# 4. BUILDING: ARTISAN BAKERY (Open Shopfront, Display Counter, Bread Ovens)
# =========================================================================
def generate_bakery():
    reset_scene()
    m_floor = get_or_create_material("M_Metro_WoodFloor", (0.42, 0.28, 0.16, 1), 0.5)
    m_brick = get_or_create_material("M_Metro_StoneBase", (0.62, 0.45, 0.38, 1), 0.7)
    m_timber = get_or_create_material("M_Metro_TimberWood", (0.34, 0.21, 0.11, 1), 0.55)
    m_awning = get_or_create_material("M_Metro_CanopyGold", (0.88, 0.65, 0.18, 1), 0.6)
    m_bread = get_or_create_material("M_Metro_BreadGold", (0.78, 0.52, 0.22, 1), 0.7)
    m_slate = get_or_create_material("M_Metro_RoofSlate", (0.28, 0.30, 0.32, 1), 0.45)

    bm = bmesh.new()
    w, d, g_h = 8.5, 9.5, 3.8
    door_w, door_h = 2.4, 2.8
    add_open_hollow_shell(bm, w, d, g_h, door_w, door_h, 0.35, 0, 1)

    hw, hd = w*0.5, d*0.5
    # Outdoor display counter on right side of open door
    add_box_to_bmesh(bm, (door_w*0.5 + 0.2, -hd - 0.9, 0), (hw - 0.2, -hd, 1.0), 2)
    # Bread baskets & loaves on counter
    add_box_to_bmesh(bm, (door_w*0.5 + 0.4, -hd - 0.8, 1.0), (hw - 0.4, -hd - 0.1, 1.3), 4)

    # Awning over display counter
    add_box_to_bmesh(bm, (door_w*0.5, -hd - 1.2, 2.5), (hw, -hd, 2.7), 3)

    # Brick Oven in back wall interior
    add_box_to_bmesh(bm, (-hw + 0.4, hd - 2.0, 0), (-hw + 2.8, hd - 0.4, 2.6), 1)

    # Roof
    add_gable_roof(bm, w, d, g_h, g_h + 2.8, 0.5, 5, 2)
    # Chimney from oven
    add_box_to_bmesh(bm, (-hw + 0.6, hd - 1.8, g_h), (-hw + 1.6, hd - 0.8, g_h + 3.5), 1)

    mesh = bpy.data.meshes.new("Building_Bakery_Shop")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_Bakery_Shop", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_Bakery_Shop", [m_floor, m_brick, m_timber, m_awning, m_bread, m_slate], "Building_Bakery_Shop.fbx")

# =========================================================================
# 5. BUILDING: GRAND GUILDHALL (Monumental Pillars, Open Great Hall)
# =========================================================================
def generate_guildhall():
    reset_scene()
    m_stone = get_or_create_material("M_Metro_Limestone", (0.75, 0.72, 0.68, 1), 0.6)
    m_marble = get_or_create_material("M_Metro_Marble", (0.85, 0.84, 0.82, 1), 0.3)
    m_gold = get_or_create_material("M_Metro_CanopyGold", (0.85, 0.70, 0.15, 1), 0.3, 0.7)
    m_banner = get_or_create_material("M_Metro_CanopyRed", (0.72, 0.12, 0.14, 1), 0.5)
    m_slate = get_or_create_material("M_Metro_RoofSlate", (0.20, 0.24, 0.28, 1), 0.4)

    bm = bmesh.new()
    w, d, g_h, tot_h = 16.0, 20.0, 5.5, 11.0
    door_w, door_h = 4.2, 4.8
    add_open_hollow_shell(bm, w, d, g_h, door_w, door_h, 0.5, 1, 0)

    hw, hd = w*0.5, d*0.5
    # Grand facade portico with 4 columns in front of entrance
    col_x = [-5.5, -2.5, 2.5, 5.5]
    for cx in col_x:
        add_cylinder_to_bmesh(bm, (cx, -hd - 1.5, g_h*0.5), 0.5, g_h, 12, 1)
    
    # Portico pediment above columns
    add_box_to_bmesh(bm, (-hw, -hd - 2.0, g_h), (hw, -hd, g_h + 1.2), 0)

    # Heraldic Shield Crests on front
    add_box_to_bmesh(bm, (-4.5, -hd - 0.1, g_h + 1.5), (-2.5, -hd + 0.05, g_h + 3.8), 3)
    add_box_to_bmesh(bm, ( 2.5, -hd - 0.1, g_h + 1.5), ( 4.5, -hd + 0.05, g_h + 3.8), 3)

    # Upper floor and roof
    add_box_to_bmesh(bm, (-hw, -hd, g_h), (hw, hd, tot_h), 0)
    add_gable_roof(bm, w, d, tot_h, tot_h + 4.5, 0.8, 4, 0)

    mesh = bpy.data.meshes.new("Building_Guildhall_Grand")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_Guildhall_Grand", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_Guildhall_Grand", [m_stone, m_marble, m_gold, m_banner, m_slate], "Building_Guildhall_Grand.fbx")

# =========================================================================
# 6. PROP: BAZAAR STAND / STREET MARKET STALL (Striped Canopy, Crates, Open)
# =========================================================================
def generate_bazaar_stand():
    reset_scene()
    m_wood = get_or_create_material("M_Metro_TimberWood", (0.36, 0.22, 0.12, 1), 0.6)
    m_awning_red = get_or_create_material("M_Metro_CanopyRed", (0.75, 0.15, 0.15, 1), 0.6)
    m_awning_white = get_or_create_material("M_Metro_CanopyGold", (0.90, 0.88, 0.80, 1), 0.6)
    m_goods = get_or_create_material("M_Metro_BreadGold", (0.65, 0.45, 0.20, 1), 0.5)

    bm = bmesh.new()
    w, d, h = 4.8, 3.2, 3.2
    hw, hd = w * 0.5, d * 0.5
    
    # 4 Corner Posts
    pw = 0.15
    add_box_to_bmesh(bm, (-hw, -hd, 0), (-hw + pw, -hd + pw, 2.5), 0)
    add_box_to_bmesh(bm, ( hw - pw, -hd, 0), ( hw, -hd + pw, 2.5), 0)
    add_box_to_bmesh(bm, (-hw,  hd - pw, 0), (-hw + pw,  hd, 2.5), 0)
    add_box_to_bmesh(bm, ( hw - pw,  hd - pw, 0), ( hw,  hd, 2.5), 0)

    # Front display counter
    add_box_to_bmesh(bm, (-hw, -hd, 0), (hw, -hd + 0.9, 0.95), 0)

    # Crates of trade goods on counter
    add_box_to_bmesh(bm, (-hw + 0.2, -hd + 0.1, 0.95), (-hw + 1.4, -hd + 0.8, 1.35), 3)
    add_box_to_bmesh(bm, (-0.6, -hd + 0.1, 0.95), (0.6, -hd + 0.8, 1.4), 3)
    add_box_to_bmesh(bm, (hw - 1.4, -hd + 0.1, 0.95), (hw - 0.2, -hd + 0.8, 1.35), 3)

    # Barrels alongside
    add_cylinder_to_bmesh(bm, (-hw - 0.6, -hd + 0.6, 0.55), 0.4, 1.1, 10, 0)
    add_cylinder_to_bmesh(bm, ( hw + 0.6, -hd + 0.6, 0.55), 0.4, 1.1, 10, 0)

    # Striped Pitched Awning Canopy
    # Ridge beam at Y=0, Z=h
    # Left slope (-X)
    add_box_to_bmesh(bm, (-hw - 0.4, -hd - 0.4, 2.4), (0.0, hd + 0.4, h), 1)
    # Right slope (+X)
    add_box_to_bmesh(bm, (0.0, -hd - 0.4, 2.4), (hw + 0.4, hd + 0.4, h), 2)

    mesh = bpy.data.meshes.new("Prop_BazaarStand_Street")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Prop_BazaarStand_Street", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Prop_BazaarStand_Street", [m_wood, m_awning_red, m_awning_white, m_goods], "Prop_BazaarStand_Street.fbx")

# =========================================================================
# 7. BUILDING: MAGIC ITEM STORE (Crescent Finials, Crystal Pedestals, Open)
# =========================================================================
def generate_magic_item_store():
    reset_scene()
    m_stone = get_or_create_material("M_Metro_Marble", (0.78, 0.76, 0.82, 1), 0.4)
    m_blue_slate = get_or_create_material("M_Metro_CanopyBlue", (0.15, 0.25, 0.45, 1), 0.35)
    m_gold = get_or_create_material("M_Metro_CanopyGold", (0.88, 0.72, 0.20, 1), 0.3, 0.7)
    m_rune_glow = get_or_create_material("M_Metro_RuneGlow", (0.1, 0.8, 1.0, 1), 0.2, 0.0, (0.1, 0.8, 1.0, 1), 3.5)

    bm = bmesh.new()
    w, d, g_h, tot_h = 8.8, 10.5, 4.0, 7.8
    door_w, door_h = 2.4, 3.0
    add_open_hollow_shell(bm, w, d, g_h, door_w, door_h, 0.4, 0, 0)

    hw, hd, hdw = w*0.5, d*0.5, door_w*0.5
    # Mystical Archway Trim with Gold Rune inlays
    add_box_to_bmesh(bm, (-hdw - 0.2, -hd - 0.1, 0), (-hdw, -hd + 0.05, door_h + 0.2), 2)
    add_box_to_bmesh(bm, (hdw, -hd - 0.1, 0), (hdw + 0.2, -hd + 0.05, door_h + 0.2), 2)
    add_box_to_bmesh(bm, (-hdw - 0.2, -hd - 0.1, door_h), (hdw + 0.2, -hd + 0.05, door_h + 0.3), 3)

    # Interior: Crystal display pedestals
    add_cylinder_to_bmesh(bm, (-2.0, 0.0, 0.5), 0.45, 1.0, 10, 0)
    add_cylinder_to_bmesh(bm, (-2.0, 0.0, 1.2), 0.2, 0.4, 8, 3) # Glowing crystal
    add_cylinder_to_bmesh(bm, ( 2.0, 0.0, 0.5), 0.45, 1.0, 10, 0)
    add_cylinder_to_bmesh(bm, ( 2.0, 0.0, 1.2), 0.2, 0.4, 8, 3) # Glowing crystal

    # Upper floor
    add_box_to_bmesh(bm, (-hw, -hd, g_h), (hw, hd, tot_h), 0)
    add_gable_roof(bm, w, d, tot_h, tot_h + 3.2, 0.5, 1, 0)

    # Crescent Moon Finial on roof peak
    add_cylinder_to_bmesh(bm, (0.0, -hd - 0.2, tot_h + 3.6), 0.5, 0.15, 12, 2)

    mesh = bpy.data.meshes.new("Building_MagicItemStore")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_MagicItemStore", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_MagicItemStore", [m_stone, m_blue_slate, m_gold, m_rune_glow], "Building_MagicItemStore.fbx")

# =========================================================================
# 8. BUILDING: ENCHANTER WORKSHOP (Octagonal Runic Tower, Arcane Pedestal)
# =========================================================================
def generate_enchanter_workshop():
    reset_scene()
    m_stone = get_or_create_material("M_Metro_CathedralStone", (0.45, 0.43, 0.42, 1), 0.7)
    m_rune = get_or_create_material("M_Metro_RuneGlow", (0.2, 0.85, 1.0, 1), 0.2, 0.0, (0.2, 0.85, 1.0, 1), 4.5)
    m_slate = get_or_create_material("M_Metro_RoofSlate", (0.18, 0.22, 0.28, 1), 0.4)
    m_gold = get_or_create_material("M_Metro_CanopyGold", (0.85, 0.70, 0.18, 1), 0.35, 0.6)

    bm = bmesh.new()
    radius = 5.2
    wall_h = 5.0
    segments = 8
    door_seg = 0 # Front -Y is open
    
    # Octagonal base & open entrance
    angle_step = (math.pi * 2.0) / segments
    for i in range(segments):
        ang1 = i * angle_step
        ang2 = (i + 1) % angle_step
        x1, y1 = math.sin(ang1) * radius, -math.cos(ang1) * radius
        x2, y2 = math.sin(ang2) * radius, -math.cos(ang2) * radius
        
        # Leave front segment (i=0) OPEN as doorway!
        if i == 0:
            # Lintel above doorway at 3.2m
            add_box_to_bmesh(bm, (x1, y1, 3.2), (x2, y2 + 0.4, wall_h), 0)
        else:
            # Wall box
            add_box_to_bmesh(bm, (min(x1,x2)-0.2, min(y1,y2)-0.2, 0), (max(x1,x2)+0.2, max(y1,y2)+0.2, wall_h), 0)

    # Floor slab
    add_cylinder_to_bmesh(bm, (0, 0, 0.02), radius - 0.2, 0.04, 16, 0)

    # Central Enchanting Altar
    add_cylinder_to_bmesh(bm, (0, 0, 0.5), 1.4, 1.0, 12, 0)
    add_cylinder_to_bmesh(bm, (0, 0, 1.02), 1.2, 0.04, 12, 1) # Glowing arcane disc
    add_cylinder_to_bmesh(bm, (0, 0, 1.6), 0.35, 0.7, 8, 1) # Floating crystal

    # Conical Roof
    top_v = bm.verts.new((0, 0, wall_h + 3.8))
    rim_verts = []
    for i in range(segments):
        ang = i * angle_step
        rx, ry = math.sin(ang) * (radius + 0.4), -math.cos(ang) * (radius + 0.4)
        rim_verts.append(bm.verts.new((rx, ry, wall_h)))
    
    for i in range(segments):
        next_i = (i + 1) % segments
        f = bm.faces.new((rim_verts[i], rim_verts[next_i], top_v))
        f.material_index = 2

    # Spire Tip
    add_cylinder_to_bmesh(bm, (0, 0, wall_h + 4.5), 0.25, 1.4, 8, 3)

    mesh = bpy.data.meshes.new("Building_EnchanterWorkshop")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_EnchanterWorkshop", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_EnchanterWorkshop", [m_stone, m_rune, m_slate, m_gold], "Building_EnchanterWorkshop.fbx")

# =========================================================================
# 9. BUILDING: TAVERN (2-Story, Open Taproom, Beer Kegs, Signpost)
# =========================================================================
def generate_tavern():
    reset_scene()
    m_floor = get_or_create_material("M_Metro_WoodFloor", (0.35, 0.22, 0.12, 1), 0.6)
    m_stone = get_or_create_material("M_Metro_StoneBase", (0.50, 0.48, 0.45, 1), 0.7)
    m_timber = get_or_create_material("M_Metro_TimberWood", (0.30, 0.18, 0.08, 1), 0.6)
    m_slate = get_or_create_material("M_Metro_RoofSlate", (0.22, 0.25, 0.28, 1), 0.4)
    m_gold = get_or_create_material("M_Metro_CanopyGold", (0.85, 0.68, 0.18, 1), 0.4)

    bm = bmesh.new()
    w, d, g_h, tot_h = 12.0, 14.0, 4.2, 8.5
    door_w, door_h = 2.8, 3.2
    add_open_hollow_shell(bm, w, d, g_h, door_w, door_h, 0.4, 0, 1)

    hw, hd = w*0.5, d*0.5
    # Long Bar Counter inside
    add_box_to_bmesh(bm, (-hw + 1.2, 0.0, 0), (-hw + 2.2, hd - 1.5, 1.1), 2)
    # Beer Casks behind bar
    add_cylinder_to_bmesh(bm, (-hw + 0.8, 2.0, 0.6), 0.45, 1.2, 10, 2)
    add_cylinder_to_bmesh(bm, (-hw + 0.8, 4.0, 0.6), 0.45, 1.2, 10, 2)

    # Hanging Tavern Signpost outside front
    add_box_to_bmesh(bm, (hw - 0.4, -hd - 1.5, 3.0), (hw - 0.15, -hd, 3.2), 2)
    add_box_to_bmesh(bm, (hw - 0.35, -hd - 1.4, 2.0), (hw - 0.2, -hd - 0.6, 2.9), 4)

    # Upper timber floor with overhang
    ow, od = w + 0.6, d + 0.6
    add_box_to_bmesh(bm, (-ow*0.5, -od*0.5, g_h), (ow*0.5, od*0.5, tot_h), 2)
    add_gable_roof(bm, ow, od, tot_h, tot_h + 3.8, 0.6, 3, 2)

    # Stone Chimney
    add_box_to_bmesh(bm, (ow*0.35, od*0.2, tot_h), (ow*0.35 + 1.1, od*0.2 + 1.1, tot_h + 4.8), 1)

    mesh = bpy.data.meshes.new("Building_Tavern_Open")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_Tavern_Open", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_Tavern_Open", [m_floor, m_stone, m_timber, m_slate, m_gold], "Building_Tavern_Open.fbx")

# =========================================================================
# 10. BUILDING: TRAVELER'S INN (Timber Lodge, Covered Porch, Open Entrance)
# =========================================================================
def generate_inn_lodge():
    reset_scene()
    m_floor = get_or_create_material("M_Metro_WoodFloor", (0.38, 0.24, 0.14, 1), 0.55)
    m_stone = get_or_create_material("M_Metro_StoneBase", (0.52, 0.50, 0.48, 1), 0.7)
    m_timber = get_or_create_material("M_Metro_TimberWood", (0.32, 0.20, 0.10, 1), 0.6)
    m_slate = get_or_create_material("M_Metro_RoofSlate", (0.24, 0.26, 0.30, 1), 0.45)

    bm = bmesh.new()
    w, d, g_h, tot_h = 11.0, 13.0, 3.8, 7.8
    door_w, door_h = 2.2, 2.8
    add_open_hollow_shell(bm, w, d, g_h, door_w, door_h, 0.4, 0, 1)

    hw, hd = w*0.5, d*0.5
    # Covered front porch
    porch_d = 2.2
    add_box_to_bmesh(bm, (-hw, -hd - porch_d, 0.1), (hw, -hd, 0.3), 0) # porch deck
    # Porch posts
    add_box_to_bmesh(bm, (-hw + 0.2, -hd - porch_d + 0.2, 0.3), (-hw + 0.4, -hd - porch_d + 0.4, 3.2), 2)
    add_box_to_bmesh(bm, ( hw - 0.4, -hd - porch_d + 0.2, 0.3), ( hw - 0.2, -hd - porch_d + 0.4, 3.2), 2)
    # Porch roof
    add_box_to_bmesh(bm, (-hw - 0.2, -hd - porch_d - 0.2, 3.0), (hw + 0.2, -hd, 3.4), 3)

    # Main building upper story & roof
    add_box_to_bmesh(bm, (-hw, -hd, g_h), (hw, hd, tot_h), 2)
    add_gable_roof(bm, w, d, tot_h, tot_h + 3.4, 0.5, 3, 2)

    mesh = bpy.data.meshes.new("Building_Inn_Lodge")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Building_Inn_Lodge", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Building_Inn_Lodge", [m_floor, m_stone, m_timber, m_slate], "Building_Inn_Lodge.fbx")

# =========================================================================
# 11. PROP: ARCANE SPIRE (Mages' Quarter Landmark)
# =========================================================================
def generate_arcane_spire():
    reset_scene()
    m_stone = get_or_create_material("M_Metro_Marble", (0.80, 0.82, 0.88, 1), 0.3)
    m_rune = get_or_create_material("M_Metro_RuneGlow", (0.1, 0.85, 1.0, 1), 0.2, 0.0, (0.1, 0.85, 1.0, 1), 5.0)
    m_gold = get_or_create_material("M_Metro_CanopyGold", (0.88, 0.75, 0.20, 1), 0.3, 0.8)

    bm = bmesh.new()
    # Hexagonal tiered spire base
    add_cylinder_to_bmesh(bm, (0, 0, 1.5), 4.2, 3.0, 6, 0)
    add_cylinder_to_bmesh(bm, (0, 0, 5.0), 3.2, 4.0, 6, 0)
    add_cylinder_to_bmesh(bm, (0, 0, 12.0), 2.2, 10.0, 6, 0)
    add_cylinder_to_bmesh(bm, (0, 0, 20.0), 1.2, 8.0, 6, 0)

    # Floating Arcane Rings at Z=16, 22, 26
    for z, r in [(16.0, 3.5), (22.0, 2.8), (26.0, 2.0)]:
        add_cylinder_to_bmesh(bm, (0, 0, z), r, 0.35, 16, 1)

    # Summit Leviathan Crystal Cluster
    add_cylinder_to_bmesh(bm, (0, 0, 28.0), 1.5, 6.0, 6, 1)

    mesh = bpy.data.meshes.new("Prop_ArcaneSpire_MagesQuarter")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Prop_ArcaneSpire_MagesQuarter", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Prop_ArcaneSpire_MagesQuarter", [m_stone, m_rune, m_gold], "Prop_ArcaneSpire_MagesQuarter.fbx")

# =========================================================================
# 12. PROP: HOVER PAD (Citadel Arcane Landing Disc)
# =========================================================================
def generate_hover_pad():
    reset_scene()
    m_stone = get_or_create_material("M_Metro_Marble", (0.82, 0.82, 0.85, 1), 0.3)
    m_rune = get_or_create_material("M_Metro_RuneGlow", (0.1, 0.9, 1.0, 1), 0.2, 0.0, (0.1, 0.9, 1.0, 1), 4.5)
    m_gold = get_or_create_material("M_Metro_CanopyGold", (0.88, 0.75, 0.20, 1), 0.3, 0.7)

    bm = bmesh.new()
    # Octagonal levitation disc
    add_cylinder_to_bmesh(bm, (0, 0, 0.25), 4.5, 0.5, 8, 0)
    # Inner glowing rune mandala
    add_cylinder_to_bmesh(bm, (0, 0, 0.52), 3.6, 0.04, 16, 1)
    # 4 Outer Pylons
    for ang in [0, math.pi*0.5, math.pi, math.pi*1.5]:
        px, py = math.sin(ang)*4.2, math.cos(ang)*4.2
        add_cylinder_to_bmesh(bm, (px, py, 0.8), 0.4, 1.2, 8, 2)
        add_cylinder_to_bmesh(bm, (px, py, 1.5), 0.2, 0.3, 8, 1)

    mesh = bpy.data.meshes.new("Prop_HoverPad_Citadel")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Prop_HoverPad_Citadel", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Prop_HoverPad_Citadel", [m_stone, m_rune, m_gold], "Prop_HoverPad_Citadel.fbx")

# =========================================================================
# 13. PROP: MANA-SHIELD GENERATOR TOWER
# =========================================================================
def generate_mana_shield_tower():
    reset_scene()
    m_stone = get_or_create_material("M_Metro_TownWall", (0.50, 0.48, 0.45, 1), 0.75)
    m_brass = get_or_create_material("M_Metro_ClockworkBrass", (0.80, 0.65, 0.25, 1), 0.35, 0.8)
    m_shield = get_or_create_material("M_Metro_RuneGlow", (0.15, 0.85, 1.0, 1), 0.2, 0.0, (0.15, 0.85, 1.0, 1), 5.5)

    bm = bmesh.new()
    # Fortress Tower
    add_cylinder_to_bmesh(bm, (0, 0, 7.0), 3.2, 14.0, 12, 0)
    # Flared Battlement
    add_cylinder_to_bmesh(bm, (0, 0, 14.5), 3.8, 1.5, 12, 0)

    # Brass Gimbal Mount
    add_cylinder_to_bmesh(bm, (0, 0, 15.6), 2.2, 0.8, 12, 1)

    # Glowing Mana Orb Generator Core
    add_cylinder_to_bmesh(bm, (0, 0, 17.2), 1.6, 2.4, 12, 2)

    mesh = bpy.data.meshes.new("Prop_ManaShield_GeneratorTower")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Prop_ManaShield_GeneratorTower", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Prop_ManaShield_GeneratorTower", [m_stone, m_brass, m_shield], "Prop_ManaShield_GeneratorTower.fbx")

# =========================================================================
# 14. PROP: BASTION PORTAL GATE (Open Fortress Gate with Mana Barrier)
# =========================================================================
def generate_bastion_portal_gate():
    reset_scene()
    m_stone = get_or_create_material("M_Metro_TownWall", (0.52, 0.50, 0.46, 1), 0.7)
    m_portal = get_or_create_material("M_Metro_RuneGlow", (0.1, 0.85, 1.0, 1), 0.1, 0.0, (0.1, 0.85, 1.0, 1), 4.5)
    m_trim = get_or_create_material("M_Metro_Limestone", (0.75, 0.72, 0.68, 1), 0.6)

    bm = bmesh.new()
    gate_w, gate_d, gate_h = 22.0, 8.0, 16.0
    arch_w, arch_h = 8.0, 10.0
    hw = gate_w * 0.5
    h_arch = arch_w * 0.5

    # Left Bastion Tower
    add_box_to_bmesh(bm, (-hw, -gate_d*0.5, 0), (-h_arch, gate_d*0.5, gate_h), 0)
    # Right Bastion Tower
    add_box_to_bmesh(bm, (h_arch, -gate_d*0.5, 0), (hw, gate_d*0.5, gate_h), 0)
    # Lintel / Gatehouse Walkway above arch
    add_box_to_bmesh(bm, (-h_arch, -gate_d*0.5, arch_h), (h_arch, gate_d*0.5, gate_h), 0)

    # Arched portal trim
    add_box_to_bmesh(bm, (-h_arch - 0.3, -gate_d*0.5 - 0.1, 0), (-h_arch, gate_d*0.5 + 0.1, arch_h + 0.3), 2)
    add_box_to_bmesh(bm, (h_arch, -gate_d*0.5 - 0.1, 0), (h_arch + 0.3, gate_d*0.5 + 0.1, arch_h + 0.3), 2)
    add_box_to_bmesh(bm, (-h_arch - 0.3, -gate_d*0.5 - 0.1, arch_h), (h_arch + 0.3, gate_d*0.5 + 0.1, arch_h + 0.6), 2)

    # Glowing Mana-Shield Energy Barrier (Thin translucent energy plane across arch)
    add_box_to_bmesh(bm, (-h_arch + 0.05, -0.15, 0.05), (h_arch - 0.05, 0.15, arch_h - 0.05), 1)

    mesh = bpy.data.meshes.new("Prop_Bastion_PortalGate")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Prop_Bastion_PortalGate", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    export_current_mesh("Prop_Bastion_PortalGate", [m_stone, m_portal, m_trim], "Prop_Bastion_PortalGate.fbx")

# =========================================================================
# RUN GENERATION OF ALL 14 BLUEPRINT MODELS
# =========================================================================
print("--- Starting Generation of All 14 Metropolis Blueprint Models ---")
generate_house_residential()
generate_house_low_district()
generate_blacksmith()
generate_bakery()
generate_guildhall()
generate_bazaar_stand()
generate_magic_item_store()
generate_enchanter_workshop()
generate_tavern()
generate_inn_lodge()
generate_arcane_spire()
generate_hover_pad()
generate_mana_shield_tower()
generate_bastion_portal_gate()
print("--- ALL 14 METROPOLIS MODELS GENERATED SUCCESSFULLY ---")

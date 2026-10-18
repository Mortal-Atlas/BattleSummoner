import bpy
import bmesh
import math
import random
import os

OUT_DIR = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Metropolis"
os.makedirs(OUT_DIR, exist_ok=True)

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def get_mat(name, color, roughness=0.7, metallic=0.1, emissive=None):
    mat = bpy.data.materials.get(name)
    if not mat:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = color
        bsdf.inputs['Roughness'].default_value = roughness
        bsdf.inputs['Metallic'].default_value = metallic
        if emissive and 'Emission Color' in bsdf.inputs:
            bsdf.inputs['Emission Color'].default_value = emissive
            if 'Emission Strength' in bsdf.inputs:
                bsdf.inputs['Emission Strength'].default_value = 3.0
    return mat

def export_fbx(filename):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH':
            obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    path = os.path.join(OUT_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=False,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y'
    )
    print(f"Exported: {filename}")

# ==============================================================================
# 1. METROPOLIS GRAND STAIRS & ARCHWAY
# ==============================================================================
def build_grand_stairs():
    reset_scene()
    m_stone = get_mat("M_StairsStone", (0.55, 0.53, 0.50, 1.0), roughness=0.85)
    m_cobble = get_mat("M_StairsCobble", (0.42, 0.40, 0.38, 1.0), roughness=0.8)
    m_gold = get_mat("M_StairsGold", (0.88, 0.74, 0.25, 1.0), roughness=0.35, metallic=0.9)
    m_banner = get_mat("M_StairsBanner", (0.75, 0.15, 0.20, 1.0), roughness=0.6)

    root = bpy.data.objects.new("Metropolis_GrandStairs", None)
    bpy.context.collection.objects.link(root)

    # 3 Flights of wide stairs with 2 large landings
    flight_data = [
        {"steps": 14, "w": 8.0, "step_d": 0.5, "step_h": 0.25, "start": (0, 0, 0)},
        {"steps": 16, "w": 7.5, "step_d": 0.5, "step_h": 0.25, "start": (0, 3.5, 9.5)},
        {"steps": 18, "w": 7.0, "step_d": 0.5, "step_h": 0.25, "start": (0, 7.5, 20.0)}
    ]

    cur_h = 0.0
    cur_z = 0.0
    for f_idx, f in enumerate(flight_data):
        w = f["w"]
        # Steps
        for s in range(f["steps"]):
            sy = cur_h + s * f["step_h"]
            sz = cur_z + s * f["step_d"]
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, sy + f["step_h"]*0.5, sz + f["step_d"]*0.5))
            step = bpy.context.active_object
            step.name = f"Step_{f_idx}_{s}"
            step.scale = (w, f["step_h"], f["step_d"])
            step.parent = root
            step.data.materials.append(m_stone)

        cur_h += f["steps"] * f["step_h"]
        cur_z += f["steps"] * f["step_d"]

        # Landing Plaza
        landing_depth = 3.5
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, cur_h - 0.2, cur_z + landing_depth*0.5))
        land = bpy.context.active_object
        land.name = f"Landing_{f_idx}"
        land.scale = (w + 1.5, 0.4, landing_depth)
        land.parent = root
        land.data.materials.append(m_cobble)

        # Balustrades on sides
        for side in [-1, 1]:
            bx = side * (w * 0.5 + 0.3)
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, cur_h + 0.45, cur_z + landing_depth*0.5))
            bal = bpy.context.active_object
            bal.name = f"Balustrade_Land_{f_idx}_{side}"
            bal.scale = (0.5, 0.9, landing_depth)
            bal.parent = root
            bal.data.materials.append(m_stone)

            # Decorative urn / finial on corner
            bpy.ops.mesh.primitive_cylinder_add(radius=0.35, depth=0.6, location=(bx, cur_h + 1.1, cur_z + landing_depth*0.5))
            urn = bpy.context.active_object
            urn.parent = root
            urn.data.materials.append(m_gold)

        cur_z += landing_depth

    # Overhead Gothic Archway spanning over Flight 2
    arch_z = 14.0
    arch_y = 5.2
    for side in [-1, 1]:
        ax = side * 4.6
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(ax, arch_y + 3.0, arch_z))
        col = bpy.context.active_object
        col.name = f"StairArch_Col_{side}"
        col.scale = (1.2, 6.0, 1.4)
        col.parent = root
        col.data.materials.append(m_stone)

        # Banner hanging from column
        bpy.ops.mesh.primitive_plane_add(size=1.0, location=(ax * 0.85, arch_y + 4.0, arch_z + 0.1))
        ban = bpy.context.active_object
        ban.name = f"StairBanner_{side}"
        ban.scale = (0.9, 2.8, 1.0)
        ban.rotation_euler = (math.pi*0.5, 0, 0)
        ban.parent = root
        ban.data.materials.append(m_banner)

    # Arch span
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, arch_y + 6.2, arch_z))
    span = bpy.context.active_object
    span.name = "StairArch_Span"
    span.scale = (10.4, 0.9, 1.6)
    span.parent = root
    span.data.materials.append(m_stone)

    export_fbx("Metropolis_GrandStairs.fbx")

# ==============================================================================
# 2. TIERED RETAINING WALLS & CLIFF FOUNDATIONS
# ==============================================================================
def build_retaining_walls():
    reset_scene()
    m_stone = get_mat("M_WallStone", (0.50, 0.48, 0.45, 1.0), roughness=0.85)
    m_moss = get_mat("M_WallMoss", (0.24, 0.48, 0.18, 1.0), roughness=0.6)
    m_trim = get_mat("M_WallTrim", (0.35, 0.34, 0.32, 1.0), roughness=0.7)

    root = bpy.data.objects.new("Metropolis_RetainingWalls_Tiered", None)
    bpy.context.collection.objects.link(root)

    # 3 massive tiered retaining walls climbing from bottom to top
    tiers = [
        {"y": 3.0, "z": 0.0, "w": 40.0, "h": 6.0, "d": 4.0},
        {"y": 9.0, "z": 15.0, "w": 36.0, "h": 8.0, "d": 4.0},
        {"y": 17.0, "z": 30.0, "w": 32.0, "h": 10.0, "d": 4.0},
    ]

    for t_idx, t in enumerate(tiers):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, t["y"], t["z"]))
        wall = bpy.context.active_object
        wall.name = f"TierWall_{t_idx}"
        wall.scale = (t["w"], t["h"], t["d"])
        wall.parent = root
        wall.data.materials.append(m_stone)

        # Arched alcoves along the face
        num_alcoves = int(t["w"] / 4.5)
        for a in range(num_alcoves):
            ax = -t["w"]*0.42 + (a / float(num_alcoves - 1)) * (t["w"]*0.84)
            # Buttress pillar
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(ax, t["y"], t["z"] - t["d"]*0.55))
            but = bpy.context.active_object
            but.name = f"Buttress_{t_idx}_{a}"
            but.scale = (1.1, t["h"] * 1.05, 1.2)
            but.parent = root
            but.data.materials.append(m_trim)

        # Moss patches on base
        for m in range(8):
            mx = (random.random() - 0.5) * t["w"] * 0.9
            my = t["y"] - t["h"]*0.4 + random.random()*1.2
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(mx, my, t["z"] - t["d"]*0.52))
            moss = bpy.context.active_object
            moss.scale = (2.2, 0.8, 0.2)
            moss.parent = root
            moss.data.materials.append(m_moss)

        # Parapet walkway on top
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, t["y"] + t["h"]*0.5 + 0.45, t["z"] - t["d"]*0.4))
        parapet = bpy.context.active_object
        parapet.name = f"Parapet_{t_idx}"
        parapet.scale = (t["w"], 0.9, 0.6)
        parapet.parent = root
        parapet.data.materials.append(m_trim)

    export_fbx("Metropolis_RetainingWalls_Tiered.fbx")

# ==============================================================================
# 3. SKY BRIDGE & AQUEDUCT
# ==============================================================================
def build_sky_bridge_aqueduct():
    reset_scene()
    m_stone = get_mat("M_AqueductStone", (0.52, 0.50, 0.46, 1.0), roughness=0.85)
    m_trim = get_mat("M_AqueductTrim", (0.38, 0.36, 0.34, 1.0), roughness=0.7)
    m_water = get_mat("M_AqueductWater", (0.15, 0.55, 0.85, 1.0), roughness=0.1, metallic=0.2)

    root = bpy.data.objects.new("Metropolis_SkyBridgeAqueduct", None)
    bpy.context.collection.objects.link(root)

    span_length = 32.0
    bridge_h = 14.0
    bridge_w = 4.2

    # Upper aqueduct flume / walkway deck
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, bridge_h, 0))
    deck = bpy.context.active_object
    deck.name = "Aqueduct_Deck"
    deck.scale = (span_length, 1.2, bridge_w)
    deck.parent = root
    deck.data.materials.append(m_stone)

    # Water channel inside flume
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, bridge_h + 0.65, 0))
    water = bpy.context.active_object
    water.name = "Aqueduct_WaterChannel"
    water.scale = (span_length, 0.3, bridge_w * 0.5)
    water.parent = root
    water.data.materials.append(m_water)

    # Parapet walls
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, bridge_h + 0.8, side * (bridge_w * 0.5 - 0.25)))
        p = bpy.context.active_object
        p.scale = (span_length, 0.9, 0.4)
        p.parent = root
        p.data.materials.append(m_trim)

    # Arched Piers supporting the span
    num_piers = 4
    for i in range(num_piers):
        px = -span_length*0.38 + i * (span_length * 0.76 / (num_piers - 1))
        # Main vertical column
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, bridge_h * 0.5, 0))
        pier = bpy.context.active_object
        pier.name = f"AqueductPier_{i}"
        pier.scale = (2.2, bridge_h, bridge_w * 1.1)
        pier.parent = root
        pier.data.materials.append(m_stone)

        # Arch collar
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, bridge_h - 1.2, 0))
        collar = bpy.context.active_object
        collar.scale = (3.2, 0.8, bridge_w * 1.2)
        collar.parent = root
        collar.data.materials.append(m_trim)

    export_fbx("Metropolis_SkyBridgeAqueduct.fbx")

# ==============================================================================
# 4. CHURCH OF REVIVAL (GOTHIC CATHEDRAL CITADEL)
# ==============================================================================
def build_church_of_revival():
    reset_scene()
    m_stone = get_mat("M_CathedralStone", (0.58, 0.56, 0.54, 1.0), roughness=0.8)
    m_slate = get_mat("M_CathedralSlate", (0.22, 0.25, 0.30, 1.0), roughness=0.55)
    m_gold = get_mat("M_CathedralGold", (0.92, 0.78, 0.28, 1.0), roughness=0.3, metallic=0.95)
    m_glass = get_mat("M_CathedralRoseGlass", (0.85, 0.25, 0.45, 1.0), roughness=0.2, emissive=(0.85, 0.35, 0.65, 1.0))

    root = bpy.data.objects.new("Metropolis_ChurchOfRevival", None)
    bpy.context.collection.objects.link(root)

    # Main Nave Body
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 10.0, 0))
    nave = bpy.context.active_object
    nave.name = "Cathedral_Nave"
    nave.scale = (18.0, 20.0, 32.0)
    nave.parent = root
    nave.data.materials.append(m_stone)

    # Steep Gable Nave Roof
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=14.0, depth=12.0, location=(0, 26.0, 0))
    roof = bpy.context.active_object
    roof.name = "Cathedral_Roof"
    roof.scale = (0.75, 1.2, 1.0)
    roof.rotation_euler = (0, 0, math.pi*0.25)
    roof.parent = root
    roof.data.materials.append(m_slate)

    # Twin Front Spires (Left & Right)
    for side in [-1, 1]:
        sx = side * 8.5
        sz = -15.0
        # Tower base
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx, 16.0, sz))
        tower = bpy.context.active_object
        tower.name = f"Cathedral_Tower_{side}"
        tower.scale = (5.5, 32.0, 5.5)
        tower.parent = root
        tower.data.materials.append(m_stone)

        # High Spire Point
        bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=3.4, depth=14.0, location=(sx, 39.0, sz))
        spire = bpy.context.active_object
        spire.name = f"Cathedral_SpireTip_{side}"
        spire.parent = root
        spire.data.materials.append(m_slate)

        # Golden Cross Finial
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx, 46.5, sz))
        cross = bpy.context.active_object
        cross.scale = (0.8, 2.0, 0.3)
        cross.parent = root
        cross.data.materials.append(m_gold)

    # Grand Rose Window on Front Facade
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=3.8, depth=0.8, location=(0, 16.0, -16.2))
    rose = bpy.context.active_object
    rose.name = "Cathedral_RoseWindow"
    rose.rotation_euler = (math.pi*0.5, 0, 0)
    rose.parent = root
    rose.data.materials.append(m_glass)

    # Grand Entry Portal Arch
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 4.5, -16.4))
    portal = bpy.context.active_object
    portal.name = "Cathedral_Portal"
    portal.scale = (6.0, 9.0, 1.2)
    portal.parent = root
    portal.data.materials.append(m_stone)

    # Flying Buttresses along sides (4 on each side)
    for side in [-1, 1]:
        for b in range(4):
            bz = -8.0 + b * 6.5
            bx = side * 11.5
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, 9.0, bz))
            butt = bpy.context.active_object
            butt.name = f"FlyingButtress_{side}_{b}"
            butt.scale = (3.5, 12.0, 1.4)
            butt.parent = root
            butt.data.materials.append(m_stone)

            # Pinnacle spire on buttress
            bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=1.0, depth=4.0, location=(bx, 17.0, bz))
            pin = bpy.context.active_object
            pin.parent = root
            pin.data.materials.append(m_slate)

    export_fbx("Metropolis_ChurchOfRevival.fbx")

# ==============================================================================
# 5. ACADEMY BUILDINGS & DOMED OBSERVATORY
# ==============================================================================
def build_academy_buildings():
    reset_scene()
    m_marble = get_mat("M_AcademyMarble", (0.82, 0.80, 0.78, 1.0), roughness=0.6)
    m_dome = get_mat("M_AcademyDomePatina", (0.28, 0.62, 0.55, 1.0), roughness=0.45, metallic=0.3)
    m_gold = get_mat("M_AcademyGold", (0.90, 0.75, 0.25, 1.0), roughness=0.3, metallic=0.9)

    root = bpy.data.objects.new("Metropolis_AcademyBuildings", None)
    bpy.context.collection.objects.link(root)

    # Main Academic Hall
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 6.0, 0))
    hall = bpy.context.active_object
    hall.name = "Academy_Hall"
    hall.scale = (22.0, 12.0, 14.0)
    hall.parent = root
    hall.data.materials.append(m_marble)

    # Grand Colonnade Portico
    for c in range(6):
        cx = -8.0 + c * 3.2
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.65, depth=9.0, location=(cx, 4.5, -7.8))
        col = bpy.context.active_object
        col.name = f"AcademyCol_{c}"
        col.parent = root
        col.data.materials.append(m_marble)

    # Pediment / Frieze Triangular Gable
    bpy.ops.mesh.primitive_cone_add(vertices=3, radius1=11.5, depth=2.0, location=(0, 10.2, -7.8))
    ped = bpy.context.active_object
    ped.name = "Academy_Pediment"
    ped.rotation_euler = (0, 0, math.pi*0.5)
    ped.scale = (0.35, 1.0, 1.0)
    ped.parent = root
    ped.data.materials.append(m_marble)

    # Central Copper Observatory Dome
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=16, radius=5.5, location=(0, 15.0, 0))
    dome = bpy.context.active_object
    dome.name = "Academy_ObservatoryDome"
    dome.scale = (1.0, 0.85, 1.0)
    dome.parent = root
    dome.data.materials.append(m_dome)

    # Astrolabe finial on top of dome
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=1.2, depth=0.2, location=(0, 20.2, 0))
    ast = bpy.context.active_object
    ast.name = "Academy_AstrolabeRing"
    ast.parent = root
    ast.data.materials.append(m_gold)

    export_fbx("Metropolis_AcademyBuildings.fbx")

# ==============================================================================
# 6. TIERED TIMBER-FRAME HOUSES
# ==============================================================================
def build_timber_houses():
    reset_scene()
    m_plaster = get_mat("M_HousePlaster", (0.86, 0.84, 0.78, 1.0), roughness=0.85)
    m_wood = get_mat("M_HouseTimber", (0.28, 0.18, 0.12, 1.0), roughness=0.7)
    m_roof = get_mat("M_HouseMossySlate", (0.30, 0.35, 0.28, 1.0), roughness=0.6)

    root = bpy.data.objects.new("Metropolis_TimberHouses_Tiered", None)
    bpy.context.collection.objects.link(root)

    # Row of 3 staggered medieval houses
    house_offsets = [(-6.0, 0, 0), (0, 1.8, 1.5), (6.5, 0.8, -0.8)]

    for idx, (hx, hy, hz) in enumerate(house_offsets):
        # Ground floor (stone/plaster)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, hy + 2.0, hz))
        h1 = bpy.context.active_object
        h1.name = f"House_{idx}_Floor1"
        h1.scale = (5.2, 4.0, 6.0)
        h1.parent = root
        h1.data.materials.append(m_plaster)

        # Overhanging second floor (cantilever timber)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, hy + 5.2, hz + 0.3))
        h2 = bpy.context.active_object
        h2.name = f"House_{idx}_Floor2"
        h2.scale = (5.6, 3.2, 6.6)
        h2.parent = root
        h2.data.materials.append(m_plaster)

        # Exposed timber corner studs
        for ex in [-2.7, 2.7]:
            for ez in [-3.2, 3.2]:
                bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx + ex, hy + 5.2, hz + ez))
                stud = bpy.context.active_object
                stud.scale = (0.35, 3.2, 0.35)
                stud.parent = root
                stud.data.materials.append(m_wood)

        # Steep gable roof
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=4.4, depth=3.8, location=(hx, hy + 8.4, hz + 0.3))
        roof = bpy.context.active_object
        roof.name = f"House_{idx}_Roof"
        roof.rotation_euler = (0, 0, math.pi*0.25)
        roof.scale = (0.9, 1.1, 1.0)
        roof.parent = root
        roof.data.materials.append(m_roof)

        # Balcony with flower box
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, hy + 4.2, hz + 3.8))
        balc = bpy.context.active_object
        balc.scale = (3.2, 0.8, 1.2)
        balc.parent = root
        balc.data.materials.append(m_wood)

    export_fbx("Metropolis_TimberHouses_Tiered.fbx")

# ==============================================================================
# 7. BAKERY BUILDING WITH WOODEN BREAD SIGN
# ==============================================================================
def build_bakery_building():
    reset_scene()
    m_plaster = get_mat("M_BakeryPlaster", (0.88, 0.85, 0.76, 1.0), roughness=0.85)
    m_wood = get_mat("M_BakeryTimber", (0.24, 0.16, 0.10, 1.0), roughness=0.7)
    m_roof = get_mat("M_BakeryMossyRoof", (0.28, 0.36, 0.25, 1.0), roughness=0.6)
    m_iron = get_mat("M_BakeryWroughtIron", (0.12, 0.12, 0.14, 1.0), roughness=0.6, metallic=0.8)
    m_bread = get_mat("M_BakeryBreadGold", (0.88, 0.60, 0.22, 1.0), roughness=0.55)

    root = bpy.data.objects.new("Prop_BakeryBuilding", None)
    bpy.context.collection.objects.link(root)

    # Two-story cozy bakery building
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 3.2, 0))
    bldg = bpy.context.active_object
    bldg.name = "Bakery_Structure"
    bldg.scale = (6.4, 6.4, 7.2)
    bldg.parent = root
    bldg.data.materials.append(m_plaster)

    # Steep overhanging mossy roof
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=5.2, depth=4.6, location=(0, 8.2, 0))
    roof = bpy.context.active_object
    roof.name = "Bakery_Roof"
    roof.rotation_euler = (0, 0, math.pi*0.25)
    roof.scale = (0.9, 1.2, 1.0)
    roof.parent = root
    roof.data.materials.append(m_roof)

    # Stone Bakery Chimney puffing smoke
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(2.4, 8.5, 1.5))
    chimney = bpy.context.active_object
    chimney.scale = (1.2, 5.0, 1.2)
    chimney.parent = root
    chimney.data.materials.append(m_wood)

    # --- THE ICONIC CARVED WOODEN BREAD LOAF SIGN ---
    # Wrought iron mounting bracket extending from wall
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-3.8, 5.2, 1.8))
    bracket = bpy.context.active_object
    bracket.name = "BakerySign_Bracket"
    bracket.scale = (1.6, 0.12, 0.12)
    bracket.parent = root
    bracket.data.materials.append(m_iron)

    # Hanging Chains
    for cx in [-3.5, -4.3]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.04, depth=0.6, location=(cx, 4.85, 1.8))
        chain = bpy.context.active_object
        chain.parent = root
        chain.data.materials.append(m_iron)

    # Ornate Wooden Signboard Frame
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-3.9, 4.2, 1.8))
    frame = bpy.context.active_object
    frame.name = "BakerySign_Board"
    frame.scale = (1.8, 1.2, 0.16)
    frame.parent = root
    frame.data.materials.append(m_wood)

    # Carved 3D Loaf of Bread mounted on board!
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=12, radius=0.45, location=(-3.9, 4.2, 1.95))
    bread = bpy.context.active_object
    bread.name = "BakerySign_CarvedLoaf"
    bread.scale = (1.4, 0.75, 0.65)
    bread.parent = root
    bread.data.materials.append(m_bread)

    export_fbx("Prop_BakeryBuilding.fbx")

# ==============================================================================
# 8. BAZAAR MARKET STALLS CLUSTER & POTIONS
# ==============================================================================
def build_market_stalls():
    reset_scene()
    m_wood = get_mat("M_MarketWood", (0.35, 0.22, 0.14, 1.0), roughness=0.75)
    m_cloth_red = get_mat("M_CanopyRed", (0.82, 0.18, 0.18, 1.0), roughness=0.6)
    m_cloth_gold = get_mat("M_CanopyGold", (0.92, 0.75, 0.22, 1.0), roughness=0.6)
    m_cloth_blue = get_mat("M_CanopyBlue", (0.18, 0.42, 0.82, 1.0), roughness=0.6)
    m_pot_ruby = get_mat("M_PotionRuby", (0.95, 0.15, 0.25, 1.0), roughness=0.2, emissive=(0.95, 0.15, 0.25, 1.0))
    m_pot_azure = get_mat("M_PotionAzure", (0.15, 0.65, 0.95, 1.0), roughness=0.2, emissive=(0.15, 0.65, 0.95, 1.0))

    root = bpy.data.objects.new("Prop_MarketStalls_Cluster", None)
    bpy.context.collection.objects.link(root)

    # Two connected market stalls
    for s_idx, (sx, canopy_mat) in enumerate([(-2.2, m_cloth_red), (2.2, m_cloth_blue)]):
        # Stall table / counter
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx, 0.9, 0))
        table = bpy.context.active_object
        table.name = f"StallTable_{s_idx}"
        table.scale = (3.4, 0.9, 1.8)
        table.parent = root
        table.data.materials.append(m_wood)

        # 4 Corner awning poles
        for px in [-1.5, 1.5]:
            for pz in [-0.8, 0.8]:
                bpy.ops.mesh.primitive_cylinder_add(radius=0.06, depth=2.8, location=(sx + px, 1.5, pz))
                pole = bpy.context.active_object
                pole.parent = root
                pole.data.materials.append(m_wood)

        # Striped fabric canopy awning
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=2.6, depth=1.1, location=(sx, 3.1, 0))
        canopy = bpy.context.active_object
        canopy.name = f"Canopy_{s_idx}"
        canopy.rotation_euler = (0, 0, math.pi*0.25)
        canopy.scale = (0.9, 1.4, 1.0)
        canopy.parent = root
        canopy.data.materials.append(canopy_mat)

        # Shelf with glass potion bottles
        for pot in range(4):
            pox = sx - 1.0 + pot * 0.65
            p_mat = m_pot_ruby if pot % 2 == 0 else m_pot_azure
            bpy.ops.mesh.primitive_cylinder_add(radius=0.10, depth=0.32, location=(pox, 1.52, -0.3))
            bottle = bpy.context.active_object
            bottle.name = f"Potion_{s_idx}_{pot}"
            bottle.parent = root
            bottle.data.materials.append(p_mat)

    export_fbx("Prop_MarketStalls_Cluster.fbx")

# ==============================================================================
# 9. HANGING PAPER LANTERNS FESTOON
# ==============================================================================
def build_paper_lanterns():
    reset_scene()
    m_paper = get_mat("M_PaperLantern", (0.98, 0.85, 0.45, 1.0), roughness=0.3, emissive=(1.0, 0.85, 0.40, 1.0))
    m_tassel = get_mat("M_LanternTassel", (0.85, 0.18, 0.18, 1.0), roughness=0.6)
    m_cord = get_mat("M_LanternCord", (0.15, 0.12, 0.10, 1.0), roughness=0.8)

    root = bpy.data.objects.new("Prop_PaperLanterns_String", None)
    bpy.context.collection.objects.link(root)

    span = 12.0
    num_lanterns = 6
    # Catinary catenary line
    bpy.ops.mesh.primitive_cylinder_add(radius=0.02, depth=span, location=(0, 4.2, 0))
    cable = bpy.context.active_object
    cable.rotation_euler = (0, math.pi*0.5, 0)
    cable.parent = root
    cable.data.materials.append(m_cord)

    for i in range(num_lanterns):
        u = (i + 0.5) / float(num_lanterns)
        lx = -span*0.5 + u * span
        sag_y = 4.2 - 0.7 * math.sin(u * math.pi)

        # Hexagonal Paper Lantern body
        bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.32, depth=0.65, location=(lx, sag_y - 0.45, 0))
        lant = bpy.context.active_object
        lant.name = f"Lantern_{i}"
        lant.parent = root
        lant.data.materials.append(m_paper)

        # Red hanging tassel
        bpy.ops.mesh.primitive_cylinder_add(radius=0.04, depth=0.35, location=(lx, sag_y - 0.95, 0))
        tas = bpy.context.active_object
        tas.parent = root
        tas.data.materials.append(m_tassel)

    export_fbx("Prop_PaperLanterns_String.fbx")

# ==============================================================================
# 10. BLACKSMITH FORGE & WORKSHOP
# ==============================================================================
def build_blacksmith_forge():
    reset_scene()
    m_stone = get_mat("M_ForgeStone", (0.42, 0.40, 0.38, 1.0), roughness=0.85)
    m_iron = get_mat("M_ForgeIron", (0.15, 0.15, 0.17, 1.0), roughness=0.5, metallic=0.8)
    m_wood = get_mat("M_ForgeWood", (0.32, 0.20, 0.12, 1.0), roughness=0.8)
    m_fire = get_mat("M_ForgeFireCoals", (1.0, 0.45, 0.05, 1.0), roughness=0.2, emissive=(1.0, 0.45, 0.05, 1.0))
    m_steel = get_mat("M_WeaponSteel", (0.85, 0.85, 0.88, 1.0), roughness=0.25, metallic=0.95)

    root = bpy.data.objects.new("Prop_BlacksmithForge_Complete", None)
    bpy.context.collection.objects.link(root)

    # Timber Awning Shed
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.5, 0))
    shed = bpy.context.active_object
    shed.name = "Forge_Awning"
    shed.scale = (7.0, 0.3, 5.0)
    shed.parent = root
    shed.data.materials.append(m_wood)

    for px in [-3.2, 3.2]:
        for pz in [-2.2, 2.2]:
            bpy.ops.mesh.primitive_cylinder_add(radius=0.15, depth=4.8, location=(px, 0.2, pz))
            post = bpy.context.active_object
            post.parent = root
            post.data.materials.append(m_wood)

    # Stone Hearth Forge & Chimney
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-1.8, 1.0, 0))
    hearth = bpy.context.active_object
    hearth.name = "Forge_Hearth"
    hearth.scale = (2.4, 1.4, 2.4)
    hearth.parent = root
    hearth.data.materials.append(m_stone)

    # Chimney hood
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=1.8, depth=2.8, location=(-1.8, 3.5, 0))
    hood = bpy.context.active_object
    hood.rotation_euler = (0, 0, math.pi*0.25)
    hood.parent = root
    hood.data.materials.append(m_stone)

    # Glowing Coal Fire Basin
    bpy.ops.mesh.primitive_cylinder_add(radius=0.8, depth=0.2, location=(-1.8, 1.75, 0))
    fire = bpy.context.active_object
    fire.name = "Forge_FireCoals"
    fire.parent = root
    fire.data.materials.append(m_fire)

    # Heavy Anvil on Stump
    bpy.ops.mesh.primitive_cylinder_add(radius=0.55, depth=0.8, location=(1.5, 0.4, 0))
    stump = bpy.context.active_object
    stump.name = "Forge_AnvilStump"
    stump.parent = root
    stump.data.materials.append(m_wood)

    # Cast Iron Anvil
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(1.5, 1.0, 0))
    anvil = bpy.context.active_object
    anvil.name = "Forge_Anvil"
    anvil.scale = (1.2, 0.45, 0.5)
    anvil.parent = root
    anvil.data.materials.append(m_iron)

    # Weapon Display Rack with Broadswords & Shields
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(2.8, 1.2, 1.8))
    rack = bpy.context.active_object
    rack.name = "Forge_WeaponRack"
    rack.scale = (2.2, 2.4, 0.4)
    rack.parent = root
    rack.data.materials.append(m_wood)

    # 3 Broadswords on rack
    for sw in range(3):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(2.2 + sw * 0.6, 1.2, 1.95))
        sword = bpy.context.active_object
        sword.scale = (0.1, 1.8, 0.05)
        sword.parent = root
        sword.data.materials.append(m_steel)

    export_fbx("Prop_BlacksmithForge_Complete.fbx")

# ==============================================================================
# 11. SPARRING ARENA TARGETS & DUMMIES
# ==============================================================================
def build_sparring_arena():
    reset_scene()
    m_wood = get_mat("M_DummyWood", (0.42, 0.28, 0.16, 1.0), roughness=0.8)
    m_straw = get_mat("M_DummyStraw", (0.78, 0.68, 0.35, 1.0), roughness=0.9)
    m_target_red = get_mat("M_TargetRed", (0.85, 0.18, 0.18, 1.0), roughness=0.6)
    m_target_white = get_mat("M_TargetWhite", (0.92, 0.92, 0.90, 1.0), roughness=0.6)

    root = bpy.data.objects.new("Prop_SparringArena_TargetsDummies", None)
    bpy.context.collection.objects.link(root)

    # Combat Training Dummy (Wooden post with crossbeam & straw chest)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.18, depth=2.4, location=(-2.0, 1.2, 0))
    dummy_post = bpy.context.active_object
    dummy_post.name = "TrainingDummy_Post"
    dummy_post.parent = root
    dummy_post.data.materials.append(m_wood)

    # Crossbeam arms
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-2.0, 1.6, 0))
    arms = bpy.context.active_object
    arms.scale = (1.6, 0.15, 0.15)
    arms.parent = root
    arms.data.materials.append(m_wood)

    # Straw wrapped body
    bpy.ops.mesh.primitive_cylinder_add(radius=0.35, depth=1.1, location=(-2.0, 1.3, 0))
    body = bpy.context.active_object
    body.parent = root
    body.data.materials.append(m_straw)

    # Archery Target 1 & 2
    for t_idx, tx in enumerate([1.2, 3.2]):
        # Tripod legs
        for leg in [-0.4, 0.4]:
            bpy.ops.mesh.primitive_cylinder_add(radius=0.06, depth=1.8, location=(tx + leg, 0.8, -0.2))
            l = bpy.context.active_object
            l.rotation_euler = (0.2, 0, leg * 0.4)
            l.parent = root
            l.data.materials.append(m_wood)

        # Concentric Bullseye Target Disc
        bpy.ops.mesh.primitive_cylinder_add(radius=0.85, depth=0.15, location=(tx, 1.4, 0))
        target_outer = bpy.context.active_object
        target_outer.rotation_euler = (math.pi*0.5 - 0.1, 0, 0)
        target_outer.parent = root
        target_outer.data.materials.append(m_target_white)

        # Red center bullseye
        bpy.ops.mesh.primitive_cylinder_add(radius=0.45, depth=0.18, location=(tx, 1.4, 0.02))
        bullseye = bpy.context.active_object
        bullseye.rotation_euler = (math.pi*0.5 - 0.1, 0, 0)
        bullseye.parent = root
        bullseye.data.materials.append(m_target_red)

    export_fbx("Prop_SparringArena_TargetsDummies.fbx")

# ==============================================================================
# 12. ANCIENT MOSS STONE GOLEM
# ==============================================================================
def build_ancient_stone_golem():
    reset_scene()
    m_stone = get_mat("M_GolemStone", (0.45, 0.43, 0.40, 1.0), roughness=0.9)
    m_moss = get_mat("M_GolemMoss", (0.22, 0.52, 0.16, 1.0), roughness=0.6)
    m_rune = get_mat("M_GolemRuneGlow", (0.25, 0.85, 0.98, 1.0), roughness=0.2, emissive=(0.25, 0.85, 0.98, 1.0))

    root = bpy.data.objects.new("Golem_AncientMossStone", None)
    bpy.context.collection.objects.link(root)

    # Massive Boulder Torso (4.2m tall titan)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.6, location=(0, 2.6, 0))
    torso = bpy.context.active_object
    torso.name = "StoneGolem_Torso"
    torso.scale = (1.2, 1.4, 1.0)
    torso.parent = root
    torso.data.materials.append(m_stone)

    # Glowing Arcane Rune Plate on chest
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.65, depth=0.2, location=(0, 2.7, 0.95))
    rune = bpy.context.active_object
    rune.name = "StoneGolem_RuneCore"
    rune.rotation_euler = (math.pi*0.5, 0, 0)
    rune.parent = root
    rune.data.materials.append(m_rune)

    # Stone Boulder Head with glowing eye slot
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.85, location=(0, 4.3, 0.2))
    head = bpy.context.active_object
    head.name = "StoneGolem_Head"
    head.scale = (1.1, 0.9, 1.0)
    head.parent = root
    head.data.materials.append(m_stone)

    # Glowing Eye slit
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 4.35, 0.95))
    eye = bpy.context.active_object
    eye.scale = (0.6, 0.15, 0.2)
    eye.parent = root
    eye.data.materials.append(m_rune)

    # Huge Boulder Arms & Fists
    for side in [-1, 1]:
        ax = side * 2.3
        # Shoulder pauldron boulder
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.9, location=(ax, 3.2, 0))
        shoulder = bpy.context.active_object
        shoulder.parent = root
        shoulder.data.materials.append(m_moss)

        # Forearm & Boulder Fist
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=(ax * 1.15, 1.5, 0.4))
        fist = bpy.context.active_object
        fist.parent = root
        fist.data.materials.append(m_stone)

    # Pillar Legs
    for side in [-1, 1]:
        lx = side * 1.0
        bpy.ops.mesh.primitive_cylinder_add(radius=0.65, depth=2.0, location=(lx, 1.0, 0))
        leg = bpy.context.active_object
        leg.parent = root
        leg.data.materials.append(m_stone)

    export_fbx("Golem_AncientMossStone.fbx")

# ==============================================================================
# 13. INDUSTRIAL RUSTED IRON AUTOMATON
# ==============================================================================
def build_industrial_iron_automaton():
    reset_scene()
    m_rust = get_mat("M_AutoRustedIron", (0.32, 0.24, 0.20, 1.0), roughness=0.6, metallic=0.7)
    m_brass = get_mat("M_AutoBrassGear", (0.75, 0.60, 0.25, 1.0), roughness=0.4, metallic=0.85)
    m_core = get_mat("M_AutoFurnaceCore", (1.0, 0.55, 0.10, 1.0), roughness=0.2, emissive=(1.0, 0.55, 0.10, 1.0))

    root = bpy.data.objects.new("Golem_IndustrialRustedIron", None)
    bpy.context.collection.objects.link(root)

    # Riveted Boiler Cylinder Torso (3.6m tall)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=1.1, depth=2.2, location=(0, 2.5, 0))
    torso = bpy.context.active_object
    torso.name = "IronAuto_Torso"
    torso.parent = root
    torso.data.materials.append(m_rust)

    # Circular Furnace Hatch with glowing core
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.6, depth=0.25, location=(0, 2.4, 1.05))
    hatch = bpy.context.active_object
    hatch.name = "IronAuto_FurnaceHatch"
    hatch.rotation_euler = (math.pi*0.5, 0, 0)
    hatch.parent = root
    hatch.data.materials.append(m_core)

    # Rivet ring around boiler
    bpy.ops.mesh.primitive_torus_add(major_radius=1.15, minor_radius=0.08, location=(0, 3.1, 0))
    rivet = bpy.context.active_object
    rivet.parent = root
    rivet.data.materials.append(m_brass)

    # Iron Dome Head with Brass Visor
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=12, radius=0.75, location=(0, 3.8, 0))
    head = bpy.context.active_object
    head.name = "IronAuto_Head"
    head.parent = root
    head.data.materials.append(m_rust)

    # Shoulder Exhaust Smokestacks
    for side in [-1, 1]:
        sx = side * 0.75
        bpy.ops.mesh.primitive_cylinder_add(radius=0.18, depth=1.2, location=(sx, 4.2, -0.6))
        pipe = bpy.context.active_object
        pipe.parent = root
        pipe.data.materials.append(m_rust)

    # Piston arms with claw hands
    for side in [-1, 1]:
        ax = side * 1.7
        bpy.ops.mesh.primitive_cylinder_add(radius=0.25, depth=1.8, location=(ax, 2.2, 0))
        arm = bpy.context.active_object
        arm.parent = root
        arm.data.materials.append(m_rust)

        # Claw hand
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(ax, 1.1, 0.2))
        claw = bpy.context.active_object
        claw.scale = (0.45, 0.45, 0.6)
        claw.parent = root
        claw.data.materials.append(m_brass)

    # Sturdy Iron Piston Legs
    for side in [-1, 1]:
        lx = side * 0.7
        bpy.ops.mesh.primitive_cylinder_add(radius=0.45, depth=1.8, location=(lx, 0.9, 0))
        leg = bpy.context.active_object
        leg.parent = root
        leg.data.materials.append(m_rust)

    export_fbx("Golem_IndustrialRustedIron.fbx")

# ==============================================================================
# 14. CLOCKWORK BRASS ROBOT ASSISTANT
# ==============================================================================
def build_clockwork_brass_assistant():
    reset_scene()
    m_brass = get_mat("M_ClockworkBrass", (0.88, 0.72, 0.25, 1.0), roughness=0.35, metallic=0.9)
    m_gear = get_mat("M_ClockworkSteelGear", (0.75, 0.75, 0.80, 1.0), roughness=0.3, metallic=0.85)
    m_lens = get_mat("M_ClockworkOcularLens", (0.2, 0.85, 1.0, 1.0), roughness=0.1, emissive=(0.2, 0.85, 1.0, 1.0))

    root = bpy.data.objects.new("Golem_ClockworkBrassAssistant", None)
    bpy.context.collection.objects.link(root)

    # 1.3m tall charming clockwork robot
    bpy.ops.mesh.primitive_cylinder_add(radius=0.45, depth=0.8, location=(0, 0.85, 0))
    torso = bpy.context.active_object
    torso.name = "BrassBot_Torso"
    torso.parent = root
    torso.data.materials.append(m_brass)

    # Brass Spherical Head
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=12, radius=0.42, location=(0, 1.45, 0))
    head = bpy.context.active_object
    head.name = "BrassBot_Head"
    head.parent = root
    head.data.materials.append(m_brass)

    # Dual Glowing Ocular Lenses
    for eye_x in [-0.16, 0.16]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.15, location=(eye_x, 1.5, 0.38))
        lens = bpy.context.active_object
        lens.rotation_euler = (math.pi*0.5, 0, 0)
        lens.parent = root
        lens.data.materials.append(m_lens)

    # Wind-Up Key in Back!
    bpy.ops.mesh.primitive_cylinder_add(radius=0.04, depth=0.3, location=(0, 0.95, -0.55))
    key_shaft = bpy.context.active_object
    key_shaft.rotation_euler = (math.pi*0.5, 0, 0)
    key_shaft.parent = root
    key_shaft.data.materials.append(m_brass)

    # Key wing handle
    bpy.ops.mesh.primitive_torus_add(major_radius=0.18, minor_radius=0.04, location=(0, 0.95, -0.72))
    key_wing = bpy.context.active_object
    key_wing.parent = root
    key_wing.data.materials.append(m_brass)

    # Small brass carrying basket
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.65, 0.55))
    basket = bpy.context.active_object
    basket.scale = (0.55, 0.35, 0.4)
    basket.parent = root
    basket.data.materials.append(m_gear)

    export_fbx("Golem_ClockworkBrassAssistant.fbx")

# ==============================================================================
# 15. ARMORED GIANT WOLF BEAST & KNIGHT RIDER
# ==============================================================================
def build_armored_wolf_mount():
    reset_scene()
    m_fur = get_mat("M_WolfGreyFur", (0.35, 0.37, 0.42, 1.0), roughness=0.85)
    m_armor = get_mat("M_WolfPlateArmor", (0.75, 0.78, 0.82, 1.0), roughness=0.3, metallic=0.9)
    m_gold = get_mat("M_WolfGoldTrim", (0.88, 0.72, 0.25, 1.0), roughness=0.35, metallic=0.9)
    m_leather = get_mat("M_SaddleLeather", (0.28, 0.16, 0.10, 1.0), roughness=0.6)

    root = bpy.data.objects.new("Mount_ArmoredWolfBeast", None)
    bpy.context.collection.objects.link(root)

    # Massive Dire Wolf Body (2.4m shoulder height)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.7, 0))
    body = bpy.context.active_object
    body.name = "Wolf_Body"
    body.scale = (1.1, 1.3, 2.8)
    body.parent = root
    body.data.materials.append(m_fur)

    # Wolf Neck & Head
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.5, 1.4))
    neck = bpy.context.active_object
    neck.name = "Wolf_Neck"
    neck.scale = (0.8, 1.2, 1.1)
    neck.parent = root
    neck.data.materials.append(m_fur)

    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.6, depth=1.3, location=(0, 2.5, 2.1))
    snout = bpy.context.active_object
    snout.rotation_euler = (math.pi*0.5, 0, 0)
    snout.parent = root
    snout.data.materials.append(m_fur)

    # Steel Chamfron (Head Armor Plate)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.8, 1.8))
    chamfron = bpy.context.active_object
    chamfron.name = "Wolf_Chamfron"
    chamfron.scale = (0.75, 0.45, 1.0)
    chamfron.parent = root
    chamfron.data.materials.append(m_armor)

    # Wolf Ears
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.25, depth=0.7, location=(side * 0.45, 3.2, 1.3))
        ear = bpy.context.active_object
        ear.parent = root
        ear.data.materials.append(m_fur)

    # 4 Strong Canine Legs
    for side in [-1, 1]:
        for zpos in [-1.0, 1.0]:
            bpy.ops.mesh.primitive_cylinder_add(radius=0.28, depth=1.8, location=(side * 0.75, 0.9, zpos))
            leg = bpy.context.active_object
            leg.parent = root
            leg.data.materials.append(m_fur)

    # Bushy Tail
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.35, depth=1.8, location=(0, 1.5, -1.8))
    tail = bpy.context.active_object
    tail.rotation_euler = (-0.6, 0, 0)
    tail.parent = root
    tail.data.materials.append(m_fur)

    # Saddle & Saddlebags
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.45, 0))
    saddle = bpy.context.active_object
    saddle.scale = (1.2, 0.35, 1.1)
    saddle.parent = root
    saddle.data.materials.append(m_leather)

    # Mounted Knight Adventurer Rider
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 3.2, 0))
    rider_torso = bpy.context.active_object
    rider_torso.name = "Knight_Rider"
    rider_torso.scale = (0.75, 1.1, 0.6)
    rider_torso.parent = root
    rider_torso.data.materials.append(m_armor)

    # Knight Helmet
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.35, location=(0, 3.9, 0))
    helm = bpy.context.active_object
    helm.parent = root
    helm.data.materials.append(m_armor)

    export_fbx("Mount_ArmoredWolfBeast.fbx")

# ==============================================================================
# 16. HORNED REPTILE PACK BEAST & TRAVELER
# ==============================================================================
def build_horned_pack_beast():
    reset_scene()
    m_scale = get_mat("M_ReptileScale", (0.38, 0.45, 0.32, 1.0), roughness=0.7)
    m_horn = get_mat("M_ReptileHorn", (0.75, 0.70, 0.62, 1.0), roughness=0.5)
    m_pack = get_mat("M_PackCloth", (0.72, 0.55, 0.38, 1.0), roughness=0.85)

    root = bpy.data.objects.new("Mount_HornedReptilePackBeast", None)
    bpy.context.collection.objects.link(root)

    # Quadrupedal Heavy Pack Beast Body
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.6, 0))
    body = bpy.context.active_object
    body.name = "ReptileBeast_Body"
    body.scale = (1.4, 1.3, 3.0)
    body.parent = root
    body.data.materials.append(m_scale)

    # Head with sweeping horns
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.1, 1.8))
    head = bpy.context.active_object
    head.scale = (1.1, 0.9, 1.3)
    head.parent = root
    head.data.materials.append(m_scale)

    # Dual Horns
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.25, depth=1.6, location=(side * 0.75, 2.7, 1.8))
        horn = bpy.context.active_object
        horn.rotation_euler = (-0.4, side * 0.5, 0)
        horn.parent = root
        horn.data.materials.append(m_horn)

    # Heavy Cargo Crates & Bedroll on back
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.6, -0.3))
    cargo = bpy.context.active_object
    cargo.name = "Cargo_Pack"
    cargo.scale = (1.6, 1.0, 1.6)
    cargo.parent = root
    cargo.data.materials.append(m_pack)

    # 4 Stocky Legs
    for side in [-1, 1]:
        for zpos in [-1.1, 1.1]:
            bpy.ops.mesh.primitive_cylinder_add(radius=0.35, depth=1.6, location=(side * 0.9, 0.8, zpos))
            leg = bpy.context.active_object
            leg.parent = root
            leg.data.materials.append(m_scale)

    export_fbx("Mount_HornedReptilePackBeast.fbx")

# ==============================================================================
# 17. FEATHERED CHOCOBO BIRD MOUNT
# ==============================================================================
def build_feathered_bird_mount():
    reset_scene()
    m_feather = get_mat("M_BirdFeatherBlue", (0.22, 0.55, 0.88, 1.0), roughness=0.6)
    m_beak = get_mat("M_BirdBeakGold", (0.95, 0.78, 0.20, 1.0), roughness=0.4)
    m_saddle = get_mat("M_BirdSaddle", (0.75, 0.18, 0.20, 1.0), roughness=0.6)

    root = bpy.data.objects.new("Mount_FeatheredChocoboBird", None)
    bpy.context.collection.objects.link(root)

    # Oval Avian Body
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=12, radius=1.1, location=(0, 1.8, 0))
    body = bpy.context.active_object
    body.name = "Bird_Body"
    body.scale = (1.0, 1.1, 1.5)
    body.parent = root
    body.data.materials.append(m_feather)

    # S-curved Long Neck
    bpy.ops.mesh.primitive_cylinder_add(radius=0.3, depth=1.4, location=(0, 2.8, 0.8))
    neck = bpy.context.active_object
    neck.rotation_euler = (0.35, 0, 0)
    neck.parent = root
    neck.data.materials.append(m_feather)

    # Bird Head & Beak
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.45, location=(0, 3.4, 1.1))
    head = bpy.context.active_object
    head.parent = root
    head.data.materials.append(m_feather)

    # Golden Beak
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.22, depth=0.8, location=(0, 3.3, 1.6))
    beak = bpy.context.active_object
    beak.rotation_euler = (math.pi*0.5, 0, 0)
    beak.parent = root
    beak.data.materials.append(m_beak)

    # Long Avian Stilt Legs with Talons
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.14, depth=2.0, location=(side * 0.45, 1.0, 0))
        leg = bpy.context.active_object
        leg.parent = root
        leg.data.materials.append(m_beak)

    # Plumed Feather Tail
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.6, depth=1.6, location=(0, 2.0, -1.8))
    tail = bpy.context.active_object
    tail.rotation_euler = (-0.8, 0, 0)
    tail.parent = root
    tail.data.materials.append(m_feather)

    # Saddle
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.7, 0))
    saddle = bpy.context.active_object
    saddle.scale = (0.9, 0.25, 0.9)
    saddle.parent = root
    saddle.data.materials.append(m_saddle)

    export_fbx("Mount_FeatheredChocoboBird.fbx")

# ==============================================================================
# 18. ANIME WITCH FLYING ON BROOMSTICK
# ==============================================================================
def build_witch_flying_broom():
    reset_scene()
    m_robe = get_mat("M_WitchRobePurple", (0.35, 0.15, 0.48, 1.0), roughness=0.6)
    m_hair = get_mat("M_WitchHairAuburn", (0.78, 0.35, 0.18, 1.0), roughness=0.6)
    m_broom = get_mat("M_BroomWood", (0.42, 0.28, 0.16, 1.0), roughness=0.7)
    m_straw = get_mat("M_BroomStraw", (0.82, 0.72, 0.35, 1.0), roughness=0.8)
    m_spark = get_mat("M_MagicSparkle", (0.95, 0.45, 0.85, 1.0), roughness=0.1, emissive=(0.95, 0.45, 0.85, 1.0))

    root = bpy.data.objects.new("Character_WitchFlyingBroom", None)
    bpy.context.collection.objects.link(root)

    # Gnarled Wooden Broomstick
    bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=2.8, location=(0, 0, 0))
    broom = bpy.context.active_object
    broom.name = "Broomstick"
    broom.rotation_euler = (0.2, 0, 0)
    broom.parent = root
    broom.data.materials.append(m_broom)

    # Straw Bristles at back of broom
    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=0.35, depth=0.9, location=(0, -0.2, -1.3))
    bristles = bpy.context.active_object
    bristles.rotation_euler = (-math.pi*0.5 + 0.2, 0, 0)
    bristles.parent = root
    bristles.data.materials.append(m_straw)

    # Witch Body seated astride broom
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.55, 0.1))
    body = bpy.context.active_object
    body.name = "Witch_Body"
    body.scale = (0.55, 0.85, 0.45)
    body.parent = root
    body.data.materials.append(m_robe)

    # Witch Head
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.28, location=(0, 1.15, 0.1))
    head = bpy.context.active_object
    head.parent = root
    head.data.materials.append(m_hair)

    # Iconic Pointed Witch Hat
    bpy.ops.mesh.primitive_cylinder_add(radius=0.6, depth=0.06, location=(0, 1.35, 0.1))
    brim = bpy.context.active_object
    brim.parent = root
    brim.data.materials.append(m_robe)

    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=0.38, depth=0.85, location=(0, 1.8, 0.05))
    cone = bpy.context.active_object
    cone.rotation_euler = (-0.2, 0, 0)
    cone.parent = root
    cone.data.materials.append(m_robe)

    # Magic Star Sparkle tip
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.12, location=(0.35, 0.8, 1.2))
    sparkle = bpy.context.active_object
    sparkle.parent = root
    sparkle.data.materials.append(m_spark)

    export_fbx("Character_WitchFlyingBroom.fbx")

# ==============================================================================
# 19. SKY WYVERN DRAGON
# ==============================================================================
def build_sky_wyvern():
    reset_scene()
    m_dragon = get_mat("M_DragonEmeraldScales", (0.18, 0.52, 0.35, 1.0), roughness=0.6)
    m_wing = get_mat("M_DragonWingMembrane", (0.35, 0.65, 0.45, 1.0), roughness=0.5)
    m_horn = get_mat("M_DragonHornGold", (0.92, 0.78, 0.28, 1.0), roughness=0.4, metallic=0.7)

    root = bpy.data.objects.new("Creature_SkyWyvernDragon", None)
    bpy.context.collection.objects.link(root)

    # Sinuous Dragon Body (9m wingspan)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.5, depth=4.2, location=(0, 0, 0))
    body = bpy.context.active_object
    body.name = "Dragon_Body"
    body.rotation_euler = (math.pi*0.5, 0, 0)
    body.parent = root
    body.data.materials.append(m_dragon)

    # Dragon Head & Horns
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.45, depth=1.4, location=(0, 0.2, 2.4))
    head = bpy.context.active_object
    head.rotation_euler = (-math.pi*0.5, 0, 0)
    head.parent = root
    head.data.materials.append(m_dragon)

    # Swept-back Horns
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.15, depth=1.1, location=(side * 0.3, 0.5, 2.1))
        horn = bpy.context.active_object
        horn.rotation_euler = (0.5, side * 0.4, 0)
        horn.parent = root
        horn.data.materials.append(m_horn)

    # Massive Broad Wings (Left & Right)
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(side * 2.8, 0.3, 0.2))
        wing = bpy.context.active_object
        wing.name = f"DragonWing_{side}"
        wing.scale = (4.5, 0.08, 2.4)
        wing.rotation_euler = (0, side * 0.15, side * 0.2)
        wing.parent = root
        wing.data.materials.append(m_wing)

    # Long Tail
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.4, depth=3.2, location=(0, -0.1, -3.2))
    tail = bpy.context.active_object
    tail.rotation_euler = (math.pi*0.5, 0, 0)
    tail.parent = root
    tail.data.materials.append(m_dragon)

    export_fbx("Creature_SkyWyvernDragon.fbx")

# ==============================================================================
# 20. BEASTFOLK (WOLF & FOX CHARACTERS)
# ==============================================================================
def build_beastfolk_wolf():
    reset_scene()
    m_fur_wolf = get_mat("M_BeastfolkWolfFur", (0.45, 0.45, 0.48, 1.0), roughness=0.7)
    m_tunic = get_mat("M_BeastfolkTunic", (0.28, 0.45, 0.35, 1.0), roughness=0.6)

    root_wolf = bpy.data.objects.new("Character_Beastfolk_Wolf", None)
    bpy.context.collection.objects.link(root_wolf)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.0, 0))
    torso = bpy.context.active_object
    torso.scale = (0.75, 1.2, 0.5)
    torso.parent = root_wolf
    torso.data.materials.append(m_tunic)

    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.35, location=(0, 1.8, 0))
    head = bpy.context.active_object
    head.parent = root_wolf
    head.data.materials.append(m_fur_wolf)

    # Pointed Wolf Ears
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.15, depth=0.45, location=(side * 0.25, 2.15, 0))
        ear = bpy.context.active_object
        ear.parent = root_wolf
        ear.data.materials.append(m_fur_wolf)

    # Bushy Wolf Tail
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.22, depth=0.9, location=(0, 0.8, -0.6))
    tail = bpy.context.active_object
    tail.rotation_euler = (-0.6, 0, 0)
    tail.parent = root_wolf
    tail.data.materials.append(m_fur_wolf)

    export_fbx("Character_Beastfolk_Wolf.fbx")

def build_beastfolk_fox():
    reset_scene()
    m_fur_fox = get_mat("M_BeastfolkFoxFur", (0.92, 0.45, 0.18, 1.0), roughness=0.6)
    m_vest = get_mat("M_BeastfolkFoxVest", (0.75, 0.65, 0.25, 1.0), roughness=0.6)

    root_fox = bpy.data.objects.new("Character_Beastfolk_Fox", None)
    bpy.context.collection.objects.link(root_fox)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.0, 0))
    torso = bpy.context.active_object
    torso.scale = (0.7, 1.1, 0.45)
    torso.parent = root_fox
    torso.data.materials.append(m_vest)

    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.32, location=(0, 1.75, 0))
    head = bpy.context.active_object
    head.parent = root_fox
    head.data.materials.append(m_fur_fox)

    # Alert Fox Ears
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.14, depth=0.45, location=(side * 0.22, 2.1, 0))
        ear = bpy.context.active_object
        ear.parent = root_fox
        ear.data.materials.append(m_fur_fox)

    # Large Bushy Fox Tail with white tip
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.28, depth=1.1, location=(0, 0.8, -0.7))
    tail = bpy.context.active_object
    tail.rotation_euler = (-0.7, 0, 0)
    tail.parent = root_fox
    tail.data.materials.append(m_fur_fox)

    export_fbx("Character_Beastfolk_Fox.fbx")

# ==============================================================================
# 21. BARD WITH LUTE, RUNNING CHILDREN, OWL & FAIRY
# ==============================================================================
def build_bard_character():
    reset_scene()
    m_bard = get_mat("M_BardDoublet", (0.85, 0.25, 0.28, 1.0), roughness=0.6)
    m_lute = get_mat("M_LuteWood", (0.75, 0.50, 0.20, 1.0), roughness=0.5)

    root_bard = bpy.data.objects.new("Character_BardLute", None)
    bpy.context.collection.objects.link(root_bard)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.0, 0))
    torso = bpy.context.active_object
    torso.scale = (0.75, 1.1, 0.5)
    torso.parent = root_bard
    torso.data.materials.append(m_bard)

    # Pear-shaped wooden Lute held in arms
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.35, location=(0.2, 1.0, 0.45))
    lute_body = bpy.context.active_object
    lute_body.scale = (0.7, 0.9, 0.45)
    lute_body.parent = root_bard
    lute_body.data.materials.append(m_lute)

    bpy.ops.mesh.primitive_cylinder_add(radius=0.06, depth=0.8, location=(0.45, 1.35, 0.5))
    lute_neck = bpy.context.active_object
    lute_neck.rotation_euler = (0, 0, -0.6)
    lute_neck.parent = root_bard
    lute_neck.data.materials.append(m_lute)

    export_fbx("Character_BardLute.fbx")

def build_running_kids():
    reset_scene()
    m_cloth_a = get_mat("M_KidClothGreen", (0.25, 0.65, 0.35, 1.0), roughness=0.7)
    m_cloth_b = get_mat("M_KidClothGold", (0.88, 0.75, 0.25, 1.0), roughness=0.7)
    m_skin = get_mat("M_KidSkin", (0.95, 0.85, 0.75, 1.0), roughness=0.4)

    root = bpy.data.objects.new("Character_RunningKids", None)
    bpy.context.collection.objects.link(root)

    for k_idx, (kx, k_mat) in enumerate([(-0.6, m_cloth_a), (0.6, m_cloth_b)]):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(kx, 0.65, 0))
        kid = bpy.context.active_object
        kid.name = f"Kid_{k_idx}_Torso"
        kid.scale = (0.45, 0.7, 0.35)
        kid.parent = root
        kid.data.materials.append(k_mat)

        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=8, radius=0.22, location=(kx, 1.15, 0))
        head = bpy.context.active_object
        head.parent = root
        head.data.materials.append(m_skin)

    export_fbx("Character_RunningKids.fbx")

def build_glowing_fairy():
    reset_scene()
    m_fairy = get_mat("M_FairyGlow", (0.45, 0.95, 1.0, 1.0), roughness=0.1, emissive=(0.45, 0.95, 1.0, 1.0))

    root_fairy = bpy.data.objects.new("Critter_GlowingFairy", None)
    bpy.context.collection.objects.link(root_fairy)

    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=8, radius=0.15, location=(0, 0, 0))
    fairy_core = bpy.context.active_object
    fairy_core.parent = root_fairy
    fairy_core.data.materials.append(m_fairy)

    # 4 Gossamer Wings
    for side in [-1, 1]:
        for w_idx, wy in enumerate([0.15, -0.1]):
            bpy.ops.mesh.primitive_circle_add(vertices=8, radius=0.35, location=(side * 0.3, wy, 0))
            wing = bpy.context.active_object
            wing.rotation_euler = (0, side * 0.4, 0)
            wing.parent = root_fairy
            wing.data.materials.append(m_fairy)

    export_fbx("Critter_GlowingFairy.fbx")

def build_perched_owl():
    reset_scene()
    m_owl = get_mat("M_OwlFeathers", (0.42, 0.35, 0.25, 1.0), roughness=0.8)

    root_owl = bpy.data.objects.new("Critter_PerchedOwl", None)
    bpy.context.collection.objects.link(root_owl)

    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=8, radius=0.25, location=(0, 0.35, 0))
    owl_body = bpy.context.active_object
    owl_body.scale = (0.9, 1.2, 0.9)
    owl_body.parent = root_owl
    owl_body.data.materials.append(m_owl)

    # Horn tufts
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cone_add(vertices=3, radius1=0.08, depth=0.22, location=(side * 0.12, 0.65, 0.05))
        tuft = bpy.context.active_object
        tuft.parent = root_owl
        tuft.data.materials.append(m_owl)

    export_fbx("Critter_PerchedOwl.fbx")

# ==============================================================================
# 22. OVERHANGING GRAND OAK & CASCADE WATERFALL
# ==============================================================================
def build_overhanging_oak():
    reset_scene()
    m_bark = get_mat("M_OakTrunkBark", (0.28, 0.18, 0.12, 1.0), roughness=0.85)
    m_leaf = get_mat("M_OakLushLeaves", (0.22, 0.52, 0.18, 1.0), roughness=0.6)

    root_tree = bpy.data.objects.new("Foliage_OverhangingGrandOak", None)
    bpy.context.collection.objects.link(root_tree)

    # Gnarled Trunk that arches outward
    bpy.ops.mesh.primitive_cylinder_add(radius=1.2, depth=7.0, location=(0, 3.5, 0))
    trunk = bpy.context.active_object
    trunk.name = "Oak_Trunk"
    trunk.rotation_euler = (0.18, 0, 0.12)
    trunk.parent = root_tree
    trunk.data.materials.append(m_bark)

    # Heavy twisting overhanging boughs
    boughs = [
        {"loc": (2.2, 6.2, 1.8), "rot": (0.35, 0.5, 0.2), "rad": 0.65, "len": 5.2},
        {"loc": (-1.8, 5.8, 2.4), "rot": (0.45, -0.4, -0.2), "rad": 0.55, "len": 4.5},
        {"loc": (0.4, 7.5, -1.5), "rot": (-0.3, 0.2, 0.1), "rad": 0.6, "len": 4.0}
    ]

    for b_idx, b in enumerate(boughs):
        bpy.ops.mesh.primitive_cylinder_add(radius=b["rad"], depth=b["len"], location=b["loc"])
        branch = bpy.context.active_object
        branch.rotation_euler = b["rot"]
        branch.parent = root_tree
        branch.data.materials.append(m_bark)

        # Dense Leaf Clusters on branch ends
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=2.2, location=b["loc"])
        leaf = bpy.context.active_object
        leaf.name = f"LeafCluster_{b_idx}"
        leaf.scale = (1.4, 1.1, 1.5)
        leaf.parent = root_tree
        leaf.data.materials.append(m_leaf)

    export_fbx("Foliage_OverhangingGrandOak.fbx")

def build_cliffside_waterfall():
    reset_scene()
    m_water = get_mat("M_CascadeWater", (0.25, 0.75, 0.95, 1.0), roughness=0.1, metallic=0.15)
    m_foam = get_mat("M_CascadeFoam", (0.95, 0.98, 1.0, 1.0), roughness=0.2)

    root_fall = bpy.data.objects.new("Foliage_CliffsideWaterfall", None)
    bpy.context.collection.objects.link(root_fall)

    # 3 Stepped Waterfall Sheets plunging down cliff
    steps = [
        {"y": 14.0, "z": 0.0, "len": 6.0, "w": 4.5},
        {"y": 8.0, "z": 2.5, "len": 7.0, "w": 5.2},
        {"y": 2.0, "z": 5.5, "len": 6.5, "w": 6.0}
    ]

    for s_idx, s in enumerate(steps):
        bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, s["y"], s["z"]))
        sheet = bpy.context.active_object
        sheet.name = f"WaterfallSheet_{s_idx}"
        sheet.scale = (s["w"], s["len"], 1.0)
        sheet.rotation_euler = (math.pi*0.42, 0, 0)
        sheet.parent = root_fall
        sheet.data.materials.append(m_water)

        # Foaming splash pool at base
        bpy.ops.mesh.primitive_cylinder_add(radius=s["w"]*0.7, depth=0.25, location=(0, s["y"] - s["len"]*0.45, s["z"] + 1.2))
        foam = bpy.context.active_object
        foam.parent = root_fall
        foam.data.materials.append(m_foam)

    export_fbx("Foliage_CliffsideWaterfall.fbx")

# ==============================================================================
# MAIN BATCH EXECUTION
# ==============================================================================
if __name__ == "__main__":
    print("==================================================================")
    print("STARTING BLENDER PROCEDURAL GENERATION OF FANTASY METROPOLIS ASSETS")
    print("==================================================================")
    build_grand_stairs()
    build_retaining_walls()
    build_sky_bridge_aqueduct()
    build_church_of_revival()
    build_academy_buildings()
    build_timber_houses()
    build_bakery_building()
    build_market_stalls()
    build_paper_lanterns()
    build_blacksmith_forge()
    build_sparring_arena()
    build_ancient_stone_golem()
    build_industrial_iron_automaton()
    build_clockwork_brass_assistant()
    build_armored_wolf_mount()
    build_horned_pack_beast()
    build_feathered_bird_mount()
    build_witch_flying_broom()
    build_sky_wyvern()
    build_beastfolk_wolf()
    build_beastfolk_fox()
    build_bard_character()
    build_running_kids()
    build_glowing_fairy()
    build_perched_owl()
    build_overhanging_oak()
    build_cliffside_waterfall()
    print("==================================================================")
    print("ALL 26 METROPOLIS ASSET KITS GENERATED AND EXPORTED SUCCESSFULLY!")
    print("==================================================================")

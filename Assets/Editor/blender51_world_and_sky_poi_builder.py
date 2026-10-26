import bpy
import math
import random
import os
from mathutils import Vector

# ==============================================================================
# BLENDER 5.1 MASTER BUILDER: SURFACE & SKY VILLAGES, TEMPLES, FARMS & OUTPOSTS
# ==============================================================================

def build_world_and_sky_points_of_interest():
    col_name = "07_World_And_Sky_Activities_POIs"
    poi_col = bpy.data.collections.get(col_name) or bpy.data.collections.new(col_name)
    if col_name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(poi_col)

    # Purge old POI proxies if re-running
    for obj in list(poi_col.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

    # Painterly Materials aligned with Luminous Astral Fantasy Aesthetic Bible
    mat_ivory_stone = make_poi_mat("MAT_POI_IvoryStone", (0.91, 0.89, 0.82, 1.0), 0.72)
    mat_honey_cedar = make_poi_mat("MAT_POI_HoneyCedar", (0.65, 0.43, 0.30, 1.0), 0.78)
    mat_terracotta  = make_poi_mat("MAT_POI_RoofTerracotta", (0.76, 0.38, 0.24, 1.0), 0.68)
    mat_slate_roof  = make_poi_mat("MAT_POI_PeriwinkleSlate", (0.38, 0.46, 0.62, 1.0), 0.60)
    mat_astral_gold = make_poi_mat("MAT_POI_AstralGold", (0.89, 0.75, 0.47, 1.0), 0.25, metallic=0.92)
    mat_aether_cyan = make_poi_mat("MAT_POI_AetherCyan", (0.22, 0.74, 0.98, 1.0), 0.15, emission=(0.22, 0.74, 0.98, 1.0), em_str=6.0)
    mat_crop_gold   = make_poi_mat("MAT_POI_GoldenWheat", (0.88, 0.74, 0.26, 1.0), 0.85)
    mat_dirt_road   = make_poi_mat("MAT_POI_DirtPathway", (0.58, 0.46, 0.34, 1.0), 0.92)

    # --------------------------------------------------------------------------
    # 1. BUILD 4 SATELLITE BIOME VILLAGES (DOCKCITY, OAKHAVEN, FROST-ANVIL, SUN-SCREE)
    # --------------------------------------------------------------------------
    villages = [
        ("Village_01_Seaward_Dockcity", Vector((1180.0, -980.0, 19.0)), 14, mat_honey_cedar, mat_slate_roof),
        ("Village_02_Oakhaven_Sylvan",  Vector((980.0, 920.0, 34.0)),   12, mat_honey_cedar, mat_terracotta),
        ("Village_03_FrostAnvil_Mine",  Vector((180.0, 1380.0, 95.0)),  12, mat_ivory_stone, mat_slate_roof),
        ("Village_04_SunScree_Oasis",   Vector((-1120.0, -740.0, 42.0)), 14, mat_ivory_stone, mat_terracotta),
    ]
    for v_name, v_origin, house_count, m_wall, m_roof in villages:
        build_satellite_village(v_name, v_origin, house_count, m_wall, m_roof, mat_astral_gold, poi_col)

    # --------------------------------------------------------------------------
    # 2. BUILD 6 ELEMENTAL FULL-INFORMATION TEMPLES (WITH PILLAR ARENAS & GIANT CHESTS)
    # --------------------------------------------------------------------------
    temples = [
        ("Temple_01_SunkenChalice_NE",   Vector((740.0, 420.0, 24.0))),
        ("Temple_02_IronKnuckle_W",      Vector((-920.0, 180.0, 68.0))),
        ("Temple_03_FrostMirror_N",      Vector((-240.0, 1520.0, 145.0))),
        ("Temple_04_AshenCaldera_NW",    Vector((-980.0, 1020.0, 175.0))),
        ("Temple_05_TideCaller_Island",  Vector((1680.0, -1180.0, 26.0))),
        ("Temple_06_ResonanceAbbey_S",   Vector((120.0, -1150.0, 32.0))),
    ]
    for t_name, t_origin in temples:
        build_elemental_temple(t_name, t_origin, mat_ivory_stone, mat_astral_gold, mat_aether_cyan, poi_col)

    # --------------------------------------------------------------------------
    # 3. BUILD 8 COZY FARMHOUSE & WINDMILL HOMESTEADS (COOKING MAMA & VINE SEED HUBS)
    # --------------------------------------------------------------------------
    for f_idx in range(8):
        ang = (f_idx / 8.0) * math.pi * 2.0 + 0.22
        dist = 690.0 if (f_idx % 2 == 0) else 940.0
        fx, fy = math.cos(ang) * dist, math.sin(ang) * dist
        build_farmhouse_homestead(
            f"Farmhouse_Homestead_{f_idx:02d}",
            Vector((fx, fy, 22.0)),
            mat_honey_cedar, mat_terracotta, mat_crop_gold, mat_ivory_stone, poi_col
        )

    # --------------------------------------------------------------------------
    # 4. BUILD 8 WILDERNESS OUTPOSTS (WATCHTOWERS, HAWK ROOSTS & GOLEM MECH BAYS)
    # --------------------------------------------------------------------------
    for o_idx in range(8):
        ang = (o_idx / 8.0) * math.pi * 2.0 + 0.39
        dist = 820.0 if (o_idx % 2 == 0) else 1280.0
        ox, oy = math.cos(ang) * dist, math.sin(ang) * dist
        build_wilderness_outpost(
            f"Outpost_{'GuildWatchtower' if o_idx % 2 == 0 else 'PortalGolemCamp'}_{o_idx:02d}",
            Vector((ox, oy, 24.0)),
            is_golem_camp=(o_idx % 2 == 1),
            mat_stone=mat_ivory_stone, mat_wood=mat_honey_cedar, mat_cyan=mat_aether_cyan, col=poi_col
        )

    # --------------------------------------------------------------------------
    # 5. POPULATE SKY ISLANDS (00..11) WITH SKY-FARMS, WIND-TEMPLES, OUTPOSTS &
    #    UNDERSIDE GRAVITY-FLIP CRYSTAL GEODE CAVERNS + 4 VERTICAL CARGO ELEVATORS
    # --------------------------------------------------------------------------
    rng = random.Random(20261018)
    for i in range(12):
        t = i / 11.0
        angle = t * math.pi * 3.2
        dist = 280.0 + t * 1150.0
        ix, iy, iz = math.cos(angle) * dist, math.sin(angle) * dist, 360.0 + t * 400.0
        isl_rad = rng.uniform(45.0, 115.0)
        isl_depth = isl_rad * 1.15
        sky_pos = Vector((ix, iy, iz))

        if i % 3 == 0:
            build_farmhouse_homestead(
                f"SkyIsland_{i:02d}_CloudCatcherFarm",
                sky_pos, mat_ivory_stone, mat_slate_roof, mat_crop_gold, mat_astral_gold, poi_col
            )
        elif i % 3 == 1:
            build_elemental_temple(
                f"SkyIsland_{i:02d}_CelestialWindTemple",
                sky_pos, mat_ivory_stone, mat_astral_gold, mat_aether_cyan, poi_col
            )
        else:
            build_wilderness_outpost(
                f"SkyIsland_{i:02d}_AerialGolemOutpost",
                sky_pos, is_golem_camp=True,
                mat_stone=mat_ivory_stone, mat_wood=mat_honey_cedar, mat_cyan=mat_aether_cyan, col=poi_col
            )

        build_gravity_flip_underside_cavern(
            f"SkyIsland_{i:02d}_GravityFlipUnderside",
            sky_pos, isl_rad, isl_depth, mat_aether_cyan, mat_astral_gold, poi_col
        )

    # --------------------------------------------------------------------------
    # 6. 4 VERTICAL SPIRE-TO-SKY CARGO ELEVATOR HEIST CHAINS (NE, NW, SE, SW)
    # --------------------------------------------------------------------------
    for e_idx, e_ang in enumerate([0.785, 2.356, 3.927, 5.497]):
        ex, ey = math.cos(e_ang) * 510.0, math.sin(e_ang) * 510.0
        for p_lvl in range(6):
            pz = 95.0 + p_lvl * 125.0
            bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=9.5, depth=2.5, location=(ex, ey, pz))
            plat = bpy.context.active_object
            plat.name = f"SkyElevatorHeist_Line{e_idx}_Platform_{p_lvl}"
            plat.data.materials.append(mat_astral_gold)
            apply_bevel_and_link(plat, poi_col)

    # --------------------------------------------------------------------------
    # 7. CONNECTING DIRT PATHWAY & SUSPENSION BRIDGE ARCS
    # --------------------------------------------------------------------------
    road_targets = [
        Vector((1180.0, -980.0, 19.0)),
        Vector((980.0, 920.0, 34.0)),
        Vector((180.0, 1380.0, 95.0)),
        Vector((-1120.0, -740.0, 42.0)),
    ]
    for r_idx, target in enumerate(road_targets):
        gate_ang = math.atan2(target.y, target.x)
        gate_pos = Vector((math.cos(gate_ang) * 470.0, math.sin(gate_ang) * 470.0, 19.5))
        # Build 6 waypoint segments connecting gate to village
        for seg in range(6):
            frac = (seg + 0.5) / 6.0
            pos = gate_pos.lerp(target, frac)
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=pos)
            rd = bpy.context.active_object
            rd.name = f"DirtPathway_Route{r_idx}_Seg_{seg}"
            rd.scale = (14.0, 14.0, 0.4)
            rd.data.materials.append(mat_dirt_road)
            apply_bevel_and_link(rd, poi_col)

    print("[Blender 5.1 MCP] Built 4 Villages, 6 Surface Temples, 8 Farmhouses, 8 Outposts, 12 Sky-Island Activity Hubs, 4 Elevator Heist Chains & Dirt Pathways.")

    # --------------------------------------------------------------------------
    # 8. EXPORT COMPLETE FBX TO UNITY PROJECT
    # --------------------------------------------------------------------------
    fbx_out = "D:/Games/BattleSummoner/BattleSummoner/Assets/Models/Environments/World_And_Sky_Activities_POIs.fbx"
    os.makedirs(os.path.dirname(fbx_out), exist_ok=True)
    
    # Select only our collection objects for export
    bpy.ops.object.select_all(action='DESELECT')
    for o in poi_col.objects:
        o.select_set(True)
    
    bpy.ops.export_scene.fbx(
        filepath=fbx_out,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        apply_scale_options='FBX_SCALE_ALL'
    )
    print(f"[Blender 5.1 MCP] Exported {len(poi_col.objects)} POI objects to FBX: {fbx_out}")


def build_satellite_village(name, origin, count, mat_wall, mat_roof, mat_gold, col):
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=origin)
    root = bpy.context.active_object
    root.name = f"{name}_Root"
    link_to_col(root, col)

    # Central Village Campfire Rumor Plaza & Fountain/Totem
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=16.0, depth=1.2, location=Vector((0, 0, 0.6)))
    plaza = bpy.context.active_object
    plaza.name = f"{name}_CentralCampfirePlaza"
    plaza.parent = root
    plaza.data.materials.append(mat_gold)
    apply_bevel_and_link(plaza, col)

    # San Francisco-style tight row clustering along two wide mount-ready avenues
    for i in range(count):
        row = i // (count // 2)
        slot = i % (count // 2)
        hx = (slot - (count // 4)) * 11.2
        hy = -22.0 if row == 0 else 22.0
        h_height = 8.5 + (i % 3) * 2.8

        bpy.ops.mesh.primitive_cube_add(size=1.0, location=Vector((hx, hy, h_height * 0.5)))
        house = bpy.context.active_object
        house.name = f"{name}_RowHouse_{i:02d}"
        house.scale = (10.6, 12.0, h_height)
        house.parent = root
        house.data.materials.append(mat_wall)
        apply_bevel_and_link(house, col)

        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=8.2, depth=5.5,
                                        location=Vector((hx, hy, h_height + 2.75)),
                                        rotation=(0, 0, math.pi * 0.25))
        roof = bpy.context.active_object
        roof.name = f"{name}_RowRoof_{i:02d}"
        roof.parent = root
        roof.data.materials.append(mat_roof)
        apply_bevel_and_link(roof, col)


def build_elemental_temple(name, origin, mat_stone, mat_gold, mat_cyan, col):
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=origin)
    root = bpy.context.active_object
    root.name = f"{name}_Root"
    link_to_col(root, col)

    # Stepped Ziggurat Plinth (Open-Air Full-Information 9:16 Viewport Court)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=Vector((0, 0, 3.0)))
    plinth = bpy.context.active_object
    plinth.name = f"{name}_SanctuaryPlinth"
    plinth.scale = (46.0, 54.0, 6.0)
    plinth.parent = root
    plinth.data.materials.append(mat_stone)
    apply_bevel_and_link(plinth, col)

    # 8 Destructible Iron-Knuckle Arena Pillars (Combat + World Merged!)
    for p_idx in range(8):
        px = -15.0 if p_idx < 4 else 15.0
        py = -18.0 + (p_idx % 4) * 12.0
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=1.6, depth=14.0, location=Vector((px, py, 13.0)))
        pillar = bpy.context.active_object
        pillar.name = f"{name}_DestructibleArenaPillar_{p_idx:02d}"
        pillar.parent = root
        pillar.data.materials.append(mat_stone)
        apply_bevel_and_link(pillar, col)

    # Central Giant Master Relic Chest Pedestal (Visible immediately on entry!)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=Vector((0, 14.0, 7.8)))
    giant_chest = bpy.context.active_object
    giant_chest.name = f"{name}_GiantMasterChest_015s"
    giant_chest.scale = (4.2, 2.8, 2.6)
    giant_chest.parent = root
    giant_chest.data.materials.append(mat_gold)
    apply_bevel_and_link(giant_chest, col)

    # Towering Visual-Weenie Obelisk Beacon (Visible from miles away - Elden Ring rule)
    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=4.5, depth=38.0, location=Vector((0, 22.0, 25.0)))
    spire = bpy.context.active_object
    spire.name = f"{name}_LandmarkBeaconSpire"
    spire.parent = root
    spire.data.materials.append(mat_cyan)
    apply_bevel_and_link(spire, col)


def build_farmhouse_homestead(name, origin, mat_wood, mat_roof, mat_wheat, mat_stone, col):
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=origin)
    root = bpy.context.active_object
    root.name = f"{name}_Root"
    link_to_col(root, col)

    # Cozy Timber Farmhouse
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=Vector((-8.0, 0, 4.5)))
    house = bpy.context.active_object
    house.name = f"{name}_CozyBarnHouse"
    house.scale = (14.0, 18.0, 9.0)
    house.parent = root
    house.data.materials.append(mat_wood)
    apply_bevel_and_link(house, col)

    # Tall Windmill Tower & 4 Rotating Sails (Landmark + Weather Wind Indicator)
    bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=4.8, radius2=2.8, depth=22.0, location=Vector((10.0, 8.0, 11.0)))
    mill = bpy.context.active_object
    mill.name = f"{name}_WindmillTower"
    mill.parent = root
    mill.data.materials.append(mat_stone)
    apply_bevel_and_link(mill, col)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=Vector((10.0, 4.5, 17.5)))
    sails = bpy.context.active_object
    sails.name = f"{name}_WindmillSails_Rotor"
    sails.scale = (18.0, 0.4, 2.2)
    sails.parent = root
    sails.data.materials.append(mat_wood)
    apply_bevel_and_link(sails, col)

    # 4 Interactive Crop Fields (For Selective Z-Axis Cleave Harvesting -> Cooking Mama!)
    for c_idx in range(4):
        cx = -12.0 + (c_idx % 2) * 16.0
        cy = -18.0 - (c_idx // 2) * 12.0
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=Vector((cx, cy, 0.8)))
        crop = bpy.context.active_object
        crop.name = f"{name}_HarvestableCropPlot_{c_idx:02d}"
        crop.scale = (12.0, 9.0, 1.6)
        crop.parent = root
        crop.data.materials.append(mat_wheat)
        apply_bevel_and_link(crop, col)


def build_wilderness_outpost(name, origin, is_golem_camp, mat_stone, mat_wood, mat_cyan, col):
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=origin)
    root = bpy.context.active_object
    root.name = f"{name}_Root"
    link_to_col(root, col)

    # Watchtower & Bounty Hawk Roost Perch
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=4.2, depth=24.0, location=Vector((0, 0, 12.0)))
    tower = bpy.context.active_object
    tower.name = f"{name}_WatchtowerAndHawkRoost"
    tower.parent = root
    tower.data.materials.append(mat_stone if not is_golem_camp else mat_wood)
    apply_bevel_and_link(tower, col)

    if is_golem_camp:
        # Parked Stealable Golem Ride-Armor Bay (Mega Man X Hijack Mechanic!)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=Vector((9.5, -6.0, 2.8)))
        golem = bpy.context.active_object
        golem.name = f"{name}_StealableGolemRideArmor_Chassis"
        golem.scale = (3.8, 3.2, 5.6)
        golem.parent = root
        golem.data.materials.append(mat_cyan)
        apply_bevel_and_link(golem, col)


def build_gravity_flip_underside_cavern(name, sky_pos, radius, depth, mat_cyan, mat_gold, col):
    # Inversion Rune Ring on Sky Island Rim + Upside-Down Geode Nodes on the Bottom!
    underside_z = sky_pos.z - (depth * 0.72)
    bpy.ops.mesh.primitive_torus_add(major_radius=4.5, minor_radius=0.45, location=sky_pos + Vector((radius * 0.78, 0, 1.0)))
    ring = bpy.context.active_object
    ring.name = f"{name}_GravityFlipRuneRing"
    ring.data.materials.append(mat_cyan)
    apply_bevel_and_link(ring, col)

    for g_idx in range(5):
        ang = (g_idx / 5.0) * math.pi * 2.0
        gx = sky_pos.x + math.cos(ang) * (radius * 0.35)
        gy = sky_pos.y + math.sin(ang) * (radius * 0.35)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=2.4, location=(gx, gy, underside_z))
        geode = bpy.context.active_object
        geode.name = f"{name}_UpsideDownGeodeNode_{g_idx:02d}"
        geode.data.materials.append(mat_gold)
        apply_bevel_and_link(geode, col)


def apply_bevel_and_link(obj, col):
    bev = obj.modifiers.new(name="PolishBevel", type='BEVEL')
    bev.width = 0.03
    bev.segments = 2
    bev.limit_method = 'ANGLE'
    wn = obj.modifiers.new(name="PolishWeightedNormal", type='WEIGHTED_NORMAL')
    wn.keep_sharp = True
    link_to_col(obj, col)


def make_poi_mat(name, rgba, rough, metallic=0.0, emission=None, em_str=0.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = rough
        bsdf.inputs["Metallic"].default_value = metallic
        if emission and "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = emission
            bsdf.inputs["Emission Strength"].default_value = em_str
    return mat


def link_to_col(obj, col):
    if obj.name not in col.objects:
        col.objects.link(obj)
    if obj.name in bpy.context.scene.collection.objects:
        bpy.context.scene.collection.objects.unlink(obj)

if __name__ == "__main__":
    build_world_and_sky_points_of_interest()

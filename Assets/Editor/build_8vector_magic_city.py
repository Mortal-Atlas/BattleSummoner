import bpy
import bmesh
import math
from mathutils import Vector

# ==============================================================================
# ANTIGRAVITY BLENDER 4.X/5.X BUILDER: 8-VECTOR MAGIC CITY & NORSE GUILD HALL
# Master Specification implementation for 8-Vector Concentric Magic Metropolis
# ==============================================================================

def create_mat(name, base_rgba, roughness, metallic=0.0, emission=None, em_strength=0.0):
    if name in bpy.data.materials:
        mat = bpy.data.materials[name]
    else:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = base_rgba
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        if emission:
            # Blender 4+ and 5+ support 'Emission Color' or 'Emission'
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = emission
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = em_strength
    return mat

def link_to_col(obj, target_col):
    if obj.name not in target_col.objects:
        target_col.objects.link(obj)
    if obj.name in bpy.context.scene.collection.objects:
        bpy.context.scene.collection.objects.unlink(obj)

def build_magic_city_and_landmarks():
    print("=== STARTING 8-VECTOR MAGIC CITY & NORSE GUILD HALL BUILDER ===")

    # 1. Master Collection setup
    col_name = "02_MagicCity_8Vector_Architecture"
    if col_name in bpy.data.collections:
        city_col = bpy.data.collections[col_name]
    else:
        city_col = bpy.data.collections.new(col_name)
        bpy.context.scene.collection.children.link(city_col)

    # Clean previous generated objects in city_col if rerun
    for obj in list(city_col.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

    # Find or create Concentric_Citadel_Root
    citadel_root = bpy.data.objects.get("Concentric_Citadel_Root")
    if not citadel_root:
        bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
        citadel_root = bpy.context.active_object
        citadel_root.name = "Concentric_Citadel_Root"
    citadel_root.location = (0, 0, 0)
    link_to_col(citadel_root, city_col)

    # Remove old 6-viaduct bridges, old procedural boxes, and old small walls
    old_citadel_children = [c for c in citadel_root.children if c.name != "Citadel_Meshy_GrandCathedral"]
    for old_obj in old_citadel_children:
        for grand_child in list(old_obj.children):
            bpy.data.objects.remove(grand_child, do_unlink=True)
        bpy.data.objects.remove(old_obj, do_unlink=True)
    print("Cleaned up old 6-viaduct and procedural box structures.")

    # 2. Materials
    mat_stone     = create_mat("MAT_City_Limestone", (0.72, 0.70, 0.67, 1.0), 0.85)
    mat_timber    = create_mat("MAT_Norse_DarkPine", (0.22, 0.14, 0.09, 1.0), 0.78)
    mat_copper    = create_mat("MAT_Utility_Copper", (0.78, 0.42, 0.24, 1.0), 0.35, metallic=0.9)
    mat_mana      = create_mat("MAT_Arcane_CyanConduit", (0.0, 0.88, 1.0, 1.0), 0.2, emission=(0.0, 0.92, 1.0, 1.0), em_strength=6.5)
    mat_brass     = create_mat("MAT_Spigot_Brass", (0.85, 0.68, 0.22, 1.0), 0.25, metallic=0.95)
    mat_royalblue = create_mat("MAT_City_RoyalBlueRoof", (0.10, 0.28, 0.78, 1.0), 0.45) # Nice royal blue roof!

    # 3. Build 4 Concentric Stepped Elevation Tiers (Z = +20m to +85m)
    # Tier 1 (R: 310m-450m, top Z = +35m)
    # Tier 2 (R: 195m-310m, top Z = +52m)
    # Tier 3 (R: 95m-195m,  top Z = +70m)
    # Tier 4 (R: 0m-95m,    top Z = +85m)
    tier_specs = [
        ("Tier1_OuterBastion_BazaarRing",        450.0, 27.5, 15.0),
        ("Tier2_Artisans_Bakery_GuildRing",      310.0, 43.5, 17.0),
        ("Tier3_Mages_SanctumTerrace",           195.0, 61.0, 18.0),
        ("Tier4_InnerCitadel_CathedralAcropolis", 95.0, 77.5, 15.0)
    ]
    for t_name, rad, z_center, height in tier_specs:
        bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=rad, depth=height, location=(0, 0, z_center))
        t_obj = bpy.context.active_object
        t_obj.name = f"City_{t_name}"
        t_obj.parent = citadel_root
        t_obj.data.materials.append(mat_stone)
        link_to_col(t_obj, city_col)

    # 4. Giant Outer Fortified Wall (R = 450m) & 16 Mana-Shield Generator Turrets
    bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=452.0, depth=18.0, location=(0, 0, 44.0))
    wall_mesh = bpy.context.active_object
    wall_mesh.name = "City_GiantOuterFortifiedWall"
    wall_mesh.parent = citadel_root
    wall_mesh.data.materials.append(mat_stone)
    link_to_col(wall_mesh, city_col)

    for i in range(16):
        ang = (i / 16.0) * math.pi * 2.0
        tx, ty = math.cos(ang) * 450.0, math.sin(ang) * 450.0
        
        # Turret cylinder: radius 9.5m, height 38m (Z=20 to Z=58)
        bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=9.5, depth=38.0, location=(tx, ty, 39.0))
        turret = bpy.context.active_object
        turret.name = f"Wall_ShieldTurret_{i:02d}"
        turret.parent = citadel_root
        turret.data.materials.append(mat_stone)
        link_to_col(turret, city_col)

        # Mana-Shield Crystalline Generator on Turret Roof (Z = 61m)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=3.8, location=(tx, ty, 61.0))
        shield_gen = bpy.context.active_object
        shield_gen.name = f"ManaShield_Generator_{i:02d}"
        shield_gen.parent = citadel_root
        shield_gen.data.materials.append(mat_mana)
        link_to_col(shield_gen, city_col)

    # 5. 8 Cardinal & Intercardinal Bridges, Gatehouses & Anti-Gravity Radial Lanes
    # Directions: N=0, NE=45, E=90, SE=135, S=180, SW=225, W=270, NW=315
    directions = [
        ("N_0",    0.0 * math.pi, True),
        ("NE_45",  0.25 * math.pi, False),
        ("E_90",   0.5 * math.pi, True),
        ("SE_135", 0.75 * math.pi, False),
        ("S_180",  1.0 * math.pi, True),
        ("SW_225", 1.25 * math.pi, False),
        ("W_270",  1.5 * math.pi, True),
        ("NW_315", 1.75 * math.pi, False)
    ]

    for dir_label, ang, is_cardinal in directions:
        # Moat Bridge (Spanning R=450m to 545m, Width=18m, Depth=8m, Z=22m)
        bx, by = math.cos(ang) * 497.5, math.sin(ang) * 497.5
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, by, 22.0), rotation=(0, 0, ang))
        bridge = bpy.context.active_object
        bridge.name = f"Bridge_8Vector_{dir_label}"
        bridge.scale = (95.0, 18.0, 8.0) # Length 95m, Width 18m, Height 8m
        bridge.parent = citadel_root
        bridge.data.materials.append(mat_stone)
        link_to_col(bridge, city_col)

        # Gatehouse (Cardinal Twin-Tower Portal vs Intercardinal Bastion Energy Dome)
        gx, gy = math.cos(ang) * 448.0, math.sin(ang) * 448.0
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(gx, gy, 38.0), rotation=(0, 0, ang))
        gate = bpy.context.active_object
        gate.name = f"Gatehouse_{'CardinalPortal' if is_cardinal else 'BastionEnergy'}_{dir_label}"
        gate.scale = (22.0, 38.0 if is_cardinal else 24.0, 36.0)
        gate.parent = citadel_root
        gate.data.materials.append(mat_stone)
        link_to_col(gate, city_col)

        # Portal barrier / energy core
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=24, radius=6.5, depth=18.0,
            location=(gx, gy, 34.0),
            rotation=(0, math.pi * 0.5, ang)
        )
        portal_core = bpy.context.active_object
        portal_core.name = f"Gate_ArcaneCore_{dir_label}"
        portal_core.parent = citadel_root
        portal_core.data.materials.append(mat_mana)
        link_to_col(portal_core, city_col)

        # Radial Anti-Gravity Conduit Lane (R=65m to 445m, Width=3.5m, stepped incline Z=20m to 85m)
        cx, cy = math.cos(ang) * 255.0, math.sin(ang) * 255.0
        slope_pitch = math.atan2(85.0 - 20.0, 380.0)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx, cy, 54.0), rotation=(0, -slope_pitch, ang))
        ag_lane = bpy.context.active_object
        ag_lane.name = f"AntiGravity_RadialLane_{dir_label}"
        ag_lane.scale = (380.0, 3.5, 0.6)
        ag_lane.parent = citadel_root
        ag_lane.data.materials.append(mat_mana)
        link_to_col(ag_lane, city_col)

    # 6. Circular Anti-Gravity Hover Pads (Diameter = 18m -> Radius 9m)
    hover_coords = [
        ("HoverPad_NW", (-295,  245, 36.0)),
        ("HoverPad_NE", ( 245,  295, 36.0)),
        ("HoverPad_E",  ( 355,   85, 36.0)),
        ("HoverPad_SE", ( 285, -245, 36.0)),
        ("HoverPad_SW", (-285, -245, 36.0))
    ]
    for hp_name, h_loc in hover_coords:
        bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=9.0, depth=1.2, location=h_loc)
        h_pad = bpy.context.active_object
        h_pad.name = hp_name
        h_pad.parent = citadel_root
        h_pad.data.materials.append(mat_mana)
        link_to_col(h_pad, city_col)

    # 7. Tier 4 Central Cathedral & 4 Crystalline Spire Pylons
    # Seat Meshy Cathedral on Tier 4 at Z = +85m if present
    cathedral_mesh = bpy.data.objects.get("Citadel_Meshy_GrandCathedral")
    if cathedral_mesh:
        cathedral_mesh.location = (0, 0, 85.0)
        cathedral_mesh.parent = citadel_root
        print("Seated Citadel_Meshy_GrandCathedral at Z = +85.0m on Tier 4 Acropolis!")
    else:
        # Fallback landmark cathedral nave
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 115.0))
        cathedral_nave = bpy.context.active_object
        cathedral_nave.name = "Landmark_01_CentralCathedral_Nave"
        cathedral_nave.scale = (68.0, 96.0, 60.0)
        cathedral_nave.parent = citadel_root
        cathedral_nave.data.materials.append(mat_stone)
        link_to_col(cathedral_nave, city_col)

        bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=18.0, depth=110.0, location=(0, -28.0, 185.0))
        cathedral_spire = bpy.context.active_object
        cathedral_spire.name = "Landmark_01_CentralCathedral_HighSpire"
        cathedral_spire.parent = citadel_root
        cathedral_spire.data.materials.append(mat_stone)
        link_to_col(cathedral_spire, city_col)

    # 4 Crystalline Arcane Obelisk Pylons at R = 68m (45, 135, 225, 315 deg)
    pylons = [
        ("NW_GreatSpirePylon", -48.0,  48.0, 52.0),
        ("NE_CitadelPylon",     48.0,  48.0, 28.0),
        ("SE_CitadelPylon",     48.0, -48.0, 28.0),
        ("SW_CitadelPylon",    -48.0, -48.0, 28.0)
    ]
    for p_label, px, py, p_height in pylons:
        bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=5.5, depth=p_height, location=(px, py, 85.0 + p_height * 0.5))
        pylon = bpy.context.active_object
        pylon.name = f"Tier4_ArcaneObelisk_{p_label}"
        pylon.parent = citadel_root
        pylon.data.materials.append(mat_mana)
        link_to_col(pylon, city_col)

    # 8. Build The Grand Norse-Swedish Guild Hall (With High Banisters & 3x 10ft Barrels)
    build_norse_swedish_guild_hall(
        origin=Vector((-65.0, 220.0, 52.0)),
        city_col=city_col,
        citadel_root=citadel_root,
        mat_timber=mat_timber,
        mat_stone=mat_stone,
        mat_copper=mat_copper,
        mat_brass=mat_brass,
        mat_royalblue=mat_royalblue
    )

    print("=== 8-VECTOR MAGIC CITY BUILD COMPLETED SUCCESSFULLY! ===")


def build_norse_swedish_guild_hall(origin, city_col, citadel_root, mat_timber, mat_stone, mat_copper, mat_brass, mat_royalblue):
    """
    Constructs the Norse-Swedish Stave Guild Hall at (-65, 220, +52m) featuring:
    - High-ceiling stave timber frame & wooden banisters holding up the roof trusses
    - 3 horizontal 10-foot (3.048m diameter) barrels of Mead, Wine, and Beer in the back
      with taps spouting directly from the circular disc side (face) of each barrel.
    """
    # A. Stone Foundation Plinth (28m wide x 48m long x 2m high)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=origin + Vector((0, 0, 1.0)))
    plinth = bpy.context.active_object
    plinth.name = "GuildHall_StonePlinth"
    plinth.scale = (28.0, 48.0, 2.0)
    plinth.parent = citadel_root
    plinth.data.materials.append(mat_stone)
    link_to_col(plinth, city_col)

    # Back Heart-Pine Timber Wall (behind the 3 barrels)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=origin + Vector((0, 21.0, 10.0)))
    back_wall = bpy.context.active_object
    back_wall.name = "GuildHall_TimberBackWall"
    back_wall.scale = (27.0, 0.6, 18.0)
    back_wall.parent = citadel_root
    back_wall.data.materials.append(mat_timber)
    link_to_col(back_wall, city_col)

    # Side Heart-Pine Timber Walls
    for s_wall, sw_name in [(-13.0, "Wall_Left"), (13.0, "Wall_Right")]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=origin + Vector((s_wall, 0, 9.0)))
        side_wall = bpy.context.active_object
        side_wall.name = f"GuildHall_Timber{sw_name}"
        side_wall.scale = (0.6, 44.0, 16.0)
        side_wall.parent = citadel_root
        side_wall.data.materials.append(mat_timber)
        link_to_col(side_wall, city_col)

    # B. Interior Wooden Stave Columns & Upper Mezzanine Banisters Holding Up the Roof
    for side in [-1.0, 1.0]:
        # Longitudinal Wooden Banister Beams (Lower Gallery & Upper Roof-Support Banisters)
        for z_banister, b_label in [(5.5, "LowerGalleryBanister"), (11.5, "UpperRoofSupportBanister")]:
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=origin + Vector((side * 10.5, 0, z_banister)))
            banister = bpy.context.active_object
            banister.name = f"GuildHall_{b_label}_{'L' if side < 0 else 'R'}"
            banister.scale = (0.45, 42.0, 1.25)
            banister.parent = citadel_root
            banister.data.materials.append(mat_timber)
            link_to_col(banister, city_col)

        # Vertical Heart-Pine Stave Pillars supporting the Roof Trusses (8 per side)
        for p_idx in range(8):
            py = -19.5 + p_idx * 5.5
            bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.55, depth=18.0, location=origin + Vector((side * 10.5, py, 11.0)))
            pillar = bpy.context.active_object
            pillar.name = f"GuildHall_StavePillar_{'L' if side < 0 else 'R'}_{p_idx}"
            pillar.parent = citadel_root
            pillar.data.materials.append(mat_timber)
            link_to_col(pillar, city_col)

    # C. Steep Pitched Norse-Swedish Stave Roof (22m Ridge Height) with Royal Blue Roof Shingles
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=24.0, depth=14.0, location=origin + Vector((0, 0, 22.0)), rotation=(0, 0, math.pi*0.25))
    roof = bpy.context.active_object
    roof.name = "GuildHall_SteepNorseTimberRoof"
    roof.scale = (0.85, 1.45, 1.0)
    roof.parent = citadel_root
    roof.data.materials.append(mat_royalblue)
    link_to_col(roof, city_col)

    # D. THE THREE 10-FOOT (3.048m Diameter) BARRELS OF MEAD, WINE & BEER IN THE BACK
    # Positioned along the back wall (Local Y = +17.5m), lying horizontally on their sides,
    # with taps spouting from the front circular DISC SIDE (facing -Y toward the hall nave).
    barrel_radius = 1.524  # Exact 10-foot (3.048m) diameter -> 1.524m radius
    barrel_length = 4.20   # Horizontal length
    casks = [
        ("Barrel_01_GoldenHoneyMead",  -4.6),
        ("Barrel_02_SpicedCrimsonWine", 0.0),
        ("Barrel_03_DarkFrostholdBeer",  4.6)
    ]

    for c_name, x_offset in casks:
        c_loc = origin + Vector((x_offset, 17.5, 4.2))

        # Horizontal 10-Foot Oak Barrel (Rotated 90 deg on X so circular disc heads face Y / -Y)
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=32,
            radius=barrel_radius,
            depth=barrel_length,
            location=c_loc,
            rotation=(math.pi * 0.5, 0.0, 0.0)
        )
        barrel = bpy.context.active_object
        barrel.name = f"GuildHall_10Ft_{c_name}"
        barrel.parent = citadel_root
        barrel.data.materials.append(mat_timber)
        link_to_col(barrel, city_col)

        # Tap / Spigot Assembly Spouting from Front Circular Disc Side (Local Y = 17.5 - 2.1 = 15.4m)
        disc_face_y = 17.5 - (barrel_length * 0.5)
        tap_loc = origin + Vector((x_offset, disc_face_y - 0.35, 3.65))

        # Horizontal Spigot Pipe jutting out of the circular wooden disc face
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=16,
            radius=0.14,
            depth=0.75,
            location=tap_loc,
            rotation=(math.pi * 0.5, 0.0, 0.0)
        )
        tap_pipe = bpy.context.active_object
        tap_pipe.name = f"{c_name}_DiscSide_TapPipe"
        tap_pipe.parent = citadel_root
        tap_pipe.data.materials.append(mat_brass)
        link_to_col(tap_pipe, city_col)

        # Downward Pouring Nozzle Spout at tip of the Tap
        spout_loc = origin + Vector((x_offset, disc_face_y - 0.65, 3.42))
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=16,
            radius=0.10,
            depth=0.45,
            location=spout_loc,
            rotation=(0.0, 0.0, 0.0)
        )
        tap_spout = bpy.context.active_object
        tap_spout.name = f"{c_name}_DiscSide_PourNozzle"
        tap_spout.parent = citadel_root
        tap_spout.data.materials.append(mat_copper)
        link_to_col(tap_spout, city_col)

if __name__ == "__main__":
    build_magic_city_and_landmarks()

    # Save blend file
    blend_path = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\CircularContinent_MacroLevel1.blend"
    print(f"Saving updated .blend to {blend_path}...")
    bpy.ops.wm.save_mainfile()

    # Export FBX
    fbx_path = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\CircularContinent_MacroLevel1.fbx"
    print(f"Exporting updated .fbx to {fbx_path}...")
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=False,
        use_active_collection=False,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True,
        object_types={'MESH', 'EMPTY'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        axis_forward='-Z',
        axis_up='Y'
    )
    print("ALL BLENDER GENERATION & EXPORT COMPLETE!")

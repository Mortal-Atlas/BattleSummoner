import bpy
import bmesh
import math
import random
import os

OUT_DIR = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Prefabs\Environment"
os.makedirs(OUT_DIR, exist_ok=True)

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def get_or_create_mat(name, color, roughness=0.7, metallic=0.1, emissive=None):
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
                bsdf.inputs['Emission Strength'].default_value = 2.5
    return mat

# ==========================================
# 1. FANTASY STONE BRIDGE
# ==========================================
def build_stone_bridge():
    clear_scene()
    mat_stone = get_or_create_mat("M_BridgeStone", (0.52, 0.50, 0.47, 1.0), roughness=0.85)
    mat_deck = get_or_create_mat("M_BridgeDeckCobble", (0.42, 0.40, 0.38, 1.0), roughness=0.8)
    mat_brass = get_or_create_mat("M_BridgeBrass", (0.85, 0.72, 0.28, 1.0), roughness=0.35, metallic=0.85)
    mat_glow = get_or_create_mat("M_BridgeLanternGlow", (1.0, 0.9, 0.5, 1.0), emissive=(1.0, 0.85, 0.45, 1.0))

    # Roadway Deck
    mesh = bpy.data.meshes.new("BridgeStructure_Mesh")
    obj = bpy.data.objects.new("FantasyStoneBridge", mesh)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()

    length = 24.0
    width = 6.4
    seg_x = 24
    seg_z = 6

    # Create arched roadway grid
    verts = []
    for ix in range(seg_x + 1):
        u = ix / float(seg_x)
        x = -length * 0.5 + u * length
        arch_y = 1.0 + 1.2 * math.cos((x / (length * 0.5)) * (math.pi * 0.5))
        row = []
        for iz in range(seg_z + 1):
            w = iz / float(seg_z)
            z = -width * 0.5 + w * width
            row.append(bm.verts.new((x, arch_y, z)))
        verts.append(row)

    bm.verts.ensure_lookup_table()
    for ix in range(seg_x):
        for iz in range(seg_z):
            bm.faces.new([verts[ix][iz], verts[ix+1][iz], verts[ix+1][iz+1], verts[ix][iz+1]])

    # Under-bridge abutments & piers
    pier_w = 4.0
    pier_h = 3.0
    # Center pier
    bmesh.ops.create_cube(bm, size=1.0)
    bm.to_mesh(mesh)
    bm.free()
    obj.data.materials.append(mat_deck)

    # Parapets (North & South)
    for side in [-1, 1]:
        z_pos = side * (width * 0.5 - 0.2)
        p_mesh = bpy.data.meshes.new(f"Parapet_{side}_Mesh")
        p_obj = bpy.data.objects.new(f"Parapet_{side}", p_mesh)
        p_obj.parent = obj
        bpy.context.collection.objects.link(p_obj)
        p_bm = bmesh.new()
        p_verts = []
        for ix in range(seg_x + 1):
            x = -length * 0.5 + (ix / float(seg_x)) * length
            arch_y = 1.0 + 1.2 * math.cos((x / (length * 0.5)) * (math.pi * 0.5))
            v0 = p_bm.verts.new((x, arch_y, z_pos - 0.2))
            v1 = p_bm.verts.new((x, arch_y, z_pos + 0.2))
            v2 = p_bm.verts.new((x, arch_y + 1.1, z_pos + 0.2))
            v3 = p_bm.verts.new((x, arch_y + 1.1, z_pos - 0.2))
            p_verts.append((v0, v1, v2, v3))
        p_bm.verts.ensure_lookup_table()
        for ix in range(seg_x):
            vA = p_verts[ix]
            vB = p_verts[ix+1]
            p_bm.faces.new([vA[0], vB[0], vB[3], vA[3]])
            p_bm.faces.new([vA[1], vA[2], vB[2], vB[1]])
            p_bm.faces.new([vA[3], vB[3], vB[2], vA[2]])
        p_bm.to_mesh(p_mesh)
        p_bm.free()
        p_obj.data.materials.append(mat_stone)

    # Posts & Lanterns
    posts = [(-length*0.5, -width*0.5), (-length*0.5, width*0.5), (length*0.5, -width*0.5), (length*0.5, width*0.5), (0.0, -width*0.5), (0.0, width*0.5)]
    for idx, (px, pz) in enumerate(posts):
        arch_y = 1.0 + 1.2 * math.cos((px / (length * 0.5)) * (math.pi * 0.5))
        bpy.ops.mesh.primitive_cube_add(size=0.8, location=(px, arch_y + 0.8, pz))
        post = bpy.context.active_object
        post.name = f"Post_{idx}"
        post.scale = (0.7, 1.8, 0.7)
        post.parent = obj
        post.data.materials.append(mat_stone)

        # Center posts get lanterns
        if abs(px) < 0.1:
            bpy.ops.mesh.primitive_cylinder_add(radius=0.25, depth=0.6, location=(px, arch_y + 2.4, pz))
            lantern = bpy.context.active_object
            lantern.name = f"Lantern_{idx}"
            lantern.parent = post
            lantern.data.materials.append(mat_glow)

    out_fbx = os.path.join(OUT_DIR, "FantasyStoneBridge.fbx")
    bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=False, apply_unit_scale=True, global_scale=1.0)
    print("Bridge exported to:", out_fbx)

# ==========================================
# 2. FANTASY GOTHIC ARCHWAY
# ==========================================
def build_gothic_archway():
    clear_scene()
    mat_stone = get_or_create_mat("M_GothicStone", (0.45, 0.43, 0.42, 1.0), roughness=0.85)
    mat_trim = get_or_create_mat("M_GothicSlateTrim", (0.22, 0.24, 0.28, 1.0), roughness=0.6)
    mat_gold = get_or_create_mat("M_GothicGold", (0.85, 0.72, 0.28, 1.0), roughness=0.35, metallic=0.9)

    root = bpy.data.objects.new("FantasyGothicArchway", None)
    bpy.context.collection.objects.link(root)

    # Left & Right Towers
    for side in [-1, 1]:
        tx = side * 3.4
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(tx, 4.5, 0))
        tower = bpy.context.active_object
        tower.name = f"Arch_Tower_{side}"
        tower.scale = (1.8, 9.0, 2.2)
        tower.parent = root
        tower.data.materials.append(mat_stone)

        # Spire Roof
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=1.4, depth=2.8, location=(tx, 10.4, 0))
        spire = bpy.context.active_object
        spire.name = f"Arch_Spire_{side}"
        spire.rotation_euler = (0, 0, math.pi * 0.25)
        spire.parent = tower
        spire.data.materials.append(mat_trim)

    # Cross Arch Span
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 7.8, 0))
    cross = bpy.context.active_object
    cross.name = "Arch_CrossBeam"
    cross.scale = (7.0, 1.4, 2.0)
    cross.parent = root
    cross.data.materials.append(mat_stone)

    # Pointed Crest Finial
    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=0.8, depth=2.2, location=(0, 9.6, 0))
    finial = bpy.context.active_object
    finial.name = "Arch_GothicCrest"
    finial.parent = root
    finial.data.materials.append(mat_gold)

    out_fbx = os.path.join(OUT_DIR, "FantasyGothicArchway.fbx")
    bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=False, apply_unit_scale=True, global_scale=1.0)
    print("Archway exported to:", out_fbx)

# ==========================================
# 3. FANTASY FLORAL PLANTER
# ==========================================
def build_floral_planter():
    clear_scene()
    mat_stone = get_or_create_mat("M_PlanterStone", (0.55, 0.53, 0.50, 1.0), roughness=0.8)
    mat_soil = get_or_create_mat("M_PlanterSoil", (0.20, 0.15, 0.11, 1.0), roughness=0.95)
    mat_ivy = get_or_create_mat("M_PlanterIvy", (0.16, 0.48, 0.18, 1.0), roughness=0.6)
    mat_blossom = get_or_create_mat("M_PlanterBlossom", (0.92, 0.35, 0.58, 1.0), roughness=0.5)

    # Stone Urn Base
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.65, depth=0.3, location=(0, 0.15, 0))
    base = bpy.context.active_object
    base.name = "FantasyFloralPlanter"
    base.data.materials.append(mat_stone)

    # Fluted Urn Body
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.75, depth=0.9, location=(0, 0.75, 0))
    body = bpy.context.active_object
    body.name = "Urn_Body"
    body.parent = base
    body.data.materials.append(mat_stone)

    # Soil Surface
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.68, depth=0.1, location=(0, 1.15, 0))
    soil = bpy.context.active_object
    soil.name = "Urn_Soil"
    soil.parent = base
    soil.data.materials.append(mat_soil)

    # Trailing Ivy and Blooming Blossoms
    random.seed(42)
    for i in range(14):
        angle = (i / 14.0) * math.pi * 2.0
        rad = 0.55 + random.random() * 0.18
        ix = math.cos(angle) * rad
        iz = math.sin(angle) * rad
        # Foliage cluster
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.18, location=(ix, 1.25 + random.random()*0.15, iz))
        fol = bpy.context.active_object
        fol.parent = base
        fol.data.materials.append(mat_ivy)

        # Flower blossom
        if i % 2 == 0:
            bpy.ops.mesh.primitive_uv_sphere_add(segments=6, ring_count=5, radius=0.09, location=(ix*1.15, 1.22 + random.random()*0.1, iz*1.15))
            fl = bpy.context.active_object
            fl.parent = base
            fl.data.materials.append(mat_blossom)

    out_fbx = os.path.join(OUT_DIR, "FantasyFloralPlanter.fbx")
    bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=False, apply_unit_scale=True, global_scale=1.0)
    print("Planter exported to:", out_fbx)

# ==========================================
# 4. FANTASY LUSH GRASS CLUMP (FOLIAGE TUFT)
# ==========================================
def build_grass_clump():
    clear_scene()
    mat_grass = get_or_create_mat("M_GrassBlade", (0.22, 0.58, 0.20, 1.0), roughness=0.6)
    mat_flower_blue = get_or_create_mat("M_WildflowerBlue", (0.35, 0.65, 0.98, 1.0), roughness=0.4)
    mat_flower_gold = get_or_create_mat("M_WildflowerGold", (0.98, 0.85, 0.22, 1.0), roughness=0.4)

    mesh = bpy.data.meshes.new("FantasyLushGrassClump_Mesh")
    obj = bpy.data.objects.new("FantasyLushGrassClump", mesh)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()

    random.seed(1337)
    num_blades = 28

    # Dimensions strictly: diameter 1.1m, max height 0.75m
    for i in range(num_blades):
        angle = random.random() * math.pi * 2.0
        dist = 0.08 + random.random() * 0.35
        bx = math.cos(angle) * dist
        bz = math.sin(angle) * dist
        h = 0.45 + random.random() * 0.30
        curve_dir = angle + (random.random() - 0.5) * 0.6
        lean = 0.15 + random.random() * 0.22

        bw = 0.05
        # 3 segment curved blade
        v0 = bm.verts.new((bx - math.sin(angle)*bw, 0.0, bz + math.cos(angle)*bw))
        v1 = bm.verts.new((bx + math.sin(angle)*bw, 0.0, bz - math.cos(angle)*bw))

        mid_x = bx + math.cos(curve_dir) * lean * 0.4
        mid_z = bz + math.sin(curve_dir) * lean * 0.4
        v2 = bm.verts.new((mid_x + math.sin(angle)*bw*0.7, h*0.5, mid_z - math.cos(angle)*bw*0.7))
        v3 = bm.verts.new((mid_x - math.sin(angle)*bw*0.7, h*0.5, mid_z + math.cos(angle)*bw*0.7))

        tip_x = bx + math.cos(curve_dir) * lean
        tip_z = bz + math.sin(curve_dir) * lean
        v4 = bm.verts.new((tip_x, h, tip_z))

        bm.faces.new([v0, v1, v2, v3])
        bm.faces.new([v3, v2, v4])

    bm.to_mesh(mesh)
    bm.free()
    obj.data.materials.append(mat_grass)

    # 6 Blooming Wildflowers inside clump
    for f in range(6):
        fa = (f / 6.0) * math.pi * 2.0 + 0.2
        fx = math.cos(fa) * 0.32
        fz = math.sin(fa) * 0.32
        fh = 0.35 + random.random() * 0.2
        f_mat = mat_flower_blue if (f % 2 == 0) else mat_flower_gold
        bpy.ops.mesh.primitive_uv_sphere_add(segments=6, ring_count=5, radius=0.055, location=(fx, fh, fz))
        flower = bpy.context.active_object
        flower.name = f"Wildflower_{f}"
        flower.parent = obj
        flower.data.materials.append(f_mat)

    out_fbx = os.path.join(OUT_DIR, "FantasyLushGrassClump.fbx")
    bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=False, apply_unit_scale=True, global_scale=1.0)
    print("Grass clump exported to:", out_fbx)

if __name__ == "__main__":
    print("=== Generating Isolated Clean Blender Fantasy Assets ===")
    build_stone_bridge()
    build_gothic_archway()
    build_floral_planter()
    build_grass_clump()
    print("=== All Assets Successfully Generated and Exported! ===")

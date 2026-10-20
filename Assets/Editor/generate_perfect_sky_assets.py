import bpy
import bmesh
import math
import os
import random
from mathutils import Vector, noise

OUTPUT_DIR = os.path.abspath("Assets/Models/Environments/Sky")
os.makedirs(OUTPUT_DIR, exist_ok=True)

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def create_material(name, diffuse_color, roughness=0.8):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = diffuse_color
            bsdf.inputs["Roughness"].default_value = roughness
    return mat

def generate_cloud(name, max_radius, lobes_config, seed=42):
    clear_scene()
    random.seed(seed)
    
    mat_cloud = create_material("M_Stylized_Cloud", (0.96, 0.98, 1.0, 1.0), roughness=0.35)
    
    spheres = []
    for (ox, oy, oz, r) in lobes_config:
        bpy.ops.mesh.primitive_uv_sphere_add(
            segments=28, ring_count=20, radius=r,
            location=(ox, oy, oz)
        )
        s = bpy.context.active_object
        for v in s.data.vertices:
            local_y = v.co.y
            if local_y < -r * 0.2:
                v.co.y = -r * 0.2 + (local_y + r * 0.2) * 0.35
        spheres.append(s)
    
    bpy.ops.object.select_all(action='DESELECT')
    for s in spheres:
        s.select_set(True)
    bpy.context.view_layer.objects.active = spheres[0]
    bpy.ops.object.join()
    cloud_obj = bpy.context.active_object
    cloud_obj.name = name
    
    remesh_voxel_size = max(0.45, max_radius * 0.038)
    remesh_mod = cloud_obj.modifiers.new("CloudRemesh", "REMESH")
    remesh_mod.mode = 'VOXEL'
    remesh_mod.voxel_size = remesh_voxel_size
    remesh_mod.adaptivity = 0.02
    bpy.ops.object.modifier_apply(modifier="CloudRemesh")
    
    smooth_mod = cloud_obj.modifiers.new("CloudSmooth", "SMOOTH")
    smooth_mod.factor = 0.85
    smooth_mod.iterations = 5
    bpy.ops.object.modifier_apply(modifier="CloudSmooth")
    
    bpy.ops.object.shade_smooth()
    
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    
    cloud_obj.data.materials.append(mat_cloud)
    
    fbx_path = os.path.join(OUTPUT_DIR, f"{name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        axis_forward='Z',
        axis_up='Y',
        apply_scale_options='FBX_SCALE_ALL'
    )
    print(f"Exported {name} to {fbx_path}")
    return fbx_path

def generate_floating_island(name, top_radius, depth, seed=101):
    clear_scene()
    random.seed(seed)
    
    mat_grass = create_material("M_Island_Grass", (0.28, 0.65, 0.22, 1.0), roughness=0.7)
    mat_rock = create_material("M_Island_Rock", (0.35, 0.32, 0.28, 1.0), roughness=0.85)
    
    bm = bmesh.new()
    
    num_sectors = 36
    rings = 5
    
    # Center vertex at (0, 0, 0)
    v_center = bm.verts.new(Vector((0, 0, 0)))
    
    prev_ring = [v_center]
    for ring_idx in range(1, rings + 1):
        frac = ring_idx / float(rings)
        r = top_radius * frac
        current_ring = []
        for sec in range(num_sectors):
            angle = (sec / float(num_sectors)) * math.pi * 2.0
            wobble = 1.0
            if ring_idx == rings:
                wobble += (noise.noise(Vector((math.cos(angle)*2.5, math.sin(angle)*2.5, seed))) * 0.12)
            x = math.cos(angle) * r * wobble
            z = math.sin(angle) * r * wobble
            current_ring.append(bm.verts.new(Vector((x, 0.0, z))))
        
        # Connect faces with normal pointing UP (+Y)
        if ring_idx == 1:
            for sec in range(num_sectors):
                v1 = current_ring[sec]
                v2 = current_ring[(sec + 1) % num_sectors]
                bm.faces.new([v_center, v2, v1])
        else:
            for sec in range(num_sectors):
                v1 = prev_ring[sec]
                v2 = current_ring[sec]
                v3 = current_ring[(sec + 1) % num_sectors]
                v4 = prev_ring[(sec + 1) % num_sectors]
                bm.faces.new([v1, v2, v3, v4])
        
        prev_ring = current_ring
    
    rim_verts = prev_ring
    
    # Underside crags tapering down to apex
    underside_tiers = 7
    last_tier_verts = rim_verts
    
    for tier in range(1, underside_tiers + 1):
        t_frac = tier / float(underside_tiers)
        radius_factor = math.pow(1.0 - t_frac, 0.85)
        tier_y = -depth * math.pow(t_frac, 1.15)
        
        if tier == underside_tiers:
            tip_x = (random.random() - 0.5) * top_radius * 0.1
            tip_z = (random.random() - 0.5) * top_radius * 0.1
            v_tip = bm.verts.new(Vector((tip_x, -depth, tip_z)))
            for sec in range(num_sectors):
                v1 = last_tier_verts[sec]
                v2 = last_tier_verts[(sec + 1) % num_sectors]
                bm.faces.new([v1, v2, v_tip])
        else:
            current_tier_verts = []
            for sec in range(num_sectors):
                angle = (sec / float(num_sectors)) * math.pi * 2.0
                n_val = noise.noise(Vector((math.cos(angle)*3.5, math.sin(angle)*3.5, tier*1.2 + seed)))
                rock_wobble = 1.0 + (n_val * 0.28)
                
                spike_n = noise.noise(Vector((math.cos(angle)*7.0, math.sin(angle)*7.0, tier*2.5)))
                if spike_n > 0.2:
                    rock_wobble += spike_n * 0.25
                
                x = math.cos(angle) * (top_radius * radius_factor) * rock_wobble
                z = math.sin(angle) * (top_radius * radius_factor) * rock_wobble
                y = tier_y + (n_val * depth * 0.08)
                current_tier_verts.append(bm.verts.new(Vector((x, y, z))))
            
            for sec in range(num_sectors):
                v1 = last_tier_verts[sec]
                v2 = last_tier_verts[(sec + 1) % num_sectors]
                v3 = current_tier_verts[(sec + 1) % num_sectors]
                v4 = current_tier_verts[sec]
                bm.faces.new([v1, v2, v3, v4])
            
            last_tier_verts = current_tier_verts

    # Automatically recalculate all face normals pointing outwards from volume
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    
    obj.data.materials.append(mat_grass) # Slot 0: Top flat plateau
    obj.data.materials.append(mat_rock)  # Slot 1: Sides & rocky underside
    
    # Assign material slots: Faces pointing upwards near Y = 0 get slot 0
    for f in obj.data.polygons:
        if f.normal.y > 0.35 and f.center.y > -0.2:
            f.material_index = 0
        else:
            f.material_index = 1
            
    bev = obj.modifiers.new("RockBevel", "BEVEL")
    bev.width = max(0.2, top_radius * 0.015)
    bev.segments = 2
    bpy.ops.object.modifier_apply(modifier="RockBevel")
    
    sub = obj.modifiers.new("IslandSubdiv", "SUBSURF")
    sub.levels = 1
    sub.render_levels = 1
    bpy.ops.object.modifier_apply(modifier="IslandSubdiv")
    
    # Re-verify material slots on polygons after modifiers
    for f in obj.data.polygons:
        if f.normal.y > 0.35 and f.center.y > -0.2:
            f.material_index = 0
        else:
            f.material_index = 1

    # Flatten the top vertices strictly back to Y = 0 so the top is a completely flat, pristine building plateau!
    for v in obj.data.vertices:
        if v.co.y > -0.35:
            v.co.y = 0.0
            
    bpy.ops.object.shade_smooth()
    
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    
    fbx_path = os.path.join(OUTPUT_DIR, f"{name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        axis_forward='Z',
        axis_up='Y',
        apply_scale_options='FBX_SCALE_ALL'
    )
    print(f"Exported {name} to {fbx_path}")
    return fbx_path

def main():
    print("=======================================================")
    print("GENERATING 3 CLOUDS & 3 FLOATING ISLANDS (UPWARD NORMALS)")
    print("=======================================================")
    
    # 1. Cloud Variations (X, Z horizontal, Y vertical)
    small_lobes = [
        (0, 0, 0, 8.0),
        (-6.5, 0.5, -1.0, 6.0),
        (6.0, -0.5, 0.5, 6.5),
        (0.5, 1.5, 3.5, 5.0)
    ]
    generate_cloud("Cloud_Small", 12.0, small_lobes, seed=10)
    
    med_lobes = [
        (0, 0, 0, 15.0),
        (-14.0, 1.0, 2.0, 11.5),
        (13.5, -1.0, -2.5, 12.0),
        (-4.0, 2.5, 6.0, 10.0),
        (7.5, 2.0, 5.5, 9.5),
        (-22.0, -1.5, -1.0, 8.0),
        (21.0, -1.0, 1.5, 8.5),
        (1.0, 1.0, -6.0, 9.0)
    ]
    generate_cloud("Cloud_Medium", 28.0, med_lobes, seed=25)
    
    large_lobes = [
        (0, 0, 0, 28.0),
        (-26.0, 2.0, 4.0, 21.0),
        (25.0, -2.0, -5.0, 22.0),
        (-10.0, 4.0, 12.0, 18.0),
        (14.0, 3.5, 8.0, 17.5),
        (-42.0, -1.5, -2.0, 15.0),
        (40.0, -2.0, 3.0, 16.0),
        (2.0, 3.0, -12.0, 17.0),
        (-18.0, -1.5, -10.0, 16.0),
        (16.0, 4.5, 14.0, 14.0),
        (-32.0, 3.0, 10.0, 13.5),
        (33.0, -2.5, -10.0, 14.5),
        (-5.0, 1.5, 20.0, 13.0),
        (8.0, -1.5, -18.0, 12.5)
    ]
    generate_cloud("Cloud_Large", 55.0, large_lobes, seed=50)
    
    # 2. Floating Island Variations (Small, Medium, Large)
    generate_floating_island("FloatingIsland_Small", top_radius=10.0, depth=14.0, seed=101)
    generate_floating_island("FloatingIsland_Medium", top_radius=22.0, depth=28.0, seed=202)
    generate_floating_island("FloatingIsland_Large", top_radius=42.0, depth=50.0, seed=303)
    
    print("ALL 6 ASSETS GENERATED SUCCESSFULLY WITH UPWARD NORMALS!")

if __name__ == "__main__":
    main()

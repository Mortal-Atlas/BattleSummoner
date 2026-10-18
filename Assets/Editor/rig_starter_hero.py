"""
Blender headless Python script for auto-rigging starter hero humanoids.
Executed inside Blender's embedded Python runtime.
Automated skinning and bone weight calculations via numpy and mathutils.
"""

import bpy  # type: ignore
import mathutils  # type: ignore
import numpy as np  # type: ignore
import os

def create_humanoid_armature(is_female=True):
    bpy.ops.object.armature_add()
    arm_obj = bpy.context.active_object
    arm_obj.name = "Armature"
    bpy.ops.object.mode_set(mode='EDIT')
    b = arm_obj.data.edit_bones
    b.remove(b[0]) # remove default bone
    
    # Proportions
    w_sh = 0.14 if is_female else 0.17
    w_arm = 0.33 if is_female else 0.36
    w_fa = 0.48 if is_female else 0.51
    w_h = 0.57 if is_female else 0.60
    
    w_leg = 0.10 if is_female else 0.11
    
    hips = b.new('Hips')
    hips.head = (0, 0, 0.03)
    hips.tail = (0, 0, 0.18)
    
    spine = b.new('Spine')
    spine.head = hips.tail
    spine.tail = (0, 0, 0.36)
    spine.parent = hips
    
    chest = b.new('Chest')
    chest.head = spine.tail
    chest.tail = (0, 0, 0.56)
    chest.parent = spine
    
    neck = b.new('Neck')
    neck.head = chest.tail
    neck.tail = (0, 0, 0.67)
    neck.parent = chest
    
    head = b.new('Head')
    head.head = neck.tail
    head.tail = (0, 0, 0.91)
    head.parent = neck
    
    # Left Arm
    l_sh = b.new('LeftShoulder')
    l_sh.head = (0, 0, 0.54)
    l_sh.tail = (-w_sh, -0.01, 0.53)
    l_sh.parent = chest
    
    l_arm = b.new('LeftUpperArm')
    l_arm.head = l_sh.tail
    l_arm.tail = (-w_arm, -0.03, 0.27)
    l_arm.parent = l_sh
    
    l_fa = b.new('LeftLowerArm')
    l_fa.head = l_arm.tail
    l_fa.tail = (-w_fa, -0.04, 0.06)
    l_fa.parent = l_arm
    
    l_h = b.new('LeftHand')
    l_h.head = l_fa.tail
    l_h.tail = (-w_h, -0.05, -0.01)
    l_h.parent = l_fa
    
    # Right Arm
    r_sh = b.new('RightShoulder')
    r_sh.head = (0, 0, 0.54)
    r_sh.tail = (w_sh, -0.01, 0.53)
    r_sh.parent = chest
    
    r_arm = b.new('RightUpperArm')
    r_arm.head = r_sh.tail
    r_arm.tail = (w_arm, -0.03, 0.27)
    r_arm.parent = r_sh
    
    r_fa = b.new('RightLowerArm')
    r_fa.head = r_arm.tail
    r_fa.tail = (w_fa, -0.04, 0.06)
    r_fa.parent = r_arm
    
    r_h = b.new('RightHand')
    r_h.head = r_fa.tail
    r_h.tail = (w_h, -0.05, -0.01)
    r_h.parent = r_fa
    
    # Left Leg
    l_ul = b.new('LeftUpperLeg')
    l_ul.head = (-w_leg, 0.0, 0.02)
    l_ul.tail = (-w_leg, 0.02, -0.44)
    l_ul.parent = hips
    
    l_ll = b.new('LeftLowerLeg')
    l_ll.head = l_ul.tail
    l_ll.tail = (-w_leg, -0.01, -0.84)
    l_ll.parent = l_ul
    
    l_ft = b.new('LeftFoot')
    l_ft.head = l_ll.tail
    l_ft.tail = (-w_leg, 0.08, -0.92)
    l_ft.parent = l_ll
    
    l_toe = b.new('LeftToes')
    l_toe.head = l_ft.tail
    l_toe.tail = (-w_leg, 0.16, -0.94)
    l_toe.parent = l_ft
    
    # Right Leg
    r_ul = b.new('RightUpperLeg')
    r_ul.head = (w_leg, 0.0, 0.02)
    r_ul.tail = (w_leg, 0.02, -0.44)
    r_ul.parent = hips
    
    r_ll = b.new('RightLowerLeg')
    r_ll.head = r_ul.tail
    r_ll.tail = (w_leg, -0.01, -0.84)
    r_ll.parent = r_ul
    
    r_ft = b.new('RightFoot')
    r_ft.head = r_ll.tail
    r_ft.tail = (w_leg, 0.08, -0.92)
    r_ft.parent = r_ll
    
    r_toe = b.new('RightToes')
    r_toe.head = r_ft.tail
    r_toe.tail = (w_leg, 0.16, -0.94)
    r_toe.parent = r_ft
    
    bpy.ops.object.mode_set(mode='OBJECT')
    return arm_obj

def dist_point_to_segment(p, a, b):
    # Vectorized point to segment distance
    # p: (N, 3), a: (3,), b: (3,)
    ab = b - a
    ab_len_sq = np.dot(ab, ab)
    if ab_len_sq < 1e-8:
        return np.linalg.norm(p - a, axis=1)
    t = np.clip(np.sum((p - a) * ab, axis=1) / ab_len_sq, 0.0, 1.0)
    projection = a + t[:, np.newaxis] * ab
    return np.linalg.norm(p - projection, axis=1)

def skin_mesh_to_armature(mesh_obj, arm_obj):
    n_verts = len(mesh_obj.data.vertices)
    coords = np.empty(n_verts * 3, dtype=np.float32)
    mesh_obj.data.vertices.foreach_get('co', coords)
    coords = coords.reshape((n_verts, 3))
    
    bones = arm_obj.data.bones
    bone_names = [bone.name for bone in bones]
    n_bones = len(bone_names)
    
    # Calculate distance matrix (n_verts, n_bones)
    distances = np.zeros((n_verts, n_bones), dtype=np.float32)
    for i, name in enumerate(bone_names):
        bone = bones[name]
        a = np.array(bone.head_local, dtype=np.float32)
        b = np.array(bone.tail_local, dtype=np.float32)
        distances[:, i] = dist_point_to_segment(coords, a, b)
    
    # Inverse distance weighting with power 2.5
    eps = 0.015
    raw_weights = 1.0 / (np.maximum(distances, 0.001) + eps) ** 2.5
    
    # Keep top 4 bones per vertex (standard Unity GPU skinning)
    top_indices = np.argsort(-raw_weights, axis=1)[:, :4]
    
    # Create vertex groups
    mesh_obj.vertex_groups.clear()
    vgroups = [mesh_obj.vertex_groups.new(name=name) for name in bone_names]
    
    # Build sparse vertex-bone weight entries
    print(f"Assigning skin weights for {n_verts} vertices...")
    for v_idx in range(n_verts):
        b_idx_top = top_indices[v_idx]
        w_top = raw_weights[v_idx, b_idx_top]
        w_sum = np.sum(w_top)
        if w_sum > 1e-6:
            w_norm = w_top / w_sum
            for bi, w in zip(b_idx_top, w_norm):
                if w > 0.01:
                    vgroups[bi].add([v_idx], float(w), 'REPLACE')
    
    # Parent mesh to armature with Armature Modifier
    mesh_obj.parent = arm_obj
    arm_mod = mesh_obj.modifiers.new(name="Armature", type='ARMATURE')
    arm_mod.object = arm_obj
    print(f"✅ Skinned {mesh_obj.name} to {arm_obj.name} with {n_bones} Humanoid bones.")

def rig_character(glb_path, out_fbx_path, tex_path, is_female):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    print(f"\n=======================================================")
    print(f"RIGGING CHARACTER: {glb_path}")
    print(f"=======================================================")
    
    bpy.ops.import_scene.gltf(filepath=glb_path)
    mesh_obj = [o for o in bpy.data.objects if o.type == 'MESH'][0]
    mesh_obj.name = "Character_Mesh"
    
    # Decimate slightly if over 100k verts for optimal game performance while preserving pristine visuals
    orig_verts = len(mesh_obj.data.vertices)
    if orig_verts > 80000:
        ratio = 45000.0 / float(orig_verts)
        print(f"Optimizing mesh for real-time engine: {orig_verts} -> ~45k verts (ratio {ratio:.3f})")
        bpy.context.view_layer.objects.active = mesh_obj
        bpy.ops.object.modifier_add(type='DECIMATE')
        mesh_obj.modifiers['Decimate'].ratio = ratio
        bpy.ops.object.modifier_apply(modifier='Decimate')
        print(f"Mesh optimized: {len(mesh_obj.data.vertices)} vertices.")
    
    # Build Armature
    arm_obj = create_humanoid_armature(is_female)
    
    # Skinning
    skin_mesh_to_armature(mesh_obj, arm_obj)
    
    # Setup PBR Material
    mat = bpy.data.materials.new(name="M_Hero_Starter")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf and os.path.exists(tex_path):
        tex_node = mat.node_tree.nodes.new('ShaderNodeTexImage')
        tex_node.image = bpy.data.images.load(tex_path)
        mat.node_tree.links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])
    
    mesh_obj.data.materials.clear()
    mesh_obj.data.materials.append(mat)
    
    # Export FBX
    bpy.ops.object.select_all(action='DESELECT')
    mesh_obj.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    
    bpy.ops.export_scene.fbx(
        filepath=out_fbx_path,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'ARMATURE', 'MESH'},
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        armature_nodetype='NULL'
    )
    print(f"🎉 Exported rigged humanoid FBX to: {out_fbx_path}")

if __name__ == "__main__":
    female_glb = "Assets/Models/Characters/Starter/femalestarter.glb"
    female_fbx = "Assets/Models/Characters/Starter/Starter_Female_Rigged.fbx"
    female_tex = "Assets/Models/Characters/Starter/T_Female_Starter_Albedo.png"
    rig_character(female_glb, female_fbx, female_tex, is_female=True)
    
    male_glb = "Assets/Models/Characters/Starter/MaleStarter.glb"
    male_fbx = "Assets/Models/Characters/Starter/Starter_Male_Rigged.fbx"
    male_tex = "Assets/Models/Characters/Starter/T_Male_Starter_Albedo.png"
    rig_character(male_glb, male_fbx, male_tex, is_female=False)

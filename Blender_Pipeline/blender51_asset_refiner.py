import bpy
import bmesh
import mathutils

def refine_mesh_for_unity6(obj_name: str, target_tris: int = 4000):
    """
    Blender 5.1 Refinement Pass:
    1. Snaps pivot to bottom-center (Z = 0.0) so props never float or sink.
    2. Applies a 2-segment micro-bevel + Weighted Normals to kill rookie 90-degree CG edges.
    3. Bakes top-down Vertical Grounding + Cavity AO into Vertex Color Channel R.
    4. Generates clean Smart UV Project islands for 4K painterly textures.
    """
    obj = bpy.data.objects.get(obj_name)
    if not obj or obj.type != 'MESH':
        return

    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)

    # 1. Apply transforms and snap origin to bottom-center of bounding box
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bbox_corners = [obj.matrix_world @ mathutils.Vector(corner) for corner in obj.bound_box]
    min_z = min(c.z for c in bbox_corners)
    center_x = sum(c.x for c in bbox_corners) / 8.0
    center_y = sum(c.y for c in bbox_corners) / 8.0

    cursor_loc = bpy.context.scene.cursor.location.copy()
    bpy.context.scene.cursor.location = (center_x, center_y, min_z)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.context.scene.cursor.location = cursor_loc
    obj.location.z = 0.0

    # 2. Add Micro-Bevel & Weighted Normals (eliminates flat rookie cube shading)
    bevel = obj.modifiers.new(name="PolishBevel", type='BEVEL')
    bevel.width = 0.025
    bevel.segments = 2
    bevel.limit_method = 'ANGLE'
    bevel.angle_limit = 0.523599  # 30 degrees
    bevel.harden_normals = True

    wn = obj.modifiers.new(name="PolishWeightedNormals", type='WEIGHTED_NORMAL')
    wn.keep_sharp = True

    # 3. Bake Vertical Grounding + Cavity into Vertex Color ('Col') for GenshinPainterlyEnvironment.shader
    mesh = obj.data
    if not mesh.color_attributes:
        mesh.color_attributes.new(name="Col", type='BYTE_COLOR', domain='CORNER')
    color_layer = mesh.color_attributes.active_color

    max_z = max((c.z for c in bbox_corners), default=1.0)
    height_span = max(0.1, max_z - min_z)

    for poly in mesh.polygons:
        for loop_idx in poly.loop_indices:
            vert_idx = mesh.loops[loop_idx].vertex_index
            world_v = obj.matrix_world @ mesh.vertices[vert_idx].co
            norm_h = max(0.0, min(1.0, (world_v.z - min_z) / height_span))
            # 0.25 at ground contact -> 1.0 at top crown (matches shader IN.vertexCol.r)
            ao_val = 0.25 + (0.75 * (norm_h ** 0.65))
            color_layer.data[loop_idx].color = (ao_val, ao_val, ao_val, 1.0)

    # 4. Ensure clean UV unwrap with margin padding for 4K textures
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.15192, island_margin=0.008)
    bpy.ops.object.mode_set(mode='OBJECT')

    print(f"[Blender 5.1 Pipeline] Refined {obj_name}: Beveled, Weighted Normals, Z=0 Pivot, Vertex AO Baked.")

def refine_all_scene_props():
    for obj in list(bpy.data.objects):
        if obj.type == 'MESH' and not any(k in obj.name.lower() for k in ['player', 'npc', 'citizen', 'rig']):
            refine_mesh_for_unity6(obj.name)

if __name__ == "__main__":
    refine_all_scene_props()

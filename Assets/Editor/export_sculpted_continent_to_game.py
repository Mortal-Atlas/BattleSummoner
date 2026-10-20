import bpy
import os
import shutil

BLEND_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\Continent_Map_Sculptjz.blend"
NEW_FBX_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\Continent_Map_Sculptjz.fbx"
COMPAT_FBX_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\CircularContinent_MacroLevel1.fbx"
SCULPT_FBX_PATH = r"D:\Games\BattleSummoner\BattleSummoner\Assets\Models\Environments\Continent_Map_Sculpt.fbx"

def export_sculpted_continent():
    terrain_obj = bpy.data.objects.get("Floor01_MacroContinent_Terrain")
    if not terrain_obj:
        print("❌ Error: Floor01_MacroContinent_Terrain not found in current scene!")
        return

    # Switch to object mode to ensure all vertex updates are flushed
    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.object.select_all(action='DESELECT')
    terrain_obj.select_set(True)
    bpy.context.view_layer.objects.active = terrain_obj

    print(f"Exporting sculpted terrain ({len(terrain_obj.data.vertices):,} vertices) to FBX...")
    bpy.ops.export_scene.fbx(
        filepath=NEW_FBX_PATH,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'MESH'},
        mesh_smooth_type='OFF',
        add_leaf_bones=False,
        bake_anim=False
    )
    print(f"✅ Successfully exported to: {NEW_FBX_PATH}")

    # Sync to compatibility paths
    shutil.copyfile(NEW_FBX_PATH, COMPAT_FBX_PATH)
    shutil.copyfile(NEW_FBX_PATH, SCULPT_FBX_PATH)
    print(f"✅ Synced compatibility FBXs to: {COMPAT_FBX_PATH} and {SCULPT_FBX_PATH}")

if __name__ == "__main__":
    export_sculpted_continent()

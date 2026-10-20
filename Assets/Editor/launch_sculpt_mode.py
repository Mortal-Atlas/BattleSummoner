import bpy

# 1. Select the high-resolution terrain mesh and enter Sculpt Mode
obj = bpy.data.objects.get("Floor01_MacroContinent_Terrain")
if obj:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.object.mode_set(mode='SCULPT')
    except Exception as e:
        print("Mode set exception:", e)

# 2. Viewport optimization: MatCap Cavity shading and wide clipping
for a in bpy.context.screen.areas:
    if a.type == 'VIEW_3D':
        for sp in a.spaces:
            if sp.type == 'VIEW_3D':
                sp.clip_start = 1.0
                sp.clip_end = 50000.0
                sp.shading.type = 'SOLID'
                sp.shading.light = 'MATCAP'
                sp.shading.show_cavity = True
                sp.shading.cavity_type = 'BOTH'
                sp.shading.cavity_ridge_factor = 2.0
                sp.shading.cavity_valley_factor = 2.0

# 3. Sculpt brush settings: Locked to SCENE (meters)
ts = bpy.context.tool_settings
sculpt = ts.sculpt
ups = sculpt.unified_paint_settings
ups.use_unified_size = True
ups.use_locked_size = 'SCENE'
ups.unprojected_size = 120.0  # 120 meters diameter
ups.size = 150
ups.use_unified_strength = True
ups.strength = 0.5

clay = bpy.data.brushes.get("Clay Strips")
if clay:
    sculpt.brush = clay

# 4. Try auto-starting BlenderMCP socket server
try:
    import addon
    if hasattr(addon, "start_server"):
        addon.start_server()
    elif hasattr(bpy.ops, "blendermcp"):
        getattr(bpy.ops.blendermcp, "start", lambda: None)()
except Exception as e:
    pass

print("✨ Ready for continent sculpting!")

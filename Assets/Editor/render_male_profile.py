import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
blend_path = os.path.join(project_root, "Assets/Models/Characters/Character_Base_Male_MouthRigged.blend")
out_img = os.path.join(project_root, "Assets/Screenshots/Male_Profile_Blender.png")

bpy.ops.wm.open_mainfile(filepath=blend_path)

# Set up render settings
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, 'RenderSettings') and 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
scene.render.resolution_x = 800
scene.render.resolution_y = 800
scene.render.filepath = out_img

# Remove existing cameras and lights
for obj in list(scene.objects):
    if obj.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(obj, do_unlink=True)

# Add camera looking from +X towards origin at head level
# Character head is around Z=1.6, Y=0
cam_data = bpy.data.cameras.new("ProfileCamera")
cam_data.ortho_scale = 1.0
cam_data.type = 'ORTHO'
cam_obj = bpy.data.objects.new("ProfileCamera", cam_data)
scene.collection.objects.link(cam_obj)
scene.camera = cam_obj

# Position at X=3, Y=0, Z=1.58
# In Blender, looking along -X: rotation = (90 deg on X, 0 on Y, 90 deg on Z)
cam_obj.location = (3.0, 0.0, 1.58)
cam_obj.rotation_euler = (math.radians(90), 0, math.radians(90))

# Add a light
light_data = bpy.data.lights.new(name="SunLight", type='SUN')
light_data.energy = 3.0
light_obj = bpy.data.objects.new("SunLight", light_data)
scene.collection.objects.link(light_obj)
light_obj.rotation_euler = (math.radians(45), math.radians(30), 0)

bpy.ops.render.render(write_still=True)
print(f"Rendered profile to: {out_img}")

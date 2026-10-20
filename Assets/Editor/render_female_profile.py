import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
blend_path = os.path.join(project_root, "Assets/Models/Characters/Character_Base_Female_MouthRigged.blend")
out_img = os.path.join(project_root, "Assets/Screenshots/Female_Profile_Blender.png")

bpy.ops.wm.open_mainfile(filepath=blend_path)

scene = bpy.context.scene
scene.render.resolution_x = 800
scene.render.resolution_y = 800
scene.render.filepath = out_img

for obj in list(scene.objects):
    if obj.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(obj, do_unlink=True)

cam_data = bpy.data.cameras.new("ProfileCamera")
cam_data.ortho_scale = 1.0
cam_data.type = 'ORTHO'
cam_obj = bpy.data.objects.new("ProfileCamera", cam_data)
scene.collection.objects.link(cam_obj)
scene.camera = cam_obj

cam_obj.location = (3.0, 0.0, 1.48)
cam_obj.rotation_euler = (math.radians(90), 0, math.radians(90))

light_data = bpy.data.lights.new(name="SunLight", type='SUN')
light_data.energy = 3.0
light_obj = bpy.data.objects.new("SunLight", light_data)
scene.collection.objects.link(light_obj)
light_obj.rotation_euler = (math.radians(45), math.radians(30), 0)

bpy.ops.render.render(write_still=True)
print(f"Rendered female profile to: {out_img}")

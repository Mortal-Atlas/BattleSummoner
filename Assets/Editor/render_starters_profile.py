import bpy
import math
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
models = [
    ("femalestarter.glb", "Assets/Models/Characters/Starter/femalestarter.glb", "FemaleStarter_Profile.png"),
    ("MaleStarter.glb", "Assets/Models/Characters/Starter/MaleStarter.glb", "MaleStarter_Profile.png"),
]

for name, rel_path, out_png in models:
    full_path = os.path.join(project_root, rel_path)
    out_img = os.path.join(project_root, f"Assets/Screenshots/{out_png}")
    
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=full_path)
    
    scene = bpy.context.scene
    scene.render.resolution_x = 600
    scene.render.resolution_y = 600
    scene.render.filepath = out_img
    
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 1.0
    cam_obj = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    
    # Camera at +X looking along -X (+Y is to the LEFT, -Y is to the RIGHT)
    cam_obj.location = (3.0, 0.0, 0.75)
    cam_obj.rotation_euler = (math.radians(90), 0, math.radians(90))
    
    light_data = bpy.data.lights.new("Sun", type='SUN')
    light_data.energy = 3.0
    light_obj = bpy.data.objects.new("Sun", light_data)
    scene.collection.objects.link(light_obj)
    light_obj.rotation_euler = (math.radians(45), math.radians(30), 0)
    
    bpy.ops.render.render(write_still=True)
    print(f"Rendered {out_png}")

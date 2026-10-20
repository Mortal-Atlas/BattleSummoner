import bpy
import os

project_root = r"d:/Games/BattleSummoner/BattleSummoner"
out_fbx = f"{project_root}/Assets/Models/Characters/Starter/coord_test.fbx"

bpy.ops.wm.read_factory_settings(use_empty=True)
mesh_data = bpy.data.meshes.new("CoordTestMesh")
verts = [
    (0.0, 0.0, 2.0),   # 0: Head
    (1.0, 0.0, 1.0),   # 1: Blender +X
    (-1.0, 0.0, 1.0),  # 2: Blender -X
    (0.0, 1.0, 1.0),   # 3: Blender +Y
    (0.0, -1.0, 1.0)   # 4: Blender -Y
]
faces = [
    (0, 1, 3), # Head, +X, +Y
    (0, 3, 2), # Head, +Y, -X
    (0, 2, 4), # Head, -X, -Y
    (0, 4, 1)  # Head, -Y, +X
]
mesh_data.from_pydata(verts, [], faces)
mesh_obj = bpy.data.objects.new("CoordTestMesh", mesh_data)
bpy.context.collection.objects.link(mesh_obj)

bpy.ops.export_scene.fbx(
    filepath=out_fbx,
    axis_forward='-Z',
    axis_up='Y',
    bake_space_transform=True
)
print("Exported coord_test.fbx with faces")

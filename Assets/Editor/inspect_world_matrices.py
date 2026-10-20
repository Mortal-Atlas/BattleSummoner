import bpy

print("=== OBJECT PARENTING AND WORLD TRANSFORMS ===")
for obj in bpy.data.objects:
    if "bridge" in obj.name.lower() or "citadel" in obj.name.lower():
        matrix_world = obj.matrix_world
        loc, rot, scale = matrix_world.decompose()
        parent_name = obj.parent.name if obj.parent else "None"
        print(f"{obj.name}: parent={parent_name}, local_loc={obj.location.z:.2f}, world_pos=({loc.x:.1f}, {loc.y:.1f}, {loc.z:.1f})")

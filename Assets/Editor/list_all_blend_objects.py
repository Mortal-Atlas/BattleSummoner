import bpy

print("=== ALL OBJECTS IN BLEND FILE ===")
for obj in bpy.data.objects:
    print(f"- {obj.name} (type: {obj.type}, parent: {obj.parent.name if obj.parent else 'None'}, loc: {obj.location}, dim: {obj.dimensions})")
    if obj.modifiers:
        print(f"    modifiers: {[m.name + ' (' + m.type + ')' for m in obj.modifiers]}")

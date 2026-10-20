import bpy

print("=== COLLECTIONS AND OBJECTS IN BLEND FILE ===")
for col in bpy.data.collections:
    print(f"Collection: {col.name} ({len(col.objects)} objects)")
    for obj in col.objects[:15]:
        print(f"  - {obj.name} (type: {obj.type}, loc: {obj.location})")
    if len(col.objects) > 15:
        print(f"  ... and {len(col.objects) - 15} more objects")

print("\n=== TOP LEVEL OBJECTS ===")
for obj in bpy.context.scene.objects:
    if not obj.users_collection:
        print(f"Orphan/Scene object: {obj.name}")

print("\n=== BRIDGES ===")
bridges = [obj for obj in bpy.data.objects if "bridge" in obj.name.lower()]
for b in bridges:
    print(f"Bridge: {b.name} at {b.location}, rot={b.rotation_euler}, dim={b.dimensions}")

print("\n=== CENTRAL CITADEL / CATHEDRAL ===")
citadel_objs = [obj for obj in bpy.data.objects if any(k in obj.name.lower() for k in ["citadel", "cathedral", "church", "center", "castle", "palace"])]
for c in citadel_objs:
    print(f"Citadel/Cathedral: {c.name} at {c.location}, dim={c.dimensions}")

print("\n=== TERRAIN / CONTINENT OBJECTS ===")
terrains = [obj for obj in bpy.data.objects if any(k in obj.name.lower() for k in ["continent", "terrain", "island", "landscape", "volcano", "mountain"])]
for t in terrains:
    print(f"Terrain: {t.name}, vertices={len(t.data.vertices) if t.type=='MESH' else 0}, bounds={t.dimensions}, loc={t.location}")

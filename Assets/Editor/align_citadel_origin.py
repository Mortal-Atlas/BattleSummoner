import bpy
import mathutils

citadel_root = bpy.data.objects.get("Concentric_Citadel_Root")
print("Citadel root location before:", citadel_root.location)

# If citadel_root location is (0, 0, 16), then all children whose world pos was ~32 should actually be at world pos ~16!
# Let's inspect cathedral world pos
cathedral = bpy.data.objects.get("Citadel_Meshy_GrandCathedral")
if cathedral:
    print("Cathedral local pos:", cathedral.location)
    print("Cathedral world pos:", cathedral.matrix_world.translation)

# Set citadel root to (0, 0, 0)
citadel_root.location = (0.0, 0.0, 0.0)

# Now ensure every citadel element sits cleanly at its true world elevation (Citadel terrain floor Z ≈ 16.0 - 16.5m)
for child in citadel_root.children:
    # If the child was placed assuming root at 0 or root at 16:
    # We want child's world Z to match terrain floor Z ≈ 16.0
    if child == cathedral:
        # Mesh base at 0, sits flush on terrain Z = 16.2
        min_z = min(v.co.z for v in child.data.vertices)
        child.location = (0.0, 0.0, 16.3 - min_z)
    elif child.name.startswith("Citadel_Tier") or child.name.startswith("Citadel_House"):
        child.location.z = 16.0
    elif child.name.startswith("Citadel_Outer") or child.name.startswith("Citadel_Inner"):
        # Curtain walls base at 16.0
        pass
    elif child.name.startswith("Citadel_Gatehouse"):
        child.location.z = 16.0

print("\nCathedral world pos after:", cathedral.matrix_world.translation if cathedral else "None")

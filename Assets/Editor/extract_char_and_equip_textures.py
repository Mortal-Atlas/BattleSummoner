import bpy
import os

models_dirs = [
    r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Characters",
    r"d:\Games\BattleSummoner\BattleSummoner\Assets\Models\Equipment"
]

for mdir in models_dirs:
    tex_dir = os.path.join(mdir, "Textures")
    os.makedirs(tex_dir, exist_ok=True)
    fbx_files = [f for f in os.listdir(mdir) if f.endswith(".fbx")]
    
    for fbx_name in fbx_files:
        base_name = os.path.splitext(fbx_name)[0]
        fbx_path = os.path.join(mdir, fbx_name)
        
        bpy.ops.wm.read_factory_settings(use_empty=True)
        try:
            bpy.ops.import_scene.fbx(filepath=fbx_path)
        except Exception as e:
            print(f"Error importing {fbx_name}: {e}")
            continue
            
        print(f"\nProcessing {base_name}...")
        img_idx = 0
        for img in bpy.data.images:
            if img.name != 'Render Result':
                tex_out = os.path.join(tex_dir, f"tex_{base_name}_{img_idx}.png")
                img.filepath_raw = tex_out
                img.file_format = 'PNG'
                try:
                    img.save()
                    print(f"  Saved texture: {tex_out}")
                except Exception as e:
                    print(f"  Save failed: {e}")
                img_idx += 1

print("\nAll equipment & character textures extracted successfully!")

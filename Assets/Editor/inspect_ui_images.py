import bpy  # type: ignore
import numpy as np  # type: ignore

def analyze_textures():
    # 1. Emote
    img_emote = bpy.data.images.load(r'C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.user_uploaded\media_1792294499405.jpg')
    w, h = img_emote.size
    arr_emote = np.array(img_emote.pixels[:], dtype=np.float32).reshape((h, w, 4))
    
    # 2. Carousel
    img_car = bpy.data.images.load(r'C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.user_uploaded\media_1792295308756.jpg')
    cw, ch = img_car.size
    arr_car = np.array(img_car.pixels[:], dtype=np.float32).reshape((ch, cw, 4))
    
    print(f"Emote size: {w}x{h}, Carousel size: {cw}x{ch}")
    
    # Analyze Emote circle
    cy, cx = h / 2.0, w / 2.0
    row = arr_emote[int(cy), :, :3]
    # Find transition along horizontal centerline from center to right
    for rad in range(50, 400, 5):
        px = row[int(cx + rad)]
        diff_from_corner = np.linalg.norm(px - arr_emote[10, 10, :3])
        if rad % 20 == 0:
            print(f"Emote r={rad}: px={px}, diff_from_bg={diff_from_corner:.3f}")

    # Analyze Carousel
    print("\nCarousel vertical centerline sample:")
    for y_rel in [0.1, 0.25, 0.5, 0.75, 0.9]:
        print(f"Y={y_rel}: px={arr_car[int(ch * y_rel), int(cw * 0.5), :3]}")

analyze_textures()

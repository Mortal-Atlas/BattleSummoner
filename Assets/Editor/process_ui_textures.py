import bpy  # type: ignore
import numpy as np  # type: ignore

def process_textures():
    # ----------------------------------------------------
    # 1. Process Emote Button Drama Masks
    # ----------------------------------------------------
    emote_path = r'C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.user_uploaded\media_1792294499405.jpg'
    img_emote = bpy.data.images.load(emote_path)
    w, h = img_emote.size
    arr_emote = np.array(img_emote.pixels[:], dtype=np.float32).reshape((h, w, 4))
    
    cx, cy = 512.0, 285.5
    R = 225.0
    
    # Crop to square around circle
    pad = 4
    x0 = int(cx - R - pad)
    x1 = int(cx + R + pad)
    y0 = int(cy - R - pad)
    y1 = int(cy + R + pad)
    
    cropped = arr_emote[y0:y1, x0:x1].copy()
    ch, cw, _ = cropped.shape
    
    # Compute antialiased alpha
    y_coords, x_coords = np.ogrid[:ch, :cw]
    dist = np.sqrt((x_coords - (cx - x0))**2 + (y_coords - (cy - y0))**2)
    alpha = np.clip((R - dist) / 1.5, 0.0, 1.0)
    cropped[:, :, 3] = alpha
    
    import os
    out_path_emote = os.path.abspath("Assets/Textures/UI/T_Emote_Button_DramaMasks.png")
    os.makedirs(os.path.dirname(out_path_emote), exist_ok=True)
    out_emote = bpy.data.images.new("T_Emote_Button_DramaMasks", width=cw, height=ch, alpha=True)
    out_emote.pixels = cropped.ravel().tolist()
    out_emote.save_render(filepath=out_path_emote)
    print(f"✅ Saved Emote Button: {cw}x{ch} to {out_path_emote}")
    
    # ----------------------------------------------------
    # 2. Process Item Carousel Chassis
    # ----------------------------------------------------
    car_path = r'C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.user_uploaded\media_1792295308756.jpg'
    img_car = bpy.data.images.load(car_path)
    cw, ch = img_car.size
    arr_car = np.array(img_car.pixels[:], dtype=np.float32).reshape((ch, cw, 4))
    
    # Detect background checkerboard
    rgb = arr_car[:, :, :3]
    max_c = rgb.max(axis=-1)
    min_c = rgb.min(axis=-1)
    chroma = max_c - min_c
    lum = rgb.mean(axis=-1)
    
    # Checkerboard is desaturated and dark
    # Dark checker squares: lum < 0.02
    # Light checker squares: lum ~ 0.11, chroma < 0.025
    is_bg = (lum < 0.135) & (chroma < 0.035)
    
    # Inside the left and right sockets (where items will be rendered):
    # Left socket center: X = 0.27 * cw, Y = 0.50 * ch, radius ~ 0.11 * cw
    # Right socket center: X = 0.73 * cw, Y = 0.50 * ch, radius ~ 0.11 * cw
    y_grid, x_grid = np.ogrid[:ch, :cw]
    left_socket_dist = np.sqrt((x_grid - 0.27 * cw)**2 + (y_grid - 0.50 * ch)**2)
    right_socket_dist = np.sqrt((x_grid - 0.73 * cw)**2 + (y_grid - 0.50 * ch)**2)
    
    # In the socket interiors (radius < 0.09 * cw), make background transparent so dynamic item icons show through
    in_left_socket = left_socket_dist < (0.095 * cw)
    in_right_socket = right_socket_dist < (0.095 * cw)
    
    # Alpha mask:
    # Foreground chassis has alpha 1.0
    # Background has alpha 0.0
    alpha_car = np.ones((ch, cw), dtype=np.float32)
    alpha_car[is_bg] = 0.0
    alpha_car[in_left_socket & (lum < 0.15)] = 0.0
    alpha_car[in_right_socket & (lum < 0.15)] = 0.0
    
    arr_car[:, :, 3] = alpha_car
    
    out_path_car = os.path.abspath("Assets/Textures/UI/T_ItemCarousel_Chassis.png")
    os.makedirs(os.path.dirname(out_path_car), exist_ok=True)
    out_car = bpy.data.images.new("T_ItemCarousel_Chassis", width=cw, height=ch, alpha=True)
    out_car.pixels = arr_car.ravel().tolist()
    out_car.save_render(filepath=out_path_car)
    print(f"✅ Saved Item Carousel Chassis: {cw}x{ch} to {out_path_car}")

if __name__ == "__main__":
    process_textures()

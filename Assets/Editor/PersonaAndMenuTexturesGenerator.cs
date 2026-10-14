using System;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// PersonaAndMenuTexturesGenerator generates custom procedural textures for:
/// - Persona-style Visual Novel Dialogue (Skewed dialogue box, slanted nameplate, comic choice tags, portraits)
/// - Inventory Menu (Frame, slot icons, rarity borders)
/// - Item Shop Menu (Gilded canopy banner, currency crests)
/// - Settings Menu (Frosted slate panel, slider tracks)
/// - Summoning Ritual Menu (Altar canvas, concentric rune rings)
/// </summary>
public static class PersonaAndMenuTexturesGenerator
{
    private const string TEXTURE_DIR = "Assets/Textures/UI";

    [MenuItem("Tools/BattleSummoner/Generate Menu & Persona Textures")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(TEXTURE_DIR);

        GeneratePersonaDialogueBox();
        GeneratePersonaNameplate();
        GeneratePersonaChoiceBtn();
        GeneratePersonaPortraitMargo();
        GeneratePersonaPortraitMerchant();
        GeneratePersonaPortraitDemon();
        GenerateInventoryFrame();
        GenerateItemSlot();
        GenerateShopBanner();
        GenerateSettingsPanel();
        GenerateSummoningAltar();

        // Tomodachi & Social Link (Megulink / Divinatus) Textures
        GenerateSocialHubFrame();
        GenerateSocialArcanaCard();
        GenerateSocialHeartIcon();
        GenerateSocialBrokenHeart();
        GenerateSocialAngerMark();
        GenerateSocialLinkHudBtn();
        GenerateSocialPortraitCorvus();
        GenerateSocialPortraitIgnis();
        GenerateSocialPortraitTalos();
        GenerateSocialPortraitAegis();

        // Character Customization Textures
        GenerateCustomizationFrame();
        GenerateCustomizationTabBtn();
        GenerateCustomizationTurntableRing();
        GenerateCustomizationSliderTrack();
        GenerateCustomizationSliderThumb();
        GenerateCustomizationColorSwatch();
        GenerateCustomizationRandomBtn();
        GenerateCustomizationResetBtn();
        GenerateCustomizationSaveBtn();
        GenerateCustomizeHudBtn();

        // Famigotchi / Tamagotchi Pet Familiar Care Textures
        GenerateSocialPortraitPygmy();
        GenerateSocialPortraitAqua();
        GeneratePetFoodChocoDrop();
        GeneratePetFoodStarFlan();
        GeneratePetFoodBerryPuff();
        GeneratePetFoodHoneyPuff();
        GeneratePetWaterDewBowl();
        GeneratePetBatheSponge();
        GeneratePetPresentToyBell();
        GeneratePetPresentGiftBox();
        GeneratePetCareHudBtn();

        // Risk of Rain & Dungeon Dimension Textures
        GenerateRoRTimerFrame();
        GenerateRoRDifficultyBarFill();
        GenerateBoonCardFrame();
        GenerateBoonVoidEdge();
        GenerateBoonChronoStride();
        GenerateBoonVampiricLeech();
        GenerateBoonStaticConduit();
        GenerateBoonTitansAegis();
        GenerateBoonManaTorrent();
        GenerateBoonFamiliarFrenzy();
        GenerateBoonExecuteStrike();
        GenerateBoonCullWeak();

        ReimportAllSprites();
        Debug.Log("🎨 [TEXTURES] Successfully generated and imported all Persona Dialogue & Menu sprites!");
    }

    public static void ReimportAllSprites()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string[] files = Directory.GetFiles(TEXTURE_DIR, "Mockup_*.png");
        for (int i = 0; i < files.Length; i++)
        {
            string p = files[i].Replace('\\', '/');
            TextureImporter imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void SavePNG(Texture2D tex, string fileName)
    {
        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(TEXTURE_DIR, fileName);
        File.WriteAllBytes(path, bytes);
        UnityEngine.Object.DestroyImmediate(tex);
    }

    #region Persona Dialogue Textures
    private static void GeneratePersonaDialogueBox()
    {
        int w = 680;
        int h = 280;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgCharcoal = new Color(0.08f, 0.09f, 0.12f, 0.96f);
        Color bgInner = new Color(0.12f, 0.13f, 0.17f, 0.96f);
        Color borderRed = new Color(0.92f, 0.20f, 0.26f, 1f);
        Color borderGold = new Color(1.0f, 0.82f, 0.24f, 1f);
        Color hatchColor = new Color(0.18f, 0.20f, 0.25f, 0.8f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        // Skew angle: slant right by ~18 pixels from bottom to top
        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h;
            float skewOffset = (normY - 0.5f) * 28f;
            float leftEdge = 24f + skewOffset;
            float rightEdge = (w - 24f) + skewOffset;

            for (int x = 0; x < w; x++)
            {
                if (x >= leftEdge && x <= rightEdge && y >= 14 && y <= h - 14)
                {
                    // Distance to borders
                    float distLeft = x - leftEdge;
                    float distRight = rightEdge - x;
                    float distBottom = y - 14;
                    float distTop = (h - 14) - y;
                    float minDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

                    if (minDist < 4f)
                    {
                        // Outer sharp high-contrast border: red to gold gradient along top
                        cols[y * w + x] = Color.Lerp(borderRed, borderGold, normY);
                    }
                    else if (minDist < 7f)
                    {
                        // Inner dark accent pinstripe
                        cols[y * w + x] = new Color(0.02f, 0.02f, 0.03f, 1f);
                    }
                    else
                    {
                        // Background base
                        Color c = Color.Lerp(bgCharcoal, bgInner, normY);

                        // Persona-style dynamic diagonal comic hatch stripes along the bottom-right
                        if ((x + y) % 18 < 3 && x > w * 0.55f && y < h * 0.45f)
                        {
                            c = Color.Lerp(c, hatchColor, 0.6f);
                        }

                        // Subtle golden corner wedge on bottom-left
                        if (x < leftEdge + 45f && y < 45f)
                        {
                            c = Color.Lerp(c, borderGold, 0.4f);
                        }

                        cols[y * w + x] = c;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PersonaDialogueBox.png");
    }

    private static void GeneratePersonaNameplate()
    {
        int w = 340;
        int h = 68;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color fillCrimson = new Color(0.85f, 0.15f, 0.22f, 1f);
        Color fillDark = new Color(0.12f, 0.02f, 0.04f, 1f);
        Color goldTrim = new Color(1.0f, 0.88f, 0.30f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        // Slanted parallelogram badge
        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h;
            float slant = (normY - 0.5f) * 22f;
            float left = 18f + slant;
            float right = (w - 18f) + slant;

            for (int x = 0; x < w; x++)
            {
                if (x >= left && x <= right && y >= 6 && y <= h - 6)
                {
                    float distLeft = x - left;
                    float distRight = right - x;
                    float distBottom = y - 6;
                    float distTop = (h - 6) - y;
                    float minDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

                    if (minDist < 3.5f)
                    {
                        cols[y * w + x] = goldTrim;
                    }
                    else
                    {
                        float normX = (x - left) / (right - left);
                        cols[y * w + x] = Color.Lerp(fillCrimson, fillDark, normX * 0.7f);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PersonaNameplate.png");
    }

    private static void GeneratePersonaChoiceBtn()
    {
        int w = 480;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgDark = new Color(0.10f, 0.11f, 0.15f, 0.95f);
        Color bgHighlight = new Color(0.16f, 0.18f, 0.24f, 0.95f);
        Color borderGold = new Color(1.0f, 0.84f, 0.28f, 1f);
        Color accentRed = new Color(0.92f, 0.22f, 0.28f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        // Slanted comic dialogue choice speech tag with left chevron notch
        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h;
            float slant = (normY - 0.5f) * 16f;
            float left = 20f + slant;
            float right = (w - 14f) + slant;

            for (int x = 0; x < w; x++)
            {
                if (x >= left && x <= right && y >= 6 && y <= h - 6)
                {
                    float distLeft = x - left;
                    float distRight = right - x;
                    float distBottom = y - 6;
                    float distTop = (h - 6) - y;
                    float minDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

                    if (minDist < 3f)
                    {
                        cols[y * w + x] = borderGold;
                    }
                    else if (distLeft < 14f)
                    {
                        // Left angled accent pip
                        cols[y * w + x] = accentRed;
                    }
                    else
                    {
                        cols[y * w + x] = Color.Lerp(bgDark, bgHighlight, (float)x / w);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PersonaChoiceBtn.png");
    }

    private static void GeneratePersonaPortraitMargo()
    {
        int w = 280;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgHalo = new Color(0.92f, 0.20f, 0.28f, 0.35f);
        Color skinColor = new Color(0.98f, 0.88f, 0.82f, 1f);
        Color hairViolet = new Color(0.24f, 0.14f, 0.34f, 1f);
        Color hairDark = new Color(0.14f, 0.08f, 0.22f, 1f);
        Color goldPin = new Color(1.0f, 0.85f, 0.25f, 1f);
        Color eyeGlow = new Color(0.35f, 0.85f, 1.0f, 1f);
        Color robeBlack = new Color(0.10f, 0.11f, 0.15f, 1f);
        Color robeGold = new Color(0.95f, 0.78f, 0.25f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = w * 0.5f;
        float headCy = h * 0.58f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - headCy;

                // 1. Pop-art comic halo backing
                float haloR = Mathf.Sqrt(dx * dx + (dy * 0.9f) * (dy * 0.9f));
                if (haloR < 130f)
                {
                    // Halftone pop dots
                    if ((x % 10 < 4) && (y % 10 < 4) && haloR > 70f)
                    {
                        cols[y * w + x] = bgHalo;
                    }
                }

                // 2. High-collar gothic robes (shoulders)
                if (y < headCy - 40f && Mathf.Abs(dx) < 115f - (headCy - 40f - y) * 0.2f)
                {
                    cols[y * w + x] = robeBlack;

                    // Gold collar trim
                    if (Mathf.Abs(dx) < 45f && y > headCy - 80f)
                    {
                        cols[y * w + x] = robeGold;
                    }
                }

                // 3. Neck
                if (y >= headCy - 55f && y <= headCy - 25f && Mathf.Abs(dx) < 22f)
                {
                    cols[y * w + x] = skinColor * 0.92f;
                }

                // 4. Face oval
                float faceDist = Mathf.Sqrt((dx * dx) / (46f * 46f) + (dy * dy) / (56f * 56f));
                if (faceDist <= 1.0f && y <= headCy + 20f)
                {
                    cols[y * w + x] = skinColor;

                    // Eyes
                    if (Mathf.Abs(dy - 4f) < 5f && (Mathf.Abs(dx - 18f) < 9f || Mathf.Abs(dx + 18f) < 9f))
                    {
                        cols[y * w + x] = eyeGlow;
                    }

                    // Blush
                    if (Mathf.Abs(dy + 8f) < 4f && (Mathf.Abs(dx - 24f) < 8f || Mathf.Abs(dx + 24f) < 8f))
                    {
                        cols[y * w + x] = Color.Lerp(skinColor, new Color(1f, 0.5f, 0.6f), 0.5f);
                    }

                    // Lips
                    if (Mathf.Abs(dy + 22f) < 3f && Mathf.Abs(dx) < 8f)
                    {
                        cols[y * w + x] = new Color(0.92f, 0.40f, 0.48f);
                    }
                }

                // 5. Hair (Hime cut with blunt bangs and long side locks)
                // Top hair volume
                float hairDist = Mathf.Sqrt((dx * dx) / (56f * 56f) + ((dy - 12f) * (dy - 12f)) / (58f * 58f));
                if (hairDist <= 1.0f && (y > headCy || Mathf.Abs(dx) > 42f))
                {
                    cols[y * w + x] = Color.Lerp(hairViolet, hairDark, (float)y / h);
                }

                // Blunt bangs across forehead
                if (dy >= 10f && dy <= 30f && Mathf.Abs(dx) < 42f)
                {
                    cols[y * w + x] = hairViolet;
                }

                // Side hair locks framing face
                if (dy >= -50f && dy <= 15f && (Mathf.Abs(dx - 48f) < 14f || Mathf.Abs(dx + 48f) < 14f))
                {
                    cols[y * w + x] = hairViolet;
                }

                // Gold celestial hairpin
                if (Mathf.Abs(dx - 36f) < 8f && Mathf.Abs(dy - 28f) < 6f)
                {
                    cols[y * w + x] = goldPin;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PersonaPortrait_Margo.png");
    }

    private static void GeneratePersonaPortraitMerchant()
    {
        int w = 280;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgHalo = new Color(1.0f, 0.80f, 0.20f, 0.35f);
        Color cloakBurgundy = new Color(0.38f, 0.08f, 0.14f, 1f);
        Color cloakShadow = new Color(0.18f, 0.04f, 0.08f, 1f);
        Color goldEmbroidery = new Color(1.0f, 0.85f, 0.28f, 1f);
        Color monocleCyan = new Color(0.20f, 0.95f, 1.0f, 1f);
        Color eyeGold = new Color(1.0f, 0.92f, 0.35f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = w * 0.5f;
        float cy = h * 0.58f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Gold coin halo
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < 130f && dist > 75f && (x + y) % 12 < 3)
                {
                    cols[y * w + x] = bgHalo;
                }

                // Hood & Cloak
                float hoodDist = Mathf.Sqrt((dx * dx) / (65f * 65f) + ((dy - 8f) * (dy - 8f)) / (72f * 72f));
                if (hoodDist <= 1.0f)
                {
                    cols[y * w + x] = Color.Lerp(cloakBurgundy, cloakShadow, (float)y / h);

                    // Hood shadow interior
                    float innerDist = Mathf.Sqrt((dx * dx) / (45f * 45f) + (dy * dy) / (52f * 52f));
                    if (innerDist <= 1.0f && y < cy + 30f)
                    {
                        cols[y * w + x] = new Color(0.04f, 0.02f, 0.03f, 0.95f);

                        // Glowing Monocle on right eye
                        float monocleDist = Mathf.Sqrt((dx - 16f) * (dx - 16f) + (dy - 6f) * (dy - 6f));
                        if (monocleDist < 12f)
                        {
                            cols[y * w + x] = monocleDist > 9f ? goldEmbroidery : monocleCyan;
                        }

                        // Left piercing eye
                        if (Mathf.Abs(dx + 16f) < 7f && Mathf.Abs(dy - 6f) < 4f)
                        {
                            cols[y * w + x] = eyeGold;
                        }

                        // Knowing smirk
                        if (Mathf.Abs(dy + 20f) < 3f && dx >= -10f && dx <= 12f)
                        {
                            cols[y * w + x] = goldEmbroidery * 0.8f;
                        }
                    }

                    // Gold embroidery along hood hem
                    if (hoodDist > 0.88f && hoodDist <= 1.0f)
                    {
                        cols[y * w + x] = goldEmbroidery;
                    }
                }

                // Heavy draped shoulders
                if (y < cy - 40f && Mathf.Abs(dx) < 125f)
                {
                    cols[y * w + x] = cloakShadow;
                    if (Mathf.Abs(dx) < 6f && y > cy - 90f)
                    {
                        cols[y * w + x] = goldEmbroidery; // Golden clasp
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PersonaPortrait_Merchant.png");
    }

    private static void GeneratePersonaPortraitDemon()
    {
        int w = 280;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color nebulaPurple = new Color(0.55f, 0.15f, 0.85f, 0.4f);
        Color cosmicDark = new Color(0.08f, 0.06f, 0.15f, 1f);
        Color runeCyan = new Color(0.20f, 0.95f, 1.0f, 1f);
        Color hornObsidian = new Color(0.12f, 0.10f, 0.18f, 1f);
        Color eyeGold = new Color(1.0f, 0.85f, 0.20f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = w * 0.5f;
        float cy = h * 0.55f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Cosmic nebula aura
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < 135f && (x * 3 + y * 7) % 15 < 4)
                {
                    cols[y * w + x] = nebulaPurple;
                }

                // Curved Horns curving outward and up
                float hornLeftX = -35f - (dy - 40f) * 0.5f;
                float hornRightX = 35f + (dy - 40f) * 0.5f;
                if (dy > 20f && dy < 120f)
                {
                    if (Mathf.Abs(dx - hornLeftX) < 14f - (dy - 20f) * 0.1f ||
                        Mathf.Abs(dx - hornRightX) < 14f - (dy - 20f) * 0.1f)
                    {
                        cols[y * w + x] = hornObsidian;
                        if ((x + y) % 14 < 3) cols[y * w + x] = runeCyan; // Glowing horn runes
                    }
                }

                // Mask / Face plate
                float faceDist = Mathf.Sqrt((dx * dx) / (45f * 45f) + (dy * dy) / (56f * 56f));
                if (faceDist <= 1.0f)
                {
                    cols[y * w + x] = cosmicDark;

                    // Glowing celestial slit eyes
                    if (Mathf.Abs(dy - 6f) < 4f && (Mathf.Abs(dx - 18f) < 12f || Mathf.Abs(dx + 18f) < 12f))
                    {
                        cols[y * w + x] = eyeGold;
                    }

                    // Forehead Astral Crest
                    float crestDist = Mathf.Abs(dx) + Mathf.Abs(dy - 30f);
                    if (crestDist < 12f)
                    {
                        cols[y * w + x] = runeCyan;
                    }
                }

                // Shoulders
                if (y < cy - 40f && Mathf.Abs(dx) < 110f)
                {
                    cols[y * w + x] = hornObsidian * 0.85f;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PersonaPortrait_Demon.png");
    }
    #endregion

    #region Menu Textures
    private static void GenerateInventoryFrame()
    {
        int w = 660;
        int h = 900;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgSlate = new Color(0.09f, 0.10f, 0.14f, 0.98f);
        Color bgHeader = new Color(0.14f, 0.15f, 0.20f, 1f);
        Color borderGold = new Color(0.92f, 0.78f, 0.30f, 1f);
        Color borderDark = new Color(0.04f, 0.04f, 0.06f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x >= 8 && x <= w - 8 && y >= 8 && y <= h - 8)
                {
                    float dist = Mathf.Min(Mathf.Min(x - 8, (w - 8) - x), Mathf.Min(y - 8, (h - 8) - y));

                    if (dist < 4f)
                    {
                        cols[y * w + x] = borderGold;
                    }
                    else if (dist < 7f)
                    {
                        cols[y * w + x] = borderDark;
                    }
                    else
                    {
                        // Header area
                        if (y > h - 100)
                        {
                            cols[y * w + x] = bgHeader;
                        }
                        else
                        {
                            cols[y * w + x] = bgSlate;
                        }

                        // Ornate corner brackets
                        if ((x < 45 || x > w - 45) && (y < 45 || y > h - 45))
                        {
                            if (dist < 12f && (x % 3 == 0 || y % 3 == 0))
                            {
                                cols[y * w + x] = borderGold;
                            }
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_InventoryFrame.png");
    }

    private static void GenerateItemSlot()
    {
        int w = 96;
        int h = 96;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgDark = new Color(0.12f, 0.13f, 0.18f, 0.95f);
        Color bgCavity = new Color(0.06f, 0.07f, 0.10f, 0.95f);
        Color borderNormal = new Color(0.35f, 0.40f, 0.50f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x >= 4 && x <= w - 4 && y >= 4 && y <= h - 4)
                {
                    float dist = Mathf.Min(Mathf.Min(x - 4, (w - 4) - x), Mathf.Min(y - 4, (h - 4) - y));

                    if (dist < 3f)
                    {
                        cols[y * w + x] = borderNormal;
                    }
                    else if (dist < 5f)
                    {
                        cols[y * w + x] = Color.black;
                    }
                    else
                    {
                        float normY = (float)y / h;
                        cols[y * w + x] = Color.Lerp(bgCavity, bgDark, normY);
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_ItemSlot.png");
    }

    private static void GenerateShopBanner()
    {
        int w = 660;
        int h = 140;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgCanopy = new Color(0.35f, 0.08f, 0.12f, 1f);
        Color bgDark = new Color(0.18f, 0.04f, 0.06f, 1f);
        Color goldTrim = new Color(1.0f, 0.85f, 0.28f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x >= 8 && x <= w - 8 && y >= 8 && y <= h - 8)
                {
                    float dist = Mathf.Min(Mathf.Min(x - 8, (w - 8) - x), Mathf.Min(y - 8, (h - 8) - y));

                    if (dist < 4f)
                    {
                        cols[y * w + x] = goldTrim;
                    }
                    else
                    {
                        // Alternating canopy stripes
                        bool stripe = (x / 40) % 2 == 0;
                        cols[y * w + x] = stripe ? bgCanopy : bgDark;

                        // Bottom scalloped trim
                        if (y < 22)
                        {
                            cols[y * w + x] = goldTrim;
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_ShopBanner.png");
    }

    private static void GenerateSettingsPanel()
    {
        int w = 660;
        int h = 900;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgSlate = new Color(0.08f, 0.09f, 0.13f, 0.98f);
        Color borderCyan = new Color(0.20f, 0.85f, 1.0f, 1f);
        Color borderDark = new Color(0.03f, 0.03f, 0.05f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x >= 8 && x <= w - 8 && y >= 8 && y <= h - 8)
                {
                    float dist = Mathf.Min(Mathf.Min(x - 8, (w - 8) - x), Mathf.Min(y - 8, (h - 8) - y));

                    if (dist < 4f)
                    {
                        cols[y * w + x] = borderCyan;
                    }
                    else if (dist < 7f)
                    {
                        cols[y * w + x] = borderDark;
                    }
                    else
                    {
                        cols[y * w + x] = bgSlate;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SettingsPanel.png");
    }

    private static void GenerateSummoningAltar()
    {
        int w = 660;
        int h = 900;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgVoid = new Color(0.05f, 0.05f, 0.09f, 0.98f);
        Color goldRing = new Color(1.0f, 0.84f, 0.28f, 0.85f);
        Color cyanGlow = new Color(0.20f, 0.92f, 1.0f, 0.75f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = w * 0.5f;
        float cy = h * 0.55f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x >= 8 && x <= w - 8 && y >= 8 && y <= h - 8)
                {
                    cols[y * w + x] = bgVoid;

                    // Concentric magical circles on the altar canvas
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (Mathf.Abs(dist - 240f) < 3.5f || Mathf.Abs(dist - 190f) < 2.5f || Mathf.Abs(dist - 140f) < 2.5f)
                    {
                        cols[y * w + x] = goldRing;
                    }

                    // 5-Pointed Astral Star lines
                    for (int pt = 0; pt < 5; pt++)
                    {
                        float ang = (pt * 72f) * Mathf.Deg2Rad;
                        float px = cx + Mathf.Sin(ang) * 190f;
                        float py = cy + Mathf.Cos(ang) * 190f;
                        float dNode = Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
                        if (dNode < 12f)
                        {
                            cols[y * w + x] = cyanGlow;
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SummoningAltar.png");
    }

    #region Tomodachi & Social Link Textures
    private static void GenerateSocialHubFrame()
    {
        int w = 680;
        int h = 940;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgObsidian = new Color(0.06f, 0.07f, 0.10f, 0.98f);
        Color bgInner = new Color(0.10f, 0.11f, 0.15f, 0.98f);
        Color goldTrim = new Color(1.0f, 0.84f, 0.28f, 1f);
        Color crimsonTrim = new Color(0.92f, 0.22f, 0.35f, 1f);
        Color cyanAccent = new Color(0.25f, 0.88f, 1.0f, 0.85f);
        Color hatchColor = new Color(0.16f, 0.18f, 0.24f, 0.7f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                if (x >= 12 && x <= w - 12 && y >= 12 && y <= h - 12)
                {
                    float distLeft = x - 12;
                    float distRight = (w - 12) - x;
                    float distBottom = y - 12;
                    float distTop = (h - 12) - y;
                    float minDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

                    if (minDist < 4f)
                    {
                        // Outer neon border: Crimson on bottom/left, Gold on top/right
                        cols[y * w + x] = Color.Lerp(crimsonTrim, goldTrim, (normY + (float)x / w) * 0.5f);
                    }
                    else if (minDist < 7f)
                    {
                        cols[y * w + x] = new Color(0.02f, 0.02f, 0.03f, 1f);
                    }
                    else if (minDist < 9f)
                    {
                        cols[y * w + x] = cyanAccent;
                    }
                    else
                    {
                        // Background fill
                        Color baseCol = Color.Lerp(bgObsidian, bgInner, normY);

                        // Persona diagonal hatch stripes on top-right and bottom-left
                        if ((x + y) % 24 < 3 && ((x > w * 0.7f && y > h * 0.7f) || (x < w * 0.3f && y < h * 0.3f)))
                        {
                            baseCol = Color.Lerp(baseCol, hatchColor, 0.5f);
                        }

                        // Header separator bar groove at y = h - 120
                        if (Mathf.Abs(y - (h - 120)) < 3)
                        {
                            baseCol = Color.Lerp(baseCol, goldTrim, 0.7f);
                        }

                        cols[y * w + x] = baseCol;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialHubFrame.png");
    }

    private static void GenerateSocialArcanaCard()
    {
        int w = 240;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgVoid = new Color(0.08f, 0.06f, 0.14f, 1f);
        Color bgCenter = new Color(0.14f, 0.10f, 0.24f, 1f);
        Color goldFiligree = new Color(1.0f, 0.85f, 0.30f, 1f);
        Color cyanStar = new Color(0.35f, 0.90f, 1.0f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = w * 0.5f;
        float cy = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                if (x >= 8 && x <= w - 8 && y >= 8 && y <= h - 8)
                {
                    float distLeft = x - 8;
                    float distRight = (w - 8) - x;
                    float distBottom = y - 8;
                    float distTop = (h - 8) - y;
                    float minDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

                    if (minDist < 3.5f)
                    {
                        cols[y * w + x] = goldFiligree;
                    }
                    else if (minDist < 6.5f)
                    {
                        cols[y * w + x] = new Color(0.04f, 0.03f, 0.07f, 1f);
                    }
                    else if (minDist < 8.5f)
                    {
                        cols[y * w + x] = Color.Lerp(goldFiligree, cyanStar, 0.5f);
                    }
                    else
                    {
                        float distCenter = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        Color baseCol = Color.Lerp(bgCenter, bgVoid, distCenter / (w * 0.7f));

                        // Concentric Tarot rings
                        if (Mathf.Abs(distCenter - 75f) < 2f || Mathf.Abs(distCenter - 55f) < 1.5f)
                        {
                            baseCol = Color.Lerp(baseCol, goldFiligree, 0.8f);
                        }

                        // 8-Pointed Celestial Star
                        float dx = x - cx;
                        float dy = y - cy;
                        float angle = Mathf.Atan2(dy, dx);
                        float starRadius = 38f + 16f * Mathf.Cos(angle * 8f);
                        if (distCenter < starRadius)
                        {
                            baseCol = Color.Lerp(cyanStar, goldFiligree, (distCenter / starRadius));
                        }

                        // Diamond corner accents
                        if ((x < 35 && y < 35) || (x > w - 35 && y < 35) || (x < 35 && y > h - 35) || (x > w - 35 && y > h - 35))
                        {
                            if ((x + y) % 4 == 0) baseCol = goldFiligree;
                        }

                        cols[y * w + x] = baseCol;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialArcanaCard.png");
    }

    private static void GenerateSocialHeartIcon()
    {
        int w = 128;
        int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color heartRed = new Color(1.0f, 0.18f, 0.38f, 1f);
        Color heartHighlight = new Color(1.0f, 0.65f, 0.80f, 1f);
        Color goldRim = new Color(1.0f, 0.88f, 0.35f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        for (int y = 0; y < h; y++)
        {
            float ny = (y - 52f) / 44f;
            for (int x = 0; x < w; x++)
            {
                float nx = (x - 64f) / 44f;

                // Cardioid / Heart implicit equation: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                float f = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - (nx * nx * ny * ny * ny);

                if (f <= 0.08f)
                {
                    if (f > -0.05f)
                    {
                        cols[y * w + x] = goldRim;
                    }
                    else
                    {
                        // Specular shine on upper left lobe
                        float distSpecular = Mathf.Sqrt((x - 48f) * (x - 48f) + (y - 78f) * (y - 78f));
                        if (distSpecular < 14f)
                        {
                            cols[y * w + x] = Color.Lerp(heartHighlight, heartRed, distSpecular / 14f);
                        }
                        else
                        {
                            cols[y * w + x] = heartRed;
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialHeartIcon.png");
    }

    private static void GenerateSocialBrokenHeart()
    {
        int w = 128;
        int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color heartDarkCrimson = new Color(0.65f, 0.08f, 0.18f, 1f);
        Color fractureColor = new Color(0.95f, 0.80f, 0.20f, 1f); // Electric spark in crack
        Color shadowRim = new Color(0.20f, 0.02f, 0.06f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        for (int y = 0; y < h; y++)
        {
            float ny = (y - 52f) / 44f;
            for (int x = 0; x < w; x++)
            {
                float nx = (x - 64f) / 44f;
                float f = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - (nx * nx * ny * ny * ny);

                if (f <= 0.08f)
                {
                    // Lightning crack path through center: x = 64 + 6*sin(y*0.35)
                    float crackX = 64f + 8f * Mathf.Sin(y * 0.28f);
                    float distCrack = Mathf.Abs(x - crackX);

                    if (distCrack < 2.5f)
                    {
                        cols[y * w + x] = fractureColor;
                    }
                    else if (distCrack < 4.5f)
                    {
                        cols[y * w + x] = clear; // Physical gap split
                    }
                    else
                    {
                        cols[y * w + x] = (f > -0.05f) ? shadowRim : heartDarkCrimson;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialBrokenHeart.png");
    }

    private static void GenerateSocialAngerMark()
    {
        int w = 128;
        int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color angerRed = new Color(0.95f, 0.12f, 0.18f, 1f);
        Color angerHighlight = new Color(1.0f, 0.50f, 0.55f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = 64f;
        float cy = 64f;

        // 4 curved cross marks (classic anime vein pop)
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx);
                float dy = Mathf.Abs(y - cy);

                // Two intersecting arcs forming brackets
                bool onHorizArc = (dx >= 12f && dx <= 38f) && Mathf.Abs(dy - (dx * 0.45f)) < 4.5f;
                bool onVertArc = (dy >= 12f && dy <= 38f) && Mathf.Abs(dx - (dy * 0.45f)) < 4.5f;

                if (onHorizArc || onVertArc)
                {
                    cols[y * w + x] = (dx < 20f || dy < 20f) ? angerHighlight : angerRed;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialAngerMark.png");
    }

    private static void GenerateSocialLinkHudBtn()
    {
        int w = 128;
        int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgDark = new Color(0.10f, 0.11f, 0.16f, 0.95f);
        Color goldRing = new Color(1.0f, 0.85f, 0.28f, 1f);
        Color heartPink = new Color(1.0f, 0.25f, 0.50f, 1f);
        Color chainSilver = new Color(0.85f, 0.90f, 0.98f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = 64f;
        float cy = 64f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (dist <= 58f)
                {
                    if (dist >= 52f)
                    {
                        cols[y * w + x] = goldRing;
                    }
                    else if (dist >= 48f)
                    {
                        cols[y * w + x] = new Color(0.04f, 0.04f, 0.06f, 1f);
                    }
                    else
                    {
                        cols[y * w + x] = bgDark;

                        // Center mini heart
                        float nx = (x - 64f) / 22f;
                        float ny = (y - 60f) / 22f;
                        float f = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - (nx * nx * ny * ny * ny);
                        if (f <= 0f)
                        {
                            cols[y * w + x] = heartPink;
                        }

                        // Interlocking chain links across heart
                        if ((Mathf.Abs(x - 42f) < 3f || Mathf.Abs(x - 86f) < 3f) && Mathf.Abs(y - 64f) < 14f)
                        {
                            cols[y * w + x] = chainSilver;
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialLinkHudBtn.png");
    }

    private static void GenerateSocialPortraitCorvus()
    {
        int w = 360;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color mysticViolet = new Color(0.20f, 0.08f, 0.35f, 1f);
        Color obsidianPlumage = new Color(0.08f, 0.08f, 0.12f, 1f);
        Color ravenHighlight = new Color(0.25f, 0.30f, 0.45f, 1f);
        Color amberEye = new Color(1.0f, 0.85f, 0.15f, 1f);
        Color runeCyan = new Color(0.25f, 0.90f, 1.0f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = 180f;
        float cy = 180f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Silhouette of sleek Arcane Raven bust
                float dx = x - cx;
                float dy = y - cy;

                // Hood/Cape of raven feathers
                if (dy < 60f && dy > -150f && Mathf.Abs(dx) < (150f - dy * 0.4f))
                {
                    cols[y * w + x] = mysticViolet;

                    // Feather ridges
                    if ((x * 3 + y * 2) % 18 < 3)
                    {
                        cols[y * w + x] = ravenHighlight;
                    }
                }

                // Raven head & beak
                float dHead = Mathf.Sqrt(dx * dx + (dy - 40f) * (dy - 40f));
                if (dHead < 65f)
                {
                    cols[y * w + x] = obsidianPlumage;
                }

                // Sharp curved beak pointing left
                if (x < cx - 25f && x > cx - 110f && y > cy + 15f && y < cy + 55f)
                {
                    float beakSlope = (cx - 25f - x) * 0.35f;
                    if (Mathf.Abs(y - (cy + 35f)) < (20f - beakSlope * 0.3f))
                    {
                        cols[y * w + x] = new Color(0.15f, 0.15f, 0.20f, 1f);
                        if (x > cx - 80f && Mathf.Abs(y - (cy + 35f)) < 2f)
                        {
                            cols[y * w + x] = runeCyan; // Glowing rune on beak
                        }
                    }
                }

                // Piercing glowing amber eye
                float dEye = Mathf.Sqrt((x - (cx - 18f)) * (x - (cx - 18f)) + (y - (cy + 48f)) * (y - (cy + 48f)));
                if (dEye < 10f)
                {
                    cols[y * w + x] = amberEye;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialPortrait_Corvus.png");
    }

    private static void GenerateSocialPortraitIgnis()
    {
        int w = 360;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color salamanderRed = new Color(0.88f, 0.22f, 0.12f, 1f);
        Color emberOrange = new Color(1.0f, 0.58f, 0.15f, 1f);
        Color flameGold = new Color(1.0f, 0.88f, 0.25f, 1f);
        Color emeraldEye = new Color(0.20f, 0.95f, 0.45f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = 180f;
        float cy = 170f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Salamander / Fire Drake body
                if (dy < 40f && dy > -150f && Mathf.Abs(dx) < (140f - dy * 0.35f))
                {
                    cols[y * w + x] = salamanderRed;
                    if ((x * 2 + y) % 22 < 4) cols[y * w + x] = emberOrange;
                }

                // Flame frills on sides of head
                if (Mathf.Abs(dx) > 60f && Mathf.Abs(dx) < 140f && y > cy + 10f && y < cy + 120f)
                {
                    float flameWave = Mathf.Sin((x + y) * 0.15f) * 15f;
                    if (y < cy + 80f + flameWave)
                    {
                        cols[y * w + x] = Color.Lerp(emberOrange, flameGold, (float)(y - cy) / 120f);
                    }
                }

                // Rounded drake head
                float dHead = Mathf.Sqrt(dx * dx + (dy - 40f) * (dy - 40f));
                if (dHead < 70f)
                {
                    cols[y * w + x] = salamanderRed;
                }

                // Expressive Emerald Eyes
                float dEyeL = Mathf.Sqrt((x - (cx - 32f)) * (x - (cx - 32f)) + (y - (cy + 55f)) * (y - (cy + 55f)));
                float dEyeR = Mathf.Sqrt((x - (cx + 32f)) * (x - (cx + 32f)) + (y - (cy + 55f)) * (y - (cy + 55f)));
                if (dEyeL < 11f || dEyeR < 11f)
                {
                    cols[y * w + x] = emeraldEye;
                }

                // Golden crest crown
                if (Mathf.Abs(dx) < 25f && y > cy + 95f && y < cy + 140f)
                {
                    cols[y * w + x] = flameGold;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialPortrait_Ignis.png");
    }

    private static void GenerateSocialPortraitTalos()
    {
        int w = 360;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color brassPlate = new Color(0.72f, 0.52f, 0.25f, 1f);
        Color ironFrame = new Color(0.25f, 0.28f, 0.35f, 1f);
        Color cyanOptics = new Color(0.20f, 0.95f, 1.0f, 1f);
        Color goldRivet = new Color(1.0f, 0.85f, 0.35f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = 180f;
        float cy = 180f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Armored clockwork shoulders
                if (dy < 20f && dy > -150f && Mathf.Abs(dx) < (160f - dy * 0.25f))
                {
                    cols[y * w + x] = ironFrame;
                    if (Mathf.Abs(dx) > 90f) cols[y * w + x] = brassPlate;
                }

                // Angular automaton helm
                if (Mathf.Abs(dx) < 65f && dy >= -20f && dy <= 100f)
                {
                    cols[y * w + x] = brassPlate;

                    // Beveled plate border
                    if (Mathf.Abs(dx) > 58f || dy < -12f || dy > 92f)
                    {
                        cols[y * w + x] = ironFrame;
                    }
                }

                // Visor slit with cyan glowing optics
                if (Mathf.Abs(dx) < 48f && dy > 35f && dy < 55f)
                {
                    cols[y * w + x] = cyanOptics;
                }

                // Cogwheel crest on forehead
                float dCog = Mathf.Sqrt(dx * dx + (dy - 120f) * (dy - 120f));
                if (dCog < 32f)
                {
                    int teeth = (int)(Mathf.Atan2(dy - 120f, dx) * Mathf.Rad2Deg + 360f) % 45;
                    cols[y * w + x] = (teeth < 24) ? goldRivet : brassPlate;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialPortrait_Talos.png");
    }

    private static void GenerateSocialPortraitAegis()
    {
        int w = 360;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color platinumSilver = new Color(0.88f, 0.92f, 0.98f, 1f);
        Color celestialCyan = new Color(0.35f, 0.85f, 1.0f, 1f);
        Color goldHalo = new Color(1.0f, 0.88f, 0.35f, 1f);
        Color midnightNavy = new Color(0.10f, 0.12f, 0.22f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = 180f;
        float cy = 170f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Halo ring behind head
                float dHalo = Mathf.Sqrt(dx * dx + (dy - 70f) * (dy - 70f));
                if (Mathf.Abs(dHalo - 75f) < 4f)
                {
                    cols[y * w + x] = goldHalo;
                }

                // Celestial Wings extending outwards
                if (Mathf.Abs(dx) > 55f && Mathf.Abs(dx) < 170f && dy > -30f && dy < 130f)
                {
                    float wingCurve = Mathf.Abs(dx) * 0.6f;
                    if (dy < wingCurve + 30f && dy > wingCurve - 40f)
                    {
                        cols[y * w + x] = Color.Lerp(celestialCyan, platinumSilver, (float)(y - cy) / 120f);
                    }
                }

                // Shoulders & Torso armor
                if (dy < 20f && dy > -150f && Mathf.Abs(dx) < (145f - dy * 0.3f))
                {
                    cols[y * w + x] = midnightNavy;
                    if (Mathf.Abs(dx) < 55f) cols[y * w + x] = platinumSilver;
                }

                // Knight helm & visor
                float dHelm = Mathf.Sqrt(dx * dx + (dy - 45f) * (dy - 45f));
                if (dHelm < 60f)
                {
                    cols[y * w + x] = platinumSilver;

                    // Starlight cross visor
                    if ((Mathf.Abs(dx) < 30f && Mathf.Abs(dy - 45f) < 6f) || (Mathf.Abs(dx) < 6f && Mathf.Abs(dy - 45f) < 22f))
                    {
                        cols[y * w + x] = celestialCyan;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialPortrait_Aegis.png");
    }
    #endregion
    #endregion

    #region Character Customization Textures
    private static void GenerateCustomizationFrame()
    {
        int w = 512;
        int h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgObsidian = new Color(0.06f, 0.07f, 0.10f, 0.94f);
        Color goldBorder = new Color(1.0f, 0.84f, 0.32f, 1f);
        Color cyanAccent = new Color(0.25f, 0.85f, 0.95f, 0.85f);
        Color gridLine = new Color(0.12f, 0.15f, 0.22f, 0.4f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x < 12 || x > w - 13 || y < 12 || y > h - 13)
                {
                    cols[y * w + x] = clear;
                    continue;
                }

                float distBorder = Mathf.Min(Mathf.Min(x - 12, (w - 13) - x), Mathf.Min(y - 12, (h - 13) - y));
                if (distBorder < 4f)
                {
                    cols[y * w + x] = goldBorder;
                }
                else if (distBorder < 7f)
                {
                    cols[y * w + x] = cyanAccent;
                }
                else
                {
                    // Diagonal cyber hash pattern
                    if ((x + y) % 32 == 0) cols[y * w + x] = gridLine;
                    else cols[y * w + x] = bgObsidian;
                }

                // Corner decorative brackets
                bool cornerTL = (x < 42 && y > h - 42) && (x < 18 || y > h - 18);
                bool cornerTR = (x > w - 42 && y > h - 42) && (x > w - 18 || y > h - 18);
                bool cornerBL = (x < 42 && y < 42) && (x < 18 || y < 18);
                bool cornerBR = (x > w - 42 && y < 42) && (x > w - 18 || y < 18);
                if (cornerTL || cornerTR || cornerBL || cornerBR)
                {
                    cols[y * w + x] = goldBorder;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationFrame.png");
    }

    private static void GenerateCustomizationTabBtn()
    {
        int w = 256;
        int h = 80;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgDark = new Color(0.10f, 0.12f, 0.18f, 0.95f);
        Color goldLine = new Color(1.0f, 0.85f, 0.35f, 1f);
        Color borderDark = new Color(0.20f, 0.24f, 0.35f, 1f);

        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                // Chamfered top corners
                if ((x < 16 && y > h - 16 && (16 - x) + (y - (h - 16)) > 16) ||
                    (x > w - 16 && y > h - 16 && (x - (w - 16)) + (y - (h - 16)) > 16))
                {
                    cols[y * w + x] = clear;
                    continue;
                }

                if (y < 4)
                {
                    cols[y * w + x] = goldLine; // Glowing active indicator along base
                }
                else if (x < 3 || x >= w - 3 || y >= h - 3)
                {
                    cols[y * w + x] = borderDark;
                }
                else
                {
                    cols[y * w + x] = Color.Lerp(bgDark, new Color(0.16f, 0.19f, 0.28f, 0.95f), normY);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationTabBtn.png");
    }

    private static void GenerateCustomizationTurntableRing()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color gold = new Color(1.0f, 0.84f, 0.30f, 0.9f);
        Color cyan = new Color(0.30f, 0.88f, 1.0f, 0.75f);
        Color disc = new Color(0.08f, 0.10f, 0.15f, 0.65f);

        float center = size * 0.5f;
        float radiusOuter = size * 0.46f;
        float radiusInner = size * 0.38f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > radiusOuter)
                {
                    cols[y * size + x] = clear;
                }
                else if (d >= radiusInner)
                {
                    // Ring area
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // Tick marks every 30 degrees
                    bool isMajorTick = (Mathf.Abs(angle % 30f) < 2.5f || Mathf.Abs(angle % 30f) > 27.5f);
                    bool isOuterRim = (d > radiusOuter - 3f);
                    bool isInnerRim = (d < radiusInner + 3f);

                    if (isOuterRim || isInnerRim)
                    {
                        cols[y * size + x] = gold;
                    }
                    else if (isMajorTick)
                    {
                        cols[y * size + x] = cyan;
                    }
                    else
                    {
                        cols[y * size + x] = disc;
                    }
                }
                else
                {
                    cols[y * size + x] = clear;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationTurntableRing.png");
    }

    private static void GenerateCustomizationSliderTrack()
    {
        int w = 256;
        int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color border = new Color(0.35f, 0.42f, 0.55f, 1f);
        Color trackDark = new Color(0.06f, 0.08f, 0.12f, 0.9f);
        Color accentGlow = new Color(0.20f, 0.75f, 0.90f, 0.8f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Rounded ends
                if (x < 16)
                {
                    float d = Mathf.Sqrt((x - 16) * (x - 16) + (y - 16) * (y - 16));
                    if (d > 14f) { cols[y * w + x] = clear; continue; }
                    if (d > 11f) { cols[y * w + x] = border; continue; }
                }
                else if (x > w - 16)
                {
                    float d = Mathf.Sqrt((x - (w - 16)) * (x - (w - 16)) + (y - 16) * (y - 16));
                    if (d > 14f) { cols[y * w + x] = clear; continue; }
                    if (d > 11f) { cols[y * w + x] = border; continue; }
                }
                else
                {
                    if (y < 2 || y > h - 3) { cols[y * w + x] = border; continue; }
                }

                // Inner slot
                if (y >= 13 && y <= 18)
                {
                    cols[y * w + x] = accentGlow;
                }
                else
                {
                    cols[y * w + x] = trackDark;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationSliderTrack.png");
    }

    private static void GenerateCustomizationSliderThumb()
    {
        int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color gold = new Color(1.0f, 0.85f, 0.30f, 1f);
        Color brightGold = new Color(1.0f, 0.96f, 0.65f, 1f);
        Color deepGold = new Color(0.70f, 0.50f, 0.12f, 1f);

        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);

                // Faceted diamond shape: |dx| + |dy| <= radius
                if (dx + dy <= 20f)
                {
                    if (dx + dy <= 6f)
                    {
                        cols[y * size + x] = brightGold;
                    }
                    else if (y > center)
                    {
                        cols[y * size + x] = gold;
                    }
                    else
                    {
                        cols[y * size + x] = deepGold;
                    }
                }
                else
                {
                    cols[y * size + x] = clear;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationSliderThumb.png");
    }

    private static void GenerateCustomizationColorSwatch()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color silverRing = new Color(0.85f, 0.90f, 0.96f, 1f);
        Color goldActive = new Color(1.0f, 0.85f, 0.30f, 1f);
        Color innerFill = Color.white; // Will be tinted by UI Image color!

        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > 28f)
                {
                    cols[y * size + x] = clear;
                }
                else if (d > 24f)
                {
                    cols[y * size + x] = goldActive;
                }
                else if (d > 21f)
                {
                    cols[y * size + x] = silverRing;
                }
                else
                {
                    // Gradient highlight inside swatch
                    float highlight = Mathf.Clamp01(1.0f - (d / 22f));
                    cols[y * size + x] = Color.Lerp(innerFill, Color.white, highlight * 0.35f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationColorSwatch.png");
    }

    private static void GenerateCustomizationRandomBtn()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color purpleBg = new Color(0.35f, 0.12f, 0.55f, 0.95f);
        Color goldRing = new Color(1.0f, 0.85f, 0.35f, 1f);
        Color starWhite = Color.white;

        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > 58f)
                {
                    cols[y * size + x] = clear;
                }
                else if (d > 52f)
                {
                    cols[y * size + x] = goldRing;
                }
                else
                {
                    // 4-point celestial star in center
                    float adx = Mathf.Abs(dx);
                    float ady = Mathf.Abs(dy);
                    bool inStar = (adx < 5f && ady < 30f) || (ady < 5f && adx < 30f) ||
                                 (adx + ady < 18f);

                    if (inStar)
                    {
                        cols[y * size + x] = starWhite;
                    }
                    else
                    {
                        cols[y * size + x] = purpleBg;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationRandomBtn.png");
    }

    private static void GenerateCustomizationResetBtn()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color cyanRing = new Color(0.25f, 0.82f, 0.95f, 1f);
        Color darkBg = new Color(0.08f, 0.12f, 0.18f, 0.95f);

        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > 58f)
                {
                    cols[y * size + x] = clear;
                }
                else if (d > 52f)
                {
                    cols[y * size + x] = cyanRing;
                }
                else
                {
                    // Circular rewind arrow arc
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    bool inArc = (d >= 28f && d <= 42f && angle > 60f && angle < 320f);
                    // Arrowhead
                    bool inArrow = (dx > 12f && dx < 36f && dy > 18f && dy < 44f && (dx - dy) < 6f);

                    if (inArc || inArrow)
                    {
                        cols[y * size + x] = cyanRing;
                    }
                    else
                    {
                        cols[y * size + x] = darkBg;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationResetBtn.png");
    }

    private static void GenerateCustomizationSaveBtn()
    {
        int w = 256;
        int h = 96;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color goldBg = new Color(0.95f, 0.76f, 0.22f, 1f);
        Color goldBorder = new Color(1.0f, 0.92f, 0.60f, 1f);
        Color darkCharcoal = new Color(0.08f, 0.08f, 0.10f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Chamfered ends
                if (x < 16 && (16 - x) + Mathf.Abs(y - 48) > 42) { cols[y * w + x] = clear; continue; }
                if (x > w - 16 && (x - (w - 16)) + Mathf.Abs(y - 48) > 42) { cols[y * w + x] = clear; continue; }

                if (x < 4 || x > w - 5 || y < 4 || y > h - 5)
                {
                    cols[y * w + x] = goldBorder;
                }
                else
                {
                    // Checkmark glyph near left side
                    float cx = x - 48f;
                    float cy = y - 48f;
                    bool inCheck = (cx >= -14f && cx <= -2f && cy >= -12f && cy <= 0f && Mathf.Abs(cx + cy + 8f) < 4f) ||
                                  (cx >= -4f && cx <= 16f && cy >= -4f && cy <= 18f && Mathf.Abs(cx - cy + 2f) < 4f);

                    if (inCheck)
                    {
                        cols[y * w + x] = darkCharcoal;
                    }
                    else
                    {
                        cols[y * w + x] = goldBg;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizationSaveBtn.png");
    }

    private static void GenerateCustomizeHudBtn()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color goldBorder = new Color(1.0f, 0.84f, 0.28f, 1f);
        Color bgObsidian = new Color(0.08f, 0.10f, 0.16f, 0.95f);
        Color avatarCyan = new Color(0.30f, 0.88f, 1.0f, 1f);

        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > 58f)
                {
                    cols[y * size + x] = clear;
                }
                else if (d > 52f)
                {
                    cols[y * size + x] = goldBorder;
                }
                else if (d > 48f)
                {
                    cols[y * size + x] = new Color(0.20f, 0.15f, 0.05f, 1f);
                }
                else
                {
                    // Stylized humanoid silhouette bust inside mirror
                    float dHead = Mathf.Sqrt(dx * dx + (dy - 14f) * (dy - 14f));
                    bool inHead = (dHead < 14f);
                    bool inShoulders = (dy < 6f && dy > -24f && Mathf.Abs(dx) < (28f - dy * 0.45f));

                    if (inHead || inShoulders)
                    {
                        cols[y * size + x] = avatarCyan;
                    }
                    else
                    {
                        cols[y * size + x] = bgObsidian;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_CustomizeHudBtn.png");
    }
    #endregion

    #region Famigotchi / Tamagotchi Pet Familiar Care Textures
    private static void GenerateSocialPortraitPygmy()
    {
        int w = 360;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color bgAura = new Color(0.12f, 0.10f, 0.22f, 1f);
        Color skinColor = new Color(1.0f, 0.94f, 0.90f, 1f);
        Color blushPink = new Color(1.0f, 0.50f, 0.65f, 0.9f);
        Color fairyPinkWing = new Color(1.0f, 0.65f, 0.85f, 0.75f);
        Color fairyCyanWing = new Color(0.40f, 0.90f, 1.0f, 0.75f);
        Color starGold = new Color(1.0f, 0.88f, 0.25f, 1f);
        Color eyeViolet = new Color(0.35f, 0.20f, 0.60f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = 180f;
        float cy = 175f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Circular frame border
                if (dist <= 170f)
                {
                    cols[y * w + x] = bgAura;

                    // Gossamer fairy wings
                    if (Mathf.Abs(dx) > 30f && Mathf.Abs(dx) < 165f && dy > -30f && dy < 140f)
                    {
                        float wingCurveTop = Mathf.Sin(Mathf.Abs(dx) * 0.025f) * 80f + 30f;
                        float wingCurveBottom = -Mathf.Abs(dx) * 0.35f - 10f;
                        if (dy < wingCurveTop && dy > wingCurveBottom)
                        {
                            cols[y * w + x] = Color.Lerp(fairyCyanWing, fairyPinkWing, (float)x / w);
                        }
                    }

                    // Tiny body / robe
                    if (dy < -20f && dy > -130f && Mathf.Abs(dx) < (35f - dy * 0.35f))
                    {
                        cols[y * w + x] = new Color(0.92f, 0.95f, 1.0f, 1f);
                    }

                    // Round sprite head
                    float headDist = Mathf.Sqrt(dx * dx + (dy - 10f) * (dy - 10f));
                    if (headDist < 60f)
                    {
                        cols[y * w + x] = skinColor;

                        // Rosy blush cheeks
                        float blushL = Mathf.Sqrt((dx + 30f) * (dx + 30f) + (dy - 2f) * (dy - 2f));
                        float blushR = Mathf.Sqrt((dx - 30f) * (dx - 30f) + (dy - 2f) * (dy - 2f));
                        if (blushL < 14f || blushR < 14f)
                        {
                            cols[y * w + x] = Color.Lerp(skinColor, blushPink, 0.6f);
                        }

                        // Big starry eyes
                        float eyeL = Mathf.Sqrt((dx + 22f) * (dx + 22f) + (dy - 16f) * (dy - 16f) * 0.7f);
                        float eyeR = Mathf.Sqrt((dx - 22f) * (dx - 22f) + (dy - 16f) * (dy - 16f) * 0.7f);
                        if (eyeL < 14f || eyeR < 14f)
                        {
                            cols[y * w + x] = eyeViolet;

                            // White eye sparkle glints
                            float glintL1 = Mathf.Sqrt((dx + 25f) * (dx + 25f) + (dy - 20f) * (dy - 20f));
                            float glintR1 = Mathf.Sqrt((dx - 19f) * (dx - 19f) + (dy - 20f) * (dy - 20f));
                            if (glintL1 < 4f || glintR1 < 4f) cols[y * w + x] = Color.white;
                        }

                        // Smiling mouth
                        if (Mathf.Abs(dx) < 10f && dy >= -10f && dy <= -4f)
                        {
                            float mouthCurve = -8f + (dx * dx) * 0.04f;
                            if (Mathf.Abs(dy - mouthCurve) < 2f)
                            {
                                cols[y * w + x] = new Color(0.85f, 0.25f, 0.40f, 1f);
                            }
                        }
                    }

                    // Golden Star Tiara on forehead
                    float starDist = Mathf.Sqrt(dx * dx + (dy - 85f) * (dy - 85f));
                    if (starDist < 26f)
                    {
                        float angle = Mathf.Atan2(dy - 85f, dx);
                        float r = 16f + 10f * Mathf.Cos(5f * angle);
                        if (starDist < r) cols[y * w + x] = starGold;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialPortrait_Pygmy.png");
    }

    private static void GenerateSocialPortraitAqua()
    {
        int w = 360;
        int h = 360;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        Color clear = Color.clear;
        Color marineBg = new Color(0.06f, 0.12f, 0.24f, 1f);
        Color waterAqua = new Color(0.20f, 0.85f, 1.0f, 0.9f);
        Color skinColor = new Color(0.95f, 0.96f, 1.0f, 1f);
        Color deepOcean = new Color(0.05f, 0.35f, 0.70f, 1f);
        Color pearlWhite = new Color(0.95f, 0.98f, 1.0f, 1f);

        for (int i = 0; i < cols.Length; i++) cols[i] = clear;

        float cx = 180f;
        float cy = 175f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= 170f)
                {
                    cols[y * w + x] = marineBg;

                    // Swirling flowing water hair
                    if (dist > 50f && dist < 155f)
                    {
                        float swirl = Mathf.Sin((dist * 0.1f) + Mathf.Atan2(dy, dx) * 4f);
                        if (swirl > 0.3f)
                        {
                            cols[y * w + x] = Color.Lerp(deepOcean, waterAqua, swirl);
                        }
                    }

                    // Floating bubbles around her
                    float b1 = Mathf.Sqrt((dx - 100f) * (dx - 100f) + (dy - 90f) * (dy - 90f));
                    float b2 = Mathf.Sqrt((dx + 110f) * (dx + 110f) + (dy - 70f) * (dy - 70f));
                    float b3 = Mathf.Sqrt((dx - 80f) * (dx - 80f) + (dy + 80f) * (dy + 80f));
                    if ((b1 < 16f && b1 > 11f) || (b2 < 20f && b2 > 15f) || (b3 < 14f && b3 > 10f))
                    {
                        cols[y * w + x] = pearlWhite;
                    }

                    // Nymph face & head
                    float headDist = Mathf.Sqrt(dx * dx + (dy - 10f) * (dy - 10f));
                    if (headDist < 58f)
                    {
                        cols[y * w + x] = skinColor;

                        // Deep aquatic eyes
                        float eyeL = Mathf.Sqrt((dx + 20f) * (dx + 20f) + (dy - 14f) * (dy - 14f) * 0.8f);
                        float eyeR = Mathf.Sqrt((dx - 20f) * (dx - 20f) + (dy - 14f) * (dy - 14f) * 0.8f);
                        if (eyeL < 12f || eyeR < 12f)
                        {
                            cols[y * w + x] = deepOcean;
                            float glint = Mathf.Sqrt((dx + 22f) * (dx + 22f) + (dy - 17f) * (dy - 17f));
                            if (glint < 4f) cols[y * w + x] = Color.white;
                        }

                        // Gentle smile
                        if (Mathf.Abs(dx) < 12f && dy >= -10f && dy <= -5f)
                        {
                            cols[y * w + x] = new Color(0.20f, 0.60f, 0.85f, 1f);
                        }
                    }

                    // Pearl headpiece crest
                    float crest = Mathf.Sqrt(dx * dx + (dy - 74f) * (dy - 74f));
                    if (crest < 14f)
                    {
                        cols[y * w + x] = pearlWhite;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_SocialPortrait_Aqua.png");
    }

    private static void GeneratePetFoodChocoDrop()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color chocoDark = new Color(0.24f, 0.12f, 0.05f, 1f);
        Color chocoLight = new Color(0.48f, 0.25f, 0.12f, 1f);
        Color flameRed = new Color(0.95f, 0.35f, 0.10f, 1f);
        Color flameGold = new Color(1.0f, 0.80f, 0.20f, 1f);

        float cx = 64f;
        float cy = 60f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Teardrop / kiss shape
                float normY = (dy + 35f) / 70f;
                if (normY >= 0f && normY <= 1.0f)
                {
                    float maxR = 34f * Mathf.Sqrt(1f - normY) * Mathf.Sin(normY * Mathf.PI * 0.9f + 0.1f);
                    if (Mathf.Abs(dx) < maxR)
                    {
                        // Chocolate base with flame swirl gradient
                        Color c = Color.Lerp(chocoDark, chocoLight, (dx + 25f) / 50f);
                        if (dy < -15f)
                        {
                            c = Color.Lerp(flameRed, flameGold, (dx + 20f) / 40f);
                        }
                        // Glossy highlight curve
                        if (dx > -18f && dx < -8f && dy > -10f && dy < 20f)
                        {
                            c = Color.Lerp(c, Color.white, 0.5f);
                        }
                        cols[y * size + x] = c;
                        continue;
                    }
                }
                cols[y * size + x] = clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetFood_ChocoDrop.png");
    }

    private static void GeneratePetFoodStarFlan()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color custardYellow = new Color(0.98f, 0.86f, 0.35f, 1f);
        Color caramelDark = new Color(0.40f, 0.16f, 0.05f, 1f);
        Color starCyan = new Color(0.30f, 0.90f, 1.0f, 1f);

        float cx = 64f;
        float cy = 54f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Flan trapezoid shape
                if (dy >= -28f && dy <= 18f)
                {
                    float widthAtY = 46f - (dy * 0.4f);
                    if (Mathf.Abs(dx) <= widthAtY)
                    {
                        if (dy > 8f)
                        {
                            // Caramel glaze topping
                            cols[y * size + x] = caramelDark;
                        }
                        else
                        {
                            // Custard body
                            cols[y * size + x] = custardYellow;
                            if (dx > 10f) cols[y * size + x] = Color.Lerp(custardYellow, Color.black, 0.15f);
                        }
                        continue;
                    }
                }

                // 5-point Astral Star Candy on top
                float sDist = Mathf.Sqrt(dx * dx + (dy - 34f) * (dy - 34f));
                if (sDist < 16f)
                {
                    float angle = Mathf.Atan2(dy - 34f, dx);
                    float r = 10f + 6f * Mathf.Cos(5f * angle);
                    if (sDist < r)
                    {
                        cols[y * size + x] = starCyan;
                        continue;
                    }
                }

                cols[y * size + x] = clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetFood_StarFlan.png");
    }

    private static void GeneratePetFoodBerryPuff()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color pastryPink = new Color(0.98f, 0.50f, 0.70f, 1f);
        Color whippedCream = new Color(0.98f, 0.98f, 1.0f, 1f);
        Color berryViolet = new Color(0.60f, 0.15f, 0.85f, 1f);

        float cx = 64f;
        float cy = 56f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Pastry puff base
                float distBase = Mathf.Sqrt((dx * 0.85f) * (dx * 0.85f) + (dy + 12f) * (dy + 12f));
                if (distBase < 36f && dy < 4f)
                {
                    cols[y * size + x] = pastryPink;
                    continue;
                }

                // Whipped cream swirl dollop
                float distCream = Mathf.Sqrt((dx * 0.95f) * (dx * 0.95f) + (dy - 10f) * (dy - 10f));
                if (distCream < 26f)
                {
                    cols[y * size + x] = whippedCream;
                    continue;
                }

                // Luminescent Berries on top
                float b1 = Mathf.Sqrt(dx * dx + (dy - 32f) * (dy - 32f));
                float b2 = Mathf.Sqrt((dx + 12f) * (dx + 12f) + (dy - 24f) * (dy - 24f));
                float b3 = Mathf.Sqrt((dx - 12f) * (dx - 12f) + (dy - 24f) * (dy - 24f));
                if (b1 < 9f || b2 < 8f || b3 < 8f)
                {
                    cols[y * size + x] = berryViolet;
                    continue;
                }

                cols[y * size + x] = clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetFood_BerryPuff.png");
    }

    private static void GeneratePetFoodHoneyPuff()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color pastryGold = new Color(0.95f, 0.75f, 0.20f, 1f);
        Color honeyAmber = new Color(0.98f, 0.55f, 0.05f, 1f);
        Color crustDark = new Color(0.65f, 0.38f, 0.08f, 1f);

        float cx = 64f;
        float cy = 60f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist < 42f)
                {
                    // Honeycomb hexagonal pattern
                    float hexX = Mathf.Abs(dx) % 18f;
                    float hexY = Mathf.Abs(dy) % 18f;
                    bool isRidge = (hexX < 2f || hexY < 2f);

                    if (isRidge)
                    {
                        cols[y * size + x] = crustDark;
                    }
                    else if (dy < 6f)
                    {
                        cols[y * size + x] = honeyAmber;
                    }
                    else
                    {
                        cols[y * size + x] = pastryGold;
                    }
                    continue;
                }

                cols[y * size + x] = clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetFood_HoneyPuff.png");
    }

    private static void GeneratePetWaterDewBowl()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color ceramicWhite = new Color(0.92f, 0.95f, 1.0f, 1f);
        Color ceramicShade = new Color(0.65f, 0.72f, 0.85f, 1f);
        Color waterCyan = new Color(0.25f, 0.85f, 1.0f, 1f);
        Color waterDeep = new Color(0.08f, 0.45f, 0.85f, 1f);

        float cx = 64f;
        float cy = 60f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Oval porcelain bowl rim
                float bowlDist = (dx * dx) / (48f * 48f) + (dy * dy) / (32f * 32f);
                if (bowlDist <= 1.0f)
                {
                    float innerWater = (dx * dx) / (38f * 38f) + (dy * dy) / (24f * 24f);
                    if (innerWater <= 1.0f)
                    {
                        // Sparkling spring water pool
                        float ripple = Mathf.Sin(innerWater * 14f);
                        cols[y * size + x] = Color.Lerp(waterDeep, waterCyan, (ripple + 1f) * 0.5f);

                        // Droplet glint
                        if (dx > -15f && dx < -8f && dy > 4f && dy < 12f)
                        {
                            cols[y * size + x] = Color.white;
                        }
                    }
                    else
                    {
                        // Ceramic rim
                        cols[y * size + x] = (dy > 0) ? ceramicWhite : ceramicShade;
                    }
                    continue;
                }

                cols[y * size + x] = clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetWater_DewBowl.png");
    }

    private static void GeneratePetBatheSponge()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color spongeTeal = new Color(0.20f, 0.80f, 0.75f, 1f);
        Color spongeDark = new Color(0.12f, 0.55f, 0.50f, 1f);
        Color foamWhite = new Color(0.96f, 0.98f, 1.0f, 0.95f);
        Color rainbowBubble = new Color(0.85f, 0.65f, 1.0f, 0.8f);

        float cx = 64f;
        float cy = 56f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Sponge body
                float sDist = (dx * dx) / (42f * 42f) + (dy * dy) / (28f * 28f);
                if (sDist <= 1.0f)
                {
                    cols[y * size + x] = (dy > 0) ? spongeTeal : spongeDark;

                    // Little sponge pore dots
                    if ((x % 8 == 0 && y % 8 == 0) || (x % 7 == 3 && y % 7 == 3))
                    {
                        cols[y * size + x] = spongeDark;
                    }
                    continue;
                }

                // Frothy foam bubbles overflowing top
                float b1 = Mathf.Sqrt((dx + 16f) * (dx + 16f) + (dy - 26f) * (dy - 26f));
                float b2 = Mathf.Sqrt((dx - 18f) * (dx - 18f) + (dy - 28f) * (dy - 28f));
                float b3 = Mathf.Sqrt(dx * dx + (dy - 34f) * (dy - 34f));
                if (b1 < 16f || b2 < 14f || b3 < 18f)
                {
                    cols[y * size + x] = foamWhite;
                    continue;
                }

                // Floating rainbow bubble
                float floatBubble = Mathf.Sqrt((dx - 32f) * (dx - 32f) + (dy - 44f) * (dy - 44f));
                if (floatBubble < 10f && floatBubble > 6f)
                {
                    cols[y * size + x] = rainbowBubble;
                    continue;
                }

                cols[y * size + x] = clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetBathe_Sponge.png");
    }

    private static void GeneratePetPresentToyBell()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color velvetRed = new Color(0.85f, 0.15f, 0.25f, 1f);
        Color bellGold = new Color(1.0f, 0.84f, 0.20f, 1f);
        Color bellShade = new Color(0.70f, 0.50f, 0.08f, 1f);

        float cx = 64f;
        float cy = 56f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Velvet collar band
                if (dy >= 22f && dy <= 38f && Mathf.Abs(dx) < 46f)
                {
                    cols[y * size + x] = velvetRed;
                    continue;
                }

                // Golden spherical bell
                float bDist = Mathf.Sqrt(dx * dx + (dy + 6f) * (dy + 6f));
                if (bDist < 30f)
                {
                    // Bell slit opening
                    if (dy < -14f && Mathf.Abs(dx) < 18f)
                    {
                        cols[y * size + x] = new Color(0.20f, 0.12f, 0.04f, 1f);
                    }
                    else
                    {
                        cols[y * size + x] = (dx < -6f && dy > 0f) ? Color.Lerp(bellGold, Color.white, 0.6f) : Color.Lerp(bellGold, bellShade, (float)y / size);
                    }
                    continue;
                }

                cols[y * size + x] = clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetPresent_ToyBell.png");
    }

    private static void GeneratePetPresentGiftBox()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color boxMagenta = new Color(0.90f, 0.20f, 0.55f, 1f);
        Color ribbonGold = new Color(1.0f, 0.85f, 0.25f, 1f);

        float cx = 64f;
        float cy = 56f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // Box base
                if (Mathf.Abs(dx) < 36f && dy >= -36f && dy <= 22f)
                {
                    // Cross ribbon
                    if (Mathf.Abs(dx) < 8f || (dy >= -8f && dy <= 4f))
                    {
                        cols[y * size + x] = ribbonGold;
                    }
                    else
                    {
                        cols[y * size + x] = boxMagenta;
                    }
                    continue;
                }

                // Big bow loops on top
                float loopL = Mathf.Sqrt((dx + 16f) * (dx + 16f) + (dy - 32f) * (dy - 32f));
                float loopR = Mathf.Sqrt((dx - 16f) * (dx - 16f) + (dy - 32f) * (dy - 32f));
                if (loopL < 16f || loopR < 16f)
                {
                    cols[y * size + x] = ribbonGold;
                    continue;
                }

                cols[y * size + x] = clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetPresent_GiftBox.png");
    }

    private static void GeneratePetCareHudBtn()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color clear = Color.clear;
        Color goldRing = new Color(1.0f, 0.84f, 0.25f, 1f);
        Color darkBg = new Color(0.08f, 0.10f, 0.15f, 0.95f);
        Color pawPink = new Color(1.0f, 0.45f, 0.65f, 1f);

        float cx = size * 0.5f;
        float cy = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > 58f)
                {
                    cols[y * size + x] = clear;
                }
                else if (d > 52f)
                {
                    cols[y * size + x] = goldRing;
                }
                else
                {
                    cols[y * size + x] = darkBg;

                    // Central main paw pad
                    float padDist = (dx * dx) / (20f * 20f) + ((dy + 6f) * (dy + 6f)) / (16f * 16f);
                    if (padDist <= 1.0f)
                    {
                        cols[y * size + x] = pawPink;
                    }

                    // 4 toe pads
                    float t1 = Mathf.Sqrt((dx + 20f) * (dx + 20f) + (dy - 12f) * (dy - 12f));
                    float t2 = Mathf.Sqrt((dx + 7f) * (dx + 7f) + (dy - 22f) * (dy - 22f));
                    float t3 = Mathf.Sqrt((dx - 7f) * (dx - 7f) + (dy - 22f) * (dy - 22f));
                    float t4 = Mathf.Sqrt((dx - 20f) * (dx - 20f) + (dy - 12f) * (dy - 12f));
                    if (t1 < 6.5f || t2 < 7f || t3 < 7f || t4 < 6.5f)
                    {
                        cols[y * size + x] = pawPink;
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_PetCareHudBtn.png");
    }
    #endregion

    #region Risk of Rain & Dungeon Dimension Textures
    private static void GenerateRoRTimerFrame()
    {
        int w = 260;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        Color border = new Color(0.20f, 0.90f, 1.0f, 0.95f);
        Color bg = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        Color accent = new Color(0.95f, 0.35f, 0.15f, 0.85f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = (x < 3 || x >= w - 3 || y < 3 || y >= h - 3);
                bool isCorner = (x < 12 && (y < 4 || y >= h - 4)) || (x >= w - 12 && (y < 4 || y >= h - 4));
                if (isCorner) cols[y * w + x] = accent;
                else if (isBorder) cols[y * w + x] = border;
                else cols[y * w + x] = bg;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_RoR_TimerFrame.png");
    }

    private static void GenerateRoRDifficultyBarFill()
    {
        int w = 180;
        int h = 18;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (float)x / w;
                cols[y * w + x] = Color.Lerp(new Color(0.2f, 0.95f, 0.4f), new Color(1.0f, 0.2f, 0.3f), t);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_RoR_DifficultyBarFill.png");
    }

    private static void GenerateBoonCardFrame()
    {
        int w = 220;
        int h = 320;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];
        Color border = new Color(0.85f, 0.70f, 0.25f, 1f);
        Color bg = new Color(0.08f, 0.10f, 0.15f, 0.95f);
        Color glow = new Color(0.20f, 0.85f, 1.0f, 0.8f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isOuter = (x < 4 || x >= w - 4 || y < 4 || y >= h - 4);
                bool isInnerBorder = (x == 10 || x == w - 11 || y == 10 || y == h - 11);
                if (isOuter) cols[y * w + x] = border;
                else if (isInnerBorder) cols[y * w + x] = glow;
                else cols[y * w + x] = bg;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_BoonCard_Frame.png");
    }

    private static void GenerateBoonVoidEdge()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color purple = new Color(0.75f, 0.20f, 1.0f, 1f);
        Color blade = new Color(0.95f, 0.85f, 1.0f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                // Diagonal sword blade
                int diff = Mathf.Abs((x - y));
                if (diff < 3 && x > 15 && x < 65) cols[y * s + x] = blade;
                else if (diff < 7 && x > 12 && x < 68) cols[y * s + x] = purple;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_VoidEdge.png");
    }

    private static void GenerateBoonChronoStride()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color cyan = new Color(0.20f, 0.90f, 1.0f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                float d = Mathf.Sqrt((x - 40) * (x - 40) + (y - 40) * (y - 40));
                // Speed ring + arrow
                if (d > 24f && d < 29f) cols[y * s + x] = cyan;
                if (y > 36 && y < 44 && x > 20 && x < 55) cols[y * s + x] = Color.white;
                if (Mathf.Abs(x - 55) + Mathf.Abs(y - 40) < 12) cols[y * s + x] = cyan;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_ChronoStride.png");
    }

    private static void GenerateBoonVampiricLeech()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color blood = new Color(0.95f, 0.10f, 0.20f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                float dx = x - 40;
                float dy = y - 32;
                // Teardrop / blood drop
                if (dy <= 0)
                {
                    if (Mathf.Abs(dx) < (32 + dy) * 0.7f && y > 10) cols[y * s + x] = blood;
                }
                else
                {
                    if (dx * dx + dy * dy < 22f * 22f) cols[y * s + x] = blood;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_VampiricLeech.png");
    }

    private static void GenerateBoonStaticConduit()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color elec = new Color(0.30f, 0.85f, 1.0f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                // Lightning bolt
                bool bolt1 = (y > 40 && Mathf.Abs((x - 48) + (y - 40) * 0.6f) < 4f);
                bool bolt2 = (y <= 45 && Mathf.Abs((x - 34) + (y - 20) * 0.6f) < 4f);
                if (bolt1 || bolt2) cols[y * s + x] = elec;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_StaticConduit.png");
    }

    private static void GenerateBoonTitansAegis()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color gold = new Color(1.0f, 0.80f, 0.20f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                // Shield outline
                float dx = Mathf.Abs(x - 40);
                if (dx < 26f && y > 20 && y < 65)
                {
                    if (y < 45 || dx < (65 - y) * 1.3f) cols[y * s + x] = gold;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_TitansAegis.png");
    }

    private static void GenerateBoonManaTorrent()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color mana = new Color(0.20f, 0.50f, 1.0f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                float d = Mathf.Sqrt((x - 40) * (x - 40) + (y - 40) * (y - 40));
                // Swirling mana orb
                if (d < 24f && d > 6f)
                {
                    float angle = Mathf.Atan2(y - 40, x - 40);
                    if (Mathf.Sin(angle * 3f + d * 0.3f) > 0f) cols[y * s + x] = mana;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_ManaTorrent.png");
    }

    private static void GenerateBoonFamiliarFrenzy()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color green = new Color(0.30f, 1.0f, 0.50f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                // Glowing fairy star
                float dx = Mathf.Abs(x - 40);
                float dy = Mathf.Abs(y - 40);
                if (dx + dy < 22f || (dx < 5f && dy < 32f) || (dy < 5f && dx < 32f))
                {
                    cols[y * s + x] = green;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_FamiliarFrenzy.png");
    }

    private static void GenerateBoonExecuteStrike()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color gold = new Color(1.0f, 0.85f, 0.20f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                float d = Mathf.Sqrt((x - 40) * (x - 40) + (y - 40) * (y - 40));
                // Crossed scythe / execute marks
                if ((Mathf.Abs(x - y) < 4 || Mathf.Abs(x + y - 80) < 4) && d < 28f)
                {
                    cols[y * s + x] = gold;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_ExecuteStrike.png");
    }

    private static void GenerateBoonCullWeak()
    {
        int s = 80;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color[] cols = new Color[s * s];
        Color orange = new Color(1.0f, 0.40f, 0.15f, 1f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                cols[y * s + x] = Color.clear;
                float d = Mathf.Sqrt((x - 40) * (x - 40) + (y - 40) * (y - 40));
                // Shockwave explosion rings
                if ((d > 12f && d < 17f) || (d > 24f && d < 28f))
                {
                    cols[y * s + x] = orange;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        SavePNG(tex, "Mockup_Boon_CullWeak.png");
    }
    #endregion
}

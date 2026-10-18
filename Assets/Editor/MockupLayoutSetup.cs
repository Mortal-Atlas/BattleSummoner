using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BattleSummoner.Combat;

/// <summary>
/// MockupLayoutSetup procedurally generates UI sprite assets matching the user's mockup layout,
/// builds the complete HUD hierarchy in MainDungeonFloor.unity, and wires all components:
/// - Settings button on top-left
/// - Auto-Battler AI button directly under Settings on top-left
/// - Player Stats Frame (Portrait, LV.12, CBP bar, HP/MP bars, 4 Potions) on top-left
/// - Spire Broadcast Banner on top-center
/// - Minimap, Messenger Bag, Whetstone on top-right
/// - 8-Person Party Squad vertical list on right edge
/// - Camera snapshot button and bottom controls
/// </summary>
public static class MockupLayoutSetup
{
    private const string TEXTURE_DIR = "Assets/Textures/UI";

    [MenuItem("Tools/BattleSummoner/Apply Mockup Layout")]
    public static void ApplyMockupLayout()
    {
        EnsureDirectory(TEXTURE_DIR);

        // 1. Generate procedural UI sprites
        GenerateAllMockupTextures();
        ReimportAllMockupSprites();

        // 2. Build / Update the Canvas Hierarchy in MainDungeonFloor scene
        ConfigureSceneUI();

        Debug.Log("✨ [MOCKUP LAYOUT] Successfully applied the master UI mockup layout to MainDungeonFloor.unity!");
    }

    #region Texture Generation & Importing
    public static void ReimportAllMockupSprites()
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

    private static Sprite GetSprite(string filename)
    {
        string path = (TEXTURE_DIR + "/" + filename).Replace('\\', '/');
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return sp;
    }

    private static void GenerateAllMockupTextures()
    {
        CreateTexture("Mockup_SettingsGear.png", 128, 128, DrawSettingsGear);
        CreateTexture("Mockup_AIAutoRobot.png", 128, 128, DrawAIAutoRobot);
        CreateTexture("Mockup_GothicHeroinePortrait.png", 256, 256, DrawGothicHeroinePortrait);
        CreateTexture("Mockup_MinimapRadar.png", 256, 256, DrawMinimapRadar);
        CreateTexture("Mockup_MessengerBag.png", 128, 128, DrawMessengerBag);
        CreateTexture("Mockup_Whetstone.png", 128, 128, DrawWhetstone);
        CreateTexture("Mockup_PotionRed.png", 128, 128, (c, x, y, w, h) => DrawPotionFlask(c, x, y, w, h, new Color(0.95f, 0.2f, 0.25f)));
        CreateTexture("Mockup_PotionBlue.png", 128, 128, (c, x, y, w, h) => DrawPotionFlask(c, x, y, w, h, new Color(0.15f, 0.75f, 1.0f)));
        CreateTexture("Mockup_PotionAmber.png", 128, 128, (c, x, y, w, h) => DrawPotionFlask(c, x, y, w, h, new Color(1.0f, 0.75f, 0.15f)));
        CreateTexture("Mockup_PotionElixir.png", 128, 128, (c, x, y, w, h) => DrawPotionFlask(c, x, y, w, h, new Color(0.85f, 0.35f, 0.95f)));
        CreateTexture("Mockup_CameraSnap.png", 128, 128, DrawCameraSnap);
        CreateTexture("Mockup_CarouselFrame.png", 320, 128, DrawCarouselFrame);
        CreateTexture("Mockup_ArrowLeft.png", 96, 96, (c, ox, oy, w, h) => DrawArrowChevron(c, ox, oy, w, h, true));
        CreateTexture("Mockup_ArrowRight.png", 96, 96, (c, ox, oy, w, h) => DrawArrowChevron(c, ox, oy, w, h, false));
        CreateTexture("Mockup_CastButton.png", 192, 96, DrawCastButton);
        CreateTexture("Mockup_CastDisc.png", 256, 256, DrawCastDisc);
        CreateTexture("Mockup_DispelButton.png", 256, 96, DrawDispelButton);
        CreateTexture("Mockup_RuneCircle.png", 384, 384, DrawRuneCircle);
        CreateTexture("Mockup_BroadcastBanner.png", 512, 96, DrawBroadcastBanner);
        CreateTexture("Mockup_JoystickRing.png", 256, 256, DrawJoystickRing);
        CreateTexture("Mockup_JoystickHandle.png", 128, 128, DrawJoystickHandle);
        CreateTexture("Mockup_JumpButton.png", 128, 128, DrawJumpButton);
        CreateTexture("Mockup_ScopeButton.png", 128, 128, DrawScopeButton);
        CreateTexture("Mockup_GrimoireButton.png", 128, 128, DrawGrimoireButton);
        CreateTexture("Mockup_CrosshairReticle.png", 256, 256, DrawCrosshairReticle);
        CreateTexture("Mockup_GrimoireParchment.png", 512, 384, DrawGrimoireParchment);

        Color[] squadTints = {
            new Color(0.3f, 0.5f, 0.9f),   // 1. Azure Knight
            new Color(0.2f, 0.85f, 0.5f),  // 2. Emerald Ranger
            new Color(0.95f, 0.4f, 0.2f),  // 3. Flame Berserker
            new Color(0.85f, 0.3f, 0.85f), // 4. Violet Hexer
            new Color(0.3f, 0.85f, 0.9f),  // 5. Frost Sorceress
            new Color(0.9f, 0.75f, 0.2f),  // 6. Radiant Paladin
            new Color(0.95f, 0.3f, 0.3f),  // 7. Crimson Lancer
            new Color(0.5f, 0.6f, 0.95f),  // 8. Astral Summoner
            new Color(0.95f, 0.55f, 0.75f), // 9. Rose Cleric
            new Color(0.95f, 0.80f, 0.25f)  // 10. Solar Valkyrie
        };

        for (int i = 0; i < 10; i++)
        {
            int idx = i;
            CreateTexture($"Mockup_Squad_{idx + 1}.png", 128, 128, (c, x, y, w, h) => DrawSquadMember(c, x, y, w, h, squadTints[idx], idx));
        }
    }

    private static void CreateTexture(string filename, int width, int height, Action<Color[], int, int, int, int> drawAction)
    {
        string fullPath = Path.Combine(TEXTURE_DIR, filename).Replace('\\', '/');
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        Color clear = new Color(0, 0, 0, 0);
        for (int i = 0; i < colors.Length; i++) colors[i] = clear;

        drawAction(colors, 0, 0, width, height);

        tex.SetPixels(colors);
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }
    #endregion

    #region Procedural Drawing Methods
    private static void DrawSettingsGear(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        float rOuter = w * 0.46f;
        float rBase = w * 0.35f;
        float rInnerRing = w * 0.26f;
        float rHole = w * 0.14f;

        Color goldRim = new Color(0.88f, 0.74f, 0.32f, 1f);
        Color goldHi = new Color(1.0f, 0.92f, 0.60f, 1f);
        Color holeDark = new Color(0.09f, 0.08f, 0.13f, 0.95f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    float angle = Mathf.Atan2(dy, dx);
                    float toothWave = Mathf.Cos(angle * 12f);
                    float toothRadius = (toothWave > 0.15f) ? rOuter : rBase;

                    if (d <= toothRadius)
                    {
                        if (d < rHole)
                        {
                            SetPixel(c, x, y, w, holeDark);
                        }
                        else if (d <= rHole + 3f)
                        {
                            SetPixel(c, x, y, w, goldRim);
                        }
                        else if (d > rInnerRing && d < rBase - 3f)
                        {
                            float spokeAngle = Mathf.Abs(Mathf.Sin(angle * 6f));
                            if (spokeAngle < 0.65f)
                            {
                                SetPixel(c, x, y, w, holeDark);
                            }
                            else
                            {
                                SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                            }
                        }
                        else
                        {
                            if (d >= toothRadius - 3f)
                            {
                                SetPixel(c, x, y, w, goldHi);
                            }
                            else
                            {
                                SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                            }
                        }
                    }
                }
            }
        }
    }

    private static void DrawAIAutoRobot(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.44f;
        Color bg = new Color(0.18f, 0.19f, 0.25f, 0.95f);
        Color rim = new Color(0.85f, 0.72f, 0.38f, 1f);
        Color robotColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        Color eyeGlow = new Color(0.15f, 0.85f, 1.0f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    if (d >= rOuter - 4f) SetPixel(c, x, y, w, rim);
                    else
                    {
                        SetPixel(c, x, y, w, bg);

                        // Draw robot head
                        if (x >= cx - 22 && x <= cx + 22 && y >= cy - 14 && y <= cy + 18)
                        {
                            SetPixel(c, x, y, w, robotColor);
                            // Eyes
                            if ((x >= cx - 14 && x <= cx - 6 || x >= cx + 6 && x <= cx + 14) && y >= cy + 2 && y <= cy + 9)
                            {
                                SetPixel(c, x, y, w, eyeGlow);
                            }
                            // Smile
                            if (x >= cx - 10 && x <= cx + 10 && y >= cy - 8 && y <= cy - 6)
                            {
                                SetPixel(c, x, y, w, new Color(0.15f, 0.18f, 0.25f));
                            }
                        }
                        // Antenna
                        if (x >= cx - 2 && x <= cx + 2 && y >= cy + 18 && y <= cy + 28) SetPixel(c, x, y, w, robotColor);
                        if (x >= cx - 4 && x <= cx + 4 && y >= cy + 28 && y <= cy + 34) SetPixel(c, x, y, w, rim);
                    }
                }
            }
        }
    }

    private static void DrawGothicHeroinePortrait(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color goldRim = new Color(0.92f, 0.80f, 0.35f, 1f);
        Color goldHi = new Color(1.0f, 0.95f, 0.70f, 1f);
        Color goldInner = new Color(0.48f, 0.36f, 0.14f, 1f);
        Color bgDark = new Color(0.11f, 0.09f, 0.16f, 1f);
        Color bgPop = new Color(0.24f, 0.14f, 0.35f, 1f);
        Color hairDark = new Color(0.12f, 0.10f, 0.15f, 1f);
        Color hairHi = new Color(0.32f, 0.28f, 0.42f, 1f);
        Color skinPale = new Color(0.96f, 0.88f, 0.84f, 1f);
        Color eyesViolet = new Color(0.68f, 0.42f, 0.92f, 1f);
        Color eyesHi = new Color(1.0f, 0.95f, 1.0f, 1f);
        Color lipsRose = new Color(0.85f, 0.38f, 0.48f, 1f);
        Color collarBlack = new Color(0.08f, 0.06f, 0.10f, 1f);

        float maxHalfW = w * 0.46f;
        float maxHalfH = h * 0.46f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy;
                // Sharp slanted cut-in parallelogram (Persona 3/4 aesthetic)
                float slantX = dx - (dy * 0.16f);
                if (Mathf.Abs(slantX) <= maxHalfW && Mathf.Abs(dy) <= maxHalfH)
                {
                    // Sharp angled comic border
                    if (Mathf.Abs(slantX) >= maxHalfW - 5f || Mathf.Abs(dy) >= maxHalfH - 5f)
                    {
                        SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                    }
                    else if (Mathf.Abs(slantX) >= maxHalfW - 8f || Mathf.Abs(dy) >= maxHalfH - 8f)
                    {
                        SetPixel(c, x, y, w, goldInner);
                    }
                    else
                    {
                        // Dynamic Persona dual-tone comic background split
                        SetPixel(c, x, y, w, (slantX + dy * 0.5f > 12f) ? bgPop : bgDark);

                        // High contrast neck / collar
                        if (x >= cx - 26 && x <= cx + 26 && y >= cy - 65 && y <= cy - 14)
                        {
                            SetPixel(c, x, y, w, (y < cy - 40) ? collarBlack : skinPale);
                        }

                        // Sharp angular face profile
                        float fdx = (x - cx) * 1.0f; float fdy = (y - (cy + 10)) * 0.85f;
                        if (Mathf.Sqrt(fdx * fdx + fdy * fdy) < 54f)
                        {
                            SetPixel(c, x, y, w, skinPale);
                            // Dramatic anime eyes
                            float edy = y - (cy + 16);
                            if (edy >= -4 && edy <= 5 && ((x >= cx - 28 && x <= cx - 12) || (x >= cx + 12 && x <= cx + 28)))
                            {
                                SetPixel(c, x, y, w, (edy >= 2 && (x == cx - 18 || x == cx + 22)) ? eyesHi : eyesViolet);
                            }
                            // Sharp nose highlight
                            if (x >= cx - 2 && x <= cx + 2 && y >= cy && y <= cy + 6) SetPixel(c, x, y, w, new Color(0.86f, 0.76f, 0.72f));
                            // Lips
                            if (x >= cx - 10 && x <= cx + 10 && y >= cy - 14 && y <= cy - 9) SetPixel(c, x, y, w, lipsRose);
                        }

                        // Sharp Gothic Hair strands
                        float hdx = (x - cx); float hdy = (y - (cy + 30));
                        if (Mathf.Sqrt(hdx * hdx + hdy * hdy * 0.9f) < 72f && (y > cy + 22 || Mathf.Abs(x - cx) > 40))
                        {
                            SetPixel(c, x, y, w, (Mathf.Abs(hdx) < 16 && y > cy + 45) ? hairHi : hairDark);
                        }
                        if (x >= cx - 36 && x <= cx - 16 && y >= cy + 10 && y <= cy + 40) SetPixel(c, x, y, w, hairDark);
                        if (x >= cx + 16 && x <= cx + 38 && y >= cy + 12 && y <= cy + 42) SetPixel(c, x, y, w, hairDark);
                    }
                }
            }
        }
    }

    private static void DrawCastButton(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color rimGold = new Color(0.95f, 0.82f, 0.38f, 1f);
        Color rimHi = new Color(1.0f, 0.95f, 0.65f, 1f);
        Color bgDark = new Color(0.14f, 0.10f, 0.22f, 0.95f);
        Color runeGlow = new Color(0.98f, 0.78f, 0.25f, 0.9f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx); float dy = Mathf.Abs(y - cy);
                float cornerCut = Mathf.Max(0f, (dx - (w * 0.5f - 14f))) + Mathf.Max(0f, (dy - (h * 0.5f - 14f)));
                if (cornerCut < 14f)
                {
                    if (cornerCut > 9.5f || dx > (w * 0.5f - 4f) || dy > (h * 0.5f - 4f))
                    {
                        SetPixel(c, x, y, w, Color.Lerp(rimGold, rimHi, (float)y / h));
                    }
                    else if (cornerCut > 6.5f || dx > (w * 0.5f - 7f) || dy > (h * 0.5f - 7f))
                    {
                        SetPixel(c, x, y, w, new Color(0.38f, 0.28f, 0.12f, 0.95f));
                    }
                    else
                    {
                        SetPixel(c, x, y, w, bgDark);
                        if (dx > (w * 0.5f - 16f) && Mathf.Abs(dy - 4f) < 3f) SetPixel(c, x, y, w, runeGlow);
                    }
                }
            }
        }
    }

    private static void DrawCastDisc(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        float rOuter = w * 0.47f;
        float rMid = w * 0.40f;
        float rInner = w * 0.31f;
        float rCore = w * 0.16f;

        Color goldRim = new Color(0.98f, 0.85f, 0.32f, 1f);
        Color goldHi = new Color(1.0f, 0.96f, 0.65f, 1f);
        Color goldDark = new Color(0.42f, 0.30f, 0.10f, 1f);
        Color arcaneBg1 = new Color(0.12f, 0.08f, 0.22f, 0.95f);
        Color arcaneBg2 = new Color(0.24f, 0.10f, 0.32f, 0.95f);
        Color rubyGlow = new Color(0.95f, 0.25f, 0.40f, 1f);
        Color runeGold = new Color(1.0f, 0.88f, 0.45f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    float angle = Mathf.Atan2(dy, dx);

                    // Outer notched gold rim
                    if (d >= rMid)
                    {
                        // 12 outer teeth/notches
                        float notch = Mathf.Sin(angle * 12f);
                        if (d >= rOuter - 5f || (notch > 0.65f && d >= rMid + 4f))
                        {
                            SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                        }
                        else if (d <= rMid + 3f)
                        {
                            SetPixel(c, x, y, w, goldDark);
                        }
                        else
                        {
                            SetPixel(c, x, y, w, new Color(0.18f, 0.14f, 0.25f, 0.95f));
                            if (Mathf.Abs(notch) > 0.85f) SetPixel(c, x, y, w, rubyGlow);
                        }
                    }
                    else if (d >= rInner)
                    {
                        // Outer rune ring
                        SetPixel(c, x, y, w, Color.Lerp(arcaneBg1, arcaneBg2, (float)(x + y) / (w + h)));
                        float oct1 = Mathf.Abs(Mathf.Sin(angle * 4f));
                        float oct2 = Mathf.Abs(Mathf.Cos(angle * 4f));
                        if (Mathf.Abs(d - (rMid + rInner) * 0.5f) < 2.5f || oct1 > 0.96f || oct2 > 0.96f)
                        {
                            SetPixel(c, x, y, w, runeGold);
                        }
                    }
                    else if (d >= rCore)
                    {
                        // Inner chamber
                        SetPixel(c, x, y, w, arcaneBg1);
                        float triDist = PointToTriangleEdgeDistance(dx, dy, rInner * 0.88f);
                        float triDistInv = PointToTriangleEdgeDistance(dx, -dy, rInner * 0.88f);
                        if (triDist < 2.5f || triDistInv < 2.5f)
                        {
                            SetPixel(c, x, y, w, runeGold);
                        }
                        if (Mathf.Abs(Mathf.Abs(dx) + Mathf.Abs(dy) - rCore * 1.5f) < 2.2f)
                        {
                            SetPixel(c, x, y, w, rubyGlow);
                        }
                    }
                    else
                    {
                        // Glowing center medallion
                        float coreNorm = d / rCore;
                        if (d >= rCore - 3f)
                        {
                            SetPixel(c, x, y, w, goldRim);
                        }
                        else
                        {
                            Color coreGlow = Color.Lerp(new Color(1f, 0.95f, 0.8f), rubyGlow, coreNorm);
                            SetPixel(c, x, y, w, coreGlow);
                        }
                    }
                }
            }
        }
    }

    private static void DrawDispelButton(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color rimCrimson = new Color(0.92f, 0.22f, 0.28f, 1f);
        Color rimGold = new Color(0.95f, 0.82f, 0.35f, 1f);
        Color bgDark = new Color(0.18f, 0.06f, 0.10f, 0.95f);
        Color bgMid = new Color(0.32f, 0.08f, 0.14f, 0.95f);
        Color slashGlow = new Color(1.0f, 0.35f, 0.40f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx); float dy = Mathf.Abs(y - cy);
                float cornerCut = Mathf.Max(0f, (dx - (w * 0.5f - 18f))) + Mathf.Max(0f, (dy - (h * 0.5f - 18f)));
                if (cornerCut < 18f)
                {
                    if (cornerCut > 13f || dx > (w * 0.5f - 5f) || dy > (h * 0.5f - 5f))
                    {
                        SetPixel(c, x, y, w, Color.Lerp(rimCrimson, rimGold, (float)x / w));
                    }
                    else if (cornerCut > 10f || dx > (w * 0.5f - 8f) || dy > (h * 0.5f - 8f))
                    {
                        SetPixel(c, x, y, w, new Color(0.10f, 0.02f, 0.04f, 0.95f));
                    }
                    else
                    {
                        float stripe = Mathf.Sin((x + y) * 0.3f);
                        Color bg = Color.Lerp(bgDark, bgMid, (float)y / h);
                        if (stripe > 0.7f) bg += new Color(0.06f, 0.02f, 0.03f, 0f);
                        SetPixel(c, x, y, w, bg);

                        // Arcane "X" Dispel / Cancel Glyph on the left side
                        float glyphCx = 46f;
                        float gdx = Mathf.Abs(x - glyphCx);
                        float gdy = Mathf.Abs(y - cy);
                        if (gdx < 20f && gdy < 20f)
                        {
                            if (Mathf.Abs(gdx - gdy) < 2.5f && (gdx + gdy) < 28f)
                            {
                                SetPixel(c, x, y, w, slashGlow);
                            }
                            else if (Mathf.Abs(gdx + gdy - 14f) < 2f)
                            {
                                SetPixel(c, x, y, w, rimGold);
                            }
                        }

                        // Right edge ornamental chevron
                        float rightChevronX = w - 42f;
                        float cdx = x - rightChevronX;
                        if (Mathf.Abs(cdx - Mathf.Abs(y - cy) * 0.5f) < 2f && Mathf.Abs(y - cy) < 20f)
                        {
                            SetPixel(c, x, y, w, rimGold);
                        }
                    }
                }
            }
        }
    }

    private static void DrawMinimapRadar(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.47f;
        Color goldRim = new Color(0.88f, 0.74f, 0.32f, 1f);
        Color goldHi = new Color(1.0f, 0.92f, 0.55f, 1f);
        Color mapBg = new Color(0.09f, 0.11f, 0.14f, 0.96f);
        Color corridor = new Color(0.20f, 0.24f, 0.30f, 1f);
        Color playerBlip = new Color(1.0f, 0.88f, 0.20f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    if (d >= rOuter - 5f)
                    {
                        SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                    }
                    else
                    {
                        SetPixel(c, x, y, w, mapBg);
                        if ((Mathf.Abs(dx) < 14f && Mathf.Abs(dy) < rOuter - 10f) || (Mathf.Abs(dy) < 14f && Mathf.Abs(dx) < rOuter - 10f))
                        {
                            SetPixel(c, x, y, w, corridor);
                        }
                        if ((Mathf.Abs(dx) < 3f && d > rOuter - 16f) || (Mathf.Abs(dy) < 3f && d > rOuter - 16f))
                        {
                            SetPixel(c, x, y, w, goldHi);
                        }
                        if (d <= 7f) SetPixel(c, x, y, w, playerBlip);
                    }
                }
            }
        }
    }

    private static void DrawMessengerBag(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.44f;
        Color rim = new Color(0.85f, 0.70f, 0.28f, 1f);
        Color leather = new Color(0.55f, 0.32f, 0.16f, 1f);
        Color leatherDark = new Color(0.38f, 0.20f, 0.10f, 1f);
        Color buckle = new Color(0.95f, 0.85f, 0.35f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    if (d >= rOuter - 4f) SetPixel(c, x, y, w, rim);
                    else
                    {
                        SetPixel(c, x, y, w, new Color(0.12f, 0.10f, 0.15f, 0.9f));
                        if (x >= cx - 28 && x <= cx + 28 && y >= cy - 22 && y <= cy + 20)
                        {
                            SetPixel(c, x, y, w, (y > cy - 2) ? leather : leatherDark);
                            if ((x >= cx - 18 && x <= cx - 12 || x >= cx + 12 && x <= cx + 18) && y >= cy - 20 && y <= cy + 15)
                            {
                                SetPixel(c, x, y, w, buckle);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void DrawWhetstone(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.44f;
        Color rim = new Color(0.85f, 0.70f, 0.28f, 1f);
        Color stone = new Color(0.55f, 0.58f, 0.62f, 1f);
        Color steel = new Color(0.88f, 0.92f, 0.98f, 1f);
        Color spark = new Color(1.0f, 0.85f, 0.20f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    if (d >= rOuter - 4f) SetPixel(c, x, y, w, rim);
                    else
                    {
                        SetPixel(c, x, y, w, new Color(0.12f, 0.10f, 0.15f, 0.9f));
                        float u = (dx + dy) * 0.707f; float v = (-dx + dy) * 0.707f;
                        if (Mathf.Abs(u) < 26f && Mathf.Abs(v) < 14f) SetPixel(c, x, y, w, stone);
                        if (Mathf.Abs(u - 4f) < 4f && Mathf.Abs(v) < 28f) SetPixel(c, x, y, w, steel);
                        if (Mathf.Abs(dx) < 6f && Mathf.Abs(dy) < 6f) SetPixel(c, x, y, w, spark);
                    }
                }
            }
        }
    }

    private static void DrawPotionFlask(Color[] c, int ox, int oy, int w, int h, Color liquid)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.44f;
        Color rim = new Color(0.85f, 0.70f, 0.28f, 1f);
        Color glass = new Color(0.75f, 0.85f, 0.95f, 0.5f);
        Color cork = new Color(0.65f, 0.42f, 0.22f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    if (d >= rOuter - 3f) SetPixel(c, x, y, w, rim);
                    else
                    {
                        SetPixel(c, x, y, w, new Color(0.08f, 0.08f, 0.12f, 0.9f));
                        float bdx = x - cx; float bdy = y - (cy - 6);
                        float bd = Mathf.Sqrt(bdx * bdx + bdy * bdy);
                        if (bd <= 24f) SetPixel(c, x, y, w, (bdy < 8f) ? liquid : glass);
                        if (x >= cx - 6 && x <= cx + 6 && y >= cy + 12 && y <= cy + 24) SetPixel(c, x, y, w, (y > cy + 18) ? cork : glass);
                    }
                }
            }
        }
    }

    private static void DrawCameraSnap(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.44f;
        Color rim = new Color(0.85f, 0.70f, 0.28f, 1f);
        Color body = new Color(0.92f, 0.94f, 0.98f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    if (d >= rOuter - 3f) SetPixel(c, x, y, w, rim);
                    else
                    {
                        SetPixel(c, x, y, w, new Color(0.12f, 0.14f, 0.18f, 0.9f));
                        if (x >= cx - 22 && x <= cx + 22 && y >= cy - 14 && y <= cy + 14)
                        {
                            SetPixel(c, x, y, w, body);
                            float ldx = x - cx; float ldy = y - cy;
                            if (Mathf.Sqrt(ldx * ldx + ldy * ldy) < 9f) SetPixel(c, x, y, w, new Color(0.2f, 0.25f, 0.35f));
                        }
                        if (x >= cx - 14 && x <= cx - 6 && y >= cy + 14 && y <= cy + 18) SetPixel(c, x, y, w, body);
                    }
                }
            }
        }
    }

    private static void DrawCarouselFrame(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color rimGold = new Color(0.88f, 0.74f, 0.32f, 1f);
        Color rimHi = new Color(1.0f, 0.94f, 0.65f, 1f);
        Color rimDark = new Color(0.35f, 0.25f, 0.10f, 1f);
        Color bgDark = new Color(0.10f, 0.08f, 0.14f, 0.95f);
        Color bgMid = new Color(0.18f, 0.14f, 0.24f, 0.95f);
        Color slotBorder = new Color(0.55f, 0.45f, 0.25f, 0.8f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx); float dy = Mathf.Abs(y - cy);
                float cornerCut = Mathf.Max(0f, (dx - (w * 0.5f - 16f))) + Mathf.Max(0f, (dy - (h * 0.5f - 16f)));
                if (cornerCut < 16f)
                {
                    if (cornerCut > 11.5f || dx > (w * 0.5f - 4.5f) || dy > (h * 0.5f - 4.5f))
                    {
                        SetPixel(c, x, y, w, Color.Lerp(rimGold, rimHi, (float)y / h));
                    }
                    else if (cornerCut > 8.5f || dx > (w * 0.5f - 7f) || dy > (h * 0.5f - 7f))
                    {
                        SetPixel(c, x, y, w, rimDark);
                    }
                    else
                    {
                        Color bg = Color.Lerp(bgDark, bgMid, (float)y / h);
                        if (dx < 72f && dy < (h * 0.5f - 10f))
                        {
                            if (Mathf.Abs(dx - 70f) < 2f || dy > (h * 0.5f - 13f) || dy < 2f)
                            {
                                SetPixel(c, x, y, w, slotBorder);
                            }
                            else
                            {
                                SetPixel(c, x, y, w, bg + new Color(0.04f, 0.03f, 0.06f, 0f));
                            }
                        }
                        else
                        {
                            SetPixel(c, x, y, w, bg);
                        }
                    }
                }
            }
        }
    }

    private static void DrawArrowChevron(Color[] c, int ox, int oy, int w, int h, bool isLeft)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color goldRim = new Color(0.96f, 0.82f, 0.35f, 1f);
        Color goldHi = new Color(1.0f, 0.96f, 0.70f, 1f);
        Color bgDark = new Color(0.18f, 0.12f, 0.22f, 0.92f);
        Color chevronGold = new Color(1.0f, 0.90f, 0.45f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= w * 0.44f)
                {
                    if (d >= w * 0.44f - 3f)
                    {
                        SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                    }
                    else
                    {
                        SetPixel(c, x, y, w, bgDark);

                        float arrowX = isLeft ? -dx : dx;
                        float arrowDist = Mathf.Abs(dy) * 0.85f;
                        if (arrowX >= -12f && arrowX <= 12f)
                        {
                            float tipX = 8f - arrowDist;
                            if (arrowX <= tipX && arrowX >= tipX - 8f && Mathf.Abs(dy) < 24f)
                            {
                                SetPixel(c, x, y, w, chevronGold);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void DrawRuneCircle(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.46f; float rMid = w * 0.36f; float rInner = w * 0.26f;
        Color goldGlow = new Color(1.0f, 0.85f, 0.25f, 0.95f);
        Color runeLines = new Color(1.0f, 0.92f, 0.50f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (Mathf.Abs(d - rOuter) < 4f || Mathf.Abs(d - rMid) < 3f || Mathf.Abs(d - rInner) < 3f)
                {
                    SetPixel(c, x, y, w, goldGlow);
                }
                else if (d > rMid && d < rOuter && (Mathf.Abs(Mathf.Sin(Mathf.Atan2(dy, dx) * 12f)) > 0.92f))
                {
                    SetPixel(c, x, y, w, runeLines);
                }
                else if (d <= rMid)
                {
                    float edgeDist = PointToTriangleEdgeDistance(dx, dy, rMid * 0.85f);
                    if (edgeDist < 2.5f) SetPixel(c, x, y, w, runeLines);
                }
            }
        }
    }

    private static float PointToTriangleEdgeDistance(float px, float py, float r)
    {
        Vector2 p = new Vector2(px, py);
        Vector2 v1 = new Vector2(0, r);
        Vector2 v2 = new Vector2(-r * 0.866f, -r * 0.5f);
        Vector2 v3 = new Vector2(r * 0.866f, -r * 0.5f);

        float d1 = DistToSegment(p, v1, v2);
        float d2 = DistToSegment(p, v2, v3);
        float d3 = DistToSegment(p, v3, v1);
        return Mathf.Min(d1, Mathf.Min(d2, d3));
    }

    private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + t * ab);
    }

    private static void DrawBroadcastBanner(Color[] c, int ox, int oy, int w, int h)
    {
        Color goldRim = new Color(0.88f, 0.74f, 0.32f, 1f);
        Color goldHi = new Color(1.0f, 0.90f, 0.55f, 1f);
        Color bgTrans = new Color(0.08f, 0.07f, 0.10f, 0.88f);
        float cx = w * 0.5f; float cy = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Cartouche with tapered decorative tips at left and right edges
                float endCut = Mathf.Max(0f, (Mathf.Abs(x - cx) - (cx - 40f)));
                float allowedY = (cy - endCut * 0.45f);
                if (Mathf.Abs(y - cy) <= allowedY)
                {
                    if (Mathf.Abs(y - cy) >= allowedY - 4f || Mathf.Abs(x - cx) >= cx - 6f)
                    {
                        SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                    }
                    else if (Mathf.Abs(y - cy) >= allowedY - 7f)
                    {
                        SetPixel(c, x, y, w, new Color(0.35f, 0.25f, 0.10f, 0.9f));
                    }
                    else
                    {
                        SetPixel(c, x, y, w, bgTrans);
                    }
                }
            }
        }
    }

    private static void DrawSquadMember(Color[] c, int ox, int oy, int w, int h, Color elementColor, int seed)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color rim = new Color(0.88f, 0.74f, 0.32f, 1f);
        Color skin = new Color(0.96f, 0.88f, 0.82f, 1f);
        Color eyeColor = (seed % 2 == 0) ? new Color(0.5f, 0.75f, 1.0f) : new Color(0.95f, 0.45f, 0.35f);

        Color[] hairColors = {
            new Color(0.12f, 0.12f, 0.16f), // 1. Black
            new Color(0.15f, 0.45f, 0.30f), // 2. Emerald
            new Color(0.70f, 0.18f, 0.18f), // 3. Crimson
            new Color(0.85f, 0.85f, 0.92f), // 4. Silver White
            new Color(0.18f, 0.28f, 0.55f), // 5. Navy Blue
            new Color(0.88f, 0.75f, 0.25f), // 6. Blonde
            new Color(0.55f, 0.25f, 0.65f), // 7. Violet
            new Color(0.20f, 0.65f, 0.65f), // 8. Teal
            new Color(0.85f, 0.40f, 0.55f)  // 9. Rose
        };
        Color hair = hairColors[seed % hairColors.Length];

        float maxHalfW = w * 0.46f;
        float maxHalfH = h * 0.46f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy;
                // Slanted Persona cut-in angle
                float slantX = dx - (dy * 0.16f);
                if (Mathf.Abs(slantX) <= maxHalfW && Mathf.Abs(dy) <= maxHalfH)
                {
                    if (Mathf.Abs(slantX) >= maxHalfW - 3.5f || Mathf.Abs(dy) >= maxHalfH - 3.5f)
                    {
                        SetPixel(c, x, y, w, rim);
                    }
                    else
                    {
                        // Elemental split background
                        Color bg = (slantX + dy > 5f) ? (elementColor * 0.45f + new Color(0.08f, 0.08f, 0.12f)) : new Color(0.10f, 0.11f, 0.15f, 0.95f);
                        SetPixel(c, x, y, w, bg);

                        // Torso / Collar
                        if (x >= cx - 22 && x <= cx + 22 && y < cy - 14)
                        {
                            SetPixel(c, x, y, w, new Color(0.14f, 0.14f, 0.20f));
                        }

                        // Face oval
                        float fdx = x - cx; float fdy = (y - (cy + 2)) * 0.9f;
                        if (Mathf.Sqrt(fdx * fdx + fdy * fdy) < 22f)
                        {
                            SetPixel(c, x, y, w, skin);
                            // Eyes
                            if ((x >= cx - 11 && x <= cx - 5 || x >= cx + 5 && x <= cx + 11) && y >= cy + 4 && y <= cy + 8)
                            {
                                SetPixel(c, x, y, w, eyeColor);
                            }
                        }

                        // Hair
                        float hdx = x - cx; float hdy = (y - (cy + 12)) * 0.85f;
                        if (Mathf.Sqrt(hdx * hdx + hdy * hdy) < 28f && (y > cy + 8 || Mathf.Abs(x - cx) > 15))
                        {
                            SetPixel(c, x, y, w, hair);
                        }

                        // Element badge at corner
                        float bdx = x - (cx - 18); float bdy = y - (cy - 18);
                        float bd = Mathf.Sqrt(bdx * bdx + bdy * bdy);
                        if (bd <= 10f)
                        {
                            SetPixel(c, x, y, w, (bd >= 8.5f) ? rim : elementColor);
                        }
                    }
                }
            }
        }
    }

    private static void DrawJoystickRing(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.46f; float rInner = w * 0.38f;
        Color ringColor = new Color(0.85f, 0.85f, 0.90f, 0.45f);
        Color ringBg = new Color(0.08f, 0.10f, 0.15f, 0.35f);
        Color arrowColor = new Color(0.95f, 0.95f, 1.0f, 0.65f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    if (d >= rInner)
                    {
                        SetPixel(c, x, y, w, ringColor);
                    }
                    else
                    {
                        SetPixel(c, x, y, w, ringBg);
                        // Directional triangle arrows at 4 cardinal directions
                        // Up
                        if (Mathf.Abs(dx) < (cy + rInner - 8f - y) * 0.6f && y >= cy + rInner - 24f && y <= cy + rInner - 8f)
                            SetPixel(c, x, y, w, arrowColor);
                        // Down
                        if (Mathf.Abs(dx) < (y - (cy - rInner + 8f)) * 0.6f && y >= cy - rInner + 8f && y <= cy - rInner + 24f)
                            SetPixel(c, x, y, w, arrowColor);
                        // Left
                        if (Mathf.Abs(dy) < (x - (cx - rInner + 8f)) * 0.6f && x >= cx - rInner + 8f && x <= cx - rInner + 24f)
                            SetPixel(c, x, y, w, arrowColor);
                        // Right
                        if (Mathf.Abs(dy) < (cx + rInner - 8f - x) * 0.6f && x >= cx + rInner - 24f && x <= cx + rInner - 8f)
                            SetPixel(c, x, y, w, arrowColor);
                    }
                }
            }
        }
    }

    private static void DrawJoystickHandle(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f; float rOuter = w * 0.44f;
        Color rim = new Color(0.95f, 0.95f, 1.0f, 0.75f);
        Color body = new Color(0.20f, 0.22f, 0.28f, 0.85f);
        Color centerPuck = new Color(0.40f, 0.45f, 0.55f, 0.9f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= rOuter)
                {
                    if (d >= rOuter - 3f) SetPixel(c, x, y, w, rim);
                    else if (d <= rOuter * 0.45f) SetPixel(c, x, y, w, centerPuck);
                    else SetPixel(c, x, y, w, body);
                }
            }
        }
    }

    private static void DrawJumpButton(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color goldRim = new Color(0.96f, 0.82f, 0.35f, 1f);
        Color goldHi = new Color(1.0f, 0.96f, 0.70f, 1f);
        Color goldDark = new Color(0.42f, 0.30f, 0.10f, 1f);
        Color bgDark = new Color(0.12f, 0.10f, 0.16f, 0.95f);
        Color bgInner = new Color(0.20f, 0.16f, 0.28f, 0.95f);
        Color chevronGold = new Color(1.0f, 0.90f, 0.45f, 1f);
        Color wingGold = new Color(0.85f, 0.75f, 0.35f, 0.85f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= w * 0.46f)
                {
                    if (d >= w * 0.46f - 4f)
                    {
                        SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                    }
                    else if (d >= w * 0.46f - 6.5f)
                    {
                        SetPixel(c, x, y, w, goldDark);
                    }
                    else
                    {
                        SetPixel(c, x, y, w, Color.Lerp(bgDark, bgInner, (float)y / h));

                        // Upward Jump Chevron (raised slightly above center for label)
                        float arrowY = dy - 5f;
                        float arrowDist = Mathf.Abs(dx) * 0.85f;
                        if (arrowY >= -10f && arrowY <= 22f)
                        {
                            float tipY = 22f - arrowDist;
                            if (arrowY <= tipY && arrowY >= tipY - 8f && Mathf.Abs(dx) < 24f)
                            {
                                SetPixel(c, x, y, w, chevronGold);
                            }
                        }

                        // Side winged arcs
                        if (d > w * 0.22f && d < w * 0.36f && dy > -12f && dy < 14f && Mathf.Abs(dx) > 10f)
                        {
                            if (Mathf.Abs(Mathf.Abs(dx) - 18f) < 2.5f)
                            {
                                SetPixel(c, x, y, w, wingGold);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void DrawScopeButton(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color cyanRim = new Color(0.20f, 0.90f, 1.0f, 1f);
        Color cyanHi = new Color(0.60f, 0.96f, 1.0f, 1f);
        Color rimDark = new Color(0.08f, 0.25f, 0.35f, 1f);
        Color bgDark = new Color(0.06f, 0.08f, 0.14f, 0.95f);
        Color bgInner = new Color(0.10f, 0.15f, 0.24f, 0.95f);
        Color reticleCyan = new Color(0.25f, 0.95f, 1.0f, 1f);
        Color goldDot = new Color(1.0f, 0.85f, 0.35f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= w * 0.46f)
                {
                    if (d >= w * 0.46f - 4f)
                    {
                        SetPixel(c, x, y, w, Color.Lerp(cyanRim, cyanHi, (float)y / h));
                    }
                    else if (d >= w * 0.46f - 6.5f)
                    {
                        SetPixel(c, x, y, w, rimDark);
                    }
                    else
                    {
                        SetPixel(c, x, y, w, Color.Lerp(bgDark, bgInner, (float)y / h));

                        // Concentric Rangefinder Ring
                        float scopeY = dy - 6f; // raised slightly for label
                        float scopeD = Mathf.Sqrt(dx * dx + scopeY * scopeY);
                        if (Mathf.Abs(scopeD - 22f) < 1.8f)
                        {
                            SetPixel(c, x, y, w, reticleCyan);
                        }

                        // 4 Crosshair Lines with central gap
                        if (scopeD > 6f && scopeD < 32f)
                        {
                            if (Mathf.Abs(dx) < 1.4f || Mathf.Abs(scopeY) < 1.4f)
                            {
                                SetPixel(c, x, y, w, reticleCyan);
                            }
                        }

                        // Center precision Pip
                        if (scopeD <= 3.2f)
                        {
                            SetPixel(c, x, y, w, goldDot);
                        }
                    }
                }
            }
        }
    }

    private static void DrawGrimoireButton(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color goldRim = new Color(0.96f, 0.82f, 0.35f, 1f);
        Color goldHi = new Color(1.0f, 0.96f, 0.70f, 1f);
        Color rimDark = new Color(0.42f, 0.30f, 0.10f, 1f);
        Color bgDark = new Color(0.14f, 0.08f, 0.18f, 0.95f);
        Color bgInner = new Color(0.24f, 0.12f, 0.30f, 0.95f);
        Color leatherCover = new Color(0.52f, 0.18f, 0.22f, 1f);
        Color goldSpine = new Color(0.95f, 0.80f, 0.35f, 1f);
        Color parchmentRibbon = new Color(0.95f, 0.90f, 0.75f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= w * 0.46f)
                {
                    if (d >= w * 0.46f - 4f)
                    {
                        SetPixel(c, x, y, w, Color.Lerp(goldRim, goldHi, (float)y / h));
                    }
                    else if (d >= w * 0.46f - 6.5f)
                    {
                        SetPixel(c, x, y, w, rimDark);
                    }
                    else
                    {
                        SetPixel(c, x, y, w, Color.Lerp(bgDark, bgInner, (float)y / h));

                        // Book silhouette (raised 6px for bottom label)
                        float by = dy - 6f;
                        if (Mathf.Abs(dx) < 22f && Mathf.Abs(by) < 24f)
                        {
                            // Leather book cover
                            SetPixel(c, x, y, w, leatherCover);

                            // Left spine ribs
                            if (dx >= -22f && dx <= -16f)
                            {
                                SetPixel(c, x, y, w, goldSpine);
                            }

                            // Book cover gold diamond rune
                            float runeDist = Mathf.Abs(dx - 2f) + Mathf.Abs(by);
                            if (Mathf.Abs(runeDist - 10f) < 1.6f || runeDist < 3f)
                            {
                                SetPixel(c, x, y, w, goldSpine);
                            }

                            // Bookmark ribbon hanging down
                            if (dx >= -4f && dx <= 0f && by < -12f && by > -28f)
                            {
                                SetPixel(c, x, y, w, parchmentRibbon);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void DrawCrosshairReticle(Color[] c, int ox, int oy, int w, int h)
    {
        float cx = w * 0.5f; float cy = h * 0.5f;
        Color reticleCyan = new Color(0.25f, 0.95f, 1.0f, 0.95f);
        Color glowCyan = new Color(0.10f, 0.70f, 0.95f, 0.45f);
        Color centerPip = new Color(1.0f, 0.90f, 0.35f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx; float dy = y - cy;
                float absX = Mathf.Abs(dx); float absY = Mathf.Abs(dy);

                // Center pip dot
                float centerDist = Mathf.Sqrt(dx * dx + dy * dy);
                if (centerDist <= 3.2f)
                {
                    SetPixel(c, x, y, w, centerPip);
                    continue;
                }

                // 4 Corner Brackets around [ -48 to 48 ]
                float bDist = 48f; float bLen = 18f; float bThick = 2.5f;
                bool isCornerH = (Mathf.Abs(absY - bDist) <= bThick && absX >= bDist - bLen && absX <= bDist);
                bool isCornerV = (Mathf.Abs(absX - bDist) <= bThick && absY >= bDist - bLen && absY <= bDist);

                if (isCornerH || isCornerV)
                {
                    SetPixel(c, x, y, w, reticleCyan);
                    continue;
                }

                // Mil-dot crosshair axis tick marks
                if ((absX < 1.5f && absY >= 14f && absY <= 78f) || (absY < 1.5f && absX >= 14f && absX <= 78f))
                {
                    // Gap between ticks
                    if (absX < 1.5f && (Mathf.Abs(absY - 28f) < 4f || Mathf.Abs(absY - 52f) < 4f || Mathf.Abs(absY - 74f) < 4f))
                    {
                        SetPixel(c, x, y, w, reticleCyan);
                    }
                    else if (absY < 1.5f && (Mathf.Abs(absX - 28f) < 4f || Mathf.Abs(absX - 52f) < 4f || Mathf.Abs(absX - 74f) < 4f))
                    {
                        SetPixel(c, x, y, w, reticleCyan);
                    }
                    else
                    {
                        SetPixel(c, x, y, w, glowCyan);
                    }
                }
            }
        }
    }

    private static void DrawGrimoireParchment(Color[] c, int ox, int oy, int w, int h)
    {
        Color leatherBorder = new Color(0.24f, 0.14f, 0.10f, 0.98f);
        Color leatherSpine = new Color(0.18f, 0.10f, 0.07f, 0.98f);
        Color goldFiligree = new Color(0.85f, 0.70f, 0.30f, 0.95f);
        Color pageL = new Color(0.96f, 0.91f, 0.80f, 0.98f);
        Color pageR = new Color(0.94f, 0.89f, 0.77f, 0.98f);
        Color lineFaint = new Color(0.82f, 0.76f, 0.65f, 0.60f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Outer Leather Border (14px)
                if (x < 14 || x >= w - 14 || y < 14 || y >= h - 14)
                {
                    SetPixel(c, x, y, w, leatherBorder);
                    // Gold corner filigree
                    if ((x < 24 && y < 24) || (x > w - 24 && y < 24) || (x < 24 && y > h - 24) || (x > w - 24 && y > h - 24))
                    {
                        SetPixel(c, x, y, w, goldFiligree);
                    }
                    continue;
                }

                // Center Spine Shadow (width = 20px)
                float spineDist = Mathf.Abs(x - w * 0.5f);
                if (spineDist < 10f)
                {
                    float t = spineDist / 10f;
                    SetPixel(c, x, y, w, Color.Lerp(leatherSpine, pageL, t));
                    continue;
                }

                // Left Page vs Right Page
                bool isLeft = x < w * 0.5f;
                Color basePage = isLeft ? pageL : pageR;

                // Subtle horizontal notebook ledger lines
                if (y % 28 == 0 && y > 30 && y < h - 30)
                {
                    SetPixel(c, x, y, w, Color.Lerp(basePage, lineFaint, 0.4f));
                }
                else
                {
                    SetPixel(c, x, y, w, basePage);
                }
            }
        }
    }

    private static TextMeshProUGUI CreateOrGetTMP(Transform parent, string name, string text, float x, float y, float w, float h, int fontSize, TextAlignmentOptions align, Color color)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            var tmp = existing.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                return tmp;
            }
        }
        return CreateTMPLabel(parent, name, text, x, y, w, h, fontSize, align, color);
    }

    private static void SetPixel(Color[] c, int x, int y, int w, Color color)
    {
        if (x >= 0 && x < w && y >= 0 && (y * w + x) < c.Length)
        {
            c[y * w + x] = color;
        }
    }
    #endregion

    #region Scene UI Configuration
    private static void ConfigureSceneUI()
    {
        string scenePath = "Assets/Scenes/MainDungeonFloor.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

        Canvas canvas = null;
        var portraitCanvasObj = GameObject.Find("PortraitHUDCanvas");
        if (portraitCanvasObj != null) canvas = portraitCanvasObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            var allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (int i = 0; i < allCanvases.Length; i++)
            {
                if (allCanvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = allCanvases[i];
                    break;
                }
            }
        }
        if (canvas == null) canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MOCKUP LAYOUT] No Canvas found in scene!");
            return;
        }

        Transform canvasTrans = canvas.transform;

        // Ensure Player_Hero has AutoBattlerAIController
        var playerObj = GameObject.Find("Player_Hero");
        if (playerObj != null)
        {
            var autoAI = playerObj.GetComponent<AutoBattlerAIController>();
            if (autoAI == null) autoAI = playerObj.AddComponent<AutoBattlerAIController>();
            autoAI.ResolveReferences();
        }

        // Clean up or find MockupMasterHUD
        GameObject hudRoot = GameObject.Find("MockupMasterHUD");
        if (hudRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(hudRoot);
        }

        hudRoot = new GameObject("MockupMasterHUD", typeof(RectTransform), typeof(CanvasGroup));
        hudRoot.transform.SetParent(canvasTrans, false);

        RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.sizeDelta = Vector2.zero;
        hudRect.anchoredPosition = Vector2.zero;

        var cg = hudRoot.GetComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // 1. Build Top Left: Settings Button
        GameObject settingsBtnObj = CreateButton(hudRoot.transform, "SettingsButton", 0f, 0f, 0f, 0f, new Vector2(0f, 1f), "Mockup_SettingsGear.png", new Vector2(0f, 1f));
        RectTransform settingsRt = settingsBtnObj.GetComponent<RectTransform>();
        settingsRt.anchorMin = new Vector2(0f, 1f);
        settingsRt.anchorMax = new Vector2(0f, 1f);
        settingsRt.pivot = new Vector2(0f, 1f);
        settingsRt.anchoredPosition = new Vector2(24f, -20f);
        settingsRt.sizeDelta = new Vector2(62f, 62f);
        Image settingsImg = settingsBtnObj.GetComponent<Image>();
        if (settingsImg != null) settingsImg.preserveAspect = true;

        // 2. Build Top Left: Auto Battler AI Button directly UNDER Settings button!
        GameObject aiAutoBtnObj = CreateButton(hudRoot.transform, "AutoBattlerAIButton", 0f, 0f, 0f, 0f, new Vector2(0f, 1f), "Mockup_AIAutoRobot.png", new Vector2(0f, 1f));
        RectTransform aiAutoRt = aiAutoBtnObj.GetComponent<RectTransform>();
        aiAutoRt.anchorMin = new Vector2(0f, 1f);
        aiAutoRt.anchorMax = new Vector2(0f, 1f);
        aiAutoRt.pivot = new Vector2(0f, 1f);
        aiAutoRt.anchoredPosition = new Vector2(24f, -92f);
        aiAutoRt.sizeDelta = new Vector2(62f, 62f);
        Image aiAutoImg = aiAutoBtnObj.GetComponent<Image>();
        if (aiAutoImg != null) aiAutoImg.preserveAspect = true;
        CreateTMPLabel(aiAutoBtnObj.transform, "AIAutoLabel", "AI Auto", 31f, -42f, 80f, 18f, 11, TextAlignmentOptions.Center, new Color(0.95f, 0.85f, 0.45f));

        // 3. Build Top Left: Player Stats Frame (Portrait, LV.12, CBP bar, HP/MP bars)
        GameObject statsFrameObj = new GameObject("PlayerStatsFrame", typeof(RectTransform), typeof(Image));
        statsFrameObj.transform.SetParent(hudRoot.transform, false);
        RectTransform statsRect = statsFrameObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 1f);
        statsRect.anchorMax = new Vector2(0f, 1f);
        statsRect.pivot = new Vector2(0f, 1f);
        statsRect.anchoredPosition = new Vector2(24f, -168f);
        statsRect.sizeDelta = new Vector2(360f, 112f);
        Image statsBg = statsFrameObj.GetComponent<Image>();
        statsBg.color = new Color(0.08f, 0.07f, 0.12f, 0.75f);

        // Gold border matting behind portrait
        GameObject portraitBorder = CreateImage(statsFrameObj.transform, "PortraitBorder", 44f, -44f, 82f, 82f, new Vector2(0f, 1f), null, new Color(0.85f, 0.70f, 0.25f, 0.90f), new Vector2(0.5f, 0.5f));
        portraitBorder.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, -5.5f);

        // Character Portrait with Persona -5.5 degree tilt
        GameObject portraitObj = CreateImage(statsFrameObj.transform, "CharacterPortrait", 44f, -44f, 76f, 76f, new Vector2(0f, 1f), "Mockup_GothicHeroinePortrait.png", null, new Vector2(0.5f, 0.5f));
        RectTransform pfpRt = portraitObj.GetComponent<RectTransform>();
        Image pfpImg = portraitObj.GetComponent<Image>();
        pfpImg.preserveAspect = true;
        pfpRt.localEulerAngles = new Vector3(0f, 0f, -5.5f);

        // LV. 12 Badge directly below portrait
        GameObject lvlBadgeObj = CreateImage(statsFrameObj.transform, "LevelBadge", 44f, -94f, 64f, 20f, new Vector2(0f, 1f), null, new Color(0.12f, 0.10f, 0.18f, 0.95f), new Vector2(0.5f, 0.5f));
        CreateTMPLabel(lvlBadgeObj.transform, "LevelText", "LV. 12", 0f, 0f, 64f, 18f, 12, TextAlignmentOptions.Center, new Color(0.98f, 0.88f, 0.40f));

        // Resource Bars (HP / MP) on the right side of stats frame
        GameObject hpBarObj = CreateResourceBar(statsFrameObj.transform, "PlayerHPBar", 134f, -26f, 216f, 22f, new Color(0.88f, 0.18f, 0.24f), "HP", "1220 / 1350");
        GameObject mpBarObj = CreateResourceBar(statsFrameObj.transform, "PlayerMPBar", 134f, -56f, 216f, 20f, new Color(0.15f, 0.65f, 0.98f), "MP", "720 / 750");

        // CBP bar directly below MP bar
        GameObject cbpObj = CreateImage(statsFrameObj.transform, "CBPBar", 106f, -80f, 244f, 7f, new Vector2(0f, 1f), null, new Color(0.08f, 0.08f, 0.12f, 0.9f), new Vector2(0f, 0.5f));
        CreateImage(cbpObj.transform, "CBPFill", 0f, 0f, 190f, 5f, new Vector2(0f, 0.5f), null, new Color(0.2f, 0.88f, 0.40f, 1f), new Vector2(0f, 0.5f));

        // Camera Snapshot Button directly UNDER PlayerStatsFrame
        GameObject cameraBtnObj = CreateButton(hudRoot.transform, "CameraSnapshotButton", 0f, 0f, 0f, 0f, new Vector2(0f, 1f), "Mockup_CameraSnap.png", new Vector2(0f, 1f));
        RectTransform camRt = cameraBtnObj.GetComponent<RectTransform>();
        camRt.anchorMin = new Vector2(0f, 1f);
        camRt.anchorMax = new Vector2(0f, 1f);
        camRt.pivot = new Vector2(0f, 1f);
        camRt.anchoredPosition = new Vector2(24f, -290f);
        camRt.sizeDelta = new Vector2(58f, 58f);
        Image camImg = cameraBtnObj.GetComponent<Image>();
        if (camImg != null) camImg.preserveAspect = true;
        CreateTMPLabel(cameraBtnObj.transform, "CameraLabel", "Camera", 29f, -38f, 80f, 16f, 10, TextAlignmentOptions.Center, new Color(0.90f, 0.85f, 0.70f));

        // 4. Center Top: Spire Broadcast Banner (Slim, elegant ribbon with bold legible text)
        GameObject bannerObj = CreateImage(hudRoot.transform, "SpireBroadcastBanner", 0f, 0f, 0f, 0f, new Vector2(0.5f, 1f), "Mockup_BroadcastBanner.png", null, new Vector2(0.5f, 1f));
        RectTransform bRt = bannerObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.5f, 1f);
        bRt.anchorMax = new Vector2(0.5f, 1f);
        bRt.pivot = new Vector2(0.5f, 1f);
        bRt.anchoredPosition = new Vector2(0f, -20f);
        bRt.sizeDelta = new Vector2(620f, 42f);
        Image bImg = bannerObj.GetComponent<Image>();
        if (bImg != null)
        {
            bImg.color = new Color(0.10f, 0.08f, 0.16f, 0.92f);
        }
        var bTxt = CreateTMPLabel(bannerObj.transform, "BroadcastText", "<b>SPIRE BROADCAST:</b> Player extracted a rare <color=#FFD700>[Prismatic Heart]</color>", 0f, 0f, 600f, 36f, 16, TextAlignmentOptions.Center, new Color(0.96f, 0.90f, 0.76f));
        bTxt.textWrappingMode = TextWrappingModes.NoWrap;
        bTxt.overflowMode = TextOverflowModes.Ellipsis;

        // 5. Top Right: Minimap Radar (Crisp 1:1 true circle)
        GameObject miniMapObj = CreateButton(hudRoot.transform, "MinimapFrame", 0f, 0f, 0f, 0f, new Vector2(1f, 1f), "Mockup_MinimapRadar.png", new Vector2(1f, 1f));
        RectTransform mapRt = miniMapObj.GetComponent<RectTransform>();
        mapRt.anchorMin = new Vector2(1f, 1f);
        mapRt.anchorMax = new Vector2(1f, 1f);
        mapRt.pivot = new Vector2(1f, 1f);
        mapRt.anchoredPosition = new Vector2(-24f, -20f);
        mapRt.sizeDelta = new Vector2(144f, 144f);
        Image mapImg = miniMapObj.GetComponent<Image>();
        if (mapImg != null) mapImg.preserveAspect = true;

        // Messenger Bag button below Minimap
        GameObject bagObj = CreateButton(hudRoot.transform, "MessengerBagButton", 0f, 0f, 0f, 0f, new Vector2(1f, 1f), "Mockup_MessengerBag.png", new Vector2(0.5f, 0.5f));
        RectTransform bagRt = bagObj.GetComponent<RectTransform>();
        bagRt.anchorMin = new Vector2(1f, 1f);
        bagRt.anchorMax = new Vector2(1f, 1f);
        bagRt.pivot = new Vector2(0.5f, 0.5f);
        bagRt.anchoredPosition = new Vector2(-96f, -208f);
        bagRt.sizeDelta = new Vector2(68f, 68f);
        Image bagImg = bagObj.GetComponent<Image>();
        if (bagImg != null) bagImg.preserveAspect = true;
        CreateTMPLabel(bagObj.transform, "BagLabel", "Messenger Bag", 0f, -44f, 110f, 18f, 11, TextAlignmentOptions.Center, new Color(0.92f, 0.86f, 0.72f));

        // 6. Right Edge: Party Squad Vertical Column (9 Members) - Enlarged Ergonomic Touch Targets
        GameObject squadContainer = new GameObject("PartySquadColumn", typeof(RectTransform));
        squadContainer.transform.SetParent(hudRoot.transform, false);
        RectTransform sqRect = squadContainer.GetComponent<RectTransform>();
        sqRect.anchorMin = new Vector2(0.80f, 0.16f);
        sqRect.anchorMax = new Vector2(0.99f, 0.77f);
        sqRect.offsetMin = Vector2.zero;
        sqRect.offsetMax = Vector2.zero;
        sqRect.anchoredPosition = Vector2.zero;

        GridLayoutGroup allyGrid = squadContainer.AddComponent<GridLayoutGroup>();
        allyGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        allyGrid.constraintCount = 1;
        allyGrid.cellSize = new Vector2(130f, 72f);
        allyGrid.spacing = new Vector2(0f, 7f);
        allyGrid.childAlignment = TextAnchor.UpperCenter;

        Image[] allyPortraitImages = new Image[9];
        string[] squadNames = new string[] { "Aqua", "Ignis", "Zephyr", "Sylph", "Terra", "Volt", "Glacies", "Umbra", "Aura" };
        for (int i = 0; i < 9; i++)
        {
            GameObject mObj = new GameObject($"SquadMember_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            mObj.transform.SetParent(squadContainer.transform, false);
            RectTransform mRt = mObj.GetComponent<RectTransform>();
            mRt.sizeDelta = new Vector2(130f, 72f);

            // Card background plate: sleek translucent dark plate
            Image bgImg = mObj.GetComponent<Image>();
            bgImg.color = new Color(0.12f, 0.09f, 0.16f, 0.88f);

            // Large Ally Portrait (58x58) with -5.5 degree dynamic comic tilt
            GameObject pfpChild = CreateImage(mObj.transform, "AllyPortrait", -33f, 0f, 58f, 58f, new Vector2(0.5f, 0.5f), $"Mockup_Squad_{i + 1}.png", null, new Vector2(0.5f, 0.5f));
            Image pImg = pfpChild.GetComponent<Image>();
            pImg.preserveAspect = true;
            pImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, -5.5f);
            allyPortraitImages[i] = pImg;

            // Member Name Tag
            string allyName = (i < squadNames.Length) ? squadNames[i] : $"Ally {i + 1}";
            CreateTMPLabel(mObj.transform, "AllyName", allyName, 28f, 18f, 62f, 16f, 11, TextAlignmentOptions.Left, new Color(0.95f, 0.88f, 0.70f));

            // Mini HP Bar (54x9)
            GameObject hpObj = CreateImage(mObj.transform, "MiniHP", 28f, 2f, 54f, 9f, new Vector2(0.5f, 0.5f), null, new Color(0.08f, 0.06f, 0.12f, 0.95f), new Vector2(0.5f, 0.5f));
            CreateImage(hpObj.transform, "MiniHPFill", 0f, 0f, 50f, 7f, new Vector2(0.5f, 0.5f), null, new Color(0.18f, 0.85f, 0.40f, 1f));

            // Mini MP Bar (54x7)
            GameObject mpObj = CreateImage(mObj.transform, "MiniMP", 28f, -12f, 54f, 7f, new Vector2(0.5f, 0.5f), null, new Color(0.08f, 0.06f, 0.12f, 0.95f), new Vector2(0.5f, 0.5f));
            CreateImage(mpObj.transform, "MiniMPFill", 0f, 0f, 46f, 5f, new Vector2(0.5f, 0.5f), null, new Color(0.20f, 0.70f, 0.95f, 1f));
        }

        // 7. Casting Zone: Cast Icon Disc and Geometric Magic Circle
        GameObject castBtnObj = CreateButton(hudRoot.transform, "CastActionButton", 0f, 0f, 0f, 0f, new Vector2(0.60f, 0.25f), "Mockup_CastDisc.png", new Vector2(0.5f, 0.5f));
        RectTransform castRect = castBtnObj.GetComponent<RectTransform>();
        castRect.anchorMin = new Vector2(0.60f, 0.25f);
        castRect.anchorMax = new Vector2(0.75f, 0.32f);
        castRect.offsetMin = Vector2.zero;
        castRect.offsetMax = Vector2.zero;
        castRect.anchoredPosition = Vector2.zero;
        Image castImg = castBtnObj.GetComponent<Image>();
        if (castImg != null) castImg.preserveAspect = true;
        CreateTMPLabel(castBtnObj.transform, "CastLabel", "CAST", 0f, 0f, 120f, 32f, 18, TextAlignmentOptions.Center, new Color(1f, 0.94f, 0.65f));

        // Geometric Magic Circle directly beneath CAST button
        GameObject runeCircleObj = CreateImage(hudRoot.transform, "MockupRuneCircleVisual", 0f, 0f, 0f, 0f, new Vector2(0.675f, 0.15f), "Mockup_RuneCircle.png", null, new Vector2(0.5f, 0.5f));
        RectTransform runeRect = runeCircleObj.GetComponent<RectTransform>();
        runeRect.anchorMin = new Vector2(0.53f, 0.05f);
        runeRect.anchorMax = new Vector2(0.82f, 0.24f);
        runeRect.offsetMin = Vector2.zero;
        runeRect.offsetMax = Vector2.zero;
        runeRect.anchoredPosition = Vector2.zero;
        Image runeImg = runeCircleObj.GetComponent<Image>();
        if (runeImg != null) runeImg.preserveAspect = true;

        // Dispel Button: Bottom-left directly above movement controls
        GameObject dispelBtnObj = CreateButton(hudRoot.transform, "DispelSpellButton", 0f, 0f, 0f, 0f, new Vector2(0.04f, 0.22f), "Mockup_DispelButton.png", new Vector2(0.5f, 0.5f));
        RectTransform dispelRt = dispelBtnObj.GetComponent<RectTransform>();
        dispelRt.anchorMin = new Vector2(0.04f, 0.22f);
        dispelRt.anchorMax = new Vector2(0.36f, 0.29f);
        dispelRt.offsetMin = Vector2.zero;
        dispelRt.offsetMax = Vector2.zero;
        dispelRt.anchoredPosition = Vector2.zero;
        CreateTMPLabel(dispelBtnObj.transform, "DispelLabel", "DISPEL", 12f, 0f, 140f, 28f, 16, TextAlignmentOptions.Center, new Color(1f, 0.90f, 0.90f));
        dispelBtnObj.SetActive(false); // starts hidden until spell is primed

        // 8. Quick Item Carousel: Right above controls in bottom middle of screen with left/right arrows!
        GameObject carouselRoot = new GameObject("QuickItemCarousel", typeof(RectTransform), typeof(Image));
        carouselRoot.transform.SetParent(hudRoot.transform, false);
        RectTransform carouselRt = carouselRoot.GetComponent<RectTransform>();
        carouselRt.anchorMin = new Vector2(0.35f, 0.22f);
        carouselRt.anchorMax = new Vector2(0.65f, 0.29f);
        carouselRt.offsetMin = Vector2.zero;
        carouselRt.offsetMax = Vector2.zero;
        carouselRt.anchoredPosition = Vector2.zero;
        Image carouselBg = carouselRoot.GetComponent<Image>();
        carouselBg.sprite = GetSprite("Mockup_CarouselFrame.png");
        carouselBg.type = Image.Type.Simple;
        carouselBg.color = Color.white;

        // Left Arrow Button (<)
        GameObject leftArrowObj = CreateButton(carouselRoot.transform, "ArrowLeftButton", 0f, 0f, 0f, 0f, new Vector2(0.03f, 0.5f), "Mockup_ArrowLeft.png", new Vector2(0.5f, 0.5f));
        RectTransform leftRt = leftArrowObj.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0.03f, 0.12f);
        leftRt.anchorMax = new Vector2(0.20f, 0.88f);
        leftRt.offsetMin = Vector2.zero;
        leftRt.offsetMax = Vector2.zero;
        Image leftImg = leftArrowObj.GetComponent<Image>();
        if (leftImg != null) leftImg.preserveAspect = true;

        // Active Item Slot Button (Center)
        GameObject itemSlotObj = CreateButton(carouselRoot.transform, "ActiveItemButton", 0f, 0f, 0f, 0f, new Vector2(0.5f, 0.5f), null, new Vector2(0.5f, 0.5f));
        RectTransform itemSlotRt = itemSlotObj.GetComponent<RectTransform>();
        itemSlotRt.anchorMin = new Vector2(0.22f, 0.05f);
        itemSlotRt.anchorMax = new Vector2(0.78f, 0.95f);
        itemSlotRt.offsetMin = Vector2.zero;
        itemSlotRt.offsetMax = Vector2.zero;
        Image slotImg = itemSlotObj.GetComponent<Image>();
        slotImg.color = new Color(0, 0, 0, 0.005f);

        // Item Icon Image
        GameObject iconObj = CreateImage(itemSlotObj.transform, "ActiveItemIcon", 0f, 6f, 0f, 0f, new Vector2(0.5f, 0.6f), "Mockup_PotionRed.png", null, new Vector2(0.5f, 0.5f));
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.15f, 0.28f);
        iconRt.anchorMax = new Vector2(0.85f, 0.96f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;

        // Item Count Badge (x5)
        TextMeshProUGUI countTMP = CreateTMPLabel(itemSlotObj.transform, "ItemCountText", "x5", 0f, 0f, 0f, 0f, 13, TextAlignmentOptions.Right, new Color(1f, 0.95f, 0.55f));
        RectTransform countRt = countTMP.rectTransform;
        countRt.anchorMin = new Vector2(0.50f, 0.22f);
        countRt.anchorMax = new Vector2(0.92f, 0.52f);
        countRt.offsetMin = Vector2.zero;
        countRt.offsetMax = Vector2.zero;

        // Item Name Label
        TextMeshProUGUI nameTMP = CreateTMPLabel(itemSlotObj.transform, "ItemNameText", "Crimson Draught", 0f, 0f, 0f, 0f, 10, TextAlignmentOptions.Center, new Color(0.95f, 0.85f, 0.45f));
        RectTransform nameRt = nameTMP.rectTransform;
        nameRt.anchorMin = new Vector2(0.02f, 0.02f);
        nameRt.anchorMax = new Vector2(0.98f, 0.28f);
        nameRt.offsetMin = Vector2.zero;
        nameRt.offsetMax = Vector2.zero;

        // Right Arrow Button (>)
        GameObject rightArrowObj = CreateButton(carouselRoot.transform, "ArrowRightButton", 0f, 0f, 0f, 0f, new Vector2(0.97f, 0.5f), "Mockup_ArrowRight.png", new Vector2(0.5f, 0.5f));
        RectTransform rightRt = rightArrowObj.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.80f, 0.12f);
        rightRt.anchorMax = new Vector2(0.97f, 0.88f);
        rightRt.offsetMin = Vector2.zero;
        rightRt.offsetMax = Vector2.zero;
        Image rightImg = rightArrowObj.GetComponent<Image>();
        if (rightImg != null) rightImg.preserveAspect = true;

        // Attach & Configure QuickItemCarouselController
        QuickItemCarouselController itemCarousel = hudRoot.GetComponent<QuickItemCarouselController>();
        if (itemCarousel == null) itemCarousel = hudRoot.AddComponent<QuickItemCarouselController>();

        itemCarousel.BindReferences(
            leftArrowObj.GetComponent<Button>(),
            rightArrowObj.GetComponent<Button>(),
            itemSlotObj.GetComponent<Button>(),
            iconImg,
            countTMP,
            nameTMP,
            carouselRt
        );

        // 8b. Quick Z-Targeting Control Cluster (Right-hand side of Quick Item Carousel)
        GameObject targetingRoot = new GameObject("QuickZTargetingCluster", typeof(RectTransform), typeof(Image));
        targetingRoot.transform.SetParent(hudRoot.transform, false);
        RectTransform targetRt = targetingRoot.GetComponent<RectTransform>();
        targetRt.anchorMin = new Vector2(0.665f, 0.215f);
        targetRt.anchorMax = new Vector2(0.975f, 0.295f);
        targetRt.offsetMin = Vector2.zero;
        targetRt.offsetMax = Vector2.zero;
        targetRt.anchoredPosition = Vector2.zero;
        Image targetBg = targetingRoot.GetComponent<Image>();
        targetBg.color = new Color(0.08f, 0.09f, 0.13f, 0.90f);

        // Top Row: [ ◀ ] [ 🎯 Z-TARGET ] [ ▶ ]
        GameObject topRowObj = new GameObject("TopRow_ZLockAndArrows", typeof(RectTransform));
        topRowObj.transform.SetParent(targetingRoot.transform, false);
        RectTransform topRowRt = topRowObj.GetComponent<RectTransform>();
        topRowRt.anchorMin = new Vector2(0.03f, 0.48f);
        topRowRt.anchorMax = new Vector2(0.97f, 0.97f);
        topRowRt.offsetMin = Vector2.zero;
        topRowRt.offsetMax = Vector2.zero;

        // Left Arrow Button (<)
        GameObject prevBtnObj = CreateButton(topRowObj.transform, "PrevTargetArrowButton", 0f, 0f, 0f, 0f, new Vector2(0.12f, 0.5f), "Mockup_ArrowLeft.png", new Vector2(0.5f, 0.5f));
        RectTransform prevRt = prevBtnObj.GetComponent<RectTransform>();
        prevRt.anchorMin = new Vector2(0f, 0.05f);
        prevRt.anchorMax = new Vector2(0.22f, 0.95f);
        prevRt.offsetMin = Vector2.zero;
        prevRt.offsetMax = Vector2.zero;
        Image prevImg = prevBtnObj.GetComponent<Image>();
        if (prevImg != null) prevImg.preserveAspect = true;

        // Z-Target Main Button (Center)
        GameObject zTargetBtnObj = CreateButton(topRowObj.transform, "ZTargetFocusButton", 0f, 0f, 0f, 0f, new Vector2(0.5f, 0.5f), null, new Vector2(0.5f, 0.5f));
        RectTransform zBtnRt = zTargetBtnObj.GetComponent<RectTransform>();
        zBtnRt.anchorMin = new Vector2(0.25f, 0.05f);
        zBtnRt.anchorMax = new Vector2(0.75f, 0.95f);
        zBtnRt.offsetMin = Vector2.zero;
        zBtnRt.offsetMax = Vector2.zero;
        Image zBtnBg = zTargetBtnObj.GetComponent<Image>();
        zBtnBg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

        TextMeshProUGUI zBtnTMP = CreateTMPLabel(zTargetBtnObj.transform, "ZTargetText", "<b>Z-TARGET</b>", 0f, 0f, 0f, 0f, 13, TextAlignmentOptions.Center, Color.white);
        RectTransform zTxtRt = zBtnTMP.rectTransform;
        zTxtRt.anchorMin = Vector2.zero;
        zTxtRt.anchorMax = Vector2.one;
        zTxtRt.offsetMin = Vector2.zero;
        zTxtRt.offsetMax = Vector2.zero;

        // Right Arrow Button (>)
        GameObject nextBtnObj = CreateButton(topRowObj.transform, "NextTargetArrowButton", 0f, 0f, 0f, 0f, new Vector2(0.88f, 0.5f), "Mockup_ArrowRight.png", new Vector2(0.5f, 0.5f));
        RectTransform nextRt = nextBtnObj.GetComponent<RectTransform>();
        nextRt.anchorMin = new Vector2(0.78f, 0.05f);
        nextRt.anchorMax = new Vector2(1.0f, 0.95f);
        nextRt.offsetMin = Vector2.zero;
        nextRt.offsetMax = Vector2.zero;
        Image nextImg = nextBtnObj.GetComponent<Image>();
        if (nextImg != null) nextImg.preserveAspect = true;

        // Bottom Row: [ BOSS ] [ MOBS ]
        GameObject botRowObj = new GameObject("BottomRow_Toggles", typeof(RectTransform));
        botRowObj.transform.SetParent(targetingRoot.transform, false);
        RectTransform botRowRt = botRowObj.GetComponent<RectTransform>();
        botRowRt.anchorMin = new Vector2(0.03f, 0.03f);
        botRowRt.anchorMax = new Vector2(0.97f, 0.45f);
        botRowRt.offsetMin = Vector2.zero;
        botRowRt.offsetMax = Vector2.zero;

        // Boss Toggle Button
        GameObject bossBtnObj = CreateButton(botRowObj.transform, "BossToggleButton", 0f, 0f, 0f, 0f, new Vector2(0.24f, 0.5f), null, new Vector2(0.5f, 0.5f));
        RectTransform bossRt = bossBtnObj.GetComponent<RectTransform>();
        bossRt.anchorMin = new Vector2(0f, 0.05f);
        bossRt.anchorMax = new Vector2(0.48f, 0.95f);
        bossRt.offsetMin = Vector2.zero;
        bossRt.offsetMax = Vector2.zero;
        Image bossBg = bossBtnObj.GetComponent<Image>();
        bossBg.color = new Color(0.18f, 0.20f, 0.28f, 0.90f);

        TextMeshProUGUI bossTMP = CreateTMPLabel(bossBtnObj.transform, "BossText", "<b>BOSS</b>", 0f, 0f, 0f, 0f, 12, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.4f));
        RectTransform bTxtRt = bossTMP.rectTransform;
        bTxtRt.anchorMin = Vector2.zero;
        bTxtRt.anchorMax = Vector2.one;
        bTxtRt.offsetMin = Vector2.zero;
        bTxtRt.offsetMax = Vector2.zero;

        // Mobs Toggle Button
        GameObject mobsBtnObj = CreateButton(botRowObj.transform, "MobsToggleButton", 0f, 0f, 0f, 0f, new Vector2(0.76f, 0.5f), null, new Vector2(0.5f, 0.5f));
        RectTransform mobsRt = mobsBtnObj.GetComponent<RectTransform>();
        mobsRt.anchorMin = new Vector2(0.52f, 0.05f);
        mobsRt.anchorMax = new Vector2(1.0f, 0.95f);
        mobsRt.offsetMin = Vector2.zero;
        mobsRt.offsetMax = Vector2.zero;
        Image mobsBg = mobsBtnObj.GetComponent<Image>();
        mobsBg.color = new Color(0.18f, 0.20f, 0.28f, 0.90f);

        TextMeshProUGUI mobsTMP = CreateTMPLabel(mobsBtnObj.transform, "MobsText", "<b>MOBS</b>", 0f, 0f, 0f, 0f, 12, TextAlignmentOptions.Center, new Color(0.85f, 0.90f, 0.95f));
        RectTransform mTxtRt = mobsTMP.rectTransform;
        mTxtRt.anchorMin = Vector2.zero;
        mTxtRt.anchorMax = Vector2.one;
        mTxtRt.offsetMin = Vector2.zero;
        mTxtRt.offsetMax = Vector2.zero;

        // Attach & Configure QuickZTargetingController
        QuickZTargetingController zTargeting = hudRoot.GetComponent<QuickZTargetingController>();
        if (zTargeting == null) zTargeting = hudRoot.AddComponent<QuickZTargetingController>();

        zTargeting.BindReferences(
            zTargetBtnObj.GetComponent<Button>(),
            zBtnBg,
            zBtnTMP,
            bossBtnObj.GetComponent<Button>(),
            bossBg,
            bossTMP,
            mobsBtnObj.GetComponent<Button>(),
            mobsBg,
            mobsTMP,
            prevBtnObj.GetComponent<Button>(),
            nextBtnObj.GetComponent<Button>()
        );

        // Attach & Configure CameraSnapshotController
        CameraSnapshotController snapController = hudRoot.GetComponent<CameraSnapshotController>();
        if (snapController == null) snapController = hudRoot.AddComponent<CameraSnapshotController>();
        snapController.BindButton(cameraBtnObj.GetComponent<Button>());

        // 9. Purged Elements: Completely delete circular "Hold & Flick"
        var flickBtn = GameObject.Find("HoldAndFlickGestureButton");
        if (flickBtn != null) UnityEngine.Object.DestroyImmediate(flickBtn);

        // 10. Attach & Configure BattleViewportRebalancer
        BattleViewportRebalancer rebalancer = hudRoot.GetComponent<BattleViewportRebalancer>();
        if (rebalancer == null) rebalancer = hudRoot.AddComponent<BattleViewportRebalancer>();

        rebalancer.BindReferences(
            settingsRt,
            aiAutoRt,
            statsRect,
            pfpImg,
            sqRect,
            allyGrid,
            allyPortraitImages,
            castRect,
            null
        );

        // 11. Attach & Configure SpellCastingFlowController
        SpellCastingFlowController flowController = hudRoot.GetComponent<SpellCastingFlowController>();
        if (flowController == null) flowController = hudRoot.AddComponent<SpellCastingFlowController>();

        flowController.BindReferences(
            castRect,
            castBtnObj.GetComponent<Button>(),
            runeRect,
            dispelRt,
            dispelBtnObj.GetComponent<Button>(),
            castBtnObj.GetComponentInChildren<TextMeshProUGUI>(),
            dispelBtnObj.GetComponentInChildren<TextMeshProUGUI>()
        );

        // 9. Deactivate old telemetry HUD & overlapping banners to avoid overlap
        var oldTelemetry = GameObject.Find("TopTelemetryHUD");
        if (oldTelemetry != null) oldTelemetry.SetActive(false);

        var sysBanner = GameObject.Find("SystemBroadcastBanner");
        if (sysBanner != null) sysBanner.SetActive(false);

        var allyBanner = GameObject.Find("ActiveAllyTargetBanner");
        if (allyBanner != null) allyBanner.SetActive(false);

        var spellBanner = GameObject.Find("ActiveSpellBanner");
        if (spellBanner != null) spellBanner.SetActive(false);

        var devSwitcher = GameObject.Find("DevClassSwitcherBar");
        if (devSwitcher != null) devSwitcher.SetActive(false);

        var hint = GameObject.Find("CameraOrbitHintText");
        if (hint != null) hint.SetActive(false);

        var lookZone = GameObject.Find("TouchCameraLookZone");
        if (lookZone != null)
        {
            foreach (var t in lookZone.GetComponentsInChildren<UnityEngine.UI.Text>(true)) t.text = "";
            foreach (var tmp in lookZone.GetComponentsInChildren<TextMeshProUGUI>(true)) tmp.text = "";
        }

        var deck = GameObject.Find("BottomControlDeck");
        if (deck != null)
        {
            var img = deck.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = new Color(0, 0, 0, 0.002f);
        }

        // Style FloatingVirtualStick with circular joystick graphics
        var stickRoot = GameObject.Find("FloatingVirtualStick");
        if (stickRoot != null)
        {
            var rootImg = stickRoot.GetComponent<Image>();
            if (rootImg != null) rootImg.color = Color.clear;
        }

        var stickBg = GameObject.Find("StickBackground");
        if (stickBg != null)
        {
            var img = stickBg.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetSprite("Mockup_JoystickRing.png");
                img.color = Color.white;
            }
        }
        var stickHandle = GameObject.Find("StickHandle");
        if (stickHandle != null)
        {
            var img = stickHandle.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetSprite("Mockup_JoystickHandle.png");
                img.color = Color.white;
            }
        }

        // 11. Jump Button: Bottom right middle, bottom left of camera area
        var jumpBtnObj = GameObject.Find("JumpButton");
        if (jumpBtnObj == null)
        {
            Transform jumpParent = deck != null ? deck.transform : hudRoot.transform;
            jumpBtnObj = CreateButton(jumpParent, "JumpButton", 0f, 0f, 105f, 105f, new Vector2(0.595f, 0.34f), "Mockup_JumpButton.png", new Vector2(0.5f, 0.5f));
        }
        RectTransform jumpRt = jumpBtnObj.GetComponent<RectTransform>();
        if (deck != null)
        {
            jumpRt.SetParent(deck.transform, false);
            jumpRt.anchorMin = new Vector2(0.595f, 0.34f);
            jumpRt.anchorMax = new Vector2(0.595f, 0.34f);
            jumpRt.pivot = new Vector2(0.5f, 0.5f);
            jumpRt.anchoredPosition = Vector2.zero;
            jumpRt.sizeDelta = new Vector2(105f, 105f);
        }
        Image jumpImg = jumpBtnObj.GetComponent<Image>();
        if (jumpImg != null)
        {
            jumpImg.sprite = GetSprite("Mockup_JumpButton.png");
            jumpImg.preserveAspect = true;
            jumpImg.color = Color.white;
        }

        var existingJumpLabel = jumpBtnObj.transform.Find("JumpLabel");
        if (existingJumpLabel == null)
        {
            CreateTMPLabel(jumpBtnObj.transform, "JumpLabel", "<b>JUMP</b>", 0f, -22f, 90f, 24f, 11, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.55f));
        }

        Button jBtn = jumpBtnObj.GetComponent<Button>();
        if (jBtn != null)
        {
            var playerMv = UnityEngine.Object.FindAnyObjectByType<PlayerMovementController>();
            if (playerMv != null)
            {
                jBtn.onClick.RemoveAllListeners();
                jBtn.onClick.AddListener(playerMv.Jump);
            }
        }

        var drawerTab = GameObject.Find("ActionDrawerTabButton");
        if (drawerTab != null) drawerTab.SetActive(false);

        // 12. Scope ADS Button (Right next to Jump Button on BottomControlDeck)
        var scopeBtnObj = GameObject.Find("ScopeADSButton");
        if (scopeBtnObj == null)
        {
            Transform scopeParent = deck != null ? deck.transform : hudRoot.transform;
            scopeBtnObj = CreateButton(scopeParent, "ScopeADSButton", 0f, 0f, 105f, 105f, new Vector2(0.74f, 0.34f), "Mockup_ScopeButton.png", new Vector2(0.5f, 0.5f));
        }
        RectTransform scopeRt = scopeBtnObj.GetComponent<RectTransform>();
        if (deck != null)
        {
            scopeRt.SetParent(deck.transform, false);
            scopeRt.anchorMin = new Vector2(0.74f, 0.34f);
            scopeRt.anchorMax = new Vector2(0.74f, 0.34f);
            scopeRt.pivot = new Vector2(0.5f, 0.5f);
            scopeRt.anchoredPosition = Vector2.zero;
            scopeRt.sizeDelta = new Vector2(105f, 105f);
        }
        Image scopeImg = scopeBtnObj.GetComponent<Image>();
        if (scopeImg != null)
        {
            scopeImg.sprite = GetSprite("Mockup_ScopeButton.png");
            scopeImg.preserveAspect = true;
            scopeImg.color = Color.white;
        }

        var existingScopeLabel = scopeBtnObj.transform.Find("ScopeLabel");
        TextMeshProUGUI scopeTmp = null;
        if (existingScopeLabel == null)
        {
            scopeTmp = CreateTMPLabel(scopeBtnObj.transform, "ScopeLabel", "<b>SCOPE</b>", 0f, -22f, 90f, 24f, 11, TextAlignmentOptions.Center, new Color(0.4f, 0.92f, 1f));
        }
        else
        {
            scopeTmp = existingScopeLabel.GetComponent<TextMeshProUGUI>();
        }

        // 13. Grimoire Tome Button (Next to Scope Button on BottomControlDeck)
        var tomeBtnObj = GameObject.Find("GrimoireHUDButton");
        if (tomeBtnObj == null)
        {
            Transform tomeParent = deck != null ? deck.transform : hudRoot.transform;
            tomeBtnObj = CreateButton(tomeParent, "GrimoireHUDButton", 0f, 0f, 105f, 105f, new Vector2(0.885f, 0.34f), "Mockup_GrimoireButton.png", new Vector2(0.5f, 0.5f));
        }
        RectTransform tomeRt = tomeBtnObj.GetComponent<RectTransform>();
        if (deck != null)
        {
            tomeRt.SetParent(deck.transform, false);
            tomeRt.anchorMin = new Vector2(0.885f, 0.34f);
            tomeRt.anchorMax = new Vector2(0.885f, 0.34f);
            tomeRt.pivot = new Vector2(0.5f, 0.5f);
            tomeRt.anchoredPosition = Vector2.zero;
            tomeRt.sizeDelta = new Vector2(105f, 105f);
        }
        Image tomeImg = tomeBtnObj.GetComponent<Image>();
        if (tomeImg != null)
        {
            tomeImg.sprite = GetSprite("Mockup_GrimoireButton.png");
            tomeImg.preserveAspect = true;
            tomeImg.color = Color.white;
        }

        var existingTomeLabel = tomeBtnObj.transform.Find("TomeLabel");
        if (existingTomeLabel == null)
        {
            CreateTMPLabel(tomeBtnObj.transform, "TomeLabel", "<b>TOME</b>", 0f, -22f, 90f, 24f, 11, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.45f));
        }

        // 14. ADS Crosshair Canvas & Dynamic Rangefinder
        var crosshairRoot = GameObject.Find("ADSCrosshairCanvas");
        if (crosshairRoot == null)
        {
            Transform safeArea = canvas.transform.Find("SafeAreaContainer") ?? canvas.transform;
            crosshairRoot = new GameObject("ADSCrosshairCanvas", typeof(RectTransform));
            crosshairRoot.transform.SetParent(safeArea, false);
        }
        RectTransform crosshairRt = crosshairRoot.GetComponent<RectTransform>();
        crosshairRt.anchorMin = new Vector2(0.5f, 0.55f);
        crosshairRt.anchorMax = new Vector2(0.5f, 0.55f);
        crosshairRt.pivot = new Vector2(0.5f, 0.5f);
        crosshairRt.anchoredPosition = Vector2.zero;
        crosshairRt.sizeDelta = new Vector2(256f, 256f);

        var reticleObj = crosshairRoot.transform.Find("CrosshairReticleImage")?.gameObject;
        if (reticleObj == null)
        {
            reticleObj = new GameObject("CrosshairReticleImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            reticleObj.transform.SetParent(crosshairRoot.transform, false);
        }
        RectTransform reticleRt = reticleObj.GetComponent<RectTransform>();
        reticleRt.anchorMin = Vector2.zero;
        reticleRt.anchorMax = Vector2.one;
        reticleRt.offsetMin = Vector2.zero;
        reticleRt.offsetMax = Vector2.zero;
        Image reticleImg = reticleObj.GetComponent<Image>();
        reticleImg.sprite = GetSprite("Mockup_CrosshairReticle.png");
        reticleImg.preserveAspect = true;
        reticleImg.color = new Color(0.25f, 0.92f, 1.0f, 0.9f);
        reticleImg.raycastTarget = false;

        var rangefinderObj = crosshairRoot.transform.Find("RangefinderTMP")?.gameObject;
        TextMeshProUGUI rangefinderTMP = null;
        if (rangefinderObj == null)
        {
            rangefinderTMP = CreateTMPLabel(crosshairRoot.transform, "RangefinderTMP", "<color=#40EBFF>[ RANGE: 25.0m ]</color>", 0f, -55f, 320f, 32f, 14, TextAlignmentOptions.Center, Color.white);
        }
        else
        {
            rangefinderTMP = rangefinderObj.GetComponent<TextMeshProUGUI>();
        }

        // Attach & Configure WeaponScopeADSController
        WeaponScopeADSController adsController = hudRoot.GetComponent<WeaponScopeADSController>();
        if (adsController == null) adsController = hudRoot.AddComponent<WeaponScopeADSController>();
        adsController.BindReferences(
            scopeBtnObj.GetComponent<Button>(),
            scopeImg,
            scopeTmp,
            crosshairRoot,
            reticleImg,
            rangefinderTMP
        );

        // 15. Grimoire Assistant Overlay & SpellbookManager
        SpellbookManager spellbookMgr = hudRoot.GetComponent<SpellbookManager>();
        if (spellbookMgr == null) spellbookMgr = hudRoot.AddComponent<SpellbookManager>();

        var grimoireOverlayObj = GameObject.Find("GrimoireAssistantOverlay");
        if (grimoireOverlayObj == null)
        {
            Transform safeArea = canvas.transform.Find("SafeAreaContainer") ?? canvas.transform;
            grimoireOverlayObj = new GameObject("GrimoireAssistantOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            grimoireOverlayObj.transform.SetParent(safeArea, false);
        }
        RectTransform grimRt = grimoireOverlayObj.GetComponent<RectTransform>();
        grimRt.anchorMin = new Vector2(0.04f, 0.12f);
        grimRt.anchorMax = new Vector2(0.96f, 0.88f);
        grimRt.offsetMin = Vector2.zero;
        grimRt.offsetMax = Vector2.zero;
        Image grimBg = grimoireOverlayObj.GetComponent<Image>();
        grimBg.sprite = GetSprite("Mockup_GrimoireParchment.png");
        grimBg.type = Image.Type.Simple;
        grimBg.color = Color.white;

        // Tabs Container across top
        var tabsBar = grimoireOverlayObj.transform.Find("GrimoireTabsBar")?.gameObject;
        if (tabsBar == null)
        {
            tabsBar = new GameObject("GrimoireTabsBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsBar.transform.SetParent(grimoireOverlayObj.transform, false);
        }
        RectTransform tbRt = tabsBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0.04f, 0.90f);
        tbRt.anchorMax = new Vector2(0.90f, 0.98f);
        tbRt.offsetMin = Vector2.zero;
        tbRt.offsetMax = Vector2.zero;
        var hlg = tabsBar.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Close Button on top-right
        var closeGrimBtnObj = grimoireOverlayObj.transform.Find("GrimoireCloseBtn")?.gameObject;
        if (closeGrimBtnObj == null)
        {
            closeGrimBtnObj = CreateButton(grimoireOverlayObj.transform, "GrimoireCloseBtn", 0f, 0f, 50f, 50f, new Vector2(0.95f, 0.94f), null, new Vector2(0.5f, 0.5f));
            closeGrimBtnObj.GetComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f, 0.95f);
            CreateTMPLabel(closeGrimBtnObj.transform, "XLabel", "<b>X</b>", 0f, 0f, 50f, 50f, 22, TextAlignmentOptions.Center, Color.white);
        }

        // Left Page: Book lore & Spells List
        var leftPageObj = grimoireOverlayObj.transform.Find("LeftPageContainer")?.gameObject;
        if (leftPageObj == null)
        {
            leftPageObj = new GameObject("LeftPageContainer", typeof(RectTransform));
            leftPageObj.transform.SetParent(grimoireOverlayObj.transform, false);
        }
        RectTransform lpRt = leftPageObj.GetComponent<RectTransform>();
        lpRt.anchorMin = new Vector2(0.05f, 0.05f);
        lpRt.anchorMax = new Vector2(0.48f, 0.88f);
        lpRt.offsetMin = Vector2.zero;
        lpRt.offsetMax = Vector2.zero;

        var bTitleTMP = CreateOrGetTMP(leftPageObj.transform, "BookTitleTMP", "IGNIS COMPENDIUM", 0f, 250f, 380f, 40f, 20, TextAlignmentOptions.TopLeft, new Color(0.25f, 0.12f, 0.08f));
        var bAuthorTMP = CreateOrGetTMP(leftPageObj.transform, "BookAuthorTMP", "By Archmage Ignis", 0f, 215f, 380f, 28f, 13, TextAlignmentOptions.TopLeft, new Color(0.55f, 0.35f, 0.15f));
        var bDescTMP = CreateOrGetTMP(leftPageObj.transform, "BookDescTMP", "Ancient codex containing prime fire runes and incantations.", 0f, 160f, 380f, 75f, 12, TextAlignmentOptions.TopLeft, new Color(0.35f, 0.25f, 0.20f));

        var spellListObj = leftPageObj.transform.Find("SpellListContainer")?.gameObject;
        if (spellListObj == null)
        {
            spellListObj = new GameObject("SpellListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            spellListObj.transform.SetParent(leftPageObj.transform, false);
        }
        RectTransform slRt = spellListObj.GetComponent<RectTransform>();
        slRt.anchorMin = new Vector2(0f, 0f);
        slRt.anchorMax = new Vector2(1f, 0.58f);
        slRt.offsetMin = Vector2.zero;
        slRt.offsetMax = Vector2.zero;
        var vlg = spellListObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        // Right Page: Spell Assistant Details & Prime Button
        var rightPageObj = grimoireOverlayObj.transform.Find("RightPageContainer")?.gameObject;
        if (rightPageObj == null)
        {
            rightPageObj = new GameObject("RightPageContainer", typeof(RectTransform));
            rightPageObj.transform.SetParent(grimoireOverlayObj.transform, false);
        }
        RectTransform rpRt = rightPageObj.GetComponent<RectTransform>();
        rpRt.anchorMin = new Vector2(0.52f, 0.05f);
        rpRt.anchorMax = new Vector2(0.95f, 0.88f);
        rpRt.offsetMin = Vector2.zero;
        rpRt.offsetMax = Vector2.zero;

        var sNameTMP = CreateOrGetTMP(rightPageObj.transform, "SelectedSpellNameTMP", "FIREBALL", 0f, 250f, 380f, 38f, 22, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.25f, 0.10f));
        var sElemTMP = CreateOrGetTMP(rightPageObj.transform, "SelectedElementTMP", "ELEMENT: FIRE", 0f, 215f, 190f, 28f, 13, TextAlignmentOptions.TopLeft, new Color(0.70f, 0.20f, 0.10f));
        var sCostTMP = CreateOrGetTMP(rightPageObj.transform, "SelectedCostTMP", "COST: 25 MP", 180f, 215f, 190f, 28f, 13, TextAlignmentOptions.TopRight, new Color(0.10f, 0.45f, 0.75f));
        var sLoreTMP = CreateOrGetTMP(rightPageObj.transform, "SelectedLoreTMP", "Conjures a concentrated sphere of blazing fire that explodes upon striking the target.", 0f, 160f, 380f, 75f, 12, TextAlignmentOptions.TopLeft, new Color(0.35f, 0.25f, 0.20f));

        var gHeaderTMP = CreateOrGetTMP(rightPageObj.transform, "GestureHeaderTMP", "<b>RUNIC GESTURE:</b> Triangle", 0f, 90f, 380f, 28f, 14, TextAlignmentOptions.TopLeft, new Color(0.20f, 0.15f, 0.10f));
        var gInstrTMP = CreateOrGetTMP(rightPageObj.transform, "GestureInstrTMP", "Draw a Triangle starting from top apex, down right, across left, and back to apex. Then tap spinning CAST.", 0f, 45f, 380f, 60f, 12, TextAlignmentOptions.TopLeft, new Color(0.40f, 0.30f, 0.25f));

        var runeCanvasObj = rightPageObj.transform.Find("RuneStrokeCanvas")?.gameObject;
        if (runeCanvasObj == null)
        {
            runeCanvasObj = new GameObject("RuneStrokeCanvas", typeof(RectTransform), typeof(Image));
            runeCanvasObj.transform.SetParent(rightPageObj.transform, false);
            runeCanvasObj.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.20f, 0.85f);
        }
        RectTransform rCanvasRt = runeCanvasObj.GetComponent<RectTransform>();
        rCanvasRt.anchorMin = new Vector2(0.5f, 0.32f);
        rCanvasRt.anchorMax = new Vector2(0.5f, 0.32f);
        rCanvasRt.pivot = new Vector2(0.5f, 0.5f);
        rCanvasRt.sizeDelta = new Vector2(340f, 160f);

        // Prime / Cast Assist Button
        var primeBtnObj = rightPageObj.transform.Find("PrimeSpellAssistButton")?.gameObject;
        if (primeBtnObj == null)
        {
            primeBtnObj = CreateButton(rightPageObj.transform, "PrimeSpellAssistButton", 0f, -230f, 340f, 56f, new Vector2(0.5f, 0.5f), null, new Vector2(0.5f, 0.5f));
            primeBtnObj.GetComponent<Image>().color = new Color(0.85f, 0.65f, 0.15f, 0.95f);
        }
        var primeTMP = CreateOrGetTMP(primeBtnObj.transform, "PrimeLabelTMP", "<b>[ PRIME SPELL TO CAST ]</b>", 0f, 0f, 340f, 50f, 15, TextAlignmentOptions.Center, Color.white);

        // Bind GrimoireAssistantUI
        GrimoireAssistantUI grimoireUI = hudRoot.GetComponent<GrimoireAssistantUI>();
        if (grimoireUI == null) grimoireUI = hudRoot.AddComponent<GrimoireAssistantUI>();
        grimoireUI.BindReferences(
            grimoireOverlayObj,
            grimRt,
            tabsBar.transform,
            bTitleTMP,
            bAuthorTMP,
            bDescTMP,
            spellListObj.transform,
            sNameTMP,
            sElemTMP,
            sCostTMP,
            sLoreTMP,
            gHeaderTMP,
            gInstrTMP,
            rCanvasRt,
            primeBtnObj.GetComponent<Button>(),
            primeTMP,
            closeGrimBtnObj.GetComponent<Button>()
        );

        // Wire Tome HUD button to toggle Grimoire
        Button tomeBtn = tomeBtnObj.GetComponent<Button>();
        tomeBtn.onClick.RemoveAllListeners();
        tomeBtn.onClick.AddListener(spellbookMgr.ToggleGrimoire);

        // 10. Wire references to PortraitHUDController
        var hudController = UnityEngine.Object.FindAnyObjectByType<PortraitHUDController>();
        if (hudController != null)
        {
            hudController.BindMockupReferences(
                settingsBtnObj.GetComponent<Button>(),
                aiAutoBtnObj.GetComponent<Button>(),
                null,
                bagObj.GetComponent<Button>(),
                hpBarObj.GetComponentInChildren<Slider>(),
                mpBarObj.GetComponentInChildren<Slider>(),
                hpBarObj.transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>() ?? hpBarObj.GetComponentInChildren<TextMeshProUGUI>(),
                mpBarObj.transform.Find("ValueText")?.GetComponent<TextMeshProUGUI>() ?? mpBarObj.GetComponentInChildren<TextMeshProUGUI>(),
                lvlBadgeObj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>() ?? lvlBadgeObj.GetComponentInChildren<TextMeshProUGUI>(),
                bannerObj.transform.Find("BroadcastText")?.GetComponent<TextMeshProUGUI>() ?? bannerObj.GetComponentInChildren<TextMeshProUGUI>(),
                new Button[] { itemSlotObj.GetComponent<Button>() }
            );
        }

        // Mark scene dirty and save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("💾 [MOCKUP LAYOUT] Saved MainDungeonFloor.unity with updated mockup UI layout.");
    }

    private static GameObject CreateButton(Transform parent, string name, float x, float y, float w, float h, Vector2 anchor, string spriteName, Vector2? pivot = null)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot ?? anchor;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        Image img = obj.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = true;

        if (!string.IsNullOrEmpty(spriteName))
        {
            Sprite sp = GetSprite(spriteName);
            if (sp != null) img.sprite = sp;
        }

        return obj;
    }

    private static GameObject CreateImage(Transform parent, string name, float x, float y, float w, float h, Vector2 anchor, string spriteName = null, Color? color = null, Vector2? pivot = null)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot ?? anchor;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        Image img = obj.GetComponent<Image>();
        img.color = color ?? Color.white;
        img.raycastTarget = false;

        if (!string.IsNullOrEmpty(spriteName))
        {
            Sprite sp = GetSprite(spriteName);
            if (sp != null) img.sprite = sp;
        }

        return obj;
    }

    private static TextMeshProUGUI CreateTMPLabel(Transform parent, string name, string text, float x, float y, float w, float h, int fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject CreateResourceBar(Transform parent, string name, float x, float y, float w, float h, Color fillColor, string badgeText, string valText)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        Slider slider = root.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.88f;

        // Background
        GameObject bgObj = CreateImage(root.transform, "Background", 0f, 0f, w, h, new Vector2(0, 0), null, new Color(0.10f, 0.08f, 0.14f, 0.95f), new Vector2(0, 0));
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero; bgRt.pivot = new Vector2(0.5f, 0.5f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one; faRt.sizeDelta = new Vector2(-4, -4);

        // Fill
        GameObject fillObj = CreateImage(fillArea.transform, "Fill", 0f, 0f, 0f, 0f, new Vector2(0, 0), null, fillColor, new Vector2(0, 0));
        RectTransform fRt = fillObj.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one; fRt.sizeDelta = Vector2.zero;

        slider.fillRect = fRt;

        // Badge box on left
        GameObject badgeObj = CreateImage(root.transform, "Badge", -28f, 0f, 26f, h, new Vector2(0, 0.5f), null, fillColor * 0.85f, new Vector2(0, 0.5f));
        CreateTMPLabel(badgeObj.transform, "BadgeTxt", badgeText, 0f, 0f, 26f, h, 12, TextAlignmentOptions.Center, Color.white);

        // Value TMP on top of bar
        CreateTMPLabel(root.transform, "ValueText", valText, 0f, 0f, w, h, 13, TextAlignmentOptions.Center, Color.white);

        return root;
    }

    private static GameObject CreateResourceBarAnchored(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color fillColor, string badgeText, string valText)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Slider slider = root.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.88f;

        // Background
        GameObject bgObj = CreateImage(root.transform, "Background", 0f, 0f, 0f, 0f, new Vector2(0, 0), null, new Color(0.10f, 0.08f, 0.14f, 0.95f), new Vector2(0, 0));
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0.18f, 0.10f); faRt.anchorMax = new Vector2(0.98f, 0.90f); faRt.offsetMin = Vector2.zero; faRt.offsetMax = Vector2.zero;

        // Fill
        GameObject fillObj = CreateImage(fillArea.transform, "Fill", 0f, 0f, 0f, 0f, new Vector2(0, 0), null, fillColor, new Vector2(0, 0));
        RectTransform fRt = fillObj.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one; fRt.offsetMin = Vector2.zero; fRt.offsetMax = Vector2.zero;

        slider.fillRect = fRt;

        // Badge box on left
        GameObject badgeObj = CreateImage(root.transform, "Badge", 0f, 0f, 0f, 0f, new Vector2(0, 0), null, fillColor * 0.85f, new Vector2(0, 0));
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 0f); badgeRt.anchorMax = new Vector2(0.17f, 1f); badgeRt.offsetMin = Vector2.zero; badgeRt.offsetMax = Vector2.zero;
        CreateTMPLabel(badgeObj.transform, "BadgeTxt", badgeText, 0f, 0f, 26f, 18f, 11, TextAlignmentOptions.Center, Color.white);

        // Value TMP on top of bar
        CreateTMPLabel(root.transform, "ValueText", valText, 10f, 0f, 120f, 20f, 11, TextAlignmentOptions.Center, Color.white);

        return root;
    }
    #endregion
}

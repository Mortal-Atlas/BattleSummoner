using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorTools
{
    public static class BattleSideProfileExtractor
    {
        private const string SheetPath = "Assets/Textures/UI/BattleSideProfile_ReferenceSheet.png";
        private const string OutputDir = "Assets/Textures/UI/BattleSideProfile";

        [MenuItem("BattleSummoner/UI/Extract Battle Side Profile Sprites")]
        public static void ExtractAllSprites()
        {
            TextureImporter sheetImporter = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (sheetImporter == null)
            {
                Debug.LogError($"[EXTRACTOR] Reference sheet not found at: {SheetPath}");
                return;
            }

            sheetImporter.isReadable = true;
            sheetImporter.alphaIsTransparency = true;
            sheetImporter.SaveAndReimport();

            Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            if (sheet == null)
            {
                Debug.LogError("[EXTRACTOR] Failed to load reference sheet texture.");
                return;
            }

            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            int w = sheet.width;
            int h = sheet.height;
            Color mauveBg = sheet.GetPixel(512, 120);

            // 1. Extract Full Masc Template (Badge + Head + HP/MP Vitals)
            ExtractRegionWithDarkBgKeying(sheet, "Template_BattlePortrait_Masc.png", 35, 230, 470, 318, 0.22f);

            // 2. Extract Full Feme Template (Badge + Head + HP/MP Vitals)
            ExtractRegionWithDarkBgKeying(sheet, "Template_BattlePortrait_Feme.png", 520, 230, 470, 318, 0.22f);

            // 3. Extract Dual Gauge Vitals Only (HP/MP skewed bars)
            ExtractRegionWithDarkBgKeying(sheet, "BattleGauge_Dual_HP_MP.png", 92, 245, 410, 110, 0.22f);
            ExtractRegionWithDarkBgKeying(sheet, "Gauge_HP_Cyan.png", 96, 282, 240, 68, 0.22f);
            ExtractRegionWithDarkBgKeying(sheet, "Gauge_MP_Lime.png", 120, 252, 246, 68, 0.22f);

            // 4. Extract Masc & Feme Badge Frames
            ExtractRegion(sheet, "Frame_Badge_Masc.png", 44, 242, 296, 296, null);
            ExtractRegion(sheet, "Frame_Badge_Feme.png", 529, 242, 296, 296, null);

            // Extract isolated base heads (keyed against the purple frame background #6B21A8)
            ExtractIsolatedHead(sheet, "Base_Head_Masc.png", 44, 242, 296, 296);
            ExtractIsolatedHead(sheet, "Base_Head_Feme.png", 529, 242, 296, 296);

            // 5. Extract Customizable Assets from Bottom Strip with Mauve background removal
            // HAIR
            ExtractStripItem(sheet, "Hair_Spiky_Silver.png", 26, 48, 96, 134, mauveBg);
            ExtractStripItem(sheet, "Hair_Shaggy_Brown.png", 124, 48, 113, 132, mauveBg);
            ExtractStripItem(sheet, "Hair_Long_Black.png", 242, 48, 122, 132, mauveBg);

            // FACE / NOSE
            ExtractStripItem(sheet, "Face_Masc_Neutral.png", 388, 114, 38, 62, mauveBg);
            ExtractStripItem(sheet, "Face_Battle_Grit.png", 387, 54, 56, 64, mauveBg);
            ExtractStripItem(sheet, "Face_Feme_Parted.png", 458, 114, 62, 62, mauveBg);
            ExtractStripItem(sheet, "Face_Smirk.png", 465, 62, 48, 56, mauveBg);

            // EYE COLOR
            ExtractStripItem(sheet, "Eye_Side_Black.png", 558, 139, 44, 32, mauveBg);
            ExtractStripItem(sheet, "Eye_Side_Crimson.png", 558, 100, 44, 32, mauveBg);
            ExtractStripItem(sheet, "Eye_Side_Azure.png", 558, 61, 44, 33, mauveBg);

            // CLOTHING
            ExtractStripItem(sheet, "Clothing_Choker.png", 640, 134, 72, 38, mauveBg);
            ExtractStripItem(sheet, "Clothing_SailorFuku.png", 626, 48, 104, 80, mauveBg);
            ExtractStripItem(sheet, "Clothing_SuitBlazer.png", 736, 50, 164, 128, mauveBg);

            // ACCESSORIES
            ExtractStripItem(sheet, "Accessory_Headband.png", 920, 132, 60, 46, mauveBg);
            ExtractStripItem(sheet, "Accessory_Glasses.png", 904, 88, 86, 50, mauveBg);
            ExtractStripItem(sheet, "Accessory_Earrings.png", 924, 56, 48, 38, mauveBg);
            // 6. Generate Clean Gauge versions for dynamic live vitals
            CreateCleanGaugeVersion("Template_BattlePortrait_Masc.png", "Template_BattlePortrait_Masc_CleanGauge.png");
            CreateCleanGaugeVersion("Template_BattlePortrait_Feme.png", "Template_BattlePortrait_Feme_CleanGauge.png");

            AssetDatabase.Refresh();
            ConfigureAllExtractedSprites();
            Debug.Log("✨ [EXTRACTOR] Successfully extracted and configured all 22 Battle Side Profile sprites!");
        }

        private static void CreateCleanGaugeVersion(string sourceFileName, string targetFileName)
        {
            string sourcePath = Path.Combine(OutputDir, sourceFileName);
            if (!File.Exists(sourcePath)) return;
            byte[] fileData = File.ReadAllBytes(sourcePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);

            Color[] pix = tex.GetPixels();
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    int idx = y * tex.width + x;
                    // HP text row
                    if (y >= 50 && y <= 118 && x >= 304)
                    {
                        if (pix[idx].a > 0.01f) pix[idx] = Color.clear;
                    }
                    // MP text row
                    if (y < 50 && y >= 15 && x >= 335)
                    {
                        if (pix[idx].a > 0.01f) pix[idx] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutputDir, targetFileName), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ExtractStripItem(Texture2D source, string fileName, int x, int y, int width, int height, Color mauveBg)
        {
            Texture2D itemTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels(x, y, width, height);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float dist = ColorDistance(c, mauveBg);

                if (dist < 0.12f)
                {
                    // Fully transparent
                    pixels[i] = Color.clear;
                }
                else if (dist < 0.22f)
                {
                    // Smooth antialiased blend
                    float alpha = Mathf.InverseLerp(0.12f, 0.22f, dist);
                    pixels[i] = new Color(c.r, c.g, c.b, alpha);
                }
            }

            itemTex.SetPixels(pixels);
            itemTex.Apply();

            byte[] bytes = itemTex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(OutputDir, fileName), bytes);
            UnityEngine.Object.DestroyImmediate(itemTex);
        }

        private static void ExtractRegionWithDarkBgKeying(Texture2D source, string fileName, int x, int y, int width, int height, float threshold)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels(x, y, width, height);

            // Dark plum / burgundy background keying
            // Background is around #230E1A to #301120 (R: 0.12-0.20, G: 0.05-0.08, B: 0.10-0.15)
            // But gold border is #FACC15 (R: 0.98, G: 0.80, B: 0.08), purple inside is #6B21A8
            Color darkBgRef = new Color(0.15f, 0.06f, 0.12f, 1f);

            // Also check for flood-fill or edge boundary to only key outer background
            bool[,] isKept = new bool[width, height];
            bool[,] visited = new bool[width, height];

            // Run breadth-first search from outer perimeter to identify background pixels connected to outside
            System.Collections.Generic.Queue<Vector2Int> queue = new System.Collections.Generic.Queue<Vector2Int>();

            for (int ix = 0; ix < width; ix++)
            {
                queue.Enqueue(new Vector2Int(ix, 0));
                queue.Enqueue(new Vector2Int(ix, height - 1));
            }
            for (int iy = 0; iy < height; iy++)
            {
                queue.Enqueue(new Vector2Int(0, iy));
                queue.Enqueue(new Vector2Int(width - 1, iy));
            }

            while (queue.Count > 0)
            {
                Vector2Int p = queue.Dequeue();
                if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) continue;
                if (visited[p.x, p.y]) continue;
                visited[p.x, p.y] = true;

                int idx = p.y * width + p.x;
                Color c = pixels[idx];

                int sheetX = x + p.x;
                int sheetY = y + p.y;
                bool isTitleBanner = (sheetX >= 340 && sheetX <= 665 && sheetY >= 522);

                // Check if this pixel is the outer background
                bool isGoldOrYellow = (c.r > 0.6f && c.g > 0.45f && c.b < 0.35f);
                bool isCyanHP = (c.b > 0.7f && c.g > 0.5f);
                bool isLimeMP = (c.g > 0.7f && c.r > 0.2f && c.b < 0.6f);
                bool isWhiteText = (c.r > 0.85f && c.g > 0.85f && c.b > 0.85f);
                bool isVibrantPurple = (c.r > 0.35f && c.b > 0.55f);

                bool isForeground = !isTitleBanner && (isGoldOrYellow || isCyanHP || isLimeMP || isWhiteText || isVibrantPurple);

                if (!isForeground)
                {
                    // Exterior background pixel -> make transparent
                    pixels[idx] = Color.clear;

                    // Expand to 4 neighbors
                    if (p.x > 0 && !visited[p.x - 1, p.y]) queue.Enqueue(new Vector2Int(p.x - 1, p.y));
                    if (p.x < width - 1 && !visited[p.x + 1, p.y]) queue.Enqueue(new Vector2Int(p.x + 1, p.y));
                    if (p.y > 0 && !visited[p.x, p.y - 1]) queue.Enqueue(new Vector2Int(p.x, p.y - 1));
                    if (p.y < height - 1 && !visited[p.x, p.y + 1]) queue.Enqueue(new Vector2Int(p.x, p.y + 1));
                }
            }

            // Cleanup any stray disconnected banner pixels in the title region
            for (int iy = 0; iy < height; iy++)
            {
                for (int ix = 0; ix < width; ix++)
                {
                    int sx = x + ix;
                    int sy = y + iy;
                    if (sx >= 340 && sx <= 665 && sy >= 522)
                    {
                        pixels[iy * width + ix] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(OutputDir, fileName), bytes);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ExtractIsolatedHead(Texture2D source, string fileName, int x, int y, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels(x, y, width, height);

            Color purpleBg = new Color(0.42f, 0.13f, 0.66f, 1f); // #6B21A8

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];

                // Check if purple frame background
                bool isPurple = (c.b > 0.45f && c.r > 0.25f && c.g < 0.25f);
                // Check if outer dark plum background
                bool isOuterDark = (c.r < 0.25f && c.g < 0.15f && c.b < 0.22f && c.r > 0.08f);
                // Check if gold border
                bool isGold = (c.r > 0.65f && c.g > 0.50f && c.b < 0.35f);

                if (isPurple || isOuterDark || isGold)
                {
                    pixels[i] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(OutputDir, fileName), bytes);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ExtractRegion(Texture2D source, string fileName, int x, int y, int width, int height, Color? removeColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels(x, y, width, height);

            if (removeColor.HasValue)
            {
                Color bg = removeColor.Value;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (ColorDistance(pixels[i], bg) < 0.15f)
                    {
                        pixels[i] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(OutputDir, fileName), bytes);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static float ColorDistance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        private static void ConfigureAllExtractedSprites()
        {
            string[] files = Directory.GetFiles(OutputDir, "*.png");
            foreach (string file in files)
            {
                string unityPath = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.isReadable = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}

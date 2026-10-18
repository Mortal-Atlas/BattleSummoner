#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BattleSummoner.Character.FacialAnimation;

namespace BattleSummoner.Character.FacialAnimation.Editor
{
    public static class AnimeEyeReferenceExtractor
    {
        private const string SOURCE_IMAGE_CANDIDATE = @"C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.user_uploaded\media_1792299754301.jpg";
        private const string EYES_OUTPUT_DIR = "Assets/Sprites/CharacterFaces/Eyes";
        private const string DB_PATH = "Assets/Data/FacialAnimation/DefaultFacialExpressionDB.asset";

        private const int OUT_W = 256;
        private const int OUT_H = 128;
        private const int BASELINE_Y = 32;

        public struct EyeMetadata
        {
            public string name;
            public int row; // 0=Top (Normal..Angry), 1=Mid (Sad..Excited), 2=Bottom (Open..Closed)
            public int col; // 0..3
            public FacialEmotion emotion;
            public bool isBlinkFrame;
        }

        public static readonly EyeMetadata[] AllEyeDefs = new EyeMetadata[]
        {
            // Row 0: Normal / 通常, Happy / 笑顔, Surprised / 驚き, Angry / 怒り
            new EyeMetadata { name = "Eye_Normal", row = 0, col = 0, emotion = FacialEmotion.Neutral, isBlinkFrame = false },
            new EyeMetadata { name = "Eye_Happy", row = 0, col = 1, emotion = FacialEmotion.Happy, isBlinkFrame = false },
            new EyeMetadata { name = "Eye_Surprised", row = 0, col = 2, emotion = FacialEmotion.Surprised, isBlinkFrame = false },
            new EyeMetadata { name = "Eye_Angry", row = 0, col = 3, emotion = FacialEmotion.Angry, isBlinkFrame = false },

            // Row 1: Sad / 悲しい, Wink / ウインク, Annoyed / 嫌悪, Excited / 興奮
            new EyeMetadata { name = "Eye_Sad", row = 1, col = 0, emotion = FacialEmotion.Sad, isBlinkFrame = false },
            new EyeMetadata { name = "Eye_Wink", row = 1, col = 1, emotion = FacialEmotion.Smug, isBlinkFrame = false },
            new EyeMetadata { name = "Eye_Annoyed", row = 1, col = 2, emotion = FacialEmotion.CrazyOverload, isBlinkFrame = false },
            new EyeMetadata { name = "Eye_Excited", row = 1, col = 3, emotion = FacialEmotion.HeartEyesBlush, isBlinkFrame = false },

            // Row 2: Open / 開眼, Half Closed / 半開き, Nearly Closed / ほぼ閉, Closed / 閉眼
            new EyeMetadata { name = "Eye_Open", row = 2, col = 0, emotion = FacialEmotion.Neutral, isBlinkFrame = false },
            new EyeMetadata { name = "Eye_HalfClosed", row = 2, col = 1, emotion = FacialEmotion.Neutral, isBlinkFrame = true },
            new EyeMetadata { name = "Eye_NearlyClosed", row = 2, col = 2, emotion = FacialEmotion.Neutral, isBlinkFrame = true },
            new EyeMetadata { name = "Eye_Closed", row = 2, col = 3, emotion = FacialEmotion.Neutral, isBlinkFrame = true }
        };

        [MenuItem("BattleSummoner/Character/4. Extract Anime Eyes from Reference & Setup Rig", false, 4)]
        public static void ExtractAllEyesAndSetupRig()
        {
            string sourcePath = FindReferenceImagePath();
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                Debug.LogError($"[Eye Extractor] Reference image not found at '{sourcePath}'. Please supply the uploaded image path.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
            if (!AssetDatabase.IsValidFolder("Assets/Sprites/CharacterFaces")) AssetDatabase.CreateFolder("Assets/Sprites", "CharacterFaces");
            if (!AssetDatabase.IsValidFolder(EYES_OUTPUT_DIR)) AssetDatabase.CreateFolder("Assets/Sprites/CharacterFaces", "Eyes");

            byte[] fileBytes = File.ReadAllBytes(sourcePath);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(fileBytes);

            int srcW = srcTex.width;
            int srcH = srcTex.height;
            Color32[] srcPixels = srcTex.GetPixels32();

            // Calibrated centers and row lower lash baselines
            int[] colCenters = new int[] { 140, 389, 638, 888 };
            int[] rowBaselines = new int[] { 435, 267, 73 };

            // Chroma reference: royal blue (0, 101, 254)
            float refR = 0f, refG = 101f, refB = 254f;

            Debug.Log($"🎨 [Eye Extractor] Processing 12 Anime Eye Expressions from '{Path.GetFileName(sourcePath)}' ({srcW}x{srcH})...");

            var extractedSprites = new Dictionary<string, Sprite>();

            foreach (var def in AllEyeDefs)
            {
                int centerX = colCenters[def.col];
                int baseY = rowBaselines[def.row];

                Color32[] outPixels = new Color32[OUT_W * OUT_H];

                for (int y = 0; y < OUT_H; y++)
                {
                    int srcY = baseY + (y - BASELINE_Y);
                    if (srcY < 0 || srcY >= srcH) continue;

                    for (int x = 0; x < OUT_W; x++)
                    {
                        int srcX = centerX + (x - OUT_W / 2);
                        if (srcX < 0 || srcX >= srcW) continue;

                        Color32 p = srcPixels[srcY * srcW + srcX];

                        // Chroma key distance
                        float dr = p.r - refR;
                        float dg = p.g - refG;
                        float db = p.b - refB;
                        float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);

                        float alpha;
                        if (dist <= 28f)
                        {
                            alpha = 0f;
                        }
                        else if (dist >= 75f)
                        {
                            alpha = 1f;
                        }
                        else
                        {
                            alpha = Mathf.SmoothStep(0f, 1f, (dist - 28f) / 47f);
                        }

                        // Blue despill filter for clean antialiased edges
                        byte r = p.r;
                        byte g = p.g;
                        byte b = p.b;
                        if (alpha > 0f)
                        {
                            float maxOther = Mathf.Max(r, g);
                            if (b > maxOther + 12f)
                            {
                                b = (byte)Mathf.Clamp(maxOther + 8f, 0f, 255f);
                            }
                        }

                        byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                        outPixels[y * OUT_W + x] = new Color32(r, g, b, a);
                    }
                }

                Texture2D outTex = new Texture2D(OUT_W, OUT_H, TextureFormat.RGBA32, false);
                outTex.SetPixels32(outPixels);
                outTex.Apply();

                string outPath = $"{EYES_OUTPUT_DIR}/{def.name}.png";
                File.WriteAllBytes(outPath, outTex.EncodeToPNG());

                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 200f;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var def in AllEyeDefs)
            {
                string path = $"{EYES_OUTPUT_DIR}/{def.name}.png";
                extractedSprites[def.name] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            // Update FacialExpressionDatabaseSO
            UpdateDatabaseWithExtractedEyes(extractedSprites);

            // Rebuild SpriteAtlas
            EditorModularFaceRigBuilder.GenerateCharacterFaceSpriteAtlas();

            // Refresh Hero in Scene
            RefreshHeroFaceRig();

            Debug.Log("✅ [Eye Extractor] Successfully extracted 12 high-fidelity anime eye pairs, configured DB, and updated hero!");
        }

        public static void UpdateDatabaseWithExtractedEyes(Dictionary<string, Sprite> eyeSprites)
        {
            var db = AssetDatabase.LoadAssetAtPath<FacialExpressionDatabaseSO>(DB_PATH);
            if (db == null) return;

            Sprite GetEye(string name)
            {
                return eyeSprites.TryGetValue(name, out Sprite s) ? s : null;
            }

            var normalEye = GetEye("Eye_Normal") ?? GetEye("Eye_Open");
            var closedEye = GetEye("Eye_Closed");
            var halfClosedEye = GetEye("Eye_HalfClosed");
            var nearlyClosedEye = GetEye("Eye_NearlyClosed");

            // Mapping 9 emotions to authentic extracted eye expressions
            var emotionMapping = new Dictionary<FacialEmotion, (Sprite open, Sprite blink)>()
            {
                { FacialEmotion.Neutral, (normalEye, closedEye) },
                { FacialEmotion.Happy, (GetEye("Eye_Happy") ?? normalEye, closedEye) },
                { FacialEmotion.Angry, (GetEye("Eye_Angry") ?? normalEye, nearlyClosedEye) },
                { FacialEmotion.Sad, (GetEye("Eye_Sad") ?? normalEye, closedEye) },
                { FacialEmotion.Surprised, (GetEye("Eye_Surprised") ?? normalEye, halfClosedEye) },
                { FacialEmotion.Smug, (GetEye("Eye_Wink") ?? normalEye, closedEye) },
                { FacialEmotion.HeartEyesBlush, (GetEye("Eye_Excited") ?? normalEye, closedEye) },
                { FacialEmotion.CrazyOverload, (GetEye("Eye_Annoyed") ?? normalEye, nearlyClosedEye) },
                { FacialEmotion.PainHit, (closedEye, closedEye) }
            };

            for (int i = 0; i < db.emotionSets.Count; i++)
            {
                var set = db.emotionSets[i];
                if (emotionMapping.TryGetValue(set.emotion, out var sprites))
                {
                    set.eyesAndBrowsOpen = sprites.open;
                    set.eyesAndBrowsBlink = sprites.blink;
                    db.emotionSets[i] = set;
                }
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        public static Texture2D RecolorEyeIrisTexture(Texture2D sourceTex, Color targetColor)
        {
            int w = sourceTex.width;
            int h = sourceTex.height;
            Color32[] px = sourceTex.GetPixels32();
            Color32[] result = new Color32[px.Length];

            Color.RGBToHSV(targetColor, out float targetH, out float targetS, out float targetV);

            for (int i = 0; i < px.Length; i++)
            {
                Color32 p = px[i];
                if (p.a == 0)
                {
                    result[i] = p;
                    continue;
                }

                Color.RGBToHSV(p, out float origH, out float origS, out float origV);

                // Detect green anime iris pixels (135° to 195°, i.e. 0.375 to 0.54 in 0-1)
                if (origH >= 0.375f && origH <= 0.54f && origS > 0.20f && origV > 0.10f)
                {
                    float newH = targetH;
                    float newS = Mathf.Clamp01(origS * (targetS / 0.75f));
                    float newV = origV;

                    Color newColor = Color.HSVToRGB(newH, newS, newV);
                    result[i] = new Color32(
                        (byte)Mathf.RoundToInt(newColor.r * 255f),
                        (byte)Mathf.RoundToInt(newColor.g * 255f),
                        (byte)Mathf.RoundToInt(newColor.b * 255f),
                        p.a);
                }
                else
                {
                    result[i] = p;
                }
            }

            Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels32(result);
            outTex.Apply();
            return outTex;
        }

        [MenuItem("BattleSummoner/Character/5. Bake Preset Eye Color Palettes (Blue, Red, Gold, etc.)", false, 5)]
        public static void BakeEyeColorPalettes()
        {
            var presets = new Dictionary<string, Color>()
            {
                { "SapphireBlue", new Color(0.15f, 0.45f, 0.95f) },
                { "CrimsonRed", new Color(0.85f, 0.15f, 0.25f) },
                { "GoldenAmber", new Color(0.95f, 0.70f, 0.15f) },
                { "AmethystPurple", new Color(0.68f, 0.22f, 0.88f) },
                { "SkyCyan", new Color(0.12f, 0.82f, 0.92f) },
                { "RosePink", new Color(0.95f, 0.35f, 0.60f) },
                { "Obsidian", new Color(0.32f, 0.22f, 0.18f) },
                { "Silver", new Color(0.75f, 0.78f, 0.85f) }
            };

            string paletteBaseDir = $"{EYES_OUTPUT_DIR}/Palettes";
            if (!Directory.Exists(paletteBaseDir)) Directory.CreateDirectory(paletteBaseDir);

            string normalEyePath = $"{EYES_OUTPUT_DIR}/Eye_Normal.png";
            if (!File.Exists(normalEyePath)) return;

            byte[] b = File.ReadAllBytes(normalEyePath);
            Texture2D src = new Texture2D(2, 2);
            src.LoadImage(b);

            foreach (var kvp in presets)
            {
                Texture2D recolored = RecolorEyeIrisTexture(src, kvp.Value);
                string outPath = $"{paletteBaseDir}/Eye_Normal_{kvp.Key}.png";
                File.WriteAllBytes(outPath, recolored.EncodeToPNG());

                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"🎨 [Eye Extractor] Successfully baked {presets.Count} eye color palette sprites into '{paletteBaseDir}'!");
        }

        [MenuItem("BattleSummoner/Character/6. Cycle Hero Eye Color (Test Tinting)", false, 6)]
        public static void CycleHeroEyeColor()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero == null) return;
            var rig = hero.GetComponentInChildren<ModularFaceRigController>();
            if (rig == null) return;

            Color[] testPalette = new Color[]
            {
                new Color(0.15f, 0.45f, 0.95f), // Sapphire Blue
                new Color(0.85f, 0.15f, 0.25f), // Crimson Red
                new Color(0.95f, 0.70f, 0.15f), // Golden Amber
                new Color(0.68f, 0.22f, 0.88f), // Amethyst Purple
                new Color(0.13f, 0.65f, 0.35f)  // Original Emerald Green
            };

            int nextIdx = 0;
            for (int i = 0; i < testPalette.Length; i++)
            {
                if (Vector4.Distance(rig.eyeIrisColor, testPalette[i]) < 0.1f)
                {
                    nextIdx = (i + 1) % testPalette.Length;
                    break;
                }
            }

            rig.SetEyeColor(testPalette[nextIdx]);
            EditorUtility.SetDirty(rig);
            Debug.Log($"👁️ [Hero Eye Customization] Cycled eye color to {rig.eyeIrisColor}");
        }

        private static void RefreshHeroFaceRig()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                var rig = hero.GetComponentInChildren<ModularFaceRigController>();
                if (rig != null)
                {
                    rig.ApplySpatialAnchors();
                    rig.ApplyEyeColorCustomization();
                    rig.SetEmotion(FacialEmotion.Happy);
                    EditorUtility.SetDirty(hero);
                }
            }
        }

        private static string FindReferenceImagePath()
        {
            if (File.Exists(SOURCE_IMAGE_CANDIDATE)) return SOURCE_IMAGE_CANDIDATE;

            // Search in Assets/Sprites/
            string[] found = Directory.GetFiles("Assets", "media_1792299754301.jpg", SearchOption.AllDirectories);
            if (found.Length > 0) return found[0];

            return null;
        }
    }
}
#endif

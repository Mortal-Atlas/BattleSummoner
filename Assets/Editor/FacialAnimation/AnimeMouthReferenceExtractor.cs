#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BattleSummoner.Character.FacialAnimation;

namespace BattleSummoner.Character.FacialAnimation.Editor
{
    public static class AnimeMouthReferenceExtractor
    {
        private const string SOURCE_IMAGE_CANDIDATE = @"C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.user_uploaded\media_1792300278360.png";
        private const string MOUTHS_OUTPUT_DIR = "Assets/Sprites/CharacterFaces/Mouths";
        private const string FACES_ROOT_DIR = "Assets/Sprites/CharacterFaces";
        private const string DB_PATH = "Assets/Data/FacialAnimation/DefaultFacialExpressionDB.asset";

        private const int OUT_W = 256;
        private const int OUT_H = 128;

        public struct MouthDef
        {
            public string assetName;
            public int xMin, xMax, yMin, yMax;
            public string description;
        }

        // Bounding boxes calibrated to crop out all text labels and all blue eyes in the mood section
        public static readonly MouthDef[] AllMouths = new MouthDef[]
        {
            // Row 1: Speaking Phonemes & Vowels
            new MouthDef { assetName = "Vowel_A", xMin = 65, xMax = 180, yMin = 425, yMax = 490, description = "Vowel A (Wide Full Open)" },
            new MouthDef { assetName = "Vowel_E", xMin = 255, xMax = 385, yMin = 425, yMax = 485, description = "Vowel E (Mid-Wide Open)" },
            new MouthDef { assetName = "Vowel_I", xMin = 475, xMax = 550, yMin = 425, yMax = 480, description = "Vowel I (Small Narrow Teeth)" },
            new MouthDef { assetName = "Vowel_O", xMin = 665, xMax = 745, yMin = 425, yMax = 495, description = "Vowel O (Round Open)" },
            new MouthDef { assetName = "Vowel_U", xMin = 860, xMax = 950, yMin = 425, yMax = 485, description = "Vowel U (Pursed Small)" },

            // Row 2: Consonants
            new MouthDef { assetName = "Consonant_MPB", xMin = 70, xMax = 175, yMin = 325, yMax = 355, description = "M/P/B (Closed Rest Line)" },
            new MouthDef { assetName = "Consonant_FV_Lip", xMin = 265, xMax = 375, yMin = 325, yMax = 355, description = "F/V (Lip Bite Line)" },
            new MouthDef { assetName = "Consonant_FV_Teeth", xMin = 455, xMax = 565, yMin = 325, yMax = 365, description = "F/V (Side Bite Teeth)" },
            new MouthDef { assetName = "Consonant_L", xMin = 650, xMax = 760, yMin = 325, yMax = 368, description = "L (Tongue to Teeth)" },
            new MouthDef { assetName = "Consonant_SZT", xMin = 850, xMax = 955, yMin = 325, yMax = 365, description = "S/Z/T (Teeth Closed Line)" },

            // Row 3: Mood Expressions (yMax strictly 120, completely cropping out the blue eyes above!)
            new MouthDef { assetName = "Mood_Happy", xMin = 55, xMax = 170, yMin = 65, yMax = 120, description = "Happy (Joyful Open Smile)" },
            new MouthDef { assetName = "Mood_Sad", xMin = 195, xMax = 300, yMin = 65, yMax = 115, description = "Sad (Downturned Frown)" },
            new MouthDef { assetName = "Mood_Angry", xMin = 320, xMax = 435, yMin = 65, yMax = 120, description = "Angry (Bared Teeth Snarl)" },
            new MouthDef { assetName = "Mood_Surprised", xMin = 490, xMax = 540, yMin = 65, yMax = 115, description = "Surprised (Small 'o' Gasp)" },
            new MouthDef { assetName = "Mood_Smirk", xMin = 600, xMax = 705, yMin = 65, yMax = 120, description = "Smirk (Side Smile with Teeth)" },
            new MouthDef { assetName = "Mood_Pouting", xMin = 740, xMax = 828, yMin = 65, yMax = 115, description = "Pouting (Downturned Pout)" },
            new MouthDef { assetName = "Mood_CuteCat", xMin = 875, xMax = 972, yMin = 65, yMax = 115, description = "Cute Cat (:3 Omega Mouth)" }
        };

        [MenuItem("BattleSummoner/Character/7. Extract Anime Mouths & Visemes from Reference", false, 7)]
        public static void ExtractAllMouthsAndSetupRig()
        {
            string sourcePath = FindReferenceImagePath();
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                Debug.LogError($"[Mouth Extractor] Reference image not found at '{sourcePath}'.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
            if (!AssetDatabase.IsValidFolder(FACES_ROOT_DIR)) AssetDatabase.CreateFolder("Assets/Sprites", "CharacterFaces");
            if (!AssetDatabase.IsValidFolder(MOUTHS_OUTPUT_DIR)) AssetDatabase.CreateFolder(FACES_ROOT_DIR, "Mouths");

            byte[] fileBytes = File.ReadAllBytes(sourcePath);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(fileBytes);

            int srcW = srcTex.width;
            int srcH = srcTex.height;
            Color32[] srcPixels = srcTex.GetPixels32();

            // Background slate blue: (66, 128, 173)
            Color32 bgRef = new Color32(66, 128, 173, 255);

            Debug.Log($"🎨 [Mouth Extractor] Processing 17 Anime Mouths & Phonemes from '{Path.GetFileName(sourcePath)}' ({srcW}x{srcH})...");

            var mouthTextures = new Dictionary<string, Texture2D>();

            foreach (var def in AllMouths)
            {
                // 1. Locate tight bounds of non-background pixels within definition window
                int minX = def.xMax, maxX = def.xMin, minY = def.yMax, maxY = def.yMin;
                int nonBgCount = 0;

                for (int y = def.yMin; y <= def.yMax; y++)
                {
                    for (int x = def.xMin; x <= def.xMax; x++)
                    {
                        Color32 p = srcPixels[y * srcW + x];
                        int dr = p.r - bgRef.r;
                        int dg = p.g - bgRef.g;
                        int db = p.b - bgRef.b;
                        if ((dr * dr + dg * dg + db * db) > 350)
                        {
                            nonBgCount++;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (nonBgCount == 0 || minX > maxX || minY > maxY)
                {
                    minX = def.xMin; maxX = def.xMax; minY = def.yMin; maxY = def.yMax;
                }

                // Add 2px border padding for clean edge filtering
                minX = Mathf.Max(0, minX - 2);
                maxX = Mathf.Min(srcW - 1, maxX + 2);
                minY = Mathf.Max(0, minY - 2);
                maxY = Mathf.Min(srcH - 1, maxY + 2);

                int boxW = maxX - minX + 1;
                int boxH = maxY - minY + 1;

                Color32[] outPixels = new Color32[OUT_W * OUT_H];
                int pasteX = (OUT_W - boxW) / 2;
                int pasteY = (OUT_H - boxH) / 2;

                for (int y = 0; y < boxH; y++)
                {
                    int sy = minY + y;
                    for (int x = 0; x < boxW; x++)
                    {
                        int sx = minX + x;
                        Color32 p = srcPixels[sy * srcW + sx];

                        float dr = p.r - bgRef.r;
                        float dg = p.g - bgRef.g;
                        float db = p.b - bgRef.b;
                        float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);

                        float alpha = Mathf.Clamp01((dist - 8f) / 22f);
                        alpha = Mathf.SmoothStep(0f, 1f, alpha);

                        byte r = p.r, g = p.g, b = p.b;
                        // Despill slate blue fringe
                        if (alpha > 0f && b > r + 10 && b > g)
                        {
                            b = (byte)Mathf.Clamp(Mathf.Max(r, g), 0, 255);
                        }

                        int dx = pasteX + x;
                        int dy = pasteY + y;
                        outPixels[dy * OUT_W + dx] = new Color32(r, g, b, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                Texture2D outTex = new Texture2D(OUT_W, OUT_H, TextureFormat.RGBA32, false);
                outTex.SetPixels32(outPixels);
                outTex.Apply();

                mouthTextures[def.assetName] = outTex;

                // Save to Mouths directory
                string path = $"{MOUTHS_OUTPUT_DIR}/{def.assetName}.png";
                SaveTextureAsSprite(outTex, path);
            }

            // 2. Overwrite / wire standard Phonetic Visemes in CharacterFaces root
            SaveTextureAsSprite(mouthTextures["Consonant_MPB"], $"{FACES_ROOT_DIR}/Viseme_0_Closed_Rest.png");
            SaveTextureAsSprite(mouthTextures["Vowel_I"], $"{FACES_ROOT_DIR}/Viseme_1_Vowel_I_Small.png");
            SaveTextureAsSprite(mouthTextures["Vowel_E"], $"{FACES_ROOT_DIR}/Viseme_2_Vowel_E_Mid.png");
            SaveTextureAsSprite(mouthTextures["Vowel_U"], $"{FACES_ROOT_DIR}/Viseme_3_Vowel_U_Round.png");
            SaveTextureAsSprite(mouthTextures["Vowel_O"], $"{FACES_ROOT_DIR}/Viseme_4_Vowel_O_Round.png");
            SaveTextureAsSprite(mouthTextures["Vowel_A"], $"{FACES_ROOT_DIR}/Viseme_5_Vowel_A_Wide.png");

            // 3. Overwrite / wire Resting Emotion Mouths in CharacterFaces root
            SaveTextureAsSprite(mouthTextures["Consonant_MPB"], $"{FACES_ROOT_DIR}/Mouth_Rest_Neutral.png");
            SaveTextureAsSprite(mouthTextures["Mood_Happy"], $"{FACES_ROOT_DIR}/Mouth_Rest_Happy.png");
            SaveTextureAsSprite(mouthTextures["Mood_Angry"], $"{FACES_ROOT_DIR}/Mouth_Rest_Angry.png");
            SaveTextureAsSprite(mouthTextures["Mood_Sad"], $"{FACES_ROOT_DIR}/Mouth_Rest_Sad.png");
            SaveTextureAsSprite(mouthTextures["Mood_Surprised"], $"{FACES_ROOT_DIR}/Mouth_Rest_Surprised.png");
            SaveTextureAsSprite(mouthTextures["Mood_Smirk"], $"{FACES_ROOT_DIR}/Mouth_Rest_Smug.png");
            SaveTextureAsSprite(mouthTextures["Mood_CuteCat"], $"{FACES_ROOT_DIR}/Mouth_Rest_HeartEyesBlush.png");
            SaveTextureAsSprite(mouthTextures["Mood_Angry"], $"{FACES_ROOT_DIR}/Mouth_Rest_CrazyOverload.png");
            SaveTextureAsSprite(mouthTextures["Mood_Pouting"], $"{FACES_ROOT_DIR}/Mouth_Rest_PainHit.png");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 4. Update Database references
            UpdateDatabaseReferences();

            // 5. Rebuild SpriteAtlas
            EditorModularFaceRigBuilder.GenerateCharacterFaceSpriteAtlas();

            // 6. Refresh active hero in scene
            RefreshHeroFaceRig();

            Debug.Log("✅ [Mouth Extractor] Successfully extracted 17 authentic anime mouth expressions, updated 6 visemes, 9 resting mouths, and rigged hero!");
        }

        private static void SaveTextureAsSprite(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
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

        private static void UpdateDatabaseReferences()
        {
            var db = AssetDatabase.LoadAssetAtPath<FacialExpressionDatabaseSO>(DB_PATH);
            if (db == null) return;

            // Load visemes
            db.phoneticMouthVisemes = new Sprite[6]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>($"{FACES_ROOT_DIR}/Viseme_0_Closed_Rest.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{FACES_ROOT_DIR}/Viseme_1_Vowel_I_Small.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{FACES_ROOT_DIR}/Viseme_2_Vowel_E_Mid.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{FACES_ROOT_DIR}/Viseme_3_Vowel_U_Round.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{FACES_ROOT_DIR}/Viseme_4_Vowel_O_Round.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{FACES_ROOT_DIR}/Viseme_5_Vowel_A_Wide.png")
            };

            // Update resting emotion mouths in emotion sets
            var restingMouthMap = new Dictionary<FacialEmotion, string>()
            {
                { FacialEmotion.Neutral, "Mouth_Rest_Neutral" },
                { FacialEmotion.Happy, "Mouth_Rest_Happy" },
                { FacialEmotion.Angry, "Mouth_Rest_Angry" },
                { FacialEmotion.Sad, "Mouth_Rest_Sad" },
                { FacialEmotion.Surprised, "Mouth_Rest_Surprised" },
                { FacialEmotion.Smug, "Mouth_Rest_Smug" },
                { FacialEmotion.HeartEyesBlush, "Mouth_Rest_HeartEyesBlush" },
                { FacialEmotion.CrazyOverload, "Mouth_Rest_CrazyOverload" },
                { FacialEmotion.PainHit, "Mouth_Rest_PainHit" }
            };

            for (int i = 0; i < db.emotionSets.Count; i++)
            {
                var set = db.emotionSets[i];
                if (restingMouthMap.TryGetValue(set.emotion, out string spriteName))
                {
                    set.restingEmotionMouth = AssetDatabase.LoadAssetAtPath<Sprite>($"{FACES_ROOT_DIR}/{spriteName}.png");
                    db.emotionSets[i] = set;
                }
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
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
                    rig.SetEmotion(FacialEmotion.Happy);
                    EditorUtility.SetDirty(hero);
                }
            }
        }

        private static string FindReferenceImagePath()
        {
            if (File.Exists(SOURCE_IMAGE_CANDIDATE)) return SOURCE_IMAGE_CANDIDATE;

            string[] found = Directory.GetFiles("Assets", "media_1792300278360.png", SearchOption.AllDirectories);
            if (found.Length > 0) return found[0];

            return null;
        }
    }
}
#endif

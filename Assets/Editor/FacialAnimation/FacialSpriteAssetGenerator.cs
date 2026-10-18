#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;
using BattleSummoner.Character.FacialAnimation;
using BattleSummoner.Character.FacialAnimation.Editor;

namespace BattleSummoner.Character.FacialAnimation.Editor
{
    public static class FacialSpriteAssetGenerator
    {
        private const string SPRITES_DIR = "Assets/Sprites/CharacterFaces";
        private const string DATA_DIR = "Assets/Data/FacialAnimation";
        private const string DB_PATH = "Assets/Data/FacialAnimation/DefaultFacialExpressionDB.asset";
        private const int TEX_W = 256;
        private const int TEX_H = 128;

        [MenuItem("BattleSummoner/Character/3. Generate Default Facial Sprites, DB & Rig Hero", false, 3)]
        public static void GenerateAllFacialAssetsAndRigHero()
        {
            EnsureDirectories();

            Debug.Log("🎨 Generating High-Quality Anime Facial Sprites (Eyes/Brows & Mouths)...");

            // 1. Generate Eyes & Eyebrows Sprites
            var emotions = (FacialEmotion[])System.Enum.GetValues(typeof(FacialEmotion));
            var openSprites = new Dictionary<FacialEmotion, Sprite>();
            var blinkSprites = new Dictionary<FacialEmotion, Sprite>();
            var restMouthSprites = new Dictionary<FacialEmotion, Sprite>();

            foreach (var emo in emotions)
            {
                string openName = $"EyesBrows_{emo}_Open";
                string blinkName = $"EyesBrows_{emo}_Blink";
                string mouthName = $"Mouth_Rest_{emo}";

                var texOpen = DrawEyesAndBrows(emo, false);
                var texBlink = DrawEyesAndBrows(emo, true);
                var texMouth = DrawRestingMouth(emo);

                SaveTextureAsSprite(texOpen, $"{SPRITES_DIR}/{openName}.png");
                SaveTextureAsSprite(texBlink, $"{SPRITES_DIR}/{blinkName}.png");
                SaveTextureAsSprite(texMouth, $"{SPRITES_DIR}/{mouthName}.png");
            }

            // 2. Generate 6 Phonetic Lip-Sync Mouth Visemes
            var visemes = (MouthViseme[])System.Enum.GetValues(typeof(MouthViseme));
            var visemeSprites = new Sprite[6];
            for (int i = 0; i < visemes.Length; i++)
            {
                var v = visemes[i];
                string vName = $"Viseme_{(int)v}_{v}";
                var texViseme = DrawMouthViseme(v);
                SaveTextureAsSprite(texViseme, $"{SPRITES_DIR}/{vName}.png");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 3. Load all generated Sprites
            foreach (var emo in emotions)
            {
                openSprites[emo] = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/EyesBrows_{emo}_Open.png");
                blinkSprites[emo] = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/EyesBrows_{emo}_Blink.png");
                restMouthSprites[emo] = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Mouth_Rest_{emo}.png");
            }

            for (int i = 0; i < visemes.Length; i++)
            {
                visemeSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_DIR}/Viseme_{i}_{visemes[i]}.png");
            }

            // 4. Create and Populate FacialExpressionDatabaseSO
            var db = AssetDatabase.LoadAssetAtPath<FacialExpressionDatabaseSO>(DB_PATH);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<FacialExpressionDatabaseSO>();
                AssetDatabase.CreateAsset(db, DB_PATH);
            }

            db.emotionSets.Clear();
            for (int i = 0; i < emotions.Length; i++)
            {
                var emo = emotions[i];
                int row = i / 4;
                int col = i % 4;
                db.emotionSets.Add(new EmotionSpriteSet
                {
                    emotion = emo,
                    eyesAndBrowsOpen = openSprites[emo],
                    eyesAndBrowsBlink = blinkSprites[emo],
                    restingEmotionMouth = restMouthSprites[emo],
                    uvAtlasOpenCoord = new Vector2Int(col, row),
                    uvAtlasBlinkCoord = new Vector2Int(col, row + 2),
                    uvAtlasRestingMouthCoord = new Vector2Int(col, row + 4)
                });
            }

            db.phoneticMouthVisemes = visemeSprites;
            db.phoneticMouthUVCoords = new Vector2Int[6]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1)
            };
            db.atlasGridCells = new Vector2Int(4, 4);

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ [Facial DB Created] Saved DefaultFacialExpressionDB at '{DB_PATH}' with {db.emotionSets.Count} emotion sets and 6 phonetic visemes!");

            // 5. Generate SpriteAtlas
            EditorModularFaceRigBuilder.GenerateCharacterFaceSpriteAtlas();

            // 6. Auto-Rig Active Hero in Scene
            RigSceneHeroes(db);
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Sprites")) AssetDatabase.CreateFolder("Assets", "Sprites");
            if (!AssetDatabase.IsValidFolder(SPRITES_DIR)) AssetDatabase.CreateFolder("Assets/Sprites", "CharacterFaces");
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(DATA_DIR)) AssetDatabase.CreateFolder("Assets/Data", "FacialAnimation");
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

        private static void RigSceneHeroes(FacialExpressionDatabaseSO db)
        {
            var candidates = new string[] { "Starter_Female_Rigged_Instance", "Starter_Female_Active", "Player_Hero" };
            foreach (var name in candidates)
            {
                var hero = GameObject.Find(name);
                if (hero != null)
                {
                    Selection.activeGameObject = hero;
                    EditorModularFaceRigBuilder.BuildFaceRigOnSelected();
                    var ctrl = hero.GetComponent<ModularFaceRigController>();
                    if (ctrl != null)
                    {
                        ctrl.expressionDB = db;
                        // Bind hero audio source if present
                        var aud = hero.GetComponent<AudioSource>() ?? hero.GetComponentInChildren<AudioSource>();
                        if (aud != null) ctrl.BindAudioSource(aud);
                        ctrl.SetEmotion(FacialEmotion.Happy);
                        EditorUtility.SetDirty(hero);
                        Debug.Log($"🎭 [Hero Rigged] Successfully bound ModularFaceRigController with Expression DB on '{hero.name}'!");
                    }
                }
            }
        }

        // =========================================================================
        // PROCEDURAL ANIME TEXTURE DRAWING (CLEAN ALPHA + HIGH CONTRAST)
        // =========================================================================
        private static Texture2D CreateClearTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];
            Color32 clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            tex.SetPixels32(pixels);
            return tex;
        }

        private static Texture2D DrawEyesAndBrows(FacialEmotion emotion, bool isBlink)
        {
            var tex = CreateClearTexture(TEX_W, TEX_H);
            Color32 lashColor = new Color32(32, 24, 28, 255);
            Color32 browColor = new Color32(45, 34, 40, 255);
            Color32 irisColor = new Color32(40, 110, 210, 255);
            Color32 pupilColor = new Color32(18, 28, 55, 255);
            Color32 highlight = new Color32(255, 255, 255, 255);

            int leftEyeX = 72;
            int rightEyeX = 184;
            int eyeY = 48;

            if (isBlink)
            {
                // Closed eyes: delicate curved eyelash lines
                DrawCurve(tex, leftEyeX - 32, eyeY, leftEyeX + 32, eyeY, -6f, lashColor, 4);
                DrawCurve(tex, rightEyeX - 32, eyeY, rightEyeX + 32, eyeY, -6f, lashColor, 4);
            }
            else
            {
                // Open eyes: Whites, Iris, Pupil, Highlights, and Upper Lash
                DrawEye(tex, leftEyeX, eyeY, emotion, irisColor, pupilColor, highlight, lashColor);
                DrawEye(tex, rightEyeX, eyeY, emotion, irisColor, pupilColor, highlight, lashColor);
            }

            // Eyebrows based on emotion
            DrawEyebrows(tex, leftEyeX, rightEyeX, eyeY + 36, emotion, browColor);

            tex.Apply();
            return tex;
        }

        private static void DrawEye(Texture2D tex, int cx, int cy, FacialEmotion emotion, Color32 irisColor, Color32 pupilColor, Color32 highlight, Color32 lashColor)
        {
            int rx = 26;
            int ry = 22;

            if (emotion == FacialEmotion.Surprised) { rx = 28; ry = 28; }
            else if (emotion == FacialEmotion.Smug) { ry = 16; }
            else if (emotion == FacialEmotion.HeartEyesBlush) { irisColor = new Color32(240, 70, 120, 255); }

            // Sclera (Eye whites)
            DrawFilledEllipse(tex, cx, cy, rx, ry, new Color32(245, 248, 252, 255));

            // Iris
            DrawFilledEllipse(tex, cx, cy - 2, rx - 6, ry - 4, irisColor);

            // Pupil
            if (emotion == FacialEmotion.HeartEyesBlush)
            {
                // Pink heart pupils
                DrawFilledEllipse(tex, cx - 4, cy + 2, 5, 5, new Color32(255, 220, 235, 255));
                DrawFilledEllipse(tex, cx + 4, cy + 2, 5, 5, new Color32(255, 220, 235, 255));
            }
            else if (emotion == FacialEmotion.CrazyOverload)
            {
                // Spiral/cross pupil
                DrawFilledEllipse(tex, cx, cy - 2, 5, 5, new Color32(255, 230, 80, 255));
            }
            else
            {
                DrawFilledEllipse(tex, cx, cy - 2, 8, 8, pupilColor);
            }

            // Catchlight Highlights
            DrawFilledEllipse(tex, cx - 7, cy + 6, 5, 5, highlight);
            DrawFilledEllipse(tex, cx + 6, cy - 6, 3, 3, highlight);

            // Upper Eyelash line (Thick anime contour)
            DrawCurve(tex, cx - rx - 2, cy + ry - 4, cx + rx + 2, cy + ry - 4, 12f, lashColor, 5);
        }

        private static void DrawEyebrows(Texture2D tex, int leftX, int rightX, int y, FacialEmotion emotion, Color32 col)
        {
            float bend = 4f;
            int yL_start = y;
            int yL_end = y;
            int yR_start = y;
            int yR_end = y;

            switch (emotion)
            {
                case FacialEmotion.Happy:
                    bend = 8f;
                    break;
                case FacialEmotion.Angry:
                    yL_start = y - 8; yL_end = y + 8; // Slanted down towards center
                    yR_start = y + 8; yR_end = y - 8;
                    bend = -4f;
                    break;
                case FacialEmotion.Sad:
                case FacialEmotion.PainHit:
                    yL_start = y + 8; yL_end = y - 6; // Arched up in middle, drooping outward
                    yR_start = y - 6; yR_end = y + 8;
                    bend = 6f;
                    break;
                case FacialEmotion.Surprised:
                    y += 8;
                    bend = 10f;
                    break;
                case FacialEmotion.Smug:
                    yL_start = y + 4; yR_start = y - 2;
                    bend = 6f;
                    break;
            }

            DrawCurve(tex, leftX - 30, yL_start, leftX + 28, yL_end, bend, col, 4);
            DrawCurve(tex, rightX - 28, yR_start, rightX + 30, yR_end, bend, col, 4);
        }

        private static Texture2D DrawRestingMouth(FacialEmotion emotion)
        {
            var tex = CreateClearTexture(TEX_W, 64);
            Color32 mouthLine = new Color32(55, 30, 35, 255);
            int cx = TEX_W / 2;
            int cy = 32;

            switch (emotion)
            {
                case FacialEmotion.Happy:
                    // Gentle curved smile
                    DrawCurve(tex, cx - 22, cy + 4, cx + 22, cy + 4, -8f, mouthLine, 4);
                    break;
                case FacialEmotion.Angry:
                    // Tight down-curved frown
                    DrawCurve(tex, cx - 20, cy - 2, cx + 20, cy - 2, 6f, mouthLine, 4);
                    break;
                case FacialEmotion.Sad:
                    DrawCurve(tex, cx - 18, cy, cx + 18, cy, 7f, mouthLine, 4);
                    break;
                case FacialEmotion.Surprised:
                    // Small 'o' mouth
                    DrawFilledEllipse(tex, cx, cy, 10, 12, mouthLine);
                    DrawFilledEllipse(tex, cx, cy, 7, 9, new Color32(180, 50, 60, 255));
                    break;
                case FacialEmotion.Smug:
                    // Asymmetric smirk
                    DrawCurve(tex, cx - 18, cy - 4, cx + 24, cy + 8, -4f, mouthLine, 4);
                    break;
                case FacialEmotion.HeartEyesBlush:
                    // W-smile / cute open mouth
                    DrawCurve(tex, cx - 20, cy + 2, cx, cy, -6f, mouthLine, 3);
                    DrawCurve(tex, cx, cy, cx + 20, cy + 2, -6f, mouthLine, 3);
                    break;
                case FacialEmotion.CrazyOverload:
                    // Jagged grin
                    DrawCurve(tex, cx - 26, cy, cx + 26, cy, -10f, mouthLine, 5);
                    break;
                case FacialEmotion.PainHit:
                    // Wavy grimace
                    DrawCurve(tex, cx - 22, cy - 4, cx + 22, cy + 4, 5f, mouthLine, 4);
                    break;
                default:
                    // Neutral: neat closed line
                    DrawCurve(tex, cx - 18, cy, cx + 18, cy, -2f, mouthLine, 3);
                    break;
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D DrawMouthViseme(MouthViseme viseme)
        {
            var tex = CreateClearTexture(TEX_W, 64);
            Color32 mouthLine = new Color32(45, 25, 30, 255);
            Color32 innerMouth = new Color32(160, 42, 55, 255);
            Color32 teethColor = new Color32(250, 250, 252, 255);
            Color32 tongueColor = new Color32(230, 95, 115, 255);

            int cx = TEX_W / 2;
            int cy = 32;

            switch (viseme)
            {
                case MouthViseme.Closed_Rest:
                    DrawCurve(tex, cx - 18, cy, cx + 18, cy, -2f, mouthLine, 3);
                    break;

                case MouthViseme.Vowel_I_Small:
                    // Narrow teeth ("ee", "ih")
                    DrawFilledEllipse(tex, cx, cy, 24, 7, innerMouth);
                    DrawFilledEllipse(tex, cx, cy + 2, 20, 3, teethColor);
                    DrawCurve(tex, cx - 26, cy + 2, cx + 26, cy + 2, -2f, mouthLine, 3);
                    break;

                case MouthViseme.Vowel_E_Mid:
                    // Mid-wide ("eh", "ae")
                    DrawFilledEllipse(tex, cx, cy, 26, 12, innerMouth);
                    DrawFilledEllipse(tex, cx, cy + 5, 22, 4, teethColor);
                    DrawFilledEllipse(tex, cx, cy - 5, 16, 6, tongueColor);
                    DrawCurve(tex, cx - 28, cy + 6, cx + 28, cy + 6, -4f, mouthLine, 4);
                    break;

                case MouthViseme.Vowel_U_Round:
                    // Pursed small ("oo", "w")
                    DrawFilledEllipse(tex, cx, cy, 10, 10, innerMouth);
                    DrawFilledEllipse(tex, cx, cy - 3, 7, 5, tongueColor);
                    DrawCurve(tex, cx - 12, cy + 6, cx + 12, cy + 6, -3f, mouthLine, 3);
                    break;

                case MouthViseme.Vowel_O_Round:
                    // Round open ("oh", "aw")
                    DrawFilledEllipse(tex, cx, cy, 16, 18, innerMouth);
                    DrawFilledEllipse(tex, cx, cy + 8, 12, 4, teethColor);
                    DrawFilledEllipse(tex, cx, cy - 8, 12, 7, tongueColor);
                    DrawCurve(tex, cx - 18, cy + 10, cx + 18, cy + 10, -5f, mouthLine, 4);
                    break;

                case MouthViseme.Vowel_A_Wide:
                    // Full open ("ah", "aa")
                    DrawFilledEllipse(tex, cx, cy - 2, 28, 20, innerMouth);
                    DrawFilledEllipse(tex, cx, cy + 9, 22, 5, teethColor);
                    DrawFilledEllipse(tex, cx, cy - 10, 18, 9, tongueColor);
                    DrawCurve(tex, cx - 30, cy + 10, cx + 30, cy + 10, -6f, mouthLine, 4);
                    break;
            }

            tex.Apply();
            return tex;
        }

        // =========================================================================
        // PRIMITIVE DRAWING HELPERS
        // =========================================================================
        private static void DrawFilledEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color32 col)
        {
            int x0 = Mathf.Clamp(cx - rx, 0, tex.width - 1);
            int x1 = Mathf.Clamp(cx + rx, 0, tex.width - 1);
            int y0 = Mathf.Clamp(cy - ry, 0, tex.height - 1);
            int y1 = Mathf.Clamp(cy + ry, 0, tex.height - 1);

            float rx2 = rx * rx;
            float ry2 = ry * ry;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if ((dx * dx) / rx2 + (dy * dy) / ry2 <= 1.0f)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }

        private static void DrawCurve(Texture2D tex, int x0, int y0, int x1, int y1, float bend, Color32 col, int thickness)
        {
            int steps = 40;
            float midX = (x0 + x1) * 0.5f;
            float midY = (y0 + y1) * 0.5f + bend;

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                // Quadratic bezier
                float x = (1f - t) * (1f - t) * x0 + 2f * (1f - t) * t * midX + t * t * x1;
                float y = (1f - t) * (1f - t) * y0 + 2f * (1f - t) * t * midY + t * t * y1;

                int px = Mathf.RoundToInt(x);
                int py = Mathf.RoundToInt(y);

                for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                {
                    for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                    {
                        int fx = px + tx;
                        int fy = py + ty;
                        if (fx >= 0 && fx < tex.width && fy >= 0 && fy < tex.height)
                        {
                            tex.SetPixel(fx, fy, col);
                        }
                    }
                }
            }
        }
    }
}
#endif

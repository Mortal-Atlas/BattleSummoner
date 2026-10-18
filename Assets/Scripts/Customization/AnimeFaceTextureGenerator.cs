using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.Customization
{
    /// <summary>
    /// AnimeFaceTextureGenerator generates high-definition Genshin / Persona aesthetic
    /// 2D anime facial feature atlases (eyes, brows, mouths, visemes) with transparency.
    /// Clean, elegant lines, gradient irises, specular highlights, anti-aliased contours.
    /// </summary>
    public static class AnimeFaceTextureGenerator
    {
        public static void GenerateAllAtlases()
        {
            string dir = "Assets/Textures/Characters/AnimeFace";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Generate Male Atlas (Amber / Bronze eyes, bold anime lashes)
            var maleTex = GenerateAtlas(new Color(0.92f, 0.62f, 0.15f), new Color(0.25f, 0.15f, 0.08f), isFemale: false);
            File.WriteAllBytes($"{dir}/AnimeFace_Male_Atlas.png", maleTex.EncodeToPNG());

            // Generate Female Atlas (Azure / Sapphire eyes, delicate lashes)
            var femaleTex = GenerateAtlas(new Color(0.20f, 0.65f, 0.95f), new Color(0.08f, 0.18f, 0.35f), isFemale: true);
            File.WriteAllBytes($"{dir}/AnimeFace_Female_Atlas.png", femaleTex.EncodeToPNG());

#if UNITY_EDITOR
            AssetDatabase.Refresh();
            ConfigureTextureImporter($"{dir}/AnimeFace_Male_Atlas.png");
            ConfigureTextureImporter($"{dir}/AnimeFace_Female_Atlas.png");
            AssetDatabase.SaveAssets();
#endif
            Debug.Log("[AnimeFaceTextureGenerator] Generated Genshin/Persona anime face atlases.");
        }

#if UNITY_EDITOR
        private static void ConfigureTextureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
#endif

        private static Texture2D GenerateAtlas(Color irisColor, Color shadowIrisColor, bool isFemale)
        {
            const int W = 2048;
            const int H = 2048;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color[] pixels = new Color[W * H];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Atlas Layout (4x4 grid of 512x512 cells):
            // Row 0 (Top, y=1536..2048): Eyes
            //   [0,0] Open Left    [0,1] Open Right    [0,2] HalfBlink Left   [0,3] HalfBlink Right
            // Row 1 (y=1024..1536):
            //   [1,0] Closed Left  [1,1] Closed Right  [1,2] Joy/Smile Left   [1,3] Joy/Smile Right
            // Row 2 (y=512..1024):
            //   [2,0] Focus Left   [2,1] Focus Right   [2,2] Surprised Left   [2,3] Surprised Right
            // Row 3 (Bottom, y=0..512): Mouths
            //   [3,0] Closed/Neutral [3,1] Smile/Joy   [3,2] Viseme A/Talk    [3,3] Viseme O/Gasp

            // Draw Eyes (Col 0/2: Character's Right Eye / Viewer's Left, Col 1/3: Character's Left Eye / Viewer's Right)
            DrawEyeCell(pixels, W, H, col: 0, row: 3, isCharacterRightEye: true,  state: EyeState.Open, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 1, row: 3, isCharacterRightEye: false, state: EyeState.Open, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 2, row: 3, isCharacterRightEye: true,  state: EyeState.HalfBlink, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 3, row: 3, isCharacterRightEye: false, state: EyeState.HalfBlink, irisColor, shadowIrisColor, isFemale);

            DrawEyeCell(pixels, W, H, col: 0, row: 2, isCharacterRightEye: true,  state: EyeState.Closed, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 1, row: 2, isCharacterRightEye: false, state: EyeState.Closed, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 2, row: 2, isCharacterRightEye: true,  state: EyeState.Joy, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 3, row: 2, isCharacterRightEye: false, state: EyeState.Joy, irisColor, shadowIrisColor, isFemale);

            DrawEyeCell(pixels, W, H, col: 0, row: 1, isCharacterRightEye: true,  state: EyeState.Determined, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 1, row: 1, isCharacterRightEye: false, state: EyeState.Determined, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 2, row: 1, isCharacterRightEye: true,  state: EyeState.Surprised, irisColor, shadowIrisColor, isFemale);
            DrawEyeCell(pixels, W, H, col: 3, row: 1, isCharacterRightEye: false, state: EyeState.Surprised, irisColor, shadowIrisColor, isFemale);

            // Draw Mouths (Row 0: bottom row)
            DrawMouthCell(pixels, W, H, col: 0, row: 0, MouthState.Neutral);
            DrawMouthCell(pixels, W, H, col: 1, row: 0, MouthState.Smile);
            DrawMouthCell(pixels, W, H, col: 2, row: 0, MouthState.OpenTalk);
            DrawMouthCell(pixels, W, H, col: 3, row: 0, MouthState.GaspO);

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public enum EyeState { Open, HalfBlink, Closed, Joy, Determined, Surprised }
        public enum MouthState { Neutral, Smile, OpenTalk, GaspO }

        private static void DrawEyeCell(Color[] pixels, int texW, int texH, int col, int row, bool isCharacterRightEye, EyeState state, Color irisCol, Color shadowIrisCol, bool isFemale)
        {
            // isCharacterRightEye: false = Character's Left Eye (Viewer's Right)
            //                      true  = Character's Right Eye (Viewer's Left)
            int cellX = col * 512;
            int cellY = row * 512;
            int centerX = cellX + 256;
            int centerY = cellY + 256;

            Color darkLash = new Color(0.10f, 0.07f, 0.09f, 1f);
            Color softLash = new Color(0.18f, 0.12f, 0.14f, 0.85f);
            Color sclera = new Color(0.97f, 0.98f, 1.0f, 0.96f);
            Color scleraShadow = new Color(0.78f, 0.82f, 0.90f, 0.95f);
            Color browCol = isFemale ? new Color(0.24f, 0.16f, 0.14f, 1f) : new Color(0.18f, 0.14f, 0.12f, 1f);

            float rX = isFemale ? 95f : 105f;
            float rY = isFemale ? 120f : 98f;
            if (state == EyeState.HalfBlink) rY *= 0.40f;
            if (state == EyeState.Determined) rY *= 0.80f;
            if (state == EyeState.Surprised) { rX *= 1.12f; rY *= 1.18f; }

            // 1. Draw Sclera (Eye White)
            if (state != EyeState.Closed && state != EyeState.Joy)
            {
                for (int y = -140; y <= 140; y++)
                {
                    for (int x = -130; x <= 130; x++)
                    {
                        float nx = x / rX;
                        float ny = y / rY;
                        float distSq = nx * nx + ny * ny;
                        if (distSq <= 1.0f)
                        {
                            float alpha = Mathf.Clamp01((1.0f - distSq) * 16f);
                            // Ambient top eyelid shadow cast onto sclera
                            Color sc = y > 15 ? Color.Lerp(sclera, scleraShadow, (y - 15f) / 80f) : sclera;
                            sc.a *= alpha;
                            BlendPixel(pixels, texW, centerX + x, centerY + y, sc);
                        }
                    }
                }

                // 2. Draw Iris & Pupil with rich Genshin / Persona gradient depth
                float irisRX = isFemale ? 60f : 56f;
                float irisRY = isFemale ? 90f : 76f;
                float irisOffsetY = -6f;
                if (state == EyeState.Surprised) { irisRX *= 0.84f; irisRY *= 0.84f; }

                for (int y = -130; y <= 130; y++)
                {
                    for (int x = -110; x <= 110; x++)
                    {
                        float nx = x / irisRX;
                        float ny = (y - irisOffsetY) / irisRY;
                        float distSq = nx * nx + ny * ny;
                        if (distSq <= 1.0f)
                        {
                            float tY = Mathf.InverseLerp(-irisRY, irisRY, y - irisOffsetY);
                            // Top half: deep shadow iris. Bottom half: luminous vibrant gradient
                            Color colGrad = Color.Lerp(irisCol, shadowIrisCol, Mathf.SmoothStep(0.25f, 0.95f, tY));

                            // Genshin-style crescent light reflection in lower iris
                            if (distSq < 0.65f && tY < 0.40f)
                            {
                                float glow = Mathf.Clamp01((0.40f - tY) * 2.8f);
                                colGrad = Color.Lerp(colGrad, Color.white, glow * 0.48f);
                            }

                            // Dark pupil with soft top fade
                            float pDist = (x * x) / (18f * 18f) + ((y - irisOffsetY) * (y - irisOffsetY)) / (34f * 34f);
                            if (pDist <= 1.0f)
                            {
                                colGrad = Color.Lerp(colGrad, new Color(0.04f, 0.02f, 0.05f, 1f), 0.95f);
                            }

                            // Crisp Limbal Ring (outer iris boundary)
                            if (distSq > 0.78f)
                            {
                                float ringT = (distSq - 0.78f) / 0.22f;
                                colGrad = Color.Lerp(colGrad, darkLash, ringT * 0.90f);
                            }

                            float edgeAlpha = Mathf.Clamp01((1.0f - distSq) * 16f);
                            colGrad.a = edgeAlpha;
                            BlendPixel(pixels, texW, centerX + x, centerY + y, colGrad);
                        }
                    }
                }

                // 3. Crisp Anime Specular Glints
                // Main light glint: placed towards the top light source
                int glintX = isCharacterRightEye ? 18 : -18;
                int glintY = 32;
                DrawSoftCircle(pixels, texW, centerX + glintX, centerY + glintY, 16, 24, Color.white);

                // Secondary soft glint on lower opposite side
                int glint2X = isCharacterRightEye ? -16 : 16;
                int glint2Y = -32;
                DrawSoftCircle(pixels, texW, centerX + glint2X, centerY + glint2Y, 8, 14, new Color(1f, 1f, 1f, 0.60f));
            }

            // 4. Eyelash Line & Eyeliner (Flick on OUTER side towards temple!)
            // Outer side is: -X for Character's Right Eye (Viewer's Left), +X for Character's Left Eye (Viewer's Right)
            float outerSign = isCharacterRightEye ? -1f : 1f;

            if (state == EyeState.Closed)
            {
                // Curved closed anime lash arc
                DrawCurvedThickLine(pixels, texW,
                    centerX - 95, centerY + (isCharacterRightEye ? 8 : -15),
                    centerX, centerY - 20,
                    centerX + 95, centerY + (isCharacterRightEye ? -15 : 8),
                    14f, darkLash);
            }
            else if (state == EyeState.Joy)
            {
                // Inverted happy anime crescent (^ ^)
                DrawCurvedThickLine(pixels, texW,
                    centerX - 90, centerY - 15,
                    centerX, centerY + 30,
                    centerX + 90, centerY - 15,
                    15f, darkLash);
            }
            else
            {
                // Upper Eyeliner
                float archY = state == EyeState.Determined ? 78f : 88f;
                float innerY = 22f;
                float outerY = 40f;

                float xInner = centerX - (outerSign * 105f);
                float xOuter = centerX + (outerSign * 115f);

                DrawCurvedThickLine(pixels, texW,
                    xInner, centerY + innerY,
                    centerX, centerY + archY,
                    xOuter, centerY + outerY,
                    16f, darkLash);

                // Elegant outer wing flick
                DrawSoftCircle(pixels, texW, Mathf.RoundToInt(xOuter + outerSign * 10f), centerY + Mathf.RoundToInt(outerY + 8f), 10, 14, darkLash);

                // Lower lash line
                DrawCurvedThickLine(pixels, texW,
                    centerX - 50, centerY - 78,
                    centerX, centerY - 85,
                    centerX + 50, centerY - 80,
                    6f, softLash);

                // Double eyelid fold
                DrawCurvedThickLine(pixels, texW,
                    centerX - 60, centerY + 112,
                    centerX, centerY + 120,
                    centerX + 60, centerY + 114,
                    5f, new Color(0.32f, 0.20f, 0.20f, 0.55f));
            }

            // 5. Eyebrow
            float browBaseY = 165f;
            float browAngle = 0f;
            if (state == EyeState.Determined) browAngle = outerSign * 22f; // angled down inwards
            if (state == EyeState.Joy) browBaseY += 16f;
            if (state == EyeState.Surprised) browBaseY += 28f;

            DrawCurvedThickLine(pixels, texW,
                centerX - 100, centerY + browBaseY - browAngle,
                centerX, centerY + browBaseY + 16f,
                centerX + 100, centerY + browBaseY + browAngle,
                isFemale ? 8f : 12f, browCol);
        }

        private static void DrawMouthCell(Color[] pixels, int texW, int texH, int col, int row, MouthState state)
        {
            int cellX = col * 512;
            int cellY = row * 512;
            int centerX = cellX + 256;
            int centerY = cellY + 256;

            Color lipColor = new Color(0.40f, 0.20f, 0.22f, 0.95f);
            Color insideMouth = new Color(0.48f, 0.15f, 0.20f, 1f);
            Color teethColor = new Color(0.96f, 0.96f, 0.98f, 1f);

            switch (state)
            {
                case MouthState.Neutral:
                    // Subtle, refined anime mouth line
                    DrawCurvedThickLine(pixels, texW, centerX - 65, centerY - 5, centerX, centerY - 2, centerX + 65, centerY - 5, 7f, lipColor);
                    // Under-lip shadow dot
                    DrawSoftCircle(pixels, texW, centerX, centerY - 32, 14, 20, new Color(0.45f, 0.25f, 0.25f, 0.35f));
                    break;

                case MouthState.Smile:
                    // Friendly upturned anime smile
                    DrawCurvedThickLine(pixels, texW, centerX - 80, centerY + 18, centerX, centerY - 14, centerX + 80, centerY + 18, 9f, lipColor);
                    DrawSoftCircle(pixels, texW, centerX, centerY - 36, 16, 22, new Color(0.45f, 0.25f, 0.25f, 0.35f));
                    break;

                case MouthState.OpenTalk:
                    // Open mouth cavity (Ah / speech)
                    for (int y = -50; y <= 50; y++)
                    {
                        for (int x = -75; x <= 75; x++)
                        {
                            float nx = x / 70f;
                            float ny = y / 45f;
                            if (nx * nx + ny * ny <= 1f)
                            {
                                Color c = insideMouth;
                                if (y > 22) c = teethColor; // upper teeth peak
                                BlendPixel(pixels, texW, centerX + x, centerY + y, c);
                            }
                        }
                    }
                    // Outer lip contour
                    DrawCurvedThickLine(pixels, texW, centerX - 75, centerY + 15, centerX, centerY + 45, centerX + 75, centerY + 15, 8f, lipColor);
                    DrawCurvedThickLine(pixels, texW, centerX - 75, centerY + 15, centerX, centerY - 45, centerX + 75, centerY + 15, 8f, lipColor);
                    break;

                case MouthState.GaspO:
                    // Round surprised 'O'
                    for (int y = -45; y <= 45; y++)
                    {
                        for (int x = -40; x <= 40; x++)
                        {
                            float nx = x / 36f;
                            float ny = y / 42f;
                            if (nx * nx + ny * ny <= 1f)
                            {
                                BlendPixel(pixels, texW, centerX + x, centerY + y, insideMouth);
                            }
                        }
                    }
                    // Outer lip ring
                    for (int y = -55; y <= 55; y++)
                    {
                        for (int x = -50; x <= 50; x++)
                        {
                            float nx = x / 44f;
                            float ny = y / 50f;
                            float d = nx * nx + ny * ny;
                            if (d <= 1.25f && d >= 0.75f)
                            {
                                float a = Mathf.Sin((d - 0.75f) / 0.5f * Mathf.PI);
                                BlendPixel(pixels, texW, centerX + x, centerY + y, new Color(lipColor.r, lipColor.g, lipColor.b, a * 0.95f));
                            }
                        }
                    }
                    break;
            }
        }

        private static void DrawSoftCircle(Color[] pixels, int texW, int cx, int cy, float rInner, float rOuter, Color col)
        {
            int rMax = Mathf.CeilToInt(rOuter);
            for (int y = -rMax; y <= rMax; y++)
            {
                for (int x = -rMax; x <= rMax; x++)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (d <= rOuter)
                    {
                        float a = d <= rInner ? 1.0f : Mathf.SmoothStep(1.0f, 0.0f, (d - rInner) / (rOuter - rInner));
                        Color c = col;
                        c.a *= a;
                        BlendPixel(pixels, texW, cx + x, cy + y, c);
                    }
                }
            }
        }

        private static void DrawCurvedThickLine(Color[] pixels, int texW, float x0, float y0, float x1, float y1, float x2, float y2, float thickness, Color col)
        {
            // Quadratic Bezier curve
            int steps = 120;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float it = 1f - t;
                float px = it * it * x0 + 2f * it * t * x1 + t * t * x2;
                float py = it * it * y0 + 2f * it * t * y1 + t * t * y2;

                DrawSoftCircle(pixels, texW, Mathf.RoundToInt(px), Mathf.RoundToInt(py), thickness * 0.4f, thickness * 0.5f, col);
            }
        }

        private static void BlendPixel(Color[] pixels, int texW, int x, int y, Color src)
        {
            if (x < 0 || x >= texW || y < 0 || y >= 2048) return;
            int idx = y * texW + x;
            Color dst = pixels[idx];
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.001f) return;
            Color outRGB = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
            outRGB.a = outA;
            pixels[idx] = outRGB;
        }
    }
}

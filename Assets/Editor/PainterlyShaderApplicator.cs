using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    public static class PainterlyShaderApplicator
    {
        private const string PAINTERLY_SHADER_NAME = "Shader Graphs/PainterlyLitSimple";
        private const string PAINTERLY_FULL_SHADER_NAME = "Shader Graphs/PainterlyLit";

        [MenuItem("BattleSummoner/Apply Painterly Normals Shader To Game", false, 10)]
        public static void ApplyPainterlyShadersToGame()
        {
            var painterlyShader = Shader.Find(PAINTERLY_SHADER_NAME);
            if (painterlyShader == null)
            {
                Debug.LogError($"[Painterly Applicator] Could not find shader '{PAINTERLY_SHADER_NAME}'!");
                return;
            }

            Debug.Log("======================================================================");
            Debug.Log("🎨 APPLYING PAINTERLY NORMALS SHADER ACROSS BATTLE SUMMONER GAME WORLD");
            Debug.Log("======================================================================");

            // 1. Gather all candidate materials in the project
            string[] searchFolders = new[]
            {
                "Assets/Materials",
                "Assets/Models",
                "Assets/Medieval props",
                "Assets/RPG Tiny Hero Duo/Material",
                "Assets/Downloaded Assets"
            };

            string[] guids = AssetDatabase.FindAssets("t:Material", searchFolders);
            int convertedCount = 0;
            int skippedCount = 0;

            var convertedList = new List<string>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Check if material is eligible for painterly shading
                if (!IsEligibleForPainterly(mat, path))
                {
                    skippedCount++;
                    continue;
                }

                if (ConvertMaterialToPainterly(mat, painterlyShader))
                {
                    convertedCount++;
                    convertedList.Add(mat.name);
                    EditorUtility.SetDirty(mat);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"🎉 [PAINTERLY APPLICATION COMPLETE] Converted {convertedCount} materials to '{PAINTERLY_SHADER_NAME}' ({skippedCount} special/transparent/UI materials skipped).");
        }

        private static bool IsEligibleForPainterly(Material mat, string path)
        {
            if (mat == null || mat.shader == null) return false;

            string shaderName = mat.shader.name;

            // Skip if already using painterly shader
            if (shaderName.Contains("PainterlyLit")) return false;

            // Exclude water, foliage leaves, sprites, UI, and custom FX shaders
            if (shaderName.Contains("Water") ||
                shaderName.Contains("Leaf") ||
                shaderName.Contains("Tree") ||
                shaderName.Contains("Sprite") ||
                shaderName.Contains("UI") ||
                shaderName.Contains("TextMeshPro") ||
                shaderName.Contains("Skybox") ||
                shaderName.Contains("Terrain") ||
                shaderName.Contains("Unlit") ||
                shaderName.Contains("Particles") ||
                shaderName.Contains("VFX") ||
                shaderName.Contains("MouthCavity"))
            {
                return false;
            }

            // Exclude materials by name if special
            string lowerName = mat.name.ToLower();
            if (lowerName.Contains("water") ||
                lowerName.Contains("glow") ||
                lowerName.Contains("glass") ||
                lowerName.Contains("splat") ||
                lowerName.Contains("eye") ||
                lowerName.Contains("mouth"))
            {
                return false;
            }

            // Convert standard Lit or Standard shaders
            return shaderName == "Universal Render Pipeline/Lit" ||
                   shaderName == "Standard" ||
                   shaderName == "Standard (Specular setup)";
        }

        public static bool ConvertMaterialToPainterly(Material mat, Shader painterlyShader)
        {
            try
            {
                // Capture original properties
                Texture mainTex = null;
                if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                {
                    mainTex = mat.GetTexture("_BaseMap");
                }
                else if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                {
                    mainTex = mat.GetTexture("_MainTex");
                }
                else if (mat.mainTexture != null)
                {
                    mainTex = mat.mainTexture;
                }

                Color baseColor = Color.white;
                if (mat.HasProperty("_BaseColor"))
                {
                    baseColor = mat.GetColor("_BaseColor");
                }
                else if (mat.HasProperty("_Color"))
                {
                    baseColor = mat.GetColor("_Color");
                }

                float smoothness = 0.25f;
                if (mat.HasProperty("_Smoothness"))
                {
                    smoothness = mat.GetFloat("_Smoothness");
                }
                else if (mat.HasProperty("_Glossiness"))
                {
                    smoothness = mat.GetFloat("_Glossiness");
                }

                float metallic = 0.0f;
                if (mat.HasProperty("_Metallic"))
                {
                    metallic = mat.GetFloat("_Metallic");
                }

                // Switch shader
                mat.shader = painterlyShader;

                // Re-bind properties to PainterlyLitSimple
                if (mainTex != null)
                {
                    mat.SetTexture("_BC_rgb_smoothness_a", mainTex);
                }
                mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Smoothness", Mathf.Clamp(smoothness, 0.05f, 0.65f));
                mat.SetFloat("_Metalic", Mathf.Clamp01(metallic));

                // Tune painterly parameters for optimal painterly brushstrokes
                mat.SetFloat("_StrokeRandomness", 0.75f);
                mat.SetFloat("_StrokeScale", 18.0f);
                mat.SetFloat("_UseBlendingNoise", 1.0f);
                mat.SetFloat("_StrokeBlendingNoiseStrength", 0.28f);
                mat.SetFloat("_StrokeBlendingNoiseScale", 8.0f);
                mat.SetFloat("_Variation", 0.14f);

                // Subtle warm & cool pastel painterly ambient variations
                mat.SetColor("_VariationColorA", new Color(1.05f, 0.98f, 0.92f, 1.0f)); // Warm daylight wash
                mat.SetColor("_VariationColorB", new Color(0.92f, 0.96f, 1.05f, 1.0f)); // Cool skylight wash

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Painterly Applicator] Failed to convert '{mat.name}': {ex.Message}");
                return false;
            }
        }
    }
}

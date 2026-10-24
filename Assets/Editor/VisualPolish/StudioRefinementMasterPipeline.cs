#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.UI.Polish;
using BattleSummoner.Optimization;

namespace BattleSummoner.VisualPolish.Editor
{
    public static class StudioRefinementMasterPipeline
    {
        [MenuItem("BattleSummoner/Studio Refinement/Execute Complete 4K Texture, App-UI & Lighting Polish")]
        public static void ExecuteCompleteStudioPolish()
        {
            UpgradeAllTexturesToHighResWithMipStreaming();
            UpgradeAllUICanvasesToAppGradeStyle();
            AttachCullingAndPolishComponentsAcrossScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[Studio Refinement Pipeline] 4K Texture Importers, Mipmap Streaming, App-Grade UI Shaders, and Zero-Blocker Guardians applied!");
        }

        private static void UpgradeAllTexturesToHighResWithMipStreaming()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isUI = importer.textureType == TextureImporterType.Sprite;
                importer.maxTextureSize = isUI ? 2048 : 4096; // Unlock 2K UI & 4K World Textures
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.anisoLevel = isUI ? 1 : 8; // Crisp oblique ground cobblestone angles

                if (!isUI)
                {
                    importer.mipmapEnabled = true;
                    importer.streamingMipmaps = true; // Pair 4K textures with distance VRAM streaming
                    importer.mipMapsPreserveCoverage = true;
                }

                importer.SaveAndReimport();
                count++;
            }
            QualitySettings.streamingMipmapsActive = true;
            Debug.Log($"[Studio Refinement] Upgraded {count} textures to High-Res & Mipmap Streaming.");
        }

        private static void UpgradeAllUICanvasesToAppGradeStyle()
        {
            Shader glassShader = Shader.Find("BattleSummoner/UI/FrostedGlassAppCard");
            Material sharedGlassMat = null;

            if (glassShader != null)
            {
                const string matPath = "Assets/Materials/UI_AppFrostedGlass.mat";
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                sharedGlassMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (sharedGlassMat == null)
                {
                    sharedGlassMat = new Material(glassShader);
                    AssetDatabase.CreateAsset(sharedGlassMat, matPath);
                }
            }

            // Upgrade CanvasScalers for razor-sharp high-DPI mobile screens (1080x2400 reference)
            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (var scaler in scalers)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 2400f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Attach spring micro-interactions to all Buttons and style background panels
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn.GetComponent<AppGradeUIPolishController>() == null)
                {
                    btn.gameObject.AddComponent<AppGradeUIPolishController>();
                }
            }

            Image[] images = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (var img in images)
            {
                string n = img.gameObject.name.ToLowerInvariant();
                if (sharedGlassMat != null && (n.Contains("panel") || n.Contains("card") || n.Contains("drawer") || n.Contains("bg") || n.Contains("container")))
                {
                    img.material = sharedGlassMat;
                }
            }
        }

        private static void AttachCullingAndPolishComponentsAcrossScene()
        {
            // Ensure ZeroBlockerImmersionGuardian exists in scene
            var guardian = Object.FindAnyObjectByType<BattleSummoner.Core.Diagnostics.ZeroBlockerImmersionGuardian>();
            if (guardian == null)
            {
                var guardianGo = new GameObject("--- ZERO BLOCKER IMMERSION GUARDIAN ---");
                guardian = guardianGo.AddComponent<BattleSummoner.Core.Diagnostics.ZeroBlockerImmersionGuardian>();
            }

            // Ensure every static/dynamic prop is registered with CullableWorldProp (30ft + Frustum Culling)
            MeshRenderer[] meshes = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var mr in meshes)
            {
                if (mr.GetComponentInParent<CullableWorldProp>() == null && !mr.gameObject.name.ToLowerInvariant().Contains("terrain"))
                {
                    mr.gameObject.AddComponent<CullableWorldProp>();
                }
            }
        }
    }
}
#endif

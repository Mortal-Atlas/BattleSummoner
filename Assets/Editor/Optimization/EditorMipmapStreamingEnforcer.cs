#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.Optimization.Editor
{
    public static class EditorMipmapStreamingEnforcer
    {
        [MenuItem("BattleSummoner/Optimization/1. Enable Hardware Mipmap Streaming on All 3D Textures")]
        public static void EnableMipmapStreamingAcrossProject()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            int updatedCount = 0;

            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // Skip UI Sprites (2D HUD icons do not use 3D distance mipmaps)
                if (importer.textureType == TextureImporterType.Sprite) continue;

                bool modified = false;
                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    modified = true;
                }
                if (!importer.streamingMipmaps)
                {
                    importer.streamingMipmaps = true;
                    importer.streamingMipmapsPriority = 0;
                    modified = true;
                }

                if (modified)
                {
                    importer.SaveAndReimport();
                    updatedCount++;
                }
            }

            QualitySettings.streamingMipmapsActive = true;
            Debug.Log($"[Optimization Pipeline] Enabled Mipmap Streaming on {updatedCount} 3D textures.");
        }
    }
}
#endif

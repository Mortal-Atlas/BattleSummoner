#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorScripts
{
    public static class SceneMeshCompressor
    {
        [MenuItem("BattleSummoner/Optimization/Compress Scene Meshes and Save", false, 10)]
        public static void CompressSceneMeshes()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[SceneMeshCompressor] Starting mesh deduplication and extraction on {activeScene.path}...");

            string folder = "Assets/GeneratedMeshes";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "GeneratedMeshes");
            }

            // Pre-cache all existing assets in GeneratedMeshes
            var cachedAssets = new Dictionary<string, Mesh>();
            string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { folder });
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(p);
                if (m != null)
                {
                    cachedAssets[m.name] = m;
                    string filename = Path.GetFileNameWithoutExtension(p);
                    cachedAssets[filename] = m;
                }
            }

            Debug.Log($"[SceneMeshCompressor] Loaded {cachedAssets.Count} pre-existing mesh assets from {folder}");

            var meshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            int convertedFilters = 0;
            int newAssetsCreated = 0;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                var mf = meshFilters[i];
                var m = mf.sharedMesh;
                if (m == null) continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) continue;

                string key = GetMeshKey(m);
                if (!cachedAssets.TryGetValue(key, out Mesh assetMesh))
                {
                    string assetPath = $"{folder}/{key}.asset";
                    assetMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                    if (assetMesh == null)
                    {
                        Mesh copy = UnityEngine.Object.Instantiate(m);
                        copy.name = key;
                        AssetDatabase.CreateAsset(copy, assetPath);
                        assetMesh = copy;
                        newAssetsCreated++;
                    }
                    cachedAssets[key] = assetMesh;
                }

                mf.sharedMesh = assetMesh;
                convertedFilters++;
            }

            var meshColliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
            int convertedColliders = 0;
            for (int i = 0; i < meshColliders.Length; i++)
            {
                var mc = meshColliders[i];
                var m = mc.sharedMesh;
                if (m == null) continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) continue;

                string key = GetMeshKey(m);
                if (!cachedAssets.TryGetValue(key, out Mesh assetMesh))
                {
                    string assetPath = $"{folder}/{key}.asset";
                    assetMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                    if (assetMesh == null)
                    {
                        Mesh copy = UnityEngine.Object.Instantiate(m);
                        copy.name = key;
                        AssetDatabase.CreateAsset(copy, assetPath);
                        assetMesh = copy;
                        newAssetsCreated++;
                    }
                    cachedAssets[key] = assetMesh;
                }

                mc.sharedMesh = assetMesh;
                convertedColliders++;
            }

            if (newAssetsCreated > 0)
            {
                AssetDatabase.SaveAssets();
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            bool saved = EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"[SceneMeshCompressor] Converted {convertedFilters} MeshFilters, {convertedColliders} MeshColliders. Created {newAssetsCreated} new assets. Scene saved: {saved}");
        }

        private static string GetMeshKey(Mesh m)
        {
            string baseName = string.IsNullOrEmpty(m.name) ? "Mesh" : m.name;
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(invalid, '_');
            }
            baseName = baseName.Replace(" ", "_").Replace(":", "_").Replace("(", "_").Replace(")", "_");
            return $"{baseName}_{m.vertexCount}_{m.triangles.Length}";
        }
    }
}
#endif

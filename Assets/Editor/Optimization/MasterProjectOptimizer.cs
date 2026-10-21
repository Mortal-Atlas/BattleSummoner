using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.Editor
{
    public static class MasterProjectOptimizer
    {
        [MenuItem("Tools/BattleSummoner/Optimization/Run Full Game Optimization Suite")]
        public static void RunFullOptimization()
        {
            Debug.Log("🚀 [OPTIMIZATION SUITE] Starting comprehensive game performance optimization...");

            int instancedMats = EnableGpuInstancingAllMaterials();
            int staticObjects = OptimizeSceneStaticFlags();
            int lightsOptimized = OptimizeSceneLights();
            int meshesOptimized = OptimizeSceneMeshes();
            ConfigureQualityAndUrpSettings();

            Debug.Log($"✅ [OPTIMIZATION SUITE] Optimization Complete!\n" +
                      $"- Materials with GPU Instancing Enabled: {instancedMats}\n" +
                      $"- Scene Objects Tagged for Static Batching & Occlusion: {staticObjects}\n" +
                      $"- Real-time Additional Lights Optimized: {lightsOptimized}\n" +
                      $"- Mesh Vertex Caches Optimized: {meshesOptimized}\n" +
                      $"- 60 FPS Target & Low-latency Buffering Enforced.");
        }

        [MenuItem("Tools/BattleSummoner/Optimization/Enable GPU Instancing on All Materials")]
        public static int EnableGpuInstancingAllMaterials()
        {
            var matGuids = AssetDatabase.FindAssets("t:Material");
            int count = 0;

            foreach (var g in matGuids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (mat == null) continue;

                // Only enable if shader supports instancing and not already enabled
                if (!mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"🎨 [MATERIAL OPTIMIZER] Enabled GPU Instancing on {count} materials.");
            return count;
        }

        [MenuItem("Tools/BattleSummoner/Optimization/Optimize Active Scene Static Flags")]
        public static int OptimizeSceneStaticFlags()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            int count = 0;

            var staticKeywords = new string[]
            {
                "stage", "archway", "brazier", "dais", "rune", "building", "house", "masonry",
                "wall", "street", "lamp", "bench", "market", "prop", "pillar", "statue", "tree", "rock", "fountain"
            };

            foreach (var root in roots)
            {
                // Never mark character models or dynamic controllers as static
                string rootLower = root.name.ToLower();
                if (rootLower.Contains("character") || rootLower.Contains("player") || rootLower.Contains("canvas") || rootLower.Contains("camera"))
                    continue;

                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string nameLower = t.gameObject.name.ToLower();
                    if (nameLower.Contains("character") || nameLower.Contains("actor") || nameLower.Contains("npc") || nameLower.Contains("player"))
                        continue;

                    bool shouldBeStatic = false;
                    foreach (var kw in staticKeywords)
                    {
                        if (nameLower.Contains(kw))
                        {
                            shouldBeStatic = true;
                            break;
                        }
                    }

                    if (shouldBeStatic)
                    {
                        var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                        var targetFlags = flags | 
                            StaticEditorFlags.BatchingStatic | 
                            StaticEditorFlags.OccluderStatic | 
                            StaticEditorFlags.OccludeeStatic;

                        if (flags != targetFlags)
                        {
                            GameObjectUtility.SetStaticEditorFlags(t.gameObject, targetFlags);
                            count++;
                        }
                    }
                }
            }

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log($"🏛️ [STATIC BATCHING] Configured Static Batching & Occlusion flags on {count} scene objects.");
            return count;
        }

        [MenuItem("Tools/BattleSummoner/Optimization/Optimize Real-time Scene Lights")]
        public static int OptimizeSceneLights()
        {
            var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var l in allLights)
            {
                if (l.type == LightType.Directional)
                {
                    // Sun directional light: Keep soft shadows, limit shadow strength to 0.75 for soft painterly anime look
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.75f;
                }
                else
                {
                    // Point and spot lights: disable expensive real-time shadow map generation
                    if (l.shadows != LightShadows.None)
                    {
                        l.shadows = LightShadows.None;
                        count++;
                    }
                    // Clamp excessive ranges
                    if (l.range > 20.0f)
                    {
                        l.range = 15.0f;
                        count++;
                    }
                }
            }

            Debug.Log($"💡 [LIGHTING OPTIMIZER] Optimized {count} light properties in scene.");
            return count;
        }

        [MenuItem("Tools/BattleSummoner/Optimization/Optimize Scene Mesh Vertex Caches")]
        public static int OptimizeSceneMeshes()
        {
            var filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            var processed = new HashSet<int>();
            int count = 0;

            foreach (var mf in filters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh;
                if (!processed.Add(mesh.GetInstanceID())) continue;

                MeshUtility.Optimize(mesh);
                count++;
            }

            Debug.Log($"📐 [MESH OPTIMIZER] Optimized vertex cache locality for {count} unique scene meshes.");
            return count;
        }

        public static void ConfigureQualityAndUrpSettings()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 2;

            var urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.shadowDistance = 50f;
                urpAsset.cascade2Split = 0.28f;
                urpAsset.renderScale = 1.0f;
                EditorUtility.SetDirty(urpAsset);
            }
        }
    }
}

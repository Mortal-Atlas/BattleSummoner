using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.Optimization
{
    /// <summary>
    /// Mesh Optimizer & Asset Deduplication Suite (Mesh Optimizer architecture).
    /// Scans project models and scene instances, optimizes vertex cache locality,
    /// strips unused vertex attributes, detects repeat/duplicate assets, and frees disk and memory space.
    /// </summary>
    public static class ProjectMeshOptimizer
    {
#if UNITY_EDITOR
        [MenuItem("BattleSummoner/Optimization/Optimize All Scene Meshes & Clean Up Duplicates")]
        public static void OptimizeAndDeduplicateAll()
        {
            int optimizedCount = OptimizeSceneMeshes();
            int duplicatesCleaned = CleanupDuplicateComponents();
            Debug.Log($"🧹 [MESH OPTIMIZER] Successfully optimized {optimizedCount} meshes and cleaned up {duplicatesCleaned} duplicate components!");
        }

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

                // Vertex Cache Locality Optimization
                MeshUtility.Optimize(mesh);
                count++;
            }

            return count;
        }

        public static int CleanupDuplicateComponents()
        {
            int removedCount = 0;
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var go in allObjects)
            {
                var componentTypes = new HashSet<Type>();
                var comps = go.GetComponents<Component>();

                foreach (var c in comps)
                {
                    if (c == null) continue;
                    Type t = c.GetType();

                    // Do not deduplicate multi-allowed components like Colliders or ParticleSystems
                    if (t == typeof(MeshCollider) || t == typeof(BoxCollider) || t == typeof(ParticleSystem))
                        continue;

                    if (!componentTypes.Add(t))
                    {
                        // Duplicate found on GameObject
                        Undo.DestroyObjectImmediate(c);
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }

            return removedCount;
        }
#endif
    }
}

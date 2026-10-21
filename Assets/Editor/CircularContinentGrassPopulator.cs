using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Environment;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// CircularContinentGrassPopulator populates dense, lush interactive anime wind grass
    /// (from Nyxis Wind Grass Interaction System) across ALL designated grass areas of the Circular Continent.
    /// 
    /// Architecture:
    /// 1. Seamless Global Lattice: Uses exact mathematical chunk subdivisions (zero remainder) and
    ///    continuous hexagonal row-staggering across chunk boundaries, eliminating all chunk-edge seam gaps.
    /// 2. Barycentric Face & Edge Blending: Directly samples hit triangle vertices and uses barycentric
    ///    coordinates to interpolate vertex colors and normals smoothly across all terrain faces.
    /// 3. Soft Probabilistic Edge Feathering: Softly blends grass density and scales down blades at biome
    ///    transitions and slope edges, replacing harsh boolean cutoffs with natural organic gradients.
    /// 4. Organizes grass into spatial chunks, combining each chunk into a single unified mesh (1 draw call).
    /// 5. Attaches GrassLODSpatialStreamer to dynamically unload chunks beyond 150m (0 draw calls, 0 overhead).
    /// 6. Custom_URP_Genshin_Grass_Unity6 shader fades grass to transparent past the 400-foot (121.92m) threshold.
    /// </summary>
    public static class CircularContinentGrassPopulator
    {
        private const string ROOT_NAME = "--- WORLD WIND GRASS ---";
        private const string GRASS_PREFAB_PATH = "Assets/Nyxis_GrassInteractionSystem/Prefab/Grass_Patch.prefab";
        private const string GRASS_MAT_PATH = "Assets/Nyxis_GrassInteractionSystem/Mat/Custom_URP_Genshin_Grass_Unity6.mat";
        
        private const float CHUNK_SIZE = 18f;
        private const float TARGET_SPACING = 0.85f; // Dense, lush frequency with seamless interlocking

        [MenuItem("BattleSummoner/Populate Circular Continent Wind Grass", false, 10)]
        public static string PopulateGrass()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            var terrainObj = GameObject.Find("Circular_Continent_Terrain");
            if (terrainObj == null)
            {
                return "Error: Circular_Continent_Terrain not found in scene!";
            }

            var terrainCollider = terrainObj.GetComponent<MeshCollider>();
            var terrainMF = terrainObj.GetComponent<MeshFilter>();
            if (terrainCollider == null || terrainMF == null || terrainMF.sharedMesh == null)
            {
                return "Error: Terrain mesh collider or mesh missing!";
            }

            var grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GRASS_PREFAB_PATH);
            var grassMat = AssetDatabase.LoadAssetAtPath<Material>(GRASS_MAT_PATH);
            if (grassPrefab == null || grassMat == null)
            {
                return "Error: Grass prefab or material missing!";
            }

            var sourceMF = grassPrefab.GetComponentInChildren<MeshFilter>();
            if (sourceMF == null || sourceMF.sharedMesh == null)
            {
                return "Error: Grass source mesh missing!";
            }
            Mesh sourceMesh = sourceMF.sharedMesh;

            // Remove existing grass root if present
            var existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            var rootObj = new GameObject(ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(rootObj, "Populate Continent Wind Grass");

            // Attach / verify WindGrassInteractionDriver on Player_Hero
            var hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                var driver = hero.GetComponent<WindGrassInteractionDriver>();
                if (driver == null)
                {
                    driver = Undo.AddComponent<WindGrassInteractionDriver>(hero);
                }
                driver.SetInteractionRadius(2.6f);
            }

            // Read terrain mesh data for barycentric face blending
            Mesh tMesh = terrainMF.sharedMesh;
            Color[] meshColors = tMesh.colors;
            Vector3[] meshNormals = tMesh.normals;
            int[] meshTris = tMesh.triangles;
            bool hasVertexColors = (meshColors != null && meshColors.Length > 0);
            bool hasNormals = (meshNormals != null && meshNormals.Length > 0);

            // Continent bounds
            Bounds bounds = terrainCollider.bounds;
            float minX = bounds.min.x + 20f;
            float maxX = bounds.max.x - 20f;
            float minZ = bounds.min.z + 20f;
            float maxZ = bounds.max.z - 20f;

            int chunkCountX = Mathf.CeilToInt((maxX - minX) / CHUNK_SIZE);
            int chunkCountZ = Mathf.CeilToInt((maxZ - minZ) / CHUNK_SIZE);

            // Exact mathematical subdivision per chunk (zero leftover edge gap)
            int samplesPerChunk = Mathf.RoundToInt(CHUNK_SIZE / TARGET_SPACING); // 21
            float step = CHUNK_SIZE / samplesPerChunk; // exact step (~0.857m)

            var registeredChunks = new List<GrassLODSpatialStreamer.GrassChunk>();
            int totalPatchesSpawned = 0;
            int chunksCreated = 0;

            var combineInstances = new List<CombineInstance>();
            UnityEngine.Random.InitState(42819);

            // Mini-boss dais locations to exclude
            Vector2[] miniBossLocs = new Vector2[]
            {
                new Vector2(130.0f, -210.0f),
                new Vector2(-180.0f, -330.0f),
                new Vector2(-270.0f, -310.0f),
                new Vector2(-380.0f, -330.0f),
                new Vector2(-220.0f, -210.0f),
                new Vector2(-130.0f, 260.0f)
            };

            for (int cz = 0; cz < chunkCountZ; cz++)
            {
                float chunkZMin = minZ + cz * CHUNK_SIZE;

                for (int cx = 0; cx < chunkCountX; cx++)
                {
                    float chunkXMin = minX + cx * CHUNK_SIZE;
                    combineInstances.Clear();

                    for (int sz = 0; sz < samplesPerChunk; sz++)
                    {
                        // Seamless hexagonal row-staggering using global row index
                        int globalRow = cz * samplesPerChunk + sz;
                        float rowOffset = (globalRow % 2 == 1) ? (0.5f * step) : 0f;

                        for (int sx = 0; sx < samplesPerChunk; sx++)
                        {
                            float jx = UnityEngine.Random.Range(-0.35f, 0.35f) * step;
                            float jz = UnityEngine.Random.Range(-0.35f, 0.35f) * step;

                            float sampleX = chunkXMin + (sx + 0.5f) * step + rowOffset + jx;
                            float sampleZ = chunkZMin + (sz + 0.5f) * step + jz;

                            // Distance and sector exclusion checks
                            float distFromOrigin = Mathf.Sqrt(sampleX * sampleX + sampleZ * sampleZ);

                            // Inner Citadel Core (R < 70m)
                            if (distFromOrigin < 70f) continue;

                            // Moat Water (R 70m - 106m)
                            if (distFromOrigin >= 70f && distFromOrigin <= 106f) continue;

                            // Perimeter Highway (R 106m - 118m)
                            if (distFromOrigin >= 106f && distFromOrigin <= 118f) continue;

                            // 6 Radial Viaduct Bridges (Angles: 30, 90, 150, 210, 270, 330 deg)
                            if (distFromOrigin <= 122f)
                            {
                                float angleDeg = Mathf.Atan2(sampleZ, sampleX) * Mathf.Rad2Deg;
                                if (angleDeg < 0) angleDeg += 360f;
                                bool onBridge = false;
                                for (int b = 0; b < 6; b++)
                                {
                                    float bridgeAngle = 30f + b * 60f;
                                    float diff = Mathf.Abs(Mathf.DeltaAngle(angleDeg, bridgeAngle));
                                    float arc = distFromOrigin * Mathf.Deg2Rad * diff;
                                    if (arc < 5.2f) { onBridge = true; break; }
                                }
                                if (onBridge) continue;
                            }

                            // Farmland 3x3 gridded agricultural plots exclusion
                            if (sampleX >= 160f && sampleX <= 245f && Mathf.Abs(sampleZ) >= 65f && Mathf.Abs(sampleZ) <= 145f) continue;

                            // Floor Boss Obsidian Dais exclusion at (260, -270)
                            float dxBoss = sampleX - 260f;
                            float dzBoss = sampleZ - (-270f);
                            if (dxBoss * dxBoss + dzBoss * dzBoss < 44f * 44f) continue;

                            // STRICT VOLCANO EXCLUSION (Exclude both (260, -270) and (-260, 270) with 190m radius)
                            float dxVolc1 = sampleX - 260f;
                            float dzVolc1 = sampleZ - (-270f);
                            if (dxVolc1 * dxVolc1 + dzVolc1 * dzVolc1 < 190f * 190f) continue;

                            float dxVolc2 = sampleX - (-260f);
                            float dzVolc2 = sampleZ - 270f;
                            if (dxVolc2 * dxVolc2 + dzVolc2 * dzVolc2 < 190f * 190f) continue;

                            // 6 Mini-Boss Stone Platforms exclusion (22m radius)
                            bool onMiniBossDais = false;
                            for (int m = 0; m < miniBossLocs.Length; m++)
                            {
                                float mdx = sampleX - miniBossLocs[m].x;
                                float mdz = sampleZ - miniBossLocs[m].y;
                                if (mdx * mdx + mdz * mdz < 22f * 22f) { onMiniBossDais = true; break; }
                            }
                            if (onMiniBossDais) continue;

                            // Raycast down to find exact surface elevation and hit triangle
                            Ray ray = new Ray(new Vector3(sampleX, 350f, sampleZ), Vector3.down);
                            if (!terrainCollider.Raycast(ray, out RaycastHit hit, 400f)) continue;

                            // Altitude check: STRICT DRY LAND CHECK (Water level is Y=18.0m)
                            if (hit.point.y < 18.5f) continue;

                            // Barycentric Face Interpolation (eliminates per-face discretization & hard edge seams)
                            Color blendedCol = Color.green;
                            Vector3 blendedNormal = hit.normal;

                            int tri = hit.triangleIndex;
                            if (tri >= 0 && tri * 3 + 2 < meshTris.Length)
                            {
                                int i0 = meshTris[tri * 3];
                                int i1 = meshTris[tri * 3 + 1];
                                int i2 = meshTris[tri * 3 + 2];
                                Vector3 bary = hit.barycentricCoordinate;

                                if (hasVertexColors)
                                {
                                    blendedCol = meshColors[i0] * bary.x + meshColors[i1] * bary.y + meshColors[i2] * bary.z;
                                }

                                if (hasNormals)
                                {
                                    blendedNormal = (meshNormals[i0] * bary.x + meshNormals[i1] * bary.y + meshNormals[i2] * bary.z).normalized;
                                }
                            }

                            // Biome weights from blended vertex color: Grass (R), Rock (G), Sand (B), Snow (A)
                            float grassWeight = blendedCol.r;
                            float nonGrassWeight = Mathf.Max(blendedCol.g, Mathf.Max(blendedCol.b, blendedCol.a));

                            // Smooth organic probability gradients across face edges and biome transitions
                            float grassFactor = Mathf.SmoothStep(0.18f, 0.42f, grassWeight);
                            float nonGrassPenalty = 1.0f - Mathf.SmoothStep(0.20f, 0.40f, nonGrassWeight);
                            float slopeFactor = Mathf.SmoothStep(0.52f, 0.70f, blendedNormal.y);
                            float heightFactor = Mathf.SmoothStep(18.5f, 19.5f, hit.point.y);

                            float totalProbability = grassFactor * nonGrassPenalty * slopeFactor * heightFactor;
                            if (totalProbability < 0.05f) continue;
                            if (UnityEngine.Random.value > totalProbability) continue;

                            // Edge scale feathering: near face boundaries or transition edges, grass gently scales down
                            float edgeScaleFactor = Mathf.Lerp(0.72f, 1.0f, totalProbability);

                            // Transform for combined mesh instance
                            Vector3 patchPos = hit.point + blendedNormal * 0.02f;
                            Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, blendedNormal);
                            Quaternion yawRot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            Quaternion finalRot = Quaternion.Slerp(yawRot, slopeRot * yawRot, 0.42f);

                            float baseScale = UnityEngine.Random.Range(1.20f, 1.80f) * edgeScaleFactor;
                            Vector3 finalScale = new Vector3(
                                baseScale * UnityEngine.Random.Range(0.92f, 1.10f),
                                baseScale * UnityEngine.Random.Range(0.85f, 1.25f),
                                baseScale * UnityEngine.Random.Range(0.92f, 1.10f)
                            );

                            Matrix4x4 patchMatrix = Matrix4x4.TRS(patchPos, finalRot, finalScale);

                            var ci = new CombineInstance();
                            ci.mesh = sourceMesh;
                            ci.transform = patchMatrix;
                            combineInstances.Add(ci);
                        }
                    }

                    if (combineInstances.Count == 0) continue;

                    // Create chunk GameObject
                    var chunkObj = new GameObject($"GrassChunk_{cx:D2}_{cz:D2}");
                    chunkObj.transform.SetParent(rootObj.transform);
                    chunkObj.transform.position = Vector3.zero;

                    var mf = chunkObj.AddComponent<MeshFilter>();
                    var mr = chunkObj.AddComponent<MeshRenderer>();

                    Mesh combinedMesh = new Mesh();
                    combinedMesh.name = $"Mesh_GrassChunk_{cx:D2}_{cz:D2}";
                    combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
                    combinedMesh.RecalculateBounds();

                    mf.sharedMesh = combinedMesh;
                    mr.sharedMaterial = grassMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // High performance: terrain shadows cover grass
                    mr.receiveShadows = true;

                    // Register chunk for spatial LOD streaming
                    Vector3 chunkCenter = combinedMesh.bounds.center;
                    float chunkRadius = Mathf.Max(combinedMesh.bounds.extents.x, combinedMesh.bounds.extents.z);

                    registeredChunks.Add(new GrassLODSpatialStreamer.GrassChunk
                    {
                        gameObject = chunkObj,
                        center = chunkCenter,
                        radius = chunkRadius,
                        isActive = true
                    });

                    totalPatchesSpawned += combineInstances.Count;
                    chunksCreated++;
                }
            }

            // Configure 400-foot (121.92m) fade distances on grass material
            if (grassMat.HasProperty("_FadeStartDist")) grassMat.SetFloat("_FadeStartDist", 121.92f);
            if (grassMat.HasProperty("_FadeEndDist")) grassMat.SetFloat("_FadeEndDist", 145.0f);
            EditorUtility.SetDirty(grassMat);

            // Attach and configure GrassLODSpatialStreamer for 400+ foot streaming
            var streamer = rootObj.AddComponent<GrassLODSpatialStreamer>();
            streamer.SetUnloadDistance(150.0f);
            streamer.RegisterChunks(registeredChunks);

            EditorUtility.SetDirty(rootObj);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            return $"Complete! Created {chunksCreated} optimized grass chunks containing {totalPatchesSpawned} blended grass patches. Zero edge gaps, barycentric face blending active.";
        }
    }
}

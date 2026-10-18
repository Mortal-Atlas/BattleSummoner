using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Environment;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// WindGrassPopulator populates interactive wind-swaying anime grass patches
    /// from Nyxis Wind Grass Interaction System (URP_Grass shader) strictly on the green patches outside the kingdom.
    /// 
    /// Biome Ground-Truth:
    /// In IslandBiomeSplatLit.shader and Island_Overworld_Terrain:
    /// - R channel (> 0.60): Lush Green Grass / Meadows / Forest ground.
    /// - G channel (> 0.15): Arid Desert sand (STRICTLY ZERO GRASS).
    /// - B channel (> 0.15): Volcanic basalt / caldera rock (STRICTLY ZERO GRASS).
    /// - A channel (> 0.35): Beach sand and riverbeds (STRICTLY ZERO GRASS).
    /// 
    /// Spatial Boundaries:
    /// - Outside Kingdom: R = 350m to 735m.
    /// - Avoid 8 radial rivers (within 34m).
    /// - Avoid water / underwater (Y < 0.5m).
    /// - Avoid vertical rock cliffs (normal.y < 0.72).
    /// </summary>
    public static class WindGrassPopulator
    {
        private const string ROOT_NAME = "--- WORLD WIND GRASS ---";
        private const string GRASS_PREFAB_PATH = "Assets/Nyxis_GrassInteractionSystem/Prefab/Grass_Patch.prefab";

        private class TerrainSpatialHash
        {
            private const int GRID_DIM = 85;
            private const float CELL_SIZE = 20f;
            private const float HALF_SPAN = 850f;

            private readonly Vector3[] verts;
            private readonly Color[] colors;
            private readonly List<int>[,] cells;

            public TerrainSpatialHash(Vector3[] meshVerts, Color[] meshColors)
            {
                verts = meshVerts;
                colors = meshColors;
                cells = new List<int>[GRID_DIM, GRID_DIM];

                for (int i = 0; i < verts.Length; i++)
                {
                    int gx = Mathf.Clamp((int)((verts[i].x + HALF_SPAN) / CELL_SIZE), 0, GRID_DIM - 1);
                    int gz = Mathf.Clamp((int)((verts[i].z + HALF_SPAN) / CELL_SIZE), 0, GRID_DIM - 1);
                    if (cells[gx, gz] == null) cells[gx, gz] = new List<int>();
                    cells[gx, gz].Add(i);
                }
            }

            public Color QueryColor(float x, float z)
            {
                int cx = Mathf.Clamp((int)((x + HALF_SPAN) / CELL_SIZE), 0, GRID_DIM - 1);
                int cz = Mathf.Clamp((int)((z + HALF_SPAN) / CELL_SIZE), 0, GRID_DIM - 1);
                float bestSq = float.MaxValue;
                int bestIdx = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx;
                    if (nx < 0 || nx >= GRID_DIM) continue;
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nz = cz + dz;
                        if (nz < 0 || nz >= GRID_DIM) continue;
                        var cell = cells[nx, nz];
                        if (cell == null) continue;
                        for (int k = 0; k < cell.Count; k++)
                        {
                            int idx = cell[k];
                            float dxx = verts[idx].x - x;
                            float dzz = verts[idx].z - z;
                            float sq = dxx * dxx + dzz * dzz;
                            if (sq < bestSq)
                            {
                                bestSq = sq;
                                bestIdx = idx;
                            }
                        }
                    }
                }

                return colors[bestIdx];
            }
        }

        [MenuItem("BattleSummoner/Populate Wind Grass On Green Patches", false, 12)]
        public static void PopulateWindGrass()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("======================================================================");
            Debug.Log("🌿 POPULATING INTERACTIVE WIND GRASS ON GREEN PATCHES OUTSIDE KINGDOM");
            Debug.Log("======================================================================");

            var grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GRASS_PREFAB_PATH);
            if (grassPrefab == null)
            {
                Debug.LogError($"[WindGrassPopulator] Grass patch prefab not found at: {GRASS_PREFAB_PATH}");
                return;
            }

            var terrainObj = GameObject.Find("Island_Overworld_Terrain");
            if (terrainObj == null)
            {
                Debug.LogError("[WindGrassPopulator] Island_Overworld_Terrain not found in active scene!");
                return;
            }

            var terrainCollider = terrainObj.GetComponent<Collider>();
            var terrainMeshFilter = terrainObj.GetComponent<MeshFilter>();
            var terrainMesh = terrainMeshFilter ? terrainMeshFilter.sharedMesh : null;

            if (terrainCollider == null || terrainMesh == null)
            {
                Debug.LogError("[WindGrassPopulator] Terrain collider or mesh missing!");
                return;
            }

            // Remove existing root cleanly
            var existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            var rootObj = new GameObject(ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(rootObj, "Populate Wind Grass");

            // Attach WindGrassInteractionDriver to Player_Hero
            AttachInteractionDriverToPlayer();

            // Build Spatial Hash for exact vertex color checks
            var verts = terrainMesh.vertices;
            var colors = terrainMesh.colors;
            var spatialHash = new TerrainSpatialHash(verts, colors);

            var greenSeedsSector1 = new List<Vector3>(); // X > 0, Z > 0 green sector
            var greenSeedsSector2 = new List<Vector3>(); // X < 0, Z < 0 green sector
            var greenSeedsFoothills = new List<Vector3>(); // R in [350, 420] outer moat berms

            for (int i = 0; i < verts.Length; i += 2)
            {
                var c = colors[i];
                // Ground truth: strictly green grass channel
                if (c.r > 0.65f && c.g < 0.15f && c.b < 0.15f && c.a < 0.30f)
                {
                    var v = verts[i];
                    float dist = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                    float ang = Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
                    if (ang < 0) ang += 360f;

                    if (dist < 350f || dist > 730f || v.y < 0.7f) continue;
                    if (IsNearRiverBed(ang, dist, 36f)) continue;

                    if (dist <= 420f)
                    {
                        greenSeedsFoothills.Add(v);
                    }
                    else if (v.x >= 0f && v.z >= 0f)
                    {
                        greenSeedsSector1.Add(v);
                    }
                    else if (v.x <= 0f && v.z <= 0f)
                    {
                        greenSeedsSector2.Add(v);
                    }
                }
            }

            Debug.Log($"📍 Identified {greenSeedsSector1.Count} seeds in Sector 1 (NE Green Terraces), {greenSeedsSector2.Count} seeds in Sector 2 (SW Green Plains), and {greenSeedsFoothills.Count} seeds along Kingdom Foothills.");

            int totalSpawned = 0;

            // 1. Sector 1 Green Terraces & Meadows (X > 0, Z > 0)
            var group1 = new GameObject("Group_Green_Terraces_NorthEast");
            group1.transform.SetParent(rootObj.transform);
            int count1 = SpawnSwathsFromSeeds(
                group1.transform,
                grassPrefab,
                terrainCollider,
                spatialHash,
                greenSeedsSector1,
                targetSwaths: 60,
                patchesPerSwathMin: 18,
                patchesPerSwathMax: 30,
                swathRadius: 10f);
            totalSpawned += count1;
            Debug.Log($"🌿 [Sector 1 NE Green Terraces] Populated {count1} interactive grass patches.");

            // 2. Sector 2 Green Rolling Plains (X < 0, Z < 0)
            var group2 = new GameObject("Group_Green_Plains_SouthWest");
            group2.transform.SetParent(rootObj.transform);
            int count2 = SpawnSwathsFromSeeds(
                group2.transform,
                grassPrefab,
                terrainCollider,
                spatialHash,
                greenSeedsSector2,
                targetSwaths: 60,
                patchesPerSwathMin: 18,
                patchesPerSwathMax: 30,
                swathRadius: 10f);
            totalSpawned += count2;
            Debug.Log($"🌾 [Sector 2 SW Green Plains] Populated {count2} interactive grass patches.");

            // 3. Kingdom Outer Green Foothills (R = 350m to 420m)
            var group3 = new GameObject("Group_Kingdom_OuterGreenBerms");
            group3.transform.SetParent(rootObj.transform);
            int count3 = SpawnSwathsFromSeeds(
                group3.transform,
                grassPrefab,
                terrainCollider,
                spatialHash,
                greenSeedsFoothills,
                targetSwaths: 25,
                patchesPerSwathMin: 14,
                patchesPerSwathMax: 22,
                swathRadius: 7.5f);
            totalSpawned += count3;
            Debug.Log($"🏰 [Kingdom Outer Berms] Populated {count3} interactive grass patches along moat green hills.");

            // Set dirty and save scene
            EditorUtility.SetDirty(rootObj);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("======================================================================");
            Debug.Log($"🎉 [WIND GRASS POPULATION COMPLETE] Spawned {totalSpawned} interactive wind grass patches strictly across green zones outside the kingdom!");
            Debug.Log("   Strictly verified: 0 Desert, 0 Volcanic, 0 River, 0 Water.");
            Debug.Log("======================================================================");
        }

        private static void AttachInteractionDriverToPlayer()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                var driver = hero.GetComponent<WindGrassInteractionDriver>();
                if (driver == null)
                {
                    driver = Undo.AddComponent<WindGrassInteractionDriver>(hero);
                    Debug.Log("⚡ Attached WindGrassInteractionDriver to Player_Hero.");
                }
                Shader.SetGlobalVector("_GlobalPlayerPos", hero.transform.position);
                Shader.SetGlobalFloat("_GlobalPlayerRadius", 2.5f);
            }
        }

        private static int SpawnSwathsFromSeeds(
            Transform parentGroup,
            GameObject prefab,
            Collider terrainCollider,
            TerrainSpatialHash spatialHash,
            List<Vector3> seedPool,
            int targetSwaths,
            int patchesPerSwathMin,
            int patchesPerSwathMax,
            float swathRadius)
        {
            if (seedPool == null || seedPool.Count == 0) return 0;

            int spawned = 0;
            var shuffledSeeds = new List<Vector3>(seedPool);
            for (int i = 0; i < shuffledSeeds.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, shuffledSeeds.Count);
                var temp = shuffledSeeds[i];
                shuffledSeeds[i] = shuffledSeeds[r];
                shuffledSeeds[r] = temp;
            }

            int swathsCreated = 0;
            int seedIndex = 0;

            while (swathsCreated < targetSwaths && seedIndex < shuffledSeeds.Count)
            {
                Vector3 seed = shuffledSeeds[seedIndex++];

                Ray downRay = new Ray(new Vector3(seed.x, 300f, seed.z), Vector3.down);
                if (!terrainCollider.Raycast(downRay, out RaycastHit centerHit, 400f)) continue;

                if (centerHit.point.y < 0.6f || centerHit.normal.y < 0.72f) continue;
                Color centerCol = spatialHash.QueryColor(centerHit.point.x, centerHit.point.z);
                // Strict green requirement
                if (centerCol.r < 0.60f || centerCol.g > 0.15f || centerCol.b > 0.15f || centerCol.a > 0.35f) continue;

                var swathObj = new GameObject($"Swath_{swathsCreated + 1:D2}");
                swathObj.transform.SetParent(parentGroup);

                int patchesInSwath = UnityEngine.Random.Range(patchesPerSwathMin, patchesPerSwathMax + 1);

                for (int p = 0; p < patchesInSwath; p++)
                {
                    Vector2 offset2D = UnityEngine.Random.insideUnitCircle * swathRadius;
                    Vector3 candidatePos = centerHit.point + new Vector3(offset2D.x, 0f, offset2D.y);

                    float candAngle = Mathf.Atan2(candidatePos.z, candidatePos.x) * Mathf.Rad2Deg;
                    if (candAngle < 0) candAngle += 360f;
                    float candDist = Mathf.Sqrt(candidatePos.x * candidatePos.x + candidatePos.z * candidatePos.z);

                    if (IsNearRiverBed(candAngle, candDist, 34f)) continue;
                    if (candDist < 350f || candDist > 735f) continue;

                    Ray patchRay = new Ray(new Vector3(candidatePos.x, 300f, candidatePos.z), Vector3.down);
                    if (!terrainCollider.Raycast(patchRay, out RaycastHit patchHit, 400f)) continue;

                    if (patchHit.point.y < 0.5f || patchHit.normal.y < 0.70f) continue;

                    // Absolute strict green validation: R must be dominant grass, G & B must be low
                    Color pCol = spatialHash.QueryColor(patchHit.point.x, patchHit.point.z);
                    if (pCol.r < 0.58f || pCol.g > 0.15f || pCol.b > 0.15f || pCol.a > 0.35f) continue;

                    GameObject patch = (GameObject)PrefabUtility.InstantiatePrefab(prefab, swathObj.transform);
                    if (patch == null) patch = UnityEngine.Object.Instantiate(prefab, swathObj.transform);

                    patch.transform.position = patchHit.point + Vector3.up * 0.02f;

                    Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, patchHit.normal);
                    Quaternion yawRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    patch.transform.rotation = Quaternion.Slerp(yawRotation, slopeRotation * yawRotation, 0.35f);

                    float scaleBase = UnityEngine.Random.Range(1.20f, 2.25f);
                    float varX = UnityEngine.Random.Range(0.90f, 1.12f);
                    float varY = UnityEngine.Random.Range(0.85f, 1.25f);
                    float varZ = UnityEngine.Random.Range(0.90f, 1.12f);
                    patch.transform.localScale = new Vector3(scaleBase * varX, scaleBase * varY, scaleBase * varZ);

                    patch.isStatic = true;
                    spawned++;
                }

                swathsCreated++;
            }

            return spawned;
        }

        private static bool IsNearRiverBed(float angleDeg, float radius, float bufferMeters)
        {
            if (radius < 260f) return false;
            for (int r = 0; r < 8; r++)
            {
                float riverAngle = r * 45f;
                float diff = Mathf.Abs(Mathf.DeltaAngle(angleDeg, riverAngle));
                float arc = radius * Mathf.Deg2Rad * diff;
                if (arc < bufferMeters) return true;
            }
            return false;
        }
    }
}

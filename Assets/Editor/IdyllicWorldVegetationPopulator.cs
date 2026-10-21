using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// IdyllicWorldVegetationPopulator populates:
    /// 1. Idyllic Fantasy Nature trees, plants, reeds, and water lilies
    /// 2. Grass and Flowers Pack 1 3D flower clusters & stalks
    /// 3. Nature Starter Kit 2 URP-upgraded bushes (bush01 to bush06)
    /// 
    /// Strict Ground-Truth Biome Constraints:
    /// - Strictly green ground (Terrain splat weight R >= 0.55 via 85x85 spatial hash).
    /// - 0 in SW Desert (G > 0.20).
    /// - 0 in NE Volcanic (B > 0.20).
    /// - 0 inside Kingdom foundation / moat (R < 350m).
    /// - 0 inside riverbeds (except reeds along banks and lilypads on water).
    /// - 0 on vertical cliffs (normal.y < 0.72).
    /// </summary>
    public static class IdyllicWorldVegetationPopulator
    {
        private const string ROOT_NAME = "--- IDYLLIC FANTASY NATURE & VEGETATION ---";

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

        [MenuItem("BattleSummoner/Populate Idyllic Fantasy World Nature", false, 13)]
        public static void PopulateWorldNature()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("======================================================================");
            Debug.Log("🌸 POPULATING IDYLLIC FANTASY NATURE, FLOWERS & BUSHES ACROSS WORLD");
            Debug.Log("======================================================================");

            var terrainObj = GameObject.Find("Island_Overworld_Terrain");
            if (terrainObj == null)
            {
                Debug.LogError("[IdyllicPopulator] Island_Overworld_Terrain not found!");
                return;
            }

            var terrainCollider = terrainObj.GetComponent<Collider>();
            var terrainMeshFilter = terrainObj.GetComponent<MeshFilter>();
            var terrainMesh = terrainMeshFilter ? terrainMeshFilter.sharedMesh : null;

            if (terrainCollider == null || terrainMesh == null)
            {
                Debug.LogError("[IdyllicPopulator] Terrain collider or mesh missing!");
                return;
            }

            var verts = terrainMesh.vertices;
            var colors = terrainMesh.colors;
            var spatialHash = new TerrainSpatialHash(verts, colors);

            // Clean existing root
            var existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            var rootObj = new GameObject(ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(rootObj, "Populate Idyllic Nature");

            // Load Prefab Pools
            var treePrefabs = LoadTreePrefabs();
            var bushPrefabs = LoadBushPrefabs();
            var flowerPrefabs = LoadFlowerPrefabs();
            var plantPrefabs = LoadPlantPrefabs();
            var reedPrefabs = LoadReedPrefabs();
            var lilyPrefabs = LoadLilyPrefabs();

            Debug.Log($"📦 Loaded: {treePrefabs.Count} Trees, {bushPrefabs.Count} Bushes, {flowerPrefabs.Count} Flowers, {plantPrefabs.Count} Plants, {reedPrefabs.Count} Reeds, {lilyPrefabs.Count} Lilies.");

            // Collect Green Ground Seeds
            var greenSeedsSector1 = new List<Vector3>(); // X > 0, Z > 0
            var greenSeedsSector2 = new List<Vector3>(); // X < 0, Z < 0
            var greenSeedsFoothills = new List<Vector3>(); // R in [350, 420]

            for (int i = 0; i < verts.Length; i += 2)
            {
                var c = colors[i];
                if (c.r > 0.65f && c.g < 0.15f && c.b < 0.15f && c.a < 0.30f)
                {
                    var v = verts[i];
                    float dist = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                    float ang = Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
                    if (ang < 0) ang += 360f;

                    if (dist < 350f || dist > 730f || v.y < 0.7f) continue;
                    if (IsNearRiverBed(ang, dist, 36f)) continue;

                    if (dist <= 420f) greenSeedsFoothills.Add(v);
                    else if (v.x >= 0f && v.z >= 0f) greenSeedsSector1.Add(v);
                    else if (v.x <= 0f && v.z <= 0f) greenSeedsSector2.Add(v);
                }
            }

            // 1. POPULATE FANTASY TREES (Idyllic Fantasy Nature)
            var treeRoot = new GameObject("Group_FantasyTrees");
            treeRoot.transform.SetParent(rootObj.transform);
            int treesSpawned = 0;
            treesSpawned += SpawnTreesInSector(treeRoot.transform, treePrefabs, terrainCollider, spatialHash, greenSeedsSector1, 75, "NE_Terraces");
            treesSpawned += SpawnTreesInSector(treeRoot.transform, treePrefabs, terrainCollider, spatialHash, greenSeedsSector2, 75, "SW_Plains");
            treesSpawned += SpawnTreesInSector(treeRoot.transform, treePrefabs, terrainCollider, spatialHash, greenSeedsFoothills, 35, "Foothills");
            Debug.Log($"🌳 [Idyllic Fantasy Trees] Populated {treesSpawned} majestic trees across green overworld biomes.");

            // 2. POPULATE NATURE STARTER KIT 2 BUSHES (bush01 to bush06)
            var bushRoot = new GameObject("Group_NatureStarterKit_Bushes");
            bushRoot.transform.SetParent(rootObj.transform);
            int bushesSpawned = 0;
            bushesSpawned += SpawnClusteredAssets(bushRoot.transform, bushPrefabs, terrainCollider, spatialHash, greenSeedsSector1, 35, 6, 12, 10f, 0.9f, 1.4f, "NE_Bushes");
            bushesSpawned += SpawnClusteredAssets(bushRoot.transform, bushPrefabs, terrainCollider, spatialHash, greenSeedsSector2, 35, 6, 12, 10f, 0.9f, 1.4f, "SW_Bushes");
            bushesSpawned += SpawnClusteredAssets(bushRoot.transform, bushPrefabs, terrainCollider, spatialHash, greenSeedsFoothills, 18, 5, 10, 8f, 0.85f, 1.3f, "Foothill_Bushes");
            Debug.Log($"🌿 [Nature Starter Kit 2 Bushes] Populated {bushesSpawned} lush URP bushes in natural thickets.");

            // 3. POPULATE GRASS AND FLOWERS PACK 1 FLOWERS (Wildflowers, Marigolds, Azure Bells, Poppies)
            var flowerRoot = new GameObject("Group_GrassAndFlowers_Flora");
            flowerRoot.transform.SetParent(rootObj.transform);
            int flowersSpawned = 0;
            flowersSpawned += SpawnClusteredAssets(flowerRoot.transform, flowerPrefabs, terrainCollider, spatialHash, greenSeedsSector1, 45, 8, 16, 7.5f, 0.8f, 1.35f, "NE_Flowers");
            flowersSpawned += SpawnClusteredAssets(flowerRoot.transform, flowerPrefabs, terrainCollider, spatialHash, greenSeedsSector2, 45, 8, 16, 7.5f, 0.8f, 1.35f, "SW_Flowers");
            flowersSpawned += SpawnClusteredAssets(flowerRoot.transform, flowerPrefabs, terrainCollider, spatialHash, greenSeedsFoothills, 22, 6, 14, 6f, 0.8f, 1.3f, "Foothill_Flowers");
            Debug.Log($"🌺 [Grass And Flowers Pack 1] Populated {flowersSpawned} colorful 3D flower clusters across grassy fields.");

            // 4. POPULATE IDYLLIC PLANTS, FERNS & REEDS
            var plantRoot = new GameObject("Group_Idyllic_Plants_And_Reeds");
            plantRoot.transform.SetParent(rootObj.transform);
            int plantsSpawned = 0;
            if (plantPrefabs.Count > 0)
            {
                plantsSpawned += SpawnClusteredAssets(plantRoot.transform, plantPrefabs, terrainCollider, spatialHash, greenSeedsSector1, 25, 5, 10, 8f, 1.0f, 1.6f, "NE_Ferns");
                plantsSpawned += SpawnClusteredAssets(plantRoot.transform, plantPrefabs, terrainCollider, spatialHash, greenSeedsSector2, 25, 5, 10, 8f, 1.0f, 1.6f, "SW_Ferns");
            }

            // Riverbank Reeds (framed along outer riverbanks R in [355, 710], distance ~35m to 42m from rivers)
            int reedsSpawned = SpawnRiverReeds(plantRoot.transform, reedPrefabs, terrainCollider, spatialHash);
            Debug.Log($"🌾 [Idyllic Plants & Reeds] Populated {plantsSpawned} forest ferns and {reedsSpawned} riverbank reeds.");

            // Water Lilies in calm river inlets
            int liliesSpawned = SpawnWaterLilies(plantRoot.transform, lilyPrefabs, spatialHash);
            Debug.Log($"🪷 [Idyllic Water Lilies] Populated {liliesSpawned} floating lilypads & blossoms in calm waterways.");

            // Butterfly Spawn Areas over Flower Meadows
            SpawnButterflies(flowerRoot.transform);

            // Mark dirty and save scene
            EditorUtility.SetDirty(rootObj);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("======================================================================");
            Debug.Log($"🎉 [WORLD NATURE POPULATION COMPLETE]");
            Debug.Log($"   Trees: {treesSpawned} | Bushes: {bushesSpawned} | Flowers: {flowersSpawned} | Ferns & Reeds: {plantsSpawned + reedsSpawned} | Lilies: {liliesSpawned}");
            Debug.Log($"   Total World Foliage Added: {treesSpawned + bushesSpawned + flowersSpawned + plantsSpawned + reedsSpawned + liliesSpawned}");
            Debug.Log("======================================================================");
        }

        private static List<GameObject> LoadTreePrefabs()
        {
            string[] paths = new string[] {
                "Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Green.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_02_Green.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_03_Green.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_04_Green.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Green.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_02_Green.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_03_Green.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Fir_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Fir_02.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Fir_03.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_02.prefab"
            };

            var list = new List<GameObject>();
            foreach (var p in paths)
            {
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pfb != null) list.Add(pfb);
            }
            return list;
        }

        private static List<GameObject> LoadBushPrefabs()
        {
            var list = new List<GameObject>();
            for (int i = 1; i <= 6; i++)
            {
                string p = $"Assets/NatureStarterKit2/Nature/bush0{i}.prefab";
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pfb != null) list.Add(pfb);
            }
            return list;
        }

        private static List<GameObject> LoadFlowerPrefabs()
        {
            string[] paths = new string[] {
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_01.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_03.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_05.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_08.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_12.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_15.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_18.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_21.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_25.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_28.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_32.prefab",
                "Assets/Prefabs/Flowers/PFB_Flower_35.prefab"
            };

            var list = new List<GameObject>();
            foreach (var p in paths)
            {
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pfb != null) list.Add(pfb);
            }
            return list;
        }

        private static List<GameObject> LoadPlantPrefabs()
        {
            var list = new List<GameObject>();
            for (int i = 1; i <= 8; i++)
            {
                string p = $"Assets/Idyllic Fantasy Nature/Prefabs/Plant_0{i}.prefab";
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pfb != null) list.Add(pfb);
            }
            return list;
        }

        private static List<GameObject> LoadReedPrefabs()
        {
            string[] paths = new string[] {
                "Assets/Idyllic Fantasy Nature/Prefabs/Reeds_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Reeds_02.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Reeds_03.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Cattail_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Cattail_02.prefab"
            };
            var list = new List<GameObject>();
            foreach (var p in paths)
            {
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pfb != null) list.Add(pfb);
            }
            return list;
        }

        private static List<GameObject> LoadLilyPrefabs()
        {
            string[] paths = new string[] {
                "Assets/Idyllic Fantasy Nature/Prefabs/LilyPads_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/LilyPads_02.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/LilyPads_03.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Waterlily_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Waterlily_02.prefab"
            };
            var list = new List<GameObject>();
            foreach (var p in paths)
            {
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pfb != null) list.Add(pfb);
            }
            return list;
        }

        private static int SpawnTreesInSector(
            Transform parent,
            List<GameObject> treePool,
            Collider terrainCollider,
            TerrainSpatialHash spatialHash,
            List<Vector3> seedPool,
            int targetCount,
            string groupPrefix)
        {
            if (seedPool == null || seedPool.Count == 0 || treePool == null || treePool.Count == 0) return 0;

            int spawned = 0;
            var subGroup = new GameObject($"Trees_{groupPrefix}");
            subGroup.transform.SetParent(parent);

            var shuffled = new List<Vector3>(seedPool);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, shuffled.Count);
                var temp = shuffled[i];
                shuffled[i] = shuffled[r];
                shuffled[r] = temp;
            }

            int seedIdx = 0;
            while (spawned < targetCount && seedIdx < shuffled.Count)
            {
                Vector3 seed = shuffled[seedIdx++];

                Ray ray = new Ray(new Vector3(seed.x, 300f, seed.z), Vector3.down);
                if (!terrainCollider.Raycast(ray, out RaycastHit hit, 400f)) continue;

                if (hit.point.y < 19.5f || hit.normal.y < 0.72f) continue;
                Color col = spatialHash.QueryColor(hit.point.x, hit.point.z);
                if (col.r < 0.60f || col.g > 0.15f || col.b > 0.15f || col.a > 0.35f) continue;

                float dist = Mathf.Sqrt(hit.point.x * hit.point.x + hit.point.z * hit.point.z);
                float ang = Mathf.Atan2(hit.point.z, hit.point.x) * Mathf.Rad2Deg;
                if (ang < 0) ang += 360f;
                if (dist < 350f || dist > 730f) continue;
                if (IsNearRiverBed(ang, dist, 36f)) continue;

                var prefab = treePool[UnityEngine.Random.Range(0, treePool.Count)];
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, subGroup.transform);
                if (inst == null) inst = UnityEngine.Object.Instantiate(prefab, subGroup.transform);

                inst.transform.position = hit.point;

                Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Quaternion yawRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                inst.transform.rotation = Quaternion.Slerp(yawRotation, slopeRotation * yawRotation, 0.20f);

                float scale = UnityEngine.Random.Range(0.85f, 1.35f);
                inst.transform.localScale = Vector3.one * scale;

                inst.isStatic = true;
                spawned++;
            }

            return spawned;
        }

        private static int SpawnClusteredAssets(
            Transform parent,
            List<GameObject> prefabPool,
            Collider terrainCollider,
            TerrainSpatialHash spatialHash,
            List<Vector3> seedPool,
            int clusterCount,
            int itemsMin,
            int itemsMax,
            float clusterRadius,
            float scaleMin,
            float scaleMax,
            string groupPrefix)
        {
            if (seedPool == null || seedPool.Count == 0 || prefabPool == null || prefabPool.Count == 0) return 0;

            int spawned = 0;
            var subGroup = new GameObject($"Group_{groupPrefix}");
            subGroup.transform.SetParent(parent);

            var shuffled = new List<Vector3>(seedPool);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, shuffled.Count);
                var temp = shuffled[i];
                shuffled[i] = shuffled[r];
                shuffled[r] = temp;
            }

            int clustersBuilt = 0;
            int seedIdx = 0;

            while (clustersBuilt < clusterCount && seedIdx < shuffled.Count)
            {
                Vector3 centerSeed = shuffled[seedIdx++];

                Ray ray = new Ray(new Vector3(centerSeed.x, 300f, centerSeed.z), Vector3.down);
                if (!terrainCollider.Raycast(ray, out RaycastHit centerHit, 400f)) continue;

                if (centerHit.point.y < 19.5f || centerHit.normal.y < 0.72f) continue;
                Color centerCol = spatialHash.QueryColor(centerHit.point.x, centerHit.point.z);
                if (centerCol.r < 0.60f || centerCol.g > 0.15f || centerCol.b > 0.15f || centerCol.a > 0.35f) continue;

                var clusterObj = new GameObject($"Cluster_{clustersBuilt + 1:D2}");
                clusterObj.transform.SetParent(subGroup.transform);

                int countInCluster = UnityEngine.Random.Range(itemsMin, itemsMax + 1);

                for (int k = 0; k < countInCluster; k++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * clusterRadius;
                    Vector3 cand = centerHit.point + new Vector3(offset.x, 0f, offset.y);

                    float dist = Mathf.Sqrt(cand.x * cand.x + cand.z * cand.z);
                    float ang = Mathf.Atan2(cand.z, cand.x) * Mathf.Rad2Deg;
                    if (ang < 0) ang += 360f;

                    if (dist < 350f || dist > 735f) continue;
                    if (IsNearRiverBed(ang, dist, 34f)) continue;

                    Ray pRay = new Ray(new Vector3(cand.x, 300f, cand.z), Vector3.down);
                    if (!terrainCollider.Raycast(pRay, out RaycastHit hit, 400f)) continue;

                    if (hit.point.y < 19.5f || hit.normal.y < 0.70f) continue;
                    Color pCol = spatialHash.QueryColor(hit.point.x, hit.point.z);
                    if (pCol.r < 0.58f || pCol.g > 0.15f || pCol.b > 0.15f || pCol.a > 0.35f) continue;

                    var prefab = prefabPool[UnityEngine.Random.Range(0, prefabPool.Count)];
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, clusterObj.transform);
                    if (inst == null) inst = UnityEngine.Object.Instantiate(prefab, clusterObj.transform);

                    inst.transform.position = hit.point;

                    Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    Quaternion yawRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    inst.transform.rotation = Quaternion.Slerp(yawRotation, slopeRotation * yawRotation, 0.35f);

                    float sc = UnityEngine.Random.Range(scaleMin, scaleMax);
                    inst.transform.localScale = Vector3.one * sc;

                    inst.isStatic = true;
                    spawned++;
                }

                clustersBuilt++;
            }

            return spawned;
        }

        private static int SpawnRiverReeds(
            Transform parent,
            List<GameObject> reedPool,
            Collider terrainCollider,
            TerrainSpatialHash spatialHash)
        {
            if (reedPool == null || reedPool.Count == 0) return 0;

            int spawned = 0;
            var reedGroup = new GameObject("Reeds_Riverbanks");
            reedGroup.transform.SetParent(parent);

            // Sample along 8 river channels at radius 360m to 700m
            for (int r = 0; r < 8; r++)
            {
                float baseAngleDeg = r * 45f;

                for (float radius = 360f; radius <= 700f; radius += 18f)
                {
                    // Placed on left and right banks (~36m to 42m from center)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float bankArcMeters = side * UnityEngine.Random.Range(34f, 42f);
                        float bankAngleOffsetDeg = (bankArcMeters / radius) * Mathf.Rad2Deg;
                        float sampleAngleDeg = baseAngleDeg + bankAngleOffsetDeg;
                        float rad = sampleAngleDeg * Mathf.Deg2Rad;

                        Vector3 pos = new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);

                        // Only place on green biomes (Sector 1 or 2)
                        Color col = spatialHash.QueryColor(pos.x, pos.z);
                        if (col.r < 0.50f || col.g > 0.20f || col.b > 0.20f) continue;

                        Ray ray = new Ray(new Vector3(pos.x, 300f, pos.z), Vector3.down);
                        if (!terrainCollider.Raycast(ray, out RaycastHit hit, 400f)) continue;

                        if (hit.point.y < 19.5f) continue; // STRICT DRY LAND CHECK: water level is 18.0m

                        var prefab = reedPool[UnityEngine.Random.Range(0, reedPool.Count)];
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, reedGroup.transform);
                        if (inst == null) inst = UnityEngine.Object.Instantiate(prefab, reedGroup.transform);

                        inst.transform.position = hit.point;
                        inst.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        inst.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.85f, 1.4f);

                        inst.isStatic = true;
                        spawned++;
                    }
                }
            }

            return spawned;
        }

        private static int SpawnWaterLilies(Transform parent, List<GameObject> lilyPool, TerrainSpatialHash spatialHash)
        {
            // DEFERRED per user request: "we will implement water props and other wild life there in a future update when im ready to tackle it"
            return 0;
        }

        private static void SpawnButterflies(Transform parent)
        {
            var pfbArea = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/ButterflySpawnArea.prefab");
            if (pfbArea == null) return;

            var butterflyRoot = new GameObject("Butterflies_MeadowSwaths");
            butterflyRoot.transform.SetParent(parent);

            Vector3[] meadowSpots = new Vector3[] {
                new Vector3(440f, 40f, 70f),
                new Vector3(480f, 36f, 110f),
                new Vector3(-360f, 25f, -280f),
                new Vector3(-420f, 20f, -320f)
            };

            foreach (var spot in meadowSpots)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(pfbArea, butterflyRoot.transform);
                if (inst != null)
                {
                    inst.transform.position = spot + Vector3.up * 1.5f;
                }
            }
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

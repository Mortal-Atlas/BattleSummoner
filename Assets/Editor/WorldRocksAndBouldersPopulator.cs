using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    public static class WorldRocksAndBouldersPopulator
    {
        private const string ROOT_NAME = "--- WORLD ROCKS AND BOULDERS ---";

        [MenuItem("BattleSummoner/Populate World Rocks and Boulders", false, 11)]
        public static void PopulateRocksAndBoulders()
        {
            Debug.Log("======================================================================");
            Debug.Log("🪨 POPULATING ROCKS AND BOULDERS ACROSS OVERWORLD MAP BIOMES");
            Debug.Log("======================================================================");

            // Use only the cleanly modeled stylized rock prefabs (3m to 5m natural scale)
            var pfbBoulderLarge = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StylizedRocks/PFB_Rock_BoulderLarge.prefab");
            var pfbCliffSpire = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StylizedRocks/PFB_Rock_CliffSpire.prefab");
            var pfbRiverStone = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StylizedRocks/PFB_Rock_RiverStoneMedium.prefab");

            var allPrefabs = new List<GameObject> { pfbBoulderLarge, pfbCliffSpire, pfbRiverStone };
            allPrefabs.RemoveAll(p => p == null);

            if (allPrefabs.Count == 0)
            {
                Debug.LogError("[Rock Populator] Stylized rock prefabs not found!");
                return;
            }

            // Find or create Root
            var existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            var rootObj = new GameObject(ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(rootObj, "Populate World Rocks");

            int totalSpawned = 0;

            // 1. SW Desert Arid Boulders & Sandstone Formations (X < -30, Z < -30)
            var desertGroup = new GameObject("Group_Desert_AridBoulders");
            desertGroup.transform.SetParent(rootObj.transform);
            totalSpawned += SpawnRockBiomeClusters(
                desertGroup.transform,
                allPrefabs,
                clusterCount: 22,
                rocksPerCluster: 6,
                quadrantAngleMin: 185f,
                quadrantAngleMax: 265f,
                minRadius: 330f,
                maxRadius: 740f,
                baseScaleMin: 0.75f,
                baseScaleMax: 1.65f);

            // 2. NE Volcanic Basalt Crags & Obsidian Spires (X > 50, Z > 50)
            var volcGroup = new GameObject("Group_Volcanic_BasaltCrags");
            volcGroup.transform.SetParent(rootObj.transform);
            totalSpawned += SpawnRockBiomeClusters(
                volcGroup.transform,
                allPrefabs,
                clusterCount: 18,
                rocksPerCluster: 5,
                quadrantAngleMin: 10f,
                quadrantAngleMax: 80f,
                minRadius: 350f,
                maxRadius: 720f,
                baseScaleMin: 0.85f,
                baseScaleMax: 1.85f);

            // 3. NW Mistwood Ancient Lichen Boulders (X < -30, Z > 50)
            var mistwoodGroup = new GameObject("Group_Mistwood_AncientMonoliths");
            mistwoodGroup.transform.SetParent(rootObj.transform);
            totalSpawned += SpawnRockBiomeClusters(
                mistwoodGroup.transform,
                allPrefabs,
                clusterCount: 16,
                rocksPerCluster: 5,
                quadrantAngleMin: 95f,
                quadrantAngleMax: 175f,
                minRadius: 360f,
                maxRadius: 720f,
                baseScaleMin: 0.70f,
                baseScaleMax: 1.50f);

            // 4. SE Meadows Field Stones & Rocky Knolls (X > 50, Z < -30)
            var meadowsGroup = new GameObject("Group_Meadows_LichenBoulders");
            meadowsGroup.transform.SetParent(rootObj.transform);
            totalSpawned += SpawnRockBiomeClusters(
                meadowsGroup.transform,
                allPrefabs,
                clusterCount: 16,
                rocksPerCluster: 4,
                quadrantAngleMin: 275f,
                quadrantAngleMax: 355f,
                minRadius: 360f,
                maxRadius: 710f,
                baseScaleMin: 0.65f,
                baseScaleMax: 1.40f);

            // 5. Coastal Breakwater Sea Stacks (Outer perimeter R = 740m to 820m)
            var coastalGroup = new GameObject("Group_Coastal_SeaStacks");
            coastalGroup.transform.SetParent(rootObj.transform);
            totalSpawned += SpawnRockBiomeClusters(
                coastalGroup.transform,
                allPrefabs,
                clusterCount: 20,
                rocksPerCluster: 4,
                quadrantAngleMin: 0f,
                quadrantAngleMax: 360f,
                minRadius: 750f,
                maxRadius: 820f,
                baseScaleMin: 0.90f,
                baseScaleMax: 2.10f);

            // 6. Kingdom Hillside Terraces & Berms (R = 210m to 285m, outside town wall / inside moat)
            var kingdomFoothillGroup = new GameObject("Group_Kingdom_FoothillRocks");
            kingdomFoothillGroup.transform.SetParent(rootObj.transform);
            totalSpawned += SpawnRockBiomeClusters(
                kingdomFoothillGroup.transform,
                allPrefabs,
                clusterCount: 12,
                rocksPerCluster: 3,
                quadrantAngleMin: 0f,
                quadrantAngleMax: 360f,
                minRadius: 215f,
                maxRadius: 280f,
                baseScaleMin: 0.55f,
                baseScaleMax: 1.15f);

            EditorUtility.SetDirty(rootObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"🎉 [ROCK POPULATION COMPLETE] Spawned {totalSpawned} natural rocks, spires, and boulders across all island biomes!");
        }

        private static int SpawnRockBiomeClusters(
            Transform parentGroup,
            List<GameObject> prefabPool,
            int clusterCount,
            int rocksPerCluster,
            float quadrantAngleMin,
            float quadrantAngleMax,
            float minRadius,
            float maxRadius,
            float baseScaleMin,
            float baseScaleMax)
        {
            int spawned = 0;
            var terrain = Terrain.activeTerrain;
            int groundMask = LayerMask.GetMask("Default", "Terrain");
            if (groundMask == 0) groundMask = ~0;

            for (int c = 0; c < clusterCount; c++)
            {
                float clusterAngle = UnityEngine.Random.Range(quadrantAngleMin, quadrantAngleMax) * Mathf.Deg2Rad;
                float clusterDist = UnityEngine.Random.Range(minRadius, maxRadius);

                // Avoid placing directly inside river beds
                if (IsNearRiverBed(clusterAngle * Mathf.Rad2Deg, clusterDist, 35f)) continue;

                Vector3 clusterCenter = new Vector3(
                    Mathf.Cos(clusterAngle) * clusterDist,
                    0f,
                    Mathf.Sin(clusterAngle) * clusterDist);

                var clusterGo = new GameObject($"Cluster_{c + 1}");
                clusterGo.transform.SetParent(parentGroup);

                for (int r = 0; r < rocksPerCluster; r++)
                {
                    Vector2 localOffset = r == 0 ? Vector2.zero : UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(2.5f, 9.0f);
                    Vector3 candidatePos = clusterCenter + new Vector3(localOffset.x, 250f, localOffset.y);

                    float groundY = 0f;
                    if (Physics.Raycast(candidatePos, Vector3.down, out RaycastHit hit, 500f, groundMask))
                    {
                        groundY = hit.point.y;
                    }
                    else if (terrain != null)
                    {
                        groundY = terrain.SampleHeight(candidatePos);
                    }

                    // Avoid deep underwater placement unless coastal
                    if (groundY < -2.5f && minRadius < 700f) continue;

                    Vector3 spawnPos = new Vector3(candidatePos.x, groundY - 0.20f, candidatePos.z);

                    GameObject prefab = prefabPool[UnityEngine.Random.Range(0, prefabPool.Count)];
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, clusterGo.transform);
                    if (instance == null) instance = UnityEngine.Object.Instantiate(prefab, clusterGo.transform);

                    instance.transform.position = spawnPos;

                    // Organic random rotation
                    float yaw = UnityEngine.Random.Range(0f, 360f);
                    float pitch = UnityEngine.Random.Range(-8f, 8f);
                    float roll = UnityEngine.Random.Range(-8f, 8f);
                    instance.transform.rotation = Quaternion.Euler(pitch, yaw, roll);

                    // Organic scaling: anchor boulder is larger, flankers are smaller
                    float scaleFactor = (r == 0)
                        ? UnityEngine.Random.Range(baseScaleMax * 0.85f, baseScaleMax)
                        : UnityEngine.Random.Range(baseScaleMin, baseScaleMax * 0.65f);

                    float nonUniformX = UnityEngine.Random.Range(0.88f, 1.15f);
                    float nonUniformY = UnityEngine.Random.Range(0.85f, 1.20f);
                    float nonUniformZ = UnityEngine.Random.Range(0.88f, 1.15f);

                    instance.transform.localScale = new Vector3(
                        scaleFactor * nonUniformX,
                        scaleFactor * nonUniformY,
                        scaleFactor * nonUniformZ);

                    spawned++;
                }
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

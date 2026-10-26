using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Procedurally populates protruding rocks, boulders, floating lily pads,
    /// cattails, reeds, and shoreline water plants across all water bodies:
    /// 1. The Concentric Capital Moat & River (R = 440m to 565m, 360 degrees).
    /// 2. The Western Dunes River, Estuary & Bay (X = -2150..-850, Z = -1150..350).
    /// 3. The Eastern River Delta & Estuary (X = 600..1650, Z = -400..600).
    /// 4. The Northern River & Inlet (X = -600..600, Z = 600..1500).
    /// 5. The SE Coastal Bay & Offshore Islands (X = 1050..1850, Z = -1350..-800).
    /// </summary>
    public static class AquaticFloraAndWaterRockPopulator
    {
        private const string ROOT_CONTAINER_NAME = "--- AQUATIC FLORA & PROTRUDING WATER ROCKS ---";
        private const float WATER_SURFACE_Y = 18.00f;
        private const float LILYPAD_SURFACE_Y = 18.08f; // Tuned so pads float crisp and dry above wave displacement

        // Prefab paths
        private static readonly string[] LILYPAD_PREFABS = {
            "Assets/Idyllic Fantasy Nature/Prefabs/LilyPads_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/LilyPads_02.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/LilyPads_03.prefab"
        };

        private static readonly string[] CATTAIL_PREFABS = {
            "Assets/Idyllic Fantasy Nature/Prefabs/Cattail_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Cattail_02.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Cattail_03.prefab"
        };

        private static readonly string[] WATER_PLANT_PREFABS = {
            "Assets/Idyllic Fantasy Nature/Prefabs/Plant_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Plant_02.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Plant_03.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Plant_04.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Flower_Blue_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Flower_Purple.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/FlowerMeadow_BluePurple.prefab"
        };

        private static readonly string[] BOULDER_PREFABS = {
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_02.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_03.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Medium_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Medium_02.prefab",
            "Assets/Prefabs/StylizedRocks/PFB_Rock_BoulderLarge.prefab",
            "Assets/Prefabs/StylizedRocks/PFB_Rock_RiverStoneMedium.prefab"
        };

        private static readonly string[] SMALL_RIVER_STONES = {
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Small_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Small_02.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Small_03.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Stones_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Stones_02.prefab",
            "Assets/Prefabs/StylizedRocks/PFB_Rock_RiverStoneMedium.prefab"
        };

        private static readonly string[] SEASTACK_PREFABS = {
            "Assets/Idyllic Fantasy Nature/Prefabs/Cliff_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Cliff_02.prefab",
            "Assets/Prefabs/StylizedRocks/PFB_Rock_CliffSpire.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_01.prefab",
            "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_03.prefab"
        };

        [MenuItem("BattleSummoner/World Activities/Populate Aquatic Flora & Protruding Rocks", false, 30)]
        public static string PopulateAllWaterBodies()
        {
            var continent = GameObject.Find("CircularContinent_MacroLevel1");
            var mc = continent != null ? continent.GetComponentInChildren<MeshCollider>() : null;
            if (mc == null)
            {
                Debug.LogError("[Aquatic Populator] MeshCollider missing on CircularContinent_MacroLevel1!");
                return "Failed: MeshCollider missing";
            }

            // Remove previous container if present
            var oldRoot = GameObject.Find(ROOT_CONTAINER_NAME);
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            var root = new GameObject(ROOT_CONTAINER_NAME);
            Undo.RegisterCreatedObjectUndo(root, "Populate Aquatic Flora & Rocks");

            var moatGroup = new GameObject("Moat_Water_Features");
            moatGroup.transform.SetParent(root.transform, false);

            var westGroup = new GameObject("Western_Dunes_River_And_Bay_Features");
            westGroup.transform.SetParent(root.transform, false);

            var eastGroup = new GameObject("Eastern_River_And_Estuary_Features");
            eastGroup.transform.SetParent(root.transform, false);

            var northGroup = new GameObject("Northern_River_And_Inlet_Features");
            northGroup.transform.SetParent(root.transform, false);

            var coastGroup = new GameObject("Coastal_SeaStacks_And_Shoals");
            coastGroup.transform.SetParent(root.transform, false);

            int rocksSpawned = 0;
            int lilypadsSpawned = 0;
            int cattailsSpawned = 0;
            int plantsSpawned = 0;

            var rng = new System.Random(428395);

            // -----------------------------------------------------------------
            // 1. CAPITAL MOAT & RIVER RING (360 degrees around R = 440m to 565m)
            // Moat is an engineered fortress canal: zero boulders/cliffs, clean water
            // with subtle lily pads and outer shoreline reeds.
            // -----------------------------------------------------------------
            for (int deg = 0; deg < 360; deg += 2)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                // Sample 12 radial rings across the entire moat width
                for (float r = 440f; r <= 565f; r += 10f)
                {
                    float jitterX = (float)(rng.NextDouble() * 5.0 - 2.5);
                    float jitterZ = (float)(rng.NextDouble() * 5.0 - 2.5);
                    Vector3 testPos = new Vector3(dir.x * r + jitterX, 300f, dir.y * r + jitterZ);

                    Ray ray = new Ray(testPos, Vector3.down);
                    if (mc.Raycast(ray, out RaycastHit hit, 400f))
                    {
                        SpawnFeaturesAtHit(hit, moatGroup.transform, rng, ref rocksSpawned, ref lilypadsSpawned, ref cattailsSpawned, ref plantsSpawned,
                            boulderChance: 0.0, lilypadChance: 0.25, cattailChance: 0.35, plantChance: 0.45, isMoat: true, radius: r);
                    }
                }
            }

            // -----------------------------------------------------------------
            // 2. WESTERN DUNES RIVER, ESTUARY & BAY (X = -2150..-850, Z = -1150..350)
            // Sparse river stones (boulderChance: 0.015) for natural, open flowing water
            // -----------------------------------------------------------------
            for (float wx = -2150f; wx <= -850f; wx += 24f)
            {
                for (float wz = -1150f; wz <= 350f; wz += 24f)
                {
                    float jx = (float)(rng.NextDouble() * 10.0 - 5.0);
                    float jz = (float)(rng.NextDouble() * 10.0 - 5.0);
                    Vector3 testPos = new Vector3(wx + jx, 300f, wz + jz);

                    Ray ray = new Ray(testPos, Vector3.down);
                    if (mc.Raycast(ray, out RaycastHit hit, 400f))
                    {
                        SpawnFeaturesAtHit(hit, westGroup.transform, rng, ref rocksSpawned, ref lilypadsSpawned, ref cattailsSpawned, ref plantsSpawned,
                            boulderChance: 0.015, lilypadChance: 0.28, cattailChance: 0.35, plantChance: 0.40, isMoat: false);
                    }
                }
            }

            // -----------------------------------------------------------------
            // 3. EASTERN RIVER & ESTUARY DELTA (X = 600..1650, Z = -400..600)
            // Sparse low riverbed stones (boulderChance: 0.012)
            // -----------------------------------------------------------------
            for (float ex = 600f; ex <= 1650f; ex += 25f)
            {
                for (float ez = -400f; ez <= 600f; ez += 25f)
                {
                    float jx = (float)(rng.NextDouble() * 10.0 - 5.0);
                    float jz = (float)(rng.NextDouble() * 10.0 - 5.0);
                    Vector3 testPos = new Vector3(ex + jx, 300f, ez + jz);

                    Ray ray = new Ray(testPos, Vector3.down);
                    if (mc.Raycast(ray, out RaycastHit hit, 400f))
                    {
                        SpawnFeaturesAtHit(hit, eastGroup.transform, rng, ref rocksSpawned, ref lilypadsSpawned, ref cattailsSpawned, ref plantsSpawned,
                            boulderChance: 0.012, lilypadChance: 0.25, cattailChance: 0.30, plantChance: 0.38, isMoat: false);
                    }
                }
            }

            // -----------------------------------------------------------------
            // 4. NORTHERN RIVER & INLET (X = -600..600, Z = 600..1500)
            // Sparse river stones (boulderChance: 0.012)
            // -----------------------------------------------------------------
            for (float nx = -600f; nx <= 600f; nx += 25f)
            {
                for (float nz = 600f; nz <= 1500f; nz += 25f)
                {
                    float jx = (float)(rng.NextDouble() * 10.0 - 5.0);
                    float jz = (float)(rng.NextDouble() * 10.0 - 5.0);
                    Vector3 testPos = new Vector3(nx + jx, 300f, nz + jz);

                    Ray ray = new Ray(testPos, Vector3.down);
                    if (mc.Raycast(ray, out RaycastHit hit, 400f))
                    {
                        SpawnFeaturesAtHit(hit, northGroup.transform, rng, ref rocksSpawned, ref lilypadsSpawned, ref cattailsSpawned, ref plantsSpawned,
                            boulderChance: 0.012, lilypadChance: 0.25, cattailChance: 0.30, plantChance: 0.38, isMoat: false);
                    }
                }
            }

            // -----------------------------------------------------------------
            // 5. SE COASTAL BAY, SEA STACKS & HARBOR (X = 1050..1850, Z = -1350..-800)
            // Sparse landmark sea stacks (~6-10 iconic spires across the bay)
            // -----------------------------------------------------------------
            for (float cx = 1050f; cx <= 1850f; cx += 80f)
            {
                for (float cz = -1350f; cz <= -800f; cz += 80f)
                {
                    float jx = (float)(rng.NextDouble() * 25.0 - 12.5);
                    float jz = (float)(rng.NextDouble() * 25.0 - 12.5);
                    Vector3 coastPos = new Vector3(cx + jx, 18.0f, cz + jz);

                    // Avoid spawning directly inside village house coordinates
                    if (Vector2.Distance(new Vector2(coastPos.x, coastPos.z), new Vector2(1180f, -980f)) < 35f) continue;
                    if (Vector2.Distance(new Vector2(coastPos.x, coastPos.z), new Vector2(1680f, -1180f)) < 35f) continue;

                    // A. PROTRUDING COASTAL SEA STACKS & GIANT REEF BOULDERS (Only 6-8 landmark spires)
                    if (rng.NextDouble() < 0.12)
                    {
                        var stackPath = SEASTACK_PREFABS[rng.Next(SEASTACK_PREFABS.Length)];
                        var stackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stackPath);
                        if (stackPrefab != null)
                        {
                            float scale = 1.6f + (float)rng.NextDouble() * 2.8f;
                            float protrudeAboveWater = 3.5f + (float)rng.NextDouble() * 9.5f;
                            float stackY = WATER_SURFACE_Y + protrudeAboveWater - (10.0f * scale * 0.5f);

                            var stack = (GameObject)PrefabUtility.InstantiatePrefab(stackPrefab, coastGroup.transform);
                            stack.transform.position = new Vector3(coastPos.x, stackY, coastPos.z);
                            stack.transform.rotation = Quaternion.Euler((float)(rng.NextDouble() * 12f - 6f), (float)(rng.NextDouble() * 360f), (float)(rng.NextDouble() * 12f - 6f));
                            stack.transform.localScale = Vector3.one * scale;
                            stack.isStatic = true;
                            rocksSpawned++;
                        }
                    }

                    // B. COASTAL WATER LILY & KELP PATCHES (calm ocean inlets)
                    if (rng.NextDouble() < 0.20)
                    {
                        var lpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LILYPAD_PREFABS[rng.Next(LILYPAD_PREFABS.Length)]);
                        if (lpPrefab != null)
                        {
                            var lp = (GameObject)PrefabUtility.InstantiatePrefab(lpPrefab, coastGroup.transform);
                            lp.transform.position = new Vector3(coastPos.x, LILYPAD_SURFACE_Y, coastPos.z);
                            lp.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
                            lp.transform.localScale = Vector3.one * (1.1f + (float)rng.NextDouble() * 0.5f);
                            lp.isStatic = true;
                            lilypadsSpawned++;
                        }
                    }
                }
            }

            // Mark scene dirty and save
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            string report = $"[Aquatic Populator] Successfully populated {ROOT_CONTAINER_NAME}!\n" +
                $"• Protruding Rocks & Boulders: {rocksSpawned}\n" +
                $"• Floating Lily Pad Clusters: {lilypadsSpawned}\n" +
                $"• Cattail & Reed Groups: {cattailsSpawned}\n" +
                $"• Shoreline Water Flowers & Plants: {plantsSpawned}\n" +
                $"Total Water Features: {rocksSpawned + lilypadsSpawned + cattailsSpawned + plantsSpawned}";

            Debug.Log(report);
            return report;
        }

        private static void SpawnFeaturesAtHit(
            RaycastHit hit,
            Transform parent,
            System.Random rng,
            ref int rocksSpawned,
            ref int lilypadsSpawned,
            ref int cattailsSpawned,
            ref int plantsSpawned,
            double boulderChance,
            double lilypadChance,
            double cattailChance,
            double plantChance,
            bool isMoat = false,
            float radius = 0f)
        {
            float bedY = hit.point.y;

            // -------------------------------------------------------------
            // A. IN THE WATER (bedY < 17.8m)
            // -------------------------------------------------------------
            if (bedY < 17.8f)
            {
                // 1. SHALLOW TO MEDIUM WATER: SPARSE NATURAL RIVER STONES
                if (bedY >= 5.0f)
                {
                    // Strict moat check and sparse river stone check
                    if (!isMoat && boulderChance > 0.0 && rng.NextDouble() < boulderChance)
                    {
                        // Use low river stones and subtle boulders
                        var stonePool = (rng.NextDouble() < 0.6) ? SMALL_RIVER_STONES : BOULDER_PREFABS;
                        var mainPath = stonePool[rng.Next(stonePool.Length)];
                        var mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainPath);
                        if (mainPrefab != null)
                        {
                            float scaleFactor = 0.8f + (float)rng.NextDouble() * 0.9f;
                            float protrudeAboveWater = 0.25f + (float)rng.NextDouble() * 0.7f;
                            float meshHeight = 2.0f * scaleFactor;
                            float targetY = WATER_SURFACE_Y + protrudeAboveWater - (meshHeight * 0.55f);
                            targetY = Mathf.Max(targetY, bedY + (meshHeight * 0.15f));

                            var mainInst = (GameObject)PrefabUtility.InstantiatePrefab(mainPrefab, parent);
                            mainInst.transform.position = new Vector3(hit.point.x, targetY, hit.point.z);
                            mainInst.transform.rotation = Quaternion.Euler(
                                (float)(rng.NextDouble() * 12f - 6f),
                                (float)(rng.NextDouble() * 360f),
                                (float)(rng.NextDouble() * 12f - 6f));
                            mainInst.transform.localScale = Vector3.one * scaleFactor;
                            mainInst.isStatic = true;
                            rocksSpawned++;
                        }
                    }

                    // 2. FLOATING LILY PADS (clusters floating gracefully on water surface)
                    if (rng.NextDouble() < lilypadChance)
                    {
                        int clusterCount = rng.Next(3, 7);
                        for (int c = 0; c < clusterCount; c++)
                        {
                            var lpPath = LILYPAD_PREFABS[rng.Next(LILYPAD_PREFABS.Length)];
                            var lpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lpPath);
                            if (lpPrefab != null)
                            {
                                float offsetX = (float)(rng.NextDouble() * 7.0 - 3.5);
                                float offsetZ = (float)(rng.NextDouble() * 7.0 - 3.5);
                                var lp = (GameObject)PrefabUtility.InstantiatePrefab(lpPrefab, parent);
                                lp.transform.position = new Vector3(hit.point.x + offsetX, LILYPAD_SURFACE_Y, hit.point.z + offsetZ);
                                lp.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
                                float lpScale = 0.9f + (float)(rng.NextDouble() * 0.45f);
                                lp.transform.localScale = Vector3.one * lpScale;
                                lp.isStatic = true;
                                lilypadsSpawned++;
                            }
                        }
                    }

                    // 3. CATTAILS & REEDS (in shallow water margins)
                    bool canSpawnCattails = isMoat ? (radius > 535f || radius < 465f) : (bedY >= 14.5f);
                    if (canSpawnCattails && rng.NextDouble() < cattailChance)
                    {
                        int reedCount = rng.Next(4, 9);
                        for (int rc = 0; rc < reedCount; rc++)
                        {
                            var ctPath = CATTAIL_PREFABS[rng.Next(CATTAIL_PREFABS.Length)];
                            var ctPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ctPath);
                            if (ctPrefab != null)
                            {
                                float rox = (float)(rng.NextDouble() * 4.0 - 2.0);
                                float roz = (float)(rng.NextDouble() * 4.0 - 2.0);
                                var ct = (GameObject)PrefabUtility.InstantiatePrefab(ctPrefab, parent);
                                ct.transform.position = new Vector3(hit.point.x + rox, WATER_SURFACE_Y - 0.4f, hit.point.z + roz);
                                ct.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
                                float ctScale = 1.0f + (float)(rng.NextDouble() * 0.4f);
                                ct.transform.localScale = Vector3.one * ctScale;
                                ct.isStatic = true;
                                cattailsSpawned++;
                            }
                        }
                    }
                }
            }
            // -------------------------------------------------------------
            // B. ON THE IMMEDIATE SHORELINE BANK (17.8m <= bedY <= 20.5m)
            // -------------------------------------------------------------
            else if (bedY <= 20.5f)
            {
                if (rng.NextDouble() < plantChance)
                {
                    int plantCount = rng.Next(2, 5);
                    for (int pi = 0; pi < plantCount; pi++)
                    {
                        var wpPath = WATER_PLANT_PREFABS[rng.Next(WATER_PLANT_PREFABS.Length)];
                        var wpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wpPath);
                        if (wpPrefab != null)
                        {
                            float pox = (float)(rng.NextDouble() * 3.5 - 1.75);
                            float poz = (float)(rng.NextDouble() * 3.5 - 1.75);
                            var wp = (GameObject)PrefabUtility.InstantiatePrefab(wpPrefab, parent);
                            wp.transform.position = new Vector3(hit.point.x + pox, hit.point.y, hit.point.z + poz);
                            wp.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
                            float wpScale = 0.9f + (float)(rng.NextDouble() * 0.5f);
                            wp.transform.localScale = Vector3.one * wpScale;
                            wp.isStatic = true;
                            plantsSpawned++;
                        }
                    }
                }
            }
        }
    }
}

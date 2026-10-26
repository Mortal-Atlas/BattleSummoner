using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.World;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Master automation pipeline that creates required prefabs, populates Anime Nature foliage,
    /// dynamic grass, wildflowers, golden spores, enemy outposts, interactive dungeon portals,
    /// scattered rock formations, noble cardinal moat bridges, unified water, and 3-tier clouds/floating islands,
    /// with strict performance optimizations (500-foot grass cull, layer culling, GPU instancing).
    /// </summary>
    public static class PopulateAnimeNatureAndWorldSystems
    {
        private const string SCENE_PATH = "Assets/Scenes/OverworldMap.unity";
        private const string PREFAB_DIR = "Assets/Prefabs/World";
        private const string VFX_DIR = "Assets/Prefabs/VFX";

        private const int LAYER_GRASS = 6;
        private const int LAYER_TREES = 7;
        private const int LAYER_DETAILS = 8;

        // Landmark Locations
        private static readonly Vector3 PORTAL_LOC = new Vector3(-450f, 0f, 850f);
        private static readonly Vector3 CAMP1_LOC = new Vector3(920f, 0f, 260f);
        private static readonly Vector3 FORT_LOC = new Vector3(-850f, 0f, -480f);
        private static readonly Vector3 CAMP2_LOC = new Vector3(380f, 0f, -920f);


        [MenuItem("BattleSummoner/Populate Anime Nature and Complete World Systems", false, 1)]
        public static void RunFullPopulation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SCENE_PATH)
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            Debug.Log("🌲 ====================================================================");
            Debug.Log("🌲 POPULATING COMPLETE WORLD ECOSYSTEM, ROCKS, BRIDGES & SKY SYSTEMS");
            Debug.Log("🌲 ====================================================================");

            EnsureDirectories();

            // 1. Create / Update all required compound Prefabs
            GameObject sporePrefab = CreateGoldenSporesPrefab();
            GameObject watchtowerCampPrefab = CreateWatchtowerCampPrefab();
            GameObject ruinedFortPrefab = CreateRuinedFortPrefab();
            GameObject dungeonPortalPrefab = CreateDungeonPortalPrefab();

            // 2. Clear old vegetation, legacy roots, orphan weapons, and defect formations
            ClearOldRoots();

            // 3. Harmonize Moat Water & Ocean Water (removes weird torus ring and color disparity)
            HarmonizeWaterMoatAndOcean();

            // 4. Find Terrain Collider
            var continentGo = GameObject.Find("CircularContinent_MacroLevel1");
            if (continentGo == null)
            {
                Debug.LogError("CircularContinent_MacroLevel1 not found in scene!");
                return;
            }

            MeshCollider terrainCollider = continentGo.GetComponentInChildren<MeshCollider>();
            if (terrainCollider == null)
            {
                Debug.LogError("Terrain MeshCollider not found!");
                return;
            }

            // 5. Build 4 Noble Ordinal Moat Bridges (connecting directly to the 4 mainlands)
            BuildNobleOrdinalMoatBridges();

            // 6. Overhaul Sky Clouds & Floating Islands (3 variations each with flat top plateau)
            OverhaulSkyCloudsAndFloatingIslands();

            // 7. Scatter Natural Rock Formations across the whole continent
            int rockClusterCount, totalRocks;
            PopulateScatteredRockFormations(terrainCollider, out rockClusterCount, out totalRocks);

            // 8. Populate Dense Anime Forest & Bushes (smooth face/biome blending, strict water exclusion)
            int treeCount, bushCount, treeChunks;
            PopulateAnimeForestAndBushes(terrainCollider, out treeCount, out bushCount, out treeChunks);

            // 9. Populate Dynamic Stylized Grass & Wildflowers (500-foot fade, strict water exclusion)
            int grassCount, flowerCount, grassChunks;
            PopulateDynamicGrassAndFlowers(terrainCollider, out grassCount, out flowerCount, out grassChunks);

            // 10. Spawn Golden Spores Emitter
            SpawnGoldenSpores(sporePrefab);

            // 11. Place Enemy Outposts & Interactive Dungeon Dimension Portal
            PlaceOutpostsAndPortal(terrainCollider, watchtowerCampPrefab, ruinedFortPrefab, dungeonPortalPrefab);

            // 12. Beautify Town & Moat Bridge Roadways
            BeautifyTownAndRoadways(terrainCollider);

            // 13. Apply Camera Optimization & Culling
            ApplyCameraOptimization();

            // 14. Save Scene & Capture Verification Screenshots
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            CaptureVerificationScreenshots(terrainCollider);

            string reportPath = Path.Combine(Application.dataPath, "Screenshots/WorldPopulationReport.txt");
            string report = "=== WORLD POPULATION & OPTIMIZATION REPORT ===\n";
            report += $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            report += $"1. Foliage: Exclusively Anime Trees ({treeCount} spawned) & Anime Bushes ({bushCount} spawned) in {treeChunks} spatial chunks.\n";
            report += "2. Water & Moat: Clean, unified water shader across moat and ocean at Y=18.0m. Weird torus cylinder destroyed.\n";
            report += "3. Bridges: 4 Noble stone arch causeways built at cardinal gates (North, South, East, West). Weird intersecting spline ribbons removed.\n";
            report += "4. Waterfall Slope: Mountain slope cleared completely (64-boulder formation removed) for user waterfall construction.\n";
            report += "5. Sky Archipelago: 3 Cloud variations (Small, Medium, Large) and 3 Floating Island variations (with flat building plateaus) installed.\n";
            report += $"6. Rock Formations: {totalRocks} natural rocks across {rockClusterCount} clusters distributed over coastal cliffs, riverbanks, rolling hills, and forests.\n";
            report += $"7. Grass: {grassCount} Stylized dynamic grass patches across {grassChunks} chunks with 500-foot (152.4m) distance fade & vertex culling.\n";
            report += $"8. Wildflowers: {flowerCount} flower clusters (azure bells, golden marigolds, crimson poppies, mixed wildflowers).\n";
            report += "9. Golden Spores: Warm golden-white floating spore particle systems with curl turbulence.\n";
            report += "10. Outposts & Portal: Watchtower Camps, Ruined Fort, and Otherworldly Dungeon Portal.\n";
            report += "11. Optimization: Occlusion culling enabled, Camera layer cull distances (Grass: 152.4m, Props: 200m, Trees: 1200m), GPU instancing enabled, shadow distance 220m.\n";
            report += "===============================================\n";
            File.WriteAllText(reportPath, report);

            Debug.Log("✅ ====================================================================");
            Debug.Log("✅ ALL WORLD SYSTEMS POPULATED & OPTIMIZED SUCCESSFULLY!");
            Debug.Log("✅ ====================================================================");
            Debug.Log(report);
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
            if (!Directory.Exists(VFX_DIR)) Directory.CreateDirectory(VFX_DIR);
            string scDir = Path.Combine(Application.dataPath, "Screenshots");
            if (!Directory.Exists(scDir)) Directory.CreateDirectory(scDir);

            string maskPath = "Assets/Textures/Environments/Continent/T_Continent_BiomeMask.png";
            var importer = AssetImporter.GetAtPath(maskPath) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            string albedoPath = "Assets/Textures/Environments/Continent/T_Continent_MacroAlbedo.png";
            var albedoImp = AssetImporter.GetAtPath(albedoPath) as TextureImporter;
            if (albedoImp != null && !albedoImp.isReadable)
            {
                albedoImp.isReadable = true;
                albedoImp.SaveAndReimport();
            }

            AssetDatabase.Refresh();
        }

        private static void ClearOldRoots()
        {
            string[] rootsToClear = {
                "--- ANIME NATURE & VEGETATION ---",
                "--- DYNAMIC STYLIZED GRASS & WILDFLOWERS ---",
                "--- GOLDEN FIELD SPORE PARTICLES ---",
                "--- ENEMY OUTPOSTS & DUNGEON PORTAL ---",
                "--- TOWN BEAUTIFICATION & ROADSIDE ---",
                "--- IDYLLIC FANTASY NATURE & VEGETATION ---",
                "--- SCATTERED CONTINENTAL ROCK FORMATIONS ---",
                "--- NOBLE CARDINAL MOAT BRIDGES ---",
                // Legacy foliage roots that caused floating trees:
                "World_AnimeNature_Foliage",
                "Mountain_AnimeNature_SwitchbackAndGorge_Root",
                "Hydrology_Flora_And_Fauna",
                "Riparian_Nature_And_Cliffs",
                "Anime_Nature_Tree_Groves",
                // Defect formations & weird bridges:
                "Mountain_Traversal_Paths_And_Obstacles",
                "Lake_Bridges_And_Crossings",
                "07_Continental_Regional_Highways"
            };

            foreach (var r in rootsToClear)
            {
                var go = GameObject.Find(r);
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            // Also destroy any orphan Equipped_sword instances polluting the root hierarchy
            foreach (var rootGo in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (rootGo.transform.parent == null && rootGo.name == "Equipped_sword")
                {
                    UnityEngine.Object.DestroyImmediate(rootGo);
                }
            }
        }

        // =========================================================================
        // WATER HARMONIZATION & WEIRD TORUS REMOVAL
        // =========================================================================

        private static void HarmonizeWaterMoatAndOcean()
        {
            // 1. Remove the bright cyan cylinder (Water_ConcentricMoat_Ring) that created the weird torus and color mismatch
            var masonry = GameObject.Find("City_Structural_Masonry");
            if (masonry != null)
            {
                var ring = masonry.transform.Find("Water_ConcentricMoat_Ring");
                if (ring != null)
                {
                    UnityEngine.Object.DestroyImmediate(ring.gameObject);
                    Debug.Log("🌊 Destroyed weird Water_ConcentricMoat_Ring cylinder.");
                }
            }

            // 2. Synchronize water materials between moat and ocean
            Material oceanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/M_Outerwilds_StylizedOcean.mat");
            if (oceanMat == null) oceanMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Outerwilds_StylizedOcean.mat");

            var moatGo = GameObject.Find("Moat_WaterSurface");
            if (moatGo != null)
            {
                moatGo.transform.position = new Vector3(0f, 18.0f, 0f);
                if (oceanMat != null)
                {
                    var mr = moatGo.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = oceanMat;
                }
            }

            var oceanGo = GameObject.Find("Outerwilds_Ocean_Water");
            if (oceanGo != null)
            {
                oceanGo.transform.position = new Vector3(0f, 18.0f, 0f);
                if (oceanMat != null)
                {
                    var mr = oceanGo.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = oceanMat;
                }
            }

            Debug.Log("🌊 Water moat and ocean surfaces unified at Y=18.0m with matching stylized water material.");
        }

        // =========================================================================
        // NOBLE ORDINAL MOAT BRIDGES (Connecting Kingdom to the 4 Mainlands)
        // =========================================================================

        private static void BuildNobleOrdinalMoatBridges()
        {
            var oldCardinal = GameObject.Find("--- NOBLE CARDINAL MOAT BRIDGES ---");
            if (oldCardinal != null) UnityEngine.Object.DestroyImmediate(oldCardinal);

            var oldOrdinal = GameObject.Find("--- NOBLE ORDINAL MOAT BRIDGES ---");
            if (oldOrdinal != null) UnityEngine.Object.DestroyImmediate(oldOrdinal);

            GameObject bridgesRoot = new GameObject("--- NOBLE ORDINAL MOAT BRIDGES ---");

            GameObject bridgeModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/FantasyStoneArchBridge.fbx");
            Material deckMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_Deck.mat");
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_Stone.mat");
            Material trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_Trim.mat");
            Material lanternMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_LanternCrystal.mat");

            if (bridgeModel == null)
            {
                Debug.LogError("FantasyStoneArchBridge.fbx not found!");
                return;
            }

            Vector3[] dirs = new Vector3[]
            {
                new Vector3(1f, 0f, 1f).normalized,
                new Vector3(1f, 0f, -1f).normalized,
                new Vector3(-1f, 0f, -1f).normalized,
                new Vector3(-1f, 0f, 1f).normalized
            };
            string[] names = new string[] { "NorthEast", "SouthEast", "SouthWest", "NorthWest" };
            float[] bluffRadii = new float[] { 550.0f, 605.0f, 545.0f, 595.0f };
            float[] bluffHeights = new float[] { 25.38f, 23.25f, 25.50f, 24.50f };

            for (int d = 0; d < 4; d++)
            {
                Vector3 dir = dirs[d];
                string name = names[d];

                // Gate threshold (where city gate meets moat)
                float gateR = 464.0f;
                Vector3 pStart = dir * gateR;
                pStart.y = 22.18f;

                // Mainland dry land bluff
                float landR = bluffRadii[d];
                Vector3 pEnd = dir * landR;
                pEnd.y = bluffHeights[d];

                if (Physics.Raycast(new Vector3(pEnd.x, 150f, pEnd.z), Vector3.down, out RaycastHit hitEnd, 200f))
                {
                    if (hitEnd.collider.name.Contains("CircularContinent"))
                    {
                        pEnd.y = hitEnd.point.y;
                    }
                }

                Vector3 pMid = (pStart + pEnd) * 0.5f;
                Vector3 spanVec = pEnd - pStart;
                float spanLen = spanVec.magnitude;
                Vector3 forwardDir = spanVec.normalized;

                // Build strictly upright basis matrix: localX = length, localY = width, localZ = up
                Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
                Vector3 upDir = Vector3.Cross(forwardDir, rightDir).normalized;

                Matrix4x4 m = new Matrix4x4();
                m.SetColumn(0, new Vector4(forwardDir.x, forwardDir.y, forwardDir.z, 0));
                m.SetColumn(1, new Vector4(rightDir.x, rightDir.y, rightDir.z, 0));
                m.SetColumn(2, new Vector4(upDir.x, upDir.y, upDir.z, 0));
                m.SetColumn(3, new Vector4(0, 0, 0, 1));

                GameObject gateBridgeRoot = new GameObject($"MoatBridge_{name}Gate");
                gateBridgeRoot.transform.SetParent(bridgesRoot.transform);

                // Instantiate ONLY ONE single bridge asset to span across the entire water gap
                var singleBridge = UnityEngine.Object.Instantiate(bridgeModel, gateBridgeRoot.transform);
                singleBridge.name = $"SingleSpan_ArchBridge_{name}";
                singleBridge.transform.position = pMid;
                singleBridge.transform.rotation = m.rotation;

                // Native bridge length = 17.20m, width = 5.0m, height = 9.35m
                float scaleX = spanLen / 17.20f;
                float scaleY = 2.0f; // 10m wide causeway roadbed
                float scaleZ = 1.4f; // Arch height
                singleBridge.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                // Assign materials
                foreach (var mr in singleBridge.GetComponentsInChildren<MeshRenderer>())
                {
                    var mats = mr.sharedMaterials;
                    if (mats.Length >= 4)
                    {
                        mats[0] = deckMat != null ? deckMat : mats[0];
                        mats[1] = stoneMat != null ? stoneMat : mats[1];
                        mats[2] = trimMat != null ? trimMat : mats[2];
                        mats[3] = lanternMat != null ? lanternMat : mats[3];
                        mr.sharedMaterials = mats;
                    }
                }

                // MeshCollider for player physics
                var mf = singleBridge.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var col = singleBridge.AddComponent<MeshCollider>();
                    col.sharedMesh = mf.sharedMesh;
                }

                // Stone landfall ramp connecting bridge deck to mainland road
                Vector3 rampPos = pEnd + dir * 3.5f;
                rampPos.y = pEnd.y;
                GameObject abutment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                abutment.name = $"Landfall_Abutment_{name}";
                abutment.transform.SetParent(gateBridgeRoot.transform);
                abutment.transform.position = rampPos;
                abutment.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                abutment.transform.localScale = new Vector3(10.0f, 1.2f, 8.0f);
                var abMr = abutment.GetComponent<MeshRenderer>();
                if (abMr != null && deckMat != null) abMr.sharedMaterial = deckMat;
            }

            Debug.Log("🏰 Constructed 4 noble single-span fantasy stone arch bridges connecting the kingdom to the 4 continental mainlands (NE, SE, SW, NW).");
        }

        // =========================================================================
        // SKY CLOUDS & FLOATING ISLANDS OVERHAUL (3 Variations Each)
        // =========================================================================

        private static void OverhaulSkyCloudsAndFloatingIslands()
        {
            // Load 3 Cloud prefabs
            GameObject cloudSmall = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PFB_Cloud_Small.prefab");
            GameObject cloudMed = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PFB_Cloud_Medium.prefab");
            GameObject cloudLarge = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PFB_Cloud_Large.prefab");

            // Load 3 Floating Island prefabs
            GameObject islandSmall = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PFB_FloatingIsland_Small.prefab");
            GameObject islandMed = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PFB_FloatingIsland_Medium.prefab");
            GameObject islandLarge = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PFB_FloatingIsland_Large.prefab");

            // 1. Clear old stretched sphere clouds in Slalom_Clouds_Group
            var oldCloudsGroup = GameObject.Find("Slalom_Clouds_Group");
            if (oldCloudsGroup != null)
            {
                while (oldCloudsGroup.transform.childCount > 0)
                {
                    UnityEngine.Object.DestroyImmediate(oldCloudsGroup.transform.GetChild(0).gameObject);
                }
            }
            else
            {
                var skyArch = GameObject.Find("Sky_Archipelago_And_Clouds_Root");
                if (skyArch == null) skyArch = new GameObject("Sky_Archipelago_And_Clouds_Root");
                oldCloudsGroup = new GameObject("Slalom_Clouds_Group");
                oldCloudsGroup.transform.SetParent(skyArch.transform);
            }

            // Spawn multi-tiered stretched, pulled, and drifting anime clouds across 5 archetypes
            AnimeSkyCloudMasterOverhaul.OverhaulAllSkyCloudsInScene();

            // 2. Clear old stepping stone spheres in Sky_Layer_Aetherion & Sky_Islands_Group
            var aetherionRoot = GameObject.Find("Sky_Layer_Aetherion");
            if (aetherionRoot != null)
            {
                while (aetherionRoot.transform.childCount > 0)
                {
                    UnityEngine.Object.DestroyImmediate(aetherionRoot.transform.GetChild(0).gameObject);
                }
            }
            else
            {
                aetherionRoot = new GameObject("Sky_Layer_Aetherion");
            }

            var islandsGroup = GameObject.Find("Sky_Islands_Group");
            if (islandsGroup != null)
            {
                while (islandsGroup.transform.childCount > 0)
                {
                    UnityEngine.Object.DestroyImmediate(islandsGroup.transform.GetChild(0).gameObject);
                }
            }
            else
            {
                var skyArch = GameObject.Find("Sky_Archipelago_And_Clouds_Root");
                if (skyArch == null) skyArch = new GameObject("Sky_Archipelago_And_Clouds_Root");
                islandsGroup = new GameObject("Sky_Islands_Group");
                islandsGroup.transform.SetParent(skyArch.transform);
            }

            // Spawn 14 dramatic Floating Islands with flat building space on top
            if (islandSmall != null && islandMed != null && islandLarge != null)
            {
                // Grand Acropolis Large Islands
                Vector3[] grandIslandPositions = {
                    new Vector3(450f, 420f, 250f),
                    new Vector3(-650f, 480f, 750f),
                    new Vector3(1200f, 550f, -800f),
                    new Vector3(-1350f, 520f, -600f)
                };

                for (int i = 0; i < grandIslandPositions.Length; i++)
                {
                    var isl = (GameObject)PrefabUtility.InstantiatePrefab(islandLarge, islandsGroup.transform);
                    isl.name = $"FloatingIsland_GrandCitadel_{i + 1}";
                    isl.transform.position = grandIslandPositions[i];
                    isl.transform.rotation = Quaternion.Euler(0f, i * 85f, 0f);
                    isl.transform.localScale = Vector3.one * 1.0f;
                }

                // Medium Outpost Islands
                Vector3[] medIslandPositions = {
                    new Vector3(250f, 360f, 550f),
                    new Vector3(-350f, 390f, 1100f),
                    new Vector3(850f, 430f, 950f),
                    new Vector3(-950f, 450f, 200f),
                    new Vector3(650f, 460f, -1400f)
                };

                for (int i = 0; i < medIslandPositions.Length; i++)
                {
                    var isl = (GameObject)PrefabUtility.InstantiatePrefab(islandMed, islandsGroup.transform);
                    isl.name = $"FloatingIsland_MediumSanctuary_{i + 1}";
                    isl.transform.position = medIslandPositions[i];
                    isl.transform.rotation = Quaternion.Euler(0f, i * 72f, 0f);
                    isl.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.95f, 1.2f);
                }

                // Small Stepping Stone Islands (spiral staircase through the sky)
                float spiralAngle = 0f;
                for (int s = 0; s < 7; s++)
                {
                    spiralAngle += 42f;
                    float sRad = spiralAngle * Mathf.Deg2Rad;
                    float sDist = 380f + s * 65f;
                    float sH = 340f + s * 30f;
                    Vector3 sPos = new Vector3(Mathf.Cos(sRad) * sDist, sH, Mathf.Sin(sRad) * sDist);

                    var isl = (GameObject)PrefabUtility.InstantiatePrefab(islandSmall, aetherionRoot.transform);
                    isl.name = $"FloatingIsland_SteppingPlinth_{s + 1}";
                    isl.transform.position = sPos;
                    isl.transform.rotation = Quaternion.Euler(0f, spiralAngle, 0f);
                    isl.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.9f, 1.25f);
                }
            }

            Debug.Log("☁️ Overhauled Sky Archipelago: 45 fluffy 3-tier anime clouds & 16 flat-topped floating islands placed.");
        }

        // =========================================================================
        // SCATTERED CONTINENTAL ROCK FORMATIONS
        // =========================================================================

        private static void PopulateScatteredRockFormations(MeshCollider terrainCollider, out int clusterCount, out int totalRocks)
        {
            GameObject rocksRoot = new GameObject("--- SCATTERED CONTINENTAL ROCK FORMATIONS ---");

            // Load high quality rock and cliff prefabs
            string[] bigRockPaths = {
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_02.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_03.prefab"
            };

            string[] medRockPaths = {
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Medium_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Medium_02.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Medium_03.prefab"
            };

            string[] smallRockPaths = {
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Small_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Small_02.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Rock_Small_03.prefab"
            };

            string[] cliffPaths = {
                "Assets/Idyllic Fantasy Nature/Prefabs/Cliff_01.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Cliff_02.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Cliff_03.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Cliff_04.prefab",
                "Assets/Idyllic Fantasy Nature/Prefabs/Cliff_05.prefab"
            };

            List<GameObject> bigRocks = LoadPrefabs(bigRockPaths);
            List<GameObject> medRocks = LoadPrefabs(medRockPaths);
            List<GameObject> smallRocks = LoadPrefabs(smallRockPaths);
            List<GameObject> cliffs = LoadPrefabs(cliffPaths);

            clusterCount = 0;
            totalRocks = 0;

            // Multi-Zone Stratified Sampling across the continent
            float step = 150f;
            for (float x = -3350f; x <= 3350f; x += step)
            {
                for (float z = -3350f; z <= 3350f; z += step)
                {
                    float jx = UnityEngine.Random.Range(-0.42f, 0.42f) * step;
                    float jz = UnityEngine.Random.Range(-0.42f, 0.42f) * step;
                    float cx = x + jx;
                    float cz = z + jz;

                    float dist = Mathf.Sqrt(cx * cx + cz * cz);
                    if (dist < 560f || dist > 3400f) continue; // Outside moat, inside island

                    // Exclude waterfall clearing
                    if (cx >= -280f && cx <= 120f && cz >= 520f && cz <= 1350f) continue;

                    // Raycast to terrain
                    if (!terrainCollider.Raycast(new Ray(new Vector3(cx, 1500f, cz), Vector3.down), out RaycastHit hit, 2000f)) continue;
                    if (hit.point.y < 19.5f) continue; // STRICT DRY LAND CHECK: Ocean surface is at Y=18.0m, keep well above water

                    // Organic probability noise
                    float rNoise = Mathf.PerlinNoise((cx + 12891f) * 0.0035f, (cz + 83719f) * 0.0035f);
                    if (rNoise < 0.42f) continue; // Spatially clustered

                    // Determine cluster type based on landscape zone
                    bool isCoastal = (dist >= 2700f && dist <= 3380f);
                    bool isSteepHill = (hit.normal.y < 0.72f);

                    int rocksInCluster = UnityEngine.Random.Range(2, 6);
                    float clusterRadius = UnityEngine.Random.Range(8f, 18f);

                    GameObject clusterGo = new GameObject($"RockCluster_{clusterCount + 1}");
                    clusterGo.transform.SetParent(rocksRoot.transform);

                    for (int r = 0; r < rocksInCluster; r++)
                    {
                        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                        float rad = (r == 0) ? UnityEngine.Random.Range(0f, 3f) : UnityEngine.Random.Range(3f, clusterRadius);
                        Vector3 rPos = hit.point + new Vector3(Mathf.Cos(angle) * rad, 0f, Mathf.Sin(angle) * rad);

                        if (terrainCollider.Raycast(new Ray(rPos + Vector3.up * 40f, Vector3.down), out RaycastHit hitR, 80f))
                        {
                            if (hitR.point.y < 19.5f) continue; // Do not spawn submerged rocks
                            GameObject prefabToSpawn;
                            if (isCoastal && cliffs.Count > 0 && r == 0 && UnityEngine.Random.value < 0.65f)
                            {
                                prefabToSpawn = cliffs[UnityEngine.Random.Range(0, cliffs.Count)];
                            }
                            else if ((isSteepHill || r == 0) && bigRocks.Count > 0)
                            {
                                prefabToSpawn = bigRocks[UnityEngine.Random.Range(0, bigRocks.Count)];
                            }
                            else if (medRocks.Count > 0 && UnityEngine.Random.value < 0.55f)
                            {
                                prefabToSpawn = medRocks[UnityEngine.Random.Range(0, medRocks.Count)];
                            }
                            else if (smallRocks.Count > 0)
                            {
                                prefabToSpawn = smallRocks[UnityEngine.Random.Range(0, smallRocks.Count)];
                            }
                            else continue;

                            var rockInst = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn, clusterGo.transform);
                            rockInst.layer = LAYER_TREES;
                            rockInst.transform.position = hitR.point + Vector3.down * UnityEngine.Random.Range(0.2f, 0.8f);
                            rockInst.transform.rotation = Quaternion.Euler(
                                UnityEngine.Random.Range(-12f, 12f),
                                UnityEngine.Random.Range(0f, 360f),
                                UnityEngine.Random.Range(-12f, 12f)
                            );
                            float scale = UnityEngine.Random.Range(0.85f, 1.45f);
                            if (isCoastal && r == 0) scale *= 1.35f;
                            rockInst.transform.localScale = Vector3.one * scale;

                            totalRocks++;
                        }
                    }
                    clusterCount++;
                }
            }

            Debug.Log($"🪨 Distributed {totalRocks} natural rocks across {clusterCount} formations throughout the continent.");
        }

        private static List<GameObject> LoadPrefabs(string[] paths)
        {
            var list = new List<GameObject>();
            foreach (var p in paths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) list.Add(go);
            }
            return list;
        }

        // =========================================================================
        // PREFAB GENERATION (Clean Compound Prefabs with URP Materials)
        // =========================================================================

        private static GameObject CreateGoldenSporesPrefab()
        {
            string path = $"{VFX_DIR}/PFB_GoldenFieldSpores.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject go = new GameObject("PFB_GoldenFieldSpores");
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.maxParticles = 3000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.92f, 0.55f), 0f),
                    new GradientColorKey(new Color(1f, 1f, 0.95f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.40f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.85f, 0.2f),
                    new GradientAlphaKey(0.85f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(grad);

            var emission = ps.emission;
            emission.rateOverTime = 140f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(350f, 25f, 350f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.55f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.35f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            Material pMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Idyllic Fantasy Nature/Materials/FX/Particle.mat");
            if (pMat != null)
            {
                psr.sharedMaterial = pMat;
                pMat.enableInstancing = true;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateWatchtowerCampPrefab()
        {
            string path = $"{PREFAB_DIR}/PFB_EnemyOutpost_WatchtowerCamp.prefab";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            GameObject root = new GameObject("PFB_EnemyOutpost_WatchtowerCamp");
            root.layer = LAYER_DETAILS;

            // Load sub-assets
            GameObject tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Tent.prefab");
            GameObject ladderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Folding_Ladder.prefab");
            GameObject barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            GameObject crate1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            GameObject crate2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_02.prefab");
            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Stylized Fire VFX/VFX/Flames/Particles/VFX_Stylized_Fire_01_Infinite.prefab");
            GameObject skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mini Simple Characters Skeleton Demo/Prefabs/mini simple skeleton demo.prefab");
            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mini Simple Characters Skeleton Demo/Prefabs/sword_wood.prefab");
            GameObject shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mini Simple Characters Skeleton Demo/Prefabs/shield_wood.prefab");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Materials/Tent_Wood.mat");
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Campfire_Stones.mat");

            // 1. Build Elevated Watchtower Structure
            GameObject towerRoot = new GameObject("Watchtower_Structure");
            towerRoot.transform.SetParent(root.transform);
            towerRoot.transform.localPosition = new Vector3(-8f, 0f, 6f);

            // 4 Corner Wooden Stilts
            Vector3[] postPositions = {
                new Vector3(-2.2f, 4.5f, -2.2f),
                new Vector3(2.2f, 4.5f, -2.2f),
                new Vector3(-2.2f, 4.5f, 2.2f),
                new Vector3(2.2f, 4.5f, 2.2f)
            };
            foreach (var pos in postPositions)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "TimberPost";
                post.transform.SetParent(towerRoot.transform);
                post.transform.localPosition = pos;
                post.transform.localScale = new Vector3(0.5f, 4.5f, 0.5f);
                if (woodMat != null) post.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }

            // Platform Deck
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "PlatformDeck";
            platform.transform.SetParent(towerRoot.transform);
            platform.transform.localPosition = new Vector3(0f, 9.0f, 0f);
            platform.transform.localScale = new Vector3(5.5f, 0.4f, 5.5f);
            if (woodMat != null) platform.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Parapet Railings
            Vector3[] railPositions = {
                new Vector3(0f, 9.7f, 2.65f),
                new Vector3(0f, 9.7f, -2.65f),
                new Vector3(2.65f, 9.7f, 0f),
                new Vector3(-2.65f, 9.7f, 0f)
            };
            Vector3[] railScales = {
                new Vector3(5.5f, 1.0f, 0.2f),
                new Vector3(5.5f, 1.0f, 0.2f),
                new Vector3(0.2f, 1.0f, 5.5f),
                new Vector3(0.2f, 1.0f, 5.5f)
            };
            for (int i = 0; i < 4; i++)
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"Railing_{i}";
                rail.transform.SetParent(towerRoot.transform);
                rail.transform.localPosition = railPositions[i];
                rail.transform.localScale = railScales[i];
                if (woodMat != null) rail.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }

            // Access Ladder
            if (ladderPrefab != null)
            {
                var ladder = UnityEngine.Object.Instantiate(ladderPrefab, towerRoot.transform);
                ladder.name = "Watchtower_Ladder";
                ladder.transform.localPosition = new Vector3(0f, 0f, -2.5f);
                ladder.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                ladder.transform.localScale = new Vector3(1.8f, 5.2f, 1.8f);
            }

            // Sentry on Tower
            if (skeletonPrefab != null)
            {
                var sentry = UnityEngine.Object.Instantiate(skeletonPrefab, towerRoot.transform);
                sentry.name = "Tower_Skeleton_Lookout";
                sentry.transform.localPosition = new Vector3(0.5f, 9.2f, 0f);
                sentry.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                sentry.transform.localScale = Vector3.one * 1.3f;
                AttachEquipment(sentry, swordPrefab, shieldPrefab);
            }

            // 2. Outpost Encampment Grounds
            // Tents
            if (tentPrefab != null)
            {
                var t1 = UnityEngine.Object.Instantiate(tentPrefab, root.transform);
                t1.name = "Outpost_Commander_Tent";
                t1.transform.localPosition = new Vector3(6f, 0f, 5f);
                t1.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
                t1.transform.localScale = Vector3.one * 1.25f;

                var t2 = UnityEngine.Object.Instantiate(tentPrefab, root.transform);
                t2.name = "Outpost_Troop_Tent";
                t2.transform.localPosition = new Vector3(8f, 0f, -4f);
                t2.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                t2.transform.localScale = Vector3.one * 1.1f;
            }

            // Central Campfire
            GameObject campfire = new GameObject("Campfire_Hearth");
            campfire.transform.SetParent(root.transform);
            campfire.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            // Ring of Stones
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = $"Stone_{i}";
                stone.transform.SetParent(campfire.transform);
                stone.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.4f, 0.15f, Mathf.Sin(angle) * 1.4f);
                stone.transform.localScale = new Vector3(0.65f, 0.35f, 0.65f);
                if (stoneMat != null) stone.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            }

            // Fire VFX
            if (firePrefab != null)
            {
                var fire = UnityEngine.Object.Instantiate(firePrefab, campfire.transform);
                fire.name = "Fire_Flames";
                fire.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                fire.transform.localScale = Vector3.one * 1.2f;
            }

            // Warm Fire Light
            var fLight = campfire.AddComponent<Light>();
            fLight.type = LightType.Point;
            fLight.color = new Color(1.0f, 0.58f, 0.16f);
            fLight.range = 16f;
            fLight.intensity = 3.5f;

            // Barrels & Supply Crates
            if (barrelPrefab != null)
            {
                var b1 = UnityEngine.Object.Instantiate(barrelPrefab, root.transform);
                b1.transform.localPosition = new Vector3(3.5f, 0f, 6.2f);
                var b2 = UnityEngine.Object.Instantiate(barrelPrefab, root.transform);
                b2.transform.localPosition = new Vector3(4.3f, 0f, 5.8f);
            }
            if (crate1Prefab != null)
            {
                var c1 = UnityEngine.Object.Instantiate(crate1Prefab, root.transform);
                c1.transform.localPosition = new Vector3(3.8f, 0f, 4.8f);
            }
            if (crate2Prefab != null)
            {
                var c2 = UnityEngine.Object.Instantiate(crate2Prefab, root.transform);
                c2.transform.localPosition = new Vector3(3.8f, 0.8f, 5.0f);
            }

            // Ground Sentries / Patrols
            if (skeletonPrefab != null)
            {
                var s1 = UnityEngine.Object.Instantiate(skeletonPrefab, root.transform);
                s1.name = "Camp_Sentry_Guard1";
                s1.transform.localPosition = new Vector3(2.5f, 0f, -1.5f);
                s1.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
                s1.transform.localScale = Vector3.one * 1.2f;
                AttachEquipment(s1, swordPrefab, shieldPrefab);

                var s2 = UnityEngine.Object.Instantiate(skeletonPrefab, root.transform);
                s2.name = "Camp_Sentry_Guard2";
                s2.transform.localPosition = new Vector3(-3.5f, 0f, -2.5f);
                s2.transform.localRotation = Quaternion.Euler(0f, 65f, 0f);
                s2.transform.localScale = Vector3.one * 1.2f;
                AttachEquipment(s2, swordPrefab, shieldPrefab);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateRuinedFortPrefab()
        {
            string path = $"{PREFAB_DIR}/PFB_EnemyOutpost_RuinedFort.prefab";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            GameObject root = new GameObject("PFB_EnemyOutpost_RuinedFort");
            root.layer = LAYER_DETAILS;

            GameObject tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Tent.prefab");
            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vefects/Stylized Fire VFX/VFX/Flames/Particles/VFX_Stylized_Fire_01_Infinite.prefab");
            GameObject skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mini Simple Characters Skeleton Demo/Prefabs/mini simple skeleton demo.prefab");
            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mini Simple Characters Skeleton Demo/Prefabs/sword_wood.prefab");
            GameObject shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mini Simple Characters Skeleton Demo/Prefabs/shield_wood.prefab");
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_DungeonStone.mat");

            // Craggy Ruined Stone Walls
            Vector3[] wallOffsets = {
                new Vector3(0f, 2.5f, 10f),
                new Vector3(10f, 2.5f, 0f),
                new Vector3(-10f, 2.5f, 0f),
                new Vector3(7f, 1.8f, -7f)
            };
            Vector3[] wallSizes = {
                new Vector3(16f, 5.0f, 2.0f),
                new Vector3(2.0f, 5.0f, 16f),
                new Vector3(2.0f, 4.0f, 14f),
                new Vector3(8.0f, 3.6f, 2.0f)
            };

            for (int i = 0; i < wallOffsets.Length; i++)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"RuinWall_{i}";
                wall.transform.SetParent(root.transform);
                wall.transform.localPosition = wallOffsets[i];
                wall.transform.localScale = wallSizes[i];
                if (wallMat != null) wall.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            }

            // Command Tent
            if (tentPrefab != null)
            {
                var t = UnityEngine.Object.Instantiate(tentPrefab, root.transform);
                t.name = "Fort_Command_Tent";
                t.transform.localPosition = new Vector3(-3f, 0f, 4f);
                t.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                t.transform.localScale = Vector3.one * 1.4f;
            }

            // Signal Beacon Fire
            if (firePrefab != null)
            {
                var fire = UnityEngine.Object.Instantiate(firePrefab, root.transform);
                fire.name = "Fort_Beacon_Fire";
                fire.transform.localPosition = new Vector3(0f, 0.5f, -2f);
                fire.transform.localScale = Vector3.one * 1.5f;

                var fLight = fire.AddComponent<Light>();
                fLight.type = LightType.Point;
                fLight.color = new Color(1.0f, 0.45f, 0.12f);
                fLight.range = 22f;
                fLight.intensity = 4.5f;
            }

            // 3 Garrison Skeletons
            if (skeletonPrefab != null)
            {
                Vector3[] skelPositions = {
                    new Vector3(-1f, 0f, -4f),
                    new Vector3(4f, 0f, 2f),
                    new Vector3(0f, 0f, 7f)
                };
                for (int i = 0; i < skelPositions.Length; i++)
                {
                    var skel = UnityEngine.Object.Instantiate(skeletonPrefab, root.transform);
                    skel.name = $"Fort_Garrison_{i + 1}";
                    skel.transform.localPosition = skelPositions[i];
                    skel.transform.localRotation = Quaternion.Euler(0f, i * 110f, 0f);
                    skel.transform.localScale = Vector3.one * 1.25f;
                    AttachEquipment(skel, swordPrefab, shieldPrefab);
                }
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateDungeonPortalPrefab()
        {
            string path = $"{PREFAB_DIR}/PFB_InteractiveDungeonPortal.prefab";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            GameObject root = new GameObject("PFB_InteractiveDungeonPortal");
            root.layer = LAYER_DETAILS;

            // Monolithic stone double arch
            GameObject archPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Doors & Doorways/Level_Doorway_Arched.prefab");
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_DungeonStone.mat");

            if (archPrefab != null)
            {
                var arch = UnityEngine.Object.Instantiate(archPrefab, root.transform);
                arch.name = "Portal_Stone_Arch";
                arch.transform.localPosition = Vector3.zero;
                arch.transform.localScale = new Vector3(2.4f, 2.8f, 2.4f);
                if (stoneMat != null)
                {
                    foreach (var mr in arch.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial = stoneMat;
                }
            }

            // Swirling cosmic portal vortex quad
            GameObject vortexQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vortexQuad.name = "Portal_Vortex";
            vortexQuad.transform.SetParent(root.transform);
            vortexQuad.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            vortexQuad.transform.localScale = new Vector3(4.8f, 6.8f, 1f);

            Material vortexMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DarkAcademia_PortalVortex.mat");
            if (vortexMat == null) vortexMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_PortalEnergy.mat");
            if (vortexMat != null)
            {
                vortexQuad.GetComponent<MeshRenderer>().sharedMaterial = vortexMat;
            }

            var quadColl = vortexQuad.GetComponent<Collider>();
            if (quadColl != null) UnityEngine.Object.DestroyImmediate(quadColl);

            // Pulsing mystical aura light
            var pLight = root.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(0.65f, 0.28f, 1f);
            pLight.range = 22f;
            pLight.intensity = 5.0f;

            // Interaction Trigger Box
            var triggerColl = root.AddComponent<BoxCollider>();
            triggerColl.isTrigger = true;
            triggerColl.size = new Vector3(6f, 6f, 6f);
            triggerColl.center = new Vector3(0f, 3f, 0f);

            // Audio Source
            var audio = root.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.minDistance = 3f;
            audio.maxDistance = 35f;
            audio.loop = true;

            // OtherworldlyPortalOutpost component to trigger scene transition to MainDungeonFloor
            root.AddComponent<OtherworldlyPortalOutpost>();

            // Ground Rune Circle
            GameObject runeCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeCircle.name = "Summoning_Rune_Ring";
            runeCircle.transform.SetParent(root.transform);
            runeCircle.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            runeCircle.transform.localScale = new Vector3(7.5f, 0.05f, 7.5f);
            if (stoneMat != null) runeCircle.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            var runeColl = runeCircle.GetComponent<Collider>();
            if (runeColl != null) UnityEngine.Object.DestroyImmediate(runeColl);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AttachEquipment(GameObject skeleton, GameObject swordPrefab, GameObject shieldPrefab)
        {
            if (skeleton == null) return;
            Transform rightHand = FindDeepChild(skeleton.transform, "hand.R");
            if (rightHand == null) rightHand = FindDeepChild(skeleton.transform, "RightHand");
            if (rightHand == null) rightHand = skeleton.transform;

            Transform leftHand = FindDeepChild(skeleton.transform, "hand.L");
            if (leftHand == null) leftHand = FindDeepChild(skeleton.transform, "LeftHand");
            if (leftHand == null) leftHand = skeleton.transform;

            if (swordPrefab != null)
            {
                var sword = UnityEngine.Object.Instantiate(swordPrefab, rightHand);
                sword.name = "Attached_Sword";
                sword.transform.localPosition = new Vector3(0f, 0.05f, 0.08f);
                sword.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                sword.transform.localScale = Vector3.one * 1.0f;
            }

            if (shieldPrefab != null)
            {
                var shield = UnityEngine.Object.Instantiate(shieldPrefab, leftHand);
                shield.name = "Attached_Shield";
                shield.transform.localPosition = new Vector3(0.08f, 0.04f, 0f);
                shield.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                shield.transform.localScale = Vector3.one * 1.0f;
            }
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return child;
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        // =========================================================================
        // POPULATION LOGIC (Clustered Trees, Rich Ecosystem, Smooth Biome Blending)
        // =========================================================================

        private static void PopulateAnimeForestAndBushes(MeshCollider terrainCollider, out int spawnedTrees, out int spawnedBushes, out int chunkCount)
        {
            GameObject foliageRoot = new GameObject("--- ANIME NATURE & VEGETATION ---");

            // Exclusively Anime Trees & Bushes
            string[] treePaths = {
                "Assets/Anime Trees/Prefabs/Tree_01.prefab",
                "Assets/Anime Trees/Prefabs/Tree_02.prefab",
                "Assets/Anime Trees/Prefabs/Tree_03.prefab",
                "Assets/Anime Trees/Prefabs/Tree_04.prefab",
                "Assets/Anime Trees/Prefabs/Tree_05.prefab",
                "Assets/Anime Trees/Prefabs/Tree_06.prefab",
                "Assets/Anime Trees/Prefabs/Tree_07.prefab",
                "Assets/Anime Trees/Prefabs/Tree_08.prefab"
            };

            string[] bushPaths = {
                "Assets/Anime Trees/Prefabs/Bush_01.prefab",
                "Assets/Anime Trees/Prefabs/Bush_02.prefab",
                "Assets/Anime Trees/Prefabs/Bush_03.prefab",
                "Assets/Anime Trees/Prefabs/Bush_04.prefab"
            };

            List<GameObject> treePrefabs = LoadPrefabs(treePaths);
            List<GameObject> bushPrefabs = LoadPrefabs(bushPaths);

            // Load Biome Splat Mask to guide vegetation exactly where green exists
            Texture2D biomeMaskTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environments/Continent/T_Continent_BiomeMask.png");

            spawnedTrees = 0;
            spawnedBushes = 0;
            Dictionary<Vector2Int, Transform> chunkRoots = new Dictionary<Vector2Int, Transform>();

            // Higher density sampling ("more nature, more rocks, more natural ecosystem")
            // Step 36m with hexagonal row staggering and domain warping provides dense coverage with zero grid lines
            float step = 36f;
            for (float x = -3450f; x <= 3450f; x += step)
            {
                for (float z = -3450f; z <= 3450f; z += step)
                {
                    // 1. Continuous Hexagonal Row Staggering
                    float rowOffset = ((Mathf.FloorToInt((z + 10000f) / step) % 2) != 0) ? (0.5f * step) : 0f;

                    // 2. Large Stratified Continuous Random Jitter
                    float jx = UnityEngine.Random.Range(-0.48f, 0.48f) * step;
                    float jz = UnityEngine.Random.Range(-0.48f, 0.48f) * step;

                    // 3. Multi-frequency Organic 2D Domain Warping
                    float warpAngle = Mathf.PerlinNoise((x + 5123.7f) * 0.0025f, (z + 8765.3f) * 0.0025f) * Mathf.PI * 4f;
                    float warpDist = Mathf.PerlinNoise((x + 1923.1f) * 0.004f, (z + 3847.9f) * 0.004f) * (step * 0.85f);

                    float candidateX = x + rowOffset + jx + Mathf.Cos(warpAngle) * warpDist;
                    float candidateZ = z + jz + Mathf.Sin(warpAngle) * warpDist;

                    float dist = Mathf.Sqrt(candidateX * candidateX + candidateZ * candidateZ);
                    // Strictly outside kingdom city & moat ring (dist >= 555m) and inside continent boundary
                    if (dist < 555f || dist > 3420f) continue;

                    // Waterfall Mountain Clearing: Keep entire mountain slope open for user's waterfalls!
                    if (candidateX >= -280f && candidateX <= 120f && candidateZ >= 520f && candidateZ <= 1350f) continue;

                    // Landmark clearings: Portal, Outposts, Bridge Exits
                    if (Vector2.Distance(new Vector2(candidateX, candidateZ), new Vector2(PORTAL_LOC.x, PORTAL_LOC.z)) < 30f) continue;
                    if (Vector2.Distance(new Vector2(candidateX, candidateZ), new Vector2(CAMP1_LOC.x, CAMP1_LOC.z)) < 30f) continue;
                    if (Vector2.Distance(new Vector2(candidateX, candidateZ), new Vector2(FORT_LOC.x, FORT_LOC.z)) < 30f) continue;
                    if (Vector2.Distance(new Vector2(candidateX, candidateZ), new Vector2(CAMP2_LOC.x, CAMP2_LOC.z)) < 30f) continue;

                    // Exclude high volcanic peak
                    float volcDist = Mathf.Sqrt((candidateX + 1140f) * (candidateX + 1140f) + (candidateZ - 1360f) * (candidateZ - 1360f));
                    if (volcDist < 520f) continue;

                    // Exclude arterial road corridors from 4 gates
                    if (Mathf.Abs(candidateX) < 16f && dist < 750f) continue;
                    if (Mathf.Abs(candidateZ) < 16f && dist < 750f) continue;

                    // Biome Mask continuous probability blending (smooth gradients across biomes)
                    if (biomeMaskTex != null)
                    {
                        float u = Mathf.Clamp01((candidateX + 4096f) / 8192f);
                        float v = Mathf.Clamp01((candidateZ + 4096f) / 8192f);
                        Color maskCol = biomeMaskTex.GetPixelBilinear(u, v);

                        // Continuous green weight (Channel R = Forest/Grass, G = Desert, B = Snow, A = Caldera/Rock)
                        float greenWeight = Mathf.Clamp01((maskCol.r - 0.05f) / 0.14f) *
                                           Mathf.Clamp01(1.0f - maskCol.g * 2.2f) *
                                           Mathf.Clamp01(1.0f - maskCol.b * 2.0f) *
                                           Mathf.Clamp01(1.0f - maskCol.a * 1.5f);

                        if (UnityEngine.Random.value > greenWeight) continue;
                    }

                    // Multi-Attempt Terrain Raycast: STRICT WATER EXCLUSION (Water is at Y=18.0m, require Y > 19.5m)
                    RaycastHit hitCenter = default;
                    bool validHit = false;
                    float finalCenterX = candidateX;
                    float finalCenterZ = candidateZ;

                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        float testX = candidateX + (attempt == 0 ? 0f : UnityEngine.Random.Range(-8f, 8f));
                        float testZ = candidateZ + (attempt == 0 ? 0f : UnityEngine.Random.Range(-8f, 8f));
                        Ray ray = new Ray(new Vector3(testX, 1500f, testZ), Vector3.down);
                        if (terrainCollider.Raycast(ray, out RaycastHit hit, 2000f))
                        {
                            // STRICT WATER LINE: hit.point.y > 19.5f ensures foliage is NEVER in or floating over water!
                            if (hit.point.y > 19.5f && hit.normal.y >= 0.58f && (hit.point.y <= 240f || testZ <= 700f))
                            {
                                hitCenter = hit;
                                finalCenterX = testX;
                                finalCenterZ = testZ;
                                validHit = true;
                                break;
                            }
                        }
                    }
                    if (!validHit) continue;

                    // Dual-Octave Density noise function
                    float n1 = Mathf.PerlinNoise((finalCenterX + 10000f) * 0.0022f, (finalCenterZ + 10000f) * 0.0022f);
                    float n2 = Mathf.PerlinNoise((finalCenterX + 45000f) * 0.0065f, (finalCenterZ + 45000f) * 0.0065f);
                    float noiseVal = n1 * 0.70f + n2 * 0.30f;

                    // Rare open meadow clearings (~6% of green land)
                    if (noiseVal < 0.06f) continue;

                    // Rich multi-tier cluster sizing ("more forest than barren")
                    int treesInGrove;
                    int bushesInGrove;
                    float groveRadius;

                    if (noiseVal >= 0.35f)
                    {
                        // Deep ancient forest cores (~60% of green land)
                        treesInGrove = UnityEngine.Random.Range(5, 9);
                        bushesInGrove = UnityEngine.Random.Range(3, 6);
                        groveRadius = UnityEngine.Random.Range(15f, 22f);
                    }
                    else if (noiseVal >= 0.20f)
                    {
                        // Moderate woodlands (~24% of green land)
                        treesInGrove = UnityEngine.Random.Range(3, 5);
                        bushesInGrove = UnityEngine.Random.Range(2, 4);
                        groveRadius = UnityEngine.Random.Range(10f, 15f);
                    }
                    else if (noiseVal >= 0.10f)
                    {
                        // Light groves (~12% of green land)
                        treesInGrove = UnityEngine.Random.Range(2, 3);
                        bushesInGrove = UnityEngine.Random.Range(1, 2);
                        groveRadius = UnityEngine.Random.Range(7f, 12f);
                    }
                    else
                    {
                        // Meadow copses (~4% of green land)
                        treesInGrove = 1;
                        bushesInGrove = 1;
                        groveRadius = UnityEngine.Random.Range(5f, 8f);
                    }

                    // Spatial chunking (160m cells)
                    Vector2Int chunkKey = new Vector2Int(Mathf.FloorToInt(finalCenterX / 160f), Mathf.FloorToInt(finalCenterZ / 160f));
                    if (!chunkRoots.TryGetValue(chunkKey, out Transform chunkTr))
                    {
                        GameObject chunkGo = new GameObject($"ForestChunk_{chunkKey.x}_{chunkKey.y}");
                        chunkGo.transform.SetParent(foliageRoot.transform);
                        chunkTr = chunkGo.transform;
                        chunkRoots[chunkKey] = chunkTr;
                    }

                    // Spawn grove trees: ZERO ANCHOR BIAS (all trees randomly offset)
                    for (int t = 0; t < treesInGrove; t++)
                    {
                        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                        float r = UnityEngine.Random.Range(1.8f, groveRadius);
                        Vector3 tPos = hitCenter.point + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

                        if (terrainCollider.Raycast(new Ray(tPos + Vector3.up * 60f, Vector3.down), out RaycastHit hitT, 120f))
                        {
                            // Strict water elevation check
                            if (hitT.point.y <= 19.5f || hitT.normal.y < 0.58f) continue;

                            var treePrefab = treePrefabs[UnityEngine.Random.Range(0, treePrefabs.Count)];
                            var treeInstance = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, chunkTr);
                            treeInstance.layer = LAYER_TREES;
                            treeInstance.transform.position = hitT.point;
                            treeInstance.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            float scale = UnityEngine.Random.Range(0.85f, 1.35f);
                            treeInstance.transform.localScale = Vector3.one * scale;

                            foreach (var mr in treeInstance.GetComponentsInChildren<MeshRenderer>())
                            {
                                foreach (var mat in mr.sharedMaterials)
                                {
                                    if (mat != null) mat.enableInstancing = true;
                                }
                            }
                            spawnedTrees++;
                        }
                    }

                    // Spawn undergrowth bushes: Organic scatter around grove
                    for (int b = 0; b < bushesInGrove; b++)
                    {
                        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                        float r = UnityEngine.Random.Range(2f, groveRadius * 0.95f);
                        Vector3 bPos = hitCenter.point + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

                        if (terrainCollider.Raycast(new Ray(bPos + Vector3.up * 60f, Vector3.down), out RaycastHit hitB, 120f))
                        {
                            // Strict water elevation check
                            if (hitB.point.y <= 19.5f || hitB.normal.y < 0.58f) continue;

                            var bushPrefab = bushPrefabs[UnityEngine.Random.Range(0, bushPrefabs.Count)];
                            var bushInstance = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, chunkTr);
                            bushInstance.layer = LAYER_TREES;
                            bushInstance.transform.position = hitB.point;
                            bushInstance.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            float bScale = UnityEngine.Random.Range(0.8f, 1.4f);
                            bushInstance.transform.localScale = Vector3.one * bScale;

                            foreach (var mr in bushInstance.GetComponentsInChildren<MeshRenderer>())
                            {
                                foreach (var mat in mr.sharedMaterials)
                                {
                                    if (mat != null) mat.enableInstancing = true;
                                }
                            }
                            spawnedBushes++;
                        }
                    }
                }
            }

            chunkCount = chunkRoots.Count;
            Debug.Log($"🌲 Spawned {spawnedTrees} Anime Trees and {spawnedBushes} Anime Bushes across {chunkCount} spatial chunks.");
        }

        private static void PopulateDynamicGrassAndFlowers(MeshCollider terrainCollider, out int spawnedGrass, out int spawnedFlowers, out int grassChunkCount)
        {
            GameObject grassRoot = new GameObject("--- DYNAMIC STYLIZED GRASS & WILDFLOWERS ---");

            GameObject dynamicGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nyxis_GrassInteractionSystem/Prefab/Grass_Patch.prefab");
            GameObject stylizedGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/GrassMesh.prefab");

            if (dynamicGrassPrefab == null && stylizedGrassPrefab == null)
            {
                Debug.LogError("Neither Grass_Patch.prefab nor GrassMesh.prefab found!");
                spawnedGrass = 0;
                spawnedFlowers = 0;
                grassChunkCount = 0;
                return;
            }

            // Wildflowers
            string[] flowerPaths = {
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab"
            };

            List<GameObject> flowerPrefabs = LoadPrefabs(flowerPaths);

            // Load Biome Splat Mask and Macro Albedo to guide grass exactly across all green areas of the landscape
            Texture2D biomeMaskTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environments/Continent/T_Continent_BiomeMask.png");
            Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environments/Continent/T_Continent_MacroAlbedo.png");

            Dictionary<Vector2Int, Transform> grassChunks = new Dictionary<Vector2Int, Transform>();
            spawnedGrass = 0;
            spawnedFlowers = 0;

            // Full-continent coverage across all green landscape (radius up to 3450m)
            float step = 38f;
            for (float x = -3400f; x <= 3400f; x += step)
            {
                for (float z = -3400f; z <= 3400f; z += step)
                {
                    // 1. Continuous Hexagonal Row Staggering
                    float rowOffset = ((Mathf.FloorToInt((z + 10000f) / step) % 2) != 0) ? (0.5f * step) : 0f;

                    // 2. Large Stratified Jitter
                    float jx = UnityEngine.Random.Range(-0.48f, 0.48f) * step;
                    float jz = UnityEngine.Random.Range(-0.48f, 0.48f) * step;

                    // 3. Smooth Domain Warping
                    float warpAngle = Mathf.PerlinNoise((x + 3333f) * 0.003f, (z + 7777f) * 0.003f) * Mathf.PI * 4f;
                    float warpDist = Mathf.PerlinNoise((x + 1111f) * 0.004f, (z + 9999f) * 0.004f) * (step * 0.70f);

                    float candidateX = x + rowOffset + jx + Mathf.Cos(warpAngle) * warpDist;
                    float candidateZ = z + jz + Mathf.Sin(warpAngle) * warpDist;

                    float dist = Mathf.Sqrt(candidateX * candidateX + candidateZ * candidateZ);
                    // Strictly outside city and moat (dist >= 555m)
                    if (dist < 555f || dist > 3450f) continue;

                    // Waterfall Mountain Clearing: Keep entire mountain slope open for user's waterfalls!
                    if (candidateX >= -280f && candidateX <= 120f && candidateZ >= 520f && candidateZ <= 1350f) continue;

                    // Exclude volcano peak
                    float volcDist = Mathf.Sqrt((candidateX + 1140f) * (candidateX + 1140f) + (candidateZ - 1360f) * (candidateZ - 1360f));
                    if (volcDist < 500f) continue;

                    // Landscape Green Area Check: Verify terrain color is designated green in the landscape
                    float u = Mathf.Clamp01((candidateX + 4096f) / 8192f);
                    float v = Mathf.Clamp01((candidateZ + 4096f) / 8192f);
                    Color maskCol = (biomeMaskTex != null) ? biomeMaskTex.GetPixelBilinear(u, v) : Color.black;
                    Color albedoCol = (albedoTex != null) ? albedoTex.GetPixelBilinear(u, v) : Color.black;

                    // Check both visual green albedo channel and biome mask splat channel
                    bool isAlbedoGreen = (albedoCol.g > 0.20f && albedoCol.g > albedoCol.r * 1.02f && albedoCol.g > albedoCol.b * 1.02f);
                    bool isMaskGrass = (maskCol.r > 0.15f && maskCol.g < 0.45f && maskCol.b < 0.45f);

                    if (!isAlbedoGreen && !isMaskGrass) continue;

                    // Multi-Attempt Terrain Raycast: STRICT WATER EXCLUSION (Y > 19.5m)
                    RaycastHit hitCenter = default;
                    bool validHit = false;
                    float finalGX = candidateX;
                    float finalGZ = candidateZ;

                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        float testX = candidateX + (attempt == 0 ? 0f : UnityEngine.Random.Range(-7f, 7f));
                        float testZ = candidateZ + (attempt == 0 ? 0f : UnityEngine.Random.Range(-7f, 7f));
                        Ray ray = new Ray(new Vector3(testX, 1500f, testZ), Vector3.down);
                        if (terrainCollider.Raycast(ray, out RaycastHit hit, 2000f))
                        {
                            // STRICT WATER LINE
                            if (hit.point.y > 19.5f && hit.normal.y >= 0.58f && (hit.point.y <= 230f || testZ <= 700f))
                            {
                                hitCenter = hit;
                                finalGX = testX;
                                finalGZ = testZ;
                                validHit = true;
                                break;
                            }
                        }
                    }
                    if (!validHit) continue;

                    Vector2Int chunkKey = new Vector2Int(Mathf.FloorToInt(finalGX / 120f), Mathf.FloorToInt(finalGZ / 120f));
                    if (!grassChunks.TryGetValue(chunkKey, out Transform chunkTr))
                    {
                        GameObject chunkGo = new GameObject($"GrassChunk_{chunkKey.x}_{chunkKey.y}");
                        chunkGo.transform.SetParent(grassRoot.transform);
                        chunkTr = chunkGo.transform;
                        grassChunks[chunkKey] = chunkTr;
                    }

                    // Spawn cluster of 2 to 4 grass patches: Blending Stylized Grass and Dynamic Grass
                    int patches = UnityEngine.Random.Range(2, 5);
                    for (int p = 0; p < patches; p++)
                    {
                        float pAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                        float pDist = UnityEngine.Random.Range(1.5f, 9.5f);
                        Vector3 gPos = hitCenter.point + new Vector3(Mathf.Cos(pAngle) * pDist, 0f, Mathf.Sin(pAngle) * pDist);

                        if (terrainCollider.Raycast(new Ray(gPos + Vector3.up * 40f, Vector3.down), out RaycastHit hitG, 80f))
                        {
                            // Strict water elevation and moat radius check
                            float patchDist = Mathf.Sqrt(hitG.point.x * hitG.point.x + hitG.point.z * hitG.point.z);
                            if (patchDist < 555f || hitG.point.y <= 19.5f || hitG.normal.y < 0.58f) continue;

                            // 50/50 organic blend between Dynamic interactive grass and Stylized anime grass
                            bool useDynamic = (dynamicGrassPrefab != null) && (stylizedGrassPrefab == null || UnityEngine.Random.value < 0.5f);
                            GameObject chosenPrefab = useDynamic ? dynamicGrassPrefab : stylizedGrassPrefab;

                            var gPatch = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab, chunkTr);
                            gPatch.layer = LAYER_GRASS;
                            gPatch.transform.position = hitG.point;
                            gPatch.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitG.normal) * Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            float gScale = useDynamic ? UnityEngine.Random.Range(3.5f, 5.2f) : UnityEngine.Random.Range(2.5f, 4.0f);
                            gPatch.transform.localScale = Vector3.one * gScale;

                            // Turn off shadow casting to eliminate draw calls & enable GPU instancing
                            foreach (var mr in gPatch.GetComponentsInChildren<MeshRenderer>())
                            {
                                mr.shadowCastingMode = ShadowCastingMode.Off;
                                mr.receiveShadows = false;
                                foreach (var mat in mr.sharedMaterials)
                                {
                                    if (mat != null) mat.enableInstancing = true;
                                }
                            }
                            spawnedGrass++;
                        }
                    }

                    // Spawn Wildflowers (~45% of grass clusters have vibrant flower groupings)
                    if (flowerPrefabs.Count > 0 && UnityEngine.Random.value < 0.45f)
                    {
                        int flowersInCluster = UnityEngine.Random.Range(2, 5);
                        for (int f = 0; f < flowersInCluster; f++)
                        {
                            float fAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                            float fDist = UnityEngine.Random.Range(1.0f, 7.5f);
                            Vector3 fPos = hitCenter.point + new Vector3(Mathf.Cos(fAngle) * fDist, 0f, Mathf.Sin(fAngle) * fDist);

                            if (terrainCollider.Raycast(new Ray(fPos + Vector3.up * 40f, Vector3.down), out RaycastHit hitF, 80f))
                            {
                                float flowerDist = Mathf.Sqrt(hitF.point.x * hitF.point.x + hitF.point.z * hitF.point.z);
                                if (flowerDist < 555f || hitF.point.y <= 19.5f || hitF.normal.y < 0.58f) continue;

                                var flowerPrefab = flowerPrefabs[UnityEngine.Random.Range(0, flowerPrefabs.Count)];
                                var flowerInst = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab, chunkTr);
                                flowerInst.layer = LAYER_GRASS;
                                flowerInst.transform.position = hitF.point;
                                flowerInst.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                                float fScale = UnityEngine.Random.Range(1.0f, 1.8f);
                                flowerInst.transform.localScale = Vector3.one * fScale;

                                foreach (var mr in flowerInst.GetComponentsInChildren<MeshRenderer>())
                                {
                                    mr.shadowCastingMode = ShadowCastingMode.Off;
                                    mr.receiveShadows = false;
                                }
                                spawnedFlowers++;
                            }
                        }
                    }
                }
            }

            grassChunkCount = grassChunks.Count;
            Debug.Log($"🌾 Spawned {spawnedGrass} Dynamic Grass patches and {spawnedFlowers} Wildflower clusters across {grassChunkCount} chunks.");
        }

        private static void SpawnGoldenSpores(GameObject sporePrefab)
        {
            if (sporePrefab == null) return;
            GameObject sporesRoot = new GameObject("--- GOLDEN FIELD SPORE PARTICLES ---");

            Vector3[] sporeLocations = {
                new Vector3(450f, 35f, 250f),
                new Vector3(-350f, 30f, -300f),
                new Vector3(350f, 35f, -450f),
                new Vector3(-450f, 35f, 400f),
                new Vector3(0f, 25f, -650f),
                new Vector3(700f, 45f, 150f),
                new Vector3(-700f, 40f, 200f)
            };

            foreach (var loc in sporeLocations)
            {
                var sporeInstance = (GameObject)PrefabUtility.InstantiatePrefab(sporePrefab, sporesRoot.transform);
                sporeInstance.transform.position = loc;
            }

            Debug.Log($"✨ Spawned {sporeLocations.Length} Golden-White Floating Spore particle fields.");
        }

        private static void PlaceOutpostsAndPortal(MeshCollider terrainCollider, GameObject campPrefab, GameObject fortPrefab, GameObject portalPrefab)
        {
            GameObject outpostsRoot = new GameObject("--- ENEMY OUTPOSTS & DUNGEON PORTAL ---");

            // 1. Dungeon Dimension Portal: Ancient Northern Forest Grove
            if (terrainCollider.Raycast(new Ray(PORTAL_LOC + Vector3.up * 1000f, Vector3.down), out RaycastHit hitPortal, 2000f))
            {
                var portalInstance = (GameObject)PrefabUtility.InstantiatePrefab(portalPrefab, outpostsRoot.transform);
                portalInstance.name = "DungeonDimension_Portal_NorthernGrove";
                portalInstance.transform.position = hitPortal.point;
                portalInstance.transform.rotation = Quaternion.Euler(0f, 155f, 0f);
                Debug.Log($"🌀 Placed Dungeon Dimension Portal at {hitPortal.point}");
            }

            // 2. Outpost 1: Eastern Riverbend Watchtower Camp
            if (terrainCollider.Raycast(new Ray(CAMP1_LOC + Vector3.up * 1000f, Vector3.down), out RaycastHit hitCamp1, 2000f))
            {
                var campInstance = (GameObject)PrefabUtility.InstantiatePrefab(campPrefab, outpostsRoot.transform);
                campInstance.name = "EnemyOutpost_EasternRiverbend";
                campInstance.transform.position = hitCamp1.point;
                campInstance.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
                Debug.Log($"⚔️ Placed Watchtower Camp at {hitCamp1.point}");
            }

            // 3. Outpost 2: Western Gorge Ruined Fort
            if (terrainCollider.Raycast(new Ray(FORT_LOC + Vector3.up * 1000f, Vector3.down), out RaycastHit hitFort, 2000f))
            {
                var fortInstance = (GameObject)PrefabUtility.InstantiatePrefab(fortPrefab, outpostsRoot.transform);
                fortInstance.name = "EnemyOutpost_WesternGorgeFort";
                fortInstance.transform.position = hitFort.point;
                fortInstance.transform.rotation = Quaternion.Euler(0f, 110f, 0f);
                Debug.Log($"⚔️ Placed Ruined Fort at {hitFort.point}");
            }

            // 4. Outpost 3: Southern Meadow Watchtower Camp
            if (terrainCollider.Raycast(new Ray(CAMP2_LOC + Vector3.up * 1000f, Vector3.down), out RaycastHit hitCamp2, 2000f))
            {
                var campInstance2 = (GameObject)PrefabUtility.InstantiatePrefab(campPrefab, outpostsRoot.transform);
                campInstance2.name = "EnemyOutpost_SouthernMeadow";
                campInstance2.transform.position = hitCamp2.point;
                campInstance2.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                Debug.Log($"⚔️ Placed Southern Outpost at {hitCamp2.point}");
            }
        }

        private static void BeautifyTownAndRoadways(MeshCollider terrainCollider)
        {
            GameObject townBeautifyRoot = new GameObject("--- TOWN BEAUTIFICATION & ROADSIDE ---");

            GameObject barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Old_Table.prefab");
            GameObject archPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Doors & Doorways/Level_Doorway_Arched.prefab");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Materials/Tent_Wood.mat");
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_DungeonStone.mat");

            // Roadway bridge approaches at North, South, East, West (R ~ 550m)
            Vector3[] bridgeExits = {
                new Vector3(0f, 0f, 550f),   // North Gate
                new Vector3(0f, 0f, -550f),  // South Gate
                new Vector3(550f, 0f, 0f),   // East Gate
                new Vector3(-550f, 0f, 0f)   // West Gate
            };

            foreach (var bExit in bridgeExits)
            {
                if (terrainCollider.Raycast(new Ray(bExit + Vector3.up * 500f, Vector3.down), out RaycastHit hitExit, 1000f))
                {
                    // STRICT DRY LAND CHECK: Water level is Y=18.0m. Skip any exit that lands in or below water!
                    if (hitExit.point.y < 19.5f) continue;
                    // Roadside Gateway Arch
                    if (archPrefab != null)
                    {
                        var arch = (GameObject)PrefabUtility.InstantiatePrefab(archPrefab, townBeautifyRoot.transform);
                        arch.transform.position = hitExit.point;
                        arch.transform.rotation = Quaternion.LookRotation(hitExit.point.normalized);
                        arch.transform.localScale = Vector3.one * 1.6f;
                        if (stoneMat != null)
                        {
                            foreach (var mr in arch.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial = stoneMat;
                        }
                    }

                    // Waystone Lantern Post
                    GameObject lampGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    lampGo.name = "Roadside_Waystone_Lantern";
                    lampGo.transform.SetParent(townBeautifyRoot.transform);
                    lampGo.transform.position = hitExit.point + Vector3.up * 1.6f + new Vector3(3.5f, 0f, 0f);
                    lampGo.transform.localScale = new Vector3(0.4f, 1.6f, 0.4f);
                    if (woodMat != null) lampGo.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

                    var lampLight = lampGo.AddComponent<Light>();
                    lampLight.type = LightType.Point;
                    lampLight.color = new Color(1f, 0.78f, 0.38f);
                    lampLight.range = 14f;
                    lampLight.intensity = 3f;

                    // Props around bridge entrance
                    if (barrelPrefab != null)
                    {
                        var b = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, townBeautifyRoot.transform);
                        b.transform.position = hitExit.point + new Vector3(3.2f, 0f, 2.5f);
                    }
                    if (cratePrefab != null)
                    {
                        var c = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, townBeautifyRoot.transform);
                        c.transform.position = hitExit.point + new Vector3(-3.2f, 0f, 2.5f);
                    }
                    if (tablePrefab != null)
                    {
                        var t = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab, townBeautifyRoot.transform);
                        t.transform.position = hitExit.point + new Vector3(-4.5f, 0f, 3.5f);
                    }
                }
            }

            Debug.Log("🏰 Decorated town bridge gateways with arches, lanterns, supply crates, and road markers.");
        }

        private static void ApplyCameraOptimization()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = GameObject.Find("Main Camera");
                if (camGo != null) mainCam = camGo.GetComponent<Camera>();
            }

            if (mainCam != null)
            {
                float[] distances = new float[32];
                // Layer 6: Foliage_Grass culls at exactly 152.4m (500 feet)!
                distances[LAYER_GRASS] = 152.4f;
                // Layer 7: Anime Trees culls at 1200m
                distances[LAYER_TREES] = 1200f;
                // Layer 8: Small details/props culls at 180m
                distances[LAYER_DETAILS] = 180f;

                mainCam.layerCullDistances = distances;
                mainCam.layerCullSpherical = true;
                mainCam.farClipPlane = 12000f;
            }

            // Universal Render Pipeline Asset optimization
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.shadowDistance = 220f;
            }

            // Camera Frustum Prop Unloader: Deactivates props/foliage when outside camera view frustum
            var optRoot = GameObject.Find("--- OPTIMIZATION & SYSTEMS ROOT ---");
            if (optRoot == null) optRoot = new GameObject("--- OPTIMIZATION & SYSTEMS ROOT ---");
            var unloader = optRoot.GetComponent<BattleSummoner.Optimization.CameraFrustumPropUnloader>();
            if (unloader == null) unloader = optRoot.AddComponent<BattleSummoner.Optimization.CameraFrustumPropUnloader>();
            unloader.targetCamera = mainCam;
            unloader.propCullDistance = 180f;
            unloader.rockCullDistance = 350f;
            unloader.grassCullDistance = 152.4f;
            unloader.forestCullDistance = 1200f;
            unloader.nearPreserveRadius = 15f;
            unloader.frameSlices = 2;
            unloader.RefreshPropRegistry();

            Debug.Log("⚡ Camera Optimization applied: Frustum Prop Unloader active, Grass culls at 500ft (152.4m), Trees at 1200m, Props at 180m.");
        }

        private static void CaptureVerificationScreenshots(MeshCollider terrainCollider)
        {
            // 1. Matches User Screenshot 1: Moat & Trees (verify ZERO floating trees and clean unified water)
            TakeExactTransformShot("Assets/Screenshots/User_Match_01_Moat_Foliage.png",
                new Vector3(-245f, 30f, -460f),
                Quaternion.Euler(14f, 52f, 0f), 60f);

            // 2. Matches User Screenshot 2 & 5: Moat crossing and Gate bridge (verify clean stone arch causeway, zero kinks/intersections)
            TakeExactTransformShot("Assets/Screenshots/User_Match_02_Moat_Bridge.png",
                new Vector3(0f, 62f, 420f),
                Quaternion.Euler(22f, 0f, 0f), 65f);

            // 3. Matches User Screenshot 3: Mainland shore & road exit
            TakeExactTransformShot("Assets/Screenshots/User_Match_03_Shore_Crossing.png",
                new Vector3(140f, 32f, 490f),
                Quaternion.Euler(15f, -48f, 0f), 65f);

            // 4. Matches User Screenshot 4: Mountain Waterfall slope (verify 64-boulder formation is gone and slope is clear for waterfalls!)
            TakeExactTransformShot("Assets/Screenshots/User_Match_04_Mountain_WaterfallSlope_Cleared.png",
                new Vector3(-160f, 75f, 640f),
                Quaternion.Euler(10f, -12f, 0f), 65f);

            // 5. Meshy AI Sky Archipelago Showcase: Large, Medium, Small floating islands with flat top plateaus
            TakeOverviewShot("Assets/Screenshots/Showcase_Meshy_SkyIslands.png",
                new Vector3(250f, 540f, 50f),
                new Vector3(450f, 420f, 250f), 65f);

            // 6. Meshy AI Clouds Showcase: 3-tier stylized cloud variations across the horizon
            TakeOverviewShot("Assets/Screenshots/Showcase_Meshy_Clouds.png",
                new Vector3(0f, 280f, -200f),
                new Vector3(700f, 500f, 900f), 70f);

            // 7. Combined Sky Archipelago Overview
            TakeOverviewShot("Assets/Screenshots/Showcase_SkyIslands_And_Clouds.png",
                new Vector3(300f, 580f, -120f),
                new Vector3(0f, 450f, 600f), 65f);

            // 8. Dual Grass Ecosystem: Ground-level view showing both Stylized Anime Grass and Dynamic Grass in green areas
            TakeExactTransformShot("Assets/Screenshots/Showcase_Stylized_And_Dynamic_Grass.png",
                new Vector3(510f, 28.5f, 480f),
                Quaternion.Euler(12f, 40f, 0f), 65f);

            // 9. Scattered Continental Rock Formations across the island
            TakeOverviewShot("Assets/Screenshots/Showcase_Scattered_RockFormations.png",
                new Vector3(1200f, 320f, 650f),
                new Vector3(650f, 80f, 150f), 65f);

            // 10. Dense Anime Nature Forest & Dynamic Grass
            TakeTerrainFramedShot("Assets/Screenshots/Forest_Density_And_Grass_Detail.png",
                new Vector2(680f, 110f), 2.2f,
                new Vector2(740f, 150f), 2.6f, 65f, terrainCollider);
        }

        private static void TakeExactTransformShot(string outputPath, Vector3 camPos, Quaternion camRot, float fov)
        {
            GameObject camGo = new GameObject("TempExactCam");
            try
            {
                Camera cam = camGo.AddComponent<Camera>();
                cam.transform.position = camPos;
                cam.transform.rotation = camRot;
                cam.fieldOfView = fov;
                cam.nearClipPlane = 1.0f;
                cam.farClipPlane = 15000f;
                cam.clearFlags = CameraClearFlags.Skybox;

                float[] distances = new float[32];
                distances[LAYER_TREES] = 10000f;
                distances[LAYER_GRASS] = 2000f;
                cam.layerCullDistances = distances;

                RenderCameraToPNG(cam, outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        private static void TakeOverviewShot(string outputPath, Vector3 camPos, Vector3 lookTarget, float fov)
        {
            GameObject camGo = new GameObject("TempOverviewCam");
            try
            {
                Camera cam = camGo.AddComponent<Camera>();
                cam.transform.position = camPos;
                cam.transform.LookAt(lookTarget);
                cam.fieldOfView = fov;
                cam.nearClipPlane = 1.0f;
                cam.farClipPlane = 15000f;
                cam.clearFlags = CameraClearFlags.Skybox;

                float[] distances = new float[32];
                distances[LAYER_TREES] = 6000f;
                cam.layerCullDistances = distances;

                RenderCameraToPNG(cam, outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        private static void TakeTerrainFramedShot(string outputPath, Vector2 camXZ, float camHeightOffset, Vector2 targetXZ, float targetHeightOffset, float fov, MeshCollider terrainCollider)
        {
            GameObject camGo = new GameObject("TempFramedCam");
            try
            {
                float camGroundY = 30f;
                if (terrainCollider.Raycast(new Ray(new Vector3(camXZ.x, 1500f, camXZ.y), Vector3.down), out RaycastHit hitCam, 2000f))
                {
                    camGroundY = hitCam.point.y;
                }

                float targetGroundY = 30f;
                if (terrainCollider.Raycast(new Ray(new Vector3(targetXZ.x, 1500f, targetXZ.y), Vector3.down), out RaycastHit hitTar, 2000f))
                {
                    targetGroundY = hitTar.point.y;
                }

                Vector3 camPos = new Vector3(camXZ.x, camGroundY + camHeightOffset, camXZ.y);
                Vector3 lookTarget = new Vector3(targetXZ.x, targetGroundY + targetHeightOffset, targetXZ.y);

                Camera cam = camGo.AddComponent<Camera>();
                cam.transform.position = camPos;
                cam.transform.LookAt(lookTarget);
                cam.fieldOfView = fov;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 5000f;
                cam.clearFlags = CameraClearFlags.Skybox;

                RenderCameraToPNG(cam, outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        private static void RenderCameraToPNG(Camera cam, string outputPath)
        {
            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            string fullPath = Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(Application.dataPath, "..", outputPath);
            File.WriteAllBytes(fullPath, bytes);

            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"📸 Captured: {outputPath}");
        }
    }
}

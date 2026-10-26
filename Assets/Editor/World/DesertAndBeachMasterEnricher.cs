using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.WorldActivities;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Master Desert and Beach Enricher & Interaction Pipeline.
    /// Overhauls the boring/empty deserts and beaches into rich, living, interactive adventure zones:
    /// 
    /// 1. SW DESERT FLORA & VEGETATION:
    ///    - Towering Saguaros & desert cacti clusters (PP_Cactus_04)
    ///    - Radiant Sun-Fire Agaves & Miragedew Succulents (Plant_1_Big, Plant_7_Big)
    ///    - Arid twisted desert trees & sun-bleached desert driftwood (TwistedTree_2, DeadTree_1)
    ///    - Hardy desert scrub bushes and dry brush (Bush_01)
    /// 
    /// 2. SW DESERT GAMEPLAY INTERACTIONS:
    ///    - Harvestable Sun-Fire Agave & Miragedew nodes yielding alchemical reagents & mana/hp
    ///    - Half-buried ancient Sandstone Relic Urns with golden amber loot glow
    ///    - Resonant Sun-Stone Obelisks granting the "Solar Strider" explorer blessing
    /// 
    /// 3. COASTAL BEACH FLORA & PROPS:
    ///    - Leaning coastal date palms angled toward the ocean surf (Tree_07)
    ///    - Weathered sea-driftwood logs and stumps (DeadTree_1)
    ///    - Wispy sea-oats and coastal sand grass (Grass_Wispy_Short)
    /// 
    /// 4. COASTAL BEACH GAMEPLAY INTERACTIONS:
    ///    - Gatherable Pearlescent Shell beds yielding luminous sea pearls
    ///    - Washed-up Flotsam Cargo Crates & driftwood barrels with salvageable maritime goods
    ///    - Sparkling Tidal Geodes yielding oceanic quartz & azure mana prisms
    /// 
    /// Strictly snaps all elements to CircularContinent_MacroLevel1 (Y >= 18.5m above water level 18.0m).
    /// </summary>
    public static class DesertAndBeachMasterEnricher
    {
        private const string ROOT_NAME = "--- DESERT & BEACH ECOSYSTEMS AND INTERACTIONS ---";
        private const string SCENE_PATH = "Assets/Scenes/OverworldMap.unity";

        // Model & Prefab Paths
        private const string CACTUS_PATH = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Cactus_04.prefab";
        private const string PLANT_AGAVE_BIG_PATH = "Assets/Models/QuaterniusMegaKits/StylizedNatureMegaKit/FBX (Unity)/Plant_1_Big.fbx";
        private const string PLANT_SUCCULENT_BIG_PATH = "Assets/Models/QuaterniusMegaKits/StylizedNatureMegaKit/FBX (Unity)/Plant_7_Big.fbx";
        private const string PLANT_AGAVE_PATH = "Assets/Models/QuaterniusMegaKits/StylizedNatureMegaKit/FBX (Unity)/Plant_1.fbx";
        private const string PLANT_SUCCULENT_PATH = "Assets/Models/QuaterniusMegaKits/StylizedNatureMegaKit/FBX (Unity)/Plant_7.fbx";
        private const string DEAD_TREE_PATH = "Assets/Models/QuaterniusMegaKits/StylizedNatureMegaKit/FBX (Unity)/DeadTree_1.fbx";
        private const string TWISTED_TREE_PATH = "Assets/Models/QuaterniusMegaKits/StylizedNatureMegaKit/FBX (Unity)/TwistedTree_2.fbx";
        private const string BEACH_GRASS_PATH = "Assets/Models/QuaterniusMegaKits/StylizedNatureMegaKit/FBX (Unity)/Grass_Wispy_Short.fbx";
        private const string PALM_TREE_PATH = "Assets/Anime Trees/Prefabs/Tree_07.prefab";
        private const string DESERT_BUSH_PATH = "Assets/Anime Trees/Prefabs/Bush_01.prefab";

        private const string URN_JAR_PATH = "Assets/Free Simple Dungeon Props - YP/Prefabs/Jar.prefab";
        private const string URN_POT_PATH = "Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Props/Pot_Large.prefab";
        private const string CRATE_PATH = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Crate_Wooden_03.prefab";
        private const string BARREL_PATH = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Barrel_03.prefab";
        private const string GEODE_CRYSTAL_PATH = "Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Stone_Crystal_02_Gold.prefab";
        private const string MANA_CRYSTAL_PATH = "Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx";
        private const string STARS_VFX_PATH = "Assets/PurePoly/Mining_Free_Assets/Prefabs/FX/FX_Stars_01.prefab";

        // Stylized Materials Directory
        private const string MAT_DIR = "Assets/Materials/Environment";

        [MenuItem("BattleSummoner/World Refinement/Enrich Deserts and Beaches (Flora & Interactions)", false, 2)]
        public static void EnrichDesertsAndBeaches()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH)
            {
                activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            var continentObj = GameObject.Find("CircularContinent_MacroLevel1") ?? GameObject.Find("Circular_Continent_Terrain");
            if (continentObj == null)
            {
                Debug.LogError("[Desert & Beach Enricher] CircularContinent_MacroLevel1 not found in scene!");
                return;
            }

            var terrainCollider = continentObj.GetComponent<MeshCollider>();
            if (terrainCollider == null)
            {
                Debug.LogError("[Desert & Beach Enricher] MeshCollider missing on terrain!");
                return;
            }

            // Remove previous root if present
            var existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            var rootObj = new GameObject(ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(rootObj, "Enrich Deserts and Beaches");

            var desertFloraTr = new GameObject("01_SW_Desert_Flora_And_Vegetation").transform;
            desertFloraTr.SetParent(rootObj.transform, false);

            var desertInteractTr = new GameObject("02_SW_Desert_Interactive_Nodes").transform;
            desertInteractTr.SetParent(rootObj.transform, false);

            var beachFloraTr = new GameObject("03_Coastal_Beach_Flora_And_Driftwood").transform;
            beachFloraTr.SetParent(rootObj.transform, false);

            var beachInteractTr = new GameObject("04_Coastal_Beach_Interactive_Nodes").transform;
            beachInteractTr.SetParent(rootObj.transform, false);

            // Load / Create stylized materials
            var matAgave = GetOrCreateStylizedMaterial("M_Desert_SunFireAgave", new Color(0.98f, 0.65f, 0.20f), 0.15f, new Color(0.95f, 0.45f, 0.10f) * 0.45f);
            var matSucculent = GetOrCreateStylizedMaterial("M_Desert_MiragedewSucculent", new Color(0.35f, 0.88f, 0.82f), 0.35f, new Color(0.20f, 0.75f, 0.80f) * 0.38f);
            var matCactus = GetOrCreateStylizedMaterial("M_Desert_SolarCactus", new Color(0.42f, 0.72f, 0.32f), 0.20f);
            var matUrn = GetOrCreateStylizedMaterial("M_Desert_SandstoneUrn", new Color(0.85f, 0.68f, 0.45f), 0.12f, new Color(1.0f, 0.75f, 0.20f) * 0.25f);
            var matObelisk = GetOrCreateStylizedMaterial("M_Desert_SunStoneObelisk", new Color(0.32f, 0.30f, 0.34f), 0.15f, new Color(1.0f, 0.82f, 0.30f) * 0.65f);
            var matShell = GetOrCreateStylizedMaterial("M_Beach_PearlescentShell", new Color(0.95f, 0.92f, 0.98f), 0.85f, new Color(0.70f, 0.90f, 1.0f) * 0.35f);
            var matDriftwood = GetOrCreateStylizedMaterial("M_Beach_ShoreDriftwood", new Color(0.68f, 0.65f, 0.60f), 0.10f);
            var matGeode = GetOrCreateStylizedMaterial("M_Beach_TidalGeode", new Color(0.25f, 0.75f, 0.95f), 0.65f, new Color(0.20f, 0.80f, 1.0f) * 0.55f);

            // Load prefabs & models
            var pfbCactus = AssetDatabase.LoadAssetAtPath<GameObject>(CACTUS_PATH);
            var pfbAgaveBig = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_AGAVE_BIG_PATH);
            var pfbSucculentBig = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_SUCCULENT_BIG_PATH);
            var pfbAgave = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_AGAVE_PATH);
            var pfbSucculent = AssetDatabase.LoadAssetAtPath<GameObject>(PLANT_SUCCULENT_PATH);
            var pfbDeadTree = AssetDatabase.LoadAssetAtPath<GameObject>(DEAD_TREE_PATH);
            var pfbTwistedTree = AssetDatabase.LoadAssetAtPath<GameObject>(TWISTED_TREE_PATH);
            var pfbBeachGrass = AssetDatabase.LoadAssetAtPath<GameObject>(BEACH_GRASS_PATH);
            var pfbPalm = AssetDatabase.LoadAssetAtPath<GameObject>(PALM_TREE_PATH);
            var pfbBush = AssetDatabase.LoadAssetAtPath<GameObject>(DESERT_BUSH_PATH);

            var pfbJar = AssetDatabase.LoadAssetAtPath<GameObject>(URN_JAR_PATH);
            var pfbPot = AssetDatabase.LoadAssetAtPath<GameObject>(URN_POT_PATH);
            var pfbCrate = AssetDatabase.LoadAssetAtPath<GameObject>(CRATE_PATH);
            var pfbBarrel = AssetDatabase.LoadAssetAtPath<GameObject>(BARREL_PATH);
            var pfbGeode = AssetDatabase.LoadAssetAtPath<GameObject>(GEODE_CRYSTAL_PATH);
            var pfbManaCrystal = AssetDatabase.LoadAssetAtPath<GameObject>(MANA_CRYSTAL_PATH);
            var pfbStarsVFX = AssetDatabase.LoadAssetAtPath<GameObject>(STARS_VFX_PATH);

            UnityEngine.Random.InitState(77319);

            Debug.Log("🌵 [Enricher] Building Southwest Desert Flora & Interactions...");
            PurgeStrayWoodlandFoliageFromDesert(out int purgedWoodlandTrees);
            int desertPlantsCount = 0;
            int desertInteractsCount = 0;

            // -----------------------------------------------------------------
            // PART 1 & 2: SOUTHWEST DESERT (X: -1350 to -350, Z: -950 to -220)
            // -----------------------------------------------------------------
            float desertXMin = -1350f, desertXMax = -350f;
            float desertZMin = -950f, desertZMax = -220f;
            float desertStep = 32f;

            for (float x = desertXMin; x <= desertXMax; x += desertStep)
            {
                for (float z = desertZMin; z <= desertZMax; z += desertStep)
                {
                    float jx = UnityEngine.Random.Range(-12f, 12f);
                    float jz = UnityEngine.Random.Range(-12f, 12f);
                    float targetX = x + jx;
                    float targetZ = z + jz;

                    // Avoid city center moat radius
                    float r = Mathf.Sqrt(targetX * targetX + targetZ * targetZ);
                    if (r < 475f) continue;

                    Ray ray = new Ray(new Vector3(targetX, 450f, targetZ), Vector3.down);
                    if (!terrainCollider.Raycast(ray, out RaycastHit hit, 600f)) continue;

                    // Dry sand elevation check (Y >= 18.8m)
                    if (hit.point.y < 18.8f) continue;
                    // Dunes/flats slope check
                    if (hit.normal.y < 0.40f) continue;

                    float roll = UnityEngine.Random.value;

                    // A. Interactive Buried Urn (Rare, 6% chance)
                    if (roll < 0.065f && (pfbJar != null || pfbPot != null))
                    {
                        var urnPrefab = (UnityEngine.Random.value < 0.5f && pfbJar != null) ? pfbJar : pfbPot;
                        var urnObj = (GameObject)PrefabUtility.InstantiatePrefab(urnPrefab, desertInteractTr);
                        urnObj.name = $"Buried_Sandstone_Urn_{desertInteractsCount + 1}";
                        // Half bury into dune sand
                        urnObj.transform.position = hit.point + Vector3.down * 0.22f;
                        urnObj.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-12f, 12f));
                        urnObj.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.3f, 1.8f);

                        ApplyMaterialRecursive(urnObj, matUrn);
                        EnsureCollider(urnObj);

                        var interactable = urnObj.AddComponent<DesertAndBeachInteractable>();
                        interactable.kind = DesertBeachInteractableKind.BuriedSandstoneUrn;
                        interactable.displayName = "Ancient Sandstone Relic Urn";
                        interactable.itemId = "relic_desert_sandstone_urn";
                        interactable.yieldAmount = 1;
                        interactable.goldValue = UnityEngine.Random.Range(45, 120);
                        interactable.itemDescription = "Ancient terracotta urn unearthed from desert sand. Filled with sunstone shards and minted old-world coins.";

                        desertInteractsCount++;
                    }
                    // B. Interactive Sun-Fire Agave or Miragedew Succulent (15% chance)
                    else if (roll < 0.22f && (pfbAgaveBig != null || pfbSucculentBig != null))
                    {
                        bool isAgave = (UnityEngine.Random.value < 0.60f);
                        var plantPfb = isAgave ? (pfbAgaveBig ?? pfbAgave) : (pfbSucculentBig ?? pfbSucculent);
                        if (plantPfb != null)
                        {
                            var plantObj = (GameObject)PrefabUtility.InstantiatePrefab(plantPfb, desertInteractTr);
                            plantObj.name = isAgave ? $"Harvestable_SunFire_Agave_{desertInteractsCount + 1}" : $"Harvestable_Miragedew_Succulent_{desertInteractsCount + 1}";
                            plantObj.transform.position = hit.point;
                            plantObj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            plantObj.transform.localScale = Vector3.one * (isAgave ? UnityEngine.Random.Range(1.2f, 1.7f) : UnityEngine.Random.Range(1.4f, 2.0f));

                            ApplyMaterialRecursive(plantObj, isAgave ? matAgave : matSucculent);
                            EnsureCollider(plantObj);

                            var interactable = plantObj.AddComponent<DesertAndBeachInteractable>();
                            interactable.kind = isAgave ? DesertBeachInteractableKind.DesertSunAgave : DesertBeachInteractableKind.MiragedewSucculent;
                            interactable.displayName = isAgave ? "Sun-Fire Agave" : "Miragedew Succulent";
                            interactable.itemId = isAgave ? "mat_desert_sun_fire_leaf" : "mat_miragedew_nectar";
                            interactable.yieldAmount = UnityEngine.Random.Range(2, 4);
                            interactable.goldValue = isAgave ? 35 : 55;
                            interactable.itemDescription = isAgave
                                ? "Radiant golden succulent storing solar aether. Restores MP and crafts high-tier fire tonics."
                                : "Rare crystalline desert succulent condensing morning mirage dew. Potent alchemical elixir catalyst.";

                            desertInteractsCount++;
                        }
                    }
                    // C. Desert Saguaros & Cacti Clusters (30% chance)
                    else if (roll < 0.52f && pfbCactus != null)
                    {
                        int clusterSize = UnityEngine.Random.Range(1, 4);
                        for (int c = 0; c < clusterSize; c++)
                        {
                            Vector3 cOffset = (c == 0) ? Vector3.zero : new Vector3(UnityEngine.Random.Range(-4f, 4f), 0f, UnityEngine.Random.Range(-4f, 4f));
                            Ray cRay = new Ray(hit.point + cOffset + Vector3.up * 20f, Vector3.down);
                            if (terrainCollider.Raycast(cRay, out RaycastHit cHit, 40f) && cHit.point.y >= 18.8f)
                            {
                                var cactus = (GameObject)PrefabUtility.InstantiatePrefab(pfbCactus, desertFloraTr);
                                cactus.name = $"Desert_Cactus_Saguaro_{desertPlantsCount + 1}";
                                cactus.transform.position = cHit.point;
                                cactus.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                                cactus.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.0f, 1.65f);

                                // 30% of cacti are interactable harvestable nodes (Prickly Pear / Solar Sap)
                                if (c == 0 && UnityEngine.Random.value < 0.35f)
                                {
                                    EnsureCollider(cactus);
                                    var interactable = cactus.AddComponent<DesertAndBeachInteractable>();
                                    interactable.kind = DesertBeachInteractableKind.DesertSolarCactus;
                                    interactable.displayName = "Solar Saguaro Cactus";
                                    interactable.itemId = "food_prickly_pear_fruit";
                                    interactable.yieldAmount = UnityEngine.Random.Range(2, 5);
                                    interactable.goldValue = 25;
                                    interactable.itemDescription = "Succulent red prickly pear fruit and purifying cactus water. Restores HP and provides desert stamina.";
                                    desertInteractsCount++;
                                }

                                desertPlantsCount++;
                            }
                        }
                    }
                    // D. Arid Weathered Trees & Sun-Bleached Driftwood (20% chance)
                    else if (roll < 0.72f && (pfbTwistedTree != null || pfbDeadTree != null))
                    {
                        var treePrefab = (UnityEngine.Random.value < 0.5f && pfbTwistedTree != null) ? pfbTwistedTree : pfbDeadTree;
                        var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, desertFloraTr);
                        tree.name = $"Desert_Weathered_ThornTree_{desertPlantsCount + 1}";
                        tree.transform.position = hit.point;
                        tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 1.9f);

                        ApplyMaterialRecursive(tree, matDriftwood);
                        desertPlantsCount++;
                    }
                    // E. Desert Scrub Bushes & Dry Brush (25% chance)
                    else if (pfbBush != null)
                    {
                        var bush = (GameObject)PrefabUtility.InstantiatePrefab(pfbBush, desertFloraTr);
                        bush.name = $"Desert_Scrub_Bush_{desertPlantsCount + 1}";
                        bush.transform.position = hit.point;
                        bush.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        bush.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.85f, 1.4f);
                        desertPlantsCount++;
                    }
                }
            }

            // F. Spawn 4 Monumental Ancient Sun-Stone Obelisks across the desert
            Vector3[] obeliskLocations = new Vector3[]
            {
                new Vector3(-550f, 25f, -380f),
                new Vector3(-880f, 38f, -480f),
                new Vector3(-1180f, 152f, -710f), // Near SunScree Oasis
                new Vector3(-740f, 22f, -760f)
            };

            for (int i = 0; i < obeliskLocations.Length; i++)
            {
                Ray oRay = new Ray(obeliskLocations[i] + Vector3.up * 80f, Vector3.down);
                if (terrainCollider.Raycast(oRay, out RaycastHit oHit, 160f) && oHit.point.y >= 18.8f)
                {
                    var obeliskGo = BuildAncientSunStoneObelisk(oHit.point, desertInteractTr, i + 1, matObelisk);
                    desertInteractsCount++;
                }
            }

            Debug.Log($"🌵 [Desert Complete] Populated {desertPlantsCount} desert plants/trees and {desertInteractsCount} interactive desert nodes!");

            // -----------------------------------------------------------------
            // PART 3 & 4: COASTAL BEACHES & SHORELINE (Y: 18.5m to 24.5m)
            // -----------------------------------------------------------------
            Debug.Log("🏖️ [Enricher] Building Coastal Beach Flora, Flotsam & Interactions...");
            int beachFloraCount = 0;
            int beachInteractsCount = 0;

            // Sample along coastal radii (R = 1200m to 2500m) and southern coast bays
            float minCoastRadius = 1150f;
            float maxCoastRadius = 2450f;
            float radialStep = 45f;

            for (float radius = minCoastRadius; radius <= maxCoastRadius; radius += radialStep)
            {
                int angularSlices = Mathf.CeilToInt((2f * Mathf.PI * radius) / 55f);
                float angleStep = 360f / angularSlices;

                for (int a = 0; a < angularSlices; a++)
                {
                    float angleDeg = a * angleStep + UnityEngine.Random.Range(-angleStep * 0.35f, angleStep * 0.35f);
                    float rad = angleDeg * Mathf.Deg2Rad;

                    float targetX = Mathf.Cos(rad) * radius;
                    float targetZ = Mathf.Sin(rad) * radius;

                    Ray bRay = new Ray(new Vector3(targetX, 250f, targetZ), Vector3.down);
                    if (!terrainCollider.Raycast(bRay, out RaycastHit bHit, 350f)) continue;

                    // STRICT COASTAL BEACH ELEVATION (18.5m to 24.5m)
                    if (bHit.point.y < 18.5f || bHit.point.y > 24.5f) continue;
                    // Gentle beach slope check
                    if (bHit.normal.y < 0.55f) continue;

                    float roll = UnityEngine.Random.value;

                    // A. Interactive Pearlescent Shell Bed (18% chance)
                    if (roll < 0.18f)
                    {
                        var shellGo = BuildPearlescentShellBed(bHit.point, beachInteractTr, beachInteractsCount + 1, matShell);
                        beachInteractsCount++;
                    }
                    // B. Interactive Washed-Up Cargo Flotsam (14% chance)
                    else if (roll < 0.32f && (pfbCrate != null || pfbBarrel != null))
                    {
                        bool isCrate = (UnityEngine.Random.value < 0.60f && pfbCrate != null);
                        var flotsamPfb = isCrate ? pfbCrate : (pfbBarrel ?? pfbCrate);
                        var flotsam = (GameObject)PrefabUtility.InstantiatePrefab(flotsamPfb, beachInteractTr);
                        flotsam.name = $"Flotsam_Cargo_Crate_{beachInteractsCount + 1}";
                        // Nestled into wet tide sand
                        flotsam.transform.position = bHit.point + Vector3.down * 0.10f;
                        flotsam.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-10f, 10f));
                        flotsam.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 1.5f);

                        EnsureCollider(flotsam);

                        var interactable = flotsam.AddComponent<DesertAndBeachInteractable>();
                        interactable.kind = DesertBeachInteractableKind.FlotsamCargoCrate;
                        interactable.displayName = "Washed-Up Cargo Flotsam";
                        interactable.itemId = "salvage_maritime_cargo_crate";
                        interactable.yieldAmount = 1;
                        interactable.goldValue = UnityEngine.Random.Range(30, 85);
                        interactable.itemDescription = "Sturdy wooden merchant crate washed ashore by the tides. Contains salvaged nautical timber, iron rivets, and trade coins.";

                        beachInteractsCount++;
                    }
                    // C. Interactive Tidal Geode (10% chance)
                    else if (roll < 0.42f && (pfbGeode != null || pfbManaCrystal != null))
                    {
                        var geodePfb = (pfbGeode != null) ? pfbGeode : pfbManaCrystal;
                        var geode = (GameObject)PrefabUtility.InstantiatePrefab(geodePfb, beachInteractTr);
                        geode.name = $"Tidal_Quartz_Geode_{beachInteractsCount + 1}";
                        geode.transform.position = bHit.point;
                        geode.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-15f, 15f));
                        geode.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 1.8f);

                        ApplyMaterialRecursive(geode, matGeode);
                        EnsureCollider(geode);

                        var interactable = geode.AddComponent<DesertAndBeachInteractable>();
                        interactable.kind = DesertBeachInteractableKind.TidalGeode;
                        interactable.displayName = "Tidal Quartz Geode";
                        interactable.itemId = "gem_tidal_quartz_prism";
                        interactable.yieldAmount = UnityEngine.Random.Range(1, 3);
                        interactable.goldValue = 60;
                        interactable.itemDescription = "Crystalline geode mineralized by saltwater tides and lunar pulls. Yields pristine oceanic mana prisms.";

                        beachInteractsCount++;
                    }
                    // D. Leaning Coastal Palm Trees (22% chance)
                    else if (roll < 0.64f && pfbPalm != null)
                    {
                        var palm = (GameObject)PrefabUtility.InstantiatePrefab(pfbPalm, beachFloraTr);
                        palm.name = $"Beach_Coastal_Palm_{beachFloraCount + 1}";
                        palm.transform.position = bHit.point;

                        // Lean gently toward the sea (away from origin)
                        Vector3 outwardDir = new Vector3(targetX, 0f, targetZ).normalized;
                        Quaternion yaw = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        Quaternion lean = Quaternion.AngleAxis(UnityEngine.Random.Range(12f, 26f), Vector3.Cross(Vector3.up, outwardDir));
                        palm.transform.rotation = lean * yaw;
                        palm.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.15f, 1.6f);

                        beachFloraCount++;
                    }
                    // E. Shoreline Driftwood Trunks & Logs (16% chance)
                    else if (roll < 0.80f && pfbDeadTree != null)
                    {
                        var driftwood = (GameObject)PrefabUtility.InstantiatePrefab(pfbDeadTree, beachFloraTr);
                        driftwood.name = $"Beach_Driftwood_Trunk_{beachFloraCount + 1}";
                        driftwood.transform.position = bHit.point + Vector3.down * 0.15f;
                        driftwood.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-15f, 15f));
                        driftwood.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.9f, 1.4f);

                        ApplyMaterialRecursive(driftwood, matDriftwood);
                        beachFloraCount++;
                    }
                    // F. Wispy Coastal Sand Grass / Sea Oats (20% chance)
                    else if (pfbBeachGrass != null)
                    {
                        var grass = (GameObject)PrefabUtility.InstantiatePrefab(pfbBeachGrass, beachFloraTr);
                        grass.name = $"Beach_Wispy_SandGrass_{beachFloraCount + 1}";
                        grass.transform.position = bHit.point;
                        grass.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        grass.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 1.8f);

                        beachFloraCount++;
                    }
                }
            }

            Debug.Log($"🏖️ [Beach Complete] Populated {beachFloraCount} coastal palms/driftwood/grass and {beachInteractsCount} interactive beach nodes!");

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"🎉 [DESERT & BEACH MASTER SUCCESS] Created comprehensive living ecosystems: {desertPlantsCount + beachFloraCount} flora/props & {desertInteractsCount + beachInteractsCount} rich interactive nodes!");
        }

        private static GameObject BuildAncientSunStoneObelisk(Vector3 pos, Transform parent, int index, Material matObelisk)
        {
            var obeliskRoot = new GameObject($"SunStone_Ancient_Obelisk_{index}");
            obeliskRoot.transform.SetParent(parent, false);
            obeliskRoot.transform.position = pos;

            // Central Monolith Pillar
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Pillar_Monolith";
            pillar.transform.SetParent(obeliskRoot.transform, false);
            pillar.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            pillar.transform.localScale = new Vector3(1.3f, 5.5f, 1.3f);
            pillar.GetComponent<MeshRenderer>().sharedMaterial = matObelisk;

            // Cap pyramid / gem
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Obelisk_Capstone";
            cap.transform.SetParent(obeliskRoot.transform, false);
            cap.transform.localPosition = new Vector3(0f, 5.8f, 0f);
            cap.transform.localScale = new Vector3(1.4f, 0.4f, 1.4f);
            cap.GetComponent<MeshRenderer>().sharedMaterial = matObelisk;

            // Stepped Pedestal Base
            var base1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            base1.name = "Pedestal_Base_Lower";
            base1.transform.SetParent(obeliskRoot.transform, false);
            base1.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            base1.transform.localScale = new Vector3(3.2f, 0.4f, 3.2f);
            base1.GetComponent<MeshRenderer>().sharedMaterial = matObelisk;

            var base2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            base2.name = "Pedestal_Base_Upper";
            base2.transform.SetParent(obeliskRoot.transform, false);
            base2.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            base2.transform.localScale = new Vector3(2.4f, 0.35f, 2.4f);
            base2.GetComponent<MeshRenderer>().sharedMaterial = matObelisk;

            // Solar Core Beacon Light
            var beaconLight = obeliskRoot.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(1.0f, 0.85f, 0.40f);
            beaconLight.range = 18f;
            beaconLight.intensity = 3.5f;

            // Interaction component
            var interactable = obeliskRoot.AddComponent<DesertAndBeachInteractable>();
            interactable.kind = DesertBeachInteractableKind.SunStoneObelisk;
            interactable.displayName = "Ancient Sun-Stone Obelisk";
            interactable.itemId = "relic_sunstone_astral_fragment";
            interactable.yieldAmount = 1;
            interactable.goldValue = 150;
            interactable.interactionDistance = 4.5f;
            interactable.itemDescription = "Ancient monolith carved by the First Sun Scribes. Resonating grants the Solar Strider explorer blessing, boosting sprint speed.";

            return obeliskRoot;
        }

        private static GameObject BuildPearlescentShellBed(Vector3 pos, Transform parent, int index, Material matShell)
        {
            var shellRoot = new GameObject($"Pearlescent_Shell_Bed_{index}");
            shellRoot.transform.SetParent(parent, false);
            shellRoot.transform.position = pos;

            // Multi-clam cluster
            int clamCount = UnityEngine.Random.Range(2, 5);
            for (int i = 0; i < clamCount; i++)
            {
                var clam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                clam.name = $"Clam_{i + 1}";
                clam.transform.SetParent(shellRoot.transform, false);
                Vector3 offset = (i == 0) ? Vector3.zero : new Vector3(UnityEngine.Random.Range(-0.45f, 0.45f), 0f, UnityEngine.Random.Range(-0.45f, 0.45f));
                clam.transform.localPosition = offset + Vector3.up * 0.08f;
                // Flatten to organic clam / shell form
                clam.transform.localScale = new Vector3(UnityEngine.Random.Range(0.55f, 0.85f), UnityEngine.Random.Range(0.18f, 0.28f), UnityEngine.Random.Range(0.45f, 0.70f));
                clam.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-10f, 10f));
                clam.GetComponent<MeshRenderer>().sharedMaterial = matShell;
            }

            var col = shellRoot.AddComponent<SphereCollider>();
            col.radius = 0.8f;
            col.center = Vector3.up * 0.2f;

            var interactable = shellRoot.AddComponent<DesertAndBeachInteractable>();
            interactable.kind = DesertBeachInteractableKind.BeachPearlescentShell;
            interactable.displayName = "Pearlescent Sea Shell";
            interactable.itemId = "mat_luminous_sea_pearl";
            interactable.yieldAmount = UnityEngine.Random.Range(1, 4);
            interactable.goldValue = 40;
            interactable.itemDescription = "Lustrous sea pearl and iridescent mother-of-pearl clam shell gathered from the tidal surf. Essential for water alchemy and jewelry.";

            return shellRoot;
        }

        private static Material GetOrCreateStylizedMaterial(string matName, Color albedoColor, float smoothness, Color? emissionColor = null)
        {
            string fullPath = $"{MAT_DIR}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                mat.name = matName;
                mat.SetColor("_BaseColor", albedoColor);
                mat.SetFloat("_Smoothness", smoothness);

                if (emissionColor.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor.Value);
                }

                if (!Directory.Exists(MAT_DIR)) Directory.CreateDirectory(MAT_DIR);
                AssetDatabase.CreateAsset(mat, fullPath);
            }
            return mat;
        }

        private static void ApplyMaterialRecursive(GameObject go, Material mat)
        {
            if (go == null || mat == null) return;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterial = mat;
            }
        }

        private static void EnsureCollider(GameObject go)
        {
            if (go == null) return;
            if (go.GetComponent<Collider>() != null || go.GetComponentInChildren<Collider>() != null) return;

            var mr = go.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = mr.bounds.center - go.transform.position;
                box.size = mr.bounds.size;
            }
            else
            {
                var sph = go.AddComponent<SphereCollider>();
                sph.radius = 1.0f;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Environment;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Master World Refinement Director
    /// Executes all world refinement mandates:
    /// 1. Floating yellow-white glowing semi-transparent particles everywhere (100 feet radius around player).
    /// 2. Flora overhaul: Replace all trees and bushes with Anime Nature pack (Tree_01..08, Bush_01..04).
    /// 3. Floor generation: Interactive Nyxis wind grass + Low Poly Flower Pack (clusters & blossoms).
    /// 4. Strict Volcano Exclusion: Zero trees, bushes, grass, or flowers on the volcano.
    /// 5. Kingdom population: Gothic Cathedral materials, shops, workshops, street props, and lively NPCs.
    /// </summary>
    public static class WorldRefinementDirector
    {
        private const string PARTICLES_ROOT_NAME = "Ambient_Glowing_Motes_System";
        private const string PARTICLES_MAT_PATH = "Assets/Materials/M_AmbientGlowingMotes.mat";
        private const string PARTICLES_TEX_PATH = "Assets/Idyllic Fantasy Nature/Textures/FX/Particle.png";

        private const string ANIME_TREES_ROOT = "--- WORLD ANIME VEGETATION ---";
        private const string FLOWERS_ROOT_NAME = "--- WORLD BLOSSOMS & FLOWERS ---";
        private const string CITADEL_PROPS_ROOT = "Citadel_Lively_Populated_Props";

        // Volcano exclusion center and radius (Unity coordinates: X=260, Z=-270, Magio Volcanic Range)
        private static readonly Vector2 VOLCANO_CENTER = new Vector2(260f, -270f);
        private const float VOLCANO_EXCLUSION_RADIUS = 185f;

        [MenuItem("BattleSummoner/Refine Entire World (Cathedral, Foliage, Particles, NPCs)", false, 1)]
        public static void RefineEntireWorld()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("<color=cyan>=== STARTING MASTER WORLD REFINEMENT ===</color>");

            // 1. Setup Ambient Glowing Particles
            SetupGlowingMotesSystem();

            // 2. Setup Cathedral Materials
            RefineMeshyCathedralMaterials();

            // 3. Remove Old Trees & Bushes
            CleanupOldVegetation();

            // 4. Populate Anime Trees & Bushes (With Volcano Exclusion)
            PopulateAnimeVegetation();

            // 5. Populate Flowers on Floor Generation (With Volcano Exclusion)
            PopulateWorldFlowers();

            // 6. Populate Concentric Kingdom Shops, Props & NPCs
            PopulateKingdomAndDockcityLively();

            // 7. Regenerate Wind Grass with strict volcano exclusion
            CircularContinentGrassPopulator.PopulateGrass();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>=== MASTER WORLD REFINEMENT COMPLETED SUCCESSFULLY! ===</color>");
        }

        // =========================================================================
        // 1. AMBIENT GLOWING MOTES PARTICLE SYSTEM
        // =========================================================================
        [MenuItem("BattleSummoner/Setup Ambient Glowing Motes", false, 2)]
        public static void SetupGlowingMotesSystem()
        {
            Debug.Log("Configuring Ambient Glowing Motes System...");

            // Create or load glowing particle material
            Material motesMat = AssetDatabase.LoadAssetAtPath<Material>(PARTICLES_MAT_PATH);
            if (motesMat == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Particles/Standard Unlit");
                motesMat = new Material(unlitShader);
                motesMat.name = "M_AmbientGlowingMotes";
                motesMat.SetFloat("_Surface", 1); // Transparent
                motesMat.SetFloat("_Blend", 0);   // Alpha blend
                motesMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                motesMat.renderQueue = 3000;

                Texture2D particleTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PARTICLES_TEX_PATH);
                if (particleTex != null)
                {
                    motesMat.SetTexture("_BaseMap", particleTex);
                }
                motesMat.SetColor("_BaseColor", new Color(1.0f, 0.98f, 0.85f, 0.65f));
                motesMat.SetColor("_EmissionColor", new Color(1.6f, 1.45f, 0.95f, 1.0f));
                motesMat.EnableKeyword("_EMISSION");

                AssetDatabase.CreateAsset(motesMat, PARTICLES_MAT_PATH);
                AssetDatabase.SaveAssets();
            }

            // Find or create GameObject in scene
            GameObject motesObj = GameObject.Find(PARTICLES_ROOT_NAME);
            if (motesObj == null)
            {
                motesObj = new GameObject(PARTICLES_ROOT_NAME);
                Undo.RegisterCreatedObjectUndo(motesObj, "Create Glowing Motes");
            }

            var ps = motesObj.GetComponent<ParticleSystem>();
            if (ps == null) ps = motesObj.AddComponent<ParticleSystem>();

            var psr = motesObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.material = motesMat;
                psr.renderMode = ParticleSystemRenderMode.Billboard;
                psr.sortMode = ParticleSystemSortMode.Distance;
                psr.alignment = ParticleSystemRenderSpace.Facing;
            }

            var controller = motesObj.GetComponent<AmbientGlowingMotesController>();
            if (controller == null) controller = motesObj.AddComponent<AmbientGlowingMotesController>();

            controller.ConfigureMotesParameters();
            Debug.Log("Ambient Glowing Motes System configured successfully!");
        }

        // =========================================================================
        // 2. MESHY CATHEDRAL MATERIALS REFINEMENT
        // =========================================================================
        [MenuItem("BattleSummoner/Refine Cathedral Materials", false, 3)]
        public static void RefineMeshyCathedralMaterials()
        {
            var cathedral = GameObject.Find("Citadel_Meshy_GrandCathedral");
            if (cathedral == null)
            {
                Debug.LogWarning("Citadel_Meshy_GrandCathedral not found in scene!");
                return;
            }

            var mr = cathedral.GetComponent<MeshRenderer>();
            if (mr == null) return;

            Material matExt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Metropolis/M_CathedralGothic_Exterior.mat");
            Material matDoors = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Metropolis/M_CathedralGothic_Doors.mat");
            Material matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Metropolis/M_CathedralGothic_Floor.mat");

            if (matExt == null) matExt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_CathedralStone.mat");
            if (matDoors == null) matDoors = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Cathedral_Doors_Oak.mat");
            if (matFloor == null) matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Cathedral_StoneFloor.mat");

            var newMats = new Material[mr.sharedMaterials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                if (i == 0) newMats[i] = matExt;
                else if (i == 1) newMats[i] = matDoors;
                else newMats[i] = matFloor;
            }
            mr.sharedMaterials = newMats;
            Debug.Log("Meshy Cathedral materials assigned with high-detail gothic shaders!");
        }

        // =========================================================================
        // 3. CLEANUP OLD PLACEHOLDER VEGETATION
        // =========================================================================
        private static void CleanupOldVegetation()
        {
            Debug.Log("Cleaning up old willow / placeholder trees...");
            var oldVegRoot = GameObject.Find(ANIME_TREES_ROOT);
            if (oldVegRoot != null) Undo.DestroyObjectImmediate(oldVegRoot);

            var oldFlowersRoot = GameObject.Find(FLOWERS_ROOT_NAME);
            if (oldFlowersRoot != null) Undo.DestroyObjectImmediate(oldFlowersRoot);

            // Clean old trees from Hydrology and Riparian roots
            var hydro = GameObject.Find("Hydrology_Flora_And_Fauna");
            if (hydro != null)
            {
                var filters = hydro.GetComponentsInChildren<MeshFilter>(true);
                foreach (var f in filters)
                {
                    if (f != null && f.sharedMesh != null && f.sharedMesh.name.ToLower().Contains("tree"))
                    {
                        // Remove parent tree GameObject
                        Transform p = f.transform;
                        while (p.parent != null && p.parent != hydro.transform && !p.parent.name.StartsWith("Flora_"))
                        {
                            p = p.parent;
                        }
                        if (p != null && p.gameObject != null && p.gameObject != hydro)
                        {
                            Undo.DestroyObjectImmediate(p.gameObject);
                        }
                    }
                }
            }

            var riparian = GameObject.Find("Riparian_Nature_And_Cliffs");
            if (riparian != null)
            {
                var willows = riparian.transform.Find("01_Riparian_Willow_Trees");
                if (willows != null) Undo.DestroyObjectImmediate(willows.gameObject);
            }
        }

        // =========================================================================
        // 4. POPULATE ANIME NATURE PACK (TREES & BUSHES) WITH VOLCANO EXCLUSION
        // =========================================================================
        [MenuItem("BattleSummoner/Populate Anime Trees", false, 4)]
        public static void PopulateAnimeVegetation()
        {
            Debug.Log("Populating lush Anime Nature trees and bushes...");

            var terrain = GameObject.Find("Circular_Continent_Terrain");
            if (terrain == null) return;
            var collider = terrain.GetComponent<Collider>();
            if (collider == null) return;

            string[] treePaths = new string[] {
                "Assets/Anime Trees/Prefabs/Tree_01.prefab",
                "Assets/Anime Trees/Prefabs/Tree_02.prefab",
                "Assets/Anime Trees/Prefabs/Tree_03.prefab",
                "Assets/Anime Trees/Prefabs/Tree_04.prefab",
                "Assets/Anime Trees/Prefabs/Tree_05.prefab",
                "Assets/Anime Trees/Prefabs/Tree_06.prefab",
                "Assets/Anime Trees/Prefabs/Tree_07.prefab",
                "Assets/Anime Trees/Prefabs/Tree_08.prefab"
            };

            string[] bushPaths = new string[] {
                "Assets/Anime Trees/Prefabs/Bush_01.prefab",
                "Assets/Anime Trees/Prefabs/Bush_02.prefab",
                "Assets/Anime Trees/Prefabs/Bush_03.prefab",
                "Assets/Anime Trees/Prefabs/Bush_04.prefab"
            };

            var treePrefabs = new List<GameObject>();
            foreach (var p in treePaths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) treePrefabs.Add(go);
            }

            var bushPrefabs = new List<GameObject>();
            foreach (var p in bushPaths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) bushPrefabs.Add(go);
            }

            if (treePrefabs.Count == 0 || bushPrefabs.Count == 0)
            {
                Debug.LogError("Anime Trees or Bushes prefabs could not be loaded!");
                return;
            }

            GameObject rootObj = new GameObject(ANIME_TREES_ROOT);
            Undo.RegisterCreatedObjectUndo(rootObj, "Populate Anime Trees");

            // Biome spawning zones
            // 1. Temperate Forest & Brestuoil (NE): X > 60, Z > 40
            // 2. Southern Ancient Forest (South): Z < -150, X between -100 and 220
            // 3. Glacial Foothills: Z > 180, X between -50 and 80
            // 4. Citadel Outskirts & Riverbanks
            int treeCount = 0;
            int bushCount = 0;

            UnityEngine.Random.InitState(4242);

            // A. Forest Clusters
            for (int cluster = 0; cluster < 65; cluster++)
            {
                Vector2 clusterCenter;
                float clusterRadius = UnityEngine.Random.Range(20f, 45f);
                int treesInCluster = UnityEngine.Random.Range(10, 22);
                int bushesInCluster = UnityEngine.Random.Range(8, 18);

                int biomeChoice = cluster % 3;
                if (biomeChoice == 0)
                {
                    // NE Temperate Forest & Lakeshore
                    clusterCenter = new Vector2(UnityEngine.Random.Range(90f, 340f), UnityEngine.Random.Range(70f, 320f));
                }
                else if (biomeChoice == 1)
                {
                    // South Forest & Delta
                    clusterCenter = new Vector2(UnityEngine.Random.Range(10f, 210f), UnityEngine.Random.Range(-360f, -160f));
                }
                else
                {
                    // Glacial Foothills & Highland Terraces
                    clusterCenter = new Vector2(UnityEngine.Random.Range(-140f, 60f), UnityEngine.Random.Range(150f, 280f));
                }

                // Check volcano exclusion
                if (Vector2.Distance(clusterCenter, VOLCANO_CENTER) < (VOLCANO_EXCLUSION_RADIUS + 30f))
                    continue;

                // Spawn Trees in cluster
                for (int t = 0; t < treesInCluster; t++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * clusterRadius;
                    Vector3 samplePos = new Vector3(clusterCenter.x + offset.x, 300f, clusterCenter.y + offset.y);

                    // STRICT VOLCANO EXCLUSION
                    if (Vector2.Distance(new Vector2(samplePos.x, samplePos.z), VOLCANO_CENTER) < VOLCANO_EXCLUSION_RADIUS)
                        continue;

                    // Citadel exclusion
                    if (Mathf.Sqrt(samplePos.x * samplePos.x + samplePos.z * samplePos.z) < 118f)
                        continue;

                    if (collider.Raycast(new Ray(samplePos, Vector3.down), out RaycastHit hit, 400f))
                    {
                        if (hit.point.y < 1.5f || hit.normal.y < 0.65f) continue;

                        GameObject pfb = treePrefabs[UnityEngine.Random.Range(0, treePrefabs.Count)];
                        GameObject treeInstance = (GameObject)PrefabUtility.InstantiatePrefab(pfb, rootObj.transform);
                        treeInstance.transform.position = hit.point;
                        treeInstance.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        float s = UnityEngine.Random.Range(0.85f, 1.45f);
                        treeInstance.transform.localScale = Vector3.one * s;
                        treeCount++;
                    }
                }

                // Spawn Bushes in cluster
                for (int b = 0; b < bushesInCluster; b++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * clusterRadius;
                    Vector3 samplePos = new Vector3(clusterCenter.x + offset.x, 300f, clusterCenter.y + offset.y);

                    if (Vector2.Distance(new Vector2(samplePos.x, samplePos.z), VOLCANO_CENTER) < VOLCANO_EXCLUSION_RADIUS)
                        continue;
                    if (Mathf.Sqrt(samplePos.x * samplePos.x + samplePos.z * samplePos.z) < 115f)
                        continue;

                    if (collider.Raycast(new Ray(samplePos, Vector3.down), out RaycastHit hit, 400f))
                    {
                        if (hit.point.y < 1.2f || hit.normal.y < 0.60f) continue;

                        GameObject pfb = bushPrefabs[UnityEngine.Random.Range(0, bushPrefabs.Count)];
                        GameObject bushInstance = (GameObject)PrefabUtility.InstantiatePrefab(pfb, rootObj.transform);
                        bushInstance.transform.position = hit.point;
                        bushInstance.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        float s = UnityEngine.Random.Range(0.9f, 1.6f);
                        bushInstance.transform.localScale = Vector3.one * s;
                        bushCount++;
                    }
                }
            }

            Debug.Log($"Populated {treeCount} Anime Trees and {bushCount} Anime Bushes! (Volcano completely excluded).");
        }

        // =========================================================================
        // 5. POPULATE FLOWERS ON FLOOR GENERATION WITH VOLCANO EXCLUSION
        // =========================================================================
        [MenuItem("BattleSummoner/Populate World Flowers", false, 5)]
        public static void PopulateWorldFlowers()
        {
            Debug.Log("Scattering Low Poly Flower Pack clusters onto floor generation...");

            var terrain = GameObject.Find("Circular_Continent_Terrain");
            if (terrain == null) return;
            var collider = terrain.GetComponent<Collider>();
            if (collider == null) return;

            string[] clusterPaths = new string[] {
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab",
                "Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab"
            };

            var clusterPrefabs = new List<GameObject>();
            foreach (var p in clusterPaths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) clusterPrefabs.Add(go);
            }

            if (clusterPrefabs.Count == 0)
            {
                Debug.LogWarning("Flower cluster prefabs not found!");
                return;
            }

            GameObject rootObj = new GameObject(FLOWERS_ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(rootObj, "Populate World Flowers");

            int flowersSpawned = 0;
            UnityEngine.Random.InitState(777);

            // Scatter 280 dense flower clusters across grassy meadows and roadsides
            for (int i = 0; i < 350; i++)
            {
                float rx = UnityEngine.Random.Range(-440f, 440f);
                float rz = UnityEngine.Random.Range(-440f, 440f);

                // STRICT VOLCANO EXCLUSION
                if (Vector2.Distance(new Vector2(rx, rz), VOLCANO_CENTER) < VOLCANO_EXCLUSION_RADIUS)
                    continue;

                // Desert exclusion (West)
                if (rx < -90f && rz > -300f && rz < 120f)
                    continue;

                // Central Citadel moat interior
                float distOrigin = Mathf.Sqrt(rx * rx + rz * rz);
                if (distOrigin < 112f && distOrigin > 68f) // Moat water
                    continue;

                if (collider.Raycast(new Ray(new Vector3(rx, 300f, rz), Vector3.down), out RaycastHit hit, 400f))
                {
                    // Must be above sea and gentle terrain
                    if (hit.point.y < 1.2f || hit.normal.y < 0.70f) continue;

                    GameObject pfb = clusterPrefabs[UnityEngine.Random.Range(0, clusterPrefabs.Count)];
                    GameObject flowerInstance = (GameObject)PrefabUtility.InstantiatePrefab(pfb, rootObj.transform);
                    flowerInstance.transform.position = hit.point + Vector3.up * 0.02f;
                    Quaternion alignRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    Quaternion yawRot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    flowerInstance.transform.rotation = alignRot * yawRot;
                    float s = UnityEngine.Random.Range(0.85f, 1.35f);
                    flowerInstance.transform.localScale = Vector3.one * s;
                    flowersSpawned++;
                }
            }

            Debug.Log($"Scattered {flowersSpawned} Flower Clusters across meadows and floors!");
        }

        // =========================================================================
        // 6. POPULATE CONCENTRIC KINGDOM SHOPS, PROPS & LIVELY NPCS
        // =========================================================================
        [MenuItem("BattleSummoner/Populate Kingdom Shops, Props & NPCs", false, 6)]
        public static void PopulateKingdomAndDockcityLively()
        {
            Debug.Log("Populating Concentric Kingdom with shops, bazaar stalls, street props, and NPCs...");

            var oldRoot = GameObject.Find(CITADEL_PROPS_ROOT);
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

            GameObject root = new GameObject(CITADEL_PROPS_ROOT);
            Undo.RegisterCreatedObjectUndo(root, "Populate Kingdom Props & NPCs");

            // Helper to spawn a model at location
            Action<string, Vector3, float, float> spawnModel = (path, pos, yaw, scale) =>
            {
                var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (pfb != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(pfb, root.transform);
                    instance.transform.position = pos;
                    instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    instance.transform.localScale = Vector3.one * scale;
                }
            };

            // Tier 4 Plaza (R < 25m) - Flanking Grand Gothic Cathedral
            // Hover Pads, Fountain, Glowing Fairies, Perched Owls
            spawnModel("Assets/Models/Metropolis/Prop_HoverPad_Citadel.fbx", new Vector3(18f, 16.2f, 12f), 45f, 1.2f);
            spawnModel("Assets/Models/Metropolis/Prop_HoverPad_Citadel.fbx", new Vector3(-18f, 16.2f, 12f), -45f, 1.2f);
            spawnModel("Assets/Models/Metropolis/Critter_GlowingFairy.fbx", new Vector3(14f, 18.5f, 8f), 0f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Critter_GlowingFairy.fbx", new Vector3(-14f, 18.5f, 8f), 0f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Critter_GlowingFairy.fbx", new Vector3(0f, 19.5f, -18f), 0f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Critter_PerchedOwl.fbx", new Vector3(8f, 21.0f, -12f), 120f, 1.0f);

            // Tier 3 Mages' Quarter & High-End Sector (R ~ 32m - 42m)
            // Arcane Spire, Alchemist Workshop, Enchanter Workshop, Wizard Observatory, Witch on Broom, Mana Crystals
            spawnModel("Assets/Models/Metropolis/Prop_ArcaneSpire_MagesQuarter.fbx", new Vector3(-25f, 16.2f, 22f), 0f, 1.4f);
            spawnModel("Assets/Models/Metropolis/Building_AlchemistWorkshop.fbx", new Vector3(-32f, 16.2f, 15f), 75f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Building_EnchanterWorkshop.fbx", new Vector3(-28f, 16.2f, 28f), 110f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Building_WizardObservatory.fbx", new Vector3(-35f, 16.2f, -12f), 45f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Character_WitchFlyingBroom.fbx", new Vector3(-22f, 20.5f, 18f), -60f, 1.1f);
            spawnModel("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx", new Vector3(-20f, 16.2f, 24f), 0f, 1.5f);
            spawnModel("Assets/Models/TownProps/Prop_AlchemyLabSet.fbx", new Vector3(-30f, 16.2f, 17f), 0f, 1.2f);

            // Tier 2 Commercial Shops & Vibrant Bazaars (R ~ 48m - 62m)
            // Bakery, Blacksmith Forge, Open Tavern, Magic Item Store, Guildhall
            spawnModel("Assets/Models/Metropolis/Building_Bakery_Shop.fbx", new Vector3(38f, 16.2f, -28f), -45f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Building_Blacksmith_OpenForge.fbx", new Vector3(45f, 16.2f, -18f), -20f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Building_Tavern_Open.fbx", new Vector3(42f, 16.2f, 15f), 135f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Building_MagicItemStore.fbx", new Vector3(25f, 16.2f, 38f), 160f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Building_Guildhall_Grand.fbx", new Vector3(12f, 16.2f, 48f), 180f, 1.1f);

            // Market Stalls, Bazaar Booths, Barrels, Crates & Coins
            spawnModel("Assets/Models/Metropolis/Prop_MarketStalls_Cluster.fbx", new Vector3(30f, 16.2f, -22f), 30f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Prop_MarketStalls_Cluster.fbx", new Vector3(22f, 16.2f, -32f), -15f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Prop_BazaarStand_Street.fbx", new Vector3(35f, 16.2f, -12f), 80f, 1.0f);
            spawnModel("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", new Vector3(40f, 16.2f, -24f), 0f, 1.2f);
            spawnModel("Assets/Models/TownProps/Prop_GoldCoinsChest.fbx", new Vector3(32f, 16.2f, -20f), 45f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Prop_PaperLanterns_String.fbx", new Vector3(28f, 19.0f, -26f), 25f, 1.0f);

            // NPCs in Market & Plazas
            spawnModel("Assets/Models/Metropolis/Character_BardLute.fbx", new Vector3(38f, 16.2f, 12f), -60f, 1.0f); // Bard by tavern
            spawnModel("Assets/Models/Metropolis/Character_Beastfolk_Fox.fbx", new Vector3(28f, 16.2f, -18f), 120f, 1.0f); // Fox merchant
            spawnModel("Assets/Models/Metropolis/Character_Beastfolk_Wolf.fbx", new Vector3(31f, 16.2f, -17f), -60f, 1.0f); // Wolf customer
            spawnModel("Assets/Models/Metropolis/Character_RunningKids.fbx", new Vector3(20f, 16.2f, -10f), 40f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Golem_ClockworkBrassAssistant.fbx", new Vector3(42f, 16.2f, -14f), 0f, 1.2f); // Brass golem at forge

            // Tier 1 Military Barracks & Bastion Defenses (R ~ 64m - 68m)
            // Guild Armory, Sparring Arena Dummies, Bastion Portal Gates, Mana Shield Generators
            spawnModel("Assets/Models/Metropolis/Building_GuildArmory.fbx", new Vector3(-45f, 16.2f, -42f), 35f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Prop_SparringArena_TargetsDummies.fbx", new Vector3(-38f, 16.2f, -48f), 0f, 1.0f);
            spawnModel("Assets/Models/Metropolis/Mount_ArmoredWolfBeast.fbx", new Vector3(-40f, 16.2f, -38f), 90f, 1.1f);
            spawnModel("Assets/Models/Metropolis/Mount_FeatheredChocoboBird.fbx", new Vector3(15f, 16.2f, -45f), -30f, 1.1f);

            // Mana Shield Generator Towers on Citadel Wall Bastions
            float[] bastionAngles = new float[] { 0f, 60f, 120f, 180f, 240f, 300f };
            foreach (var ang in bastionAngles)
            {
                float rad = ang * Mathf.Deg2Rad;
                float bx = 68.0f * Mathf.Cos(rad);
                float bz = 68.0f * Mathf.Sin(rad);
                spawnModel("Assets/Models/Metropolis/Prop_ManaShield_GeneratorTower.fbx", new Vector3(bx, 23.0f, bz), ang, 1.3f);
            }

            Debug.Log("Concentric Kingdom populated with vibrant shops, street props, lanterns, and lively NPCs!");
        }
    }
}

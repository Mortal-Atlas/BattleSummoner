using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// MasterWorldRefinementPipeline addresses all user feedback:
    /// 1. Binds the sculpted, upward-normal Island_Overworld_Terrain mesh to the terrain object.
    /// 2. Fixes terrain shader parameters and detail tiling (200x) so biomes are rich, distinct, and not white/flat.
    /// 3. Builds 24 monumental stone staircases connecting concentric tiers across the 8 radial avenues.
    /// 4. Optimizes kingdom masonry and road texture tiling so stones are human-scaled rather than stretched.
    /// 5. Purges ALL old non-anime trees and procedural primitives.
    /// 6. Populates the island densely with ONLY the newest Anime Trees (Tree_01 to Tree_08, Bush_01 to Bush_04)
    ///    at proper human proportions (scale 0.45 - 0.75), perfectly ground-clamped via terrain raycasts.
    /// </summary>
    public static class MasterWorldRefinementPipeline
    {
        private const string SCENE_PATH = "Assets/Scenes/OverworldMap.unity";
        private const string CLEAN_MESH_PATH = "Assets/Models/Environment/Mesh_Island_Overworld_Terrain.asset";
        private const string TERRAIN_MAT_PATH = "Assets/Materials/Environment/Mat_Island_Overworld_Terrain.mat";

        private struct TierTransition
        {
            public string name;
            public float lowerY;
            public float upperY;
            public float radius;
            public float runLength;
            public float stairWidth;
            public int stepCount;
        }

        [MenuItem("Tools/BattleSummoner/Execute Complete World Refinement Pipeline", false, 1)]
        public static void ExecuteWorldRefinement()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SCENE_PATH)
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
            }

            Debug.Log("======================================================================");
            Debug.Log("🌟 [MASTER REFINEMENT] STARTING ENVIRONMENT & KINGDOM TRANSFORMATION");
            Debug.Log("======================================================================");

            // Step 1: Bind Clean Sculpted Terrain Mesh and Configure PBR Multi-Splat Material
            RefineTerrainAndBiomes();

            // Step 2: Build Monumental Kingdom Staircases (24 staircases connecting Tiers 1-4)
            BuildKingdomStaircases();

            // Step 3: Fix Kingdom Texture Tiling (Scale textures properly to humanoid proportions)
            FixKingdomTextureTiling();

            // Step 4: Purge All Old Non-Anime Trees and Giant Trees
            PurgeOldAndGiantTrees();

            // Step 5: Densely Populate World with ONLY Newest Anime Trees & Bushes
            PopulateDenseAnimeTreesAndBushes();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("🎉 [MASTER REFINEMENT COMPLETE] Sculpted 1.7km island with rich biomes, monumental stairs, properly tiled textures, and dense anime forests successfully created!");
        }

        // =========================================================================
        // 1. TERRAIN & BIOME MATERIAL SETUP
        // =========================================================================
        private static void RefineTerrainAndBiomes()
        {
            var terrainObj = GameObject.Find("Island_Overworld_Terrain");
            if (terrainObj == null)
            {
                terrainObj = new GameObject("Island_Overworld_Terrain");
                var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
                if (metroRoot != null) terrainObj.transform.SetParent(metroRoot.transform, false);
            }

            terrainObj.transform.position = Vector3.zero;
            terrainObj.transform.rotation = Quaternion.identity;
            terrainObj.transform.localScale = Vector3.one;

            var cleanMesh = AssetDatabase.LoadAssetAtPath<Mesh>(CLEAN_MESH_PATH);
            if (cleanMesh == null)
            {
                Debug.LogError($"[Terrain Refine] {CLEAN_MESH_PATH} not found!");
                return;
            }

            var mf = terrainObj.GetComponent<MeshFilter>() ?? terrainObj.AddComponent<MeshFilter>();
            mf.sharedMesh = cleanMesh;

            var mr = terrainObj.GetComponent<MeshRenderer>() ?? terrainObj.AddComponent<MeshRenderer>();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(TERRAIN_MAT_PATH);
            if (mat != null)
            {
                mat.SetFloat("_DetailTiling", 200.0f);
                mat.SetFloat("_CliffThreshold", 0.55f);
                mat.SetFloat("_CliffBlend", 0.12f);
                mat.SetFloat("_SatelliteTintStrength", 0.35f);
                mat.SetFloat("_Smoothness", 0.05f);
                EditorUtility.SetDirty(mat);
                mr.sharedMaterial = mat;
            }

            var mc = terrainObj.GetComponent<MeshCollider>() ?? terrainObj.AddComponent<MeshCollider>();
            mc.sharedMesh = null;
            mc.sharedMesh = cleanMesh;

            // Remove obsolete flat plain & redundant volcanic crag planes
            var oldPlain = GameObject.Find("Tier0_Outerwilds_Plain");
            if (oldPlain != null) UnityEngine.Object.DestroyImmediate(oldPlain);

            var oldCrags = GameObject.Find("Outerwilds_NE_VolcanicCrags");
            if (oldCrags != null) UnityEngine.Object.DestroyImmediate(oldCrags);

            Debug.Log("🏝️ [Terrain Refine] Island_Overworld_Terrain successfully bound with 1.7km clean upward-normal mesh and 200x detail PBR multi-biome shader.");
        }

        // =========================================================================
        // 2. MONUMENTAL KINGDOM STAIRCASES (CONNECTING CONCENTRIC TIERS)
        // =========================================================================
        private static void BuildKingdomStaircases()
        {
            var oldStairsRoot = GameObject.Find("09_Kingdom_Monumental_Staircases");
            if (oldStairsRoot != null) UnityEngine.Object.DestroyImmediate(oldStairsRoot);

            var stairsRoot = new GameObject("09_Kingdom_Monumental_Staircases");
            var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metroRoot != null) stairsRoot.transform.SetParent(metroRoot.transform, false);

            var stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/Mat_City_PavedLimestone.mat")
                          ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/Mat_World_TownWall.mat");
            var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/Mat_World_TownWall.mat");
            var lanternPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/LanternV1.prefab")
                             ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Metropolis/Streetlight_DualBanner_Lantern.prefab");

            // 8 Radial Avenues: Cardinal (N, E, S, W) and Diagonal (NE, SE, SW, NW)
            float[] avenueAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            string[] avenueNames = new string[] { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" };

            // 3 Tier Transitions:
            // Transition 1: Tier 1 (Y=0m) to Tier 2 (Y=6m) at R ≈ 236m (RetainingWall_Tier2_Mid)
            // Transition 2: Tier 2 (Y=6m) to Tier 3 (Y=12m) at R ≈ 156m (RetainingWall_Tier3_Upper)
            // Transition 3: Tier 3 (Y=12m) to Tier 4 (Y=18m) at R ≈ 76m (RetainingWall_Tier4_Citadel)

            TierTransition[] transitions = new TierTransition[]
            {
                new TierTransition { name = "Tier1_to_Tier2", lowerY = 0f, upperY = 6f, radius = 236f, runLength = 16f, stairWidth = 14f, stepCount = 20 },
                new TierTransition { name = "Tier2_to_Tier3", lowerY = 6f, upperY = 12f, radius = 156f, runLength = 14f, stairWidth = 12f, stepCount = 20 },
                new TierTransition { name = "Tier3_to_Tier4", lowerY = 12f, upperY = 18f, radius = 76f, runLength = 12f, stairWidth = 10f, stepCount = 20 }
            };

            for (int a = 0; a < avenueAngles.Length; a++)
            {
                float angDeg = avenueAngles[a];
                float rad = angDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 rightDir = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));
                Quaternion rot = Quaternion.Euler(0f, angDeg, 0f);

                for (int t = 0; t < transitions.Length; t++)
                {
                    var trans = transitions[t];
                    string stairName = $"Stair_{avenueNames[a]}_{trans.name}";

                    var stairObj = new GameObject(stairName);
                    stairObj.transform.SetParent(stairsRoot.transform, false);

                    // Stair center position
                    Vector3 centerPos = dir * trans.radius + Vector3.up * ((trans.lowerY + trans.upperY) * 0.5f);
                    stairObj.transform.position = centerPos;
                    stairObj.transform.rotation = rot;

                    // Generate individual carved stone steps
                    float stepRise = (trans.upperY - trans.lowerY) / trans.stepCount;
                    float stepRun = trans.runLength / trans.stepCount;

                    for (int s = 0; s < trans.stepCount; s++)
                    {
                        var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        step.name = $"Step_{s + 1:D2}";
                        step.transform.SetParent(stairObj.transform, false);

                        // Local coordinate: steps ascend inward towards center (decreasing radius) or outward
                        // Inward ascent: outer is lowerY, inner is upperY
                        float localZ = (trans.runLength * 0.5f) - (s + 0.5f) * stepRun;
                        float localY = -(trans.upperY - trans.lowerY) * 0.5f + (s + 0.5f) * stepRise;

                        step.transform.localPosition = new Vector3(0f, localY, localZ);
                        step.transform.localRotation = Quaternion.identity;
                        step.transform.localScale = new Vector3(trans.stairWidth, stepRise, stepRun * 1.05f);

                        var r = step.GetComponent<Renderer>();
                        if (r != null && stoneMat != null) r.sharedMaterial = stoneMat;

                        // Remove individual box collider to avoid snagging, we add smooth ramp collider
                        var col = step.GetComponent<Collider>();
                        if (col != null) UnityEngine.Object.DestroyImmediate(col);
                    }

                    // Left Balustrade Parapet Wall
                    CreateBalustrade(stairObj.transform, -trans.stairWidth * 0.5f - 0.45f, trans.runLength, trans.upperY - trans.lowerY, wallMat);
                    // Right Balustrade Parapet Wall
                    CreateBalustrade(stairObj.transform, trans.stairWidth * 0.5f + 0.45f, trans.runLength, trans.upperY - trans.lowerY, wallMat);

                    // Decorative Entrance Pillars with Lanterns at base and top
                    CreatePillarWithLantern(stairObj.transform, -trans.stairWidth * 0.5f - 0.45f, trans.runLength * 0.5f, -(trans.upperY - trans.lowerY) * 0.5f, wallMat, lanternPrefab);
                    CreatePillarWithLantern(stairObj.transform, trans.stairWidth * 0.5f + 0.45f, trans.runLength * 0.5f, -(trans.upperY - trans.lowerY) * 0.5f, wallMat, lanternPrefab);
                    CreatePillarWithLantern(stairObj.transform, -trans.stairWidth * 0.5f - 0.45f, -trans.runLength * 0.5f, (trans.upperY - trans.lowerY) * 0.5f, wallMat, lanternPrefab);
                    CreatePillarWithLantern(stairObj.transform, trans.stairWidth * 0.5f + 0.45f, -trans.runLength * 0.5f, (trans.upperY - trans.lowerY) * 0.5f, wallMat, lanternPrefab);

                    // Smooth Invisible BoxCollider Ramp for silky player traversal
                    var rampColliderObj = new GameObject("Ramp_Walkway_Collider");
                    rampColliderObj.transform.SetParent(stairObj.transform, false);
                    rampColliderObj.transform.localPosition = Vector3.zero;

                    float slopeAng = Mathf.Atan2(trans.upperY - trans.lowerY, trans.runLength) * Mathf.Rad2Deg;
                    rampColliderObj.transform.localRotation = Quaternion.Euler(-slopeAng, 0f, 0f);

                    float rampLen = Mathf.Sqrt(Mathf.Pow(trans.runLength, 2) + Mathf.Pow(trans.upperY - trans.lowerY, 2));
                    var boxCol = rampColliderObj.AddComponent<BoxCollider>();
                    boxCol.size = new Vector3(trans.stairWidth, 0.4f, rampLen);
                    boxCol.center = Vector3.zero;
                }
            }

            Debug.Log($"🏛️ [Staircases Built] Successfully constructed 24 monumental stone staircases with balustrades, decorative pillars, and smooth collision ramps across all 8 avenues!");
        }

        private static void CreateBalustrade(Transform parent, float localX, float runLength, float riseHeight, Material mat)
        {
            var bal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bal.name = $"Balustrade_{(localX < 0 ? "Left" : "Right")}";
            bal.transform.SetParent(parent, false);

            float slopeAng = Mathf.Atan2(riseHeight, runLength) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(runLength * runLength + riseHeight * riseHeight);

            bal.transform.localPosition = new Vector3(localX, 0.65f, 0f);
            bal.transform.localRotation = Quaternion.Euler(-slopeAng, 0f, 0f);
            bal.transform.localScale = new Vector3(0.9f, 1.3f, len + 0.5f);

            var r = bal.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
        }

        private static void CreatePillarWithLantern(Transform parent, float x, float z, float y, Material mat, GameObject lanternPrefab)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Balustrade_Pillar";
            pillar.transform.SetParent(parent, false);
            pillar.transform.localPosition = new Vector3(x, y + 1.1f, z);
            pillar.transform.localRotation = Quaternion.identity;
            pillar.transform.localScale = new Vector3(1.1f, 2.2f, 1.1f);

            var r = pillar.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            if (lanternPrefab != null)
            {
                var lantern = (GameObject)PrefabUtility.InstantiatePrefab(lanternPrefab, pillar.transform);
                lantern.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                lantern.transform.localRotation = Quaternion.identity;
                lantern.transform.localScale = Vector3.one * 0.65f;
            }
        }

        // =========================================================================
        // 3. KINGDOM TEXTURE TILING OPTIMIZATION
        // =========================================================================
        private static void FixKingdomTextureTiling()
        {
            var materialsToRetile = new Dictionary<string, Vector2>()
            {
                { "Assets/Materials/Metropolis/Mat_Tier1_GroundRing.mat", new Vector2(24.0f, 24.0f) },
                { "Assets/Materials/Metropolis/Mat_Tier2_MidRing.mat", new Vector2(20.0f, 20.0f) },
                { "Assets/Materials/Metropolis/Mat_Tier3_UpperRing.mat", new Vector2(16.0f, 16.0f) },
                { "Assets/Materials/Metropolis/Mat_Tier4_CitadelPlateau.mat", new Vector2(12.0f, 12.0f) },
                { "Assets/Materials/Metropolis/Mat_City_Farmland.mat", new Vector2(8.0f, 8.0f) },
                { "Assets/Materials/Metropolis/Mat_City_PavedLimestone.mat", new Vector2(3.0f, 3.0f) },
                { "Assets/Materials/Metropolis/Mat_World_TownWall.mat", new Vector2(8.0f, 8.0f) },
                { "Assets/Materials/Metropolis/M_Metro_Limestone.mat", new Vector2(8.0f, 8.0f) },
                { "Assets/Materials/Metropolis/M_Metro_CathedralStone.mat", new Vector2(8.0f, 16.0f) },
                { "Assets/Materials/Metropolis/M_WallTrim.mat", new Vector2(6.0f, 12.0f) },
                { "Assets/Materials/Metropolis/M_WallMoss.mat", new Vector2(6.0f, 6.0f) }
            };

            foreach (var kvp in materialsToRetile)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(kvp.Key);
                if (mat != null)
                {
                    mat.mainTextureScale = kvp.Value;
                    if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", kvp.Value);
                    EditorUtility.SetDirty(mat);
                }
            }

            Debug.Log("🎨 [Textures Re-Tiled] Scaled all city terraces, walls, roads, and stone masonry to realistic human proportions!");
        }

        // =========================================================================
        // 4. PURGE OLD NON-ANIME & GIANT TREES
        // =========================================================================
        private static void PurgeOldAndGiantTrees()
        {
            int purgedCount = 0;

            // 1. Destroy old procedural tree root
            var oldAnimeRoot = GameObject.Find("08_Anime_Nature_Trees");
            if (oldAnimeRoot != null)
            {
                purgedCount += oldAnimeRoot.GetComponentsInChildren<Transform>().Length;
                UnityEngine.Object.DestroyImmediate(oldAnimeRoot);
            }

            // 2. Destroy old primitive forests & groves
            var oldExteriorForests = GameObject.Find("Exterior_Forests_And_Groves");
            if (oldExteriorForests != null)
            {
                purgedCount += oldExteriorForests.GetComponentsInChildren<Transform>().Length;
                UnityEngine.Object.DestroyImmediate(oldExteriorForests);
            }

            // 3. Clear old trees inside biomes root
            var biomesRoot = GameObject.Find("06d_Outerwilds_Biomes");
            if (biomesRoot != null)
            {
                var toDestroy = new List<GameObject>();
                for (int i = 0; i < biomesRoot.transform.childCount; i++)
                {
                    var child = biomesRoot.transform.GetChild(i);
                    string n = child.name;
                    if (n.Contains("Mistwood_Grove_") || n.Contains("NW_Mistwood_AncientHenge") || n.Contains("SE_Verdant_Terraces"))
                    {
                        toDestroy.Add(child.gameObject);
                    }
                }
                foreach (var g in toDestroy)
                {
                    purgedCount++;
                    UnityEngine.Object.DestroyImmediate(g);
                }
            }

            // 4. Clean up any loose giant trees in the scene (lossyScale > 2.5 on trees)
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null || t.gameObject == null) continue;
                string n = t.name.ToLower();
                if ((n.Contains("tree") || n.Contains("bush") || n.Contains("oak") || n.Contains("pine")) && t.lossyScale.x > 2.5f)
                {
                    // Only destroy if it's an environment tree object
                    if (!n.Contains("camera") && !n.Contains("player") && !n.Contains("hero"))
                    {
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                        purgedCount++;
                    }
                }
            }

            Debug.Log($"🧹 [Trees Purged] Purged {purgedCount} old, non-anime, and giant out-of-scale tree objects from the scene.");
        }

        // =========================================================================
        // 5. DENSE ANIME TREE & BUSH POPULATION (STRICT GREEN LAND ONLY)
        // =========================================================================
        private static void PopulateDenseAnimeTreesAndBushes()
        {
            var natureRoot = GameObject.Find("10_Anime_Nature_Ecosystem");
            if (natureRoot != null) UnityEngine.Object.DestroyImmediate(natureRoot);

            natureRoot = new GameObject("10_Anime_Nature_Ecosystem");
            var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metroRoot != null) natureRoot.transform.SetParent(metroRoot.transform, false);

            // Load ONLY the newest Anime Trees prefabs
            var treePrefabs = new List<GameObject>();
            for (int i = 1; i <= 8; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Anime Trees/Prefabs/Tree_0{i}.prefab");
                if (p != null) treePrefabs.Add(p);
            }

            var bushPrefabs = new List<GameObject>();
            for (int i = 1; i <= 4; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Anime Trees/Prefabs/Bush_0{i}.prefab");
                if (p != null) bushPrefabs.Add(p);
            }

            if (treePrefabs.Count == 0)
            {
                Debug.LogError("No Anime Tree prefabs found in Assets/Anime Trees/Prefabs!");
                return;
            }

            int totalTreesPlaced = 0;
            int totalBushesPlaced = 0;

            // =====================================================================
            // A. NORTHWEST MISTWOOD HIGHLANDS (LUSH ANCIENT FOREST - GREEN LAND)
            // =====================================================================
            var nwGroup = new GameObject("Group_NW_Mistwood_AncientCanopy");
            nwGroup.transform.SetParent(natureRoot.transform, false);

            // 1. Dense Packed Forest Groves (Tight shoulder-to-shoulder clusters)
            Vector3[] nwDenseGroveCenters = new Vector3[]
            {
                new Vector3(-440f, 0f, 480f),
                new Vector3(-520f, 0f, 390f),
                new Vector3(-370f, 0f, 540f),
                new Vector3(-460f, 0f, 360f)
            };

            foreach (var center in nwDenseGroveCenters)
            {
                PlantDenseCluster(nwGroup.transform, center, 32, 16, 28f, treePrefabs, bushPrefabs, ref totalTreesPlaced, ref totalBushesPlaced);
            }

            // 2. Scattered Mistwood Trees (Organic spread across green hills)
            PlantScatteredTrees(nwGroup.transform, new Vector2(-650f, -360f), new Vector2(360f, 650f), 85, treePrefabs, bushPrefabs, ref totalTreesPlaced, ref totalBushesPlaced);

            // =====================================================================
            // B. SOUTHEAST VERDANT MEADOWS & ORCHARDS (PASTORAL GREEN LAND)
            // =====================================================================
            var seGroup = new GameObject("Group_SE_Verdant_Terrace_Meadows");
            seGroup.transform.SetParent(natureRoot.transform, false);

            // 1. Dense Packed Copse Groves
            Vector3[] seDenseGroveCenters = new Vector3[]
            {
                new Vector3(440f, 0f, -480f),
                new Vector3(530f, 0f, -380f),
                new Vector3(380f, 0f, -530f),
                new Vector3(460f, 0f, -360f)
            };

            foreach (var center in seDenseGroveCenters)
            {
                PlantDenseCluster(seGroup.transform, center, 28, 14, 25f, treePrefabs, bushPrefabs, ref totalTreesPlaced, ref totalBushesPlaced);
            }

            // 2. Scattered Meadow & Orchard Trees
            PlantScatteredTrees(seGroup.transform, new Vector2(360f, 650f), new Vector2(-650f, -360f), 80, treePrefabs, bushPrefabs, ref totalTreesPlaced, ref totalBushesPlaced);

            // =====================================================================
            // C. KINGDOM FARMLAND (TIER 1) & CIVIC PARKS (TIER 2) URBAN FOLIAGE
            // =====================================================================
            var cityGroup = new GameObject("Group_Kingdom_UrbanParks_And_Boulevards");
            cityGroup.transform.SetParent(natureRoot.transform, false);

            // Tier 1 Farmland Orchard Clusters (R ~ 245m to 285m, outside avenues)
            for (int i = 0; i < 40; i++)
            {
                float ang = UnityEngine.Random.Range(0f, 360f);
                if (Mathf.Abs(ang % 45f) < 8f) continue; // Avoid radial avenues
                float rad = ang * Mathf.Deg2Rad;
                float dist = UnityEngine.Random.Range(245f, 285f);
                Vector3 cityPos = new Vector3(Mathf.Sin(rad) * dist, 0f, Mathf.Cos(rad) * dist);
                PlantSingleItem(cityGroup.transform, cityPos, treePrefabs, 0.45f, 0.65f, ref totalTreesPlaced);
                if (bushPrefabs.Count > 0 && UnityEngine.Random.value < 0.5f)
                {
                    PlantSingleBush(cityGroup.transform, cityPos + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0f, UnityEngine.Random.Range(-2f, 2f)), bushPrefabs, ref totalBushesPlaced);
                }
            }

            // Tier 2 Civic Ring Gardens (R ~ 165m to 195m)
            for (int i = 0; i < 28; i++)
            {
                float ang = UnityEngine.Random.Range(0f, 360f);
                if (Mathf.Abs(ang % 45f) < 8f) continue;
                float rad = ang * Mathf.Deg2Rad;
                float dist = UnityEngine.Random.Range(165f, 195f);
                Vector3 cityPos = new Vector3(Mathf.Sin(rad) * dist, 6f, Mathf.Cos(rad) * dist);
                PlantSingleItem(cityGroup.transform, cityPos, treePrefabs, 0.40f, 0.58f, ref totalTreesPlaced);
            }

            Debug.Log($"🌲 [Green Land Tree Population] Successfully planted {totalTreesPlaced} Anime Trees and {totalBushesPlaced} bushes exclusively on Green Land (NW Mistwood, SE Meadows, and Kingdom Parks)! STRICTLY ZERO trees in desert or riverbanks.");
        }

        /// <summary>
        /// Strict validator ensuring trees ONLY spawn on Green Land, never in the SW Desert,
        /// never in rivers or river banks, and never in the moat.
        /// </summary>
        private static bool IsValidGreenLandLocation(Vector3 pos)
        {
            float r = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);

            // 1. Moat Canal Exclusion (R in [290m, 360m])
            if (r >= 290f && r <= 360f) return false;

            // 2. Central Church Peak Exclusion
            if (r < 65f) return false;

            // 3. Island Outer Ocean/Cliffs Exclusion
            if (r > 750f) return false;

            // 4. Outside Kingdom (R > 290m): River & Biome Validation
            if (r > 290f)
            {
                // River exclusion: 8 radiating rivers at 0, 45, 90, 135, 180, 225, 270, 315 deg
                float ang = Mathf.Atan2(pos.x, pos.z) * Mathf.Rad2Deg;
                if (ang < 0f) ang += 360f;
                float mod45 = ang % 45f;
                float deltaAngle = Mathf.Min(mod45, 45f - mod45);
                float distToRiver = r * Mathf.Sin(deltaAngle * Mathf.Deg2Rad);
                if (distToRiver < 42f) return false; // Strict 42m river + bank buffer!

                // SW Desert Biome Exclusion (x < -30 and z < -30) -> STRICTLY ZERO TREES!
                if (pos.x < -30f && pos.z < -30f) return false;

                // NE Volcanic Caldera Exclusion (x > 30 and z > 30) -> No lush green trees in lava/obsidian
                if (pos.x > 30f && pos.z > 30f) return false;

                // Must be in Northwest Mistwood or Southeast Meadows
                bool isNWMistwood = (pos.x < -30f && pos.z > 30f);
                bool isSEMeadows = (pos.x > 30f && pos.z < -30f);
                if (!isNWMistwood && !isSEMeadows) return false;
            }

            return true;
        }

        private static void PlantDenseCluster(Transform parent, Vector3 center, int treeCount, int bushCount, float clusterRadius,
            List<GameObject> treePrefabs, List<GameObject> bushPrefabs, ref int totalTrees, ref int totalBushes)
        {
            GameObject clusterObj = new GameObject($"DenseCluster_{center.x:F0}_{center.z:F0}");
            clusterObj.transform.SetParent(parent, false);

            for (int i = 0; i < treeCount; i++)
            {
                float ang = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = UnityEngine.Random.Range(2.5f, clusterRadius);
                Vector3 candidate = center + new Vector3(Mathf.Sin(ang) * dist, 0f, Mathf.Cos(ang) * dist);

                if (!IsValidGreenLandLocation(candidate)) continue;

                float scale = (UnityEngine.Random.value < 0.15f)
                    ? UnityEngine.Random.Range(0.85f, 1.05f) // Grand ancient tree
                    : UnityEngine.Random.Range(0.50f, 0.75f);

                PlantSingleItem(clusterObj.transform, candidate, treePrefabs, scale, scale, ref totalTrees);
            }

            for (int i = 0; i < bushCount; i++)
            {
                float ang = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = UnityEngine.Random.Range(1.5f, clusterRadius * 0.9f);
                Vector3 candidate = center + new Vector3(Mathf.Sin(ang) * dist, 0f, Mathf.Cos(ang) * dist);
                if (!IsValidGreenLandLocation(candidate)) continue;

                PlantSingleBush(clusterObj.transform, candidate, bushPrefabs, ref totalBushes);
            }
        }

        private static void PlantScatteredTrees(Transform parent, Vector2 xRange, Vector2 zRange, int count,
            List<GameObject> treePrefabs, List<GameObject> bushPrefabs, ref int totalTrees, ref int totalBushes)
        {
            GameObject scatteredObj = new GameObject($"ScatteredTrees_{xRange.x:F0}_{zRange.x:F0}");
            scatteredObj.transform.SetParent(parent, false);

            int attempts = 0;
            int placed = 0;
            while (placed < count && attempts < count * 3)
            {
                attempts++;
                float x = UnityEngine.Random.Range(Mathf.Min(xRange.x, xRange.y), Mathf.Max(xRange.x, xRange.y));
                float z = UnityEngine.Random.Range(Mathf.Min(zRange.x, zRange.y), Mathf.Max(zRange.x, zRange.y));
                Vector3 candidate = new Vector3(x, 0f, z);

                if (!IsValidGreenLandLocation(candidate)) continue;

                float scale = UnityEngine.Random.Range(0.52f, 0.78f);
                PlantSingleItem(scatteredObj.transform, candidate, treePrefabs, scale, scale, ref totalTrees);
                placed++;

                if (bushPrefabs.Count > 0 && UnityEngine.Random.value < 0.4f)
                {
                    Vector3 bushPos = candidate + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f));
                    PlantSingleBush(scatteredObj.transform, bushPos, bushPrefabs, ref totalBushes);
                }
            }
        }

        private static void PlantSingleItem(Transform parent, Vector3 targetPos, List<GameObject> prefabs, float minScale, float maxScale, ref int count)
        {
            // Sample terrain height via raycast from 250m above
            float groundY = targetPos.y;
            Vector3 rayOrigin = new Vector3(targetPos.x, 250f, targetPos.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 500f))
            {
                groundY = hit.point.y;
                // Reject if submerged under water surface
                if (groundY <= 0.8f) return;
            }

            var prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = new Vector3(targetPos.x, groundY, targetPos.z);
            instance.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            float s = UnityEngine.Random.Range(minScale, maxScale);
            instance.transform.localScale = Vector3.one * s;
            count++;
        }


        private static void PlantSingleBush(Transform parent, Vector3 targetPos, List<GameObject> prefabs, ref int count)
        {
            float groundY = targetPos.y;
            Vector3 rayOrigin = new Vector3(targetPos.x, 250f, targetPos.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 500f))
            {
                groundY = hit.point.y;
            }

            var prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = new Vector3(targetPos.x, groundY, targetPos.z);
            instance.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            instance.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.40f, 0.65f);
            count++;
        }
    }
}

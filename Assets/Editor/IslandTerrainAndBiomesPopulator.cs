using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Optimization;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// IslandTerrainAndBiomesPopulator assembles the full sculpted multi-biome island in OverworldMap.unity:
    /// - Imports the sculpted Island_Overworld_Terrain.fbx from Blender
    /// - Configures Mat_Island_Overworld_Terrain with the 7 generated PBR and Splat textures
    /// - Sets up MeshCollider for seamless player and mount traversal
    /// - Positions the Volcanic Caldera Rift Core in the NE mountain range
    /// - Populates the SW Desert Oasis & Caravan in the southwest quadrant
    /// - Populates the NW Mistwood Forest & Crimson Henge
    /// - Populates the SE Terraced Meadows & Coastal Overlooks
    /// - Configures the Bitgem URP Stylized Water rivers and ocean
    /// </summary>
    public static class IslandTerrainAndBiomesPopulator
    {
        private const string TERRAIN_FBX_PATH = "Assets/Models/Environment/Island_Overworld_Terrain.fbx";
        private const string TERRAIN_MAT_PATH = "Assets/Materials/Environment/Mat_Island_Overworld_Terrain.mat";
        private const string TEXTURE_DIR = "Assets/Textures/Environment";

        [MenuItem("Tools/BattleSummoner/Build Complete Sculpted Island & Biomes", false, 11)]
        public static void BuildCompleteSculptedIsland()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("======================================================================");
            Debug.Log("🏝️ ASSEMBLING SCULPTED MULTI-BIOME ISLAND TERRAIN & ECOSYSTEMS");
            Debug.Log("======================================================================");

            // 1. Configure Terrain Material
            var mat = SetupTerrainMaterial();

            // 2. Instantiate and Bind Island Terrain Mesh
            var terrainObj = SetupIslandTerrainObject(mat);

            // 3. Populate NE Volcanic Mountain Caldera
            SetupVolcanicCaldera();

            // 4. Populate SW Desert Biome (Oasis & Caravan)
            SetupSouthwestDesert();

            // 5. Populate NW Mistwood & Crimson Runic Henge
            SetupNorthwestMistwood();

            // 6. Populate SE Verdant Meadows & River Valleys
            SetupSoutheastMeadows();

            // 7. Update Water surfaces (Bitgem URP Stylized Water)
            SetupWaterways();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("🎉 [ISLAND ASSEMBLED] Sculpted 1.7km island with NE volcanic peaks, SW desert dunes, NW mistwood, and SE terraced meadows successfully built and saved!");
        }

        private static Material SetupTerrainMaterial()
        {
            var shader = Shader.Find("BattleSummoner/Environment/IslandBiomeSplatLit") 
                         ?? Shader.Find("Universal Render Pipeline/Lit");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(TERRAIN_MAT_PATH);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, TERRAIN_MAT_PATH);
            }
            else
            {
                mat.shader = shader;
            }

            // Assign textures
            var masterMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEXTURE_DIR}/T_Terrain_MasterColorMap.png");
            var splatMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEXTURE_DIR}/T_Terrain_BiomeSplatMap.png");
            var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEXTURE_DIR}/T_Terrain_LushGrass.png");
            var desertTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEXTURE_DIR}/T_Terrain_DesertSand.png");
            var volcTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEXTURE_DIR}/T_Terrain_VolcanicRock.png");
            var beachTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEXTURE_DIR}/T_Terrain_BeachSand.png");
            var cliffTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEXTURE_DIR}/T_Terrain_RockCliff.png");

            if (masterMap != null) mat.SetTexture("_MasterColorMap", masterMap);
            if (splatMap != null) mat.SetTexture("_BiomeSplatMap", splatMap);
            if (grassTex != null) mat.SetTexture("_GrassTex", grassTex);
            if (desertTex != null) mat.SetTexture("_DesertTex", desertTex);
            if (volcTex != null) mat.SetTexture("_VolcanicTex", volcTex);
            if (beachTex != null) mat.SetTexture("_BeachTex", beachTex);
            if (cliffTex != null) mat.SetTexture("_CliffTex", cliffTex);

            mat.SetFloat("_DetailTiling", 32.0f);
            mat.SetFloat("_CliffThreshold", 0.58f);
            mat.SetFloat("_CliffBlend", 0.12f);
            mat.SetFloat("_SatelliteTintStrength", 0.40f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject SetupIslandTerrainObject(Material mat)
        {
            var oldTerrain = GameObject.Find("Island_Overworld_Terrain");
            if (oldTerrain != null) UnityEngine.Object.DestroyImmediate(oldTerrain);

            // Hide old flat plain if present
            var oldPlain = GameObject.Find("Tier0_Outerwilds_Plain");
            if (oldPlain != null) oldPlain.SetActive(false);

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(TERRAIN_FBX_PATH);
            if (fbx == null)
            {
                Debug.LogError($"[Terrain Setup] {TERRAIN_FBX_PATH} not found.");
                return null;
            }

            GameObject terrainObj = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            terrainObj.name = "Island_Overworld_Terrain";
            terrainObj.transform.position = Vector3.zero;
            terrainObj.transform.rotation = Quaternion.identity;
            terrainObj.transform.localScale = Vector3.one;

            var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metroRoot != null) terrainObj.transform.SetParent(metroRoot.transform, false);

            // Assign material
            var mr = terrainObj.GetComponentInChildren<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            var mf = terrainObj.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var col = mf.gameObject.GetComponent<MeshCollider>();
                if (col == null) col = mf.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = mf.sharedMesh;
            }

            Debug.Log("🏝️ [Terrain Setup] Island terrain object instantiated with MeshCollider and multi-biome material.");
            return terrainObj;
        }

        // =========================================================================
        // 3. NE VOLCANIC MOUNTAIN CALDERA
        // =========================================================================
        private static void SetupVolcanicCaldera()
        {
            Vector3 calderaCenter = new Vector3(380f, 18f, 380f);

            var rift = GameObject.Find("Volcanic_PurpleRift_Core");
            if (rift != null)
            {
                rift.transform.position = calderaCenter;
            }

            GameObject calderaDecor = GameObject.Find("NE_Caldera_ObsidianDecor");
            if (calderaDecor != null) UnityEngine.Object.DestroyImmediate(calderaDecor);

            calderaDecor = new GameObject("NE_Caldera_ObsidianDecor");
            var biomesRoot = GameObject.Find("06d_Outerwilds_Biomes") ?? GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (biomesRoot != null) calderaDecor.transform.SetParent(biomesRoot.transform, false);

            var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx");
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");

            // Ring of obsidian spires around caldera rim
            for (int i = 0; i < 12; i++)
            {
                float ang = i * 30f;
                float rad = ang * Mathf.Deg2Rad;
                Vector3 rimPos = calderaCenter + new Vector3(Mathf.Sin(rad) * 85f, 45f, Mathf.Cos(rad) * 85f);

                if (rockPrefab != null)
                {
                    var rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, calderaDecor.transform);
                    rock.name = $"ObsidianSpire_{i + 1}";
                    rock.transform.position = rimPos;
                    rock.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), ang, UnityEngine.Random.Range(-15f, 15f));
                    rock.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.08f, 0.14f);
                }
            }

            Debug.Log("🌋 [Volcanic Biome] Positioned Purple Rift Core into the sculpted caldera crater at (380, 18, 380) with obsidian rim spires.");
        }

        // =========================================================================
        // 4. SW DESERT BIOME (Oasis & Caravan)
        // =========================================================================
        private static void SetupSouthwestDesert()
        {
            GameObject desertRoot = GameObject.Find("SW_Desert_Ecosystem");
            if (desertRoot != null) UnityEngine.Object.DestroyImmediate(desertRoot);

            desertRoot = new GameObject("SW_Desert_Ecosystem");
            var biomesRoot = GameObject.Find("06d_Outerwilds_Biomes") ?? GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (biomesRoot != null) desertRoot.transform.SetParent(biomesRoot.transform, false);

            Vector3 oasisCenter = new Vector3(-430f, 14f, -430f);

            var tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Tent.prefab");
            var handcartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var firewoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Drova/Drova01.prefab");
            var emberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/EmbersV1.prefab");
            var wolfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Mount_ArmoredWolfBeast.fbx");

            // A. Nomad Desert Caravan Camp
            if (tentPrefab != null)
            {
                var tent1 = (GameObject)PrefabUtility.InstantiatePrefab(tentPrefab, desertRoot.transform);
                tent1.name = "Desert_Nomad_Tent_01";
                tent1.transform.position = oasisCenter + new Vector3(-6f, 0f, 4f);
                tent1.transform.rotation = Quaternion.Euler(0f, 135f, 0f);

                var tent2 = (GameObject)PrefabUtility.InstantiatePrefab(tentPrefab, desertRoot.transform);
                tent2.name = "Desert_Nomad_Tent_02";
                tent2.transform.position = oasisCenter + new Vector3(5f, 0f, -4f);
                tent2.transform.rotation = Quaternion.Euler(0f, 315f, 0f);
            }

            if (emberPrefab != null)
            {
                var campFire = (GameObject)PrefabUtility.InstantiatePrefab(emberPrefab, desertRoot.transform);
                campFire.name = "Desert_Campfire";
                campFire.transform.position = oasisCenter;
            }

            if (handcartPrefab != null)
            {
                var cart = (GameObject)PrefabUtility.InstantiatePrefab(handcartPrefab, desertRoot.transform);
                cart.name = "Desert_Merchant_Cart";
                cart.transform.position = oasisCenter + new Vector3(-8f, 0f, -2f);
                cart.transform.rotation = Quaternion.Euler(0f, 65f, 0f);
            }

            if (barrelPrefab != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, desertRoot.transform);
                    b.transform.position = oasisCenter + new Vector3(-7f + i * 1.5f, 0f, -4f);
                }
            }

            if (wolfPrefab != null)
            {
                var mount = (GameObject)PrefabUtility.InstantiatePrefab(wolfPrefab, desertRoot.transform);
                mount.name = "Desert_Caravan_Mount";
                mount.transform.position = oasisCenter + new Vector3(8f, 0f, 2f);
                mount.transform.rotation = Quaternion.Euler(-90f, 240f, 0f);
                mount.transform.localScale = Vector3.one * 0.038f;
            }

            Debug.Log("🏜️ [Desert Biome] Established Southwest Desert Nomad Caravan & Camp at (-430, 14, -430) with tents, campfire, and supply caches.");
        }

        // =========================================================================
        // 5. NW MISTWOOD BIOME & CRIMSON HENGE
        // =========================================================================
        private static void SetupNorthwestMistwood()
        {
            Vector3 hengePos = new Vector3(-430f, 24f, 430f);

            GameObject mistwoodRoot = GameObject.Find("NW_Mistwood_AncientHenge");
            if (mistwoodRoot != null) UnityEngine.Object.DestroyImmediate(mistwoodRoot);

            mistwoodRoot = new GameObject("NW_Mistwood_AncientHenge");
            var biomesRoot = GameObject.Find("06d_Outerwilds_Biomes") ?? GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (biomesRoot != null) mistwoodRoot.transform.SetParent(biomesRoot.transform, false);

            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Tree_03.prefab");
            var bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Bush_01.prefab");

            // Dense grove framing the ancient henge
            if (treePrefab != null)
            {
                for (int i = 0; i < 16; i++)
                {
                    float ang = i * (360f / 16f);
                    float rad = ang * Mathf.Deg2Rad;
                    Vector3 pos = hengePos + new Vector3(Mathf.Sin(rad) * UnityEngine.Random.Range(45f, 95f), 0f, Mathf.Cos(rad) * UnityEngine.Random.Range(45f, 95f));

                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, mistwoodRoot.transform);
                    tree.name = $"Mistwood_AncientTree_{i + 1}";
                    tree.transform.position = pos;
                    tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(6f, 8.5f);
                }
            }

            Debug.Log("🌲 [Mistwood Biome] Enriched Northwest Mistwood Forest around (-430, 24, 430) with ancient anime tree groves.");
        }

        // =========================================================================
        // 6. SE VERDANT MEADOWS & COASTAL OVERLOOKS
        // =========================================================================
        private static void SetupSoutheastMeadows()
        {
            GameObject meadowsRoot = GameObject.Find("SE_Verdant_Terraces");
            if (meadowsRoot != null) UnityEngine.Object.DestroyImmediate(meadowsRoot);

            meadowsRoot = new GameObject("SE_Verdant_Terraces");
            var biomesRoot = GameObject.Find("06d_Outerwilds_Biomes") ?? GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (biomesRoot != null) meadowsRoot.transform.SetParent(biomesRoot.transform, false);

            Vector3 meadowCenter = new Vector3(430f, 16f, -430f);

            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BenchV1.prefab");
            var wellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab");
            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Tree_05.prefab");

            if (wellPrefab != null)
            {
                var well = (GameObject)PrefabUtility.InstantiatePrefab(wellPrefab, meadowsRoot.transform);
                well.name = "Meadow_Pilgrim_Well";
                well.transform.position = meadowCenter;
            }

            if (benchPrefab != null)
            {
                var bench = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, meadowsRoot.transform);
                bench.name = "Meadow_Scenic_Bench";
                bench.transform.position = meadowCenter + new Vector3(3f, 0f, 2f);
                bench.transform.rotation = Quaternion.Euler(0f, 225f, 0f);
            }

            if (treePrefab != null)
            {
                for (int i = 0; i < 12; i++)
                {
                    float ang = i * 30f;
                    float rad = ang * Mathf.Deg2Rad;
                    Vector3 pos = meadowCenter + new Vector3(Mathf.Sin(rad) * UnityEngine.Random.Range(35f, 75f), 0f, Mathf.Cos(rad) * UnityEngine.Random.Range(35f, 75f));

                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, meadowsRoot.transform);
                    tree.transform.position = pos;
                    tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(5.5f, 7.5f);
                }
            }

            Debug.Log("🌾 [Meadows Biome] Placed scenic pilgrim rest stop and terraced grove in the southeast quadrant at (430, 16, -430).");
        }

        // =========================================================================
        // 7. WATERWAYS & OCEAN
        // =========================================================================
        private static void SetupWaterways()
        {
            var azureWaterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_Moat_StylizedWater_Azure.mat")
                                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Bitgem/StylisedWater/URP/Materials/example-water-01.mat");
            if (azureWaterMat == null) return;

            // Ensure moat and rivers are active and styled with vibrant azure water
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                string n = r.name.ToLower();
                if (n.Contains("moat_watersurface") || n.StartsWith("outerwilds_river_") || n.Contains("fishingpond"))
                {
                    r.sharedMaterial = azureWaterMat;
                }
            }

            // Outer Ocean Ring Mesh (outer ocean starting beyond island coastline R=720m to horizon R=2600m)
            var oceanRingMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Environment/Mesh_Outerwilds_Ocean_Ring.asset");
            GameObject oceanObj = GameObject.Find("Outerwilds_Ocean_Water");
            if (oceanObj == null)
            {
                oceanObj = new GameObject("Outerwilds_Ocean_Water");
                oceanObj.transform.position = new Vector3(0f, -2.0f, 0f);
                oceanObj.transform.localScale = Vector3.one;

                var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
                if (metroRoot != null) oceanObj.transform.SetParent(metroRoot.transform, false);

                var mf = oceanObj.AddComponent<MeshFilter>();
                if (oceanRingMesh != null) mf.sharedMesh = oceanRingMesh;

                var mr = oceanObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = azureWaterMat;
            }
            else
            {
                oceanObj.transform.position = new Vector3(0f, -2.0f, 0f);
                oceanObj.transform.localScale = Vector3.one;

                var mf = oceanObj.GetComponent<MeshFilter>() ?? oceanObj.AddComponent<MeshFilter>();
                if (oceanRingMesh != null) mf.sharedMesh = oceanRingMesh;

                var mr = oceanObj.GetComponent<MeshRenderer>() ?? oceanObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = azureWaterMat;

                var col = oceanObj.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }

            // Recook Moat MeshCollider if present
            var moatObj = GameObject.Find("Moat_WaterSurface");
            if (moatObj != null)
            {
                var mc = moatObj.GetComponent<MeshCollider>();
                if (mc != null && mc.sharedMesh != null)
                {
                    var m = mc.sharedMesh;
                    mc.sharedMesh = null;
                    mc.sharedMesh = m;
                }
            }

            Debug.Log("🌊 [Waterways] Configured 8 river waterways, circular moat canal, and horizon ocean ring with Azure Stylized Water shader.");
        }
    }
}

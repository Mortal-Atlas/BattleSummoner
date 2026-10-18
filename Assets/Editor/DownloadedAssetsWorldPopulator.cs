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
    /// DownloadedAssetsWorldPopulator integrates the authentic downloaded Unity Asset Store packages:
    /// 1. Urban Props Pack (urban-props-pack-377016): StreetLight Modern 01 & 02, Park Benches, Trash Cans, Fire Hydrants
    /// 2. Anime Nature (anime-nature-276931): Stylized anime trees (Tree 01-08) & bushes (Bush 01-04)
    /// 3. Bitgem URP Stylized Water Shader (urp-stylized-water-shader-proto-series-187485): WaterVolume-URP material
    /// 4. Stylized Blacksmith Starter Kit (free-stylized-blacksmith-props-starter-kit-371198): Anvil, Coal Pile, Tongs, Hammer
    /// 5. CozyShops Coffee Shop & Bakery Lite (cozyshops-coffee-shop-lite-10-free-props-401100): Cake Stand, Side Tables, Stools, Teapots, Mugs
    /// 6. RPG Medieval Props Pack 01 (rpg-medieval-props-pack-01-294298): URP Barrels, Crates, Handcarts, Tables, Tents, Buckets
    /// 7. Medieval Props V2 & Classic (medieval-props-v2-258935, medieval-props-41540): Beer Barrels, Benches, Lanterns, Sacks, Carts, Banners
    /// 8. Survival Village Props (survival-village-props-free-59280): Firewood (Drova), Buckets, Fences, Pallets, Woodcutter Axes
    /// </summary>
    public static class DownloadedAssetsWorldPopulator
    {
        private const string PROPS_ROOT_NAME = "06_Lively_Street_Props";
        private const string ANIME_TREES_ROOT_NAME = "08_Anime_Nature_Trees";

        private const float Tier4Height = 18.0f;
        private const float Tier3Height = 12.0f;
        private const float Tier2Height = 6.0f;
        private const float Tier1Height = 0.0f;

        [MenuItem("Tools/BattleSummoner/Populate All Downloaded Assets Into Overworld", false, 10)]
        public static void PopulateAllDownloadedAssets()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("======================================================================");
            Debug.Log("🌿 POPULATING OVERWORLD WITH DOWNLOADED ASSET STORE PACKAGES");
            Debug.Log("======================================================================");

            // 1. Bitgem URP Stylized Water Shader
            ApplyBitgemWaterShader();

            // 2. Anime Nature Trees & Foliage
            PopulateAnimeNatureTrees();

            // 3. Urban Props Street Lamps, Benches, Hydrants
            PopulateDownloadedStreetProps();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("🎉 [POPULATION COMPLETE] All downloaded asset store packages integrated and saved into OverworldMap.unity!");
        }

        // =========================================================================
        // 1. BITGEM URP STYLIZED WATER SHADER
        // =========================================================================
        private static void ApplyBitgemWaterShader()
        {
            var bitgemWaterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bitgem/StylisedWater/URP/Materials/example-water-01.mat");
            if (bitgemWaterMat == null)
            {
                Debug.LogWarning("[Water Populator] Bitgem example-water-01.mat not found.");
                return;
            }

            int count = 0;
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                string rName = r.name.ToLower();
                if (rName.Contains("water") || rName.Contains("moat_watersurface") || rName.StartsWith("outerwilds_river_") || rName.Contains("fishingpond"))
                {
                    r.sharedMaterial = bitgemWaterMat;
                    count++;
                }
            }

            Debug.Log($"🌊 [Water Shader] Successfully applied Bitgem URP Stylized Water Shader to {count} water surfaces (Moat, Rivers, Fishing Pond)!");
        }

        // =========================================================================
        // 2. ANIME NATURE TREES (anime-nature-276931)
        // =========================================================================
        private static void PopulateAnimeNatureTrees()
        {
            // Remove older pine trees that the user disliked
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int removedOld = 0;
            foreach (var t in transforms)
            {
                if (t != null && t.name.StartsWith("Pine_"))
                {
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                    removedOld++;
                }
            }
            if (removedOld > 0)
            {
                Debug.Log($"🌲 [Anime Nature] Replaced {removedOld} older placeholder pines with high-fidelity Anime Nature trees.");
            }

            // Prepare Anime Tree prefabs
            List<GameObject> treePrefabs = new List<GameObject>();
            for (int i = 1; i <= 8; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Anime Trees/Prefabs/Tree_0{i}.prefab");
                if (p != null) treePrefabs.Add(p);
            }

            List<GameObject> bushPrefabs = new List<GameObject>();
            for (int i = 1; i <= 4; i++)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Anime Trees/Prefabs/Bush_0{i}.prefab");
                if (p != null) bushPrefabs.Add(p);
            }

            if (treePrefabs.Count == 0)
            {
                Debug.LogWarning("[Anime Nature] Tree prefabs not found in Assets/Anime Trees/Prefabs.");
                return;
            }

            // Find or create root
            GameObject oldRoot = GameObject.Find(ANIME_TREES_ROOT_NAME);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

            GameObject treesRoot = new GameObject(ANIME_TREES_ROOT_NAME);
            var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metroRoot != null) treesRoot.transform.SetParent(metroRoot.transform, false);

            int spawnedTrees = 0;

            // A. Outer Green Land Groves (Strictly NW Mistwood & SE Meadows, never SW Desert or Riverbanks)
            float[] forestAngles = new float[] { 135f, 155f, 315f, 335f };
            string[] forestNames = new string[] { "NorthWest_Mistwood_A", "NorthWest_Mistwood_B", "SouthEast_Meadows_A", "SouthEast_Meadows_B" };

            for (int f = 0; f < forestAngles.Length; f++)
            {
                float fAng = forestAngles[f];
                float fRad = Mathf.Deg2Rad * fAng;
                Vector3 center = new Vector3(Mathf.Sin(fRad) * 540f, Tier1Height, Mathf.Cos(fRad) * 540f);

                GameObject grove = new GameObject($"GreenLand_AnimeGrove_{forestNames[f]}");
                grove.transform.SetParent(treesRoot.transform, false);

                for (int t = 0; t < 28; t++)
                {
                    float tAng = t * (360f / 28f) + UnityEngine.Random.Range(-5f, 5f);
                    float tDist = UnityEngine.Random.Range(20f, 95f);
                    Vector3 offset = new Vector3(Mathf.Sin(tAng * Mathf.Deg2Rad) * tDist, 0f, Mathf.Cos(tAng * Mathf.Deg2Rad) * tDist);
                    Vector3 pos = center + offset;

                    // Strict river buffer: avoid radial rivers (every 45 degrees)
                    float angCheck = Mathf.Atan2(pos.x, pos.z) * Mathf.Rad2Deg;
                    if (angCheck < 0) angCheck += 360f;
                    float mod45 = angCheck % 45f;
                    if (Mathf.Min(mod45, 45f - mod45) < 6.5f) continue; // Skip if near river!

                    var treePrefab = treePrefabs[UnityEngine.Random.Range(0, treePrefabs.Count)];
                    GameObject treeObj = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, grove.transform);
                    treeObj.name = $"AnimeTree_{forestNames[f]}_{t + 1}";
                    treeObj.transform.position = pos;
                    float yaw = UnityEngine.Random.Range(0f, 360f);
                    treeObj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                    float scale = UnityEngine.Random.Range(0.55f, 0.85f);
                    treeObj.transform.localScale = Vector3.one * scale;

                    var cull = treeObj.GetComponent<CullableWorldProp>() ?? treeObj.AddComponent<CullableWorldProp>();
                    cull.boundingRadius = 12f;
                    spawnedTrees++;
                }

                // Add Anime Bushes around each grove
                if (bushPrefabs.Count > 0)
                {
                    for (int b = 0; b < 14; b++)
                    {
                        float bAng = UnityEngine.Random.Range(0f, 360f);
                        float bDist = UnityEngine.Random.Range(15f, 85f);
                        Vector3 bPos = center + new Vector3(Mathf.Sin(bAng * Mathf.Deg2Rad) * bDist, 0f, Mathf.Cos(bAng * Mathf.Deg2Rad) * bDist);

                        var bushPrefab = bushPrefabs[UnityEngine.Random.Range(0, bushPrefabs.Count)];
                        GameObject bushObj = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, grove.transform);
                        bushObj.name = $"AnimeBush_{forestNames[f]}_{b + 1}";
                        bushObj.transform.position = bPos;
                        bushObj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        bushObj.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.40f, 0.65f);
                    }
                }
            }

            Debug.Log($"🍃 [Anime Nature] Spawned {spawnedTrees} anime trees and lush bushes exclusively on Green Land (NW Mistwood and SE Meadows)!");

        }

        // =========================================================================
        // 3. URBAN PROPS, BLACKSMITH, COZYSHOPS, SURVIVAL, MEDIEVAL PROPS
        // =========================================================================
        private static void PopulateDownloadedStreetProps()
        {
            GameObject oldProps = GameObject.Find(PROPS_ROOT_NAME);
            if (oldProps != null) UnityEngine.Object.DestroyImmediate(oldProps);

            GameObject propsRoot = new GameObject(PROPS_ROOT_NAME);
            var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metroRoot != null) propsRoot.transform.SetParent(metroRoot.transform, false);

            // Load exact downloaded prefabs
            // 1. Urban Props Pack
            var lampUrban1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_StreetLight_Modern_01.prefab");
            var lampUrban2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_StreetLight_Modern_02.prefab");
            var benchUrban = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_Park_Bench_01.prefab");
            var trashUrban = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_TrashCan_01.prefab");
            var hydrantUrban = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_FireHydrant_01.prefab");
            var barrierUrban = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_Barrier_01.prefab");

            // 2. Stylized Blacksmith Props Starter Kit
            var anvilImported = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpDraftArt/BlackSmith Props Sampler/Prefabs/Anvil.prefab");
            var coalImported = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpDraftArt/BlackSmith Props Sampler/Prefabs/Coal Pile.prefab");
            var tongsImported = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpDraftArt/BlackSmith Props Sampler/Prefabs/Flat Tongs.prefab");
            var hammerImported = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpDraftArt/BlackSmith Props Sampler/Prefabs/Small Hammer.prefab");

            // 3. CozyShops Bakery & Coffee Shop Props
            var cakeStand = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sari3D/CozyShops_Lite/Prefabs/P_CozyShops_CakeStand.prefab");
            var sideTable = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sari3D/CozyShops_Lite/Prefabs/P_CozyShops_SideTable.prefab");
            var stool = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sari3D/CozyShops_Lite/Prefabs/P_CozyShops_Stool.prefab");
            var teapot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sari3D/CozyShops_Lite/Prefabs/P_CozyShops_Teapot.prefab");
            var cupMug = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sari3D/CozyShops_Lite/Prefabs/P_CozyShops_Cup_Mug.prefab");
            var pottedPlant = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sari3D/CozyShops_Lite/Prefabs/P_CozyShops_PottedPlant.prefab");

            // 4. Survival Village Props
            var drova1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Drova/Drova01.prefab");
            var drova2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Drova/Drova02.prefab");
            var bucket = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/bucket01.prefab");
            var fence = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Fense/Fense01.prefab");
            var pallet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Pallets/Pallet_01.prefab");
            var axeWood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Topor_props.prefab");

            // 5. RPG Medieval Props Pack 01 & Medieval props v2
            var barrelUrp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var crate1Urp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            var crate2Urp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_02.prefab");
            var crate3Urp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_03.prefab");
            var handcartUrp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var tableUrp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Old_Table.prefab");
            var tentUrp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Tent.prefab");
            var beerBarrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BeerBarrelV1.prefab");
            var medBench = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BenchV1.prefab");
            var medLantern = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/LanternV1.prefab");
            var medSack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/SackV1.prefab");
            var cartV2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/propsv2/cart1_low.prefab");
            var flagV2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/propsv2/flag1_low.prefab");

            // Procedural Kits for specialized magic & masonry
            var spellbookLectern = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Wizard_SpellbookLectern.prefab");
            var crystalPedestal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Wizard_CrystalPedestal.prefab");
            var distillationBench = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Alchemy_DistillationBench.prefab");
            var mortarPestle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Alchemy_MortarPestleRack.prefab");
            var villageWell = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab");
            var noticeBoard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab");
            var weaponRack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab");
            var tavernSet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab");

            int propCount = 0;

            // ---------------------------------------------------------------------
            // A. URBAN PROPS PACK: STREET LAMPS (urban-props-pack-377016)
            // ---------------------------------------------------------------------
            var lampPrefab = lampUrban1 ?? lampUrban2;
            if (lampPrefab != null)
            {
                // 1. Citadel Promenade Circle (Tier 4, R=32m)
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * 45f;
                    float rad = ang * Mathf.Deg2Rad;
                    Vector3 pos = new Vector3(Mathf.Sin(rad) * 32f, Tier4Height, Mathf.Cos(rad) * 32f);
                    SpawnUrbanLamp(lampPrefab, propsRoot.transform, pos, Quaternion.Euler(0f, ang + 180f, 0f));
                    propCount++;
                }

                // 2. 8 Grand Radial Avenues (flanking curbs at R=75, 105, 155, 210, 255, 285m)
                float[] avenueRadii = new float[] { 75f, 105f, 155f, 210f, 255f, 285f };
                for (int a = 0; a < 8; a++)
                {
                    float avenueAngle = a * 45f;
                    float rad = avenueAngle * Mathf.Deg2Rad;
                    Vector3 forwardDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                    Vector3 sideDir = new Vector3(forwardDir.z, 0f, -forwardDir.x);

                    foreach (float r in avenueRadii)
                    {
                        float h = r < 90f ? Tier3Height : (r < 190f ? Tier2Height : Tier1Height);
                        Vector3 centerPos = forwardDir * r; centerPos.y = h;

                        // Left curb lamp
                        Vector3 leftPos = centerPos - sideDir * 6.5f;
                        SpawnUrbanLamp(lampPrefab, propsRoot.transform, leftPos, Quaternion.Euler(0f, avenueAngle + 90f, 0f));

                        // Right curb lamp
                        Vector3 rightPos = centerPos + sideDir * 6.5f;
                        SpawnUrbanLamp(lampPrefab, propsRoot.transform, rightPos, Quaternion.Euler(0f, avenueAngle - 90f, 0f));
                        propCount += 2;
                    }
                }

                // 3. 4 Bastion Gates and Moat Bridges (R=305m)
                Vector3[] bridgeGates = new Vector3[]
                {
                    new Vector3(0f, Tier1Height, 305f),
                    new Vector3(0f, Tier1Height, -305f),
                    new Vector3(305f, Tier1Height, 0f),
                    new Vector3(-305f, Tier1Height, 0f)
                };
                foreach (var gPos in bridgeGates)
                {
                    Vector3 lat = (gPos.x == 0f) ? Vector3.right : Vector3.forward;
                    SpawnUrbanLamp(lampPrefab, propsRoot.transform, gPos - lat * 6f, Quaternion.identity);
                    SpawnUrbanLamp(lampPrefab, propsRoot.transform, gPos + lat * 6f, Quaternion.identity);
                    propCount += 2;
                }

                // 4. Civic Ring Boulevard Lamps (Tier 2, R=195m)
                for (int b = 0; b < 6; b++)
                {
                    float bAng = 55f + (b * 26f);
                    float bRad = bAng * Mathf.Deg2Rad;
                    Vector3 bPos = new Vector3(Mathf.Sin(bRad) * 190f, Tier2Height, Mathf.Cos(bRad) * 190f);
                    SpawnUrbanLamp(lampPrefab, propsRoot.transform, bPos, Quaternion.Euler(0f, bAng, 0f));
                    propCount++;
                }
            }

            // B. Urban Park Benches, Trash Cans, Fire Hydrants
            if (benchUrban != null)
            {
                SpawnWorldProp(benchUrban, propsRoot.transform, new Vector3(15f, Tier4Height, 30f), Quaternion.Euler(0f, 210f, 0f), 1f);
                SpawnWorldProp(benchUrban, propsRoot.transform, new Vector3(-15f, Tier4Height, 30f), Quaternion.Euler(0f, 150f, 0f), 1f);
                SpawnWorldProp(benchUrban, propsRoot.transform, new Vector3(145f, Tier2Height, -135f), Quaternion.Euler(0f, 45f, 0f), 1f);
                SpawnWorldProp(benchUrban, propsRoot.transform, new Vector3(150f, Tier2Height, -140f), Quaternion.Euler(0f, 45f, 0f), 1f);
                propCount += 4;
            }
            if (trashUrban != null)
            {
                SpawnWorldProp(trashUrban, propsRoot.transform, new Vector3(18f, Tier4Height, 28f), Quaternion.identity, 1f);
                SpawnWorldProp(trashUrban, propsRoot.transform, new Vector3(143f, Tier2Height, -133f), Quaternion.identity, 1f);
                propCount += 2;
            }
            if (hydrantUrban != null)
            {
                SpawnWorldProp(hydrantUrban, propsRoot.transform, new Vector3(7f, Tier1Height, 295f), Quaternion.identity, 1f);
                SpawnWorldProp(hydrantUrban, propsRoot.transform, new Vector3(-7f, Tier1Height, -295f), Quaternion.identity, 1f);
                propCount += 2;
            }

            // ---------------------------------------------------------------------
            // B. STYLIZED BLACKSMITH STARTER KIT (free-stylized-blacksmith-props-starter-kit-371198)
            // ---------------------------------------------------------------------
            Vector3 torvaldPos = new Vector3(115f, Tier3Height, 10f);
            if (anvilImported != null)
            {
                SpawnWorldProp(anvilImported, propsRoot.transform, torvaldPos + new Vector3(-2.2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 1f);
                propCount++;
            }
            if (coalImported != null)
            {
                SpawnWorldProp(coalImported, propsRoot.transform, torvaldPos + new Vector3(-2f, 0f, -2.2f), Quaternion.identity, 1f);
                propCount++;
            }
            if (tongsImported != null)
            {
                SpawnWorldProp(tongsImported, propsRoot.transform, torvaldPos + new Vector3(-1.8f, 0.9f, 0.4f), Quaternion.Euler(0f, 45f, 0f), 1f);
                propCount++;
            }
            if (hammerImported != null)
            {
                SpawnWorldProp(hammerImported, propsRoot.transform, torvaldPos + new Vector3(-2.2f, 0.9f, -0.2f), Quaternion.Euler(0f, -30f, 0f), 1f);
                propCount++;
            }

            // Bastion Secondary Smithy
            if (anvilImported != null)
            {
                SpawnWorldProp(anvilImported, propsRoot.transform, new Vector3(12f, Tier1Height, 282f), Quaternion.Euler(0f, 180f, 0f), 1f);
                propCount++;
            }

            // ---------------------------------------------------------------------
            // C. COZYSHOPS BAKERY PROPS (cozyshops-coffee-shop-lite-10-free-props-401100)
            // ---------------------------------------------------------------------
            Vector3 pierrePos = new Vector3(25f, Tier2Height, -190f);
            if (sideTable != null)
            {
                SpawnWorldProp(sideTable, propsRoot.transform, pierrePos + new Vector3(0f, 0f, 1.8f), Quaternion.identity, 1f);
                SpawnWorldProp(sideTable, propsRoot.transform, pierrePos + new Vector3(-2.8f, 0f, 1.0f), Quaternion.identity, 1f);
                propCount += 2;
            }
            if (cakeStand != null)
            {
                SpawnWorldProp(cakeStand, propsRoot.transform, pierrePos + new Vector3(0f, 0.75f, 1.8f), Quaternion.identity, 1f);
                propCount++;
            }
            if (teapot != null)
            {
                SpawnWorldProp(teapot, propsRoot.transform, pierrePos + new Vector3(-2.8f, 0.75f, 1.0f), Quaternion.identity, 1f);
                propCount++;
            }
            if (cupMug != null)
            {
                SpawnWorldProp(cupMug, propsRoot.transform, pierrePos + new Vector3(-2.6f, 0.75f, 1.2f), Quaternion.identity, 1f);
                propCount++;
            }
            if (stool != null)
            {
                SpawnWorldProp(stool, propsRoot.transform, pierrePos + new Vector3(-3.5f, 0f, 1.0f), Quaternion.identity, 1f);
                SpawnWorldProp(stool, propsRoot.transform, pierrePos + new Vector3(-2.1f, 0f, 1.0f), Quaternion.identity, 1f);
                propCount += 2;
            }
            if (pottedPlant != null)
            {
                SpawnWorldProp(pottedPlant, propsRoot.transform, pierrePos + new Vector3(1.8f, 0f, 1.5f), Quaternion.identity, 1f);
                propCount++;
            }

            // ---------------------------------------------------------------------
            // D. SURVIVAL VILLAGE PROPS (survival-village-props-free-59280)
            // ---------------------------------------------------------------------
            if (drova1 != null || drova2 != null)
            {
                var dPrefab = drova1 ?? drova2;
                SpawnWorldProp(dPrefab, propsRoot.transform, pierrePos + new Vector3(2.8f, 0f, -2.0f), Quaternion.Euler(0f, 90f, 0f), 1f);
                SpawnWorldProp(dPrefab, propsRoot.transform, torvaldPos + new Vector3(2.8f, 0f, -1.0f), Quaternion.Euler(0f, 90f, 0f), 1f);
                SpawnWorldProp(dPrefab, propsRoot.transform, new Vector3(85f, Tier2Height, -145f), Quaternion.identity, 1f);
                SpawnWorldProp(dPrefab, propsRoot.transform, new Vector3(-85f, Tier2Height, -145f), Quaternion.identity, 1f);
                propCount += 4;
            }
            if (bucket != null)
            {
                SpawnWorldProp(bucket, propsRoot.transform, torvaldPos + new Vector3(-2.0f, 0f, 1.8f), Quaternion.identity, 1f);
                SpawnWorldProp(bucket, propsRoot.transform, new Vector3(142f, Tier2Height, -123f), Quaternion.identity, 1f);
                propCount += 2;
            }
            if (pallet != null)
            {
                SpawnWorldProp(pallet, propsRoot.transform, new Vector3(135f, Tier2Height, -145f), Quaternion.identity, 1f);
                SpawnWorldProp(pallet, propsRoot.transform, new Vector3(-135f, Tier2Height, -145f), Quaternion.identity, 1f);
                propCount += 2;
            }
            if (axeWood != null)
            {
                SpawnWorldProp(axeWood, propsRoot.transform, torvaldPos + new Vector3(3.2f, 0f, -0.5f), Quaternion.identity, 1f);
                propCount++;
            }

            // ---------------------------------------------------------------------
            // E. RPG MEDIEVAL PROPS PACK 01 & MEDIEVAL PROPS V2
            // ---------------------------------------------------------------------
            if (barrelUrp != null || beerBarrel != null)
            {
                var bPrefab = barrelUrp ?? beerBarrel;
                for (int b = 0; b < 10; b++)
                {
                    float bAng = 65f + b * 22f;
                    float rad = bAng * Mathf.Deg2Rad;
                    Vector3 bPos = new Vector3(Mathf.Sin(rad) * 192f, Tier2Height, Mathf.Cos(rad) * 192f);
                    SpawnWorldProp(bPrefab, propsRoot.transform, bPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f), 1f);
                    propCount++;
                }
            }

            if (crate1Urp != null || crate2Urp != null)
            {
                var cPrefab = crate1Urp ?? crate2Urp;
                for (int c = 0; c < 8; c++)
                {
                    float cAng = 70f + c * 25f;
                    float rad = cAng * Mathf.Deg2Rad;
                    Vector3 cPos = new Vector3(Mathf.Sin(rad) * 197f, Tier2Height, Mathf.Cos(rad) * 197f);
                    SpawnWorldProp(cPrefab, propsRoot.transform, cPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f), 1f);
                    propCount++;
                }
            }

            if (handcartUrp != null || cartV2 != null)
            {
                var cartPrefab = handcartUrp ?? cartV2;
                SpawnWorldProp(cartPrefab, propsRoot.transform, new Vector3(138f, Tier2Height, -130f), Quaternion.Euler(0f, 35f, 0f), 1f);
                SpawnWorldProp(cartPrefab, propsRoot.transform, new Vector3(10f, Tier1Height, 285f), Quaternion.Euler(0f, 180f, 0f), 1f);
                propCount += 2;
            }

            if (tentUrp != null)
            {
                SpawnWorldProp(tentUrp, propsRoot.transform, new Vector3(150f, Tier2Height, -115f), Quaternion.Euler(0f, 45f, 0f), 1f);
                SpawnWorldProp(tentUrp, propsRoot.transform, new Vector3(125f, Tier2Height, -155f), Quaternion.Euler(0f, 315f, 0f), 1f);
                propCount += 2;
            }

            if (flagV2 != null)
            {
                SpawnWorldProp(flagV2, propsRoot.transform, new Vector3(6f, Tier1Height, 302f), Quaternion.identity, 1f);
                SpawnWorldProp(flagV2, propsRoot.transform, new Vector3(-6f, Tier1Height, 302f), Quaternion.identity, 1f);
                SpawnWorldProp(flagV2, propsRoot.transform, new Vector3(6f, Tier1Height, -302f), Quaternion.identity, 1f);
                SpawnWorldProp(flagV2, propsRoot.transform, new Vector3(-6f, Tier1Height, -302f), Quaternion.identity, 1f);
                propCount += 4;
            }

            // ---------------------------------------------------------------------
            // F. WIZARD, ALCHEMY, VILLAGE WELLS & WEAPON RACKS
            // ---------------------------------------------------------------------
            Vector3 vesperaPos = new Vector3(0f, Tier4Height, 35f);
            Vector3 lysandraPos = new Vector3(-105f, Tier3Height, 35f);
            Vector3 theresaPos = new Vector3(12f, Tier4Height, -15f);
            Vector3 lyraPos = new Vector3(45f, Tier3Height, -45f);

            if (spellbookLectern != null)
            {
                SpawnWorldProp(spellbookLectern, propsRoot.transform, vesperaPos + new Vector3(1.6f, 0f, -0.6f), Quaternion.Euler(0f, 200f, 0f), 1f);
                SpawnWorldProp(spellbookLectern, propsRoot.transform, lysandraPos + new Vector3(1.8f, 0f, -0.8f), Quaternion.Euler(0f, 150f, 0f), 1f);
                propCount += 2;
            }

            if (crystalPedestal != null)
            {
                SpawnWorldProp(crystalPedestal, propsRoot.transform, vesperaPos + new Vector3(-2.8f, 0f, 0.5f), Quaternion.identity, 1f);
                SpawnWorldProp(crystalPedestal, propsRoot.transform, vesperaPos + new Vector3(2.8f, 0f, 0.5f), Quaternion.identity, 1f);
                SpawnWorldProp(crystalPedestal, propsRoot.transform, new Vector3(0f, Tier4Height, 0f), Quaternion.identity, 1f);
                SpawnWorldProp(crystalPedestal, propsRoot.transform, lysandraPos + new Vector3(-2.5f, 0f, 1.5f), Quaternion.identity, 1f);
                propCount += 4;
            }

            if (distillationBench != null)
            {
                SpawnWorldProp(distillationBench, propsRoot.transform, lysandraPos + new Vector3(0f, 0f, 2.2f), Quaternion.Euler(0f, 180f, 0f), 1f);
                propCount++;
            }

            if (mortarPestle != null)
            {
                SpawnWorldProp(mortarPestle, propsRoot.transform, lysandraPos + new Vector3(-2.2f, 0f, -1.2f), Quaternion.Euler(0f, 45f, 0f), 1f);
                propCount++;
            }

            if (villageWell != null)
            {
                SpawnWorldProp(villageWell, propsRoot.transform, new Vector3(142f, Tier2Height, -125f), Quaternion.identity, 1f);
                SpawnWorldProp(villageWell, propsRoot.transform, new Vector3(-142f, Tier2Height, -125f), Quaternion.identity, 1f);
                SpawnWorldProp(villageWell, propsRoot.transform, new Vector3(0f, Tier1Height, 175f), Quaternion.identity, 1f);
                propCount += 3;
            }

            if (noticeBoard != null)
            {
                SpawnWorldProp(noticeBoard, propsRoot.transform, new Vector3(7.5f, Tier3Height, 114.5f), Quaternion.Euler(0f, 180f, 0f), 1f);
                SpawnWorldProp(noticeBoard, propsRoot.transform, new Vector3(138f, Tier2Height, -138f), Quaternion.Euler(0f, 315f, 0f), 1f);
                SpawnWorldProp(noticeBoard, propsRoot.transform, new Vector3(6f, Tier1Height, 286f), Quaternion.Euler(0f, 180f, 0f), 1f);
                SpawnWorldProp(noticeBoard, propsRoot.transform, new Vector3(6f, Tier1Height, -286f), Quaternion.Euler(0f, 0f, 0f), 1f);
                propCount += 4;
            }

            if (weaponRack != null)
            {
                SpawnWorldProp(weaponRack, propsRoot.transform, theresaPos + new Vector3(2.5f, 0f, 1.0f), Quaternion.Euler(0f, 210f, 0f), 1f);
                SpawnWorldProp(weaponRack, propsRoot.transform, theresaPos + new Vector3(-2.5f, 0f, 1.0f), Quaternion.Euler(0f, 150f, 0f), 1f);
                SpawnWorldProp(weaponRack, propsRoot.transform, torvaldPos + new Vector3(1.5f, 0f, 2.5f), Quaternion.Euler(0f, 90f, 0f), 1f);
                SpawnWorldProp(weaponRack, propsRoot.transform, new Vector3(-5f, Tier1Height, 288f), Quaternion.Euler(0f, 180f, 0f), 1f);
                SpawnWorldProp(weaponRack, propsRoot.transform, new Vector3(-5f, Tier1Height, -288f), Quaternion.identity, 1f);
                propCount += 5;
            }

            if (tavernSet != null)
            {
                SpawnWorldProp(tavernSet, propsRoot.transform, lyraPos + new Vector3(0f, 0f, 3.8f), Quaternion.Euler(0f, 180f, 0f), 1f);
                SpawnWorldProp(tavernSet, propsRoot.transform, lyraPos + new Vector3(-3.5f, 0f, -1.0f), Quaternion.Euler(0f, 75f, 0f), 1f);
                SpawnWorldProp(tavernSet, propsRoot.transform, lyraPos + new Vector3(3.5f, 0f, -1.0f), Quaternion.Euler(0f, -75f, 0f), 1f);
                SpawnWorldProp(tavernSet, propsRoot.transform, new Vector3(65f, Tier1Height, 65f), Quaternion.identity, 1f);
                SpawnWorldProp(tavernSet, propsRoot.transform, new Vector3(72f, Tier1Height, 68f), Quaternion.Euler(0f, 30f, 0f), 1f);
                propCount += 5;
            }

            Debug.Log($"🏮 [Street Props] Populated {propCount} authentic asset store props and urban street lamps across the city!");
        }

        private static GameObject SpawnUrbanLamp(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            obj.transform.localScale = Vector3.one;

            // Ensure Point Light attached for warm illumination
            var light = obj.GetComponentInChildren<Light>();
            if (light == null)
            {
                var lightObj = new GameObject("PointLight_Lantern");
                lightObj.transform.SetParent(obj.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 3.4f, 0.35f);

                light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1.0f, 0.74f, 0.38f);
                light.intensity = 2.2f;
                light.range = 9.5f;
                light.shadows = LightShadows.None; // Zero-cost mobile lighting
            }

            // Ensure CullableWorldProp
            var cull = obj.GetComponent<CullableWorldProp>() ?? obj.AddComponent<CullableWorldProp>();
            cull.boundingRadius = 8.0f;
            cull.isDynamicMover = false;

            return obj;
        }

        private static GameObject SpawnWorldProp(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            obj.transform.localScale = Vector3.one * scale;

            var cull = obj.GetComponent<CullableWorldProp>() ?? obj.AddComponent<CullableWorldProp>();
            cull.boundingRadius = 4.0f;
            cull.isDynamicMover = false;

            return obj;
        }
    }
}

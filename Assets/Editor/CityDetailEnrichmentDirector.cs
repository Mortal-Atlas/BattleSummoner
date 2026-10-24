using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// CityDetailEnrichmentDirector dresses and enriches all districts of the Solas Metropolis:
    /// - Cleans duplicate roots, legacy orphan models, and broken missing prefabs.
    /// - Replaces broken legacy market squares with verified, vibrant market hubs.
    /// - District I: Citadel Acropolis & Celestial Astrolabe Plaza (Sakura Planters, Flower Benches, Mondstadt Street Lamps, Weapon Racks).
    /// - District II: Grand Bazaar Central & Merchant Row (Booths, Tents, Handcarts, Produce Sets, Pottery, Bakery Storefront).
    /// - District III: Western Craftsmen Forge & Tavern Terrace (Anvil, Coal Basin, Grindstone, Quench Trough, Dining Tables, Ale Barrels, Village Well).
    /// - District III-B: Eastern Artisan Quarter (Barnaby's Open Forge & Tavern Plaza).
    /// - District IV: Arcane Enclave & Margo's Sanctuary (Crystal Pedestals, Spellbook Lecterns, Cauldrons, Distillation Bench, Botanical Flowerbeds).
    /// - District V: Seaward Harbor Vista & Cargo Pier (Lookout Benches, Palettes, Heavy Crates, Mooring Ropes, Barrels, Maritime Lanterns).
    /// - District VI: Grand Guildhall & Martial Armory (Notice Boards, Weapon Racks, Supply Crates, Benches).
    /// - District VII: Southern Gatehouse Bastion (Zephyr's Outpost, Sentry Racks, Frontier Provisions).
    /// - Radial Avenues & Shaded Boulevards: Warm street lamps and Anime Broadleaf/Oak shade trees.
    /// </summary>
    public static class CityDetailEnrichmentDirector
    {
        private const string ScenePath = "Assets/Scenes/OverworldMap.unity";
        private const string RootName = "--- METROPOLIS_DISTRICT_DETAILS ---";

        [MenuItem("BattleSummoner/City/Enrich Metropolis Districts With Full Detail", false, 1)]
        public static void EnrichCityDistricts()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log("<color=cyan><b>🏰 [CITY ENRICHMENT] Starting comprehensive metropolis detail pass...</b></color>");

            // 1. Clean legacy duplicates, orphan swords, and broken missing prefabs
            CleanLegacyDuplicatesAndOrphans();
            CleanAndRebuildBrokenMarketSquares();
            FixSingleMissingDrova();

            // 2. Find or create master detail root
            GameObject masterRoot = GameObject.Find(RootName);
            if (masterRoot != null)
            {
                GameObject.DestroyImmediate(masterRoot);
            }
            masterRoot = new GameObject(RootName);

            // 3. Populate all districts
            DressDistrictI_CitadelAcropolisAndAstrolabe(masterRoot.transform);
            DressDistrictII_GrandBazaarCentral(masterRoot.transform);
            DressDistrictIII_WesternForgeAndTavern(masterRoot.transform);
            DressDistrictIIIB_EasternArtisanQuarter(masterRoot.transform);
            DressDistrictIV_ArcaneEnclaveAndMargo(masterRoot.transform);
            DressDistrictV_SeawardHarborVista(masterRoot.transform);
            DressDistrictVI_GrandGuildhall(masterRoot.transform);
            DressDistrictVII_SouthernGatehouseBastion(masterRoot.transform);
            DressRadialAvenuesAndBoulevards(masterRoot.transform);

            // 4. Save scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=green><b>🎉 [CITY ENRICHMENT] Solas Metropolis fully dressed, detailed, and saved!</b></color>");
        }

        private static void CleanLegacyDuplicatesAndOrphans()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();

            int removedSwords = 0;
            int removedDecor = 0;
            int removedSky = 0;

            int decorSeen = 0;
            int skySeen = 0;

            foreach (var r in roots)
            {
                if (r == null) continue;

                if (r.name == "Equipped_sword")
                {
                    Undo.DestroyObjectImmediate(r);
                    removedSwords++;
                }
                else if (r.name == "Grand_Metropolis_StreetLife_And_Decor")
                {
                    decorSeen++;
                    if (decorSeen > 1)
                    {
                        Undo.DestroyObjectImmediate(r);
                        removedDecor++;
                    }
                }
                else if (r.name == "Grand_Sky_Archipelago_System")
                {
                    skySeen++;
                    if (skySeen > 1)
                    {
                        Undo.DestroyObjectImmediate(r);
                        removedSky++;
                    }
                }
            }

            Debug.Log($"🧹 [CITY HYGIENE] Cleaned {removedSwords} orphan swords, {removedDecor} duplicate decor roots, {removedSky} duplicate sky roots.");
        }

        private static void CleanAndRebuildBrokenMarketSquares()
        {
            var beautyRoot = GameObject.Find("8Vector_Metropolis_Root/06_Metropolis_Beauty_Enrichment");
            if (beautyRoot == null) return;

            var oldBazaars = beautyRoot.transform.Find("Bustling_Market_Bazaars");
            if (oldBazaars != null)
            {
                GameObject.DestroyImmediate(oldBazaars.gameObject);
            }

            var newBazaars = new GameObject("Bustling_Market_Bazaars");
            newBazaars.transform.SetParent(beautyRoot.transform, false);

            // Load verified replacement assets
            var boothPrefab = LoadPrefab("Assets/Prefabs/Overworld/Market_Booth.prefab");
            var tentPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Tent.prefab");
            var tablePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Old_Table.prefab");
            var cratesPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
            var barrelPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var cratePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            var cartPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var benchPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var clayPotPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Clay Pot.prefab");

            Vector3[] squareCoords = new Vector3[]
            {
                new Vector3(-130f, 40.78f, -95f),
                new Vector3(-105f, 39.80f, -135f),
                new Vector3(250f, 23.12f, 70f),
                new Vector3(-240f, 25.89f, 90f),
                new Vector3(180f, 8.24f, 350f),
                new Vector3(-280f, 9.58f, -280f)
            };

            for (int i = 0; i < squareCoords.Length; i++)
            {
                Vector3 center = squareCoords[i];
                var sqGo = new GameObject($"MarketSquare_{center.x}_{center.z}");
                sqGo.transform.SetParent(newBazaars.transform, false);
                sqGo.transform.position = center;

                var mainStructure = (i % 2 == 0) ? boothPrefab : tentPrefab;
                float structScale = (i % 2 == 0) ? 1.35f : 1.0f;
                float structYaw = (i % 2 == 0) ? 0f : 180f; // Booth front counter faces plaza (-Z), Tent entrance faces plaza
                float structYOffset = (i % 2 == 0) ? (0.87f * 1.35f) : 0f; // Booth pivot is at center, offset Y so feet rest on ground

                SpawnSnapped(mainStructure, sqGo, center + new Vector3(0f, structYOffset, 3.5f), structYaw, structScale);
                SpawnSnapped(tablePrefab, sqGo, center + new Vector3(-2.5f, 0f, 0.5f), 90f, 1.0f);
                SpawnSnapped(clayPotPrefab, sqGo, center + new Vector3(-2.5f, 0f, 0.8f), 0f, 0.9f);
                SpawnSnapped(cratesPrefab, sqGo, center + new Vector3(2.5f, 0f, 1.2f), -15f, 1.0f);
                SpawnSnapped(barrelPrefab, sqGo, center + new Vector3(-3.2f, 0f, -2.0f), 0f, 1.0f);
                SpawnSnapped(barrelPrefab, sqGo, center + new Vector3(-3.8f, 0f, -1.5f), 35f, 1.0f);
                SpawnSnapped(cratePrefab, sqGo, center + new Vector3(3.2f, 0f, -2.2f), 15f, 1.0f);
                SpawnSnapped(cartPrefab, sqGo, center + new Vector3(0.5f, 0f, -3.5f), 160f, 1.0f);
                SpawnSnapped(benchPrefab, sqGo, center + new Vector3(-4.5f, 0f, 0f), 90f, 0.95f);
                SpawnSnapped(lampPrefab, sqGo, center + new Vector3(4.5f, 0f, 0f), -45f, 0.9f);
            }

            Debug.Log($"✨ [MARKET REBUILD] Rebuilt {squareCoords.Length} regional market squares with clean, verified props!");
        }

        private static void FixSingleMissingDrova()
        {
            var drova = GameObject.Find("Seaward_Port_Town_Lively/04_Shipwright_Blacksmith_Forge/Drova01");
            if (drova != null)
            {
                Transform parent = drova.transform.parent;
                Vector3 pos = drova.transform.position;
                Quaternion rot = drova.transform.rotation;
                GameObject.DestroyImmediate(drova);

                var woodPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab");
                if (woodPrefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(woodPrefab, parent);
                    inst.name = "FirewoodStack_Port";
                    inst.transform.position = pos;
                    inst.transform.rotation = rot;
                }
                Debug.Log("🪵 [PORT HYGIENE] Replaced missing Drova01 with valid PFB_Medieval_FirewoodStack!");
            }
        }

        #region District I: Citadel Acropolis & Celestial Astrolabe Plaza
        private static void DressDistrictI_CitadelAcropolisAndAstrolabe(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "District_I_Citadel_Acropolis_And_Astrolabe");

            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var benchPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            var planterPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_SakuraStonePlanterRing.prefab");
            var weaponRackPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab");
            var goldFlowersPrefab = LoadPrefab("Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab");
            var azureFlowersPrefab = LoadPrefab("Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab");

            // A. Celestial Astrolabe Plaza at (0, 80, 26)
            Vector3 astrolabePos = new Vector3(0f, 80f, 26f);
            float rAstrolabe = 8.5f;

            // 4 Benches framing the Astrolabe facing inward
            SpawnSnapped(benchPrefab, dRoot, astrolabePos + new Vector3(0f, 0f, -rAstrolabe), 0f, 1.05f);
            SpawnSnapped(benchPrefab, dRoot, astrolabePos + new Vector3(0f, 0f, rAstrolabe), 180f, 1.05f);
            SpawnSnapped(benchPrefab, dRoot, astrolabePos + new Vector3(-rAstrolabe, 0f, 0f), 90f, 1.05f);
            SpawnSnapped(benchPrefab, dRoot, astrolabePos + new Vector3(rAstrolabe, 0f, 0f), -90f, 1.05f);

            // 4 Corner Street Lamps illuminating the Astrolabe dais
            float rCorner = 7.2f;
            SpawnSnapped(lampPrefab, dRoot, astrolabePos + new Vector3(-rCorner, 0f, -rCorner), 45f, 0.95f);
            SpawnSnapped(lampPrefab, dRoot, astrolabePos + new Vector3(rCorner, 0f, -rCorner), -45f, 0.95f);
            SpawnSnapped(lampPrefab, dRoot, astrolabePos + new Vector3(-rCorner, 0f, rCorner), 135f, 0.95f);
            SpawnSnapped(lampPrefab, dRoot, astrolabePos + new Vector3(rCorner, 0f, rCorner), -135f, 0.95f);

            // Floral accents around the Astrolabe steps
            SpawnSnapped(goldFlowersPrefab, dRoot, astrolabePos + new Vector3(-5.5f, 0f, -5.5f), 20f, 1.0f);
            SpawnSnapped(azureFlowersPrefab, dRoot, astrolabePos + new Vector3(5.5f, 0f, -5.5f), -20f, 1.0f);
            SpawnSnapped(azureFlowersPrefab, dRoot, astrolabePos + new Vector3(-5.5f, 0f, 5.5f), 110f, 1.0f);
            SpawnSnapped(goldFlowersPrefab, dRoot, astrolabePos + new Vector3(5.5f, 0f, 5.5f), -110f, 1.0f);

            // B. Grand Cathedral Promenade (Z: -16 to -54)
            SpawnSnapped(planterPrefab, dRoot, new Vector3(-8.8f, 80f, -28f), 0f, 1.1f);
            SpawnSnapped(planterPrefab, dRoot, new Vector3(8.8f, 80f, -28f), 0f, 1.1f);
            SpawnSnapped(planterPrefab, dRoot, new Vector3(-8.8f, 80f, -42f), 0f, 1.1f);
            SpawnSnapped(planterPrefab, dRoot, new Vector3(8.8f, 80f, -42f), 0f, 1.1f);

            SpawnSnapped(benchPrefab, dRoot, new Vector3(-7.2f, 80f, -35f), 90f, 1.0f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(7.2f, 80f, -35f), -90f, 1.0f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(-7.2f, 80f, -48f), 90f, 1.0f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(7.2f, 80f, -48f), -90f, 1.0f);

            SpawnSnapped(lampPrefab, dRoot, new Vector3(-6.0f, 80f, -22f), 45f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(6.0f, 80f, -22f), -45f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-6.0f, 80f, -40f), 45f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(6.0f, 80f, -40f), -45f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-6.0f, 80f, -54f), 45f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(6.0f, 80f, -54f), -45f, 0.9f);

            // Ceremonial Guard Posts & Weapon Racks flanking cathedral portal
            SpawnSnapped(weaponRackPrefab, dRoot, new Vector3(-5.2f, 80f, -16f), 20f, 1.0f);
            SpawnSnapped(weaponRackPrefab, dRoot, new Vector3(5.2f, 80f, -16f), -20f, 1.0f);

            SpawnSnapped(goldFlowersPrefab, dRoot, new Vector3(-7.8f, 80f, -30f), 15f, 1.0f);
            SpawnSnapped(azureFlowersPrefab, dRoot, new Vector3(7.8f, 80f, -30f), -35f, 1.0f);
            SpawnSnapped(goldFlowersPrefab, dRoot, new Vector3(-7.8f, 80f, -44f), 55f, 1.0f);
            SpawnSnapped(azureFlowersPrefab, dRoot, new Vector3(7.8f, 80f, -44f), -15f, 1.0f);
        }
        #endregion

        #region District II: Grand Bazaar Central & Merchant Row
        private static void DressDistrictII_GrandBazaarCentral(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "District_II_Grand_Bazaar_Central");

            var cratesPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var chalkboardPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Bakery_MenuChalkboard.prefab");
            var pastryPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Bakery_PastryDisplay.prefab");
            var benchPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            var crimsonFlowersPrefab = LoadPrefab("Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab");
            var mixedFlowersPrefab = LoadPrefab("Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab");

            var tentPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Tent.prefab");
            var handcartPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var barrelPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var cratePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            var crate2Prefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_02.prefab");
            var boothPrefab = LoadPrefab("Assets/Prefabs/Overworld/Market_Booth.prefab");
            var tablePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Old_Table.prefab");
            var clayPotPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Clay Pot.prefab");
            var ropePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Rope.prefab");

            // Merchant Booths & Pavilions
            float boothOffset = 0.87f * 1.35f;
            SpawnSnapped(boothPrefab, dRoot, new Vector3(10.5f, 40.68f + boothOffset, -164.0f), 0f, 1.35f);
            SpawnSnapped(boothPrefab, dRoot, new Vector3(18.5f, 40.43f + boothOffset, -164.5f), 0f, 1.35f);
            SpawnSnapped(tentPrefab, dRoot, new Vector3(22.0f, 40f, -174.0f), -45f, 1.1f);
            SpawnSnapped(handcartPrefab, dRoot, new Vector3(7.5f, 40f, -165.0f), 35f, 1.0f);
            SpawnSnapped(handcartPrefab, dRoot, new Vector3(16.0f, 40f, -179.5f), 170f, 1.0f);

            // Pottery & Artisan display table
            SpawnSnapped(tablePrefab, dRoot, new Vector3(11.0f, 40f, -172.0f), -45f, 1.0f);
            SpawnSnapped(clayPotPrefab, dRoot, new Vector3(11.2f, 40f, -171.8f), 0f, 0.9f);
            SpawnSnapped(ropePrefab, dRoot, new Vector3(7.0f, 40f, -163.5f), 0f, 1.0f);

            // Cargo crates & wine barrels
            SpawnSnapped(barrelPrefab, dRoot, new Vector3(12.8f, 40f, -162.8f), 0f, 1.0f);
            SpawnSnapped(barrelPrefab, dRoot, new Vector3(13.5f, 40f, -163.2f), 45f, 1.0f);
            SpawnSnapped(cratePrefab, dRoot, new Vector3(19.2f, 40f, -163.0f), 15f, 1.0f);
            SpawnSnapped(crate2Prefab, dRoot, new Vector3(19.8f, 40f, -163.5f), -30f, 1.0f);

            // Produce sets
            SpawnSnapped(cratesPrefab, dRoot, new Vector3(12.5f, 40f, -167.5f), 10f, 1.0f);
            SpawnSnapped(cratesPrefab, dRoot, new Vector3(17.8f, 40f, -168.0f), -25f, 1.0f);
            SpawnSnapped(cratesPrefab, dRoot, new Vector3(9.2f, 40f, -172.0f), 45f, 1.0f);
            SpawnSnapped(cratesPrefab, dRoot, new Vector3(14.0f, 40f, -176.0f), 180f, 1.0f);

            // Bakery storefront
            SpawnSnapped(pastryPrefab, dRoot, new Vector3(24.5f, 40f, -168.0f), -80f, 1.0f);
            SpawnSnapped(chalkboardPrefab, dRoot, new Vector3(25.8f, 40f, -166.5f), -60f, 1.0f);

            // Shoppers rest area & lamps
            SpawnSnapped(benchPrefab, dRoot, new Vector3(8.0f, 40f, -170.0f), 85f, 0.95f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(8.0f, 40f, -177.0f), 85f, 0.95f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(10.5f, 40f, -161.0f), 30f, 0.85f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(19.5f, 40f, -161.0f), -30f, 0.85f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(14.5f, 40f, -183.0f), 180f, 0.85f);

            // Floral accents
            SpawnSnapped(crimsonFlowersPrefab, dRoot, new Vector3(6.5f, 40f, -167.0f), 0f, 1.0f);
            SpawnSnapped(mixedFlowersPrefab, dRoot, new Vector3(6.5f, 40f, -175.0f), 45f, 1.0f);
        }
        #endregion

        #region District III: Western Craftsmen Forge & Tavern Terrace
        private static void DressDistrictIII_WesternForgeAndTavern(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "District_III_Western_Forge_And_Tavern");

            var anvilPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Anvil_Stump.prefab");
            var coalBasinPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_CoalBasin.prefab");
            var grindstonePrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Grindstone.prefab");
            var quenchPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_QuenchTrough.prefab");
            var firewoodPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab");
            var noticeBoardPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab");
            var wellPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab");
            var weaponRackPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab");
            var diningTablePrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab");
            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var ladderPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Folding_Ladder.prefab");

            var barrelPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var bucketPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Wooden_Bucket_1.prefab");
            var handcartPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var mugPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Clay Mug 2.prefab");

            // Blacksmith Forge Terrace (around X: -225, Z: 142)
            SpawnSnapped(anvilPrefab, dRoot, new Vector3(-226.5f, 25f, 143.0f), 30f, 1.0f);
            SpawnSnapped(coalBasinPrefab, dRoot, new Vector3(-228.0f, 25f, 144.5f), 0f, 1.0f);
            SpawnSnapped(grindstonePrefab, dRoot, new Vector3(-224.5f, 25f, 144.8f), -45f, 1.0f);
            SpawnSnapped(quenchPrefab, dRoot, new Vector3(-227.8f, 25f, 141.5f), 15f, 1.0f);
            SpawnSnapped(bucketPrefab, dRoot, new Vector3(-227.0f, 25f, 141.0f), 0f, 1.0f);
            SpawnSnapped(firewoodPrefab, dRoot, new Vector3(-229.5f, 25f, 143.0f), 90f, 1.0f);
            SpawnSnapped(firewoodPrefab, dRoot, new Vector3(-229.5f, 25f, 144.5f), 90f, 1.0f);
            SpawnSnapped(weaponRackPrefab, dRoot, new Vector3(-223.5f, 25f, 141.8f), -15f, 1.0f);
            SpawnSnapped(handcartPrefab, dRoot, new Vector3(-230.5f, 25f, 141.0f), 45f, 1.0f);
            SpawnSnapped(ladderPrefab, dRoot, new Vector3(-231.2f, 25f, 145.5f), 75f, 1.0f);

            // Village Well in plaza
            SpawnSnapped(wellPrefab, dRoot, new Vector3(-218.0f, 25f, 148.0f), 0f, 1.0f);
            SpawnSnapped(bucketPrefab, dRoot, new Vector3(-217.2f, 25f, 148.8f), 20f, 1.0f);

            // Tavern Outdoor Dining Terrace & Ale Barrels
            SpawnSnapped(diningTablePrefab, dRoot, new Vector3(-219.0f, 25f, 136.5f), 15f, 1.0f);
            SpawnSnapped(mugPrefab, dRoot, new Vector3(-219.0f, 26f, 136.5f), 0f, 1.0f);
            SpawnSnapped(diningTablePrefab, dRoot, new Vector3(-222.5f, 25f, 135.5f), -20f, 1.0f);
            SpawnSnapped(mugPrefab, dRoot, new Vector3(-222.5f, 26f, 135.5f), 45f, 1.0f);
            SpawnSnapped(diningTablePrefab, dRoot, new Vector3(-216.0f, 25f, 138.0f), 45f, 1.0f);

            SpawnSnapped(barrelPrefab, dRoot, new Vector3(-224.2f, 25f, 134.8f), 0f, 1.0f);
            SpawnSnapped(barrelPrefab, dRoot, new Vector3(-224.8f, 25f, 135.5f), 30f, 1.0f);

            // Adventurers' Guild Notice Board & Lanterns
            SpawnSnapped(noticeBoardPrefab, dRoot, new Vector3(-207.0f, 38f, 178.0f), -60f, 1.1f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-204.5f, 38f, 182.0f), 45f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-221.0f, 25f, 144.0f), 0f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-215.0f, 25f, 150.0f), -90f, 0.9f);
        }
        #endregion

        #region District III-B: Eastern Artisan Quarter (Barnaby's Open Forge & Tavern Plaza)
        private static void DressDistrictIIIB_EasternArtisanQuarter(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "District_IIIB_Eastern_Artisan_Quarter");

            var anvilPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Anvil_Stump.prefab");
            var coalBasinPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_CoalBasin.prefab");
            var grindstonePrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Grindstone.prefab");
            var quenchPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_QuenchTrough.prefab");
            var firewoodPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab");
            var weaponRackPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab");
            var diningTablePrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab");
            var wellPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab");
            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var benchPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            var barrelPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var bucketPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Wooden_Bucket_1.prefab");

            // Position Barnaby directly onto the paved East Roadbed
            var barnaby = GameObject.Find("LIVELY_METROPOLIS_NPCS/01_Story_And_Quest_Givers/NPC_Barnaby");
            if (barnaby == null) barnaby = GameObject.Find("NPC_Barnaby");
            if (barnaby != null)
            {
                barnaby.transform.position = new Vector3(255f, 38.18f, 2.0f);
                barnaby.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // Barnaby's Blacksmith Forge Terrace along north curb of East Roadbed
            Vector3 forgeCenter = new Vector3(255f, 38f, 4.0f);
            SpawnSnapped(anvilPrefab, dRoot, forgeCenter + new Vector3(2.5f, 0f, 0.5f), -45f, 1.0f);
            SpawnSnapped(coalBasinPrefab, dRoot, forgeCenter + new Vector3(4.0f, 0f, 1.5f), 180f, 1.0f);
            SpawnSnapped(quenchPrefab, dRoot, forgeCenter + new Vector3(3.5f, 0f, -1.0f), -75f, 1.0f);
            SpawnSnapped(bucketPrefab, dRoot, forgeCenter + new Vector3(3.0f, 0f, -1.5f), 0f, 1.0f);
            SpawnSnapped(grindstonePrefab, dRoot, forgeCenter + new Vector3(0.8f, 0f, 2.0f), 45f, 1.0f);
            SpawnSnapped(firewoodPrefab, dRoot, forgeCenter + new Vector3(5.0f, 0f, 0.5f), 0f, 1.0f);
            SpawnSnapped(weaponRackPrefab, dRoot, forgeCenter + new Vector3(-0.5f, 0f, 2.8f), -30f, 1.0f);

            // Eastern Tavern Dining Terrace on south curb of East Roadbed
            Vector3 tavernCenter = new Vector3(255f, 38f, -4.5f);
            SpawnSnapped(diningTablePrefab, dRoot, tavernCenter + new Vector3(0f, 0f, 0f), 0f, 1.0f);
            SpawnSnapped(barrelPrefab, dRoot, tavernCenter + new Vector3(2.5f, 0f, -1.0f), 0f, 1.0f);
            SpawnSnapped(barrelPrefab, dRoot, tavernCenter + new Vector3(3.2f, 0f, -0.5f), 40f, 1.0f);

            // Community well & bench
            SpawnSnapped(wellPrefab, dRoot, new Vector3(248.0f, 38f, 4.0f), 0f, 1.0f);
            SpawnSnapped(bucketPrefab, dRoot, new Vector3(248.8f, 38f, 4.6f), 15f, 1.0f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(248.5f, 38f, -4.0f), 0f, 0.95f);

            // Illuminating street lamps on both curbs
            SpawnSnapped(lampPrefab, dRoot, new Vector3(251.0f, 38f, 3.8f), 30f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(251.0f, 38f, -3.8f), -30f, 0.9f);
        }
        #endregion

        #region District IV: Arcane Enclave & Margo's Sanctuary
        private static void DressDistrictIV_ArcaneEnclaveAndMargo(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "District_IV_Arcane_Enclave_And_Margo");

            var crystalPedestalPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Wizard_CrystalPedestal.prefab");
            var spellbookLecternPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Wizard_SpellbookLectern.prefab");
            var cauldronPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_AlchemistCauldron_Stylized.prefab");
            var distillationPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Alchemy_DistillationBench.prefab");
            var mortarPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Alchemy_MortarPestleRack.prefab");
            var benchPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var azureFlowersPrefab = LoadPrefab("Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab");
            var goldFlowersPrefab = LoadPrefab("Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab");

            // A. Margo's Witch Sanctuary & Magic Items Plaza at (-115, 58, 115)
            SpawnSnapped(crystalPedestalPrefab, dRoot, new Vector3(-118.5f, 58f, 117.5f), 0f, 1.0f);
            SpawnSnapped(spellbookLecternPrefab, dRoot, new Vector3(-113.0f, 58f, 117.0f), 135f, 1.0f);
            SpawnSnapped(cauldronPrefab, dRoot, new Vector3(-116.5f, 58f, 118.5f), 30f, 0.95f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(-110.0f, 58f, 112.0f), -45f, 0.95f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-119.0f, 58f, 112.5f), 45f, 0.9f);
            SpawnSnapped(azureFlowersPrefab, dRoot, new Vector3(-118.0f, 58f, 119.5f), 0f, 1.1f);
            SpawnSnapped(azureFlowersPrefab, dRoot, new Vector3(-112.0f, 58f, 118.5f), 45f, 1.1f);

            // B. Enchanter Workshop storefront at (-96, 58, 130)
            SpawnSnapped(distillationPrefab, dRoot, new Vector3(-98.0f, 58f, 128.5f), 45f, 1.0f);
            SpawnSnapped(mortarPrefab, dRoot, new Vector3(-95.5f, 58f, 132.0f), -30f, 1.0f);
            SpawnSnapped(crystalPedestalPrefab, dRoot, new Vector3(-94.0f, 58f, 127.5f), 0f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-99.5f, 58f, 126.0f), -45f, 0.9f);
            SpawnSnapped(goldFlowersPrefab, dRoot, new Vector3(-96.5f, 58f, 126.5f), 20f, 1.0f);

            // C. Western Apothecary at (-218, 41, 163)
            SpawnSnapped(distillationPrefab, dRoot, new Vector3(-219.5f, 41f, 163.5f), 30f, 1.0f);
            SpawnSnapped(mortarPrefab, dRoot, new Vector3(-221.0f, 41f, 164.8f), -15f, 1.0f);
            SpawnSnapped(cauldronPrefab, dRoot, new Vector3(-217.2f, 41f, 164.2f), 45f, 0.85f);
            SpawnSnapped(azureFlowersPrefab, dRoot, new Vector3(-215.5f, 41f, 161.0f), 0f, 1.1f);
            SpawnSnapped(goldFlowersPrefab, dRoot, new Vector3(-215.5f, 41f, 163.2f), 35f, 1.1f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-216.5f, 41f, 159.5f), -45f, 0.9f);
        }
        #endregion

        #region District V: Seaward Harbor Vista & Cargo Pier
        private static void DressDistrictV_SeawardHarborVista(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "District_V_Seaward_Harbor_Vista");

            var benchPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            var cratesPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var azureFlowersPrefab = LoadPrefab("Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab");

            var barrelPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var handcartPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var cratePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            var crate2Prefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_02.prefab");
            var palettePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Palette.prefab");
            var ropePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Rope.prefab");
            var ladderPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Folding_Ladder.prefab");

            // Scenic ocean lookout promenade (around X: 348, Z: 12)
            SpawnSnapped(benchPrefab, dRoot, new Vector3(348.0f, 22f, 8.0f), 90f, 1.05f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(348.0f, 22f, 16.0f), 90f, 1.05f);

            // Dockside shipping cargo depot
            SpawnSnapped(palettePrefab, dRoot, new Vector3(354.0f, 22f, 5.0f), 0f, 1.0f);
            SpawnSnapped(cratePrefab, dRoot, new Vector3(354.0f, 22f, 5.0f), 15f, 1.0f);
            SpawnSnapped(crate2Prefab, dRoot, new Vector3(355.2f, 22f, 6.2f), -20f, 1.0f);

            SpawnSnapped(handcartPrefab, dRoot, new Vector3(352.0f, 22f, 2.5f), 15f, 1.0f);
            SpawnSnapped(barrelPrefab, dRoot, new Vector3(354.5f, 22f, 2.0f), 0f, 1.0f);
            SpawnSnapped(barrelPrefab, dRoot, new Vector3(355.2f, 22f, 2.8f), 30f, 1.0f);
            SpawnSnapped(ropePrefab, dRoot, new Vector3(351.0f, 22f, 1.2f), 45f, 1.0f);
            SpawnSnapped(ladderPrefab, dRoot, new Vector3(356.0f, 22f, 8.5f), -75f, 1.0f);

            SpawnSnapped(cratesPrefab, dRoot, new Vector3(353.5f, 22f, 10.0f), -35f, 1.0f);
            SpawnSnapped(cratesPrefab, dRoot, new Vector3(354.0f, 22f, 18.5f), 15f, 1.0f);

            // Harbor lanterns & wildflowers
            SpawnSnapped(lampPrefab, dRoot, new Vector3(346.5f, 22f, 4.0f), 45f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(346.5f, 22f, 20.0f), -45f, 0.9f);
            SpawnSnapped(azureFlowersPrefab, dRoot, new Vector3(349.5f, 22f, 12.0f), 0f, 1.0f);
        }
        #endregion

        #region District VI: Grand Guildhall & Martial Armory
        private static void DressDistrictVI_GrandGuildhall(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "District_VI_Grand_Guildhall");

            var noticeBoardPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab");
            var weaponRackPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab");
            var benchPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var cratePrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            var boxPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Wooden Box 01.prefab");

            // Forecourt of Grand Guildhall at (-205, 38, 180)
            SpawnSnapped(noticeBoardPrefab, dRoot, new Vector3(-202.5f, 38f, 177.0f), -45f, 1.1f);
            SpawnSnapped(weaponRackPrefab, dRoot, new Vector3(-208.5f, 38f, 178.5f), 30f, 1.0f);
            SpawnSnapped(weaponRackPrefab, dRoot, new Vector3(-209.5f, 38f, 180.5f), 30f, 1.0f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(-203.0f, 38f, 183.0f), -135f, 0.95f);
            SpawnSnapped(cratePrefab, dRoot, new Vector3(-207.0f, 38f, 175.5f), 10f, 1.0f);
            SpawnSnapped(boxPrefab, dRoot, new Vector3(-206.2f, 38f, 175.8f), -20f, 1.0f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(-201.5f, 38f, 180.5f), -90f, 0.9f);
        }
        #endregion

        #region District VII: Southern Gatehouse Bastion (Zephyr's Outpost)
        private static void DressDistrictVII_SouthernGatehouseBastion(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "District_VII_Southern_Gatehouse_Bastion");

            var weaponRackPrefab = LoadPrefab("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab");
            var benchPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var handcartPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var boxPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Wooden Box 01.prefab");
            var barrelPrefab = LoadPrefab("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");

            // Outpost around Zephyr on the south promenade (around X: 15, Y: 22.01, Z: -445)
            SpawnSnapped(weaponRackPrefab, dRoot, new Vector3(12.0f, 22.01f, -443.0f), 20f, 1.0f);
            SpawnSnapped(weaponRackPrefab, dRoot, new Vector3(17.5f, 22.01f, -443.0f), -20f, 1.0f);
            SpawnSnapped(benchPrefab, dRoot, new Vector3(17.5f, 22.01f, -446.0f), -90f, 0.95f);
            SpawnSnapped(handcartPrefab, dRoot, new Vector3(11.5f, 22.01f, -446.0f), 30f, 1.0f);
            SpawnSnapped(boxPrefab, dRoot, new Vector3(12.5f, 22.01f, -447.0f), 10f, 1.0f);
            SpawnSnapped(barrelPrefab, dRoot, new Vector3(10.8f, 22.01f, -446.5f), 0f, 1.0f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(12.0f, 22.01f, -440.0f), 45f, 0.9f);
            SpawnSnapped(lampPrefab, dRoot, new Vector3(17.5f, 22.01f, -440.0f), -45f, 0.9f);
        }
        #endregion

        #region Radial Avenues & Shaded Boulevards
        private static void DressRadialAvenuesAndBoulevards(Transform parent)
        {
            var dRoot = CreateSubGroup(parent, "Metropolis_Radial_Avenues_Flora_And_Lamps");

            var lampPrefab = LoadPrefab("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            var broadleafTree = LoadPrefab("Assets/Prefabs/AnimeNature/PFB_AnimeTree_Broadleaf.prefab");
            var grandOakTree = LoadPrefab("Assets/Prefabs/AnimeNature/PFB_AnimeTree_GrandOak.prefab");
            var mixedFlowers = LoadPrefab("Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab");

            Vector3[] avenueNodes = new Vector3[]
            {
                new Vector3(0f, 80f, -90f),
                new Vector3(-25f, 80f, -115f),
                new Vector3(25f, 80f, -115f),
                new Vector3(-65f, 58f, -140f),
                new Vector3(65f, 58f, -140f),
                new Vector3(-110f, 58f, -120f),
                new Vector3(110f, 58f, -120f),
                new Vector3(-150f, 38f, 20f),
                new Vector3(150f, 38f, 20f),
                new Vector3(-180f, 38f, 90f),
                new Vector3(180f, 38f, 90f),
                new Vector3(0f, 38f, 120f)
            };

            for (int i = 0; i < avenueNodes.Length; i++)
            {
                Vector3 node = avenueNodes[i];
                float sideOffset = (i % 2 == 0) ? 7.5f : -7.5f;

                // Lamp on one curb
                Vector3 lampPos = node + new Vector3(sideOffset, 0f, 0f);
                SpawnSnapped(lampPrefab, dRoot, lampPos, (i % 2 == 0) ? -45f : 45f, 0.85f);

                // Tree on opposite curb
                Vector3 treePos = node + new Vector3(-sideOffset * 1.3f, 0f, 0f);
                var treePrefab = (i % 3 == 0) ? grandOakTree : broadleafTree;
                SpawnSnapped(treePrefab, dRoot, treePos, UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0.85f, 1.15f));

                // Flower accent at tree base
                SpawnSnapped(mixedFlowers, dRoot, treePos + new Vector3(1.2f, 0f, 0.8f), UnityEngine.Random.Range(0f, 360f), 0.9f);
            }
        }
        #endregion

        #region Spawning & Ground Snap Utility
        private static GameObject CreateSubGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject LoadPrefab(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p == null)
            {
                Debug.LogWarning($"[CITY ENRICHMENT] Missing prefab at '{path}'");
            }
            return p;
        }

        private static GameObject SpawnSnapped(GameObject prefab, GameObject parent, Vector3 targetPos, float yaw, float uniformScale)
        {
            if (prefab == null || parent == null) return null;

            Vector3 spawnPos = SnapToGround(targetPos);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            instance.transform.position = spawnPos;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * uniformScale;
            return instance;
        }

        private static Vector3 SnapToGround(Vector3 targetPos)
        {
            Vector3 origin = new Vector3(targetPos.x, targetPos.y + 40f, targetPos.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 100f);

            float bestY = targetPos.y;
            float minDiff = float.MaxValue;
            bool foundGround = false;

            foreach (var h in hits)
            {
                string n = h.collider.name.ToLower();
                // Filter out non-ground obstacles
                if (n.Contains("npc") || n.Contains("character") || n.Contains("roof") || 
                    n.Contains("wall") || n.Contains("astrolabe") || n.Contains("bench") || 
                    n.Contains("gate") || n.Contains("curtain") || n.Contains("door") ||
                    n.Contains("drum") || n.Contains("foundation"))
                {
                    continue;
                }

                float diff = Mathf.Abs(h.point.y - targetPos.y);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestY = h.point.y;
                    foundGround = true;
                }
            }

            if (foundGround)
            {
                return new Vector3(targetPos.x, bestY, targetPos.z);
            }

            return targetPos;
        }
        #endregion
    }
}

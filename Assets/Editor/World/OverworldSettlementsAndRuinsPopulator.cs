using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleSummoner.World
{
    /// <summary>
    /// Populates the expansive continent of BattleSummoner with rich, atmospheric rural towns,
    /// farmhouses, hilltop temples & shrines, and ancient abandoned ruins.
    /// Uses dynamic bounding-box ground snapping against CircularContinent_MacroLevel1 so all
    /// houses, ruins, and props sit flush with the terrain (no sunken foundations, no floating).
    /// </summary>
    public static class OverworldSettlementsAndRuinsPopulator
    {
        public const string RootName = "--- CONTINENTAL_RURAL_TOWNS_AND_RUINS ---";

        private static Material _matStone;
        private static Material _matMarble;
        private static Material _matWood;

        [MenuItem("BattleSummoner/World/Populate Continental Towns, Farmhouses, Temples & Ruins")]
        public static void PopulateAllSettlementsAndRuins()
        {
            var continent = GameObject.Find("CircularContinent_MacroLevel1");
            if (continent == null)
            {
                Debug.LogError("CircularContinent_MacroLevel1 not found in scene!");
                return;
            }

            var terrainCol = continent.GetComponent<Collider>();
            if (terrainCol == null)
            {
                Debug.LogError("No Collider found on CircularContinent_MacroLevel1!");
                return;
            }

            // Load shared architectural materials
            _matStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            _matMarble = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_Marble.mat");
            _matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_WoodFloor.mat");

            // Clean up old root if present
            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Populate Settlements & Ruins");

            // 1. SouthEast: Oakhaven Farming Hamlet
            GameObject oakhaven = new GameObject("01_Oakhaven_Farming_Hamlet");
            oakhaven.transform.SetParent(root.transform, false);
            ClearVegetationInRadius(new Vector3(580f, 0f, -650f), 38f);
            BuildOakhavenFarmingHamlet(oakhaven.transform, terrainCol);

            // 2. NorthEast: Sanctuary of the Four Winds (Ancient Temple & Shrines)
            GameObject temple = new GameObject("02_Sanctuary_Of_Four_Winds_Temple");
            temple.transform.SetParent(root.transform, false);
            ClearVegetationInRadius(new Vector3(850f, 0f, 800f), 42f);
            BuildSanctuaryOfTheFourWinds(temple.transform, terrainCol);

            // 3. NorthEast Coastal: Eastwatch Pilgrim Village
            GameObject eastwatch = new GameObject("03_Eastwatch_Pilgrim_Village");
            eastwatch.transform.SetParent(root.transform, false);
            ClearVegetationInRadius(new Vector3(650f, 0f, 650f), 38f);
            BuildEastwatchPilgrimVillage(eastwatch.transform, terrainCol);

            // 4. NorthWest: Highland Pine & Blacksmith Hamlet
            GameObject highland = new GameObject("04_Highland_Pine_Blacksmith_Hamlet");
            highland.transform.SetParent(root.transform, false);
            ClearVegetationInRadius(new Vector3(-600f, 0f, 600f), 48f);
            BuildHighlandBlacksmithHamlet(highland.transform, terrainCol);

            // 5. SouthWest: The Sunken Citadel (Ancient Ruined Fort & Relic Cache)
            GameObject sunkenCitadel = new GameObject("05_Sunken_Citadel_Ancient_Ruins");
            sunkenCitadel.transform.SetParent(root.transform, false);
            ClearVegetationInRadius(new Vector3(-800f, 0f, -1000f), 55f);
            BuildSunkenCitadelAncientRuins(sunkenCitadel.transform, terrainCol);

            // 6. SouthWest River: Whispering Hermitage & River Ruins
            GameObject hermitage = new GameObject("06_Whispering_Hermitage_Riverside");
            hermitage.transform.SetParent(root.transform, false);
            ClearVegetationInRadius(new Vector3(-500f, 0f, -700f), 36f);
            BuildWhisperingHermitage(hermitage.transform, terrainCol);

            // Mark Scene Dirty and Save
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"🏡 [OverworldSettlements] Successfully populated 6 continental POIs under '{RootName}'.");
        }

        // =========================================================================
        // 1. OAKHAVEN FARMING HAMLET (SouthEast: 580, -650)
        // =========================================================================
        private static void BuildOakhavenFarmingHamlet(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(580f, 0f, -650f);

            // Village Well at center
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab", parent, center, 0f, 1.4f, terrainCol);

            // Village Market Booth facing square
            SpawnProp("Assets/Prefabs/Overworld/Market_Booth.prefab", parent, center + new Vector3(6f, 0f, -5f), 15f, 1.35f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab", parent, center + new Vector3(10f, 0f, -4f), 35f, 1.2f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", parent, center + new Vector3(4f, 0f, -9f), 0f, 1.1f, terrainCol);

            // Community Notice Board & Benches
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab", parent, center + new Vector3(-6f, 0f, 6f), -45f, 1.2f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(-8f, 0f, -5f), 110f, 1.1f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(7f, 0f, 7f), -70f, 1.1f, terrainCol);

            // Street Lanterns
            SpawnLantern(parent, center + new Vector3(4f, 0f, 4f), terrainCol);
            SpawnLantern(parent, center + new Vector3(-6f, 0f, -3f), terrainCol);
            SpawnLantern(parent, center + new Vector3(11f, 0f, -12f), terrainCol);
            SpawnLantern(parent, center + new Vector3(-12f, 0f, 12f), terrainCol);

            // 4 Farmhouses encircling the village square
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab", parent, center + new Vector3(0f, 0f, 22f), 180f, 5.5f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Alchemist_Apothecary.prefab", parent, center + new Vector3(24f, 0f, 0f), -90f, 5.0f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Merchant_HarborManse.prefab", parent, center + new Vector3(0f, 0f, -24f), 0f, 5.0f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab", parent, center + new Vector3(-24f, 0f, 0f), 90f, 5.2f, terrainCol);

            // Firewood stacks & tool racks
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(18f, 0f, 14f), 0f, 1.3f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(-16f, 0f, -14f), 45f, 1.3f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab", parent, center + new Vector3(-14f, 0f, 10f), 20f, 1.2f, terrainCol);

            // Split-rail wooden fence surrounding the farmstead
            BuildRusticFenceEnclosure(parent, center, 32f, terrainCol);
        }

        // =========================================================================
        // 2. SANCTUARY OF THE FOUR WINDS (NorthEast Hilltop Temple: 850, 800)
        // =========================================================================
        private static void BuildSanctuaryOfTheFourWinds(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(850f, 0f, 800f);

            // Central Sacred Arcane Spire
            SpawnProp("Assets/Models/Metropolis/Prop_ArcaneSpire_MagesQuarter.fbx", parent, center, 0f, 1.3f, terrainCol);

            // Altar of the Winds: Crystal Pedestal & Spellbook
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Wizard_CrystalPedestal.prefab", parent, center + new Vector3(0f, 0f, -9f), 0f, 1.6f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx", parent, center + new Vector3(1.2f, 0f, -9f), 30f, 1.4f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Wizard_SpellbookLectern.prefab", parent, center + new Vector3(-1.8f, 0f, -8f), 20f, 1.3f, terrainCol);

            // Stone Stairs leading to the Altar (scaled to 12m wide from 10k raw export)
            SpawnProp("Assets/Prefabs/Overworld/Stone_Stairs.prefab", parent, center + new Vector3(0f, 0f, -16f), 0f, 0.0012f, terrainCol);

            // 4 Grand Marble Temple Pillars framing the sanctum
            Vector3[] pillarOffsets = {
                new Vector3(-12f, 0f, -12f),
                new Vector3(12f, 0f, -12f),
                new Vector3(-12f, 0f, 12f),
                new Vector3(12f, 0f, 12f)
            };
            foreach (var off in pillarOffsets)
            {
                SpawnPillar(parent, center + off, 1.3f, 8.5f, _matMarble, terrainCol);
            }

            // Sakura Stone Planter Rings filled with blooming flowers
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_SakuraStonePlanterRing.prefab", parent, center + new Vector3(-16f, 0f, -6f), 0f, 1.25f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_SakuraStonePlanterRing.prefab", parent, center + new Vector3(16f, 0f, -6f), 0f, 1.25f, terrainCol);

            // Stone prayer benches
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(-10f, 0f, -18f), 45f, 1.15f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(10f, 0f, -18f), -45f, 1.15f, terrainCol);

            // 4 Cerulean Mystic Temple Braziers
            SpawnLantern(parent, center + new Vector3(-6f, 0f, -22f), terrainCol, new Color(0.25f, 0.75f, 1.0f), 3.0f, 16f);
            SpawnLantern(parent, center + new Vector3(6f, 0f, -22f), terrainCol, new Color(0.25f, 0.75f, 1.0f), 3.0f, 16f);
            SpawnLantern(parent, center + new Vector3(-16f, 0f, 4f), terrainCol, new Color(0.25f, 0.75f, 1.0f), 2.5f, 14f);
            SpawnLantern(parent, center + new Vector3(16f, 0f, 4f), terrainCol, new Color(0.25f, 0.75f, 1.0f), 2.5f, 14f);
        }

        // =========================================================================
        // 3. EASTWATCH PILGRIM VILLAGE (NorthEast Coastal Plateau: 650, 650)
        // =========================================================================
        private static void BuildEastwatchPilgrimVillage(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(650f, 0f, 650f);

            // Village Well & Gathering Terrace
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab", parent, center, 0f, 1.4f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab", parent, center + new Vector3(7f, 0f, 4f), -20f, 1.3f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab", parent, center + new Vector3(-5f, 0f, 5f), -35f, 1.2f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab", parent, center + new Vector3(6f, 0f, -6f), 10f, 1.2f, terrainCol);

            // Market Booth & Crates
            SpawnProp("Assets/Prefabs/Overworld/Market_Booth.prefab", parent, center + new Vector3(-12f, 0f, -5f), 45f, 1.35f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab", parent, center + new Vector3(-15f, 0f, -3f), 65f, 1.2f, terrainCol);

            // 4 Pilgrim Houses & Scholar Spire
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Gothic_ScholarSpire.prefab", parent, center + new Vector3(18f, 0f, 16f), -135f, 5.0f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Merchant_HarborManse.prefab", parent, center + new Vector3(-20f, 0f, 14f), 65f, 5.0f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab", parent, center + new Vector3(6f, 0f, -22f), 5f, 5.2f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Alchemist_Apothecary.prefab", parent, center + new Vector3(-12f, 0f, -20f), 25f, 4.8f, terrainCol);

            // Benches & Planters
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(-2f, 0f, 8f), 0f, 1.15f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(10f, 0f, -4f), 90f, 1.15f, terrainCol);

            // Street Lanterns & Barrels
            SpawnLantern(parent, center + new Vector3(-4f, 0f, -8f), terrainCol);
            SpawnLantern(parent, center + new Vector3(12f, 0f, -4f), terrainCol);
            SpawnLantern(parent, center + new Vector3(-10f, 0f, 10f), terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", parent, center + new Vector3(13f, 0f, 9f), 30f, 1.2f, terrainCol);

            // Rustic Fence Enclosure around the perimeter
            BuildRusticFenceEnclosure(parent, center, 30f, terrainCol);
        }

        // =========================================================================
        // 4. HIGHLAND PINE & BLACKSMITH HAMLET (NorthWest Foothills: -600, 600)
        // =========================================================================
        private static void BuildHighlandBlacksmithHamlet(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(-600f, 0f, 600f);

            // Covered Blacksmith Crafting Pavilion & Tools
            SpawnProp("Assets/Prefabs/Overworld/Market_Booth.prefab", parent, center + new Vector3(-6f, 0f, 6f), 45f, 1.45f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Anvil_Stump.prefab", parent, center + new Vector3(-4f, 0f, 3f), 0f, 1.3f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Grindstone.prefab", parent, center + new Vector3(-8f, 0f, 4f), 90f, 1.25f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_QuenchTrough.prefab", parent, center + new Vector3(-3.5f, 0f, 7.5f), 0f, 1.25f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_CoalBasin.prefab", parent, center + new Vector3(-7.5f, 0f, 7.5f), 0f, 1.3f, terrainCol);
            SpawnLantern(parent, center + new Vector3(-7.5f, 0f, 7.5f), terrainCol, new Color(1.0f, 0.45f, 0.1f), 3.5f, 12f);

            // Firewood Stacks & Weapon Racks
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(-12f, 0f, 2f), 15f, 1.4f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(-12f, 0f, -4f), 0f, 1.4f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab", parent, center + new Vector3(-2f, 0f, 5f), -45f, 1.2f, terrainCol);

            // 3 Mountain Timber Chalets
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab", parent, center + new Vector3(16f, 0f, 14f), -125f, 5.2f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Alchemist_Apothecary.prefab", parent, center + new Vector3(20f, 0f, -12f), -45f, 5.0f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab", parent, center + new Vector3(-18f, 0f, -16f), 140f, 5.2f, terrainCol);

            // Highland Watchtower Camp
            SpawnProp("Assets/Prefabs/World/PFB_EnemyOutpost_WatchtowerCamp.prefab", parent, center + new Vector3(32f, 0f, 0f), -90f, 1.0f, terrainCol);

            // Benches & Barrels
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(2f, 0f, -4f), 0f, 1.15f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", parent, center + new Vector3(5f, 0f, 6f), 20f, 1.2f, terrainCol);

            // Street Lanterns
            SpawnLantern(parent, center + new Vector3(0f, 0f, 0f), terrainCol);
            SpawnLantern(parent, center + new Vector3(14f, 0f, 2f), terrainCol);
            SpawnLantern(parent, center + new Vector3(-8f, 0f, -8f), terrainCol);

            // Rustic Fence Enclosure
            BuildRusticFenceEnclosure(parent, center, 32f, terrainCol);
        }

        // =========================================================================
        // 5. THE SUNKEN CITADEL (SouthWest Forgotten Plateau Ruins: -800, -1000)
        // =========================================================================
        private static void BuildSunkenCitadelAncientRuins(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(-800f, 0f, -1000f);

            // Large Ruined Fortress Ramparts Complex
            SpawnProp("Assets/Prefabs/World/PFB_EnemyOutpost_RuinedFort.prefab", parent, center, 25f, 1.35f, terrainCol);

            // 6 Ancient Weathered Ruin Pillars of varying broken heights and tilts
            Vector3[] ruinPillars = {
                new Vector3(-16f, 0f, -12f),
                new Vector3(-14f, 0f, 14f),
                new Vector3(16f, 0f, -14f),
                new Vector3(14f, 0f, 16f),
                new Vector3(-6f, 0f, -18f),
                new Vector3(8f, 0f, -18f)
            };
            float[] pillarHeights = { 7.5f, 4.0f, 9.0f, 5.5f, 6.0f, 3.5f };
            float[] pillarTilts = { 4f, -7f, 3f, -5f, 2f, 6f };

            for (int i = 0; i < ruinPillars.Length; i++)
            {
                SpawnPillar(parent, center + ruinPillars[i], 1.6f, pillarHeights[i], _matStone, terrainCol, pillarTilts[i]);
            }

            // Weathered Boulders & Fallen Rock Slabs
            SpawnProp("Assets/Models/TownProps/Prop_WeatheredBoulders.fbx", parent, center + new Vector3(-14f, 0f, 16f), 80f, 1.8f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WeatheredBoulders.fbx", parent, center + new Vector3(16f, 0f, -16f), -40f, 1.8f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Cliff_Boulder.prefab", parent, center + new Vector3(-18f, 0f, -6f), 20f, 0.0014f, terrainCol);

            // Ancient Stone Stairs leading to ruined wall (scaled to 12m wide from 10k raw export)
            SpawnProp("Assets/Prefabs/Overworld/Stone_Stairs.prefab", parent, center + new Vector3(-8f, 0f, 6f), 115f, 0.0012f, terrainCol);

            // Hidden Relic Treasure Cache
            SpawnProp("Assets/Models/TownProps/Prop_GoldCoinsChest.fbx", parent, center + new Vector3(-2f, 0f, -2f), 45f, 1.4f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx", parent, center + new Vector3(4f, 0f, -3f), -20f, 1.5f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", parent, center + new Vector3(-5f, 0f, -5f), 10f, 1.2f, terrainCol);

            // Spectral Emerald Lanterns
            SpawnLantern(parent, center + new Vector3(-12f, 0f, 0f), terrainCol, new Color(0.2f, 1.0f, 0.6f), 2.5f, 15f);
            SpawnLantern(parent, center + new Vector3(12f, 0f, 0f), terrainCol, new Color(0.2f, 1.0f, 0.6f), 2.5f, 15f);
            SpawnLantern(parent, center + new Vector3(0f, 0f, 14f), terrainCol, new Color(0.2f, 1.0f, 0.6f), 2.5f, 15f);
        }

        // =========================================================================
        // 6. WHISPERING HERMITAGE (SouthWest Riverbank: -500, -700)
        // =========================================================================
        private static void BuildWhisperingHermitage(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(-500f, 0f, -700f);

            // Ancient Stone Bridge spanning towards the water
            SpawnProp("Assets/Prefabs/Environment/FantasyStoneBridge.fbx", parent, center + new Vector3(15f, 0f, 15f), -45f, 1.4f, terrainCol);

            // 2 Solitary Cottages
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab", parent, center + new Vector3(-12f, 0f, 8f), 60f, 5.0f, terrainCol);
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Merchant_HarborManse.prefab", parent, center + new Vector3(8f, 0f, -14f), -110f, 4.8f, terrainCol);

            // Hermit Shrine: Altar & Mana Crystals
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Wizard_CrystalPedestal.prefab", parent, center + new Vector3(-2f, 0f, 14f), 0f, 1.4f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx", parent, center + new Vector3(-1f, 0f, 14f), 25f, 1.2f, terrainCol);

            // Hermit Well, Firewood, and Benches
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab", parent, center + new Vector3(0f, 0f, 0f), 0f, 1.3f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(-8f, 0f, 12f), 30f, 1.3f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(4f, 0f, 6f), -30f, 1.1f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", parent, center + new Vector3(7f, 0f, 2f), -15f, 1.1f, terrainCol);

            // Lanterns
            SpawnLantern(parent, center + new Vector3(-2f, 0f, -4f), terrainCol);
            SpawnLantern(parent, center + new Vector3(10f, 0f, 8f), terrainCol);
        }

        // =========================================================================
        // HELPER UTILITIES: VEGETATION CLEARING, FENCES, PILLARS & LIGHTS
        // =========================================================================

        private static void ClearVegetationInRadius(Vector3 center, float radius)
        {
            float rSq = radius * radius;
            int cleared = 0;

            string[] vegRoots = new string[] {
                "World_AnimeNature_Foliage",
                "--- ANIME NATURE & VEGETATION ---",
                "Grand_Mainland_Nature_And_Outskirts",
                "Mountain_AnimeNature_SwitchbackAndGorge_Root"
            };

            foreach (var rootName in vegRoots)
            {
                var r = GameObject.Find(rootName);
                if (r == null) continue;

                var allTransforms = r.GetComponentsInChildren<Transform>(true);
                for (int i = allTransforms.Length - 1; i >= 0; i--)
                {
                    var t = allTransforms[i];
                    if (t == null || t == r.transform) continue;

                    // Only destroy top-level prefab instances, not inner meshes
                    if (t.parent != r.transform && !t.parent.name.Contains("Chunk") && !t.parent.name.Contains("Folder") && !t.parent.name.Contains("Switchback"))
                        continue;

                    float dSq = (t.position.x - center.x) * (t.position.x - center.x) +
                                (t.position.z - center.z) * (t.position.z - center.z);
                    if (dSq <= rSq)
                    {
                        Undo.DestroyObjectImmediate(t.gameObject);
                        cleared++;
                    }
                }
            }

            // Metropolis district details flora overflow
            var metroDetails = GameObject.Find("--- METROPOLIS_DISTRICT_DETAILS ---");
            if (metroDetails != null)
            {
                var avenues = metroDetails.transform.Find("Metropolis_Radial_Avenues_Flora_And_Lamps");
                if (avenues != null)
                {
                    for (int i = avenues.childCount - 1; i >= 0; i--)
                    {
                        var child = avenues.GetChild(i);
                        float dSq = (child.position.x - center.x) * (child.position.x - center.x) +
                                    (child.position.z - center.z) * (child.position.z - center.z);
                        if (dSq <= rSq)
                        {
                            Undo.DestroyObjectImmediate(child.gameObject);
                            cleared++;
                        }
                    }
                }
            }

            if (cleared > 0)
            {
                Debug.Log($"🪓 Cleared {cleared} trees/bushes within {radius:F0}m of settlement at ({center.x:F0}, {center.z:F0}).");
            }
        }

        private static void BuildRusticFenceEnclosure(Transform parent, Vector3 center, float radius, Collider terrainCol)
        {
            GameObject fenceRoot = new GameObject("Rustic_Farm_Fence_Enclosure");
            fenceRoot.transform.SetParent(parent, true);
            fenceRoot.transform.position = center;

            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                // Leave 2 openings for village entry roads (North and South)
                if (i == 0 || i == 8) continue;

                float a1 = i * (Mathf.PI * 2f / segments);
                float a2 = (i + 1) * (Mathf.PI * 2f / segments);

                Vector3 p1 = center + new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                Vector3 p2 = center + new Vector3(Mathf.Cos(a2) * radius, 0f, Mathf.Sin(a2) * radius);

                float y1 = GetGroundY(p1, terrainCol);
                float y2 = GetGroundY(p2, terrainCol);
                p1.y = y1;
                p2.y = y2;

                // Post
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"FencePost_{i}";
                post.transform.SetParent(fenceRoot.transform, false);
                post.transform.position = p1 + Vector3.up * 0.65f;
                post.transform.localScale = new Vector3(0.25f, 1.3f, 0.25f);
                if (_matWood != null) post.GetComponent<Renderer>().sharedMaterial = _matWood;

                // Top & Bottom Rails connecting to next post
                Vector3 mid = (p1 + p2) * 0.5f;
                float segLen = Vector3.Distance(p1, p2);
                Quaternion rot = Quaternion.LookRotation(p2 - p1);

                GameObject railTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railTop.name = $"FenceRailTop_{i}";
                railTop.transform.SetParent(fenceRoot.transform, false);
                railTop.transform.position = mid + Vector3.up * 0.95f;
                railTop.transform.rotation = rot;
                railTop.transform.localScale = new Vector3(0.12f, 0.12f, segLen);
                if (_matWood != null) railTop.GetComponent<Renderer>().sharedMaterial = _matWood;

                GameObject railBot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railBot.name = $"FenceRailBot_{i}";
                railBot.transform.SetParent(fenceRoot.transform, false);
                railBot.transform.position = mid + Vector3.up * 0.45f;
                railBot.transform.rotation = rot;
                railBot.transform.localScale = new Vector3(0.12f, 0.12f, segLen);
                if (_matWood != null) railBot.GetComponent<Renderer>().sharedMaterial = _matWood;
            }
        }

        private static void SpawnPillar(Transform parent, Vector3 targetPos, float diameter, float height, Material mat, Collider terrainCol, float tiltAngle = 0f)
        {
            float groundY = GetGroundY(targetPos, terrainCol);
            Vector3 pos = new Vector3(targetPos.x, groundY + height * 0.5f, targetPos.z);

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Ancient_Stone_Pillar";
            pillar.transform.SetParent(parent, false);
            pillar.transform.position = pos;
            pillar.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            if (tiltAngle != 0f) pillar.transform.rotation = Quaternion.Euler(tiltAngle, UnityEngine.Random.Range(0f, 360f), tiltAngle * 0.5f);
            if (mat != null) pillar.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static GameObject SpawnHouse(string prefabPath, Transform parent, Vector3 targetPos, float yaw, float scale, Collider terrainCol)
        {
            return SpawnProp(prefabPath, parent, targetPos, yaw, scale, terrainCol, 0f, true);
        }

        private static GameObject SpawnProp(string assetPath, Transform parent, Vector3 targetPos, float yaw, float scale, Collider terrainCol, float extraYOffset = 0f, bool isHouse = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[OverworldSettlements] Could not load asset at '{assetPath}'");
                return null;
            }

            GameObject instance;
            if (PrefabUtility.IsPartOfPrefabAsset(asset))
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(asset, parent);
                instance.name = asset.name;
            }

            instance.transform.localScale = Vector3.one * scale;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float groundY = GetGroundY(targetPos, terrainCol);
            instance.transform.position = new Vector3(targetPos.x, groundY, targetPos.z);

            // Dynamically calculate lowest bounds Y across all child mesh renderers to prevent sunken/floating models
            var meshRenderers = instance.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length > 0)
            {
                float minY = float.MaxValue;
                foreach (var mr in meshRenderers)
                {
                    if (mr.bounds.min.y < minY) minY = mr.bounds.min.y;
                }
                float delta = groundY - minY;
                float embed = isHouse ? 0.15f : 0.05f;
                instance.transform.position += Vector3.up * (delta - embed + extraYOffset);
            }

            EnsureCollider(instance, isHouse);
            return instance;
        }

        private static float GetGroundY(Vector3 pos, Collider terrainCol)
        {
            Ray ray = new Ray(new Vector3(pos.x, 600f, pos.z), Vector3.down);
            if (terrainCol.Raycast(ray, out RaycastHit hit, 1200f))
            {
                return hit.point.y;
            }
            return pos.y;
        }

        private static void SpawnLantern(Transform parent, Vector3 targetPos, Collider terrainCol, Color? lightColor = null, float intensity = 2.5f, float range = 12f)
        {
            var lamp = SpawnProp("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab", parent, targetPos, 0f, 1.25f, terrainCol, 0f);
            if (lamp == null) return;

            GameObject lightObj = new GameObject("PointLight_Lantern");
            lightObj.transform.SetParent(lamp.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);

            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = lightColor ?? new Color(1.0f, 0.78f, 0.45f);
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        private static void EnsureCollider(GameObject go, bool isHouse)
        {
            var colliders = go.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
            {
                var renderers = go.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

                    if (isHouse)
                    {
                        var col = go.AddComponent<BoxCollider>();
                        col.center = go.transform.InverseTransformPoint(b.center);
                        Vector3 lossy = go.transform.lossyScale;
                        col.size = new Vector3(
                            Mathf.Abs(lossy.x) > 0.001f ? b.size.x / lossy.x : b.size.x,
                            Mathf.Abs(lossy.y) > 0.001f ? b.size.y / lossy.y : b.size.y,
                            Mathf.Abs(lossy.z) > 0.001f ? b.size.z / lossy.z : b.size.z
                        );
                    }
                    else
                    {
                        var mf = go.GetComponentInChildren<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            var mc = mf.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                        }
                        else
                        {
                            var col = go.AddComponent<BoxCollider>();
                            col.center = go.transform.InverseTransformPoint(b.center);
                            col.size = b.size;
                        }
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleSummoner.World
{
    /// <summary>
    /// Master Overworld World Refinement Populator
    /// Populates and refines the expansive continent of BattleSummoner:
    /// - 6 living rural towns, farmhouses, mountaintop temples, and ancient ruins.
    /// - 4 regional crossroads with directional signposts, lanterns, and rest stops.
    /// - Animated fantasy NPCs with living idle animation cycles at every settlement.
    /// - Flagstone stepping paver walking trails connecting houses, wells, gates, and altars.
    /// - Interactive world props: horse carts, bakery displays, alchemy benches, tool racks.
    /// - Precision dynamic bounds ground-snapping flush to CircularContinent_MacroLevel1.
    /// </summary>
    public static class OverworldSettlementsAndRuinsPopulator
    {
        public const string RootName = "--- CONTINENTAL_RURAL_TOWNS_AND_RUINS ---";

        private static Material _matStone;
        private static Material _matMarble;
        private static Material _matWood;
        private static Material _matDungeonStone;
        private static Material _matArchBridgeStone;
        private static Material _matMossyArch;
        private static Material _matMossyDoorStandard;
        private static Material _matMossyFireDish;
        private static Material _matMossyWeaponRack;
        private static Material _matMossyChest;
        private static Material _matCathedralStone;

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
            _matDungeonStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_DungeonStone.mat");
            _matArchBridgeStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_Stone.mat");
            _matMossyArch = AssetDatabase.LoadAssetAtPath<Material>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Materials/M_Doorway_Arched_Stone_Moss.mat");
            _matMossyDoorStandard = AssetDatabase.LoadAssetAtPath<Material>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Materials/M_Doorway_Standard_Stone_Moss.mat");
            _matMossyFireDish = AssetDatabase.LoadAssetAtPath<Material>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Materials/M_Fire_Dish_Brass_Moss.mat");
            _matMossyWeaponRack = AssetDatabase.LoadAssetAtPath<Material>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Materials/M_Weapon_Rack_Wood_Moss.mat");
            _matMossyChest = AssetDatabase.LoadAssetAtPath<Material>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Materials/M_Chest_With_Doors_Metal_Moss.mat");
            _matCathedralStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_CathedralStone.mat");

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

            // 7. Continental Crossroads, Road Signposts & Traveller Waypoints
            GameObject waypoints = new GameObject("07_Continental_Crossroads_And_Waypoints");
            waypoints.transform.SetParent(root.transform, false);
            BuildContinentalCrossroads(waypoints.transform, terrainCol);

            // Mark Scene Dirty and Save
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"🏡 [OverworldSettlements] Successfully populated & refined continental world under '{RootName}'.");
        }

        // =========================================================================
        // 1. OAKHAVEN FARMING HAMLET (SouthEast: 580, -650)
        // =========================================================================
        private static void BuildOakhavenFarmingHamlet(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(580f, 0f, -650f);

            // Village Well at center
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab", parent, center, 0f, 1.4f, terrainCol);

            // Village Market Booth facing square with Bakery Display
            SpawnProp("Assets/Prefabs/Overworld/Market_Booth.prefab", parent, center + new Vector3(6f, 0f, -5f), 15f, 1.35f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Bakery_PastryDisplay.prefab", parent, center + new Vector3(6f, 0f, -4.2f), 15f, 1.1f, terrainCol);
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

            // Herbalist Apothecary Outdoor Distillation Bench
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Alchemy_DistillationBench.prefab", parent, center + new Vector3(18f, 0f, -4f), 0f, 1.15f, terrainCol);

            // Wooden Horse Cart loaded with harvest produce
            SpawnProp("Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Horse_Cart_Used_02.prefab", parent, center + new Vector3(-14f, 0f, 16f), -35f, 1.0f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", parent, center + new Vector3(-11f, 0f, 16f), 15f, 1.1f, terrainCol);

            // Firewood stacks & tool racks
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(18f, 0f, 14f), 0f, 1.3f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(-16f, 0f, -14f), 45f, 1.3f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab", parent, center + new Vector3(-14f, 0f, 10f), 20f, 1.2f, terrainCol);

            // Split-rail wooden fence surrounding the farmstead
            BuildRusticFenceEnclosure(parent, center, 32f, terrainCol);

            // Walking Trails: connecting gate to well, houses, and market booth
            SpawnStoneTrail(parent, center + new Vector3(0f, 0f, -32f), center, 14, _matStone, terrainCol, 1.4f);
            SpawnStoneTrail(parent, center, center + new Vector3(0f, 0f, 20f), 10, _matStone, terrainCol, 1.2f);
            SpawnStoneTrail(parent, center, center + new Vector3(20f, 0f, 0f), 10, _matStone, terrainCol, 1.2f);
            SpawnStoneTrail(parent, center, center + new Vector3(-20f, 0f, 0f), 10, _matStone, terrainCol, 1.2f);
            SpawnStoneTrail(parent, center, center + new Vector3(6f, 0f, -5f), 6, _matStone, terrainCol, 1.2f);

            // Living Villager NPCs
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", parent, center + new Vector3(-12f, 0f, 14f), 45f, terrainCol, "NPC_Farmer_Giles");
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Female.prefab", parent, center + new Vector3(16f, 0f, -3f), -45f, terrainCol, "NPC_Herbalist_Elise");
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", parent, center + new Vector3(-3f, 0f, 2f), -135f, terrainCol, "NPC_Villager_Bram");
        }

        // =========================================================================
        // 2. SANCTUARY OF THE FOUR WINDS (NorthEast Hilltop Temple: 850, 800)
        // =========================================================================
        private static void BuildSanctuaryOfTheFourWinds(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(850f, 0f, 800f);

            // Sacred Arcane Spire set as the majestic backdrop rising behind the sanctum
            SpawnProp("Assets/Models/Metropolis/Prop_ArcaneSpire_MagesQuarter.fbx", parent, center + new Vector3(0f, 0f, 16f), 0f, 1.35f, terrainCol, 0f, false, _matCathedralStone);

            // Temple Sacred Altar: Crystal Pedestal, Mana Crystals & Spellbook Lectern
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Wizard_CrystalPedestal.prefab", parent, center + new Vector3(0f, 0f, -2f), 0f, 1.6f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx", parent, center + new Vector3(1.4f, 0f, -2f), 30f, 1.4f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Wizard_SpellbookLectern.prefab", parent, center + new Vector3(-1.8f, 0f, -1.5f), 20f, 1.3f, terrainCol);

            // Ceremonial Arched Stone Portal Entrance
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Doorway_Arched_low.fbx", parent, center + new Vector3(0f, 0f, -16f), 0f, 2.2f, terrainCol, 0f, false, _matMossyArch);

            // Stone Stairs leading up to the ceremonial entrance
            SpawnProp("Assets/Prefabs/Overworld/Stone_Stairs.prefab", parent, center + new Vector3(0f, 0f, -21f), 0f, 0.0012f, terrainCol);

            // 4 Grand Marble Temple Pillars framing the sanctum
            Vector3[] pillarOffsets = {
                new Vector3(-11f, 0f, -8f),
                new Vector3(11f, 0f, -8f),
                new Vector3(-11f, 0f, 8f),
                new Vector3(11f, 0f, 8f)
            };
            foreach (var off in pillarOffsets)
            {
                SpawnPillar(parent, center + off, 1.3f, 8.5f, _matMarble, terrainCol);
            }

            // Sakura Stone Planter Rings filled with blooming flowers
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_SakuraStonePlanterRing.prefab", parent, center + new Vector3(-14f, 0f, -2f), 0f, 1.25f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_SakuraStonePlanterRing.prefab", parent, center + new Vector3(14f, 0f, -2f), 0f, 1.25f, terrainCol);

            // Stone prayer benches
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(-8f, 0f, -12f), 45f, 1.15f, terrainCol);
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", parent, center + new Vector3(8f, 0f, -12f), -45f, 1.15f, terrainCol);

            // 4 Cerulean Mystic Temple Braziers
            SpawnLantern(parent, center + new Vector3(-5f, 0f, -18f), terrainCol, new Color(0.25f, 0.75f, 1.0f), 3.0f, 16f);
            SpawnLantern(parent, center + new Vector3(5f, 0f, -18f), terrainCol, new Color(0.25f, 0.75f, 1.0f), 3.0f, 16f);
            SpawnLantern(parent, center + new Vector3(-13f, 0f, 4f), terrainCol, new Color(0.25f, 0.75f, 1.0f), 2.5f, 14f);
            SpawnLantern(parent, center + new Vector3(13f, 0f, 4f), terrainCol, new Color(0.25f, 0.75f, 1.0f), 2.5f, 14f);

            // Sacred Marble Processional Trail
            SpawnStoneTrail(parent, center + new Vector3(0f, 0f, -24f), center + new Vector3(0f, 0f, -4f), 12, _matMarble, terrainCol, 1.6f);

            // Living Temple Priestess & Acolyte NPCs standing tall
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Female.prefab", parent, center + new Vector3(0f, 0f, 0.2f), 180f, terrainCol, "NPC_Priestess_Alyssa");
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", parent, center + new Vector3(-4.5f, 0f, -6f), 55f, terrainCol, "NPC_Acolyte_Kael");
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

            // Alchemy Bench outside Apothecary
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Alchemy_DistillationBench.prefab", parent, center + new Vector3(-7f, 0f, -18f), 25f, 1.15f, terrainCol);

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

            // Stone Walking Trails
            SpawnStoneTrail(parent, center + new Vector3(0f, 0f, -30f), center, 14, _matStone, terrainCol, 1.35f);
            SpawnStoneTrail(parent, center, center + new Vector3(-12f, 0f, -5f), 7, _matStone, terrainCol, 1.2f);
            SpawnStoneTrail(parent, center, center + new Vector3(7f, 0f, 4f), 6, _matStone, terrainCol, 1.2f);
            SpawnStoneTrail(parent, center, center + new Vector3(15f, 0f, 13f), 10, _matStone, terrainCol, 1.2f);

            // Village Guard, Merchant, and Pilgrim NPCs
            SpawnNPC("Assets/Prefabs/NPCs/NPC_CityGuard.prefab", parent, center + new Vector3(0f, 0f, -28f), 180f, terrainCol, "NPC_Eastwatch_Sentry");
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", parent, center + new Vector3(-11f, 0f, -4f), 45f, terrainCol, "NPC_Merchant_Rowan");
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Female.prefab", parent, center + new Vector3(8.2f, 0f, 4.2f), -75f, terrainCol, "NPC_Pilgrim_Lyra");
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

            // Hanging Blacksmithing Sign
            SpawnProp("Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Blacksmithing_Sign_03.prefab", parent, center + new Vector3(-2f, 0f, 2f), -45f, 0.9f, terrainCol);

            // Leaning Work Tools (Pickaxe, Shovel, Iron Axe)
            SpawnProp("Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Pickaxe_Used_04_Iron.prefab", parent, center + new Vector3(-2.8f, 0f, 5.5f), 20f, 1.2f, terrainCol);
            SpawnProp("Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Shovel_Used_03_Iron.prefab", parent, center + new Vector3(-2.4f, 0f, 5.2f), -15f, 1.2f, terrainCol);
            SpawnProp("Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Axe_Used_07_Iron.prefab", parent, center + new Vector3(-9.2f, 0f, 5.2f), 30f, 1.2f, terrainCol);

            // Firewood Stacks & Weapon Racks
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(-12f, 0f, 2f), 15f, 1.4f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", parent, center + new Vector3(-12f, 0f, -4f), 0f, 1.4f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab", parent, center + new Vector3(-1f, 0f, 6.5f), -45f, 1.2f, terrainCol);

            // Wooden Timber Wagon
            SpawnProp("Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Horse_Cart_Used_02.prefab", parent, center + new Vector3(5f, 0f, -14f), 75f, 1.0f, terrainCol);

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

            // Walking Trails
            SpawnStoneTrail(parent, center + new Vector3(0f, 0f, -32f), center, 14, _matStone, terrainCol, 1.3f);
            SpawnStoneTrail(parent, center, center + new Vector3(-5f, 0f, 4f), 6, _matStone, terrainCol, 1.2f);
            SpawnStoneTrail(parent, center, center + new Vector3(14f, 0f, 11f), 9, _matStone, terrainCol, 1.2f);

            // Blacksmith, Scout, and Chalet Keeper NPCs
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", parent, center + new Vector3(-4f, 0f, 4.2f), -135f, terrainCol, "NPC_Blacksmith_Bronn");
            SpawnNPC("Assets/Prefabs/NPCs/NPC_CityGuard.prefab", parent, center + new Vector3(26f, 0f, 0f), -90f, terrainCol, "NPC_Highland_Scout");
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Female.prefab", parent, center + new Vector3(17f, 0f, -10f), -60f, terrainCol, "NPC_Chalet_Keeper");
        }

        // =========================================================================
        // 5. THE SUNKEN CITADEL (SouthWest Forgotten Plateau Ruins: -800, -1000)
        // =========================================================================
        private static void BuildSunkenCitadelAncientRuins(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(-800f, 0f, -1000f);

            // Grand Ancient Ceremonial Gateway Arch (Central)
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Doorway_Arched_low.fbx", parent, center + new Vector3(0f, 0f, 9f), 0f, 3.2f, terrainCol, 0f, false, _matMossyArch);

            // Flanking Ruin Portals (Left Arched & Right Standard)
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Doorway_Arched_low.fbx", parent, center + new Vector3(-8.5f, 0f, 6.5f), -25f, 2.5f, terrainCol, 0f, false, _matMossyArch);
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Doorway_Standard_low.fbx", parent, center + new Vector3(8.5f, 0f, 6.5f), 25f, 2.5f, terrainCol, 0f, false, _matMossyDoorStandard);

            // Courtyard Enclosure Portals (West & East Wings)
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Doorway_Arched_low.fbx", parent, center + new Vector3(-14.5f, 0f, -2.5f), -50f, 2.2f, terrainCol, 0f, false, _matMossyArch);
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Doorway_Standard_low.fbx", parent, center + new Vector3(14.5f, 0f, -2.5f), 50f, 2.2f, terrainCol, 0f, false, _matMossyDoorStandard);

            // Ancient Stone Steps leading up to the gateway
            SpawnProp("Assets/Prefabs/Overworld/Stone_Stairs.prefab", parent, center + new Vector3(0f, 0f, 4.5f), 0f, 0.0015f, terrainCol);

            // Mossy Brass Fire Dishes (Gateway Braziers)
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Fire_Dish_low.fbx", parent, center + new Vector3(-3.4f, 0f, 6f), 0f, 1.8f, terrainCol, 0f, false, _matMossyFireDish);
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Fire_Dish_low.fbx", parent, center + new Vector3(3.4f, 0f, 6f), 0f, 1.8f, terrainCol, 0f, false, _matMossyFireDish);

            // Ancient Weapon Rack & Relic Ironbound Chest
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Weapon_rack_low.fbx", parent, center + new Vector3(-6.5f, 0f, 1.5f), 35f, 1.5f, terrainCol, 0f, false, _matMossyWeaponRack);
            SpawnProp("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Models/Chest_With_Doors_low_2.fbx", parent, center + new Vector3(-4f, 0f, -3f), 25f, 1.4f, terrainCol, 0f, false, _matMossyChest);

            // 8 Ancient Weathered Ruin Pillars of varying broken heights and tilts
            Vector3[] ruinPillars = {
                new Vector3(-14f, 0f, -14f),
                new Vector3(-12f, 0f, 14f),
                new Vector3(16f, 0f, -14f),
                new Vector3(12f, 0f, 18f),
                new Vector3(-6f, 0f, -18f),
                new Vector3(8f, 0f, -18f),
                new Vector3(-18f, 0f, 4f),
                new Vector3(18f, 0f, 4f)
            };
            float[] pillarHeights = { 7.5f, 4.0f, 9.0f, 5.5f, 6.0f, 3.5f, 8.0f, 5.0f };
            float[] pillarTilts = { 4f, -7f, 3f, -5f, 2f, 6f, -4f, 5f };

            for (int i = 0; i < ruinPillars.Length; i++)
            {
                SpawnPillar(parent, center + ruinPillars[i], 1.6f, pillarHeights[i], _matStone, terrainCol, pillarTilts[i]);
            }

            // Weathered Boulders & Fortress Cliff Rock Slabs
            SpawnProp("Assets/Models/TownProps/Prop_WeatheredBoulders.fbx", parent, center + new Vector3(-15f, 0f, 14f), 80f, 1.8f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WeatheredBoulders.fbx", parent, center + new Vector3(18f, 0f, -16f), -40f, 1.8f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Cliff_Boulder.prefab", parent, center + new Vector3(-18f, 0f, -6f), 20f, 0.0016f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Cliff_Boulder.prefab", parent, center + new Vector3(17f, 0f, 14f), -35f, 0.0018f, terrainCol);
            SpawnProp("Assets/Prefabs/Overworld/Cliff_Boulder.prefab", parent, center + new Vector3(-3f, 0f, 16f), 15f, 0.0020f, terrainCol);

            // Hidden Relic Treasure Cache
            SpawnProp("Assets/Models/TownProps/Prop_GoldCoinsChest.fbx", parent, center + new Vector3(-2f, 0f, -2f), 45f, 1.4f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx", parent, center + new Vector3(3.5f, 0f, -3f), -20f, 1.5f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", parent, center + new Vector3(-6f, 0f, -4f), 10f, 1.2f, terrainCol);

            // Spectral Emerald Lanterns
            SpawnLantern(parent, center + new Vector3(-10f, 0f, 0f), terrainCol, new Color(0.2f, 1.0f, 0.6f), 2.8f, 16f);
            SpawnLantern(parent, center + new Vector3(10f, 0f, 0f), terrainCol, new Color(0.2f, 1.0f, 0.6f), 2.8f, 16f);
            SpawnLantern(parent, center + new Vector3(0f, 0f, 16f), terrainCol, new Color(0.2f, 1.0f, 0.6f), 2.8f, 16f);

            // Ruin Stone Walking Path leading to treasure cache
            SpawnStoneTrail(parent, center + new Vector3(0f, 0f, -22f), center + new Vector3(0f, 0f, -3f), 12, _matStone, terrainCol, 1.4f);

            // Living Relic Archaeologist NPC standing tall beside relic cache
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", parent, center + new Vector3(-0.8f, 0f, -1.2f), 35f, terrainCol, "NPC_Archaeologist_Valen");
        }

        // =========================================================================
        // 6. WHISPERING HERMITAGE (SouthWest Riverbank: -500, -700)
        // =========================================================================
        private static void BuildWhisperingHermitage(Transform parent, Collider terrainCol)
        {
            Vector3 center = new Vector3(-500f, 0f, -700f);

            // Majestic 17-meter Stone Arch Bridge spanning along the riverbank
            SpawnProp("Assets/Models/Environment/FantasyStoneArchBridge.fbx", parent, center + new Vector3(14f, 0f, 12f), -40f, 1.0f, terrainCol, 0f, false, _matArchBridgeStone);

            // 2 Solitary Hermitage Cottages
            SpawnHouse("Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab", parent, center + new Vector3(-14f, 0f, 8f), 60f, 5.0f, terrainCol);
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

            // Riverbank Stepping Path connecting cottage to well, bridge, and shrine
            SpawnStoneTrail(parent, center + new Vector3(-10f, 0f, 6f), center, 6, _matStone, terrainCol, 1.2f);
            SpawnStoneTrail(parent, center, center + new Vector3(12f, 0f, 10f), 8, _matStone, terrainCol, 1.2f);
            SpawnStoneTrail(parent, center, center + new Vector3(-2f, 0f, 12f), 7, _matStone, terrainCol, 1.2f);

            // Living Hermit NPC standing on the stone bridge
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", parent, center + new Vector3(13f, 0f, 11f), -40f, terrainCol, "NPC_Hermit_Elden");
        }

        // =========================================================================
        // 7. CONTINENTAL CROSSROADS, ROAD SIGNPOSTS & TRAVELLER WAYPOINTS
        // =========================================================================
        private static void BuildContinentalCrossroads(Transform parent, Collider terrainCol)
        {
            // 4 Major Travel Crossroads connecting the central Metropolis outward
            var crossroads = new (string name, Vector3 pos, float yaw, string armA, string armB)[]
            {
                ("SE_Oakhaven_Crossroads", new Vector3(510f, 0f, -580f), 35f, "Aethelburg Citadel", "Oakhaven Hamlet"),
                ("NE_Pilgrim_Crossroads", new Vector3(510f, 0f, 550f), -45f, "Four Winds Temple", "Eastwatch Village"),
                ("NW_Highland_Crossroads", new Vector3(-480f, 0f, 510f), 135f, "Highland Forge", "Aethelburg Citadel"),
                ("SW_Ruins_Crossroads", new Vector3(-480f, 0f, -640f), -125f, "Sunken Citadel", "Whispering Hermitage")
            };

            foreach (var cr in crossroads)
            {
                ClearVegetationInRadius(cr.pos, 24f);
                SpawnCrossroadsSignpost(parent, cr.pos, cr.yaw, cr.name, terrainCol, cr.armA, cr.armB);
            }
        }

        // =========================================================================
        // HELPER UTILITIES: VEGETATION CLEARING, FENCES, PILLARS, PATHS & NPCS
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

        private static void SpawnStoneTrail(Transform parent, Vector3 start, Vector3 end, int stepCount, Material mat, Collider terrainCol, float width = 1.3f)
        {
            GameObject trailRoot = new GameObject("Stone_Paver_Trail");
            trailRoot.transform.SetParent(parent, false);

            for (int i = 0; i <= stepCount; i++)
            {
                float t = (float)i / stepCount;
                Vector3 pos = Vector3.Lerp(start, end, t);

                // Subtle organic offset
                float jitterX = (Mathf.PerlinNoise(pos.x * 0.12f, pos.z * 0.12f) - 0.5f) * 0.5f;
                float jitterZ = (Mathf.PerlinNoise(pos.z * 0.12f, pos.x * 0.12f) - 0.5f) * 0.5f;
                pos.x += jitterX;
                pos.z += jitterZ;

                float groundY = GetGroundY(pos, terrainCol);
                pos.y = groundY + 0.02f; // Snugly cresting terrain surface

                GameObject paver = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                paver.name = $"Paver_{i}";
                paver.transform.SetParent(trailRoot.transform, false);
                paver.transform.position = pos;
                float d = width * UnityEngine.Random.Range(0.85f, 1.15f);
                paver.transform.localScale = new Vector3(d, 0.03f, d);
                paver.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                if (mat != null) paver.GetComponent<Renderer>().sharedMaterial = mat;

                // Remove collider on decorative stepping stone pavers to conserve physics engine overhead
                var col = paver.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
        }

        private static void SpawnCrossroadsSignpost(Transform parent, Vector3 pos, float yaw, string signName, Collider terrainCol, string armA = null, string armB = null)
        {
            GameObject signObj = new GameObject($"Signpost_{signName}");
            signObj.transform.SetParent(parent, false);
            float groundY = GetGroundY(pos, terrainCol);
            signObj.transform.position = new Vector3(pos.x, groundY, pos.z);
            signObj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Vertical wooden post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post";
            post.transform.SetParent(signObj.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            post.transform.localScale = new Vector3(0.2f, 2.6f, 0.2f);
            if (_matWood != null) post.GetComponent<Renderer>().sharedMaterial = _matWood;

            // Direction arm 1 (pointing forward-right)
            GameObject arm1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm1.name = $"SignArm_{armA ?? "A"}";
            arm1.transform.SetParent(signObj.transform, false);
            arm1.transform.localPosition = new Vector3(0.45f, 2.2f, 0f);
            arm1.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
            arm1.transform.localScale = new Vector3(0.9f, 0.22f, 0.06f);
            if (_matWood != null) arm1.GetComponent<Renderer>().sharedMaterial = _matWood;

            // Direction arm 2 (pointing backward-left)
            GameObject arm2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm2.name = $"SignArm_{armB ?? "B"}";
            arm2.transform.SetParent(signObj.transform, false);
            arm2.transform.localPosition = new Vector3(-0.45f, 1.85f, 0f);
            arm2.transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
            arm2.transform.localScale = new Vector3(0.9f, 0.22f, 0.06f);
            if (_matWood != null) arm2.GetComponent<Renderer>().sharedMaterial = _matWood;

            // Hanging Lantern on post
            SpawnLantern(signObj.transform, pos + signObj.transform.forward * 0.4f, terrainCol, null, 2.0f, 10f);

            // 4-way Crossroads Stone Highway Trails
            SpawnStoneTrail(signObj.transform, pos, pos + signObj.transform.forward * 14f, 8, _matStone, terrainCol, 1.4f);
            SpawnStoneTrail(signObj.transform, pos, pos - signObj.transform.forward * 14f, 8, _matStone, terrainCol, 1.4f);
            SpawnStoneTrail(signObj.transform, pos, pos + signObj.transform.right * 14f, 8, _matStone, terrainCol, 1.4f);
            SpawnStoneTrail(signObj.transform, pos, pos - signObj.transform.right * 14f, 8, _matStone, terrainCol, 1.4f);

            // Rest stop bench & wooden crates
            SpawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", signObj.transform, pos + signObj.transform.right * 2.2f, yaw - 90f, 1.0f, terrainCol);
            SpawnProp("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx", signObj.transform, pos - signObj.transform.right * 1.8f, yaw + 30f, 1.0f, terrainCol);

            // Travelling Merchant Horse Cart & Living Traveller NPC
            SpawnProp("Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Horse_Cart_Used_02.prefab", signObj.transform, pos + signObj.transform.forward * 4.5f + signObj.transform.right * 3.5f, yaw - 25f, 1.0f, terrainCol);
            SpawnNPC("Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", signObj.transform, pos + signObj.transform.right * 2.2f + signObj.transform.forward * 0.8f, yaw + 90f, terrainCol, $"NPC_Traveller_{signName}");
        }

        private static GameObject SpawnNPC(string prefabPath, Transform parent, Vector3 targetPos, float yaw, Collider terrainCol, string displayName = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset == null)
            {
                Debug.LogWarning($"[OverworldSettlements] Could not load NPC at '{prefabPath}'");
                return null;
            }

            GameObject instance = PrefabUtility.IsPartOfPrefabAsset(asset)
                ? (GameObject)PrefabUtility.InstantiatePrefab(asset, parent)
                : UnityEngine.Object.Instantiate(asset, parent);

            if (!string.IsNullOrEmpty(displayName)) instance.name = displayName;
            instance.transform.localScale = Vector3.one * 1.05f;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float groundY = GetGroundY(targetPos, terrainCol);
            // Character models have their pivot at their feet (0, 0, 0).
            // Place directly at ground surface + 0.02f offset so they stand upright on the terrain.
            instance.transform.position = new Vector3(targetPos.x, groundY + 0.02f, targetPos.z);

            var col = instance.GetComponent<Collider>();
            if (col == null)
            {
                var cap = instance.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 1.0f, 0f);
                cap.radius = 0.35f;
                cap.height = 1.9f;
            }

            var anim = instance.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            return instance;
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

        private static GameObject SpawnProp(string assetPath, Transform parent, Vector3 targetPos, float yaw, float scale, Collider terrainCol, float extraYOffset = 0f, bool isHouse = false, Material overrideMat = null)
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
                    if (overrideMat != null)
                    {
                        var mats = mr.sharedMaterials;
                        for (int m = 0; m < mats.Length; m++) mats[m] = overrideMat;
                        mr.sharedMaterials = mats;
                    }
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

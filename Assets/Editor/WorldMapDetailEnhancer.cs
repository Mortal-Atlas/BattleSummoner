using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Master World Map Detail Enhancer:
    /// Populates the continent with rich downloaded assets, branching hydrology,
    /// architectural bridges spanning lake narrows and river forks, and a lively port town.
    /// Uses exact terrain surface sampling so all structures sit naturally on the landscape.
    /// </summary>
    public static class WorldMapDetailEnhancer
    {
        private const string PORT_ROOT_NAME = "Seaward_Port_Town_Lively";
        private const string BRIDGES_ROOT_NAME = "Lake_Bridges_And_Crossings";
        private const string FLORA_ROOT_NAME = "Hydrology_Flora_And_Fauna";
        private const string NATURE_ROOT_NAME = "Riparian_Nature_And_Cliffs";

        private static Vector3[] _terrainVertsCache;
        private static Matrix4x4 _terrainLocalToWorld;

        [MenuItem("Tools/BattleSummoner/Overhaul Map With Downloaded Assets", false, 1)]
        public static void EnhanceEntireWorld()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("==========================================================================");
            Debug.Log("🌟 [WORLD MAP OVERHAUL] REBUILDING PORT TOWN, BRIDGES & EXPANDED HYDROLOGY");
            Debug.Log("==========================================================================");

            CacheTerrainMesh();
            CleanOldRoots();

            // 0. Ensure PBR textures are assigned to continent terrain and village props
            AssignTerrainPBRTextures();
            AssignWoodAndPropsPBRTextures();

            // 1. Lively Port Town
            BuildLivelyPortTown();

            // 2. Lake & River Bridges
            BuildLakeAndRiverBridges();

            // 3. Hydrological Flora
            PopulateLakeAndRiverFlora();

            // 4. Riparian Nature, Willows, and Cliffs
            PopulateRiparianNatureAndCliffs();

            // 5. Stylized Water Shaders on all Hydrology surfaces
            ApplySapphireWaterShader();

            // 6. Cull any floating grass chunks suspended over lake water bodies
            CleanFloatingLakeGrass();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("🎉 [OVERHAUL COMPLETE] World successfully populated and saved!");
        }

        private static Collider _terrainColliderCache;

        private static void CacheTerrainMesh()
        {
            var continent = GameObject.Find("CircularContinent_MacroLevel1");
            if (continent != null)
            {
                _terrainColliderCache = continent.GetComponentInChildren<Collider>();
            }
        }

        public static float GetGroundHeight(float x, float z)
        {
            if (_terrainColliderCache == null)
            {
                CacheTerrainMesh();
            }

            if (_terrainColliderCache != null)
            {
                Ray ray = new Ray(new Vector3(x, 250f, z), Vector3.down);
                if (_terrainColliderCache.Raycast(ray, out RaycastHit hit, 400f))
                {
                    return hit.point.y;
                }
            }
            return 0f;
        }

        public static void CleanOldRoots()
        {
            string[] rootsToClean = new string[] { PORT_ROOT_NAME, BRIDGES_ROOT_NAME, FLORA_ROOT_NAME, NATURE_ROOT_NAME };
            foreach (var rName in rootsToClean)
            {
                var existing = GameObject.Find(rName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                }
            }

            // Ensure old primitive boxes in Seaward_Dockcity_Root are inactive or cleaned
            var dockcityRoot = GameObject.Find("CircularContinent_MacroLevel1/Seaward_Dockcity_Root");
            if (dockcityRoot != null)
            {
                for (int i = 0; i < dockcityRoot.transform.childCount; i++)
                {
                    var child = dockcityRoot.transform.GetChild(i);
                    if (child.name.StartsWith("Pier_") || child.name.StartsWith("Quay_") || child.name.StartsWith("Vessel_") || child.name.StartsWith("Bridge_"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            // Also ensure any leftover primitive boxes in Civil_Infrastructure_Root are inactive
            var civilRoot = GameObject.Find("CircularContinent_MacroLevel1/Civil_Infrastructure_Root");
            if (civilRoot != null)
            {
                for (int i = 0; i < civilRoot.transform.childCount; i++)
                {
                    var child = civilRoot.transform.GetChild(i);
                    if (child.name.StartsWith("Bridge_"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        // =========================================================================
        // 1. SEAWARD PORT TOWN OVERHAUL
        // =========================================================================
        public static void BuildLivelyPortTown()
        {
            var portRoot = new GameObject(PORT_ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(portRoot, "Create Port Town Root");

            var bldgsGroup = new GameObject("01_Port_Buildings");
            bldgsGroup.transform.SetParent(portRoot.transform);

            var docksGroup = new GameObject("02_Docks_And_Moored_Boats");
            docksGroup.transform.SetParent(portRoot.transform);

            var marketGroup = new GameObject("03_Harbor_Market_And_Props");
            marketGroup.transform.SetParent(portRoot.transform);

            var forgeGroup = new GameObject("04_Shipwright_Blacksmith_Forge");
            forgeGroup.transform.SetParent(portRoot.transform);

            var tavernGroup = new GameObject("05_Seaside_Tavern_Terrace");
            tavernGroup.transform.SetParent(portRoot.transform);

            var bastionGroup = new GameObject("06_Offshore_Bastion_Fortress");
            bastionGroup.transform.SetParent(portRoot.transform);

            // A. Buildings (Positioned at exact terrain surface heights!)
            // 1. The Salty Mermaid Tavern & Seaside Lodge
            float tavX = -225.0f, tavZ = 142.0f;
            float tavY = GetGroundHeight(tavX, tavZ);
            var tav = SpawnAsset("Assets/Models/Metropolis/Building_Tavern_Open.fbx", new Vector3(tavX, tavY, tavZ), Quaternion.Euler(0, 110, 0), Vector3.one * 1.0f, bldgsGroup.transform);
            AssignBuildingMaterials(tav);

            float innX = -215.0f, innZ = 152.0f;
            float innY = GetGroundHeight(innX, innZ);
            var inn = SpawnAsset("Assets/Models/Metropolis/Building_Inn_Lodge.fbx", new Vector3(innX, innY, innZ), Quaternion.Euler(0, 100, 0), Vector3.one * 0.95f, bldgsGroup.transform);
            AssignBuildingMaterials(inn);

            // 2. Blacksmith & Shipwright Forge
            float forgeX = -232.0f, forgeZ = 132.0f;
            float forgeY = GetGroundHeight(forgeX, forgeZ);
            var forge = SpawnAsset("Assets/Models/Metropolis/Building_Blacksmith_OpenForge.fbx", new Vector3(forgeX, forgeY, forgeZ), Quaternion.Euler(0, 200, 0), Vector3.one * 1.0f, forgeGroup.transform);
            AssignBuildingMaterials(forge);

            // 3. Port Authority / Customs Guildhall
            float guildX = -205.0f, guildZ = 180.0f;
            float guildY = GetGroundHeight(guildX, guildZ);
            var guild = SpawnAsset("Assets/Models/Metropolis/Building_Guildhall_Grand.fbx", new Vector3(guildX, guildY, guildZ), Quaternion.Euler(0, 280, 0), Vector3.one * 0.95f, bldgsGroup.transform);
            AssignBuildingMaterials(guild);

            // 4. Harbor Apothecary
            float apoX = -236.0f, apoZ = 208.0f;
            float apoY = GetGroundHeight(apoX, apoZ);
            var apo = SpawnAsset("Assets/Models/Metropolis/Building_AlchemistWorkshop.fbx", new Vector3(apoX, apoY, apoZ), Quaternion.Euler(0, 75, 0), Vector3.one * 1.0f, bldgsGroup.transform);
            AssignBuildingMaterials(apo);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Alchemy_DistillationBench.prefab", new Vector3(apoX + 2f, apoY, apoZ - 2f), Quaternion.Euler(0, 160, 0), Vector3.one, bldgsGroup.transform);

            // 5. Port Bakery & General Store
            float bakeX = -228.0f, bakeZ = 175.0f;
            float bakeY = GetGroundHeight(bakeX, bakeZ);
            var bake = SpawnAsset("Assets/Models/Metropolis/Building_Bakery_Shop.fbx", new Vector3(bakeX, bakeY, bakeZ), Quaternion.Euler(0, 260, 0), Vector3.one * 0.95f, bldgsGroup.transform);
            AssignBuildingMaterials(bake);

            float shopX = -218.0f, shopZ = 162.0f;
            float shopY = GetGroundHeight(shopX, shopZ);
            SpawnAsset("Assets/Prefabs/Overworld/Shop_Building_Exterior.prefab", new Vector3(shopX, shopY, shopZ), Quaternion.Euler(0, 275, 0), Vector3.one * 0.11f, bldgsGroup.transform);

            // 6. Waterfront Residential Cottages
            float cott1X = -238.0f, cott1Z = 150.0f;
            float cott1Y = GetGroundHeight(cott1X, cott1Z);
            var cott1 = SpawnAsset("Assets/Models/Metropolis/Building_House_Residential.fbx", new Vector3(cott1X, cott1Y, cott1Z), Quaternion.Euler(0, 85, 0), Vector3.one * 0.95f, bldgsGroup.transform);
            AssignBuildingMaterials(cott1);

            float cott2X = -232.0f, cott2Z = 195.0f;
            float cott2Y = GetGroundHeight(cott2X, cott2Z);
            var cott2 = SpawnAsset("Assets/Models/Metropolis/Building_House_LowDistrict.fbx", new Vector3(cott2X, cott2Y, cott2Z), Quaternion.Euler(0, 115, 0), Vector3.one * 0.95f, bldgsGroup.transform);
            AssignBuildingMaterials(cott2);

            // B. Seaside Tavern Terrace Props
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab", new Vector3(tavX + 4.5f, GetGroundHeight(tavX + 4.5f, tavZ - 3f), tavZ - 3f), Quaternion.Euler(0, 30, 0), Vector3.one, tavernGroup.transform);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab", new Vector3(tavX + 6.0f, GetGroundHeight(tavX + 6.0f, tavZ + 2f), tavZ + 2f), Quaternion.Euler(0, 75, 0), Vector3.one, tavernGroup.transform);
            SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab", new Vector3(tavX + 3.0f, GetGroundHeight(tavX + 3.0f, tavZ - 5f), tavZ - 5f), Quaternion.identity, Vector3.one * 1.0f, tavernGroup.transform);
            SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab", new Vector3(tavX + 2.2f, GetGroundHeight(tavX + 2.2f, tavZ - 4.2f), tavZ - 4.2f), Quaternion.Euler(0, 45, 0), Vector3.one * 1.0f, tavernGroup.transform);
            SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Wooden_Bucket.prefab", new Vector3(tavX + 3.6f, GetGroundHeight(tavX + 3.6f, tavZ - 4.6f), tavZ - 4.6f), Quaternion.identity, Vector3.one, tavernGroup.transform);
            SpawnAsset("Assets/Medieval props/Prefabs/BenchV1.prefab", new Vector3(tavX + 7.5f, GetGroundHeight(tavX + 7.5f, tavZ - 1f), tavZ - 1f), Quaternion.Euler(0, 40, 0), Vector3.one, tavernGroup.transform);
            SpawnAsset("Assets/Medieval props/Prefabs/LanternV1.prefab", new Vector3(tavX + 4.8f, GetGroundHeight(tavX + 4.8f, tavZ - 3f) + 0.8f, tavZ - 3f), Quaternion.identity, Vector3.one * 0.45f, tavernGroup.transform);

            // C. Blacksmith & Forge Equipment
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Anvil_Stump.prefab", new Vector3(forgeX + 3.5f, forgeY, forgeZ - 2f), Quaternion.Euler(0, 110, 0), Vector3.one, forgeGroup.transform);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_CoalBasin.prefab", new Vector3(forgeX + 2.0f, forgeY, forgeZ - 3.5f), Quaternion.identity, Vector3.one, forgeGroup.transform);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Grindstone.prefab", new Vector3(forgeX + 4.5f, forgeY, forgeZ + 1f), Quaternion.Euler(0, 20, 0), Vector3.one, forgeGroup.transform);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_QuenchTrough.prefab", new Vector3(forgeX + 1.2f, forgeY, forgeZ - 1.5f), Quaternion.Euler(0, 90, 0), Vector3.one, forgeGroup.transform);
            SpawnAsset("Assets/UpDraftArt/BlackSmith Props Sampler/Prefabs/Anvil.prefab", new Vector3(forgeX + 3.2f, forgeY, forgeZ - 1.8f), Quaternion.identity, Vector3.one * 1.2f, forgeGroup.transform);
            SpawnAsset("Assets/UpDraftArt/BlackSmith Props Sampler/Prefabs/Coal Pile.prefab", new Vector3(forgeX + 1.0f, forgeY, forgeZ - 4.0f), Quaternion.identity, Vector3.one * 1.3f, forgeGroup.transform);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab", new Vector3(forgeX - 1.5f, forgeY, forgeZ - 3.0f), Quaternion.Euler(0, 15, 0), Vector3.one, forgeGroup.transform);
            SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Drova/Drova01.prefab", new Vector3(forgeX - 2.0f, forgeY, forgeZ - 1.5f), Quaternion.identity, Vector3.one, forgeGroup.transform);

            // D. Harbor Market Square & Quays
            // Central Well and Notice Board
            float wellX = -236.0f, wellZ = 164.0f;
            float wellY = GetGroundHeight(wellX, wellZ);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab", new Vector3(wellX, wellY, wellZ), Quaternion.identity, Vector3.one * 1.15f, marketGroup.transform);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab", new Vector3(wellX + 3.5f, wellY, wellZ + 3f), Quaternion.Euler(0, 240, 0), Vector3.one, marketGroup.transform);

            // Market Booths & Merchant Tents
            float b1X = -240.0f, b1Z = 158.0f;
            SpawnAsset("Assets/Prefabs/Overworld/Market_Booth.prefab", new Vector3(b1X, GetGroundHeight(b1X, b1Z), b1Z), Quaternion.Euler(0, 190, 0), Vector3.one * 0.08f, marketGroup.transform);

            float b2X = -232.0f, b2Z = 172.0f;
            SpawnAsset("Assets/Prefabs/Overworld/Market_Booth.prefab", new Vector3(b2X, GetGroundHeight(b2X, b2Z), b2Z), Quaternion.Euler(0, 280, 0), Vector3.one * 0.08f, marketGroup.transform);

            // Canvas Merchant Tent
            float tentX = -238.0f, tentZ = 175.0f;
            SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Tent.prefab", new Vector3(tentX, GetGroundHeight(tentX, tentZ), tentZ), Quaternion.Euler(0, 120, 0), Vector3.one * 1.2f, marketGroup.transform);

            float cartX = -242.0f, cartZ = 168.0f;
            SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab", new Vector3(cartX, GetGroundHeight(cartX, cartZ), cartZ), Quaternion.Euler(0, 130, 0), Vector3.one, marketGroup.transform);

            // Varied cargo piles along quayside (using detailed Medieval Props Pack 01)
            Vector2[] cargoLocs = new Vector2[] {
                new Vector2(-248.0f, 145.0f),
                new Vector2(-250.0f, 158.0f),
                new Vector2(-246.0f, 172.0f),
                new Vector2(-252.0f, 182.0f),
                new Vector2(-242.0f, 188.0f),
                new Vector2(-246.0f, 136.0f),
            };
            foreach (var cl in cargoLocs)
            {
                float cy = GetGroundHeight(cl.x, cl.y);
                Vector3 cp = new Vector3(cl.x, cy, cl.y);
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Pallets/Pallet_01.prefab", cp, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), Vector3.one, marketGroup.transform);
                SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab", cp + new Vector3(0, 0.35f, 0), Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), Vector3.one, marketGroup.transform);
                SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_02.prefab", cp + new Vector3(0.7f, 0, 0.3f), Quaternion.identity, Vector3.one, marketGroup.transform);
                SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab", cp + new Vector3(-0.6f, 0, 0.2f), Quaternion.identity, Vector3.one, marketGroup.transform);
                SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Rope.prefab", cp + new Vector3(0.4f, 0, -0.5f), Quaternion.Euler(0, 45, 0), Vector3.one, marketGroup.transform);
                SpawnAsset("Assets/Medieval props/Prefabs/LanternV1.prefab", cp + new Vector3(-0.3f, 0.8f, 0.1f), Quaternion.identity, Vector3.one * 0.08f, marketGroup.transform);
            }

            // Flags and Banners at Customs Guildhall
            SpawnAsset("Assets/propsv2/flag1_low.prefab", new Vector3(guildX + 2f, guildY + 5.5f, guildZ - 3f), Quaternion.Euler(0, 110, 0), Vector3.one * 1.6f, marketGroup.transform);
            SpawnAsset("Assets/propsv2/flag2_low.prefab", new Vector3(guildX - 2f, guildY + 5.5f, guildZ + 3f), Quaternion.Euler(0, 70, 0), Vector3.one * 1.6f, marketGroup.transform);

            // Street Lighting along main harbor road
            Vector2[] lampLocations = new Vector2[] {
                new Vector2(-222.0f, 136.0f),
                new Vector2(-228.0f, 148.0f),
                new Vector2(-226.0f, 162.0f),
                new Vector2(-222.0f, 178.0f),
                new Vector2(-232.0f, 185.0f),
                new Vector2(-236.0f, 200.0f),
                new Vector2(-246.0f, 150.0f),
                new Vector2(-248.0f, 175.0f),
            };
            foreach (var lp in lampLocations)
            {
                float ly = GetGroundHeight(lp.x, lp.y);
                SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Urban_StreetLampPost.prefab", new Vector3(lp.x, ly, lp.y), Quaternion.identity, Vector3.one * 1.1f, marketGroup.transform);
            }

            // Fencing along waterfront quayside edge
            for (float z = 128.0f; z <= 202.0f; z += 6.5f)
            {
                float x = -252.0f;
                float fy = GetGroundHeight(x, z);
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Fense/Fense01.prefab", new Vector3(x, fy, z), Quaternion.Euler(0, 90, 0), Vector3.one * 1.3f, marketGroup.transform);
            }

            // E. Docks, Wooden Piers & Moored Boats
            // 3 Finger Piers extending from quayside (-254m) westward out over the water (deck Y = 3.2m)
            BuildFingerPier(new Vector3(-254.0f, 3.2f, 140.0f), 34.0f, 5.5f, -95.0f, docksGroup.transform);
            BuildFingerPier(new Vector3(-254.0f, 3.2f, 165.0f), 40.0f, 6.0f, -90.0f, docksGroup.transform);
            BuildFingerPier(new Vector3(-254.0f, 3.2f, 190.0f), 34.0f, 5.5f, -85.0f, docksGroup.transform);

            // Moored wooden boats floating on water level (Y = 0.35m)
            Vector3[] boatLocations = new Vector3[] {
                new Vector3(-264.0f, 0.35f, 145.0f),
                new Vector3(-274.0f, 0.35f, 136.0f),
                new Vector3(-284.0f, 0.35f, 144.0f),
                new Vector3(-266.0f, 0.35f, 160.0f),
                new Vector3(-276.0f, 0.35f, 170.0f),
                new Vector3(-288.0f, 0.35f, 162.0f),
                new Vector3(-264.0f, 0.35f, 185.0f),
                new Vector3(-274.0f, 0.35f, 195.0f),
                new Vector3(-284.0f, 0.35f, 186.0f),
                new Vector3(-302.0f, 0.35f, 155.0f),
                new Vector3(-308.0f, 0.35f, 175.0f),
            };
            for (int i = 0; i < boatLocations.Length; i++)
            {
                var bp = boatLocations[i];
                float yaw = -80.0f + i * 25.0f;
                var boat = SpawnAsset("Assets/PainterlyAsset/Demo/Prefabs/boat.prefab", bp, Quaternion.Euler(UnityEngine.Random.Range(-1.5f, 1.5f), yaw, UnityEngine.Random.Range(-2f, 2f)), Vector3.one * 1.8f, docksGroup.transform);
                if (boat != null)
                {
                    var demoWater = boat.transform.Find("water_surface");
                    if (demoWater != null)
                    {
                        UnityEngine.Object.DestroyImmediate(demoWater.gameObject);
                    }
                    SpawnAsset("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Rope.prefab", bp + new Vector3(0.5f, 0.3f, 0.5f), Quaternion.identity, Vector3.one, boat.transform);
                }
            }

            // F. Offshore Bastion Island (Strategic coastal landmark)
            float bastX = -370.0f, bastZ = 180.0f;
            float bastY = GetGroundHeight(bastX, bastZ);
            SpawnAsset("Assets/Medieval props/Prefabs/FirePlaceV1.prefab", new Vector3(bastX, bastY + 0.2f, bastZ), Quaternion.identity, Vector3.one * 1.5f, bastionGroup.transform);
            SpawnAsset("Assets/Medieval props/Prefabs/EmbersV1.prefab", new Vector3(bastX, bastY + 0.3f, bastZ), Quaternion.identity, Vector3.one * 1.5f, bastionGroup.transform);
            SpawnAsset("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab", new Vector3(bastX + 4f, bastY, bastZ + 2f), Quaternion.Euler(0, 45, 0), Vector3.one, bastionGroup.transform);
            SpawnAsset("Assets/Medieval props/Prefabs/BenchV1.prefab", new Vector3(bastX - 3f, bastY, bastZ - 2f), Quaternion.Euler(0, 120, 0), Vector3.one, bastionGroup.transform);
            SpawnAsset("Assets/Medieval props/Prefabs/LanternV2.prefab", new Vector3(bastX + 2f, bastY + 1.2f, bastZ + 4f), Quaternion.identity, Vector3.one * 1.4f, bastionGroup.transform);

            Debug.Log("⚓ [PORT TOWN COMPLETE] Seaward Port Town lively with timber buildings, piers, boats, forge, and market!");
        }

        private static void BuildFingerPier(Vector3 rootPos, float length, float width, float angleDeg, Transform parent)
        {
            if (rootPos.y < 19.0f) return; // STRICT DRY LAND CHECK: Ocean surface is at Y=18.0m
            var pierGO = new GameObject($"Finger_Pier_{length:F0}m");
            pierGO.transform.SetParent(parent);
            pierGO.transform.position = rootPos;
            pierGO.transform.rotation = Quaternion.Euler(0, angleDeg, 0);

            // board01 length (3.03m) is along Y. Rotate (90, 90, 0) to lay flat across the pier width!
            float lengthScale = width / 3.03f;
            int numPlanks = Mathf.CeilToInt(length / 0.60f);
            for (int i = 0; i < numPlanks; i++)
            {
                float zOffset = i * 0.60f;
                var plankPos = rootPos + pierGO.transform.forward * zOffset;
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Boards/board01.prefab", plankPos, pierGO.transform.rotation * Quaternion.Euler(90, 90, 0), new Vector3(2.8f, lengthScale, 1.0f), pierGO.transform);
            }

            // log01 length (5.4m) is along Z. Rotate 90 deg around X to stand vertically as piling!
            int numBents = Mathf.CeilToInt(length / 5.5f);
            for (int b = 0; b <= numBents; b++)
            {
                float zOffset = b * 5.4f;
                Vector3 center = rootPos + pierGO.transform.forward * zOffset;
                Vector3 leftPiling = center - pierGO.transform.right * (width * 0.48f);
                Vector3 rightPiling = center + pierGO.transform.right * (width * 0.48f);

                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Logs/log01.prefab", new Vector3(leftPiling.x, 1.2f, leftPiling.z), Quaternion.Euler(90, 0, 0), new Vector3(0.8f, 0.8f, 0.8f), pierGO.transform);
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Logs/log01.prefab", new Vector3(rightPiling.x, 1.2f, rightPiling.z), Quaternion.Euler(90, 0, 0), new Vector3(0.8f, 0.8f, 0.8f), pierGO.transform);

                if (b == numBents)
                {
                    SpawnAsset("Assets/Medieval props/Prefabs/LanternV1.prefab", leftPiling + Vector3.up * 0.3f, Quaternion.identity, Vector3.one * 0.08f, pierGO.transform);
                    SpawnAsset("Assets/Medieval props/Prefabs/LanternV1.prefab", rightPiling + Vector3.up * 0.3f, Quaternion.identity, Vector3.one * 0.08f, pierGO.transform);
                    SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Ladder/ladder_01.prefab", center + pierGO.transform.forward * 1.2f, pierGO.transform.rotation, Vector3.one, pierGO.transform);
                }
            }
        }

        // =========================================================================
        // 2. LAKE BRIDGES & RIVER CROSSINGS
        // =========================================================================
        public static void BuildLakeAndRiverBridges()
        {
            var bridgesRoot = new GameObject(BRIDGES_ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(bridgesRoot, "Create Bridges Root");

            var matDeck = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_Deck.mat");
            var matStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_Stone.mat");
            var matTrim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_Trim.mat");
            var matCrystal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_ArchBridge_LanternCrystal.mat");

            // Bridge 1: Grand Stone Arch Bridge over Lake Brestuoil Narrows
            var brestuoilBridge = SpawnAsset("Assets/Models/Environment/FantasyStoneArchBridge.fbx", new Vector3(-315.0f, 22.2f, -258.0f), Quaternion.Euler(0, 42.0f, 0), new Vector3(1.8f, 1.3f, 1.5f), bridgesRoot.transform);
            if (brestuoilBridge != null)
            {
                brestuoilBridge.name = "Bridge_Lake_Brestuoil_StoneArch";
                ApplyArchBridgeMaterials(brestuoilBridge, matDeck, matStone, matTrim, matCrystal);

                // Parent lanterns to bridgesRoot with realistic human-scaled size (0.09x)
                SpawnAsset("Assets/Medieval props/Prefabs/LanternV1.prefab", new Vector3(-321.0f, 25.2f, -263.5f), Quaternion.identity, Vector3.one * 0.09f, bridgesRoot.transform);
                SpawnAsset("Assets/Medieval props/Prefabs/LanternV1.prefab", new Vector3(-309.0f, 25.2f, -252.5f), Quaternion.identity, Vector3.one * 0.09f, bridgesRoot.transform);
            }

            // Bridge 2: Wooden Trestle Boardwalk Bridge across Silvermirror Lake
            var smBridge = new GameObject("Bridge_Silvermirror_Lake_TrestleBoardwalk");
            smBridge.transform.SetParent(bridgesRoot.transform);
            smBridge.transform.position = new Vector3(-75.0f, 10.8f, 235.0f);
            smBridge.transform.rotation = Quaternion.Euler(0, -18.0f, 0);

            float smLen = 36.0f;
            float smWidth = 4.8f;
            float smPlankLenScale = smWidth / 3.03f;
            for (float d = -smLen * 0.5f; d <= smLen * 0.5f; d += 0.60f)
            {
                Vector3 deckPos = smBridge.transform.position + smBridge.transform.forward * d;
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Boards/board01.prefab", deckPos, smBridge.transform.rotation * Quaternion.Euler(90, 90, 0), new Vector3(2.8f, smPlankLenScale, 1.0f), smBridge.transform);
            }
            for (float d = -smLen * 0.5f; d <= smLen * 0.5f; d += 2.45f)
            {
                Vector3 fencePos = smBridge.transform.position + smBridge.transform.forward * d;
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Fense/Fense02.prefab", fencePos - smBridge.transform.right * 2.35f + Vector3.up * 0.1f, smBridge.transform.rotation, new Vector3(1.0f, 0.65f, 1.0f), smBridge.transform);
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Fense/Fense02.prefab", fencePos + smBridge.transform.right * 2.35f + Vector3.up * 0.1f, smBridge.transform.rotation, new Vector3(1.0f, 0.65f, 1.0f), smBridge.transform);
            }
            for (float d = -smLen * 0.5f; d <= smLen * 0.5f; d += 5.5f)
            {
                Vector3 bentCenter = smBridge.transform.position + smBridge.transform.forward * d;
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Logs/log01.prefab", new Vector3(bentCenter.x - smBridge.transform.right.x * 2.2f, 8.5f, bentCenter.z - smBridge.transform.right.z * 2.2f), Quaternion.Euler(90, 0, 0), new Vector3(0.8f, 0.8f, 0.8f), smBridge.transform);
                SpawnAsset("Assets/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Logs/log01.prefab", new Vector3(bentCenter.x + smBridge.transform.right.x * 2.2f, 8.5f, bentCenter.z + smBridge.transform.right.z * 2.2f), Quaternion.Euler(90, 0, 0), new Vector3(0.8f, 0.8f, 0.8f), smBridge.transform);
            }
            SpawnAsset("Assets/Medieval props/Prefabs/LanternV2.prefab", smBridge.transform.position - smBridge.transform.forward * 18.0f + Vector3.up * 0.8f, Quaternion.identity, Vector3.one * 0.08f, bridgesRoot.transform);
            SpawnAsset("Assets/Medieval props/Prefabs/LanternV2.prefab", smBridge.transform.position + smBridge.transform.forward * 18.0f + Vector3.up * 0.8f, Quaternion.identity, Vector3.one * 0.08f, bridgesRoot.transform);

            // Bridge 3: Highland Tarn Cascade Footbridge
            var htBridge = SpawnAsset("Assets/Models/Environment/FantasyStoneArchBridge.fbx", new Vector3(92.0f, 18.0f, -235.0f), Quaternion.Euler(0, 68.0f, 0), new Vector3(0.95f, 0.80f, 0.95f), bridgesRoot.transform);
            if (htBridge != null)
            {
                htBridge.name = "Bridge_Highland_Tarn_Footbridge";
                ApplyArchBridgeMaterials(htBridge, matDeck, matStone, matTrim, matCrystal);
                SpawnAsset("Assets/Medieval props/Prefabs/LanternV1.prefab", new Vector3(91.5f, 20.5f, -241.0f), Quaternion.identity, Vector3.one * 0.08f, bridgesRoot.transform);
                SpawnAsset("Assets/Medieval props/Prefabs/LanternV1.prefab", new Vector3(92.5f, 20.5f, -229.0f), Quaternion.identity, Vector3.one * 0.08f, bridgesRoot.transform);
            }

            // Bridge 4: Southern River Fork Viaduct
            var forkViaduct = SpawnAsset("Assets/Models/Environment/FantasyStoneArchBridge.fbx", new Vector3(-10.0f, 11.2f, 190.0f), Quaternion.Euler(0, 90.0f, 0), new Vector3(1.6f, 1.2f, 1.4f), bridgesRoot.transform);
            if (forkViaduct != null)
            {
                forkViaduct.name = "Bridge_Southern_River_Fork_Viaduct";
                ApplyArchBridgeMaterials(forkViaduct, matDeck, matStone, matTrim, matCrystal);

                SpawnAsset("Assets/Medieval props/Prefabs/LanternV3.prefab", new Vector3(-18.0f, 14.2f, 190.0f), Quaternion.identity, Vector3.one * 0.09f, bridgesRoot.transform);
                SpawnAsset("Assets/Medieval props/Prefabs/LanternV3.prefab", new Vector3(-2.0f, 14.2f, 190.0f), Quaternion.identity, Vector3.one * 0.09f, bridgesRoot.transform);
            }

            Debug.Log("🌉 [BRIDGES COMPLETE] 4 architectural bridges spanning lakes and river forks instantiated and textured!");
        }

        private static void ApplyArchBridgeMaterials(GameObject bridge, Material matDeck, Material matStone, Material matTrim, Material matCrystal)
        {
            var rend = bridge.GetComponent<Renderer>();
            if (rend != null)
            {
                var mats = rend.sharedMaterials;
                if (mats.Length >= 4)
                {
                    if (matDeck != null) mats[0] = matDeck;
                    if (matStone != null) mats[1] = matStone;
                    if (matTrim != null) mats[2] = matTrim;
                    if (matCrystal != null) mats[3] = matCrystal;
                    rend.sharedMaterials = mats;
                    EditorUtility.SetDirty(rend);
                }
            }
        }

        // =========================================================================
        // 3. HYDROLOGICAL FLORA & LAKE DRESSING
        // =========================================================================
        public static void PopulateLakeAndRiverFlora()
        {
            var floraRoot = new GameObject(FLORA_ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(floraRoot, "Create Flora Root");

            var lakes = new (Vector3 center, float radius, float waterY, string name)[] {
                (new Vector3(-335.0f, 19.5f, -275.0f), 38.0f, 19.5f, "Lake_Brestuoil_North"),
                (new Vector3(-295.0f, 19.5f, -240.0f), 35.0f, 19.5f, "Lake_Brestuoil_South"),
                (new Vector3(110.0f, 16.5f, -235.0f), 32.0f, 16.5f, "Highland_Tarn"),
                (new Vector3(-210.0f, 16.0f, -310.0f), 34.0f, 16.0f, "Emerald_Lily_Lake"),
                (new Vector3(-85.0f, 9.2f, 235.0f), 40.0f, 9.2f, "Silvermirror_Lake"),
            };

            foreach (var lake in lakes)
            {
                var lakeGroup = new GameObject($"Flora_{lake.name}");
                lakeGroup.transform.SetParent(floraRoot.transform);

                int numLilies = UnityEngine.Random.Range(14, 22);
                for (int i = 0; i < numLilies; i++)
                {
                    float ang = UnityEngine.Random.Range(0, Mathf.PI * 2f);
                    float dist = UnityEngine.Random.Range(6.0f, lake.radius * 0.75f);
                    Vector3 lilyPos = lake.center + new Vector3(Mathf.Cos(ang) * dist, 0.05f, Mathf.Sin(ang) * dist);
                    lilyPos.y = lake.waterY + 0.05f;

                    string lilyPrefab = (i % 2 == 0) ? "Assets/Idyllic Fantasy Nature/Prefabs/Waterlily_01.prefab" : "Assets/Idyllic Fantasy Nature/Prefabs/Waterlily_02.prefab";
                    SpawnAsset(lilyPrefab, lilyPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), Vector3.one * UnityEngine.Random.Range(1.0f, 1.5f), lakeGroup.transform);

                    string padPrefab = $"Assets/Idyllic Fantasy Nature/Prefabs/LilyPads_0{UnityEngine.Random.Range(1, 4)}.prefab";
                    SpawnAsset(padPrefab, lilyPos + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0, UnityEngine.Random.Range(-1.5f, 1.5f)), Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), Vector3.one * UnityEngine.Random.Range(1.1f, 1.6f), lakeGroup.transform);
                }

                int numReeds = UnityEngine.Random.Range(20, 30);
                for (int i = 0; i < numReeds; i++)
                {
                    float ang = (i / (float)numReeds) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.1f, 0.1f);
                    float dist = lake.radius + UnityEngine.Random.Range(-2.5f, 2.0f);
                    Vector3 reedPos = lake.center + new Vector3(Mathf.Cos(ang) * dist, 0, Mathf.Sin(ang) * dist);
                    reedPos.y = lake.waterY;

                    string reedPrefab = (i % 2 == 0)
                        ? $"Assets/Idyllic Fantasy Nature/Prefabs/Cattail_0{UnityEngine.Random.Range(1, 4)}.prefab"
                        : $"Assets/Idyllic Fantasy Nature/Prefabs/Reeds_0{UnityEngine.Random.Range(1, 4)}.prefab";

                    SpawnAsset(reedPrefab, reedPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), Vector3.one * UnityEngine.Random.Range(1.0f, 1.4f), lakeGroup.transform);
                }

                for (int i = 0; i < 6; i++)
                {
                    float ang = UnityEngine.Random.Range(0, Mathf.PI * 2f);
                    float dist = UnityEngine.Random.Range(10.0f, lake.radius * 0.7f);
                    Vector3 leafPos = lake.center + new Vector3(Mathf.Cos(ang) * dist, 0.08f, Mathf.Sin(ang) * dist);
                    leafPos.y = lake.waterY + 0.08f;
                    SpawnAsset("Assets/Idyllic Fantasy Nature/Prefabs/FloatingLeafs_Green.prefab", leafPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), Vector3.one * 1.4f, lakeGroup.transform);
                }
            }

            Debug.Log("🪷 [HYDROLOGICAL FLORA COMPLETE] Waterlilies, cattails, reeds, and floating leaves populated!");
        }

        // =========================================================================
        // 4. RIPARIAN WILLOWS, CLIFFS & NATURE DRESSING
        // =========================================================================
        public static void PopulateRiparianNatureAndCliffs()
        {
            var natureRoot = new GameObject(NATURE_ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(natureRoot, "Create Nature Root");

            var willowsGroup = new GameObject("01_Riparian_Willow_Trees");
            willowsGroup.transform.SetParent(natureRoot.transform);

            var cliffsGroup = new GameObject("02_River_Gorge_Cliffs_And_Boulders");
            cliffsGroup.transform.SetParent(natureRoot.transform);

            var grovesGroup = new GameObject("03_Blossom_Groves_And_Wildflowers");
            grovesGroup.transform.SetParent(natureRoot.transform);

            Vector3[] willowLocations = new Vector3[] {
                new Vector3(-345.0f, 19.8f, -255.0f),
                new Vector3(-328.0f, 19.8f, -295.0f),
                new Vector3(-280.0f, 19.8f, -225.0f),
                new Vector3(-305.0f, 20.0f, -270.0f),
                new Vector3(-115.0f, 9.6f, 225.0f),
                new Vector3(-65.0f, 9.6f, 255.0f),
                new Vector3(-42.0f, 9.6f, 245.0f),
                new Vector3(-95.0f, 9.6f, 260.0f),
                new Vector3(-235.0f, 16.4f, -295.0f),
                new Vector3(-195.0f, 16.4f, -325.0f),
                new Vector3(135.0f, 16.8f, -250.0f),
                new Vector3(130.0f, 16.8f, -220.0f),
                new Vector3(-35.0f, 10.2f, 195.0f),
                new Vector3(5.0f, 10.2f, 160.0f),
                new Vector3(-18.0f, 9.5f, 225.0f),
                new Vector3(15.0f, 9.0f, 250.0f),
            };

            for (int i = 0; i < willowLocations.Length; i++)
            {
                int willowIdx = (i % 5) + 1;
                string willowPrefab = $"Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_0{willowIdx}_Green.prefab";
                float yaw = UnityEngine.Random.Range(0, 360);
                float scale = UnityEngine.Random.Range(1.1f, 1.5f);
                var pos = willowLocations[i];
                pos.y = GetGroundHeight(pos.x, pos.z);
                SpawnAsset(willowPrefab, pos, Quaternion.Euler(0, yaw, 0), Vector3.one * scale, willowsGroup.transform);
            }

            Vector3[] cliffLocations = new Vector3[] {
                new Vector3(-16.0f, 24.0f, -310.0f),
                new Vector3(18.0f, 26.0f, -290.0f),
                new Vector3(-15.0f, 20.0f, -250.0f),
                new Vector3(20.0f, 21.0f, -210.0f),
                new Vector3(-12.0f, 16.0f, -160.0f),
                new Vector3(14.0f, 15.0f, -130.0f),
                new Vector3(-38.0f, 14.0f, 195.0f),
                new Vector3(28.0f, 14.0f, 205.0f),
                new Vector3(-20.0f, 11.0f, 280.0f),
                new Vector3(35.0f, 11.0f, 310.0f),
                new Vector3(-15.0f, 7.0f, 370.0f),
                new Vector3(40.0f, 6.0f, 400.0f),
                new Vector3(-355.0f, 22.0f, -280.0f),
                new Vector3(-310.0f, 22.0f, -220.0f),
                new Vector3(135.0f, 20.0f, -245.0f),
                new Vector3(85.0f, 19.5f, -220.0f),
            };

            for (int i = 0; i < cliffLocations.Length; i++)
            {
                int cliffIdx = (i % 5) + 1;
                string cliffPrefab = $"Assets/Idyllic Fantasy Nature/Prefabs/Cliff_0{cliffIdx}.prefab";
                float yaw = UnityEngine.Random.Range(0, 360);
                float scale = UnityEngine.Random.Range(1.3f, 2.2f);
                var pos = cliffLocations[i];
                pos.y = GetGroundHeight(pos.x, pos.z);
                SpawnAsset(cliffPrefab, pos, Quaternion.Euler(0, yaw, 0), Vector3.one * scale, cliffsGroup.transform);

                int rockIdx = (i % 3) + 1;
                string rockPrefab = $"Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_0{rockIdx}.prefab";
                SpawnAsset(rockPrefab, pos + new Vector3(UnityEngine.Random.Range(-5f, 5f), -1f, UnityEngine.Random.Range(-5f, 5f)), Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0), Vector3.one * UnityEngine.Random.Range(1.2f, 1.8f), cliffsGroup.transform);
            }

            Vector2[] meadowCoords = new Vector2[] {
                new Vector2(-180.0f, 110.0f),
                new Vector2(-190.0f, 125.0f),
                new Vector2(-175.0f, 160.0f),
                new Vector2(-185.0f, 180.0f),
                new Vector2(-200.0f, 225.0f),
                new Vector2(-150.0f, 140.0f),
                new Vector2(-130.0f, 160.0f),
            };

            for (int i = 0; i < meadowCoords.Length; i++)
            {
                var mc = meadowCoords[i];
                float my = GetGroundHeight(mc.x, mc.y);
                Vector3 mp = new Vector3(mc.x, my, mc.y);
                int bIdx = (i % 5) + 1;
                SpawnAsset($"Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_0{bIdx}.prefab", mp, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0), Vector3.one * UnityEngine.Random.Range(1.1f, 1.4f), grovesGroup.transform);

                SpawnAsset("Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab", mp + new Vector3(2.5f, 0, 1.2f), Quaternion.identity, Vector3.one * 1.5f, grovesGroup.transform);
                SpawnAsset("Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab", mp + new Vector3(-2.2f, 0, -1.8f), Quaternion.identity, Vector3.one * 1.5f, grovesGroup.transform);
                SpawnAsset("Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab", mp + new Vector3(1.0f, 0, -2.5f), Quaternion.identity, Vector3.one * 1.5f, grovesGroup.transform);

                SpawnAsset("Assets/NatureStarterKit2/Nature/bush01.prefab", mp + new Vector3(-3.5f, 0, 2.0f), Quaternion.identity, Vector3.one * 1.2f, grovesGroup.transform);
                SpawnAsset("Assets/NatureStarterKit2/Nature/bush02.prefab", mp + new Vector3(3.0f, 0, -3.0f), Quaternion.identity, Vector3.one * 1.2f, grovesGroup.transform);
            }

            Debug.Log("🌳 [NATURE COMPLETE] Willows, gorge cliffs, blossom groves, and wildflowers populated!");
        }

        // =========================================================================
        // 5. STYLIZED WATER SHADER
        // =========================================================================
        public static void ApplySapphireWaterShader()
        {
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_DA_WaterSapphire.mat");
            if (waterMat == null)
            {
                Debug.LogWarning("[Water Populator] M_DA_WaterSapphire.mat not found.");
                return;
            }

            var hydro = GameObject.Find("CircularContinent_MacroLevel1/Hydrology_Network_Root");
            if (hydro == null) return;

            int count = 0;
            var renderers = hydro.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.sharedMaterial = waterMat;
                count++;
            }

            Debug.Log($"🌊 [WATER SHADER] Applied M_DA_WaterSapphire to {count} Hydrology surfaces!");
        }

        // =========================================================================
        // 6. CULL FLOATING LAKE GRASS
        // =========================================================================
        public static void CleanFloatingLakeGrass()
        {
            var grassRoot = GameObject.Find("--- WORLD WIND GRASS ---");
            if (grassRoot == null) return;

            var lakes = new (Vector3 center, float r, float waterY)[] {
                (new Vector3(110f, 16.5f, -235f), 38f, 16.5f),
                (new Vector3(-85f, 9.2f, 235f), 45f, 9.2f),
                (new Vector3(-335f, 19.5f, -275f), 45f, 19.5f),
                (new Vector3(-295f, 19.5f, -240f), 45f, 19.5f),
                (new Vector3(-210f, 16.0f, -310f), 40f, 16.0f)
            };

            int removed = 0;
            var toDestroy = new List<GameObject>();
            foreach (Transform t in grassRoot.transform)
            {
                var r = t.GetComponent<Renderer>();
                if (r == null) continue;
                var c = r.bounds.center;
                foreach (var l in lakes)
                {
                    float d = Vector2.Distance(new Vector2(c.x, c.z), new Vector2(l.center.x, l.center.z));
                    if (d < l.r && c.y > l.waterY + 1.0f)
                    {
                        toDestroy.Add(t.gameObject);
                        break;
                    }
                }
            }

            foreach (var go in toDestroy)
            {
                Undo.DestroyObjectImmediate(go);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log($"🌾 [GRASS CLEANUP] Culled {removed} floating grass chunks suspended over water bodies.");
            }
        }

        // Helper
        private static GameObject SpawnAsset(string assetPath, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent)
        {
            // STRICT WATER EXCLUSION: Water level is Y=18.0m. Skip any asset placed in or below water!
            if (position.y < 19.0f) return null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return null;

            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }
        public static void AssignBuildingMaterials(GameObject building)
        {
            if (building == null) return;
            foreach (var r in building.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string cleanName = mats[i].name;
                    if (cleanName.EndsWith(" (Instance)")) cleanName = cleanName.Substring(0, cleanName.Length - 11);
                    var targetMat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Metropolis/{cleanName}.mat");
                    if (targetMat != null)
                    {
                        mats[i] = targetMat;
                        changed = true;
                    }
                }
                if (changed)
                {
                    r.sharedMaterials = mats;
                    EditorUtility.SetDirty(r);
                }
            }
        }

        public static void AssignTerrainPBRTextures()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CircularContinent_MacroTerrain.mat");
            if (mat == null) return;

            mat.SetTexture("_GrassTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Grass/Grass_Albedo.png"));
            mat.SetTexture("_GrassNormal", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Grass/Grass_Normal.png"));
            mat.SetTexture("_RockTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Rock/Rock_Albedo.png"));
            mat.SetTexture("_RockNormal", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Rock/Rock_Normal.png"));
            mat.SetTexture("_SandTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Sand/Sand_Albedo.png"));
            mat.SetTexture("_SandNormal", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Sand/Sand_Normal.png"));
            mat.SetTexture("_CobbleTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Cobblestone/Cobblestone_Albedo.png"));
            mat.SetTexture("_CobbleNormal", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Cobblestone/Cobblestone_Normal.png"));
            mat.SetFloat("_GrassTiling", 0.08f);
            mat.SetFloat("_RockTiling", 0.04f);
            mat.SetFloat("_SandTiling", 0.06f);
            mat.SetFloat("_CobbleTiling", 0.12f);
            mat.SetFloat("_CliffSlopeThreshold", 0.62f);
            mat.SetFloat("_CliffBlendSmoothness", 0.15f);

            EditorUtility.SetDirty(mat);
        }

        public static void AssignWoodAndPropsPBRTextures()
        {
            var mapping = new (string matPath, string albPath, string metPath, string nrmPath)[] {
                ("Assets/NikolayFedorov/PBR_Tiled/MaterialsPBR/Wood_Bark01.mat",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/Wood_Bark01/plane_3_3_DefaultMaterial_AlbedoTransparency.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/Wood_Bark01/plane_3_3_DefaultMaterial_MetallicSmoothness.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/Wood_Bark01/plane_3_3_DefaultMaterial_Normal.tga"),

                ("Assets/NikolayFedorov/PBR_Tiled/MaterialsPBR/WoodBark02.mat",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodBark02/plane_3_3_DefaultMaterial_AlbedoTransparency.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodBark02/plane_3_3_DefaultMaterial_MetallicSmoothness.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodBark02/plane_3_3_DefaultMaterial_Normal.tga"),

                ("Assets/NikolayFedorov/PBR_Tiled/MaterialsPBR/WoodBark03.mat",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodBark03/plane_3_3_DefaultMaterial_AlbedoTransparency.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodBark03/plane_3_3_DefaultMaterial_MetallicSmoothness.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodBark03/plane_3_3_DefaultMaterial_Normal.tga"),

                ("Assets/NikolayFedorov/PBR_Tiled/MaterialsPBR/Wood_planks_05.mat",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodPlanks05/plane_3_3_DefaultMaterial_AlbedoTransparency.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodPlanks05/plane_3_3_DefaultMaterial_MetallicSmoothness.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodPlanks05/plane_3_3_DefaultMaterial_Normal.tga"),

                ("Assets/NikolayFedorov/PBR_Tiled/MaterialsPBR/Wood_planks_08.mat",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodPlanks08/plane_3_3_DefaultMaterial_AlbedoTransparency.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodPlanks08/plane_3_3_DefaultMaterial_MetallicSmoothness.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Wood/WoodPlanks08/plane_3_3_DefaultMaterial_Normal.tga"),

                ("Assets/NikolayFedorov/PBR_Tiled/MaterialsPBR/Roof_metall01.mat",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Roof/Roof_metall01/plane_3_3_DefaultMaterial_AlbedoTransparency.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Roof/Roof_metall01/plane_3_3_DefaultMaterial_MetallicSmoothness.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Roof/Roof_metall01/plane_3_3_DefaultMaterial_Normal.tga"),

                ("Assets/NikolayFedorov/PBR_Tiled/MaterialsPBR/Rust.mat",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Metall/Rust/plane_3_3_DefaultMaterial_AlbedoTransparency.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Metall/Rust/plane_3_3_DefaultMaterial_MetallicSmoothness.tga",
                 "Assets/NikolayFedorov/PBR_Tiled/Textures/Metall/Rust/plane_3_3_DefaultMaterial_Normal.tga")
            };

            foreach (var m in mapping) {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(m.matPath);
                if (mat == null) continue;
                var alb = AssetDatabase.LoadAssetAtPath<Texture2D>(m.albPath);
                var met = AssetDatabase.LoadAssetAtPath<Texture2D>(m.metPath);
                var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(m.nrmPath);

                if (alb != null) mat.SetTexture("_BaseMap", alb);
                if (met != null) mat.SetTexture("_MetallicGlossMap", met);
                if (nrm != null) mat.SetTexture("_BumpMap", nrm);
                EditorUtility.SetDirty(mat);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattleSummoner.Editor.World
{
    /// <summary>
    /// MasterHousePersonalizationPipeline
    /// Personalizes each house across the metropolis to make the city feel richly lived-in,
    /// authentic, warm, and animated with domestic life.
    ///
    /// Implements:
    /// 1. Diverse Roof Slate Palette (6 harmonic anime colorways assigned by neighborhood cluster).
    /// 2. Front Doorway System (wooden door panel, iron/brass handle, flagstone doorstep, ajar doors).
    /// 3. Warm Interior Window Glow (welcoming amber/honey lighting).
    /// 4. Doorway Wall Sconces & Lanterns (warm cast-iron lanterns beside doors).
    /// 5. Window Flower Boxes & Stoop Herb Pots (blooming marigolds, poppies, azure bells, lavender).
    /// 6. Domestic Clutter (stacked firewood logs, rainwater barrels, flour sacks, storage crates).
    /// 7. Porch Furniture & Craftsman Gear (porch benches, leaning ladders, handcarts).
    /// 8. Chimney Hearth Smoke VFX (gentle billowing anime smoke puffs from active hearths).
    /// 9. Trade Signs & Guild Plaques (hanging shop signs for bakeries, forges, apothecaries, taverns).
    /// 10. Rooftop Critters & Festive Street Paper Lanterns (perched owls and strings of lanterns).
    /// </summary>
    public static class MasterHousePersonalizationPipeline
    {
        private const string ROW_HOUSES_PATH = "8Vector_Metropolis_Root/01_Tier_Stepped_RowHouses";
        private const string LIVED_IN_CONTAINER_NAME = "LIVED_IN_DETAILS";

        // Building local coordinate metadata (frontage, depth, height, chimney local offset, front door facade Z)
        private struct HouseDimensions
        {
            public float frontage;
            public float depth;
            public float height;
            public float facadeZ;
            public Vector3 chimneyOffset;
            public bool hasChimney;
        }

        private static readonly Dictionary<string, HouseDimensions> ModelSpecs = new Dictionary<string, HouseDimensions>
        {
            { "Building_House_Residential", new HouseDimensions { frontage = 9.5f, depth = 11.5f, height = 11.4f, facadeZ = 5.05f, chimneyOffset = new Vector3(-2.13f, 11.40f, -1.05f), hasChimney = true } },
            { "Building_House_LowDistrict", new HouseDimensions { frontage = 7.6f, depth = 9.0f, height = 6.8f, facadeZ = 4.10f, chimneyOffset = new Vector3(0.60f, 6.80f, -3.10f), hasChimney = true } },
            { "Building_Inn_Lodge", new HouseDimensions { frontage = 12.0f, depth = 15.9f, height = 11.2f, facadeZ = 8.70f, chimneyOffset = new Vector3(2.20f, 11.20f, 0.50f), hasChimney = true } },
            { "Building_Bakery_Shop", new HouseDimensions { frontage = 9.5f, depth = 11.2f, height = 7.3f, facadeZ = 5.85f, chimneyOffset = new Vector3(-1.80f, 7.30f, -2.00f), hasChimney = true } },
            { "Building_Blacksmith_OpenForge", new HouseDimensions { frontage = 14.3f, depth = 11.7f, height = 7.2f, facadeZ = 4.05f, chimneyOffset = new Vector3(-3.50f, 7.20f, 1.00f), hasChimney = true } },
            { "Building_AlchemistWorkshop", new HouseDimensions { frontage = 11.2f, depth = 11.2f, height = 10.0f, facadeZ = 5.60f, chimneyOffset = new Vector3(1.50f, 10.00f, -1.50f), hasChimney = true } },
            { "Building_EnchanterWorkshop", new HouseDimensions { frontage = 11.2f, depth = 11.2f, height = 10.2f, facadeZ = 5.60f, chimneyOffset = new Vector3(1.80f, 10.20f, -1.20f), hasChimney = true } },
            { "Building_GuildArmory", new HouseDimensions { frontage = 11.5f, depth = 14.0f, height = 8.5f, facadeZ = 7.00f, chimneyOffset = Vector3.zero, hasChimney = false } },
            { "Building_Tavern_Open", new HouseDimensions { frontage = 13.8f, depth = 16.4f, height = 13.3f, facadeZ = 8.40f, chimneyOffset = new Vector3(-2.40f, 13.30f, 0.00f), hasChimney = true } },
            { "Building_Guildhall_Grand", new HouseDimensions { frontage = 17.6f, depth = 22.8f, height = 15.5f, facadeZ = 11.40f, chimneyOffset = new Vector3(3.50f, 15.50f, 2.00f), hasChimney = true } },
            { "Building_WizardObservatory", new HouseDimensions { frontage = 13.5f, depth = 13.5f, height = 18.0f, facadeZ = 6.75f, chimneyOffset = Vector3.zero, hasChimney = false } }
        };

        [MenuItem("Antigravity/Metropolis/Personalize Each House (Make Lived In)")]
        public static void PersonalizeAllHouses()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Debug.Log("<color=#FFD54F><b>[HOUSE PERSONALIZATION] Commencing comprehensive lived-in enrichment of metropolis houses...</b></color>");

            var rowHousesRoot = GameObject.Find(ROW_HOUSES_PATH);
            if (rowHousesRoot == null)
            {
                Debug.LogError($"[HOUSE PERSONALIZATION] Could not find root '{ROW_HOUSES_PATH}' in active scene!");
                return;
            }

            int totalHouses = rowHousesRoot.transform.childCount;
            Debug.Log($"[HOUSE PERSONALIZATION] Found {totalHouses} houses to personalize.");

            // 1. Load Materials
            var timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_TimberWood.mat");
            var stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            var ironMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_ForgeIron.mat");
            var lanternGlowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_LanternGlow.mat");
            var brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_ClockworkBrass.mat");

            // Roof Materials Palette
            var roofMaterials = new Material[]
            {
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_RoofSlate.mat"),      // Royal Blue
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_AlchemistClayRoof.mat"),    // Terracotta Clay
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_BakeryMossyRoof.mat"),      // Moss Green
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_HouseMossySlate.mat"),      // Forest Sage
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_Roof_ArcanePlum.mat"), // Arcane Plum
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_Roof_WarmOchre.mat")  // Warm Ochre
            };

            // 2. Load Prefabs
            var lantern1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/LanternV1.prefab");
            var lantern2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/LanternV2.prefab");
            var wood1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/PileOfWoodV1.prefab");
            var wood2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/PileOfWoodV2.prefab");
            var barrel1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BeerBarrelV1.prefab");
            var barrel2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BenchV1.prefab");
            var sackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/SackV1.prefab");
            var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BoxV1.prefab");
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            var ladderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Folding_Ladder.prefab");
            var cartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var potPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Clay Pot.prefab");
            var vasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Vase_1.prefab");
            var owlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Critter_PerchedOwl.fbx");
            var paperLanternsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_PaperLanterns_String.fbx");

            var flowerPrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab")
            };

            // Shared simple meshes for door panels, steps, and flower boxes to optimize memory
            var sharedDoorMesh = CreateBoxMesh(1.4f, 2.5f, 0.12f, new Vector3(0f, 1.25f, 0f));
            var sharedStepMesh = CreateBoxMesh(2.0f, 0.16f, 0.8f, new Vector3(0f, 0.08f, 0f));
            var sharedFlowerBoxMesh = CreateBoxMesh(1.4f, 0.25f, 0.35f, new Vector3(0f, 0.125f, 0f));
            var sharedWindowGlowMesh = CreateBoxMesh(1.2f, 1.4f, 0.05f, Vector3.zero);

            System.Random rng = new System.Random(5072026);
            int personalizedCount = 0;
            int smokeCount = 0;
            int flowerCount = 0;
            int lanternCount = 0;
            int firewoodCount = 0;
            int barrelCount = 0;
            int benchCount = 0;

            try
            {
                for (int i = 0; i < totalHouses; i++)
                {
                    var house = rowHousesRoot.transform.GetChild(i);
                    if (house == null) continue;

                    if (i % 100 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Personalizing Metropolis Houses", $"Decorating house {i + 1} of {totalHouses}...", (float)i / totalHouses);
                    }

                    // 1. Identify Model Specification
                    string matchedKey = null;
                    foreach (var key in ModelSpecs.Keys)
                    {
                        if (house.name.Contains(key))
                        {
                            matchedKey = key;
                            break;
                        }
                    }

                    HouseDimensions dims = matchedKey != null ? ModelSpecs[matchedKey] : new HouseDimensions
                    {
                        frontage = 9.5f,
                        depth = 11.5f,
                        height = 10.0f,
                        facadeZ = 5.75f,
                        chimneyOffset = new Vector3(-2.0f, 10.0f, -1.0f),
                        hasChimney = true
                    };

                    // 2. Remove any old lived-in container to allow clean re-runs
                    var oldDecor = house.Find(LIVED_IN_CONTAINER_NAME);
                    if (oldDecor != null)
                    {
                        UnityEngine.Object.DestroyImmediate(oldDecor.gameObject);
                    }

                    var decorRoot = new GameObject(LIVED_IN_CONTAINER_NAME);
                    decorRoot.transform.SetParent(house, false);
                    float pScaleX = house.lossyScale.x;
                    if (pScaleX > 0.001f)
                    {
                        float inv = 1f / pScaleX;
                        decorRoot.transform.localScale = new Vector3(inv, inv, inv);
                    }

                    // 3. Assign Harmonious Roof Material (Neighborhood Clusters)
                    // Group houses into harmonic clusters of ~4-7 buildings using angle
                    float houseAngleDeg = Mathf.Atan2(house.position.x, house.position.z) * Mathf.Rad2Deg;
                    if (houseAngleDeg < 0) houseAngleDeg += 360f;
                    int clusterIndex = Mathf.FloorToInt(houseAngleDeg / 8.0f) % roofMaterials.Length;
                    // Add slight local variation (1 in 5 houses has an accent roof)
                    if (rng.NextDouble() < 0.20)
                    {
                        clusterIndex = (clusterIndex + rng.Next(1, roofMaterials.Length)) % roofMaterials.Length;
                    }
                    Material chosenRoofMat = roofMaterials[clusterIndex] ?? roofMaterials[0];
                    ApplyRoofMaterial(house.gameObject, chosenRoofMat);

                    // 4. Front Doorway System
                    // Wooden door panel with iron handle
                    bool doorAjar = (rng.NextDouble() < 0.15); // 15% of doors slightly open
                    float doorAngle = doorAjar ? (float)(rng.NextDouble() * 20.0 + 10.0) : 0f;

                    var doorGo = new GameObject("Front_Door");
                    doorGo.transform.SetParent(decorRoot.transform, false);
                    doorGo.transform.localPosition = new Vector3(0f, 0f, dims.facadeZ - 0.05f);
                    doorGo.transform.localRotation = Quaternion.Euler(0f, doorAngle, 0f);

                    var doorMf = doorGo.AddComponent<MeshFilter>();
                    doorMf.sharedMesh = sharedDoorMesh;
                    var doorMr = doorGo.AddComponent<MeshRenderer>();
                    doorMr.sharedMaterial = timberMat;

                    // Iron Ring/Pull Handle
                    var handleGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    handleGo.name = "Door_Handle";
                    handleGo.transform.SetParent(doorGo.transform, false);
                    handleGo.transform.localPosition = new Vector3(0.42f, 1.25f, 0.08f);
                    handleGo.transform.localScale = new Vector3(0.07f, 0.09f, 0.07f);
                    handleGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    UnityEngine.Object.DestroyImmediate(handleGo.GetComponent<Collider>());
                    handleGo.GetComponent<MeshRenderer>().sharedMaterial = ironMat;

                    // Stone Doorstep Flagstone
                    var stepGo = new GameObject("Doorstep_Stone");
                    stepGo.transform.SetParent(decorRoot.transform, false);
                    stepGo.transform.localPosition = new Vector3(0f, 0f, dims.facadeZ + 0.35f);
                    var stepMf = stepGo.AddComponent<MeshFilter>();
                    stepMf.sharedMesh = sharedStepMesh;
                    var stepMr = stepGo.AddComponent<MeshRenderer>();
                    stepMr.sharedMaterial = stoneMat;

                    // 5. Warm Interior Window Glow with Dark Timber Frame
                    // Add warm window glow behind front windows with authentic timber casing
                    void CreateWindow(string name, float posX)
                    {
                        var windowRoot = new GameObject(name);
                        windowRoot.transform.SetParent(decorRoot.transform, false);
                        windowRoot.transform.localPosition = new Vector3(posX, 1.7f, dims.facadeZ - 0.02f);

                        // Window Glass Pane
                        var wgMf = windowRoot.AddComponent<MeshFilter>();
                        wgMf.sharedMesh = sharedWindowGlowMesh;
                        var wgMr = windowRoot.AddComponent<MeshRenderer>();
                        wgMr.sharedMaterial = lanternGlowMat;
                        windowRoot.transform.localScale = new Vector3(0.92f, 0.92f, 1.0f);

                        // Exterior Timber Window Casing / Frame
                        var frameGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        frameGo.name = "Window_Frame";
                        frameGo.transform.SetParent(windowRoot.transform, false);
                        frameGo.transform.localPosition = new Vector3(0f, 0f, -0.015f);
                        frameGo.transform.localScale = new Vector3(1.30f, 1.50f, 0.04f);
                        UnityEngine.Object.DestroyImmediate(frameGo.GetComponent<Collider>());
                        if (timberMat != null) frameGo.GetComponent<MeshRenderer>().sharedMaterial = timberMat;
                    }

                    CreateWindow("Window_Glow_Left", -dims.frontage * 0.28f);
                    CreateWindow("Window_Glow_Right", dims.frontage * 0.28f);

                    // 6. Doorstep Ground Lantern (70% of houses)
                    // Freestanding candle lantern set upright ON THE GROUND / doorstep, NOT hung from a wall
                    if (rng.NextDouble() < 0.70 && lantern1Prefab != null)
                    {
                        var lantern = (GameObject)PrefabUtility.InstantiatePrefab(lantern1Prefab, decorRoot.transform);
                        lantern.name = "Door_Sconce_Lantern";
                        float sideX = (rng.NextDouble() < 0.5) ? 0.95f : -0.95f;
                        // Set flush on ground/doorstep level (Y = 0.08f), sitting neatly on the stoop
                        lantern.transform.localPosition = new Vector3(sideX, 0.08f, dims.facadeZ + 0.35f);
                        lantern.transform.localRotation = Quaternion.identity;
                        lantern.transform.localScale = Vector3.one * 0.28f;

                        // Freestanding ground lantern: no wall bracket

                        // Add warm point light on 35% of street frontage houses to create nighttime brilliance
                        if (rng.NextDouble() < 0.35)
                        {
                            var lt = lantern.AddComponent<Light>();
                            lt.type = LightType.Point;
                            lt.range = 3.5f;
                            lt.intensity = 1.0f;
                            lt.color = new Color(1.0f, 0.82f, 0.45f);
                        }
                        lanternCount++;
                    }

                    // 7. Window Flower Boxes (55% of houses)
                    if (rng.NextDouble() < 0.55 && flowerPrefabs.Length > 0)
                    {
                        float flowerSideX = (rng.NextDouble() < 0.5) ? dims.frontage * 0.28f : -dims.frontage * 0.28f;
                        var flowerBox = new GameObject("Window_FlowerBox");
                        flowerBox.transform.SetParent(decorRoot.transform, false);
                        flowerBox.transform.localPosition = new Vector3(flowerSideX, 1.0f, dims.facadeZ + 0.15f);
                        var fbMf = flowerBox.AddComponent<MeshFilter>();
                        fbMf.sharedMesh = sharedFlowerBoxMesh;
                        var fbMr = flowerBox.AddComponent<MeshRenderer>();
                        fbMr.sharedMaterial = timberMat;

                        var flowerPrefab = flowerPrefabs[rng.Next(0, flowerPrefabs.Length)];
                        if (flowerPrefab != null)
                        {
                            var flowers = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab, flowerBox.transform);
                            flowers.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                            flowers.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
                        }
                        flowerCount++;
                    }

                    // 8. Firewood Stack on Side Wall (45% of houses)
                    if (rng.NextDouble() < 0.45)
                    {
                        var woodPrefab = (rng.NextDouble() < 0.5) ? wood1Prefab : wood2Prefab;
                        if (woodPrefab != null)
                        {
                            var wood = (GameObject)PrefabUtility.InstantiatePrefab(woodPrefab, decorRoot.transform);
                            wood.name = "Firewood_Stack";
                            float sideX = (rng.NextDouble() < 0.5) ? (dims.frontage * 0.51f) : -(dims.frontage * 0.51f);
                            float stackZ = dims.facadeZ - 2.5f;
                            wood.transform.localPosition = new Vector3(sideX, 0f, stackZ);
                            wood.transform.localRotation = Quaternion.Euler(0f, sideX > 0 ? 90f : -90f, 0f);
                            wood.transform.localScale = Vector3.one * 0.70f;
                            firewoodCount++;
                        }
                    }

                    // 9. Rainwater / Ale Barrel or Storage Crates (50% of houses)
                    if (rng.NextDouble() < 0.50 && barrel2Prefab != null)
                    {
                        var barrel = (GameObject)PrefabUtility.InstantiatePrefab(barrel2Prefab, decorRoot.transform);
                        barrel.name = "Eave_Water_Barrel";
                        float cornerX = (rng.NextDouble() < 0.5) ? (dims.frontage * 0.46f) : -(dims.frontage * 0.46f);
                        barrel.transform.localPosition = new Vector3(cornerX, 0f, dims.facadeZ - 0.35f);
                        barrel.transform.localScale = Vector3.one * 0.85f;
                        barrelCount++;
                    }
                    else if (rng.NextDouble() < 0.40)
                    {
                        // Pantry Storage Crates or Flour Sacks
                        var crateAsset = (rng.NextDouble() < 0.5) ? cratePrefab : sackPrefab;
                        if (crateAsset != null)
                        {
                            var crate = (GameObject)PrefabUtility.InstantiatePrefab(crateAsset, decorRoot.transform);
                            crate.name = "Porch_Pantry_Supply";
                            float sideX = (rng.NextDouble() < 0.5) ? 1.6f : -1.6f;
                            crate.transform.localPosition = new Vector3(sideX, 0f, dims.facadeZ + 0.35f);
                            if (crateAsset == sackPrefab)
                            {
                                crate.transform.localRotation = Quaternion.Euler(270f, 180f, 0f);
                                crate.transform.localScale = Vector3.one * 0.22f;
                            }
                            else
                            {
                                crate.transform.localRotation = Quaternion.identity;
                                crate.transform.localScale = Vector3.one * 0.8f;
                            }
                        }
                    }

                    // 10. Front Stoop Seating / Artisan Gear (25% of houses)
                    if (rng.NextDouble() < 0.25)
                    {
                        if (benchPrefab != null && dims.frontage >= 9.0f)
                        {
                            var bench = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, decorRoot.transform);
                            bench.name = "Front_Porch_Bench";
                            float benchSideX = (rng.NextDouble() < 0.5) ? 2.4f : -2.4f;
                            // Bench center offset: Euler(0, -90, 90) centers bench at +1.04m from pivot
                            bench.transform.localPosition = new Vector3(benchSideX - 1.04f, 0f, dims.facadeZ + 0.35f);
                            bench.transform.localRotation = Quaternion.Euler(0f, -90f, 90f);
                            bench.transform.localScale = Vector3.one * 0.16f;
                            benchCount++;
                        }
                        else if (ladderPrefab != null && rng.NextDouble() < 0.4)
                        {
                            var ladder = (GameObject)PrefabUtility.InstantiatePrefab(ladderPrefab, decorRoot.transform);
                            ladder.name = "Tradesman_Ladder";
                            float sideX = (dims.frontage * 0.51f);
                            ladder.transform.localPosition = new Vector3(sideX, 0f, 1.0f);
                            ladder.transform.localRotation = Quaternion.Euler(15f, 90f, 0f);
                            ladder.transform.localScale = Vector3.one * 0.8f;
                        }
                    }

                    // 11. Chimney Hearth Smoke VFX (35% of houses with chimneys)
                    if (dims.hasChimney && rng.NextDouble() < 0.35)
                    {
                        CreateChimneySmokeVFX(decorRoot.transform, dims.chimneyOffset);
                        smokeCount++;
                    }

                    // 12. Trade Signs for Specialized District Buildings
                    if (matchedKey != null && !matchedKey.Contains("Residential") && !matchedKey.Contains("LowDistrict"))
                    {
                        CreateHangingShopSign(decorRoot.transform, dims.facadeZ, matchedKey, timberMat, brassMat);
                    }

                    // 13. Rooftop Perched Owl (1 in 40 houses)
                    if (owlPrefab != null && dims.hasChimney && rng.NextDouble() < 0.025)
                    {
                        var owl = (GameObject)PrefabUtility.InstantiatePrefab(owlPrefab, decorRoot.transform);
                        owl.name = "Rooftop_Perched_Owl";
                        owl.transform.localPosition = dims.chimneyOffset + new Vector3(0.35f, 0.2f, 0f);
                        owl.transform.localScale = Vector3.one * 0.65f;
                    }

                    // 14. Festive Paper Lantern Strings between Neighbors (1 in 30 houses)
                    if (paperLanternsPrefab != null && rng.NextDouble() < 0.035)
                    {
                        var paperLanterns = (GameObject)PrefabUtility.InstantiatePrefab(paperLanternsPrefab, decorRoot.transform);
                        paperLanterns.name = "Festive_Paper_Lanterns_String";
                        paperLanterns.transform.localPosition = new Vector3(0f, 4.5f, dims.facadeZ + 1.2f);
                        paperLanterns.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        paperLanterns.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                    }

                    personalizedCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"<color=#76FF03><b>[HOUSE PERSONALIZATION COMPLETE] Successfully personalized all {personalizedCount} houses across the metropolis!</b></color>\n" +
                      $"  • Chimneys with Active Hearth Smoke: {smokeCount}\n" +
                      $"  • Window Flower Planters: {flowerCount}\n" +
                      $"  • Warm Doorway Wall Sconces: {lanternCount}\n" +
                      $"  • Side Wall Firewood Stacks: {firewoodCount}\n" +
                      $"  • Eave Water/Ale Barrels: {barrelCount}\n" +
                      $"  • Porch Benches: {benchCount}\n" +
                      $"  • 6 Distinct Roof Tile Neighborhood Colorways Applied!");
        }

        private static void ApplyRoofMaterial(GameObject house, Material roofMat)
        {
            if (house == null || roofMat == null) return;
            string hName = house.name;

            var mr = house.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mats = mr.sharedMaterials;
                int roofSlot = -1;
                if (hName.Contains("Building_House_Residential") && mats.Length >= 5) roofSlot = 4;
                else if (hName.Contains("Building_House_LowDistrict") && mats.Length >= 4) roofSlot = 2;
                else if (hName.Contains("Building_Tavern_Open") && mats.Length >= 5) roofSlot = 4;
                else if (hName.Contains("Building_Bakery_Shop") && mats.Length >= 6) roofSlot = 5;
                else if ((hName.Contains("Building_Inn_Lodge") || hName.Contains("Terrace_")) && mats.Length >= 4) roofSlot = 3;
                else if (hName.Contains("Building_Guildhall_Grand") && mats.Length >= 4) roofSlot = 3;
                else if (hName.Contains("Building_Blacksmith_OpenForge") && mats.Length >= 5) roofSlot = 4;
                else if (hName.Contains("Building_EnchanterWorkshop") && mats.Length >= 4) roofSlot = 2;

                if (roofSlot >= 0 && roofSlot < mats.Length)
                {
                    mats[roofSlot] = roofMat;
                    mr.sharedMaterials = mats;
                    return;
                }
            }

            // Fallback for multi-part or nested objects
            foreach (var childMr in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (childMr.transform.parent != null && childMr.transform.parent.name == LIVED_IN_CONTAINER_NAME) continue;
                if (childMr.name.Contains("Roof") || childMr.name.Contains("Dome"))
                {
                    childMr.sharedMaterial = roofMat;
                }
            }
        }

        private static void CreateChimneySmokeVFX(Transform parent, Vector3 localChimneyTop)
        {
            var smokeGo = new GameObject("Chimney_Hearth_Smoke");
            smokeGo.transform.SetParent(parent, false);
            smokeGo.transform.localPosition = localChimneyTop + new Vector3(0f, 0.2f, 0f);

            var ps = smokeGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 4.2f;
            main.startSpeed = 0.75f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.5f);
            main.startColor = new Color(0.88f, 0.88f, 0.92f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 10;

            var emission = ps.emission;
            emission.rateOverTime = 2.2f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.20f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = 0.25f; // Gentle atmospheric drift
            vel.y = 0.55f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0.25f);
            curve.AddKey(1f, 1.8f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.9f, 0.95f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.40f, 0.25f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;
        }

        private static void CreateHangingShopSign(Transform parent, float facadeZ, string buildingType, Material timberMat, Material brassMat)
        {
            var signRoot = new GameObject("Hanging_Trade_Sign");
            signRoot.transform.SetParent(parent, false);
            signRoot.transform.localPosition = new Vector3(0.95f, 3.1f, facadeZ + 0.1f);

            // Iron/Brass Arm Bracket
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Sign_Bracket";
            arm.transform.SetParent(signRoot.transform, false);
            arm.transform.localPosition = new Vector3(0f, 0f, 0.45f);
            arm.transform.localScale = new Vector3(0.06f, 0.06f, 0.9f);
            UnityEngine.Object.DestroyImmediate(arm.GetComponent<Collider>());
            arm.GetComponent<MeshRenderer>().sharedMaterial = brassMat;

            // Wooden Signboard Plaque
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Sign_Plaque";
            board.transform.SetParent(signRoot.transform, false);
            board.transform.localPosition = new Vector3(0f, -0.42f, 0.70f);
            board.transform.localScale = new Vector3(0.05f, 0.65f, 0.85f);
            UnityEngine.Object.DestroyImmediate(board.GetComponent<Collider>());
            board.GetComponent<MeshRenderer>().sharedMaterial = timberMat;
        }

        private static Mesh CreateBoxMesh(float width, float height, float depth, Vector3 centerOffset)
        {
            var mesh = new Mesh();
            mesh.name = $"ProceduralBox_{width}x{height}x{depth}";

            float hx = width * 0.5f;
            float hy = height * 0.5f;
            float hz = depth * 0.5f;

            Vector3 c = centerOffset;

            Vector3[] verts = new Vector3[]
            {
                // Front
                c + new Vector3(-hx, -hy,  hz), c + new Vector3( hx, -hy,  hz), c + new Vector3( hx,  hy,  hz), c + new Vector3(-hx,  hy,  hz),
                // Back
                c + new Vector3( hx, -hy, -hz), c + new Vector3(-hx, -hy, -hz), c + new Vector3(-hx,  hy, -hz), c + new Vector3( hx,  hy, -hz),
                // Top
                c + new Vector3(-hx,  hy,  hz), c + new Vector3( hx,  hy,  hz), c + new Vector3( hx,  hy, -hz), c + new Vector3(-hx,  hy, -hz),
                // Bottom
                c + new Vector3(-hx, -hy, -hz), c + new Vector3( hx, -hy, -hz), c + new Vector3( hx, -hy,  hz), c + new Vector3(-hx, -hy,  hz),
                // Left
                c + new Vector3(-hx, -hy, -hz), c + new Vector3(-hx, -hy,  hz), c + new Vector3(-hx,  hy,  hz), c + new Vector3(-hx,  hy, -hz),
                // Right
                c + new Vector3( hx, -hy,  hz), c + new Vector3( hx, -hy, -hz), c + new Vector3( hx,  hy, -hz), c + new Vector3( hx,  hy,  hz)
            };

            int[] tris = new int[]
            {
                0, 2, 1, 0, 3, 2,       // Front
                4, 6, 5, 4, 7, 6,       // Back
                8, 10, 9, 8, 11, 10,    // Top
                12, 14, 13, 12, 15, 14, // Bottom
                16, 18, 17, 16, 19, 18, // Left
                20, 22, 21, 20, 23, 22  // Right
            };

            Vector2[] uvs = new Vector2[24];
            for (int i = 0; i < 6; i++)
            {
                uvs[i * 4 + 0] = new Vector2(0f, 0f);
                uvs[i * 4 + 1] = new Vector2(1f, 0f);
                uvs[i * 4 + 2] = new Vector2(1f, 1f);
                uvs[i * 4 + 3] = new Vector2(0f, 1f);
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}

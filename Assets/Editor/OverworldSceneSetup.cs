using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using BattleSummoner.Quests;

/// <summary>
/// OverworldSceneSetup procedurally constructs:
/// 1. OverworldMap.unity: A rich open world featuring Solas Town (homes, buildings, tavern, forge, apothecary),
///    Mistwood Forest (trees, winding paths, shrine), and The Abyssal Spire Dungeons entrance (ruins, glowing portal),
///    plus roaming NPCs and full mobile portrait ARPG controls.
/// 2. BuildingInterior.unity: The warm, atmospheric interior of The Wandering Wyvern Tavern with stone fireplace,
///    service bar counter, dining tables, patrons, and exit doorway.
/// 3. ShopInterior.unity: The artisanal workshop interior of Ironheart Forge & Alchemy Apothecary with anvil,
///    furnace, potion brewing stand, shopkeeper NPCs, and exit doorway.
/// 4. Wires all doorway triggers with BuildingEntranceTrigger and SceneEntranceMarker to enable seamless
///    screen fade-to-black scene loading transitions between exterior and interior.
/// 5. Automatically registers all scenes in EditorBuildSettings.scenes.
/// </summary>
public static class OverworldSceneSetup
{
    private const string MAT_DIR = "Assets/Materials/World";

    [MenuItem("Tools/BattleSummoner/Generate Overworld & Interior Scenes")]
    public static void GenerateAllScenes()
    {
        EnsureDirectory(MAT_DIR);
        EnsureDirectory("Assets/Scenes");

        var materials = SetupWorldMaterials();

        // 1. Build OverworldMap.unity
        BuildOverworldScene(materials);

        // 2. Build BuildingInterior.unity (Tavern & Inn)
        BuildTavernInteriorScene(materials);

        // 3. Build ShopInterior.unity (Forge & Apothecary)
        BuildShopInteriorScene(materials);

        // 4. Build GuildHallInterior.unity (Adventurer's Guild & Task Board)
        BuildGuildHallInteriorScene(materials);

        // 5. Register all scenes in EditorBuildSettings
        RegisterScenesInBuildSettings();

        Debug.Log("✨ [OVERWORLD SETUP] Successfully generated OverworldMap, BuildingInterior, ShopInterior, and GuildHallInterior scenes with doorway fade transitions!");
    }

    #region Material Generation
    private class WorldMaterials
    {
        public Material cobblestone;
        public Material townWall;
        public Material roofTile;
        public Material roofSlate;
        public Material woodPlank;
        public Material woodBeam;
        public Material forestGrass;
        public Material forestDirt;
        public Material pineNeedles;
        public Material woodBark;
        public Material dungeonStone;
        public Material portalEnergy;
        public Material doorWood;
        public Material windowGlass;
        public Material hearthFire;
        public Material fountainWater;
        public Material lanternGlow;
        public Material goldTrim;
        public Material guildRed;
        public Material corkboard;
        public Material parchment;
    }

    private static WorldMaterials SetupWorldMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        WorldMaterials m = new WorldMaterials();
        m.cobblestone = GetOrCreateMat("Mat_World_Cobblestone", litShader, new Color(0.48f, 0.46f, 0.44f), 0.1f, 0.6f);
        m.townWall = GetOrCreateMat("Mat_World_TownWall", litShader, new Color(0.86f, 0.82f, 0.74f), 0.05f, 0.8f);
        m.roofTile = GetOrCreateMat("Mat_World_RoofTile", litShader, new Color(0.72f, 0.28f, 0.22f), 0.1f, 0.7f);
        m.roofSlate = GetOrCreateMat("Mat_World_RoofSlate", litShader, new Color(0.24f, 0.28f, 0.36f), 0.15f, 0.5f);
        m.woodPlank = GetOrCreateMat("Mat_World_WoodPlank", litShader, new Color(0.55f, 0.38f, 0.22f), 0.1f, 0.5f);
        m.woodBeam = GetOrCreateMat("Mat_World_WoodBeam", litShader, new Color(0.32f, 0.20f, 0.12f), 0.05f, 0.7f);
        m.forestGrass = GetOrCreateMat("Mat_World_ForestGrass", litShader, new Color(0.24f, 0.48f, 0.26f), 0.05f, 0.85f);
        m.forestDirt = GetOrCreateMat("Mat_World_ForestDirt", litShader, new Color(0.52f, 0.40f, 0.28f), 0.05f, 0.9f);
        m.pineNeedles = GetOrCreateMat("Mat_World_PineNeedles", litShader, new Color(0.12f, 0.35f, 0.18f), 0.05f, 0.8f);
        m.woodBark = GetOrCreateMat("Mat_World_WoodBark", litShader, new Color(0.38f, 0.26f, 0.16f), 0.05f, 0.85f);
        m.dungeonStone = GetOrCreateMat("Mat_World_DungeonStone", litShader, new Color(0.16f, 0.15f, 0.20f), 0.2f, 0.4f);
        m.portalEnergy = GetOrCreateMat("Mat_World_PortalEnergy", litShader, new Color(0.65f, 0.25f, 1.0f), 0.5f, 0.2f, new Color(0.75f, 0.35f, 1.0f) * 2.5f);
        m.doorWood = GetOrCreateMat("Mat_World_DoorWood", litShader, new Color(0.42f, 0.25f, 0.14f), 0.1f, 0.6f);
        m.windowGlass = GetOrCreateMat("Mat_World_WindowGlass", litShader, new Color(1.0f, 0.85f, 0.45f), 0.2f, 0.3f, new Color(1.0f, 0.80f, 0.35f) * 1.5f);
        m.hearthFire = GetOrCreateMat("Mat_World_HearthFire", litShader, new Color(1.0f, 0.50f, 0.10f), 0.1f, 0.2f, new Color(1.0f, 0.45f, 0.08f) * 3.5f);
        m.fountainWater = GetOrCreateMat("Mat_World_FountainWater", litShader, new Color(0.20f, 0.65f, 0.85f, 0.85f), 0.8f, 0.1f);
        m.lanternGlow = GetOrCreateMat("Mat_World_LanternGlow", litShader, new Color(1.0f, 0.88f, 0.45f), 0.1f, 0.2f, new Color(1.0f, 0.85f, 0.40f) * 2.5f);
        m.goldTrim = GetOrCreateMat("Mat_World_GoldTrim", litShader, new Color(0.92f, 0.78f, 0.32f), 0.7f, 0.3f);
        m.guildRed = GetOrCreateMat("Mat_World_GuildRed", litShader, new Color(0.72f, 0.15f, 0.18f), 0.1f, 0.4f);
        m.corkboard = GetOrCreateMat("Mat_World_Corkboard", litShader, new Color(0.68f, 0.52f, 0.35f), 0.05f, 0.85f);
        m.parchment = GetOrCreateMat("Mat_World_Parchment", litShader, new Color(0.96f, 0.92f, 0.82f), 0.05f, 0.8f);
        return m;
    }

    private static Material GetOrCreateMat(string matName, Shader shader, Color baseColor, float metallic = 0f, float smoothness = 0.5f, Color? emission = null)
    {
        string path = $"{MAT_DIR}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }
    #endregion

    #region Scene 1: OverworldMap.unity
    private static void BuildOverworldScene(WorldMaterials m)
    {
        string scenePath = "Assets/Scenes/OverworldMap.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        GameObject sun = new GameObject("Sun_DirectionalLight", typeof(Light));
        Light sunLight = sun.GetComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.95f, 0.85f);
        sunLight.intensity = 1.25f;
        sunLight.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.48f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        // 2. Scene Transition Manager (Persistent Singleton)
        GameObject stmObj = new GameObject("SceneTransitionManager", typeof(SceneTransitionManager));

        // 3. Ground Terrain Zones
        GameObject terrainRoot = new GameObject("OverworldTerrain");

        // Town Cobblestone Plaza (Center/West: X in [-38, 12], Z in [-38, 12])
        GameObject townFloor = CreateBox(terrainRoot.transform, "TownCobblestonePlaza", new Vector3(-13f, -0.1f, -13f), new Vector3(54f, 0.2f, 54f), m.cobblestone);

        // Forest Grass (East: X in [12, 58], Z in [-38, 38])
        GameObject forestFloor = CreateBox(terrainRoot.transform, "ForestGrassZone", new Vector3(35f, -0.1f, 0f), new Vector3(48f, 0.2f, 76f), m.forestGrass);

        // Spire Dungeon Plateau (North: X in [-38, 12], Z in [12, 54])
        GameObject spireFloor = CreateBox(terrainRoot.transform, "SpireDungeonPlateau", new Vector3(-13f, -0.1f, 33f), new Vector3(54f, 0.2f, 44f), m.dungeonStone);

        // Connecting Dirt Paths
        CreateBox(terrainRoot.transform, "Path_TownToForest", new Vector3(12f, -0.05f, -14f), new Vector3(18f, 0.2f, 8f), m.forestDirt);
        CreateBox(terrainRoot.transform, "Path_TownToSpire", new Vector3(-13f, -0.05f, 13f), new Vector3(8f, 0.2f, 16f), m.forestDirt);
        CreateBox(terrainRoot.transform, "Path_ForestDeepTrail", new Vector3(35f, -0.05f, 8f), new Vector3(8f, 0.2f, 40f), m.forestDirt);

        // 4. Solas Town Architecture & Buildings
        GameObject townRoot = new GameObject("SolasTownHub");

        // Central Fountain in Town Square
        CreateTownFountain(townRoot.transform, new Vector3(-13f, 0f, -14f), m);

        // Street Lanterns
        CreateStreetLantern(townRoot.transform, new Vector3(-7f, 0f, -14f), m);
        CreateStreetLantern(townRoot.transform, new Vector3(-19f, 0f, -14f), m);
        CreateStreetLantern(townRoot.transform, new Vector3(-13f, 0f, -8f), m);
        CreateStreetLantern(townRoot.transform, new Vector3(-13f, 0f, -20f), m);

        // Market Stalls & Crates
        CreateMarketStall(townRoot.transform, new Vector3(-8f, 0f, -10f), new Color(0.9f, 0.3f, 0.25f), m);
        CreateMarketStall(townRoot.transform, new Vector3(-18f, 0f, -18f), new Color(0.2f, 0.6f, 0.9f), m);
        CreateCrateStack(townRoot.transform, new Vector3(-7f, 0f, -7f), m);
        CreateCrateStack(townRoot.transform, new Vector3(-20f, 0f, -10f), m);

        // BUILDING 1: The Wandering Wyvern Tavern (Large 2-story tavern)
        CreateBuilding(townRoot.transform, "Tavern_TheWanderingWyvern",
            new Vector3(-24f, 0f, -5f), new Vector3(14f, 7f, 9f),
            m.townWall, m.roofSlate, m.woodBeam, m.doorWood, m.windowGlass,
            targetSceneName: "BuildingInterior",
            targetSpawnMarkerId: "Tavern_Interior_Doorway",
            locationTitle: "The Wandering Wyvern Tavern",
            outsideSpawnMarkerId: "Overworld_TavernDoor",
            doorLocalPos: new Vector3(0f, 0f, 4.5f)
        );

        // BUILDING 2: Ironheart Forge & Blacksmith Workshop
        CreateBuilding(townRoot.transform, "Shop_IronheartForge",
            new Vector3(-2f, 0f, -25f), new Vector3(10f, 5.5f, 8f),
            m.cobblestone, m.roofTile, m.woodBeam, m.doorWood, m.windowGlass,
            targetSceneName: "ShopInterior",
            targetSpawnMarkerId: "Shop_Interior_Doorway",
            locationTitle: "Ironheart Forge & Blacksmith",
            outsideSpawnMarkerId: "Overworld_ForgeDoor",
            doorLocalPos: new Vector3(0f, 0f, 4f)
        );
        // Anvil outside forge
        CreateBox(townRoot.transform, "ForgeAnvilOutside", new Vector3(-2f, 0.45f, -19.5f), new Vector3(0.9f, 0.9f, 0.6f), m.woodBeam);

        // BUILDING 3: Starfall Alchemy Apothecary
        CreateBuilding(townRoot.transform, "Shop_StarfallApothecary",
            new Vector3(-24f, 0f, -25f), new Vector3(10f, 5.5f, 8f),
            m.townWall, m.roofTile, m.woodBeam, m.doorWood, m.windowGlass,
            targetSceneName: "BuildingInterior",
            targetSpawnMarkerId: "Tavern_Interior_Doorway",
            locationTitle: "Starfall Alchemy Apothecary",
            outsideSpawnMarkerId: "Overworld_ApothecaryDoor",
            doorLocalPos: new Vector3(0f, 0f, 4f)
        );

        // BUILDING 4: Solas Adventurer's Guild Hall & Task Board Headquarters
        CreateBuilding(townRoot.transform, "GuildHall_AdventurersGuild",
            new Vector3(2f, 0f, -5f), new Vector3(13f, 8.5f, 11f),
            m.townWall, m.roofSlate, m.goldTrim, m.doorWood, m.windowGlass,
            targetSceneName: "GuildHallInterior",
            targetSpawnMarkerId: "GuildHall_Interior_Doorway",
            locationTitle: "Solas Adventurer's Guild Hall",
            outsideSpawnMarkerId: "Overworld_GuildHallDoor",
            doorLocalPos: new Vector3(0f, 0f, 5.5f)
        );
        // Guild Hall Grand Crest Sign above entrance
        CreateBox(townRoot.transform, "GuildSign_Crest", new Vector3(2f, 4.4f, 0.7f), new Vector3(3.6f, 1.0f, 0.3f), m.guildRed);
        CreateBox(townRoot.transform, "GuildSign_Trim", new Vector3(2f, 4.4f, 0.72f), new Vector3(3.8f, 1.15f, 0.15f), m.goldTrim);

        // BUILDING 5: Cozy Residential Cottage
        CreateBuilding(townRoot.transform, "Cottage_MeadowHome",
            new Vector3(-2f, 0f, 5f), new Vector3(8f, 5f, 7f),
            m.townWall, m.roofTile, m.woodBeam, m.doorWood, m.windowGlass,
            targetSceneName: "BuildingInterior",
            targetSpawnMarkerId: "Tavern_Interior_Doorway",
            locationTitle: "Meadow Home Cottage",
            outsideSpawnMarkerId: "Overworld_CottageDoor",
            doorLocalPos: new Vector3(0f, 0f, 3.5f)
        );

        // Town Perimeter Wall Segments & Archway
        CreateBox(townRoot.transform, "TownWall_South", new Vector3(-13f, 2f, -37f), new Vector3(50f, 4f, 2f), m.cobblestone);
        CreateBox(townRoot.transform, "TownWall_West", new Vector3(-37f, 2f, -13f), new Vector3(2f, 4f, 50f), m.cobblestone);

        // 5. Mistwood Forest Zone (Trees, Rocks, Shrine)
        GameObject forestRoot = new GameObject("MistwoodForest");

        // Clustered Trees
        float[,] treeCoords = {
            { 16f, -28f }, { 22f, -32f }, { 30f, -26f }, { 25f, -18f }, { 34f, -15f },
            { 42f, -22f }, { 45f, -30f }, { 38f, -6f }, { 46f, -10f }, { 26f, -4f },
            { 48f, 2f }, { 40f, 10f }, { 46f, 18f }, { 34f, 22f }, { 44f, 28f },
            { 22f, 15f }, { 18f, 25f }, { 28f, 32f }, { 38f, 36f }, { 20f, -8f },
            { 18f, -2f }, { 16f, 6f }, { 24f, 8f }, { 32f, 4f }, { 36f, 16f }
        };

        for (int i = 0; i < treeCoords.GetLength(0); i++)
        {
            Vector3 pos = new Vector3(treeCoords[i, 0], 0f, treeCoords[i, 1]);
            float scale = 0.85f + (i % 5) * 0.12f;
            if (i % 2 == 0) CreatePineTree(forestRoot.transform, pos, scale, m);
            else CreateOakTree(forestRoot.transform, pos, scale, m);
        }

        // Boulders in Forest
        CreateRock(forestRoot.transform, new Vector3(20f, 0f, -20f), new Vector3(2.5f, 1.8f, 2.2f), m.dungeonStone);
        CreateRock(forestRoot.transform, new Vector3(38f, 0f, -12f), new Vector3(3.2f, 2.4f, 2.8f), m.dungeonStone);
        CreateRock(forestRoot.transform, new Vector3(30f, 0f, 18f), new Vector3(2.8f, 2.0f, 2.4f), m.dungeonStone);

        // Ancient Forest Shrine Altar
        CreateForestShrine(forestRoot.transform, new Vector3(42f, 0f, 4f), m);

        // 6. The Abyssal Spire Dungeon Entrance Zone
        GameObject dungeonRoot = new GameObject("SpireDungeonEntrance");

        // Gothic Archway Gate with Glowing Arcane Portal
        CreateSpireDungeonPortal(dungeonRoot.transform, new Vector3(-13f, 0f, 40f), m);

        // Broken Gothic Pillars & Monoliths
        CreateDungeonPillar(dungeonRoot.transform, new Vector3(-22f, 0f, 26f), 5f, m.dungeonStone);
        CreateDungeonPillar(dungeonRoot.transform, new Vector3(-4f, 0f, 26f), 4.2f, m.dungeonStone);
        CreateDungeonPillar(dungeonRoot.transform, new Vector3(-26f, 0f, 36f), 6.5f, m.dungeonStone);
        CreateDungeonPillar(dungeonRoot.transform, new Vector3(0f, 0f, 36f), 5.8f, m.dungeonStone);

        // 7. Overworld NPCs with In-World Nametags
        GameObject npcRoot = new GameObject("OverworldNPCs");
        CreateOverworldNPC(npcRoot.transform, "Mayor Barnaby", new Vector3(-10f, 0f, -14f), new Color(0.85f, 0.45f, 0.2f), "Mayor", "Welcome to Solas Town! Rest at the Wyvern or prepare for the Spire.", m);
        CreateOverworldNPC(npcRoot.transform, "Margo the Gothic Guide", new Vector3(-16f, 0f, -14f), new Color(0.2f, 0.2f, 0.35f), "Guide", "The hundredfold spire awaits... ensure your potions are stocked.", m);
        CreateOverworldNPC(npcRoot.transform, "Blacksmith Keith", new Vector3(-2f, 0f, -18f), new Color(0.6f, 0.3f, 0.2f), "Smith", "Need your blade honed? Visit my forge anytime.", m);
        CreateOverworldNPC(npcRoot.transform, "Alchemist Cheryl", new Vector3(-22f, 0f, -19f), new Color(0.2f, 0.7f, 0.8f), "Alchemist", "Potions brewed from rare Mistwood herbs.", m);
        CreateOverworldNPC(npcRoot.transform, "Ranger Zephyr", new Vector3(15f, 0f, -12f), new Color(0.2f, 0.6f, 0.3f), "Scout", "Watch the treeline. The forest grows denser ahead.", m);
        CreateOverworldNPC(npcRoot.transform, "Spire Warden Gregory", new Vector3(-13f, 0f, 32f), new Color(0.5f, 0.2f, 0.6f), "Warden", "None venture past the Spire Gate without absolute conviction.", m);

        // 8. Player Hero & Portrait Camera
        GameObject player = CreatePlayerHero(new Vector3(-13f, 0.9f, -17f), m);

        // ARPG Camera
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PortraitARPGCamera));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;

        PortraitARPGCamera arpgCam = camObj.GetComponent<PortraitARPGCamera>();
        arpgCam.playerTarget = player.transform;
        arpgCam.pitchAngle = 24f;
        arpgCam.cameraDistance = 3.2f;

        Vector3 focusPoint = player.transform.position + Vector3.up * (arpgCam.targetFocusHeight + 0.75f);
        Quaternion camRot = Quaternion.Euler(arpgCam.pitchAngle, arpgCam.yawAngle, 0f);
        camObj.transform.position = focusPoint - camRot * Vector3.forward * arpgCam.cameraDistance;
        camObj.transform.rotation = camRot;

        // 9. Mobile Overworld HUD (Virtual Stick & Minimap banner)
        CreateOverworldHUD(player);

        // Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"💾 [OVERWORLD SETUP] Saved '{scenePath}' successfully!");
    }
    #endregion

    #region Scene 2: BuildingInterior.unity (Tavern & Inn)
    private static void BuildTavernInteriorScene(WorldMaterials m)
    {
        string scenePath = "Assets/Scenes/BuildingInterior.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Warm Interior Lighting & Ambient
        GameObject sun = new GameObject("InteriorAmbientLight", typeof(Light));
        Light light = sun.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.88f, 0.75f);
        light.intensity = 0.5f;
        sun.transform.rotation = Quaternion.Euler(60f, 30f, 0f);

        RenderSettings.ambientLight = new Color(0.32f, 0.24f, 0.18f);

        // 2. Scene Transition Manager
        new GameObject("SceneTransitionManager", typeof(SceneTransitionManager));

        // 3. Room Shell: 18m x 14m x 5m warm wooden hall
        GameObject roomRoot = new GameObject("TavernInteriorHall");

        // Polished Wooden Plank Floor
        CreateBox(roomRoot.transform, "TavernFloor", new Vector3(0f, -0.1f, 0f), new Vector3(18f, 0.2f, 14f), m.woodPlank);

        // Timber Walls (South walls are low cutaway walls so isometric camera is unobstructed)
        CreateBox(roomRoot.transform, "Wall_North", new Vector3(0f, 2.5f, 7f), new Vector3(18f, 5f, 0.6f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_South_Left", new Vector3(-5.5f, 0.6f, -7f), new Vector3(7f, 1.2f, 0.6f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_South_Right", new Vector3(5.5f, 0.6f, -7f), new Vector3(7f, 1.2f, 0.6f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_East", new Vector3(9f, 2.5f, 0f), new Vector3(0.6f, 5f, 14f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_West", new Vector3(-9f, 2.5f, 0f), new Vector3(0.6f, 5f, 14f), m.townWall);

        // High Perimeter Beams & Roof Trusses
        CreateBox(roomRoot.transform, "PerimeterBeam_North", new Vector3(0f, 4.8f, 6.7f), new Vector3(18f, 0.4f, 0.4f), m.woodBeam);
        CreateBox(roomRoot.transform, "PerimeterBeam_East", new Vector3(8.7f, 4.8f, 0f), new Vector3(0.4f, 0.4f, 14f), m.woodBeam);
        CreateBox(roomRoot.transform, "PerimeterBeam_West", new Vector3(-8.7f, 4.8f, 0f), new Vector3(0.4f, 0.4f, 14f), m.woodBeam);
        CreateBox(roomRoot.transform, "CeilingBeam_HighNorth", new Vector3(0f, 4.8f, 4.0f), new Vector3(18f, 0.4f, 0.4f), m.woodBeam);

        // 4. Roaring Stone Fireplace Hearth on North Wall
        CreateStoneFireplace(roomRoot.transform, new Vector3(0f, 0f, 6.2f), m);

        // 5. Service Bar Counter (East Wall)
        GameObject barRoot = new GameObject("ServiceBarCounter");
        barRoot.transform.SetParent(roomRoot.transform, false);
        CreateBox(barRoot.transform, "CounterTop", new Vector3(5.5f, 0.55f, 0.5f), new Vector3(2f, 1.1f, 7f), m.woodPlank);
        CreateBox(barRoot.transform, "CounterLReturn", new Vector3(4f, 0.55f, -3f), new Vector3(3f, 1.1f, 1.5f), m.woodPlank);

        // Back-bar shelves with potion bottles & kegs
        CreateBox(barRoot.transform, "BackBarShelves", new Vector3(8.4f, 2.2f, 0.5f), new Vector3(0.6f, 3.5f, 7.5f), m.woodBeam);
        CreateBox(barRoot.transform, "Keg_1", new Vector3(8f, 1.5f, 2.2f), new Vector3(0.9f, 1.2f, 0.9f), m.woodPlank);
        CreateBox(barRoot.transform, "Keg_2", new Vector3(8f, 1.5f, -1.2f), new Vector3(0.9f, 1.2f, 0.9f), m.woodPlank);

        // Barstools
        for (int s = -2; s <= 3; s += 2)
        {
            CreateBox(barRoot.transform, $"Barstool_{s}", new Vector3(3.8f, 0.35f, s), new Vector3(0.6f, 0.7f, 0.6f), m.woodBeam);
        }

        // Innkeeper Barnaby behind counter
        CreateOverworldNPC(roomRoot.transform, "Innkeeper Barnaby", new Vector3(6.8f, 0f, 0.5f), new Color(0.85f, 0.45f, 0.2f), "Innkeeper", "Warm yourself by the fire! Room and stew are fresh.", m);

        // 6. Dining Tables & Chairs
        CreateDiningTable(roomRoot.transform, new Vector3(-4f, 0f, 2.5f), m);
        CreateDiningTable(roomRoot.transform, new Vector3(-4f, 0f, -2.5f), m);

        // Patrons at tables
        CreateOverworldNPC(roomRoot.transform, "Adventurer Lyra", new Vector3(-4f, 0f, 4f), new Color(0.2f, 0.5f, 0.85f), "Mercenary", "I heard deep tremors from the Spire last night...", m);
        CreateOverworldNPC(roomRoot.transform, "Scholar Dennis", new Vector3(-5.5f, 0f, -2.5f), new Color(0.6f, 0.3f, 0.7f), "Historian", "Ancient summoners used to seal runes inside relics.", m);

        // 7. South Exit Doorway & Trigger back to OverworldMap
        GameObject doorwayRoot = new GameObject("TavernExitDoorway");
        doorwayRoot.transform.SetParent(roomRoot.transform, false);
        doorwayRoot.transform.position = new Vector3(0f, 0f, -6.8f);

        // Wooden Door & Frame
        CreateBox(doorwayRoot.transform, "DoorFrame", new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 2.2f, 0.4f), m.woodBeam);
        CreateBox(doorwayRoot.transform, "DoorLeaf", new Vector3(0f, 0.95f, 0f), new Vector3(2.0f, 1.9f, 0.15f), m.doorWood);
        CreateBox(doorwayRoot.transform, "Doormat", new Vector3(0f, 0.02f, 0.6f), new Vector3(2.2f, 0.04f, 1.2f), m.woodBeam);

        // Doorway Trigger Component
        GameObject exitTrigger = new GameObject("ExitToOverworldTrigger", typeof(BoxCollider), typeof(BuildingEntranceTrigger));
        exitTrigger.transform.SetParent(doorwayRoot.transform, false);
        exitTrigger.transform.localPosition = new Vector3(0f, 1.2f, 0.4f);
        BoxCollider boxCol = exitTrigger.GetComponent<BoxCollider>();
        boxCol.size = new Vector3(2.2f, 2.6f, 1.5f);
        boxCol.isTrigger = true;

        BuildingEntranceTrigger trig = exitTrigger.GetComponent<BuildingEntranceTrigger>();
        trig.targetSceneName = "OverworldMap";
        trig.targetSpawnMarkerId = "Overworld_TavernDoor";
        trig.locationTitle = "Solas Town Square";
        trig.triggerOnTouch = true;

        // Spawn Marker inside the tavern (where player appears after entering from Overworld)
        GameObject spawnMarker = new GameObject("Tavern_Interior_Doorway", typeof(SceneEntranceMarker));
        spawnMarker.transform.position = new Vector3(0f, 0.9f, -5.2f);
        SceneEntranceMarker markerComp = spawnMarker.GetComponent<SceneEntranceMarker>();
        markerComp.markerId = "Tavern_Interior_Doorway";
        markerComp.facingYaw = 0f; // Facing into the room (North)

        // 8. Player Hero & Camera
        GameObject player = CreatePlayerHero(new Vector3(0f, 0.9f, -5.2f), m);

        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PortraitARPGCamera));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0.12f, 0.08f, 0.06f);
        cam.fieldOfView = 68f;

        PortraitARPGCamera arpgCam = camObj.GetComponent<PortraitARPGCamera>();
        arpgCam.playerTarget = player.transform;
        arpgCam.pitchAngle = 24f;
        arpgCam.cameraDistance = 3.2f;

        Vector3 focusPoint = player.transform.position + Vector3.up * (arpgCam.targetFocusHeight + 0.75f);
        Quaternion camRot = Quaternion.Euler(arpgCam.pitchAngle, arpgCam.yawAngle, 0f);
        camObj.transform.position = focusPoint - camRot * Vector3.forward * arpgCam.cameraDistance;
        camObj.transform.rotation = camRot;

        // 9. Mobile HUD
        CreateOverworldHUD(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"💾 [TAVERN INTERIOR] Saved '{scenePath}' successfully!");
    }
    #endregion

    #region Scene 3: ShopInterior.unity (Forge & Apothecary)
    private static void BuildShopInteriorScene(WorldMaterials m)
    {
        string scenePath = "Assets/Scenes/ShopInterior.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting
        GameObject sun = new GameObject("ShopAmbientLight", typeof(Light));
        Light light = sun.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.92f, 0.82f);
        light.intensity = 0.55f;
        sun.transform.rotation = Quaternion.Euler(65f, -20f, 0f);

        new GameObject("SceneTransitionManager", typeof(SceneTransitionManager));

        // 2. Room Shell: 15m x 12m stone workshop
        GameObject roomRoot = new GameObject("ShopWorkshopRoom");
        CreateBox(roomRoot.transform, "ShopFloor", new Vector3(0f, -0.1f, 0f), new Vector3(15f, 0.2f, 12f), m.cobblestone);

        // Walls (South cutaway walls for isometric visibility)
        CreateBox(roomRoot.transform, "Wall_North", new Vector3(0f, 2.5f, 6f), new Vector3(15f, 5f, 0.6f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_South_Left", new Vector3(-4.5f, 0.6f, -6f), new Vector3(6f, 1.2f, 0.6f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_South_Right", new Vector3(4.5f, 0.6f, -6f), new Vector3(6f, 1.2f, 0.6f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_East", new Vector3(7.5f, 2.5f, 0f), new Vector3(0.6f, 5f, 12f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_West", new Vector3(-7.5f, 2.5f, 0f), new Vector3(0.6f, 5f, 12f), m.townWall);

        // Forge Furnace on East side
        GameObject forgeRoot = new GameObject("BlacksmithForgeStation");
        forgeRoot.transform.SetParent(roomRoot.transform, false);
        CreateBox(forgeRoot.transform, "FurnaceBody", new Vector3(5.5f, 1.5f, 2.5f), new Vector3(2.5f, 3f, 2.5f), m.dungeonStone);
        CreateBox(forgeRoot.transform, "GlowingCoals", new Vector3(5.5f, 0.8f, 2.5f), new Vector3(1.8f, 0.4f, 1.8f), m.hearthFire);

        GameObject forgeLight = new GameObject("ForgeGlowLight", typeof(Light));
        forgeLight.transform.SetParent(forgeRoot.transform, false);
        forgeLight.transform.position = new Vector3(5.5f, 1.5f, 2.5f);
        Light fl = forgeLight.GetComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(1.0f, 0.45f, 0.1f);
        fl.range = 8f;
        fl.intensity = 2f;

        CreateBox(forgeRoot.transform, "Anvil", new Vector3(4f, 0.5f, 0f), new Vector3(1.2f, 1f, 0.8f), m.woodBeam);
        CreateOverworldNPC(roomRoot.transform, "Master Smith Keith", new Vector3(5.5f, 0f, 0f), new Color(0.6f, 0.3f, 0.2f), "Blacksmith", "Quality steel forged in spire flames.", m);

        // Alchemy Brewing Station on West side
        GameObject alchRoot = new GameObject("AlchemyBrewingStation");
        alchRoot.transform.SetParent(roomRoot.transform, false);
        CreateBox(alchRoot.transform, "HerbTable", new Vector3(-5f, 0.5f, 0.5f), new Vector3(2.2f, 1f, 5f), m.woodPlank);
        CreateBox(alchRoot.transform, "AlembicCauldron", new Vector3(-5f, 1.3f, 1.5f), new Vector3(0.8f, 0.8f, 0.8f), m.goldTrim);
        CreateBox(alchRoot.transform, "PotionRack", new Vector3(-7f, 2.2f, 0.5f), new Vector3(0.5f, 2.5f, 5f), m.woodBeam);
        CreateOverworldNPC(roomRoot.transform, "Apothecary Cheryl", new Vector3(-3.5f, 0f, 0.5f), new Color(0.2f, 0.7f, 0.8f), "Alchemist", "Herbs and draughts prepared to soothe any wound.", m);

        // Exit Doorway on South Wall
        GameObject doorwayRoot = new GameObject("ShopExitDoorway");
        doorwayRoot.transform.SetParent(roomRoot.transform, false);
        doorwayRoot.transform.position = new Vector3(0f, 0f, -5.8f);

        CreateBox(doorwayRoot.transform, "DoorFrame", new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 2.2f, 0.4f), m.woodBeam);
        CreateBox(doorwayRoot.transform, "DoorLeaf", new Vector3(0f, 0.95f, 0f), new Vector3(2.0f, 1.9f, 0.15f), m.doorWood);

        GameObject exitTrigger = new GameObject("ExitToOverworldTrigger", typeof(BoxCollider), typeof(BuildingEntranceTrigger));
        exitTrigger.transform.SetParent(doorwayRoot.transform, false);
        exitTrigger.transform.localPosition = new Vector3(0f, 1.2f, 0.4f);
        BoxCollider boxCol = exitTrigger.GetComponent<BoxCollider>();
        boxCol.size = new Vector3(2.2f, 2.6f, 1.5f);
        boxCol.isTrigger = true;

        BuildingEntranceTrigger trig = exitTrigger.GetComponent<BuildingEntranceTrigger>();
        trig.targetSceneName = "OverworldMap";
        trig.targetSpawnMarkerId = "Overworld_ForgeDoor";
        trig.locationTitle = "Solas Town Square";
        trig.triggerOnTouch = true;

        // Inside spawn marker
        GameObject spawnMarker = new GameObject("Shop_Interior_Doorway", typeof(SceneEntranceMarker));
        spawnMarker.transform.position = new Vector3(0f, 0.9f, -4.5f);
        SceneEntranceMarker markerComp = spawnMarker.GetComponent<SceneEntranceMarker>();
        markerComp.markerId = "Shop_Interior_Doorway";
        markerComp.facingYaw = 0f;

        // Player & Camera
        GameObject player = CreatePlayerHero(new Vector3(0f, 0.9f, -4.5f), m);

        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PortraitARPGCamera));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.15f);
        cam.fieldOfView = 68f;

        PortraitARPGCamera arpgCam = camObj.GetComponent<PortraitARPGCamera>();
        arpgCam.playerTarget = player.transform;
        arpgCam.pitchAngle = 24f;
        arpgCam.cameraDistance = 3.2f;

        Vector3 focusPoint = player.transform.position + Vector3.up * (arpgCam.targetFocusHeight + 0.75f);
        Quaternion camRot = Quaternion.Euler(arpgCam.pitchAngle, arpgCam.yawAngle, 0f);
        camObj.transform.position = focusPoint - camRot * Vector3.forward * arpgCam.cameraDistance;
        camObj.transform.rotation = camRot;

        CreateOverworldHUD(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"💾 [SHOP INTERIOR] Saved '{scenePath}' successfully!");
    }
    #endregion

    #region Scene 4: GuildHallInterior.unity (Adventurer's Guild & Task Board)
    private static void BuildGuildHallInteriorScene(WorldMaterials m)
    {
        string scenePath = "Assets/Scenes/GuildHallInterior.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        GameObject sun = new GameObject("Hall_KeyLight", typeof(Light));
        Light sl = sun.GetComponent<Light>();
        sl.type = LightType.Directional;
        sl.color = new Color(1.0f, 0.92f, 0.82f);
        sl.intensity = 1.1f;
        sl.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        RenderSettings.ambientLight = new Color(0.35f, 0.30f, 0.38f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        // 2. Persistent Singletons
        new GameObject("SceneTransitionManager", typeof(SceneTransitionManager));
        new GameObject("QuestManager", typeof(QuestManager));

        // 3. Room Shell: 20m x 16m grand timber & stone guild hall
        GameObject roomRoot = new GameObject("GuildHallChamber");

        // Polished Wooden Plank Floor
        CreateBox(roomRoot.transform, "GuildFloor", new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 16f), m.woodPlank);

        // Grand Guild Crimson Runner Carpet down center aisle
        CreateBox(roomRoot.transform, "GuildRunnerCarpet", new Vector3(0f, 0.02f, -0.5f), new Vector3(4.5f, 0.04f, 14f), m.guildRed);
        CreateBox(roomRoot.transform, "CarpetTrimLeft", new Vector3(-2.3f, 0.03f, -0.5f), new Vector3(0.2f, 0.04f, 14f), m.goldTrim);
        CreateBox(roomRoot.transform, "CarpetTrimRight", new Vector3(2.3f, 0.03f, -0.5f), new Vector3(0.2f, 0.04f, 14f), m.goldTrim);

        // Timber & Stone Walls (South walls are low cutaway walls for isometric visibility)
        CreateBox(roomRoot.transform, "Wall_North", new Vector3(0f, 3f, 8f), new Vector3(20f, 6f, 0.6f), m.dungeonStone);
        CreateBox(roomRoot.transform, "Wall_South_Left", new Vector3(-6.2f, 0.6f, -8f), new Vector3(7.6f, 1.2f, 0.6f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_South_Right", new Vector3(6.2f, 0.6f, -8f), new Vector3(7.6f, 1.2f, 0.6f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_East", new Vector3(10f, 3f, 0f), new Vector3(0.6f, 6f, 16f), m.townWall);
        CreateBox(roomRoot.transform, "Wall_West", new Vector3(-10f, 3f, 0f), new Vector3(0.6f, 6f, 16f), m.townWall);

        // High Perimeter Ceiling Trusses
        CreateBox(roomRoot.transform, "PerimeterBeam_North", new Vector3(0f, 5.8f, 7.7f), new Vector3(20f, 0.4f, 0.4f), m.woodBeam);
        CreateBox(roomRoot.transform, "PerimeterBeam_East", new Vector3(9.7f, 5.8f, 0f), new Vector3(0.4f, 0.4f, 16f), m.woodBeam);
        CreateBox(roomRoot.transform, "PerimeterBeam_West", new Vector3(-9.7f, 5.8f, 0f), new Vector3(0.4f, 0.4f, 16f), m.woodBeam);
        CreateBox(roomRoot.transform, "CeilingTruss_High", new Vector3(0f, 5.8f, 4f), new Vector3(20f, 0.4f, 0.4f), m.woodBeam);

        // 4. North End: Grand Guildmaster Reception Desk & Guild Banner
        GameObject receptionRoot = new GameObject("GuildmasterReceptionDesk");
        receptionRoot.transform.SetParent(roomRoot.transform, false);
        CreateBox(receptionRoot.transform, "DeskCounter", new Vector3(0f, 0.6f, 5.2f), new Vector3(7.5f, 1.2f, 1.6f), m.woodBeam);
        CreateBox(receptionRoot.transform, "DeskTopPlank", new Vector3(0f, 1.22f, 5.2f), new Vector3(7.8f, 0.08f, 1.8f), m.woodPlank);
        CreateBox(receptionRoot.transform, "GuildLedgerBook", new Vector3(-1.8f, 1.3f, 5.2f), new Vector3(0.6f, 0.12f, 0.8f), m.parchment);
        CreateBox(receptionRoot.transform, "InkWellAndQuill", new Vector3(-2.4f, 1.32f, 5.2f), new Vector3(0.2f, 0.15f, 0.2f), m.dungeonStone);

        // Guild Banner behind Guildmaster
        CreateBox(receptionRoot.transform, "GuildBannerBackdrop", new Vector3(0f, 4.2f, 7.6f), new Vector3(3.8f, 3.2f, 0.1f), m.guildRed);
        CreateBox(receptionRoot.transform, "GuildCrestGoldTrim", new Vector3(0f, 4.2f, 7.55f), new Vector3(1.8f, 1.8f, 0.12f), m.goldTrim);

        // Guildmaster Vance NPC
        CreateOverworldNPC(roomRoot.transform, "Guildmaster Vance", new Vector3(0f, 0f, 6.5f), new Color(0.85f, 0.35f, 0.15f), "Guildmaster", "The Spire is stirring. Check the Task Board on the west wall for open contracts.", m);

        // 5. West Side: THE PHYSICAL 3D QUEST TASK BOARD
        GameObject taskBoardRoot = new GameObject("GuildTaskBoard_3D");
        taskBoardRoot.transform.SetParent(roomRoot.transform, false);
        taskBoardRoot.transform.position = new Vector3(-3.8f, 0f, 0.5f);
        taskBoardRoot.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

        // Sturdy wooden support pillars
        CreateBox(taskBoardRoot.transform, "SupportPillar_Left", new Vector3(0f, 1.2f, -2.6f), new Vector3(0.35f, 2.4f, 0.35f), m.woodBeam);
        CreateBox(taskBoardRoot.transform, "SupportPillar_Right", new Vector3(0f, 1.2f, 2.6f), new Vector3(0.35f, 2.4f, 0.35f), m.woodBeam);

        // Wooden frame
        CreateBox(taskBoardRoot.transform, "BoardFrame", new Vector3(0f, 2.4f, 0f), new Vector3(0.35f, 2.7f, 5.8f), m.woodBeam);
        // Corkboard surface
        CreateBox(taskBoardRoot.transform, "CorkboardSurface", new Vector3(0.12f, 2.4f, 0f), new Vector3(0.15f, 2.4f, 5.4f), m.corkboard);
        // Top Plaque Header
        CreateBox(taskBoardRoot.transform, "PlaqueHeader", new Vector3(0.20f, 3.85f, 0f), new Vector3(0.10f, 0.40f, 4.4f), m.goldTrim);

        // Pinned Parchment Notices across the corkboard
        float[,] flyerOffsets = {
            { 2.85f, -1.8f }, { 2.85f, -0.6f }, { 2.85f, 0.6f }, { 2.85f, 1.8f },
            { 1.85f, -1.8f }, { 1.85f, -0.6f }, { 1.85f, 0.6f }, { 1.85f, 1.8f }
        };
        for (int p = 0; p < flyerOffsets.GetLength(0); p++)
        {
            float fy = flyerOffsets[p, 0];
            float fz = flyerOffsets[p, 1];
            GameObject notice = CreateBox(taskBoardRoot.transform, $"PinnedNotice_{p + 1}", new Vector3(0.20f, fy, fz), new Vector3(0.02f, 0.70f, 0.60f), m.parchment);
            float rotRoll = (p % 2 == 0) ? -5f : 5f;
            notice.transform.localRotation = Quaternion.Euler(0f, 0f, rotRoll);
            CreateBox(notice.transform, "IronPin", new Vector3(0.02f, 0.28f, 0f), new Vector3(0.04f, 0.08f, 0.08f), m.woodBeam);
        }

        // Spotlight illuminating the Task Board
        GameObject boardLightObj = new GameObject("TaskBoardSpotlight", typeof(Light));
        boardLightObj.transform.SetParent(taskBoardRoot.transform, false);
        boardLightObj.transform.localPosition = new Vector3(1.8f, 3.4f, 0f);
        Light bl = boardLightObj.GetComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(1.0f, 0.95f, 0.82f);
        bl.range = 7.5f;
        bl.intensity = 2.2f;

        // Attach GuildTaskBoardTrigger to the Task Board
        BoxCollider boardCol = taskBoardRoot.AddComponent<BoxCollider>();
        boardCol.isTrigger = true;
        boardCol.center = new Vector3(1.5f, 1.8f, 0f);
        boardCol.size = new Vector3(4.0f, 3.5f, 6.5f);
        taskBoardRoot.AddComponent<GuildTaskBoardTrigger>();

        // Quest Clerk Mirabel standing next to the board
        CreateOverworldNPC(roomRoot.transform, "Clerk Mirabel", new Vector3(-5.2f, 0f, 2.8f), new Color(0.20f, 0.58f, 0.85f), "Quest Clerk", "Contracts for bounties, hunts, escorts, and supplies are pinned to the board.", m);

        // 6. East Side: Strategy Table & Adventurer Lounge
        GameObject loungeRoot = new GameObject("AdventurerLounge");
        loungeRoot.transform.SetParent(roomRoot.transform, false);
        loungeRoot.transform.position = new Vector3(3.8f, 0f, 0.5f);
        CreateBox(loungeRoot.transform, "WarRoomTable", new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1.0f, 5.5f), m.woodPlank);
        CreateBox(loungeRoot.transform, "MapScroll", new Vector3(0f, 1.02f, 0f), new Vector3(1.6f, 0.04f, 2.8f), m.parchment);
        CreateBox(loungeRoot.transform, "Bench_East", new Vector3(1.6f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 5.0f), m.woodBeam);
        CreateBox(loungeRoot.transform, "Bench_West", new Vector3(-1.6f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 5.0f), m.woodBeam);

        // Weapon Trophy Rack on East Wall
        CreateBox(roomRoot.transform, "ShieldTrophy", new Vector3(9.6f, 3.2f, 0.5f), new Vector3(0.15f, 1.5f, 1.2f), m.goldTrim);
        CreateBox(roomRoot.transform, "WeaponRack", new Vector3(9.6f, 1.2f, 4.2f), new Vector3(0.4f, 2.2f, 2.4f), m.woodBeam);

        // Veteran Adventurer NPC at table
        CreateOverworldNPC(roomRoot.transform, "Veteran Roland", new Vector3(4.8f, 0f, 3.6f), new Color(0.55f, 0.25f, 0.20f), "Veteran", "Don't take on the Dire Wolf bounty without ample potion reserves.", m);

        // 7. South Exit Doorway & Trigger back to OverworldMap
        GameObject doorwayRoot = new GameObject("GuildExitDoorway");
        doorwayRoot.transform.SetParent(roomRoot.transform, false);
        doorwayRoot.transform.position = new Vector3(0f, 0f, -7.8f);

        CreateBox(doorwayRoot.transform, "DoorFrame", new Vector3(0f, 1.1f, 0f), new Vector3(2.6f, 2.2f, 0.4f), m.woodBeam);
        CreateBox(doorwayRoot.transform, "DoorLeaf", new Vector3(0f, 0.95f, 0f), new Vector3(2.2f, 1.9f, 0.15f), m.doorWood);
        CreateBox(doorwayRoot.transform, "Doormat", new Vector3(0f, 0.02f, 0.6f), new Vector3(2.4f, 0.04f, 1.2f), m.woodBeam);

        GameObject exitTrigger = new GameObject("ExitToOverworldTrigger", typeof(BoxCollider), typeof(BuildingEntranceTrigger));
        exitTrigger.transform.SetParent(doorwayRoot.transform, false);
        exitTrigger.transform.localPosition = new Vector3(0f, 1.2f, 0.4f);
        BoxCollider boxCol = exitTrigger.GetComponent<BoxCollider>();
        boxCol.size = new Vector3(2.4f, 2.6f, 1.5f);
        boxCol.isTrigger = true;

        BuildingEntranceTrigger trig = exitTrigger.GetComponent<BuildingEntranceTrigger>();
        trig.targetSceneName = "OverworldMap";
        trig.targetSpawnMarkerId = "Overworld_GuildHallDoor";
        trig.locationTitle = "Solas Town Square";
        trig.triggerOnTouch = true;

        // Interior Spawn Marker
        GameObject spawnMarker = new GameObject("GuildHall_Interior_Doorway", typeof(SceneEntranceMarker));
        spawnMarker.transform.position = new Vector3(0f, 0.9f, -6.5f);
        SceneEntranceMarker markerComp = spawnMarker.GetComponent<SceneEntranceMarker>();
        markerComp.markerId = "GuildHall_Interior_Doorway";
        markerComp.facingYaw = 0f; // Facing into room (North down red carpet)

        // 8. Player Hero & Camera
        GameObject player = CreatePlayerHero(new Vector3(0f, 0.9f, -6.5f), m);

        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PortraitARPGCamera));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0.10f, 0.08f, 0.12f);
        cam.fieldOfView = 68f;

        PortraitARPGCamera arpgCam = camObj.GetComponent<PortraitARPGCamera>();
        arpgCam.playerTarget = player.transform;
        arpgCam.pitchAngle = 24f;
        arpgCam.cameraDistance = 3.2f;

        Vector3 focusPoint = player.transform.position + Vector3.up * (arpgCam.targetFocusHeight + 0.75f);
        Quaternion camRot = Quaternion.Euler(arpgCam.pitchAngle, arpgCam.yawAngle, 0f);
        camObj.transform.position = focusPoint - camRot * Vector3.forward * arpgCam.cameraDistance;
        camObj.transform.rotation = camRot;

        // 9. Mobile HUD & GuildTaskBoardUI
        CreateOverworldHUD(player);

        GameObject uiManagerGo = new GameObject("GuildTaskBoardUI", typeof(GuildTaskBoardUI));
        GuildTaskBoardUI boardUI = uiManagerGo.GetComponent<GuildTaskBoardUI>();
        boardUI.BuildUIHierarchyIfMissing();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"💾 [GUILD HALL INTERIOR] Saved '{scenePath}' successfully!");
    }
    #endregion

    #region Helper Procedural Builders
    private static void CreateBuilding(
        Transform parent, string name, Vector3 pos, Vector3 size,
        Material wallMat, Material roofMat, Material beamMat, Material doorMat, Material windowMat,
        string targetSceneName, string targetSpawnMarkerId, string locationTitle, string outsideSpawnMarkerId,
        Vector3 doorLocalPos)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;

        float w = size.x; float h = size.y; float d = size.z;

        // Main Building Box Walls
        CreateBox(root.transform, "BuildingWalls", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wallMat);

        // Corner Timber Beams
        float halfW = w * 0.5f; float halfD = d * 0.5f;
        CreateBox(root.transform, "Beam_FL", new Vector3(-halfW, h * 0.5f, halfD), new Vector3(0.5f, h, 0.5f), beamMat);
        CreateBox(root.transform, "Beam_FR", new Vector3(halfW, h * 0.5f, halfD), new Vector3(0.5f, h, 0.5f), beamMat);
        CreateBox(root.transform, "Beam_BL", new Vector3(-halfW, h * 0.5f, -halfD), new Vector3(0.5f, h, 0.5f), beamMat);
        CreateBox(root.transform, "Beam_BR", new Vector3(halfW, h * 0.5f, -halfD), new Vector3(0.5f, h, 0.5f), beamMat);

        // Pitched Roof
        GameObject roof = CreateBox(root.transform, "PitchedRoof", new Vector3(0f, h + 1.2f, 0f), new Vector3(w + 1.2f, 2.4f, d + 1.2f), roofMat);
        roof.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

        // Chimney
        CreateBox(root.transform, "Chimney", new Vector3(halfW - 1.2f, h + 2.5f, 0f), new Vector3(1.2f, 3f, 1.2f), wallMat);

        // Windows with glowing warm glass
        CreateBox(root.transform, "Window_L", new Vector3(-halfW * 0.5f, h * 0.6f, halfD + 0.05f), new Vector3(1.2f, 1.4f, 0.1f), windowMat);
        CreateBox(root.transform, "Window_R", new Vector3(halfW * 0.5f, h * 0.6f, halfD + 0.05f), new Vector3(1.2f, 1.4f, 0.1f), windowMat);

        // Doorway
        Vector3 doorWorldPos = pos + doorLocalPos;
        GameObject doorway = new GameObject("BuildingDoorway");
        doorway.transform.SetParent(root.transform, false);
        doorway.transform.localPosition = doorLocalPos;

        // Wooden Door Graphic
        CreateBox(doorway.transform, "DoorLeaf", new Vector3(0f, 1.2f, 0f), new Vector3(1.8f, 2.4f, 0.15f), doorMat);
        CreateBox(doorway.transform, "DoorFrame", new Vector3(0f, 1.4f, 0f), new Vector3(2.2f, 2.8f, 0.35f), beamMat);

        // Warm threshold lantern
        GameObject doorLight = new GameObject("DoorwayLantern", typeof(Light));
        doorLight.transform.SetParent(doorway.transform, false);
        doorLight.transform.localPosition = new Vector3(0f, 2.6f, 0.6f);
        Light l = doorLight.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.85f, 0.45f);
        l.range = 5f;
        l.intensity = 1.2f;

        // Doorway Entrance Trigger (loads the interior scene)
        GameObject trigGo = new GameObject("EntranceTrigger", typeof(BoxCollider), typeof(BuildingEntranceTrigger));
        trigGo.transform.SetParent(doorway.transform, false);
        trigGo.transform.localPosition = new Vector3(0f, 1.0f, 0.8f);
        BoxCollider bc = trigGo.GetComponent<BoxCollider>();
        bc.size = new Vector3(2.2f, 2.4f, 1.8f);
        bc.isTrigger = true;

        BuildingEntranceTrigger bet = trigGo.GetComponent<BuildingEntranceTrigger>();
        bet.targetSceneName = targetSceneName;
        bet.targetSpawnMarkerId = targetSpawnMarkerId;
        bet.locationTitle = locationTitle;
        bet.triggerOnTouch = true;

        // Outside Spawn Marker (where player appears after leaving interior back to overworld)
        if (!string.IsNullOrEmpty(outsideSpawnMarkerId))
        {
            GameObject outsideMarker = new GameObject(outsideSpawnMarkerId, typeof(SceneEntranceMarker));
            outsideMarker.transform.position = doorWorldPos + new Vector3(0f, 0.9f, 1.8f);
            SceneEntranceMarker sm = outsideMarker.GetComponent<SceneEntranceMarker>();
            sm.markerId = outsideSpawnMarkerId;
            sm.facingYaw = 180f; // Facing away from door into town
        }
    }

    private static void CreatePineTree(Transform parent, Vector3 pos, float scale, WorldMaterials m)
    {
        GameObject tree = new GameObject("PineTree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;

        // Trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 1.5f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.5f * scale, 1.5f * scale, 0.5f * scale);
        trunk.GetComponent<Renderer>().sharedMaterial = m.woodBark;

        // 3 Layered Foliage Cones
        for (int i = 0; i < 3; i++)
        {
            GameObject tier = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tier.name = $"FoliageTier_{i + 1}";
            tier.transform.SetParent(tree.transform, false);
            tier.transform.localPosition = new Vector3(0f, (2.6f + i * 1.5f) * scale, 0f);
            float radius = (2.8f - i * 0.7f) * scale;
            tier.transform.localScale = new Vector3(radius, 0.8f * scale, radius);
            tier.GetComponent<Renderer>().sharedMaterial = m.pineNeedles;
        }
    }

    private static void CreateOakTree(Transform parent, Vector3 pos, float scale, WorldMaterials m)
    {
        GameObject tree = new GameObject("OakTree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 1.8f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.7f * scale, 1.8f * scale, 0.7f * scale);
        trunk.GetComponent<Renderer>().sharedMaterial = m.woodBark;

        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.name = "Canopy";
        foliage.transform.SetParent(tree.transform, false);
        foliage.transform.localPosition = new Vector3(0f, 4.2f * scale, 0f);
        foliage.transform.localScale = new Vector3(3.8f * scale, 3.2f * scale, 3.8f * scale);
        foliage.GetComponent<Renderer>().sharedMaterial = m.forestGrass;
    }

    private static void CreateRock(Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = "RockBoulder";
        rock.transform.SetParent(parent, false);
        rock.transform.position = pos + new Vector3(0f, size.y * 0.45f, 0f);
        rock.transform.localScale = size;
        rock.transform.localEulerAngles = new Vector3(15f, 35f, 10f);
        rock.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreateTownFountain(Transform parent, Vector3 pos, WorldMaterials m)
    {
        GameObject fRoot = new GameObject("TownFountain");
        fRoot.transform.SetParent(parent, false);
        fRoot.transform.position = pos;

        // Circular stone basin
        GameObject basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basin.transform.SetParent(fRoot.transform, false);
        basin.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        basin.transform.localScale = new Vector3(6f, 0.4f, 6f);
        basin.GetComponent<Renderer>().sharedMaterial = m.cobblestone;

        // Water surface
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        water.transform.SetParent(fRoot.transform, false);
        water.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        water.transform.localScale = new Vector3(5.2f, 0.1f, 5.2f);
        water.GetComponent<Renderer>().sharedMaterial = m.fountainWater;

        // Center Pillar & Spire Jet
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.transform.SetParent(fRoot.transform, false);
        pillar.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        pillar.transform.localScale = new Vector3(1.2f, 1.8f, 1.2f);
        pillar.GetComponent<Renderer>().sharedMaterial = m.goldTrim;
    }

    private static void CreateStreetLantern(Transform parent, Vector3 pos, WorldMaterials m)
    {
        GameObject lantern = new GameObject("StreetLantern");
        lantern.transform.SetParent(parent, false);
        lantern.transform.position = pos;

        // Post
        CreateBox(lantern.transform, "Post", new Vector3(0f, 1.8f, 0f), new Vector3(0.2f, 3.6f, 0.2f), m.woodBeam);
        // Lantern head
        CreateBox(lantern.transform, "Lamp", new Vector3(0f, 3.4f, 0f), new Vector3(0.6f, 0.8f, 0.6f), m.lanternGlow);

        GameObject lGo = new GameObject("LanternLight", typeof(Light));
        lGo.transform.SetParent(lantern.transform, false);
        lGo.transform.localPosition = new Vector3(0f, 3.4f, 0f);
        Light l = lGo.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.85f, 0.45f);
        l.range = 9f;
        l.intensity = 1.4f;
    }

    private static void CreateMarketStall(Transform parent, Vector3 pos, Color clothColor, WorldMaterials m)
    {
        GameObject stall = new GameObject("MarketStall");
        stall.transform.SetParent(parent, false);
        stall.transform.position = pos;

        CreateBox(stall.transform, "StallTable", new Vector3(0f, 0.5f, 0f), new Vector3(2.5f, 1f, 1.4f), m.woodPlank);
        CreateBox(stall.transform, "Post_L", new Vector3(-1.1f, 1.4f, 0f), new Vector3(0.12f, 1.8f, 0.12f), m.woodBeam);
        CreateBox(stall.transform, "Post_R", new Vector3(1.1f, 1.4f, 0f), new Vector3(0.12f, 1.8f, 0.12f), m.woodBeam);

        Material canopyMat = new Material(m.roofTile);
        canopyMat.color = clothColor;
        CreateBox(stall.transform, "Canopy", new Vector3(0f, 2.3f, 0f), new Vector3(2.8f, 0.15f, 1.8f), canopyMat);
    }

    private static void CreateCrateStack(Transform parent, Vector3 pos, WorldMaterials m)
    {
        GameObject stack = new GameObject("CrateStack");
        stack.transform.SetParent(parent, false);
        stack.transform.position = pos;

        CreateBox(stack.transform, "Crate_1", new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f), m.woodPlank);
        CreateBox(stack.transform, "Crate_2", new Vector3(0.7f, 0.35f, 0.2f), new Vector3(0.7f, 0.7f, 0.7f), m.woodPlank);
        CreateBox(stack.transform, "Crate_3", new Vector3(0.1f, 1.05f, 0.1f), new Vector3(0.6f, 0.6f, 0.6f), m.woodPlank);
    }

    private static void CreateForestShrine(Transform parent, Vector3 pos, WorldMaterials m)
    {
        GameObject shrine = new GameObject("ForestAltarShrine");
        shrine.transform.SetParent(parent, false);
        shrine.transform.position = pos;

        CreateBox(shrine.transform, "AltarBase", new Vector3(0f, 0.4f, 0f), new Vector3(3f, 0.8f, 2.2f), m.dungeonStone);
        CreateBox(shrine.transform, "RelicCore", new Vector3(0f, 1.2f, 0f), new Vector3(0.6f, 0.8f, 0.6f), m.portalEnergy);

        GameObject pLight = new GameObject("ShrineGlow", typeof(Light));
        pLight.transform.SetParent(shrine.transform, false);
        pLight.transform.position = pos + new Vector3(0f, 1.5f, 0f);
        Light l = pLight.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.4f, 0.85f, 1.0f);
        l.range = 7f;
        l.intensity = 2f;
    }

    private static void CreateSpireDungeonPortal(Transform parent, Vector3 pos, WorldMaterials m)
    {
        GameObject portalRoot = new GameObject("SpireDungeonGate");
        portalRoot.transform.SetParent(parent, false);
        portalRoot.transform.position = pos;

        // Left & Right Gothic Arch Pillars
        CreateBox(portalRoot.transform, "PortalPillar_L", new Vector3(-2.8f, 4f, 0f), new Vector3(1.4f, 8f, 1.4f), m.dungeonStone);
        CreateBox(portalRoot.transform, "PortalPillar_R", new Vector3(2.8f, 4f, 0f), new Vector3(1.4f, 8f, 1.4f), m.dungeonStone);
        CreateBox(portalRoot.transform, "PortalArch_Top", new Vector3(0f, 7.8f, 0f), new Vector3(7f, 1.5f, 1.6f), m.dungeonStone);

        // Glowing Ethereal Portal Vortex
        GameObject vortex = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vortex.name = "PortalVortex";
        vortex.transform.SetParent(portalRoot.transform, false);
        vortex.transform.localPosition = new Vector3(0f, 4f, 0f);
        vortex.transform.localScale = new Vector3(4.5f, 6.5f, 1f);
        vortex.GetComponent<Renderer>().sharedMaterial = m.portalEnergy;

        // Glowing Portal Light
        GameObject pLight = new GameObject("PortalLight", typeof(Light));
        pLight.transform.SetParent(portalRoot.transform, false);
        pLight.transform.localPosition = new Vector3(0f, 4f, 0.5f);
        Light l = pLight.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.7f, 0.2f, 1f);
        l.range = 14f;
        l.intensity = 3.5f;

        // Doorway Trigger Component linking to MainDungeonFloor
        GameObject trigGo = new GameObject("DungeonEntranceTrigger", typeof(BoxCollider), typeof(BuildingEntranceTrigger));
        trigGo.transform.SetParent(portalRoot.transform, false);
        trigGo.transform.localPosition = new Vector3(0f, 2f, 0.8f);
        BoxCollider bc = trigGo.GetComponent<BoxCollider>();
        bc.size = new Vector3(4f, 4f, 2f);
        bc.isTrigger = true;

        BuildingEntranceTrigger bet = trigGo.GetComponent<BuildingEntranceTrigger>();
        bet.targetSceneName = "MainDungeonFloor";
        bet.locationTitle = "The Abyssal Spire - Floor 1";
        bet.triggerOnTouch = true;
    }

    private static void CreateDungeonPillar(Transform parent, Vector3 pos, float height, Material mat)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "BrokenSpirePillar";
        pillar.transform.SetParent(parent, false);
        pillar.transform.position = pos + new Vector3(0f, height * 0.5f, 0f);
        pillar.transform.localScale = new Vector3(1.4f, height * 0.5f, 1.4f);
        pillar.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreateOverworldNPC(Transform parent, string name, Vector3 pos, Color coatColor, string role, string quote, WorldMaterials m)
    {
        GameObject npc = new GameObject($"NPC_{name}");
        npc.transform.SetParent(parent, false);
        npc.transform.position = pos;

        // Body Capsule
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(npc.transform, false);
        body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

        Material coatMat = new Material(m.woodPlank);
        coatMat.color = coatColor;
        body.GetComponent<Renderer>().sharedMaterial = coatMat;

        // Head Sphere
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(npc.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // In-World Floating Nametag
        GameObject tagGo = new GameObject("NametagCanvas", typeof(RectTransform), typeof(Canvas), typeof(TextMeshProUGUI));
        tagGo.transform.SetParent(npc.transform, false);
        tagGo.transform.localPosition = new Vector3(0f, 2.45f, 0f);

        Canvas c = tagGo.GetComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        RectTransform rt = tagGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250f, 60f);
        rt.localScale = Vector3.one * 0.008f;
        tagGo.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);

        TextMeshProUGUI tmp = tagGo.GetComponent<TextMeshProUGUI>();
        tmp.text = $"<color=#FFD700><b>{name}</b></color>\n<size=80%>[{role}]</size>";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 18;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static void CreateStoneFireplace(Transform parent, Vector3 pos, WorldMaterials m)
    {
        GameObject fp = new GameObject("StoneFireplaceHearth");
        fp.transform.SetParent(parent, false);
        fp.transform.position = pos;

        // Stone Chimney & Mantel
        CreateBox(fp.transform, "Mantel", new Vector3(0f, 1.8f, 0f), new Vector3(4.5f, 3.6f, 1.4f), m.dungeonStone);
        CreateBox(fp.transform, "HearthOpening", new Vector3(0f, 0.8f, 0.2f), new Vector3(2.5f, 1.6f, 1.2f), m.dungeonStone);
        CreateBox(fp.transform, "BurningLogs", new Vector3(0f, 0.35f, 0.2f), new Vector3(1.8f, 0.4f, 0.8f), m.hearthFire);

        // Warm flickering light
        GameObject fireLight = new GameObject("HearthFireLight", typeof(Light));
        fireLight.transform.SetParent(fp.transform, false);
        fireLight.transform.localPosition = new Vector3(0f, 1.0f, 0.6f);
        Light l = fireLight.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.50f, 0.12f);
        l.range = 10f;
        l.intensity = 2.4f;
    }

    private static void CreateDiningTable(Transform parent, Vector3 pos, WorldMaterials m)
    {
        GameObject table = new GameObject("DiningTableSet");
        table.transform.SetParent(parent, false);
        table.transform.position = pos;

        // Table Top & Legs
        CreateBox(table.transform, "TableTop", new Vector3(0f, 0.75f, 0f), new Vector3(2.8f, 0.15f, 1.6f), m.woodPlank);
        CreateBox(table.transform, "Leg_1", new Vector3(-1.2f, 0.35f, -0.6f), new Vector3(0.15f, 0.7f, 0.15f), m.woodBeam);
        CreateBox(table.transform, "Leg_2", new Vector3(1.2f, 0.35f, -0.6f), new Vector3(0.15f, 0.7f, 0.15f), m.woodBeam);
        CreateBox(table.transform, "Leg_3", new Vector3(-1.2f, 0.35f, 0.6f), new Vector3(0.15f, 0.7f, 0.15f), m.woodBeam);
        CreateBox(table.transform, "Leg_4", new Vector3(1.2f, 0.35f, 0.6f), new Vector3(0.15f, 0.7f, 0.15f), m.woodBeam);

        // Candle Centerpiece
        CreateBox(table.transform, "Candle", new Vector3(0f, 0.95f, 0f), new Vector3(0.15f, 0.3f, 0.15f), m.lanternGlow);
        GameObject candleLight = new GameObject("CandleLight", typeof(Light));
        candleLight.transform.SetParent(table.transform, false);
        candleLight.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        Light l = candleLight.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.85f, 0.5f);
        l.range = 4f;
        l.intensity = 0.8f;

        // Chairs
        CreateBox(table.transform, "Chair_North", new Vector3(0f, 0.45f, 1.1f), new Vector3(0.6f, 0.8f, 0.6f), m.woodBeam);
        CreateBox(table.transform, "Chair_South", new Vector3(0f, 0.45f, -1.1f), new Vector3(0.6f, 0.8f, 0.6f), m.woodBeam);
    }

    private static GameObject CreatePlayerHero(Vector3 spawnPos, WorldMaterials m)
    {
        GameObject player = new GameObject("Player_Hero", typeof(CharacterController), typeof(PlayerMovementController));
        player.tag = "Player";
        player.transform.position = spawnPos;

        CharacterController cc = player.GetComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        // Visual Capsule Hero
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "HeroBody";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

        Material heroMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Player_Hero.mat") ?? m.roofSlate;
        body.GetComponent<Renderer>().sharedMaterial = heroMat;

        return player;
    }

    private static void CreateOverworldHUD(GameObject player)
    {
        GameObject canvasGo = new GameObject("OverworldHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        // Movement Joystick (Bottom-Left)
        GameObject stickRoot = new GameObject("FloatingVirtualStick", typeof(RectTransform), typeof(Image), typeof(FloatingVirtualStick));
        stickRoot.transform.SetParent(canvasGo.transform, false);
        RectTransform stickRt = stickRoot.GetComponent<RectTransform>();
        stickRt.anchorMin = new Vector2(0.04f, 0.05f);
        stickRt.anchorMax = new Vector2(0.38f, 0.22f);
        stickRt.offsetMin = Vector2.zero;
        stickRt.offsetMax = Vector2.zero;

        Image stickBgImg = stickRoot.GetComponent<Image>();
        stickBgImg.color = new Color(0, 0, 0, 0.005f);

        GameObject ringObj = new GameObject("StickBackground", typeof(RectTransform), typeof(Image));
        ringObj.transform.SetParent(stickRoot.transform, false);
        RectTransform ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = new Vector2(0.5f, 0.5f);
        ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.sizeDelta = new Vector2(190f, 190f);
        ringRt.anchoredPosition = Vector2.zero;
        Image ringImg = ringObj.GetComponent<Image>();
        Sprite ringSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Mockup_JoystickRing.png");
        if (ringSp != null) ringImg.sprite = ringSp;
        ringImg.color = Color.white;

        GameObject handleObj = new GameObject("StickHandle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(ringObj.transform, false);
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0.5f, 0.5f);
        handleRt.anchorMax = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(90f, 90f);
        handleRt.anchoredPosition = Vector2.zero;
        Image handleImg = handleObj.GetComponent<Image>();
        Sprite handleSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Mockup_JoystickHandle.png");
        if (handleSp != null) handleImg.sprite = handleSp;
        handleImg.color = Color.white;

        FloatingVirtualStick fvs = stickRoot.GetComponent<FloatingVirtualStick>();
        fvs.BindStickVisuals(ringRt, handleRt, 80f);

        // Top Spire Broadcast Banner
        GameObject bannerObj = new GameObject("SpireBroadcastBanner", typeof(RectTransform), typeof(Image));
        bannerObj.transform.SetParent(canvasGo.transform, false);
        RectTransform bRt = bannerObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.10f, 0.93f);
        bRt.anchorMax = new Vector2(0.90f, 0.985f);
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;
        Image bImg = bannerObj.GetComponent<Image>();
        Sprite bSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Mockup_BroadcastBanner.png");
        if (bSp != null) bImg.sprite = bSp;
        bImg.color = Color.white;

        GameObject textObj = new GameObject("BannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(bannerObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "SOLAS OVERWORLD: Explore towns, enter buildings, or venture into the Mistwood Spire.";
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.88f, 0.70f);
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPos;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = mat;
        return box;
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }

    [MenuItem("Tools/BattleSummoner/Register All Scenes in Build Settings")]
    public static void RegisterScenesInBuildSettings()
    {
        string[] scenePaths = {
            "Assets/Scenes/OverworldMap.unity",
            "Assets/Scenes/CampGroupChat.unity",
            "Assets/Scenes/CampfireHub.unity",
            "Assets/Scenes/MainDungeonFloor.unity",
            "Assets/Scenes/WorldEntryExperience.unity",
            "Assets/Scenes/BuildingInterior.unity",
            "Assets/Scenes/ShopInterior.unity",
            "Assets/Scenes/GuildHallInterior.unity",
            "Assets/Scenes/RelicTestingStage.unity",
            "Assets/Scenes/MultiCharacterDialogueHub.unity"
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < scenePaths.Length; i++)
        {
            if (File.Exists(scenePaths[i]))
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePaths[i], true));
            }
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"📋 [BUILD SETTINGS] Registered {buildScenes.Count} scenes into EditorBuildSettings.");
    }
    #endregion
}

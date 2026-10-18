using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OverworldPieceBuilder
{
    private const string MODELS_DIR = "Assets/Models/OverworldPieces/";
    private const string TEXTURES_DIR = "Assets/Models/OverworldPieces/Textures/";
    private const string MATS_DIR = "Assets/Models/OverworldPieces/Materials/";
    private const string PREFABS_DIR = "Assets/Prefabs/Overworld/";

    [MenuItem("BattleSummoner/Build Overworld Pieces and Interior Scene")]
    public static void BuildAllPiecesAndScenes()
    {
        EnsureDirectories();
        AssetDatabase.Refresh();

        // 1. Create Materials for each piece
        Dictionary<string, Material> materials = CreateMaterials();

        // 2. Build Prefabs
        CreatePrefabs(materials);

        // 3. Setup OverworldMap Scene with the Meshy Base Map Floor & Doorless Exterior
        SetupOverworldScene();

        // 4. Create and Setup ShopInterior Scene with the Meshy Interior Asset
        SetupInteriorScene();

        Debug.Log("🏰 [OVERWORLD BUILDER] Successfully built all Meshy overworld pieces, prefabs, overworld layout, and interior store scene!");
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(MATS_DIR)) Directory.CreateDirectory(MATS_DIR);
        if (!Directory.Exists(PREFABS_DIR)) Directory.CreateDirectory(PREFABS_DIR);
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        var dict = new Dictionary<string, Material>();
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        string[] pieceNames = new string[] {
            "Overworld_BaseMap_Floor",
            "Prop_Fantasy_Tree",
            "Prop_Stone_Stairs",
            "Prop_Cliff_Boulder",
            "Prop_Foliage_Bush",
            "Prop_Market_Booth",
            "Building_Shop_Exterior",
            "Building_Shop_Interior"
        };

        foreach (var piece in pieceNames)
        {
            string matPath = $"{MATS_DIR}M_{piece}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            string texPath = $"{TEXTURES_DIR}tex_{piece}_0.png";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
                mat.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(mat);
            }

            dict[piece] = mat;
        }

        AssetDatabase.SaveAssets();
        return dict;
    }

    private static void CreatePrefabs(Dictionary<string, Material> materials)
    {
        // 1. BaseMap_Floor Prefab
        CreateBaseMapPrefab(materials["Overworld_BaseMap_Floor"]);

        // 2. Repeatable Props
        CreatePropPrefab("Prop_Fantasy_Tree", "Fantasy_Tree", materials["Prop_Fantasy_Tree"], typeof(CapsuleCollider), new Vector3(0, 2.5f, 0), 5.0f, 1.2f);
        CreatePropPrefab("Prop_Stone_Stairs", "Stone_Stairs", materials["Prop_Stone_Stairs"], typeof(MeshCollider));
        CreatePropPrefab("Prop_Cliff_Boulder", "Cliff_Boulder", materials["Prop_Cliff_Boulder"], typeof(MeshCollider));
        CreatePropPrefab("Prop_Foliage_Bush", "Foliage_Bush", materials["Prop_Foliage_Bush"], typeof(SphereCollider), new Vector3(0, 0.8f, 0), 1.6f, 0.8f);
        CreatePropPrefab("Prop_Market_Booth", "Market_Booth", materials["Prop_Market_Booth"], typeof(BoxCollider), new Vector3(0, 1.3f, 0), 2.6f, 1.4f);

        // 3. Shop Building Exterior (No Doors + Entrance Trigger)
        CreateExteriorShopPrefab(materials["Building_Shop_Exterior"]);

        // 4. Shop Building Interior (Indoor Room + Exit Trigger)
        CreateInteriorShopPrefab(materials["Building_Shop_Interior"]);

        AssetDatabase.SaveAssets();
    }

    private static void CreateBaseMapPrefab(Material mat)
    {
        string fbxPath = $"{MODELS_DIR}Overworld_BaseMap_Floor.fbx";
        string prefabPath = $"{PREFABS_DIR}BaseMap_Floor.prefab";

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        instance.name = "BaseMap_Floor";

        // Assign material
        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterial = mat;
        }

        // Add MeshCollider
        foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
        {
            var mc = mf.gameObject.GetComponent<MeshCollider>();
            if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void CreatePropPrefab(string fbxName, string prefabName, Material mat, System.Type colliderType, Vector3 center = default, float height = 2f, float radius = 1f)
    {
        string fbxPath = $"{MODELS_DIR}{fbxName}.fbx";
        string prefabPath = $"{PREFABS_DIR}{prefabName}.prefab";

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        instance.name = prefabName;

        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterial = mat;
        }

        if (colliderType == typeof(MeshCollider))
        {
            foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                var mc = mf.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }
        else if (colliderType == typeof(CapsuleCollider))
        {
            var cc = instance.AddComponent<CapsuleCollider>();
            cc.center = center;
            cc.height = height;
            cc.radius = radius;
        }
        else if (colliderType == typeof(SphereCollider))
        {
            var sc = instance.AddComponent<SphereCollider>();
            sc.center = center;
            sc.radius = radius;
        }
        else if (colliderType == typeof(BoxCollider))
        {
            var bc = instance.AddComponent<BoxCollider>();
            bc.center = center;
            bc.size = new Vector3(radius * 2f, height, radius * 2f);
        }

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void CreateExteriorShopPrefab(Material mat)
    {
        string fbxPath = $"{MODELS_DIR}Building_Shop_Exterior.fbx";
        string prefabPath = $"{PREFABS_DIR}Shop_Building_Exterior.prefab";

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        instance.name = "Shop_Building_Exterior";

        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterial = mat;
        }

        // Add MeshCollider for building walls/roof
        foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
        {
            var mc = mf.gameObject.GetComponent<MeshCollider>();
            if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }

        // Add Doorway Entrance Trigger (No door - walk-through doorway trigger)
        GameObject doorwayTrigger = new GameObject("Doorway_EntranceTrigger");
        doorwayTrigger.transform.SetParent(instance.transform);
        // Position at the front doorway entrance arch
        doorwayTrigger.transform.localPosition = new Vector3(0f, 1.0f, -0.9f);

        var box = doorwayTrigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.4f, 2.2f, 1.0f);

        var entrance = doorwayTrigger.AddComponent<BuildingEntranceTrigger>();
        entrance.targetSceneName = "ShopInterior";
        entrance.targetSpawnMarkerId = "Interior_Doorway";
        entrance.locationTitle = "The Alchemist & Apothecary Shop";
        entrance.triggerOnTouch = true;

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void CreateInteriorShopPrefab(Material mat)
    {
        string fbxPath = $"{MODELS_DIR}Building_Shop_Interior.fbx";
        string prefabPath = $"{PREFABS_DIR}Shop_Building_Interior.prefab";

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        instance.name = "Shop_Building_Interior";

        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterial = mat;
        }

        // Add MeshCollider for walls and floor
        foreach (var mf in instance.GetComponentsInChildren<MeshFilter>(true))
        {
            var mc = mf.gameObject.GetComponent<MeshCollider>();
            if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }

        // Add Exit Doorway Trigger (Walk through to return to Overworld)
        GameObject exitTrigger = new GameObject("Exit_DoorwayTrigger");
        exitTrigger.transform.SetParent(instance.transform);
        exitTrigger.transform.localPosition = new Vector3(0f, 1.0f, -0.85f);

        var box = exitTrigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.4f, 2.2f, 1.0f);

        var entrance = exitTrigger.AddComponent<BuildingEntranceTrigger>();
        entrance.targetSceneName = "OverworldMap";
        entrance.targetSpawnMarkerId = "Overworld_ShopDoor";
        entrance.locationTitle = "Overworld - The Spire Island";
        entrance.triggerOnTouch = true;

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void SetupOverworldScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");

        // Clean out any old BaseMap or old pieces
        var oldBase = GameObject.Find("--- MESHY OVERWORLD ROOT ---");
        if (oldBase != null) Object.DestroyImmediate(oldBase);

        var root = new GameObject("--- MESHY OVERWORLD ROOT ---");
        root.transform.position = Vector3.zero;

        // 1. Instantiate Base Map Floor
        GameObject floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}BaseMap_Floor.prefab");
        if (floorPrefab != null)
        {
            GameObject floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, root.transform);
            floor.name = "Meshy_BaseMap_Floor";
            floor.transform.position = Vector3.zero;
            // Scale up to expansive overworld island size (e.g. 50x to create full playable continent)
            floor.transform.localScale = new Vector3(45f, 25f, 45f);
        }

        // 2. Instantiate Doorless Exterior Shop Building
        GameObject shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}Shop_Building_Exterior.prefab");
        if (shopPrefab != null)
        {
            GameObject shop = (GameObject)PrefabUtility.InstantiatePrefab(shopPrefab, root.transform);
            shop.name = "Shop_Building_Exterior";
            shop.transform.position = new Vector3(-6.5f, 1.8f, 8.5f);
            shop.transform.localScale = Vector3.one * 3.5f;
            shop.transform.rotation = Quaternion.Euler(0, 140f, 0);

            // Add SceneEntranceMarker for returning from interior
            var marker = new GameObject("Overworld_ShopDoor");
            marker.transform.SetParent(shop.transform);
            marker.transform.localPosition = new Vector3(0f, 0.2f, -1.8f);
            var entranceMarker = marker.AddComponent<SceneEntranceMarker>();
            entranceMarker.markerId = "Overworld_ShopDoor";
            entranceMarker.facingYaw = 180f;
        }

        // 3. Instantiate Repeatable Props (Market Booth, Stairs, Trees, Boulders, Bushes)
        PopulateRepeatableProps(root.transform);

        // 4. Ensure Player and Camera setup
        GameObject player = GameObject.Find("Player_Hero");
        if (player != null)
        {
            player.transform.position = new Vector3(-6.5f, 2.0f, 4.0f);
            player.transform.rotation = Quaternion.identity;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[OVERWORLD BUILDER] OverworldMap setup completed with BaseMap Floor and Doorless Shop Exterior.");
    }

    private static void PopulateRepeatableProps(Transform root)
    {
        var propsGroup = new GameObject("Repeatable_Props").transform;
        propsGroup.SetParent(root);

        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}Fantasy_Tree.prefab");
        GameObject boulderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}Cliff_Boulder.prefab");
        GameObject boothPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}Market_Booth.prefab");
        GameObject stairsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}Stone_Stairs.prefab");
        GameObject bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}Foliage_Bush.prefab");

        // Repeatable Trees along grassy slopes
        Vector3[] treePositions = new Vector3[] {
            new Vector3(-18f, 3.2f, 14f),
            new Vector3(-22f, 4.0f, 6f),
            new Vector3(-12f, 2.5f, -8f),
            new Vector3(14f, 2.8f, 12f),
            new Vector3(20f, 4.5f, 4f),
            new Vector3(16f, 3.5f, -10f),
            new Vector3(-5f, 2.0f, -18f)
        };
        for (int i = 0; i < treePositions.Length; i++)
        {
            if (treePrefab != null)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, propsGroup);
                tree.name = $"Tree_{i + 1}";
                tree.transform.position = treePositions[i];
                tree.transform.localScale = Vector3.one * Random.Range(3.5f, 5.0f);
                tree.transform.rotation = Quaternion.Euler(-90f, Random.Range(0, 360f), 0);
            }
        }

        // Repeatable Boulders along rocky mountain edges & cave
        Vector3[] boulderPositions = new Vector3[] {
            new Vector3(-24f, 4.5f, 18f),
            new Vector3(22f, 5.0f, 16f),
            new Vector3(26f, 6.2f, -4f),
            new Vector3(0f, 6.8f, 28f), // High mountain peak
            new Vector3(-16f, 3.0f, -22f)
        };
        for (int i = 0; i < boulderPositions.Length; i++)
        {
            if (boulderPrefab != null)
            {
                var boulder = (GameObject)PrefabUtility.InstantiatePrefab(boulderPrefab, propsGroup);
                boulder.name = $"Boulder_{i + 1}";
                boulder.transform.position = boulderPositions[i];
                boulder.transform.localScale = Vector3.one * Random.Range(3.0f, 5.5f);
                boulder.transform.rotation = Quaternion.Euler(Random.Range(-10, 10), Random.Range(0, 360f), Random.Range(-10, 10));
            }
        }

        // Repeatable Market Booths in the Town Square
        if (boothPrefab != null)
        {
            var booth1 = (GameObject)PrefabUtility.InstantiatePrefab(boothPrefab, propsGroup);
            booth1.name = "Market_Booth_Potions";
            booth1.transform.position = new Vector3(2.5f, 1.8f, 7.5f);
            booth1.transform.localScale = Vector3.one * 2.8f;
            booth1.transform.rotation = Quaternion.Euler(0, 210f, 0);

            var booth2 = (GameObject)PrefabUtility.InstantiatePrefab(boothPrefab, propsGroup);
            booth2.name = "Market_Booth_Bakery";
            booth2.transform.position = new Vector3(7.0f, 1.8f, 4.0f);
            booth2.transform.localScale = Vector3.one * 2.8f;
            booth2.transform.rotation = Quaternion.Euler(0, 250f, 0);
        }

        // Repeatable Stone Stairs connecting tier elevations
        if (stairsPrefab != null)
        {
            var stairs1 = (GameObject)PrefabUtility.InstantiatePrefab(stairsPrefab, propsGroup);
            stairs1.name = "Stairs_Plaza_To_HighRidge";
            stairs1.transform.position = new Vector3(-1.0f, 1.5f, 16.0f);
            stairs1.transform.localScale = Vector3.one * 3.2f;
            stairs1.transform.rotation = Quaternion.Euler(0, 0f, 0);
        }

        // Repeatable Bushes around shop and paths
        Vector3[] bushPositions = new Vector3[] {
            new Vector3(-4.0f, 1.8f, 6.8f),
            new Vector3(-9.2f, 1.8f, 10.2f),
            new Vector3(4.8f, 1.8f, 5.5f),
            new Vector3(-0.5f, 1.8f, 1.2f),
            new Vector3(1.8f, 1.8f, 12.0f)
        };
        for (int i = 0; i < bushPositions.Length; i++)
        {
            if (bushPrefab != null)
            {
                var bush = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, propsGroup);
                bush.name = $"Bush_{i + 1}";
                bush.transform.position = bushPositions[i];
                bush.transform.localScale = Vector3.one * Random.Range(2.0f, 3.2f);
                bush.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            }
        }
    }

    private static void SetupInteriorScene()
    {
        string scenePath = "Assets/Scenes/ShopInterior.unity";
        Scene interiorScene;

        if (File.Exists(scenePath))
        {
            interiorScene = EditorSceneManager.OpenScene(scenePath);
        }
        else
        {
            interiorScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // Clean root objects
        foreach (var r in interiorScene.GetRootGameObjects())
        {
            Object.DestroyImmediate(r);
        }

        var root = new GameObject("--- SHOP INTERIOR ROOT ---");
        root.transform.position = Vector3.zero;

        // 1. Camera & AudioListener
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.06f, 0.05f);
        cam.fieldOfView = 50f;
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0f, 2.8f, -4.5f);
        camObj.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

        // 2. Interior Lighting (Warm cozy tavern/alchemy shop lighting)
        GameObject lightObj = new GameObject("Interior_WarmLight");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.0f, 0.82f, 0.62f);
        light.intensity = 2.8f;
        light.range = 14f;
        light.shadows = LightShadows.Soft;
        lightObj.transform.position = new Vector3(0f, 2.5f, 0.5f);

        // Fireplace hearth accent light
        GameObject hearthLight = new GameObject("Hearth_EmberLight");
        var hLight = hearthLight.AddComponent<Light>();
        hLight.type = LightType.Point;
        hLight.color = new Color(1.0f, 0.45f, 0.15f);
        hLight.intensity = 3.5f;
        hLight.range = 8f;
        hearthLight.transform.position = new Vector3(1.2f, 1.0f, 1.8f);

        // 3. Shop Building Interior Asset
        GameObject interiorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_DIR}Shop_Building_Interior.prefab");
        if (interiorPrefab != null)
        {
            GameObject interior = (GameObject)PrefabUtility.InstantiatePrefab(interiorPrefab, root.transform);
            interior.name = "Shop_Building_Interior";
            interior.transform.position = Vector3.zero;
            interior.transform.localScale = Vector3.one * 4.0f;

            // SceneEntranceMarker where player appears upon entry
            GameObject spawnMarker = new GameObject("Interior_Doorway");
            spawnMarker.transform.SetParent(interior.transform);
            spawnMarker.transform.localPosition = new Vector3(0f, 0.1f, -0.6f);
            var entrance = spawnMarker.AddComponent<SceneEntranceMarker>();
            entrance.markerId = "Interior_Doorway";
            entrance.facingYaw = 0f;
        }

        // 4. SceneTransitionManager
        GameObject stm = new GameObject("SceneTransitionManager");
        stm.AddComponent<SceneTransitionManager>();

        EditorSceneManager.SaveScene(interiorScene, scenePath);
        Debug.Log($"[OVERWORLD BUILDER] ShopInterior scene created and saved at '{scenePath}'.");

        // Ensure ShopInterior is in EditorBuildSettings
        AddSceneToBuildSettings(scenePath);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var currentScenes = EditorBuildSettings.scenes;
        foreach (var s in currentScenes)
        {
            if (s.path == scenePath) return; // Already present
        }

        var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
        System.Array.Copy(currentScenes, newScenes, currentScenes.Length);
        newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log($"[BUILD SETTINGS] Added '{scenePath}' to EditorBuildSettings scenes.");
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// HighResFantasyWorldBuilder constructs a high-resolution fantasy world in Unity:
/// 1. Integrates Blender-modeled assets:
///    - FantasyStoneBridge (arched double-span stone bridge with lanterns)
///    - FantasyGothicArchway (gothic town/forest portal gateway)
///    - FantasyLushGrassClump (3D multi-blade grass tufts with blooming wildflowers)
///    - FantasyFloralPlanter (carved stone urns with trailing ivy and pink blossoms)
/// 2. Integrates Asset Store water shaders and VFX:
///    - Simple Water Shader URP (Water_mat_03 turquoise water with refraction and caustics)
///    - NamuFX Stylized Water Effects (splashes, bubbles, ripples)
/// 3. Builds an Enchanted River flowing between Solas Town Hub and Mistwood Forest
/// 4. Scatters hundreds of 3D grass clumps, wildflowers, and ambient lively pollen/firefly particles
/// </summary>
public static class HighResFantasyWorldBuilder
{
    private const string PREFAB_DIR = "Assets/Prefabs/Environment/";
    private const string WATER_MAT_PATH = "Assets/IgniteCoders/Simple Water Shader/Resources/Water_mat_03.mat";
    private const string WATER_SPLASH_PATH = "Assets/NamuFX/StylizedWaterEffects/Prefabs/Water_Splash_Multiple.prefab";
    private const string WATER_BUBBLE_PATH = "Assets/NamuFX/StylizedWaterEffects/Prefabs/Bubbles_Vertical_Loop.prefab";

    [MenuItem("Tools/BattleSummoner/Build High-Res Fantasy World & Grassy Environment")]
    public static void BuildWorld()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "OverworldMap")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
        }

        // Configure Camera URP depth texture if needed
        ConfigureMainCamera();

        // Load Asset Store materials and Blender assets
        var waterMat = AssetDatabase.LoadAssetAtPath<Material>(WATER_MAT_PATH);
        var bridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "FantasyStoneBridge.fbx");
        var archwayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "FantasyGothicArchway.fbx");
        var grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "FantasyLushGrassClump.fbx");
        var planterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_DIR + "FantasyFloralPlanter.fbx");

        var cobblestoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_Cobblestone.mat");
        var grassGroundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_ForestGrass.mat");
        var stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_DA_CarvedStone.mat");
        var brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_DA_AntiqueBrass.mat");
        var ironMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_DA_WroughtIron.mat");
        var flameMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_DA_LanternFlame.mat");
        var foliageMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_DA_Foliage.mat");
        var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_DA_Bark.mat");
        var blueFlowerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_NPC_CherylVest.mat");
        var roseFlowerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_NPC_MargoDress.mat");

        // Clean existing root
        var existingRoot = GameObject.Find("HighResFantasyWorld_Root");
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        var root = new GameObject("HighResFantasyWorld_Root");
        Undo.RegisterCreatedObjectUndo(root, "Build High-Res Fantasy World");

        // 1. Upgrade Town Fountain with Asset Store Water & Planters
        UpgradeTownFountain(waterMat, planterPrefab, root.transform);

        // 2. Build Enchanted River between Town and Forest
        BuildEnchantedRiver(waterMat, cobblestoneMat, stoneMat, root.transform);

        // 3. Place Blender Fantasy Stone Bridge over River
        if (bridgePrefab != null)
        {
            BuildFantasyStoneBridge(bridgePrefab, planterPrefab, stoneMat, cobblestoneMat, flameMat, barkMat, foliageMat, roseFlowerMat, root.transform);
        }

        // 4. Place Blender Gothic Archway at Forest Portal
        if (archwayPrefab != null)
        {
            BuildFantasyGothicArchway(archwayPrefab, planterPrefab, stoneMat, ironMat, brassMat, barkMat, foliageMat, roseFlowerMat, root.transform);
        }

        // 5. Scatter 3D Lush Grass Clumps & Blooming Wildflowers
        if (grassPrefab != null)
        {
            ScatterLushGrassAndFlora(grassPrefab, foliageMat, blueFlowerMat, brassMat, root.transform);
        }

        // 6. Add Town Planters along walkways
        if (planterPrefab != null)
        {
            PlaceTownFloralPlanters(planterPrefab, stoneMat, barkMat, foliageMat, roseFlowerMat, root.transform);
        }

        // 7. Add Ambient Lively Pollen / Firefly Particle System
        CreateLivelyPollenVFX(root.transform);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("✨ [HIGH-RES FANTASY WORLD] World created with Blender Stone Bridge, Gothic Archway, 3D Grass Clumps, Asset Store Water & Lively Flora!");
    }

    private static void ConfigureMainCamera()
    {
        var camGo = GameObject.Find("Main Camera");
        if (camGo != null)
        {
            var data = camGo.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data == null)
            {
                data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            }
            data.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.On;
            data.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.On;
            data.renderPostProcessing = true;
        }
    }

    private static void UpgradeTownFountain(Material waterMat, GameObject planterPrefab, Transform rootParent)
    {
        var fountain = GameObject.Find("TownFountain");
        if (fountain == null) return;

        // Apply water material
        if (waterMat != null)
        {
            foreach (var r in fountain.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.gameObject.name == "EnchantedWaterSurface" || r.gameObject.name == "UpperWater")
                {
                    r.sharedMaterial = waterMat;
                }
            }
        }

        // Add Water Particles
        var splashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WATER_SPLASH_PATH);
        var bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WATER_BUBBLE_PATH);

        if (splashPrefab != null && fountain.transform.Find("FountainSplashVFX") == null)
        {
            var splash = (GameObject)PrefabUtility.InstantiatePrefab(splashPrefab, fountain.transform);
            splash.name = "FountainSplashVFX";
            splash.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            splash.transform.localScale = Vector3.one * 0.75f;
        }

        if (bubblePrefab != null && fountain.transform.Find("FountainBubblesVFX") == null)
        {
            var bubbles = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab, fountain.transform);
            bubbles.name = "FountainBubblesVFX";
            bubbles.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            bubbles.transform.localScale = Vector3.one * 0.8f;
        }

        // Place 4 Blender Floral Planters at the 4 diagonal corners of the fountain plaza
        if (planterPrefab != null)
        {
            Vector3 fPos = fountain.transform.position;
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-4.8f, 0f, -4.8f),
                new Vector3(4.8f, 0f, -4.8f),
                new Vector3(-4.8f, 0f, 4.8f),
                new Vector3(4.8f, 0f, 4.8f)
            };

            var fPlantersGroup = new GameObject("Fountain_FloralPlanters");
            fPlantersGroup.transform.parent = fountain.transform;
            fPlantersGroup.transform.localPosition = Vector3.zero;

            foreach (var off in offsets)
            {
                var planter = (GameObject)PrefabUtility.InstantiatePrefab(planterPrefab, fPlantersGroup.transform);
                planter.name = "Planter_Urn";
                planter.transform.position = fPos + off;
                planter.transform.localScale = Vector3.one * 0.9f;
                AddBoxCollider(planter, new Vector3(1.2f, 1.6f, 1.2f), new Vector3(0f, 0.8f, 0f));
            }
        }
    }

    private static void BuildEnchantedRiver(Material waterMat, Material bedMat, Material stoneMat, Transform root)
    {
        var riverGroup = new GameObject("EnchantedRiver");
        riverGroup.transform.parent = root;

        // River parameters: along X = 12.0, from Z = -38 to +38, width = 8.0m
        float riverX = 12.0f;
        float riverZCenter = 0.0f;
        float riverLength = 76.0f;
        float riverWidth = 8.0f;

        // 1. Riverbed (Sunken ground channel)
        var riverbed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        riverbed.name = "Riverbed_Channel";
        riverbed.transform.parent = riverGroup.transform;
        riverbed.transform.position = new Vector3(riverX, -0.65f, riverZCenter);
        riverbed.transform.localScale = new Vector3(riverWidth + 1.2f, 0.6f, riverLength);
        if (bedMat != null) riverbed.GetComponent<MeshRenderer>().sharedMaterial = bedMat;

        // 2. Water Surface Plane (Using Asset Store Water Shader)
        var waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "EnchantedRiver_WaterSurface";
        waterPlane.transform.parent = riverGroup.transform;
        waterPlane.transform.position = new Vector3(riverX, -0.12f, riverZCenter);
        // Unity plane is 10x10 meters default
        waterPlane.transform.localScale = new Vector3(riverWidth / 10.0f, 1.0f, riverLength / 10.0f);
        if (waterMat != null) waterPlane.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
        // Water planes don't block walking, remove mesh collider or set trigger
        var planeCol = waterPlane.GetComponent<Collider>();
        if (planeCol != null) Object.DestroyImmediate(planeCol);

        // 3. Riverbanks: Natural river stones and berms along both edges
        var riverbanksGroup = new GameObject("Riverbanks_RocksAndBerms");
        riverbanksGroup.transform.parent = riverGroup.transform;

        Random.InitState(777);
        float[] bankX = new float[] { riverX - (riverWidth * 0.5f + 0.3f), riverX + (riverWidth * 0.5f + 0.3f) };

        for (int i = 0; i < 36; i++)
        {
            float z = -36.0f + (72.0f * i / 35.0f);
            // Skip the bridge crossing zone (Z between -18 and -10)
            if (z >= -19.0f && z <= -9.0f) continue;

            foreach (var bx in bankX)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "RiverRock_" + i;
                rock.transform.parent = riverbanksGroup.transform;
                float rScaleX = Random.Range(0.6f, 1.4f);
                float rScaleY = Random.Range(0.35f, 0.7f);
                float rScaleZ = Random.Range(0.6f, 1.5f);
                float xJitter = Random.Range(-0.4f, 0.4f);
                rock.transform.position = new Vector3(bx + xJitter, -0.1f + rScaleY * 0.3f, z + Random.Range(-0.3f, 0.3f));
                rock.transform.localScale = new Vector3(rScaleX, rScaleY, rScaleZ);
                rock.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), 0);
                if (stoneMat != null) rock.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            }
        }

        // 4. Water ripple / splash particle effects along the river
        var splashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WATER_SPLASH_PATH);
        if (splashPrefab != null)
        {
            for (int s = 0; s < 4; s++)
            {
                float sz = -30.0f + s * 18.0f;
                if (sz >= -18f && sz <= -10f) sz += 6.0f;
                var splash = (GameObject)PrefabUtility.InstantiatePrefab(splashPrefab, riverGroup.transform);
                splash.name = "RiverSplashFX_" + s;
                splash.transform.position = new Vector3(riverX + Random.Range(-1.5f, 1.5f), -0.1f, sz);
                splash.transform.localScale = Vector3.one * 0.5f;
            }
        }
    }

    private static void BuildFantasyStoneBridge(GameObject bridgePrefab, GameObject planterPrefab, Material stoneMat, Material cobblestoneMat, Material flameMat, Material barkMat, Material foliageMat, Material roseFlowerMat, Transform root)
    {
        var existing = GameObject.Find("FantasyStoneBridge_Instance");
        if (existing != null) Object.DestroyImmediate(existing);

        var bridge = (GameObject)PrefabUtility.InstantiatePrefab(bridgePrefab, root);
        bridge.name = "FantasyStoneBridge_Instance";
        bridge.transform.position = new Vector3(12.0f, -0.18f, -14.0f);
        bridge.transform.rotation = Quaternion.identity;
        bridge.transform.localScale = Vector3.one;

        // Ensure colliders & materials on bridge
        var renderers = bridge.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            if (r.gameObject.name.Contains("FantasyStoneBridge") || r.gameObject.name.Contains("Structure"))
            {
                var mc = r.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = r.gameObject.AddComponent<MeshCollider>();
                mc.convex = false;
                if (cobblestoneMat != null) r.sharedMaterial = cobblestoneMat;
            }
            else if (r.gameObject.name.StartsWith("Post_") || r.gameObject.name.StartsWith("Parapet_"))
            {
                if (stoneMat != null) r.sharedMaterial = stoneMat;
                if (r.gameObject.name.StartsWith("Post_"))
                {
                    var bc = r.gameObject.GetComponent<BoxCollider>();
                    if (bc == null) bc = r.gameObject.AddComponent<BoxCollider>();
                }
            }
            else if (r.gameObject.name.StartsWith("Lantern_"))
            {
                if (flameMat != null) r.sharedMaterial = flameMat;
            }
        }

        // Bridge Warm Ambient Lights at Lanterns
        var lightGroup = new GameObject("Bridge_LanternLights");
        lightGroup.transform.parent = bridge.transform;
        lightGroup.transform.localPosition = Vector3.zero;

        Vector3[] lanternOffsets = new Vector3[]
        {
            new Vector3(0.0f, 2.6f, -3.15f),
            new Vector3(0.0f, 2.6f,  3.15f)
        };

        foreach (var lo in lanternOffsets)
        {
            var lObj = new GameObject("Bridge_LanternLight");
            lObj.transform.parent = lightGroup.transform;
            lObj.transform.localPosition = lo;
            var l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.88f, 0.55f);
            l.range = 8.5f;
            l.intensity = 1.8f;
        }

        // Add 4 Blender Floral Planters at the 4 approach corners of the bridge
        if (planterPrefab != null)
        {
            Vector3[] approachOffsets = new Vector3[]
            {
                new Vector3(-9.2f, 0.0f, -3.8f),
                new Vector3(-9.2f, 0.0f,  3.8f),
                new Vector3( 9.2f, 0.0f, -3.8f),
                new Vector3( 9.2f, 0.0f,  3.8f)
            };

            for (int i = 0; i < approachOffsets.Length; i++)
            {
                var planter = (GameObject)PrefabUtility.InstantiatePrefab(planterPrefab, bridge.transform);
                planter.name = "BridgeApproachPlanter_" + i;
                planter.transform.localPosition = approachOffsets[i];
                planter.transform.localScale = Vector3.one * 0.95f;
                AddBoxCollider(planter, new Vector3(1.2f, 1.6f, 1.2f), new Vector3(0f, 0.8f, 0f));
                ApplyPlanterMaterials(planter, stoneMat, barkMat, foliageMat, roseFlowerMat);
            }
        }

        // Retune Path_TownToForest so it seamlessly aligns with bridge ramps
        var townToForestPath = GameObject.Find("Path_TownToForest");
        if (townToForestPath != null)
        {
            townToForestPath.transform.position = new Vector3(12.0f, -0.25f, -14.0f);
        }
    }

    private static void BuildFantasyGothicArchway(GameObject archwayPrefab, GameObject planterPrefab, Material stoneMat, Material ironMat, Material brassMat, Material barkMat, Material foliageMat, Material roseFlowerMat, Transform root)
    {
        var existing = GameObject.Find("FantasyGothicArchway_Instance");
        if (existing != null) Object.DestroyImmediate(existing);

        var arch = (GameObject)PrefabUtility.InstantiatePrefab(archwayPrefab, root);
        arch.name = "FantasyGothicArchway_Instance";
        arch.transform.position = new Vector3(21.8f, -0.05f, -14.0f);
        arch.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        arch.transform.localScale = Vector3.one * 0.85f;

        // Apply Archway Materials & Colliders
        var renderers = arch.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            if (r.gameObject.name.Contains("Tower") || r.gameObject.name.Contains("CrossBeam"))
            {
                if (stoneMat != null) r.sharedMaterial = stoneMat;
            }
            else if (r.gameObject.name.Contains("Spire"))
            {
                if (ironMat != null) r.sharedMaterial = ironMat;
            }
            else if (r.gameObject.name.Contains("Crest"))
            {
                if (brassMat != null) r.sharedMaterial = brassMat;
            }
        }

        var towerLeft = arch.transform.Find("Arch_Tower_-1");
        if (towerLeft != null && towerLeft.GetComponent<BoxCollider>() == null)
        {
            towerLeft.gameObject.AddComponent<BoxCollider>();
        }
        var towerRight = arch.transform.Find("Arch_Tower_1");
        if (towerRight != null && towerRight.GetComponent<BoxCollider>() == null)
        {
            towerRight.gameObject.AddComponent<BoxCollider>();
        }

        // Flank with 2 Planters
        if (planterPrefab != null)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var p = (GameObject)PrefabUtility.InstantiatePrefab(planterPrefab, arch.transform);
                p.name = "Arch_FlankPlanter_" + s;
                p.transform.localPosition = new Vector3(s * 3.8f, 0f, 0f);
                p.transform.localScale = Vector3.one * 0.85f;
                ApplyPlanterMaterials(p, stoneMat, barkMat, foliageMat, roseFlowerMat);
            }
        }
    }

    private static void ScatterLushGrassAndFlora(GameObject grassPrefab, Material foliageMat, Material blueFlowerMat, Material goldFlowerMat, Transform root)
    {
        var floraGroup = new GameObject("Lush3DGrassAndFlora_Scatter");
        floraGroup.transform.parent = root;

        Random.InitState(12345);

        // 1. Forest Meadow Scatter (Dense 3D grass tufts with blooming wildflowers)
        int meadowClumps = 140;
        for (int i = 0; i < meadowClumps; i++)
        {
            float gx = Random.Range(16.0f, 54.0f);
            float gz = Random.Range(-34.0f, 34.0f);

            if (gz >= -16.0f && gz <= -12.0f && gx < 38.0f) continue;
            if (gx >= 33.0f && gx <= 37.0f && gz >= 0.0f) continue;

            var clump = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, floraGroup.transform);
            clump.name = "GrassClump_Meadow_" + i;
            float scale = Random.Range(0.85f, 1.35f);
            clump.transform.position = new Vector3(gx, -0.05f, gz);
            clump.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            clump.transform.localScale = Vector3.one * scale;
            ApplyGrassMaterials(clump, foliageMat, blueFlowerMat, goldFlowerMat);
        }

        // 2. Riverbank Grassy Tufts (clustering near the water's edge)
        int riverbankClumps = 60;
        float riverX = 12.0f;
        for (int i = 0; i < riverbankClumps; i++)
        {
            float side = (Random.value < 0.5f) ? -1f : 1f;
            float gx = riverX + side * Random.Range(4.2f, 6.5f);
            float gz = Random.Range(-35.0f, 35.0f);

            if (gz >= -18.0f && gz <= -10.0f) continue;

            var clump = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, floraGroup.transform);
            clump.name = "GrassClump_Riverbank_" + i;
            float scale = Random.Range(0.9f, 1.4f);
            clump.transform.position = new Vector3(gx, -0.05f, gz);
            clump.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            clump.transform.localScale = Vector3.one * scale;
            ApplyGrassMaterials(clump, foliageMat, blueFlowerMat, goldFlowerMat);
        }

        // 3. Town Greenery Accents (patches of grass softening town cobblestone borders)
        int townClumps = 35;
        for (int i = 0; i < townClumps; i++)
        {
            float side = (Random.value < 0.5f) ? -1f : 1f;
            float gx = Random.Range(-35.0f, 8.0f);
            float gz = (side < 0) ? Random.Range(-36.0f, -32.0f) : Random.Range(8.0f, 12.0f);

            var clump = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, floraGroup.transform);
            clump.name = "GrassClump_TownEdge_" + i;
            float scale = Random.Range(0.75f, 1.1f);
            clump.transform.position = new Vector3(gx, -0.05f, gz);
            clump.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            clump.transform.localScale = Vector3.one * scale;
            ApplyGrassMaterials(clump, foliageMat, blueFlowerMat, goldFlowerMat);
        }
    }

    private static void ApplyPlanterMaterials(GameObject planter, Material stoneMat, Material barkMat, Material foliageMat, Material blossomMat)
    {
        var renderers = planter.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            string n = r.gameObject.name;
            if (n.Contains("Planter") || n.Contains("Urn_Body") || n.Contains("Base"))
            {
                if (stoneMat != null) r.sharedMaterial = stoneMat;
            }
            else if (n.Contains("Soil"))
            {
                if (barkMat != null) r.sharedMaterial = barkMat;
            }
            else if (n.Contains("Icosphere") || n.Contains("fol"))
            {
                if (foliageMat != null) r.sharedMaterial = foliageMat;
            }
            else if (n.Contains("Sphere") || n.Contains("fl"))
            {
                if (blossomMat != null) r.sharedMaterial = blossomMat;
            }
        }
    }

    private static void ApplyGrassMaterials(GameObject clump, Material foliageMat, Material blueFlowerMat, Material goldFlowerMat)
    {
        var renderers = clump.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            string n = r.gameObject.name;
            if (n.Contains("Grass") || n.Contains("Mesh"))
            {
                if (foliageMat != null) r.sharedMaterial = foliageMat;
            }
            else if (n.StartsWith("Wildflower_"))
            {
                int idx = 0;
                int.TryParse(n.Replace("Wildflower_", ""), out idx);
                r.sharedMaterial = (idx % 2 == 0) ? blueFlowerMat : goldFlowerMat;
            }
        }
    }

    private static void PlaceTownFloralPlanters(GameObject planterPrefab, Material stoneMat, Material barkMat, Material foliageMat, Material roseFlowerMat, Transform root)
    {
        var planterGroup = new GameObject("TownFloralPlanters");
        planterGroup.transform.parent = root;

        Vector3[] streetPlanterPositions = new Vector3[]
        {
            new Vector3(-24.5f, 0f, -8.0f),
            new Vector3(-19.5f, 0f, -8.0f),
            new Vector3(-24.5f, 0f, -20.0f),
            new Vector3(-19.5f, 0f, -20.0f),
            new Vector3(-6.5f, 0f, -8.0f),
            new Vector3(-1.5f, 0f, -8.0f),
            new Vector3(-6.5f, 0f, -20.0f),
            new Vector3(-1.5f, 0f, -20.0f),
            new Vector3(-13.0f, 0f, -4.5f),
            new Vector3(-13.0f, 0f, -23.5f),
            new Vector3(2.5f, 0f, -11.5f),
            new Vector3(2.5f, 0f, -16.5f)
        };

        for (int i = 0; i < streetPlanterPositions.Length; i++)
        {
            var p = (GameObject)PrefabUtility.InstantiatePrefab(planterPrefab, planterGroup.transform);
            p.name = "TownStreetPlanter_" + i;
            p.transform.position = streetPlanterPositions[i];
            p.transform.localScale = Vector3.one * 0.85f;
            AddBoxCollider(p, new Vector3(1.2f, 1.6f, 1.2f), new Vector3(0f, 0.8f, 0f));
            ApplyPlanterMaterials(p, stoneMat, barkMat, foliageMat, roseFlowerMat);
        }
    }

    private static void CreateLivelyPollenVFX(Transform root)
    {
        var vfxGo = new GameObject("LivelyPollenAndFireflies_VFX");
        vfxGo.transform.parent = root;
        vfxGo.transform.position = new Vector3(25.0f, 1.5f, 0.0f);

        var ps = vfxGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 7.0f;
        main.startSpeed = 0.35f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.95f, 0.90f, 0.40f, 0.85f), // Golden firefly
            new Color(0.45f, 0.85f, 0.95f, 0.75f)  // Arcane cyan spore
        );
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 16.0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(50.0f, 3.5f, 60.0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.18f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.20f, 0.20f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.25f), new GradientAlphaKey(0.8f, 0.75f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var renderer = vfxGo.GetComponent<ParticleSystemRenderer>();
        var urpParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpParticleShader != null)
        {
            var pMat = new Material(urpParticleShader);
            pMat.name = "Mat_LivelyPollen_URP";
            renderer.sharedMaterial = pMat;
        }
    }

    private static void AddBoxCollider(GameObject go, Vector3 size, Vector3 center)
    {
        var bc = go.GetComponent<BoxCollider>();
        if (bc == null) bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.center = center;
    }

    [MenuItem("Tools/BattleSummoner/Capture High-Res World Screenshots")]
    public static void CaptureScreenshots()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "OverworldMap")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
        }

        var dir = "Assets/Screenshots";
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

        var cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            Debug.LogError("No camera found in scene!");
            return;
        }

        var urpData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (urpData == null) urpData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urpData.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.On;
        urpData.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.On;
        urpData.renderPostProcessing = true;

        var arpgCam = cam.GetComponent<PortraitARPGCamera>();
        if (arpgCam != null) arpgCam.enabled = false;

        var shots = new (string name, Vector3 pos, Vector3 target)[]
        {
            // 1. Floating above the Enchanted River looking south at the arched stone bridge
            ("HighRes_BlenderBridge_And_River", new Vector3(12.0f, 4.2f, -2.5f), new Vector3(12.0f, 1.2f, -14.0f)),
            // 2. Standing on the bridge deck looking east through the Gothic Archway into the forest
            ("HighRes_GothicArchway_Portal", new Vector3(5.0f, 3.0f, -14.0f), new Vector3(21.8f, 2.5f, -14.0f)),
            // 3. Close-up meadow view looking across the lush 3D grass clumps and blooming wildflowers
            ("HighRes_Forest_GrassyMeadow", new Vector3(21.0f, 2.0f, -8.0f), new Vector3(26.0f, 0.4f, -4.0f)),
            // 4. Standing on the promenade walkway looking at the town fountain with water shader
            ("HighRes_TownFountain_AssetStoreWater", new Vector3(-13.0f, 3.8f, -9.0f), new Vector3(-13.0f, 1.0f, -14.0f))
        };

        int width = 1280;
        int height = 720;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        var prevTarget = cam.targetTexture;
        var prevPos = cam.transform.position;
        var prevRot = cam.transform.rotation;

        cam.targetTexture = rt;

        foreach (var s in shots)
        {
            cam.transform.position = s.pos;
            cam.transform.LookAt(s.target);
            cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] bytes = tex.EncodeToPNG();
            string path = System.IO.Path.Combine(dir, s.name + ".png");
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log("📸 Saved screenshot: " + path);
        }

        cam.targetTexture = prevTarget;
        cam.transform.position = prevPos;
        cam.transform.rotation = prevRot;
        if (arpgCam != null) arpgCam.enabled = true;

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);

        AssetDatabase.Refresh();
    }
}


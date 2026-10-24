#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Core;
using BattleSummoner.Environment;

[InitializeOnLoad]
public static class TowerOfGlyphsSetup
{
    private const string SETUP_COMPLETE_KEY = "TowerOfGlyphs_SetupExecuted_v2";

    static TowerOfGlyphsSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(SETUP_COMPLETE_KEY, false))
            {
                SessionState.SetBool(SETUP_COMPLETE_KEY, true);
                ExecuteFullSetup();
            }
        };
    }

    [MenuItem("Tower of Glyphs/Run Full Project Setup & Validation")]
    public static void ExecuteFullSetup()
    {
        Debug.Log("[Tower of Glyphs] Beginning automated project configuration (Full ARPG Subsystems)...");

        ConfigureDirectories();
        ConfigureProjectSettings();
        ConfigureURPAsset();
        EnforceHighQualityPortraitGameView();
        CreateBespokeMaterials();
        CreateBespokeNPCDataAssets();
        CreatePrefabs();
        ConfigureSampleSceneCamera();
        BuildMainDungeonScene();
        BuildRelicTestingScene();
        BuildMultiCharacterDialogueScene();
        UpdateBuildSettingsScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Tower of Glyphs] Full setup completed successfully! Android portrait ARPG ready.");
    }

    private static void ConfigureDirectories()
    {
        string[] dirs = {
            "Assets/Plugins/Android",
            "Assets/Prefabs/UI",
            "Assets/Prefabs/Combat",
            "Assets/Prefabs/Relics",
            "Assets/Prefabs/Mining",
            "Assets/Prefabs/NPCs",
            "Assets/Scripts/Core",
            "Assets/Scripts/Camera",
            "Assets/Scripts/Input",
            "Assets/Scripts/Combat",
            "Assets/Scripts/Progression",
            "Assets/Scripts/Mining",
            "Assets/Scripts/NPC",
            "Assets/Scripts/Social",
            "Assets/Scripts/Monetization",
            "Assets/Scripts/Crafting",
            "Assets/Scripts/Inspection",
            "Assets/Scripts/UI",
            "Assets/Scenes",
            "Assets/Settings",
            "Assets/Materials",
            "Assets/Editor"
        };

        foreach (var d in dirs)
        {
            if (!Directory.Exists(d))
            {
                Directory.CreateDirectory(d);
            }
        }
    }

    private static void ConfigureProjectSettings()
    {
        PlayerSettings.productName = "Tower of Glyphs";
        PlayerSettings.companyName = "Glyph Studio";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.glyphstudio.towerofglyphs");

        // Strict portrait orientation locking
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        // Android target architectures: ARM64 = true, ARMv7 = false
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

        // Scripting backend IL2CPP and Release compiler
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

        // Target Graphics APIs: Vulkan primary, GLES3 fallback
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new GraphicsDeviceType[] {
            GraphicsDeviceType.Vulkan,
            GraphicsDeviceType.OpenGLES3
        });

        // Texture compression: ASTC
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        // Disable Unity Remote ADB check to prevent CommandInvokationFailure when no device is plugged in
        EditorSettings.unityRemoteDevice = "None";
    }

    private static void ConfigureURPAsset()
    {
        string targetUrpPath = "Assets/Settings/Mobile_RPAsset.asset";
        UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(targetUrpPath);

        if (urpAsset != null)
        {
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;
        }
    }

    [MenuItem("Tower of Glyphs/Enforce High-Quality Portrait Resolution")]
    public static void EnforceHighQualityPortraitGameView()
    {
        try
        {
            var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gvType, false, null, false);
            if (window == null) return;

            var lowResProp = gvType.GetProperty("lowResolutionForAspectRatios", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            lowResProp?.SetValue(window, false);

            var prop = gvType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var sizesInstance = instanceProp?.GetValue(null);

            var currentGroupProp = sizesType.GetProperty("currentGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var group = currentGroupProp?.GetValue(sizesInstance);

            var getDisplayTextsMethod = group?.GetType().GetMethod("GetDisplayTexts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var texts = getDisplayTextsMethod?.Invoke(group, null) as string[];

            int targetIndex = 5; // default 1920x1080 Portrait
            if (texts != null)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].Contains("1920x1080 Portrait") || texts[i].Contains("1080x1920"))
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }

            prop?.SetValue(window, targetIndex);
            window.Repaint();
            Debug.Log($"[Tower of Glyphs] GameView locked to crisp High-Res Portrait (Index: {targetIndex}, LowRes: false).");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Tower of Glyphs] EnforceHighQualityPortraitGameView notice: {ex.Message}");
        }
    }

    public static Material GetOrCreateMaterial(string matName, Color baseColor, float metallic = 0f, float smoothness = 0.5f, Color? emissionColor = null, bool unlit = false)
    {
        string dir = "Assets/Materials";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string path = $"{dir}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        string shaderName = unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit";
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader && shader != null)
        {
            mat.shader = shader;
        }

        mat.SetColor("_BaseColor", baseColor);
        if (!unlit)
        {
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emissionColor.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor.Value);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    [MenuItem("Tower of Glyphs/Create Bespoke Materials")]
    public static void CreateBespokeMaterials()
    {
        Debug.Log("[Tower of Glyphs] Generating and serializing studio-grade PBR URP materials...");

        // Characters & NPCs
        GetOrCreateMaterial("Mat_NPC_Margo", new Color(0.24f, 0.08f, 0.32f, 1f), 0.25f, 0.78f, new Color(0.55f, 0.15f, 0.85f) * 0.9f);
        GetOrCreateMaterial("Mat_NPC_Barnaby", new Color(0.58f, 0.34f, 0.14f, 1f), 0.85f, 0.62f, new Color(1.0f, 0.4f, 0.08f) * 0.45f);
        GetOrCreateMaterial("Mat_NPC_Zephyr", new Color(0.10f, 0.42f, 0.38f, 1f), 0.30f, 0.72f, new Color(0.15f, 0.8f, 0.7f) * 0.45f);
        GetOrCreateMaterial("Mat_Witch_Hat", new Color(0.12f, 0.07f, 0.18f, 1f), 0.1f, 0.65f);
        GetOrCreateMaterial("Mat_Floating_Geode", new Color(0.6f, 0.15f, 0.85f, 1f), 0.35f, 0.92f, new Color(0.75f, 0.25f, 1.0f) * 2.5f);
        GetOrCreateMaterial("Mat_Anvil_Iron", new Color(0.16f, 0.17f, 0.19f, 1f), 0.92f, 0.65f);
        GetOrCreateMaterial("Mat_Scout_Leather", new Color(0.20f, 0.16f, 0.12f, 1f), 0.05f, 0.35f);
        GetOrCreateMaterial("Mat_Player_Hero", new Color(0.12f, 0.52f, 0.30f, 1f), 0.75f, 0.80f, new Color(0.2f, 0.8f, 0.4f) * 0.5f);
        GetOrCreateMaterial("Mat_SpireBoss_Aethelgard", new Color(0.52f, 0.08f, 0.12f, 1f), 0.88f, 0.70f, new Color(1.0f, 0.12f, 0.18f) * 2.2f);
        GetOrCreateMaterial("Mat_SpireGargoyle", new Color(0.20f, 0.22f, 0.28f, 1f), 0.15f, 0.42f, new Color(0.7f, 0.2f, 0.9f) * 0.5f);

        // Environment & Campsite
        GetOrCreateMaterial("Mat_Campfire_Floor", new Color(0.08f, 0.09f, 0.12f, 1f), 0.05f, 0.35f);
        GetOrCreateMaterial("Mat_Campfire_Stones", new Color(0.22f, 0.24f, 0.27f, 1f), 0.08f, 0.40f);
        GetOrCreateMaterial("Mat_Campfire_Embers", new Color(0.25f, 0.08f, 0.02f, 1f), 0.05f, 0.30f, new Color(1.0f, 0.42f, 0.04f) * 4.2f);
        GetOrCreateMaterial("Mat_Cavern_Pillars", new Color(0.12f, 0.13f, 0.18f, 1f), 0.35f, 0.55f);
        GetOrCreateMaterial("Mat_Cavern_Dome", new Color(0.05f, 0.06f, 0.08f, 1f), 0.02f, 0.20f);
        GetOrCreateMaterial("Mat_SpeechBubble_Glass", new Color(0.05f, 0.06f, 0.10f, 0.92f), 0.1f, 0.85f, null, true);
        GetOrCreateMaterial("Mat_Dungeon_Arena", new Color(0.12f, 0.12f, 0.15f, 1f), 0.15f, 0.45f);

        // Relics, Mining & VFX
        GetOrCreateMaterial("Mat_Relic_Cryptex_Core", new Color(1f, 0.84f, 0.1f, 1f), 0.85f, 0.75f, new Color(1f, 0.8f, 0.2f) * 0.6f);
        GetOrCreateMaterial("Mat_Relic_Cryptex_Ring", new Color(0.2f, 0.65f, 0.9f, 1f), 0.80f, 0.70f);
        GetOrCreateMaterial("Mat_Mining_Vein_Rock", new Color(0.16f, 0.17f, 0.20f, 1f), 0.10f, 0.35f);
        GetOrCreateMaterial("Mat_Mining_Vein_Crystal", new Color(0.1f, 0.75f, 0.95f, 1f), 0.40f, 0.90f, new Color(0.2f, 0.8f, 1.0f) * 1.8f);
        GetOrCreateMaterial("Mat_Geode_Shell", new Color(0.25f, 0.22f, 0.18f, 1f), 0.10f, 0.35f);
        GetOrCreateMaterial("Mat_Geode_Core", new Color(0.7f, 0.15f, 0.9f, 1f), 0.45f, 0.85f, new Color(0.8f, 0.2f, 1.0f) * 2.0f);
        GetOrCreateMaterial("Mat_Boss_Shockwave", new Color(1f, 0.2f, 0.5f, 0.8f), 0.2f, 0.6f, new Color(1f, 0.2f, 0.5f) * 1.5f);

        AssetDatabase.SaveAssets();
    }

    private static void AttachNPCVisualAccessories(GameObject npcObj, string name)
    {
        if (name == "NPC_Margo")
        {
            // Goth Witch Hat: brim and crown
            GameObject hatRoot = new GameObject("WitchHat");
            hatRoot.transform.SetParent(npcObj.transform, false);
            hatRoot.transform.localPosition = new Vector3(0f, 1.05f, 0f);

            GameObject brim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            brim.name = "HatBrim";
            brim.transform.SetParent(hatRoot.transform, false);
            brim.transform.localScale = new Vector3(1.3f, 0.04f, 1.3f);
            var bCol = brim.GetComponent<Collider>(); if (bCol != null) Object.DestroyImmediate(bCol);
            var bRend = brim.GetComponent<Renderer>();
            Material hatMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Witch_Hat.mat");
            if (bRend != null && hatMat != null) bRend.sharedMaterial = hatMat;

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "HatCrown";
            crown.transform.SetParent(hatRoot.transform, false);
            crown.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            crown.transform.localScale = new Vector3(0.6f, 0.40f, 0.6f);
            crown.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            var cCol = crown.GetComponent<Collider>(); if (cCol != null) Object.DestroyImmediate(cCol);
            var cRend = crown.GetComponent<Renderer>();
            if (cRend != null && hatMat != null) cRend.sharedMaterial = hatMat;

            // Floating Void Geode hovering near her hand
            GameObject geode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            geode.name = "FloatingVoidGeode";
            geode.transform.SetParent(npcObj.transform, false);
            geode.transform.localPosition = new Vector3(0.7f, 0.4f, 0.35f);
            geode.transform.localScale = new Vector3(0.35f, 0.45f, 0.32f);
            var gCol = geode.GetComponent<Collider>(); if (gCol != null) Object.DestroyImmediate(gCol);
            var gRend = geode.GetComponent<Renderer>();
            Material geodeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Floating_Geode.mat");
            if (gRend != null && geodeMat != null) gRend.sharedMaterial = geodeMat;

            GameObject geodeLightObj = new GameObject("GeodeGlow", typeof(Light));
            geodeLightObj.transform.SetParent(geode.transform, false);
            Light gLight = geodeLightObj.GetComponent<Light>();
            gLight.type = LightType.Point;
            gLight.color = new Color(0.85f, 0.3f, 1f);
            gLight.intensity = 1.8f;
            gLight.range = 3.5f;
        }
        else if (name == "NPC_Barnaby")
        {
            // Stout blacksmith silhouette
            npcObj.transform.localScale = new Vector3(1.15f, 0.95f, 1.15f);

            // Anvil beside him
            GameObject anvil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anvil.name = "BlacksmithAnvil";
            anvil.transform.SetParent(npcObj.transform, false);
            anvil.transform.localPosition = new Vector3(0.85f, -0.4f, 0.2f);
            anvil.transform.localScale = new Vector3(0.55f, 0.65f, 0.95f);
            var aCol = anvil.GetComponent<Collider>(); if (aCol != null) Object.DestroyImmediate(aCol);
            var aRend = anvil.GetComponent<Renderer>();
            Material anvilMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Anvil_Iron.mat");
            if (aRend != null && anvilMat != null) aRend.sharedMaterial = anvilMat;

            // Hammer on anvil
            GameObject hammerHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hammerHead.name = "ForgeHammer";
            hammerHead.transform.SetParent(anvil.transform, false);
            hammerHead.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            hammerHead.transform.localScale = new Vector3(0.45f, 0.22f, 0.35f);
            var hCol = hammerHead.GetComponent<Collider>(); if (hCol != null) Object.DestroyImmediate(hCol);
            var hRend = hammerHead.GetComponent<Renderer>();
            if (hRend != null && anvilMat != null) hRend.sharedMaterial = anvilMat;
        }
        else if (name == "NPC_Zephyr")
        {
            // Slender scout silhouette
            npcObj.transform.localScale = new Vector3(0.9f, 1.05f, 0.9f);

            // Scout Cowl / Shoulder Mantle
            GameObject cowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cowl.name = "ScoutCowl";
            cowl.transform.SetParent(npcObj.transform, false);
            cowl.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            cowl.transform.localScale = new Vector3(1.12f, 0.25f, 1.12f);
            var sCol = cowl.GetComponent<Collider>(); if (sCol != null) Object.DestroyImmediate(sCol);
            var sRend = cowl.GetComponent<Renderer>();
            Material cowlMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_NPC_Zephyr.mat");
            if (sRend != null && cowlMat != null) sRend.sharedMaterial = cowlMat;

            // Scout Quiver strapped on back
            GameObject quiver = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            quiver.name = "ScoutQuiver";
            quiver.transform.SetParent(npcObj.transform, false);
            quiver.transform.localPosition = new Vector3(0.18f, 0.2f, -0.48f);
            quiver.transform.localScale = new Vector3(0.24f, 0.85f, 0.24f);
            quiver.transform.localRotation = Quaternion.Euler(20f, 0f, 15f);
            var qCol = quiver.GetComponent<Collider>(); if (qCol != null) Object.DestroyImmediate(qCol);
            var qRend = quiver.GetComponent<Renderer>();
            Material leatherMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Scout_Leather.mat");
            if (qRend != null && leatherMat != null) qRend.sharedMaterial = leatherMat;
        }
    }

    private static void CreateBespokeNPCDataAssets()
    {
        // NPC 1: Margo
        string margoPath = "Assets/Settings/NPC_Margo_Data.asset";
        BespokeNPCObject margo = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>(margoPath);
        if (margo == null)
        {
            margo = ScriptableObject.CreateInstance<BespokeNPCObject>();
            margo.npcId = "npc_margo";
            margo.displayName = "Margo";
            margo.title = "Goth Occultist & Appraiser";
            margo.primaryRole = NPCRole.Alchemist;
            margo.quirk = NPCQuirk.HoarderOfGems;
            margo.systemPromptPersona = "You are Margo, a cynical, sweet-and-sour goth alchemist living in the Spire base camp. You love shiny dark geodes and wear striped arm-warmers. Speak with sharp wit, use subtle kaomojis, and complain about clumsy delvers.";
            margo.affinityLevel = 2;
            margo.combatAttackPower = 48f;
            margo.companionSupportCooldown = 10f;
            margo.expeditionSpeedMultiplier = 1.2f;
            margo.rareDiscoveryBonus = 0.25f;
            AssetDatabase.CreateAsset(margo, margoPath);
        }

        // NPC 2: Barnaby
        string barnabyPath = "Assets/Settings/NPC_Barnaby_Data.asset";
        BespokeNPCObject barnaby = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>(barnabyPath);
        if (barnaby == null)
        {
            barnaby = ScriptableObject.CreateInstance<BespokeNPCObject>();
            barnaby.npcId = "npc_barnaby";
            barnaby.displayName = "Barnaby";
            barnaby.title = "Master Anvil Artisan";
            barnaby.primaryRole = NPCRole.Blacksmith;
            barnaby.quirk = NPCQuirk.MasterSmelter;
            barnaby.systemPromptPersona = "You are Barnaby, a grizzled, booming-voiced blacksmith who respects steel and edge temper above all else. You warn delvers of the Three-Strike Anvil rule and take immense pride in quenching fractured Damascus blades.";
            barnaby.affinityLevel = 3;
            barnaby.combatAttackPower = 65f;
            barnaby.companionSupportCooldown = 8f;
            barnaby.expeditionSpeedMultiplier = 0.9f;
            barnaby.rareDiscoveryBonus = 0.1f;
            AssetDatabase.CreateAsset(barnaby, barnabyPath);
        }

        // NPC 3: Zephyr
        string zephyrPath = "Assets/Settings/NPC_Zephyr_Data.asset";
        BespokeNPCObject zephyr = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>(zephyrPath);
        if (zephyr == null)
        {
            zephyr = ScriptableObject.CreateInstance<BespokeNPCObject>();
            zephyr.npcId = "npc_zephyr";
            zephyr.displayName = "Zephyr";
            zephyr.title = "Cavern Scout & Windstalker";
            zephyr.primaryRole = NPCRole.BattleCompanion;
            zephyr.quirk = NPCQuirk.ArcaneResonator;
            zephyr.systemPromptPersona = "You are Zephyr, an agile windstalker delver wrapped in lightweight silks. Calm, observant, and swift, you track crystalline mana veins and glide silently across subterranean chasms.";
            zephyr.affinityLevel = 2;
            zephyr.combatAttackPower = 52f;
            zephyr.companionSupportCooldown = 6f;
            zephyr.expeditionSpeedMultiplier = 1.4f;
            zephyr.rareDiscoveryBonus = 0.20f;
            AssetDatabase.CreateAsset(zephyr, zephyrPath);
        }
    }

    private static void CreatePrefabs()
    {
        CreateBossShockwaveVFXPrefab();
        CreatePuzzleCryptexPrefab();
        CreateMineralVeinPrefab();
        CreateUnrefinedGeodePrefab();
        CreateNPCPrefabs();
        CreateSystemBroadcastBannerPrefab();
        CreateStoreAccessoryModalPrefab();
        CreateExpeditionBoardModalPrefab();
        CreateRelicInspectionOverlayPrefab();
        CreatePortraitHUDPrefab();
    }

    private static void CreateBossShockwaveVFXPrefab()
    {
        string path = "Assets/Prefabs/Combat/BossShockwaveVFX.prefab";
        GameObject shockwaveObj = new GameObject("BossShockwaveVFX");
        shockwaveObj.AddComponent<BossShockwaveVFX>();

        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(shockwaveObj.transform, false);
        cylinder.transform.localScale = new Vector3(1f, 0.05f, 1f);
        Collider col = cylinder.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Renderer rend = cylinder.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = GetOrCreateMaterial("Mat_Boss_Shockwave", new Color(1f, 0.2f, 0.5f, 0.8f), 0.2f, 0.6f, new Color(1f, 0.2f, 0.5f) * 1.5f);
        }

        PrefabUtility.SaveAsPrefabAsset(shockwaveObj, path);
        Object.DestroyImmediate(shockwaveObj);
    }

    private static void CreatePuzzleCryptexPrefab()
    {
        string path = "Assets/Prefabs/Relics/PuzzleCryptex.prefab";
        GameObject cryptexObj = new GameObject("PuzzleCryptex");
        var boxCol = cryptexObj.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(1.6f, 1.6f, 2.4f);
        PuzzleCryptex puzzleRoot = cryptexObj.AddComponent<PuzzleCryptex>();

        // Materials
        Material brassMat = GetOrCreateMaterial("Mat_Relic_BrushedBrass", new Color(0.85f, 0.72f, 0.35f, 1f), 0.88f, 0.72f);
        Material obsidianMat = GetOrCreateMaterial("Mat_Relic_ObsidianChassis", new Color(0.06f, 0.07f, 0.10f, 1f), 0.35f, 0.85f);
        Material ringsMat = GetOrCreateMaterial("Mat_Relic_RunedRings", new Color(0.12f, 0.35f, 0.65f, 1f), 0.82f, 0.78f, new Color(0f, 0.65f, 1f) * 1.5f);
        Material boltMat = GetOrCreateMaterial("Mat_Relic_TumblerBolt", new Color(0.85f, 0.88f, 0.92f, 1f), 0.95f, 0.88f);
        Material gemMat = GetOrCreateMaterial("Mat_Relic_AstralGem", new Color(0f, 0.95f, 0.85f, 1f), 0.1f, 0.95f, new Color(0f, 1f, 0.9f) * 4.5f);

        // Chassis End-Caps
        float[] capZ = { -0.95f, 0.95f };
        for (int i = 0; i < 2; i++)
        {
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = $"EndCap_{(i == 0 ? "Left" : "Right")}";
            cap.transform.SetParent(cryptexObj.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0f, capZ[i]);
            cap.transform.localScale = new Vector3(1.25f, 0.12f, 1.25f);
            cap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var capRend = cap.GetComponent<Renderer>(); if (capRend != null) capRend.sharedMaterial = brassMat;

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "BevelRim";
            rim.transform.SetParent(cap.transform, false);
            rim.transform.localPosition = new Vector3(0f, (i == 0 ? 0.06f : -0.06f), 0f);
            rim.transform.localScale = new Vector3(1.08f, 0.4f, 1.08f);
            var rimCol = rim.GetComponent<Collider>(); if (rimCol != null) Object.DestroyImmediate(rimCol);
            var rimRend = rim.GetComponent<Renderer>(); if (rimRend != null) rimRend.sharedMaterial = obsidianMat;
        }

        // Structural Frame Spines (3 perimeter rods)
        for (int r = 0; r < 3; r++)
        {
            float angle = r * Mathf.PI * 2f / 3f + 0.5f;
            Vector3 rodPos = new Vector3(Mathf.Cos(angle) * 0.64f, Mathf.Sin(angle) * 0.64f, 0f);
            GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = $"FrameSpine_{r + 1}";
            rod.transform.SetParent(cryptexObj.transform, false);
            rod.transform.localPosition = rodPos;
            rod.transform.localScale = new Vector3(0.06f, 0.95f, 0.06f);
            rod.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var rodCol = rod.GetComponent<Collider>(); if (rodCol != null) Object.DestroyImmediate(rodCol);
            var rodRend = rod.GetComponent<Renderer>(); if (rodRend != null) rodRend.sharedMaterial = brassMat;
        }

        // 3 Rotating Cipher Rings
        CryptexPuzzlePiece[] rings = new CryptexPuzzlePiece[3];
        float[] ringZ = { -0.50f, 0f, 0.50f };
        int[] targetSteps = { 2, 4, 1 };

        for (int i = 0; i < 3; i++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"CipherRing_{i + 1}";
            ring.transform.SetParent(cryptexObj.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0f, ringZ[i]);
            ring.transform.localScale = new Vector3(1.18f, 0.22f, 1.18f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ringRend = ring.GetComponent<Renderer>(); if (ringRend != null) ringRend.sharedMaterial = ringsMat;

            // 6 Notch Teeth / Glyph Markings along circumference
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3f;
                GameObject notch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                notch.name = $"GlyphNotch_{k + 1}";
                notch.transform.SetParent(ring.transform, false);
                notch.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.52f, 0f, Mathf.Sin(a) * 0.52f);
                notch.transform.localScale = new Vector3(0.12f, 0.75f, 0.12f);
                notch.transform.localRotation = Quaternion.Euler(0f, -k * 60f, 0f);
                var nCol = notch.GetComponent<Collider>(); if (nCol != null) Object.DestroyImmediate(nCol);
                var nRend = notch.GetComponent<Renderer>(); if (nRend != null) nRend.sharedMaterial = (k == targetSteps[i]) ? gemMat : brassMat;
            }

            CryptexPuzzlePiece piece = ring.AddComponent<CryptexPuzzlePiece>();
            SerializedObject so = new SerializedObject(piece);
            so.FindProperty("targetSolutionStep").intValue = targetSteps[i];
            so.FindProperty("rotationAxis").vector3Value = Vector3.up;
            so.ApplyModifiedProperties();

            rings[i] = piece;
        }

        // Sliding Tumbler Guide Rail & Bolt
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "TumblerGuideRail";
        rail.transform.SetParent(cryptexObj.transform, false);
        rail.transform.localPosition = new Vector3(0f, 0.68f, 0f);
        rail.transform.localScale = new Vector3(0.22f, 0.10f, 1.45f);
        var railCol = rail.GetComponent<Collider>(); if (railCol != null) Object.DestroyImmediate(railCol);
        var railRend = rail.GetComponent<Renderer>(); if (railRend != null) railRend.sharedMaterial = obsidianMat;

        GameObject boltObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boltObj.name = "TumblerLockingBolt";
        boltObj.transform.SetParent(cryptexObj.transform, false);
        boltObj.transform.localPosition = new Vector3(0f, 0.72f, -0.25f);
        boltObj.transform.localScale = new Vector3(0.16f, 0.16f, 0.42f);
        var boltRend = boltObj.GetComponent<Renderer>(); if (boltRend != null) boltRend.sharedMaterial = boltMat;

        CryptexTumblerBolt tumbler = boltObj.AddComponent<CryptexTumblerBolt>();
        SerializedObject tumblerSo = new SerializedObject(tumbler);
        tumblerSo.FindProperty("slideAxis").vector3Value = Vector3.forward;
        tumblerSo.FindProperty("slideDistance").floatValue = 0.5f;
        tumblerSo.ApplyModifiedProperties();

        // Inner Pneumatic Core Chamber
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = "CoreChamber";
        core.transform.SetParent(cryptexObj.transform, false);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = new Vector3(0.68f, 1.6f, 0.68f);
        core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var coreRend = core.GetComponent<Renderer>(); if (coreRend != null) coreRend.sharedMaterial = brassMat;

        // Hidden Astral Gem inside core
        GameObject gemObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gemObj.name = "AstralGem";
        gemObj.transform.SetParent(core.transform, false);
        gemObj.transform.localPosition = Vector3.zero;
        gemObj.transform.localScale = new Vector3(0.48f, 0.22f, 0.48f);
        var gemRend = gemObj.GetComponent<Renderer>(); if (gemRend != null) gemRend.sharedMaterial = gemMat;

        GameObject gemLightObj = new GameObject("GemRadianceLight", typeof(Light));
        gemLightObj.transform.SetParent(gemObj.transform, false);
        Light gemLight = gemLightObj.GetComponent<Light>();
        gemLight.type = LightType.Point;
        gemLight.color = new Color(0f, 1f, 0.9f);
        gemLight.intensity = 2.4f;
        gemLight.range = 3.5f;

        CryptexGemCore gemCore = gemObj.AddComponent<CryptexGemCore>();

        // Wire PuzzleCryptex root
        SerializedObject puzzleSo = new SerializedObject(puzzleRoot);
        SerializedProperty ringsProp = puzzleSo.FindProperty("cipherRings");
        ringsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            ringsProp.GetArrayElementAtIndex(i).objectReferenceValue = rings[i];
        }
        puzzleSo.FindProperty("tumblerBolt").objectReferenceValue = tumbler;
        puzzleSo.FindProperty("coreChamber").objectReferenceValue = core.transform;
        puzzleSo.FindProperty("gemCore").objectReferenceValue = gemCore;
        puzzleSo.FindProperty("coreUnlockedOffset").vector3Value = new Vector3(0f, 0f, 1.15f);
        puzzleSo.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(cryptexObj, path);
        Object.DestroyImmediate(cryptexObj);
    }

    private static void CreateMineralVeinPrefab()
    {
        string path = "Assets/Prefabs/Mining/MineralVeinNode.prefab";
        GameObject nodeObj = new GameObject("MineralVeinNode");
        CaveMiningNode nodeScript = nodeObj.AddComponent<CaveMiningNode>();
        nodeObj.AddComponent<AudioSource>();

        // Cluster rocks
        GameObject rockBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rockBase.name = "RockBase";
        rockBase.transform.SetParent(nodeObj.transform, false);
        rockBase.transform.localScale = new Vector3(1.4f, 1.2f, 1.4f);
        rockBase.transform.localRotation = Quaternion.Euler(15f, 25f, 10f);

        Renderer rockRend = rockBase.GetComponent<Renderer>();
        if (rockRend != null)
        {
            rockRend.sharedMaterial = GetOrCreateMaterial("Mat_Mining_Vein_Rock", new Color(0.16f, 0.17f, 0.20f, 1f), 0.10f, 0.35f);
        }

        // Crystal spire protruding
        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crystal.name = "CrystallineCluster";
        crystal.transform.SetParent(nodeObj.transform, false);
        crystal.transform.localPosition = new Vector3(0.1f, 0.7f, 0f);
        crystal.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
        crystal.transform.localRotation = Quaternion.Euler(20f, 10f, -15f);

        Renderer crystalRend = crystal.GetComponent<Renderer>();
        if (crystalRend != null)
        {
            crystalRend.sharedMaterial = GetOrCreateMaterial("Mat_Mining_Vein_Crystal", new Color(0.1f, 0.75f, 0.95f, 1f), 0.40f, 0.90f, new Color(0.2f, 0.8f, 1.0f) * 1.8f);
        }

        // Particle sparks
        GameObject sparkObj = new GameObject("ImpactSparksVFX");
        sparkObj.transform.SetParent(nodeObj.transform, false);
        ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.4f;
        main.startSpeed = 5f;
        main.startSize = 0.1f;
        main.playOnAwake = false;

        SerializedObject so = new SerializedObject(nodeScript);
        so.FindProperty("impactSparksVFX").objectReferenceValue = ps;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(nodeObj, path);
        Object.DestroyImmediate(nodeObj);
    }

    private static void CreateUnrefinedGeodePrefab()
    {
        string path = "Assets/Prefabs/Mining/UnrefinedGeode.prefab";
        GameObject geodeObj = new GameObject("UnrefinedGeode");
        UnrefinedGeode geodeScript = geodeObj.AddComponent<UnrefinedGeode>();

        // Upper shell
        GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        upper.name = "GeodeUpperHalf";
        upper.transform.SetParent(geodeObj.transform, false);
        upper.transform.localScale = new Vector3(0.9f, 0.45f, 0.9f);
        upper.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        Renderer upRend = upper.GetComponent<Renderer>();
        if (upRend != null)
        {
            upRend.sharedMaterial = GetOrCreateMaterial("Mat_Geode_Shell", new Color(0.25f, 0.22f, 0.18f, 1f), 0.10f, 0.35f);
        }

        // Lower shell
        GameObject lower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lower.name = "GeodeLowerHalf";
        lower.transform.SetParent(geodeObj.transform, false);
        lower.transform.localScale = new Vector3(0.9f, 0.45f, 0.9f);
        lower.transform.localPosition = new Vector3(0f, -0.22f, 0f);
        Renderer lowRend = lower.GetComponent<Renderer>();
        if (lowRend != null)
        {
            lowRend.sharedMaterial = GetOrCreateMaterial("Mat_Geode_Shell", new Color(0.25f, 0.22f, 0.18f, 1f), 0.10f, 0.35f);
        }

        // Inner glowing gem core
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "InnerGemCore";
        core.transform.SetParent(geodeObj.transform, false);
        core.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        core.SetActive(false);
        Renderer coreRend = core.GetComponent<Renderer>();
        if (coreRend != null)
        {
            coreRend.sharedMaterial = GetOrCreateMaterial("Mat_Geode_Core", new Color(0.7f, 0.15f, 0.9f, 1f), 0.45f, 0.85f, new Color(0.8f, 0.2f, 1.0f) * 2.0f);
        }

        SerializedObject so = new SerializedObject(geodeScript);
        so.FindProperty("geodeUpperHalf").objectReferenceValue = upper.transform;
        so.FindProperty("geodeLowerHalf").objectReferenceValue = lower.transform;
        so.FindProperty("innerGemCore").objectReferenceValue = core;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(geodeObj, path);
        Object.DestroyImmediate(geodeObj);
    }

    private static void CreateNPCPrefabs()
    {
        CreateIndividualNPCPrefab("NPC_Margo", "Assets/Settings/NPC_Margo_Data.asset", "Mat_NPC_Margo");
        CreateIndividualNPCPrefab("NPC_Barnaby", "Assets/Settings/NPC_Barnaby_Data.asset", "Mat_NPC_Barnaby");
        CreateIndividualNPCPrefab("NPC_Zephyr", "Assets/Settings/NPC_Zephyr_Data.asset", "Mat_NPC_Zephyr");
    }

    private static void CreateIndividualNPCPrefab(string name, string dataAssetPath, string materialName)
    {
        string path = $"Assets/Prefabs/NPCs/{name}.prefab";
        GameObject npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npcObj.name = name;
        npcObj.tag = "Untagged";

        BespokeNPCObject data = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>(dataAssetPath);

        NPCConversationAgent agent = npcObj.AddComponent<NPCConversationAgent>();
        agent.npcProfile = data;

        CompanionAIController companionAI = npcObj.AddComponent<CompanionAIController>();
        companionAI.companionProfile = data;

        Renderer rend = npcObj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{materialName}.mat");
            if (mat != null)
            {
                rend.sharedMaterial = mat;
            }
        }

        AttachNPCVisualAccessories(npcObj, name);

        PrefabUtility.SaveAsPrefabAsset(npcObj, path);
        Object.DestroyImmediate(npcObj);
    }

    private static void CreateSystemBroadcastBannerPrefab()
    {
        string path = "Assets/Prefabs/UI/SystemBroadcastBanner.prefab";
        GameObject bannerObj = new GameObject("SystemBroadcastBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(SystemBroadcastManager));
        RectTransform rt = bannerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.94f);
        rt.anchorMax = new Vector2(0.95f, 0.985f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = bannerObj.GetComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.12f, 0.92f);

        CanvasGroup cg = bannerObj.GetComponent<CanvasGroup>();
        cg.alpha = 1f;

        GameObject textObj = new GameObject("BroadcastMarqueeTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(bannerObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16f, 4f);
        textRt.offsetMax = new Vector2(-16f, -4f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "[SOVEREIGN] [SPIRE BROADCAST] Ancient Glyphs resonate across the subterranean vault!";
        tmp.fontSize = 24;
        tmp.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        SystemBroadcastManager mgr = bannerObj.GetComponent<SystemBroadcastManager>();
        SerializedObject so = new SerializedObject(mgr);
        so.FindProperty("broadcastBannerContainer").objectReferenceValue = bannerObj;
        so.FindProperty("broadcastText").objectReferenceValue = tmp;
        so.FindProperty("bannerCanvasGroup").objectReferenceValue = cg;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(bannerObj, path);
        Object.DestroyImmediate(bannerObj);
    }

    private static void CreateStoreAccessoryModalPrefab()
    {
        string path = "Assets/Prefabs/UI/StoreAccessoryModal.prefab";
        GameObject modalObj = new GameObject("StoreAccessoryModal", typeof(RectTransform));
        RectTransform rt = modalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = modalObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

        StoreAccessoryModalController ctrl = modalObj.AddComponent<StoreAccessoryModalController>();

        CreateTextObject(modalObj.transform, "TitleText", "ASTRAL RELIQUARY (STORE)", 44, TextAnchor.MiddleCenter, new Vector2(0f, 850f), new Vector2(900f, 120f), Color.cyan);
        CreateTextObject(modalObj.transform, "DescText", "Google Play Compliant - Additive Utility Accessories (+0.25x)\nHigh-tier multiplicative items must be forged in the Spire.", 26, TextAnchor.MiddleCenter, new Vector2(0f, 720f), new Vector2(920f, 100f), Color.white);
        GameObject statusObj = CreateTextObject(modalObj.transform, "StatusText", "Ready to purchase utility boost.", 28, TextAnchor.MiddleCenter, new Vector2(0f, -600f), new Vector2(800f, 80f), Color.yellow);

        GameObject btn1 = CreateButtonObject(modalObj.transform, "BuyWhetstoneSigilBtn", "Gilded Whetstone Sigil (+0.25x Satiety)", new Vector2(0f, 400f), new Vector2(820f, 140f), new Color(0.2f, 0.4f, 0.7f, 1f));
        GameObject btn2 = CreateButtonObject(modalObj.transform, "BuyAstralPendantBtn", "Astral Core Pendant (+0.25x Max Mana)", new Vector2(0f, 200f), new Vector2(820f, 140f), new Color(0.3f, 0.2f, 0.6f, 1f));
        GameObject btn3 = CreateButtonObject(modalObj.transform, "BuyVoracityRingBtn", "Ring of Voracity (+0.25x Leech Rate)", new Vector2(0f, 0f), new Vector2(820f, 140f), new Color(0.6f, 0.2f, 0.3f, 1f));
        GameObject closeBtn = CreateButtonObject(modalObj.transform, "CloseBtn", "CLOSE STORE", new Vector2(0f, -400f), new Vector2(600f, 120f), new Color(0.4f, 0.4f, 0.4f, 1f));

        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        so.FindProperty("statusFeedbackText").objectReferenceValue = statusObj.GetComponent<Text>();
        so.FindProperty("buyWhetstoneSigilBtn").objectReferenceValue = btn1.GetComponent<Button>();
        so.FindProperty("buyAstralPendantBtn").objectReferenceValue = btn2.GetComponent<Button>();
        so.FindProperty("buyVoracityRingBtn").objectReferenceValue = btn3.GetComponent<Button>();
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(modalObj, path);
        Object.DestroyImmediate(modalObj);
    }

    private static void CreateExpeditionBoardModalPrefab()
    {
        string path = "Assets/Prefabs/UI/ExpeditionBoardModal.prefab";
        GameObject modalObj = new GameObject("ExpeditionBoardModal", typeof(RectTransform));
        RectTransform rt = modalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = modalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.12f, 0.95f);

        ExpeditionBoardModalController ctrl = modalObj.AddComponent<ExpeditionBoardModalController>();

        // Load NPC profiles
        BespokeNPCObject margo = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>("Assets/Settings/NPC_Margo_Data.asset");
        BespokeNPCObject barnaby = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>("Assets/Settings/NPC_Barnaby_Data.asset");
        BespokeNPCObject zephyr = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>("Assets/Settings/NPC_Zephyr_Data.asset");

        ctrl.rosterProfiles.Clear();
        if (margo != null) ctrl.rosterProfiles.Add(margo);
        if (barnaby != null) ctrl.rosterProfiles.Add(barnaby);
        if (zephyr != null) ctrl.rosterProfiles.Add(zephyr);

        CreateTextObject(modalObj.transform, "TitleText", "EXPEDITION & FORAGING DISPATCH", 40, TextAnchor.MiddleCenter, new Vector2(0f, 950f), new Vector2(960f, 100f), Color.cyan);
        CreateTextObject(modalObj.transform, "SubtitleText", "Monster Hunter Safari Style - Max 3 Dispatches | Active Partner: 1 Strict Cap", 24, TextAnchor.MiddleCenter, new Vector2(0f, 860f), new Vector2(980f, 80f), Color.white);
        GameObject statusInfo = CreateTextObject(modalObj.transform, "StatusInfoText", "Select bespoke companion actions:", 26, TextAnchor.MiddleCenter, new Vector2(0f, 780f), new Vector2(960f, 70f), Color.yellow);

        // Row 1: Margo
        GameObject txt1 = CreateTextObject(modalObj.transform, "RowText1", "Margo (Hoarder of Gems)\nAlchemist Specialist", 28, TextAnchor.MiddleLeft, new Vector2(-160f, 550f), new Vector2(500f, 120f), Color.white);
        GameObject raidBtn1 = CreateButtonObject(modalObj.transform, "RaidBtn1", "ASSIGN RAID (1 Max)", new Vector2(240f, 590f), new Vector2(300f, 70f), new Color(0.2f, 0.6f, 0.3f, 1f));
        GameObject expBtn1 = CreateButtonObject(modalObj.transform, "ExpBtn1", "DISPATCH SAFARI (3 Max)", new Vector2(240f, 510f), new Vector2(300f, 70f), new Color(0.25f, 0.45f, 0.7f, 1f));

        // Row 2: Barnaby
        GameObject txt2 = CreateTextObject(modalObj.transform, "RowText2", "Barnaby (Master Smelter)\nBlacksmith Anvil Artisan", 28, TextAnchor.MiddleLeft, new Vector2(-160f, 320f), new Vector2(500f, 120f), Color.white);
        GameObject raidBtn2 = CreateButtonObject(modalObj.transform, "RaidBtn2", "ASSIGN RAID (1 Max)", new Vector2(240f, 360f), new Vector2(300f, 70f), new Color(0.2f, 0.6f, 0.3f, 1f));
        GameObject expBtn2 = CreateButtonObject(modalObj.transform, "ExpBtn2", "DISPATCH SAFARI (3 Max)", new Vector2(240f, 280f), new Vector2(300f, 70f), new Color(0.25f, 0.45f, 0.7f, 1f));

        // Row 3: Zephyr
        GameObject txt3 = CreateTextObject(modalObj.transform, "RowText3", "Zephyr (Arcane Resonator)\nCavern Scout & Windstalker", 28, TextAnchor.MiddleLeft, new Vector2(-160f, 90f), new Vector2(500f, 120f), Color.white);
        GameObject raidBtn3 = CreateButtonObject(modalObj.transform, "RaidBtn3", "ASSIGN RAID (1 Max)", new Vector2(240f, 130f), new Vector2(300f, 70f), new Color(0.2f, 0.6f, 0.3f, 1f));
        GameObject expBtn3 = CreateButtonObject(modalObj.transform, "ExpBtn3", "DISPATCH SAFARI (3 Max)", new Vector2(240f, 50f), new Vector2(300f, 70f), new Color(0.25f, 0.45f, 0.7f, 1f));

        // Close button
        GameObject closeBtn = CreateButtonObject(modalObj.transform, "CloseBtn", "CLOSE EXPEDITION BOARD", new Vector2(0f, -800f), new Vector2(650f, 110f), new Color(0.4f, 0.4f, 0.4f, 1f));

        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        so.FindProperty("statusInfoText").objectReferenceValue = statusInfo.GetComponent<Text>();
        so.FindProperty("assignRaidBtn1").objectReferenceValue = raidBtn1.GetComponent<Button>();
        so.FindProperty("dispatchExpBtn1").objectReferenceValue = expBtn1.GetComponent<Button>();
        so.FindProperty("npcStatusText1").objectReferenceValue = txt1.GetComponent<Text>();
        so.FindProperty("assignRaidBtn2").objectReferenceValue = raidBtn2.GetComponent<Button>();
        so.FindProperty("dispatchExpBtn2").objectReferenceValue = expBtn2.GetComponent<Button>();
        so.FindProperty("npcStatusText2").objectReferenceValue = txt2.GetComponent<Text>();
        so.FindProperty("assignRaidBtn3").objectReferenceValue = raidBtn3.GetComponent<Button>();
        so.FindProperty("dispatchExpBtn3").objectReferenceValue = expBtn3.GetComponent<Button>();
        so.FindProperty("npcStatusText3").objectReferenceValue = txt3.GetComponent<Text>();
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(modalObj, path);
        Object.DestroyImmediate(modalObj);
    }

    private static void CreateRelicInspectionOverlayPrefab()
    {
        string path = "Assets/Prefabs/UI/RelicInspectionOverlay.prefab";
        GameObject overlayObj = new GameObject("RelicInspectionOverlay", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rt = overlayObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = overlayObj.AddComponent<Image>();
        Sprite vignetteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Relic_RadialVignette.png");
        if (vignetteSprite != null)
        {
            bg.sprite = vignetteSprite;
            bg.type = Image.Type.Simple;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0.01f, 0.02f, 0.04f, 0.88f);
        }
        bg.raycastTarget = false; // Do not block raycasts to 3D puzzle!

        RelicInspectionOverlayController ctrl = overlayObj.AddComponent<RelicInspectionOverlayController>();

        CreateTextObject(overlayObj.transform, "RelicTitle", "★ 3D RELIC INSPECTOR: SPIRE ASTRALCRYPT ★", 30, TextAnchor.MiddleCenter, new Vector2(0f, 760f), new Vector2(980f, 80f), new Color(0.95f, 0.85f, 0.3f, 1f));
        GameObject modeTxt = CreateTextObject(overlayObj.transform, "ModeIndicatorText", "MODE: RELIC INSPECTION (ASMR ACTIVE)", 24, TextAnchor.MiddleCenter, new Vector2(0f, 700f), new Vector2(850f, 50f), new Color(0.4f, 0.9f, 1f, 1f));
        GameObject guideTxt = CreateTextObject(overlayObj.transform, "GuidePromptText", "1-Finger: Orbit | 2-Fingers: Zoom | Tap: Turn Rings | Slide: Tumbler Pin", 22, TextAnchor.MiddleCenter, new Vector2(0f, -630f), new Vector2(980f, 70f), new Color(1f, 0.9f, 0.5f, 0.95f));
        GameObject statusTxt = CreateTextObject(overlayObj.transform, "PuzzleStatusText", "[CIPHER RINGS: 0/3 ALIGNED | TUMBLER BOLT: LOCKED]", 22, TextAnchor.MiddleCenter, new Vector2(0f, -710f), new Vector2(980f, 60f), new Color(0.85f, 0.9f, 1f, 0.95f));

        GameObject solveBtn = CreateButtonObject(overlayObj.transform, "SolveTestBtn", "TEST SOLVE CODE", new Vector2(-230f, -810f), new Vector2(400f, 90f), new Color(0.12f, 0.45f, 0.32f, 1f));
        GameObject closeBtn = CreateButtonObject(overlayObj.transform, "CloseBtn", "EXIT INSPECTION", new Vector2(230f, -810f), new Vector2(400f, 90f), new Color(0.55f, 0.18f, 0.22f, 1f));

        GameObject cryptexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Relics/PuzzleCryptex.prefab");

        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("closeInspectionBtn").objectReferenceValue = closeBtn.GetComponent<Button>();
        so.FindProperty("solveTestBtn").objectReferenceValue = solveBtn.GetComponent<Button>();
        so.FindProperty("modeIndicatorText").objectReferenceValue = modeTxt.GetComponent<Text>();
        so.FindProperty("guidePromptText").objectReferenceValue = guideTxt.GetComponent<Text>();
        so.FindProperty("puzzleStatusText").objectReferenceValue = statusTxt.GetComponent<Text>();
        so.FindProperty("cryptexPrefab").objectReferenceValue = cryptexPrefab;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(overlayObj, path);
        Object.DestroyImmediate(overlayObj);
    }

    private static void CreatePortraitHUDPrefab()
    {
        string path = "Assets/Prefabs/UI/PortraitHUD.prefab";
        GameObject canvasObj = BuildCanvasHierarchy();
        PrefabUtility.SaveAsPrefabAsset(canvasObj, path);
        Object.DestroyImmediate(canvasObj);
    }

    public static GameObject BuildCanvasHierarchy()
    {
        // Screen Space - Overlay Canvas
        GameObject canvasObj = new GameObject("PortraitHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;
        scaler.dynamicPixelsPerUnit = 2f;

        PortraitHUDController hudCtrl = canvasObj.AddComponent<PortraitHUDController>();

        // SafeAreaContainer (Has AndroidSafeAreaFitter.cs)
        GameObject safeArea = new GameObject("SafeAreaContainer", typeof(RectTransform), typeof(AndroidSafeAreaFitter));
        safeArea.transform.SetParent(canvasObj.transform, false);
        RectTransform safeAreaRt = safeArea.GetComponent<RectTransform>();
        safeAreaRt.anchorMin = Vector2.zero;
        safeAreaRt.anchorMax = Vector2.one;
        safeAreaRt.offsetMin = Vector2.zero;
        safeAreaRt.offsetMax = Vector2.zero;

        // SystemBroadcastBanner (Anchors: Min[0.05, 0.94], Max[0.95, 0.98])
        GameObject bannerObj = new GameObject("SystemBroadcastBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(SystemBroadcastManager));
        bannerObj.transform.SetParent(safeArea.transform, false);
        RectTransform bannerRt = bannerObj.GetComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.05f, 0.94f);
        bannerRt.anchorMax = new Vector2(0.95f, 0.985f);
        bannerRt.offsetMin = Vector2.zero;
        bannerRt.offsetMax = Vector2.zero;
        Image bannerBg = bannerObj.GetComponent<Image>();
        bannerBg.color = new Color(0.06f, 0.08f, 0.16f, 0.92f);
        CanvasGroup bannerCg = bannerObj.GetComponent<CanvasGroup>();
        bannerCg.alpha = 1f;

        GameObject marqueeObj = new GameObject("BroadcastMarqueeTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        marqueeObj.transform.SetParent(bannerObj.transform, false);
        RectTransform marqueeRt = marqueeObj.GetComponent<RectTransform>();
        marqueeRt.anchorMin = Vector2.zero;
        marqueeRt.anchorMax = Vector2.one;
        marqueeRt.offsetMin = new Vector2(16f, 4f);
        marqueeRt.offsetMax = new Vector2(-16f, -4f);
        TextMeshProUGUI marqueeTmp = marqueeObj.GetComponent<TextMeshProUGUI>();
        marqueeTmp.text = "[SOVEREIGN] [SPIRE BROADCAST] Ancient Glyphs resonate across the subterranean vault!";
        marqueeTmp.fontSize = 24;
        marqueeTmp.color = new Color(1f, 0.85f, 0.2f, 1f);
        marqueeTmp.alignment = TextAlignmentOptions.Center;
        marqueeTmp.enableWordWrapping = false;

        SystemBroadcastManager sysMgr = bannerObj.GetComponent<SystemBroadcastManager>();
        SerializedObject sysSo = new SerializedObject(sysMgr);
        sysSo.FindProperty("broadcastBannerContainer").objectReferenceValue = bannerObj;
        sysSo.FindProperty("broadcastText").objectReferenceValue = marqueeTmp;
        sysSo.FindProperty("bannerCanvasGroup").objectReferenceValue = bannerCg;
        sysSo.ApplyModifiedProperties();

        // TopTelemetryHUD (Anchors: Min[0, 0.84], Max[1, 0.94])
        GameObject topTelemetry = new GameObject("TopTelemetryHUD", typeof(RectTransform), typeof(Image));
        topTelemetry.transform.SetParent(safeArea.transform, false);
        RectTransform topRt = topTelemetry.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0f, 0.84f);
        topRt.anchorMax = new Vector2(1f, 0.94f);
        topRt.offsetMin = Vector2.zero;
        topRt.offsetMax = Vector2.zero;
        Image topBg = topTelemetry.GetComponent<Image>();
        topBg.color = new Color(0.08f, 0.08f, 0.14f, 0.85f);

        // FloorIndexText
        GameObject floorTxtObj = CreateTextObject(topTelemetry.transform, "FloorIndexText", "SPIRE FLOOR 14 - SUBTERRANEAN CAVERNS", 30, TextAnchor.MiddleCenter, new Vector2(0f, 65f), new Vector2(900f, 50f), new Color(1f, 0.85f, 0.3f, 1f));

        // BossHealthBar
        GameObject bossBarObj = CreateSliderObject(topTelemetry.transform, "BossHealthBar", new Vector2(0f, 15f), new Vector2(800f, 26f), new Color(0.85f, 0.15f, 0.2f, 1f), "BOSS INTEGRITY");

        // SatietyGauge
        GameObject satietyObj = CreateSliderObject(topTelemetry.transform, "SatietyGauge", new Vector2(0f, -25f), new Vector2(800f, 20f), new Color(0.6f, 0.15f, 0.85f, 1f), "WEAPON SATIETY");

        // GemVaultCounterTMP
        GameObject vaultObj = new GameObject("GemVaultCounterTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        vaultObj.transform.SetParent(topTelemetry.transform, false);
        RectTransform vaultRt = vaultObj.GetComponent<RectTransform>();
        vaultRt.anchoredPosition = new Vector2(-220f, -68f);
        vaultRt.sizeDelta = new Vector2(500f, 40f);
        TextMeshProUGUI vaultTmp = vaultObj.GetComponent<TextMeshProUGUI>();
        vaultTmp.text = "<color=#FFD700>[CORE]</color> 250 | <color=#00D8FF>[AZUR]</color> 3 | <color=#FF4444>[RUBY]</color> 2";
        vaultTmp.fontSize = 22;
        vaultTmp.color = Color.cyan;
        vaultTmp.alignment = TextAlignmentOptions.Left;

        // ActiveCompanionSlot (Shows 1 active raid partner icon/text)
        GameObject companionSlotObj = CreateTextObject(topTelemetry.transform, "ActiveCompanionSlot", "[PARTNER]: None (Solo Raid)", 22, TextAnchor.MiddleRight, new Vector2(240f, -68f), new Vector2(450f, 40f), new Color(0.3f, 1f, 0.5f, 1f));

        // CentralGestureCanvas (Anchors: Min[0, 0.28], Max[1, 0.84])
        GameObject gestureObj = new GameObject("CentralGestureCanvas", typeof(RectTransform), typeof(Image), typeof(PortraitGestureCanvas));
        gestureObj.transform.SetParent(safeArea.transform, false);
        RectTransform gestureRt = gestureObj.GetComponent<RectTransform>();
        gestureRt.anchorMin = new Vector2(0f, 0.28f);
        gestureRt.anchorMax = new Vector2(1f, 0.84f);
        gestureRt.offsetMin = Vector2.zero;
        gestureRt.offsetMax = Vector2.zero;
        Image gestureImg = gestureObj.GetComponent<Image>();
        gestureImg.color = new Color(0f, 0f, 0f, 0f);
        gestureImg.raycastTarget = true;

        // BottomControlDeck (Anchors: Min[0, 0.00], Max[1, 0.28])
        GameObject bottomDeck = new GameObject("BottomControlDeck", typeof(RectTransform), typeof(Image));
        bottomDeck.transform.SetParent(safeArea.transform, false);
        RectTransform bottomRt = bottomDeck.GetComponent<RectTransform>();
        bottomRt.anchorMin = new Vector2(0f, 0.00f);
        bottomRt.anchorMax = new Vector2(1f, 0.28f);
        bottomRt.offsetMin = Vector2.zero;
        bottomRt.offsetMax = Vector2.zero;
        Image bottomBg = bottomDeck.GetComponent<Image>();
        bottomBg.color = new Color(0.06f, 0.06f, 0.1f, 0.85f);

        // FloatingVirtualStick (Left half: Min[0,0], Max[0.5, 1])
        GameObject stickRoot = new GameObject("FloatingVirtualStick", typeof(RectTransform), typeof(FloatingVirtualStick));
        stickRoot.transform.SetParent(bottomDeck.transform, false);
        RectTransform stickRt = stickRoot.GetComponent<RectTransform>();
        stickRt.anchorMin = new Vector2(0f, 0f);
        stickRt.anchorMax = new Vector2(0.5f, 1f);
        stickRt.offsetMin = Vector2.zero;
        stickRt.offsetMax = Vector2.zero;

        GameObject stickBgObj = new GameObject("StickBackground", typeof(RectTransform), typeof(Image));
        stickBgObj.transform.SetParent(stickRoot.transform, false);
        RectTransform stickBgRt = stickBgObj.GetComponent<RectTransform>();
        stickBgRt.sizeDelta = new Vector2(240f, 240f);
        Image stickBgImg = stickBgObj.GetComponent<Image>();
        stickBgImg.color = new Color(1f, 1f, 1f, 0.18f);

        GameObject stickHandleObj = new GameObject("StickHandle", typeof(RectTransform), typeof(Image));
        stickHandleObj.transform.SetParent(stickBgObj.transform, false);
        RectTransform stickHandleRt = stickHandleObj.GetComponent<RectTransform>();
        stickHandleRt.sizeDelta = new Vector2(100f, 100f);
        Image stickHandleImg = stickHandleObj.GetComponent<Image>();
        stickHandleImg.color = new Color(0.2f, 0.8f, 1f, 0.7f);

        FloatingVirtualStick stickScript = stickRoot.GetComponent<FloatingVirtualStick>();
        SerializedObject stickSo = new SerializedObject(stickScript);
        stickSo.FindProperty("stickBackground").objectReferenceValue = stickBgRt;
        stickSo.FindProperty("stickHandle").objectReferenceValue = stickHandleRt;
        stickSo.ApplyModifiedProperties();

        // TouchCameraLookZone (Right half: Min[0.5, 0], Max[1, 1])
        // Dedicated touch orbit drag area right next to joystick as requested
        GameObject lookZoneObj = new GameObject("TouchCameraLookZone", typeof(RectTransform), typeof(Image), typeof(TouchCameraLookZone));
        lookZoneObj.transform.SetParent(bottomDeck.transform, false);
        RectTransform lookRt = lookZoneObj.GetComponent<RectTransform>();
        lookRt.anchorMin = new Vector2(0.5f, 0f);
        lookRt.anchorMax = new Vector2(1f, 1f);
        lookRt.offsetMin = Vector2.zero;
        lookRt.offsetMax = Vector2.zero;
        Image lookImg = lookZoneObj.GetComponent<Image>();
        lookImg.color = new Color(0.08f, 0.10f, 0.18f, 0.35f);
        lookImg.raycastTarget = true;

        GameObject lookHintObj = CreateTextObject(lookZoneObj.transform, "CameraOrbitHintText", "✥ DRAG TO ORBIT CAMERA ✥\n<size=17>(Touch Look Zone)</size>", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(460f, 90f), new Color(0.65f, 0.75f, 0.90f, 0.75f));
        Text hintTxt = lookHintObj.GetComponent<Text>();
        if (hintTxt != null) hintTxt.raycastTarget = false;

        // Action Drawer Tab Button (Positioned directly above bottom control deck)
        GameObject tabBtnObj = new GameObject("ActionDrawerTabButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        tabBtnObj.transform.SetParent(safeArea.transform, false);
        RectTransform tabRt = tabBtnObj.GetComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0.12f, 0.282f);
        tabRt.anchorMax = new Vector2(0.88f, 0.328f);
        tabRt.offsetMin = Vector2.zero;
        tabRt.offsetMax = Vector2.zero;
        Image tabImg = tabBtnObj.GetComponent<Image>();
        tabImg.color = new Color(0.12f, 0.14f, 0.22f, 0.96f);
        Outline tabOutline = tabBtnObj.GetComponent<Outline>();
        tabOutline.effectColor = new Color(0.75f, 0.45f, 0.95f, 0.85f);
        tabOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject tabLabelObj = new GameObject("TabLabelTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        tabLabelObj.transform.SetParent(tabBtnObj.transform, false);
        RectTransform tabLblRt = tabLabelObj.GetComponent<RectTransform>();
        tabLblRt.anchorMin = Vector2.zero;
        tabLblRt.anchorMax = Vector2.one;
        tabLblRt.offsetMin = Vector2.zero;
        tabLblRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tabLblTmp = tabLabelObj.GetComponent<TextMeshProUGUI>();
        tabLblTmp.text = "<color=#BA55D3>▲</color> <b>ACTIONS & BELT DRAWER</b> <color=#BA55D3>▲</color>";
        tabLblTmp.fontSize = 22;
        tabLblTmp.alignment = TextAlignmentOptions.Center;
        tabLblTmp.color = new Color(0.95f, 0.92f, 1f);
        tabLblTmp.raycastTarget = false;

        // Slide-Out Action Drawer Panel (Collapsible drawer housing all secondary belt actions)
        GameObject drawerPanelObj = new GameObject("SlideOutActionDrawer", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Outline), typeof(HUDActionDrawerController));
        drawerPanelObj.transform.SetParent(safeArea.transform, false);
        RectTransform drawerRt = drawerPanelObj.GetComponent<RectTransform>();
        drawerRt.anchorMin = new Vector2(0.04f, 0.332f);
        drawerRt.anchorMax = new Vector2(0.96f, 0.760f);
        drawerRt.offsetMin = Vector2.zero;
        drawerRt.offsetMax = Vector2.zero;
        Image drawerBg = drawerPanelObj.GetComponent<Image>();
        drawerBg.color = new Color(0.05f, 0.06f, 0.11f, 0.97f);
        Outline drawerOutline = drawerPanelObj.GetComponent<Outline>();
        drawerOutline.effectColor = new Color(0.85f, 0.65f, 0.25f, 0.85f);
        drawerOutline.effectDistance = new Vector2(2f, -2f);

        CanvasGroup drawerCg = drawerPanelObj.GetComponent<CanvasGroup>();
        drawerCg.alpha = 0f;
        drawerCg.interactable = false;
        drawerCg.blocksRaycasts = false;

        // Wire HUDActionDrawerController
        HUDActionDrawerController drawerCtrl = drawerPanelObj.GetComponent<HUDActionDrawerController>();
        SerializedObject drawerSo = new SerializedObject(drawerCtrl);
        drawerSo.FindProperty("drawerTabButton").objectReferenceValue = tabBtnObj.GetComponent<Button>();
        drawerSo.FindProperty("tabLabelTMP").objectReferenceValue = tabLblTmp;
        drawerSo.FindProperty("drawerPanelRect").objectReferenceValue = drawerRt;
        drawerSo.FindProperty("drawerCanvasGroup").objectReferenceValue = drawerCg;
        drawerSo.FindProperty("closedAnchoredPosition").vector2Value = new Vector2(0f, -650f);
        drawerSo.FindProperty("openAnchoredPosition").vector2Value = new Vector2(0f, 0f);
        drawerSo.ApplyModifiedProperties();

        // Drawer Title Header
        CreateTextObject(drawerPanelObj.transform, "DrawerTitle", "✦ ADVENTURER'S BELT & EXPEDITION SATCHEL ✦", 22, TextAnchor.MiddleCenter, new Vector2(0f, 360f), new Vector2(900f, 40f), new Color(0.95f, 0.85f, 0.55f, 1f));

        // 11 Action Buttons neatly arranged inside SlideOutActionDrawer
        GameObject whetstoneBtnObj = CreateButtonObject(drawerPanelObj.transform, "WhetstoneItemButton", "HONE BLADE\n(Whetstone)", new Vector2(-235f, 285f), new Vector2(440f, 86f), new Color(0.7f, 0.45f, 0.1f, 1f));
        GameObject chiselBtnObj = CreateButtonObject(drawerPanelObj.transform, "MiningChiselToggleButton", "MINING CHISEL\n(Toggle Mode)", new Vector2(235f, 285f), new Vector2(440f, 86f), new Color(0.2f, 0.65f, 0.8f, 1f));

        GameObject expeditionBtnObj = CreateButtonObject(drawerPanelObj.transform, "ExpeditionSafariButton", "EXPEDITIONS\n(NPC Safari)", new Vector2(-235f, 180f), new Vector2(440f, 86f), new Color(0.3f, 0.65f, 0.35f, 1f));
        GameObject manaBtnObj = CreateButtonObject(drawerPanelObj.transform, "EmergencyManaDraftButton", "MANA DRAFT\n(+35 MP)", new Vector2(235f, 180f), new Vector2(440f, 86f), new Color(0.1f, 0.45f, 0.75f, 1f));

        GameObject storeBtnObj = CreateButtonObject(drawerPanelObj.transform, "StoreButton", "STORE", new Vector2(-235f, 75f), new Vector2(440f, 86f), new Color(0.4f, 0.2f, 0.5f, 1f));
        GameObject inspectBtnObj = CreateButtonObject(drawerPanelObj.transform, "InspectRelicButton", "INSPECT RELIC", new Vector2(235f, 75f), new Vector2(440f, 86f), new Color(0.5f, 0.25f, 0.6f, 1f));

        GameObject tribunalBtnObj = CreateButtonObject(drawerPanelObj.transform, "TribunalAppealButton", "TRIBUNAL APPEAL", new Vector2(-235f, -30f), new Vector2(440f, 86f), new Color(0.85f, 0.2f, 0.2f, 1f));
        GameObject margoBtnObj = CreateButtonObject(drawerPanelObj.transform, "InvokeMargoDialogueButton", "✦ ASK MARGO (H) \u2665", new Vector2(235f, -30f), new Vector2(440f, 86f), new Color(0.38f, 0.12f, 0.55f, 1f));

        GameObject kalimbaBtnObj = CreateButtonObject(drawerPanelObj.transform, "KalimbaInstrumentToggleButton", "KALIMBA INSTRUMENT", new Vector2(-235f, -135f), new Vector2(440f, 86f), new Color(0.2f, 0.45f, 0.7f, 1f));
        GameObject summonBtnObj = CreateButtonObject(drawerPanelObj.transform, "SummoningRitualButton", "SUMMONING RITUAL", new Vector2(235f, -135f), new Vector2(440f, 86f), new Color(0.55f, 0.25f, 0.7f, 1f));

        GameObject campfireBtnObj = CreateButtonObject(drawerPanelObj.transform, "CampfireHubButton", "CAMPFIRE MULTI-CHARACTER HUB", new Vector2(0f, -240f), new Vector2(910f, 86f), new Color(0.95f, 0.45f, 0.15f, 1f));

        CreateTextObject(drawerPanelObj.transform, "DrawerDismissHint", "<color=#7F8FA6>[ Tap Tab Above Deck or Press ESC to Dismiss Drawer ]</color>", 16, TextAnchor.MiddleCenter, new Vector2(0f, -330f), new Vector2(900f, 32f), new Color(0.6f, 0.65f, 0.75f, 0.8f));

        // Kalimba Instrument Overlay (Anchored to lower third)
        GameObject kalimbaOverlay = new GameObject("KalimbaOverlayPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(PortraitInstrumentUI));
        kalimbaOverlay.transform.SetParent(safeArea.transform, false);
        RectTransform kRt = kalimbaOverlay.GetComponent<RectTransform>();
        kRt.anchorMin = new Vector2(0f, 0f);
        kRt.anchorMax = new Vector2(1f, 0.34f);
        kRt.offsetMin = Vector2.zero;
        kRt.offsetMax = Vector2.zero;
        CanvasGroup kCg = kalimbaOverlay.GetComponent<CanvasGroup>();
        kCg.alpha = 0f;
        kCg.interactable = false;
        kCg.blocksRaycasts = false;

        // Summoning Ritual Modal (Full safe area modal, initially inactive)
        GameObject summonModal = CreateSummoningRitualModal(safeArea.transform);
        summonModal.SetActive(false);

        // Link HUD references
        SerializedObject hudSo = new SerializedObject(hudCtrl);
        hudSo.FindProperty("floorIndexText").objectReferenceValue = floorTxtObj.GetComponent<Text>();
        hudSo.FindProperty("bossHealthBar").objectReferenceValue = bossBarObj.GetComponent<Slider>();
        hudSo.FindProperty("satietyGauge").objectReferenceValue = satietyObj.GetComponent<Slider>();
        hudSo.FindProperty("gemVaultCounterTMP").objectReferenceValue = vaultTmp;
        hudSo.FindProperty("activeCompanionSlotText").objectReferenceValue = companionSlotObj.GetComponent<Text>();
        hudSo.FindProperty("whetstoneItemButton").objectReferenceValue = whetstoneBtnObj.GetComponent<Button>();
        hudSo.FindProperty("miningChiselToggleButton").objectReferenceValue = chiselBtnObj.GetComponent<Button>();
        hudSo.FindProperty("expeditionSafariButton").objectReferenceValue = expeditionBtnObj.GetComponent<Button>();
        hudSo.FindProperty("emergencyManaDraftButton").objectReferenceValue = manaBtnObj.GetComponent<Button>();
        hudSo.FindProperty("storeButton").objectReferenceValue = storeBtnObj.GetComponent<Button>();
        hudSo.FindProperty("inspectRelicButton").objectReferenceValue = inspectBtnObj.GetComponent<Button>();
        hudSo.FindProperty("tribunalAppealButton").objectReferenceValue = tribunalBtnObj.GetComponent<Button>();
        hudSo.FindProperty("invokeMargoDialogueButton").objectReferenceValue = margoBtnObj.GetComponent<Button>();
        hudSo.FindProperty("kalimbaInstrumentToggleButton").objectReferenceValue = kalimbaBtnObj.GetComponent<Button>();
        hudSo.FindProperty("summoningRitualButton").objectReferenceValue = summonBtnObj.GetComponent<Button>();
        hudSo.FindProperty("campfireHubButton").objectReferenceValue = campfireBtnObj.GetComponent<Button>();
        hudSo.FindProperty("kalimbaOverlayPanel").objectReferenceValue = kalimbaOverlay;
        hudSo.FindProperty("summoningRitualModal").objectReferenceValue = summonModal;
        hudSo.ApplyModifiedProperties();

        return canvasObj;
    }

    private static void BuildMainDungeonScene()
    {
        string scenePath = "Assets/Scenes/MainDungeonFloor.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Directional Light
        GameObject lightObj = new GameObject("Directional Light", typeof(Light));
        Light light = lightObj.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.intensity = 1.1f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Dungeon Floor Arena
        GameObject arena = GameObject.CreatePrimitive(PrimitiveType.Plane);
        arena.name = "DungeonFloorArena";
        arena.transform.localScale = new Vector3(8f, 1f, 8f);
        Renderer floorRend = arena.GetComponent<Renderer>();
        if (floorRend != null)
        {
            floorRend.sharedMaterial = GetOrCreateMaterial("Mat_Dungeon_Arena", new Color(0.12f, 0.12f, 0.15f, 1f), 0.15f, 0.45f);
        }

        // Surrounding Spire Pillars
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI / 3f;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 32f, 8f, Mathf.Sin(angle) * 32f);
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"SpirePillar_{i + 1}";
            pillar.transform.position = pos;
            pillar.transform.localScale = new Vector3(3f, 16f, 3f);
            Renderer pRend = pillar.GetComponent<Renderer>();
            if (pRend != null)
            {
                pRend.sharedMaterial = GetOrCreateMaterial("Mat_Cavern_Pillars", new Color(0.12f, 0.13f, 0.18f, 1f), 0.35f, 0.55f);
            }
        }

        // Subterranean Mining Mineral Vein Nodes
        GameObject veinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mining/MineralVeinNode.prefab");
        if (veinPrefab != null)
        {
            Vector3[] veinPositions = {
                new Vector3(-12f, 0.5f, -4f),
                new Vector3(12f, 0.5f, -4f),
                new Vector3(-14f, 0.5f, 8f),
                new Vector3(14f, 0.5f, 8f)
            };
            MineralRarity[] veinTypes = {
                MineralRarity.AzuriteCore,
                MineralRarity.CinnabarRuby,
                MineralRarity.GildedTopaz,
                MineralRarity.PrismaticHeart
            };

            for (int i = 0; i < veinPositions.Length; i++)
            {
                GameObject veinInstance = (GameObject)PrefabUtility.InstantiatePrefab(veinPrefab);
                veinInstance.name = $"MineralVein_{veinTypes[i]}";
                veinInstance.transform.position = veinPositions[i];
                CaveMiningNode cmNode = veinInstance.GetComponent<CaveMiningNode>();
                if (cmNode != null) cmNode.mineralType = veinTypes[i];
            }
        }

        // Player GameObject
        GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObj.name = "Player_Hero";
        playerObj.tag = "Player";
        playerObj.transform.position = new Vector3(0f, 1f, -8f);

        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0f, 0f);
        cc.radius = 0.5f;
        cc.height = 2f;

        PlayerProgression progression = playerObj.AddComponent<PlayerProgression>();
        LivingGearSystem livingGear = playerObj.AddComponent<LivingGearSystem>();
        WeaponTemper weaponTemper = playerObj.AddComponent<WeaponTemper>();
        ModularSpellEngine spellEngine = playerObj.AddComponent<ModularSpellEngine>();
        EquipmentSystem equipSystem = playerObj.AddComponent<EquipmentSystem>();
        playerObj.AddComponent<PlayerMovementController>();
        InstrumentCatalyst instCat = playerObj.AddComponent<InstrumentCatalyst>();
        instCat.isEquippedOnSpawn = false;
        playerObj.AddComponent<DevClassLoadoutManager>();
        playerObj.AddComponent<MargoGuideCompanion>();

        Renderer playerRend = playerObj.GetComponent<Renderer>();
        if (playerRend != null)
        {
            playerRend.sharedMaterial = GetOrCreateMaterial("Mat_Player_Hero", new Color(0.12f, 0.52f, 0.30f, 1f), 0.75f, 0.80f, new Color(0.2f, 0.8f, 0.4f) * 0.5f);
        }

        // Persistent Townsfolk NPCs in Base Camp corner
        GameObject margoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Margo.prefab");
        if (margoPrefab != null)
        {
            GameObject margoInst = (GameObject)PrefabUtility.InstantiatePrefab(margoPrefab);
            margoInst.transform.position = new Vector3(-8f, 1f, -12f);
        }

        GameObject barnabyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Barnaby.prefab");
        if (barnabyPrefab != null)
        {
            GameObject barnabyInst = (GameObject)PrefabUtility.InstantiatePrefab(barnabyPrefab);
            barnabyInst.transform.position = new Vector3(-4f, 1f, -13f);
        }

        GameObject zephyrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Zephyr.prefab");
        if (zephyrPrefab != null)
        {
            GameObject zephyrInst = (GameObject)PrefabUtility.InstantiatePrefab(zephyrPrefab);
            zephyrInst.transform.position = new Vector3(4f, 1f, -13f);
        }

        // Tall Spire Boss Sentinel
        GameObject bossObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bossObj.name = "SpireBoss_Aethelgard";
        bossObj.tag = "Enemy";
        bossObj.transform.position = new Vector3(0f, 3f, 12f);
        bossObj.transform.localScale = new Vector3(2.5f, 3.5f, 2.5f);

        BossController bossCtrl = bossObj.AddComponent<BossController>();
        GameObject shockwavePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/BossShockwaveVFX.prefab");
        SerializedObject bossSo = new SerializedObject(bossCtrl);
        bossSo.FindProperty("shockwaveVFXPrefab").objectReferenceValue = shockwavePrefab;
        bossSo.ApplyModifiedProperties();

        Renderer bossRend = bossObj.GetComponent<Renderer>();
        if (bossRend != null)
        {
            bossRend.sharedMaterial = GetOrCreateMaterial("Mat_SpireBoss_Aethelgard", new Color(0.52f, 0.08f, 0.12f, 1f), 0.88f, 0.70f, new Color(1.0f, 0.12f, 0.18f) * 2.2f);
        }

        // Minion Enemies
        for (int i = 0; i < 2; i++)
        {
            GameObject minion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            minion.name = $"SpireGargoyle_{i + 1}";
            minion.tag = "Enemy";
            minion.transform.position = new Vector3((i == 0 ? -6f : 6f), 1f, 5f);
            minion.AddComponent<SpireEnemy>();
            Renderer minRend = minion.GetComponent<Renderer>();
            if (minRend != null)
            {
                minRend.sharedMaterial = GetOrCreateMaterial("Mat_SpireGargoyle", new Color(0.20f, 0.22f, 0.28f, 1f), 0.15f, 0.42f, new Color(0.7f, 0.2f, 0.9f) * 0.5f);
            }
        }

        // Camera Rig (Decoupled Orbital ARPG Camera with elevated 42 deg pitch & 12m tactical framing)
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData), typeof(HighFidelityMobileResolutionEnforcer));
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 9.5f, -11.5f);
        camObj.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.fieldOfView = 75f;
        cam.allowMSAA = true;
        cam.allowHDR = true;

        UniversalAdditionalCameraData camData = camObj.GetComponent<UniversalAdditionalCameraData>();
        camData.renderType = CameraRenderType.Base;
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camData.antialiasingQuality = AntialiasingQuality.High;

        PortraitARPGCamera arpgCam = camObj.AddComponent<PortraitARPGCamera>();
        arpgCam.playerTarget = playerObj.transform;
        arpgCam.activeBossTarget = bossObj.transform;

        // UI Canvas Hierarchy
        GameObject canvasObj = BuildCanvasHierarchy();

        // Instantiate Store, Relic, and Expedition Modals (initially inactive)
        GameObject storeModalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/StoreAccessoryModal.prefab");
        GameObject storeModalInstance = null;
        if (storeModalPrefab != null)
        {
            storeModalInstance = (GameObject)PrefabUtility.InstantiatePrefab(storeModalPrefab, canvasObj.transform);
            storeModalInstance.SetActive(false);
        }

        GameObject relicOverlayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/RelicInspectionOverlay.prefab");
        GameObject relicOverlayInstance = null;
        if (relicOverlayPrefab != null)
        {
            relicOverlayInstance = (GameObject)PrefabUtility.InstantiatePrefab(relicOverlayPrefab, canvasObj.transform);
            relicOverlayInstance.SetActive(false);
        }

        GameObject expeditionModalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ExpeditionBoardModal.prefab");
        GameObject expeditionModalInstance = null;
        if (expeditionModalPrefab != null)
        {
            expeditionModalInstance = (GameObject)PrefabUtility.InstantiatePrefab(expeditionModalPrefab, canvasObj.transform);
            expeditionModalInstance.SetActive(false);
        }

        // Connect HUD links
        PortraitHUDController hud = canvasObj.GetComponent<PortraitHUDController>();
        SerializedObject hudSo = new SerializedObject(hud);
        hudSo.FindProperty("playerProgression").objectReferenceValue = progression;
        hudSo.FindProperty("livingGear").objectReferenceValue = livingGear;
        hudSo.FindProperty("weaponTemper").objectReferenceValue = weaponTemper;
        hudSo.FindProperty("bossController").objectReferenceValue = bossCtrl;
        hudSo.FindProperty("storeModal").objectReferenceValue = storeModalInstance;
        hudSo.FindProperty("relicOverlay").objectReferenceValue = relicOverlayInstance;
        hudSo.FindProperty("expeditionBoardModal").objectReferenceValue = expeditionModalInstance;
        hudSo.ApplyModifiedProperties();

        // Relic Inspection Rig in Stage
        GameObject relicRig = new GameObject("RelicInspectionRig");
        relicRig.transform.position = new Vector3(100f, 0f, 0f);
        GameObject relicPivot = new GameObject("RelicPivot");
        relicPivot.transform.SetParent(relicRig.transform, false);

        GameObject inspectCamObj = new GameObject("InspectionCamera", typeof(Camera));
        inspectCamObj.transform.SetParent(relicRig.transform, false);
        inspectCamObj.transform.localPosition = new Vector3(0f, 0f, -4f);
        Camera inspectCam = inspectCamObj.GetComponent<Camera>();
        inspectCam.fieldOfView = 45f;
        inspectCam.clearFlags = CameraClearFlags.SolidColor;
        inspectCam.backgroundColor = new Color(0.03f, 0.05f, 0.08f, 1f);
        inspectCam.depth = 1;
        inspectCam.enabled = false;

        RelicInspectorController inspectorCtrl = relicRig.AddComponent<RelicInspectorController>();
        inspectorCtrl.relicPivot = relicPivot.transform;
        inspectorCtrl.inspectionCamera = inspectCam;
        inspectorCtrl.inspectionUIPanel = relicOverlayInstance;

        // Managers: Gemology, Expedition, Store, Forge, Audio, TTS, Unfiltered Dialogue, Tribunal, Bardic Music, Summoning, Core & Trade
        GameObject managers = new GameObject("SystemsManager");
        managers.AddComponent<GemologySystem>();
        managers.AddComponent<NPCExpeditionManager>();
        managers.AddComponent<StoreMonetizationManager>();
        managers.AddComponent<EnchantmentForgeThreeStrike>();
        managers.AddComponent<BattleSummonerAudioMaster>();
        managers.AddComponent<ElevenLabsTTSBridge>();
        managers.AddComponent<UnfilteredDialoguePipeline>();
        managers.AddComponent<TribunalAppealController>();
        managers.AddComponent<InstrumentFreeplaySync>();
        managers.AddComponent<MelodyRecognizer>();
        SummoningRitualManager ritualMgr = managers.AddComponent<SummoningRitualManager>();
        managers.AddComponent<SummoningRitualEngine>();
        managers.AddComponent<TradeSystem>();
        managers.AddComponent<UnityThreadDispatcher>();
        managers.AddComponent<SaveSystem>();

        // Wire Summoning Ritual UI References if present in canvas hierarchy
        Transform modalTransform = canvasObj.transform.Find("SafeAreaContainer/SummoningRitualModal");
        if (modalTransform != null)
        {
            SerializedObject ritSo = new SerializedObject(ritualMgr);
            ritSo.FindProperty("ritualModalPanel").objectReferenceValue = modalTransform.gameObject;
            Transform s1 = modalTransform.Find("Step1_NomenclatureSeed");
            if (s1 != null)
            {
                ritSo.FindProperty("step1SeedContainer").objectReferenceValue = s1.gameObject;
                ritSo.FindProperty("seedInputField").objectReferenceValue = s1.Find("SeedInputFieldTMP")?.GetComponent<TMP_InputField>();
                ritSo.FindProperty("submitSeedButton").objectReferenceValue = s1.Find("SubmitSeedBtn")?.GetComponent<Button>();
            }
            Transform s2 = modalTransform.Find("Step2_LLMInquisition");
            if (s2 != null)
            {
                ritSo.FindProperty("step2InquisitionContainer").objectReferenceValue = s2.gameObject;
                ritSo.FindProperty("inquisitionQuestionText").objectReferenceValue = s2.Find("InquisitionQuestionTMP")?.GetComponent<TextMeshProUGUI>();
                ritSo.FindProperty("choiceButtonA").objectReferenceValue = s2.Find("ChoiceBtnA")?.GetComponent<Button>();
                ritSo.FindProperty("choiceButtonB").objectReferenceValue = s2.Find("ChoiceBtnB")?.GetComponent<Button>();
                ritSo.FindProperty("choiceButtonC").objectReferenceValue = s2.Find("ChoiceBtnC")?.GetComponent<Button>();
            }
            Transform s3 = modalTransform.Find("Step3_AstralGeometry");
            if (s3 != null)
            {
                ritSo.FindProperty("step3GeometryContainer").objectReferenceValue = s3.gameObject;
                ritSo.FindProperty("geometryInstructionText").objectReferenceValue = s3.Find("GeometryInstructionTMP")?.GetComponent<TextMeshProUGUI>();
            }
            Transform s4 = modalTransform.Find("Step4_StatSynthesis");
            if (s4 != null)
            {
                ritSo.FindProperty("step4SynthesisContainer").objectReferenceValue = s4.gameObject;
                ritSo.FindProperty("accuracyResultText").objectReferenceValue = s4.Find("AccuracyResultTMP")?.GetComponent<TextMeshProUGUI>();
                ritSo.FindProperty("finalizeRitualButton").objectReferenceValue = s4.Find("FinalizeBtn")?.GetComponent<Button>();
            }
            ritSo.FindProperty("closeRitualModalButton").objectReferenceValue = modalTransform.Find("CloseRitualBtn")?.GetComponent<Button>();
            ritSo.ApplyModifiedProperties();
        }

        // The Warden's Tribunal Monolith (Center Hub / Quarantine landmark)
        GameObject monolithObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        monolithObj.name = "WardensTribunalMonolith";
        monolithObj.transform.position = new Vector3(-8f, 1.8f, 0f);
        monolithObj.transform.localScale = new Vector3(1.2f, 1.8f, 1.2f);
        monolithObj.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_Cavern_Pillars", new Color(0.12f, 0.14f, 0.22f, 1f), 0.35f, 0.55f);

        // EventSystem
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    [MenuItem("Tower of Glyphs/Build Relic Inspection Testing Stage Scene")]
    public static void BuildRelicTestingScene()
    {
        CreatePuzzleCryptexPrefab();
        CreateRelicInspectionOverlayPrefab();

        string scenePath = "Assets/Scenes/RelicTestingStage.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Directional Light (Subdued for dramatic focus)
        GameObject lightObj = new GameObject("Directional Light", typeof(Light));
        Light light = lightObj.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.25f;
        light.color = new Color(0.4f, 0.45f, 0.55f);
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Relic Rig
        GameObject relicRig = new GameObject("RelicInspectionRig");
        GameObject relicPivot = new GameObject("RelicPivot");
        relicPivot.transform.SetParent(relicRig.transform, false);
        relicPivot.transform.localPosition = new Vector3(-0.25f, 0.05f, 0f);
        relicPivot.transform.localRotation = Quaternion.Euler(18f, -45f, 12f);

        // Dramatic Lighting Rig
        GameObject keyLightObj = new GameObject("KeySpotlight", typeof(Light));
        keyLightObj.transform.SetParent(relicRig.transform, false);
        keyLightObj.transform.position = new Vector3(1.6f, 3.2f, -2.4f);
        keyLightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        Light keyLight = keyLightObj.GetComponent<Light>();
        keyLight.type = LightType.Spot;
        keyLight.color = new Color(1.0f, 0.94f, 0.82f);
        keyLight.intensity = 4.5f;
        keyLight.range = 10f;
        keyLight.spotAngle = 55f;

        GameObject rimLightObj = new GameObject("RimSpotlight", typeof(Light));
        rimLightObj.transform.SetParent(relicRig.transform, false);
        rimLightObj.transform.position = new Vector3(-2.2f, 2.2f, 2.6f);
        rimLightObj.transform.rotation = Quaternion.Euler(30f, 140f, 0f);
        Light rimLight = rimLightObj.GetComponent<Light>();
        rimLight.type = LightType.Spot;
        rimLight.color = new Color(0.25f, 0.78f, 1.0f);
        rimLight.intensity = 2.8f;
        rimLight.range = 10f;
        rimLight.spotAngle = 60f;

        GameObject underglowObj = new GameObject("UnderglowLight", typeof(Light));
        underglowObj.transform.SetParent(relicRig.transform, false);
        underglowObj.transform.position = new Vector3(0f, -1.2f, 0f);
        Light underglow = underglowObj.GetComponent<Light>();
        underglow.type = LightType.Point;
        underglow.color = new Color(1.0f, 0.55f, 0.15f);
        underglow.intensity = 1.2f;
        underglow.range = 4f;

        // Dark Vignette Backdrop Quad behind relic
        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "DimmerBackdropVignette";
        backdrop.transform.SetParent(relicRig.transform, false);
        backdrop.transform.position = new Vector3(0f, 0f, 4.0f);
        backdrop.transform.localScale = new Vector3(18f, 18f, 1f);
        var bCol = backdrop.GetComponent<Collider>(); if (bCol != null) Object.DestroyImmediate(bCol);
        var bRend = backdrop.GetComponent<Renderer>();
        if (bRend != null) bRend.sharedMaterial = GetOrCreateMaterial("Mat_Relic_DimmerVignette", new Color(0.015f, 0.02f, 0.035f, 1f), 0.05f, 0.2f, null, true);

        // Inspection Camera
        GameObject camObj = new GameObject("InspectionCamera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 0.25f, -6.8f);
        camObj.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);
        Camera inspectCam = camObj.GetComponent<Camera>();
        inspectCam.fieldOfView = 38f;
        inspectCam.clearFlags = CameraClearFlags.SolidColor;
        inspectCam.backgroundColor = new Color(0.01f, 0.015f, 0.025f, 1f);
        inspectCam.allowMSAA = true;
        inspectCam.allowHDR = true;

        UniversalAdditionalCameraData camData = camObj.GetComponent<UniversalAdditionalCameraData>();
        camData.renderType = CameraRenderType.Base;
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camData.antialiasingQuality = AntialiasingQuality.High;

        // Spawn Cryptex under pivot
        GameObject cryptexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Relics/PuzzleCryptex.prefab");
        if (cryptexPrefab != null)
        {
            PrefabUtility.InstantiatePrefab(cryptexPrefab, relicPivot.transform);
        }

        // Canvas for Testing Stage
        GameObject canvasObj = new GameObject("StageCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = inspectCam;
        canvas.planeDistance = 2.0f;

        float frustumHeight = 2.0f * 2.0f * Mathf.Tan(inspectCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float initialScale = frustumHeight / 1920f;
        canvasObj.transform.localScale = new Vector3(initialScale, initialScale, initialScale);

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;
        scaler.dynamicPixelsPerUnit = 2f;

        GameObject safeArea = new GameObject("SafeAreaContainer", typeof(RectTransform), typeof(AndroidSafeAreaFitter));
        safeArea.transform.SetParent(canvasObj.transform, false);
        RectTransform safeAreaRt = safeArea.GetComponent<RectTransform>();
        safeAreaRt.anchorMin = Vector2.zero;
        safeAreaRt.anchorMax = Vector2.one;
        safeAreaRt.offsetMin = Vector2.zero;
        safeAreaRt.offsetMax = Vector2.zero;

        // Gesture canvas active in RelicInspection mode
        GameObject gestureObj = new GameObject("RelicGestureCanvas", typeof(RectTransform), typeof(Image), typeof(PortraitGestureCanvas));
        gestureObj.transform.SetParent(safeArea.transform, false);
        RectTransform gestureRt = gestureObj.GetComponent<RectTransform>();
        gestureRt.anchorMin = Vector2.zero;
        gestureRt.anchorMax = Vector2.one;
        gestureRt.offsetMin = Vector2.zero;
        gestureRt.offsetMax = Vector2.zero;
        Image gImg = gestureObj.GetComponent<Image>();
        gImg.color = new Color(0f, 0f, 0f, 0f);
        gImg.raycastTarget = true;
        PortraitGestureCanvas gCanvas = gestureObj.GetComponent<PortraitGestureCanvas>();
        gCanvas.currentMode = InteractionMode.RelicInspection;

        // Overlay UI
        GameObject overlayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/RelicInspectionOverlay.prefab");
        GameObject overlayInstance = null;
        if (overlayPrefab != null)
        {
            overlayInstance = (GameObject)PrefabUtility.InstantiatePrefab(overlayPrefab, safeArea.transform);
        }

        RelicInspectorController inspectorCtrl = relicRig.AddComponent<RelicInspectorController>();
        inspectorCtrl.relicPivot = relicPivot.transform;
        inspectorCtrl.inspectionCamera = inspectCam;
        inspectorCtrl.inspectionUIPanel = overlayInstance;
        inspectorCtrl.keySpotlight = keyLight;
        inspectorCtrl.rimSpotlight = rimLight;
        inspectorCtrl.underglowLight = underglow;
        if (overlayInstance != null)
        {
            inspectorCtrl.dimmerCanvasGroup = overlayInstance.GetComponent<CanvasGroup>();
            inspectorCtrl.dimmerVignetteImage = overlayInstance.GetComponent<Image>();
        }

        // Add Audio Master & ASMR Synthesizer
        GameObject audioObj = new GameObject("AudioMaster", typeof(BattleSummonerAudioMaster));

        // EventSystem
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[Tower of Glyphs] RelicTestingStage scene successfully built with 3D tactile puzzle, dramatic lighting, and ASMR rig.");
    }

    [MenuItem("Tower of Glyphs/Build Multi-Character Dialogue Hub Scene")]
    public static void BuildMultiCharacterDialogueScene()
    {
        string scenePath = "Assets/Scenes/MultiCharacterDialogueHub.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Directional Light (atmospheric night ambience)
        GameObject dirLightObj = new GameObject("Directional Light", typeof(Light));
        Light dirLight = dirLightObj.GetComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = new Color(0.4f, 0.45f, 0.6f);
        dirLight.intensity = 0.5f;
        dirLightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Campfire Stage Floor
        GameObject stageFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        stageFloor.name = "CampfireFloor";
        stageFloor.transform.position = Vector3.zero;
        stageFloor.transform.localScale = new Vector3(3f, 1f, 3f);
        Renderer floorRend = stageFloor.GetComponent<Renderer>();
        if (floorRend != null)
        {
            floorRend.sharedMaterial = GetOrCreateMaterial("Mat_Campfire_Floor", new Color(0.08f, 0.09f, 0.12f, 1f), 0.05f, 0.35f);
        }

        // Surrounding Cave/Spire Pillars in background
        for (int i = 0; i < 4; i++)
        {
            float angle = (i * Mathf.PI / 2f) + 0.35f;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 14f, 6f, Mathf.Sin(angle) * 14f);
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"BackgroundPillar_{i + 1}";
            pillar.transform.position = pos;
            pillar.transform.localScale = new Vector3(2.5f, 12f, 2.5f);
            Renderer pRend = pillar.GetComponent<Renderer>();
            if (pRend != null)
            {
                pRend.sharedMaterial = GetOrCreateMaterial("Mat_Cavern_Pillars", new Color(0.12f, 0.14f, 0.18f, 1f), 0.35f, 0.55f);
            }
        }

        // Surrounding Cavern Backdrop Wall
        GameObject cavernDome = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cavernDome.name = "CavernBackdropWall";
        cavernDome.transform.position = new Vector3(0f, 6f, 3f);
        cavernDome.transform.localScale = new Vector3(26f, 12f, 26f);
        var cdCol = cavernDome.GetComponent<Collider>(); if (cdCol != null) Object.DestroyImmediate(cdCol);
        var cdRend = cavernDome.GetComponent<Renderer>();
        if (cdRend != null) cdRend.sharedMaterial = GetOrCreateMaterial("Mat_Cavern_Dome", new Color(0.05f, 0.06f, 0.08f, 1f), 0.02f, 0.20f);

        // Central Campfire Hearth
        GameObject hearth = new GameObject("CampfireHearth");
        hearth.transform.position = Vector3.zero;

        // Hearth Stones Ring
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            Vector3 stonePos = new Vector3(Mathf.Cos(angle) * 0.9f, 0.15f, Mathf.Sin(angle) * 0.9f);
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = $"HearthStone_{i + 1}";
            stone.transform.SetParent(hearth.transform, false);
            stone.transform.position = stonePos;
            stone.transform.localScale = new Vector3(0.45f, 0.35f, 0.45f);
            Renderer sRend = stone.GetComponent<Renderer>();
            if (sRend != null)
            {
                sRend.sharedMaterial = GetOrCreateMaterial("Mat_Campfire_Stones", new Color(0.22f, 0.24f, 0.27f, 1f), 0.08f, 0.40f);
            }
        }

        // Glowing Embers
        GameObject embers = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        embers.name = "GlowingEmbers";
        embers.transform.SetParent(hearth.transform, false);
        embers.transform.position = new Vector3(0f, 0.1f, 0f);
        embers.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
        Renderer emberRend = embers.GetComponent<Renderer>();
        if (emberRend != null)
        {
            emberRend.sharedMaterial = GetOrCreateMaterial("Mat_Campfire_Embers", new Color(0.25f, 0.08f, 0.02f, 1f), 0.05f, 0.30f, new Color(1.0f, 0.42f, 0.04f) * 4.2f);
        }

        // Campfire Flame Light
        GameObject fireLightObj = new GameObject("HearthFireLight", typeof(Light));
        fireLightObj.transform.SetParent(hearth.transform, false);
        fireLightObj.transform.position = new Vector3(0f, 0.6f, 0f);
        Light fireLight = fireLightObj.GetComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f, 0.6f, 0.2f);
        fireLight.intensity = 3.2f;
        fireLight.range = 10f;
        fireLightObj.AddComponent<CampfireFlickerLight>();

        // Campfire Sparks Particle System
        GameObject sparksObj = new GameObject("CampfireSparks", typeof(ParticleSystem));
        sparksObj.transform.SetParent(hearth.transform, false);
        sparksObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        ParticleSystem sparksPs = sparksObj.GetComponent<ParticleSystem>();
        var psMain = sparksPs.main;
        psMain.startLifetime = 2.0f;
        psMain.startSpeed = 1.2f;
        psMain.startSize = 0.07f;
        psMain.startColor = new Color(1f, 0.55f, 0.1f, 0.9f);
        psMain.maxParticles = 60;
        psMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var psEmission = sparksPs.emission;
        psEmission.rateOverTime = 18f;
        var psShape = sparksPs.shape;
        psShape.shapeType = ParticleSystemShapeType.Circle;
        psShape.radius = 0.45f;
        var psVel = sparksPs.velocityOverLifetime;
        psVel.enabled = true;
        psVel.y = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);

        // Global Post-Processing Volume (Tonemapping, Bloom, Vignette, Color Grading)
        GameObject volumeObj = new GameObject("GlobalPostProcessingVolume", typeof(Volume));
        Volume vol = volumeObj.GetComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;

        string volProfilePath = "Assets/Settings/DialogueHub_VolumeProfile.asset";
        VolumeProfile volProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volProfilePath);
        if (volProfile == null)
        {
            volProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(volProfile, volProfilePath);
        }

        if (!volProfile.TryGet<Tonemapping>(out var tonemapping)) tonemapping = volProfile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        if (!volProfile.TryGet<Bloom>(out var bloom)) bloom = volProfile.Add<Bloom>(true);
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(0.85f);
        bloom.scatter.Override(0.7f);

        if (!volProfile.TryGet<Vignette>(out var vignette)) vignette = volProfile.Add<Vignette>(true);
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.35f);

        if (!volProfile.TryGet<ColorAdjustments>(out var colorAdj)) colorAdj = volProfile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.Override(0.18f);
        colorAdj.contrast.Override(14f);

        EditorUtility.SetDirty(volProfile);
        vol.sharedProfile = volProfile;

        // Player Observer Reference Transform
        GameObject playerObserver = new GameObject("PlayerObserver");
        playerObserver.transform.position = new Vector3(0f, 1.2f, -3.8f);

        // Main Camera Rig
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 1.85f, -4.2f);
        camObj.transform.rotation = Quaternion.Euler(11f, 0f, 0f);
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
        cam.fieldOfView = 56f;
        cam.allowMSAA = true;
        cam.allowHDR = true;
        camObj.AddComponent<BattleSummoner.Dialogue.CampfirePanoramicCameraController>();

        UniversalAdditionalCameraData camData = camObj.GetComponent<UniversalAdditionalCameraData>();
        camData.renderType = CameraRenderType.Base;
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camData.antialiasingQuality = AntialiasingQuality.High;

        // Load NPC ScriptableObjects
        BespokeNPCObject margoData = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>("Assets/Settings/NPC_Margo_Data.asset");
        BespokeNPCObject barnabyData = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>("Assets/Settings/NPC_Barnaby_Data.asset");
        BespokeNPCObject zephyrData = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>("Assets/Settings/NPC_Zephyr_Data.asset");

        // Margo
        GameObject margoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Margo.prefab");
        GameObject margoInst = (margoPrefab != null) ? (GameObject)PrefabUtility.InstantiatePrefab(margoPrefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        margoInst.name = "NPC_Margo";
        margoInst.transform.position = new Vector3(-2.0f, 0f, 0.4f);
        margoInst.transform.rotation = Quaternion.Euler(0f, 115f, 0f);
        if (margoInst.GetComponent<Collider>() == null)
        {
            var col = margoInst.AddComponent<CapsuleCollider>();
            col.height = 1.8f; col.radius = 0.45f; col.center = new Vector3(0, 0.9f, 0);
        }

        GameObject margoSpotObj = new GameObject("MargoSpotlight", typeof(Light));
        margoSpotObj.transform.SetParent(margoInst.transform, false);
        margoSpotObj.transform.localPosition = new Vector3(0f, 3.2f, 0.5f);
        margoSpotObj.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
        Light margoSpot = margoSpotObj.GetComponent<Light>();
        margoSpot.type = LightType.Spot;
        margoSpot.color = new Color(0.75f, 0.4f, 0.95f);
        margoSpot.intensity = 1.2f;
        margoSpot.spotAngle = 60f;
        margoSpot.range = 6f;

        GameObject margoBubbleObj = Create3DSpeechBubble(margoInst.transform, new Color(0.75f, 0.4f, 0.95f));
        TextMeshPro margoBubbleTmp = margoBubbleObj.GetComponentInChildren<TextMeshPro>();

        // Barnaby
        GameObject barnabyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Barnaby.prefab");
        GameObject barnabyInst = (barnabyPrefab != null) ? (GameObject)PrefabUtility.InstantiatePrefab(barnabyPrefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        barnabyInst.name = "NPC_Barnaby";
        barnabyInst.transform.position = new Vector3(0f, 0f, 2.0f);
        barnabyInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        if (barnabyInst.GetComponent<Collider>() == null)
        {
            var col = barnabyInst.AddComponent<CapsuleCollider>();
            col.height = 1.8f; col.radius = 0.45f; col.center = new Vector3(0, 0.9f, 0);
        }

        GameObject barnabySpotObj = new GameObject("BarnabySpotlight", typeof(Light));
        barnabySpotObj.transform.SetParent(barnabyInst.transform, false);
        barnabySpotObj.transform.localPosition = new Vector3(0f, 3.2f, 0.5f);
        barnabySpotObj.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
        Light barnabySpot = barnabySpotObj.GetComponent<Light>();
        barnabySpot.type = LightType.Spot;
        barnabySpot.color = new Color(1f, 0.65f, 0.2f);
        barnabySpot.intensity = 1.2f;
        barnabySpot.spotAngle = 60f;
        barnabySpot.range = 6f;

        GameObject barnabyBubbleObj = Create3DSpeechBubble(barnabyInst.transform, new Color(1f, 0.65f, 0.2f));
        TextMeshPro barnabyBubbleTmp = barnabyBubbleObj.GetComponentInChildren<TextMeshPro>();

        // Zephyr
        GameObject zephyrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Zephyr.prefab");
        GameObject zephyrInst = (zephyrPrefab != null) ? (GameObject)PrefabUtility.InstantiatePrefab(zephyrPrefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        zephyrInst.name = "NPC_Zephyr";
        zephyrInst.transform.position = new Vector3(2.0f, 0f, 0.4f);
        zephyrInst.transform.rotation = Quaternion.Euler(0f, -115f, 0f);
        if (zephyrInst.GetComponent<Collider>() == null)
        {
            var col = zephyrInst.AddComponent<CapsuleCollider>();
            col.height = 1.8f; col.radius = 0.45f; col.center = new Vector3(0, 0.9f, 0);
        }

        GameObject zephyrSpotObj = new GameObject("ZephyrSpotlight", typeof(Light));
        zephyrSpotObj.transform.SetParent(zephyrInst.transform, false);
        zephyrSpotObj.transform.localPosition = new Vector3(0f, 3.2f, 0.5f);
        zephyrSpotObj.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
        Light zephyrSpot = zephyrSpotObj.GetComponent<Light>();
        zephyrSpot.type = LightType.Spot;
        zephyrSpot.color = new Color(0.2f, 0.85f, 0.75f);
        zephyrSpot.intensity = 1.2f;
        zephyrSpot.spotAngle = 60f;
        zephyrSpot.range = 6f;

        GameObject zephyrBubbleObj = Create3DSpeechBubble(zephyrInst.transform, new Color(0.2f, 0.85f, 0.75f));
        TextMeshPro zephyrBubbleTmp = zephyrBubbleObj.GetComponentInChildren<TextMeshPro>();

        // UI Canvas Hierarchy
        GameObject canvasObj = new GameObject("CampfireDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;
        scaler.dynamicPixelsPerUnit = 2f;

        GameObject safeArea = new GameObject("SafeAreaContainer", typeof(RectTransform), typeof(AndroidSafeAreaFitter));
        safeArea.transform.SetParent(canvasObj.transform, false);
        RectTransform safeAreaRt = safeArea.GetComponent<RectTransform>();
        safeAreaRt.anchorMin = Vector2.zero;
        safeAreaRt.anchorMax = Vector2.one;
        safeAreaRt.offsetMin = Vector2.zero;
        safeAreaRt.offsetMax = Vector2.zero;

        // Top Header
        GameObject headerObj = new GameObject("TopHeaderBar", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(safeArea.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 0.925f);
        headerRt.anchorMax = new Vector2(1f, 0.995f);
        headerRt.offsetMin = Vector2.zero;
        headerRt.offsetMax = Vector2.zero;
        Image headerImg = headerObj.GetComponent<Image>();
        headerImg.color = new Color(0.06f, 0.07f, 0.12f, 0.92f);

        GameObject titleObj = new GameObject("TitleTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(-120f, 20f);
        titleRt.sizeDelta = new Vector2(680f, 38f);
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "[CAMPFIRE ARCHIVE: THE SPIRE TRIAD]";
        titleTmp.fontSize = 28;
        titleTmp.color = new Color(1f, 0.85f, 0.3f, 1f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        GameObject speakerHeaderObj = new GameObject("ActiveSpeakerHeaderTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        speakerHeaderObj.transform.SetParent(headerObj.transform, false);
        RectTransform spkRt = speakerHeaderObj.GetComponent<RectTransform>();
        spkRt.anchoredPosition = new Vector2(-120f, -18f);
        spkRt.sizeDelta = new Vector2(680f, 32f);
        TextMeshProUGUI speakerHeaderTmp = speakerHeaderObj.GetComponent<TextMeshProUGUI>();
        speakerHeaderTmp.text = "<color=#88DDFF>[LISTENING]</color> Delvers conversing at the hearth...";
        speakerHeaderTmp.fontSize = 24;
        speakerHeaderTmp.color = Color.white;
        speakerHeaderTmp.alignment = TextAlignmentOptions.Center;

        GameObject returnBtnObj = CreateTMPButtonObject(headerObj.transform, "ReturnToDungeonBtn", "RETURN >>", new Vector2(400f, 0f), new Vector2(210f, 65f), new Color(0.65f, 0.2f, 0.2f, 1f), 24);
        Button returnBtn = returnBtnObj.GetComponent<Button>();

        // Central Transcript Scroll View
        GameObject transcriptPanelObj = new GameObject("TranscriptPanel", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        transcriptPanelObj.transform.SetParent(safeArea.transform, false);
        RectTransform transRt = transcriptPanelObj.GetComponent<RectTransform>();
        transRt.anchorMin = new Vector2(0.04f, 0.58f);
        transRt.anchorMax = new Vector2(0.96f, 0.915f);
        transRt.offsetMin = Vector2.zero;
        transRt.offsetMax = Vector2.zero;
        Image transBg = transcriptPanelObj.GetComponent<Image>();
        transBg.color = new Color(0.04f, 0.05f, 0.09f, 0.58f);

        ScrollRect scrollRect = transcriptPanelObj.GetComponent<ScrollRect>();

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObj.transform.SetParent(transcriptPanelObj.transform, false);
        RectTransform vpRt = viewportObj.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(16f, 16f);
        vpRt.offsetMax = new Vector2(-16f, -16f);
        Image vpImg = viewportObj.GetComponent<Image>();
        vpImg.color = Color.white;
        Mask vpMask = viewportObj.GetComponent<Mask>();
        vpMask.showMaskGraphic = false;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject transcriptTextObj = new GameObject("TranscriptTextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        transcriptTextObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI transcriptTmp = transcriptTextObj.GetComponent<TextMeshProUGUI>();
        transcriptTmp.fontSize = 26;
        transcriptTmp.color = Color.white;
        transcriptTmp.enableWordWrapping = true;
        transcriptTmp.richText = true;
        transcriptTmp.text = "<color=#666688><i>[Campfire embers crackle as the delvers assemble around the hearth...]</i></color>\n\n";

        scrollRect.content = contentRt;
        scrollRect.viewport = vpRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Quick Prompt Chips (2x2 Grid)
        GameObject chipsPanel = new GameObject("QuickChipsPanel", typeof(RectTransform));
        chipsPanel.transform.SetParent(safeArea.transform, false);
        RectTransform chipsRt = chipsPanel.GetComponent<RectTransform>();
        chipsRt.anchorMin = new Vector2(0.04f, 0.22f);
        chipsRt.anchorMax = new Vector2(0.96f, 0.32f);
        chipsRt.offsetMin = Vector2.zero;
        chipsRt.offsetMax = Vector2.zero;

        GameObject q1Obj = CreateTMPButtonObject(chipsPanel.transform, "Chip1_Floor50", "Floor 50 Dangers?", new Vector2(-240f, 36f), new Vector2(460f, 60f), new Color(0.2f, 0.3f, 0.45f, 1f), 22);
        GameObject q2Obj = CreateTMPButtonObject(chipsPanel.transform, "Chip2_Geode", "Margo: Geode Secrets", new Vector2(240f, 36f), new Vector2(460f, 60f), new Color(0.4f, 0.2f, 0.5f, 1f), 22);
        GameObject q3Obj = CreateTMPButtonObject(chipsPanel.transform, "Chip3_Blade", "Barnaby: Inspect Blade", new Vector2(-240f, -36f), new Vector2(460f, 60f), new Color(0.5f, 0.35f, 0.15f, 1f), 22);
        GameObject q4Obj = CreateTMPButtonObject(chipsPanel.transform, "Chip4_Scout", "Zephyr: Recon Report", new Vector2(240f, -36f), new Vector2(460f, 60f), new Color(0.15f, 0.45f, 0.4f, 1f), 22);

        // Bottom Input Deck
        GameObject inputDeck = new GameObject("BottomInputDeck", typeof(RectTransform), typeof(Image));
        inputDeck.transform.SetParent(safeArea.transform, false);
        RectTransform deckRt = inputDeck.GetComponent<RectTransform>();
        deckRt.anchorMin = new Vector2(0.04f, 0.02f);
        deckRt.anchorMax = new Vector2(0.96f, 0.21f);
        deckRt.offsetMin = Vector2.zero;
        deckRt.offsetMax = Vector2.zero;
        Image deckImg = inputDeck.GetComponent<Image>();
        deckImg.color = new Color(0.06f, 0.07f, 0.12f, 0.9f);

        // Input Field
        GameObject inputFieldObj = new GameObject("PlayerMessageInputFieldTMP", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputFieldObj.transform.SetParent(inputDeck.transform, false);
        RectTransform inRt = inputFieldObj.GetComponent<RectTransform>();
        inRt.anchoredPosition = new Vector2(0f, 75f);
        inRt.sizeDelta = new Vector2(940f, 85f);
        Image inBg = inputFieldObj.GetComponent<Image>();
        inBg.color = new Color(0.12f, 0.14f, 0.22f, 1f);

        GameObject inTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        inTextObj.transform.SetParent(inputFieldObj.transform, false);
        RectTransform inTxtRt = inTextObj.GetComponent<RectTransform>();
        inTxtRt.anchorMin = Vector2.zero;
        inTxtRt.anchorMax = Vector2.one;
        inTxtRt.offsetMin = new Vector2(16f, 0f);
        inTxtRt.offsetMax = new Vector2(-16f, 0f);
        TextMeshProUGUI inTmp = inTextObj.GetComponent<TextMeshProUGUI>();
        inTmp.fontSize = 26;
        inTmp.color = Color.white;

        GameObject inPlaceholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        inPlaceholderObj.transform.SetParent(inputFieldObj.transform, false);
        RectTransform inPlaceRt = inPlaceholderObj.GetComponent<RectTransform>();
        inPlaceRt.anchorMin = Vector2.zero;
        inPlaceRt.anchorMax = Vector2.one;
        inPlaceRt.offsetMin = new Vector2(16f, 0f);
        inPlaceRt.offsetMax = new Vector2(-16f, 0f);
        TextMeshProUGUI inPlaceTmp = inPlaceholderObj.GetComponent<TextMeshProUGUI>();
        inPlaceTmp.text = "Speak to the delvers or ask a question...";
        inPlaceTmp.fontSize = 26;
        inPlaceTmp.color = new Color(0.6f, 0.65f, 0.75f, 0.6f);

        TMP_InputField inputField = inputFieldObj.GetComponent<TMP_InputField>();
        inputField.textComponent = inTmp;
        inputField.placeholder = inPlaceTmp;

        // Send Button & Toggle Banter Button
        GameObject sendBtnObj = CreateTMPButtonObject(inputDeck.transform, "SendPlayerMessageBtn", "SPEAK / SEND >>", new Vector2(-240f, -40f), new Vector2(450f, 80f), new Color(0.18f, 0.55f, 0.3f, 1f), 26);
        GameObject toggleBtnObj = CreateTMPButtonObject(inputDeck.transform, "ToggleBanterBtn", "PAUSE BANTER", new Vector2(240f, -40f), new Vector2(450f, 80f), new Color(0.35f, 0.22f, 0.6f, 1f), 26);
        TextMeshProUGUI toggleBtnTmp = toggleBtnObj.GetComponentInChildren<TextMeshProUGUI>();

        // DialogueHubController & Script wiring
        GameObject hubObj = new GameObject("DialogueHubController");
        MultiCharacterConversationHub hub = hubObj.AddComponent<MultiCharacterConversationHub>();
        hubObj.AddComponent<BattleSummoner.Dialogue.CampfireDualDialogueController>();
        SerializedObject hubSo = new SerializedObject(hub);

        SerializedProperty actorsProp = hubSo.FindProperty("actors");
        actorsProp.arraySize = 3;

        // Margo
        SerializedProperty a0 = actorsProp.GetArrayElementAtIndex(0);
        a0.FindPropertyRelative("npcId").stringValue = "npc_margo";
        a0.FindPropertyRelative("displayName").stringValue = "Margo";
        a0.FindPropertyRelative("themeColor").colorValue = new Color(0.75f, 0.4f, 0.95f, 1f);
        a0.FindPropertyRelative("profile").objectReferenceValue = margoData;
        a0.FindPropertyRelative("characterRoot").objectReferenceValue = margoInst.transform;
        a0.FindPropertyRelative("meshRenderer").objectReferenceValue = margoInst.GetComponentInChildren<Renderer>();
        a0.FindPropertyRelative("spotlight").objectReferenceValue = margoSpot;
        a0.FindPropertyRelative("speechBubbleContainer").objectReferenceValue = margoBubbleObj;
        a0.FindPropertyRelative("speechBubbleText").objectReferenceValue = margoBubbleTmp;

        // Barnaby
        SerializedProperty a1 = actorsProp.GetArrayElementAtIndex(1);
        a1.FindPropertyRelative("npcId").stringValue = "npc_barnaby";
        a1.FindPropertyRelative("displayName").stringValue = "Barnaby";
        a1.FindPropertyRelative("themeColor").colorValue = new Color(1f, 0.65f, 0.2f, 1f);
        a1.FindPropertyRelative("profile").objectReferenceValue = barnabyData;
        a1.FindPropertyRelative("characterRoot").objectReferenceValue = barnabyInst.transform;
        a1.FindPropertyRelative("meshRenderer").objectReferenceValue = barnabyInst.GetComponentInChildren<Renderer>();
        a1.FindPropertyRelative("spotlight").objectReferenceValue = barnabySpot;
        a1.FindPropertyRelative("speechBubbleContainer").objectReferenceValue = barnabyBubbleObj;
        a1.FindPropertyRelative("speechBubbleText").objectReferenceValue = barnabyBubbleTmp;

        // Zephyr
        SerializedProperty a2 = actorsProp.GetArrayElementAtIndex(2);
        a2.FindPropertyRelative("npcId").stringValue = "npc_zephyr";
        a2.FindPropertyRelative("displayName").stringValue = "Zephyr";
        a2.FindPropertyRelative("themeColor").colorValue = new Color(0.2f, 0.85f, 0.75f, 1f);
        a2.FindPropertyRelative("profile").objectReferenceValue = zephyrData;
        a2.FindPropertyRelative("characterRoot").objectReferenceValue = zephyrInst.transform;
        a2.FindPropertyRelative("meshRenderer").objectReferenceValue = zephyrInst.GetComponentInChildren<Renderer>();
        a2.FindPropertyRelative("spotlight").objectReferenceValue = zephyrSpot;
        a2.FindPropertyRelative("speechBubbleContainer").objectReferenceValue = zephyrBubbleObj;
        a2.FindPropertyRelative("speechBubbleText").objectReferenceValue = zephyrBubbleTmp;

        // Wire other serialized properties
        hubSo.FindProperty("playerObserverTransform").objectReferenceValue = playerObserver.transform;
        hubSo.FindProperty("transcriptLogText").objectReferenceValue = transcriptTmp;
        hubSo.FindProperty("transcriptScrollRect").objectReferenceValue = scrollRect;
        hubSo.FindProperty("activeSpeakerHeader").objectReferenceValue = speakerHeaderTmp;
        hubSo.FindProperty("playerMessageInput").objectReferenceValue = inputField;
        hubSo.FindProperty("sendPlayerMessageButton").objectReferenceValue = sendBtnObj.GetComponent<Button>();
        hubSo.FindProperty("toggleBanterButton").objectReferenceValue = toggleBtnObj.GetComponent<Button>();
        hubSo.FindProperty("toggleBanterButtonText").objectReferenceValue = toggleBtnTmp;
        hubSo.FindProperty("returnToDungeonButton").objectReferenceValue = returnBtn;
        hubSo.FindProperty("quickPromptBtn1").objectReferenceValue = q1Obj.GetComponent<Button>();
        hubSo.FindProperty("quickPromptBtn2").objectReferenceValue = q2Obj.GetComponent<Button>();
        hubSo.FindProperty("quickPromptBtn3").objectReferenceValue = q3Obj.GetComponent<Button>();
        hubSo.FindProperty("quickPromptBtn4").objectReferenceValue = q4Obj.GetComponent<Button>();
        hubSo.ApplyModifiedProperties();

        // Systems Managers
        GameObject systemsMgr = new GameObject("SystemsManager");
        systemsMgr.AddComponent<BattleSummonerAudioMaster>();
        systemsMgr.AddComponent<ElevenLabsTTSBridge>();
        systemsMgr.AddComponent<UnfilteredDialoguePipeline>();
        systemsMgr.AddComponent<UnityThreadDispatcher>();

        // Event System
        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[Tower of Glyphs] MultiCharacterDialogueHub scene successfully created and saved at: " + scenePath);
    }

    private static GameObject Create3DSpeechBubble(Transform parent, Color themeColor)
    {
        GameObject bubbleRoot = new GameObject("SpeechBubbleContainer");
        bubbleRoot.transform.SetParent(parent, false);
        bubbleRoot.transform.localPosition = new Vector3(0f, 2.5f, 0f);

        // Background billboard panel
        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgObj.name = "BubbleBackground";
        bgObj.transform.SetParent(bubbleRoot.transform, false);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(3.2f, 1.2f, 1f);
        Collider col = bgObj.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        Renderer bgRend = bgObj.GetComponent<Renderer>();
        if (bgRend != null)
        {
            bgRend.sharedMaterial = GetOrCreateMaterial("Mat_SpeechBubble_Glass", new Color(0.04f, 0.05f, 0.09f, 0.88f), 0.1f, 0.85f, null, true);
        }

        // TextMeshPro component
        GameObject textObj = new GameObject("SpeechText", typeof(TextMeshPro));
        textObj.transform.SetParent(bubbleRoot.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(3.0f, 1.0f);

        TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
        tmp.fontSize = 2.4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.color = Color.white;
        tmp.text = "...";

        bubbleRoot.SetActive(false);
        return bubbleRoot;
    }

    private static GameObject CreateTMPButtonObject(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color btnColor, int fontSize = 24)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.color = btnColor;

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject textObj = new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(obj.transform, false);
        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(8f, 4f);
        txtRt.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 18;
        tmp.fontSizeMax = fontSize > 28 ? fontSize : 28;

        return obj;
    }

    private static void UpdateBuildSettingsScenes()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/OverworldMap.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/CampfireHub.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainDungeonFloor.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/RelicTestingStage.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/WorldEntryExperience.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/ShopInterior.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GuildHallInterior.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/BuildingInterior.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MultiCharacterDialogueHub.unity", false)
        };
        EditorBuildSettings.scenes = scenes;
    }

    private static GameObject CreateTextObject(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Text txt = obj.GetComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return obj;
    }

    private static GameObject CreateButtonObject(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color btnColor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.color = btnColor;

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        CreateTextObject(obj.transform, "Label", label, 24, TextAnchor.MiddleCenter, Vector2.zero, size, Color.white);
        return obj;
    }

    private static GameObject CreateSliderObject(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor, string label)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 0.7f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = fillColor;

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.targetGraphic = fillImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        CreateTextObject(sliderObj.transform, "Label", label, 18, TextAnchor.MiddleCenter, Vector2.zero, size, Color.white);

        return sliderObj;
    }

    private static GameObject CreateSummoningRitualModal(Transform parent)
    {
        GameObject modal = new GameObject("SummoningRitualModal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(parent, false);
        RectTransform rt = modal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.04f, 0.08f);
        rt.anchorMax = new Vector2(0.96f, 0.92f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image bg = modal.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.07f, 0.12f, 0.96f);

        // Header Title
        CreateTextObject(modal.transform, "ModalTitle", "★ THE SUMMONING RITUAL ★\n(Patronus Engine)", 30, TextAnchor.MiddleCenter, new Vector2(0f, 850f), new Vector2(800f, 100f), new Color(0.9f, 0.7f, 1f, 1f));

        // Close Button
        CreateButtonObject(modal.transform, "CloseRitualBtn", "CLOSE", new Vector2(0f, -850f), new Vector2(300f, 80f), new Color(0.4f, 0.15f, 0.2f, 1f));

        // Step 1 Container
        GameObject step1 = new GameObject("Step1_NomenclatureSeed", typeof(RectTransform));
        step1.transform.SetParent(modal.transform, false);
        RectTransform s1Rt = step1.GetComponent<RectTransform>();
        s1Rt.anchorMin = Vector2.zero;
        s1Rt.anchorMax = Vector2.one;
        s1Rt.offsetMin = Vector2.zero;
        s1Rt.offsetMax = Vector2.zero;

        CreateTextObject(step1.transform, "Step1Prompt", "STEP 1: NOMENCLATURE SEED\n\nInscribe the name or concept of your summon:\n(e.g., 'Aegis', 'Ignis', 'Bob', 'Sylph')", 26, TextAnchor.MiddleCenter, new Vector2(0f, 300f), new Vector2(800f, 200f), Color.white);

        GameObject inputFieldObj = new GameObject("SeedInputFieldTMP", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputFieldObj.transform.SetParent(step1.transform, false);
        RectTransform inRt = inputFieldObj.GetComponent<RectTransform>();
        inRt.anchoredPosition = new Vector2(0f, 50f);
        inRt.sizeDelta = new Vector2(600f, 90f);
        Image inBg = inputFieldObj.GetComponent<Image>();
        inBg.color = new Color(0.12f, 0.14f, 0.22f, 1f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(inputFieldObj.transform, false);
        RectTransform txtRt = textObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(16f, 0f);
        txtRt.offsetMax = new Vector2(-16f, 0f);
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 32;
        tmp.color = Color.cyan;
        tmp.text = "Aegis";

        TMP_InputField inputField = inputFieldObj.GetComponent<TMP_InputField>();
        inputField.textComponent = tmp;
        inputField.text = "Aegis";

        CreateButtonObject(step1.transform, "SubmitSeedBtn", "INITIATE INQUISITION >>", new Vector2(0f, -120f), new Vector2(500f, 100f), new Color(0.3f, 0.2f, 0.6f, 1f));

        // Step 2 Container
        GameObject step2 = new GameObject("Step2_LLMInquisition", typeof(RectTransform));
        step2.transform.SetParent(modal.transform, false);
        RectTransform s2Rt = step2.GetComponent<RectTransform>();
        s2Rt.anchorMin = Vector2.zero;
        s2Rt.anchorMax = Vector2.one;
        s2Rt.offsetMin = Vector2.zero;
        s2Rt.offsetMax = Vector2.zero;
        step2.SetActive(false);

        GameObject qTxtObj = new GameObject("InquisitionQuestionTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        qTxtObj.transform.SetParent(step2.transform, false);
        RectTransform qRt = qTxtObj.GetComponent<RectTransform>();
        qRt.anchoredPosition = new Vector2(0f, 350f);
        qRt.sizeDelta = new Vector2(850f, 250f);
        TextMeshProUGUI qTmp = qTxtObj.GetComponent<TextMeshProUGUI>();
        qTmp.fontSize = 26;
        qTmp.color = new Color(1f, 0.85f, 0.3f, 1f);
        qTmp.alignment = TextAlignmentOptions.Center;
        qTmp.text = "[THE INQUISITION: 1/3]\nWhen the cosmic spire collapses under entropic weight, where does your summon's essence retreat?";

        CreateButtonObject(step2.transform, "ChoiceBtnA", "A) Into the roaring abyssal flames (Fire)", new Vector2(0f, 100f), new Vector2(800f, 90f), new Color(0.2f, 0.25f, 0.4f, 1f));
        CreateButtonObject(step2.transform, "ChoiceBtnB", "B) Into the silent gravitational void (Void)", new Vector2(0f, -30f), new Vector2(800f, 90f), new Color(0.2f, 0.25f, 0.4f, 1f));
        CreateButtonObject(step2.transform, "ChoiceBtnC", "C) Into the unyielding stone bedrock (Earth)", new Vector2(0f, -160f), new Vector2(800f, 90f), new Color(0.2f, 0.25f, 0.4f, 1f));

        // Step 3 Container
        GameObject step3 = new GameObject("Step3_AstralGeometry", typeof(RectTransform));
        step3.transform.SetParent(modal.transform, false);
        RectTransform s3Rt = step3.GetComponent<RectTransform>();
        s3Rt.anchorMin = Vector2.zero;
        s3Rt.anchorMax = Vector2.one;
        s3Rt.offsetMin = Vector2.zero;
        s3Rt.offsetMax = Vector2.zero;
        step3.SetActive(false);

        GameObject gTxtObj = new GameObject("GeometryInstructionTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        gTxtObj.transform.SetParent(step3.transform, false);
        RectTransform gRt = gTxtObj.GetComponent<RectTransform>();
        gRt.anchoredPosition = new Vector2(0f, 200f);
        gRt.sizeDelta = new Vector2(850f, 400f);
        TextMeshProUGUI gTmp = gTxtObj.GetComponent<TextMeshProUGUI>();
        gTmp.fontSize = 28;
        gTmp.color = new Color(0.4f, 0.9f, 1f, 1f);
        gTmp.alignment = TextAlignmentOptions.Center;
        gTmp.text = "★ STEP 3: THE ASTRAL GEOMETRY ★\n\nDraw a closed magic circle on screen!\n\nGeometric precision dictates stats:\n98%+ = Apex Sovereign\n<50% = Malformed Squashed Potato";

        // Step 4 Container
        GameObject step4 = new GameObject("Step4_StatSynthesis", typeof(RectTransform));
        step4.transform.SetParent(modal.transform, false);
        RectTransform s4Rt = step4.GetComponent<RectTransform>();
        s4Rt.anchorMin = Vector2.zero;
        s4Rt.anchorMax = Vector2.one;
        s4Rt.offsetMin = Vector2.zero;
        s4Rt.offsetMax = Vector2.zero;
        step4.SetActive(false);

        GameObject aTxtObj = new GameObject("AccuracyResultTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        aTxtObj.transform.SetParent(step4.transform, false);
        RectTransform aRt = aTxtObj.GetComponent<RectTransform>();
        aRt.anchoredPosition = new Vector2(0f, 200f);
        aRt.sizeDelta = new Vector2(850f, 400f);
        TextMeshProUGUI aTmp = aTxtObj.GetComponent<TextMeshProUGUI>();
        aTmp.fontSize = 28;
        aTmp.color = Color.white;
        aTmp.alignment = TextAlignmentOptions.Center;
        aTmp.text = "★ [APEX CELESTIAL SOVEREIGN]\nName: Aegis\nAffinity: Celestial | Temperament: Loyal\nAccuracy: 99.4%\nMax HP: 450 | ATK: 95 | DEF: 45";

        CreateButtonObject(step4.transform, "FinalizeBtn", "AWAKEN SUMMON >>", new Vector2(0f, -200f), new Vector2(600f, 100f), new Color(0.2f, 0.6f, 0.3f, 1f));

        return modal;
    }

    [MenuItem("Tower of Glyphs/Configure SampleScene Camera")]
    public static void ConfigureSampleSceneCamera()
    {
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        if (!File.Exists(sampleScenePath)) return;

        var scene = EditorSceneManager.OpenScene(sampleScenePath, OpenSceneMode.Single);
        GameObject camObj = GameObject.FindWithTag("MainCamera");
        if (camObj != null)
        {
            camObj.transform.position = new Vector3(0f, 9.5f, -11.5f);
            camObj.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            Camera cam = camObj.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.fieldOfView = 75f;
                cam.allowMSAA = true;
                cam.allowHDR = true;
            }

            if (camObj.GetComponent<HighFidelityMobileResolutionEnforcer>() == null)
            {
                camObj.AddComponent<HighFidelityMobileResolutionEnforcer>();
            }

            UniversalAdditionalCameraData camData = camObj.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Base;
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;

            PortraitARPGCamera arpgCam = camObj.GetComponent<PortraitARPGCamera>();
            if (arpgCam == null) arpgCam = camObj.AddComponent<PortraitARPGCamera>();
            arpgCam.AdjustFieldOfViewForAspect();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif

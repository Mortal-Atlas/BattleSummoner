using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// TieredMetropolisWorldBuilder constructs the vibrant, vertically tiered fantasy metropolis
/// matching the Ghibli-meets-Sword Art Online aesthetic of image_11.png:
/// 1. Terrain, Verticality & Structural Core:
///    - Tiered stone retaining walls, giant stone arches, double-tiered sky bridge aqueduct.
///    - Massive central stone stairway with heavy balustrades, banners, and overhead arch.
///    - Cascading cliffside waterfall on the far right.
/// 2. Lower Gateway & Bustling Bazaar (Bottom-Left):
///    - Timber bakery building with carved wooden loaf-of-bread sign.
///    - Overhanging ancient oak tree with perched horned owl.
///    - Clustered market stalls with colorful striped fabric awnings, potions, spices.
///    - Festoon of hanging glowing paper lanterns with warm illumination.
///    - Characters: Wolf and fox beastfolk, running village children, clockwork brass robot assistant.
///    - Mounts: Adventurer riding armored dire wolf, traveler riding horned reptilian pack beast.
/// 3. Guild Training Grounds & Blacksmith (Bottom-Right):
///    - Open-air forge workshop with burning hearth coals, anvil, and weapon racks.
///    - Sparring arena with archery bullseye targets and straw combat dummies.
///    - 4.2m Ancient moss stone golem sentinel with glowing chest runes.
///    - 3.6m Industrial rusted-iron automaton guard with glowing chest furnace.
/// 4. High-Tier Citadel & Academy (Upper Peak):
///    - "Church of Revival" monumental Gothic cathedral with soaring twin spires, rose window, and buttresses.
///    - Neoclassical scholar academy with patina observatory dome and colonnade portico.
///    - Stepped half-timbered medieval hillside houses.
/// 5. The Sky & Atmosphere:
///    - Flying anime witch on broomstick trailing magical sparkles.
///    - Soaring sky wyvern dragon circling cathedral heights.
///    - Glowing fairies fluttering around the bazaar.
///    - Anime-styled lighting, point lights, and volumetric atmosphere.
/// </summary>
public static class TieredMetropolisWorldBuilder
{
    private const string MODEL_DIR = "Assets/Models/Metropolis/";
    private const string MAT_DIR = "Assets/Materials/Metropolis/";
    private const string SCREENSHOT_DIR = "Assets/Screenshots/";
    private static MetropolisPalette s_Palette;

    [MenuItem("Tools/BattleSummoner/Build Tiered Fantasy Metropolis (Ghibli & SAO Style)")]
    public static void BuildMetropolis()
    {
        EnsureDirectories();
        var mats = CreateMetropolisMaterials();
        s_Palette = mats;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "OverworldMap")
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
        }

        // Clean up previous metropolis build if present
        var existing = GameObject.Find("--- TIERED FANTASY METROPOLIS ---");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("--- TIERED FANTASY METROPOLIS ---");
        root.transform.position = Vector3.zero;

        // 1. Structural Core & Verticality
        BuildStructuralCore(root.transform, mats);

        // 2. Lower-Left Bazaar, Bakery & Overhanging Tree
        BuildBazaarAndBakery(root.transform, mats);

        // 3. Lower-Right Blacksmith Forge, Sparring Arena & Golems
        BuildBlacksmithAndArena(root.transform, mats);

        // 4. Central Grand Staircase & Ascending Artery
        BuildGrandStairsArtery(root.transform, mats);

        // 5. Upper-Tier Citadel, Cathedral & Academy
        BuildHighTierCitadel(root.transform, mats);

        // 6. Sky Aerial Creatures & Flying Witches
        BuildSkyAndAerials(root.transform, mats);

        // 7. Atmospheric Lighting & Ambient Glows
        ConfigureAtmosphericLighting(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("🏰 [TIERED METROPOLIS] Successfully constructed the vibrant Ghibli/SAO tiered metropolis!");

        // Automatically capture high-res showcase screenshot
        CaptureShowcaseScreenshot();
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(MAT_DIR)) Directory.CreateDirectory(MAT_DIR);
        if (!Directory.Exists(SCREENSHOT_DIR)) Directory.CreateDirectory(SCREENSHOT_DIR);
    }

    #region Material Palette Creation (Ghibli / SAO Palette)
    public class MetropolisPalette
    {
        public Material limestone;
        public Material cathedStone;
        public Material mossGreen;
        public Material timberWood;
        public Material roofSlate;
        public Material canopyRed;
        public Material canopyGold;
        public Material canopyBlue;
        public Material breadGold;
        public Material lanternGlow;
        public Material forgeFire;
        public Material runeGlow;
        public Material rustedIron;
        public Material clockworkBrass;
        public Material wolfFur;
        public Material plateArmor;
        public Material cascadeWater;
        public Material fairyGlow;
        public Material marble;
        public Material domePatina;
        public Material roseGlass;
    }

    public static MetropolisPalette CreateMetropolisMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");

        var p = new MetropolisPalette();

        // 1. Core Architectural & Landscape Materials with rich Ghibli / SAO Textures
        p.limestone = GetOrCreateMat("M_Metro_Limestone", lit, new Color(0.92f, 0.90f, 0.85f), 0.35f, 0.05f, "Assets/Textures/DarkAcademia/tex_gothic_stone_wall.jpg", new Vector2(4, 4));
        p.cathedStone = GetOrCreateMat("M_Metro_CathedralStone", lit, new Color(0.95f, 0.95f, 0.95f), 0.40f, 0.08f, "Assets/Textures/DarkAcademia/dark_academia_gothic_wall.jpg", new Vector2(3, 3));
        p.mossGreen = GetOrCreateMat("M_Metro_MossGreen", lit, new Color(0.85f, 1.0f, 0.8f), 0.50f, 0.02f, "Assets/Textures/DarkAcademia/ground_forest_moss_turf.jpg", new Vector2(4, 4));
        p.timberWood = GetOrCreateMat("M_Metro_TimberWood", lit, new Color(0.90f, 0.82f, 0.72f), 0.45f, 0.05f, "Assets/Textures/DarkAcademia/tex_dark_timber_wood.jpg", new Vector2(2, 2));
        p.roofSlate = GetOrCreateMat("M_Metro_RoofSlate", lit, new Color(0.88f, 0.92f, 0.95f), 0.55f, 0.10f, "Assets/Textures/DarkAcademia/tex_gothic_slate_roof.jpg", new Vector2(3, 3));

        // 2. Bazaar Fabrics
        p.canopyRed = GetOrCreateMat("M_Metro_CanopyRed", lit, new Color(0.85f, 0.20f, 0.20f), 0.60f, 0.02f);
        p.canopyGold = GetOrCreateMat("M_Metro_CanopyGold", lit, new Color(0.95f, 0.78f, 0.22f), 0.60f, 0.02f);
        p.canopyBlue = GetOrCreateMat("M_Metro_CanopyBlue", lit, new Color(0.20f, 0.45f, 0.85f), 0.60f, 0.02f);

        // 3. Bakery Loaf of Bread
        p.breadGold = GetOrCreateMat("M_Metro_BreadGold", lit, new Color(0.92f, 0.62f, 0.22f), 0.50f, 0.0f);

        // 4. Emissive Materials
        p.lanternGlow = CreateEmissiveMat("M_Metro_LanternGlow", unlit, new Color(1.0f, 0.82f, 0.35f), 3.5f);
        p.forgeFire = CreateEmissiveMat("M_Metro_ForgeFire", unlit, new Color(1.0f, 0.45f, 0.05f), 4.0f);
        p.runeGlow = CreateEmissiveMat("M_Metro_RuneGlow", unlit, new Color(0.20f, 0.85f, 1.0f), 3.5f, "Assets/Textures/DarkAcademia/dark_academia_rune_circle.jpg");
        p.fairyGlow = CreateEmissiveMat("M_Metro_FairyGlow", unlit, new Color(0.85f, 0.95f, 1.0f), 3.0f);
        p.roseGlass = CreateEmissiveMat("M_Metro_RoseGlass", unlit, new Color(0.92f, 0.32f, 0.60f), 2.8f);

        // 5. Metal & Creature Materials
        p.rustedIron = GetOrCreateMat("M_Metro_RustedIron", lit, new Color(0.40f, 0.32f, 0.28f), 0.55f, 0.75f);
        p.clockworkBrass = GetOrCreateMat("M_Metro_ClockworkBrass", lit, new Color(0.95f, 0.80f, 0.30f), 0.35f, 0.90f);
        p.wolfFur = GetOrCreateMat("M_Metro_WolfFur", lit, new Color(0.42f, 0.44f, 0.48f), 0.80f, 0.05f);
        p.plateArmor = GetOrCreateMat("M_Metro_PlateArmor", lit, new Color(0.90f, 0.92f, 0.94f), 0.25f, 0.95f);
        p.cascadeWater = GetOrCreateMat("M_Metro_CascadeWater", lit, new Color(0.75f, 0.92f, 1.0f), 0.10f, 0.20f, "Assets/Textures/DarkAcademia/tex_fountain_water.jpg", new Vector2(2, 4));
        p.marble = GetOrCreateMat("M_Metro_Marble", lit, new Color(0.92f, 0.90f, 0.88f), 0.40f, 0.05f);
        p.domePatina = GetOrCreateMat("M_Metro_DomePatina", lit, new Color(0.25f, 0.68f, 0.58f), 0.45f, 0.35f);

        AssetDatabase.SaveAssets();
        return p;
    }

    private static Material GetOrCreateMat(string name, Shader s, Color col, float smoothness, float metallic, string texPath = null, Vector2? tiling = null)
    {
        string path = MAT_DIR + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(s) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = col;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);

        if (!string.IsNullOrEmpty(texPath))
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap"))
                {
                    m.SetTexture("_BaseMap", tex);
                    if (tiling.HasValue) m.SetTextureScale("_BaseMap", tiling.Value);
                }
                else
                {
                    m.mainTexture = tex;
                    if (tiling.HasValue) m.mainTextureScale = tiling.Value;
                }
            }
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material CreateEmissiveMat(string name, Shader s, Color col, float intensity, string texPath = null)
    {
        string path = MAT_DIR + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(s) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = col;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);

        if (!string.IsNullOrEmpty(texPath))
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                else m.mainTexture = tex;
            }
        }
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", col * intensity);
        EditorUtility.SetDirty(m);
        return m;
    }
    #endregion

    #region Section 1: Structural Core & Verticality
    private static void BuildStructuralCore(Transform parent, MetropolisPalette p)
    {
        var group = new GameObject("1_Structural_Core").transform;
        group.parent = parent;

        // 1. Tiered Retaining Walls
        var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Metropolis_RetainingWalls_Tiered.fbx");
        if (wallPrefab != null)
        {
            var wall = Object.Instantiate(wallPrefab, group);
            wall.name = "Metropolis_RetainingWalls";
            wall.transform.position = new Vector3(0f, 0f, 18.0f);
            ApplyMaterialRecursive(wall, p.limestone, p.mossGreen);
            AddBoxCollider(wall, new Vector3(42f, 18f, 35f), new Vector3(0f, 9f, 15f));
        }

        // 2. Colossal Sky Bridge & Aqueduct spanning cross-tier
        var aqueductPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Metropolis_SkyBridgeAqueduct.fbx");
        if (aqueductPrefab != null)
        {
            var aq = Object.Instantiate(aqueductPrefab, group);
            aq.name = "Metropolis_SkyBridgeAqueduct";
            aq.transform.position = new Vector3(-2.0f, 2.0f, 22.0f);
            ApplyMaterialRecursive(aq, p.limestone, p.cascadeWater);
            AddBoxCollider(aq, new Vector3(34f, 16f, 5f), new Vector3(0f, 7f, 0f));
        }

        // 3. Cascading Cliffside Waterfall (Far-Right)
        var waterfallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Foliage_CliffsideWaterfall.fbx");
        if (waterfallPrefab != null)
        {
            var wf = Object.Instantiate(waterfallPrefab, group);
            wf.name = "Foliage_CliffsideWaterfall";
            wf.transform.position = new Vector3(18.5f, 0f, 20.0f);
            ApplyMaterialRecursive(wf, p.cascadeWater, p.limestone);
        }
    }
    #endregion

    #region Section 2: Lower Bazaar, Bakery & Oak Tree
    private static void BuildBazaarAndBakery(Transform parent, MetropolisPalette p)
    {
        var group = new GameObject("2_Bazaar_And_Bakery_Quarter").transform;
        group.parent = parent;

        // 1. Timber-Frame Bakery with Carved Bread Sign
        var bakeryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Prop_BakeryBuilding.fbx");
        if (bakeryPrefab != null)
        {
            var bakery = Object.Instantiate(bakeryPrefab, group);
            bakery.name = "Bakery_Building_WithBreadSign";
            bakery.transform.position = new Vector3(-13.5f, 0f, 4.5f);
            bakery.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            ApplyMaterialRecursive(bakery, p.timberWood, p.roofSlate);
            AddBoxCollider(bakery, new Vector3(7f, 10f, 8f), new Vector3(0f, 5f, 0f));
        }

        // 2. Overhanging Grand Oak Tree (Top-Left canopy sheltering the bakery)
        var oakPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Foliage_OverhangingGrandOak.fbx");
        if (oakPrefab != null)
        {
            var oak = Object.Instantiate(oakPrefab, group);
            oak.name = "Foliage_OverhangingGrandOak";
            oak.transform.position = new Vector3(-16.0f, 0f, 7.5f);
            oak.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            oak.transform.localScale = Vector3.one * 1.35f;
            ApplyMaterialRecursive(oak, p.timberWood, p.mossGreen);

            // Perched Horned Owl on the lower bough!
            var owlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Critter_PerchedOwl.fbx");
            if (owlPrefab != null)
            {
                var owl = Object.Instantiate(owlPrefab, oak.transform);
                owl.name = "Critter_PerchedOwl";
                owl.transform.localPosition = new Vector3(1.6f, 4.8f, 1.4f);
                owl.transform.localRotation = Quaternion.Euler(0f, 160f, 0f);
                owl.transform.localScale = Vector3.one * 0.85f;
                ApplyMaterialRecursive(owl, p.timberWood, p.breadGold);
            }
        }

        // 3. Market Stalls Cluster (Potions, Spices, Textiles)
        var stallsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Prop_MarketStalls_Cluster.fbx");
        if (stallsPrefab != null)
        {
            var stalls1 = Object.Instantiate(stallsPrefab, group);
            stalls1.name = "Bazaar_Stalls_PotionsSpices";
            stalls1.transform.position = new Vector3(-9.2f, 0f, 0.5f);
            stalls1.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            ApplyMaterialRecursive(stalls1, p.timberWood, p.canopyRed);
            AddBoxCollider(stalls1, new Vector3(6f, 3.5f, 3f), new Vector3(0f, 1.7f, 0f));

            var stalls2 = Object.Instantiate(stallsPrefab, group);
            stalls2.name = "Bazaar_Stalls_Textiles";
            stalls2.transform.position = new Vector3(-14.0f, 0f, -4.0f);
            stalls2.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
            ApplyMaterialRecursive(stalls2, p.timberWood, p.canopyBlue);
            AddBoxCollider(stalls2, new Vector3(6f, 3.5f, 3f), new Vector3(0f, 1.7f, 0f));
        }

        // 4. Festoon of Hanging Paper Lanterns
        var lanternsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Prop_PaperLanterns_String.fbx");
        if (lanternsPrefab != null)
        {
            var lanterns = Object.Instantiate(lanternsPrefab, group);
            lanterns.name = "Prop_PaperLanterns_String";
            lanterns.transform.position = new Vector3(-10.5f, 0f, 2.0f);
            lanterns.transform.rotation = Quaternion.Euler(0f, 10f, 0f);
            ApplyMaterialRecursive(lanterns, p.lanternGlow, p.canopyRed);

            // Add warm point lights along the string
            for (int li = 0; li < 3; li++)
            {
                var lightObj = new GameObject($"Lantern_Light_{li}");
                lightObj.transform.parent = lanterns.transform;
                lightObj.transform.localPosition = new Vector3(-4f + li * 4f, 3.5f, 0f);
                var l = lightObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.78f, 0.38f);
                l.range = 6.5f;
                l.intensity = 2.0f;
            }
        }

        // 5. Bazaar Characters: Fox Merchant, Wolf Warrior, Running Children, Clockwork Robot
        SpawnCharacter(group, "Character_Beastfolk_Fox", new Vector3(-8.5f, 0f, 1.2f), 190f, p.breadGold);
        SpawnCharacter(group, "Character_Beastfolk_Wolf", new Vector3(-11.2f, 0f, -0.5f), 45f, p.wolfFur);
        SpawnCharacter(group, "Character_RunningKids", new Vector3(-6.5f, 0f, -1.8f), 75f, p.canopyGold);
        SpawnCharacter(group, "Golem_ClockworkBrassAssistant", new Vector3(-7.2f, 0f, 2.8f), -35f, p.clockworkBrass);

        // 6. Rideable Mounts: Armored Wolf Mount & Horned Reptile Pack Beast
        var wolfMountPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Mount_ArmoredWolfBeast.fbx");
        if (wolfMountPrefab != null)
        {
            var wolf = Object.Instantiate(wolfMountPrefab, group);
            wolf.name = "Mount_ArmoredWolfBeast_WithKnight";
            wolf.transform.position = new Vector3(-8.8f, 0f, -3.2f);
            wolf.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            ApplyMaterialRecursive(wolf, p.wolfFur, p.plateArmor);
            AddBoxCollider(wolf, new Vector3(2.5f, 3.8f, 3.5f), new Vector3(0f, 1.9f, 0f));
        }

        var reptilePackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Mount_HornedReptilePackBeast.fbx");
        if (reptilePackPrefab != null)
        {
            var rep = Object.Instantiate(reptilePackPrefab, group);
            rep.name = "Mount_HornedReptilePackBeast_WithTraveler";
            rep.transform.position = new Vector3(-5.2f, 0f, 4.2f);
            rep.transform.rotation = Quaternion.Euler(0f, 140f, 0f);
            ApplyMaterialRecursive(rep, p.mossGreen, p.timberWood);
            AddBoxCollider(rep, new Vector3(2.2f, 3.2f, 3.5f), new Vector3(0f, 1.6f, 0f));
        }

        // 7. Fluttering Glowing Fairies around lanterns
        var fairyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Critter_GlowingFairy.fbx");
        if (fairyPrefab != null)
        {
            for (int f = 0; f < 3; f++)
            {
                var fairy = Object.Instantiate(fairyPrefab, group);
                fairy.name = $"Critter_GlowingFairy_{f}";
                fairy.transform.position = new Vector3(-9.5f + f * 1.8f, 3.2f + (f % 2) * 0.6f, 1.5f + f * 0.8f);
                ApplyMaterialRecursive(fairy, p.fairyGlow, p.fairyGlow);
            }
        }
    }
    #endregion

    #region Section 3: Guild Training Grounds, Blacksmith & Golems
    private static void BuildBlacksmithAndArena(Transform parent, MetropolisPalette p)
    {
        var group = new GameObject("3_Guild_Blacksmith_Arena_Quarter").transform;
        group.parent = parent;

        // 1. Blacksmith Forge Workshop
        var forgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Prop_BlacksmithForge_Complete.fbx");
        if (forgePrefab != null)
        {
            var forge = Object.Instantiate(forgePrefab, group);
            forge.name = "Blacksmith_Forge_Complete";
            forge.transform.position = new Vector3(12.5f, 0f, 2.5f);
            forge.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
            ApplyMaterialRecursive(forge, p.timberWood, p.rustedIron);
            AddBoxCollider(forge, new Vector3(8f, 5f, 6f), new Vector3(0f, 2.5f, 0f));

            // Hearth Fire Light
            var fireLight = new GameObject("Forge_FireLight");
            fireLight.transform.parent = forge.transform;
            fireLight.transform.localPosition = new Vector3(-1.8f, 1.8f, 0f);
            var l = fireLight.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.42f, 0.05f);
            l.range = 8.5f;
            l.intensity = 3.2f;
        }

        // 2. Sparring Arena Targets & Dummies
        var arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Prop_SparringArena_TargetsDummies.fbx");
        if (arenaPrefab != null)
        {
            var arena = Object.Instantiate(arenaPrefab, group);
            arena.name = "Sparring_Arena_TargetsDummies";
            arena.transform.position = new Vector3(16.5f, 0f, -1.5f);
            arena.transform.rotation = Quaternion.Euler(0f, 10f, 0f);
            ApplyMaterialRecursive(arena, p.timberWood, p.canopyRed);
        }

        // 3. Ancient Moss-Covered Stone Golem (Sentinel)
        var golemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Golem_AncientMossStone.fbx");
        if (golemPrefab != null)
        {
            var golem = Object.Instantiate(golemPrefab, group);
            golem.name = "Golem_AncientMossStone_Sentinel";
            golem.transform.position = new Vector3(8.8f, 0f, 5.5f);
            golem.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
            ApplyMaterialRecursive(golem, p.limestone, p.runeGlow);
            AddBoxCollider(golem, new Vector3(3.2f, 4.8f, 2.5f), new Vector3(0f, 2.4f, 0f));

            // Arcane Chest Rune Glow Light
            var runeLight = new GameObject("Golem_RuneLight");
            runeLight.transform.parent = golem.transform;
            runeLight.transform.localPosition = new Vector3(0f, 2.8f, 1.1f);
            var l = runeLight.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.2f, 0.85f, 1.0f);
            l.range = 4.5f;
            l.intensity = 2.0f;
        }

        // 4. Industrial Rusted-Iron Automaton Guard
        var ironAutoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Golem_IndustrialRustedIron.fbx");
        if (ironAutoPrefab != null)
        {
            var auto = Object.Instantiate(ironAutoPrefab, group);
            auto.name = "Golem_IndustrialRustedIron_Guard";
            auto.transform.position = new Vector3(18.8f, 0f, 4.5f);
            auto.transform.rotation = Quaternion.Euler(0f, -30f, 0f);
            ApplyMaterialRecursive(auto, p.rustedIron, p.forgeFire);
            AddBoxCollider(auto, new Vector3(2.8f, 4.2f, 2.5f), new Vector3(0f, 2.1f, 0f));
        }
    }
    #endregion

    #region Section 4: Central Grand Stairs & Ascending Artery
    private static void BuildGrandStairsArtery(Transform parent, MetropolisPalette p)
    {
        var group = new GameObject("4_Grand_Stairs_Artery").transform;
        group.parent = parent;

        // 1. Massive Grand Stairs with Tier Landings & Arch
        var stairsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Metropolis_GrandStairs.fbx");
        if (stairsPrefab != null)
        {
            var stairs = Object.Instantiate(stairsPrefab, group);
            stairs.name = "Metropolis_GrandStairs";
            stairs.transform.position = new Vector3(0f, 0f, 0f);
            ApplyMaterialRecursive(stairs, p.limestone, p.canopyRed);
            AddBoxCollider(stairs, new Vector3(10f, 10f, 32f), new Vector3(0f, 5f, 16f));
        }

        // 2. Bard Playing Lute on Stairway Landing
        SpawnCharacter(group, "Character_BardLute", new Vector3(3.2f, 3.5f, 10.5f), -50f, p.canopyRed);

        // 3. Feathered Chocobo Bird Mount waiting near landing
        var birdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Mount_FeatheredChocoboBird.fbx");
        if (birdPrefab != null)
        {
            var bird = Object.Instantiate(birdPrefab, group);
            bird.name = "Mount_FeatheredChocoboBird";
            bird.transform.position = new Vector3(5.2f, 3.5f, 12.0f);
            bird.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            ApplyMaterialRecursive(bird, p.canopyBlue, p.canopyGold);
            AddBoxCollider(bird, new Vector3(2.2f, 3.6f, 2.8f), new Vector3(0f, 1.8f, 0f));
        }

        // 4. Staggered Half-Timbered Houses along hill flanks
        var housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Metropolis_TimberHouses_Tiered.fbx");
        if (housePrefab != null)
        {
            var hLeft = Object.Instantiate(housePrefab, group);
            hLeft.name = "TimberHouses_FlankLeft";
            hLeft.transform.position = new Vector3(-11.5f, 4.5f, 16.0f);
            hLeft.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            ApplyMaterialRecursive(hLeft, p.timberWood, p.roofSlate);

            var hRight = Object.Instantiate(housePrefab, group);
            hRight.name = "TimberHouses_FlankRight";
            hRight.transform.position = new Vector3(11.5f, 5.0f, 18.0f);
            hRight.transform.rotation = Quaternion.Euler(0f, -30f, 0f);
            ApplyMaterialRecursive(hRight, p.timberWood, p.roofSlate);
        }
    }
    #endregion

    #region Section 5: Upper Citadel, Cathedral & Academy
    private static void BuildHighTierCitadel(Transform parent, MetropolisPalette p)
    {
        var group = new GameObject("5_Citadel_Cathedral_Academy").transform;
        group.parent = parent;

        // 1. "Church of Revival" Monumental Gothic Cathedral
        var cathedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Metropolis_ChurchOfRevival.fbx");
        if (cathedPrefab != null)
        {
            var cathed = Object.Instantiate(cathedPrefab, group);
            cathed.name = "Church_Of_Revival_Cathedral";
            cathed.transform.position = new Vector3(8.5f, 12.0f, 48.0f);
            cathed.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
            ApplyMaterialRecursive(cathed, p.cathedStone, p.roseGlass);
            AddBoxCollider(cathed, new Vector3(26f, 48f, 36f), new Vector3(0f, 24f, 0f));
        }

        // 2. Scholar Academy with Observatory Dome & Colonnade
        var academyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Metropolis_AcademyBuildings.fbx");
        if (academyPrefab != null)
        {
            var acad = Object.Instantiate(academyPrefab, group);
            acad.name = "Scholar_Academy_Observatory";
            acad.transform.position = new Vector3(-12.0f, 11.5f, 42.0f);
            acad.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            ApplyMaterialRecursive(acad, p.marble, p.domePatina);
            AddBoxCollider(acad, new Vector3(24f, 22f, 16f), new Vector3(0f, 11f, 0f));
        }
    }
    #endregion

    #region Section 6: Sky Aerials & Flying Witches
    private static void BuildSkyAndAerials(Transform parent, MetropolisPalette p)
    {
        var group = new GameObject("6_Sky_Aerials_Creatures").transform;
        group.parent = parent;

        // 1. Soaring Sky Wyvern Dragon
        var dragonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Creature_SkyWyvernDragon.fbx");
        if (dragonPrefab != null)
        {
            var dragon = Object.Instantiate(dragonPrefab, group);
            dragon.name = "Creature_SkyWyvernDragon";
            dragon.transform.position = new Vector3(16.0f, 34.0f, 38.0f);
            dragon.transform.rotation = Quaternion.Euler(15f, -35f, 18f);
            ApplyMaterialRecursive(dragon, p.mossGreen, p.canopyGold);
        }

        // 2. Flying Anime Witch on Broomstick
        var witchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + "Character_WitchFlyingBroom.fbx");
        if (witchPrefab != null)
        {
            var witch = Object.Instantiate(witchPrefab, group);
            witch.name = "Character_WitchFlyingBroom";
            witch.transform.position = new Vector3(-4.5f, 21.0f, 22.0f);
            witch.transform.rotation = Quaternion.Euler(-12f, 35f, -10f);
            ApplyMaterialRecursive(witch, p.canopyBlue, p.fairyGlow);

            // Sparkling Magic Trail Particle System
            var psGo = new GameObject("Witch_BroomTrail_VFX");
            psGo.transform.parent = witch.transform;
            psGo.transform.localPosition = new Vector3(0f, -0.2f, -1.4f);
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.8f;
            main.startSpeed = 1.2f;
            main.startSize = 0.25f;
            main.startColor = new Color(0.95f, 0.45f, 0.95f, 0.85f);
            var emission = ps.emission;
            emission.rateOverTime = 25f;
        }
    }
    #endregion

    #region Section 7: Lighting & Atmosphere
    private static void ConfigureAtmosphericLighting(Transform root)
    {
        // Directional Sun Light configured for crisp Ghibli anime daylight
        var sunObj = GameObject.Find("Directional Light") ?? GameObject.Find("Sun");
        if (sunObj != null)
        {
            var sun = sunObj.GetComponent<Light>();
            if (sun != null)
            {
                sun.color = new Color(1.0f, 0.96f, 0.88f);
                sun.intensity = 1.45f;
                sun.shadows = LightShadows.Soft;
                sunObj.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
            }
        }

        // Ambient anime fill light
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.68f, 0.82f, 0.98f);
        RenderSettings.ambientEquatorColor = new Color(0.85f, 0.82f, 0.74f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.45f, 0.32f);

        // Soft atmospheric mountain fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0035f;
        RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.95f);
    }
    #endregion

    #region Helpers & Screenshot Capture
    private static void SpawnCharacter(Transform parent, string fbxName, Vector3 pos, float rotY, Material mat)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_DIR + fbxName + ".fbx");
        if (prefab != null)
        {
            var go = Object.Instantiate(prefab, parent);
            go.name = fbxName;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            ApplyMaterialRecursive(go, mat, mat);
            AddBoxCollider(go, new Vector3(1.2f, 2.0f, 1.2f), new Vector3(0f, 1.0f, 0f));
        }
    }

    private static void ApplyMaterialRecursive(GameObject go, Material primary, Material secondary)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            var mats = r.sharedMaterials;
            bool replacedAny = false;
            for (int m = 0; m < mats.Length; m++)
            {
                var cur = mats[m];
                string name = ((cur != null ? cur.name : "") + " " + r.gameObject.name).ToLowerInvariant();
                Material mapped = MapNameToPalette(name);
                if (mapped != null)
                {
                    mats[m] = mapped;
                    replacedAny = true;
                }
                else
                {
                    mats[m] = (i % 2 == 1 && secondary != null) ? secondary : primary;
                }
            }
            if (replacedAny || mats.Length > 0)
            {
                r.sharedMaterials = mats;
            }
        }
    }

    private static Material MapNameToPalette(string name)
    {
        if (s_Palette == null) s_Palette = CreateMetropolisMaterials();
        var p = s_Palette;
        name = name.ToLowerInvariant();

        if (name.Contains("bread")) return p.breadGold;
        if (name.Contains("fire") || name.Contains("hearth") || name.Contains("coals")) return p.forgeFire;
        if (name.Contains("rune") || name.Contains("sparkle") || name.Contains("magic")) return p.runeGlow;
        if (name.Contains("fairy")) return p.fairyGlow;
        if (name.Contains("rose") || name.Contains("glass") || name.Contains("stained")) return p.roseGlass;
        if (name.Contains("lantern") || name.Contains("lamp") || name.Contains("core") || name.Contains("eye") || name.Contains("lens")) return p.lanternGlow;
        if (name.Contains("water") || name.Contains("cascade") || name.Contains("foam")) return p.cascadeWater;
        if (name.Contains("moss") || name.Contains("leaf") || name.Contains("leaves") || name.Contains("foliage") || name.Contains("ivy")) return p.mossGreen;
        if (name.Contains("timber") || name.Contains("wood") || name.Contains("bark") || name.Contains("trunk") || name.Contains("branch") || name.Contains("tunic") || name.Contains("lute") || name.Contains("straw") || name.Contains("shed") || name.Contains("stump") || name.Contains("post")) return p.timberWood;
        if (name.Contains("roof") || name.Contains("slate")) return p.roofSlate;
        if (name.Contains("patina") || name.Contains("dome")) return p.domePatina;
        if (name.Contains("marble") || name.Contains("pediment") || name.Contains("column") || name.Contains("col")) return p.marble;
        if (name.Contains("cathed") || name.Contains("gothic") || name.Contains("spire") || name.Contains("buttress") || name.Contains("portal") || name.Contains("trim")) return p.cathedStone;
        if (name.Contains("brass") || name.Contains("beak") || name.Contains("gold") || name.Contains("urn") || name.Contains("target_bullseye")) return p.clockworkBrass;
        if (name.Contains("canopyred") || name.Contains("banner") || name.Contains("saddle") || name.Contains("brim") || name.Contains("cone") || name.Contains("cloth") || name.Contains("kid")) return p.canopyRed;
        if (name.Contains("canopyblue") || name.Contains("feather") || name.Contains("robe")) return p.canopyBlue;
        if (name.Contains("canopygold")) return p.canopyGold;
        if (name.Contains("fur") || name.Contains("wolf")) return p.wolfFur;
        if (name.Contains("plate") || name.Contains("armor") || name.Contains("steel") || name.Contains("blade") || name.Contains("sword") || name.Contains("helm")) return p.plateArmor;
        if (name.Contains("rust") || name.Contains("iron") || name.Contains("anvil") || name.Contains("wrought") || name.Contains("bracket") || name.Contains("chain")) return p.rustedIron;
        if (name.Contains("stone") || name.Contains("cobble") || name.Contains("limestone") || name.Contains("step") || name.Contains("balustrade") || name.Contains("plaster") || name.Contains("pier") || name.Contains("wall") || name.Contains("arch")) return p.limestone;

        return null;
    }

    private static void AddBoxCollider(GameObject go, Vector3 size, Vector3 center)
    {
        var bc = go.GetComponent<BoxCollider>();
        if (bc == null) bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.center = center;
    }

    private struct CameraShot
    {
        public string filename;
        public Vector3 position;
        public Vector3 rotation;
        public float fov;
        public string description;

        public CameraShot(string fn, Vector3 pos, Vector3 rot, float fv, string desc)
        {
            filename = fn;
            position = pos;
            rotation = rot;
            fov = fv;
            description = desc;
        }
    }

    [MenuItem("Tools/BattleSummoner/Capture Metropolis Ghibli SAO Showcase Screenshot")]
    public static void CaptureShowcaseScreenshot()
    {
        var dir = "Assets/Screenshots";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string artifactDir = @"C:\Users\jedij\.gemini\antigravity-ide\brain\6972dda2-714e-4e80-9b19-b069c8a3656f";

        var cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null) return;

        // Temporarily hide legacy scene objects and character tags during beauty showcase
        string[] hideDuringShot = new string[] {
            "SolasTownHub", "DarkAcademia_SceneryRoot", "HighResFantasyWorld_Root",
            "Player_Hero", "OverworldNPCs", "SpireDungeonEntrance"
        };
        var disabledRoots = new List<GameObject>();
        foreach (var h in hideDuringShot)
        {
            var go = GameObject.Find(h);
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
                disabledRoots.Add(go);
            }
        }

        var prevPos = cam.transform.position;
        var prevRot = cam.transform.rotation;
        var prevFov = cam.fieldOfView;
        var prevTarget = cam.targetTexture;

        int width = 1920;
        int height = 1080;

        CameraShot[] shots = new CameraShot[]
        {
            new CameraShot(
                "TieredMetropolis_Ghibli_SAO_Showcase.png",
                new Vector3(-2.0f, 5.2f, -22.0f),
                new Vector3(18f, 4f, 0f),
                64f,
                "Grand Street-Level Panorama (Ghibli / SAO image_11.png perspective)"
            ),
            new CameraShot(
                "TieredMetropolis_Bazaar_Bakery.png",
                new Vector3(-8.5f, 4.0f, -14.0f),
                new Vector3(12f, -18f, 0f),
                56f,
                "Lower-Left Bazaar, Carved Bread Sign, Overhanging Oak, Lanterns & Wolf Mount"
            ),
            new CameraShot(
                "TieredMetropolis_Blacksmith_Golems.png",
                new Vector3(7.5f, 4.2f, -14.0f),
                new Vector3(14f, 20f, 0f),
                56f,
                "Guild Training Grounds, Blacksmith Forge & Colossal Ancient Moss Golem"
            ),
            new CameraShot(
                "TieredMetropolis_Citadel_SkyAscent.png",
                new Vector3(0f, 7.5f, 4.0f),
                new Vector3(26f, 0f, 0f),
                62f,
                "Ascending Artery, Sky Bridge, Church of Revival Cathedral Spires & Sky Wyvern"
            ),
            new CameraShot(
                "TieredMetropolis_Aerial_Metropolis.png",
                new Vector3(-22.0f, 38.0f, 6.0f),
                new Vector3(34f, 52f, 0f),
                64f,
                "High-Altitude Overview capturing the full tiered metropolis layout matching map"
            )
        };

        foreach (var s in shots)
        {
            cam.transform.position = s.position;
            cam.transform.rotation = Quaternion.Euler(s.rotation);
            cam.fieldOfView = s.fov;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string outPath = Path.Combine(dir, s.filename);
            File.WriteAllBytes(outPath, png);

            if (Directory.Exists(artifactDir))
            {
                File.WriteAllBytes(Path.Combine(artifactDir, s.filename), png);
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log($"📸 [METROPOLIS SHOWCASE] Captured {s.description} -> {outPath}");
        }

        // Restore camera
        cam.transform.position = prevPos;
        cam.transform.rotation = prevRot;
        cam.fieldOfView = prevFov;
        cam.targetTexture = prevTarget;

        // Restore temporarily hidden legacy roots
        foreach (var go in disabledRoots)
        {
            if (go != null) go.SetActive(true);
        }

        AssetDatabase.Refresh();
        Debug.Log("📸 [METROPOLIS SHOWCASE] All 5 high-resolution showcase beauty screenshots captured successfully!");
    }
    #endregion
}

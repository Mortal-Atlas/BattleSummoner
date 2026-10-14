using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Combat;

/// <summary>
/// DarkAcademiaWorldBuilder completely upgrades the game world to a high-detail
/// Fantasy Dark Academia aesthetic:
/// 1. Persona 3/4/5 anime styled protagonist and NPC characters with slender 8-head proportions,
///    tailored academy uniforms, Gekkoukan/Shujin blazers, SEES armbands, and stylish anime hair.
/// 2. Rich ground and architectural textures (seamless concentric cobblestone with moss,
///    forest turf with clover, crushed slate gravel, mossy slate flagstones, gothic limestone walls,
///    diamond slate roofs, polished mahogany timber, and enchanted celestial fountain water).
/// 3. Ornate Dark Academia 3D props: Gothic astrolabe fountain, wrought-iron gaslight lampposts
///    with warm amber point lights, carved scholar benches, striped velvet market stalls with
///    alchemical phials and grimoires, sentinel gargoyles, and grimoire pedestals.
/// 4. Lush greenery: Gothic Cypress trees, ancient gnarled oaks with multi-branch boughs,
///    mossy boulders, climbing ivy, and fern clusters.
/// </summary>
public static class DarkAcademiaWorldBuilder
{
    private const string MAT_DIR = "Assets/Materials/DarkAcademia/";
    private const string WORLD_MAT_DIR = "Assets/Materials/World/";

    [MenuItem("Tools/BattleSummoner/Build Dark Academia World, Greenery & Persona Protagonist")]
    public static void BuildAll()
    {
        EnsureDirectory(MAT_DIR);
        EnsureDirectory(WORLD_MAT_DIR);
        var mats = CreatePaletteMaterials();

        // 1. Upgrade all World Materials with high-res Dark Academia textures
        ApplyDarkAcademiaWorldTextures();

        // 2. Populate Overworld Scene (Plaza, Town, Greenery, NPCs, Protagonist)
        PopulateOverworldScene(mats);

        // 3. Populate Dungeon Floor Scene (Braziers, Lecterns, Gargoyles, Protagonist)
        PopulateDungeonFloorScene(mats);

        Debug.Log("🏰 [DARK ACADEMIA WORLD] Successfully generated Persona protagonist, NPCs, fountain, trees, greenery, lampposts, and gothic props across all worlds!");
    }

    private static void EnsureDirectory(string path)
    {
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
    }

    #region World Material Texturing
    public static void ApplyDarkAcademiaWorldTextures()
    {
        var cobbleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/ground_cobblestone_dark_academia.jpg");
        var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/ground_forest_moss_turf.jpg");
        var gravelTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/ground_scholastic_gravel.jpg");
        var slateTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/ground_spire_mossy_slate.jpg");
        var wallTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/tex_gothic_stone_wall.jpg");
        var roofTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/tex_gothic_slate_roof.jpg");
        var woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/tex_dark_timber_wood.jpg");
        var waterTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/tex_fountain_water.jpg");

        ConfigureWorldMat("Mat_World_Cobblestone.mat", cobbleTex, new Vector2(12f, 12f), Color.white, 0.45f, 0.05f);
        ConfigureWorldMat("Mat_World_ForestGrass.mat", grassTex, new Vector2(16f, 16f), Color.white, 0.35f, 0.05f);
        ConfigureWorldMat("Mat_World_ForestDirt.mat", gravelTex, new Vector2(4f, 16f), Color.white, 0.25f, 0.05f);
        ConfigureWorldMat("Mat_World_DungeonStone.mat", slateTex, new Vector2(8f, 8f), Color.white, 0.50f, 0.10f);
        ConfigureWorldMat("Mat_World_TownWall.mat", wallTex, new Vector2(3f, 3f), Color.white, 0.35f, 0.05f);
        ConfigureWorldMat("Mat_World_RoofSlate.mat", roofTex, new Vector2(4f, 4f), Color.white, 0.60f, 0.15f);
        ConfigureWorldMat("Mat_World_RoofTile.mat", roofTex, new Vector2(4f, 4f), Color.white, 0.60f, 0.15f);
        ConfigureWorldMat("Mat_World_WoodBeam.mat", woodTex, new Vector2(1f, 3f), Color.white, 0.40f, 0.05f);
        ConfigureWorldMat("Mat_World_WoodPlank.mat", woodTex, new Vector2(2f, 2f), Color.white, 0.40f, 0.05f);
        ConfigureWorldMat("Mat_World_DoorWood.mat", woodTex, new Vector2(1f, 2f), Color.white, 0.45f, 0.08f);
        ConfigureWorldMat("Mat_World_WoodBark.mat", woodTex, new Vector2(1f, 4f), new Color(0.65f, 0.55f, 0.45f), 0.20f, 0.05f);
        ConfigureWorldMat("Mat_World_FountainWater.mat", waterTex, new Vector2(2f, 2f), Color.white, 0.95f, 0.20f);

        // Glowing Lantern Emissive
        var mLantern = AssetDatabase.LoadAssetAtPath<Material>(WORLD_MAT_DIR + "Mat_World_LanternGlow.mat");
        if (mLantern != null)
        {
            mLantern.color = new Color(1.0f, 0.85f, 0.45f);
            mLantern.EnableKeyword("_EMISSION");
            mLantern.SetColor("_EmissionColor", new Color(1.0f, 0.80f, 0.35f) * 2.5f);
            EditorUtility.SetDirty(mLantern);
        }

        AssetDatabase.SaveAssets();
    }

    private static void ConfigureWorldMat(string fileName, Texture2D tex, Vector2 scale, Color color, float smoothness, float metallic)
    {
        string path = WORLD_MAT_DIR + fileName;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) return;

        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", scale);
        }
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(mat);
    }
    #endregion

    #region Palette Materials
    public class WorldPalette
    {
        public Material bark;
        public Material foliage;
        public Material pineNeedles;
        public Material moss;
        public Material wroughtIron;
        public Material antiqueBrass;
        public Material lanternFlame;
        public Material carvedStone;
        public Material grimoireLeather;
        public Material parchmentPaper;
        public Material waterSapphire;

        // Persona Character Materials
        public Material personaBlazer;
        public Material personaShirt;
        public Material personaTie;
        public Material personaArmband;
        public Material personaHair;
        public Material personaSkin;
        public Material personaEyes;
        public Material personaPants;
        public Material personaShoes;

        // NPC Unique Materials
        public Material npcMargoDress;
        public Material npcMargoTrim;
        public Material npcMayorCoat;
        public Material npcMayorHair;
        public Material npcKeithApron;
        public Material npcCherylVest;
        public Material npcZephyrCloak;
        public Material npcGregoryCoat;
        public Material potionAzure;
        public Material potionRuby;
        public Material potionAmber;
    }

    public static WorldPalette CreatePaletteMaterials()
    {
        EnsureDirectory(MAT_DIR);
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");

        var p = new WorldPalette();

        p.bark = GetOrCreateMat("M_DA_Bark", lit, new Color(0.20f, 0.14f, 0.10f), 0.15f, 0.05f);
        p.foliage = GetOrCreateMat("M_DA_Foliage", lit, new Color(0.12f, 0.32f, 0.16f), 0.35f, 0.05f);
        p.pineNeedles = GetOrCreateMat("M_DA_PineNeedles", lit, new Color(0.08f, 0.22f, 0.14f), 0.25f, 0.05f);
        p.moss = GetOrCreateMat("M_DA_Moss", lit, new Color(0.24f, 0.48f, 0.18f), 0.40f, 0.05f);
        p.wroughtIron = GetOrCreateMat("M_DA_WroughtIron", lit, new Color(0.10f, 0.10f, 0.12f), 0.65f, 0.75f);
        p.antiqueBrass = GetOrCreateMat("M_DA_AntiqueBrass", lit, new Color(0.78f, 0.62f, 0.25f), 0.70f, 0.85f);
        p.carvedStone = GetOrCreateMat("M_DA_CarvedStone", lit, new Color(0.38f, 0.36f, 0.35f), 0.25f, 0.10f);
        p.grimoireLeather = GetOrCreateMat("M_DA_GrimoireLeather", lit, new Color(0.35f, 0.10f, 0.12f), 0.45f, 0.10f);
        p.parchmentPaper = GetOrCreateMat("M_DA_ParchmentPaper", lit, new Color(0.88f, 0.82f, 0.68f), 0.15f, 0.0f);
        p.waterSapphire = GetOrCreateMat("M_DA_WaterSapphire", lit, new Color(0.08f, 0.32f, 0.55f), 0.95f, 0.15f);

        // Lantern Flame (Unlit Emissive)
        p.lanternFlame = new Material(unlit) { name = "M_DA_LanternFlame", color = new Color(1.0f, 0.75f, 0.25f) };
        AssetDatabase.CreateAsset(p.lanternFlame, MAT_DIR + "M_DA_LanternFlame.mat");

        // Persona Protagonist Materials
        p.personaBlazer = GetOrCreateMat("M_Persona_Blazer", lit, new Color(0.08f, 0.10f, 0.16f), 0.45f, 0.10f); // Shujin/Gekkoukan midnight navy
        p.personaShirt = GetOrCreateMat("M_Persona_Shirt", lit, new Color(0.95f, 0.95f, 0.96f), 0.20f, 0.0f);
        p.personaTie = GetOrCreateMat("M_Persona_Tie", lit, new Color(0.75f, 0.12f, 0.15f), 0.55f, 0.10f); // Persona crimson ribbon
        p.personaArmband = GetOrCreateMat("M_Persona_Armband", lit, new Color(0.85f, 0.15f, 0.15f), 0.60f, 0.20f); // SEES red armband
        p.personaHair = GetOrCreateMat("M_Persona_Hair", lit, new Color(0.12f, 0.14f, 0.18f), 0.70f, 0.15f); // Deep anime slate/navy
        p.personaSkin = GetOrCreateMat("M_Persona_Skin", lit, new Color(0.98f, 0.88f, 0.80f), 0.35f, 0.0f); // Smooth anime tone
        p.personaEyes = GetOrCreateMat("M_Persona_Eyes", lit, new Color(0.15f, 0.45f, 0.65f), 0.90f, 0.10f); // Electric steel blue
        p.personaPants = GetOrCreateMat("M_Persona_Pants", lit, new Color(0.12f, 0.13f, 0.16f), 0.30f, 0.05f); // Tailored dark plaid/slate
        p.personaShoes = GetOrCreateMat("M_Persona_Shoes", lit, new Color(0.05f, 0.05f, 0.06f), 0.85f, 0.40f); // Polished leather loafers

        // NPC Unique Materials
        p.npcMargoDress = GetOrCreateMat("M_NPC_MargoDress", lit, new Color(0.06f, 0.09f, 0.22f), 0.50f, 0.15f); // Velvet Room royal blue
        p.npcMargoTrim = GetOrCreateMat("M_NPC_MargoTrim", lit, new Color(0.88f, 0.75f, 0.35f), 0.70f, 0.75f); // Gold filigree trim
        p.npcMayorCoat = GetOrCreateMat("M_NPC_MayorCoat", lit, new Color(0.24f, 0.08f, 0.12f), 0.45f, 0.10f); // Deep burgundy scholar coat
        p.npcMayorHair = GetOrCreateMat("M_NPC_MayorHair", lit, new Color(0.72f, 0.73f, 0.75f), 0.50f, 0.10f); // Distinguished silver hair
        p.npcKeithApron = GetOrCreateMat("M_NPC_KeithApron", lit, new Color(0.28f, 0.18f, 0.12f), 0.35f, 0.05f); // Heavy leather apron
        p.npcCherylVest = GetOrCreateMat("M_NPC_CherylVest", lit, new Color(0.08f, 0.22f, 0.14f), 0.45f, 0.10f); // Emerald alchemist vest
        p.npcZephyrCloak = GetOrCreateMat("M_NPC_ZephyrCloak", lit, new Color(0.14f, 0.24f, 0.16f), 0.40f, 0.05f); // Forest ranger cloak
        p.npcGregoryCoat = GetOrCreateMat("M_NPC_GregoryCoat", lit, new Color(0.12f, 0.12f, 0.14f), 0.55f, 0.20f); // Spire warden trenchcoat

        // Potions
        p.potionAzure = new Material(unlit) { name = "M_DA_PotionAzure", color = new Color(0.2f, 0.7f, 1.0f) };
        AssetDatabase.CreateAsset(p.potionAzure, MAT_DIR + "M_DA_PotionAzure.mat");
        p.potionRuby = new Material(unlit) { name = "M_DA_PotionRuby", color = new Color(1.0f, 0.2f, 0.3f) };
        AssetDatabase.CreateAsset(p.potionRuby, MAT_DIR + "M_DA_PotionRuby.mat");
        p.potionAmber = new Material(unlit) { name = "M_DA_PotionAmber", color = new Color(1.0f, 0.65f, 0.1f) };
        AssetDatabase.CreateAsset(p.potionAmber, MAT_DIR + "M_DA_PotionAmber.mat");

        AssetDatabase.SaveAssets();
        return p;
    }

    private static Material GetOrCreateMat(string name, Shader s, Color col, float smoothness, float metallic)
    {
        string path = MAT_DIR + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(s) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = col;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(m);
        return m;
    }
    #endregion

    #region Persona Protagonist Model
    public static void BuildPersonaProtagonistInActiveScene(WorldPalette p)
    {
        var player = GameObject.Find("Player_Hero");
        if (player == null) return;

        var mr = player.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        var oldHeroBody = player.transform.Find("HeroBody");
        if (oldHeroBody != null) Object.DestroyImmediate(oldHeroBody.gameObject);

        var existingRig = player.transform.Find("AvatarRigRoot");
        if (existingRig != null) Object.DestroyImmediate(existingRig.gameObject);

        // Build Persona 3/4/5 anime character hierarchy (Slender 8-head aesthetic)
        GameObject rigRoot = new GameObject("AvatarRigRoot");
        rigRoot.transform.SetParent(player.transform, false);
        rigRoot.transform.localPosition = Vector3.zero;
        rigRoot.transform.localRotation = Quaternion.identity;

        // 1. Hips (Waist)
        GameObject hips = CreateBone(rigRoot.transform, "HipsBone", new Vector3(0f, 0.98f, 0f));
        CreatePart(hips.transform, "HipsMesh", PrimitiveType.Cube, Vector3.zero, new Vector3(0.28f, 0.18f, 0.20f), p.personaPants);

        // 2. Torso (Spine / Midriff with vest)
        GameObject torso = CreateBone(hips.transform, "TorsoBone", new Vector3(0f, 0.14f, 0f));
        CreatePart(torso.transform, "TorsoMesh", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.26f, 0.10f, 0.18f), p.personaShirt);

        // 3. Chest (Tailored Academy Blazer with Lapels)
        GameObject chest = CreateBone(torso.transform, "ChestBone", new Vector3(0f, 0.20f, 0f));
        CreatePart(chest.transform, "BlazerBody", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(0.34f, 0.38f, 0.22f), p.personaBlazer);
        CreatePart(chest.transform, "ShirtCollar", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0.09f), new Vector3(0.12f, 0.05f, 0.04f), p.personaShirt);
        CreatePart(chest.transform, "SchoolTie", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.12f), new Vector3(0.055f, 0.22f, 0.02f), p.personaTie);
        CreatePart(chest.transform, "SchoolCrestPin", PrimitiveType.Cylinder, new Vector3(-0.11f, 0.16f, 0.12f), new Vector3(0.035f, 0.01f, 0.035f), p.antiqueBrass);

        // 4. Neck & Head
        GameObject neck = CreateBone(chest.transform, "NeckBone", new Vector3(0f, 0.24f, 0f));
        CreatePart(neck.transform, "NeckMesh", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.10f, 0.06f, 0.10f), p.personaSkin);

        GameObject head = CreateBone(neck.transform, "HeadBone", new Vector3(0f, 0.12f, 0f));
        CreatePart(head.transform, "Cranium", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.22f, 0.24f, 0.22f), p.personaSkin);
        CreatePart(head.transform, "JawChin", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.04f), new Vector3(0.13f, 0.10f, 0.12f), p.personaSkin);

        CreatePart(head.transform, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.052f, 0.035f, 0.095f), new Vector3(0.035f, 0.030f, 0.02f), p.personaEyes);
        CreatePart(head.transform, "Eye_R", PrimitiveType.Sphere, new Vector3(0.052f, 0.035f, 0.095f), new Vector3(0.035f, 0.030f, 0.02f), p.personaEyes);

        // Hair (Persona protagonist shaggy swept bangs)
        GameObject hairRoot = new GameObject("HairRoot");
        hairRoot.transform.SetParent(head.transform, false);
        CreatePart(hairRoot.transform, "HairTop", PrimitiveType.Sphere, new Vector3(0f, 0.11f, -0.01f), new Vector3(0.26f, 0.20f, 0.26f), p.personaHair);
        CreatePart(hairRoot.transform, "HairBangsFront", PrimitiveType.Cube, new Vector3(-0.02f, 0.08f, 0.10f), new Vector3(0.18f, 0.10f, 0.08f), p.personaHair);
        CreatePart(hairRoot.transform, "HairBangLeft", PrimitiveType.Cube, new Vector3(-0.11f, 0.03f, 0.07f), new Vector3(0.06f, 0.16f, 0.08f), p.personaHair);
        CreatePart(hairRoot.transform, "HairBangRight", PrimitiveType.Cube, new Vector3(0.10f, 0.04f, 0.06f), new Vector3(0.05f, 0.14f, 0.07f), p.personaHair);
        CreatePart(hairRoot.transform, "HairBackSpikes", PrimitiveType.Sphere, new Vector3(0f, 0.04f, -0.10f), new Vector3(0.24f, 0.20f, 0.16f), p.personaHair);

        // 5. Arms (Left with red armband)
        GameObject lUpperArm = CreateBone(chest.transform, "LeftUpperArm", new Vector3(-0.21f, 0.18f, 0f));
        CreatePart(lUpperArm.transform, "L_BicepMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0f), new Vector3(0.10f, 0.14f, 0.10f), p.personaBlazer);
        CreatePart(lUpperArm.transform, "RedArmband", PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0f), new Vector3(0.115f, 0.05f, 0.115f), p.personaArmband);

        GameObject lForearm = CreateBone(lUpperArm.transform, "LeftForearm", new Vector3(0f, -0.26f, 0f));
        CreatePart(lForearm.transform, "L_ForearmMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0f), new Vector3(0.09f, 0.13f, 0.09f), p.personaBlazer);

        GameObject lHand = CreateBone(lForearm.transform, "LeftHand", new Vector3(0f, -0.25f, 0f));
        CreatePart(lHand.transform, "L_HandMesh", PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0f), new Vector3(0.07f, 0.08f, 0.06f), p.personaSkin);

        GameObject rUpperArm = CreateBone(chest.transform, "RightUpperArm", new Vector3(0.21f, 0.18f, 0f));
        CreatePart(rUpperArm.transform, "R_BicepMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0f), new Vector3(0.10f, 0.14f, 0.10f), p.personaBlazer);

        GameObject rForearm = CreateBone(rUpperArm.transform, "RightForearm", new Vector3(0f, -0.26f, 0f));
        CreatePart(rForearm.transform, "R_ForearmMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0f), new Vector3(0.09f, 0.13f, 0.09f), p.personaBlazer);

        GameObject rHand = CreateBone(rForearm.transform, "RightHand", new Vector3(0f, -0.25f, 0f));
        CreatePart(rHand.transform, "R_HandMesh", PrimitiveType.Sphere, new Vector3(0f, -0.04f, 0f), new Vector3(0.07f, 0.08f, 0.06f), p.personaSkin);

        // 6. Legs (Elongated anime legs)
        GameObject lThigh = CreateBone(hips.transform, "LeftThigh", new Vector3(-0.095f, -0.06f, 0f));
        CreatePart(lThigh.transform, "L_ThighMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.20f, 0f), new Vector3(0.13f, 0.22f, 0.13f), p.personaPants);

        GameObject lShin = CreateBone(lThigh.transform, "LeftShin", new Vector3(0f, -0.42f, 0f));
        CreatePart(lShin.transform, "L_ShinMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.22f, 0f), new Vector3(0.11f, 0.24f, 0.11f), p.personaPants);

        GameObject lFoot = CreateBone(lShin.transform, "LeftFoot", new Vector3(0f, -0.46f, 0f));
        CreatePart(lFoot.transform, "L_ShoeMesh", PrimitiveType.Cube, new Vector3(0f, -0.035f, 0.06f), new Vector3(0.10f, 0.065f, 0.24f), p.personaShoes);

        GameObject rThigh = CreateBone(hips.transform, "RightThigh", new Vector3(0.095f, -0.06f, 0f));
        CreatePart(rThigh.transform, "R_ThighMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.20f, 0f), new Vector3(0.13f, 0.22f, 0.13f), p.personaPants);

        GameObject rShin = CreateBone(rThigh.transform, "RightShin", new Vector3(0f, -0.42f, 0f));
        CreatePart(rShin.transform, "R_ShinMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.22f, 0f), new Vector3(0.11f, 0.24f, 0.11f), p.personaPants);

        GameObject rFoot = CreateBone(rShin.transform, "RightFoot", new Vector3(0f, -0.46f, 0f));
        CreatePart(rFoot.transform, "R_ShoeMesh", PrimitiveType.Cube, new Vector3(0f, -0.035f, 0.06f), new Vector3(0.10f, 0.065f, 0.24f), p.personaShoes);

        // Connect HumanoidAnimationController
        var animController = player.GetComponent<HumanoidAnimationController>();
        if (animController == null) animController = player.AddComponent<HumanoidAnimationController>();
        animController.ResolveAnimator();

        Debug.Log("✨ [PERSONA PROTAGONIST] Successfully constructed Persona anime protagonist model on Player_Hero!");
    }
    #endregion

    #region Persona NPCs Models
    public static void BuildPersonaNPCs(WorldPalette p)
    {
        var npcsRoot = GameObject.Find("OverworldNPCs");
        if (npcsRoot == null) return;

        for (int i = 0; i < npcsRoot.transform.childCount; i++)
        {
            var npc = npcsRoot.transform.GetChild(i);
            string name = npc.name;

            // Hide old primitive body/head
            var body = npc.Find("Body");
            if (body != null)
            {
                var r = body.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
            var head = npc.Find("Head");
            if (head != null)
            {
                var r = head.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }

            // Remove any old avatar rig
            var oldRig = npc.Find("AvatarRigRoot");
            if (oldRig != null) Object.DestroyImmediate(oldRig.gameObject);

            // Construct specialized Persona anime model based on NPC role
            if (name.Contains("Margo"))
            {
                BuildMargoPersonaModel(npc, p);
            }
            else if (name.Contains("Mayor"))
            {
                BuildMayorPersonaModel(npc, p);
            }
            else if (name.Contains("Blacksmith"))
            {
                BuildKeithPersonaModel(npc, p);
            }
            else if (name.Contains("Alchemist"))
            {
                BuildCherylPersonaModel(npc, p);
            }
            else if (name.Contains("Ranger"))
            {
                BuildZephyrPersonaModel(npc, p);
            }
            else if (name.Contains("Warden"))
            {
                BuildGregoryPersonaModel(npc, p);
            }
        }

        Debug.Log("🎭 [PERSONA NPCS] Successfully constructed Persona anime character models for all NPCs!");
    }

    private static void BuildMargoPersonaModel(Transform parent, WorldPalette p)
    {
        // Velvet Room / Academy Scholar attendant aesthetic
        GameObject root = new GameObject("AvatarRigRoot");
        root.transform.SetParent(parent, false);

        // Hips & Flared Velvet Academy Skirt
        GameObject hips = CreateBone(root.transform, "HipsBone", new Vector3(0f, 0.95f, 0f));
        CreatePart(hips.transform, "Skirt", PrimitiveType.Cylinder, new Vector3(0f, -0.15f, 0f), new Vector3(0.38f, 0.22f, 0.38f), p.npcMargoDress);

        // Slender Torso with Gold Filigree Buttons
        GameObject torso = CreateBone(hips.transform, "TorsoBone", new Vector3(0f, 0.14f, 0f));
        CreatePart(torso.transform, "DressTorso", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.24f, 0.12f, 0.16f), p.npcMargoDress);

        // Chest with Lace Collar & Ribbon
        GameObject chest = CreateBone(torso.transform, "ChestBone", new Vector3(0f, 0.18f, 0f));
        CreatePart(chest.transform, "DressChest", PrimitiveType.Cube, new Vector3(0f, 0.10f, 0f), new Vector3(0.28f, 0.20f, 0.18f), p.npcMargoDress);
        CreatePart(chest.transform, "LaceCollar", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0.09f), new Vector3(0.14f, 0.04f, 0.04f), p.personaShirt);
        CreatePart(chest.transform, "GoldBrooch", PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.11f), new Vector3(0.04f, 0.04f, 0.02f), p.npcMargoTrim);
        CreatePart(chest.transform, "VelvetRibbon", PrimitiveType.Cube, new Vector3(0f, 0.11f, 0.10f), new Vector3(0.05f, 0.12f, 0.02f), p.personaTie);

        // Neck & Head
        GameObject neck = CreateBone(chest.transform, "NeckBone", new Vector3(0f, 0.22f, 0f));
        CreatePart(neck.transform, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.08f, 0.06f, 0.08f), p.personaSkin);

        GameObject head = CreateBone(neck.transform, "HeadBone", new Vector3(0f, 0.11f, 0f));
        CreatePart(head.transform, "Cranium", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.20f, 0.22f, 0.20f), p.personaSkin);
        CreatePart(head.transform, "Jaw", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.035f), new Vector3(0.11f, 0.09f, 0.10f), p.personaSkin);
        CreatePart(head.transform, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.045f, 0.03f, 0.085f), new Vector3(0.03f, 0.03f, 0.02f), p.personaEyes);
        CreatePart(head.transform, "Eye_R", PrimitiveType.Sphere, new Vector3(0.045f, 0.03f, 0.085f), new Vector3(0.03f, 0.03f, 0.02f), p.personaEyes);

        // Sleek Anime Bob Haircut
        GameObject hair = new GameObject("HairRoot");
        hair.transform.SetParent(head.transform, false);
        CreatePart(hair.transform, "BobTop", PrimitiveType.Sphere, new Vector3(0f, 0.10f, 0f), new Vector3(0.24f, 0.18f, 0.24f), p.personaHair);
        CreatePart(hair.transform, "StraightBangs", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.09f), new Vector3(0.18f, 0.08f, 0.05f), p.personaHair);
        CreatePart(hair.transform, "SideBob_L", PrimitiveType.Cube, new Vector3(-0.11f, 0.02f, 0.02f), new Vector3(0.05f, 0.16f, 0.16f), p.personaHair);
        CreatePart(hair.transform, "SideBob_R", PrimitiveType.Cube, new Vector3(0.11f, 0.02f, 0.02f), new Vector3(0.05f, 0.16f, 0.16f), p.personaHair);

        // Slender Arms with White Lace Cuffs
        BuildSlenderLimb(chest.transform, new Vector3(-0.18f, 0.16f, 0f), p.npcMargoDress, p.personaSkin, false);
        BuildSlenderLimb(chest.transform, new Vector3(0.18f, 0.16f, 0f), p.npcMargoDress, p.personaSkin, false);

        // Slender Legs with Dark Stockings & Shoes
        BuildSlenderLeg(hips.transform, new Vector3(-0.08f, -0.15f, 0f), p.npcMargoDress, p.personaShoes);
        BuildSlenderLeg(hips.transform, new Vector3(0.08f, -0.15f, 0f), p.npcMargoDress, p.personaShoes);
    }

    private static void BuildMayorPersonaModel(Transform parent, WorldPalette p)
    {
        // Academy Dean / Headmaster aesthetic
        GameObject root = new GameObject("AvatarRigRoot");
        root.transform.SetParent(parent, false);

        GameObject hips = CreateBone(root.transform, "HipsBone", new Vector3(0f, 0.98f, 0f));
        CreatePart(hips.transform, "HipsMesh", PrimitiveType.Cube, Vector3.zero, new Vector3(0.30f, 0.18f, 0.22f), p.personaPants);

        GameObject torso = CreateBone(hips.transform, "TorsoBone", new Vector3(0f, 0.15f, 0f));
        CreatePart(torso.transform, "Vest", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.12f, 0.20f), p.npcMayorCoat);

        GameObject chest = CreateBone(torso.transform, "ChestBone", new Vector3(0f, 0.20f, 0f));
        CreatePart(chest.transform, "ScholarFrockCoat", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(0.36f, 0.26f, 0.24f), p.npcMayorCoat);
        // Gold pocket watch chain
        CreatePart(chest.transform, "WatchChain", PrimitiveType.Cube, new Vector3(0.06f, 0.08f, 0.13f), new Vector3(0.10f, 0.02f, 0.02f), p.antiqueBrass);
        // Ascot cravat
        CreatePart(chest.transform, "Ascot", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0.12f), new Vector3(0.08f, 0.12f, 0.03f), p.parchmentPaper);

        GameObject neck = CreateBone(chest.transform, "NeckBone", new Vector3(0f, 0.25f, 0f));
        CreatePart(neck.transform, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.10f, 0.06f, 0.10f), p.personaSkin);

        GameObject head = CreateBone(neck.transform, "HeadBone", new Vector3(0f, 0.12f, 0f));
        CreatePart(head.transform, "Cranium", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.22f, 0.24f, 0.22f), p.personaSkin);
        CreatePart(head.transform, "Jaw", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.04f), new Vector3(0.13f, 0.10f, 0.12f), p.personaSkin);
        CreatePart(head.transform, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.052f, 0.035f, 0.095f), new Vector3(0.035f, 0.030f, 0.02f), p.personaEyes);
        CreatePart(head.transform, "Eye_R", PrimitiveType.Sphere, new Vector3(0.052f, 0.035f, 0.095f), new Vector3(0.035f, 0.030f, 0.02f), p.personaEyes);
        // Gold Wire Spectacles
        CreatePart(head.transform, "Glasses_L", PrimitiveType.Cylinder, new Vector3(-0.052f, 0.035f, 0.11f), new Vector3(0.045f, 0.005f, 0.045f), p.antiqueBrass);
        CreatePart(head.transform, "Glasses_R", PrimitiveType.Cylinder, new Vector3(0.052f, 0.035f, 0.11f), new Vector3(0.045f, 0.005f, 0.045f), p.antiqueBrass);

        // Distinguished Silver Anime Hair
        GameObject hair = new GameObject("HairRoot");
        hair.transform.SetParent(head.transform, false);
        CreatePart(hair.transform, "SilverHairTop", PrimitiveType.Sphere, new Vector3(0f, 0.11f, 0f), new Vector3(0.26f, 0.20f, 0.26f), p.npcMayorHair);
        CreatePart(hair.transform, "PartedFringe", PrimitiveType.Cube, new Vector3(-0.04f, 0.08f, 0.10f), new Vector3(0.16f, 0.09f, 0.08f), p.npcMayorHair);

        BuildSlenderLimb(chest.transform, new Vector3(-0.21f, 0.18f, 0f), p.npcMayorCoat, p.personaSkin, false);
        BuildSlenderLimb(chest.transform, new Vector3(0.21f, 0.18f, 0f), p.npcMayorCoat, p.personaSkin, false);

        BuildSlenderLeg(hips.transform, new Vector3(-0.095f, -0.06f, 0f), p.personaPants, p.personaShoes);
        BuildSlenderLeg(hips.transform, new Vector3(0.095f, -0.06f, 0f), p.personaPants, p.personaShoes);
    }

    private static void BuildKeithPersonaModel(Transform parent, WorldPalette p)
    {
        // Apprentice blacksmith student
        GameObject root = new GameObject("AvatarRigRoot");
        root.transform.SetParent(parent, false);

        GameObject hips = CreateBone(root.transform, "HipsBone", new Vector3(0f, 0.98f, 0f));
        CreatePart(hips.transform, "HipsMesh", PrimitiveType.Cube, Vector3.zero, new Vector3(0.30f, 0.18f, 0.22f), p.personaPants);

        GameObject torso = CreateBone(hips.transform, "TorsoBone", new Vector3(0f, 0.15f, 0f));
        CreatePart(torso.transform, "Shirt", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.12f, 0.20f), p.personaShirt);

        GameObject chest = CreateBone(torso.transform, "ChestBone", new Vector3(0f, 0.20f, 0f));
        CreatePart(chest.transform, "ChestShirt", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(0.36f, 0.26f, 0.22f), p.personaShirt);
        // Heavy Leather Apron
        CreatePart(chest.transform, "LeatherApron", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.12f), new Vector3(0.28f, 0.38f, 0.04f), p.npcKeithApron);

        GameObject neck = CreateBone(chest.transform, "NeckBone", new Vector3(0f, 0.25f, 0f));
        CreatePart(neck.transform, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.10f, 0.06f, 0.10f), p.personaSkin);

        GameObject head = CreateBone(neck.transform, "HeadBone", new Vector3(0f, 0.12f, 0f));
        CreatePart(head.transform, "Cranium", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.22f, 0.24f, 0.22f), p.personaSkin);
        CreatePart(head.transform, "Jaw", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.04f), new Vector3(0.13f, 0.10f, 0.12f), p.personaSkin);

        // Undercut anime hair
        GameObject hair = new GameObject("HairRoot");
        hair.transform.SetParent(head.transform, false);
        CreatePart(hair.transform, "HairTop", PrimitiveType.Sphere, new Vector3(0f, 0.12f, 0f), new Vector3(0.24f, 0.18f, 0.24f), p.personaHair);

        BuildSlenderLimb(chest.transform, new Vector3(-0.21f, 0.18f, 0f), p.personaSkin, p.npcKeithApron, true);
        BuildSlenderLimb(chest.transform, new Vector3(0.21f, 0.18f, 0f), p.personaSkin, p.npcKeithApron, true);

        BuildSlenderLeg(hips.transform, new Vector3(-0.095f, -0.06f, 0f), p.personaPants, p.personaShoes);
        BuildSlenderLeg(hips.transform, new Vector3(0.095f, -0.06f, 0f), p.personaPants, p.personaShoes);
    }

    private static void BuildCherylPersonaModel(Transform parent, WorldPalette p)
    {
        // Alchemist student with potion bandolier
        GameObject root = new GameObject("AvatarRigRoot");
        root.transform.SetParent(parent, false);

        GameObject hips = CreateBone(root.transform, "HipsBone", new Vector3(0f, 0.95f, 0f));
        CreatePart(hips.transform, "Skirt", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0f), new Vector3(0.34f, 0.20f, 0.34f), p.npcCherylVest);

        GameObject torso = CreateBone(hips.transform, "TorsoBone", new Vector3(0f, 0.14f, 0f));
        CreatePart(torso.transform, "Vest", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.24f, 0.12f, 0.16f), p.npcCherylVest);

        GameObject chest = CreateBone(torso.transform, "ChestBone", new Vector3(0f, 0.18f, 0f));
        CreatePart(chest.transform, "ChestShirt", PrimitiveType.Cube, new Vector3(0f, 0.10f, 0f), new Vector3(0.28f, 0.20f, 0.18f), p.personaShirt);
        // Potion Bandolier with glowing vials
        CreatePart(chest.transform, "BandolierStrap", PrimitiveType.Cube, new Vector3(0f, 0.10f, 0.10f), new Vector3(0.06f, 0.28f, 0.02f), p.grimoireLeather);
        CreatePart(chest.transform, "Vial_1", PrimitiveType.Cylinder, new Vector3(-0.05f, 0.14f, 0.12f), new Vector3(0.03f, 0.06f, 0.03f), p.potionRuby);
        CreatePart(chest.transform, "Vial_2", PrimitiveType.Cylinder, new Vector3(0.04f, 0.06f, 0.12f), new Vector3(0.03f, 0.06f, 0.03f), p.potionAzure);

        GameObject neck = CreateBone(chest.transform, "NeckBone", new Vector3(0f, 0.22f, 0f));
        CreatePart(neck.transform, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.08f, 0.06f, 0.08f), p.personaSkin);

        GameObject head = CreateBone(neck.transform, "HeadBone", new Vector3(0f, 0.11f, 0f));
        CreatePart(head.transform, "Cranium", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.20f, 0.22f, 0.20f), p.personaSkin);
        CreatePart(head.transform, "Jaw", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.035f), new Vector3(0.11f, 0.09f, 0.10f), p.personaSkin);

        // Side ponytail anime hair
        GameObject hair = new GameObject("HairRoot");
        hair.transform.SetParent(head.transform, false);
        CreatePart(hair.transform, "HairTop", PrimitiveType.Sphere, new Vector3(0f, 0.10f, 0f), new Vector3(0.24f, 0.18f, 0.24f), p.personaHair);
        CreatePart(hair.transform, "SidePonytail", PrimitiveType.Sphere, new Vector3(0.14f, 0.08f, -0.08f), new Vector3(0.12f, 0.20f, 0.12f), p.personaHair);

        BuildSlenderLimb(chest.transform, new Vector3(-0.18f, 0.16f, 0f), p.npcCherylVest, p.personaSkin, false);
        BuildSlenderLimb(chest.transform, new Vector3(0.18f, 0.16f, 0f), p.npcCherylVest, p.personaSkin, false);

        BuildSlenderLeg(hips.transform, new Vector3(-0.08f, -0.12f, 0f), p.personaPants, p.personaShoes);
        BuildSlenderLeg(hips.transform, new Vector3(0.08f, -0.12f, 0f), p.personaPants, p.personaShoes);
    }

    private static void BuildZephyrPersonaModel(Transform parent, WorldPalette p)
    {
        // Forest Scout student with hooded capelet
        GameObject root = new GameObject("AvatarRigRoot");
        root.transform.SetParent(parent, false);

        GameObject hips = CreateBone(root.transform, "HipsBone", new Vector3(0f, 0.98f, 0f));
        CreatePart(hips.transform, "HipsMesh", PrimitiveType.Cube, Vector3.zero, new Vector3(0.28f, 0.18f, 0.20f), p.personaPants);

        GameObject torso = CreateBone(hips.transform, "TorsoBone", new Vector3(0f, 0.14f, 0f));
        CreatePart(torso.transform, "TorsoMesh", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.26f, 0.10f, 0.18f), p.npcZephyrCloak);

        GameObject chest = CreateBone(torso.transform, "ChestBone", new Vector3(0f, 0.20f, 0f));
        CreatePart(chest.transform, "ChestMesh", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(0.34f, 0.24f, 0.22f), p.npcZephyrCloak);
        // Travel capelet
        CreatePart(chest.transform, "Capelet", PrimitiveType.Cube, new Vector3(0f, 0.14f, -0.10f), new Vector3(0.38f, 0.30f, 0.06f), p.npcZephyrCloak);

        GameObject neck = CreateBone(chest.transform, "NeckBone", new Vector3(0f, 0.24f, 0f));
        CreatePart(neck.transform, "NeckMesh", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.10f, 0.06f, 0.10f), p.personaSkin);

        GameObject head = CreateBone(neck.transform, "HeadBone", new Vector3(0f, 0.12f, 0f));
        CreatePart(head.transform, "Cranium", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.22f, 0.24f, 0.22f), p.personaSkin);
        CreatePart(head.transform, "JawChin", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.04f), new Vector3(0.13f, 0.10f, 0.12f), p.personaSkin);

        GameObject hair = new GameObject("HairRoot");
        hair.transform.SetParent(head.transform, false);
        CreatePart(hair.transform, "WindblownHair", PrimitiveType.Sphere, new Vector3(0f, 0.11f, 0f), new Vector3(0.26f, 0.20f, 0.26f), p.personaHair);

        BuildSlenderLimb(chest.transform, new Vector3(-0.21f, 0.18f, 0f), p.npcZephyrCloak, p.personaSkin, false);
        BuildSlenderLimb(chest.transform, new Vector3(0.21f, 0.18f, 0f), p.npcZephyrCloak, p.personaSkin, false);

        BuildSlenderLeg(hips.transform, new Vector3(-0.095f, -0.06f, 0f), p.personaPants, p.personaShoes);
        BuildSlenderLeg(hips.transform, new Vector3(0.095f, -0.06f, 0f), p.personaPants, p.personaShoes);
    }

    private static void BuildGregoryPersonaModel(Transform parent, WorldPalette p)
    {
        // Spire Warden in dark slate trenchcoat
        GameObject root = new GameObject("AvatarRigRoot");
        root.transform.SetParent(parent, false);

        GameObject hips = CreateBone(root.transform, "HipsBone", new Vector3(0f, 0.98f, 0f));
        CreatePart(hips.transform, "HipsMesh", PrimitiveType.Cube, Vector3.zero, new Vector3(0.30f, 0.18f, 0.22f), p.personaPants);

        GameObject torso = CreateBone(hips.transform, "TorsoBone", new Vector3(0f, 0.15f, 0f));
        CreatePart(torso.transform, "CoatTorso", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.12f, 0.20f), p.npcGregoryCoat);

        GameObject chest = CreateBone(torso.transform, "ChestBone", new Vector3(0f, 0.20f, 0f));
        CreatePart(chest.transform, "TrenchcoatChest", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(0.36f, 0.26f, 0.24f), p.npcGregoryCoat);
        // Brass pauldrons
        CreatePart(chest.transform, "Pauldron_L", PrimitiveType.Cylinder, new Vector3(-0.22f, 0.22f, 0f), new Vector3(0.12f, 0.04f, 0.12f), p.antiqueBrass);
        CreatePart(chest.transform, "Pauldron_R", PrimitiveType.Cylinder, new Vector3(0.22f, 0.22f, 0f), new Vector3(0.12f, 0.04f, 0.12f), p.antiqueBrass);

        GameObject neck = CreateBone(chest.transform, "NeckBone", new Vector3(0f, 0.25f, 0f));
        CreatePart(neck.transform, "Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.10f, 0.06f, 0.10f), p.personaSkin);

        GameObject head = CreateBone(neck.transform, "HeadBone", new Vector3(0f, 0.12f, 0f));
        CreatePart(head.transform, "Cranium", PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.22f, 0.24f, 0.22f), p.personaSkin);
        CreatePart(head.transform, "Jaw", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.04f), new Vector3(0.13f, 0.10f, 0.12f), p.personaSkin);

        GameObject hair = new GameObject("HairRoot");
        hair.transform.SetParent(head.transform, false);
        CreatePart(hair.transform, "SternHair", PrimitiveType.Sphere, new Vector3(0f, 0.11f, 0f), new Vector3(0.26f, 0.20f, 0.26f), p.personaHair);

        BuildSlenderLimb(chest.transform, new Vector3(-0.21f, 0.18f, 0f), p.npcGregoryCoat, p.personaSkin, false);
        BuildSlenderLimb(chest.transform, new Vector3(0.21f, 0.18f, 0f), p.npcGregoryCoat, p.personaSkin, false);

        BuildSlenderLeg(hips.transform, new Vector3(-0.095f, -0.06f, 0f), p.personaPants, p.personaShoes);
        BuildSlenderLeg(hips.transform, new Vector3(0.095f, -0.06f, 0f), p.personaPants, p.personaShoes);
    }

    private static void BuildSlenderLimb(Transform chest, Vector3 shoulderPos, Material sleeveMat, Material handMat, bool rolledSleeve)
    {
        GameObject upper = CreateBone(chest, "UpperArm", shoulderPos);
        CreatePart(upper.transform, "Bicep", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0f), new Vector3(0.09f, 0.13f, 0.09f), sleeveMat);

        GameObject fore = CreateBone(upper.transform, "Forearm", new Vector3(0f, -0.25f, 0f));
        CreatePart(fore.transform, "ForearmMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, 0f), new Vector3(0.08f, 0.12f, 0.08f), rolledSleeve ? handMat : sleeveMat);

        GameObject hand = CreateBone(fore.transform, "Hand", new Vector3(0f, -0.24f, 0f));
        CreatePart(hand.transform, "HandMesh", PrimitiveType.Sphere, new Vector3(0f, -0.035f, 0f), new Vector3(0.065f, 0.075f, 0.055f), handMat);
    }

    private static void BuildSlenderLeg(Transform hips, Vector3 hipOffset, Material pantsMat, Material shoeMat)
    {
        GameObject thigh = CreateBone(hips, "Thigh", hipOffset);
        CreatePart(thigh.transform, "ThighMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.20f, 0f), new Vector3(0.12f, 0.21f, 0.12f), pantsMat);

        GameObject shin = CreateBone(thigh.transform, "Shin", new Vector3(0f, -0.42f, 0f));
        CreatePart(shin.transform, "ShinMesh", PrimitiveType.Cylinder, new Vector3(0f, -0.22f, 0f), new Vector3(0.10f, 0.23f, 0.10f), pantsMat);

        GameObject foot = CreateBone(shin.transform, "Foot", new Vector3(0f, -0.45f, 0f));
        CreatePart(foot.transform, "ShoeMesh", PrimitiveType.Cube, new Vector3(0f, -0.035f, 0.055f), new Vector3(0.095f, 0.06f, 0.23f), shoeMat);
    }
    #endregion

    #region Town Fountain & Props Upgrades
    public static void TransformTownFountain(WorldPalette p)
    {
        var fountainObj = GameObject.Find("TownFountain");
        if (fountainObj == null) return;

        // Destroy old primitive cylinders inside TownFountain
        for (int i = fountainObj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(fountainObj.transform.GetChild(i).gameObject);
        }

        Vector3 center = Vector3.zero;

        // 1. Gothic Carved Octagonal Stone Basin Rim
        int sides = 8;
        float radius = 3.1f;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * (360f / sides) * Mathf.Deg2Rad;
            float nextAngle = (i + 1) * (360f / sides) * Mathf.Deg2Rad;
            Vector3 p1 = new Vector3(Mathf.Sin(angle) * radius, 0.35f, Mathf.Cos(angle) * radius);
            Vector3 p2 = new Vector3(Mathf.Sin(nextAngle) * radius, 0.35f, Mathf.Cos(nextAngle) * radius);
            Vector3 mid = (p1 + p2) * 0.5f;
            float length = Vector3.Distance(p1, p2);

            var wall = CreatePart(fountainObj.transform, $"BasinWall_{i}", PrimitiveType.Cube, mid, new Vector3(0.35f, 0.70f, length + 0.1f), p.carvedStone);
            wall.transform.LookAt(fountainObj.transform.position + mid + (p2 - p1));
        }

        // 2. Enchanted Water Pool Surface
        var water = CreatePart(fountainObj.transform, "EnchantedWaterSurface", PrimitiveType.Cylinder, new Vector3(0f, 0.52f, 0f), new Vector3(5.8f, 0.05f, 5.8f), p.waterSapphire);
        var waterMat = AssetDatabase.LoadAssetAtPath<Material>(WORLD_MAT_DIR + "Mat_World_FountainWater.mat");
        if (waterMat != null) water.GetComponent<Renderer>().sharedMaterial = waterMat;

        // 3. Central Fluted Stone Pedestal (Tier 1)
        CreatePart(fountainObj.transform, "PedestalBase", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(1.6f, 0.50f, 1.6f), p.carvedStone);
        CreatePart(fountainObj.transform, "PedestalShaft", PrimitiveType.Cylinder, new Vector3(0f, 1.30f, 0f), new Vector3(0.90f, 1.10f, 0.90f), p.carvedStone);

        // 4. Upper Stone Basin (Tier 2)
        CreatePart(fountainObj.transform, "UpperBasin", PrimitiveType.Cylinder, new Vector3(0f, 1.95f, 0f), new Vector3(2.2f, 0.25f, 2.2f), p.carvedStone);
        var upperWater = CreatePart(fountainObj.transform, "UpperWater", PrimitiveType.Cylinder, new Vector3(0f, 2.05f, 0f), new Vector3(2.0f, 0.05f, 2.0f), p.waterSapphire);
        if (waterMat != null) upperWater.GetComponent<Renderer>().sharedMaterial = waterMat;

        // 5. Celestial Armillary Astrolabe Spout atop Fountain
        CreatePart(fountainObj.transform, "AstrolabePedestal", PrimitiveType.Cylinder, new Vector3(0f, 2.30f, 0f), new Vector3(0.50f, 0.40f, 0.50f), p.antiqueBrass);

        // Interlocking Brass Rings
        var ring1 = CreatePart(fountainObj.transform, "BrassRing_1", PrimitiveType.Cylinder, new Vector3(0f, 2.85f, 0f), new Vector3(1.10f, 0.04f, 1.10f), p.antiqueBrass);
        ring1.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        var ring2 = CreatePart(fountainObj.transform, "BrassRing_2", PrimitiveType.Cylinder, new Vector3(0f, 2.85f, 0f), new Vector3(1.10f, 0.04f, 1.10f), p.antiqueBrass);
        ring2.transform.localRotation = Quaternion.Euler(-45f, 60f, 0f);

        // Glowing Sapphire Core Orb
        CreatePart(fountainObj.transform, "CelestialWaterCore", PrimitiveType.Sphere, new Vector3(0f, 2.85f, 0f), new Vector3(0.45f, 0.45f, 0.45f), p.potionAzure);

        // Gentle Aquatic Point Light
        GameObject flObj = new GameObject("FountainAuraLight", typeof(Light));
        flObj.transform.SetParent(fountainObj.transform, false);
        flObj.transform.localPosition = new Vector3(0f, 2.85f, 0f);
        Light fl = flObj.GetComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.35f, 0.75f, 1.0f);
        fl.range = 8.5f;
        fl.intensity = 1.8f;

        Debug.Log("⛲ [FOUNTAIN] Upgraded TownFountain to ornate Gothic Astrolabe Fountain!");
    }

    public static void UpgradeSolasTownLanternsAndStalls(WorldPalette p)
    {
        var hub = GameObject.Find("SolasTownHub");
        if (hub == null) return;

        // Replace 4 old StreetLanterns with GothicLampposts
        for (int i = hub.transform.childCount - 1; i >= 0; i--)
        {
            var child = hub.transform.GetChild(i);
            if (child.name.StartsWith("StreetLantern"))
            {
                Vector3 pos = child.position;
                Object.DestroyImmediate(child.gameObject);
                CreateGothicLamppost(hub.transform, pos, p);
            }
        }

        // Upgrade Market Stalls with Velvet Canopies and Alchemical Props
        for (int i = 0; i < hub.transform.childCount; i++)
        {
            var child = hub.transform.GetChild(i);
            if (child.name.StartsWith("MarketStall"))
            {
                var canopy = child.Find("Canopy");
                if (canopy != null)
                {
                    bool isAlchemist = child.position.x > -12f;
                    canopy.GetComponent<Renderer>().sharedMaterial = isAlchemist ? p.npcMayorCoat : p.npcMargoDress;
                    canopy.localScale = new Vector3(2.8f, 0.22f, 1.9f);
                }

                // Add Grimoires and Potion Phials on Counter
                var table = child.Find("StallTable");
                if (table != null)
                {
                    table.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(WORLD_MAT_DIR + "Mat_World_WoodPlank.mat") ?? p.bark;

                    // Props on table
                    CreatePart(child, "Tome_1", PrimitiveType.Cube, table.localPosition + new Vector3(-0.6f, 0.55f, 0f), new Vector3(0.35f, 0.08f, 0.45f), p.grimoireLeather);
                    CreatePart(child, "PotionFlask_1", PrimitiveType.Cylinder, table.localPosition + new Vector3(0.4f, 0.60f, -0.2f), new Vector3(0.08f, 0.16f, 0.08f), p.potionRuby);
                    CreatePart(child, "PotionFlask_2", PrimitiveType.Cylinder, table.localPosition + new Vector3(0.6f, 0.60f, 0.2f), new Vector3(0.08f, 0.16f, 0.08f), p.potionAzure);
                    CreatePart(child, "ScaleBrass", PrimitiveType.Cylinder, table.localPosition + new Vector3(0f, 0.65f, 0f), new Vector3(0.14f, 0.24f, 0.14f), p.antiqueBrass);
                }
            }
        }
    }
    #endregion

    #region Scene Population
    public static void PopulateOverworldScene(WorldPalette p)
    {
        string scenePath = "Assets/Scenes/OverworldMap.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Apply ground and building textures
        ApplyDarkAcademiaWorldTextures();

        // Transform Town Fountain
        TransformTownFountain(p);

        // Upgrade Solas Town Lanterns and Stalls
        UpgradeSolasTownLanternsAndStalls(p);

        // Build Persona NPCs
        BuildPersonaNPCs(p);

        // Clean any old procedural scenery root
        var oldScenery = GameObject.Find("DarkAcademia_SceneryRoot");
        if (oldScenery != null) Object.DestroyImmediate(oldScenery);

        GameObject root = new GameObject("DarkAcademia_SceneryRoot");

        // 1. Street Gaslight Lampposts along Cobblestone Plaza & Spire Avenue
        Vector3[] lampPositions = {
            new Vector3(-9f, 0f, -20f),
            new Vector3(-17f, 0f, -20f),
            new Vector3(-9f, 0f, -8f),
            new Vector3(-17f, 0f, -8f),
            new Vector3(-13f, 0f, 0f),
            new Vector3(-13f, 0f, 8f),
            new Vector3(-13f, 0f, 16f),
            new Vector3(-13f, 0f, 24f),
            new Vector3(-7f, 0f, 28f),
            new Vector3(-19f, 0f, 28f)
        };
        foreach (var pos in lampPositions)
        {
            CreateGothicLamppost(root.transform, pos, p);
        }

        // 2. Carved Stone Scholar Benches (Flanking Plaza and Fountain)
        Vector3[] benchPositions = {
            new Vector3(-18f, 0f, -14f),
            new Vector3(-8f, 0f, -14f),
            new Vector3(-16f, 0f, -18f),
            new Vector3(-10f, 0f, -18f),
            new Vector3(-13f, 0f, -3f)
        };
        for (int i = 0; i < benchPositions.Length; i++)
        {
            float rotY = (i < 2) ? 90f : 0f;
            CreateScholarBench(root.transform, benchPositions[i], rotY, p);
        }

        // 3. Gothic Cypress Trees (Framing plaza corners & Spire avenue - strictly avoiding fountain)
        Vector3[] cypressPositions = {
            new Vector3(-23f, 0f, -12f),
            new Vector3(-23f, 0f, -16f),
            new Vector3(-3f, 0f, -12f),
            new Vector3(-3f, 0f, -16f),
            new Vector3(-17f, 0f, 2f),
            new Vector3(-9f, 0f, 2f),
            new Vector3(-17f, 0f, 10f),
            new Vector3(-9f, 0f, 10f),
            new Vector3(-17f, 0f, 18f),
            new Vector3(-9f, 0f, 18f),
            new Vector3(-19f, 0f, 26f),
            new Vector3(-7f, 0f, 26f)
        };
        foreach (var pos in cypressPositions)
        {
            float h = Random.Range(6.0f, 8.5f);
            CreateGothicCypressTree(root.transform, pos, h, p);
        }

        // 4. Ancient Gnarled Oaks in Mistwood Forest Zone (Centered around x=35, z=0)
        Vector3[] oakPositions = {
            new Vector3(28f, 0f, -14f),
            new Vector3(36f, 0f, -12f),
            new Vector3(42f, 0f, -6f),
            new Vector3(32f, 0f, 4f),
            new Vector3(40f, 0f, 8f),
            new Vector3(28f, 0f, 16f),
            new Vector3(38f, 0f, 18f)
        };
        foreach (var pos in oakPositions)
        {
            float s = Random.Range(1.15f, 1.5f);
            CreateGnarledOakTree(root.transform, pos, s, p);
        }

        // 5. Mossy Rock Formations along paths and forest edge
        Vector3[] rockPositions = {
            new Vector3(-5f, 0f, -8f),
            new Vector3(5f, 0f, -12f),
            new Vector3(18f, 0f, -10f),
            new Vector3(25f, 0f, -6f),
            new Vector3(30f, 0f, 0f),
            new Vector3(-18f, 0f, 22f),
            new Vector3(-8f, 0f, 32f)
        };
        foreach (var pos in rockPositions)
        {
            CreateMossyRockFormation(root.transform, pos, p);
        }

        // 6. Lush Fern Clusters around building foundations and trees
        Vector3[] fernPositions = {
            new Vector3(-23f, 0f, -4f),
            new Vector3(-25f, 0f, -6f),
            new Vector3(-3f, 0f, -23f),
            new Vector3(1f, 0f, -4f),
            new Vector3(-7f, 0f, -16f),
            new Vector3(-19f, 0f, -16f),
            new Vector3(-15f, 0f, -19f),
            new Vector3(-11f, 0f, -19f),
            new Vector3(-15f, 0f, -9f),
            new Vector3(-11f, 0f, -9f),
            new Vector3(14f, 0f, -14f),
            new Vector3(20f, 0f, -12f)
        };
        foreach (var pos in fernPositions)
        {
            CreateFernCluster(root.transform, pos, p);
        }

        // 7. Arcane Grimoire Pedestals
        CreateGrimoirePedestal(root.transform, new Vector3(-13f, 0f, -22f), p);
        CreateGrimoirePedestal(root.transform, new Vector3(-13f, 0f, 20f), p);

        // 8. Sentinel Gargoyle Statues at Spire Outpost
        CreateGargoyleStatue(root.transform, new Vector3(-16f, 0f, 28f), 30f, p);
        CreateGargoyleStatue(root.transform, new Vector3(-10f, 0f, 28f), -30f, p);

        // Build Persona protagonist in Overworld
        BuildPersonaProtagonistInActiveScene(p);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("🌲 [OVERWORLD] Successfully refreshed Overworld with dark academia textures, fountain, greenery, lampposts, and Persona characters!");
    }

    public static void PopulateDungeonFloorScene(WorldPalette p)
    {
        string scenePath = "Assets/Scenes/MainDungeonFloor.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        ApplyDarkAcademiaWorldTextures();

        var oldScenery = GameObject.Find("DarkAcademia_DungeonProps");
        if (oldScenery != null) Object.DestroyImmediate(oldScenery);

        GameObject root = new GameObject("DarkAcademia_DungeonProps");

        // 1. Cast Iron Braziers with warm flame light around arena perimeter
        Vector3[] brazierPositions = {
            new Vector3(-9f, 0f, -9f),
            new Vector3(9f, 0f, -9f),
            new Vector3(-9f, 0f, 9f),
            new Vector3(9f, 0f, 9f),
            new Vector3(-12f, 0f, 0f),
            new Vector3(12f, 0f, 0f)
        };
        foreach (var pos in brazierPositions)
        {
            CreateIronBrazier(root.transform, pos, p);
        }

        // 2. Scholar Grimoire Lecterns around arena pillars
        CreateGrimoirePedestal(root.transform, new Vector3(-6f, 0f, 6f), p);
        CreateGrimoirePedestal(root.transform, new Vector3(6f, 0f, 6f), p);

        // 3. Gothic Sentinel Gargoyles guarding arena exits
        CreateGargoyleStatue(root.transform, new Vector3(-4f, 0f, 11f), 180f, p);
        CreateGargoyleStatue(root.transform, new Vector3(4f, 0f, 11f), 180f, p);

        // 4. Creeping Moss & Ferns around arena perimeter stones
        Vector3[] floorFernPositions = {
            new Vector3(-11f, 0f, -5f),
            new Vector3(-11f, 0f, 5f),
            new Vector3(11f, 0f, -5f),
            new Vector3(11f, 0f, 5f),
            new Vector3(-5f, 0f, -11f),
            new Vector3(5f, 0f, -11f)
        };
        foreach (var pos in floorFernPositions)
        {
            CreateFernCluster(root.transform, pos, p);
        }

        // Build Persona protagonist in Dungeon Floor
        BuildPersonaProtagonistInActiveScene(p);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("🏛️ [DUNGEON FLOOR] Successfully updated MainDungeonFloor with braziers, lecterns, gargoyles, moss, and Persona protagonist!");
    }
    #endregion

    #region Scenery Prefab Builders
    public static GameObject CreateGothicLamppost(Transform parent, Vector3 pos, WorldPalette p)
    {
        GameObject lamp = new GameObject("GothicLamppost");
        lamp.transform.SetParent(parent, false);
        lamp.transform.position = pos;

        // Base & Fluted Shaft
        CreatePart(lamp.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.45f, 0.2f, 0.45f), p.wroughtIron);
        CreatePart(lamp.transform, "Shaft", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.12f, 1.6f, 0.12f), p.wroughtIron);
        CreatePart(lamp.transform, "Collar", PrimitiveType.Sphere, new Vector3(0f, 3.4f, 0f), new Vector3(0.24f, 0.15f, 0.24f), p.antiqueBrass);

        // Lantern Cage
        CreatePart(lamp.transform, "LanternCage", PrimitiveType.Cube, new Vector3(0f, 3.75f, 0f), new Vector3(0.35f, 0.45f, 0.35f), p.wroughtIron);
        CreatePart(lamp.transform, "FlameCore", PrimitiveType.Sphere, new Vector3(0f, 3.75f, 0f), new Vector3(0.16f, 0.24f, 0.16f), p.lanternFlame);

        // Point Light
        GameObject lightObj = new GameObject("LanternLight", typeof(Light));
        lightObj.transform.SetParent(lamp.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 3.75f, 0f);
        Light l = lightObj.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.78f, 0.45f);
        l.range = 8.5f;
        l.intensity = 1.8f;

        return lamp;
    }

    public static GameObject CreateScholarBench(Transform parent, Vector3 pos, float rotY, WorldPalette p)
    {
        GameObject bench = new GameObject("ScholarBench");
        bench.transform.SetParent(parent, false);
        bench.transform.position = pos;
        bench.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        // Stone seat slab
        CreatePart(bench.transform, "Slab", PrimitiveType.Cube, new Vector3(0f, 0.50f, 0f), new Vector3(1.8f, 0.10f, 0.55f), p.carvedStone);
        CreatePart(bench.transform, "LegL", PrimitiveType.Cube, new Vector3(-0.65f, 0.22f, 0f), new Vector3(0.22f, 0.44f, 0.45f), p.carvedStone);
        CreatePart(bench.transform, "LegR", PrimitiveType.Cube, new Vector3(0.65f, 0.22f, 0f), new Vector3(0.22f, 0.44f, 0.45f), p.carvedStone);
        CreatePart(bench.transform, "Backrest", PrimitiveType.Cube, new Vector3(0f, 0.85f, -0.24f), new Vector3(1.8f, 0.50f, 0.08f), p.carvedStone);

        return bench;
    }

    public static GameObject CreateGothicCypressTree(Transform parent, Vector3 pos, float height, WorldPalette p)
    {
        GameObject tree = new GameObject("GothicCypress");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;

        // Slender tall trunk
        float trunkH = height * 0.35f;
        CreatePart(tree.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, trunkH * 0.5f, 0f), new Vector3(0.30f, trunkH * 0.5f, 0.30f), p.bark);

        // 5 Tapered Evergreen Needle Tiers
        int tiers = 5;
        for (int i = 0; i < tiers; i++)
        {
            float prog = (float)i / tiers;
            float y = trunkH * 0.7f + prog * (height - trunkH);
            float radius = Mathf.Lerp(1.6f, 0.35f, prog);
            float tierH = (height - trunkH) / tiers * 1.35f;

            var part = CreatePart(tree.transform, $"NeedleTier_{i}", PrimitiveType.Cylinder, new Vector3(0f, y, 0f), new Vector3(radius, tierH * 0.5f, radius), p.pineNeedles);
            part.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
        }

        // Moss skirt at base
        CreatePart(tree.transform, "BaseMoss", PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0f), new Vector3(0.9f, 0.25f, 0.9f), p.moss);
        return tree;
    }

    public static GameObject CreateGnarledOakTree(Transform parent, Vector3 pos, float scale, WorldPalette p)
    {
        GameObject tree = new GameObject("GnarledOak");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;

        // Central twisted trunk
        CreatePart(tree.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.8f * scale, 0f), new Vector3(0.75f * scale, 1.8f * scale, 0.75f * scale), p.bark);

        // 3 Angled Boughs
        Vector3[] boughDirs = {
            new Vector3(1.2f, 3.0f, 0f) * scale,
            new Vector3(-0.9f, 3.2f, 1.0f) * scale,
            new Vector3(-0.6f, 2.9f, -1.2f) * scale
        };
        for (int b = 0; b < boughDirs.Length; b++)
        {
            var bough = CreatePart(tree.transform, $"Bough_{b}", PrimitiveType.Cylinder, boughDirs[b] * 0.6f, new Vector3(0.35f, 1.0f, 0.35f) * scale, p.bark);
            bough.transform.LookAt(tree.transform.position + boughDirs[b]);
        }

        // 4 Spreading Canopy Lobes
        Vector3[] canopyOffsets = {
            new Vector3(0f, 4.5f, 0f) * scale,
            new Vector3(1.6f, 4.2f, 0.8f) * scale,
            new Vector3(-1.4f, 4.3f, 1.1f) * scale,
            new Vector3(-0.8f, 4.1f, -1.5f) * scale
        };
        foreach (var cOff in canopyOffsets)
        {
            CreatePart(tree.transform, "CanopyLobe", PrimitiveType.Sphere, cOff, new Vector3(3.2f, 2.4f, 3.2f) * scale, p.foliage);
        }

        return tree;
    }

    public static GameObject CreateMossyRockFormation(Transform parent, Vector3 pos, WorldPalette p)
    {
        GameObject rock = new GameObject("MossyRockFormation");
        rock.transform.SetParent(parent, false);
        rock.transform.position = pos;

        CreatePart(rock.transform, "Boulder_Main", PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 1.2f, 1.6f), p.carvedStone);
        CreatePart(rock.transform, "Boulder_Sub", PrimitiveType.Sphere, new Vector3(0.9f, 0.35f, 0.4f), new Vector3(1.1f, 0.7f, 1.0f), p.carvedStone);
        CreatePart(rock.transform, "MossCap", PrimitiveType.Sphere, new Vector3(0.1f, 1.05f, 0.05f), new Vector3(1.4f, 0.4f, 1.3f), p.moss);

        return rock;
    }

    public static GameObject CreateFernCluster(Transform parent, Vector3 pos, WorldPalette p)
    {
        GameObject fern = new GameObject("FernCluster");
        fern.transform.SetParent(parent, false);
        fern.transform.position = pos;

        for (int i = 0; i < 4; i++)
        {
            float rotY = i * 45f;
            var frond = CreatePart(fern.transform, $"Frond_{i}", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(1.1f, 0.45f, 0.08f), p.moss);
            frond.transform.rotation = Quaternion.Euler(18f, rotY, 0f);
        }
        return fern;
    }

    public static GameObject CreateGrimoirePedestal(Transform parent, Vector3 pos, WorldPalette p)
    {
        GameObject ped = new GameObject("GrimoirePedestal");
        ped.transform.SetParent(parent, false);
        ped.transform.position = pos;

        CreatePart(ped.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.3f, 0.7f), p.carvedStone);
        CreatePart(ped.transform, "Shaft", PrimitiveType.Cylinder, new Vector3(0f, 0.7f, 0f), new Vector3(0.35f, 0.5f, 0.35f), p.carvedStone);
        CreatePart(ped.transform, "LecternPlate", PrimitiveType.Cube, new Vector3(0f, 1.22f, 0f), new Vector3(0.85f, 0.08f, 0.65f), p.carvedStone);

        // Open Leather Grimoire
        var tomeL = CreatePart(ped.transform, "TomeLeaf_L", PrimitiveType.Cube, new Vector3(-0.16f, 1.30f, 0f), new Vector3(0.30f, 0.04f, 0.45f), p.parchmentPaper);
        tomeL.transform.rotation = Quaternion.Euler(0f, 0f, 12f);
        var tomeR = CreatePart(ped.transform, "TomeLeaf_R", PrimitiveType.Cube, new Vector3(0.16f, 1.30f, 0f), new Vector3(0.30f, 0.04f, 0.45f), p.parchmentPaper);
        tomeR.transform.rotation = Quaternion.Euler(0f, 0f, -12f);

        // Candle Sconce
        CreatePart(ped.transform, "Candle", PrimitiveType.Cylinder, new Vector3(0.32f, 1.35f, 0.20f), new Vector3(0.04f, 0.10f, 0.04f), p.parchmentPaper);
        CreatePart(ped.transform, "Flame", PrimitiveType.Sphere, new Vector3(0.32f, 1.48f, 0.20f), new Vector3(0.04f, 0.06f, 0.04f), p.lanternFlame);

        // Candle light
        GameObject lightObj = new GameObject("CandleLight", typeof(Light));
        lightObj.transform.SetParent(ped.transform, false);
        lightObj.transform.localPosition = new Vector3(0.32f, 1.50f, 0.20f);
        Light cl = lightObj.GetComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(1.0f, 0.75f, 0.35f);
        cl.range = 3.5f;
        cl.intensity = 1.2f;

        return ped;
    }

    public static GameObject CreateGargoyleStatue(Transform parent, Vector3 pos, float rotY, WorldPalette p)
    {
        GameObject garg = new GameObject("GargoyleSentinel");
        garg.transform.SetParent(parent, false);
        garg.transform.position = pos;
        garg.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        CreatePart(garg.transform, "Plinth", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.85f, 0.9f, 0.85f), p.carvedStone);
        CreatePart(garg.transform, "Body", PrimitiveType.Cube, new Vector3(0f, 1.25f, -0.05f), new Vector3(0.5f, 0.45f, 0.4f), p.carvedStone);
        CreatePart(garg.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.55f, 0.15f), new Vector3(0.32f, 0.30f, 0.35f), p.carvedStone);

        var wingL = CreatePart(garg.transform, "Wing_L", PrimitiveType.Cube, new Vector3(-0.35f, 1.6f, -0.15f), new Vector3(0.6f, 0.45f, 0.06f), p.carvedStone);
        wingL.transform.localRotation = Quaternion.Euler(15f, 35f, -25f);
        var wingR = CreatePart(garg.transform, "Wing_R", PrimitiveType.Cube, new Vector3(0.35f, 1.6f, -0.15f), new Vector3(0.6f, 0.45f, 0.06f), p.carvedStone);
        wingR.transform.localRotation = Quaternion.Euler(15f, -35f, 25f);

        return garg;
    }

    public static GameObject CreateIronBrazier(Transform parent, Vector3 pos, WorldPalette p)
    {
        GameObject braz = new GameObject("IronBrazier");
        braz.transform.SetParent(parent, false);
        braz.transform.position = pos;

        CreatePart(braz.transform, "BaseTripod", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.4f, 0.6f), p.wroughtIron);
        CreatePart(braz.transform, "Bowl", PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f), new Vector3(0.85f, 0.2f, 0.85f), p.wroughtIron);
        CreatePart(braz.transform, "Embers", PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0f), new Vector3(0.65f, 0.25f, 0.65f), p.lanternFlame);

        GameObject flObj = new GameObject("FireLight", typeof(Light));
        flObj.transform.SetParent(braz.transform, false);
        flObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        Light fl = flObj.GetComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(1.0f, 0.45f, 0.12f);
        fl.range = 8.0f;
        fl.intensity = 2.2f;

        return braz;
    }

    private static GameObject CreateBone(Transform parent, string name, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        return go;
    }

    private static GameObject CreatePart(Transform parent, string name, PrimitiveType prim, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(prim);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;
        if (mat != null) part.GetComponent<Renderer>().sharedMaterial = mat;

        var coll = part.GetComponent<Collider>();
        if (coll != null) Object.DestroyImmediate(coll);
        return part;
    }
    #endregion
}

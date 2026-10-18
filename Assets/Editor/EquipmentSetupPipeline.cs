using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EquipmentSetupPipeline
{
    private const string CHAR_DIR = "Assets/Models/Characters/";
    private const string EQUIP_DIR = "Assets/Models/Equipment/";
    private const string MAT_DIR = "Assets/Models/Equipment/Materials/";
    private const string PREFAB_DIR = "Assets/Prefabs/Equipment/";
    private const string CHAR_PREFAB_DIR = "Assets/Prefabs/Characters/";

    [MenuItem("BattleSummoner/Build All Equipment and Character Prefabs")]
    public static void BuildAllEquipmentAndPrefabs()
    {
        if (!Directory.Exists(MAT_DIR)) Directory.CreateDirectory(MAT_DIR);
        if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
        if (!Directory.Exists(CHAR_PREFAB_DIR)) Directory.CreateDirectory(CHAR_PREFAB_DIR);

        AssetDatabase.Refresh();

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // 1. Setup Base Naked Rigged Model
        SetupHumanoidModel(CHAR_DIR + "Character_Anime_BaseMesh_Rigged.fbx");
        SetupHumanoidModel(CHAR_DIR + "Character_Anime_Fitted_Equipped.fbx");

        // 2. Create Equipment Materials
        string[] equipList = new string[] {
            "Equipment_Helmet",
            "Equipment_Torso",
            "Equipment_Leggings",
            "Equipment_Shoes",
            "Weapon_TwoHanded_BattleAxe",
            "Weapon_Sword",
            "Weapon_Staff",
            "Weapon_Wand"
        };

        var equipMats = new Dictionary<string, Material>();
        foreach (var eq in equipList)
        {
            string matPath = $"{MAT_DIR}M_{eq}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null)
            {
                m = new Material(urpLit);
                AssetDatabase.CreateAsset(m, matPath);
            }

            string texPath = $"{EQUIP_DIR}Textures/tex_{eq}_0.png";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTexture("_MainTex", tex);
                m.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(m);
            }
            equipMats[eq] = m;
        }

        // Material for Base Character
        string charMatPath = $"{CHAR_DIR}M_Anime_Body.mat";
        Material charMat = AssetDatabase.LoadAssetAtPath<Material>(charMatPath);
        if (charMat == null)
        {
            charMat = new Material(urpLit);
            AssetDatabase.CreateAsset(charMat, charMatPath);
        }
        string charTexPath = $"{CHAR_DIR}Textures/tex_Character_Anime_BaseMesh_0.png";
        Texture2D charTex = AssetDatabase.LoadAssetAtPath<Texture2D>(charTexPath);
        if (charTex != null)
        {
            charMat.SetTexture("_BaseMap", charTex);
            charMat.SetTexture("_MainTex", charTex);
            charMat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(charMat);
        }

        // 3. Create Prefabs for all Weapons & Equipment pieces
        foreach (var eq in equipList)
        {
            CreateEquipmentPrefab(eq, equipMats[eq]);
        }

        // 4. Create Naked Rigged Character Prefab
        CreateRiggedCharacterPrefab("Character_Anime_BaseMesh_Rigged", charMat, false);

        // 5. Create Fully Equipped Character Prefab (Base Body + Fitted Helmet, Torso, Leggings, Shoes)
        CreateRiggedCharacterPrefab("Character_Anime_Fitted_Equipped", charMat, true, equipMats);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("⚔️ [EQUIPMENT PIPELINE] Successfully created all 8 equipment prefabs, base naked rigged character, and fully equipped character prefabs!");
    }

    private static void SetupHumanoidModel(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();
        }
    }

    private static void CreateEquipmentPrefab(string itemName, Material mat)
    {
        string fbxPath = $"{EQUIP_DIR}{itemName}.fbx";
        string prefabPath = $"{PREFAB_DIR}{itemName}.prefab";

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        instance.name = itemName;

        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterial = mat;
        }

        // Add BoxCollider for physics/pickup if needed
        var col = instance.AddComponent<BoxCollider>();
        col.isTrigger = true;

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void CreateRiggedCharacterPrefab(string fbxName, Material bodyMat, bool isEquipped, Dictionary<string, Material> equipMats = null)
    {
        string fbxPath = $"{CHAR_DIR}{fbxName}.fbx";
        string prefabPath = $"{CHAR_PREFAB_DIR}{fbxName}.prefab";

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        instance.name = fbxName;

        // Apply materials
        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            string rName = r.gameObject.name.ToLower();
            if (isEquipped && equipMats != null)
            {
                if (rName.Contains("helmet") && equipMats.ContainsKey("Equipment_Helmet")) r.sharedMaterial = equipMats["Equipment_Helmet"];
                else if (rName.Contains("torso") && equipMats.ContainsKey("Equipment_Torso")) r.sharedMaterial = equipMats["Equipment_Torso"];
                else if (rName.Contains("legging") && equipMats.ContainsKey("Equipment_Leggings")) r.sharedMaterial = equipMats["Equipment_Leggings"];
                else if (rName.Contains("shoe") && equipMats.ContainsKey("Equipment_Shoes")) r.sharedMaterial = equipMats["Equipment_Shoes"];
                else r.sharedMaterial = bodyMat;
            }
            else
            {
                r.sharedMaterial = bodyMat;
            }
        }

        // Add ModularEquipmentManager component
        var equipMgr = instance.AddComponent<ModularEquipmentManager>();
        equipMgr.AutoDetectSockets();

        // Add CapsuleCollider
        var capsule = instance.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0, 0.95f, 0);
        capsule.height = 1.9f;
        capsule.radius = 0.35f;

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }
}

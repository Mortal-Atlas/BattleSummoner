using System.IO;
using UnityEngine;
using UnityEditor;
using BattleSummoner.Core;
using BattleSummoner.UI;

namespace BattleSummoner.EditorScripts
{
    public static class SetupGenshinBaseCharacters
    {
        [MenuItem("BattleSummoner/Setup Genshin Base Characters")]
        public static void ExecuteSetup()
        {
            Debug.Log("[SetupGenshinBaseCharacters] Starting setup of Genshin base characters...");

            EnsureFolder("Assets/Prefabs/Characters");

            // 1. Setup Materials
            Material maleMat = SetupMaterial("Assets/Models/Characters/M_Male_Base_Genshin.mat", "Assets/Models/Characters/Textures/Character_Base_Male_Genshin_BaseColor.png");
            Material femaleMat = SetupMaterial("Assets/Models/Characters/M_Female_Base_Genshin.mat", "Assets/Models/Characters/Textures/Character_Base_Female_Genshin_BaseColor.png");

            // 2. Build Prefabs
            GameObject malePrefab = BuildCharacterPrefab(
                "Assets/Models/Characters/Character_Base_Male_Genshin_Rigged.fbx",
                "Assets/Prefabs/Characters/Character_Base_Male_Genshin.prefab",
                maleMat,
                false
            );

            GameObject femalePrefab = BuildCharacterPrefab(
                "Assets/Models/Characters/Character_Base_Female_Genshin_Rigged.fbx",
                "Assets/Prefabs/Characters/Character_Base_Female_Genshin.prefab",
                femaleMat,
                true
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SetupGenshinBaseCharacters] Successfully created Genshin Base Male and Female prefabs!");
        }

        private static Material SetupMaterial(string matPath, string texPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = Path.GetFileNameWithoutExtension(matPath);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject BuildCharacterPrefab(string fbxPath, string prefabPath, Material mat, bool isFemale)
        {
            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj == null)
            {
                Debug.LogError($"[SetupGenshinBaseCharacters] FBX not found at {fbxPath}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxObj);
            instance.name = Path.GetFileNameWithoutExtension(prefabPath);

            // Apply material to renderers
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (mat != null)
                {
                    renderer.sharedMaterial = mat;
                }
            }

            // Setup ModularEquipmentManager
            var equipMgr = instance.GetComponent<ModularEquipmentManager>();
            if (equipMgr == null) equipMgr = instance.AddComponent<ModularEquipmentManager>();

            // Setup SimsBodyMorphController
            var morphCtrl = instance.GetComponent<SimsBodyMorphController>();
            if (morphCtrl == null) morphCtrl = instance.AddComponent<SimsBodyMorphController>();

            // Wire bones and sockets
            WireMorphAndSockets(instance, morphCtrl, equipMgr);

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"[SetupGenshinBaseCharacters] Saved prefab to {prefabPath}");
            return savedPrefab;
        }

        private static void WireMorphAndSockets(GameObject root, SimsBodyMorphController morph, ModularEquipmentManager equip)
        {
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

            // Morph bones
            morph.headBone = FindTransform(allTransforms, "Head");
            morph.chestBone = FindTransform(allTransforms, "Chest");
            morph.spineBone = FindTransform(allTransforms, "Spine");
            morph.hipsBone = FindTransform(allTransforms, "Hips");
            morph.leftShoulderBone = FindTransform(allTransforms, "LeftShoulder");
            morph.rightShoulderBone = FindTransform(allTransforms, "RightShoulder");
            morph.leftArmBone = FindTransform(allTransforms, "LeftArm");
            morph.rightArmBone = FindTransform(allTransforms, "RightArm");
            morph.leftLegBone = FindTransform(allTransforms, "LeftUpLeg");
            morph.rightLegBone = FindTransform(allTransforms, "RightUpLeg");

            // Equip sockets
            equip.headSocket = FindTransform(allTransforms, "Socket_Head_Helmet") ?? morph.headBone;
            equip.chestSocket = FindTransform(allTransforms, "Socket_Chest_Armor") ?? morph.chestBone;
            equip.hipsSocket = FindTransform(allTransforms, "Socket_Legs_Armor") ?? morph.hipsBone;
            equip.leftFootSocket = FindTransform(allTransforms, "Socket_Feet_Shoes_Left") ?? morph.leftLegBone;
            equip.rightFootSocket = FindTransform(allTransforms, "Socket_Feet_Shoes_Right") ?? morph.rightLegBone;
            equip.rightHandSocket = FindTransform(allTransforms, "Socket_Weapon_RightHand") ?? FindTransform(allTransforms, "RightHand");
            equip.leftHandSocket = FindTransform(allTransforms, "Socket_Weapon_LeftHand") ?? FindTransform(allTransforms, "LeftHand");
            equip.AutoDetectSockets();
        }

        private static Transform FindTransform(Transform[] list, string name)
        {
            foreach (var t in list)
            {
                if (t.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return t;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Master NPC Population Pipeline:
    /// Replaces legacy primitive/dummy NPCs with articulated, fully rigged humanoid base models
    /// (Starter_Male_Rigged & Starter_Female_Rigged). Equips them with proper NPC role materials,
    /// stylized hair, expressive face sprites, headgear/props, human animators, and colliders.
    /// </summary>
    public static class MasterNPCPopulationPipeline
    {
        private const string FBX_MALE_PATH = "Assets/Models/Characters/Starter/Starter_Male_Rigged.fbx";
        private const string FBX_FEMALE_PATH = "Assets/Models/Characters/Starter/Starter_Female_Rigged.fbx";
        private const string HAIR_MESH_PATH = "Assets/Models/Characters/Starter/Hair01_Clean.asset";

        // Face Sprites
        private const string EYE_SPRITE_PATH = "Assets/Sprites/CharacterFaces/EyesBrows_Neutral_Open.png";
        private const string MOUTH_SPRITE_PATH = "Assets/Sprites/CharacterFaces/Mouth_Rest_Neutral.png";

        // Props & Weapons
        private const string SWORD_PREFAB_PATH = "Assets/Prefabs/Equipment/Weapon_Sword.prefab";
        private const string SHIELD_PREFAB_PATH = "Assets/Mini Simple Characters Skeleton Demo/Prefabs/Props/shield_wood.prefab";
        private const string STAFF_PREFAB_PATH = "Assets/Prefabs/Equipment/Weapon_Staff.prefab";
        private const string HAMMER_PREFAB_PATH = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Crafting Animations/Prefabs/Tools/Human_Hammer.prefab";
        private const string BOW_PREFAB_PATH = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Archer Animations/Prefabs/HumanArcher_Bow.prefab";

        // Animators
        private const string ANIM_IDLE_PATH = "Assets/MoCapCentral/MC_Sample/Animations/ctrl_Idles.controller";
        private const string ANIM_TALK_PATH = "Assets/MoCapCentral/MC_Sample/Animations/ctrl_Idle_Talk.controller";
        private const string ANIM_SPELL_PATH = "Assets/MoCapCentral/MC_Sample/Animations/ctrl_Spellbook.controller";
        private const string ANIM_GUARD_PATH = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/AnimatorControllers/Male/1H/HumanM@Combat - Sword and Shield.controller";

        private class NPCMaterials
        {
            public Material maleBase;
            public Material femaleBase;
            public Material maleCitizen;
            public Material femaleCitizen;
            public Material maleGuard;
            public Material maleMerchant;
            public Material femaleMerchant;
            public Material maleArtisan;
            public Material scholarMage;
            public Material margoWitch;
            public Material barnabySmith;
            public Material zephyrScout;

            // Hair materials
            public Material hairBlonde;
            public Material hairBrown;
            public Material hairBlack;
            public Material hairAuburn;
            public Material hairSilver;
            public Material hairViolet;
        }

        private class NPCAssets
        {
            public GameObject maleFbx;
            public GameObject femaleFbx;
            public Mesh hairMesh;
            public Sprite eyeSprite;
            public Sprite mouthSprite;
            public GameObject swordPrefab;
            public GameObject shieldPrefab;
            public GameObject staffPrefab;
            public GameObject hammerPrefab;
            public GameObject bowPrefab;
            public RuntimeAnimatorController animIdle;
            public RuntimeAnimatorController animTalk;
            public RuntimeAnimatorController animSpell;
            public RuntimeAnimatorController animGuard;
            public NPCMaterials mats;
        }

        [MenuItem("Tools/BattleSummoner/Metropolis/Populate Base Model NPCs", priority = 310)]
        public static void ExecuteNPCPopulation()
        {
            Debug.Log("<color=cyan><b>[Master NPC Pipeline] Initializing Humanoid Base Model NPC Population...</b></color>");

            NPCAssets assets = LoadNPCAssets();
            if (assets == null) return;

            // 1. Setup Materials
            assets.mats = SetupNPCMaterials();

            // 2. Remove Legacy Primitive / Placeholder NPCs
            CleanupLegacyPrimitiveNPCs();

            // 3. Update / Recreate Core Named NPC Prefabs
            UpdateCoreNamedNPCPrefabs(assets);

            // 4. Populate Lively Metropolis NPCs in Active Scene
            PopulateSceneNPCs(assets);

            // 5. Mark Scene Dirty & Report
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            Debug.Log("<color=green><b>[Master NPC Pipeline] SUCCESS! All Metropolis NPCs replaced with articulated rigged base models!</b></color>");
        }

        #region Asset Loading
        private static NPCAssets LoadNPCAssets()
        {
            NPCAssets a = new NPCAssets();
            a.maleFbx = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_MALE_PATH);
            a.femaleFbx = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_FEMALE_PATH);
            a.hairMesh = AssetDatabase.LoadAssetAtPath<Mesh>(HAIR_MESH_PATH);

            if (a.maleFbx == null || a.femaleFbx == null)
            {
                Debug.LogError($"[Master NPC Pipeline] Failed to load Starter FBX models from {FBX_MALE_PATH} or {FBX_FEMALE_PATH}!");
                return null;
            }

            // Find face sprites from hero or fallback
            var hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                var srEyes = hero.transform.Find("AvatarRigRoot/Starter_Female_Rigged_Instance/Armature/Hips/Spine/Chest/Neck/Head/FaceRig_SortingGroup/UpperFace_EyesBrows")?.GetComponent<SpriteRenderer>();
                var srMouth = hero.transform.Find("AvatarRigRoot/Starter_Female_Rigged_Instance/Armature/Hips/Spine/Chest/Neck/Head/FaceRig_SortingGroup/LowerFace_Mouth")?.GetComponent<SpriteRenderer>();
                if (srEyes != null) a.eyeSprite = srEyes.sprite;
                if (srMouth != null) a.mouthSprite = srMouth.sprite;
            }
            if (a.eyeSprite == null) a.eyeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EYE_SPRITE_PATH);
            if (a.mouthSprite == null) a.mouthSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MOUTH_SPRITE_PATH);

            // Equipment prefabs
            a.swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SWORD_PREFAB_PATH);
            a.shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SHIELD_PREFAB_PATH);
            a.staffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(STAFF_PREFAB_PATH);
            a.hammerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HAMMER_PREFAB_PATH);
            a.bowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BOW_PREFAB_PATH);

            // Animators
            a.animIdle = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIM_IDLE_PATH);
            a.animTalk = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIM_TALK_PATH);
            a.animSpell = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIM_SPELL_PATH);
            a.animGuard = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIM_GUARD_PATH);

            if (a.animTalk == null) a.animTalk = a.animIdle;
            if (a.animSpell == null) a.animSpell = a.animTalk ?? a.animIdle;
            if (a.animGuard == null) a.animGuard = a.animIdle;

            return a;
        }
        #endregion

        #region Material Setup
        private static NPCMaterials SetupNPCMaterials()
        {
            NPCMaterials mats = new NPCMaterials();

            mats.maleBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Characters/M_Starter_Male_Hero.mat");
            mats.femaleBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Characters/M_Starter_Female_Hero.mat");

            Shader painterlyShader = mats.maleBase?.shader ?? Shader.Find("Universal Render Pipeline/Lit");

            Texture2D texMale = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Characters/Starter/T_Male_Starter_Albedo.png");
            Texture2D texFemale = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Characters/Starter/T_Female_Starter_Albedo.png");

            // Role materials
            mats.maleCitizen = GetOrCreateRoleMat("M_NPC_Male_Citizen", painterlyShader, texMale, new Color(0.92f, 0.88f, 0.82f, 1f));
            mats.femaleCitizen = GetOrCreateRoleMat("M_NPC_Female_Citizen", painterlyShader, texFemale, new Color(0.95f, 0.85f, 0.90f, 1f));
            mats.maleGuard = GetOrCreateRoleMat("M_NPC_Male_Guard", painterlyShader, texMale, new Color(0.72f, 0.78f, 0.88f, 1f));
            mats.maleMerchant = GetOrCreateRoleMat("M_NPC_Male_Merchant", painterlyShader, texMale, new Color(0.95f, 0.82f, 0.50f, 1f));
            mats.femaleMerchant = GetOrCreateRoleMat("M_NPC_Female_Merchant", painterlyShader, texFemale, new Color(0.92f, 0.70f, 0.65f, 1f));
            mats.maleArtisan = GetOrCreateRoleMat("M_NPC_Male_Artisan", painterlyShader, texMale, new Color(0.80f, 0.65f, 0.55f, 1f));
            mats.scholarMage = GetOrCreateRoleMat("M_NPC_Scholar_Mage", painterlyShader, texFemale, new Color(0.65f, 0.75f, 0.95f, 1f));
            mats.margoWitch = GetOrCreateRoleMat("M_NPC_Margo_Witch", painterlyShader, texFemale, new Color(0.38f, 0.22f, 0.48f, 1f));
            mats.barnabySmith = GetOrCreateRoleMat("M_NPC_Barnaby_Smith", painterlyShader, texMale, new Color(0.75f, 0.60f, 0.50f, 1f));
            mats.zephyrScout = GetOrCreateRoleMat("M_NPC_Zephyr_Scout", painterlyShader, texMale, new Color(0.55f, 0.75f, 0.55f, 1f));

            // Hair materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            mats.hairBlonde = GetOrCreateHairMat("M_NPC_Hair_Blonde", litShader, new Color(0.92f, 0.80f, 0.45f, 1f));
            mats.hairBrown = GetOrCreateHairMat("M_NPC_Hair_Brown", litShader, new Color(0.35f, 0.23f, 0.14f, 1f));
            mats.hairBlack = GetOrCreateHairMat("M_NPC_Hair_Black", litShader, new Color(0.12f, 0.12f, 0.15f, 1f));
            mats.hairAuburn = GetOrCreateHairMat("M_NPC_Hair_Auburn", litShader, new Color(0.60f, 0.28f, 0.18f, 1f));
            mats.hairSilver = GetOrCreateHairMat("M_NPC_Hair_Silver", litShader, new Color(0.85f, 0.88f, 0.92f, 1f));
            mats.hairViolet = GetOrCreateHairMat("M_NPC_Hair_Violet", litShader, new Color(0.40f, 0.20f, 0.55f, 1f));

            AssetDatabase.SaveAssets();
            return mats;
        }

        private static Material GetOrCreateRoleMat(string name, Shader shader, Texture2D albedo, Color tint)
        {
            string path = $"Assets/Materials/Characters/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                m.name = name;
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            if (m.HasProperty("_Color")) m.color = tint;
            if (albedo != null)
            {
                if (m.HasProperty("_BC_rgb_smoothness_a")) m.SetTexture("_BC_rgb_smoothness_a", albedo);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", albedo);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", albedo);
            }
            return m;
        }

        private static Material GetOrCreateHairMat(string name, Shader shader, Color color)
        {
            string path = $"Assets/Materials/Characters/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                m.name = name;
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = color;
            m.SetFloat("_Smoothness", 0.45f);
            return m;
        }
        #endregion

        #region Cleanup Legacy Primitive NPCs
        private static void CleanupLegacyPrimitiveNPCs()
        {
            var oldNpcs = GameObject.Find("8Vector_Metropolis_Root/05_Lively_Town_NPCs");
            if (oldNpcs != null)
            {
                Undo.DestroyObjectImmediate(oldNpcs);
                Debug.Log("[Master NPC Pipeline] Destroyed legacy 05_Lively_Town_NPCs dummy shapes.");
            }

            GameObject[] streetLifeContainers = new GameObject[]
            {
                GameObject.Find("07_Detailed_Street_Scenes/03_District_Street_Life"),
                GameObject.Find("METROPOLIS_INFRASTRUCTURE/03_District_Street_Life")
            };

            foreach (var streetLife in streetLifeContainers)
            {
                if (streetLife == null) continue;
                List<GameObject> toRemove = new List<GameObject>();
                foreach (Transform child in streetLife.transform)
                {
                    string n = child.name.ToLower();
                    if (n.Contains("npc_margo") || n.Contains("npc_zephyr") || n.Contains("npc_barnaby") ||
                        n.Contains("character_bard") || n.Contains("character_beast") || n.Contains("character_running"))
                    {
                        toRemove.Add(child.gameObject);
                    }
                }
                foreach (var g in toRemove) Undo.DestroyObjectImmediate(g);
                Debug.Log($"[Master NPC Pipeline] Cleaned {toRemove.Count} primitive placeholder NPCs from {streetLife.name}.");
            }

            // Also clean any loose dummy capsule NPCs across the whole scene
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            int looseCount = 0;
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                string n = t.name.ToLower();
                if ((n.StartsWith("npc_") || n.StartsWith("character_")) && t.GetComponent<CapsuleCollider>() != null && t.GetComponent<Animator>() == null && t.parent != null && (t.parent.name.Contains("Street") || t.parent.name.Contains("Infrastructure")))
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                    looseCount++;
                }
            }
            if (looseCount > 0) Debug.Log($"[Master NPC Pipeline] Cleaned {looseCount} loose dummy capsule NPCs from scene.");

            var existingRoot = GameObject.Find("LIVELY_METROPOLIS_NPCS");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }
        }
        #endregion

        #region Prefab Creation / Update
        private static void UpdateCoreNamedNPCPrefabs(NPCAssets a)
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs/NPCs");

            // NPC_Margo.prefab
            CreateHumanoidNPCPrefab(
                "Assets/Prefabs/NPCs/NPC_Margo.prefab", "NPC_Margo",
                a.femaleFbx, a.mats.margoWitch, a.hairMesh, a.mats.hairViolet,
                a.animSpell, a.eyeSprite, a.mouthSprite,
                dataAssetPath: "Assets/Settings/NPC_Margo_Data.asset",
                isMargoWitch: true
            );

            // NPC_Barnaby.prefab
            CreateHumanoidNPCPrefab(
                "Assets/Prefabs/NPCs/NPC_Barnaby.prefab", "NPC_Barnaby",
                a.maleFbx, a.mats.barnabySmith, a.hairMesh, a.mats.hairBrown,
                a.animTalk, a.eyeSprite, a.mouthSprite,
                dataAssetPath: "Assets/Settings/NPC_Barnaby_Data.asset",
                rightHandProp: a.hammerPrefab
            );

            // NPC_Zephyr.prefab
            CreateHumanoidNPCPrefab(
                "Assets/Prefabs/NPCs/NPC_Zephyr.prefab", "NPC_Zephyr",
                a.maleFbx, a.mats.zephyrScout, a.hairMesh, a.mats.hairBlonde,
                a.animIdle, a.eyeSprite, a.mouthSprite,
                dataAssetPath: "Assets/Settings/NPC_Zephyr_Data.asset",
                leftHandProp: a.bowPrefab
            );

            // Generic Prefabs
            CreateHumanoidNPCPrefab(
                "Assets/Prefabs/NPCs/NPC_Citizen_Male.prefab", "NPC_Citizen_Male",
                a.maleFbx, a.mats.maleCitizen, a.hairMesh, a.mats.hairBrown,
                a.animIdle, a.eyeSprite, a.mouthSprite
            );

            CreateHumanoidNPCPrefab(
                "Assets/Prefabs/NPCs/NPC_Citizen_Female.prefab", "NPC_Citizen_Female",
                a.femaleFbx, a.mats.femaleCitizen, a.hairMesh, a.mats.hairBlonde,
                a.animTalk, a.eyeSprite, a.mouthSprite
            );

            CreateHumanoidNPCPrefab(
                "Assets/Prefabs/NPCs/NPC_CityGuard.prefab", "NPC_CityGuard",
                a.maleFbx, a.mats.maleGuard, a.hairMesh, a.mats.hairBlack,
                a.animGuard, a.eyeSprite, a.mouthSprite,
                rightHandProp: a.swordPrefab, leftHandProp: a.shieldPrefab
            );

            AssetDatabase.SaveAssets();
            Debug.Log("[Master NPC Pipeline] Updated all core humanoid NPC prefabs in Assets/Prefabs/NPCs/.");
        }

        private static void CreateHumanoidNPCPrefab(
            string prefabPath, string name,
            GameObject fbxModel, Material bodyMat,
            Mesh hairMesh, Material hairMat,
            RuntimeAnimatorController animCtrl,
            Sprite eyeSprite, Sprite mouthSprite,
            string dataAssetPath = null,
            bool isMargoWitch = false,
            GameObject rightHandProp = null,
            GameObject leftHandProp = null)
        {
            GameObject root = new GameObject(name);
            root.tag = "Untagged";

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.0f, 0f);
            col.height = 2.0f;
            col.radius = 0.35f;

            Animator rootAnim = root.AddComponent<Animator>();
            rootAnim.avatar = fbxModel.GetComponent<Animator>().avatar;
            rootAnim.runtimeAnimatorController = animCtrl;
            rootAnim.applyRootMotion = false;

            if (!string.IsNullOrEmpty(dataAssetPath))
            {
                BespokeNPCObject data = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>(dataAssetPath);
                if (data != null)
                {
                    NPCConversationAgent agent = root.AddComponent<NPCConversationAgent>();
                    agent.npcProfile = data;

                    CompanionAIController companion = root.AddComponent<CompanionAIController>();
                    companion.companionProfile = data;
                }
            }

            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxModel, root.transform);
            modelInst.name = "HumanoidBody";
            modelInst.transform.localPosition = new Vector3(0f, 1.077f, 0f);
            modelInst.transform.localRotation = Quaternion.identity;

            Animator childAnim = modelInst.GetComponent<Animator>();
            if (childAnim != null) UnityEngine.Object.DestroyImmediate(childAnim);

            SkinnedMeshRenderer smr = modelInst.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && bodyMat != null)
            {
                smr.sharedMaterial = bodyMat;
            }

            // Head Features (Hair + Face Sprites)
            Transform headBone = modelInst.transform.Find("Armature/Hips/Spine/Chest/Neck/Head");
            if (headBone != null)
            {
                AttachHairAndFace(headBone, hairMesh, hairMat, eyeSprite, mouthSprite);

                if (isMargoWitch)
                {
                    AttachMargoWitchHatAndGeode(modelInst, headBone);
                }
            }

            // Props
            AttachHandProps(modelInst, rightHandProp, leftHandProp);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void AttachHairAndFace(Transform headBone, Mesh hairMesh, Material hairMat, Sprite eyeSprite, Sprite mouthSprite)
        {
            if (hairMesh != null && hairMat != null)
            {
                GameObject hairGo = new GameObject("Hero_Hair", typeof(MeshFilter), typeof(MeshRenderer));
                hairGo.transform.SetParent(headBone, false);
                hairGo.transform.localPosition = new Vector3(0f, 0.13f, 0.01f);
                hairGo.transform.localRotation = Quaternion.Euler(0f, 90f, 280f);
                hairGo.transform.localScale = new Vector3(0.32f, 0.33f, 0.32f);

                hairGo.GetComponent<MeshFilter>().sharedMesh = hairMesh;
                hairGo.GetComponent<MeshRenderer>().sharedMaterial = hairMat;
            }

            if (eyeSprite != null || mouthSprite != null)
            {
                GameObject faceGroup = new GameObject("FaceRig_SortingGroup");
                faceGroup.transform.SetParent(headBone, false);
                faceGroup.transform.localPosition = new Vector3(0f, 0f, 0.09f);
                faceGroup.transform.localRotation = Quaternion.identity;

                if (eyeSprite != null)
                {
                    GameObject eyes = new GameObject("UpperFace_EyesBrows");
                    eyes.transform.SetParent(faceGroup.transform, false);
                    eyes.transform.localPosition = new Vector3(0f, 0.14f, 0.005f);
                    eyes.transform.localScale = Vector3.one * 0.12f;
                    var sr = eyes.AddComponent<SpriteRenderer>();
                    sr.sprite = eyeSprite;
                    sr.sortingOrder = 2;
                }

                if (mouthSprite != null)
                {
                    GameObject mouth = new GameObject("LowerFace_Mouth");
                    mouth.transform.SetParent(faceGroup.transform, false);
                    mouth.transform.localPosition = new Vector3(0f, 0.08f, 0.005f);
                    mouth.transform.localScale = Vector3.one * 0.06f;
                    var sr = mouth.AddComponent<SpriteRenderer>();
                    sr.sprite = mouthSprite;
                    sr.sortingOrder = 2;
                }
            }
        }

        private static void AttachMargoWitchHatAndGeode(GameObject modelInst, Transform headBone)
        {
            GameObject hatRoot = new GameObject("WitchHat");
            hatRoot.transform.SetParent(headBone, false);
            hatRoot.transform.localPosition = new Vector3(0f, 0.21f, 0.02f);
            hatRoot.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);

            GameObject brim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            brim.name = "HatBrim";
            brim.transform.SetParent(hatRoot.transform, false);
            brim.transform.localScale = new Vector3(0.50f, 0.008f, 0.50f);
            UnityEngine.Object.DestroyImmediate(brim.GetComponent<Collider>());

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "HatCrown";
            crown.transform.SetParent(hatRoot.transform, false);
            crown.transform.localPosition = new Vector3(0f, 0.08f, -0.02f);
            crown.transform.localScale = new Vector3(0.20f, 0.12f, 0.20f);
            crown.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());

            Material hatMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Witch_Hat.mat")
                           ?? new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.18f, 0.09f, 0.25f) };
            brim.GetComponent<Renderer>().sharedMaterial = hatMat;
            crown.GetComponent<Renderer>().sharedMaterial = hatMat;

            Transform rightHand = modelInst.transform.Find("Armature/Hips/Spine/Chest/RightShoulder/RightUpperArm/RightLowerArm/RightHand");
            if (rightHand != null)
            {
                GameObject geode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                geode.name = "FloatingVoidGeode";
                geode.transform.SetParent(rightHand, false);
                geode.transform.localPosition = new Vector3(0.18f, 0.12f, 0.12f);
                geode.transform.localScale = Vector3.one * 0.22f;
                UnityEngine.Object.DestroyImmediate(geode.GetComponent<Collider>());

                Material geodeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Floating_Geode.mat")
                                 ?? new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.55f, 0.12f, 0.85f) };
                geode.GetComponent<Renderer>().sharedMaterial = geodeMat;
            }
        }

        private static void AttachHandProps(GameObject modelInst, GameObject rightPropPrefab, GameObject leftPropPrefab)
        {
            if (rightPropPrefab != null)
            {
                Transform rightHand = modelInst.transform.Find("Armature/Hips/Spine/Chest/RightShoulder/RightUpperArm/RightLowerArm/RightHand");
                if (rightHand != null)
                {
                    GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(rightPropPrefab, rightHand);
                    p.name = "Equipped_Prop_Right";
                    if (p.name.ToLower().Contains("hammer") || rightPropPrefab.name.ToLower().Contains("hammer"))
                    {
                        p.transform.localPosition = new Vector3(0.04f, 0.02f, 0f);
                        p.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                        p.transform.localScale = Vector3.one * 0.45f;
                    }
                    else
                    {
                        p.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                        p.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        p.transform.localScale = Vector3.one * 0.85f;
                    }
                }
            }

            if (leftPropPrefab != null)
            {
                Transform leftHand = modelInst.transform.Find("Armature/Hips/Spine/Chest/LeftShoulder/LeftUpperArm/LeftLowerArm/LeftHand");
                if (leftHand != null)
                {
                    GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(leftPropPrefab, leftHand);
                    p.name = "Equipped_Prop_Left";
                    if (p.name.ToLower().Contains("bow") || leftPropPrefab.name.ToLower().Contains("bow"))
                    {
                        p.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                        p.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                        p.transform.localScale = Vector3.one * 0.65f;
                    }
                    else
                    {
                        p.transform.localPosition = new Vector3(0.05f, 0.02f, 0f);
                        p.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        p.transform.localScale = Vector3.one * 0.85f;
                    }
                }
            }
        }
        #endregion

        #region Scene Population
        private static void PopulateSceneNPCs(NPCAssets a)
        {
            GameObject npcsRoot = new GameObject("LIVELY_METROPOLIS_NPCS");
            Undo.RegisterCreatedObjectUndo(npcsRoot, "Populate Base Model NPCs");

            Transform storyParent = new GameObject("01_Story_And_Quest_Givers").transform;
            storyParent.SetParent(npcsRoot.transform, false);

            Transform guardsParent = new GameObject("02_City_Guards_And_Sentinels").transform;
            guardsParent.SetParent(npcsRoot.transform, false);

            Transform merchantsParent = new GameObject("03_Market_Merchants_And_Artisans").transform;
            merchantsParent.SetParent(npcsRoot.transform, false);

            Transform citizensParent = new GameObject("04_Citizens_And_Villagers").transform;
            citizensParent.SetParent(npcsRoot.transform, false);

            Transform scholarsParent = new GameObject("05_Scholars_And_Mages").transform;
            scholarsParent.SetParent(npcsRoot.transform, false);

            Transform regionalParent = new GameObject("06_Regional_Highways_And_Harbor").transform;
            regionalParent.SetParent(npcsRoot.transform, false);

            var terrainGo = GameObject.Find("Floor01_MacroContinent_Terrain");
            MeshCollider terrainCol = terrainGo?.GetComponent<MeshCollider>();

            // 1. STORY & QUEST GIVERS
            SpawnConfiguredNPC(
                storyParent, "NPC_Margo", a.femaleFbx,
                new Vector3(-115f, 58.08f, 115f), Quaternion.Euler(0f, 135f, 0f),
                a.mats.margoWitch, a.hairMesh, a.mats.hairViolet, a.animSpell,
                a.eyeSprite, a.mouthSprite, terrainCol,
                dataAssetPath: "Assets/Settings/NPC_Margo_Data.asset",
                isMargoWitch: true
            );

            SpawnConfiguredNPC(
                storyParent, "NPC_Barnaby", a.maleFbx,
                new Vector3(255f, 38.08f, 25f), Quaternion.Euler(0f, -90f, 0f),
                a.mats.barnabySmith, a.hairMesh, a.mats.hairBrown, a.animTalk,
                a.eyeSprite, a.mouthSprite, terrainCol,
                dataAssetPath: "Assets/Settings/NPC_Barnaby_Data.asset",
                rightHandProp: a.hammerPrefab
            );

            SpawnConfiguredNPC(
                storyParent, "NPC_Zephyr", a.maleFbx,
                new Vector3(15f, 22.08f, -445f), Quaternion.Euler(0f, 180f, 0f),
                a.mats.zephyrScout, a.hairMesh, a.mats.hairBlonde, a.animIdle,
                a.eyeSprite, a.mouthSprite, terrainCol,
                dataAssetPath: "Assets/Settings/NPC_Zephyr_Data.asset",
                leftHandProp: a.bowPrefab
            );

            // 2. CITY GUARDS & SENTINELS
            SpawnGuardNPC(guardsParent, "Guard_Cathedral_Left", a.maleFbx, new Vector3(-4f, 80.08f, -16f), 180f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_Cathedral_Right", a.maleFbx, new Vector3(4f, 80.08f, -16f), 180f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_Citadel_NorthStairs", a.maleFbx, new Vector3(0f, 80.08f, -75f), 180f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_Citadel_EastStairs", a.maleFbx, new Vector3(75f, 80.08f, 0f), 90f, a, terrainCol);

            // 4 Grand Continental Gates
            SpawnGuardNPC(guardsParent, "Guard_NorthGate_L", a.maleFbx, new Vector3(-7f, 22.08f, 455f), 0f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_NorthGate_R", a.maleFbx, new Vector3(7f, 22.08f, 455f), 0f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_EastGate_L", a.maleFbx, new Vector3(455f, 22.08f, 7f), 90f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_EastGate_R", a.maleFbx, new Vector3(455f, 22.08f, -7f), 90f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_SouthGate_L", a.maleFbx, new Vector3(-7f, 22.08f, -455f), 180f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_SouthGate_R", a.maleFbx, new Vector3(7f, 22.08f, -455f), 180f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_WestGate_L", a.maleFbx, new Vector3(-455f, 22.08f, -7f), 270f, a, terrainCol);
            SpawnGuardNPC(guardsParent, "Guard_WestGate_R", a.maleFbx, new Vector3(-455f, 22.08f, 7f), 270f, a, terrainCol);

            // 3. MARKET MERCHANTS & ARTISANS
            SpawnConfiguredNPC(merchantsParent, "Merchant_Baker_Helena", a.femaleFbx, new Vector3(68f, 38.08f, -245f), Quaternion.Euler(0f, 45f, 0f), a.mats.femaleMerchant, a.hairMesh, a.mats.hairBlonde, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(merchantsParent, "Artisan_Smith_Apprentice", a.maleFbx, new Vector3(260f, 38.08f, 28f), Quaternion.Euler(0f, -45f, 0f), a.mats.maleArtisan, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol, rightHandProp: a.hammerPrefab);
            SpawnConfiguredNPC(merchantsParent, "Merchant_Apothecary_Linus", a.maleFbx, new Vector3(238f, 38.08f, -72f), Quaternion.Euler(0f, 120f, 0f), a.mats.maleMerchant, a.hairMesh, a.mats.hairSilver, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(merchantsParent, "Merchant_Spices_Amira", a.femaleFbx, new Vector3(182f, 38.08f, -178f), Quaternion.Euler(0f, -60f, 0f), a.mats.femaleMerchant, a.hairMesh, a.mats.hairAuburn, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(merchantsParent, "Merchant_Weapons_Goran", a.maleFbx, new Vector3(196f, 38.08f, -168f), Quaternion.Euler(0f, -110f, 0f), a.mats.maleMerchant, a.hairMesh, a.mats.hairBrown, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol, rightHandProp: a.swordPrefab);
            SpawnConfiguredNPC(merchantsParent, "Bard_Tristan", a.maleFbx, new Vector3(175f, 38.08f, -162f), Quaternion.Euler(0f, 30f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairBlonde, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);

            // 4. CITIZENS & VILLAGERS
            SpawnCitizenPair(citizensParent, "Noble_Couple_Tier4", a.maleFbx, a.femaleFbx, a, new Vector3(0f, 80.08f, -50f), a.animTalk, a.animIdle, terrainCol);
            SpawnCitizenPair(citizensParent, "Citizen_Pair_T3_North", a.maleFbx, a.femaleFbx, a, new Vector3(110f, 58.08f, 110f), a.animTalk, a.animTalk, terrainCol);
            SpawnConfiguredNPC(citizensParent, "Citizen_Solo_T3_West", a.femaleFbx, new Vector3(-148f, 58.08f, 42f), Quaternion.Euler(0f, 80f, 0f), a.mats.femaleCitizen, a.hairMesh, a.mats.hairAuburn, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);

            SpawnCitizenPair(citizensParent, "Shoppers_T2_East", a.maleFbx, a.femaleFbx, a, new Vector3(250f, 38.08f, -30f), a.animTalk, a.animIdle, terrainCol);
            SpawnCitizenPair(citizensParent, "Shoppers_T2_South", a.maleFbx, a.femaleFbx, a, new Vector3(30f, 38.08f, -260f), a.animIdle, a.animTalk, terrainCol);
            SpawnConfiguredNPC(citizensParent, "Citizen_Bench_T2", a.maleFbx, new Vector3(180f, 38.08f, -192f), Quaternion.Euler(0f, -45f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairBrown, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);

            SpawnConfiguredNPC(citizensParent, "Tavern_Innkeeper_Barkeep", a.maleFbx, new Vector3(295f, 22.08f, -288f), Quaternion.Euler(0f, 135f, 0f), a.mats.maleMerchant, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnCitizenPair(citizensParent, "Tavern_Patrons_Table1", a.maleFbx, a.maleFbx, a, new Vector3(302f, 22.08f, -294f), a.animTalk, a.animTalk, terrainCol);
            SpawnCitizenPair(citizensParent, "Tavern_Patrons_Table2", a.femaleFbx, a.maleFbx, a, new Vector3(308f, 22.08f, -290f), a.animTalk, a.animIdle, terrainCol);

            SpawnCitizenPair(citizensParent, "Villagers_T1_North", a.femaleFbx, a.maleFbx, a, new Vector3(20f, 22.08f, 410f), a.animTalk, a.animIdle, terrainCol);
            SpawnCitizenPair(citizensParent, "Villagers_T1_East", a.maleFbx, a.femaleFbx, a, new Vector3(410f, 22.08f, 20f), a.animIdle, a.animTalk, terrainCol);
            SpawnCitizenPair(citizensParent, "Villagers_T1_South", a.maleFbx, a.femaleFbx, a, new Vector3(-20f, 22.08f, -410f), a.animTalk, a.animTalk, terrainCol);
            SpawnCitizenPair(citizensParent, "Villagers_T1_West", a.femaleFbx, a.maleFbx, a, new Vector3(-410f, 22.08f, -20f), a.animTalk, a.animIdle, terrainCol);

            // 5. SCHOLARS & MAGES
            SpawnConfiguredNPC(scholarsParent, "Mage_Apprentice_Caelen", a.maleFbx, new Vector3(-126f, 58.08f, 106f), Quaternion.Euler(0f, 45f, 0f), a.mats.scholarMage, a.hairMesh, a.mats.hairSilver, a.animSpell, a.eyeSprite, a.mouthSprite, terrainCol, rightHandProp: a.staffPrefab);
            SpawnConfiguredNPC(scholarsParent, "Mage_Apprentice_Lyra", a.femaleFbx, new Vector3(-122f, 58.08f, 108f), Quaternion.Euler(0f, 225f, 0f), a.mats.scholarMage, a.hairMesh, a.mats.hairBlonde, a.animSpell, a.eyeSprite, a.mouthSprite, terrainCol, rightHandProp: a.staffPrefab);
            SpawnConfiguredNPC(scholarsParent, "Scholar_Astrologer_Vael", a.maleFbx, new Vector3(24f, 80.08f, -14f), Quaternion.Euler(0f, -45f, 0f), a.mats.scholarMage, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);

            // 6. REGIONAL HIGHWAYS & PORT HARBOR
            SpawnConfiguredNPC(regionalParent, "Port_HarborMaster_Darius", a.maleFbx, new Vector3(952f, -13.5f, -948f), Quaternion.Euler(0f, -45f, 0f), a.mats.maleGuard, a.hairMesh, a.mats.hairSilver, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol, rightHandProp: a.swordPrefab);
            SpawnConfiguredNPC(regionalParent, "Harbor_Dockworker_Bran", a.maleFbx, new Vector3(942f, -13.8f, -938f), Quaternion.Euler(0f, 60f, 0f), a.mats.maleArtisan, a.hairMesh, a.mats.hairBrown, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(regionalParent, "Harbor_Dockworker_Kael", a.maleFbx, new Vector3(962f, -13.8f, -944f), Quaternion.Euler(0f, -120f, 0f), a.mats.maleArtisan, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);

            SpawnConfiguredNPC(regionalParent, "Fisherman_Eamonn", a.maleFbx, new Vector3(-84f, 17.6f, 706f), Quaternion.Euler(0f, -80f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairBrown, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(regionalParent, "Fisherman_Finn", a.maleFbx, new Vector3(-92f, 17.6f, 714f), Quaternion.Euler(0f, -70f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairAuburn, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);

            SpawnConfiguredNPC(regionalParent, "Caravan_Trader_Tariq", a.maleFbx, new Vector3(626f, 26.2f, -584f), Quaternion.Euler(0f, -30f, 0f), a.mats.maleMerchant, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(regionalParent, "Caravan_Guard_Nadia", a.femaleFbx, new Vector3(623f, 26.2f, -586f), Quaternion.Euler(0f, 150f, 0f), a.mats.femaleMerchant, a.hairMesh, a.mats.hairBlonde, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol, rightHandProp: a.swordPrefab);

            // 7. STREET DISTRICT PERFORMERS, ARTISANS & YOUTH (Replacing former dummy primitives)
            Transform streetPerformersParent = new GameObject("07_Street_District_Performers_And_Youth").transform;
            streetPerformersParent.SetParent(npcsRoot.transform, false);

            // Bards along the street avenues
            SpawnConfiguredNPC(streetPerformersParent, "Bard_Lyrist_Rowan", a.maleFbx, new Vector3(-112.78f, 58.08f, -105.01f), Quaternion.Euler(0f, 45f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairAuburn, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Bard_Singer_Seraphina", a.femaleFbx, new Vector3(-132.58f, 58.08f, -124.80f), Quaternion.Euler(0f, -60f, 0f), a.mats.femaleCitizen, a.hairMesh, a.mats.hairViolet, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Bard_Lutist_Gareth", a.maleFbx, new Vector3(-175.26f, 38.08f, -166.98f), Quaternion.Euler(0f, 90f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairBlonde, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Bard_Flutist_Minette", a.femaleFbx, new Vector3(-204.95f, 38.08f, -196.68f), Quaternion.Euler(0f, 135f, 0f), a.mats.femaleCitizen, a.hairMesh, a.mats.hairSilver, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);

            // Traveling Merchants & Artisans
            SpawnConfiguredNPC(streetPerformersParent, "Merchant_Potter_Ren", a.maleFbx, new Vector3(32.28f, 80.08f, 39.56f), Quaternion.Euler(0f, -45f, 0f), a.mats.maleArtisan, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Merchant_Silk_Mei", a.femaleFbx, new Vector3(42.18f, 80.08f, 49.46f), Quaternion.Euler(0f, 180f, 0f), a.mats.femaleMerchant, a.hairMesh, a.mats.hairBrown, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Trader_Caravan_Alim", a.maleFbx, new Vector3(290.30f, 22.08f, 299.14f), Quaternion.Euler(0f, 30f, 0f), a.mats.maleMerchant, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Trader_Caravan_Khadija", a.femaleFbx, new Vector3(310.10f, 22.08f, 318.94f), Quaternion.Euler(0f, -150f, 0f), a.mats.femaleMerchant, a.hairMesh, a.mats.hairAuburn, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Artisan_GemCutter_Tobias", a.maleFbx, new Vector3(64.80f, 80.08f, 5.15f), Quaternion.Euler(0f, -90f, 0f), a.mats.maleArtisan, a.hairMesh, a.mats.hairBrown, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Merchant_Herbalist_Clara", a.femaleFbx, new Vector3(128.80f, 58.08f, 5.50f), Quaternion.Euler(0f, 90f, 0f), a.mats.femaleMerchant, a.hairMesh, a.mats.hairBlonde, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Artisan_Weaver_Jonas", a.maleFbx, new Vector3(170.80f, 58.08f, 5.50f), Quaternion.Euler(0f, 0f, 0f), a.mats.maleArtisan, a.hairMesh, a.mats.hairSilver, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);

            // Youth / Running Apprentices (scaled to 0.75f)
            SpawnConfiguredNPC(streetPerformersParent, "Apprentice_Youth_Pip", a.maleFbx, new Vector3(-37.00f, 80.08f, -5.15f), Quaternion.Euler(0f, 110f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairBlonde, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol, scale: 0.75f);
            SpawnConfiguredNPC(streetPerformersParent, "Apprentice_Youth_Milo", a.maleFbx, new Vector3(-65.00f, 80.08f, -5.15f), Quaternion.Euler(0f, -70f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairBrown, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol, scale: 0.75f);
            SpawnConfiguredNPC(streetPerformersParent, "Town_Youth_Nellie", a.femaleFbx, new Vector3(-287.00f, 38.08f, -5.85f), Quaternion.Euler(0f, 40f, 0f), a.mats.femaleCitizen, a.hairMesh, a.mats.hairAuburn, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol, scale: 0.75f);

            // Plaza Townsfolk & Market Shoppers (replacing duplicate dummy capsules)
            SpawnConfiguredNPC(streetPerformersParent, "Citizen_Alchemist_Assistant", a.femaleFbx, new Vector3(123.96f, 58.08f, -116.18f), Quaternion.Euler(0f, -45f, 0f), a.mats.scholarMage, a.hairMesh, a.mats.hairViolet, a.animSpell, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Citizen_Market_Shopper_A", a.femaleFbx, new Vector3(186.43f, 38.08f, -178.16f), Quaternion.Euler(0f, 120f, 0f), a.mats.femaleCitizen, a.hairMesh, a.mats.hairBrown, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Citizen_Market_Shopper_B", a.femaleFbx, new Vector3(288.53f, 22.08f, -279.70f), Quaternion.Euler(0f, -30f, 0f), a.mats.femaleCitizen, a.hairMesh, a.mats.hairBlonde, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Citizen_Tavern_Guest", a.maleFbx, new Vector3(308.33f, 22.08f, -299.50f), Quaternion.Euler(0f, 150f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Citizen_Boulevard_Walker_1", a.maleFbx, new Vector3(5.50f, 58.08f, -127.80f), Quaternion.Euler(0f, 0f, 0f), a.mats.maleCitizen, a.hairMesh, a.mats.hairBrown, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Citizen_Boulevard_Walker_2", a.femaleFbx, new Vector3(5.50f, 58.08f, -169.80f), Quaternion.Euler(0f, 180f, 0f), a.mats.femaleCitizen, a.hairMesh, a.mats.hairAuburn, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Citizen_Plaza_Resting", a.femaleFbx, new Vector3(5.85f, 38.08f, -257.80f), Quaternion.Euler(0f, 90f, 0f), a.mats.femaleCitizen, a.hairMesh, a.mats.hairBlonde, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Citadel_Scout_Watcher_1", a.maleFbx, new Vector3(-19.20f, 80.08f, 11.91f), Quaternion.Euler(0f, 135f, 0f), a.mats.zephyrScout, a.hairMesh, a.mats.hairBlonde, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "Citadel_Scout_Watcher_2", a.maleFbx, new Vector3(-48.90f, 80.08f, 41.61f), Quaternion.Euler(0f, 60f, 0f), a.mats.zephyrScout, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "District_Ranger_1", a.maleFbx, new Vector3(-104.30f, 58.08f, 96.52f), Quaternion.Euler(0f, -120f, 0f), a.mats.zephyrScout, a.hairMesh, a.mats.hairBrown, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "District_Ranger_2", a.maleFbx, new Vector3(-124.10f, 58.08f, 116.32f), Quaternion.Euler(0f, 45f, 0f), a.mats.zephyrScout, a.hairMesh, a.mats.hairSilver, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "OuterRing_Scout_1", a.maleFbx, new Vector3(-288.68f, 22.08f, 279.84f), Quaternion.Euler(0f, -80f, 0f), a.mats.zephyrScout, a.hairMesh, a.mats.hairAuburn, a.animIdle, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(streetPerformersParent, "OuterRing_Scout_2", a.maleFbx, new Vector3(-318.37f, 22.08f, 309.54f), Quaternion.Euler(0f, 100f, 0f), a.mats.zephyrScout, a.hairMesh, a.mats.hairBlack, a.animTalk, a.eyeSprite, a.mouthSprite, terrainCol);

            Debug.Log($"[Master NPC Pipeline] Successfully populated {npcsRoot.GetComponentsInChildren<CapsuleCollider>().Length} living NPCs across the Metropolis and Region!");
        }

        private static GameObject SpawnConfiguredNPC(
            Transform parent, string name, GameObject fbxModel,
            Vector3 targetPos, Quaternion targetRot,
            Material bodyMat, Mesh hairMesh, Material hairMat,
            RuntimeAnimatorController animCtrl,
            Sprite eyeSprite, Sprite mouthSprite,
            MeshCollider terrainCol,
            string dataAssetPath = null,
            bool isMargoWitch = false,
            GameObject rightHandProp = null,
            GameObject leftHandProp = null,
            float scale = 1.0f)
        {
            if (terrainCol != null)
            {
                Ray ray = new Ray(new Vector3(targetPos.x, targetPos.y + 10f, targetPos.z), Vector3.down);
                if (terrainCol.Raycast(ray, out RaycastHit hit, 30f))
                {
                    targetPos.y = hit.point.y;
                }
            }

            // STRICT WATER EXCLUSION: Water level is Y=18.0m. Skip any NPC spawning in or below water!
            if (targetPos.y < 19.0f)
            {
                Debug.LogWarning($"[NPC] Skipping {name} as target position Y={targetPos.y:F2} is in or below water level (18.0m).");
                return null;
            }

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = targetPos;
            root.transform.rotation = targetRot;
            if (scale != 1.0f)
            {
                root.transform.localScale = Vector3.one * scale;
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.0f, 0f);
            col.height = 2.0f;
            col.radius = 0.35f;

            Animator anim = root.AddComponent<Animator>();
            anim.avatar = fbxModel.GetComponent<Animator>().avatar;
            anim.runtimeAnimatorController = animCtrl;
            anim.applyRootMotion = false;
            anim.speed = UnityEngine.Random.Range(0.94f, 1.06f);

            if (!string.IsNullOrEmpty(dataAssetPath))
            {
                BespokeNPCObject data = AssetDatabase.LoadAssetAtPath<BespokeNPCObject>(dataAssetPath);
                if (data != null)
                {
                    NPCConversationAgent agent = root.AddComponent<NPCConversationAgent>();
                    agent.npcProfile = data;

                    CompanionAIController companion = root.AddComponent<CompanionAIController>();
                    companion.companionProfile = data;
                }
            }

            GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxModel, root.transform);
            modelInst.name = "HumanoidBody";
            modelInst.transform.localPosition = new Vector3(0f, 1.077f, 0f);
            modelInst.transform.localRotation = Quaternion.identity;

            Animator childAnim = modelInst.GetComponent<Animator>();
            if (childAnim != null) UnityEngine.Object.DestroyImmediate(childAnim);

            SkinnedMeshRenderer smr = modelInst.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && bodyMat != null)
            {
                smr.sharedMaterial = bodyMat;
            }

            Transform headBone = modelInst.transform.Find("Armature/Hips/Spine/Chest/Neck/Head");
            if (headBone != null)
            {
                AttachHairAndFace(headBone, hairMesh, hairMat, eyeSprite, mouthSprite);

                if (isMargoWitch)
                {
                    AttachMargoWitchHatAndGeode(modelInst, headBone);
                }
            }

            AttachHandProps(modelInst, rightHandProp, leftHandProp);

            return root;
        }

        private static void SpawnGuardNPC(
            Transform parent, string name, GameObject maleFbx,
            Vector3 pos, float yawAngle,
            NPCAssets a, MeshCollider terrainCol)
        {
            SpawnConfiguredNPC(
                parent, name, maleFbx, pos, Quaternion.Euler(0f, yawAngle, 0f),
                a.mats.maleGuard, a.hairMesh, a.mats.hairBlack, a.animGuard,
                a.eyeSprite, a.mouthSprite, terrainCol,
                rightHandProp: a.swordPrefab, leftHandProp: a.shieldPrefab
            );
        }

        private static void SpawnCitizenPair(
            Transform parent, string pairName,
            GameObject fbx1, GameObject fbx2,
            NPCAssets a, Vector3 centerPos,
            RuntimeAnimatorController anim1, RuntimeAnimatorController anim2,
            MeshCollider terrainCol)
        {
            Vector3 p1 = centerPos + new Vector3(-0.85f, 0f, 0f);
            Vector3 p2 = centerPos + new Vector3(0.85f, 0f, 0f);

            bool isF1 = fbx1.name.Contains("Female");
            bool isF2 = fbx2.name.Contains("Female");

            Material bMat1 = isF1 ? a.mats.femaleCitizen : a.mats.maleCitizen;
            Material bMat2 = isF2 ? a.mats.femaleCitizen : a.mats.maleCitizen;
            Material hMat1 = isF1 ? a.mats.hairBlonde : a.mats.hairBrown;
            Material hMat2 = isF2 ? a.mats.hairAuburn : a.mats.hairBlack;

            SpawnConfiguredNPC(parent, $"{pairName}_A", fbx1, p1, Quaternion.Euler(0f, 75f, 0f), bMat1, a.hairMesh, hMat1, anim1, a.eyeSprite, a.mouthSprite, terrainCol);
            SpawnConfiguredNPC(parent, $"{pairName}_B", fbx2, p2, Quaternion.Euler(0f, -105f, 0f), bMat2, a.hairMesh, hMat2, anim2, a.eyeSprite, a.mouthSprite, terrainCol);
        }
        #endregion
    }
}

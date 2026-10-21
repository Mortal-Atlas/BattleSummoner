using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.UI;

namespace BattleSummoner.Editor
{
    public static class CharacterEquipmentShowcaseRefiner
    {
        [MenuItem("Tools/BattleSummoner/Refine Character Equipment Showcase")]
        public static void RefineShowcase()
        {
            const string scenePath = "Assets/Scenes/CharacterEquipmentShowcase.unity";
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            Debug.Log("[ShowcaseRefiner] Beginning overhaul of CharacterEquipmentShowcase scene...");

            // 1. Remove legacy naked dummy mannequins
            var legacyMale = GameObject.Find("Showcase_Male_Base_Naked");
            if (legacyMale != null)
            {
                Undo.DestroyObjectImmediate(legacyMale);
                Debug.Log("[ShowcaseRefiner] Removed legacy naked male model.");
            }

            var legacyFemale = GameObject.Find("Showcase_Female_Base_Naked");
            if (legacyFemale != null)
            {
                Undo.DestroyObjectImmediate(legacyFemale);
                Debug.Log("[ShowcaseRefiner] Removed legacy naked female model.");
            }

            // Also check for any existing starter instances to refresh cleanly
            var existingMale = GameObject.Find("Showcase_Character_Male_Starter");
            if (existingMale != null) Undo.DestroyObjectImmediate(existingMale);

            var existingFemale = GameObject.Find("Showcase_Character_Female_Starter");
            if (existingFemale != null) Undo.DestroyObjectImmediate(existingFemale);

            // 2. Remove any stray Equipped_sword objects at scene root
            var roots = activeScene.GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r != null && (r.name.StartsWith("Equipped_sword") || r.name.StartsWith("Weapon_")))
                {
                    string rName = r.name;
                    Undo.DestroyObjectImmediate(r);
                    Debug.Log($"[ShowcaseRefiner] Removed stray root weapon: {rName}");
                }
            }

            // 3. Load Canonical Starter Prefabs
            var malePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Starter_Male.prefab");
            var femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Starter_Female.prefab");

            if (malePrefab == null || femalePrefab == null)
            {
                Debug.LogError("[ShowcaseRefiner] Failed to find Character_Starter_Male or Character_Starter_Female prefabs!");
                return;
            }

            // 4. Instantiate and Configure Male Starter
            var maleInstance = (GameObject)PrefabUtility.InstantiatePrefab(malePrefab);
            maleInstance.name = "Showcase_Character_Male_Starter";
            maleInstance.transform.position = new Vector3(-0.46f, 0.95f, 0f);
            maleInstance.transform.rotation = Quaternion.Euler(0f, 170f, 0f);

            var maleCol = maleInstance.GetComponent<CapsuleCollider>();
            if (maleCol == null) maleCol = maleInstance.AddComponent<CapsuleCollider>();
            maleCol.center = Vector3.zero;
            maleCol.height = 1.90f;
            maleCol.radius = 0.45f;

            FitStarterCharacterHair(maleInstance, isFemale: false);
            var maleEyes = maleInstance.transform.Find("FaceOverlay_Eyes");
            if (maleEyes != null) maleEyes.localPosition = new Vector3(0f, 0.775f, 0.13f);

            var maleSockets = EnsureSockets(maleInstance, isFemale: false);
            var maleEquip = ConfigureEquipmentManager(maleInstance, maleSockets);
            var maleMorph = maleInstance.GetComponent<SimsBodyMorphController>();
            if (maleMorph == null) maleMorph = maleInstance.AddComponent<SimsBodyMorphController>();

            // Equip default hero sword
            var swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Sword.prefab");
            if (swordPrefab != null && maleEquip != null)
            {
                maleEquip.Equip(new EquipmentItemDefinition
                {
                    itemId = "sword",
                    itemName = "Hero Sword",
                    slot = EquipmentSlot.MainHandWeapon,
                    prefab = swordPrefab
                });
            }

            // 5. Instantiate and Configure Female Starter
            var femaleInstance = (GameObject)PrefabUtility.InstantiatePrefab(femalePrefab);
            femaleInstance.name = "Showcase_Character_Female_Starter";
            femaleInstance.transform.position = new Vector3(0.46f, 0.95f, 0f);
            femaleInstance.transform.rotation = Quaternion.Euler(0f, 190f, 0f);

            var femaleCol = femaleInstance.GetComponent<CapsuleCollider>();
            if (femaleCol == null) femaleCol = femaleInstance.AddComponent<CapsuleCollider>();
            femaleCol.center = Vector3.zero;
            femaleCol.height = 1.85f;
            femaleCol.radius = 0.42f;

            FitStarterCharacterHair(femaleInstance, isFemale: true);
            var femaleEyes = femaleInstance.transform.Find("FaceOverlay_Eyes");
            if (femaleEyes != null) femaleEyes.localPosition = new Vector3(0f, 0.775f, 0.10f);

            var femaleSockets = EnsureSockets(femaleInstance, isFemale: true);
            var femaleEquip = ConfigureEquipmentManager(femaleInstance, femaleSockets);
            var femaleMorph = femaleInstance.GetComponent<SimsBodyMorphController>();
            if (femaleMorph == null) femaleMorph = femaleInstance.AddComponent<SimsBodyMorphController>();

            // Equip default apprentice wand
            var wandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Wand.prefab");
            if (wandPrefab != null && femaleEquip != null)
            {
                femaleEquip.Equip(new EquipmentItemDefinition
                {
                    itemId = "wand",
                    itemName = "Apprentice Wand",
                    slot = EquipmentSlot.MainHandWeapon,
                    prefab = wandPrefab
                });
            }

            // 6. Camera & Turntable Controller
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 1.15f, -4.6f);
                mainCam.transform.rotation = Quaternion.Euler(3f, 0f, 0f);

                var turntable = mainCam.GetComponent<ShowcaseTurntableController>();
                if (turntable == null) turntable = mainCam.gameObject.AddComponent<ShowcaseTurntableController>();
                turntable.focalPoint = new Vector3(0f, 1.05f, 0f);
                turntable.distance = 4.6f;
                turntable.autoRotate = false;
                turntable.minDistance = 1.0f;
                turntable.maxDistance = 5.5f;
            }

            // 7. Update Canvas & UI bindings
            var canvasGo = GameObject.Find("CharacterCreatorCanvas");
            if (canvasGo != null)
            {
                var ui = canvasGo.GetComponent<SimsCharacterCreatorUI>();
                if (ui != null)
                {
                    ui.maleCharacterInstance = maleInstance;
                    ui.femaleCharacterInstance = femaleInstance;
                    ui.currentMorphController = maleMorph;
                    ui.currentEquipmentManager = maleEquip;
                    if (mainCam != null) ui.turntableController = mainCam.GetComponent<ShowcaseTurntableController>();

                    // Bind prefabs
                    if (ui.helmetPrefab == null) ui.helmetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Helmet.prefab");
                    if (ui.torsoPrefab == null) ui.torsoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Torso.prefab");
                    if (ui.leggingsPrefab == null) ui.leggingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Leggings.prefab");
                    if (ui.shoesPrefab == null) ui.shoesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Shoes.prefab");
                    if (ui.swordPrefab == null) ui.swordPrefab = swordPrefab;
                    if (ui.battleAxePrefab == null) ui.battleAxePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_TwoHanded_BattleAxe.prefab");
                    if (ui.staffPrefab == null) ui.staffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Staff.prefab");
                    if (ui.wandPrefab == null) ui.wandPrefab = wandPrefab;

                    ui.SetViewMode(2); // Start in dual view
                }
            }

            // 8. Lighting & Static Batching Optimization
            OptimizeSceneStageAndLighting();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[ShowcaseRefiner] Successfully refined CharacterEquipmentShowcase scene with starter models!");
        }

        private static void FitStarterCharacterHair(GameObject character, bool isFemale)
        {
            if (character == null) return;

            // 1. Hide 2D face rig to avoid sorting conflict with 3D mesh
            var faceRig = character.transform.Find("FaceRig_SortingGroup");
            if (faceRig != null) faceRig.gameObject.SetActive(false);

            // 2. Load hair mesh from ModularCharacterPolyart
            var polyPath = "Assets/RPG Tiny Hero Duo/Mesh/ModularCharacterPolyart.fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(polyPath);
            Mesh hairMesh = null;
            foreach (var a in assets)
            {
                if (a is Mesh m && m.name == "Hair06") { hairMesh = m; break; }
            }

            // 3. Hair Material
            var matPath = isFemale
                ? "Assets/Materials/Characters/M_NPC_Hair_Violet.mat"
                : "Assets/Materials/Characters/M_NPC_Hair_Silver.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            var hairTf = character.transform.Find("Hero_Hair");
            if (hairTf == null)
            {
                var hairGo = new GameObject("Hero_Hair", typeof(MeshFilter), typeof(MeshRenderer));
                hairTf = hairGo.transform;
                hairTf.SetParent(character.transform, false);
            }

            if (hairMesh != null) hairTf.GetComponent<MeshFilter>().sharedMesh = hairMesh;
            if (mat != null) hairTf.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Aligned upright snug on forehead
            hairTf.localRotation = new Quaternion(0.45451948f, -0.5416752f, 0.45451948f, -0.5416752f);
            hairTf.localPosition = new Vector3(0f, 0.785f, 0.01f);
            hairTf.localScale = new Vector3(0.46f, 0.46f, 0.46f);
        }

        private struct CharacterSockets
        {
            public Transform head;
            public Transform chest;
            public Transform hips;
            public Transform rightHand;
            public Transform leftHand;
            public Transform leftFoot;
            public Transform rightFoot;
        }

        private static CharacterSockets EnsureSockets(GameObject character, bool isFemale)
        {
            Transform GetOrCreateSocket(string name, Vector3 localPos, Vector3 localRot)
            {
                var t = character.transform.Find(name);
                if (t == null)
                {
                    var go = new GameObject(name);
                    t = go.transform;
                    t.SetParent(character.transform, false);
                }
                t.localPosition = localPos;
                t.localEulerAngles = localRot;
                return t;
            }

            float yHead = isFemale ? 0.74f : 0.76f;
            float yChest = isFemale ? 0.33f : 0.35f;

            // Outer hand orientation so weapons are held outward cleanly
            Vector3 weaponRPos = isFemale ? new Vector3(-0.38f, 0.08f, 0.08f) : new Vector3(0.40f, 0.08f, 0.08f);
            Vector3 weaponRRot = isFemale ? new Vector3(-15f, 0f, -25f) : new Vector3(-15f, 0f, 25f);
            Vector3 weaponLPos = isFemale ? new Vector3(0.38f, 0.08f, 0.08f) : new Vector3(-0.40f, 0.08f, 0.08f);
            Vector3 weaponLRot = isFemale ? new Vector3(-15f, 0f, 25f) : new Vector3(-15f, 0f, -25f);

            var sockets = new CharacterSockets
            {
                head = GetOrCreateSocket("Socket_Head", new Vector3(0f, yHead, 0.02f), Vector3.zero),
                chest = GetOrCreateSocket("Socket_Chest", new Vector3(0f, yChest, 0.02f), Vector3.zero),
                hips = GetOrCreateSocket("Socket_Hips", new Vector3(0f, -0.05f, 0.01f), Vector3.zero),
                rightHand = GetOrCreateSocket("Socket_Weapon_R", weaponRPos, weaponRRot),
                leftHand = GetOrCreateSocket("Socket_Weapon_L", weaponLPos, weaponLRot),
                leftFoot = GetOrCreateSocket("Socket_Feet_Shoes_L", new Vector3(-0.16f, -0.92f, 0.03f), Vector3.zero),
                rightFoot = GetOrCreateSocket("Socket_Feet_Shoes_R", new Vector3(0.16f, -0.92f, 0.03f), Vector3.zero)
            };

            return sockets;
        }

        private static ModularEquipmentManager ConfigureEquipmentManager(GameObject character, CharacterSockets sockets)
        {
            var equip = character.GetComponent<ModularEquipmentManager>();
            if (equip == null) equip = character.AddComponent<ModularEquipmentManager>();

            equip.headSocket = sockets.head;
            equip.chestSocket = sockets.chest;
            equip.hipsSocket = sockets.hips;
            equip.rightHandSocket = sockets.rightHand;
            equip.leftHandSocket = sockets.leftHand;
            equip.leftFootSocket = sockets.leftFoot;
            equip.rightFootSocket = sockets.rightFoot;
            equip.modularAppearance = null; // High-poly single-mesh models use socket gear

            return equip;
        }

        private static void OptimizeSceneStageAndLighting()
        {
            // Static flags on stage geometry
            void MakeStatic(string goName)
            {
                var go = GameObject.Find(goName);
                if (go != null)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, 
                        StaticEditorFlags.BatchingStatic | 
                        StaticEditorFlags.OccluderStatic | 
                        StaticEditorFlags.OccludeeStatic);
                    foreach (Transform child in go.transform)
                    {
                        GameObjectUtility.SetStaticEditorFlags(child.gameObject, 
                            StaticEditorFlags.BatchingStatic | 
                            StaticEditorFlags.OccluderStatic | 
                            StaticEditorFlags.OccludeeStatic);
                    }
                }
            }

            MakeStatic("Studio_ExhibitionStage");
            MakeStatic("Studio_FantasyArchway");
            MakeStatic("Stage_Braziers");
            MakeStatic("Dais_GlowingRuneCircle");

            // Key light: Directional with gentle soft shadows
            var sunGo = GameObject.Find("Sun_DirectionalLight");
            if (sunGo != null)
            {
                var l = sunGo.GetComponent<Light>();
                if (l != null)
                {
                    l.intensity = 1.2f;
                    l.shadows = LightShadows.Soft;
                    l.color = new Color(1.0f, 0.96f, 0.90f);
                }
            }

            // Fill light: No shadows (saves pass)
            var fillGo = GameObject.Find("Studio_FillLight");
            if (fillGo != null)
            {
                var l = fillGo.GetComponent<Light>();
                if (l != null)
                {
                    l.intensity = 0.55f;
                    l.shadows = LightShadows.None;
                    l.color = new Color(0.85f, 0.92f, 1.0f);
                }
            }

            // Rim light: No shadows
            var rimGo = GameObject.Find("Studio_RimLight");
            if (rimGo != null)
            {
                var l = rimGo.GetComponent<Light>();
                if (l != null)
                {
                    l.intensity = 0.85f;
                    l.shadows = LightShadows.None;
                    l.color = new Color(0.95f, 0.85f, 0.70f);
                }
            }

            // Dais under-light: subtle cyan glow, no shadows
            var underLightGo = GameObject.Find("Dais_SummoningUnderLight");
            if (underLightGo != null)
            {
                var l = underLightGo.GetComponent<Light>();
                if (l != null)
                {
                    l.intensity = 0.65f;
                    l.range = 3.2f;
                    l.shadows = LightShadows.None;
                    l.color = new Color(0.35f, 0.85f, 1.0f);
                }
            }

            // Braziers: Turn off expensive per-light shadow passes on small point lights
            var braziers = GameObject.Find("Stage_Braziers");
            if (braziers != null)
            {
                var brazierLights = braziers.GetComponentsInChildren<Light>();
                foreach (var bl in brazierLights)
                {
                    bl.shadows = LightShadows.None;
                    bl.range = 3.5f;
                    bl.intensity = 0.8f;
                }
            }
        }
    }
}

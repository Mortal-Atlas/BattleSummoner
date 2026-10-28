using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using BattleSummoner.Combat;
using BattleSummoner.Core;

namespace BattleSummoner.EditorScripts.Combat
{
    public static class LocalAssetAndSwordOverrideSetup
    {
        private const string VAULT_DIR = "D:/Games/BattleSummoner/Assets";
        private const string UAL2_PROJECT_PATH = "Assets/Models/QuaterniusMegaKits/UniversalAnimationLibrary2/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx";
        private const string FANTASY_PROPS_DIR = "Assets/Models/QuaterniusMegaKits/FantasyPropsMegaKit/Exports/FBX";
        private const string CONTROLLER_PATH = "Assets/Animations/PlayerBlendedSwordOverride.controller";
        private const string MASK_PATH = "Assets/Animations/PlayerUpperBodyCombatMask.mask";

        [MenuItem("BattleSummoner/Combat/1. Sync Local Assets, Blend Universal Animations & Wire World-Space Sword Override", priority = 10)]
        public static void SyncAndWireWorldSpaceSwordOverride()
        {
            Debug.Log("⚔️ [Sword Override Setup] Starting Local Asset Sync & Animation Blending Pipeline...");

            // 1. Sync & Ingest Local Assets
            SyncLocalAssetsAndRigs();

            // 2. Build Blended Controller
            AnimatorController ctrl = BuildPlayerBlendedSwordOverrideController();

            // 3. Wire into Scene & Player_Hero
            WirePlayerHeroInCurrentScene(ctrl);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("✅ [Sword Override Setup] COMPLETE! Universal Animation Library 2 blended + WorldSpaceSwordHandOverrideSolver wired with Bronze Sword & Wooden Shield!");
        }

        private static void SyncLocalAssetsAndRigs()
        {
            // A. Ensure Fantasy Props weapon scales are 100m for proper 1:1 real-world size
            string[] props = { "Sword_Bronze.fbx", "Shield_Wooden.fbx", "Axe_Bronze.fbx", "Pickaxe_Bronze.fbx" };
            foreach (var prop in props)
            {
                string p = $"{FANTASY_PROPS_DIR}/{prop}";
                if (File.Exists(p))
                {
                    var imp = AssetImporter.GetAtPath(p) as ModelImporter;
                    if (imp != null && (Mathf.Abs(imp.globalScale - 100f) > 0.01f || !imp.useFileUnits))
                    {
                        imp.globalScale = 100f;
                        imp.useFileUnits = true;
                        imp.SaveAndReimport();
                        Debug.Log($"[Sword Override Setup] Scaled weapon asset {prop} to globalScale=100");
                    }
                }
            }

            // B. Ensure Humanoid Rigs
            string[] rigs = {
                "Assets/Models/Characters/malestarter.fbx",
                "Assets/Models/Characters/malestarterriggedbodyblock.fbx",
                UAL2_PROJECT_PATH,
                "Assets/Models/QuaterniusMegaKits/UniversalAnimationLibrary2/Universal Animation Library 2[Standard]/Unity/UAL2_Standard_RM.fbx"
            };

            foreach (var rigPath in rigs)
            {
                if (File.Exists(rigPath))
                {
                    var imp = AssetImporter.GetAtPath(rigPath) as ModelImporter;
                    if (imp != null && (imp.animationType != ModelImporterAnimationType.Human || imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel))
                    {
                        imp.animationType = ModelImporterAnimationType.Human;
                        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        imp.SaveAndReimport();
                        Debug.Log($"[Sword Override Setup] Configured rig {rigPath} as Humanoid Avatar");
                    }
                }
            }
        }

        private static AnimatorController BuildPlayerBlendedSwordOverrideController()
        {
            // Gather all available AnimationClips
            var pool = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);

            IndexClipsFromFolder("Assets/Models/QuaterniusMegaKits/UniversalAnimationLibrary2", pool);
            IndexClipsFromFolder("Assets/Animations/Character", pool);
            IndexClipsFromFolder("Assets/Animations/Mixamo", pool);
            IndexClipsFromFolder("Assets/Animations/HumanoidClips", pool);

            Debug.Log($"[Sword Override Setup] Indexed {pool.Count} AnimationClips from project & Universal Animation Library 2.");

            // Create AvatarMask for Upper Body
            AvatarMask upperMask = CreateOrGetUpperBodyMask();

            // Create Controller
            if (File.Exists(CONTROLLER_PATH)) AssetDatabase.DeleteAsset(CONTROLLER_PATH);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

            // Add Parameters
            ctrl.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("SlashAngleNormalized", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsShieldBlocking", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsChargingWeaponArt", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Trig_Jump", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_Dash", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_SlashRight", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_SlashLeft", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_HeavyCleave", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_Parry", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_CastSpell", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_HitReact", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_Death", AnimatorControllerParameterType.Trigger);

            // =================================================================
            // LAYER 0: FULL-BODY BLENDED LOCOMOTION & ACTIONS
            // =================================================================
            AnimatorStateMachine baseSm = ctrl.layers[0].stateMachine;

            // Pick Best Clips
            AnimationClip idleClip  = PickBestClip(pool, "Combat_Idle", "Armature|Idle_Shield_Loop", "Player_Idle", "Idle_FoldArms_Loop", "Idle");
            AnimationClip walkClip  = PickBestClip(pool, "Armature|Walk_Carry_Loop", "Player_Walk", "Walk");
            AnimationClip runClip   = PickBestClip(pool, "Combat_Run", "Player_Run", "Armature|Sword_Dash", "Player_Sprint", "Run");
            AnimationClip jumpClip  = PickBestClip(pool, "Armature|NinjaJump_Start", "Player_Jump_Takeoff", "Humanoid_JumpStart", "Jump");
            AnimationClip dashClip  = PickBestClip(pool, "Combat_Dodge_Roll", "Armature|Shield_Dash", "Armature|Sword_Dash", "Player_DodgeRoll", "Dash");
            AnimationClip blockClip = PickBestClip(pool, "Armature|Sword_Block", "Idle_Shield_Loop", "Humanoid_Defend", "Block");
            AnimationClip parryClip = PickBestClip(pool, "Armature|Shield_OneShot", "Parry", "Block");
            AnimationClip castClip  = PickBestClip(pool, "Armature|Consume", "Player_SpellCast", "OverhandThrow", "Cast");
            AnimationClip hitClip   = PickBestClip(pool, "Armature|Hit_Knockback", "Humanoid_GetHit", "Damage", "Hit");
            AnimationClip deathClip = PickBestClip(pool, "CombatDeath01", "Humanoid_Die", "Death");

            // Locomotion BlendTree
            BlendTree locoTree;
            AnimatorState locoState = ctrl.CreateBlendTreeInController("Locomotion_BlendTree", out locoTree, 0);
            locoTree.blendType = BlendTreeType.Simple1D;
            locoTree.blendParameter = "MoveSpeed";
            if (idleClip != null) locoTree.AddChild(idleClip, 0.0f);
            if (walkClip != null) locoTree.AddChild(walkClip, 0.45f);
            if (runClip != null)  locoTree.AddChild(runClip,  1.0f);
            baseSm.defaultState = locoState;

            // Jump State
            if (jumpClip != null)
            {
                AnimatorState stJump = baseSm.AddState("Action_Jump");
                stJump.motion = jumpClip;
                var t = locoState.AddTransition(stJump);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_Jump");
                t.duration = 0.08f;
                var back = stJump.AddTransition(locoState);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                back.duration = 0.15f;
            }

            // Dash / Roll State
            if (dashClip != null)
            {
                AnimatorState stDash = baseSm.AddState("Action_Dash");
                stDash.motion = dashClip;
                var t = locoState.AddTransition(stDash);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_Dash");
                t.duration = 0.05f;
                var back = stDash.AddTransition(locoState);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                back.duration = 0.15f;
            }

            // Shield Block State
            if (blockClip != null)
            {
                AnimatorState stBlock = baseSm.AddState("Action_ShieldBlock");
                stBlock.motion = blockClip;
                var tTo = locoState.AddTransition(stBlock);
                tTo.AddCondition(AnimatorConditionMode.If, 0, "IsShieldBlocking");
                tTo.duration = 0.10f;
                var tBack = stBlock.AddTransition(locoState);
                tBack.AddCondition(AnimatorConditionMode.IfNot, 0, "IsShieldBlocking");
                tBack.duration = 0.12f;
            }

            // Parry State
            if (parryClip != null)
            {
                AnimatorState stParry = baseSm.AddState("Action_Parry");
                stParry.motion = parryClip;
                var t = locoState.AddTransition(stParry);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_Parry");
                t.duration = 0.05f;
                var back = stParry.AddTransition(locoState);
                back.hasExitTime = true;
                back.exitTime = 0.80f;
                back.duration = 0.15f;
            }

            // Spell Cast State
            if (castClip != null)
            {
                AnimatorState stCast = baseSm.AddState("Action_SpellCast");
                stCast.motion = castClip;
                var t = locoState.AddTransition(stCast);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_CastSpell");
                t.duration = 0.10f;
                var back = stCast.AddTransition(locoState);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                back.duration = 0.15f;
            }

            // Hit React
            if (hitClip != null)
            {
                AnimatorState stHit = baseSm.AddState("Action_HitReact");
                stHit.motion = hitClip;
                var t = baseSm.AddAnyStateTransition(stHit);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_HitReact");
                t.duration = 0.05f;
                var back = stHit.AddTransition(locoState);
                back.hasExitTime = true;
                back.exitTime = 0.80f;
                back.duration = 0.15f;
            }

            // Death
            if (deathClip != null)
            {
                AnimatorState stDeath = baseSm.AddState("Action_Death");
                stDeath.motion = deathClip;
                var t = baseSm.AddAnyStateTransition(stDeath);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_Death");
                t.duration = 0.10f;
            }

            // =================================================================
            // LAYER 1: UPPER-BODY COMBAT (SLASHES & OFF-HAND ACTIONS)
            // =================================================================
            AnimatorControllerLayer upperLayer = new AnimatorControllerLayer
            {
                name = "UpperBody_Combat",
                defaultWeight = 1.0f,
                avatarMask = upperMask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                iKPass = true,
                stateMachine = new AnimatorStateMachine { name = "UpperBody_Combat", hideFlags = HideFlags.HideInHierarchy }
            };
            AssetDatabase.AddObjectToAsset(upperLayer.stateMachine, ctrl);
            ctrl.AddLayer(upperLayer);

            AnimatorStateMachine upSm = upperLayer.stateMachine;
            AnimatorState stPassThrough = upSm.AddState("Upper_PassThrough");
            upSm.defaultState = stPassThrough;

            AnimationClip slash1Clip = PickBestClip(pool, "Attack_Slash_1", "Armature|Sword_Regular_A", "Attack_Slash_RightToLeft", "Sword_Regular_A", "Slash");
            AnimationClip slash2Clip = PickBestClip(pool, "Attack_Slash_2", "Armature|Sword_Regular_B", "Attack_Slash_LeftToRight", "Sword_Regular_B", "Slash");
            AnimationClip heavyClip  = PickBestClip(pool, "Attack_Spin_Heavy", "Armature|Sword_Heavy_Combo", "Attack_Slash_Overhead", "Sword_Heavy", "Heavy");

            // Slash Right Hand
            if (slash1Clip != null)
            {
                AnimatorState stSlashR = upSm.AddState("Slash_RightHand");
                stSlashR.motion = slash1Clip;
                var t = upSm.AddAnyStateTransition(stSlashR);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_SlashRight");
                t.duration = 0.05f;
                var back = stSlashR.AddTransition(stPassThrough);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                back.duration = 0.15f;
            }

            // Slash Left Hand
            if (slash2Clip != null)
            {
                AnimatorState stSlashL = upSm.AddState("Slash_LeftHand");
                stSlashL.motion = slash2Clip;
                var t = upSm.AddAnyStateTransition(stSlashL);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_SlashLeft");
                t.duration = 0.05f;
                var back = stSlashL.AddTransition(stPassThrough);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                back.duration = 0.15f;
            }

            // Heavy Cleave
            if (heavyClip != null)
            {
                AnimatorState stHeavy = upSm.AddState("Slash_HeavyCleave");
                stHeavy.motion = heavyClip;
                var t = upSm.AddAnyStateTransition(stHeavy);
                t.AddCondition(AnimatorConditionMode.If, 0, "Trig_HeavyCleave");
                t.duration = 0.06f;
                var back = stHeavy.AddTransition(stPassThrough);
                back.hasExitTime = true;
                back.exitTime = 0.88f;
                back.duration = 0.15f;
            }

            // Passive Shield Guard (Upper Body)
            if (blockClip != null)
            {
                AnimatorState stUpBlock = upSm.AddState("Upper_Shield_Guard");
                stUpBlock.motion = blockClip;
                var tTo = stPassThrough.AddTransition(stUpBlock);
                tTo.AddCondition(AnimatorConditionMode.If, 0, "IsShieldBlocking");
                tTo.duration = 0.10f;
                var tBack = stUpBlock.AddTransition(stPassThrough);
                tBack.AddCondition(AnimatorConditionMode.IfNot, 0, "IsShieldBlocking");
                tBack.duration = 0.12f;
            }

            var allLayers = ctrl.layers;
            if (allLayers.Length > 0)
            {
                allLayers[0].name = "FullBody_BlendedLocomotionAndActions";
                allLayers[0].iKPass = true;
                allLayers[0].defaultWeight = 0f; // Base layer weight is always 0 in editor data
            }
            if (allLayers.Length > 1)
            {
                allLayers[1].iKPass = true;
                allLayers[1].defaultWeight = 1.0f;
            }
            ctrl.layers = allLayers;

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private static void WirePlayerHeroInCurrentScene(AnimatorController controller)
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero == null)
            {
                Debug.LogWarning("[Sword Override Setup] Player_Hero not found in current scene!");
                return;
            }

            var pcCtrl = hero.GetComponent<PCModeDungeonTestingController>();

            // Setup character models
            string[] modelNames = { "Model_MaleStarter", "Model_FemaleStarter", "Model_ManBase", "Model_WomanBase" };
            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FANTASY_PROPS_DIR}/Sword_Bronze.fbx");
            GameObject shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FANTASY_PROPS_DIR}/Shield_Wooden.fbx");

            for (int i = 0; i < modelNames.Length; i++)
            {
                Transform modelTrans = hero.transform.Find(modelNames[i]);
                if (modelTrans == null) continue;

                Animator anim = modelTrans.GetComponent<Animator>();
                if (anim == null) anim = modelTrans.gameObject.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;

                // Sockets
                Transform handR = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
                Transform handL = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;

                Transform socketR = handR != null ? handR.Find("WeaponSocket_R") : modelTrans.Find("WeaponSocket_R");
                if (socketR == null && handR != null)
                {
                    var rObj = new GameObject("WeaponSocket_R");
                    rObj.transform.SetParent(handR, false);
                    rObj.transform.localPosition = new Vector3(0.02f, 0.08f, 0.02f);
                    rObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    socketR = rObj.transform;
                }

                Transform socketL = handL != null ? handL.Find("OffHandSocket_L") : modelTrans.Find("OffHandSocket_L");
                if (socketL == null && handL != null)
                {
                    var lObj = new GameObject("OffHandSocket_L");
                    lObj.transform.SetParent(handL, false);
                    lObj.transform.localPosition = new Vector3(-0.02f, 0.08f, 0.02f);
                    lObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    socketL = lObj.transform;
                }

                // Attach WorldSpaceSwordHandOverrideSolver
                var swordSolver = modelTrans.GetComponent<WorldSpaceSwordHandOverrideSolver>();
                if (swordSolver == null) swordSolver = modelTrans.gameObject.AddComponent<WorldSpaceSwordHandOverrideSolver>();
                swordSolver.characterAnimator = anim;
                swordSolver.rightHandWeaponSocket = socketR;
                swordSolver.swordBladeLength = 1.05f;
                swordSolver.alwaysOverrideSwordHand = true;
                swordSolver.enabled = modelTrans.gameObject.activeSelf;

                // Attach ProceduralSlashIKAndAnimatorBridge
                var bridge = modelTrans.GetComponent<ProceduralSlashIKAndAnimatorBridge>();
                if (bridge == null) bridge = modelTrans.gameObject.AddComponent<ProceduralSlashIKAndAnimatorBridge>();
                bridge.characterAnimator = anim;
                bridge.rightHandWeaponSocket = socketR;
                bridge.leftHandOffHandSocket = socketL;
                bridge.hasShieldEquipped = true;

                // Instantiate physical Sword_Bronze mesh under WeaponSocket_R
                if (socketR != null && swordPrefab != null)
                {
                    for (int c = socketR.childCount - 1; c >= 0; c--)
                    {
                        var child = socketR.GetChild(c);
                        if (!child.name.Contains("Trail")) UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                    GameObject swordInstance = UnityEngine.Object.Instantiate(swordPrefab, socketR);
                    swordInstance.name = "Equipped_MainWeapon";
                    swordInstance.transform.localPosition = Vector3.zero;
                    swordInstance.transform.localRotation = Quaternion.identity;
                    swordInstance.transform.localScale = Vector3.one;
                }

                // Instantiate physical Shield_Wooden mesh under OffHandSocket_L
                if (socketL != null && shieldPrefab != null)
                {
                    for (int c = socketL.childCount - 1; c >= 0; c--)
                    {
                        var child = socketL.GetChild(c);
                        if (!child.name.Contains("Trail")) UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                    GameObject shieldInstance = UnityEngine.Object.Instantiate(shieldPrefab, socketL);
                    shieldInstance.name = "Equipped_OffHand";
                    shieldInstance.transform.localPosition = Vector3.zero;
                    shieldInstance.transform.localRotation = Quaternion.identity;
                    shieldInstance.transform.localScale = Vector3.one;
                }

                EditorUtility.SetDirty(modelTrans.gameObject);
            }

            if (pcCtrl != null)
            {
                // Ensure Loadout 1 uses Fantasy Props Bronze Sword & Shield
                if (pcCtrl.weaponLoadouts != null && pcCtrl.weaponLoadouts.Count > 0)
                {
                    pcCtrl.weaponLoadouts[0].weaponName = "Bronze Sword & Wooden Shield";
                    pcCtrl.weaponLoadouts[0].rightWeaponPath = $"{FANTASY_PROPS_DIR}/Sword_Bronze.fbx";
                    pcCtrl.weaponLoadouts[0].leftOffHandPath = $"{FANTASY_PROPS_DIR}/Shield_Wooden.fbx";
                    pcCtrl.weaponLoadouts[0].hasShield = true;
                }
                pcCtrl.activeWeaponIndex = 1;
                EditorUtility.SetDirty(pcCtrl);
            }

            EditorUtility.SetDirty(hero);
        }

        private static void IndexClipsFromFolder(string folderPath, Dictionary<string, AnimationClip> pool)
        {
            if (!Directory.Exists(folderPath)) return;
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in assets)
                {
                    if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        if (!pool.ContainsKey(clip.name))
                            pool[clip.name] = clip;
                    }
                }
            }
        }

        private static AnimationClip PickBestClip(Dictionary<string, AnimationClip> pool, params string[] searchTokens)
        {
            foreach (var token in searchTokens)
            {
                foreach (var kvp in pool)
                {
                    if (kvp.Key.Equals(token, StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
                }
                foreach (var kvp in pool)
                {
                    if (kvp.Key.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        return kvp.Value;
                }
            }
            return null;
        }

        private static AvatarMask CreateOrGetUpperBodyMask()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MASK_PATH);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MASK_PATH);
            }

            // Enable only Upper Body parts (Head, Arms, Hands) so locomotion (Hips/Legs) keeps playing
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
                bool isUpper = part == AvatarMaskBodyPart.Head ||
                               part == AvatarMaskBodyPart.LeftArm ||
                               part == AvatarMaskBodyPart.RightArm ||
                               part == AvatarMaskBodyPart.LeftFingers ||
                               part == AvatarMaskBodyPart.RightFingers;
                mask.SetHumanoidBodyPartActive(part, isUpper);
            }

            EditorUtility.SetDirty(mask);
            return mask;
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BattleSummoner.EditorTools.Combat
{
    public static class CombatModelAndAnimationFixerEditor
    {
        private const string CHAR_DIR = "Assets/Models/Characters";
        private const string ANIM_LIB_DIR = "Assets/Models/QuaterniusMegaKits/UniversalAnimationLibrary2";
        private const string KEVIN_DIR = "Assets/Kevin Iglesias/Human Animations";
        private const string BESTIARY_DIR = "Assets/Models/QuaterniusMegaKits/BestiaryDungeonMonstersKit";
        private const string GENERATED_ANIM_DIR = "Assets/Animations/CombatGenerated";

        [MenuItem("BattleSummoner/Combat Overhaul/1. Fix All Character GLBs, Quaternius Animations, Bestiary Rigs & Weapon Sockets")]
        public static void ExecuteCompleteCombatModelAndAnimFix()
        {
            Directory.CreateDirectory(GENERATED_ANIM_DIR);

            // 1. Force Humanoid Rig on Character Models, Animations & Mixamo Body Block
            FixHumanoidModelImportersInDirectory(CHAR_DIR, isAnimationLibrary: false);
            FixHumanoidModelImportersInDirectory(ANIM_LIB_DIR, isAnimationLibrary: true);
            FixHumanoidModelImportersInDirectory(KEVIN_DIR, isAnimationLibrary: true);

            // 2. Configure Bestiary Monster Importers & Loop Settings
            FixBestiaryMonsterImporters(BESTIARY_DIR);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Index all AnimationClips across Kevin Iglesias, Universal Animation Library 2, and Character Rigs
            Dictionary<string, AnimationClip> humanClips = IndexAllAnimationClips(KEVIN_DIR, ANIM_LIB_DIR, CHAR_DIR);

            // 4. Build Upper-Body AvatarMask (Head, Arms, Hands only - NEVER Body/Hips to prevent moonwalking!)
            AvatarMask upperBodyMask = CreateOrUpdateUpperBodyAvatarMask();

            // 5. Build Master 2-Layer Player Combat Animator Controller
            AnimatorController playerController = BuildPlayerCombatAnimatorController(humanClips, upperBodyMask);

            // 6. Wire Player_Hero & All 4 Starter GLB Models in Scene with Sockets, Animator & Combat Bridge
            WireScenePlayerAndAdventurerRigs(playerController);

            // 7. Wire Bestiary Monsters in Scene/Prefabs with Animator & Seam AI
            WireBestiaryMonstersInScene();

            AssetDatabase.SaveAssets();
            Debug.Log("[CombatModelAndAnimationFixer] SUCCESS: Built smooth forward locomotion, UpperBody mask, weapon sockets, and wired Bestiary Monsters!");
        }

        private static void FixHumanoidModelImportersInDirectory(string folderPath, bool isAnimationLibrary)
        {
            if (!Directory.Exists(folderPath)) return;

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                bool changed = false;
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    changed = true;
                }

                if (path.IndexOf("malestarterriggedbodyblock", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var defClips = importer.defaultClipAnimations;
                    if (defClips != null && defClips.Length > 0)
                    {
                        for (int i = 0; i < defClips.Length; i++)
                        {
                            defClips[i].name = "Body_Block";
                            defClips[i].loopTime = true;
                            defClips[i].lockRootHeightY = true;
                            defClips[i].lockRootRotation = true;
                        }
                        importer.clipAnimations = defClips;
                        changed = true;
                    }
                }
                else if (isAnimationLibrary)
                {
                    ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                    if (clips != null && clips.Length > 0)
                    {
                        for (int i = 0; i < clips.Length; i++)
                        {
                            string low = clips[i].name.ToLowerInvariant();
                            bool shouldLoop = low.Contains("idle") || low.Contains("walk") || low.Contains("run") ||
                                              low.Contains("sprint") || low.Contains("block") || low.Contains("hold");
                            clips[i].loopTime = shouldLoop;
                            clips[i].lockRootHeightY = true;
                            clips[i].lockRootRotation = true;
                            clips[i].keepOriginalPositionY = true;
                            clips[i].keepOriginalOrientation = true;
                        }
                        importer.clipAnimations = clips;
                        changed = true;
                    }
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void FixBestiaryMonsterImporters(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                if (importer.animationType == ModelImporterAnimationType.None)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                }

                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        string low = clips[i].name.ToLowerInvariant();
                        clips[i].loopTime = low.Contains("idle") || low.Contains("walk") || low.Contains("run") || low.Contains("fly");
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }
        }

        private static Dictionary<string, AnimationClip> IndexAllAnimationClips(params string[] rootDirs)
        {
            var dict = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            foreach (string rootDir in rootDirs)
            {
                if (!Directory.Exists(rootDir)) continue;

                string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { rootDir });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var obj in assets)
                    {
                        if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                        {
                            dict[clip.name] = clip;
                        }
                    }
                }
            }
            return dict;
        }

        private static AnimationClip PickBestClip(Dictionary<string, AnimationClip> pool, params string[] keywords)
        {
            foreach (string kw in keywords)
            {
                foreach (var kvp in pool)
                {
                    if (kvp.Key.Equals("Scene", StringComparison.OrdinalIgnoreCase)) continue;
                    if (kvp.Key.Contains("A_TPose")) continue;
                    if (kvp.Key.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        return kvp.Value;
                }
            }
            foreach (var kvp in pool)
            {
                if (kvp.Key.Equals("Scene", StringComparison.OrdinalIgnoreCase)) continue;
                if (kvp.Key.Contains("A_TPose")) continue;
                return kvp.Value;
            }
            return null;
        }

        private static AvatarMask CreateOrUpdateUpperBodyAvatarMask()
        {
            string maskPath = $"{GENERATED_ANIM_DIR}/UpperBodyCombatMask.mask";
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, maskPath);
            }

            // Exclude Body (Hips/Pelvis) and Root so lower body walk/run never flips or moonwalks!
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
                bool activeOnUpper = part == AvatarMaskBodyPart.Head ||
                                     part == AvatarMaskBodyPart.LeftArm ||
                                     part == AvatarMaskBodyPart.RightArm ||
                                     part == AvatarMaskBodyPart.LeftFingers ||
                                     part == AvatarMaskBodyPart.RightFingers;
                mask.SetHumanoidBodyPartActive(part, activeOnUpper);
            }

            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimatorController BuildPlayerCombatAnimatorController(
            Dictionary<string, AnimationClip> clips, AvatarMask upperBodyMask)
        {
            string ctrlPath = $"{GENERATED_ANIM_DIR}/PlayerCombatAnimator.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

            // Parameters
            ctrl.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("SlashAngleNormalized", AnimatorControllerParameterType.Float); // -1 (Left) to +1 (Right)
            ctrl.AddParameter("LoadoutStance", AnimatorControllerParameterType.Int);          // 0=1H, 1=2H, 2=Dual, 3=Shield, 4=Mage
            ctrl.AddParameter("IsShieldBlocking", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsChargingWeaponArt", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Trig_SlashRight", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_SlashLeft", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_HeavyCleave", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_Parry", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_CastSpell", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_HitReact", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Trig_Death", AnimatorControllerParameterType.Trigger);

            // =================================================================
            // LAYER 0: FULL-BODY LOCOMOTION & FULL-BODY OVERRIDES
            // =================================================================
            AnimatorStateMachine baseSm = ctrl.layers[0].stateMachine;

            AnimationClip idleClip   = PickBestClip(clips, "HumanM@CombatIdle1H01", "CombatIdle1H01", "Idle_Shield_Loop", "Idle_No_Loop", "Idle");
            AnimationClip walkClip   = PickBestClip(clips, "HumanM@Walk01_Forward", "Walk01_Forward", "Walk_Carry_Loop", "Walk");
            AnimationClip runClip    = PickBestClip(clips, "HumanM@Run01_Forward", "Run01_Forward", "Sword_Dash", "Run");
            AnimationClip hitClip    = PickBestClip(clips, "HumanM@CombatDamage01", "CombatDamage01", "Hit_Knockback", "Hit");
            AnimationClip deathClip  = PickBestClip(clips, "HumanM@CombatDeath01", "CombatDeath01", "Death01", "Death");

            BlendTree locoTree;
            AnimatorState locoState = ctrl.CreateBlendTreeInController("Locomotion_BlendTree", out locoTree, 0);
            locoTree.blendType = BlendTreeType.Simple1D;
            locoTree.blendParameter = "MoveSpeed";
            if (idleClip != null) locoTree.AddChild(idleClip, 0.0f);
            if (walkClip != null) locoTree.AddChild(walkClip, 0.5f);
            if (runClip  != null) locoTree.AddChild(runClip,  1.0f);
            baseSm.defaultState = locoState;

            if (hitClip != null)
            {
                AnimatorState stHit = baseSm.AddState("HitReact");
                stHit.motion = hitClip;
                var tHit = baseSm.AddAnyStateTransition(stHit);
                tHit.AddCondition(AnimatorConditionMode.If, 0, "Trig_HitReact");
                tHit.duration = 0.05f;
                var tBack = stHit.AddTransition(locoState);
                tBack.hasExitTime = true;
                tBack.exitTime = 0.85f;
            }

            if (deathClip != null)
            {
                AnimatorState stDeath = baseSm.AddState("Death");
                stDeath.motion = deathClip;
                var tDeath = baseSm.AddAnyStateTransition(stDeath);
                tDeath.AddCondition(AnimatorConditionMode.If, 0, "Trig_Death");
                tDeath.duration = 0.1f;
            }

            // =================================================================
            // LAYER 1: UPPER-BODY COMBAT (SLASHES, DUAL-WIELD, SHIELD, CASTING)
            // =================================================================
            AnimatorControllerLayer upperLayer = new AnimatorControllerLayer
            {
                name = "UpperBody_Combat",
                defaultWeight = 1.0f,
                avatarMask = upperBodyMask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = new AnimatorStateMachine { name = "UpperBody_Combat", hideFlags = HideFlags.HideInHierarchy }
            };
            AssetDatabase.AddObjectToAsset(upperLayer.stateMachine, ctrl);
            ctrl.AddLayer(upperLayer);

            AnimatorStateMachine upSm = upperLayer.stateMachine;

            AnimatorState stEmpty = upSm.AddState("Upper_PassThrough");
            upSm.defaultState = stEmpty;

            AnimationClip slash1Clip  = PickBestClip(clips, "HumanM@Attack1H01_R", "Attack1H01_R", "Sword_Regular_A", "Sword");
            AnimationClip slash2Clip  = PickBestClip(clips, "HumanM@Attack1H01_L", "Attack1H01_L", "Sword_Regular_B", "Slash");
            AnimationClip heavyClip   = PickBestClip(clips, "HumanM@Attack2H01", "Attack2H01", "Sword_Heavy_Combo", "Heavy");
            AnimationClip blockClip   = PickBestClip(clips, "Body_Block", "Sword_Block", "Idle_Shield_Loop", "Block");
            AnimationClip parryClip   = PickBestClip(clips, "HumanM@AttackShield01", "AttackShield01", "Shield_OneShot", "Parry");
            AnimationClip castClip    = PickBestClip(clips, "MagicAttackCall1H01", "OverhandThrow", "Consume", "Cast");

            // Passive Shield Block State
            AnimatorState stBlock = upSm.AddState("Passive_Shield_Guard");
            stBlock.motion = blockClip != null ? blockClip : idleClip;
            var tToBlock = stEmpty.AddTransition(stBlock);
            tToBlock.AddCondition(AnimatorConditionMode.If, 0, "IsShieldBlocking");
            tToBlock.duration = 0.1f;
            var tFromBlock = stBlock.AddTransition(stEmpty);
            tFromBlock.AddCondition(AnimatorConditionMode.IfNot, 0, "IsShieldBlocking");
            tFromBlock.duration = 0.12f;

            // Right-Hand Finger 0 Slash
            AddUpperCombatActionState(upSm, stEmpty, "Slash_RightHand", slash1Clip, "Trig_SlashRight", 1.35f);
            // Left-Hand Finger 1 Dual-Wield Slash
            AddUpperCombatActionState(upSm, stEmpty, "Slash_LeftHand", slash2Clip != null ? slash2Clip : slash1Clip, "Trig_SlashLeft", 1.45f);
            // Charged Weapon Art / Heavy Cleave
            AddUpperCombatActionState(upSm, stEmpty, "Heavy_WeaponArt", heavyClip != null ? heavyClip : slash1Clip, "Trig_HeavyCleave", 1.15f);
            // Universal 2-Finger Tap Parry
            AddUpperCombatActionState(upSm, stEmpty, "TwoFinger_Parry", parryClip != null ? parryClip : blockClip, "Trig_Parry", 1.5f);
            // Circle-First Rune Cast Launch
            AddUpperCombatActionState(upSm, stEmpty, "Rune_SpellCast", castClip != null ? castClip : slash1Clip, "Trig_CastSpell", 1.25f);

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static void AddUpperCombatActionState(
            AnimatorStateMachine sm, AnimatorState returnState, string stateName,
            AnimationClip clip, string triggerParam, float playbackSpeed)
        {
            AnimatorState st = sm.AddState(stateName);
            st.motion = clip;
            st.speed = playbackSpeed;

            var tAny = sm.AddAnyStateTransition(st);
            tAny.AddCondition(AnimatorConditionMode.If, 0, triggerParam);
            tAny.duration = 0.06f;
            tAny.canTransitionToSelf = true;

            var tRet = st.AddTransition(returnState);
            tRet.hasExitTime = true;
            tRet.exitTime = 0.88f;
            tRet.duration = 0.12f;
        }

        private static void WireScenePlayerAndAdventurerRigs(AnimatorController playerController)
        {
            Animator[] allAnimators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator anim in allAnimators)
            {
                if (anim.avatar != null && anim.avatar.isHuman)
                {
                    anim.runtimeAnimatorController = playerController;
                    anim.applyRootMotion = false;

                    // Attach Weapon Sockets to RightHand & LeftHand bones
                    Transform rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                    Transform leftHand  = anim.GetBoneTransform(HumanBodyBones.LeftHand);

                    Transform socketR = EnsureSocket(rightHand, "WeaponSocket_R", new Vector3(0.02f, 0.08f, 0.02f), new Vector3(90f, 0f, 0f));
                    Transform socketL = EnsureSocket(leftHand,  "OffHandSocket_L", new Vector3(-0.02f, 0.06f, 0.02f), new Vector3(90f, 0f, 0f));

                    // If this is Player_Hero, attach the Procedural Slash IK + Combat Bridge
                    if (anim.gameObject.name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        anim.transform.root.name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var bridge = anim.gameObject.GetComponent<BattleSummoner.Combat.ProceduralSlashIKAndAnimatorBridge>();
                        if (bridge == null)
                        {
                            bridge = anim.gameObject.AddComponent<BattleSummoner.Combat.ProceduralSlashIKAndAnimatorBridge>();
                        }
                        bridge.characterAnimator = anim;
                        bridge.rightHandWeaponSocket = socketR;
                        bridge.leftHandOffHandSocket = socketL;
                    }
                }
            }
        }

        private static Transform EnsureSocket(Transform parentBone, string socketName, Vector3 localPos, Vector3 localEuler)
        {
            if (parentBone == null) return null;
            Transform existing = parentBone.Find(socketName);
            if (existing == null)
            {
                GameObject go = new GameObject(socketName);
                existing = go.transform;
                existing.SetParent(parentBone, false);
            }
            existing.localPosition = localPos;
            existing.localRotation = Quaternion.Euler(localEuler);
            return existing;
        }

        private static void WireBestiaryMonstersInScene()
        {
            var monsters = UnityEngine.Object.FindObjectsByType<BattleSummoner.Combat.BestiaryCombatAIAndSeamReceiver>(FindObjectsSortMode.None);
            foreach (var m in monsters)
            {
                if (m.GetComponent<Collider>() == null)
                {
                    CapsuleCollider cap = m.gameObject.AddComponent<CapsuleCollider>();
                    cap.center = new Vector3(0f, 1.0f, 0f);
                    cap.radius = 0.65f;
                    cap.height = 2.0f;
                }
            }
        }
    }
}
#endif

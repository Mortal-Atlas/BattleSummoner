using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using TMPro;
using BattleSummoner.Combat;
using BattleSummoner.UI;

namespace BattleSummoner.EditorScripts
{
    public static class HeroAndUIBuilder
    {
        [MenuItem("Tools/BattleSummoner/Build Rigged Hero, Animations, Emote & Carousel UI", false, 12)]
        public static void BuildAll()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("======================================================================");
            Debug.Log("🛡️ BUILDING HERO RIG, EXTENDED ANIMATIONS, EMOTE BUTTON & 3-SLOT CAROUSEL");
            Debug.Log("======================================================================");

            // 1. Setup Hero Rig and Visuals
            SetupHeroModelAndRig();

            // 2. Setup Extended Animations in PlayerHeroAnimator Controller
            SetupHeroAnimatorController();

            // 3. Setup Emote Button under AI Button
            SetupEmoteButtonInHUD();

            // 4. Setup 3-Slot Item Carousel with Gemini Specifications
            Setup3SlotItemCarouselInHUD();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("🎉 [HERO & UI ASSEMBLED] Rigged Hero, Animations, Emote Button, and 3-Slot Carousel successfully configured!");
        }

        // =========================================================================
        // 1. HERO MODEL & RIG
        // =========================================================================
        public static void SetupHeroModelAndRig()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero == null)
            {
                Debug.LogError("[Hero Setup] Player_Hero not found in scene!");
                return;
            }

            // A. Clean up duplicate Equipped_sword objects
            var swords = hero.GetComponentsInChildren<MeshRenderer>(true)
                             .Where(m => m.name == "Equipped_sword")
                             .ToList();
            if (swords.Count > 1)
            {
                for (int i = 1; i < swords.Count; i++)
                {
                    UnityEngine.Object.DestroyImmediate(swords[i].gameObject);
                }
                Debug.Log($"[Hero Setup] Cleaned up {swords.Count - 1} duplicate Equipped_sword instances.");
            }

            // B. Ensure AvatarRigRoot exists
            var avatarRoot = hero.transform.Find("AvatarRigRoot");
            if (avatarRoot == null)
            {
                var go = new GameObject("AvatarRigRoot");
                go.transform.SetParent(hero.transform, false);
                avatarRoot = go.transform;
            }
            avatarRoot.gameObject.SetActive(true);

            // C. Clean up old placeholder meshes inside AvatarRigRoot
            var oldStarter = avatarRoot.Find("Starter_Female_Rigged_Instance");
            if (oldStarter != null) UnityEngine.Object.DestroyImmediate(oldStarter.gameObject);

            // D. Instantiate newly rigged Starter_Female_Rigged.fbx
            string femaleFbxPath = "Assets/Models/Characters/Starter/Starter_Female_Rigged.fbx";
            var femaleFbx = AssetDatabase.LoadAssetAtPath<GameObject>(femaleFbxPath);
            if (femaleFbx != null)
            {
                var starterInstance = (GameObject)PrefabUtility.InstantiatePrefab(femaleFbx, avatarRoot);
                starterInstance.name = "Starter_Female_Rigged_Instance";
                starterInstance.transform.localPosition = Vector3.zero;
                starterInstance.transform.localRotation = Quaternion.identity;
                starterInstance.transform.localScale = Vector3.one;

                // Assign PBR material
                var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Characters/M_Starter_Female_Hero.mat");
                var smr = starterInstance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null && mat != null)
                {
                    smr.sharedMaterial = mat;
                }
            }

            // E. Configure Animator on AvatarRigRoot
            var animator = avatarRoot.GetComponent<Animator>() ?? avatarRoot.gameObject.AddComponent<Animator>();
            var femaleAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(femaleFbxPath);
            if (femaleAvatar != null) animator.avatar = femaleAvatar;

            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animators/PlayerHeroAnimator.controller");
            if (ctrl != null) animator.runtimeAnimatorController = ctrl;

            // F. Refresh HumanoidAnimationController
            var animCtrl = hero.GetComponent<HumanoidAnimationController>() ?? hero.AddComponent<HumanoidAnimationController>();
            animCtrl.ResolveAnimator();

            // Hide old modular placeholder armor parts
            for (int i = 0; i < avatarRoot.childCount; i++)
            {
                var child = avatarRoot.GetChild(i);
                if (child.name.StartsWith("Modular_") && child.name != "Starter_Female_Rigged_Instance")
                {
                    child.gameObject.SetActive(false);
                }
            }

            Debug.Log("🛡️ [Hero Setup] Player_Hero successfully bound to Starter_Female_Rigged FBX with Humanoid Avatar & PBR material!");
        }

        // =========================================================================
        // 2. HERO ANIMATOR CONTROLLER (Add Dance, Emotes, Brawler, Crafting, etc.)
        // =========================================================================
        public static void SetupHeroAnimatorController()
        {
            string ctrlPath = "Assets/Animators/PlayerHeroAnimator.controller";
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl == null) return;

            var sm = ctrl.layers[0].stateMachine;

            // 1. Dance Emote 01
            var dance01Clip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Social/Dance/Steps/HumanF@Dance01.fbx");
            AddOrUpdateState(sm, "Dance01", dance01Clip, "DanceAction", 1);

            // 2. Dance Emote 02
            var dance02Clip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Social/Dance/Steps/HumanF@Dance02.fbx");
            AddOrUpdateState(sm, "Dance02", dance02Clip, "DanceAction", 2);

            // 3. Dance Emote 03
            var dance03Clip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Social/Dance/Steps/HumanF@Dance03.fbx");
            AddOrUpdateState(sm, "Dance03", dance03Clip, "DanceAction", 3);

            // 4. Martial Arts Taunt
            var tauntClip = LoadClip("Assets/Martial Art Animations Sample/Animations/Taunt.fbx");
            AddOrUpdateState(sm, "MartialArt_Taunt", tauntClip, "MartialArtStrike", 1);

            // 5. Brawler Punch
            var punchClip = LoadClip("Assets/Brawler Animations Sample/Animations/Atk_P_1.fbx");
            AddOrUpdateState(sm, "Brawler_Punch", punchClip, "BrawlerStrike", 1);

            // 6. Brawler Kick
            var kickClip = LoadClip("Assets/Brawler Animations Sample/Animations/Atk_K_1.fbx");
            AddOrUpdateState(sm, "Brawler_Kick", kickClip, "BrawlerStrike", 2);

            // 7. Crafting Hammer Work
            var hammerClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Work/Hammering/HumanF@HammeringGround01_R - Loop.fbx");
            AddOrUpdateState(sm, "Crafting_Hammer", hammerClip, "CraftingAction", 1);

            // 8. Magic Attack Direct
            var magicClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx");
            if (magicClip != null)
            {
                var magicState = sm.states.FirstOrDefault(s => s.state.name == "Magic_Direct").state 
                                 ?? sm.AddState("Magic_Direct");
                magicState.motion = magicClip;
            }

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            Debug.Log("✨ [Animator Setup] Added Dance01, Dance02, Dance03, Taunt, Brawler Punch/Kick, Hammering, and Magic Attack states to PlayerHeroAnimator!");
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
            }
            return null;
        }

        private static void AddOrUpdateState(AnimatorStateMachine sm, string stateName, AnimationClip clip, string paramName, int paramVal)
        {
            if (clip == null) return;

            var existing = sm.states.FirstOrDefault(s => s.state.name == stateName);
            var state = existing.state ?? sm.AddState(stateName);
            state.motion = clip;

            // Transition from AnyState if condition is met
            bool hasTrans = sm.anyStateTransitions.Any(t => t.destinationState == state);
            if (!hasTrans)
            {
                var trans = sm.AddAnyStateTransition(state);
                trans.AddCondition(AnimatorConditionMode.Equals, paramVal, paramName);
                trans.duration = 0.12f;
                trans.hasExitTime = false;
                trans.canTransitionToSelf = false;

                // Return transition to Locomotion
                var loco = sm.states.FirstOrDefault(s => s.state.name == "Locomotion").state;
                if (loco != null)
                {
                    var returnTrans = state.AddTransition(loco);
                    returnTrans.hasExitTime = true;
                    returnTrans.exitTime = 0.90f;
                    returnTrans.duration = 0.15f;
                }
            }
        }

        // =========================================================================
        // 3. EMOTE BUTTON UNDER AI BUTTON
        // =========================================================================
        public static void SetupEmoteButtonInHUD()
        {
            var hudCanvas = GameObject.Find("PlayerHUDCanvas");
            if (hudCanvas == null) return;

            var aiBtn = GameObject.Find("AIPlayButton");
            Vector2 targetPos = new Vector2(95f, -560f); // directly under AI button (95, -425)
            Vector2 targetSize = new Vector2(110f, 110f);

            var emoteObj = GameObject.Find("EmoteButton");
            if (emoteObj == null)
            {
                emoteObj = new GameObject("EmoteButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(EmoteButtonController));
                emoteObj.transform.SetParent(hudCanvas.transform, false);
            }

            var rt = emoteObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = targetPos;
            rt.sizeDelta = targetSize;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/T_Emote_Button_DramaMasks.png");
            var img = emoteObj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
            }

            // Ensure EmoteButtonController is active and initialized
            var ctrl = emoteObj.GetComponent<EmoteButtonController>() ?? emoteObj.AddComponent<EmoteButtonController>();
            EditorUtility.SetDirty(emoteObj);

            Debug.Log($"🎭 [Emote Button Setup] Emote Drama Masks button successfully placed under AI button at {targetPos}!");
        }

        // =========================================================================
        // 4. 3-SLOT QUICK ITEM CAROUSEL
        // =========================================================================
        public static void Setup3SlotItemCarouselInHUD()
        {
            var hudCanvas = GameObject.Find("PlayerHUDCanvas");
            if (hudCanvas == null) return;

            var carouselObj = GameObject.Find("QuickItemCarousel");
            if (carouselObj == null)
            {
                carouselObj = new GameObject("QuickItemCarousel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                carouselObj.transform.SetParent(hudCanvas.transform, false);
            }

            // Position & Dimensions (width 440, height 160)
            var rt = carouselObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 110f);
            rt.sizeDelta = new Vector2(440f, 160f);

            var chassisSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/T_ItemCarousel_Chassis.png");
            var chassisImg = carouselObj.GetComponent<Image>();
            chassisImg.sprite = chassisSprite;
            chassisImg.preserveAspect = true;
            chassisImg.raycastTarget = false; // let children hitboxes intercept

            // Clean old child objects
            while (carouselObj.transform.childCount > 0)
            {
                UnityEngine.Object.DestroyImmediate(carouselObj.transform.GetChild(0).gameObject);
            }

            // A. Track Drag Zone (covering X: 0.15 to 0.85)
            var dragZone = CreateUIElement("Track_Drag_Zone", carouselObj.transform);
            var dragRt = dragZone.GetComponent<RectTransform>();
            dragRt.anchorMin = new Vector2(0.15f, 0.10f);
            dragRt.anchorMax = new Vector2(0.85f, 0.90f);
            dragRt.offsetMin = Vector2.zero;
            dragRt.offsetMax = Vector2.zero;
            var dragImg = dragZone.AddComponent<Image>();
            dragImg.color = new Color(0f, 0f, 0f, 0.005f); // invisible raycast target

            // B. Sockets Container
            var socketsContainer = CreateUIElement("Sockets_Container", carouselObj.transform);
            var scRt = socketsContainer.GetComponent<RectTransform>();
            scRt.anchorMin = Vector2.zero;
            scRt.anchorMax = Vector2.one;
            scRt.offsetMin = Vector2.zero;
            scRt.offsetMax = Vector2.zero;

            // 1. Left Lateral Node (X: 0.27, Y: 0.50), Scale 0.75, Alpha 0.4
            var leftSlot = CreateSocketNode("Slot_Left", socketsContainer.transform, new Vector2(0.27f, 0.50f), new Vector2(75f, 75f), 0.75f, 0.4f);
            // 2. Right Lateral Node (X: 0.73, Y: 0.50), Scale 0.75, Alpha 0.4
            var rightSlot = CreateSocketNode("Slot_Right", socketsContainer.transform, new Vector2(0.73f, 0.50f), new Vector2(75f, 75f), 0.75f, 0.4f);
            // 3. Center Node (X: 0.50, Y: 0.50), Scale 1.0, Alpha 1.0 (Top Layer)
            var centerSlot = CreateSocketNode("Slot_Center", socketsContainer.transform, new Vector2(0.50f, 0.50f), new Vector2(95f, 95f), 1.0f, 1.0f);
            centerSlot.transform.SetAsLastSibling(); // top layer

            // C. Center Activation Button (X: 0.35 to 0.65)
            var centerBtnObj = CreateUIElement("Center_Activation_Button", carouselObj.transform);
            var cbRt = centerBtnObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.35f, 0.15f);
            cbRt.anchorMax = new Vector2(0.65f, 0.85f);
            cbRt.offsetMin = Vector2.zero;
            cbRt.offsetMax = Vector2.zero;
            var cbImg = centerBtnObj.AddComponent<Image>();
            cbImg.color = new Color(1f, 1f, 1f, 0.005f);
            var centerBtn = centerBtnObj.AddComponent<Button>();

            // D. Navigation Chevrons
            // Left Arrow at (X: 0.07, Y: 0.50)
            var leftArrowObj = CreateUIElement("Arrow_Left", carouselObj.transform);
            var laRt = leftArrowObj.GetComponent<RectTransform>();
            laRt.anchorMin = new Vector2(0.07f, 0.50f);
            laRt.anchorMax = new Vector2(0.07f, 0.50f);
            laRt.pivot = new Vector2(0.5f, 0.5f);
            laRt.sizeDelta = new Vector2(44f, 54f);
            var laImg = leftArrowObj.AddComponent<Image>();
            laImg.color = new Color(1f, 1f, 1f, 0.005f); // covers brass chevron
            var laBtn = leftArrowObj.AddComponent<Button>();

            // Right Arrow at (X: 0.93, Y: 0.50)
            var rightArrowObj = CreateUIElement("Arrow_Right", carouselObj.transform);
            var raRt = rightArrowObj.GetComponent<RectTransform>();
            raRt.anchorMin = new Vector2(0.93f, 0.50f);
            raRt.anchorMax = new Vector2(0.93f, 0.50f);
            raRt.pivot = new Vector2(0.5f, 0.5f);
            raRt.sizeDelta = new Vector2(44f, 54f);
            var raImg = rightArrowObj.AddComponent<Image>();
            raImg.color = new Color(1f, 1f, 1f, 0.005f); // covers brass chevron
            var raBtn = rightArrowObj.AddComponent<Button>();

            // E. Center Name Label
            var nameObj = CreateUIElement("Item_Name_Label", carouselObj.transform);
            var nRt = nameObj.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0.5f, 1f);
            nRt.anchorMax = new Vector2(0.5f, 1f);
            nRt.pivot = new Vector2(0.5f, 0f);
            nRt.anchoredPosition = new Vector2(0f, 6f);
            nRt.sizeDelta = new Vector2(300f, 28f);
            var nTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nTxt.text = "Crimson Vitality Draught";
            nTxt.fontSize = 16;
            nTxt.alignment = TextAlignmentOptions.Center;
            nTxt.color = new Color(1f, 0.92f, 0.75f, 1f);

            // F. Attach and Bind QuickItemCarouselController
            var ctrl = carouselObj.GetComponent<QuickItemCarouselController>() 
                       ?? carouselObj.AddComponent<QuickItemCarouselController>();

            var serialized = new SerializedObject(ctrl);
            serialized.FindProperty("chassisImage").objectReferenceValue = chassisImg;
            serialized.FindProperty("leftSocket").objectReferenceValue = leftSlot.GetComponent<RectTransform>();
            serialized.FindProperty("centerSocket").objectReferenceValue = centerSlot.GetComponent<RectTransform>();
            serialized.FindProperty("rightSocket").objectReferenceValue = rightSlot.GetComponent<RectTransform>();

            serialized.FindProperty("leftIcon").objectReferenceValue = leftSlot.transform.Find("Icon").GetComponent<Image>();
            serialized.FindProperty("leftCanvasGroup").objectReferenceValue = leftSlot.GetComponent<CanvasGroup>();
            serialized.FindProperty("leftCountText").objectReferenceValue = leftSlot.transform.Find("Count").GetComponent<TextMeshProUGUI>();

            serialized.FindProperty("centerIcon").objectReferenceValue = centerSlot.transform.Find("Icon").GetComponent<Image>();
            serialized.FindProperty("centerCanvasGroup").objectReferenceValue = centerSlot.GetComponent<CanvasGroup>();
            serialized.FindProperty("centerCountText").objectReferenceValue = centerSlot.transform.Find("Count").GetComponent<TextMeshProUGUI>();
            serialized.FindProperty("centerNameText").objectReferenceValue = nTxt;

            serialized.FindProperty("rightIcon").objectReferenceValue = rightSlot.transform.Find("Icon").GetComponent<Image>();
            serialized.FindProperty("rightCanvasGroup").objectReferenceValue = rightSlot.GetComponent<CanvasGroup>();
            serialized.FindProperty("rightCountText").objectReferenceValue = rightSlot.transform.Find("Count").GetComponent<TextMeshProUGUI>();

            serialized.FindProperty("leftArrowButton").objectReferenceValue = laBtn;
            serialized.FindProperty("leftArrowRect").objectReferenceValue = laRt;
            serialized.FindProperty("leftArrowImage").objectReferenceValue = laImg;

            serialized.FindProperty("rightArrowButton").objectReferenceValue = raBtn;
            serialized.FindProperty("rightArrowRect").objectReferenceValue = raRt;
            serialized.FindProperty("rightArrowImage").objectReferenceValue = raImg;

            serialized.FindProperty("centerActivationButton").objectReferenceValue = centerBtn;
            serialized.FindProperty("trackDragZone").objectReferenceValue = dragRt;

            serialized.ApplyModifiedProperties();

            ctrl.InitializeDefaultItemsIfEmpty();
            ctrl.BindEventListeners();
            ctrl.RefreshDisplay(true);

            EditorUtility.SetDirty(carouselObj);
            Debug.Log("🧪 [Carousel Setup] 3-Slot Item Carousel built matching Gemini specification with Left (0.27), Center (0.50), Right (0.73), Chevrons (0.07/0.93)!");
        }

        private static GameObject CreateSocketNode(string name, Transform parent, Vector2 normPos, Vector2 size, float scale, float alpha)
        {
            var node = CreateUIElement(name, parent);
            var rt = node.GetComponent<RectTransform>();
            rt.anchorMin = normPos;
            rt.anchorMax = normPos;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * scale;

            var cg = node.AddComponent<CanvasGroup>();
            cg.alpha = alpha;

            // Icon
            var iconObj = CreateUIElement("Icon", node.transform);
            var iRt = iconObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.12f, 0.12f);
            iRt.anchorMax = new Vector2(0.88f, 0.88f);
            iRt.offsetMin = Vector2.zero;
            iRt.offsetMax = Vector2.zero;
            var iImg = iconObj.AddComponent<Image>();
            iImg.preserveAspect = true;

            // Count Label
            var countObj = CreateUIElement("Count", node.transform);
            var cRt = countObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.6f, 0.05f);
            cRt.anchorMax = new Vector2(0.95f, 0.40f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            var cTxt = countObj.AddComponent<TextMeshProUGUI>();
            cTxt.fontSize = 14;
            cTxt.fontStyle = FontStyles.Bold;
            cTxt.alignment = TextAlignmentOptions.BottomRight;
            cTxt.color = Color.white;

            return node;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}

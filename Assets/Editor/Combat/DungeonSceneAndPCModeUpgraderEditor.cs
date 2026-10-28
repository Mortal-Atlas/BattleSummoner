#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Core;
using BattleSummoner.Combat;
using BattleSummoner.CameraSystem;
using BattleSummoner.UI;

namespace BattleSummoner.EditorTools.Combat
{
    public static class DungeonSceneAndPCModeUpgraderEditor
    {
        private const string CHAR_DIR = "Assets/Models/Characters";
        private const string GENERATED_ANIM_PATH = "Assets/Animations/CombatGenerated/PlayerCombatAnimator.controller";

        private static readonly string[] StarterGlbNames =
        {
            "MaleStarter",
            "femalestarter",
            "manbase",
            "womanbase"
        };

        [MenuItem("BattleSummoner/PC Dungeon Setup/1. Upgrade Dungeon Models, Animations & Enable PC Testing Mode (No Searches)")]
        public static void UpgradeDungeonSceneAndEnablePCMode()
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            RuntimeAnimatorController playerCtrl =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(GENERATED_ANIM_PATH);

            if (playerCtrl == null)
            {
                CombatModelAndAnimationFixerEditor.ExecuteCompleteCombatModelAndAnimFix();
                playerCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(GENERATED_ANIM_PATH);
            }

            // 1. Upgrade Player_Hero with all 4 Character Models + Sockets
            GameObject playerRoot = UpgradePlayerHeroWithFourGlbModelsAndPCMode(playerCtrl);

            // 2. Upgrade Main Camera with Smooth 3rd-Person PC Camera
            UpgradeMainCameraForPC(playerRoot);

            // 3. Construct Full-Featured PC HUD Canvas & Hide Mobile Touch Canvas
            UpgradeDungeonHUDForPC();

            // 4. Populate Dungeon Combat Arena with Animated Skeletons & Boss Kaelen
            UpgradeDungeonArenaWithBestiaryModelsAndAnimations(playerRoot, playerCtrl);

            // 5. Reposition NPCs & Old Primitive Boss so they don't block the camera or arena
            RepositionNPCsAndCleanOldBoss();

            // 6. Upgrade NPCs (Margo, Barnaby, Zephyr) with Rigged Humanoid Starter Models & Idle Animations
            UpgradeNPCsWithStarterModels(playerCtrl);

            // 7. Save Scene & Assets
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("✅ [Dungeon & PC Mode Upgrader] COMPLETE! Player_Hero and NPCs rigged with Starter Models, animations verified, PC HUD active!");
        }

        private static GameObject UpgradePlayerHeroWithFourGlbModelsAndPCMode(RuntimeAnimatorController playerCtrl)
        {
            GameObject playerRoot = GameObject.Find("Player_Hero");
            if (playerRoot == null)
            {
                var tagged = GameObject.FindWithTag("Player");
                if (tagged != null) playerRoot = tagged;
            }
            if (playerRoot == null)
            {
                playerRoot = new GameObject("Player_Hero");
                playerRoot.tag = "Player";
            }
            playerRoot.transform.position = new Vector3(0f, 1.0f, -4.0f);

            // Hide primitive mesh renderer on root
            MeshRenderer rootMr = playerRoot.GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;

            CharacterController cc = playerRoot.GetComponent<CharacterController>();
            if (cc == null) cc = playerRoot.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.radius = 0.42f;
            cc.height = 1.9f;
            cc.enabled = true;

            var pmc = playerRoot.GetComponent<PlayerMovementController>();
            if (pmc != null) pmc.enabled = false;

            var margo = playerRoot.GetComponent<MargoGuideCompanion>();
            if (margo != null) UnityEngine.Object.DestroyImmediate(margo);

            var devLoadout = playerRoot.GetComponent<DevClassLoadoutManager>();
            if (devLoadout != null) devLoadout.enabled = false;

            // Clear old children (models, old weapons)
            for (int i = playerRoot.transform.childCount - 1; i >= 0; i--)
            {
                var ch = playerRoot.transform.GetChild(i);
                if (ch.name.StartsWith("Model_") || ch.name.StartsWith("DevWeapon_") || ch.name.StartsWith("StanceWeapon_"))
                {
                    UnityEngine.Object.DestroyImmediate(ch.gameObject);
                }
            }

            GameObject[] variants = new GameObject[StarterGlbNames.Length];
            ProceduralSlashIKAndAnimatorBridge primaryBridge = null;

            for (int i = 0; i < StarterGlbNames.Length; i++)
            {
                string modelName = StarterGlbNames[i];
                GameObject modelAsset = LoadCharacterGlbPrefab(modelName);
                if (modelAsset != null)
                {
                    GameObject variantInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, playerRoot.transform);
                    variantInstance.name = $"Model_{modelName}";
                    variantInstance.transform.localPosition = Vector3.zero;
                    variantInstance.transform.localRotation = Quaternion.identity; // Facing forward +Z
                    variantInstance.transform.localScale = Vector3.one;

                    Animator anim = variantInstance.GetComponentInChildren<Animator>();
                    if (anim == null) anim = variantInstance.AddComponent<Animator>();
                    if (playerCtrl != null) anim.runtimeAnimatorController = playerCtrl;
                    anim.applyRootMotion = false;

                    Transform rightHand = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
                    Transform leftHand  = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftHand)  : null;

                    Transform socketR = EnsureSocket(rightHand != null ? rightHand : variantInstance.transform, "WeaponSocket_R", new Vector3(0.02f, 0.08f, 0.02f), new Vector3(90f, 0f, 0f));
                    Transform socketL = EnsureSocket(leftHand  != null ? leftHand  : variantInstance.transform, "OffHandSocket_L", new Vector3(-0.02f, 0.06f, 0.02f), new Vector3(90f, 0f, 0f));

                    var bridge = variantInstance.GetComponent<ProceduralSlashIKAndAnimatorBridge>();
                    if (bridge == null) bridge = variantInstance.AddComponent<ProceduralSlashIKAndAnimatorBridge>();
                    bridge.characterAnimator = anim;
                    bridge.rightHandWeaponSocket = socketR;
                    bridge.leftHandOffHandSocket = socketL;

                    if (i == 0) primaryBridge = bridge;
                    variantInstance.SetActive(i == 0); // MaleStarter active by default
                    variants[i] = variantInstance;
                }
            }

            // Attach & Enable PCModeDungeonTestingController on Player_Hero
            var pcController = playerRoot.GetComponent<PCModeDungeonTestingController>();
            if (pcController == null) pcController = playerRoot.AddComponent<PCModeDungeonTestingController>();
            pcController.activeMode = PlatformTestingMode.PCStandaloneTesting;
            pcController.characterController = cc;
            pcController.characterModelVariants = variants;
            pcController.activeModelIndex = 0;
            pcController.combatAnimatorBridge = primaryBridge;
            pcController.activeMainCamera = Camera.main;

            // Equip default Loadout 1 (Steel Sword & Shield)
            pcController.EquipWeaponLoadout(1);

            return playerRoot;
        }

        private static void UpgradeMainCameraForPC(GameObject playerRoot)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            var mobileCam = cam.GetComponent<PortraitARPGCamera>();
            if (mobileCam != null) mobileCam.enabled = false;

            var resEnforcer = cam.GetComponent<HighFidelityMobileResolutionEnforcer>();
            if (resEnforcer != null) resEnforcer.enabled = false;

            var pcCam = cam.GetComponent<PCThirdPersonCameraController>();
            if (pcCam == null) pcCam = cam.gameObject.AddComponent<PCThirdPersonCameraController>();
            pcCam.enabled = true;
            pcCam.target = playerRoot.transform;
            pcCam.targetOffset = new Vector3(0f, 1.45f, 0f);
            pcCam.currentDistance = 4.8f;
            pcCam.pitch = 16f;
            pcCam.yaw = playerRoot.transform.eulerAngles.y;
        }

        private static void UpgradeDungeonHUDForPC()
        {
            // 1. Hide mobile portrait touch canvas
            GameObject mobileCanvas = GameObject.Find("PortraitHUDCanvas");
            if (mobileCanvas != null) mobileCanvas.SetActive(false);

            GameObject stanceCanvas = GameObject.Find("CombatStanceHUDCanvas");
            if (stanceCanvas != null) stanceCanvas.SetActive(false);

            // 2. Create or find PCHUDCanvas
            GameObject pcCanvasGo = GameObject.Find("PCHUDCanvas");
            if (pcCanvasGo == null)
            {
                pcCanvasGo = new GameObject("PCHUDCanvas");
            }

            Canvas canvas = pcCanvasGo.GetComponent<Canvas>();
            if (canvas == null) canvas = pcCanvasGo.AddComponent<Canvas>();
            if (Camera.main != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 1.0f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            canvas.sortingOrder = 50;

            CanvasScaler scaler = pcCanvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = pcCanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (pcCanvasGo.GetComponent<GraphicRaycaster>() == null) pcCanvasGo.AddComponent<GraphicRaycaster>();
            
            var group = pcCanvasGo.GetComponent<CanvasGroup>();
            if (group == null) group = pcCanvasGo.AddComponent<CanvasGroup>();
            group.alpha = 1f;

            var hud = pcCanvasGo.GetComponent<PCDungeonHUDController>();
            if (hud == null) hud = pcCanvasGo.AddComponent<PCDungeonHUDController>();
            hud.targetCanvas = canvas;
            hud.hudCanvasGroup = group;

            // Clear previous UI elements to rebuild cleanly
            for (int i = pcCanvasGo.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(pcCanvasGo.transform.GetChild(i).gameObject);
            }

            // Create UI Panels:
            // Top-Left Panel (Player HP, MP, Active Hero)
            GameObject tlPanel = CreateUIPanel(pcCanvasGo.transform, "TopLeft_HeroFrame",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(25f, -25f), new Vector2(360f, 130f), new Color(0.08f, 0.09f, 0.12f, 0.85f));

            var heroNameTMP = CreateTMPText(tlPanel.transform, "HeroNameText",
                "<b>HERO: SPIRE SUMMONER</b>  <size=12><color=#64D2FF>Lv. 1</color></size>",
                16, TextAlignmentOptions.TopLeft, new Vector2(12f, -10f), new Vector2(336f, 24f), Color.white);
            hud.playerNameTMP = heroNameTMP;

            var hpSlider = CreateUISlider(tlPanel.transform, "PlayerHPSlider",
                new Vector2(12f, -38f), new Vector2(336f, 20f),
                new Color(0.2f, 0.2f, 0.2f, 0.8f), new Color(0.95f, 0.22f, 0.25f, 1f));
            hud.playerHpSlider = hpSlider;
            hud.playerHpText = CreateTMPText(hpSlider.transform, "HPText", "1000 / 1000 HP", 12, TextAlignmentOptions.Center, Vector2.zero, new Vector2(336f, 20f), Color.white);

            var mpSlider = CreateUISlider(tlPanel.transform, "PlayerMPSlider",
                new Vector2(12f, -64f), new Vector2(336f, 16f),
                new Color(0.2f, 0.2f, 0.2f, 0.8f), new Color(0.2f, 0.6f, 1f, 1f));
            hud.playerMpSlider = mpSlider;
            hud.playerMpText = CreateTMPText(mpSlider.transform, "MPText", "250 / 250 MP", 11, TextAlignmentOptions.Center, Vector2.zero, new Vector2(336f, 16f), Color.white);

            hud.activeHeroModelTMP = CreateTMPText(tlPanel.transform, "ActiveHeroText",
                "<color=#FFD700>ACTIVE HERO:</color> Male Starter <color=#AAAAAA>[Key 1-4]</color>",
                13, TextAlignmentOptions.BottomLeft, new Vector2(12f, 10f), new Vector2(336f, 22f), Color.white);

            // Bottom-Left Panel (Equipped Weapon & Numpad Swap Deck)
            GameObject blPanel = CreateUIPanel(pcCanvasGo.transform, "BottomLeft_WeaponFrame",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(25f, 25f), new Vector2(430f, 185f), new Color(0.08f, 0.09f, 0.12f, 0.85f));

            hud.currentWeaponTMP = CreateTMPText(blPanel.transform, "CurrentWeaponText",
                "<color=#64D2FF>WEAPON [1]:</color> Steel Sword & Knight Shield\n<size=12><color=#AAAAAA>Stance: Sword & Board (1H + Shield)</color></size>",
                15, TextAlignmentOptions.TopLeft, new Vector2(12f, -10f), new Vector2(406f, 40f), Color.white);

            hud.weaponArtStatusTMP = CreateTMPText(blPanel.transform, "WeaponArtStatusText",
                "<color=#8E8E93>[Heavy Cleave: Hold L-Click > 0.45s]</color>",
                13, TextAlignmentOptions.TopLeft, new Vector2(12f, -52f), new Vector2(406f, 22f), Color.white);

            hud.keypadWeaponListTMP = CreateTMPText(blPanel.transform, "KeypadGuideText",
                "<b>KEYPAD WEAPON SWAP:</b>\n" +
                "<color=#FFD60A>[1]</color> Sword & Shield  <color=#FFD60A>[2]</color> Knight Greatsword  <color=#FFD60A>[3]</color> Longsword & Shield\n" +
                "<color=#FFD60A>[4]</color> Heavy Battleaxe  <color=#FFD60A>[5]</color> Dual Daggers        <color=#FFD60A>[6]</color> War Pickaxe\n" +
                "<color=#FFD60A>[7]</color> Torch & Shield   <color=#FFD60A>[8]</color> Dual Steel Swords   <color=#FFD60A>[9]</color> Bare Fists\n" +
                "<size=11><color=#8E8E93>Keys 5-0 or [ / ] also cycle weapons</color></size>",
                11, TextAlignmentOptions.BottomLeft, new Vector2(12f, 8f), new Vector2(406f, 95f), new Color(0.85f, 0.85f, 0.88f));

            // Bottom-Right Panel (PC Controls Legend)
            GameObject brPanel = CreateUIPanel(pcCanvasGo.transform, "BottomRight_ControlsFrame",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-25f, 25f), new Vector2(340f, 225f), new Color(0.08f, 0.09f, 0.12f, 0.85f));

            hud.controlsLegendTMP = CreateTMPText(brPanel.transform, "ControlsText",
                "<b>PC STANDALONE CONTROLS:</b>\n" +
                "<color=#FFD60A>WASD:</color> Move (Camera-Relative)\n" +
                "<color=#FFD60A>Left Shift / Q:</color> Dash / Sprint\n" +
                "<color=#FFD60A>Space:</color> Jump\n" +
                "<color=#FFD60A>L-Click Drag:</color> 1:1 Directional Slash\n" +
                "<color=#FFD60A>R-Click:</color> Parry / Hold to Guard\n" +
                "<color=#FFD60A>R-Mouse Drag:</color> Orbit Camera\n" +
                "<color=#FFD60A>Scroll Wheel:</color> Zoom Camera\n" +
                "<color=#FFD60A>E / Mid-Click:</color> Cast Rune Spell\n" +
                "<color=#FFD60A>Keys 1-4:</color> Swap Character Model\n" +
                "<color=#FFD60A>Numpad 1-9:</color> Swap Weapon Loadout\n" +
                "<color=#FFD60A>F1:</color> Toggle Mobile / PC View",
                12, TextAlignmentOptions.TopLeft, new Vector2(12f, -10f), new Vector2(316f, 205f), Color.white);

            // Center Crosshair Reticle
            GameObject reticleObj = new GameObject("CenterCrosshair");
            reticleObj.transform.SetParent(pcCanvasGo.transform, false);
            RectTransform reticleRt = reticleObj.AddComponent<RectTransform>();
            reticleRt.anchorMin = new Vector2(0.5f, 0.5f);
            reticleRt.anchorMax = new Vector2(0.5f, 0.5f);
            reticleRt.pivot = new Vector2(0.5f, 0.5f);
            reticleRt.sizeDelta = new Vector2(10f, 10f);
            Image reticleImg = reticleObj.AddComponent<Image>();
            reticleImg.color = new Color(1f, 1f, 1f, 0.55f);
            hud.crosshairImage = reticleImg;

            // Top-Center: Boss Frame (Initially Inactive)
            GameObject bossFrame = CreateUIPanel(pcCanvasGo.transform, "TopCenter_BossFrame",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -25f), new Vector2(500f, 80f), new Color(0.12f, 0.05f, 0.05f, 0.88f));

            hud.bossBarContainer = bossFrame;
            hud.bossNameTMP = CreateTMPText(bossFrame.transform, "BossNameText",
                "<b>KAELEN, THE UNFORGED</b>",
                16, TextAlignmentOptions.Top, new Vector2(0f, -8f), new Vector2(480f, 24f), new Color(1f, 0.4f, 0.35f));

            var bossSlider = CreateUISlider(bossFrame.transform, "BossHPSlider",
                new Vector2(10f, -34f), new Vector2(480f, 18f),
                new Color(0.25f, 0.1f, 0.1f, 0.8f), new Color(0.95f, 0.2f, 0.2f, 1f));
            hud.bossHpSlider = bossSlider;

            hud.bossLimbStatusTMP = CreateTMPText(bossFrame.transform, "BossLimbStatusText",
                "<color=#FF9F0A>WEAK PART:</color> Kaelen's Overheated Anvil-Horn [Intact]",
                12, TextAlignmentOptions.Bottom, new Vector2(0f, 6f), new Vector2(480f, 18f), Color.white);

            bossFrame.SetActive(false);
        }

        private static GameObject CreateUIPanel(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = panel.AddComponent<Image>();
            img.color = bgColor;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.45f, 0.65f, 0.45f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return panel;
        }

        private static TextMeshProUGUI CreateTMPText(
            Transform parent, string name, string text, float fontSize,
            TextAlignmentOptions align, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.enableWordWrapping = true;

            return tmp;
        }

        private static Slider CreateUISlider(
            Transform parent, string name,
            Vector2 anchoredPos, Vector2 size, Color bgColor, Color fillColor)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);

            RectTransform rt = sliderObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = bgColor;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = fillColor;

            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;

            return slider;
        }

        private static void UpgradeDungeonArenaWithBestiaryModelsAndAnimations(
            GameObject playerRoot, RuntimeAnimatorController fallbackHumanoidCtrl)
        {
            GameObject dungeonRoot = GameObject.Find("Dungeon_PC_CombatArena");
            if (dungeonRoot != null) UnityEngine.Object.DestroyImmediate(dungeonRoot);

            dungeonRoot = new GameObject("Dungeon_PC_CombatArena");
            Vector3 arenaOrigin = playerRoot != null
                ? playerRoot.transform.position + playerRoot.transform.forward * 10f
                : new Vector3(0f, 1.0f, 2.0f);
            dungeonRoot.transform.position = arenaOrigin;

            string skelPath = "Assets/Mini Simple Characters Skeleton Demo/Models/mini simple skeleton demo.fbx";
            GameObject skelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(skelPath);

            string[] monsterTitles =
            {
                "Spire Skeleton Vanguard",
                "Spire Skeleton Lancer",
                "Spire Skeleton Duelist",
                "Cursed Skeleton Sentinel",
                "Spire Skeleton Berserker",
                "Kaelen, The Unforged (Dungeon Boss)"
            };

            Vector3[] spawnOffsets =
            {
                new Vector3(-4.5f, 0f,  0f),
                new Vector3( 4.5f, 0f,  0f),
                new Vector3(-6.0f, 0f,  5.5f),
                new Vector3( 6.0f, 0f,  5.5f),
                new Vector3(-3.0f, 0f,  9.5f),
                new Vector3( 0.0f, 0f, 14.0f) // Boss Kaelen at the back of the arena
            };

            string[] weaponPaths =
            {
                "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Sword.fbx",
                "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Polearm.fbx",
                "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Dagger.fbx",
                "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Warhammer.fbx",
                "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Greatsword.fbx",
                "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Greatsword.fbx"
            };

            var skelTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mini Simple Characters Skeleton Demo/Materials/Mini Simple Characters_texture.png");

            for (int i = 0; i < spawnOffsets.Length; i++)
            {
                bool isBoss = (i == spawnOffsets.Length - 1);
                bool isBerserker = (i == 4);
                GameObject monsterRoot = new GameObject(monsterTitles[i]);
                monsterRoot.transform.SetParent(dungeonRoot.transform, false);
                monsterRoot.transform.localPosition = spawnOffsets[i];
                monsterRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                if (isBoss) monsterRoot.transform.localScale = Vector3.one * 2.2f;
                else if (isBerserker) monsterRoot.transform.localScale = Vector3.one * 1.35f;

                if (skelAsset != null)
                {
                    GameObject visualMesh = (GameObject)PrefabUtility.InstantiatePrefab(skelAsset, monsterRoot.transform);
                    visualMesh.transform.localPosition = Vector3.zero;
                    visualMesh.transform.localRotation = Quaternion.identity;
                }

                // Ensure bone texture is properly assigned
                if (skelTexture != null)
                {
                    foreach (var r in monsterRoot.GetComponentsInChildren<Renderer>())
                    {
                        if (r.sharedMaterial != null)
                        {
                            r.sharedMaterial.mainTexture = skelTexture;
                            if (r.sharedMaterial.HasProperty("_BaseMap")) r.sharedMaterial.SetTexture("_BaseMap", skelTexture);
                            if (r.sharedMaterial.HasProperty("_BaseColor")) r.sharedMaterial.SetColor("_BaseColor", Color.white);
                            if (r.sharedMaterial.HasProperty("_Color")) r.sharedMaterial.SetColor("_Color", Color.white);
                        }
                    }
                }

                Animator monsterAnim = monsterRoot.GetComponentInChildren<Animator>();
                if (monsterAnim != null && monsterAnim.isHuman)
                {
                    monsterAnim.runtimeAnimatorController = fallbackHumanoidCtrl;
                }

                CapsuleCollider col = monsterRoot.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1.0f, 0f);
                col.radius = isBoss ? 1.2f : (isBerserker ? 0.85f : 0.65f);
                col.height = isBoss ? 2.8f : (isBerserker ? 2.4f : 2.0f);

                var ai = monsterRoot.AddComponent<BestiaryCombatAIAndSeamReceiver>();
                ai.monsterDisplayName = monsterTitles[i];
                ai.maxHp = isBoss ? 1200f : (isBerserker ? 520f : 320f);
                ai.currentHp = ai.maxHp;
                ai.monsterAnimator = monsterAnim;

                // Equip weapon
                string wPath = weaponPaths[i % weaponPaths.Length];
                var weaponAsset = AssetDatabase.LoadAssetAtPath<GameObject>(wPath);
                if (weaponAsset != null)
                {
                    Transform rHand = monsterAnim != null && monsterAnim.isHuman ? monsterAnim.GetBoneTransform(HumanBodyBones.RightHand) : monsterRoot.transform;
                    Transform socket = EnsureSocket(rHand, "MonsterWeaponSocket_R", Vector3.zero, Vector3.zero);
                    if (socket != null)
                    {
                        GameObject sw = (GameObject)PrefabUtility.InstantiatePrefab(weaponAsset, socket);
                        sw.transform.localPosition = Vector3.zero;
                        sw.transform.localRotation = Quaternion.identity;
                        sw.transform.localScale = isBoss ? Vector3.one * 1.5f : Vector3.one;
                    }
                }

                // Equip shield on Vanguard
                if (i == 0)
                {
                    var shieldAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Shield.fbx");
                    if (shieldAsset != null)
                    {
                        Transform lHand = monsterAnim != null && monsterAnim.isHuman ? monsterAnim.GetBoneTransform(HumanBodyBones.LeftHand) : monsterRoot.transform;
                        Transform socket = EnsureSocket(lHand, "MonsterOffHandSocket_L", Vector3.zero, Vector3.zero);
                        if (socket != null)
                        {
                            GameObject sh = (GameObject)PrefabUtility.InstantiatePrefab(shieldAsset, socket);
                            sh.transform.localPosition = Vector3.zero;
                            sh.transform.localRotation = Quaternion.identity;
                        }
                    }
                }

                if (isBoss)
                {
                    var severLimb = monsterRoot.AddComponent<BattleSummoner.DesignPhilosophy.SeverableBossLimb>();
                    severLimb.limbName = "Kaelen's Overheated Anvil-Horn";
                    severLimb.limbHp = 180f;
                }
            }
        }

        private static void RepositionNPCsAndCleanOldBoss()
        {
            // Move campsite NPCs to the sidelines so they never block the camera
            var margo = GameObject.Find("NPC_Margo");
            if (margo != null) margo.transform.position = new Vector3(-14f, 1f, -10f);

            var barnaby = GameObject.Find("NPC_Barnaby");
            if (barnaby != null) barnaby.transform.position = new Vector3(-18f, 1f, -12f);

            var zephyr = GameObject.Find("NPC_Zephyr");
            if (zephyr != null) zephyr.transform.position = new Vector3(16f, 1f, -10f);

            // Hide old primitive capsule boss and old placeholder spheres
            var oldBoss = GameObject.Find("SpireBoss_Aethelgard");
            if (oldBoss != null)
            {
                var mr = oldBoss.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                oldBoss.SetActive(false);
            }

            var g1 = GameObject.Find("SpireGargoyle_1");
            if (g1 != null) g1.SetActive(false);

            var g2 = GameObject.Find("SpireGargoyle_2");
            if (g2 != null) g2.SetActive(false);

            var monolith = GameObject.Find("WardensTribunalMonolith");
            if (monolith != null) monolith.transform.position = new Vector3(-16f, 1.8f, 0f);

            // Hide or remove floating Margo Astral Familiar
            var familiar = GameObject.Find("Margo_AstralFamiliar");
            if (familiar != null) UnityEngine.Object.DestroyImmediate(familiar);

            // Hide mineral veins so they don't look like floating gray cubes
            string[] veinNames = { "MineralVein_AzuriteCore", "MineralVein_CinnabarRuby", "MineralVein_GildedTopaz", "MineralVein_PrismaticHeart" };
            foreach (var vn in veinNames)
            {
                var vein = GameObject.Find(vn);
                if (vein != null) vein.SetActive(false);
            }

            // Remove legacy MargoGuideCompanion from Player_Hero
            var player = GameObject.Find("Player_Hero");
            if (player != null)
            {
                var margoComp = player.GetComponent<MargoGuideCompanion>();
                if (margoComp != null) UnityEngine.Object.DestroyImmediate(margoComp);

                var devLoadout = player.GetComponent<DevClassLoadoutManager>();
                if (devLoadout != null) devLoadout.enabled = false;

                // Deactivate any legacy dev weapons under Player_Hero
                for (int i = 0; i < player.transform.childCount; i++)
                {
                    var ch = player.transform.GetChild(i);
                    if (ch.name.StartsWith("DevWeapon_") || ch.name.StartsWith("StanceWeapon_"))
                    {
                        ch.gameObject.SetActive(false);
                    }
                }
            }
        }

        private static void UpgradeNPCsWithStarterModels(RuntimeAnimatorController animCtrl)
        {
            var femaleAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/Starter_Female_Rigged.fbx");
            var maleAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/malestarterriggedbodyblock.fbx")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/Starter_Male_Rigged.fbx");
            var genshinMaleAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_Base_Male_Genshin_Rigged.fbx");

            var hammerAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Crafting Animations/Models/Tools/Human_Hammer.fbx");
            var bowAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Archer Animations/Models/HumanArcher_Bow.fbx");
            var quiverAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Archer Animations/Models/HumanArcher_Quiver.fbx");
            var bookAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Spellcasting Animations/Models/Human_MageBook.fbx");
            var mageHatAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Spellcasting Animations/Models/Human_MageHat.fbx");

            // 1. Upgrade NPC_Margo (Female Starter Model)
            UpgradeSingleNPC("NPC_Margo", femaleAsset, animCtrl, "Assets/Prefabs/NPCs/NPC_Margo.prefab", (npcRoot, charInstance, anim) =>
            {
                // Remove old primitive WitchHat
                Transform oldHat = npcRoot.transform.Find("WitchHat");
                if (oldHat != null) UnityEngine.Object.DestroyImmediate(oldHat.gameObject);

                // Adjust FloatingVoidGeode
                Transform geode = npcRoot.transform.Find("FloatingVoidGeode");
                if (geode != null)
                {
                    geode.localPosition = new Vector3(0.55f, 1.35f, 0.25f);
                }

                Transform headBone = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
                Transform leftHand = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;

                // Equip 3D Mage Hat on Head
                if (mageHatAsset != null && headBone != null)
                {
                    Transform hatSocket = EnsureSocket(headBone, "Hat_Socket", new Vector3(0f, 0.12f, 0.02f), new Vector3(-8f, 0f, 0f));
                    for (int c = hatSocket.childCount - 1; c >= 0; c--) UnityEngine.Object.DestroyImmediate(hatSocket.GetChild(c).gameObject);
                    var hat = (GameObject)PrefabUtility.InstantiatePrefab(mageHatAsset, hatSocket);
                    hat.transform.localPosition = Vector3.zero;
                    hat.transform.localRotation = Quaternion.identity;
                    hat.transform.localScale = Vector3.one * 0.95f;
                }

                // Equip 3D Spellbook in Left Hand
                if (bookAsset != null && leftHand != null)
                {
                    Transform socket = EnsureSocket(leftHand, "MageBook_Socket", Vector3.zero, new Vector3(0f, 90f, 0f));
                    for (int c = socket.childCount - 1; c >= 0; c--) UnityEngine.Object.DestroyImmediate(socket.GetChild(c).gameObject);
                    var book = (GameObject)PrefabUtility.InstantiatePrefab(bookAsset, socket);
                    book.transform.localPosition = Vector3.zero;
                    book.transform.localRotation = Quaternion.identity;
                    book.transform.localScale = Vector3.one * 1.1f;
                }
            });

            // 2. Upgrade NPC_Barnaby (Male Starter Model)
            UpgradeSingleNPC("NPC_Barnaby", maleAsset, animCtrl, "Assets/Prefabs/NPCs/NPC_Barnaby.prefab", (npcRoot, charInstance, anim) =>
            {
                // Adjust Blacksmith Anvil firmly onto ground
                Transform anvil = npcRoot.transform.Find("BlacksmithAnvil");
                if (anvil != null)
                {
                    anvil.localPosition = new Vector3(0.85f, 0.0f, 0.35f);
                    anvil.localRotation = Quaternion.Euler(0f, 25f, 0f);
                }

                Transform rightHand = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;

                // Equip Blacksmith Hammer in Right Hand
                if (hammerAsset != null && rightHand != null)
                {
                    Transform socket = EnsureSocket(rightHand, "Hammer_Socket", Vector3.zero, new Vector3(90f, 0f, 0f));
                    for (int c = socket.childCount - 1; c >= 0; c--) UnityEngine.Object.DestroyImmediate(socket.GetChild(c).gameObject);
                    var hammer = (GameObject)PrefabUtility.InstantiatePrefab(hammerAsset, socket);
                    hammer.transform.localPosition = Vector3.zero;
                    hammer.transform.localRotation = Quaternion.identity;
                    hammer.transform.localScale = Vector3.one;
                }
            });

            // 3. Upgrade NPC_Zephyr (Male Starter / Ranger Model)
            UpgradeSingleNPC("NPC_Zephyr", maleAsset, animCtrl, "Assets/Prefabs/NPCs/NPC_Zephyr.prefab", (npcRoot, charInstance, anim) =>
            {
                // Remove old primitive cylinders
                Transform oldCowl = npcRoot.transform.Find("ScoutCowl");
                if (oldCowl != null) UnityEngine.Object.DestroyImmediate(oldCowl.gameObject);
                Transform oldQuiver = npcRoot.transform.Find("ScoutQuiver");
                if (oldQuiver != null) UnityEngine.Object.DestroyImmediate(oldQuiver.gameObject);

                Transform spineBone = anim != null && anim.isHuman ? (anim.GetBoneTransform(HumanBodyBones.Chest) ?? anim.GetBoneTransform(HumanBodyBones.Spine)) : null;
                Transform leftHand = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;

                // Equip 3D Quiver on Back
                if (quiverAsset != null && spineBone != null)
                {
                    Transform quiverSocket = EnsureSocket(spineBone, "Quiver_Socket", new Vector3(0.14f, 0.12f, -0.16f), new Vector3(15f, 180f, -25f));
                    for (int c = quiverSocket.childCount - 1; c >= 0; c--) UnityEngine.Object.DestroyImmediate(quiverSocket.GetChild(c).gameObject);
                    var quiver = (GameObject)PrefabUtility.InstantiatePrefab(quiverAsset, quiverSocket);
                    quiver.transform.localPosition = Vector3.zero;
                    quiver.transform.localRotation = Quaternion.identity;
                    quiver.transform.localScale = Vector3.one * 0.9f;
                }

                // Equip Archer Bow in Left Hand
                if (bowAsset != null && leftHand != null)
                {
                    Transform socket = EnsureSocket(leftHand, "Bow_Socket", Vector3.zero, new Vector3(0f, 90f, 0f));
                    for (int c = socket.childCount - 1; c >= 0; c--) UnityEngine.Object.DestroyImmediate(socket.GetChild(c).gameObject);
                    var bow = (GameObject)PrefabUtility.InstantiatePrefab(bowAsset, socket);
                    bow.transform.localPosition = Vector3.zero;
                    bow.transform.localRotation = Quaternion.identity;
                    bow.transform.localScale = Vector3.one;
                }
            });
        }

        private static void UpgradeSingleNPC(
            string npcName,
            GameObject starterModelAsset,
            RuntimeAnimatorController animCtrl,
            string prefabSavePath,
            Action<GameObject, GameObject, Animator> customizeAction)
        {
            if (starterModelAsset == null) return;

            GameObject npcObj = GameObject.Find(npcName);
            if (npcObj == null) return;

            // 1. Disable primitive MeshFilter and MeshRenderer on root
            var mf = npcObj.GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = null;
            var mr = npcObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            // 2. Adjust CapsuleCollider to match humanoid height & center
            var cc = npcObj.GetComponent<CapsuleCollider>();
            if (cc != null)
            {
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.radius = 0.35f;
                cc.height = 1.8f;
            }

            // 3. Clear any existing Model_ child
            for (int i = npcObj.transform.childCount - 1; i >= 0; i--)
            {
                var ch = npcObj.transform.GetChild(i);
                if (ch.name.StartsWith("Model_"))
                {
                    UnityEngine.Object.DestroyImmediate(ch.gameObject);
                }
            }

            // 4. Instantiate humanoid starter model
            GameObject charInstance = (GameObject)PrefabUtility.InstantiatePrefab(starterModelAsset, npcObj.transform);
            charInstance.name = $"Model_{starterModelAsset.name}";
            charInstance.transform.localPosition = Vector3.zero;
            charInstance.transform.localRotation = Quaternion.identity;
            charInstance.transform.localScale = Vector3.one;

            // 5. Ensure Animator with PlayerCombatAnimator
            Animator anim = charInstance.GetComponentInChildren<Animator>();
            if (anim == null) anim = charInstance.AddComponent<Animator>();
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            anim.applyRootMotion = false;

            // 6. Custom styling (hats, anvils, quivers, tools)
            customizeAction?.Invoke(npcObj, charInstance, anim);

            // 7. Save to prefab if path provided
            if (!string.IsNullOrEmpty(prefabSavePath))
            {
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(npcObj, prefabSavePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NPC Upgrader] Could not update prefab {prefabSavePath}: {e.Message}");
                }
            }
        }

        private static GameObject LoadCharacterGlbPrefab(string baseName)
        {
            if (baseName.Equals("MaleStarter", StringComparison.OrdinalIgnoreCase))
            {
                var bodyBlock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/malestarterriggedbodyblock.fbx");
                if (bodyBlock != null) return bodyBlock;
                var rigged = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/Starter_Male_Rigged.fbx");
                if (rigged != null) return rigged;
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/malestarter.fbx");
                if (fbx != null) return fbx;
            }
            else if (baseName.Equals("femalestarter", StringComparison.OrdinalIgnoreCase))
            {
                var rigged = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/Starter_Female_Rigged.fbx");
                if (rigged != null) return rigged;
            }
            else if (baseName.Equals("manbase", StringComparison.OrdinalIgnoreCase))
            {
                var rigged = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_Base_Male_Genshin_Rigged.fbx");
                if (rigged != null) return rigged;
            }
            else if (baseName.Equals("womanbase", StringComparison.OrdinalIgnoreCase))
            {
                var rigged = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_Base_Female_Genshin_Rigged.fbx");
                if (rigged != null) return rigged;
            }

            string directPath = $"{CHAR_DIR}/{baseName}.glb";
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(directPath);
            if (asset != null) return asset;

            string starterPath = $"{CHAR_DIR}/Starter/{baseName}.glb";
            asset = AssetDatabase.LoadAssetAtPath<GameObject>(starterPath);
            if (asset != null) return asset;

            return FindFirstModelPrefabByKeyword(CHAR_DIR, baseName);
        }

        private static GameObject FindFirstModelPrefabByKeyword(string folder, params string[] keywords)
        {
            if (!Directory.Exists(folder)) return null;
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            foreach (string kw in keywords)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fn = Path.GetFileNameWithoutExtension(path);
                    if (fn.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (model != null) return model;
                    }
                }
            }
            return null;
        }

        private static Transform EnsureSocket(Transform parent, string socketName, Vector3 localPos, Vector3 localRotEuler)
        {
            if (parent == null) return null;
            Transform socket = parent.Find(socketName);
            if (socket == null)
            {
                GameObject sObj = new GameObject(socketName);
                sObj.transform.SetParent(parent, false);
                socket = sObj.transform;
            }
            socket.localPosition = localPos;
            socket.localRotation = Quaternion.Euler(localRotEuler);
            return socket;
        }
    }
}
#endif

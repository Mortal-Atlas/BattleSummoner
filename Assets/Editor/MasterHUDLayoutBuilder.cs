using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BattleSummoner.Combat;
using BattleSummoner.UI;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// MasterHUDLayoutBuilder builds the unified mobile portrait player HUD (1080x2340)
    /// following the user's diagram specification with complete fidelity:
    /// - Top Row: Settings (top-left), Notification Broadcast Banner (top-center), Mini Map (top-right).
    /// - Left: Player Profile avatar (red border), Health bar (top), Mana bar (bottom), 5 Buff/Debuff circular slots, AI button, Dispel button (bottom-left of play area).
    /// - Center: Play Area framed by cyan boundary.
    /// - Right: Inventory chest button, Ally 1 through Ally 9 cards (single column).
    /// - Bottom Deck: Item Carousel (with left/right arrows), Dash button (center), Jump button (large center),
    ///   Joy Stick (bottom-left), Z-Target & Swap buttons (bottom-right), Camera Control Area (bottom-right touch pan).
    /// Deletes all unnecessary clutter and unifies all scenes onto one Player HUD prefab.
    /// </summary>
    public static class MasterHUDLayoutBuilder
    {
        private const string PROCESSED_DIR = "Assets/Textures/HUD_UserAssets/Processed";
        private const string TEXTURE_UI_DIR = "Assets/Textures/UI";
        public const string PREFAB_PATH = "Assets/Prefabs/UI/PortraitHUD.prefab";
        public const string ALT_PREFAB_PATH = "Assets/Prefabs/UI/PlayerHUD.prefab";

        private static readonly string[] TARGET_SCENES = new string[]
        {
            "Assets/Scenes/OverworldMap.unity",
            "Assets/Scenes/MainDungeonFloor.unity",
            "Assets/Scenes/BuildingInterior.unity",
            "Assets/Scenes/GuildHallInterior.unity",
            "Assets/Scenes/ShopInterior.unity"
        };

        [MenuItem("Tools/BattleSummoner/Apply Unified Player HUD to All Scenes")]
        public static void ApplyToAllScenes()
        {
            // 1. Build and save the master prefab
            var prefabObj = BuildMasterPrefab();

            // 2. Apply prefab to every target scene
            foreach (var scenePath in TARGET_SCENES)
            {
                ApplyPrefabToScene(scenePath, prefabObj);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("✨ [MASTER HUD] Successfully unified and applied the Player HUD prefab across all scenes!");
        }

        public static GameObject BuildMasterPrefab()
        {
            // Build root GameObject
            var rootObj = new GameObject("PlayerHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootObj.layer = 5;

            var canvas = rootObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = rootObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var hudController = rootObj.AddComponent<PlayerHUDController>();

            // Build all HUD hierarchy elements
            ConstructHUDElements(rootObj.transform, hudController);

            // Save as PortraitHUD.prefab and PlayerHUD.prefab
            string dir = Path.GetDirectoryName(PREFAB_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootObj, PREFAB_PATH);
            PrefabUtility.SaveAsPrefabAsset(rootObj, ALT_PREFAB_PATH);

            UnityEngine.Object.DestroyImmediate(rootObj);
            Debug.Log($"💾 [MASTER HUD] Successfully saved prefab to {PREFAB_PATH} and {ALT_PREFAB_PATH}!");
            return savedPrefab;
        }

        public static void ApplyPrefabToScene(string scenePath, GameObject prefabObj = null)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[MASTER HUD] Scene not found: {scenePath}");
                return;
            }

            if (prefabObj == null)
            {
                prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            // 1. Clean up legacy or mismatched HUD objects in scene
            string[] legacyNames = new string[]
            {
                "OverworldHUDCanvas", "PortraitHUDCanvas", "PlayerHUDCanvas",
                "MockupMasterHUD", "SpireBroadcastBanner", "TopRight_NavigationDeck",
                "BottomRight_CombatDeck", "CenterToastGroup", "QuickItemCarousel",
                "TopTelemetryHUD", "HoldAndFlickGestureButton", "SystemBroadcastBanner",
                "BottomControlDeck", "QuickZTargetingPanel", "PlayerStatsCluster",
                "BattleSideProfile_HUD", "TwoColumnPartyRoster", "PartySquadColumn",
                "FloatingVirtualStick", "InspectionCamera"
            };

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var name in legacyNames)
                {
                    if (root != null && root.name == name)
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                        break;
                    }
                }
            }

            // 2. Instantiate master prefab
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabObj, scene);
            instance.name = "PlayerHUDCanvas";

            // 3. Mark dirty and save scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"💾 [MASTER HUD] Applied PlayerHUD prefab to {scenePath}");
        }

        private static void ConstructHUDElements(Transform root, PlayerHUDController hud)
        {
            // Helper functions
            Sprite LoadSprite(string path)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp == null)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null)
                    {
                        sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
                return sp;
            }

            GameObject CreateImage(Transform parent, string name, float x, float y, float w, float h, Vector2 anchor, Sprite sprite, Color color, bool raycast = false)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = 5;
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(w, h);
                var img = go.GetComponent<Image>();
                img.color = color;
                img.sprite = sprite;
                img.raycastTarget = raycast;
                if (sprite != null) img.preserveAspect = true;
                return go;
            }

            GameObject CreateButton(Transform parent, string name, float x, float y, float w, float h, Vector2 anchor, Sprite sprite, Color? color = null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.layer = 5;
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(w, h);
                var img = go.GetComponent<Image>();
                img.color = color ?? Color.white;
                img.sprite = sprite;
                img.raycastTarget = true;
                if (sprite != null) img.preserveAspect = true;

                var btn = go.GetComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                btn.colors = colors;
                return go;
            }

            TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float x, float y, float w, float h, int fontSize, TextAlignmentOptions align, Color color, Vector2? anchor = null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.layer = 5;
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                var anc = anchor ?? new Vector2(0.5f, 0.5f);
                rt.anchorMin = anc; rt.anchorMax = anc; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(w, h);
                var tmp = go.GetComponent<TextMeshProUGUI>();
                tmp.text = text;
                tmp.fontSize = fontSize;
                tmp.alignment = align;
                tmp.color = color;
                tmp.raycastTarget = false;
                return tmp;
            }

            // -------------------------------------------------------------
            // 0. SPELL DRAWING INK CANVAS (Background Gesture Layer)
            // -------------------------------------------------------------
            var inkCanvasObj = new GameObject("SpellDrawingInkCanvas", typeof(RectTransform), typeof(Image), typeof(SpellDrawingInkCanvas));
            inkCanvasObj.layer = 5;
            inkCanvasObj.transform.SetParent(root, false);
            inkCanvasObj.transform.SetAsFirstSibling();
            var inkRt = inkCanvasObj.GetComponent<RectTransform>();
            inkRt.anchorMin = Vector2.zero; inkRt.anchorMax = Vector2.one;
            inkRt.offsetMin = Vector2.zero; inkRt.offsetMax = Vector2.zero;
            var inkImg = inkCanvasObj.GetComponent<Image>();
            inkImg.color = Color.clear;
            inkImg.raycastTarget = false;

            // Green Ring Sprite for diagram action buttons (hidden by default)
            var greenRingSprite = LoadSprite($"{PROCESSED_DIR}/UI_GreenCircleRing.png");

            void AddGreenRing(GameObject btnGo, float size)
            {
                var ringGo = CreateImage(btnGo.transform, "GreenRing", 0f, 0f, size, size, new Vector2(0.5f, 0.5f), greenRingSprite, Color.white);
                ringGo.transform.SetAsFirstSibling();
                ringGo.SetActive(false); // Diagram guide line hidden
            }

            // -------------------------------------------------------------
            // 1. TOP ROW: SETTINGS (Left), NOTIFICATION BROADCAST (Center), MINI MAP (Right)
            // -------------------------------------------------------------
            // A. Settings Button (top-left)
            var settingsSprite = LoadSprite($"{PROCESSED_DIR}/UI_Settings.png");
            var settingsGo = CreateButton(root, "SettingsButton", 90f, -100f, 120f, 120f, new Vector2(0f, 1f), settingsSprite);
            AddGreenRing(settingsGo, 130f);
            hud.settingsButton = settingsGo.GetComponent<Button>();

            // B. Notification Broadcast Banner (top-center)
            var notifGo = new GameObject("NotificationBroadcastBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            notifGo.layer = 5;
            notifGo.transform.SetParent(root, false);
            var notifRt = notifGo.GetComponent<RectTransform>();
            notifRt.anchorMin = new Vector2(0.5f, 1f); notifRt.anchorMax = new Vector2(0.5f, 1f); notifRt.pivot = new Vector2(0.5f, 0.5f);
            notifRt.anchoredPosition = new Vector2(0f, -100f);
            notifRt.sizeDelta = new Vector2(620f, 95f);
            var notifImg = notifGo.GetComponent<Image>();
            notifImg.color = new Color(0.08f, 0.10f, 0.15f, 0.88f); // Sleek dark translucent backdrop
            notifImg.raycastTarget = false;

            // Magenta outline border (diagram guide line hidden)
            var notifBorder = CreateImage(notifGo.transform, "MagentaBorder", 0f, 0f, 620f, 95f, new Vector2(0.5f, 0.5f), null, new Color(0.92f, 0.18f, 0.56f, 1f));
            var borderOutline = notifBorder.AddComponent<Outline>();
            borderOutline.effectColor = new Color(1f, 0.2f, 0.65f, 0.9f);
            borderOutline.effectDistance = new Vector2(3f, -3f);
            notifBorder.SetActive(false);

            var notifTmp = CreateTMP(notifGo.transform, "NotificationText", "<b>Notification Broadcast</b>", 0f, 0f, 580f, 60f, 20, TextAlignmentOptions.Center, new Color(1f, 0.94f, 0.82f));
            hud.notificationBanner = notifRt;
            hud.notificationText = notifTmp;

            // C. Mini Map (Circle, top-right)
            var compassSprite = LoadSprite($"{PROCESSED_DIR}/UI_Compass_Minimap.png");
            var minimapGo = new GameObject("MiniMapFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MinimapRadarController));
            minimapGo.layer = 5;
            minimapGo.transform.SetParent(root, false);
            var mmRt = minimapGo.GetComponent<RectTransform>();
            mmRt.anchorMin = new Vector2(1f, 1f); mmRt.anchorMax = new Vector2(1f, 1f); mmRt.pivot = new Vector2(0.5f, 0.5f);
            mmRt.anchoredPosition = new Vector2(-95f, -100f);
            mmRt.sizeDelta = new Vector2(130f, 130f);
            var mmImg = minimapGo.GetComponent<Image>();
            mmImg.sprite = compassSprite;
            mmImg.color = Color.white;
            mmImg.preserveAspect = true;
            mmImg.raycastTarget = true; // Enables tapping the minimap to open the World Exploration Map!
            var mmBtn = minimapGo.AddComponent<Button>();

            // Player Radar Blip in center of minimap
            var blipGo = CreateImage(minimapGo.transform, "PlayerRadarBlip", 0f, 0f, 16f, 16f, new Vector2(0.5f, 0.5f), null, new Color(0.18f, 0.95f, 0.40f, 1f));
            var blipOutline = blipGo.AddComponent<Outline>();
            blipOutline.effectColor = Color.white;
            blipOutline.effectDistance = new Vector2(1.5f, 1.5f);

            hud.minimapFrame = mmRt;
            hud.minimapRadar = minimapGo.GetComponent<MinimapRadarController>();

            // Ensure WorldMapExplorationModal on HUD Canvas
            if (root.GetComponent<WorldMapExplorationModal>() == null)
            {
                root.gameObject.AddComponent<WorldMapExplorationModal>();
            }

            // -------------------------------------------------------------
            // 2. CENTER: PLAY AREA
            // -------------------------------------------------------------
            var playAreaGo = new GameObject("PlayAreaFrame", typeof(RectTransform));
            playAreaGo.layer = 5;
            playAreaGo.transform.SetParent(root, false);
            var paRt = playAreaGo.GetComponent<RectTransform>();
            paRt.anchorMin = Vector2.zero; paRt.anchorMax = Vector2.one;
            paRt.offsetMin = new Vector2(15f, 705f); // bottom above carousel
            paRt.offsetMax = new Vector2(-150f, -165f); // right before allies, top below top bar

            // Cyan Border Lines (Top, Bottom, Left, Right) - diagram guide lines hidden by default
            Color cyanColor = new Color(0.0f, 0.92f, 1.0f, 0.95f);
            float borderThick = 4f;

            // Left Cyan Border
            var bLeft = CreateImage(playAreaGo.transform, "BorderLeft", 0f, 0f, borderThick, 0f, new Vector2(0f, 0.5f), null, cyanColor);
            var bLeftRt = bLeft.GetComponent<RectTransform>();
            bLeftRt.anchorMin = new Vector2(0f, 0f); bLeftRt.anchorMax = new Vector2(0f, 1f);
            bLeftRt.sizeDelta = new Vector2(borderThick, 0f);
            bLeft.SetActive(false);

            // Right Cyan Border
            var bRight = CreateImage(playAreaGo.transform, "BorderRight", 0f, 0f, borderThick, 0f, new Vector2(1f, 0.5f), null, cyanColor);
            var bRightRt = bRight.GetComponent<RectTransform>();
            bRightRt.anchorMin = new Vector2(1f, 0f); bRightRt.anchorMax = new Vector2(1f, 1f);
            bRightRt.sizeDelta = new Vector2(borderThick, 0f);
            bRight.SetActive(false);

            // Top Cyan Border
            var bTop = CreateImage(playAreaGo.transform, "BorderTop", 0f, 0f, 0f, borderThick, new Vector2(0.5f, 1f), null, cyanColor);
            var bTopRt = bTop.GetComponent<RectTransform>();
            bTopRt.anchorMin = new Vector2(0f, 1f); bTopRt.anchorMax = new Vector2(1f, 1f);
            bTopRt.sizeDelta = new Vector2(0f, borderThick);
            bTop.SetActive(false);

            // Bottom Cyan Border
            var bBottom = CreateImage(playAreaGo.transform, "BorderBottom", 0f, 0f, 0f, borderThick, new Vector2(0.5f, 0f), null, cyanColor);
            var bBottomRt = bBottom.GetComponent<RectTransform>();
            bBottomRt.anchorMin = new Vector2(0f, 0f); bBottomRt.anchorMax = new Vector2(1f, 0f);
            bBottomRt.sizeDelta = new Vector2(0f, borderThick);
            bBottom.SetActive(false);

            hud.playAreaFrame = paRt;
            hud.showGuideLines = false;

            // -------------------------------------------------------------
            // 3. LEFT CLUSTER: PLAYER PROFILE, HEALTH & MANA BARS, BUFF/DEBUFF, AI, DISPEL
            // -------------------------------------------------------------
            // A. Player Profile Avatar
            var pfpBoxGo = new GameObject("PlayerProfileBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pfpBoxGo.layer = 5;
            pfpBoxGo.transform.SetParent(root, false);
            var pfpBoxRt = pfpBoxGo.GetComponent<RectTransform>();
            pfpBoxRt.anchorMin = new Vector2(0f, 1f); pfpBoxRt.anchorMax = new Vector2(0f, 1f); pfpBoxRt.pivot = new Vector2(0.5f, 0.5f);
            pfpBoxRt.anchoredPosition = new Vector2(95f, -245f);
            pfpBoxRt.sizeDelta = new Vector2(130f, 130f);
            var pfpBoxImg = pfpBoxGo.GetComponent<Image>();
            pfpBoxImg.color = new Color(0.18f, 0.22f, 0.30f, 0.85f); // Sleek metallic frame

            // Inner dark backing
            var pfpInner = CreateImage(pfpBoxGo.transform, "InnerBacking", 0f, 0f, 120f, 120f, new Vector2(0.5f, 0.5f), null, new Color(0.08f, 0.08f, 0.12f, 1f));
            var pfpSprite = LoadSprite("Assets/Textures/UI/Portraits/Portrait_Player_Feme_Right_Cutout.png")
                ?? LoadSprite($"{PROCESSED_DIR}/UI_Profile_Female.png");
            var pfpAvatar = CreateImage(pfpInner.transform, "ProfileAvatar", 0f, 0f, 116f, 116f, new Vector2(0.5f, 0.5f), pfpSprite, Color.white);
            pfpAvatar.GetComponent<Image>().preserveAspect = true;
            hud.playerProfileAvatar = pfpAvatar.GetComponent<Image>();

            // B. Health Bar (Red, on top of mana bar)
            var hpBarGo = CreateImage(root, "HealthBar", 320f, -215f, 300f, 48f, new Vector2(0f, 1f), null, new Color(0.24f, 0.06f, 0.08f, 0.95f));
            var hpFillGo = CreateImage(hpBarGo.transform, "HealthFill", 0f, 0f, 292f, 40f, new Vector2(0.5f, 0.5f), null, new Color(0.88f, 0.18f, 0.24f, 1f));
            var hpFillImg = hpFillGo.GetComponent<Image>();
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.fillAmount = 0.90f;
            hud.playerHealthFill = hpFillImg;

            CreateTMP(hpBarGo.transform, "HPLabel", "<b>HEALTH</b>", -95f, 0f, 80f, 28f, 13, TextAlignmentOptions.Left, Color.white);
            var hpValTMP = CreateTMP(hpBarGo.transform, "HPValue", "1220 / 1350", 45f, 0f, 170f, 28f, 13, TextAlignmentOptions.Right, Color.white);
            hud.playerHealthText = hpValTMP;

            // C. Mana Bar (Cyan, directly below health bar)
            var mpBarGo = CreateImage(root, "ManaBar", 320f, -272f, 300f, 48f, new Vector2(0f, 1f), null, new Color(0.05f, 0.18f, 0.26f, 0.95f));
            var mpFillGo = CreateImage(mpBarGo.transform, "ManaFill", 0f, 0f, 292f, 40f, new Vector2(0.5f, 0.5f), null, new Color(0.15f, 0.65f, 0.98f, 1f));
            var mpFillImg = mpFillGo.GetComponent<Image>();
            mpFillImg.type = Image.Type.Filled;
            mpFillImg.fillMethod = Image.FillMethod.Horizontal;
            mpFillImg.fillAmount = 0.96f;
            hud.playerManaFill = mpFillImg;

            CreateTMP(mpBarGo.transform, "MPLabel", "<b>MANA</b>", -95f, 0f, 80f, 28f, 13, TextAlignmentOptions.Left, Color.white);
            var mpValTMP = CreateTMP(mpBarGo.transform, "MPValue", "720 / 750", 45f, 0f, 170f, 28f, 13, TextAlignmentOptions.Right, Color.white);
            hud.playerManaText = mpValTMP;

            // D. Buff / Debuff Icons (Row of 5 circles directly below Mana bar)
            var buffRowGo = new GameObject("BuffDebuffRow", typeof(RectTransform));
            buffRowGo.layer = 5;
            buffRowGo.transform.SetParent(root, false);
            var buffRt = buffRowGo.GetComponent<RectTransform>();
            buffRt.anchorMin = new Vector2(0f, 1f); buffRt.anchorMax = new Vector2(0f, 1f); buffRt.pivot = new Vector2(0.5f, 0.5f);
            buffRt.anchoredPosition = new Vector2(320f, -332f);
            buffRt.sizeDelta = new Vector2(300f, 44f);

            string[] buffSprites = new string[]
            {
                $"{TEXTURE_UI_DIR}/Mockup_Buff_Attack.png",
                $"{TEXTURE_UI_DIR}/Mockup_Buff_Defense.png",
                $"{TEXTURE_UI_DIR}/Mockup_Buff_Haste.png",
                $"{TEXTURE_UI_DIR}/Mockup_Buff_Regen.png",
                $"{TEXTURE_UI_DIR}/Mockup_Buff_Spirit.png"
            };

            float buffStartX = -120f;
            float buffStepX = 60f;
            for (int i = 0; i < 5; i++)
            {
                var slotCircle = CreateImage(buffRowGo.transform, $"BuffSlot_{i + 1}", buffStartX + i * buffStepX, 0f, 44f, 44f, new Vector2(0.5f, 0.5f), null, new Color(0.16f, 0.48f, 1.0f, 1f));
                var slotInner = CreateImage(slotCircle.transform, "Inner", 0f, 0f, 38f, 38f, new Vector2(0.5f, 0.5f), null, new Color(0.08f, 0.12f, 0.22f, 0.95f));
                var sp = LoadSprite(buffSprites[i]);
                var iconGo = CreateImage(slotInner.transform, "Icon", 0f, 0f, 32f, 32f, new Vector2(0.5f, 0.5f), sp, Color.white);
                hud.buffDebuffSlots[i] = iconGo.GetComponent<Image>();
            }

            // E. AI Button (Green circle, directly below Player Profile)
            var aiSprite = LoadSprite($"{PROCESSED_DIR}/UI_AI.png");
            var aiGo = CreateButton(root, "AIPlayButton", 95f, -425f, 120f, 120f, new Vector2(0f, 1f), aiSprite);
            AddGreenRing(aiGo, 130f);
            hud.aiButton = aiGo.GetComponent<Button>();

            // F. Dispel Button (bottom-left of Play area)
            var dispelSprite = LoadSprite($"{PROCESSED_DIR}/UI_Dispel.png");
            var dispelGo = CreateButton(root, "DispelButton", 115f, 765f, 175f, 95f, new Vector2(0f, 0f), null, new Color(0.20f, 0.14f, 0.18f, 0.95f));
            var dOutline = dispelGo.AddComponent<Outline>();
            dOutline.effectColor = new Color(0.45f, 0.22f, 0.28f, 0.75f);
            dOutline.effectDistance = new Vector2(2f, -2f);

            if (dispelSprite != null)
            {
                CreateImage(dispelGo.transform, "DispelIcon", -45f, 0f, 65f, 65f, new Vector2(0.5f, 0.5f), dispelSprite, Color.white);
                CreateTMP(dispelGo.transform, "DispelText", "<b>DISPEL</b>", 25f, 0f, 100f, 40f, 18, TextAlignmentOptions.Center, Color.white);
            }
            else
            {
                CreateTMP(dispelGo.transform, "DispelText", "<b>DISPEL</b>", 0f, 0f, 160f, 40f, 20, TextAlignmentOptions.Center, Color.white);
            }
            hud.dispelButton = dispelGo.GetComponent<Button>();

            // -------------------------------------------------------------
            // 4. RIGHT FLANK: INVENTORY & ALLIES 1 THROUGH 9 (Single Column)
            // -------------------------------------------------------------
            // A. Inventory Chest Button (Below Mini Map)
            var invSprite = LoadSprite($"{PROCESSED_DIR}/UI_Inventory.png");
            var invGo = CreateButton(root, "InventoryButton", -75f, -235f, 115f, 110f, new Vector2(1f, 1f), invSprite);
            hud.inventoryButton = invGo.GetComponent<Button>();

            // B. Single-Column Party Squad (Allies 1 to 9)
            var squadColGo = new GameObject("PartySquadColumn", typeof(RectTransform));
            squadColGo.layer = 5;
            squadColGo.transform.SetParent(root, false);
            var sqRt = squadColGo.GetComponent<RectTransform>();
            sqRt.anchorMin = new Vector2(1f, 1f); sqRt.anchorMax = new Vector2(1f, 1f); sqRt.pivot = new Vector2(0.5f, 1f);
            sqRt.anchoredPosition = new Vector2(-75f, -345f);
            sqRt.sizeDelta = new Vector2(125f, 1000f);
            hud.partySquadColumn = sqRt;

            float allyStartY = 0f;
            float allyStepY = -106f;
            for (int i = 0; i < 9; i++)
            {
                int allyIndex = i + 1;
                var memberGo = CreateButton(squadColGo.transform, $"SquadMember_{allyIndex}", 0f, allyStartY + i * allyStepY, 120f, 98f, new Vector2(0.5f, 1f), null, new Color(0.14f, 0.16f, 0.22f, 0.95f)); // Sleek dark card border
                var mOutline = memberGo.AddComponent<Outline>();
                mOutline.effectColor = new Color(0.30f, 0.28f, 0.35f, 0.65f);
                mOutline.effectDistance = new Vector2(2f, -2f);

                // Inner card dark background
                var cardInner = CreateImage(memberGo.transform, "Inner", 0f, 0f, 112f, 90f, new Vector2(0.5f, 0.5f), null, new Color(0.10f, 0.08f, 0.14f, 0.95f));

                // Ally Portrait (Allies strictly look LEFT)
                string allySpritePath = (allyIndex % 2 == 0)
                    ? "Assets/Textures/UI/Portraits/Portrait_Ally_Feme_Left_Cutout.png"
                    : "Assets/Textures/UI/Portraits/Portrait_Ally_Masc_Left_Cutout.png";
                var squadSp = LoadSprite(allySpritePath) ?? LoadSprite($"{TEXTURE_UI_DIR}/Mockup_Squad_{allyIndex}.png");
                var pfpImg = CreateImage(cardInner.transform, "AllyPortrait", -28f, 16f, 48f, 48f, new Vector2(0.5f, 0.5f), squadSp, Color.white);
                pfpImg.GetComponent<Image>().preserveAspect = true;

                // Ally Label ("Sylph", "Ignis", etc.)
                string[] squadNames = new string[] { "Sylph", "Ignis", "Aqua", "Golem", "Aegis", "Faerie", "Shadow", "Falcon", "Valkyrie" };
                CreateTMP(cardInner.transform, "AllyLabel", $"<b>{squadNames[i]}</b>", 22f, 24f, 58f, 20f, 13, TextAlignmentOptions.Left, new Color(1f, 0.92f, 0.70f));

                string[] squadRoles = new string[] { "<color=#88FFBB>HEALER</color>", "<color=#FF6655>MAGE</color>", "<color=#55CCFF>SUPPORT</color>", "<color=#FFBB44>TANK</color>", "<color=#FFAA33>KNIGHT</color>", "<color=#88FFBB>CLERIC</color>", "<color=#CC88FF>ROGUE</color>", "<color=#55DDFF>ARCHER</color>", "<color=#FFCC00>PALADIN</color>" };
                CreateTMP(cardInner.transform, "RoleSubtitle", squadRoles[i], 22f, 10f, 58f, 16f, 9, TextAlignmentOptions.Left, Color.white);

                // Mini HP Bar (Red)
                float hpVal = 220f + allyIndex * 25f;
                var miniHp = CreateImage(cardInner.transform, "MiniHP", -16f, -14f, 66f, 10f, new Vector2(0.5f, 0.5f), null, new Color(0.20f, 0.05f, 0.07f, 0.95f));
                var miniHpFill = CreateImage(miniHp.transform, "Fill", 0f, 0f, 64f, 8f, new Vector2(0.5f, 0.5f), null, new Color(0.92f, 0.20f, 0.26f, 1f));
                var hfImg = miniHpFill.GetComponent<Image>();
                hfImg.type = Image.Type.Filled;
                hfImg.fillMethod = Image.FillMethod.Horizontal;
                hfImg.fillAmount = hpVal / 500f;

                // Number next to HP bar
                CreateTMP(cardInner.transform, "HPNumberText", $"<color=#FF5566><b>{(int)hpVal}</b></color>", 36f, -14f, 36f, 14f, 11, TextAlignmentOptions.Right, Color.white);

                // Mini MP Bar (Cyan)
                float mpVal = 120f + allyIndex * 8f;
                var miniMp = CreateImage(cardInner.transform, "MiniMP", -16f, -28f, 66f, 8f, new Vector2(0.5f, 0.5f), null, new Color(0.04f, 0.12f, 0.22f, 0.95f));
                var miniMpFill = CreateImage(miniMp.transform, "Fill", 0f, 0f, 64f, 6f, new Vector2(0.5f, 0.5f), null, new Color(0.18f, 0.75f, 1.0f, 1f));
                var mfImg = miniMpFill.GetComponent<Image>();
                mfImg.type = Image.Type.Filled;
                mfImg.fillMethod = Image.FillMethod.Horizontal;
                mfImg.fillAmount = mpVal / 200f;

                // Number next to MP bar
                CreateTMP(cardInner.transform, "MPNumberText", $"<color=#33DDFF><b>{(int)mpVal}</b></color>", 36f, -28f, 36f, 14f, 10, TextAlignmentOptions.Right, Color.white);

                hud.allyButtons[i] = memberGo.GetComponent<Button>();
            }

            // -------------------------------------------------------------
            // 5. BOTTOM CONTROLS DECK: CAROUSEL, DASH, JUMP, JOYSTICK, TARGETING, CAMERA
            // -------------------------------------------------------------
            // A. Item Carousel (Center, directly below Play area)
            var carouselSprite = LoadSprite($"{PROCESSED_DIR}/UI_ItemCarousel.png");
            var carouselGo = CreateImage(root, "QuickItemCarousel", 0f, 580f, 360f, 115f, new Vector2(0.5f, 0f), carouselSprite, Color.white);
            hud.itemCarouselRoot = carouselGo.GetComponent<RectTransform>();

            var arrowL = LoadSprite($"{TEXTURE_UI_DIR}/Mockup_ArrowLeft.png");
            var leftBtnGo = CreateButton(carouselGo.transform, "ArrowLeftButton", -135f, 0f, 55f, 75f, new Vector2(0.5f, 0.5f), arrowL);
            hud.carouselLeftBtn = leftBtnGo.GetComponent<Button>();

            var arrowR = LoadSprite($"{TEXTURE_UI_DIR}/Mockup_ArrowRight.png");
            var rightBtnGo = CreateButton(carouselGo.transform, "ArrowRightButton", 135f, 0f, 55f, 75f, new Vector2(0.5f, 0.5f), arrowR);
            hud.carouselRightBtn = rightBtnGo.GetComponent<Button>();

            var activeSlotGo = CreateButton(carouselGo.transform, "ActiveItemSlot", 0f, 0f, 85f, 85f, new Vector2(0.5f, 0.5f), null, Color.clear);
            hud.carouselItemSlot = activeSlotGo.GetComponent<Button>();

            var potionSprite = LoadSprite($"{TEXTURE_UI_DIR}/Mockup_PotionRed.png");
            var potionImgGo = CreateImage(activeSlotGo.transform, "ActiveItemIcon", 0f, 6f, 60f, 60f, new Vector2(0.5f, 0.5f), potionSprite, Color.white);
            hud.carouselItemIcon = potionImgGo.GetComponent<Image>();

            var countTMP = CreateTMP(activeSlotGo.transform, "ItemCountText", "x5", 24f, -14f, 35f, 20f, 14, TextAlignmentOptions.Right, new Color(1f, 0.90f, 0.50f));
            hud.carouselItemCount = countTMP;

            var nameTMP = CreateTMP(activeSlotGo.transform, "ItemNameText", "Crimson Draught", 0f, -38f, 130f, 20f, 12, TextAlignmentOptions.Center, new Color(0.92f, 0.22f, 0.28f));
            hud.carouselItemName = nameTMP;

            var carouselController = root.gameObject.AddComponent<QuickItemCarouselController>();
            carouselController.BindReferences(
                hud.carouselLeftBtn,
                hud.carouselRightBtn,
                hud.carouselItemSlot,
                hud.carouselItemIcon,
                hud.carouselItemCount,
                hud.carouselItemName,
                hud.itemCarouselRoot
            );

            // B. Dash Button (Green circle, directly below Item Carousel)
            var dashSprite = LoadSprite($"{PROCESSED_DIR}/UI_Dash.png");
            var dashGo = CreateButton(root, "DashButton", 0f, 430f, 120f, 120f, new Vector2(0.5f, 0f), dashSprite);
            AddGreenRing(dashGo, 130f);
            hud.dashButton = dashGo.GetComponent<Button>();

            // C. Jump Button (Green circle, directly below Dash Button, larger)
            var jumpSprite = LoadSprite($"{PROCESSED_DIR}/UI_Jump.png");
            var jumpGo = CreateButton(root, "JumpButton", 0f, 240f, 175f, 175f, new Vector2(0.5f, 0f), jumpSprite);
            AddGreenRing(jumpGo, 185f);
            hud.jumpButton = jumpGo.GetComponent<Button>();

            // D. Virtual Joy Stick (Bottom-Left)
            var stickRoot = new GameObject("FloatingVirtualStick", typeof(RectTransform), typeof(FloatingVirtualStick));
            stickRoot.layer = 5;
            stickRoot.transform.SetParent(root, false);
            var stickRt = stickRoot.GetComponent<RectTransform>();
            stickRt.anchorMin = new Vector2(0f, 0f); stickRt.anchorMax = new Vector2(0f, 0f); stickRt.pivot = new Vector2(0.5f, 0.5f);
            stickRt.anchoredPosition = new Vector2(240f, 260f);
            stickRt.sizeDelta = new Vector2(260f, 260f);

            var stickRingSprite = LoadSprite($"{TEXTURE_UI_DIR}/Mockup_JoystickRing.png");
            var stickHandleSprite = LoadSprite($"{TEXTURE_UI_DIR}/Mockup_JoystickHandle.png");

            var stickBgObj = CreateImage(stickRoot.transform, "StickBackground", 0f, 0f, 260f, 260f, new Vector2(0.5f, 0.5f), stickRingSprite, new Color(0.12f, 0.52f, 0.95f, 0.95f)); // Vibrant blue ring
            var stickHandleObj = CreateImage(stickRoot.transform, "StickHandle", 0f, 0f, 105f, 105f, new Vector2(0.5f, 0.5f), stickHandleSprite, new Color(0.26f, 0.65f, 1.0f, 1f)); // Blue handle

            var fvs = stickRoot.GetComponent<FloatingVirtualStick>();
            if (fvs != null)
            {
                fvs.BindStickVisuals(stickBgObj.GetComponent<RectTransform>(), stickHandleObj.GetComponent<RectTransform>(), 85f);
            }
            hud.virtualStick = fvs;

            // E. Z Target Button (Green circle, bottom-right)
            var zTargetSprite = LoadSprite($"{PROCESSED_DIR}/UI_ZTarget.png");
            var zTargetGo = CreateButton(root, "ZTargetButton", 260f, 450f, 105f, 105f, new Vector2(0.5f, 0f), zTargetSprite);
            AddGreenRing(zTargetGo, 115f);
            hud.zTargetButton = zTargetGo.GetComponent<Button>();

            // F. Swap Button (Green circle, right of and slightly lower than Z Target)
            var swapSprite = LoadSprite($"{PROCESSED_DIR}/UI_Swap.png");
            var swapGo = CreateButton(root, "SwapButton", 380f, 400f, 95f, 95f, new Vector2(0.5f, 0f), swapSprite);
            AddGreenRing(swapGo, 105f);
            hud.swapButton = swapGo.GetComponent<Button>();

            // Ensure QuickZTargetingController
            var zTargetController = root.gameObject.AddComponent<QuickZTargetingController>();
            zTargetController.BindReferences(
                hud.zTargetButton,
                null, // Do NOT pass image to avoid dark tinting!
                null,
                null, null, null,
                null, null, null,
                null, null, null
            );

            // G. Camera Control Area (Bottom-Right, touch pan zone with 4-way arrows)
            var camCrossSprite = LoadSprite($"{PROCESSED_DIR}/UI_CameraCrossArrows.png");
            var camZoneGo = new GameObject("CameraControlArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TouchCameraLookZone));
            camZoneGo.layer = 5;
            camZoneGo.transform.SetParent(root, false);
            var camRt = camZoneGo.GetComponent<RectTransform>();
            camRt.anchorMin = new Vector2(1f, 0f); camRt.anchorMax = new Vector2(1f, 0f); camRt.pivot = new Vector2(0.5f, 0.5f);
            camRt.anchoredPosition = new Vector2(-195f, 170f);
            camRt.sizeDelta = new Vector2(340f, 260f);

            var camZoneImg = camZoneGo.GetComponent<Image>();
            camZoneImg.color = new Color(0f, 0f, 0f, 0.002f); // Transparent raycast target
            camZoneImg.raycastTarget = true;

            // 4-Way Arrow Cross Display (Non-raycast)
            CreateImage(camZoneGo.transform, "CameraCrossArrows", 0f, 15f, 160f, 160f, new Vector2(0.5f, 0.5f), camCrossSprite, new Color(0.40f, 0.75f, 1.0f, 0.45f));
            CreateTMP(camZoneGo.transform, "CameraControlText", "<b>Camera Control</b>", 0f, -80f, 240f, 28f, 13, TextAlignmentOptions.Center, new Color(0.60f, 0.82f, 1.0f, 0.75f));

            hud.cameraLookZone = camZoneGo.GetComponent<TouchCameraLookZone>();
        }
    }
}

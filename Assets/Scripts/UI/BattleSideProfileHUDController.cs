using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Customization;

namespace BattleSummoner.UI
{
    /// <summary>
    /// BattleSideProfileHUDController manages the Persona/anime styled battle side-profile HUD:
    /// - Dynamic angled frame badge with gold rim and purple backdrop.
    /// - Masc and Feme side profile silhouettes with warm anime skin.
    /// - Real-time customizable asset layers: Hair, Clothing, Accessories, Eye Color, Face Expressions.
    /// - Slanted Cyan HP bar and Lime MP bar with live text telemetry.
    /// - Reactive facial expressions (e.g. Grit on taking damage, Smirk on kill/level up).
    /// </summary>
    public class BattleSideProfileHUDController : MonoBehaviour
    {
        public static BattleSideProfileHUDController Instance { get; private set; }

        [Header("Frame & Base Layer References")]
        [SerializeField] private Image frameBadgeImage;
        [SerializeField] private Image baseHeadImage;
        [SerializeField] private bool useDirectCleanPortrait = true;

        [Header("Clean Generated Portraits")]
        [SerializeField] private Sprite cleanPortraitMascSprite;
        [SerializeField] private Sprite cleanPortraitFemeSprite;

        [Header("Customizable Asset Layer Overlays")]
        [SerializeField] private Image hairOverlayImage;
        [SerializeField] private Image faceExpressionImage;
        [SerializeField] private Image eyeColorOverlayImage;
        [SerializeField] private Image clothingOverlayImage;
        [SerializeField] private Image accessoryOverlayImage;

        [Header("Slanted Battle Gauges (HP & MP)")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider mpSlider;
        [SerializeField] private RectTransform hpRect;
        [SerializeField] private RectTransform mpRect;
        [SerializeField] private TextMeshProUGUI hpValueTextTMP;
        [SerializeField] private TextMeshProUGUI mpValueTextTMP;
        [SerializeField] private RectTransform gaugeBezelRect;

        [Header("Subsystem References")]
        [SerializeField] private PlayerProgression playerProgression;

        [Header("Loaded Sprite Assets")]
        private Sprite frameBadgeSprite;
        private Sprite baseMascSprite;
        private Sprite baseFemeSprite;
        private Sprite hpCyanSprite;
        private Sprite mpLimeSprite;
        private Sprite dualBezelSprite;

        private readonly Dictionary<string, Sprite> assetSprites = new Dictionary<string, Sprite>();

        private Coroutine activeReactionRoutine;
        private Vector3 initialBadgePos;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            LoadSprites();
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            if (frameBadgeImage != null) initialBadgePos = frameBadgeImage.rectTransform.anchoredPosition;
            SyncWithCustomization();
            UpdateGauges();
        }

        private void OnEnable()
        {
            if (playerProgression != null)
            {
                playerProgression.OnHealthChanged += HandleHealthChanged;
                playerProgression.OnManaChanged += HandleManaChanged;
                playerProgression.OnLevelUpEvent += HandleLevelUp;
            }
        }

        private void OnDisable()
        {
            if (playerProgression != null)
            {
                playerProgression.OnHealthChanged -= HandleHealthChanged;
                playerProgression.OnManaChanged -= HandleManaChanged;
                playerProgression.OnLevelUpEvent -= HandleLevelUp;
            }
        }

        private void ResolveReferences()
        {
            if (playerProgression == null) playerProgression = FindAnyObjectByType<PlayerProgression>();
        }

        private void LoadSprites()
        {
            string basePath = "Textures/UI/BattleSideProfile/";
            cleanPortraitMascSprite = LoadSpriteFromAssets("Assets/Textures/UI/Portraits/Portrait_Player_Masc_Right_Cutout.png")
                ?? LoadSpriteFromAssets("Assets/Textures/UI/Portraits/Portrait_Player_Masc_Right.png")
                ?? LoadSpriteFromAssets("Assets/Textures/UI/Portraits/Profile_Male_Persona.jpg")
                ?? Resources.Load<Sprite>(basePath + "Template_BattlePortrait_Masc_Clean")
                ?? Resources.Load<Sprite>(basePath + "Template_BattlePortrait_Clean") 
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/Template_BattlePortrait_Masc_Clean.png")
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/Template_BattlePortrait_Clean.png");

            cleanPortraitFemeSprite = LoadSpriteFromAssets("Assets/Textures/UI/Portraits/Portrait_Player_Feme_Right_Cutout.png")
                ?? LoadSpriteFromAssets("Assets/Textures/UI/Portraits/Portrait_Player_Feme_Right.png")
                ?? LoadSpriteFromAssets("Assets/Textures/UI/Portraits/Profile_Female_Persona.jpg")
                ?? Resources.Load<Sprite>(basePath + "Template_BattlePortrait_Feme_Clean") 
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/Template_BattlePortrait_Feme_Clean.png");

            frameBadgeSprite = Resources.Load<Sprite>(basePath + "Frame_Badge_Masc") 
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/Frame_Badge_Masc.png");
            baseMascSprite = Resources.Load<Sprite>(basePath + "Base_Head_Masc") 
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/Base_Head_Masc.png");
            baseFemeSprite = Resources.Load<Sprite>(basePath + "Base_Head_Feme") 
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/Base_Head_Feme.png");
            hpCyanSprite = Resources.Load<Sprite>(basePath + "Gauge_HP_Cyan") 
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/Gauge_HP_Cyan.png");
            mpLimeSprite = Resources.Load<Sprite>(basePath + "Gauge_MP_Lime") 
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/Gauge_MP_Lime.png");
            dualBezelSprite = Resources.Load<Sprite>(basePath + "BattleGauge_Dual_HP_MP") 
                ?? LoadSpriteFromAssets("Assets/Textures/UI/BattleSideProfile/BattleGauge_Dual_HP_MP.png");

            // Customizable asset sprites
            string[] assetFiles = new string[]
            {
                "Hair_Spiky_Silver", "Hair_Shaggy_Brown", "Hair_Long_Black",
                "Face_Masc_Neutral", "Face_Feme_Parted", "Face_Battle_Grit", "Face_Smirk",
                "Eye_Side_Black", "Eye_Side_Crimson", "Eye_Side_Azure",
                "Clothing_Choker", "Clothing_SailorFuku", "Clothing_SuitBlazer",
                "Accessory_Headband", "Accessory_Glasses", "Accessory_Earrings"
            };

            foreach (var file in assetFiles)
            {
                var sp = LoadSpriteFromAssets($"Assets/Textures/UI/BattleSideProfile/{file}.png");
                if (sp != null) assetSprites[file] = sp;
            }
        }

        private Sprite LoadSpriteFromAssets(string path)
        {
#if UNITY_EDITOR
            var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
#endif
            return null;
        }

        /// <summary>
        /// Synchronizes the HUD side profile with the player's customized character data.
        /// </summary>
        public void SyncWithCustomization(CharacterCustomizationData data = null)
        {
            var modProfile = ModularPlayerAppearanceData.LoadLocalProfile();
            if (data == null)
            {
                if (CustomizablePlayerAvatar.Instance != null && CustomizablePlayerAvatar.Instance.CurrentData != null)
                {
                    data = CustomizablePlayerAvatar.Instance.CurrentData;
                }
                else
                {
                    data = CharacterCustomizationData.LoadFromPrefs();
                }
            }

            bool isFemale = modProfile != null ? (modProfile.gender == 1) : (data.sex == SexOption.Female || data.sex == SexOption.NonBinary);

            if (useDirectCleanPortrait)
            {
                var cleanSprite = isFemale ? (cleanPortraitFemeSprite ?? cleanPortraitMascSprite) : (cleanPortraitMascSprite ?? cleanPortraitFemeSprite);
                if (frameBadgeImage != null)
                {
                    frameBadgeImage.sprite = cleanSprite;
                    frameBadgeImage.enabled = (cleanSprite != null);
                    frameBadgeImage.preserveAspect = true;
                }
                if (baseHeadImage != null) baseHeadImage.gameObject.SetActive(false);
                if (hairOverlayImage != null) hairOverlayImage.gameObject.SetActive(false);
                if (faceExpressionImage != null) faceExpressionImage.gameObject.SetActive(false);
                if (eyeColorOverlayImage != null) eyeColorOverlayImage.gameObject.SetActive(false);
                if (clothingOverlayImage != null) clothingOverlayImage.gameObject.SetActive(false);
                if (accessoryOverlayImage != null) accessoryOverlayImage.gameObject.SetActive(false);

                // Calibrate gauge slots exactly to the active portrait template
                if (hpRect != null && mpRect != null)
                {
                    if (isFemale)
                    {
                        hpRect.anchorMin = new Vector2(0.150f, 0.145f);
                        hpRect.anchorMax = new Vector2(0.850f, 0.260f);
                        hpRect.localEulerAngles = new Vector3(0, 0, 5.8f);

                        mpRect.anchorMin = new Vector2(0.130f, 0.045f);
                        mpRect.anchorMax = new Vector2(0.840f, 0.135f);
                        mpRect.localEulerAngles = new Vector3(0, 0, 5.8f);
                    }
                    else
                    {
                        hpRect.anchorMin = new Vector2(0.170f, 0.160f);
                        hpRect.anchorMax = new Vector2(0.920f, 0.300f);
                        hpRect.localEulerAngles = new Vector3(0, 0, 10.5f);

                        mpRect.anchorMin = new Vector2(0.170f, 0.025f);
                        mpRect.anchorMax = new Vector2(0.900f, 0.135f);
                        mpRect.localEulerAngles = new Vector3(0, 0, 10.5f);
                    }
                }
            }
            else
            {
                // 1. Base Head & Frame Badge (Modular Mode)
                if (baseHeadImage != null)
                {
                    baseHeadImage.gameObject.SetActive(true);
                    baseHeadImage.sprite = isFemale ? baseFemeSprite : baseMascSprite;
                    baseHeadImage.enabled = (baseHeadImage.sprite != null);
                    if (modProfile != null) baseHeadImage.color = modProfile.skinColor;
                }

                if (frameBadgeImage != null)
                {
                    frameBadgeImage.sprite = frameBadgeSprite;
                    frameBadgeImage.enabled = (frameBadgeSprite != null);
                    frameBadgeImage.preserveAspect = true;
                }
            }

            if (playerNameTMP != null && modProfile != null && !string.IsNullOrEmpty(modProfile.playerName))
            {
                playerNameTMP.text = $"<b>{modProfile.playerName}</b>";
            }
        }

        public void SetFaceExpression(string expressionKey)
        {
            if (faceExpressionImage != null && assetSprites.TryGetValue(expressionKey, out var sp))
            {
                faceExpressionImage.sprite = sp;
                faceExpressionImage.enabled = true;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            UpdateGauges();

            // Reaction to taking damage: grit teeth, shake badge
            if (current < max * 0.98f)
            {
                TriggerDamageReaction();
            }
        }

        private void HandleManaChanged(float current, float max)
        {
            UpdateGauges();
        }

        private void HandleLevelUp(int newLevel)
        {
            SetFaceExpression("Face_Smirk");
            if (activeReactionRoutine != null) StopCoroutine(activeReactionRoutine);
            activeReactionRoutine = StartCoroutine(ResetExpressionAfterDelay(3.5f));
        }

        private void TriggerDamageReaction()
        {
            SetFaceExpression("Face_Battle_Grit");

            if (activeReactionRoutine != null) StopCoroutine(activeReactionRoutine);
            activeReactionRoutine = StartCoroutine(DamageShakeRoutine());
        }

        private IEnumerator DamageShakeRoutine()
        {
            if (frameBadgeImage != null)
            {
                float duration = 0.25f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float offset = (1f - (elapsed / duration)) * 6f;
                    float rx = UnityEngine.Random.Range(-offset, offset);
                    float ry = UnityEngine.Random.Range(-offset, offset);
                    frameBadgeImage.rectTransform.anchoredPosition = initialBadgePos + new Vector3(rx, ry, 0);
                    yield return null;
                }
                frameBadgeImage.rectTransform.anchoredPosition = initialBadgePos;
            }

            yield return new WaitForSeconds(1.8f);

            // Return to neutral expression
            bool isFemale = true;
            if (CustomizablePlayerAvatar.Instance != null && CustomizablePlayerAvatar.Instance.CurrentData != null)
            {
                isFemale = CustomizablePlayerAvatar.Instance.CurrentData.sex == SexOption.Female;
            }
            SetFaceExpression(isFemale ? "Face_Feme_Parted" : "Face_Masc_Neutral");
            activeReactionRoutine = null;
        }

        private IEnumerator ResetExpressionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            bool isFemale = true;
            if (CustomizablePlayerAvatar.Instance != null && CustomizablePlayerAvatar.Instance.CurrentData != null)
            {
                isFemale = CustomizablePlayerAvatar.Instance.CurrentData.sex == SexOption.Female;
            }
            SetFaceExpression(isFemale ? "Face_Feme_Parted" : "Face_Masc_Neutral");
            activeReactionRoutine = null;
        }

        public void UpdateGauges()
        {
            if (playerProgression == null) ResolveReferences();
            if (playerProgression == null) return;

            float hpPct = playerProgression.currentHp / Mathf.Max(1f, playerProgression.maxHp);
            float mpPct = playerProgression.currentMp / Mathf.Max(1f, playerProgression.maxMp);

            if (hpSlider != null) hpSlider.value = hpPct;
            if (mpSlider != null) mpSlider.value = mpPct;

            if (hpValueTextTMP != null)
            {
                hpValueTextTMP.text = $"<color=#00E5FF><b>HP</b></color> {Mathf.RoundToInt(playerProgression.currentHp)}/{Mathf.RoundToInt(playerProgression.maxHp)}";
            }

            if (mpValueTextTMP != null)
            {
                mpValueTextTMP.text = $"<color=#76FF03><b>MP</b></color> {Mathf.RoundToInt(playerProgression.currentMp)}/{Mathf.RoundToInt(playerProgression.maxMp)}";
            }

            if (playerLevelTMP != null)
            {
                playerLevelTMP.text = $"<b>Lv. {playerProgression.currentLevel}</b>";
            }

            if (playerNameTMP != null)
            {
                playerNameTMP.text = $"<b>{(!string.IsNullOrEmpty(playerProgression.playerName) ? playerProgression.playerName : "HERO")}</b>";
            }
        }

        /// <summary>
        /// Ensures a BattleSideProfileHUD exists in the scene hierarchy.
        /// </summary>
        public static BattleSideProfileHUDController EnsureInScene(Transform parentTransform = null, bool forceRebuild = false)
        {
            var existingList = FindObjectsByType<BattleSideProfileHUDController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (forceRebuild)
            {
                foreach (var ex in existingList)
                {
                    if (ex != null) DestroyImmediate(ex.gameObject);
                }
                Instance = null;
            }
            else if (existingList != null && existingList.Length > 0)
            {
                for (int i = 1; i < existingList.Length; i++)
                {
                    if (existingList[i] != null) DestroyImmediate(existingList[i].gameObject);
                }
                if (existingList[0] != null)
                {
                    existingList[0].gameObject.SetActive(true);
                    Instance = existingList[0];
                    return Instance;
                }
            }
            else if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
                return Instance;
            }

            if (parentTransform == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    parentTransform = canvas.transform;
                }
                else
                {
                    GameObject canvasGo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    var c = canvasGo.GetComponent<Canvas>();
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    parentTransform = canvasGo.transform;
                }
            }

            GameObject rootObj = new GameObject("BattleSideProfile_HUD", typeof(RectTransform));
            if (parentTransform != null) rootObj.transform.SetParent(parentTransform, false);

            var controller = rootObj.AddComponent<BattleSideProfileHUDController>();
            controller.BuildProceduralHierarchy();
            Instance = controller;
            return controller;
        }

        [Header("Player Metadata")]
        [SerializeField] private TextMeshProUGUI playerNameTMP;
        [SerializeField] private TextMeshProUGUI playerLevelTMP;

        private void BuildProceduralHierarchy()
        {
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            // Anchor to TOP-RIGHT of screen (as specified in UI references)
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-15f, -10f);
            rootRect.sizeDelta = new Vector2(250f, 250f);

            // 1. Frame Badge Root
            GameObject badgeObj = new GameObject("Frame_Badge", typeof(RectTransform));
            badgeObj.transform.SetParent(transform, false);
            RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
            badgeRect.anchorMin = Vector2.zero;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = Vector2.zero;
            frameBadgeImage = badgeObj.AddComponent<Image>();
            frameBadgeImage.sprite = cleanPortraitMascSprite ?? frameBadgeSprite;
            frameBadgeImage.raycastTarget = false;
            frameBadgeImage.preserveAspect = true;

            // 1b. Player Level Badge
            GameObject lvlObj = new GameObject("Player_Level_Badge", typeof(RectTransform), typeof(Image));
            lvlObj.transform.SetParent(badgeObj.transform, false);
            RectTransform lvlRect = lvlObj.GetComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(0.04f, 0.85f);
            lvlRect.anchorMax = new Vector2(0.32f, 0.98f);
            lvlRect.offsetMin = Vector2.zero;
            lvlRect.offsetMax = Vector2.zero;
            Image lvlBg = lvlObj.GetComponent<Image>();
            lvlBg.color = new Color(0.08f, 0.06f, 0.16f, 0.75f);

            GameObject lvlTxtObj = new GameObject("Text_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            lvlTxtObj.transform.SetParent(lvlObj.transform, false);
            RectTransform lvlTxtRt = lvlTxtObj.GetComponent<RectTransform>();
            lvlTxtRt.anchorMin = Vector2.zero;
            lvlTxtRt.anchorMax = Vector2.one;
            lvlTxtRt.offsetMin = Vector2.zero;
            lvlTxtRt.offsetMax = Vector2.zero;
            playerLevelTMP = lvlTxtObj.GetComponent<TextMeshProUGUI>();
            playerLevelTMP.text = "<b>Lv. 1</b>";
            playerLevelTMP.fontSize = 12f;
            playerLevelTMP.alignment = TextAlignmentOptions.Center;
            playerLevelTMP.color = new Color(1f, 0.85f, 0.35f);

            // 1c. Player Name Label
            GameObject nameObj = new GameObject("Player_Name_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(badgeObj.transform, false);
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.34f, 0.85f);
            nameRect.anchorMax = new Vector2(0.96f, 0.98f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            playerNameTMP = nameObj.GetComponent<TextMeshProUGUI>();
            playerNameTMP.text = "<b>HERO</b>";
            playerNameTMP.fontSize = 13f;
            playerNameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            playerNameTMP.color = Color.white;

            // 2. Base Head Image
            GameObject headObj = new GameObject("Base_Head", typeof(RectTransform));
            headObj.transform.SetParent(badgeObj.transform, false);
            RectTransform headRect = headObj.GetComponent<RectTransform>();
            headRect.anchorMin = new Vector2(0.08f, 0.08f);
            headRect.anchorMax = new Vector2(0.92f, 0.92f);
            headRect.anchoredPosition = Vector2.zero;
            headRect.sizeDelta = Vector2.zero;
            baseHeadImage = headObj.AddComponent<Image>();
            baseHeadImage.sprite = baseFemeSprite;
            baseHeadImage.raycastTarget = false;
            baseHeadImage.preserveAspect = true;
            headObj.SetActive(!useDirectCleanPortrait);

            // 3. Asset Overlays
            clothingOverlayImage = CreateOverlay(badgeObj.transform, "Overlay_Clothing");
            hairOverlayImage = CreateOverlay(badgeObj.transform, "Overlay_Hair");
            faceExpressionImage = CreateOverlay(badgeObj.transform, "Overlay_Face");
            eyeColorOverlayImage = CreateOverlay(badgeObj.transform, "Overlay_EyeColor");
            accessoryOverlayImage = CreateOverlay(badgeObj.transform, "Overlay_Accessory");

            // 4. Slanted Dual Gauge Container (HP Cyan + MP Lime)
            GameObject gaugeRoot = new GameObject("Dual_BattleGauges", typeof(RectTransform));
            gaugeRoot.transform.SetParent(transform, false);
            gaugeBezelRect = gaugeRoot.GetComponent<RectTransform>();
            gaugeBezelRect.anchorMin = Vector2.zero;
            gaugeBezelRect.anchorMax = Vector2.one;
            gaugeBezelRect.anchoredPosition = Vector2.zero;
            gaugeBezelRect.sizeDelta = Vector2.zero;

            // HP Gauge (Cyan)
            GameObject hpObj = new GameObject("Gauge_HP", typeof(RectTransform));
            hpObj.transform.SetParent(gaugeRoot.transform, false);
            hpRect = hpObj.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.150f, 0.145f);
            hpRect.anchorMax = new Vector2(0.850f, 0.260f);
            hpRect.anchoredPosition = Vector2.zero;
            hpRect.sizeDelta = Vector2.zero;
            hpRect.localEulerAngles = new Vector3(0, 0, 5.8f);

            hpSlider = hpObj.AddComponent<Slider>();
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = 1f;

            Image hpBg = hpObj.AddComponent<Image>();
            hpBg.color = Color.clear;

            GameObject hpFill = new GameObject("Fill", typeof(RectTransform));
            hpFill.transform.SetParent(hpObj.transform, false);
            RectTransform hpFillRect = hpFill.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.sizeDelta = Vector2.zero;
            Image hpFillImg = hpFill.AddComponent<Image>();
            hpFillImg.sprite = hpCyanSprite;
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.color = new Color(0f, 0.90f, 1f, 0.95f); // Vibrant Persona Cyan
            hpSlider.fillRect = hpFillRect;

            GameObject hpTextObj = new GameObject("Text_HP_TMP", typeof(RectTransform));
            hpTextObj.transform.SetParent(hpObj.transform, false);
            RectTransform hpTextRect = hpTextObj.GetComponent<RectTransform>();
            hpTextRect.anchorMin = Vector2.zero;
            hpTextRect.anchorMax = Vector2.one;
            hpTextRect.anchoredPosition = new Vector2(8f, 0f);
            hpTextRect.sizeDelta = Vector2.zero;
            hpValueTextTMP = hpTextObj.AddComponent<TextMeshProUGUI>();
            hpValueTextTMP.fontSize = 12f;
            hpValueTextTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
            hpValueTextTMP.alignment = TextAlignmentOptions.MidlineLeft;
            hpValueTextTMP.color = Color.white;

            // MP Gauge (Lime)
            GameObject mpObj = new GameObject("Gauge_MP", typeof(RectTransform));
            mpObj.transform.SetParent(gaugeRoot.transform, false);
            mpRect = mpObj.GetComponent<RectTransform>();
            mpRect.anchorMin = new Vector2(0.130f, 0.045f);
            mpRect.anchorMax = new Vector2(0.840f, 0.135f);
            mpRect.anchoredPosition = Vector2.zero;
            mpRect.sizeDelta = Vector2.zero;
            mpRect.localEulerAngles = new Vector3(0, 0, 5.8f);

            mpSlider = mpObj.AddComponent<Slider>();
            mpSlider.minValue = 0f;
            mpSlider.maxValue = 1f;
            mpSlider.value = 1f;

            Image mpBg = mpObj.AddComponent<Image>();
            mpBg.color = Color.clear;

            GameObject mpFill = new GameObject("Fill", typeof(RectTransform));
            mpFill.transform.SetParent(mpObj.transform, false);
            RectTransform mpFillRect = mpFill.GetComponent<RectTransform>();
            mpFillRect.anchorMin = Vector2.zero;
            mpFillRect.anchorMax = Vector2.one;
            mpFillRect.sizeDelta = Vector2.zero;
            Image mpFillImg = mpFill.AddComponent<Image>();
            mpFillImg.sprite = mpLimeSprite;
            mpFillImg.type = Image.Type.Filled;
            mpFillImg.fillMethod = Image.FillMethod.Horizontal;
            mpFillImg.color = new Color(0.46f, 1f, 0.01f, 0.95f); // Persona Neon Lime
            mpSlider.fillRect = mpFillRect;

            GameObject mpTextObj = new GameObject("Text_MP_TMP", typeof(RectTransform));
            mpTextObj.transform.SetParent(mpObj.transform, false);
            RectTransform mpTextRect = mpTextObj.GetComponent<RectTransform>();
            mpTextRect.anchorMin = Vector2.zero;
            mpTextRect.anchorMax = Vector2.one;
            mpTextRect.anchoredPosition = new Vector2(8f, 0f);
            mpTextRect.sizeDelta = Vector2.zero;
            mpValueTextTMP = mpTextObj.AddComponent<TextMeshProUGUI>();
            mpValueTextTMP.fontSize = 12f;
            mpValueTextTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
            mpValueTextTMP.alignment = TextAlignmentOptions.MidlineLeft;
            mpValueTextTMP.color = Color.white;

            LoadSprites();
            SyncWithCustomization();
            UpdateGauges();
        }

        private Image CreateOverlay(Transform parent, string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.08f);
            rt.anchorMax = new Vector2(0.92f, 0.92f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            Image img = obj.AddComponent<Image>();
            img.enabled = false;
            img.raycastTarget = false;
            return img;
        }
    }
}

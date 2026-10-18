using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.UI
{
    public enum BattlePortraitGender
    {
        Masc,
        Feme
    }

    public enum BattlePortraitHair
    {
        BaldBase,
        SpikySilver,
        ShaggyBrown,
        LongBlack
    }

    public enum BattlePortraitFace
    {
        DefaultProfile,
        MascNeutral,
        FemeParted,
        BattleGritSweat,
        ConfidentSmirk
    }

    public enum BattlePortraitEyeColor
    {
        BlackGrey,
        CrimsonRed,
        AzureBlue,
        AmberGold,
        EmeraldGreen,
        OccultViolet
    }

    public enum BattlePortraitClothing
    {
        None,
        LeatherChoker,
        SailorFuku,
        FormalSuitBlazer
    }

    public enum BattlePortraitAccessory
    {
        None,
        Headband,
        RectangularGlasses,
        DropEarrings
    }

    /// <summary>
    /// BattleSideProfilePortrait renders the authentic Persona-styled side-profile battle HUD portrait:
    /// - Angular tilted purple badge with vibrant gold borders.
    /// - Male and Female base silhouettes facing left.
    /// - Modular layers for Hair, Face/Nose/Mouth, Eye Color, Clothing, and Accessories.
    /// - Skewed Cyan HP bar ("HP 100/100") and Lime Green MP bar ("MP 50/50") with gold trim.
    /// - Interactive damage flash, low-health pulse, and real-time telemetry binding.
    /// </summary>
    [ExecuteAlways]
    public class BattleSideProfilePortrait : MonoBehaviour
    {
        public static BattleSideProfilePortrait Instance { get; private set; }

        [Header("Template Fallback / Direct Mode")]
        [Tooltip("When true, displays the pixel-perfect reference sheet template directly with live text/bar overlay")]
        [SerializeField] private bool useTemplateDirectMode = true;
        [SerializeField] private Image templateDirectImage;
        [SerializeField] private Sprite templateMascSprite;
        [SerializeField] private Sprite templateFemeSprite;
        [SerializeField] private Sprite cleanTemplateMascSprite;
        [SerializeField] private Sprite cleanTemplateFemeSprite;

        [Header("Modular Layer Images")]
        [SerializeField] private GameObject modularLayersRoot;
        [SerializeField] private Image frameBackingImage;
        [SerializeField] private Image baseHeadImage;
        [SerializeField] private Image eyeImage;
        [SerializeField] private Image faceExpressionImage;
        [SerializeField] private Image clothingImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image accessoryImage;
        [SerializeField] private Image frameGoldBorderImage;

        [Header("Vitals Gauges (Skewed HP & MP Bars)")]
        [SerializeField] private RectTransform vitalsContainer;
        [SerializeField] private Image dualGaugeBackground;
        [SerializeField] private Slider hpBarSlider;
        [SerializeField] private Image hpBarFillImage;
        [SerializeField] private TextMeshProUGUI hpValueText;
        [SerializeField] private Slider mpBarSlider;
        [SerializeField] private Image mpBarFillImage;
        [SerializeField] private TextMeshProUGUI mpValueText;
        [SerializeField] private TextMeshProUGUI characterNameText;

        [Header("Active Configuration")]
        [SerializeField] private BattlePortraitGender activeGender = BattlePortraitGender.Masc;
        [SerializeField] private BattlePortraitHair activeHair = BattlePortraitHair.BaldBase;
        [SerializeField] private BattlePortraitFace activeFace = BattlePortraitFace.DefaultProfile;
        [SerializeField] private BattlePortraitEyeColor activeEyeColor = BattlePortraitEyeColor.BlackGrey;
        [SerializeField] private BattlePortraitClothing activeClothing = BattlePortraitClothing.None;
        [SerializeField] private BattlePortraitAccessory activeAccessory = BattlePortraitAccessory.None;

        [Header("Current Vitals State")]
        [SerializeField] private float currentHp = 100f;
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float currentMp = 50f;
        [SerializeField] private float maxMp = 50f;

        [Header("Sprite Library References")]
        [SerializeField] private Sprite sprFrameMasc;
        [SerializeField] private Sprite sprFrameFeme;
        [SerializeField] private Sprite sprHairSpiky;
        [SerializeField] private Sprite sprHairShaggy;
        [SerializeField] private Sprite sprHairLong;
        [SerializeField] private Sprite sprFaceMascNeutral;
        [SerializeField] private Sprite sprFaceFemeParted;
        [SerializeField] private Sprite sprFaceBattleGrit;
        [SerializeField] private Sprite sprFaceSmirk;
        [SerializeField] private Sprite sprEyeBlack;
        [SerializeField] private Sprite sprEyeCrimson;
        [SerializeField] private Sprite sprEyeAzure;
        [SerializeField] private Sprite sprClothingChoker;
        [SerializeField] private Sprite sprClothingSailor;
        [SerializeField] private Sprite sprClothingBlazer;
        [SerializeField] private Sprite sprAccHeadband;
        [SerializeField] private Sprite sprAccGlasses;
        [SerializeField] private Sprite sprAccEarrings;
        [SerializeField] private Sprite sprDualGauge;

        public BattlePortraitGender ActiveGender => activeGender;
        public BattlePortraitHair ActiveHair => activeHair;
        public BattlePortraitFace ActiveFace => activeFace;
        public BattlePortraitEyeColor ActiveEyeColor => activeEyeColor;
        public BattlePortraitClothing ActiveClothing => activeClothing;
        public BattlePortraitAccessory ActiveAccessory => activeAccessory;

        private Coroutine damageFlashCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            LoadSpriteLibrary();
        }

        private void Start()
        {
            RefreshDisplay();
            UpdateVitalsDisplay();
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            LoadSpriteLibrary();
            RefreshDisplay();
            UpdateVitalsDisplay();
        }

        public void LoadSpriteLibrary()
        {
            if (templateMascSprite == null)
                templateMascSprite = Resources.Load<Sprite>("UI/BattleSideProfile/Template_BattlePortrait_Masc") ?? LoadSprite("Assets/Textures/UI/BattleSideProfile/Template_BattlePortrait_Masc.png");
            if (templateFemeSprite == null)
                templateFemeSprite = Resources.Load<Sprite>("UI/BattleSideProfile/Template_BattlePortrait_Feme") ?? LoadSprite("Assets/Textures/UI/BattleSideProfile/Template_BattlePortrait_Feme.png");

            if (cleanTemplateMascSprite == null)
                cleanTemplateMascSprite = LoadSprite("Assets/Textures/UI/Portraits/Portrait_Player_Masc_Right_Cutout.png")
                    ?? LoadSprite("Assets/Textures/UI/Portraits/Portrait_Player_Masc_Right.png")
                    ?? Resources.Load<Sprite>("UI/BattleSideProfile/Template_BattlePortrait_Masc_CleanGauge")
                    ?? LoadSprite("Assets/Textures/UI/BattleSideProfile/Template_BattlePortrait_Masc_CleanGauge.png");
            if (cleanTemplateFemeSprite == null)
                cleanTemplateFemeSprite = LoadSprite("Assets/Textures/UI/Portraits/Portrait_Player_Feme_Right_Cutout.png")
                    ?? LoadSprite("Assets/Textures/UI/Portraits/Portrait_Player_Feme_Right.png")
                    ?? Resources.Load<Sprite>("UI/BattleSideProfile/Template_BattlePortrait_Feme_CleanGauge")
                    ?? LoadSprite("Assets/Textures/UI/BattleSideProfile/Template_BattlePortrait_Feme_CleanGauge.png");

            if (sprFrameMasc == null) sprFrameMasc = LoadSprite("Assets/Textures/UI/BattleSideProfile/Frame_Badge_Masc.png");
            if (sprFrameFeme == null) sprFrameFeme = LoadSprite("Assets/Textures/UI/BattleSideProfile/Frame_Badge_Feme.png");

            if (sprHairSpiky == null) sprHairSpiky = LoadSprite("Assets/Textures/UI/BattleSideProfile/Hair_Spiky_Silver.png");
            if (sprHairShaggy == null) sprHairShaggy = LoadSprite("Assets/Textures/UI/BattleSideProfile/Hair_Shaggy_Brown.png");
            if (sprHairLong == null) sprHairLong = LoadSprite("Assets/Textures/UI/BattleSideProfile/Hair_Long_Black.png");

            if (sprFaceMascNeutral == null) sprFaceMascNeutral = LoadSprite("Assets/Textures/UI/BattleSideProfile/Face_Masc_Neutral.png");
            if (sprFaceFemeParted == null) sprFaceFemeParted = LoadSprite("Assets/Textures/UI/BattleSideProfile/Face_Feme_Parted.png");
            if (sprFaceBattleGrit == null) sprFaceBattleGrit = LoadSprite("Assets/Textures/UI/BattleSideProfile/Face_Battle_Grit.png");
            if (sprFaceSmirk == null) sprFaceSmirk = LoadSprite("Assets/Textures/UI/BattleSideProfile/Face_Smirk.png");

            if (sprEyeBlack == null) sprEyeBlack = LoadSprite("Assets/Textures/UI/BattleSideProfile/Eye_Side_Black.png");
            if (sprEyeCrimson == null) sprEyeCrimson = LoadSprite("Assets/Textures/UI/BattleSideProfile/Eye_Side_Crimson.png");
            if (sprEyeAzure == null) sprEyeAzure = LoadSprite("Assets/Textures/UI/BattleSideProfile/Eye_Side_Azure.png");

            if (sprClothingChoker == null) sprClothingChoker = LoadSprite("Assets/Textures/UI/BattleSideProfile/Clothing_Choker.png");
            if (sprClothingSailor == null) sprClothingSailor = LoadSprite("Assets/Textures/UI/BattleSideProfile/Clothing_SailorFuku.png");
            if (sprClothingBlazer == null) sprClothingBlazer = LoadSprite("Assets/Textures/UI/BattleSideProfile/Clothing_SuitBlazer.png");

            if (sprAccHeadband == null) sprAccHeadband = LoadSprite("Assets/Textures/UI/BattleSideProfile/Accessory_Headband.png");
            if (sprAccGlasses == null) sprAccGlasses = LoadSprite("Assets/Textures/UI/BattleSideProfile/Accessory_Glasses.png");
            if (sprAccEarrings == null) sprAccEarrings = LoadSprite("Assets/Textures/UI/BattleSideProfile/Accessory_Earrings.png");

            if (sprDualGauge == null) sprDualGauge = LoadSprite("Assets/Textures/UI/BattleSideProfile/BattleGauge_Dual_HP_MP.png");
        }

        private Sprite LoadSprite(string assetPath)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }

        public void SetGender(BattlePortraitGender gender)
        {
            activeGender = gender;
            RefreshDisplay();
        }

        public void SetHair(BattlePortraitHair hair)
        {
            activeHair = hair;
            // If selecting a customized asset, switch to modular mode so user sees layer changes
            if (hair != BattlePortraitHair.BaldBase) useTemplateDirectMode = false;
            RefreshDisplay();
        }

        public void SetFace(BattlePortraitFace face)
        {
            activeFace = face;
            if (face != BattlePortraitFace.DefaultProfile) useTemplateDirectMode = false;
            RefreshDisplay();
        }

        public void SetEyeColor(BattlePortraitEyeColor eyeColor)
        {
            activeEyeColor = eyeColor;
            RefreshDisplay();
        }

        public void SetClothing(BattlePortraitClothing clothing)
        {
            activeClothing = clothing;
            if (clothing != BattlePortraitClothing.None) useTemplateDirectMode = false;
            RefreshDisplay();
        }

        public void SetAccessory(BattlePortraitAccessory accessory)
        {
            activeAccessory = accessory;
            if (accessory != BattlePortraitAccessory.None) useTemplateDirectMode = false;
            RefreshDisplay();
        }

        public void SetUseTemplateDirectMode(bool direct)
        {
            useTemplateDirectMode = direct;
            RefreshDisplay();
        }

        public void SetVitals(float hp, float maxH, float mp, float maxM)
        {
            bool tookDamage = hp < currentHp;
            currentHp = Mathf.Clamp(hp, 0f, maxH);
            maxHp = Mathf.Max(1f, maxH);
            currentMp = Mathf.Clamp(mp, 0f, maxM);
            maxMp = Mathf.Max(1f, maxM);

            UpdateVitalsDisplay();

            if (tookDamage && Application.isPlaying)
            {
                TriggerDamageFlash();
            }
        }

        public void RefreshDisplay()
        {
            LoadSpriteLibrary();

            if (vitalsContainer == null)
            {
                var vt = transform.Find("VitalsContainer");
                if (vt != null) vitalsContainer = vt as RectTransform;
            }

            // 1. Template Direct Mode
            if (useTemplateDirectMode)
            {
                if (templateDirectImage != null)
                {
                    templateDirectImage.gameObject.SetActive(true);
                    templateDirectImage.sprite = (activeGender == BattlePortraitGender.Masc)
                        ? templateMascSprite
                        : templateFemeSprite;
                    templateDirectImage.preserveAspect = true;
                }

                if (modularLayersRoot != null)
                {
                    modularLayersRoot.SetActive(false);
                }

                if (vitalsContainer != null)
                {
                    vitalsContainer.gameObject.SetActive(false);
                }
            }
            else
            {
                if (templateDirectImage != null)
                {
                    templateDirectImage.gameObject.SetActive(false);
                }

                if (modularLayersRoot != null)
                {
                    modularLayersRoot.SetActive(true);
                }

                if (vitalsContainer != null)
                {
                    vitalsContainer.gameObject.SetActive(true);
                }

                // 2. Modular Layer Composition
                // Base Frame + Silhouette (with clean gauge background)
                if (frameBackingImage != null)
                {
                    frameBackingImage.gameObject.SetActive(true);
                    frameBackingImage.sprite = (activeGender == BattlePortraitGender.Masc) 
                        ? (cleanTemplateMascSprite ?? templateMascSprite) 
                        : (cleanTemplateFemeSprite ?? templateFemeSprite);
                    frameBackingImage.preserveAspect = true;
                }

                // Prevent rendering bare white rect if base head sprite is unassigned
                if (baseHeadImage != null)
                {
                    baseHeadImage.gameObject.SetActive(false);
                }

                // Hair Layer
                if (hairImage != null)
                {
                    RectTransform hRt = hairImage.rectTransform;
                    hRt.anchorMin = new Vector2(0.5f, 0.5f);
                    hRt.anchorMax = new Vector2(0.5f, 0.5f);
                    hRt.pivot = new Vector2(0.5f, 0.5f);

                    switch (activeHair)
                    {
                        case BattlePortraitHair.SpikySilver:
                            hairImage.gameObject.SetActive(true);
                            hairImage.sprite = sprHairSpiky;
                            hRt.anchoredPosition = new Vector2(-88f, 68f);
                            hRt.sizeDelta = new Vector2(165f, 175f);
                            break;
                        case BattlePortraitHair.ShaggyBrown:
                            hairImage.gameObject.SetActive(true);
                            hairImage.sprite = sprHairShaggy;
                            hRt.anchoredPosition = new Vector2(-82f, 62f);
                            hRt.sizeDelta = new Vector2(175f, 175f);
                            break;
                        case BattlePortraitHair.LongBlack:
                            hairImage.gameObject.SetActive(true);
                            hairImage.sprite = sprHairLong;
                            hRt.anchoredPosition = new Vector2(-75f, 38f);
                            hRt.sizeDelta = new Vector2(185f, 185f);
                            break;
                        case BattlePortraitHair.BaldBase:
                        default:
                            hairImage.gameObject.SetActive(false);
                            break;
                    }
                    hairImage.preserveAspect = true;
                }

                // Face / Expression Layer
                if (faceExpressionImage != null)
                {
                    RectTransform fRt = faceExpressionImage.rectTransform;
                    fRt.anchorMin = new Vector2(0.5f, 0.5f);
                    fRt.anchorMax = new Vector2(0.5f, 0.5f);
                    fRt.pivot = new Vector2(0.5f, 0.5f);
                    fRt.anchoredPosition = new Vector2(-135f, 25f);
                    fRt.sizeDelta = new Vector2(65f, 65f);

                    switch (activeFace)
                    {
                        case BattlePortraitFace.MascNeutral:
                            faceExpressionImage.gameObject.SetActive(true);
                            faceExpressionImage.sprite = sprFaceMascNeutral;
                            break;
                        case BattlePortraitFace.FemeParted:
                            faceExpressionImage.gameObject.SetActive(true);
                            faceExpressionImage.sprite = sprFaceFemeParted;
                            break;
                        case BattlePortraitFace.BattleGritSweat:
                            faceExpressionImage.gameObject.SetActive(true);
                            faceExpressionImage.sprite = sprFaceBattleGrit;
                            break;
                        case BattlePortraitFace.ConfidentSmirk:
                            faceExpressionImage.gameObject.SetActive(true);
                            faceExpressionImage.sprite = sprFaceSmirk;
                            break;
                        case BattlePortraitFace.DefaultProfile:
                        default:
                            faceExpressionImage.gameObject.SetActive(false);
                            break;
                    }
                    faceExpressionImage.preserveAspect = true;
                }

                // Eye Layer
                if (eyeImage != null)
                {
                    RectTransform eRt = eyeImage.rectTransform;
                    eRt.anchorMin = new Vector2(0.5f, 0.5f);
                    eRt.anchorMax = new Vector2(0.5f, 0.5f);
                    eRt.pivot = new Vector2(0.5f, 0.5f);
                    eRt.anchoredPosition = new Vector2(-114.5f, 51.5f);
                    eRt.sizeDelta = new Vector2(38f, 28f);

                    eyeImage.gameObject.SetActive(true);
                    switch (activeEyeColor)
                    {
                        case BattlePortraitEyeColor.BlackGrey:
                            eyeImage.sprite = sprEyeBlack;
                            eyeImage.color = Color.white;
                            break;
                        case BattlePortraitEyeColor.CrimsonRed:
                            eyeImage.sprite = sprEyeCrimson;
                            eyeImage.color = Color.white;
                            break;
                        case BattlePortraitEyeColor.AzureBlue:
                            eyeImage.sprite = sprEyeAzure;
                            eyeImage.color = Color.white;
                            break;
                        case BattlePortraitEyeColor.AmberGold:
                            eyeImage.sprite = sprEyeBlack;
                            eyeImage.color = new Color(1f, 0.85f, 0.2f);
                            break;
                        case BattlePortraitEyeColor.EmeraldGreen:
                            eyeImage.sprite = sprEyeBlack;
                            eyeImage.color = new Color(0.2f, 0.9f, 0.4f);
                            break;
                        case BattlePortraitEyeColor.OccultViolet:
                            eyeImage.sprite = sprEyeBlack;
                            eyeImage.color = new Color(0.7f, 0.3f, 0.9f);
                            break;
                    }
                    eyeImage.preserveAspect = true;
                }

                // Clothing Layer
                if (clothingImage != null)
                {
                    RectTransform cRt = clothingImage.rectTransform;
                    cRt.anchorMin = new Vector2(0.5f, 0.5f);
                    cRt.anchorMax = new Vector2(0.5f, 0.5f);
                    cRt.pivot = new Vector2(0.5f, 0.5f);

                    switch (activeClothing)
                    {
                        case BattlePortraitClothing.LeatherChoker:
                            clothingImage.gameObject.SetActive(true);
                            clothingImage.sprite = sprClothingChoker;
                            cRt.anchoredPosition = new Vector2(-85f, -12f);
                            cRt.sizeDelta = new Vector2(85f, 42f);
                            break;
                        case BattlePortraitClothing.SailorFuku:
                            clothingImage.gameObject.SetActive(true);
                            clothingImage.sprite = sprClothingSailor;
                            cRt.anchoredPosition = new Vector2(-75f, -32f);
                            cRt.sizeDelta = new Vector2(150f, 95f);
                            break;
                        case BattlePortraitClothing.FormalSuitBlazer:
                            clothingImage.gameObject.SetActive(true);
                            clothingImage.sprite = sprClothingBlazer;
                            cRt.anchoredPosition = new Vector2(-55f, -44f);
                            cRt.sizeDelta = new Vector2(195f, 125f);
                            break;
                        case BattlePortraitClothing.None:
                        default:
                            clothingImage.gameObject.SetActive(false);
                            break;
                    }
                    clothingImage.preserveAspect = true;
                }

                // Accessory Layer
                if (accessoryImage != null)
                {
                    RectTransform aRt = accessoryImage.rectTransform;
                    aRt.anchorMin = new Vector2(0.5f, 0.5f);
                    aRt.anchorMax = new Vector2(0.5f, 0.5f);
                    aRt.pivot = new Vector2(0.5f, 0.5f);

                    switch (activeAccessory)
                    {
                        case BattlePortraitAccessory.Headband:
                            accessoryImage.gameObject.SetActive(true);
                            accessoryImage.sprite = sprAccHeadband;
                            aRt.anchoredPosition = new Vector2(-95f, 82f);
                            aRt.sizeDelta = new Vector2(65f, 52f);
                            break;
                        case BattlePortraitAccessory.RectangularGlasses:
                            accessoryImage.gameObject.SetActive(true);
                            accessoryImage.sprite = sprAccGlasses;
                            aRt.anchoredPosition = new Vector2(-114f, 52f);
                            aRt.sizeDelta = new Vector2(82f, 44f);
                            break;
                        case BattlePortraitAccessory.DropEarrings:
                            accessoryImage.gameObject.SetActive(true);
                            accessoryImage.sprite = sprAccEarrings;
                            aRt.anchoredPosition = new Vector2(-72f, 38f);
                            aRt.sizeDelta = new Vector2(34f, 34f);
                            break;
                        case BattlePortraitAccessory.None:
                        default:
                            accessoryImage.gameObject.SetActive(false);
                            break;
                    }
                    accessoryImage.preserveAspect = true;
                }
            }

            UpdateVitalsDisplay();
        }

        public void UpdateVitalsDisplay()
        {
            float hpPct = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));
            float mpPct = Mathf.Clamp01(currentMp / Mathf.Max(1f, maxMp));

            if (hpBarSlider != null) hpBarSlider.value = hpPct;
            if (mpBarSlider != null) mpBarSlider.value = mpPct;

            if (hpValueText != null)
            {
                // Persona stylized format: HP 100/100
                hpValueText.text = $"<color=#00e5ff><i><b>HP</b></i></color>  {Mathf.RoundToInt(currentHp)}/{Mathf.RoundToInt(maxHp)}";
            }

            if (mpValueText != null)
            {
                // Persona stylized format: MP 50/50
                mpValueText.text = $"<color=#76ff03><i><b>MP</b></i></color>  {Mathf.RoundToInt(currentMp)}/{Mathf.RoundToInt(maxMp)}";
            }
        }

        public void TriggerDamageFlash()
        {
            if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = StartCoroutine(AnimateDamageFlash());
        }

        private IEnumerator AnimateDamageFlash()
        {
            Color flashCol = new Color(1f, 0.4f, 0.4f, 1f);
            if (templateDirectImage != null) templateDirectImage.color = flashCol;
            if (baseHeadImage != null) baseHeadImage.color = flashCol;

            yield return new WaitForSeconds(0.12f);

            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                Color cur = Color.Lerp(flashCol, Color.white, t / 0.25f);
                if (templateDirectImage != null) templateDirectImage.color = cur;
                if (baseHeadImage != null) baseHeadImage.color = cur;
                yield return null;
            }

            if (templateDirectImage != null) templateDirectImage.color = Color.white;
            if (baseHeadImage != null) baseHeadImage.color = Color.white;
        }

        public void BindHierarchyReferences(
            Image templateDirect,
            GameObject modularRoot,
            Image frameBacking,
            Image baseHead,
            Image hair,
            Image face,
            Image eye,
            Image clothing,
            Image accessory,
            Slider hpSlider,
            Slider mpSlider,
            TextMeshProUGUI hpTxt,
            TextMeshProUGUI mpTxt,
            TextMeshProUGUI nameTxt = null,
            RectTransform vitalsRoot = null)
        {
            if (vitalsRoot != null) vitalsContainer = vitalsRoot;
            templateDirectImage = templateDirect;
            modularLayersRoot = modularRoot;
            frameBackingImage = frameBacking;
            baseHeadImage = baseHead;
            hairImage = hair;
            faceExpressionImage = face;
            eyeImage = eye;
            clothingImage = clothing;
            accessoryImage = accessory;
            hpBarSlider = hpSlider;
            mpBarSlider = mpSlider;
            hpValueText = hpTxt;
            mpValueText = mpTxt;
            characterNameText = nameTxt;

            RefreshDisplay();
            UpdateVitalsDisplay();
        }

        private void OnValidate()
        {
            RefreshDisplay();
            UpdateVitalsDisplay();
        }
    }
}

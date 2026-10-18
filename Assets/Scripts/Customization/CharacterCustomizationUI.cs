using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Customization
{
    public enum CustomizationTab
    {
        Identity,
        BodyMorphology,
        FaceVisage,
        HairStyling,
        AnatomyReferences
    }

    /// <summary>
    /// CharacterCustomizationUI provides an interactive, full-screen customization suite:
    /// - Identity: Name, Sex (Female, Male, Non-Binary, Custom), Pronouns (She/Her, He/Him, They/Them, It/Its, Custom)
    /// - Body & Morphology: Body Type presets + Sliders for Neck Length, Arm Length, Torso, Hips, Legs, Feet, Height, Muscle
    /// - Face & Visage: Face Shape, Skin Tone Palette, Eye Shape, Eye Color + Heterochromia, Nose Shape + Scale, Mouth Shape
    /// - Hair & Styling: 8 Hair Styles, Hair Color Palette, Highlight Color, Outfit Primary & Secondary Colors
    /// - Anatomy & References: Male/Female Anatomy Breakdown + Model Turnaround Sheets 1-5
    /// - 3D Turntable: 360-degree rotation drag, Dynamic Camera Framing (Full Body vs. Portrait/Face Close-up)
    /// - Action Deck: Randomize, Reset, Save & Apply
    /// </summary>
    public class CharacterCustomizationUI : MonoBehaviour
    {
        private static CharacterCustomizationUI _instance;
        public static CharacterCustomizationUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<CharacterCustomizationUI>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Root & Canvas")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button closeButton;

        [Header("Tab Headers")]
        [SerializeField] private Button identityTabBtn;
        [SerializeField] private Button bodyTabBtn;
        [SerializeField] private Button faceTabBtn;
        [SerializeField] private Button hairTabBtn;
        [SerializeField] private Button referencesTabBtn;

        [Header("Tab Content Panels")]
        [SerializeField] private GameObject identityPanel;
        [SerializeField] private GameObject bodyPanel;
        [SerializeField] private GameObject facePanel;
        [SerializeField] private GameObject hairPanel;
        [SerializeField] private GameObject referencesPanel;

        [Header("Anatomy & Reference Gallery")]
        [SerializeField] private ModelReferenceGalleryViewer referenceGalleryViewer;
        [SerializeField] private Button viewAnatomyGuideBtn;

        [Header("Identity Controls")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private TextMeshProUGUI pronounPreviewText;
        [SerializeField] private Button[] sexButtons;
        [SerializeField] private Button[] pronounButtons;

        [Header("Body Controls (Morphology Sliders)")]
        [SerializeField] private Button[] bodyTypeButtons;
        [SerializeField] private Slider neckLengthSlider;
        [SerializeField] private TextMeshProUGUI neckLengthValueTMP;
        [SerializeField] private Slider armLengthSlider;
        [SerializeField] private TextMeshProUGUI armLengthValueTMP;
        [SerializeField] private Slider torsoScaleSlider;
        [SerializeField] private TextMeshProUGUI torsoScaleValueTMP;
        [SerializeField] private Slider hipsScaleSlider;
        [SerializeField] private TextMeshProUGUI hipsScaleValueTMP;
        [SerializeField] private Slider legsLengthSlider;
        [SerializeField] private TextMeshProUGUI legsLengthValueTMP;
        [SerializeField] private Slider feetScaleSlider;
        [SerializeField] private TextMeshProUGUI feetScaleValueTMP;
        [SerializeField] private Slider heightScaleSlider;
        [SerializeField] private TextMeshProUGUI heightScaleValueTMP;

        [Header("Face Controls")]
        [SerializeField] private Button[] faceShapeButtons;
        [SerializeField] private Button[] eyeShapeButtons;
        [SerializeField] private Button[] noseShapeButtons;
        [SerializeField] private Slider noseScaleSlider;
        [SerializeField] private TextMeshProUGUI noseScaleValueTMP;
        [SerializeField] private Button[] mouthShapeButtons;
        [SerializeField] private Toggle heterochromiaToggle;
        [SerializeField] private Transform skinToneContainer;
        [SerializeField] private Transform eyeColorContainer;
        [SerializeField] private Transform rightEyeColorContainer;

        [Header("Hair Controls")]
        [SerializeField] private Button[] hairStyleButtons;
        [SerializeField] private Transform hairColorContainer;
        [SerializeField] private Transform outfitPrimaryContainer;
        [SerializeField] private Transform outfitSecondaryContainer;

        [Header("3D Turntable & Camera Framing")]
        [SerializeField] private Button cameraFramingToggleBtn;
        [SerializeField] private TextMeshProUGUI cameraFramingLabelTMP;
        [SerializeField] private Button rotateLeftBtn;
        [SerializeField] private Button rotateRightBtn;
        [SerializeField] private Toggle autoTurntableToggle;

        [Header("Action Deck")]
        [SerializeField] private Button randomizeButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button saveAndApplyButton;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public CharacterCustomizationData WorkingData => workingData;

        // Runtime Working Copy
        private CharacterCustomizationData workingData;
        private CharacterCustomizationData originalSavedData;
        private CustomizablePlayerAvatar targetAvatar;

        // Turntable & Camera Tracking
        private float currentTurntableYaw = 0f;
        private bool isPortraitFaceView = false;
        private float defaultCamDist = 12f;
        private float defaultCamHeight = 1.6f;
        private float defaultCamPitch = 42f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            ResolveReferences();
        }

        private void Start()
        {
            BindAllEvents();
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        private void Update()
        {
            // Desktop Hotkey 'C' for Character Customization
            if (Input.GetKeyDown(KeyCode.C))
            {
                ToggleCustomizationScreen();
            }

            // Auto Turntable Orbit
            if (IsOpen && autoTurntableToggle != null && autoTurntableToggle.isOn)
            {
                currentTurntableYaw += Time.deltaTime * 35f;
                ApplyTurntableRotation();
            }
        }

        public void ResolveReferences()
        {
            if (targetAvatar == null)
            {
                targetAvatar = FindAnyObjectByType<CustomizablePlayerAvatar>(FindObjectsInactive.Include);
                if (targetAvatar == null)
                {
                    var player = GameObject.Find("Player_Hero");
                    if (player != null)
                    {
                        targetAvatar = player.GetComponent<CustomizablePlayerAvatar>() ?? player.AddComponent<CustomizablePlayerAvatar>();
                    }
                }
            }
        }

        #region Screen Lifecycle
        public void ToggleCustomizationScreen()
        {
            if (IsOpen) CloseCustomizationScreen();
            else OpenCustomizationScreen();
        }

        public void OpenCustomizationScreen()
        {
            ResolveReferences();
            if (overlayRoot != null) overlayRoot.SetActive(true);

            // Load saved data and clone into working copy
            originalSavedData = CharacterCustomizationData.LoadFromPrefs();
            workingData = originalSavedData.Clone();

            if (targetAvatar != null)
            {
                targetAvatar.EnsureRigConstructed();
                targetAvatar.ApplyCustomization(workingData);
            }

            // Lock dungeon look/movement
            if (PortraitARPGCamera.Instance != null)
            {
                defaultCamDist = PortraitARPGCamera.Instance.cameraDistance;
                defaultCamHeight = PortraitARPGCamera.Instance.targetFocusHeight;
                defaultCamPitch = PortraitARPGCamera.Instance.pitchAngle;
            }

            SetCameraFraming(false); // Start with Full Body view
            SwitchTab(CustomizationTab.Identity);
            RefreshAllUI();

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.9f);
            }
        }

        public void CloseCustomizationScreen()
        {
            if (overlayRoot != null) overlayRoot.SetActive(false);

            // Restore camera
            if (PortraitARPGCamera.Instance != null)
            {
                PortraitARPGCamera.Instance.cameraDistance = defaultCamDist;
                PortraitARPGCamera.Instance.targetFocusHeight = defaultCamHeight;
                PortraitARPGCamera.Instance.pitchAngle = defaultCamPitch;
            }

            // Revert unsaved preview changes
            if (originalSavedData != null && targetAvatar != null)
            {
                targetAvatar.ApplyCustomization(originalSavedData);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.85f);
            }
        }

        private void SaveAndApply()
        {
            if (workingData == null) return;

            // Commit name from input field
            if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
            {
                workingData.characterName = nameInputField.text.Trim();
            }

            workingData.SaveToPrefs();
            originalSavedData = workingData.Clone();

            if (targetAvatar != null)
            {
                targetAvatar.ApplyCustomization(workingData);
            }

            // Sync with PlayerProgression
            var prog = FindAnyObjectByType<PlayerProgression>(FindObjectsInactive.Include);
            if (prog != null)
            {
                prog.playerName = workingData.characterName;
            }

            // Restore camera & close
            if (PortraitARPGCamera.Instance != null)
            {
                PortraitARPGCamera.Instance.cameraDistance = defaultCamDist;
                PortraitARPGCamera.Instance.targetFocusHeight = defaultCamHeight;
                PortraitARPGCamera.Instance.pitchAngle = defaultCamPitch;
            }

            if (overlayRoot != null) overlayRoot.SetActive(false);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("quest_complete", 1.0f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(
                    $"<color=#FFD700>[AVATAR TRANSMUTATION SEALED]</color>\n" +
                    $"Name: {workingData.characterName} | {workingData.sex} | {workingData.GetPronounsSummary()}\n" +
                    $"{workingData.GetSampleSentence()}"
                );
            }
        }

        private void ResetToDefault()
        {
            if (originalSavedData != null)
            {
                workingData = originalSavedData.Clone();
            }
            else
            {
                workingData = new CharacterCustomizationData();
                workingData.ApplyBodyTypePreset(BodyTypePreset.Athletic);
            }

            if (targetAvatar != null) targetAvatar.ApplyCustomization(workingData);
            RefreshAllUI();

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.85f);
            }
        }

        private void RandomizeCharacter()
        {
            if (workingData == null) workingData = new CharacterCustomizationData();
            workingData.Randomize();

            if (targetAvatar != null) targetAvatar.ApplyCustomization(workingData);
            RefreshAllUI();

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("spell_cast", 0.9f);
            }
        }
        #endregion

        #region Tabs & Camera Framing
        public void SwitchTab(CustomizationTab tab)
        {
            if (identityPanel != null) identityPanel.SetActive(tab == CustomizationTab.Identity);
            if (bodyPanel != null) bodyPanel.SetActive(tab == CustomizationTab.BodyMorphology);
            if (facePanel != null) facePanel.SetActive(tab == CustomizationTab.FaceVisage);
            if (hairPanel != null) hairPanel.SetActive(tab == CustomizationTab.HairStyling);
            if (referencesPanel != null) referencesPanel.SetActive(tab == CustomizationTab.AnatomyReferences);

            // Auto-frame camera based on category
            if (tab == CustomizationTab.FaceVisage || tab == CustomizationTab.HairStyling)
            {
                SetCameraFraming(true); // Close-up portrait
            }
            else
            {
                SetCameraFraming(false); // Full body silhouette
            }

            HighlightTabHeader(tab);
        }

        public void OpenAnatomyGuide()
        {
            SwitchTab(CustomizationTab.AnatomyReferences);
            if (referenceGalleryViewer != null)
            {
                referenceGalleryViewer.ShowSheet(0); // 0 is Male & Female Anatomy Breakdown
            }
        }

        private void HighlightTabHeader(CustomizationTab tab)
        {
            Color activeCol = new Color(1.0f, 0.85f, 0.30f, 1f);
            Color inactiveCol = new Color(0.60f, 0.65f, 0.75f, 0.85f);

            SetButtonColor(identityTabBtn, tab == CustomizationTab.Identity ? activeCol : inactiveCol);
            SetButtonColor(bodyTabBtn, tab == CustomizationTab.BodyMorphology ? activeCol : inactiveCol);
            SetButtonColor(faceTabBtn, tab == CustomizationTab.FaceVisage ? activeCol : inactiveCol);
            SetButtonColor(hairTabBtn, tab == CustomizationTab.HairStyling ? activeCol : inactiveCol);
            SetButtonColor(referencesTabBtn, tab == CustomizationTab.AnatomyReferences ? activeCol : inactiveCol);
        }

        private void SetButtonColor(Button btn, Color c)
        {
            if (btn == null) return;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = c;
        }

        public void ToggleCameraFraming()
        {
            SetCameraFraming(!isPortraitFaceView);
        }

        public void SetCameraFraming(bool portraitMode)
        {
            isPortraitFaceView = portraitMode;
            if (PortraitARPGCamera.Instance != null)
            {
                if (isPortraitFaceView)
                {
                    // Close-up Face / Head framing
                    PortraitARPGCamera.Instance.cameraDistance = 1.35f;
                    PortraitARPGCamera.Instance.targetFocusHeight = 1.62f * (workingData != null ? workingData.heightScale : 1f);
                    PortraitARPGCamera.Instance.pitchAngle = 6.0f;
                }
                else
                {
                    // Full Body silhouette framing
                    PortraitARPGCamera.Instance.cameraDistance = 3.40f;
                    PortraitARPGCamera.Instance.targetFocusHeight = 1.05f * (workingData != null ? workingData.heightScale : 1f);
                    PortraitARPGCamera.Instance.pitchAngle = 10.0f;
                }
            }

            if (cameraFramingLabelTMP != null)
            {
                cameraFramingLabelTMP.text = isPortraitFaceView ? "[VIEW: PORTRAIT]" : "[VIEW: FULL BODY]";
            }
        }

        public void RotateTurntable(float deltaDegrees)
        {
            currentTurntableYaw += deltaDegrees;
            ApplyTurntableRotation();
        }

        private void ApplyTurntableRotation()
        {
            if (targetAvatar != null)
            {
                targetAvatar.transform.rotation = Quaternion.Euler(0, currentTurntableYaw, 0);
            }
        }
        #endregion

        #region UI Event Binding & Refresh
        private void BindAllEvents()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseCustomizationScreen);
            if (randomizeButton != null) randomizeButton.onClick.AddListener(RandomizeCharacter);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefault);
            if (saveAndApplyButton != null) saveAndApplyButton.onClick.AddListener(SaveAndApply);

            // Tab headers
            if (identityTabBtn != null) identityTabBtn.onClick.AddListener(() => SwitchTab(CustomizationTab.Identity));
            if (bodyTabBtn != null) bodyTabBtn.onClick.AddListener(() => SwitchTab(CustomizationTab.BodyMorphology));
            if (faceTabBtn != null) faceTabBtn.onClick.AddListener(() => SwitchTab(CustomizationTab.FaceVisage));
            if (hairTabBtn != null) hairTabBtn.onClick.AddListener(() => SwitchTab(CustomizationTab.HairStyling));
            if (referencesTabBtn != null) referencesTabBtn.onClick.AddListener(() => SwitchTab(CustomizationTab.AnatomyReferences));
            if (viewAnatomyGuideBtn != null) viewAnatomyGuideBtn.onClick.AddListener(OpenAnatomyGuide);

            // Turntable controls
            if (cameraFramingToggleBtn != null) cameraFramingToggleBtn.onClick.AddListener(ToggleCameraFraming);
            if (rotateLeftBtn != null) rotateLeftBtn.onClick.AddListener(() => RotateTurntable(-25f));
            if (rotateRightBtn != null) rotateRightBtn.onClick.AddListener(() => RotateTurntable(25f));

            // Morphology Sliders
            BindSlider(neckLengthSlider, v => { workingData.neckLength = v; UpdateMorphology(); }, neckLengthValueTMP, "x");
            BindSlider(armLengthSlider, v => { workingData.armLength = v; UpdateMorphology(); }, armLengthValueTMP, "x");
            BindSlider(torsoScaleSlider, v => { workingData.torsoScale = v; UpdateMorphology(); }, torsoScaleValueTMP, "x");
            BindSlider(hipsScaleSlider, v => { workingData.hipsScale = v; UpdateMorphology(); }, hipsScaleValueTMP, "x");
            BindSlider(legsLengthSlider, v => { workingData.legsLength = v; UpdateMorphology(); }, legsLengthValueTMP, "x");
            BindSlider(feetScaleSlider, v => { workingData.feetScale = v; UpdateMorphology(); }, feetScaleValueTMP, "x");
            BindSlider(heightScaleSlider, v => { workingData.heightScale = v; UpdateMorphology(); }, heightScaleValueTMP, "x");
            BindSlider(noseScaleSlider, v => { workingData.noseScale = v; UpdateMorphology(); }, noseScaleValueTMP, "x");

            // Sex Buttons
            if (sexButtons != null)
            {
                for (int i = 0; i < sexButtons.Length; i++)
                {
                    int idx = i;
                    sexButtons[i].onClick.AddListener(() => {
                        workingData.sex = (SexOption)idx;
                        RefreshIdentityDisplay();
                        UpdateMorphology();
                    });
                }
            }

            // Pronoun Buttons
            if (pronounButtons != null)
            {
                for (int i = 0; i < pronounButtons.Length; i++)
                {
                    int idx = i;
                    pronounButtons[i].onClick.AddListener(() => {
                        workingData.pronouns = (PronounOption)idx;
                        RefreshIdentityDisplay();
                    });
                }
            }

            // Body Type Presets
            if (bodyTypeButtons != null)
            {
                for (int i = 0; i < bodyTypeButtons.Length; i++)
                {
                    int idx = i;
                    bodyTypeButtons[i].onClick.AddListener(() => {
                        workingData.ApplyBodyTypePreset((BodyTypePreset)idx);
                        RefreshAllUI();
                        UpdateMorphology();
                    });
                }
            }

            // Face Shape Buttons
            if (faceShapeButtons != null)
            {
                for (int i = 0; i < faceShapeButtons.Length; i++)
                {
                    int idx = i;
                    faceShapeButtons[i].onClick.AddListener(() => {
                        workingData.faceShape = (FaceShapePreset)idx;
                        UpdateMorphology();
                    });
                }
            }

            // Eye Shape Buttons
            if (eyeShapeButtons != null)
            {
                for (int i = 0; i < eyeShapeButtons.Length; i++)
                {
                    int idx = i;
                    eyeShapeButtons[i].onClick.AddListener(() => {
                        workingData.eyeShape = (EyeShapePreset)idx;
                        UpdateMorphology();
                    });
                }
            }

            // Nose Shape Buttons
            if (noseShapeButtons != null)
            {
                for (int i = 0; i < noseShapeButtons.Length; i++)
                {
                    int idx = i;
                    noseShapeButtons[i].onClick.AddListener(() => {
                        workingData.noseShape = (NoseShapePreset)idx;
                        UpdateMorphology();
                    });
                }
            }

            // Mouth Shape Buttons
            if (mouthShapeButtons != null)
            {
                for (int i = 0; i < mouthShapeButtons.Length; i++)
                {
                    int idx = i;
                    mouthShapeButtons[i].onClick.AddListener(() => {
                        workingData.mouthShape = (MouthShapePreset)idx;
                        UpdateMorphology();
                    });
                }
            }

            // Hair Style Buttons
            if (hairStyleButtons != null)
            {
                for (int i = 0; i < hairStyleButtons.Length; i++)
                {
                    int idx = i;
                    hairStyleButtons[i].onClick.AddListener(() => {
                        workingData.hairStyle = (HairStyleOption)idx;
                        UpdateMorphology();
                    });
                }
            }

            // Heterochromia Toggle
            if (heterochromiaToggle != null)
            {
                heterochromiaToggle.onValueChanged.AddListener(val => {
                    workingData.heterochromia = val;
                    if (rightEyeColorContainer != null) rightEyeColorContainer.gameObject.SetActive(val);
                    UpdateMorphology();
                });
            }
        }

        private void BindSlider(Slider slider, Action<float> onValChanged, TextMeshProUGUI valText, string suffix)
        {
            if (slider == null) return;
            slider.onValueChanged.AddListener(v => {
                onValChanged?.Invoke(v);
                if (valText != null) valText.text = $"{v:F2}{suffix}";
            });
        }

        private void UpdateMorphology()
        {
            if (targetAvatar != null && workingData != null)
            {
                targetAvatar.ApplyCustomization(workingData);
            }
        }

        private void RefreshAllUI()
        {
            if (workingData == null) return;

            // Identity
            if (nameInputField != null) nameInputField.text = workingData.characterName;
            RefreshIdentityDisplay();

            // Sliders
            SetSliderValue(neckLengthSlider, workingData.neckLength, neckLengthValueTMP, "x");
            SetSliderValue(armLengthSlider, workingData.armLength, armLengthValueTMP, "x");
            SetSliderValue(torsoScaleSlider, workingData.torsoScale, torsoScaleValueTMP, "x");
            SetSliderValue(hipsScaleSlider, workingData.hipsScale, hipsScaleValueTMP, "x");
            SetSliderValue(legsLengthSlider, workingData.legsLength, legsLengthValueTMP, "x");
            SetSliderValue(feetScaleSlider, workingData.feetScale, feetScaleValueTMP, "x");
            SetSliderValue(heightScaleSlider, workingData.heightScale, heightScaleValueTMP, "x");
            SetSliderValue(noseScaleSlider, workingData.noseScale, noseScaleValueTMP, "x");

            if (heterochromiaToggle != null) heterochromiaToggle.isOn = workingData.heterochromia;
            if (rightEyeColorContainer != null) rightEyeColorContainer.gameObject.SetActive(workingData.heterochromia);
        }

        private void RefreshIdentityDisplay()
        {
            if (pronounPreviewText != null && workingData != null)
            {
                pronounPreviewText.text =
                    $"<color=#FFD700>Sex:</color> {workingData.sex}   |   " +
                    $"<color=#FFD700>Pronouns:</color> {workingData.GetPronounsSummary()}\n" +
                    $"<color=#A0E0FF>{workingData.GetSampleSentence()}</color>";
            }
        }

        private void SetSliderValue(Slider s, float val, TextMeshProUGUI txt, string suffix)
        {
            if (s != null)
            {
                s.SetValueWithoutNotify(val);
                if (txt != null) txt.text = $"{val:F2}{suffix}";
            }
        }

        public void SelectSkinTone(int idx)
        {
            if (workingData == null) return;
            workingData.skinToneIndex = Mathf.Clamp(idx, 0, CharacterCustomizationData.SkinTones.Length - 1);
            UpdateMorphology();
        }

        public void SelectEyeColor(int idx)
        {
            if (workingData == null) return;
            workingData.eyeColorIndex = Mathf.Clamp(idx, 0, CharacterCustomizationData.EyeColors.Length - 1);
            UpdateMorphology();
        }

        public void SelectRightEyeColor(int idx)
        {
            if (workingData == null) return;
            workingData.rightEyeColorIndex = Mathf.Clamp(idx, 0, CharacterCustomizationData.EyeColors.Length - 1);
            UpdateMorphology();
        }

        public void SelectHairColor(int idx)
        {
            if (workingData == null) return;
            workingData.hairColorIndex = Mathf.Clamp(idx, 0, CharacterCustomizationData.HairColors.Length - 1);
            UpdateMorphology();
        }

        public void SelectOutfitPrimary(int idx)
        {
            if (workingData == null) return;
            workingData.outfitPrimaryColor = Mathf.Clamp(idx, 0, CharacterCustomizationData.OutfitColors.Length - 1);
            UpdateMorphology();
        }

        public void SelectOutfitSecondary(int idx)
        {
            if (workingData == null) return;
            workingData.outfitSecondaryColor = Mathf.Clamp(idx, 0, CharacterCustomizationData.OutfitColors.Length - 1);
            UpdateMorphology();
        }
        #endregion
    }
}

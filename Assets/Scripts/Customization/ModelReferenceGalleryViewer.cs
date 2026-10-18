using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Customization
{
    /// <summary>
    /// ModelReferenceGalleryViewer provides an interactive in-game reference sheet gallery:
    /// - Displays the Male & Female Anatomy Breakdown (Ref_Body_Anatomy_Breakdown.jpg)
    /// - Displays Character Turnaround Model Reference Sheets 1-5 (Ref_Sheet_1 to 5.jpg)
    /// - Features previous/next navigation, quick sheet selectors, fullscreen zoom, and descriptive anatomy notes.
    /// - Embedded inside CharacterCustomizationUI (Anatomy & Refs tab) and accessible via F4 hotkey.
    /// </summary>
    public class ModelReferenceGalleryViewer : MonoBehaviour
    {
        public static ModelReferenceGalleryViewer Instance { get; private set; }

        [System.Serializable]
        public class ReferenceSheetEntry
        {
            public string title;
            public string category;
            [TextArea(2, 4)]
            public string description;
            public string assetPath;
            public Sprite sprite;
        }

        [Header("UI Visual Elements")]
        [SerializeField] private Image referenceDisplayImage;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI categoryBadgeTMP;
        [SerializeField] private TextMeshProUGUI descriptionTMP;
        [SerializeField] private TextMeshProUGUI pageIndicatorTMP;

        [Header("Navigation Buttons")]
        [SerializeField] private Button prevBtn;
        [SerializeField] private Button nextBtn;
        [SerializeField] private Button[] sheetQuickButtons;
        [SerializeField] private Button zoomToggleBtn;
        [SerializeField] private TextMeshProUGUI zoomLabelTMP;

        [Header("Reference Catalog")]
        [SerializeField] private List<ReferenceSheetEntry> catalog = new List<ReferenceSheetEntry>();

        private int currentIndex = 0;
        private bool isZoomed = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            InitializeCatalog();
        }

        private void Start()
        {
            BindButtons();
            ShowSheet(0);
        }

        private void Update()
        {
            // Desktop Hotkey 'F4' toggles standalone gallery modal
            if (Input.GetKeyDown(KeyCode.F4))
            {
                ToggleGallery();
            }

            // Keyboard navigation when active
            if (gameObject.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    PreviousSheet();
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    NextSheet();
                }
            }
        }

        public void InitializeCatalog()
        {
            if (catalog.Count > 0 && catalog[0].sprite != null) return;

            catalog = new List<ReferenceSheetEntry>
            {
                new ReferenceSheetEntry
                {
                    title = "Male & Female Anatomy Breakdown",
                    category = "ANATOMICAL RATIOS",
                    description = "Comprehensive proportion blueprints for Head & Neck, Full Trunk (Torso & Pelvis), Arms & Hands, and Thighs, Legs & Feet. Reference when tuning neck, arm, torso, and hip morphology sliders.",
                    assetPath = "Assets/Textures/References/Ref_Body_Anatomy_Breakdown.jpg"
                },
                new ReferenceSheetEntry
                {
                    title = "Model Reference Sheet 1",
                    category = "HEROIC TURNAROUND",
                    description = "Front, 3/4, side, and rear orthographic full-body silhouettes and baseline armature alignment.",
                    assetPath = "Assets/Textures/References/Ref_Sheet_1.jpg"
                },
                new ReferenceSheetEntry
                {
                    title = "Model Reference Sheet 2",
                    category = "ORTHOGRAPHIC SPECS",
                    description = "Detailed limb articulation, muscle flow, and joint pivot reference sheets for character rig deformation.",
                    assetPath = "Assets/Textures/References/Ref_Sheet_2.jpg"
                },
                new ReferenceSheetEntry
                {
                    title = "Model Reference Sheet 3",
                    category = "DYNAMIC ACTION SILHOUETTES",
                    description = "Combat stances, center-of-mass weight distribution, and gear silhouette balance.",
                    assetPath = "Assets/Textures/References/Ref_Sheet_3.jpg"
                },
                new ReferenceSheetEntry
                {
                    title = "Model Reference Sheet 4",
                    category = "FACIAL MORPHOLOGY & VOLUMES",
                    description = "Jawline angles, eye spacing, nose bridge contours, and anime hair volume breakdowns.",
                    assetPath = "Assets/Textures/References/Ref_Sheet_4.jpg"
                },
                new ReferenceSheetEntry
                {
                    title = "Model Reference Sheet 5",
                    category = "COSTUME & EQUIPMENT LAYERING",
                    description = "Garment flow, catalyst pouch rigging, and accessory layering over base body meshes.",
                    assetPath = "Assets/Textures/References/Ref_Sheet_5.jpg"
                }
            };

            foreach (var entry in catalog)
            {
                entry.sprite = LoadSprite(entry.assetPath);
            }
        }

        private Sprite LoadSprite(string path)
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

        public void BindReferences(
            Image displayImage,
            TextMeshProUGUI title,
            TextMeshProUGUI categoryBadge,
            TextMeshProUGUI description,
            TextMeshProUGUI pageIndicator,
            Button previousButton,
            Button nextButton,
            Button zoomButton,
            TextMeshProUGUI zoomLabel,
            Button[] quickButtons = null)
        {
            referenceDisplayImage = displayImage;
            titleTMP = title;
            categoryBadgeTMP = categoryBadge;
            descriptionTMP = description;
            pageIndicatorTMP = pageIndicator;
            prevBtn = previousButton;
            nextBtn = nextButton;
            zoomToggleBtn = zoomButton;
            zoomLabelTMP = zoomLabel;
            sheetQuickButtons = quickButtons;

            BindButtons();
            InitializeCatalog();
            ShowSheet(currentIndex);
        }

        private void BindButtons()
        {
            if (prevBtn != null)
            {
                prevBtn.onClick.RemoveAllListeners();
                prevBtn.onClick.AddListener(PreviousSheet);
            }
            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(NextSheet);
            }
            if (zoomToggleBtn != null)
            {
                zoomToggleBtn.onClick.RemoveAllListeners();
                zoomToggleBtn.onClick.AddListener(ToggleZoom);
            }

            if (sheetQuickButtons != null)
            {
                for (int i = 0; i < sheetQuickButtons.Length; i++)
                {
                    int index = i;
                    if (sheetQuickButtons[i] != null)
                    {
                        sheetQuickButtons[i].onClick.RemoveAllListeners();
                        sheetQuickButtons[i].onClick.AddListener(() => ShowSheet(index));
                    }
                }
            }
        }

        public void ShowSheet(int index)
        {
            if (catalog == null || catalog.Count == 0) InitializeCatalog();
            if (index < 0) index = catalog.Count - 1;
            if (index >= catalog.Count) index = 0;

            currentIndex = index;
            var entry = catalog[currentIndex];

            if (referenceDisplayImage != null)
            {
                if (entry.sprite == null) entry.sprite = LoadSprite(entry.assetPath);
                referenceDisplayImage.sprite = entry.sprite;
                referenceDisplayImage.preserveAspect = true;
                referenceDisplayImage.enabled = (entry.sprite != null);
            }

            if (titleTMP != null) titleTMP.text = $"<b><color=#FFD700>{entry.title}</color></b>";
            if (categoryBadgeTMP != null) categoryBadgeTMP.text = $"<color=#8BE9FD>[{entry.category}]</color>";
            if (descriptionTMP != null) descriptionTMP.text = entry.description;
            if (pageIndicatorTMP != null) pageIndicatorTMP.text = $"<b>{currentIndex + 1} / {catalog.Count}</b>";

            HighlightActiveButton(currentIndex);
        }

        public void NextSheet()
        {
            ShowSheet(currentIndex + 1);
        }

        public void PreviousSheet()
        {
            ShowSheet(currentIndex - 1);
        }

        public void ToggleZoom()
        {
            isZoomed = !isZoomed;
            if (referenceDisplayImage != null)
            {
                var rt = referenceDisplayImage.rectTransform;
                if (isZoomed)
                {
                    rt.localScale = new Vector3(1.35f, 1.35f, 1f);
                    if (zoomLabelTMP != null) zoomLabelTMP.text = "ZOOM: 135%";
                }
                else
                {
                    rt.localScale = Vector3.one;
                    if (zoomLabelTMP != null) zoomLabelTMP.text = "ZOOM: FIT";
                }
            }
        }

        private void HighlightActiveButton(int activeIdx)
        {
            if (sheetQuickButtons == null) return;
            for (int i = 0; i < sheetQuickButtons.Length; i++)
            {
                if (sheetQuickButtons[i] == null) continue;
                var img = sheetQuickButtons[i].GetComponent<Image>();
                var txt = sheetQuickButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                bool isActive = (i == activeIdx);
                if (img != null) img.color = isActive ? new Color(0.9f, 0.75f, 0.25f, 0.9f) : new Color(0.12f, 0.16f, 0.24f, 0.8f);
                if (txt != null) txt.color = isActive ? new Color(0.08f, 0.08f, 0.12f, 1f) : Color.white;
            }
        }

        public void ToggleGallery()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}

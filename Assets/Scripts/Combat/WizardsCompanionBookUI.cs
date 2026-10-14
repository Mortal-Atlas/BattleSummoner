using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Combat
{
    [System.Serializable]
    public class CompanionSpellPage
    {
        public string spellId;
        public string spellName;
        public SpellElement element;
        public int manaCost;
        public string chapterTitle;
        [TextArea(2, 3)] public string loreSnippet;
        public string gestureShapeName;
        [TextArea(2, 4)] public string castingInstructions;
        public string specialMechanicNote; // e.g., "Continuous Circuits" or "Hold up to 1.50x Charge"
        public Color elementColor;
        public Vector2[] strokePoints;
        public Vector2[] secondaryStrokePoints; // For composite shapes like V + Dash
        public string[] strokeLabels; // e.g. ["(1) Start cleft", "(2) Left lobe", ...]
        public Vector2[] labelPositions;
    }

    /// <summary>
    /// WizardsCompanionBookUI provides a compact, illuminated arcane spellbook
    /// inspired by "The Wizard's Companion" in Ni no Kuni:
    /// - Occupies a small, dedicated section of the screen out of the way of controls and menus
    ///   (anchored in the upper-mid region between player stats and party squad, above the casting rune).
    /// - Displays illustrated stroke diagrams with numbered steps, directional arrows, and channeling rules.
    /// - Includes detailed pages for the Cartoon Heart (Healing) and V-with-Dash (Fireball).
    /// - Features interactive page turning ([◀ Prev] / [Next ▶]) and a collapsible bookmark ribbon.
    /// </summary>
    public class WizardsCompanionBookUI : MonoBehaviour
    {
        private static WizardsCompanionBookUI _instance;
        public static WizardsCompanionBookUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<WizardsCompanionBookUI>();
                }
                return _instance;
            }
        }

        [Header("Root & Frame References")]
        [SerializeField] private GameObject companionBookRoot;
        [SerializeField] private RectTransform bookWindowRect;
        [SerializeField] private GameObject collapsedRibbonTab;
        [SerializeField] private Button expandRibbonButton;
        [SerializeField] private Button minimizeButton;

        [Header("Header Elements")]
        [SerializeField] private TextMeshProUGUI bookHeaderTMP;
        [SerializeField] private TextMeshProUGUI chapterSubtitleTMP;
        [SerializeField] private TextMeshProUGUI pageCounterTMP;

        [Header("Spell Content Elements")]
        [SerializeField] private TextMeshProUGUI spellTitleTMP;
        [SerializeField] private TextMeshProUGUI spellMetaTMP;
        [SerializeField] private TextMeshProUGUI spellLoreTMP;
        [SerializeField] private TextMeshProUGUI castingInstructionsTMP;
        [SerializeField] private TextMeshProUGUI specialMechanicTMP;

        [Header("Diagram Canvas")]
        [SerializeField] private RectTransform diagramCanvasRect;
        [SerializeField] private Button primeSpellButton;

        [Header("Page Navigation Buttons")]
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;

        [Header("Configuration")]
        [SerializeField] private bool isOpenByDefault = true;
        [SerializeField] private int currentPageIndex = 0;

        // Spell Pages Library
        private readonly List<CompanionSpellPage> pages = new List<CompanionSpellPage>();

        public bool IsBookOpen => companionBookRoot != null && companionBookRoot.activeSelf;
        public int CurrentPageIndex => currentPageIndex;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            BuildCompanionPagesLibrary();
        }

        private void Start()
        {
            EnsureUIHierarchy();
            SetBookOpen(isOpenByDefault);
            DisplayCurrentPage();
        }

        private void Update()
        {
            // Desktop shortcut: 'B' toggles Wizard's Companion
            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleBookVisibility();
            }

            // Arrow keys flip pages when book is open
            if (IsBookOpen)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    PreviousPage();
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    NextPage();
                }
            }
        }

        #region Pages Library Setup (Ni no Kuni Style)
        private void BuildCompanionPagesLibrary()
        {
            pages.Clear();

            // PAGE 1: Healing Harmony (Cartoon Heart)
            pages.Add(new CompanionSpellPage
            {
                spellId = "companion_healing_harmony",
                spellName = "Healing Harmony",
                element = SpellElement.Healing,
                manaCost = 30,
                chapterTitle = "Chapter I: Rites of Restoration",
                loreSnippet = "Ancient astral scripture of healing. The caster locks into this rite upon casting, streaming life ether into comrades.",
                gestureShapeName = "Cartoon Heart (Continuous Circuits)",
                castingInstructions = "<b>[1]</b> Top cleft start -> <b>[2]</b> Left lobe sweep -> <b>[3]</b> Bottom apex -> <b>[4]</b> Right lobe closure.",
                specialMechanicNote = "<color=#00AA55><b>CONTINUOUS CIRCUITS:</b></color> Press CAST -> Tap ally portrait on right -> Continuously trace hearts/circles to stream healing ticks. Bigger & more accurate = larger heals!",
                elementColor = new Color(0.10f, 0.75f, 0.45f),
                strokePoints = GenerateHeartPoints(),
                strokeLabels = new string[] { "1 Start Cleft", "2 Left Lobe", "3 Apex", "4 Right Lobe" },
                labelPositions = new Vector2[] { new Vector2(0f, 44f), new Vector2(-68f, 25f), new Vector2(0f, -68f), new Vector2(68f, 25f) }
            });

            // PAGE 2: Flame Slinger ('V' with Dash)
            pages.Add(new CompanionSpellPage
            {
                spellId = "companion_flame_slinger",
                spellName = "Flame Slinger",
                element = SpellElement.Fire,
                manaCost = 25,
                chapterTitle = "Chapter II: Pyromantic Arts",
                loreSnippet = "Summons a concentrated solar ember into the caster's palm. The ether condenses into a blazing ballistic projectile.",
                gestureShapeName = "Chevron 'V' + Horizontal Crossbar",
                castingInstructions = "<b>[1]</b> Stroke 1: Draw downward chevron 'V' to acute bottom apex -> <b>[2]</b> Stroke 2: Cross horizontal dash through center.",
                specialMechanicNote = "<color=#DD4400><b>HOLD TO CHARGE (1.50x):</b></color> Press CAST -> Hold hand flame to charge power up to 1.50x DMG (+50% bonus). Drag and release to sling fireball!",
                elementColor = new Color(0.95f, 0.40f, 0.10f),
                strokePoints = new Vector2[] { new Vector2(-50f, 42f), new Vector2(0f, -38f), new Vector2(50f, 42f) },
                secondaryStrokePoints = new Vector2[] { new Vector2(-42f, 6f), new Vector2(42f, 6f) },
                strokeLabels = new string[] { "1 Chevron 'V'", "2 Crossbar Dash" },
                labelPositions = new Vector2[] { new Vector2(-64f, 42f), new Vector2(-54f, 6f) }
            });

            // PAGE 3: Ice Lance (Undulating Wave)
            pages.Add(new CompanionSpellPage
            {
                spellId = "companion_ice_lance",
                spellName = "Glacies Ice Lance",
                element = SpellElement.Water,
                manaCost = 28,
                chapterTitle = "Chapter III: Cryomancy & Frost",
                loreSnippet = "Condenses ambient moisture into a razor crystalline javelin that pierces frozen armor.",
                gestureShapeName = "Undulating Sine Wave",
                castingInstructions = "<b>[1]</b> Draw a fluid horizontal wave from left to right with two crests and a trough.",
                specialMechanicNote = "<color=#00CCFF><b>FROST CHILL:</b></color> Inflicts Chilled status (-35% movement). Striking frozen targets triggers SHATTERED bonus damage.",
                elementColor = new Color(0.25f, 0.85f, 1.0f),
                strokePoints = new Vector2[] { new Vector2(-50f, 0f), new Vector2(-25f, 35f), new Vector2(0f, -35f), new Vector2(25f, 35f), new Vector2(50f, 0f) },
                strokeLabels = new string[] { "1 Start", "2 Crest", "3 Trough", "4 End" },
                labelPositions = new Vector2[] { new Vector2(-58f, -12f), new Vector2(-25f, 45f), new Vector2(0f, -48f), new Vector2(58f, -12f) }
            });

            // PAGE 4: Frost Barrier (Aegis Square)
            pages.Add(new CompanionSpellPage
            {
                spellId = "companion_frost_barrier",
                spellName = "Aegis Frost Barrier",
                element = SpellElement.Rock,
                manaCost = 35,
                chapterTitle = "Chapter IV: Wardcraft & Aegis",
                loreSnippet = "Erects a protective crystalline polygon that absorbs physical strikes and deflects projectile impacts.",
                gestureShapeName = "4-Corner Square Aegis",
                castingInstructions = "<b>[1]</b> Draw a closed square: Top-left -> Top-right -> Bottom-right -> Bottom-left -> Close.",
                specialMechanicNote = "<color=#E0A96D><b>WARD REINFORCE:</b></color> Fortifies caster armor by +40% for 12 seconds.",
                elementColor = new Color(0.85f, 0.70f, 0.45f),
                strokePoints = new Vector2[] { new Vector2(-38f, 38f), new Vector2(38f, 38f), new Vector2(38f, -38f), new Vector2(-38f, -38f), new Vector2(-38f, 38f) },
                strokeLabels = new string[] { "1 Top-L", "2 Top-R", "3 Bot-R", "4 Bot-L" },
                labelPositions = new Vector2[] { new Vector2(-48f, 48f), new Vector2(48f, 48f), new Vector2(48f, -48f), new Vector2(-48f, -48f) }
            });

            // PAGE 5: Inferno Singularity (Circle + Triangle)
            pages.Add(new CompanionSpellPage
            {
                spellId = "companion_inferno_singularity",
                spellName = "Inferno Singularity",
                element = SpellElement.Fire,
                manaCost = 55,
                chapterTitle = "Chapter V: Grand Occult Evocations",
                loreSnippet = "High-tier pyromancy creating a miniature gravitational sun that collapses in a catastrophic burst.",
                gestureShapeName = "Outer Circle + Inscribed Triangle",
                castingInstructions = "<b>[1]</b> Inscribe a full 360-degree circle -> <b>[2]</b> Draw inner 3-point triangle.",
                specialMechanicNote = "<color=#FF3366><b>VORTEX COLLAPSE:</b></color> Pulls all nearby monsters to epicenter before detonating for 320 Fire DMG.",
                elementColor = new Color(1.0f, 0.30f, 0.40f),
                strokePoints = GenerateCirclePoints(40f),
                secondaryStrokePoints = new Vector2[] { new Vector2(0f, 32f), new Vector2(28f, -18f), new Vector2(-28f, -18f), new Vector2(0f, 32f) },
                strokeLabels = new string[] { "1 Outer Seal", "2 Inner Triangle" },
                labelPositions = new Vector2[] { new Vector2(0f, 52f), new Vector2(0f, -6f) }
            });
        }

        private Vector2[] GenerateHeartPoints()
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= 24; i++)
            {
                float t = (i / 24f) * Mathf.PI * 2f;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                pts.Add(new Vector2(x * 2.5f, y * 2.5f));
            }
            return pts.ToArray();
        }

        private Vector2[] GenerateCirclePoints(float radius)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= 20; i++)
            {
                float a = (i / 20f) * Mathf.PI * 2f;
                pts.Add(new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius));
            }
            return pts.ToArray();
        }
        #endregion

        #region Book Visibility & Page Navigation
        public void ToggleBookVisibility()
        {
            SetBookOpen(!IsBookOpen);
        }

        public void SetBookOpen(bool open)
        {
            if (companionBookRoot != null)
            {
                companionBookRoot.SetActive(open);
            }
            if (collapsedRibbonTab != null)
            {
                collapsedRibbonTab.SetActive(!open);
            }

            if (open)
            {
                DisplayCurrentPage();
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.85f);
                }
            }
            else
            {
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.75f);
                }
            }
        }

        public void NextPage()
        {
            if (pages.Count == 0) BuildCompanionPagesLibrary();
            if (pages.Count == 0) return;
            currentPageIndex = (currentPageIndex + 1) % pages.Count;
            DisplayCurrentPage();
            PlayPageTurnSFX();
        }

        public void PreviousPage()
        {
            if (pages.Count == 0) BuildCompanionPagesLibrary();
            if (pages.Count == 0) return;
            currentPageIndex = (currentPageIndex - 1 + pages.Count) % pages.Count;
            DisplayCurrentPage();
            PlayPageTurnSFX();
        }

        public void GoToSpell(string spellId)
        {
            if (pages.Count == 0) BuildCompanionPagesLibrary();
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].spellId == spellId)
                {
                    currentPageIndex = i;
                    SetBookOpen(true);
                    DisplayCurrentPage();
                    return;
                }
            }
        }

        private void PlayPageTurnSFX()
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("tab_click", 0.9f);
            }
        }
        #endregion

        #region Page Display & Diagram Drawing
        public void DisplayCurrentPage()
        {
            if (pages.Count == 0) BuildCompanionPagesLibrary();
            if (pages.Count == 0 || currentPageIndex < 0 || currentPageIndex >= pages.Count) return;

            var page = pages[currentPageIndex];

            // 1. Header & Page Counter
            if (bookHeaderTMP != null)
            {
                bookHeaderTMP.text = "THE WIZARD'S COMPANION";
            }
            if (chapterSubtitleTMP != null)
            {
                chapterSubtitleTMP.text = page.chapterTitle;
            }
            if (pageCounterTMP != null)
            {
                pageCounterTMP.text = $"Folio {currentPageIndex + 1} / {pages.Count}";
            }

            // 2. Spell Title & Meta
            if (spellTitleTMP != null)
            {
                spellTitleTMP.text = $"<b>{page.spellName.ToUpper()}</b>";
            }
            if (spellMetaTMP != null)
            {
                string elemHex = ColorUtility.ToHtmlStringRGB(page.elementColor);
                spellMetaTMP.text = $"<color=#{elemHex}>{page.element}</color> • <color=#336699>{page.manaCost} MP</color> • <i>{page.gestureShapeName}</i>";
            }

            // 3. Instructions & Mechanic
            if (castingInstructionsTMP != null)
            {
                castingInstructionsTMP.text = $"{page.castingInstructions}\n{page.specialMechanicNote}";
            }
            if (specialMechanicTMP != null)
            {
                specialMechanicTMP.text = page.specialMechanicNote;
            }
            if (spellLoreTMP != null)
            {
                spellLoreTMP.text = $"<i>\"{page.loreSnippet}\"</i>";
            }

            // 4. Draw Illuminated Spell Diagram
            DrawNiNoKuniDiagram(page);
        }

        private void DrawNiNoKuniDiagram(CompanionSpellPage page)
        {
            if (diagramCanvasRect == null) return;

            // Clear previous diagram elements (handling both Edit Mode and Play Mode)
            for (int i = diagramCanvasRect.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(diagramCanvasRect.GetChild(i).gameObject);
                else
                    Destroy(diagramCanvasRect.GetChild(i).gameObject);
#else
                Destroy(diagramCanvasRect.GetChild(i).gameObject);
#endif
            }

            // A. Parchment Watermark / Seal Frame
            var seal = new GameObject("RunicSealBackground", typeof(RectTransform), typeof(Image));
            seal.transform.SetParent(diagramCanvasRect, false);
            var sRT = seal.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0.5f, 0.5f);
            sRT.anchorMax = new Vector2(0.5f, 0.5f);
            sRT.sizeDelta = new Vector2(140f, 140f);
            var sImg = seal.GetComponent<Image>();
            sImg.color = new Color(0.72f, 0.65f, 0.52f, 0.18f); // Faint parchment ink seal
            sImg.raycastTarget = false;

            // B. Draw Primary Stroke Path & Segments
            DrawStrokePolyline(page.strokePoints, page.elementColor, diagramCanvasRect);

            // C. Draw Secondary Stroke Path (if present, e.g. crossbar dash)
            if (page.secondaryStrokePoints != null && page.secondaryStrokePoints.Length > 0)
            {
                DrawStrokePolyline(page.secondaryStrokePoints, new Color(1f, 0.85f, 0.25f, 0.95f), diagramCanvasRect);
            }

            // D. Illuminated In-Diagram Annotation (e.g. Heart Channel / Hold 1.50x)
            if (page.spellId == "companion_healing_harmony")
            {
                CreateDiagramAnnotation("HEART CIRCUIT", new Vector2(0f, 0f), new Color(0.12f, 0.55f, 0.32f, 0.90f), 9.5f);
            }
            else if (page.spellId == "companion_flame_slinger")
            {
                CreateDiagramAnnotation("HOLD -> 1.50x CHARGE", new Vector2(0f, -54f), new Color(0.85f, 0.32f, 0.05f, 0.95f), 9.5f);
            }

            // E. Draw Step Numbers & Badges (Ni no Kuni style numbered callouts)
            if (page.strokeLabels != null && page.labelPositions != null)
            {
                for (int i = 0; i < page.strokeLabels.Length && i < page.labelPositions.Length; i++)
                {
                    var badgeGo = new GameObject("StepBadge_" + i, typeof(RectTransform), typeof(Image));
                    badgeGo.transform.SetParent(diagramCanvasRect, false);
                    var bRT = badgeGo.GetComponent<RectTransform>();
                    bRT.anchorMin = new Vector2(0.5f, 0.5f);
                    bRT.anchorMax = new Vector2(0.5f, 0.5f);
                    bRT.sizeDelta = new Vector2(18f, 18f);
                    bRT.anchoredPosition = page.labelPositions[i];

                    var bImg = badgeGo.GetComponent<Image>();
                    bImg.color = new Color(0.24f, 0.16f, 0.08f, 0.95f); // Antique leather stamp

                    var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    labelGo.transform.SetParent(badgeGo.transform, false);
                    var lRT = labelGo.GetComponent<RectTransform>();
                    lRT.anchorMin = Vector2.zero;
                    lRT.anchorMax = Vector2.one;

                    var tmp = labelGo.GetComponent<TextMeshProUGUI>();
                    tmp.fontSize = 11f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(1f, 0.88f, 0.45f); // Gilded lettering
                    string txt = page.strokeLabels[i];
                    int spaceIdx = txt.IndexOf(' ');
                    string badgeDigit = spaceIdx > 0 ? txt.Substring(0, spaceIdx) : txt;
                    tmp.text = $"<b>{badgeDigit}</b>";
                    tmp.raycastTarget = false;
                }
            }
        }

        private void CreateDiagramAnnotation(string text, Vector2 pos, Color col, float size)
        {
            var noteGo = new GameObject("DiagramNote", typeof(RectTransform), typeof(TextMeshProUGUI));
            noteGo.transform.SetParent(diagramCanvasRect, false);
            var rt = noteGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(130f, 18f);
            rt.anchoredPosition = pos;

            var tmp = noteGo.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{text}</b>";
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = col;
            tmp.raycastTarget = false;
        }

        private void DrawStrokePolyline(Vector2[] points, Color strokeColor, RectTransform container)
        {
            if (points == null || points.Length < 2) return;

            for (int i = 0; i < points.Length; i++)
            {
                // Inscription pip
                var pip = new GameObject("Pip_" + i, typeof(RectTransform), typeof(Image));
                pip.transform.SetParent(container, false);
                var pRT = pip.GetComponent<RectTransform>();
                pRT.anchorMin = new Vector2(0.5f, 0.5f);
                pRT.anchorMax = new Vector2(0.5f, 0.5f);
                pRT.sizeDelta = new Vector2(8f, 8f);
                pRT.anchoredPosition = points[i];
                var pImg = pip.GetComponent<Image>();
                pImg.color = (i == 0) ? new Color(1f, 0.95f, 0.3f, 1f) : strokeColor;
                pImg.raycastTarget = false;

                // Connecting stroke segment
                if (i < points.Length - 1)
                {
                    Vector2 p0 = points[i];
                    Vector2 p1 = points[i + 1];
                    Vector2 diff = p1 - p0;
                    float dist = diff.magnitude;
                    float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                    var segment = new GameObject("Seg_" + i, typeof(RectTransform), typeof(Image));
                    segment.transform.SetParent(container, false);
                    var sRT = segment.GetComponent<RectTransform>();
                    sRT.anchorMin = new Vector2(0.5f, 0.5f);
                    sRT.anchorMax = new Vector2(0.5f, 0.5f);
                    sRT.sizeDelta = new Vector2(dist, 3.5f);
                    sRT.anchoredPosition = p0 + diff * 0.5f;
                    sRT.localRotation = Quaternion.Euler(0f, 0f, angle);

                    var sImg = segment.GetComponent<Image>();
                    sImg.color = strokeColor;
                    sImg.raycastTarget = false;
                }
            }
        }
        #endregion

        #region UI Hierarchy Construction & Binding
        /// <summary>
        /// Automatically constructs the small, out-of-the-way Ni no Kuni styled grimoire book
        /// within the HUD canvas if not already wired in the inspector.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var hudParent = GameObject.Find("MockupMasterHUD")?.transform ?? canvas.transform;

            // 1. Collapsed Bookmark Ribbon Tab (Tiny bookmark that stays neatly tucked away)
            if (collapsedRibbonTab == null)
            {
                var tabGo = GameObject.Find("WizardCompanionRibbonTab");
                if (tabGo == null)
                {
                    tabGo = new GameObject("WizardCompanionRibbonTab", typeof(RectTransform), typeof(Image), typeof(Button));
                    tabGo.transform.SetParent(hudParent, false);

                    var rt = tabGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.34f, 0.88f);
                    rt.anchorMax = new Vector2(0.66f, 0.92f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    var img = tabGo.GetComponent<Image>();
                    img.color = new Color(0.38f, 0.22f, 0.12f, 0.95f); // Rich antique leather

                    var txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(tabGo.transform, false);
                    var tRT = txtObj.GetComponent<RectTransform>();
                    tRT.anchorMin = Vector2.zero;
                    tRT.anchorMax = Vector2.one;

                    var tmp = txtObj.GetComponent<TextMeshProUGUI>();
                    tmp.fontSize = 12;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0.98f, 0.88f, 0.60f); // Gold foil lettering
                    tmp.text = "<b>WIZARD'S COMPANION [B]</b>";
                    tmp.raycastTarget = false;

                    expandRibbonButton = tabGo.GetComponent<Button>();
                    expandRibbonButton.onClick.RemoveAllListeners();
                    expandRibbonButton.onClick.AddListener(() => SetBookOpen(true));
                }
                collapsedRibbonTab = tabGo;
            }

            // 2. Compact Illustrated Grimoire Frame (Sitting in upper-mid space, out of controls/menus)
            if (companionBookRoot == null)
            {
                var bookGo = GameObject.Find("WizardsCompanionSmallBook");
                if (bookGo == null)
                {
                    bookGo = new GameObject("WizardsCompanionSmallBook", typeof(RectTransform), typeof(Image));
                    bookGo.transform.SetParent(hudParent, false);

                    bookWindowRect = bookGo.GetComponent<RectTransform>();
                    // Occupies a small, dedicated section: x: 0.28 to 0.79, y: 0.58 to 0.89
                    // Away from player stats (left), party squad (right), casting circle (bottom), broadcast banner (top)
                    bookWindowRect.anchorMin = new Vector2(0.28f, 0.58f);
                    bookWindowRect.anchorMax = new Vector2(0.79f, 0.89f);
                    bookWindowRect.offsetMin = Vector2.zero;
                    bookWindowRect.offsetMax = Vector2.zero;

                    // Antique Leather Cover Frame
                    var coverImg = bookGo.GetComponent<Image>();
                    coverImg.color = new Color(0.24f, 0.15f, 0.09f, 0.98f);

                    // Inner Aged Parchment Page
                    var pageObj = new GameObject("ParchmentPage", typeof(RectTransform), typeof(Image));
                    pageObj.transform.SetParent(bookGo.transform, false);
                    var pRT = pageObj.GetComponent<RectTransform>();
                    pRT.anchorMin = Vector2.zero;
                    pRT.anchorMax = Vector2.one;
                    pRT.offsetMin = new Vector2(6f, 6f);
                    pRT.offsetMax = new Vector2(-6f, -6f);

                    var pImg = pageObj.GetComponent<Image>();
                    pImg.color = new Color(0.96f, 0.91f, 0.82f, 1.0f); // Aged warm parchment

                    // Gold Foil Filigree Header Bar
                    var headerBar = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
                    headerBar.transform.SetParent(pageObj.transform, false);
                    var hRT = headerBar.GetComponent<RectTransform>();
                    hRT.anchorMin = new Vector2(0f, 0.87f);
                    hRT.anchorMax = new Vector2(1f, 1f);
                    hRT.offsetMin = Vector2.zero;
                    hRT.offsetMax = Vector2.zero;
                    var hImg = headerBar.GetComponent<Image>();
                    hImg.color = new Color(0.35f, 0.23f, 0.14f, 0.95f);

                    var headerText = new GameObject("HeaderText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    headerText.transform.SetParent(headerBar.transform, false);
                    var htRT = headerText.GetComponent<RectTransform>();
                    htRT.anchorMin = Vector2.zero;
                    htRT.anchorMax = Vector2.one;
                    htRT.offsetMin = new Vector2(8f, 0f);
                    htRT.offsetMax = new Vector2(-28f, 0f);
                    bookHeaderTMP = headerText.GetComponent<TextMeshProUGUI>();
                    bookHeaderTMP.fontSize = 11;
                    bookHeaderTMP.alignment = TextAlignmentOptions.MidlineLeft;
                    bookHeaderTMP.color = new Color(0.98f, 0.85f, 0.45f);
                    bookHeaderTMP.text = "THE WIZARD'S COMPANION";

                    // Close / Minimize Ribbon Button
                    var minBtnObj = new GameObject("MinimizeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    minBtnObj.transform.SetParent(headerBar.transform, false);
                    var mbRT = minBtnObj.GetComponent<RectTransform>();
                    mbRT.anchorMin = new Vector2(1f, 0.5f);
                    mbRT.anchorMax = new Vector2(1f, 0.5f);
                    mbRT.sizeDelta = new Vector2(24f, 22f);
                    mbRT.anchoredPosition = new Vector2(-15f, 0f);
                    var mbImg = minBtnObj.GetComponent<Image>();
                    mbImg.color = new Color(0.85f, 0.30f, 0.20f, 0.85f);
                    minimizeButton = minBtnObj.GetComponent<Button>();
                    minimizeButton.onClick.RemoveAllListeners();
                    minimizeButton.onClick.AddListener(() => SetBookOpen(false));

                    var minTxt = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
                    minTxt.transform.SetParent(minBtnObj.transform, false);
                    var mtRT = minTxt.GetComponent<RectTransform>();
                    mtRT.anchorMin = Vector2.zero;
                    mtRT.anchorMax = Vector2.one;
                    var mtTMP = minTxt.GetComponent<TextMeshProUGUI>();
                    mtTMP.fontSize = 12;
                    mtTMP.alignment = TextAlignmentOptions.Center;
                    mtTMP.color = Color.white;
                    mtTMP.text = "X";

                    // Spell Title & Meta (Underneath Header)
                    var titleObj = new GameObject("SpellTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleObj.transform.SetParent(pageObj.transform, false);
                    var tRT = titleObj.GetComponent<RectTransform>();
                    tRT.anchorMin = new Vector2(0.04f, 0.77f);
                    tRT.anchorMax = new Vector2(0.96f, 0.87f);
                    tRT.offsetMin = Vector2.zero;
                    tRT.offsetMax = Vector2.zero;
                    spellTitleTMP = titleObj.GetComponent<TextMeshProUGUI>();
                    spellTitleTMP.fontSize = 14;
                    spellTitleTMP.color = new Color(0.22f, 0.15f, 0.08f);

                    var metaObj = new GameObject("SpellMeta", typeof(RectTransform), typeof(TextMeshProUGUI));
                    metaObj.transform.SetParent(pageObj.transform, false);
                    var mRT = metaObj.GetComponent<RectTransform>();
                    mRT.anchorMin = new Vector2(0.04f, 0.69f);
                    mRT.anchorMax = new Vector2(0.96f, 0.77f);
                    mRT.offsetMin = Vector2.zero;
                    mRT.offsetMax = Vector2.zero;
                    spellMetaTMP = metaObj.GetComponent<TextMeshProUGUI>();
                    spellMetaTMP.fontSize = 10;
                    spellMetaTMP.color = new Color(0.35f, 0.28f, 0.20f);

                    // Diagram Canvas (Middle of small book)
                    var diagObj = new GameObject("DiagramCanvas", typeof(RectTransform), typeof(Image));
                    diagObj.transform.SetParent(pageObj.transform, false);
                    diagramCanvasRect = diagObj.GetComponent<RectTransform>();
                    diagramCanvasRect.anchorMin = new Vector2(0.06f, 0.27f);
                    diagramCanvasRect.anchorMax = new Vector2(0.94f, 0.68f);
                    diagramCanvasRect.offsetMin = Vector2.zero;
                    diagramCanvasRect.offsetMax = Vector2.zero;
                    var dImg = diagObj.GetComponent<Image>();
                    dImg.color = new Color(0.92f, 0.87f, 0.77f, 0.65f); // Subtle parchment inset

                    // Instructions & Special Mechanic Text (Bottom of page)
                    var instrObj = new GameObject("CastingInstructions", typeof(RectTransform), typeof(TextMeshProUGUI));
                    instrObj.transform.SetParent(pageObj.transform, false);
                    var iRT = instrObj.GetComponent<RectTransform>();
                    iRT.anchorMin = new Vector2(0.04f, 0.11f);
                    iRT.anchorMax = new Vector2(0.96f, 0.26f);
                    iRT.offsetMin = Vector2.zero;
                    iRT.offsetMax = Vector2.zero;
                    castingInstructionsTMP = instrObj.GetComponent<TextMeshProUGUI>();
                    castingInstructionsTMP.fontSize = 9.5f;
                    castingInstructionsTMP.color = new Color(0.18f, 0.14f, 0.08f);

                    // Bottom Navigation Bar: [< Prev] [Page 1/5] [Next >]
                    var navBar = new GameObject("NavBar", typeof(RectTransform));
                    navBar.transform.SetParent(pageObj.transform, false);
                    var nbRT = navBar.GetComponent<RectTransform>();
                    nbRT.anchorMin = new Vector2(0.02f, 0.01f);
                    nbRT.anchorMax = new Vector2(0.98f, 0.10f);
                    nbRT.offsetMin = Vector2.zero;
                    nbRT.offsetMax = Vector2.zero;

                    // Prev Button
                    var prevObj = new GameObject("PrevBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    prevObj.transform.SetParent(navBar.transform, false);
                    var pBtnRT = prevObj.GetComponent<RectTransform>();
                    pBtnRT.anchorMin = new Vector2(0f, 0f);
                    pBtnRT.anchorMax = new Vector2(0.28f, 1f);
                    pBtnRT.offsetMin = Vector2.zero;
                    pBtnRT.offsetMax = Vector2.zero;
                    prevObj.GetComponent<Image>().color = new Color(0.35f, 0.24f, 0.14f, 0.90f);
                    prevPageButton = prevObj.GetComponent<Button>();
                    prevPageButton.onClick.RemoveAllListeners();
                    prevPageButton.onClick.AddListener(PreviousPage);

                    var pTxt = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pTxt.transform.SetParent(prevObj.transform, false);
                    var ptRT = pTxt.GetComponent<RectTransform>();
                    ptRT.anchorMin = Vector2.zero;
                    ptRT.anchorMax = Vector2.one;
                    var ptTMP = pTxt.GetComponent<TextMeshProUGUI>();
                    ptTMP.fontSize = 10;
                    ptTMP.alignment = TextAlignmentOptions.Center;
                    ptTMP.color = Color.white;
                    ptTMP.text = "< PREV";

                    // Next Button
                    var nextObj = new GameObject("NextBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                    nextObj.transform.SetParent(navBar.transform, false);
                    var nBtnRT = nextObj.GetComponent<RectTransform>();
                    nBtnRT.anchorMin = new Vector2(0.72f, 0f);
                    nBtnRT.anchorMax = new Vector2(1f, 1f);
                    nBtnRT.offsetMin = Vector2.zero;
                    nBtnRT.offsetMax = Vector2.zero;
                    nextObj.GetComponent<Image>().color = new Color(0.35f, 0.24f, 0.14f, 0.90f);
                    nextPageButton = nextObj.GetComponent<Button>();
                    nextPageButton.onClick.RemoveAllListeners();
                    nextPageButton.onClick.AddListener(NextPage);

                    var nTxt = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    nTxt.transform.SetParent(nextObj.transform, false);
                    var ntRT = nTxt.GetComponent<RectTransform>();
                    ntRT.anchorMin = Vector2.zero;
                    ntRT.anchorMax = Vector2.one;
                    var ntTMP = nTxt.GetComponent<TextMeshProUGUI>();
                    ntTMP.fontSize = 10;
                    ntTMP.alignment = TextAlignmentOptions.Center;
                    ntTMP.color = Color.white;
                    ntTMP.text = "NEXT >";

                    // Page Counter
                    var pcObj = new GameObject("PageCounter", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pcObj.transform.SetParent(navBar.transform, false);
                    var pcRT = pcObj.GetComponent<RectTransform>();
                    pcRT.anchorMin = new Vector2(0.30f, 0f);
                    pcRT.anchorMax = new Vector2(0.70f, 1f);
                    pcRT.offsetMin = Vector2.zero;
                    pcRT.offsetMax = Vector2.zero;
                    pageCounterTMP = pcObj.GetComponent<TextMeshProUGUI>();
                    pageCounterTMP.fontSize = 9.5f;
                    pageCounterTMP.alignment = TextAlignmentOptions.Center;
                    pageCounterTMP.color = new Color(0.30f, 0.22f, 0.14f);
                }
                companionBookRoot = bookGo;
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GrimoireAssistantUI provides an ancient arcane two-page grimoire overlay
/// allowing Spellcasters and Bards to inspect their equipped books, study rune stroke
/// guides / melody note sequences, and immediately prime spells into the casting flow.
/// </summary>
public class GrimoireAssistantUI : MonoBehaviour
{
    private static GrimoireAssistantUI _instance;
    public static GrimoireAssistantUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GrimoireAssistantUI>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Root Canvas & Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private RectTransform bookFrameRect;

    [Header("Book Tabs Header")]
    [SerializeField] private Transform tabsContainer;

    [Header("Left Page Elements (Book Lore & Spell List)")]
    [SerializeField] private TextMeshProUGUI bookTitleTMP;
    [SerializeField] private TextMeshProUGUI bookAuthorTMP;
    [SerializeField] private TextMeshProUGUI bookDescriptionTMP;
    [SerializeField] private Transform spellListContainer;

    [Header("Right Page Elements (Spellcraft Assistant)")]
    [SerializeField] private TextMeshProUGUI selectedSpellNameTMP;
    [SerializeField] private TextMeshProUGUI selectedSpellElementTMP;
    [SerializeField] private TextMeshProUGUI selectedSpellCostTMP;
    [SerializeField] private TextMeshProUGUI selectedSpellLoreTMP;
    [SerializeField] private TextMeshProUGUI gestureHeaderTMP;
    [SerializeField] private TextMeshProUGUI gestureInstructionsTMP;
    [SerializeField] private RectTransform runeCanvasRect;
    [SerializeField] private Button primeSpellButton;
    [SerializeField] private TextMeshProUGUI primeButtonTMP;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    private GrimoireSpellEntry currentSelectedSpell;
    private readonly List<GameObject> activeTabButtons = new List<GameObject>();
    private readonly List<GameObject> activeSpellButtons = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => SetVisible(false));
        }
        if (primeSpellButton != null)
        {
            primeSpellButton.onClick.AddListener(OnPrimeSpellClicked);
        }
    }

    private void Start()
    {
        if (overlayRoot == null) overlayRoot = gameObject;
        // Start closed by default
        SetVisible(false);
    }

    public void ToggleVisibility()
    {
        bool newState = !overlayRoot.activeSelf;
        SetVisible(newState);
    }

    public void SetVisible(bool visible)
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(visible);
        }

        if (visible)
        {
            RefreshCurrentBook();
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.9f);
            }
        }
        else
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.8f);
            }
        }
    }

    public void RefreshCurrentBook()
    {
        if (SpellbookManager.Instance == null) return;

        var books = SpellbookManager.Instance.EquippedBooks;
        int activeIdx = SpellbookManager.Instance.ActiveBookIndex;

        // 1. Rebuild Tabs
        RebuildBookTabs(books, activeIdx);

        // 2. Populate Left Page
        var activeBook = SpellbookManager.Instance.ActiveBook;
        if (activeBook != null)
        {
            if (bookTitleTMP != null) bookTitleTMP.text = activeBook.bookTitle.ToUpper();
            if (bookAuthorTMP != null) bookAuthorTMP.text = $"By {activeBook.authorLore}";
            if (bookDescriptionTMP != null) bookDescriptionTMP.text = activeBook.description;

            RebuildSpellList(activeBook.inscribedSpells);

            if (activeBook.inscribedSpells.Count > 0)
            {
                SelectSpell(activeBook.inscribedSpells[0]);
            }
        }
    }

    private void RebuildBookTabs(IReadOnlyList<SpellbookItem> books, int activeIdx)
    {
        if (tabsContainer == null) return;

        for (int i = 0; i < activeTabButtons.Count; i++)
        {
            if (activeTabButtons[i] != null) Destroy(activeTabButtons[i]);
        }
        activeTabButtons.Clear();

        for (int i = 0; i < books.Count; i++)
        {
            int idx = i;
            var book = books[i];

            GameObject tabObj = new GameObject($"Tab_{book.bookId}", typeof(RectTransform), typeof(Image), typeof(Button));
            tabObj.transform.SetParent(tabsContainer, false);
            activeTabButtons.Add(tabObj);

            RectTransform rt = tabObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(170f, 44f);

            Image img = tabObj.GetComponent<Image>();
            bool isCurrent = (idx == activeIdx);
            img.color = isCurrent ? book.themeColor : new Color(0.18f, 0.20f, 0.26f, 0.85f);

            Button btn = tabObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                SpellbookManager.Instance.SelectBook(idx);
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("tab_click", 0.8f);
                }
            });

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(tabObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(4f, 2f);
            txtRt.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = book.bookTitle;
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isCurrent ? Color.white : new Color(0.8f, 0.85f, 0.9f);
        }
    }

    private void RebuildSpellList(List<GrimoireSpellEntry> spells)
    {
        if (spellListContainer == null) return;

        for (int i = 0; i < activeSpellButtons.Count; i++)
        {
            if (activeSpellButtons[i] != null) Destroy(activeSpellButtons[i]);
        }
        activeSpellButtons.Clear();

        for (int i = 0; i < spells.Count; i++)
        {
            var spell = spells[i];
            GameObject spellBtnObj = new GameObject($"SpellBtn_{spell.spellId}", typeof(RectTransform), typeof(Image), typeof(Button));
            spellBtnObj.transform.SetParent(spellListContainer, false);
            activeSpellButtons.Add(spellBtnObj);

            RectTransform rt = spellBtnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 52f);

            Image img = spellBtnObj.GetComponent<Image>();
            img.color = new Color(0.14f, 0.16f, 0.24f, 0.90f);

            Button btn = spellBtnObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                SelectSpell(spell);
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("button_click", 0.8f);
                }
            });

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(spellBtnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 4f);
            txtRt.offsetMax = new Vector2(-10f, -4f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{spell.spellName}</b>  <color=#00D8FF>[{spell.manaCost} MP]</color>\n<size=11><color=#FFD700>{spell.gestureShapeName}</color></size>";
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
        }
    }

    public void SelectSpell(GrimoireSpellEntry spell)
    {
        currentSelectedSpell = spell;
        if (spell == null) return;

        if (selectedSpellNameTMP != null) selectedSpellNameTMP.text = spell.spellName.ToUpper();
        if (selectedSpellElementTMP != null) selectedSpellElementTMP.text = $"ELEMENT: {spell.element.ToString().ToUpper()}";
        if (selectedSpellCostTMP != null) selectedSpellCostTMP.text = $"COST: {spell.manaCost} MP";
        if (selectedSpellLoreTMP != null) selectedSpellLoreTMP.text = spell.loreDescription;

        if (gestureHeaderTMP != null)
        {
            gestureHeaderTMP.text = $"<b>RUNIC GESTURE:</b> {spell.gestureShapeName}";
        }

        if (gestureInstructionsTMP != null)
        {
            gestureInstructionsTMP.text = spell.gestureInstructions;
        }

        if (primeButtonTMP != null)
        {
            primeButtonTMP.text = $"<b>[ PRIME {spell.spellName.ToUpper()} ]</b>";
        }

        DrawRuneStrokeGuide(spell);
    }

    private void DrawRuneStrokeGuide(GrimoireSpellEntry spell)
    {
        if (runeCanvasRect == null) return;

        // Clear previous guide visual children
        for (int i = runeCanvasRect.childCount - 1; i >= 0; i--)
        {
            Destroy(runeCanvasRect.GetChild(i).gameObject);
        }

        // Create visual stroke markers
        if (spell.guideStrokePoints != null && spell.guideStrokePoints.Length > 1)
        {
            float scale = 65f;
            for (int i = 0; i < spell.guideStrokePoints.Length; i++)
            {
                Vector2 pt = spell.guideStrokePoints[i] * scale;

                // Point pip
                GameObject pip = new GameObject($"Point_{i}", typeof(RectTransform), typeof(Image));
                pip.transform.SetParent(runeCanvasRect, false);
                RectTransform pRt = pip.GetComponent<RectTransform>();
                pRt.anchoredPosition = pt;
                pRt.sizeDelta = new Vector2(16f, 16f);
                Image pImg = pip.GetComponent<Image>();
                pImg.color = (i == 0) ? new Color(1f, 0.9f, 0.2f) : new Color(0.2f, 0.9f, 1f);

                // Connecting stroke segment to next point
                if (i < spell.guideStrokePoints.Length - 1)
                {
                    Vector2 nextPt = spell.guideStrokePoints[i + 1] * scale;
                    Vector2 diff = nextPt - pt;
                    float dist = diff.magnitude;
                    float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                    GameObject line = new GameObject($"Stroke_{i}", typeof(RectTransform), typeof(Image));
                    line.transform.SetParent(runeCanvasRect, false);
                    RectTransform lRt = line.GetComponent<RectTransform>();
                    lRt.anchoredPosition = pt + diff * 0.5f;
                    lRt.sizeDelta = new Vector2(dist, 4f);
                    lRt.localRotation = Quaternion.Euler(0f, 0f, angle);
                    Image lImg = line.GetComponent<Image>();
                    lImg.color = new Color(0.2f, 0.85f, 1f, 0.75f);
                }
            }
        }
    }

    private void OnPrimeSpellClicked()
    {
        if (currentSelectedSpell != null && SpellbookManager.Instance != null)
        {
            SpellbookManager.Instance.PrimeSpellAssisted(currentSelectedSpell);
        }
    }

    public void BindReferences(
        GameObject root,
        RectTransform frame,
        Transform tabs,
        TextMeshProUGUI bTitle,
        TextMeshProUGUI bAuthor,
        TextMeshProUGUI bDesc,
        Transform spellContainer,
        TextMeshProUGUI sName,
        TextMeshProUGUI sElem,
        TextMeshProUGUI sCost,
        TextMeshProUGUI sLore,
        TextMeshProUGUI gHeader,
        TextMeshProUGUI gInstr,
        RectTransform rCanvas,
        Button primeBtn,
        TextMeshProUGUI primeBtnTxt,
        Button closeBtn)
    {
        overlayRoot = root;
        bookFrameRect = frame;
        tabsContainer = tabs;
        bookTitleTMP = bTitle;
        bookAuthorTMP = bAuthor;
        bookDescriptionTMP = bDesc;
        spellListContainer = spellContainer;
        selectedSpellNameTMP = sName;
        selectedSpellElementTMP = sElem;
        selectedSpellCostTMP = sCost;
        selectedSpellLoreTMP = sLore;
        gestureHeaderTMP = gHeader;
        gestureInstructionsTMP = gInstr;
        runeCanvasRect = rCanvas;
        primeSpellButton = primeBtn;
        primeButtonTMP = primeBtnTxt;
        closeButton = closeBtn;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => SetVisible(false));
        }
        if (primeSpellButton != null)
        {
            primeSpellButton.onClick.RemoveAllListeners();
            primeSpellButton.onClick.AddListener(OnPrimeSpellClicked);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerAccessoryMenuUI : MonoBehaviour
{
    private static PlayerAccessoryMenuUI instance;
    public static PlayerAccessoryMenuUI Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<PlayerAccessoryMenuUI>();
            return instance;
        }
        private set { instance = value; }
    }

    [Header("Spatial Anchors (Top-Left Under Player Vitals)")]
    [SerializeField] private RectTransform hudRibbonContainer;
    [SerializeField] private RectTransform expandedDrawerModal;
    [SerializeField] private ScrollRect infiniteScrollRect;
    [SerializeField] private GridLayoutGroup drawerGridLayout;

    [Header("Prefabs & References")]
    [SerializeField] private GameObject accessorySlotPrefab;
    [SerializeField] private int maxRibbonPreviewIcons = 5;
    [SerializeField] private Button ribbonExpandButton;
    [SerializeField] private Button messengerBagButton;

    [Header("Telemetry Tooltip Inspector")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipTitleText;
    [SerializeField] private TextMeshProUGUI tooltipMathText;
    [SerializeField] private TextMeshProUGUI tooltipLoreText;
    [SerializeField] private Button tooltipCloseButton;

    private Dictionary<string, GameObject> spawnedDrawerSlots = new Dictionary<string, GameObject>();
    private List<GameObject> spawnedRibbonIcons = new List<GameObject>();
    private bool isDrawerOpen = false;

    public bool IsDrawerOpen => isDrawerOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureHierarchySetup();
    }

    private void Start()
    {
        SnapAnchorsToPortraitSpec();

        if (UncappedAccessorySystem.Instance != null)
        {
            UncappedAccessorySystem.Instance.OnAccessoryBufferMutated += RefreshAccessoryUI;
            RebuildAllUI();
        }

        if (expandedDrawerModal != null) expandedDrawerModal.gameObject.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        BindExpandAndBagButtons();
    }

    private void OnDestroy()
    {
        if (UncappedAccessorySystem.Instance != null)
        {
            UncappedAccessorySystem.Instance.OnAccessoryBufferMutated -= RefreshAccessoryUI;
        }
    }

    public void SnapAnchorsToPortraitSpec()
    {
        // Mounts the collapsed ribbon directly beneath the Top-Left Player Stats (0.02, 0.70)
        if (hudRibbonContainer != null)
        {
            hudRibbonContainer.anchorMin = new Vector2(0.02f, 0.63f);
            hudRibbonContainer.anchorMax = new Vector2(0.38f, 0.69f);
            hudRibbonContainer.anchoredPosition = Vector2.zero;
            hudRibbonContainer.sizeDelta = Vector2.zero;
        }

        // Configures the expandable scrollable matrix drawer in the left-mid viewport
        if (expandedDrawerModal != null)
        {
            expandedDrawerModal.anchorMin = new Vector2(0.02f, 0.25f);
            expandedDrawerModal.anchorMax = new Vector2(0.55f, 0.62f);
            expandedDrawerModal.anchoredPosition = Vector2.zero;
            expandedDrawerModal.sizeDelta = Vector2.zero;
        }

        if (drawerGridLayout != null)
        {
            drawerGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            drawerGridLayout.constraintCount = 5;
            drawerGridLayout.cellSize = new Vector2(64f, 64f);
            drawerGridLayout.spacing = new Vector2(8f, 8f);
            drawerGridLayout.padding = new RectOffset(8, 8, 8, 8);
        }
    }

    private void BindExpandAndBagButtons()
    {
        if (ribbonExpandButton != null)
        {
            ribbonExpandButton.onClick.RemoveAllListeners();
            ribbonExpandButton.onClick.AddListener(ToggleAccessoryDrawer);
        }

        if (messengerBagButton == null)
        {
            var bagObj = GameObject.Find("MessengerBagButton");
            if (bagObj != null) messengerBagButton = bagObj.GetComponent<Button>();
        }

        if (messengerBagButton != null)
        {
            messengerBagButton.onClick.AddListener(ToggleAccessoryDrawer);
        }

        if (tooltipCloseButton != null)
        {
            tooltipCloseButton.onClick.RemoveAllListeners();
            tooltipCloseButton.onClick.AddListener(CloseTooltip);
        }
    }

    public void ToggleAccessoryDrawer()
    {
        SetDrawerOpen(!isDrawerOpen);
    }

    public void SetDrawerOpen(bool open)
    {
        isDrawerOpen = open;
        if (expandedDrawerModal != null)
        {
            expandedDrawerModal.gameObject.SetActive(isDrawerOpen);
        }
        if (!isDrawerOpen && tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }

        if (isDrawerOpen)
        {
            RebuildAllUI();
        }
    }

    public void CloseTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private void SafeDestroy(GameObject obj)
    {
        if (obj == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(obj);
            return;
        }
#endif
        Destroy(obj);
    }

    public void RebuildAllUI()
    {
        if (UncappedAccessorySystem.Instance == null) return;

        var buffer = UncappedAccessorySystem.Instance.GetActiveBuffer();

        // 1. Clean existing drawer slot objects to avoid duplicates across domain reloads
        Transform contentParent = drawerGridLayout != null ? drawerGridLayout.transform : (infiniteScrollRect != null ? infiniteScrollRect.content : null);
        if (contentParent != null)
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(contentParent.GetChild(i).gameObject);
            }
        }
        spawnedDrawerSlots.Clear();

        // Re-populate unique slots in drawer
        foreach (var node in buffer)
        {
            if (node == null || node.definition == null || node.stackCount <= 0) continue;
            string id = node.definition.accessoryId;

            GameObject slotObj = InstantiateSlot(contentParent, 64f);
            spawnedDrawerSlots[id] = slotObj;
            ConfigureSlotVisuals(slotObj, node, true, 64f);
        }

        // 2. Rebuild compact ribbon
        RebuildCompactRibbon();
    }

    private void RefreshAccessoryUI(AccessoryStackNode mutatedNode)
    {
        if (mutatedNode == null || mutatedNode.definition == null) return;
        string id = mutatedNode.definition.accessoryId;

        // 1. Update or Instantiate Slot in Expanded Scroll Grid
        if (mutatedNode.stackCount <= 0)
        {
            if (spawnedDrawerSlots.TryGetValue(id, out GameObject slotObj))
            {
                if (slotObj != null) SafeDestroy(slotObj);
                spawnedDrawerSlots.Remove(id);
            }
        }
        else
        {
            if (!spawnedDrawerSlots.TryGetValue(id, out GameObject slotObj) || slotObj == null)
            {
                Transform parent = drawerGridLayout != null ? drawerGridLayout.transform : (infiniteScrollRect != null ? infiniteScrollRect.content : transform);
                slotObj = InstantiateSlot(parent, 64f);
                spawnedDrawerSlots[id] = slotObj;
            }
            ConfigureSlotVisuals(slotObj, mutatedNode, true, 64f);
        }

        // 2. Rebuild Compact Top-Left HUD Preview Ribbon
        RebuildCompactRibbon();
    }

    private void RebuildCompactRibbon()
    {
        if (hudRibbonContainer == null) return;

        // Clean up previous ribbon slots
        for (int i = hudRibbonContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = hudRibbonContainer.GetChild(i);
            if (child.name != "ExpandPlusButton")
            {
                SafeDestroy(child.gameObject);
            }
        }
        spawnedRibbonIcons.Clear();

        if (UncappedAccessorySystem.Instance == null) return;

        var activeList = UncappedAccessorySystem.Instance.GetActiveBuffer();
        int count = Mathf.Min(activeList.Count, maxRibbonPreviewIcons);

        for (int i = 0; i < count; i++)
        {
            if (activeList[i].stackCount <= 0) continue;
            GameObject ribbonSlot = InstantiateSlot(hudRibbonContainer, 36f);
            ConfigureSlotVisuals(ribbonSlot, activeList[i], false, 36f);
            spawnedRibbonIcons.Add(ribbonSlot);
        }

        // Ensure expand (+) button is present at end of ribbon
        EnsureRibbonExpandButton();
    }

    private void EnsureRibbonExpandButton()
    {
        if (hudRibbonContainer == null) return;

        if (ribbonExpandButton == null)
        {
            var btnTrans = hudRibbonContainer.Find("ExpandPlusButton");
            if (btnTrans != null)
            {
                ribbonExpandButton = btnTrans.GetComponent<Button>();
            }
            else
            {
                GameObject plusObj = new GameObject("ExpandPlusButton", typeof(RectTransform), typeof(Image), typeof(Button));
                plusObj.transform.SetParent(hudRibbonContainer, false);
                RectTransform rt = plusObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(36f, 36f);

                Image img = plusObj.GetComponent<Image>();
                img.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

                ribbonExpandButton = plusObj.GetComponent<Button>();

                // Add "+" text
                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(plusObj.transform, false);
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "+";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 24f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.95f, 0.85f, 0.4f, 1f);
            }
        }

        if (ribbonExpandButton != null)
        {
            ribbonExpandButton.transform.SetAsLastSibling();
            ribbonExpandButton.onClick.RemoveAllListeners();
            ribbonExpandButton.onClick.AddListener(ToggleAccessoryDrawer);
        }
    }

    private GameObject InstantiateSlot(Transform parent, float size = 64f)
    {
        GameObject slotObj;
        if (accessorySlotPrefab != null)
        {
            slotObj = Instantiate(accessorySlotPrefab, parent);
        }
        else
        {
            slotObj = CreateProceduralSlot(parent, size);
        }

        RectTransform rt = slotObj.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(size, size);

        return slotObj;
    }

    private GameObject CreateProceduralSlot(Transform parent, float size)
    {
        // Procedural fallback matching the exact spec:
        // root Button (64x64), child Image "Icon", bottom-right TextMeshProUGUI "StackBadge" with black outline
        GameObject slotObj = new GameObject("AccessorySlot", typeof(RectTransform), typeof(Image), typeof(Button));
        slotObj.transform.SetParent(parent, false);

        RectTransform slotRt = slotObj.GetComponent<RectTransform>();
        slotRt.sizeDelta = new Vector2(size, size);

        Image bgImg = slotObj.GetComponent<Image>();
        bgImg.color = new Color(0.14f, 0.16f, 0.22f, 0.92f);

        // Child 1: Icon (stretched to edges)
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(slotObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = Vector2.zero;
        iconRt.offsetMin = new Vector2(4f, 4f);
        iconRt.offsetMax = new Vector2(-4f, -4f);

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;

        // Child 2: StackBadge (bottom-right anchored TextMeshProUGUI with high-contrast outline)
        GameObject badgeObj = new GameObject("StackBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeObj.transform.SetParent(slotObj.transform, false);
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.35f, 0f);
        badgeRt.anchorMax = new Vector2(1f, 0.45f);
        badgeRt.anchoredPosition = Vector2.zero;
        badgeRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI badgeTMP = badgeObj.GetComponent<TextMeshProUGUI>();
        badgeTMP.alignment = TextAlignmentOptions.BottomRight;
        badgeTMP.fontSize = size < 48f ? 11f : 15f;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.color = new Color(1f, 0.92f, 0.45f, 1f);
        badgeTMP.outlineColor = Color.black;
        badgeTMP.outlineWidth = 0.28f;
        badgeTMP.textWrappingMode = TextWrappingModes.NoWrap;

        return slotObj;
    }

    private void ConfigureSlotVisuals(GameObject slotObj, AccessoryStackNode node, bool bindTooltip, float size = 64f)
    {
        if (slotObj == null || node == null) return;

        RectTransform slotRt = slotObj.GetComponent<RectTransform>();
        if (slotRt != null && size > 0f)
        {
            slotRt.sizeDelta = new Vector2(size, size);
        }

        Image iconImg = slotObj.transform.Find("Icon")?.GetComponent<Image>();
        TextMeshProUGUI badgeText = slotObj.transform.Find("StackBadge")?.GetComponent<TextMeshProUGUI>();
        Button slotBtn = slotObj.GetComponent<Button>();

        if (iconImg != null)
        {
            if (node.definition != null && node.definition.iconSprite != null)
            {
                iconImg.sprite = node.definition.iconSprite;
                iconImg.color = Color.white;
            }
            else
            {
                // Fallback tinted color if sprite not loaded
                iconImg.color = GetRarityColor(node.definition?.scalingType ?? ScalingCurveType.LinearAdditive);
            }
        }

        if (badgeText != null)
        {
            badgeText.text = node.stackCount > 1 ? $"x{node.stackCount}" : "";
            badgeText.fontSize = size < 48f ? 11f : 15f;
        }

        if (slotBtn != null)
        {
            slotBtn.onClick.RemoveAllListeners();
            if (bindTooltip)
            {
                slotBtn.onClick.AddListener(() => DisplayStackTelemetry(node));
            }
            else
            {
                slotBtn.onClick.AddListener(ToggleAccessoryDrawer);
            }
        }
    }

    public void DisplayStackTelemetry(AccessoryStackNode node)
    {
        if (node == null || node.definition == null) return;

        if (tooltipPanel == null)
        {
            EnsureTooltipHierarchy();
        }

        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(true);
        tooltipPanel.transform.SetAsLastSibling();

        float effectiveVal = node.GetEffectiveValue();
        if (tooltipTitleText != null)
        {
            tooltipTitleText.text = $"Item: {node.definition.accessoryName} <color=#FFD700>(x{node.stackCount})</color>";
        }

        string formulaSummary = node.definition.scalingType switch
        {
            ScalingCurveType.LinearAdditive => $"Linear (+{effectiveVal * 100f:F0}%)",
            ScalingCurveType.HyperbolicAsymptote => $"Hyperbolic (+{effectiveVal * 100f:F1}%)",
            ScalingCurveType.ExponentialCompound => $"Compound ({effectiveVal:F2}x)",
            _ => ""
        };

        if (tooltipMathText != null)
        {
            tooltipMathText.text = $"Scaling: {formulaSummary}";
        }

        if (tooltipLoreText != null)
        {
            tooltipLoreText.text = node.definition.loreDescription;
        }
    }

    private Color GetRarityColor(ScalingCurveType curve)
    {
        return curve switch
        {
            ScalingCurveType.LinearAdditive => new Color(0.4f, 0.85f, 1f, 1f),     // Cyan/Azure
            ScalingCurveType.HyperbolicAsymptote => new Color(0.9f, 0.45f, 1f, 1f), // Purple/Amethyst
            ScalingCurveType.ExponentialCompound => new Color(1f, 0.82f, 0.2f, 1f), // Legendary Gold
            _ => Color.white
        };
    }

    private void EnsureHierarchySetup()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        // Check if containers are already wired
        if (hudRibbonContainer == null)
        {
            var ribbonObj = transform.Find("AccessoryRibbonContainer");
            if (ribbonObj != null) hudRibbonContainer = ribbonObj.GetComponent<RectTransform>();
            else if (canvas != null)
            {
                var existing = canvas.transform.Find("MockupMasterHUD/AccessoryRibbonContainer") ?? canvas.transform.Find("AccessoryRibbonContainer");
                if (existing != null) hudRibbonContainer = existing.GetComponent<RectTransform>();
            }
        }

        if (expandedDrawerModal == null)
        {
            var drawerObj = transform.Find("AccessoryDrawerModal");
            if (drawerObj != null) expandedDrawerModal = drawerObj.GetComponent<RectTransform>();
            else if (canvas != null)
            {
                var existing = canvas.transform.Find("MockupMasterHUD/AccessoryDrawerModal") ?? canvas.transform.Find("AccessoryDrawerModal");
                if (existing != null) expandedDrawerModal = existing.GetComponent<RectTransform>();
            }
        }

        if (infiniteScrollRect == null && expandedDrawerModal != null)
        {
            infiniteScrollRect = expandedDrawerModal.GetComponentInChildren<ScrollRect>(true);
        }

        if (drawerGridLayout == null && expandedDrawerModal != null)
        {
            drawerGridLayout = expandedDrawerModal.GetComponentInChildren<GridLayoutGroup>(true);
        }

        if (tooltipPanel == null && expandedDrawerModal != null)
        {
            var tt = expandedDrawerModal.Find("TooltipInspectorPanel");
            if (tt != null) tooltipPanel = tt.gameObject;
        }

        EnsureTooltipHierarchy();
    }

    private void EnsureTooltipHierarchy()
    {
        if (tooltipPanel != null)
        {
            if (tooltipTitleText == null) tooltipTitleText = tooltipPanel.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (tooltipMathText == null) tooltipMathText = tooltipPanel.transform.Find("MathSummary")?.GetComponent<TextMeshProUGUI>();
            if (tooltipLoreText == null) tooltipLoreText = tooltipPanel.transform.Find("Lore")?.GetComponent<TextMeshProUGUI>();
            if (tooltipCloseButton == null) tooltipCloseButton = tooltipPanel.transform.Find("CloseButton")?.GetComponent<Button>();
        }
    }
}

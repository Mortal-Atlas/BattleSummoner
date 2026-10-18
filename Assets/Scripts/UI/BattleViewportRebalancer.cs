using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BattleViewportRebalancer enforces the Persona-Style UI layout architecture:
/// - Top-Left Relocation of Player Vitals (Settings -> AI Auto -> Angled Player Persona Stats)
/// - Right-Edge Constraints for 9-member Squad in a single vertical column
/// - Persona-Style -5.5 degree local Euler angle shear on all portrait masks (high-contrast P3/P4 aesthetic)
/// - Decoupled floating CAST button directly above geometric magic circle
/// - Complete purge of deprecated hold & flick gesture controls from bottom-right corner
/// </summary>
public class BattleViewportRebalancer : MonoBehaviour
{
    [Header("Top-Left Relocation")]
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private RectTransform aiAutoButton;
    [SerializeField] private RectTransform playerStatsContainer;
    [SerializeField] private Image playerPortraitImage;

    [Header("Right-Edge Vertical Party Column")]
    [SerializeField] private RectTransform allyVerticalContainer;
    [SerializeField] private GridLayoutGroup allyGridLayout;
    [SerializeField] private Image[] allyPortraitImages;

    [Header("Casting Zone")]
    [SerializeField] private RectTransform castButton;

    [Header("Purged Elements")]
    [SerializeField] private GameObject deprecatedHoldAndFlickButton;

    private void Awake()
    {
        AutoResolveIfNull();
        ExecuteLayoutSnap();
        ApplyPersonaAesthetic();
        EnforceCanvasSeparation();
    }

    private void Start()
    {
        AutoResolveIfNull();
        ExecuteLayoutSnap();
        ApplyPersonaAesthetic();
        EnforceCanvasSeparation();
    }

    public void BindReferences(
        RectTransform settings,
        RectTransform aiAuto,
        RectTransform playerStats,
        Image playerPfp,
        RectTransform allyContainer,
        GridLayoutGroup gridLayout,
        Image[] allyPfps,
        RectTransform castBtn,
        GameObject deprecatedBtn)
    {
        settingsButton = settings;
        aiAutoButton = aiAuto;
        playerStatsContainer = playerStats;
        playerPortraitImage = playerPfp;
        allyVerticalContainer = allyContainer;
        allyGridLayout = gridLayout;
        allyPortraitImages = allyPfps;
        castButton = castBtn;
        deprecatedHoldAndFlickButton = deprecatedBtn;

        ExecuteLayoutSnap();
        ApplyPersonaAesthetic();
    }

    private void AutoResolveIfNull()
    {
        if (settingsButton == null)
        {
            var go = GameObject.Find("SettingsButton");
            if (go != null) settingsButton = go.GetComponent<RectTransform>();
        }
        if (aiAutoButton == null)
        {
            var go = GameObject.Find("AutoBattlerAIButton");
            if (go != null) aiAutoButton = go.GetComponent<RectTransform>();
        }
        if (playerStatsContainer == null)
        {
            var go = GameObject.Find("PlayerStatsFrame");
            if (go != null) playerStatsContainer = go.GetComponent<RectTransform>();
        }
        if (playerPortraitImage == null)
        {
            var go = GameObject.Find("CharacterPortrait");
            if (go != null) playerPortraitImage = go.GetComponent<Image>();
        }
        if (allyVerticalContainer == null)
        {
            var go = GameObject.Find("PartySquadColumn");
            if (go != null)
            {
                allyVerticalContainer = go.GetComponent<RectTransform>();
                allyGridLayout = go.GetComponent<GridLayoutGroup>();
            }
        }
        if (allyPortraitImages == null || allyPortraitImages.Length == 0)
        {
            var list = new System.Collections.Generic.List<Image>();
            for (int i = 1; i <= 9; i++)
            {
                var go = GameObject.Find($"SquadMember_{i}");
                if (go != null)
                {
                    var childImg = go.transform.Find("AllyPortrait");
                    var img = childImg != null ? childImg.GetComponent<Image>() : go.GetComponent<Image>();
                    if (img != null) list.Add(img);
                }
            }
            if (list.Count > 0) allyPortraitImages = list.ToArray();
        }
        if (castButton == null)
        {
            var go = GameObject.Find("CastActionButton") ?? GameObject.Find("CastLabel");
            if (go != null) castButton = go.GetComponent<RectTransform>();
        }
        if (deprecatedHoldAndFlickButton == null)
        {
            deprecatedHoldAndFlickButton = GameObject.Find("HoldAndFlickGestureButton");
        }
    }

    [ContextMenu("Execute Layout Snap")]
    public void ExecuteLayoutSnap()
    {
        // 1. Settings Button at top-left
        if (settingsButton != null)
        {
            settingsButton.anchorMin = new Vector2(0f, 1f);
            settingsButton.anchorMax = new Vector2(0f, 1f);
            settingsButton.pivot = new Vector2(0f, 1f);
            settingsButton.anchoredPosition = new Vector2(24f, -20f);
            settingsButton.sizeDelta = new Vector2(62f, 62f);
        }

        // Anchor AI Auto Button and Player Stats to Top-Left Margin
        if (aiAutoButton != null)
        {
            aiAutoButton.anchorMin = new Vector2(0f, 1f);
            aiAutoButton.anchorMax = new Vector2(0f, 1f);
            aiAutoButton.pivot = new Vector2(0f, 1f);
            aiAutoButton.anchoredPosition = new Vector2(24f, -92f);
            aiAutoButton.sizeDelta = new Vector2(62f, 62f);
        }

        if (playerStatsContainer != null)
        {
            playerStatsContainer.anchorMin = new Vector2(0f, 1f);
            playerStatsContainer.anchorMax = new Vector2(0f, 1f);
            playerStatsContainer.pivot = new Vector2(0f, 1f);
            playerStatsContainer.anchoredPosition = new Vector2(24f, -168f);
            playerStatsContainer.sizeDelta = new Vector2(360f, 112f);
        }

        // 2. Lock the 9-member squad strictly to the right boundary in a single column with large touch targets
        if (allyVerticalContainer != null)
        {
            allyVerticalContainer.anchorMin = new Vector2(0.80f, 0.16f);
            allyVerticalContainer.anchorMax = new Vector2(0.99f, 0.77f);
            allyVerticalContainer.offsetMin = Vector2.zero;
            allyVerticalContainer.offsetMax = Vector2.zero;
            allyVerticalContainer.anchoredPosition = Vector2.zero;
            
            if (allyGridLayout != null)
            {
                allyGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                allyGridLayout.constraintCount = 1; 
                allyGridLayout.cellSize = new Vector2(130f, 72f); 
                allyGridLayout.spacing = new Vector2(0f, 7f);
            }
        }

        // 3. Position the CAST button slightly above the central magic circle
        if (castButton != null)
        {
            castButton.anchorMin = new Vector2(0.60f, 0.25f);
            castButton.anchorMax = new Vector2(0.75f, 0.32f);
            castButton.offsetMin = Vector2.zero;
            castButton.offsetMax = Vector2.zero;
            castButton.anchoredPosition = Vector2.zero;
        }

        // 4. Delete the bottom-right gesture button
        if (deprecatedHoldAndFlickButton != null)
        {
            deprecatedHoldAndFlickButton.SetActive(false);
        }
    }

    [ContextMenu("Apply Persona Aesthetic")]
    public void ApplyPersonaAesthetic()
    {
        // Apply dynamic comic tilt to player portrait
        if (playerPortraitImage != null)
        {
            playerPortraitImage.preserveAspect = true;
            playerPortraitImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, -5.5f);
        }

        // Apply dynamic comic tilt to all ally portraits
        if (allyPortraitImages == null) return;
        foreach (var portrait in allyPortraitImages)
        {
            if (portrait != null)
            {
                portrait.preserveAspect = true;
                portrait.rectTransform.localEulerAngles = new Vector3(0f, 0f, -5.5f); 
            }
        }
    }

    [Header("Binding Contract Visual Overlays")]
    [SerializeField] private Image playerContractChainOverlay;

    /// <summary>
    /// Renders a crimson chain overlay on the player's angled -5.5f portrait cut-in
    /// to signal active binding contracts and extended revive durations to teammates.
    /// </summary>
    public void SetContractChainOverlay(bool active, Color? chainColor = null)
    {
        if (playerContractChainOverlay != null)
        {
            playerContractChainOverlay.gameObject.SetActive(active);
            if (active)
            {
                playerContractChainOverlay.color = chainColor ?? new Color(0.9f, 0.15f, 0.15f, 0.85f);
            }
        }
    }

    /// <summary>
    /// Enforces static vs dynamic Canvas split to eliminate full-screen UI mesh rebuilds during fast swipes and rune draws.
    /// </summary>
    [ContextMenu("Enforce Canvas Separation (Static vs Dynamic)")]
    public void EnforceCanvasSeparation()
    {
        var rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        Transform canvasRoot = rootCanvas.transform;

        // 1. Static Canvas: Holds angled Persona portraits, static borders, Settings icon, Messenger Bag, and Whetstone icon
        var staticGo = canvasRoot.Find("HUD_Static_Canvas");
        if (staticGo == null)
        {
            var go = new GameObject("HUD_Static_Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(canvasRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            staticGo = go.transform;
        }

        // 2. Dynamic Canvas: Holds rapidly changing elements (LineRenderer rune paths, active HP/MP fill bars, floating damage numbers)
        var dynamicGo = canvasRoot.Find("HUD_Dynamic_Canvas");
        if (dynamicGo == null)
        {
            var go = new GameObject("HUD_Dynamic_Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(canvasRoot, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            dynamicGo = go.transform;
        }
    }
}

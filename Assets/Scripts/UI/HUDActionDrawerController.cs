using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUDActionDrawerController manages a collapsible slide-out drawer containing secondary belt actions
/// (Whetstone, Mining Chisel, Expeditions, Mana Draft, Store, Inspect Relic, Tribunal, Ask Margo, Kalimba, Summon, Campfire).
/// Opens/closes when the player taps the drawer tab positioned above the bottom joystick/camera control deck.
/// </summary>
public class HUDActionDrawerController : MonoBehaviour
{
    public static HUDActionDrawerController Instance { get; private set; }

    [Header("Drawer Components")]
    [SerializeField] private Button drawerTabButton;
    [SerializeField] private TextMeshProUGUI tabLabelTMP;
    [SerializeField] private RectTransform drawerPanelRect;
    [SerializeField] private CanvasGroup drawerCanvasGroup;

    [Header("Slide Animation Settings")]
    [SerializeField] private float animationDuration = 0.22f;
    [SerializeField] private Vector2 closedAnchoredPosition = new Vector2(0f, -420f);
    [SerializeField] private Vector2 openAnchoredPosition = new Vector2(0f, 0f);

    private bool isOpen = false;
    private Coroutine slideCoroutine;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (drawerTabButton != null)
        {
            drawerTabButton.onClick.AddListener(ToggleDrawer);
        }
    }

    private void Start()
    {
        // Start closed by default so screen remains clean
        SetDrawerStateImmediate(false);
    }

    private void Update()
    {
        // Android Back button or Escape closes open drawer
        if (isOpen && (Input.GetKeyDown(KeyCode.Escape)))
        {
            CloseDrawer();
        }
    }

    public void ToggleDrawer()
    {
        if (isOpen) CloseDrawer();
        else OpenDrawer();
    }

    public void OpenDrawer()
    {
        isOpen = true;
        UpdateTabVisuals();
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(AnimateDrawer(true));

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.6f);
        }
    }

    public void CloseDrawer()
    {
        isOpen = false;
        UpdateTabVisuals();
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(AnimateDrawer(false));
    }

    private void UpdateTabVisuals()
    {
        if (tabLabelTMP != null)
        {
            tabLabelTMP.text = isOpen 
                ? "<color=#FFCC00>▼</color> CLOSE ACTIONS DRAWER <color=#FFCC00>▼</color>"
                : "<color=#BA55D3>▲</color> ACTIONS & BELT DRAWER <color=#BA55D3>▲</color>";
        }
    }

    private void SetDrawerStateImmediate(bool open)
    {
        isOpen = open;
        UpdateTabVisuals();

        if (drawerPanelRect != null)
        {
            drawerPanelRect.anchoredPosition = open ? openAnchoredPosition : closedAnchoredPosition;
        }

        if (drawerCanvasGroup != null)
        {
            drawerCanvasGroup.alpha = open ? 1f : 0f;
            drawerCanvasGroup.blocksRaycasts = open;
            drawerCanvasGroup.interactable = open;
        }
    }

    private IEnumerator AnimateDrawer(bool targetOpen)
    {
        if (drawerPanelRect == null || drawerCanvasGroup == null) yield break;

        Vector2 startPos = drawerPanelRect.anchoredPosition;
        Vector2 endPos = targetOpen ? openAnchoredPosition : closedAnchoredPosition;
        float startAlpha = drawerCanvasGroup.alpha;
        float endAlpha = targetOpen ? 1f : 0f;

        drawerCanvasGroup.blocksRaycasts = targetOpen;
        drawerCanvasGroup.interactable = targetOpen;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            drawerPanelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            drawerCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, smoothT);
            yield return null;
        }

        drawerPanelRect.anchoredPosition = endPos;
        drawerCanvasGroup.alpha = endAlpha;
    }
}

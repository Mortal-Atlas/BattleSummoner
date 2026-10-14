using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// SceneTransitionManager handles smooth cinematic fade-to-black scene transitions
/// between Overworld, Building Interiors, and Dungeons.
/// Features:
/// - Persistent singleton across all scenes (DontDestroyOnLoad).
/// - Dynamic screen fade canvas overlay (black full-screen CanvasGroup).
/// - Position positioning: places the player at the exact target SceneEntranceMarker.
/// - Synchronizes PortraitARPGCamera tracking and orientation.
/// - Audio and toast feedback during doorways and zone transitions.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<SceneTransitionManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    _instance = go.AddComponent<SceneTransitionManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.45f;
    [SerializeField] private string defaultTransitionAudio = "relic_chime";

    [Header("UI References")]
    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Image fadeImage;
    [SerializeField] private TextMeshProUGUI transitionTitleTMP;

    private bool isTransitioning = false;
    public bool IsTransitioning => isTransitioning;

    public event Action<string> OnTransitionStarted;
    public event Action<string> OnTransitionCompleted;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureFadeOverlay();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureFadeOverlay()
    {
        if (fadeCanvas != null && fadeCanvasGroup != null) return;

        // Create overlay canvas dynamically
        GameObject canvasGo = new GameObject("ScreenFadeOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        fadeCanvas = canvasGo.GetComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;

        fadeCanvasGroup = canvasGo.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        // Full-screen black image
        GameObject imgGo = new GameObject("BlackFadeImage", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform imgRt = imgGo.GetComponent<RectTransform>();
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.sizeDelta = Vector2.zero;
        imgRt.anchoredPosition = Vector2.zero;

        fadeImage = imgGo.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;

        // Transition Title Banner (e.g. "The Wandering Wyvern Tavern")
        GameObject textGo = new GameObject("TransitionTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.1f, 0.45f);
        textRt.anchorMax = new Vector2(0.9f, 0.55f);
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        transitionTitleTMP = textGo.GetComponent<TextMeshProUGUI>();
        transitionTitleTMP.text = "";
        transitionTitleTMP.fontSize = 24;
        transitionTitleTMP.alignment = TextAlignmentOptions.Center;
        transitionTitleTMP.color = new Color(0.95f, 0.85f, 0.45f);
        transitionTitleTMP.raycastTarget = false;
    }

    /// <summary>
    /// Executes a smooth cinematic fade to black, loads the target scene,
    /// places the player at the target entrance marker, and fades back in.
    /// </summary>
    public void TransitionToScene(string sceneName, string targetSpawnMarkerId = null, string locationTitle = null, Vector3? fallbackSpawnPos = null)
    {
        if (isTransitioning) return;
        StartCoroutine(DoSceneTransition(sceneName, targetSpawnMarkerId, locationTitle, fallbackSpawnPos));
    }

    private IEnumerator DoSceneTransition(string sceneName, string targetSpawnMarkerId, string locationTitle, Vector3? fallbackSpawnPos)
    {
        isTransitioning = true;
        EnsureFadeOverlay();
        fadeCanvasGroup.blocksRaycasts = true;

        OnTransitionStarted?.Invoke(sceneName);

        if (transitionTitleTMP != null)
        {
            transitionTitleTMP.text = !string.IsNullOrEmpty(locationTitle) ? locationTitle : "";
        }

        // 1. Play door / transition SFX
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX(defaultTransitionAudio, 0.9f);
        }

        // 2. Fade to black (alpha: 0 -> 1)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // Brief hold at full black
        yield return new WaitForSecondsRealtime(0.1f);

        // 3. Load Target Scene Asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(sceneName);
            yield return null;
        }

        // Wait one extra frame for all Awake/Start in new scene to settle
        yield return new WaitForEndOfFrame();

        // 4. Reposition Player Hero at Target Spawn Marker
        RepositionPlayerAtMarker(targetSpawnMarkerId, fallbackSpawnPos);

        // 5. Brief hold before revealing new scene
        yield return new WaitForSecondsRealtime(0.12f);

        // 6. Fade from black (alpha: 1 -> 0)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        if (transitionTitleTMP != null)
        {
            transitionTitleTMP.text = "";
        }

        isTransitioning = false;
        OnTransitionCompleted?.Invoke(sceneName);

        // Show welcome toast in new scene
        if (!string.IsNullOrEmpty(locationTitle) && DevClassLoadoutManager.Instance != null)
        {
            DevClassLoadoutManager.Instance.ShowToast($"<color=#FFD700>[{locationTitle.ToUpper()}]</color>\nEntered zone.");
        }
    }

    private void RepositionPlayerAtMarker(string markerId, Vector3? fallbackPos)
    {
        // Find Player in the new scene
        GameObject player = GameObject.Find("Player_Hero") ?? GameObject.FindWithTag("Player");
        if (player == null)
        {
            var movement = UnityEngine.Object.FindAnyObjectByType<PlayerMovementController>();
            if (movement != null) player = movement.gameObject;
        }

        if (player == null)
        {
            Debug.LogWarning("⚠️ [SCENE TRANSITION] Player_Hero not found in loaded scene!");
            return;
        }

        Vector3 targetPos = fallbackPos ?? player.transform.position;
        float targetYaw = player.transform.eulerAngles.y;

        if (!string.IsNullOrEmpty(markerId))
        {
            SceneEntranceMarker[] markers = UnityEngine.Object.FindObjectsByType<SceneEntranceMarker>(FindObjectsSortMode.None);
            SceneEntranceMarker matched = null;
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i].markerId == markerId)
                {
                    matched = markers[i];
                    break;
                }
            }

            if (matched != null)
            {
                targetPos = matched.transform.position;
                targetYaw = matched.facingYaw != 0f ? matched.facingYaw : matched.transform.eulerAngles.y;
            }
        }

        // Temporarily disable CharacterController so position assignment takes effect
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = targetPos;
        player.transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);

        if (cc != null) cc.enabled = true;

        // Snap camera to new player position
        Camera cam = Camera.main;
        if (cam != null)
        {
            var arpgCam = cam.GetComponent<PortraitARPGCamera>();
            if (arpgCam != null)
            {
                arpgCam.playerTarget = player.transform;
                Vector3 focusPoint = player.transform.position + Vector3.up * (arpgCam.targetFocusHeight + 0.75f);
                Quaternion camRot = Quaternion.Euler(arpgCam.pitchAngle, arpgCam.yawAngle, 0f);
                cam.transform.position = focusPoint - camRot * Vector3.forward * arpgCam.cameraDistance;
                cam.transform.rotation = camRot;
            }
        }

        Debug.Log($"🚪 [SCENE TRANSITION] Positioned Player at {targetPos} (marker: '{markerId}')");
    }

    [ContextMenu("Test Fade To Black")]
    public void TestFadeToBlack()
    {
        EnsureFadeOverlay();
        StartCoroutine(DoTestFade());
    }

    private IEnumerator DoTestFade()
    {
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(0.5f);
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
}

using UnityEngine;

/// <summary>
/// AndroidSafeAreaFitter guarantees that UI containers dynamically conform to
/// hardware display safe areas (preventing notches, camera punch-holes, dynamic islands,
/// and bottom gesture navigation bars from cutting off UI elements).
/// Works at runtime on Android / iOS and in the Unity Editor with [ExecuteAlways].
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AndroidSafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2 lastScreenResolution = Vector2.zero;

    [Header("Editor Notch & Cutout Simulation")]
    [Tooltip("Simulate a modern smartphone notch / dynamic island and bottom home bar in the Unity Editor.")]
    [SerializeField] private bool simulateNotchInEditor = false;
    [Range(0f, 0.12f)]
    [SerializeField] private float simulatedTopNotchRatio = 0.052f; // ~5.2% top cutout
    [Range(0f, 0.08f)]
    [SerializeField] private float simulatedBottomBarRatio = 0.038f; // ~3.8% bottom bar

    public bool SimulateNotchInEditor
    {
        get => simulateNotchInEditor;
        set
        {
            simulateNotchInEditor = value;
            ApplySafeArea();
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea || 
            !Mathf.Approximately(lastScreenResolution.x, Screen.width) || 
            !Mathf.Approximately(lastScreenResolution.y, Screen.height))
        {
            ApplySafeArea();
        }
    }

    public void ForceApplySafeArea()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        if (rectTransform == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        lastSafeArea = Screen.safeArea;
        lastScreenResolution = new Vector2(Screen.width, Screen.height);

        Rect safeArea = Screen.safeArea;
        float refWidth = Screen.width;
        float refHeight = Screen.height;

        // In Editor simulation mode (when Screen.safeArea is full screen)
        if (Application.isEditor && simulateNotchInEditor)
        {
            float topInset = refHeight * simulatedTopNotchRatio;
            float botInset = refHeight * simulatedBottomBarRatio;
            safeArea = new Rect(0f, botInset, refWidth, Mathf.Max(10f, refHeight - topInset - botInset));
        }
        else
        {
            // If safeArea dimensions match native screen currentResolution rather than rendered viewport,
            // normalize against currentResolution:
            if (safeArea.width > Screen.width * 1.05f || safeArea.height > Screen.height * 1.05f)
            {
                if (Screen.currentResolution.width > 0 && Screen.currentResolution.height > 0)
                {
                    refWidth = Screen.currentResolution.width;
                    refHeight = Screen.currentResolution.height;
                }
            }
        }

        Vector2 anchorMin = new Vector2(safeArea.x / refWidth, safeArea.y / refHeight);
        Vector2 anchorMax = new Vector2((safeArea.x + safeArea.width) / refWidth, (safeArea.y + safeArea.height) / refHeight);

        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp(anchorMax.x, anchorMin.x + 0.1f, 1f);
        anchorMax.y = Mathf.Clamp(anchorMax.y, anchorMin.y + 0.1f, 1f);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

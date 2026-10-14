using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// HighFidelityMobileResolutionEnforcer guarantees that the game renders at 100% native
/// device resolution without downsampling, blurry buffers, or low DPI scaling on mobile devices.
/// Enforces:
/// - Screen.SetResolution to native hardware display dimensions
/// - UniversalRenderPipelineAsset.renderScale = 1.0f
/// - ScalableBufferManager = 1.0f (no dynamic resolution downscaling)
/// - Target 60 FPS and 4x MSAA anti-aliasing
/// </summary>
[DefaultExecutionOrder(-1000)]
public class HighFidelityMobileResolutionEnforcer : MonoBehaviour
{
    public static HighFidelityMobileResolutionEnforcer Instance { get; private set; }

    [Header("Resolution Enforcement Configuration")]
    [SerializeField] private bool lockToPortrait = true;
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool forceNativeResolution = true;
    [SerializeField] private bool forceFullRenderScale = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PrewarmResolutionSettings()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        QualitySettings.resolutionScalingFixedDPIFactor = 1.0f;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ApplyHighFidelityResolution();
    }

    private void Start()
    {
        ApplyHighFidelityResolution();
    }

    public void ApplyHighFidelityResolution()
    {
        if (lockToPortrait)
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToPortrait = true;
        }

        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
        QualitySettings.resolutionScalingFixedDPIFactor = 1.0f;

        // Force native screen resolution on mobile devices (Android / iOS)
        if (forceNativeResolution && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer))
        {
            Resolution currentRes = Screen.currentResolution;
            if (currentRes.width > 0 && currentRes.height > 0)
            {
                // Ensure orientation width < height for portrait
                int w = Mathf.Min(currentRes.width, currentRes.height);
                int h = Mathf.Max(currentRes.width, currentRes.height);

                if (Screen.width != w || Screen.height != h)
                {
                    Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
                    Debug.Log($"[RESOLUTION ENFORCER] Screen resolution locked to native hardware: {w}x{h} ({Screen.dpi} DPI)");
                }
            }
        }

        // Enforce URP 1.0 render scale and crisp upscaling
        if (forceFullRenderScale)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = 1.0f;
                urp.msaaSampleCount = 4;
                urp.upscalingFilter = UpscalingFilterSelection.FSR;
                urp.fsrSharpness = 0.92f;
            }

            // Prevent dynamic resolution degradation
            ScalableBufferManager.ResizeBuffers(1.0f, 1.0f);
        }

        Debug.Log($"[RESOLUTION ENFORCER] Active viewport: {Screen.width}x{Screen.height} @ {Application.targetFrameRate} FPS. RenderScale: 1.0.");
    }
}

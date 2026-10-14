using System;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSummoner.Core
{
    /// <summary>
    /// AdaptiveScreenResolutionFitter guarantees that the entire game (3D world and 2D UI)
    /// scales dynamically across all modern smartphone and tablet screen sizes without anything
    /// getting cut off or cropped.
    ///
    /// Key Capabilities:
    /// 1. Dynamic CanvasScaler Adaptation:
    ///    - Tall Phones (aspect <= 9:16, e.g. iPhone 19.5:9, Galaxy 20:9, Xperia 21:9): Matches Width (1080)
    ///      so width is never clipped and vertical real estate naturally expands.
    ///    - Tablets / Foldables (aspect > 9:16, e.g. iPad 3:4, Android 16:10): Matches Height (1920)
    ///      so height is never squished and horizontal real estate expands.
    ///    - Mathematical Guarantee: Visible canvas is ALWAYS at least 1080px wide and 1920px tall!
    /// 2. Hardware Safe Area Enforcement:
    ///    - Prevents notches, camera punch-holes, dynamic islands, and home indicator bars from cutting
    ///      into HUD buttons (Settings, AI Auto, Spire Broadcast, Minimap).
    /// 3. 3D Camera Gate Fit:
    ///    - Locks horizontal gate fit so dungeon walls, enemies, and interactive altars never get cropped.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-950)]
    public class AdaptiveScreenResolutionFitter : MonoBehaviour
    {
        public static AdaptiveScreenResolutionFitter Instance { get; private set; }

        [Header("Target Reference Resolution")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private float referenceAspect = 9f / 16f; // 0.5625f

        [Header("Live Metrics (Read Only)")]
        [SerializeField] private string currentDeviceClass = "Standard 9:16";
        [SerializeField] private float currentAspectRatio = 0.5625f;
        [SerializeField] private float currentMatchFactor = 0f;
        [SerializeField] private Vector2 effectiveCanvasSize = new Vector2(1080f, 1920f);
        [SerializeField] private Rect currentSafeArea = Rect.zero;

        [Header("References")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private RectTransform mockupHudRect;
        [SerializeField] private RectTransform safeAreaContainerRect;
        [SerializeField] private Camera gameCamera;

        public float CurrentAspectRatio => currentAspectRatio;
        public Vector2 EffectiveCanvasSize => effectiveCanvasSize;

        public static event Action<Vector2, float> OnResolutionAdapted;

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private Rect lastSafeAreaRect = Rect.zero;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            ResolveReferences();
            ApplyAdaptiveScaling();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplyAdaptiveScaling();
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || 
                Screen.height != lastScreenHeight || 
                Screen.safeArea != lastSafeAreaRect)
            {
                ApplyAdaptiveScaling();
            }
        }

        public void ResolveReferences()
        {
            if (rootCanvas == null) rootCanvas = GetComponent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (rootCanvas != null && canvasScaler == null) canvasScaler = rootCanvas.GetComponent<CanvasScaler>();

            if (rootCanvas != null)
            {
                if (mockupHudRect == null)
                {
                    var hud = rootCanvas.transform.Find("MockupMasterHUD");
                    if (hud != null) mockupHudRect = hud.GetComponent<RectTransform>();
                }

                if (safeAreaContainerRect == null)
                {
                    var safe = rootCanvas.transform.Find("SafeAreaContainer");
                    if (safe != null) safeAreaContainerRect = safe.GetComponent<RectTransform>();
                }
            }

            if (gameCamera == null)
            {
                gameCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            }

            // Ensure AndroidSafeAreaFitter exists on MockupMasterHUD
            if (mockupHudRect != null)
            {
                var hudFitter = mockupHudRect.GetComponent<AndroidSafeAreaFitter>();
                if (hudFitter == null)
                {
                    hudFitter = mockupHudRect.gameObject.AddComponent<AndroidSafeAreaFitter>();
                }
            }

            // Ensure AndroidSafeAreaFitter exists on SafeAreaContainer
            if (safeAreaContainerRect != null)
            {
                var safeFitter = safeAreaContainerRect.GetComponent<AndroidSafeAreaFitter>();
                if (safeFitter == null)
                {
                    safeFitter = safeAreaContainerRect.gameObject.AddComponent<AndroidSafeAreaFitter>();
                }
            }
        }

        [ContextMenu("Apply Adaptive Scaling")]
        public void ApplyAdaptiveScaling()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeAreaRect = Screen.safeArea;
            currentSafeArea = Screen.safeArea;

            currentAspectRatio = (float)Screen.width / (float)Screen.height;

            // 1. Adapt CanvasScaler
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = referenceResolution;
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                // Golden Adaptive Rule:
                // If screen is wider than 9:16 (aspect > 0.5625f, like 3:4 tablets), match Height (1.0f)
                // If screen is taller than or equal to 9:16 (aspect <= 0.5625f, like 9:19.5 phones), match Width (0.0f)
                currentMatchFactor = (currentAspectRatio > referenceAspect + 0.005f) ? 1.0f : 0.0f;
                canvasScaler.matchWidthOrHeight = currentMatchFactor;

                // Calculate resulting effective canvas size
                if (currentMatchFactor < 0.5f)
                {
                    effectiveCanvasSize = new Vector2(
                        referenceResolution.x,
                        referenceResolution.x / currentAspectRatio
                    );
                }
                else
                {
                    effectiveCanvasSize = new Vector2(
                        referenceResolution.y * currentAspectRatio,
                        referenceResolution.y
                    );
                }
            }

            // 2. Classify Device Ratio for Telemetry
            if (currentAspectRatio < 0.44f) currentDeviceClass = "Ultra-Tall Phone (21:9 Xperia)";
            else if (currentAspectRatio < 0.48f) currentDeviceClass = "Modern Tall Phone (19.5:9 iPhone / Galaxy)";
            else if (currentAspectRatio < 0.53f) currentDeviceClass = "Tall Phone (18:9 Pixel)";
            else if (currentAspectRatio <= 0.58f) currentDeviceClass = "Standard Portrait (16:9 Phone)";
            else if (currentAspectRatio <= 0.65f) currentDeviceClass = "Tablet Wide (16:10 Android)";
            else currentDeviceClass = "Tablet / Foldable (3:4 iPad)";

            // 3. Configure Camera Gate Fit to prevent 3D world clipping
            if (gameCamera != null)
            {
                gameCamera.gateFit = Camera.GateFitMode.Horizontal;
            }

            // 4. Force update all safe area fitters
            if (mockupHudRect != null)
            {
                var hudFitter = mockupHudRect.GetComponent<AndroidSafeAreaFitter>();
                if (hudFitter != null) hudFitter.ForceApplySafeArea();
            }

            if (safeAreaContainerRect != null)
            {
                var safeFitter = safeAreaContainerRect.GetComponent<AndroidSafeAreaFitter>();
                if (safeFitter != null) safeFitter.ForceApplySafeArea();
            }

            // 5. Tune Menu Grids for Centering
            TuneMenuGridCentering();

            OnResolutionAdapted?.Invoke(effectiveCanvasSize, currentAspectRatio);
        }

        private void TuneMenuGridCentering()
        {
            if (rootCanvas == null) return;

            // Tune Inventory Slot Grid
            var invObj = rootCanvas.transform.Find("InventoryMenuOverlay");
            if (invObj != null)
            {
                var gridTrans = invObj.Find("SlotGridContainer");
                if (gridTrans != null)
                {
                    var grid = gridTrans.GetComponent<GridLayoutGroup>();
                    if (grid != null)
                    {
                        grid.childAlignment = TextAnchor.UpperCenter;
                        grid.cellSize = new Vector2(88f, 88f);
                        grid.spacing = new Vector2(12f, 12f);
                    }
                }
            }

            // Tune Item Shop Wares Grid
            var shopObj = rootCanvas.transform.Find("ItemShopOverlay");
            if (shopObj != null)
            {
                var gridTrans = shopObj.Find("WaresGridContainer");
                if (gridTrans != null)
                {
                    var grid = gridTrans.GetComponent<GridLayoutGroup>();
                    if (grid != null)
                    {
                        grid.childAlignment = TextAnchor.UpperCenter;
                        grid.cellSize = new Vector2(88f, 88f);
                        grid.spacing = new Vector2(12f, 12f);
                    }
                }
            }

            // Tune Tomodachi Social Hub
            var socialObj = rootCanvas.transform.Find("TomodachiSocialHubOverlay");
            if (socialObj != null)
            {
                var drawer = socialObj.Find("GiftDrawerOverlay");
                if (drawer != null)
                {
                    var giftGrid = drawer.Find("GiftGridContainer");
                    if (giftGrid != null)
                    {
                        var grid = giftGrid.GetComponent<GridLayoutGroup>();
                        if (grid != null)
                        {
                            grid.childAlignment = TextAnchor.UpperCenter;
                            grid.cellSize = new Vector2(250f, 64f);
                            grid.spacing = new Vector2(10f, 10f);
                        }
                    }
                }
            }

            // Tune Character Customization Overlay
            var customObj = rootCanvas.transform.Find("CharacterCustomizationOverlay");
            if (customObj != null)
            {
                var frame = customObj.Find("CustomizationFrame");
                if (frame != null)
                {
                    var frt = frame.GetComponent<RectTransform>();
                    if (frt != null)
                    {
                        if (currentAspectRatio <= 0.48f) // Tall phone 19.5:9
                        {
                            frt.anchorMin = new Vector2(0.02f, 0.04f);
                            frt.anchorMax = new Vector2(0.98f, 0.96f);
                        }
                        else if (currentAspectRatio >= 0.65f) // Tablet 3:4
                        {
                            frt.anchorMin = new Vector2(0.08f, 0.03f);
                            frt.anchorMax = new Vector2(0.92f, 0.97f);
                        }
                        else // Standard 16:9
                        {
                            frt.anchorMin = new Vector2(0.03f, 0.02f);
                            frt.anchorMax = new Vector2(0.97f, 0.98f);
                        }
                    }
                }
            }
        }
    }
}

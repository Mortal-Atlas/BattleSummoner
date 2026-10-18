using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BattleSummoner.UI
{
    /// <summary>
    /// MinimapRadarController provides a real-time, live top-down tactical radar in the top-right of the screen.
    /// - Follows Player_Hero position seamlessly in X and Z.
    /// - Orthographic top-down camera rendering into a dedicated RenderTexture.
    /// - Circular mask with ornate golden bezel and center directional player arrow.https://assetstore.unity.com/packages/vfx/particles/environment/rain-maker-2d-and-3d-rain-particle-system-for-unity-34938
    /// - Tapping the radar opens the Monster Hunter-style World Exploration Map modal!
    /// </summary>
    public class MinimapRadarController : MonoBehaviour, IPointerClickHandler
    {
        public static MinimapRadarController Instance { get; private set; }

        [Header("Tracking Target")]
        [SerializeField] private Transform playerTransform;

        [Header("Radar Camera Settings")]
        [SerializeField] private float cameraHeight = 35f;
        [SerializeField] private float orthographicSize = 22f;
        [SerializeField] private LayerMask cullingMask = ~0; // Render all layers except UI

        [Header("Render Texture")]
        private RenderTexture minimapRenderTexture;
        private Camera minimapCamera;

        [Header("UI References")]
        [SerializeField] private RectTransform minimapFrameRect;
        [SerializeField] private RawImage minimapDisplay;
        [SerializeField] private RectTransform playerBlipRect;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            FindPlayer();
            SetupRenderTexture();
            SetupMinimapCamera();
            BindToMinimapFrame();
        }

        private void Start()
        {
            FindPlayer();
            BindToMinimapFrame();
        }

        public void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.Find("Player_Hero");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void SetupRenderTexture()
        {
            if (minimapRenderTexture == null)
            {
                minimapRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
                {
                    name = "RT_Minimap_Live",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                minimapRenderTexture.Create();
            }
        }

        private void SetupMinimapCamera()
        {
            if (minimapCamera == null)
            {
                var existingCam = GameObject.Find("MinimapCamera");
                if (existingCam != null)
                {
                    minimapCamera = existingCam.GetComponent<Camera>();
                }
                else
                {
                    GameObject camGo = new GameObject("MinimapCamera");
                    minimapCamera = camGo.AddComponent<Camera>();
                }
            }

            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = orthographicSize;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.04f, 0.04f, 0.07f, 1f);
            minimapCamera.cullingMask = cullingMask & ~(1 << LayerMask.NameToLayer("UI"));
            minimapCamera.targetTexture = minimapRenderTexture;
            minimapCamera.depth = -10;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            UpdateCameraPosition();
        }

        public void BindToMinimapFrame()
        {
            if (minimapFrameRect == null)
            {
                var frameObj = GameObject.Find("MinimapFrame");
                if (frameObj != null) minimapFrameRect = frameObj.GetComponent<RectTransform>();
            }

            if (minimapFrameRect == null) return;

            // 1. Ensure a circular mask container exists inside MinimapFrame
            var maskTrans = minimapFrameRect.Find("MinimapMask");
            RectTransform maskRect;
            if (maskTrans == null)
            {
                GameObject maskGo = new GameObject("MinimapMask", typeof(RectTransform), typeof(Image), typeof(Mask));
                maskGo.transform.SetParent(minimapFrameRect, false);
                maskRect = maskGo.GetComponent<RectTransform>();
                maskRect.anchorMin = new Vector2(0.5f, 0.5f);
                maskRect.anchorMax = new Vector2(0.5f, 0.5f);
                maskRect.pivot = new Vector2(0.5f, 0.5f);
                maskRect.anchoredPosition = Vector2.zero;
                maskRect.sizeDelta = new Vector2(144f, 144f);

                var maskImg = maskGo.GetComponent<Image>();
                maskImg.sprite = GetCircleSprite();
                maskImg.color = Color.white;

                var maskComp = maskGo.GetComponent<Mask>();
                maskComp.showMaskGraphic = false;
                maskGo.SetActive(Application.isPlaying);
            }
            else
            {
                maskRect = maskTrans.GetComponent<RectTransform>();
                maskRect.sizeDelta = new Vector2(144f, 144f);
                maskTrans.gameObject.SetActive(Application.isPlaying);
            }

            // 2. Ensure RawImage inside Mask
            var rawTrans = maskRect.Find("LiveRadarFeed");
            if (rawTrans == null)
            {
                GameObject rawGo = new GameObject("LiveRadarFeed", typeof(RectTransform), typeof(RawImage));
                rawGo.transform.SetParent(maskRect, false);
                var rawRt = rawGo.GetComponent<RectTransform>();
                rawRt.anchorMin = Vector2.zero;
                rawRt.anchorMax = Vector2.one;
                rawRt.offsetMin = Vector2.zero;
                rawRt.offsetMax = Vector2.zero;

                minimapDisplay = rawGo.GetComponent<RawImage>();
            }
            else
            {
                minimapDisplay = rawTrans.GetComponent<RawImage>();
            }

            if (minimapDisplay != null)
            {
                minimapDisplay.texture = minimapRenderTexture;
            }

            // 3. Clean up any legacy CompassBezelOverlay so Mockup_MinimapRadar.png is never obscured
            var bezelTrans = minimapFrameRect.Find("CompassBezelOverlay");
            if (bezelTrans != null)
            {
                DestroyImmediate(bezelTrans.gameObject);
            }

            // 4. Ensure Player Directional Blip in center
            var blipTrans = minimapFrameRect.Find("PlayerRadarBlip");
            if (blipTrans == null)
            {
                GameObject blipGo = new GameObject("PlayerRadarBlip", typeof(RectTransform), typeof(Image));
                blipGo.transform.SetParent(minimapFrameRect, false);
                playerBlipRect = blipGo.GetComponent<RectTransform>();
                playerBlipRect.anchorMin = new Vector2(0.5f, 0.5f);
                playerBlipRect.anchorMax = new Vector2(0.5f, 0.5f);
                playerBlipRect.pivot = new Vector2(0.5f, 0.5f);
                playerBlipRect.anchoredPosition = Vector2.zero;
                playerBlipRect.sizeDelta = new Vector2(28f, 28f);

                var blipImg = blipGo.GetComponent<Image>();
                blipImg.sprite = GetArrowSprite();
                blipImg.color = new Color(1f, 0.85f, 0.25f, 1f); // Glowing golden chevron
                blipImg.raycastTarget = false;
                blipGo.SetActive(Application.isPlaying);
            }
            else
            {
                playerBlipRect = blipTrans.GetComponent<RectTransform>();
                blipTrans.gameObject.SetActive(Application.isPlaying);
            }

            // Ensure the frame border image stays configured
            var frameImg = minimapFrameRect.GetComponent<Image>();
            if (frameImg != null)
            {
#if UNITY_EDITOR
                if (frameImg.sprite == null || frameImg.sprite.name.Contains("Compass"))
                {
                    frameImg.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Mockup_MinimapRadar.png");
                }
#endif
                frameImg.color = Color.white;
                frameImg.raycastTarget = true; // Enables tapping the mini map!
                frameImg.preserveAspect = false;

                // Also attach Button for immediate tactile feedback
                var btn = minimapFrameRect.GetComponent<Button>();
                if (btn == null) btn = minimapFrameRect.gameObject.AddComponent<Button>();
                btn.onClick.RemoveListener(OpenWorldMap);
                btn.onClick.AddListener(OpenWorldMap);
            }

            // Layering order: Mask behind, then Bezel overlay, then Player Blip on top
            if (bezelTrans != null) bezelTrans.SetAsLastSibling();
            if (playerBlipRect != null) playerBlipRect.SetAsLastSibling();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OpenWorldMap();
        }

        public void OpenWorldMap()
        {
            if (WorldMapExplorationModal.Instance != null)
            {
                WorldMapExplorationModal.Instance.OpenModal();
            }
            else
            {
                var map = FindAnyObjectByType<WorldMapExplorationModal>();
                if (map == null)
                {
                    var go = new GameObject("WorldMapExplorationModal", typeof(WorldMapExplorationModal));
                    map = go.GetComponent<WorldMapExplorationModal>();
                }
                map.OpenModal();
            }
        }

        private static Sprite cachedCircleSprite;
        private static Sprite GetCircleSprite()
        {
            if (cachedCircleSprite != null) return cachedCircleSprite;
            int r = 64;
            int size = r * 2;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(r, r);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(r - dist);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedCircleSprite;
        }

        private static Sprite cachedArrowSprite;
        private static Sprite GetArrowSprite()
        {
            if (cachedArrowSprite != null) return cachedArrowSprite;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

            // Draw a crisp upward triangle / chevron
            for (int y = 10; y < 54; y++)
            {
                float halfWidth = (y - 10) * 0.55f;
                int minX = Mathf.RoundToInt(32 - halfWidth);
                int maxX = Mathf.RoundToInt(32 + halfWidth);
                for (int x = minX; x <= maxX; x++)
                {
                    // Notch at bottom
                    if (y < 22 && Mathf.Abs(x - 32) < 8) continue;
                    colors[y * size + x] = Color.white;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            cachedArrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedArrowSprite;
        }

        private void LateUpdate()
        {
            if (playerTransform == null) FindPlayer();
            UpdateCameraPosition();

            if (playerTransform != null && playerBlipRect != null)
            {
                // Player arrow rotates with player's Y heading
                float yaw = playerTransform.eulerAngles.y;
                playerBlipRect.localEulerAngles = new Vector3(0f, 0f, -yaw);
            }
        }

        private void UpdateCameraPosition()
        {
            if (minimapCamera != null && playerTransform != null)
            {
                minimapCamera.transform.position = new Vector3(
                    playerTransform.position.x,
                    playerTransform.position.y + cameraHeight,
                    playerTransform.position.z
                );
            }
        }

        private void OnEnable()
        {
            if (CampfireRumorPropagationService.Instance != null)
            {
                CampfireRumorPropagationService.Instance.OnDynamicMapMarkerInjected += HandleDynamicMarkerInjected;
            }
        }

        private void OnDisable()
        {
            if (CampfireRumorPropagationService.Instance != null)
            {
                CampfireRumorPropagationService.Instance.OnDynamicMapMarkerInjected -= HandleDynamicMarkerInjected;
            }
        }

        private void HandleDynamicMarkerInjected(WorldRumorPayload rumor)
        {
            AddDynamicWaypoint(rumor.worldCoordinates, rumor.discoveryType);
        }

        public void AddDynamicWaypoint(Vector3 worldPos, string label)
        {
            if (minimapFrameRect == null) return;

            GameObject waypointObj = new GameObject($"Waypoint_{label}");
            waypointObj.transform.SetParent(minimapFrameRect, false);

            var rect = waypointObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(16f, 16f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var img = waypointObj.AddComponent<Image>();
            img.color = new Color(1.0f, 0.85f, 0.2f, 0.95f);

            var marker = waypointObj.AddComponent<MinimapWaypointMarker>();
            marker.targetWorldPosition = worldPos;
            marker.orthographicSize = orthographicSize;
            marker.player = playerTransform;

            Debug.Log($"📍 [MINIMAP WAYPOINT] Injected dynamic waypoint '{label}' at {worldPos} from campfire rumor.");
        }

        private void OnDestroy()
        {
            if (minimapRenderTexture != null)
            {
                minimapRenderTexture.Release();
                Destroy(minimapRenderTexture);
            }
        }
    }

    public class MinimapWaypointMarker : MonoBehaviour
    {
        public Vector3 targetWorldPosition;
        public float orthographicSize = 22f;
        public Transform player;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                var p = GameObject.Find("Player_Hero");
                if (p != null) player = p.transform;
                if (player == null) return;
            }

            Vector3 delta = targetWorldPosition - player.position;
            float scale = 65f / orthographicSize;
            Vector2 screenOffset = new Vector2(delta.x * scale, delta.z * scale);

            if (screenOffset.magnitude > 65f)
            {
                screenOffset = screenOffset.normalized * 65f;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = screenOffset;
            }
        }
    }
}

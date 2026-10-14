using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RelicInspectorController drives the 3D puzzle inspection stage.
/// - 1-finger orbit (pitch & yaw with smooth inertia damping).
/// - 2-finger pinch zoom (and mouse scroll wheel) with smooth clamping.
/// - Tapping and sliding interactions on 3D puzzle pieces.
/// - Dark vignette dimmer screen that dims and focuses exclusively on the inspected relic.
/// - Muffles world audio, TTS, and music while relic ASMR plays crystal-clear.
/// </summary>
public class RelicInspectorController : MonoBehaviour
{
    [Header("Stage Rig")]
    public Transform relicPivot;
    public Camera inspectionCamera;
    public GameObject inspectionUIPanel;
    public CanvasGroup dimmerCanvasGroup;
    public Image dimmerVignetteImage;

    [Header("Dramatic Lighting")]
    public Light keySpotlight;
    public Light rimSpotlight;
    public Light underglowLight;

    [Header("Sensitivity & Inertia")]
    [SerializeField] private float orbitSpeed = 0.28f;
    [SerializeField] private float orbitDamping = 8f;
    [SerializeField] private float zoomSpeed = 0.03f;
    [SerializeField] private float minFov = 22f;
    [SerializeField] private float maxFov = 58f;
    [SerializeField] private float defaultFov = 38f;

    [Header("Interaction Thresholds")]
    [SerializeField] private float tapMaxDuration = 0.28f;
    [SerializeField] private float tapMaxDistance = 24f;

    private Vector2 targetOrbitVelocity;
    private Vector2 currentOrbitVelocity;
    private float targetFov;
    private bool isPointerDown = false;
    private Vector2 pointerDownPos;
    private float pointerDownTime;
    private Vector2 currentPointerPos;
    private bool isDraggingPiece = false;
    private IInspectablePuzzlePiece activePieceUnderPointer = null;

    private Coroutine dimmerFadeCoroutine;

    private void Awake()
    {
        targetFov = defaultFov;
        if (inspectionCamera != null)
        {
            inspectionCamera.fieldOfView = defaultFov;
            Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (allCams.Length > 1)
            {
                inspectionCamera.enabled = false;
            }
            else
            {
                inspectionCamera.enabled = true;
            }
        }

        // Ensure RelicASMRSynthesizer is present on this rig
        if (GetComponent<RelicASMRSynthesizer>() == null)
        {
            gameObject.AddComponent<RelicASMRSynthesizer>();
        }
    }

    private void OnEnable()
    {
        PortraitGestureCanvas.OnRelicPointerDown += HandlePointerDown;
        PortraitGestureCanvas.OnRelicDrag += HandlePointerDrag;
        PortraitGestureCanvas.OnRelicPointerUp += HandlePointerUp;
        PortraitGestureCanvas.OnRelicOrbit += HandleOrbitFromCanvas;
        PortraitGestureCanvas.OnRelicZoom += HandleZoomFromCanvas;
        PortraitGestureCanvas.OnRelicTap += HandleTapFromCanvas;
    }

    private void OnDisable()
    {
        PortraitGestureCanvas.OnRelicPointerDown -= HandlePointerDown;
        PortraitGestureCanvas.OnRelicDrag -= HandlePointerDrag;
        PortraitGestureCanvas.OnRelicPointerUp -= HandlePointerUp;
        PortraitGestureCanvas.OnRelicOrbit -= HandleOrbitFromCanvas;
        PortraitGestureCanvas.OnRelicZoom -= HandleZoomFromCanvas;
        PortraitGestureCanvas.OnRelicTap -= HandleTapFromCanvas;
    }

    private void Update()
    {
        if (inspectionCamera == null || !inspectionCamera.enabled) return;

        // Smooth Camera Zoom (FOV)
        inspectionCamera.fieldOfView = Mathf.Lerp(inspectionCamera.fieldOfView, targetFov, Time.deltaTime * 10f);

        // Editor / Mouse Scroll Wheel Zoom
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetFov = Mathf.Clamp(targetFov - scroll * 4.5f, minFov, maxFov);
        }

        // Apply smooth orbit rotation with inertia
        if (relicPivot != null)
        {
            currentOrbitVelocity = Vector2.Lerp(currentOrbitVelocity, targetOrbitVelocity, Time.deltaTime * orbitDamping);
            if (currentOrbitVelocity.sqrMagnitude > 0.001f)
            {
                relicPivot.Rotate(Vector3.up, -currentOrbitVelocity.x * Time.deltaTime * 60f, Space.World);
                relicPivot.Rotate(Vector3.right, currentOrbitVelocity.y * Time.deltaTime * 60f, Space.World);
            }

            // Decay velocity when not actively dragging
            if (!isPointerDown)
            {
                targetOrbitVelocity = Vector2.Lerp(targetOrbitVelocity, Vector2.zero, Time.deltaTime * 4f);
            }
        }
    }

    #region Inspection Lifecycle
    public void OpenInspection(GameObject relicPrefab)
    {
        if (inspectionCamera != null) inspectionCamera.enabled = true;
        if (inspectionUIPanel != null) inspectionUIPanel.SetActive(true);

        targetFov = defaultFov;
        if (inspectionCamera != null) inspectionCamera.fieldOfView = defaultFov;

        // Populate Relic in Pivot
        if (relicPivot != null)
        {
            for (int i = relicPivot.childCount - 1; i >= 0; i--)
            {
                Destroy(relicPivot.GetChild(i).gameObject);
            }

            if (relicPrefab != null)
            {
                Instantiate(relicPrefab, relicPivot);
            }
            relicPivot.localRotation = Quaternion.Euler(22f, -48f, 15f);
        }

        // Dramatic Lighting
        SetDramaticLights(true);

        // Fade in black vignette dimmer screen
        SetDimmerScreen(true);

        // Muffle external world audio (TTS, background music, SFX)
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.SetInspectionMuffledAudio(true, 0.4f);
        }

        Debug.Log("[RELIC INSPECTOR] Inspection mode activated. World dimmed & muffled. Intimate ASMR active.");
    }

    public void CloseInspection()
    {
        if (inspectionCamera != null) inspectionCamera.enabled = false;
        if (inspectionUIPanel != null) inspectionUIPanel.SetActive(false);

        SetDramaticLights(false);
        SetDimmerScreen(false);

        // Restore normal world audio
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.SetInspectionMuffledAudio(false, 0.35f);
        }

        Debug.Log("[RELIC INSPECTOR] Inspection closed. World restored.");
    }

    private void SetDramaticLights(bool active)
    {
        if (keySpotlight != null) keySpotlight.enabled = active;
        if (rimSpotlight != null) rimSpotlight.enabled = active;
        if (underglowLight != null) underglowLight.enabled = active;
    }

    private void SetDimmerScreen(bool show)
    {
        if (dimmerCanvasGroup != null)
        {
            if (dimmerFadeCoroutine != null) StopCoroutine(dimmerFadeCoroutine);
            dimmerFadeCoroutine = StartCoroutine(FadeDimmerRoutine(show ? 1f : 0f, 0.35f));
        }
    }

    private IEnumerator FadeDimmerRoutine(float targetAlpha, float duration)
    {
        float startAlpha = dimmerCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            dimmerCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        dimmerCanvasGroup.alpha = targetAlpha;
    }
    #endregion

    #region Input & Gesture Event Callbacks
    public void HandlePointerDown(Vector2 screenPos)
    {
        isPointerDown = true;
        pointerDownPos = screenPos;
        currentPointerPos = screenPos;
        pointerDownTime = Time.time;
        isDraggingPiece = false;
        activePieceUnderPointer = null;

        // Raycast to check if user clicked directly on an interactive puzzle piece
        if (inspectionCamera != null)
        {
            Ray ray = inspectionCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                if (hit.collider.TryGetComponent<IInspectablePuzzlePiece>(out var piece))
                {
                    activePieceUnderPointer = piece;
                }
            }
        }
    }

    public void HandlePointerDrag(Vector2 currentPos, Vector2 delta)
    {
        if (!isPointerDown) return;
        currentPointerPos = currentPos;

        float distFromStart = Vector2.Distance(currentPos, pointerDownPos);

        if (activePieceUnderPointer != null)
        {
            if (distFromStart > tapMaxDistance || isDraggingPiece)
            {
                isDraggingPiece = true;
                activePieceUnderPointer.OnPuzzleSlide(delta);
            }
        }
        else
        {
            // Empty space drag: Orbit the entire 3D object
            targetOrbitVelocity = delta * orbitSpeed;
        }
    }

    public void HandlePointerUp(Vector2 screenPos)
    {
        if (!isPointerDown) return;
        isPointerDown = false;

        float duration = Time.time - pointerDownTime;
        float dist = Vector2.Distance(screenPos, pointerDownPos);

        if (activePieceUnderPointer != null)
        {
            if (isDraggingPiece)
            {
                activePieceUnderPointer.OnPuzzleSlideEnd();
            }
            else if (duration <= tapMaxDuration && dist <= tapMaxDistance)
            {
                // Crisp Tap on puzzle piece
                activePieceUnderPointer.OnPuzzleTapped();
            }
            activePieceUnderPointer = null;
            isDraggingPiece = false;
        }
        else
        {
            // Tap on general relic body / background
            if (duration <= tapMaxDuration && dist <= tapMaxDistance)
            {
                if (inspectionCamera != null)
                {
                    Ray ray = inspectionCamera.ScreenPointToRay(screenPos);
                    if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                    {
                        if (hit.collider.TryGetComponent<IInspectablePuzzlePiece>(out var piece))
                        {
                            piece.OnPuzzleTapped();
                        }
                        else
                        {
                            RelicASMRSynthesizer.Instance?.PlayHollowKnock(0.7f, 1.0f);
                        }
                    }
                }
            }
        }
    }

    private void HandleOrbitFromCanvas(Vector2 delta)
    {
        // 1-finger orbit delta from PortraitGestureCanvas
        if (activePieceUnderPointer != null)
        {
            activePieceUnderPointer.OnPuzzleSlide(delta);
        }
        else
        {
            targetOrbitVelocity = delta * orbitSpeed;
        }
    }

    private void HandleZoomFromCanvas(float pinchDelta)
    {
        // 2-finger pinch zoom
        targetFov = Mathf.Clamp(targetFov - pinchDelta * zoomSpeed, minFov, maxFov);
    }

    private void HandleTapFromCanvas(Vector2 screenPosition)
    {
        if (inspectionCamera == null) return;

        Ray ray = inspectionCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
        {
            if (hit.collider.TryGetComponent<IInspectablePuzzlePiece>(out var puzzle))
            {
                puzzle.OnPuzzleTapped();
            }
            else
            {
                RelicASMRSynthesizer.Instance?.PlayHollowKnock(0.75f, 1.0f);
            }
        }
    }
    #endregion
}

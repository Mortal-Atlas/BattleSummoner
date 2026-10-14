using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// FloatingVirtualStick provides thumb-driven movement controls for portrait mobile gameplay.
/// Supports both uGUI PointerEventData and direct native touch polling for guaranteed mobile responsiveness.
/// </summary>
public class FloatingVirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Stick Visuals")]
    [SerializeField] private RectTransform stickBackground;
    [SerializeField] private RectTransform stickHandle;
    [SerializeField] private float handleRange = 80f;

    [Header("Input Vector Output")]
    [SerializeField] private Vector2 moveInput = Vector2.zero;

    [Header("Sprint Parameters")]
    [SerializeField] private float sprintThreshold = 0.88f;
    [SerializeField] private Color normalHandleColor = Color.white;
    [SerializeField] private Color sprintHandleColor = new Color(0.25f, 0.92f, 1.0f, 1.0f);

    [Header("Flick Dash Parameters")]
    [SerializeField] private float maxFlickDuration = 0.28f;
    [SerializeField] private float minFlickDistance = 35f;
    [SerializeField] private float minFlickVelocity = 450f;
    [SerializeField] private float flickCooldown = 0.35f;

    public Vector2 MoveInput => moveInput;
    public bool IsAtFarEdge => moveInput.magnitude >= sprintThreshold;

    public static event System.Action<Vector2> OnJoystickFlick;
    public event System.Action<Vector2> OnFlick;

    public void BindStickVisuals(RectTransform bg, RectTransform handle, float range = 80f)
    {
        stickBackground = bg;
        stickHandle = handle;
        handleRange = range;
        if (stickHandle != null) handleImage = stickHandle.GetComponent<Image>();
        if (stickBackground != null) defaultBackgroundPosition = stickBackground.anchoredPosition;
    }

    private Vector2 defaultBackgroundPosition;
    private bool isPressed = false;
    private int directTouchFingerId = -1;
    private Vector2 directTouchStartPos;
    private float touchDownTime = 0f;
    private Vector2 lastHandleOffset = Vector2.zero;
    private Vector2 currentHandleVelocity = Vector2.zero;
    private float lastFlickTime = -10f;
    private Image handleImage;

    private void Awake()
    {
        if (stickBackground != null)
        {
            defaultBackgroundPosition = stickBackground.anchoredPosition;
        }
        if (stickHandle != null)
        {
            handleImage = stickHandle.GetComponent<Image>();
        }

        #if ENABLE_INPUT_SYSTEM
        try
        {
            if (!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
            {
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            }
        }
        catch { }
        #endif
    }

    private void Update()
    {
        // Direct mobile touch fallback if uGUI raycaster dropped touch
        HandleDirectMobileTouches();

        // Update handle visual feedback based on sprint (far edge) state
        UpdateHandleVisuals();
    }

    private void UpdateHandleVisuals()
    {
        if (handleImage == null && stickHandle != null) handleImage = stickHandle.GetComponent<Image>();
        if (handleImage != null)
        {
            Color targetColor = IsAtFarEdge ? sprintHandleColor : normalHandleColor;
            handleImage.color = Color.Lerp(handleImage.color, targetColor, Time.deltaTime * 14f);
        }

        if (stickHandle != null)
        {
            float targetScale = IsAtFarEdge ? 1.14f : 1.0f;
            Vector3 s = stickHandle.localScale;
            s.x = Mathf.Lerp(s.x, targetScale, Time.deltaTime * 14f);
            s.y = s.x;
            stickHandle.localScale = s;
        }
    }

    private void TriggerFlick(Vector2 direction)
    {
        if (Time.time - lastFlickTime < flickCooldown) return;
        lastFlickTime = Time.time;

        if (direction.sqrMagnitude < 0.001f) direction = moveInput.sqrMagnitude > 0.001f ? moveInput.normalized : Vector2.up;
        else direction.Normalize();

        OnJoystickFlick?.Invoke(direction);
        OnFlick?.Invoke(direction);
    }

    private void HandleDirectMobileTouches()
    {
        if (isPressed) return; // Managed by uGUI pointer events

        if (Input.touchSupported && Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                Vector2 pos = t.position;

                // Virtual stick zone: bottom-left 45% width, bottom 32% height
                bool inStickZone = (pos.x < Screen.width * 0.45f && pos.y < Screen.height * 0.32f);

                if (t.phase == TouchPhase.Began && directTouchFingerId == -1 && inStickZone)
                {
                    directTouchFingerId = t.fingerId;
                    directTouchStartPos = pos;
                    touchDownTime = Time.time;
                    lastHandleOffset = Vector2.zero;
                    currentHandleVelocity = Vector2.zero;

                    if (stickBackground != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        transform as RectTransform, pos, null, out Vector2 localPoint))
                    {
                        stickBackground.anchoredPosition = localPoint;
                    }
                }
                else if (t.fingerId == directTouchFingerId)
                {
                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        Vector2 delta = pos - directTouchStartPos;
                        float dist = delta.magnitude;
                        Vector2 dir = dist > 0.001f ? delta.normalized : Vector2.zero;
                        float clampedDist = Mathf.Min(dist, handleRange);

                        Vector2 handlePos = dir * clampedDist;
                        if (Time.deltaTime > 0f)
                        {
                            currentHandleVelocity = (handlePos - lastHandleOffset) / Time.deltaTime;
                        }
                        lastHandleOffset = handlePos;

                        if (stickHandle != null)
                        {
                            stickHandle.anchoredPosition = handlePos;
                        }

                        moveInput = dir * (clampedDist / handleRange);

                        // In-drag rapid flick outward check
                        if (currentHandleVelocity.magnitude >= 600f && clampedDist >= handleRange * 0.82f)
                        {
                            TriggerFlick(dir);
                        }
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        float duration = Time.time - touchDownTime;
                        float totalDist = (pos - directTouchStartPos).magnitude;
                        if ((duration <= maxFlickDuration && totalDist >= minFlickDistance) || currentHandleVelocity.magnitude >= minFlickVelocity)
                        {
                            Vector2 flickDir = totalDist > 0.001f ? (pos - directTouchStartPos).normalized : (lastHandleOffset.sqrMagnitude > 0.001f ? lastHandleOffset.normalized : Vector2.up);
                            TriggerFlick(flickDir);
                        }

                        ResetStick();
                        directTouchFingerId = -1;
                    }
                }
            }
        }
        else if (directTouchFingerId != -1)
        {
            ResetStick();
            directTouchFingerId = -1;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        directTouchFingerId = -1;
        touchDownTime = Time.time;
        directTouchStartPos = eventData.position;
        lastHandleOffset = Vector2.zero;
        currentHandleVelocity = Vector2.zero;

        if (stickBackground != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            stickBackground.anchoredPosition = localPoint;
        }
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressed || stickBackground == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            stickBackground, eventData.position, eventData.pressEventCamera, out Vector2 localHandlePoint))
        {
            float distance = localHandlePoint.magnitude;
            Vector2 direction = distance > 0.001f ? localHandlePoint.normalized : Vector2.zero;
            float clampedDist = Mathf.Min(distance, handleRange);

            Vector2 handlePos = direction * clampedDist;
            if (Time.deltaTime > 0f)
            {
                currentHandleVelocity = (handlePos - lastHandleOffset) / Time.deltaTime;
            }
            lastHandleOffset = handlePos;

            if (stickHandle != null)
            {
                stickHandle.anchoredPosition = handlePos;
            }

            moveInput = direction * (clampedDist / handleRange);

            // In-drag rapid flick outward check
            if (currentHandleVelocity.magnitude >= 600f && clampedDist >= handleRange * 0.82f)
            {
                TriggerFlick(direction);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        float duration = Time.time - touchDownTime;
        float totalDist = (eventData.position - directTouchStartPos).magnitude;
        if ((duration <= maxFlickDuration && totalDist >= minFlickDistance) || currentHandleVelocity.magnitude >= minFlickVelocity)
        {
            Vector2 flickDir = totalDist > 0.001f ? (eventData.position - directTouchStartPos).normalized : (lastHandleOffset.sqrMagnitude > 0.001f ? lastHandleOffset.normalized : Vector2.up);
            TriggerFlick(flickDir);
        }

        ResetStick();
    }

    private void ResetStick()
    {
        moveInput = Vector2.zero;
        lastHandleOffset = Vector2.zero;
        currentHandleVelocity = Vector2.zero;

        if (stickHandle != null)
        {
            stickHandle.anchoredPosition = Vector2.zero;
        }

        if (stickBackground != null)
        {
            stickBackground.anchoredPosition = defaultBackgroundPosition;
        }
    }
}

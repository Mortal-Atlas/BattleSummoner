using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// TouchCameraLookZone provides an empty touch drag zone on the bottom right of the screen
/// (opposite the floating virtual joystick on the left) for rotating the camera yaw and pitch.
/// Supports both uGUI Pointer events and direct mobile touch polling.
/// </summary>
[RequireComponent(typeof(Image))]
public class TouchCameraLookZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Sensitivity Settings")]
    [SerializeField] private float sensitivityMultiplier = 1.0f;
    [SerializeField] private bool invertPitch = false;

    private int activePointerId = -999;
    private Image zoneImage;
    private int directTouchFingerId = -1;
    private Vector2 lastDirectTouchPos;

    private void Awake()
    {
        zoneImage = GetComponent<Image>();
        if (zoneImage != null)
        {
            // Transparent raycast target so it catches touches without blocking visuals
            Color c = zoneImage.color;
            c.a = 0.002f; // tiny alpha so Unity UI raycast detects it reliably
            zoneImage.color = c;
            zoneImage.raycastTarget = true;
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
        HandleDirectMobileTouches();
    }

    private void HandleDirectMobileTouches()
    {
        if (activePointerId != -999) return; // Managed by uGUI

        if (Input.touchSupported && Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                Vector2 pos = t.position;

                // Look zone: right 50% width, bottom 32% height
                bool inLookZone = (pos.x >= Screen.width * 0.50f && pos.y < Screen.height * 0.32f);

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                {
                    continue;
                }

                if (t.phase == TouchPhase.Began && directTouchFingerId == -1 && inLookZone)
                {
                    directTouchFingerId = t.fingerId;
                    lastDirectTouchPos = pos;
                }
                else if (t.fingerId == directTouchFingerId)
                {
                    if (t.phase == TouchPhase.Moved)
                    {
                        Vector2 delta = pos - lastDirectTouchPos;
                        lastDirectTouchPos = pos;

                        if (PortraitARPGCamera.Instance != null)
                        {
                            float deltaX = delta.x * sensitivityMultiplier;
                            float deltaY = delta.y * sensitivityMultiplier * (invertPitch ? -1f : 1f);
                            PortraitARPGCamera.Instance.RotateCamera(deltaX, deltaY);
                        }
                    }
                    else if (t.phase == TouchPhase.Stationary)
                    {
                        lastDirectTouchPos = pos;
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        directTouchFingerId = -1;
                    }
                }
            }
        }
        else if (directTouchFingerId != -1)
        {
            directTouchFingerId = -1;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId == -999)
        {
            activePointerId = eventData.pointerId;
            directTouchFingerId = -1;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        if (PortraitARPGCamera.Instance != null)
        {
            float deltaX = eventData.delta.x * sensitivityMultiplier;
            float deltaY = eventData.delta.y * sensitivityMultiplier * (invertPitch ? -1f : 1f);

            PortraitARPGCamera.Instance.RotateCamera(deltaX, deltaY);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            activePointerId = -999;
        }
    }
}

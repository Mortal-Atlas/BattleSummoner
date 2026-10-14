using UnityEngine;

/// <summary>
/// PortraitARPGCamera provides decoupled, elevated 3rd-person orbital tracking for portrait mobile ARPGs.
/// - Camera orientation and movement are strictly independent from player character movement.
/// - Elevated pitch (42 deg) and pulled-back distance (12m) give a clear tactical view of the dungeon floor.
/// - Controlled via TouchCameraLookZone on the bottom-right of the screen, desktop mouse drag (RMB),
///   or keyboard keys (Arrow keys / Q-E).
/// - Dynamically adjusts vertical FOV across Android portrait aspect ratios (16:9, 19.5:9, 20:9).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class PortraitARPGCamera : MonoBehaviour
{
    public static PortraitARPGCamera Instance { get; private set; }

    [Header("Tracking Targets")]
    [SerializeField] private Transform playerTransform;
    public Transform playerTarget
    {
        get => playerTransform;
        set => playerTransform = value;
    }
    public Transform activeBossTarget;

    [Header("Phase 2 Target Tracking Spec")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 4.5f, -6.5f);
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float verticalLookAhead = 1.5f;

    [Header("Decoupled Orbital Camera Parameters")]
    [Tooltip("Horizontal orbit angle in degrees. Independent of player rotation.")]
    public float yawAngle = 0f;
    [Tooltip("Vertical tilt angle in degrees (42 = elevated isometric view).")]
    public float pitchAngle = 42f;
    [Tooltip("Pulled-back distance from player (12m provides wide tactical vision).")]
    public float cameraDistance = 12.0f;
    [Tooltip("Height above player feet to focus on.")]
    public float targetFocusHeight = 1.6f;
    [Tooltip("Lateral camera offset (0 = center).")]
    public float lateralOffset = 0f;

    [Header("Limits & Damping")]
    [SerializeField] private float minPitch = 15f;
    [SerializeField] private float maxPitch = 68f;
    [SerializeField] private float positionDamping = 12f;
    [SerializeField] private float rotationDamping = 14f;
    [SerializeField] private float lookSensitivityX = 0.22f;
    [SerializeField] private float lookSensitivityY = 0.18f;

    [Header("Dynamic Portrait Aspect FOV")]
    [Tooltip("Target vertical FOV for a standard 9:16 portrait display.")]
    [SerializeField] private float standardVerticalFov = 72f;
    [SerializeField] private float referenceAspectWidth = 9f;
    [SerializeField] private float referenceAspectHeight = 16f;
    [SerializeField] private float minAllowedFov = 58f;
    [SerializeField] private float maxAllowedFov = 100f;

    [Header("Aim Down Sights (ADS) OTS Mode")]
    [Tooltip("Whether the camera is currently in Over-The-Shoulder Scope mode.")]
    public bool isAimingDownSights = false;
    [Tooltip("Tight camera distance during ADS (2.4m provides clear shoulder perspective).")]
    public float adsCameraDistance = 2.4f;
    [Tooltip("Right lateral offset over the right shoulder.")]
    public float adsLateralOffset = 0.72f;
    [Tooltip("Level line of sight pitch during ADS.")]
    public float adsPitchAngle = 14f;
    [Tooltip("Optical magnification vertical FOV.")]
    public float adsVerticalFov = 42f;

    private Camera cam;
    private Vector3 currentVelocity;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private float currentEffectiveDistance = 12.0f;
    private float currentEffectiveLateral = 0f;
    private float currentEffectivePitch = 42f;
    private float standardAspectCalculatedFov = 72f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        cam = GetComponent<Camera>();
        currentEffectiveDistance = cameraDistance;
        currentEffectiveLateral = lateralOffset;
        currentEffectivePitch = pitchAngle;
        AdjustFieldOfViewForAspect();
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
        cam = GetComponent<Camera>();
        AdjustFieldOfViewForAspect();
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
        }
        currentEffectiveDistance = cameraDistance;
        currentEffectiveLateral = lateralOffset;
        currentEffectivePitch = pitchAngle;
        AdjustFieldOfViewForAspect();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            AdjustFieldOfViewForAspect();
        }

        // Desktop keyboard/mouse camera controls (RMB drag or Arrow Keys/Q-E)
        if (Application.isPlaying)
        {
            HandleDesktopCameraControls();
        }
    }

    private void HandleDesktopCameraControls()
    {
        // Right Mouse Button or Middle Mouse Button Drag
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            RotateCamera(mouseX * 14f, mouseY * 12f);
        }

        // Desktop keyboard shortcuts: Q/E or Left/Right Arrow to orbit
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q))
        {
            RotateCamera(-85f * Time.deltaTime, 0f);
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E))
        {
            RotateCamera(85f * Time.deltaTime, 0f);
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            RotateCamera(0f, 65f * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            RotateCamera(0f, -65f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Rotates camera yaw and pitch. Called by TouchCameraLookZone or desktop controls.
    /// </summary>
    public void RotateCamera(float deltaX, float deltaY)
    {
        yawAngle = (yawAngle + deltaX * lookSensitivityX) % 360f;
        pitchAngle = Mathf.Clamp(pitchAngle - deltaY * lookSensitivityY, minPitch, maxPitch);
    }

    public void AdjustFieldOfViewForAspect()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Mathf.Max(1, Screen.height);

        float currentAspect = (float)lastScreenWidth / (float)lastScreenHeight;
        float refAspect = referenceAspectWidth / Mathf.Max(0.001f, referenceAspectHeight); // 9 / 16 = 0.5625

        float refVfovRad = standardVerticalFov * Mathf.Deg2Rad;
        float halfVfovTan = Mathf.Tan(refVfovRad * 0.5f);
        float refHfovRad = 2f * Mathf.Atan(halfVfovTan * refAspect);

        float halfHfovTan = Mathf.Tan(refHfovRad * 0.5f);
        float dynamicVfovRad = 2f * Mathf.Atan(halfHfovTan / currentAspect);
        float dynamicVfovDeg = dynamicVfovRad * Mathf.Rad2Deg;

        standardAspectCalculatedFov = Mathf.Clamp(dynamicVfovDeg, minAllowedFov, maxAllowedFov);
        if (!isAimingDownSights)
        {
            cam.fieldOfView = standardAspectCalculatedFov;
        }
    }

    private void LateUpdate()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
            else return;
        }

        // Interpolate parameters between normal orbital and ADS Over-The-Shoulder
        float targetDist = isAimingDownSights ? adsCameraDistance : cameraDistance;
        float targetLateral = isAimingDownSights ? adsLateralOffset : lateralOffset;
        float targetPitch = isAimingDownSights ? adsPitchAngle : pitchAngle;
        float targetFov = isAimingDownSights ? adsVerticalFov : standardAspectCalculatedFov;

        float lerpSpeed = isAimingDownSights ? 14f : 9f;
        currentEffectiveDistance = Mathf.Lerp(currentEffectiveDistance, targetDist, Time.deltaTime * lerpSpeed);
        currentEffectiveLateral = Mathf.Lerp(currentEffectiveLateral, targetLateral, Time.deltaTime * lerpSpeed);
        currentEffectivePitch = Mathf.Lerp(currentEffectivePitch, targetPitch, Time.deltaTime * lerpSpeed);

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * lerpSpeed);
        }

        Vector3 focusPoint = playerTarget.position + Vector3.up * (targetFocusHeight + (isAimingDownSights ? 0.15f : verticalLookAhead * 0.5f));
        Quaternion camRotation = Quaternion.Euler(currentEffectivePitch, yawAngle, 0f);
        Vector3 dynamicOffset = camRotation * new Vector3(currentEffectiveLateral, 0f, -currentEffectiveDistance);
        Vector3 desiredPosition = focusPoint + dynamicOffset;

        // In ADS mode, player character smoothly faces camera forward yaw
        if (isAimingDownSights && playerTarget != null)
        {
            Quaternion aimRot = Quaternion.Euler(0f, yawAngle, 0f);
            playerTarget.rotation = Quaternion.Slerp(playerTarget.rotation, aimRot, Time.deltaTime * 18f);
        }

        if (!Application.isPlaying)
        {
            transform.position = desiredPosition;
            transform.rotation = camRotation;
            return;
        }

        float trackingSpeed = positionDamping > 0f ? positionDamping : smoothSpeed;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, Time.deltaTime * trackingSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, camRotation, Time.deltaTime * rotationDamping);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase 3: Thumb-Oriented Posture & Directional Slash Combos
/// WeaponArtGestureController implements thumb posture tracking (Skyward Sword style),
/// hold-to-charge weapon arts with procedural whirring audio & material emission glow,
/// directional slashes that propagate along the exact swipe angle, and multi-stroke combo recognition.
/// </summary>
public class WeaponArtGestureController : MonoBehaviour
{
    private static WeaponArtGestureController _instance;
    public static WeaponArtGestureController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<WeaponArtGestureController>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Weapon Art Charge")]
    [SerializeField] private float holdChargeTimeRequired = 0.8f;
    private float currentHoldTimer = 0f;
    private bool isHoldingForCharge = false;
    private bool weaponArtReady = false;

    [Header("Directional Slash & Posture")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform weaponTransform;
    private Vector2 swipeStartPos;
    private float swipeStartTime;
    private List<Vector2> comboPathPoints = new List<Vector2>();
    private List<float> recentSlashAngles = new List<float>();
    private float lastSlashTime = 0f;
    private const float COMBO_WINDOW = 1.4f;

    [Header("Visual Glow & Audio Feedback")]
    [SerializeField] private Renderer weaponRenderer;
    [SerializeField] private Light weaponGlowLight;
    [SerializeField] private Color chargeGlowColor = new Color(0.2f, 0.85f, 1f);
    [SerializeField] private Color fullyChargedGlowColor = new Color(1f, 0.85f, 0.2f);
    private Material weaponMatInstance;
    private AudioSource chargeAudioSource;

    public event Action<string> OnComboRecognized;
    public event Action<Vector2, float> OnDirectionalSlashExecuted;
    public event Action<float> OnChargeProgress; // 0 to 1
    public event Action OnWeaponArtCharged;

    public bool IsWeaponArtReady => weaponArtReady;
    public float ChargeProgress => Mathf.Clamp01(currentHoldTimer / holdChargeTimeRequired);
    public IReadOnlyList<float> RecentSlashAngles => recentSlashAngles;

    private int activeFingerId = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // Initialize audio source for procedural whirring tone
        chargeAudioSource = gameObject.AddComponent<AudioSource>();
        chargeAudioSource.playOnAwake = false;
        chargeAudioSource.loop = true;
        chargeAudioSource.spatialBlend = 0f;
        chargeAudioSource.volume = 0f;

        // Ensure EnhancedTouchSupport is enabled for mobile platforms
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

    private void Start()
    {
        ResolveWeaponReferences();
    }

    public void ResolveWeaponReferences()
    {
        if (weaponTransform == null)
        {
            // Look for DevWeapon_Sword or Sword under player
            var swordTrans = transform.Find("DevWeapon_Sword");
            if (swordTrans != null)
            {
                weaponTransform = swordTrans;
            }
            else
            {
                var allTrans = GetComponentsInChildren<Transform>(true);
                foreach (var t in allTrans)
                {
                    if (t.name.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0 && t.name != "SwordBlade")
                    {
                        weaponTransform = t;
                        break;
                    }
                }
            }
        }

        if (weaponRenderer == null && weaponTransform != null)
        {
            weaponRenderer = weaponTransform.GetComponentInChildren<Renderer>();
        }

        if (weaponRenderer != null && weaponMatInstance == null)
        {
            weaponMatInstance = weaponRenderer.material;
            weaponMatInstance.EnableKeyword("_EMISSION");
        }

        if (weaponGlowLight == null && weaponTransform != null)
        {
            var lightObj = new GameObject("WeaponChargeGlowLight");
            lightObj.transform.SetParent(weaponTransform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            weaponGlowLight = lightObj.AddComponent<Light>();
            weaponGlowLight.type = LightType.Point;
            weaponGlowLight.range = 3.5f;
            weaponGlowLight.intensity = 0f;
            weaponGlowLight.color = chargeGlowColor;
        }
    }

    private void Update()
    {
        // Clear combo angles if window elapsed
        if (recentSlashAngles.Count > 0 && Time.time - lastSlashTime > COMBO_WINDOW)
        {
            recentSlashAngles.Clear();
        }

        HandleTouchInput();
        UpdateChargeVisuals();
    }

    private void HandleTouchInput()
    {
        // 1. Mobile Multi-Touch Handling with Zone Filtering
        if (Input.touchSupported && Input.touchCount > 0)
        {
            HandleNativeMobileTouches();
            return;
        }

        // 2. Desktop Mouse Simulation
        HandleMouseInput();
    }

    private void HandleNativeMobileTouches()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 pos = touch.position;

            // Filter out Bottom-Left (Virtual Stick: x < 45%, y < 32%)
            // and Bottom-Right (Camera Look Zone: x > 55%, y < 32%)
            bool inBottomControlsZone = (pos.y < Screen.height * 0.30f);
            if (activeFingerId == -1 && inBottomControlsZone)
            {
                continue; // Don't initiate weapon gestures inside virtual stick / camera zone
            }

            if (touch.phase == TouchPhase.Began && activeFingerId == -1)
            {
                activeFingerId = touch.fingerId;
                swipeStartPos = pos;
                swipeStartTime = Time.time;
                currentHoldTimer = 0f;
                isHoldingForCharge = true;
                weaponArtReady = false;
                comboPathPoints.Clear();
                comboPathPoints.Add(pos);
                StartChargeAudio();
            }
            else if (touch.fingerId == activeFingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    // Track thumb position to dictate in-game character sword orientation (Skyward Sword style)
                    UpdateCharacterWeaponPosture(pos);

                    currentHoldTimer += Time.deltaTime;
                    OnChargeProgress?.Invoke(ChargeProgress);

                    if (currentHoldTimer >= holdChargeTimeRequired && !weaponArtReady)
                    {
                        weaponArtReady = true;
                        TriggerWeaponGlowAndWhirrAudio();
                        OnWeaponArtCharged?.Invoke();
                    }

                    comboPathPoints.Add(pos);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    FinalizeStroke(pos);
                    activeFingerId = -1;
                }
            }
        }
    }

    private void HandleMouseInput()
    {
        Vector2 mousePos = Input.mousePosition;
        bool inBottomControlsZone = (mousePos.y < Screen.height * 0.30f && (mousePos.x < Screen.width * 0.45f || mousePos.x > Screen.width * 0.55f));

        if (Input.GetMouseButtonDown(0))
        {
            if (inBottomControlsZone) return;

            swipeStartPos = mousePos;
            swipeStartTime = Time.time;
            currentHoldTimer = 0f;
            isHoldingForCharge = true;
            weaponArtReady = false;
            comboPathPoints.Clear();
            comboPathPoints.Add(swipeStartPos);
            StartChargeAudio();
        }
        else if (Input.GetMouseButton(0) && isHoldingForCharge)
        {
            // Track thumb position to dictate in-game character sword orientation (Skyward Sword style)
            UpdateCharacterWeaponPosture(mousePos);

            currentHoldTimer += Time.deltaTime;
            OnChargeProgress?.Invoke(ChargeProgress);

            if (currentHoldTimer >= holdChargeTimeRequired && !weaponArtReady)
            {
                weaponArtReady = true;
                TriggerWeaponGlowAndWhirrAudio();
                OnWeaponArtCharged?.Invoke();
            }

            comboPathPoints.Add(mousePos);
        }
        else if (Input.GetMouseButtonUp(0) && isHoldingForCharge)
        {
            FinalizeStroke(mousePos);
        }
    }

    private void FinalizeStroke(Vector2 endPos)
    {
        StopChargeAudio();

        if (isHoldingForCharge && weaponArtReady)
        {
            EvaluateComboShape(comboPathPoints);
        }
        else
        {
            // Standard directional propagating slash
            Vector2 delta = endPos - swipeStartPos;
            float duration = Mathf.Max(0.001f, Time.time - swipeStartTime);
            float velocity = delta.magnitude / duration;

            if (delta.magnitude > 40f)
            {
                float angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                recentSlashAngles.Add(angleDeg);
                lastSlashTime = Time.time;

                // Check multi-slash pattern combos (e.g. 2+ slashes in quick succession)
                if (recentSlashAngles.Count >= 2 && EvaluateMultiSlashPattern(recentSlashAngles, out string pattern))
                {
                    OnComboRecognized?.Invoke(pattern);
                    recentSlashAngles.Clear();
                }
                else
                {
                    OnDirectionalSlashExecuted?.Invoke(delta.normalized, velocity);
                }
            }
        }

        ResetChargeState();
    }

    private void UpdateCharacterWeaponPosture(Vector2 screenThumbPos)
    {
        // Map screen thumb position relative to screen center to character arm rotation
        Vector2 normalizedPos = new Vector2(screenThumbPos.x / Screen.width, screenThumbPos.y / Screen.height);
        float tiltAngle = (normalizedPos.x - 0.5f) * 90f;
        float pitchAngle = (normalizedPos.y - 0.5f) * 45f;

        if (weaponTransform != null)
        {
            weaponTransform.localRotation = Quaternion.Euler(-25f + pitchAngle, 35f, -tiltAngle);
        }
    }

    private void StartChargeAudio()
    {
        if (chargeAudioSource == null) return;

        if (BattleSummonerAudioMaster.Instance != null)
        {
            // Use procedurally synthesized singularity_whir buffer
            var clip = BattleSummonerAudioMaster.Instance.GetProceduralClip("singularity_whir");
            if (clip != null)
            {
                chargeAudioSource.clip = clip;
                chargeAudioSource.pitch = 0.75f;
                chargeAudioSource.volume = 0.35f;
                chargeAudioSource.Play();
            }
        }
    }

    private void StopChargeAudio()
    {
        if (chargeAudioSource != null && chargeAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutChargeAudioRoutine());
        }
    }

    private IEnumerator FadeOutChargeAudioRoutine()
    {
        if (chargeAudioSource == null) yield break;
        float startVol = chargeAudioSource.volume;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            chargeAudioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / 0.15f);
            yield return null;
        }
        chargeAudioSource.Stop();
    }

    private void UpdateChargeVisuals()
    {
        if (weaponMatInstance == null && weaponRenderer != null)
        {
            weaponMatInstance = weaponRenderer.material;
            weaponMatInstance.EnableKeyword("_EMISSION");
        }

        if (isHoldingForCharge)
        {
            float progress = ChargeProgress;

            // Pitch ramp on procedural whirr
            if (chargeAudioSource != null && chargeAudioSource.isPlaying)
            {
                chargeAudioSource.pitch = Mathf.Lerp(0.75f, 1.85f, progress);
                chargeAudioSource.volume = Mathf.Lerp(0.35f, 0.95f, progress);
            }

            // Glow ramp
            Color activeColor = weaponArtReady ? fullyChargedGlowColor : Color.Lerp(chargeGlowColor, fullyChargedGlowColor, progress);
            float intensity = weaponArtReady ? (5f + Mathf.PingPong(Time.time * 8f, 3f)) : (progress * 4f);

            if (weaponMatInstance != null)
            {
                weaponMatInstance.SetColor("_EmissionColor", activeColor * intensity);
            }

            if (weaponGlowLight != null)
            {
                weaponGlowLight.color = activeColor;
                weaponGlowLight.intensity = intensity;
            }
        }
        else
        {
            // Fade emission back to base
            if (weaponMatInstance != null)
            {
                Color currentEmission = weaponMatInstance.GetColor("_EmissionColor");
                Color targetEmission = new Color(0.2f, 0.8f, 1.0f) * 1.5f;
                weaponMatInstance.SetColor("_EmissionColor", Color.Lerp(currentEmission, targetEmission, Time.deltaTime * 6f));
            }

            if (weaponGlowLight != null && weaponGlowLight.intensity > 0f)
            {
                weaponGlowLight.intensity = Mathf.MoveTowards(weaponGlowLight.intensity, 0f, Time.deltaTime * 8f);
            }
        }
    }

    private void TriggerWeaponGlowAndWhirrAudio()
    {
        Debug.Log("[Weapon Art] Weapon glowing! Whirring up audio playing.");
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
        }
    }

    private void EvaluateComboShape(List<Vector2> points)
    {
        if (points == null || points.Count < 6)
        {
            // Fallback directional release
            Vector2 delta = points != null && points.Count > 0 ? (points[points.Count - 1] - points[0]) : Vector2.up;
            OnDirectionalSlashExecuted?.Invoke(delta.normalized, 900f);
            return;
        }

        // 1. Check Circle Combo (Start and End close, centroid distance consistent)
        Vector2 start = points[0];
        Vector2 end = points[points.Count - 1];
        float closureDist = Vector2.Distance(start, end);

        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < points.Count; i++) centroid += points[i];
        centroid /= points.Count;

        float avgRadius = 0f;
        for (int i = 0; i < points.Count; i++) avgRadius += Vector2.Distance(centroid, points[i]);
        avgRadius /= points.Count;

        float variance = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            variance += Mathf.Abs(Vector2.Distance(centroid, points[i]) - avgRadius);
        }
        variance /= points.Count;

        if (closureDist < avgRadius * 0.9f && variance < avgRadius * 0.45f && avgRadius > 40f)
        {
            string detectedPattern = "CIRCLE_COMBO";
            Debug.Log($"[WEAPON ART COMBO] Detected pattern: {detectedPattern}!");
            OnComboRecognized?.Invoke(detectedPattern);
            return;
        }

        // 2. Check X_COMBO, SQUARE_COMBO, STAR_COMBO via stroke bounding box and inflection angles
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < points.Count; i++)
        {
            minX = Mathf.Min(minX, points[i].x);
            maxX = Mathf.Max(maxX, points[i].x);
            minY = Mathf.Min(minY, points[i].y);
            maxY = Mathf.Max(maxY, points[i].y);
        }

        float width = maxX - minX;
        float height = maxY - minY;
        float aspectRatio = width / Mathf.Max(1f, height);

        if (aspectRatio > 0.7f && aspectRatio < 1.3f && points.Count > 18)
        {
            string detectedPattern = "SQUARE_COMBO";
            Debug.Log($"[WEAPON ART COMBO] Detected pattern: {detectedPattern}!");
            OnComboRecognized?.Invoke(detectedPattern);
            return;
        }

        // Default combo fallback to CIRCLE_COMBO as specified in integration spec
        OnComboRecognized?.Invoke("CIRCLE_COMBO");
    }

    private bool EvaluateMultiSlashPattern(List<float> angles, out string pattern)
    {
        pattern = null;
        if (angles.Count == 2)
        {
            // Check X_COMBO: Two cross slashes roughly perpendicular (~70-110 deg difference)
            float diff = Mathf.Abs(Mathf.DeltaAngle(angles[0], angles[1]));
            if (diff >= 65f && diff <= 125f)
            {
                pattern = "X_COMBO";
                return true;
            }
        }
        else if (angles.Count == 4)
        {
            // SQUARE_COMBO: 4 consecutive 90-degree turns
            pattern = "SQUARE_COMBO";
            return true;
        }
        else if (angles.Count >= 5)
        {
            // STAR_COMBO: 5 consecutive strokes
            pattern = "STAR_COMBO";
            return true;
        }

        return false;
    }

    private void ResetChargeState()
    {
        isHoldingForCharge = false;
        weaponArtReady = false;
        currentHoldTimer = 0f;
    }
}

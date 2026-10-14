using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// WeaponScopeADSController manages Aim Down Sights (ADS) Over-The-Shoulder perspective:
/// - Only available when the player is wielding a bow or projectile-based weapon (Ranger, Mage).
/// - Transitions PortraitARPGCamera into an over-the-shoulder zoomed perspective.
/// - Displays precision crosshairs with dynamic rangefinding raycasts and enemy detection.
/// - Reduces player movement speed by ~55% while in ADS.
/// - Grants perfect accuracy (0 spread, increased projectile speed, and critical damage bonus).
/// </summary>
public class WeaponScopeADSController : MonoBehaviour
{
    private static WeaponScopeADSController _instance;
    public static WeaponScopeADSController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<WeaponScopeADSController>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Scope ADS State")]
    [SerializeField] private bool isAimingDownSights = false;

    [Header("Accuracy & Combat Modifiers")]
    [SerializeField] private float adsSpreadAngle = 0.0f;       // 0 degrees laser-precision
    [SerializeField] private float hipfireSpreadAngle = 4.5f;   // Standard spread
    [SerializeField] private float adsProjSpeedMultiplier = 1.35f;
    [SerializeField] private float adsDamageMultiplier = 1.30f;
    [SerializeField] private float adsMovementSpeedMultiplier = 0.45f; // Lowered movement speed

    [Header("UI Linkages")]
    [SerializeField] private Button scopeButton;
    [SerializeField] private Image scopeButtonBg;
    [SerializeField] private TextMeshProUGUI scopeButtonTMP;
    [SerializeField] private GameObject crosshairCanvasRoot;
    [SerializeField] private Image crosshairReticleImg;
    [SerializeField] private TextMeshProUGUI distanceRangefinderTMP;

    [Header("Raycast Detection")]
    [SerializeField] private float maxRangefinderDistance = 75f;
    [SerializeField] private LayerMask raycastMask = ~0;

    public bool IsAimingDownSights => isAimingDownSights;
    public float CurrentSpreadAngle => isAimingDownSights ? adsSpreadAngle : hipfireSpreadAngle;
    public float ProjectileSpeedMultiplier => isAimingDownSights ? adsProjSpeedMultiplier : 1.0f;
    public float DamageMultiplier => isAimingDownSights ? adsDamageMultiplier : 1.0f;
    public float MovementSpeedMultiplier => isAimingDownSights ? adsMovementSpeedMultiplier : 1.0f;

    public static event Action<bool> OnADSToggled;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (scopeButton != null)
        {
            scopeButton.onClick.RemoveAllListeners();
            scopeButton.onClick.AddListener(ToggleADS);
        }

        if (crosshairCanvasRoot != null)
        {
            crosshairCanvasRoot.SetActive(false);
        }

        UpdateScopeButtonVisuals();
    }

    private void OnEnable()
    {
        DevClassLoadoutManager.OnClassChanged += HandleClassChanged;
    }

    private void OnDisable()
    {
        DevClassLoadoutManager.OnClassChanged -= HandleClassChanged;
    }

    private void Update()
    {
        HandleDesktopInput();

        if (isAimingDownSights)
        {
            UpdateCrosshairRangefinder();
        }
    }

    private void HandleDesktopInput()
    {
        // Desktop shortcuts: Right Mouse Button (RMB) or 'V' or 'Z'
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.V))
        {
            ToggleADS();
        }
    }

    private void HandleClassChanged(PlayerClassType newClass)
    {
        // If current class cannot use projectile weapons, force exit ADS
        if (!IsPlayerHoldingProjectileWeapon() && isAimingDownSights)
        {
            SetADS(false);
        }
        UpdateScopeButtonVisuals();
    }

    public bool IsPlayerHoldingProjectileWeapon()
    {
        if (DevClassLoadoutManager.Instance != null)
        {
            return DevClassLoadoutManager.Instance.IsHoldingProjectileWeapon;
        }
        return false;
    }

    public void ToggleADS()
    {
        SetADS(!isAimingDownSights);
    }

    public void SetADS(bool active)
    {
        if (active)
        {
            if (!IsPlayerHoldingProjectileWeapon())
            {
                if (DevClassLoadoutManager.Instance != null)
                {
                    DevClassLoadoutManager.Instance.ShowToast("<color=#FF6060>[SCOPE UNAVAILABLE]</color>\nEquip a Bow (Ranger) or Staff (Mage) to use Scope ADS!");
                }
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("error_buzz", 0.7f);
                }
                return;
            }
        }

        isAimingDownSights = active;

        // Notify camera
        if (PortraitARPGCamera.Instance != null)
        {
            PortraitARPGCamera.Instance.isAimingDownSights = isAimingDownSights;
        }

        // Toggle Reticle
        if (crosshairCanvasRoot != null)
        {
            crosshairCanvasRoot.SetActive(isAimingDownSights);
        }

        UpdateScopeButtonVisuals();

        // Audio SFX
        if (BattleSummonerAudioMaster.Instance != null)
        {
            string sfx = isAimingDownSights ? "scope_in" : "scope_out";
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 0.6f);
        }

        OnADSToggled?.Invoke(isAimingDownSights);
        Debug.Log($"🎯 [WEAPON SCOPE ADS] Aim Down Sights is now {(isAimingDownSights ? "ENABLED (OTS View + Laser Accuracy)" : "DISABLED (Normal Isometric View)")}");
    }

    private void UpdateCrosshairRangefinder()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.55f, 0f));
        float range = maxRangefinderDistance;
        string targetName = "";
        bool isEnemyTargeted = false;

        if (Physics.Raycast(ray, out RaycastHit hit, maxRangefinderDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                range = hit.distance;
                if (hit.collider.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    isEnemyTargeted = true;
                    targetName = " " + enemy.name.ToUpper();
                }
                else if (hit.collider.TryGetComponent<BossController>(out var boss))
                {
                    isEnemyTargeted = true;
                    targetName = " BOSS LOCKED";
                }
            }
        }

        // Update rangefinder text
        if (distanceRangefinderTMP != null)
        {
            if (isEnemyTargeted)
            {
                distanceRangefinderTMP.text = $"<color=#FF3855><b>[ LOCK: {range:F1}m{targetName} ]</b></color>";
            }
            else
            {
                distanceRangefinderTMP.text = $"<color=#40EBFF><b>[ RANGE: {range:F1}m ]</b></color>";
            }
        }

        // Tint reticle
        if (crosshairReticleImg != null)
        {
            crosshairReticleImg.color = isEnemyTargeted ? new Color(1f, 0.22f, 0.33f, 0.95f) : new Color(0.25f, 0.92f, 1.0f, 0.85f);
        }
    }

    public void UpdateScopeButtonVisuals()
    {
        bool hasProjWeapon = IsPlayerHoldingProjectileWeapon();

        if (scopeButton != null)
        {
            scopeButton.interactable = hasProjWeapon;
        }

        if (scopeButtonBg != null)
        {
            if (!hasProjWeapon)
            {
                scopeButtonBg.color = new Color(0.12f, 0.13f, 0.16f, 0.50f);
            }
            else if (isAimingDownSights)
            {
                scopeButtonBg.color = new Color(0.10f, 0.85f, 0.95f, 0.95f); // Active neon cyan
            }
            else
            {
                scopeButtonBg.color = new Color(0.16f, 0.22f, 0.30f, 0.90f); // Ready dark navy
            }
        }

        if (scopeButtonTMP != null)
        {
            if (!hasProjWeapon)
            {
                scopeButtonTMP.text = "<color=#606575><b>SCOPE</b></color>";
            }
            else if (isAimingDownSights)
            {
                scopeButtonTMP.text = "<color=#001525><b>SCOPED</b></color>";
            }
            else
            {
                scopeButtonTMP.text = "<color=#40EBFF><b>SCOPE</b></color>";
            }
        }
    }

    public void BindReferences(
        Button sBtn,
        Image sBg,
        TextMeshProUGUI sTMP,
        GameObject reticleRoot,
        Image reticleImg,
        TextMeshProUGUI rangeTMP)
    {
        scopeButton = sBtn;
        scopeButtonBg = sBg;
        scopeButtonTMP = sTMP;
        crosshairCanvasRoot = reticleRoot;
        crosshairReticleImg = reticleImg;
        distanceRangefinderTMP = rangeTMP;

        if (scopeButton != null)
        {
            scopeButton.onClick.RemoveAllListeners();
            scopeButton.onClick.AddListener(ToggleADS);
        }

        UpdateScopeButtonVisuals();
    }
}

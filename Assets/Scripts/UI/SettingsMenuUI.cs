using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SettingsTab
{
    Audio,
    Graphics,
    Gameplay
}

/// <summary>
/// SettingsMenuUI provides comprehensive configuration over:
/// - Audio: Master, BGM, SFX, and Voice/TTS volume levels
/// - Graphics: Framerate targets (30/60/120 FPS), Dynamic Portrait FOV, Bloom VFX, Shadows
/// - Gameplay: Camera Orbit Sensitivity, Auto-Aim Assist, Gesture Stroke Tolerance, Screen Shake
/// Persists all values to PlayerPrefs and applies changes immediately to engine systems.
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    private static SettingsMenuUI _instance;
    public static SettingsMenuUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SettingsMenuUI>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Root & Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button closeButton;

    [Header("Tab Headers")]
    [SerializeField] private Button audioTabBtn;
    [SerializeField] private Button graphicsTabBtn;
    [SerializeField] private Button gameplayTabBtn;

    [Header("Tab Container Panels")]
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject gameplayPanel;

    [Header("Audio Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private TextMeshProUGUI bgmVolumeLabel;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
    [SerializeField] private Slider voiceVolumeSlider;
    [SerializeField] private TextMeshProUGUI voiceVolumeLabel;

    [Header("Graphics Controls")]
    [SerializeField] private Button fpsCapToggleBtn;
    [SerializeField] private TextMeshProUGUI fpsCapLabel;
    [SerializeField] private Slider cameraFovSlider;
    [SerializeField] private TextMeshProUGUI cameraFovLabel;
    [SerializeField] private Toggle bloomVfxToggle;
    [SerializeField] private Toggle dynamicShadowsToggle;

    [Header("Gameplay Controls")]
    [SerializeField] private Slider lookSensitivitySlider;
    [SerializeField] private TextMeshProUGUI lookSensitivityLabel;
    [SerializeField] private Toggle autoAimAssistToggle;
    [SerializeField] private Slider gestureToleranceSlider;
    [SerializeField] private TextMeshProUGUI gestureToleranceLabel;
    [SerializeField] private Toggle screenShakeToggle;

    [Header("Footer Actions")]
    [SerializeField] private Button saveAndApplyButton;
    [SerializeField] private Button resetDefaultsButton;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    // Runtime Cached Values
    private float masterVol = 1.0f;
    private float bgmVol = 0.8f;
    private float sfxVol = 0.9f;
    private float voiceVol = 1.0f;
    private int targetFps = 60;
    private float customFov = 72f;
    private bool bloomEnabled = true;
    private bool shadowsEnabled = true;
    private float lookSensitivity = 0.22f;
    private bool autoAimEnabled = true;
    private float gestureTolerance = 1.0f; // 0.8=Strict, 1.0=Normal, 1.2=Forgiving
    private bool screenShakeEnabled = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        LoadSettings();
    }

    private void Start()
    {
        if (overlayRoot == null) overlayRoot = gameObject;
        BindEventListeners();
        SetVisible(false);
    }

    private void Update()
    {
        // Desktop shortcut: 'O' or 'Escape'
        if (Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    public void ToggleSettings()
    {
        SetVisible(!IsOpen);
    }

    public void SetVisible(bool visible)
    {
        if (overlayRoot != null) overlayRoot.SetActive(visible);

        Transform parentTrans = transform.parent;
        if (parentTrans != null)
        {
            Transform hud = parentTrans.Find("MockupMasterHUD");
            if (hud != null) hud.gameObject.SetActive(!visible);

            Transform safeArea = parentTrans.Find("SafeAreaContainer");
            if (safeArea != null) safeArea.gameObject.SetActive(!visible);
        }
        else
        {
            GameObject hudGo = GameObject.Find("MockupMasterHUD");
            if (hudGo != null) hudGo.SetActive(!visible);
            GameObject safeGo = GameObject.Find("SafeAreaContainer");
            if (safeGo != null) safeGo.SetActive(!visible);
        }

        if (visible)
        {
            SelectTab(SettingsTab.Audio);
            UpdateAllUIElements();
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.9f);
            }
        }
        else
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.8f);
            }
        }
    }

    private void BindEventListeners()
    {
        if (closeButton != null) closeButton.onClick.AddListener(() => SetVisible(false));
        if (audioTabBtn != null) audioTabBtn.onClick.AddListener(() => SelectTab(SettingsTab.Audio));
        if (graphicsTabBtn != null) graphicsTabBtn.onClick.AddListener(() => SelectTab(SettingsTab.Graphics));
        if (gameplayTabBtn != null) gameplayTabBtn.onClick.AddListener(() => SelectTab(SettingsTab.Gameplay));

        // Audio Sliders
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(v => { masterVol = v; UpdateLabels(); });
        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(v => { bgmVol = v; UpdateLabels(); });
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(v => { sfxVol = v; UpdateLabels(); });
        if (voiceVolumeSlider != null) voiceVolumeSlider.onValueChanged.AddListener(v => { voiceVol = v; UpdateLabels(); });

        // Graphics Sliders & Toggles
        if (fpsCapToggleBtn != null) fpsCapToggleBtn.onClick.AddListener(CycleTargetFps);
        if (cameraFovSlider != null) cameraFovSlider.onValueChanged.AddListener(v => { customFov = v; UpdateLabels(); });
        if (bloomVfxToggle != null) bloomVfxToggle.onValueChanged.AddListener(b => bloomEnabled = b);
        if (dynamicShadowsToggle != null) dynamicShadowsToggle.onValueChanged.AddListener(b => shadowsEnabled = b);

        // Gameplay Sliders & Toggles
        if (lookSensitivitySlider != null) lookSensitivitySlider.onValueChanged.AddListener(v => { lookSensitivity = v; UpdateLabels(); });
        if (autoAimAssistToggle != null) autoAimAssistToggle.onValueChanged.AddListener(b => autoAimEnabled = b);
        if (gestureToleranceSlider != null) gestureToleranceSlider.onValueChanged.AddListener(v => { gestureTolerance = v; UpdateLabels(); });
        if (screenShakeToggle != null) screenShakeToggle.onValueChanged.AddListener(b => screenShakeEnabled = b);

        // Actions
        if (saveAndApplyButton != null) saveAndApplyButton.onClick.AddListener(SaveAndApplySettings);
        if (resetDefaultsButton != null) resetDefaultsButton.onClick.AddListener(ResetDefaults);
    }

    public void SelectTab(SettingsTab tab)
    {
        if (audioPanel != null) audioPanel.SetActive(tab == SettingsTab.Audio);
        if (graphicsPanel != null) graphicsPanel.SetActive(tab == SettingsTab.Graphics);
        if (gameplayPanel != null) gameplayPanel.SetActive(tab == SettingsTab.Gameplay);

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.6f);
        }
    }

    private void CycleTargetFps()
    {
        targetFps = targetFps switch
        {
            30 => 60,
            60 => 120,
            _ => 30
        };
        UpdateLabels();
    }

    private void UpdateAllUIElements()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgmVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (voiceVolumeSlider != null) voiceVolumeSlider.value = voiceVol;

        if (cameraFovSlider != null) cameraFovSlider.value = customFov;
        if (bloomVfxToggle != null) bloomVfxToggle.isOn = bloomEnabled;
        if (dynamicShadowsToggle != null) dynamicShadowsToggle.isOn = shadowsEnabled;

        if (lookSensitivitySlider != null) lookSensitivitySlider.value = lookSensitivity;
        if (autoAimAssistToggle != null) autoAimAssistToggle.isOn = autoAimEnabled;
        if (gestureToleranceSlider != null) gestureToleranceSlider.value = gestureTolerance;
        if (screenShakeToggle != null) screenShakeToggle.isOn = screenShakeEnabled;

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if (masterVolumeLabel != null) masterVolumeLabel.text = $"{Mathf.RoundToInt(masterVol * 100)}%";
        if (bgmVolumeLabel != null) bgmVolumeLabel.text = $"{Mathf.RoundToInt(bgmVol * 100)}%";
        if (sfxVolumeLabel != null) sfxVolumeLabel.text = $"{Mathf.RoundToInt(sfxVol * 100)}%";
        if (voiceVolumeLabel != null) voiceVolumeLabel.text = $"{Mathf.RoundToInt(voiceVol * 100)}%";

        if (fpsCapLabel != null) fpsCapLabel.text = $"{targetFps} FPS";
        if (cameraFovLabel != null) cameraFovLabel.text = $"{Mathf.RoundToInt(customFov)} deg";
        if (lookSensitivityLabel != null) lookSensitivityLabel.text = $"{lookSensitivity:F2}x";
        if (gestureToleranceLabel != null)
        {
            string mode = gestureTolerance < 0.9f ? "Strict" : (gestureTolerance > 1.1f ? "Forgiving" : "Standard");
            gestureToleranceLabel.text = mode;
        }
    }

    public void SaveAndApplySettings()
    {
        // 1. Audio
        AudioListener.volume = masterVol;
        PlayerPrefs.SetFloat("Setting_MasterVol", masterVol);
        PlayerPrefs.SetFloat("Setting_BgmVol", bgmVol);
        PlayerPrefs.SetFloat("Setting_SfxVol", sfxVol);
        PlayerPrefs.SetFloat("Setting_VoiceVol", voiceVol);

        // 2. Graphics
        Application.targetFrameRate = targetFps;
        PlayerPrefs.SetInt("Setting_TargetFps", targetFps);
        PlayerPrefs.SetFloat("Setting_Fov", customFov);
        PlayerPrefs.SetInt("Setting_Bloom", bloomEnabled ? 1 : 0);
        PlayerPrefs.SetInt("Setting_Shadows", shadowsEnabled ? 1 : 0);

        if (Camera.main != null && PortraitARPGCamera.Instance != null && !PortraitARPGCamera.Instance.isAimingDownSights)
        {
            Camera.main.fieldOfView = customFov;
        }

        // 3. Gameplay
        PlayerPrefs.SetFloat("Setting_LookSensitivity", lookSensitivity);
        PlayerPrefs.SetInt("Setting_AutoAim", autoAimEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("Setting_GestureTolerance", gestureTolerance);
        PlayerPrefs.SetInt("Setting_ScreenShake", screenShakeEnabled ? 1 : 0);

        PlayerPrefs.Save();

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
        }
        if (DevClassLoadoutManager.Instance != null)
        {
            DevClassLoadoutManager.Instance.ShowToast("<color=#30FF80>[SETTINGS APPLIED]</color>\nPreferences saved successfully.");
        }

        SetVisible(false);
    }

    public void ResetDefaults()
    {
        masterVol = 1.0f;
        bgmVol = 0.8f;
        sfxVol = 0.9f;
        voiceVol = 1.0f;
        targetFps = 60;
        customFov = 72f;
        bloomEnabled = true;
        shadowsEnabled = true;
        lookSensitivity = 0.22f;
        autoAimEnabled = true;
        gestureTolerance = 1.0f;
        screenShakeEnabled = true;

        UpdateAllUIElements();
        SaveAndApplySettings();
    }

    public void LoadSettings()
    {
        masterVol = PlayerPrefs.GetFloat("Setting_MasterVol", 1.0f);
        bgmVol = PlayerPrefs.GetFloat("Setting_BgmVol", 0.8f);
        sfxVol = PlayerPrefs.GetFloat("Setting_SfxVol", 0.9f);
        voiceVol = PlayerPrefs.GetFloat("Setting_VoiceVol", 1.0f);
        targetFps = PlayerPrefs.GetInt("Setting_TargetFps", 60);
        customFov = PlayerPrefs.GetFloat("Setting_Fov", 72f);
        bloomEnabled = PlayerPrefs.GetInt("Setting_Bloom", 1) == 1;
        shadowsEnabled = PlayerPrefs.GetInt("Setting_Shadows", 1) == 1;
        lookSensitivity = PlayerPrefs.GetFloat("Setting_LookSensitivity", 0.22f);
        autoAimEnabled = PlayerPrefs.GetInt("Setting_AutoAim", 1) == 1;
        gestureTolerance = PlayerPrefs.GetFloat("Setting_GestureTolerance", 1.0f);
        screenShakeEnabled = PlayerPrefs.GetInt("Setting_ScreenShake", 1) == 1;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PortraitHUDController : MonoBehaviour
{
    [Header("Top Telemetry HUD References")]
    [SerializeField] private Text floorIndexText;
    [SerializeField] private Slider bossHealthBar;
    [SerializeField] private Slider satietyGauge;
    [SerializeField] private Slider playerHealthBar;
    [SerializeField] private Slider playerManaBar;
    [SerializeField] private TextMeshProUGUI gemVaultCounterTMP;
    [SerializeField] private Text activeCompanionSlotText;

    [Header("Top Left: Settings & AI Auto")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button autoBattlerAIButton;

    [Header("Top Left: Player Stats Frame")]
    [SerializeField] private TextMeshProUGUI playerHpValueTMP;
    [SerializeField] private TextMeshProUGUI playerMpValueTMP;
    [SerializeField] private TextMeshProUGUI playerLevelTMP;
    [SerializeField] private TextMeshProUGUI broadcastBannerTMP;
    [SerializeField] private Button messengerBagButton;

    [Header("Bottom Control Deck (Belt Slots)")]
    [SerializeField] private Button whetstoneItemButton;
    [SerializeField] private Button miningChiselToggleButton;
    [SerializeField] private Button expeditionSafariButton;
    [SerializeField] private Button emergencyManaDraftButton;
    [SerializeField] private Button storeButton;
    [SerializeField] private Button inspectRelicButton;
    [SerializeField] private Button tribunalAppealButton;
    [SerializeField] private Button invokeMargoDialogueButton;
    [SerializeField] private Button kalimbaInstrumentToggleButton;
    [SerializeField] private Button summoningRitualButton;
    [SerializeField] private Button campfireHubButton;
    [SerializeField] private Button socialLinkButton;
    [SerializeField] private Button customizeAvatarButton;

    [Header("Subsystems & Modals")]
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private LivingGearSystem livingGear;
    [SerializeField] private WeaponTemper weaponTemper;
    [SerializeField] private BossController bossController;
    [SerializeField] private PortraitGestureCanvas gestureCanvas;
    [SerializeField] private GameObject storeModal;
    [SerializeField] private GameObject relicOverlay;
    [SerializeField] private GameObject expeditionBoardModal;
    [SerializeField] private GameObject kalimbaOverlayPanel;
    [SerializeField] private GameObject summoningRitualModal;

    private int currentFloor = 14;

    private void Awake()
    {
        ResolveSubsystems();
        BindButtons();
    }

    [Header("Battle Side Profile HUD (Persona Style)")]
    [SerializeField] private BattleSummoner.UI.BattleSideProfileHUDController battleSideProfileHUD;

    private void Start()
    {
        if (battleSideProfileHUD == null)
        {
            battleSideProfileHUD = BattleSummoner.UI.BattleSideProfileHUDController.EnsureInScene(this.transform);
        }

        // Only instantiate standalone QuickItemCarousel and ZTargeting if MockupMasterHUD is NOT active
        if (transform.Find("MockupMasterHUD") == null)
        {
            QuickItemCarouselController.EnsureInScene(this.transform);
            BattleSummoner.Combat.QuickZTargetingController.EnsureInScene(this.transform);
        }
        else
        {
            var oldCarousel = transform.Find("QuickItemCarousel");
            if (oldCarousel != null && oldCarousel.parent == this.transform) oldCarousel.gameObject.SetActive(false);
            var oldZTarget = transform.Find("QuickZTargetingController");
            if (oldZTarget != null && oldZTarget.parent == this.transform) oldZTarget.gameObject.SetActive(false);
        }

        var playerObj = GameObject.Find("Player_Hero");
        BattleSummoner.Combat.WizardSpellSlingerController.EnsureInScene(playerObj != null ? playerObj.transform : this.transform);

        UpdateFloorText();
        UpdateAllGauges();
        UpdateVaultDisplay();
        UpdateCompanionDisplay(NPCExpeditionManager.Instance != null ? NPCExpeditionManager.Instance.activeRaidCompanion : null);
    }

    private void OnEnable()
    {
        if (playerProgression != null)
        {
            playerProgression.OnHealthChanged += HandlePlayerHealthChanged;
            playerProgression.OnManaChanged += HandlePlayerManaChanged;
            playerProgression.OnLevelUpEvent += HandleLevelUp;
        }

        if (bossController != null)
        {
            bossController.OnBossHealthChanged += HandleBossHealthChanged;
        }

        if (weaponTemper != null)
        {
            weaponTemper.OnTemperChanged += HandleTemperChanged;
        }

        if (GemologySystem.Instance != null)
        {
            GemologySystem.Instance.OnInventoryUpdated += HandleGemUpdated;
            GemologySystem.Instance.OnManaCoresChanged += HandleCoresChanged;
        }

        NPCExpeditionManager.OnRaidCompanionChanged += UpdateCompanionDisplay;
    }

    private void OnDisable()
    {
        if (playerProgression != null)
        {
            playerProgression.OnHealthChanged -= HandlePlayerHealthChanged;
            playerProgression.OnManaChanged -= HandlePlayerManaChanged;
            playerProgression.OnLevelUpEvent -= HandleLevelUp;
        }

        if (bossController != null)
        {
            bossController.OnBossHealthChanged -= HandleBossHealthChanged;
        }

        if (weaponTemper != null)
        {
            weaponTemper.OnTemperChanged -= HandleTemperChanged;
        }

        if (GemologySystem.Instance != null)
        {
            GemologySystem.Instance.OnInventoryUpdated -= HandleGemUpdated;
            GemologySystem.Instance.OnManaCoresChanged -= HandleCoresChanged;
        }

        NPCExpeditionManager.OnRaidCompanionChanged -= UpdateCompanionDisplay;
    }

    private void Update()
    {
        if (livingGear != null && satietyGauge != null)
        {
            satietyGauge.value = livingGear.currentSatiety / Mathf.Max(1f, livingGear.maxSatiety);
        }

        // Quick Campfire Rest shortcut (Key C)
        if (Input.GetKeyDown(KeyCode.C))
        {
            OnCampfireHubClicked();
        }
    }

    private void ResolveSubsystems()
    {
        if (playerProgression == null) playerProgression = FindAnyObjectByType<PlayerProgression>();
        if (livingGear == null) livingGear = FindAnyObjectByType<LivingGearSystem>();
        if (weaponTemper == null) weaponTemper = FindAnyObjectByType<WeaponTemper>();
        if (bossController == null) bossController = FindAnyObjectByType<BossController>();
        if (gestureCanvas == null) gestureCanvas = FindAnyObjectByType<PortraitGestureCanvas>();
    }

    private void BindButtons()
    {
        if (settingsButton == null)
        {
            var obj = GameObject.Find("SettingsButton");
            if (obj != null) settingsButton = obj.GetComponent<Button>();
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (autoBattlerAIButton == null)
        {
            var obj = GameObject.Find("AutoBattlerAIButton");
            if (obj != null) autoBattlerAIButton = obj.GetComponent<Button>();
        }
        if (autoBattlerAIButton != null)
        {
            autoBattlerAIButton.onClick.RemoveAllListeners();
            autoBattlerAIButton.onClick.AddListener(OnAutoBattlerClicked);
        }

        if (messengerBagButton == null)
        {
            var obj = GameObject.Find("MessengerBagButton");
            if (obj != null) messengerBagButton = obj.GetComponent<Button>();
        }
        if (messengerBagButton != null)
        {
            messengerBagButton.onClick.RemoveAllListeners();
            messengerBagButton.onClick.AddListener(() =>
            {
                if (PlayerAccessoryMenuUI.Instance != null)
                {
                    PlayerAccessoryMenuUI.Instance.ToggleAccessoryDrawer();
                }
                else
                {
                    OnStoreClicked();
                }
            });
        }

        // Whetstone is accessed via QuickItemCarouselController; deactivate standalone button
        if (whetstoneItemButton != null)
        {
            whetstoneItemButton.gameObject.SetActive(false);
        }
        else
        {
            var obj = GameObject.Find("WhetstoneButton");
            if (obj != null) obj.SetActive(false);
        }

        if (quickPotionButtons == null || quickPotionButtons.Length == 0)
        {
            var pList = new System.Collections.Generic.List<Button>();
            for (int i = 1; i <= 4; i++)
            {
                var pObj = GameObject.Find($"PotionSlot_{i}");
                if (pObj != null)
                {
                    var btn = pObj.GetComponent<Button>();
                    if (btn != null) pList.Add(btn);
                }
            }
            if (pList.Count > 0) quickPotionButtons = pList.ToArray();
        }

        if (quickPotionButtons != null)
        {
            for (int i = 0; i < quickPotionButtons.Length; i++)
            {
                int slotIdx = i;
                if (quickPotionButtons[i] != null)
                {
                    quickPotionButtons[i].onClick.RemoveAllListeners();
                    quickPotionButtons[i].onClick.AddListener(() => OnPotionClicked(slotIdx));
                }
            }
        }

        if (miningChiselToggleButton != null)
        {
            miningChiselToggleButton.onClick.AddListener(OnMiningChiselToggleClicked);
        }

        if (expeditionSafariButton != null)
        {
            expeditionSafariButton.onClick.AddListener(OnExpeditionSafariClicked);
        }

        if (emergencyManaDraftButton != null)
        {
            emergencyManaDraftButton.onClick.AddListener(OnManaDraftClicked);
        }

        if (storeButton != null)
        {
            storeButton.onClick.AddListener(OnStoreClicked);
        }

        if (inspectRelicButton != null)
        {
            inspectRelicButton.onClick.AddListener(OnInspectRelicClicked);
        }

        if (tribunalAppealButton != null)
        {
            tribunalAppealButton.onClick.AddListener(OnTribunalClicked);
        }

        if (invokeMargoDialogueButton != null)
        {
            invokeMargoDialogueButton.onClick.AddListener(OnInvokeMargoClicked);
        }

        if (kalimbaInstrumentToggleButton != null)
        {
            kalimbaInstrumentToggleButton.onClick.AddListener(OnKalimbaToggleClicked);
        }

        if (summoningRitualButton != null)
        {
            summoningRitualButton.onClick.AddListener(OnSummoningRitualClicked);
        }

        if (campfireHubButton != null)
        {
            campfireHubButton.onClick.AddListener(OnCampfireHubClicked);
        }

        if (socialLinkButton == null)
        {
            var obj = GameObject.Find("SocialLinkButton");
            if (obj != null) socialLinkButton = obj.GetComponent<Button>();
        }
        if (socialLinkButton != null)
        {
            socialLinkButton.onClick.RemoveAllListeners();
            socialLinkButton.onClick.AddListener(OnSocialLinkClicked);
        }

        if (customizeAvatarButton == null)
        {
            var obj = GameObject.Find("CustomizeAvatarButton");
            if (obj != null) customizeAvatarButton = obj.GetComponent<Button>();
        }
        if (customizeAvatarButton != null)
        {
            customizeAvatarButton.onClick.RemoveAllListeners();
            customizeAvatarButton.onClick.AddListener(OnCustomizeAvatarClicked);
        }
    }

    [Header("Quick Potions")]
    [SerializeField] private Button[] quickPotionButtons;

    public void BindMockupReferences(
        Button settingsBtn,
        Button aiAutoBtn,
        Button whetstoneBtn,
        Button bagBtn,
        Slider hpSlider,
        Slider mpSlider,
        TextMeshProUGUI hpTxt,
        TextMeshProUGUI mpTxt,
        TextMeshProUGUI lvlTxt,
        TextMeshProUGUI bannerTxt,
        Button[] potionBtns = null)
    {
        settingsButton = settingsBtn;
        autoBattlerAIButton = aiAutoBtn;
        whetstoneItemButton = whetstoneBtn;
        messengerBagButton = bagBtn;
        playerHealthBar = hpSlider;
        playerManaBar = mpSlider;
        playerHpValueTMP = hpTxt;
        playerMpValueTMP = mpTxt;
        playerLevelTMP = lvlTxt;
        broadcastBannerTMP = bannerTxt;
        quickPotionButtons = potionBtns;

        if (quickPotionButtons != null)
        {
            for (int i = 0; i < quickPotionButtons.Length; i++)
            {
                int slotIdx = i;
                if (quickPotionButtons[i] != null)
                {
                    quickPotionButtons[i].onClick.RemoveAllListeners();
                    quickPotionButtons[i].onClick.AddListener(() => OnPotionClicked(slotIdx));
                }
            }
        }

        ResolveSubsystems();
        BindButtons();
        UpdateAllGauges();
    }

    public void OnPotionClicked(int slotIndex)
    {
        if (playerProgression != null)
        {
            if (slotIndex == 0 || slotIndex == 2)
            {
                playerProgression.Heal(250f);
                if (DevClassLoadoutManager.Instance != null)
                    DevClassLoadoutManager.Instance.ShowToast("<color=#FF4444>[HEALING POTION]</color>\nRestored +250 HP!");
            }
            else if (slotIndex == 1)
            {
                playerProgression.RestoreMana(150f);
                if (DevClassLoadoutManager.Instance != null)
                    DevClassLoadoutManager.Instance.ShowToast("<color=#FFB300>[AMBER DRAUGHT]</color>\nRestored +150 MP!");
            }
            else if (slotIndex == 3)
            {
                playerProgression.Heal(250f);
                playerProgression.RestoreMana(150f);
                if (DevClassLoadoutManager.Instance != null)
                    DevClassLoadoutManager.Instance.ShowToast("<color=#44AAFF>[PRIMORDIAL ELIXIR]</color>\nRestored +250 HP & +150 MP!");
            }
            if (BattleSummonerAudioMaster.Instance != null)
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.9f);
        }
    }

    private void OnAutoBattlerClicked()
    {
        if (AutoBattlerAIController.Instance != null)
        {
            AutoBattlerAIController.Instance.ToggleAutoBattler();
            bool active = AutoBattlerAIController.Instance.IsActive;
            if (autoBattlerAIButton != null)
            {
                var img = autoBattlerAIButton.GetComponent<Image>();
                if (img != null) img.color = active ? new Color(1f, 0.9f, 0.4f) : Color.white;
            }
        }
        else
        {
            var playerObj = GameObject.Find("Player_Hero");
            if (playerObj != null)
            {
                var autoAI = playerObj.GetComponent<AutoBattlerAIController>() ?? playerObj.AddComponent<AutoBattlerAIController>();
                autoAI.ToggleAutoBattler();
            }
        }
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[SETTINGS] Opening Settings modal.");
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.9f);
        }
        if (DevClassLoadoutManager.Instance != null)
        {
            DevClassLoadoutManager.Instance.ShowToast("<color=#FFD700>[SETTINGS]</color>\nResolution: 1080x1920 (9:16 Portrait)\nGraphics: Linear / Vulkan API");
        }
    }

    private void OnWhetstoneClicked()
    {
        if (weaponTemper != null)
        {
            weaponTemper.HoneWithWhetstone();
        }
        else if (livingGear != null)
        {
            livingGear.RestoreFractureDebtAtForge();
        }
    }

    private void OnMiningChiselToggleClicked()
    {
        if (gestureCanvas == null) gestureCanvas = FindAnyObjectByType<PortraitGestureCanvas>();
        if (gestureCanvas != null)
        {
            gestureCanvas.currentMode = (gestureCanvas.currentMode == InteractionMode.MiningChisel) 
                ? InteractionMode.Combat 
                : InteractionMode.MiningChisel;

            Text btnTxt = miningChiselToggleButton.GetComponentInChildren<Text>();
            if (btnTxt != null)
            {
                btnTxt.text = (gestureCanvas.currentMode == InteractionMode.MiningChisel) 
                    ? "CHISEL: ON\n(Tap Veins)" 
                    : "MINING CHISEL\n(Toggle Mode)";
            }
            Debug.Log($"[GESTURE MODE] Switched to: {gestureCanvas.currentMode}");
        }
    }

    private void OnExpeditionSafariClicked()
    {
        if (expeditionBoardModal != null)
        {
            expeditionBoardModal.SetActive(!expeditionBoardModal.activeSelf);
        }
    }

    private void OnManaDraftClicked()
    {
        if (playerProgression != null)
        {
            playerProgression.RestoreMana(35f);
        }
    }

    private void OnStoreClicked()
    {
        if (storeModal != null)
        {
            storeModal.SetActive(!storeModal.activeSelf);
        }
    }

    private void OnInspectRelicClicked()
    {
        if (relicOverlay != null)
        {
            relicOverlay.SetActive(!relicOverlay.activeSelf);
        }
    }

    public void OnTribunalClicked()
    {
        if (TribunalAppealController.Instance != null)
        {
            TribunalAppealController.Instance.OpenTribunalMonolithUI();
        }
    }

    public void OnInvokeMargoClicked()
    {
        if (MargoGuideCompanion.Instance != null)
        {
            MargoGuideCompanion.Instance.ProvideOnDemandGuidance();
        }
        else if (UnfilteredDialoguePipeline.Instance != null)
        {
            UnfilteredDialoguePipeline.Instance.ProcessPlayerInput("Margo, what guidance do you have for surviving this floor of the Spire?", "npc_margo");
        }
    }

    public void OnCampfireHubClicked()
    {
        Debug.Log("[HUD] Setting up Campfire Hub...");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.PitchCamp("Campfire Rest");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CampfireHub");
        }
    }

    public void OnKalimbaToggleClicked()
    {
        if (InstrumentCatalyst.Instance != null)
        {
            InstrumentCatalyst.Instance.ToggleInstrument();
        }
        else if (kalimbaOverlayPanel != null)
        {
            kalimbaOverlayPanel.SetActive(!kalimbaOverlayPanel.activeSelf);
        }
    }

    public void OnSummoningRitualClicked()
    {
        if (SummoningRitualManager.Instance != null)
        {
            SummoningRitualManager.Instance.OpenSummoningRitualModal();
        }
        else if (summoningRitualModal != null)
        {
            summoningRitualModal.SetActive(!summoningRitualModal.activeSelf);
        }
    }

    public void OnSocialLinkClicked()
    {
        if (BattleSummoner.Social.TomodachiSocialHubUI.Instance != null)
        {
            BattleSummoner.Social.TomodachiSocialHubUI.Instance.ToggleSocialHub();
        }
    }

    public void OnCustomizeAvatarClicked()
    {
        if (BattleSummoner.Customization.CharacterCustomizationUI.Instance != null)
        {
            BattleSummoner.Customization.CharacterCustomizationUI.Instance.ToggleCustomizationScreen();
        }
    }

    private void UpdateFloorText()
    {
        if (floorIndexText != null)
        {
            floorIndexText.text = $"SPIRE FLOOR {currentFloor:D2} - SUBTERRANEAN CAVERNS";
        }
    }

    private void UpdateAllGauges()
    {
        if (playerProgression != null)
        {
            if (playerHealthBar != null)
                playerHealthBar.value = playerProgression.currentHp / Mathf.Max(1f, playerProgression.maxHp);
            if (playerManaBar != null)
                playerManaBar.value = playerProgression.currentMp / Mathf.Max(1f, playerProgression.maxMp);
        }

        if (bossController != null && bossHealthBar != null)
        {
            bossHealthBar.value = bossController.currentHealth / Mathf.Max(1f, bossController.maxHealth);
        }

        if (livingGear != null && satietyGauge != null)
        {
            satietyGauge.value = livingGear.currentSatiety / Mathf.Max(1f, livingGear.maxSatiety);
        }
    }

    public void UpdateVaultDisplay()
    {
        if (gemVaultCounterTMP != null && GemologySystem.Instance != null)
        {
            int cores = GemologySystem.Instance.totalManaCores;
            int azurite = GemologySystem.Instance.GetGemCount(MineralRarity.AzuriteCore);
            int ruby = GemologySystem.Instance.GetGemCount(MineralRarity.CinnabarRuby);
            gemVaultCounterTMP.text = $"<color=#FFD700>[CORE]</color> {cores} | <color=#00D8FF>[AZUR]</color> {azurite} | <color=#FF4444>[RUBY]</color> {ruby}";
        }
    }

    private void UpdateCompanionDisplay(BespokeNPCObject companion)
    {
        if (activeCompanionSlotText != null)
        {
            if (companion != null)
            {
                activeCompanionSlotText.text = $"[PARTNER]: <b>{companion.displayName}</b> ({companion.quirk})";
            }
            else
            {
                activeCompanionSlotText.text = "[PARTNER]: None (Solo Raid)";
            }
        }
    }

    private void HandleGemUpdated(MineralRarity gem, int count)
    {
        UpdateVaultDisplay();
    }

    private void HandleCoresChanged(int totalCores)
    {
        UpdateVaultDisplay();
    }

    private void HandlePlayerHealthChanged(float current, float max)
    {
        if (playerHealthBar != null) playerHealthBar.value = current / Mathf.Max(1f, max);
        if (playerHpValueTMP != null) playerHpValueTMP.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    private void HandlePlayerManaChanged(float current, float max)
    {
        if (playerManaBar != null) playerManaBar.value = current / Mathf.Max(1f, max);
        if (playerMpValueTMP != null) playerMpValueTMP.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    private void HandleBossHealthChanged(float current, float max)
    {
        if (bossHealthBar != null) bossHealthBar.value = current / Mathf.Max(1f, max);
    }

    private void HandleTemperChanged(float currentTemper) {}

    private void HandleLevelUp(int newLevel)
    {
        UpdateFloorText();
        if (playerLevelTMP != null) playerLevelTMP.text = $"LV. {newLevel}";
    }
}

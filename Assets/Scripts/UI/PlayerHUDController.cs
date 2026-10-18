using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Combat;

namespace BattleSummoner.UI
{
    /// <summary>
    /// PlayerHUDController manages the unified Anime ARPG Player HUD matching the exact design layout:
    /// - Top Row: Settings (top-left), Notification Broadcast Banner (top-center), Mini Map (top-right).
    /// - Left: Player Profile avatar, Health Bar (top), Mana Bar (bottom), 5 Buff/Debuff circular slots, AI button, Dispel button.
    /// - Center: Play Area framed by cyan boundary.
    /// - Right: Inventory chest button, Ally 1 through Ally 9 cards (single column).
    /// - Bottom Deck: Item Carousel (with left/right arrows), Dash button (center), Jump button (large center),
    ///   Joy Stick (bottom-left), Z-Target & Swap buttons (bottom-right), Camera Control Area (bottom-right touch pan).
    /// </summary>
    public class PlayerHUDController : MonoBehaviour
    {
        public static PlayerHUDController Instance { get; private set; }

        [Header("Top Bar")]
        public Button settingsButton;
        public RectTransform notificationBanner;
        public TextMeshProUGUI notificationText;
        public RectTransform minimapFrame;
        public MinimapRadarController minimapRadar;

        [Header("Left: Profile, Health, Mana & Buffs")]
        public Image playerProfileAvatar;
        public Image playerHealthFill;
        public TextMeshProUGUI playerHealthText;
        public Image playerManaFill;
        public TextMeshProUGUI playerManaText;
        public Image[] buffDebuffSlots = new Image[5];
        public Button aiButton;
        public Button dispelButton;

        [Header("Center: Play Area")]
        public RectTransform playAreaFrame;

        [Header("Right: Inventory & Allies (1 to 9)")]
        public Button inventoryButton;
        public RectTransform partySquadColumn;
        public Button[] allyButtons = new Button[9];

        [Header("Bottom Deck: Carousel & Mobility")]
        public RectTransform itemCarouselRoot;
        public Button carouselLeftBtn;
        public Button carouselRightBtn;
        public Button carouselItemSlot;
        public Image carouselItemIcon;
        public TextMeshProUGUI carouselItemCount;
        public TextMeshProUGUI carouselItemName;
        public Button dashButton;
        public Button jumpButton;

        [Header("Bottom Deck: Virtual Joystick")]
        public FloatingVirtualStick virtualStick;

        [Header("Bottom Deck: Targeting & Camera Control")]
        public Button zTargetButton;
        public Button swapButton;
        public TouchCameraLookZone cameraLookZone;

        [Header("Guide Lines (Diagram Visual Aids)")]
        [Tooltip("If false, hides the colored diagram guide lines (cyan boundary, magenta box, green rings) while preserving all button and widget anchors.")]
        public bool showGuideLines = false;

        [Header("Subsystem References")]
        [SerializeField] private PlayerProgression playerProgression;
        [SerializeField] private PlayerMovementController playerMovement;

        private Coroutine toastCoroutine;
        private const string DEFAULT_BROADCAST = "<b>Notification Broadcast</b>";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            ResolveSubsystems();
        }

        private void Start()
        {
            ResolveSubsystems();
            BindListeners();
            UpdateGauges();
            ApplyGuideLinesVisibility(showGuideLines);
            if (notificationText != null && string.IsNullOrEmpty(notificationText.text))
            {
                notificationText.text = DEFAULT_BROADCAST;
            }
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            ResolveSubsystems();
            if (playerProgression != null)
            {
                playerProgression.OnHealthChanged += HandleHealthChanged;
                playerProgression.OnManaChanged += HandleManaChanged;
            }
        }

        private void OnDisable()
        {
            if (playerProgression != null)
            {
                playerProgression.OnHealthChanged -= HandleHealthChanged;
                playerProgression.OnManaChanged -= HandleManaChanged;
            }
        }

        public void ResolveSubsystems()
        {
            if (playerProgression == null) playerProgression = FindAnyObjectByType<PlayerProgression>();
            if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovementController>();

            if (minimapRadar == null && minimapFrame != null)
            {
                minimapRadar = minimapFrame.GetComponent<MinimapRadarController>() ?? minimapFrame.GetComponentInChildren<MinimapRadarController>();
            }

            if (virtualStick == null)
            {
                virtualStick = GetComponentInChildren<FloatingVirtualStick>();
            }

            if (cameraLookZone == null)
            {
                cameraLookZone = GetComponentInChildren<TouchCameraLookZone>();
            }
        }

        public void BindListeners()
        {
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (aiButton != null)
            {
                aiButton.onClick.RemoveAllListeners();
                aiButton.onClick.AddListener(OnAIClicked);
            }

            if (dispelButton != null)
            {
                dispelButton.onClick.RemoveAllListeners();
                dispelButton.onClick.AddListener(OnDispelClicked);
            }

            if (inventoryButton != null)
            {
                inventoryButton.onClick.RemoveAllListeners();
                inventoryButton.onClick.AddListener(OnInventoryClicked);
            }

            if (dashButton != null)
            {
                dashButton.onClick.RemoveAllListeners();
                dashButton.onClick.AddListener(OnDashClicked);
            }

            if (jumpButton != null)
            {
                jumpButton.onClick.RemoveAllListeners();
                jumpButton.onClick.AddListener(OnJumpClicked);
            }

            if (zTargetButton != null)
            {
                zTargetButton.onClick.RemoveAllListeners();
                zTargetButton.onClick.AddListener(OnZTargetClicked);
            }

            if (swapButton != null)
            {
                swapButton.onClick.RemoveAllListeners();
                swapButton.onClick.AddListener(OnSwapClicked);
            }

            // Wire QuickItemCarouselController if present
            var carousel = GetComponentInChildren<QuickItemCarouselController>() ?? GetComponent<QuickItemCarouselController>();
            if (carousel != null)
            {
                if (carouselLeftBtn != null && carouselRightBtn != null && carouselItemSlot != null && carouselItemIcon != null && carouselItemCount != null && carouselItemName != null)
                {
                    carousel.BindReferences(carouselLeftBtn, carouselRightBtn, carouselItemSlot, carouselItemIcon, carouselItemCount, carouselItemName, itemCarouselRoot);
                }
            }

            // Wire AllyPartyTargetingController
            var allyController = AllyPartyTargetingController.Instance;
            if (allyController != null)
            {
                allyController.InitializeSquadMembers();
            }

            // Wire Minimap Tap to open World Exploration Map modal
            if (minimapFrame != null)
            {
                var btn = minimapFrame.GetComponent<Button>();
                if (btn == null) btn = minimapFrame.gameObject.AddComponent<Button>();
                btn.onClick.RemoveListener(OnMinimapClicked);
                btn.onClick.AddListener(OnMinimapClicked);
            }
        }

        public void OnMinimapClicked()
        {
            if (WorldMapExplorationModal.Instance != null)
            {
                WorldMapExplorationModal.Instance.OpenModal();
            }
            else
            {
                var mapModal = FindAnyObjectByType<WorldMapExplorationModal>();
                if (mapModal == null)
                {
                    var go = new GameObject("WorldMapExplorationModal", typeof(WorldMapExplorationModal));
                    mapModal = go.GetComponent<WorldMapExplorationModal>();
                }
                mapModal.OpenModal();
            }
        }

        public void OnSettingsClicked()
        {
            if (SettingsMenuUI.Instance != null)
            {
                SettingsMenuUI.Instance.ToggleSettings();
            }
            else
            {
                ShowToast("✦ SETTINGS: Audio 100%, Graphics: Anime High-Fidelity ✦");
            }
        }

        public void OnAIClicked()
        {
            var player = GameObject.Find("Player_Hero") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                var autoAI = player.GetComponent<AutoBattlerAIController>();
                if (autoAI != null)
                {
                    autoAI.ToggleAutoBattler();
                    ShowToast(autoAI.IsActive ? "🤖 AUTO-BATTLER AI: <color=#00FF88>ENABLED</color>" : "🤖 AUTO-BATTLER AI: <color=#FF8844>DISABLED</color>");
                    return;
                }
            }
            ShowToast("🤖 AUTO-BATTLER AI: Toggled Tactical Stance");
        }

        public void OnDispelClicked()
        {
            if (SpellCastingFlowController.Instance != null)
            {
                SpellCastingFlowController.Instance.OnDispelButtonPressed();
            }
            else if (RuneShapeCastingEngine.Instance != null)
            {
                RuneShapeCastingEngine.Instance.DispelDrawnRune();
            }
            ShowToast("💨 DISPEL: Magic casting dispelled & neutral stance restored");
        }

        public void OnInventoryClicked()
        {
            if (InventoryMenuUI.Instance != null)
            {
                InventoryMenuUI.Instance.ToggleInventory();
            }
            else
            {
                ShowToast("🎒 INVENTORY: Equipped Gear & Adventurer Pouch");
            }
        }

        public void OnDashClicked()
        {
            if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovementController>();
            if (playerMovement != null)
            {
                playerMovement.Dash();
            }
        }

        public void OnJumpClicked()
        {
            if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovementController>();
            if (playerMovement != null)
            {
                playerMovement.Jump();
            }
        }

        public void OnZTargetClicked()
        {
            if (QuickZTargetingController.Instance != null)
            {
                QuickZTargetingController.Instance.ToggleLockOn();
            }
        }

        public void OnSwapClicked()
        {
            if (QuickZTargetingController.Instance != null)
            {
                QuickZTargetingController.Instance.CycleTarget(1);
            }
        }

        public void ShowToast(string message, float duration = 3.5f)
        {
            if (notificationText == null) return;
            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
        }

        private IEnumerator ToastRoutine(string message, float duration)
        {
            notificationText.text = message;
            yield return new WaitForSeconds(duration);
            notificationText.text = DEFAULT_BROADCAST;
            toastCoroutine = null;
        }

        private void HandleHealthChanged(float current, float max)
        {
            UpdateHealth(current, max);
        }

        private void HandleManaChanged(float current, float max)
        {
            UpdateMana(current, max);
        }

        private void UpdateGauges()
        {
            if (playerProgression != null)
            {
                UpdateHealth(playerProgression.currentHp, playerProgression.maxHp);
                UpdateMana(playerProgression.currentMp, playerProgression.maxMp);
            }
            else
            {
                UpdateHealth(1220f, 1350f);
                UpdateMana(720f, 750f);
            }
        }

        private void UpdateHealth(float current, float max)
        {
            float ratio = Mathf.Clamp01(current / Mathf.Max(1f, max));
            if (playerHealthFill != null) playerHealthFill.fillAmount = ratio;
            if (playerHealthText != null) playerHealthText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
        }

        private void UpdateMana(float current, float max)
        {
            float ratio = Mathf.Clamp01(current / Mathf.Max(1f, max));
            if (playerManaFill != null) playerManaFill.fillAmount = ratio;
            if (playerManaText != null) playerManaText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
        }

        private void OnValidate()
        {
            ApplyGuideLinesVisibility(showGuideLines);
        }

        /// <summary>
        /// Toggles visibility of the colored diagram guide lines (cyan boundary lines, magenta box, green rings)
        /// while preserving all RectTransforms, buttons, anchors, and widgets in their exact screen positions.
        /// </summary>
        public void ApplyGuideLinesVisibility(bool visible)
        {
            // 1. Play area cyan border lines
            if (playAreaFrame != null)
            {
                foreach (Transform child in playAreaFrame)
                {
                    if (child.name.StartsWith("Border"))
                    {
                        child.gameObject.SetActive(visible);
                    }
                }
            }

            // 2. Green rings and Magenta diagram box
            var allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.name == "GreenRing" || t.name == "MagentaBorder")
                {
                    t.gameObject.SetActive(visible);
                }
            }
        }
    }
}

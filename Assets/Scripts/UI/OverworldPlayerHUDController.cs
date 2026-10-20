using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Combat;
using BattleSummoner.World;

namespace BattleSummoner.UI
{
    /// <summary>
    /// OverworldPlayerHUDController manages the comprehensive anime ARPG HUD for the Overworld:
    /// - Top-Left: Persona Battle Side-Profile portrait, Player Level badge, Soul Archetype, HP & MP bars.
    /// - Top-Right: Location title banner, Gem & Gold vault counters, Menu shortcuts (Bag, Campfire Hub, Settings).
    /// - Bottom-Right: Action & Combat deck (Attack [J], Dash [Space], Skills [1,2,3], Potions [Q,E]).
    /// - Bottom-Left: Floating Virtual Movement Stick.
    /// - Center: Interactive Toast & Contextual Interaction Prompts.
    /// </summary>
    public class OverworldPlayerHUDController : MonoBehaviour
    {
        public static OverworldPlayerHUDController Instance { get; private set; }

        [Header("Subsystems")]
        [SerializeField] private PlayerProgression playerProgression;
        [SerializeField] private PlayerMovementController playerMovement;
        [SerializeField] private HumanoidAnimationController humanoidAnimation;
        [SerializeField] private BattleSideProfileHUDController battleSideProfileHUD;

        [Header("Top-Left Header UI")]
        [SerializeField] private TextMeshProUGUI playerNameTMP;
        [SerializeField] private TextMeshProUGUI playerLevelTMP;
        [SerializeField] private TextMeshProUGUI playerClassTMP;
        [SerializeField] private Slider expSlider;

        [Header("Top-Right Vault & Location")]
        [SerializeField] private TextMeshProUGUI locationTitleTMP;
        [SerializeField] private TextMeshProUGUI goldCounterTMP;
        [SerializeField] private TextMeshProUGUI gemCounterTMP;
        [SerializeField] private Button bagButton;
        [SerializeField] private Button campfireMapButton;
        [SerializeField] private Button settingsButton;

        [Header("Bottom-Right Combat Deck")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button dashButton;
        [SerializeField] private Image dashCooldownFill;
        [SerializeField] private Button skill1Button;
        [SerializeField] private Image skill1CooldownFill;
        [SerializeField] private Button skill2Button;
        [SerializeField] private Image skill2CooldownFill;
        [SerializeField] private Button skill3Button;
        [SerializeField] private Image skill3CooldownFill;
        [SerializeField] private Button hpPotionButton;
        [SerializeField] private TextMeshProUGUI hpPotionCountTMP;
        [SerializeField] private Button mpPotionButton;
        [SerializeField] private TextMeshProUGUI mpPotionCountTMP;

        [Header("Center Toast Notification")]
        [SerializeField] private CanvasGroup toastGroup;
        [SerializeField] private TextMeshProUGUI toastTextTMP;

        [Header("Runtime State")]
        private int hpPotionCount = 5;
        private int mpPotionCount = 3;
        private float skill1CooldownTimer = 0f;
        private float skill2CooldownTimer = 0f;
        private float skill3CooldownTimer = 0f;
        private float dashCooldownTimer = 0f;

        private const float Skill1MaxCD = 4.0f;
        private const float Skill2MaxCD = 8.0f;
        private const float Skill3MaxCD = 15.0f;
        private const float DashMaxCD = 0.45f;

        private Coroutine activeToastRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            ResolveReferences();
            BindButtons();
            UpdateTelemetry();
            UpdatePotionCounts();
        }

        private void ResolveReferences()
        {
            var player = GameObject.Find("Player_Hero");
            if (player != null)
            {
                if (playerProgression == null) playerProgression = player.GetComponent<PlayerProgression>();
                if (playerMovement == null) playerMovement = player.GetComponent<PlayerMovementController>();
                if (humanoidAnimation == null) humanoidAnimation = player.GetComponent<HumanoidAnimationController>();
            }

            if (battleSideProfileHUD == null)
            {
                battleSideProfileHUD = BattleSideProfileHUDController.EnsureInScene(transform);
            }

            // Ensure Quick Item Carousel is positioned directly above the movement controls
            QuickItemCarouselController.EnsureInScene(transform);

            // Ensure WizardSpellSlingerController & QuickZTargetingController
            BattleSummoner.Combat.WizardSpellSlingerController.EnsureInScene(player != null ? player.transform : transform);
            BattleSummoner.Combat.QuickZTargetingController.EnsureInScene(transform);

            // Ensure full-screen transparent PortraitGestureCanvas for touch slinging & 3D finger tracking
            var gestureCanvas = GetComponentInChildren<PortraitGestureCanvas>();
            if (gestureCanvas == null)
            {
                var gcObj = new GameObject("PortraitGestureCanvas", typeof(RectTransform), typeof(Image), typeof(PortraitGestureCanvas));
                gcObj.transform.SetParent(transform, false);
                gcObj.transform.SetAsFirstSibling();
                var rt = gcObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var img = gcObj.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0.002f);
                img.raycastTarget = true;
            }
        }

        private void BindButtons()
        {
            if (attackButton != null)
            {
                attackButton.onClick.RemoveAllListeners();
                attackButton.onClick.AddListener(ExecuteAttack);
            }

            if (dashButton != null)
            {
                dashButton.onClick.RemoveAllListeners();
                dashButton.onClick.AddListener(ExecuteDash);
            }

            if (skill1Button != null)
            {
                skill1Button.onClick.RemoveAllListeners();
                skill1Button.onClick.AddListener(ExecuteSkill1);
            }

            if (skill2Button != null)
            {
                skill2Button.onClick.RemoveAllListeners();
                skill2Button.onClick.AddListener(ExecuteSkill2);
            }

            if (skill3Button != null)
            {
                skill3Button.onClick.RemoveAllListeners();
                skill3Button.onClick.AddListener(ExecuteSkill3);
            }

            if (hpPotionButton != null)
            {
                hpPotionButton.onClick.RemoveAllListeners();
                hpPotionButton.onClick.AddListener(UseHpPotion);
            }

            if (mpPotionButton != null)
            {
                mpPotionButton.onClick.RemoveAllListeners();
                mpPotionButton.onClick.AddListener(UseMpPotion);
            }

            if (campfireMapButton != null)
            {
                campfireMapButton.onClick.RemoveAllListeners();
                campfireMapButton.onClick.AddListener(TransitionToCampfireHub);
            }

            if (bagButton != null)
            {
                bagButton.onClick.RemoveAllListeners();
                bagButton.onClick.AddListener(() => ShowToast("✦ INVENTORY: Equipped Starter Adventure Raiment ✦"));
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(() => ShowToast("✦ SETTINGS: Audio 100%, Graphics: Anime High Fidelity ✦"));
            }
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
            UpdateCooldowns();
        }

        private void HandleKeyboardShortcuts()
        {
            // Attack shortcut (J or Left Click when not over UI)
            if (Input.GetKeyDown(KeyCode.J))
            {
                ExecuteAttack();
            }

            // Dash shortcut (Space or Left Shift)
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K))
            {
                ExecuteDash();
            }

            // Skills (1, 2, 3)
            if (Input.GetKeyDown(KeyCode.Alpha1)) ExecuteSkill1();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ExecuteSkill2();
            if (Input.GetKeyDown(KeyCode.Alpha3)) ExecuteSkill3();

            // Potions (Q for HP, R for MP)
            if (Input.GetKeyDown(KeyCode.Q)) UseHpPotion();
            if (Input.GetKeyDown(KeyCode.R)) UseMpPotion();

            // Campfire travel shortcut (M)
            if (Input.GetKeyDown(KeyCode.M)) TransitionToCampfireHub();
        }

        public void ExecuteAttack()
        {
            if (humanoidAnimation != null)
            {
                humanoidAnimation.PlaySlashAnimation();
            }
            PlaySound("sword_whoosh");
        }

        public void ExecuteDash()
        {
            if (dashCooldownTimer > 0f) return;

            dashCooldownTimer = DashMaxCD;
            if (playerMovement != null)
            {
                var stick = FindAnyObjectByType<FloatingVirtualStick>();
                Vector2 dir = stick != null ? stick.MoveInput : Vector2.up;
                if (dir.sqrMagnitude < 0.01f)
                {
                    float h = Input.GetAxisRaw("Horizontal");
                    float v = Input.GetAxisRaw("Vertical");
                    dir = new Vector2(h, v).normalized;
                    if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                }
                playerMovement.TriggerDash(dir);
            }
            PlaySound("whoosh");
        }

        public void ExecuteSkill1()
        {
            if (skill1CooldownTimer > 0f) return;
            skill1CooldownTimer = Skill1MaxCD;

            if (playerProgression != null && playerProgression.currentMp >= 25f)
            {
                playerProgression.currentMp = Mathf.Max(0f, playerProgression.currentMp - 25f);
            }

            if (humanoidAnimation != null)
            {
                humanoidAnimation.PlaySlashAnimation();
            }
            PlaySound("spell_fireball_launch");
            ShowToast("✦ ASTRAL FLAME ARC UNLEASHED ✦");
        }

        public void ExecuteSkill2()
        {
            if (skill2CooldownTimer > 0f) return;
            skill2CooldownTimer = Skill2MaxCD;

            if (playerProgression != null && playerProgression.currentMp >= 40f)
            {
                playerProgression.currentMp = Mathf.Max(0f, playerProgression.currentMp - 40f);
            }

            if (humanoidAnimation != null)
            {
                humanoidAnimation.PlayCastAnimation();
            }
            PlaySound("margo_ward");
            ShowToast("✦ AEGIS CELESTIAL WARD ACTIVATED ✦");
        }

        public void ExecuteSkill3()
        {
            if (skill3CooldownTimer > 0f) return;
            skill3CooldownTimer = Skill3MaxCD;

            if (playerProgression != null && playerProgression.currentMp >= 80f)
            {
                playerProgression.currentMp = Mathf.Max(0f, playerProgression.currentMp - 80f);
            }

            if (humanoidAnimation != null)
            {
                humanoidAnimation.PlayCastAnimation();
            }
            PlaySound("apex_celestial_roar");
            ShowToast("✦ ULTIMATE: SOLAS SPIRE RESONANCE BURST ✦");
        }

        public void UseHpPotion()
        {
            if (hpPotionCount <= 0)
            {
                ShowToast("No Vitality Elixirs remaining!");
                return;
            }

            hpPotionCount--;
            UpdatePotionCounts();

            if (playerProgression != null)
            {
                playerProgression.currentHp = Mathf.Min(playerProgression.maxHp, playerProgression.currentHp + 500f);
            }
            Transform heroTf = playerProgression != null ? playerProgression.transform : transform;
            BattleSummoner.VFX.PotionDrinkingSpellVFX.PlayDrinkEffect(heroTf, BattleSummoner.VFX.PotionBottleType.VitalityHP);
            PlaySound("relic_chime");
            ShowToast($"✦ DRANK VITALITY ELIXIR (+500 HP) — {hpPotionCount} Left ✦");
        }

        public void UseMpPotion()
        {
            if (mpPotionCount <= 0)
            {
                ShowToast("No Astral Draughts remaining!");
                return;
            }

            mpPotionCount--;
            UpdatePotionCounts();

            if (playerProgression != null)
            {
                playerProgression.currentMp = Mathf.Min(playerProgression.maxMp, playerProgression.currentMp + 120f);
            }
            Transform heroTf = playerProgression != null ? playerProgression.transform : transform;
            BattleSummoner.VFX.PotionDrinkingSpellVFX.PlayDrinkEffect(heroTf, BattleSummoner.VFX.PotionBottleType.AstralMP);
            PlaySound("relic_chime");
            ShowToast($"✦ DRANK ASTRAL DRAUGHT (+120 MP) — {mpPotionCount} Left ✦");
        }

        private void TransitionToCampfireHub()
        {
            ShowToast("✦ VENTURING TO CAMPFIRE DIALOGUE HUB... ✦");
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene("MultiCharacterDialogueHub", "Campfire_Arrival", "Solas Campfire Rendezvous");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MultiCharacterDialogueHub");
            }
        }

        private void UpdateCooldowns()
        {
            float dt = Time.deltaTime;

            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= dt;
                if (dashCooldownFill != null) dashCooldownFill.fillAmount = dashCooldownTimer / DashMaxCD;
            }
            else if (dashCooldownFill != null) dashCooldownFill.fillAmount = 0f;

            if (skill1CooldownTimer > 0f)
            {
                skill1CooldownTimer -= dt;
                if (skill1CooldownFill != null) skill1CooldownFill.fillAmount = skill1CooldownTimer / Skill1MaxCD;
            }
            else if (skill1CooldownFill != null) skill1CooldownFill.fillAmount = 0f;

            if (skill2CooldownTimer > 0f)
            {
                skill2CooldownTimer -= dt;
                if (skill2CooldownFill != null) skill2CooldownFill.fillAmount = skill2CooldownTimer / Skill2MaxCD;
            }
            else if (skill2CooldownFill != null) skill2CooldownFill.fillAmount = 0f;

            if (skill3CooldownTimer > 0f)
            {
                skill3CooldownTimer -= dt;
                if (skill3CooldownFill != null) skill3CooldownFill.fillAmount = skill3CooldownTimer / Skill3MaxCD;
            }
            else if (skill3CooldownFill != null) skill3CooldownFill.fillAmount = 0f;
        }

        public void UpdateTelemetry()
        {
            if (playerProgression != null)
            {
                if (playerNameTMP != null) playerNameTMP.text = playerProgression.playerName;
                if (playerLevelTMP != null) playerLevelTMP.text = $"LV. {playerProgression.currentLevel}";
                if (playerClassTMP != null) playerClassTMP.text = "✦ Archon Summoner ✦";

                if (expSlider != null)
                {
                    double reqExp = playerProgression.GetRequiredExp(playerProgression.currentLevel);
                    expSlider.value = reqExp > 0 ? (float)(playerProgression.currentExp / reqExp) : 0f;
                }
            }
            else
            {
                if (playerLevelTMP != null) playerLevelTMP.text = "LV. 42";
                if (playerClassTMP != null) playerClassTMP.text = "✦ Archon Summoner ✦";
            }

            if (locationTitleTMP != null) locationTitleTMP.text = "✦ SOLAS OVERWORLD — SPIRE PLAZA ✦";
            if (goldCounterTMP != null) goldCounterTMP.text = "✦ 14,250";
            if (gemCounterTMP != null) gemCounterTMP.text = "◆ 180";
        }

        private void UpdatePotionCounts()
        {
            if (hpPotionCountTMP != null) hpPotionCountTMP.text = hpPotionCount.ToString();
            if (mpPotionCountTMP != null) mpPotionCountTMP.text = mpPotionCount.ToString();
        }

        public void ShowToast(string message, float duration = 2.4f)
        {
            if (PlayerHUDController.Instance != null)
            {
                PlayerHUDController.Instance.ShowToast(message, duration);
            }
            if (toastTextTMP != null) toastTextTMP.text = message;
            if (toastGroup != null)
            {
                if (activeToastRoutine != null) StopCoroutine(activeToastRoutine);
                activeToastRoutine = StartCoroutine(ToastRoutine(duration));
            }
        }

        private IEnumerator ToastRoutine(float duration)
        {
            toastGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            float fadeIn = 0.2f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                toastGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeIn);
                yield return null;
            }
            toastGroup.alpha = 1f;

            yield return new WaitForSeconds(duration);

            elapsed = 0f;
            float fadeOut = 0.35f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                toastGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOut);
                yield return null;
            }
            toastGroup.alpha = 0f;
            toastGroup.gameObject.SetActive(false);
        }

        private void PlaySound(string soundId)
        {
            var audioMaster = FindAnyObjectByType<BattleSummonerAudioMaster>();
            if (audioMaster != null)
            {
                var clip = audioMaster.GetProceduralClip(soundId);
                var src = audioMaster.GetSFXSource();
                if (clip != null && src != null)
                {
                    src.PlayOneShot(clip);
                }
            }
        }
    }
}

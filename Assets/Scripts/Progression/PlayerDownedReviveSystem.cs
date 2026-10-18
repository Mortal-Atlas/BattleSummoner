using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Progression
{
    /// <summary>
    /// PlayerDownedReviveSystem handles the 3-minute Downed / Revive / Respawn mechanics:
    /// - When player HP reaches 0, player collapses into Downed state.
    /// - Player movement and offensive actions are completely locked.
    /// - 3-minute (180 second) countdown timer begins.
    /// - Player can either:
    ///   1. Wait / call another player or NPC companion for a field revive (50% HP restoration).
    ///   2. Respawn at the beginning Cathedral (100% HP & MP restoration).
    /// - If 3 minutes expire, auto-respawns at Cathedral.
    /// </summary>
    public class PlayerDownedReviveSystem : MonoBehaviour
    {
        public static PlayerDownedReviveSystem Instance { get; private set; }

        [Header("Timing Configuration")]
        [Tooltip("Max duration player can remain downed before forced cathedral respawn (seconds)")]
        public float maxDownedDuration = 180f; // 3 minutes

        [Header("State Status")]
        [SerializeField] private bool isDowned = false;
        [SerializeField] private float downedTimer = 0f;
        [SerializeField] private bool isReviving = false;

        [Header("Cathedral Respawn Target")]
        public string cathedralSceneName = "WorldEntryExperience";
        public Vector3 cathedralSpawnPosition = new Vector3(0f, 1.0f, 0f);

        [Header("UI Overlay References (Auto-Created If Null)")]
        [SerializeField] private GameObject downedCanvasObj;
        [SerializeField] private TextMeshProUGUI timerTextTMP;
        [SerializeField] private Slider timerProgressBar;
        [SerializeField] private Button callReviveButton;
        [SerializeField] private Button respawnCathedralButton;
        [SerializeField] private TextMeshProUGUI statusBannerTMP;

        [Header("Player Components")]
        [SerializeField] private PlayerProgression playerProgression;
        [SerializeField] private CharacterController characterController;

        public bool IsDowned => isDowned;
        public float RemainingTime => Mathf.Max(0f, maxDownedDuration - downedTimer);

        public event Action OnPlayerDownedEvent;
        public event Action OnPlayerRevivedEvent;
        public event Action OnCathedralRespawnEvent;

        private Coroutine activeChannelRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            ResolveReferences();
            EnsureUIConstructed();
            if (downedCanvasObj != null) downedCanvasObj.SetActive(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (playerProgression != null)
            {
                playerProgression.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (playerProgression != null)
            {
                playerProgression.OnHealthChanged -= HandleHealthChanged;
            }
        }

        public void ResolveReferences()
        {
            if (playerProgression == null) playerProgression = GetComponent<PlayerProgression>() ?? FindAnyObjectByType<PlayerProgression>();
            if (characterController == null) characterController = GetComponent<CharacterController>() ?? (playerProgression != null ? playerProgression.GetComponent<CharacterController>() : null);
        }

        private void Update()
        {
            if (!isDowned) return;

            downedTimer += Time.deltaTime;
            float remaining = RemainingTime;

            // Update UI
            if (timerTextTMP != null)
            {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                timerTextTMP.text = $"<color=#FF4444>REVIVAL WINDOW:</color> <b>{minutes:D2}:{seconds:D2}</b>";
            }

            if (timerProgressBar != null)
            {
                timerProgressBar.value = remaining / maxDownedDuration;
            }

            // Keyboard Shortcuts
            if (Input.GetKeyDown(KeyCode.E) && !isReviving)
            {
                RequestAllyRevive();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RespawnAtBeginningCathedral();
            }

            // Expiry Check (3 minutes elapsed)
            if (downedTimer >= maxDownedDuration)
            {
                Debug.Log("[REVIVE SYSTEM] 3 minutes expired. Auto-transporting summoner to Cathedral...");
                RespawnAtBeginningCathedral();
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (current <= 0.01f && !isDowned)
            {
                EnterDownedState();
            }
        }

        /// <summary>
        /// Trigger the Downed state: lock movement, display UI, start 3-minute clock.
        /// </summary>
        public void EnterDownedState()
        {
            ResolveReferences();
            if (isDowned) return;
            isDowned = true;
            downedTimer = 0f;
            isReviving = false;

            Debug.LogWarning("⚠️ [PLAYER DOWNED] Astral core destabilized. 3-minute revival window open.");

            // 1. Lock Movement
            if (characterController != null) characterController.enabled = false;

            // 2. Play Sound
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_shatter", 1.0f);
            }

            // 3. Downed Pose / Animation
            var anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger("Downed");

            // 4. Show Downed UI
            EnsureUIConstructed();
            if (downedCanvasObj != null) downedCanvasObj.SetActive(true);

            if (statusBannerTMP != null)
            {
                statusBannerTMP.text = "<color=#FF2244><b>ASTRAL SHELL COLLAPSED</b></color>\nWait for an NPC or Player to revive you, or return to the Cathedral.";
            }

            OnPlayerDownedEvent?.Invoke();
        }

        /// <summary>
        /// Action 1: Call ally or NPC for field revive (3s channel, restores 50% HP).
        /// </summary>
        public void RequestAllyRevive()
        {
            if (!isDowned || isReviving) return;
            isReviving = true;

            if (activeChannelRoutine != null) StopCoroutine(activeChannelRoutine);
            activeChannelRoutine = StartCoroutine(ReviveChannelRoutine());
        }

        private IEnumerator ReviveChannelRoutine()
        {
            string helperName = "Margo";
            if (NPCExpeditionManager.Instance != null && NPCExpeditionManager.Instance.activeRaidCompanion != null)
            {
                helperName = NPCExpeditionManager.Instance.activeRaidCompanion.displayName;
            }

            if (statusBannerTMP != null)
            {
                statusBannerTMP.text = $"<color=#00E5FF><b>REVIVAL IN PROGRESS</b></color>\n{helperName} is channeling celestial restorative light...";
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.9f);
            }

            float channelDuration = 3.0f;
            if (BindingContractSystem.Instance != null)
            {
                channelDuration = BindingContractSystem.Instance.GetKnockedDownPickupDuration();
            }
            float elapsed = 0f;

            while (elapsed < channelDuration)
            {
                elapsed += Time.deltaTime;
                if (statusBannerTMP != null)
                {
                    float pct = elapsed / channelDuration;
                    statusBannerTMP.text = $"<color=#00E5FF><b>REVIVING ({Mathf.RoundToInt(pct * 100)}%)</b></color>\n{helperName} is restoring your life force...";
                }
                yield return null;
            }

            // Successful Revive!
            isDowned = false;
            isReviving = false;
            if (downedCanvasObj != null) downedCanvasObj.SetActive(false);

            if (playerProgression != null)
            {
                playerProgression.Heal(playerProgression.maxHp * 0.50f);
                playerProgression.RestoreMana(playerProgression.maxMp * 0.50f);
            }

            if (characterController != null) characterController.enabled = true;

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#76FF03><b>[REVIVED]</b></color>\nRestored to 50% HP by {helperName}!");
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("boon_pickup", 1.0f);
            }

            OnPlayerRevivedEvent?.Invoke();
            activeChannelRoutine = null;
        }

        /// <summary>
        /// Instantly finishes the revive process (e.g. for testing, instant charms, or admin commands).
        /// </summary>
        public void CompleteRevive(string helperName = "Allied Summoner")
        {
            ResolveReferences();
            if (activeChannelRoutine != null) StopCoroutine(activeChannelRoutine);
            isDowned = false;
            isReviving = false;
            if (downedCanvasObj != null) downedCanvasObj.SetActive(false);

            if (playerProgression != null)
            {
                playerProgression.Heal(playerProgression.maxHp * 0.50f);
                playerProgression.RestoreMana(playerProgression.maxMp * 0.50f);
            }

            if (characterController != null) characterController.enabled = true;

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#76FF03><b>[REVIVED]</b></color>\nRestored to 50% HP by {helperName}!");
            }

            OnPlayerRevivedEvent?.Invoke();
            activeChannelRoutine = null;
        }

        /// <summary>
        /// Action 2: Respawn at the beginning Cathedral (100% HP & MP restored).
        /// </summary>
        public void RespawnAtBeginningCathedral()
        {
            if (!isDowned) return;
            isDowned = false;
            isReviving = false;

            if (activeChannelRoutine != null)
            {
                StopCoroutine(activeChannelRoutine);
                activeChannelRoutine = null;
            }

            if (downedCanvasObj != null) downedCanvasObj.SetActive(false);

            Debug.Log("⛪ [CATHEDRAL RESPAWN] Teleporting player to Cathedral of the Beginning...");

            // Restore Player to 100%
            if (playerProgression != null)
            {
                playerProgression.Heal(playerProgression.maxHp);
                playerProgression.RestoreMana(playerProgression.maxMp);
            }

            if (characterController != null) characterController.enabled = true;

            // Teleport or load Cathedral scene
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != cathedralSceneName)
            {
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.TransitionToScene(cathedralSceneName, "Cathedral_Sanctuary_Spawn");
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(cathedralSceneName);
                }
            }
            else
            {
                // Already in cathedral / entry scene: teleport to spawn
                var spawnMarker = GameObject.Find("SceneEntrance_Cathedral") ?? GameObject.Find("SpawnPoint");
                Vector3 targetPos = spawnMarker != null ? spawnMarker.transform.position : cathedralSpawnPosition;
                if (characterController != null)
                {
                    characterController.enabled = false;
                    transform.position = targetPos;
                    characterController.enabled = true;
                }
                else
                {
                    transform.position = targetPos;
                }
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#FFD700><b>[CATHEDRAL RESPAWN]</b></color>\nSanctuary blessings restored your vital energies!");
            }

            OnCathedralRespawnEvent?.Invoke();
        }

        #region UI Construction
        private void EnsureUIConstructed()
        {
            if (downedCanvasObj != null) return;

            var existing = GameObject.Find("DownedReviveCanvas");
            if (existing != null)
            {
                downedCanvasObj = existing;
                BindUIReferences(downedCanvasObj);
                return;
            }

            // Create Canvas
            var canvasGo = new GameObject("DownedReviveCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvasGo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 999;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            // Dark Vignette Backdrop
            var bgGo = new GameObject("DownedVignette", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.02f, 0.04f, 0.85f);

            // Modal Panel
            var panelGo = new GameObject("ModalPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var pRt = panelGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(720f, 480f);
            pRt.anchoredPosition = Vector2.zero;
            var pImg = panelGo.GetComponent<Image>();
            pImg.color = new Color(0.12f, 0.08f, 0.16f, 0.95f);

            // Gold Border
            var borderGo = new GameObject("BorderFrame", typeof(RectTransform), typeof(Outline));
            borderGo.transform.SetParent(panelGo.transform, false);
            var bRt = borderGo.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.sizeDelta = Vector2.zero;
            var outline = borderGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.80f, 0.25f, 1f);
            outline.effectDistance = new Vector2(4f, 4f);

            // Title
            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panelGo.transform, false);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 180f);
            tRt.sizeDelta = new Vector2(650f, 60f);
            var titleTMP = titleGo.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "<color=#FF2244><b>CRITICAL STATE: DOWNED</b></color>";
            titleTMP.fontSize = 32;
            titleTMP.alignment = TextAlignmentOptions.Center;

            // Status Banner
            var bannerGo = new GameObject("StatusBanner", typeof(RectTransform), typeof(TextMeshProUGUI));
            bannerGo.transform.SetParent(panelGo.transform, false);
            var bnRt = bannerGo.GetComponent<RectTransform>();
            bnRt.anchoredPosition = new Vector2(0f, 105f);
            bnRt.sizeDelta = new Vector2(650f, 70f);
            statusBannerTMP = bannerGo.GetComponent<TextMeshProUGUI>();
            statusBannerTMP.text = "Astral core destabilized. Waiting for allied revive or cathedral recall.";
            statusBannerTMP.fontSize = 20;
            statusBannerTMP.alignment = TextAlignmentOptions.Center;

            // Timer Text
            var timerGo = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            timerGo.transform.SetParent(panelGo.transform, false);
            var tmRt = timerGo.GetComponent<RectTransform>();
            tmRt.anchoredPosition = new Vector2(0f, 35f);
            tmRt.sizeDelta = new Vector2(650f, 50f);
            timerTextTMP = timerGo.GetComponent<TextMeshProUGUI>();
            timerTextTMP.text = "REVIVAL WINDOW: 03:00";
            timerTextTMP.fontSize = 34;
            timerTextTMP.alignment = TextAlignmentOptions.Center;

            // Progress Slider
            var sliderGo = new GameObject("TimerProgressBar", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(panelGo.transform, false);
            var sRt = sliderGo.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, -20f);
            sRt.sizeDelta = new Vector2(560f, 22f);
            timerProgressBar = sliderGo.GetComponent<Slider>();
            timerProgressBar.minValue = 0f;
            timerProgressBar.maxValue = 1f;
            timerProgressBar.value = 1f;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(1f, 0.25f, 0.35f, 1f);
            timerProgressBar.fillRect = fRt;

            // Button 1: Wait / Ally Revive
            callReviveButton = CreateModalButton(panelGo.transform, "CallReviveButton", new Vector2(-155f, -140f), new Vector2(280f, 65f),
                "<color=#00E5FF><b>[E] CALL ALLY</b></color>\n<size=75%>Wait for Field Revive</size>", RequestAllyRevive);

            // Button 2: Cathedral Respawn
            respawnCathedralButton = CreateModalButton(panelGo.transform, "RespawnCathedralButton", new Vector2(155f, -140f), new Vector2(280f, 65f),
                "<color=#FFD700><b>[R] CATHEDRAL</b></color>\n<size=75%>Instant Sanctuary Recall</size>", RespawnAtBeginningCathedral);

            downedCanvasObj = canvasGo;
        }

        private Button CreateModalButton(Transform parent, string name, Vector2 pos, Vector2 size, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.18f, 0.15f, 0.25f, 1f);

            var outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.75f, 0.35f, 0.8f);
            outline.effectDistance = new Vector2(2f, 2f);

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(onClick);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 17;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private void BindUIReferences(GameObject root)
        {
            timerTextTMP = root.transform.Find("ModalPanel/TimerText")?.GetComponent<TextMeshProUGUI>();
            statusBannerTMP = root.transform.Find("ModalPanel/StatusBanner")?.GetComponent<TextMeshProUGUI>();
            timerProgressBar = root.transform.Find("ModalPanel/TimerProgressBar")?.GetComponent<Slider>();
            callReviveButton = root.transform.Find("ModalPanel/CallReviveButton")?.GetComponent<Button>();
            respawnCathedralButton = root.transform.Find("ModalPanel/RespawnCathedralButton")?.GetComponent<Button>();

            if (callReviveButton != null)
            {
                callReviveButton.onClick.RemoveAllListeners();
                callReviveButton.onClick.AddListener(RequestAllyRevive);
            }
            if (respawnCathedralButton != null)
            {
                respawnCathedralButton.onClick.RemoveAllListeners();
                respawnCathedralButton.onClick.AddListener(RespawnAtBeginningCathedral);
            }
        }
        #endregion
    }
}

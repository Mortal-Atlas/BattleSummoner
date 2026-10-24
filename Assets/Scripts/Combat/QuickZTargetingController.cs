using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Combat
{
    public enum TargetFilterMode
    {
        All,
        BossOnly,
        MobsOnly
    }

    /// <summary>
    /// QuickZTargetingController manages tactical lock-on targeting positioned on the right
    /// of the quick item carousel.
    /// Features:
    /// - Quick Z-Targeting button to Focus / Unfocus (lock-on toggle).
    /// - Toggle for Main Boss (prioritize / lock specifically onto the boss).
    /// - Toggle for Other Enemies (prioritize regular mobs / minions).
    /// - Arrow buttons (◀ and ▶) to cycle targets smoothly.
    /// - Synchronizes PortraitARPGCamera yaw angle and active target tracking.
    /// - 3D In-World animated target reticle hovering above the locked enemy.
    /// - Keyboard shortcuts: Z / Tab (lock-on), B (boss), M (mobs), [ and ] (swap targets).
    /// </summary>
    public class QuickZTargetingController : MonoBehaviour
    {
        public static QuickZTargetingController Instance { get; private set; }

        [Header("Targeting Parameters")]
        [SerializeField] private float maxTargetRange = 38f;
        [SerializeField] private TargetFilterMode currentFilter = TargetFilterMode.All;
        [SerializeField] private Transform currentTarget;
        [SerializeField] private bool isLockedOn = false;

        public bool IsLockedOn => isLockedOn && currentTarget != null;
        public Transform CurrentTarget => currentTarget;
        public TargetFilterMode CurrentFilter => currentFilter;

        [Header("UI Buttons")]
        [SerializeField] private Button zTargetButton;
        [SerializeField] private Image zTargetButtonBg;
        [SerializeField] private TextMeshProUGUI zTargetButtonText;
        [SerializeField] private Button bossToggleButton;
        [SerializeField] private Image bossToggleBg;
        [SerializeField] private TextMeshProUGUI bossToggleText;
        [SerializeField] private Button mobsToggleButton;
        [SerializeField] private Image mobsToggleBg;
        [SerializeField] private TextMeshProUGUI mobsToggleText;
        [SerializeField] private Button prevArrowButton;
        [SerializeField] private Button nextArrowButton;
        [SerializeField] private TextMeshProUGUI targetInfoText;

        [Header("3D In-World Reticle")]
        [SerializeField] private GameObject reticleInstance;
        private Transform playerTransform;

        public event Action<Transform> OnTargetChanged;
        public event Action<bool> OnLockOnToggled;
        public event Action<TargetFilterMode> OnFilterChanged;

        public static QuickZTargetingController EnsureInScene(Transform targetParent = null)
        {
            if (Instance != null) return Instance;

            var existing = UnityEngine.Object.FindAnyObjectByType<QuickZTargetingController>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var go = new GameObject("QuickZTargetingController");
            if (targetParent != null) go.transform.SetParent(targetParent);
            Instance = go.AddComponent<QuickZTargetingController>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            EnsureInWorldReticle();
        }

        private void Start()
        {
            FindPlayerReference();
            if (zTargetButton == null)
            {
                var masterZ = GameObject.Find("MasterZTargetWidget");
                if (masterZ != null)
                {
                    zTargetButton = masterZ.GetComponent<Button>();
                    if (zTargetButton == null)
                    {
                        zTargetButton = masterZ.GetComponentInChildren<Button>();
                    }
                }
                else
                {
                    EnsureDefaultUI();
                }
            }
            BindUIListeners();
            UpdateUIState();
        }

        private void EnsureDefaultUI()
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var panelGo = new GameObject("QuickZTargetingPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform, false);

            var panelRT = panelGo.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(1f, 0.42f);
            panelRT.anchorMax = new Vector2(1f, 0.42f);
            panelRT.pivot = new Vector2(1f, 0.5f);
            panelRT.anchoredPosition = new Vector2(-15f, 0f);
            panelRT.sizeDelta = new Vector2(140f, 96f);

            var bgImg = panelGo.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.16f, 0.85f);

            // 1. Target Info Text (Top)
            var infoGo = new GameObject("TargetInfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
            infoGo.transform.SetParent(panelGo.transform, false);
            var iRT = infoGo.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0f, 0.65f);
            iRT.anchorMax = new Vector2(1f, 1f);
            iRT.offsetMin = new Vector2(4f, 0f);
            iRT.offsetMax = new Vector2(-4f, 0f);

            targetInfoText = infoGo.GetComponent<TextMeshProUGUI>();
            targetInfoText.fontSize = 11;
            targetInfoText.alignment = TextAlignmentOptions.Center;
            targetInfoText.color = new Color(0.8f, 0.85f, 0.95f);
            targetInfoText.text = "Target: [FREE]";

            // 2. Main Z-Target Button (Middle)
            var zBtnGo = new GameObject("ZTargetButton", typeof(RectTransform), typeof(Image), typeof(Button));
            zBtnGo.transform.SetParent(panelGo.transform, false);
            var zRT = zBtnGo.GetComponent<RectTransform>();
            zRT.anchorMin = new Vector2(0.05f, 0.28f);
            zRT.anchorMax = new Vector2(0.95f, 0.65f);
            zRT.offsetMin = Vector2.zero;
            zRT.offsetMax = Vector2.zero;

            zTargetButton = zBtnGo.GetComponent<Button>();
            zTargetButtonBg = zBtnGo.GetComponent<Image>();
            zTargetButtonBg.color = new Color(0.14f, 0.16f, 0.22f, 0.90f);

            var zTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            zTxtGo.transform.SetParent(zBtnGo.transform, false);
            var ztRT = zTxtGo.GetComponent<RectTransform>();
            ztRT.anchorMin = Vector2.zero;
            ztRT.anchorMax = Vector2.one;

            zTargetButtonText = zTxtGo.GetComponent<TextMeshProUGUI>();
            zTargetButtonText.text = "<b>[Z] LOCK-ON</b>";
            zTargetButtonText.fontSize = 12;
            zTargetButtonText.alignment = TextAlignmentOptions.Center;
            zTargetButtonText.color = Color.white;

            // 3. Bottom Row: Prev and Next cycle buttons
            var prevGo = new GameObject("PrevBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            prevGo.transform.SetParent(panelGo.transform, false);
            var pRT = prevGo.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.05f, 0.04f);
            pRT.anchorMax = new Vector2(0.48f, 0.26f);
            pRT.offsetMin = Vector2.zero;
            pRT.offsetMax = Vector2.zero;

            prevArrowButton = prevGo.GetComponent<Button>();
            var pImg = prevGo.GetComponent<Image>();
            pImg.color = new Color(0.2f, 0.24f, 0.32f, 0.9f);

            var pTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            pTxtGo.transform.SetParent(prevGo.transform, false);
            var ptRT = pTxtGo.GetComponent<RectTransform>();
            ptRT.anchorMin = Vector2.zero;
            ptRT.anchorMax = Vector2.one;
            var pt = pTxtGo.GetComponent<TextMeshProUGUI>();
            pt.text = "◀";
            pt.fontSize = 12;
            pt.alignment = TextAlignmentOptions.Center;
            pt.color = Color.white;

            var nextGo = new GameObject("NextBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            nextGo.transform.SetParent(panelGo.transform, false);
            var nRT = nextGo.GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0.52f, 0.04f);
            nRT.anchorMax = new Vector2(0.95f, 0.26f);
            nRT.offsetMin = Vector2.zero;
            nRT.offsetMax = Vector2.zero;

            nextArrowButton = nextGo.GetComponent<Button>();
            var nImg = nextGo.GetComponent<Image>();
            nImg.color = new Color(0.2f, 0.24f, 0.32f, 0.9f);

            var nTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            nTxtGo.transform.SetParent(nextGo.transform, false);
            var ntRT = nTxtGo.GetComponent<RectTransform>();
            ntRT.anchorMin = Vector2.zero;
            ntRT.anchorMax = Vector2.one;
            var nt = nTxtGo.GetComponent<TextMeshProUGUI>();
            nt.text = "▶";
            nt.fontSize = 12;
            nt.alignment = TextAlignmentOptions.Center;
            nt.color = Color.white;
        }

        public void BindReferences(
            Button zBtn, Image zBg, TextMeshProUGUI zTxt,
            Button bossBtn, Image bossBg, TextMeshProUGUI bossTxt,
            Button mobsBtn, Image mobsBg, TextMeshProUGUI mobsTxt,
            Button prevBtn, Button nextBtn, TextMeshProUGUI infoTxt = null)
        {
            zTargetButton = zBtn;
            zTargetButtonBg = zBg;
            zTargetButtonText = zTxt;

            bossToggleButton = bossBtn;
            bossToggleBg = bossBg;
            bossToggleText = bossTxt;

            mobsToggleButton = mobsBtn;
            mobsToggleBg = mobsBg;
            mobsToggleText = mobsTxt;

            prevArrowButton = prevBtn;
            nextArrowButton = nextBtn;
            targetInfoText = infoTxt;

            BindUIListeners();
            UpdateUIState();
        }

        private void BindUIListeners()
        {
            if (zTargetButton != null)
            {
                zTargetButton.onClick.RemoveAllListeners();
                zTargetButton.onClick.AddListener(ToggleLockOn);
            }

            if (bossToggleButton != null)
            {
                bossToggleButton.onClick.RemoveAllListeners();
                bossToggleButton.onClick.AddListener(ToggleBossFilter);
            }

            if (mobsToggleButton != null)
            {
                mobsToggleButton.onClick.RemoveAllListeners();
                mobsToggleButton.onClick.AddListener(ToggleMobsFilter);
            }

            if (prevArrowButton != null)
            {
                prevArrowButton.onClick.RemoveAllListeners();
                prevArrowButton.onClick.AddListener(() => CycleTarget(-1));
            }

            if (nextArrowButton != null)
            {
                nextArrowButton.onClick.RemoveAllListeners();
                nextArrowButton.onClick.AddListener(() => CycleTarget(1));
            }
        }

        private void FindPlayerReference()
        {
            if (playerTransform != null) return;
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerMovementController>();
                if (pc != null) playerTransform = pc.transform;
            }
        }

        private void Update()
        {
            FindPlayerReference();

            // Keyboard Shortcuts
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleLockOn();
            }
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.X))
            {
                ToggleBossFilter();
            }
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.C))
            {
                ToggleMobsFilter();
            }
            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Comma))
            {
                CycleTarget(-1);
            }
            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Period))
            {
                CycleTarget(1);
            }

            // Target Validity Check
            if (isLockedOn)
            {
                if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
                {
                    // Lost target, attempt to acquire next valid
                    AcquireBestTarget(true);
                }
                else if (playerTransform != null)
                {
                    float dist = Vector3.Distance(playerTransform.position, currentTarget.position);
                    if (dist > maxTargetRange * 1.35f)
                    {
                        Unfocus();
                    }
                }
            }

            // Update in-world reticle position & animation
            UpdateReticle();

            // Synchronize ARPG camera orientation with target
            UpdateCameraTracking();
        }

        public void ToggleLockOn()
        {
            if (isLockedOn)
            {
                Unfocus();
            }
            else
            {
                Focus();
            }
        }

        public void LockTarget(Transform target)
        {
            if (target == null) return;
            currentTarget = target;
            isLockedOn = true;
            if (PortraitARPGCamera.Instance != null)
            {
                PortraitARPGCamera.Instance.activeBossTarget = target;
            }
            PlaySFX("relic_chime");
            ShowToast($"<color=#00E5FF><b>[Z-LOCK]</b></color> Focused on {GetTargetDisplayName(target)}");
            OnLockOnToggled?.Invoke(true);
            OnTargetChanged?.Invoke(target);
            UpdateUIState();
        }

        public void Focus()
        {
            AcquireBestTarget(false);
            if (currentTarget != null)
            {
                isLockedOn = true;
                PlaySFX("relic_chime");
                ShowToast($"<color=#00E5FF><b>[Z-LOCK]</b></color> Focused on {GetTargetDisplayName(currentTarget)}");
            }
            else
            {
                ShowToast("<color=#A0A5B5>No target in range</color>");
            }

            OnLockOnToggled?.Invoke(isLockedOn);
            UpdateUIState();
        }

        public void Unfocus()
        {
            isLockedOn = false;
            currentTarget = null;

            if (reticleInstance != null) reticleInstance.SetActive(false);

            if (PortraitARPGCamera.Instance != null)
            {
                PortraitARPGCamera.Instance.activeBossTarget = null;
            }

            PlaySFX("relic_chime");
            ShowToast("<color=#8890A0><b>[Z-LOCK]</b></color> Target Cleared");

            OnLockOnToggled?.Invoke(false);
            OnTargetChanged?.Invoke(null);
            UpdateUIState();
        }

        public void ToggleBossFilter()
        {
            if (currentFilter == TargetFilterMode.BossOnly)
            {
                currentFilter = TargetFilterMode.All;
                ShowToast("<color=#FFD700><b>[TARGET MODE]</b></color> All Enemies");
            }
            else
            {
                currentFilter = TargetFilterMode.BossOnly;
                ShowToast("<color=#FF3030><b>[TARGET MODE]</b></color> Main Boss Priority");
                if (!isLockedOn) Focus();
                else AcquireBestTarget(false);
            }

            OnFilterChanged?.Invoke(currentFilter);
            UpdateUIState();
        }

        public void ToggleMobsFilter()
        {
            if (currentFilter == TargetFilterMode.MobsOnly)
            {
                currentFilter = TargetFilterMode.All;
                ShowToast("<color=#FFD700><b>[TARGET MODE]</b></color> All Enemies");
            }
            else
            {
                currentFilter = TargetFilterMode.MobsOnly;
                ShowToast("<color=#FFA500><b>[TARGET MODE]</b></color> Minions & Mobs Priority");
                if (!isLockedOn) Focus();
                else AcquireBestTarget(false);
            }

            OnFilterChanged?.Invoke(currentFilter);
            UpdateUIState();
        }

        public void CycleTarget(int direction)
        {
            List<Transform> candidates = GetCandidateTargets();
            if (candidates.Count == 0)
            {
                ShowToast("<color=#A0A5B5>No enemies nearby</color>");
                return;
            }

            if (!isLockedOn || currentTarget == null)
            {
                currentTarget = candidates[0];
                isLockedOn = true;
            }
            else
            {
                int currentIndex = candidates.IndexOf(currentTarget);
                if (currentIndex < 0) currentIndex = 0;

                int nextIndex = (currentIndex + direction + candidates.Count) % candidates.Count;
                currentTarget = candidates[nextIndex];
            }

            PlaySFX("relic_chime");
            ShowToast($"<color=#00E5FF><b>[SWAP TARGET]</b></color> {GetTargetDisplayName(currentTarget)}");

            OnTargetChanged?.Invoke(currentTarget);
            UpdateUIState();
        }

        private void AcquireBestTarget(bool silent)
        {
            List<Transform> candidates = GetCandidateTargets();
            if (candidates.Count > 0)
            {
                currentTarget = candidates[0];
                isLockedOn = true;
                OnTargetChanged?.Invoke(currentTarget);
            }
            else
            {
                currentTarget = null;
                isLockedOn = false;
            }
            UpdateUIState();
        }

        private List<Transform> GetCandidateTargets()
        {
            List<Transform> result = new List<Transform>();
            Vector3 origin = playerTransform != null ? playerTransform.position : Vector3.zero;

            // 1. Collect Bosses
            var bosses = UnityEngine.Object.FindObjectsByType<BossController>(FindObjectsSortMode.None);
            for (int i = 0; i < bosses.Length; i++)
            {
                if (bosses[i] != null && bosses[i].gameObject.activeInHierarchy && bosses[i].currentHealth > 0)
                {
                    float d = Vector3.Distance(origin, bosses[i].transform.position);
                    if (d <= maxTargetRange && currentFilter != TargetFilterMode.MobsOnly)
                    {
                        result.Add(bosses[i].transform);
                    }
                }
            }

            // 2. Collect Spire Enemies / Mobs
            var enemies = UnityEngine.Object.FindObjectsByType<SpireEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].gameObject.activeInHierarchy)
                {
                    float d = Vector3.Distance(origin, enemies[i].transform.position);
                    if (d <= maxTargetRange && currentFilter != TargetFilterMode.BossOnly)
                    {
                        result.Add(enemies[i].transform);
                    }
                }
            }

            // Fallback for tags if component search returned empty
            if (result.Count == 0)
            {
                GameObject[] bossObjs = GameObject.FindGameObjectsWithTag("Boss");
                foreach (var b in bossObjs)
                {
                    if (b.activeInHierarchy && currentFilter != TargetFilterMode.MobsOnly) result.Add(b.transform);
                }

                GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (var e in enemyObjs)
                {
                    if (e.activeInHierarchy && currentFilter != TargetFilterMode.BossOnly) result.Add(e.transform);
                }
            }

            // Sort candidates: Bosses first if BossOnly or All with boss nearby, then by proximity to player
            result.Sort((a, b) =>
            {
                bool aIsBoss = IsBoss(a);
                bool bIsBoss = IsBoss(b);
                if (currentFilter == TargetFilterMode.BossOnly || currentFilter == TargetFilterMode.All)
                {
                    if (aIsBoss && !bIsBoss) return -1;
                    if (!aIsBoss && bIsBoss) return 1;
                }
                float distA = Vector3.Distance(origin, a.position);
                float distB = Vector3.Distance(origin, b.position);
                return distA.CompareTo(distB);
            });

            return result;
        }

        private bool IsBoss(Transform t)
        {
            if (t == null) return false;
            if (t.GetComponent<BossController>() != null) return true;
            if (t.CompareTag("Boss")) return true;
            if (t.name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (t.name.IndexOf("Aethelgard", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private string GetTargetDisplayName(Transform t)
        {
            if (t == null) return "None";
            var boss = t.GetComponent<BossController>();
            if (boss != null && !string.IsNullOrEmpty(boss.bossName)) return boss.bossName;
            var enemy = t.GetComponent<SpireEnemy>();
            if (enemy != null) return enemy.name;
            return t.name.Replace("(Clone)", "").Trim();
        }

        private void UpdateUIState()
        {
            // Z-Target Button State
            if (zTargetButtonBg != null)
            {
                if (isLockedOn)
                {
                    zTargetButtonBg.color = new Color(0.05f, 0.65f, 0.85f, 0.95f); // Cyan locked
                }
                else
                {
                    zTargetButtonBg.color = new Color(0.14f, 0.16f, 0.22f, 0.90f); // Idle dark slate
                }
            }
            if (zTargetButtonText != null)
            {
                zTargetButtonText.text = isLockedOn ? "<b>LOCKED</b>" : "<b>Z-TARGET</b>";
                zTargetButtonText.color = isLockedOn ? Color.white : new Color(0.85f, 0.90f, 0.95f);
            }

            // Boss Toggle Button State
            if (bossToggleBg != null)
            {
                bool active = currentFilter == TargetFilterMode.BossOnly;
                bossToggleBg.color = active ? new Color(0.85f, 0.22f, 0.22f, 0.95f) : new Color(0.16f, 0.18f, 0.24f, 0.85f);
            }
            if (bossToggleText != null)
            {
                bool active = currentFilter == TargetFilterMode.BossOnly;
                bossToggleText.text = active ? "<b>BOSS ONLY</b>" : "<b>BOSS</b>";
                bossToggleText.color = active ? new Color(1f, 0.9f, 0.4f) : new Color(0.75f, 0.78f, 0.85f);
            }

            // Mobs Toggle Button State
            if (mobsToggleBg != null)
            {
                bool active = currentFilter == TargetFilterMode.MobsOnly;
                mobsToggleBg.color = active ? new Color(0.88f, 0.52f, 0.15f, 0.95f) : new Color(0.16f, 0.18f, 0.24f, 0.85f);
            }
            if (mobsToggleText != null)
            {
                bool active = currentFilter == TargetFilterMode.MobsOnly;
                mobsToggleText.text = active ? "<b>MOBS ONLY</b>" : "<b>MOBS</b>";
                mobsToggleText.color = active ? Color.white : new Color(0.75f, 0.78f, 0.85f);
            }

            // Target Info Text
            if (targetInfoText != null)
            {
                if (isLockedOn && currentTarget != null)
                {
                    float dist = playerTransform != null ? Vector3.Distance(playerTransform.position, currentTarget.position) : 0f;
                    string targetName = GetTargetDisplayName(currentTarget);
                    string typeTag = IsBoss(currentTarget) ? "<color=#FF4040>[BOSS]</color>" : "<color=#FFA500>[MOB]</color>";
                    targetInfoText.text = $"{typeTag} <b>{targetName}</b> ({dist:F0}m)";
                }
                else
                {
                    targetInfoText.text = "<color=#707888>Target: [FREE ROAM]</color>";
                }
            }
        }

        private void UpdateReticle()
        {
            if (reticleInstance == null) EnsureInWorldReticle();
            if (reticleInstance == null) return;

            if (isLockedOn && currentTarget != null)
            {
                reticleInstance.SetActive(true);
                Vector3 targetPos = currentTarget.position + Vector3.up * 2.3f;
                reticleInstance.transform.position = targetPos;

                // Billboard facing camera
                if (Camera.main != null)
                {
                    reticleInstance.transform.rotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, 0f);
                }

                // Gentle pulsing animation
                float pulse = 1.0f + 0.12f * Mathf.Sin(Time.time * 8f);
                reticleInstance.transform.localScale = Vector3.one * (0.012f * pulse);
            }
            else
            {
                reticleInstance.SetActive(false);
            }
        }

        private void UpdateCameraTracking()
        {
            if (!isLockedOn || currentTarget == null || playerTransform == null) return;

            if (PortraitARPGCamera.Instance != null)
            {
                PortraitARPGCamera.Instance.activeBossTarget = currentTarget;

                // Smoothly bias camera yaw toward target
                Vector3 toTarget = currentTarget.position - playerTransform.position;
                if (toTarget.sqrMagnitude > 0.05f)
                {
                    float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                    PortraitARPGCamera.Instance.yawAngle = Mathf.LerpAngle(
                        PortraitARPGCamera.Instance.yawAngle,
                        targetYaw,
                        Time.deltaTime * 5.5f
                    );
                }
            }
        }

        private void EnsureInWorldReticle()
        {
            if (reticleInstance != null) return;

            reticleInstance = new GameObject("LockOnReticle_3D", typeof(RectTransform), typeof(Canvas), typeof(TextMeshProUGUI));
            Canvas c = reticleInstance.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = reticleInstance.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 100f);
            rt.localScale = Vector3.one * 0.012f;

            TextMeshProUGUI tmp = reticleInstance.GetComponent<TextMeshProUGUI>();
            tmp.text = "<color=#00E5FF><b>V</b></color>\n<size=75%><color=#FFD700><b>[ LOCKED ]</b></color></size>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 26;

            reticleInstance.SetActive(false);
        }

        private void PlaySFX(string name)
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX(name, 0.85f);
            }
        }

        private void ShowToast(string message)
        {
            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(message);
            }
        }
    }
}

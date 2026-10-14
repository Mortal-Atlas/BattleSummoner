using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.DungeonDimension
{
    /// <summary>
    /// DungeonDimensionHUD renders the Risk of Rain styled timed difficulty interface:
    /// - Continuous time-elapsed clock (MM:SS) and difficulty coefficient indicator.
    /// - Colorful Risk of Rain difficulty tier badge with filling threat gauge.
    /// - Area name, room index, and remaining enemy count.
    /// - Horizontal strip displaying active temporary run boons.
    /// - Cinematic tier escalation alert flash banner.
    /// </summary>
    public class DungeonDimensionHUD : MonoBehaviour
    {
        private static DungeonDimensionHUD _instance;
        public static DungeonDimensionHUD Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DungeonDimensionHUD>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("HUD Root")]
        [SerializeField] private GameObject hudRoot;

        [Header("Risk of Rain Difficulty Clock UI")]
        [SerializeField] private TextMeshProUGUI timerTMP;
        [SerializeField] private TextMeshProUGUI tierBadgeTMP;
        [SerializeField] private Slider tierProgressBar;
        [SerializeField] private Image tierProgressFill;
        [SerializeField] private TextMeshProUGUI coeffTMP;

        [Header("Tier Escalation Alert Banner")]
        [SerializeField] private GameObject tierAlertBannerObj;
        [SerializeField] private TextMeshProUGUI tierAlertTMP;

        [Header("Area & Objective Status")]
        [SerializeField] private TextMeshProUGUI areaTitleTMP;
        [SerializeField] private TextMeshProUGUI enemyCountTMP;

        [Header("Active Run Boons Strip")]
        [SerializeField] private Transform activeBoonsContainer;
        [SerializeField] private GameObject boonBadgePrefab;

        private readonly List<GameObject> activeBadgeObjects = new List<GameObject>();
        private Coroutine alertRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (tierAlertBannerObj != null) tierAlertBannerObj.SetActive(false);
        }

        private void OnEnable()
        {
            var clock = RiskOfRainDifficultyClock.Instance;
            if (clock != null)
            {
                clock.OnDifficultyTierChanged += HandleTierChanged;
                clock.OnDifficultyTick += HandleClockTick;
            }

            var manager = DungeonDimensionManager.Instance;
            if (manager != null)
            {
                manager.OnAreaStarted += HandleAreaStarted;
                manager.OnEnemyCountChanged += HandleEnemyCountChanged;
            }

            var buffSys = DungeonRunBuffSystem.Instance;
            if (buffSys != null)
            {
                buffSys.OnBoonAcquired += HandleBoonAcquired;
                buffSys.OnBuffsReset += HandleBuffsReset;
            }
        }

        private void OnDisable()
        {
            var clock = RiskOfRainDifficultyClock.Instance;
            if (clock != null)
            {
                clock.OnDifficultyTierChanged -= HandleTierChanged;
                clock.OnDifficultyTick -= HandleClockTick;
            }

            var manager = DungeonDimensionManager.Instance;
            if (manager != null)
            {
                manager.OnAreaStarted -= HandleAreaStarted;
                manager.OnEnemyCountChanged -= HandleEnemyCountChanged;
            }

            var buffSys = DungeonRunBuffSystem.Instance;
            if (buffSys != null)
            {
                buffSys.OnBoonAcquired -= HandleBoonAcquired;
                buffSys.OnBuffsReset -= HandleBuffsReset;
            }
        }

        public void SetVisible(bool visible)
        {
            if (hudRoot != null) hudRoot.SetActive(visible);
        }

        private void HandleClockTick(float runTime)
        {
            var clock = RiskOfRainDifficultyClock.Instance;
            if (clock == null) return;

            if (timerTMP != null)
            {
                timerTMP.text = clock.GetFormattedTime();
            }

            if (tierProgressBar != null)
            {
                tierProgressBar.value = clock.GetProgressToNextTier();
            }

            if (coeffTMP != null)
            {
                coeffTMP.text = $"<size=80%>SCALE</size> <b>{clock.GetDifficultyCoefficient():F2}x</b>";
            }
        }

        private void HandleTierChanged(DifficultyTier newTier, float coeff)
        {
            string tierName = RiskOfRainDifficultyClock.GetTierName(newTier);
            Color tierColor = RiskOfRainDifficultyClock.GetTierColor(newTier);
            string hex = ColorUtility.ToHtmlStringRGB(tierColor);

            if (tierBadgeTMP != null)
            {
                tierBadgeTMP.text = $"<color=#{hex}><b>{tierName}</b></color>";
            }

            if (tierProgressFill != null)
            {
                tierProgressFill.color = tierColor;
            }

            // Trigger pulsing alert banner
            if (tierAlertBannerObj != null && tierAlertTMP != null)
            {
                if (alertRoutine != null) StopCoroutine(alertRoutine);
                alertRoutine = StartCoroutine(ShowTierAlertRoutine(tierName, hex));
            }
        }

        private IEnumerator ShowTierAlertRoutine(string tierName, string hex)
        {
            tierAlertBannerObj.SetActive(true);
            tierAlertTMP.text = $"<color=#FF2040>▲ DIFFICULTY INCREASE ▲</color>\n<size=125%><color=#{hex}><b>>> {tierName} <<</b></color></size>\n<size=75%><color=#E0E5F0>Enemies are deadlier and swarm in greater numbers!</color></size>";

            yield return new WaitForSeconds(3.5f);

            tierAlertBannerObj.SetActive(false);
            alertRoutine = null;
        }

        private void HandleAreaStarted(int areaIndex, string areaName)
        {
            if (areaTitleTMP != null)
            {
                areaTitleTMP.text = $"<color=#40EBFF><b>AREA {areaIndex}:</b></color> <color=#FFD700>{areaName.ToUpper()}</color>";
            }
        }

        private void HandleEnemyCountChanged(int count)
        {
            if (enemyCountTMP != null)
            {
                enemyCountTMP.text = count > 0 
                    ? $"<color=#FF5544>ENEMIES REMAINING:</color> <b>{count}</b>" 
                    : "<color=#30FF80>✔ AREA CLEARED!</color>";
            }
        }

        private void HandleBoonAcquired(DungeonBoon boon)
        {
            RefreshBoonsStrip();
        }

        private void HandleBuffsReset()
        {
            RefreshBoonsStrip();
        }

        public void RefreshBoonsStrip()
        {
            if (activeBoonsContainer == null) return;

            // Clear old badges
            for (int i = activeBadgeObjects.Count - 1; i >= 0; i--)
            {
                if (activeBadgeObjects[i] != null) Destroy(activeBadgeObjects[i]);
            }
            activeBadgeObjects.Clear();

            var boons = DungeonRunBuffSystem.Instance.ActiveBoons;
            for (int i = 0; i < boons.Count; i++)
            {
                var boon = boons[i];
                GameObject badge = CreateBoonBadge(boon, i);
                activeBadgeObjects.Add(badge);
            }
        }

        private GameObject CreateBoonBadge(DungeonBoon boon, int index)
        {
            GameObject badgeObj = new GameObject($"BoonBadge_{index}_{boon.id}", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(activeBoonsContainer, false);

            RectTransform rt = badgeObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(34f, 34f);

            Image bg = badgeObj.GetComponent<Image>();
            bg.color = new Color(0.10f, 0.12f, 0.18f, 0.95f);

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(badgeObj.transform, false);
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.1f, 0.1f);
            irt.anchorMax = new Vector2(0.9f, 0.9f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;

            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.color = boon.GetRarityColor();
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(boon.iconSpriteName))
            {
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/" + boon.iconSpriteName);
                if (sp != null)
                {
                    iconImg.sprite = sp;
                    iconImg.color = Color.white;
                }
            }
#endif

            // Stack badge
            if (boon.stackCount > 1)
            {
                GameObject stackObj = new GameObject("StackTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
                stackObj.transform.SetParent(badgeObj.transform, false);
                RectTransform srt = stackObj.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0f);
                srt.anchorMax = new Vector2(1f, 0.5f);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;
                TextMeshProUGUI stmp = stackObj.GetComponent<TextMeshProUGUI>();
                stmp.text = $"x{boon.stackCount}";
                stmp.fontSize = 9;
                stmp.alignment = TextAlignmentOptions.BottomRight;
                stmp.color = Color.yellow;
            }

            return badgeObj;
        }
    }
}

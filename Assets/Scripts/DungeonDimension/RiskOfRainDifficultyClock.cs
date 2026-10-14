using System;
using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    public enum DifficultyTier
    {
        VeryEasy,   // 0:00 - 1:00
        Easy,       // 1:00 - 2:30
        Medium,     // 2:30 - 4:00
        Hard,       // 4:00 - 6:00
        VeryHard,   // 6:00 - 8:30
        Insane,     // 8:30 - 11:30
        Impossible, // 11:30 - 15:00
        ISeeYou,    // 15:00 - 20:00
        Hahaha      // 20:00+
    }

    /// <summary>
    /// RiskOfRainDifficultyClock implements the continuous, time-based difficulty scaling system:
    /// - Tracks real-time elapsed in the Dungeon Dimension.
    /// - Calculates the dynamic difficulty coefficient C(t).
    /// - Progresses through 9 Risk of Rain styled difficulty tiers.
    /// - Dynamically scales enemy HP, Damage, Speed, and Elite spawn probability.
    /// </summary>
    public class RiskOfRainDifficultyClock : MonoBehaviour
    {
        private static RiskOfRainDifficultyClock _instance;
        public static RiskOfRainDifficultyClock Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<RiskOfRainDifficultyClock>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Clock State")]
        [SerializeField] private float runTime = 0f;
        [SerializeField] private bool isClockRunning = false;
        [SerializeField] private DifficultyTier currentTier = DifficultyTier.VeryEasy;

        [Header("Tuning Parameters")]
        [SerializeField] private float timeCoefficientRate = 0.0055f; // Exponential climb per second
        [SerializeField] private float playerLevelWeight = 0.08f;

        public float RunTime => runTime;
        public bool IsRunning => isClockRunning;
        public DifficultyTier CurrentTier => currentTier;

        public event Action<DifficultyTier, float> OnDifficultyTierChanged;
        public event Action<float> OnDifficultyTick;

        // Tier thresholds in seconds
        private static readonly float[] TierThresholds = {
            0f,     // VeryEasy
            60f,    // Easy (1m)
            150f,   // Medium (2m30s)
            240f,   // Hard (4m)
            360f,   // VeryHard (6m)
            510f,   // Insane (8m30s)
            690f,   // Impossible (11m30s)
            900f,   // ISeeYou (15m)
            1200f   // Hahaha (20m)
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Update()
        {
            if (!isClockRunning) return;

            runTime += Time.deltaTime;
            CheckTierTransition();
            OnDifficultyTick?.Invoke(runTime);
        }

        public void StartRun()
        {
            runTime = 0f;
            currentTier = DifficultyTier.VeryEasy;
            isClockRunning = true;
            OnDifficultyTierChanged?.Invoke(currentTier, GetDifficultyCoefficient());
        }

        public void PauseClock()
        {
            isClockRunning = false;
        }

        public void ResumeClock()
        {
            isClockRunning = true;
        }

        public void StopAndReset()
        {
            isClockRunning = false;
            runTime = 0f;
            currentTier = DifficultyTier.VeryEasy;
        }

        private void CheckTierTransition()
        {
            DifficultyTier calculatedTier = DifficultyTier.VeryEasy;
            for (int i = TierThresholds.Length - 1; i >= 0; i--)
            {
                if (runTime >= TierThresholds[i])
                {
                    calculatedTier = (DifficultyTier)i;
                    break;
                }
            }

            if (calculatedTier != currentTier)
            {
                currentTier = calculatedTier;
                float coeff = GetDifficultyCoefficient();
                OnDifficultyTierChanged?.Invoke(currentTier, coeff);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("ror_tier_up", 1.0f);
                }
            }
        }

        public float GetDifficultyCoefficient()
        {
            int playerLevel = 1;
            var prog = FindAnyObjectByType<PlayerProgression>();
            if (prog != null) playerLevel = prog.currentLevel;

            // RoR style formula: (1 + Time * rate) * (1 + (Level - 1) * weight)
            float timeFactor = 1f + (runTime * timeCoefficientRate);
            float levelFactor = 1f + Mathf.Max(0, playerLevel - 1) * playerLevelWeight;
            return timeFactor * levelFactor;
        }

        public float GetProgressToNextTier()
        {
            int index = (int)currentTier;
            if (index >= TierThresholds.Length - 1) return 1f;

            float currentStart = TierThresholds[index];
            float nextStart = TierThresholds[index + 1];
            return Mathf.Clamp01((runTime - currentStart) / (nextStart - currentStart));
        }

        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(runTime / 60f);
            int seconds = Mathf.FloorToInt(runTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        public float GetEnemyHealthMultiplier()
        {
            float coeff = GetDifficultyCoefficient();
            return Mathf.Max(1f, coeff);
        }

        public float GetEnemyDamageMultiplier()
        {
            float coeff = GetDifficultyCoefficient();
            return 1f + (coeff - 1f) * 0.55f;
        }

        public float GetEnemySpeedMultiplier()
        {
            float coeff = GetDifficultyCoefficient();
            return Mathf.Min(1.65f, 1f + (coeff - 1f) * 0.08f);
        }

        public float GetEliteChance()
        {
            float coeff = GetDifficultyCoefficient();
            // Starts at 5% at tier 1, reaches 80% at Hahaha
            return Mathf.Clamp((coeff - 1f) * 0.12f, 0.05f, 0.85f);
        }

        public static string GetTierName(DifficultyTier tier)
        {
            return tier switch
            {
                DifficultyTier.VeryEasy => "VERY EASY",
                DifficultyTier.Easy => "EASY",
                DifficultyTier.Medium => "MEDIUM",
                DifficultyTier.Hard => "HARD",
                DifficultyTier.VeryHard => "VERY HARD",
                DifficultyTier.Insane => "INSANE",
                DifficultyTier.Impossible => "IMPOSSIBLE",
                DifficultyTier.ISeeYou => "I SEE YOU",
                DifficultyTier.Hahaha => "HAHAHAHA",
                _ => "UNKNOWN"
            };
        }

        public static Color GetTierColor(DifficultyTier tier)
        {
            return tier switch
            {
                DifficultyTier.VeryEasy => new Color(0.20f, 0.95f, 0.40f), // Neon Lime Green
                DifficultyTier.Easy => new Color(0.20f, 0.90f, 1.00f),     // Bright Cyan
                DifficultyTier.Medium => new Color(1.00f, 0.92f, 0.20f),   // Canary Yellow
                DifficultyTier.Hard => new Color(1.00f, 0.60f, 0.15f),     // Amber Orange
                DifficultyTier.VeryHard => new Color(1.00f, 0.35f, 0.10f), // Fiery Orange
                DifficultyTier.Insane => new Color(1.00f, 0.15f, 0.25f),   // Crimson Red
                DifficultyTier.Impossible => new Color(0.70f, 0.20f, 1.00f),// Abyssal Purple
                DifficultyTier.ISeeYou => new Color(1.00f, 0.10f, 0.80f),  // Neon Magenta
                DifficultyTier.Hahaha => new Color(0.95f, 0.00f, 0.10f),   // Blood Red
                _ => Color.white
            };
        }
    }
}

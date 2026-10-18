using System;
using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    public enum InstabilityStage { Stable, Shifting, Volatile, Critical, CollapseImminent }

    /// <summary>
    /// DungeonInstabilityManager regulates time-based scaling inside the Dungeon Dimension.
    /// In the overworld, difficulty remains unscaled. Once a portal is entered, instability escalates
    /// based on elapsed time and squad size, inflating enemy HP, damage, spawn density, and loot rarity.
    /// </summary>
    public class DungeonInstabilityManager : MonoBehaviour
    {
        public static DungeonInstabilityManager Instance { get; private set; }

        [Header("Instability State")]
        public bool isInDungeonDimension = false;
        public float timeInDimensionSeconds = 0f;
        public int partyMemberCount = 1;
        public float instabilityMultiplier = 1.0f;
        public InstabilityStage currentStage = InstabilityStage.Stable;

        public event Action<InstabilityStage> OnInstabilityStageChanged;
        public event Action<bool> OnPortalStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!isInDungeonDimension) return;
            timeInDimensionSeconds += Time.deltaTime;
            UpdateInstabilityMath();
        }

        public void EnterDungeonDimension(int partySize = 1, float inheritedInstability = 1.0f)
        {
            partyMemberCount = Mathf.Clamp(partySize, 1, 10);
            timeInDimensionSeconds = 0f;
            instabilityMultiplier = Mathf.Max(1.0f, inheritedInstability);
            currentStage = InstabilityStage.Stable;
            isInDungeonDimension = true;
            OnPortalStateChanged?.Invoke(true);
            Debug.Log($"🌀 [DUNGEON DIMENSION] Portal Breach! Party size: {partyMemberCount}, Initial Instability: {instabilityMultiplier:F2}x.");
        }

        public void ExitDungeonDimension()
        {
            isInDungeonDimension = false;
            timeInDimensionSeconds = 0f;
            instabilityMultiplier = 1.0f;
            currentStage = InstabilityStage.Stable;
            OnPortalStateChanged?.Invoke(false);
            Debug.Log("🛡️ [DUNGEON DIMENSION] Exited to Overworld. Instability stabilized.");
        }

        public void AdvanceDimensionTime(float deltaSeconds)
        {
            if (!isInDungeonDimension) return;
            timeInDimensionSeconds += deltaSeconds;
            UpdateInstabilityMath();
        }

        public void UpdateInstabilityMath()
        {
            float minutes = timeInDimensionSeconds / 60f;
            float squadFactor = 1.0f + (0.2f * (partyMemberCount - 1));
            instabilityMultiplier = (1.0f + (0.18f * minutes * squadFactor)) * Mathf.Pow(1.12f, minutes / 3.5f);

            InstabilityStage nextStage = minutes >= 18f ? InstabilityStage.CollapseImminent :
                                         minutes >= 12f ? InstabilityStage.Critical :
                                         minutes >= 7f  ? InstabilityStage.Volatile :
                                         minutes >= 3f  ? InstabilityStage.Shifting : InstabilityStage.Stable;

            if (nextStage != currentStage)
            {
                currentStage = nextStage;
                OnInstabilityStageChanged?.Invoke(currentStage);
                Debug.Log($"⚠️ [INSTABILITY SURGE] Stage Transition: {currentStage} (Multiplier: {instabilityMultiplier:F2}x)!");
            }
        }

        public float GetEnemyHealthMultiplier() => instabilityMultiplier;
        public float GetEnemyDamageMultiplier() => 1.0f + ((instabilityMultiplier - 1.0f) * 0.65f);
        public float GetSpawnDensityMultiplier() => Mathf.Min(4.0f, 1.0f + ((instabilityMultiplier - 1.0f) * 0.35f));
        public float GetLootRarityMultiplier() => 1.0f + ((instabilityMultiplier - 1.0f) * 0.50f);
    }
}

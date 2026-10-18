using System;
using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    public enum OutpostTier { Tier0_Clear, Tier1_Encampment, Tier2_Outpost, Tier3_RampagingFortress }

    /// <summary>
    /// PortalOutpostFortressNode tracks outside monster incursions around a dungeon portal.
    /// If left unchecked over real time, enemy activity escalates from Encampment (10m) -> Outpost (30m) -> Rampaging Fortress (60m).
    /// Rampaging Fortresses lock the portal entrance with a massive siege gate and prompt a Guild Mass Raid broadcast.
    /// </summary>
    public class PortalOutpostFortressNode : MonoBehaviour
    {
        [Header("Portal Identity")]
        public string portalId = "Portal_01";
        public string portalRegionName = "Spire Outer Woods";
        public int recommendedLevel = 15;

        [Header("Incursion Progression")]
        public float uncheckedDurationMinutes = 0f;
        public OutpostTier currentOutpostTier = OutpostTier.Tier0_Clear;
        public bool isGuildMassRaidActive = false;

        [Header("Siege Gate Vitals")]
        public float fortressGateMaxHp = 5000f;
        public float fortressGateCurrentHp = 0f;
        public bool isPortalEntranceBlockedByFortress = false;

        [Header("Visual Prefabs")]
        [SerializeField] private GameObject tier1EncampmentPrefab;
        [SerializeField] private GameObject tier2OutpostPrefab;
        [SerializeField] private GameObject tier3FortressPrefab;

        public event Action<string, int> OnGuildMassRaidIssued;

        private void Update()
        {
            uncheckedDurationMinutes += Time.deltaTime / 60f;
            OutpostTier nextTier = uncheckedDurationMinutes >= 60f ? OutpostTier.Tier3_RampagingFortress :
                                   uncheckedDurationMinutes >= 30f ? OutpostTier.Tier2_Outpost :
                                   uncheckedDurationMinutes >= 10f ? OutpostTier.Tier1_Encampment : OutpostTier.Tier0_Clear;

            if (nextTier != currentOutpostTier) EscalateToTier(nextTier);
        }

        public void EscalateToTier(OutpostTier newTier)
        {
            currentOutpostTier = newTier;
            if (tier1EncampmentPrefab != null) tier1EncampmentPrefab.SetActive(newTier == OutpostTier.Tier1_Encampment);
            if (tier2OutpostPrefab != null)    tier2OutpostPrefab.SetActive(newTier == OutpostTier.Tier2_Outpost);
            if (tier3FortressPrefab != null)   tier3FortressPrefab.SetActive(newTier == OutpostTier.Tier3_RampagingFortress);

            if (newTier != OutpostTier.Tier0_Clear)
            {
                isPortalEntranceBlockedByFortress = true;
                fortressGateCurrentHp = newTier == OutpostTier.Tier3_RampagingFortress ? fortressGateMaxHp : 1500f;
            }

            if (newTier == OutpostTier.Tier3_RampagingFortress && !isGuildMassRaidActive)
            {
                isGuildMassRaidActive = true;
                int bounty = 1500 * Mathf.Max(1, recommendedLevel / 5);
                SystemBroadcastManager.Instance?.DispatchBroadcast(
                    $"[GUILD MASS RAID] Unchecked Portal at {portalRegionName} is now a Rampaging Fortress! Reward: {bounty} Mana Cores!", true);
                OnGuildMassRaidIssued?.Invoke(portalId, bounty);
            }
        }

        public void ApplySiegeDamageToFortress(float damage)
        {
            if (!isPortalEntranceBlockedByFortress) return;
            fortressGateCurrentHp = Mathf.Max(0f, fortressGateCurrentHp - damage);
            if (fortressGateCurrentHp <= 0f) isPortalEntranceBlockedByFortress = false;
        }

        public float GetInheritedStartingInstability() => 1.0f + (uncheckedDurationMinutes / 15f) * 0.5f;

        public void CompleteGuildSubjugationAndReset()
        {
            uncheckedDurationMinutes = 0f;
            isGuildMassRaidActive = false;
            isPortalEntranceBlockedByFortress = false;
            EscalateToTier(OutpostTier.Tier0_Clear);
        }
    }
}

using UnityEngine;
using BattleSummoner.Core;
using BattleSummoner.Core.DimensionalCompiler;
using BattleSummoner.DesignPhilosophy;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// Bridges 1:1 blade swipes, Mega Man X boss limb severing, and medieval instrument frequencies
    /// directly into the Blood & Hearth Master Director and 0D-11D Runtime Compiler.
    /// </summary>
    public class CombatToBloodHearthBridge : MonoBehaviour
    {
        [Header("Equipped Heirloom / Improvised Gear References")]
        public WeaponTemper equippedWeaponTemper;
        public DimensionalArtifact equippedWeapon12D;
        public bool isEquippedWeaponImprovisedProp = false;
        public bool isEquippedWeaponCulinaryWok = false;
        public string improvisedPropArchetype = "Wok";

        [Header("Active Summon 7D/10D Acoustic Link")]
        public DimensionalArtifact activeSummon12D;
        public InstrumentFreeplaySync medievalInstrumentSync;

        private void OnEnable()
        {
            if (medievalInstrumentSync != null)
            {
                medievalInstrumentSync.OnNotePlayed += HandleMedievalInstrumentNotePlayed;
            }
        }

        private void OnDisable()
        {
            if (medievalInstrumentSync != null)
            {
                medievalInstrumentSync.OnNotePlayed -= HandleMedievalInstrumentNotePlayed;
            }
        }

        /// <summary>
        /// Call from FruitNinjaBladeCutter when a 1:1 swipe slices an enemy or boss weak-spot seam.
        /// </summary>
        public void ProcessLiveSwipeHit(
            Vector3 hitWorldPos,
            Vector3 swipeDir,
            float baseSwipeVelocity,
            bool hitWeakSpotSeam,
            SeverableBossLimb targetBossLimb = null,
            string activeElementTag = "Physical")
        {
            if (BloodAndHearthMasterDirector.Instance == null) return;

            float finalDamage = baseSwipeVelocity;

            // 1. Route "Anything Is A Weapon" & +0.05 Wok Cooking EXP if holding an improvised prop
            if (isEquippedWeaponImprovisedProp)
            {
                finalDamage = BloodAndHearthMasterDirector.Instance.ExecuteImprovisedPropStrike(
                    improvisedPropArchetype,
                    isEquippedWeaponCulinaryWok,
                    baseSwipeVelocity,
                    equippedWeapon12D);
            }
            else if (equippedWeapon12D != null)
            {
                // Apply 5D Density Matrix Armor Penetration from the 0D-11D Compiler
                finalDamage *= Mathf.Max(1.0f, equippedWeapon12D.densityMatrix_5D * 0.08f);
            }

            // 2. Check Mega Man X / Monster Surgery Boss Limb Severing
            bool limbSevered = false;
            if (targetBossLimb != null)
            {
                float temperMult = equippedWeaponTemper != null
                    ? Mathf.Clamp(equippedWeaponTemper.currentDurability / Mathf.Max(1f, equippedWeaponTemper.maxDurability), 0.2f, 1.5f)
                    : 1.0f;

                limbSevered = targetBossLimb.ApplyTargetedSlice(finalDamage, activeElementTag, temperMult);
                if (limbSevered)
                {
                    // Automatically feed or drop the severed boss catalyst for Shield Hero Heirloom Evolution!
                    DevouredMonsterCatalyst severedCatalyst = new DevouredMonsterCatalyst
                    {
                        monsterPartId = targetBossLimb.limbName.Replace(" ", "_"),
                        monsterPartDisplayName = targetBossLimb.limbName,
                        unlockedFormOrCounterTrait = $"Cleaver of {targetBossLimb.limbName}",
                        bonusBaseDamage = 18f,
                        bonus5DDensity = 6.5f,
                        counterElementTag = activeElementTag
                    };
                    BloodAndHearthMasterDirector.Instance.FeedSeveredMonsterPartToHeirloom(
                        severedCatalyst, equippedWeaponTemper, equippedWeapon12D);
                }
            }

            // 3. Trigger directional blood splatter, armor grime, screen-edge droplets & DanMachi Excelsia
            BloodAndHearthMasterDirector.Instance.RegisterVisceralCombatHit(
                hitWorldPos,
                swipeDir,
                finalDamage,
                hitWeakSpotSeam || limbSevered,
                false);
        }

        /// <summary>
        /// Replaces deprecated DJ Turntables: playing a Kalimba, Lute, or Battle Horn note matching
        /// the summon's hidden 10D Carrier Frequency resets its 7D Temporal Decay!
        /// </summary>
        private void HandleMedievalInstrumentNotePlayed(string noteName, float playedFrequencyHz)
        {
            if (activeSummon12D == null) return;

            float freqDelta = Mathf.Abs(playedFrequencyHz - activeSummon12D.carrierFrequency_10D);
            if (freqDelta <= 28f)
            {
                activeSummon12D.temporalDecay_7D = 0f;
                activeSummon12D.quantumStability_11D = Mathf.Min(1.25f, activeSummon12D.quantumStability_11D + 0.10f);
                Debug.Log($"[10D Acoustic Resonance] Medieval instrument note {noteName} ({playedFrequencyHz}Hz) matched '{activeSummon12D.baseClassName_8D}'! 7D Summon Decay halted!");
            }
        }
    }
}

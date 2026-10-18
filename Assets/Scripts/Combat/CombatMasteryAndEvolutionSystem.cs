using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// Weak-Spot System, Monster Bestiary Sketchbook, Disarm Counter-Parry & Heirloom Evolution.
    /// Evaluates physical angle seams (0.88+ dot) and elemental weaknesses (2.25x crit).
    /// Bestiary sketchbook progress (0-100%) unlocks gold glowing weak spots on HUD.
    /// Two-finger tap parries scale with Dexterity to Disarm & Snatch enemy weapons.
    /// Starter iron blades maintained across 500+ kills trigger Heirloom Awakening (2.8x dmg).
    /// Grudge-Marked killers generate a Warwick-style red Blood-Scent LineRenderer path (2x Gold & 2x EXP).
    /// </summary>
    public class CombatMasteryAndEvolutionSystem : MonoBehaviour
    {
        public static CombatMasteryAndEvolutionSystem Instance { get; private set; }

        [Header("Player Dexterity (Scales Disarm & Snatch Parry)")]
        public int dexterityStat = 10;

        [Header("Monster Bestiary Knowledge (SpeciesID -> 0..100%)")]
        public Dictionary<string, float> bestiaryKnowledge = new Dictionary<string, float>();

        [Header("Blood-Scent Grudge Trail")]
        [SerializeField] private LineRenderer bloodScentTrailRenderer;
        public Transform grudgeTargetEnemy;
        public bool hasActiveGrudgeTarget = false;

        public event Action<string, float> OnBestiaryUpdated; // (speciesId, currentPct)
        public event Action<string> OnWeakSpotMastered;        // (speciesId) at 100%
        public event Action<string> OnEnemyDisarmed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (grudgeTargetEnemy != null && bloodScentTrailRenderer != null)
            {
                bloodScentTrailRenderer.enabled = true;
                bloodScentTrailRenderer.positionCount = 2;
                bloodScentTrailRenderer.SetPosition(0, transform.position + Vector3.up * 0.5f);
                bloodScentTrailRenderer.SetPosition(1, grudgeTargetEnemy.position + Vector3.up * 0.5f);
            }
            else if (bloodScentTrailRenderer != null && bloodScentTrailRenderer.enabled)
            {
                bloodScentTrailRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Evaluates Physical Angle Seam or Elemental Weakness hits.
        /// </summary>
        public float EvaluateWeakSpotHit(
            string speciesId,
            Vector2 playerSwipeAngle,
            Vector2 monsterWeakSpotSeamAngle,
            SpellAffinity hitElement,
            SpellAffinity monsterElementalWeakness,
            out bool triggeredWeakSpotCrit)
        {
            RecordBestiaryProgress(speciesId, 2.5f);
            float angleDot = Vector2.Dot(playerSwipeAngle.normalized, monsterWeakSpotSeamAngle.normalized);
            bool hitPhysicalSeam = Mathf.Abs(angleDot) >= 0.88f; // Swipe aligned with weak-spot seam
            bool hitElementalWeakness = (hitElement != SpellAffinity.None && hitElement == monsterElementalWeakness);

            if (hitPhysicalSeam || hitElementalWeakness)
            {
                triggeredWeakSpotCrit = true;

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.85f);
                }

                Debug.Log($"🎯 [WEAK-SPOT CRIT] {(hitPhysicalSeam ? "Physical Seam" : "Elemental")} strike on {speciesId}! 2.25x Damage!");
                return 2.25f; // 2.25x Weak-Spot Critical Multiplier
            }

            triggeredWeakSpotCrit = false;
            return 1.0f;
        }

        public bool IsBestiaryWeakSpotGoldHighlighted(string speciesId)
        {
            return bestiaryKnowledge.TryGetValue(speciesId, out float pct) && pct >= 100f;
        }

        public void RecordBestiaryProgress(string speciesId, float gain)
        {
            if (string.IsNullOrEmpty(speciesId)) return;

            if (!bestiaryKnowledge.ContainsKey(speciesId)) bestiaryKnowledge[speciesId] = 0f;
            float prev = bestiaryKnowledge[speciesId];
            bestiaryKnowledge[speciesId] = Mathf.Clamp(bestiaryKnowledge[speciesId] + gain, 0f, 100f);

            OnBestiaryUpdated?.Invoke(speciesId, bestiaryKnowledge[speciesId]);

            if (prev < 100f && bestiaryKnowledge[speciesId] >= 100f)
            {
                OnWeakSpotMastered?.Invoke(speciesId);
                if (SystemBroadcastManager.Instance != null)
                {
                    SystemBroadcastManager.Instance.DispatchBroadcast(
                        $"📖 [BESTIARY MASTERED] 100% Knowledge on {speciesId}! Weak spots now permanently highlighted in Gold!", false);
                }
            }
        }

        /// <summary>
        /// Called on a successful Two-Finger Tap Parry. Higher Dexterity increases chance to Disarm, Collect, and Counter-Slash in one go.
        /// </summary>
        public bool TryDexterityDisarmAndCollectCounter(string enemyWeaponItemId, out float counterDamageMultiplier)
        {
            // Base 5% chance + 0.35% per Dexterity point (capped at 40%)
            float disarmChance = Mathf.Clamp(0.05f + (dexterityStat * 0.0035f), 0.05f, 0.40f);
            if (UnityEngine.Random.value <= disarmChance)
            {
                counterDamageMultiplier = 2.5f;
                Debug.Log($"⚡ [DEX DISARM COUNTER!] Disarmed & collected '{enemyWeaponItemId}' and unleashed a 2.5x Counter-Parry!");

                if (SystemBroadcastManager.Instance != null)
                {
                    SystemBroadcastManager.Instance.DispatchBroadcast($"⚡ [DISARM COUNTER] Snatched {enemyWeaponItemId} from enemy grasp!", false);
                }

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 1.0f);
                }

                OnEnemyDisarmed?.Invoke(enemyWeaponItemId);
                return true;
            }

            counterDamageMultiplier = 1.25f;
            return false;
        }

        /// <summary>
        /// Heirloom Weapon Kill-Count Awakening: Evolves starter weapons after 500+ kills.
        /// </summary>
        public void RegisterWeaponKillForHeirloomEvolution(
            ref string weaponName,
            ref int totalKills,
            ref float baseDamage,
            ref bool isHeirloomAwakened,
            string mostKilledSpecies)
        {
            totalKills++;
            if (!isHeirloomAwakened && totalKills >= 500)
            {
                isHeirloomAwakened = true;
                weaponName = $"Awakened {mostKilledSpecies}-Bane {weaponName}";
                baseDamage *= 2.8f; // Jumps to end-game stat tier!

                if (SystemBroadcastManager.Instance != null)
                {
                    SystemBroadcastManager.Instance.DispatchBroadcast(
                        $"👑 A cherished blade has achieved Heirloom Awakening: {weaponName}! (2.8x Damage)", true);
                }

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.8f);
                }
            }
        }

        /// <summary>
        /// Dungeon Weapon Fusion Anvil: Fuses two weapons together for the duration of the run.
        /// </summary>
        public void FuseWeaponsForRun(string nameA, float dmgA, string nameB, float dmgB, out string fusedWeaponName, out float fusedDamage)
        {
            fusedWeaponName = $"Fused {nameA} & {nameB}";
            fusedDamage = (dmgA + dmgB) * 0.85f; // Synergistic damage fusion

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast($"🔥 [ANVIL FUSION] Created {fusedWeaponName} (Base Dmg: {fusedDamage:F1})!", false);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }
        }

        /// <summary>
        /// Boss/Enemy Grudge Mark: When an enemy kills the player, it marks them and enables Warwick-style blood trail.
        /// </summary>
        public void SetGrudgeTarget(Transform enemy, string speciesId)
        {
            grudgeTargetEnemy = enemy;
            hasActiveGrudgeTarget = enemy != null;

            if (hasActiveGrudgeTarget)
            {
                if (SystemBroadcastManager.Instance != null)
                {
                    SystemBroadcastManager.Instance.DispatchBroadcast(
                        $"🩸 [GRUDGE-MARK] Blood-Scent trail active! Hunt down the {speciesId} for 2x Gold and 2x EXP!", true);
                }
            }
        }
    }
}

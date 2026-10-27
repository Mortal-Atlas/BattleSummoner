using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Progression.ThreeMasks;

namespace BattleSummoner.Spellcraft
{
    public enum SpellDeliveryPrefab
    {
        StraightShot, HeavyLobArc, AoEDomeBubble, HomingSwarm, OrbitAura, BladeArrowCoat, TetherTrap
    }

    public enum SpellImpactBehavior
    {
        InstantBurst, PeriodicTick, StickyLimbLatch
    }

    public enum TimeManipulationMode
    {
        None,
        LocalizedChronoSlowBubble, // Ekko-style zone: slows enemies/projectiles; Player moves 100% speed!
        LimbTemporalDrag,          // Slows only the struck enemy limb
        FastForwardDecay,          // Accelerates bleed/poison ticks & armor rust
        DelayedAfterimageEcho      // Repeats player slash/cast 1.5s later
    }

    [Serializable]
    public class CustomConstructedSpell
    {
        public string spellFormulaHash;
        public string customSpellName;
        public string customInnerRuneNotationId; // Option A: Player's custom drawn inner symbol
        public SpellDeliveryPrefab deliveryPrefab;
        public SpellImpactBehavior impactBehavior;
        public AffinityElement affinityEffect;
        public TimeManipulationMode timeMode = TimeManipulationMode.None;

        [Range(1.0f, 10.0f)] public float durationSeconds = 3.0f;
        [Range(0.5f, 8.0f)]  public float radiusMeters = 3.5f;
        [Range(0.15f, 0.80f)] public float potencyRatio = 0.65f; // Max 80% slow (NEVER 100% time stop!)

        public float compiledManaCost;
        public float compiledCooldownSeconds;
        public float instabilityRating01;
        public bool isPreExistingServerPatent;
        public string originalPatentHolderName;
    }

    public class BuildASpellWorkbenchAndChronoEngine : MonoBehaviour
    {
        public static BuildASpellWorkbenchAndChronoEngine Instance { get; private set; }

        public List<CustomConstructedSpell> playerGrimoireSpells = new List<CustomConstructedSpell>();
        private Dictionary<string, string> _serverPatentLedger = new Dictionary<string, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public CustomConstructedSpell CompileSpellAtWorkbench(
            string spellName,
            string drawnInnerRuneNotationId,
            SpellDeliveryPrefab delivery,
            SpellImpactBehavior behavior,
            AffinityElement affinity,
            TimeManipulationMode timeMode,
            float requestedDuration,
            float requestedRadius,
            float requestedPotency,
            float player11DStabilityTolerance = 100f)
        {
            float safePotency = Mathf.Clamp(requestedPotency, 0.15f, 0.80f);
            float safeDuration = Mathf.Clamp(requestedDuration, 1.0f, 10.0f);
            float safeRadius = Mathf.Clamp(requestedRadius, 0.5f, 8.0f);

            float deliveryMult = delivery switch
            {
                SpellDeliveryPrefab.HomingSwarm => 1.55f,
                SpellDeliveryPrefab.AoEDomeBubble => 1.35f,
                SpellDeliveryPrefab.BladeArrowCoat => 1.25f,
                _ => 1.0f
            };

            float rawLoad = (safeDuration * 8f) * (1f + safeRadius * 0.35f) * (1f + safePotency * 1.5f) * deliveryMult;

            if (ThreeMasksAndWildcardTalentSystem.Instance != null &&
                ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1 != null &&
                ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1.grantedAffinities.Contains(affinity))
            {
                rawLoad *= (1.0f - ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1.spellCraftStabilityDiscount);
            }

            string formulaHash = $"{delivery}_{behavior}_{affinity}_{timeMode}_{Mathf.RoundToInt(safeDuration)}_{Mathf.RoundToInt(safeRadius)}_{Mathf.RoundToInt(safePotency * 10f)}";
            bool patentExists = false;
            string originalInventor = "LocalDreamer";

            if (GuildPatentOffice.Instance != null)
            {
                patentExists = GuildPatentOffice.Instance.CheckPatentCollision(formulaHash, out var matchedPatent, out float discount, out float royaltyCut);
                if (patentExists && matchedPatent != null)
                {
                    rawLoad *= (1.0f - discount); // -15% Instability discount
                    originalInventor = matchedPatent.inventorName;
                }
                else
                {
                    GuildPatentOffice.Instance.RegisterNewPatent(formulaHash, spellName, "LocalDreamer", drawnInnerRuneNotationId);
                }
            }
            else
            {
                patentExists = _serverPatentLedger.TryGetValue(formulaHash, out originalInventor);
                if (patentExists) rawLoad *= 0.85f;
                else
                {
                    _serverPatentLedger[formulaHash] = "LocalDreamer";
                    originalInventor = "LocalDreamer";
                }
            }

            var compiled = new CustomConstructedSpell
            {
                spellFormulaHash = formulaHash,
                customSpellName = spellName,
                customInnerRuneNotationId = drawnInnerRuneNotationId,
                deliveryPrefab = delivery,
                impactBehavior = behavior,
                affinityEffect = affinity,
                timeMode = timeMode,
                durationSeconds = safeDuration,
                radiusMeters = safeRadius,
                potencyRatio = safePotency,
                compiledManaCost = Mathf.Round(rawLoad * 0.9f),
                compiledCooldownSeconds = Mathf.Round(safeDuration * deliveryMult * 1.8f),
                instabilityRating01 = rawLoad / Mathf.Max(10f, player11DStabilityTolerance),
                isPreExistingServerPatent = patentExists,
                originalPatentHolderName = originalInventor
            };

            playerGrimoireSpells.RemoveAll(s => s.customInnerRuneNotationId == drawnInnerRuneNotationId);
            playerGrimoireSpells.Add(compiled);
            return compiled;
        }
    }
}

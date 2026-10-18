using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;

/// <summary>
/// SpellSynergyMatrix: Evaluates cross-elemental bitmask synergies and multi-mage combinations.
/// Produces amplified constructs, 12D telemetry mutations, and elemental reactions.
/// </summary>
public static class SpellSynergyMatrix
{
    [Serializable]
    public class SynergyEvaluation
    {
        public string synergyName;
        public string description;
        public ExtendedSpellElement combinedMask;
        public float finalPower;
        public float aoeRadiusMultiplier = 1.0f;
        public float densityReduction_5D = 0f;
        public float thermalEnthalpy_6D = 20f;
        public float carrierFrequency_10D = 60f;
        public float quantumStabilityBonus_11D = 0f;
        public bool isAegisWard = false;
        public string statusDebuffApplied = "None";
    }

    public static SynergyEvaluation EvaluateSynergy(ExtendedSpellElement mask, float baseCombinedPower, int mageCount = 1)
    {
        var eval = new SynergyEvaluation
        {
            combinedMask = mask,
            finalPower = baseCombinedPower * 1.35f, // Cross-elemental reaction multiplier baseline
            quantumStabilityBonus_11D = 0.15f * Mathf.Max(0, mageCount - 1)
        };

        // 1. Water + Lightning -> Arc-Conduction Torrent
        if (HasFlags(mask, ExtendedSpellElement.Water | ExtendedSpellElement.Lightning))
        {
            eval.synergyName = "Arc-Conduction Torrent (Water + Lightning)";
            eval.description = "Electrifies the entire water volume, chaining guaranteed crits and micro-stuns across all targets touching the puddle.";
            eval.aoeRadiusMultiplier = 1.8f;
            eval.finalPower *= 1.25f;
            eval.carrierFrequency_10D = 120f;
            eval.statusDebuffApplied = "ElectrifiedPuddle_MicroStun";
            return eval;
        }

        // 2. Lightning + Fire -> Magnetized Plasma Lance
        if (HasFlags(mask, ExtendedSpellElement.Lightning | ExtendedSpellElement.Fire))
        {
            eval.synergyName = "Magnetized Plasma Lance (Lightning + Fire)";
            eval.description = "High-density thermal arc that melts target armor (5D Density Matrix reduction) and detonates on impact.";
            eval.densityReduction_5D = 4.5f;
            eval.thermalEnthalpy_6D = 1800f;
            eval.finalPower *= 1.40f;
            eval.carrierFrequency_10D = 240f;
            eval.statusDebuffApplied = "ArmorMelt_PlasmaBurn";
            return eval;
        }

        // 3. Fire + Wind (Aero) -> Pressurized Firestorm Vortex
        if (HasFlags(mask, ExtendedSpellElement.Fire | ExtendedSpellElement.Wind))
        {
            eval.synergyName = "Pressurized Firestorm Vortex (Fire + Wind)";
            eval.description = "Feeds oxygen into the flame, expanding AoE radius by 250%, pulling enemies inward, and doubling Lingering Oxidation burn ticks.";
            eval.aoeRadiusMultiplier = 2.50f;
            eval.thermalEnthalpy_6D = 950f;
            eval.finalPower *= 1.30f;
            eval.statusDebuffApplied = "LingeringOxidation_VortexPull";
            return eval;
        }

        // 4. Rock (Earth) + Fire -> Tectonic Magma Slag
        if (HasFlags(mask, ExtendedSpellElement.Rock | ExtendedSpellElement.Fire))
        {
            eval.synergyName = "Tectonic Magma Slag (Rock + Fire)";
            eval.description = "Turns solid rock projectiles into molten area-denial pools that inflict severe 6D Thermal Enthalpy overheat and shatter shield poise.";
            eval.thermalEnthalpy_6D = 1200f;
            eval.aoeRadiusMultiplier = 1.6f;
            eval.finalPower *= 1.30f;
            eval.statusDebuffApplied = "MagmaAreaDenial_PoiseShatter";
            return eval;
        }

        // 5. Wind (Aero) + Water -> Cryo-Shear Razor Mist
        if (HasFlags(mask, ExtendedSpellElement.Wind | ExtendedSpellElement.Water))
        {
            eval.synergyName = "Cryo-Shear Razor Mist (Wind + Water)";
            eval.description = "High-RPM compressed water/air discs (PlanarDisc) that inflict heavy Bleed + Chilled stacks.";
            eval.finalPower *= 1.35f;
            eval.aoeRadiusMultiplier = 1.4f;
            eval.statusDebuffApplied = "BleedAndChilled";
            return eval;
        }

        // 6. Rock (Earth) + Lightning -> Fulgarite Monolith
        if (HasFlags(mask, ExtendedSpellElement.Rock | ExtendedSpellElement.Lightning))
        {
            eval.synergyName = "Fulgarite Monolith (Rock + Lightning)";
            eval.description = "Spawns a magnetized stone pillar that pulses shockwaves and rips metallic weapons out of enemy hands.";
            eval.aoeRadiusMultiplier = 2.0f;
            eval.finalPower *= 1.30f;
            eval.densityReduction_5D = 3.0f;
            eval.statusDebuffApplied = "MagnetizedShockwave_Disarm";
            return eval;
        }

        // 7. Healing (Heart) + Any Element -> Aegis Infusion
        if ((mask & ExtendedSpellElement.Healing) != 0)
        {
            ExtendedSpellElement infused = mask & ~ExtendedSpellElement.Healing;
            eval.synergyName = $"Aegis Infusion ({infused})";
            eval.description = "Converts offensive elemental energy into a party-wide elemental ward and cleanses matching status debuffs across the 9-ally right-hand column.";
            eval.isAegisWard = true;
            eval.finalPower *= 1.15f;
            eval.statusDebuffApplied = "PartyAegisWard_Cleanse";
            return eval;
        }

        // Generic fallback
        eval.synergyName = $"Prismatic Convergence ({mask})";
        eval.description = "Multi-elemental convergence surging with unstable harmonic power.";
        return eval;
    }

    /// <summary>
    /// Checks bitmask containment.
    /// </summary>
    public static bool HasFlags(ExtendedSpellElement mask, ExtendedSpellElement check) => (mask & check) == check;

    /// <summary>
    /// Executes party-wide warding and cleanse across active allies and players.
    /// </summary>
    public static void ApplyAegisPartyCleanse()
    {
        var allReceivers = UnityEngine.Object.FindObjectsByType<StatusEffectReceiver>(FindObjectsSortMode.None);
        int cleansed = 0;
        foreach (var receiver in allReceivers)
        {
            if (receiver.CompareTag("Player") || receiver.name.Contains("Player") || receiver.name.Contains("Companion") || receiver.name.Contains("Ally"))
            {
                receiver.CleanseAllDebuffs();
                cleansed++;
            }
        }
        Debug.Log($"🛡️ [AEGIS INFUSION] Party elemental ward active! Cleansed status debuffs across {cleansed} allies.");
    }
}

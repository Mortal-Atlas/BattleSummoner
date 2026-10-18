using UnityEngine;
using BattleSummoner.Hobbies;

public class EnchantmentForgeThreeStrike : MonoBehaviour
{
    public enum ForgeOutcome
    {
        Accepted,
        Strike1_Deflection,
        Strike2_FractureWarning,
        Strike3_CatastrophicDetonation
    }

    [Header("Anvil State")]
    public int strikeCounter = 0;
    public int safeCapacitySockets = 3;

    [Header("12D Artifact Coupling")]
    [SerializeField] private DimensionalArtifact activeForgeArtifact;

    public ForgeOutcome AttemptEnchant(LivingGearSystem weapon, int currentSocketCount, PlayerProgression player, DimensionalArtifact boundArtifact = null, DimensionalRuntimeExecutor runtimeExecutor = null)
    {
        DimensionalArtifact targetArtifact = boundArtifact != null ? boundArtifact : activeForgeArtifact;

        // Apply 12D mutation: Each enchantment socket increases 0D Logic Complexity and reduces 11D Quantum Stability
        if (targetArtifact != null && ItemEnchantmentMatrix.Instance != null)
        {
            ItemEnchantmentMatrix.Instance.ApplyEnchantmentTelemetry(targetArtifact, currentSocketCount + 1);
        }

        if (currentSocketCount < safeCapacitySockets)
        {
            strikeCounter = 0;
            return ForgeOutcome.Accepted;
        }

        strikeCounter++;

        switch (strikeCounter)
        {
            case 1:
                return ForgeOutcome.Strike1_Deflection;

            case 2:
                // Strike 2: Halves current temper and inflicts fracture debt
                if (weapon != null)
                {
                    weapon.currentTemper *= 0.5f;
                    weapon.fractureDebt += 20f;
                }
                return ForgeOutcome.Strike2_FractureWarning;

            case 3:
            default:
                // Strike 3: Detonation & 11D Wavefunction Collapse
                if (weapon != null)
                {
                    weapon.currentTemper = 0f;
                    weapon.fractureDebt = weapon.maxTemper;
                }
                if (player != null)
                {
                    player.TakeDirectDamage(350f);
                    SystemBroadcastManager.Instance?.TriggerAchievement("ach_secret_anvil_suicide", player.playerName);
                }

                // 12D Subsystem Directive 3: Over-limit attempt drops 11D Stability to 0% and triggers Wavefunction Collapse
                if (targetArtifact != null)
                {
                    targetArtifact.quantumStability_11D = 0f;
                    targetArtifact.logicComplexity_0D += 500f;
                }

                if (runtimeExecutor != null)
                {
                    runtimeExecutor.TriggerWavefunctionCollapse();
                }
                else
                {
                    var sceneExecutor = FindFirstObjectByType<DimensionalRuntimeExecutor>();
                    if (sceneExecutor != null)
                    {
                        sceneExecutor.TriggerWavefunctionCollapse();
                    }
                    else if (SystemBroadcastManager.Instance != null)
                    {
                        SystemBroadcastManager.Instance.DispatchBroadcast("Secret Axiom Unlocked: Wavefunction Decoherence", false);
                    }
                }

                strikeCounter = 0;
                return ForgeOutcome.Strike3_CatastrophicDetonation;
        }
    }

    public void StabilizeWeaponMatrix()
    {
        strikeCounter = 0;
    }

    /// <summary>
    /// Called when the player finishes hammering at a campfire or hub anvil.
    /// Overwrites the sword's baseDamage, maxDurability, and maxSockets.
    /// </summary>
    public ForgedSwordStats FinishHammering(WeaponTemper weapon = null, bool atCampfire = false)
    {
        var result = BlacksmithForgeController.QuenchAndFinishSword(weapon, weapon?.GetComponent<LivingGearSystem>(), atCampfire);
        if (result != null)
        {
            safeCapacitySockets = result.maxSockets;
        }
        return result;
    }
}

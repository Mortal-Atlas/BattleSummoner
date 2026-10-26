using System;
using UnityEngine;
using BattleSummoner.Core.DimensionalCompiler;

/// <summary>
/// ItemEnchantmentMatrix handles 3-strike anvil forging, 12D telemetry coupling, and gem resonance matrices.
/// Each socketed enchantment mutates 0D Logic Complexity and 11D Quantum Stability.
/// </summary>
public class ItemEnchantmentMatrix : MonoBehaviour
{
    public static ItemEnchantmentMatrix Instance { get; private set; }

    [Header("Anvil & Weapon Matrix State")]
    public int baseCapacity = 3;
    public float currentWeaponDurability = 150f;

    [Header("12D Enchantment Telemetry")]
    [SerializeField] private float complexityPerSocket_0D = 18f;
    [SerializeField] private float stabilityPenaltyPerSocket_11D = 0.22f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public float CalculateEnchantmentYield(int strikePrecision, float gemPurity)
    {
        return Mathf.Clamp01(strikePrecision / 100f) * gemPurity * 1.5f;
    }

    /// <summary>
    /// Mutates an artifact's 12D telemetry when an enchantment socket is forged.
    /// </summary>
    public void ApplyEnchantmentTelemetry(DimensionalArtifact artifact, int totalSockets)
    {
        if (artifact == null) return;

        artifact.logicComplexity_0D += complexityPerSocket_0D;
        artifact.quantumStability_11D = Mathf.Clamp01(artifact.quantumStability_11D - stabilityPenaltyPerSocket_11D);
        artifact.inertialMass_4D += 2.5f; // Sockets add physical mass
        artifact.thermalEnthalpy_6D += 12f; // Forging heat flux
        
        // Re-evaluate 5D density matrix: rho = m / V
        float volume_3D = artifact.spatialGrid_1D_3D.x * artifact.spatialGrid_1D_3D.y * artifact.spatialGrid_1D_3D.z;
        artifact.densityMatrix_5D = artifact.inertialMass_4D / Mathf.Max(volume_3D, 0.001f);

        Debug.Log($"[12D ENCHANTMENT MATRIX] Socket #{totalSockets} applied to '{artifact.baseClassName_8D}'. " +
                  $"0D Complexity: {artifact.logicComplexity_0D:F1}, 11D Stability: {artifact.quantumStability_11D * 100f:F0}%");
    }
}

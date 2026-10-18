using System;
using System.Collections.Generic;
using UnityEngine;

public enum PhaseState { Solid, Liquid, Gas, Plasma }

[CreateAssetMenu(fileName = "NewDimensionalArtifact", menuName = "BattleSummoner/12D_Artifact")]
public class DimensionalArtifact : ScriptableObject
{
    [Header("--- VISIBLE UI LAYER (8D & 9D) ---")]
    [Tooltip("8D: Structural Knowledge - The visible Base Class Prefab/Icon fetched from The Hollow.")]
    public string baseClassName_8D = "Uncompiled_Construct";
    public GameObject visualPrefab_8D;
    public Sprite uiIcon_8D;

    [Tooltip("9D: Semantic Metadata - Visible lore/incantation that dynamically mutates hidden 0D-7D, 10D, 11D stats.")]
    [TextArea(4, 8)]
    public string semanticDescription_9D = "A standard construct.";

    // =====================================================================
    // HIDDEN BACKEND TELEMETRY (Obfuscated from standard Player HUD)
    // Only viewable via True Sight AR Lenses or Console Debugging
    // =====================================================================
    [HideInInspector] public float logicComplexity_0D;   // Math checksum & Singularity aggro
    [HideInInspector] public Vector3 spatialGrid_1D_3D;  // X (Reach/1D), Y (Spread/2D), Z (Volume/3D)
    [HideInInspector] public float inertialMass_4D;      // Kinetic momentum, knockback & stamina drain
    [HideInInspector] public float densityMatrix_5D;     // Structural rigidity (m / V) & Armor Pen
    [HideInInspector] public float thermalEnthalpy_6D;   // Internal heat flux (Q_gen)
    [HideInInspector] public PhaseState phaseState_6D;   // Current thermodynamic phase state
    [HideInInspector] public float temporalDecay_7D;     // Cooldown clock-cycles & Summon rot rate
    [HideInInspector] public float carrierFrequency_10D; // Harmonic Hz anchor & Gauntlet back-EMF torque
    [HideInInspector] public float quantumStability_11D; // 0.0 to 1.0 (Wavefunction integrity / API tolerance)

    public event Action OnTelemetryRecompiled;

    /// <summary>
    /// Runtime Compiler: Scans 9D Semantic Metadata for Lexical Hash Collisions and mutates hidden dimensions.
    /// </summary>
    public void Compile9DMetadata()
    {
        // 1. Reset to 8D Base Class defaults prior to 9D lexical injection
        logicComplexity_0D = 10f;
        spatialGrid_1D_3D = Vector3.one;
        inertialMass_4D = 10f;
        thermalEnthalpy_6D = 20f; // 20°C ambient baseline
        phaseState_6D = PhaseState.Solid;
        temporalDecay_7D = 1.0f;  // 1.0x standard entropic decay per tick
        carrierFrequency_10D = 60f; // 60Hz baseline hum
        quantumStability_11D = 1.0f; // 100% stable wavefunction

        if (string.IsNullOrEmpty(semanticDescription_9D)) return;
        string parsedText = semanticDescription_9D.ToLower();

        // 2. Lexical Hash Collision Checks (9D Semantic Injection)
        if (parsedText.Contains("heavy") || parsedText.Contains("immovable") || parsedText.Contains("inertia"))
        {
            inertialMass_4D *= 3.5f;
            carrierFrequency_10D -= 20f; // Drops carrier wave to 40Hz sub-bass growl
            logicComplexity_0D += 15f;
        }

        if (parsedText.Contains("spite") || parsedText.Contains("overclock") || parsedText.Contains("wrath"))
        {
            thermalEnthalpy_6D += 150f;
            phaseState_6D = PhaseState.Plasma;
            carrierFrequency_10D += 80f;
            quantumStability_11D -= 0.35f; // High risk of decoherence
            logicComplexity_0D += 45f;
        }

        if (parsedText.Contains("competition") || parsedText.Contains("spark") || parsedText.Contains("inspire"))
        {
            spatialGrid_1D_3D.x *= 1.75f; // +75% 1D Linear Reach
            carrierFrequency_10D = 432f;  // Harmonic resonance lock
            quantumStability_11D = Mathf.Min(1.0f, quantumStability_11D + 0.15f);
        }

        if (parsedText.Contains("disc") || parsedText.Contains("razor") || parsedText.Contains("compressed"))
        {
            // Compresses 3D Z-thickness down to 0.05 while expanding 2D radial width
            spatialGrid_1D_3D = new Vector3(1.5f, 1.5f, 0.05f);
            logicComplexity_0D += 30f;
        }

        if (parsedText.Contains("eternal") || parsedText.Contains("anchor") || parsedText.Contains("undying"))
        {
            temporalDecay_7D *= 0.2f; // Slows entropic rot by 80%
            logicComplexity_0D += 50f;
        }

        if (parsedText.Contains("singularity") || parsedText.Contains("void") || parsedText.Contains("nuke"))
        {
            // Forbidden World-Breaker hash collision
            logicComplexity_0D += 999f;
            inertialMass_4D *= 100f;
            spatialGrid_1D_3D = new Vector3(0.01f, 0.01f, 0.01f);
            quantumStability_11D = 0.01f;
        }

        // 3. Deterministic 5D Density Calculation: rho = m / V
        float volume_3D = spatialGrid_1D_3D.x * spatialGrid_1D_3D.y * spatialGrid_1D_3D.z;
        densityMatrix_5D = inertialMass_4D / Mathf.Max(volume_3D, 0.001f);

        // 4. Phase State Evaluation from 6D Thermal Enthalpy
        if (phaseState_6D != PhaseState.Plasma)
        {
            if (thermalEnthalpy_6D >= 120f) phaseState_6D = PhaseState.Plasma;
            else if (thermalEnthalpy_6D >= 80f) phaseState_6D = PhaseState.Gas;
            else if (thermalEnthalpy_6D >= 45f) phaseState_6D = PhaseState.Liquid;
            else phaseState_6D = PhaseState.Solid;
        }

        OnTelemetryRecompiled?.Invoke();

        Debug.Log($"[COMPILER 8D->9D]: '{baseClassName_8D}' compiled. " +
                  $"Hidden Telemetry -> 0D_Math: {logicComplexity_0D} | 4D_Mass: {inertialMass_4D} | " +
                  $"5D_Density: {densityMatrix_5D:F1} | 6D_Temp: {thermalEnthalpy_6D}C ({phaseState_6D}) | " +
                  $"10D_Freq: {carrierFrequency_10D}Hz | 11D_Stability: {quantumStability_11D * 100f:F0}%");
    }
}

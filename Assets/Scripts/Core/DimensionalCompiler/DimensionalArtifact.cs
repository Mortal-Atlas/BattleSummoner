using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Core.DimensionalCompiler
{
    public enum PhaseState { Solid, Liquid, Gas, Plasma }
    public enum DimensionalCollapseState { Stable3D, DegradingFlicker, Collapsed2DPaper }

    /// <summary>
    /// 12-Dimensional Crafting, Spell, Golem & Heirloom Telemetry Container (0D-11D).
    /// 8D & 9D are visible on the UI. 0D-7D, 10D, and 11D execute as the hidden backend physics engine.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDimensionalArtifact", menuName = "BattleSummoner/12D/Dimensional Artifact")]
    public class DimensionalArtifact : ScriptableObject
    {
        [Header("--- VISIBLE UI LAYER (8D & 9D) ---")]
        [Tooltip("8D: Structural Knowledge - Visible Base Class Prefab/Icon.")]
        public string baseClassName_8D = "Iron Broadsword";
        public GameObject visualPrefab_8D;
        public Sprite uiIcon_8D;

        [Tooltip("9D: Semantic Metadata - Visible lore/incantation string that mutates hidden 0D-11D physics.")]
        [TextArea(4, 8)]
        public string semanticDescription_9D = "A balanced Spire-forged blade tempered for kinetic slashing.";

        // =====================================================================
        // HIDDEN BACKEND TELEMETRY (0D-7D, 10D, 11D)
        // Only viewable via True Sight AR Lenses or Artificer Monocle
        // =====================================================================
        [HideInInspector] public float logicComplexity_0D;    // Math checksum & Singularity aggro
        [HideInInspector] public Vector3 spatialGrid_1D_3D;   // X (Reach), Y (Spread), Z (Volume)
        [HideInInspector] public float inertialMass_4D;       // Knockback, posture break & stamina drain
        [HideInInspector] public float densityMatrix_5D;      // Rigidity (m / V) & Armor Penetration
        [HideInInspector] public float thermalEnthalpy_6D;    // Heat generation (Celsius)
        [HideInInspector] public PhaseState phaseState_6D;    // Matter state (Solid/Liquid/Gas/Plasma)
        [HideInInspector] public float temporalDecay_7D;      // Buff duration & Summon lifespan decay rate
        [HideInInspector] public float carrierFrequency_10D;  // Acoustic Hz resonance (Kalimba/Lute/Horn)
        [HideInInspector] public float quantumStability_11D;  // 0.0 to 1.0 Wavefunction integrity
        [HideInInspector] public DimensionalCollapseState collapseState = DimensionalCollapseState.Stable3D;

        public event Action OnTelemetryRecompiled;

        /// <summary>
        /// Parses the 9D Semantic Description & Devoured Monster Traits to mutate hidden 0D-11D physics.
        /// </summary>
        public void Compile9DMetadata(bool hasTavernChorusWard = false)
        {
            spatialGrid_1D_3D = Vector3.one;
            logicComplexity_0D = 10f;
            inertialMass_4D = 12f;
            thermalEnthalpy_6D = 20f;
            phaseState_6D = PhaseState.Solid;
            temporalDecay_7D = 1.0f;
            carrierFrequency_10D = 432f; // Natural medieval lute/kalimba tuning baseline (Hz)
            quantumStability_11D = hasTavernChorusWard ? 1.25f : 1.0f;

            string parsed = string.IsNullOrEmpty(semanticDescription_9D)
                ? string.Empty
                : semanticDescription_9D.ToLowerInvariant();

            // 4D Mass & 5D Density Lexical Triggers
            if (parsed.Contains("heavy") || parsed.Contains("immovable") || parsed.Contains("behemoth") || parsed.Contains("iron"))
            {
                inertialMass_4D *= 3.25f;
                carrierFrequency_10D -= 110f;
                logicComplexity_0D += 15f;
            }

            // 1D-3D Compression / Pinched Air-Disc Triggers
            if (parsed.Contains("compressed") || parsed.Contains("pinched") || parsed.Contains("razor") || parsed.Contains("disc"))
            {
                spatialGrid_1D_3D = new Vector3(1.8f, 0.05f, 1.8f); // Ultra-thin high-density razor disc
                logicComplexity_0D += 25f;
            }

            // 6D Thermal Enthalpy & Industrial Magic Triggers (Release That Witch)
            if (parsed.Contains("spite") || parsed.Contains("magma") || parsed.Contains("furnace") || parsed.Contains("seared"))
            {
                thermalEnthalpy_6D += 480f;
                phaseState_6D = PhaseState.Plasma;
                quantumStability_11D -= 0.25f;
                logicComplexity_0D += 35f;
            }
            else if (parsed.Contains("steam") || parsed.Contains("pressurized"))
            {
                thermalEnthalpy_6D += 160f;
                phaseState_6D = PhaseState.Gas;
            }

            // 7D Temporal Anchor Triggers
            if (parsed.Contains("eternal") || parsed.Contains("anchor") || parsed.Contains("heirloom") || parsed.Contains("hearth"))
            {
                temporalDecay_7D *= 0.25f;
                logicComplexity_0D += 30f;
            }

            // Devoured Monster Part Triggers (Shield Hero / Exiled Heavy Knight)
            if (parsed.Contains("devoured"))
            {
                inertialMass_4D *= 1.35f;
                quantumStability_11D = Mathf.Min(1.5f, quantumStability_11D + 0.15f);
            }

            // Compute 5D Density Matrix: rho = m / V
            float volume = Mathf.Max(0.005f, spatialGrid_1D_3D.x * spatialGrid_1D_3D.y * spatialGrid_1D_3D.z);
            densityMatrix_5D = inertialMass_4D / volume;

            EvaluateDimensionalCollapseState();
            OnTelemetryRecompiled?.Invoke();
        }

        /// <summary>
        /// Evaluates whether 0D Logic Complexity has overwhelmed 11D Wavefunction Stability,
        /// triggering Reverse Plato's Cave collapse (3D Mesh -> 2D Paper Cutout).
        /// </summary>
        public DimensionalCollapseState EvaluateDimensionalCollapseState()
        {
            if (quantumStability_11D <= 0f || logicComplexity_0D > (quantumStability_11D * 180f))
            {
                collapseState = DimensionalCollapseState.Collapsed2DPaper;
            }
            else if (quantumStability_11D < 0.35f)
            {
                collapseState = DimensionalCollapseState.DegradingFlicker;
            }
            else
            {
                collapseState = DimensionalCollapseState.Stable3D;
            }
            return collapseState;
        }
    }
}

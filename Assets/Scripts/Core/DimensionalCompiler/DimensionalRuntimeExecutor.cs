using UnityEngine;
using BattleSummoner.Core.DimensionalCompiler;

[RequireComponent(typeof(AudioSource))]
public class DimensionalRuntimeExecutor : MonoBehaviour
{
    [Header("12D Artifact Binding")]
    public DimensionalArtifact artifactData;

    [Header("Subsystem Links")]
    public bool isGolemConstruct = false;
    public bool isSummonedFamiliar = false;
    public bool isActiveSpell = false;

    [Header("Runtime State")]
    [SerializeField] private float currentLifespanRemaining = 60f;
    [SerializeField] private float internalHeatAccumulator = 0f;
    [SerializeField] private bool isServoLocked = false;

    private AudioSource carrierWaveAudio;
    private InstrumentFreeplaySync activeMusicDeck;

    private void Awake()
    {
        carrierWaveAudio = GetComponent<AudioSource>();
        activeMusicDeck = FindFirstObjectByType<InstrumentFreeplaySync>();
    }

    private void OnEnable()
    {
        if (activeMusicDeck != null)
        {
            activeMusicDeck.OnNotePlayed += OnAcousticResonanceFrequencyReceived;
        }

        if (artifactData != null)
        {
            artifactData.Compile9DMetadata();
            ApplySpatialAndAudioTelemetry();
            ValidateQuantumWavefunction();
        }
    }

    private void OnDisable()
    {
        if (activeMusicDeck != null)
        {
            activeMusicDeck.OnNotePlayed -= OnAcousticResonanceFrequencyReceived;
        }
    }

    private void Update()
    {
        if (artifactData == null) return;

        // 1. 7D Temporal Vector (Entropic Decay for Summons & Buffs)
        if (isSummonedFamiliar)
        {
            currentLifespanRemaining -= artifactData.temporalDecay_7D * Time.deltaTime;
            if (currentLifespanRemaining <= 0f)
            {
                EvaporateIntoTheHollow();
            }
        }

        // 2. 6D Thermal Enthalpy & 5D Density Check for Golem Chassis
        if (isGolemConstruct && !isServoLocked)
        {
            EvaluateGolemThermodynamics();
        }
    }

    /// <summary>
    /// Applies 1D-3D Spatial Vectors to transform scale and tunes 10D Carrier Frequency audio pitch.
    /// </summary>
    private void ApplySpatialAndAudioTelemetry()
    {
        transform.localScale = artifactData.spatialGrid_1D_3D;

        if (carrierWaveAudio != null)
        {
            // Map 60Hz baseline to 1.0 pitch
            carrierWaveAudio.pitch = Mathf.Clamp(artifactData.carrierFrequency_10D / 60f, 0.25f, 3.0f);
        }
    }

    /// <summary>
    /// Evaluates 0D Logic Complexity against 11D Quantum Superposition Stability.
    /// Replaces static anti-nuke checks with dynamic Wavefunction Decoherence.
    /// </summary>
    public void ValidateQuantumWavefunction()
    {
        // Maximum safe complexity scales with 11D Quantum Stability
        float stabilityThreshold = artifactData.quantumStability_11D * 100f;

        if (artifactData.logicComplexity_0D > stabilityThreshold)
        {
            TriggerWavefunctionCollapse();
        }
    }

    /// <summary>
    /// Golemcraft Hardware Check: Chassis 3D Volume & 5D Density must support 6D Thermal & 10D Flux.
    /// </summary>
    private void EvaluateGolemThermodynamics()
    {
        float structuralThermalCapacity = artifactData.densityMatrix_5D * 4.5f;
        float heatLoad = artifactData.thermalEnthalpy_6D + (artifactData.carrierFrequency_10D * 0.25f);

        if (heatLoad > structuralThermalCapacity)
        {
            internalHeatAccumulator += (heatLoad - structuralThermalCapacity) * Time.deltaTime;

            if (internalHeatAccumulator > 100f)
            {
                isServoLocked = true;
                Debug.LogWarning($"[6D THERMAL RUNAWAY]: Golem '{artifactData.baseClassName_8D}' redlined! " +
                                 $"Low 5D Density ({artifactData.densityMatrix_5D:F1}) failed to contain 6D/10D flux. Servos locked, leaking black oil.");
            }
        }
    }

    /// <summary>
    /// Summoning Refresh Loop: Matching the summon's 10D Carrier Frequency on acoustic instruments (Kalimba/Lute/Horns) halts 7D Decay.
    /// </summary>
    public void OnAcousticResonanceFrequencyReceived(string noteName, float playedFrequencyHz)
    {
        if (!isSummonedFamiliar || artifactData == null) return;

        // If the played frequency is within +/- 15Hz (or a harmonic octave) of the 10D Carrier Frequency
        float harmonicDiff = Mathf.Abs(playedFrequencyHz - artifactData.carrierFrequency_10D);
        float subHarmonicDiff = Mathf.Abs((playedFrequencyHz * 0.5f) - artifactData.carrierFrequency_10D);

        if (harmonicDiff <= 15f || subHarmonicDiff <= 15f)
        {
            currentLifespanRemaining += 12f; // Emergency resonant refresh loop
            Debug.Log($"[10D RESONANCE LOCK]: Played {playedFrequencyHz}Hz matched '{artifactData.baseClassName_8D}' " +
                      $"10D Carrier Wave ({artifactData.carrierFrequency_10D}Hz). 7D Decay reversed!");
        }
    }

    public void TriggerWavefunctionCollapse()
    {
        Debug.LogError($"[11D DECOHERENCE]: '{artifactData.baseClassName_8D}' suffered wavefunction collapse! " +
                       $"0D Complexity ({artifactData.logicComplexity_0D}) exceeded 11D Stability ({artifactData.quantumStability_11D * 100f}%). " +
                       $"Tearing localized memory leak and spawning corrupted Absolute!");

        // Deal catastrophic backfire damage to nearby entities (including caster)
        Collider2D[] hitEntities2D = Physics2D.OverlapCircleAll(transform.position, 5.0f);
        if (hitEntities2D != null)
        {
            foreach (var hit in hitEntities2D)
            {
                if (hit != null && hit.TryGetComponent<ICombatEntity>(out var combatEntity))
                {
                    combatEntity.ApplyTrueDamage(9999f);
                }
            }
        }

        Collider[] hitEntities3D = Physics.OverlapSphere(transform.position, 5.0f);
        if (hitEntities3D != null)
        {
            foreach (var hit in hitEntities3D)
            {
                if (hit != null && hit.TryGetComponent<ICombatEntity>(out var combatEntity))
                {
                    combatEntity.ApplyTrueDamage(9999f);
                }
            }
        }

        // Unlock Secret Axiom silently via SystemBroadcastManager
        if (SystemBroadcastManager.Instance != null)
        {
            SystemBroadcastManager.Instance.DispatchBroadcast("Secret Axiom Unlocked: Wavefunction Decoherence", false);
        }

        Destroy(gameObject);
    }

    private void EvaporateIntoTheHollow()
    {
        Debug.Log($"[7D ENTROPIC DECAY]: Summon '{artifactData.baseClassName_8D}' depleted its 7D Temporal Vector and evaporated into The Hollow.");
        Destroy(gameObject);
    }
}

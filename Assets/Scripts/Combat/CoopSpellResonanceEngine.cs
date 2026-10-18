using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum ExtendedSpellElement
{
    None      = 0,
    Healing   = 1 << 0, // Heart Glyph
    Fire      = 1 << 1, // Triangle Glyph
    Rock      = 1 << 2, // Square Glyph
    Water     = 1 << 3, // Wavy Lines Glyph
    Wind      = 1 << 4, // Aero Catalyst
    Lightning = 1 << 5  // Plasma/Spark Catalyst
}

[Serializable]
public class ActiveCastIntent
{
    public string casterEntityId;
    public Vector3 originPosition;
    public Vector2 launchDirection;
    public ExtendedSpellElement element;
    public SpellGeometry geometry; // Sphere, PlanarDisc, DualFunnelThrust
    public float basePower;
    public float carrierFrequency10D;
    public float timestamp;
}

public class CoopSpellResonanceEngine : MonoBehaviour
{
    public static CoopSpellResonanceEngine Instance { get; private set; }

    [Header("Resonance Thresholds")]
    [SerializeField] private float resonanceRadius = 6.0f;
    [SerializeField] private float convergenceWindowSeconds = 1.5f;
    [SerializeField] private float perMagePowerBonus = 0.40f;

    private readonly List<ActiveCastIntent> pendingCasts = new List<ActiveCastIntent>();
    private static readonly Collider[] _overlapBuffer = new Collider[64];

    public static int QueryNearbyEntitiesNonAlloc(Vector3 center, float radius, Collider[] buffer, int layerMask = ~0)
    {
        return Physics.OverlapSphereNonAlloc(center, radius, buffer ?? _overlapBuffer, layerMask, QueryTriggerInteraction.Ignore);
    }

    public event Action<string, float, Vector3> OnComboSpellExecuted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterCastFromRuneCanvas(ActiveCastIntent newCast)
    {
        pendingCasts.RemoveAll(c => Time.time - c.timestamp > convergenceWindowSeconds);

        List<ActiveCastIntent> nearbyAllies = new List<ActiveCastIntent>();
        foreach (var existing in pendingCasts)
        {
            if (existing.casterEntityId != newCast.casterEntityId &&
                Vector3.Distance(existing.originPosition, newCast.originPosition) <= resonanceRadius)
            {
                nearbyAllies.Add(existing);
            }
        }

        if (nearbyAllies.Count == 0)
        {
            pendingCasts.Add(newCast);
            ExecuteStandardOrDelayedSoloCast(newCast);
            return;
        }

        // Consume matched ally casts and merge
        foreach (var ally in nearbyAllies) pendingCasts.Remove(ally);
        nearbyAllies.Add(newCast);

        ResolveMergedCast(nearbyAllies);
    }

    private void ResolveMergedCast(List<ActiveCastIntent> group)
    {
        ExtendedSpellElement combinedMask = ExtendedSpellElement.None;
        bool allSameElement = true;
        ExtendedSpellElement firstElem = group[0].element;
        float sumPower = 0f;
        float sumCarrier10D = 0f;
        Vector3 centroid = Vector3.zero;

        foreach (var cast in group)
        {
            combinedMask |= cast.element;
            if (cast.element != firstElem) allSameElement = false;
            sumPower += cast.basePower;
            sumCarrier10D += cast.carrierFrequency10D;
            centroid += cast.originPosition;
        }

        centroid /= group.Count;
        int mageCount = group.Count;
        float baseCombinedOutput = sumPower * (1f + perMagePowerBonus * (mageCount - 1));
        float avgCarrier10D = sumCarrier10D / mageCount;

        // 12D Telemetry Tie-In (TrueSightARDebuggerUI & DimensionalArtifact)
        // Combining casts averages the 10D Carrier Frequency and boosts 11D Quantum Stability by +15% per additional mage
        if (RuneShapeCastingEngine.Instance != null && RuneShapeCastingEngine.Instance.ActiveSpellArtifact != null)
        {
            var art = RuneShapeCastingEngine.Instance.ActiveSpellArtifact;
            art.carrierFrequency_10D = avgCarrier10D;
            art.quantumStability_11D = Mathf.Clamp01(art.quantumStability_11D + 0.15f * (mageCount - 1));

            var arUI = UnityEngine.Object.FindAnyObjectByType<TrueSightARDebuggerUI>();
            if (arUI != null)
            {
                arUI.InspectArtifact(art);
            }
        }

        if (allSameElement)
        {
            string megaName = $"Grand {firstElem} {group[0].geometry} [x{mageCount} Mage Resonance]";
            Debug.Log($"[Co-Op Resonance] {megaName} | Output: {baseCombinedOutput:F1} | 10D f: {avgCarrier10D:F1}Hz | 11D Psi boost: +{15 * (mageCount - 1)}%");
            OnComboSpellExecuted?.Invoke(megaName, baseCombinedOutput, centroid);
        }
        else
        {
            var synergy = SpellSynergyMatrix.EvaluateSynergy(combinedMask, baseCombinedOutput, mageCount);
            Debug.Log($"[Elemental Synergy] {synergy.synergyName} | Output: {synergy.finalPower:F1} | {synergy.description}");

            if (synergy.isAegisWard)
            {
                SpellSynergyMatrix.ApplyAegisPartyCleanse();
            }

            OnComboSpellExecuted?.Invoke(synergy.synergyName, synergy.finalPower, centroid);
        }
    }

    private void ExecuteStandardOrDelayedSoloCast(ActiveCastIntent soloCast)
    {
        OnComboSpellExecuted?.Invoke($"Solo {soloCast.element} ({soloCast.geometry})", soloCast.basePower, soloCast.originPosition);
    }
}

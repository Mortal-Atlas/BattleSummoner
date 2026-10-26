using UnityEngine;
using TMPro;
using BattleSummoner.Core.DimensionalCompiler;

public class TrueSightARDebuggerUI : MonoBehaviour
{
    [Header("True Sight AR Overlay State")]
    public bool areTrueSightLensesEquipped = false;
    [SerializeField] private GameObject arWireframePanel;

    [Header("8D & 9D Public UI Elements")]
    [SerializeField] private TextMeshProUGUI publicTitle_8D;
    [SerializeField] private TextMeshProUGUI publicLore_9D;

    [Header("Obfuscated Telemetry Readouts (0D-7D, 10D, 11D)")]
    [SerializeField] private TextMeshProUGUI telemetryReadoutText;

    private DimensionalArtifact currentInspectedArtifact;

    private void Start()
    {
        if (arWireframePanel != null)
        {
            arWireframePanel.SetActive(areTrueSightLensesEquipped);
        }
    }

    public void ToggleTrueSightLenses()
    {
        areTrueSightLensesEquipped = !areTrueSightLensesEquipped;
        if (arWireframePanel != null)
        {
            arWireframePanel.SetActive(areTrueSightLensesEquipped);
        }
        RefreshInspectionView();
    }

    public void InspectArtifact(DimensionalArtifact artifact)
    {
        currentInspectedArtifact = artifact;
        RefreshInspectionView();
    }

    public void RefreshInspectionView()
    {
        if (currentInspectedArtifact == null) return;

        // Always display 8D and 9D to standard player HUD
        if (publicTitle_8D != null) publicTitle_8D.text = currentInspectedArtifact.baseClassName_8D;
        if (publicLore_9D != null) publicLore_9D.text = currentInspectedArtifact.semanticDescription_9D;

        // Only populate 0D-7D, 10D, 11D if True Sight AR Lenses are active
        if (areTrueSightLensesEquipped && telemetryReadoutText != null)
        {
            Vector3 s = currentInspectedArtifact.spatialGrid_1D_3D;
            telemetryReadoutText.text =
                $"<b>[TRUE SIGHT 12D TELEMETRY OVERLAY]</b>\n" +
                $"<color=#00FFCC>0D Logic Substrate:</color> {currentInspectedArtifact.logicComplexity_0D:F1} Checksum\n" +
                $"<color=#00FFCC>1D Reach (x):</color> {s.x:F2}m | <color=#00FFCC>2D Spread (y):</color> {s.y:F2}m\n" +
                $"<color=#00FFCC>3D Volume (z):</color> {s.z:F2}m³\n" +
                $"<color=#FFAA00>4D Inertial Mass (m):</color> {currentInspectedArtifact.inertialMass_4D:F1} kg\n" +
                $"<color=#FFAA00>5D Density Matrix (ρ):</color> {currentInspectedArtifact.densityMatrix_5D:F2} kg/m³\n" +
                $"<color=#FF4444>6D Thermal Enthalpy (T):</color> {currentInspectedArtifact.thermalEnthalpy_6D:F1}°C [{currentInspectedArtifact.phaseState_6D}]\n" +
                $"<color=#AAAAFF>7D Temporal Vector (t):</color> {currentInspectedArtifact.temporalDecay_7D:F2}x Tick Decay\n" +
                $"<color=#FF00FF>10D Carrier Wave (f/B):</color> {currentInspectedArtifact.carrierFrequency_10D:F1} Hz\n" +
                $"<color=#00FF00>11D Quantum Stability (Ψ):</color> {currentInspectedArtifact.quantumStability_11D * 100f:F1}%";
        }
    }
}

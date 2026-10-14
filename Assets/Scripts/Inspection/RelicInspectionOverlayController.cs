using UnityEngine;
using UnityEngine.UI;

public class RelicInspectionOverlayController : MonoBehaviour
{
    [Header("Controller Links")]
    [SerializeField] private RelicInspectorController inspectorController;
    [SerializeField] private PortraitGestureCanvas gestureCanvas;
    [SerializeField] private GameObject cryptexPrefab;

    [Header("UI Controls")]
    [SerializeField] private Button closeInspectionBtn;
    [SerializeField] private Button switchModeBtn;
    [SerializeField] private Button solveTestBtn;
    [SerializeField] private Text modeIndicatorText;
    [SerializeField] private Text guidePromptText;
    [SerializeField] private Text puzzleStatusText;

    private PuzzleCryptex activeCryptex;

    private void Awake()
    {
        if (inspectorController == null) inspectorController = FindAnyObjectByType<RelicInspectorController>();
        if (gestureCanvas == null) gestureCanvas = FindAnyObjectByType<PortraitGestureCanvas>();

        if (closeInspectionBtn != null)
        {
            closeInspectionBtn.onClick.AddListener(CloseInspection);
        }

        if (switchModeBtn != null)
        {
            switchModeBtn.onClick.AddListener(CycleInteractionMode);
        }

        if (solveTestBtn != null)
        {
            solveTestBtn.onClick.AddListener(AutoSolveForTesting);
        }
    }

    private void OnEnable()
    {
        if (gestureCanvas != null)
        {
            gestureCanvas.currentMode = InteractionMode.RelicInspection;
        }

        if (inspectorController != null && cryptexPrefab != null)
        {
            inspectorController.OpenInspection(cryptexPrefab);
        }

        HookActiveCryptex();
        UpdateModeUI();
        UpdatePuzzleStatusUI();
    }

    private void OnDisable()
    {
        UnhookActiveCryptex();

        if (gestureCanvas != null)
        {
            gestureCanvas.currentMode = InteractionMode.Combat;
        }

        if (inspectorController != null)
        {
            inspectorController.CloseInspection();
        }
    }

    public void HookActiveCryptex()
    {
        if (activeCryptex == null) activeCryptex = FindAnyObjectByType<PuzzleCryptex>();
        if (activeCryptex != null)
        {
            activeCryptex.OnCryptexUnlocked += HandleCryptexUnlocked;
        }
    }

    private void UnhookActiveCryptex()
    {
        if (activeCryptex != null)
        {
            activeCryptex.OnCryptexUnlocked -= HandleCryptexUnlocked;
        }
    }

    private void HandleCryptexUnlocked()
    {
        UpdatePuzzleStatusUI();
    }

    public void AutoSolveForTesting()
    {
        if (activeCryptex == null) activeCryptex = FindAnyObjectByType<PuzzleCryptex>();
        if (activeCryptex != null)
        {
            // Rotate all rings to their target solution steps
            var rings = activeCryptex.GetComponentsInChildren<CryptexPuzzlePiece>();
            foreach (var r in rings)
            {
                // Step to target
                int needed = (r.TotalSteps + 10) % r.TotalSteps;
                for (int i = 0; i < 6; i++)
                {
                    if (r.IsSolved) break;
                    r.OnPuzzleTapped();
                }
            }

            var bolt = activeCryptex.GetComponentInChildren<CryptexTumblerBolt>();
            if (bolt != null && !bolt.IsUnlocked)
            {
                bolt.OnPuzzleTapped();
            }

            activeCryptex.CheckSolutionState();
            UpdatePuzzleStatusUI();
        }
    }

    public void UpdatePuzzleStatusUI()
    {
        if (puzzleStatusText == null) return;
        if (activeCryptex == null) activeCryptex = FindAnyObjectByType<PuzzleCryptex>();

        if (activeCryptex == null)
        {
            puzzleStatusText.text = "[REPLICATING ANCIENT VAULT SCHEMATICS...]";
            return;
        }

        if (activeCryptex.IsFullyUnlocked)
        {
            puzzleStatusText.text = "★ VAULT UNLOCKED: ASTRAL GEM ACTIVE (ASMR CHIME) ★";
            puzzleStatusText.color = new Color(0.1f, 1f, 0.8f, 1f);
            return;
        }

        var rings = activeCryptex.GetComponentsInChildren<CryptexPuzzlePiece>();
        int solvedCount = 0;
        foreach (var r in rings) if (r.IsSolved) solvedCount++;

        var bolt = activeCryptex.GetComponentInChildren<CryptexTumblerBolt>();
        bool boltOpen = (bolt != null && bolt.IsUnlocked);

        if (solvedCount == rings.Length)
        {
            puzzleStatusText.text = "★ ALL RINGS ALIGNED! PULL TUMBLER BOLT TO OPEN ★";
            puzzleStatusText.color = new Color(1f, 0.9f, 0.3f, 1f);
        }
        else
        {
            puzzleStatusText.text = $"RINGS ALIGNED: {solvedCount}/{rings.Length} | BOLT: {(boltOpen ? "OPEN" : "LOCKED")}";
            puzzleStatusText.color = new Color(0.8f, 0.85f, 0.95f, 0.95f);
        }
    }

    public void CloseInspection()
    {
        gameObject.SetActive(false);
    }

    private void CycleInteractionMode()
    {
        if (gestureCanvas == null) return;

        switch (gestureCanvas.currentMode)
        {
            case InteractionMode.Combat:
                gestureCanvas.currentMode = InteractionMode.Spellcraft;
                break;
            case InteractionMode.Spellcraft:
                gestureCanvas.currentMode = InteractionMode.BlacksmithForging;
                break;
            case InteractionMode.BlacksmithForging:
                gestureCanvas.currentMode = InteractionMode.RelicInspection;
                break;
            case InteractionMode.RelicInspection:
            default:
                gestureCanvas.currentMode = InteractionMode.Combat;
                break;
        }

        UpdateModeUI();
    }

    private void UpdateModeUI()
    {
        if (gestureCanvas == null) return;

        if (modeIndicatorText != null)
        {
            modeIndicatorText.text = $"MODE: {gestureCanvas.currentMode.ToString().ToUpper()}";
        }

        if (guidePromptText != null)
        {
            switch (gestureCanvas.currentMode)
            {
                case InteractionMode.RelicInspection:
                    guidePromptText.text = "Drag: 3D Orbit | Pinch: Zoom | Tap: Align Cipher Rings";
                    break;
                case InteractionMode.Combat:
                    guidePromptText.text = "Fast Swipe: Blade Slash Dash | Lower-Left: Locomotion";
                    break;
                case InteractionMode.Spellcraft:
                    guidePromptText.text = "Draw Circle: Priming Sigil | Inward Pinch: Slicing Gale";
                    break;
                case InteractionMode.BlacksmithForging:
                    guidePromptText.text = "Center Tap: Hammer Strike | Beware Detonation on 3rd Strike";
                    break;
            }
        }
    }
}

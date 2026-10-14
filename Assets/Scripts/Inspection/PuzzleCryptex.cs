using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// PuzzleCryptex manages the 3D rotating cipher rings, slide mechanisms,
/// hollow acoustic resonance inspection, and needle trap disarming.
/// Orchestrates tactile ASMR sound effects, pneumatic chamber opening, and crystal revelation.
/// </summary>
public class PuzzleCryptex : MonoBehaviour, IInspectablePuzzlePiece
{
    [Header("Cryptex Mechanisms")]
    [SerializeField] private CryptexPuzzlePiece[] cipherRings;
    [SerializeField] private CryptexTumblerBolt tumblerBolt;
    [SerializeField] private Transform coreChamber;
    [SerializeField] private CryptexGemCore gemCore;
    [SerializeField] private Vector3 coreUnlockedOffset = new Vector3(0f, 0f, 0.75f);

    [Header("Trap & Disarm State")]
    [SerializeField] private bool isRiggedWithNeedleTrap = true;
    [SerializeField] private bool isNeedleTrapDisarmed = false;

    public event Action OnCryptexUnlocked;
    public event Action<bool> OnNeedleTrapTriggered;

    private bool isFullyUnlocked = false;
    private Vector3 initialCorePos;
    private Coroutine unlockCoroutine;

    public bool IsFullyUnlocked => isFullyUnlocked;
    public bool IsNeedleTrapDisarmed => isNeedleTrapDisarmed;
    public bool IsRiggedWithNeedleTrap => isRiggedWithNeedleTrap;

    private void Awake()
    {
        if (cipherRings == null || cipherRings.Length == 0)
        {
            cipherRings = GetComponentsInChildren<CryptexPuzzlePiece>();
        }
        if (tumblerBolt == null)
        {
            tumblerBolt = GetComponentInChildren<CryptexTumblerBolt>();
        }
        if (gemCore == null)
        {
            gemCore = GetComponentInChildren<CryptexGemCore>();
        }
        if (coreChamber != null)
        {
            initialCorePos = coreChamber.localPosition;
        }
    }

    private void OnEnable()
    {
        if (cipherRings != null)
        {
            for (int i = 0; i < cipherRings.Length; i++)
            {
                cipherRings[i].OnDialRotated += HandleDialRotated;
            }
        }

        if (tumblerBolt != null)
        {
            tumblerBolt.OnTumblerStateChanged += HandleTumblerChanged;
        }
    }

    private void OnDisable()
    {
        if (cipherRings != null)
        {
            for (int i = 0; i < cipherRings.Length; i++)
            {
                cipherRings[i].OnDialRotated -= HandleDialRotated;
            }
        }

        if (tumblerBolt != null)
        {
            tumblerBolt.OnTumblerStateChanged -= HandleTumblerChanged;
        }
    }

    public void OnPuzzleInteracted()
    {
        OnPuzzleTapped();
    }

    public void OnPuzzleTapped()
    {
        // Tapping the outer chassis produces hollow acoustic reverberation
        TapSurfaceForHollowAcoustics();
    }

    public void OnPuzzleSlide(Vector2 screenDelta)
    {
    }

    public void OnPuzzleSlideEnd()
    {
    }

    /// <summary>
    /// Tap on cryptex casing to listen for hollow resonance.
    /// ASMR acoustic hollow knock plays.
    /// </summary>
    public bool TapSurfaceForHollowAcoustics()
    {
        RelicASMRSynthesizer.Instance?.PlayHollowKnock(1.0f, UnityEngine.Random.Range(0.96f, 1.04f));

        if (isRiggedWithNeedleTrap && !isNeedleTrapDisarmed)
        {
            Debug.LogWarning("[CRYPTEX INSPECTION] *Hollow Wood/Metal Clack* — Internal chamber resonates hollow. 90% probability of spring-loaded poison needles!");
            return true;
        }

        Debug.Log("[CRYPTEX INSPECTION] Solid reverberation. Internal mechanism stable.");
        return false;
    }

    private void HandleDialRotated(int step, bool ringSolved)
    {
        CheckSolutionState();
    }

    private void HandleTumblerChanged(bool isUnlocked)
    {
        CheckSolutionState();
    }

    public void CheckSolutionState()
    {
        if (isFullyUnlocked) return;

        bool allRingsSolved = true;
        if (cipherRings != null && cipherRings.Length > 0)
        {
            for (int i = 0; i < cipherRings.Length; i++)
            {
                if (!cipherRings[i].IsSolved)
                {
                    allRingsSolved = false;
                    break;
                }
            }
        }

        bool tumblerOpen = (tumblerBolt == null) || tumblerBolt.IsUnlocked;

        if (allRingsSolved && tumblerOpen)
        {
            ExecuteUnlock();
        }
        else if (allRingsSolved && !tumblerOpen)
        {
            Debug.Log("[CRYPTEX MECHANISM] Cipher rings aligned! Slide the tumbler bolt to release the chamber latch.");
            RelicASMRSynthesizer.Instance?.PlayClick(1.0f, 1.25f);
        }
    }

    private void ExecuteUnlock()
    {
        if (isFullyUnlocked) return;
        isFullyUnlocked = true;
        isNeedleTrapDisarmed = true;

        Debug.Log("[CRYPTEX RELIC] Cipher matrix solved and latch disengaged! Vault chamber opening.");

        if (unlockCoroutine != null) StopCoroutine(unlockCoroutine);
        unlockCoroutine = StartCoroutine(AnimateChamberOpeningRoutine());

        OnCryptexUnlocked?.Invoke();
    }

    private IEnumerator AnimateChamberOpeningRoutine()
    {
        // Stage 1: Mechanical latch release
        RelicASMRSynthesizer.Instance?.PlayLatch(1.0f, 1.0f);
        yield return new WaitForSeconds(0.12f);

        // Stage 2: Pneumatic decompression hiss
        RelicASMRSynthesizer.Instance?.PlayPneumaticHiss(1.0f);

        // Stage 3: Smooth sliding expansion of the inner core chamber
        if (coreChamber != null)
        {
            Vector3 startPos = coreChamber.localPosition;
            Vector3 targetPos = initialCorePos + coreUnlockedOffset;
            float duration = 0.85f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                coreChamber.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            coreChamber.localPosition = targetPos;
        }

        // Stage 4: Celestial crystal harmonic chime
        yield return new WaitForSeconds(0.15f);
        RelicASMRSynthesizer.Instance?.PlayCrystalChime(1.0f, 1.0f);
        if (gemCore != null)
        {
            gemCore.OnPuzzleTapped();
        }
    }
}

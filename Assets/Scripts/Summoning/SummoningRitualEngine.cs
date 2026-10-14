using UnityEngine;

/// <summary>
/// SummoningRitualEngine (Phase 10.1) provides the architectural facade and engine entry point
/// for Patronus-Style Human Summoning Rituals, coordinating seed nomenclature, LLM questionnaires,
/// and geometric unistroke magic circle accuracy calculations.
/// </summary>
public class SummoningRitualEngine : MonoBehaviour
{
    public static SummoningRitualEngine Instance { get; private set; }

    public SummoningRitualManager Manager
    {
        get
        {
            if (SummoningRitualManager.Instance != null) return SummoningRitualManager.Instance;
            return GetComponent<SummoningRitualManager>() ?? FindAnyObjectByType<SummoningRitualManager>();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Opens the Patronus-Style Summoning ritual modal and initializes Step 1.
    /// </summary>
    public void OpenRitual()
    {
        Manager?.OpenSummoningRitualModal();
    }

    /// <summary>
    /// Closes the modal and restores interaction state to combat.
    /// </summary>
    public void CloseRitual()
    {
        Manager?.CloseRitualModal();
    }

    /// <summary>
    /// Evaluates whether the ritual modal is currently actively displayed.
    /// </summary>
    public bool IsRitualActive()
    {
        var mgr = Manager;
        return mgr != null && mgr.CurrentStep != RitualStep.Closed;
    }
}

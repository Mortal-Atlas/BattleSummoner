using UnityEngine;

/// <summary>
/// QuarantineManager coordinates the Phantom Spire quarantine partition sandbox.
/// </summary>
public class QuarantineManager : MonoBehaviour
{
    public static QuarantineManager Instance { get; private set; }

    public bool IsPlayerQuarantined => TribunalAppealController.QuarantineStatus == 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void EnforceQuarantineRouting()
    {
        if (IsPlayerQuarantined)
        {
            Debug.LogWarning("[QuarantineManager] Player routed to the Phantom Spire sandbox.");
        }
    }
}

using System;
using UnityEngine;

/// <summary>
/// InteractivePuzzleEngine coordinates tactile cryptex tumbler alignment and relic solution verification.
/// </summary>
public class InteractivePuzzleEngine : MonoBehaviour
{
    public static InteractivePuzzleEngine Instance { get; private set; }

    public event Action<bool> OnPuzzleStateChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void VerifySolution(bool isSolved)
    {
        OnPuzzleStateChanged?.Invoke(isSolved);
        if (isSolved)
        {
            Debug.Log("[Puzzle Engine] Tactile Cryptex successfully solved!");
        }
    }
}

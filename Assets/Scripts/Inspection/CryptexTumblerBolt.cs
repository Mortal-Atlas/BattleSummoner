using System;
using UnityEngine;

/// <summary>
/// CryptexTumblerBolt represents an interactable sliding locking pin mounted along the side rail.
/// Players can slide it back and forth with a finger or tap it to toggle its lock state.
/// When unlocked, it allows the cryptex chamber to release once cipher rings are aligned.
/// </summary>
public class CryptexTumblerBolt : MonoBehaviour, IInspectablePuzzlePiece
{
    [Header("Tumbler Configuration")]
    [SerializeField] private Vector3 slideAxis = Vector3.forward;
    [SerializeField] private float slideDistance = 0.32f;
    [SerializeField] private float smoothSpeed = 16f;
    [SerializeField] private float slideSensitivity = 0.003f;
    [SerializeField] private bool isUnlocked = false;

    public event Action<bool> OnTumblerStateChanged;

    private Vector3 initialLocalPos;
    private Vector3 targetLocalPos;
    private float currentProgress = 0f; // 0 = locked, 1 = unlocked
    private bool isBeingDragged = false;

    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        currentProgress = isUnlocked ? 1f : 0f;
        targetLocalPos = initialLocalPos + slideAxis * (currentProgress * slideDistance);
        transform.localPosition = targetLocalPos;
    }

    private void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * smoothSpeed);
    }

    public void OnPuzzleInteracted()
    {
        OnPuzzleTapped();
    }

    public void OnPuzzleTapped()
    {
        SetUnlockedState(!isUnlocked);
    }

    public void OnPuzzleSlide(Vector2 screenDelta)
    {
        isBeingDragged = true;

        // Slide along drag delta
        float delta = (screenDelta.y + screenDelta.x) * slideSensitivity;
        currentProgress = Mathf.Clamp01(currentProgress + delta);
        targetLocalPos = initialLocalPos + slideAxis * (currentProgress * slideDistance);

        // Subtle metallic sliding friction sound
        if (Mathf.Abs(delta) > 0.01f)
        {
            RelicASMRSynthesizer.Instance?.PlaySlide(0.4f, 1.0f + currentProgress * 0.2f);
        }
    }

    public void OnPuzzleSlideEnd()
    {
        if (!isBeingDragged) return;
        isBeingDragged = false;

        // Snap to nearest state: 0 (Locked) or 1 (Unlocked)
        bool shouldUnlock = currentProgress >= 0.5f;
        SetUnlockedState(shouldUnlock);
    }

    public void SetUnlockedState(bool unlocked)
    {
        isUnlocked = unlocked;
        currentProgress = isUnlocked ? 1f : 0f;
        targetLocalPos = initialLocalPos + slideAxis * (currentProgress * slideDistance);

        if (isUnlocked)
        {
            RelicASMRSynthesizer.Instance?.PlaySlide(0.7f, 1.1f);
            RelicASMRSynthesizer.Instance?.PlayLatch(1.0f, 1.0f);
            Debug.Log("[CRYPTEX TUMBLER] Tumbler pin retracted: UNLOCKED.");
        }
        else
        {
            RelicASMRSynthesizer.Instance?.PlaySlide(0.7f, 0.95f);
            RelicASMRSynthesizer.Instance?.PlayLatch(0.9f, 0.85f);
            Debug.Log("[CRYPTEX TUMBLER] Tumbler pin engaged: LOCKED.");
        }

        OnTumblerStateChanged?.Invoke(isUnlocked);
    }
}

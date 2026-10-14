using System;
using UnityEngine;

/// <summary>
/// CryptexPuzzlePiece represents an interactable rotating cipher ring.
/// It responds to both single-finger taps (stepping +1 notch with an ASMR ratchet click)
/// and direct finger dragging/sliding (continuous tactile rotation with real-time gear teeth clicks).
/// </summary>
public class CryptexPuzzlePiece : MonoBehaviour, IInspectablePuzzlePiece
{
    [Header("Cipher Ring Configuration")]
    [SerializeField] private int totalSteps = 6;
    [SerializeField] private int currentStep = 0;
    [SerializeField] private int targetSolutionStep = 3;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private float rotationAnglePerStep = 60f;
    [SerializeField] private float smoothSpeed = 14f;
    [SerializeField] private float slideSensitivity = 0.45f;

    public event Action<int, bool> OnDialRotated;

    private Quaternion targetRotation;
    private float currentContinuousAngle = 0f;
    private float lastClickAngle = 0f;
    private bool isBeingDragged = false;
    private bool isSolved = false;

    public bool IsSolved => isSolved;
    public int CurrentStep => currentStep;
    public int TotalSteps => totalSteps;

    private void Awake()
    {
        currentContinuousAngle = currentStep * rotationAnglePerStep;
        lastClickAngle = currentContinuousAngle;
        targetRotation = Quaternion.Euler(rotationAxis * currentContinuousAngle);
        transform.localRotation = targetRotation;
        EvaluateSolution();
    }

    private void Update()
    {
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    public void OnPuzzleInteracted()
    {
        OnPuzzleTapped();
    }

    public void OnPuzzleTapped()
    {
        currentStep = (currentStep + 1) % totalSteps;
        currentContinuousAngle = currentStep * rotationAnglePerStep;
        lastClickAngle = currentContinuousAngle;
        targetRotation = Quaternion.Euler(rotationAxis * currentContinuousAngle);

        EvaluateSolution();

        // ASMR Ratchet Click with subtle pitch shift per step
        float pitch = 0.95f + (currentStep * 0.04f);
        RelicASMRSynthesizer.Instance?.PlayClick(1.0f, pitch);

        OnDialRotated?.Invoke(currentStep, isSolved);
        Debug.Log($"[CRYPTEX DIAL TAP] {gameObject.name} rotated to notch {currentStep}/{totalSteps} (Solved: {isSolved})");
    }

    public void OnPuzzleSlide(Vector2 screenDelta)
    {
        isBeingDragged = true;

        // Calculate angular delta based on screen drag movement
        float deltaAngle = (screenDelta.x - screenDelta.y) * slideSensitivity;
        currentContinuousAngle += deltaAngle;
        targetRotation = Quaternion.Euler(rotationAxis * currentContinuousAngle);

        // Tactile gear tooth clicking when passing step intervals
        if (Mathf.Abs(currentContinuousAngle - lastClickAngle) >= rotationAnglePerStep)
        {
            lastClickAngle = currentContinuousAngle;
            RelicASMRSynthesizer.Instance?.PlayClick(0.75f, UnityEngine.Random.Range(0.98f, 1.05f));
        }
    }

    public void OnPuzzleSlideEnd()
    {
        if (!isBeingDragged) return;
        isBeingDragged = false;

        // Snap to the nearest notch
        int rawStep = Mathf.RoundToInt(currentContinuousAngle / rotationAnglePerStep);
        int normalizedStep = rawStep % totalSteps;
        if (normalizedStep < 0) normalizedStep += totalSteps;

        currentStep = normalizedStep;
        currentContinuousAngle = rawStep * rotationAnglePerStep;
        lastClickAngle = currentContinuousAngle;
        targetRotation = Quaternion.Euler(rotationAxis * currentContinuousAngle);

        // Final satisfying mechanical latch click
        RelicASMRSynthesizer.Instance?.PlayClick(1.0f, 1.08f);

        EvaluateSolution();
        OnDialRotated?.Invoke(currentStep, isSolved);
        Debug.Log($"[CRYPTEX DIAL SLIDE END] {gameObject.name} snapped to notch {currentStep} (Solved: {isSolved})");
    }

    private void EvaluateSolution()
    {
        isSolved = (currentStep == targetSolutionStep);
    }
}

using System;
using UnityEngine;

/// <summary>
/// InstrumentCatalyst is a mobile weapon archetype in BattleSummoner.
/// Equipping an instrument projects the Kalimba UI over the gesture canvas.
/// Combat Mobility: Playing notes does not root the player, retaining 80%
/// movement vectoring while tapping out melodies.
/// </summary>
public class InstrumentCatalyst : MonoBehaviour
{
    public static InstrumentCatalyst Instance { get; private set; }

    [Header("Instrument Identity")]
    public string instrumentId = "weapon_astral_kalimba";
    public string instrumentName = "Astral Celestiphone";
    public bool isEquippedOnSpawn = true;

    [Header("Mobility Attributes")]
    [Tooltip("Movement vectoring retention (0.80 = 80% speed while actively playing notes).")]
    public float activePlayingMobilityRetention = 0.80f;

    public static bool IsInstrumentEquipped { get; private set; } = false;
    public static bool IsActivelyPlayingMelody => (Time.time - lastNoteTappedTime) < 1.2f;

    private static float lastNoteTappedTime = -10f;

    public event Action<bool> OnInstrumentEquipToggled;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (isEquippedOnSpawn)
        {
            EquipInstrument();
        }
    }

    private void OnEnable()
    {
        MelodyRecognizer.OnNotePushedToBuffer += HandleNoteTappedForMobility;
    }

    private void OnDisable()
    {
        MelodyRecognizer.OnNotePushedToBuffer -= HandleNoteTappedForMobility;
    }

    private void HandleNoteTappedForMobility(string note)
    {
        lastNoteTappedTime = Time.time;
    }

    public void EquipInstrument()
    {
        IsInstrumentEquipped = true;

        if (PortraitInstrumentUI.Instance != null)
        {
            PortraitInstrumentUI.Instance.SetVisible(true);
        }

        PortraitGestureCanvas canvas = FindAnyObjectByType<PortraitGestureCanvas>();
        if (canvas != null)
        {
            canvas.currentMode = InteractionMode.InstrumentKalimba;
        }

        Debug.Log($"[INSTRUMENT CATALYST] Equipped {instrumentName}. Kalimba V-Shape overlay active. 80% mobility enabled.");
        OnInstrumentEquipToggled?.Invoke(true);
    }

    public void UnequipInstrument()
    {
        IsInstrumentEquipped = false;

        if (PortraitInstrumentUI.Instance != null)
        {
            PortraitInstrumentUI.Instance.SetVisible(false);
        }

        PortraitGestureCanvas canvas = FindAnyObjectByType<PortraitGestureCanvas>();
        if (canvas != null)
        {
            canvas.currentMode = InteractionMode.Combat;
        }

        Debug.Log($"[INSTRUMENT CATALYST] Unequipped {instrumentName}. Swapped back to pure swipe combat canvas.");
        OnInstrumentEquipToggled?.Invoke(false);
    }

    public void ToggleInstrument()
    {
        if (IsInstrumentEquipped) UnequipInstrument();
        else EquipInstrument();
    }
}

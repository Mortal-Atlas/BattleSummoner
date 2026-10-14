using System;
using System.Collections.Generic;
using UnityEngine;

public enum BardicBuffPayloadType
{
    HymnOfTheSpire_DamageBuff,     // Party +25% damage boost & radiant AoE
    SongOfFortitude_Shield,        // 150 HP kinetic barrier shield & stagger immunity
    AriaOfInferno_FireBurst,       // 360 AoE 450 fire damage detonation
    SonataOfSwiftness_Speed,       // +40% movement vectoring & reduced cooldowns
    MargosLullaby_Stun             // 4s void slumber / stun on non-boss foes
}

/// <summary>
/// Discoverable lore books and grimoires. Reading a book adds its sequence
/// to the player's repertoire, overlaying faint glowing dots on the Kalimba UI
/// to guide thumb positioning.
/// </summary>
[CreateAssetMenu(fileName = "SheetMusic_New", menuName = "BattleSummoner/Sheet Music Lore Grimoire")]
public class SheetMusicItem : ScriptableObject
{
    [Header("Grimoire Lore")]
    public string sheetId = "sheet_hymn_spire";
    public string songTitle = "Hymn of the Spire";
    [TextArea(2, 4)]
    public string loreDescription = "Ancient astral verse scribed upon the Spire ramparts. Playing these notes summons a resonant aura of damage amplification.";

    [Header("Melody Note Sequence (4 - 8 notes)")]
    public string[] noteSequence = new string[] { "C4", "E4", "G4", "C5" };

    [Header("Bardic Payload Payload")]
    public BardicBuffPayloadType buffPayload = BardicBuffPayloadType.HymnOfTheSpire_DamageBuff;
    public float payloadMagnitude = 0.25f;
    public float durationSeconds = 12f;
    public float aoeRadius = 8f;

    [Header("Discovery State")]
    public bool isDiscovered = true;

    /// <summary>
    /// Checks if the tail of the rolling buffer matches this sequence.
    /// </summary>
    public bool MatchesTail(List<string> notesBuffer)
    {
        if (notesBuffer == null || noteSequence == null || noteSequence.Length == 0) return false;
        if (notesBuffer.Count < noteSequence.Length) return false;

        int startIndex = notesBuffer.Count - noteSequence.Length;
        for (int i = 0; i < noteSequence.Length; i++)
        {
            if (!string.Equals(notesBuffer[startIndex + i], noteSequence[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpellbookItem represents an arcane grimoire or lore book that spellcasters (Mages)
/// and Bards can equip to assist with spellcasting and musical performance.
/// </summary>
[System.Serializable]
public class GrimoireSpellEntry
{
    public string spellId = "spell_fireball";
    public string spellName = "Fireball";
    public SpellElement element = SpellElement.Fire;
    public int manaCost = 25;
    [TextArea(2, 3)]
    public string loreDescription = "Concentrated sphere of primeval fire that detonates upon impact.";

    [Header("Casting Assistant Gesture / Notes")]
    public string gestureShapeName = "Triangle";
    [TextArea(2, 3)]
    public string gestureInstructions = "Inscribe a 3-point triangle apex -> bottom-right -> bottom-left, then hit CAST.";
    public Vector2[] guideStrokePoints;

    [Header("Bard Melodic Sequence (if Bard song)")]
    public string[] noteSequence;
    public BardicBuffPayloadType buffPayload = BardicBuffPayloadType.HymnOfTheSpire_DamageBuff;
    public float payloadMagnitude = 0.25f;
    public float durationSeconds = 14f;
}

[CreateAssetMenu(fileName = "Spellbook_New", menuName = "BattleSummoner/Spellbook Grimoire")]
public class SpellbookItem : ScriptableObject
{
    [Header("Book Identity")]
    public string bookId = "grimoire_ignis";
    public string bookTitle = "Ignis Compendium";
    public string authorLore = "Archmage Ignis of the Crimson Spire";
    [TextArea(2, 4)]
    public string description = "Ancient codex bound in salamander hide. Contains prime pyromantic runes for devastating flame spells.";
    public Color themeColor = new Color(1.0f, 0.45f, 0.15f);

    [Header("Allowed Classes")]
    public PlayerClassType[] allowedClasses = new PlayerClassType[] { PlayerClassType.Mage };

    [Header("Inscribed Spells / Songs")]
    public List<GrimoireSpellEntry> inscribedSpells = new List<GrimoireSpellEntry>();

    public bool IsUsableBy(PlayerClassType classType)
    {
        if (allowedClasses == null || allowedClasses.Length == 0) return true;
        for (int i = 0; i < allowedClasses.Length; i++)
        {
            if (allowedClasses[i] == classType) return true;
        }
        return false;
    }
}

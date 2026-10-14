using UnityEngine;

public enum NPCRole
{
    Blacksmith,
    Alchemist,
    ExpeditionLeader,
    Gemologist,
    BattleCompanion
}

public enum NPCQuirk
{
    HoarderOfGems,     // +30% Gem yield on expeditions, -10% survival rate
    CautiousScout,     // 100% expedition success, -15% rare item drop rate
    MasterSmelter,     // -20% forge fracture debt accumulation
    ArcaneResonator    // Boosts elemental spell damage by +15% when accompanying
}

[CreateAssetMenu(fileName = "NewBespokeNPC", menuName = "Spire/Bespoke NPC Data")]
public class BespokeNPCObject : ScriptableObject
{
    [Header("Unique Identity (Never Random)")]
    public string npcId = "npc_margo";
    public string displayName = "Margo";
    public string title = "Goth Occultist & Appraiser";
    public NPCRole primaryRole = NPCRole.Alchemist;
    public NPCQuirk quirk = NPCQuirk.HoarderOfGems;

    [Header("Visual Profile Portrait")]
    public Sprite profilePortrait;
    public Sprite sideProfileReference;

    [Header("Conversational AI Persona")]
    [TextArea(4, 10)]
    public string systemPromptPersona = "You are Margo, a cynical, sweet-and-sour goth alchemist living in the Spire base camp. You love shiny dark geodes and wear striped arm-warmers. Speak with sharp wit, use subtle kaomojis, and complain about clumsy delvers.";

    [Header("Affinity & Companion Stats")]
    public int affinityLevel = 1;
    public int maxAffinity = 10;
    public float combatAttackPower = 45f;
    public float companionSupportCooldown = 12f;

    [Header("Expedition Aptitude")]
    public float expeditionSpeedMultiplier = 1.0f;
    public float rareDiscoveryBonus = 0.15f;
}

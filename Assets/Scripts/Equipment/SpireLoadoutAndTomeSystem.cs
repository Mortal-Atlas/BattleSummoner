using System;
using System.Collections.Generic;
using UnityEngine;

public enum HandSlotRule
{
    OneHanded,      // 1H Swords, Daggers, 1H Wands
    TwoHanded,      // 2H Staves, 2H Greatswords (Locks Off-Hand)
    OffHandOnly     // Shields, Tomes, Wizarding Books (Second-Hand Only)
}

public enum EquipmentCategory
{
    None,
    Blade1H,        // Sword or Dagger (Grants +1 Slash Finger)
    Greatsword2H,   // Two-Handed Blade (1 Slash Finger, High Mass)
    Wand1H,         // One-Handed Catalyst (Grants +1 Rune Finger)
    Staff2H,        // Two-Handed Catalyst (1 Rune Finger, +35% Power, Locks Off-Hand)
    ShieldOffHand,  // Second-Hand Only (Grants Passive Frontal Block)
    TomeOffHand     // Second-Hand Only Wizarding Book (Grants Affinity Match Buff)
}

public enum SpellAffinity
{
    None,
    Healing,        // Heart Glyph (❤️) -> Stronger Heals
    Fire,           // Triangle Glyph (🔺) -> Stronger Fire Power
    Rock,           // Square Glyph (⬛) -> Stronger Kinetic/Earth Power
    Water,          // Wavy Lines (〰️) -> Stronger Hydro/Flow Power
    Protection,     // Shield Glyph (⛨) -> Stronger Ally Barriers
    Wind,           // Aero Catalyst
    Aero = Wind,
    Lightning       // Spark Catalyst
}

[Serializable]
public class SpireEquippableItem
{
    public string itemId;
    public string itemName;
    public EquipmentCategory category;
    public HandSlotRule slotRule;
    public float basePower = 50f;

    [Header("Tome / Wizarding Book Affinity Settings")]
    public SpellAffinity itemAffinity = SpellAffinity.None;
    [Tooltip("Tiny buff multiplier when spell matches Tome affinity (default: 0.15 = +15%)")]
    public float affinityMatchBuff = 0.15f;

    [Header("Passive Matrix Multipliers")]
    public float postureDamageMultiplier = 1.0f;
    public float temperDecayRate = 1.0f;
    public float cleaveMassMultiplier = 1.0f;
    public float armorPenetration = 0f;
    public float quantumStabilityMultiplier = 1.0f;

    public SpireEquippableItem() { }

    public SpireEquippableItem(string id, string name, EquipmentCategory cat, HandSlotRule rule, float power = 50f, SpellAffinity affinity = SpellAffinity.None, float buff = 0.15f)
    {
        itemId = id;
        itemName = name;
        category = cat;
        slotRule = rule;
        basePower = power;
        itemAffinity = affinity;
        affinityMatchBuff = buff;
    }
}

/// <summary>
/// SpireLoadoutAndTomeSystem governs the Master Equipment, Finger-Pointer & Catalyst Matrix:
/// 1. HandSlotRules (OneHanded, TwoHanded, OffHandOnly).
/// 2. Active Touch Pointer Derivation (allowedSlashFingers, allowedRuneFingers).
/// 3. Passive Shield Blocking (equipped Shield in secondHand).
/// 4. Second-Hand Tomes & Wizarding Books (+15% Affinity Resonance Buffs for Healing, Fire, Rock, Water, Protection).
/// 5. Universal Two-Finger Tap Parry coordination.
/// </summary>
public class SpireLoadoutAndTomeSystem : MonoBehaviour
{
    private static SpireLoadoutAndTomeSystem _instance;
    public static SpireLoadoutAndTomeSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<SpireLoadoutAndTomeSystem>();
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("SpireLoadoutAndTomeSystem");
                    _instance = go.AddComponent<SpireLoadoutAndTomeSystem>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Active Hand Slots")]
    public SpireEquippableItem mainHand;
    public SpireEquippableItem secondHand; // Off-Hand Slot (2nd Sword, Shield, 2nd Wand, or Tome/Book)

    [Header("Live Derived Capabilities (Read by Touch Controller)")]
    public int allowedSlashFingers { get; private set; }
    public int allowedRuneFingers { get; private set; }
    public bool hasPassiveShieldBlock { get; private set; }
    public float baseCatalystMultiplier { get; private set; } = 1.0f;

    // Margo Master Blueprint Spec Aliases
    public int maxActiveSlashFingers => allowedSlashFingers;
    public int maxActiveRuneFingers => allowedRuneFingers;
    public bool hasPassiveFrontalShield => hasPassiveShieldBlock;

    public float GetCatalystAndTomeSpellMultiplier(SpellAffinity castAffinity)
    {
        float mult = baseCatalystMultiplier;
        if (secondHand != null && secondHand.category == EquipmentCategory.TomeOffHand)
        {
            if (secondHand.itemAffinity == castAffinity && castAffinity != SpellAffinity.None)
            {
                mult += secondHand.affinityMatchBuff;
            }
        }
        return mult;
    }

    [Header("Passive Archetype Modifiers")]
    public float activePostureDamageMultiplier { get; private set; } = 1.0f;
    public float activeTemperDecayRate { get; private set; } = 1.0f;
    public float activeCleaveMassMultiplier { get; private set; } = 1.0f;
    public float activeArmorPenetration { get; private set; } = 0f;
    public float activeQuantumStabilityMultiplier { get; private set; } = 1.0f;

    public event Action OnLoadoutReconfigured;

    private void Awake()
    {
        if (_instance == null || !_instance) _instance = this;
        else if (_instance != this)
        {
            if (Application.isPlaying) Destroy(this);
            else DestroyImmediate(this);
            return;
        }

        if (mainHand == null && secondHand == null)
        {
            EquipPreset("SingleBlade");
        }
        else
        {
            RecalculateStanceCapabilities();
        }
    }

    private void Start()
    {
        RecalculateStanceCapabilities();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public bool EquipMainHand(SpireEquippableItem newItem)
    {
        if (newItem != null && newItem.slotRule == HandSlotRule.OffHandOnly)
        {
            Debug.LogWarning($"[Equipment] {newItem.itemName} (Shield/Tome) is a Second-Hand item only!");
            return false;
        }

        mainHand = newItem;

        // If equipping a 2H Staff or 2H Greatsword, automatically clear/lock the Second Hand
        if (mainHand != null && mainHand.slotRule == HandSlotRule.TwoHanded)
        {
            secondHand = null;
        }

        RecalculateStanceCapabilities();
        return true;
    }

    public bool EquipSecondHand(SpireEquippableItem newOffHandItem)
    {
        if (mainHand != null && mainHand.slotRule == HandSlotRule.TwoHanded)
        {
            Debug.LogWarning($"[Equipment] Cannot equip {newOffHandItem?.itemName} while wielding Two-Handed {mainHand.itemName}!");
            return false;
        }

        if (newOffHandItem != null && newOffHandItem.slotRule == HandSlotRule.TwoHanded)
        {
            Debug.LogWarning($"[Equipment] {newOffHandItem.itemName} is Two-Handed and cannot go in the Second Hand!");
            return false;
        }

        secondHand = newOffHandItem;
        RecalculateStanceCapabilities();
        return true;
    }

    public void RecalculateStanceCapabilities()
    {
        allowedSlashFingers = 0;
        allowedRuneFingers = 0;
        hasPassiveShieldBlock = false;
        baseCatalystMultiplier = 1.0f;

        activePostureDamageMultiplier = 1.0f;
        activeTemperDecayRate = 1.0f;
        activeCleaveMassMultiplier = 1.0f;
        activeArmorPenetration = 0f;
        activeQuantumStabilityMultiplier = 1.0f;

        // Evaluate Main-Hand
        if (mainHand != null)
        {
            switch (mainHand.category)
            {
                case EquipmentCategory.Blade1H:
                    allowedSlashFingers++;
                    // Archetype: Single Blade bonus (+25% Posture, 0.75x temper decay) if off-hand empty
                    activePostureDamageMultiplier = 1.25f;
                    activeTemperDecayRate = 0.75f;
                    break;

                case EquipmentCategory.Greatsword2H:
                    allowedSlashFingers = 1;
                    // Archetype: 2H Greatsword (+50% Cleave Mass, heavy armor pen)
                    activeCleaveMassMultiplier = 1.50f;
                    activeArmorPenetration = 35f;
                    break;

                case EquipmentCategory.Wand1H:
                    allowedRuneFingers++;
                    baseCatalystMultiplier += 0.10f;
                    break;

                case EquipmentCategory.Staff2H:
                    allowedRuneFingers = 1;
                    baseCatalystMultiplier = 1.35f; // 2H Staff commitment power bonus (+35%)
                    activeQuantumStabilityMultiplier = 1.20f; // +20% 11D Quantum Stability
                    OnLoadoutReconfigured?.Invoke();
                    return;
            }
        }

        // Evaluate Second-Hand (Off-Hand)
        if (secondHand != null)
        {
            switch (secondHand.category)
            {
                case EquipmentCategory.Blade1H:
                    allowedSlashFingers++; // Unlocks 2nd simultaneous slash finger!
                    // Dual wield replaces single blade posture bonus with twin-slashing output
                    activePostureDamageMultiplier = 1.0f;
                    activeTemperDecayRate = 1.0f;
                    break;

                case EquipmentCategory.Wand1H:
                    allowedRuneFingers++;  // Unlocks 2-finger simultaneous rune drawing!
                    baseCatalystMultiplier += 0.10f;
                    break;

                case EquipmentCategory.ShieldOffHand:
                    hasPassiveShieldBlock = true; // Passively blocks frontal hits when not mid-action
                    break;

                case EquipmentCategory.TomeOffHand:
                    // Wizarding Book / Tome equipped in off-hand: ensures player can cast runes
                    if (allowedRuneFingers == 0) allowedRuneFingers = 1;
                    break;
            }
        }

        OnLoadoutReconfigured?.Invoke();
    }

    /// <summary>
    /// Evaluates total spell/heal/shield multiplier, granting the Tome's tiny affinity buff (+15%)
    /// if the cast SpellAffinity matches the equipped Second-Hand Tome/Wizarding Book.
    /// </summary>
    public float EvaluateSpellOutputWithTomeAffinity(SpellAffinity castAffinity, float rawBaseValue)
    {
        float totalMultiplier = baseCatalystMultiplier;

        if (secondHand != null && secondHand.category == EquipmentCategory.TomeOffHand)
        {
            if (secondHand.itemAffinity == castAffinity && castAffinity != SpellAffinity.None)
            {
                totalMultiplier *= (1.0f + secondHand.affinityMatchBuff); // +15% stronger output
                Debug.Log($"📖 [TOME AFFINITY MATCH] '{secondHand.itemName}' boosted {castAffinity} output by +{secondHand.affinityMatchBuff * 100f:F0}%!");
            }
        }

        return rawBaseValue * totalMultiplier;
    }

    #region Preset Loadouts Factory
    public void EquipPreset(string archetype)
    {
        switch (archetype)
        {
            case "SingleBlade":
                EquipMainHand(new SpireEquippableItem("spire_steel_sword", "Spire Steel Sword", EquipmentCategory.Blade1H, HandSlotRule.OneHanded, 55f));
                EquipSecondHand(null);
                break;

            case "Greatsword2H":
                EquipMainHand(new SpireEquippableItem("titan_colossus_blade", "Titan Colossus Greatsword", EquipmentCategory.Greatsword2H, HandSlotRule.TwoHanded, 95f));
                break;

            case "DualWield":
                EquipMainHand(new SpireEquippableItem("spire_steel_sword", "Spire Steel Sword", EquipmentCategory.Blade1H, HandSlotRule.OneHanded, 55f));
                EquipSecondHand(new SpireEquippableItem("spire_twin_dagger", "Fiery Amber Dagger", EquipmentCategory.Blade1H, HandSlotRule.OneHanded, 45f));
                break;

            case "SwordAndShield":
                EquipMainHand(new SpireEquippableItem("spire_steel_sword", "Spire Steel Sword", EquipmentCategory.Blade1H, HandSlotRule.OneHanded, 55f));
                EquipSecondHand(new SpireEquippableItem("spire_aegis_shield", "Spire Aegis Shield", EquipmentCategory.ShieldOffHand, HandSlotRule.OffHandOnly, 40f));
                break;

            case "GrandStaff":
            case "GrandStaff2H":
                EquipMainHand(new SpireEquippableItem("astral_grand_staff", "Astral Grand Staff", EquipmentCategory.Staff2H, HandSlotRule.TwoHanded, 80f));
                break;

            case "WandAndShield":
                EquipMainHand(new SpireEquippableItem("arcane_crystal_wand", "Arcane Crystal Wand", EquipmentCategory.Wand1H, HandSlotRule.OneHanded, 45f));
                EquipSecondHand(new SpireEquippableItem("spire_aegis_shield", "Spire Aegis Shield", EquipmentCategory.ShieldOffHand, HandSlotRule.OffHandOnly, 40f));
                break;

            case "WandAndTome":
            case "CodexOfMending":
                EquipMainHand(new SpireEquippableItem("arcane_crystal_wand", "Arcane Crystal Wand", EquipmentCategory.Wand1H, HandSlotRule.OneHanded, 45f));
                EquipSecondHand(new SpireEquippableItem("codex_of_mending", "Codex of Mending", EquipmentCategory.TomeOffHand, HandSlotRule.OffHandOnly, 35f, SpellAffinity.Healing, 0.15f));
                break;

            case "PyreGrimoire":
                EquipMainHand(new SpireEquippableItem("arcane_crystal_wand", "Arcane Crystal Wand", EquipmentCategory.Wand1H, HandSlotRule.OneHanded, 45f));
                EquipSecondHand(new SpireEquippableItem("pyre_grimoire", "Pyre Grimoire", EquipmentCategory.TomeOffHand, HandSlotRule.OffHandOnly, 35f, SpellAffinity.Fire, 0.15f));
                break;

            case "LithicTome":
                EquipMainHand(new SpireEquippableItem("arcane_crystal_wand", "Arcane Crystal Wand", EquipmentCategory.Wand1H, HandSlotRule.OneHanded, 45f));
                EquipSecondHand(new SpireEquippableItem("lithic_tome", "Lithic Tome", EquipmentCategory.TomeOffHand, HandSlotRule.OffHandOnly, 35f, SpellAffinity.Rock, 0.15f));
                break;

            case "TidalCompendium":
                EquipMainHand(new SpireEquippableItem("arcane_crystal_wand", "Arcane Crystal Wand", EquipmentCategory.Wand1H, HandSlotRule.OneHanded, 45f));
                EquipSecondHand(new SpireEquippableItem("tidal_compendium", "Tidal Compendium", EquipmentCategory.TomeOffHand, HandSlotRule.OffHandOnly, 35f, SpellAffinity.Water, 0.15f));
                break;

            case "BastionLexicon":
                EquipMainHand(new SpireEquippableItem("arcane_crystal_wand", "Arcane Crystal Wand", EquipmentCategory.Wand1H, HandSlotRule.OneHanded, 45f));
                EquipSecondHand(new SpireEquippableItem("bastion_lexicon", "Bastion Lexicon", EquipmentCategory.TomeOffHand, HandSlotRule.OffHandOnly, 35f, SpellAffinity.Protection, 0.15f));
                break;

            case "SpellswordAndTome":
                EquipMainHand(new SpireEquippableItem("spire_steel_sword", "Spire Steel Sword", EquipmentCategory.Blade1H, HandSlotRule.OneHanded, 55f));
                EquipSecondHand(new SpireEquippableItem("bastion_lexicon", "Bastion Lexicon", EquipmentCategory.TomeOffHand, HandSlotRule.OffHandOnly, 35f, SpellAffinity.Protection, 0.15f));
                break;

            case "DualWands":
                EquipMainHand(new SpireEquippableItem("wand_celestial_dawn", "Wand of Celestial Dawn", EquipmentCategory.Wand1H, HandSlotRule.OneHanded, 45f));
                EquipSecondHand(new SpireEquippableItem("wand_astral_dusk", "Wand of Astral Dusk", EquipmentCategory.Wand1H, HandSlotRule.OneHanded, 45f));
                break;
        }
    }
    #endregion
}

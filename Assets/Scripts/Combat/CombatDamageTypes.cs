using System;
using UnityEngine;

namespace BattleSummoner.Combat
{
    public enum DamageType
    {
        PhysicalSlash,     // Swipe swings, blades, cleaves
        PhysicalBlunt,     // Hammers, clubs, shield bashes
        PhysicalPierce,    // Arrows, thrusts, needles
        Fire,              // Flames, pyre bursts, burning embers
        Ice,               // Frost, sub-zero crystals, blizzard chills
        Electric,          // Lightning bolts, galvanic discharge, shocks
        Poison,            // Venomous toxins, serpentine ichor
        Acid,              // Corrosive bile, vitriol, armor-dissolving liquids
        ArcaneVoid,        // Astral ether, gravitation singularities
        RadiantHoly        // Solar judgment, celestial brilliance
    }

    public enum HitOutcome
    {
        Miss,              // Evaded / missed entirely (0 damage)
        Glancing,          // Glancing blow (50% damage, no status application)
        Normal,            // Standard direct hit (100% damage)
        Critical,          // Critical strike (175% - 250% damage, bold impact)
        Blocked,           // Passive shield block (-65% damage mitigation)
        Parried            // Deflected with perfect two-finger parry (100% negation)
    }

    public enum StatusEffectType
    {
        None,
        Burn,              // Fire DoT every 0.5s, visual fiery aura
        Freeze,            // Encased solid in crystalline ice block, cannot move or act
        Chilled,           // Movement & attack speed slowed by 40%, precursor to Shattered
        Shocked,           // Crackling electricity, micro-stuns every 1.5s
        Poisoned,          // Toxic venom DoT every 1.0s, -50% healing efficacy
        AcidCorroded,      // Armor shredded (-35% damage mitigation) + corrosive tick damage
        Slowed,            // Movement and cast speed slowed by 50%
        Sleep,             // Incapacitated in deep slumber, waking hit triggers Nightmare Crit!
        Paralyzed          // Stunned with static halo, completely locked for duration
    }

    public enum ElementalComboType
    {
        None,
        Shattered,         // Ice (Chilled/Frozen) + Electric -> Explosive ice shatter (+150% bonus damage + AoE frost shrapnel)
        Combustion,        // Fire + Poison/Acid -> Volatile detonation (+120% AoE fire blast + knockback)
        MeltThermalShock,  // Ice + Fire -> Extreme temperature shock (2x burst damage + blinding steam cloud)
        Superconduct,      // Electric + Acid/Water -> Electro-corrosion discharge arcing to 4 nearby foes
        NightmareReap      // Sleep + Physical Slash -> 2.5x wake-up critical strike + fear pulse
    }

    [System.Serializable]
    public class DamagePayload
    {
        public float baseDamage = 25f;
        public DamageType damageType = DamageType.PhysicalSlash;
        public Vector3 hitPoint = Vector3.zero;
        public Vector3 hitDirection = Vector3.forward;
        public float critChance = 0.18f;
        public float critMultiplier = 2.0f;
        public float accuracy = 0.95f; // Accuracy vs Evasion (can cause Miss)
        public GameObject attacker;
        public bool isAoE = false;
        public float aoeRadius = 0f;
        public StatusEffectType appliedStatus = StatusEffectType.None;
        public float statusDuration = 4.0f;
        public float statusPotency = 12f;
        public float armorPenetration = 0f;
        public float postureDamage = 0f;

        public DamagePayload() { }

        public DamagePayload(float dmg, DamageType type, Vector3 point, Vector3 dir, GameObject source = null)
        {
            baseDamage = dmg;
            damageType = type;
            hitPoint = point;
            hitDirection = dir.normalized;
            attacker = source;
        }
    }

    [System.Serializable]
    public class DamageResult
    {
        public float finalDamage;
        public HitOutcome outcome = HitOutcome.Normal;
        public DamageType damageType = DamageType.PhysicalSlash;
        public ElementalComboType comboTriggered = ElementalComboType.None;
        public float comboBonusDamage = 0f;
        public string comboName = "";
        public bool targetDied = false;
    }

    public interface IDamageReceiver
    {
        DamageResult TakeAdvancedDamage(DamagePayload payload);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Spellcraft
{
    [Serializable]
    public class SpellFormulaPatent
    {
        public string formulaHash;
        public string spellName;
        public string inventorName;
        public string customInnerRuneNotation;
        public long totalRoyaltiesAccrued;
        public DateTime registrationDate;
    }

    /// <summary>
    /// GuildPatentOffice tracks all spell craft formulas discovered across the Spire.
    /// When crafting at the Build-A-Spell Workbench:
    /// - If the formula matches an existing patent: grants -15% Instability, displays [KNOWN FORMULA DISCOVERED],
    ///   and routes a 2% Mana Core royalty to the original inventor.
    /// - If novel: patents the formula in the player's name.
    /// </summary>
    public class GuildPatentOffice : MonoBehaviour
    {
        public static GuildPatentOffice Instance { get; private set; }

        [SerializeField] private List<SpellFormulaPatent> registeredPatents = new List<SpellFormulaPatent>();
        private readonly Dictionary<string, SpellFormulaPatent> _patentLookup = new Dictionary<string, SpellFormulaPatent>();

        public event Action<SpellFormulaPatent> OnPatentCollision;
        public event Action<SpellFormulaPatent> OnNewPatentRegistered;

        private void Awake()
        {
            if (Instance != null && Instance != this && Application.isPlaying)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SeedKnownPatents();
        }

        public void SeedKnownPatents()
        {
            if (_patentLookup.Count > 0) return;

            if (registeredPatents.Count > 0)
            {
                foreach (var p in registeredPatents)
                {
                    if (!string.IsNullOrEmpty(p.formulaHash))
                        _patentLookup[p.formulaHash] = p;
                }
                return;
            }

            RegisterPatentInternal("StraightShot_InstantBurst_Fire_None_3_3_6", "Astral Cinder Lance", "Archmage Ignis", "Rune_FlameSpire");
            RegisterPatentInternal("AoEDomeBubble_PeriodicTick_Time_LocalizedChronoSlowBubble_5_5_8", "7D Chrono-Anchor Dome", "Clockmaker Zephyr", "Rune_HourglassLoop");
            RegisterPatentInternal("BladeArrowCoat_InstantBurst_Lightning_None_4_2_5", "Thunder-Edged Stride", "Swordmaster Raiden", "Rune_LightningCrest");
        }

        private void RegisterPatentInternal(string hash, string name, string inventor, string rune)
        {
            var patent = new SpellFormulaPatent
            {
                formulaHash = hash,
                spellName = name,
                inventorName = inventor,
                customInnerRuneNotation = rune,
                totalRoyaltiesAccrued = 0,
                registrationDate = DateTime.UtcNow
            };
            registeredPatents.Add(patent);
            _patentLookup[hash] = patent;
        }

        public bool CheckPatentCollision(string formulaHash, out SpellFormulaPatent matchedPatent, out float instabilityDiscount, out float royaltyCut)
        {
            if (_patentLookup.Count == 0)
            {
                SeedKnownPatents();
            }

            if (_patentLookup.TryGetValue(formulaHash, out matchedPatent))
            {
                instabilityDiscount = 0.15f; // -15% Instability
                royaltyCut = 0.02f;          // 2% Mana Core royalty to original inventor
                matchedPatent.totalRoyaltiesAccrued += 2;
                OnPatentCollision?.Invoke(matchedPatent);
                Debug.Log($"<color=#F59E0B><b>[KNOWN FORMULA DISCOVERED] Formula matched patent: '{matchedPatent.spellName}' by {matchedPatent.inventorName}! (-15% Instability applied, 2% royalty routed)</b></color>");
                return true;
            }

            instabilityDiscount = 0f;
            royaltyCut = 0f;
            matchedPatent = null;
            return false;
        }

        public SpellFormulaPatent RegisterNewPatent(string formulaHash, string spellName, string inventorName, string customRuneNotation)
        {
            var newPatent = new SpellFormulaPatent
            {
                formulaHash = formulaHash,
                spellName = spellName,
                inventorName = inventorName,
                customInnerRuneNotation = customRuneNotation,
                totalRoyaltiesAccrued = 0,
                registrationDate = DateTime.UtcNow
            };
            registeredPatents.Add(newPatent);
            _patentLookup[formulaHash] = newPatent;
            OnNewPatentRegistered?.Invoke(newPatent);
            Debug.Log($"<color=#10B981><b>✦ [PATENT REGISTERED] New spell patent '{spellName}' officially registered to {inventorName}!</b></color>");
            return newPatent;
        }
    }
}

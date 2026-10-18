using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Social
{
    public enum FactionType
    {
        SolasCitizenry,     // Townsfolk, commoners, and village inhabitants
        MerchantConsortium, // Curio dealers, blacksmiths, traders, apothecaries
        SpireWardenOrder,   // City guards, fortress wardens, military peacekeepers
        ArcaneAcademy       // Astral scholars, researchers, librarians, spellcrafters
    }

    public enum ReputationTier
    {
        Despised,   // -100 to -50: Feared or despised; guards inspect, merchants gouge prices
        Distrusted, // -49 to -10: Suspicious whispers; cool reception, +15% merchant markup
        Neutral,    // -9 to +19: Unknown traveler; standard greetings and standard trade
        Respected,  // +20 to +59: Friendly greeting, 10% merchant discount, guards nod
        Honored,    // +60 to +89: Acclaimed hero, 20% merchant discount, guards salute
        Exalted     // +90 to +100: "Champion of Solas", 30% discount, citizens cheer
    }

    [System.Serializable]
    public class FactionReputation
    {
        public FactionType faction;
        public string factionName;
        [Range(-100, 100)] public int currentReputation = 0;
        public string titleRank;
        [TextArea(2, 3)] public string publicOpinionDescription;
    }

    /// <summary>
    /// ReputationManager tracks public renown and what the general populous think about the player:
    /// - Tracks standing across Solas Citizenry, Merchant Consortium, Spire Warden Order, and Arcane Academy
    /// - Governs populous greetings, merchant discounts/surcharges, and guard reactions
    /// </summary>
    public class ReputationManager : MonoBehaviour
    {
        private static ReputationManager _instance;
        public static ReputationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ReputationManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var go = new GameObject("ReputationManager");
                        _instance = go.AddComponent<ReputationManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Faction Standings")]
        [SerializeField] private List<FactionReputation> factionStandings = new List<FactionReputation>();

        public IReadOnlyList<FactionReputation> Factions => factionStandings;

        public static event Action<FactionType, int, ReputationTier> OnReputationChanged;
        public static event Action<ReputationTier> OnGlobalRenownTierChanged;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            if (factionStandings == null || factionStandings.Count == 0)
            {
                InitializeDefaultFactions();
            }
        }

        public void InitializeDefaultFactions()
        {
            factionStandings.Clear();
            factionStandings.Add(new FactionReputation
            {
                faction = FactionType.SolasCitizenry,
                factionName = "Solas Citizenry",
                currentReputation = 15, // Starts as a modest newcomer
                titleRank = "Recognized Traveler",
                publicOpinionDescription = "The townsfolk recognize your face along the promenade and whisper curious rumors about your summoner arts."
            });

            factionStandings.Add(new FactionReputation
            {
                faction = FactionType.MerchantConsortium,
                factionName = "Merchant Consortium",
                currentReputation = 20, // Welcomed customer
                titleRank = "Valued Patron",
                publicOpinionDescription = "Market shopkeepers and curio traders smile as you approach, eager to barter and offer friendly rates."
            });

            factionStandings.Add(new FactionReputation
            {
                faction = FactionType.SpireWardenOrder,
                factionName = "Spire Warden Order",
                currentReputation = 10,
                titleRank = "Auxiliary Watcher",
                publicOpinionDescription = "The fortress sentries acknowledge your presence at the city gates with formal, cautious vigilance."
            });

            factionStandings.Add(new FactionReputation
            {
                faction = FactionType.ArcaneAcademy,
                factionName = "Arcane Academy",
                currentReputation = 25,
                titleRank = "Initiate of the Astral Weave",
                publicOpinionDescription = "Scholars and alchemists admire your ability to manifest astral familiars and weave kinetic spells."
            });
        }

        public int GetReputation(FactionType faction)
        {
            var f = GetFactionData(faction);
            return f != null ? f.currentReputation : 0;
        }

        public ReputationTier GetTier(FactionType faction)
        {
            int rep = GetReputation(faction);
            if (rep <= -50) return ReputationTier.Despised;
            if (rep <= -10) return ReputationTier.Distrusted;
            if (rep <= 19)  return ReputationTier.Neutral;
            if (rep <= 59)  return ReputationTier.Respected;
            if (rep <= 89)  return ReputationTier.Honored;
            return ReputationTier.Exalted;
        }

        public ReputationTier GetGlobalRenownTier()
        {
            int total = 0;
            for (int i = 0; i < factionStandings.Count; i++) total += factionStandings[i].currentReputation;
            int avg = factionStandings.Count > 0 ? total / factionStandings.Count : 0;

            if (avg <= -50) return ReputationTier.Despised;
            if (avg <= -10) return ReputationTier.Distrusted;
            if (avg <= 19)  return ReputationTier.Neutral;
            if (avg <= 59)  return ReputationTier.Respected;
            if (avg <= 89)  return ReputationTier.Honored;
            return ReputationTier.Exalted;
        }

        public void AddReputation(FactionType faction, int amount, string reason = "")
        {
            var f = GetFactionData(faction);
            if (f == null) return;

            int oldRep = f.currentReputation;
            var oldTier = GetTier(faction);

            f.currentReputation = Mathf.Clamp(f.currentReputation + amount, -100, 100);
            var newTier = GetTier(faction);

            UpdateFactionTitle(f);

            Debug.Log($"📜 [REPUTATION CHANGE] {f.factionName}: {(amount >= 0 ? "+" : "")}{amount} ({f.currentReputation}/100) [{newTier}]. Reason: {reason}");
            OnReputationChanged?.Invoke(faction, f.currentReputation, newTier);

            if (oldTier != newTier)
            {
                OnGlobalRenownTierChanged?.Invoke(GetGlobalRenownTier());
                if (DevClassLoadoutManager.Instance != null)
                {
                    string colorHex = newTier >= ReputationTier.Respected ? "#22FF66" : (newTier <= ReputationTier.Distrusted ? "#FF3344" : "#FFE066");
                    DevClassLoadoutManager.Instance.ShowToast($"<color={colorHex}><b>[REPUTATION: {newTier.ToString().ToUpper()}]</b></color>\n{f.factionName}: {f.titleRank}");
                }
            }
        }

        public float GetMerchantPriceMultiplier()
        {
            var tier = GetTier(FactionType.MerchantConsortium);
            return tier switch
            {
                ReputationTier.Despised   => 1.35f, // +35% gouging
                ReputationTier.Distrusted => 1.15f, // +15% markup
                ReputationTier.Neutral    => 1.00f, // standard
                ReputationTier.Respected  => 0.90f, // 10% discount
                ReputationTier.Honored    => 0.80f, // 20% discount
                ReputationTier.Exalted    => 0.70f, // 30% VIP discount
                _ => 1.0f
            };
        }

        /// <summary>
        /// Contextual town dialogue based on what the general populous thinks about you.
        /// </summary>
        public string GetPopulousGreeting(string npcName, FactionType faction)
        {
            var tier = GetTier(faction);
            return tier switch
            {
                ReputationTier.Despised =>
                    $"*Averts eyes and steps back with a tremble* \"K-Keep moving, stranger... We don't want trouble in Solas today...\"",

                ReputationTier.Distrusted =>
                    $"*Crosses arms guardedly* \"Hmph. What brings you to this district? The Wardens have their eyes on you, you know.\"",

                ReputationTier.Neutral =>
                    $"\"Greetings, traveler. Another brisk morning in Solas. Mind the Spire mist if you're venturing past the bridge.\"",

                ReputationTier.Respected =>
                    $"*Smiles warmly and waves* \"Good day, summoner! Word of your courage in the outer rings is spreading fast!\"",

                ReputationTier.Honored =>
                    $"*Bows respectfully* \"An honor to see you, champion! The town feels that much safer knowing you walk our streets.\"",

                ReputationTier.Exalted =>
                    $"*Cheers with bright admiration* \"Look, it's the Champion of Solas! May the astral stars forever bless your path!\"",

                _ => "\"Greetings, summoner.\""
            };
        }

        private FactionReputation GetFactionData(FactionType faction)
        {
            for (int i = 0; i < factionStandings.Count; i++)
            {
                if (factionStandings[i].faction == faction) return factionStandings[i];
            }
            return null;
        }

        private void UpdateFactionTitle(FactionReputation f)
        {
            var tier = GetTier(f.faction);
            f.titleRank = tier switch
            {
                ReputationTier.Despised   => "Outcast of Solas",
                ReputationTier.Distrusted => "Suspicious Drifter",
                ReputationTier.Neutral    => "Recognized Traveler",
                ReputationTier.Respected  => "Trusted Ally",
                ReputationTier.Honored    => "Hero of the Spire",
                ReputationTier.Exalted    => "Champion of Solas",
                _ => "Traveler"
            };
        }
    }
}

using System;
using UnityEngine;

namespace BattleSummoner.Progression.ThreeMasks
{
    /// <summary>
    /// NPCTalentRecognitionService enables world NPCs to dynamically react to the player's
    /// active Three Masks Talents, Dormant feats, and equipped Prestige Titles.
    /// </summary>
    public class NPCTalentRecognitionService : MonoBehaviour
    {
        public static NPCTalentRecognitionService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public string GetNPCGreeting(string npcName, string npcRole)
        {
            string playerTitle = "Traveler";
            if (UncappedTitleSystem.Instance != null && !string.IsNullOrEmpty(UncappedTitleSystem.Instance.GetEquippedDisplayTitle()))
            {
                playerTitle = UncappedTitleSystem.Instance.GetEquippedDisplayTitle();
            }

            DoubleEdgedTalentDefinition activeTalent = null;
            if (ThreeMasksAndWildcardTalentSystem.Instance != null)
            {
                activeTalent = ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1;
            }

            if (activeTalent != null)
            {
                switch (activeTalent.wordId)
                {
                    case TalentWordID.Chef:
                        return $"\"Ah, greetings {playerTitle}! Smell that savory broth? A culinarian of your renown is always welcome here.\"";
                    case TalentWordID.Ferryman:
                        return $"\"Hail, {playerTitle}. The tides whisper of your safe crossings along the Spire's waterways.\"";
                    case TalentWordID.VesselOfKnowledge:
                        return $"\"Greetings, scholar. Word of your vast grimoire and eidetic memory precedes you in the Grand Bazaar.\"";
                    case TalentWordID.Gambler:
                        return $"\"Well, well, the fortune spinner yourself! Let us hope the dice land favorably today, {playerTitle}!\"";
                    case TalentWordID.Salaryman:
                        return $"\"Punctual as clockwork, {playerTitle}! Even the Spire watches your relentless hustle.\"";
                    case TalentWordID.Blacksmith:
                        return $"\"Stand true, forgemaster! The steel sings praise of your tempering touch.\"";
                }
            }

            return $"\"Welcome to the Spire Metropolis, {playerTitle}. May your journey through the hundred tiers bear great fruit.\"";
        }
    }
}

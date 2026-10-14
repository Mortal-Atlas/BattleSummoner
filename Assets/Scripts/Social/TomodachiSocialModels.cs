using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Social
{
    public enum SocialEntityType
    {
        All,
        NPC,
        Familiar,
        Summon,
        Construct
    }

    public enum TomodachiMood
    {
        Ecstatic,   // Glowing hearts, +50% gift affinity bonus, ecstatic banter
        Happy,      // Smiling, radiant particles, normal affinity gain
        Content,    // Relaxed, calm, standard demeanor
        Annoyed,    // Grumbling, furrowed brow, -25% affinity gain
        Enraged,    // Veins popping, furious, gifts risk link fractures
        Bored       // Sleepy/sighing, craves curiosities or sparring
    }

    public enum RelationshipTier
    {
        Nemesis,    // -100 to -60: Hostile, defiant, heavy active debuffs
        Distrusted, // -59 to -20: Cold, guarded, minor active debuffs
        Neutral,    // -19 to +19: Transactional, formal
        Friendly,   // +20 to +59: Warm, supportive, minor active buffs
        Soulbound   // +60 to +100: Bound in heart & soul, pinnacle active buffs
    }

    public enum GiftReaction
    {
        Loved,
        Liked,
        Neutral,
        Disliked,
        Hated
    }

    public enum TarotArcana
    {
        TheFool,
        TheMagician,
        TheHighPriestess,
        TheEmpress,
        TheEmperor,
        TheHierophant,
        TheLovers,
        TheChariot,
        Justice,
        TheHermit,
        WheelOfFortune,
        Strength,
        TheHangedMan,
        Death,
        Temperance,
        TheDevil,
        TheTower,
        TheStar,
        TheMoon,
        TheSun,
        Judgement,
        TheWorld
    }

    [System.Serializable]
    public class SocialLinkModifier
    {
        public string id;
        public string title;
        public bool isBuff;
        [TextArea(2, 3)]
        public string description;

        // Modifiers applied when active
        public float xpMultiplier = 1.0f;
        public float shopBuyMultiplier = 1.0f;
        public float shopSellMultiplier = 1.0f;
        public float playerDefenseMultiplier = 1.0f;
        public float playerMoveSpeedMultiplier = 1.0f;
        public float summonAttackMultiplier = 1.0f;
        public float summonHpMultiplier = 1.0f;
        public float summonCooldownMultiplier = 1.0f;
        public float autoShieldThreshold = 0f;
    }

    [System.Serializable]
    public class PersonaChatBranch
    {
        public string promptDialogue;
        public string choiceOptionA;
        public string reactionDialogueA;
        public int affinityDeltaA = 10;
        public TomodachiMood moodA = TomodachiMood.Happy;

        public string choiceOptionB;
        public string reactionDialogueB;
        public int affinityDeltaB = -5;
        public TomodachiMood moodB = TomodachiMood.Annoyed;
    }

    [System.Serializable]
    public class SocialEntityProfile
    {
        [Header("Identity & Lore")]
        public string entityId;
        public string displayName;
        public string subtitle;
        public SocialEntityType entityType;
        public TarotArcana arcana;
        public string arcanaTitle;
        public string portraitSpriteName;
        [TextArea(2, 4)]
        public string loreDescription;

        [Header("Social Dynamics")]
        [Range(-100, 100)]
        public int currentAffinity = 0;
        [Range(0, 10)]
        public int socialLinkRank = 0;
        public TomodachiMood currentMood = TomodachiMood.Content;

        [Header("Gift Taste Matrix")]
        public List<string> lovedGiftIds = new List<string>();
        public List<string> likedGiftIds = new List<string>();
        public List<string> dislikedGiftIds = new List<string>();
        public List<string> hatedGiftIds = new List<string>();

        [Header("Persona Dialogue Lines")]
        [TextArea(2, 3)] public string lovedGiftLine;
        [TextArea(2, 3)] public string likedGiftLine;
        [TextArea(2, 3)] public string neutralGiftLine;
        [TextArea(2, 3)] public string dislikedGiftLine;
        [TextArea(2, 3)] public string hatedGiftLine;
        [TextArea(2, 3)] public string rankUpLine;

        [Header("VN Interactive Chats")]
        public List<PersonaChatBranch> chatBranches = new List<PersonaChatBranch>();

        [Header("Active Link Effects")]
        public SocialLinkModifier positiveBuff;
        public SocialLinkModifier negativeDebuff;

        [Header("Tamagotchi Pet Familiar Care")]
        public FamiliarCareState careState = new FamiliarCareState();

        public RelationshipTier GetRelationshipTier()
        {
            if (currentAffinity <= -60) return RelationshipTier.Nemesis;
            if (currentAffinity <= -20) return RelationshipTier.Distrusted;
            if (currentAffinity <= 19) return RelationshipTier.Neutral;
            if (currentAffinity <= 59) return RelationshipTier.Friendly;
            return RelationshipTier.Soulbound;
        }

        public Color GetTierColor()
        {
            return GetRelationshipTier() switch
            {
                RelationshipTier.Nemesis => new Color(0.95f, 0.15f, 0.22f),    // Blazing crimson
                RelationshipTier.Distrusted => new Color(0.95f, 0.50f, 0.18f), // Rust orange
                RelationshipTier.Neutral => new Color(0.70f, 0.75f, 0.85f),    // Cool slate
                RelationshipTier.Friendly => new Color(0.25f, 0.90f, 0.50f),   // Jade green
                RelationshipTier.Soulbound => new Color(1.0f, 0.85f, 0.20f),   // Radiant gold
                _ => Color.white
            };
        }

        public string GetMoodEmoji()
        {
            return currentMood switch
            {
                TomodachiMood.Ecstatic => "<color=#FF4090>[ECSTATIC]</color>",
                TomodachiMood.Happy => "<color=#FFE040>[HAPPY]</color>",
                TomodachiMood.Content => "<color=#80D0FF>[CONTENT]</color>",
                TomodachiMood.Annoyed => "<color=#FF8020>[ANNOYED]</color>",
                TomodachiMood.Enraged => "<color=#FF2030>[ENRAGED]</color>",
                TomodachiMood.Bored => "<color=#909090>[BORED]</color>",
                _ => "[NEUTRAL]"
            };
        }

        public GiftReaction EvaluateGift(string itemId)
        {
            if (lovedGiftIds.Contains(itemId)) return GiftReaction.Loved;
            if (likedGiftIds.Contains(itemId)) return GiftReaction.Liked;
            if (hatedGiftIds.Contains(itemId)) return GiftReaction.Hated;
            if (dislikedGiftIds.Contains(itemId)) return GiftReaction.Disliked;
            return GiftReaction.Neutral;
        }

        public SocialLinkModifier GetActiveEffect()
        {
            RelationshipTier tier = GetRelationshipTier();
            if (tier == RelationshipTier.Soulbound || (tier == RelationshipTier.Friendly && socialLinkRank >= 3))
            {
                return positiveBuff;
            }
            if (tier == RelationshipTier.Nemesis || tier == RelationshipTier.Distrusted)
            {
                return negativeDebuff;
            }
            return null;
        }
    }

    /// <summary>
    /// Tamagotchi Pet Familiar Care State:
    /// Tracks Fullness, Hydration, Cleanliness, Affection Hearts, and Ni no Kuni style stat growth.
    /// </summary>
    [System.Serializable]
    public class FamiliarCareState
    {
        [Header("Tamagotchi Vitals (0 - 100)")]
        [Range(0f, 100f)] public float hunger = 65f;       // Food / Fullness
        [Range(0f, 100f)] public float hydration = 75f;    // Water / Hydration
        [Range(0f, 100f)] public float cleanliness = 60f;  // Bathed / Hygiene
        [Range(0f, 100f)] public float happiness = 80f;    // Joy / Mood

        [Header("Ni no Kuni Stat Growth")]
        public int bonusAttack = 0;   // Raised by Fire/Choco Treats
        public int bonusDefense = 0;  // Raised by Earth/Honey Treats
        public int bonusMagic = 0;    // Raised by Water/Flan Treats
        public int bonusSpeed = 0;    // Raised by Wind/Berry Treats

        [Header("Pokémon Affection Hearts (1 to 5)")]
        [Range(1, 5)] public int affectionHearts = 2;
        public int totalTimesFed = 0;
        public int totalTimesBathed = 0;
        public int totalPresentsReceived = 0;
        public int totalPettings = 0;

        [Header("Favorites & Quirk")]
        public string favoriteFood = "Choco Drop";
        public string favoriteToy = "Jingle Bell Collar";
        public string speciesDietDesc = "Craves sweet elemental pastries and fresh mountain dew.";

        public bool Feed(PetTreatItem treat, out string feedback)
        {
            if (hunger >= 98f)
            {
                feedback = "*Pats tummy and shakes head* Full and happy! Too stuffed for more treats right now!";
                return false;
            }

            hunger = Mathf.Clamp(hunger + treat.hungerGain, 0f, 100f);
            happiness = Mathf.Clamp(happiness + treat.happinessGain, 0f, 100f);
            totalTimesFed++;

            // Ni no Kuni elemental stat growth
            switch (treat.element.ToLower())
            {
                case "fire": bonusAttack += treat.statGain; break;
                case "water": bonusMagic += treat.statGain; break;
                case "earth": bonusDefense += treat.statGain; break;
                case "wind": bonusSpeed += treat.statGain; break;
                default:
                    bonusAttack += treat.statGain / 2;
                    bonusMagic += treat.statGain / 2;
                    bonusDefense += treat.statGain / 2;
                    bonusSpeed += treat.statGain / 2;
                    break;
            }

            CheckAffectionProgress();

            bool isFavorite = string.Equals(treat.name, favoriteFood, StringComparison.OrdinalIgnoreCase);
            if (isFavorite)
            {
                happiness = Mathf.Clamp(happiness + 20f, 0f, 100f);
                feedback = $"*Eyes sparkle with sheer joy!* FAVORITE FOOD! Munched the delicious {treat.name}! (+{treat.hungerGain:F0}% Fullness, +{treat.statGain} {treat.statBoostType})";
            }
            else
            {
                feedback = $"*Nom nom nom!* Happily munched the tasty {treat.name}! (+{treat.hungerGain:F0}% Fullness, +{treat.statGain} {treat.statBoostType})";
            }

            return true;
        }

        public bool GiveWater(out string feedback)
        {
            if (hydration >= 98f)
            {
                feedback = "*Licks lips happily* Perfectly quenched! Doesn't need more water right now.";
                return false;
            }

            hydration = Mathf.Clamp(hydration + 35f, 0f, 100f);
            happiness = Mathf.Clamp(happiness + 15f, 0f, 100f);

            CheckAffectionProgress();

            feedback = "*Slurp slurp slurp!* Lapped up the crystal-clear mountain spring water! Re-energized and refreshed! (+35% Hydration)";
            return true;
        }

        public bool Bathe(out string feedback)
        {
            cleanliness = 100f;
            happiness = Mathf.Clamp(happiness + 25f, 0f, 100f);
            totalTimesBathed++;

            CheckAffectionProgress();

            feedback = "*Splashes playfully in warm bubbles and shakes off water droplets!* ✨ SQUEAKY CLEAN! ✨ (+Cleanliness 100%, +Affection)";
            return true;
        }

        public bool GivePresent(PetPresentItem present, out string feedback)
        {
            happiness = Mathf.Clamp(happiness + present.happinessGain, 0f, 100f);
            totalPresentsReceived++;

            bool isFavorite = string.Equals(present.name, favoriteToy, StringComparison.OrdinalIgnoreCase);
            if (isFavorite)
            {
                happiness = 100f;
                feedback = $"*Hugs the {present.name} with ecstatic squeaks!* FAVORITE PRESENT! Pure bliss! (+{present.happinessGain:F0}% Joy, +Affection)";
            }
            else
            {
                feedback = $"*Unwraps the {present.name} with joyful chirps!* Loves the wonderful gift! (+{present.happinessGain:F0}% Joy)";
            }

            CheckAffectionProgress();
            return true;
        }

        public bool Pet(out string feedback)
        {
            happiness = Mathf.Clamp(happiness + 10f, 0f, 100f);
            totalPettings++;

            CheckAffectionProgress();

            feedback = "*Leans into your hand, purring and chirping with floating hearts!* Feels deeply loved and cherished! (+Affection)";
            return true;
        }

        private void CheckAffectionProgress()
        {
            int score = (int)(hunger * 0.2f + hydration * 0.2f + cleanliness * 0.25f + happiness * 0.35f);
            if (score >= 90 && totalTimesFed >= 3 && totalTimesBathed >= 1)
            {
                affectionHearts = Mathf.Max(affectionHearts, 5);
            }
            else if (score >= 75 && totalTimesFed >= 2)
            {
                affectionHearts = Mathf.Max(affectionHearts, 4);
            }
            else if (score >= 60)
            {
                affectionHearts = Mathf.Max(affectionHearts, 3);
            }
            else if (score >= 40)
            {
                affectionHearts = Mathf.Max(affectionHearts, 2);
            }
        }

        public string GetAffectionHeartsString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 1; i <= 5; i++)
            {
                if (i <= affectionHearts) sb.Append("<color=#FF3366>♥</color> ");
                else sb.Append("<color=#44475A>♡</color> ");
            }
            return sb.ToString().Trim();
        }

        public string GetPerkDescription()
        {
            return affectionHearts switch
            {
                5 => "<color=#FFD700><b>[BOND PERK: SOULBOUND]</b></color>\nSurvives fatal blows with 1 HP out of love (Pokémon Amie Sturdy) & +25% Critical Hit Chance.",
                4 => "<color=#FF4090><b>[BOND PERK: DEVOTED PAL]</b></color>\n+20% Combat Attack Power & Spawns healing orbs in battle.",
                3 => "<color=#30FF80><b>[BOND PERK: BEST BUDDY]</b></color>\n+15% Movement Speed & Generates Mana Dew in dungeon runs.",
                2 => "<color=#40EBFF><b>[BOND PERK: COMPANION]</b></color>\n+10% Familiar Attack Power.",
                _ => "<color=#A0A5B5><b>[BOND PERK: ACQUAINTANCE]</b></color>\nFeed, bathe, and give presents to deepen bond."
            };
        }
    }

    [System.Serializable]
    public class PetTreatItem
    {
        public string id;
        public string name;
        public string iconSpriteName;
        public string element; // Fire, Water, Earth, Wind, Celestial
        public string statBoostType; // ATK, DEF, MAG, SPD
        public int statGain = 5;
        public float hungerGain = 25f;
        public float happinessGain = 15f;
        public string description;
        public Color themeColor = Color.white;
    }

    [System.Serializable]
    public class PetPresentItem
    {
        public string id;
        public string name;
        public string iconSpriteName;
        public float happinessGain = 35f;
        public int affectionPoints = 20;
        public string description;
        public Color themeColor = Color.white;
    }
}

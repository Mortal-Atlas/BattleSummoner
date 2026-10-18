using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Minigames.Insects
{
    public enum InsectType
    {
        Butterfly,
        Beetle,
        Mantis,
        Firefly
    }

    public enum InsectRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    [Serializable]
    public class InsectSpeciesData
    {
        public string id;
        public string insectName;
        public InsectType type;
        public InsectRarity rarity;
        public int basePrice;
        public string catchPun;
        public Color primaryColor;
        public Color wingColor;
    }

    public static class InsectDatabase
    {
        private static readonly List<InsectSpeciesData> allInsects = new List<InsectSpeciesData>
        {
            new InsectSpeciesData
            {
                id = "common_butterfly",
                insectName = "Common Butterfly",
                type = InsectType.Butterfly,
                rarity = InsectRarity.Common,
                basePrice = 160,
                catchPun = "I caught a Common Butterfly! They say to fly like one, but I'm grounded!",
                primaryColor = new Color(0.2f, 0.2f, 0.2f),
                wingColor = new Color(0.96f, 0.96f, 0.90f)
            },
            new InsectSpeciesData
            {
                id = "monarch_butterfly",
                insectName = "Monarch Butterfly",
                type = InsectType.Butterfly,
                rarity = InsectRarity.Uncommon,
                basePrice = 1400,
                catchPun = "I caught a Monarch Butterfly! Guess that makes me royalty!",
                primaryColor = new Color(0.1f, 0.1f, 0.1f),
                wingColor = new Color(0.95f, 0.45f, 0.08f)
            },
            new InsectSpeciesData
            {
                id = "emperor_butterfly",
                insectName = "Emperor Butterfly",
                type = InsectType.Butterfly,
                rarity = InsectRarity.Rare,
                basePrice = 4000,
                catchPun = "I caught an Emperor Butterfly! It reigns supreme in azure beauty!",
                primaryColor = new Color(0.08f, 0.12f, 0.22f),
                wingColor = new Color(0.15f, 0.70f, 0.95f)
            },
            new InsectSpeciesData
            {
                id = "horned_dynastid",
                insectName = "Horned Dynastid",
                type = InsectType.Beetle,
                rarity = InsectRarity.Common,
                basePrice = 1350,
                catchPun = "I caught a Horned Dynastid! That horn is dynas-tite!",
                primaryColor = new Color(0.28f, 0.16f, 0.10f),
                wingColor = new Color(0.22f, 0.12f, 0.08f)
            },
            new InsectSpeciesData
            {
                id = "golden_stag",
                insectName = "Golden Stag",
                type = InsectType.Beetle,
                rarity = InsectRarity.Legendary,
                basePrice = 12000,
                catchPun = "I caught a Golden Stag! A truly golden opportunity realized!",
                primaryColor = new Color(0.95f, 0.80f, 0.20f),
                wingColor = new Color(1.0f, 0.88f, 0.25f)
            },
            new InsectSpeciesData
            {
                id = "orchid_mantis",
                insectName = "Orchid Mantis",
                type = InsectType.Mantis,
                rarity = InsectRarity.Rare,
                basePrice = 2400,
                catchPun = "I caught an Orchid Mantis! Our paths cross-pollinated!",
                primaryColor = new Color(0.95f, 0.65f, 0.80f),
                wingColor = new Color(0.98f, 0.80f, 0.88f)
            },
            new InsectSpeciesData
            {
                id = "firefly",
                insectName = "Firefly",
                type = InsectType.Firefly,
                rarity = InsectRarity.Uncommon,
                basePrice = 300,
                catchPun = "I caught a Firefly! It really brightened up my evening!",
                primaryColor = new Color(0.20f, 0.20f, 0.18f),
                wingColor = new Color(0.75f, 1.0f, 0.25f)
            }
        };

        public static IReadOnlyList<InsectSpeciesData> AllInsects => allInsects;

        public static InsectSpeciesData GetRandom(InsectType? type = null)
        {
            if (type.HasValue)
            {
                var matches = allInsects.FindAll(i => i.type == type.Value);
                if (matches.Count > 0) return matches[UnityEngine.Random.Range(0, matches.Count)];
            }
            return allInsects[UnityEngine.Random.Range(0, allInsects.Count)];
        }

        public static InsectSpeciesData GetById(string id)
        {
            return allInsects.Find(i => i.id == id) ?? allInsects[0];
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Minigames.Fishing
{
    public enum FishShadowSize
    {
        Tiny,
        Small,
        Medium,
        Large,
        Huge,
        Finned
    }

    public enum FishRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    [Serializable]
    public class FishSpeciesData
    {
        public string id;
        public string speciesName;
        public FishShadowSize shadowSize;
        public FishRarity rarity;
        public float minLengthCm;
        public float maxLengthCm;
        public int basePrice;
        public string catchPun;
        public Color fishColor;

        public float GenerateRandomLength()
        {
            return UnityEngine.Random.Range(minLengthCm, maxLengthCm);
        }
    }

    public static class FishDatabase
    {
        private static readonly List<FishSpeciesData> allSpecies = new List<FishSpeciesData>
        {
            new FishSpeciesData
            {
                id = "pale_chub",
                speciesName = "Pale Chub",
                shadowSize = FishShadowSize.Tiny,
                rarity = FishRarity.Common,
                minLengthCm = 12.0f,
                maxLengthCm = 17.5f,
                basePrice = 200,
                catchPun = "I caught a Pale Chub! That name sounds a bit judgmental...",
                fishColor = new Color(0.85f, 0.88f, 0.90f)
            },
            new FishSpeciesData
            {
                id = "goldfish",
                speciesName = "Pop-eyed Goldfish",
                shadowSize = FishShadowSize.Tiny,
                rarity = FishRarity.Uncommon,
                minLengthCm = 14.0f,
                maxLengthCm = 19.0f,
                basePrice = 1300,
                catchPun = "I caught a Pop-eyed Goldfish! It looks surprised to see me!",
                fishColor = new Color(0.95f, 0.40f, 0.15f)
            },
            new FishSpeciesData
            {
                id = "crucian_carp",
                speciesName = "Crucian Carp",
                shadowSize = FishShadowSize.Small,
                rarity = FishRarity.Common,
                minLengthCm = 20.0f,
                maxLengthCm = 28.0f,
                basePrice = 160,
                catchPun = "I caught a Crucian Carp! My skills are definitely not subpar!",
                fishColor = new Color(0.70f, 0.65f, 0.50f)
            },
            new FishSpeciesData
            {
                id = "black_bass",
                speciesName = "Black Bass",
                shadowSize = FishShadowSize.Medium,
                rarity = FishRarity.Common,
                minLengthCm = 45.0f,
                maxLengthCm = 62.0f,
                basePrice = 400,
                catchPun = "I caught a Black Bass! The most metal fish in the stream!",
                fishColor = new Color(0.25f, 0.35f, 0.22f)
            },
            new FishSpeciesData
            {
                id = "red_snapper",
                speciesName = "Red Snapper",
                shadowSize = FishShadowSize.Medium,
                rarity = FishRarity.Uncommon,
                minLengthCm = 50.0f,
                maxLengthCm = 75.0f,
                basePrice = 3000,
                catchPun = "I caught a Red Snapper! It's looking pretty dapper!",
                fishColor = new Color(0.92f, 0.25f, 0.28f)
            },
            new FishSpeciesData
            {
                id = "sea_bass",
                speciesName = "Sea Bass",
                shadowSize = FishShadowSize.Large,
                rarity = FishRarity.Common,
                minLengthCm = 90.0f,
                maxLengthCm = 115.0f,
                basePrice = 400,
                catchPun = "I caught a Sea Bass! No, wait—it's at least a C+!",
                fishColor = new Color(0.40f, 0.48f, 0.45f)
            },
            new FishSpeciesData
            {
                id = "koi",
                speciesName = "Koi",
                shadowSize = FishShadowSize.Large,
                rarity = FishRarity.Rare,
                minLengthCm = 60.0f,
                maxLengthCm = 85.0f,
                basePrice = 4000,
                catchPun = "I caught a Koi! I don't know why it's acting so coy!",
                fishColor = new Color(0.95f, 0.65f, 0.20f)
            },
            new FishSpeciesData
            {
                id = "golden_trout",
                speciesName = "Golden Trout",
                shadowSize = FishShadowSize.Medium,
                rarity = FishRarity.Legendary,
                minLengthCm = 45.0f,
                maxLengthCm = 65.0f,
                basePrice = 15000,
                catchPun = "I caught a Golden Trout! Look at that majestic shimmering luster!",
                fishColor = new Color(1.0f, 0.85f, 0.20f)
            },
            new FishSpeciesData
            {
                id = "oarfish",
                speciesName = "Oarfish",
                shadowSize = FishShadowSize.Huge,
                rarity = FishRarity.Legendary,
                minLengthCm = 280.0f,
                maxLengthCm = 360.0f,
                basePrice = 9000,
                catchPun = "I caught an Oarfish! I'm shore glad I reeled this giant in!",
                fishColor = new Color(0.85f, 0.90f, 0.95f)
            },
            new FishSpeciesData
            {
                id = "coelacanth",
                speciesName = "Coelacanth",
                shadowSize = FishShadowSize.Finned,
                rarity = FishRarity.Legendary,
                minLengthCm = 135.0f,
                maxLengthCm = 175.0f,
                basePrice = 15000,
                catchPun = "I caught a Coelacanth! Think positive! Be a coela-CAN-th!",
                fishColor = new Color(0.18f, 0.22f, 0.35f)
            }
        };

        public static IReadOnlyList<FishSpeciesData> AllSpecies => allSpecies;

        public static FishSpeciesData GetRandomFish(FishShadowSize size)
        {
            var matching = allSpecies.FindAll(f => f.shadowSize == size);
            if (matching.Count == 0) return allSpecies[UnityEngine.Random.Range(0, allSpecies.Count)];
            return matching[UnityEngine.Random.Range(0, matching.Count)];
        }

        public static FishSpeciesData GetById(string id)
        {
            return allSpecies.Find(f => f.id == id) ?? allSpecies[0];
        }
    }
}

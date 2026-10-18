using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Minigames.Gardening
{
    public enum PlantCategory
    {
        Flower,
        ProduceCrop,
        AlchemyHerb
    }

    [Serializable]
    public class PlantSeedData
    {
        public string id;
        public string plantName;
        public PlantCategory category;
        public float baseGrowthSeconds; // Time to grow to maturity
        public int harvestYieldCount;
        public int marketValue;
        public Color bloomColor;
        public Color foliageColor;
        public string harvestItemName;
    }

    public static class PlantDatabase
    {
        private static readonly List<PlantSeedData> allPlants = new List<PlantSeedData>
        {
            new PlantSeedData
            {
                id = "red_rose",
                plantName = "Crimson Rose",
                category = PlantCategory.Flower,
                baseGrowthSeconds = 12.0f,
                harvestYieldCount = 3,
                marketValue = 240,
                bloomColor = new Color(0.92f, 0.15f, 0.22f),
                foliageColor = new Color(0.18f, 0.45f, 0.16f),
                harvestItemName = "Crimson Rose Bouquet"
            },
            new PlantSeedData
            {
                id = "golden_cosmos",
                plantName = "Golden Cosmos",
                category = PlantCategory.Flower,
                baseGrowthSeconds = 14.0f,
                harvestYieldCount = 3,
                marketValue = 350,
                bloomColor = new Color(1.0f, 0.82f, 0.18f),
                foliageColor = new Color(0.25f, 0.52f, 0.20f),
                harvestItemName = "Golden Cosmos Petals"
            },
            new PlantSeedData
            {
                id = "astral_orchid",
                plantName = "Astral Orchid",
                category = PlantCategory.Flower,
                baseGrowthSeconds = 20.0f,
                harvestYieldCount = 2,
                marketValue = 1200,
                bloomColor = new Color(0.70f, 0.35f, 0.95f),
                foliageColor = new Color(0.12f, 0.32f, 0.38f),
                harvestItemName = "Astral Orchid Bloom"
            },
            new PlantSeedData
            {
                id = "strawberry",
                plantName = "Sweet Strawberry",
                category = PlantCategory.ProduceCrop,
                baseGrowthSeconds = 15.0f,
                harvestYieldCount = 4,
                marketValue = 480,
                bloomColor = new Color(0.95f, 0.22f, 0.30f),
                foliageColor = new Color(0.20f, 0.55f, 0.18f),
                harvestItemName = "Sweet Strawberries"
            },
            new PlantSeedData
            {
                id = "pumpkin",
                plantName = "Sun Pumpkin",
                category = PlantCategory.ProduceCrop,
                baseGrowthSeconds = 18.0f,
                harvestYieldCount = 2,
                marketValue = 750,
                bloomColor = new Color(0.95f, 0.55f, 0.10f),
                foliageColor = new Color(0.25f, 0.48f, 0.15f),
                harvestItemName = "Ripe Sun Pumpkin"
            },
            new PlantSeedData
            {
                id = "frost_mint",
                plantName = "Frost Mint",
                category = PlantCategory.AlchemyHerb,
                baseGrowthSeconds = 10.0f,
                harvestYieldCount = 5,
                marketValue = 300,
                bloomColor = new Color(0.55f, 0.90f, 0.95f),
                foliageColor = new Color(0.30f, 0.65f, 0.60f),
                harvestItemName = "Chilled Frost Mint"
            }
        };

        public static IReadOnlyList<PlantSeedData> AllPlants => allPlants;

        public static PlantSeedData GetById(string id)
        {
            return allPlants.Find(p => p.id == id) ?? allPlants[0];
        }

        public static PlantSeedData GetRandom()
        {
            return allPlants[UnityEngine.Random.Range(0, allPlants.Count)];
        }
    }
}

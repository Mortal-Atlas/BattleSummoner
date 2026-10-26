using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.WorldActivities
{
    public enum WorldPOIType
    {
        SatelliteVillage,
        FullInfoElementalTemple,
        CozyFarmhouseHomestead,
        GuildWatchtowerOutpost,
        EnemyGolemPortalOutpost,
        SkyIslandWindmillFarm,
        SkyIslandGravityFlipCavern,
        VerticalCargoElevatorHeist
    }

    /// <summary>
    /// Master runtime controller for Surface & Sky Points of Interest.
    /// Enforces Egoraptor's Sequelitis & Barry Kramer's How About This Game laws across all POIs:
    /// - Auto-clears overlapping forest trees around every Village, Farmhouse, Temple & Outpost.
    /// - Harvests Farmhouse & Sky-Farm crops via Selective Z-Axis Blade Sweeps -> yields Cooking Mama ingredients & Insta-Vine Seeds.
    /// - Runs Vertical Cargo Elevator Heists between the Ground Continent (+80m) and Sky City Aetherion (+840m).
    /// - Enables Mario Galaxy Upside-Down Gravity-Flip Geode Mining under all 12 Sky Islands.
    /// </summary>
    public class WorldAndSkyActivityDirector : MonoBehaviour
    {
        public static WorldAndSkyActivityDirector Instance { get; private set; }

        [Header("Auto-Deforestation Around Settlements & Roads")]
        public Terrain activeContinentTerrain;
        public float settlementTreeClearanceRadius = 38f;

        [Header("Farmhouse & Sky-Farm Crop Yields (Cooking Mama & Death Stranding Seeds)")]
        public string[] harvestableCookingIngredients = {
            "Golden Spire Wheat", "Sky-Saffron Petal", "Aether-Berry Cluster", "Frosthold Garlic", "Insta-Vine Climbing Seed"
        };

        [Header("Vertical Cargo Elevator Heists (Ground +80m <-> Sky +840m)")]
        public List<Transform> activeCargoElevatorPlatforms = new List<Transform>();
        public float elevatorVerticalSpeed = 14.5f;
        public float elevatorMinHeight = 80f;
        public float elevatorMaxHeight = 840f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            AutoRegisterAndClearTreesAroundAllPOIs();
        }

        private void Update()
        {
            UpdateVerticalCargoElevators();
        }

        /// <summary>
        /// Ensures vegetation never clips inside newly placed Villages, Temples, Farmhouses, or Outposts.
        /// </summary>
        public void AutoRegisterAndClearTreesAroundAllPOIs()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject go in allObjects)
            {
                if (go.name.EndsWith("_Root") &&
                    (go.name.StartsWith("Village_") || go.name.StartsWith("Temple_") ||
                     go.name.StartsWith("Farmhouse_") || go.name.StartsWith("Outpost_") ||
                     go.name.Contains("_CloudCatcherFarm") || go.name.Contains("_CelestialWindTemple") ||
                     go.name.Contains("_AerialGolemOutpost")))
                {
                    ClearVegetationInFootprint(go.transform.position, settlementTreeClearanceRadius);
                }
                else if (go.name.StartsWith("SkyElevatorHeist_"))
                {
                    if (!activeCargoElevatorPlatforms.Contains(go.transform))
                    {
                        activeCargoElevatorPlatforms.Add(go.transform);
                    }
                }
            }
        }

        public void ClearVegetationInFootprint(Vector3 worldCenter, float radius)
        {
            // 1. Clear prefab trees/bushes on Foliage layer inside building footprint
            Collider[] overlaps = Physics.OverlapSphere(worldCenter, radius);
            foreach (var col in overlaps)
            {
                if (col == null || col.gameObject == null) continue;
                string n = col.gameObject.name.ToLowerInvariant();
                if (n.Contains("tree") || n.Contains("bush") || n.Contains("pine") || n.Contains("oak"))
                {
                    col.gameObject.SetActive(false);
                }
            }

            // 2. Clear Unity Terrain tree instances if present
            if (activeContinentTerrain == null || activeContinentTerrain.terrainData == null) return;
            TerrainData tData = activeContinentTerrain.terrainData;
            Vector3 tPos = activeContinentTerrain.transform.position;
            List<TreeInstance> keptTrees = new List<TreeInstance>(tData.treeInstanceCount);

            foreach (TreeInstance tree in tData.treeInstances)
            {
                Vector3 treeWorld = Vector3.Scale(tree.position, tData.size) + tPos;
                if (Vector2.Distance(new Vector2(treeWorld.x, treeWorld.z), new Vector2(worldCenter.x, worldCenter.z)) > radius)
                {
                    keptTrees.Add(tree);
                }
            }
            tData.SetTreeInstances(keptTrees.ToArray(), true);
        }

        /// <summary>
        /// Called when the player sweeps their blade through a Farmhouse or Sky-Island Crop Plot.
        /// Uses Zelda Sequelitis instant gratification: one wide slash harvests ingredients + Insta-Vine Seeds with zero cutscene delay!
        /// </summary>
        public void OnCropPlotSliced(GameObject cropPlotObject, Vector3 slashDirection)
        {
            string ingredient = harvestableCookingIngredients[UnityEngine.Random.Range(0, harvestableCookingIngredients.Length)];
            int count = UnityEngine.Random.Range(2, 5);
            Debug.Log($"[Cozy Farmhouse Harvest] Cleaved {cropPlotObject.name} -> Instantly gathered {count}x [{ingredient}] for Cooking Mama & Climber Vine Ladders!");
        }

        /// <summary>
        /// Triggered when the player steps into or flies through a Sky Island's *_GravityFlipRuneRing.
        /// Implements Barry Kramer's Mario Galaxy rule: flips personal gravity so the player walks upside-down
        /// on the Sky Island's rocky underside to mine glowing 4D Buoyancy Geodes!
        /// </summary>
        public void OnSkyIslandGravityRingTriggered(GameObject playerActor)
        {
            if (playerActor == null) return;
            var movement = playerActor.GetComponent<BattleSummoner.DesignPhilosophy.ExpressiveMovementAndSlopeDashController>();
            if (movement == null)
            {
                movement = playerActor.AddComponent<BattleSummoner.DesignPhilosophy.ExpressiveMovementAndSlopeDashController>();
            }
            if (movement != null)
            {
                movement.TogglePersonalGravityFlip();
            }
        }

        /// <summary>
        /// Animates the 4 Vertical Spire-to-Sky Cargo Elevator chains so players can grapple onto them mid-flight
        /// for high-speed vertical wind-tunnel heists!
        /// </summary>
        private void UpdateVerticalCargoElevators()
        {
            float span = elevatorMaxHeight - elevatorMinHeight;
            if (span <= 1f) return;

            for (int i = 0; i < activeCargoElevatorPlatforms.Count; i++)
            {
                Transform plat = activeCargoElevatorPlatforms[i];
                if (plat == null) continue;

                Vector3 pos = plat.position;
                pos.y += elevatorVerticalSpeed * Time.deltaTime;
                if (pos.y > elevatorMaxHeight)
                {
                    pos.y = elevatorMinHeight;
                }
                plat.position = pos;
            }
        }
    }
}

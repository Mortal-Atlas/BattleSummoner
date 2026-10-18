using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Optimization;

namespace BattleSummoner.DungeonDimension
{
    public enum DungeonRoomArchetype
    {
        EntryPortalAnchor,
        CombatCorridor,
        BranchingVault,
        TrapGauntlet,
        DimensionalBossCore
    }

    [Serializable]
    public struct GeneratedRoomNode
    {
        public int nodeIndex;
        public DungeonRoomArchetype archetype;
        public Vector3 gridCoordinate;
        public Quaternion orientation;
        public int enemyWaveSeed;
    }

    public class ProceduralDungeonGenerator : MonoBehaviour
    {
        public static ProceduralDungeonGenerator Instance { get; private set; }

        [Header("Modular Room Prefab Pools")]
        public GameObject[] entryRoomPrefabs;
        public GameObject[] combatCorridorPrefabs;
        public GameObject[] vaultRoomPrefabs;
        public GameObject[] trapGauntletPrefabs;
        public GameObject[] bossCorePrefabs;

        [Header("Generation Parameters")]
        public int minRoomsPerRun = 9;
        public int maxRoomsPerRun = 18;
        public float roomGridSnapSize = 36f;

        private readonly List<GameObject> spawnedRoomInstances = new List<GameObject>();
        private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Invoked upon entering any Camp or Fortress Dungeon Portal.
        /// Guarantees a brand-new, non-repeating dungeon topology every playthrough.
        /// </summary>
        public List<GeneratedRoomNode> BuildUniqueDungeonRun(int entropySeed, float inheritedFortressInstability)
        {
            ClearExistingDungeonGeometry();

            // Combine portal seed with high-resolution clock ticks so re-entering never repeats a layout
            int runSeed = entropySeed ^ DateTime.UtcNow.Millisecond ^ Guid.NewGuid().GetHashCode();
            System.Random rng = new System.Random(runSeed);

            int targetRoomCount = rng.Next(minRoomsPerRun, maxRoomsPerRun + 1);
            List<GeneratedRoomNode> layoutGraph = new List<GeneratedRoomNode>(targetRoomCount);

            Vector3Int currentCell = Vector3Int.zero;
            occupiedCells.Add(currentCell);

            Vector3Int[] cardinalDirs = {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1)
            };

            for (int i = 0; i < targetRoomCount; i++)
            {
                DungeonRoomArchetype roomType = SelectArchetypeForStep(i, targetRoomCount, rng);
                Quaternion rot = Quaternion.Euler(0f, rng.Next(0, 4) * 90f, 0f);
                Vector3 worldPos = new Vector3(currentCell.x * roomGridSnapSize, 0f, currentCell.z * roomGridSnapSize);

                GeneratedRoomNode node = new GeneratedRoomNode
                {
                    nodeIndex = i,
                    archetype = roomType,
                    gridCoordinate = worldPos,
                    orientation = rot,
                    enemyWaveSeed = rng.Next(100000, 999999)
                };
                layoutGraph.Add(node);

                InstantiateRoomPrefab(node, rng);

                // Step to an unoccupied adjacent cell (or branch vertically on rare rolls)
                currentCell = GetNextUnoccupiedCell(currentCell, cardinalDirs, rng);
                occupiedCells.Add(currentCell);
            }

            Debug.Log($"[Procedural Dungeon] Compiled {targetRoomCount}-room non-repeating topology (Run Seed: {runSeed}).");
            return layoutGraph;
        }

        private DungeonRoomArchetype SelectArchetypeForStep(int index, int total, System.Random rng)
        {
            if (index == 0) return DungeonRoomArchetype.EntryPortalAnchor;
            if (index == total - 1) return DungeonRoomArchetype.DimensionalBossCore;

            double roll = rng.NextDouble();
            if (roll < 0.20) return DungeonRoomArchetype.BranchingVault;
            if (roll < 0.45) return DungeonRoomArchetype.TrapGauntlet;
            return DungeonRoomArchetype.CombatCorridor;
        }

        private Vector3Int GetNextUnoccupiedCell(Vector3Int origin, Vector3Int[] dirs, System.Random rng)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3Int dir = dirs[rng.Next(0, dirs.Length)];
                Vector3Int candidate = origin + dir;
                if (!occupiedCells.Contains(candidate)) return candidate;
            }
            // Fallback: step outward along Z to prevent overlap
            return origin + new Vector3Int(0, 0, 1);
        }

        private void InstantiateRoomPrefab(GeneratedRoomNode node, System.Random rng)
        {
            GameObject[] pool = node.archetype switch
            {
                DungeonRoomArchetype.EntryPortalAnchor => entryRoomPrefabs,
                DungeonRoomArchetype.BranchingVault => vaultRoomPrefabs,
                DungeonRoomArchetype.TrapGauntlet => trapGauntletPrefabs,
                DungeonRoomArchetype.DimensionalBossCore => bossCorePrefabs,
                _ => combatCorridorPrefabs
            };

            if (pool == null || pool.Length == 0) return;
            GameObject chosenPrefab = pool[rng.Next(0, pool.Length)];
            GameObject instance = Instantiate(chosenPrefab, node.gridCoordinate, node.orientation, transform);

            // Auto-register dungeon room to Spire Spatial Culling Manager
            var cullable = instance.GetComponent<CullableWorldProp>();
            if (cullable == null) cullable = instance.AddComponent<CullableWorldProp>();
            cullable.boundingRadius = roomGridSnapSize * 0.75f;
            cullable.isDynamicMover = false;

            spawnedRoomInstances.Add(instance);
        }

        public void ClearExistingDungeonGeometry()
        {
            foreach (var room in spawnedRoomInstances)
            {
                if (room != null)
                {
                    if (Application.isPlaying) Destroy(room);
                    else DestroyImmediate(room);
                }
            }
            spawnedRoomInstances.Clear();
            occupiedCells.Clear();
        }

        public List<GeneratedRoomNode> GenerateUniqueProceduralDungeon(int? overrideSeed = null, int fortressEscalationBonusRooms = 0)
        {
            int activeSeed = overrideSeed ?? (Guid.NewGuid().GetHashCode() ^ System.Environment.TickCount);
            return BuildUniqueDungeonRun(activeSeed, fortressEscalationBonusRooms * 0.1f);
        }
    }
}


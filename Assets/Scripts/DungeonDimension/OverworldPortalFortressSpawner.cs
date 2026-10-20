using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BattleSummoner.Optimization;

namespace BattleSummoner.DungeonDimension
{
    public enum PortalEscalationState
    {
        Stage0_PortalOnly,
        Stage1_EnemyCamp,
        Stage2_Fortress,
        Stage3_TownRaidActive
    }

    public class OverworldPortalFortressSpawner : MonoBehaviour
    {
        [Header("Kingdom Boundary Constraints")]
        public Vector3 kingdomCenter = Vector3.zero;
        [Tooltip("Portals are strictly forbidden from spawning inside this radius.")]
        public float kingdomWallOuterRadius = 480f;
        public float maxOverworldSpawnRadius = 1800f;

        [Header("Forest Deforestation Settings")]
        public float forestClearingRadius = 28f;
        public LayerMask treeAndFoliageLayer;
        public Terrain activeOverworldTerrain;

        [Header("Escalation Timers (Seconds)")]
        public float timeToBecomeCamp = 300f;       // 5 mins unchecked -> Camp
        public float timeToBecomeFortress = 900f;   // 15 mins unchecked -> Fortress
        public float timeToLaunchTownRaid = 1500f;  // 25 mins unchecked -> Raids Kingdom Walls

        [Header("Prefabs")]
        public GameObject dungeonPortalPrefab;
        public GameObject campStructurePrefab;
        public GameObject fortressStructurePrefab;
        public GameObject townRaidSiegeWavePrefab;

        [Header("Live Runtime Telemetry")]
        public PortalEscalationState currentState = PortalEscalationState.Stage0_PortalOnly;
        public float uncheckedTimerSeconds = 0f;
        public int currentProceduralDungeonSeed;

        private GameObject activeCampInstance;
        private GameObject activeFortressInstance;
        private float raidWaveCooldown = 0f;

        private void Start()
        {
            GenerateFreshDungeonSeed();
        }

        [ContextMenu("Spawn Portal Outside Kingdom Walls")]
        public void SpawnPortalInOverworld()
        {
            Vector3 spawnPos = SampleExteriorSpawnCoordinate();
            transform.position = spawnPos;

            ClearForestFoliageAtFootprint(spawnPos, forestClearingRadius);
            GenerateFreshDungeonSeed();

            currentState = PortalEscalationState.Stage0_PortalOnly;
            uncheckedTimerSeconds = 0f;

            if (dungeonPortalPrefab != null)
            {
                Instantiate(dungeonPortalPrefab, transform.position, Quaternion.identity, transform);
            }
        }

        /// <summary>
        /// Guarantees spawn position is strictly outside the Kingdom Perimeter Walls.
        /// </summary>
        public Vector3 SampleExteriorSpawnCoordinate()
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = UnityEngine.Random.Range(kingdomWallOuterRadius + 45f, maxOverworldSpawnRadius);

            Vector3 candidate = new Vector3(
                kingdomCenter.x + Mathf.Cos(angle) * distance,
                kingdomCenter.y,
                kingdomCenter.z + Mathf.Sin(angle) * distance
            );

            if (activeOverworldTerrain != null)
            {
                candidate.y = activeOverworldTerrain.SampleHeight(candidate) + activeOverworldTerrain.GetPosition().y;
            }
            return candidate;
        }

        private static readonly Collider[] _foliageOverlapBuffer = new Collider[64];

        /// <summary>
        /// Removes both GameObject trees and Unity Terrain TreeInstances if spawned inside a forest (Zero-Alloc).
        /// </summary>
        public void ClearForestFoliageAtFootprint(Vector3 center, float radius)
        {
            // 1. Remove prefab/collider trees on the foliage layer (NonAlloc)
            int hitCount = Physics.OverlapSphereNonAlloc(center, radius, _foliageOverlapBuffer, treeAndFoliageLayer);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _foliageOverlapBuffer[i];
                if (hit != null)
                {
                    if (Application.isPlaying) Destroy(hit.gameObject);
                    else DestroyImmediate(hit.gameObject);
                    _foliageOverlapBuffer[i] = null;
                }
            }

            // 2. Strip Terrain Data TreeInstances within the normalized radius
            if (activeOverworldTerrain != null && activeOverworldTerrain.terrainData != null)
            {
                TerrainData tData = activeOverworldTerrain.terrainData;
                Vector3 terrainPos = activeOverworldTerrain.GetPosition();
                List<TreeInstance> survivingTrees = new List<TreeInstance>();

                foreach (TreeInstance tree in tData.treeInstances)
                {
                    Vector3 worldTreePos = Vector3.Scale(tree.position, tData.size) + terrainPos;
                    if (Vector2.Distance(new Vector2(worldTreePos.x, worldTreePos.z), new Vector2(center.x, center.z)) > radius)
                    {
                        survivingTrees.Add(tree);
                    }
                }
                tData.treeInstances = survivingTrees.ToArray();
            }
        }

        private void Update()
        {
            uncheckedTimerSeconds += Time.deltaTime;
            EvaluateEscalationStateMachine();

            if (currentState == PortalEscalationState.Stage3_TownRaidActive)
            {
                TickTownRaidInvasion();
            }
        }

        private void EvaluateEscalationStateMachine()
        {
            if (uncheckedTimerSeconds >= timeToLaunchTownRaid && currentState != PortalEscalationState.Stage3_TownRaidActive)
            {
                TransitionToState(PortalEscalationState.Stage3_TownRaidActive);
            }
            else if (uncheckedTimerSeconds >= timeToBecomeFortress && currentState < PortalEscalationState.Stage2_Fortress)
            {
                TransitionToState(PortalEscalationState.Stage2_Fortress);
            }
            else if (uncheckedTimerSeconds >= timeToBecomeCamp && currentState < PortalEscalationState.Stage1_EnemyCamp)
            {
                TransitionToState(PortalEscalationState.Stage1_EnemyCamp);
            }
        }

        private void TransitionToState(PortalEscalationState nextState)
        {
            currentState = nextState;

            switch (nextState)
            {
                case PortalEscalationState.Stage1_EnemyCamp:
                    ClearForestFoliageAtFootprint(transform.position, forestClearingRadius * 1.25f);
                    if (campStructurePrefab != null)
                    {
                        activeCampInstance = Instantiate(campStructurePrefab, transform.position, Quaternion.identity, transform);
                        var prop = activeCampInstance.GetComponent<CullableWorldProp>() ?? activeCampInstance.AddComponent<CullableWorldProp>();
                        prop.boundingRadius = forestClearingRadius * 1.25f;
                        prop.isDynamicMover = false;
                    }
                    break;

                case PortalEscalationState.Stage2_Fortress:
                    if (activeCampInstance != null) Destroy(activeCampInstance);
                    ClearForestFoliageAtFootprint(transform.position, forestClearingRadius * 1.8f);
                    if (fortressStructurePrefab != null)
                    {
                        activeFortressInstance = Instantiate(fortressStructurePrefab, transform.position, Quaternion.identity, transform);
                        var prop = activeFortressInstance.GetComponent<CullableWorldProp>() ?? activeFortressInstance.AddComponent<CullableWorldProp>();
                        prop.boundingRadius = forestClearingRadius * 1.8f;
                        prop.isDynamicMover = false;
                    }
                    break;

                case PortalEscalationState.Stage3_TownRaidActive:
                    DispatchTownRaidAlert();
                    break;
            }
        }

        private void TickTownRaidInvasion()
        {
            raidWaveCooldown -= Time.deltaTime;
            if (raidWaveCooldown <= 0f)
            {
                raidWaveCooldown = 45f; // Dispatch a new marching siege wave every 45 seconds
                SpawnMarchingRaidWave();
            }
        }

        private void SpawnMarchingRaidWave()
        {
            if (townRaidSiegeWavePrefab == null) return;

            GameObject wave = Instantiate(townRaidSiegeWavePrefab, transform.position, Quaternion.identity);
            if (wave.TryGetComponent<NavMeshAgent>(out var agent))
            {
                // Direct the raid squad straight toward the Kingdom Perimeter Gate
                Vector3 dirToTown = (kingdomCenter - transform.position).normalized;
                Vector3 gateTarget = kingdomCenter - (dirToTown * kingdomWallOuterRadius);
                agent.SetDestination(gateTarget);
            }
        }

        private void DispatchTownRaidAlert()
        {
            Debug.LogWarning("[TOWN RAID ALERT] An unchecked Fortress has launched a full invasion toward the Kingdom Walls!");
            if (World.MagicCityUtilityAndTransitGrid.Instance != null)
            {
                World.MagicCityUtilityAndTransitGrid.Instance.SetPerimeterManaShieldSiegeState(true);
            }
        }

        public void GenerateFreshDungeonSeed()
        {
            currentProceduralDungeonSeed = Guid.NewGuid().GetHashCode() ^ System.Environment.TickCount;
        }
    }
}

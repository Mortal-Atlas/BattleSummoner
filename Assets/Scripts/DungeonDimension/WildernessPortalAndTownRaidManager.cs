using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Core;

namespace BattleSummoner.DungeonDimension
{
    /// <summary>
    /// Section 4: Wilderness Portal Spawning (Forest Tree Clearing), Procedural Dungeons & Town Raid Invasions.
    /// Manages spawning portals outside kingdom walls, dynamic forest tree clearing, and portal escalation.
    /// </summary>
    public class WildernessPortalAndTownRaidManager : MonoBehaviour
    {
        public static WildernessPortalAndTownRaidManager Instance { get; private set; }

        [Header("Wilderness Spawn Ring (Outside Kingdom Walls)")]
        public Vector3 kingdomCenter = Vector3.zero;
        public float kingdomWallRadius = 460f;
        public float maxWildernessSpawnRadius = 950f;
        public float forestTreeClearingRadius = 42f;
        public LayerMask treePrefabLayerMask;
        public LayerMask groundTerrainLayerMask;

        [Header("References")]
        public GameObject wildernessPortalNodePrefab;
        public Transform nearestKingdomGateTarget;
        public List<PortalOutpostFortressNode> activeWildernessPortals = new List<PortalOutpostFortressNode>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ContextMenu("Spawn Wilderness Portal Outside Kingdom Walls")]
        public PortalOutpostFortressNode SpawnPortalOutsideKingdomWalls()
        {
            float angleRad = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float dist = UnityEngine.Random.Range(kingdomWallRadius + 60f, maxWildernessSpawnRadius);
            Vector3 candidatePos = kingdomCenter + new Vector3(Mathf.Cos(angleRad) * dist, 200f, Mathf.Sin(angleRad) * dist);

            if (Physics.Raycast(candidatePos, Vector3.down, out RaycastHit hit, 500f, groundTerrainLayerMask))
            {
                candidatePos = hit.point;
            }
            else
            {
                candidatePos.y = 0f;
            }

            ClearForestTreesInArea(candidatePos, forestTreeClearingRadius);

            GameObject nodeObj;
            if (wildernessPortalNodePrefab != null)
            {
                nodeObj = Instantiate(wildernessPortalNodePrefab, candidatePos, Quaternion.identity, transform);
            }
            else
            {
                nodeObj = new GameObject($"WildPortal_{activeWildernessPortals.Count + 1}");
                nodeObj.transform.position = candidatePos;
                nodeObj.transform.SetParent(transform, true);
                nodeObj.AddComponent<PortalOutpostFortressNode>();
            }

            var portalNode = nodeObj.GetComponent<PortalOutpostFortressNode>();
            if (portalNode != null)
            {
                portalNode.portalId = $"WildPortal_{activeWildernessPortals.Count + 1}";
                activeWildernessPortals.Add(portalNode);
            }

            Debug.Log($"[Wilderness Portal Spawner] Cleared forest trees and spawned Dungeon Portal at {candidatePos} (Outside Kingdom Walls).");
            return portalNode;
        }

        public void ClearForestTreesInArea(Vector3 centerWorldPos, float clearRadius)
        {
            Collider[] treeColliders = Physics.OverlapSphere(centerWorldPos, clearRadius, treePrefabLayerMask);
            int removedPrefabs = 0;
            foreach (var col in treeColliders)
            {
                Destroy(col.gameObject);
                removedPrefabs++;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.terrainData != null)
            {
                TerrainData tData = terrain.terrainData;
                List<TreeInstance> survivingTrees = new List<TreeInstance>(tData.treeInstanceCount);

                foreach (var tree in tData.treeInstances)
                {
                    Vector3 treeWorldPos = Vector3.Scale(tree.position, tData.size) + terrain.transform.position;
                    float horizontalDist = Vector2.Distance(
                        new Vector2(treeWorldPos.x, treeWorldPos.z),
                        new Vector2(centerWorldPos.x, centerWorldPos.z));

                    if (horizontalDist > clearRadius)
                    {
                        survivingTrees.Add(tree);
                    }
                }

                tData.SetTreeInstances(survivingTrees.ToArray(), true);
            }

            Debug.Log($"[Forest Deforestation] Cleared trees within {clearRadius}m of {centerWorldPos} ({removedPrefabs} tree objects removed) to make room for Portal & Fortress.");
        }
    }
}

using System;
using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    /// <summary>
    /// DungeonPortalTrigger detects player collision to enter or exit the Dungeon Dimension.
    /// Blocks entry if a Fortress siege gate is obstructing the portal, and passes inherited
    /// starting instability from unchecked portal incursions.
    /// </summary>
    public class DungeonPortalTrigger : MonoBehaviour
    {
        [Header("Portal Configuration")]
        public bool isEntrancePortal = true;
        public PortalOutpostFortressNode linkedOutpostNode;
        [Header("Procedural Dungeon & Spawner Handshake")]
        public OverworldPortalFortressSpawner linkedSpawner;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (isEntrancePortal)
            {
                if (linkedOutpostNode != null && linkedOutpostNode.isPortalEntranceBlockedByFortress)
                {
                    Debug.LogWarning("⛔ [PORTAL BLOCKED] Incursion Fortress Gate is sealing the portal entrance! Breach the gate first.");
                    if (DevClassLoadoutManager.Instance != null)
                    {
                        DevClassLoadoutManager.Instance.ShowToast("<color=#FF4444>[PORTAL BLOCKED]</color>\nBreach the Fortress Gate first!");
                    }
                    return;
                }

                float startInstability = linkedOutpostNode != null ? linkedOutpostNode.GetInheritedStartingInstability() : 1.0f;
                int dungeonSeed = linkedSpawner != null 
                    ? linkedSpawner.currentProceduralDungeonSeed 
                    : (Guid.NewGuid().GetHashCode() ^ System.Environment.TickCount);

                // Build unique, non-repeating procedural dungeon topology for this playthrough
                if (ProceduralDungeonGenerator.Instance != null)
                {
                    ProceduralDungeonGenerator.Instance.BuildUniqueDungeonRun(dungeonSeed, startInstability);
                }

                DungeonInstabilityManager.Instance?.EnterDungeonDimension(1, startInstability);
            }
            else
            {
                DungeonInstabilityManager.Instance?.ExitDungeonDimension();
            }
        }
    }
}

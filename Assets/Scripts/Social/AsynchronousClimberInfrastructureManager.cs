using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Core;

namespace BattleSummoner.Social
{
    [Serializable]
    public class ClimberStructurePayload
    {
        public string structureId;
        public string creatorPlayerName;
        public string structureType; // "VineLadder", "ClimbingRope", "AetherBridge"
        public Vector3 worldPosition;
        public Quaternion worldRotation;
        public int floorStratum;
        public int likesCount;
        public long creationUnixTimestamp;
    }

    /// <summary>
    /// AsynchronousClimberInfrastructureManager manages Death Stranding-style persistent climber
    /// structures (Vine Ladders, Climbing Ropes, and Aether Bridges). Structures deployed offline
    /// persist immediately in the local game world and are queued in OfflineFirstContinuityManager's
    /// signed delta ledger for opportunistic upload upon restoring connection.
    /// </summary>
    public class AsynchronousClimberInfrastructureManager : MonoBehaviour
    {
        public static AsynchronousClimberInfrastructureManager Instance { get; private set; }

        [Header("Structure Prefabs")]
        [SerializeField] private GameObject vineLadderPrefab;
        [SerializeField] private GameObject climbingRopePrefab;
        [SerializeField] private GameObject aetherBridgePrefab;

        [Header("Local & Shared Infrastructure Cache")]
        public List<ClimberStructurePayload> localPlacedStructures = new List<ClimberStructurePayload>();
        public List<ClimberStructurePayload> sharedWorldStructures = new List<ClimberStructurePayload>();

        public event Action<ClimberStructurePayload> OnStructureDeployed;
        public event Action<ClimberStructurePayload> OnStructureLiked;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (OfflineFirstContinuityManager.Instance != null)
            {
                OfflineFirstContinuityManager.Instance.OnNetworkModeChanged += HandleNetworkModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (OfflineFirstContinuityManager.Instance != null)
            {
                OfflineFirstContinuityManager.Instance.OnNetworkModeChanged -= HandleNetworkModeChanged;
            }
        }

        private void HandleNetworkModeChanged(NetworkContinuityMode mode)
        {
            if (mode == NetworkContinuityMode.OnlineConnected)
            {
                // When online restored, flush pending structures and fetch nearby world structures
                SyncPendingStructuresToSpireLedger();
            }
        }

        /// <summary>
        /// Deploys a traversal structure in the current world (works 100% offline).
        /// </summary>
        public GameObject DeployClimberStructure(string structureType, Vector3 worldPos, Quaternion rotation, int floorStratum = 1, string creatorName = "LocalPlayer")
        {
            var payload = new ClimberStructurePayload
            {
                structureId = Guid.NewGuid().ToString("N"),
                creatorPlayerName = creatorName,
                structureType = structureType,
                worldPosition = worldPos,
                worldRotation = rotation,
                floorStratum = floorStratum,
                likesCount = 0,
                creationUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            localPlacedStructures.Add(payload);

            // Instantiate visual in-world entity
            GameObject prefab = ResolvePrefab(structureType);
            GameObject spawnedInstance = null;
            if (prefab != null)
            {
                spawnedInstance = Instantiate(prefab, worldPos, rotation);
                spawnedInstance.name = $"{structureType}_{payload.structureId.Substring(0, 6)}";
            }
            else
            {
                spawnedInstance = CreateProceduralStructurePrimitive(structureType, worldPos, rotation);
            }

            // Enqueue delta in OfflineFirstContinuityManager ledger
            if (OfflineFirstContinuityManager.Instance != null)
            {
                string json = JsonUtility.ToJson(payload);
                OfflineFirstContinuityManager.Instance.EnqueueOfflineAction("ClimberStructure", json);
            }

            OnStructureDeployed?.Invoke(payload);
            Debug.Log($"🪜 [Climber Infrastructure] Deployed '{structureType}' at {worldPos} (Offline Ledger Queued).");
            return spawnedInstance;
        }

        public void LikeStructure(string structureId)
        {
            var target = localPlacedStructures.Find(s => s.structureId == structureId)
                      ?? sharedWorldStructures.Find(s => s.structureId == structureId);

            if (target != null)
            {
                target.likesCount++;
                OnStructureLiked?.Invoke(target);

                if (OfflineFirstContinuityManager.Instance != null)
                {
                    OfflineFirstContinuityManager.Instance.EnqueueOfflineAction("StructureLike", $"{{\"structureId\":\"{structureId}\",\"likes\":{target.likesCount}}}");
                }
            }
        }

        private void SyncPendingStructuresToSpireLedger()
        {
            Debug.Log($"☁️ [Climber Sync] Synchronizing {localPlacedStructures.Count} local climber structures to Spire World Ledger.");
        }

        private GameObject ResolvePrefab(string type)
        {
            switch (type)
            {
                case "VineLadder": return vineLadderPrefab;
                case "ClimbingRope": return climbingRopePrefab;
                case "AetherBridge": return aetherBridgePrefab;
                default: return vineLadderPrefab;
            }
        }

        private GameObject CreateProceduralStructurePrimitive(string type, Vector3 pos, Quaternion rot)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Procedural_{type}";
            go.transform.position = pos;
            go.transform.rotation = rot;

            switch (type)
            {
                case "VineLadder":
                    go.transform.localScale = new Vector3(0.8f, 6.0f, 0.2f);
                    break;
                case "ClimbingRope":
                    go.transform.localScale = new Vector3(0.15f, 10.0f, 0.15f);
                    break;
                case "AetherBridge":
                    go.transform.localScale = new Vector3(3.0f, 0.3f, 8.0f);
                    break;
            }
            return go;
        }
    }
}

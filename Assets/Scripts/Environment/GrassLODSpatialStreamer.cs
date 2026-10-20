using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Environment
{
    /// <summary>
    /// GrassLODSpatialStreamer dynamically streams and unloads grass chunks around the player.
    /// Requirements:
    /// - Only chunks within unloadThreshold are loaded/active.
    /// - Far-away chunks are completely unloaded (SetActive(false)) with 0 draw calls.
    /// - The grass shader (Nyxis/URP_Grass) handles 50-foot threshold transparency fading.
    /// </summary>
    [ExecuteAlways]
    public class GrassLODSpatialStreamer : MonoBehaviour
    {
        [Header("--- STREAMING SETTINGS ---")]
        [Tooltip("Distance in meters to keep chunks active (400 ft = 121.92m fade start, ~150m unload limit)")]
        [SerializeField] private float unloadDistance = 150.0f;

        public float UnloadDistance
        {
            get => unloadDistance;
            set => unloadDistance = value;
        }

        public void SetUnloadDistance(float dist)
        {
            unloadDistance = dist;
            UpdateChunksImmediate();
        }

        [Tooltip("Update frequency in seconds (avoids running distance checks every single frame)")]
        [SerializeField] private float checkInterval = 0.08f;

        [Tooltip("Reference to player/target. If null, automatically finds Player_Hero or Camera.main")]
        [SerializeField] private Transform targetTransform;

        [System.Serializable]
        public class GrassChunk
        {
            public GameObject gameObject;
            public Vector3 center;
            public float radius;
            [System.NonSerialized] public bool isActive;
        }

        [SerializeField] private List<GrassChunk> registeredChunks = new List<GrassChunk>();

        private float nextCheckTime;
        private Vector3 lastCheckedPos = new Vector3(-9999f, -9999f, -9999f);

        public int TotalChunkCount => registeredChunks.Count;

        private void OnEnable()
        {
            ResolveTarget();
            UpdateChunksImmediate();
        }

        private void Start()
        {
            ResolveTarget();
            UpdateChunksImmediate();
        }

        private void Update()
        {
            if (targetTransform == null)
            {
                ResolveTarget();
                if (targetTransform == null) return;
            }

            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + checkInterval;
                Vector3 currentPos = targetTransform.position;

                // Only evaluate if moved more than 0.5 meters
                if ((currentPos - lastCheckedPos).sqrMagnitude > 0.25f)
                {
                    lastCheckedPos = currentPos;
                    UpdateChunks(currentPos);
                }
            }
        }

        private void ResolveTarget()
        {
            if (targetTransform != null) return;

            var hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                targetTransform = hero.transform;
                return;
            }

            if (Camera.main != null)
            {
                targetTransform = Camera.main.transform;
            }
        }

        public void RegisterChunks(List<GrassChunk> chunks)
        {
            registeredChunks = chunks;
            UpdateChunksImmediate();
        }

        public void ClearRegisteredChunks()
        {
            registeredChunks.Clear();
        }

        public void UpdateChunksImmediate()
        {
            ResolveTarget();
            Vector3 center = targetTransform != null ? targetTransform.position : Vector3.zero;
            lastCheckedPos = center;
            UpdateChunks(center);
        }

        private void UpdateChunks(Vector3 targetPos)
        {
            float maxDistSq = unloadDistance * unloadDistance;
            int count = registeredChunks.Count;

            for (int i = 0; i < count; i++)
            {
                var chunk = registeredChunks[i];
                if (chunk.gameObject == null) continue;

                // 2D distance on XZ plane
                float dx = chunk.center.x - targetPos.x;
                float dz = chunk.center.z - targetPos.z;
                float distSq = dx * dx + dz * dz;

                // Expand check slightly by chunk radius so chunk edges don't pop
                float effectiveLimit = unloadDistance + chunk.radius;
                bool shouldBeActive = distSq <= (effectiveLimit * effectiveLimit);

                if (chunk.isActive != shouldBeActive)
                {
                    chunk.isActive = shouldBeActive;
                    chunk.gameObject.SetActive(shouldBeActive);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = targetTransform != null ? targetTransform.position : transform.position;
            Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(center, 121.92f); // 400 feet threshold (121.92m)

            Gizmos.color = new Color(0.9f, 0.3f, 0.1f, 0.3f);
            Gizmos.DrawWireSphere(center, unloadDistance); // Unload limit (150m)
        }
    }
}

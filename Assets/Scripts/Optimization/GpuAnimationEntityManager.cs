using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleSummoner.Optimization
{
    /// <summary>
    /// High-Performance Mobile GPU Animation & Entity Instancing System
    /// (GPU Animation Entities architecture).
    /// Bypasses CPU SkinnedMeshRenderer bottlenecks on mobile ARM64 devices by batching
    /// animated mob crowds, summons, and townspeople via GPU instancing and vertex shader transforms.
    /// </summary>
    public class GpuAnimationEntityManager : MonoBehaviour
    {
        public static GpuAnimationEntityManager Instance { get; private set; }

        [Header("Instancing & Mobile Budgets")]
        [SerializeField] private bool enableGpuInstancing = true;
        [SerializeField] private int maxEntitiesPerBatch = 256;
        [SerializeField] private float cullingDistance = 55.0f;
        [SerializeField] private float lowTierLODDistance = 28.0f;

        [Header("GPU Animation Shader")]
        [SerializeField] private Shader gpuAnimationShader;

        // Registered entities categorized by mesh & material signature
        private readonly Dictionary<int, BatchGroup> batchGroups = new Dictionary<int, BatchGroup>();
        private Camera mainCamera;
        private Plane[] cameraFrustumPlanes = new Plane[6];

        private class BatchGroup
        {
            public Mesh mesh;
            public Material material;
            public MaterialPropertyBlock propertyBlock;
            public Matrix4x4[] matrices;
            public Vector4[] animParams; // x: animTime, y: animSpeed, z: animCycle, w: lodTier
            public int activeCount;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            mainCamera = Camera.main;
            if (gpuAnimationShader == null)
            {
                gpuAnimationShader = Shader.Find("Universal Render Pipeline/Lit");
            }
        }

        private void LateUpdate()
        {
            if (!enableGpuInstancing) return;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                GeometryUtility.CalculateFrustumPlanes(mainCamera, cameraFrustumPlanes);
            }

            RenderAllBatches();
        }

        /// <summary>
        /// Registers or updates an entity's transform and animation state for GPU instanced rendering.
        /// </summary>
        public void SubmitEntity(Mesh mesh, Material material, Matrix4x4 transformMatrix, float animTime, float animSpeed, float animCycle)
        {
            if (mesh == null || material == null) return;

            // Distance culling check against camera
            if (mainCamera != null)
            {
                Vector3 pos = transformMatrix.GetColumn(3);
                float distSqr = (pos - mainCamera.transform.position).sqrMagnitude;
                if (distSqr > (cullingDistance * cullingDistance)) return; // Frustum / Proximity Culled

                // Frustum cull
                var bounds = new Bounds(pos, Vector3.one * 2.5f);
                if (!GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, bounds)) return;
            }

            int key = HashCode.Combine(mesh.GetInstanceID(), material.GetInstanceID());
            if (!batchGroups.TryGetValue(key, out var group))
            {
                group = new BatchGroup
                {
                    mesh = mesh,
                    material = material,
                    propertyBlock = new MaterialPropertyBlock(),
                    matrices = new Matrix4x4[maxEntitiesPerBatch],
                    animParams = new Vector4[maxEntitiesPerBatch],
                    activeCount = 0
                };
                batchGroups[key] = group;
            }

            if (group.activeCount < maxEntitiesPerBatch)
            {
                group.matrices[group.activeCount] = transformMatrix;
                group.animParams[group.activeCount] = new Vector4(animTime, animSpeed, animCycle, 1.0f);
                group.activeCount++;
            }
        }

        private void RenderAllBatches()
        {
            foreach (var kvp in batchGroups)
            {
                var group = kvp.Value;
                if (group.activeCount == 0) continue;

                group.propertyBlock.SetVectorArray("_GpuAnimParams", group.animParams);

                Graphics.DrawMeshInstanced(
                    group.mesh,
                    0,
                    group.material,
                    group.matrices,
                    group.activeCount,
                    group.propertyBlock,
                    ShadowCastingMode.On,
                    receiveShadows: true,
                    layer: 0,
                    camera: null,
                    LightProbeUsage.BlendProbes
                );

                // Reset batch for next frame
                group.activeCount = 0;
            }
        }
    }
}

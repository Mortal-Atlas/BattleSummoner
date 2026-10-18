using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Optimization
{
    public class SpireSpatialCullingManager : MonoBehaviour
    {
        public static SpireSpatialCullingManager Instance { get; private set; }

        [Header("Spatial Culling Thresholds (1 Foot = 0.3048 Meters)")]
        [Tooltip("30 feet (9.144m) around the player/camera is ALWAYS loaded regardless of camera angle.")]
        public float alwaysLoadedRadiusMeters = 9.144f; // 30 Feet
        [Tooltip("Distance at which visible props transition to medium-res mip/LOD state.")]
        public float midResolutionDistanceMeters = 28.0f;
        [Tooltip("Distance at which visible props transition to low-res mip/LOD state.")]
        public float lowResolutionDistanceMeters = 60.0f;
        [Tooltip("Hard cutoff distance for small props.")]
        public float maxCullDistanceMeters = 150.0f;

        [Header("References")]
        public Camera targetCamera;
        public Transform playerTransform;

        private CullingGroup _cullingGroup;
        private BoundingSphere[] _boundingSpheres;
        private readonly List<CullableWorldProp> _registeredProps = new List<CullableWorldProp>();
        private int _capacity = 2048;
        private int _activeCount = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (targetCamera == null) targetCamera = Camera.main;
            InitializeCullingGroup();
            ConfigureHardwareTextureStreaming();
        }

        private void InitializeCullingGroup()
        {
            _cullingGroup = new CullingGroup();
            _cullingGroup.targetCamera = targetCamera;

            Transform refTransform = playerTransform != null ? playerTransform : (targetCamera != null ? targetCamera.transform : transform);
            _cullingGroup.SetDistanceReferencePoint(refTransform);

            // Band 0: 0m to 9.144m (Within 30ft - Always Loaded, Full Res)
            // Band 1: 9.144m to 28m (Frustum Dependent - Full Res)
            // Band 2: 28m to 60m (Frustum Dependent - Mid Res Texture/LOD)
            // Band 3: 60m to 150m (Frustum Dependent - Low Res Texture/LOD)
            // Band 4: >150m (Culled)
            _cullingGroup.SetBoundingDistances(new float[]
            {
                alwaysLoadedRadiusMeters,
                midResolutionDistanceMeters,
                lowResolutionDistanceMeters,
                maxCullDistanceMeters
            });

            _boundingSpheres = new BoundingSphere[_capacity];
            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(0);
            _cullingGroup.onStateChanged = OnCullingStateChanged;
        }

        private void ConfigureHardwareTextureStreaming()
        {
            // Enables GPU Mipmap Streaming: far away props automatically use lower-res mipmaps in VRAM
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = 512f; // 512 MB VRAM budget for mobile
            QualitySettings.streamingMipmapsRenderersPerFrame = 512;
            QualitySettings.streamingMipmapsMaxLevelReduction = 2;
        }

        public int RegisterProp(CullableWorldProp prop)
        {
            if (_activeCount >= _capacity)
            {
                ExpandCapacity(_capacity * 2);
            }

            int index = _activeCount;
            _registeredProps.Add(prop);
            _boundingSpheres[index] = new BoundingSphere(prop.transform.position, prop.boundingRadius);
            _activeCount++;
            _cullingGroup.SetBoundingSphereCount(_activeCount);

            return index;
        }

        public void UnregisterProp(CullableWorldProp prop)
        {
            int index = _registeredProps.IndexOf(prop);
            if (index < 0) return;

            int lastIndex = _activeCount - 1;
            if (index != lastIndex)
            {
                // Swap-back removal for O(1) performance without shifting arrays
                CullableWorldProp lastProp = _registeredProps[lastIndex];
                _registeredProps[index] = lastProp;
                _boundingSpheres[index] = _boundingSpheres[lastIndex];
                lastProp.AssignedCullingIndex = index;
            }

            _registeredProps.RemoveAt(lastIndex);
            _activeCount--;
            _cullingGroup.SetBoundingSphereCount(_activeCount);
        }

        private void ExpandCapacity(int newCapacity)
        {
            _capacity = newCapacity;
            BoundingSphere[] newArray = new BoundingSphere[_capacity];
            Array.Copy(_boundingSpheres, newArray, _activeCount);
            _boundingSpheres = newArray;
            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(_activeCount);
        }

        private void Update()
        {
            // Update dynamic positions only for movable registered entities
            for (int i = 0; i < _activeCount; i++)
            {
                if (_registeredProps[i] != null && _registeredProps[i].isDynamicMover)
                {
                    _boundingSpheres[i].position = _registeredProps[i].transform.position;
                }
            }
        }

        private void OnCullingStateChanged(CullingGroupEvent evt)
        {
            if (evt.index < 0 || evt.index >= _activeCount) return;
            CullableWorldProp prop = _registeredProps[evt.index];
            if (prop == null) return;

            int distanceBand = evt.currentDistance;
            bool isInCameraView = evt.isVisible;
            bool isWithin30Feet = (distanceBand == 0); // Band 0 is <= 9.144 meters (30 feet)

            // RULE: Load if inside the 30-foot perimeter OR visible inside the camera frustum (within max range)
            bool shouldBeLoaded = isWithin30Feet || (isInCameraView && distanceBand <= 3);

            prop.ApplySpatialState(shouldBeLoaded, distanceBand);
        }

        private void OnDestroy()
        {
            if (_cullingGroup != null)
            {
                _cullingGroup.Dispose();
                _cullingGroup = null;
            }
        }
    }
}

using UnityEngine;

namespace BattleSummoner.Optimization
{
    public class CullableWorldProp : MonoBehaviour
    {
        [Header("Spatial Configuration")]
        public float boundingRadius = 2.5f;
        [Tooltip("Enable only for moving NPCs/Monsters; leave false for static buildings and props.")]
        public bool isDynamicMover = false;

        [HideInInspector] public int AssignedCullingIndex = -1;

        private Renderer[] _renderers;
        private Animator[] _animators;
        private ParticleSystem[] _particles;
        private Light[] _lights;
        private LODGroup _lodGroup;
        private bool _isCurrentlyLoaded = true;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _animators = GetComponentsInChildren<Animator>(true);
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _lights = GetComponentsInChildren<Light>(true);
            _lodGroup = GetComponent<LODGroup>();
        }

        private void Start()
        {
            if (SpireSpatialCullingManager.Instance != null)
            {
                AssignedCullingIndex = SpireSpatialCullingManager.Instance.RegisterProp(this);
            }
        }

        private void OnDestroy()
        {
            if (SpireSpatialCullingManager.Instance != null)
            {
                SpireSpatialCullingManager.Instance.UnregisterProp(this);
            }
        }

        /// <summary>
        /// Executed by SpireSpatialCullingManager when frustum visibility or distance band changes.
        /// </summary>
        public void ApplySpatialState(bool shouldLoad, int distanceBand)
        {
            // 1. Toggle active rendering & simulation state
            if (_isCurrentlyLoaded != shouldLoad)
            {
                _isCurrentlyLoaded = shouldLoad;

                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] != null) _renderers[i].enabled = shouldLoad;
                }

                for (int i = 0; i < _animators.Length; i++)
                {
                    if (_animators[i] != null) _animators[i].enabled = shouldLoad;
                }

                for (int i = 0; i < _particles.Length; i++)
                {
                    if (_particles[i] == null) continue;
                    if (shouldLoad) _particles[i].Play(true);
                    else _particles[i].Pause(true);
                }

                for (int i = 0; i < _lights.Length; i++)
                {
                    if (_lights[i] != null) _lights[i].enabled = shouldLoad;
                }
            }

            if (!shouldLoad) return;

            // 2. Distance-Based Texture Resolution & Shadow Trimming (Zero Visual Loss)
            // Band 0 (<=30ft) & Band 1 (<=28m): Full Resolution + Full Shadows
            // Band 2 (28m-60m): Mid-Res Mip Bias + Shadows On
            // Band 3 (>60m): Low-Res Mip Bias + Disable expensive dynamic shadow casting on small props
            bool castShadows = distanceBand <= 2;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].shadowCastingMode = castShadows
                        ? UnityEngine.Rendering.ShadowCastingMode.On
                        : UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            if (_lodGroup != null)
            {
                if (distanceBand >= 3 && _lodGroup.lodCount >= 3)
                    _lodGroup.ForceLOD(2);
                else
                    _lodGroup.ForceLOD(-1); // Standard screen-size evaluation
            }
        }
    }
}

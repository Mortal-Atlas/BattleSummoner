using UnityEngine;

namespace BattleSummoner.Environment
{
    /// <summary>
    /// WindGrassInteractionDriver feeds the player/mount's world position to Nyxis URP_Grass shader.
    /// Drives real-time physical grass blade bending and parting as the character runs or rides through grass.
    /// Runs seamlessly in both Editor scene preview and runtime gameplay.
    /// </summary>
    [ExecuteAlways]
    public class WindGrassInteractionDriver : MonoBehaviour
    {
        [Header("--- GRASS INTERACTION SETTINGS ---")]
        [Tooltip("Radius around the character where grass blades bend away")]
        [SerializeField] private float interactionRadius = 2.4f;

        [Tooltip("Smoothing time for push movement (lower = snappy, higher = smooth lag)")]
        [SerializeField] private float smoothTime = 0.06f;

        [Tooltip("Optional target transform. If null, tracks this GameObject.")]
        [SerializeField] private Transform customTarget;

        private Vector3 smoothedPosition;
        private Vector3 velocity;

        private static readonly int GlobalPlayerPosId = Shader.PropertyToID("_GlobalPlayerPos");
        private static readonly int GlobalPlayerRadiusId = Shader.PropertyToID("_GlobalPlayerRadius");

        private void OnEnable()
        {
            smoothedPosition = TargetPosition;
            UpdateShaderGlobals();
        }

        private void Update()
        {
            Vector3 targetPos = TargetPosition;
            if (Application.isPlaying)
            {
                smoothedPosition = Vector3.SmoothDamp(smoothedPosition, targetPos, ref velocity, smoothTime);
            }
            else
            {
                smoothedPosition = targetPos;
            }

            UpdateShaderGlobals();
        }

        private void OnDisable()
        {
            // Reset radius or keep last known position
        }

        public Vector3 TargetPosition
        {
            get
            {
                if (customTarget != null) return customTarget.position;
                return transform.position;
            }
        }

        public void SetTarget(Transform target)
        {
            customTarget = target;
        }

        public void SetInteractionRadius(float radius)
        {
            interactionRadius = radius;
            Shader.SetGlobalFloat(GlobalPlayerRadiusId, interactionRadius);
        }

        private void UpdateShaderGlobals()
        {
            Shader.SetGlobalVector(GlobalPlayerPosId, smoothedPosition);
            Shader.SetGlobalFloat(GlobalPlayerRadiusId, interactionRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(TargetPosition, interactionRadius);
        }
    }
}

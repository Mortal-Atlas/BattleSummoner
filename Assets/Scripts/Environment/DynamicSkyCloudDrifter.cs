using UnityEngine;

namespace BattleSummoner.Environment
{
    /// <summary>
    /// Imparts gentle, organic atmospheric drift, slow 3D yaw rotation,
    /// and subtle vertical thermal bobbing to sky clouds.
    /// Seamlessly wraps across the world boundary so the sky remains dynamically populated.
    /// </summary>
    [DisallowMultipleComponent]
    public class DynamicSkyCloudDrifter : MonoBehaviour
    {
        [Header("Wind Drift")]
        [Tooltip("Horizontal wind heading in the X-Z plane (normalized).")]
        public Vector2 windDirection = new Vector2(0.707f, 0.707f);

        [Tooltip("Drift speed across the skybox in meters per second.")]
        public float driftSpeed = 2.4f;

        [Header("Thermal Bobbing")]
        [Tooltip("Amplitude of vertical thermal oscillation in meters.")]
        public float verticalAmplitude = 3.5f;

        [Tooltip("Frequency of vertical thermal oscillation in radians/sec.")]
        public float verticalFrequency = 0.22f;

        [Header("Rotational Shear")]
        [Tooltip("Very slow yaw rotation rate in degrees per second.")]
        public float slowYawSpeed = 0.08f;

        [Header("Skybox Boundary & Loop")]
        [Tooltip("Maximum horizontal radius from origin before wrapping across the horizon.")]
        public float skyBoundaryRadius = 3600f;

        private float _baseHeight;
        private float _phaseOffset;
        private float _lateralJitter;

        private void Start()
        {
            _baseHeight = transform.position.y;
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            _lateralJitter = Random.Range(-400f, 400f);

            // Ensure normalized wind vector
            if (windDirection.sqrMagnitude < 0.001f)
            {
                windDirection = new Vector2(0.8f, 0.6f).normalized;
            }
            else
            {
                windDirection.Normalize();
            }
        }

        private void Update()
        {
            Vector3 pos = transform.position;
            float dt = Time.deltaTime;

            // 1. Horizontal wind drift
            pos.x += windDirection.x * driftSpeed * dt;
            pos.z += windDirection.y * driftSpeed * dt;

            // 2. Vertical thermal breathing
            pos.y = _baseHeight + Mathf.Sin((Time.time * verticalFrequency) + _phaseOffset) * verticalAmplitude;

            // 3. Gentle yaw rotation
            if (Mathf.Abs(slowYawSpeed) > 0.0001f)
            {
                transform.Rotate(Vector3.up, slowYawSpeed * dt, Space.World);
            }

            // 4. Horizon boundary wrapping (seamless loop from downwind edge back to upwind horizon)
            Vector2 pos2D = new Vector2(pos.x, pos.z);
            float distFromCenter = pos2D.magnitude;

            if (distFromCenter > skyBoundaryRadius)
            {
                // Check if moving outward downwind
                float dot = Vector2.Dot(pos2D.normalized, windDirection);
                if (dot > 0.25f)
                {
                    // Respawn along upwind horizon arc
                    Vector2 perp = new Vector2(-windDirection.y, windDirection.x);
                    float crossSpread = Random.Range(-skyBoundaryRadius * 0.75f, skyBoundaryRadius * 0.75f);
                    Vector2 upwindOrigin = (-windDirection * (skyBoundaryRadius * 0.95f)) + (perp * crossSpread);

                    pos.x = upwindOrigin.x;
                    pos.z = upwindOrigin.y;
                }
            }

            transform.position = pos;
        }

        /// <summary>
        /// Configures custom wind and drift parameters based on cloud scale and elevation archetype.
        /// </summary>
        public void Configure(Vector2 windDir, float speed, float vertAmp, float vertFreq, float yawSpeed, float boundary)
        {
            windDirection = windDir.sqrMagnitude > 0.01f ? windDir.normalized : new Vector2(0.8f, 0.6f).normalized;
            driftSpeed = speed;
            verticalAmplitude = vertAmp;
            verticalFrequency = vertFreq;
            slowYawSpeed = yawSpeed;
            skyBoundaryRadius = boundary;
            _baseHeight = transform.position.y;
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }
}

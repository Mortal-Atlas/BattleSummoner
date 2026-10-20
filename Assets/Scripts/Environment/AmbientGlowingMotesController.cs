using UnityEngine;

namespace BattleSummoner.Environment
{
    /// <summary>
    /// AmbientGlowingMotesController manages a graceful, atmospheric field of
    /// floating yellow-white glowing semi-transparent particles around the player.
    /// 
    /// Specifications:
    /// - Max spawn distance: 100 feet (~30.48m) from the player.
    /// - Graceful motion: subtle floating drift, graceful upward jumps/bursts, random 3D spins.
    /// - High visual quality: semi-transparent luminous yellow-white glow (#FFFBD0 / #FFFFFE).
    /// - Zero overhead: dynamically follows Player_Hero, smooth simulated space.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    public class AmbientGlowingMotesController : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private float maxDistanceFeet = 100f; // 100 feet = 30.48m

        [Header("Particle System")]
        [SerializeField] private ParticleSystem motesParticleSystem;

        private const float FEET_TO_METERS = 0.3048f;

        private void Awake()
        {
            if (motesParticleSystem == null)
            {
                motesParticleSystem = GetComponent<ParticleSystem>();
            }
            FindPlayerTarget();
        }

        private void OnEnable()
        {
            FindPlayerTarget();
        }

        private void Update()
        {
            if (targetPlayer == null)
            {
                FindPlayerTarget();
            }

            if (targetPlayer != null)
            {
                // Smoothly follow the player's position without snapping violently
                Vector3 targetPos = targetPlayer.position + Vector3.up * 1.5f;
                transform.position = targetPos;
            }
        }

        private void FindPlayerTarget()
        {
            if (targetPlayer != null) return;

            var player = GameObject.Find("Player_Hero");
            if (player != null)
            {
                targetPlayer = player.transform;
            }
            else if (Camera.main != null)
            {
                targetPlayer = Camera.main.transform;
            }
        }

        public void ConfigureMotesParameters()
        {
            if (motesParticleSystem == null)
                motesParticleSystem = GetComponent<ParticleSystem>();

            float radiusMeters = maxDistanceFeet * FEET_TO_METERS; // ~30.48m

            var main = motesParticleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // World space for graceful floating while player moves
            main.maxParticles = 450;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.gravityModifier = -0.015f; // Gentle negative gravity for buoyant floating drift

            // Yellow-white glowing semi-transparent color gradient
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1.0f, 0.98f, 0.82f), 0.0f), // Soft warm yellow-white
                    new GradientColorKey(new Color(1.0f, 1.0f, 0.95f), 0.5f),  // Luminous white
                    new GradientColorKey(new Color(1.0f, 0.92f, 0.65f), 1.0f)   // Golden amber glow
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.60f, 0.25f), // Semi-transparent
                    new GradientAlphaKey(0.65f, 0.75f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(grad);

            // Shape module: Sphere with radius = 100 feet (30.48m)
            var shape = motesParticleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radiusMeters;
            shape.radiusThickness = 0.85f; // Distributed throughout sphere volume

            // Velocity over Lifetime: random 3D wafting & graceful jumping
            var velocityOverLifetime = motesParticleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            // Upward wafting with occasional graceful jump
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.8f);
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

            // Rotation over Lifetime: Graceful random spinning
            var rotOverLifetime = motesParticleSystem.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f); // ~70 deg/sec spin

            // Size over Lifetime: Smooth swell and shrink
            var sizeOverLifetime = motesParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0.0f, 0.0f);
            sizeCurve.AddKey(0.2f, 1.0f);
            sizeCurve.AddKey(0.8f, 0.9f);
            sizeCurve.AddKey(1.0f, 0.0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

            // Noise module: Organic turbulence for firefly-like dancing and upward jumps
            var noise = motesParticleSystem.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.5f;
            noise.damping = true;
            noise.octaveCount = 2;

            // Emission module: continuous gentle flow
            var emission = motesParticleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 45f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (motesParticleSystem == null)
                motesParticleSystem = GetComponent<ParticleSystem>();
        }
#endif
    }
}

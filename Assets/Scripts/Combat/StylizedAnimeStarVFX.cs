using System.Collections;
using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// Anime Star VFX URP (Anime Star VFX URP architecture).
    /// Generates vibrant 4-point anime star bursts, sparkle bursts, and critical hit impact star trails.
    /// Integrated with CombatFeedbackManager and spell impacts.
    /// </summary>
    public class StylizedAnimeStarVFX : MonoBehaviour
    {
        public static StylizedAnimeStarVFX Instance { get; private set; }

        [Header("Star VFX Colors")]
        [SerializeField] private Color criticalStarColor = new Color(1.0f, 0.85f, 0.25f, 1.0f);
        [SerializeField] private Color arcaneStarColor = new Color(0.45f, 0.75f, 1.0f, 1.0f);
        [SerializeField] private Color healingStarColor = new Color(0.35f, 1.0f, 0.55f, 1.0f);

        [Header("Particle System")]
        [SerializeField] private ParticleSystem starBurstSystem;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (starBurstSystem == null) starBurstSystem = GetComponentInChildren<ParticleSystem>();
        }

        /// <summary>
        /// Spawns an Anime Star burst at world position.
        /// </summary>
        public static void SpawnStarBurst(Vector3 position, Color color, float scale = 1.0f)
        {
            if (Instance == null)
            {
                var go = new GameObject("--- ANIME STAR VFX POOL ---");
                Instance = go.AddComponent<StylizedAnimeStarVFX>();
            }

            Instance.TriggerStarBurst(position, color, scale);
        }

        public void TriggerStarBurst(Vector3 position, Color color, float scale)
        {
            if (starBurstSystem != null)
            {
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = position,
                    startColor = color,
                    startSize = 1.5f * scale,
                    startLifetime = 0.65f
                };
                starBurstSystem.Emit(emitParams, 12);
            }
        }
    }
}

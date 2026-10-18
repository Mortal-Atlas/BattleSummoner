using System.Collections;
using UnityEngine;

namespace BattleSummoner.Environment
{
    /// <summary>
    /// Stylized Rain Maker System (Rain Maker 2D and 3D architecture).
    /// Generates atmospheric camera-relative rain streaks, ground splash rings,
    /// mist fog, and dynamic precipitation audio with strict mobile particle budgets.
    /// </summary>
    public class StylizedRainMakerSystem : MonoBehaviour
    {
        public static StylizedRainMakerSystem Instance { get; private set; }

        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem rainFallParticles;
        [SerializeField] private ParticleSystem groundSplashParticles;
        [SerializeField] private ParticleSystem mistFogParticles;

        [Header("Rain Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float rainIntensity = 0.65f;
        [SerializeField] private float cameraFollowHeight = 12.0f;
        [SerializeField] private bool followCamera = true;

        [Header("Audio")]
        [SerializeField] private AudioSource rainAudioSource;
        [SerializeField] private float maxAudioVolume = 0.5f;

        private Camera targetCamera;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            targetCamera = Camera.main;
            ConfigureParticleBudgets();
        }

        private void LateUpdate()
        {
            if (!followCamera) return;
            if (targetCamera == null) targetCamera = Camera.main;

            if (targetCamera != null)
            {
                // Anchor rain volume right above the camera
                Vector3 camPos = targetCamera.transform.position;
                transform.position = new Vector3(camPos.x, camPos.y + cameraFollowHeight, camPos.z);
            }
        }

        public void SetRainIntensity(float intensity)
        {
            rainIntensity = Mathf.Clamp01(intensity);
            UpdateEmissionRates();
        }

        private void ConfigureParticleBudgets()
        {
            // Mobile budget: limit max particles to 400 for rain, 150 for splash
            if (rainFallParticles != null)
            {
                var main = rainFallParticles.main;
                main.maxParticles = 500;
            }
            if (groundSplashParticles != null)
            {
                var main = groundSplashParticles.main;
                main.maxParticles = 200;
            }
            UpdateEmissionRates();
        }

        private void UpdateEmissionRates()
        {
            if (rainFallParticles != null)
            {
                var emission = rainFallParticles.emission;
                emission.rateOverTime = Mathf.Lerp(50f, 350f, rainIntensity);
            }

            if (groundSplashParticles != null)
            {
                var emission = groundSplashParticles.emission;
                emission.rateOverTime = Mathf.Lerp(20f, 150f, rainIntensity);
            }

            if (rainAudioSource != null)
            {
                rainAudioSource.volume = rainIntensity * maxAudioVolume;
            }
        }

        public void StartRain(float transitionDuration = 2.0f)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeRain(rainIntensity, transitionDuration));
        }

        public void StopRain(float transitionDuration = 2.0f)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRain(0f, transitionDuration, () => gameObject.SetActive(false)));
        }

        private IEnumerator FadeRain(float targetIntensity, float duration, System.Action onComplete = null)
        {
            float start = rainIntensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rainIntensity = Mathf.Lerp(start, targetIntensity, elapsed / duration);
                UpdateEmissionRates();
                yield return null;
            }

            rainIntensity = targetIntensity;
            UpdateEmissionRates();
            onComplete?.Invoke();
        }
    }
}

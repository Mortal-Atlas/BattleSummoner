using UnityEngine;

namespace BattleSummoner.Environment
{
    /// <summary>
    /// Smooth Perlin-noise light flicker script for campfire and torchlight
    /// giving dynamic, living hearth ambiance without jarring jumps.
    /// </summary>
    public class CampfireFlickerLight : MonoBehaviour
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private float minIntensity = 2.4f;
        [SerializeField] private float maxIntensity = 3.6f;
        [SerializeField] private float flickerSpeed = 4.5f;

        private float baseRange = 10f;
        private float seed;

        private void Awake()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }
            if (targetLight != null)
            {
                baseRange = targetLight.range;
            }
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (targetLight == null) return;

            float noise = Mathf.PerlinNoise(seed + Time.time * flickerSpeed, seed);
            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
            targetLight.range = baseRange + (noise - 0.5f) * 0.6f;
        }
    }
}

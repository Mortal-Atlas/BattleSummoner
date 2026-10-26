using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    public class LightFlickerEffect : MonoBehaviour
    {
        public float baseIntensity = 2.2f;
        public float flickerAmplitude = 0.5f;
        public float flickerSpeed = 12f;

        private Light targetLight;
        private float noiseOffset;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            noiseOffset = Random.Range(0f, 1000f);
        }

        private void Update()
        {
            if (targetLight == null) return;
            float noise = Mathf.PerlinNoise(noiseOffset, Time.time * flickerSpeed);
            targetLight.intensity = baseIntensity + (noise - 0.5f) * 2f * flickerAmplitude;
        }
    }
}

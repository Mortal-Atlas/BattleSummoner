using UnityEngine;

namespace BattleSummoner.VFX
{
    /// <summary>
    /// Rainbow Lens Flare VFX URP (Rainbow Lens Flare VFX URP architecture).
    /// Positions and animates a chromatic rainbow anamorphic lens flare with prismatic halo rings,
    /// dynamic occlusion fade against scene geometry, and sun alignment.
    /// </summary>
    [ExecuteAlways]
    public class RainbowLensFlareURP : MonoBehaviour
    {
        public static RainbowLensFlareURP Instance { get; private set; }

        [Header("Light Source Linkage")]
        [SerializeField] private Transform sunLightTransform;
        [SerializeField] private float flareDistance = 1.5f;
        [SerializeField] private float flareScale = 1.8f;

        [Header("Occlusion & Fade")]
        [SerializeField] private bool enableOcclusion = true;
        [SerializeField] private LayerMask occlusionLayers = ~0;
        [SerializeField] private float fadeSpeed = 8.0f;

        private Camera mainCamera;
        private MeshRenderer meshRenderer;
        private Material flareMaterial;
        private float currentFade = 1.0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            mainCamera = Camera.main;
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                flareMaterial = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;
            }
            if (sunLightTransform == null) FindSunDirectionalLight();
        }

        private void LateUpdate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
            if (sunLightTransform == null) FindSunDirectionalLight();

            Vector3 sunDir = sunLightTransform != null ? -sunLightTransform.forward : Vector3.forward;

            // Check if sun is in front of camera
            float dot = Vector3.Dot(mainCamera.transform.forward, sunDir);
            bool isVisible = dot > 0.15f;

            if (isVisible && enableOcclusion && Application.isPlaying)
            {
                // Raycast from camera towards sun
                if (Physics.Raycast(mainCamera.transform.position, sunDir, out RaycastHit hit, 200f, occlusionLayers))
                {
                    // If hit something solid, occluded
                    isVisible = false;
                }
            }

            float targetFade = isVisible ? Mathf.Clamp01((dot - 0.15f) / 0.35f) : 0f;
            currentFade = Mathf.Lerp(currentFade, targetFade, Time.deltaTime * fadeSpeed);

            // Position flare quad directly in front of camera aligned with sun projection
            Vector3 flarePos = mainCamera.transform.position + (mainCamera.transform.forward * 0.7f + sunDir * 0.3f).normalized * flareDistance;
            transform.position = flarePos;
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            transform.localScale = Vector3.one * (flareScale * (0.8f + currentFade * 0.4f));

            if (flareMaterial != null)
            {
                flareMaterial.SetFloat("_Intensity", currentFade * 2.0f);
            }
        }

        private void FindSunDirectionalLight()
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sunLightTransform = l.transform;
                    break;
                }
            }
        }
    }
}

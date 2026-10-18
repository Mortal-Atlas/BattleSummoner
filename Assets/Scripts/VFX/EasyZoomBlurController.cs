using System.Collections;
using UnityEngine;

namespace BattleSummoner.VFX
{
    /// <summary>
    /// Easy Zoom Blur URP Controller (Easy Zoom Blur URP architecture).
    /// Manages full-screen radial zoom blur impulses with chromatic aberration for
    /// combat dashes, critical strikes, boss ground slams, and spell releases.
    /// </summary>
    public class EasyZoomBlurController : MonoBehaviour
    {
        public static EasyZoomBlurController Instance { get; private set; }

        [Header("Shader & Material")]
        [SerializeField] private Shader zoomBlurShader;
        private Material zoomBlurMaterial;

        [Header("Default Parameters")]
        [SerializeField] private float defaultStrength = 0.55f;
        [SerializeField] private float defaultDuration = 0.22f;

        private float currentStrength = 0f;
        private Coroutine activeBlurRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeMaterial();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeMaterial()
        {
            if (zoomBlurShader == null)
            {
                zoomBlurShader = Shader.Find("BattleSummoner/VFX/RadialZoomBlur");
            }
            if (zoomBlurShader != null)
            {
                zoomBlurMaterial = new Material(zoomBlurShader);
            }
        }

        public static void TriggerBlur(float strength = 0.55f, float duration = 0.22f, Vector2? screenCenter = null)
        {
            EnsureInstance();
            Instance.TriggerRadialBlur(strength, duration, screenCenter);
        }

        public void TriggerRadialBlur(float strength, float duration, Vector2? screenCenter = null)
        {
            if (activeBlurRoutine != null) StopCoroutine(activeBlurRoutine);
            activeBlurRoutine = StartCoroutine(RoutineZoomBlur(strength, duration, screenCenter ?? new Vector2(0.5f, 0.5f)));
        }

        private IEnumerator RoutineZoomBlur(float peakStrength, float duration, Vector2 center)
        {
            if (zoomBlurMaterial != null)
            {
                zoomBlurMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0, 0));
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Quick attack, smooth exponential decay
                currentStrength = Mathf.Lerp(peakStrength, 0f, Mathf.Pow(t, 0.75f));

                if (zoomBlurMaterial != null)
                {
                    zoomBlurMaterial.SetFloat("_BlurStrength", currentStrength);
                }
                yield return null;
            }

            currentStrength = 0f;
            if (zoomBlurMaterial != null)
            {
                zoomBlurMaterial.SetFloat("_BlurStrength", 0f);
            }
            activeBlurRoutine = null;
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("--- EASY ZOOM BLUR CONTROLLER ---");
                Instance = go.AddComponent<EasyZoomBlurController>();
            }
        }
    }
}

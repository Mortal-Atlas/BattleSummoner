using System.Collections;
using UnityEngine;

namespace BattleSummoner.VFX
{
    public enum AnimeImpactStyle
    {
        InvertMangaSketch = 0,
        HighContrastMonochrome = 1,
        ChromaticShock = 2
    }

    /// <summary>
    /// Anime Impact Frame Controller (Easy Impact Frames architecture).
    /// Triggers dramatic 2-5 frame black & white negative / inverted color flashes with speedlines
    /// on critical strikes, blade parries, and boss ground rupture impacts.
    /// </summary>
    public class AnimeImpactFrameController : MonoBehaviour
    {
        public static AnimeImpactFrameController Instance { get; private set; }

        [Header("Impact Material")]
        [SerializeField] private Shader impactFrameShader;
        private Material impactFrameMaterial;

        private Coroutine activeImpactRoutine;

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
            if (impactFrameShader == null)
            {
                impactFrameShader = Shader.Find("BattleSummoner/VFX/AnimeImpactFrame");
            }
            if (impactFrameShader != null)
            {
                impactFrameMaterial = new Material(impactFrameShader);
            }
        }

        /// <summary>
        /// Triggers an anime impact frame for the specified duration (typically 0.06 - 0.12s).
        /// </summary>
        public static void Trigger(AnimeImpactStyle style = AnimeImpactStyle.InvertMangaSketch, float duration = 0.08f, Vector2? center = null)
        {
            EnsureInstance();
            Instance.TriggerImpact(style, duration, center);
        }

        public void TriggerImpact(AnimeImpactStyle style, float duration, Vector2? screenCenter = null)
        {
            if (activeImpactRoutine != null) StopCoroutine(activeImpactRoutine);
            activeImpactRoutine = StartCoroutine(RoutineImpactFrame(style, duration, screenCenter ?? new Vector2(0.5f, 0.5f)));
        }

        private IEnumerator RoutineImpactFrame(AnimeImpactStyle style, float duration, Vector2 center)
        {
            if (impactFrameMaterial != null)
            {
                impactFrameMaterial.SetFloat("_Mode", (float)style);
                impactFrameMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0, 0));
                impactFrameMaterial.SetFloat("_ActiveStrength", 1.0f);
            }

            // High impact freeze duration (2-5 frames)
            yield return new WaitForSeconds(duration);

            if (impactFrameMaterial != null)
            {
                impactFrameMaterial.SetFloat("_ActiveStrength", 0f);
            }
            activeImpactRoutine = null;
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("--- ANIME IMPACT FRAME CONTROLLER ---");
                Instance = go.AddComponent<AnimeImpactFrameController>();
            }
        }
    }
}

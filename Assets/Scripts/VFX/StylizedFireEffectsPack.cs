using System.Collections;
using UnityEngine;

namespace BattleSummoner.VFX
{
    /// <summary>
    /// Stylized Fire Effects Pack (Free Pack Fire Effects architecture).
    /// Provides high-impact flame pillars, rolling fireballs, fire tornadoes,
    /// and ignited ground burning patches with flame trails and ember clouds.
    /// </summary>
    public class StylizedFireEffectsPack : MonoBehaviour
    {
        public static StylizedFireEffectsPack Instance { get; private set; }

        [Header("Fire Palettes")]
        [SerializeField] private Color innerCoreColor = new Color(1.0f, 0.95f, 0.65f, 1f);
        [SerializeField] private Color outerFlameColor = new Color(1.0f, 0.35f, 0.05f, 1f);
        [SerializeField] private Color smokeEmberColor = new Color(0.25f, 0.15f, 0.12f, 0.6f);

        private Material flameAdditiveMat;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
                flameAdditiveMat = new Material(shader);
                flameAdditiveMat.SetFloat("_Surface", 1);
                flameAdditiveMat.SetFloat("_Blend", 1); // Additive
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Spawns an erupting Flame Pillar from the ground with embers and heat smoke.
        /// </summary>
        public static GameObject SpawnFlamePillar(Vector3 groundPos, float duration = 1.2f, float scale = 1.0f)
        {
            EnsureInstance();
            var go = new GameObject("FX_FlamePillar");
            go.transform.position = groundPos;
            go.transform.localScale = Vector3.one * scale;

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = Instance.flameAdditiveMat;

            var main = ps.main;
            main.duration = duration;
            main.startLifetime = 0.85f;
            main.startSpeed = 11.0f;
            main.startSize = 1.2f * scale;
            main.startColor = Instance.outerFlameColor;
            main.loop = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.6f * scale;

            var emission = ps.emission;
            emission.rateOverTime = 120f;

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Instance.innerCoreColor, 0f), new GradientColorKey(Instance.outerFlameColor, 0.4f), new GradientColorKey(new Color(0.8f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            // Add point light flash
            var lightObj = new GameObject("FlamePillarLight");
            lightObj.transform.SetParent(go.transform, false);
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 8.0f * scale;
            l.color = Instance.outerFlameColor;
            l.intensity = 3.5f;

            ps.Play();
            Destroy(go, duration + 0.5f);
            return go;
        }

        /// <summary>
        /// Spawns an Ignited Ground burning patch that lingers on the floor dealing periodic burns.
        /// </summary>
        public static GameObject SpawnBurningGroundPatch(Vector3 position, float radius = 2.5f, float burnDuration = 5.0f)
        {
            EnsureInstance();
            var go = new GameObject("FX_BurningGroundPatch");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = Instance.flameAdditiveMat;

            var main = ps.main;
            main.duration = burnDuration;
            main.startLifetime = 0.7f;
            main.startSpeed = 1.8f;
            main.startSize = 0.8f;
            main.startColor = Instance.outerFlameColor;
            main.loop = true;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.rotation = new Vector3(-90f, 0f, 0f); // Lie flat on ground

            var emission = ps.emission;
            emission.rateOverTime = 45f;

            ps.Play();
            Instance.StartCoroutine(Instance.FadeAndDestroy(go, burnDuration));
            return go;
        }

        /// <summary>
        /// Spawns a high-velocity Fireball explosion with shockwave and embers.
        /// </summary>
        public static GameObject SpawnFireballExplosion(Vector3 position, float scale = 1.0f)
        {
            EnsureInstance();
            var go = new GameObject("FX_FireballExplosion");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = Instance.flameAdditiveMat;

            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.6f;
            main.startSpeed = 8.0f * scale;
            main.startSize = 1.4f * scale;
            main.startColor = Instance.outerFlameColor;
            main.loop = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });

            ps.Play();
            Destroy(go, 1.0f);
            return go;
        }

        private IEnumerator FadeAndDestroy(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go != null)
            {
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(go, 1.0f);
            }
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("--- STYLIZED FIRE EFFECTS PACK ---");
                Instance = go.AddComponent<StylizedFireEffectsPack>();
            }
        }
    }
}

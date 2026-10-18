using System.Collections;
using UnityEngine;

namespace BattleSummoner.VFX
{
    /// <summary>
    /// Potion Drinking & Elixir Activation VFX (VFX Particles Bottles & Spells architecture).
    /// Generates cork pop bursts, ascending double-helix energy ribbons, and glowing restorative sparkles.
    /// </summary>
    public class PotionDrinkingSpellVFX : MonoBehaviour
    {
        public static PotionDrinkingSpellVFX Instance { get; private set; }

        private Material auraAdditiveMat;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit");
                auraAdditiveMat = new Material(shader);
                auraAdditiveMat.SetFloat("_Surface", 1);
                auraAdditiveMat.SetFloat("_Blend", 1); // Additive
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Plays potion consumption VFX on the target character.
        /// </summary>
        public static void PlayDrinkEffect(Transform character, PotionBottleType type)
        {
            EnsureInstance();
            Instance.TriggerDrinkEffect(character, type);
        }

        public void TriggerDrinkEffect(Transform character, PotionBottleType type)
        {
            if (character == null) return;

            Color auraColor = type switch
            {
                PotionBottleType.VitalityHP => new Color(1.0f, 0.2f, 0.35f, 1f),
                PotionBottleType.AstralMP => new Color(0.25f, 0.65f, 1.0f, 1f),
                PotionBottleType.SwiftnessHaste => new Color(0.35f, 1.0f, 0.55f, 1f),
                PotionBottleType.CelestialElixir => new Color(1.0f, 0.90f, 0.35f, 1f),
                _ => Color.white
            };

            // 1. Cork Pop Puff at character chest
            Vector3 chestPos = character.position + Vector3.up * 1.2f;
            ElementalSpellParticlePack.PlaySpellEffect(SpellEffectType.ArcaneBurstGlow, chestPos, null, 0.75f);

            // 2. Ascending Healing Ribbon Particles
            var auraGo = new GameObject("FX_ElixirAura");
            auraGo.transform.position = character.position;
            auraGo.transform.SetParent(character, true);

            var ps = auraGo.AddComponent<ParticleSystem>();
            var psr = auraGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = auraAdditiveMat;

            var main = ps.main;
            main.duration = 1.2f;
            main.startLifetime = 1.0f;
            main.startSpeed = 2.4f;
            main.startSize = 0.22f;
            main.startColor = auraColor;
            main.loop = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.65f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var emission = ps.emission;
            emission.rateOverTime = 60f;

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(auraColor, 0f), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(auraColor * 0.7f, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            // Audio chime
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 1.0f);
            }

            ps.Play();
            Destroy(auraGo, 2.0f);
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("--- POTION DRINKING VFX ---");
                Instance = go.AddComponent<PotionDrinkingSpellVFX>();
            }
        }
    }
}

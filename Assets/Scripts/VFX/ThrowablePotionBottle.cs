using System;
using UnityEngine;
using BattleSummoner.Combat;

namespace BattleSummoner.VFX
{
    /// <summary>
    /// Throwable Potion Flask Projectile (VFX Particles Bottles & Spells architecture).
    /// Arcs through the air with fluid sparkle trails, shatters on impact into glass shards,
    /// and leaves an expanding elemental liquid puddle with AoE effects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowablePotionBottle : MonoBehaviour
    {
        [Header("Potion Configuration")]
        [SerializeField] private PotionBottleType bottleType = PotionBottleType.CausticAcid;
        [SerializeField] private float splashRadius = 3.5f;
        [SerializeField] private float baseEffectPower = 65f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem flightTrailParticles;
        [SerializeField] private GameObject glassShatterVfxPrefab;

        private Rigidbody rb;
        private bool hasImpacted = false;
        private Vector3 tumbleTorque;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            tumbleTorque = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-5f, 5f));
        }

        private void Start()
        {
            rb.AddTorque(tumbleTorque, ForceMode.Impulse);
            if (flightTrailParticles != null) flightTrailParticles.Play();
        }

        public void Launch(Vector3 velocity, PotionBottleType type, float power = 65f)
        {
            bottleType = type;
            baseEffectPower = power;
            if (rb == null) rb = GetComponent<Rigidbody>();
            rb.linearVelocity = velocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasImpacted) return;
            hasImpacted = true;

            Vector3 impactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            Vector3 impactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

            ExecuteShatter(impactPoint, impactNormal);
        }

        private void ExecuteShatter(Vector3 point, Vector3 normal)
        {
            // 1. Spawn Glass Shards & Liquid Splash VFX
            SpawnGlassShatterVFX(point, normal);

            // 2. Play sound
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }

            // 3. Trigger AoE effect according to bottle type
            switch (bottleType)
            {
                case PotionBottleType.CausticAcid:
                    ElementalSpellParticlePack.PlaySpellEffect(SpellEffectType.AcidSplashPuddle, point);
                    ApplyAoEDamage(point, splashRadius, baseEffectPower, DamageType.Acid, StatusEffectType.AcidCorroded);
                    break;

                case PotionBottleType.MoltenFire:
                    StylizedFireEffectsPack.SpawnFireballExplosion(point, 1.2f);
                    StylizedFireEffectsPack.SpawnBurningGroundPatch(point, splashRadius, 4.5f);
                    ApplyAoEDamage(point, splashRadius, baseEffectPower, DamageType.Fire, StatusEffectType.Burn);
                    break;

                case PotionBottleType.VitalityHP:
                    ElementalSpellParticlePack.PlaySpellEffect(SpellEffectType.DivineHealingShower, point);
                    HealAlliesInRadius(point, splashRadius, baseEffectPower * 4.0f);
                    break;

                case PotionBottleType.AstralMP:
                    ElementalSpellParticlePack.PlaySpellEffect(SpellEffectType.ArcaneNova, point);
                    break;

                default:
                    ElementalSpellParticlePack.PlaySpellEffect(SpellEffectType.ArcaneBurstGlow, point);
                    break;
            }

            Destroy(gameObject);
        }

        private void SpawnGlassShatterVFX(Vector3 point, Vector3 normal)
        {
            var shatterGo = new GameObject("FX_BottleGlassShatter");
            shatterGo.transform.position = point;
            shatterGo.transform.up = normal;

            var ps = shatterGo.AddComponent<ParticleSystem>();
            var psr = shatterGo.GetComponent<ParticleSystemRenderer>();

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.9f, 0.95f, 1.0f, 0.8f));
            mat.SetFloat("_Smoothness", 0.95f);
            psr.sharedMaterial = mat;

            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = 0.65f;
            main.startSpeed = 5.5f;
            main.startSize = 0.08f;
            main.gravityModifier = 1.2f;
            main.loop = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.2f;

            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });

            ps.Play();
            Destroy(shatterGo, 1.2f);
        }

        private void ApplyAoEDamage(Vector3 center, float radius, float damage, DamageType dtype, StatusEffectType status)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            foreach (var col in hits)
            {
                if (col.CompareTag("Player") || col.name.Contains("Hero")) continue;
                if (col.TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    var payload = new DamagePayload(damage, dtype, col.transform.position, Vector3.up, gameObject)
                    {
                        appliedStatus = status,
                        statusDuration = 5.0f,
                        isAoE = true,
                        aoeRadius = radius
                    };
                    receiver.TakeAdvancedDamage(payload);
                }
            }
        }

        private void HealAlliesInRadius(Vector3 center, float radius, float healAmount)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            foreach (var col in hits)
            {
                if (col.CompareTag("Player") || col.name.Contains("Hero") || col.name.Contains("Companion"))
                {
                    var prog = col.GetComponent<PlayerProgression>() ?? col.GetComponentInParent<PlayerProgression>();
                    if (prog != null)
                    {
                        prog.Heal(healAmount);
                    }
                }
            }
        }
    }
}

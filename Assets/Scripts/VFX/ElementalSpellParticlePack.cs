using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.VFX
{
    public enum SpellEffectType
    {
        // Arcane (1-8)
        ArcaneMissileHit,
        ArcaneNova,
        ArcaneShieldAura,
        ArcaneVortexRift,
        ArcaneSpiralRune,
        ArcaneBurstGlow,
        ArcaneEnergyLance,
        ArcaneTeleportFlash,

        // Frost / Ice (9-16)
        FrostNova,
        IceSpikeEruption,
        BlizzardSwirl,
        GlacialFreezeBurst,
        FreezingMistPuff,
        IceShardImpact,
        FrostArmorGlow,
        PermafrostChill,

        // Electric / Lightning (17-24)
        ElectricShockRing,
        GalvanicSparkFountain,
        PlasmaBallCharge,
        StaticFieldAura,
        TeslaCoilDischarge,
        ThunderShockImpact,
        OverloadArcBlast,
        VoltShieldPulse,

        // Fire / Flame (25-32)
        FireballExplosion,
        FlamePillarGeyser,
        FireNovaRing,
        EmberCloudDrift,
        FlamethrowerStream,
        FireTornadoVortex,
        IgnitedGroundPatch,
        CombustionDetonation,

        // Nature / Poison (33-40)
        ToxicPoisonCloud,
        AcidSplashPuddle,
        ThornBrierBurst,
        SporeBloomDrift,
        PoisonDripHit,
        VineEntangleGlow,
        VerdantBloomAura,
        LeechSeedDrain,

        // Holy & Dark (41-48)
        DivineHealingShower,
        RadiantHolyPillar,
        CelestialBlessingAura,
        SunlightFlash,
        VoidShadowOrb,
        AbyssalDarkVortex,
        SoulSiphonTendril,
        CurseHexSkull
    }

    /// <summary>
    /// Comprehensive 48 Elemental Spell Particle Effect Pack (48 Particle Effect Pack architecture).
    /// Provides 48 distinct stylized particle effects spanning Arcane, Frost, Electric, Fire,
    /// Nature, Holy, and Dark elements, optimized for mobile URP budgets.
    /// </summary>
    public class ElementalSpellParticlePack : MonoBehaviour
    {
        public static ElementalSpellParticlePack Instance { get; private set; }

        private readonly Dictionary<SpellEffectType, GameObject> effectPrefabs = new Dictionary<SpellEffectType, GameObject>();
        private Material particleAdditiveMat;
        private Material particleAlphaMat;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeParticleMaterials();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeParticleMaterials()
        {
            Shader unlitParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                         Shader.Find("Universal Render Pipeline/Lit");

            particleAdditiveMat = new Material(unlitParticleShader);
            particleAdditiveMat.SetFloat("_Surface", 1); // Transparent
            particleAdditiveMat.SetFloat("_Blend", 1);   // Additive

            particleAlphaMat = new Material(unlitParticleShader);
            particleAlphaMat.SetFloat("_Surface", 1);
            particleAlphaMat.SetFloat("_Blend", 0);     // Alpha blend
        }

        /// <summary>
        /// Plays any of the 48 Elemental Spell Particle Effects at the given position.
        /// </summary>
        public static GameObject PlaySpellEffect(SpellEffectType effectType, Vector3 position, Quaternion? rotation = null, float scale = 1.0f)
        {
            EnsureInstance();
            Quaternion rot = rotation ?? Quaternion.identity;
            return Instance.SpawnEffect(effectType, position, rot, scale);
        }

        public GameObject SpawnEffect(SpellEffectType effectType, Vector3 position, Quaternion rotation, float scale)
        {
            var go = new GameObject($"FX_{effectType}");
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * scale;

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = particleAdditiveMat;

            ConfigureEffectByType(effectType, ps, psr);

            ps.Play();
            Destroy(go, ps.main.duration + ps.main.startLifetime.constantMax + 0.2f);
            return go;
        }

        private void ConfigureEffectByType(SpellEffectType effectType, ParticleSystem ps, ParticleSystemRenderer psr)
        {
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var colOverLife = ps.colorOverLifetime;
            var sizeOverLife = ps.sizeOverLifetime;

            main.loop = false;
            main.playOnAwake = false;
            colOverLife.enabled = true;
            sizeOverLife.enabled = true;

            Color baseCol = GetColorForEffect(effectType);
            main.startColor = baseCol;

            // Gradient fade to transparent
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(baseCol, 0f), new GradientColorKey(baseCol * 1.2f, 0.4f), new GradientColorKey(baseCol * 0.5f, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            // Shape and emission customized per category
            switch (effectType)
            {
                case SpellEffectType.ArcaneNova:
                case SpellEffectType.FrostNova:
                case SpellEffectType.ElectricShockRing:
                case SpellEffectType.FireNovaRing:
                    main.duration = 0.5f;
                    main.startLifetime = 0.55f;
                    main.startSpeed = 8.5f;
                    main.startSize = 0.65f;
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 0.5f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
                    break;

                case SpellEffectType.FlamePillarGeyser:
                case SpellEffectType.RadiantHolyPillar:
                case SpellEffectType.IceSpikeEruption:
                    main.duration = 0.8f;
                    main.startLifetime = 0.75f;
                    main.startSpeed = 12.0f;
                    main.startSize = 0.85f;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 12f;
                    shape.radius = 0.8f;
                    emission.rateOverTime = 80f;
                    break;

                case SpellEffectType.ArcaneVortexRift:
                case SpellEffectType.FireTornadoVortex:
                case SpellEffectType.AbyssalDarkVortex:
                    main.duration = 1.2f;
                    main.startLifetime = 0.9f;
                    main.startSpeed = 4.0f;
                    main.startSize = 0.5f;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 35f;
                    emission.rateOverTime = 65f;
                    var rotBySpeed = ps.rotationBySpeed;
                    rotBySpeed.enabled = true;
                    break;

                default: // Bursts and Impacts
                    main.duration = 0.4f;
                    main.startLifetime = 0.45f;
                    main.startSpeed = 6.0f;
                    main.startSize = 0.55f;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.35f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
                    break;
            }
        }

        private Color GetColorForEffect(SpellEffectType effectType)
        {
            return effectType switch
            {
                // Arcane (Magenta / Indigo)
                SpellEffectType.ArcaneMissileHit or SpellEffectType.ArcaneNova or
                SpellEffectType.ArcaneShieldAura or SpellEffectType.ArcaneVortexRift or
                SpellEffectType.ArcaneSpiralRune or SpellEffectType.ArcaneBurstGlow or
                SpellEffectType.ArcaneEnergyLance or SpellEffectType.ArcaneTeleportFlash
                    => new Color(0.85f, 0.25f, 1.0f, 1f),

                // Frost (Cyan / Ice Blue)
                SpellEffectType.FrostNova or SpellEffectType.IceSpikeEruption or
                SpellEffectType.BlizzardSwirl or SpellEffectType.GlacialFreezeBurst or
                SpellEffectType.FreezingMistPuff or SpellEffectType.IceShardImpact or
                SpellEffectType.FrostArmorGlow or SpellEffectType.PermafrostChill
                    => new Color(0.35f, 0.88f, 1.0f, 1f),

                // Electric (Neon Electric Cyan / Gold)
                SpellEffectType.ElectricShockRing or SpellEffectType.GalvanicSparkFountain or
                SpellEffectType.PlasmaBallCharge or SpellEffectType.StaticFieldAura or
                SpellEffectType.TeslaCoilDischarge or SpellEffectType.ThunderShockImpact or
                SpellEffectType.OverloadArcBlast or SpellEffectType.VoltShieldPulse
                    => new Color(0.25f, 0.92f, 1.0f, 1f),

                // Fire (Vibrant Crimson Gold)
                SpellEffectType.FireballExplosion or SpellEffectType.FlamePillarGeyser or
                SpellEffectType.FireNovaRing or SpellEffectType.EmberCloudDrift or
                SpellEffectType.FlamethrowerStream or SpellEffectType.FireTornadoVortex or
                SpellEffectType.IgnitedGroundPatch or SpellEffectType.CombustionDetonation
                    => new Color(1.0f, 0.45f, 0.10f, 1f),

                // Nature / Poison (Emerald / Lime)
                SpellEffectType.ToxicPoisonCloud or SpellEffectType.AcidSplashPuddle or
                SpellEffectType.ThornBrierBurst or SpellEffectType.SporeBloomDrift or
                SpellEffectType.PoisonDripHit or SpellEffectType.VineEntangleGlow or
                SpellEffectType.VerdantBloomAura or SpellEffectType.LeechSeedDrain
                    => new Color(0.32f, 0.95f, 0.28f, 1f),

                // Holy (Pure Gold / Warm Radiance)
                SpellEffectType.DivineHealingShower or SpellEffectType.RadiantHolyPillar or
                SpellEffectType.CelestialBlessingAura or SpellEffectType.SunlightFlash
                    => new Color(1.0f, 0.92f, 0.45f, 1f),

                // Dark (Abyssal Void Purple / Black-Crimson)
                _ => new Color(0.42f, 0.08f, 0.65f, 1f)
            };
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("--- 48 ELEMENTAL SPELL PARTICLES ---");
                Instance = go.AddComponent<ElementalSpellParticlePack>();
            }
        }
    }
}

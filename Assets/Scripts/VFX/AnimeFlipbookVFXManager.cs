using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.VFX
{
    public enum FlipbookEffectType
    {
        AnimeSlashCrescent,
        ComicSmokePoof,
        ImpactHitSparkle,
        RadialShockwaveRing
    }

    /// <summary>
    /// Anime Flipbook VFX Manager (Free Flipbook VFX Bundle Lite architecture).
    /// Generates and coordinates hand-drawn animated 2D/3D spritesheet flipbook particles
    /// for weapon slashes, smoke puffs, and comic impact sparks with mobile-friendly batching.
    /// </summary>
    public class AnimeFlipbookVFXManager : MonoBehaviour
    {
        public static AnimeFlipbookVFXManager Instance { get; private set; }

        private readonly Dictionary<FlipbookEffectType, Texture2D> flipbookTextures = new Dictionary<FlipbookEffectType, Texture2D>();
        private Material flipbookParticleMat;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeMaterialsAndTextures();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeMaterialsAndTextures()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                 Shader.Find("Universal Render Pipeline/Lit");

            flipbookParticleMat = new Material(unlitShader);
            flipbookParticleMat.SetFloat("_Surface", 1); // Transparent
            flipbookParticleMat.SetFloat("_Blend", 1);   // Additive

            // Generate procedural 4x4 flipbook sheets
            flipbookTextures[FlipbookEffectType.AnimeSlashCrescent] = GenerateSlashFlipbookTexture();
            flipbookTextures[FlipbookEffectType.ComicSmokePoof] = GenerateSmokeFlipbookTexture();
            flipbookTextures[FlipbookEffectType.ImpactHitSparkle] = GenerateSparkleFlipbookTexture();
        }

        /// <summary>
        /// Plays an animated anime flipbook particle burst at world position.
        /// </summary>
        public static GameObject PlayFlipbook(FlipbookEffectType type, Vector3 position, Quaternion? rotation = null, float scale = 1.0f)
        {
            EnsureInstance();
            Quaternion rot = rotation ?? Quaternion.identity;
            return Instance.SpawnFlipbookParticle(type, position, rot, scale);
        }

        public GameObject SpawnFlipbookParticle(FlipbookEffectType type, Vector3 position, Quaternion rotation, float scale)
        {
            var go = new GameObject($"FX_Flipbook_{type}");
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * scale;

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();

            // Setup material with flipbook texture
            var mat = new Material(flipbookParticleMat);
            if (flipbookTextures.TryGetValue(type, out var tex))
            {
                mat.mainTexture = tex;
                mat.SetColor("_BaseColor", GetFlipbookColor(type));
            }
            psr.sharedMaterial = mat;

            var main = ps.main;
            main.duration = 0.35f;
            main.startLifetime = 0.35f;
            main.startSpeed = 0.5f;
            main.startSize = 2.0f * scale;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            // Texture Sheet Animation module (4x4 tiles = 16 frames)
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.numTilesX = 4;
            tsa.numTilesY = 4;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.cycleCount = 1;

            ps.Play();
            Destroy(go, 0.6f);
            return go;
        }

        private Color GetFlipbookColor(FlipbookEffectType type)
        {
            return type switch
            {
                FlipbookEffectType.AnimeSlashCrescent => new Color(0.35f, 0.88f, 1.0f, 1f), // Neon cyan slash
                FlipbookEffectType.ComicSmokePoof => new Color(0.95f, 0.95f, 0.92f, 0.85f),  // Warm cream smoke
                FlipbookEffectType.ImpactHitSparkle => new Color(1.0f, 0.90f, 0.25f, 1f),   // Golden star spark
                _ => Color.white
            };
        }

        private Texture2D GenerateSlashFlipbookTexture()
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            int tiles = 4;
            int tileSize = size / tiles;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int tileX = x / tileSize;
                    int tileY = y / tileSize;
                    int frameIndex = (3 - tileY) * tiles + tileX;

                    float localX = (float)(x % tileSize) / tileSize * 2f - 1f;
                    float localY = (float)(y % tileSize) / tileSize * 2f - 1f;
                    float dist = Mathf.Sqrt(localX * localX + localY * localY);

                    // Crescent arc progress based on frame
                    float sweepProgress = (float)frameIndex / 15f;
                    float angle = Mathf.Atan2(localY, localX);
                    float arcMask = Mathf.Abs(angle - Mathf.Lerp(-2.5f, 2.5f, sweepProgress));

                    float crescent = Mathf.Exp(-Mathf.Pow(dist - 0.7f, 2f) * 60f) * Mathf.Exp(-arcMask * 4f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, crescent));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateSmokeFlipbookTexture()
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            int tiles = 4;
            int tileSize = size / tiles;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int tileX = x / tileSize;
                    int tileY = y / tileSize;
                    int frameIndex = (3 - tileY) * tiles + tileX;

                    float localX = (float)(x % tileSize) / tileSize * 2f - 1f;
                    float localY = (float)(y % tileSize) / tileSize * 2f - 1f;
                    float dist = Mathf.Sqrt(localX * localX + localY * localY);

                    float expandRadius = Mathf.Lerp(0.15f, 0.85f, (float)frameIndex / 15f);
                    float fade = 1.0f - ((float)frameIndex / 15f);
                    float puff = Mathf.Clamp01(1.0f - Mathf.Abs(dist - expandRadius) * 3.5f) * fade;

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, puff));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateSparkleFlipbookTexture()
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            int tiles = 4;
            int tileSize = size / tiles;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int tileX = x / tileSize;
                    int tileY = y / tileSize;
                    int frameIndex = (3 - tileY) * tiles + tileX;

                    float localX = (float)(x % tileSize) / tileSize * 2f - 1f;
                    float localY = (float)(y % tileSize) / tileSize * 2f - 1f;

                    // 4-point star ray cross
                    float star = Mathf.Exp(-Mathf.Abs(localX * localY) * 45f) * Mathf.Exp(-(localX * localX + localY * localY) * 2f);
                    float fade = 1.0f - Mathf.Abs(((float)frameIndex / 15f) - 0.5f) * 2.0f;

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, star * fade));
                }
            }
            tex.Apply();
            return tex;
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("--- ANIME FLIPBOOK VFX MANAGER ---");
                Instance = go.AddComponent<AnimeFlipbookVFXManager>();
            }
        }
    }
}

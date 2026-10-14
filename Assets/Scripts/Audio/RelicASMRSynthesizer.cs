using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RelicASMRSynthesizer provides studio-grade procedural ASMR audio synthesis for 3D puzzle inspection.
/// All sound effects are synthesized mathematically and played through an AudioSource with
/// bypassListenerEffects = true so that external audio can be muffled while the puzzle sounds
/// remain crystal-clear, intimate, and high-fidelity.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RelicASMRSynthesizer : MonoBehaviour
{
    public static RelicASMRSynthesizer Instance { get; private set; }

    [Header("Audio Output")]
    [SerializeField] private AudioSource asmrSource;
    [SerializeField] private int sampleRate = 44100;
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1.0f;

    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (asmrSource == null) asmrSource = GetComponent<AudioSource>();
        if (asmrSource == null) asmrSource = gameObject.AddComponent<AudioSource>();

        // Critical: Bypass AudioListener filters (LowPass, Reverb) so relic ASMR remains crystal clear
        asmrSource.playOnAwake = false;
        asmrSource.spatialBlend = 0f; // 2D binaural/intimate feel
        asmrSource.bypassListenerEffects = true;
        asmrSource.bypassEffects = true;
        asmrSource.priority = 0; // Highest priority

        SynthesizeASMRBuffers();
    }

    private void SynthesizeASMRBuffers()
    {
        clipCache["metallic_click"] = GenerateMetallicRatchet();
        clipCache["tumbler_slide"] = GenerateTumblerSlide();
        clipCache["heavy_latch"] = GenerateHeavyLatch();
        clipCache["crystal_bell"] = GenerateCrystalBell();
        clipCache["pneumatic_hiss"] = GeneratePneumaticHiss();
        clipCache["hollow_knock"] = GenerateHollowKnock();
    }

    #region Procedural ASMR Waveform Generators
    private AudioClip GenerateMetallicRatchet()
    {
        float duration = 0.055f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(42);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Primary high-frequency click (4.2 kHz)
            float strike = Mathf.Sin(2f * Mathf.PI * 4200f * t) * Mathf.Exp(-120f * t);
            // Metallic overtone (8.4 kHz)
            float ring = Mathf.Sin(2f * Mathf.PI * 8400f * t) * Mathf.Exp(-95f * t) * 0.4f;
            // Micro friction transient
            float friction = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-160f * t) * 0.25f;

            samples[i] = Mathf.Clamp((strike * 0.65f + ring + friction) * 1.2f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("asmr_metallic_click", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateTumblerSlide()
    {
        float duration = 0.14f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(108);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Bandpassed metallic friction noise
            float noise = ((float)rnd.NextDouble() * 2f - 1f);
            float grain = Mathf.Sin(2f * Mathf.PI * 2600f * t) * noise;
            float slideEnvelope = Mathf.Sin(Mathf.PI * (t / duration)); // Bow-shaped envelope
            // Subtle scraping sub-resonance
            float rumble = Mathf.Sin(2f * Mathf.PI * 480f * t) * 0.2f;

            samples[i] = Mathf.Clamp((grain * 0.7f + rumble) * slideEnvelope * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("asmr_tumbler_slide", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateHeavyLatch()
    {
        float duration = 0.18f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(777);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Deep low-frequency mechanical thud (110 Hz)
            float thud = Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-32f * t);
            // Sharp metallic impact snap (2.8 kHz)
            float snap = Mathf.Sin(2f * Mathf.PI * 2800f * t) * Mathf.Exp(-80f * t) * 0.7f;
            // Small spring rattle rebound at 35ms
            float rebound = (t > 0.035f) ? Mathf.Sin(2f * Mathf.PI * 1800f * (t - 0.035f)) * Mathf.Exp(-65f * (t - 0.035f)) * 0.35f : 0f;
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-90f * t) * 0.15f;

            samples[i] = Mathf.Clamp((thud * 0.5f + snap * 0.45f + rebound + noise) * 1.3f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("asmr_heavy_latch", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateCrystalBell()
    {
        float duration = 1.6f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        float f0 = 1174.66f; // D6 Note (Celestial astral key)
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Four pristine partial harmonics (chime ratios)
            float p0 = Mathf.Sin(2f * Mathf.PI * f0 * t) * Mathf.Exp(-3.2f * t);
            float p1 = Mathf.Sin(2f * Mathf.PI * (f0 * 2.76f) * t) * Mathf.Exp(-4.5f * t) * 0.45f;
            float p2 = Mathf.Sin(2f * Mathf.PI * (f0 * 5.40f) * t) * Mathf.Exp(-6.8f * t) * 0.25f;
            float p3 = Mathf.Sin(2f * Mathf.PI * (f0 * 8.93f) * t) * Mathf.Exp(-9.5f * t) * 0.12f;
            // Shimmer vibrato / tremolo
            float shimmer = 1.0f + 0.08f * Mathf.Sin(2f * Mathf.PI * 7.5f * t);

            samples[i] = Mathf.Clamp((p0 + p1 + p2 + p3) * shimmer * 0.65f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("asmr_crystal_bell", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GeneratePneumaticHiss()
    {
        float duration = 0.42f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(999);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float noise = ((float)rnd.NextDouble() * 2f - 1f);
            // High-pressure release envelope: rapid onset, smooth exponential decay
            float envelope = Mathf.Pow(t / 0.04f, 1.2f) * Mathf.Exp(-8.5f * t);
            // Whispering air resonance (filtered breath)
            float air = Mathf.Sin(2f * Mathf.PI * 3400f * t) * 0.2f + noise * 0.8f;

            samples[i] = Mathf.Clamp(air * envelope * 0.6f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("asmr_pneumatic_hiss", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateHollowKnock()
    {
        float duration = 0.11f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Dual-mode hollow acoustic chamber resonance (210 Hz & 390 Hz)
            float lowMode = Mathf.Sin(2f * Mathf.PI * 210f * t) * Mathf.Exp(-36f * t);
            float highMode = Mathf.Sin(2f * Mathf.PI * 390f * t) * Mathf.Exp(-44f * t) * 0.55f;
            float click = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-110f * t) * 0.35f;

            samples[i] = Mathf.Clamp((lowMode + highMode + click) * 0.95f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("asmr_hollow_knock", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion

    #region Public Playback API
    public void PlayClick(float volume = 1f, float pitch = 1f)
    {
        PlayClip("metallic_click", volume, pitch);
    }

    public void PlaySlide(float volume = 0.8f, float pitch = 1f)
    {
        PlayClip("tumbler_slide", volume, pitch);
    }

    public void PlayLatch(float volume = 1f, float pitch = 1f)
    {
        PlayClip("heavy_latch", volume, pitch);
    }

    public void PlayCrystalChime(float volume = 1f, float pitch = 1f)
    {
        PlayClip("crystal_bell", volume, pitch);
    }

    public void PlayPneumaticHiss(float volume = 0.9f)
    {
        PlayClip("pneumatic_hiss", volume, 1f);
    }

    public void PlayHollowKnock(float volume = 0.85f, float pitch = 1f)
    {
        PlayClip("hollow_knock", volume, pitch);
    }

    private void PlayClip(string key, float volume, float pitch)
    {
        if (clipCache.TryGetValue(key, out AudioClip clip) && asmrSource != null)
        {
            asmrSource.pitch = pitch;
            asmrSource.PlayOneShot(clip, volume * masterVolume);
        }
    }
    #endregion
}

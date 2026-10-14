using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class SfxCue { public string preset; public float timestamp; }

/// <summary>
/// BattleSummonerAudioMaster controls all procedural audio synthesis buffers,
/// regex-based [SFX: preset | at: Xs | vol: Y%] extraction from LLM responses,
/// and hard -28% gain reduction ducking on SFX channels whenever voiceover is active.
/// </summary>
public class BattleSummonerAudioMaster : MonoBehaviour
{
    public static BattleSummonerAudioMaster Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource vocalSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("Ducking Configuration")]
    [Range(0.1f, 1f)] private float duckingAttenuation = 0.72f;
    [Tooltip("Hard -28% gain reduction when voiceover is active.")]
    [SerializeField] private float duckingGainReduction = 0.28f;
    [SerializeField] private float baseSfxVolume = 1.0f;
    [SerializeField] private float duckingSmoothTime = 0.08f;

    [Header("Procedural Presets")]
    [SerializeField] private int sampleRate = 44100;

    private readonly Dictionary<string, AudioClip> proceduralClipCache = new Dictionary<string, AudioClip>();
    private Coroutine duckingCoroutine;
    private bool isVoiceoverActive = false;

    private readonly Regex sfxRegex = new Regex(@"\[SFX:\s*([a-zA-Z0-9_\-]+)(?:\s*\|\s*at:\s*([0-9.]+)s?)?\]", RegexOptions.IgnoreCase);

    // Regex to match [SFX: preset | at: Xs | vol: Y%]
    private static readonly Regex sfxTagRegex = new Regex(
        @"\[SFX:\s*([a-zA-Z0-9_\-]+)\s*\|\s*at:\s*([0-9\.]+)s\s*\|\s*vol:\s*([0-9\.]+)%?\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string ParseCuesAndStripText(string rawScript, out List<SfxCue> cues)
    {
        cues = new List<SfxCue>();
        foreach (Match match in sfxRegex.Matches(rawScript))
        {
            cues.Add(new SfxCue { preset = match.Groups[1].Value.ToLower(), timestamp = match.Groups[2].Success ? float.Parse(match.Groups[2].Value) : 0f });
        }
        return sfxRegex.Replace(rawScript, "").Trim();
    }

    public AudioClip GetProceduralClip(string key)
    {
        if (proceduralClipCache.TryGetValue(key.ToLower(), out var clip))
        {
            return clip;
        }
        return null;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeAudioComponents();
            GenerateProceduralAudioBuffers();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioComponents()
    {
        if (vocalSource == null)
        {
            vocalSource = gameObject.AddComponent<AudioSource>();
            vocalSource.playOnAwake = false;
            vocalSource.spatialBlend = 0f; // 2D clean voiceover
            vocalSource.priority = 10;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = baseSfxVolume;
        }

        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.volume = 0.4f;
        }
    }

    #region Procedural Audio Buffer Synthesis
    private void GenerateProceduralAudioBuffers()
    {
        proceduralClipCache["cinematic_boom"] = SynthesizeCinematicBoom();
        proceduralClipCache["glitch"] = SynthesizeGlitch();
        proceduralClipCache["anvil_shatter"] = SynthesizeAnvilShatter();
        proceduralClipCache["singularity_whir"] = SynthesizeSingularityWhir();
        proceduralClipCache["relic_chime"] = SynthesizeRelicChime();
        proceduralClipCache["needle_trap"] = SynthesizeNeedleTrap();
        proceduralClipCache["hunting_horn_fanfare"] = SynthesizeHuntingHornFanfare();
        proceduralClipCache["malformed_wheeze"] = SynthesizeMalformedWheeze();
        proceduralClipCache["apex_celestial_roar"] = SynthesizeApexCelestialRoar();
        proceduralClipCache["sword_whoosh"] = SynthesizeSwordWhoosh();
        proceduralClipCache["sword_impact"] = SynthesizeSwordImpact();
        proceduralClipCache["spell_fireball_launch"] = SynthesizeSpellLaunch();
        proceduralClipCache["bard_chord_strum"] = SynthesizeBardChord();
        proceduralClipCache["loadout_switch"] = SynthesizeLoadoutSwitch();
        proceduralClipCache["margo_chime"] = SynthesizeMargoChime();
        proceduralClipCache["margo_ward"] = SynthesizeMargoWard();
        proceduralClipCache["pet_happy_chirp"] = SynthesizePetHappyChirp();
        proceduralClipCache["pet_munch"] = SynthesizePetMunch();
        proceduralClipCache["pet_slurp"] = SynthesizePetSlurp();
        proceduralClipCache["pet_bubble_bath"] = SynthesizePetBubbleBath();
        proceduralClipCache["pet_present_fanfare"] = SynthesizePetPresentFanfare();
        proceduralClipCache["portal_ambient_hum"] = SynthesizePortalHum();
        proceduralClipCache["portal_warp"] = SynthesizePortalWarp();
        proceduralClipCache["ror_tier_up"] = SynthesizeRoRTierUp();
        proceduralClipCache["boon_card_reveal"] = SynthesizeBoonCardReveal();
        proceduralClipCache["boon_card_select"] = SynthesizeBoonCardSelect();

        PreloadKalimbaTines();
    }

    private AudioClip SynthesizePetHappyChirp()
    {
        float duration = 0.35f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(600f, 1320f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float sine2 = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.3f;
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = Mathf.Clamp((sine + sine2) * envelope * 0.7f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("pet_happy_chirp", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizePetMunch()
    {
        float duration = 0.3f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(77);
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float pop = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-20f * (t % 0.1f));
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-15f * (t % 0.1f)) * 0.3f;
            samples[i] = Mathf.Clamp((pop + noise) * 0.8f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("pet_munch", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizePetSlurp()
    {
        float duration = 0.4f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float bubbleFreq = 300f + Mathf.Sin(2f * Mathf.PI * 25f * t) * 80f;
            float sine = Mathf.Sin(2f * Mathf.PI * bubbleFreq * t);
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = Mathf.Clamp(sine * envelope * 0.6f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("pet_slurp", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizePetBubbleBath()
    {
        float duration = 0.6f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(123);
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float chirp = Mathf.Sin(2f * Mathf.PI * (450f + 600f * t) * t);
            float froth = ((float)rnd.NextDouble() * 2f - 1f) * 0.25f;
            float envelope = Mathf.Exp(-3f * t);
            samples[i] = Mathf.Clamp((chirp * 0.5f + froth) * envelope * 0.8f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("pet_bubble_bath", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizePetPresentFanfare()
    {
        float duration = 0.8f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float c5 = Mathf.Sin(2f * Mathf.PI * 523.25f * t) * Mathf.Exp(-4f * t);
            float e5 = t > 0.15f ? Mathf.Sin(2f * Mathf.PI * 659.25f * (t - 0.15f)) * Mathf.Exp(-4f * (t - 0.15f)) : 0f;
            float g5 = t > 0.30f ? Mathf.Sin(2f * Mathf.PI * 783.99f * (t - 0.30f)) * Mathf.Exp(-4f * (t - 0.30f)) : 0f;
            float c6 = t > 0.45f ? Mathf.Sin(2f * Mathf.PI * 1046.50f * (t - 0.45f)) * Mathf.Exp(-3f * (t - 0.45f)) : 0f;
            samples[i] = Mathf.Clamp((c5 + e5 + g5 + c6) * 0.7f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("pet_present_fanfare", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizePortalHum()
    {
        float duration = 2.0f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float lfo = 1f + 0.25f * Mathf.Sin(2f * Mathf.PI * 2.5f * t);
            float baseTone = Mathf.Sin(2f * Mathf.PI * 65f * lfo * t);
            float harmonic = Mathf.Sin(2f * Mathf.PI * 130f * t) * 0.35f;
            float sub = Mathf.Sin(2f * Mathf.PI * 32.5f * t) * 0.5f;
            samples[i] = Mathf.Clamp((baseTone + harmonic + sub) * 0.5f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("portal_ambient_hum", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizePortalWarp()
    {
        float duration = 0.9f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(800f, 60f, Mathf.Pow(t / duration, 2.2f));
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = Mathf.Clamp(sine * envelope * 0.85f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("portal_warp", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeRoRTierUp()
    {
        float duration = 1.2f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(404);
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Metallic strike impact
            float gong1 = Mathf.Sin(2f * Mathf.PI * 380f * t) * Mathf.Exp(-4.5f * t);
            float gong2 = Mathf.Sin(2f * Mathf.PI * 570f * t) * Mathf.Exp(-5.5f * t) * 0.6f;
            float clang = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-30f * t) * 0.4f;
            // Descending ominous drone
            float drone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 75f, t / duration) * t) * Mathf.Exp(-1.8f * t) * 0.7f;
            samples[i] = Mathf.Clamp((gong1 + gong2 + clang + drone) * 0.8f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("ror_tier_up", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeBoonCardReveal()
    {
        float duration = 0.75f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float d6 = Mathf.Sin(2f * Mathf.PI * 1174.66f * t) * Mathf.Exp(-5f * t);
            float f6 = t > 0.12f ? Mathf.Sin(2f * Mathf.PI * 1396.91f * (t - 0.12f)) * Mathf.Exp(-5f * (t - 0.12f)) : 0f;
            float a6 = t > 0.24f ? Mathf.Sin(2f * Mathf.PI * 1760.00f * (t - 0.24f)) * Mathf.Exp(-5f * (t - 0.24f)) : 0f;
            float d7 = t > 0.36f ? Mathf.Sin(2f * Mathf.PI * 2349.32f * (t - 0.36f)) * Mathf.Exp(-4f * (t - 0.36f)) : 0f;
            samples[i] = Mathf.Clamp((d6 + f6 + a6 + d7) * 0.65f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("boon_card_reveal", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeBoonCardSelect()
    {
        float duration = 1.0f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float surge = Mathf.Sin(2f * Mathf.PI * (110f + 220f * (1f - t)) * t) * Mathf.Exp(-2.5f * t);
            float bass = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-2.0f * t);
            samples[i] = Mathf.Clamp((surge * 0.6f + bass * 0.5f), -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("boon_card_select", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeCinematicBoom()
    {
        float duration = 1.8f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(120f, 28f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float subBass = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t);

            // Sub-bass saturation + exponential decay envelope
            float envelope = Mathf.Exp(-3.2f * t);
            float noise = ((float)new System.Random(i).NextDouble() * 2f - 1f) * 0.15f * Mathf.Exp(-12f * t);
            float sample = Mathf.Clamp((sine * 0.6f + subBass * 0.4f + noise) * envelope, -1f, 1f);
            samples[i] = sample;
        }

        AudioClip clip = AudioClip.Create("cinematic_boom", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeGlitch()
    {
        float duration = 0.45f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(1337);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float carrier = Mathf.Sin(2f * Mathf.PI * 880f * t);
            float mod = Mathf.Sin(2f * Mathf.PI * 140f * t);
            float noise = ((float)rnd.NextDouble() * 2f - 1f);

            // Quantized bitcrush effect
            float raw = (carrier * mod + noise * 0.35f);
            float quantized = Mathf.Round(raw * 6f) / 6f;
            float envelope = (1f - (t / duration));
            samples[i] = quantized * envelope * 0.8f;
        }

        AudioClip clip = AudioClip.Create("glitch", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeAnvilShatter()
    {
        float duration = 1.2f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(42);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float bell1 = Mathf.Sin(2f * Mathf.PI * 1450f * t) * Mathf.Exp(-8f * t);
            float bell2 = Mathf.Sin(2f * Mathf.PI * 2800f * t) * Mathf.Exp(-14f * t);
            float transient = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-40f * t);
            samples[i] = (bell1 * 0.5f + bell2 * 0.3f + transient * 0.5f) * 0.9f;
        }

        AudioClip clip = AudioClip.Create("anvil_shatter", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeSingularityWhir()
    {
        float duration = 2.2f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(45f, 960f, Mathf.Pow(t / duration, 2.5f));
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float phaseLfo = Mathf.Sin(2f * Mathf.PI * 8f * t) * 0.25f;
            float envelope = Mathf.Sin((t / duration) * Mathf.PI);
            samples[i] = Mathf.Clamp((sine + phaseLfo) * envelope * 0.75f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("singularity_whir", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeRelicChime()
    {
        float duration = 1.0f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float tone1 = Mathf.Sin(2f * Mathf.PI * 1046.50f * t) * Mathf.Exp(-4f * t); // C6
            float tone2 = Mathf.Sin(2f * Mathf.PI * 1318.51f * t) * Mathf.Exp(-5f * t); // E6
            float tone3 = Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * Mathf.Exp(-6f * t); // G6
            samples[i] = (tone1 * 0.4f + tone2 * 0.3f + tone3 * 0.3f);
        }

        AudioClip clip = AudioClip.Create("relic_chime", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeNeedleTrap()
    {
        float duration = 0.3f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(99);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float snap = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-60f * t);
            float hiss = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Sin(2f * Mathf.PI * 4000f * t) * Mathf.Exp(-12f * t);
            samples[i] = (snap * 0.7f + hiss * 0.3f);
        }

        AudioClip clip = AudioClip.Create("needle_trap", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeHuntingHornFanfare()
    {
        float duration = 1.4f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        // Triumphant fifths and octaves: C4 (261.6), G4 (392.0), C5 (523.2), E5 (659.2)
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-2.2f * t) * Mathf.Min(1f, t * 40f);
            float h1 = Mathf.Sin(2f * Mathf.PI * 261.63f * t);
            float h2 = Mathf.Sin(2f * Mathf.PI * 392.00f * t);
            float h3 = Mathf.Sin(2f * Mathf.PI * 523.25f * t);
            float h4 = Mathf.Sin(2f * Mathf.PI * 659.25f * t);
            samples[i] = Mathf.Clamp((h1 * 0.3f + h2 * 0.25f + h3 * 0.25f + h4 * 0.2f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("hunting_horn_fanfare", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeMalformedWheeze()
    {
        float duration = 1.2f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(42);

        // Wheezy asthmatic descending pitch with irregular breath noise
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float pitch = Mathf.Lerp(160f, 95f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * pitch * t);
            float breath = ((float)rnd.NextDouble() * 2f - 1f) * 0.4f;
            float envelope = Mathf.Exp(-1.8f * t) * (1f + 0.3f * Mathf.Sin(2f * Mathf.PI * 8f * t));
            samples[i] = Mathf.Clamp((sine * 0.4f + breath) * envelope * 0.8f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("malformed_wheeze", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeApexCelestialRoar()
    {
        float duration = 2.2f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        // Deep sub-bass roar coupled with crystalline astral bells (1046Hz, 1568Hz)
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-1.5f * t);
            float shimmer = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * Mathf.Exp(-3.5f * t);
            float shimmer2 = Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * Mathf.Exp(-4.0f * t);
            float roar = Mathf.Sin(2f * Mathf.PI * (110f + 20f * Mathf.Sin(2f * Mathf.PI * 5f * t)) * t) * Mathf.Exp(-2.0f * t);
            samples[i] = Mathf.Clamp(sub * 0.45f + shimmer * 0.25f + shimmer2 * 0.15f + roar * 0.35f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("apex_celestial_roar", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeSwordWhoosh()
    {
        float duration = 0.22f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(42);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(750f, 180f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * 0.4f;
            float envelope = Mathf.Sin(Mathf.PI * (t / duration));
            samples[i] = Mathf.Clamp((sine * 0.6f + noise) * envelope * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("sword_whoosh", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeSwordImpact()
    {
        float duration = 0.28f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];
        System.Random rnd = new System.Random(77);

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float metalClang = Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Exp(-28f * t);
            float metalRing = Mathf.Sin(2f * Mathf.PI * 920f * t) * Mathf.Exp(-12f * t);
            float thud = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-18f * t);
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-35f * t);
            samples[i] = Mathf.Clamp(metalClang * 0.45f + metalRing * 0.35f + thud * 0.4f + noise * 0.25f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("sword_impact", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeSpellLaunch()
    {
        float duration = 0.35f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(260f, 980f, t / duration);
            float chirp = Mathf.Sin(2f * Mathf.PI * freq * t);
            float sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t);
            float envelope = Mathf.Exp(-4.0f * t) * Mathf.Min(1f, t * 80f);
            samples[i] = Mathf.Clamp((chirp * 0.6f + sub * 0.4f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("spell_fireball_launch", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeBardChord()
    {
        float duration = 1.2f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        // C-Major Triad (C4 261.63, E4 329.63, G4 392.00, C5 523.25)
        float[] chordFreqs = { 261.63f, 329.63f, 392.00f, 523.25f };
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float chord = 0f;
            for (int c = 0; c < chordFreqs.Length; c++)
            {
                chord += Mathf.Sin(2f * Mathf.PI * chordFreqs[c] * t) * Mathf.Exp(-(2.5f + c * 0.4f) * t);
            }
            samples[i] = Mathf.Clamp(chord * 0.28f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("bard_chord_strum", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeLoadoutSwitch()
    {
        float duration = 0.4f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float tone1 = Mathf.Sin(2f * Mathf.PI * 659.25f * t) * Mathf.Exp(-8f * t); // E5
            float tone2 = Mathf.Sin(2f * Mathf.PI * 987.77f * t) * Mathf.Exp(-10f * t); // B5
            samples[i] = Mathf.Clamp((tone1 * 0.5f + tone2 * 0.5f), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("loadout_switch", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeMargoChime()
    {
        float duration = 0.95f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        // Ethereal gothic minor bell: D5 (587.33Hz) + F5 (698.46Hz) + minor 7th C6 (1046.5Hz) shimmer
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float toneD = Mathf.Sin(2f * Mathf.PI * 587.33f * t) * Mathf.Exp(-4.2f * t);
            float toneF = Mathf.Sin(2f * Mathf.PI * 698.46f * t) * Mathf.Exp(-4.8f * t);
            float toneC = Mathf.Sin(2f * Mathf.PI * 1046.50f * t) * Mathf.Exp(-7.5f * t) * 0.35f;
            float pulse = Mathf.Sin(2f * Mathf.PI * 4.5f * t); // subtle occult tremolo
            float sample = (toneD * 0.45f + toneF * 0.40f + toneC) * (1.0f + 0.15f * pulse);
            samples[i] = Mathf.Clamp(sample * 0.75f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("margo_chime", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SynthesizeMargoWard()
    {
        float duration = 1.35f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        // Deep occult shield pulse: rising sub-harmonic resonance with crystalline shimmer
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(110f, 440f, Mathf.Pow(t / duration, 0.5f));
            float bassPulse = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-2.8f * t);
            float shimmer = Mathf.Sin(2f * Mathf.PI * (freq * 3f) * t) * Mathf.Exp(-4.5f * t) * 0.35f;
            samples[i] = Mathf.Clamp((bassPulse * 0.7f + shimmer * 0.3f), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("margo_ward", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PreloadKalimbaTines()
    {
        string[] noteNames = {
            "C4", "D4", "E4", "F4", "G4", "A4", "B4",
            "C5", "D5", "E5", "F5", "G5", "A5", "B5",
            "C6", "D6", "E6"
        };
        float[] freqs = {
            261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 493.88f,
            523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880.00f, 987.77f,
            1046.50f, 1174.66f, 1318.51f
        };

        for (int i = 0; i < noteNames.Length; i++)
        {
            string key = "kalimba_" + noteNames[i].ToLowerInvariant();
            proceduralClipCache[key] = SynthesizeKalimbaTine(freqs[i], noteNames[i]);
        }
    }

    public AudioClip SynthesizeKalimbaTine(float frequency, string noteLabel = "")
    {
        float duration = 1.6f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        // Kalimba acoustics:
        // Fundamental sine + strike ping (2.75x) + metallic overtone (5.4x) + wood resonance body
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;

            // Attack transient thump
            float attack = Mathf.Exp(-65f * t);
            float ping = Mathf.Sin(2f * Mathf.PI * (frequency * 2.75f) * t) * Mathf.Exp(-18f * t);
            float highOvertone = Mathf.Sin(2f * Mathf.PI * (frequency * 5.4f) * t) * Mathf.Exp(-32f * t);

            // Fundamental tone with gentle decay
            float fundamental = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-2.4f * t);

            // Subtle body resonance
            float woodBody = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-14f * t) * 0.15f;

            float sample = (fundamental * 0.65f) + (ping * 0.22f) + (highOvertone * 0.08f) + (attack * 0.15f) + woodBody;
            samples[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(string.IsNullOrEmpty(noteLabel) ? $"kalimba_{frequency:F0}hz" : $"kalimba_{noteLabel}", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Plays an individual Kalimba tine note with zero latency.
    /// </summary>
    public void PlayKalimbaNote(string noteName, float frequency, float velocity = 1.0f)
    {
        string key = "kalimba_" + noteName.ToLowerInvariant();
        if (!proceduralClipCache.TryGetValue(key, out AudioClip clip))
        {
            clip = SynthesizeKalimbaTine(frequency, noteName);
            proceduralClipCache[key] = clip;
        }

        if (clip != null && sfxSource != null)
        {
            float targetVol = Mathf.Clamp01(velocity) * sfxSource.volume;
            sfxSource.PlayOneShot(clip, targetVol);
        }
    }
    #endregion

    #region SFX Parsing & Scheduled Execution
    /// <summary>
    /// Parses text containing [SFX: preset | at: Xs | vol: Y%] and schedules all sound cues.
    /// Also returns cleaned text with SFX tags stripped for clean UI display.
    /// </summary>
    public string ProcessAndScheduleSFXTags(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return rawText;

        MatchCollection matches = sfxTagRegex.Matches(rawText);
        foreach (Match match in matches)
        {
            string preset = match.Groups[1].Value.ToLowerInvariant();
            if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float delaySeconds) &&
                float.TryParse(match.Groups[3].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float volPercent))
            {
                float volumeNormalized = Mathf.Clamp01(volPercent / 100f);
                StartCoroutine(PlayScheduledPresetRoutine(preset, delaySeconds, volumeNormalized));
            }
        }

        // Return stripped text for dialogue boxes / text overlays
        return sfxTagRegex.Replace(rawText, "").Trim();
    }

    private IEnumerator PlayScheduledPresetRoutine(string presetName, float delaySeconds, float volume)
    {
        if (delaySeconds > 0.001f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        PlayPresetSFX(presetName, volume);
    }

    public void PlayPresetSFX(string presetName, float volume = 1.0f)
    {
        presetName = presetName.ToLowerInvariant();
        if (proceduralClipCache.TryGetValue(presetName, out AudioClip clip))
        {
            float targetVol = volume * sfxSource.volume;
            sfxSource.PlayOneShot(clip, targetVol);
        }
        else
        {
            // Default fallback chime
            if (proceduralClipCache.TryGetValue("relic_chime", out AudioClip fallbackClip))
            {
                sfxSource.PlayOneShot(fallbackClip, volume * sfxSource.volume);
            }
        }
    }
    #endregion

    #region Hard -28% SFX Ducking Engine
    /// <summary>
    /// Called when voiceover starts streaming or playing.
    /// Applies a hard -28% gain reduction to the SFX channel.
    /// </summary>
    public void NotifyVoiceoverStarted()
    {
        isVoiceoverActive = true;
        if (duckingCoroutine != null) StopCoroutine(duckingCoroutine);
        float duckedVolume = baseSfxVolume * duckingAttenuation; // Exact 0.72x (-28% reduction)
        duckingCoroutine = StartCoroutine(SmoothVolumeRoutine(sfxSource, duckedVolume, duckingSmoothTime));
    }

    /// <summary>
    /// Called when voiceover audio finishes playing.
    /// Restores full SFX channel volume.
    /// </summary>
    public void NotifyVoiceoverEnded()
    {
        isVoiceoverActive = false;
        if (duckingCoroutine != null) StopCoroutine(duckingCoroutine);
        duckingCoroutine = StartCoroutine(SmoothVolumeRoutine(sfxSource, baseSfxVolume, duckingSmoothTime * 1.5f));
    }

    private IEnumerator SmoothVolumeRoutine(AudioSource source, float targetVol, float duration)
    {
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, targetVol, elapsed / duration);
            yield return null;
        }
        source.volume = targetVol;
    }

    public AudioSource GetVocalSource() => vocalSource;
    public AudioSource GetSFXSource() => sfxSource;
    public bool IsVoiceoverActive => isVoiceoverActive;
    #endregion

    #region Relic Inspection Muffling Engine
    private AudioLowPassFilter listenerLowPassFilter;
    private Coroutine mufflingCoroutine;
    private bool isMuffledActive = false;

    public bool IsMuffledActive => isMuffledActive;

    /// <summary>
    /// Muffles all world sound effects, TTS vocal playback, and ambient music during relic inspection,
    /// so that intimate ASMR sounds produced by the inspected object are crystal clear.
    /// </summary>
    public void SetInspectionMuffledAudio(bool muffled, float duration = 0.35f)
    {
        isMuffledActive = muffled;
        if (listenerLowPassFilter == null)
        {
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            if (listener != null)
            {
                listenerLowPassFilter = listener.GetComponent<AudioLowPassFilter>();
                if (listenerLowPassFilter == null)
                {
                    listenerLowPassFilter = listener.gameObject.AddComponent<AudioLowPassFilter>();
                }
            }
        }

        if (listenerLowPassFilter != null)
        {
            listenerLowPassFilter.enabled = true;
            listenerLowPassFilter.lowpassResonanceQ = 1.15f;
        }

        if (mufflingCoroutine != null) StopCoroutine(mufflingCoroutine);
        mufflingCoroutine = StartCoroutine(SmoothMuffleRoutine(muffled, duration));
    }

    private IEnumerator SmoothMuffleRoutine(bool muffled, float duration)
    {
        float startFreq = (listenerLowPassFilter != null) ? listenerLowPassFilter.cutoffFrequency : 22000f;
        float targetFreq = muffled ? 650f : 22000f;

        float startAmb = (ambientSource != null) ? ambientSource.volume : 0.4f;
        float targetAmb = muffled ? 0.12f : 0.4f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (listenerLowPassFilter != null)
            {
                listenerLowPassFilter.cutoffFrequency = Mathf.Lerp(startFreq, targetFreq, t);
            }

            if (ambientSource != null)
            {
                ambientSource.volume = Mathf.Lerp(startAmb, targetAmb, t);
            }

            yield return null;
        }

        if (listenerLowPassFilter != null)
        {
            listenerLowPassFilter.cutoffFrequency = targetFreq;
            if (!muffled) listenerLowPassFilter.enabled = false;
        }

        if (ambientSource != null) ambientSource.volume = targetAmb;
    }
    #endregion
}

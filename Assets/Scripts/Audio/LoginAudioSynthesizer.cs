using System;
using UnityEngine;

namespace BattleSummoner.Audio
{
    /// <summary>
    /// LoginAudioSynthesizer procedurally synthesizes realistic sound effects for:
    /// 1. Persona 4 CRT TV power hum, static hiss, and liquid television plunge.
    /// 2. Sword Art Online "Link Start" laser charge, warp tunnel sonic rush, and login confirmation chime.
    /// </summary>
    public class LoginAudioSynthesizer : MonoBehaviour
    {
        public static LoginAudioSynthesizer Instance { get; private set; }

        private AudioSource sfxSource;
        private AudioSource ambientSource;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
        }

        /// <summary>
        /// Persona 4 CRT high-voltage 15kHz flyback hum and warm analog buzz.
        /// </summary>
        public void StartCRTHum()
        {
            int sampleRate = 44100;
            int length = sampleRate * 2; // 2 second loop
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                // 60Hz mains hum
                float hum60 = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.15f;
                float hum120 = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.08f;
                // TV static noise
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.04f;
                // High frequency CRT flyback whistle (approx 7.8kHz subharmonic of 15.7kHz)
                float flyback = Mathf.Sin(2f * Mathf.PI * 7850f * t) * 0.02f;

                samples[i] = hum60 + hum120 + noise + flyback;
            }

            AudioClip clip = AudioClip.Create("CRTHum", length, 1, sampleRate, false);
            clip.SetData(samples, 0);

            ambientSource.clip = clip;
            ambientSource.volume = 0.45f;
            ambientSource.Play();
        }

        public void StopCRTHum()
        {
            if (ambientSource.isPlaying) ambientSource.Stop();
        }

        /// <summary>
        /// Persona 4 liquid water plunge when touching and entering the TV screen.
        /// </summary>
        public void PlayWaterPlunge()
        {
            int sampleRate = 44100;
            float duration = 1.4f;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 2.8f);
                // Swept sine bubble frequency
                float f = Mathf.Lerp(480f, 90f, t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * f * t);
                // Secondary water ripple resonance
                float ripple = Mathf.Sin(2f * Mathf.PI * 340f * t + Mathf.Sin(t * 30f)) * 0.4f;
                // Low end whoosh
                float whoosh = (UnityEngine.Random.value * 2f - 1f) * (1f - t / duration) * 0.35f;

                samples[i] = (wave * 0.6f + ripple + whoosh) * env;
            }

            AudioClip clip = AudioClip.Create("WaterPlunge", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            sfxSource.PlayOneShot(clip, 0.85f);
        }

        /// <summary>
        /// Sword Art Online "Link Start!" rising laser sweep into radiant celestial chime.
        /// </summary>
        public void PlaySAOLinkStart()
        {
            int sampleRate = 44100;
            float duration = 2.2f;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float s = 0f;

                if (t < 0.65f)
                {
                    // Phase 1: High speed rising frequency laser charge
                    float progress = t / 0.65f;
                    float freq = Mathf.Lerp(220f, 1760f, progress * progress);
                    s = Mathf.Sin(2f * Mathf.PI * freq * t) * (progress * 0.7f);
                    // Add cyber flanging
                    s += Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * (progress * 0.3f);
                }
                else
                {
                    // Phase 2: Radiant Celestial Chime (SAO login chord C-E-G-B-D major 9th)
                    float tChime = t - 0.65f;
                    float env = Mathf.Exp(-tChime * 1.8f);

                    float f1 = Mathf.Sin(2f * Mathf.PI * 523.25f * tChime); // C5
                    float f2 = Mathf.Sin(2f * Mathf.PI * 659.25f * tChime); // E5
                    float f3 = Mathf.Sin(2f * Mathf.PI * 783.99f * tChime); // G5
                    float f4 = Mathf.Sin(2f * Mathf.PI * 987.77f * tChime); // B5
                    float f5 = Mathf.Sin(2f * Mathf.PI * 1174.66f * tChime); // D6
                    float shimmer = Mathf.Sin(2f * Mathf.PI * 2349.32f * tChime) * 0.25f; // D7 shimmer

                    s = (f1 * 0.25f + f2 * 0.22f + f3 * 0.20f + f4 * 0.18f + f5 * 0.15f + shimmer) * env;
                }

                samples[i] = Mathf.Clamp(s, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("LinkStartChime", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            sfxSource.PlayOneShot(clip, 0.95f);
        }

        /// <summary>
        /// Hyperspace light streak sonic whoosh.
        /// </summary>
        public void PlayWarpWhoosh()
        {
            int sampleRate = 44100;
            float duration = 1.8f;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float env = Mathf.Sin(progress * Mathf.PI);
                float noise = (UnityEngine.Random.value * 2f - 1f);
                // Swept bandpass low drone
                float drone = Mathf.Sin(2f * Mathf.PI * (80f + progress * 240f) * t);

                samples[i] = (noise * 0.4f + drone * 0.6f) * env;
            }

            AudioClip clip = AudioClip.Create("WarpWhoosh", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            sfxSource.PlayOneShot(clip, 0.70f);
        }

        /// <summary>
        /// Holographic button click sound.
        /// </summary>
        public void PlayUIClick()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 3600f * t) * 0.3f;
                samples[i] = wave * env;
            }

            AudioClip clip = AudioClip.Create("UIClick", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            sfxSource.PlayOneShot(clip, 0.5f);
        }

        /// <summary>
        /// Final divine world induction fanfare.
        /// </summary>
        public void PlayInductionFanfare()
        {
            int sampleRate = 44100;
            float duration = 2.5f;
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 1.2f);
                // Grand orchestra synth chord
                float f1 = Mathf.Sin(2f * Mathf.PI * 261.63f * t); // C4
                float f2 = Mathf.Sin(2f * Mathf.PI * 329.63f * t); // E4
                float f3 = Mathf.Sin(2f * Mathf.PI * 392.00f * t); // G4
                float f4 = Mathf.Sin(2f * Mathf.PI * 523.25f * t); // C5
                float f5 = Mathf.Sin(2f * Mathf.PI * 659.25f * t); // E5

                samples[i] = (f1 + f2 + f3 + f4 + f5) * 0.2f * env;
            }

            AudioClip clip = AudioClip.Create("InductionFanfare", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            sfxSource.PlayOneShot(clip, 1.0f);
        }
    }
}

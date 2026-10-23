using System.Collections;
using UnityEngine;

namespace BattleSummoner.Audio
{
    /// <summary>
    /// OverworldTownAmbiance provides peaceful, organic procedural background audio for the Solas Overworld:
    /// - Soft filtered wind murmur.
    /// - Delicate harmonic wind chime accents.
    /// - Continuous soothing ambient loop.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class OverworldTownAmbiance : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitAmbianceHook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (scene.name == "OverworldMap")
                {
                    if (UnityEngine.Object.FindAnyObjectByType<OverworldTownAmbiance>() == null)
                    {
                        new GameObject("OverworldTownAmbiance", typeof(AudioSource), typeof(OverworldTownAmbiance));
                    }
                }
            };
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "OverworldMap")
            {
                if (UnityEngine.Object.FindAnyObjectByType<OverworldTownAmbiance>() == null)
                {
                    new GameObject("OverworldTownAmbiance", typeof(AudioSource), typeof(OverworldTownAmbiance));
                }
            }
        }

        [Header("Audio Settings")]
        [SerializeField] private float masterVolume = 0.40f;
        [SerializeField] private bool playOnAwake = true;

        private AudioSource audioSource;
        private Coroutine chimeRoutine;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D ambient background
            audioSource.loop = true;
            audioSource.volume = masterVolume;

            if (audioSource.clip == null)
            {
                audioSource.clip = CreateBreezeLoop();
            }
        }

        private void Start()
        {
            if (playOnAwake)
            {
                audioSource.Play();
                chimeRoutine = StartCoroutine(WindChimeRoutine());
            }
        }

        private void OnDisable()
        {
            if (chimeRoutine != null)
            {
                StopCoroutine(chimeRoutine);
                chimeRoutine = null;
            }
        }

        private AudioClip CreateBreezeLoop()
        {
            int sampleRate = 22050;
            float duration = 6.0f;
            int numSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            float prevSample = 0f;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                // Soft pink/brown filtered noise
                float white = (Random.value * 2f - 1f) * 0.08f;
                prevSample = (prevSample * 0.96f) + (white * 0.04f);

                // Gentle undulating swell
                float swell = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / duration);
                float subHarmonic = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.015f;

                samples[i] = (prevSample + subHarmonic) * swell;
            }

            AudioClip clip = AudioClip.Create("overworld_breeze_ambiance", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private IEnumerator WindChimeRoutine()
        {
            float[] chimeFreqs = new float[] { 659.25f, 830.61f, 987.77f, 1318.51f, 1661.22f }; // E5, G#5, B5, E6, G#6

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(4.5f, 9.0f));

                float freq = chimeFreqs[Random.Range(0, chimeFreqs.Length)];
                PlayChimeTone(freq);
            }
        }

        private void PlayChimeTone(float freq)
        {
            int sampleRate = 22050;
            float duration = 1.6f;
            int numSamples = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-3.2f * t);
                float fundamental = Mathf.Sin(2f * Mathf.PI * freq * t);
                float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 2.756f * t) * 0.25f;
                samples[i] = (fundamental + harmonic) * decay * 0.14f;
            }

            AudioClip chime = AudioClip.Create("chime_tone", numSamples, 1, sampleRate, false);
            chime.SetData(samples, 0);

            var tempObj = new GameObject("TempChimeAudio", typeof(AudioSource));
            tempObj.transform.position = transform.position;
            var src = tempObj.GetComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.volume = masterVolume * 0.65f;
            src.PlayOneShot(chime);
            Destroy(tempObj, duration + 0.2f);
        }
    }
}

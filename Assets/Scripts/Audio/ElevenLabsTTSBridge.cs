using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ElevenLabsTTSBridge streams dialogue text into ElevenLabs flash API (eleven_flash_v2_5)
/// with chunked HTTP streaming for zero-latency NPC voiceover in portrait mobile ARPG.
/// Coordinates with BattleSummonerAudioMaster to duck SFX channels by -28% during speech.
/// </summary>
public class ElevenLabsTTSBridge : MonoBehaviour
{
    public static ElevenLabsTTSBridge Instance { get; private set; }

    [Header("API Configuration")]
    [Tooltip("Leave empty in project files. Key is securely resolved at runtime from ELEVENLABS_API_KEY environment variable or external user profile secret.")]
    [SerializeField] private string apiKey = "";
    [SerializeField] private string modelId = "eleven_flash_v2_5";
    [SerializeField] private string defaultVoiceId = "21m00Tcm4TlvDq8ikWAM"; // Rachel / Margo style

    [Header("Character Voice Mappings")]
    [SerializeField] private string margoVoiceId = "qFjcP4hHD9WIdODvuJOJ";    // Jenna (Warm, Soft, Sultry / Goth Companion)
    [SerializeField] private string barnabyVoiceId = "VR6AewLTigWG4xSOukaG";  // Booming deep anvil tone
    [SerializeField] private string zephyrVoiceId = "pNInz6obpgDQGcFmaJgB";   // Swift agile whisper

    [Header("Audio Output")]
    [SerializeField] private AudioSource speechAudioSource;

    public event Action OnVoiceoverPlaybackStarted;
    public event Action OnVoiceoverPlaybackCompleted;

    private static string runtimeKeyOverride = null;
    private Coroutine activeStreamRoutine;

    /// <summary>
    /// Sets an in-memory API key for the current game session without saving it into any project file.
    /// </summary>
    public static void SetRuntimeApiKey(string key)
    {
        runtimeKeyOverride = key?.Trim();
    }

    /// <summary>
    /// Securely resolves the ElevenLabs API key at runtime from memory, environment variables,
    /// or user profile directory, completely omitting any secret from project repository files.
    /// </summary>
    public string GetEffectiveApiKey()
    {
        // 1. In-memory runtime override (never persisted to project files)
        if (!string.IsNullOrEmpty(runtimeKeyOverride))
        {
            return runtimeKeyOverride;
        }

        // 2. Local serialized field if set (never committed with real secrets)
        if (!string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_ELEVENLABS_KEY")
        {
            return apiKey.Trim();
        }

        // 3. System / User environment variables (stored outside repository in Windows registry)
        string envKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
        if (!string.IsNullOrEmpty(envKey)) return envKey.Trim();

        envKey = Environment.GetEnvironmentVariable("XI_API_KEY");
        if (!string.IsNullOrEmpty(envKey)) return envKey.Trim();

        // 4. External user profile directory secret (~/.elevenlabs_key outside project root)
        try
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                string externalKeyFile = System.IO.Path.Combine(userProfile, ".elevenlabs_key");
                if (System.IO.File.Exists(externalKeyFile))
                {
                    string fileKey = System.IO.File.ReadAllText(externalKeyFile).Trim();
                    if (!string.IsNullOrEmpty(fileKey)) return fileKey;
                }
            }
        }
        catch { }

        return "";
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
            if (speechAudioSource == null)
            {
                speechAudioSource = GetComponent<AudioSource>();
                if (speechAudioSource == null)
                {
                    speechAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            speechAudioSource.spatialBlend = 0f;
            speechAudioSource.playOnAwake = false;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public string GetVoiceIdForNPC(string npcId)
    {
        switch (npcId?.ToLowerInvariant())
        {
            case "npc_margo": return margoVoiceId;
            case "npc_barnaby": return barnabyVoiceId;
            case "npc_zephyr": return zephyrVoiceId;
            default: return defaultVoiceId;
        }
    }

    /// <summary>
    /// Streams voiceover for NPC dialogue text.
    /// Strips SFX tags before sending to TTS engine.
    /// </summary>
    public void SpeakDialogue(string dialogueText, string npcId = "npc_margo")
    {
        if (activeStreamRoutine != null)
        {
            StopCoroutine(activeStreamRoutine);
        }

        string voiceId = GetVoiceIdForNPC(npcId);

        // Strip [SFX: ...] tags for clean text to speech synthesis
        string cleanText = BattleSummonerAudioMaster.Instance != null 
            ? BattleSummonerAudioMaster.Instance.ProcessAndScheduleSFXTags(dialogueText)
            : dialogueText;

        Debug.Log($"[ElevenLabsTTSBridge] Voicing dialogue for {npcId} (Voice: {voiceId}): \"{cleanText}\"");

        string effectiveKey = GetEffectiveApiKey();
        if (string.IsNullOrEmpty(effectiveKey))
        {
            Debug.LogWarning("[ElevenLabsTTSBridge] No ElevenLabs API key found in ELEVENLABS_API_KEY environment variable. Using procedural voice synthesis fallback.");
            activeStreamRoutine = StartCoroutine(ProceduralVoiceoverFallbackRoutine(cleanText, npcId));
            return;
        }

        activeStreamRoutine = StartCoroutine(StreamTTSChunkedRoutine(cleanText, voiceId, effectiveKey));
    }

    private IEnumerator StreamTTSChunkedRoutine(string text, string voiceId, string effectiveKey)
    {
        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}/stream?output_format=mp3_44100_128";

        string jsonBody = "{\"text\":\"" + EscapeJson(text) + "\",\"model_id\":\"" + modelId + "\",\"voice_settings\":{\"stability\":0.45,\"similarity_boost\":0.80}}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("xi-api-key", effectiveKey);

            // Signal start of stream & trigger SFX ducking
            NotifySpeechStarted();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip != null)
                {
                    speechAudioSource.clip = clip;
                    speechAudioSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }
            }
            else
            {
                Debug.LogWarning($"[ElevenLabsTTSBridge] Online stream failed: {req.error}. Switching to local synthesis.");
                yield return ProceduralVoiceoverFallbackRoutine(text, "fallback");
            }

            NotifySpeechEnded();
        }
    }

    /// <summary>
    /// Procedural vocal synthesis fallback: generates personality-coded formant phoneme bursts
    /// so voiceover playback and -28% SFX ducking function 100% offline without requiring internet.
    /// </summary>
    private IEnumerator ProceduralVoiceoverFallbackRoutine(string cleanText, string npcId)
    {
        NotifySpeechStarted();

        int wordCount = Mathf.Clamp(cleanText.Split(' ').Length, 3, 25);
        float basePitch = 1.0f;
        float formantFreq = 340f;

        if (npcId == "npc_margo")
        {
            basePitch = 1.15f;
            formantFreq = 420f;
        }
        else if (npcId == "npc_barnaby")
        {
            basePitch = 0.72f;
            formantFreq = 180f;
        }
        else if (npcId == "npc_zephyr")
        {
            basePitch = 1.35f;
            formantFreq = 540f;
        }

        int sampleRate = 22050;
        float totalDuration = wordCount * 0.16f;
        int numSamples = Mathf.FloorToInt(sampleRate * totalDuration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float wordPhase = (t / 0.16f) % 1.0f;
            float envelope = Mathf.Sin(wordPhase * Mathf.PI);

            float voiceCarrier = Mathf.Sin(2f * Mathf.PI * (formantFreq * basePitch) * t);
            float harmonic = Mathf.Sin(4f * Mathf.PI * (formantFreq * basePitch) * t) * 0.4f;
            float breath = ((float)new System.Random(i).NextDouble() * 2f - 1f) * 0.08f;

            samples[i] = (voiceCarrier + harmonic + breath) * envelope * 0.45f;
        }

        AudioClip clip = AudioClip.Create("procedural_npc_utterance", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);

        speechAudioSource.pitch = 1.0f;
        speechAudioSource.PlayOneShot(clip, 0.95f);
        yield return new WaitForSeconds(totalDuration);

        NotifySpeechEnded();
    }

    private void NotifySpeechStarted()
    {
        OnVoiceoverPlaybackStarted?.Invoke();
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.NotifyVoiceoverStarted();
        }
    }

    private void NotifySpeechEnded()
    {
        OnVoiceoverPlaybackCompleted?.Invoke();
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.NotifyVoiceoverEnded();
        }
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}

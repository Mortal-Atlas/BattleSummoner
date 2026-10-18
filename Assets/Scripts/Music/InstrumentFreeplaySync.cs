using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight RPC synchronization payload for multiplayer bardic jamming.
/// </summary>
[System.Serializable]
public struct NoteRPCPayload
{
    public string peerId;
    public string noteName;
    public float frequency;
    public float velocity;
    public Vector3 worldPosition;
    public double timestamp;
}

/// <summary>
/// InstrumentFreeplaySync is the multiplayer broadcasting layer for instrument magic.
/// Every individual note tapped (e.g. [Note: C4, Velocity: 0.8]) is immediately sent
/// as a lightweight RPC to all nearby players in the instance for real-time collaborative
/// jamming and serenading.
/// </summary>
public class InstrumentFreeplaySync : MonoBehaviour
{
    public static InstrumentFreeplaySync Instance { get; private set; }

    [Header("Network Simulation & Proximity")]
    [SerializeField] private float broadcastRadius = 24f;
    [SerializeField] private bool simulatePeerJamming = true;

    public static event Action<NoteRPCPayload> OnLocalNoteBroadcasted;
    public static event Action<NoteRPCPayload> OnRemoteNoteReceived;
    public event Action<string, float> OnNotePlayed;

    private readonly List<NoteRPCPayload> recentJamLog = new List<NoteRPCPayload>();

    public void PlayNote(string noteName, float frequency)
    {
        Debug.Log($"[Instrument Freeplay] Note broadcast: {noteName} ({frequency}Hz)");
        OnNotePlayed?.Invoke(noteName, frequency);
        BroadcastNoteTapped(noteName, frequency, 1.0f);

        // Map 4D Mass and 10D Frequency to Android vibration/haptic amplitude
        float mass = 12f;
        if (RuneShapeCastingEngine.Instance != null && RuneShapeCastingEngine.Instance.ActiveSpellArtifact != null)
        {
            mass = RuneShapeCastingEngine.Instance.ActiveSpellArtifact.inertialMass_4D;
        }
        TriggerGauntletHapticFeedback(mass, frequency);
    }

    /// <summary>
    /// Simulates physical back-EMF torque on the player's mobile phone mapped from 4D Mass and 10D Frequency.
    /// </summary>
    public static void TriggerGauntletHapticFeedback(float inertialMass_4D = 15f, float carrierFrequency_10D = 60f)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            int amplitude = Mathf.Clamp((int)(inertialMass_4D * 2.5f + (carrierFrequency_10D * 0.35f)), 25, 255);
            int durationMs = Mathf.Clamp((int)(inertialMass_4D * 3.5f), 30, 280);

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)durationMs, amplitude);
                        vibrator.Call("vibrate", effect);
                        return;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GAUNTLET HAPTICS] Android vibration fallback: {ex.Message}");
        }
        Handheld.Vibrate();
#else
        // Editor / Non-Android simulation: back-EMF torque impulse
#endif
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
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Broadcasts an individual note tapped by the local player.
    /// </summary>
    public void BroadcastNoteTapped(string noteName, float frequency, float velocity = 1.0f)
    {
        Vector3 playerPos = Vector3.zero;
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerPos = playerObj.transform.position;
        }

        NoteRPCPayload payload = new NoteRPCPayload
        {
            peerId = "Player_Local",
            noteName = noteName,
            frequency = frequency,
            velocity = Mathf.Clamp01(velocity),
            worldPosition = playerPos,
            timestamp = Time.timeAsDouble
        };

        recentJamLog.Add(payload);
        if (recentJamLog.Count > 100) recentJamLog.RemoveAt(0);

        Debug.Log($"[BARDIC RPC] Transmitted note: [Note: {noteName} ({frequency:F1}Hz), Velocity: {velocity:F2}] to Spire instance.");

        OnLocalNoteBroadcasted?.Invoke(payload);

        // Spawn lightweight floating 3D music note symbol over player
        SpawnFloatingNoteVisual(playerPos, noteName);

        // In multiplayer / simulated town hub: Echo back harmonizing peer response if enabled
        if (simulatePeerJamming && UnityEngine.Random.value < 0.20f)
        {
            Invoke(nameof(SimulatePeerHarmonizingNote), 0.28f);
        }
    }

    /// <summary>
    /// Invoked when a remote player in the instance taps a note.
    /// </summary>
    public void ReceiveRemoteNoteRPC(NoteRPCPayload payload)
    {
        // Play note with distance attenuation
        if (BattleSummonerAudioMaster.Instance != null)
        {
            float dist = Vector3.Distance(Camera.main != null ? Camera.main.transform.position : Vector3.zero, payload.worldPosition);
            if (dist <= broadcastRadius)
            {
                float attenuation = Mathf.Clamp01(1f - (dist / broadcastRadius));
                BattleSummonerAudioMaster.Instance.PlayKalimbaNote(payload.noteName, payload.frequency, payload.velocity * attenuation);
            }
        }

        SpawnFloatingNoteVisual(payload.worldPosition, payload.noteName);
        OnRemoteNoteReceived?.Invoke(payload);
    }

    private void SimulatePeerHarmonizingNote()
    {
        string[] harmonizingNotes = { "E4", "G4", "B4", "D5", "G5" };
        float[] freqs = { 329.63f, 392.00f, 493.88f, 587.33f, 783.99f };
        int idx = UnityEngine.Random.Range(0, harmonizingNotes.Length);

        Vector3 peerPos = new Vector3(-4f, 1f, -12f); // Near Margo's base camp
        NoteRPCPayload peerPayload = new NoteRPCPayload
        {
            peerId = "Peer_Bard_Lyra",
            noteName = harmonizingNotes[idx],
            frequency = freqs[idx],
            velocity = 0.75f,
            worldPosition = peerPos,
            timestamp = Time.timeAsDouble
        };

        ReceiveRemoteNoteRPC(peerPayload);
    }

    private void SpawnFloatingNoteVisual(Vector3 position, string noteLabel)
    {
        // Simple procedural visual dot / floating note marker
        Vector3 spawnPos = position + Vector3.up * 2.2f + UnityEngine.Random.insideUnitSphere * 0.35f;
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = $"NoteGlow_{noteLabel}";
        marker.transform.position = spawnPos;
        marker.transform.localScale = Vector3.one * 0.18f;

        Collider col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = marker.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.3f, 0.85f, 1f, 0.9f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.2f, 0.7f, 1f) * 2f);
            rend.sharedMaterial = mat;
        }

        Destroy(marker, 0.85f);
    }
}

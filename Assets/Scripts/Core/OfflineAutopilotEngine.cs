using System;
using UnityEngine;
using BattleSummoner.Core;

/// <summary>
/// OfflineAutopilotEngine runs autonomous background expedition and rest loops.
/// Validates elapsed offline time against hardware monotonic tick counters via
/// OfflineFirstContinuityManager to prevent Airplane-Mode clock skipping, capping
/// passive offline rewards at 12.0 hours and routing tampering to The Phantom Spire.
/// </summary>
public class OfflineAutopilotEngine : MonoBehaviour
{
    private const string SNAPSHOT_UTC_KEY = "PLAYER_OFFLINE_SNAPSHOT_UTC";
    private const string SNAPSHOT_MONO_KEY = "PLAYER_OFFLINE_SNAPSHOT_MONO";

    public static OfflineAutopilotEngine Instance { get; private set; }

    [Header("Expedition Reward Tuning")]
    [SerializeField] private float expPerOfflineHour = 120f;
    [SerializeField] private int manaCoresPerOfflineHour = 45;

    public event Action<float, int, int> OnOfflineExpeditionProcessed; // hours, exp, cores

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

    private void Start()
    {
        ProcessOfflineProgression();
    }

    private void OnApplicationQuit() => SerializeAndSaveState();

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SerializeAndSaveState();
        else ProcessOfflineProgression();
    }

    private void SerializeAndSaveState()
    {
        long utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long monoNow = OfflineFirstContinuityManager.GetMonotonicSeconds();

        PlayerPrefs.SetString(SNAPSHOT_UTC_KEY, utcNow.ToString());
        PlayerPrefs.SetString(SNAPSHOT_MONO_KEY, monoNow.ToString());
        PlayerPrefs.Save();
        Debug.Log($"🤖💤 [Autopilot] Snapshot saved at UTC={utcNow}, Monotonic={monoNow}s. Autonomous background loops active.");
    }

    public void ProcessOfflineProgression()
    {
        if (!PlayerPrefs.HasKey(SNAPSHOT_UTC_KEY)) return;

        long savedUtc = 0;
        long savedMono = 0;
        long.TryParse(PlayerPrefs.GetString(SNAPSHOT_UTC_KEY, "0"), out savedUtc);
        long.TryParse(PlayerPrefs.GetString(SNAPSHOT_MONO_KEY, "0"), out savedMono);

        float verifiedHours = 0f;
        if (OfflineFirstContinuityManager.Instance != null)
        {
            verifiedHours = OfflineFirstContinuityManager.Instance.CalculateVerifiedOfflineHours(savedUtc, savedMono);
        }
        else
        {
            long elapsedSec = Math.Max(0L, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - savedUtc);
            verifiedHours = Mathf.Clamp(elapsedSec / 3600f, 0f, 12.0f);
        }

        // Check for aggressive forward clock tampering (UTC jumped forward while hardware ticks did not)
        long currentUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long currentMono = OfflineFirstContinuityManager.GetMonotonicSeconds();
        long utcDelta = currentUtc - savedUtc;
        long monoDelta = currentMono - savedMono;

        if (savedMono > 0 && currentMono >= savedMono && utcDelta > monoDelta + 3600L)
        {
            Debug.LogWarning("⚠️ [Phantom Spire Quarantine] Clock-skipping manipulation detected in Airplane Mode! Player flagged for Tribunal review.");
            TribunalAppealController.FlagPlayerForQuarantine("Clock-Skipping Airplane Mode Offline Exploit");
            if (OfflineFirstContinuityManager.Instance != null)
            {
                OfflineFirstContinuityManager.Instance.isFlaggedForPhantomSpireQuarantine = true;
            }
        }

        if (verifiedHours > 0.05f)
        {
            int earnedExp = Mathf.RoundToInt(verifiedHours * expPerOfflineHour);
            int earnedCores = Mathf.RoundToInt(verifiedHours * manaCoresPerOfflineHour);

            Debug.Log($"🤖💤 [Autopilot] Welcome back! Character rested for {verifiedHours:F2}h offline. Earned +{earnedExp} EXP and +{earnedCores} Mana Cores.");

            // Award EXP and Cores locally
            var playerProg = FindAnyObjectByType<PlayerProgression>();
            if (playerProg != null)
            {
                playerProg.AddExperience(earnedExp, 1);
            }

            // Enqueue verified state delta for opportunistic sync
            if (OfflineFirstContinuityManager.Instance != null)
            {
                string payload = $"{{\"verifiedHours\":{verifiedHours:F2},\"earnedExp\":{earnedExp},\"earnedCores\":{earnedCores}}}";
                OfflineFirstContinuityManager.Instance.EnqueueOfflineAction("ExpeditionReturn", payload);
            }

            OnOfflineExpeditionProcessed?.Invoke(verifiedHours, earnedExp, earnedCores);
        }

        PlayerPrefs.DeleteKey(SNAPSHOT_UTC_KEY);
        PlayerPrefs.DeleteKey(SNAPSHOT_MONO_KEY);
        PlayerPrefs.Save();
    }
}

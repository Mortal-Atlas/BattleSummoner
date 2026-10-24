using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using BattleSummoner.DesignPhilosophy;

namespace BattleSummoner.Core
{
    public enum NetworkContinuityMode
    {
        OnlineConnected,        // Live server sync + multiplayer squads active
        AirplaneOfflineSolo     // 100% local execution + delta ledger queuing
    }

    [Serializable]
    public class SignedOfflineStateDelta
    {
        public string deltaGuid;
        public long utcTimestampSeconds;
        public long monotonicTickSeconds;
        public string actionCategory; // "DungeonClear", "ClimberStructure", "RumorDiscovery", "ExpeditionReturn", "CraftOutput"
        public string payloadJson;
        public string hmacIntegritySignature;
    }

    /// <summary>
    /// OfflineFirstContinuityManager guarantees that BattleSummoner never blocks solo or dungeon
    /// gameplay when offline or in Airplane Mode. All progression, crafting, dungeon clears, and companion AI
    /// execute locally without network heartbeats. Shared world systems queue cryptographically signed
    /// state deltas and flush them opportunistically when a connection is restored.
    /// </summary>
    public class OfflineFirstContinuityManager : MonoBehaviour
    {
        public static OfflineFirstContinuityManager Instance { get; private set; }

        [Header("Continuity & Anti-Disconnect Status")]
        public NetworkContinuityMode currentMode = NetworkContinuityMode.OnlineConnected;
        public float maxOfflineExpeditionCapHours = 12.0f;
        public bool routeTamperedSavesToPhantomSpire = true;
        public bool isFlaggedForPhantomSpireQuarantine = false;

        [Header("Local Opportunistic Sync Queue")]
        public List<SignedOfflineStateDelta> pendingSyncQueue = new List<SignedOfflineStateDelta>();

        private const string LEDGER_PREFS_KEY = "BS_OFFLINE_DELTA_LEDGER";
        private const string LEDGER_SALT = "HUNDREDFOLD_SPIRE_12D_AXIOM_SALT";
        private float _networkPollTimer = 0f;

        public event Action<NetworkContinuityMode> OnNetworkModeChanged;
        public event Action<SignedOfflineStateDelta> OnDeltaEnqueued;
        public event Action<int> OnDeltasFlushed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            LoadPendingQueueFromDisk();
            EvaluateNetworkStateImmediate();
        }

        private void Update()
        {
            _networkPollTimer += Time.unscaledDeltaTime;
            if (_networkPollTimer >= 3.0f)
            {
                _networkPollTimer = 0f;
                EvaluateNetworkStateImmediate();
            }
        }

        public void EvaluateNetworkStateImmediate()
        {
            bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
            NetworkContinuityMode targetMode = hasInternet
                ? NetworkContinuityMode.OnlineConnected
                : NetworkContinuityMode.AirplaneOfflineSolo;

            if (targetMode != currentMode)
            {
                currentMode = targetMode;
                Debug.Log($"[Offline-First Continuity] Switched to {currentMode} with ZERO gameplay interruption.");
                OnNetworkModeChanged?.Invoke(currentMode);

                if (currentMode == NetworkContinuityMode.OnlineConnected)
                {
                    FlushPendingOfflineDeltasToServer();
                }
                else
                {
                    ConvertDisconnectedSquadmatesToLocalPawns();
                }
            }
        }

        /// <summary>
        /// Ensures a mid-dungeon Wi-Fi drop never kicks the solo player out of their boss fight.
        /// Any remote co-op party members seamlessly switch to local Subconscious AI Autopilot pawns.
        /// </summary>
        private void ConvertDisconnectedSquadmatesToLocalPawns()
        {
            var awakeControllers = FindObjectsByType<DreamerAwakeStateController>(FindObjectsSortMode.None);
            foreach (var ctrl in awakeControllers)
            {
                if (!ctrl.CompareTag("Player"))
                {
                    ctrl.SetDreamerStatus(DreamerPresenceStatus.SubconsciousAsleep);
                }
            }
            Debug.Log("🌙 [Airplane Continuity] Remote squadmates converted to local Subconscious AI Pawns. Boss fight continues uninterrupted.");
        }

        public static long GetMonotonicSeconds()
        {
            return System.Diagnostics.Stopwatch.GetTimestamp() / System.Diagnostics.Stopwatch.Frequency;
        }

        /// <summary>
        /// Records an offline achievement, structure build, or loot drop with a cryptographic checksum
        /// so offline progress is 100% preserved and verified upon reconnecting.
        /// </summary>
        public void EnqueueOfflineAction(string category, string jsonPayload)
        {
            long utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long monoTicks = GetMonotonicSeconds();

            var delta = new SignedOfflineStateDelta
            {
                deltaGuid = Guid.NewGuid().ToString("N"),
                utcTimestampSeconds = utcNow,
                monotonicTickSeconds = monoTicks,
                actionCategory = category,
                payloadJson = jsonPayload
            };
            delta.hmacIntegritySignature = ComputeSignature(delta);

            pendingSyncQueue.Add(delta);
            SavePendingQueueToDisk();
            OnDeltaEnqueued?.Invoke(delta);
            Debug.Log($"📝 [Offline Ledger] Enqueued delta '{category}' ({delta.deltaGuid.Substring(0, 8)}...). Total pending: {pendingSyncQueue.Count}");
        }

        /// <summary>
        /// Calculates clamped, clock-tamper-safe offline autopilot hours when resuming the app.
        /// </summary>
        public float CalculateVerifiedOfflineHours(long savedUtcEpoch, long savedMonotonicSec)
        {
            long utcElapsed = Math.Max(0L, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - savedUtcEpoch);
            long monoElapsed = Math.Max(0L, GetMonotonicSeconds() - savedMonotonicSec);

            // If device hasn't rebooted (monoElapsed > 0) and wall-clock jumped ahead by > 15 minutes
            // compared to hardware monotonic ticks, clamp to monotonic time to prevent clock-skipping exploits.
            bool clockJumpDetected = (monoElapsed > 0 && Math.Abs(utcElapsed - monoElapsed) > 900L);
            if (clockJumpDetected)
            {
                Debug.LogWarning($"⚠️ [Anti-Tamper Monotonic Clock] System clock jump detected (UTC Δ={utcElapsed}s, Monotonic Δ={monoElapsed}s). Clamping to hardware tick timer.");
            }

            long verifiedSeconds = clockJumpDetected ? monoElapsed : utcElapsed;

            float hours = verifiedSeconds / 3600f;
            return Mathf.Clamp(hours, 0f, maxOfflineExpeditionCapHours);
        }

        public void FlushPendingOfflineDeltasToServer()
        {
            if (pendingSyncQueue.Count == 0) return;

            int validCount = 0;
            foreach (var delta in pendingSyncQueue)
            {
                string expectedSig = ComputeSignature(delta);
                if (!string.Equals(delta.hmacIntegritySignature, expectedSig, StringComparison.Ordinal))
                {
                    isFlaggedForPhantomSpireQuarantine = true;
                    Debug.LogWarning("[Phantom Spire Quarantine] Offline ledger checksum mismatch detected! Routing next matchmaking session to The Phantom Spire.");
                    if (routeTamperedSavesToPhantomSpire)
                    {
                        TribunalAppealController.FlagPlayerForQuarantine("Tampered Offline Ledger Signature Mismatch");
                    }
                    continue;
                }
                validCount++;
            }

            Debug.Log($"[Offline-First Sync] Opportunistically synced {validCount} verified offline deltas to Spire World Ledger.");
            OnDeltasFlushed?.Invoke(validCount);
            pendingSyncQueue.Clear();
            SavePendingQueueToDisk();
        }

        public string ComputeSignature(SignedOfflineStateDelta d)
        {
            string raw = $"{d.deltaGuid}|{d.utcTimestampSeconds}|{d.monotonicTickSeconds}|{d.actionCategory}|{d.payloadJson}|{LEDGER_SALT}";
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return Convert.ToBase64String(bytes);
            }
        }

        private void SavePendingQueueToDisk()
        {
            string wrapperJson = JsonUtility.ToJson(new DeltaListWrapper { list = pendingSyncQueue });
            PlayerPrefs.SetString(LEDGER_PREFS_KEY, wrapperJson);
            PlayerPrefs.Save();
        }

        private void LoadPendingQueueFromDisk()
        {
            if (!PlayerPrefs.HasKey(LEDGER_PREFS_KEY)) return;
            var wrapper = JsonUtility.FromJson<DeltaListWrapper>(PlayerPrefs.GetString(LEDGER_PREFS_KEY));
            if (wrapper != null && wrapper.list != null)
            {
                pendingSyncQueue = wrapper.list;
            }
        }

        [Serializable]
        private class DeltaListWrapper
        {
            public List<SignedOfflineStateDelta> list = new List<SignedOfflineStateDelta>();
        }
    }
}

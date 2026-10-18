using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Core
{
    [Serializable]
    public class GuardianContractData
    {
        public string avatarName;
        public string guardianPrintedName;
        public List<Vector2> signatureStrokePoints = new List<Vector2>();
        public long bindingTimestampUnix;

        public string ToJson() => JsonUtility.ToJson(this, true);

        public static GuardianContractData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<GuardianContractData>(json);
        }
    }

    /// <summary>
    /// Character Creation: The Guardian & Guide Binding Signature.
    /// Because player avatars exist persistently on the world register, character creation
    /// is framed as a binding pact. The finger-drawn signature coordinates are serialized,
    /// stored persistently, and stamped onto forged weapons and signed guild contracts.
    /// </summary>
    public class GuardianSignatureBinder : MonoBehaviour
    {
        public static GuardianSignatureBinder Instance { get; private set; }

        private const string PREFS_KEY_SIGNATURE = "GuardianSignatureData";

        [Header("Visual Line Renderer (Parchment Ink)")]
        [SerializeField] private LineRenderer signatureLineRenderer;
        [SerializeField] private Camera signatureUiCamera;

        [Header("Active Guardian Contract")]
        public GuardianContractData activeContract = new GuardianContractData();
        private bool isSigning = false;

        public event Action<GuardianContractData> OnAvatarBoundToWorld;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSavedContract();
        }

        public void LoadSavedContract()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY_SIGNATURE))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY_SIGNATURE);
                var loaded = GuardianContractData.FromJson(json);
                if (loaded != null && !string.IsNullOrEmpty(loaded.avatarName))
                {
                    activeContract = loaded;
                    Debug.Log($"[Guardian Pact] Loaded persistent binding pact for avatar '{activeContract.avatarName}' bound to Guardian '{activeContract.guardianPrintedName}'.");
                }
            }
        }

        public void BeginSignatureStroke()
        {
            isSigning = true;
        }

        public void RecordSignaturePoint(Vector2 screenPos, Camera uiCam = null)
        {
            if (!isSigning) return;

            Camera cam = uiCam != null ? uiCam : (signatureUiCamera != null ? signatureUiCamera : Camera.main);
            Vector3 worldPt;

            if (cam != null)
            {
                worldPt = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 5f));
            }
            else
            {
                worldPt = new Vector3(screenPos.x / Screen.width, screenPos.y / Screen.height, 0f);
            }

            activeContract.signatureStrokePoints.Add(new Vector2(worldPt.x, worldPt.y));

            if (signatureLineRenderer != null)
            {
                signatureLineRenderer.positionCount = activeContract.signatureStrokePoints.Count;
                signatureLineRenderer.SetPosition(activeContract.signatureStrokePoints.Count - 1, worldPt);
            }
        }

        public void EndSignatureStroke()
        {
            isSigning = false;
        }

        public void ClearSignature()
        {
            isSigning = false;
            activeContract.signatureStrokePoints.Clear();
            if (signatureLineRenderer != null)
            {
                signatureLineRenderer.positionCount = 0;
            }
        }

        public bool CompleteGuardianPact(string assignedAvatarName, string guardianName)
        {
            if (activeContract.signatureStrokePoints.Count < 8)
            {
                Debug.LogWarning("[Guardian Pact] Signature too short! Please sign your name with finger to bind your avatar.");
                return false;
            }

            isSigning = false;
            activeContract.avatarName = assignedAvatarName;
            activeContract.guardianPrintedName = guardianName;
            activeContract.bindingTimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string serializedJson = activeContract.ToJson();
            PlayerPrefs.SetString(PREFS_KEY_SIGNATURE, serializedJson);
            PlayerPrefs.Save();

            // Audio & HUD Broadcast Confirmation
            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast(
                    $"[Guardian Pact] Avatar '{assignedAvatarName}' is now eternally bound to Guardian '{guardianName}'!", false);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.95f);
            }

            OnAvatarBoundToWorld?.Invoke(activeContract);

            if (CoreVerticalSliceSandboxController.Instance != null)
            {
                CoreVerticalSliceSandboxController.Instance.CompleteCreationAndSpawnInChurch(activeContract);
            }

            Debug.Log($"[Guardian Pact] {assignedAvatarName} is now bound to Guardian '{guardianName}' with {activeContract.signatureStrokePoints.Count} vector points.");
            return true;
        }

        public bool HasBoundGuardian()
        {
            return activeContract != null &&
                   !string.IsNullOrEmpty(activeContract.avatarName) &&
                   activeContract.signatureStrokePoints.Count >= 8;
        }

        public string StampSignatureOnForgedWeapon(string weaponName)
        {
            if (!HasBoundGuardian()) return weaponName;
            return $"{weaponName} [Maker's Mark: {activeContract.guardianPrintedName}]";
        }

        public string StampSignatureOnGuildContract(string contractTitle)
        {
            if (!HasBoundGuardian()) return contractTitle;
            return $"{contractTitle} (Sealed by Guardian {activeContract.guardianPrintedName} on {DateTimeOffset.FromUnixTimeSeconds(activeContract.bindingTimestampUnix):yyyy-MM-dd})";
        }
    }
}

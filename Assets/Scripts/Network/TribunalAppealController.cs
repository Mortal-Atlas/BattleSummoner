using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TribunalAppealController manages the Phantom Spire Quarantine Partition (shadowban sandbox)
/// and the interactive in-universe monolith "The Warden's Tribunal" for player appeals.
/// </summary>
public class TribunalAppealController : MonoBehaviour
{
    public static TribunalAppealController Instance { get; private set; }

    [SerializeField] private string tribunalEndpoint = "https://api.battlesummoner.internal/v1/appeals/submit";

    private const string PREF_QUARANTINE_STATUS = "Player_QuarantineStatus";
    private const string PREF_VIOLATION_REASON = "Player_QuarantineViolationReason";
    private const string TRIBUNAL_API_ENDPOINT = "https://api.battlesummoner.game/v1/tribunal/appeal";

    public async Task<bool> SubmitAppealAsync(string statement)
    {
        string json = $@"{{ ""deviceId"": ""{SystemInfo.deviceUniqueIdentifier}"", ""statement"": ""{statement}"" }}";
        using (UnityWebRequest req = new UnityWebRequest(tribunalEndpoint, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            return req.result == UnityWebRequest.Result.Success;
        }
    }

    [Header("UI Monolith Overlay")]
    [SerializeField] private GameObject tribunalModalPanel;
    [SerializeField] private TextMeshProUGUI statusTitleText;
    [SerializeField] private TextMeshProUGUI violationDetailsText;
    [SerializeField] private TMP_InputField appealInputField;
    [SerializeField] private Button submitAppealButton;
    [SerializeField] private TextMeshProUGUI feedbackResponseText;

    public static event Action<int, string> OnQuarantineStatusChanged;

    public static int QuarantineStatus
    {
        get => PlayerPrefs.GetInt(PREF_QUARANTINE_STATUS, 0);
        set
        {
            PlayerPrefs.SetInt(PREF_QUARANTINE_STATUS, value);
            PlayerPrefs.Save();
        }
    }

    public static string ViolationReason
    {
        get => PlayerPrefs.GetString(PREF_VIOLATION_REASON, "Thermodynamic Limit Override");
        set
        {
            PlayerPrefs.SetString(PREF_VIOLATION_REASON, value);
            PlayerPrefs.Save();
        }
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
            return;
        }

        if (submitAppealButton != null)
        {
            submitAppealButton.onClick.AddListener(OnSubmitAppealClicked);
        }

        if (tribunalModalPanel != null)
        {
            tribunalModalPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Flags player for quarantine partition (0 -> 1) without hard banning or kicking them.
    /// Next login routes them strictly to the Phantom Spire sandbox.
    /// </summary>
    public static void FlagPlayerForQuarantine(string reason)
    {
        QuarantineStatus = 1;
        ViolationReason = reason;

        Debug.LogWarning($"[TribunalAppealController] Player quarantined into Phantom Spire. Reason: {reason}");
        OnQuarantineStatusChanged?.Invoke(1, reason);

        if (SystemBroadcastManager.Instance != null)
        {
            SystemBroadcastManager.Instance.TriggerAchievement("ach_secret_forbidden_collapse", "LocalPlayer");
        }
    }

    public static void ClearQuarantine()
    {
        QuarantineStatus = 0;
        ViolationReason = "Pardoned";
        OnQuarantineStatusChanged?.Invoke(0, "Pardoned");
    }

    /// <summary>
    /// Called when player taps 'The Warden's Tribunal' monolith in the hub.
    /// </summary>
    public void OpenTribunalMonolithUI()
    {
        if (tribunalModalPanel == null) return;

        tribunalModalPanel.SetActive(true);

        bool isQuarantined = QuarantineStatus == 1;

        if (statusTitleText != null)
        {
            statusTitleText.text = isQuarantined 
                ? "⚖️ THE WARDEN'S TRIBUNAL — [PHANTOM SPIRE QUARANTINE]"
                : "⚖️ THE WARDEN'S TRIBUNAL — [CLEAN RECORD]";
            statusTitleText.color = isQuarantined ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(0.3f, 1f, 0.6f, 1f);
        }

        if (violationDetailsText != null)
        {
            violationDetailsText.text = isQuarantined
                ? $"Transgression Record: {ViolationReason}\nMatchmaking Pool: Phantom Spire (Entropy Harvester Sandbox)\n\nSubmit your canon in-universe plea to the Spire Wardens for cosmic arbitration."
                : "Your thermodynamic footprint complies with Spire laws. No active infractions detected.";
        }

        if (feedbackResponseText != null)
        {
            feedbackResponseText.text = "";
        }
    }

    public void CloseTribunalMonolithUI()
    {
        if (tribunalModalPanel != null)
        {
            tribunalModalPanel.SetActive(false);
        }
    }

    public void OnSubmitAppealClicked()
    {
        string appealPlea = appealInputField != null ? appealInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(appealPlea))
        {
            if (feedbackResponseText != null)
            {
                feedbackResponseText.text = "⚠️ You must state your plea to the Wardens.";
            }
            return;
        }

        StartCoroutine(SendAppealToDevTeamRoutine(appealPlea));
    }

    private IEnumerator SendAppealToDevTeamRoutine(string appealPlea)
    {
        if (feedbackResponseText != null)
        {
            feedbackResponseText.text = "Transmitting petition across the astral rift...";
        }

        string jsonPayload = "{" +
            $"\"playerToken\":\"{SystemInfo.deviceUniqueIdentifier}\"," +
            $"\"violation\":\"{ViolationReason}\"," +
            $"\"appealText\":\"{appealPlea.Replace("\"", "\\\"")}\"," +
            $"\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}" +
            "}";

        using (UnityWebRequest req = new UnityWebRequest(TRIBUNAL_API_ENDPOINT, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                if (feedbackResponseText != null)
                {
                    feedbackResponseText.text = "[TRIBUNAL ARCHIVE] Petition received by the Wardens of Floor 100. Judgment will arrive via the Spire Ticker.";
                    feedbackResponseText.color = Color.cyan;
                }
            }
            else
            {
                // In-universe lore fallback for offline / development environments
                if (feedbackResponseText != null)
                {
                    feedbackResponseText.text = "[TRIBUNAL ARCHIVE] The Wardens of Floor 100 have recorded your petition in the astral archive. Continue harvesting entropy in the Phantom Spire until judgment falls.";
                    feedbackResponseText.color = new Color(1f, 0.85f, 0.3f, 1f);
                }
            }
        }
    }
}

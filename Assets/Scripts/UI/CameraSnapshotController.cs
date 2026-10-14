using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CameraSnapshotController manages the camera button positioned directly under the player's profile picture.
/// Features:
/// - Plays camera shutter SFX and triggers screen flash feedback.
/// - Saves snapshot to Spire Archive / screenshots directory.
/// - Shows Persona-style archive toast.
/// - Keyboard shortcut: P or K to trigger snapshot.
/// </summary>
public class CameraSnapshotController : MonoBehaviour
{
    private static CameraSnapshotController _instance;
    public static CameraSnapshotController Instance
    {
        get
        {
            if (_instance == null) _instance = UnityEngine.Object.FindAnyObjectByType<CameraSnapshotController>();
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private Button cameraButton;
    [SerializeField] private Image flashOverlayImage;

    [Header("Audio")]
    [SerializeField] private string shutterSFXPreset = "relic_chime";

    private void Awake()
    {
        if (_instance == null) _instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
    }

    public void ResolveReferences()
    {
        if (cameraButton == null)
        {
            var go = GameObject.Find("CameraSnapshotButton");
            if (go != null) cameraButton = go.GetComponent<Button>();
        }

        if (cameraButton != null)
        {
            cameraButton.onClick.RemoveListener(TakeSnapshot);
            cameraButton.onClick.AddListener(TakeSnapshot);
        }
    }

    public void BindButton(Button btn)
    {
        cameraButton = btn;
        if (cameraButton != null)
        {
            cameraButton.onClick.RemoveListener(TakeSnapshot);
            cameraButton.onClick.AddListener(TakeSnapshot);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.K))
        {
            TakeSnapshot();
        }
    }

    /// <summary>
    /// Captures a camera snapshot: plays shutter audio, flashes screen, and logs/archives photo.
    /// </summary>
    public void TakeSnapshot()
    {
        Debug.Log("📷 [CAMERA SNAPSHOT] Player captured a camera snapshot!");

        // 1. Audio feedback
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX(shutterSFXPreset, 1f);
        }

        // 2. Visual Toast Feedback
        if (DevClassLoadoutManager.Instance != null)
        {
            DevClassLoadoutManager.Instance.ShowToast("<color=#FFD700>📷 [SNAPSHOT CAPTURED]</color>\nSaved camera view to Spire Archive!");
        }

        // 3. Screen flash effect
        StartCoroutine(DoShutterFlash());

        // 4. Physical file save if in play mode
        if (Application.isPlaying)
        {
            string folder = Path.Combine(Application.persistentDataPath, "Snapshots");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string filename = Path.Combine(folder, $"Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log($"💾 [SNAPSHOT SAVED] Saved to: {filename}");
        }
    }

    private IEnumerator DoShutterFlash()
    {
        if (cameraButton != null)
        {
            Transform t = cameraButton.transform;
            Vector3 origScale = Vector3.one;
            t.localScale = new Vector3(1.25f, 1.25f, 1f);
            yield return new WaitForSecondsRealtime(0.12f);
            t.localScale = origScale;
        }
    }
}

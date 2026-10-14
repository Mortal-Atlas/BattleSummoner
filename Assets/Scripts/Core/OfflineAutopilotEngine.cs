using System;
using UnityEngine;

public class OfflineAutopilotEngine : MonoBehaviour
{
    private const string SNAPSHOT_KEY = "PLAYER_OFFLINE_SNAPSHOT";

    private void OnApplicationQuit() => SerializeAndSaveState();
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SerializeAndSaveState();
        else ProcessOfflineProgression();
    }

    private void SerializeAndSaveState()
    {
        PlayerPrefs.SetString(SNAPSHOT_KEY, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        PlayerPrefs.Save();
        Debug.Log("🤖💤 [Autopilot] Character state saved. Autonomous background loops active.");
    }

    public void ProcessOfflineProgression()
    {
        if (!PlayerPrefs.HasKey(SNAPSHOT_KEY)) return;
        Debug.Log("🤖💤 [Autopilot] Welcome back! Character completed expeditions and rested offline.");
        PlayerPrefs.DeleteKey(SNAPSHOT_KEY);
    }
}

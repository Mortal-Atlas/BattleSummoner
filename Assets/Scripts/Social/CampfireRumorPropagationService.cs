using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldRumorPayload
{
    public string rumorId;
    public string originPlayerName;
    public int floorStratum;
    public Vector3 worldCoordinates;
    public string discoveryType; // e.g., "Prismatic Crystal Vein", "Apex Nemesis Sighting"
    public long expirationUnixTime;
}

public class CampfireRumorPropagationService : MonoBehaviour
{
    public static CampfireRumorPropagationService Instance { get; private set; }

    [Header("Audio & Dialogue Pipeline References")]
    [SerializeField] private BattleSummonerAudioMaster audioMaster;

    [Header("Local Character Rumor Queues (Persisted via SaveSystem)")]
    public List<WorldRumorPayload> myDiscoveredRumors = new List<WorldRumorPayload>();
    public List<WorldRumorPayload> unreadTavernRumorsOnLogin = new List<WorldRumorPayload>();
    public List<string> offlineWorldEventSummary = new List<string>();

    public event Action<WorldRumorPayload> OnDynamicMapMarkerInjected;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (audioMaster == null)
        {
            audioMaster = FindAnyObjectByType<BattleSummonerAudioMaster>();
        }
    }

    private void Start()
    {
        // Trigger login catch-up briefing upon game session start
        TriggerLoginWorldCatchUpBriefing();
    }

    /// <summary>
    /// Called during live runs or Offline Autopilot expeditions when a CaveMiningNode vein or Nemesis is found.
    /// </summary>
    public void RecordDiscovery(string playerName, int floor, Vector3 coords, string discoveryLabel, float validHours = 12f)
    {
        var rumor = new WorldRumorPayload
        {
            rumorId = Guid.NewGuid().ToString(),
            originPlayerName = playerName,
            floorStratum = floor,
            worldCoordinates = coords,
            discoveryType = discoveryLabel,
            expirationUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)(validHours * 3600f)
        };
        myDiscoveredRumors.Add(rumor);
    }

    /// <summary>
    /// Executed on server/tavern tick when two Offline Autopilot characters rest near the same campfire.
    /// </summary>
    public static void ExchangeRumorsAtTavern(CampfireRumorPropagationService aiShadowA, CampfireRumorPropagationService aiShadowB)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var rumor in aiShadowA.myDiscoveredRumors)
        {
            if (rumor.expirationUnixTime > now &&
                !aiShadowB.unreadTavernRumorsOnLogin.Exists(r => r.rumorId == rumor.rumorId))
            {
                aiShadowB.unreadTavernRumorsOnLogin.Add(rumor);
            }
        }
    }

    /// <summary>
    /// Triggered immediately upon Player Login to brief the user on what happened while they were offline.
    /// </summary>
    public void TriggerLoginWorldCatchUpBriefing()
    {
        if (unreadTavernRumorsOnLogin.Count == 0 && offlineWorldEventSummary.Count == 0) return;

        List<string> dialogueLines = new List<string>();

        foreach (var rumor in unreadTavernRumorsOnLogin)
        {
            string rawScript = $"[SFX: campfire_crackle | at: 0.0s] Heard a rumor from {rumor.originPlayerName}'s shadow about a {rumor.discoveryType} on the {rumor.floorStratum}th stratum. [SFX: parchment_mark | at: 2.4s]";

            string cleanSpeech = rawScript;
            if (audioMaster != null)
            {
                cleanSpeech = audioMaster.ParseCuesAndStripText(rawScript, out List<SfxCue> cues);
                Debug.Log($"[Login Catch-Up TTS] {cleanSpeech} (Scheduled {cues.Count} SFX cues)");

                // Play timed sound effects
                foreach (var cue in cues)
                {
                    audioMaster.PlayPresetSFX(cue.preset, 0.85f);
                }
            }
            dialogueLines.Add(cleanSpeech);

            // Organically inject temporary waypoint onto the Floor Mini-Map without a quest board
            OnDynamicMapMarkerInjected?.Invoke(rumor);
        }

        // Persona Dialogue UI briefing
        var dialogueUI = FindAnyObjectByType<PersonaDialogueUI>();
        if (dialogueUI != null && dialogueLines.Count > 0)
        {
            dialogueUI.ShowSimpleDialogue("Margo", "Campfire Shadow Dispatch", null, dialogueLines.ToArray());
        }

        unreadTavernRumorsOnLogin.Clear();
    }
}

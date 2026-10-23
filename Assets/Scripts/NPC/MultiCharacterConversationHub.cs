using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DialogueUtterance
{
    public string speakerId;      // "npc_margo", "npc_barnaby", "npc_zephyr", "player"
    public string speakerName;
    public string text;
    public string addresseeId;    // Target character being spoken to
    public float customDelayAfter = 1.6f;
}

[System.Serializable]
public class ConversationTopicArc
{
    public string topicTitle;
    public List<DialogueUtterance> utterances = new List<DialogueUtterance>();
}

[System.Serializable]
public class CharacterStageActor
{
    public string npcId;
    public string displayName;
    public Color themeColor = Color.white;
    public BespokeNPCObject profile;
    public Transform characterRoot;
    public Renderer meshRenderer;
    public Light spotlight;
    public GameObject speechBubbleContainer;
    public TextMeshPro speechBubbleText;
    public BattleSummoner.Customization.FacialExpressionController facialController;
    [HideInInspector] public Quaternion initialRotation;

    public BattleSummoner.Customization.FacialExpressionController GetOrFindFacialRig()
    {
        if (facialController == null && characterRoot != null)
        {
            facialController = characterRoot.GetComponentInChildren<BattleSummoner.Customization.FacialExpressionController>();
        }
        return facialController;
    }
}

/// <summary>
/// MultiCharacterConversationHub manages a live multi-character campfire dialogue hub
/// where Margo, Barnaby, and Zephyr autonomously converse with each other using distinct
/// ElevenLabs TTS voices and live typewriter speech bubbles.
/// When the player speaks or asks a question, all NPCs halt banter, turn to face the player,
/// provide tailored answers in their assigned voice, and smoothly resume their campfire banter.
/// </summary>
public class MultiCharacterConversationHub : MonoBehaviour
{
    public static MultiCharacterConversationHub Instance { get; private set; }

    [Header("Bespoke Stage Actors")]
    [SerializeField] private List<CharacterStageActor> actors = new List<CharacterStageActor>();
    private readonly Dictionary<string, CharacterStageActor> actorRegistry = new Dictionary<string, CharacterStageActor>();

    public CharacterStageActor GetActor(string npcId)
    {
        if (actorRegistry.Count == 0 && actors != null)
        {
            foreach (var a in actors)
            {
                if (a != null && !string.IsNullOrEmpty(a.npcId))
                {
                    actorRegistry[a.npcId] = a;
                }
            }
        }

        if (actorRegistry.TryGetValue(npcId, out var actor)) return actor;
        return null;
    }

    [Header("Player Target Transform")]
    [SerializeField] private Transform playerObserverTransform;

    [Header("UI Transcript & Telemetry")]
    [SerializeField] private TextMeshProUGUI transcriptLogText;
    [SerializeField] private ScrollRect transcriptScrollRect;
    [SerializeField] private TextMeshProUGUI activeSpeakerHeader;
    [SerializeField] private TMP_InputField playerMessageInput;
    [SerializeField] private Button sendPlayerMessageButton;
    [SerializeField] private Button toggleBanterButton;
    [SerializeField] private TextMeshProUGUI toggleBanterButtonText;

    [SerializeField] private Button returnToDungeonButton;
    [SerializeField] private Button restAtCampfireButton;
    [SerializeField] private Button toggleTranscriptButton;
    [SerializeField] private GameObject transcriptPanelObj;

    [Header("Quick Question Chips")]
    [SerializeField] private Button quickPromptBtn1; // "What lies on Floor 50?"
    [SerializeField] private Button quickPromptBtn2; // "Aria, what guidance do you have?"
    [SerializeField] private Button quickPromptBtn3; // "Leo, how is the vanguard holding?"
    [SerializeField] private Button quickPromptBtn4; // "Tell me about the ancient rifts."

    [Header("Banter Parameters")]
    [SerializeField] private float basePauseBetweenSpeakers = 1.8f;
    [SerializeField] private bool isBanterActive = true;
    [SerializeField] private bool isPlayerTurnActive = false;

    private Coroutine activeBanterRoutine;
    private Coroutine activeTypewriterRoutine;
    private readonly List<ConversationTopicArc> authoredTopicArcs = new List<ConversationTopicArc>();
    private int currentArcIndex = 0;
    private int currentUtteranceIndex = 0;
    private string currentActiveSpeakerId = "";

    public event Action<string, string> OnUtteranceSpoken; // speakerId, text

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeActorRegistry();
        BuildAuthoredConversationArcs();
        BindUIControls();
    }

    private void Start()
    {
        if (transcriptLogText != null)
        {
            transcriptLogText.text = "<color=#666688><i>[Campfire embers crackle softly as companions rest by the hearth...]</i></color>\n\n";
        }

        // Hide transcript by default to preserve visual clarity and avoid overstimulation
        if (transcriptPanelObj != null)
        {
            transcriptPanelObj.SetActive(false);
        }

        if (isBanterActive)
        {
            activeBanterRoutine = StartCoroutine(AutonomousCampfireBanterRoutine());
        }
    }

    private void InitializeActorRegistry()
    {
        actorRegistry.Clear();
        foreach (var actor in actors)
        {
            if (actor != null && !string.IsNullOrEmpty(actor.npcId))
            {
                actorRegistry[actor.npcId] = actor;
                if (actor.characterRoot != null)
                {
                    actor.initialRotation = actor.characterRoot.rotation;
                }
                if (actor.speechBubbleContainer != null)
                {
                    actor.speechBubbleContainer.SetActive(false);
                }
                if (actor.spotlight != null)
                {
                    actor.spotlight.intensity = 0.5f;
                }
            }
        }
    }

    private void BindUIControls()
    {
        if (sendPlayerMessageButton != null)
        {
            sendPlayerMessageButton.onClick.AddListener(() => BattleSummonerAudioMaster.Instance?.PlayButtonClick());
            sendPlayerMessageButton.onClick.AddListener(OnPlayerSendClicked);
        }

        if (toggleBanterButton != null)
        {
            toggleBanterButton.onClick.AddListener(() => BattleSummonerAudioMaster.Instance?.PlayButtonClick());
            toggleBanterButton.onClick.AddListener(ToggleBanterPlayback);
        }

        if (returnToDungeonButton != null)
        {
            returnToDungeonButton.onClick.AddListener(() => BattleSummonerAudioMaster.Instance?.PlayButtonClick());
            returnToDungeonButton.onClick.AddListener(BreakCampAndReturn);
        }

        if (restAtCampfireButton != null)
        {
            restAtCampfireButton.onClick.AddListener(() => BattleSummonerAudioMaster.Instance?.PlayButtonClick());
            restAtCampfireButton.onClick.AddListener(RestAtCampfire);
        }

        if (toggleTranscriptButton != null)
        {
            toggleTranscriptButton.onClick.AddListener(() => BattleSummonerAudioMaster.Instance?.PlayButtonClick());
            toggleTranscriptButton.onClick.AddListener(ToggleTranscriptPanel);
        }

        if (quickPromptBtn1 != null) quickPromptBtn1.onClick.AddListener(() => SubmitPlayerUtterance("What lies on Floor 50?"));
        if (quickPromptBtn2 != null) quickPromptBtn2.onClick.AddListener(() => StartDirectDialogueWithActor(GetBestCompanionId(true)));
        if (quickPromptBtn3 != null) quickPromptBtn3.onClick.AddListener(() => StartDirectDialogueWithActor(GetBestCompanionId(false)));
        if (quickPromptBtn4 != null) quickPromptBtn4.onClick.AddListener(() => SubmitPlayerUtterance("Tell me about the ancient rifts we are exploring."));
    }

    private string GetBestCompanionId(bool femaleFirst)
    {
        if (femaleFirst)
        {
            if (actorRegistry.ContainsKey("starter_female")) return "starter_female";
            if (actorRegistry.ContainsKey("npc_margo")) return "npc_margo";
        }
        else
        {
            if (actorRegistry.ContainsKey("starter_male")) return "starter_male";
            if (actorRegistry.ContainsKey("npc_barnaby")) return "npc_barnaby";
            if (actorRegistry.ContainsKey("npc_zephyr")) return "npc_zephyr";
        }

        foreach (var id in actorRegistry.Keys) return id;
        return "starter_female";
    }

    public void BreakCampAndReturn()
    {
        Debug.Log("[MultiCharacterConversationHub] Packing up camp and returning to journey...");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.BreakCamp();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("OverworldMap");
        }
    }

    public void ReturnToDungeon()
    {
        BreakCampAndReturn();
    }

    public void ToggleTranscriptPanel()
    {
        if (transcriptPanelObj != null)
        {
            transcriptPanelObj.SetActive(!transcriptPanelObj.activeSelf);
        }
    }

    public void RestAtCampfire()
    {
        Debug.Log("[MultiCharacterConversationHub] Resting at campfire hearth...");
        var hero = GameObject.Find("Player_Hero") ?? GameObject.FindWithTag("Player");
        var prog = hero != null ? (hero.GetComponent<PlayerProgression>() ?? hero.GetComponentInChildren<PlayerProgression>()) : UnityEngine.Object.FindAnyObjectByType<PlayerProgression>();
        if (prog != null)
        {
            prog.currentHp = prog.maxHp;
            prog.currentMp = prog.maxMp;
        }

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
        }

        if (DevClassLoadoutManager.Instance != null)
        {
            DevClassLoadoutManager.Instance.ShowToast("✦ RESTED AT HEARTH — HP & MP RESTORED TO 100% ✦");
        }

        AppendToTranscript("Campfire Hearth", "The soothing warmth of the hearth and fragrant herbal stew wash away fatigue. HP and MP fully restored!", new Color(1f, 0.85f, 0.4f));
    }

    #region Autonomous Inter-NPC Banter System
    private IEnumerator AutonomousCampfireBanterRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        while (isBanterActive)
        {
            if (isPlayerTurnActive)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (authoredTopicArcs.Count == 0) yield break;

            var currentArc = authoredTopicArcs[currentArcIndex % authoredTopicArcs.Count];
            if (currentUtteranceIndex >= currentArc.utterances.Count)
            {
                currentUtteranceIndex = 0;
                currentArcIndex++;
                yield return new WaitForSeconds(2.5f); // Pause between distinct topic arcs
                continue;
            }

            var utterance = currentArc.utterances[currentUtteranceIndex];
            yield return StartCoroutine(ExecuteSpokenUtterance(utterance));

            currentUtteranceIndex++;
            yield return new WaitForSeconds(utterance.customDelayAfter);
        }
    }

    private IEnumerator ExecuteSpokenUtterance(DialogueUtterance utterance)
    {
        currentActiveSpeakerId = utterance.speakerId;

        CharacterStageActor speaker = null;
        actorRegistry.TryGetValue(utterance.speakerId, out speaker);

        CharacterStageActor addressee = null;
        if (!string.IsNullOrEmpty(utterance.addresseeId))
        {
            actorRegistry.TryGetValue(utterance.addresseeId, out addressee);
        }

        // Highlight speaker, dim other actors
        HighlightActiveSpeaker(utterance.speakerId);

        // Rotate speaker towards addressee (or player if specified)
        if (speaker != null && speaker.characterRoot != null)
        {
            Vector3 targetLook = (addressee != null && addressee.characterRoot != null)
                ? addressee.characterRoot.position
                : (playerObserverTransform != null ? playerObserverTransform.position : speaker.characterRoot.position + speaker.characterRoot.forward);

            Vector3 dir = (targetLook - speaker.characterRoot.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                speaker.characterRoot.rotation = Quaternion.LookRotation(dir);
            }
        }

        // Update active speaker UI header
        if (activeSpeakerHeader != null)
        {
            string colHex = speaker != null ? ColorUtility.ToHtmlStringRGB(speaker.themeColor) : "FFFFFF";
            activeSpeakerHeader.text = $"<color=#{colHex}>● {utterance.speakerName}</color> speaking...";
        }

        Debug.Log($"[Campfire Hub] {utterance.speakerName} -> {(addressee != null ? addressee.displayName : "Group")}: \"{utterance.text}\"");

        // Append to transcript log (clean text, no double scheduling)
        AppendToTranscript(utterance.speakerName, utterance.text, speaker != null ? speaker.themeColor : Color.white);

        string cleanDialogue = BattleSummoner.Audio.SFXCueParser.StripSfxTags(utterance.text);

        // Display 3D speech bubble
        if (speaker != null && speaker.speechBubbleContainer != null && speaker.speechBubbleText != null)
        {
            speaker.speechBubbleContainer.SetActive(true);
            speaker.speechBubbleText.text = "";
        }

        var facialRig = speaker != null ? speaker.GetOrFindFacialRig() : null;
        bool speechCompleted = false;

        // Trigger Voiceover through ElevenLabs TTS Bridge with synchronized SFX & Lip Sync
        if (ElevenLabsTTSBridge.Instance != null)
        {
            ElevenLabsTTSBridge.Instance.SpeakDialogueSynchronized(
                utterance.text,
                utterance.speakerId,
                facialRig,
                onPlaybackStarted: () =>
                {
                    OnUtteranceSpoken?.Invoke(utterance.speakerId, utterance.text);
                },
                onPlaybackCompleted: () =>
                {
                    speechCompleted = true;
                },
                onProgress: (progress) =>
                {
                    if (speaker != null && speaker.speechBubbleText != null)
                    {
                        int charCount = Mathf.Clamp(Mathf.RoundToInt(progress * cleanDialogue.Length), 0, cleanDialogue.Length);
                        speaker.speechBubbleText.text = cleanDialogue.Substring(0, charCount);
                    }
                }
            );

            // Wait until the speech playback finishes
            while (!speechCompleted)
            {
                yield return null;
            }
        }
        else
        {
            // Direct text display fallback
            if (speaker != null && speaker.speechBubbleText != null)
            {
                speaker.speechBubbleText.text = cleanDialogue;
            }
            yield return new WaitForSeconds(Mathf.Clamp(cleanDialogue.Length * 0.05f, 1.5f, 5.0f));
        }

        // Brief reading pause after speech ends
        yield return new WaitForSeconds(0.35f);

        // Hide speech bubble after speaking
        if (speaker != null && speaker.speechBubbleContainer != null)
        {
            speaker.speechBubbleContainer.SetActive(false);
        }
    }

    private void HighlightActiveSpeaker(string speakerId)
    {
        foreach (var kvp in actorRegistry)
        {
            var actor = kvp.Value;
            bool isCurrent = (actor.npcId == speakerId);

            if (actor.spotlight != null)
            {
                actor.spotlight.intensity = isCurrent ? 2.5f : 0.4f;
                actor.spotlight.color = isCurrent ? actor.themeColor : new Color(0.4f, 0.4f, 0.5f);
            }

            if (actor.speechBubbleContainer != null && !isCurrent)
            {
                actor.speechBubbleContainer.SetActive(false);
            }
        }
    }

    private void AppendToTranscript(string speakerName, string text, Color speakerColor)
    {
        if (transcriptLogText == null) return;

        string colHex = ColorUtility.ToHtmlStringRGB(speakerColor);
        string cleanText = BattleSummoner.Audio.SFXCueParser.StripSfxTags(text);

        transcriptLogText.text += $"<color=#{colHex}><b>[{speakerName}]</b></color>: {cleanText}\n\n";

        // Auto scroll to bottom
        if (transcriptScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            transcriptScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    #endregion

    #region Player Interruption & Focus System
    public void OnPlayerSendClicked()
    {
        if (playerMessageInput != null && !string.IsNullOrWhiteSpace(playerMessageInput.text))
        {
            string message = playerMessageInput.text.Trim();
            playerMessageInput.text = "";
            SubmitPlayerUtterance(message);
        }
    }

    /// <summary>
    /// Player speaks to the assembled NPCs.
    /// Halts autonomous background chatter, focuses all NPC gazes on the player,
    /// routes direct answering dialogue from the primary respondent,
    /// followed by banter from a second companion, and then smoothly resumes.
    /// </summary>
    public void SubmitPlayerUtterance(string playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText)) return;

        if (activeBanterRoutine != null)
        {
            StopCoroutine(activeBanterRoutine);
        }

        StartCoroutine(HandlePlayerInteractionSequence(playerText));
    }

    private IEnumerator HandlePlayerInteractionSequence(string playerText)
    {
        isPlayerTurnActive = true;

        // 1. Log player statement in bright emerald
        AppendToTranscript("Delver", playerText, new Color(0.1f, 1f, 0.5f));

        // 2. All NPCs turn to face the player
        if (playerObserverTransform != null)
        {
            foreach (var actor in actors)
            {
                if (actor.characterRoot != null)
                {
                    Vector3 lookDir = (playerObserverTransform.position - actor.characterRoot.position);
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        actor.characterRoot.rotation = Quaternion.LookRotation(lookDir);
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.6f);

        // 3. Determine best responding NPC based on query context
        string lower = playerText.ToLowerInvariant();
        bool hasStarters = actorRegistry.ContainsKey("starter_female") || actorRegistry.ContainsKey("starter_male");
        string primaryNpcId = hasStarters ? "starter_female" : "npc_margo";
        string secondaryNpcId = hasStarters ? "starter_male" : "npc_barnaby";

        if (hasStarters)
        {
            if (lower.Contains("blade") || lower.Contains("fight") || lower.Contains("shield") || lower.Contains("vanguard") || lower.Contains("leo") || lower.Contains("weapon"))
            {
                primaryNpcId = actorRegistry.ContainsKey("starter_male") ? "starter_male" : "starter_female";
                secondaryNpcId = actorRegistry.ContainsKey("starter_female") ? "starter_female" : primaryNpcId;
            }
            else
            {
                primaryNpcId = actorRegistry.ContainsKey("starter_female") ? "starter_female" : "starter_male";
                secondaryNpcId = actorRegistry.ContainsKey("starter_male") ? "starter_male" : primaryNpcId;
            }
        }
        else if (lower.Contains("forge") || lower.Contains("blade") || lower.Contains("anvil") || lower.Contains("temper") || lower.Contains("metal") || lower.Contains("fracture") || lower.Contains("barnaby"))
        {
            primaryNpcId = "npc_barnaby";
            secondaryNpcId = "npc_margo";
        }
        else if (lower.Contains("scout") || lower.Contains("wind") || lower.Contains("aqueduct") || lower.Contains("gargoyle") || lower.Contains("path") || lower.Contains("below") || lower.Contains("zephyr"))
        {
            primaryNpcId = "npc_zephyr";
            secondaryNpcId = "npc_barnaby";
        }
        else if (lower.Contains("floor 50") || lower.Contains("boss") || lower.Contains("sentinel") || lower.Contains("arbiter"))
        {
            primaryNpcId = "npc_zephyr";
            secondaryNpcId = "npc_margo";
        }

        // 4. Generate contextual responses
        string primaryReply = GenerateAuthoredReplyForNPC(primaryNpcId, playerText);
        string secondaryChimeIn = GenerateAuthoredChimeIn(secondaryNpcId, primaryNpcId, playerText);

        actorRegistry.TryGetValue(primaryNpcId, out var primaryActor);
        actorRegistry.TryGetValue(secondaryNpcId, out var secondaryActor);

        // 5. Primary respondent speaks directly to player
        yield return StartCoroutine(ExecuteSpokenUtterance(new DialogueUtterance
        {
            speakerId = primaryNpcId,
            speakerName = primaryActor != null ? primaryActor.displayName : "Companion",
            text = primaryReply,
            addresseeId = "player",
            customDelayAfter = 1.4f
        }));

        yield return new WaitForSeconds(1.0f);

        // 6. Secondary companion reacts/chimes in (if distinct from primary)
        if (secondaryNpcId != primaryNpcId)
        {
            yield return StartCoroutine(ExecuteSpokenUtterance(new DialogueUtterance
            {
                speakerId = secondaryNpcId,
                speakerName = secondaryActor != null ? secondaryActor.displayName : "Companion",
                text = secondaryChimeIn,
                addresseeId = primaryNpcId,
                customDelayAfter = 2.0f
            }));
        }

        // 7. Transition back to autonomous campfire conversation
        isPlayerTurnActive = false;
        if (isBanterActive)
        {
            activeBanterRoutine = StartCoroutine(AutonomousCampfireBanterRoutine());
        }
    }

    private string GenerateAuthoredReplyForNPC(string npcId, string playerInput)
    {
        string lower = playerInput.ToLowerInvariant();

        if (npcId == "starter_female")
        {
            if (lower.Contains("stew") || lower.Contains("food") || lower.Contains("cauldron") || lower.Contains("eat"))
                return "Aria: 'The stew in our alchemist cauldron has wild mountain chanterelles and sunlit roots. Have a warm bowl and let your strength return.'";
            if (lower.Contains("rest") || lower.Contains("camp") || lower.Contains("sleep"))
                return "Aria: 'Pacing ourselves at the campfire is what keeps us safe. Rest your eyes and let the fire warm you; tomorrow is another chapter.'";
            if (lower.Contains("rift") || lower.Contains("magic") || lower.Contains("ancient"))
                return "Aria: 'These rifts are living dimensional fractures. They test our mental fortitude as much as our weapons. Maintain your inner calm, and we will master them.'";
            return "Aria: 'Take heart. When we stay focused and trust one another, there isn't a dungeon floor we can't conquer together.'";
        }
        else if (npcId == "starter_male")
        {
            if (lower.Contains("blade") || lower.Contains("shield") || lower.Contains("fight") || lower.Contains("weapon"))
                return "Leo: 'My vanguard shield is steady and my blade is razor sharp. Whenever you unleash your summon or spells, I'll hold the battle line!'";
            if (lower.Contains("rest") || lower.Contains("camp") || lower.Contains("tired"))
                return "Leo: 'Setting up camp by this hearth was the best call. Catching our breath here means we enter the next rift with full vitality.'";
            return "Leo: 'We've faced down tough monsters together, and we're only growing stronger. Let's finish our meal and recharge for the road ahead.'";
        }
        else if (npcId == "npc_barnaby")
        {
            if (lower.Contains("fracture") || lower.Contains("blade") || lower.Contains("temper"))
                return "Barnaby: 'Keep that edge quenched, delver. Two strikes is a warning sign; don't let avarice blow your anvil to kingdom come. Treat high-carbon steel with respect.' [SFX: anvil_shatter | at: 0.2s | vol: 70%]";
            if (lower.Contains("floor 50") || lower.Contains("boss"))
                return "Barnaby: 'The Arbiter on Floor 50? Its armor plate is forged from ancient basalt alloy. You won't pierce it without an Apex living weapon or heavy stagger burst.'";
            return "Barnaby: 'Hah! Bring your ore to my hearth anytime. If you want gear that bites back instead of chipping on gargoyle bone, I'm your artisan.'";
        }
        else if (npcId == "npc_zephyr")
        {
            if (lower.Contains("gargoyle") || lower.Contains("scout") || lower.Contains("current") || lower.Contains("aqueduct"))
                return "Zephyr: 'The winds in the Lower Aqueduct carry ozone and rot. The gargoyles nest in clusters of four along the high vault rafters. Walk silent, or face their kinetic dive.' [SFX: relic_chime | at: 0.1s | vol: 65%]";
            if (lower.Contains("floor 50") || lower.Contains("boss"))
                return "Zephyr: 'I climbed to the outer rim of Floor 50. The air becomes thin, charged with astral static. The Arbiter does not sleep; it watches the ascension gate.'";
            return "Zephyr: 'The Spire's heartbeat echoes through the rock. Follow the wind drafts if you ever lose your footing in the subterranean mazes.'";
        }
        else // Margo
        {
            if (lower.Contains("geode") || lower.Contains("void") || lower.Contains("amethyst") || lower.Contains("crystal"))
                return "Margo: 'That Void Amethyst contains unrefined entropic resonance. If you bring it to my alembic, I can distill the fracture debt out of your scythe. Just don't track mud on my arm-warmers. (¬_¬)' [SFX: relic_chime | at: 0.2s | vol: 80%]";
            if (lower.Contains("floor 50") || lower.Contains("boss"))
                return "Margo: 'Floor 50 is where thermodynamic backfires become truly catastrophic. If you try casting an uncontained gravitational singularity up there, you will collapse your own ribcage. Be smart about it. 🖤'";
            return "Margo: 'Back already, delver? Don't look at me like that—I'm an alchemist, not a nanny. If you need tonics or geode appraisal, step into my sanctum.' [SFX: relic_chime | at: 0.1s | vol: 65%]";
        }
    }

    private string GenerateAuthoredChimeIn(string secondaryNpcId, string primaryNpcId, string playerInput)
    {
        if (secondaryNpcId == "starter_female")
        {
            return "Aria: 'Leo speaks the truth. Let the quiet rhythm of the fire center your mind tonight.'";
        }
        else if (secondaryNpcId == "starter_male")
        {
            return "Leo: 'Count on us, partner. Between Aria's arcane insight and our collective resolve, we've got this.'";
        }
        else if (secondaryNpcId == "npc_margo")
        {
            return "Margo: 'Listen to Barnaby if you must, but when your blade gets hungry, high-carbon steel won't feed its ego index. You'll need dark mana cores from my vault.'";
        }
        else if (secondaryNpcId == "npc_barnaby")
        {
            return "Barnaby: 'Hah! Margo talks a grim game, but without my bellows heating her cauldrons, her potions would be cold vinegar. We've got your back, delver.'";
        }
        else // Zephyr
        {
            return "Zephyr: 'Whatever your choice, the shadows grow longer by the minute. Gather your provisions, delver; the descent waits for no one.'";
        }
    }
    #endregion

    #region Direct 1-on-1 Dialogue Integration
    public void StartDirectDialogueWithActor(string npcId)
    {
        var actor = GetActor(npcId);
        string displayName = actor != null ? actor.displayName : "Companion";

        Debug.Log($"[Campfire Hub] Opening 1-on-1 Persona dialogue with {displayName} ({npcId})...");

        // Pause autonomous banter while talking directly
        if (activeBanterRoutine != null)
        {
            StopCoroutine(activeBanterRoutine);
            activeBanterRoutine = null;
        }

        if (activeSpeakerHeader != null)
        {
            activeSpeakerHeader.text = $"<color=#FFD54F>★ Speaking directly with {displayName}</color>";
        }

        if (BattleSummoner.Dialogue.CampfirePanoramicCameraController.Instance != null)
        {
            BattleSummoner.Dialogue.CampfirePanoramicCameraController.Instance.InitiateDirectConversation(npcId, displayName);
        }
        else if (BattleSummoner.Dialogue.CampfireDualDialogueController.Instance != null)
        {
            BattleSummoner.Dialogue.CampfireDualDialogueController.Instance.StartDialogueWithCharacter(npcId, displayName);
        }
    }
    #endregion

    #region Banter Controls
    public void ToggleBanterPlayback()
    {
        isBanterActive = !isBanterActive;

        if (toggleBanterButtonText != null)
        {
            toggleBanterButtonText.text = isBanterActive ? "PAUSE BANTER" : "RESUME BANTER";
        }

        if (isBanterActive)
        {
            if (activeBanterRoutine != null) StopCoroutine(activeBanterRoutine);
            activeBanterRoutine = StartCoroutine(AutonomousCampfireBanterRoutine());
        }
        else
        {
            if (activeBanterRoutine != null)
            {
                StopCoroutine(activeBanterRoutine);
                activeBanterRoutine = null;
            }
            if (activeSpeakerHeader != null)
            {
                activeSpeakerHeader.text = "<color=#AAAAAA>Campfire banter paused. Tap a prompt or speak to resume.</color>";
            }
        }
    }
    #endregion

    #region Authored Multi-Turn Thematic Arcs
    private void BuildAuthoredConversationArcs()
    {
        authoredTopicArcs.Clear();

        // Arc 0A: Warm Hearth & Campfire Comfort (Starter Companions)
        var starterArc1 = new ConversationTopicArc { topicTitle = "Hearth & Harmony" };
        starterArc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "starter_female",
            speakerName = "Aria",
            addresseeId = "starter_male",
            text = "The night chill is quiet tonight. It's easy to forget how perilous the rifts are when you're resting by a crackling campfire.",
            customDelayAfter = 1.6f
        });
        starterArc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "starter_male",
            speakerName = "Leo",
            addresseeId = "starter_female",
            text = "That's why setting up camp is essential. A delver who pushes forward without rest is bound to make fatal mistakes.",
            customDelayAfter = 1.8f
        });
        starterArc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "starter_female",
            speakerName = "Aria",
            addresseeId = "starter_male",
            text = "The alchemist cauldron has been simmering nicely. I infused the stew with wild star-sage to speed up our mana restoration.",
            customDelayAfter = 1.6f
        });
        starterArc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "starter_male",
            speakerName = "Leo",
            addresseeId = "starter_female",
            text = "Smells incredible. We'll be at full fighting strength before dawn. Let's make sure our summoner gets plenty of rest too.",
            customDelayAfter = 2.0f
        });
        authoredTopicArcs.Add(starterArc1);

        // Arc 0B: Delver Synergy & Tactical Calm
        var starterArc2 = new ConversationTopicArc { topicTitle = "Tactical Synergy" };
        starterArc2.utterances.Add(new DialogueUtterance
        {
            speakerId = "starter_male",
            speakerName = "Leo",
            addresseeId = "starter_female",
            text = "During today's skirmish, your warding barrier deflected that void shockwave right before it breached our perimeter.",
            customDelayAfter = 1.7f
        });
        starterArc2.utterances.Add(new DialogueUtterance
        {
            speakerId = "starter_female",
            speakerName = "Aria",
            addresseeId = "starter_male",
            text = "And your vanguard shield bash gave me the casting window I needed. When we move with calm synchronization, no rift anomaly can overwhelm us.",
            customDelayAfter = 1.8f
        });
        starterArc2.utterances.Add(new DialogueUtterance
        {
            speakerId = "starter_male",
            speakerName = "Leo",
            addresseeId = "starter_female",
            text = "Clear eyes and steady breath. That's how we conquer the deep spires.",
            customDelayAfter = 2.2f
        });
        authoredTopicArcs.Add(starterArc2);

        // Arc 1: Alchemical Geodes vs Forge Temper
        var arc1 = new ConversationTopicArc { topicTitle = "Geodes vs Steel" };
        arc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_margo",
            speakerName = "Margo",
            addresseeId = "npc_barnaby",
            text = "Barnaby, your hammers are vibrating at a frequency that's clouding my Void Amethyst distillations. Tone it down, will you? (¬_¬)",
            customDelayAfter = 1.6f
        });
        arc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_barnaby",
            speakerName = "Barnaby",
            addresseeId = "npc_margo",
            text = "Tone it down?! That's Damascus fold-welding, girl! [SFX: anvil_shatter | at: 0.6s | vol: 75%] Without my temper, our delver's scythe would snap like a dry pine twig on Floor 10!",
            customDelayAfter = 1.8f
        });
        arc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_zephyr",
            speakerName = "Zephyr",
            addresseeId = "npc_margo",
            text = "Both of you, hush. [SFX: whoosh | at: 0.4s | vol: 60%] The drafts from the Lower Aqueduct are carrying strange vibrations. It's not Barnaby's anvil... something heavy is shifting below.",
            customDelayAfter = 1.6f
        });
        arc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_margo",
            speakerName = "Margo",
            addresseeId = "npc_zephyr",
            text = "Fabulous. More gargoyles to feed to my scythe. [SFX: relic_chime | at: 0.8s | vol: 70%] Did you at least unearth the Azurite clusters I asked for, Zephyr?",
            customDelayAfter = 1.5f
        });
        arc1.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_zephyr",
            speakerName = "Zephyr",
            addresseeId = "npc_margo",
            text = "Three unrefined nodes in the sunken vein. Chiseled clean along the fault line. No volatile fractures.",
            customDelayAfter = 1.8f
        });
        authoredTopicArcs.Add(arc1);

        // Arc 2: The Three-Strike Rule & Thermodynamic Collapse
        var arc2 = new ConversationTopicArc { topicTitle = "The Three-Strike Rule" };
        arc2.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_barnaby",
            speakerName = "Barnaby",
            addresseeId = "npc_margo",
            text = "I saw that delver tampering with a gravitational singularity matrix earlier. [SFX: singularity_whir | at: 0.5s | vol: 80%] Pure reckless insanity!",
            customDelayAfter = 1.6f
        });
        arc2.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_margo",
            speakerName = "Margo",
            addresseeId = "npc_barnaby",
            text = "I warned them. The Spire doesn't tolerate cheat codes or thermodynamic over-pressurization. [SFX: glitch | at: 0.8s | vol: 65%] Breach the limit, and the anvil blows you straight into the Phantom Spire.",
            customDelayAfter = 1.8f
        });
        arc2.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_zephyr",
            speakerName = "Zephyr",
            addresseeId = "npc_barnaby",
            text = "The Wardens of Floor 100 never blink. [SFX: thunder | at: 0.3s | vol: 85%] They quarantine rule-breakers before the shockwave even settles in the dust.",
            customDelayAfter = 1.7f
        });
        arc2.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_barnaby",
            speakerName = "Barnaby",
            addresseeId = "npc_zephyr",
            text = "Aye. That's why I keep to good honest metallurgy. Hit the anvil three times on a fractured matrix, [SFX: anvil_shatter | at: 1.2s | vol: 85%] and you've got no one to blame but yourself!",
            customDelayAfter = 1.8f
        });
        authoredTopicArcs.Add(arc2);

        // Arc 3: Floor 50 Sovereign Arbiter & Bardic Hymns
        var arc3 = new ConversationTopicArc { topicTitle = "The Arbiter & Bardic Songs" };
        arc3.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_zephyr",
            speakerName = "Zephyr",
            addresseeId = "npc_margo",
            text = "I heard music echoing from the central chamber earlier. [SFX: chime | at: 0.3s | vol: 70%] Someone was playing a Kalimba notation... Song of Fortitude, if I'm not mistaken.",
            customDelayAfter = 1.7f
        });
        arc3.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_margo",
            speakerName = "Margo",
            addresseeId = "npc_zephyr",
            text = "A bard with a 17-tine Celestiphone. [SFX: bard_chord_strum | at: 0.4s | vol: 75%] At least their fingers were agile enough to dodge boss telegraphs while dual-thumbing the tines. 🖤",
            customDelayAfter = 1.9f
        });
        arc3.utterances.Add(new DialogueUtterance
        {
            speakerId = "npc_barnaby",
            speakerName = "Barnaby",
            addresseeId = "npc_margo",
            text = "Hah! Give me a solid broadsword over a plucked music box any day. [SFX: sword_whoosh | at: 0.6s | vol: 70%] Though... I won't deny that +25% armor resonance felt damn good during the gargoyle raid!",
            customDelayAfter = 1.8f
        });
        authoredTopicArcs.Add(arc3);
    }
    #endregion
}

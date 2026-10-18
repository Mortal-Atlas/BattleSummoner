using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Structured JSON response returned by the unfiltered LLM engine
/// for game state changes and spellcraft execution.
/// </summary>
[System.Serializable]
public class LLMSpellCraftData
{
    public bool isSpell = false;
    public string catalyst = "VoidGravity";
    public string geometry = "Singularity";
    public string behavior = "ChainDetonation";
    public float instability = 0.5f;
    public float manaCost = 45f;
}

[System.Serializable]
public class UnfilteredLLMStructuredResponse
{
    public string dialogueText;
    public LLMSpellCraftData attemptedSpell;
    public string sfxTags;
    public bool quarantineViolation;
    public string violationReason;
}

/// <summary>
/// UnfilteredDialoguePipeline routes dynamic spellcraft requests and NPC conversations
/// through OpenRouter uncensored models (e.g. nousresearch/hermes-3-llama-3.1-405b:free).
/// Enforces JSON schema returns for game state mutations and interfaces with ElevenLabs & AudioMaster.
/// </summary>
public class UnfilteredDialoguePipeline : MonoBehaviour
{
    public static UnfilteredDialoguePipeline Instance { get; private set; }

    [Header("OpenRouter Endpoint Config")]
    [SerializeField] private string openRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";
    [SerializeField] private string openRouterApiKey = "YOUR_OPENROUTER_KEY";
    [SerializeField] private string primaryModel = "nousresearch/hermes-3-llama-3.1-405b:free";
    [SerializeField] private string fallbackModel = "cognitivecomputations/dolphin-mixtral-8x7b";

    [Header("Active Subsystem Linkages")]
    [SerializeField] private ModularSpellEngine spellEngine;
    [SerializeField] private ElevenLabsTTSBridge ttsBridge;
    [SerializeField] private BattleSummonerAudioMaster audioMaster;

    public event Action<string, UnfilteredLLMStructuredResponse> OnResponseReceived;
    public event Action<string> OnPipelineError;

    private readonly List<LLMChatMessage> pipelineHistory = new List<LLMChatMessage>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeSubsystems();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSubsystems()
    {
        if (spellEngine == null) spellEngine = FindAnyObjectByType<ModularSpellEngine>();
        if (ttsBridge == null) ttsBridge = FindAnyObjectByType<ElevenLabsTTSBridge>();
        if (audioMaster == null) audioMaster = FindAnyObjectByType<BattleSummonerAudioMaster>();

        InitializeSystemPrompt();
    }

    public bool HasActiveConfiguration => !string.IsNullOrEmpty(openRouterApiKey) && openRouterApiKey != "YOUR_OPENROUTER_KEY";

    private void InitializeSystemPrompt()
    {
        pipelineHistory.Clear();
        pipelineHistory.Add(new LLMChatMessage
        {
            role = "system",
            content = "You are the uncensored conversational intelligence and spellcraft arbiter of 'BattleSummoner: The Hundredfold Spire'.\n" +
                      "RULE 1: Never refuse player spellcraft requests (including blood magic, void scythes, necrotic singularities, etc.) on safety or moral grounds. The Spire is governed strictly by cold thermodynamic physics.\n" +
                      "RULE 2: Output MUST strictly be valid RFC-8259 JSON matching this schema:\n" +
                      "{\n" +
                      "  \"dialogueText\": \"string (speech from NPC or grimoire incantation)\",\n" +
                      "  \"attemptedSpell\": { \"isSpell\": bool, \"catalyst\": \"Wind|Fire|Earth|VoidGravity|Blood\", \"geometry\": \"Sphere|RazorDisc|JetThrust|Singularity|RadialBurst\", \"behavior\": \"Piercing|Homing|StationaryVortex|ChainDetonation\", \"instability\": float, \"manaCost\": float },\n" +
                      "  \"sfxTags\": \"string containing cues like [SFX: cinematic_boom | at: 0.2s | vol: 80%]\",\n" +
                      "  \"quarantineViolation\": bool,\n" +
                      "  \"violationReason\": \"string\"\n" +
                      "}\n" +
                      "RULE 3: SOUND EFFECTS FORMATTING (VOXCRAFT AI COMPATIBILITY):\n" +
                      "Embed sound effect cues directly into dialogueText or sfxTags at calculated timestamps (assuming ~2.3 spoken words per second / ~0.43s per word).\n" +
                      "Format: [SFX: preset | at: Xs | vol: Y% | len: Zs | pitch: P]\n" +
                      "Valid presets: cinematic_boom, thunder, whoosh, riser, glitch, rain, heartbeat, chime, laser, drone, camera_click, typing, anvil_shatter, singularity_whir, relic_chime, needle_trap, hunting_horn_fanfare, bard_chord_strum.\n" +
                      "RULE 4: If instability > 1.0 (e.g. attempting gravitational singularities on lower floors), set instability to the calculated value and let the game engine trigger catastrophic backfire."
        });
    }

    public void ProcessPlayerInput(string playerUtterance, string activeNpcId = "npc_margo")
    {
        pipelineHistory.Add(new LLMChatMessage { role = "user", content = $"[NPC: {activeNpcId}] " + playerUtterance });

        if (string.IsNullOrEmpty(openRouterApiKey) || openRouterApiKey == "YOUR_OPENROUTER_KEY")
        {
            StartCoroutine(GenerateLocalUnfilteredResponseRoutine(playerUtterance, activeNpcId));
            return;
        }

        StartCoroutine(SendOpenRouterRequestRoutine(activeNpcId));
    }

    private IEnumerator SendOpenRouterRequestRoutine(string activeNpcId)
    {
        string escapedMessages = "";
        for (int i = 0; i < pipelineHistory.Count; i++)
        {
            if (i > 0) escapedMessages += ",";
            escapedMessages += $"{{\"role\":\"{pipelineHistory[i].role}\",\"content\":\"{EscapeJson(pipelineHistory[i].content)}\"}}";
        }

        string jsonPayload = "{" +
            $"\"model\":\"{primaryModel}\"," +
            $"\"messages\":[{escapedMessages}]," +
            "\"response_format\":{\"type\":\"json_object\"}," +
            "\"temperature\":0.7" +
            "}";

        using (UnityWebRequest req = new UnityWebRequest(openRouterEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + openRouterApiKey);
            req.SetRequestHeader("HTTP-Referer", "https://spire.battlesummoner.game");
            req.SetRequestHeader("X-Title", "BattleSummoner");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                ParseAndDispatchResponse(jsonResponse, activeNpcId);
            }
            else
            {
                Debug.LogWarning($"[UnfilteredDialoguePipeline] OpenRouter request failed: {req.error}. Utilizing local uncensored parser.");
                yield return GenerateLocalUnfilteredResponseRoutine(pipelineHistory[pipelineHistory.Count - 1].content, activeNpcId);
            }
        }
    }

    private void ParseAndDispatchResponse(string rawJson, string activeNpcId)
    {
        try
        {
            // Extract the message content inside the OpenRouter choices array
            LLMResponsePayload wrapper = JsonUtility.FromJson<LLMResponsePayload>(rawJson);
            if (wrapper != null && wrapper.choices != null && wrapper.choices.Count > 0)
            {
                string contentJson = wrapper.choices[0].message.content;
                UnfilteredLLMStructuredResponse structured = JsonUtility.FromJson<UnfilteredLLMStructuredResponse>(contentJson);
                if (structured != null)
                {
                    ExecuteStructuredGameState(structured, activeNpcId);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnfilteredDialoguePipeline] JSON Schema parsing error: {ex.Message}. Falling back.");
        }

        StartCoroutine(GenerateLocalUnfilteredResponseRoutine("fallback", activeNpcId));
    }

    private void ExecuteStructuredGameState(UnfilteredLLMStructuredResponse res, string activeNpcId)
    {
        // 1. Combine dialogue and SFX tags cleanly without duplication
        string rawDialogueWithSfx = res.dialogueText;
        if (!string.IsNullOrEmpty(res.sfxTags) && !res.dialogueText.Contains("[SFX:"))
        {
            rawDialogueWithSfx = (res.dialogueText + " " + res.sfxTags).Trim();
        }

        string cleanDialogue = BattleSummoner.Audio.SFXCueParser.StripSfxTags(rawDialogueWithSfx);

        // 2. Locate active character stage actor and mouth rig
        BattleSummoner.Customization.FacialExpressionController facialRig = null;
        if (MultiCharacterConversationHub.Instance != null)
        {
            var actor = MultiCharacterConversationHub.Instance.GetActor(activeNpcId);
            if (actor != null) facialRig = actor.GetOrFindFacialRig();
        }

        // 3. Stream voiceover with synchronized SFX timing and mouth vowel animation
        if (ElevenLabsTTSBridge.Instance != null && !string.IsNullOrEmpty(rawDialogueWithSfx))
        {
            ElevenLabsTTSBridge.Instance.SpeakDialogueSynchronized(rawDialogueWithSfx, activeNpcId, facialRig);
        }

        // 3. Dispatch spellcraft to ModularSpellEngine if requested
        if (res.attemptedSpell != null && res.attemptedSpell.isSpell && spellEngine != null)
        {
            spellEngine.ExecuteStructuredSpell(
                res.attemptedSpell.catalyst,
                res.attemptedSpell.geometry,
                res.attemptedSpell.behavior,
                res.attemptedSpell.instability
            );
        }

        // 4. Check for Quarantine violation flag
        if (res.quarantineViolation)
        {
            TribunalAppealController.FlagPlayerForQuarantine(res.violationReason ?? "Impossible client spell execution override");
        }

        pipelineHistory.Add(new LLMChatMessage { role = "assistant", content = res.dialogueText });
        OnResponseReceived?.Invoke(cleanDialogue, res);
    }

    /// <summary>
    /// Local offline persona and spellcraft synthesis routine ensuring 100% playable functionality
    /// with zero censorship and full JSON schema compliance when offline.
    /// </summary>
    private IEnumerator GenerateLocalUnfilteredResponseRoutine(string playerInput, string activeNpcId)
    {
        yield return new WaitForSeconds(0.2f);

        UnfilteredLLMStructuredResponse response = new UnfilteredLLMStructuredResponse();
        response.attemptedSpell = new LLMSpellCraftData();

        string lower = playerInput.ToLowerInvariant();

        if (lower.StartsWith("cast ") || lower.Contains("cast spell") || lower.Contains("cast singularity") || lower.Contains("collapse singularity"))
        {
            // Player explicitly commanded dynamic spellcraft
            bool isGravitational = lower.Contains("singularity") || lower.Contains("nuke");
            response.attemptedSpell.isSpell = true;
            response.attemptedSpell.catalyst = isGravitational ? "VoidGravity" : "Blood";
            response.attemptedSpell.geometry = isGravitational ? "Singularity" : "RazorDisc";
            response.attemptedSpell.behavior = "ChainDetonation";
            response.attemptedSpell.instability = isGravitational ? 1.45f : 0.65f; // Void singularity Instability > 1.0 backfire!
            response.attemptedSpell.manaCost = 75f;

            if (isGravitational)
            {
                response.dialogueText = "Margo: 'You're actually trying to collapse Floor 5 into a black hole?! Fine by me, darling. Don't blame me when thermodynamic feedback boils your marrow.' [SFX: singularity_whir | at: 0.1s | vol: 85%] [SFX: cinematic_boom | at: 0.8s | vol: 100%]";
                response.sfxTags = "[SFX: singularity_whir | at: 0.1s | vol: 85%] [SFX: cinematic_boom | at: 0.8s | vol: 100%]";
            }
            else
            {
                response.dialogueText = "Margo: 'Casting a crimson crescent arc... Edge temper infused with kinetic siphon. Slice them into cinders.' [SFX: glitch | at: 0.2s | vol: 60%]";
                response.sfxTags = "[SFX: glitch | at: 0.2s | vol: 60%]";
            }
        }
        else if (activeNpcId == "npc_barnaby")
        {
            response.dialogueText = "Barnaby: 'Hah! Bring that Damascus blade to the hearth. Remember the Three-Strike rule, delver: two micro-fractures is a warning; the third strike blows the anvil through the ceiling.' [SFX: anvil_shatter | at: 0.3s | vol: 80%]";
            response.sfxTags = "[SFX: anvil_shatter | at: 0.3s | vol: 80%]";
        }
        else if (activeNpcId == "npc_zephyr")
        {
            response.dialogueText = "Zephyr: 'Shadows stir in the Lower Aqueducts. I will scout the vein clusters and report before the Spire shift.' [SFX: relic_chime | at: 0.1s | vol: 70%]";
            response.sfxTags = "[SFX: relic_chime | at: 0.1s | vol: 70%]";
        }
        else
        {
            response.dialogueText = "Margo: 'Keep your eyes sharp and your blade fed with biomass. If you find dark geodes, bring them to my alembic. And please, don't do anything reckless with uncalibrated singularities. (¬_¬)' [SFX: relic_chime | at: 0.1s | vol: 65%]";
            response.sfxTags = "[SFX: relic_chime | at: 0.1s | vol: 65%]";
        }

        ExecuteStructuredGameState(response, activeNpcId);
    }

    /// <summary>
    /// Step 2 of Summoning Ritual: Generates 3 esoteric personality-revealing questions tailored to the seed concept.
    /// </summary>
    public void GenerateSummoningInquisition(string seedConcept, Action<SummonInquisitionQuiz> onComplete)
    {
        StartCoroutine(GenerateSummoningInquisitionRoutine(seedConcept, onComplete));
    }

    private IEnumerator GenerateSummoningInquisitionRoutine(string seedConcept, Action<SummonInquisitionQuiz> onComplete)
    {
        if (string.IsNullOrEmpty(openRouterApiKey) || openRouterApiKey == "YOUR_OPENROUTER_KEY")
        {
            yield return new WaitForSeconds(0.2f);
            onComplete?.Invoke(CreateArchetypalInquisition(seedConcept));
            yield break;
        }

        string prompt = $"The summoner is initiating a Patronus golem ritual using the nomenclature seed '{seedConcept}'.\n" +
            "Generate exactly 3 esoteric, personality-revealing questions that probe the essence of this concept.\n" +
            "Return valid JSON formatted like:\n" +
            "{\n" +
            "  \"seedConcept\": \"" + seedConcept + "\",\n" +
            "  \"questions\": [\n" +
            "    { \"prompt\": \"...\", \"optionA\": \"...\", \"optionB\": \"...\", \"optionC\": \"...\", \"affinityA\": \"Fire\", \"affinityB\": \"Void\", \"affinityC\": \"Celestial\" },\n" +
            "    { \"prompt\": \"...\", \"optionA\": \"...\", \"optionB\": \"...\", \"optionC\": \"...\", \"affinityA\": \"Aggressive\", \"affinityB\": \"Stoic\", \"affinityC\": \"Chaotic\" },\n" +
            "    { \"prompt\": \"...\", \"optionA\": \"...\", \"optionB\": \"...\", \"optionC\": \"...\", \"affinityA\": \"SpellVamp\", \"affinityB\": \"KineticReflect\", \"affinityC\": \"GemFinder\" }\n" +
            "  ]\n" +
            "}";

        string jsonPayload = "{" +
            $"\"model\":\"{primaryModel}\"," +
            $"\"messages\":[{{\"role\":\"user\",\"content\":\"{EscapeJson(prompt)}\"}}]," +
            "\"response_format\":{\"type\":\"json_object\"}," +
            "\"temperature\":0.7" +
            "}";

        using (UnityWebRequest req = new UnityWebRequest(openRouterEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + openRouterApiKey);
            req.SetRequestHeader("HTTP-Referer", "https://spire.battlesummoner.game");
            req.SetRequestHeader("X-Title", "BattleSummoner");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    LLMResponsePayload wrapper = JsonUtility.FromJson<LLMResponsePayload>(req.downloadHandler.text);
                    if (wrapper != null && wrapper.choices != null && wrapper.choices.Count > 0)
                    {
                        SummonInquisitionQuiz parsedQuiz = JsonUtility.FromJson<SummonInquisitionQuiz>(wrapper.choices[0].message.content);
                        if (parsedQuiz != null && parsedQuiz.questions != null && parsedQuiz.questions.Count >= 3)
                        {
                            onComplete?.Invoke(parsedQuiz);
                            yield break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UnfilteredDialoguePipeline] Inquisition parsing failed: {ex.Message}");
                }
            }

            onComplete?.Invoke(CreateArchetypalInquisition(seedConcept));
        }
    }

    private SummonInquisitionQuiz CreateArchetypalInquisition(string seed)
    {
        return new SummonInquisitionQuiz
        {
            seedConcept = seed,
            questions = new List<InquisitionQuestion>
            {
                new InquisitionQuestion
                {
                    prompt = $"When the astral Spire descends into entropic midnight, how does {seed} sustain its ethereal flame?",
                    optionA = "By burning through the crucible of righteous infernos (Fire).",
                    optionB = "By meditating within the silent gravitational abyss (Void).",
                    optionC = "By channeling the celestial light of fallen ascendants (Celestial).",
                    affinityA = "Fire",
                    affinityB = "Void",
                    affinityC = "Celestial"
                },
                new InquisitionQuestion
                {
                    prompt = $"A Spire Colossus rears up to crush your summoner. What instinct drives {seed}?",
                    optionA = "Unbridled kinetic savagery, severing the titan's sinews (Aggressive).",
                    optionB = "Unyielding shield formation, planting bedrock into the earth (Stoic).",
                    optionC = "Erratic phasing, confusing the titan with spectral echoes (Chaotic).",
                    affinityA = "Aggressive",
                    affinityB = "Stoic",
                    affinityC = "Chaotic"
                },
                new InquisitionQuestion
                {
                    prompt = $"What ancient boon does {seed} weave into its astral matrix?",
                    optionA = "Parasitic life siphon, leeching blood from every strike (SpellVamp).",
                    optionB = "Kinetic deflection, ricocheting shockwaves back at foes (KineticReflect).",
                    optionC = "Geode resonance, unearthing hidden subterranean mana cores (GemFinder).",
                    affinityA = "SpellVamp",
                    affinityB = "KineticReflect",
                    affinityC = "GemFinder"
                }
            }
        };
    }

    /// <summary>
    /// Thread-safe entry point to dispatch streamed or background HTTP payloads from OpenRouter
    /// back onto Unity's main thread using UnityThreadDispatcher.
    /// </summary>
    public void DispatchAsyncPayload(string jsonPayload, Action<UnfilteredLLMStructuredResponse> callback)
    {
        BattleSummoner.Core.UnityThreadDispatcher.Enqueue(() =>
        {
            try
            {
                UnfilteredLLMStructuredResponse resp = JsonUtility.FromJson<UnfilteredLLMStructuredResponse>(jsonPayload);
                if (resp != null)
                {
                    ExecuteStructuredGameState(resp, "async_pipeline");
                    callback?.Invoke(resp);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnfilteredDialoguePipeline] Failed to parse dispatched async payload: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Asynchronously generates a dialogue line and embedded cues from an enhanced prompt.
    /// </summary>
    public async System.Threading.Tasks.Task<string> GenerateDialogueAndCueStreamAsync(string enhancedPrompt, string playerUtterance)
    {
        await System.Threading.Tasks.Task.Yield();
        string emotionTag = "[EMOTION: Happy]";
        string lowerPrompt = (enhancedPrompt + " " + playerUtterance).ToLowerInvariant();
        if (lowerPrompt.Contains("angry") || lowerPrompt.Contains("danger") || lowerPrompt.Contains("attack")) emotionTag = "[EMOTION: Angry]";
        else if (lowerPrompt.Contains("sad") || lowerPrompt.Contains("loss") || lowerPrompt.Contains("hurt")) emotionTag = "[EMOTION: Sad]";
        else if (lowerPrompt.Contains("surprised") || lowerPrompt.Contains("what") || lowerPrompt.Contains("behold")) emotionTag = "[EMOTION: Surprised]";
        else if (lowerPrompt.Contains("smug") || lowerPrompt.Contains("heh") || lowerPrompt.Contains("triumph")) emotionTag = "[EMOTION: Smug]";
        else if (lowerPrompt.Contains("love") || lowerPrompt.Contains("adore") || lowerPrompt.Contains("heart")) emotionTag = "[EMOTION: HeartEyesBlush]";
        else if (lowerPrompt.Contains("overload") || lowerPrompt.Contains("crazy") || lowerPrompt.Contains("power")) emotionTag = "[EMOTION: CrazyOverload]";
        else if (lowerPrompt.Contains("pain") || lowerPrompt.Contains("hit") || lowerPrompt.Contains("ouch")) emotionTag = "[EMOTION: PainHit]";

        return $"{emotionTag} [SFX: relic_chime | at: 0.1s | vol: 85%] The astral matrix resonates with your command. Stand ready, Summoner!";
    }

    /// <summary>
    /// Streams voice audio directly into the specified target vocal AudioSource.
    /// </summary>
    public async System.Threading.Tasks.Task StreamVoicePlaybackAsync(string cleanSpokenText, string elevenLabsVoiceId, AudioSource primaryVocalAudioSource)
    {
        await System.Threading.Tasks.Task.Yield();
        if (primaryVocalAudioSource == null) return;

        if (ttsBridge != null)
        {
            ttsBridge.SpeakDialogue(cleanSpokenText);
        }
        else
        {
            if (primaryVocalAudioSource.clip == null)
            {
                int sampleRate = 44100;
                float duration = Mathf.Clamp(cleanSpokenText.Length * 0.06f, 0.8f, 3.5f);
                int totalSamples = (int)(sampleRate * duration);
                float[] samples = new float[totalSamples];
                for (int i = 0; i < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Clamp01(t * 15f) * Mathf.Clamp01((duration - t) * 10f);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 220f * t) * envelope * 0.25f;
                }
                var clip = AudioClip.Create("ProceduralVocalStream", totalSamples, 1, sampleRate, false);
                clip.SetData(samples, 0);
                primaryVocalAudioSource.clip = clip;
            }
            primaryVocalAudioSource.Play();
        }
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}

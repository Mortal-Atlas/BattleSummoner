using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class LLMChatMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class LLMRequestPayload
{
    public string model;
    public List<LLMChatMessage> messages;
    public float temperature;
}

[System.Serializable]
public class LLMResponseChoice
{
    public LLMChatMessage message;
}

[System.Serializable]
public class LLMResponsePayload
{
    public List<LLMResponseChoice> choices;
}

public class NPCConversationAgent : MonoBehaviour
{
    [Header("Active NPC Config")]
    public BespokeNPCObject npcProfile;
    [SerializeField] private string llmEndpoint = "https://api.together.xyz/v1/chat/completions";
    [SerializeField] private string apiKey = "YOUR_INFERENCE_API_KEY";
    [SerializeField] private string targetModel = "mistralai/Mistral-7B-Instruct-v0.3";

    [Header("Speech & Rigging")]
    [SerializeField] private bool autoSpeakDialogue = true;
    [SerializeField] private BattleSummoner.Customization.FacialExpressionController facialController;

    private readonly List<LLMChatMessage> conversationMemory = new List<LLMChatMessage>();

    public event Action<string> OnDialogueReceived;
    public event Action<string> OnDialogueFailed;

    private void Awake()
    {
        if (facialController == null)
        {
            facialController = GetComponentInChildren<BattleSummoner.Customization.FacialExpressionController>();
        }
    }

    private void Start()
    {
        InitializeMemory();
    }

    public void InitializeMemory()
    {
        conversationMemory.Clear();
        if (npcProfile != null)
        {
            conversationMemory.Add(new LLMChatMessage
            {
                role = "system",
                content = npcProfile.systemPromptPersona + 
                          $"\nPlayer Affinity Level: {npcProfile.affinityLevel}/{npcProfile.maxAffinity}." +
                          "\nFormat response in RFC-8259 JSON or natural speech with optional VoxCraft sound effect cues like [SFX: preset | at: Xs | vol: Y%]."
            });
        }
    }

    public void SendUserUtterance(string userText)
    {
        conversationMemory.Add(new LLMChatMessage { role = "user", content = userText });

        // If default dummy key is present, provide instant personality dialogue without blocking
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_INFERENCE_API_KEY")
        {
            StartCoroutine(GenerateLocalPersonaReplyRoutine(userText));
            return;
        }

        StartCoroutine(PostInferenceRoutine());
    }

    private IEnumerator GenerateLocalPersonaReplyRoutine(string userText)
    {
        yield return new WaitForSeconds(0.35f);
        string reply = GetSimulatedPersonaReply(userText);
        DispatchDialogueReply(reply);
    }

    private string GetSimulatedPersonaReply(string userText)
    {
        if (npcProfile == null) return "The figure nods quietly.";

        switch (npcProfile.npcId)
        {
            case "npc_margo":
                return "Oh, look who stumbled out of the abyss... [SFX: relic_chime | at: 0.2s | vol: 60%] Did you bring any shiny dark geodes, or just more mundane scrap? (¬_¬) Don't break my distillation coils.";
            case "npc_barnaby":
                return "Ho there, Wanderer! Look at that edge temper! [SFX: anvil_shatter | at: 0.4s | vol: 80%] Three strikes on the anvil is all the cosmos grants before fracture debt consumes the blade. Tread carefully!";
            case "npc_zephyr":
                return "The wind whispers of unrest on Floor 50. [SFX: whoosh | at: 0.3s | vol: 65%] Dispatch me on the low caverns—I will gather Azurite and strike true before the shadows lengthen.";
            default:
                return $"{npcProfile.displayName}: The Spire never yields its glyphs without blood and discipline. [SFX: chime | at: 0.2s | vol: 50%]";
        }
    }

    private void DispatchDialogueReply(string reply)
    {
        conversationMemory.Add(new LLMChatMessage { role = "assistant", content = reply });
        OnDialogueReceived?.Invoke(reply);

        if (autoSpeakDialogue && ElevenLabsTTSBridge.Instance != null && npcProfile != null)
        {
            ElevenLabsTTSBridge.Instance.SpeakDialogueSynchronized(reply, npcProfile.npcId, facialController);
        }
    }

    private IEnumerator PostInferenceRoutine()
    {
        LLMRequestPayload payload = new LLMRequestPayload
        {
            model = targetModel,
            messages = conversationMemory,
            temperature = 0.7f
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(llmEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                LLMResponsePayload response = JsonUtility.FromJson<LLMResponsePayload>(responseText);

                if (response != null && response.choices != null && response.choices.Count > 0)
                {
                    string reply = response.choices[0].message.content;
                    conversationMemory.Add(new LLMChatMessage { role = "assistant", content = reply });
                    OnDialogueReceived?.Invoke(reply);
                    yield break;
                }
            }

            // Fallback to local simulated persona on network lag
            string fallback = GetSimulatedPersonaReply("delve");
            conversationMemory.Add(new LLMChatMessage { role = "assistant", content = fallback });
            OnDialogueReceived?.Invoke(fallback);
        }
    }
}

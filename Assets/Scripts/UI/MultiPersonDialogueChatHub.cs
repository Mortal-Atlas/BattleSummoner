using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.UI
{
    [System.Serializable]
    public class ChatBubbleData
    {
        public string speakerId;      // "alex", "elena", "player", "coop"
        public string speakerDisplayName;
        public string messageText;
        public bool isRightAligned;
        public Sprite portraitBadge;
        public Color bubbleColor;
        public Color headerColor;
    }

    /// <summary>
    /// MultiPersonDialogueChatHub provides the visual novel multi-person dialogue hub
    /// featuring dual anime character busts (Alex & Elena), anchored alternating speech bubbles,
    /// a bottom text chat field with Divinatus-style TTS read-aloud for talking to NPCs & co-op players,
    /// and conversation controls (SKIP, AUTO, LOG, PAUSE).
    /// </summary>
    public class MultiPersonDialogueChatHub : MonoBehaviour
    {
        public static MultiPersonDialogueChatHub Instance { get; private set; }

        [Header("Root & Overlay Canvas")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button backToVNButton;

        [Header("Character Busts")]
        [SerializeField] private Image bustAlexImage;
        [SerializeField] private Image bustElenaImage;
        [SerializeField] private Sprite alexBustSprite;
        [SerializeField] private Sprite elenaBustSprite;

        [Header("Portrait Badges")]
        [SerializeField] private Sprite alexBadgeSprite;
        [SerializeField] private Sprite elenaBadgeSprite;
        [SerializeField] private Sprite playerBadgeSprite;

        [Header("Dialogue Flow & Message Container")]
        [SerializeField] private Transform messageContainer;
        [SerializeField] private ScrollRect dialogueScrollRect;
        [SerializeField] private GameObject bubblePrefab;

        [Header("Dialogue Controls")]
        [SerializeField] private Button skipButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private TextMeshProUGUI autoButtonText;
        [SerializeField] private Button logButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI pauseButtonText;

        [Header("Bottom Text Chat Bar")]
        [SerializeField] private Button plusActionButton;
        [SerializeField] private TMP_InputField messageInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private GameObject quickChipsPanel;
        [SerializeField] private Button quickPromptBtn1;
        [SerializeField] private Button quickPromptBtn2;
        [SerializeField] private Button quickPromptBtn3;

        [Header("Transcript Log Modal")]
        [SerializeField] private GameObject transcriptModal;
        [SerializeField] private TextMeshProUGUI transcriptBodyText;
        [SerializeField] private Button closeTranscriptButton;

        // Visual Theme Palette
        private readonly Color colAlexBubble = new Color(0.18f, 0.17f, 0.17f, 0.94f); // Slate-charcoal
        private readonly Color colElenaBubble = new Color(0.14f, 0.22f, 0.15f, 0.94f); // Moss-green
        private readonly Color colPlayerBubble = new Color(0.12f, 0.18f, 0.25f, 0.94f); // Twilight-blue
        private readonly Color colCoopBubble = new Color(0.24f, 0.14f, 0.28f, 0.94f); // Amethyst-purple
        private readonly Color colGoldHeader = new Color(0.92f, 0.78f, 0.38f); // Gold title

        private readonly List<ChatBubbleData> messageHistory = new List<ChatBubbleData>();
        private readonly List<GameObject> activeBubbleObjects = new List<GameObject>();

        private bool isAutoPlaying = false;
        private bool isPaused = false;
        private Coroutine activeBanterRoutine;
        private Coroutine activeDialogueVoiceRoutine;
        private string activeSpeakerId = "";

        public bool IsOverlayActive => overlayRoot != null && overlayRoot.activeSelf;

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

            LoadAssetsIfMissing();
            SetupButtonListeners();
        }

        private void Start()
        {
            // Populate initial dialogue conversation from reference
            InitializeReferenceConversation();
        }

        private void LoadAssetsIfMissing()
        {
            if (alexBadgeSprite == null) alexBadgeSprite = Resources.Load<Sprite>("UI/Dialogue/Crop_Alex_Badge") ?? LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dialogue/Crop_Alex_Badge.png");
            if (elenaBadgeSprite == null) elenaBadgeSprite = Resources.Load<Sprite>("UI/Dialogue/Crop_Elena_Badge") ?? LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dialogue/Crop_Elena_Badge.png");
            if (alexBustSprite == null) alexBustSprite = Resources.Load<Sprite>("UI/Dialogue/Crop_Alex_Bust") ?? LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dialogue/Crop_Alex_Bust.png");
            if (elenaBustSprite == null) elenaBustSprite = Resources.Load<Sprite>("UI/Dialogue/Crop_Elena_Bust") ?? LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dialogue/Crop_Elena_Bust.png");
            if (playerBadgeSprite == null) playerBadgeSprite = Resources.Load<Sprite>("UI/Portraits/Portrait_Protagonist") ?? LoadAssetAtPath<Sprite>("Assets/Textures/UI/Portraits/Portrait_Protagonist.png");

            if (bustAlexImage != null && alexBustSprite != null) bustAlexImage.sprite = alexBustSprite;
            if (bustElenaImage != null && elenaBustSprite != null) bustElenaImage.sprite = elenaBustSprite;
        }

        private T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }

        private void SetupButtonListeners()
        {
            if (sendButton != null)
            {
                sendButton.onClick.RemoveAllListeners();
                sendButton.onClick.AddListener(OnSendButtonClicked);
            }

            if (messageInputField != null)
            {
                messageInputField.onSubmit.RemoveAllListeners();
                messageInputField.onSubmit.AddListener(text => OnSendButtonClicked());
            }

            if (plusActionButton != null)
            {
                plusActionButton.onClick.RemoveAllListeners();
                plusActionButton.onClick.AddListener(ToggleQuickChips);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            if (autoButton != null)
            {
                autoButton.onClick.RemoveAllListeners();
                autoButton.onClick.AddListener(ToggleAutoPlay);
            }

            if (logButton != null)
            {
                logButton.onClick.RemoveAllListeners();
                logButton.onClick.AddListener(OpenTranscriptLog);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(TogglePause);
            }

            if (backToVNButton != null)
            {
                backToVNButton.onClick.RemoveAllListeners();
                backToVNButton.onClick.AddListener(() => SetVisible(false));
            }

            if (closeTranscriptButton != null)
            {
                closeTranscriptButton.onClick.RemoveAllListeners();
                closeTranscriptButton.onClick.AddListener(() => {
                    if (transcriptModal != null) transcriptModal.SetActive(false);
                });
            }

            // Quick chips setup
            if (quickPromptBtn1 != null) quickPromptBtn1.onClick.AddListener(() => SendQuickPrompt("The guards at the gate look restless. What do you notice?"));
            if (quickPromptBtn2 != null) quickPromptBtn2.onClick.AddListener(() => SendQuickPrompt("Should we prepare for the Spire floor boss?"));
            if (quickPromptBtn3 != null) quickPromptBtn3.onClick.AddListener(() => SendQuickPrompt("Elena, let's keep our distance for now."));
        }

        public void SetVisible(bool visible)
        {
            if (overlayRoot != null) overlayRoot.SetActive(visible);
            if (!visible)
            {
                if (ElevenLabsTTSBridge.Instance != null)
                {
                    ElevenLabsTTSBridge.Instance.StopSpeaking();
                }
            }
        }

        public void InitializeReferenceConversation()
        {
            if (messageHistory.Count > 0) return;

            // Seed initial conversation matching user screenshot:
            // 1. Alex: "The energy in this town feels restless... Is something happening beyond the spires?"
            AddBubble(new ChatBubbleData
            {
                speakerId = "alex",
                speakerDisplayName = "ALEX",
                messageText = "The energy in this town feels restless... Is something happening beyond the spires?",
                isRightAligned = false,
                portraitBadge = alexBadgeSprite,
                bubbleColor = colAlexBubble,
                headerColor = colGoldHeader
            }, readAloud: false);

            // 2. Elena: "I feel it too. The guards at the gate seem on edge. Let's keep our distance for now."
            AddBubble(new ChatBubbleData
            {
                speakerId = "elena",
                speakerDisplayName = "ELENA",
                messageText = "I feel it too. The guards at the gate seem on edge. Let's keep our distance for now.",
                isRightAligned = true,
                portraitBadge = elenaBadgeSprite,
                bubbleColor = colElenaBubble,
                headerColor = colGoldHeader
            }, readAloud: false);
        }

        private void OnSendButtonClicked()
        {
            if (messageInputField == null) return;
            string userText = messageInputField.text?.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            messageInputField.text = "";
            messageInputField.ActivateInputField();

            if (quickChipsPanel != null) quickChipsPanel.SetActive(false);

            // 1. Append Player Speech Bubble
            ChatBubbleData playerBubble = new ChatBubbleData
            {
                speakerId = "player",
                speakerDisplayName = "YOU",
                messageText = userText,
                isRightAligned = true,
                portraitBadge = playerBadgeSprite,
                bubbleColor = colPlayerBubble,
                headerColor = colGoldHeader
            };

            AddBubble(playerBubble, readAloud: true);

            // 2. Trigger Companion dynamic NPC reaction
            StartCoroutine(RoutineCompanionResponseSequence(userText));
        }

        private void SendQuickPrompt(string promptText)
        {
            if (messageInputField != null)
            {
                messageInputField.text = promptText;
                OnSendButtonClicked();
            }
        }

        private void ToggleQuickChips()
        {
            if (quickChipsPanel != null)
            {
                quickChipsPanel.SetActive(!quickChipsPanel.activeSelf);
            }
        }

        /// <summary>
        /// Instantiates a styled speech bubble matching the visual novel mockup:
        /// - Angular portrait badge
        /// - Gold speaker nameplate
        /// - Sleek tinted container
        /// - Divinatus TTS read-aloud
        /// </summary>
        public void AddBubble(ChatBubbleData data, bool readAloud = true)
        {
            messageHistory.Add(data);

            if (messageContainer == null) return;

            GameObject bubbleGo = CreateBubbleUIObject(data);
            activeBubbleObjects.Add(bubbleGo);

            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (dialogueScrollRect != null)
            {
                dialogueScrollRect.normalizedPosition = new Vector2(0f, 0f);
            }

            HighlightSpeakerBust(data.speakerId);

            // Read aloud like Divinatus via ElevenLabsTTSBridge
            if (readAloud && !isPaused)
            {
                ReadAloudUtterance(data.messageText, data.speakerId);
            }
        }

        private void ReadAloudUtterance(string text, string speakerId)
        {
            if (ElevenLabsTTSBridge.Instance == null) return;

            activeSpeakerId = speakerId;
            ElevenLabsTTSBridge.Instance.SpeakDialogueSynchronized(
                text,
                speakerId,
                facialRig: null,
                onPlaybackStarted: () => HighlightSpeakerBust(speakerId),
                onPlaybackCompleted: () => {
                    if (isAutoPlaying && !isPaused)
                    {
                        TriggerNextAutoTopic();
                    }
                }
            );
        }

        private void HighlightSpeakerBust(string speakerId)
        {
            float dimAlpha = 0.65f;
            float brightAlpha = 1.0f;

            if (bustAlexImage != null)
            {
                bool isAlex = speakerId.Equals("alex", StringComparison.OrdinalIgnoreCase);
                bustAlexImage.color = new Color(1f, 1f, 1f, isAlex ? brightAlpha : dimAlpha);
                bustAlexImage.transform.localScale = isAlex ? Vector3.one * 1.03f : Vector3.one;
            }

            if (bustElenaImage != null)
            {
                bool isElena = speakerId.Equals("elena", StringComparison.OrdinalIgnoreCase);
                bustElenaImage.color = new Color(1f, 1f, 1f, isElena ? brightAlpha : dimAlpha);
                bustElenaImage.transform.localScale = isElena ? Vector3.one * 1.03f : Vector3.one;
            }
        }

        private GameObject CreateBubbleUIObject(ChatBubbleData data)
        {
            // Bubble Row Root
            GameObject rowObj = new GameObject($"Bubble_{data.speakerDisplayName}_{messageHistory.Count}", typeof(RectTransform));
            rowObj.transform.SetParent(messageContainer, false);
            var rowRt = rowObj.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(380f, 110f);

            // Anchoring: Left vs Right
            float anchorX = data.isRightAligned ? 0.95f : 0.05f;
            rowRt.pivot = new Vector2(data.isRightAligned ? 1f : 0f, 0.5f);

            // Bubble background box
            GameObject boxObj = new GameObject("BubbleBox", typeof(RectTransform), typeof(Image));
            boxObj.transform.SetParent(rowObj.transform, false);
            var boxRt = boxObj.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(data.isRightAligned ? 0.15f : 0f, 0f);
            boxRt.anchorMax = new Vector2(data.isRightAligned ? 1f : 0.85f, 1f);
            boxRt.offsetMin = new Vector2(data.isRightAligned ? 0f : 75f, 0f);
            boxRt.offsetMax = new Vector2(data.isRightAligned ? -75f : 0f, 0f);

            var boxImg = boxObj.GetComponent<Image>();
            boxImg.color = data.bubbleColor;

            // Speaker Name Header (Gold)
            GameObject headerObj = new GameObject("HeaderTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(boxObj.transform, false);
            var hrt = headerObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.08f, 0.72f);
            hrt.anchorMax = new Vector2(0.92f, 0.96f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;

            var htmp = headerObj.GetComponent<TextMeshProUGUI>();
            htmp.text = $"<b>{data.speakerDisplayName.ToUpper()}</b>";
            htmp.fontSize = 17;
            htmp.fontStyle = FontStyles.Bold;
            htmp.color = data.headerColor;
            htmp.alignment = data.isRightAligned ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;

            // Message text body
            GameObject textObj = new GameObject("MessageTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(boxObj.transform, false);
            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.08f, 0.08f);
            trt.anchorMax = new Vector2(0.92f, 0.72f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var ttmp = textObj.GetComponent<TextMeshProUGUI>();
            ttmp.text = data.messageText;
            ttmp.fontSize = 15;
            ttmp.color = new Color(0.95f, 0.95f, 0.92f);
            ttmp.enableWordWrapping = true;
            ttmp.alignment = data.isRightAligned ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

            // Portrait Badge (Angular persona badge)
            GameObject badgeObj = new GameObject("PortraitBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(rowObj.transform, false);
            var brt = badgeObj.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(65f, 65f);
            if (data.isRightAligned)
            {
                brt.anchorMin = new Vector2(1f, 0.5f);
                brt.anchorMax = new Vector2(1f, 0.5f);
                brt.pivot = new Vector2(1f, 0.5f);
                brt.anchoredPosition = new Vector2(0f, 0f);
            }
            else
            {
                brt.anchorMin = new Vector2(0f, 0.5f);
                brt.anchorMax = new Vector2(0f, 0.5f);
                brt.pivot = new Vector2(0f, 0.5f);
                brt.anchoredPosition = new Vector2(0f, 0f);
            }

            var bImg = badgeObj.GetComponent<Image>();
            if (data.portraitBadge != null)
            {
                bImg.sprite = data.portraitBadge;
                bImg.preserveAspect = true;
            }
            else
            {
                bImg.color = colGoldHeader;
            }

            return rowObj;
        }

        private IEnumerator RoutineCompanionResponseSequence(string userText)
        {
            yield return new WaitForSeconds(0.6f);

            string lower = userText.ToLowerInvariant();

            // Determine if Elena or Alex responds first
            bool elenaFirst = lower.Contains("elena") || lower.Contains("guard") || lower.Contains("town") || lower.Contains("distance");
            string firstSpeaker = elenaFirst ? "elena" : "alex";
            string secondSpeaker = elenaFirst ? "alex" : "elena";

            // Generate contextual responses
            string reply1 = GenerateNPCReply(firstSpeaker, userText);
            string reply2 = GenerateNPCReply(secondSpeaker, reply1);

            // Primary response
            AddBubble(new ChatBubbleData
            {
                speakerId = firstSpeaker,
                speakerDisplayName = firstSpeaker.ToUpper(),
                messageText = reply1,
                isRightAligned = (firstSpeaker == "elena"),
                portraitBadge = (firstSpeaker == "elena") ? elenaBadgeSprite : alexBadgeSprite,
                bubbleColor = (firstSpeaker == "elena") ? colElenaBubble : colAlexBubble,
                headerColor = colGoldHeader
            }, readAloud: true);

            yield return new WaitForSeconds(2.8f);

            // Secondary chime-in
            AddBubble(new ChatBubbleData
            {
                speakerId = secondSpeaker,
                speakerDisplayName = secondSpeaker.ToUpper(),
                messageText = reply2,
                isRightAligned = (secondSpeaker == "elena"),
                portraitBadge = (secondSpeaker == "elena") ? elenaBadgeSprite : alexBadgeSprite,
                bubbleColor = (secondSpeaker == "elena") ? colElenaBubble : colAlexBubble,
                headerColor = colGoldHeader
            }, readAloud: true);
        }

        private string GenerateNPCReply(string npcId, string context)
        {
            string lower = context.ToLowerInvariant();

            if (npcId == "alex")
            {
                if (lower.Contains("boss") || lower.Contains("fight") || lower.Contains("spire"))
                    return "Keep your blade honed. When an Apex falls, we'll carve its horn before the glyphs destabilize.";
                if (lower.Contains("guard") || lower.Contains("gate") || lower.Contains("town"))
                    return "The town militia is gripping their spears too tightly. Someone stirred up the lower catacombs.";
                if (lower.Contains("loot") || lower.Contains("item") || lower.Contains("carve"))
                    return "Small fiends drop their essence immediately, but true bosses demand proper carving patience.";
                return "I'm with you. Just say the word and we push through the gateway.";
            }
            else // Elena
            {
                if (lower.Contains("boss") || lower.Contains("fight") || lower.Contains("spire"))
                    return "Watch for their kinetic sweep attacks. And remember—look for shiny geodes scattered across the floor!";
                if (lower.Contains("guard") || lower.Contains("gate") || lower.Contains("town"))
                    return "I've been monitoring the aether frequencies. The guards aren't just scared—they're under an influence.";
                if (lower.Contains("loot") || lower.Contains("item") || lower.Contains("carve"))
                    return "Guild masters pay top gold for rare apex carapaces. Make sure you don't leave the carcass uncarved.";
                return "Agreed. Let's stay alert and gather our resources first.";
            }
        }

        #region Dialogue Controls
        private void OnSkipClicked()
        {
            if (ElevenLabsTTSBridge.Instance != null)
            {
                ElevenLabsTTSBridge.Instance.StopSpeaking();
            }
            HighlightSpeakerBust("");
        }

        private void ToggleAutoPlay()
        {
            isAutoPlaying = !isAutoPlaying;
            if (autoButtonText != null)
            {
                autoButtonText.text = isAutoPlaying ? "<color=#FFDD44>AUTO ON</color>" : "AUTO";
            }

            if (isAutoPlaying)
            {
                TriggerNextAutoTopic();
            }
        }

        private void TriggerNextAutoTopic()
        {
            if (!isAutoPlaying || isPaused) return;

            if (activeBanterRoutine != null) StopCoroutine(activeBanterRoutine);
            activeBanterRoutine = StartCoroutine(RoutineAutoBanter());
        }

        private IEnumerator RoutineAutoBanter()
        {
            yield return new WaitForSeconds(2.0f);
            if (!isAutoPlaying || isPaused) yield break;

            string[] banterPrompts = {
                "The winds coming from the higher floors feel cold.",
                "Did you see the merchant's latest stock of draughts?",
                "They say the hundredth floor holds the Spire's beating heart."
            };

            string pick = banterPrompts[UnityEngine.Random.Range(0, banterPrompts.Length)];
            AddBubble(new ChatBubbleData
            {
                speakerId = "alex",
                speakerDisplayName = "ALEX",
                messageText = pick,
                isRightAligned = false,
                portraitBadge = alexBadgeSprite,
                bubbleColor = colAlexBubble,
                headerColor = colGoldHeader
            }, readAloud: true);
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            if (pauseButtonText != null)
            {
                pauseButtonText.text = isPaused ? "<color=#FF5555>RESUME</color>" : "PAUSE";
            }

            if (isPaused)
            {
                if (ElevenLabsTTSBridge.Instance != null)
                {
                    ElevenLabsTTSBridge.Instance.StopSpeaking();
                }
            }
        }

        private void OpenTranscriptLog()
        {
            if (transcriptModal == null) return;
            transcriptModal.SetActive(true);

            if (transcriptBodyText != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var b in messageHistory)
                {
                    string colorHex = ColorUtility.ToHtmlStringRGB(b.headerColor);
                    sb.AppendLine($"<color=#{colorHex}><b>[{b.speakerDisplayName}]</b></color>: {b.messageText}\n");
                }
                transcriptBodyText.text = sb.ToString();
            }
        }
        #endregion
    }
}

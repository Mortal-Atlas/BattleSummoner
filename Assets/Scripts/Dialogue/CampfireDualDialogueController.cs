using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Dialogue
{
    /// <summary>
    /// CampfireDualDialogueController manages the 1-on-1 dialogue encounter at the campfire:
    /// - Displays two full-body 3/4th angle anime-stylized characters facing each other (Persona 4 / Genshin style).
    /// - Plain naked base layers layered with modular clothes, armor, and expressions.
    /// - Authentic Persona 4 dialogue box (angled yellow nameplate, warm dark frame, concentric indicator).
    /// - Speaker focus highlighting, typewriter text, choices, and interactive chat input.
    /// - Smooth return to campfire panoramic look-around.
    /// </summary>
    public class CampfireDualDialogueController : MonoBehaviour
    {
        public static CampfireDualDialogueController Instance { get; private set; }

        [Header("Root Canvas")]
        [SerializeField] private GameObject dialogueRootContainer;
        [SerializeField] private Canvas dialogueCanvas;

        [Header("Left Character (Player - Facing Right)")]
        [SerializeField] private RectTransform leftCharacterRoot;
        [SerializeField] private Image leftBaseImage;
        [SerializeField] private Image leftOutfitImage;
        [SerializeField] private CanvasGroup leftCanvasGroup;

        [Header("Right Character (Companion - Facing Left)")]
        [SerializeField] private RectTransform rightCharacterRoot;
        [SerializeField] private Image rightBaseImage;
        [SerializeField] private Image rightOutfitImage;
        [SerializeField] private CanvasGroup rightCanvasGroup;

        [Header("Persona 4 Dialogue Box UI")]
        [SerializeField] private RectTransform dialogueBoxRect;
        [SerializeField] private Image dialogueBoxFrameImage;
        [SerializeField] private TextMeshProUGUI speakerNameTMP;
        [SerializeField] private TextMeshProUGUI dialogueBodyTMP;
        [SerializeField] private Image promptIndicatorIcon;
        [SerializeField] private GameObject timeBadgeObj;
        [SerializeField] private TextMeshProUGUI timeBadgeTMP;

        [Header("Interactive Response & Input Controls")]
        [SerializeField] private GameObject choicesContainer;
        [SerializeField] private Button choiceButton1;
        [SerializeField] private TextMeshProUGUI choiceText1;
        [SerializeField] private Button choiceButton2;
        [SerializeField] private TextMeshProUGUI choiceText2;
        [SerializeField] private Button choiceButton3;
        [SerializeField] private TextMeshProUGUI choiceText3;
        [SerializeField] private Button leaveButton;

        [SerializeField] private TMP_InputField customChatInput;
        [SerializeField] private Button sendChatButton;

        [Header("Sprites")]
        private Sprite baseFemaleLeft;
        private Sprite baseFemaleRight;
        private Sprite baseMaleRight;
        private Sprite baseMaleLeft;
        private Sprite outfitFemaleLeft;
        private Sprite outfitFemaleRight;
        private Sprite outfitMaleRight;
        private Sprite outfitMaleLeft;
        private Sprite p4FrameSprite;
        private Sprite p4IndicatorSprite;
        private Sprite p4TimeBadgeSprite;

        // Runtime State
        private bool isDialogueActive = false;
        private string activeNpcId = "";
        private string activeDisplayName = "";
        private Coroutine activeTypewriterRoutine;
        private bool isTypewriterFinished = false;
        private string currentFullDialogue = "";

        public bool IsDialogueActive => isDialogueActive;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            LoadSprites();
            EnsureUIConstructed();
        }

        private void Start()
        {
            if (dialogueRootContainer != null) dialogueRootContainer.SetActive(false);
            BindEvents();
        }

        private void Update()
        {
            if (!isDialogueActive) return;

            // Idle breathing bob for both characters
            float breath = Mathf.Sin(Time.time * 2.2f) * 4f;
            if (leftCharacterRoot != null)
            {
                leftCharacterRoot.anchoredPosition = new Vector2(leftCharacterRoot.anchoredPosition.x, breath);
            }
            if (rightCharacterRoot != null)
            {
                rightCharacterRoot.anchoredPosition = new Vector2(rightCharacterRoot.anchoredPosition.x, -breath * 0.8f);
            }

            // Indicator blinking/pulse
            if (promptIndicatorIcon != null && promptIndicatorIcon.gameObject.activeSelf)
            {
                float alpha = 0.5f + Mathf.PingPong(Time.time * 2.8f, 0.5f);
                promptIndicatorIcon.color = new Color(1f, 0.85f, 0.2f, alpha);
                float sc = 1.0f + Mathf.Sin(Time.time * 5f) * 0.08f;
                promptIndicatorIcon.transform.localScale = new Vector3(sc, sc, 1f);
            }

            // Space or Click to advance / fast-forward typewriter
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                if (!isTypewriterFinished && activeTypewriterRoutine != null)
                {
                    // Fast-forward
                    StopCoroutine(activeTypewriterRoutine);
                    if (dialogueBodyTMP != null) dialogueBodyTMP.text = currentFullDialogue;
                    isTypewriterFinished = true;
                    if (promptIndicatorIcon != null) promptIndicatorIcon.gameObject.SetActive(true);
                    if (choicesContainer != null) choicesContainer.SetActive(true);
                }
            }

            // ESC to leave dialogue
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EndDialogue();
            }
        }

        private void LoadSprites()
        {
            string path = "Assets/Textures/UI/CampfireDialogue/";
            baseFemaleLeft = LoadSprite($"{path}Base_Female_34.png");
            baseFemaleRight = LoadSprite($"{path}Base_Female_34_FacingRight.png");
            baseMaleRight = LoadSprite($"{path}Base_Male_34.png");
            baseMaleLeft = LoadSprite($"{path}Base_Male_34_FacingLeft.png");

            outfitFemaleLeft = LoadSprite($"{path}Outfit_Female_Adventurer.png");
            outfitFemaleRight = LoadSprite($"{path}Outfit_Female_Adventurer_FacingRight.png");
            outfitMaleRight = LoadSprite($"{path}Outfit_Male_Blazer.png");
            outfitMaleLeft = LoadSprite($"{path}Outfit_Male_Blazer_FacingLeft.png");

            p4FrameSprite = LoadSprite($"{path}Persona4_DialogueBox_Frame.png");
            p4IndicatorSprite = LoadSprite($"{path}Persona4_Indicator_Icon.png");
            p4TimeBadgeSprite = LoadSprite($"{path}Persona4_TimeBadge.png");
        }

        private Sprite LoadSprite(string assetPath)
        {
#if UNITY_EDITOR
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
#endif
            return null;
        }

        private void BindEvents()
        {
            if (choiceButton1 != null) choiceButton1.onClick.AddListener(() => OnChoiceSelected(1));
            if (choiceButton2 != null) choiceButton2.onClick.AddListener(() => OnChoiceSelected(2));
            if (choiceButton3 != null) choiceButton3.onClick.AddListener(() => OnChoiceSelected(3));
            if (leaveButton != null) leaveButton.onClick.AddListener(EndDialogue);

            if (sendChatButton != null) sendChatButton.onClick.AddListener(SubmitCustomChat);
            if (customChatInput != null) customChatInput.onSubmit.AddListener((_) => SubmitCustomChat());
        }

        /// <summary>
        /// Starts direct 1-on-1 Persona 4 dialogue with the specified campfire companion.
        /// </summary>
        public void StartDialogueWithCharacter(string npcId, string displayName)
        {
            activeNpcId = npcId;
            activeDisplayName = displayName;
            isDialogueActive = true;

            if (dialogueRootContainer != null) dialogueRootContainer.SetActive(true);

            // 1. Setup Left Character (Player - Facing Right)
            // Left character is Player: Male Blazer or Female Adventurer
            if (leftBaseImage != null)
            {
                leftBaseImage.sprite = baseMaleRight ?? baseFemaleRight;
                leftBaseImage.enabled = (leftBaseImage.sprite != null);
            }
            if (leftOutfitImage != null)
            {
                leftOutfitImage.sprite = outfitMaleRight ?? outfitFemaleRight;
                leftOutfitImage.enabled = (leftOutfitImage.sprite != null);
            }

            // 2. Setup Right Character (Companion - Facing Left towards Player)
            if (npcId == "npc_margo")
            {
                if (rightBaseImage != null) rightBaseImage.sprite = baseFemaleLeft;
                if (rightOutfitImage != null) rightOutfitImage.sprite = outfitFemaleLeft;
            }
            else // Barnaby or Zephyr
            {
                if (rightBaseImage != null) rightBaseImage.sprite = baseMaleLeft;
                if (rightOutfitImage != null) rightOutfitImage.sprite = outfitMaleLeft;
            }

            if (rightBaseImage != null) rightBaseImage.enabled = (rightBaseImage.sprite != null);
            if (rightOutfitImage != null) rightOutfitImage.enabled = (rightOutfitImage.sprite != null);

            // 3. Setup Choices for this companion
            SetupChoicesForCompanion(npcId);

            // 4. Initial Greeting from companion
            string greeting = GetInitialGreeting(npcId);
            PlaySpeech(displayName, greeting, isNpcSpeaking: true);
        }

        private void SetupChoicesForCompanion(string npcId)
        {
            if (npcId == "npc_margo")
            {
                if (choiceText1 != null) choiceText1.text = "How do you distill Void Amethyst geodes?";
                if (choiceText2 != null) choiceText2.text = "What is the three-strike thermodynamic rule?";
                if (choiceText3 != null) choiceText3.text = "How did you become an alchemist delver?";
            }
            else if (npcId == "npc_barnaby")
            {
                if (choiceText1 != null) choiceText1.text = "Can this blade survive the deep Spire descent?";
                if (choiceText2 != null) choiceText2.text = "Teach me the secrets of Damascus fold-welding.";
                if (choiceText3 != null) choiceText3.text = "What happened on your first raid on Floor 50?";
            }
            else // Zephyr
            {
                if (choiceText1 != null) choiceText1.text = "Report on the wind currents in the Aqueduct.";
                if (choiceText2 != null) choiceText2.text = "How do we bypass the gargoyle cluster ambush?";
                if (choiceText3 != null) choiceText3.text = "Have you seen the Sovereign Arbiter above?";
            }
        }

        private string GetInitialGreeting(string npcId)
        {
            if (npcId == "npc_margo")
            {
                return "Pull up a log, delver. The embers are nice, but if you're here to ask about my alchemical reagents, keep your hands off the alembic. (¬_¬) What's on your mind?";
            }
            else if (npcId == "npc_barnaby")
            {
                return "Ho! Join the hearth, kid. A warm fire and a cool sharpening stone are the only two things a warrior can trust in the Spire. What can old Barnaby do for ya?";
            }
            else // Zephyr
            {
                return "Sit with your back to the fire. It leaves your eyes adjusted to the shadows outside our light. Speak quietly... the cavern echoes every breath.";
            }
        }

        public void PlaySpeech(string speakerName, string text, bool isNpcSpeaking)
        {
            if (speakerNameTMP != null) speakerNameTMP.text = speakerName;
            currentFullDialogue = text;
            isTypewriterFinished = false;

            if (promptIndicatorIcon != null) promptIndicatorIcon.gameObject.SetActive(false);
            if (choicesContainer != null) choicesContainer.SetActive(false);

            // Persona Focus Highlighting
            if (isNpcSpeaking)
            {
                SetSpeakerFocus(leftSpeaking: false, rightSpeaking: true);
            }
            else
            {
                SetSpeakerFocus(leftSpeaking: true, rightSpeaking: false);
            }

            if (activeTypewriterRoutine != null) StopCoroutine(activeTypewriterRoutine);
            activeTypewriterRoutine = StartCoroutine(TypewriterRoutine(text));
        }

        private void SetSpeakerFocus(bool leftSpeaking, bool rightSpeaking)
        {
            if (leftCanvasGroup != null)
            {
                leftCanvasGroup.alpha = leftSpeaking ? 1.0f : 0.65f;
            }
            if (leftCharacterRoot != null)
            {
                leftCharacterRoot.localScale = leftSpeaking ? new Vector3(1.04f, 1.04f, 1f) : new Vector3(0.97f, 0.97f, 1f);
            }

            if (rightCanvasGroup != null)
            {
                rightCanvasGroup.alpha = rightSpeaking ? 1.0f : 0.65f;
            }
            if (rightCharacterRoot != null)
            {
                rightCharacterRoot.localScale = rightSpeaking ? new Vector3(1.04f, 1.04f, 1f) : new Vector3(0.97f, 0.97f, 1f);
            }
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            if (dialogueBodyTMP == null) yield break;

            dialogueBodyTMP.text = "";
            float charDelay = 0.024f;

            for (int i = 0; i < fullText.Length; i++)
            {
                dialogueBodyTMP.text += fullText[i];
                char c = fullText[i];
                if (c == '.' || c == '!' || c == '?') yield return new WaitForSeconds(0.18f);
                else if (c == ',') yield return new WaitForSeconds(0.08f);
                else yield return new WaitForSeconds(charDelay);
            }

            isTypewriterFinished = true;
            if (promptIndicatorIcon != null) promptIndicatorIcon.gameObject.SetActive(true);
            if (choicesContainer != null) choicesContainer.SetActive(true);
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            string playerQuery = "";
            string npcResponse = "";

            if (activeNpcId == "npc_margo")
            {
                if (choiceIndex == 1)
                {
                    playerQuery = "How do you distill Void Amethyst geodes?";
                    npcResponse = "Margo: 'You don't just crack them open like walnuts! Void Amethysts contain volatile entropic resonance. You need a heated lead crucible and a steady solvent flow. One tremor, and the geode collapses your workbench. 🖤'";
                }
                else if (choiceIndex == 2)
                {
                    playerQuery = "What is the three-strike thermodynamic rule?";
                    npcResponse = "Margo: 'Strike an entropic matrix once: it fractals. Strike it twice: resonance spikes. Strike it a third time without coolant: boom. The Spire will blow you straight into the astral void. Don't be greedy.'";
                }
                else
                {
                    playerQuery = "How did you become an alchemist delver?";
                    npcResponse = "Margo: 'The High Academy in the Capital wanted me to brew dull perfumes for aristocrats. I wanted to see what happens when you synthesize dark matter with Spire ether. So I packed my alembic and headed into the deep dark.'";
                }
            }
            else if (activeNpcId == "npc_barnaby")
            {
                if (choiceIndex == 1)
                {
                    playerQuery = "Can this blade survive the deep Spire descent?";
                    npcResponse = "Barnaby: 'With my temper? It'll cleave gargoyle plate and drink shadow ether without a notch on the edge! Just remember to clean the core residue off after every dungeon floor.'";
                }
                else if (choiceIndex == 2)
                {
                    playerQuery = "Teach me the secrets of Damascus fold-welding.";
                    npcResponse = "Barnaby: 'Hah! Fold it seventy times in the flame, quench it in mineral oil, and listen to the ring of the steel. When the iron sings like a church bell, the matrix is locked.'";
                }
                else
                {
                    playerQuery = "What happened on your first raid on Floor 50?";
                    npcResponse = "Barnaby: 'We were green recruits. Thought we were heroes until the Sovereign Arbiter slammed its hammer down. Only two of us made it back to the cathedral. That's why I forge armor that doesn't bend.'";
                }
            }
            else // Zephyr
            {
                if (choiceIndex == 1)
                {
                    playerQuery = "Report on the wind currents in the Aqueduct.";
                    npcResponse = "Zephyr: 'The winds carry moisture and ozone from the abyss. If the draft smells of copper, a gargoyle swarm is hunting. Stick close to the stone pillars and avoid open arches.'";
                }
                else if (choiceIndex == 2)
                {
                    playerQuery = "How do we bypass the gargoyle cluster ambush?";
                    npcResponse = "Zephyr: 'They react to sudden kinetic footfalls. If you move during the wind gusts, the noise of the drafts masks your presence. I'll drop a beacon when the path is clear.'";
                }
                else
                {
                    playerQuery = "Have you seen the Sovereign Arbiter above?";
                    npcResponse = "Zephyr: 'From the highest parapet of Floor 49. Its halo burns with astral fire. It doesn't move like a beast; it moves like an inevitable judgment. We must be ready.'";
                }
            }

            StartCoroutine(RunPlayerThenNpcExchange(playerQuery, npcResponse));
        }

        private IEnumerator RunPlayerThenNpcExchange(string playerQuery, string npcResponse)
        {
            // 1. Player speaks first
            PlaySpeech("Summoner", playerQuery, isNpcSpeaking: false);
            while (!isTypewriterFinished) yield return null;
            yield return new WaitForSeconds(1.0f);

            // 2. Companion responds
            PlaySpeech(activeDisplayName, npcResponse, isNpcSpeaking: true);
        }

        private void SubmitCustomChat()
        {
            if (customChatInput == null || string.IsNullOrWhiteSpace(customChatInput.text)) return;
            string text = customChatInput.text.Trim();
            customChatInput.text = "";

            string reply = GenerateDynamicReply(activeNpcId, text);
            StartCoroutine(RunPlayerThenNpcExchange(text, reply));
        }

        private string GenerateDynamicReply(string npcId, string query)
        {
            string lower = query.ToLower();
            if (npcId == "npc_margo")
            {
                if (lower.Contains("food") || lower.Contains("cook") || lower.Contains("eat"))
                    return "Margo: 'If you want food, ask Barnaby. The only thing I can brew at this campfire is a high-potency restorative elixir, and it tastes like boiled brimstone.'";
                if (lower.Contains("help") || lower.Contains("magic") || lower.Contains("spell"))
                    return "Margo: 'My scythe will harvest their shadows, and my potions will keep your heart beating. Just don't ask me to smile while doing it. (¬_¬)'";
                return $"Margo: 'Interesting thought about \"{query}\". In alchemy, balance is everything. Keep your wits sharp tomorrow and we'll both survive.'";
            }
            else if (npcId == "npc_barnaby")
            {
                if (lower.Contains("fight") || lower.Contains("weapon") || lower.Contains("kill"))
                    return "Barnaby: 'Steel against bone, kid! Keep your shield high and your footing firm, and there isn't a demon in this Spire that can break our line!'";
                return $"Barnaby: 'Aye, regarding \"{query}\"... that's the spirit of an adventurer! Have another sip of broth and let your muscles rest before dawn.'";
            }
            else // Zephyr
            {
                if (lower.Contains("scout") || lower.Contains("path") || lower.Contains("danger"))
                    return "Zephyr: 'The winds never lie. Every corridor has an exhale; find it, and you'll find the exit before the shadows close in.'";
                return $"Zephyr: 'You speak with purpose, delver. Listen to the quiet around this fire. Tomorrow the storm begins.'";
            }
        }

        public void EndDialogue()
        {
            isDialogueActive = false;
            if (activeTypewriterRoutine != null) StopCoroutine(activeTypewriterRoutine);
            if (dialogueRootContainer != null) dialogueRootContainer.SetActive(false);

            if (CampfirePanoramicCameraController.Instance != null)
            {
                CampfirePanoramicCameraController.Instance.ResumeFreeLook();
            }
        }

        #region Procedural UI Hierarchy Builder
        private void EnsureUIConstructed()
        {
            if (dialogueRootContainer != null) return;

            dialogueCanvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (dialogueCanvas == null)
            {
                var canvasGo = new GameObject("CampfireDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                dialogueCanvas = canvasGo.GetComponent<Canvas>();
                dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                dialogueCanvas.sortingOrder = 100;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            dialogueRootContainer = new GameObject("Persona4_DualDialogueStage", typeof(RectTransform));
            dialogueRootContainer.transform.SetParent(dialogueCanvas.transform, false);
            RectTransform stageRect = dialogueRootContainer.GetComponent<RectTransform>();
            stageRect.anchorMin = Vector2.zero;
            stageRect.anchorMax = Vector2.one;
            stageRect.sizeDelta = Vector2.zero;

            // 1. Semi-transparent theatrical dark vignette backdrop
            GameObject vignetteObj = new GameObject("StageBackdrop", typeof(RectTransform), typeof(Image));
            vignetteObj.transform.SetParent(stageRect, false);
            RectTransform vigRect = vignetteObj.GetComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.sizeDelta = Vector2.zero;
            Image vigImg = vignetteObj.GetComponent<Image>();
            vigImg.color = new Color(0.04f, 0.03f, 0.05f, 0.45f); // Subtle mood tint allowing 3D campfire embers to show behind!
            vigImg.raycastTarget = false;

            // 2. Left Character Container (Player - 3/4th facing Right)
            GameObject leftObj = new GameObject("LeftCharacter_Player", typeof(RectTransform), typeof(CanvasGroup));
            leftObj.transform.SetParent(stageRect, false);
            leftCharacterRoot = leftObj.GetComponent<RectTransform>();
            leftCharacterRoot.anchorMin = new Vector2(0.03f, 0.05f);
            leftCharacterRoot.anchorMax = new Vector2(0.42f, 0.95f);
            leftCharacterRoot.pivot = new Vector2(0.5f, 0.5f);
            leftCharacterRoot.sizeDelta = Vector2.zero;
            leftCanvasGroup = leftObj.GetComponent<CanvasGroup>();

            // Left Base Image
            GameObject leftBaseObj = new GameObject("BaseLayer", typeof(RectTransform), typeof(Image));
            leftBaseObj.transform.SetParent(leftCharacterRoot, false);
            RectTransform lbRect = leftBaseObj.GetComponent<RectTransform>();
            lbRect.anchorMin = Vector2.zero;
            lbRect.anchorMax = Vector2.one;
            lbRect.sizeDelta = Vector2.zero;
            leftBaseImage = leftBaseObj.GetComponent<Image>();
            leftBaseImage.preserveAspect = true;
            leftBaseImage.raycastTarget = false;

            // Left Outfit Overlay Image
            GameObject leftOutfitObj = new GameObject("OutfitLayer", typeof(RectTransform), typeof(Image));
            leftOutfitObj.transform.SetParent(leftCharacterRoot, false);
            RectTransform loRect = leftOutfitObj.GetComponent<RectTransform>();
            loRect.anchorMin = Vector2.zero;
            loRect.anchorMax = Vector2.one;
            loRect.sizeDelta = Vector2.zero;
            leftOutfitImage = leftOutfitObj.GetComponent<Image>();
            leftOutfitImage.preserveAspect = true;
            leftOutfitImage.raycastTarget = false;

            // 3. Right Character Container (Companion - 3/4th facing Left)
            GameObject rightObj = new GameObject("RightCharacter_Companion", typeof(RectTransform), typeof(CanvasGroup));
            rightObj.transform.SetParent(stageRect, false);
            rightCharacterRoot = rightObj.GetComponent<RectTransform>();
            rightCharacterRoot.anchorMin = new Vector2(0.58f, 0.05f);
            rightCharacterRoot.anchorMax = new Vector2(0.97f, 0.95f);
            rightCharacterRoot.pivot = new Vector2(0.5f, 0.5f);
            rightCharacterRoot.sizeDelta = Vector2.zero;
            rightCanvasGroup = rightObj.GetComponent<CanvasGroup>();

            // Right Base Image
            GameObject rightBaseObj = new GameObject("BaseLayer", typeof(RectTransform), typeof(Image));
            rightBaseObj.transform.SetParent(rightCharacterRoot, false);
            RectTransform rbRect = rightBaseObj.GetComponent<RectTransform>();
            rbRect.anchorMin = Vector2.zero;
            rbRect.anchorMax = Vector2.one;
            rbRect.sizeDelta = Vector2.zero;
            rightBaseImage = rightBaseObj.GetComponent<Image>();
            rightBaseImage.preserveAspect = true;
            rightBaseImage.raycastTarget = false;

            // Right Outfit Overlay Image
            GameObject rightOutfitObj = new GameObject("OutfitLayer", typeof(RectTransform), typeof(Image));
            rightOutfitObj.transform.SetParent(rightCharacterRoot, false);
            RectTransform roRect = rightOutfitObj.GetComponent<RectTransform>();
            roRect.anchorMin = Vector2.zero;
            roRect.anchorMax = Vector2.one;
            roRect.sizeDelta = Vector2.zero;
            rightOutfitImage = rightOutfitObj.GetComponent<Image>();
            rightOutfitImage.preserveAspect = true;
            rightOutfitImage.raycastTarget = false;

            // 4. Persona 4 Dialogue Frame Container (Bottom Center)
            GameObject frameObj = new GameObject("Persona4_DialogueBox", typeof(RectTransform), typeof(Image));
            frameObj.transform.SetParent(stageRect, false);
            dialogueBoxRect = frameObj.GetComponent<RectTransform>();
            dialogueBoxRect.anchorMin = new Vector2(0.12f, 0.04f);
            dialogueBoxRect.anchorMax = new Vector2(0.88f, 0.32f);
            dialogueBoxRect.sizeDelta = Vector2.zero;
            dialogueBoxFrameImage = frameObj.GetComponent<Image>();
            dialogueBoxFrameImage.sprite = p4FrameSprite;
            dialogueBoxFrameImage.type = Image.Type.Sliced;
            dialogueBoxFrameImage.color = Color.white;

            // Speaker Name Banner Text (Top-Left of Box)
            GameObject nameObj = new GameObject("SpeakerName_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(dialogueBoxRect, false);
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.035f, 0.72f);
            nameRect.anchorMax = new Vector2(0.35f, 0.95f);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = Vector2.zero;
            speakerNameTMP = nameObj.GetComponent<TextMeshProUGUI>();
            speakerNameTMP.fontSize = 24f;
            speakerNameTMP.fontStyle = FontStyles.Bold;
            speakerNameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            speakerNameTMP.color = new Color(0.12f, 0.10f, 0.08f, 1f); // Dark bold on Yellow badge!
            speakerNameTMP.text = "Margo";

            // Dialogue Body Text
            GameObject bodyObj = new GameObject("DialogueBody_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObj.transform.SetParent(dialogueBoxRect, false);
            RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.04f, 0.12f);
            bodyRect.anchorMax = new Vector2(0.92f, 0.68f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = Vector2.zero;
            dialogueBodyTMP = bodyObj.GetComponent<TextMeshProUGUI>();
            dialogueBodyTMP.fontSize = 21f;
            dialogueBodyTMP.lineSpacing = 16f;
            dialogueBodyTMP.color = new Color(0.98f, 0.96f, 0.92f, 1f); // Crisp ivory text
            dialogueBodyTMP.alignment = TextAlignmentOptions.TopLeft;

            // Concentric Prompt Indicator Icon (Bottom-Right of Frame)
            GameObject indObj = new GameObject("PersonaIndicator", typeof(RectTransform), typeof(Image));
            indObj.transform.SetParent(dialogueBoxRect, false);
            RectTransform indRect = indObj.GetComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0.95f, 0.12f);
            indRect.anchorMax = new Vector2(0.95f, 0.12f);
            indRect.anchoredPosition = new Vector2(-15f, 15f);
            indRect.sizeDelta = new Vector2(36f, 36f);
            promptIndicatorIcon = indObj.GetComponent<Image>();
            promptIndicatorIcon.sprite = p4IndicatorSprite;
            promptIndicatorIcon.color = new Color(1f, 0.85f, 0.2f, 1f);
            promptIndicatorIcon.raycastTarget = false;

            // 5. Time/Date Badge (Top-Right)
            timeBadgeObj = new GameObject("PersonaTimeBadge", typeof(RectTransform), typeof(Image));
            timeBadgeObj.transform.SetParent(stageRect, false);
            RectTransform tbRect = timeBadgeObj.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.82f, 0.90f);
            tbRect.anchorMax = new Vector2(0.98f, 0.97f);
            tbRect.sizeDelta = Vector2.zero;
            Image tbImg = timeBadgeObj.GetComponent<Image>();
            tbImg.sprite = p4TimeBadgeSprite;
            tbImg.raycastTarget = false;

            GameObject tbText = new GameObject("TimeText_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            tbText.transform.SetParent(timeBadgeObj.transform, false);
            RectTransform tbtRect = tbText.GetComponent<RectTransform>();
            tbtRect.anchorMin = new Vector2(0.08f, 0.15f);
            tbtRect.anchorMax = new Vector2(0.75f, 0.85f);
            tbtRect.sizeDelta = Vector2.zero;
            timeBadgeTMP = tbText.GetComponent<TextMeshProUGUI>();
            timeBadgeTMP.fontSize = 17f;
            timeBadgeTMP.fontStyle = FontStyles.Bold;
            timeBadgeTMP.alignment = TextAlignmentOptions.Center;
            timeBadgeTMP.color = new Color(1f, 0.92f, 0.4f, 1f);
            timeBadgeTMP.text = "05/06 FRI Evening";

            // 6. Interactive Response Chips Container (Floats above dialogue box)
            choicesContainer = new GameObject("ChoicesContainer", typeof(RectTransform));
            choicesContainer.transform.SetParent(stageRect, false);
            RectTransform ccRect = choicesContainer.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0.20f, 0.33f);
            ccRect.anchorMax = new Vector2(0.80f, 0.44f);
            ccRect.sizeDelta = Vector2.zero;

            choiceButton1 = CreateChoiceButton(choicesContainer.transform, new Vector2(0f, 0.55f), new Vector2(0.48f, 1f), out choiceText1, "Topic 1");
            choiceButton2 = CreateChoiceButton(choicesContainer.transform, new Vector2(0.52f, 0.55f), new Vector2(1f, 1f), out choiceText2, "Topic 2");
            choiceButton3 = CreateChoiceButton(choicesContainer.transform, new Vector2(0f, 0f), new Vector2(0.48f, 0.45f), out choiceText3, "Topic 3");
            leaveButton = CreateChoiceButton(choicesContainer.transform, new Vector2(0.52f, 0f), new Vector2(1f, 0.45f), out var leaveTMP, "[Leave Conversation]", isExit: true);

            // 7. Interactive Custom Chat Bar (Bottom)
            GameObject chatBarObj = new GameObject("CustomChatBar", typeof(RectTransform), typeof(Image));
            chatBarObj.transform.SetParent(stageRect, false);
            RectTransform cbRect = chatBarObj.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.20f, 0.005f);
            cbRect.anchorMax = new Vector2(0.80f, 0.038f);
            cbRect.sizeDelta = Vector2.zero;
            Image cbImg = chatBarObj.GetComponent<Image>();
            cbImg.color = new Color(0.1f, 0.1f, 0.14f, 0.90f);

            GameObject inputObj = new GameObject("InputField", typeof(RectTransform), typeof(TMP_InputField));
            inputObj.transform.SetParent(chatBarObj.transform, false);
            RectTransform inpRect = inputObj.GetComponent<RectTransform>();
            inpRect.anchorMin = new Vector2(0.02f, 0.1f);
            inpRect.anchorMax = new Vector2(0.85f, 0.9f);
            inpRect.sizeDelta = Vector2.zero;
            customChatInput = inputObj.GetComponent<TMP_InputField>();

            GameObject inputTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            inputTextObj.transform.SetParent(inputObj.transform, false);
            RectTransform itRect = inputTextObj.GetComponent<RectTransform>();
            itRect.anchorMin = Vector2.zero;
            itRect.anchorMax = Vector2.one;
            itRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI inText = inputTextObj.GetComponent<TextMeshProUGUI>();
            inText.fontSize = 14f;
            inText.color = Color.white;
            customChatInput.textComponent = inText;

            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phObj.transform.SetParent(inputObj.transform, false);
            RectTransform phRect = phObj.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI phText = phObj.GetComponent<TextMeshProUGUI>();
            phText.fontSize = 14f;
            phText.color = new Color(0.7f, 0.7f, 0.8f, 0.6f);
            phText.text = "Type a question to your companion and press Enter...";
            customChatInput.placeholder = phText;

            // Send button
            GameObject sendObj = new GameObject("SendBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            sendObj.transform.SetParent(chatBarObj.transform, false);
            RectTransform sendRect = sendObj.GetComponent<RectTransform>();
            sendRect.anchorMin = new Vector2(0.87f, 0.1f);
            sendRect.anchorMax = new Vector2(0.98f, 0.9f);
            sendRect.sizeDelta = Vector2.zero;
            Image sendImg = sendObj.GetComponent<Image>();
            sendImg.color = new Color(0.95f, 0.70f, 0.15f, 1f);
            sendChatButton = sendObj.GetComponent<Button>();

            GameObject sendTextObj = new GameObject("Text_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            sendTextObj.transform.SetParent(sendObj.transform, false);
            RectTransform stRect = sendTextObj.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI stTMP = sendTextObj.GetComponent<TextMeshProUGUI>();
            stTMP.fontSize = 13f;
            stTMP.fontStyle = FontStyles.Bold;
            stTMP.alignment = TextAlignmentOptions.Center;
            stTMP.color = Color.black;
            stTMP.text = "SEND";
        }

        private Button CreateChoiceButton(Transform parent, Vector2 anchorMin, Vector2 anchorMax, out TextMeshProUGUI labelTMP, string defaultText, bool isExit = false)
        {
            GameObject btnObj = new GameObject("ChoiceBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;

            Image img = btnObj.GetComponent<Image>();
            img.color = isExit ? new Color(0.35f, 0.12f, 0.12f, 0.90f) : new Color(0.14f, 0.14f, 0.18f, 0.90f);

            Button btn = btnObj.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = isExit ? new Color(0.6f, 0.2f, 0.2f, 1f) : new Color(0.85f, 0.65f, 0.15f, 1f);
            colors.pressedColor = new Color(1f, 0.85f, 0.2f, 1f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Label_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 2f);
            trt.offsetMax = new Vector2(-10f, -2f);

            labelTMP = textObj.GetComponent<TextMeshProUGUI>();
            labelTMP.fontSize = 14f;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.color = Color.white;
            labelTMP.text = defaultText;

            return btn;
        }
        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PersonaDialogueLine
{
    public string speakerName;
    public string speakerTitle;
    public Sprite portraitSprite;
    [TextArea(2, 4)]
    public string text;
    public string audioSfx = "margo_chime";
}

[System.Serializable]
public class PersonaChoiceOption
{
    public string choiceText;
    public Action onSelected;

    public PersonaChoiceOption(string text, Action action)
    {
        choiceText = text;
        onSelected = action;
    }
}

/// <summary>
/// PersonaDialogueUI implements the iconic, stylized visual novel dialogue presentation:
/// - Skewed, high-contrast angular dialogue box with dynamic borders and halftone pop accents
/// - Slanted speaker nameplate badge
/// - Expressive 2D character portrait bust with emotion states and subtle breathing animation
/// - Typewriter text cadence with punctuation pauses and voice blip SFX
/// - Floating angled comic response choice speech bubbles
/// - Cinematic letterbox bars that slide into frame during conversation
/// </summary>
public class PersonaDialogueUI : MonoBehaviour
{
    private static PersonaDialogueUI _instance;
    public static PersonaDialogueUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PersonaDialogueUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Root & Letterbox Framing")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private RectTransform topLetterbox;
    [SerializeField] private RectTransform bottomLetterbox;

    [Header("Dialogue Box & Nameplate")]
    [SerializeField] private RectTransform dialogueBoxRect;
    [SerializeField] private Image dialogueBoxImg;
    [SerializeField] private RectTransform nameplateRect;
    [SerializeField] private TextMeshProUGUI speakerNameTMP;
    [SerializeField] private TextMeshProUGUI dialogueBodyTMP;
    [SerializeField] private Image advanceChevronImg;

    [Header("Character Portrait Bust")]
    [SerializeField] private RectTransform portraitContainerRect;
    [SerializeField] private Image characterPortraitImg;

    [Header("Persona-Style Floating Choices")]
    [SerializeField] private RectTransform choiceContainerRect;
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("Preloaded Character Busts")]
    [SerializeField] private Sprite margoPortraitSprite;
    [SerializeField] private Sprite barnabyPortraitSprite;
    [SerializeField] private Sprite zephyrPortraitSprite;
    [SerializeField] private Sprite ariaPortraitSprite;
    [SerializeField] private Sprite leoPortraitSprite;
    [SerializeField] private Sprite playerPortraitSprite;
    [SerializeField] private Sprite alexPortraitSprite;
    [SerializeField] private Sprite elenaPortraitSprite;
    [SerializeField] private Sprite cherylPortraitSprite;
    [SerializeField] private Sprite keithPortraitSprite;
    [SerializeField] private Sprite merchantPortraitSprite;
    [SerializeField] private Sprite demonPortraitSprite;

    [Header("Typewriter Timing")]
    [SerializeField] private float charTypeInterval = 0.024f;
    [SerializeField] private float punctuationPause = 0.18f;

    public bool IsDialogueActive => overlayRoot != null && overlayRoot.activeSelf;

    public static event Action OnDialogueStarted;
    public static event Action OnDialogueEnded;

    private readonly List<PersonaDialogueLine> queuedLines = new List<PersonaDialogueLine>();
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private string fullTargetText = "";
    private Coroutine typewriterCoroutine;
    private Action currentOnCompleteCallback;
    private readonly List<GameObject> activeChoiceButtons = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        LoadDefaultPortraits();
    }

    private void Start()
    {
        if (overlayRoot == null) overlayRoot = gameObject;
        SetVisible(false);
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        // Pulse advance chevron when waiting for input
        if (!isTyping && advanceChevronImg != null && activeChoiceButtons.Count == 0)
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 8f);
            advanceChevronImg.color = new Color(1.0f, 0.85f, 0.25f, pulse);
        }

        // Tap/click or Space/Enter to advance
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            // If choices are displayed, don't advance on blank click (wait for choice tap)
            if (activeChoiceButtons.Count > 0) return;
            HandleDialogueAdvance();
        }
    }

    public void LoadDefaultPortraits()
    {
        if (margoPortraitSprite == null)
            margoPortraitSprite = Resources.Load<Sprite>("Mockup_PersonaPortrait_Margo") ?? LoadSpriteFromTextures("Portraits/Portrait_Margo.png") ?? LoadSpriteFromTextures("Mockup_PersonaPortrait_Margo.png");
        if (barnabyPortraitSprite == null)
            barnabyPortraitSprite = LoadSpriteFromTextures("Portraits/Portrait_MayorBarnaby.png");
        if (zephyrPortraitSprite == null)
            zephyrPortraitSprite = LoadSpriteFromTextures("Portraits/Portrait_Zephyr.png");
        if (ariaPortraitSprite == null)
            ariaPortraitSprite = LoadSpriteFromTextures("Portraits/Portrait_Ally_Feme_Left.png");
        if (leoPortraitSprite == null)
            leoPortraitSprite = LoadSpriteFromTextures("Portraits/Portrait_Ally_Masc_Left.png");
        if (playerPortraitSprite == null)
            playerPortraitSprite = LoadSpriteFromTextures("Portraits/Portrait_Protagonist.png");
        if (alexPortraitSprite == null)
            alexPortraitSprite = LoadSpriteFromTextures("Dialogue/Crop_Alex_Bust.png");
        if (elenaPortraitSprite == null)
            elenaPortraitSprite = LoadSpriteFromTextures("Dialogue/Crop_Elena_Bust.png");
        if (cherylPortraitSprite == null)
            cherylPortraitSprite = LoadSpriteFromTextures("Portraits/Portrait_Cheryl.png");
        if (keithPortraitSprite == null)
            keithPortraitSprite = LoadSpriteFromTextures("Portraits/Portrait_Keith.png");
        if (merchantPortraitSprite == null)
            merchantPortraitSprite = Resources.Load<Sprite>("Mockup_PersonaPortrait_Merchant") ?? LoadSpriteFromTextures("Mockup_PersonaPortrait_Merchant.png");
        if (demonPortraitSprite == null)
            demonPortraitSprite = Resources.Load<Sprite>("Mockup_PersonaPortrait_Demon") ?? LoadSpriteFromTextures("Mockup_PersonaPortrait_Demon.png");
    }

    public Sprite GetOrResolvePortrait(string speakerName, Sprite fallback = null)
    {
        if (fallback != null) return fallback;
        if (string.IsNullOrEmpty(speakerName)) return margoPortraitSprite;

        string lower = speakerName.ToLowerInvariant();
        if (lower.Contains("margo")) return margoPortraitSprite;
        if (lower.Contains("barnaby")) return barnabyPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("zephyr")) return zephyrPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("aria") || lower.Contains("starter_female")) return ariaPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("leo") || lower.Contains("starter_male")) return leoPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("alex")) return alexPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("elena")) return elenaPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("cheryl")) return cherylPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("keith")) return keithPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("player") || lower.Contains("you") || lower.Contains("summoner")) return playerPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("merchant") || lower.Contains("balthazar")) return merchantPortraitSprite ?? margoPortraitSprite;
        if (lower.Contains("demon") || lower.Contains("boss")) return demonPortraitSprite ?? margoPortraitSprite;

        return margoPortraitSprite;
    }

    public string GetDefaultAudioSfx(string speaker)
    {
        if (string.IsNullOrEmpty(speaker)) return "margo_chime";
        string lower = speaker.ToLowerInvariant();
        if (lower.Contains("barnaby")) return "forge_strike";
        if (lower.Contains("zephyr")) return "harp_flourish";
        if (lower.Contains("cheryl")) return "bell_chime";
        if (lower.Contains("keith")) return "sword_unsheath";
        if (lower.Contains("player") || lower.Contains("you")) return "relic_chime";
        return "margo_chime";
    }

    private Sprite LoadSpriteFromTextures(string fileName)
    {
#if UNITY_EDITOR
        string path = "Assets/Textures/UI/" + fileName;
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }

    public void ShowSimpleDialogue(string speaker, string title, Sprite portrait, string[] lines, Action onComplete = null)
    {
        List<PersonaDialogueLine> list = new List<PersonaDialogueLine>();
        Sprite resolvedPortrait = portrait != null ? portrait : GetOrResolvePortrait(speaker);
        for (int i = 0; i < lines.Length; i++)
        {
            list.Add(new PersonaDialogueLine
            {
                speakerName = speaker,
                speakerTitle = title,
                portraitSprite = resolvedPortrait,
                text = lines[i],
                audioSfx = GetDefaultAudioSfx(speaker)
            });
        }
        StartDialogueSequence(list, onComplete);
    }

    /// <summary>
    /// Executes a dynamic multi-person conversation anywhere in the game world.
    /// Each line can have a distinct speakerName, speakerTitle, portraitSprite, and audioSfx.
    /// Missing portraits or SFX are resolved automatically.
    /// </summary>
    public void ShowMultiPersonDialogue(IEnumerable<PersonaDialogueLine> lines, Action onComplete = null)
    {
        if (lines == null) return;
        List<PersonaDialogueLine> list = new List<PersonaDialogueLine>();
        foreach (var l in lines)
        {
            if (l.portraitSprite == null)
            {
                l.portraitSprite = GetOrResolvePortrait(l.speakerName);
            }
            if (string.IsNullOrEmpty(l.audioSfx))
            {
                l.audioSfx = GetDefaultAudioSfx(l.speakerName);
            }
            list.Add(l);
        }
        StartDialogueSequence(list, onComplete);
    }

    public void StartDialogueSequence(List<PersonaDialogueLine> lines, Action onComplete = null)
    {
        if (lines == null || lines.Count == 0) return;

        queuedLines.Clear();
        queuedLines.AddRange(lines);
        currentLineIndex = 0;
        currentOnCompleteCallback = onComplete;

        ClearChoices();
        SetVisible(true);
        PlayLine(queuedLines[0]);
        OnDialogueStarted?.Invoke();
    }

    private void PlayLine(PersonaDialogueLine line)
    {
        if (speakerNameTMP != null)
        {
            string titleTag = string.IsNullOrEmpty(line.speakerTitle) ? "" : $" <size=70%><color=#FFE070>// {line.speakerTitle.ToUpper()}</color></size>";
            speakerNameTMP.text = $"<b>{line.speakerName.ToUpper()}</b>{titleTag}";
        }

        if (characterPortraitImg != null)
        {
            Sprite resolvedSprite = line.portraitSprite != null ? line.portraitSprite : GetOrResolvePortrait(line.speakerName);
            if (resolvedSprite != null)
            {
                characterPortraitImg.sprite = resolvedSprite;
                characterPortraitImg.gameObject.SetActive(true);

                // Pop breathing bounce on speaker change
                if (portraitContainerRect != null)
                {
                    StopCoroutine("AnimatePortraitEntrance");
                    StartCoroutine(AnimatePortraitEntrance());
                }
            }
        }

        fullTargetText = line.text;
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterRoutine(fullTargetText, line.audioSfx));
    }

    private IEnumerator AnimatePortraitEntrance()
    {
        Vector3 startScale = new Vector3(0.92f, 0.92f, 1f);
        Vector3 targetScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < 0.22f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.22f;
            portraitContainerRect.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            yield return null;
        }
        portraitContainerRect.localScale = targetScale;
    }

    private IEnumerator TypewriterRoutine(string target, string audioSfx)
    {
        isTyping = true;
        if (advanceChevronImg != null) advanceChevronImg.gameObject.SetActive(false);
        if (dialogueBodyTMP != null) dialogueBodyTMP.text = "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < target.Length; i++)
        {
            char ch = target[i];
            sb.Append(ch);
            if (dialogueBodyTMP != null) dialogueBodyTMP.text = sb.ToString();

            // SFX chime every few characters
            if (i % 3 == 0 && BattleSummonerAudioMaster.Instance != null && !char.IsWhiteSpace(ch))
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 0.15f);
            }

            if (ch == '.' || ch == '!' || ch == '?' || ch == '—')
            {
                yield return new WaitForSeconds(punctuationPause);
            }
            else if (ch == ',')
            {
                yield return new WaitForSeconds(punctuationPause * 0.5f);
            }
            else
            {
                yield return new WaitForSeconds(charTypeInterval);
            }
        }

        isTyping = false;
        if (advanceChevronImg != null) advanceChevronImg.gameObject.SetActive(true);
    }

    public void HandleDialogueAdvance()
    {
        if (isTyping)
        {
            // Fast-forward text
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            isTyping = false;
            if (dialogueBodyTMP != null) dialogueBodyTMP.text = fullTargetText;
            if (advanceChevronImg != null) advanceChevronImg.gameObject.SetActive(true);
            return;
        }

        currentLineIndex++;
        if (currentLineIndex < queuedLines.Count)
        {
            PlayLine(queuedLines[currentLineIndex]);
        }
        else
        {
            CloseDialogue();
        }
    }

    public void ShowChoices(PersonaChoiceOption[] choices)
    {
        ClearChoices();
        if (choices == null || choices.Length == 0 || choiceContainerRect == null) return;

        if (advanceChevronImg != null) advanceChevronImg.gameObject.SetActive(false);

        for (int i = 0; i < choices.Length; i++)
        {
            var opt = choices[i];
            int choiceIndex = i;

            GameObject btnObj = new GameObject($"ChoiceBtn_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(choiceContainerRect, false);
            activeChoiceButtons.Add(btnObj);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 54f);
            rt.anchoredPosition = new Vector2(0f, -i * 62f);

            Image img = btnObj.GetComponent<Image>();
            Sprite choiceSprite = Resources.Load<Sprite>("Mockup_PersonaChoiceBtn") ?? LoadSpriteFromTextures("Mockup_PersonaChoiceBtn.png");
            if (choiceSprite != null) img.sprite = choiceSprite;
            img.color = Color.white;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
                }
                ClearChoices();
                opt.onSelected?.Invoke();
            });

            // Choice label text
            GameObject txtObj = new GameObject("ChoiceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(32f, 4f);
            trt.offsetMax = new Vector2(-16f, -4f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#FFD700><b>[{choiceIndex + 1}]</b></color> {opt.choiceText}";
            tmp.fontSize = 15;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
        }
    }

    public void ClearChoices()
    {
        for (int i = 0; i < activeChoiceButtons.Count; i++)
        {
            if (activeChoiceButtons[i] != null) Destroy(activeChoiceButtons[i]);
        }
        activeChoiceButtons.Clear();
    }

    public void CloseDialogue()
    {
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        isTyping = false;
        ClearChoices();
        SetVisible(false);

        Action cb = currentOnCompleteCallback;
        currentOnCompleteCallback = null;
        cb?.Invoke();

        OnDialogueEnded?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        if (overlayRoot != null) overlayRoot.SetActive(visible);

        Transform parentTrans = transform.parent;
        if (parentTrans != null)
        {
            Transform hud = parentTrans.Find("MockupMasterHUD");
            if (hud != null) hud.gameObject.SetActive(!visible);

            Transform safeArea = parentTrans.Find("SafeAreaContainer");
            if (safeArea != null) safeArea.gameObject.SetActive(!visible);
        }
        else
        {
            GameObject hudGo = GameObject.Find("MockupMasterHUD");
            if (hudGo != null) hudGo.SetActive(!visible);
            GameObject safeGo = GameObject.Find("SafeAreaContainer");
            if (safeGo != null) safeGo.SetActive(!visible);
        }

        if (visible)
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.85f);
            }
        }
    }

    public void BindReferences(
        GameObject root,
        RectTransform topBar,
        RectTransform bottomBar,
        RectTransform boxRect,
        Image boxImg,
        RectTransform nameRect,
        TextMeshProUGUI speakerTmp,
        TextMeshProUGUI bodyTmp,
        Image chevron,
        RectTransform portraitRect,
        Image portraitImg,
        RectTransform choicesRect)
    {
        overlayRoot = root;
        topLetterbox = topBar;
        bottomLetterbox = bottomBar;
        dialogueBoxRect = boxRect;
        dialogueBoxImg = boxImg;
        nameplateRect = nameRect;
        speakerNameTMP = speakerTmp;
        dialogueBodyTMP = bodyTmp;
        advanceChevronImg = chevron;
        portraitContainerRect = portraitRect;
        characterPortraitImg = portraitImg;
        choiceContainerRect = choicesRect;
    }
}

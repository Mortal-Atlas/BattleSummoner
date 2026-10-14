using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MargoChattinessMode
{
    Normal,   // Tactical warnings + occasional ambient gothic remarks (18-25s cd)
    Quiet,    // Only life-threatening emergencies (Boss slam, <20% HP)
    Chatty    // Frequent gothic lore, environmental quips, and advice (8-12s cd)
}

/// <summary>
/// MargoGuideCompanion provides an intelligent, non-annoying gothic guiding presence (Navi done right).
/// Manifests as Margo's Astral Occult Familiar hovering over the player's shoulder in the dungeon.
/// 
/// Core Features:
/// - Real-time contextual warnings: Boss telegraph slams, Phase shifts, Low HP, Weapon temper dulling/overheat.
/// - Occult Amethyst Ward: Automatically deploys a protective barrier on critical HP (<20%).
/// - Class Swap Commentary: Distinct gothic quips when switching between Warrior, Mage, and Bard.
/// - Environmental Appraisal: Detects nearby mineral veins (Void Amethysts, Prismatic Hearts) & Relic Cryptex puzzles.
/// - Anti-Navi System: Non-blocking UI, polite cooldowns, 3 chattiness modes (Normal/Quiet/Chatty), hotkey toggle (M).
/// - On-Demand Guidance: Press H or Tab, tap the familiar in 3D, or click the HUD "ASK MARGO" button for live tactical analysis.
/// </summary>
public class MargoGuideCompanion : MonoBehaviour
{
    public static MargoGuideCompanion Instance { get; private set; }

    [Header("Chattiness Configuration")]
    [SerializeField] private MargoChattinessMode chattinessMode = MargoChattinessMode.Normal;
    [SerializeField] private float speechDisplayDuration = 4.2f;

    [Header("Hover & Follow Mechanics")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 shoulderOffset = new Vector3(-0.75f, 1.85f, -0.35f);
    [SerializeField] private float followDamping = 0.22f;
    [SerializeField] private float bobbingAmplitude = 0.08f;
    [SerializeField] private float bobbingSpeed = 2.4f;

    [Header("Subsystem Linkages")]
    [SerializeField] private BossController bossController;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private WeaponTemper weaponTemper;

    // Visual Elements of Familiar
    private GameObject familiarRoot;
    private Transform familiarCore;
    private Transform orbitalShardA;
    private Transform orbitalShardB;
    private Transform orbitalShardC;
    private Transform occultHalo;
    private Light familiarLight;
    private GameObject wardShieldSphere;

    // 3D World Dialogue Bubble
    private GameObject worldBubbleObj;
    private Text worldBubbleText;
    private CanvasGroup worldBubbleCanvasGroup;

    // Screen Space Gothic HUD Toast
    private GameObject screenHudCard;
    private CanvasGroup screenHudCanvasGroup;
    private TextMeshProUGUI hudSpeakerTitleTMP;
    private TextMeshProUGUI hudSpeechContentTMP;
    private TextMeshProUGUI hudModeBadgeTMP;

    // Motion Tracking Velocity
    private Vector3 currentVelocity = Vector3.zero;
    private Coroutine activeSpeechCoroutine;
    private Coroutine wardCoroutine;

    // Cooldown trackers (Anti-Navi throttle)
    private float lastGeneralSpeechTime = -99f;
    private float lastBossTelegraphTime = -99f;
    private float lastHealthWarningTime = -99f;
    private float lastTemperWarningTime = -99f;
    private float lastMiningAlertTime = -99f;
    private float lastRelicAlertTime = -99f;
    private float lastWardTriggerTime = -99f;
    private float idleAmbientTimer = 0f;

    private readonly HashSet<CaveMiningNode> alertedVeins = new HashSet<CaveMiningNode>();
    private bool hasAlertedRelic = false;

    public MargoChattinessMode CurrentMode => chattinessMode;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        ResolveDependencies();
        BuildFamiliarVisualRig();
        BuildWorldDialogueBubble();
        BuildScreenGothicToastHUD();
    }

    private void Start()
    {
        HookSubsystemEvents();

        // Initial welcoming gothic remark after floor load
        StartCoroutine(InitialGreetingRoutine());
    }

    private void OnDestroy()
    {
        UnhookSubsystemEvents();
    }

    private void Update()
    {
        UpdateFamiliarMovement();
        UpdateOrbitalAnimation();
        HandleDesktopShortcuts();
        UpdateProximitySensors();
        UpdateIdleAmbientTimer();
    }

    #region Subsystems & Dependencies
    private void ResolveDependencies()
    {
        if (playerTransform == null)
        {
            GameObject hero = GameObject.Find("Player_Hero");
            if (hero != null) playerTransform = hero.transform;
            else
            {
                var cc = FindAnyObjectByType<CharacterController>();
                if (cc != null) playerTransform = cc.transform;
            }
        }

        if (bossController == null) bossController = FindAnyObjectByType<BossController>();
        if (playerProgression == null) playerProgression = FindAnyObjectByType<PlayerProgression>();
        if (weaponTemper == null) weaponTemper = FindAnyObjectByType<WeaponTemper>();
    }

    private void HookSubsystemEvents()
    {
        if (bossController != null)
        {
            bossController.OnBossTelegraphSlam += HandleBossTelegraph;
            bossController.OnBossHealthChanged += HandleBossHealthChanged;
            bossController.OnBossDefeated += HandleBossDefeated;
        }

        if (playerProgression != null)
        {
            playerProgression.OnHealthChanged += HandlePlayerHealthChanged;
        }

        if (weaponTemper != null)
        {
            weaponTemper.OnEdgeDullWarning += HandleEdgeDullWarning;
            weaponTemper.OnOverheatWarning += HandleTemperOverheatWarning;
        }

        DevClassLoadoutManager.OnClassChanged += HandleClassLoadoutChanged;
        CaveMiningNode.OnMineralMined += HandleMineralMined;
    }

    private void UnhookSubsystemEvents()
    {
        if (bossController != null)
        {
            bossController.OnBossTelegraphSlam -= HandleBossTelegraph;
            bossController.OnBossHealthChanged -= HandleBossHealthChanged;
            bossController.OnBossDefeated -= HandleBossDefeated;
        }

        if (playerProgression != null)
        {
            playerProgression.OnHealthChanged -= HandlePlayerHealthChanged;
        }

        if (weaponTemper != null)
        {
            weaponTemper.OnEdgeDullWarning -= HandleEdgeDullWarning;
            weaponTemper.OnOverheatWarning -= HandleTemperOverheatWarning;
        }

        DevClassLoadoutManager.OnClassChanged -= HandleClassLoadoutChanged;
        CaveMiningNode.OnMineralMined -= HandleMineralMined;
    }
    #endregion

    #region 3D Visual Familiar Rig Construction
    private void BuildFamiliarVisualRig()
    {
        familiarRoot = new GameObject("Margo_AstralFamiliar");
        if (playerTransform != null)
        {
            familiarRoot.transform.position = playerTransform.position + shoulderOffset;
        }

        // Tap/Click Collider on Familiar
        SphereCollider col = familiarRoot.AddComponent<SphereCollider>();
        col.radius = 0.45f;
        col.isTrigger = true;

        // Core Glowing Amethyst Sphere
        GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObj.name = "Familiar_Core";
        coreObj.transform.SetParent(familiarRoot.transform, false);
        coreObj.transform.localScale = new Vector3(0.32f, 0.36f, 0.32f);
        var cCol = coreObj.GetComponent<Collider>(); if (cCol != null) Destroy(cCol);
        familiarCore = coreObj.transform;

        Renderer coreRend = coreObj.GetComponent<Renderer>();
        if (coreRend != null)
        {
            Material coreMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            coreMat.color = new Color(0.35f, 0.08f, 0.55f, 1f);
            if (coreMat.HasProperty("_EmissionColor"))
            {
                coreMat.EnableKeyword("_EMISSION");
                coreMat.SetColor("_EmissionColor", new Color(0.85f, 0.35f, 1.0f) * 2.2f);
            }
            coreMat.SetFloat("_Metallic", 0.4f);
            coreMat.SetFloat("_Smoothness", 0.9f);
            coreRend.sharedMaterial = coreMat;
        }

        // Occult Halo Ring floating above core
        GameObject haloObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        haloObj.name = "Occult_Halo";
        haloObj.transform.SetParent(familiarRoot.transform, false);
        haloObj.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        haloObj.transform.localScale = new Vector3(0.55f, 0.015f, 0.55f);
        haloObj.transform.localRotation = Quaternion.Euler(-18f, 0f, 14f);
        var hCol = haloObj.GetComponent<Collider>(); if (hCol != null) Destroy(hCol);
        occultHalo = haloObj.transform;

        Renderer haloRend = haloObj.GetComponent<Renderer>();
        if (haloRend != null)
        {
            Material haloMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            haloMat.color = new Color(0.12f, 0.04f, 0.20f, 1f);
            if (haloMat.HasProperty("_EmissionColor"))
            {
                haloMat.EnableKeyword("_EMISSION");
                haloMat.SetColor("_EmissionColor", new Color(0.70f, 0.25f, 0.95f) * 1.5f);
            }
            haloRend.sharedMaterial = haloMat;
        }

        // Orbiting Obsidian Raven Feathers / Shards (Tri-fold orbit)
        orbitalShardA = CreateOrbitingShard("Orbital_Shard_A", new Vector3(0.12f, 0.46f, 0.06f), new Color(0.06f, 0.04f, 0.10f));
        orbitalShardB = CreateOrbitingShard("Orbital_Shard_B", new Vector3(0.10f, 0.40f, 0.06f), new Color(0.12f, 0.06f, 0.18f));
        orbitalShardC = CreateOrbitingShard("Orbital_Shard_C", new Vector3(0.11f, 0.42f, 0.06f), new Color(0.08f, 0.04f, 0.14f));

        // Soft Amethyst Point Light
        GameObject lightObj = new GameObject("Familiar_PointLight", typeof(Light));
        lightObj.transform.SetParent(familiarRoot.transform, false);
        familiarLight = lightObj.GetComponent<Light>();
        familiarLight.type = LightType.Point;
        familiarLight.color = new Color(0.82f, 0.42f, 1.0f);
        familiarLight.intensity = 1.6f;
        familiarLight.range = 4.2f;

        // Amethyst Ward Shield Bubble (initial disabled)
        wardShieldSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wardShieldSphere.name = "Occult_AmethystWardShield";
        wardShieldSphere.transform.SetParent(familiarRoot.transform, false);
        wardShieldSphere.transform.localScale = Vector3.zero;
        var wCol = wardShieldSphere.GetComponent<Collider>(); if (wCol != null) Destroy(wCol);
        Renderer wRend = wardShieldSphere.GetComponent<Renderer>();
        if (wRend != null)
        {
            Material wMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            wMat.color = new Color(0.65f, 0.15f, 0.95f, 0.45f);
            if (wMat.HasProperty("_EmissionColor"))
            {
                wMat.EnableKeyword("_EMISSION");
                wMat.SetColor("_EmissionColor", new Color(0.75f, 0.25f, 1.0f) * 1.8f);
            }
            wRend.sharedMaterial = wMat;
        }
        wardShieldSphere.SetActive(false);
    }

    private Transform CreateOrbitingShard(string name, Vector3 scale, Color color)
    {
        GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shard.name = name;
        shard.transform.SetParent(familiarRoot.transform, false);
        shard.transform.localScale = scale;
        var sCol = shard.GetComponent<Collider>(); if (sCol != null) Destroy(sCol);

        Renderer sRend = shard.GetComponent<Renderer>();
        if (sRend != null)
        {
            Material sMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            sMat.color = color;
            sMat.SetFloat("_Metallic", 0.85f);
            sMat.SetFloat("_Smoothness", 0.92f);
            sRend.sharedMaterial = sMat;
        }
        return shard.transform;
    }
    #endregion

    #region World-Space & Screen-Space Dialogue UI
    private void BuildWorldDialogueBubble()
    {
        worldBubbleObj = new GameObject("World_DialogueBubble", typeof(Canvas), typeof(CanvasGroup));
        worldBubbleObj.transform.SetParent(familiarRoot.transform, false);
        worldBubbleObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);

        Canvas canvas = worldBubbleObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        worldBubbleObj.transform.localScale = Vector3.one * 0.0075f;

        worldBubbleCanvasGroup = worldBubbleObj.GetComponent<CanvasGroup>();
        worldBubbleCanvasGroup.alpha = 0f;

        // Background Panel
        GameObject panelObj = new GameObject("BubblePanel", typeof(Image));
        panelObj.transform.SetParent(worldBubbleObj.transform, false);
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240f, 80f);
        Image bgImg = panelObj.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.04f, 0.10f, 0.92f);

        // Dialogue text
        GameObject textObj = new GameObject("BubbleText", typeof(Text));
        textObj.transform.SetParent(panelObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(220f, 70f);
        worldBubbleText = textObj.GetComponent<Text>();
        worldBubbleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        worldBubbleText.fontSize = 15;
        worldBubbleText.alignment = TextAnchor.MiddleCenter;
        worldBubbleText.color = new Color(0.92f, 0.82f, 1.0f);
    }

    private void BuildScreenGothicToastHUD()
    {
        Canvas screenCanvas = null;
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode != RenderMode.WorldSpace)
            {
                screenCanvas = canvases[i];
                break;
            }
        }
        if (screenCanvas == null) return;

        screenHudCard = new GameObject("MargoGuide_ScreenToastCard", typeof(RectTransform), typeof(CanvasGroup));
        screenHudCard.transform.SetParent(screenCanvas.transform, false);
        screenHudCard.transform.SetAsLastSibling();
        RectTransform cardRt = screenHudCard.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.04f, 0.77f);
        cardRt.anchorMax = new Vector2(0.96f, 0.88f);
        cardRt.offsetMin = Vector2.zero;
        cardRt.offsetMax = Vector2.zero;

        screenHudCanvasGroup = screenHudCard.GetComponent<CanvasGroup>();
        screenHudCanvasGroup.alpha = 0f;
        screenHudCanvasGroup.blocksRaycasts = false;

        // Card Obsidian Background with glowing violet outline
        GameObject bgObj = new GameObject("Card_BG", typeof(Image), typeof(Outline));
        bgObj.transform.SetParent(screenHudCard.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.05f, 0.02f, 0.10f, 0.98f);

        Outline outline = bgObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.22f, 0.95f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject titleObj = new GameObject("HeaderTitle", typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(screenHudCard.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.04f, 0.65f);
        titleRt.anchorMax = new Vector2(0.68f, 0.95f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        hudSpeakerTitleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        hudSpeakerTitleTMP.text = "<color=#DDA0DD>[*] MARGO</color> <color=#BA55D3><size=13>OCCULT GUIDE</size></color>";
        hudSpeakerTitleTMP.fontSize = 16;
        hudSpeakerTitleTMP.fontStyle = FontStyles.Bold;
        hudSpeakerTitleTMP.color = new Color(0.90f, 0.50f, 1.0f);

        // Chattiness Mode Badge
        GameObject badgeObj = new GameObject("ModeBadge", typeof(TextMeshProUGUI), typeof(Button));
        badgeObj.transform.SetParent(screenHudCard.transform, false);
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.68f, 0.65f);
        badgeRt.anchorMax = new Vector2(0.96f, 0.95f);
        badgeRt.offsetMin = Vector2.zero;
        badgeRt.offsetMax = Vector2.zero;
        hudModeBadgeTMP = badgeObj.GetComponent<TextMeshProUGUI>();
        hudModeBadgeTMP.fontSize = 13;
        hudModeBadgeTMP.alignment = TextAlignmentOptions.Right;
        hudModeBadgeTMP.color = new Color(0.75f, 0.70f, 0.88f);
        UpdateModeBadge();

        Button badgeBtn = badgeObj.GetComponent<Button>();
        badgeBtn.onClick.AddListener(CycleChattinessMode);

        // Speech Content
        GameObject contentObj = new GameObject("SpeechContent", typeof(TextMeshProUGUI));
        contentObj.transform.SetParent(screenHudCard.transform, false);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.04f, 0.06f);
        contentRt.anchorMax = new Vector2(0.96f, 0.64f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;
        hudSpeechContentTMP = contentObj.GetComponent<TextMeshProUGUI>();
        hudSpeechContentTMP.fontSize = 15;
        hudSpeechContentTMP.textWrappingMode = TextWrappingModes.Normal;
        hudSpeechContentTMP.color = new Color(1.0f, 0.97f, 1.0f);
        hudSpeechContentTMP.text = "Margo accompanies you through the Spire floor.";
    }

    private void UpdateModeBadge()
    {
        if (hudModeBadgeTMP != null)
        {
            hudModeBadgeTMP.text = $"[Mode: {chattinessMode} (M)]";
        }
    }
    #endregion

    #region Movement & Floating Physics
    private void UpdateFamiliarMovement()
    {
        if (playerTransform == null || familiarRoot == null) return;

        // Hover slightly over left shoulder, gently leading or lagging based on velocity
        Vector3 targetPos = playerTransform.position + (playerTransform.rotation * shoulderOffset);
        float floatBob = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmplitude;
        targetPos.y += floatBob;

        familiarRoot.transform.position = Vector3.SmoothDamp(
            familiarRoot.transform.position,
            targetPos,
            ref currentVelocity,
            followDamping
        );

        // Turn world bubble towards camera
        if (worldBubbleObj != null && Camera.main != null)
        {
            worldBubbleObj.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void UpdateOrbitalAnimation()
    {
        if (orbitalShardA != null)
        {
            orbitalShardA.RotateAround(familiarCore.position, Vector3.up, 85f * Time.deltaTime);
            orbitalShardA.Rotate(new Vector3(45f, 25f, 15f) * Time.deltaTime);
        }

        if (orbitalShardB != null)
        {
            orbitalShardB.RotateAround(familiarCore.position, Vector3.forward + Vector3.up, -110f * Time.deltaTime);
            orbitalShardB.Rotate(new Vector3(15f, 50f, 30f) * Time.deltaTime);
        }

        if (orbitalShardC != null)
        {
            orbitalShardC.RotateAround(familiarCore.position, new Vector3(1f, 1f, 0.5f), 135f * Time.deltaTime);
            orbitalShardC.Rotate(new Vector3(30f, 40f, 20f) * Time.deltaTime);
        }

        if (occultHalo != null)
        {
            occultHalo.Rotate(Vector3.up, 30f * Time.deltaTime, Space.Self);
        }
    }
    #endregion

    #region Desktop Hotkeys & Input
    private void HandleDesktopShortcuts()
    {
        // 'H' key or Tab triggers On-Demand Guidance
        if (Input.GetKeyDown(KeyCode.H))
        {
            ProvideOnDemandGuidance();
        }

        // 'M' key cycles Chattiness mode
        if (Input.GetKeyDown(KeyCode.M))
        {
            CycleChattinessMode();
        }

        // Tap/click familiar in 3D world
        if (Input.GetMouseButtonDown(0) && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                if (hit.collider.gameObject == familiarRoot || hit.transform.IsChildOf(familiarRoot.transform))
                {
                    ProvideOnDemandGuidance();
                }
            }
        }
    }

    public void CycleChattinessMode()
    {
        chattinessMode = (MargoChattinessMode)(((int)chattinessMode + 1) % 3);
        UpdateModeBadge();

        string modeMessage = chattinessMode switch
        {
            MargoChattinessMode.Normal => "Switched to Normal mode. I'll advise when relevant. (¬_¬)",
            MargoChattinessMode.Quiet => "Quiet mode active. I shall speak only if death knocks at your door. 🖤",
            MargoChattinessMode.Chatty => "Chatty mode active. Prepare for unsolicited alchemical commentary.",
            _ => "Mode updated."
        };

        Speak(modeMessage, "margo_chime", true);
        Debug.Log($"[MARGO GUIDE] Chattiness mode set to: {chattinessMode}");
    }
    #endregion

    #region Core Speech & Announcement Pipeline
    public void Speak(string text, string sfxPreset = null, bool forceBypassCooldown = false, float preDelay = 0.40f)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Anti-Navi Cooldown Check - slowed down so she speaks calmly and infrequently
        float minInterval = chattinessMode switch
        {
            MargoChattinessMode.Chatty => 18f,
            MargoChattinessMode.Quiet => 50f,
            _ => 30f // Normal mode: at least 30s between unprompted quips
        };

        if (!forceBypassCooldown && (Time.time - lastGeneralSpeechTime) < minInterval)
        {
            return;
        }

        lastGeneralSpeechTime = Time.time;
        idleAmbientTimer = 0f;

        // Strip rich text tags before sending to ElevenLabs TTS
        string cleanVoiceText = StripRichText(text);

        // Voice dialogue via ElevenLabs TTS (NO CHIME NOISE!)
        if (ElevenLabsTTSBridge.Instance != null && !string.IsNullOrEmpty(cleanVoiceText))
        {
            ElevenLabsTTSBridge.Instance.SpeakDialogue(cleanVoiceText, "npc_margo");
        }

        // Pulse familiar light
        StartCoroutine(PulseFamiliarLightRoutine());

        // Update UI displays with deliberate conversational pacing
        if (activeSpeechCoroutine != null) StopCoroutine(activeSpeechCoroutine);
        activeSpeechCoroutine = StartCoroutine(DisplaySpeechRoutine(text, preDelay));
    }

    private string StripRichText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty)
            .Replace("[*]", "")
            .Replace("(¬_¬)", "")
            .Trim();
    }

    private IEnumerator DisplaySpeechRoutine(string text, float preDelay)
    {
        if (preDelay > 0.01f)
        {
            yield return new WaitForSeconds(preDelay);
        }

        // 1. World bubble
        if (worldBubbleText != null)
        {
            string shortSnippet = text.Length > 85 ? text.Substring(0, 82) + "..." : text;
            worldBubbleText.text = shortSnippet;
        }

        // 2. Screen HUD Card
        if (hudSpeechContentTMP != null)
        {
            hudSpeechContentTMP.text = text;
        }

        // Fade in
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, t / 0.25f);
            if (worldBubbleCanvasGroup != null) worldBubbleCanvasGroup.alpha = a;
            if (screenHudCanvasGroup != null) screenHudCanvasGroup.alpha = a;
            yield return null;
        }

        yield return new WaitForSeconds(speechDisplayDuration);

        // Fade out
        t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / 0.45f);
            if (worldBubbleCanvasGroup != null) worldBubbleCanvasGroup.alpha = a;
            if (screenHudCanvasGroup != null) screenHudCanvasGroup.alpha = a;
            yield return null;
        }

        if (worldBubbleCanvasGroup != null) worldBubbleCanvasGroup.alpha = 0f;
        if (screenHudCanvasGroup != null) screenHudCanvasGroup.alpha = 0f;
    }

    private IEnumerator PulseFamiliarLightRoutine()
    {
        if (familiarLight == null) yield break;

        float originalIntensity = 1.6f;
        float pulseIntensity = 3.2f;

        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            familiarLight.intensity = Mathf.Lerp(originalIntensity, pulseIntensity, t / 0.15f);
            yield return null;
        }

        t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            familiarLight.intensity = Mathf.Lerp(pulseIntensity, originalIntensity, t / 0.35f);
            yield return null;
        }

        familiarLight.intensity = originalIntensity;
    }
    #endregion

    #region Contextual Event Triggers (Boss, Health, Temper, Loadouts)
    private void HandleBossTelegraph()
    {
        if ((Time.time - lastBossTelegraphTime) < 18.0f) return;
        lastBossTelegraphTime = Time.time;

        string[] telegraphLines = {
            "Aethelgard's tectonic core is surging! Step back unless you want your ribs crushed into gothic mosaics.",
            "Kinetic shockwave incoming! Clear the impact zone, delver. (¬_¬)",
            "The Colossus is priming a ground slam! Evade immediately!"
        };

        Speak(telegraphLines[UnityEngine.Random.Range(0, telegraphLines.Length)], null, true, 0.2f);
    }

    private void HandleBossHealthChanged(float current, float max)
    {
        float ratio = current / max;

        if (ratio < 0.5f && ratio > 0.42f && (Time.time - lastBossTelegraphTime) > 15f)
        {
            lastBossTelegraphTime = Time.time;
            Speak("Its outer mantle has ruptured. Focus on the exposed mana conduits—shatter it before it recovers! (¬_¬)", "relic_chime");
        }
        else if (ratio < 0.20f && ratio > 0.12f && (Time.time - lastBossTelegraphTime) > 15f)
        {
            lastBossTelegraphTime = Time.time;
            Speak("Structural integrity critical! One decisive assault and this behemoth becomes ancient scrap.", "margo_chime");
        }
    }

    private void HandleBossDefeated()
    {
        Speak("Reduced to dust and shattered glyphs. A poetic demise. Scoop up its mana cores before ambient entropy claims them. <color=#BA55D3><3</color>", "relic_chime", true);
    }

    private void HandlePlayerHealthChanged(float current, float max)
    {
        float ratio = current / max;

        // Clutch Occult Ward Emergency (< 20% HP)
        if (ratio < 0.20f && (Time.time - lastWardTriggerTime) > 45f)
        {
            lastWardTriggerTime = Time.time;
            DeployAmethystWard();
            return;
        }

        // Standard Low HP warning (< 35% HP)
        if (ratio < 0.35f && (Time.time - lastHealthWarningTime) > 20f && chattinessMode != MargoChattinessMode.Quiet)
        {
            lastHealthWarningTime = Time.time;
            Speak("You're bleeding vital essence all over the flagstones. Drink an emergency draught—measuring you for a coffin is terrible for my manicure.", "margo_chime", true);
        }
    }

    private void DeployAmethystWard()
    {
        if (wardCoroutine != null) StopCoroutine(wardCoroutine);
        wardCoroutine = StartCoroutine(AmethystWardRoutine());
    }

    private IEnumerator AmethystWardRoutine()
    {
        // Audio & Visual trigger
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("margo_ward", 1.0f);
        }

        if (wardShieldSphere != null)
        {
            wardShieldSphere.SetActive(true);
            wardShieldSphere.transform.localScale = Vector3.zero;
        }

        Speak("Occult Ward deployed! Don't make me carry your corpse back to camp. <color=#BA55D3><3</color>", "margo_ward", true);

        // Emergency clutch heal of 25 HP
        if (playerProgression != null)
        {
            playerProgression.currentHp = Mathf.Min(playerProgression.maxHp, playerProgression.currentHp + 25f);
        }

        // Expanding shield animation
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            if (wardShieldSphere != null)
            {
                wardShieldSphere.transform.localScale = Vector3.one * Mathf.Lerp(0f, 2.8f, t / 0.3f);
            }
            yield return null;
        }

        yield return new WaitForSeconds(2.2f);

        // Shrinking shield
        t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            if (wardShieldSphere != null)
            {
                wardShieldSphere.transform.localScale = Vector3.one * Mathf.Lerp(2.8f, 0f, t / 0.3f);
            }
            yield return null;
        }

        if (wardShieldSphere != null) wardShieldSphere.SetActive(false);
    }

    private void HandleEdgeDullWarning()
    {
        if (chattinessMode == MargoChattinessMode.Quiet) return;
        if ((Time.time - lastTemperWarningTime) < 22f) return;
        lastTemperWarningTime = Time.time;

        Speak("Your blade's edge is as dull as Barnaby's conversation. Hone it on your whetstone, or prepare to tickle your enemies. (¬_¬)", "margo_chime");
    }

    private void HandleTemperOverheatWarning()
    {
        if (chattinessMode == MargoChattinessMode.Quiet) return;
        if ((Time.time - lastTemperWarningTime) < 18f) return;
        lastTemperWarningTime = Time.time;

        Speak("Your weapon is smoking from thermal friction. Let the lattice cool before it fractures into shrapnel.", "glitch");
    }

    private void HandleClassLoadoutChanged(PlayerClassType newClass)
    {
        if (chattinessMode == MargoChattinessMode.Quiet) return;

        string quip = newClass switch
        {
            PlayerClassType.Warrior => "Back to heavy steel and brute force? How wonderfully medieval... try not to chip the edge on gargoyle stone.",
            PlayerClassType.Mage => "Manipulating astral thermal dynamics? Remember: containment is an illusion. Don't melt your own eyebrows.",
            PlayerClassType.Bard => "A kalimba in a haunted spire? If you must pluck strings in a graveyard, delver, at least keep it in D minor. <color=#BA55D3><3</color>",
            _ => "A curious swap."
        };

        Speak(quip, "margo_chime", true);
    }

    private void HandleMineralMined(MineralRarity rarity, int yield, int value)
    {
        if (chattinessMode == MargoChattinessMode.Quiet) return;

        string gemComment = rarity switch
        {
            MineralRarity.VoidAmethyst => "Splendid harvest... Void Amethyst. Pure crystalline entropy. Bring it to my alembic at camp. <color=#BA55D3><3</color>",
            MineralRarity.PrismaticHeart => "By the Void... a Prismatic Heart! That's legendary apex grade. Keep it safe in your vault!",
            MineralRarity.CinnabarRuby => "A Cinnabar core. Highly volatile, wonderfully combustible. Good strike.",
            _ => "Neat extraction. Unrefined minerals make excellent catalysts."
        };

        Speak(gemComment, "relic_chime");
    }
    #endregion

    #region Proximity Environmental Sensors
    private void UpdateProximitySensors()
    {
        if (playerTransform == null || chattinessMode == MargoChattinessMode.Quiet) return;

        // Mineral Vein Proximity (< 7.0m)
        if ((Time.time - lastMiningAlertTime) > 25f)
        {
            var nodes = FindObjectsByType<CaveMiningNode>(FindObjectsSortMode.None);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (alertedVeins.Contains(nodes[i])) continue;

                float dist = Vector3.Distance(playerTransform.position, nodes[i].transform.position);
                if (dist < 7.0f)
                {
                    alertedVeins.Add(nodes[i]);
                    lastMiningAlertTime = Time.time;

                    string veinAdvice = nodes[i].mineralType switch
                    {
                        MineralRarity.VoidAmethyst => "Hold. A Void Amethyst vein is humming to the flank. Switch to your chisel—I need that core. <color=#BA55D3><3</color>",
                        MineralRarity.PrismaticHeart => "Look! A legendary Prismatic Heart seam! Don't walk past it without mining!",
                        MineralRarity.CinnabarRuby => "Cinnabar seam nearby. Crack it open for pyrotechnic reagents.",
                        _ => $"I detect an unrefined {nodes[i].mineralType} node nearby. Chisel the core before moving on."
                    };

                    Speak(veinAdvice, "relic_chime");
                    break;
                }
            }
        }

        // Relic / Cryptex Proximity (< 8.0m)
        if (!hasAlertedRelic && (Time.time - lastRelicAlertTime) > 30f)
        {
            var cryptex = FindAnyObjectByType<PuzzleCryptex>();
            if (cryptex != null)
            {
                float dist = Vector3.Distance(playerTransform.position, cryptex.transform.position);
                if (dist < 8.0f)
                {
                    hasAlertedRelic = true;
                    lastRelicAlertTime = Time.time;
                    Speak("An ancient Spire Cryptex rests on that dais. Study the concentric rings; align the runes with the void frequency before pulling the bolt.", "relic_chime");
                }
            }
        }
    }

    private void UpdateIdleAmbientTimer()
    {
        if (chattinessMode == MargoChattinessMode.Quiet) return;

        idleAmbientTimer += Time.deltaTime;
        float interval = chattinessMode == MargoChattinessMode.Chatty ? 45f : 80f;

        if (idleAmbientTimer >= interval && (Time.time - lastGeneralSpeechTime) >= interval)
        {
            idleAmbientTimer = 0f;
            TriggerAmbientGothicObservation();
        }
    }

    private void TriggerAmbientGothicObservation()
    {
        string[] ambientLines = {
            "The ambient temperature on this floor is plummeting. Entropy at its finest.",
            "If you perish down here, I'm calling dibs on your boots. Barnaby says they're dragon-hide.",
            "I like the gloom in this corridor. It matches my arm-warmers. (¬_¬)",
            "Stay vigilant. The Spire doesn't care about your aspirations—only thermodynamic balance.",
            "You're doing surprisingly well for someone who stumbled into the Spire by accident. Don't ruin it now. <color=#BA55D3><3</color>",
            "Listen closely to the stone. The foundation whispers in discordant frequencies."
        };

        Speak(ambientLines[UnityEngine.Random.Range(0, ambientLines.Length)], null, false, 0.5f);
    }
    #endregion

    #region On-Demand Tactical Guidance ("Ask Margo")
    /// <summary>
    /// Invoked when the player presses H, Tab, clicks Margo's floating familiar in 3D,
    /// or presses the "ASK MARGO" HUD button.
    /// Provides intelligent, context-aware room assessment with gothic flavor.
    /// </summary>
    public void ProvideOnDemandGuidance()
    {
        if (playerTransform == null) return;

        // 1. Check if Boss is actively telegraphing a slam
        if (bossController != null && bossController.IsPreparingSlam)
        {
            Speak("Aethelgard is winding up a kinetic ground-zero right now! Evade, delver!", null, true, 0.2f);
            return;
        }

        // 2. Check if Boss is alive and nearby
        if (bossController != null && bossController.currentHealth > 0f)
        {
            float bossDist = Vector3.Distance(playerTransform.position, bossController.transform.position);
            if (bossDist < 20f)
            {
                float bossHpRatio = bossController.currentHealth / bossController.maxHealth;
                if (bossHpRatio > 0.70f)
                {
                    Speak("Aethelgard's tectonic mantle is heavily reinforced. Light strikes won't dent it; stagger it with heavy combos to expose its heart core. (¬_¬)", null, true, 0.45f);
                }
                else if (bossHpRatio > 0.25f)
                {
                    Speak("Its outer armor is fracturing. Stay nimble, watch the floor for purple surge arcs, and strike during its recovery frames.", null, true, 0.45f);
                }
                else
                {
                    Speak("The Colossus is on the verge of total collapse. One aggressive barrage will reduce it to scrap and mana cores. Finish it! <color=#BA55D3><3</color>", null, true, 0.45f);
                }
                return;
            }
        }

        // 3. Check Player Vitals
        if (playerProgression != null)
        {
            float hpRatio = playerProgression.currentHp / playerProgression.maxHp;
            if (hpRatio < 0.35f)
            {
                Speak("Your astral core is leaking essence. Drink an emergency draught before you collapse onto the flagstones.", null, true, 0.45f);
                return;
            }
        }

        // 4. Check Weapon Temper
        if (weaponTemper != null && weaponTemper.HeatBuildup > 75f)
        {
            Speak("Your weapon is overheated. Step back and let the thermal stress dissipate before the blade fractures.", null, true, 0.45f);
            return;
        }

        // 5. Check nearby unmined nodes
        var nearestNode = FindNearestUnminedNode(10f);
        if (nearestNode != null)
        {
            Speak($"I sense an unmined {nearestNode.mineralType} geode right nearby. Tap with your mining chisel and bring the spoils to my alembic. <color=#BA55D3><3</color>", null, true, 0.45f);
            return;
        }

        // 6. Check Cryptex Relic
        var cryptex = FindAnyObjectByType<PuzzleCryptex>();
        if (cryptex != null && !cryptex.IsFullyUnlocked)
        {
            Speak("That Cryptex puzzle requires patience. Rotate the rings until the runes align with void resonance, then withdraw the bolt.", null, true, 0.45f);
            return;
        }

        // 7. General peaceful guidance
        string[] generalAdvice = {
            "The chamber is momentarily clear. Check your equipment temper and prepare for the next Spire shift.",
            "Quiet moments in the Spire are rare. Take a breath, delver, but keep your hand on the hilt. (¬_¬)",
            "No immediate threats detected. Just ancient dust and lingering void memories."
        };
        Speak(generalAdvice[UnityEngine.Random.Range(0, generalAdvice.Length)], null, true, 0.45f);
    }

    private CaveMiningNode FindNearestUnminedNode(float maxRange)
    {
        var nodes = FindObjectsByType<CaveMiningNode>(FindObjectsSortMode.None);
        CaveMiningNode nearest = null;
        float bestDist = maxRange;

        for (int i = 0; i < nodes.Length; i++)
        {
            float d = Vector3.Distance(playerTransform.position, nodes[i].transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = nodes[i];
            }
        }
        return nearest;
    }

    private IEnumerator InitialGreetingRoutine()
    {
        yield return new WaitForSeconds(2.8f);
        Speak("Floor 14. Keep your senses keen and don't touch uncalibrated singularities. I'm watching over you, delver. <color=#BA55D3><3</color>", null, true, 0.5f);
    }
    #endregion
}

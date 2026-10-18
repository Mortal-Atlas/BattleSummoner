using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// PersonaAndMenusHierarchyBuilder constructs the complete UI hierarchy for:
/// - Persona-style Visual Novel Dialogue overlay
/// - Inventory Menu (Messenger Bag)
/// - Settings Menu
/// - Item Shop Menu (with Merchant Balthazar)
/// - Summoning Ritual Altar Menu
/// And connects all buttons, listeners, and components in MainDungeonFloor.unity.
/// </summary>
public static class PersonaAndMenusHierarchyBuilder
{
    private const string TEXTURE_DIR = "Assets/Textures/UI/";

    [MenuItem("Tools/BattleSummoner/Build All Menus & Persona Dialogue UI")]
    public static void BuildAllMenus()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var canvasGo = GameObject.Find("PortraitHUDCanvas");
        if (canvasGo == null)
        {
            Debug.LogError("PortraitHUDCanvas not found in scene!");
            return;
        }

        Transform safeArea = canvasGo.transform.Find("SafeAreaContainer");
        Transform hudRoot = canvasGo.transform.Find("MockupMasterHUD");
        if (safeArea == null) safeArea = canvasGo.transform;

        // 1. Build Persona Dialogue Overlay
        BuildPersonaDialogueUI(canvasGo.transform);

        // 2. Build Inventory Menu Overlay
        BuildInventoryMenuUI(canvasGo.transform);

        // 3. Build Settings Menu Overlay
        BuildSettingsMenuUI(canvasGo.transform);

        // 4. Build Item Shop Menu Overlay
        BuildItemShopMenuUI(canvasGo.transform);

        // 5. Build Summoning Ritual Altar Menu Overlay
        BuildSummoningRitualUI(canvasGo.transform);

        // 6. Build Tomodachi & Social Link (Megulink / Divinatus) Hub Overlay
        BuildTomodachiSocialHubUI(canvasGo.transform);

        // 7. Build Character Customization Screen Overlay
        BuildCharacterCustomizationUI(canvasGo.transform);

        // 8. Build Multi-Person Dialogue & Text Chat Overlay
        BuildMultiPersonDialogueChatUI(canvasGo.transform);

        // 9. Connect HUD buttons on MockupMasterHUD
        ConnectHUDButtons(canvasGo, hudRoot);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("🎉 [MENUS & PERSONA UI] Successfully built and wired all menus in MainDungeonFloor.unity!");
    }

    private static Sprite GetSprite(string filename)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(TEXTURE_DIR + filename);
    }

    #region 1. Persona Dialogue Overlay
    private static void BuildPersonaDialogueUI(Transform parent)
    {
        Transform existing = parent.Find("PersonaDialogueOverlay");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject overlay = new GameObject("PersonaDialogueOverlay", typeof(RectTransform));
        overlay.transform.SetParent(parent, false);
        RectTransform ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;

        // Top Letterbox Bar
        GameObject topBar = CreateUIObject("TopLetterbox", overlay.transform, new Vector2(0f, 0.94f), new Vector2(1f, 1f));
        Image topImg = topBar.AddComponent<Image>();
        topImg.color = new Color(0.03f, 0.03f, 0.04f, 0.95f);

        // Bottom Letterbox Bar
        GameObject botBar = CreateUIObject("BottomLetterbox", overlay.transform, new Vector2(0f, 0f), new Vector2(1f, 0.06f));
        Image botImg = botBar.AddComponent<Image>();
        botImg.color = new Color(0.03f, 0.03f, 0.04f, 0.95f);

        // Character Portrait Bust (Right Side)
        GameObject portraitObj = CreateUIObject("CharacterPortraitBust", overlay.transform, new Vector2(0.50f, 0.20f), new Vector2(0.98f, 0.58f));
        Image portraitImg = portraitObj.AddComponent<Image>();
        portraitImg.sprite = GetSprite("Mockup_PersonaPortrait_Margo.png");
        portraitImg.preserveAspect = true;

        // Skewed Dialogue Box
        GameObject boxObj = CreateUIObject("PersonaDialogueBox", overlay.transform, new Vector2(0.02f, 0.10f), new Vector2(0.98f, 0.32f));
        Image boxImg = boxObj.AddComponent<Image>();
        boxImg.sprite = GetSprite("Mockup_PersonaDialogueBox.png");
        boxImg.type = Image.Type.Simple;

        // Slanted Nameplate Badge
        GameObject nameObj = CreateUIObject("PersonaNameplate", overlay.transform, new Vector2(0.04f, 0.31f), new Vector2(0.62f, 0.37f));
        Image nameImg = nameObj.AddComponent<Image>();
        nameImg.sprite = GetSprite("Mockup_PersonaNameplate.png");

        GameObject nameTextObj = CreateUIObject("SpeakerNameTMP", nameObj.transform, new Vector2(0.08f, 0.1f), new Vector2(0.95f, 0.9f));
        TextMeshProUGUI nameTMP = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "<b>MARGO</b> <size=70%><color=#FFE070>// SPIRE ARCHIVIST</color></size>";
        nameTMP.fontSize = 17;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
        nameTMP.color = Color.white;

        // Dialogue Body Text inside Box
        GameObject bodyTextObj = CreateUIObject("DialogueBodyTMP", boxObj.transform, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.86f));
        TextMeshProUGUI bodyTMP = bodyTextObj.AddComponent<TextMeshProUGUI>();
        bodyTMP.text = "Welcome to the Spire, Summoner. The ancient monoliths are awakening...";
        bodyTMP.fontSize = 17;
        bodyTMP.lineSpacing = 16f;
        bodyTMP.alignment = TextAlignmentOptions.TopLeft;
        bodyTMP.color = Color.white;

        // Flashing Advance Chevron
        GameObject chevObj = CreateUIObject("AdvanceChevron", boxObj.transform, new Vector2(0.92f, 0.10f), new Vector2(0.96f, 0.22f));
        Image chevImg = chevObj.AddComponent<Image>();
        chevImg.sprite = GetSprite("Mockup_ArrowRight.png");
        chevImg.color = new Color(1.0f, 0.85f, 0.25f, 1f);

        // Persona-style Floating Choices Container (Above dialogue box)
        GameObject choiceContainer = CreateUIObject("PersonaChoiceContainer", overlay.transform, new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.65f));

        // Add PersonaDialogueUI Component
        PersonaDialogueUI dialUI = overlay.AddComponent<PersonaDialogueUI>();
        dialUI.BindReferences(
            overlay,
            topBar.GetComponent<RectTransform>(),
            botBar.GetComponent<RectTransform>(),
            boxObj.GetComponent<RectTransform>(),
            boxImg,
            nameObj.GetComponent<RectTransform>(),
            nameTMP,
            bodyTMP,
            chevImg,
            portraitObj.GetComponent<RectTransform>(),
            portraitImg,
            choiceContainer.GetComponent<RectTransform>()
        );

        overlay.SetActive(false);
    }
    #endregion

    #region 2. Inventory Menu Overlay
    private static void BuildInventoryMenuUI(Transform parent)
    {
        Transform existing = parent.Find("InventoryMenuOverlay");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject overlay = CreateUIObject("InventoryMenuOverlay", parent, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));
        Image frameImg = overlay.AddComponent<Image>();
        frameImg.sprite = GetSprite("Mockup_InventoryFrame.png");

        // Close Button 'X'
        GameObject closeBtnObj = CreateButton("CloseBtn", overlay.transform, new Vector2(0.88f, 0.93f), new Vector2(0.97f, 0.98f), "X");

        // Header Title
        GameObject titleObj = CreateUIObject("TitleTMP", overlay.transform, new Vector2(0.06f, 0.93f), new Vector2(0.55f, 0.98f));
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<color=#FFD700><b>MESSENGER SATCHEL</b></color> <size=70%><color=#A0A0A0>// INVENTORY</color></size>";
        titleTMP.fontSize = 17;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Currency Counters
        GameObject goldObj = CreateUIObject("GoldTMP", overlay.transform, new Vector2(0.55f, 0.93f), new Vector2(0.72f, 0.98f));
        TextMeshProUGUI goldTMP = goldObj.AddComponent<TextMeshProUGUI>();
        goldTMP.text = "<color=#FFD700><b>1,450 G</b></color>";
        goldTMP.fontSize = 15;
        goldTMP.alignment = TextAlignmentOptions.MidlineRight;

        GameObject shardsObj = CreateUIObject("ShardsTMP", overlay.transform, new Vector2(0.73f, 0.93f), new Vector2(0.87f, 0.98f));
        TextMeshProUGUI shardsTMP = shardsObj.AddComponent<TextMeshProUGUI>();
        shardsTMP.text = "<color=#40EBFF><b>85 Shards</b></color>";
        shardsTMP.fontSize = 14;
        shardsTMP.alignment = TextAlignmentOptions.MidlineRight;

        // Category Filter Tabs
        GameObject tabAll = CreateButton("TabAll", overlay.transform, new Vector2(0.05f, 0.86f), new Vector2(0.22f, 0.91f), "ALL");
        GameObject tabCons = CreateButton("TabCons", overlay.transform, new Vector2(0.23f, 0.86f), new Vector2(0.42f, 0.91f), "POTIONS");
        GameObject tabWeap = CreateButton("TabWeap", overlay.transform, new Vector2(0.43f, 0.86f), new Vector2(0.61f, 0.91f), "WEAPONS");
        GameObject tabRel = CreateButton("TabRel", overlay.transform, new Vector2(0.62f, 0.86f), new Vector2(0.79f, 0.91f), "RELICS");
        GameObject tabMat = CreateButton("TabMat", overlay.transform, new Vector2(0.80f, 0.86f), new Vector2(0.95f, 0.91f), "MINERALS");

        // Grid Container
        GameObject gridObj = CreateUIObject("SlotGridContainer", overlay.transform, new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.84f));
        gridObj.AddComponent<RectMask2D>();
        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.cellSize = new Vector2(88f, 88f);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        // Item Detail Inspector (Bottom Half)
        GameObject detailPanel = CreateUIObject("DetailInspectorPanel", overlay.transform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.38f));
        Image detBg = detailPanel.AddComponent<Image>();
        detBg.color = new Color(0.06f, 0.07f, 0.10f, 0.90f);

        GameObject iconObj = CreateUIObject("DetailIcon", detailPanel.transform, new Vector2(0.04f, 0.52f), new Vector2(0.26f, 0.94f));
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = GetSprite("Mockup_PotionRed.png");

        GameObject nameText = CreateUIObject("DetailNameTMP", detailPanel.transform, new Vector2(0.28f, 0.76f), new Vector2(0.96f, 0.96f));
        TextMeshProUGUI nameTMP = nameText.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "<color=#FF6060><b>CRIMSON DRAUGHT</b></color>";
        nameTMP.fontSize = 16;

        GameObject catText = CreateUIObject("DetailCategoryTMP", detailPanel.transform, new Vector2(0.28f, 0.56f), new Vector2(0.96f, 0.74f));
        TextMeshProUGUI catTMP = catText.AddComponent<TextMeshProUGUI>();
        catTMP.text = "<color=#A0A0A0>COMMON // CONSUMABLE</color>";
        catTMP.fontSize = 12;

        GameObject descText = CreateUIObject("DetailDescTMP", detailPanel.transform, new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.52f));
        TextMeshProUGUI descTMP = descText.AddComponent<TextMeshProUGUI>();
        descTMP.text = "Standard curative flask boiled from Crimson Spire lichen.";
        descTMP.fontSize = 13;

        GameObject statsText = CreateUIObject("DetailStatsTMP", detailPanel.transform, new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.28f));
        TextMeshProUGUI statsTMP = statsText.AddComponent<TextMeshProUGUI>();
        statsTMP.text = "<color=#30FF80><b>EFFECT:</b> +250 HP Restoration</color>";
        statsTMP.fontSize = 13;

        GameObject useBtn = CreateButton("UseActionButton", detailPanel.transform, new Vector2(0.04f, 0.03f), new Vector2(0.60f, 0.14f), "[ USE POTION ]");
        GameObject discardBtn = CreateButton("DiscardActionButton", detailPanel.transform, new Vector2(0.64f, 0.03f), new Vector2(0.96f, 0.14f), "[ DISCARD ]");

        // Attach & Bind InventoryMenuUI
        InventoryMenuUI invUI = overlay.AddComponent<InventoryMenuUI>();
        SetField(invUI, "overlayRoot", overlay);
        SetField(invUI, "closeButton", closeBtnObj.GetComponent<Button>());
        SetField(invUI, "goldCurrencyTMP", goldTMP);
        SetField(invUI, "shardsCurrencyTMP", shardsTMP);
        SetField(invUI, "tabAllBtn", tabAll.GetComponent<Button>());
        SetField(invUI, "tabConsumablesBtn", tabCons.GetComponent<Button>());
        SetField(invUI, "tabWeaponsBtn", tabWeap.GetComponent<Button>());
        SetField(invUI, "tabRelicsBtn", tabRel.GetComponent<Button>());
        SetField(invUI, "tabMaterialsBtn", tabMat.GetComponent<Button>());
        SetField(invUI, "slotGridContainer", gridObj.transform);
        SetField(invUI, "detailIconImg", iconImg);
        SetField(invUI, "detailNameTMP", nameTMP);
        SetField(invUI, "detailRarityCategoryTMP", catTMP);
        SetField(invUI, "detailDescriptionTMP", descTMP);
        SetField(invUI, "detailStatsTMP", statsTMP);
        SetField(invUI, "useActionButton", useBtn.GetComponent<Button>());
        SetField(invUI, "useActionLabelTMP", useBtn.GetComponentInChildren<TextMeshProUGUI>());
        SetField(invUI, "discardActionButton", discardBtn.GetComponent<Button>());

        overlay.SetActive(false);
    }
    #endregion

    #region 3. Settings Menu Overlay
    private static void BuildSettingsMenuUI(Transform parent)
    {
        Transform existing = parent.Find("SettingsMenuOverlay");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject overlay = CreateUIObject("SettingsMenuOverlay", parent, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));
        Image frameImg = overlay.AddComponent<Image>();
        frameImg.sprite = GetSprite("Mockup_SettingsPanel.png");

        // Close Button
        GameObject closeBtn = CreateButton("CloseBtn", overlay.transform, new Vector2(0.88f, 0.93f), new Vector2(0.97f, 0.98f), "X");

        // Title
        GameObject titleObj = CreateUIObject("TitleTMP", overlay.transform, new Vector2(0.06f, 0.93f), new Vector2(0.70f, 0.98f));
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<color=#20D8FF><b>SYSTEM SETTINGS</b></color> <size=70%><color=#A0A0A0>// SPIRE ARCHIVE</color></size>";
        titleTMP.fontSize = 17;

        // Tabs
        GameObject tabAudio = CreateButton("TabAudio", overlay.transform, new Vector2(0.06f, 0.86f), new Vector2(0.34f, 0.91f), "AUDIO");
        GameObject tabGraph = CreateButton("TabGraphics", overlay.transform, new Vector2(0.36f, 0.86f), new Vector2(0.64f, 0.91f), "GRAPHICS");
        GameObject tabGame = CreateButton("TabGameplay", overlay.transform, new Vector2(0.66f, 0.86f), new Vector2(0.94f, 0.91f), "GAMEPLAY");

        // Audio Panel
        GameObject audioPanel = CreateUIObject("AudioPanel", overlay.transform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.84f));
        var masterSlider = CreateSettingSlider("MasterVolume", audioPanel.transform, 0.80f, "MASTER VOLUME", out var masterLabel);
        var bgmSlider = CreateSettingSlider("BgmVolume", audioPanel.transform, 0.60f, "BGM MUSIC", out var bgmLabel);
        var sfxSlider = CreateSettingSlider("SfxVolume", audioPanel.transform, 0.40f, "COMBAT SFX", out var sfxLabel);
        var voiceSlider = CreateSettingSlider("VoiceVolume", audioPanel.transform, 0.20f, "VOICE / DIALOGUE", out var voiceLabel);

        // Graphics Panel
        GameObject graphicsPanel = CreateUIObject("GraphicsPanel", overlay.transform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.84f));
        var fovSlider = CreateSettingSlider("CameraFOV", graphicsPanel.transform, 0.75f, "PORTRAIT CAMERA FOV", out var fovLabel);
        GameObject fpsCapBtn = CreateButton("FpsCapBtn", graphicsPanel.transform, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.62f), "TARGET FRAMERATE: 60 FPS");
        var bloomToggle = CreateSettingToggle("BloomToggle", graphicsPanel.transform, 0.38f, "BLOOM & GLOW POST-VFX");
        var shadowsToggle = CreateSettingToggle("ShadowsToggle", graphicsPanel.transform, 0.24f, "DYNAMIC SHADOWS");

        // Gameplay Panel
        GameObject gameplayPanel = CreateUIObject("GameplayPanel", overlay.transform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.84f));
        var lookSensSlider = CreateSettingSlider("LookSensitivity", gameplayPanel.transform, 0.75f, "CAMERA SENSITIVITY", out var lookSensLabel);
        var aimAssistToggle = CreateSettingToggle("AimAssistToggle", gameplayPanel.transform, 0.55f, "AUTO-AIM TARGET LOCK");
        var gestureTolSlider = CreateSettingSlider("GestureTolerance", gameplayPanel.transform, 0.35f, "RUNE STROKE TOLERANCE", out var gestureTolLabel);
        var shakeToggle = CreateSettingToggle("ScreenShakeToggle", gameplayPanel.transform, 0.18f, "HAPTIC SCREEN SHAKE");

        // Footer Buttons
        GameObject saveBtn = CreateButton("SaveBtn", overlay.transform, new Vector2(0.06f, 0.03f), new Vector2(0.55f, 0.09f), "[ SAVE & APPLY ]");
        GameObject resetBtn = CreateButton("ResetBtn", overlay.transform, new Vector2(0.58f, 0.03f), new Vector2(0.94f, 0.09f), "[ RESET DEFAULTS ]");

        // Attach & Bind SettingsMenuUI
        SettingsMenuUI setUI = overlay.AddComponent<SettingsMenuUI>();
        SetField(setUI, "overlayRoot", overlay);
        SetField(setUI, "closeButton", closeBtn.GetComponent<Button>());
        SetField(setUI, "audioTabBtn", tabAudio.GetComponent<Button>());
        SetField(setUI, "graphicsTabBtn", tabGraph.GetComponent<Button>());
        SetField(setUI, "gameplayTabBtn", tabGame.GetComponent<Button>());
        SetField(setUI, "audioPanel", audioPanel);
        SetField(setUI, "graphicsPanel", graphicsPanel);
        SetField(setUI, "gameplayPanel", gameplayPanel);

        SetField(setUI, "masterVolumeSlider", masterSlider);
        SetField(setUI, "masterVolumeLabel", masterLabel);
        SetField(setUI, "bgmVolumeSlider", bgmSlider);
        SetField(setUI, "bgmVolumeLabel", bgmLabel);
        SetField(setUI, "sfxVolumeSlider", sfxSlider);
        SetField(setUI, "sfxVolumeLabel", sfxLabel);
        SetField(setUI, "voiceVolumeSlider", voiceSlider);
        SetField(setUI, "voiceVolumeLabel", voiceLabel);

        SetField(setUI, "fpsCapToggleBtn", fpsCapBtn.GetComponent<Button>());
        SetField(setUI, "fpsCapLabel", fpsCapBtn.GetComponentInChildren<TextMeshProUGUI>());
        SetField(setUI, "cameraFovSlider", fovSlider);
        SetField(setUI, "cameraFovLabel", fovLabel);
        SetField(setUI, "bloomVfxToggle", bloomToggle);
        SetField(setUI, "dynamicShadowsToggle", shadowsToggle);

        SetField(setUI, "lookSensitivitySlider", lookSensSlider);
        SetField(setUI, "lookSensitivityLabel", lookSensLabel);
        SetField(setUI, "autoAimAssistToggle", aimAssistToggle);
        SetField(setUI, "gestureToleranceSlider", gestureTolSlider);
        SetField(setUI, "gestureToleranceLabel", gestureTolLabel);
        SetField(setUI, "screenShakeToggle", shakeToggle);

        SetField(setUI, "saveAndApplyButton", saveBtn.GetComponent<Button>());
        SetField(setUI, "resetDefaultsButton", resetBtn.GetComponent<Button>());

        overlay.SetActive(false);
    }
    #endregion

    #region 4. Item Shop Menu Overlay
    private static void BuildItemShopMenuUI(Transform parent)
    {
        Transform existing = parent.Find("ItemShopOverlay");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject overlay = CreateUIObject("ItemShopOverlay", parent, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));
        Image frameImg = overlay.AddComponent<Image>();
        frameImg.sprite = GetSprite("Mockup_InventoryFrame.png");

        // Canopy Banner
        GameObject canopyObj = CreateUIObject("CanopyBanner", overlay.transform, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f));
        Image canopyImg = canopyObj.AddComponent<Image>();
        canopyImg.sprite = GetSprite("Mockup_ShopBanner.png");

        // Close Button
        GameObject closeBtn = CreateButton("CloseBtn", overlay.transform, new Vector2(0.88f, 0.91f), new Vector2(0.96f, 0.97f), "X");

        // Merchant Title
        GameObject titleObj = CreateUIObject("MerchantTitleTMP", overlay.transform, new Vector2(0.08f, 0.90f), new Vector2(0.70f, 0.96f));
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<color=#FFD700><b>BALTHAZAR'S SPIRE EMPORIUM</b></color>\n<size=70%><color=#E0E0E0>Purveyor of Curiosities</color></size>";
        titleTMP.fontSize = 15;

        // Player Wallet Display
        GameObject walletObj = CreateUIObject("WalletTMP", overlay.transform, new Vector2(0.06f, 0.82f), new Vector2(0.55f, 0.87f));
        TextMeshProUGUI walletTMP = walletObj.AddComponent<TextMeshProUGUI>();
        walletTMP.text = "<color=#FFD700><b>1,450 G</b></color> | <color=#40EBFF><b>85 Shards</b></color>";
        walletTMP.fontSize = 15;

        // Talk to Merchant Button (Triggers Persona VN Dialogue)
        GameObject talkBtn = CreateButton("TalkToMerchantBtn", overlay.transform, new Vector2(0.58f, 0.82f), new Vector2(0.94f, 0.87f), "[ TALK // LORE ]");

        // Buy and Sell Mode Tabs
        GameObject buyTab = CreateButton("BuyTabBtn", overlay.transform, new Vector2(0.06f, 0.75f), new Vector2(0.48f, 0.80f), "BUY WARES");
        GameObject sellTab = CreateButton("SellTabBtn", overlay.transform, new Vector2(0.52f, 0.75f), new Vector2(0.94f, 0.80f), "SELL LOOT");

        // Wares Grid Container
        GameObject gridObj = CreateUIObject("WaresGridContainer", overlay.transform, new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.73f));
        gridObj.AddComponent<RectMask2D>();
        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.cellSize = new Vector2(88f, 88f);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        // Checkout Inspector Panel (Bottom)
        GameObject checkoutPanel = CreateUIObject("CheckoutPanel", overlay.transform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.36f));
        Image checkBg = checkoutPanel.AddComponent<Image>();
        checkBg.color = new Color(0.06f, 0.07f, 0.10f, 0.90f);

        GameObject iconObj = CreateUIObject("WareIcon", checkoutPanel.transform, new Vector2(0.04f, 0.52f), new Vector2(0.26f, 0.94f));
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = GetSprite("Mockup_PotionRed.png");

        GameObject nameText = CreateUIObject("WareNameTMP", checkoutPanel.transform, new Vector2(0.28f, 0.74f), new Vector2(0.96f, 0.96f));
        TextMeshProUGUI nameTMP = nameText.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "<color=#FF6060><b>CRIMSON DRAUGHT</b></color>";
        nameTMP.fontSize = 16;

        GameObject priceText = CreateUIObject("WarePriceTMP", checkoutPanel.transform, new Vector2(0.28f, 0.52f), new Vector2(0.96f, 0.72f));
        TextMeshProUGUI priceTMP = priceText.AddComponent<TextMeshProUGUI>();
        priceTMP.text = "<color=#FFD700><b>PRICE: 50 Gold</b></color>";
        priceTMP.fontSize = 14;

        GameObject loreText = CreateUIObject("WareLoreTMP", checkoutPanel.transform, new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.50f));
        TextMeshProUGUI loreTMP = loreText.AddComponent<TextMeshProUGUI>();
        loreTMP.text = "Restorative tonic boiled from Crimson Spire lichen.";
        loreTMP.fontSize = 13;

        GameObject statsText = CreateUIObject("WareStatsTMP", checkoutPanel.transform, new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.28f));
        TextMeshProUGUI statsTMP = statsText.AddComponent<TextMeshProUGUI>();
        statsTMP.text = "<color=#30FF80><b>EFFECT:</b> +250 HP Restoration</color>";
        statsTMP.fontSize = 13;

        // Quantity Selector & Purchase Button
        GameObject decBtn = CreateButton("DecQtyBtn", checkoutPanel.transform, new Vector2(0.04f, 0.03f), new Vector2(0.16f, 0.14f), "-");
        GameObject qtyObj = CreateUIObject("QtyLabelTMP", checkoutPanel.transform, new Vector2(0.18f, 0.03f), new Vector2(0.28f, 0.14f));
        TextMeshProUGUI qtyTMP = qtyObj.AddComponent<TextMeshProUGUI>();
        qtyTMP.text = "1";
        qtyTMP.fontSize = 16;
        qtyTMP.alignment = TextAlignmentOptions.Center;

        GameObject incBtn = CreateButton("IncQtyBtn", checkoutPanel.transform, new Vector2(0.30f, 0.03f), new Vector2(0.42f, 0.14f), "+");
        GameObject buyBtn = CreateButton("BuyActionBtn", checkoutPanel.transform, new Vector2(0.46f, 0.03f), new Vector2(0.96f, 0.14f), "[ PURCHASE ( 50 G ) ]");

        // Attach & Bind ItemShopMenuUI
        ItemShopMenuUI shopUI = overlay.AddComponent<ItemShopMenuUI>();
        SetField(shopUI, "overlayRoot", overlay);
        SetField(shopUI, "closeButton", closeBtn.GetComponent<Button>());
        SetField(shopUI, "merchantTitleTMP", titleTMP);
        SetField(shopUI, "walletCurrencyTMP", walletTMP);
        SetField(shopUI, "talkToMerchantButton", talkBtn.GetComponent<Button>());
        SetField(shopUI, "buyTabBtn", buyTab.GetComponent<Button>());
        SetField(shopUI, "sellTabBtn", sellTab.GetComponent<Button>());
        SetField(shopUI, "waresGridContainer", gridObj.transform);
        SetField(shopUI, "detailIconImg", iconImg);
        SetField(shopUI, "detailTitleTMP", nameTMP);
        SetField(shopUI, "detailPriceTMP", priceTMP);
        SetField(shopUI, "detailLoreTMP", loreTMP);
        SetField(shopUI, "detailStatsTMP", statsTMP);
        SetField(shopUI, "decrementQtyBtn", decBtn.GetComponent<Button>());
        SetField(shopUI, "incrementQtyBtn", incBtn.GetComponent<Button>());
        SetField(shopUI, "quantityLabelTMP", qtyTMP);
        SetField(shopUI, "transactionActionButton", buyBtn.GetComponent<Button>());
        SetField(shopUI, "transactionActionTMP", buyBtn.GetComponentInChildren<TextMeshProUGUI>());

        overlay.SetActive(false);
    }
    #endregion

    #region 5. Summoning Ritual Altar Menu Overlay
    private static void BuildSummoningRitualUI(Transform parent)
    {
        Transform existing = parent.Find("SummoningRitualOverlay");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject overlay = CreateUIObject("SummoningRitualOverlay", parent, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));
        Image frameImg = overlay.AddComponent<Image>();
        frameImg.sprite = GetSprite("Mockup_SummoningAltar.png");

        // Close Button
        GameObject closeBtn = CreateButton("CloseBtn", overlay.transform, new Vector2(0.88f, 0.93f), new Vector2(0.97f, 0.98f), "X");

        // Title Header
        GameObject titleObj = CreateUIObject("TitleTMP", overlay.transform, new Vector2(0.06f, 0.92f), new Vector2(0.70f, 0.98f));
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<color=#FFD700><b>ASTRAL SUMMONING ALTAR</b></color>\n<size=70%><color=#40EBFF>Patronus Synthesis Engine</color></size>";
        titleTMP.fontSize = 16;

        // Step 1: Nomenclature Seed & Catalyst Panel
        GameObject step1Panel = CreateUIObject("Step1Panel", overlay.transform, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f));
        GameObject seedInputObj = CreateUIObject("SeedInputField", step1Panel.transform, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.82f));
        Image inputBg = seedInputObj.AddComponent<Image>();
        inputBg.color = new Color(0.10f, 0.11f, 0.16f, 0.95f);
        TMP_InputField seedInput = seedInputObj.AddComponent<TMP_InputField>();
        GameObject seedTextObj = CreateUIObject("Text", seedInputObj.transform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));
        TextMeshProUGUI seedTextTMP = seedTextObj.AddComponent<TextMeshProUGUI>();
        seedTextTMP.text = "Aegis";
        seedTextTMP.fontSize = 18;
        seedTextTMP.alignment = TextAlignmentOptions.MidlineLeft;
        seedInput.textComponent = seedTextTMP;
        seedInput.text = "Aegis";

        GameObject catLabelObj = CreateUIObject("CatalystLabelTMP", step1Panel.transform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.66f));
        TextMeshProUGUI catLabelTMP = catLabelObj.AddComponent<TextMeshProUGUI>();
        catLabelTMP.text = "SELECT ESSENCE CATALYST:";
        catLabelTMP.fontSize = 14;

        GameObject catCel = CreateButton("CatCelestial", step1Panel.transform, new Vector2(0.08f, 0.44f), new Vector2(0.48f, 0.54f), "CELESTIAL STARLIGHT");
        GameObject catInf = CreateButton("CatInfernal", step1Panel.transform, new Vector2(0.52f, 0.44f), new Vector2(0.92f, 0.54f), "INFERNAL PYRE");
        GameObject catAby = CreateButton("CatAbyssal", step1Panel.transform, new Vector2(0.08f, 0.32f), new Vector2(0.48f, 0.42f), "ABYSSAL VOID");
        GameObject catVer = CreateButton("CatVerdant", step1Panel.transform, new Vector2(0.52f, 0.32f), new Vector2(0.92f, 0.42f), "VERDANT EARTH");

        GameObject activeCatObj = CreateUIObject("ActiveCatalystTMP", step1Panel.transform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.28f));
        TextMeshProUGUI activeCatTMP = activeCatObj.AddComponent<TextMeshProUGUI>();
        activeCatTMP.text = "CATALYST: <color=#FFD700><b>CELESTIAL STARLIGHT</b></color>";
        activeCatTMP.fontSize = 14;
        activeCatTMP.alignment = TextAlignmentOptions.Center;

        GameObject beginInqBtn = CreateButton("BeginInquisitionBtn", step1Panel.transform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.16f), "[ BEGIN INQUISITION TRIAL ]");

        // Step 2: The LLM Inquisition Panel
        GameObject step2Panel = CreateUIObject("Step2Panel", overlay.transform, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f));
        GameObject qTitleObj = CreateUIObject("QuestionTitleTMP", step2Panel.transform, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.88f));
        TextMeshProUGUI qTitleTMP = qTitleObj.AddComponent<TextMeshProUGUI>();
        qTitleTMP.text = "<color=#FFD700><b>THE LLM INQUISITION</b></color>";
        qTitleTMP.fontSize = 17;

        GameObject qBodyObj = CreateUIObject("QuestionBodyTMP", step2Panel.transform, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.78f));
        TextMeshProUGUI qBodyTMP = qBodyObj.AddComponent<TextMeshProUGUI>();
        qBodyTMP.text = "\"When darkness engulfs the Spire, what truth guides your blade?\"";
        qBodyTMP.fontSize = 15;

        GameObject choiceA = CreateButton("ChoiceA", step2Panel.transform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.48f), "[A] In shielding those who cannot fight.");
        GameObject choiceB = CreateButton("ChoiceB", step2Panel.transform, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.35f), "[B] In incinerating all foes in our path.");
        GameObject choiceC = CreateButton("ChoiceC", step2Panel.transform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.22f), "[C] In decoding ancient cosmic mysteries.");

        // Step 3: Astral Geometry Panel
        GameObject step3Panel = CreateUIObject("Step3Panel", overlay.transform, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f));
        GameObject geomInstrObj = CreateUIObject("GeomInstrTMP", step3Panel.transform, new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.85f));
        TextMeshProUGUI geomInstrTMP = geomInstrObj.AddComponent<TextMeshProUGUI>();
        geomInstrTMP.text = "Inscribe the sacred magic circle to channel starlight.";
        geomInstrTMP.fontSize = 15;

        GameObject geomResObj = CreateUIObject("GeomResultTMP", step3Panel.transform, new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.65f));
        TextMeshProUGUI geomResTMP = geomResObj.AddComponent<TextMeshProUGUI>();
        geomResTMP.text = "<color=#30FF80><b>RITUAL ACCURACY: 94.5%</b></color>";
        geomResTMP.fontSize = 16;
        geomResTMP.alignment = TextAlignmentOptions.Center;

        GameObject simCircleBtn = CreateButton("SimCircleBtn", step3Panel.transform, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.38f), "[ INSCRIBE SACRED CIRCLE ]");
        GameObject proceedSynthBtn = CreateButton("ProceedSynthBtn", step3Panel.transform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.22f), "[ PROCEED TO SYNTHESIS ]");

        // Step 4: Synthesis Panel
        GameObject step4Panel = CreateUIObject("Step4Panel", overlay.transform, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f));
        GameObject synthPortraitObj = CreateUIObject("SynthPortrait", step4Panel.transform, new Vector2(0.28f, 0.52f), new Vector2(0.72f, 0.88f));
        Image synthPortraitImg = synthPortraitObj.AddComponent<Image>();
        synthPortraitImg.sprite = GetSprite("Mockup_PersonaPortrait_Demon.png");
        synthPortraitImg.preserveAspect = true;

        GameObject synthNameObj = CreateUIObject("SynthNameTMP", step4Panel.transform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.50f));
        TextMeshProUGUI synthNameTMP = synthNameObj.AddComponent<TextMeshProUGUI>();
        synthNameTMP.text = "<color=#FFD700><b>AEGIS THE IMMORTAL</b></color>";
        synthNameTMP.fontSize = 18;
        synthNameTMP.alignment = TextAlignmentOptions.Center;

        GameObject synthAffObj = CreateUIObject("SynthAffTMP", step4Panel.transform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.40f));
        TextMeshProUGUI synthAffTMP = synthAffObj.AddComponent<TextMeshProUGUI>();
        synthAffTMP.text = "AFFINITY: CELESTIAL | TEMPERAMENT: LOYAL";
        synthAffTMP.fontSize = 13;
        synthAffTMP.alignment = TextAlignmentOptions.Center;

        GameObject synthStatsObj = CreateUIObject("SynthStatsTMP", step4Panel.transform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.32f));
        TextMeshProUGUI synthStatsTMP = synthStatsObj.AddComponent<TextMeshProUGUI>();
        synthStatsTMP.text = "<color=#30FF80><b>HP: 850</b></color> | <color=#FF6060><b>ATK: 68</b></color>\n<color=#40EBFF><b>QUIRK:</b> StaggerMaster</color>";
        synthStatsTMP.fontSize = 14;
        synthStatsTMP.alignment = TextAlignmentOptions.Center;

        GameObject spawnWorldBtn = CreateButton("SpawnWorldBtn", step4Panel.transform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.15f), "[ SUMMON INTO WORLD ]");

        // Attach & Bind SummoningRitualUI
        SummoningRitualUI ritUI = overlay.AddComponent<SummoningRitualUI>();
        SetField(ritUI, "overlayRoot", overlay);
        SetField(ritUI, "closeButton", closeBtn.GetComponent<Button>());
        SetField(ritUI, "step1SeedPanel", step1Panel);
        SetField(ritUI, "step2InquisitionPanel", step2Panel);
        SetField(ritUI, "step3GeometryPanel", step3Panel);
        SetField(ritUI, "step4SynthesisPanel", step4Panel);

        SetField(ritUI, "seedNameInputField", seedInput);
        SetField(ritUI, "catalystCelestialBtn", catCel.GetComponent<Button>());
        SetField(ritUI, "catalystInfernalBtn", catInf.GetComponent<Button>());
        SetField(ritUI, "catalystAbyssalBtn", catAby.GetComponent<Button>());
        SetField(ritUI, "catalystVerdantBtn", catVer.GetComponent<Button>());
        SetField(ritUI, "selectedCatalystTMP", activeCatTMP);
        SetField(ritUI, "beginInquisitionBtn", beginInqBtn.GetComponent<Button>());

        SetField(ritUI, "questionTitleTMP", qTitleTMP);
        SetField(ritUI, "questionBodyTMP", qBodyTMP);
        SetField(ritUI, "inquisitionChoiceABtn", choiceA.GetComponent<Button>());
        SetField(ritUI, "inquisitionChoiceBBtn", choiceB.GetComponent<Button>());
        SetField(ritUI, "inquisitionChoiceCBtn", choiceC.GetComponent<Button>());
        SetField(ritUI, "inquisitionChoiceATMP", choiceA.GetComponentInChildren<TextMeshProUGUI>());
        SetField(ritUI, "inquisitionChoiceBTMP", choiceB.GetComponentInChildren<TextMeshProUGUI>());
        SetField(ritUI, "inquisitionChoiceCTMP", choiceC.GetComponentInChildren<TextMeshProUGUI>());

        SetField(ritUI, "geometryInstructionTMP", geomInstrTMP);
        SetField(ritUI, "circleAccuracyResultTMP", geomResTMP);
        SetField(ritUI, "drawSimulateCircleBtn", simCircleBtn.GetComponent<Button>());
        SetField(ritUI, "proceedToSynthesisBtn", proceedSynthBtn.GetComponent<Button>());

        SetField(ritUI, "synthesizedPortraitImg", synthPortraitImg);
        SetField(ritUI, "synthesizedNameTMP", synthNameTMP);
        SetField(ritUI, "synthesizedAffinityTMP", synthAffTMP);
        SetField(ritUI, "synthesizedStatsTMP", synthStatsTMP);
        SetField(ritUI, "summonIntoWorldButton", spawnWorldBtn.GetComponent<Button>());

        overlay.SetActive(false);
    }
    #endregion

    #region 6. Tomodachi & Social Link Hub Overlay
    private static void BuildTomodachiSocialHubUI(Transform parent)
    {
        Transform existing = parent.Find("TomodachiSocialHubOverlay");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        // Ensure TomodachiSocialLinkManager is present in the scene
        var mgr = parent.GetComponentInChildren<BattleSummoner.Social.TomodachiSocialLinkManager>();
        if (mgr == null)
        {
            mgr = parent.gameObject.AddComponent<BattleSummoner.Social.TomodachiSocialLinkManager>();
        }
        mgr.InitializeDefaultRoster();

        // Main Modal Frame: Centered & Safe Area Responsive
        GameObject overlay = CreateUIObject("TomodachiSocialHubOverlay", parent, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f));
        Image frameImg = overlay.AddComponent<Image>();
        frameImg.sprite = GetSprite("Mockup_SocialHubFrame.png");
        frameImg.type = Image.Type.Simple;

        // Top Header Bar
        GameObject headerObj = CreateUIObject("HeaderBar", overlay.transform, new Vector2(0.03f, 0.92f), new Vector2(0.97f, 0.985f));
        GameObject titleObj = CreateUIObject("TitleTMP", headerObj.transform, new Vector2(0.03f, 0f), new Vector2(0.85f, 1f));
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<color=#FFD700><b>FAMIGOTCHI PET CARE</b></color> // <size=70%><color=#40EBFF>FAMILIAR SANCTUARY</color></size>";
        titleTMP.fontSize = 15;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Close Button [X]
        GameObject closeBtn = CreateButton("CloseBtn", headerObj.transform, new Vector2(0.88f, 0.10f), new Vector2(0.98f, 0.90f), "X");

        // Category Filter Tab Strip (FAMILIARS first!)
        GameObject tabStrip = CreateUIObject("CategoryTabStrip", overlay.transform, new Vector2(0.03f, 0.865f), new Vector2(0.97f, 0.915f));
        GameObject tabFamiliars = CreateButton("TabFamiliars", tabStrip.transform, new Vector2(0.00f, 0f), new Vector2(0.22f, 1f), "FAMILIARS");
        GameObject tabAll = CreateButton("TabAll", tabStrip.transform, new Vector2(0.24f, 0f), new Vector2(0.42f, 1f), "ALL");
        GameObject tabNpcs = CreateButton("TabNpcs", tabStrip.transform, new Vector2(0.44f, 0f), new Vector2(0.62f, 1f), "NPCS");
        GameObject tabSummons = CreateButton("TabSummons", tabStrip.transform, new Vector2(0.64f, 0f), new Vector2(0.81f, 1f), "SUMMONS");
        GameObject tabConstructs = CreateButton("TabConstructs", tabStrip.transform, new Vector2(0.83f, 0f), new Vector2(0.99f, 1f), "CONSTRUCTS");

        // Split Layout: Left Roster List (0.03 to 0.36), Right Inspector Showcase (0.38 to 0.97)
        // LEFT: Roster Scroll Panel
        GameObject rosterPanel = CreateUIObject("RosterPanel", overlay.transform, new Vector2(0.03f, 0.03f), new Vector2(0.36f, 0.855f));
        Image rbg = rosterPanel.AddComponent<Image>();
        rbg.color = new Color(0.05f, 0.06f, 0.09f, 0.85f);

        GameObject rHeader = CreateUIObject("RosterHeaderTMP", rosterPanel.transform, new Vector2(0.04f, 0.93f), new Vector2(0.96f, 0.99f));
        TextMeshProUGUI rHeadTmp = rHeader.AddComponent<TextMeshProUGUI>();
        rHeadTmp.text = "<color=#FFD700><b>SANCTUARY ROSTER</b></color>";
        rHeadTmp.fontSize = 12;
        rHeadTmp.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject rosterScrollObj = CreateUIObject("RosterScrollArea", rosterPanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.92f));
        rosterScrollObj.AddComponent<RectMask2D>();
        VerticalLayoutGroup rLayout = rosterScrollObj.AddComponent<VerticalLayoutGroup>();
        rLayout.spacing = 6f;
        rLayout.childAlignment = TextAnchor.UpperCenter;
        rLayout.childControlWidth = true;
        rLayout.childControlHeight = false;
        rLayout.childForceExpandWidth = true;
        rLayout.childForceExpandHeight = false;

        // RIGHT: Inspector & Pet Familiar Care Sanctuary Panel
        GameObject inspectorPanel = CreateUIObject("InspectorPanel", overlay.transform, new Vector2(0.38f, 0.03f), new Vector2(0.97f, 0.855f));
        Image ibg = inspectorPanel.AddComponent<Image>();
        ibg.color = new Color(0.05f, 0.06f, 0.09f, 0.85f);

        // Arcana Tarot Card Backing & Character Portrait Bust
        GameObject cardObj = CreateUIObject("ArcanaCardBacking", inspectorPanel.transform, new Vector2(0.56f, 0.54f), new Vector2(0.98f, 0.98f));
        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.sprite = GetSprite("Mockup_SocialArcanaCard.png");
        cardImg.preserveAspect = true;

        GameObject portObj = CreateUIObject("CharacterPortraitBust", inspectorPanel.transform, new Vector2(0.56f, 0.54f), new Vector2(0.98f, 0.98f));
        Image portImg = portObj.AddComponent<Image>();
        portImg.sprite = GetSprite("Mockup_SocialPortrait_Pygmy.png");
        portImg.preserveAspect = true;

        // Direct Touch Petting Area over Familiar Bust
        GameObject petAreaObj = CreateUIObject("PetClickArea", inspectorPanel.transform, new Vector2(0.56f, 0.54f), new Vector2(0.98f, 0.98f));
        Image petAreaImg = petAreaObj.AddComponent<Image>();
        petAreaImg.color = Color.clear;
        Button petClickBtn = petAreaObj.AddComponent<Button>();

        // Floating Hearts FX Container
        GameObject heartsFxObj = CreateUIObject("HeartsFxContainer", inspectorPanel.transform, new Vector2(0.56f, 0.54f), new Vector2(0.98f, 0.98f));
        RectTransform heartsFxRt = heartsFxObj.GetComponent<RectTransform>();

        // Dynamic Speech / Thought Bubble above Pet Familiar
        GameObject bubbleObj = CreateUIObject("FamiliarSpeechBubble", inspectorPanel.transform, new Vector2(0.04f, 0.53f), new Vector2(0.54f, 0.71f));
        Image bubbleBg = bubbleObj.AddComponent<Image>();
        bubbleBg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
        GameObject bubbleTxtObj = CreateUIObject("SpeechTMP", bubbleObj.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
        TextMeshProUGUI bubbleTMP = bubbleTxtObj.AddComponent<TextMeshProUGUI>();
        bubbleTMP.text = "*Flutters gossamer wings* <i>\"Master is the best! Pygmy feels wonderful today!\"</i>";
        bubbleTMP.fontSize = 10;
        bubbleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Identification & Badges
        GameObject nameObj = CreateUIObject("NameTMP", inspectorPanel.transform, new Vector2(0.04f, 0.91f), new Vector2(0.54f, 0.98f));
        TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "<b>PYGMY SPRITE</b>";
        nameTMP.fontSize = 15;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject subObj = CreateUIObject("SubtitleTMP", inspectorPanel.transform, new Vector2(0.04f, 0.85f), new Vector2(0.54f, 0.91f));
        TextMeshProUGUI subTMP = subObj.AddComponent<TextMeshProUGUI>();
        subTMP.text = "<color=#C0C5D5>GOSSAMER FAIRY COMPANION</color>";
        subTMP.fontSize = 10;
        subTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject arcanaObj = CreateUIObject("ArcanaRankTMP", inspectorPanel.transform, new Vector2(0.04f, 0.79f), new Vector2(0.54f, 0.85f));
        TextMeshProUGUI arcanaTMP = arcanaObj.AddComponent<TextMeshProUGUI>();
        arcanaTMP.text = "<color=#FFD700><b>THE STAR</b></color> // <color=#40EBFF>RANK 4</color>";
        arcanaTMP.fontSize = 10;
        arcanaTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject moodObj = CreateUIObject("MoodBadgeTMP", inspectorPanel.transform, new Vector2(0.04f, 0.72f), new Vector2(0.54f, 0.79f));
        TextMeshProUGUI moodTMP = moodObj.AddComponent<TextMeshProUGUI>();
        moodTMP.text = "✨ [ECSTATIC]";
        moodTMP.fontSize = 11;
        moodTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // General Affinity Slider (Shared with NPCs)
        GameObject affContainer = CreateUIObject("AffinityMeterContainer", inspectorPanel.transform, new Vector2(0.04f, 0.49f), new Vector2(0.54f, 0.54f));
        GameObject affValObj = CreateUIObject("AffinityValueTMP", affContainer.transform, new Vector2(0f, 0.5f), new Vector2(0.50f, 1f));
        TextMeshProUGUI affValTMP = affValObj.AddComponent<TextMeshProUGUI>();
        affValTMP.text = "+50 / 100";
        affValTMP.fontSize = 10;
        affValTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject tierObj = CreateUIObject("TierTagTMP", affContainer.transform, new Vector2(0.52f, 0.5f), new Vector2(1f, 1f));
        TextMeshProUGUI tierTMP = tierObj.AddComponent<TextMeshProUGUI>();
        tierTMP.text = "<color=#FFD700><b>SOULBOUND</b></color>";
        tierTMP.fontSize = 10;
        tierTMP.alignment = TextAlignmentOptions.MidlineRight;

        GameObject sliderObj = CreateUIObject("AffinitySlider", affContainer.transform, new Vector2(0f, 0f), new Vector2(1f, 0.45f));
        Slider affSlider = sliderObj.AddComponent<Slider>();
        GameObject sBg = CreateUIObject("Background", sliderObj.transform, Vector2.zero, Vector2.one);
        Image sBgImg = sBg.AddComponent<Image>();
        sBgImg.color = new Color(0.10f, 0.12f, 0.16f, 1f);
        GameObject sFillArea = CreateUIObject("FillArea", sliderObj.transform, Vector2.zero, Vector2.one);
        GameObject sFill = CreateUIObject("Fill", sFillArea.transform, Vector2.zero, Vector2.one);
        Image sFillImg = sFill.AddComponent<Image>();
        sFillImg.color = new Color(1.0f, 0.85f, 0.20f, 1f);
        affSlider.fillRect = sFill.GetComponent<RectTransform>();
        affSlider.minValue = 0f;
        affSlider.maxValue = 1f;
        affSlider.value = 0.75f;

        // Tamagotchi Need Bars Container (Fullness, Hydration, Cleanliness, Happiness)
        GameObject vitalsContainer = CreateUIObject("PetCareVitalsContainer", inspectorPanel.transform, new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.49f));
        Image vitalsBg = vitalsContainer.AddComponent<Image>();
        vitalsBg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);

        TextMeshProUGUI fullnessValTMP, hydrationValTMP, cleanlinessValTMP, happinessValTMP;
        Slider fullnessBar = CreateNeedBarUI("Fullness", vitalsContainer.transform, new Vector2(0.02f, 0.54f), new Vector2(0.48f, 0.94f), new Color(0.95f, 0.40f, 0.20f), "<color=#FF7030><b>FULLNESS</b></color>", out fullnessValTMP);
        Slider hydrationBar = CreateNeedBarUI("Hydration", vitalsContainer.transform, new Vector2(0.52f, 0.54f), new Vector2(0.98f, 0.94f), new Color(0.20f, 0.75f, 1.0f), "<color=#20B0FF><b>HYDRATION</b></color>", out hydrationValTMP);
        Slider cleanlinessBar = CreateNeedBarUI("Cleanliness", vitalsContainer.transform, new Vector2(0.02f, 0.06f), new Vector2(0.48f, 0.46f), new Color(0.35f, 0.95f, 0.75f), "<color=#30EEA0><b>CLEANLINESS</b></color>", out cleanlinessValTMP);
        Slider happinessBar = CreateNeedBarUI("Happiness", vitalsContainer.transform, new Vector2(0.52f, 0.06f), new Vector2(0.98f, 0.46f), new Color(1.0f, 0.80f, 0.25f), "<color=#FFD030><b>HAPPINESS</b></color>", out happinessValTMP);

        // Pokémon Affection Hearts & Bond Perk Box
        GameObject affBoxObj = CreateUIObject("AffectionBondContainer", inspectorPanel.transform, new Vector2(0.04f, 0.23f), new Vector2(0.96f, 0.34f));
        Image affBoxBg = affBoxObj.AddComponent<Image>();
        affBoxBg.color = new Color(0.10f, 0.11f, 0.17f, 0.95f);

        GameObject affHeartsObj = CreateUIObject("AffectionHeartsTMP", affBoxObj.transform, new Vector2(0.04f, 0.54f), new Vector2(0.96f, 0.96f));
        TextMeshProUGUI affHeartsTMP = affHeartsObj.AddComponent<TextMeshProUGUI>();
        affHeartsTMP.text = "<color=#FFD700>BOND:</color> <color=#FF3366>♥♥♥♥</color><color=#44475A>♡</color> <size=85%>(LV 4/5)</size>";
        affHeartsTMP.fontSize = 11;
        affHeartsTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject bondPerkObj = CreateUIObject("BondPerkTMP", affBoxObj.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.52f));
        TextMeshProUGUI bondPerkTMP = bondPerkObj.AddComponent<TextMeshProUGUI>();
        bondPerkTMP.text = "<color=#FF4090><b>[BOND PERK: DEVOTED PAL]</b></color> +20% Combat ATK & Spawns healing orbs in battle.";
        bondPerkTMP.fontSize = 10;
        bondPerkTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Ni no Kuni Stat Boosts Display
        GameObject statsContainer = CreateUIObject("NiNoKuniStatsContainer", inspectorPanel.transform, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.22f));
        Image statsBg = statsContainer.AddComponent<Image>();
        statsBg.color = new Color(0.07f, 0.09f, 0.13f, 0.90f);

        GameObject atkObj = CreateUIObject("StatAtkTMP", statsContainer.transform, new Vector2(0.02f, 0f), new Vector2(0.24f, 1f));
        TextMeshProUGUI statAtkTMP = atkObj.AddComponent<TextMeshProUGUI>();
        statAtkTMP.text = "<color=#FF5533><b>ATK</b></color> <b>+15</b>";
        statAtkTMP.fontSize = 10;
        statAtkTMP.alignment = TextAlignmentOptions.Center;

        GameObject defObj = CreateUIObject("StatDefTMP", statsContainer.transform, new Vector2(0.26f, 0f), new Vector2(0.48f, 1f));
        TextMeshProUGUI statDefTMP = defObj.AddComponent<TextMeshProUGUI>();
        statDefTMP.text = "<color=#EEAA22><b>DEF</b></color> <b>+10</b>";
        statDefTMP.fontSize = 10;
        statDefTMP.alignment = TextAlignmentOptions.Center;

        GameObject magObj = CreateUIObject("StatMagTMP", statsContainer.transform, new Vector2(0.50f, 0f), new Vector2(0.72f, 1f));
        TextMeshProUGUI statMagTMP = magObj.AddComponent<TextMeshProUGUI>();
        statMagTMP.text = "<color=#33BBFF><b>MAG</b></color> <b>+20</b>";
        statMagTMP.fontSize = 10;
        statMagTMP.alignment = TextAlignmentOptions.Center;

        GameObject spdObj = CreateUIObject("StatSpdTMP", statsContainer.transform, new Vector2(0.74f, 0f), new Vector2(0.98f, 1f));
        TextMeshProUGUI statSpdTMP = spdObj.AddComponent<TextMeshProUGUI>();
        statSpdTMP.text = "<color=#44EE88><b>SPD</b></color> <b>+25</b>";
        statSpdTMP.fontSize = 10;
        statSpdTMP.alignment = TextAlignmentOptions.Center;

        // Lore Box (for NPCs / Fallback)
        GameObject loreObj = CreateUIObject("LoreBox", inspectorPanel.transform, new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.50f));
        loreObj.SetActive(false);
        Image lBg = loreObj.AddComponent<Image>();
        lBg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        GameObject loreTxtObj = CreateUIObject("LoreTMP", loreObj.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
        TextMeshProUGUI loreTMP = loreTxtObj.AddComponent<TextMeshProUGUI>();
        loreTMP.text = "Cheerful tiny sprite with iridescent fairy wings and boundless curiosity.";
        loreTMP.fontSize = 11;

        // Active Link Effect Card
        GameObject effectCard = CreateUIObject("ActiveEffectCard", inspectorPanel.transform, new Vector2(0.04f, 0.15f), new Vector2(0.96f, 0.22f));
        effectCard.SetActive(false);
        Image effectBorder = effectCard.AddComponent<Image>();
        effectBorder.color = new Color(0.20f, 0.90f, 0.45f, 1f);
        GameObject effectInner = CreateUIObject("Inner", effectCard.transform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.96f));
        Image effectInnerBg = effectInner.AddComponent<Image>();
        effectInnerBg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);
        GameObject effTitleObj = CreateUIObject("TitleTMP", effectInner.transform, new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.95f));
        TextMeshProUGUI effTitleTMP = effTitleObj.AddComponent<TextMeshProUGUI>();
        effTitleTMP.text = "<color=#30FF80>[ACTIVE LINK BUFF]</color> <b>GOSSAMER WARD</b>";
        effTitleTMP.fontSize = 11;
        GameObject effDescObj = CreateUIObject("DescTMP", effectInner.transform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.52f));
        TextMeshProUGUI effDescTMP = effDescObj.AddComponent<TextMeshProUGUI>();
        effDescTMP.text = "+20% Familiar Attack Power & Survives lethal hits with 1 HP out of love.";
        effDescTMP.fontSize = 10;

        // PET CARE ACTION DECK (FEED, WATER, BATHE, PRESENT, PET)
        GameObject petActionDeck = CreateUIObject("PetActionDeck", inspectorPanel.transform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.12f));
        GameObject feedBtn = CreateButton("FeedBtn", petActionDeck.transform, new Vector2(0.00f, 0f), new Vector2(0.19f, 1f), "<b>FEED</b>");
        feedBtn.GetComponent<Image>().color = new Color(0.35f, 0.16f, 0.10f, 0.95f);
        GameObject waterBtn = CreateButton("WaterBtn", petActionDeck.transform, new Vector2(0.20f, 0f), new Vector2(0.39f, 1f), "<b>WATER</b>");
        waterBtn.GetComponent<Image>().color = new Color(0.10f, 0.22f, 0.38f, 0.95f);
        GameObject batheBtn = CreateButton("BatheBtn", petActionDeck.transform, new Vector2(0.40f, 0f), new Vector2(0.59f, 1f), "<b>BATHE</b>");
        batheBtn.GetComponent<Image>().color = new Color(0.10f, 0.32f, 0.26f, 0.95f);
        GameObject presentBtn = CreateButton("PresentBtn", petActionDeck.transform, new Vector2(0.60f, 0f), new Vector2(0.79f, 1f), "<b>PRESENT</b>");
        presentBtn.GetComponent<Image>().color = new Color(0.32f, 0.12f, 0.30f, 0.95f);
        GameObject petCuddleBtn = CreateButton("CuddleBtn", petActionDeck.transform, new Vector2(0.80f, 0f), new Vector2(0.99f, 1f), "<b>PET</b>");
        petCuddleBtn.GetComponent<Image>().color = new Color(0.38f, 0.18f, 0.25f, 0.95f);

        // NPC ACTION DECK (GIFT, TALK, SPAR) - Fallback for Non-Familiars
        GameObject npcActionDeck = CreateUIObject("NpcActionDeck", inspectorPanel.transform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.12f));
        GameObject giftBtn = CreateButton("GiftBtn", npcActionDeck.transform, new Vector2(0.00f, 0f), new Vector2(0.31f, 1f), "[ GIFT ITEM ]");
        GameObject talkBtn = CreateButton("TalkBtn", npcActionDeck.transform, new Vector2(0.34f, 0f), new Vector2(0.65f, 1f), "[ TALK / LORE ]");
        GameObject sparBtn = CreateButton("SparBtn", npcActionDeck.transform, new Vector2(0.68f, 0f), new Vector2(0.99f, 1f), "[ SPAR / BOND ]");
        npcActionDeck.SetActive(false);

        // 1. Ni no Kuni Treat Drawer
        GameObject treatDrawerObj = CreateUIObject("TreatDrawerOverlay", overlay.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.82f));
        Image treatDrawerBg = treatDrawerObj.AddComponent<Image>();
        treatDrawerBg.color = new Color(0.06f, 0.07f, 0.10f, 0.98f);
        GameObject treatHeader = CreateUIObject("TreatHeader", treatDrawerObj.transform, new Vector2(0.04f, 0.91f), new Vector2(0.85f, 0.98f));
        TextMeshProUGUI tHeadTmp = treatHeader.AddComponent<TextMeshProUGUI>();
        tHeadTmp.text = "<color=#FFD700><b>NI NO KUNI TREATS</b></color> // <color=#40EBFF>FEED TO BOOST STATS</color>";
        tHeadTmp.fontSize = 13;
        tHeadTmp.alignment = TextAlignmentOptions.MidlineLeft;
        GameObject closeTreatBtn = CreateButton("CloseTreatBtn", treatDrawerObj.transform, new Vector2(0.88f, 0.91f), new Vector2(0.96f, 0.98f), "X");
        GameObject treatScrollObj = CreateUIObject("TreatGridContainer", treatDrawerObj.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.89f));
        treatScrollObj.AddComponent<RectMask2D>();
        GridLayoutGroup tLayout = treatScrollObj.AddComponent<GridLayoutGroup>();
        tLayout.cellSize = new Vector2(250f, 68f);
        tLayout.spacing = new Vector2(10f, 10f);
        tLayout.childAlignment = TextAnchor.UpperCenter;
        treatDrawerObj.SetActive(false);

        // 2. Pokémon-Amie Present Drawer
        GameObject presentDrawerObj = CreateUIObject("PresentDrawerOverlay", overlay.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.82f));
        Image presentDrawerBg = presentDrawerObj.AddComponent<Image>();
        presentDrawerBg.color = new Color(0.06f, 0.07f, 0.10f, 0.98f);
        GameObject presentHeader = CreateUIObject("PresentHeader", presentDrawerObj.transform, new Vector2(0.04f, 0.91f), new Vector2(0.85f, 0.98f));
        TextMeshProUGUI pHeadTmp = presentHeader.AddComponent<TextMeshProUGUI>();
        pHeadTmp.text = "<color=#FFD700><b>POKÉMON-AMIE TOYS & PRESENTS</b></color> // <color=#40EBFF>GIVE TO DEEPEN BOND</color>";
        pHeadTmp.fontSize = 13;
        pHeadTmp.alignment = TextAlignmentOptions.MidlineLeft;
        GameObject closePresentBtn = CreateButton("ClosePresentBtn", presentDrawerObj.transform, new Vector2(0.88f, 0.91f), new Vector2(0.96f, 0.98f), "X");
        GameObject presentScrollObj = CreateUIObject("PresentGridContainer", presentDrawerObj.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.89f));
        presentScrollObj.AddComponent<RectMask2D>();
        GridLayoutGroup pLayout = presentScrollObj.AddComponent<GridLayoutGroup>();
        pLayout.cellSize = new Vector2(250f, 68f);
        pLayout.spacing = new Vector2(10f, 10f);
        pLayout.childAlignment = TextAnchor.UpperCenter;
        presentDrawerObj.SetActive(false);

        // 3. General Satchel Gift Drawer
        GameObject drawerObj = CreateUIObject("GiftDrawerOverlay", overlay.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.82f));
        Image drawerBg = drawerObj.AddComponent<Image>();
        drawerBg.color = new Color(0.06f, 0.07f, 0.10f, 0.98f);
        GameObject drawerHeader = CreateUIObject("DrawerHeader", drawerObj.transform, new Vector2(0.04f, 0.91f), new Vector2(0.85f, 0.98f));
        TextMeshProUGUI dHeadTmp = drawerHeader.AddComponent<TextMeshProUGUI>();
        dHeadTmp.text = "<color=#FFD700><b>MESSENGER SATCHEL</b></color> // <color=#40EBFF>SELECT GIFT TO OFFER</color>";
        dHeadTmp.fontSize = 13;
        dHeadTmp.alignment = TextAlignmentOptions.MidlineLeft;
        GameObject closeDrawerBtn = CreateButton("CloseDrawerBtn", drawerObj.transform, new Vector2(0.88f, 0.91f), new Vector2(0.96f, 0.98f), "X");
        GameObject giftScrollObj = CreateUIObject("GiftGridContainer", drawerObj.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.89f));
        giftScrollObj.AddComponent<RectMask2D>();
        GridLayoutGroup gLayout = giftScrollObj.AddComponent<GridLayoutGroup>();
        gLayout.cellSize = new Vector2(250f, 64f);
        gLayout.spacing = new Vector2(10f, 10f);
        gLayout.childAlignment = TextAnchor.UpperCenter;
        drawerObj.SetActive(false);

        // Attach & Wire TomodachiSocialHubUI
        var hubUI = overlay.AddComponent<BattleSummoner.Social.TomodachiSocialHubUI>();
        SetField(hubUI, "overlayRoot", overlay);
        SetField(hubUI, "closeButton", closeBtn.GetComponent<Button>());
        SetField(hubUI, "headerTitleTMP", titleTMP);

        SetField(hubUI, "tabAllBtn", tabAll.GetComponent<Button>());
        SetField(hubUI, "tabNpcsBtn", tabNpcs.GetComponent<Button>());
        SetField(hubUI, "tabFamiliarsBtn", tabFamiliars.GetComponent<Button>());
        SetField(hubUI, "tabSummonsBtn", tabSummons.GetComponent<Button>());
        SetField(hubUI, "tabConstructsBtn", tabConstructs.GetComponent<Button>());

        SetField(hubUI, "rosterContainer", rosterScrollObj.transform);

        SetField(hubUI, "characterPortraitImg", portImg);
        SetField(hubUI, "arcanaCardBackingImg", cardImg);
        SetField(hubUI, "nameTMP", nameTMP);
        SetField(hubUI, "subtitleTMP", subTMP);
        SetField(hubUI, "arcanaRankTMP", arcanaTMP);
        SetField(hubUI, "moodBadgeTMP", moodTMP);

        SetField(hubUI, "affinityBar", affSlider);
        SetField(hubUI, "affinityBarFill", sFillImg);
        SetField(hubUI, "affinityValueTMP", affValTMP);
        SetField(hubUI, "tierTagTMP", tierTMP);
        SetField(hubUI, "loreTMP", loreTMP);

        SetField(hubUI, "speechBubbleObj", bubbleObj);
        SetField(hubUI, "speechBubbleTMP", bubbleTMP);
        SetField(hubUI, "petClickAreaBtn", petClickBtn);

        SetField(hubUI, "petCareVitalsContainer", vitalsContainer);
        SetField(hubUI, "fullnessBar", fullnessBar);
        SetField(hubUI, "fullnessValTMP", fullnessValTMP);
        SetField(hubUI, "hydrationBar", hydrationBar);
        SetField(hubUI, "hydrationValTMP", hydrationValTMP);
        SetField(hubUI, "cleanlinessBar", cleanlinessBar);
        SetField(hubUI, "cleanlinessValTMP", cleanlinessValTMP);
        SetField(hubUI, "happinessBar", happinessBar);
        SetField(hubUI, "happinessValTMP", happinessValTMP);

        SetField(hubUI, "affectionHeartsTMP", affHeartsTMP);
        SetField(hubUI, "bondPerkTMP", bondPerkTMP);

        SetField(hubUI, "niNoKuniStatsContainer", statsContainer);
        SetField(hubUI, "statAtkTMP", statAtkTMP);
        SetField(hubUI, "statDefTMP", statDefTMP);
        SetField(hubUI, "statMagTMP", statMagTMP);
        SetField(hubUI, "statSpdTMP", statSpdTMP);

        SetField(hubUI, "petActionDeckObj", petActionDeck);
        SetField(hubUI, "feedTreatBtn", feedBtn.GetComponent<Button>());
        SetField(hubUI, "giveWaterBtn", waterBtn.GetComponent<Button>());
        SetField(hubUI, "bathePetBtn", batheBtn.GetComponent<Button>());
        SetField(hubUI, "givePresentBtn", presentBtn.GetComponent<Button>());
        SetField(hubUI, "petCuddleBtn", petCuddleBtn.GetComponent<Button>());

        SetField(hubUI, "npcActionDeckObj", npcActionDeck);
        SetField(hubUI, "giftBtn", giftBtn.GetComponent<Button>());
        SetField(hubUI, "talkBtn", talkBtn.GetComponent<Button>());
        SetField(hubUI, "sparBtn", sparBtn.GetComponent<Button>());

        SetField(hubUI, "activeEffectCardObj", effectCard);
        SetField(hubUI, "activeEffectBorderImg", effectBorder);
        SetField(hubUI, "activeEffectTitleTMP", effTitleTMP);
        SetField(hubUI, "activeEffectDescTMP", effDescTMP);

        SetField(hubUI, "treatDrawerRoot", treatDrawerObj);
        SetField(hubUI, "treatGridContainer", treatScrollObj.transform);
        SetField(hubUI, "closeTreatDrawerBtn", closeTreatBtn.GetComponent<Button>());

        SetField(hubUI, "presentDrawerRoot", presentDrawerObj);
        SetField(hubUI, "presentGridContainer", presentScrollObj.transform);
        SetField(hubUI, "closePresentDrawerBtn", closePresentBtn.GetComponent<Button>());

        SetField(hubUI, "giftDrawerRoot", drawerObj);
        SetField(hubUI, "giftGridContainer", giftScrollObj.transform);
        SetField(hubUI, "closeGiftDrawerBtn", closeDrawerBtn.GetComponent<Button>());

        SetField(hubUI, "heartsFxContainer", heartsFxRt);

        overlay.SetActive(false);
    }

    private static Slider CreateNeedBarUI(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color fillColor, string label, out TextMeshProUGUI valTMP)
    {
        GameObject rowObj = CreateUIObject(name + "_Row", parent, anchorMin, anchorMax);

        GameObject lblObj = CreateUIObject("Label", rowObj.transform, new Vector2(0.02f, 0.48f), new Vector2(0.65f, 1f));
        TextMeshProUGUI lTmp = lblObj.AddComponent<TextMeshProUGUI>();
        lTmp.text = label;
        lTmp.fontSize = 9;
        lTmp.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject valObj = CreateUIObject("Value", rowObj.transform, new Vector2(0.68f, 0.48f), new Vector2(0.98f, 1f));
        valTMP = valObj.AddComponent<TextMeshProUGUI>();
        valTMP.text = "75%";
        valTMP.fontSize = 9;
        valTMP.alignment = TextAlignmentOptions.MidlineRight;

        GameObject sliderObj = CreateUIObject("Bar", rowObj.transform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.42f));
        Slider slider = sliderObj.AddComponent<Slider>();

        GameObject bg = CreateUIObject("Background", sliderObj.transform, Vector2.zero, Vector2.one);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.14f, 0.18f, 1f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform, Vector2.zero, Vector2.one);
        GameObject fill = CreateUIObject("Fill", fillArea.transform, Vector2.zero, Vector2.one);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.75f;

        return slider;
    }
    #endregion

    #region 7. Character Customization Overlay
    private static void BuildCharacterCustomizationUI(Transform parent)
    {
        Transform existing = parent.Find("CharacterCustomizationOverlay");
        if (existing != null)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(existing.gameObject);
            else UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        GameObject overlay = new GameObject("CharacterCustomizationOverlay", typeof(RectTransform));
        overlay.transform.SetParent(parent, false);
        RectTransform ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;

        // Dimmer background
        GameObject dimmer = CreateUIObject("Dimmer", overlay.transform, Vector2.zero, Vector2.one);
        Image dimImg = dimmer.AddComponent<Image>();
        dimImg.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);

        // Frame
        GameObject frame = CreateUIObject("CustomizationFrame", overlay.transform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.98f));
        Image frameImg = frame.AddComponent<Image>();
        frameImg.sprite = GetSprite("Mockup_CustomizationFrame.png");
        frameImg.type = Image.Type.Sliced;

        // 1. Header
        GameObject header = CreateUIObject("Header", frame.transform, new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f));
        GameObject titleObj = CreateUIObject("TitleTMP", header.transform, new Vector2(0f, 0.45f), new Vector2(0.85f, 1f));
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<color=#FFD700>MIRROR OF TRANSMUTATION</color>";
        titleTMP.fontSize = 17;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject subObj = CreateUIObject("SubtitleTMP", header.transform, new Vector2(0f, 0f), new Vector2(0.85f, 0.45f));
        TextMeshProUGUI subTMP = subObj.AddComponent<TextMeshProUGUI>();
        subTMP.text = "<color=#8BE9FD>Shape Your Identity, Anatomy & Soul Signature</color>";
        subTMP.fontSize = 10;
        subTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject closeBtn = CreateButton("CloseBtn", header.transform, new Vector2(0.88f, 0.15f), new Vector2(0.98f, 0.85f), "X");

        // 2. Turntable Deck (3D camera preview area)
        GameObject deck = CreateUIObject("TurntableDeck", frame.transform, new Vector2(0.04f, 0.63f), new Vector2(0.96f, 0.91f));
        Image deckRing = deck.AddComponent<Image>();
        deckRing.sprite = GetSprite("Mockup_CustomizationTurntableRing.png");
        deckRing.preserveAspect = true;
        deckRing.color = new Color(1f, 1f, 1f, 0.40f);

        GameObject camToggleBtn = CreateButton("CamFramingToggleBtn", deck.transform, new Vector2(0.02f, 0.04f), new Vector2(0.44f, 0.22f), "[VIEW: FULL BODY]");
        TextMeshProUGUI camLabel = camToggleBtn.GetComponentInChildren<TextMeshProUGUI>();
        camLabel.fontSize = 10;

        GameObject rotLBtn = CreateButton("RotateLeftBtn", deck.transform, new Vector2(0.48f, 0.04f), new Vector2(0.64f, 0.22f), "< ROT");
        rotLBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 10;

        GameObject rotRBtn = CreateButton("RotateRightBtn", deck.transform, new Vector2(0.66f, 0.04f), new Vector2(0.82f, 0.22f), "ROT >");
        rotRBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 10;

        GameObject autoSpinToggleObj = CreateUIObject("AutoSpinToggle", deck.transform, new Vector2(0.84f, 0.04f), new Vector2(0.98f, 0.22f));
        Toggle autoSpinToggle = autoSpinToggleObj.AddComponent<Toggle>();
        Image autoSpinBg = autoSpinToggleObj.AddComponent<Image>();
        autoSpinBg.color = new Color(0.12f, 0.16f, 0.24f, 0.8f);
        GameObject autoCheck = CreateUIObject("Check", autoSpinToggleObj.transform, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
        Image autoCheckImg = autoCheck.AddComponent<Image>();
        autoCheckImg.color = new Color(1.0f, 0.85f, 0.35f, 1f);
        autoSpinToggle.graphic = autoCheckImg;
        autoSpinToggle.isOn = false;

        // 3. Tab Navigation Bar (5 Tabs)
        GameObject tabDeck = CreateUIObject("TabDeck", frame.transform, new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.62f));
        GameObject tabId = CreateButton("TabIdentity", tabDeck.transform, new Vector2(0.00f, 0f), new Vector2(0.18f, 1f), "IDENTITY");
        GameObject tabBody = CreateButton("TabBody", tabDeck.transform, new Vector2(0.20f, 0f), new Vector2(0.38f, 1f), "BODY");
        GameObject tabFace = CreateButton("TabFace", tabDeck.transform, new Vector2(0.40f, 0f), new Vector2(0.58f, 1f), "VISAGE");
        GameObject tabHair = CreateButton("TabHair", tabDeck.transform, new Vector2(0.60f, 0f), new Vector2(0.78f, 1f), "HAIR");
        GameObject tabRefs = CreateButton("TabReferences", tabDeck.transform, new Vector2(0.80f, 0f), new Vector2(1.00f, 1f), "REFS");

        // 4. Tab Panels Container
        GameObject panelContainer = CreateUIObject("TabPanelsContainer", frame.transform, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.55f));

        // ---- TAB 1: IDENTITY PANEL ----
        GameObject identityPanel = CreateUIObject("IdentityPanel", panelContainer.transform, Vector2.zero, Vector2.one);

        // Name input row
        GameObject nameLabelObj = CreateUIObject("NameLabel", identityPanel.transform, new Vector2(0.02f, 0.86f), new Vector2(0.98f, 0.98f));
        TextMeshProUGUI nameLbl = nameLabelObj.AddComponent<TextMeshProUGUI>();
        nameLbl.text = "<color=#FFD700>SOUL NAME:</color>";
        nameLbl.fontSize = 12;

        GameObject nameInputObj = CreateUIObject("NameInput", identityPanel.transform, new Vector2(0.02f, 0.74f), new Vector2(0.98f, 0.86f));
        Image nameInputBg = nameInputObj.AddComponent<Image>();
        nameInputBg.color = new Color(0.10f, 0.12f, 0.18f, 0.95f);
        TMP_InputField nameInput = nameInputObj.AddComponent<TMP_InputField>();
        GameObject nameTxtObj = CreateUIObject("Text", nameInputObj.transform, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
        TextMeshProUGUI nameTxt = nameTxtObj.AddComponent<TextMeshProUGUI>();
        nameTxt.fontSize = 13;
        nameTxt.color = Color.white;
        nameInput.textComponent = nameTxt;
        nameInput.text = "Summoner";

        // Sex options row
        GameObject sexLabelObj = CreateUIObject("SexLabel", identityPanel.transform, new Vector2(0.02f, 0.60f), new Vector2(0.98f, 0.70f));
        TextMeshProUGUI sexLbl = sexLabelObj.AddComponent<TextMeshProUGUI>();
        sexLbl.text = "<color=#FFD700>BIOLOGICAL SEX:</color>";
        sexLbl.fontSize = 12;

        string[] sexNames = new string[] { "Female", "Male", "Non-Binary", "Custom" };
        var sexBtns = new Button[sexNames.Length];
        for (int i = 0; i < sexNames.Length; i++)
        {
            float xMin = 0.02f + i * 0.245f;
            float xMax = xMin + 0.23f;
            GameObject sBtnObj = CreateButton("SexBtn_" + sexNames[i], identityPanel.transform, new Vector2(xMin, 0.48f), new Vector2(xMax, 0.59f), sexNames[i]);
            sBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 10;
            sexBtns[i] = sBtnObj.GetComponent<Button>();
        }

        // Pronoun options row
        GameObject proLabelObj = CreateUIObject("PronounLabel", identityPanel.transform, new Vector2(0.02f, 0.35f), new Vector2(0.98f, 0.45f));
        TextMeshProUGUI proLbl = proLabelObj.AddComponent<TextMeshProUGUI>();
        proLbl.text = "<color=#FFD700>PREFERRED PRONOUNS:</color>";
        proLbl.fontSize = 12;

        string[] proNames = new string[] { "She/Her", "He/Him", "They/Them", "It/Its", "Custom" };
        var proBtns = new Button[proNames.Length];
        for (int i = 0; i < proNames.Length; i++)
        {
            float xMin = 0.02f + i * 0.195f;
            float xMax = xMin + 0.185f;
            GameObject pBtnObj = CreateButton("ProBtn_" + proNames[i], identityPanel.transform, new Vector2(xMin, 0.23f), new Vector2(xMax, 0.34f), proNames[i]);
            pBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            proBtns[i] = pBtnObj.GetComponent<Button>();
        }

        // Live Pronoun Preview Banner
        GameObject proPrevObj = CreateUIObject("PronounPreview", identityPanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.19f));
        Image proPrevBg = proPrevObj.AddComponent<Image>();
        proPrevBg.color = new Color(0.08f, 0.10f, 0.15f, 0.85f);
        GameObject proTxtObj = CreateUIObject("Text", proPrevObj.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI proPrevTMP = proTxtObj.AddComponent<TextMeshProUGUI>();
        proPrevTMP.text = "<color=#FFD700>Pronouns:</color> She / Her\n<color=#A0E0FF>\"She commands familiars with her grimoire.\"</color>";
        proPrevTMP.fontSize = 11;
        proPrevTMP.alignment = TextAlignmentOptions.Center;

        // ---- TAB 2: BODY & MORPHOLOGY PANEL ----
        GameObject bodyPanel = CreateUIObject("BodyPanel", panelContainer.transform, Vector2.zero, Vector2.one);

        // Body Type presets row
        GameObject btLabelObj = CreateUIObject("BodyTypeLabel", bodyPanel.transform, new Vector2(0.02f, 0.90f), new Vector2(0.55f, 1f));
        TextMeshProUGUI btLbl = btLabelObj.AddComponent<TextMeshProUGUI>();
        btLbl.text = "<color=#FFD700>BODY ARCHETYPE:</color>";
        btLbl.fontSize = 11;

        GameObject anatomyGuideBtnObj = CreateButton("ViewAnatomyGuideBtn", bodyPanel.transform, new Vector2(0.56f, 0.90f), new Vector2(0.98f, 0.99f), "[ANATOMY BLUEPRINTS]");
        anatomyGuideBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
        anatomyGuideBtnObj.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.85f, 0.3f);
        var viewAnatomyBtn = anatomyGuideBtnObj.GetComponent<Button>();

        string[] btNames = new string[] { "Athletic", "Feminine", "Masculine", "Slender", "Heavy", "Stocky" };
        var btBtns = new Button[btNames.Length];
        for (int i = 0; i < btNames.Length; i++)
        {
            float xMin = 0.02f + i * 0.162f;
            float xMax = xMin + 0.154f;
            GameObject bBtnObj = CreateButton("BtBtn_" + btNames[i], bodyPanel.transform, new Vector2(xMin, 0.79f), new Vector2(xMax, 0.89f), btNames[i]);
            bBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            btBtns[i] = bBtnObj.GetComponent<Button>();
        }

        // Morphology Sliders (Neck, Arm, Torso, Hips, Legs, Feet, Height)
        TextMeshProUGUI neckValTMP, armValTMP, torsoValTMP, hipsValTMP, legsValTMP, feetValTMP, heightValTMP;
        Slider neckSlider = CreateMorphologySlider("NeckLength", bodyPanel.transform, 0.69f, "Neck Length", 0.70f, 1.40f, 1.0f, out neckValTMP);
        Slider armSlider = CreateMorphologySlider("ArmLength", bodyPanel.transform, 0.58f, "Arm Length", 0.70f, 1.40f, 1.0f, out armValTMP);
        Slider torsoSlider = CreateMorphologySlider("TorsoScale", bodyPanel.transform, 0.47f, "Torso Width", 0.70f, 1.40f, 1.0f, out torsoValTMP);
        Slider hipsSlider = CreateMorphologySlider("HipsScale", bodyPanel.transform, 0.36f, "Hips Scale", 0.70f, 1.40f, 1.0f, out hipsValTMP);
        Slider legsSlider = CreateMorphologySlider("LegsLength", bodyPanel.transform, 0.25f, "Legs Length", 0.70f, 1.40f, 1.0f, out legsValTMP);
        Slider feetSlider = CreateMorphologySlider("FeetScale", bodyPanel.transform, 0.14f, "Feet Scale", 0.70f, 1.40f, 1.0f, out feetValTMP);
        Slider heightSlider = CreateMorphologySlider("HeightScale", bodyPanel.transform, 0.03f, "Total Height", 0.85f, 1.15f, 1.0f, out heightValTMP);

        // ---- TAB 3: FACE & VISAGE PANEL ----
        GameObject facePanel = CreateUIObject("FacePanel", panelContainer.transform, Vector2.zero, Vector2.one);

        // Face Shape row
        GameObject fsLabelObj = CreateUIObject("FaceShapeLabel", facePanel.transform, new Vector2(0.02f, 0.89f), new Vector2(0.98f, 0.99f));
        TextMeshProUGUI fsLbl = fsLabelObj.AddComponent<TextMeshProUGUI>();
        fsLbl.text = "<color=#FFD700>FACE SHAPE & SKIN TONE:</color>";
        fsLbl.fontSize = 11;

        string[] fsNames = new string[] { "Oval", "Angular", "Chiseled", "Round", "Heart", "Sharp" };
        var fsBtns = new Button[fsNames.Length];
        for (int i = 0; i < fsNames.Length; i++)
        {
            float xMin = 0.02f + i * 0.162f;
            float xMax = xMin + 0.154f;
            GameObject fBtnObj = CreateButton("FsBtn_" + fsNames[i], facePanel.transform, new Vector2(xMin, 0.78f), new Vector2(xMax, 0.88f), fsNames[i]);
            fBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            fsBtns[i] = fBtnObj.GetComponent<Button>();
        }

        // Skin Tone Swatches
        GameObject skinSwatches = CreateUIObject("SkinToneContainer", facePanel.transform, new Vector2(0.02f, 0.69f), new Vector2(0.98f, 0.77f));
        CreateColorSwatches(skinSwatches.transform, BattleSummoner.Customization.CharacterCustomizationData.SkinTones, (idx) => {
            if (BattleSummoner.Customization.CharacterCustomizationUI.Instance != null)
                BattleSummoner.Customization.CharacterCustomizationUI.Instance.SelectSkinTone(idx);
        });

        // Eye Shape & Eye Color
        GameObject eyeLabelObj = CreateUIObject("EyeLabel", facePanel.transform, new Vector2(0.02f, 0.58f), new Vector2(0.98f, 0.68f));
        TextMeshProUGUI eyeLbl = eyeLabelObj.AddComponent<TextMeshProUGUI>();
        eyeLbl.text = "<color=#FFD700>EYES (SHAPE & IRIS COLOR):</color>";
        eyeLbl.fontSize = 11;

        string[] esNames = new string[] { "Almond", "Wide", "Stoic", "Demonic", "Gentle" };
        var esBtns = new Button[esNames.Length];
        for (int i = 0; i < esNames.Length; i++)
        {
            float xMin = 0.02f + i * 0.195f;
            float xMax = xMin + 0.185f;
            GameObject eBtnObj = CreateButton("EsBtn_" + esNames[i], facePanel.transform, new Vector2(xMin, 0.47f), new Vector2(xMax, 0.57f), esNames[i]);
            eBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            esBtns[i] = eBtnObj.GetComponent<Button>();
        }

        GameObject eyeSwatches = CreateUIObject("EyeColorContainer", facePanel.transform, new Vector2(0.02f, 0.38f), new Vector2(0.98f, 0.46f));
        CreateColorSwatches(eyeSwatches.transform, BattleSummoner.Customization.CharacterCustomizationData.EyeColors, (idx) => {
            if (BattleSummoner.Customization.CharacterCustomizationUI.Instance != null)
                BattleSummoner.Customization.CharacterCustomizationUI.Instance.SelectEyeColor(idx);
        });

        // Heterochromia Toggle
        GameObject hetToggleObj = CreateUIObject("HeterochromiaToggle", facePanel.transform, new Vector2(0.02f, 0.28f), new Vector2(0.48f, 0.37f));
        Toggle hetToggle = hetToggleObj.AddComponent<Toggle>();
        Image hetBg = hetToggleObj.AddComponent<Image>();
        hetBg.color = new Color(0.12f, 0.16f, 0.22f, 0.85f);
        GameObject hetTxtObj = CreateUIObject("Label", hetToggleObj.transform, new Vector2(0.25f, 0f), new Vector2(1f, 1f));
        TextMeshProUGUI hetTxt = hetTxtObj.AddComponent<TextMeshProUGUI>();
        hetTxt.text = "Heterochromia (Dual Eyes)";
        hetTxt.fontSize = 9;
        hetTxt.alignment = TextAlignmentOptions.MidlineLeft;
        GameObject hetCheck = CreateUIObject("Check", hetToggleObj.transform, new Vector2(0.05f, 0.2f), new Vector2(0.20f, 0.8f));
        Image hetCheckImg = hetCheck.AddComponent<Image>();
        hetCheckImg.color = new Color(1.0f, 0.85f, 0.35f, 1f);
        hetToggle.graphic = hetCheckImg;

        // Right Eye Swatches container
        GameObject rEyeSwatches = CreateUIObject("RightEyeColorContainer", facePanel.transform, new Vector2(0.50f, 0.28f), new Vector2(0.98f, 0.37f));
        CreateColorSwatches(rEyeSwatches.transform, BattleSummoner.Customization.CharacterCustomizationData.EyeColors, (idx) => {
            if (BattleSummoner.Customization.CharacterCustomizationUI.Instance != null)
                BattleSummoner.Customization.CharacterCustomizationUI.Instance.SelectRightEyeColor(idx);
        });
        rEyeSwatches.SetActive(false);

        // Nose Shape & Scale Slider
        TextMeshProUGUI noseValTMP;
        Slider noseSlider = CreateMorphologySlider("NoseScale", facePanel.transform, 0.17f, "Nose Scale", 0.70f, 1.30f, 1.0f, out noseValTMP);

        string[] nsNames = new string[] { "Straight", "Aquiline", "Button", "Sharp", "Broad" };
        var nsBtns = new Button[nsNames.Length];
        for (int i = 0; i < nsNames.Length; i++)
        {
            float xMin = 0.02f + i * 0.195f;
            float xMax = xMin + 0.185f;
            GameObject nBtnObj = CreateButton("NsBtn_" + nsNames[i], facePanel.transform, new Vector2(xMin, 0.08f), new Vector2(xMax, 0.16f), nsNames[i]);
            nBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            nsBtns[i] = nBtnObj.GetComponent<Button>();
        }

        // Mouth Shape
        string[] msNames = new string[] { "Neutral", "Smirk", "Smile", "Pout", "Fanged" };
        var msBtns = new Button[msNames.Length];
        for (int i = 0; i < msNames.Length; i++)
        {
            float xMin = 0.02f + i * 0.195f;
            float xMax = xMin + 0.185f;
            GameObject mBtnObj = CreateButton("MsBtn_" + msNames[i], facePanel.transform, new Vector2(xMin, 0.00f), new Vector2(xMax, 0.075f), msNames[i]);
            mBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            msBtns[i] = mBtnObj.GetComponent<Button>();
        }

        // ---- TAB 4: HAIR & STYLING PANEL ----
        GameObject hairPanel = CreateUIObject("HairPanel", panelContainer.transform, Vector2.zero, Vector2.one);

        // Hair Styles (8 options in 2 rows of 4)
        GameObject hsLabelObj = CreateUIObject("HairStyleLabel", hairPanel.transform, new Vector2(0.02f, 0.89f), new Vector2(0.98f, 0.99f));
        TextMeshProUGUI hsLbl = hsLabelObj.AddComponent<TextMeshProUGUI>();
        hsLbl.text = "<color=#FFD700>HAIRSTYLE & DYE:</color>";
        hsLbl.fontSize = 11;

        string[] hsNames = new string[] { "Spiky", "Ponytail", "Braids", "Crop", "Bob", "Shaggy", "Twintails", "Buzzed" };
        var hsBtns = new Button[hsNames.Length];
        for (int i = 0; i < 4; i++)
        {
            float xMin = 0.02f + i * 0.245f;
            float xMax = xMin + 0.23f;
            GameObject hBtnObj = CreateButton("HsBtn_" + hsNames[i], hairPanel.transform, new Vector2(xMin, 0.77f), new Vector2(xMax, 0.88f), hsNames[i]);
            hBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            hsBtns[i] = hBtnObj.GetComponent<Button>();
        }
        for (int i = 4; i < 8; i++)
        {
            float xMin = 0.02f + (i - 4) * 0.245f;
            float xMax = xMin + 0.23f;
            GameObject hBtnObj = CreateButton("HsBtn_" + hsNames[i], hairPanel.transform, new Vector2(xMin, 0.64f), new Vector2(xMax, 0.75f), hsNames[i]);
            hBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            hsBtns[i] = hBtnObj.GetComponent<Button>();
        }

        // Hair Color Swatches
        GameObject hairSwatches = CreateUIObject("HairColorContainer", hairPanel.transform, new Vector2(0.02f, 0.52f), new Vector2(0.98f, 0.61f));
        CreateColorSwatches(hairSwatches.transform, BattleSummoner.Customization.CharacterCustomizationData.HairColors, (idx) => {
            if (BattleSummoner.Customization.CharacterCustomizationUI.Instance != null)
                BattleSummoner.Customization.CharacterCustomizationUI.Instance.SelectHairColor(idx);
        });

        // Outfit Primary Color Swatches
        GameObject opLabelObj = CreateUIObject("OutfitPriLabel", hairPanel.transform, new Vector2(0.02f, 0.39f), new Vector2(0.98f, 0.49f));
        TextMeshProUGUI opLbl = opLabelObj.AddComponent<TextMeshProUGUI>();
        opLbl.text = "<color=#FFD700>OUTFIT PRIMARY TINT:</color>";
        opLbl.fontSize = 11;

        GameObject outfitPriSwatches = CreateUIObject("OutfitPriContainer", hairPanel.transform, new Vector2(0.02f, 0.28f), new Vector2(0.98f, 0.37f));
        CreateColorSwatches(outfitPriSwatches.transform, BattleSummoner.Customization.CharacterCustomizationData.OutfitColors, (idx) => {
            if (BattleSummoner.Customization.CharacterCustomizationUI.Instance != null)
                BattleSummoner.Customization.CharacterCustomizationUI.Instance.SelectOutfitPrimary(idx);
        });

        // Outfit Secondary Color Swatches
        GameObject osLabelObj = CreateUIObject("OutfitSecLabel", hairPanel.transform, new Vector2(0.02f, 0.15f), new Vector2(0.98f, 0.25f));
        TextMeshProUGUI osLbl = osLabelObj.AddComponent<TextMeshProUGUI>();
        osLbl.text = "<color=#FFD700>OUTFIT ACCENT & ARMOR TRIM:</color>";
        osLbl.fontSize = 11;

        GameObject outfitSecSwatches = CreateUIObject("OutfitSecContainer", hairPanel.transform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.13f));
        CreateColorSwatches(outfitSecSwatches.transform, BattleSummoner.Customization.CharacterCustomizationData.OutfitColors, (idx) => {
            if (BattleSummoner.Customization.CharacterCustomizationUI.Instance != null)
                BattleSummoner.Customization.CharacterCustomizationUI.Instance.SelectOutfitSecondary(idx);
        });

        // ---- TAB 5: ANATOMY & MODEL REFERENCE GALLERY PANEL ----
        GameObject referencesPanel = CreateUIObject("ReferencesPanel", panelContainer.transform, Vector2.zero, Vector2.one);

        // Header: Title + Category + Zoom + Page
        GameObject refHeader = CreateUIObject("RefHeader", referencesPanel.transform, new Vector2(0.02f, 0.91f), new Vector2(0.98f, 0.99f));
        GameObject refTitleObj = CreateUIObject("RefTitleTMP", refHeader.transform, new Vector2(0f, 0f), new Vector2(0.52f, 1f));
        TextMeshProUGUI refTitleTMP = refTitleObj.AddComponent<TextMeshProUGUI>();
        refTitleTMP.text = "<color=#FFD700>Male & Female Anatomy Breakdown</color>";
        refTitleTMP.fontSize = 11;
        refTitleTMP.fontStyle = FontStyles.Bold;
        refTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject refCatObj = CreateUIObject("RefCategoryTMP", refHeader.transform, new Vector2(0.53f, 0f), new Vector2(0.74f, 1f));
        TextMeshProUGUI refCatTMP = refCatObj.AddComponent<TextMeshProUGUI>();
        refCatTMP.text = "<color=#8BE9FD>[ANATOMY RATIOS]</color>";
        refCatTMP.fontSize = 9;
        refCatTMP.alignment = TextAlignmentOptions.Center;

        GameObject refPageObj = CreateUIObject("RefPageTMP", refHeader.transform, new Vector2(0.75f, 0f), new Vector2(0.86f, 1f));
        TextMeshProUGUI refPageTMP = refPageObj.AddComponent<TextMeshProUGUI>();
        refPageTMP.text = "1 / 6";
        refPageTMP.fontSize = 10;
        refPageTMP.alignment = TextAlignmentOptions.Center;

        GameObject refZoomBtn = CreateButton("ZoomBtn", refHeader.transform, new Vector2(0.87f, 0f), new Vector2(1f, 1f), "ZOOM");
        refZoomBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
        var zoomLabelTMP = refZoomBtn.GetComponentInChildren<TextMeshProUGUI>();

        // Center Viewport & Image
        GameObject refViewport = CreateUIObject("RefViewport", referencesPanel.transform, new Vector2(0.02f, 0.28f), new Vector2(0.98f, 0.90f));
        Image vpBg = refViewport.AddComponent<Image>();
        vpBg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);

        GameObject refImgObj = CreateUIObject("DisplayImage", refViewport.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
        Image refImg = refImgObj.AddComponent<Image>();
        refImg.preserveAspect = true;
        refImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/References/Ref_Body_Anatomy_Breakdown.jpg");

        // Quick Selector Row: 6 buttons
        GameObject quickBar = CreateUIObject("QuickSelectorBar", referencesPanel.transform, new Vector2(0.02f, 0.17f), new Vector2(0.98f, 0.26f));
        string[] quickNames = new string[] { "ANATOMY", "REF 1", "REF 2", "REF 3", "REF 4", "REF 5" };
        Button[] quickBtns = new Button[quickNames.Length];
        for (int i = 0; i < quickNames.Length; i++)
        {
            float x0 = i * (1f / quickNames.Length) + 0.005f;
            float x1 = (i + 1) * (1f / quickNames.Length) - 0.005f;
            GameObject qb = CreateButton("QuickBtn_" + i, quickBar.transform, new Vector2(x0, 0f), new Vector2(x1, 1f), quickNames[i]);
            qb.GetComponentInChildren<TextMeshProUGUI>().fontSize = 9;
            quickBtns[i] = qb.GetComponent<Button>();
        }

        // Bottom Navigation Bar: Prev, Description, Next
        GameObject navBar = CreateUIObject("NavBar", referencesPanel.transform, new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.15f));
        GameObject prevBtnObj = CreateButton("PrevBtn", navBar.transform, new Vector2(0f, 0f), new Vector2(0.16f, 1f), "< PREV");
        prevBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 10;

        GameObject descObj = CreateUIObject("DescTMP", navBar.transform, new Vector2(0.18f, 0f), new Vector2(0.82f, 1f));
        TextMeshProUGUI descTMP = descObj.AddComponent<TextMeshProUGUI>();
        descTMP.text = "Comprehensive proportion blueprints for Head & Neck, Full Trunk (Torso & Pelvis), Arms & Hands, and Thighs, Legs & Feet.";
        descTMP.fontSize = 9;
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.color = new Color(0.85f, 0.85f, 0.90f, 0.9f);

        GameObject nextBtnObj = CreateButton("NextBtn", navBar.transform, new Vector2(0.84f, 0f), new Vector2(1f, 1f), "NEXT >");
        nextBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 10;

        // Attach & Bind ModelReferenceGalleryViewer
        var galleryViewer = referencesPanel.AddComponent<BattleSummoner.Customization.ModelReferenceGalleryViewer>();
        galleryViewer.BindReferences(
            refImg,
            refTitleTMP,
            refCatTMP,
            descTMP,
            refPageTMP,
            prevBtnObj.GetComponent<Button>(),
            nextBtnObj.GetComponent<Button>(),
            refZoomBtn.GetComponent<Button>(),
            zoomLabelTMP,
            quickBtns
        );

        bodyPanel.SetActive(false);
        facePanel.SetActive(false);
        hairPanel.SetActive(false);
        referencesPanel.SetActive(false);

        // 5. Action Footer Deck
        GameObject footer = CreateUIObject("ActionFooterDeck", frame.transform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.085f));

        GameObject rndBtn = CreateButton("RandomizeBtn", footer.transform, new Vector2(0.00f, 0.05f), new Vector2(0.28f, 0.95f), "RANDOMIZE");
        rndBtn.GetComponent<Image>().sprite = GetSprite("Mockup_CustomizationRandomBtn.png");
        rndBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 11;

        GameObject rstBtn = CreateButton("ResetBtn", footer.transform, new Vector2(0.31f, 0.05f), new Vector2(0.53f, 0.95f), "RESET");
        rstBtn.GetComponent<Image>().sprite = GetSprite("Mockup_CustomizationResetBtn.png");
        rstBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 11;

        GameObject saveBtn = CreateButton("SaveAndApplyBtn", footer.transform, new Vector2(0.56f, 0.05f), new Vector2(1.00f, 0.95f), "APPLY & SAVE");
        saveBtn.GetComponent<Image>().sprite = GetSprite("Mockup_CustomizationSaveBtn.png");
        saveBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 12;
        saveBtn.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.05f, 0.06f, 0.08f);

        // Wire CharacterCustomizationUI Component
        var customUI = overlay.AddComponent<BattleSummoner.Customization.CharacterCustomizationUI>();
        SetField(customUI, "overlayRoot", overlay);
        SetField(customUI, "closeButton", closeBtn.GetComponent<Button>());

        SetField(customUI, "identityTabBtn", tabId.GetComponent<Button>());
        SetField(customUI, "bodyTabBtn", tabBody.GetComponent<Button>());
        SetField(customUI, "faceTabBtn", tabFace.GetComponent<Button>());
        SetField(customUI, "hairTabBtn", tabHair.GetComponent<Button>());
        SetField(customUI, "referencesTabBtn", tabRefs.GetComponent<Button>());

        SetField(customUI, "identityPanel", identityPanel);
        SetField(customUI, "bodyPanel", bodyPanel);
        SetField(customUI, "facePanel", facePanel);
        SetField(customUI, "hairPanel", hairPanel);
        SetField(customUI, "referencesPanel", referencesPanel);
        SetField(customUI, "referenceGalleryViewer", galleryViewer);
        SetField(customUI, "viewAnatomyGuideBtn", viewAnatomyBtn);

        SetField(customUI, "nameInputField", nameInput);
        SetField(customUI, "pronounPreviewText", proPrevTMP);
        SetField(customUI, "sexButtons", sexBtns);
        SetField(customUI, "pronounButtons", proBtns);

        SetField(customUI, "bodyTypeButtons", btBtns);
        SetField(customUI, "neckLengthSlider", neckSlider);
        SetField(customUI, "neckLengthValueTMP", neckValTMP);
        SetField(customUI, "armLengthSlider", armSlider);
        SetField(customUI, "armLengthValueTMP", armValTMP);
        SetField(customUI, "torsoScaleSlider", torsoSlider);
        SetField(customUI, "torsoScaleValueTMP", torsoValTMP);
        SetField(customUI, "hipsScaleSlider", hipsSlider);
        SetField(customUI, "hipsScaleValueTMP", hipsValTMP);
        SetField(customUI, "legsLengthSlider", legsSlider);
        SetField(customUI, "legsLengthValueTMP", legsValTMP);
        SetField(customUI, "feetScaleSlider", feetSlider);
        SetField(customUI, "feetScaleValueTMP", feetValTMP);
        SetField(customUI, "heightScaleSlider", heightSlider);
        SetField(customUI, "heightScaleValueTMP", heightValTMP);

        SetField(customUI, "faceShapeButtons", fsBtns);
        SetField(customUI, "eyeShapeButtons", esBtns);
        SetField(customUI, "noseShapeButtons", nsBtns);
        SetField(customUI, "noseScaleSlider", noseSlider);
        SetField(customUI, "noseScaleValueTMP", noseValTMP);
        SetField(customUI, "mouthShapeButtons", msBtns);
        SetField(customUI, "heterochromiaToggle", hetToggle);
        SetField(customUI, "skinToneContainer", skinSwatches.transform);
        SetField(customUI, "eyeColorContainer", eyeSwatches.transform);
        SetField(customUI, "rightEyeColorContainer", rEyeSwatches.transform);

        SetField(customUI, "hairStyleButtons", hsBtns);
        SetField(customUI, "hairColorContainer", hairSwatches.transform);
        SetField(customUI, "outfitPrimaryContainer", outfitPriSwatches.transform);
        SetField(customUI, "outfitSecondaryContainer", outfitSecSwatches.transform);

        SetField(customUI, "cameraFramingToggleBtn", camToggleBtn.GetComponent<Button>());
        SetField(customUI, "cameraFramingLabelTMP", camLabel);
        SetField(customUI, "rotateLeftBtn", rotLBtn.GetComponent<Button>());
        SetField(customUI, "rotateRightBtn", rotRBtn.GetComponent<Button>());
        SetField(customUI, "autoTurntableToggle", autoSpinToggle);

        SetField(customUI, "randomizeButton", rndBtn.GetComponent<Button>());
        SetField(customUI, "resetButton", rstBtn.GetComponent<Button>());
        SetField(customUI, "saveAndApplyButton", saveBtn.GetComponent<Button>());

        overlay.SetActive(false);
    }

    private static Slider CreateMorphologySlider(string name, Transform parent, float normY, string label, float min, float max, float defVal, out TextMeshProUGUI valLabel)
    {
        GameObject container = CreateUIObject(name + "_Container", parent, new Vector2(0.02f, normY - 0.045f), new Vector2(0.98f, normY + 0.045f));

        GameObject lblObj = CreateUIObject("LabelTMP", container.transform, new Vector2(0f, 0.48f), new Vector2(0.68f, 1f));
        TextMeshProUGUI ltmp = lblObj.AddComponent<TextMeshProUGUI>();
        ltmp.text = label;
        ltmp.fontSize = 11;
        ltmp.color = Color.white;

        GameObject valObj = CreateUIObject("ValueTMP", container.transform, new Vector2(0.70f, 0.48f), new Vector2(1f, 1f));
        valLabel = valObj.AddComponent<TextMeshProUGUI>();
        valLabel.text = $"{defVal:F2}x";
        valLabel.fontSize = 11;
        valLabel.alignment = TextAlignmentOptions.MidlineRight;
        valLabel.color = new Color(0.30f, 0.90f, 1.0f);

        GameObject sliderObj = CreateUIObject("Slider", container.transform, new Vector2(0f, 0f), new Vector2(1f, 0.42f));
        Slider slider = sliderObj.AddComponent<Slider>();

        GameObject bg = CreateUIObject("Background", sliderObj.transform, Vector2.zero, Vector2.one);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = GetSprite("Mockup_CustomizationSliderTrack.png");
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.10f, 0.12f, 0.18f, 0.95f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform, Vector2.zero, Vector2.one);
        GameObject fill = CreateUIObject("Fill", fillArea.transform, Vector2.zero, Vector2.one);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.25f, 0.82f, 1.0f, 0.9f);
        slider.fillRect = fill.GetComponent<RectTransform>();

        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defVal;

        return slider;
    }

    private static void CreateColorSwatches(Transform container, Color[] colors, Action<int> onSelect)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            int idx = i;
            float xMin = (float)i / colors.Length;
            float xMax = (float)(i + 1) / colors.Length - 0.015f;
            GameObject swatchObj = CreateUIObject("Swatch_" + i, container, new Vector2(xMin, 0.05f), new Vector2(xMax, 0.95f));
            Image sImg = swatchObj.AddComponent<Image>();
            sImg.sprite = GetSprite("Mockup_CustomizationColorSwatch.png");
            sImg.color = colors[i];
            Button sBtn = swatchObj.AddComponent<Button>();
            sBtn.onClick.AddListener(() => onSelect?.Invoke(idx));
        }
    }
    #endregion

    #region 8. Multi-Person Dialogue & Text Chat UI
    [MenuItem("Tools/BattleSummoner/Build Multi-Person Dialogue Chat Overlay")]
    public static void BuildMultiPersonDialogueChatMenu()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var canvasGo = GameObject.Find("PortraitHUDCanvas");
        if (canvasGo == null)
        {
            Debug.LogError("PortraitHUDCanvas not found in scene!");
            return;
        }
        BuildMultiPersonDialogueChatUI(canvasGo.transform);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("🎉 [MULTI-PERSON DIALOGUE] Successfully built Multi-Person Dialogue Chat Overlay!");
    }

    private static void BuildMultiPersonDialogueChatUI(Transform parent)
    {
        Transform existing = parent.Find("MultiPersonDialogueChatOverlay");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject overlay = CreateUIObject("MultiPersonDialogueChatOverlay", parent, Vector2.zero, Vector2.one);
        Image bgImg = overlay.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.10f, 0.94f);

        // 1. Back to Visual Novel Button (Top)
        GameObject backBtnObj = CreateButton("BackToVNBtn", overlay.transform, new Vector2(0.25f, 0.945f), new Vector2(0.75f, 0.985f), "<b>Back to Visual Novel</b>");
        Image backBtnImg = backBtnObj.GetComponent<Image>();
        backBtnImg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
        TextMeshProUGUI backBtnTMP = backBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (backBtnTMP != null)
        {
            backBtnTMP.color = new Color(0.85f, 0.80f, 0.70f);
            backBtnTMP.fontSize = 15;
        }

        // 2. Character Busts
        // Left: Alex
        GameObject alexBustObj = CreateUIObject("AlexBust", overlay.transform, new Vector2(0.00f, 0.28f), new Vector2(0.58f, 0.94f));
        Image alexBustImg = alexBustObj.AddComponent<Image>();
        alexBustImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dialogue/Crop_Alex_Bust.png");
        alexBustImg.preserveAspect = true;

        // Right: Elena
        GameObject elenaBustObj = CreateUIObject("ElenaBust", overlay.transform, new Vector2(0.42f, 0.28f), new Vector2(1.00f, 0.94f));
        Image elenaBustImg = elenaBustObj.AddComponent<Image>();
        elenaBustImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dialogue/Crop_Elena_Bust.png");
        elenaBustImg.preserveAspect = true;

        // 3. Dialogue Scroll Area
        GameObject scrollObj = CreateUIObject("DialogueScrollView", overlay.transform, new Vector2(0.03f, 0.15f), new Vector2(0.97f, 0.54f));
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewportObj = CreateUIObject("Viewport", scrollObj.transform, Vector2.zero, Vector2.one);
        Image vpImg = viewportObj.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.01f);
        viewportObj.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportObj.GetComponent<RectTransform>();

        GameObject contentObj = CreateUIObject("Content", viewportObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        RectTransform crt = contentObj.GetComponent<RectTransform>();
        crt.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = crt;

        // 4. Dialogue Controls Row (SKIP, AUTO, LOG, PAUSE)
        GameObject ctrlRowObj = CreateUIObject("DialogueControlsRow", overlay.transform, new Vector2(0.48f, 0.105f), new Vector2(0.97f, 0.145f));
        HorizontalLayoutGroup hlg = ctrlRowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        GameObject skipBtnObj = CreateButton("SkipBtn", ctrlRowObj.transform, Vector2.zero, Vector2.one, "SKIP");
        skipBtnObj.GetComponent<Image>().color = new Color(0.16f, 0.15f, 0.18f, 0.85f);
        var skipTmp = skipBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (skipTmp != null) { skipTmp.fontSize = 12; skipTmp.color = new Color(0.8f, 0.78f, 0.75f); }

        GameObject autoBtnObj = CreateButton("AutoBtn", ctrlRowObj.transform, Vector2.zero, Vector2.one, "AUTO");
        autoBtnObj.GetComponent<Image>().color = new Color(0.16f, 0.15f, 0.18f, 0.85f);
        var autoTmp = autoBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (autoTmp != null) { autoTmp.fontSize = 12; autoTmp.color = new Color(0.8f, 0.78f, 0.75f); }

        GameObject logBtnObj = CreateButton("LogBtn", ctrlRowObj.transform, Vector2.zero, Vector2.one, "LOG");
        logBtnObj.GetComponent<Image>().color = new Color(0.16f, 0.15f, 0.18f, 0.85f);
        var logTmp = logBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (logTmp != null) { logTmp.fontSize = 12; logTmp.color = new Color(0.8f, 0.78f, 0.75f); }

        GameObject pauseBtnObj = CreateButton("PauseBtn", ctrlRowObj.transform, Vector2.zero, Vector2.one, "PAUSE");
        pauseBtnObj.GetComponent<Image>().color = new Color(0.16f, 0.15f, 0.18f, 0.85f);
        var pauseTmp = pauseBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (pauseTmp != null) { pauseTmp.fontSize = 12; pauseTmp.color = new Color(0.8f, 0.78f, 0.75f); }

        // 5. Bottom Text Chat Bar
        GameObject chatBarObj = CreateUIObject("BottomChatBar", overlay.transform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.095f));
        Image chatBarImg = chatBarObj.AddComponent<Image>();
        chatBarImg.color = new Color(0.11f, 0.10f, 0.13f, 0.96f);

        // Plus action button
        GameObject plusBtnObj = CreateButton("PlusBtn", chatBarObj.transform, new Vector2(0.015f, 0.12f), new Vector2(0.12f, 0.88f), "<b>+</b>");
        plusBtnObj.GetComponent<Image>().color = new Color(0.20f, 0.18f, 0.22f, 0.95f);
        var plusTmp = plusBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (plusTmp != null) { plusTmp.fontSize = 22; plusTmp.color = new Color(0.92f, 0.78f, 0.45f); }

        // Input Field
        GameObject inputObj = CreateUIObject("InputField", chatBarObj.transform, new Vector2(0.14f, 0.12f), new Vector2(0.74f, 0.88f));
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = new Color(0.16f, 0.15f, 0.18f, 0.95f);
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

        GameObject placeholderObj = CreateUIObject("Placeholder", inputObj.transform, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
        TextMeshProUGUI placeTMP = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeTMP.text = "type a message...";
        placeTMP.fontSize = 14;
        placeTMP.fontStyle = FontStyles.Italic;
        placeTMP.alignment = TextAlignmentOptions.MidlineLeft;
        placeTMP.color = new Color(0.6f, 0.58f, 0.55f);
        inputField.placeholder = placeTMP;

        GameObject textObj = CreateUIObject("Text", inputObj.transform, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
        TextMeshProUGUI inputTMP = textObj.AddComponent<TextMeshProUGUI>();
        inputTMP.fontSize = 14;
        inputTMP.alignment = TextAlignmentOptions.MidlineLeft;
        inputTMP.color = new Color(0.95f, 0.92f, 0.88f);
        inputField.textComponent = inputTMP;

        // Send Button
        GameObject sendBtnObj = CreateButton("SendBtn", chatBarObj.transform, new Vector2(0.76f, 0.12f), new Vector2(0.985f, 0.88f), "<b>SEND ❯❯</b>");
        sendBtnObj.GetComponent<Image>().color = new Color(0.22f, 0.18f, 0.12f, 0.95f);
        var sendTmp = sendBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (sendTmp != null) { sendTmp.fontSize = 13; sendTmp.color = new Color(0.95f, 0.82f, 0.45f); }

        // 6. Quick Chips Panel
        GameObject quickChipsObj = CreateUIObject("QuickChipsPanel", overlay.transform, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.23f));
        Image qcImg = quickChipsObj.AddComponent<Image>();
        qcImg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        VerticalLayoutGroup qcVlg = quickChipsObj.AddComponent<VerticalLayoutGroup>();
        qcVlg.spacing = 4f;
        qcVlg.padding = new RectOffset(6, 6, 6, 6);
        qcVlg.childControlWidth = true;
        qcVlg.childControlHeight = true;
        qcVlg.childForceExpandWidth = true;
        qcVlg.childForceExpandHeight = true;

        GameObject chip1 = CreateButton("Chip1", quickChipsObj.transform, Vector2.zero, Vector2.one, "💬 The guards look restless. What do you notice?");
        GameObject chip2 = CreateButton("Chip2", quickChipsObj.transform, Vector2.zero, Vector2.one, "⚔️ Should we prepare for the Spire floor boss?");
        GameObject chip3 = CreateButton("Chip3", quickChipsObj.transform, Vector2.zero, Vector2.one, "🛡️ Elena, let's keep our distance for now.");
        quickChipsObj.SetActive(false);

        // 7. Transcript Modal
        GameObject modalObj = CreateUIObject("TranscriptModal", overlay.transform, Vector2.zero, Vector2.one);
        Image modalBg = modalObj.AddComponent<Image>();
        modalBg.color = new Color(0f, 0f, 0f, 0.92f);

        GameObject modalTitle = CreateUIObject("ModalTitleTMP", modalObj.transform, new Vector2(0.08f, 0.90f), new Vector2(0.85f, 0.96f));
        TextMeshProUGUI mtTMP = modalTitle.AddComponent<TextMeshProUGUI>();
        mtTMP.text = "<color=#FFD700><b>DIALOGUE TRANSCRIPT LOG</b></color>";
        mtTMP.fontSize = 18;

        GameObject closeLogBtn = CreateButton("CloseLogBtn", modalObj.transform, new Vector2(0.88f, 0.90f), new Vector2(0.96f, 0.96f), "X");

        GameObject logScroll = CreateUIObject("LogScroll", modalObj.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.88f));
        logScroll.AddComponent<ScrollRect>();
        GameObject logTextObj = CreateUIObject("LogTextTMP", logScroll.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI logTMP = logTextObj.AddComponent<TextMeshProUGUI>();
        logTMP.fontSize = 13;
        logTMP.color = Color.white;
        logTMP.enableWordWrapping = true;
        modalObj.SetActive(false);

        // 8. Attach MultiPersonDialogueChatHub & Bind fields
        var hub = overlay.AddComponent<BattleSummoner.UI.MultiPersonDialogueChatHub>();
        SetField(hub, "overlayRoot", overlay);
        SetField(hub, "backToVNButton", backBtnObj.GetComponent<Button>());
        SetField(hub, "bustAlexImage", alexBustImg);
        SetField(hub, "bustElenaImage", elenaBustImg);
        SetField(hub, "alexBustSprite", alexBustImg.sprite);
        SetField(hub, "elenaBustSprite", elenaBustImg.sprite);

        Sprite alexBadge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dialogue/Crop_Alex_Badge.png");
        Sprite elenaBadge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dialogue/Crop_Elena_Badge.png");
        Sprite playerBadge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Portraits/Portrait_Protagonist.png");
        SetField(hub, "alexBadgeSprite", alexBadge);
        SetField(hub, "elenaBadgeSprite", elenaBadge);
        SetField(hub, "playerBadgeSprite", playerBadge);

        SetField(hub, "messageContainer", contentObj.transform);
        SetField(hub, "dialogueScrollRect", scrollRect);

        SetField(hub, "skipButton", skipBtnObj.GetComponent<Button>());
        SetField(hub, "autoButton", autoBtnObj.GetComponent<Button>());
        SetField(hub, "autoButtonText", autoTmp);
        SetField(hub, "logButton", logBtnObj.GetComponent<Button>());
        SetField(hub, "pauseButton", pauseBtnObj.GetComponent<Button>());
        SetField(hub, "pauseButtonText", pauseTmp);

        SetField(hub, "plusActionButton", plusBtnObj.GetComponent<Button>());
        SetField(hub, "messageInputField", inputField);
        SetField(hub, "sendButton", sendBtnObj.GetComponent<Button>());

        SetField(hub, "quickChipsPanel", quickChipsObj);
        SetField(hub, "quickPromptBtn1", chip1.GetComponent<Button>());
        SetField(hub, "quickPromptBtn2", chip2.GetComponent<Button>());
        SetField(hub, "quickPromptBtn3", chip3.GetComponent<Button>());

        SetField(hub, "transcriptModal", modalObj);
        SetField(hub, "transcriptBodyText", logTMP);
        SetField(hub, "closeTranscriptButton", closeLogBtn.GetComponent<Button>());

        hub.InitializeReferenceConversation();
        overlay.SetActive(false);
    }
    #endregion

    #region 9. Connect HUD Buttons
    private static void ConnectHUDButtons(GameObject canvasGo, Transform hudRoot)
    {
        if (hudRoot == null) return;

        // Settings Button on HUD (top-left) -> opens SettingsMenuUI
        Transform settingsBtnTransform = hudRoot.Find("SettingsButton");
        if (settingsBtnTransform != null)
        {
            Button sBtn = settingsBtnTransform.GetComponent<Button>();
            if (sBtn != null)
            {
                sBtn.onClick.RemoveAllListeners();
                sBtn.onClick.AddListener(() =>
                {
                    if (SettingsMenuUI.Instance != null) SettingsMenuUI.Instance.ToggleSettings();
                });
            }
        }

        // Messenger Bag Button on HUD (top-right) -> opens InventoryMenuUI
        Transform bagBtnTransform = hudRoot.Find("MessengerBagButton");
        if (bagBtnTransform != null)
        {
            Button bBtn = bagBtnTransform.GetComponent<Button>();
            if (bBtn != null)
            {
                bBtn.onClick.RemoveAllListeners();
                bBtn.onClick.AddListener(() =>
                {
                    if (InventoryMenuUI.Instance != null) InventoryMenuUI.Instance.ToggleInventory();
                });
            }
        }

        // Social / Pet Care Button on HUD (next to Messenger Bag) -> opens TomodachiSocialHubUI (Famigotchi Sanctuary)
        Transform socialBtnTransform = hudRoot.Find("SocialLinkButton");
        if (socialBtnTransform == null)
        {
            GameObject socialObj = CreateUIObject("SocialLinkButton", hudRoot, new Vector2(0.73f, 0.795f), new Vector2(0.84f, 0.845f));
            Image sImg = socialObj.AddComponent<Image>();
            sImg.sprite = GetSprite("Mockup_PetCareHudBtn.png");
            sImg.preserveAspect = true;
            socialObj.AddComponent<Button>();

            GameObject lblObj = CreateUIObject("Label", socialObj.transform, new Vector2(0f, -0.45f), new Vector2(1f, 0f));
            TextMeshProUGUI lTmp = lblObj.AddComponent<TextMeshProUGUI>();
            lTmp.text = "Pet Care";
            lTmp.fontSize = 9;
            lTmp.alignment = TextAlignmentOptions.Center;
            lTmp.color = new Color(1.0f, 0.85f, 0.35f);

            socialBtnTransform = socialObj.transform;
        }
        else
        {
            RectTransform srt = socialBtnTransform.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.73f, 0.795f);
            srt.anchorMax = new Vector2(0.84f, 0.845f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            Image sImg = socialBtnTransform.GetComponent<Image>();
            if (sImg != null) sImg.sprite = GetSprite("Mockup_PetCareHudBtn.png");

            var lblTmp = socialBtnTransform.GetComponentInChildren<TextMeshProUGUI>();
            if (lblTmp != null) lblTmp.text = "Pet Care";
        }

        if (socialBtnTransform != null)
        {
            Button sBtn = socialBtnTransform.GetComponent<Button>();
            if (sBtn != null)
            {
                sBtn.onClick.RemoveAllListeners();
                sBtn.onClick.AddListener(() =>
                {
                    if (BattleSummoner.Social.TomodachiSocialHubUI.Instance != null)
                    {
                        BattleSummoner.Social.TomodachiSocialHubUI.Instance.ToggleSocialHub();
                    }
                });
            }
        }

        // Character Customization Button on HUD (next to Social Link) -> opens CharacterCustomizationUI
        Transform customBtnTransform = hudRoot.Find("CustomizeAvatarButton");
        if (customBtnTransform == null)
        {
            GameObject customObj = CreateUIObject("CustomizeAvatarButton", hudRoot, new Vector2(0.60f, 0.795f), new Vector2(0.71f, 0.845f));
            Image cImg = customObj.AddComponent<Image>();
            cImg.sprite = GetSprite("Mockup_CustomizeHudBtn.png");
            cImg.preserveAspect = true;
            customObj.AddComponent<Button>();

            GameObject lblObj = CreateUIObject("Label", customObj.transform, new Vector2(0f, -0.45f), new Vector2(1f, 0f));
            TextMeshProUGUI lTmp = lblObj.AddComponent<TextMeshProUGUI>();
            lTmp.text = "Avatar";
            lTmp.fontSize = 9;
            lTmp.alignment = TextAlignmentOptions.Center;
            lTmp.color = new Color(1.0f, 0.85f, 0.35f);

            customBtnTransform = customObj.transform;
        }
        else
        {
            RectTransform crt = customBtnTransform.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.60f, 0.795f);
            crt.anchorMax = new Vector2(0.71f, 0.845f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
        }

        if (customBtnTransform != null)
        {
            Button cBtn = customBtnTransform.GetComponent<Button>();
            if (cBtn != null)
            {
                cBtn.onClick.RemoveAllListeners();
                cBtn.onClick.AddListener(() =>
                {
                    if (BattleSummoner.Customization.CharacterCustomizationUI.Instance != null)
                    {
                        BattleSummoner.Customization.CharacterCustomizationUI.Instance.ToggleCustomizationScreen();
                    }
                });
            }
        }

        // Dialogue Chat Hub Button on HUD (next to Customization) -> opens MultiPersonDialogueChatHub
        Transform chatBtnTransform = hudRoot.Find("DialogueChatButton");
        if (chatBtnTransform == null)
        {
            GameObject chatObj = CreateUIObject("DialogueChatButton", hudRoot, new Vector2(0.47f, 0.795f), new Vector2(0.58f, 0.845f));
            Image cImg = chatObj.AddComponent<Image>();
            cImg.sprite = GetSprite("Mockup_PersonaDialogueBox.png");
            cImg.preserveAspect = true;
            chatObj.AddComponent<Button>();

            GameObject lblObj = CreateUIObject("Label", chatObj.transform, new Vector2(0f, -0.45f), new Vector2(1f, 0f));
            TextMeshProUGUI lTmp = lblObj.AddComponent<TextMeshProUGUI>();
            lTmp.text = "Chat / VN";
            lTmp.fontSize = 9;
            lTmp.alignment = TextAlignmentOptions.Center;
            lTmp.color = new Color(1.0f, 0.85f, 0.35f);

            chatBtnTransform = chatObj.transform;
        }

        if (chatBtnTransform != null)
        {
            Button cBtn = chatBtnTransform.GetComponent<Button>();
            if (cBtn != null)
            {
                cBtn.onClick.RemoveAllListeners();
                cBtn.onClick.AddListener(() =>
                {
                    if (BattleSummoner.UI.MultiPersonDialogueChatHub.Instance != null)
                    {
                        BattleSummoner.UI.MultiPersonDialogueChatHub.Instance.SetVisible(true);
                    }
                });
            }
        }

        // Connect Store, Ritual, Social, and Customization buttons in PortraitHUDController if attached
        PortraitHUDController hudCtrl = canvasGo.GetComponentInChildren<PortraitHUDController>();
        if (hudCtrl != null)
        {
            GameObject storeObj = GameObject.Find("ItemShopOverlay");
            if (storeObj != null) SetField(hudCtrl, "storeModal", storeObj);

            if (socialBtnTransform != null)
            {
                SetField(hudCtrl, "socialLinkButton", socialBtnTransform.GetComponent<Button>());
            }

            if (customBtnTransform != null)
            {
                SetField(hudCtrl, "customizeAvatarButton", customBtnTransform.GetComponent<Button>());
            }
        }
    }
    #endregion

    #region Helpers
    private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, string label)
    {
        GameObject btnObj = CreateUIObject(name, parent, anchorMin, anchorMax);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);
        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = CreateUIObject("LabelTMP", btnObj.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }

    private static Slider CreateSettingSlider(string name, Transform parent, float normY, string label, out TextMeshProUGUI valLabel)
    {
        GameObject container = CreateUIObject(name + "_Container", parent, new Vector2(0.04f, normY - 0.08f), new Vector2(0.96f, normY + 0.08f));

        GameObject lblObj = CreateUIObject("LabelTMP", container.transform, new Vector2(0f, 0.52f), new Vector2(0.70f, 1f));
        TextMeshProUGUI ltmp = lblObj.AddComponent<TextMeshProUGUI>();
        ltmp.text = label;
        ltmp.fontSize = 13;

        GameObject valObj = CreateUIObject("ValueTMP", container.transform, new Vector2(0.72f, 0.52f), new Vector2(1f, 1f));
        valLabel = valObj.AddComponent<TextMeshProUGUI>();
        valLabel.text = "100%";
        valLabel.fontSize = 13;
        valLabel.alignment = TextAlignmentOptions.MidlineRight;

        GameObject sliderObj = CreateUIObject("Slider", container.transform, new Vector2(0f, 0f), new Vector2(1f, 0.45f));
        Slider slider = sliderObj.AddComponent<Slider>();

        GameObject bg = CreateUIObject("Background", sliderObj.transform, Vector2.zero, Vector2.one);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.06f, 0.08f, 0.95f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform, Vector2.zero, Vector2.one);
        GameObject fill = CreateUIObject("Fill", fillArea.transform, Vector2.zero, Vector2.one);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.20f, 0.85f, 1.0f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }

    private static Toggle CreateSettingToggle(string name, Transform parent, float normY, string label)
    {
        GameObject container = CreateUIObject(name + "_Container", parent, new Vector2(0.04f, normY - 0.05f), new Vector2(0.96f, normY + 0.05f));

        GameObject lblObj = CreateUIObject("LabelTMP", container.transform, new Vector2(0f, 0f), new Vector2(0.75f, 1f));
        TextMeshProUGUI ltmp = lblObj.AddComponent<TextMeshProUGUI>();
        ltmp.text = label;
        ltmp.fontSize = 14;
        ltmp.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject toggleObj = CreateUIObject("Toggle", container.transform, new Vector2(0.80f, 0.1f), new Vector2(0.96f, 0.9f));
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        Image bg = toggleObj.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.12f, 0.16f, 1f);

        GameObject checkObj = CreateUIObject("Checkmark", toggleObj.transform, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
        Image check = checkObj.AddComponent<Image>();
        check.color = new Color(0.30f, 0.95f, 0.40f, 1f);
        toggle.graphic = check;
        toggle.isOn = true;

        return toggle;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(target, value);
    }
    #endregion
}

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class CharacterStudioUIBuilder
{
    [MenuItem("BattleSummoner/Build Character Studio UI")]
    public static string BuildStudioUI()
    {
        var canvasGo = GameObject.Find("CharacterCreatorCanvas");
        if (canvasGo == null) return "CharacterCreatorCanvas not found!";

        // Ensure EventSystem exists
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Clean existing children
        while (canvasGo.transform.childCount > 0)
        {
            Object.DestroyImmediate(canvasGo.transform.GetChild(0).gameObject);
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1.0f;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var ui = canvasGo.GetComponent<SimsCharacterCreatorUI>();
        ui.AutoBindReferences();

        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        TextMeshProUGUI CreateTMP(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var rt = CreateUIObject(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        Button CreateButton(string name, Transform parent, string label, Color bgColor, Color txtColor)
        {
            var rt = CreateUIObject(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bgColor;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor = bgColor * 0.75f;
            btn.colors = colors;

            var txt = CreateTMP("Text", rt, label, 12f, txtColor, TextAlignmentOptions.Center);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.sizeDelta = Vector2.zero;
            return btn;
        }

        (Slider slider, TextMeshProUGUI valTxt) CreateSliderRow(string name, Transform parent, string labelText, float minVal, float maxVal, float defVal)
        {
            var row = CreateUIObject(name, parent);
            row.sizeDelta = new Vector2(390, 34);

            var lbl = CreateTMP("Label", row, labelText, 12f, new Color(0.85f, 0.90f, 0.95f), TextAlignmentOptions.MidlineLeft);
            lbl.rectTransform.anchorMin = new Vector2(0, 0);
            lbl.rectTransform.anchorMax = new Vector2(0.38f, 1);
            lbl.rectTransform.sizeDelta = Vector2.zero;

            var sliderRt = CreateUIObject("Slider", row);
            sliderRt.anchorMin = new Vector2(0.40f, 0.22f);
            sliderRt.anchorMax = new Vector2(0.80f, 0.78f);
            sliderRt.sizeDelta = Vector2.zero;

            var bg = CreateUIObject("Background", sliderRt);
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.sizeDelta = Vector2.zero;
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.14f, 0.22f, 0.85f);

            var fillArea = CreateUIObject("Fill Area", sliderRt);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.sizeDelta = new Vector2(-10, 0);

            var fill = CreateUIObject("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.sizeDelta = Vector2.zero;
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(0.22f, 0.74f, 0.97f, 0.95f);

            var handleArea = CreateUIObject("Handle Slide Area", sliderRt);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.sizeDelta = new Vector2(-10, 0);

            var handle = CreateUIObject("Handle", handleArea);
            handle.sizeDelta = new Vector2(14, 20);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = Color.white;

            var s = sliderRt.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = handleImg;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = minVal;
            s.maxValue = maxVal;
            s.value = defVal;

            var val = CreateTMP("Value", row, $"{defVal:F2}x", 12f, new Color(0.22f, 0.74f, 0.97f), TextAlignmentOptions.MidlineRight);
            val.rectTransform.anchorMin = new Vector2(0.82f, 0);
            val.rectTransform.anchorMax = new Vector2(1.0f, 1);
            val.rectTransform.sizeDelta = Vector2.zero;

            return (s, val);
        }

        // --- Main Left Studio Panel ---
        var panelRt = CreateUIObject("Studio_LeftPanel", canvasGo.transform);
        panelRt.anchorMin = new Vector2(0, 0);
        panelRt.anchorMax = new Vector2(0, 1);
        panelRt.pivot = new Vector2(0, 0.5f);
        panelRt.anchoredPosition = new Vector2(25, 0);
        panelRt.sizeDelta = new Vector2(430, -50);

        var panelImg = panelRt.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.08f, 0.14f, 0.94f); // Deep frosted dark glass

        // Header Title
        var headerTitle = CreateTMP("Header_Title", panelRt, "BATTLE SUMMONER", 20f, new Color(0.22f, 0.74f, 0.97f), TextAlignmentOptions.TopLeft);
        headerTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.94f);
        headerTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
        headerTitle.rectTransform.sizeDelta = Vector2.zero;

        var headerSub = CreateTMP("Header_Sub", panelRt, "SIMS ANIME CHARACTER MATRIX • 12D TELEMETRY", 10f, new Color(0.58f, 0.64f, 0.72f), TextAlignmentOptions.TopLeft);
        headerSub.rectTransform.anchorMin = new Vector2(0.05f, 0.91f);
        headerSub.rectTransform.anchorMax = new Vector2(0.95f, 0.94f);
        headerSub.rectTransform.sizeDelta = Vector2.zero;

        // Archetype Selector Buttons
        var btnMale = CreateButton("Btn_Male", panelRt, "MALE HERO", new Color(0.15f, 0.23f, 0.36f), Color.white);
        btnMale.GetComponent<RectTransform>().anchorMin = new Vector2(0.05f, 0.85f);
        btnMale.GetComponent<RectTransform>().anchorMax = new Vector2(0.33f, 0.89f);
        btnMale.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnMale.onClick.AddListener(() => ui.SetViewMode(0));

        var btnFemale = CreateButton("Btn_Female", panelRt, "FEMALE MAGE", new Color(0.15f, 0.23f, 0.36f), Color.white);
        btnFemale.GetComponent<RectTransform>().anchorMin = new Vector2(0.36f, 0.85f);
        btnFemale.GetComponent<RectTransform>().anchorMax = new Vector2(0.64f, 0.89f);
        btnFemale.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnFemale.onClick.AddListener(() => ui.SetViewMode(1));

        var btnDual = CreateButton("Btn_Dual", panelRt, "DUAL VIEW", new Color(0.18f, 0.32f, 0.50f), new Color(0.38f, 0.85f, 0.98f));
        btnDual.GetComponent<RectTransform>().anchorMin = new Vector2(0.67f, 0.85f);
        btnDual.GetComponent<RectTransform>().anchorMax = new Vector2(0.95f, 0.89f);
        btnDual.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnDual.onClick.AddListener(() => ui.SetViewMode(2));

        // Tab Navigation (5 tabs)
        var tabMorphBtn = CreateButton("Tab_Morph", panelRt, "ANATOMY", new Color(0.09f, 0.14f, 0.22f), Color.white);
        tabMorphBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.04f, 0.79f);
        tabMorphBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.21f, 0.83f);
        tabMorphBtn.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        tabMorphBtn.onClick.AddListener(() => ui.SwitchTab(0));

        var tabWardrobeBtn = CreateButton("Tab_Wardrobe", panelRt, "WARDROBE", new Color(0.09f, 0.14f, 0.22f), Color.white);
        tabWardrobeBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.225f, 0.79f);
        tabWardrobeBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.395f, 0.83f);
        tabWardrobeBtn.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        tabWardrobeBtn.onClick.AddListener(() => ui.SwitchTab(1));

        var tabPortraitBtn = CreateButton("Tab_Portrait", panelRt, "PORTRAIT", new Color(0.22f, 0.12f, 0.35f), new Color(1f, 0.85f, 0.3f));
        tabPortraitBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.41f, 0.79f);
        tabPortraitBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.58f, 0.83f);
        tabPortraitBtn.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        tabPortraitBtn.onClick.AddListener(() => ui.SwitchTab(2));

        var tabTelemetryBtn = CreateButton("Tab_Telemetry", panelRt, "12D MATRIX", new Color(0.09f, 0.14f, 0.22f), Color.white);
        tabTelemetryBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.595f, 0.79f);
        tabTelemetryBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.765f, 0.83f);
        tabTelemetryBtn.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        tabTelemetryBtn.onClick.AddListener(() => ui.SwitchTab(3));

        var tabEnvBtn = CreateButton("Tab_Environment", panelRt, "WORLD & SKY", new Color(0.12f, 0.22f, 0.35f), new Color(0.38f, 0.85f, 0.98f));
        tabEnvBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.78f, 0.79f);
        tabEnvBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.96f, 0.83f);
        tabEnvBtn.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        tabEnvBtn.onClick.AddListener(() => ui.SwitchTab(4));

        // Content Container
        var contentRt = CreateUIObject("Content_Area", panelRt);
        contentRt.anchorMin = new Vector2(0.05f, 0.085f);
        contentRt.anchorMax = new Vector2(0.95f, 0.77f);
        contentRt.sizeDelta = Vector2.zero;

        // --- Bottom Dive Into World Button ---
        var btnEnterWorld = CreateButton("Btn_Enter_World", panelRt, "DIVE INTO WORLD", new Color(0.95f, 0.68f, 0.15f, 0.95f), new Color(0.04f, 0.07f, 0.14f));
        var enterRt = btnEnterWorld.GetComponent<RectTransform>();
        enterRt.anchorMin = new Vector2(0.05f, 0.018f);
        enterRt.anchorMax = new Vector2(0.95f, 0.072f);
        enterRt.sizeDelta = Vector2.zero;
        var enterTxt = btnEnterWorld.GetComponentInChildren<TextMeshProUGUI>();
        if (enterTxt != null)
        {
            enterTxt.fontSize = 13.5f;
            enterTxt.fontStyle = FontStyles.Bold;
        }
        btnEnterWorld.onClick.AddListener(() => ui.EnterWorldWithCharacter());
        ui.enterWorldButton = btnEnterWorld;

        // --- TAB 1: MORPH PANEL ---
        var morphPanel = CreateUIObject("Tab_Morph_Panel", contentRt);
        morphPanel.anchorMin = Vector2.zero;
        morphPanel.anchorMax = Vector2.one;
        morphPanel.sizeDelta = Vector2.zero;
        var morphLayout = morphPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        morphLayout.childControlHeight = false;
        morphLayout.childControlWidth = true;
        morphLayout.spacing = 6f;
        morphLayout.padding = new RectOffset(0, 0, 10, 0);

        var skinTitle = CreateTMP("Title_SkinTone", morphPanel, "SKIN TONE COMPLEXION", 13f, new Color(0.38f, 0.85f, 0.98f));
        skinTitle.rectTransform.sizeDelta = new Vector2(390, 22);

        var rowSkin = CreateUIObject("Row_SkinTone", morphPanel);
        rowSkin.sizeDelta = new Vector2(390, 32);
        string[] skinNames = new[] { "FAIR", "PEACH", "BEIGE", "TAN", "BRONZE", "DARK" };
        for (int i = 0; i < SimsCharacterCreatorUI.SkinTonePresets.Length; i++)
        {
            int sIdx = i;
            var sBtn = CreateButton($"Btn_Skin_{sIdx}", rowSkin, skinNames[i], SimsCharacterCreatorUI.SkinTonePresets[i], sIdx > 3 ? Color.white : new Color(0.08f, 0.08f, 0.12f));
            var sRt = sBtn.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(i * (1f / 6f) + 0.005f, 0f);
            sRt.anchorMax = new Vector2((i + 1) * (1f / 6f) - 0.005f, 1f);
            sRt.sizeDelta = Vector2.zero;
            sBtn.onClick.AddListener(() => ui.SetSkinTone(sIdx));
        }

        var propTitle = CreateTMP("Title_Proportions", morphPanel, "BODY PROPORTIONS", 13f, new Color(0.38f, 0.85f, 0.98f));
        propTitle.rectTransform.sizeDelta = new Vector2(390, 22);

        var (hS, hT) = CreateSliderRow("Row_Height", morphPanel, "Body Height", 0.80f, 1.25f, 1.0f);
        var (hdS, hdT) = CreateSliderRow("Row_Head", morphPanel, "Head Scale", 0.80f, 1.30f, 1.0f);
        var (shS, shT) = CreateSliderRow("Row_Shoulders", morphPanel, "Shoulder Width", 0.70f, 1.40f, 1.0f);
        var (chS, chT) = CreateSliderRow("Row_Chest", morphPanel, "Chest Volume", 0.70f, 1.40f, 1.0f);
        var (waS, waT) = CreateSliderRow("Row_Waist", morphPanel, "Waist Width", 0.70f, 1.40f, 1.0f);
        var (hiS, hiT) = CreateSliderRow("Row_Hips", morphPanel, "Hip Width", 0.70f, 1.40f, 1.0f);
        var (arS, arT) = CreateSliderRow("Row_Arms", morphPanel, "Arm Thickness", 0.80f, 1.30f, 1.0f);
        var (lgS, lgT) = CreateSliderRow("Row_Legs", morphPanel, "Leg Thickness", 0.80f, 1.30f, 1.0f);
        var (muS, muT) = CreateSliderRow("Row_Muscle", morphPanel, "Muscle (4D Mass)", 10f, 60f, 25f);
        muT.text = "25 kg";

        var rowBtns = CreateUIObject("Row_Actions", morphPanel);
        rowBtns.sizeDelta = new Vector2(390, 36);
        var btnReset = CreateButton("Btn_Reset", rowBtns, "↺ RESET DEFAULTS", new Color(0.20f, 0.25f, 0.35f), Color.white);
        btnReset.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnReset.GetComponent<RectTransform>().anchorMax = new Vector2(0.48f, 1);
        btnReset.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnReset.onClick.AddListener(() => ui.ResetProportions());

        var btnRandom = CreateButton("Btn_Random", rowBtns, "🎲 RANDOMIZE", new Color(0.18f, 0.40f, 0.32f), new Color(0.40f, 0.95f, 0.65f));
        btnRandom.GetComponent<RectTransform>().anchorMin = new Vector2(0.52f, 0);
        btnRandom.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnRandom.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnRandom.onClick.AddListener(() => ui.RandomizeProportions());

        ui.morphTabPanel = morphPanel.gameObject;
        ui.heightSlider = hS; ui.heightValueText = hT;
        ui.headSlider = hdS; ui.headValueText = hdT;
        ui.shouldersSlider = shS; ui.shouldersValueText = shT;
        ui.chestSlider = chS; ui.chestValueText = chT;
        ui.waistSlider = waS; ui.waistValueText = waT;
        ui.hipsSlider = hiS; ui.hipsValueText = hiT;
        ui.armsSlider = arS; ui.armsValueText = arT;
        ui.legsSlider = lgS; ui.legsValueText = lgT;
        ui.muscleSlider = muS; ui.muscleValueText = muT;

        // --- TAB 2: WARDROBE PANEL ---
        var wardrobePanel = CreateUIObject("Tab_Wardrobe_Panel", contentRt);
        wardrobePanel.anchorMin = Vector2.zero;
        wardrobePanel.anchorMax = Vector2.one;
        wardrobePanel.sizeDelta = Vector2.zero;
        var wardLayout = wardrobePanel.gameObject.AddComponent<VerticalLayoutGroup>();
        wardLayout.childControlHeight = false;
        wardLayout.childControlWidth = true;
        wardLayout.spacing = 8f;
        wardLayout.padding = new RectOffset(0, 0, 10, 0);

        var wardTitle1 = CreateTMP("Title_Armor", wardrobePanel, "MODULAR ARMOR PIECES", 13f, new Color(0.38f, 0.85f, 0.98f));
        wardTitle1.rectTransform.sizeDelta = new Vector2(390, 24);

        bool hOn = false, tOn = false, gOn = false, lOn = false, sOn = false;
        var btnHelmet = CreateButton("Btn_Toggle_Helmet", wardrobePanel, "TOGGLE KNIGHT HELMET", new Color(0.15f, 0.22f, 0.32f), Color.white);
        btnHelmet.GetComponent<RectTransform>().sizeDelta = new Vector2(390, 34);
        btnHelmet.onClick.AddListener(() => { hOn = !hOn; ui.ToggleHelmet(hOn); });

        var btnTorso = CreateButton("Btn_Toggle_Torso", wardrobePanel, "TOGGLE ADVENTURER CUIRASS", new Color(0.15f, 0.22f, 0.32f), Color.white);
        btnTorso.GetComponent<RectTransform>().sizeDelta = new Vector2(390, 34);
        btnTorso.onClick.AddListener(() => { tOn = !tOn; ui.ToggleTorso(tOn); });

        var btnGauntlets = CreateButton("Btn_Toggle_Gauntlets", wardrobePanel, "TOGGLE ARMORED GAUNTLETS", new Color(0.15f, 0.22f, 0.32f), Color.white);
        btnGauntlets.GetComponent<RectTransform>().sizeDelta = new Vector2(390, 34);
        btnGauntlets.onClick.AddListener(() => { gOn = !gOn; ui.ToggleGauntlets(gOn); });

        var btnLeggings = CreateButton("Btn_Toggle_Leggings", wardrobePanel, "TOGGLE ARMORED LEGGINGS", new Color(0.15f, 0.22f, 0.32f), Color.white);
        btnLeggings.GetComponent<RectTransform>().sizeDelta = new Vector2(390, 34);
        btnLeggings.onClick.AddListener(() => { lOn = !lOn; ui.ToggleLeggings(lOn); });

        var btnShoes = CreateButton("Btn_Toggle_Shoes", wardrobePanel, "TOGGLE ARMORED BOOTS", new Color(0.15f, 0.22f, 0.32f), Color.white);
        btnShoes.GetComponent<RectTransform>().sizeDelta = new Vector2(390, 34);
        btnShoes.onClick.AddListener(() => { sOn = !sOn; ui.ToggleShoes(sOn); });

        var wardTitle2 = CreateTMP("Title_Weapons", wardrobePanel, "ARSENAL WEAPONS", 13f, new Color(0.38f, 0.85f, 0.98f));
        wardTitle2.rectTransform.sizeDelta = new Vector2(390, 24);

        var rowW1 = CreateUIObject("Row_Weapons_1", wardrobePanel);
        rowW1.sizeDelta = new Vector2(390, 34);
        var btnWNone = CreateButton("Btn_WNone", rowW1, "UNARMED", new Color(0.25f, 0.15f, 0.15f), Color.white);
        btnWNone.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnWNone.GetComponent<RectTransform>().anchorMax = new Vector2(0.48f, 1);
        btnWNone.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnWNone.onClick.AddListener(() => ui.EquipWeapon("none"));

        var btnWSword = CreateButton("Btn_WSword", rowW1, "KNIGHT SWORD", new Color(0.20f, 0.30f, 0.45f), Color.white);
        btnWSword.GetComponent<RectTransform>().anchorMin = new Vector2(0.52f, 0);
        btnWSword.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnWSword.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnWSword.onClick.AddListener(() => ui.EquipWeapon("sword"));

        var rowW2 = CreateUIObject("Row_Weapons_2", wardrobePanel);
        rowW2.sizeDelta = new Vector2(390, 34);
        var btnWAxe = CreateButton("Btn_WAxe", rowW2, "BATTLE AXE", new Color(0.20f, 0.30f, 0.45f), Color.white);
        btnWAxe.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnWAxe.GetComponent<RectTransform>().anchorMax = new Vector2(0.31f, 1);
        btnWAxe.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnWAxe.onClick.AddListener(() => ui.EquipWeapon("axe"));

        var btnWStaff = CreateButton("Btn_WStaff", rowW2, "STAFF", new Color(0.20f, 0.30f, 0.45f), Color.white);
        btnWStaff.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0);
        btnWStaff.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 1);
        btnWStaff.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnWStaff.onClick.AddListener(() => ui.EquipWeapon("staff"));

        var btnWWand = CreateButton("Btn_WWand", rowW2, "WAND", new Color(0.20f, 0.30f, 0.45f), Color.white);
        btnWWand.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0);
        btnWWand.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnWWand.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnWWand.onClick.AddListener(() => ui.EquipWeapon("wand"));

        var wardTitle3 = CreateTMP("Title_Dye", wardrobePanel, "ARMOR DYE PALETTE", 13f, new Color(0.38f, 0.85f, 0.98f));
        wardTitle3.rectTransform.sizeDelta = new Vector2(390, 22);

        var rowDye = CreateUIObject("Row_ArmorDye", wardrobePanel);
        rowDye.sizeDelta = new Vector2(390, 32);
        string[] dyeNames = new[] { "BLUE", "RED", "GREEN", "GOLD", "VOID", "DARK" };
        for (int i = 0; i < SimsCharacterCreatorUI.ArmorDyePresets.Length; i++)
        {
            int dIdx = i;
            var dBtn = CreateButton($"Btn_Dye_{dIdx}", rowDye, dyeNames[i], SimsCharacterCreatorUI.ArmorDyePresets[i], Color.white);
            var dRt = dBtn.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(i * (1f / 6f) + 0.005f, 0f);
            dRt.anchorMax = new Vector2((i + 1) * (1f / 6f) - 0.005f, 1f);
            dRt.sizeDelta = Vector2.zero;
            dBtn.onClick.AddListener(() => ui.SetArmorDye(dIdx));
        }

        var rowQuick = CreateUIObject("Row_Quick_Sets", wardrobePanel);
        rowQuick.sizeDelta = new Vector2(390, 36);
        var btnFullGear = CreateButton("Btn_FullGear", rowQuick, "EQUIP FULL SET", new Color(0.18f, 0.45f, 0.30f), Color.white);
        btnFullGear.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnFullGear.GetComponent<RectTransform>().anchorMax = new Vector2(0.48f, 1);
        btnFullGear.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnFullGear.onClick.AddListener(() => { hOn = tOn = gOn = lOn = sOn = true; ui.EquipFullSet(); });

        var btnStrip = CreateButton("Btn_Strip", rowQuick, "STRIP TO BASE", new Color(0.45f, 0.20f, 0.20f), Color.white);
        btnStrip.GetComponent<RectTransform>().anchorMin = new Vector2(0.52f, 0);
        btnStrip.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnStrip.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnStrip.onClick.AddListener(() => { hOn = tOn = gOn = lOn = sOn = false; ui.StripAllGear(); });

        ui.wardrobeTabPanel = wardrobePanel.gameObject;
        wardrobePanel.gameObject.SetActive(false);

        // --- TAB 3: PORTRAIT PANEL (PERSONA BATTLE SIDE PROFILE HUD) ---
        var portraitPanel = CreateUIObject("Tab_Portrait_Panel", contentRt);
        portraitPanel.anchorMin = Vector2.zero;
        portraitPanel.anchorMax = Vector2.one;
        portraitPanel.sizeDelta = Vector2.zero;
        var pLayout = portraitPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        pLayout.childControlHeight = false;
        pLayout.childControlWidth = true;
        pLayout.spacing = 5f;
        pLayout.padding = new RectOffset(0, 0, 6, 0);

        // Header Title
        var pTitle = CreateTMP("Title_Portrait", portraitPanel, "PORTRAIT TEMPLATES & ASSETS", 12.5f, new Color(1f, 0.85f, 0.25f));
        pTitle.rectTransform.sizeDelta = new Vector2(390, 18);

        // Row 1: Gender Base & Template Toggle
        var rowPBase = CreateUIObject("Row_PBase", portraitPanel);
        rowPBase.sizeDelta = new Vector2(390, 30);
        var btnPMasc = CreateButton("Btn_PMasc", rowPBase, "BASE (MASC)", new Color(0.20f, 0.28f, 0.45f), Color.white);
        btnPMasc.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnPMasc.GetComponent<RectTransform>().anchorMax = new Vector2(0.31f, 1);
        btnPMasc.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnPMasc.onClick.AddListener(() => ui.SetPortraitGender(false));

        var btnPFeme = CreateButton("Btn_PFeme", rowPBase, "BASE (FEME)", new Color(0.38f, 0.20f, 0.35f), Color.white);
        btnPFeme.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0);
        btnPFeme.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 1);
        btnPFeme.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnPFeme.onClick.AddListener(() => ui.SetPortraitGender(true));

        var btnPTemplate = CreateButton("Btn_PTemplate", rowPBase, "TEMPLATE 1:1", new Color(0.25f, 0.18f, 0.40f), new Color(1f, 0.9f, 0.4f));
        btnPTemplate.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0);
        btnPTemplate.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnPTemplate.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnPTemplate.onClick.AddListener(() => ui.SetPortraitDirectTemplateMode(true));

        // Hair Section
        var pSecHair = CreateTMP("Sec_Hair", portraitPanel, "CUSTOMIZABLE: HAIR", 11f, new Color(0.70f, 0.80f, 0.95f));
        pSecHair.rectTransform.sizeDelta = new Vector2(390, 16);

        var rowHair = CreateUIObject("Row_Hair", portraitPanel);
        rowHair.sizeDelta = new Vector2(390, 28);
        var bH0 = CreateButton("Btn_H0", rowHair, "BALD", new Color(0.12f, 0.16f, 0.24f), Color.white);
        bH0.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        bH0.GetComponent<RectTransform>().anchorMax = new Vector2(0.22f, 1);
        bH0.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bH0.onClick.AddListener(() => ui.SetPortraitHair(0));

        var bH1 = CreateButton("Btn_H1", rowHair, "SPIKY (M)", new Color(0.18f, 0.24f, 0.35f), Color.white);
        bH1.GetComponent<RectTransform>().anchorMin = new Vector2(0.25f, 0);
        bH1.GetComponent<RectTransform>().anchorMax = new Vector2(0.48f, 1);
        bH1.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bH1.onClick.AddListener(() => ui.SetPortraitHair(1));

        var bH2 = CreateButton("Btn_H2", rowHair, "SHAGGY", new Color(0.18f, 0.24f, 0.35f), Color.white);
        bH2.GetComponent<RectTransform>().anchorMin = new Vector2(0.51f, 0);
        bH2.GetComponent<RectTransform>().anchorMax = new Vector2(0.74f, 1);
        bH2.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bH2.onClick.AddListener(() => ui.SetPortraitHair(2));

        var bH3 = CreateButton("Btn_H3", rowHair, "LONG (F)", new Color(0.18f, 0.24f, 0.35f), Color.white);
        bH3.GetComponent<RectTransform>().anchorMin = new Vector2(0.77f, 0);
        bH3.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        bH3.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bH3.onClick.AddListener(() => ui.SetPortraitHair(3));

        // Face & Expression Section
        var pSecFace = CreateTMP("Sec_Face", portraitPanel, "CUSTOMIZABLE: FACE & NOSE", 11f, new Color(0.70f, 0.80f, 0.95f));
        pSecFace.rectTransform.sizeDelta = new Vector2(390, 16);

        var rowFace1 = CreateUIObject("Row_Face1", portraitPanel);
        rowFace1.sizeDelta = new Vector2(390, 28);
        var bF0 = CreateButton("Btn_F0", rowFace1, "DEFAULT", new Color(0.12f, 0.16f, 0.24f), Color.white);
        bF0.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        bF0.GetComponent<RectTransform>().anchorMax = new Vector2(0.31f, 1);
        bF0.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bF0.onClick.AddListener(() => ui.SetPortraitFace(0));

        var bF1 = CreateButton("Btn_F1", rowFace1, "MASC NEUTRAL", new Color(0.18f, 0.24f, 0.35f), Color.white);
        bF1.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0);
        bF1.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 1);
        bF1.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bF1.onClick.AddListener(() => ui.SetPortraitFace(1));

        var bF2 = CreateButton("Btn_F2", rowFace1, "FEME PARTED", new Color(0.18f, 0.24f, 0.35f), Color.white);
        bF2.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0);
        bF2.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        bF2.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bF2.onClick.AddListener(() => ui.SetPortraitFace(2));

        var rowFace2 = CreateUIObject("Row_Face2", portraitPanel);
        rowFace2.sizeDelta = new Vector2(390, 28);
        var bF3 = CreateButton("Btn_F3", rowFace2, "BATTLE GRIT (SWEAT)", new Color(0.35f, 0.18f, 0.18f), new Color(1f, 0.6f, 0.6f));
        bF3.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        bF3.GetComponent<RectTransform>().anchorMax = new Vector2(0.48f, 1);
        bF3.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bF3.onClick.AddListener(() => ui.SetPortraitFace(3));

        var bF4 = CreateButton("Btn_F4", rowFace2, "CONFIDENT SMIRK", new Color(0.20f, 0.30f, 0.22f), new Color(0.6f, 1f, 0.7f));
        bF4.GetComponent<RectTransform>().anchorMin = new Vector2(0.52f, 0);
        bF4.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        bF4.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bF4.onClick.AddListener(() => ui.SetPortraitFace(4));

        // Eye Color Section
        var pSecEyes = CreateTMP("Sec_Eyes", portraitPanel, "CUSTOMIZABLE: EYE COLOR", 11f, new Color(0.70f, 0.80f, 0.95f));
        pSecEyes.rectTransform.sizeDelta = new Vector2(390, 16);

        var rowEyes = CreateUIObject("Row_Eyes", portraitPanel);
        rowEyes.sizeDelta = new Vector2(390, 28);
        var bE0 = CreateButton("Btn_E0", rowEyes, "BLACK/GREY", new Color(0.18f, 0.18f, 0.20f), Color.white);
        bE0.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        bE0.GetComponent<RectTransform>().anchorMax = new Vector2(0.22f, 1);
        bE0.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bE0.onClick.AddListener(() => ui.SetPortraitEyeColor(0));

        var bE1 = CreateButton("Btn_E1", rowEyes, "CRIMSON", new Color(0.42f, 0.10f, 0.15f), new Color(1f, 0.4f, 0.4f));
        bE1.GetComponent<RectTransform>().anchorMin = new Vector2(0.25f, 0);
        bE1.GetComponent<RectTransform>().anchorMax = new Vector2(0.48f, 1);
        bE1.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bE1.onClick.AddListener(() => ui.SetPortraitEyeColor(1));

        var bE2 = CreateButton("Btn_E2", rowEyes, "AZURE", new Color(0.10f, 0.28f, 0.45f), new Color(0.4f, 0.85f, 1f));
        bE2.GetComponent<RectTransform>().anchorMin = new Vector2(0.51f, 0);
        bE2.GetComponent<RectTransform>().anchorMax = new Vector2(0.74f, 1);
        bE2.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bE2.onClick.AddListener(() => ui.SetPortraitEyeColor(2));

        var bE3 = CreateButton("Btn_E3", rowEyes, "GOLD", new Color(0.40f, 0.32f, 0.10f), new Color(1f, 0.88f, 0.35f));
        bE3.GetComponent<RectTransform>().anchorMin = new Vector2(0.77f, 0);
        bE3.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        bE3.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bE3.onClick.AddListener(() => ui.SetPortraitEyeColor(3));

        // Clothing Section
        var pSecCloth = CreateTMP("Sec_Cloth", portraitPanel, "CUSTOMIZABLE: CLOTHING", 11f, new Color(0.70f, 0.80f, 0.95f));
        pSecCloth.rectTransform.sizeDelta = new Vector2(390, 16);

        var rowCloth = CreateUIObject("Row_Cloth", portraitPanel);
        rowCloth.sizeDelta = new Vector2(390, 28);
        var bC0 = CreateButton("Btn_C0", rowCloth, "BARE", new Color(0.12f, 0.16f, 0.24f), Color.white);
        bC0.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        bC0.GetComponent<RectTransform>().anchorMax = new Vector2(0.22f, 1);
        bC0.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bC0.onClick.AddListener(() => ui.SetPortraitClothing(0));

        var bC1 = CreateButton("Btn_C1", rowCloth, "CHOKER", new Color(0.22f, 0.18f, 0.25f), Color.white);
        bC1.GetComponent<RectTransform>().anchorMin = new Vector2(0.25f, 0);
        bC1.GetComponent<RectTransform>().anchorMax = new Vector2(0.48f, 1);
        bC1.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bC1.onClick.AddListener(() => ui.SetPortraitClothing(1));

        var bC2 = CreateButton("Btn_C2", rowCloth, "SAILOR FUKU", new Color(0.15f, 0.25f, 0.38f), Color.white);
        bC2.GetComponent<RectTransform>().anchorMin = new Vector2(0.51f, 0);
        bC2.GetComponent<RectTransform>().anchorMax = new Vector2(0.74f, 1);
        bC2.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bC2.onClick.AddListener(() => ui.SetPortraitClothing(2));

        var bC3 = CreateButton("Btn_C3", rowCloth, "BLAZER", new Color(0.16f, 0.20f, 0.28f), Color.white);
        bC3.GetComponent<RectTransform>().anchorMin = new Vector2(0.77f, 0);
        bC3.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        bC3.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bC3.onClick.AddListener(() => ui.SetPortraitClothing(3));

        // Accessories Section
        var pSecAcc = CreateTMP("Sec_Acc", portraitPanel, "CUSTOMIZABLE: ACCESSORIES", 11f, new Color(0.70f, 0.80f, 0.95f));
        pSecAcc.rectTransform.sizeDelta = new Vector2(390, 16);

        var rowAcc = CreateUIObject("Row_Acc", portraitPanel);
        rowAcc.sizeDelta = new Vector2(390, 28);
        var bA0 = CreateButton("Btn_A0", rowAcc, "NONE", new Color(0.12f, 0.16f, 0.24f), Color.white);
        bA0.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        bA0.GetComponent<RectTransform>().anchorMax = new Vector2(0.22f, 1);
        bA0.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bA0.onClick.AddListener(() => ui.SetPortraitAccessory(0));

        var bA1 = CreateButton("Btn_A1", rowAcc, "HEADBAND", new Color(0.20f, 0.24f, 0.32f), Color.white);
        bA1.GetComponent<RectTransform>().anchorMin = new Vector2(0.25f, 0);
        bA1.GetComponent<RectTransform>().anchorMax = new Vector2(0.48f, 1);
        bA1.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bA1.onClick.AddListener(() => ui.SetPortraitAccessory(1));

        var bA2 = CreateButton("Btn_A2", rowAcc, "GLASSES", new Color(0.20f, 0.24f, 0.32f), Color.white);
        bA2.GetComponent<RectTransform>().anchorMin = new Vector2(0.51f, 0);
        bA2.GetComponent<RectTransform>().anchorMax = new Vector2(0.74f, 1);
        bA2.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bA2.onClick.AddListener(() => ui.SetPortraitAccessory(2));

        var bA3 = CreateButton("Btn_A3", rowAcc, "EARRINGS", new Color(0.20f, 0.24f, 0.32f), Color.white);
        bA3.GetComponent<RectTransform>().anchorMin = new Vector2(0.77f, 0);
        bA3.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        bA3.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bA3.onClick.AddListener(() => ui.SetPortraitAccessory(3));

        // Vitals Test Section
        var pSecVit = CreateTMP("Sec_Vit", portraitPanel, "TEST BATTLE VITALS (HP & MP BARS)", 11f, new Color(0.70f, 0.80f, 0.95f));
        pSecVit.rectTransform.sizeDelta = new Vector2(390, 16);

        var (hpSlider, hpValTxt) = CreateSliderRow("Row_TestHP", portraitPanel, "HP Gauge", 0f, 100f, 100f);
        hpSlider.onValueChanged.AddListener((val) => ui.SetPortraitHp(val));

        var (mpSlider, mpValTxt) = CreateSliderRow("Row_TestMP", portraitPanel, "MP Gauge", 0f, 50f, 50f);
        mpSlider.onValueChanged.AddListener((val) => ui.SetPortraitMp(val));

        var rowHit = CreateUIObject("Row_Hit", portraitPanel);
        rowHit.sizeDelta = new Vector2(390, 30);
        var btnHit = CreateButton("Btn_Hit", rowHit, "TEST DAMAGE FLASH", new Color(0.45f, 0.15f, 0.18f), new Color(1f, 0.5f, 0.5f));
        btnHit.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnHit.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnHit.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnHit.onClick.AddListener(() => ui.TriggerPortraitDamageFlash());

        ui.portraitTabPanel = portraitPanel.gameObject;
        portraitPanel.gameObject.SetActive(false);

        // --- TAB 4: 12D TELEMETRY PANEL ---
        var telemetryPanel = CreateUIObject("Tab_Telemetry_Panel", contentRt);
        telemetryPanel.anchorMin = Vector2.zero;
        telemetryPanel.anchorMax = Vector2.one;
        telemetryPanel.sizeDelta = Vector2.zero;

        var telemCard = CreateUIObject("Telem_Card", telemetryPanel);
        telemCard.anchorMin = Vector2.zero;
        telemCard.anchorMax = Vector2.one;
        telemCard.sizeDelta = Vector2.zero;
        var telemCardImg = telemCard.gameObject.AddComponent<Image>();
        telemCardImg.color = new Color(0.03f, 0.05f, 0.10f, 0.96f);

        var telemTxt = CreateTMP("Telemetry_Text", telemCard, "Calculating 12D Telemetry...", 12f, Color.white, TextAlignmentOptions.TopLeft);
        telemTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
        telemTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
        telemTxt.rectTransform.sizeDelta = Vector2.zero;

        ui.telemetryTabPanel = telemetryPanel.gameObject;
        ui.telemetryDisplayText = telemTxt;
        telemetryPanel.gameObject.SetActive(false);

        // --- TAB 4: ENVIRONMENT & SKYBOX PANEL ---
        var envPanel = CreateUIObject("Tab_Environment_Panel", contentRt);
        envPanel.anchorMin = Vector2.zero;
        envPanel.anchorMax = Vector2.one;
        envPanel.sizeDelta = Vector2.zero;
        var envLayout = envPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        envLayout.childControlHeight = false;
        envLayout.childControlWidth = true;
        envLayout.spacing = 5f;
        envLayout.padding = new RectOffset(0, 0, 6, 0);

        // Section Title: Time of Day
        var envTitle1 = CreateTMP("Title_Time", envPanel, "TIME OF DAY & LIGHTING", 12.5f, new Color(0.38f, 0.85f, 0.98f));
        envTitle1.rectTransform.sizeDelta = new Vector2(390, 18);

        // 24h Time Slider
        var (timeS, timeT) = CreateSliderRow("Row_Time", envPanel, "Time (24h Clock)", 0f, 24f, 12f);
        timeT.text = "12:00 PM";

        // Quick Preset Buttons Row 1: Dawn, Noon, Sunset
        var rowPre1 = CreateUIObject("Row_Presets_1", envPanel);
        rowPre1.sizeDelta = new Vector2(390, 30);
        var btnDawn = CreateButton("Btn_Dawn", rowPre1, "DAWN (06:30)", new Color(0.28f, 0.20f, 0.18f), new Color(1f, 0.8f, 0.65f));
        btnDawn.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnDawn.GetComponent<RectTransform>().anchorMax = new Vector2(0.31f, 1);
        btnDawn.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnDawn.onClick.AddListener(() => ui.ApplyTimePreset(0));

        var btnNoon = CreateButton("Btn_Noon", rowPre1, "NOON (12:00)", new Color(0.18f, 0.28f, 0.38f), Color.white);
        btnNoon.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0);
        btnNoon.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 1);
        btnNoon.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnNoon.onClick.AddListener(() => ui.ApplyTimePreset(1));

        var btnSunset = CreateButton("Btn_Sunset", rowPre1, "SUNSET (18:30)", new Color(0.35f, 0.18f, 0.12f), new Color(1f, 0.6f, 0.35f));
        btnSunset.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0);
        btnSunset.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnSunset.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnSunset.onClick.AddListener(() => ui.ApplyTimePreset(2));

        // Quick Preset Buttons Row 2: Twilight, Night, Eclipse
        var rowPre2 = CreateUIObject("Row_Presets_2", envPanel);
        rowPre2.sizeDelta = new Vector2(390, 30);
        var btnTwilight = CreateButton("Btn_Twilight", rowPre2, "DUSK (20:30)", new Color(0.20f, 0.15f, 0.32f), new Color(0.75f, 0.65f, 1.0f));
        btnTwilight.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnTwilight.GetComponent<RectTransform>().anchorMax = new Vector2(0.31f, 1);
        btnTwilight.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnTwilight.onClick.AddListener(() => ui.ApplyTimePreset(3));

        var btnNight = CreateButton("Btn_Night", rowPre2, "NIGHT (00:00)", new Color(0.10f, 0.14f, 0.28f), new Color(0.65f, 0.85f, 1.0f));
        btnNight.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0);
        btnNight.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 1);
        btnNight.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnNight.onClick.AddListener(() => ui.ApplyTimePreset(4));

        var btnEclipse = CreateButton("Btn_Eclipse", rowPre2, "BLOOD ECLIPSE", new Color(0.30f, 0.08f, 0.15f), new Color(1f, 0.35f, 0.55f));
        btnEclipse.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0);
        btnEclipse.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnEclipse.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnEclipse.onClick.AddListener(() => ui.ApplyTimePreset(5));

        // Section Title: Skybox Theme Selector
        var envTitle2 = CreateTMP("Title_Skybox", envPanel, "SKYBOX THEMES", 12.5f, new Color(0.38f, 0.85f, 0.98f));
        envTitle2.rectTransform.sizeDelta = new Vector2(390, 18);

        // Skybox Theme Row 1
        var rowSky1 = CreateUIObject("Row_Skybox_1", envPanel);
        rowSky1.sizeDelta = new Vector2(390, 28);
        var bSkyAtmo = CreateButton("Btn_Sky_Atmo", rowSky1, "ATMOSPHERE", new Color(0.14f, 0.20f, 0.30f), Color.white);
        bSkyAtmo.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        bSkyAtmo.GetComponent<RectTransform>().anchorMax = new Vector2(0.31f, 1);
        bSkyAtmo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bSkyAtmo.onClick.AddListener(() => ui.SetSkyboxTheme(0));

        var bSkyAnime = CreateButton("Btn_Sky_Anime", rowSky1, "ANIME SKY", new Color(0.18f, 0.28f, 0.40f), new Color(0.4f, 0.9f, 1f));
        bSkyAnime.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0);
        bSkyAnime.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 1);
        bSkyAnime.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bSkyAnime.onClick.AddListener(() => ui.SetSkyboxTheme(1));

        var bSkyCosmos = CreateButton("Btn_Sky_Cosmos", rowSky1, "COSMOS", new Color(0.14f, 0.12f, 0.28f), Color.white);
        bSkyCosmos.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0);
        bSkyCosmos.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        bSkyCosmos.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bSkyCosmos.onClick.AddListener(() => ui.SetSkyboxTheme(2));

        // Skybox Theme Row 2
        var rowSky2 = CreateUIObject("Row_Skybox_2", envPanel);
        rowSky2.sizeDelta = new Vector2(390, 28);
        var bSkySunset = CreateButton("Btn_Sky_Sunset", rowSky2, "GOLDEN SUNSET", new Color(0.28f, 0.18f, 0.12f), Color.white);
        bSkySunset.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        bSkySunset.GetComponent<RectTransform>().anchorMax = new Vector2(0.31f, 1);
        bSkySunset.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bSkySunset.onClick.AddListener(() => ui.SetSkyboxTheme(3));

        var bSkyAurora = CreateButton("Btn_Sky_Aurora", rowSky2, "AURORA", new Color(0.10f, 0.25f, 0.25f), new Color(0.4f, 1f, 0.85f));
        bSkyAurora.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0);
        bSkyAurora.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 1);
        bSkyAurora.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bSkyAurora.onClick.AddListener(() => ui.SetSkyboxTheme(4));

        var bSkyStudio = CreateButton("Btn_Sky_Studio", rowSky2, "STUDIO CYC", new Color(0.16f, 0.18f, 0.24f), Color.white);
        bSkyStudio.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0);
        bSkyStudio.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        bSkyStudio.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bSkyStudio.onClick.AddListener(() => ui.SetSkyboxTheme(6));

        // Section Title: Celestial Sun Controls
        var envTitle3 = CreateTMP("Title_Sun", envPanel, "CELESTIAL & AMBIENCE FINE TUNING", 12.5f, new Color(0.38f, 0.85f, 0.98f));
        envTitle3.rectTransform.sizeDelta = new Vector2(390, 18);

        var (azS, azT) = CreateSliderRow("Row_SunAzimuth", envPanel, "Sun Azimuth (Yaw)", 0f, 360f, 160f);
        azT.text = "160°";
        var (elS, elT) = CreateSliderRow("Row_SunElevation", envPanel, "Sun Elevation (Pitch)", -10f, 90f, 45f);
        elT.text = "45°";
        var (inS, inT) = CreateSliderRow("Row_SunIntensity", envPanel, "Sun Intensity", 0f, 2.5f, 1.0f);
        var (fgS, fgT) = CreateSliderRow("Row_FogDensity", envPanel, "Atmospheric Fog", 0f, 0.04f, 0.012f);
        fgT.text = "12.0‰";
        var (glS, glT) = CreateSliderRow("Row_NightGlow", envPanel, "Night Glow Radiance", 0f, 3.0f, 1.5f);
        glT.text = "1.5x";

        // Row Actions: Timelapse Auto-Cycle, Lanterns Toggle, Runic Glow Toggle
        var rowEnvAct = CreateUIObject("Row_Env_Actions", envPanel);
        rowEnvAct.sizeDelta = new Vector2(390, 32);
        var btnTimelapse = CreateButton("Btn_Timelapse", rowEnvAct, "AUTO CYCLE", new Color(0.18f, 0.42f, 0.30f), Color.white);
        btnTimelapse.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        btnTimelapse.GetComponent<RectTransform>().anchorMax = new Vector2(0.31f, 1);
        btnTimelapse.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnTimelapse.onClick.AddListener(() => ui.OnToggleTimelapse());

        var btnLanterns = CreateButton("Btn_Lanterns", rowEnvAct, "LANTERNS: ON", new Color(0.35f, 0.22f, 0.12f), new Color(1f, 0.75f, 0.4f));
        btnLanterns.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0);
        btnLanterns.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 1);
        btnLanterns.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnLanterns.onClick.AddListener(() => ui.OnToggleLanterns());

        var btnRunic = CreateButton("Btn_RunicGlow", rowEnvAct, "RUNIC: ON", new Color(0.12f, 0.28f, 0.40f), new Color(0.40f, 0.90f, 1.0f));
        btnRunic.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0);
        btnRunic.GetComponent<RectTransform>().anchorMax = new Vector2(1.0f, 1);
        btnRunic.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        btnRunic.onClick.AddListener(() => ui.OnToggleRunicGlow());

        ui.environmentTabPanel = envPanel.gameObject;
        ui.timeOfDaySlider = timeS; ui.timeOfDayValueText = timeT;
        ui.sunAzimuthSlider = azS; ui.sunAzimuthValueText = azT;
        ui.sunElevationSlider = elS; ui.sunElevationValueText = elT;
        ui.sunIntensitySlider = inS; ui.sunIntensityValueText = inT;
        ui.fogDensitySlider = fgS; ui.fogDensityValueText = fgT;
        ui.nightGlowSlider = glS; ui.nightGlowValueText = glT;
        ui.timelapseButton = btnTimelapse;
        ui.timelapseButtonText = btnTimelapse.GetComponentInChildren<TextMeshProUGUI>();
        ui.lanternsToggleButton = btnLanterns;
        ui.lanternsToggleText = btnLanterns.GetComponentInChildren<TextMeshProUGUI>();
        ui.runicGlowToggleButton = btnRunic;
        ui.runicGlowToggleText = btnRunic.GetComponentInChildren<TextMeshProUGUI>();
        envPanel.gameObject.SetActive(false);

        // --- Top-Right Time-Of-Day Quick Bar ---
        var timeBarRt = CreateUIObject("Studio_TimeBar", canvasGo.transform);
        timeBarRt.anchorMin = new Vector2(1, 1);
        timeBarRt.anchorMax = new Vector2(1, 1);
        timeBarRt.pivot = new Vector2(1, 1);
        timeBarRt.anchoredPosition = new Vector2(-25, -75);
        timeBarRt.sizeDelta = new Vector2(480, 36);
        var timeBarImg = timeBarRt.gameObject.AddComponent<Image>();
        timeBarImg.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);

        var tLayout = timeBarRt.gameObject.AddComponent<HorizontalLayoutGroup>();
        tLayout.childControlHeight = true;
        tLayout.childControlWidth = true;
        tLayout.spacing = 5f;
        tLayout.padding = new RectOffset(5, 5, 5, 5);

        var qDawn = CreateButton("QBtn_Dawn", timeBarRt, "DAWN", new Color(0.24f, 0.16f, 0.14f), new Color(1f, 0.8f, 0.65f));
        qDawn.onClick.AddListener(() => ui.ApplyTimePreset(0));

        var qNoon = CreateButton("QBtn_Noon", timeBarRt, "NOON", new Color(0.14f, 0.22f, 0.32f), Color.white);
        qNoon.onClick.AddListener(() => ui.ApplyTimePreset(1));

        var qSunset = CreateButton("QBtn_Sunset", timeBarRt, "SUNSET", new Color(0.30f, 0.16f, 0.10f), new Color(1f, 0.6f, 0.35f));
        qSunset.onClick.AddListener(() => ui.ApplyTimePreset(2));

        var qNight = CreateButton("QBtn_Night", timeBarRt, "NIGHT", new Color(0.08f, 0.12f, 0.24f), new Color(0.65f, 0.85f, 1.0f));
        qNight.onClick.AddListener(() => ui.ApplyTimePreset(4));

        var qEclipse = CreateButton("QBtn_Eclipse", timeBarRt, "ECLIPSE", new Color(0.26f, 0.06f, 0.12f), new Color(1f, 0.35f, 0.55f));
        qEclipse.onClick.AddListener(() => ui.ApplyTimePreset(5));

        // --- Top-Right Camera View Bar ---
        var camBarRt = CreateUIObject("Studio_CamBar", canvasGo.transform);
        camBarRt.anchorMin = new Vector2(1, 1);
        camBarRt.anchorMax = new Vector2(1, 1);
        camBarRt.pivot = new Vector2(1, 1);
        camBarRt.anchoredPosition = new Vector2(-25, -25);
        camBarRt.sizeDelta = new Vector2(480, 44);
        var camBarImg = camBarRt.gameObject.AddComponent<Image>();
        camBarImg.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);

        var cLayout = camBarRt.gameObject.AddComponent<HorizontalLayoutGroup>();
        cLayout.childControlHeight = true;
        cLayout.childControlWidth = true;
        cLayout.spacing = 6f;
        cLayout.padding = new RectOffset(6, 6, 6, 6);

        var bBody = CreateButton("Btn_Cam_Body", camBarRt, "FULL BODY", new Color(0.15f, 0.22f, 0.32f), Color.white);
        bBody.onClick.AddListener(() => ui.OnFocusFullBody());

        var bFace = CreateButton("Btn_Cam_Face", camBarRt, "PORTRAIT", new Color(0.15f, 0.22f, 0.32f), Color.white);
        bFace.onClick.AddListener(() => ui.OnFocusPortrait());

        var bHands = CreateButton("Btn_Cam_Hands", camBarRt, "10 FINGERS", new Color(0.15f, 0.22f, 0.32f), Color.white);
        bHands.onClick.AddListener(() => ui.OnFocusHands());

        var bFeet = CreateButton("Btn_Cam_Feet", camBarRt, "10 TOES", new Color(0.15f, 0.22f, 0.32f), Color.white);
        bFeet.onClick.AddListener(() => ui.OnFocusFeet());

        var bSpin = CreateButton("Btn_Cam_Spin", camBarRt, "AUTO ORBIT", new Color(0.20f, 0.35f, 0.50f), new Color(0.40f, 0.90f, 1f));
        bSpin.onClick.AddListener(() => ui.OnToggleTurntable());

        // --- Persona Battle Side-Profile Portrait Widget ---
        var portraitWidgetRt = CreateUIObject("BattleSideProfilePortrait", canvasGo.transform);
        portraitWidgetRt.anchorMin = new Vector2(1f, 1f);
        portraitWidgetRt.anchorMax = new Vector2(1f, 1f);
        portraitWidgetRt.pivot = new Vector2(1f, 1f);
        portraitWidgetRt.anchoredPosition = new Vector2(-25f, -125f);
        portraitWidgetRt.sizeDelta = new Vector2(470f, 318f);

        var portraitComp = portraitWidgetRt.gameObject.AddComponent<BattleSummoner.UI.BattleSideProfilePortrait>();

        // 1. Template Direct Image (pixel-perfect 1:1 Persona reference)
        var directImgRt = CreateUIObject("TemplateDirectImage", portraitWidgetRt);
        directImgRt.anchorMin = Vector2.zero;
        directImgRt.anchorMax = Vector2.one;
        directImgRt.sizeDelta = Vector2.zero;
        var directImg = directImgRt.gameObject.AddComponent<Image>();
        directImg.preserveAspect = true;
        var mascTemplateSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/BattleSideProfile/Template_BattlePortrait_Masc.png");
        if (mascTemplateSpr != null) directImg.sprite = mascTemplateSpr;

        // 2. Modular Layers Root
        var modularRootRt = CreateUIObject("ModularLayersRoot", portraitWidgetRt);
        modularRootRt.anchorMin = Vector2.zero;
        modularRootRt.anchorMax = Vector2.one;
        modularRootRt.sizeDelta = Vector2.zero;

        // Frame Backing
        var frameBackRt = CreateUIObject("FrameBacking", modularRootRt);
        frameBackRt.anchorMin = Vector2.zero;
        frameBackRt.anchorMax = Vector2.one;
        frameBackRt.sizeDelta = Vector2.zero;
        var frameBackImg = frameBackRt.gameObject.AddComponent<Image>();
        frameBackImg.preserveAspect = true;
        var frameMascSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/BattleSideProfile/Frame_Badge_Masc.png");
        if (frameMascSpr != null) frameBackImg.sprite = frameMascSpr;

        // Base Head
        var baseHeadRt = CreateUIObject("BaseHead", modularRootRt);
        baseHeadRt.anchorMin = new Vector2(0.09f, 0.40f);
        baseHeadRt.anchorMax = new Vector2(0.48f, 0.95f);
        baseHeadRt.sizeDelta = Vector2.zero;
        var baseHeadImg = baseHeadRt.gameObject.AddComponent<Image>();
        baseHeadImg.color = Color.clear;
        baseHeadRt.gameObject.SetActive(false);

        // Eye Layer
        var eyeRt = CreateUIObject("EyeLayer", modularRootRt);
        var eyeImg = eyeRt.gameObject.AddComponent<Image>();
        eyeImg.preserveAspect = true;

        // Face / Nose Layer
        var faceRt = CreateUIObject("FaceLayer", modularRootRt);
        var faceImg = faceRt.gameObject.AddComponent<Image>();
        faceImg.preserveAspect = true;

        // Clothing Layer
        var clothRt = CreateUIObject("ClothingLayer", modularRootRt);
        var clothImg = clothRt.gameObject.AddComponent<Image>();
        clothImg.preserveAspect = true;

        // Hair Layer
        var hairRt = CreateUIObject("HairLayer", modularRootRt);
        var hairImg = hairRt.gameObject.AddComponent<Image>();
        hairImg.preserveAspect = true;

        // Accessory Layer
        var accRt = CreateUIObject("AccessoryLayer", modularRootRt);
        var accImg = accRt.gameObject.AddComponent<Image>();
        accImg.preserveAspect = true;

        modularRootRt.gameObject.SetActive(false);

        // 3. Vitals Gauges (Skewed Dual HP & MP bars with dynamic text)
        var vitalsRt = CreateUIObject("VitalsContainer", portraitWidgetRt);
        vitalsRt.anchorMin = Vector2.zero;
        vitalsRt.anchorMax = Vector2.one;
        vitalsRt.sizeDelta = Vector2.zero;

        var dualGaugeBackRt = CreateUIObject("DualGaugeBacking", vitalsRt);
        dualGaugeBackRt.anchorMin = Vector2.zero;
        dualGaugeBackRt.anchorMax = Vector2.one;
        dualGaugeBackRt.sizeDelta = Vector2.zero;
        var dualGaugeImg = dualGaugeBackRt.gameObject.AddComponent<Image>();
        dualGaugeImg.preserveAspect = true;
        dualGaugeBackRt.gameObject.SetActive(false); // Clean template provides 1:1 gauge artwork

        // HP and MP text overlays (aligned with Persona slanted gauge slots)
        var hpTextTMP = CreateTMP("HP_Text", vitalsRt, "<color=#00e5ff><i><b>HP</b></i></color>  100/100", 15f, Color.white, TextAlignmentOptions.Right);
        hpTextTMP.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        hpTextTMP.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        hpTextTMP.rectTransform.pivot = new Vector2(1f, 0.5f);
        hpTextTMP.rectTransform.anchoredPosition = new Vector2(215f, -76f);
        hpTextTMP.rectTransform.sizeDelta = new Vector2(160f, 30f);
        hpTextTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;

        var mpTextTMP = CreateTMP("MP_Text", vitalsRt, "<color=#76ff03><i><b>MP</b></i></color>  50/50", 15f, Color.white, TextAlignmentOptions.Right);
        mpTextTMP.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        mpTextTMP.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        mpTextTMP.rectTransform.pivot = new Vector2(1f, 0.5f);
        mpTextTMP.rectTransform.anchoredPosition = new Vector2(215f, -118f);
        mpTextTMP.rectTransform.sizeDelta = new Vector2(160f, 30f);
        mpTextTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;

        portraitComp.BindHierarchyReferences(
            directImg,
            modularRootRt.gameObject,
            frameBackImg,
            baseHeadImg,
            hairImg,
            faceImg,
            eyeImg,
            clothImg,
            accImg,
            null,
            null,
            hpTextTMP,
            mpTextTMP,
            null,
            vitalsRt
        );

        ui.sideProfilePortrait = portraitComp;

        // Save prefab
        var prefabFolder = "Assets/Prefabs/UI";
        if (!System.IO.Directory.Exists(prefabFolder)) System.IO.Directory.CreateDirectory(prefabFolder);
        PrefabUtility.SaveAsPrefabAssetAndConnect(portraitWidgetRt.gameObject, $"{prefabFolder}/BattleSideProfilePortrait.prefab", UnityEditor.InteractionMode.AutomatedAction);

        // Assign prefabs
        ui.helmetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Helmet.prefab");
        ui.torsoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Torso.prefab");
        ui.leggingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Leggings.prefab");
        ui.shoesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Shoes.prefab");
        ui.swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Sword.prefab");
        ui.battleAxePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_TwoHanded_BattleAxe.prefab");
        ui.staffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Staff.prefab");
        ui.wandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Wand.prefab");

        ui.InitializeSliders();
        ui.SetViewMode(2);

        EditorUtility.SetDirty(canvasGo);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        EditorSceneManager.SaveScene(canvasGo.scene);

        return "Successfully built, wired, and saved complete Character Studio UI!";
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using BattleSummoner.DungeonDimension;
using BattleSummoner.World;

public static class DungeonDimensionHierarchyBuilder
{
    private const string TEXTURE_DIR = "Assets/Textures/UI/";

    [MenuItem("Tools/BattleSummoner/Build Dungeon Dimension Systems & UI")]
    public static void BuildAll()
    {
        SetupDungeonFloorScene();
        SetupOverworldScene();
        Debug.Log("🌌 [DUNGEON DIMENSION] Successfully built all Dungeon Dimension and Overworld Portal systems!");
    }

    public static void SetupDungeonFloorScene()
    {
        string scenePath = "Assets/Scenes/MainDungeonFloor.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Ensure Manager GameObjects exist
        var mgrGo = GameObject.Find("DungeonDimensionSystems");
        if (mgrGo == null)
        {
            mgrGo = new GameObject("DungeonDimensionSystems");
        }

        var clock = mgrGo.GetComponent<RiskOfRainDifficultyClock>();
        if (clock == null) clock = mgrGo.AddComponent<RiskOfRainDifficultyClock>();

        var buffSys = mgrGo.GetComponent<DungeonRunBuffSystem>();
        if (buffSys == null) buffSys = mgrGo.AddComponent<DungeonRunBuffSystem>();

        var dimMgr = mgrGo.GetComponent<DungeonDimensionManager>();
        if (dimMgr == null) dimMgr = mgrGo.AddComponent<DungeonDimensionManager>();

        // 2. Find Canvas & Build HUD + Reward Modal
        var canvasGo = GameObject.Find("PortraitHUDCanvas");
        if (canvasGo != null)
        {
            BuildDungeonDimensionHUD(canvasGo.transform);
            BuildDungeonBoonRewardUI(canvasGo.transform);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("💾 [DUNGEON DIMENSION] MainDungeonFloor configured and saved.");
    }

    public static void SetupOverworldScene()
    {
        string scenePath = "Assets/Scenes/OverworldMap.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Add OtherworldlyRiftSpawner
        var spawnerGo = GameObject.Find("OtherworldlyRiftSpawner");
        if (spawnerGo == null)
        {
            spawnerGo = new GameObject("OtherworldlyRiftSpawner");
            spawnerGo.AddComponent<OtherworldlyRiftSpawner>();
        }

        // 2. Add an active portal forward-base at the Mistwood/Spire edge
        var existingOutpost = GameObject.Find("Otherworldly_Portal_Outpost");
        if (existingOutpost == null)
        {
            Vector3 outpostPos = new Vector3(-8f, 0f, 24f); // Guarding approach to the Spire
            BuildOutpostInOverworld(null, outpostPos);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("💾 [OVERWORLD] OverworldMap portal outpost configured and saved.");
    }

    public static GameObject BuildOutpostInOverworld(Transform parent, Vector3 position)
    {
        GameObject outpostRoot = new GameObject("Otherworldly_Portal_Outpost", typeof(OtherworldlyPortalOutpost));
        if (parent != null) outpostRoot.transform.SetParent(parent, false);
        outpostRoot.transform.position = position;

        // 1. Portal Gate Archway
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = "Arch_Top";
        gate.transform.SetParent(outpostRoot.transform, false);
        gate.transform.localPosition = new Vector3(0f, 4.5f, 0f);
        gate.transform.localScale = new Vector3(5.5f, 0.8f, 1.2f);
        gate.GetComponent<Renderer>().material.color = new Color(0.12f, 0.08f, 0.20f);

        GameObject pillarL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillarL.name = "Pillar_Left";
        pillarL.transform.SetParent(outpostRoot.transform, false);
        pillarL.transform.localPosition = new Vector3(-2.2f, 2.2f, 0f);
        pillarL.transform.localScale = new Vector3(0.9f, 4.5f, 0.9f);
        pillarL.GetComponent<Renderer>().material.color = new Color(0.12f, 0.08f, 0.20f);

        GameObject pillarR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillarR.name = "Pillar_Right";
        pillarR.transform.SetParent(outpostRoot.transform, false);
        pillarR.transform.localPosition = new Vector3(2.2f, 2.2f, 0f);
        pillarR.transform.localScale = new Vector3(0.9f, 4.5f, 0.9f);
        pillarR.GetComponent<Renderer>().material.color = new Color(0.12f, 0.08f, 0.20f);

        // 2. Swirling Vortex Quad
        GameObject vortex = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vortex.name = "PortalVortexDisc";
        vortex.transform.SetParent(outpostRoot.transform, false);
        vortex.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        vortex.transform.localScale = new Vector3(3.6f, 4.2f, 1f);
        vortex.GetComponent<Renderer>().material.color = new Color(0.70f, 0.15f, 0.95f);

        // 3. Portal Light
        GameObject lightObj = new GameObject("PortalAuraLight", typeof(Light));
        lightObj.transform.SetParent(outpostRoot.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0.5f);
        Light l = lightObj.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.85f, 0.20f, 1.0f);
        l.range = 16f;
        l.intensity = 3.2f;

        // 4. Portal Trigger Collider
        BoxCollider col = outpostRoot.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 2.0f, 0f);
        col.size = new Vector3(3.8f, 4.0f, 2.5f);
        col.isTrigger = true;

        // 5. Fortified Barricades (Spikes & Palisade)
        Vector3[] barricadeOffsets = {
            new Vector3(-4.0f, 0.6f, 3.0f),
            new Vector3(4.0f, 0.6f, 3.0f),
            new Vector3(-5.5f, 0.6f, -1.0f),
            new Vector3(5.5f, 0.6f, -1.0f)
        };
        for (int i = 0; i < barricadeOffsets.Length; i++)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = $"Barricade_{i + 1}";
            bar.transform.SetParent(outpostRoot.transform, false);
            bar.transform.localPosition = barricadeOffsets[i];
            bar.transform.localScale = new Vector3(2.5f, 1.2f, 0.6f);
            bar.GetComponent<Renderer>().material.color = new Color(0.28f, 0.18f, 0.12f);
        }

        // 6. Void Warding Pylons with glowing crystals
        Vector3[] pylonOffsets = {
            new Vector3(-3.5f, 0f, -4.0f),
            new Vector3(3.5f, 0f, -4.0f)
        };
        for (int p = 0; p < pylonOffsets.Length; p++)
        {
            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pylon.name = $"VoidPylon_{p + 1}";
            pylon.transform.SetParent(outpostRoot.transform, false);
            pylon.transform.localPosition = pylonOffsets[p] + new Vector3(0f, 1.5f, 0f);
            pylon.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
            pylon.GetComponent<Renderer>().material.color = new Color(0.15f, 0.12f, 0.25f);

            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crystal.name = "PylonCrystal";
            crystal.transform.SetParent(pylon.transform, false);
            crystal.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            crystal.transform.localScale = new Vector3(1.2f, 1.6f, 1.2f);
            crystal.GetComponent<Renderer>().material.color = new Color(0.90f, 0.20f, 0.95f);
        }

        // 7. In-World Floating Prompt Canvas
        GameObject promptGo = new GameObject("InWorldPromptCanvas", typeof(RectTransform), typeof(Canvas), typeof(TextMeshProUGUI));
        promptGo.transform.SetParent(outpostRoot.transform, false);
        promptGo.transform.localPosition = new Vector3(0f, 3.6f, 0f);
        Canvas pc = promptGo.GetComponent<Canvas>();
        pc.renderMode = RenderMode.WorldSpace;
        RectTransform prt = promptGo.GetComponent<RectTransform>();
        prt.sizeDelta = new Vector2(320f, 70f);
        prt.localScale = Vector3.one * 0.01f;
        promptGo.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);
        TextMeshProUGUI ptmp = promptGo.GetComponent<TextMeshProUGUI>();
        ptmp.text = "<color=#FFEE44><b>[F] / TAP</b></color> // <color=#40EBFF>ENTER DUNGEON DIMENSION</color>\n<size=75%><color=#D0A0FF>Risk of Rain Scaler Active • Area Boons Await</color></size>";
        ptmp.alignment = TextAlignmentOptions.Center;
        ptmp.fontSize = 18;

        // Wire fields on OtherworldlyPortalOutpost
        var outpostComp = outpostRoot.GetComponent<OtherworldlyPortalOutpost>();
        SetField(outpostComp, "portalVortexTransform", vortex.transform);
        SetField(outpostComp, "portalAuraLight", l);
        SetField(outpostComp, "portalTriggerCollider", col);
        SetField(outpostComp, "promptCanvasObj", promptGo);
        SetField(outpostComp, "promptTMP", ptmp);

        return outpostRoot;
    }

    public static void BuildDungeonDimensionHUD(Transform canvasParent)
    {
        Transform existing = canvasParent.Find("DungeonDimensionHUD");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject hudRoot = CreateUIObject("DungeonDimensionHUD", canvasParent, Vector2.zero, Vector2.one);
        var hudComp = hudRoot.AddComponent<DungeonDimensionHUD>();
        SetField(hudComp, "hudRoot", hudRoot);

        // 1. Risk of Rain Difficulty Clock Header (Top Center: x: 0.15 to 0.85, y: 0.89 to 0.94)
        GameObject clockFrame = CreateUIObject("RoR_DifficultyClockFrame", hudRoot.transform, new Vector2(0.18f, 0.885f), new Vector2(0.82f, 0.94f));
        Image frameImg = clockFrame.AddComponent<Image>();
        Sprite fSp = GetSprite("Mockup_RoR_TimerFrame.png");
        if (fSp != null) frameImg.sprite = fSp;
        frameImg.color = Color.white;

        // Timer Text (e.g. "04:15")
        GameObject timerGo = CreateUIObject("TimerTMP", clockFrame.transform, new Vector2(0.04f, 0.15f), new Vector2(0.32f, 0.85f));
        TextMeshProUGUI timerTMP = timerGo.AddComponent<TextMeshProUGUI>();
        timerTMP.text = "00:00";
        timerTMP.fontSize = 16;
        timerTMP.alignment = TextAlignmentOptions.MidlineLeft;
        timerTMP.color = Color.white;
        SetField(hudComp, "timerTMP", timerTMP);

        // Tier Badge (e.g. "VERY EASY", "HARD", "INSANE")
        GameObject tierGo = CreateUIObject("TierBadgeTMP", clockFrame.transform, new Vector2(0.34f, 0.15f), new Vector2(0.72f, 0.85f));
        TextMeshProUGUI tierTMP = tierGo.AddComponent<TextMeshProUGUI>();
        tierTMP.text = "<color=#30FF66><b>VERY EASY</b></color>";
        tierTMP.fontSize = 13;
        tierTMP.alignment = TextAlignmentOptions.Center;
        SetField(hudComp, "tierBadgeTMP", tierTMP);

        // Coefficient Multiplier Text (e.g. "1.25x")
        GameObject coeffGo = CreateUIObject("CoeffTMP", clockFrame.transform, new Vector2(0.74f, 0.15f), new Vector2(0.96f, 0.85f));
        TextMeshProUGUI coeffTMP = coeffGo.AddComponent<TextMeshProUGUI>();
        coeffTMP.text = "<size=80%>SCALE</size> <b>1.00x</b>";
        coeffTMP.fontSize = 11;
        coeffTMP.alignment = TextAlignmentOptions.MidlineRight;
        coeffTMP.color = new Color(1f, 0.85f, 0.4f);
        SetField(hudComp, "coeffTMP", coeffTMP);

        // Tier Progress Slider (Underneath the clock frame)
        GameObject sliderGo = CreateUIObject("TierProgressSlider", hudRoot.transform, new Vector2(0.20f, 0.875f), new Vector2(0.80f, 0.883f));
        Slider slider = sliderGo.AddComponent<Slider>();

        GameObject sBg = CreateUIObject("Background", sliderGo.transform, Vector2.zero, Vector2.one);
        Image sBgImg = sBg.AddComponent<Image>();
        sBgImg.color = new Color(0.05f, 0.06f, 0.08f, 0.85f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform, Vector2.zero, Vector2.one);
        GameObject fill = CreateUIObject("Fill", fillArea.transform, Vector2.zero, Vector2.one);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.20f, 0.95f, 0.40f);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        SetField(hudComp, "tierProgressBar", slider);
        SetField(hudComp, "tierProgressFill", fillImg);

        // 2. Area & Objective Status Banner (Top Left: x: 0.04 to 0.50, y: 0.945 to 0.985)
        GameObject areaGo = CreateUIObject("AreaTitleTMP", hudRoot.transform, new Vector2(0.04f, 0.95f), new Vector2(0.60f, 0.985f));
        TextMeshProUGUI areaTMP = areaGo.AddComponent<TextMeshProUGUI>();
        areaTMP.text = "<color=#40EBFF><b>AREA 1:</b></color> <color=#FFD700>THRESHOLD OF THE VOID</color>";
        areaTMP.fontSize = 13;
        areaTMP.alignment = TextAlignmentOptions.MidlineLeft;
        SetField(hudComp, "areaTitleTMP", areaTMP);

        GameObject enemyGo = CreateUIObject("EnemyCountTMP", hudRoot.transform, new Vector2(0.62f, 0.95f), new Vector2(0.96f, 0.985f));
        TextMeshProUGUI enemyTMP = enemyGo.AddComponent<TextMeshProUGUI>();
        enemyTMP.text = "<color=#FF5544>ENEMIES REMAINING:</color> <b>3</b>";
        enemyTMP.fontSize = 12;
        enemyTMP.alignment = TextAlignmentOptions.MidlineRight;
        SetField(hudComp, "enemyCountTMP", enemyTMP);

        // 3. Active Run Boons Strip (Horizontal row above controls: x: 0.04 to 0.96, y: 0.835 to 0.87)
        GameObject boonsStrip = CreateUIObject("ActiveBoonsContainer", hudRoot.transform, new Vector2(0.04f, 0.835f), new Vector2(0.96f, 0.87f));
        HorizontalLayoutGroup bLayout = boonsStrip.AddComponent<HorizontalLayoutGroup>();
        bLayout.spacing = 6f;
        bLayout.childAlignment = TextAnchor.MiddleLeft;
        bLayout.childForceExpandWidth = false;
        bLayout.childForceExpandHeight = false;
        SetField(hudComp, "activeBoonsContainer", boonsStrip.transform);

        // 4. Tier Escalation Alert Banner (Centered modal flash)
        GameObject alertBanner = CreateUIObject("TierAlertBanner", hudRoot.transform, new Vector2(0.10f, 0.45f), new Vector2(0.90f, 0.58f));
        Image aBg = alertBanner.AddComponent<Image>();
        aBg.color = new Color(0.05f, 0.06f, 0.10f, 0.95f);

        GameObject alertTextGo = CreateUIObject("AlertTextTMP", alertBanner.transform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
        TextMeshProUGUI alertTMP = alertTextGo.AddComponent<TextMeshProUGUI>();
        alertTMP.text = "<color=#FF2040>▲ DIFFICULTY INCREASE ▲</color>\n<size=125%><color=#FF9922><b>>> HARD <<</b></color></size>\n<size=75%><color=#E0E5F0>Enemies are deadlier and swarm in greater numbers!</color></size>";
        alertTMP.fontSize = 15;
        alertTMP.alignment = TextAlignmentOptions.Center;
        alertBanner.SetActive(false);
        SetField(hudComp, "tierAlertBannerObj", alertBanner);
        SetField(hudComp, "tierAlertTMP", alertTMP);
    }

    public static void BuildDungeonBoonRewardUI(Transform canvasParent)
    {
        Transform existing = canvasParent.Find("DungeonBoonRewardModal");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        // Full modal overlay
        GameObject modalRoot = CreateUIObject("DungeonBoonRewardModal", canvasParent, Vector2.zero, Vector2.one);
        Image mBg = modalRoot.AddComponent<Image>();
        mBg.color = new Color(0.02f, 0.03f, 0.06f, 0.99f);

        var rewardComp = modalRoot.AddComponent<DungeonBoonRewardUI>();
        SetField(rewardComp, "modalRoot", modalRoot);

        // Header Title
        GameObject headGo = CreateUIObject("HeaderTitleTMP", modalRoot.transform, new Vector2(0.05f, 0.89f), new Vector2(0.95f, 0.96f));
        TextMeshProUGUI headTMP = headGo.AddComponent<TextMeshProUGUI>();
        headTMP.text = "<color=#FFD700><b>AREA 1 CLEARED!</b></color>";
        headTMP.fontSize = 24;
        headTMP.alignment = TextAlignmentOptions.Center;
        SetField(rewardComp, "headerTitleTMP", headTMP);

        // Header Subtitle
        GameObject subGo = CreateUIObject("HeaderSubtitleTMP", modalRoot.transform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.88f));
        TextMeshProUGUI subTMP = subGo.AddComponent<TextMeshProUGUI>();
        subTMP.text = "<color=#40EBFF>SELECT A TEMPORARY DUNGEON BOON</color> <size=80%>(Persists for this run)</size>";
        subTMP.fontSize = 13;
        subTMP.alignment = TextAlignmentOptions.Center;
        SetField(rewardComp, "headerSubtitleTMP", subTMP);

        // 3 Cards Layout Container (Centered: x: 0.03 to 0.97, y: 0.14 to 0.80)
        GameObject cardsContainer = CreateUIObject("CardsContainer", modalRoot.transform, new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.80f));
        HorizontalLayoutGroup cLayout = cardsContainer.AddComponent<HorizontalLayoutGroup>();
        cLayout.spacing = 14f;
        cLayout.padding = new RectOffset(8, 8, 8, 8);
        cLayout.childAlignment = TextAnchor.MiddleCenter;
        cLayout.childControlWidth = true;
        cLayout.childControlHeight = true;
        cLayout.childForceExpandWidth = true;
        cLayout.childForceExpandHeight = true;
        SetField(rewardComp, "cardsContainer", cardsContainer.transform);

        List<GameObject> cardSlots = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            GameObject cardObj = CreateBoonCardSlot(cardsContainer.transform, i + 1);
            cardSlots.Add(cardObj);
        }
        SetField(rewardComp, "boonCardSlots", cardSlots);

        modalRoot.transform.SetAsLastSibling();
        modalRoot.SetActive(false);
    }

    private static GameObject CreateBoonCardSlot(Transform parent, int index)
    {
        GameObject cardObj = CreateUIObject($"BoonCard_{index}", parent, Vector2.zero, Vector2.one);
        LayoutElement le = cardObj.AddComponent<LayoutElement>();
        le.preferredWidth = 210f;
        le.preferredHeight = 580f;
        le.flexibleWidth = 1f;
        le.flexibleHeight = 1f;

        Image cardBg = cardObj.AddComponent<Image>();
        Sprite cSp = GetSprite("Mockup_BoonCard_Frame.png");
        if (cSp != null) cardBg.sprite = cSp;
        cardBg.color = Color.white;

        // Border Glow
        GameObject borderGo = CreateUIObject("BorderImg", cardObj.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));
        Image bImg = borderGo.AddComponent<Image>();
        bImg.color = new Color(0.85f, 0.70f, 0.25f, 0.15f);

        // Rarity Tag
        GameObject rGo = CreateUIObject("RarityTMP", cardObj.transform, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.95f));
        TextMeshProUGUI rTMP = rGo.AddComponent<TextMeshProUGUI>();
        rTMP.text = "<color=#FFD700>[LEGENDARY]</color>";
        rTMP.fontSize = 12;
        rTMP.alignment = TextAlignmentOptions.Center;

        // Title
        GameObject tGo = CreateUIObject("TitleTMP", cardObj.transform, new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.88f));
        TextMeshProUGUI tTMP = tGo.AddComponent<TextMeshProUGUI>();
        tTMP.text = "<b>VOID EDGE</b>";
        tTMP.fontSize = 15;
        tTMP.alignment = TextAlignmentOptions.Center;
        tTMP.color = Color.white;

        // Icon Frame & Image
        GameObject iconFrame = CreateUIObject("IconFrame", cardObj.transform, new Vector2(0.20f, 0.44f), new Vector2(0.80f, 0.76f));
        Image ifBg = iconFrame.AddComponent<Image>();
        ifBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        GameObject iconImgGo = CreateUIObject("IconImg", iconFrame.transform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
        Image iImg = iconImgGo.AddComponent<Image>();
        iImg.preserveAspect = true;
        iImg.color = Color.white;

        // Description
        GameObject dGo = CreateUIObject("DescTMP", cardObj.transform, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.42f));
        TextMeshProUGUI dTMP = dGo.AddComponent<TextMeshProUGUI>();
        dTMP.text = "+25% Melee & Spell Damage for this entire dungeon run.";
        dTMP.fontSize = 11.5f;
        dTMP.alignment = TextAlignmentOptions.Center;
        dTMP.color = new Color(0.85f, 0.90f, 0.95f);

        // Select Button
        GameObject btnGo = CreateUIObject("SelectBtn", cardObj.transform, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.135f));
        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.14f, 0.45f, 0.85f);
        Button btn = btnGo.AddComponent<Button>();

        GameObject btnTxtGo = CreateUIObject("BtnLabel", btnGo.transform, Vector2.zero, Vector2.one);
        TextMeshProUGUI bTmp = btnTxtGo.AddComponent<TextMeshProUGUI>();
        bTmp.text = "<b>CHOOSE BOON</b>";
        bTmp.fontSize = 11;
        bTmp.alignment = TextAlignmentOptions.Center;
        bTmp.color = Color.white;

        return cardObj;
    }

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

    private static Sprite GetSprite(string filename)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(TEXTURE_DIR + filename);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null) f.SetValue(target, value);
    }
    #endregion
}

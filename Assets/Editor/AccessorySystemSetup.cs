using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BattleSummoner.Combat;

public static class AccessorySystemSetup
{
    private const string TEXTURE_DIR = "Assets/Textures/UI";
    private const string PREFAB_PATH = "Assets/Prefabs/UI/AccessorySlotPrefab.prefab";

    [MenuItem("Tools/BattleSummoner/Setup Uncapped Accessory System")]
    public static void SetupAccessorySystem()
    {
        EnsureDirectories();
        GenerateAllAccessoryTextures();
        ReimportAccessorySprites();
        GameObject slotPrefab = BuildOrCreateSlotPrefab();
        ConfigureSceneAccessorySystem(slotPrefab);
        Debug.Log("✨ [ACCESSORY SYSTEM] Successfully configured Uncapped Accessory System in MainDungeonFloor.unity!");
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(TEXTURE_DIR)) Directory.CreateDirectory(TEXTURE_DIR);
        if (!Directory.Exists("Assets/Prefabs/UI")) Directory.CreateDirectory("Assets/Prefabs/UI");
    }

    #region Procedural Sprite Generation
    private static void GenerateAllAccessoryTextures()
    {
        CreateTexture("Mockup_Acc_SlotFrame.png", 128, 128, DrawSlotFrame);
        CreateTexture("Mockup_Acc_Ring.png", 128, 128, DrawRingSprite);
        CreateTexture("Mockup_Acc_Talisman.png", 128, 128, DrawTalismanSprite);
        CreateTexture("Mockup_Acc_Fang.png", 128, 128, DrawFangSprite);
        CreateTexture("Mockup_Acc_Ribbon.png", 128, 128, DrawRibbonSprite);
        CreateTexture("Mockup_Acc_Orb.png", 128, 128, DrawOrbSprite);
        CreateTexture("Mockup_Acc_Cog.png", 128, 128, DrawCogSprite);
        CreateTexture("Mockup_Acc_Bloodstone.png", 128, 128, DrawBloodstoneSprite);
        CreateTexture("Mockup_Acc_Kinetic.png", 128, 128, DrawKineticRelicSprite);
    }

    private static void CreateTexture(string filename, int width, int height, Action<Color[], int, int> drawAction)
    {
        string fullPath = Path.Combine(TEXTURE_DIR, filename).Replace('\\', '/');
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        drawAction(pixels, width, height);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();

        byte[] pngBytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        File.WriteAllBytes(fullPath, pngBytes);
    }

    private static void ReimportAccessorySprites()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string[] files = Directory.GetFiles(TEXTURE_DIR, "Mockup_Acc_*.png");
        for (int i = 0; i < files.Length; i++)
        {
            string p = files[i].Replace('\\', '/');
            TextureImporter imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void DrawSlotFrame(Color[] c, int w, int h)
    {
        Color borderGold = new Color(0.85f, 0.7f, 0.3f, 0.95f);
        Color innerDark = new Color(0.08f, 0.11f, 0.18f, 0.92f);
        Color bevelLight = new Color(0.95f, 0.85f, 0.45f, 0.5f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                bool isBorder = (x < 6 || x >= w - 6 || y < 6 || y >= h - 6);
                bool isCorner = (x < 12 && y < 12) || (x >= w - 12 && y < 12) || (x < 12 && y >= h - 12) || (x >= w - 12 && y >= h - 12);

                if (isBorder || isCorner)
                {
                    c[idx] = (x == y || x == w - y) ? bevelLight : borderGold;
                }
                else
                {
                    c[idx] = innerDark;
                }
            }
        }
    }

    private static void DrawRingSprite(Color[] c, int w, int h)
    {
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        float rOuter = w * 0.38f;
        float rInner = w * 0.24f;
        Color gold = new Color(1f, 0.84f, 0.2f, 1f);
        Color gemCyan = new Color(0.2f, 0.9f, 1f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist >= rInner && dist <= rOuter)
                {
                    c[y * w + x] = gold;
                }
                // Gem on top
                if (dist < w * 0.16f && y > h * 0.55f)
                {
                    c[y * w + x] = gemCyan;
                }
            }
        }
    }

    private static void DrawTalismanSprite(Color[] c, int w, int h)
    {
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        Color purple = new Color(0.7f, 0.3f, 0.95f, 1f);
        Color eyeGlow = new Color(0.3f, 1f, 0.8f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - center.x) / (w * 0.36f);
                float dy = Mathf.Abs(y - center.y) / (h * 0.44f);
                if (dx + dy <= 1f)
                {
                    c[y * w + x] = (dx * dx + dy * dy < 0.25f) ? eyeGlow : purple;
                }
            }
        }
    }

    private static void DrawFangSprite(Color[] c, int w, int h)
    {
        Color ivory = new Color(0.96f, 0.92f, 0.82f, 1f);
        Color bloodTip = new Color(0.85f, 0.12f, 0.2f, 1f);

        for (int y = 0; y < h; y++)
        {
            float normY = (float)y / h;
            float halfWidth = (1f - normY) * (w * 0.35f);
            float centerX = w * 0.5f + (Mathf.Sin(normY * Mathf.PI) * (w * 0.1f));

            for (int x = 0; x < w; x++)
            {
                if (Mathf.Abs(x - centerX) <= halfWidth && normY < 0.85f && normY > 0.08f)
                {
                    c[y * w + x] = (normY < 0.35f) ? bloodTip : ivory;
                }
            }
        }
    }

    private static void DrawRibbonSprite(Color[] c, int w, int h)
    {
        Color pink = new Color(0.98f, 0.45f, 0.72f, 1f);
        Color core = new Color(1f, 0.85f, 0.9f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float curve1 = h * 0.5f + Mathf.Sin((float)x / w * Mathf.PI * 2f) * (h * 0.25f);
                float curve2 = h * 0.5f - Mathf.Cos((float)x / w * Mathf.PI * 2f) * (h * 0.25f);

                if (Mathf.Abs(y - curve1) < 8f || Mathf.Abs(y - curve2) < 8f)
                {
                    c[y * w + x] = (Mathf.Abs(y - curve1) < 3f) ? core : pink;
                }
            }
        }
    }

    private static void DrawOrbSprite(Color[] c, int w, int h)
    {
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        float radius = w * 0.36f;
        Color blueDeep = new Color(0.15f, 0.4f, 0.95f, 1f);
        Color coreLight = new Color(0.6f, 0.9f, 1f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    float factor = 1f - (dist / radius);
                    c[y * w + x] = Color.Lerp(blueDeep, coreLight, factor * factor);
                }
            }
        }
    }

    private static void DrawCogSprite(Color[] c, int w, int h)
    {
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        Color brass = new Color(0.85f, 0.65f, 0.25f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float angle = Mathf.Atan2(y - center.y, x - center.x);
                float tooth = Mathf.Sin(angle * 8f);

                float outer = w * 0.35f + (tooth > 0f ? w * 0.08f : 0f);
                if (dist >= w * 0.14f && dist <= outer)
                {
                    c[y * w + x] = brass;
                }
            }
        }
    }

    private static void DrawBloodstoneSprite(Color[] c, int w, int h)
    {
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        Color crimson = new Color(0.88f, 0.08f, 0.15f, 1f);
        Color highlight = new Color(1f, 0.45f, 0.55f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= w * 0.34f)
                {
                    c[y * w + x] = (x > center.x - 8 && y > center.y + 8) ? highlight : crimson;
                }
            }
        }
    }

    private static void DrawKineticRelicSprite(Color[] c, int w, int h)
    {
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        Color electricYellow = new Color(1f, 0.95f, 0.25f, 1f);
        Color auraAmber = new Color(1f, 0.5f, 0.05f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Lightning bolt jagged path
                float normY = (float)y / h;
                float pathX = center.x + (normY > 0.5f ? (normY - 0.5f) * 25f : -(0.5f - normY) * 25f);
                if (Mathf.Abs(x - pathX) < 14f && normY > 0.1f && normY < 0.9f)
                {
                    c[y * w + x] = (Mathf.Abs(x - pathX) < 6f) ? electricYellow : auraAmber;
                }
            }
        }
    }
    #endregion

    #region Prefab Construction
    public static GameObject BuildOrCreateSlotPrefab()
    {
        Sprite slotFrame = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_SlotFrame.png"));

        GameObject root = new GameObject("AccessorySlotPrefab", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64f, 64f);

        Image bg = root.GetComponent<Image>();
        if (slotFrame != null)
        {
            bg.sprite = slotFrame;
            bg.type = Image.Type.Simple;
        }
        bg.color = Color.white;

        // Child 1: Icon (stretched to edges)
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(root.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.sizeDelta = Vector2.zero;
        iconRt.offsetMin = new Vector2(6f, 6f);
        iconRt.offsetMax = new Vector2(-6f, -6f);

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;

        // Child 2: StackBadge (bottom-right anchored TextMeshProUGUI with high-contrast black outline)
        GameObject badgeObj = new GameObject("StackBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeObj.transform.SetParent(root.transform, false);
        RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.35f, 0f);
        badgeRt.anchorMax = new Vector2(1f, 0.42f);
        badgeRt.anchoredPosition = Vector2.zero;
        badgeRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI badgeTMP = badgeObj.GetComponent<TextMeshProUGUI>();
        badgeTMP.alignment = TextAlignmentOptions.BottomRight;
        badgeTMP.fontSize = 15f;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.color = new Color(1f, 0.92f, 0.45f, 1f);
        badgeTMP.outlineColor = Color.black;
        badgeTMP.outlineWidth = 0.28f;
        badgeTMP.textWrappingMode = TextWrappingModes.NoWrap;

        // Save as Prefab asset
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        UnityEngine.Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        return prefab;
    }
    #endregion

    #region Scene Hierarchy Configuration
    public static void ConfigureSceneAccessorySystem(GameObject slotPrefab)
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in active scene!");
            return;
        }

        Transform hudParent = canvas.transform.Find("MockupMasterHUD") ?? canvas.transform;

        // 1. In-Combat Telemetry Ribbon (Collapsed State)
        Transform ribbonTrans = hudParent.Find("AccessoryRibbonContainer");
        GameObject ribbonObj;
        if (ribbonTrans == null)
        {
            ribbonObj = new GameObject("AccessoryRibbonContainer", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            ribbonObj.transform.SetParent(hudParent, false);
        }
        else
        {
            ribbonObj = ribbonTrans.gameObject;
        }

        RectTransform ribbonRt = ribbonObj.GetComponent<RectTransform>();
        ribbonRt.anchorMin = new Vector2(0.02f, 0.63f);
        ribbonRt.anchorMax = new Vector2(0.38f, 0.69f);
        ribbonRt.anchoredPosition = Vector2.zero;
        ribbonRt.sizeDelta = Vector2.zero;

        Image ribbonBg = ribbonObj.GetComponent<Image>();
        ribbonBg.color = new Color(0.05f, 0.08f, 0.14f, 0.75f);

        HorizontalLayoutGroup hlg = ribbonObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Ensure Ribbon Expand (+) button
        Transform plusTrans = ribbonObj.transform.Find("ExpandPlusButton");
        GameObject plusObj;
        if (plusTrans == null)
        {
            plusObj = new GameObject("ExpandPlusButton", typeof(RectTransform), typeof(Image), typeof(Button));
            plusObj.transform.SetParent(ribbonObj.transform, false);
        }
        else
        {
            plusObj = plusTrans.gameObject;
        }

        RectTransform plusRt = plusObj.GetComponent<RectTransform>();
        plusRt.sizeDelta = new Vector2(36f, 36f);
        Image plusImg = plusObj.GetComponent<Image>();
        plusImg.color = new Color(0.14f, 0.2f, 0.28f, 0.95f);
        Button plusBtn = plusObj.GetComponent<Button>();

        Transform plusTextTrans = plusObj.transform.Find("Text");
        TextMeshProUGUI plusText;
        if (plusTextTrans == null)
        {
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(plusObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            plusText = txtObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            plusText = plusTextTrans.GetComponent<TextMeshProUGUI>();
        }
        plusText.text = "+";
        plusText.alignment = TextAlignmentOptions.Center;
        plusText.fontSize = 24f;
        plusText.fontStyle = FontStyles.Bold;
        plusText.color = new Color(1f, 0.88f, 0.4f, 1f);


        // 2. Player Menu Drawer (Expanded State)
        Transform drawerTrans = hudParent.Find("AccessoryDrawerModal");
        GameObject drawerObj;
        if (drawerTrans == null)
        {
            drawerObj = new GameObject("AccessoryDrawerModal", typeof(RectTransform), typeof(Image));
            drawerObj.transform.SetParent(hudParent, false);
        }
        else
        {
            drawerObj = drawerTrans.gameObject;
        }

        RectTransform drawerRt = drawerObj.GetComponent<RectTransform>();
        drawerRt.anchorMin = new Vector2(0.02f, 0.25f);
        drawerRt.anchorMax = new Vector2(0.55f, 0.62f);
        drawerRt.anchoredPosition = Vector2.zero;
        drawerRt.sizeDelta = Vector2.zero;

        Image drawerBg = drawerObj.GetComponent<Image>();
        drawerBg.color = new Color(0.06f, 0.09f, 0.15f, 0.94f);

        // Header bar in Drawer
        Transform headerTrans = drawerObj.transform.Find("HeaderBar");
        GameObject headerObj;
        if (headerTrans == null)
        {
            headerObj = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(drawerObj.transform, false);
        }
        else headerObj = headerTrans.gameObject;

        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 0.91f);
        headerRt.anchorMax = Vector2.one;
        headerRt.sizeDelta = Vector2.zero;
        headerObj.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.26f, 0.98f);

        // Header Title
        Transform titleTrans = headerObj.transform.Find("TitleText");
        TextMeshProUGUI titleTMP;
        if (titleTrans == null)
        {
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.04f, 0f);
            trt.anchorMax = new Vector2(0.85f, 1f);
            trt.sizeDelta = Vector2.zero;
            titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        }
        else titleTMP = titleTrans.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "Uncapped Accessory Matrix";
        titleTMP.fontSize = 17f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.88f, 0.45f, 1f);
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Drawer Close Button
        Transform closeBtnTrans = headerObj.transform.Find("CloseDrawerButton");
        Button closeDrawerBtn;
        if (closeBtnTrans == null)
        {
            GameObject cObj = new GameObject("CloseDrawerButton", typeof(RectTransform), typeof(Image), typeof(Button));
            cObj.transform.SetParent(headerObj.transform, false);
            RectTransform crt = cObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.86f, 0.1f);
            crt.anchorMax = new Vector2(0.98f, 0.9f);
            crt.sizeDelta = Vector2.zero;
            cObj.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            closeDrawerBtn = cObj.GetComponent<Button>();

            GameObject xObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            xObj.transform.SetParent(cObj.transform, false);
            RectTransform xrt = xObj.GetComponent<RectTransform>();
            xrt.anchorMin = Vector2.zero;
            xrt.anchorMax = Vector2.one;
            xrt.sizeDelta = Vector2.zero;
            var xtmp = xObj.GetComponent<TextMeshProUGUI>();
            xtmp.text = "✕";
            xtmp.alignment = TextAlignmentOptions.Center;
            xtmp.fontSize = 18f;
            xtmp.color = Color.white;
        }
        else closeDrawerBtn = closeBtnTrans.GetComponent<Button>();

        // ScrollRect
        Transform scrollTrans = drawerObj.transform.Find("InfiniteScrollRect");
        GameObject scrollObj;
        if (scrollTrans == null)
        {
            scrollObj = new GameObject("InfiniteScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(drawerObj.transform, false);
        }
        else scrollObj = scrollTrans.gameObject;

        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.02f, 0.38f);
        scrollRt.anchorMax = new Vector2(0.98f, 0.90f);
        scrollRt.sizeDelta = Vector2.zero;

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        // Viewport
        Transform viewportTrans = scrollObj.transform.Find("Viewport");
        GameObject viewportObj;
        if (viewportTrans == null)
        {
            viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollObj.transform, false);
        }
        else viewportObj = viewportTrans.gameObject;

        RectTransform vpRt = viewportObj.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = Vector2.zero;
        scrollRect.viewport = vpRt;

        // Content (GridLayoutGroup)
        Transform contentTrans = viewportObj.transform.Find("Content");
        GameObject contentObj;
        if (contentTrans == null)
        {
            contentObj = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
        }
        else contentObj = contentTrans.gameObject;

        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, 300f);

        GridLayoutGroup grid = contentObj.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.cellSize = new Vector2(64f, 64f);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRt;


        // 3. Floating Telemetry Inspector Panel (Bottom of drawer)
        Transform tooltipTrans = drawerObj.transform.Find("TooltipInspectorPanel");
        GameObject tooltipObj;
        if (tooltipTrans == null)
        {
            tooltipObj = new GameObject("TooltipInspectorPanel", typeof(RectTransform), typeof(Image));
            tooltipObj.transform.SetParent(drawerObj.transform, false);
        }
        else tooltipObj = tooltipTrans.gameObject;

        RectTransform ttRt = tooltipObj.GetComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0.02f, 0.02f);
        ttRt.anchorMax = new Vector2(0.98f, 0.36f);
        ttRt.sizeDelta = Vector2.zero;

        Image ttBg = tooltipObj.GetComponent<Image>();
        ttBg.color = new Color(0.04f, 0.06f, 0.12f, 0.98f);

        // Tooltip Sub-Header (Stack Telemetry Inspector)
        Transform ttSubHeaderTrans = tooltipObj.transform.Find("SubHeader");
        if (ttSubHeaderTrans == null)
        {
            GameObject subObj = new GameObject("SubHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform srt = subObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.04f, 0.78f);
            srt.anchorMax = new Vector2(0.85f, 0.96f);
            srt.sizeDelta = Vector2.zero;
            var subTMP = subObj.GetComponent<TextMeshProUGUI>();
            subTMP.text = "Stack Telemetry Inspector";
            subTMP.fontSize = 11f;
            subTMP.fontStyle = FontStyles.Bold;
            subTMP.color = new Color(0.95f, 0.8f, 0.3f, 1f);
        }
        else
        {
            RectTransform srt = ttSubHeaderTrans.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.04f, 0.78f);
            srt.anchorMax = new Vector2(0.85f, 0.96f);
            srt.sizeDelta = Vector2.zero;
            var subTMP = ttSubHeaderTrans.GetComponent<TextMeshProUGUI>();
            subTMP.text = "Stack Telemetry Inspector";
            subTMP.fontSize = 11f;
            subTMP.fontStyle = FontStyles.Bold;
            subTMP.color = new Color(0.95f, 0.8f, 0.3f, 1f);
        }

        // Tooltip Title
        Transform ttTitleTrans = tooltipObj.transform.Find("Title");
        TextMeshProUGUI ttTitle;
        if (ttTitleTrans == null)
        {
            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform trt = tObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.04f, 0.54f);
            trt.anchorMax = new Vector2(0.88f, 0.76f);
            trt.sizeDelta = Vector2.zero;
            ttTitle = tObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            ttTitle = ttTitleTrans.GetComponent<TextMeshProUGUI>();
            RectTransform trt = ttTitleTrans.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.04f, 0.54f);
            trt.anchorMax = new Vector2(0.88f, 0.76f);
            trt.sizeDelta = Vector2.zero;
        }
        ttTitle.text = "Item: Voracious Fang (x12)";
        ttTitle.fontSize = 13f;
        ttTitle.fontStyle = FontStyles.Bold;
        ttTitle.color = Color.white;

        // Tooltip Math Summary
        Transform ttMathTrans = tooltipObj.transform.Find("MathSummary");
        TextMeshProUGUI ttMath;
        if (ttMathTrans == null)
        {
            GameObject mObj = new GameObject("MathSummary", typeof(RectTransform), typeof(TextMeshProUGUI));
            mObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform mrt = mObj.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.04f, 0.30f);
            mrt.anchorMax = new Vector2(0.96f, 0.52f);
            mrt.sizeDelta = Vector2.zero;
            ttMath = mObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            ttMath = ttMathTrans.GetComponent<TextMeshProUGUI>();
            RectTransform mrt = ttMathTrans.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.04f, 0.30f);
            mrt.anchorMax = new Vector2(0.96f, 0.52f);
            mrt.sizeDelta = Vector2.zero;
        }
        ttMath.text = "Scaling: Hyperbolic (+64.3%)";
        ttMath.fontSize = 12f;
        ttMath.fontStyle = FontStyles.Bold;
        ttMath.color = new Color(0.4f, 0.95f, 0.55f, 1f);

        // Tooltip Lore
        Transform ttLoreTrans = tooltipObj.transform.Find("Lore");
        TextMeshProUGUI ttLore;
        if (ttLoreTrans == null)
        {
            GameObject lObj = new GameObject("Lore", typeof(RectTransform), typeof(TextMeshProUGUI));
            lObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform lrt = lObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.04f, 0.04f);
            lrt.anchorMax = new Vector2(0.96f, 0.28f);
            lrt.sizeDelta = Vector2.zero;
            ttLore = lObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            ttLore = ttLoreTrans.GetComponent<TextMeshProUGUI>();
            RectTransform lrt = ttLoreTrans.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.04f, 0.04f);
            lrt.anchorMax = new Vector2(0.96f, 0.28f);
            lrt.sizeDelta = Vector2.zero;
        }
        ttLore.text = "A calcified abyss fang ripped from a leviathan. Scales asymptotically towards total kinetic leech.";
        ttLore.fontSize = 10f;
        ttLore.color = new Color(0.85f, 0.9f, 0.95f, 0.85f);
        ttLore.textWrappingMode = TextWrappingModes.Normal;

        // Tooltip Close
        Transform ttCloseTrans = tooltipObj.transform.Find("CloseButton");
        Button ttCloseBtn;
        if (ttCloseTrans == null)
        {
            GameObject cbObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            cbObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform cbrt = cbObj.GetComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(0.91f, 0.72f);
            cbrt.anchorMax = new Vector2(0.98f, 0.96f);
            cbrt.sizeDelta = Vector2.zero;
            cbObj.GetComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            ttCloseBtn = cbObj.GetComponent<Button>();
        }
        else ttCloseBtn = ttCloseTrans.GetComponent<Button>();


        // 4. Attach PlayerAccessoryMenuUI component to MockupMasterHUD
        PlayerAccessoryMenuUI menuUI = hudParent.GetComponent<PlayerAccessoryMenuUI>();
        if (menuUI == null) menuUI = hudParent.gameObject.AddComponent<PlayerAccessoryMenuUI>();

        // Serialized field reflection/assignment
        var so = new SerializedObject(menuUI);
        so.FindProperty("hudRibbonContainer").objectReferenceValue = ribbonRt;
        so.FindProperty("expandedDrawerModal").objectReferenceValue = drawerRt;
        so.FindProperty("infiniteScrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("drawerGridLayout").objectReferenceValue = grid;
        so.FindProperty("accessorySlotPrefab").objectReferenceValue = slotPrefab;
        so.FindProperty("ribbonExpandButton").objectReferenceValue = plusBtn;

        var bagObj = GameObject.Find("MessengerBagButton");
        if (bagObj != null)
        {
            so.FindProperty("messengerBagButton").objectReferenceValue = bagObj.GetComponent<Button>();
        }

        so.FindProperty("tooltipPanel").objectReferenceValue = tooltipObj;
        so.FindProperty("tooltipTitleText").objectReferenceValue = ttTitle;
        so.FindProperty("tooltipMathText").objectReferenceValue = ttMath;
        so.FindProperty("tooltipLoreText").objectReferenceValue = ttLore;
        so.FindProperty("tooltipCloseButton").objectReferenceValue = ttCloseBtn;
        so.ApplyModifiedProperties();

        // Wire persistent button listeners
        while (plusBtn.onClick.GetPersistentEventCount() > 0)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(plusBtn.onClick, 0);
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(plusBtn.onClick, menuUI.ToggleAccessoryDrawer);
        plusBtn.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);

        if (bagObj != null)
        {
            var b = bagObj.GetComponent<Button>();
            while (b.onClick.GetPersistentEventCount() > 0)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(b.onClick, 0);
            }
            UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, menuUI.ToggleAccessoryDrawer);
            b.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
        }

        while (closeDrawerBtn.onClick.GetPersistentEventCount() > 0)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(closeDrawerBtn.onClick, 0);
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeDrawerBtn.onClick, menuUI.ToggleAccessoryDrawer);
        closeDrawerBtn.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);

        while (ttCloseBtn.onClick.GetPersistentEventCount() > 0)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(ttCloseBtn.onClick, 0);
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(ttCloseBtn.onClick, menuUI.CloseTooltip);
        ttCloseBtn.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);

        // 5. Setup UncappedAccessorySystem & FruitNinjaBladeCutter on Player_Hero
        GameObject player = GameObject.Find("Player_Hero");
        if (player != null)
        {
            UncappedAccessorySystem accSys = player.GetComponent<UncappedAccessorySystem>();
            if (accSys == null) accSys = player.AddComponent<UncappedAccessorySystem>();

            PopulateDefaultAccessoryBuffer(accSys);

            FruitNinjaBladeCutter bladeCutter = player.GetComponent<FruitNinjaBladeCutter>();
            if (bladeCutter == null) bladeCutter = player.AddComponent<FruitNinjaBladeCutter>();
        }

        // Initially hide drawer and tooltip
        drawerObj.SetActive(false);
        tooltipObj.SetActive(false);

        // Mark scene dirty and save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    private static void PopulateDefaultAccessoryBuffer(UncappedAccessorySystem accSys)
    {
        accSys.ClearBuffer();

        Sprite spFang = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_Fang.png"));
        Sprite spRing = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_Ring.png"));
        Sprite spTalisman = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_Talisman.png"));
        Sprite spRibbon = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_Ribbon.png"));
        Sprite spOrb = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_Orb.png"));
        Sprite spCog = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_Cog.png"));
        Sprite spBlood = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_Bloodstone.png"));
        Sprite spKinetic = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TEXTURE_DIR, "Mockup_Acc_Kinetic.png"));

        accSys.AcquireAccessory(new AccessoryDefinition
        {
            accessoryId = "voracious_fang",
            accessoryName = "Voracious Fang",
            scalingType = ScalingCurveType.HyperbolicAsymptote,
            baseModifier = 0.15f,
            iconSprite = spFang,
            loreDescription = "A calcified abyss fang ripped from a leviathan. Scales asymptotically towards total kinetic leech."
        }, 12);

        accSys.AcquireAccessory(new AccessoryDefinition
        {
            accessoryId = "starfire_ring",
            accessoryName = "Starfire Ring",
            scalingType = ScalingCurveType.LinearAdditive,
            baseModifier = 0.25f,
            iconSprite = spRing,
            loreDescription = "Infuses weapon swings with steady astral resonance. Flat +25% bonus per stack."
        }, 4);

        accSys.AcquireAccessory(new AccessoryDefinition
        {
            accessoryId = "void_ward_talisman",
            accessoryName = "Void Ward Talisman",
            scalingType = ScalingCurveType.HyperbolicAsymptote,
            baseModifier = 0.20f,
            iconSprite = spTalisman,
            loreDescription = "Distorts spatial fabric to deflect incoming kinetic shockwaves."
        }, 1);

        accSys.AcquireAccessory(new AccessoryDefinition
        {
            accessoryId = "astral_ribbon",
            accessoryName = "Astral Ribbon",
            scalingType = ScalingCurveType.LinearAdditive,
            baseModifier = 0.10f,
            iconSprite = spRibbon,
            loreDescription = "Gossamer fabric spun from astral starlight. Enhances flow velocity."
        }, 3);

        accSys.AcquireAccessory(new AccessoryDefinition
        {
            accessoryId = "chrono_orb",
            accessoryName = "Chrono Orb",
            scalingType = ScalingCurveType.HyperbolicAsymptote,
            baseModifier = 0.12f,
            iconSprite = spOrb,
            loreDescription = "Compresses localized spacetime intervals during high-velocity slashes."
        }, 2);

        accSys.AcquireAccessory(new AccessoryDefinition
        {
            accessoryId = "spire_cog",
            accessoryName = "Spire Cog",
            scalingType = ScalingCurveType.LinearAdditive,
            baseModifier = 0.15f,
            iconSprite = spCog,
            loreDescription = "Clockwork cog salvaged from upper floors. Hardens mechanical impact."
        }, 7);

        accSys.AcquireAccessory(new AccessoryDefinition
        {
            accessoryId = "bloodstone_eye",
            accessoryName = "Bloodstone Eye",
            scalingType = ScalingCurveType.HyperbolicAsymptote,
            baseModifier = 0.25f,
            iconSprite = spBlood,
            loreDescription = "An all-seeing ocular gemstone that detects weak structural points."
        }, 1);

        accSys.AcquireAccessory(new AccessoryDefinition
        {
            accessoryId = "kinetic_strike_relic",
            accessoryName = "Kinetic Strike Relic",
            scalingType = ScalingCurveType.ExponentialCompound,
            baseModifier = 3.0f,
            iconSprite = spKinetic,
            loreDescription = "Legendary high-floor boss drop granting exponential kinetic multiplication damped by gamma=0.65."
        }, 5);
    }
    #endregion
}

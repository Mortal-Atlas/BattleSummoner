using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class DarkAcademiaAssetIntegrator
{
    private const string TEXTURE_DIR = "Assets/Textures/DarkAcademia/";
    private const string MAT_DIR = "Assets/Materials/";

    [MenuItem("Tools/BattleSummoner/Apply Fantasy Dark Academia Aesthetic")]
    public static void ApplyDarkAcademiaAesthetic()
    {
        CreateMaterials();
        ApplyToDungeonFloorScene();
        ApplyToOverworldScene();
        Debug.Log("🕯️ [DARK ACADEMIA] Successfully applied full Fantasy Dark Academia aesthetic across all game systems!");
    }

    public static void CreateMaterials()
    {
        if (!System.IO.Directory.Exists(MAT_DIR))
        {
            System.IO.Directory.CreateDirectory(MAT_DIR);
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

        // 1. Floor Material (Slate & Inlaid Brass Zodiac)
        Texture2D floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_DIR + "dark_academia_dungeon_floor.jpg");
        Material floorMat = new Material(litShader);
        floorMat.name = "M_DarkAcademia_Floor";
        if (floorTex != null)
        {
            floorMat.mainTexture = floorTex;
            floorMat.mainTextureScale = new Vector2(4f, 4f);
        }
        floorMat.SetFloat("_Smoothness", 0.40f);
        floorMat.SetFloat("_Metallic", 0.25f);
        AssetDatabase.CreateAsset(floorMat, MAT_DIR + "M_DarkAcademia_Floor.mat");

        // 2. Gothic Wall / Pillar Material (Ribbed Granite & Mahogany)
        Texture2D wallTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_DIR + "dark_academia_gothic_wall.jpg");
        Material wallMat = new Material(litShader);
        wallMat.name = "M_DarkAcademia_GothicWall";
        if (wallTex != null)
        {
            wallMat.mainTexture = wallTex;
            wallMat.mainTextureScale = new Vector2(1f, 3f);
        }
        wallMat.SetFloat("_Smoothness", 0.25f);
        wallMat.SetFloat("_Metallic", 0.15f);
        AssetDatabase.CreateAsset(wallMat, MAT_DIR + "M_DarkAcademia_GothicWall.mat");

        // 3. Portal Vortex Material (Celestial Astrolabe & Soul-Fire)
        Texture2D vortexTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_DIR + "dark_academia_portal_vortex.jpg");
        Material vortexMat = new Material(unlitShader);
        vortexMat.name = "M_DarkAcademia_PortalVortex";
        if (vortexTex != null)
        {
            vortexMat.mainTexture = vortexTex;
        }
        vortexMat.color = Color.white;
        AssetDatabase.CreateAsset(vortexMat, MAT_DIR + "M_DarkAcademia_PortalVortex.mat");

        // 4. Arcane Summoning Circle Material
        Texture2D runeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_DIR + "dark_academia_rune_circle.jpg");
        Material runeMat = new Material(unlitShader);
        runeMat.name = "M_DarkAcademia_SummoningCircle";
        if (runeTex != null)
        {
            runeMat.mainTexture = runeTex;
        }
        runeMat.color = Color.white;
        AssetDatabase.CreateAsset(runeMat, MAT_DIR + "M_DarkAcademia_SummoningCircle.mat");

        AssetDatabase.SaveAssets();
        Debug.Log("🎨 [DARK ACADEMIA] Materials created and saved to Assets/Materials/");
    }

    public static void ApplyToDungeonFloorScene()
    {
        string scenePath = "Assets/Scenes/MainDungeonFloor.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Apply Materials to 3D Geometry
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "M_DarkAcademia_Floor.mat");
        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "M_DarkAcademia_GothicWall.mat");

        var floorArena = GameObject.Find("DungeonFloorArena");
        if (floorArena != null && floorMat != null)
        {
            var rend = floorArena.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = floorMat;
        }

        for (int i = 1; i <= 6; i++)
        {
            var pillar = GameObject.Find($"SpirePillar_{i}");
            if (pillar != null && wallMat != null)
            {
                var rend = pillar.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = wallMat;
            }
        }

        // 2. Atmospheric Mood: Amber Candlelight & Occult Night Fog
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.10f, 0.16f); // Deep gothic indigo

        var dirLightGo = GameObject.Find("Directional Light");
        if (dirLightGo != null)
        {
            var light = dirLightGo.GetComponent<Light>();
            if (light != null)
            {
                light.color = new Color(1.0f, 0.82f, 0.60f); // Warm candlelight amber
                light.intensity = 1.05f;
            }
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.06f, 0.05f, 0.09f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;

        // 3. UI Upgrade to Dark Academia
        var canvasGo = GameObject.Find("PortraitHUDCanvas");
        if (canvasGo != null)
        {
            UpgradeRoRHUD(canvasGo.transform);
            UpgradeBoonRewardUI(canvasGo.transform);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("💾 [DARK ACADEMIA] MainDungeonFloor updated with Dark Academia assets.");
    }

    public static void ApplyToOverworldScene()
    {
        string scenePath = "Assets/Scenes/OverworldMap.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Material vortexMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "M_DarkAcademia_PortalVortex.mat");
        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "M_DarkAcademia_GothicWall.mat");

        var outpost = GameObject.Find("Otherworldly_Portal_Outpost");
        if (outpost != null)
        {
            // Apply vortex texture
            var vortexDisc = outpost.transform.Find("PortalVortexDisc");
            if (vortexDisc != null && vortexMat != null)
            {
                var rend = vortexDisc.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = vortexMat;
            }

            // Apply gothic stone to arch
            var archTop = outpost.transform.Find("Arch_Top");
            if (archTop != null && wallMat != null)
            {
                var rend = archTop.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = wallMat;
            }

            var pillarL = outpost.transform.Find("Pillar_Left");
            if (pillarL != null && wallMat != null)
            {
                var rend = pillarL.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = wallMat;
            }

            var pillarR = outpost.transform.Find("Pillar_Right");
            if (pillarR != null && wallMat != null)
            {
                var rend = pillarR.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = wallMat;
            }

            // Tune portal light to mystical amber/violet
            var lightTr = outpost.transform.Find("PortalAuraLight");
            if (lightTr != null)
            {
                var l = lightTr.GetComponent<Light>();
                if (l != null)
                {
                    l.color = new Color(0.95f, 0.40f, 0.85f);
                    l.intensity = 4.0f;
                    l.range = 18f;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("💾 [DARK ACADEMIA] OverworldMap updated with Dark Academia assets.");
    }

    private static void UpgradeRoRHUD(Transform canvasParent)
    {
        var hudTr = canvasParent.Find("DungeonDimensionHUD");
        if (hudTr == null) return;

        // Astrolabe Chronometer Frame
        var clockFrame = hudTr.Find("RoR_DifficultyClockFrame");
        if (clockFrame != null)
        {
            var img = clockFrame.GetComponent<Image>();
            Sprite clockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TEXTURE_DIR + "dark_academia_ror_clock.jpg");
            if (img != null && clockSprite != null)
            {
                img.sprite = clockSprite;
                img.color = Color.white;
            }

            // Adjust RectTransform for the ornate astrolabe chronometer
            var rt = clockFrame.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, 0.855f);
            rt.anchorMax = new Vector2(0.88f, 0.955f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Re-align child elements inside astrolabe frame
            var timerTr = clockFrame.Find("TimerTMP");
            if (timerTr != null)
            {
                var trt = timerTr.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.12f, 0.42f);
                trt.anchorMax = new Vector2(0.36f, 0.80f);
                var tmp = timerTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 18;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(1.0f, 0.92f, 0.75f); // Ivory gold
                }
            }

            var tierTr = clockFrame.Find("TierBadgeTMP");
            if (tierTr != null)
            {
                var trt = tierTr.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.36f, 0.42f);
                trt.anchorMax = new Vector2(0.68f, 0.80f);
                var tmp = tierTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 15;
                    tmp.fontStyle = FontStyles.Bold;
                }
            }

            var coeffTr = clockFrame.Find("CoeffTMP");
            if (coeffTr != null)
            {
                var trt = coeffTr.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.68f, 0.42f);
                trt.anchorMax = new Vector2(0.88f, 0.80f);
                var tmp = coeffTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 13;
                    tmp.color = new Color(1f, 0.80f, 0.35f);
                }
            }
        }

        // Tier Progress Slider (Aligned to the lower hour-glass alchemical tube)
        var sliderTr = hudTr.Find("TierProgressSlider");
        if (sliderTr != null)
        {
            var srt = sliderTr.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.34f, 0.852f);
            srt.anchorMax = new Vector2(0.66f, 0.865f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
        }

        // Active Boons Strip
        var boonsTr = hudTr.Find("ActiveBoonsContainer");
        if (boonsTr != null)
        {
            var brt = boonsTr.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.04f, 0.805f);
            brt.anchorMax = new Vector2(0.96f, 0.845f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
        }
    }

    private static void UpgradeBoonRewardUI(Transform canvasParent)
    {
        var modalTr = canvasParent.Find("DungeonBoonRewardModal");
        if (modalTr == null) return;

        Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TEXTURE_DIR + "dark_academia_boon_card.jpg");
        var container = modalTr.Find("CardsContainer");
        if (container == null) return;

        for (int i = 1; i <= 3; i++)
        {
            var cardTr = container.Find($"BoonCard_{i}");
            if (cardTr == null) continue;

            // Apply Dark Academia Tarot Card Frame
            var bgImg = cardTr.GetComponent<Image>();
            if (bgImg != null && cardSprite != null)
            {
                bgImg.sprite = cardSprite;
                bgImg.color = Color.white;
            }

            // Remove opaque border overlay so card art shines through
            var borderTr = cardTr.Find("BorderImg");
            if (borderTr != null)
            {
                var bImg = borderTr.GetComponent<Image>();
                if (bImg != null) bImg.color = Color.clear;
            }

            // Align Title inside parchment aperture
            var titleTr = cardTr.Find("TitleTMP");
            if (titleTr != null)
            {
                var trt = titleTr.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.24f, 0.65f);
                trt.anchorMax = new Vector2(0.76f, 0.73f);
                var tmp = titleTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 14;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.22f, 0.16f, 0.10f); // Dark ink brown on parchment
                }
            }

            // Align Rarity Tag
            var rarityTr = cardTr.Find("RarityTMP");
            if (rarityTr != null)
            {
                var trt = rarityTr.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.25f, 0.73f);
                trt.anchorMax = new Vector2(0.75f, 0.79f);
                var tmp = rarityTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 11;
                    tmp.fontStyle = FontStyles.Bold;
                }
            }

            // Align Icon inside upper aperture
            var iconTr = cardTr.Find("IconFrame");
            if (iconTr != null)
            {
                var irt = iconTr.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.36f, 0.48f);
                irt.anchorMax = new Vector2(0.64f, 0.64f);
                var ifBg = iconTr.GetComponent<Image>();
                if (ifBg != null) ifBg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);
            }

            // Align Description inside lower parchment body
            var descTr = cardTr.Find("DescTMP");
            if (descTr != null)
            {
                var trt = descTr.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.24f, 0.28f);
                trt.anchorMax = new Vector2(0.76f, 0.46f);
                var tmp = descTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 11;
                    tmp.color = new Color(0.25f, 0.18f, 0.12f); // Dark academia sepia ink
                }
            }

            // Button styling: Antique Brass / Wax Seal Action Button
            var btnTr = cardTr.Find("SelectBtn");
            if (btnTr != null)
            {
                var brt = btnTr.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.22f, 0.18f);
                brt.anchorMax = new Vector2(0.78f, 0.26f);
                var bImg = btnTr.GetComponent<Image>();
                if (bImg != null)
                {
                    bImg.color = new Color(0.65f, 0.18f, 0.18f, 0.95f); // Crimson wax seal button
                }
            }
        }
    }
}

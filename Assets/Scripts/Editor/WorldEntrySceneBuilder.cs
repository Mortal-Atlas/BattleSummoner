using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using BattleSummoner.Intro;
using BattleSummoner.Audio;

namespace BattleSummoner.EditorScripts
{
    public static class WorldEntrySceneBuilder
    {
        [MenuItem("BattleSummoner/Build World Entry Scene")]
        public static string BuildWorldEntryScene()
        {
            string scenePath = "Assets/Scenes/WorldEntryExperience.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Scene Lighting & Environment
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.02f, 0.04f, 0.08f);

            var dirLightGo = new GameObject("Directional_RimLight");
            var dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.35f, 0.70f, 1.0f);
            dirLight.intensity = 0.65f;
            dirLightGo.transform.rotation = Quaternion.Euler(45f, 140f, 0f);

            // 2. Main Camera & Audio Listener
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.04f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 120f;
            camGo.transform.position = new Vector3(0f, 0f, 0f);
            camGo.transform.rotation = Quaternion.identity;
            camGo.AddComponent<AudioListener>();

            // Audio Synthesizer
            var audioGo = new GameObject("AudioSynthesizer");
            var audioSynth = audioGo.AddComponent<LoginAudioSynthesizer>();

            // Event System
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            // ==========================================
            // STAGE 1: SWORD ART ONLINE WARP TUNNEL
            // WITH TESSELLATING HEXAGONAL LIGHT PILLARS
            // ==========================================
            var saoWarpRoot = new GameObject("Stage_1_SAO_WarpTunnel");
            saoWarpRoot.transform.position = Vector3.zero;

            // 1.1 Tessellating Hexagon Pillar System
            var hexTunnelGo = new GameObject("Tessellating_Hexagon_Pillars");
            hexTunnelGo.transform.SetParent(saoWarpRoot.transform, false);
            var hexTunnel = hexTunnelGo.AddComponent<SAOHexagonPillarTunnel>();
            hexTunnel.hexRadius = 0.55f;
            hexTunnel.innerClearanceRadius = 1.35f;
            hexTunnel.outerRadius = 7.2f;
            hexTunnel.tunnelLength = 85.0f;
            hexTunnel.minPillarLength = 8.0f;
            hexTunnel.maxPillarLength = 26.0f;
            hexTunnel.gridSpan = 6;
            hexTunnel.currentSpeed = 55.0f;

            // Material for Tessellating Hexagon Pillars
            var hexShader = Shader.Find("BattleSummoner/SAO_TessellatingHexagonPillars");
            var hexMat = new Material(hexShader != null ? hexShader : Shader.Find("Universal Render Pipeline/Unlit"));
            hexMat.name = "M_SAO_TessellatingHexagonPillars";
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateAsset(hexMat, "Assets/Materials/M_SAO_TessellatingHexagonPillars.mat");
            hexTunnel.pillarMaterial = hexMat;

            // Build the pillars
            hexTunnel.EnsureHexMesh();
            hexTunnel.BuildHoneycombTunnel();

            // 1.2 Particle light streaks zooming towards camera along hexagon tunnel corridor
            var psGo = new GameObject("SAO_LightStreak_Particles");
            psGo.transform.SetParent(saoWarpRoot.transform, false);
            psGo.transform.localPosition = new Vector3(0f, 0f, 40.0f);
            psGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startSpeed = 45.0f;
            main.startLifetime = 1.0f;
            main.startSize = 0.08f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.85f, 1f), new Color(0.95f, 0.3f, 0.85f));
            main.maxParticles = 600;
            var emission = ps.emission;
            emission.rateOverTime = 280f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2.4f;

            // 1.4 SAO HUD Telemetry Banner Canvas
            var saoHudCanvasGo = new GameObject("SAO_WarpHUD_Canvas");
            saoHudCanvasGo.transform.SetParent(saoWarpRoot.transform, false);
            var saoCanvas = saoHudCanvasGo.AddComponent<Canvas>();
            saoCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            saoCanvas.worldCamera = cam;
            saoCanvas.planeDistance = 2.0f;
            var saoScaler = saoHudCanvasGo.AddComponent<CanvasScaler>();
            saoScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            saoScaler.referenceResolution = new Vector2(1080, 1920);
            var hudCg = saoHudCanvasGo.AddComponent<CanvasGroup>();

            var hudTextGo = new GameObject("HUD_WarpBannerText");
            hudTextGo.transform.SetParent(saoHudCanvasGo.transform, false);
            var hudTextRt = hudTextGo.AddComponent<RectTransform>();
            hudTextRt.anchorMin = new Vector2(0.5f, 0.5f);
            hudTextRt.anchorMax = new Vector2(0.5f, 0.5f);
            hudTextRt.sizeDelta = new Vector2(800, 90);
            var hudTmp = hudTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) hudTmp.font = fontAsset;
            hudTmp.text = "✦ LINK START! ✦";
            hudTmp.fontSize = 36f;
            hudTmp.color = new Color(0.38f, 0.85f, 1.0f);
            hudTmp.alignment = TextAlignmentOptions.Center;

            var warpController = saoWarpRoot.AddComponent<SAOWarpTunnelController>();
            warpController.hexagonPillarTunnel = hexTunnel;
            warpController.tunnelRenderer = null;
            warpController.lightStreakParticles = ps;
            warpController.hudTelemetryBanners = new TextMeshProUGUI[] { hudTmp };
            warpController.hudCanvasGroup = hudCg;
            warpController.mainCamera = cam;
            warpController.tunnelDuration = 4.0f;
            warpController.peakSpeed = 90.0f;
            warpController.baseSpeed = 25.0f;

            // ==========================================
            // STAGE 2: SAO ACCOUNT INDUCTION MATRIX
            // ==========================================
            var inductionRoot = new GameObject("Stage_2_AccountInduction");
            inductionRoot.transform.position = Vector3.zero;

            // Floating Hologram Dais
            var daisGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            daisGo.name = "Hologram_Dais";
            daisGo.transform.SetParent(inductionRoot.transform, false);
            daisGo.transform.localPosition = new Vector3(0.35f, -0.65f, 2.3f);
            daisGo.transform.localScale = new Vector3(1.6f, 0.05f, 1.6f);
            var daisMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            daisMat.color = new Color(0.10f, 0.14f, 0.22f);
            daisGo.GetComponent<Renderer>().material = daisMat;
            UnityEngine.Object.DestroyImmediate(daisGo.GetComponent<Collider>());

            // Hologram Light
            var holoLightGo = new GameObject("HoloSpotlight");
            holoLightGo.transform.SetParent(daisGo.transform, false);
            holoLightGo.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            holoLightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var holoLight = holoLightGo.AddComponent<Light>();
            holoLight.type = LightType.Spot;
            holoLight.spotAngle = 48f;
            holoLight.color = new Color(0.4f, 0.85f, 1.0f);
            holoLight.intensity = 3.2f;
            holoLight.range = 5.0f;

            // Avatar Key Light for clean anime cel shading
            var keyLightGo = new GameObject("Avatar_KeyLight");
            keyLightGo.transform.SetParent(inductionRoot.transform, false);
            keyLightGo.transform.localPosition = new Vector3(-0.8f, 0.4f, 1.2f);
            keyLightGo.transform.localRotation = Quaternion.Euler(20f, 40f, 0f);
            var keyLight = keyLightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.95f, 0.90f);
            keyLight.intensity = 1.6f;

            // 3D Avatar Preview Instances (Male & Female)
            var avatarRoot = new GameObject("Avatar_Turntable_Root");
            avatarRoot.transform.SetParent(inductionRoot.transform, false);
            avatarRoot.transform.localPosition = new Vector3(0.35f, -0.60f, 2.3f);

            var malePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Base_Male_Genshin.prefab");
            var femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Base_Female_Genshin.prefab");

            GameObject maleInstance = null, femaleInstance = null;
            if (malePrefab != null)
            {
                maleInstance = (GameObject)PrefabUtility.InstantiatePrefab(malePrefab, avatarRoot.transform);
                maleInstance.name = "Hologram_Male";
                maleInstance.transform.localPosition = Vector3.zero;
                maleInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                // Strip complex gameplay scripts for lightweight preview
                UnityEngine.Object.DestroyImmediate(maleInstance.GetComponent<global::ModularEquipmentManager>());
                UnityEngine.Object.DestroyImmediate(maleInstance.GetComponent<global::SimsBodyMorphController>());
            }
            if (femalePrefab != null)
            {
                femaleInstance = (GameObject)PrefabUtility.InstantiatePrefab(femalePrefab, avatarRoot.transform);
                femaleInstance.name = "Hologram_Female";
                femaleInstance.transform.localPosition = Vector3.zero;
                femaleInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                UnityEngine.Object.DestroyImmediate(femaleInstance.GetComponent<global::ModularEquipmentManager>());
                UnityEngine.Object.DestroyImmediate(femaleInstance.GetComponent<global::SimsBodyMorphController>());
                femaleInstance.SetActive(false);
            }

            // Induction Holographic UI Canvas
            var uiCanvasGo = new GameObject("AccountInductionCanvas");
            uiCanvasGo.transform.SetParent(inductionRoot.transform, false);
            var uiCanvas = uiCanvasGo.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            uiCanvas.worldCamera = cam;
            uiCanvas.planeDistance = 2.0f;
            var uiScaler = uiCanvasGo.AddComponent<CanvasScaler>();
            uiScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            uiScaler.referenceResolution = new Vector2(1080, 1920);
            uiScaler.matchWidthOrHeight = 0f;
            uiCanvasGo.AddComponent<GraphicRaycaster>();

            // Frosted Left Cyber Panel (Width 430 on 1080 screen)
            var panelGo = new GameObject("Cyber_Induction_Panel");
            panelGo.transform.SetParent(uiCanvasGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 0.5f);
            panelRt.anchoredPosition = new Vector2(30f, 0f);
            panelRt.sizeDelta = new Vector2(430f, -60f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.06f, 0.09f, 0.15f, 0.92f);

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.92f);
            titleRt.anchorMax = new Vector2(0.95f, 0.98f);
            titleRt.sizeDelta = Vector2.zero;
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) titleTmp.font = fontAsset;
            titleTmp.text = "<b><color=#38BDF8>✦ NEURAL INDUCTION ✦</color></b>\n<size=11><color=#94A3B8>SWORD ART ONLINE • FIRST LOGIN</color></size>";
            titleTmp.fontSize = 17f;
            titleTmp.alignment = TextAlignmentOptions.TopLeft;

            // Moniker Input Field
            var inputGo = new GameObject("Input_Moniker");
            inputGo.transform.SetParent(panelGo.transform, false);
            var inputRt = inputGo.AddComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.05f, 0.83f);
            inputRt.anchorMax = new Vector2(0.95f, 0.89f);
            inputRt.sizeDelta = Vector2.zero;
            var inputImg = inputGo.AddComponent<Image>();
            inputImg.color = new Color(0.10f, 0.16f, 0.25f);
            var tmpInput = inputGo.AddComponent<TMP_InputField>();

            var textAreaGo = new GameObject("Text Area");
            textAreaGo.transform.SetParent(inputGo.transform, false);
            var textAreaRt = textAreaGo.AddComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.sizeDelta = new Vector2(-16, 0);

            var inTxtGo = new GameObject("Text");
            inTxtGo.transform.SetParent(textAreaGo.transform, false);
            var inTxtRt = inTxtGo.AddComponent<RectTransform>();
            inTxtRt.anchorMin = Vector2.zero;
            inTxtRt.anchorMax = Vector2.one;
            inTxtRt.sizeDelta = Vector2.zero;
            var inTmp = inTxtGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) inTmp.font = fontAsset;
            inTmp.fontSize = 15f;
            inTmp.color = Color.white;
            inTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var phTxtGo = new GameObject("Placeholder");
            phTxtGo.transform.SetParent(textAreaGo.transform, false);
            var phTxtRt = phTxtGo.AddComponent<RectTransform>();
            phTxtRt.anchorMin = Vector2.zero;
            phTxtRt.anchorMax = Vector2.one;
            phTxtRt.sizeDelta = Vector2.zero;
            var phTmp = phTxtGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) phTmp.font = fontAsset;
            phTmp.text = "Enter Summoner Moniker...";
            phTmp.fontSize = 15f;
            phTmp.color = new Color(0.6f, 0.7f, 0.85f, 0.5f);
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;

            tmpInput.textViewport = textAreaRt;
            tmpInput.textComponent = inTmp;
            tmpInput.placeholder = phTmp;

            // Archetype Select Buttons
            Button CreateBtn(string name, Transform parent, string label, Color bg, Color txtCol, Vector2 min, Vector2 max)
            {
                var bGo = new GameObject(name);
                bGo.transform.SetParent(parent, false);
                var bRt = bGo.AddComponent<RectTransform>();
                bRt.anchorMin = min;
                bRt.anchorMax = max;
                bRt.sizeDelta = Vector2.zero;
                var bImg = bGo.AddComponent<Image>();
                bImg.color = bg;
                var btn = bGo.AddComponent<Button>();
                var colors = btn.colors;
                colors.normalColor = bg;
                colors.highlightedColor = bg * 1.3f;
                btn.colors = colors;

                var tGo = new GameObject("Text");
                tGo.transform.SetParent(bGo.transform, false);
                var tRt = tGo.AddComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.sizeDelta = Vector2.zero;
                var tTmp = tGo.AddComponent<TextMeshProUGUI>();
                if (fontAsset != null) tTmp.font = fontAsset;
                tTmp.text = label;
                tTmp.fontSize = 12f;
                tTmp.color = txtCol;
                tTmp.alignment = TextAlignmentOptions.Center;
                return btn;
            }

            var btnM = CreateBtn("Btn_Male", panelGo.transform, "HEROIC ATHLETE (M)", new Color(0.18f, 0.45f, 0.70f), Color.white, new Vector2(0.05f, 0.74f), new Vector2(0.48f, 0.80f));
            var btnF = CreateBtn("Btn_Female", panelGo.transform, "GRACEFUL MAGE (F)", new Color(0.10f, 0.15f, 0.22f), Color.white, new Vector2(0.52f, 0.74f), new Vector2(0.95f, 0.80f));

            // Archetype Description
            var descGo = new GameObject("Archetype_Desc");
            descGo.transform.SetParent(panelGo.transform, false);
            var descRt = descGo.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.05f, 0.65f);
            descRt.anchorMax = new Vector2(0.95f, 0.72f);
            descRt.sizeDelta = Vector2.zero;
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) descTmp.font = fontAsset;
            descTmp.fontSize = 11f;
            descTmp.color = new Color(0.85f, 0.90f, 0.95f);
            descTmp.alignment = TextAlignmentOptions.TopLeft;

            // Affinity Row
            var affLblGo = new GameObject("Affinity_Label");
            affLblGo.transform.SetParent(panelGo.transform, false);
            var affLblRt = affLblGo.AddComponent<RectTransform>();
            affLblRt.anchorMin = new Vector2(0.05f, 0.60f);
            affLblRt.anchorMax = new Vector2(0.95f, 0.64f);
            affLblRt.sizeDelta = Vector2.zero;
            var affLblTmp = affLblGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) affLblTmp.font = fontAsset;
            affLblTmp.text = "<color=#94A3B8>DIMENSIONAL AFFINITY:</color>";
            affLblTmp.fontSize = 11f;

            var btnSolar = CreateBtn("Btn_Solar", panelGo.transform, "☀️ SOLAR", new Color(0.35f, 0.28f, 0.08f), new Color(1f, 0.85f, 0.3f), new Vector2(0.05f, 0.54f), new Vector2(0.26f, 0.59f));
            var btnLunar = CreateBtn("Btn_Lunar", panelGo.transform, "🌙 LUNAR", new Color(0.12f, 0.20f, 0.35f), new Color(0.5f, 0.8f, 1f), new Vector2(0.28f, 0.54f), new Vector2(0.49f, 0.59f));
            var btnAstral = CreateBtn("Btn_Astral", panelGo.transform, "⚡ ASTRAL", new Color(0.28f, 0.12f, 0.35f), new Color(0.9f, 0.5f, 1f), new Vector2(0.51f, 0.54f), new Vector2(0.72f, 0.59f));
            var btnVoid = CreateBtn("Btn_Void", panelGo.transform, "🌌 VOID", new Color(0.15f, 0.15f, 0.20f), new Color(0.7f, 0.95f, 0.8f), new Vector2(0.74f, 0.54f), new Vector2(0.95f, 0.59f));

            // Telemetry Preview Card
            var telemGo = new GameObject("Telemetry_Card");
            telemGo.transform.SetParent(panelGo.transform, false);
            var telemRt = telemGo.AddComponent<RectTransform>();
            telemRt.anchorMin = new Vector2(0.05f, 0.16f);
            telemRt.anchorMax = new Vector2(0.95f, 0.51f);
            telemRt.sizeDelta = Vector2.zero;
            var telemImg = telemGo.AddComponent<Image>();
            telemImg.color = new Color(0.03f, 0.05f, 0.09f, 0.90f);

            var telemTxtGo = new GameObject("Telemetry_Text");
            telemTxtGo.transform.SetParent(telemGo.transform, false);
            var telemTxtRt = telemTxtGo.AddComponent<RectTransform>();
            telemTxtRt.anchorMin = new Vector2(0.04f, 0.04f);
            telemTxtRt.anchorMax = new Vector2(0.96f, 0.96f);
            telemTxtRt.sizeDelta = Vector2.zero;
            var telemTmp = telemTxtGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) telemTmp.font = fontAsset;
            telemTmp.fontSize = 11.5f;
            telemTmp.color = Color.white;
            telemTmp.alignment = TextAlignmentOptions.TopLeft;

            // LINK START Button
            var btnLink = CreateBtn("Btn_LinkStart", panelGo.transform, "<b><size=15>✦ LINK START ✦</size></b>\n<size=10>DIVE INTO THE HUNDREDFOLD SPIRE</size>", new Color(0.12f, 0.55f, 0.40f), Color.white, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.13f));

            var accountUI = inductionRoot.AddComponent<AccountInductionUI>();
            accountUI.monikerInput = tmpInput;
            accountUI.btnSelectMale = btnM;
            accountUI.btnSelectFemale = btnF;
            accountUI.archetypeDescText = descTmp;
            accountUI.telemetryPreviewText = telemTmp;
            accountUI.btnLinkStart = btnLink;
            accountUI.maleHologramInstance = maleInstance;
            accountUI.femaleHologramInstance = femaleInstance;
            accountUI.avatarStageRoot = avatarRoot.transform;

            btnSolar.onClick.AddListener(() => accountUI.SetAffinity("SOLAR_DAWN"));
            btnLunar.onClick.AddListener(() => accountUI.SetAffinity("LUNAR_MIST"));
            btnAstral.onClick.AddListener(() => accountUI.SetAffinity("ASTRAL_SPIRE"));
            btnVoid.onClick.AddListener(() => accountUI.SetAffinity("VOID_SPARK"));

            // ==========================================
            // MASTER MANAGER
            // ==========================================
            var managerGo = new GameObject("WorldEntrySequenceManager");
            var manager = managerGo.AddComponent<WorldEntrySequenceManager>();
            manager.saoWarpTunnelRoot = saoWarpRoot;
            manager.saoAccountInductionRoot = inductionRoot;
            manager.saoWarpTunnelController = warpController;
            manager.accountInductionUI = accountUI;
            manager.targetWorldSceneName = "CharacterEquipmentShowcase";

            // Set initial stage visibility (Warp Tunnel starts immediately, Induction opens after warp)
            saoWarpRoot.SetActive(true);
            inductionRoot.SetActive(false);

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);

            // Register in Build Settings as Scene 0
            var originalScenes = EditorBuildSettings.scenes;
            bool alreadyInBuild = false;
            foreach (var s in originalScenes)
            {
                if (s.path == scenePath) { alreadyInBuild = true; break; }
            }
            if (!alreadyInBuild)
            {
                var newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
                for (int i = 0; i < originalScenes.Length; i++) newScenes[i + 1] = originalScenes[i];
                EditorBuildSettings.scenes = newScenes;
            }

            return $"Successfully built and saved World Entry Scene at '{scenePath}' with SAO Tessellating Hexagon Light Pillars!";
        }
    }
}

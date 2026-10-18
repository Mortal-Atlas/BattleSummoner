using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using BattleSummoner.Intro;
using BattleSummoner.Customization;
using BattleSummoner.Audio;

namespace BattleSummoner.EditorScripts
{
    public static class WorldIntroductionCutsceneBuilder
    {
        [MenuItem("BattleSummoner/Build World Introduction Cutscene Scene", priority = 111)]
        public static string BuildStandaloneCutsceneScene()
        {
            string scenePath = "Assets/Scenes/WorldIntroductionCutscene.unity";

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting & Environment
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.16f, 0.25f);

            var sunGo = new GameObject("Celestial_Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.95f, 0.92f, 0.85f);
            sun.intensity = 1.4f;
            sunGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            var rimGo = new GameObject("Spire_RimLight");
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.35f, 0.65f, 1.0f);
            rim.intensity = 0.8f;
            rimGo.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

            var keyGo = new GameObject("Facial_Key_Light");
            var keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.96f, 0.92f);
            keyLight.intensity = 1.35f;
            keyGo.transform.rotation = Quaternion.Euler(12f, 175f, 0f);

            // Audio Synthesizer
            var audioGo = new GameObject("AudioSynthesizer");
            audioGo.AddComponent<LoginAudioSynthesizer>();

            // Event System
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Dais / Platform
            var daisGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            daisGo.name = "Arrival_Dais_Platform";
            daisGo.transform.position = new Vector3(0f, -0.05f, 0f);
            daisGo.transform.localScale = new Vector3(3.5f, 0.1f, 3.5f);
            var daisMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            daisMat.color = new Color(0.15f, 0.18f, 0.24f);
            daisGo.GetComponent<Renderer>().material = daisMat;

            // Spire Rune Circle
            var runeGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeGo.name = "Spire_Rune_Circle";
            runeGo.transform.position = new Vector3(0f, 0.01f, 0f);
            runeGo.transform.localScale = new Vector3(2.6f, 0.02f, 2.6f);
            var runeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            runeMat.color = new Color(0.20f, 0.50f, 0.75f);
            runeGo.GetComponent<Renderer>().material = runeMat;

            // Arrival Spotlight
            var spotGo = new GameObject("Arrival_Light_Pillar");
            spotGo.transform.position = new Vector3(0f, 6.0f, 0f);
            spotGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 40f;
            spot.color = new Color(0.55f, 0.88f, 1.0f);
            spot.intensity = 4.5f;
            spot.range = 10f;

            // Particles
            var psGo = new GameObject("Arrival_Beams_Particles");
            psGo.transform.position = new Vector3(0f, 0.1f, 0f);
            var ps = psGo.AddComponent<ParticleSystem>();
            var pmain = ps.main;
            pmain.startLifetime = 1.2f;
            pmain.startSpeed = 3.5f;
            pmain.startSize = 0.08f;
            pmain.startColor = new Color(0.4f, 0.85f, 1.0f, 0.7f);
            pmain.maxParticles = 80;
            var pshape = ps.shape;
            pshape.shapeType = ParticleSystemShapeType.Circle;
            pshape.radius = 1.2f;

            // Spawn Character Instance
            var malePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Base_Male_Genshin.prefab");
            var femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Base_Female_Genshin.prefab");

            GameObject activeChar = null;
            if (malePrefab != null)
            {
                activeChar = (GameObject)PrefabUtility.InstantiatePrefab(malePrefab);
                activeChar.name = "Customized_Hero_Player";
                activeChar.transform.position = Vector3.zero;
                activeChar.transform.rotation = Quaternion.identity;
            }

            // Cinematic Camera
            var camGo = new GameObject("Cinematic_Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            camGo.transform.position = new Vector3(0f, 1.55f, 0.72f);
            camGo.transform.LookAt(new Vector3(0f, 1.55f, 0f));
            camGo.AddComponent<AudioListener>();

            // Font Asset
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            // Cutscene UI Canvas
            var canvasGo = new GameObject("Cutscene_Cinematic_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 2.0f;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Letterbox Bars
            var letterboxGo = new GameObject("Letterbox_Bars");
            letterboxGo.transform.SetParent(canvasGo.transform, false);
            var lbCg = letterboxGo.AddComponent<CanvasGroup>();

            var topBar = new GameObject("Top_Bar");
            topBar.transform.SetParent(letterboxGo.transform, false);
            var topRt = topBar.AddComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 0.88f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.sizeDelta = Vector2.zero;
            var topImg = topBar.AddComponent<Image>();
            topImg.color = Color.black;

            var botBar = new GameObject("Bottom_Bar");
            botBar.transform.SetParent(letterboxGo.transform, false);
            var botRt = botBar.AddComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0f, 0f);
            botRt.anchorMax = new Vector2(1f, 0.12f);
            botRt.sizeDelta = Vector2.zero;
            var botImg = botBar.AddComponent<Image>();
            botImg.color = Color.black;

            // Subtitle Box
            var subBoxGo = new GameObject("Subtitle_Panel");
            subBoxGo.transform.SetParent(canvasGo.transform, false);
            var subBoxRt = subBoxGo.AddComponent<RectTransform>();
            subBoxRt.anchorMin = new Vector2(0.08f, 0.14f);
            subBoxRt.anchorMax = new Vector2(0.92f, 0.25f);
            subBoxRt.sizeDelta = Vector2.zero;
            var subBoxImg = subBoxGo.AddComponent<Image>();
            subBoxImg.color = new Color(0.06f, 0.09f, 0.15f, 0.90f);

            var spkGo = new GameObject("Speaker_Name");
            spkGo.transform.SetParent(subBoxGo.transform, false);
            var spkRt = spkGo.AddComponent<RectTransform>();
            spkRt.anchorMin = new Vector2(0.04f, 0.72f);
            spkRt.anchorMax = new Vector2(0.96f, 0.96f);
            spkRt.sizeDelta = Vector2.zero;
            var spkTmp = spkGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) spkTmp.font = fontAsset;
            spkTmp.fontSize = 15f;
            spkTmp.text = "<b><color=#FACC15>SUMMONER</color></b>";

            var dlgGo = new GameObject("Dialogue_Text");
            dlgGo.transform.SetParent(subBoxGo.transform, false);
            var dlgRt = dlgGo.AddComponent<RectTransform>();
            dlgRt.anchorMin = new Vector2(0.04f, 0.08f);
            dlgRt.anchorMax = new Vector2(0.96f, 0.70f);
            dlgRt.sizeDelta = Vector2.zero;
            var dlgTmp = dlgGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) dlgTmp.font = fontAsset;
            dlgTmp.fontSize = 17f;
            dlgTmp.color = Color.white;
            dlgTmp.text = "System link established... Where... am I?";

            // Title Card
            var cardGo = new GameObject("Title_Card_Panel");
            cardGo.transform.SetParent(canvasGo.transform, false);
            var cardRt = cardGo.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.10f, 0.68f);
            cardRt.anchorMax = new Vector2(0.90f, 0.82f);
            cardRt.sizeDelta = Vector2.zero;
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.05f, 0.08f, 0.14f, 0.88f);

            var cardHeadGo = new GameObject("Card_Header");
            cardHeadGo.transform.SetParent(cardGo.transform, false);
            var cardHeadRt = cardHeadGo.AddComponent<RectTransform>();
            cardHeadRt.anchorMin = new Vector2(0.05f, 0.50f);
            cardHeadRt.anchorMax = new Vector2(0.95f, 0.92f);
            cardHeadRt.sizeDelta = Vector2.zero;
            var cardHeadTmp = cardHeadGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) cardHeadTmp.font = fontAsset;
            cardHeadTmp.fontSize = 24f;
            cardHeadTmp.alignment = TextAlignmentOptions.Center;
            cardHeadTmp.text = "<b><color=#FACC15>✦ SUMMONER READY ✦</color></b>";

            var cardSubGo = new GameObject("Card_Sub");
            cardSubGo.transform.SetParent(cardGo.transform, false);
            var cardSubRt = cardSubGo.AddComponent<RectTransform>();
            cardSubRt.anchorMin = new Vector2(0.05f, 0.08f);
            cardSubRt.anchorMax = new Vector2(0.95f, 0.48f);
            cardSubRt.sizeDelta = Vector2.zero;
            var cardSubTmp = cardSubGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) cardSubTmp.font = fontAsset;
            cardSubTmp.fontSize = 12f;
            cardSubTmp.alignment = TextAlignmentOptions.Center;
            cardSubTmp.text = "<color=#38BDF8>DIMENSIONAL AFFINITY: SOLAR_DAWN</color> • <color=#4ADE80>NEURAL LINK 100%</color>";

            // Skip Button
            var skipGo = new GameObject("Btn_SkipCutscene");
            skipGo.transform.SetParent(canvasGo.transform, false);
            var skipRt = skipGo.AddComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(0.70f, 0.92f);
            skipRt.anchorMax = new Vector2(0.96f, 0.96f);
            skipRt.sizeDelta = Vector2.zero;
            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(0.12f, 0.16f, 0.22f, 0.85f);
            var skipBtn = skipGo.AddComponent<Button>();

            var skipTxtGo = new GameObject("Skip_Text");
            skipTxtGo.transform.SetParent(skipGo.transform, false);
            var skipTxtRt = skipTxtGo.AddComponent<RectTransform>();
            skipTxtRt.anchorMin = Vector2.zero;
            skipTxtRt.anchorMax = Vector2.one;
            skipTxtRt.sizeDelta = Vector2.zero;
            var skipTmp = skipTxtGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) skipTmp.font = fontAsset;
            skipTmp.fontSize = 11.5f;
            skipTmp.alignment = TextAlignmentOptions.Center;
            skipTmp.text = "✦ SKIP [TAP] ✦";

            // Replay Button
            var repGo = new GameObject("Btn_ReplayCutscene");
            repGo.transform.SetParent(canvasGo.transform, false);
            var repRt = repGo.AddComponent<RectTransform>();
            repRt.anchorMin = new Vector2(0.04f, 0.92f);
            repRt.anchorMax = new Vector2(0.30f, 0.96f);
            repRt.sizeDelta = Vector2.zero;
            var repImg = repGo.AddComponent<Image>();
            repImg.color = new Color(0.15f, 0.50f, 0.35f, 0.85f);
            var repBtn = repGo.AddComponent<Button>();

            var repTxtGo = new GameObject("Replay_Text");
            repTxtGo.transform.SetParent(repGo.transform, false);
            var repTxtRt = repTxtGo.AddComponent<RectTransform>();
            repTxtRt.anchorMin = Vector2.zero;
            repTxtRt.anchorMax = Vector2.one;
            repTxtRt.sizeDelta = Vector2.zero;
            var repTmp = repTxtGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) repTmp.font = fontAsset;
            repTmp.fontSize = 11.5f;
            repTmp.alignment = TextAlignmentOptions.Center;
            repTmp.text = "✦ REPLAY CUTSCENE ✦";

            // Cutscene Director Component
            var directorGo = new GameObject("WorldIntroductionCutsceneDirector");
            var director = directorGo.AddComponent<WorldIntroductionCutsceneDirector>();
            director.cutsceneCamera = cam;
            director.malePrefab = malePrefab;
            director.femalePrefab = femalePrefab;
            director.helmetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Helmet.prefab");
            director.torsoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Torso.prefab");
            director.leggingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Leggings.prefab");
            director.shoesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Equipment_Shoes.prefab");
            director.swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Sword.prefab");
            director.battleAxePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_TwoHanded_BattleAxe.prefab");
            director.staffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Staff.prefab");
            director.wandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Weapon_Wand.prefab");
            director.activeCharacterInstance = activeChar;
            director.facialController = activeChar != null ? activeChar.GetComponent<FacialExpressionController>() : null;
            director.descentBeamParticles = ps;
            director.arrivalSpotlight = spot;
            director.cutsceneCanvas = canvas;
            director.letterboxBarsGroup = lbCg;
            director.subtitlePanel = subBoxGo;
            director.speakerNameText = spkTmp;
            director.dialogueText = dlgTmp;
            director.titleCardPanel = cardGo;
            director.titleCardHeader = cardHeadTmp;
            director.titleCardSub = cardSubTmp;
            director.skipButton = skipBtn;
            director.replayButton = repBtn;
            director.autoPlayOnStart = true;

            repBtn.onClick.AddListener(() => director.PlayIntroductionCutscene(activeChar));

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);

            // Register in Build Settings
            var originalScenes = EditorBuildSettings.scenes;
            bool exists = false;
            foreach (var s in originalScenes)
            {
                if (s.path == scenePath) { exists = true; break; }
            }
            if (!exists)
            {
                var newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                for (int i = 0; i < originalScenes.Length; i++) newScenes[i] = originalScenes[i];
                newScenes[originalScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
            }

            Debug.Log($"[CUTSCENE BUILDER] Successfully built World Introduction Cutscene scene at '{scenePath}'.");
            return scenePath;
        }
    }
}

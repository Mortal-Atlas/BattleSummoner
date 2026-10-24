using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using BattleSummoner.Dialogue;
using BattleSummoner.Environment;

namespace BattleSummoner.EditorScripts
{
    public static class CampGroupChatSetup
    {
        private const string CampGroupChatScenePath = "Assets/Scenes/CampGroupChat.unity";
        private const string MultiCharacterScenePath = "Assets/Scenes/MultiCharacterDialogueHub.unity";

        [MenuItem("BattleSummoner/Camp Group Chat/Build and Setup Camp Group Chat Scene", false, 10)]
        public static void BuildAndSetupCampGroupChatScene()
        {
            Debug.Log("================================================================================");
            Debug.Log("🏕 [CAMP GROUP CHAT] BUILDING & REFINING COMPLETE CAMP GROUP CHAT SCENE 🏕");
            Debug.Log("================================================================================");

            // 1. Create or Open Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Setup Directional Light (Indigo Twilight Ambiance)
            GameObject dirLightObj = new GameObject("Directional Light", typeof(Light), typeof(UniversalAdditionalLightData));
            Light dirLight = dirLightObj.GetComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.35f, 0.42f, 0.60f); // Twilight indigo
            dirLight.intensity = 0.45f;
            dirLightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // 3. Campfire Ground Floor
            GameObject stageFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            stageFloor.name = "CampfireFloor";
            stageFloor.transform.position = Vector3.zero;
            stageFloor.transform.localScale = new Vector3(3f, 1f, 3f);
            var floorRend = stageFloor.GetComponent<Renderer>();
            if (floorRend != null)
            {
                floorRend.sharedMaterial = GetOrCreateMaterial("Mat_Campfire_Floor", new Color(0.08f, 0.09f, 0.12f, 1f), 0.05f, 0.35f);
            }
            // Strip static batching flag
            GameObjectUtility.SetStaticEditorFlags(stageFloor, 0);

            // 4. Background Framing Pillars
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * Mathf.PI / 2f) + 0.35f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 12f, 5f, Mathf.Sin(angle) * 12f);
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"BackgroundPillar_{i + 1}";
                pillar.transform.position = pos;
                pillar.transform.localScale = new Vector3(2.0f, 10f, 2.0f);
                var pRend = pillar.GetComponent<Renderer>();
                if (pRend != null)
                {
                    pRend.sharedMaterial = GetOrCreateMaterial("Mat_Cavern_Pillars", new Color(0.12f, 0.14f, 0.18f, 1f), 0.35f, 0.55f);
                }
                GameObjectUtility.SetStaticEditorFlags(pillar, 0);
            }

            // 5. Central Campfire Hearth
            GameObject hearth = new GameObject("CampfireHearth");
            hearth.transform.position = Vector3.zero;

            // Hearth Stone Ring
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Vector3 stonePos = new Vector3(Mathf.Cos(angle) * 0.9f, 0.15f, Mathf.Sin(angle) * 0.9f);
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = $"HearthStone_{i + 1}";
                stone.transform.SetParent(hearth.transform, false);
                stone.transform.localPosition = stonePos;
                stone.transform.localScale = new Vector3(0.4f, 0.28f, 0.45f);
                stone.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), i * 45f, UnityEngine.Random.Range(-10f, 10f));
                var sRend = stone.GetComponent<Renderer>();
                if (sRend != null)
                {
                    sRend.sharedMaterial = GetOrCreateMaterial("Mat_Hearth_Stones", new Color(0.18f, 0.19f, 0.22f, 1f), 0.1f, 0.4f);
                }
                GameObjectUtility.SetStaticEditorFlags(stone, 0);
            }

            // Embers center
            GameObject embers = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            embers.name = "CampfireEmbers";
            embers.transform.SetParent(hearth.transform, false);
            embers.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            embers.transform.localScale = new Vector3(1.3f, 0.08f, 1.3f);
            var emberRend = embers.GetComponent<Renderer>();
            if (emberRend != null)
            {
                emberRend.sharedMaterial = GetOrCreateMaterial("Mat_Campfire_Embers", new Color(0.25f, 0.08f, 0.02f, 1f), 0.05f, 0.30f, new Color(1.0f, 0.42f, 0.04f) * 4.2f);
            }
            GameObjectUtility.SetStaticEditorFlags(embers, 0);

            // Campfire Flame Light with AudioSource + CampfireFlickerLight
            GameObject fireLightObj = new GameObject("HearthFireLight", typeof(Light), typeof(UniversalAdditionalLightData), typeof(AudioSource), typeof(CampfireFlickerLight));
            fireLightObj.transform.SetParent(hearth.transform, false);
            fireLightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            Light fireLight = fireLightObj.GetComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1.0f, 0.62f, 0.28f);
            fireLight.intensity = 2.6f;
            fireLight.range = 9.0f;
            fireLight.shadows = LightShadows.Soft;

            AudioSource fireAudio = fireLightObj.GetComponent<AudioSource>();
            fireAudio.spatialBlend = 1.0f;
            fireAudio.minDistance = 2.0f;
            fireAudio.maxDistance = 16.0f;
            fireAudio.loop = true;
            fireAudio.volume = 0.5f;
            fireAudio.playOnAwake = false;

            // Sparks Particle System
            GameObject sparksObj = new GameObject("CampfireSparks", typeof(ParticleSystem));
            sparksObj.transform.SetParent(hearth.transform, false);
            sparksObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            ParticleSystem sparksPs = sparksObj.GetComponent<ParticleSystem>();
            var psMain = sparksPs.main;
            psMain.startLifetime = 2.0f;
            psMain.startSpeed = 1.2f;
            psMain.startSize = 0.07f;
            psMain.startColor = new Color(1f, 0.55f, 0.1f, 0.9f);
            psMain.maxParticles = 60;
            psMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var psEmission = sparksPs.emission;
            psEmission.rateOverTime = 18f;
            var psShape = sparksPs.shape;
            psShape.shapeType = ParticleSystemShapeType.Circle;
            psShape.radius = 0.45f;
            var psVel = sparksPs.velocityOverLifetime;
            psVel.enabled = true;
            psVel.y = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);

            // 6. Cozy Meshy Props
            var propsRoot = new GameObject("CAMPFIRE_MESHY_PROPS");

            // Alchemist Cauldron
            var cauldronPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_AlchemistCauldron_Stylized.prefab");
            if (cauldronPrefab != null)
            {
                var cauldron = (GameObject)PrefabUtility.InstantiatePrefab(cauldronPrefab, propsRoot.transform);
                cauldron.name = "Campfire_Cauldron";
                cauldron.transform.position = new Vector3(0f, 0.05f, 0.35f);
                cauldron.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            }

            // Market Merchant Crates & Baskets (Rations)
            var cratesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
            if (cratesPrefab != null)
            {
                var c1 = (GameObject)PrefabUtility.InstantiatePrefab(cratesPrefab, propsRoot.transform);
                c1.name = "Camp_Supplies_Right";
                c1.transform.position = new Vector3(2.8f, 0f, 1.4f);
                c1.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
                c1.transform.localScale = Vector3.one * 0.9f;

                var c2 = (GameObject)PrefabUtility.InstantiatePrefab(cratesPrefab, propsRoot.transform);
                c2.name = "Camp_Supplies_Left";
                c2.transform.position = new Vector3(-2.8f, 0f, 1.4f);
                c2.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                c2.transform.localScale = Vector3.one * 0.85f;
            }

            // Park Benches & Flower Boxes
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            if (benchPrefab != null)
            {
                var b1 = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propsRoot.transform);
                b1.name = "Camp_Bench_Right";
                b1.transform.position = new Vector3(3.2f, 0f, -0.8f);
                b1.transform.rotation = Quaternion.Euler(0f, -70f, 0f);
                b1.transform.localScale = Vector3.one * 0.85f;

                var b2 = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propsRoot.transform);
                b2.name = "Camp_Bench_Left";
                b2.transform.position = new Vector3(-3.2f, 0f, -0.8f);
                b2.transform.rotation = Quaternion.Euler(0f, 70f, 0f);
                b2.transform.localScale = Vector3.one * 0.85f;
            }

            // 7. Global Post-Processing Volume
            GameObject volumeObj = new GameObject("GlobalPostProcessingVolume", typeof(Volume));
            Volume vol = volumeObj.GetComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;

            string volProfilePath = "Assets/Settings/DialogueHub_VolumeProfile.asset";
            VolumeProfile volProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volProfilePath);
            if (volProfile == null)
            {
                volProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(volProfile, volProfilePath);
            }
            if (!volProfile.TryGet<Tonemapping>(out var tonemapping)) tonemapping = volProfile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            if (!volProfile.TryGet<Bloom>(out var bloom)) bloom = volProfile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.85f);
            bloom.scatter.Override(0.7f);
            if (!volProfile.TryGet<Vignette>(out var vignette)) vignette = volProfile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.35f);
            EditorUtility.SetDirty(volProfile);
            vol.sharedProfile = volProfile;

            // 8. Player Observer Transform (Camera Look / Focus Target)
            GameObject playerObserver = new GameObject("PlayerObserver");
            playerObserver.transform.position = new Vector3(0f, 1.2f, -3.8f);

            // 9. Main Camera Rig
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData), typeof(CampfirePanoramicCameraController));
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 1.85f, -4.2f);
            camObj.transform.rotation = Quaternion.Euler(11f, 0f, 0f);
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            cam.fieldOfView = 56f;
            cam.allowMSAA = true;
            cam.allowHDR = true;

            var camData = camObj.GetComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Base;
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;

            // 10. SETUP 3D CHARACTERS WITH REAL GLB MODELS (NO CAPSULES!)
            var createdActors = new List<CharacterStageActor>();

            // Companion 1: Margo (Mage Companion)
            var margoActor = CreateCampCharacter(
                name: "NPC_Margo",
                npcId: "npc_margo",
                displayName: "Margo",
                themeColor: new Color(0.75f, 0.4f, 0.95f),
                position: new Vector3(-1.8f, 0f, 0.4f),
                rotation: Quaternion.Euler(0f, 115f, 0f),
                glbPath: "Assets/Models/Characters/Starter/femalestarter.glb",
                propPrefabPath: "Assets/Prefabs/NPCs/NPC_Margo.prefab"
            );
            createdActors.Add(margoActor);

            // Companion 2: Leo (Vanguard)
            var leoActor = CreateCampCharacter(
                name: "Character_Starter_Male",
                npcId: "starter_male",
                displayName: "Leo",
                themeColor: new Color(0.35f, 0.80f, 0.98f),
                position: new Vector3(1.2f, 0f, 0.4f),
                rotation: Quaternion.Euler(0f, -110f, 0f),
                glbPath: "Assets/Models/Characters/Starter/MaleStarter.glb",
                propPrefabPath: "Assets/Prefabs/Characters/Character_Starter_Male.prefab"
            );
            createdActors.Add(leoActor);

            // Companion 3: Barnaby (Blacksmith / Artisan)
            var barnabyActor = CreateCampCharacter(
                name: "NPC_Barnaby",
                npcId: "npc_barnaby",
                displayName: "Barnaby",
                themeColor: new Color(1.0f, 0.65f, 0.2f),
                position: new Vector3(0f, 0f, 2.2f),
                rotation: Quaternion.Euler(0f, 180f, 0f),
                glbPath: "Assets/Models/Characters/Starter/manbase.glb",
                propPrefabPath: "Assets/Prefabs/NPCs/NPC_Barnaby.prefab"
            );
            createdActors.Add(barnabyActor);

            // Companion 4: Zephyr (Scout)
            var zephyrActor = CreateCampCharacter(
                name: "NPC_Zephyr",
                npcId: "npc_zephyr",
                displayName: "Zephyr",
                themeColor: new Color(0.2f, 0.85f, 0.75f),
                position: new Vector3(2.2f, 0f, 1.2f),
                rotation: Quaternion.Euler(0f, -140f, 0f),
                glbPath: "Assets/Models/Characters/Starter/MaleStarter.glb",
                propPrefabPath: "Assets/Prefabs/NPCs/NPC_Zephyr.prefab"
            );
            createdActors.Add(zephyrActor);

            // Companion 5: Catherine (Guild Clerk)
            var catherineActor = CreateCampCharacter(
                name: "NPC_Catherine",
                npcId: "npc_catherine",
                displayName: "Catherine",
                themeColor: new Color(0.35f, 0.90f, 0.55f),
                position: new Vector3(-2.2f, 0f, 1.6f),
                rotation: Quaternion.Euler(0f, 140f, 0f),
                glbPath: "Assets/Models/Characters/Catherine.glb",
                propPrefabPath: null
            );
            createdActors.Add(catherineActor);

            // 11. UI Canvas Hierarchy: CAMP GROUP CHAT
            GameObject canvasObj = CreateCampGroupChatCanvas();

            // 12. Controllers & SystemsManager
            var hubGo = new GameObject("DialogueHubController");
            var conversationHub = hubGo.AddComponent<MultiCharacterConversationHub>();
            var dualCtrl = hubGo.AddComponent<CampfireDualDialogueController>();

            // Wire conversationHub serialized properties
            var serialized = new SerializedObject(conversationHub);
            serialized.FindProperty("playerObserverTransform").objectReferenceValue = playerObserver.transform;

            var actorsProp = serialized.FindProperty("actors");
            actorsProp.ClearArray();
            for (int i = 0; i < createdActors.Count; i++)
            {
                actorsProp.InsertArrayElementAtIndex(i);
                var elem = actorsProp.GetArrayElementAtIndex(i);
                var act = createdActors[i];
                elem.FindPropertyRelative("npcId").stringValue = act.npcId;
                elem.FindPropertyRelative("displayName").stringValue = act.displayName;
                elem.FindPropertyRelative("themeColor").colorValue = act.themeColor;
                elem.FindPropertyRelative("characterRoot").objectReferenceValue = act.characterRoot;
                elem.FindPropertyRelative("meshRenderer").objectReferenceValue = act.meshRenderer;
                elem.FindPropertyRelative("spotlight").objectReferenceValue = act.spotlight;
                elem.FindPropertyRelative("speechBubbleContainer").objectReferenceValue = act.speechBubbleContainer;
                elem.FindPropertyRelative("speechBubbleText").objectReferenceValue = act.speechBubbleText;
            }

            // Wire canvas controls
            var transcriptPanel = canvasObj.transform.Find("SafeAreaContainer/TranscriptPanel");
            if (transcriptPanel != null)
            {
                serialized.FindProperty("transcriptPanelObj").objectReferenceValue = transcriptPanel.gameObject;
                var textTmp = transcriptPanel.Find("Viewport/Content/TranscriptTextTMP")?.GetComponent<TextMeshProUGUI>();
                if (textTmp != null) serialized.FindProperty("transcriptLogText").objectReferenceValue = textTmp;
                var scrollRect = transcriptPanel.GetComponent<ScrollRect>();
                if (scrollRect != null) serialized.FindProperty("transcriptScrollRect").objectReferenceValue = scrollRect;
            }

            var activeHeaderTmp = canvasObj.transform.Find("SafeAreaContainer/TopHeaderBar/ActiveSpeakerHeaderTMP")?.GetComponent<TextMeshProUGUI>();
            if (activeHeaderTmp != null) serialized.FindProperty("activeSpeakerHeader").objectReferenceValue = activeHeaderTmp;

            var returnBtn = canvasObj.transform.Find("SafeAreaContainer/TopHeaderBar/ReturnToDungeonBtn")?.GetComponent<Button>();
            if (returnBtn != null) serialized.FindProperty("returnToDungeonButton").objectReferenceValue = returnBtn;

            var restBtn = canvasObj.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_RestAtFire")?.GetComponent<Button>();
            if (restBtn != null) serialized.FindProperty("restAtCampfireButton").objectReferenceValue = restBtn;

            var toggleTranscriptBtn = canvasObj.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_ToggleLog")?.GetComponent<Button>();
            if (toggleTranscriptBtn != null) serialized.FindProperty("toggleTranscriptButton").objectReferenceValue = toggleTranscriptBtn;

            var toggleBanterBtn = canvasObj.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_ToggleBanter")?.GetComponent<Button>();
            if (toggleBanterBtn != null) serialized.FindProperty("toggleBanterButton").objectReferenceValue = toggleBanterBtn;

            var breakCampBtn = canvasObj.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_BreakCamp")?.GetComponent<Button>();
            if (breakCampBtn != null) serialized.FindProperty("returnToDungeonButton").objectReferenceValue = breakCampBtn;

            var msgInput = canvasObj.transform.Find("SafeAreaContainer/BottomInputDeck/PlayerMessageInputFieldTMP")?.GetComponent<TMP_InputField>();
            if (msgInput != null) serialized.FindProperty("playerMessageInput").objectReferenceValue = msgInput;

            var sendBtn = canvasObj.transform.Find("SafeAreaContainer/BottomInputDeck/SendPlayerMessageBtn")?.GetComponent<Button>();
            if (sendBtn != null) serialized.FindProperty("sendPlayerMessageButton").objectReferenceValue = sendBtn;

            var chip1 = canvasObj.transform.Find("SafeAreaContainer/QuickChipsPanel/Chip1_Floor50")?.GetComponent<Button>();
            if (chip1 != null) serialized.FindProperty("quickPromptBtn1").objectReferenceValue = chip1;
            var chip2 = canvasObj.transform.Find("SafeAreaContainer/QuickChipsPanel/Chip2_Margo")?.GetComponent<Button>();
            if (chip2 != null) serialized.FindProperty("quickPromptBtn2").objectReferenceValue = chip2;
            var chip3 = canvasObj.transform.Find("SafeAreaContainer/QuickChipsPanel/Chip3_Barnaby")?.GetComponent<Button>();
            if (chip3 != null) serialized.FindProperty("quickPromptBtn3").objectReferenceValue = chip3;
            var chip4 = canvasObj.transform.Find("SafeAreaContainer/QuickChipsPanel/Chip4_Zephyr")?.GetComponent<Button>();
            if (chip4 != null) serialized.FindProperty("quickPromptBtn4").objectReferenceValue = chip4;

            serialized.ApplyModifiedProperties();

            // Setup SystemsManager
            var sysMgr = new GameObject("SystemsManager");
            sysMgr.AddComponent<BattleSummonerAudioMaster>();
            sysMgr.AddComponent<ElevenLabsTTSBridge>();
            sysMgr.AddComponent<UnfilteredDialoguePipeline>();
            sysMgr.AddComponent<BattleSummoner.Core.UnityThreadDispatcher>();

            // Ensure EventSystem
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            // Strip any static batching flags across the entire scene to prevent OOM
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                GameObjectUtility.SetStaticEditorFlags(mf.gameObject, 0);
            }

            // 13. Save Scene to CampGroupChat.unity
            EditorSceneManager.SaveScene(scene, CampGroupChatScenePath);
            Debug.Log($"✦ [CAMP GROUP CHAT] Saved scene at: {CampGroupChatScenePath}");

            // 14. Also sync to MultiCharacterDialogueHub.unity for full backward compatibility
            EditorSceneManager.SaveScene(scene, MultiCharacterScenePath);
            Debug.Log($"✦ [CAMP GROUP CHAT] Synchronized scene to: {MultiCharacterScenePath}");

            // 15. Register in Build Settings
            OverworldSceneSetup.RegisterScenesInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("================================================================================");
            Debug.Log("🎉 [CAMP GROUP CHAT] SCENE SETUP COMPLETE & 100% OPERATIONAL! 🎉");
            Debug.Log("================================================================================");
        }

        [MenuItem("BattleSummoner/Camp Group Chat/Open Camp Group Chat Scene", false, 11)]
        public static void OpenCampGroupChatScene()
        {
            if (!File.Exists(CampGroupChatScenePath))
            {
                BuildAndSetupCampGroupChatScene();
            }
            EditorSceneManager.OpenScene(CampGroupChatScenePath, OpenSceneMode.Single);
        }

        private static CharacterStageActor CreateCampCharacter(
            string name,
            string npcId,
            string displayName,
            Color themeColor,
            Vector3 position,
            Quaternion rotation,
            string glbPath,
            string propPrefabPath)
        {
            GameObject charRoot = new GameObject(name);
            charRoot.transform.position = position;
            charRoot.transform.rotation = rotation;

            // Capsule collider for raycast interaction
            var col = charRoot.AddComponent<CapsuleCollider>();
            col.height = 1.85f;
            col.radius = 0.42f;
            col.center = new Vector3(0f, 0.92f, 0f);

            Renderer primaryRenderer = null;

            // Instantiate 3D GLB Model (NO CAPSULES!)
            var glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
            if (glbAsset != null)
            {
                var visualObj = (GameObject)PrefabUtility.InstantiatePrefab(glbAsset, charRoot.transform);
                visualObj.name = "Visual_Model";
                visualObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = Vector3.one;

                primaryRenderer = visualObj.GetComponentInChildren<Renderer>();
            }

            // Attach props if available
            if (!string.IsNullOrEmpty(propPrefabPath))
            {
                var propPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propPrefabPath);
                if (propPrefab != null)
                {
                    // Copy accessory props from prefab (hats, anvil, quivers)
                    foreach (Transform child in propPrefab.transform)
                    {
                        if (child.name.StartsWith("Visual_") || child.name.Contains("Capsule")) continue;
                        var propInst = (GameObject)PrefabUtility.InstantiatePrefab(child.gameObject, charRoot.transform);
                        propInst.name = child.name;
                        propInst.transform.localPosition = child.localPosition;
                        propInst.transform.localRotation = child.localRotation;
                        propInst.transform.localScale = child.localScale;
                    }
                }
            }

            // Spotlight overhead
            GameObject spotObj = new GameObject($"{name}_Spotlight", typeof(Light), typeof(UniversalAdditionalLightData));
            spotObj.transform.SetParent(charRoot.transform, false);
            spotObj.transform.localPosition = new Vector3(0f, 3.2f, 0.5f);
            spotObj.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            Light spot = spotObj.GetComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = themeColor;
            spot.intensity = 1.4f;
            spot.spotAngle = 60f;
            spot.range = 6.5f;

            // 3D In-World Speech Bubble
            GameObject bubbleObj = Create3DSpeechBubble(charRoot.transform, themeColor);
            TextMeshPro bubbleTmp = bubbleObj.GetComponentInChildren<TextMeshPro>();

            return new CharacterStageActor
            {
                npcId = npcId,
                displayName = displayName,
                themeColor = themeColor,
                characterRoot = charRoot.transform,
                meshRenderer = primaryRenderer,
                spotlight = spot,
                speechBubbleContainer = bubbleObj,
                speechBubbleText = bubbleTmp,
                initialRotation = rotation
            };
        }

        private static GameObject Create3DSpeechBubble(Transform parent, Color themeColor)
        {
            GameObject bubbleObj = new GameObject("SpeechBubbleContainer");
            bubbleObj.transform.SetParent(parent, false);
            bubbleObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            // Background quad
            GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgObj.name = "BubbleBackground";
            bgObj.transform.SetParent(bubbleObj.transform, false);
            bgObj.transform.localScale = new Vector3(2.8f, 0.9f, 1f);
            var col = bgObj.GetComponent<Collider>(); if (col != null) UnityEngine.Object.DestroyImmediate(col);
            var rend = bgObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = GetOrCreateMaterial("Mat_SpeechBubble_BG", new Color(0.06f, 0.08f, 0.12f, 0.92f), 0.1f, 0.8f);
            }

            // TMP 3D Text
            GameObject textObj = new GameObject("BubbleTextTMP", typeof(TextMeshPro));
            textObj.transform.SetParent(bubbleObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
            tmp.text = "...";
            tmp.fontSize = 3.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = themeColor;
            tmp.rectTransform.sizeDelta = new Vector2(2.6f, 0.8f);

            bubbleObj.SetActive(false);
            return bubbleObj;
        }

        private static GameObject CreateCampGroupChatCanvas()
        {
            GameObject canvasObj = new GameObject("CampfireDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // Safe Area Container
            GameObject safeAreaObj = new GameObject("SafeAreaContainer", typeof(RectTransform));
            safeAreaObj.transform.SetParent(canvasObj.transform, false);
            RectTransform saRt = safeAreaObj.GetComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.offsetMin = new Vector2(24f, 40f);
            saRt.offsetMax = new Vector2(-24f, -40f);

            // Top Header Bar
            GameObject headerObj = new GameObject("TopHeaderBar", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(safeAreaObj.transform, false);
            RectTransform hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(0f, 96f);
            hRt.anchoredPosition = Vector2.zero;
            headerObj.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.90f);

            // Title: [CAMP GROUP CHAT]
            GameObject titleObj = new GameObject("TitleTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(0.7f, 1f);
            tRt.anchoredPosition = new Vector2(24f, 0f);
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "[CAMP GROUP CHAT]";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1.0f, 0.85f, 0.35f);

            // Active Speaker Subtitle
            GameObject speakerHeaderObj = new GameObject("ActiveSpeakerHeaderTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            speakerHeaderObj.transform.SetParent(headerObj.transform, false);
            RectTransform shRt = speakerHeaderObj.GetComponent<RectTransform>();
            shRt.anchorMin = new Vector2(0f, 0f);
            shRt.anchorMax = new Vector2(0.7f, 0.5f);
            shRt.anchoredPosition = new Vector2(24f, 4f);
            var shTmp = speakerHeaderObj.GetComponent<TextMeshProUGUI>();
            shTmp.text = "<color=#8899AA>Companions resting by the hearth...</color>";
            shTmp.fontSize = 20;
            shTmp.color = Color.white;

            // Return / Break Camp Button in Header
            GameObject returnBtnObj = new GameObject("ReturnToDungeonBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            returnBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform rbRt = returnBtnObj.GetComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(1f, 0.5f);
            rbRt.anchorMax = new Vector2(1f, 0.5f);
            rbRt.pivot = new Vector2(1f, 0.5f);
            rbRt.sizeDelta = new Vector2(220f, 64f);
            rbRt.anchoredPosition = new Vector2(-16f, 0f);
            returnBtnObj.GetComponent<Image>().color = new Color(0.72f, 0.28f, 0.24f, 0.92f);

            GameObject rbTextObj = new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            rbTextObj.transform.SetParent(returnBtnObj.transform, false);
            RectTransform rbtRt = rbTextObj.GetComponent<RectTransform>();
            rbtRt.anchorMin = Vector2.zero;
            rbtRt.anchorMax = Vector2.one;
            var rbtTmp = rbTextObj.GetComponent<TextMeshProUGUI>();
            rbtTmp.text = "BREAK CAMP";
            rbtTmp.alignment = TextAlignmentOptions.Center;
            rbtTmp.fontSize = 20;
            rbtTmp.fontStyle = FontStyles.Bold;
            rbtTmp.color = Color.white;

            // Action Pill Bar (Rest, Log, Banter, Break)
            GameObject barObj = new GameObject("CampfireActionBar", typeof(RectTransform));
            barObj.transform.SetParent(safeAreaObj.transform, false);
            RectTransform bRt = barObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.16f);
            bRt.anchorMax = new Vector2(1f, 0.22f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;

            var hl = barObj.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;
            hl.spacing = 16f;

            CreatePillButton(barObj.transform, "Btn_RestAtFire", "REST (HEAL)", new Color(0.95f, 0.55f, 0.15f, 0.9f));
            CreatePillButton(barObj.transform, "Btn_ToggleLog", "CHAT LOG", new Color(0.25f, 0.45f, 0.70f, 0.9f));
            CreatePillButton(barObj.transform, "Btn_ToggleBanter", "AUTO BANTER", new Color(0.35f, 0.65f, 0.35f, 0.9f));
            CreatePillButton(barObj.transform, "Btn_BreakCamp", "RETURN", new Color(0.70f, 0.28f, 0.25f, 0.9f));

            // Quick Chips Panel
            GameObject chipsObj = new GameObject("QuickChipsPanel", typeof(RectTransform));
            chipsObj.transform.SetParent(safeAreaObj.transform, false);
            RectTransform cRt = chipsObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0.10f);
            cRt.anchorMax = new Vector2(1f, 0.15f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            var chl = chipsObj.AddComponent<HorizontalLayoutGroup>();
            chl.childControlWidth = true;
            chl.childControlHeight = true;
            chl.childForceExpandWidth = true;
            chl.childForceExpandHeight = true;
            chl.spacing = 12f;

            CreateChipButton(chipsObj.transform, "Chip1_Floor50", "Floor 50?");
            CreateChipButton(chipsObj.transform, "Chip2_Margo", "Ask Margo");
            CreateChipButton(chipsObj.transform, "Chip3_Barnaby", "Ask Barnaby");
            CreateChipButton(chipsObj.transform, "Chip4_Zephyr", "Ask Zephyr");

            // Bottom Input Deck
            GameObject inputDeck = new GameObject("BottomInputDeck", typeof(RectTransform), typeof(Image));
            inputDeck.transform.SetParent(safeAreaObj.transform, false);
            RectTransform idRt = inputDeck.GetComponent<RectTransform>();
            idRt.anchorMin = new Vector2(0f, 0f);
            idRt.anchorMax = new Vector2(1f, 0.09f);
            idRt.offsetMin = Vector2.zero;
            idRt.offsetMax = Vector2.zero;
            inputDeck.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

            // Input Field
            GameObject inputFieldObj = new GameObject("PlayerMessageInputFieldTMP", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputFieldObj.transform.SetParent(inputDeck.transform, false);
            RectTransform ifRt = inputFieldObj.GetComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.02f, 0.15f);
            ifRt.anchorMax = new Vector2(0.78f, 0.85f);
            ifRt.offsetMin = Vector2.zero;
            ifRt.offsetMax = Vector2.zero;
            inputFieldObj.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.22f, 1f);

            var tmpInput = inputFieldObj.GetComponent<TMP_InputField>();

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(inputFieldObj.transform, false);
            RectTransform tRt2 = textObj.GetComponent<RectTransform>();
            tRt2.anchorMin = Vector2.zero;
            tRt2.anchorMax = Vector2.one;
            tRt2.offsetMin = new Vector2(16f, 0f);
            tRt2.offsetMax = new Vector2(-16f, 0f);
            var textTmp = textObj.GetComponent<TextMeshProUGUI>();
            textTmp.fontSize = 24;
            textTmp.color = Color.white;
            tmpInput.textComponent = textTmp;

            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phObj.transform.SetParent(inputFieldObj.transform, false);
            RectTransform phRt = phObj.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(16f, 0f);
            phRt.offsetMax = new Vector2(-16f, 0f);
            var phTmp = phObj.GetComponent<TextMeshProUGUI>();
            phTmp.text = "Type message or ask campfire group...";
            phTmp.fontSize = 24;
            phTmp.color = new Color(0.6f, 0.65f, 0.75f, 0.7f);
            tmpInput.placeholder = phTmp;

            // Send Button
            GameObject sendBtnObj = new GameObject("SendPlayerMessageBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            sendBtnObj.transform.SetParent(inputDeck.transform, false);
            RectTransform sbRt = sendBtnObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.80f, 0.15f);
            sbRt.anchorMax = new Vector2(0.98f, 0.85f);
            sbRt.offsetMin = Vector2.zero;
            sbRt.offsetMax = Vector2.zero;
            sendBtnObj.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.95f, 1f);

            GameObject sbTextObj = new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            sbTextObj.transform.SetParent(sendBtnObj.transform, false);
            RectTransform sbtRt = sbTextObj.GetComponent<RectTransform>();
            sbtRt.anchorMin = Vector2.zero;
            sbtRt.anchorMax = Vector2.one;
            var sbtTmp = sbTextObj.GetComponent<TextMeshProUGUI>();
            sbtTmp.text = "SEND";
            sbtTmp.alignment = TextAlignmentOptions.Center;
            sbtTmp.fontSize = 22;
            sbtTmp.fontStyle = FontStyles.Bold;
            sbtTmp.color = Color.white;

            // Transcript Panel (collapsible)
            GameObject transcriptPanel = new GameObject("TranscriptPanel", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            transcriptPanel.transform.SetParent(safeAreaObj.transform, false);
            RectTransform tpRt = transcriptPanel.GetComponent<RectTransform>();
            tpRt.anchorMin = new Vector2(0.04f, 0.24f);
            tpRt.anchorMax = new Vector2(0.96f, 0.75f);
            tpRt.offsetMin = Vector2.zero;
            tpRt.offsetMax = Vector2.zero;
            transcriptPanel.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.92f);

            var scrollRect = transcriptPanel.GetComponent<ScrollRect>();

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(transcriptPanel.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(16f, 16f);
            vpRt.offsetMax = new Vector2(-16f, -16f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform ctRt = content.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 1f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.pivot = new Vector2(0f, 1f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject transTextObj = new GameObject("TranscriptTextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            transTextObj.transform.SetParent(content.transform, false);
            var transTmp = transTextObj.GetComponent<TextMeshProUGUI>();
            transTmp.text = "<color=#666688><i>[Campfire embers crackle softly as companions rest by the hearth...]</i></color>\n\n";
            transTmp.fontSize = 22;
            transTmp.color = Color.white;

            scrollRect.viewport = vpRt;
            scrollRect.content = ctRt;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            transcriptPanel.SetActive(false);

            return canvasObj;
        }

        private static GameObject CreatePillButton(Transform parent, string name, string label, Color color)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = color;

            GameObject textObj = new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRt = textObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }

        private static GameObject CreateChipButton(Transform parent, string name, string label)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.30f, 0.9f);

            GameObject textObj = new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRt = textObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.90f, 1.0f);

            return btnObj;
        }

        private static Material GetOrCreateMaterial(string matName, Color albedo, float metallic, float smoothness, Color? emission = null)
        {
            string matPath = $"Assets/Materials/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(urpLit);
                mat.name = matName;
                mat.color = albedo;
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

                if (emission.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value);
                }

                if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");
                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }
    }
}

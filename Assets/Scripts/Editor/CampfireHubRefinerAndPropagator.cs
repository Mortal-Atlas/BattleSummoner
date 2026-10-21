using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using BattleSummoner.Dialogue;
using BattleSummoner.UI;

namespace BattleSummoner.Editor
{
    public static class CampfireHubRefinerAndPropagator
    {
        [MenuItem("BattleSummoner/Refine Campfire Hub & Propagate Meshy Props", false, 10)]
        public static void ExecuteAll()
        {
            Debug.Log("==================================================================");
            Debug.Log("✦ STARTING CAMPFIRE HUB REFINEMENT & MESHY ASSET PROPAGATION ✦");
            Debug.Log("==================================================================");

            RefineCampfireHubScene();
            PropagateToShopInterior();
            PropagateToGuildHallInterior();
            PropagateToBuildingInterior();
            PropagateToMainDungeonFloor();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✦ ALL SCENES REFINED AND PROPAGATED SUCCESSFULLY! ✦");
        }

        public static void RefineCampfireHubScene()
        {
            string scenePath = "Assets/Scenes/CampfireHub.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);

            Debug.Log($"[CampfireHub] Refining {scene.name}...");

            // 1. Remove old capsule placeholders
            string[] oldNpcNames = { "NPC_Margo", "NPC_Barnaby", "NPC_Zephyr" };
            foreach (var name in oldNpcNames)
            {
                var oldGo = GameObject.Find(name);
                if (oldGo != null)
                {
                    Debug.Log($"[CampfireHub] Removing legacy placeholder: {name}");
                    Undo.DestroyObjectImmediate(oldGo);
                }
            }

            // Remove harsh cavern box walls if present, keep floor
            var cavernBackdrop = GameObject.Find("CavernBackdropWall");
            if (cavernBackdrop != null)
            {
                // Lower or scale back the oppressive wall
                cavernBackdrop.transform.position = new Vector3(0f, 2f, 12f);
                cavernBackdrop.transform.localScale = new Vector3(25f, 8f, 1f);
                var mr = cavernBackdrop.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    // Soft dark slate
                    mr.sharedMaterial.color = new Color(0.12f, 0.14f, 0.18f);
                }
            }

            // 2. Adjust Pillars to frame the space gently
            var p1 = GameObject.Find("BackgroundPillar_1");
            if (p1 != null) p1.transform.position = new Vector3(7.5f, 4f, 6.5f);
            var p2 = GameObject.Find("BackgroundPillar_2");
            if (p2 != null) p2.transform.position = new Vector3(-7.5f, 4f, 6.5f);

            // 3. Setup Campsite Environment & Hearth
            var hearth = GameObject.Find("CampfireHearth");
            if (hearth == null)
            {
                hearth = new GameObject("CampfireHearth");
            }
            hearth.transform.position = Vector3.zero;

            // Soften HearthFireLight for relaxing, anti-overstimulation ambiance
            var hearthLightObj = GameObject.Find("HearthFireLight");
            if (hearthLightObj != null)
            {
                var light = hearthLightObj.GetComponent<Light>();
                if (light != null)
                {
                    light.color = new Color(1.0f, 0.62f, 0.28f);
                    light.intensity = 2.4f;
                    light.range = 8.5f;
                    light.shadows = LightShadows.Soft;
                }
            }

            // Soft Directional Light for calm twilight ambiance
            var dirLightObj = GameObject.Find("Directional Light");
            if (dirLightObj != null)
            {
                var dl = dirLightObj.GetComponent<Light>();
                if (dl != null)
                {
                    dl.color = new Color(0.35f, 0.42f, 0.60f); // Twilight indigo
                    dl.intensity = 0.45f;
                    dirLightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                }
            }

            // 4. Instantiate and Position Starter Character Models (Female Starter & Male Starter)
            var femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Starter_Female.prefab");
            var malePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Starter_Male.prefab");

            GameObject femaleInstance = GameObject.Find("Character_Starter_Female");
            if (femaleInstance == null && femalePrefab != null)
            {
                femaleInstance = (GameObject)PrefabUtility.InstantiatePrefab(femalePrefab);
                femaleInstance.name = "Character_Starter_Female";
            }
            if (femaleInstance != null)
            {
                femaleInstance.transform.position = new Vector3(-1.0f, 0.95f, 0.45f);
                femaleInstance.transform.rotation = Quaternion.Euler(0f, 105f, 0f); // Facing hearth & player

                // Ensure CapsuleCollider for raycast interaction
                var col = femaleInstance.GetComponent<CapsuleCollider>();
                if (col == null) col = femaleInstance.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.88f, 0f);
                col.height = 1.76f;
                col.radius = 0.42f;

                FitStarterCharacterHair(femaleInstance, true);

                // Add in-world speech bubble container for autonomous banter
                EnsureActorSpeechBubble(femaleInstance, "Aria", new Color(0.85f, 0.55f, 0.95f));
            }

            GameObject maleInstance = GameObject.Find("Character_Starter_Male");
            if (maleInstance == null && malePrefab != null)
            {
                maleInstance = (GameObject)PrefabUtility.InstantiatePrefab(malePrefab);
                maleInstance.name = "Character_Starter_Male";
            }
            if (maleInstance != null)
            {
                maleInstance.transform.position = new Vector3(1.0f, 0.95f, 0.45f);
                maleInstance.transform.rotation = Quaternion.Euler(0f, -105f, 0f); // Facing hearth & player

                // Ensure CapsuleCollider for raycast interaction
                var col = maleInstance.GetComponent<CapsuleCollider>();
                if (col == null) col = maleInstance.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.92f, 0f);
                col.height = 1.84f;
                col.radius = 0.44f;

                FitStarterCharacterHair(maleInstance, false);

                // Add in-world speech bubble container for autonomous banter
                EnsureActorSpeechBubble(maleInstance, "Leo", new Color(0.35f, 0.80f, 0.98f));
            }

            // 5. Populate Campfire Hub with Meshy Props
            var propsRoot = GameObject.Find("CAMPFIRE_MESHY_PROPS");
            if (propsRoot == null) propsRoot = new GameObject("CAMPFIRE_MESHY_PROPS");

            // A. Meshy Stylized Alchemist Cauldron over the fire
            var cauldronPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_AlchemistCauldron_Stylized.prefab");
            if (cauldronPrefab != null && propsRoot.transform.Find("Campfire_Cauldron") == null)
            {
                var cauldron = (GameObject)PrefabUtility.InstantiatePrefab(cauldronPrefab, propsRoot.transform);
                cauldron.name = "Campfire_Cauldron";
                cauldron.transform.position = new Vector3(0f, 0.05f, 0.35f);
                cauldron.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            }

            // B. Meshy Market Merchant Crates & Baskets (Camp Supplies / Rations)
            var cratesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
            if (cratesPrefab != null)
            {
                if (propsRoot.transform.Find("Camp_Supplies_Right") == null)
                {
                    var c1 = (GameObject)PrefabUtility.InstantiatePrefab(cratesPrefab, propsRoot.transform);
                    c1.name = "Camp_Supplies_Right";
                    c1.transform.position = new Vector3(2.8f, 0f, 1.4f);
                    c1.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
                    c1.transform.localScale = Vector3.one * 0.9f;
                }
                if (propsRoot.transform.Find("Camp_Supplies_Left") == null)
                {
                    var c2 = (GameObject)PrefabUtility.InstantiatePrefab(cratesPrefab, propsRoot.transform);
                    c2.name = "Camp_Supplies_Left";
                    c2.transform.position = new Vector3(-2.8f, 0f, 1.4f);
                    c2.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                    c2.transform.localScale = Vector3.one * 0.85f;
                }
            }

            // C. Meshy Park Bench & Flower Box (Cozy Seating)
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            if (benchPrefab != null)
            {
                if (propsRoot.transform.Find("Camp_Bench_Female") == null)
                {
                    var b1 = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propsRoot.transform);
                    b1.name = "Camp_Bench_Female";
                    b1.transform.position = new Vector3(-2.3f, 0f, 0.95f);
                    b1.transform.rotation = Quaternion.Euler(0f, 120f, 0f);
                    b1.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                }
                if (propsRoot.transform.Find("Camp_Bench_Male") == null)
                {
                    var b2 = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propsRoot.transform);
                    b2.name = "Camp_Bench_Male";
                    b2.transform.position = new Vector3(2.3f, 0f, 0.95f);
                    b2.transform.rotation = Quaternion.Euler(0f, -120f, 0f);
                    b2.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                }
            }

            // D. Meshy Wrought Iron Lamps (Soft campsite ambient lighting)
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            if (lampPrefab != null)
            {
                if (propsRoot.transform.Find("Camp_Lamp_Left") == null)
                {
                    var l1 = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, propsRoot.transform);
                    l1.name = "Camp_Lamp_Left";
                    l1.transform.position = new Vector3(-4.0f, 0f, 2.6f);
                    l1.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
                    AddSoftLanternLight(l1);
                }
                if (propsRoot.transform.Find("Camp_Lamp_Right") == null)
                {
                    var l2 = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, propsRoot.transform);
                    l2.name = "Camp_Lamp_Right";
                    l2.transform.position = new Vector3(4.0f, 0f, 2.6f);
                    l2.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
                    AddSoftLanternLight(l2);
                }
            }

            // E. Clean up any bulky backdrop obstacles for optimal visual clarity
            var oldPlanter = propsRoot.transform.Find("Camp_Planter_Backdrop");
            if (oldPlanter != null) Undo.DestroyObjectImmediate(oldPlanter.gameObject);

            var oldStones = propsRoot.transform.Find("Hearth_Stones_Surround");
            if (oldStones != null) Undo.DestroyObjectImmediate(oldStones.gameObject);

            // 6. Camera & View Setup (Positioned for balanced, clear view of campsite in portrait)
            var camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                camObj.transform.position = new Vector3(0f, 1.4f, -4.6f);
                camObj.transform.rotation = Quaternion.Euler(7f, 0f, 0f);

                var cam = camObj.GetComponent<Camera>();
                if (cam != null) cam.fieldOfView = 58f;

                var camPan = camObj.GetComponent<CampfirePanoramicCameraController>();
                if (camPan != null)
                {
                    // Configure pan angle limit
                    camPan.PanToAngle(0f);
                }
            }

            // 7. Post-Processing Configuration (Depth of Field, Warm ACES, Gentle Vignette)
            ConfigurePostProcessing();

            // 8. Visual Clarity & Anti-Overstimulation UI Redesign
            RefineCampfireCanvasUI(femaleInstance, maleInstance);

            // 9. Update DialogueHubController references
            ConfigureDialogueControllers(femaleInstance, maleInstance);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CampfireHub] Scene saved successfully!");
        }

        private static void AddSoftLanternLight(GameObject lampGo)
        {
            var light = lampGo.GetComponentInChildren<Light>();
            if (light == null)
            {
                var lightObj = new GameObject("LanternWarmLight", typeof(Light));
                lightObj.transform.SetParent(lampGo.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0.4f);
                light = lightObj.GetComponent<Light>();
            }
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.85f, 0.60f); // Warm incandescent
            light.intensity = 1.1f;
            light.range = 6.5f;
            light.shadows = LightShadows.None;
        }

        private static void EnsureActorSpeechBubble(GameObject actorGo, string speakerName, Color themeColor)
        {
            var existing = actorGo.transform.Find("SpeechBubbleContainer");
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            var bubbleContainer = new GameObject("SpeechBubbleContainer");
            bubbleContainer.transform.SetParent(actorGo.transform, false);
            bubbleContainer.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            bubbleContainer.transform.localScale = Vector3.one * 0.012f;

            // Background quad / sprite
            var bgObj = new GameObject("BubbleBg", typeof(MeshFilter), typeof(MeshRenderer));
            bgObj.transform.SetParent(bubbleContainer.transform, false);
            bgObj.transform.localScale = new Vector3(240f, 65f, 1f);

            var mf = bgObj.GetComponent<MeshFilter>();
            var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mf.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            GameObject.DestroyImmediate(tempQuad);

            var mr = bgObj.GetComponent<MeshRenderer>();
            var bubbleMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            bubbleMat.color = new Color(0.08f, 0.10f, 0.14f, 0.85f);
            mr.sharedMaterial = bubbleMat;

            // Text
            var textObj = new GameObject("SpeechText", typeof(TextMeshPro));
            textObj.transform.SetParent(bubbleContainer.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.05f);

            var tmp = textObj.GetComponent<TextMeshPro>();
            tmp.text = "";
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(230f, 60f);

            bubbleContainer.SetActive(false);
        }

        private static void FitStarterCharacterHair(GameObject character, bool isFemale)
        {
            if (character == null) return;

            // 1. Hide the legacy 2D sprite rig if present on the starter model
            var faceRig = character.transform.Find("FaceRig_SortingGroup");
            if (faceRig != null) faceRig.gameObject.SetActive(false);

            // 2. Load hair mesh (Hair06 from ModularCharacterPolyart)
            var polyPath = "Assets/RPG Tiny Hero Duo/Mesh/ModularCharacterPolyart.fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(polyPath);
            Mesh hairMesh = null;
            foreach (var a in assets)
            {
                if (a is Mesh m && m.name == "Hair06") { hairMesh = m; break; }
            }

            // 3. Load Hair Materials
            var matPath = isFemale
                ? "Assets/Materials/Characters/M_NPC_Hair_Violet.mat"
                : "Assets/Materials/Characters/M_NPC_Hair_Silver.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            var hairTf = character.transform.Find("Hero_Hair");
            if (hairTf == null)
            {
                var hairGo = new GameObject("Hero_Hair", typeof(MeshFilter), typeof(MeshRenderer));
                hairTf = hairGo.transform;
                hairTf.SetParent(character.transform, false);
            }

            if (hairMesh != null) hairTf.GetComponent<MeshFilter>().sharedMesh = hairMesh;
            if (mat != null) hairTf.GetComponent<MeshRenderer>().sharedMaterial = mat;

            hairTf.localPosition = new Vector3(0.005f, 0.825f, 0.012f);
            hairTf.localEulerAngles = new Vector3(3f, 270f, 0f);
            hairTf.localScale = new Vector3(0.47f, 0.46f, 0.48f);
        }

        private static void ConfigurePostProcessing()
        {
            var ppObj = GameObject.Find("GlobalPostProcessingVolume");
            if (ppObj == null) ppObj = new GameObject("GlobalPostProcessingVolume", typeof(Volume));
            var volume = ppObj.GetComponent<Volume>();
            volume.isGlobal = true;

            VolumeProfile profile = volume.sharedProfile;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "CampfireHub_VolumeProfile_Refined";
                AssetDatabase.CreateAsset(profile, "Assets/Settings/CampfireHub_VolumeProfile_Refined.asset");
                volume.sharedProfile = profile;
            }

            // 1. Bloom (gentle golden warmth)
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.threshold.value = 1.05f;
            bloom.intensity.value = 0.65f;
            bloom.tint.value = new Color(1f, 0.9f, 0.75f);

            // 2. Vignette (soft focus inward, reducing eye strain)
            if (!profile.TryGet<Vignette>(out var vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.active = true;
            vignette.intensity.value = 0.32f;
            vignette.smoothness.value = 0.45f;
            vignette.color.value = new Color(0.04f, 0.05f, 0.08f);

            // 3. Color Adjustments (Warm, cozy contrast)
            if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            colorAdj.active = true;
            colorAdj.postExposure.value = 0.2f;
            colorAdj.contrast.value = 10f;
            colorAdj.saturation.value = 12f;

            // 4. Depth of Field (Campfire & companion focus)
            if (!profile.TryGet<DepthOfField>(out var dof))
            {
                dof = profile.Add<DepthOfField>(true);
            }
            dof.active = true;
            dof.mode.value = DepthOfFieldMode.Gaussian;
            dof.gaussianStart.value = 4.8f;
            dof.gaussianEnd.value = 12.0f;
            dof.gaussianMaxRadius.value = 1.0f;
        }

        private static void RefineCampfireCanvasUI(GameObject femaleActor, GameObject maleActor)
        {
            var canvas = GameObject.Find("CampfireDialogueCanvas");
            if (canvas == null) return;

            // Hide the massive transcript log panel by default
            var transcriptPanel = canvas.transform.Find("SafeAreaContainer/TranscriptPanel");
            if (transcriptPanel != null)
            {
                transcriptPanel.gameObject.SetActive(false);
            }

            // Hide bulky quick chips panel by default (clean screen)
            var chipsPanel = canvas.transform.Find("SafeAreaContainer/QuickChipsPanel");
            if (chipsPanel != null)
            {
                chipsPanel.gameObject.SetActive(false);
            }

            // Hide bottom input deck by default
            var inputDeck = canvas.transform.Find("SafeAreaContainer/BottomInputDeck");
            if (inputDeck != null)
            {
                inputDeck.gameObject.SetActive(false);
            }

            // Refine Top Header Bar
            var topBar = canvas.transform.Find("SafeAreaContainer/TopHeaderBar");
            if (topBar != null)
            {
                var titleTMP = topBar.Find("TitleTMP")?.GetComponent<TextMeshProUGUI>();
                if (titleTMP != null)
                {
                    titleTMP.text = "<color=#FFD54F>✦ CAMPFIRE REST HAVEN ✦</color>\n<size=13><color=#C8D6E5>Resting & Companionship • Click companion to converse</color></size>";
                    titleTMP.fontSize = 22;
                    titleTMP.alignment = TextAlignmentOptions.Left;
                }

                var speakerTMP = topBar.Find("ActiveSpeakerHeaderTMP")?.GetComponent<TextMeshProUGUI>();
                if (speakerTMP != null)
                {
                    speakerTMP.text = "<color=#81D4FA>Resting at the hearth...</color>";
                }
            }

            // Build Sleek Minimalist Action Bar at the Bottom
            var safeArea = canvas.transform.Find("SafeAreaContainer");
            if (safeArea != null)
            {
                var actionBar = safeArea.Find("CampfireActionBar");
                if (actionBar == null)
                {
                    var barObj = new GameObject("CampfireActionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    barObj.transform.SetParent(safeArea, false);
                    var rt = barObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(0f, 32f);
                    rt.sizeDelta = new Vector2(720f, 54f);

                    var hlg = barObj.GetComponent<HorizontalLayoutGroup>();
                    hlg.spacing = 16f;
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.childControlWidth = true;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = true;
                    hlg.childForceExpandHeight = false;

                    // Button 1: Rest at Fire [R]
                    CreateActionPillButton(barObj.transform, "Btn_RestAtFire", "✦ REST (+100% HP/MP)", new Color(0.2f, 0.65f, 0.45f, 0.9f));

                    // Button 2: History Log [L]
                    CreateActionPillButton(barObj.transform, "Btn_ToggleLog", "📜 LOG", new Color(0.25f, 0.30f, 0.42f, 0.9f));

                    // Button 3: Banter Toggle [B]
                    CreateActionPillButton(barObj.transform, "Btn_ToggleBanter", "💬 BANTER: ON", new Color(0.35f, 0.30f, 0.50f, 0.9f));

                    // Button 4: Break Camp [Esc]
                    CreateActionPillButton(barObj.transform, "Btn_BreakCamp", "🏕 BREAK CAMP", new Color(0.70f, 0.28f, 0.25f, 0.9f));
                }
            }
        }

        private static GameObject CreateActionPillButton(Transform parent, string name, string label, Color bgColor)
        {
            var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 44f);

            var img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            var txtObj = new GameObject("TextTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }

        private static void ConfigureDialogueControllers(GameObject femaleActor, GameObject maleActor)
        {
            var hubControllerObj = GameObject.Find("DialogueHubController");
            if (hubControllerObj == null) return;

            var conversationHub = hubControllerObj.GetComponent<MultiCharacterConversationHub>();
            if (conversationHub != null)
            {
                var serialized = new SerializedObject(conversationHub);
                var actorsProp = serialized.FindProperty("actors");
                actorsProp.ClearArray();

                // Actor 0: Aria (Female Starter)
                actorsProp.InsertArrayElementAtIndex(0);
                var ariaProp = actorsProp.GetArrayElementAtIndex(0);
                ariaProp.FindPropertyRelative("npcId").stringValue = "starter_female";
                ariaProp.FindPropertyRelative("displayName").stringValue = "Aria";
                ariaProp.FindPropertyRelative("themeColor").colorValue = new Color(0.85f, 0.55f, 0.95f);
                if (femaleActor != null)
                {
                    ariaProp.FindPropertyRelative("characterRoot").objectReferenceValue = femaleActor.transform;
                    ariaProp.FindPropertyRelative("meshRenderer").objectReferenceValue = femaleActor.GetComponentInChildren<Renderer>();
                    var bubble = femaleActor.transform.Find("SpeechBubbleContainer");
                    if (bubble != null)
                    {
                        ariaProp.FindPropertyRelative("speechBubbleContainer").objectReferenceValue = bubble.gameObject;
                        ariaProp.FindPropertyRelative("speechBubbleText").objectReferenceValue = bubble.GetComponentInChildren<TextMeshPro>();
                    }
                }

                // Actor 1: Leo (Male Starter)
                actorsProp.InsertArrayElementAtIndex(1);
                var leoProp = actorsProp.GetArrayElementAtIndex(1);
                leoProp.FindPropertyRelative("npcId").stringValue = "starter_male";
                leoProp.FindPropertyRelative("displayName").stringValue = "Leo";
                leoProp.FindPropertyRelative("themeColor").colorValue = new Color(0.35f, 0.80f, 0.98f);
                if (maleActor != null)
                {
                    leoProp.FindPropertyRelative("characterRoot").objectReferenceValue = maleActor.transform;
                    leoProp.FindPropertyRelative("meshRenderer").objectReferenceValue = maleActor.GetComponentInChildren<Renderer>();
                    var bubble = maleActor.transform.Find("SpeechBubbleContainer");
                    if (bubble != null)
                    {
                        leoProp.FindPropertyRelative("speechBubbleContainer").objectReferenceValue = bubble.gameObject;
                        leoProp.FindPropertyRelative("speechBubbleText").objectReferenceValue = bubble.GetComponentInChildren<TextMeshPro>();
                    }
                }

                // Wire action buttons
                var canvas = GameObject.Find("CampfireDialogueCanvas");
                if (canvas != null)
                {
                    var transcriptPanel = canvas.transform.Find("SafeAreaContainer/TranscriptPanel");
                    if (transcriptPanel != null)
                    {
                        serialized.FindProperty("transcriptPanelObj").objectReferenceValue = transcriptPanel.gameObject;
                    }

                    var restBtn = canvas.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_RestAtFire")?.GetComponent<Button>();
                    if (restBtn != null) serialized.FindProperty("restAtCampfireButton").objectReferenceValue = restBtn;

                    var logBtn = canvas.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_ToggleLog")?.GetComponent<Button>();
                    if (logBtn != null) serialized.FindProperty("toggleTranscriptButton").objectReferenceValue = logBtn;

                    var banterBtn = canvas.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_ToggleBanter")?.GetComponent<Button>();
                    if (banterBtn != null) serialized.FindProperty("toggleBanterButton").objectReferenceValue = banterBtn;

                    var breakBtn = canvas.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_BreakCamp")?.GetComponent<Button>();
                    if (breakBtn != null) serialized.FindProperty("returnToDungeonButton").objectReferenceValue = breakBtn;
                }

                serialized.ApplyModifiedProperties();
                Debug.Log("[CampfireHub] MultiCharacterConversationHub configured with Starter Female & Starter Male actors.");
            }
        }

        #region Propagation to Other Scenes
        public static void PropagateToShopInterior()
        {
            string path = "Assets/Scenes/ShopInterior.unity";
            var scene = EditorSceneManager.OpenScene(path);
            Debug.Log($"[Propagate] Populating {scene.name} with Meshy assets...");

            var root = GameObject.Find("Shop_Building_Interior");
            if (root == null)
            {
                var r = GameObject.Find("--- SHOP INTERIOR ROOT ---");
                root = r != null ? r : new GameObject("Shop_Building_Interior");
            }

            var propsGroup = GameObject.Find("SHOP_MESHY_PROPS");
            if (propsGroup == null)
            {
                propsGroup = new GameObject("SHOP_MESHY_PROPS");
                propsGroup.transform.SetParent(root.transform, false);
            }

            // 1. Alchemist Cauldron for potions & brews
            var cauldronPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_AlchemistCauldron_Stylized.prefab");
            if (cauldronPrefab != null && propsGroup.transform.Find("Shop_Cauldron") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(cauldronPrefab, propsGroup.transform);
                obj.name = "Shop_Cauldron";
                obj.transform.position = new Vector3(-2.2f, 0f, 1.8f);
                obj.transform.localScale = Vector3.one * 0.9f;
            }

            // 2. Merchant Crates and Baskets
            var cratesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
            if (cratesPrefab != null && propsGroup.transform.Find("Shop_Merchandise_Crates") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(cratesPrefab, propsGroup.transform);
                obj.name = "Shop_Merchandise_Crates";
                obj.transform.position = new Vector3(2.4f, 0f, 2.0f);
                obj.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
            }

            // 3. Wrought iron lamp near entrance
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            if (lampPrefab != null && propsGroup.transform.Find("Shop_Interior_Lamp") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, propsGroup.transform);
                obj.name = "Shop_Interior_Lamp";
                obj.transform.position = new Vector3(-1.8f, 0f, -1.5f);
                AddSoftLanternLight(obj);
            }

            // 4. Bench & Flower Box
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            if (benchPrefab != null && propsGroup.transform.Find("Shop_Customer_Bench") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propsGroup.transform);
                obj.name = "Shop_Customer_Bench";
                obj.transform.position = new Vector3(2.2f, 0f, -0.8f);
                obj.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Propagate] ShopInterior saved with Meshy props.");
        }

        public static void PropagateToGuildHallInterior()
        {
            string path = "Assets/Scenes/GuildHallInterior.unity";
            var scene = EditorSceneManager.OpenScene(path);
            Debug.Log($"[Propagate] Populating {scene.name} with Meshy assets...");

            var propsGroup = GameObject.Find("GUILDHALL_MESHY_PROPS");
            if (propsGroup == null) propsGroup = new GameObject("GUILDHALL_MESHY_PROPS");

            // 1. Royal Celestial Sanctuary Arch (Grand Dais Arch)
            var archModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/MeshyGen/Royal_Celestial_Sanctuary_Arch.glb");
            if (archModel != null && propsGroup.transform.Find("Guild_Sanctuary_Arch") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(archModel, propsGroup.transform);
                obj.name = "Guild_Sanctuary_Arch";
                obj.transform.position = new Vector3(0f, 0f, 10.5f);
                obj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                obj.transform.localScale = Vector3.one * 1.8f;
            }

            // 2. Royal Celestial Astrolabe (Grand Astrolabe display)
            var astrolabeModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/MeshyGen/Royal_Celestial_Astrolabe.glb");
            if (astrolabeModel != null && propsGroup.transform.Find("Guild_Celestial_Astrolabe") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(astrolabeModel, propsGroup.transform);
                obj.name = "Guild_Celestial_Astrolabe";
                obj.transform.position = new Vector3(0f, 1.2f, 9.2f);
                obj.transform.localScale = Vector3.one * 1.1f;
            }

            // 3. Flower Box Benches along the guild walls
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            if (benchPrefab != null)
            {
                if (propsGroup.transform.Find("Guild_Bench_Left") == null)
                {
                    var b1 = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propsGroup.transform);
                    b1.name = "Guild_Bench_Left";
                    b1.transform.position = new Vector3(-4.5f, 0f, 3.5f);
                    b1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
                if (propsGroup.transform.Find("Guild_Bench_Right") == null)
                {
                    var b2 = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propsGroup.transform);
                    b2.name = "Guild_Bench_Right";
                    b2.transform.position = new Vector3(4.5f, 0f, 3.5f);
                    b2.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                }
            }

            // 4. Sakura Stone Planter Ring near the entrance
            var planterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_SakuraStonePlanterRing.prefab");
            if (planterPrefab != null && propsGroup.transform.Find("Guild_PlanterRing") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(planterPrefab, propsGroup.transform);
                obj.name = "Guild_PlanterRing";
                obj.transform.position = new Vector3(0f, 0f, -2.5f);
            }

            // 5. Wrought Iron Street Lamps flanking the dais
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            if (lampPrefab != null)
            {
                if (propsGroup.transform.Find("Guild_Lamp_Left") == null)
                {
                    var l1 = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, propsGroup.transform);
                    l1.name = "Guild_Lamp_Left";
                    l1.transform.position = new Vector3(-3.2f, 0f, 8.5f);
                    AddSoftLanternLight(l1);
                }
                if (propsGroup.transform.Find("Guild_Lamp_Right") == null)
                {
                    var l2 = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, propsGroup.transform);
                    l2.name = "Guild_Lamp_Right";
                    l2.transform.position = new Vector3(3.2f, 0f, 8.5f);
                    AddSoftLanternLight(l2);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Propagate] GuildHallInterior saved with Meshy props.");
        }

        public static void PropagateToBuildingInterior()
        {
            string path = "Assets/Scenes/BuildingInterior.unity";
            var scene = EditorSceneManager.OpenScene(path);
            Debug.Log($"[Propagate] Populating {scene.name} with Meshy assets...");

            var propsGroup = GameObject.Find("TAVERN_MESHY_PROPS");
            if (propsGroup == null) propsGroup = new GameObject("TAVERN_MESHY_PROPS");

            // 1. Merchant Crates & Baskets (Tavern Pantry Supplies)
            var cratesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
            if (cratesPrefab != null && propsGroup.transform.Find("Tavern_Pantry_Crates") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(cratesPrefab, propsGroup.transform);
                obj.name = "Tavern_Pantry_Crates";
                obj.transform.position = new Vector3(-4.0f, 0f, 3.2f);
            }

            // 2. Benches for patrons
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            if (benchPrefab != null && propsGroup.transform.Find("Tavern_Wall_Bench") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, propsGroup.transform);
                obj.name = "Tavern_Wall_Bench";
                obj.transform.position = new Vector3(3.6f, 0f, 1.5f);
                obj.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }

            // 3. Wrought Iron Lamp
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            if (lampPrefab != null && propsGroup.transform.Find("Tavern_Corner_Lamp") == null)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, propsGroup.transform);
                obj.name = "Tavern_Corner_Lamp";
                obj.transform.position = new Vector3(4.2f, 0f, -2.0f);
                AddSoftLanternLight(obj);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Propagate] BuildingInterior saved with Meshy props.");
        }

        public static void PropagateToMainDungeonFloor()
        {
            string path = "Assets/Scenes/MainDungeonFloor.unity";
            var scene = EditorSceneManager.OpenScene(path);
            Debug.Log($"[Propagate] Populating {scene.name} with Adventurer Camp Nook...");

            var campNook = GameObject.Find("DUNGEON_REST_CAMP_NOOK");
            if (campNook == null)
            {
                campNook = new GameObject("DUNGEON_REST_CAMP_NOOK");
                campNook.transform.position = new Vector3(-12f, 0f, 8f);

                // 1. Alchemist Cauldron simmering at camp nook
                var cauldronPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_AlchemistCauldron_Stylized.prefab");
                if (cauldronPrefab != null)
                {
                    var c = (GameObject)PrefabUtility.InstantiatePrefab(cauldronPrefab, campNook.transform);
                    c.name = "RestNook_Cauldron";
                    c.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                }

                // 2. Supply Crates
                var cratesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
                if (cratesPrefab != null)
                {
                    var cr = (GameObject)PrefabUtility.InstantiatePrefab(cratesPrefab, campNook.transform);
                    cr.name = "RestNook_Crates";
                    cr.transform.localPosition = new Vector3(2.2f, 0f, 1.2f);
                }

                // 3. Resting Bench
                var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
                if (benchPrefab != null)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, campNook.transform);
                    b.name = "RestNook_Bench";
                    b.transform.localPosition = new Vector3(-2.2f, 0f, 0.8f);
                    b.transform.localRotation = Quaternion.Euler(0f, 75f, 0f);
                }

                // 4. Lantern
                var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
                if (lampPrefab != null)
                {
                    var l = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, campNook.transform);
                    l.name = "RestNook_Lamp";
                    l.transform.localPosition = new Vector3(0f, 0f, 2.5f);
                    AddSoftLanternLight(l);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Propagate] MainDungeonFloor saved with Adventurer Rest Camp Nook.");
        }
        #endregion
    }
}

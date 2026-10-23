using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Customization;
using BattleSummoner.Audio;

namespace BattleSummoner.Intro
{
    public enum CutscenePhase
    {
        Idle,
        DescentPillar,
        AwakeningStir,
        FirstInspection,
        Dialogue1_Wonder,
        Dialogue2_Thinking,
        Dialogue3_Determined,
        WorldRevealBanner,
        Completed
    }

    /// <summary>
    /// WorldIntroductionCutsceneDirector coordinates the cinematic arrival cutscene:
    /// - Spawns / frames the customized player character
    /// - Dynamic multi-shot camera choreography (Wide Descent, Hand Inspection, Expressive Portrait Close-Ups, Epic Overlook)
    /// - Expressive facial animation transitions (Wonder -> Thinking -> Determined Smile)
    /// - Real-time phonetic vowel lip-sync (A, I, U, E, O) matching spoken dialogue lines
    /// - Cinematic UI: Letterbox cinematic bars, typewriter subtitle box, speaker moniker, and Spire title card
    /// </summary>
    public class WorldIntroductionCutsceneDirector : MonoBehaviour
    {
        public static WorldIntroductionCutsceneDirector Instance { get; private set; }

        [Header("Cinematic Camera & Targets")]
        public Camera cutsceneCamera;
        public Transform cameraAnchor;
        public Transform playerSpawnAnchor;

        [Header("Character Instances / Prefabs")]
        public GameObject malePrefab;
        public GameObject femalePrefab;
        public GameObject activeCharacterInstance;
        public FacialExpressionController facialController;

        [Header("Equipment Prefabs")]
        public GameObject helmetPrefab;
        public GameObject torsoPrefab;
        public GameObject leggingsPrefab;
        public GameObject shoesPrefab;
        public GameObject swordPrefab;
        public GameObject battleAxePrefab;
        public GameObject staffPrefab;
        public GameObject wandPrefab;

        [Header("Environment VFX")]
        public ParticleSystem descentBeamParticles;
        public Light arrivalSpotlight;
        public GameObject groundRuneDecal;

        [Header("Cinematic UI Elements")]
        public Canvas cutsceneCanvas;
        public CanvasGroup letterboxBarsGroup;
        public GameObject subtitlePanel;
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueText;
        public GameObject titleCardPanel;
        public TextMeshProUGUI titleCardHeader;
        public TextMeshProUGUI titleCardSub;
        public Button skipButton;
        public Button replayButton;

        [Header("Audio")]
        public AudioSource cutsceneAudioSource;

        [Header("State")]
        public CutscenePhase currentPhase = CutscenePhase.Idle;
        public bool isCutsceneRunning = false;
        public bool autoPlayOnStart = false;

        private Coroutine cutsceneRoutine;
        private string playerName = "Summoner";
        private string playerArchetype = "Male";
        private string playerAffinity = "SOLAR_DAWN";

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            LoadPlayerData();
            if (autoPlayOnStart)
            {
                PlayIntroductionCutscene();
            }
        }

        public void LoadPlayerData()
        {
            playerName = PlayerPrefs.GetString("SummonerName", "Summoner");
            playerArchetype = PlayerPrefs.GetString("SummonerArchetype", "Male");
            playerAffinity = PlayerPrefs.GetString("SummonerAffinity", "SOLAR_DAWN");
        }

        public void PlayIntroductionCutscene(GameObject existingCharacter = null)
        {
            if (isCutsceneRunning) StopCutscene();
            cutsceneRoutine = StartCoroutine(CutsceneSequenceRoutine(existingCharacter));
        }

        public void SkipCutscene()
        {
            if (cutsceneRoutine != null) StopCoroutine(cutsceneRoutine);
            CompleteCutscene();
        }

        public void StopCutscene()
        {
            if (cutsceneRoutine != null)
            {
                StopCoroutine(cutsceneRoutine);
                cutsceneRoutine = null;
            }
            isCutsceneRunning = false;
        }

        private IEnumerator CutsceneSequenceRoutine(GameObject existingCharacter)
        {
            isCutsceneRunning = true;
            LoadPlayerData();

            // Setup Character
            bool isFemale = playerArchetype.Equals("Female", StringComparison.OrdinalIgnoreCase);

            if (existingCharacter != null)
            {
                activeCharacterInstance = existingCharacter;
            }
            else
            {
                // Check if current instance matches requested archetype
                bool needsRespawn = (activeCharacterInstance == null);
                if (activeCharacterInstance != null)
                {
                    bool instanceIsFemale = activeCharacterInstance.name.ToLower().Contains("female");
                    if (instanceIsFemale != isFemale)
                    {
                        Destroy(activeCharacterInstance);
                        activeCharacterInstance = null;
                        needsRespawn = true;
                    }
                }

                if (needsRespawn)
                {
                    var prefabToUse = isFemale ? femalePrefab : malePrefab;
                    if (prefabToUse != null)
                    {
                        Vector3 spawnPos = playerSpawnAnchor != null ? playerSpawnAnchor.position : Vector3.zero;
                        Quaternion spawnRot = playerSpawnAnchor != null ? playerSpawnAnchor.rotation : Quaternion.identity;
                        activeCharacterInstance = Instantiate(prefabToUse, spawnPos, spawnRot);
                    }
                }
            }

            if (activeCharacterInstance != null)
            {
                activeCharacterInstance.SetActive(true);

                // Apply saved modular character customization (skin tone, gauntlets, torso, leggings, boots, weapon)
                var modularApp = activeCharacterInstance.GetComponent<BattleSummoner.Customization.ModularCharacterAppearance>() ?? activeCharacterInstance.GetComponentInChildren<BattleSummoner.Customization.ModularCharacterAppearance>();
                if (modularApp != null)
                {
                    modularApp.AutoDetectRenderers();
                    modularApp.ApplyAppearance(BattleSummoner.Customization.ModularPlayerAppearanceData.LoadLocalProfile());
                }

                // Apply saved body morph proportions
                var morphCtrl = activeCharacterInstance.GetComponent<SimsBodyMorphController>();
                if (morphCtrl != null)
                {
                    morphCtrl.AutoDetectBones();
                    morphCtrl.proportions.heightScale = PlayerPrefs.GetFloat("Morph_Height", 1.0f);
                    morphCtrl.proportions.headScale = PlayerPrefs.GetFloat("Morph_Head", 1.0f);
                    morphCtrl.proportions.shoulderWidth = PlayerPrefs.GetFloat("Morph_Shoulders", 1.0f);
                    morphCtrl.proportions.chestScale = PlayerPrefs.GetFloat("Morph_Chest", 1.0f);
                    morphCtrl.proportions.waistWidth = PlayerPrefs.GetFloat("Morph_Waist", 1.0f);
                    morphCtrl.proportions.hipWidth = PlayerPrefs.GetFloat("Morph_Hips", 1.0f);
                    morphCtrl.proportions.armThickness = PlayerPrefs.GetFloat("Morph_Arms", 1.0f);
                    morphCtrl.proportions.legThickness = PlayerPrefs.GetFloat("Morph_Legs", 1.0f);
                    morphCtrl.proportions.muscleMass_4D = PlayerPrefs.GetFloat("Morph_Muscle", 25f);
                    morphCtrl.ApplyProportions();
                }

                // Apply saved equipment
                var equipMgr = activeCharacterInstance.GetComponent<ModularEquipmentManager>();
                if (equipMgr != null)
                {
                    equipMgr.AutoDetectSockets();
                    ApplySavedEquipment(equipMgr);
                }

                facialController = activeCharacterInstance.GetComponent<FacialExpressionController>();
                if (facialController == null)
                    facialController = activeCharacterInstance.AddComponent<FacialExpressionController>();
                facialController.InitializeRenderer();
                facialController.SetExpression(ExpressionPreset.Neutral, 0f);
            }

            if (cutsceneCanvas != null) cutsceneCanvas.gameObject.SetActive(true);
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            if (titleCardPanel != null) titleCardPanel.SetActive(false);
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(true);
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(SkipCutscene);
            }

            // ==========================================
            // ACT 1: DESCENT PILLAR OF LIGHT
            // ==========================================
            currentPhase = CutscenePhase.DescentPillar;
            if (letterboxBarsGroup != null) StartCoroutine(FadeCanvasGroup(letterboxBarsGroup, 0f, 1f, 0.8f));
            if (descentBeamParticles != null) descentBeamParticles.Play();
            if (arrivalSpotlight != null) arrivalSpotlight.intensity = 5.0f;
            LoginAudioSynthesizer.Instance?.PlayWarpWhoosh();

            // Camera wide aerial shot looking down at dais
            Vector3 targetHeadPos = activeCharacterInstance != null ?
                activeCharacterInstance.transform.position + new Vector3(0, 1.55f, 0) : new Vector3(0, 1.55f, 0);

            if (cutsceneCamera != null)
            {
                cutsceneCamera.transform.position = targetHeadPos + new Vector3(0f, 2.2f, -3.8f);
                cutsceneCamera.transform.LookAt(targetHeadPos);
            }

            yield return new WaitForSeconds(1.8f);

            // ==========================================
            // ACT 2: AWAKENING & HAND INSPECTION
            // ==========================================
            currentPhase = CutscenePhase.AwakeningStir;
            if (arrivalSpotlight != null) StartCoroutine(FadeLightIntensity(arrivalSpotlight, 5f, 2.2f, 1.5f));

            // Camera glides down to medium front angle
            float t = 0f;
            Vector3 camStart = cutsceneCamera != null ? cutsceneCamera.transform.position : Vector3.zero;
            Vector3 camMed = targetHeadPos + new Vector3(0.35f, -0.2f, -1.8f);

            while (t < 2.0f)
            {
                t += Time.deltaTime;
                float norm = Mathf.SmoothStep(0f, 1f, t / 2.0f);
                if (cutsceneCamera != null)
                {
                    cutsceneCamera.transform.position = Vector3.Lerp(camStart, camMed, norm);
                    cutsceneCamera.transform.LookAt(targetHeadPos + new Vector3(0f, -0.1f, 0f));
                }
                yield return null;
            }

            // ==========================================
            // ACT 3: PORTRAIT CLOSE-UP & DIALOGUE 1 (WONDER / SHOCKED)
            // ==========================================
            currentPhase = CutscenePhase.Dialogue1_Wonder;
            Vector3 camPortrait = targetHeadPos + new Vector3(0.08f, -0.02f, 0.72f);
            if (cutsceneCamera != null)
            {
                cutsceneCamera.transform.position = camPortrait;
                cutsceneCamera.transform.LookAt(targetHeadPos + new Vector3(0f, 0.02f, 0f));
            }

            // Facial expression: Wonder / Shocked
            if (facialController != null)
            {
                facialController.SetExpression(ExpressionPreset.Surprised, 0.35f);
            }

            // Speak Dialogue Line 1 with Vowel Lip Sync
            string line1 = "System link established... Where... am I?";
            yield return StartCoroutine(PlayDialogueLine(playerName, line1, 2.8f));
            yield return new WaitForSeconds(0.4f);

            // ==========================================
            // ACT 4: DIALOGUE 2 (THINKING / INQUISITIVE)
            // ==========================================
            currentPhase = CutscenePhase.Dialogue2_Thinking;
            // Slight camera drift to the side
            if (cutsceneCamera != null)
            {
                cutsceneCamera.transform.position = targetHeadPos + new Vector3(-0.35f, 0.05f, 0.75f);
                cutsceneCamera.transform.LookAt(targetHeadPos + new Vector3(0f, 0.02f, 0f));
            }

            // Expression: Thinking (one brow cocked high, slight squint)
            if (facialController != null)
            {
                facialController.SetExpression(ExpressionPreset.Thinking, 0.3f);
            }

            string line2 = "Is this the gateway of the Hundredfold Spire?";
            yield return StartCoroutine(PlayDialogueLine(playerName, line2, 2.6f));
            yield return new WaitForSeconds(0.4f);

            // ==========================================
            // ACT 5: DIALOGUE 3 (DETERMINED & CONFIDENT SMILE)
            // ==========================================
            currentPhase = CutscenePhase.Dialogue3_Determined;
            // Closer portrait punch
            if (cutsceneCamera != null)
            {
                cutsceneCamera.transform.position = targetHeadPos + new Vector3(0f, 0f, 0.62f);
                cutsceneCamera.transform.LookAt(targetHeadPos + new Vector3(0f, 0.02f, 0f));
            }

            // Expression: Determined Smile
            if (facialController != null)
            {
                facialController.SetExpression(ExpressionPreset.Determined, 0.25f);
                facialController.mouthSmile = 0.55f;
            }

            LoginAudioSynthesizer.Instance?.PlayInductionFanfare();
            string line3 = "I'm ready. Let's conquer this world!";
            yield return StartCoroutine(PlayDialogueLine(playerName, line3, 2.5f));
            yield return new WaitForSeconds(0.6f);

            // ==========================================
            // ACT 6: WORLD REVEAL & TITLE BANNER
            // ==========================================
            currentPhase = CutscenePhase.WorldRevealBanner;
            if (subtitlePanel != null) subtitlePanel.SetActive(false);

            // Dramatic over-the-shoulder scenic sweep
            Vector3 camOverlook = targetHeadPos + new Vector3(-0.45f, 0.25f, -1.25f);
            Vector3 scenicFocus = targetHeadPos + new Vector3(0f, 0.35f, 5.0f);

            t = 0f;
            Vector3 curCamPos = cutsceneCamera != null ? cutsceneCamera.transform.position : Vector3.zero;
            while (t < 2.0f)
            {
                t += Time.deltaTime;
                float norm = Mathf.SmoothStep(0f, 1f, t / 2.0f);
                if (cutsceneCamera != null)
                {
                    cutsceneCamera.transform.position = Vector3.Lerp(curCamPos, camOverlook, norm);
                    cutsceneCamera.transform.LookAt(scenicFocus);
                }
                yield return null;
            }

            // Display Title Card
            if (titleCardPanel != null)
            {
                titleCardPanel.SetActive(true);
                if (titleCardHeader != null)
                    titleCardHeader.text = $"<b><color=#FACC15>✦ SUMMONER {playerName.ToUpper()} ✦</color></b>";
                if (titleCardSub != null)
                    titleCardSub.text = $"<color=#38BDF8>DIMENSIONAL AFFINITY: {playerAffinity}</color> • <color=#4ADE80>NEURAL LINK 100%</color>\n<size=12><color=#CBD5E1>WELCOME TO THE HUNDREDFOLD SPIRE</color></size>";
            }

            yield return new WaitForSeconds(3.0f);

            CompleteCutscene();
        }

        private IEnumerator PlayDialogueLine(string speaker, string sentence, float duration)
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(true);
            if (speakerNameText != null) speakerNameText.text = $"<b><color=#FACC15>{speaker.ToUpper()}</color></b>";
            if (dialogueText != null) dialogueText.text = "";

            // Animate mouth vowels via FacialExpressionController & Blender baked actions
            if (facialController != null)
            {
                facialController.SpeakDialogue(sentence, duration);

                if (sentence.Contains("Where") || sentence.Contains("established"))
                    facialController.PlayBlenderAction("Action_Dialogue_Intro_WhereAmI");
                else if (sentence.Contains("gateway") || sentence.Contains("Spire"))
                    facialController.PlayBlenderAction("Action_Dialogue_Thinking_Spire");
                else if (sentence.Contains("ready") || sentence.Contains("conquer"))
                    facialController.PlayBlenderAction("Action_Dialogue_Determined_Conquer");
            }

            // Typewriter effect on dialogue text
            float charDelay = Mathf.Clamp(duration / (sentence.Length + 4), 0.02f, 0.06f);
            for (int i = 0; i <= sentence.Length; i++)
            {
                if (dialogueText != null) dialogueText.text = sentence.Substring(0, i);
                yield return new WaitForSeconds(charDelay);
            }

            float remaining = duration - (charDelay * sentence.Length);
            if (remaining > 0) yield return new WaitForSeconds(remaining);
        }

        [Header("Open World Transition Target")]
        public string targetOpenWorldSceneName = "OverworldMap";

        private void CompleteCutscene()
        {
            currentPhase = CutscenePhase.Completed;
            isCutsceneRunning = false;

            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            if (titleCardPanel != null) titleCardPanel.SetActive(false);
            if (letterboxBarsGroup != null) StartCoroutine(FadeCanvasGroup(letterboxBarsGroup, 1f, 0f, 0.6f));
            if (skipButton != null) skipButton.gameObject.SetActive(false);

            if (facialController != null)
            {
                facialController.SetExpression(ExpressionPreset.Joy, 0.3f);
                facialController.PlayBlenderAction("Action_Expression_Joy_Smile");
            }

            Debug.Log("[INTRO CUTSCENE] Cutscene successfully completed. Transitioning to open world: " + targetOpenWorldSceneName);
            StartCoroutine(TransitionToOpenWorldRoutine());
        }

        private IEnumerator TransitionToOpenWorldRoutine()
        {
            yield return new WaitForSeconds(0.75f);

            var faderObj = GameObject.Find("SAO_ScreenFadeOverlay");
            Image fadeImg = null;
            if (faderObj != null) fadeImg = faderObj.GetComponent<Image>();

            if (fadeImg != null)
            {
                fadeImg.gameObject.SetActive(true);
                float duration = 0.60f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    fadeImg.color = new Color(0.02f, 0.04f, 0.08f, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.35f);
            }

            string worldScene = !string.IsNullOrEmpty(targetOpenWorldSceneName) ? targetOpenWorldSceneName : "OverworldMap";
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene(worldScene, "Spawn_CitadelCenter", "Arriving at Citadel Spire Sanctum...");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(worldScene);
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
        {
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / dur);
                yield return null;
            }
            cg.alpha = to;
        }

        private IEnumerator FadeLightIntensity(Light l, float from, float to, float dur)
        {
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                l.intensity = Mathf.Lerp(from, to, elapsed / dur);
                yield return null;
            }
            l.intensity = to;
        }

        private void ApplySavedEquipment(ModularEquipmentManager equipMgr)
        {
            if (equipMgr == null) return;

            if (PlayerPrefs.GetInt("Equip_Helmet", 0) == 1 && helmetPrefab != null)
            {
                equipMgr.Equip(new EquipmentItemDefinition { itemId = "helmet", itemName = "HELMET", slot = EquipmentSlot.Helmet, prefab = helmetPrefab });
            }
            if (PlayerPrefs.GetInt("Equip_Torso", 0) == 1 && torsoPrefab != null)
            {
                equipMgr.Equip(new EquipmentItemDefinition { itemId = "torso", itemName = "TORSO", slot = EquipmentSlot.Torso, prefab = torsoPrefab });
            }
            if (PlayerPrefs.GetInt("Equip_Leggings", 0) == 1 && leggingsPrefab != null)
            {
                equipMgr.Equip(new EquipmentItemDefinition { itemId = "leggings", itemName = "LEGGINGS", slot = EquipmentSlot.Leggings, prefab = leggingsPrefab });
            }
            if (PlayerPrefs.GetInt("Equip_Shoes", 0) == 1 && shoesPrefab != null)
            {
                equipMgr.Equip(new EquipmentItemDefinition { itemId = "shoes", itemName = "SHOES", slot = EquipmentSlot.Shoes, prefab = shoesPrefab });
            }

            string weapon = PlayerPrefs.GetString("Equip_Weapon", "sword");
            GameObject wPrefab = weapon switch
            {
                "axe" => battleAxePrefab,
                "sword" => swordPrefab,
                "staff" => staffPrefab,
                "wand" => wandPrefab,
                _ => null
            };
            if (wPrefab != null)
            {
                bool is2H = weapon == "axe" || weapon == "staff";
                equipMgr.Equip(new EquipmentItemDefinition { itemId = weapon, itemName = weapon.ToUpper(), slot = EquipmentSlot.MainHandWeapon, prefab = wPrefab, isTwoHanded = is2H });
            }
        }
    }
}

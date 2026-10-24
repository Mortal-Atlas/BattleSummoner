using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BattleSummoner.World;
using BattleSummoner.Combat;
using BattleSummoner.Quests;

namespace BattleSummoner.Core
{
    /// <summary>
    /// QualityOfLifeMasterController provides high-impact player accessibility and QoL systems:
    /// - Universal Pause & Master Menu [ESC / P] with Fast Travel Waypoints, In-Pause Settings, Controls Cheatsheet.
    /// - Contextual Proximity Smart Interaction Prompter [E] for NPCs, Doors, Rifts, and Task Boards.
    /// - 3D Cardinal Compass & Dynamic District Heading Banner tracking camera orientation.
    /// - Hands-Free Auto-Run Toggle [R / NumLock] with on-screen status indicator.
    /// - Cinematic Photo Mode [F1] with instant HUD hiding and watermark.
    /// - Quick Save [F5] & Quick Load [F9] with on-screen notification toasts.
    /// - Persistent across all scenes via DontDestroyOnLoad.
    /// </summary>
    public class QualityOfLifeMasterController : MonoBehaviour
    {
        private static QualityOfLifeMasterController _instance;
        public static QualityOfLifeMasterController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<QualityOfLifeMasterController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("QualityOfLifeMasterController");
                        _instance = go.AddComponent<QualityOfLifeMasterController>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureQoLMasterRuntime()
        {
            if (_instance == null)
            {
                _ = Instance;
            }
        }

        [Header("Keybindings")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode altPauseKey = KeyCode.P;
        [SerializeField] private KeyCode mapKey = KeyCode.M;
        [SerializeField] private KeyCode settingsKey = KeyCode.O;
        [SerializeField] private KeyCode controlsKey = KeyCode.H;
        [SerializeField] private KeyCode photoModeKey = KeyCode.F1;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode quickSaveKey = KeyCode.F5;
        [SerializeField] private KeyCode quickLoadKey = KeyCode.F9;

        [Header("UI Canvas Root")]
        private Canvas qolCanvas;
        private CanvasGroup pauseOverlayGroup;
        private GameObject pauseModalObj;
        private GameObject mainMenuContainerObj;
        private GameObject fastTravelPanelObj;
        private GameObject settingsPanelObj;
        private GameObject controlsGuidePanelObj;
        private GameObject interactionPromptPillObj;
        private TextMeshProUGUI interactionPromptText;
        private GameObject autoRunBadgeObj;
        private GameObject compassBannerObj;
        private TextMeshProUGUI compassHeadingText;
        private TextMeshProUGUI compassDistrictText;
        private GameObject photoModeWatermarkObj;
        private GameObject toastBannerObj;
        private TextMeshProUGUI toastBannerText;
        private Coroutine toastCoroutine;

        [Header("Settings UI Controls")]
        private Slider masterVolSlider;
        private TextMeshProUGUI masterVolValText;
        private Slider musicVolSlider;
        private TextMeshProUGUI musicVolValText;
        private Slider sfxVolSlider;
        private TextMeshProUGUI sfxVolValText;
        private TextMeshProUGUI fpsCapBtnText;
        private Slider cameraSensSlider;
        private TextMeshProUGUI cameraSensValText;
        private TextMeshProUGUI invertYBtnText;
        private Slider fovSlider;
        private TextMeshProUGUI fovValText;

        [Header("State")]
        private bool isPaused = false;
        private bool isPhotoMode = false;
        private Transform playerTransform;
        private PlayerMovementController playerMovement;
        private OverworldNPCInteractable nearestNPC;
        private BuildingEntranceTrigger nearestDoorway;
        private OtherworldlyPortalOutpost nearestPortal;
        private GuildTaskBoardTrigger nearestTaskBoard;
        private float nextProximityScanTime = 0f;

        // Settings State
        private int currentFpsTarget = 60;
        private bool invertY = false;
        private float cameraSens = 1.2f;

        // Waypoints for Fast Travel
        public struct Waypoint
        {
            public string name;
            public string district;
            public Vector3 position;
            public float yaw;
            public string icon;

            public Waypoint(string name, string district, Vector3 pos, float yaw, string icon)
            {
                this.name = name;
                this.district = district;
                this.position = pos;
                this.yaw = yaw;
                this.icon = icon;
            }
        }

        private readonly List<Waypoint> waypoints = new List<Waypoint>
        {
            new Waypoint("Citadel Acropolis", "District I • High Spire", new Vector3(0f, 80.05f, -55f), 0f, "[I]"),
            new Waypoint("High Market Promenade", "District II • Grand Bazaar", new Vector3(15f, 58.0f, -170f), 45f, "[II]"),
            new Waypoint("Crafters & Forge Terrace", "District III • Blacksmiths", new Vector3(-225f, 38.0f, 142f), 110f, "[III]"),
            new Waypoint("The Wandering Wyvern Tavern", "District III • Hospitality", new Vector3(-220f, 38.0f, 140f), 90f, "[III]"),
            new Waypoint("Summoner's Guildhall", "District III • Arcane Quarter", new Vector3(-205f, 38.0f, 180f), 280f, "[III]"),
            new Waypoint("Adventurers' Campfire Hub", "District III • Travelers Haven", new Vector3(-215f, 38.0f, 152f), 100f, "[III]"),
            new Waypoint("Alchemist & Apothecary", "District III • Herbalist Rows", new Vector3(-218f, 38.0f, 162f), 275f, "[III]"),
            new Waypoint("Harbor Vista Promenade", "District V • Ocean Pier", new Vector3(350f, 22.0f, 10f), 90f, "[V]"),
            new Waypoint("Ancient Southern Gate", "Southern Frontier", new Vector3(0f, 6.0f, -540f), 180f, "[GATE]"),
            new Waypoint("Otherworldly Rift Outpost", "Northern Grove Outpost", new Vector3(-440f, 84.5f, 835f), 0f, "[RIFT]"),
            new Waypoint("The Under-Citadel (Boss Dungeon)", "Dungeon Dimension", new Vector3(0f, 0f, 0f), 0f, "[BOSS]")
        };

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            LoadSavedSettings();
            BuildQoLUICanvas();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            FindPlayer();
            PlayerMovementController.OnAutoRunChanged += HandleAutoRunChanged;
        }

        private void OnDestroy()
        {
            PlayerMovementController.OnAutoRunChanged -= HandleAutoRunChanged;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (isPaused) Time.timeScale = 1f;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindPlayer();
            if (isPaused) TogglePause();

            // Refresh compass visibility based on scene
            bool isOverworld = scene.name == "OverworldMap";
            if (compassBannerObj != null)
            {
                compassBannerObj.SetActive(isOverworld && !isPhotoMode);
            }
        }

        private void FindPlayer()
        {
            var pObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Hero");
            if (pObj != null)
            {
                playerTransform = pObj.transform;
                playerMovement = pObj.GetComponent<PlayerMovementController>();
            }
        }

        private void Update()
        {
            // Re-acquire player if needed
            if (playerTransform == null)
            {
                FindPlayer();
            }

            // 1. Pause Menu Toggle [ESC / P]
            if (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(altPauseKey))
            {
                TogglePause();
            }

            // 2. Fast Travel / Map Shortcut [M]
            if (Input.GetKeyDown(mapKey) && !isPaused)
            {
                OpenFastTravelDirectly();
            }

            // 3. Settings Shortcut [O]
            if (Input.GetKeyDown(settingsKey) && !isPaused)
            {
                if (!isPaused) TogglePause();
                ToggleSettings();
            }

            // 4. Controls Shortcut [H]
            if (Input.GetKeyDown(controlsKey) && !isPaused)
            {
                if (!isPaused) TogglePause();
                ToggleControlsGuide();
            }

            // 5. Photo Mode Toggle [F1]
            if (Input.GetKeyDown(photoModeKey))
            {
                TogglePhotoMode();
            }

            // 6. Quick Save [F5] & Quick Load [F9]
            if (Input.GetKeyDown(quickSaveKey))
            {
                ExecuteQuickSave();
            }
            if (Input.GetKeyDown(quickLoadKey))
            {
                ExecuteQuickLoad();
            }

            // Don't process world interact / compass if paused or in photo mode
            if (isPaused || isPhotoMode) return;

            // 7. Update 3D Compass & Heading
            UpdateCompassAndDistrict();

            // 8. Proximity Smart Interaction Scan
            if (playerTransform != null && Time.time >= nextProximityScanTime)
            {
                nextProximityScanTime = Time.time + 0.12f;
                ScanNearbyInteractables();
            }

            // 9. Handle Smart Interaction Press [E]
            if (Input.GetKeyDown(interactKey))
            {
                ExecuteNearestInteraction();
            }
        }

        #region Universal Pause & Menu System
        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;

            if (pauseOverlayGroup != null)
            {
                pauseOverlayGroup.alpha = isPaused ? 1f : 0f;
                pauseOverlayGroup.blocksRaycasts = isPaused;
                pauseOverlayGroup.interactable = isPaused;
                pauseModalObj.SetActive(isPaused);
            }

            if (isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (mainMenuContainerObj != null) mainMenuContainerObj.SetActive(true);
                if (fastTravelPanelObj != null) fastTravelPanelObj.SetActive(false);
                if (settingsPanelObj != null) settingsPanelObj.SetActive(false);
                if (controlsGuidePanelObj != null) controlsGuidePanelObj.SetActive(false);
                
                var spellPalette = GameObject.Find("SpellSlingerPalette");
                if (spellPalette != null) spellPalette.SetActive(false);
                var devHud = GameObject.Find("DevLoadout_HUD_Root");
                if (devHud != null) devHud.SetActive(false);

                PlaySound("bell_chime", 0.8f);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                var spellPalette = GameObject.Find("SpellSlingerPalette");
                if (spellPalette != null) spellPalette.SetActive(true);

                PlaySound("swoosh_air", 0.7f);
            }
        }

        public void ResumeGame()
        {
            if (isPaused) TogglePause();
        }

        public void OpenFastTravelDirectly()
        {
            if (!isPaused) TogglePause();
            if (mainMenuContainerObj != null) mainMenuContainerObj.SetActive(false);
            if (fastTravelPanelObj != null) fastTravelPanelObj.SetActive(true);
            if (settingsPanelObj != null) settingsPanelObj.SetActive(false);
            if (controlsGuidePanelObj != null) controlsGuidePanelObj.SetActive(false);
            PlaySound("click", 0.7f);
        }

        public void ToggleSettings()
        {
            if (settingsPanelObj != null)
            {
                bool active = !settingsPanelObj.activeSelf;
                settingsPanelObj.SetActive(active);
                if (mainMenuContainerObj != null) mainMenuContainerObj.SetActive(!active);
                if (fastTravelPanelObj != null) fastTravelPanelObj.SetActive(false);
                if (controlsGuidePanelObj != null) controlsGuidePanelObj.SetActive(false);
                PlaySound("click", 0.7f);
            }
        }

        public void ToggleControlsGuide()
        {
            if (controlsGuidePanelObj != null)
            {
                bool active = !controlsGuidePanelObj.activeSelf;
                controlsGuidePanelObj.SetActive(active);
                if (mainMenuContainerObj != null) mainMenuContainerObj.SetActive(!active);
                if (fastTravelPanelObj != null) fastTravelPanelObj.SetActive(false);
                if (settingsPanelObj != null) settingsPanelObj.SetActive(false);
                PlaySound("click", 0.7f);
            }
        }

        public void ReturnToTitleScreen()
        {
            Time.timeScale = 1f;
            isPaused = false;
            if (pauseOverlayGroup != null)
            {
                pauseOverlayGroup.alpha = 0f;
                pauseOverlayGroup.blocksRaycasts = false;
            }
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene("WorldEntryExperience", "Title_Start", "Title Screen");
            }
            else
            {
                SceneManager.LoadScene("WorldEntryExperience");
            }
        }
        #endregion

        #region Fast Travel Execution
        public void FastTravelTo(Waypoint wp)
        {
            // Case 1: Boss Dungeon Warp
            if (wp.name.Contains("Boss Dungeon"))
            {
                TogglePause();
                ShowToast("Warping into The Under-Citadel Boss Dungeon...");
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.TransitionToScene("MainDungeonFloor", "DungeonDimension_Entry", "The Under-Citadel Boss Dungeon");
                }
                else
                {
                    SceneManager.LoadScene("MainDungeonFloor");
                }
                return;
            }

            // Case 2: Campfire Hub / CampHub Direct Scene Warp
            if (wp.name.Contains("Campfire Hub") || wp.name.Contains("CampHub"))
            {
                TogglePause();
                ShowToast("Resting at the Adventurers' Campfire Hub...");
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.PitchCamp("Adventurers' Campfire Hub");
                }
                else
                {
                    SceneManager.LoadScene("CampfireHub");
                }
                return;
            }

            // Case 3: Player is currently in an interior or dungeon and picks an Overworld landmark
            if (SceneManager.GetActiveScene().name != "OverworldMap")
            {
                TogglePause();
                ShowToast($"Departing to {wp.name}...");
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.TransitionToScene("OverworldMap", null, wp.name, wp.position);
                }
                else
                {
                    SceneManager.LoadScene("OverworldMap");
                }
                return;
            }

            // Case 4: In OverworldMap, teleport character controller directly
            if (playerTransform == null) FindPlayer();
            if (playerTransform == null) return;

            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = wp.position;
            playerTransform.rotation = Quaternion.Euler(0f, wp.yaw, 0f);

            if (cc != null) cc.enabled = true;

            // Snap camera
            var cam = Camera.main;
            if (cam != null)
            {
                var arpg = cam.GetComponent<PortraitARPGCamera>();
                if (arpg != null)
                {
                    arpg.yawAngle = wp.yaw;
                    arpg.SnapToTarget();
                }
            }

            PlaySound("portal_warp", 1.0f);
            PlaySound("cinematic_chime", 0.9f);
            ShowToast($"Arrived at {wp.name} ({wp.district})");

            // Unpause and close menu if paused
            if (isPaused) TogglePause();
        }
        #endregion

        #region Smart Interaction Prompter
        private void ScanNearbyInteractables()
        {
            if (playerTransform == null) return;

            nearestNPC = null;
            nearestDoorway = null;
            nearestPortal = null;
            nearestTaskBoard = null;
            float closestDist = 4.2f;

            // 1. Check NPCs
            var npcs = FindObjectsByType<OverworldNPCInteractable>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (!npc.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(playerTransform.position, npc.transform.position);
                if (d < closestDist && d <= npc.interactionRadius + 1.0f)
                {
                    closestDist = d;
                    nearestNPC = npc;
                }
            }

            // 2. Check Doorways
            var doors = FindObjectsByType<BuildingEntranceTrigger>(FindObjectsSortMode.None);
            foreach (var door in doors)
            {
                if (!door.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(playerTransform.position, door.transform.position);
                if (d < closestDist && d <= 4.0f)
                {
                    closestDist = d;
                    nearestDoorway = door;
                    nearestNPC = null;
                }
            }

            // 3. Check Dimensional Portals
            var portals = FindObjectsByType<OtherworldlyPortalOutpost>(FindObjectsSortMode.None);
            foreach (var portal in portals)
            {
                if (!portal.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(playerTransform.position, portal.transform.position);
                if (d < closestDist && d <= 5.5f)
                {
                    closestDist = d;
                    nearestPortal = portal;
                    nearestNPC = null;
                    nearestDoorway = null;
                }
            }

            // 4. Check Guild Task Boards
            var boards = FindObjectsByType<GuildTaskBoardTrigger>(FindObjectsSortMode.None);
            foreach (var board in boards)
            {
                if (!board.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(playerTransform.position, board.transform.position);
                if (d < closestDist && d <= 4.5f)
                {
                    closestDist = d;
                    nearestTaskBoard = board;
                    nearestNPC = null;
                    nearestDoorway = null;
                    nearestPortal = null;
                }
            }

            // Update on-screen prompt pill
            if (interactionPromptPillObj != null)
            {
                if (nearestNPC != null)
                {
                    interactionPromptPillObj.SetActive(true);
                    interactionPromptText.text = $"<color=#FFE060><b>[E] TALK</b></color>  <color=#FFFFFF>{nearestNPC.npcName}</color>  <size=80%><color=#90D0FF>〈 {nearestNPC.npcRole} 〉</color></size>";
                }
                else if (nearestDoorway != null)
                {
                    interactionPromptPillObj.SetActive(true);
                    interactionPromptText.text = $"<color=#FFE060><b>[E] ENTER</b></color>  <color=#FFFFFF>{nearestDoorway.locationTitle}</color>";
                }
                else if (nearestPortal != null)
                {
                    interactionPromptPillObj.SetActive(true);
                    interactionPromptText.text = $"<color=#FFE060><b>[E] ENTER</b></color>  <color=#D080FF>Dimensional Rift (Under-Citadel)</color>";
                }
                else if (nearestTaskBoard != null)
                {
                    interactionPromptPillObj.SetActive(true);
                    interactionPromptText.text = $"<color=#FFE060><b>[E] VIEW</b></color>  <color=#FFD700>Adventurers' Guild Task Board</color>";
                }
                else
                {
                    interactionPromptPillObj.SetActive(false);
                }
            }
        }

        public void ExecuteNearestInteraction()
        {
            if (nearestNPC != null)
            {
                nearestNPC.TriggerInteraction();
                PlaySound("dialogue_start", 0.8f);
            }
            else if (nearestDoorway != null)
            {
                nearestDoorway.TriggerEntranceTransition();
                PlaySound("door_creak", 0.9f);
            }
            else if (nearestPortal != null)
            {
                nearestPortal.EnterDungeonDimension();
            }
            else if (nearestTaskBoard != null)
            {
                nearestTaskBoard.InteractWithBoard();
            }
        }
        #endregion

        #region 3D Compass & Heading
        private void UpdateCompassAndDistrict()
        {
            if (Camera.main == null || compassHeadingText == null) return;

            float yaw = Camera.main.transform.eulerAngles.y;
            string headingStr = GetCardinalDirection(yaw);
            compassHeadingText.text = $"<color=#FFE570><b>{headingStr}</b></color>  <size=75%><color=#B0D0FF>{Mathf.RoundToInt(yaw)}°</color></size>";

            // Determine District
            if (compassDistrictText != null && playerTransform != null)
            {
                Vector3 pos = playerTransform.position;
                string district = "Metropolis Outer Territory";

                if (pos.y > 72f) district = "Citadel Acropolis • Sanctum";
                else if (pos.z > 700f) district = "Northern Grove • Otherworldly Rift Outpost";
                else if (pos.z > 80f && pos.y > 32f) district = "High Market Promenade • Tier II";
                else if (pos.x < -140f && pos.y > 32f) district = "Crafters & Blacksmiths Terrace • Tier II";
                else if (pos.z < -140f && pos.y > 32f) district = "Royal Residential Quarter • Tier II";
                else if (pos.x > 250f) district = "Eastern Harbor Vista • Promenade";
                else if (pos.z < -450f) district = "Southern Mountain Gate • Dungeon Approach";
                else if (pos.y > 15f) district = "Central Grand Boulevard • Tier I";

                compassDistrictText.text = district;
            }
        }

        private string GetCardinalDirection(float degrees)
        {
            degrees = (degrees % 360 + 360) % 360;
            if (degrees >= 337.5f || degrees < 22.5f) return "N";
            if (degrees >= 22.5f && degrees < 67.5f) return "NE";
            if (degrees >= 67.5f && degrees < 112.5f) return "E";
            if (degrees >= 112.5f && degrees < 157.5f) return "SE";
            if (degrees >= 157.5f && degrees < 202.5f) return "S";
            if (degrees >= 202.5f && degrees < 247.5f) return "SW";
            if (degrees >= 247.5f && degrees < 292.5f) return "W";
            return "NW";
        }
        #endregion

        #region Auto-Run & Photo Mode
        private void HandleAutoRunChanged(bool active)
        {
            if (autoRunBadgeObj != null)
            {
                autoRunBadgeObj.SetActive(active);
                if (active) PlaySound("dash_wind", 0.7f);
            }
        }

        public void TogglePhotoMode()
        {
            isPhotoMode = !isPhotoMode;

            // Toggle all other canvases and non-QoL UI docks
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c == qolCanvas) continue;
                c.enabled = !isPhotoMode;
            }

            var spellPalette = GameObject.Find("SpellSlingerPalette");
            if (spellPalette != null) spellPalette.SetActive(!isPhotoMode);
            var devHud = GameObject.Find("DevLoadout_HUD_Root");
            if (devHud != null) devHud.SetActive(!isPhotoMode);

            if (compassBannerObj != null) compassBannerObj.SetActive(!isPhotoMode);
            if (interactionPromptPillObj != null) interactionPromptPillObj.SetActive(!isPhotoMode);
            if (autoRunBadgeObj != null && playerMovement != null) autoRunBadgeObj.SetActive(!isPhotoMode && playerMovement.IsAutoRunning);
            if (photoModeWatermarkObj != null) photoModeWatermarkObj.SetActive(isPhotoMode);

            PlaySound(isPhotoMode ? "camera_shutter" : "swoosh_air", 0.9f);
            ShowToast(isPhotoMode ? "[PHOTO] Photo Mode Active [F1 to Exit]" : "[PHOTO] Photo Mode Exited");
        }
        #endregion

        #region Quick Save & Quick Load
        private void ExecuteQuickSave()
        {
            if (playerTransform != null)
            {
                PlayerPrefs.SetFloat("BS_Save_PosX", playerTransform.position.x);
                PlayerPrefs.SetFloat("BS_Save_PosY", playerTransform.position.y);
                PlayerPrefs.SetFloat("BS_Save_PosZ", playerTransform.position.z);
                PlayerPrefs.SetFloat("BS_Save_Yaw", playerTransform.eulerAngles.y);
                PlayerPrefs.SetString("BS_Save_Scene", SceneManager.GetActiveScene().name);
                PlayerPrefs.Save();

                PlaySound("bell_chime", 0.9f);
                ShowToast("[SAVE] Quick Save Completed [F5]");
            }
        }

        private void ExecuteQuickLoad()
        {
            if (PlayerPrefs.HasKey("BS_Save_Scene"))
            {
                string savedScene = PlayerPrefs.GetString("BS_Save_Scene");
                if (savedScene == SceneManager.GetActiveScene().name && playerTransform != null)
                {
                    var cc = playerTransform.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;

                    float x = PlayerPrefs.GetFloat("BS_Save_PosX");
                    float y = PlayerPrefs.GetFloat("BS_Save_PosY");
                    float z = PlayerPrefs.GetFloat("BS_Save_PosZ");
                    float yaw = PlayerPrefs.GetFloat("BS_Save_Yaw");

                    playerTransform.position = new Vector3(x, y, z);
                    playerTransform.rotation = Quaternion.Euler(0f, yaw, 0f);

                    if (cc != null) cc.enabled = true;

                    PlaySound("portal_warp", 0.9f);
                    ShowToast("[LOAD] Quick Load Restored [F9]");
                }
                else
                {
                    ShowToast($"[LOAD] Reloading saved scene: {savedScene}...");
                    SceneManager.LoadScene(savedScene);
                }
            }
            else
            {
                ShowToast("No Quick Save file found! Press [F5] to save.");
            }
        }
        #endregion

        #region Toast Notification System
        public void ShowToast(string message, float duration = 2.8f)
        {
            if (toastBannerObj == null || toastBannerText == null) return;

            toastBannerText.text = message;
            toastBannerObj.SetActive(true);

            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(ToastTimerRoutine(duration));
        }

        private IEnumerator ToastTimerRoutine(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            if (toastBannerObj != null) toastBannerObj.SetActive(false);
            toastCoroutine = null;
        }
        #endregion

        #region Settings Management
        private void LoadSavedSettings()
        {
            float masterVol = PlayerPrefs.GetFloat("BS_MasterVolume", 1.0f);
            AudioListener.volume = masterVol;

            currentFpsTarget = PlayerPrefs.GetInt("BS_FpsTarget", 60);
            Application.targetFrameRate = currentFpsTarget;

            cameraSens = PlayerPrefs.GetFloat("BS_CameraSens", 1.2f);
            invertY = PlayerPrefs.GetInt("BS_InvertY", 0) == 1;
        }

        public void SetMasterVolume(float val)
        {
            AudioListener.volume = val;
            PlayerPrefs.SetFloat("BS_MasterVolume", val);
            if (masterVolValText != null) masterVolValText.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        public void SetMusicVolume(float val)
        {
            PlayerPrefs.SetFloat("BS_MusicVolume", val);
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.SetMusicVolume(val);
            }
            if (musicVolValText != null) musicVolValText.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        public void SetSFXVolume(float val)
        {
            PlayerPrefs.SetFloat("BS_SfxVolume", val);
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.SetSFXVolume(val);
            }
            if (sfxVolValText != null) sfxVolValText.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        public void CycleTargetFps()
        {
            if (currentFpsTarget == 30) currentFpsTarget = 60;
            else if (currentFpsTarget == 60) currentFpsTarget = 120;
            else if (currentFpsTarget == 120) currentFpsTarget = -1;
            else currentFpsTarget = 30;

            Application.targetFrameRate = currentFpsTarget;
            PlayerPrefs.SetInt("BS_FpsTarget", currentFpsTarget);

            if (fpsCapBtnText != null)
            {
                fpsCapBtnText.text = currentFpsTarget == -1 ? "FPS: Uncapped" : $"FPS: {currentFpsTarget} FPS";
            }
            PlaySound("click", 0.7f);
        }

        public void SetCameraSensitivity(float val)
        {
            cameraSens = val;
            PlayerPrefs.SetFloat("BS_CameraSens", val);
            if (cameraSensValText != null) cameraSensValText.text = $"{val:0.0}x";

            // Update camera if present
            if (Camera.main != null)
            {
                var arpg = Camera.main.GetComponent<PortraitARPGCamera>();
                if (arpg != null) arpg.SetSensitivityMultiplier(val);
            }
        }

        public void ToggleInvertY()
        {
            invertY = !invertY;
            PlayerPrefs.SetInt("BS_InvertY", invertY ? 1 : 0);
            if (invertYBtnText != null)
            {
                invertYBtnText.text = invertY ? "INVERT Y: ON" : "INVERT Y: OFF";
            }
            PlaySound("click", 0.7f);
        }

        public void SetCameraFOV(float val)
        {
            PlayerPrefs.SetFloat("BS_CameraFOV", val);
            if (Camera.main != null) Camera.main.fieldOfView = val;
            if (fovValText != null) fovValText.text = $"{Mathf.RoundToInt(val)}°";
        }
        #endregion

        #region Sound Utility
        private void PlaySound(string preset, float vol)
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX(preset, vol);
            }
        }
        #endregion

        #region Procedural UI Hierarchy Builder
        private void BuildQoLUICanvas()
        {
            var canvasGo = new GameObject("QoL_Master_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            qolCanvas = canvasGo.GetComponent<Canvas>();
            qolCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            qolCanvas.sortingOrder = 999;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // 1. Compass & District Header (Top Center)
            BuildCompassHeader(canvasGo.transform);

            // 2. Smart Interaction Prompt Pill (Bottom Center)
            BuildInteractionPromptPill(canvasGo.transform);

            // 3. Auto-Run Badge (Left Center)
            BuildAutoRunBadge(canvasGo.transform);

            // 4. Toast Notification Banner (Top Center below Compass)
            BuildToastBanner(canvasGo.transform);

            // 5. Universal Pause Overlay Modal
            BuildPauseOverlayModal(canvasGo.transform);

            // 6. Photo Mode Watermark (Bottom Right)
            BuildPhotoModeWatermark(canvasGo.transform);
        }

        private void BuildToastBanner(Transform root)
        {
            toastBannerObj = new GameObject("Toast_Banner", typeof(RectTransform), typeof(Image));
            toastBannerObj.transform.SetParent(root, false);
            var rt = toastBannerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1.0f);
            rt.anchorMax = new Vector2(0.5f, 1.0f);
            rt.pivot = new Vector2(0.5f, 1.0f);
            rt.anchoredPosition = new Vector2(0f, -65f);
            rt.sizeDelta = new Vector2(420f, 34f);

            var img = toastBannerObj.GetComponent<Image>();
            img.color = new Color(0.04f, 0.08f, 0.16f, 0.75f);

            var textGo = new GameObject("Toast_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(toastBannerObj.transform, false);
            var tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;

            toastBannerText = textGo.GetComponent<TextMeshProUGUI>();
            toastBannerText.fontSize = 14f;
            toastBannerText.alignment = TextAlignmentOptions.Center;
            toastBannerText.color = new Color(1.0f, 0.95f, 0.6f);
            toastBannerText.text = "Notification";

            toastBannerObj.SetActive(false);
        }

        private void BuildCompassHeader(Transform root)
        {
            compassBannerObj = new GameObject("Compass_Banner", typeof(RectTransform), typeof(Image));
            compassBannerObj.transform.SetParent(root, false);
            var rt = compassBannerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1.0f);
            rt.anchorMax = new Vector2(0.5f, 1.0f);
            rt.pivot = new Vector2(0.5f, 1.0f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(380f, 36f);

            var img = compassBannerObj.GetComponent<Image>();
            img.color = new Color(0.04f, 0.06f, 0.12f, 0.60f);

            // Heading Text
            var headGo = new GameObject("Heading_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            headGo.transform.SetParent(compassBannerObj.transform, false);
            var hRt = headGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 0f);
            hRt.anchorMax = new Vector2(0.30f, 1.0f);
            hRt.offsetMin = hRt.offsetMax = Vector2.zero;
            compassHeadingText = headGo.GetComponent<TextMeshProUGUI>();
            compassHeadingText.fontSize = 16f;
            compassHeadingText.alignment = TextAlignmentOptions.Center;
            compassHeadingText.text = "<color=#FFE570><b>N</b></color> 0°";

            // District Text
            var distGo = new GameObject("District_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            distGo.transform.SetParent(compassBannerObj.transform, false);
            var dRt = distGo.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.30f, 0f);
            dRt.anchorMax = new Vector2(1.0f, 1.0f);
            dRt.offsetMin = dRt.offsetMax = Vector2.zero;
            compassDistrictText = distGo.GetComponent<TextMeshProUGUI>();
            compassDistrictText.fontSize = 15f;
            compassDistrictText.alignment = TextAlignmentOptions.MidlineLeft;
            compassDistrictText.text = "✦ Citadel Acropolis ✦";
        }

        private void BuildInteractionPromptPill(Transform root)
        {
            interactionPromptPillObj = new GameObject("Smart_Interact_Pill", typeof(RectTransform), typeof(Image), typeof(Button));
            interactionPromptPillObj.transform.SetParent(root, false);
            var rt = interactionPromptPillObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 140f);
            rt.sizeDelta = new Vector2(300f, 40f);

            var img = interactionPromptPillObj.GetComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.16f, 0.75f);

            var btn = interactionPromptPillObj.GetComponent<Button>();
            btn.onClick.AddListener(ExecuteNearestInteraction);

            var textGo = new GameObject("Prompt_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(interactionPromptPillObj.transform, false);
            var tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;

            interactionPromptText = textGo.GetComponent<TextMeshProUGUI>();
            interactionPromptText.fontSize = 15f;
            interactionPromptText.alignment = TextAlignmentOptions.Center;
            interactionPromptText.text = "<color=#FFE060><b>[E] TALK</b></color>  <color=#FFFFFF>Linus</color>";

            interactionPromptPillObj.SetActive(false);
        }

        private void BuildAutoRunBadge(Transform root)
        {
            autoRunBadgeObj = new GameObject("AutoRun_Badge", typeof(RectTransform), typeof(Image));
            autoRunBadgeObj.transform.SetParent(root, false);
            var rt = autoRunBadgeObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(25f, 30f);
            rt.sizeDelta = new Vector2(160f, 28f);

            var img = autoRunBadgeObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.20f, 0.40f, 0.70f);

            var textGo = new GameObject("Badge_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(autoRunBadgeObj.transform, false);
            var tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 13f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = "<b>[R] AUTO-RUN ACTIVE</b> (R to Stop)";

            autoRunBadgeObj.SetActive(false);
        }

        private void BuildPauseOverlayModal(Transform root)
        {
            pauseModalObj = new GameObject("Pause_Master_Overlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            pauseModalObj.transform.SetParent(root, false);
            var rt = pauseModalObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var bg = pauseModalObj.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.94f);

            pauseOverlayGroup = pauseModalObj.GetComponent<CanvasGroup>();
            pauseOverlayGroup.alpha = 0f;
            pauseOverlayGroup.blocksRaycasts = false;
            pauseOverlayGroup.interactable = false;

            // Content Panel (Center Box)
            var panelGo = new GameObject("Pause_Card", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(pauseModalObj.transform, false);
            var pRt = panelGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(740f, 940f);

            var pImg = panelGo.GetComponent<Image>();
            pImg.color = new Color(0.10f, 0.14f, 0.22f, 0.98f);

            // Container for main menu elements (hidden when subpanel is open)
            mainMenuContainerObj = new GameObject("Main_Menu_Container", typeof(RectTransform));
            mainMenuContainerObj.transform.SetParent(panelGo.transform, false);
            var mRt = mainMenuContainerObj.GetComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero;
            mRt.anchorMax = Vector2.one;
            mRt.offsetMin = mRt.offsetMax = Vector2.zero;

            // Title inside main menu container
            var titleGo = new GameObject("Title_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(mainMenuContainerObj.transform, false);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -30f);
            tRt.sizeDelta = new Vector2(680f, 50f);

            var tTmp = titleGo.GetComponent<TextMeshProUGUI>();
            tTmp.text = "<b>GAME PAUSED • MASTER MENU</b>";
            tTmp.fontSize = 28f;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = new Color(1.0f, 0.90f, 0.50f);

            // Main Menu Buttons
            float btnY = -120f;
            float btnSpacing = 76f;

            CreateMenuButton(mainMenuContainerObj.transform, "► RESUME GAME  [ESC]", new Vector2(0f, btnY), () => ResumeGame());
            CreateMenuButton(mainMenuContainerObj.transform, "METROPOLIS FAST TRAVEL  [M]", new Vector2(0f, btnY - btnSpacing * 1), () => OpenFastTravelDirectly());
            CreateMenuButton(mainMenuContainerObj.transform, "AUDIO & SYSTEM SETTINGS  [O]", new Vector2(0f, btnY - btnSpacing * 2), () => ToggleSettings());
            CreateMenuButton(mainMenuContainerObj.transform, "CONTROLS & KEYBINDINGS  [H]", new Vector2(0f, btnY - btnSpacing * 3), () => ToggleControlsGuide());
            CreateMenuButton(mainMenuContainerObj.transform, "CINEMATIC PHOTO MODE  [F1]", new Vector2(0f, btnY - btnSpacing * 4), () => { TogglePause(); TogglePhotoMode(); });
            CreateMenuButton(mainMenuContainerObj.transform, "QUIT TO TITLE SCREEN", new Vector2(0f, btnY - btnSpacing * 5), () => ReturnToTitleScreen());

            // Subpanel: Fast Travel Directory
            BuildFastTravelSubpanel(panelGo.transform);

            // Subpanel: Settings
            BuildSettingsSubpanel(panelGo.transform);

            // Subpanel: Controls Guide
            BuildControlsGuideSubpanel(panelGo.transform);

            pauseModalObj.SetActive(false);
        }

        private void CreateMenuButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject($"Btn_{label.Replace(" ", "_")}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(620f, 58f);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.18f, 0.25f, 0.38f, 0.95f);

            var btn = btnGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = new Color(0.18f, 0.25f, 0.38f, 0.95f);
            cb.highlightedColor = new Color(0.28f, 0.40f, 0.60f, 1.0f);
            cb.pressedColor = new Color(0.12f, 0.18f, 0.28f, 1.0f);
            cb.selectedColor = cb.highlightedColor;
            btn.colors = cb;
            btn.onClick.AddListener(() => PlaySound("click", 0.75f));
            btn.onClick.AddListener(action);

            var textGo = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            var tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void BuildFastTravelSubpanel(Transform parent)
        {
            fastTravelPanelObj = new GameObject("Fast_Travel_Subpanel", typeof(RectTransform), typeof(Image));
            fastTravelPanelObj.transform.SetParent(parent, false);
            var rt = fastTravelPanelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = fastTravelPanelObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.11f, 0.18f, 1.0f);

            // Subpanel Title
            var titleGo = new GameObject("FT_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(fastTravelPanelObj.transform, false);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -20f);
            tRt.sizeDelta = new Vector2(680f, 44f);
            var tmp = titleGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "<b>METROPOLIS FAST TRAVEL DIRECTORY</b>";
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.85f, 1.0f);

            // Close button
            var closeGo = new GameObject("FT_CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(fastTravelPanelObj.transform, false);
            var cRt = closeGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0f);
            cRt.anchorMax = new Vector2(0.5f, 0f);
            cRt.pivot = new Vector2(0.5f, 0f);
            cRt.anchoredPosition = new Vector2(0f, 20f);
            cRt.sizeDelta = new Vector2(300f, 48f);
            closeGo.GetComponent<Image>().color = new Color(0.3f, 0.2f, 0.2f, 0.95f);
            var cBtn = closeGo.GetComponent<Button>();
            cBtn.onClick.AddListener(() => PlaySound("click", 0.75f));
            cBtn.onClick.AddListener(() =>
            {
                fastTravelPanelObj.SetActive(false);
                if (mainMenuContainerObj != null) mainMenuContainerObj.SetActive(true);
            });

            var cTmpGo = new GameObject("CloseText", typeof(RectTransform), typeof(TextMeshProUGUI));
            cTmpGo.transform.SetParent(closeGo.transform, false);
            var ctRt = cTmpGo.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;
            var cTmp = cTmpGo.GetComponent<TextMeshProUGUI>();
            cTmp.text = "Back to Master Menu";
            cTmp.fontSize = 18f;
            cTmp.alignment = TextAlignmentOptions.Center;

            // Generate Waypoint Buttons
            float yPos = -72f;
            for (int i = 0; i < waypoints.Count; i++)
            {
                var wp = waypoints[i];
                var wpBtnGo = new GameObject($"WP_{wp.name.Replace(" ", "_")}", typeof(RectTransform), typeof(Image), typeof(Button));
                wpBtnGo.transform.SetParent(fastTravelPanelObj.transform, false);
                var wRt = wpBtnGo.GetComponent<RectTransform>();
                wRt.anchorMin = new Vector2(0.5f, 1f);
                wRt.anchorMax = new Vector2(0.5f, 1f);
                wRt.pivot = new Vector2(0.5f, 1f);
                wRt.anchoredPosition = new Vector2(0f, yPos);
                wRt.sizeDelta = new Vector2(640f, 44f);

                wpBtnGo.GetComponent<Image>().color = wp.name.Contains("Boss") ? new Color(0.50f, 0.15f, 0.22f, 0.95f) : new Color(0.15f, 0.22f, 0.35f, 0.92f);
                var wpBtn = wpBtnGo.GetComponent<Button>();
                wpBtn.transition = Selectable.Transition.ColorTint;
                var wpCb = wpBtn.colors;
                wpCb.normalColor = wp.name.Contains("Boss") ? new Color(0.50f, 0.15f, 0.22f, 0.95f) : new Color(0.15f, 0.22f, 0.35f, 0.92f);
                wpCb.highlightedColor = wp.name.Contains("Boss") ? new Color(0.65f, 0.22f, 0.30f, 1.0f) : new Color(0.26f, 0.38f, 0.58f, 1.0f);
                wpCb.pressedColor = new Color(0.10f, 0.15f, 0.25f, 1.0f);
                wpCb.selectedColor = wpCb.highlightedColor;
                wpBtn.colors = wpCb;
                wpBtn.onClick.AddListener(() => PlaySound("click", 0.75f));
                wpBtn.onClick.AddListener(() => FastTravelTo(wp));

                var labelGo = new GameObject("WP_Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(wpBtnGo.transform, false);
                var lRt = labelGo.GetComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.offsetMin = lRt.offsetMax = Vector2.zero;

                var lTmp = labelGo.GetComponent<TextMeshProUGUI>();
                lTmp.text = $"  <color=#FFE060><b>{wp.icon}</b></color>  <b>{wp.name}</b>  <size=75%><color=#A0D0FF>({wp.district})</color></size>";
                lTmp.fontSize = 17f;
                lTmp.alignment = TextAlignmentOptions.MidlineLeft;

                yPos -= 50f;
            }

            fastTravelPanelObj.SetActive(false);
        }

        private void BuildSettingsSubpanel(Transform parent)
        {
            settingsPanelObj = new GameObject("Settings_Subpanel", typeof(RectTransform), typeof(Image));
            settingsPanelObj.transform.SetParent(parent, false);
            var rt = settingsPanelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = settingsPanelObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.11f, 0.18f, 1.0f);

            // Title
            var titleGo = new GameObject("Set_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(settingsPanelObj.transform, false);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -25f);
            tRt.sizeDelta = new Vector2(680f, 44f);
            var tmp = titleGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "<b>AUDIO & SYSTEM SETTINGS</b>";
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 1.0f, 0.8f);

            float rowY = -90f;
            float rowSpacing = 68f;

            // 1. Master Volume
            CreateSettingSliderRow(settingsPanelObj.transform, "Master Volume", AudioListener.volume, 0f, 1f, new Vector2(0f, rowY), (val) => SetMasterVolume(val), out masterVolSlider, out masterVolValText);
            rowY -= rowSpacing;

            // 2. Music Volume
            float musicVol = PlayerPrefs.GetFloat("BS_MusicVolume", 0.7f);
            CreateSettingSliderRow(settingsPanelObj.transform, "Music Volume", musicVol, 0f, 1f, new Vector2(0f, rowY), (val) => SetMusicVolume(val), out musicVolSlider, out musicVolValText);
            rowY -= rowSpacing;

            // 3. SFX Volume
            float sfxVol = PlayerPrefs.GetFloat("BS_SfxVolume", 0.8f);
            CreateSettingSliderRow(settingsPanelObj.transform, "Combat & SFX", sfxVol, 0f, 1f, new Vector2(0f, rowY), (val) => SetSFXVolume(val), out sfxVolSlider, out sfxVolValText);
            rowY -= rowSpacing;

            // 4. Target Framerate Toggle Button
            CreateSettingButtonRow(settingsPanelObj.transform, "Target Framerate", currentFpsTarget == -1 ? "FPS: Uncapped" : $"FPS: {currentFpsTarget} FPS", new Vector2(0f, rowY), () => CycleTargetFps(), out fpsCapBtnText);
            rowY -= rowSpacing;

            // 5. Camera Look Sensitivity
            CreateSettingSliderRow(settingsPanelObj.transform, "Camera Sensitivity", cameraSens, 0.4f, 3.0f, new Vector2(0f, rowY), (val) => SetCameraSensitivity(val), out cameraSensSlider, out cameraSensValText);
            rowY -= rowSpacing;

            // 6. Camera Invert Y Toggle Button
            CreateSettingButtonRow(settingsPanelObj.transform, "Invert Y-Axis", invertY ? "INVERT Y: ON" : "INVERT Y: OFF", new Vector2(0f, rowY), () => ToggleInvertY(), out invertYBtnText);
            rowY -= rowSpacing;

            // 7. Field of View (FOV)
            float curFov = Camera.main != null ? Camera.main.fieldOfView : 60f;
            CreateSettingSliderRow(settingsPanelObj.transform, "Camera FOV", curFov, 45f, 90f, new Vector2(0f, rowY), (val) => SetCameraFOV(val), out fovSlider, out fovValText);
            rowY -= rowSpacing;

            // Close button
            var closeGo = new GameObject("Set_CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(settingsPanelObj.transform, false);
            var cRt = closeGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0f);
            cRt.anchorMax = new Vector2(0.5f, 0f);
            cRt.pivot = new Vector2(0.5f, 0f);
            cRt.anchoredPosition = new Vector2(0f, 25f);
            cRt.sizeDelta = new Vector2(300f, 48f);
            closeGo.GetComponent<Image>().color = new Color(0.3f, 0.2f, 0.2f, 0.95f);
            var setCBtn = closeGo.GetComponent<Button>();
            setCBtn.onClick.AddListener(() => PlaySound("click", 0.75f));
            setCBtn.onClick.AddListener(() =>
            {
                settingsPanelObj.SetActive(false);
                if (mainMenuContainerObj != null) mainMenuContainerObj.SetActive(true);
            });

            var cTmpGo = new GameObject("CloseText", typeof(RectTransform), typeof(TextMeshProUGUI));
            cTmpGo.transform.SetParent(closeGo.transform, false);
            var ctRt = cTmpGo.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;
            var cTmp = cTmpGo.GetComponent<TextMeshProUGUI>();
            cTmp.text = "Back to Master Menu";
            cTmp.fontSize = 18f;
            cTmp.alignment = TextAlignmentOptions.Center;

            settingsPanelObj.SetActive(false);
        }

        private void CreateSettingSliderRow(Transform parent, string label, float initialVal, float minVal, float maxVal, Vector2 pos, UnityEngine.Events.UnityAction<float> onValueChanged, out Slider slider, out TextMeshProUGUI valText)
        {
            var rowGo = new GameObject($"Row_{label.Replace(" ", "_")}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rRt = rowGo.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 1f);
            rRt.anchorMax = new Vector2(0.5f, 1f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = pos;
            rRt.sizeDelta = new Vector2(640f, 48f);

            // Label
            var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lGo.transform.SetParent(rowGo.transform, false);
            var lRt = lGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0.40f, 1f);
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            var lTmp = lGo.GetComponent<TextMeshProUGUI>();
            lTmp.text = label;
            lTmp.fontSize = 18f;
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lTmp.color = Color.white;

            // Slider Background
            var sGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sGo.transform.SetParent(rowGo.transform, false);
            var sRt = sGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.42f, 0.2f);
            sRt.anchorMax = new Vector2(0.82f, 0.8f);
            sRt.offsetMin = sRt.offsetMax = Vector2.zero;

            slider = sGo.GetComponent<Slider>();
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.value = initialVal;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(sGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f, 0.9f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sGo.transform, false);
            var fRt = fillArea.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = fRt.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            fillGo.GetComponent<Image>().color = new Color(0.25f, 0.65f, 1.0f, 0.95f);

            slider.fillRect = fillRt;

            // Value text
            var vGo = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            vGo.transform.SetParent(rowGo.transform, false);
            var vRt = vGo.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0.84f, 0f);
            vRt.anchorMax = new Vector2(1.0f, 1f);
            vRt.offsetMin = vRt.offsetMax = Vector2.zero;
            valText = vGo.GetComponent<TextMeshProUGUI>();
            valText.fontSize = 17f;
            valText.alignment = TextAlignmentOptions.MidlineRight;
            valText.color = new Color(1.0f, 0.85f, 0.4f);
            valText.text = maxVal <= 1.0f ? $"{Mathf.RoundToInt(initialVal * 100)}%" : $"{initialVal:0.0}";

            slider.onValueChanged.AddListener(onValueChanged);
        }

        private void CreateSettingButtonRow(Transform parent, string label, string btnInitialText, Vector2 pos, UnityEngine.Events.UnityAction onAction, out TextMeshProUGUI btnText)
        {
            var rowGo = new GameObject($"Row_{label.Replace(" ", "_")}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rRt = rowGo.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 1f);
            rRt.anchorMax = new Vector2(0.5f, 1f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = pos;
            rRt.sizeDelta = new Vector2(640f, 48f);

            // Label
            var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lGo.transform.SetParent(rowGo.transform, false);
            var lRt = lGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(0.40f, 1f);
            lRt.offsetMin = lRt.offsetMax = Vector2.zero;
            var lTmp = lGo.GetComponent<TextMeshProUGUI>();
            lTmp.text = label;
            lTmp.fontSize = 18f;
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lTmp.color = Color.white;

            // Action Button
            var bGo = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            bGo.transform.SetParent(rowGo.transform, false);
            var bRt = bGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.42f, 0.05f);
            bRt.anchorMax = new Vector2(1.0f, 0.95f);
            bRt.offsetMin = bRt.offsetMax = Vector2.zero;

            bGo.GetComponent<Image>().color = new Color(0.20f, 0.28f, 0.42f, 0.95f);
            var rowBtn = bGo.GetComponent<Button>();
            rowBtn.transition = Selectable.Transition.ColorTint;
            var rCb = rowBtn.colors;
            rCb.normalColor = new Color(0.20f, 0.28f, 0.42f, 0.95f);
            rCb.highlightedColor = new Color(0.28f, 0.42f, 0.62f, 1.0f);
            rCb.pressedColor = new Color(0.14f, 0.20f, 0.30f, 1.0f);
            rCb.selectedColor = rCb.highlightedColor;
            rowBtn.colors = rCb;
            rowBtn.onClick.AddListener(() => PlaySound("click", 0.75f));
            rowBtn.onClick.AddListener(onAction);

            var btGo = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
            btGo.transform.SetParent(bGo.transform, false);
            var btRt = btGo.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = btRt.offsetMax = Vector2.zero;

            btnText = btGo.GetComponent<TextMeshProUGUI>();
            btnText.text = btnInitialText;
            btnText.fontSize = 18f;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = new Color(0.4f, 0.9f, 1.0f);
        }

        private void BuildControlsGuideSubpanel(Transform parent)
        {
            controlsGuidePanelObj = new GameObject("Controls_Guide_Subpanel", typeof(RectTransform), typeof(Image));
            controlsGuidePanelObj.transform.SetParent(parent, false);
            var rt = controlsGuidePanelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = controlsGuidePanelObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.11f, 0.18f, 1.0f);

            var titleGo = new GameObject("Guide_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(controlsGuidePanelObj.transform, false);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -25f);
            tRt.sizeDelta = new Vector2(680f, 44f);
            var tmp = titleGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "<b>CONTROLS & KEYBINDINGS GUIDE</b>";
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1.0f, 0.85f, 0.4f);

            var contentGo = new GameObject("Guide_Content", typeof(RectTransform), typeof(TextMeshProUGUI));
            contentGo.transform.SetParent(controlsGuidePanelObj.transform, false);
            var cRt = contentGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.05f, 0.12f);
            cRt.anchorMax = new Vector2(0.95f, 0.90f);
            cRt.offsetMin = cRt.offsetMax = Vector2.zero;

            var cTmp = contentGo.GetComponent<TextMeshProUGUI>();
            cTmp.fontSize = 19f;
            cTmp.lineSpacing = 14f;
            cTmp.text =
                "<b>[ MOVEMENT & EXPLORATION ]</b>\n" +
                "<color=#FFE060>[W, A, S, D]</color> Move Character in 3D Space\n" +
                "<color=#FFE060>[Right Mouse Drag / Arrows]</color> Orbit Camera Look\n" +
                "<color=#FFE060>[Mouse Scroll Wheel]</color> Camera Zoom In / Out\n" +
                "<color=#FFE060>[Left Shift]</color> Hold to Sprint\n" +
                "<color=#FFE060>[Space / J]</color> Jump & Aerial Dash\n" +
                "<color=#FFE060>[R / NumLock]</color> Toggle Hands-Free Auto-Run\n\n" +
                "<b>[ INTERACTION & COMBAT ]</b>\n" +
                "<color=#FFE060>[E]</color> Smart Interact (Talk to NPCs, Enter Doors, Rifts, Task Board)\n" +
                "<color=#FFE060>[Left Mouse / J]</color> Attack / Cast Active Spell\n" +
                "<color=#FFE060>[1, 2, 3]</color> Combat Skills & Relic Summoning\n" +
                "<color=#FFE060>[Q / F]</color> HP / MP Potions\n\n" +
                "<b>[ QUALITY OF LIFE & MENUS ]</b>\n" +
                "<color=#FFE060>[M]</color> Fast Travel & Metropolis Landmark Directory\n" +
                "<color=#FFE060>[ESC / P]</color> Pause Game & Master Settings\n" +
                "<color=#FFE060>[O]</color> Quick Audio & Visual Settings\n" +
                "<color=#FFE060>[H]</color> Quick Controls & Keybindings Reference\n" +
                "<color=#FFE060>[F1]</color> Toggle Cinematic Photo Mode (Hide UI)\n" +
                "<color=#FFE060>[F5] / [F9]</color> Quick Save / Quick Load";

            // Close button
            var closeGo = new GameObject("Guide_CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(controlsGuidePanelObj.transform, false);
            var clRt = closeGo.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(0.5f, 0f);
            clRt.anchorMax = new Vector2(0.5f, 0f);
            clRt.pivot = new Vector2(0.5f, 0f);
            clRt.anchoredPosition = new Vector2(0f, 25f);
            clRt.sizeDelta = new Vector2(300f, 48f);
            closeGo.GetComponent<Image>().color = new Color(0.3f, 0.2f, 0.2f, 0.95f);
            var ctrlCBtn = closeGo.GetComponent<Button>();
            ctrlCBtn.onClick.AddListener(() => PlaySound("click", 0.75f));
            ctrlCBtn.onClick.AddListener(() =>
            {
                controlsGuidePanelObj.SetActive(false);
                if (mainMenuContainerObj != null) mainMenuContainerObj.SetActive(true);
            });

            var cTmpGo = new GameObject("CloseText", typeof(RectTransform), typeof(TextMeshProUGUI));
            cTmpGo.transform.SetParent(closeGo.transform, false);
            var ctRt = cTmpGo.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;
            var clTmp = cTmpGo.GetComponent<TextMeshProUGUI>();
            clTmp.text = "Back to Master Menu";
            clTmp.fontSize = 18f;
            clTmp.alignment = TextAlignmentOptions.Center;

            controlsGuidePanelObj.SetActive(false);
        }

        private void BuildPhotoModeWatermark(Transform root)
        {
            photoModeWatermarkObj = new GameObject("PhotoMode_Watermark", typeof(RectTransform), typeof(TextMeshProUGUI));
            photoModeWatermarkObj.transform.SetParent(root, false);
            var rt = photoModeWatermarkObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-40f, 30f);
            rt.sizeDelta = new Vector2(400f, 40f);

            var tmp = photoModeWatermarkObj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.BottomRight;
            tmp.color = new Color(1f, 1f, 1f, 0.65f);
            tmp.text = "<b>BATTLE SUMMONER</b> • [F1] Exit Photo Mode";

            photoModeWatermarkObj.SetActive(false);
        }
        #endregion
    }
}

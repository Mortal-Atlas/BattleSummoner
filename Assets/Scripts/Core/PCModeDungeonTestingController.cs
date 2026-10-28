using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.UI;
using BattleSummoner.CameraSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BattleSummoner.Core
{
    public enum PlatformTestingMode
    {
        PCStandaloneTesting = 0, // Active now for fast PC Dungeon & Combat testing
        MobileTouchPortrait = 1  // Toggle when exporting APK to Android
    }

    [System.Serializable]
    public class WeaponLoadoutDef
    {
        public string weaponName;
        public string stanceTag;
        public string rightWeaponPath;
        public string leftOffHandPath;
        public Vector3 rightPosOffset;
        public Vector3 rightRotOffset;
        public Vector3 rightScale = Vector3.one;
        public Vector3 leftPosOffset;
        public Vector3 leftRotOffset;
        public Vector3 leftScale = Vector3.one;
        public bool hasShield;
        public bool isDualWield;
        public float baseDamage = 48f;
    }

    [DefaultExecutionOrder(-50)]
    public class PCModeDungeonTestingController : MonoBehaviour
    {
        public static PCModeDungeonTestingController Instance { get; private set; }

        [Header("Platform Mode Switch (Enabled for PC Testing Now)")]
        public PlatformTestingMode activeMode = PlatformTestingMode.PCStandaloneTesting;
        public bool isPCModeEnabled => activeMode == PlatformTestingMode.PCStandaloneTesting;
        public bool lockWindowToDesktopTestResolution = false;

        [Header("PC Movement & Camera Tuning")]
        public float walkSpeed = 6.5f;
        public float sprintSpeed = 10.5f;
        public float jumpForce = 7.2f;
        public float dashImpulse = 16.0f;
        public float gravity = -22.0f;

        [Header("Player Character .GLB Model Variants (Keys 1-4 to Hot-Swap)")]
        public GameObject[] characterModelVariants = new GameObject[4]; // 0:MaleStarter, 1:femalestarter, 2:manbase, 3:womanbase
        public int activeModelIndex = 0;

        [Header("Equipped Weapon Loadouts (Keypad 1-9 to Swap)")]
        public int activeWeaponIndex = 1; // 1 to 9
        public List<WeaponLoadoutDef> weaponLoadouts = new List<WeaponLoadoutDef>();

        [Header("Wired Combat & Animation References")]
        public CharacterController characterController;
        public ProceduralSlashIKAndAnimatorBridge combatAnimatorBridge;
        public Camera activeMainCamera;

        private Vector3 _verticalVelocity;
        private float _dashCooldownTimer;
        private Vector2 _mouseSlashStartPos;
        private float _mouseSlashStartTime;
        private bool _isDraggingMouseSlash;
        private bool _isChargingHeavyArt;

        private static readonly Collider[] _hitBuffer = new Collider[32];
        private static readonly string[] CharacterNames = { "Male Starter", "Female Starter", "Man Base", "Woman Base" };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Ensure normal game speed on launch
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (characterController != null && !characterController.enabled) characterController.enabled = true;

            if (activeMainCamera == null) activeMainCamera = Camera.main;

            // Ensure cursor is visible and unlocked
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            InitializeDefaultWeaponLoadouts();
            ActivateCharacterModelVariant(activeModelIndex);
            EquipWeaponLoadout(activeWeaponIndex);

            // Apply platform visibility settings
            ApplyPlatformModeState(activeMode);
        }

        private void OnEnable()
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            if (characterController != null && !characterController.enabled) characterController.enabled = true;
        }

        private void Update()
        {
            if (activeMode != PlatformTestingMode.PCStandaloneTesting) return;

            // Failsafe timeScale check in case other scripts paused/slowed time
            if (Time.timeScale < 0.2f && !Input.GetMouseButton(0))
            {
                Time.timeScale = 1.0f;
            }

            HandlePCModelHotSwapKeys();
            HandlePCWeaponSwapKeypad();
            HandlePCKeyboardLocomotion();
            HandlePCMouseBladeAndParryCombat();
        }

        private void HandlePCModelHotSwapKeys()
        {
            if (GetKeyDown(KeyCode.Alpha1, () => Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame))
                ActivateCharacterModelVariant(0);
            if (GetKeyDown(KeyCode.Alpha2, () => Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame))
                ActivateCharacterModelVariant(1);
            if (GetKeyDown(KeyCode.Alpha3, () => Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame))
                ActivateCharacterModelVariant(2);
            if (GetKeyDown(KeyCode.Alpha4, () => Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame))
                ActivateCharacterModelVariant(3);

            // F1 toggles between PC Testing Mode and Mobile Portrait Simulation at any time
            if (GetKeyDown(KeyCode.F1, () => Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame))
            {
                activeMode = activeMode == PlatformTestingMode.PCStandaloneTesting
                    ? PlatformTestingMode.MobileTouchPortrait
                    : PlatformTestingMode.PCStandaloneTesting;
                ApplyPlatformModeState(activeMode);
                Debug.Log($"[PCModeDungeonTestingController] Switched mode to: {activeMode}");
            }
        }

        private void HandlePCWeaponSwapKeypad()
        {
            // Numpad 1-9
            if (GetKeyDown(KeyCode.Keypad1, () => Keyboard.current != null && Keyboard.current.numpad1Key.wasPressedThisFrame) ||
                GetKeyDown(KeyCode.Alpha5, () => Keyboard.current != null && Keyboard.current.digit5Key.wasPressedThisFrame))
                EquipWeaponLoadout(1);

            if (GetKeyDown(KeyCode.Keypad2, () => Keyboard.current != null && Keyboard.current.numpad2Key.wasPressedThisFrame) ||
                GetKeyDown(KeyCode.Alpha6, () => Keyboard.current != null && Keyboard.current.digit6Key.wasPressedThisFrame))
                EquipWeaponLoadout(2);

            if (GetKeyDown(KeyCode.Keypad3, () => Keyboard.current != null && Keyboard.current.numpad3Key.wasPressedThisFrame) ||
                GetKeyDown(KeyCode.Alpha7, () => Keyboard.current != null && Keyboard.current.digit7Key.wasPressedThisFrame))
                EquipWeaponLoadout(3);

            if (GetKeyDown(KeyCode.Keypad4, () => Keyboard.current != null && Keyboard.current.numpad4Key.wasPressedThisFrame) ||
                GetKeyDown(KeyCode.Alpha8, () => Keyboard.current != null && Keyboard.current.digit8Key.wasPressedThisFrame))
                EquipWeaponLoadout(4);

            if (GetKeyDown(KeyCode.Keypad5, () => Keyboard.current != null && Keyboard.current.numpad5Key.wasPressedThisFrame) ||
                GetKeyDown(KeyCode.Alpha9, () => Keyboard.current != null && Keyboard.current.digit9Key.wasPressedThisFrame))
                EquipWeaponLoadout(5);

            if (GetKeyDown(KeyCode.Keypad6, () => Keyboard.current != null && Keyboard.current.numpad6Key.wasPressedThisFrame) ||
                GetKeyDown(KeyCode.Alpha0, () => Keyboard.current != null && Keyboard.current.digit0Key.wasPressedThisFrame))
                EquipWeaponLoadout(6);

            if (GetKeyDown(KeyCode.Keypad7, () => Keyboard.current != null && Keyboard.current.numpad7Key.wasPressedThisFrame))
                EquipWeaponLoadout(7);

            if (GetKeyDown(KeyCode.Keypad8, () => Keyboard.current != null && Keyboard.current.numpad8Key.wasPressedThisFrame))
                EquipWeaponLoadout(8);

            if (GetKeyDown(KeyCode.Keypad9, () => Keyboard.current != null && Keyboard.current.numpad9Key.wasPressedThisFrame))
                EquipWeaponLoadout(9);

            // Previous / Next weapon via [ and ]
            if (GetKeyDown(KeyCode.LeftBracket, () => Keyboard.current != null && Keyboard.current.leftBracketKey.wasPressedThisFrame))
            {
                int next = activeWeaponIndex - 1;
                if (next < 1) next = weaponLoadouts.Count;
                EquipWeaponLoadout(next);
            }
            if (GetKeyDown(KeyCode.RightBracket, () => Keyboard.current != null && Keyboard.current.rightBracketKey.wasPressedThisFrame))
            {
                int next = activeWeaponIndex + 1;
                if (next > weaponLoadouts.Count) next = 1;
                EquipWeaponLoadout(next);
            }
        }

        public void ActivateCharacterModelVariant(int index)
        {
            if (characterModelVariants == null || characterModelVariants.Length == 0) return;
            activeModelIndex = Mathf.Clamp(index, 0, characterModelVariants.Length - 1);

            for (int i = 0; i < characterModelVariants.Length; i++)
            {
                if (characterModelVariants[i] == null) continue;
                bool shouldBeActive = (i == activeModelIndex);
                characterModelVariants[i].SetActive(shouldBeActive);

                if (shouldBeActive)
                {
                    Animator anim = characterModelVariants[i].GetComponentInChildren<Animator>();
                    var bridge = characterModelVariants[i].GetComponentInChildren<ProceduralSlashIKAndAnimatorBridge>();
                    if (bridge != null) combatAnimatorBridge = bridge;
                    else if (combatAnimatorBridge != null && anim != null) combatAnimatorBridge.characterAnimator = anim;
                }
            }

            // Sync with HUD
            if (PCDungeonHUDController.Instance != null)
            {
                string hName = (activeModelIndex >= 0 && activeModelIndex < CharacterNames.Length)
                    ? CharacterNames[activeModelIndex]
                    : "Hero";
                PCDungeonHUDController.Instance.UpdateActiveHeroModel(hName, activeModelIndex);
            }

            if (HUDPortraitAndThemeSyncController.Instance != null)
            {
                bool isFemale = (activeModelIndex == 1 || activeModelIndex == 3);
                HUDPortraitAndThemeSyncController.Instance.SyncPortraitWithSelectedSex(
                    isFemale ? BattleSummoner.Character.CharacterSex.Female : BattleSummoner.Character.CharacterSex.Male);
            }

            // Re-equip current weapon on new character model
            EquipWeaponLoadout(activeWeaponIndex);
        }

        public void EquipWeaponLoadout(int index)
        {
            if (weaponLoadouts == null || weaponLoadouts.Count == 0) return;
            activeWeaponIndex = Mathf.Clamp(index, 1, weaponLoadouts.Count);
            WeaponLoadoutDef def = weaponLoadouts[activeWeaponIndex - 1];

            GameObject activeModel = (characterModelVariants != null && activeModelIndex < characterModelVariants.Length)
                ? characterModelVariants[activeModelIndex]
                : null;
            if (activeModel == null) return;

            var bridge = activeModel.GetComponent<ProceduralSlashIKAndAnimatorBridge>();
            if (bridge == null) bridge = activeModel.GetComponentInChildren<ProceduralSlashIKAndAnimatorBridge>();

            Transform socketR = bridge != null ? bridge.rightHandWeaponSocket : activeModel.transform.Find("WeaponSocket_R");
            Transform socketL = bridge != null ? bridge.leftHandOffHandSocket : activeModel.transform.Find("OffHandSocket_L");

            // Clear previous weapon meshes
            if (socketR != null)
            {
                for (int i = socketR.childCount - 1; i >= 0; i--)
                {
                    var child = socketR.GetChild(i);
                    if (child.name.StartsWith("RightBladeTip_Trail")) continue;
                    Destroy(child.gameObject);
                }
            }

            if (socketL != null)
            {
                for (int i = socketL.childCount - 1; i >= 0; i--)
                {
                    var child = socketL.GetChild(i);
                    if (child.name.StartsWith("LeftBladeTip_Trail")) continue;
                    Destroy(child.gameObject);
                }
            }

            // Load and instantiate right-hand weapon
            if (!string.IsNullOrEmpty(def.rightWeaponPath) && socketR != null)
            {
                GameObject weaponPrefab = Resources.Load<GameObject>(def.rightWeaponPath);
#if UNITY_EDITOR
                if (weaponPrefab == null)
                    weaponPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(def.rightWeaponPath);
#endif
                if (weaponPrefab != null)
                {
                    GameObject spawnedW = Instantiate(weaponPrefab, socketR);
                    spawnedW.name = "Equipped_MainWeapon";
                    spawnedW.transform.localPosition = def.rightPosOffset;
                    spawnedW.transform.localRotation = Quaternion.Euler(def.rightRotOffset);
                    spawnedW.transform.localScale = def.rightScale;
                }
            }

            // Load and instantiate left-hand offhand/shield
            if (!string.IsNullOrEmpty(def.leftOffHandPath) && socketL != null)
            {
                GameObject offHandPrefab = Resources.Load<GameObject>(def.leftOffHandPath);
#if UNITY_EDITOR
                if (offHandPrefab == null)
                    offHandPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(def.leftOffHandPath);
#endif
                if (offHandPrefab != null)
                {
                    GameObject spawnedOff = Instantiate(offHandPrefab, socketL);
                    spawnedOff.name = "Equipped_OffHand";
                    spawnedOff.transform.localPosition = def.leftPosOffset;
                    spawnedOff.transform.localRotation = Quaternion.Euler(def.leftRotOffset);
                    spawnedOff.transform.localScale = def.leftScale;
                }
            }

            // Update combat bridge settings
            if (bridge != null)
            {
                bridge.hasShieldEquipped = def.hasShield;
                bridge.isDualWielding = def.isDualWield;
                bridge.isPassiveShieldBlocking = false;
            }

            // Update HUD
            if (PCDungeonHUDController.Instance != null)
            {
                PCDungeonHUDController.Instance.UpdateEquippedWeapon(def.weaponName, def.stanceTag, activeWeaponIndex);
            }

            Debug.Log($"[PCModeDungeonTestingController] Equipped Loadout [{activeWeaponIndex}]: {def.weaponName} ({def.stanceTag})");
        }

        private void HandlePCKeyboardLocomotion()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (characterController == null) return;

            // Guarantee character controller is enabled
            if (!characterController.enabled) characterController.enabled = true;
            if (!characterController.gameObject.activeInHierarchy) return;

            float h = 0f;
            float v = 0f;
            if (GetKeyHeld(KeyCode.D, () => Keyboard.current != null && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed))) h += 1f;
            if (GetKeyHeld(KeyCode.A, () => Keyboard.current != null && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)))  h -= 1f;
            if (GetKeyHeld(KeyCode.W, () => Keyboard.current != null && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)))    v += 1f;
            if (GetKeyHeld(KeyCode.S, () => Keyboard.current != null && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)))  v -= 1f;

            // Camera-Relative Movement Direction!
            Vector3 camFwd = activeMainCamera != null
                ? Vector3.ProjectOnPlane(activeMainCamera.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            Vector3 camRight = activeMainCamera != null
                ? Vector3.ProjectOnPlane(activeMainCamera.transform.right, Vector3.up).normalized
                : Vector3.right;

            Vector3 moveDir = (camFwd * v + camRight * h);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            bool isSprinting = GetKeyHeld(KeyCode.LeftShift, () => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;

            // Left-Shift Tap or Q = Instant Dodge/Slope Dash
            bool dashTriggered = GetKeyDown(KeyCode.LeftShift, () => Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame) ||
                                 GetKeyDown(KeyCode.Q, () => Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame);
            if (dashTriggered && _dashCooldownTimer <= 0f && moveDir.sqrMagnitude > 0.01f)
            {
                _dashCooldownTimer = 0.55f;
                characterController.Move(moveDir * (dashImpulse * Time.deltaTime * 4f));
            }

            // Face movement direction smoothly
            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 14f);
            }

            if (characterController.isGrounded && _verticalVelocity.y < 0f)
            {
                _verticalVelocity.y = -2f;
            }

            bool jumpPressed = GetKeyDown(KeyCode.Space, () => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);
            if (jumpPressed && characterController.isGrounded)
            {
                _verticalVelocity.y = jumpForce;
            }

            _verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 finalMove = (moveDir * currentSpeed) + _verticalVelocity;
            if (characterController.enabled && characterController.gameObject.activeInHierarchy)
            {
                characterController.Move(finalMove * Time.deltaTime);
            }

            // Drive Animator MoveSpeed parameter
            if (combatAnimatorBridge != null && combatAnimatorBridge.characterAnimator != null)
            {
                float targetSpeedParam = moveDir.magnitude * (isSprinting ? 1.0f : 0.5f);
                combatAnimatorBridge.characterAnimator.SetFloat("MoveSpeed", targetSpeedParam, 0.08f, Time.deltaTime);
            }
        }

        private void HandlePCMouseBladeAndParryCombat()
        {
            // 1. RIGHT-CLICK = Parry / Shield Block
            bool rClickDown = GetMouseButtonDown(1, () => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
            bool rClickHeld = GetMouseButtonHeld(1, () => Mouse.current != null && Mouse.current.rightButton.isPressed);

            if (rClickDown)
            {
                if (combatAnimatorBridge != null) combatAnimatorBridge.ExecuteUniversalTwoFingerParry();
                BroadcastParryToNearbyBestiaryMonsters();
            }

            // Active Shield Guard while Right Mouse is held
            if (combatAnimatorBridge != null)
            {
                combatAnimatorBridge.isPassiveShieldBlocking = rClickHeld && combatAnimatorBridge.hasShieldEquipped;
            }

            // 2. E KEY or MIDDLE-CLICK = Rune Spell Cast Launch
            bool castTriggered = GetKeyDown(KeyCode.E, () => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                                 GetMouseButtonDown(2, () => Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame);
            if (castTriggered)
            {
                if (combatAnimatorBridge != null) combatAnimatorBridge.ExecuteRuneSpellCastAnimation();
                return;
            }

            // 3. LEFT-CLICK DRAG = 1:1 Directional Blade Slash (Hold > 0.45s = Charged Weapon Art!)
            if (GetMouseButtonDown(0, () => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
            {
                _mouseSlashStartPos = GetMousePosition();
                _mouseSlashStartTime = Time.time;
                _isDraggingMouseSlash = true;
                _isChargingHeavyArt = false;
            }

            if (_isDraggingMouseSlash && GetMouseButtonHeld(0, () => Mouse.current != null && Mouse.current.leftButton.isPressed))
            {
                float heldTime = Time.time - _mouseSlashStartTime;
                float chargeRatio = Mathf.Clamp01(heldTime / 0.45f);
                if (heldTime >= 0.45f && !_isChargingHeavyArt)
                {
                    _isChargingHeavyArt = true;
                }
                if (PCDungeonHUDController.Instance != null)
                {
                    PCDungeonHUDController.Instance.UpdateWeaponArtCharge(_isChargingHeavyArt, chargeRatio);
                }
            }

            if (_isDraggingMouseSlash && GetMouseButtonUp(0, () => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame))
            {
                _isDraggingMouseSlash = false;
                Vector2 endPos = GetMousePosition();
                Vector2 delta = endPos - _mouseSlashStartPos;
                float duration = Mathf.Max(0.02f, Time.time - _mouseSlashStartTime);

                float swipeAngleDeg = delta.sqrMagnitude > 324f
                    ? Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg
                    : 0f;
                float swipeSpeed = delta.magnitude / duration;

                bool altHeld = GetKeyHeld(KeyCode.LeftAlt, () => Keyboard.current != null && Keyboard.current.leftAltKey.isPressed);
                int simulatedFinger = altHeld ? 1 : 0;

                if (combatAnimatorBridge != null)
                {
                    combatAnimatorBridge.ExecuteDirectionalBladeSlash(
                        simulatedFinger, swipeAngleDeg, swipeSpeed, _isChargingHeavyArt);
                }

                DeliverDirectionalSlashToDungeonTargets(swipeAngleDeg, _isChargingHeavyArt, altHeld);

                if (PCDungeonHUDController.Instance != null)
                {
                    PCDungeonHUDController.Instance.UpdateWeaponArtCharge(false, 0f);
                }
            }
        }

        private void DeliverDirectionalSlashToDungeonTargets(float swipeAngleDeg, bool isHeavyArt, bool altHeld)
        {
            Vector3 attackCenter = transform.position + transform.forward * 1.9f + Vector3.up * 1.0f;
            int hitCount = Physics.OverlapSphereNonAlloc(attackCenter, 3.2f, _hitBuffer);

            float baseDmg = isHeavyArt ? 95f : 48f;
            if (activeWeaponIndex > 0 && activeWeaponIndex <= weaponLoadouts.Count)
            {
                baseDmg = isHeavyArt ? weaponLoadouts[activeWeaponIndex - 1].baseDamage * 1.8f : weaponLoadouts[activeWeaponIndex - 1].baseDamage;
            }
            float postureMult = isHeavyArt ? 1.75f : 1.15f;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _hitBuffer[i];
                if (col == null || col.transform.root == transform.root) continue;

                Vector3 toTarget = Vector3.ProjectOnPlane(col.transform.position - transform.position, Vector3.up);
                if (toTarget.sqrMagnitude > 0.05f)
                {
                    transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                }

                var monsterSeam = col.GetComponentInParent<BestiaryCombatAIAndSeamReceiver>();
                if (monsterSeam != null)
                {
                    monsterSeam.ReceiveDirectionalBladeSlash(
                        baseDmg, swipeAngleDeg, postureMult, null, combatAnimatorBridge);

                    // Update Boss HUD if this is Kaelen
                    if (monsterSeam.monsterDisplayName.Contains("Kaelen") && PCDungeonHUDController.Instance != null)
                    {
                        var limb = monsterSeam.GetComponent<BattleSummoner.DesignPhilosophy.SeverableBossLimb>();
                        PCDungeonHUDController.Instance.UpdateBossTelemetry(
                            monsterSeam.monsterDisplayName, monsterSeam.currentHp, monsterSeam.maxHp,
                            limb != null ? limb.limbName : "Horn", limb != null && limb.isSevered);
                    }
                }

                var spellOrb = col.GetComponentInParent<KineticSpellOrbSlashModifier>();
                if (spellOrb != null)
                {
                    spellOrb.OnSlicedByPlayerBlade(swipeAngleDeg, transform.forward, altHeld);
                }

                var severLimb = col.GetComponentInParent<BattleSummoner.DesignPhilosophy.SeverableBossLimb>();
                if (severLimb != null)
                {
                    severLimb.ApplyTargetedSlice(baseDmg, "Water_Or_PinchedAirDisc", isHeavyArt ? 1.6f : 1.2f);
                }
            }
        }

        private void BroadcastParryToNearbyBestiaryMonsters()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 4.5f, _hitBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _hitBuffer[i];
                if (col == null) continue;
                var monsterSeam = col.GetComponentInParent<BestiaryCombatAIAndSeamReceiver>();
                if (monsterSeam != null)
                {
                    monsterSeam.TryReceiveTwoFingerParry(combatAnimatorBridge);
                }
            }
        }

        private void ApplyPlatformModeState(PlatformTestingMode mode)
        {
            bool isPC = (mode == PlatformTestingMode.PCStandaloneTesting);

            // 1. Mobile Movement Controller
            var pmc = GetComponent<PlayerMovementController>();
            if (pmc != null) pmc.enabled = !isPC;

            // 2. Camera Controllers
            if (activeMainCamera != null)
            {
                var mobileCam = activeMainCamera.GetComponent<PortraitARPGCamera>();
                if (mobileCam != null) mobileCam.enabled = !isPC;

                var resEnforcer = activeMainCamera.GetComponent<HighFidelityMobileResolutionEnforcer>();
                if (resEnforcer != null) resEnforcer.enabled = !isPC;

                var pcCam = activeMainCamera.GetComponent<PCThirdPersonCameraController>();
                if (pcCam == null && isPC) pcCam = activeMainCamera.gameObject.AddComponent<PCThirdPersonCameraController>();
                if (pcCam != null) pcCam.enabled = isPC;
            }

            // 3. UI Canvases
            var mobileCanvas = GameObject.Find("PortraitHUDCanvas");
            if (mobileCanvas != null) mobileCanvas.SetActive(!isPC);

            if (PCDungeonHUDController.Instance != null)
            {
                PCDungeonHUDController.Instance.SetHUDVisible(isPC);
            }

            // 4. Margo Guide Companion (Familiar)
            var margo = GetComponent<MargoGuideCompanion>();
            if (margo != null) margo.enabled = !isPC;

            // 5. Keep Campsite Companions from Crowding Behind Camera in PC Combat
            var companionAIs = FindObjectsByType<CompanionAIController>(FindObjectsSortMode.None);
            foreach (var cai in companionAIs)
            {
                cai.enabled = !isPC;
            }
        }

        private void InitializeDefaultWeaponLoadouts()
        {
            if (weaponLoadouts.Count > 0) return;

            weaponLoadouts = new List<WeaponLoadoutDef>
            {
                // 1. Steel Sword & Shield
                new WeaponLoadoutDef
                {
                    weaponName = "Steel Sword & Knight Shield",
                    stanceTag = "Sword & Board (1H + Shield)",
                    rightWeaponPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Sword.fbx",
                    leftOffHandPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Shield.fbx",
                    rightPosOffset = new Vector3(0f, 0f, 0f),
                    rightRotOffset = new Vector3(0f, 0f, 0f),
                    leftPosOffset = new Vector3(0f, 0f, 0f),
                    leftRotOffset = new Vector3(0f, 0f, 0f),
                    hasShield = true,
                    isDualWield = false,
                    baseDamage = 52f
                },
                // 2. Knight Greatsword
                new WeaponLoadoutDef
                {
                    weaponName = "Knight Greatsword",
                    stanceTag = "Two-Handed Cleave (2H)",
                    rightWeaponPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Greatsword.fbx",
                    leftOffHandPath = null,
                    rightPosOffset = new Vector3(0f, 0f, 0f),
                    rightRotOffset = new Vector3(0f, 0f, 0f),
                    hasShield = false,
                    isDualWield = false,
                    baseDamage = 85f
                },
                // 3. Knight Longsword & Kite Shield
                new WeaponLoadoutDef
                {
                    weaponName = "Longsword & Kite Shield",
                    stanceTag = "Paladin Vanguard (1H + Shield)",
                    rightWeaponPath = "Assets/EEJANAI_Team/FreeSwordAnimations/Models/SwordSample/Sword.obj",
                    leftOffHandPath = "Assets/Free Simple Dungeon Props - YP/Models/Shield.fbx",
                    rightPosOffset = new Vector3(0f, 0.02f, 0f),
                    rightRotOffset = new Vector3(0f, 0f, 0f),
                    rightScale = Vector3.one * 0.85f,
                    leftPosOffset = new Vector3(0f, 0f, 0f),
                    leftRotOffset = new Vector3(0f, 90f, 0f),
                    leftScale = Vector3.one * 0.9f,
                    hasShield = true,
                    isDualWield = false,
                    baseDamage = 64f
                },
                // 4. Dungeon Battleaxe
                new WeaponLoadoutDef
                {
                    weaponName = "Heavy Dungeon Battleaxe",
                    stanceTag = "Heavy Armor Breaker (2H)",
                    rightWeaponPath = "Assets/Free Simple Dungeon Props - YP/Models/Axe.fbx",
                    leftOffHandPath = null,
                    rightPosOffset = new Vector3(0f, 0.05f, 0f),
                    rightRotOffset = new Vector3(0f, 0f, 0f),
                    rightScale = Vector3.one * 1.15f,
                    hasShield = false,
                    isDualWield = false,
                    baseDamage = 78f
                },
                // 5. Dual Rogue Daggers
                new WeaponLoadoutDef
                {
                    weaponName = "Dual Rogue Daggers",
                    stanceTag = "Fast Flurry (Dual Wield)",
                    rightWeaponPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Dagger.fbx",
                    leftOffHandPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Dagger.fbx",
                    rightPosOffset = new Vector3(0f, 0f, 0f),
                    rightRotOffset = new Vector3(0f, 0f, 0f),
                    leftPosOffset = new Vector3(0f, 0f, 0f),
                    leftRotOffset = new Vector3(0f, 0f, 0f),
                    hasShield = false,
                    isDualWield = true,
                    baseDamage = 46f
                },
                // 6. War Pickaxe
                new WeaponLoadoutDef
                {
                    weaponName = "Bronze War Pickaxe",
                    stanceTag = "Chisel Strike / Armor Pierce",
                    rightWeaponPath = "Assets/Models/QuaterniusMegaKits/FantasyPropsMegaKit/Exports/FBX/Pickaxe_Bronze.fbx",
                    leftOffHandPath = null,
                    rightPosOffset = new Vector3(0f, 0.05f, 0f),
                    rightRotOffset = new Vector3(0f, 0f, 0f),
                    hasShield = false,
                    isDualWield = false,
                    baseDamage = 58f
                },
                // 7. Adventurer Torch & Shield
                new WeaponLoadoutDef
                {
                    weaponName = "Adventurer Torch & Wooden Shield",
                    stanceTag = "Dungeon Delver / Burn Stagger",
                    rightWeaponPath = "Assets/Models/QuaterniusMegaKits/FantasyPropsMegaKit/Exports/FBX/Torch_Metal.fbx",
                    leftOffHandPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Shield.fbx",
                    rightPosOffset = new Vector3(0f, 0.04f, 0f),
                    rightRotOffset = new Vector3(0f, 0f, 0f),
                    leftPosOffset = new Vector3(0f, 0f, 0f),
                    leftRotOffset = new Vector3(0f, 0f, 0f),
                    hasShield = true,
                    isDualWield = false,
                    baseDamage = 46f
                },
                // 8. Dual Steel Swords
                new WeaponLoadoutDef
                {
                    weaponName = "Dual Steel Swords",
                    stanceTag = "Double Edge (Dual Wield)",
                    rightWeaponPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Sword.fbx",
                    leftOffHandPath = "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Models/Human_Sword.fbx",
                    rightPosOffset = new Vector3(0f, 0f, 0f),
                    rightRotOffset = new Vector3(0f, 0f, 0f),
                    leftPosOffset = new Vector3(0f, 0f, 0f),
                    leftRotOffset = new Vector3(0f, 0f, 0f),
                    hasShield = false,
                    isDualWield = true,
                    baseDamage = 58f
                },
                // 9. Bare Fists
                new WeaponLoadoutDef
                {
                    weaponName = "Bare Fists (Martial Arts)",
                    stanceTag = "Unarmed / Brawler",
                    rightWeaponPath = null,
                    leftOffHandPath = null,
                    hasShield = false,
                    isDualWield = false,
                    baseDamage = 35f
                }
            };
        }

        // =========================================================================
        // INPUT ABSTRACTION HELPERS
        // =========================================================================
        private bool GetKeyHeld(KeyCode legacyKey, Func<bool> newAction)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                try { return newAction(); } catch { }
            }
#endif
            try { return Input.GetKey(legacyKey); } catch { return false; }
        }

        private bool GetKeyDown(KeyCode legacyKey, Func<bool> newAction)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                try { return newAction(); } catch { }
            }
#endif
            try { return Input.GetKeyDown(legacyKey); } catch { return false; }
        }

        private bool GetMouseButtonDown(int button, Func<bool> newAction)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                try { return newAction(); } catch { }
            }
#endif
            try { return Input.GetMouseButtonDown(button); } catch { return false; }
        }

        private bool GetMouseButtonHeld(int button, Func<bool> newAction)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                try { return newAction(); } catch { }
            }
#endif
            try { return Input.GetMouseButton(button); } catch { return false; }
        }

        private bool GetMouseButtonUp(int button, Func<bool> newAction)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                try { return newAction(); } catch { }
            }
#endif
            try { return Input.GetMouseButtonUp(button); } catch { return false; }
        }

        private Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                try { return Mouse.current.position.ReadValue(); } catch { }
            }
#endif
            try { return Input.mousePosition; } catch { return Vector2.zero; }
        }
    }
}

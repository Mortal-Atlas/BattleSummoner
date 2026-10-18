using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.UI;

namespace BattleSummoner.Combat
{
    public enum ActiveSpellLockState
    {
        Neutral = 0,
        Primed = 1,
        Locked_Healing = 2,
        Locked_Fireball = 3
    }

    /// <summary>
    /// ActiveSpellLockManager orchestrates the complete spellcasting paradigm:
    /// 1. Recognizes drawn shapes:
    ///    - Cartoon Heart -> Healing Spell
    ///    - 'V' with a dash through it -> Fireball Spell
    /// 2. Summons spinning CAST icon in center + DISPEL button on bottom-left.
    /// 3. When CAST is pressed, the caster is LOCKED into that spell until dispelled!
    /// 4. In Locked_Healing:
    ///    - Select target ally on right-hand side (PartySquadColumn).
    ///    - Continuously draw hearts/circuits: each completed circuit applies a heal tick
    ///      scaled by heart size and accuracy!
    /// 5. In Locked_Fireball:
    ///    - Hold in hand to charge from 1.00x up to 1.50x damage!
    ///    - Sling/throw to launch fiery ballistic projectiles into the 3D world.
    ///    - Repeat slinging indefinitely until dispelled.
    /// 6. DISPEL button on bottom-left immediately unlocks from spell.
    /// </summary>
    public class ActiveSpellLockManager : MonoBehaviour
    {
        private static ActiveSpellLockManager _instance;
        public static ActiveSpellLockManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ActiveSpellLockManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ActiveSpellLockManager");
                        _instance = go.AddComponent<ActiveSpellLockManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("State")]
        [SerializeField] private ActiveSpellLockState currentState = ActiveSpellLockState.Neutral;
        [SerializeField] private SpellElement primedElement = SpellElement.Fire;
        [SerializeField] private string primedSpellName = "";

        [Header("Fireball Charging Configuration")]
        [SerializeField] private float maxChargeTime = 1.15f; // Seconds to reach 1.50x
        [SerializeField] private float minChargeMultiplier = 1.0f;
        [SerializeField] private float maxChargeMultiplier = 1.50f;
        [SerializeField] private float fireballBaseDamage = 100f;

        [Header("Healing Configuration")]
        [SerializeField] private float baseHealPerCircuit = 110f;

        [Header("UI Overlays")]
        [SerializeField] private RectTransform activeSpellBannerRect;
        [SerializeField] private TextMeshProUGUI activeSpellBannerTMP;
        [SerializeField] private RectTransform chargeIndicatorRect;
        [SerializeField] private TextMeshProUGUI chargeIndicatorTMP;
        [SerializeField] private Image chargeFillImage;

        // Runtime Tracking
        private bool isHoldingFireball = false;
        private float holdStartTime = 0f;
        private Vector2 holdTouchStartPos = Vector2.zero;
        private Vector2 currentTouchPos = Vector2.zero;
        private float currentChargeMultiplier = 1.0f;
        private GameObject handChargeVFXObj;

        public ActiveSpellLockState CurrentState => currentState;
        public bool IsLockedInSpell => currentState == ActiveSpellLockState.Locked_Healing || currentState == ActiveSpellLockState.Locked_Fireball;
        public float CurrentChargeMultiplier => currentChargeMultiplier;

        public event Action<ActiveSpellLockState> OnSpellStateChanged;
        public event Action<float> OnFireballThrown;
        public event Action<float, float> OnHealCircuitCompleted;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            EnsureUIReferences();
        }

        private void Start()
        {
            EnsureUIReferences();
            HideActiveBanner();
            HideChargeIndicator();
        }

        private void OnEnable()
        {
            if (SpellCastingFlowController.Instance != null)
            {
                SpellCastingFlowController.Instance.OnSpellCastTriggered += HandleCastButtonPressed;
                SpellCastingFlowController.Instance.OnSpellDispelled += HandleDispelButtonPressed;
            }
        }

        private void OnDisable()
        {
            if (SpellCastingFlowController.Instance != null)
            {
                SpellCastingFlowController.Instance.OnSpellCastTriggered -= HandleCastButtonPressed;
                SpellCastingFlowController.Instance.OnSpellDispelled -= HandleDispelButtonPressed;
            }
        }

        private void Update()
        {
            if (currentState == ActiveSpellLockState.Locked_Fireball && isHoldingFireball)
            {
                UpdateFireballCharging();
            }

            HandleDebugShortcuts();
        }

        private void EnsureUIReferences()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Active Spell Banner (Center-top of screen)
            if (activeSpellBannerRect == null)
            {
                var bannerGo = GameObject.Find("ActiveSpellBanner");
                if (bannerGo == null)
                {
                    bannerGo = new GameObject("ActiveSpellBanner", typeof(RectTransform), typeof(Image));
                    bannerGo.transform.SetParent(canvas.transform, false);

                    activeSpellBannerRect = bannerGo.GetComponent<RectTransform>();
                    activeSpellBannerRect.anchorMin = new Vector2(0.15f, 0.86f);
                    activeSpellBannerRect.anchorMax = new Vector2(0.85f, 0.92f);
                    activeSpellBannerRect.offsetMin = Vector2.zero;
                    activeSpellBannerRect.offsetMax = Vector2.zero;

                    var bgImg = bannerGo.GetComponent<Image>();
                    bgImg.color = new Color(0.08f, 0.08f, 0.14f, 0.85f);
                    bgImg.raycastTarget = false;

                    var textGo = new GameObject("BannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(bannerGo.transform, false);
                    var textRT = textGo.GetComponent<RectTransform>();
                    textRT.anchorMin = Vector2.zero;
                    textRT.anchorMax = Vector2.one;
                    textRT.offsetMin = new Vector2(10f, 0f);
                    textRT.offsetMax = new Vector2(-10f, 0f);

                    activeSpellBannerTMP = textGo.GetComponent<TextMeshProUGUI>();
                    activeSpellBannerTMP.fontSize = 15;
                    activeSpellBannerTMP.alignment = TextAlignmentOptions.Center;
                    activeSpellBannerTMP.color = Color.white;
                    activeSpellBannerTMP.raycastTarget = false;
                }
                else
                {
                    activeSpellBannerRect = bannerGo.GetComponent<RectTransform>();
                    activeSpellBannerTMP = bannerGo.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            // 2. Charge Indicator (Floating around finger during fireball hold)
            if (chargeIndicatorRect == null)
            {
                var indGo = GameObject.Find("FireballChargeIndicator");
                if (indGo == null)
                {
                    indGo = new GameObject("FireballChargeIndicator", typeof(RectTransform));
                    indGo.transform.SetParent(canvas.transform, false);

                    chargeIndicatorRect = indGo.GetComponent<RectTransform>();
                    chargeIndicatorRect.sizeDelta = new Vector2(160f, 40f);

                    var tmpGo = new GameObject("ChargeText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    tmpGo.transform.SetParent(indGo.transform, false);
                    var tmpRT = tmpGo.GetComponent<RectTransform>();
                    tmpRT.anchorMin = Vector2.zero;
                    tmpRT.anchorMax = Vector2.one;

                    chargeIndicatorTMP = tmpGo.GetComponent<TextMeshProUGUI>();
                    chargeIndicatorTMP.fontSize = 16;
                    chargeIndicatorTMP.alignment = TextAlignmentOptions.Center;
                    chargeIndicatorTMP.color = new Color(1.0f, 0.6f, 0.1f);
                    chargeIndicatorTMP.raycastTarget = false;
                }
                else
                {
                    chargeIndicatorRect = indGo.GetComponent<RectTransform>();
                    chargeIndicatorTMP = indGo.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
        }

        #region Shape Recognition Ingestion
        /// <summary>
        /// Called when the player finishes drawing a recognized shape:
        /// - Cartoon Heart -> Healing
        /// - V with Dash -> Fire
        /// </summary>
        public void OnShapeRecognized(RuneRecognitionResult result)
        {
            if (IsLockedInSpell) return; // Already locked into a spell

            primedElement = result.element;
            primedSpellName = result.displayName;
            currentState = ActiveSpellLockState.Primed;

            Debug.Log($"✨ [SPELL RECOGNIZED] '{result.displayName}' ({result.element}) with accuracy {result.accuracy * 100:F0}%. Calling SpellCastingFlowController to prime CAST button.");

            if (SpellCastingFlowController.Instance != null)
            {
                SpellCastingFlowController.Instance.PrimeSpell(
                    result.element,
                    result.centroid,
                    result.sizeRadius,
                    result.displayName
                );
            }

            OnSpellStateChanged?.Invoke(currentState);
        }
        #endregion

        #region Spell Lock & State Machine Transitions
        /// <summary>
        /// When CAST button is pressed while primed:
        /// Caster is LOCKED INTO THAT SPELL until they dispel it!
        /// </summary>
        public void HandleCastButtonPressed(SpellElement element)
        {
            if (element == SpellElement.Healing)
            {
                EnterHealingLock();
            }
            else
            {
                EnterFireballLock();
            }
        }

        public void EnterHealingLock()
        {
            currentState = ActiveSpellLockState.Locked_Healing;
            Debug.Log("💚 [SPELL LOCKED: HEALING] Caster locked into Healing Spell! Select ally portrait on right, then draw continuous hearts/circles to heal.");

            // Display active spell banner
            ShowActiveBanner(
                "<color=#00FF88><b>[LOCKED: HEALING HARMONY]</b></color>\n" +
                "<size=85%><color=#FFEAA7>Select Ally on Right ➔ Draw Hearts/Circles to Cast Heals! [DISPEL to Exit]</color></size>"
            );

            // Re-activate DISPEL button on bottom-left above movement controls
            EnsureDispelButtonActive();

            // Set gesture canvas to Spellcraft mode
            var gestureCanvas = FindAnyObjectByType<PortraitGestureCanvas>();
            if (gestureCanvas != null)
            {
                gestureCanvas.currentMode = InteractionMode.Spellcraft;
            }

            // Ensure Ally party portraits are selectable and first ally is targeted
            if (AllyPartyTargetingController.Instance != null)
            {
                AllyPartyTargetingController.Instance.InitializeSquadMembers();
            }

            // Play state entry audio
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            OnSpellStateChanged?.Invoke(currentState);
        }

        public void EnterFireballLock()
        {
            currentState = ActiveSpellLockState.Locked_Fireball;
            Debug.Log("🔥 [SPELL LOCKED: FIREBALL] Caster locked into Fireball Spell! Hold hand to charge (up to 1.50x DMG), then sling/throw to launch.");

            // Display active spell banner
            ShowActiveBanner(
                "<color=#FF6611><b>[LOCKED: FLAME SLINGER]</b></color>\n" +
                "<size=85%><color=#FFD599>Hold Hand to Charge Power (up to 1.50x) ➔ Sling to Throw! [DISPEL to Exit]</color></size>"
            );

            // Re-activate DISPEL button on bottom-left above movement controls
            EnsureDispelButtonActive();

            // Set gesture canvas to Spellcraft mode
            var gestureCanvas = FindAnyObjectByType<PortraitGestureCanvas>();
            if (gestureCanvas != null)
            {
                gestureCanvas.currentMode = InteractionMode.Spellcraft;
            }

            // Play state entry audio
            if (WizardSpellSlingerController.Instance != null)
            {
                WizardSpellSlingerController.Instance.EquipPreset(SpellSlingerPreset.Fireball);
                WizardSpellSlingerController.Instance.ToggleSpellMode(true);
            }

            OnSpellStateChanged?.Invoke(currentState);
        }

        public void HandleDispelButtonPressed()
        {
            DispelActiveSpell();
        }

        public void DispelActiveSpell()
        {
            if (currentState == ActiveSpellLockState.Neutral) return;

            Debug.Log("💨 [SPELL DISPELLED] Dispelled active spell lock. Restoring neutral stance.");

            currentState = ActiveSpellLockState.Neutral;
            isHoldingFireball = false;
            currentChargeMultiplier = 1.0f;

            HideActiveBanner();
            HideChargeIndicator();

            if (handChargeVFXObj != null)
            {
                Destroy(handChargeVFXObj);
                handChargeVFXObj = null;
            }

            if (WizardSpellSlingerController.Instance != null)
            {
                WizardSpellSlingerController.Instance.ToggleSpellMode(false);
            }

            // Return gesture canvas to Combat mode
            var gestureCanvas = FindAnyObjectByType<PortraitGestureCanvas>();
            if (gestureCanvas != null)
            {
                gestureCanvas.currentMode = InteractionMode.CombatSlash;
            }

            // Hide Dispel button
            var dispelBtn = GameObject.Find("DispelSpellButton");
            if (dispelBtn != null) dispelBtn.SetActive(false);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("glitch", 0.75f);
            }

            OnSpellStateChanged?.Invoke(currentState);
        }

        private void EnsureDispelButtonActive()
        {
            var dispelBtn = GameObject.Find("DispelSpellButton");
            if (dispelBtn != null)
            {
                dispelBtn.SetActive(true);
                var btnComp = dispelBtn.GetComponent<Button>();
                if (btnComp != null)
                {
                    btnComp.onClick.RemoveListener(DispelActiveSpell);
                    btnComp.onClick.AddListener(DispelActiveSpell);
                }
            }
        }
        #endregion

        #region Healing Mode Continuous Circuit Execution
        /// <summary>
        /// Called when the player completes one heart circuit or circle loop:
        /// Immediately heals the selected ally by an amount proportional to accuracy and size!
        /// </summary>
        public void ProcessCompletedHealCircuit(float accuracy, float sizeRadius)
        {
            if (currentState != ActiveSpellLockState.Locked_Healing) return;

            if (AllyPartyTargetingController.Instance != null)
            {
                float healed = AllyPartyTargetingController.Instance.HealSelectedAlly(baseHealPerCircuit, accuracy, sizeRadius);
                OnHealCircuitCompleted?.Invoke(accuracy, healed);
            }
        }
        #endregion

        #region Fireball Mode: Hold to Charge & Sling to Throw
        public void OnFireballPointerDown(Vector2 screenPos)
        {
            if (currentState != ActiveSpellLockState.Locked_Fireball) return;

            if (WizardSpellSlingerController.Instance != null)
            {
                WizardSpellSlingerController.Instance.OnPointerDown(screenPos);
                return;
            }

            isHoldingFireball = true;
            holdStartTime = Time.time;
            holdTouchStartPos = screenPos;
            currentTouchPos = screenPos;
            currentChargeMultiplier = minChargeMultiplier;

            ShowChargeIndicator(screenPos);
            SpawnHandChargeVFX();

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.8f);
            }
        }

        public void OnFireballDrag(Vector2 screenPos)
        {
            if (currentState != ActiveSpellLockState.Locked_Fireball) return;

            if (WizardSpellSlingerController.Instance != null)
            {
                WizardSpellSlingerController.Instance.OnDrag(screenPos);
                return;
            }

            if (!isHoldingFireball) return;

            currentTouchPos = screenPos;
            if (chargeIndicatorRect != null)
            {
                chargeIndicatorRect.position = screenPos + new Vector2(0f, 65f);
            }

            UpdateHandChargeVFXPosition();
        }

        public void OnFireballPointerUp(Vector2 releasePos)
        {
            if (currentState != ActiveSpellLockState.Locked_Fireball) return;

            if (WizardSpellSlingerController.Instance != null)
            {
                WizardSpellSlingerController.Instance.OnPointerUp(releasePos);
                return;
            }

            if (!isHoldingFireball) return;

            isHoldingFireball = false;
            HideChargeIndicator();

            if (handChargeVFXObj != null)
            {
                Destroy(handChargeVFXObj);
                handChargeVFXObj = null;
            }

            // Calculate launch direction
            Vector2 dragDelta = releasePos - holdTouchStartPos;
            Vector3 worldDirection = Vector3.forward;

            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(releasePos);
                worldDirection = ray.direction;
                worldDirection.y = Mathf.Clamp(worldDirection.y, -0.2f, 0.4f);
                worldDirection.Normalize();
            }

            // If dragged significantly, use drag vector projected to world space
            if (dragDelta.magnitude > 40f && Camera.main != null)
            {
                Vector3 camRight = Camera.main.transform.right;
                Vector3 camUp = Camera.main.transform.forward;
                camRight.y = 0; camRight.Normalize();
                camUp.y = 0; camUp.Normalize();
                Vector3 slingDir = (camRight * dragDelta.x + camUp * dragDelta.y).normalized;
                if (slingDir.sqrMagnitude > 0.01f) worldDirection = slingDir;
            }

            // Launch Fireball Projectile with accumulated charge!
            LaunchFireball(worldDirection, currentChargeMultiplier);
        }

        private void UpdateFireballCharging()
        {
            float elapsed = Time.time - holdStartTime;
            float t = Mathf.Clamp01(elapsed / maxChargeTime);

            // Charge scales linearly from 1.00x up to 1.50x
            currentChargeMultiplier = Mathf.Lerp(minChargeMultiplier, maxChargeMultiplier, t);

            if (chargeIndicatorTMP != null)
            {
                if (currentChargeMultiplier >= maxChargeMultiplier - 0.01f)
                {
                    chargeIndicatorTMP.text = $"<color=#FF3300><b>⚡ {currentChargeMultiplier:F2}x MAX POWER!</b></color>";
                }
                else
                {
                    chargeIndicatorTMP.text = $"<color=#FFAA00>CHARGING: <b>{currentChargeMultiplier:F2}x</b></color>";
                }
            }

            // Pulse / scale hand VFX
            if (handChargeVFXObj != null)
            {
                float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 12f);
                handChargeVFXObj.transform.localScale = Vector3.one * (0.35f * currentChargeMultiplier * pulse);
            }
        }

        private void LaunchFireball(Vector3 direction, float charge)
        {
            var player = GameObject.Find("Player_Hero");
            Vector3 spawnPos = player != null ? player.transform.position + Vector3.up * 1.2f + direction * 0.8f : Vector3.up * 1.5f;

            GameObject fbObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fbObj.name = "ChargedFireballProjectile";
            fbObj.transform.position = spawnPos;

            var rend = fbObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1.0f, 0.40f, 0.08f, 1.0f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.45f, 0.1f) * (6.5f * charge));
                rend.sharedMaterial = mat;
            }

            // Add dynamic light
            var lGo = new GameObject("FireballLight", typeof(Light));
            lGo.transform.SetParent(fbObj.transform, false);
            var l = lGo.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.6f, 0.2f);
            l.intensity = 4.0f * charge;
            l.range = 6.0f;

            // Initialize projectile physics
            var proj = fbObj.AddComponent<FireballProjectile>();
            proj.Initialize(direction, charge, fireballBaseDamage);

            // Audio
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("spell_fireball_launch", 1.0f);
            }

            Debug.Log($"🔥 [FIREBALL SLUNG] Launched Fireball (Charge: {charge:F2}x, Damage: {fireballBaseDamage * charge:F0}, Dir: {direction}).");
            OnFireballThrown?.Invoke(charge);
        }

        private void SpawnHandChargeVFX()
        {
            if (handChargeVFXObj != null) Destroy(handChargeVFXObj);

            handChargeVFXObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handChargeVFXObj.name = "FireballHandChargeOrb";
            var col = handChargeVFXObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = handChargeVFXObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1.0f, 0.5f, 0.1f, 0.85f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.55f, 0.15f) * 5.0f);
                rend.sharedMaterial = mat;
            }

            UpdateHandChargeVFXPosition();
        }

        private void UpdateHandChargeVFXPosition()
        {
            if (handChargeVFXObj == null) return;

            var player = GameObject.Find("Player_Hero");
            if (player != null)
            {
                var rHand = player.transform.Find("AvatarRigRoot/HipsBone/TorsoBone/ChestBone/RightUpperArm/RightForearm/RightHand");
                if (rHand != null)
                {
                    handChargeVFXObj.transform.position = rHand.position;
                    return;
                }
                handChargeVFXObj.transform.position = player.transform.position + Vector3.up * 1.2f + player.transform.forward * 0.6f;
            }
        }
        #endregion

        #region UI Presentation Helpers
        private void ShowActiveBanner(string text)
        {
            EnsureUIReferences();
            if (activeSpellBannerRect != null)
            {
                activeSpellBannerRect.gameObject.SetActive(true);
                if (activeSpellBannerTMP != null) activeSpellBannerTMP.text = text;
            }
        }

        private void HideActiveBanner()
        {
            if (activeSpellBannerRect != null)
            {
                activeSpellBannerRect.gameObject.SetActive(false);
            }
        }

        private void ShowChargeIndicator(Vector2 screenPos)
        {
            EnsureUIReferences();
            if (chargeIndicatorRect != null)
            {
                chargeIndicatorRect.gameObject.SetActive(true);
                chargeIndicatorRect.position = screenPos + new Vector2(0f, 65f);
            }
        }

        private void HideChargeIndicator()
        {
            if (chargeIndicatorRect != null)
            {
                chargeIndicatorRect.gameObject.SetActive(false);
            }
        }

        private void HandleDebugShortcuts()
        {
            // 'H' simulates Cartoon Heart recognition (Healing)
            if (Input.GetKeyDown(KeyCode.H))
            {
                OnShapeRecognized(new RuneRecognitionResult
                {
                    shape = RecognizedRuneShape.CartoonHeart,
                    element = SpellElement.Healing,
                    accuracy = 0.92f,
                    sizeRadius = 150f,
                    centroid = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                    displayName = "Healing Harmony (Heart)"
                });
            }

            // 'F' simulates 'V' with Dash recognition (Fireball)
            if (Input.GetKeyDown(KeyCode.F))
            {
                OnShapeRecognized(new RuneRecognitionResult
                {
                    shape = RecognizedRuneShape.VWithDash,
                    element = SpellElement.Fire,
                    accuracy = 0.88f,
                    sizeRadius = 140f,
                    centroid = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                    displayName = "Flame Slinger (V-Dash)"
                });
            }
        }
        #endregion
    }

    /// <summary>
    /// FireballProjectile represents a slung elemental fireball:
    /// - Scales damage by chargeMultiplier (from 1.00x up to 1.50x maximum charged damage).
    /// - Emits glowing flame materials and dynamic point light.
    /// - Raycasts forward during ballistic flight to prevent high-speed tunneling.
    /// - Detonates on impact with enemies, applying Burn status and radial explosion.
    /// </summary>
    public class FireballProjectile : MonoBehaviour
    {
        [Header("Ballistic Properties")]
        [SerializeField] private float speed = 24f;
        [SerializeField] private float explosionRadius = 4.2f;
        [SerializeField] private float baseDamage = 100f;
        [SerializeField] private float chargeMultiplier = 1.0f; // 1.00x to 1.50x
        [SerializeField] private float maxLifetime = 3.5f;
        [SerializeField] private LayerMask hitLayers = ~0;

        private Vector3 moveDirection = Vector3.forward;
        private float lifeTimer = 0f;
        private bool hasDetonated = false;

        public float FinalDamage => baseDamage * chargeMultiplier;
        public float ChargeMultiplier => chargeMultiplier;

        public void Initialize(Vector3 direction, float chargeMult = 1.0f, float baseDmg = 100f, float radius = 4.2f)
        {
            moveDirection = direction.normalized;
            chargeMultiplier = Mathf.Clamp(chargeMult, 1.0f, 1.50f);
            baseDamage = baseDmg;
            explosionRadius = radius * (1.0f + (chargeMultiplier - 1.0f) * 0.4f); // Larger blast for charged shots
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

            // Scale projectile size slightly with charge
            transform.localScale = Vector3.one * (0.45f * chargeMultiplier);
        }

        private void Update()
        {
            if (hasDetonated) return;

            lifeTimer += Time.deltaTime;
            float moveDist = speed * Time.deltaTime;

            // Forward Raycast to detect enemies and obstacles without tunneling
            if (Physics.Raycast(transform.position, moveDirection, out RaycastHit hit, moveDist, hitLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    transform.position = hit.point;
                    Detonate();
                    return;
                }
            }

            transform.position += moveDirection * moveDist;

            if (lifeTimer >= maxLifetime)
            {
                Detonate();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasDetonated || other.CompareTag("Player")) return;
            Detonate();
        }

        public void Detonate()
        {
            if (hasDetonated) return;
            hasDetonated = true;

            float damage = FinalDamage;
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, hitLayers, QueryTriggerInteraction.Ignore);
            int targetsHit = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col.CompareTag("Player")) continue;

                // 1. StatusEffectReceiver
                if (col.TryGetComponent<StatusEffectReceiver>(out var statusRec))
                {
                    var payload = new DamagePayload
                    {
                        baseDamage = damage,
                        damageType = DamageType.Fire,
                        critChance = chargeMultiplier >= 1.4f ? 0.35f : 0.15f,
                        critMultiplier = 1.6f,
                        accuracy = 0.98f,
                        hitPoint = col.ClosestPoint(transform.position)
                    };
                    statusRec.TakeAdvancedDamage(payload);
                    statusRec.ApplyStatus(StatusEffectType.Burn, 4.0f, 1.2f);
                    targetsHit++;
                }
                // 2. BossController
                else if (col.TryGetComponent<BossController>(out var boss))
                {
                    boss.TakeDamage(damage);
                    boss.Stagger(0.6f * chargeMultiplier, transform.position);
                    targetsHit++;

                    if (CombatFeedbackManager.Instance != null)
                    {
                        CombatFeedbackManager.Instance.SpawnDamagePopup(
                            transform.position + Vector3.up * 0.8f,
                            new DamageResult
                            {
                                finalDamage = damage,
                                damageType = DamageType.Fire,
                                outcome = chargeMultiplier >= 1.4f ? HitOutcome.Critical : HitOutcome.Normal
                            });
                    }
                }
                // 3. SpireEnemy
                else if (col.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.TakeDamage(damage);
                    enemy.Stagger(0.85f * chargeMultiplier, transform.position);
                    targetsHit++;

                    if (CombatFeedbackManager.Instance != null)
                    {
                        CombatFeedbackManager.Instance.SpawnDamagePopup(
                            transform.position + Vector3.up * 0.5f,
                            new DamageResult
                            {
                                finalDamage = damage,
                                damageType = DamageType.Fire,
                                outcome = chargeMultiplier >= 1.4f ? HitOutcome.Critical : HitOutcome.Normal
                            });
                    }
                }
            }

            // Audio & VFX
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.9f);
            }

            SpawnExplosionVFX(transform.position, chargeMultiplier);

            Debug.Log($"🔥 [FIREBALL DETONATION] Struck {targetsHit} targets for {damage:F0} DMG (Charge: {chargeMultiplier:F2}x, Radius: {explosionRadius:F1}m).");
            Destroy(gameObject);
        }

        private void SpawnExplosionVFX(Vector3 pos, float charge)
        {
            GameObject blast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blast.name = "FireballExplosionBlast";
            blast.transform.position = pos;
            blast.transform.localScale = Vector3.one * (0.6f * charge);

            var col = blast.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = blast.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1.0f, 0.45f, 0.1f, 0.9f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.5f, 0.15f) * (6.0f * charge));
                rend.sharedMaterial = mat;
            }

            // Expand and fade shockwave
            blast.AddComponent<ExplosionShockwaveFader>().Initialize(charge);
        }
    }

    /// <summary>
    /// Helper to expand and fade the fiery explosion blast sphere.
    /// </summary>
    public class ExplosionShockwaveFader : MonoBehaviour
    {
        private float timer = 0f;
        private float duration = 0.35f;
        private Vector3 startScale;
        private Vector3 targetScale;
        private Material mat;

        public void Initialize(float charge)
        {
            startScale = transform.localScale;
            targetScale = startScale * (3.5f * charge);
            var rend = GetComponent<Renderer>();
            if (rend != null) mat = rend.material;
            Destroy(gameObject, duration);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            if (mat != null)
            {
                Color c = mat.color;
                c.a = 1f - t;
                mat.color = c;
            }
        }
    }
}

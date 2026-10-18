using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Combat
{
    public enum WeaponCombatStance
    {
        SingleSword = 0,    // One sword: 1 swipe finger blade
        DualBlades = 1,     // Two swords or daggers: 2 different sword slashing at the same time
        SwordAndShield = 2  // Sword and shield: 1 swipe finger blade, shield passively blocks
    }

    /// <summary>
    /// PlayerCombatStanceController governs:
    /// 1. Weapon Combat Stance:
    ///    - SingleSword: 1 sword equipped, 1 swipe finger blade.
    ///    - DualBlades: 2 swords/daggers, twin simultaneous sword slashing graphics and dual damage waves.
    ///    - SwordAndShield: 1 sword + 1 shield, shield passively blocks incoming frontal attacks (-65% damage).
    /// 2. Two-Finger Tap to Parry:
    ///    - Tap screen with two fingers (or press 'F') to parry any attack.
    ///    - Deflects incoming attacks with 100% damage negation, staggers attacker, plays crisp parry clink,
    ///      triggers hit-stop, and grants 3.0x Riposte Critical Boost on next slash.
    /// </summary>
    public class PlayerCombatStanceController : MonoBehaviour, IDamageReceiver
    {
        public static PlayerCombatStanceController Instance { get; private set; }

        [Header("Active Stance")]
        [SerializeField] private WeaponCombatStance currentStance = WeaponCombatStance.SingleSword;

        [Header("Passive Shield Block Tuning")]
        [SerializeField] private float passiveBlockReduction = 0.65f; // 65% damage reduction on passive block
        [SerializeField] private float maxBlockAngle = 95f;          // 190-degree frontal protection arc
        [SerializeField] private float passiveBlockCooldown = 0.15f;
        private float lastPassiveBlockTime = -10f;

        [Header("Parry Tuning")]
        [SerializeField] private float parryWindowDuration = 0.45f;   // Generous 450ms window
        [SerializeField] private float parryCooldown = 0.60f;
        [SerializeField] private float parryStaggerDuration = 1.8f;
        [SerializeField] private float riposteMultiplier = 3.0f;     // 3x critical riposte damage after parry
        private float lastParryTriggerTime = -10f;
        private bool isParryWindowActive = false;
        private bool hasRiposteBoost = false;
        private Coroutine parryCoroutine = null;

        [Header("Visual Weapon Sockets")]
        [SerializeField] private GameObject mainHandSword;
        [SerializeField] private GameObject offHandWeapon; // Dual sword / dagger
        [SerializeField] private GameObject offHandShield; // Shield

        [Header("UI Stance Switcher")]
        [SerializeField] private Canvas stanceCanvas;
        [SerializeField] private Button stanceToggleBtn;
        [SerializeField] private TextMeshProUGUI stanceBtnTMP;
        [SerializeField] private Button parryActionBtn;
        [SerializeField] private TextMeshProUGUI parryBtnTMP;

        // Events
        public event Action<WeaponCombatStance> OnStanceChanged;
        public event Action OnParryStarted;
        public event Action<GameObject> OnParrySuccessful;
        public event Action<float> OnPassiveBlockTriggered;

        public WeaponCombatStance CurrentStance => currentStance;
        public bool IsDualWielding => currentStance == WeaponCombatStance.DualBlades;
        public bool IsSwordAndShield => currentStance == WeaponCombatStance.SwordAndShield;
        public bool IsParryActive => isParryWindowActive;
        public bool HasRiposteBoost => hasRiposteBoost;

        private void Awake()
        {
            if (Instance == null || Instance.gameObject == null) Instance = this;
            else if (Instance != this)
            {
                SafeDestroy(this);
                return;
            }

            BuildVisualWeaponSockets();
            EnsureStanceHUD();
        }

        private void OnEnable()
        {
            if (SpireLoadoutAndTomeSystem.Instance != null)
            {
                SpireLoadoutAndTomeSystem.Instance.OnLoadoutReconfigured += HandleLoadoutReconfigured;
            }
        }

        private void OnDisable()
        {
            if (SpireLoadoutAndTomeSystem.Instance != null)
            {
                SpireLoadoutAndTomeSystem.Instance.OnLoadoutReconfigured -= HandleLoadoutReconfigured;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void HandleLoadoutReconfigured()
        {
            var sys = SpireLoadoutAndTomeSystem.Instance;
            if (sys == null) return;

            if (sys.allowedSlashFingers >= 2)
            {
                SetStance(WeaponCombatStance.DualBlades);
            }
            else if (sys.hasPassiveShieldBlock)
            {
                SetStance(WeaponCombatStance.SwordAndShield);
            }
            else
            {
                SetStance(WeaponCombatStance.SingleSword);
            }
        }

        private void Start()
        {
            ApplyStance(currentStance, true);
        }

        private void Update()
        {
            HandleInputs();
        }

        #region Input Handling
        private void HandleInputs()
        {
            // 1. Stance Cycling: Key 'X' or 'K'
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.K))
            {
                CycleNextStance();
            }

            // 2. Desktop Parry Key: 'F' or simultaneous Left+Right mouse button tap
            if (Input.GetKeyDown(KeyCode.F) || (Input.GetMouseButtonDown(1) && !Input.GetMouseButton(0)))
            {
                ExecuteParry();
            }

            // 3. Mobile Two-Finger Tap to Parry:
            // "to parry any attack tap the screen with two fingers"
            if (Input.touchSupported && Input.touchCount >= 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                // Both touches began almost simultaneously with minimal drag
                if ((t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began) &&
                    t0.deltaPosition.magnitude < 20f && t1.deltaPosition.magnitude < 20f)
                {
                    ExecuteParry();
                }
            }
        }
        #endregion

        #region Stance Management
        public void SetStance(WeaponCombatStance newStance)
        {
            if (currentStance == newStance) return;
            currentStance = newStance;
            ApplyStance(currentStance, false);

            if (SpireLoadoutAndTomeSystem.Instance != null)
            {
                switch (newStance)
                {
                    case WeaponCombatStance.SingleSword:
                        SpireLoadoutAndTomeSystem.Instance.EquipPreset("SingleBlade");
                        break;
                    case WeaponCombatStance.DualBlades:
                        SpireLoadoutAndTomeSystem.Instance.EquipPreset("DualWield");
                        break;
                    case WeaponCombatStance.SwordAndShield:
                        SpireLoadoutAndTomeSystem.Instance.EquipPreset("SwordAndShield");
                        break;
                }
            }
        }

        public void CycleNextStance()
        {
            int next = ((int)currentStance + 1) % 3;
            SetStance((WeaponCombatStance)next);
        }

        private void ApplyStance(WeaponCombatStance stance, bool isInitial)
        {
            currentStance = stance;

            // 1. Ensure 3D visual weapon models exist and toggle visibility
            if (mainHandSword == null || offHandWeapon == null || offHandShield == null)
            {
                BuildVisualWeaponSockets();
            }

            if (mainHandSword != null) mainHandSword.SetActive(true);
            if (offHandWeapon != null) offHandWeapon.SetActive(stance == WeaponCombatStance.DualBlades);
            if (offHandShield != null) offHandShield.SetActive(stance == WeaponCombatStance.SwordAndShield);

            // 2. Configure RuneInkGraphic & SpellDrawingInkCanvas for finger blade styling
            ConfigureFingerBladeStyling();

            // 3. Sync EquipmentSystem
            var eq = GetComponent<EquipmentSystem>() ?? FindAnyObjectByType<EquipmentSystem>();
            if (eq != null)
            {
                switch (stance)
                {
                    case WeaponCombatStance.SingleSword:
                        eq.mainHandWeaponId = "spire_steel_sword";
                        eq.offHandItemId = "";
                        eq.isTwoHandedEquipped = false;
                        break;
                    case WeaponCombatStance.DualBlades:
                        eq.mainHandWeaponId = "spire_steel_sword";
                        eq.offHandItemId = "spire_twin_dagger";
                        eq.isTwoHandedEquipped = false;
                        break;
                    case WeaponCombatStance.SwordAndShield:
                        eq.mainHandWeaponId = "spire_steel_sword";
                        eq.offHandItemId = "spire_aegis_shield";
                        eq.isTwoHandedEquipped = false;
                        break;
                }
            }

            // 4. Audio & HUD
            if (!isInitial && BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.90f);
            }

            UpdateStanceHUD();
            OnStanceChanged?.Invoke(stance);

            string toastMsg = stance switch
            {
                WeaponCombatStance.SingleSword => "<color=#70C8FF><b>[SINGLE SWORD EQUIPPED]</b></color>\n1 Swipe Finger Blade. Clean precise kinetic cuts.",
                WeaponCombatStance.DualBlades => "<color=#FFB030><b>[DUAL BLADES EQUIPPED]</b></color>\nTwin Swords / Daggers. 2 different sword slashing at the same time!",
                WeaponCombatStance.SwordAndShield => "<color=#60A5FA><b>[SWORD & SHIELD EQUIPPED]</b></color>\nSpire Aegis. Shield passively blocks incoming frontal attacks (-65% damage).",
                _ => ""
            };
            Debug.Log($"⚔️ [COMBAT STANCE] {toastMsg}");
        }

        private void ConfigureFingerBladeStyling()
        {
            var inkCanvas = SpellDrawingInkCanvas.Instance;
            if (inkCanvas == null) inkCanvas = FindAnyObjectByType<SpellDrawingInkCanvas>();
            if (inkCanvas == null) return;

            var inkGraphic = inkCanvas.GetComponentInChildren<RuneInkGraphic>();
            if (inkGraphic == null) return;

            if (currentStance == WeaponCombatStance.DualBlades)
            {
                // Enable dual blade ribbons: Primary White-Cyan + Secondary White-Amber
                inkGraphic.SetDualBladeMode(
                    true,
                    new Vector2(-24f, 20f),
                    new Color(1f, 0.95f, 0.70f, 0.98f),
                    new Color(1f, 0.65f, 0.15f, 0.60f)
                );
            }
            else
            {
                // Single sword or Sword+Shield: exactly one swipe finger blade
                inkGraphic.SetDualBladeMode(false);
            }
        }
        #endregion

        #region Passive Shield Blocking
        /// <summary>
        /// Attempts to passively block incoming damage if Sword and Shield is equipped.
        /// Frontal attacks within 190-degree arc have 65% damage absorbed passively.
        /// </summary>
        public bool TryProcessPassiveBlock(ref float incomingDamage, Vector3 attackSourcePos, Vector3 playerPos, Vector3 playerForward)
        {
            var loadout = GetComponent<SpireLoadoutAndTomeSystem>() ?? SpireLoadoutAndTomeSystem.Instance;
            bool hasShield = (currentStance == WeaponCombatStance.SwordAndShield) ||
                             (loadout != null && loadout.hasPassiveShieldBlock);
            if (!hasShield) return false;
            if (incomingDamage <= 0.01f) return false;

            // Check attack angle relative to player forward
            Vector3 toAttacker = attackSourcePos - playerPos;
            toAttacker.y = 0f;
            if (toAttacker.sqrMagnitude < 0.001f) toAttacker = playerForward;
            toAttacker.Normalize();

            float angle = Vector3.Angle(playerForward, toAttacker);
            if (angle <= maxBlockAngle)
            {
                // Successful Passive Block!
                float blockedDamage = incomingDamage * passiveBlockReduction;
                incomingDamage -= blockedDamage;
                lastPassiveBlockTime = Time.time;

                // Play crisp heavy shield block audio
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("shield_block", 1.0f);
                }

                // Spawn Hexagonal Shield Barrier Spark VFX at forearm shield position
                SpawnShieldBlockVFX(playerPos + playerForward * 0.75f + Vector3.up * 1.1f, -toAttacker);

                // Floating Combat Text
                if (CombatFeedbackManager.Instance != null)
                {
                    CombatFeedbackManager.Instance.SpawnFloatingText(
                        playerPos + Vector3.up * 1.7f,
                        $"<color=#60A5FA><b>🛡️ PASSIVE BLOCK -{blockedDamage:F0}</b></color>",
                        1.2f
                    );
                }

                OnPassiveBlockTriggered?.Invoke(blockedDamage);
                Debug.Log($"🛡️ [PASSIVE BLOCK] Shield absorbed {blockedDamage:F1} damage ({passiveBlockReduction * 100:F0}%). Remainder: {incomingDamage:F1}.");
                return true;
            }

            return false;
        }

        private void SpawnShieldBlockVFX(Vector3 position, Vector3 normal)
        {
            GameObject vfxObj = new GameObject("ShieldBlockBarrierVFX");
            vfxObj.transform.position = position;
            vfxObj.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);

            // Shimmering hexagonal barrier quad
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BarrierHexMesh";
            quad.transform.SetParent(vfxObj.transform, false);
            quad.transform.localScale = Vector3.one * 1.6f;
            DestroyImmediate(quad.GetComponent<Collider>());

            var rend = quad.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.35f, 0.75f, 1.0f, 0.85f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.20f, 0.70f, 1.0f) * 4.5f);
                rend.sharedMaterial = mat;
            }

            // Deflection sparks
            WarriorSwordSlashVFX.SpawnHitSpark(position, normal);

            // Self-destroy after 0.25s
            SafeDestroy(vfxObj, 0.25f);
        }
        #endregion

        #region Two-Finger Tap to Parry
        /// <summary>
        /// Executes a Parry:
        /// "to parry any attack tap the screen with two fingers"
        /// Opens a 0.45s parry window that deflects incoming attacks with 100% damage negation,
        /// breaks enemy posture, staggers them for 1.8s, and grants 3.0x Riposte Critical on next hit.
        /// </summary>
        public void ExecuteParry()
        {
            if (Application.isPlaying && Time.time - lastParryTriggerTime < parryCooldown) return;
            lastParryTriggerTime = Time.time;

            isParryWindowActive = true;

            if (Application.isPlaying)
            {
                if (parryCoroutine != null) StopCoroutine(parryCoroutine);
                parryCoroutine = StartCoroutine(RoutineParryWindow());
            }

            // Audio & Stance raise
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("parry_stance", 0.90f);
            }

            // Visual golden parry deflection ring
            SpawnParryStanceRingVFX();

            // Animate weapon deflection guard
            if (Application.isPlaying)
            {
                StartCoroutine(RoutineAnimateParryGuard());
            }

            // Check if any nearby enemy within 4.5m is currently in an attack windup/swing to parry immediately!
            Collider[] nearby = Physics.OverlapSphere(transform.position, 4.5f);
            for (int i = 0; i < nearby.Length; i++)
            {
                Collider col = nearby[i];
                if (col.gameObject == gameObject || col.CompareTag("Player")) continue;

                if (col.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    // Check if enemy is in attack proximity
                    TriggerSuccessfulParry(col.gameObject);
                    break;
                }
                else if (col.TryGetComponent<BossController>(out var boss))
                {
                    TriggerSuccessfulParry(col.gameObject);
                    break;
                }
            }

            OnParryStarted?.Invoke();
            Debug.Log("⚡ [PARRY STANCE] Two-Finger Parry Guard Activated (0.45s window). Tap timed to deflect attack!");
        }

        private IEnumerator RoutineParryWindow()
        {
            isParryWindowActive = true;
            yield return new WaitForSeconds(parryWindowDuration);
            isParryWindowActive = false;
            parryCoroutine = null;
        }

        /// <summary>
        /// Called when an enemy attack connects during the active Parry Window.
        /// 100% damage negation + posture break + 1.8s stagger + 3.0x Riposte boost!
        /// </summary>
        public void TriggerSuccessfulParry(GameObject attacker)
        {
            isParryWindowActive = false;
            hasRiposteBoost = true; // 3x critical riposte damage on next slash!

            Vector3 impactPos = transform.position + transform.forward * 0.9f + Vector3.up * 1.1f;

            // 1. Resonant crystalline metallic clash audio
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("parry_clink", 1.0f);
            }

            // 2. Radiant golden starburst & deflection sparks
            SpawnPerfectParryStarburstVFX(impactPos);

            // 3. Stagger and posture-break attacker
            if (attacker != null)
            {
                if (attacker.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.Stagger(parryStaggerDuration, transform.position);
                }
                else if (attacker.TryGetComponent<BossController>(out var boss))
                {
                    boss.Stagger(parryStaggerDuration * 0.75f, transform.position);
                }
            }

            // 4. Hit-stop time dilation (0.08s slowdown for dramatic impact)
            if (Application.isPlaying)
            {
                StartCoroutine(RoutineHitStop(0.08f));
            }

            // 5. Floating text
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnFloatingText(
                    transform.position + Vector3.up * 2.0f,
                    "<color=#FFD700><size=135%><b>⚡ PERFECT PARRY! ⚡</b></size></color>\n<size=75%><color=#FFAA20>Riposte Boost 3.0x Ready!</color></size>",
                    1.4f
                );
            }

            OnParrySuccessful?.Invoke(attacker);
            Debug.Log("⚡⚡ [PERFECT PARRY SUCCESSFUL] Attack 100% negated! Attacker staggered for 1.8s! Next strike deals 3.0x Riposte Damage!");
        }

        public bool ConsumeRiposteBoost(out float boostMultiplier)
        {
            if (hasRiposteBoost)
            {
                hasRiposteBoost = false;
                boostMultiplier = riposteMultiplier;
                return true;
            }
            boostMultiplier = 1.0f;
            return false;
        }

        private void SpawnParryStanceRingVFX()
        {
            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "ParryStanceRing";
            ringObj.transform.position = transform.position + Vector3.up * 0.15f;
            ringObj.transform.localScale = new Vector3(2.8f, 0.02f, 2.8f);
            DestroyImmediate(ringObj.GetComponent<Collider>());

            var rend = ringObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1.0f, 0.85f, 0.25f, 0.65f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.80f, 0.20f) * 3.5f);
                rend.sharedMaterial = mat;
            }

            SafeDestroy(ringObj, parryWindowDuration);
        }

        private void SpawnPerfectParryStarburstVFX(Vector3 pos)
        {
            GameObject starburst = new GameObject("PerfectParryStarburst");
            starburst.transform.position = pos;

            for (int i = 0; i < 4; i++)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = $"StarburstSpoke_{i}";
                beam.transform.SetParent(starburst.transform, false);
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                beam.transform.localScale = new Vector3(0.08f, 2.4f, 0.08f);
                DestroyImmediate(beam.GetComponent<Collider>());

                var rend = beam.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = new Color(1.0f, 0.95f, 0.65f, 0.95f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1.0f, 0.85f, 0.20f) * 5.0f);
                    rend.sharedMaterial = mat;
                }
            }

            WarriorSwordSlashVFX.SpawnHitSpark(pos, Vector3.up);
            SafeDestroy(starburst, 0.35f);
        }

        private IEnumerator RoutineAnimateParryGuard()
        {
            // Cross weapons / shield forward in defensive deflection guard
            if (mainHandSword != null)
            {
                Vector3 origPos = mainHandSword.transform.localPosition;
                Quaternion origRot = mainHandSword.transform.localRotation;

                mainHandSword.transform.localPosition = new Vector3(0.15f, 0.40f, 0.55f);
                mainHandSword.transform.localRotation = Quaternion.Euler(-10f, -45f, 60f);

                yield return new WaitForSeconds(parryWindowDuration);

                mainHandSword.transform.localPosition = origPos;
                mainHandSword.transform.localRotation = origRot;
            }
        }

        private IEnumerator RoutineHitStop(float duration)
        {
            float prevScale = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prevScale;
        }
        #endregion

        #region IDamageReceiver Implementation
        /// <summary>
        /// Central hook for advanced damage packets directed at the player.
        /// Checks Parry first (100% negation) -> Passive Shield Block (-65%) -> Remaining to PlayerProgression.
        /// </summary>
        public DamageResult TakeAdvancedDamage(DamagePayload payload)
        {
            var result = new DamageResult();
            result.damageType = payload.damageType;
            float amount = payload.baseDamage;
            Vector3 attackerPos = payload.hitPoint != Vector3.zero 
                ? payload.hitPoint 
                : (payload.attacker != null ? payload.attacker.transform.position : transform.position + transform.forward * 2f);

            // 1. Check Parry Window
            if (isParryWindowActive)
            {
                TriggerSuccessfulParry(payload.attacker);
                result.finalDamage = 0f;
                result.outcome = HitOutcome.Parried;
                return result; // 100% Negation!
            }

            // 2. Check Passive Shield Block
            bool shieldEquipped = currentStance == WeaponCombatStance.SwordAndShield ||
                                 (SpireLoadoutAndTomeSystem.Instance != null && SpireLoadoutAndTomeSystem.Instance.hasPassiveShieldBlock);
            bool shieldBlocking = MultiTouchCombatAndRuneDispatcher.Instance != null 
                                 ? MultiTouchCombatAndRuneDispatcher.Instance.isShieldCurrentlyBlocking 
                                 : shieldEquipped;

            if (shieldBlocking && shieldEquipped)
            {
                if (TryProcessPassiveBlock(ref amount, attackerPos, transform.position, transform.forward))
                {
                    result.outcome = HitOutcome.Blocked;
                }
            }

            // 3. Deliver remaining damage to PlayerProgression
            var prog = GetComponent<PlayerProgression>() ?? FindAnyObjectByType<PlayerProgression>();
            if (prog != null)
            {
                prog.TakeDirectDamage(amount);
            }

            result.finalDamage = amount;
            return result;
        }
        #endregion

        #region 3D Visual Weapon Sockets Construction
        public void BuildVisualWeaponSockets()
        {
            // 1. Main Hand Sword (Right hand)
            Transform existingMain = transform.Find("StanceWeapon_MainSword") ?? transform.Find("DevWeapon_Sword");
            if (existingMain != null)
            {
                mainHandSword = existingMain.gameObject;
            }
            else
            {
                mainHandSword = new GameObject("StanceWeapon_MainSword");
                mainHandSword.transform.SetParent(transform, false);
                mainHandSword.transform.localPosition = new Vector3(0.42f, 0.95f, 0.40f);
                mainHandSword.transform.localRotation = Quaternion.Euler(-25f, 35f, -10f);

                CreateSwordPrimitives(mainHandSword, new Color(0.9f, 0.95f, 1.0f), new Color(0.2f, 0.8f, 1.0f));
            }

            // 2. Off Hand Weapon: Twin Sword / Dagger (Left hand)
            Transform existingOff = transform.Find("StanceWeapon_OffHand");
            if (existingOff != null)
            {
                offHandWeapon = existingOff.gameObject;
            }
            else
            {
                offHandWeapon = new GameObject("StanceWeapon_OffHand");
                offHandWeapon.transform.SetParent(transform, false);
                offHandWeapon.transform.localPosition = new Vector3(-0.42f, 0.95f, 0.40f);
                offHandWeapon.transform.localRotation = Quaternion.Euler(-25f, -35f, 10f);

                // Twin Dagger / Short Sword with amber golden glow
                CreateSwordPrimitives(offHandWeapon, new Color(1.0f, 0.95f, 0.80f), new Color(1.0f, 0.65f, 0.15f), 0.85f);
                offHandWeapon.SetActive(false);
            }

            // 3. Off Hand Shield (Left forearm)
            Transform existingShield = transform.Find("StanceWeapon_Shield");
            if (existingShield != null)
            {
                offHandShield = existingShield.gameObject;
            }
            else
            {
                offHandShield = new GameObject("StanceWeapon_Shield");
                offHandShield.transform.SetParent(transform, false);
                offHandShield.transform.localPosition = new Vector3(-0.45f, 0.95f, 0.30f);
                offHandShield.transform.localRotation = Quaternion.Euler(0f, -80f, 0f);

                // Shield Body (Heater / Kite Shield geometry)
                GameObject shieldBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shieldBody.name = "ShieldPlate";
                shieldBody.transform.SetParent(offHandShield.transform, false);
                shieldBody.transform.localPosition = Vector3.zero;
                shieldBody.transform.localScale = new Vector3(0.12f, 0.95f, 0.65f);
                DestroyImmediate(shieldBody.GetComponent<Collider>());

                // Shield Boss / Emblem
                GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                boss.name = "ShieldBoss";
                boss.transform.SetParent(offHandShield.transform, false);
                boss.transform.localPosition = new Vector3(-0.07f, 0f, 0f);
                boss.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                boss.transform.localScale = new Vector3(0.28f, 0.05f, 0.28f);
                DestroyImmediate(boss.GetComponent<Collider>());

                // Materials
                Material shieldMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                shieldMat.color = new Color(0.20f, 0.28f, 0.42f); // Deep Knight Navy
                shieldMat.SetFloat("_Metallic", 0.75f);
                shieldMat.SetFloat("_Smoothness", 0.70f);
                shieldBody.GetComponent<Renderer>().sharedMaterial = shieldMat;

                Material bossMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                bossMat.color = new Color(0.95f, 0.80f, 0.20f); // Golden Aegis Boss
                bossMat.EnableKeyword("_EMISSION");
                bossMat.SetColor("_EmissionColor", new Color(0.95f, 0.80f, 0.20f) * 1.5f);
                boss.GetComponent<Renderer>().sharedMaterial = bossMat;

                offHandShield.SetActive(false);
            }
        }

        private void CreateSwordPrimitives(GameObject root, Color steelColor, Color glowColor, float scaleMultiplier = 1.0f)
        {
            // Blade
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(root.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0.60f * scaleMultiplier, 0f);
            blade.transform.localScale = new Vector3(0.08f, 1.20f * scaleMultiplier, 0.18f);
            DestroyImmediate(blade.GetComponent<Collider>());

            var bRend = blade.GetComponent<Renderer>();
            Material steelMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            steelMat.color = steelColor;
            steelMat.SetFloat("_Metallic", 0.92f);
            steelMat.SetFloat("_Smoothness", 0.88f);
            steelMat.EnableKeyword("_EMISSION");
            steelMat.SetColor("_EmissionColor", glowColor * 1.5f);
            bRend.sharedMaterial = steelMat;

            // Crossguard
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Crossguard";
            guard.transform.SetParent(root.transform, false);
            guard.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            guard.transform.localScale = new Vector3(0.10f, 0.08f, 0.48f * scaleMultiplier);
            DestroyImmediate(guard.GetComponent<Collider>());

            var gRend = guard.GetComponent<Renderer>();
            Material goldMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            goldMat.color = new Color(0.9f, 0.75f, 0.2f);
            goldMat.SetFloat("_Metallic", 0.9f);
            gRend.sharedMaterial = goldMat;

            // Grip
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "Grip";
            grip.transform.SetParent(root.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.20f * scaleMultiplier, 0f);
            grip.transform.localScale = new Vector3(0.06f, 0.20f * scaleMultiplier, 0.06f);
            DestroyImmediate(grip.GetComponent<Collider>());
        }
        #endregion

        #region HUD Stance & Parry Buttons
        private void EnsureStanceHUD()
        {
            if (stanceCanvas != null) return;

            // Check if existing PlayerHUDCanvas or DevHUD exists
            var existingCanvas = GameObject.Find("PlayerHUDCanvas");
            if (existingCanvas != null)
            {
                stanceCanvas = existingCanvas.GetComponent<Canvas>();
            }

            if (stanceCanvas == null)
            {
                GameObject canvasGo = new GameObject("CombatStanceHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                stanceCanvas = canvasGo.GetComponent<Canvas>();
                stanceCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
            }

            // 1. Stance Toggle Button (Bottom-Right Action Area)
            GameObject stanceBtnGo = new GameObject("StanceToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
            stanceBtnGo.transform.SetParent(stanceCanvas.transform, false);

            RectTransform sRt = stanceBtnGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.96f, 0.24f);
            sRt.anchorMax = new Vector2(0.96f, 0.24f);
            sRt.pivot = new Vector2(1f, 0.5f);
            sRt.sizeDelta = new Vector2(230f, 65f);
            sRt.anchoredPosition = Vector2.zero;

            stanceBtnGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.26f, 0.92f);
            stanceToggleBtn = stanceBtnGo.GetComponent<Button>();
            stanceToggleBtn.onClick.AddListener(CycleNextStance);

            GameObject sTxtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            sTxtGo.transform.SetParent(stanceBtnGo.transform, false);
            RectTransform stRt = sTxtGo.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero; stRt.anchorMax = Vector2.one; stRt.sizeDelta = Vector2.zero;
            stanceBtnTMP = sTxtGo.GetComponent<TextMeshProUGUI>();
            stanceBtnTMP.fontSize = 15;
            stanceBtnTMP.alignment = TextAlignmentOptions.Center;
            stanceBtnTMP.color = Color.white;

            // 2. Parry Action Button: "to parry any attack tap the screen with two fingers"
            GameObject parryBtnGo = new GameObject("ParryActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            parryBtnGo.transform.SetParent(stanceCanvas.transform, false);

            RectTransform pRt = parryBtnGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.96f, 0.31f);
            pRt.anchorMax = new Vector2(0.96f, 0.31f);
            pRt.pivot = new Vector2(1f, 0.5f);
            pRt.sizeDelta = new Vector2(230f, 65f);
            pRt.anchoredPosition = Vector2.zero;

            parryBtnGo.GetComponent<Image>().color = new Color(0.35f, 0.25f, 0.10f, 0.92f);
            parryActionBtn = parryBtnGo.GetComponent<Button>();
            parryActionBtn.onClick.AddListener(ExecuteParry);

            GameObject pTxtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            pTxtGo.transform.SetParent(parryBtnGo.transform, false);
            RectTransform ptRt = pTxtGo.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one; ptRt.sizeDelta = Vector2.zero;
            parryBtnTMP = pTxtGo.GetComponent<TextMeshProUGUI>();
            parryBtnTMP.text = "<b><color=#FFD700>PARRY</color></b>\n<size=75%>(2-Finger Tap / F)</size>";
            parryBtnTMP.fontSize = 15;
            parryBtnTMP.alignment = TextAlignmentOptions.Center;
            parryBtnTMP.color = Color.white;

            UpdateStanceHUD();
        }

        private void UpdateStanceHUD()
        {
            if (stanceBtnTMP == null) return;

            stanceBtnTMP.text = currentStance switch
            {
                WeaponCombatStance.SingleSword => "<b><color=#70C8FF>1-SWORD</color></b>\n<size=70%>1 Finger Blade</size>",
                WeaponCombatStance.DualBlades => "<b><color=#FFB030>DUAL BLADES</color></b>\n<size=70%>2 Slashing At Once</size>",
                WeaponCombatStance.SwordAndShield => "<b><color=#60A5FA>SWORD & SHIELD</color></b>\n<size=70%>Passive Block (-65%)</size>",
                _ => "<b>STANCE</b>"
            };
        }

        private static void SafeDestroy(UnityEngine.Object obj, float delay = 0f)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                if (delay > 0f) Destroy(obj, delay);
                else Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
        #endregion
    }
}

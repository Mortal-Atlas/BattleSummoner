using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Combat;
using BattleSummoner.UI;

namespace BattleSummoner.Combat
{
    public enum SpellSlingerPreset
    {
        Fireball,      // RigidBody + Fire (Ballistic arc, explosion, burn status)
        PoisonVial,    // RigidBody + Poison (Glass flask, shatters into toxic AOE puddle)
        RockThrow,     // RigidBody + Rock (Heavy boulder, blunt damage, massive stagger)
        Bomb,          // RigidBody + Fire (High-explosive grenade, massive blast radius)
        LightningRay,  // Ray + Lightning (Continuous raycast beam, constant mana drain)
        WaterTorrent,  // Ray + Water (Continuous NamuFX Water Beam, constant mana drain)
        WaterBall,     // RigidBody + Water (NamuFX Water Ball explosion on impact)
        ArcaneBolt,    // Normal + Normal (Straight piercing ballistic bolt)
        TrackingStar   // Tracking + Normal (Homing star curving through 3D space)
    }

    /// <summary>
    /// WizardSpellSlingerController manages active combat spell slinging in 3D:
    /// 1. 3D Finger-Following: 2D screen swipes translate into 3D space orbiting and moving around the caster.
    /// 2. Caster Mobility: The mage can freely run/move (virtual joystick / WASD) while the combat spell is held, aimed, and slung!
    /// 3. Z-Targeting Rapid Slinging: When Z-targeting is ON, the player can rapidly sling "fireball after fireball" straight at the target.
    /// 4. Projectile & Damage Types:
    ///    - RigidBody (throwables: Fireball, Poison Vial, Rock, Bomb)
    ///    - Ray (Continuous Lightning raycast with constant mana drain)
    ///    - Normal (Arcane Bolt)
    ///    - Tracking (Homing Star)
    /// </summary>
    public class WizardSpellSlingerController : MonoBehaviour
    {
        public static WizardSpellSlingerController Instance { get; private set; }

        [Header("Active Spell Configuration")]
        [SerializeField] private SpellSlingerPreset activePreset = SpellSlingerPreset.Fireball;
        [SerializeField] private SpellProjectileType projectileType = SpellProjectileType.RigidBody;
        [SerializeField] private SpellDamageType damageType = SpellDamageType.Fire;
        [SerializeField] private bool isSpellModeActive = true;

        [Header("Sling & Orbit Tuning")]
        [SerializeField] private float minOrbitRadius = 0.9f;
        [SerializeField] private float maxOrbitRadius = 3.2f;
        [SerializeField] private float casterWaistHeight = 1.25f;
        [SerializeField] private float launchBaseSpeed = 24f;
        [SerializeField] private float rapidSlingCadence = 0.12f; // Minimum cooldown for rapid-fire slinging in Z-lock

        [Header("Lightning Raycast Tuning")]
        [SerializeField] private float lightningManaDrainRate = 18f; // MP per second
        [SerializeField] private float lightningMaxDistance = 35f;
        [SerializeField] private float lightningDPS = 150f;
        [SerializeField] private float lightningTickInterval = 0.08f;

        [Header("Water Raycast Tuning (NamuFX)")]
        [SerializeField] private float waterManaDrainRate = 14f; // MP per second
        [SerializeField] private float waterDPS = 120f;
        [SerializeField] private float waterMaxDistance = 25f;

        [Header("References")]
        [SerializeField] private Transform casterTransform;
        [SerializeField] private Camera castingCamera;
        [SerializeField] private PlayerProgression playerProgression;

        // Runtime State
        private bool isHoldingSpell = false;
        private Vector2 touchStartPos = Vector2.zero;
        private Vector2 currentScreenPos = Vector2.zero;
        private float touchStartTime = 0f;
        private float chargeMultiplier = 1.0f;
        private float lastSlingTime = -10f;
        private float nextLightningTickTime = 0f;
        private float nextWaterTickTime = 0f;

        // Visual Instances
        private GameObject heldOrbObj;
        private Light heldOrbLight;
        private Material heldOrbMat;
        private TrailRenderer heldOrbTrail;
        private LineRenderer magicalTetherLine;
        private LineRenderer lightningBeamLine;
        private LineRenderer lightningBranchLine1;
        private LineRenderer lightningBranchLine2;
        private Light lightningHitLight;

        // UI Palette References
        private GameObject paletteRoot;
        private TextMeshProUGUI activeSpellBadgeTMP;

        public SpellSlingerPreset ActivePreset => activePreset;
        public SpellProjectileType ProjectileType => projectileType;
        public SpellDamageType DamageType => damageType;
        public bool IsSpellModeActive => isSpellModeActive;
        public bool IsHoldingSpell => isHoldingSpell;

        public event Action<SpellSlingerPreset> OnSpellPresetChanged;
        public event Action<float> OnSpellSlung;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            EquipPreset(activePreset, false);
            CreateTetherRenderer();
            CreateLightningRenderer();
            CreateSpellPaletteUI();
        }

        public static WizardSpellSlingerController EnsureInScene(Transform targetParent = null)
        {
            if (Instance != null) return Instance;

            var existing = FindAnyObjectByType<WizardSpellSlingerController>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var go = new GameObject("WizardSpellSlingerController");
            if (targetParent != null) go.transform.SetParent(targetParent);
            Instance = go.AddComponent<WizardSpellSlingerController>();
            return Instance;
        }

        private void ResolveReferences()
        {
            if (castingCamera == null) castingCamera = Camera.main;

            if (casterTransform == null)
            {
                var player = GameObject.Find("Player_Hero");
                if (player != null) casterTransform = player.transform;
                else
                {
                    var pm = FindAnyObjectByType<PlayerMovementController>();
                    if (pm != null) casterTransform = pm.transform;
                }
            }

            if (playerProgression == null && casterTransform != null)
            {
                playerProgression = casterTransform.GetComponent<PlayerProgression>();
            }
            if (playerProgression == null)
            {
                playerProgression = FindAnyObjectByType<PlayerProgression>();
            }
        }

        private void Update()
        {
            ResolveReferences();
            HandleKeyboardInput();

            if (!isSpellModeActive) return;

            // In Lightning mode, if finger/mouse is held down, update continuous raycast
            if (activePreset == SpellSlingerPreset.LightningRay && isHoldingSpell)
            {
                UpdateContinuousLightning();
            }
            else if (isHoldingSpell)
            {
                // Update 3D Finger Following around the moving caster!
                Update3DFingerFollowing();
            }
            else
            {
                // Idle floating hover near caster's right side
                UpdateIdleOrbHover();
            }
        }

        #region Keyboard & Shortcut Handling
        private void HandleKeyboardInput()
        {
            // Preset hotkeys (1-7)
            if (Input.GetKeyDown(KeyCode.Alpha1)) EquipPreset(SpellSlingerPreset.Fireball);
            if (Input.GetKeyDown(KeyCode.Alpha2)) EquipPreset(SpellSlingerPreset.PoisonVial);
            if (Input.GetKeyDown(KeyCode.Alpha3)) EquipPreset(SpellSlingerPreset.RockThrow);
            if (Input.GetKeyDown(KeyCode.Alpha4)) EquipPreset(SpellSlingerPreset.Bomb);
            if (Input.GetKeyDown(KeyCode.Alpha5)) EquipPreset(SpellSlingerPreset.LightningRay);
            if (Input.GetKeyDown(KeyCode.Alpha6)) EquipPreset(SpellSlingerPreset.ArcaneBolt);
            if (Input.GetKeyDown(KeyCode.Alpha7)) EquipPreset(SpellSlingerPreset.TrackingStar);

            // Toggle Spell Slinger mode (Key 'F' or Grimoire)
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleSpellMode(!isSpellModeActive);
            }

            // Desktop Mouse Drag Testing (when not blocked by UI)
            if (isSpellModeActive)
            {
                if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    OnPointerDown(Input.mousePosition);
                }
                else if (Input.GetMouseButton(0) && isHoldingSpell)
                {
                    OnDrag(Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(0) && isHoldingSpell)
                {
                    OnPointerUp(Input.mousePosition);
                }
            }
        }

        public void ToggleSpellMode(bool enable)
        {
            isSpellModeActive = enable;
            if (heldOrbObj != null) heldOrbObj.SetActive(enable);
            if (magicalTetherLine != null) magicalTetherLine.enabled = enable && isHoldingSpell;
            if (lightningBeamLine != null) lightningBeamLine.enabled = false;

            if (isSpellModeActive)
            {
                EnsureHeldOrbVisual();
                ShowToast($"<color=#FF7722><b>[SPELL SLINGER ACTIVE]</b></color> {activePreset} Ready!");
            }
            else
            {
                isHoldingSpell = false;
                ShowToast("<color=#888899>Spell Slinger Neutral</color>");
            }
        }
        #endregion

        #region Spell Preset Configuration
        public void EquipPreset(SpellSlingerPreset preset, bool showToast = true)
        {
            activePreset = preset;

            switch (preset)
            {
                case SpellSlingerPreset.Fireball:
                    projectileType = SpellProjectileType.RigidBody;
                    damageType = SpellDamageType.Fire;
                    break;
                case SpellSlingerPreset.PoisonVial:
                    projectileType = SpellProjectileType.RigidBody;
                    damageType = SpellDamageType.Poison;
                    break;
                case SpellSlingerPreset.RockThrow:
                    projectileType = SpellProjectileType.RigidBody;
                    damageType = SpellDamageType.Rock;
                    break;
                case SpellSlingerPreset.Bomb:
                    projectileType = SpellProjectileType.RigidBody;
                    damageType = SpellDamageType.Fire;
                    break;
                case SpellSlingerPreset.LightningRay:
                    projectileType = SpellProjectileType.Ray;
                    damageType = SpellDamageType.Lightning;
                    break;
                case SpellSlingerPreset.WaterTorrent:
                    projectileType = SpellProjectileType.Ray;
                    damageType = SpellDamageType.Water;
                    break;
                case SpellSlingerPreset.WaterBall:
                    projectileType = SpellProjectileType.RigidBody;
                    damageType = SpellDamageType.Water;
                    break;
                case SpellSlingerPreset.ArcaneBolt:
                    projectileType = SpellProjectileType.Normal;
                    damageType = SpellDamageType.Normal;
                    break;
                case SpellSlingerPreset.TrackingStar:
                    projectileType = SpellProjectileType.Tracking;
                    damageType = SpellDamageType.Normal;
                    break;
            }

            UpdateHeldOrbAppearance();
            UpdateUIBadge();

            if (showToast)
            {
                PlaySFX("loadout_switch", 0.8f);
                ShowToast($"<color=#{ColorUtility.ToHtmlStringRGB(GetPresetColor())}><b>[{preset.ToString().ToUpper()}]</b></color> Ready! Type: <b>{projectileType}</b> | DMG: <b>{damageType}</b>");
            }

            OnSpellPresetChanged?.Invoke(preset);
        }

        public Color GetPresetColor()
        {
            return damageType switch
            {
                SpellDamageType.Fire => new Color(1.0f, 0.45f, 0.10f),
                SpellDamageType.Poison => new Color(0.20f, 0.95f, 0.25f),
                SpellDamageType.Rock => new Color(0.70f, 0.55f, 0.35f),
                SpellDamageType.Lightning => new Color(0.25f, 0.85f, 1.0f),
                SpellDamageType.Water => new Color(0.15f, 0.65f, 1.0f),
                _ => new Color(0.65f, 0.35f, 1.0f)
            };
        }
        #endregion

        #region 2D-to-3D Finger Translation & Caster Orbiting
        /// <summary>
        /// Called when the player places their finger on the screen.
        /// </summary>
        public void OnPointerDown(Vector2 screenPos)
        {
            if (!isSpellModeActive) return;

            isHoldingSpell = true;
            touchStartPos = screenPos;
            currentScreenPos = screenPos;
            touchStartTime = Time.time;
            chargeMultiplier = 1.0f;

            if (activePreset == SpellSlingerPreset.LightningRay)
            {
                // Start continuous multi-branching lightning raycast
                if (lightningBeamLine != null) lightningBeamLine.enabled = true;
                if (lightningBranchLine1 != null) lightningBranchLine1.enabled = true;
                if (lightningBranchLine2 != null) lightningBranchLine2.enabled = true;
                if (lightningHitLight != null) lightningHitLight.enabled = true;
                PlaySFX("crystal_resonate", 0.9f);
            }
            else if (activePreset == SpellSlingerPreset.WaterTorrent)
            {
                PlaySFX("relic_chime", 0.85f);
            }
            else
            {
                EnsureHeldOrbVisual();
                // Tether line disabled per user request
                PlaySFX("relic_chime", 0.7f);
            }
        }

        /// <summary>
        /// Translates 2D screen coordinate into 3D space orbiting and moving around the caster!
        /// Works seamlessly even while the wizard is walking or running with the virtual joystick!
        /// </summary>
        public void OnDrag(Vector2 screenPos)
        {
            if (!isSpellModeActive || !isHoldingSpell) return;
            currentScreenPos = screenPos;
        }

        private void Update3DFingerFollowing()
        {
            if (casterTransform == null || castingCamera == null) return;

            EnsureHeldOrbVisual();

            // 1. Calculate charge multiplier based on hold time (1.00x up to 1.50x)
            float elapsed = Time.time - touchStartTime;
            chargeMultiplier = Mathf.Clamp(1.0f + elapsed * 0.45f, 1.0f, 1.50f);

            // 2. Cast Ray from 2D Screen Position into 3D World
            Ray ray = castingCamera.ScreenPointToRay(currentScreenPos);

            // 3. Define horizontal casting plane at caster waist height
            float planeY = casterTransform.position.y + casterWaistHeight;
            Plane horizontalPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

            Vector3 targetOrbPosition = casterTransform.position + Vector3.up * casterWaistHeight + casterTransform.forward * 1.5f;

            if (horizontalPlane.Raycast(ray, out float enterDist))
            {
                Vector3 worldHit = ray.GetPoint(enterDist);
                Vector3 offsetFromCaster = worldHit - (casterTransform.position + Vector3.up * casterWaistHeight);
                offsetFromCaster.y = 0f;

                float dist = offsetFromCaster.magnitude;
                float clampedDist = Mathf.Clamp(dist, minOrbitRadius, maxOrbitRadius);
                Vector3 orbitDir = dist > 0.001f ? offsetFromCaster / dist : casterTransform.forward;
                Vector3 clampedHorizontalOffset = orbitDir * clampedDist;

                // Screen Y tilt for intuitive elevation control
                float verticalScreenRatio = (currentScreenPos.y / Screen.height) - 0.5f;
                float verticalElevation = Mathf.Clamp(verticalScreenRatio * 1.6f, -0.5f, 0.85f);

                // Anchored dynamically to the caster's real-time position so caster mobility is 100% preserved!
                targetOrbPosition = casterTransform.position + Vector3.up * (casterWaistHeight + verticalElevation) + clampedHorizontalOffset;
            }

            // Smoothly lerp held orb toward target position in 3D space
            heldOrbObj.transform.position = Vector3.Lerp(heldOrbObj.transform.position, targetOrbPosition, Time.deltaTime * 24f);

            // Pulse orb scale with charge
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 10f);
            heldOrbObj.transform.localScale = Vector3.one * (0.4f * chargeMultiplier * pulse);
        }

        private void UpdateIdleOrbHover()
        {
            if (casterTransform == null || heldOrbObj == null || activePreset == SpellSlingerPreset.LightningRay) return;

            // Hover softly near the caster's right shoulder/side
            Vector3 rightShoulder = casterTransform.position + Vector3.up * (casterWaistHeight + 0.1f) + casterTransform.right * 0.95f + casterTransform.forward * 0.45f;
            float bobbing = Mathf.Sin(Time.time * 3.5f) * 0.08f;
            Vector3 idlePos = rightShoulder + Vector3.up * bobbing;

            heldOrbObj.transform.position = Vector3.Lerp(heldOrbObj.transform.position, idlePos, Time.deltaTime * 6f);
            heldOrbObj.transform.localScale = Vector3.one * 0.35f;
        }

        private void UpdateTetherLine(Vector3 orbPos)
        {
            // Intentionally disabled per user request: No tether line from character model to the spell orb
        }
        #endregion

        #region Sling Gesture Launch & Z-Targeting Rapid Fire
        /// <summary>
        /// Called when the player releases their finger (or executes a sling flick).
        /// </summary>
        public void OnPointerUp(Vector2 releasePos)
        {
            if (!isSpellModeActive || !isHoldingSpell) return;

            if (activePreset == SpellSlingerPreset.LightningRay)
            {
                StopLightningBeam();
                isHoldingSpell = false;
                return;
            }

            isHoldingSpell = false;
            if (magicalTetherLine != null) magicalTetherLine.enabled = false;

            // Fling calculation
            Vector2 dragVector = releasePos - touchStartPos;
            float dragDistance = dragVector.magnitude;
            float holdDuration = Mathf.Max(0.01f, Time.time - touchStartTime);

            // Check if Z-Targeting is active
            bool isZLocked = QuickZTargetingController.Instance != null && QuickZTargetingController.Instance.IsLockedOn;
            Transform lockedTarget = isZLocked ? QuickZTargetingController.Instance.CurrentTarget : null;

            if (isZLocked && lockedTarget != null)
            {
                // Rapid-fire cadence check
                if (Time.time - lastSlingTime < rapidSlingCadence) return;
                lastSlingTime = Time.time;

                // Launch directly at locked target!
                SlingAtLockedTarget(lockedTarget, chargeMultiplier);

                // Immediately prime/ready next fireball for continuous rapid slinging ("fireball after fireball")!
                ReadyNextSpellOrb();
            }
            else
            {
                // Free-aim fling launch
                SlingFreeAim(releasePos, dragVector, dragDistance, chargeMultiplier);
                ReadyNextSpellOrb();
            }

            OnSpellSlung?.Invoke(chargeMultiplier);
        }

        private void SlingAtLockedTarget(Transform target, float charge)
        {
            Vector3 spawnPos = heldOrbObj != null ? heldOrbObj.transform.position : casterTransform.position + Vector3.up * 1.4f;
            Vector3 targetCenter = target.position + Vector3.up * 1.0f;
            Vector3 displacement = targetCenter - spawnPos;
            float distance = displacement.magnitude;

            Vector3 launchVelocity;

            if (projectileType == SpellProjectileType.RigidBody)
            {
                // Parabolic ballistic arc calculation to hit locked enemy accurately with gravity!
                float flightTime = Mathf.Clamp(distance / launchBaseSpeed, 0.28f, 1.2f);
                float grav = Mathf.Abs(Physics.gravity.y);

                float vx = displacement.x / flightTime;
                float vz = displacement.z / flightTime;
                float vy = (displacement.y + 0.5f * grav * flightTime * flightTime) / flightTime;

                launchVelocity = new Vector3(vx, vy, vz);
            }
            else
            {
                // Direct straight/homing velocity
                launchVelocity = displacement.normalized * (launchBaseSpeed * 1.25f);
            }

            SpawnAndFireProjectile(spawnPos, launchVelocity, charge, target);
            PlaySFX("spell_fireball_launch", 0.95f);

            Debug.Log($"🔥 [Z-TARGET SLUNG] Slung {activePreset} ({projectileType}) at {target.name} (Dist: {distance:F1}m, Charge: {charge:F2}x). Next spell ready for rapid fire!");
        }

        private void SlingFreeAim(Vector2 releasePos, Vector2 dragVector, float dragDist, float charge)
        {
            Vector3 spawnPos = heldOrbObj != null ? heldOrbObj.transform.position : casterTransform.position + Vector3.up * 1.4f;
            Vector3 slingDir = casterTransform != null ? casterTransform.forward : Vector3.forward;

            if (castingCamera != null)
            {
                Ray ray = castingCamera.ScreenPointToRay(releasePos);
                slingDir = ray.direction;

                if (dragDist > 35f)
                {
                    Vector3 camRight = castingCamera.transform.right;
                    Vector3 camFwd = castingCamera.transform.forward;
                    camRight.y = 0f; camRight.Normalize();
                    camFwd.y = 0f; camFwd.Normalize();

                    Vector3 dragWorld = (camRight * dragVector.x + camFwd * dragVector.y).normalized;
                    if (dragWorld.sqrMagnitude > 0.01f)
                    {
                        slingDir = Vector3.Lerp(ray.direction, dragWorld, 0.65f).normalized;
                    }
                }
            }

            slingDir.y = Mathf.Clamp(slingDir.y, -0.2f, 0.45f);
            slingDir.Normalize();

            float speed = launchBaseSpeed * (1.0f + (charge - 1.0f) * 0.35f);
            Vector3 launchVelocity = slingDir * speed + Vector3.up * 2.5f; // Slight upward arc for throwables

            SpawnAndFireProjectile(spawnPos, launchVelocity, charge, null);
            PlaySFX("spell_fireball_launch", 0.9f);

            Debug.Log($"✨ [FREE SLING] Slung {activePreset} ({projectileType}) Dir: {slingDir}, Charge: {charge:F2}x.");
        }

        private void SpawnAndFireProjectile(Vector3 spawnPos, Vector3 velocity, float charge, Transform target)
        {
            GameObject projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projObj.name = $"{activePreset}_Projectile";
            projObj.transform.position = spawnPos;

            var projComp = projObj.AddComponent<ModularCombatSpellProjectile>();
            projComp.Initialize(projectileType, damageType, velocity, charge, 100f, 4.5f, target);

            // Hide held orb briefly during launch recoil
            if (heldOrbObj != null)
            {
                heldOrbObj.SetActive(false);
            }
        }

        private void ReadyNextSpellOrb()
        {
            // Instantly re-enable and re-prime held orb so caster can rapid-sling consecutively!
            StartCoroutine(RoutineReadyNextOrb());
        }

        private IEnumerator RoutineReadyNextOrb()
        {
            yield return new WaitForSeconds(0.06f);
            EnsureHeldOrbVisual();
            if (heldOrbObj != null) heldOrbObj.SetActive(true);
        }
        #endregion

        #region Continuous Lightning Raycast with Mana Drain
        private void UpdateContinuousLightning()
        {
            if (casterTransform == null || castingCamera == null) return;

            // 1. Mana Drain Check: Drain mana constantly while holding lightning!
            float manaToDrain = lightningManaDrainRate * Time.deltaTime;
            if (playerProgression != null)
            {
                if (!playerProgression.DrainMana(manaToDrain))
                {
                    // Out of mana!
                    StopLightningBeam();
                    PlaySFX("glitch", 0.8f);
                    ShowToast("<color=#FF3333><b>[OUT OF MANA]</b></color> Lightning Stream Exhausted!");
                    isHoldingSpell = false;
                    return;
                }
            }

            // 2. Beam Origin
            Vector3 origin = casterTransform.position + Vector3.up * casterWaistHeight + casterTransform.forward * 0.5f;

            // 3. Beam Direction: Lock-on priority or 3D screen ray
            bool isZLocked = QuickZTargetingController.Instance != null && QuickZTargetingController.Instance.IsLockedOn;
            Transform lockedTarget = isZLocked ? QuickZTargetingController.Instance.CurrentTarget : null;

            Vector3 rayDir = casterTransform.forward;
            if (lockedTarget != null)
            {
                rayDir = (lockedTarget.position + Vector3.up * 1.0f - origin).normalized;
            }
            else
            {
                Ray screenRay = castingCamera.ScreenPointToRay(currentScreenPos);
                rayDir = screenRay.direction;
            }

            // 4. Raycast in 3D Scene
            bool hitObstacle = Physics.Raycast(origin, rayDir, out RaycastHit hit, lightningMaxDistance, ~0, QueryTriggerInteraction.Ignore);
            Vector3 endPoint = hitObstacle ? hit.point : origin + rayDir * lightningMaxDistance;

            // 5. Procedural Multi-Branching Lightning Jitter
            RenderProceduralLightningBeam(origin, endPoint);

            // 6. Hit Point Light & Impact SFX
            if (lightningHitLight != null)
            {
                lightningHitLight.enabled = true;
                lightningHitLight.transform.position = endPoint;
                lightningHitLight.intensity = UnityEngine.Random.Range(4.5f, 7.5f); // Electric flicker
            }

            // 7. Periodic Continuous Damage Ticks
            if (Time.time >= nextLightningTickTime)
            {
                nextLightningTickTime = Time.time + lightningTickInterval;
                float tickDamage = (lightningDPS * lightningTickInterval);

                if (hitObstacle && !hit.collider.CompareTag("Player"))
                {
                    ApplyLightningTickDamage(hit.collider, endPoint, tickDamage);
                }
            }
        }

        private void RenderProceduralLightningBeam(Vector3 start, Vector3 end)
        {
            if (lightningBeamLine == null) return;

            int segments = 12;
            lightningBeamLine.positionCount = segments + 1;
            lightningBeamLine.SetPosition(0, start);
            lightningBeamLine.SetPosition(segments, end);

            Vector3 step = (end - start) / segments;
            for (int i = 1; i < segments; i++)
            {
                Vector3 basePt = start + step * i;
                Vector3 jitter = UnityEngine.Random.insideUnitSphere * 0.18f;
                lightningBeamLine.SetPosition(i, basePt + jitter);
            }

            // Branch 1: Dancing secondary arc curving outward
            if (lightningBranchLine1 != null && lightningBranchLine1.enabled)
            {
                int bSegments = 8;
                lightningBranchLine1.positionCount = bSegments + 1;
                lightningBranchLine1.SetPosition(0, start);
                lightningBranchLine1.SetPosition(bSegments, end);

                Vector3 bStep = (end - start) / bSegments;
                Vector3 perpendicular = Vector3.Cross((end - start).normalized, Vector3.up).normalized;
                float waveOffset = Mathf.Sin(Time.time * 25f) * 0.35f;

                for (int i = 1; i < bSegments; i++)
                {
                    Vector3 basePt = start + bStep * i + perpendicular * waveOffset;
                    Vector3 jitter = UnityEngine.Random.insideUnitSphere * 0.22f;
                    lightningBranchLine1.SetPosition(i, basePt + jitter);
                }
            }

            // Branch 2: Erratic grounding fork off midway
            if (lightningBranchLine2 != null && lightningBranchLine2.enabled)
            {
                int fSegments = 5;
                lightningBranchLine2.positionCount = fSegments + 1;
                Vector3 forkStart = start + step * 4;
                Vector3 forkEnd = forkStart + (UnityEngine.Random.insideUnitSphere * 2.2f + Vector3.down * 1.5f);
                lightningBranchLine2.SetPosition(0, forkStart);
                lightningBranchLine2.SetPosition(fSegments, forkEnd);

                Vector3 fStep = (forkEnd - forkStart) / fSegments;
                for (int i = 1; i < fSegments; i++)
                {
                    Vector3 basePt = forkStart + fStep * i + UnityEngine.Random.insideUnitSphere * 0.15f;
                    lightningBranchLine2.SetPosition(i, basePt);
                }
            }
        }

        private void ApplyLightningTickDamage(Collider col, Vector3 point, float damage)
        {
            if (col.TryGetComponent<StatusEffectReceiver>(out var statusRec))
            {
                statusRec.TakeAdvancedDamage(new DamagePayload
                {
                    baseDamage = damage,
                    damageType = BattleSummoner.Combat.DamageType.Electric,
                    critChance = 0.25f,
                    critMultiplier = 1.6f,
                    accuracy = 1.0f,
                    hitPoint = point
                });
                statusRec.ApplyStatus(StatusEffectType.Shocked, 2.5f, 0.8f);
            }
            else if (col.TryGetComponent<BossController>(out var boss))
            {
                boss.TakeDamage(damage);
                boss.Stagger(0.2f, point);
            }
            else if (col.TryGetComponent<SpireEnemy>(out var enemy))
            {
                enemy.TakeDamage(damage);
                enemy.Stagger(0.25f, point);
            }

            // Spawn NamuFX electric sparks on impact point
            if (NamuSpellVFXManager.Instance != null)
            {
                NamuSpellVFXManager.Instance.SpawnHitVFX(point, Vector3.up, false);
            }

            // Pop subtle electric damage tick number
            if (CombatFeedbackManager.Instance != null && UnityEngine.Random.value < 0.40f)
            {
                CombatFeedbackManager.Instance.SpawnDamagePopup(
                    point + Vector3.up * 0.6f,
                    new DamageResult
                    {
                        finalDamage = damage * 3f,
                        damageType = BattleSummoner.Combat.DamageType.Electric,
                        outcome = HitOutcome.Normal
                    });
            }
        }

        private void StopLightningBeam()
        {
            if (lightningBeamLine != null) lightningBeamLine.enabled = false;
            if (lightningBranchLine1 != null) lightningBranchLine1.enabled = false;
            if (lightningBranchLine2 != null) lightningBranchLine2.enabled = false;
            if (lightningHitLight != null) lightningHitLight.enabled = false;
        }

        private void UpdateContinuousWaterTorrent()
        {
            if (casterTransform == null || castingCamera == null) return;

            float manaToDrain = waterManaDrainRate * Time.deltaTime;
            if (playerProgression != null)
            {
                if (!playerProgression.DrainMana(manaToDrain))
                {
                    StopWaterBeam();
                    PlaySFX("glitch", 0.8f);
                    ShowToast("<color=#FF3333><b>[OUT OF MANA]</b></color> Water Torrent Exhausted!");
                    isHoldingSpell = false;
                    return;
                }
            }

            Vector3 origin = casterTransform.position + Vector3.up * casterWaistHeight + casterTransform.forward * 0.5f;

            bool isZLocked = QuickZTargetingController.Instance != null && QuickZTargetingController.Instance.IsLockedOn;
            Transform lockedTarget = isZLocked ? QuickZTargetingController.Instance.CurrentTarget : null;

            Vector3 rayDir = casterTransform.forward;
            if (lockedTarget != null) rayDir = (lockedTarget.position + Vector3.up * 1.0f - origin).normalized;
            else if (castingCamera != null) rayDir = castingCamera.ScreenPointToRay(currentScreenPos).direction;

            bool hitObstacle = Physics.Raycast(origin, rayDir, out RaycastHit hit, waterMaxDistance, ~0, QueryTriggerInteraction.Ignore);
            Vector3 endPoint = hitObstacle ? hit.point : origin + rayDir * waterMaxDistance;

            if (NamuSpellVFXManager.Instance != null)
            {
                NamuSpellVFXManager.Instance.UpdateContinuousWaterBeam(origin, endPoint);
            }

            if (Time.time >= nextWaterTickTime)
            {
                nextWaterTickTime = Time.time + 0.1f;
                float tickDamage = waterDPS * 0.1f;
                if (hitObstacle && !hit.collider.CompareTag("Player"))
                {
                    ApplyWaterTickDamage(hit.collider, endPoint, tickDamage);
                }
            }
        }

        private void ApplyWaterTickDamage(Collider col, Vector3 point, float damage)
        {
            if (col.TryGetComponent<StatusEffectReceiver>(out var statusRec))
            {
                statusRec.TakeAdvancedDamage(new DamagePayload
                {
                    baseDamage = damage,
                    damageType = BattleSummoner.Combat.DamageType.Ice,
                    critChance = 0.15f,
                    critMultiplier = 1.4f,
                    accuracy = 1.0f,
                    hitPoint = point
                });
                statusRec.ApplyStatus(StatusEffectType.Slowed, 3.5f, 0.6f);
            }
            else if (col.TryGetComponent<BossController>(out var boss))
            {
                boss.TakeDamage(damage);
            }
            else if (col.TryGetComponent<SpireEnemy>(out var enemy))
            {
                enemy.TakeDamage(damage);
            }

            if (NamuSpellVFXManager.Instance != null)
            {
                NamuSpellVFXManager.Instance.SpawnWaterImpact(point);
            }

            if (CombatFeedbackManager.Instance != null && UnityEngine.Random.value < 0.35f)
            {
                CombatFeedbackManager.Instance.SpawnDamagePopup(
                    point + Vector3.up * 0.6f,
                    new DamageResult
                    {
                        finalDamage = damage * 2.5f,
                        damageType = BattleSummoner.Combat.DamageType.Ice,
                        outcome = HitOutcome.Normal
                    });
            }
        }

        private void StopWaterBeam()
        {
            if (NamuSpellVFXManager.Instance != null)
            {
                NamuSpellVFXManager.Instance.StopContinuousWaterBeam();
            }
        }
        #endregion

        #region Visual Instances Setup
        private void EnsureHeldOrbVisual()
        {
            if (heldOrbObj != null) return;

            heldOrbObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            heldOrbObj.name = "WizardHeldSpellOrb";
            var col = heldOrbObj.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);

            var rend = heldOrbObj.GetComponent<Renderer>();
            if (rend != null)
            {
                heldOrbMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rend.sharedMaterial = heldOrbMat;
            }

            var lGo = new GameObject("HeldOrbPointLight", typeof(Light));
            lGo.transform.SetParent(heldOrbObj.transform, false);
            heldOrbLight = lGo.GetComponent<Light>();
            heldOrbLight.type = LightType.Point;
            heldOrbLight.range = 6.5f;

            // Subtle magical trail hovering behind the orb
            heldOrbTrail = heldOrbObj.AddComponent<TrailRenderer>();
            heldOrbTrail.time = 0.35f;
            heldOrbTrail.startWidth = 0.20f;
            heldOrbTrail.endWidth = 0.0f;
            heldOrbTrail.minVertexDistance = 0.04f;
            heldOrbTrail.autodestruct = false;

            UpdateHeldOrbAppearance();
        }

        private void UpdateHeldOrbAppearance()
        {
            if (heldOrbMat == null) return;

            Color c = GetPresetColor();
            heldOrbMat.color = c;
            heldOrbMat.EnableKeyword("_EMISSION");
            heldOrbMat.SetColor("_EmissionColor", c * 5.5f);

            if (heldOrbLight != null)
            {
                heldOrbLight.color = c;
                heldOrbLight.intensity = 3.5f;
            }

            if (heldOrbTrail != null)
            {
                Material trailMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                trailMat.color = new Color(c.r, c.g, c.b, 0.75f);
                trailMat.EnableKeyword("_EMISSION");
                trailMat.SetColor("_EmissionColor", c * 4.5f);
                heldOrbTrail.sharedMaterial = trailMat;
                heldOrbTrail.startColor = new Color(c.r, c.g, c.b, 0.85f);
                heldOrbTrail.endColor = new Color(c.r, c.g, c.b, 0.0f);
            }
        }

        private void CreateTetherRenderer()
        {
            // User requested NO line from character model to the spell orb.
            if (magicalTetherLine != null)
            {
                SafeDestroy(magicalTetherLine.gameObject);
                magicalTetherLine = null;
            }
        }

        private void CreateLightningRenderer()
        {
            if (lightningBeamLine != null) return;

            var lGo = new GameObject("ContinuousLightningBeam", typeof(LineRenderer));
            lGo.transform.SetParent(transform, false);
            lightningBeamLine = lGo.GetComponent<LineRenderer>();
            lightningBeamLine.positionCount = 13;
            lightningBeamLine.startWidth = 0.12f;
            lightningBeamLine.endWidth = 0.04f;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.3f, 0.85f, 1.0f, 0.95f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.4f, 0.9f, 1.0f) * 8f);
            lightningBeamLine.material = mat;
            lightningBeamLine.enabled = false;

            // Secondary dancing branch 1
            var b1Go = new GameObject("ContinuousLightningBranch1", typeof(LineRenderer));
            b1Go.transform.SetParent(transform, false);
            lightningBranchLine1 = b1Go.GetComponent<LineRenderer>();
            lightningBranchLine1.positionCount = 9;
            lightningBranchLine1.startWidth = 0.06f;
            lightningBranchLine1.endWidth = 0.02f;
            lightningBranchLine1.material = mat;
            lightningBranchLine1.enabled = false;

            // Secondary grounding fork branch 2
            var b2Go = new GameObject("ContinuousLightningBranch2", typeof(LineRenderer));
            b2Go.transform.SetParent(transform, false);
            lightningBranchLine2 = b2Go.GetComponent<LineRenderer>();
            lightningBranchLine2.positionCount = 6;
            lightningBranchLine2.startWidth = 0.05f;
            lightningBranchLine2.endWidth = 0.015f;
            lightningBranchLine2.material = mat;
            lightningBranchLine2.enabled = false;

            var hitLGo = new GameObject("LightningHitLight", typeof(Light));
            hitLGo.transform.SetParent(lGo.transform, false);
            lightningHitLight = hitLGo.GetComponent<Light>();
            lightningHitLight.type = LightType.Point;
            lightningHitLight.color = new Color(0.3f, 0.9f, 1.0f);
            lightningHitLight.range = 8f;
            lightningHitLight.intensity = 5f;
            lightningHitLight.enabled = false;
        }
        #endregion

        #region HUD Palette & Feedback
        private void CreateSpellPaletteUI()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("SpellSlingerPalette");
            if (existing != null)
            {
                paletteRoot = existing.gameObject;
                activeSpellBadgeTMP = paletteRoot.GetComponentInChildren<TextMeshProUGUI>();
                return;
            }

            paletteRoot = new GameObject("SpellSlingerPalette", typeof(RectTransform), typeof(Image));
            paletteRoot.transform.SetParent(canvas.transform, false);

            var rt = paletteRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.02f);
            rt.anchorMax = new Vector2(0.5f, 0.02f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 75f);
            rt.sizeDelta = new Vector2(480f, 52f);

            var bg = paletteRoot.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.12f, 0.88f);

            // Active Spell Badge Label
            var badgeGo = new GameObject("ActiveSpellBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeGo.transform.SetParent(paletteRoot.transform, false);
            var bRT = badgeGo.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0f, 0.55f);
            bRT.anchorMax = new Vector2(1f, 1f);
            bRT.offsetMin = Vector2.zero;
            bRT.offsetMax = Vector2.zero;

            activeSpellBadgeTMP = badgeGo.GetComponent<TextMeshProUGUI>();
            activeSpellBadgeTMP.fontSize = 13;
            activeSpellBadgeTMP.alignment = TextAlignmentOptions.Center;
            activeSpellBadgeTMP.color = Color.white;
            activeSpellBadgeTMP.raycastTarget = false;

            // Spell Buttons Row
            var rowGo = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(paletteRoot.transform, false);
            var rowRT = rowGo.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 0f);
            rowRT.anchorMax = new Vector2(1f, 0.55f);
            rowRT.offsetMin = new Vector2(6f, 2f);
            rowRT.offsetMax = new Vector2(-6f, -2f);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Create 7 spell buttons
            CreatePaletteButton(rowGo.transform, "1.Fire", SpellSlingerPreset.Fireball, new Color(1f, 0.4f, 0.1f));
            CreatePaletteButton(rowGo.transform, "2.Poison", SpellSlingerPreset.PoisonVial, new Color(0.2f, 0.9f, 0.2f));
            CreatePaletteButton(rowGo.transform, "3.Rock", SpellSlingerPreset.RockThrow, new Color(0.7f, 0.55f, 0.35f));
            CreatePaletteButton(rowGo.transform, "4.Bomb", SpellSlingerPreset.Bomb, new Color(1f, 0.25f, 0.15f));
            CreatePaletteButton(rowGo.transform, "5.Lightn", SpellSlingerPreset.LightningRay, new Color(0.25f, 0.85f, 1f));
            CreatePaletteButton(rowGo.transform, "6.Arcane", SpellSlingerPreset.ArcaneBolt, new Color(0.7f, 0.4f, 1f));
            CreatePaletteButton(rowGo.transform, "7.Track", SpellSlingerPreset.TrackingStar, new Color(1f, 0.85f, 0.2f));

            UpdateUIBadge();
        }

        private void CreatePaletteButton(Transform parent, string label, SpellSlingerPreset preset, Color tint)
        {
            var btnGo = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(tint.r * 0.25f, tint.g * 0.25f, tint.b * 0.25f, 0.9f);

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() => EquipPreset(preset));

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            var tRT = textGo.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = tint;
            tmp.raycastTarget = false;
        }

        private void UpdateUIBadge()
        {
            if (activeSpellBadgeTMP == null) return;
            string hex = ColorUtility.ToHtmlStringRGB(GetPresetColor());
            activeSpellBadgeTMP.text = $"<color=#{hex}><b>⚡ {activePreset.ToString().ToUpper()}</b></color> | Proj: <b>{projectileType}</b> | DMG: <b>{damageType}</b>";
        }

        private void ShowToast(string message)
        {
            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(message);
            }
            else if (OverworldPlayerHUDController.Instance != null)
            {
                OverworldPlayerHUDController.Instance.ShowToast(message);
            }
        }

        private void PlaySFX(string presetName, float volume = 1f)
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX(presetName, volume);
            }
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
        #endregion
    }
}

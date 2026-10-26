using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// ISliceable interface for entities and destructibles susceptible to gesture blade slashes.
    /// </summary>
    public interface ISliceable
    {
        void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage);
    }

    /// <summary>
    /// FruitNinjaBladeCutter pipes high-speed swipe velocity slashes through the
    /// UncappedAccessorySystem stack aggregation matrix to evaluate aggregate kinetic strike damage,
    /// triggering hit-stop impulses and cutting ISliceable targets.
    /// </summary>
    public class FruitNinjaBladeCutter : MonoBehaviour
    {
        private static FruitNinjaBladeCutter _instance;
        public static FruitNinjaBladeCutter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<FruitNinjaBladeCutter>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var go = new GameObject("FruitNinjaBladeCutter");
                        _instance = go.AddComponent<FruitNinjaBladeCutter>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Kinetic Slash Tuning")]
        [SerializeField] private float baseSlashPower = 35f;
        [SerializeField] private float velocityScalingFactor = 0.08f;
        [SerializeField] private float hitStopDurationSec = 0.045f;
        [SerializeField] private LayerMask sliceableLayers = ~0;

        [Header("Audio & Feedback")]
        [SerializeField] private string hitSFXPreset = "blade_hit";

        public event Action<Vector3, float> OnSliceExecuted;
        public event Action OnTwoFingerParryExecuted;
        public event Action<Vector2, float> OnBladeCutPerformed;

        private bool inHitStop = false;
        private static readonly RaycastHit[] _sphereCastBuffer = new RaycastHit[32];
        private static readonly Collider[] _viewCollidersBuffer = new Collider[64];

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnEnable()
        {
            PortraitGestureCanvas.OnBladeSlash += HandleBladeSlash;
            PortraitGestureCanvas.OnScreenBladeSlash += HandleScreenBladeSlash;
        }

        private void OnDisable()
        {
            PortraitGestureCanvas.OnBladeSlash -= HandleBladeSlash;
            PortraitGestureCanvas.OnScreenBladeSlash -= HandleScreenBladeSlash;
        }

        /// <summary>
        /// Fruit Ninja Screen-Space Blade Slash:
        /// Raycasts and sweeps through all objects on screen along the swipe path.
        /// Faster swings and more precise (center-passing) cuts deal significantly higher damage.
        /// </summary>
        public void HandleScreenBladeSlash(Vector2 startScreenPos, Vector2 endScreenPos, float velocity)
        {
            // Blade 1: Main Hand Sword
            Color blade1Color = new Color(0.25f, 0.85f, 1f); // Radiant Electric Cyan
            ExecuteBladeStrike(startScreenPos, endScreenPos, velocity, blade1Color, false);

            // If Dual Blades / Daggers equipped: "if they have two swords or daggers then you can give two different sword slashing at the same time."
            if (PlayerCombatStanceController.Instance != null && PlayerCombatStanceController.Instance.IsDualWielding)
            {
                // Blade 2: Off-Hand Sword / Dagger: Offset parallel/crossing stroke with fiery golden amber wave
                Vector2 delta = endScreenPos - startScreenPos;
                Vector2 perp = new Vector2(-delta.y, delta.x).normalized * 26f;
                Color blade2Color = new Color(1.0f, 0.75f, 0.20f); // Fiery Golden Amber
                ExecuteBladeStrike(startScreenPos + perp, endScreenPos + perp, velocity * 0.95f, blade2Color, true);
            }
        }

        public void ExecuteBladeStrike(Vector2 startScreenPos, Vector2 endScreenPos, float velocity, Color waveColor, bool isOffHand)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 swipeDelta = endScreenPos - startScreenPos;
            float swipeLen = swipeDelta.magnitude;
            if (swipeLen < 6f) return;

            Vector2 swipeDir = swipeDelta.normalized;
            float swipeAngleDeg = Mathf.Atan2(swipeDelta.y, swipeDelta.x) * Mathf.Rad2Deg;

            OnBladeCutPerformed?.Invoke(swipeDir, velocity);

            Vector3 camRight = cam.transform.right;
            Vector3 camUp = cam.transform.up;
            Vector3 worldSlashDir = (camRight * swipeDir.x + camUp * swipeDir.y).normalized;

            // 1. Faster swings do more damage: 1.0x at 400 px/s up to 2.8x at 2200 px/s
            float effectiveVel = velocity;
            if (BindingContractSystem.Instance != null && BindingContractSystem.Instance.signedContracts.Exists(c => c.favoredWeaponType == WeaponSlotType.OneHanded))
            {
                effectiveVel *= 1.25f; // Faster swipe charge-up via Singular Grip Contract
            }
            float speedNorm = Mathf.Clamp01((effectiveVel - 400f) / 1800f);
            float speedMultiplier = 1.0f + (speedNorm * 1.8f);

            // 2. Identify all targets on screen via direct raycast sampling and screen-space projection
            var targetPrecision = new Dictionary<Collider, float>();
            var targetHitPoint = new Dictionary<Collider, Vector3>();

            // Direct raycast sampling along the blade stroke (Zero-Alloc NonAlloc)
            int sampleCount = Mathf.Clamp(Mathf.RoundToInt(swipeLen / 24f), 5, 25);
            for (int i = 0; i <= sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                Vector2 sPt = Vector2.Lerp(startScreenPos, endScreenPos, t);
                Ray ray = cam.ScreenPointToRay(sPt);
                int hitCount = Physics.SphereCastNonAlloc(ray, 0.45f, _sphereCastBuffer, 40f, sliceableLayers, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hitCount; h++)
                {
                    Collider c = _sphereCastBuffer[h].collider;
                    if (c == null) continue;
                    if (c.CompareTag("Player") || c.transform.root.CompareTag("Player") || c.name.Contains("Player") || c.transform.root.name.Contains("Player") || c.transform.IsChildOf(cam.transform)) continue;
                    if (!targetPrecision.ContainsKey(c))
                    {
                        targetPrecision[c] = 0.72f;
                        targetHitPoint[c] = _sphereCastBuffer[h].point;
                    }
                }
            }

            // Screen-Space Projection Cut for visible enemies in camera view (Zero-Alloc NonAlloc)
            int viewCount = Physics.OverlapSphereNonAlloc(cam.transform.position + cam.transform.forward * 8f, 28f, _viewCollidersBuffer, sliceableLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < viewCount; i++)
            {
                Collider col = _viewCollidersBuffer[i];
                if (col == null) continue;
                if (col.CompareTag("Player") || col.transform.root.CompareTag("Player") || col.name.Contains("Player") || col.transform.root.name.Contains("Player") || col.transform.IsChildOf(cam.transform)) continue;

                Vector3 worldCenter = col.bounds.center;
                Vector3 screenProj = cam.WorldToScreenPoint(worldCenter);
                if (screenProj.z <= 0.4f || screenProj.z > 35f) continue; // Behind camera or out of range

                Vector2 targetScreenPt = new Vector2(screenProj.x, screenProj.y);
                float distToSegment = DistancePointToSegment(targetScreenPt, startScreenPos, endScreenPos);

                float maxExtent = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y, col.bounds.extents.z);
                float screenRadius = (maxExtent / screenProj.z) * (Screen.height * 0.45f);
                screenRadius = Mathf.Clamp(screenRadius, 32f, 160f);

                if (distToSegment <= screenRadius)
                {
                    // Precision metric: 1.0 = dead-center slice, down to 0 at edge
                    float prec = 1.0f - Mathf.Clamp01(distToSegment / screenRadius);
                    if (!targetPrecision.ContainsKey(col) || prec > targetPrecision[col])
                    {
                        targetPrecision[col] = prec;
                        targetHitPoint[col] = col.ClosestPoint(cam.transform.position + cam.transform.forward * screenProj.z);
                    }
                }
            }

            // 3. 12D Telemetry: Hook 4D Inertial Mass & 5D Density Matrix
            float rho_5D = 1.0f;
            float mass_4D = 10.0f;
            if (RuneShapeCastingEngine.Instance != null && RuneShapeCastingEngine.Instance.ActiveSpellArtifact != null)
            {
                rho_5D = RuneShapeCastingEngine.Instance.ActiveSpellArtifact.densityMatrix_5D;
                mass_4D = RuneShapeCastingEngine.Instance.ActiveSpellArtifact.inertialMass_4D;
            }
            float armorPenetration = velocity * rho_5D * 0.25f;
            float postureDamage = (mass_4D * 1.5f) + (velocity * 0.25f);

            bool struckAny = false;
            float maxPrecisionHit = 0f;

            // Check if player has Riposte Critical Boost from a successful Two-Finger Parry!
            float riposteMult = 1.0f;
            if (!isOffHand && PlayerCombatStanceController.Instance != null && PlayerCombatStanceController.Instance.ConsumeRiposteBoost(out float boost))
            {
                riposteMult = boost; // 3.0x Riposte Damage!
            }

            foreach (var kvp in targetPrecision)
            {
                Collider col = kvp.Key;
                float precision = kvp.Value;
                Vector3 contactPoint = targetHitPoint.ContainsKey(col) ? targetHitPoint[col] : col.bounds.center;

                float precisionMultiplier = precision >= 0.82f ? 2.0f : (precision >= 0.48f ? 1.4f : 1.0f);

                // Weak-Spot Seam Angle and Elemental Weakness Evaluation
                if (CombatMasteryAndEvolutionSystem.Instance != null)
                {
                    string speciesId = col.name.Replace("(Clone)", "").Trim();
                    Vector2 monsterSeam = new Vector2(col.transform.up.x, col.transform.up.y);
                    if (monsterSeam.sqrMagnitude < 0.01f) monsterSeam = Vector2.up;

                    float weakSpotMult = CombatMasteryAndEvolutionSystem.Instance.EvaluateWeakSpotHit(
                        speciesId,
                        swipeDir,
                        monsterSeam,
                        SpellAffinity.None,
                        SpellAffinity.None,
                        out bool triggeredWeakSpotCrit);

                    if (triggeredWeakSpotCrit)
                    {
                        precisionMultiplier *= weakSpotMult;
                    }
                }

                float totalMultiplier = speedMultiplier * precisionMultiplier * riposteMult;

                float rawDamage = (baseSlashPower + (velocity * velocityScalingFactor)) * totalMultiplier;
                if (BindingContractSystem.Instance != null)
                {
                    rawDamage *= BindingContractSystem.Instance.GetContractCombatStatMultiplier();
                }
                float finalKineticDamage = rawDamage;
                if (UncappedAccessorySystem.Instance != null)
                {
                    finalKineticDamage = UncappedAccessorySystem.Instance.CalculateTotalStrikeOutput(rawDamage);
                }

                if (precision > maxPrecisionHit) maxPrecisionHit = precision;
                struckAny = true;

                // Deliver Damage to target
                if (col.TryGetComponent<ISliceable>(out var sliceable))
                {
                    sliceable.OnSlice(contactPoint, worldSlashDir, finalKineticDamage + armorPenetration);
                }
                else if (col.TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    var payload = new DamagePayload(finalKineticDamage, DamageType.PhysicalSlash, contactPoint, worldSlashDir, gameObject);
                    payload.armorPenetration = armorPenetration;
                    payload.postureDamage = postureDamage;
                    payload.critMultiplier = precisionMultiplier;
                    receiver.TakeAdvancedDamage(payload);
                }
                else if (col.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.TakeDamage((int)(finalKineticDamage + armorPenetration));
                    enemy.Stagger(precision >= 0.8f ? 0.75f : 0.45f, cam.transform.position);
                }
                else if (col.TryGetComponent<BossController>(out var boss))
                {
                    boss.TakeDamage(finalKineticDamage + armorPenetration);
                }
                else if (col.TryGetComponent<CaveMiningNode>(out var node))
                {
                    node.ExecuteChiselStrike();
                }

                // Visual hit sparks & directional Zelda crescent wave with custom blade tint
                WarriorSwordSlashVFX.SpawnHitSpark(contactPoint, -cam.transform.forward);
                WarriorSwordSlashVFX.SpawnSlashWave(contactPoint, worldSlashDir, swipeAngleDeg, velocity, waveColor);

                // Blood & Hearth Visceral Combat Bridge Hook
                var hearthBridge = UnityEngine.Object.FindFirstObjectByType<CombatToBloodHearthBridge>();
                if (hearthBridge != null)
                {
                    col.TryGetComponent<BattleSummoner.DesignPhilosophy.SeverableBossLimb>(out var bossLimb);
                    hearthBridge.ProcessLiveSwipeHit(contactPoint, worldSlashDir, finalKineticDamage, precision >= 0.82f, bossLimb);
                }
                else if (BattleSummoner.Core.BloodAndHearthMasterDirector.Instance != null)
                {
                    BattleSummoner.Core.BloodAndHearthMasterDirector.Instance.RegisterVisceralCombatHit(
                        contactPoint, worldSlashDir, finalKineticDamage, precision >= 0.82f, false);
                }

                Debug.Log($"⚔️ [BLADE SLICE{(isOffHand ? " (OFF-HAND)" : "")}] Hit {col.name}! Damage={finalKineticDamage:F1} (Riposte={riposteMult:F1}x)");
            }

            // Step 2 Sequelitis Integration: Selective Z-Axis Cleave for flying swarms, destructible pillars, instant chests, and boss limbs
            CheckSliceCollisions(worldSlashDir, swipeAngleDeg, velocity, baseSlashPower * speedMultiplier, "Physical");

            if (struckAny)
            {
                float hitStop = hitStopDurationSec * (maxPrecisionHit >= 0.8f ? 1.45f : 1.0f);
                if (!inHitStop && gameObject.activeInHierarchy)
                {
                    StartCoroutine(ExecuteHitStopRoutine(hitStop));
                }

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    string sfx = maxPrecisionHit >= 0.8f ? "sword_impact" : hitSFXPreset;
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX(sfx, 0.95f);
                }

                OnSliceExecuted?.Invoke(cam.transform.position + cam.transform.forward * 2.5f, baseSlashPower * speedMultiplier);
            }
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            Vector2 proj = a + t * ab;
            return Vector2.Distance(p, proj);
        }

        private void HandleBladeSlash(Vector2 direction, float velocity)
        {
            // Backward-compatible fallback if screen coords are not provided
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            HandleScreenBladeSlash(center - direction * 120f, center + direction * 120f, velocity);
        }

        private IEnumerator ExecuteHitStopRoutine(float duration)
        {
            inHitStop = true;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1.0f;
            inHitStop = false;
        }

        /// <summary>
        /// Universal Two-Finger Tap Parry execution trigger.
        /// Evaluates Dexterity Disarm & Snatch counter through CombatMasteryAndEvolutionSystem.
        /// </summary>
        public void TriggerTwoFingerParry()
        {
            OnTwoFingerParryExecuted?.Invoke();

            if (CombatMasteryAndEvolutionSystem.Instance != null)
            {
                CombatMasteryAndEvolutionSystem.Instance.TryDexterityDisarmAndCollectCounter("enemy_blade", out float counterMult);
            }
        }

        /// <summary>
        /// Explicit API to slice a target directly through the uncapped matrix.
        /// </summary>
        public float ProcessSliceTarget(ISliceable target, Vector3 point, Vector3 direction, float rawDamage)
        {
            float finalKineticDamage = rawDamage;
            if (UncappedAccessorySystem.Instance != null)
            {
                finalKineticDamage = UncappedAccessorySystem.Instance.CalculateTotalStrikeOutput(rawDamage);
            }

            target?.OnSlice(point, direction, finalKineticDamage);
            return finalKineticDamage;
        }

        /// <summary>
        /// Sequelitis & How About Master Engine Integration:
        /// Slices with a vertical slab in 3D space to cleave flying bat swarms, low crawlers,
        /// provoke Iron Knuckle guards, sever boss limbs, smash destructible pillars, and pop instant chests.
        /// </summary>
        public void CheckSliceCollisions(Vector3 worldSlashDir, float swipeAngleDeg, float velocity, float baseDamage, string elementTag = "Physical")
        {
            if (BattleSummoner.DesignPhilosophy.SequelitisAndHowAboutMasterEngine.Instance != null)
            {
                Vector3 origin = transform.position;
                BattleSummoner.DesignPhilosophy.SequelitisAndHowAboutMasterEngine.Instance.ExecuteSelectiveZAxisSwipeCleave(
                    origin, worldSlashDir, swipeAngleDeg, velocity, baseDamage, elementTag);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;

namespace BattleSummoner.Mounts
{
    /// <summary>
    /// MountEntity handles locomotion, rider mounting/dismounting, and ramming combat mechanics.
    /// Can be a war chariot, heavy construct golem, or familiar beast.
    /// Implements high-momentum chariot ramming, trample knockback, and side scythe cleaving!
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class MountEntity : MonoBehaviour
    {
        [Header("Mount Configuration")]
        [SerializeField] private MountStats stats = new MountStats();
        public MountStats Stats => stats;

        [Header("Sockets & Transforms")]
        [SerializeField] private Transform saddleSocket;
        [SerializeField] private Transform frontPlowSocket;
        [SerializeField] private Transform leftScytheSocket;
        [SerializeField] private Transform rightScytheSocket;
        [SerializeField] private Transform leftWheel;
        [SerializeField] private Transform rightWheel;

        [Header("Kinetic Overdrive State")]
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float kineticMomentum = 0f; // 0.0 to 1.0
        [SerializeField] private bool isRammingCharge = false;

        public float CurrentSpeed => currentSpeed;
        public float KineticMomentum => kineticMomentum;
        public bool IsRammingCharge => isRammingCharge;
        public bool IsOccupied => currentRider != null;
        public Transform SaddleSocket => saddleSocket;
        public Transform CurrentRider => currentRider;

        private CharacterController controller;
        private Transform currentRider;
        private Vector3 verticalVelocity = Vector3.zero;
        private readonly Dictionary<int, float> hitCooldowns = new Dictionary<int, float>();
        private float distanceTraveled = 0f;

        // Visual effects references
        private GameObject frontRamAura;
        private GameObject dustTrailObj;

        private void Awake()
        {
            EnsureController();
            EnsureSocketsAndProceduralMesh();
        }

        private CharacterController EnsureController()
        {
            if (controller == null)
            {
                controller = GetComponent<CharacterController>();
                if (controller == null) controller = gameObject.AddComponent<CharacterController>();
                controller.radius = 0.9f;
                controller.height = 1.6f;
                controller.center = new Vector3(0f, 0.8f, 0f);
            }
            return controller;
        }

        private void Update()
        {
            // Clean up hit cooldowns
            float now = Time.time;
            var expiredKeys = new List<int>();
            foreach (var kvp in hitCooldowns)
            {
                if (now - kvp.Value > 0.8f) expiredKeys.Add(kvp.Key);
            }
            for (int i = 0; i < expiredKeys.Count; i++) hitCooldowns.Remove(expiredKeys[i]);

            // Spin chariot wheels if moving
            if (stats.mountType == MountType.RammingWarChariot)
            {
                float wheelRotation = (currentSpeed * Time.deltaTime / (0.8f * Mathf.PI)) * 360f;
                if (leftWheel != null) leftWheel.Rotate(Vector3.right, wheelRotation, Space.Self);
                if (rightWheel != null) rightWheel.Rotate(Vector3.right, wheelRotation, Space.Self);
            }

            // Visual aura update for Ramming Overdrive
            if (frontRamAura != null)
            {
                frontRamAura.SetActive(isRammingCharge && kineticMomentum > 0.4f);
            }
        }

        #region Mounting & Dismounting
        public bool TryMount(Transform rider, float riderHeight, float riderWeight, out string rejection)
        {
            if (IsOccupied)
            {
                rejection = "<color=#FF5555>[MOUNT OCCUPIED]</color>\nSomeone is already riding this mount!";
                return false;
            }

            if (!MountFittingEvaluator.CanRiderFit(stats, riderHeight, riderWeight, out rejection))
            {
                return false;
            }

            currentRider = rider;
            rider.SetParent(saddleSocket != null ? saddleSocket : transform, false);
            rider.localPosition = Vector3.zero;
            rider.localRotation = Quaternion.identity;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 1.0f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#FFEE22>[MOUNTED]</color>\nRiding <b>{stats.mountName}</b>!\nHold [Shift] for Ramming Charge!");
            }

            return true;
        }

        public void Dismount()
        {
            if (currentRider == null) return;

            Transform rider = currentRider;
            currentRider = null;
            rider.SetParent(null, true);

            // Safe offset placement beside mount
            Vector3 safeDisembark = transform.position - transform.right * 1.6f + Vector3.up * 0.1f;
            rider.position = safeDisembark;

            currentSpeed = 0f;
            kineticMomentum = 0f;
            isRammingCharge = false;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.9f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#CCCCCC>[DISMOUNTED]</color>\nDismounted from {stats.mountName}.");
            }
        }

        private void OnDestroy()
        {
            if (currentRider != null)
            {
                currentRider.SetParent(null, true);
                currentRider = null;
            }
        }
        #endregion

        #region Movement & Ramming Engine
        public void ProcessRiderInput(Vector2 moveInput, bool chargeRequested, bool jumpRequested, float dt)
        {
            // 1. Steering & Rotation
            if (Mathf.Abs(moveInput.x) > 0.05f)
            {
                float steerSpeed = stats.turnSpeedDeg * (isRammingCharge ? 0.65f : 1.0f);
                transform.Rotate(Vector3.up, moveInput.x * steerSpeed * dt);
            }

            // 2. Acceleration & Velocity
            float forwardInput = Mathf.Clamp(moveInput.y, -0.4f, 1.0f);
            bool isMovingForward = forwardInput > 0.1f;

            if (chargeRequested && isMovingForward)
            {
                isRammingCharge = true;
                kineticMomentum = Mathf.MoveTowards(kineticMomentum, 1.0f, dt * 0.65f); // 1.5s to reach 100% overdrive
            }
            else
            {
                if (!isMovingForward) isRammingCharge = false;
                kineticMomentum = Mathf.MoveTowards(kineticMomentum, 0f, dt * 1.2f);
            }

            float targetSpeed = 0f;
            if (forwardInput > 0.05f)
            {
                targetSpeed = isRammingCharge
                    ? Mathf.Lerp(stats.sprintSpeed, stats.rammingTopSpeed, kineticMomentum)
                    : stats.cruiseSpeed;
            }
            else if (forwardInput < -0.05f)
            {
                targetSpeed = -stats.cruiseSpeed * 0.4f; // Reverse gear
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, stats.acceleration * dt);

            // 3. Gravity & Jumps
            var cc = EnsureController();
            if (cc != null && cc.isGrounded)
            {
                verticalVelocity.y = -2.0f;
                if (jumpRequested)
                {
                    verticalVelocity.y = stats.jumpForce;
                    if (BattleSummonerAudioMaster.Instance != null)
                    {
                        BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_click", 1.0f);
                    }
                }
            }
            else
            {
                verticalVelocity.y -= 22f * dt;
            }

            // 4. Execute Locomotion Move
            Vector3 moveDelta = (transform.forward * currentSpeed) + verticalVelocity;
            if (cc != null) cc.Move(moveDelta * dt);
            distanceTraveled += currentSpeed * dt;

            // 5. Ramming Chariot Collision & Trample Resolution
            if (currentSpeed > stats.cruiseSpeed * 0.8f)
            {
                ExecuteRammingImpactDetection();
            }
        }

        private void ExecuteRammingImpactDetection()
        {
            Vector3 plowCenter = frontPlowSocket != null ? frontPlowSocket.position : transform.position + transform.forward * 1.8f + Vector3.up * 0.8f;
            Collider[] hits = Physics.OverlapSphere(plowCenter, stats.ramHitRadius);

            float speedRatio = Mathf.Clamp01(currentSpeed / stats.rammingTopSpeed);
            float ramDamage = stats.baseRamDamage * (0.6f + speedRatio * 0.8f) * (1.0f + kineticMomentum * 0.5f);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col.gameObject == gameObject || (currentRider != null && col.gameObject == currentRider.gameObject) || col.CompareTag("Player"))
                    continue;

                int targetId = col.gameObject.GetInstanceID();
                if (hitCooldowns.ContainsKey(targetId)) continue;
                hitCooldowns[targetId] = Time.time;

                // Deliver Kinetic Ramming Damage
                Vector3 hitPoint = col.ClosestPoint(plowCenter);
                var ramPayload = new DamagePayload(ramDamage, DamageType.PhysicalBlunt, hitPoint, transform.forward, gameObject)
                {
                    accuracy = 1.0f,
                    critChance = 0.25f,
                    critMultiplier = 2.0f,
                    appliedStatus = (kineticMomentum > 0.7f) ? StatusEffectType.Slowed : StatusEffectType.None,
                    statusDuration = 3.0f
                };

                DamageResult result = null;
                if (col.TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    result = receiver.TakeAdvancedDamage(ramPayload);
                }
                else if (col.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.TakeDamage(ramDamage);
                    result = new DamageResult { finalDamage = ramDamage, outcome = HitOutcome.Normal, damageType = DamageType.PhysicalBlunt };
                }
                else if (col.TryGetComponent<BossController>(out var boss))
                {
                    float bossDmg = ramDamage + stats.bossRamBonusDamage;
                    boss.TakeDamage(bossDmg);
                    result = new DamageResult { finalDamage = bossDmg, outcome = HitOutcome.Critical, damageType = DamageType.PhysicalBlunt };
                    
                    // Boss impact recoil slows chariot
                    currentSpeed *= 0.3f;
                    kineticMomentum = 0f;
                }

                // Apply Stagger & Knockback
                if (col.TryGetComponent<IEnemyStagger>(out var staggerable))
                {
                    staggerable.Stagger(1.5f, transform.position);
                }

                // Trample physics impulse: fling smaller enemies away from the chariot!
                var rb = col.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 knockDir = (col.transform.position - transform.position).normalized + Vector3.up * 0.4f;
                    rb.AddForce(knockDir * stats.trampleKnockbackForce, ForceMode.Impulse);
                }

                // Visual & Audio Impact Feedback
                WarriorSwordSlashVFX.SpawnHitSpark(hitPoint, Vector3.up);
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.9f);
                }

                if (CombatFeedbackManager.Instance != null && result != null)
                {
                    string label = isRammingCharge ? $"<color=#FFCC00><size=130%><b>[RAMMED! {result.finalDamage:F0}]</b></size></color>" : null;
                    if (label != null) CombatFeedbackManager.Instance.SpawnFloatingText(hitPoint + Vector3.up * 1.0f, label, 1.3f);
                }
            }

            // Chariot Side Scythe Blades Sweep
            if (stats.mountType == MountType.RammingWarChariot)
            {
                ExecuteScytheBladeSweep();
            }
        }

        private void ExecuteScytheBladeSweep()
        {
            Vector3 leftScythePos = leftScytheSocket != null ? leftScytheSocket.position : transform.position - transform.right * 1.5f + Vector3.up * 0.5f;
            Vector3 rightScythePos = rightScytheSocket != null ? rightScytheSocket.position : transform.position + transform.right * 1.5f + Vector3.up * 0.5f;

            SweepScytheAt(leftScythePos, -transform.right);
            SweepScytheAt(rightScythePos, transform.right);
        }

        private void SweepScytheAt(Vector3 scytheOrigin, Vector3 sideDir)
        {
            Collider[] hits = Physics.OverlapSphere(scytheOrigin, 1.4f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col.gameObject == gameObject || (currentRider != null && col.gameObject == currentRider.gameObject) || col.CompareTag("Player"))
                    continue;

                int targetId = col.gameObject.GetInstanceID();
                if (hitCooldowns.ContainsKey(targetId)) continue;
                hitCooldowns[targetId] = Time.time;

                Vector3 hitPoint = col.ClosestPoint(scytheOrigin);
                var scythePayload = new DamagePayload(stats.scytheCleaveDamage, DamageType.PhysicalSlash, hitPoint, sideDir, gameObject)
                {
                    critChance = 0.30f,
                    critMultiplier = 2.2f
                };

                if (col.TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    receiver.TakeAdvancedDamage(scythePayload);
                }
                else if (col.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.TakeDamage(stats.scytheCleaveDamage);
                }

                WarriorSwordSlashVFX.SpawnSlashWave(hitPoint, sideDir, 0f, 15f);
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("blade_hit", 0.9f);
                }
            }
        }
        #endregion

        #region Procedural 3D Mesh Synthesis
        public void EnsureSocketsAndProceduralMesh()
        {
            if (saddleSocket == null)
            {
                var s = new GameObject("SaddleSocket");
                s.transform.SetParent(transform, false);
                s.transform.localPosition = stats.saddleLocalOffset;
                saddleSocket = s.transform;
            }

            if (frontPlowSocket == null)
            {
                var p = new GameObject("FrontPlowSocket");
                p.transform.SetParent(transform, false);
                p.transform.localPosition = new Vector3(0f, 0.45f, stats.chassisScale.z * 0.5f);
                frontPlowSocket = p.transform;
            }

            if (leftScytheSocket == null)
            {
                var ls = new GameObject("LeftScytheSocket");
                ls.transform.SetParent(transform, false);
                ls.transform.localPosition = new Vector3(-stats.chassisScale.x * 0.65f, 0.4f, 0f);
                leftScytheSocket = ls.transform;
            }

            if (rightScytheSocket == null)
            {
                var rs = new GameObject("RightScytheSocket");
                rs.transform.SetParent(transform, false);
                rs.transform.localPosition = new Vector3(stats.chassisScale.x * 0.65f, 0.4f, 0f);
                rightScytheSocket = rs.transform;
            }

            // Build procedural visual geometry if not present
            Transform visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                var vr = new GameObject("VisualRoot");
                vr.transform.SetParent(transform, false);
                visualRoot = vr.transform;
                BuildProceduralChariotMesh(visualRoot);
            }
        }

        private void BuildProceduralChariotMesh(Transform root)
        {
            Material ironMat = CreateLitMaterial(new Color(0.25f, 0.28f, 0.32f), 0.2f);
            Material goldTrimMat = CreateLitMaterial(new Color(0.95f, 0.75f, 0.2f), 0.7f, new Color(0.9f, 0.7f, 0.1f) * 1.5f);
            Material woodMat = CreateLitMaterial(new Color(0.45f, 0.28f, 0.15f), 0.1f);
            Material bladeMat = CreateLitMaterial(new Color(0.85f, 0.9f, 0.95f), 0.8f);

            // 1. Chariot Chassis Floor & Sides
            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(root, false);
            chassis.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            chassis.transform.localScale = new Vector3(1.6f, 0.3f, 2.6f);
            RemoveCollider(chassis);
            chassis.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Front curved protection shield
            var frontShield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontShield.name = "FrontShield";
            frontShield.transform.SetParent(root, false);
            frontShield.transform.localPosition = new Vector3(0f, 1.1f, 1.1f);
            frontShield.transform.localScale = new Vector3(1.6f, 1.0f, 0.15f);
            RemoveCollider(frontShield);
            frontShield.GetComponent<Renderer>().sharedMaterial = ironMat;

            // 2. Heavy Triangular Ramming Plow (The Chariot Batter)
            var plow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plow.name = "RammingPlow";
            plow.transform.SetParent(root, false);
            plow.transform.localPosition = new Vector3(0f, 0.45f, 1.6f);
            plow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            plow.transform.localScale = new Vector3(1.1f, 0.7f, 1.1f);
            RemoveCollider(plow);
            plow.GetComponent<Renderer>().sharedMaterial = goldTrimMat;

            // 3. Dual Left and Right War Wheels
            var lWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lWheel.name = "LeftWheel";
            lWheel.transform.SetParent(root, false);
            lWheel.transform.localPosition = new Vector3(-1.0f, 0.6f, -0.2f);
            lWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            lWheel.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
            RemoveCollider(lWheel);
            lWheel.GetComponent<Renderer>().sharedMaterial = ironMat;
            leftWheel = lWheel.transform;

            var rWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rWheel.name = "RightWheel";
            rWheel.transform.SetParent(root, false);
            rWheel.transform.localPosition = new Vector3(1.0f, 0.6f, -0.2f);
            rWheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rWheel.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
            RemoveCollider(rWheel);
            rWheel.GetComponent<Renderer>().sharedMaterial = ironMat;
            rightWheel = rWheel.transform;

            // 4. Dual Side Scythe Blades (Wheeled Cleavers)
            var lScythe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lScythe.name = "LeftScytheBlade";
            lScythe.transform.SetParent(lWheel.transform, false);
            lScythe.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            lScythe.transform.localScale = new Vector3(0.08f, 0.04f, 1.6f);
            RemoveCollider(lScythe);
            lScythe.GetComponent<Renderer>().sharedMaterial = bladeMat;

            var rScythe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rScythe.name = "RightScytheBlade";
            rScythe.transform.SetParent(rWheel.transform, false);
            rScythe.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            rScythe.transform.localScale = new Vector3(0.08f, 0.04f, 1.6f);
            RemoveCollider(rScythe);
            rScythe.GetComponent<Renderer>().sharedMaterial = bladeMat;

            // 5. Driver Reins / Handlebar Console
            var handlebar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handlebar.name = "ReinsBar";
            handlebar.transform.SetParent(root, false);
            handlebar.transform.localPosition = new Vector3(0f, 1.4f, 0.8f);
            handlebar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            handlebar.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);
            RemoveCollider(handlebar);
            handlebar.GetComponent<Renderer>().sharedMaterial = goldTrimMat;

            // 6. Kinetic Ramming Shockwave Aura (Visual when at top overdrive)
            var aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aura.name = "FrontRamKineticAura";
            aura.transform.SetParent(root, false);
            aura.transform.localPosition = new Vector3(0f, 0.5f, 1.9f);
            aura.transform.localScale = new Vector3(2.5f, 1.6f, 1.8f);
            RemoveCollider(aura);
            var auraMat = CreateLitMaterial(new Color(1f, 0.8f, 0.2f, 0.5f), 0.9f, new Color(1f, 0.7f, 0.1f) * 3f);
            auraMat.SetFloat("_Surface", 1);
            auraMat.SetOverrideTag("RenderType", "Transparent");
            aura.GetComponent<Renderer>().sharedMaterial = auraMat;
            aura.SetActive(false);
            frontRamAura = aura;
        }

        private Material CreateLitMaterial(Color baseCol, float metallic, Color emission = default)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = baseCol;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", 0.65f);
            if (emission != default)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
            return mat;
        }

        private void RemoveCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);
        }
        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Combat
{
    public enum SpellProjectileType
    {
        RigidBody,   // Physics-driven throwables with gravity, mass, collision & detonation (Fireball, Bombs, Poison AOE Vials, Rocks)
        Normal,      // Straight-line direct ballistic projectile with constant velocity
        Tracking,    // Homing projectile with 3D steering forces toward targets
        Ray          // Continuous / instant raycast beam
    }

    public enum SpellDamageType
    {
        Normal,      // Standard Arcane / Ether magic
        Fire,        // Fiery explosion + Burn DoT
        Poison,      // Venomous splash + lingering toxic AOE pool
        Lightning,   // High-voltage electric shocks + micro-stuns
        Rock,        // Heavy kinetic blunt impact + posture stagger
        Water        // NamuFX hydro surge + water ball explosion + drench
    }

    /// <summary>
    /// ModularCombatSpellProjectile handles all projectile types (RigidBody, Normal, Tracking, Ray)
    /// and elemental damage types (Fire, Poison, Rock, Lightning, Normal).
    /// Fireball is explicitly a RigidBody type with gravity and physics ballistics.
    /// Poison Vials shatter into lingering toxic ground AOE puddles.
    /// Rocks deal heavy blunt posture stagger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ModularCombatSpellProjectile : MonoBehaviour
    {
        [Header("Projectile Classification")]
        [SerializeField] private SpellProjectileType projectileType = SpellProjectileType.RigidBody;
        [SerializeField] private SpellDamageType damageType = SpellDamageType.Fire;

        [Header("Ballistic & Physics Properties")]
        [SerializeField] private float speed = 25f;
        [SerializeField] private float baseDamage = 100f;
        [SerializeField] private float chargeMultiplier = 1.0f; // 1.00x up to 1.50x
        [SerializeField] private float explosionRadius = 4.5f;
        [SerializeField] private float maxLifetime = 4.0f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Tracking Parameters")]
        [SerializeField] private Transform trackingTarget;
        [SerializeField] private float homingTurnSpeed = 16f;
        [SerializeField] private float homingAcceleration = 12f;

        [Header("VFX & Lighting")]
        [SerializeField] private Light projectileLight;
        [SerializeField] private TrailRenderer trailRenderer;

        private Rigidbody rb;
        private Collider col;
        private Vector3 moveDirection = Vector3.forward;
        private float lifeTimer = 0f;
        private bool hasDetonated = false;
        private float currentSpeed;

        public SpellProjectileType ProjectileType => projectileType;
        public SpellDamageType DamageType => damageType;
        public float FinalDamage => baseDamage * chargeMultiplier;
        public float ChargeMultiplier => chargeMultiplier;

        private void Awake()
        {
            col = GetComponent<Collider>();
            if (col != null) col.isTrigger = false; // RigidBody impacts rely on collision/trigger
        }

        /// <summary>
        /// Initializes the projectile with type, damage type, initial impulse/direction, charge, and optional tracking target.
        /// </summary>
        public void Initialize(
            SpellProjectileType pType,
            SpellDamageType dType,
            Vector3 launchVelocity,
            float charge = 1.0f,
            float damage = 100f,
            float radius = 4.5f,
            Transform target = null)
        {
            projectileType = pType;
            damageType = dType;
            chargeMultiplier = Mathf.Clamp(charge, 1.0f, 1.50f);
            baseDamage = damage;
            explosionRadius = radius * (1.0f + (chargeMultiplier - 1.0f) * 0.35f);
            trackingTarget = target;
            currentSpeed = launchVelocity.magnitude;
            if (currentSpeed > 0.01f) moveDirection = launchVelocity / currentSpeed;

            // Configure Physics Rigidbody for RigidBody throwables (Fireball, Poison Vial, Rock, Bomb)
            if (projectileType == SpellProjectileType.RigidBody)
            {
                rb = GetComponent<Rigidbody>();
                if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                // Adjust mass according to damage type
                switch (damageType)
                {
                    case SpellDamageType.Rock:
                        rb.mass = 4.2f;
                        rb.linearDamping = 0.05f;
                        break;
                    case SpellDamageType.Poison:
                        rb.mass = 0.85f;
                        rb.linearDamping = 0.1f;
                        break;
                    case SpellDamageType.Fire:
                    default:
                        rb.mass = 1.1f;
                        rb.linearDamping = 0.08f;
                        break;
                }

                // Apply initial physics impulse!
                rb.linearVelocity = launchVelocity;
            }
            else
            {
                // Normal or Tracking: Kinematic flight
                rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }
                if (col != null) col.isTrigger = true;
            }

            SetupVisuals();
        }

        private void SetupVisuals()
        {
            // Set scale based on charge
            transform.localScale = Vector3.one * (0.45f * chargeMultiplier);

            // Light configuration
            if (projectileLight == null)
            {
                var lGo = new GameObject("ProjectilePointLight", typeof(Light));
                lGo.transform.SetParent(transform, false);
                projectileLight = lGo.GetComponent<Light>();
                projectileLight.type = LightType.Point;
                projectileLight.range = 7f;
            }

            // Material & Light Color Tuning based on Damage Type
            var rend = GetComponent<Renderer>();
            Material mat = null;
            if (rend != null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            }

            Color primaryColor = Color.white;
            Color emitColor = Color.white;

            switch (damageType)
            {
                case SpellDamageType.Fire:
                    primaryColor = new Color(1.0f, 0.45f, 0.10f, 1.0f);
                    emitColor = new Color(1.0f, 0.50f, 0.15f) * (6.5f * chargeMultiplier);
                    if (projectileLight != null) projectileLight.color = new Color(1.0f, 0.55f, 0.2f);
                    break;

                case SpellDamageType.Poison:
                    primaryColor = new Color(0.20f, 0.95f, 0.25f, 1.0f);
                    emitColor = new Color(0.25f, 1.0f, 0.35f) * (5.5f * chargeMultiplier);
                    if (projectileLight != null) projectileLight.color = new Color(0.2f, 1.0f, 0.3f);
                    break;

                case SpellDamageType.Rock:
                    primaryColor = new Color(0.55f, 0.42f, 0.30f, 1.0f);
                    emitColor = new Color(0.70f, 0.55f, 0.35f) * (2.0f * chargeMultiplier);
                    if (projectileLight != null) projectileLight.color = new Color(0.9f, 0.7f, 0.4f);
                    break;

                case SpellDamageType.Lightning:
                    primaryColor = new Color(0.25f, 0.85f, 1.0f, 1.0f);
                    emitColor = new Color(0.35f, 0.90f, 1.0f) * (7.0f * chargeMultiplier);
                    if (projectileLight != null) projectileLight.color = new Color(0.3f, 0.85f, 1.0f);
                    break;

                case SpellDamageType.Water:
                    primaryColor = new Color(0.15f, 0.65f, 1.0f, 1.0f);
                    emitColor = new Color(0.2f, 0.8f, 1.0f) * (6.0f * chargeMultiplier);
                    if (projectileLight != null) projectileLight.color = new Color(0.2f, 0.75f, 1.0f);
                    break;

                case SpellDamageType.Normal:
                default:
                    primaryColor = new Color(0.65f, 0.35f, 1.0f, 1.0f);
                    emitColor = new Color(0.75f, 0.45f, 1.0f) * (5.0f * chargeMultiplier);
                    if (projectileLight != null) projectileLight.color = new Color(0.7f, 0.4f, 1.0f);
                    break;
            }

            if (mat != null)
            {
                mat.color = primaryColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emitColor);
                rend.sharedMaterial = mat;
            }

            if (projectileLight != null)
            {
                projectileLight.intensity = 3.5f * chargeMultiplier;
            }
        }

        private void Update()
        {
            if (hasDetonated) return;

            lifeTimer += Time.deltaTime;

            if (projectileType == SpellProjectileType.Normal)
            {
                // Straight raycast forward movement
                float moveDist = speed * Time.deltaTime;
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
            }
            else if (projectileType == SpellProjectileType.Tracking)
            {
                // Homing steering logic
                UpdateHomingSteering();
            }

            if (lifeTimer >= maxLifetime)
            {
                Detonate();
            }
        }

        private void UpdateHomingSteering()
        {
            // Acquire fallback target if null
            if (trackingTarget == null || !trackingTarget.gameObject.activeInHierarchy)
            {
                trackingTarget = FindNearestEnemy();
            }

            if (trackingTarget != null)
            {
                Vector3 targetPoint = trackingTarget.position + Vector3.up * 1.0f;
                Vector3 desiredDir = (targetPoint - transform.position).normalized;

                // Smoothly steer moveDirection toward desiredDir
                moveDirection = Vector3.RotateTowards(moveDirection, desiredDir, homingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
                currentSpeed += homingAcceleration * Time.deltaTime;
            }

            float moveDist = currentSpeed * Time.deltaTime;
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
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        }

        private Transform FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 28f, hitLayers, QueryTriggerInteraction.Ignore);
            Transform closest = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Player")) continue;
                if (hits[i].CompareTag("Enemy") || hits[i].GetComponent<SpireEnemy>() != null || hits[i].GetComponent<BossController>() != null || hits[i].GetComponent<StatusEffectReceiver>() != null)
                {
                    float d = Vector3.Distance(transform.position, hits[i].transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closest = hits[i].transform;
                    }
                }
            }
            return closest;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasDetonated || collision.collider.CompareTag("Player")) return;
            Detonate();
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
            Vector3 impactPos = transform.position;

            // Poison Vial creates a lingering Poison AOE Puddle on the ground!
            if (damageType == SpellDamageType.Poison)
            {
                SpawnPoisonPuddle(impactPos);
            }

            // Radial Impact Damage Check
            Collider[] hits = Physics.OverlapSphere(impactPos, explosionRadius, hitLayers, QueryTriggerInteraction.Ignore);
            int targetsHit = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCol = hits[i];
                if (hitCol.CompareTag("Player")) continue;

                // 1. StatusEffectReceiver
                if (hitCol.TryGetComponent<StatusEffectReceiver>(out var statusRec))
                {
                    var payload = new DamagePayload
                    {
                        baseDamage = damage,
                        damageType = MapToProjectDamageType(damageType),
                        critChance = chargeMultiplier >= 1.35f ? 0.35f : 0.15f,
                        critMultiplier = 1.75f,
                        accuracy = 0.98f,
                        hitPoint = hitCol.ClosestPoint(impactPos)
                    };
                    statusRec.TakeAdvancedDamage(payload);

                    // Apply status effects based on damage type
                    switch (damageType)
                    {
                        case SpellDamageType.Fire:
                            statusRec.ApplyStatus(StatusEffectType.Burn, 4.5f, 1.25f);
                            break;
                        case SpellDamageType.Poison:
                            statusRec.ApplyStatus(StatusEffectType.Poisoned, 6.0f, 1.4f);
                            break;
                        case SpellDamageType.Lightning:
                            statusRec.ApplyStatus(StatusEffectType.Shocked, 3.5f, 1.0f);
                            break;
                        case SpellDamageType.Water:
                            statusRec.ApplyStatus(StatusEffectType.Slowed, 4.0f, 0.65f);
                            break;
                    }
                    targetsHit++;

                    // Spawn NamuFX high-impact stylized combat sparks
                    if (NamuSpellVFXManager.Instance != null)
                    {
                        NamuSpellVFXManager.Instance.SpawnHitVFX(hitCol.ClosestPoint(impactPos), Vector3.up, chargeMultiplier >= 1.35f);
                    }
                }
                // 2. BossController
                else if (hitCol.TryGetComponent<BossController>(out var boss))
                {
                    boss.TakeDamage(damage);
                    float staggerDuration = damageType == SpellDamageType.Rock ? 1.5f * chargeMultiplier : 0.6f * chargeMultiplier;
                    boss.Stagger(staggerDuration, impactPos);
                    targetsHit++;

                    SpawnDamagePopup(impactPos + Vector3.up * 1.0f, damage);

                    if (NamuSpellVFXManager.Instance != null)
                    {
                        NamuSpellVFXManager.Instance.SpawnHitVFX(impactPos, Vector3.up, chargeMultiplier >= 1.35f);
                    }
                }
                // 3. SpireEnemy
                else if (hitCol.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.TakeDamage(damage);
                    float staggerDuration = damageType == SpellDamageType.Rock ? 1.8f * chargeMultiplier : 0.85f * chargeMultiplier;
                    enemy.Stagger(staggerDuration, impactPos);
                    targetsHit++;

                    SpawnDamagePopup(impactPos + Vector3.up * 0.6f, damage);

                    if (NamuSpellVFXManager.Instance != null)
                    {
                        NamuSpellVFXManager.Instance.SpawnHitVFX(impactPos, Vector3.up, chargeMultiplier >= 1.35f);
                    }
                }
            }

            // Spawn NamuFX Water Detonation VFX when Water projectile hits
            if (damageType == SpellDamageType.Water && NamuSpellVFXManager.Instance != null)
            {
                NamuSpellVFXManager.Instance.SpawnWaterBallExplosion(impactPos, chargeMultiplier);
                NamuSpellVFXManager.Instance.SpawnWaterImpact(impactPos);
            }

            PlayDetonationEffects(impactPos);
            Destroy(gameObject);
        }

        private BattleSummoner.Combat.DamageType MapToProjectDamageType(SpellDamageType dt)
        {
            return dt switch
            {
                SpellDamageType.Fire => BattleSummoner.Combat.DamageType.Fire,
                SpellDamageType.Poison => BattleSummoner.Combat.DamageType.Poison,
                SpellDamageType.Lightning => BattleSummoner.Combat.DamageType.Electric,
                SpellDamageType.Rock => BattleSummoner.Combat.DamageType.PhysicalBlunt,
                SpellDamageType.Water => BattleSummoner.Combat.DamageType.Ice,
                _ => BattleSummoner.Combat.DamageType.ArcaneVoid
            };
        }

        private void SpawnDamagePopup(Vector3 pos, float dmg)
        {
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnDamagePopup(
                    pos,
                    new DamageResult
                    {
                        finalDamage = dmg,
                        damageType = MapToProjectDamageType(damageType),
                        outcome = chargeMultiplier >= 1.35f ? HitOutcome.Critical : HitOutcome.Normal
                    });
            }
        }

        private void SpawnPoisonPuddle(Vector3 pos)
        {
            // Snap to ground
            if (Physics.Raycast(pos + Vector3.up * 1f, Vector3.down, out RaycastHit gHit, 4f, hitLayers, QueryTriggerInteraction.Ignore))
            {
                pos = gHit.point + Vector3.up * 0.05f;
            }

            GameObject puddleObj = new GameObject("PoisonAoePuddle");
            puddleObj.transform.position = pos;
            var puddle = puddleObj.AddComponent<PoisonAoePuddle>();
            puddle.Initialize(explosionRadius * 1.25f, baseDamage * 0.35f, 6.5f);
        }

        private void PlayDetonationEffects(Vector3 pos)
        {
            // Audio
            if (BattleSummonerAudioMaster.Instance != null)
            {
                string sfx = damageType switch
                {
                    SpellDamageType.Fire => "cinematic_boom",
                    SpellDamageType.Poison => "malformed_wheeze",
                    SpellDamageType.Rock => "heavy_impact",
                    SpellDamageType.Lightning => "crystal_resonate",
                    _ => "cinematic_boom"
                };
                BattleSummonerAudioMaster.Instance.PlayPresetSFX(sfx, 0.9f);
            }

            // Visual Blast Sphere
            SpawnExplosionVFX(pos);
        }

        private void SpawnExplosionVFX(Vector3 pos)
        {
            GameObject blast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blast.name = $"{damageType}_DetonationBlast";
            blast.transform.position = pos;
            blast.transform.localScale = Vector3.one * (0.6f * chargeMultiplier);

            var col = blast.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = blast.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                Color blastColor = damageType switch
                {
                    SpellDamageType.Fire => new Color(1.0f, 0.45f, 0.1f, 0.9f),
                    SpellDamageType.Poison => new Color(0.25f, 1.0f, 0.3f, 0.9f),
                    SpellDamageType.Rock => new Color(0.6f, 0.45f, 0.3f, 0.9f),
                    SpellDamageType.Lightning => new Color(0.3f, 0.85f, 1.0f, 0.95f),
                    _ => new Color(0.7f, 0.4f, 1.0f, 0.9f)
                };
                mat.color = blastColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", blastColor * (6.0f * chargeMultiplier));
                rend.sharedMaterial = mat;
            }

            blast.AddComponent<ExplosionShockwaveFader>().Initialize(chargeMultiplier);
        }
    }

    /// <summary>
    /// PoisonAoePuddle creates a lingering toxic zone on the ground.
    /// Ticks poison damage and refreshes the Poisoned status effect on any enemy inside!
    /// </summary>
    public class PoisonAoePuddle : MonoBehaviour
    {
        private float radius = 5.0f;
        private float tickDamage = 35f;
        private float lifetime = 6.5f;
        private float tickInterval = 0.55f;
        private float timer = 0f;
        private float nextTickTime = 0f;
        private GameObject visualPuddleDisk;
        private Material diskMat;

        public void Initialize(float puddleRadius, float damagePerTick, float duration)
        {
            radius = puddleRadius;
            tickDamage = damagePerTick;
            lifetime = duration;

            CreateVisualDisk();
            Destroy(gameObject, lifetime);
        }

        private void CreateVisualDisk()
        {
            visualPuddleDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visualPuddleDisk.name = "ToxicPuddleVisual";
            visualPuddleDisk.transform.SetParent(transform, false);
            visualPuddleDisk.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);

            var col = visualPuddleDisk.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = visualPuddleDisk.GetComponent<Renderer>();
            if (rend != null)
            {
                diskMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                diskMat.color = new Color(0.18f, 0.95f, 0.28f, 0.70f);
                diskMat.EnableKeyword("_EMISSION");
                diskMat.SetColor("_EmissionColor", new Color(0.20f, 1.0f, 0.35f) * 3.5f);
                rend.sharedMaterial = diskMat;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // Pulsing visual effect
            if (visualPuddleDisk != null)
            {
                float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 6f);
                visualPuddleDisk.transform.localScale = new Vector3(radius * 2f * pulse, 0.04f, radius * 2f * pulse);
            }

            // Periodic damage tick
            if (Time.time >= nextTickTime)
            {
                nextTickTime = Time.time + tickInterval;
                TickDamageInPuddle();
            }

            // Fade out in last 1.5 seconds
            if (timer > lifetime - 1.5f && diskMat != null)
            {
                float fade = (lifetime - timer) / 1.5f;
                Color c = diskMat.color;
                c.a = Mathf.Clamp01(fade * 0.7f);
                diskMat.color = c;
            }
        }

        private void TickDamageInPuddle()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col.CompareTag("Player")) continue;

                if (col.TryGetComponent<StatusEffectReceiver>(out var statusRec))
                {
                    statusRec.TakeAdvancedDamage(new DamagePayload
                    {
                        baseDamage = tickDamage,
                        damageType = BattleSummoner.Combat.DamageType.Poison,
                        critChance = 0.05f,
                        critMultiplier = 1.4f,
                        accuracy = 1.0f,
                        hitPoint = col.ClosestPoint(transform.position)
                    });
                    statusRec.ApplyStatus(StatusEffectType.Poisoned, 4.0f, 1.0f);
                }
                else if (col.TryGetComponent<BossController>(out var boss))
                {
                    boss.TakeDamage(tickDamage);
                }
                else if (col.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.TakeDamage(tickDamage);
                }
            }
        }
    }
}

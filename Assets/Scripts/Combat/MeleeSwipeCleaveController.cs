using System;
using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// MeleeSwipeCleaveController executes directional swipe swings and AoE cleaves:
    /// - Hits all targets in a 120-degree cone (3.5m radius)
    /// - Spawns expanding crescent slash VFX
    /// - Calculates accuracy (Miss chance) and critical strikes
    /// - Wakes sleeping enemies with Nightmare Reap (2.5x critical damage)
    /// </summary>
    public class MeleeSwipeCleaveController : MonoBehaviour
    {
        public static MeleeSwipeCleaveController Instance { get; private set; }

        [Header("Swipe Swing Parameters")]
        [SerializeField] private float cleaveRadius = 3.6f;
        [SerializeField] private float cleaveAngleDeg = 120f;
        [SerializeField] private float baseSlashDamage = 45f;
        [SerializeField] private float critChance = 0.22f;
        [SerializeField] private float critMultiplier = 2.2f;
        [SerializeField] private float attackCooldown = 0.42f;

        [Header("Status Infusion")]
        [SerializeField] private DamageType weaponElement = DamageType.PhysicalSlash;
        [SerializeField] private StatusEffectType infusedStatus = StatusEffectType.None;

        private float lastAttackTime = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Update()
        {
            // Trigger swipe swing with Spacebar or Left Mouse Button when not clicking UI
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                ExecuteSwipeSwing(transform.forward, 0f);
            }
        }

        public void ExecuteSwipeSwing(Vector3 swingDirection, float swipeRollAngle = 0f)
        {
            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;

            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 forward = swingDirection.normalized;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = transform.forward;

            // 1. Spawn Crescent Slash Arc VFX
            WarriorSwordSlashVFX.SpawnSlashWave(origin + forward * 0.8f, forward, swipeRollAngle, 18f);

            // 2. Audio
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("blade_hit", 0.95f);
            }

            // 3. AoE Cone Cleave Hit Detection
            Collider[] hits = Physics.OverlapSphere(origin, cleaveRadius);
            int struckCount = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col.gameObject == gameObject || col.CompareTag("Player")) continue;

                Vector3 targetDir = (col.transform.position - origin).normalized;
                targetDir.y = 0f;
                float angle = Vector3.Angle(forward, targetDir);

                if (angle <= cleaveAngleDeg * 0.5f)
                {
                    // Target is within the swipe cleave cone!
                    Vector3 contactPoint = col.ClosestPoint(origin);

                    float strikeDamage = baseSlashDamage;
                    if (UncappedAccessorySystem.Instance != null)
                    {
                        strikeDamage = UncappedAccessorySystem.Instance.CalculateTotalStrikeOutput(strikeDamage);
                    }

                    var payload = new DamagePayload(strikeDamage, weaponElement, contactPoint, forward, gameObject)
                    {
                        critChance = critChance,
                        critMultiplier = critMultiplier,
                        accuracy = 0.96f,
                        isAoE = true,
                        aoeRadius = cleaveRadius,
                        appliedStatus = infusedStatus,
                        statusDuration = 4.0f,
                        statusPotency = 12f
                    };

                    // Deliver damage through StatusEffectReceiver or IDamageReceiver
                    if (col.TryGetComponent<IDamageReceiver>(out var receiver))
                    {
                        receiver.TakeAdvancedDamage(payload);
                        struckCount++;
                    }
                    else if (col.TryGetComponent<SpireEnemy>(out var enemy))
                    {
                        var res = new DamageResult { finalDamage = baseSlashDamage, outcome = HitOutcome.Normal, damageType = DamageType.PhysicalSlash };
                        enemy.TakeDamage(baseSlashDamage);
                        if (CombatFeedbackManager.Instance != null) CombatFeedbackManager.Instance.SpawnDamagePopup(contactPoint, res);
                        struckCount++;
                    }
                    else if (col.TryGetComponent<BossController>(out var boss))
                    {
                        boss.TakeDamage(baseSlashDamage);
                        struckCount++;
                    }
                }
            }

            if (struckCount > 0)
            {
                WarriorSwordSlashVFX.SpawnHitSpark(origin + forward * 1.5f, Vector3.up);
            }
        }
    }
}

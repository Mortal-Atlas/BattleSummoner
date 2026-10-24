using System;
using System.Collections;
using UnityEngine;
using BattleSummoner.Combat;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// Specialized Boss Animation & Combat Controller implementing the Giant Animations Moveset
    /// (Heavy Stomp, Overhead Slam, Sweeping Swipe, Intimidating Roar, Ground Rupture).
    /// Works with BossController and ApexNemesisEntity to deliver heavy-impact giant boss encounters.
    /// </summary>
    [RequireComponent(typeof(BossController))]
    public class GiantBossAnimationController : MonoBehaviour
    {
        [Header("Giant Animation Linkage")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform slamImpactPoint;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;

        [Header("Telegraph & Shockwave Visuals")]
        [SerializeField] private GameObject telegraphDecalPrefab;
        [SerializeField] private GameObject giantShockwavePrefab;
        [SerializeField] private ParticleSystem roarVfx;

        [Header("Giant Moveset Configuration")]
        [SerializeField] private float heavyStompDamage = 180f;
        [SerializeField] private float overheadSlamDamage = 350f;
        [SerializeField] private float sweepingSwipeDamage = 220f;
        [SerializeField] private float sweepRadius = 8.5f;
        [SerializeField] private float roarStaggerRadius = 22.0f;

        public GiantBossState CurrentState => currentState;

        private BossController bossController;
        private GiantBossState currentState = GiantBossState.Idle;
        private bool isAttacking = false;

        // Animator Parameter Hashes
        private static readonly int HashGiantState = Animator.StringToHash("GiantState");
        private static readonly int HashHeavyStomp = Animator.StringToHash("HeavyStompTrigger");
        private static readonly int HashOverheadSlam = Animator.StringToHash("OverheadSlamTrigger");
        private static readonly int HashSweepingSwipe = Animator.StringToHash("SweepingSwipeTrigger");
        private static readonly int HashRoar = Animator.StringToHash("RoarTrigger");
        private static readonly int HashStagger = Animator.StringToHash("StaggerTrigger");
        private static readonly int HashDeath = Animator.StringToHash("DeathTrigger");

        public event Action<GiantBossState> OnGiantStateChanged;
        public event Action OnGroundImpact;

        private void Awake()
        {
            bossController = GetComponent<BossController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            // Setup default impact origin if unassigned
            if (slamImpactPoint == null)
            {
                var point = new GameObject("GiantSlamImpactPoint");
                point.transform.SetParent(transform, false);
                point.transform.localPosition = new Vector3(0f, 0f, 3.2f);
                slamImpactPoint = point.transform;
            }
        }

        private void OnEnable()
        {
            if (bossController != null)
            {
                bossController.OnBossTelegraphSlam += HandleBossTelegraph;
                bossController.OnBossDefeated += HandleBossDefeated;
            }
        }

        private void OnDisable()
        {
            if (bossController != null)
            {
                bossController.OnBossTelegraphSlam -= HandleBossTelegraph;
                bossController.OnBossDefeated -= HandleBossDefeated;
            }
        }

        private void HandleBossTelegraph()
        {
            // Pick between Overhead Slam (60%) or Sweeping Swipe (40%)
            if (UnityEngine.Random.value > 0.4f)
            {
                TriggerOverheadSlam();
            }
            else
            {
                TriggerSweepingSwipe();
            }
        }

        private void HandleBossDefeated()
        {
            SetState(GiantBossState.Defeated);
            if (animator != null && animator.isInitialized)
            {
                animator.SetTrigger(HashDeath);
            }
        }

        /// <summary>
        /// Executes Giant Heavy Stomp: Foot raises, pauses, and slams the floor shaking the earth.
        /// </summary>
        public void TriggerHeavyStomp()
        {
            if (isAttacking) return;
            StartCoroutine(RoutineHeavyStomp());
        }

        private IEnumerator RoutineHeavyStomp()
        {
            isAttacking = true;
            SetState(GiantBossState.HeavyStomp);
            if (animator != null && animator.isInitialized) animator.SetTrigger(HashHeavyStomp);

            yield return new WaitForSeconds(0.65f); // Telegraph windup

            // Impact
            ExecuteGroundImpact(transform.position + transform.forward * 2.0f, 6.0f, heavyStompDamage, "EarthStomp");

            yield return new WaitForSeconds(0.55f); // Recovery
            SetState(GiantBossState.Idle);
            isAttacking = false;
        }

        /// <summary>
        /// Executes Giant Two-Handed Overhead Slam: Massive crater blast.
        /// </summary>
        public void TriggerOverheadSlam()
        {
            if (isAttacking) return;
            StartCoroutine(RoutineOverheadSlam());
        }

        private IEnumerator RoutineOverheadSlam()
        {
            isAttacking = true;
            SetState(GiantBossState.OverheadSlam);
            if (animator != null && animator.isInitialized) animator.SetTrigger(HashOverheadSlam);

            yield return new WaitForSeconds(1.0f); // Telegraph charge

            // Slam impact
            Vector3 impactPos = slamImpactPoint != null ? slamImpactPoint.position : transform.position + transform.forward * 3.5f;
            ExecuteGroundImpact(impactPos, 10.0f, overheadSlamDamage, "OverheadCraterSlam");

            yield return new WaitForSeconds(0.8f);
            SetState(GiantBossState.Idle);
            isAttacking = false;
        }

        /// <summary>
        /// Executes Giant Sweeping Swipe: 180-degree horizontal arm sweep.
        /// </summary>
        public void TriggerSweepingSwipe()
        {
            if (isAttacking) return;
            StartCoroutine(RoutineSweepingSwipe());
        }

        private IEnumerator RoutineSweepingSwipe()
        {
            isAttacking = true;
            SetState(GiantBossState.SweepingSwipe);
            if (animator != null && animator.isInitialized) animator.SetTrigger(HashSweepingSwipe);

            yield return new WaitForSeconds(0.75f); // Windup

            // Sweep hit detection in front arc
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 3f, sweepRadius);
            foreach (var col in hits)
            {
                if (col.CompareTag("Player") || col.name.Contains("Hero") || col.name.Contains("Companion"))
                {
                    var receiver = col.GetComponent<IDamageReceiver>() ?? col.GetComponentInParent<IDamageReceiver>();
                    if (receiver != null)
                    {
                        var payload = new DamagePayload
                        {
                            baseDamage = sweepingSwipeDamage,
                            damageType = DamageType.PhysicalBlunt,
                            critChance = 0.15f
                        };
                        receiver.TakeAdvancedDamage(payload);
                    }
                }
            }

            yield return new WaitForSeconds(0.6f);
            SetState(GiantBossState.Idle);
            isAttacking = false;
        }

        /// <summary>
        /// Executes Giant Intimidating Roar: Shockwave stagger on all players and minions.
        /// </summary>
        public void TriggerIntimidatingRoar()
        {
            if (isAttacking) return;
            StartCoroutine(RoutineIntimidatingRoar());
        }

        private IEnumerator RoutineIntimidatingRoar()
        {
            isAttacking = true;
            SetState(GiantBossState.IntimidatingRoar);
            if (animator != null && animator.isInitialized) animator.SetTrigger(HashRoar);

            if (roarVfx != null) roarVfx.Play();

            // AoE stagger across arena
            Collider[] hits = Physics.OverlapSphere(transform.position, roarStaggerRadius);
            foreach (var col in hits)
            {
                if (col.CompareTag("Player") || col.name.Contains("Hero"))
                {
                    var prog = col.GetComponent<PlayerProgression>() ?? col.GetComponentInParent<PlayerProgression>();
                    if (prog != null)
                    {
                        prog.TakeDirectDamage(25f);
                    }
                }
            }

            yield return new WaitForSeconds(1.5f);
            SetState(GiantBossState.Idle);
            isAttacking = false;
        }

        private void ExecuteGroundImpact(Vector3 origin, float radius, float damage, string hitType)
        {
            OnGroundImpact?.Invoke();
            BattleSummoner.VFX.EasyZoomBlurController.TriggerBlur(0.65f, 0.32f);
            BattleSummoner.VFX.AnimeImpactFrameController.Trigger(BattleSummoner.VFX.AnimeImpactStyle.HighContrastMonochrome, 0.08f);
            BattleSummoner.VFX.AnimeFlipbookVFXManager.PlayFlipbook(BattleSummoner.VFX.FlipbookEffectType.RadialShockwaveRing, origin + Vector3.up * 0.15f, Quaternion.Euler(90f, 0f, 0f), radius * 1.5f);
            BattleSummoner.VFX.AnimeFlipbookVFXManager.PlayFlipbook(BattleSummoner.VFX.FlipbookEffectType.ComicSmokePoof, origin + Vector3.up * 0.3f, null, radius * 0.8f);

            // Spawn shockwave VFX
            if (giantShockwavePrefab != null)
            {
                Instantiate(giantShockwavePrefab, origin, Quaternion.identity);
            }

            // Damage and knockback players
            Collider[] hits = Physics.OverlapSphere(origin, radius);
            foreach (var col in hits)
            {
                if (col.CompareTag("Player") || col.name.Contains("Hero") || col.name.Contains("Companion"))
                {
                    var receiver = col.GetComponent<IDamageReceiver>() ?? col.GetComponentInParent<IDamageReceiver>();
                    if (receiver != null)
                    {
                        var payload = new DamagePayload
                        {
                            baseDamage = damage,
                            damageType = DamageType.PhysicalBlunt,
                            critChance = 0.5f,
                            isAoE = true,
                            aoeRadius = radius
                        };
                        receiver.TakeAdvancedDamage(payload);
                    }
                }
            }
        }

        private void SetState(GiantBossState state)
        {
            currentState = state;
            if (animator != null && animator.isInitialized)
            {
                animator.SetInteger(HashGiantState, (int)state);
            }
            OnGiantStateChanged?.Invoke(state);
        }
    }

    public enum GiantBossState
    {
        Idle,
        WalkStomp,
        HeavyStomp,
        OverheadSlam,
        SweepingSwipe,
        IntimidatingRoar,
        Staggered,
        Defeated
    }
}

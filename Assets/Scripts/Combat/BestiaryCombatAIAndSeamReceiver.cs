using System;
using UnityEngine;

namespace BattleSummoner.Combat
{
    public class BestiaryCombatAIAndSeamReceiver : MonoBehaviour
    {
        [Header("Monster Identity & Vitals")]
        public string monsterDisplayName = "Spire Bestiary Fiend";
        public float maxHp = 450f;
        public float currentHp = 450f;
        public float maxPosture = 100f;
        public float currentPosture = 100f;

        [Header("Directional Guard Seam (Sequelitis / Exiled Heavy Knight)")]
        [Tooltip("The ideal swipe angle in degrees (-180 to 180) that slices cleanly through the monster's armor gap.")]
        public float exposedSeamSlashAngleDeg = 45f;
        public float seamAngleToleranceDeg = 38f;
        public bool hasHeavyDirectionalArmor = true;

        [Header("Two-Finger Parry & Provocation Telegraph")]
        public bool isTelegraphingParryableAttack = false;
        public float attackCycleInterval = 3.6f;
        public float parryWindowDuration = 0.45f;

        [Header("References")]
        public Animator monsterAnimator;
        public SeverableMonsterPart[] severableParts;

        private float _attackTimer = 0f;
        private bool _isStaggeredGuardBroken = false;
        private float _staggerRecoveryTimer = 0f;

        public event Action<float, bool> OnMonsterDamaged; // (damage, wasCriticalSeamHit)
        public event Action OnMonsterParriedAndStaggered;
        public event Action OnMonsterSlain;

        private void Awake()
        {
            if (monsterAnimator == null) monsterAnimator = GetComponentInChildren<Animator>();
            currentHp = maxHp;
            currentPosture = maxPosture;
            RandomizeExposedSeamAngle();
        }

        private void Update()
        {
            if (currentHp <= 0f) return;

            if (_isStaggeredGuardBroken)
            {
                _staggerRecoveryTimer -= Time.deltaTime;
                if (_staggerRecoveryTimer <= 0f)
                {
                    _isStaggeredGuardBroken = false;
                    currentPosture = maxPosture;
                    RandomizeExposedSeamAngle();
                }
                return;
            }

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackCycleInterval)
            {
                _attackTimer = 0f;
                BeginTelegraphedMonsterAttack();
            }
        }

        private void BeginTelegraphedMonsterAttack()
        {
            isTelegraphingParryableAttack = true;
            if (monsterAnimator != null)
            {
                monsterAnimator.Play("Attack", 0, 0f);
            }
            Invoke(nameof(CloseParryWindow), parryWindowDuration);
        }

        private void CloseParryWindow()
        {
            isTelegraphingParryableAttack = false;
        }

        /// <summary>
        /// Evaluates a directional finger-blade slash against this monster's armor seam and posture.
        /// </summary>
        public bool ReceiveDirectionalBladeSlash(
            float baseDamage,
            float playerSwipeAngleDeg,
            float postureMultiplier,
            WeightedBladeAndRangedLagController lagController,
            ProceduralSlashIKAndAnimatorBridge playerBridge)
        {
            if (currentHp <= 0f) return false;

            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(playerSwipeAngleDeg, exposedSeamSlashAngleDeg));
            bool hitExposedSeam = !hasHeavyDirectionalArmor || _isStaggeredGuardBroken || (angleDelta <= seamAngleToleranceDeg);

            // Check if player is cashing in a 3-Second Tactical Pivot Surge!
            bool isTacticalPivot = lagController != null && lagController.TryConsumeTacticalPivotOnWeaponOrElementSwap();

            if (!hitExposedSeam && !isTacticalPivot)
            {
                // Armored Deflection! Provokes immediate counter-swing & opens the 3s Tactical Pivot Window
                if (lagController != null) lagController.NotifyAttackBlockedOrResisted();
                BeginTelegraphedMonsterAttack();
                Debug.Log($"[Directional Armor Deflect!] {monsterDisplayName} blocked {playerSwipeAngleDeg:F0}Â° slash! Exposed Seam is at {exposedSeamSlashAngleDeg:F0}Â°. (3s Tactical Pivot Window OPEN!)");
                return false;
            }

            // Clean Seam Cut or Tactical Pivot Crush!
            float finalDamage = baseDamage * (isTacticalPivot ? 2.25f : 1.65f);
            float postureDmg  = isTacticalPivot ? (maxPosture * 0.55f) : (28f * postureMultiplier);

            currentHp = Mathf.Max(0f, currentHp - finalDamage);
            currentPosture = Mathf.Max(0f, currentPosture - postureDmg);

            if (playerBridge != null) playerBridge.TriggerTactileHitStop();

            if (monsterAnimator != null)
            {
                monsterAnimator.Play(currentHp <= 0f ? "Death" : "GetHit", 0, 0f);
            }

            if (currentPosture <= 0f && !_isStaggeredGuardBroken)
            {
                TriggerGuardBreakStagger(3.5f);
            }
            else
            {
                // Shift seam angle after each clean cut so players can't mindlessly repeat one angle
                RandomizeExposedSeamAngle();
            }

            OnMonsterDamaged?.Invoke(finalDamage, true);

            if (currentHp <= 0f)
            {
                OnMonsterSlain?.Invoke();
            }
            return true;
        }

        /// <summary>
        /// Called when the player executes a Universal Two-Finger Tap (< 0.12s) while this monster is attacking.
        /// </summary>
        public bool TryReceiveTwoFingerParry(ProceduralSlashIKAndAnimatorBridge playerBridge)
        {
            if (!isTelegraphingParryableAttack || currentHp <= 0f) return false;

            isTelegraphingParryableAttack = false;
            CancelInvoke(nameof(CloseParryWindow));

            if (playerBridge != null) playerBridge.TriggerTactileHitStop();

            TriggerGuardBreakStagger(4.0f);
            OnMonsterParriedAndStaggered?.Invoke();
            Debug.Log($"[PERFECT 2-FINGER PARRY!] Deflected {monsterDisplayName}! Armor seam wide open for 4.0s!");
            return true;
        }

        private void TriggerGuardBreakStagger(float duration)
        {
            _isStaggeredGuardBroken = true;
            _staggerRecoveryTimer = duration;
            currentPosture = 0f;
        }

        private void RandomizeExposedSeamAngle()
        {
            float[] cardinalAndDiagonalAngles = { -135f, -90f, -45f, 0f, 45f, 90f, 135f, 180f };
            exposedSeamSlashAngleDeg = cardinalAndDiagonalAngles[UnityEngine.Random.Range(0, cardinalAndDiagonalAngles.Length)];
        }
    }

    [Serializable]
    public class SeverableMonsterPart
    {
        public string partName = "Right Horn";
        public Transform partBoneOrMesh;
        public float requiredSlashAngleDeg = -45f;
        public float partHp = 120f;
        public bool isSevered = false;
    }
}


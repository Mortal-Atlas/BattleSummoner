using System;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.Mounts;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// HumanoidAnimationController smoothly coordinates humanoid skeletal animations,
    /// blend trees (Idle -> Walk -> Run -> Sprint), combat actions (Dash/Roll, Slashing, Spellcasting),
    /// and dynamic procedural secondary motion (turn banking, breathing).
    /// </summary>
    [RequireComponent(typeof(PlayerMovementController))]
    public class HumanoidAnimationController : MonoBehaviour
    {
        public static HumanoidAnimationController Instance { get; private set; }

        [Header("Animator Linkage")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform avatarRigRoot;

        [Header("Blend Parameters")]
        [SerializeField] private float speedDampTime = 0.12f;
        [SerializeField] private float walkSpeedRef = 5.5f;
        [SerializeField] private float sprintSpeedRef = 9.5f;

        [Header("Procedural Secondary Motion")]
        [SerializeField] private bool enableSpineBanking = true;
        [SerializeField] private float maxBankAngle = 8.5f;
        [SerializeField] private float bankSpeed = 10f;

        private PlayerMovementController movement;
        private CharacterController characterController;
        private Transform chestBone;
        private Transform hipsBone;

        private float currentBankAngle = 0f;
        private float lastYaw = 0f;

        // Animator Parameter Hashes for maximum runtime efficiency
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashVerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int HashIsDashing = Animator.StringToHash("IsDashing");
        private static readonly int HashIsMountRiding = Animator.StringToHash("IsMountRiding");
        private static readonly int HashDashTrigger = Animator.StringToHash("DashTrigger");
        private static readonly int HashSlashTrigger = Animator.StringToHash("SlashTrigger");
        private static readonly int HashCastTrigger = Animator.StringToHash("CastTrigger");

        private void Awake()
        {
            if (Instance == null && gameObject.name.Contains("Player_Hero"))
            {
                Instance = this;
            }

            movement = GetComponent<PlayerMovementController>();
            characterController = GetComponent<CharacterController>();

            ResolveAnimator();
        }

        public void ResolveAnimator()
        {
            if (animator == null)
            {
                if (avatarRigRoot == null)
                {
                    avatarRigRoot = transform.Find("AvatarRigRoot");
                }
                if (avatarRigRoot != null)
                {
                    animator = avatarRigRoot.GetComponent<Animator>();
                }
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }

            if (avatarRigRoot != null)
            {
                hipsBone = avatarRigRoot.Find("HipsBone");
                if (hipsBone != null)
                {
                    chestBone = hipsBone.Find("TorsoBone/ChestBone");
                }
            }

            lastYaw = transform.eulerAngles.y;
        }

        private void OnValidate()
        {
            ResolveAnimator();
        }

        private void OnEnable()
        {
            PlayerMovementController.OnPlayerJump += HandleJump;
            PlayerMovementController.OnPlayerDash += HandleDash;
            PortraitGestureCanvas.OnBladeSlash += HandleBladeSlash;
            PortraitGestureCanvas.OnSpellTapCast += HandleSpellCast;
        }

        private void OnDisable()
        {
            PlayerMovementController.OnPlayerJump -= HandleJump;
            PlayerMovementController.OnPlayerDash -= HandleDash;
            PortraitGestureCanvas.OnBladeSlash -= HandleBladeSlash;
            PortraitGestureCanvas.OnSpellTapCast -= HandleSpellCast;
        }

        private void Update()
        {
            if (animator == null)
            {
                ResolveAnimator();
                if (animator == null) return;
            }

            // 1. Calculate Horizontal Velocity & Normalized Speed
            Vector3 vel = characterController != null ? characterController.velocity : Vector3.zero;
            Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
            float speedMag = horizVel.magnitude;

            // Target normalized speed: 0.0 (Idle), 0.35 (Walk), 0.80 (Run), 1.25 (Sprint)
            float targetSpeedNorm = 0f;
            if (speedMag > 0.1f)
            {
                if (movement != null && movement.IsSprinting)
                {
                    targetSpeedNorm = Mathf.Lerp(0.80f, 1.25f, (speedMag - walkSpeedRef) / Mathf.Max(0.1f, sprintSpeedRef - walkSpeedRef));
                }
                else
                {
                    targetSpeedNorm = Mathf.Lerp(0.0f, 0.75f, speedMag / walkSpeedRef);
                }
            }

            // 2. Feed parameters into Animator with smooth dampening
            animator.SetFloat(HashSpeed, targetSpeedNorm, speedDampTime, Time.deltaTime);

            bool grounded = movement != null ? movement.IsGrounded : true;
            animator.SetBool(HashIsGrounded, grounded);
            animator.SetFloat(HashVerticalSpeed, vel.y);

            bool dashing = movement != null && movement.IsDashing;
            animator.SetBool(HashIsDashing, dashing);

            // Mount Riding check
            bool isMounted = PlayerMountController.Instance != null && PlayerMountController.Instance.IsMounted;
            animator.SetBool(HashIsMountRiding, isMounted);
        }

        private void LateUpdate()
        {
            if (!enableSpineBanking || chestBone == null) return;

            // Compute turning yaw delta for athletic banking
            float currentYaw = transform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(lastYaw, currentYaw);
            lastYaw = currentYaw;

            float targetBank = 0f;
            if (characterController != null && characterController.velocity.sqrMagnitude > 4f)
            {
                targetBank = Mathf.Clamp(-deltaYaw * 2.2f, -maxBankAngle, maxBankAngle);
            }

            currentBankAngle = Mathf.Lerp(currentBankAngle, targetBank, Time.deltaTime * bankSpeed);
            if (Mathf.Abs(currentBankAngle) > 0.05f)
            {
                chestBone.localRotation *= Quaternion.Euler(0f, 0f, currentBankAngle);
            }
        }

        private void HandleJump()
        {
            if (animator == null) return;
            animator.SetBool(HashIsGrounded, false);
            animator.SetFloat(HashVerticalSpeed, 8.5f);
        }

        private void HandleDash(Vector3 direction)
        {
            if (animator == null) return;
            animator.SetTrigger(HashDashTrigger);
            animator.SetBool(HashIsDashing, true);
        }

        private void HandleBladeSlash(Vector2 direction, float velocity)
        {
            if (animator == null) return;
            animator.SetTrigger(HashSlashTrigger);
        }

        private void HandleSpellCast(Vector2 screenPos)
        {
            if (animator == null) return;
            animator.SetTrigger(HashCastTrigger);
        }

        /// <summary>
        /// Explicit API to trigger attack swing animations from external combat abilities.
        /// </summary>
        public void PlaySlashAnimation()
        {
            if (animator == null) return;
            animator.SetTrigger(HashSlashTrigger);
        }

        /// <summary>
        /// Explicit API to trigger spellcast channeling animation.
        /// </summary>
        public void PlayCastAnimation()
        {
            if (animator == null) return;
            animator.SetTrigger(HashCastTrigger);
        }

        /// <summary>
        /// Explicit API to trigger dynamic combat dash / roll.
        /// </summary>
        public void PlayDashAnimation()
        {
            if (animator == null) return;
            animator.SetTrigger(HashDashTrigger);
        }
    }
}

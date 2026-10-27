using System;
using System.Collections.Generic;
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
        [SerializeField] private float speedDampTime = 0.15f; // Smooth blending between Idle -> Walk -> Run -> Sprint
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

        // Animator Parameter Hashes for maximum runtime efficiency and cross-fade blending
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashVerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int HashIsDashing = Animator.StringToHash("IsDashing");
        private static readonly int HashIsDefending = Animator.StringToHash("IsDefending");
        private static readonly int HashIsMountRiding = Animator.StringToHash("IsMountRiding");
        private static readonly int HashDashTrigger = Animator.StringToHash("DashTrigger");
        private static readonly int HashSlashTrigger = Animator.StringToHash("SlashTrigger");
        private static readonly int HashCastTrigger = Animator.StringToHash("CastTrigger");
        private static readonly int HashParryTrigger = Animator.StringToHash("ParryTrigger");
        private static readonly int HashHitTrigger = Animator.StringToHash("HitTrigger");
        private static readonly int HashDieTrigger = Animator.StringToHash("DieTrigger");
        private static readonly int HashVictoryTrigger = Animator.StringToHash("VictoryTrigger");
        private static readonly int HashLevelUpTrigger = Animator.StringToHash("LevelUpTrigger");

        // Advanced Moveset Hashes (Free Sword, Brawler, Cooking, Archer, Throwing, Martial Arts)
        private static readonly int HashSwordCombo = Animator.StringToHash("SwordCombo");
        private static readonly int HashBrawlerStrike = Animator.StringToHash("BrawlerStrike");
        private static readonly int HashCookingAction = Animator.StringToHash("CookingAction");
        private static readonly int HashArcheryAim = Animator.StringToHash("ArcheryAim");
        private static readonly int HashArcheryRelease = Animator.StringToHash("ArcheryRelease");
        private static readonly int HashThrowTrigger = Animator.StringToHash("ThrowTrigger");
        private static readonly int HashMartialArtStrike = Animator.StringToHash("MartialArtStrike");
        private static readonly int HashCraftingAction = Animator.StringToHash("CraftingAction");
        private static readonly int HashDanceAction = Animator.StringToHash("DanceAction");

        private readonly HashSet<int> _cachedParamHashes = new HashSet<int>();
        private readonly Dictionary<int, AnimatorControllerParameterType> _cachedParamTypes = new Dictionary<int, AnimatorControllerParameterType>();

        public void CacheParameters()
        {
            _cachedParamHashes.Clear();
            _cachedParamTypes.Clear();
            if (animator == null) return;
            var ctrl = animator.runtimeAnimatorController;
            if (ctrl == null) return;

            foreach (var p in animator.parameters)
            {
                _cachedParamHashes.Add(p.nameHash);
                _cachedParamTypes[p.nameHash] = p.type;
            }
        }

        private void SafeSetInteger(int hash, int val)
        {
            if (!IsAnimatorPlayable) return;
            if (_cachedParamHashes.Contains(hash))
            {
                if (_cachedParamTypes.TryGetValue(hash, out var type))
                {
                    if (type == AnimatorControllerParameterType.Int)
                        animator.SetInteger(hash, val);
                    else if (type == AnimatorControllerParameterType.Trigger && val > 0)
                        animator.SetTrigger(hash);
                    else if (type == AnimatorControllerParameterType.Bool)
                        animator.SetBool(hash, val > 0);
                }
            }
        }

        private void SafeSetTrigger(int hash)
        {
            if (!IsAnimatorPlayable) return;
            if (_cachedParamHashes.Contains(hash))
            {
                animator.SetTrigger(hash);
            }
        }

        private void SafeSetBool(int hash, bool val)
        {
            if (!IsAnimatorPlayable) return;
            if (_cachedParamHashes.Contains(hash))
            {
                animator.SetBool(hash, val);
            }
        }

        private void SafeSetFloat(int hash, float val, float dampTime = 0f, float deltaTime = 0f)
        {
            if (!IsAnimatorPlayable) return;
            if (_cachedParamHashes.Contains(hash))
            {
                if (dampTime > 0f)
                    animator.SetFloat(hash, val, dampTime, deltaTime);
                else
                    animator.SetFloat(hash, val);
            }
        }

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

        public bool IsAnimatorPlayable => animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && (!Application.isPlaying || animator.isInitialized);

        public void ResolveAnimator()
        {
            // 1. Prioritize AvatarRigRoot or AnimeAvatarRigRoot
            Transform rigRoot = transform.Find("AvatarRigRoot") ?? transform.Find("AnimeAvatarRigRoot");
            if (rigRoot != null)
            {
                if (!rigRoot.gameObject.activeSelf) rigRoot.gameObject.SetActive(true);
                avatarRigRoot = rigRoot;
                animator = avatarRigRoot.GetComponent<Animator>();

                // Hide unrigged static starter visuals
                Transform starter = transform.Find("StarterVisualRoot");
                if (starter != null) starter.gameObject.SetActive(false);
            }

            // 2. Fallback to any active Animator in children with a valid controller
            if (animator == null || !animator.gameObject.activeInHierarchy || animator.runtimeAnimatorController == null)
            {
                var anims = GetComponentsInChildren<Animator>(false);
                foreach (var a in anims)
                {
                    if (a.gameObject.activeInHierarchy && a.runtimeAnimatorController != null)
                    {
                        animator = a;
                        avatarRigRoot = a.transform;
                        break;
                    }
                }
            }

#if UNITY_EDITOR
            // 3. Fallback: if animator has no controller assigned, link PlayerHeroAnimator
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animators/PlayerHeroAnimator.controller");
                if (ctrl != null)
                {
                    animator.runtimeAnimatorController = ctrl;
                }
            }
#endif

            // Cache available parameters to prevent missing parameter console warnings
            CacheParameters();

            // 4. Resolve Humanoid bones for secondary physics / spine banking
            if (animator != null && animator.isHuman)
            {
                chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
                hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            }

            if (chestBone == null && avatarRigRoot != null)
            {
                hipsBone = avatarRigRoot.Find("Modular_Character_Armature/Hips") ?? avatarRigRoot.Find("AnimeHumanoidRig/Root/Hips") ?? avatarRigRoot.Find("HipsBone");
                if (hipsBone != null)
                {
                    chestBone = hipsBone.Find("Spine/Chest") ?? hipsBone.Find("TorsoBone/ChestBone");
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
            if (!IsAnimatorPlayable)
            {
                ResolveAnimator();
                if (!IsAnimatorPlayable) return;
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
            SafeSetFloat(HashSpeed, targetSpeedNorm, speedDampTime, Time.deltaTime);

            bool grounded = movement != null ? movement.IsGrounded : true;
            SafeSetBool(HashIsGrounded, grounded);
            SafeSetFloat(HashVerticalSpeed, vel.y);

            bool dashing = movement != null && movement.IsDashing;
            SafeSetBool(HashIsDashing, dashing);

            // Mount Riding check
            bool isMounted = PlayerMountController.Instance != null && PlayerMountController.Instance.IsMounted;
            SafeSetBool(HashIsMountRiding, isMounted);
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
            if (!IsAnimatorPlayable) return;
            SafeSetBool(HashIsGrounded, false);
            SafeSetFloat(HashVerticalSpeed, 8.5f);
        }

        private void HandleDash(Vector3 direction)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashDashTrigger);
            SafeSetBool(HashIsDashing, true);
        }

        private void HandleBladeSlash(Vector2 direction, float velocity)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashSlashTrigger);
        }

        private void HandleSpellCast(Vector2 screenPos)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashCastTrigger);
        }

        /// <summary>
        /// Explicit API to trigger attack swing animations from external combat abilities.
        /// </summary>
        public void PlaySlashAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashSlashTrigger);
        }

        /// <summary>
        /// Explicit API to trigger spellcast channeling animation.
        /// </summary>
        public void PlayCastAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashCastTrigger);
        }

        /// <summary>
        /// Explicit API to trigger dynamic combat dash / roll.
        /// </summary>
        public void PlayDashAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashDashTrigger);
        }

        /// <summary>
        /// Explicit API to trigger parry counter / shield impact animation.
        /// </summary>
        public void PlayParryAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashParryTrigger);
        }

        /// <summary>
        /// Triggers Free Sword combo steps (1: Horizontal Slash, 2: Diagonal Cleave, 3: Heavy Thrust Finisher).
        /// </summary>
        public void PlaySwordCombo(int comboIndex = 1)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetInteger(HashSwordCombo, Mathf.Clamp(comboIndex, 1, 3));
            SafeSetTrigger(HashSlashTrigger);
        }

        /// <summary>
        /// Triggers Brawler strikes (1: Left Jab, 2: Right Cross, 3: Rising Uppercut).
        /// </summary>
        public void PlayBrawlerStrike(int strikeType = 1)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetInteger(HashBrawlerStrike, Mathf.Clamp(strikeType, 1, 3));
        }

        /// <summary>
        /// Triggers Life-Sim Cooking animations (1: Stir Pot, 2: Add Spice/Seasoning, 3: Taste Dish, 4: Plate Food).
        /// </summary>
        public void PlayCookingAction(int actionType = 1)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetInteger(HashCookingAction, Mathf.Clamp(actionType, 1, 4));
        }

        /// <summary>
        /// Triggers Human Archer bow draw, aim holding, and arrow release.
        /// </summary>
        public void PlayArcheryAction(bool isAiming)
        {
            if (!IsAnimatorPlayable) return;
            if (isAiming)
            {
                SafeSetBool(HashArcheryAim, true);
            }
            else
            {
                SafeSetBool(HashArcheryAim, false);
                SafeSetTrigger(HashArcheryRelease);
            }
        }

        /// <summary>
        /// Triggers Human Throwing animation (Flask / Bomb / Potion toss).
        /// </summary>
        public void PlayThrowAction()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashThrowTrigger);
        }

        /// <summary>
        /// Triggers Martial Arts strikes (1: High Roundhouse Kick, 2: Palm Thrust, 3: Flying Knee).
        /// </summary>
        public void PlayMartialArtsStrike(int strikeType = 1)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetInteger(HashMartialArtStrike, Mathf.Clamp(strikeType, 1, 3));
        }

        /// <summary>
        /// Triggers Human Crafting animations (1: Anvil Hammering, 2: Woodwork / Sawing, 3: Alchemy Stir, 4: Tailoring).
        /// </summary>
        public void PlayCraftingAnimation(int craftType = 1)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetInteger(HashCraftingAction, Mathf.Clamp(craftType, 1, 4));
        }

        public void StopCraftingAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetInteger(HashCraftingAction, 0);
        }

        /// <summary>
        /// Triggers Human Dance animations (1: Celebration Folk Jig, 2: Victory Dance, 3: Campfire Rhythmic Sway).
        /// </summary>
        public void PlayDanceAnimation(int danceType = 1)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetInteger(HashDanceAction, Mathf.Clamp(danceType, 1, 6));
        }

        public void StopDanceAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetInteger(HashDanceAction, 0);
        }

        /// <summary>
        /// Toggles shield guard / defend stance with seamless enter/exit cross-fade.
        /// </summary>
        public void SetDefending(bool isDefending)
        {
            if (!IsAnimatorPlayable) return;
            SafeSetBool(HashIsDefending, isDefending);
        }

        /// <summary>
        /// Triggers hit reaction stagger animation.
        /// </summary>
        public void PlayHitReactionAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashHitTrigger);
        }

        /// <summary>
        /// Triggers death / downed animation.
        /// </summary>
        public void PlayDeathAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashDieTrigger);
        }

        /// <summary>
        /// Triggers boss subjugation victory celebration animation.
        /// </summary>
        public void PlayVictoryAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashVictoryTrigger);
        }

        /// <summary>
        /// Triggers level-up clutch surge celebration animation.
        /// </summary>
        public void PlayLevelUpAnimation()
        {
            if (!IsAnimatorPlayable) return;
            SafeSetTrigger(HashLevelUpTrigger);
        }
    }
}

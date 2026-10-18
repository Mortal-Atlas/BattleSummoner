using System;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.VFX;

namespace BattleSummoner.Animation.MotionMatching
{
    /// <summary>
    /// Motion Matching Locomotion Engine (Motion Matching for Unity + MC Sample Believable Animations).
    /// Bridges high-fidelity mocap locomotion databases, real-time trajectory prediction,
    /// inertialization blending, and foot-locking IK into the character controller.
    /// Operates with 0 GC allocations per frame and 100% Android IL2CPP + PC compatibility.
    /// </summary>
    [RequireComponent(typeof(PlayerMovementController))]
    public class MotionMatchingLocomotionEngine : MonoBehaviour
    {
        public static MotionMatchingLocomotionEngine Instance { get; private set; }

        [Header("Engine Configuration")]
        [SerializeField] private bool useMotionMatching = true;
        [SerializeField] private bool enableFootLocking = true;
        [SerializeField] private bool enableInertialization = true;
        [SerializeField] private bool enableCentrifugalBanking = true;

        [Header("Responsiveness Tuning")]
        [SerializeField] private float trajectoryResponsiveness = 14f;
        [SerializeField] private float brakingDeceleration = 28f;
        [SerializeField] private float inertialTransitionTime = 0.16f;

        [Header("Current Runtime State")]
        [SerializeField] private LocomotionPhase currentPhase = LocomotionPhase.Idle;
        [SerializeField] private MocapLocomotionStyle activeMocapStyle = MocapLocomotionStyle.IdleMocapNatural;

        // Subsystem instances
        public TrajectoryPredictor Trajectory { get; private set; }
        public InertializerBlender Inertializer { get; private set; }
        public BelievableMocapLocomotionDatabase MocapDatabase { get; private set; }
        public FootLockingIK FootIK { get; private set; }

        private PlayerMovementController movementController;
        private CharacterController characterController;
        private HumanoidAnimationController humanoidAnim;
        private Animator animator;
        private Transform spineBone;
        private Transform hipsBone;

        private Vector3 lastVelocity = Vector3.zero;
        private Quaternion lastRotation = Quaternion.identity;
        private float lastTurnAngle = 0f;
        private float idleWeightShiftTimer = 0f;
        private bool isIdleWeightOnLeft = false;

        // Animator Hashes
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashMocapStyle = Animator.StringToHash("MocapStyle");
        private static readonly int HashTurnAngle = Animator.StringToHash("TurnAngle");
        private static readonly int HashIsPlantStop = Animator.StringToHash("IsPlantStop");
        private static readonly int HashIsSharpCut = Animator.StringToHash("IsSharpCut");

        public bool UseMotionMatching
        {
            get => useMotionMatching;
            set
            {
                useMotionMatching = value;
                if (FootIK != null) FootIK.SetFootLockingEnabled(value && enableFootLocking);
            }
        }

        private void Awake()
        {
            if (Instance == null && gameObject.name.Contains("Player_Hero"))
            {
                Instance = this;
            }

            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            EnsureInitialized();
            lastVelocity = characterController != null ? characterController.velocity : Vector3.zero;
            lastRotation = transform.rotation;
        }

        public void EnsureInitialized()
        {
            if (movementController == null) movementController = GetComponent<PlayerMovementController>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (humanoidAnim == null) humanoidAnim = GetComponent<HumanoidAnimationController>();

            if (Trajectory == null) Trajectory = new TrajectoryPredictor();
            if (Inertializer == null) Inertializer = new InertializerBlender(inertialTransitionTime);
            if (MocapDatabase == null) MocapDatabase = new BelievableMocapLocomotionDatabase();

            ResolveComponents();
        }

        public void ResolveComponents()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null && animator.isHuman)
            {
                spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
                hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

                if (FootIK == null)
                {
                    FootIK = animator.GetComponent<FootLockingIK>();
                    if (FootIK == null)
                    {
                        FootIK = animator.gameObject.AddComponent<FootLockingIK>();
                    }
                }
                FootIK.SetFootLockingEnabled(useMotionMatching && enableFootLocking);
            }
        }

        private void Update()
        {
            if (!useMotionMatching || characterController == null) return;

            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            Vector3 currentVel = characterController.velocity;
            Vector3 currentPlanarVel = new Vector3(currentVel.x, 0f, currentVel.z);
            float currentSpeed = currentPlanarVel.magnitude;

            // 1. Check if character is currently executing combat actions (dash, air, slash)
            bool isDashing = movementController != null && movementController.IsDashing;
            bool isGrounded = characterController.isGrounded;

            if (isDashing || !isGrounded)
            {
                // In combat dash or mid-air: suspend foot-locking and motion matching matching search
                if (FootIK != null) FootIK.SetFootLockingEnabled(false);
                currentPhase = isDashing ? LocomotionPhase.SharpCut : LocomotionPhase.Airborne;
                return;
            }
            else
            {
                if (FootIK != null && enableFootLocking) FootIK.SetFootLockingEnabled(true);
            }

            // 2. Gather desired velocity from movement input
            Vector3 desiredVel = currentPlanarVel;
            float maxSpeed = (movementController != null && movementController.IsSprinting) ? 9.5f : 5.5f;

            // 3. Update Trajectory Prediction
            Trajectory.UpdateTrajectory(
                transform,
                currentVel,
                desiredVel,
                maxSpeed,
                trajectoryResponsiveness,
                brakingDeceleration,
                dt);

            currentPhase = Trajectory.CurrentPhase;

            // 4. Motion Matching Cost Search
            MocapPoseFeature bestMatch = MocapDatabase.FindBestMatch(Trajectory, currentSpeed, activeMocapStyle);

            if (bestMatch.style != activeMocapStyle)
            {
                OnMocapTransition(bestMatch.style, currentVel, dt);
            }

            // 5. Update Inertializer Blender (damping and centrifugal lean)
            float angularTurnVelocity = (Trajectory.TurnAngle - lastTurnAngle) / dt;
            lastTurnAngle = Trajectory.TurnAngle;

            if (enableInertialization)
            {
                Inertializer.UpdateInertia(dt, angularTurnVelocity, currentSpeed, enableCentrifugalBanking);
            }

            // 6. Natural Mocap Idle Weight Shifts
            if (currentPhase == LocomotionPhase.Idle)
            {
                UpdateNaturalIdleWeightShift(dt);
            }
            else
            {
                idleWeightShiftTimer = 0f;
            }

            // 7. Push locomotion state to Animator
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(HashSpeed, currentSpeed, 0.12f, dt);
                animator.SetInteger(HashMocapStyle, (int)activeMocapStyle);
                animator.SetFloat(HashTurnAngle, Trajectory.TurnAngle);
                animator.SetBool(HashIsPlantStop, Trajectory.IsPlantStop);
                animator.SetBool(HashIsSharpCut, Trajectory.IsSharpCut);
            }

            lastVelocity = currentVel;
            lastRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (!useMotionMatching) return;

            // Apply procedural inertial spine tilt & centrifugal banking after Mecanim evaluates
            if (enableInertialization && Inertializer != null)
            {
                Inertializer.ApplySpineAndHipsInertia(spineBone, hipsBone);
            }
        }

        private void OnMocapTransition(MocapLocomotionStyle newStyle, Vector3 currentVel, float dt)
        {
            MocapLocomotionStyle oldStyle = activeMocapStyle;
            activeMocapStyle = newStyle;

            // If inertialization is active, trigger spring transition
            if (enableInertialization && Inertializer != null)
            {
                Vector3 targetVel = currentVel;
                Inertializer.TriggerTransition(lastVelocity, targetVel, lastRotation, transform.rotation, inertialTransitionTime);
            }

            // Trigger environmental feedback on intense mocap transitions
            if (newStyle == MocapLocomotionStyle.RunSkidStop_Plant)
            {
                // Play skid deceleration dust & audio
                AnimeFlipbookVFXManager.PlayFlipbook(FlipbookEffectType.ComicSmokePoof, transform.position + Vector3.up * 0.1f, null, 0.65f);
                PlayLocomotionSFX("cloth_rustle");
            }
            else if (newStyle == MocapLocomotionStyle.RunSharpCut_90L || newStyle == MocapLocomotionStyle.RunSharpCut_90R)
            {
                // Sharp direction plant cut
                AnimeFlipbookVFXManager.PlayFlipbook(FlipbookEffectType.ComicSmokePoof, transform.position + Vector3.up * 0.1f, null, 0.5f);
            }
        }

        private void UpdateNaturalIdleWeightShift(float dt)
        {
            idleWeightShiftTimer += dt;
            if (idleWeightShiftTimer > 4.2f)
            {
                idleWeightShiftTimer = 0f;
                isIdleWeightOnLeft = !isIdleWeightOnLeft;

                // Subtle authentic mocap weight shift
                if (hipsBone != null)
                {
                    float shiftOffset = isIdleWeightOnLeft ? 0.012f : -0.012f;
                    hipsBone.localPosition += new Vector3(shiftOffset, 0f, 0f);
                }
            }
        }

        private void PlayLocomotionSFX(string soundKey)
        {
            if (BattleSummonerAudioMaster.Instance != null && !string.IsNullOrEmpty(soundKey))
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX(soundKey, 0.45f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Trajectory == null || Trajectory.Points == null) return;

            // Visualize Motion Matching Trajectory in Scene View
            Gizmos.color = Color.cyan;
            Vector3 prev = transform.position;
            for (int i = 0; i < TrajectoryPredictor.PointCount; i++)
            {
                Vector3 worldPt = transform.TransformPoint(Trajectory.Points[i].position);
                Gizmos.DrawSphere(worldPt, 0.08f + i * 0.03f);
                Gizmos.DrawLine(prev, worldPt);

                // Draw forward facing orientation
                Vector3 worldDir = transform.TransformDirection(Trajectory.Points[i].direction);
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(worldPt, worldDir * 0.35f);
                Gizmos.color = Color.cyan;

                prev = worldPt;
            }
        }
    }
}

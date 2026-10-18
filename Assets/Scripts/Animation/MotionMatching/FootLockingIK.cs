using System;
using UnityEngine;

namespace BattleSummoner.Animation.MotionMatching
{
    /// <summary>
    /// Foot Locking & Anti-Foot-Slide IK Engine (Motion Matching for Unity architecture).
    /// Detects ground contacts, plants feet firmly on uneven geometry / stairs,
    /// and completely eliminates foot sliding during stops, sharp cuts, and pivots.
    /// </summary>
    public class FootLockingIK : MonoBehaviour
    {
        [Header("Foot Locking Settings")]
        [SerializeField] private bool enableFootLocking = true;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float raycastDownDistance = 0.85f;
        [SerializeField] private float footLockThresholdSpeed = 1.2f;
        [SerializeField] private float maxLegExtension = 0.95f;
        [SerializeField] private float footHeightOffset = 0.10f;
        [SerializeField] private float lockBlendSpeed = 14f;

        private Animator animator;
        private Transform leftFootBone;
        private Transform rightFootBone;
        private Transform hipsBone;

        private struct FootLockState
        {
            public bool isLocked;
            public Vector3 lockedWorldPos;
            public Quaternion lockedWorldRot;
            public float currentIKWeight;
            public Vector3 lastWorldPos;
        }

        private FootLockState leftFoot;
        private FootLockState rightFoot;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            ResolveBones();
        }

        public void ResolveBones()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                leftFootBone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFootBone = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            }
        }

        public void SetFootLockingEnabled(bool enabled)
        {
            enableFootLocking = enabled;
            if (!enabled)
            {
                leftFoot.currentIKWeight = 0f;
                rightFoot.currentIKWeight = 0f;
                leftFoot.isLocked = false;
                rightFoot.isLocked = false;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!enableFootLocking || animator == null || !animator.isHuman) return;

            UpdateFootLock(AvatarIKGoal.LeftFoot, leftFootBone, ref leftFoot);
            UpdateFootLock(AvatarIKGoal.RightFoot, rightFootBone, ref rightFoot);
        }

        private void UpdateFootLock(AvatarIKGoal goal, Transform footBone, ref FootLockState state)
        {
            if (footBone == null) return;

            Vector3 currentPos = footBone.position;
            Vector3 footVel = (currentPos - state.lastWorldPos) / Mathf.Max(0.001f, Time.deltaTime);
            state.lastWorldPos = currentPos;

            // Ground raycast from foot bone downwards
            Vector3 rayStart = currentPos + Vector3.up * 0.4f;
            bool hitGround = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDownDistance + 0.4f, groundLayers, QueryTriggerInteraction.Ignore);

            float footSpeed = footVel.magnitude;
            float distToGround = hitGround ? (currentPos.y - hit.point.y) : 99f;

            // Evaluate lock trigger: foot is close to ground and moving relatively slowly (plant phase)
            bool shouldLock = hitGround && distToGround < 0.22f && footSpeed < footLockThresholdSpeed;

            // Check leg over-extension safety to release lock
            if (state.isLocked && hipsBone != null)
            {
                float legLength = Vector3.Distance(hipsBone.position, state.lockedWorldPos);
                if (legLength > maxLegExtension)
                {
                    shouldLock = false; // Release lock before unnatural leg hyperextension
                }
            }

            if (shouldLock)
            {
                if (!state.isLocked)
                {
                    // Lock position at ground contact point
                    state.isLocked = true;
                    state.lockedWorldPos = hit.point + Vector3.up * footHeightOffset;
                    Quaternion groundRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * footBone.rotation;
                    state.lockedWorldRot = groundRot;
                }
            }
            else
            {
                state.isLocked = false;
            }

            // Smooth IK weight blending
            float targetWeight = state.isLocked ? 1.0f : 0.0f;
            state.currentIKWeight = Mathf.MoveTowards(state.currentIKWeight, targetWeight, Time.deltaTime * lockBlendSpeed);

            if (state.currentIKWeight > 0.01f)
            {
                animator.SetIKPositionWeight(goal, state.currentIKWeight);
                animator.SetIKRotationWeight(goal, state.currentIKWeight * 0.85f);
                animator.SetIKPosition(goal, state.lockedWorldPos);
                animator.SetIKRotation(goal, state.lockedWorldRot);
            }
            else
            {
                animator.SetIKPositionWeight(goal, 0f);
                animator.SetIKRotationWeight(goal, 0f);
            }
        }
    }
}

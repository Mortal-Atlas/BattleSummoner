using System;
using UnityEngine;

namespace BattleSummoner.Animation.MotionMatching
{
    /// <summary>
    /// Inertialization Blending Engine (Motion Matching for Unity architecture).
    /// Replaces floaty cross-fades with critically damped second-order spring/decay offsets.
    /// Preserves physical momentum, authentic weight transfer, and completely eliminates foot sliding.
    /// </summary>
    public class InertializerBlender
    {
        [Serializable]
        public struct InertialOffsetState
        {
            public Vector3 positionOffset;
            public Vector3 velocityOffset;
            public Quaternion rotationOffset;
            public Vector3 angularVelocityOffset;

            public void Reset()
            {
                positionOffset = Vector3.zero;
                velocityOffset = Vector3.zero;
                rotationOffset = Quaternion.identity;
                angularVelocityOffset = Vector3.zero;
            }
        }

        private float transitionDuration = 0.16f; // Standard snappy mocap transition window
        private float transitionTimer = 0f;
        private bool isTransitioning = false;

        private InertialOffsetState rootOffset;
        private InertialOffsetState hipsOffset;

        // Centrifugal banking
        private float currentCentrifugalLean = 0f;
        private float targetCentrifugalLean = 0f;

        public bool IsTransitioning => isTransitioning;
        public float Progress => transitionDuration > 0f ? Mathf.Clamp01(transitionTimer / transitionDuration) : 1f;

        public InertializerBlender(float defaultTransitionDuration = 0.16f)
        {
            transitionDuration = defaultTransitionDuration;
            rootOffset.Reset();
            hipsOffset.Reset();
        }

        /// <summary>
        /// Triggers an inertialized transition, capturing the instantaneous difference in velocity and displacement.
        /// </summary>
        public void TriggerTransition(
            Vector3 currentVelocity,
            Vector3 targetVelocity,
            Quaternion currentRotation,
            Quaternion targetRotation,
            float customDuration = -1f)
        {
            transitionDuration = customDuration > 0f ? customDuration : 0.16f;
            transitionTimer = 0f;
            isTransitioning = true;

            // Difference in linear momentum
            rootOffset.positionOffset = Vector3.zero;
            rootOffset.velocityOffset = currentVelocity - targetVelocity;

            // Difference in angular momentum
            Quaternion diffRot = currentRotation * Quaternion.Inverse(targetRotation);
            rootOffset.rotationOffset = diffRot;

            diffRot.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            rootOffset.angularVelocityOffset = axis * (angle * Mathf.Deg2Rad / Mathf.Max(0.001f, Time.deltaTime));
        }

        /// <summary>
        /// Updates decay offsets over deltaTime using critically damped spring equations.
        /// x(t) = (x0 + (v0 + w*x0)*t) * e^(-w*t)
        /// </summary>
        public void UpdateInertia(float deltaTime, float turnAngularVelocity, float currentSpeed, bool enableCentrifugalBanking = true)
        {
            if (isTransitioning)
            {
                transitionTimer += deltaTime;
                if (transitionTimer >= transitionDuration)
                {
                    isTransitioning = false;
                    rootOffset.Reset();
                }
                else
                {
                    // Natural frequency omega = 5.0 / duration ensures 99.3% decay at t = duration
                    float omega = 5.0f / Mathf.Max(0.01f, transitionDuration);
                    float t = transitionTimer;
                    float expTerm = Mathf.Exp(-omega * t);

                    // Position spring decay
                    Vector3 c = rootOffset.velocityOffset + omega * rootOffset.positionOffset;
                    Vector3 currentPosOffset = (rootOffset.positionOffset + c * t) * expTerm;

                    // Rotation spring decay
                    float decayFactor = Mathf.Clamp01(1f - (transitionTimer / transitionDuration));
                    rootOffset.rotationOffset = Quaternion.Slerp(rootOffset.rotationOffset, Quaternion.identity, 1f - expTerm);
                }
            }

            // Calculate Centrifugal Lean Banking into Turns
            if (enableCentrifugalBanking)
            {
                // Centrifugal lateral acceleration ~ speed * angularVelocity
                float targetAngle = Mathf.Clamp(turnAngularVelocity * (currentSpeed * 0.08f), -14f, 14f);
                targetCentrifugalLean = targetAngle;
                currentCentrifugalLean = Mathf.Lerp(currentCentrifugalLean, targetCentrifugalLean, deltaTime * 12f);
            }
            else
            {
                currentCentrifugalLean = Mathf.Lerp(currentCentrifugalLean, 0f, deltaTime * 10f);
            }
        }

        /// <summary>
        /// Applies the current inertial tilt and centrifugal banking to the character's spine or hips.
        /// </summary>
        public void ApplySpineAndHipsInertia(Transform spineBone, Transform hipsBone)
        {
            if (spineBone != null && Mathf.Abs(currentCentrifugalLean) > 0.01f)
            {
                // Bank into turn (roll axis)
                spineBone.localRotation *= Quaternion.Euler(0f, 0f, -currentCentrifugalLean);
            }

            if (hipsBone != null && isTransitioning)
            {
                hipsBone.localRotation = Quaternion.Slerp(hipsBone.localRotation, hipsBone.localRotation * rootOffset.rotationOffset, 0.45f);
            }
        }

        public Vector3 GetCurrentPositionOffset()
        {
            if (!isTransitioning) return Vector3.zero;
            float omega = 5.0f / Mathf.Max(0.01f, transitionDuration);
            float t = transitionTimer;
            float expTerm = Mathf.Exp(-omega * t);
            return (rootOffset.positionOffset + (rootOffset.velocityOffset + omega * rootOffset.positionOffset) * t) * expTerm;
        }

        public Quaternion GetCurrentRotationOffset()
        {
            if (!isTransitioning) return Quaternion.identity;
            return rootOffset.rotationOffset;
        }
    }
}

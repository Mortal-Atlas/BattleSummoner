using System;
using UnityEngine;

namespace BattleSummoner.Animation.MotionMatching
{
    /// <summary>
    /// Trajectory Point representing a projected position, direction, and velocity
    /// in the character's future local space at a given time delta.
    /// </summary>
    [Serializable]
    public struct TrajectoryPoint
    {
        public Vector3 position;    // Local position offset relative to character root
        public Vector3 direction;   // Normalized forward facing direction at this time
        public Vector3 velocity;    // Instantaneous velocity vector
        public float timeOffset;    // Time horizon in seconds (e.g., 0.2s, 0.4s, 0.7s)

        public TrajectoryPoint(Vector3 pos, Vector3 dir, Vector3 vel, float t)
        {
            position = pos;
            direction = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
            velocity = vel;
            timeOffset = t;
        }
    }

    public enum LocomotionPhase
    {
        Idle,
        StartTransition,    // Initial step-off from stationary stance
        ContinuousCycle,    // Steady-state walk/run cycle
        PlantStop,          // Decelerating and planting foot to stop
        SharpCut,           // Sudden 45-180 degree direction reversal / cut
        Airborne
    }

    /// <summary>
    /// Motion Matching Trajectory Predictor (Motion Matching for Unity architecture).
    /// Projects future character paths based on current velocity, stick input deflection,
    /// turning responsiveness, and braking deceleration curves.
    /// </summary>
    public class TrajectoryPredictor
    {
        public const int PointCount = 3;
        public static readonly float[] DefaultTimeHorizons = new float[] { 0.20f, 0.45f, 0.75f };

        public TrajectoryPoint[] Points { get; private set; }
        public LocomotionPhase CurrentPhase { get; private set; } = LocomotionPhase.Idle;
        public float TurnAngle { get; private set; } = 0f;
        public bool IsSharpCut { get; private set; } = false;
        public bool IsPlantStop { get; private set; } = false;

        private Vector3 smoothedDesiredVelocity = Vector3.zero;
        private Vector3 smoothedDesiredDirection = Vector3.forward;

        public TrajectoryPredictor()
        {
            Points = new TrajectoryPoint[PointCount];
            for (int i = 0; i < PointCount; i++)
            {
                Points[i] = new TrajectoryPoint(Vector3.zero, Vector3.forward, Vector3.zero, DefaultTimeHorizons[i]);
            }
        }

        /// <summary>
        /// Updates future trajectory predictions based on input and physical velocity.
        /// </summary>
        public void UpdateTrajectory(
            Transform rootTransform,
            Vector3 currentVelocity,
            Vector3 desiredVelocity,
            float maxSpeed,
            float responsiveness,
            float brakingDeceleration,
            float deltaTime)
        {
            if (rootTransform == null) return;

            // 1. Smooth desired velocity and heading using spring damper
            float smoothFactor = Mathf.Clamp01(responsiveness * deltaTime);
            smoothedDesiredVelocity = Vector3.Lerp(smoothedDesiredVelocity, desiredVelocity, smoothFactor);

            if (desiredVelocity.sqrMagnitude > 0.01f)
            {
                smoothedDesiredDirection = desiredVelocity.normalized;
            }

            Vector3 currentPlanarVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            Vector3 desiredPlanarVel = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);

            // 2. Evaluate turn angle between current movement heading and desired direction
            if (currentPlanarVel.sqrMagnitude > 0.5f && desiredPlanarVel.sqrMagnitude > 0.5f)
            {
                TurnAngle = Vector3.SignedAngle(currentPlanarVel.normalized, desiredPlanarVel.normalized, Vector3.up);
            }
            else
            {
                TurnAngle = 0f;
            }

            // 3. Classify locomotion phase
            bool wasMoving = currentPlanarVel.sqrMagnitude > 0.25f;
            bool wantsMove = desiredPlanarVel.sqrMagnitude > 0.25f;
            float absTurn = Mathf.Abs(TurnAngle);

            if (!wasMoving && wantsMove)
            {
                CurrentPhase = LocomotionPhase.StartTransition;
                IsSharpCut = false;
                IsPlantStop = false;
            }
            else if (wasMoving && !wantsMove)
            {
                CurrentPhase = LocomotionPhase.PlantStop;
                IsSharpCut = false;
                IsPlantStop = true;
            }
            else if (wasMoving && wantsMove && absTurn > 65f)
            {
                CurrentPhase = LocomotionPhase.SharpCut;
                IsSharpCut = true;
                IsPlantStop = false;
            }
            else if (wantsMove)
            {
                CurrentPhase = LocomotionPhase.ContinuousCycle;
                IsSharpCut = false;
                IsPlantStop = false;
            }
            else
            {
                CurrentPhase = LocomotionPhase.Idle;
                IsSharpCut = false;
                IsPlantStop = false;
            }

            // 4. Project future trajectory points at time horizons in local space
            Matrix4x4 worldToLocal = rootTransform.worldToLocalMatrix;

            for (int i = 0; i < PointCount; i++)
            {
                float t = DefaultTimeHorizons[i];
                Vector3 projectedWorldPos;
                Vector3 projectedWorldDir;
                Vector3 projectedWorldVel;

                if (CurrentPhase == LocomotionPhase.PlantStop)
                {
                    // Braking deceleration curve: d = v*t - 0.5*a*t^2
                    float stopTime = Mathf.Max(0.01f, currentPlanarVel.magnitude / Mathf.Max(1f, brakingDeceleration));
                    float clampedT = Mathf.Min(t, stopTime);
                    float remainingFrac = 1f - (clampedT / stopTime);

                    projectedWorldVel = currentPlanarVel * remainingFrac;
                    projectedWorldPos = rootTransform.position + (currentPlanarVel * clampedT) - (0.5f * currentPlanarVel.normalized * brakingDeceleration * clampedT * clampedT);
                    projectedWorldDir = rootTransform.forward;
                }
                else if (CurrentPhase == LocomotionPhase.SharpCut)
                {
                    // Arc trajectory curving toward desired heading
                    float slerpFactor = Mathf.Clamp01(t * 3.5f);
                    projectedWorldDir = Vector3.Slerp(currentPlanarVel.normalized, desiredPlanarVel.normalized, slerpFactor);
                    projectedWorldVel = projectedWorldDir * Mathf.Min(maxSpeed, currentPlanarVel.magnitude + (desiredPlanarVel.magnitude - currentPlanarVel.magnitude) * slerpFactor);
                    projectedWorldPos = rootTransform.position + (currentPlanarVel * (t * 0.4f)) + (projectedWorldVel * (t * 0.6f));
                }
                else
                {
                    // Continuous acceleration toward desired velocity
                    float blend = 1f - Mathf.Exp(-responsiveness * t);
                    projectedWorldVel = Vector3.Lerp(currentPlanarVel, desiredPlanarVel, blend);
                    projectedWorldPos = rootTransform.position + Vector3.Lerp(currentPlanarVel * t, desiredPlanarVel * t, blend);
                    projectedWorldDir = (projectedWorldVel.sqrMagnitude > 0.05f) ? projectedWorldVel.normalized : rootTransform.forward;
                }

                // Transform into local space relative to character
                Vector3 localPos = worldToLocal.MultiplyPoint3x4(projectedWorldPos);
                Vector3 localDir = worldToLocal.MultiplyVector(projectedWorldDir);
                Vector3 localVel = worldToLocal.MultiplyVector(projectedWorldVel);

                Points[i] = new TrajectoryPoint(localPos, localDir, localVel, t);
            }
        }
    }
}

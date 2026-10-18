using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Animation.MotionMatching
{
    public enum MocapLocomotionStyle
    {
        IdleMocapNatural = 0,
        WalkStart_Forward = 1,
        WalkStart_Turn90L = 2,
        WalkStart_Turn90R = 3,
        WalkStart_Turn180 = 4,
        WalkCycle_Believable = 5,
        WalkStop_PlantRight = 6,
        WalkStop_PlantLeft = 7,
        RunStart_Forward = 8,
        RunStart_Turn180 = 9,
        RunCycle_Believable = 10,
        RunSharpCut_90L = 11,
        RunSharpCut_90R = 12,
        RunSkidStop_Plant = 13,
        SprintCycle_HighVelocity = 14
    }

    [Serializable]
    public struct MocapPoseFeature
    {
        public MocapLocomotionStyle style;
        public float nominalSpeed;          // Typical speed in m/s
        public float leftFootHeight;        // 0 = planted, >0 = swing
        public float rightFootHeight;       // 0 = planted, >0 = swing
        public Vector3[] localTrajectoryPos;// Trajectory points at 0.2s, 0.45s, 0.75s
        public Vector3[] localTrajectoryDir;// Trajectory forward directions
        public bool isLooping;
        public float clipDuration;

        public MocapPoseFeature(
            MocapLocomotionStyle s,
            float speed,
            float lFoot,
            float rFoot,
            Vector3[] trajPos,
            Vector3[] trajDir,
            bool loop,
            float duration)
        {
            style = s;
            nominalSpeed = speed;
            leftFootHeight = lFoot;
            rightFootHeight = rFoot;
            localTrajectoryPos = trajPos;
            localTrajectoryDir = trajDir;
            isLooping = loop;
            clipDuration = duration;
        }
    }

    /// <summary>
    /// Believable Mocap Locomotion Database (MC Sample - Believable 3D Animations by Mocap Central architecture).
    /// Stores feature vectors, cost functions, and organic human movement parameters for
    /// realistic weight shifting, authentic starts, believable plant stops, and sharp cuts.
    /// </summary>
    public class BelievableMocapLocomotionDatabase
    {
        private readonly List<MocapPoseFeature> database = new List<MocapPoseFeature>();

        // Cost function weights
        public float weightTrajectoryPos = 1.0f;
        public float weightTrajectoryDir = 1.5f;
        public float weightSpeedMatch = 0.8f;
        public float weightPhaseContinuity = 0.5f;

        public BelievableMocapLocomotionDatabase()
        {
            BuildDatabase();
        }

        private void BuildDatabase()
        {
            database.Clear();

            // 1. Natural Mocap Idle (Subtle weight shift, stationary)
            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.IdleMocapNatural,
                speed: 0f,
                lFoot: 0f, rFoot: 0f,
                trajPos: new[] { Vector3.zero, Vector3.zero, Vector3.zero },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: true, duration: 4.2f));

            // 2. Walk Starts (Forward, Turn 90L, Turn 90R, Turn 180)
            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.WalkStart_Forward,
                speed: 3.2f,
                lFoot: 0f, rFoot: 0.15f,
                trajPos: new[] { new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, 1.1f), new Vector3(0f, 0f, 2.2f) },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: false, duration: 0.65f));

            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.WalkStart_Turn90L,
                speed: 3.0f,
                lFoot: 0.12f, rFoot: 0f,
                trajPos: new[] { new Vector3(-0.3f, 0f, 0.3f), new Vector3(-0.9f, 0f, 0.8f), new Vector3(-1.8f, 0f, 1.2f) },
                trajDir: new[] { new Vector3(-0.5f, 0f, 0.86f), new Vector3(-0.86f, 0f, 0.5f), Vector3.left },
                loop: false, duration: 0.72f));

            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.WalkStart_Turn90R,
                speed: 3.0f,
                lFoot: 0f, rFoot: 0.12f,
                trajPos: new[] { new Vector3(0.3f, 0f, 0.3f), new Vector3(0.9f, 0f, 0.8f), new Vector3(1.8f, 0f, 1.2f) },
                trajDir: new[] { new Vector3(0.5f, 0f, 0.86f), new Vector3(0.86f, 0f, 0.5f), Vector3.right },
                loop: false, duration: 0.72f));

            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.WalkStart_Turn180,
                speed: 2.8f,
                lFoot: 0f, rFoot: 0.18f,
                trajPos: new[] { new Vector3(0.2f, 0f, -0.2f), new Vector3(0.1f, 0f, -0.8f), new Vector3(0f, 0f, -1.8f) },
                trajDir: new[] { Vector3.right, new Vector3(0.7f, 0f, -0.7f), Vector3.back },
                loop: false, duration: 0.85f));

            // 3. Steady Walk Cycle (Believable mocap cadence ~ 5.5 m/s)
            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.WalkCycle_Believable,
                speed: 5.5f,
                lFoot: 0f, rFoot: 0.12f,
                trajPos: new[] { new Vector3(0f, 0f, 1.1f), new Vector3(0f, 0f, 2.5f), new Vector3(0f, 0f, 4.2f) },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: true, duration: 1.15f));

            // 4. Walk Stops (Right Foot plant / Left Foot plant)
            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.WalkStop_PlantRight,
                speed: 2.0f,
                lFoot: 0.05f, rFoot: 0f,
                trajPos: new[] { new Vector3(0f, 0f, 0.45f), new Vector3(0f, 0f, 0.70f), new Vector3(0f, 0f, 0.75f) },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: false, duration: 0.55f));

            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.WalkStop_PlantLeft,
                speed: 2.0f,
                lFoot: 0f, rFoot: 0.05f,
                trajPos: new[] { new Vector3(0f, 0f, 0.45f), new Vector3(0f, 0f, 0.70f), new Vector3(0f, 0f, 0.75f) },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: false, duration: 0.55f));

            // 5. Run Starts & Steady Run Cycle (~ 7.5 m/s)
            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.RunStart_Forward,
                speed: 6.5f,
                lFoot: 0f, rFoot: 0.22f,
                trajPos: new[] { new Vector3(0f, 0f, 0.9f), new Vector3(0f, 0f, 2.4f), new Vector3(0f, 0f, 4.5f) },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: false, duration: 0.50f));

            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.RunCycle_Believable,
                speed: 7.5f,
                lFoot: 0f, rFoot: 0.25f,
                trajPos: new[] { new Vector3(0f, 0f, 1.5f), new Vector3(0f, 0f, 3.4f), new Vector3(0f, 0f, 5.8f) },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: true, duration: 0.85f));

            // 6. Sharp Running Cuts (90L, 90R)
            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.RunSharpCut_90L,
                speed: 6.0f,
                lFoot: 0.28f, rFoot: 0f, // Plant right foot, cut hard left
                trajPos: new[] { new Vector3(-0.6f, 0f, 0.8f), new Vector3(-2.0f, 0f, 1.8f), new Vector3(-3.8f, 0f, 2.4f) },
                trajDir: new[] { new Vector3(-0.6f, 0f, 0.8f), Vector3.left, Vector3.left },
                loop: false, duration: 0.48f));

            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.RunSharpCut_90R,
                speed: 6.0f,
                lFoot: 0f, rFoot: 0.28f, // Plant left foot, cut hard right
                trajPos: new[] { new Vector3(0.6f, 0f, 0.8f), new Vector3(2.0f, 0f, 1.8f), new Vector3(3.8f, 0f, 2.4f) },
                trajDir: new[] { new Vector3(0.6f, 0f, 0.8f), Vector3.right, Vector3.right },
                loop: false, duration: 0.48f));

            // 7. Run Skid Stop & Plant
            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.RunSkidStop_Plant,
                speed: 3.5f,
                lFoot: 0f, rFoot: 0f, // Both feet dig into ground
                trajPos: new[] { new Vector3(0f, 0f, 0.8f), new Vector3(0f, 0f, 1.3f), new Vector3(0f, 0f, 1.4f) },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: false, duration: 0.52f));

            // 8. High Velocity Sprint (~ 9.5 m/s)
            database.Add(new MocapPoseFeature(
                MocapLocomotionStyle.SprintCycle_HighVelocity,
                speed: 9.5f,
                lFoot: 0f, rFoot: 0.32f,
                trajPos: new[] { new Vector3(0f, 0f, 1.9f), new Vector3(0f, 0f, 4.3f), new Vector3(0f, 0f, 7.2f) },
                trajDir: new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                loop: true, duration: 0.68f));
        }

        /// <summary>
        /// Evaluates Motion Matching cost function across candidate poses given current trajectory prediction.
        /// Finds the lowest cost motion frame.
        /// </summary>
        public MocapPoseFeature FindBestMatch(
            TrajectoryPredictor trajectory,
            float currentSpeed,
            MocapLocomotionStyle currentStyle)
        {
            float lowestCost = float.MaxValue;
            MocapPoseFeature bestMatch = database[0];

            foreach (var cand in database)
            {
                float cost = 0f;

                // 1. Trajectory Position Difference Cost
                for (int i = 0; i < TrajectoryPredictor.PointCount && i < cand.localTrajectoryPos.Length; i++)
                {
                    Vector3 diffPos = trajectory.Points[i].position - cand.localTrajectoryPos[i];
                    cost += diffPos.sqrMagnitude * weightTrajectoryPos;

                    // 2. Trajectory Direction Cost
                    float dotDir = Vector3.Dot(trajectory.Points[i].direction, cand.localTrajectoryDir[i]);
                    cost += (1f - Mathf.Clamp01(dotDir)) * weightTrajectoryDir * 4.0f;
                }

                // 3. Speed match cost
                float speedDiff = Mathf.Abs(currentSpeed - cand.nominalSpeed);
                cost += speedDiff * weightSpeedMatch;

                // 4. Locomotion phase affinity
                if (trajectory.CurrentPhase == LocomotionPhase.PlantStop &&
                    (cand.style == MocapLocomotionStyle.WalkStop_PlantRight ||
                     cand.style == MocapLocomotionStyle.WalkStop_PlantLeft ||
                     cand.style == MocapLocomotionStyle.RunSkidStop_Plant))
                {
                    cost -= 4.0f; // Strongly incentivize plant stop clips
                }
                else if (trajectory.CurrentPhase == LocomotionPhase.SharpCut &&
                    (cand.style == MocapLocomotionStyle.RunSharpCut_90L ||
                     cand.style == MocapLocomotionStyle.RunSharpCut_90R ||
                     cand.style == MocapLocomotionStyle.WalkStart_Turn180))
                {
                    cost -= 4.5f; // Strongly incentivize sharp cut clips
                }
                else if (trajectory.CurrentPhase == LocomotionPhase.StartTransition &&
                    (cand.style == MocapLocomotionStyle.WalkStart_Forward ||
                     cand.style == MocapLocomotionStyle.WalkStart_Turn90L ||
                     cand.style == MocapLocomotionStyle.WalkStart_Turn90R ||
                     cand.style == MocapLocomotionStyle.RunStart_Forward))
                {
                    cost -= 3.5f; // Incentivize natural step-off starts
                }

                // 5. Hysteresis bonus for current style to prevent rapid jitter
                if (cand.style == currentStyle && cand.isLooping)
                {
                    cost -= 0.6f;
                }

                if (cost < lowestCost)
                {
                    lowestCost = cost;
                    bestMatch = cand;
                }
            }

            return bestMatch;
        }
    }
}

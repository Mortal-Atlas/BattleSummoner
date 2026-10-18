using System;
using UnityEngine;

namespace BattleSummoner.Customization
{
    /// <summary>
    /// AnimeBreathingController produces living, organic anime idle breathing animation:
    /// - Expanding and contracting the torso (ribcage X/Z expansion on inhale, relaxation on exhale).
    /// - Head bobbing (subtle vertical translation) and nodding (gentle pitch rotation).
    /// - Neck flexion synchronized with lung expansion.
    /// - Subtle shoulder rise/fall and spine sway.
    /// - Works on humanoid armatures, skinned meshes, or procedural character rigs.
    /// </summary>
    [ExecuteAlways]
    public class AnimeBreathingController : MonoBehaviour
    {
        [Header("Bone References (Auto-Detected If Null)")]
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform neckBone;
        [SerializeField] private Transform chestBone;
        [SerializeField] private Transform spineBone;
        [SerializeField] private Transform leftShoulderBone;
        [SerializeField] private Transform rightShoulderBone;

        [Header("Breathing Dynamics")]
        [Tooltip("Breaths per minute")]
        [Range(10f, 35f)] public float breathingRate = 18f;

        [Header("Torso Expansion / Contraction")]
        [Range(0f, 0.15f)] public float torsoExpandX = 0.035f; // Lateral ribcage expansion
        [Range(0f, 0.15f)] public float torsoExpandZ = 0.048f; // Forward chest expansion
        [Range(0f, 0.10f)] public float torsoExpandY = 0.015f; // Vertical lung lift

        [Header("Head Nodding & Bobbing")]
        [Tooltip("Head pitch nod in degrees")]
        [Range(0f, 6f)] public float headNodAngle = 1.8f;
        [Tooltip("Head vertical bob in meters")]
        [Range(0f, 0.03f)] public float headBobDistance = 0.007f;
        [Tooltip("Neck tilt angle in degrees")]
        [Range(0f, 4f)] public float neckTiltAngle = 0.9f;

        [Header("Shoulder Motion")]
        [Tooltip("Shoulder rise distance in meters")]
        [Range(0f, 0.02f)] public float shoulderRise = 0.006f;

        // Internal State
        private float breathPhase = 0f;
        private Vector3 initialChestScale = Vector3.one;
        private Vector3 initialHeadPos;
        private Quaternion initialHeadRot = Quaternion.identity;
        private Quaternion initialNeckRot = Quaternion.identity;
        private Vector3 initialLeftShoulderPos;
        private Vector3 initialRightShoulderPos;
        private bool initialized = false;

        private void Awake()
        {
            AutoDetectBones();
            CacheInitialTransforms();
        }

        private void OnEnable()
        {
            if (!initialized)
            {
                AutoDetectBones();
                CacheInitialTransforms();
            }
        }

        public void AutoDetectBones()
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                string n = t.name.ToLower();
                if (headBone == null && (n == "head" || n.Contains("headbone") || n.EndsWith("_head"))) headBone = t;
                else if (neckBone == null && (n == "neck" || n.Contains("neckbone") || n.EndsWith("_neck"))) neckBone = t;
                else if (chestBone == null && (n == "chest" || n == "upperchest" || n.Contains("chestbone") || n.Contains("chestarmor"))) chestBone = t;
                else if (spineBone == null && (n == "spine" || n.Contains("torsobone") || n.EndsWith("_spine"))) spineBone = t;
                else if (leftShoulderBone == null && (n.Contains("shoulder_l") || n.Contains("leftshoulder") || n.Contains("clavicle_l"))) leftShoulderBone = t;
                else if (rightShoulderBone == null && (n.Contains("shoulder_r") || n.Contains("rightshoulder") || n.Contains("clavicle_r"))) rightShoulderBone = t;
            }
        }

        public void CacheInitialTransforms()
        {
            if (chestBone != null) initialChestScale = chestBone.localScale;
            if (headBone != null)
            {
                initialHeadPos = headBone.localPosition;
                initialHeadRot = headBone.localRotation;
            }
            if (neckBone != null) initialNeckRot = neckBone.localRotation;
            if (leftShoulderBone != null) initialLeftShoulderPos = leftShoulderBone.localPosition;
            if (rightShoulderBone != null) initialRightShoulderPos = rightShoulderBone.localPosition;

            initialized = true;
        }

        private void Update()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            float freq = (breathingRate / 60f) * Mathf.PI * 2f;
            breathPhase += dt * freq;
            if (breathPhase > Mathf.PI * 200f) breathPhase -= Mathf.PI * 200f;

            // Inhale / Exhale smooth curve:
            // Sinusoidal curve shaped with slight ease for organic lung expansion
            float rawSin = Mathf.Sin(breathPhase);
            float inhaleProgress = Mathf.SmoothStep(0f, 1f, (rawSin + 1f) * 0.5f); // 0 (empty) to 1 (full lungs)
            float breathDelta = (inhaleProgress - 0.5f) * 2f; // -1 to 1

            // 1. Torso Expansion & Contraction
            if (chestBone != null)
            {
                chestBone.localScale = new Vector3(
                    initialChestScale.x * (1f + breathDelta * torsoExpandX),
                    initialChestScale.y * (1f + breathDelta * torsoExpandY),
                    initialChestScale.z * (1f + breathDelta * torsoExpandZ)
                );
            }

            // 2. Head Nodding & Bobbing
            if (headBone != null)
            {
                // Head nods slightly down on exhale, lifts slightly on inhale
                float nodPitch = -breathDelta * headNodAngle;
                headBone.localRotation = initialHeadRot * Quaternion.Euler(nodPitch, 0f, 0f);

                // Head bobs vertically
                float bobY = breathDelta * headBobDistance;
                headBone.localPosition = initialHeadPos + new Vector3(0f, bobY, 0f);
            }

            // 3. Neck Flexion
            if (neckBone != null)
            {
                float neckPitch = -breathDelta * neckTiltAngle;
                neckBone.localRotation = initialNeckRot * Quaternion.Euler(neckPitch, 0f, 0f);
            }

            // 4. Shoulder Elevation
            if (leftShoulderBone != null)
            {
                float lift = Mathf.Max(0f, breathDelta) * shoulderRise;
                leftShoulderBone.localPosition = initialLeftShoulderPos + new Vector3(0f, lift, 0f);
            }
            if (rightShoulderBone != null)
            {
                float lift = Mathf.Max(0f, breathDelta) * shoulderRise;
                rightShoulderBone.localPosition = initialRightShoulderPos + new Vector3(0f, lift, 0f);
            }
        }
    }
}

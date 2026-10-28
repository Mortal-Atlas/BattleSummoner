using System;
using System.Collections;
using UnityEngine;

namespace BattleSummoner.Combat
{
    [RequireComponent(typeof(Animator))]
    public class ProceduralSlashIKAndAnimatorBridge : MonoBehaviour
    {
        [Header("Core Rig & Socket References")]
        public Animator characterAnimator;
        public Transform rightHandWeaponSocket;
        public Transform leftHandOffHandSocket;
        public TrailRenderer rightBladeTrail3D;
        public TrailRenderer leftBladeTrail3D;

        [Header("1:1 Somatic Swipe-to-Spine Alignment")]
        [Range(0f, 1f)] public float proceduralSlashIKWeight = 0.85f;
        public float spineRollResponsiveness = 18f;
        public float hitStopFreezeDuration = 0.065f;

        [Header("Live Combat Telemetry")]
        public bool isPassiveShieldBlocking = false;
        public bool isDualWielding = false;
        public bool hasShieldEquipped = false;
        public float currentSwipeAngleDeg = 0f;
        public float currentWeaponThermalC = 22f; // Heats up on rapid slashes; quenches in water/ice!

        private Transform _spineBone;
        private Transform _chestBone;
        private Transform _rightUpperArm;
        private Transform _leftUpperArm;

        private float _activeSlashPoseTimer = 0f;
        private Quaternion _targetChestSlashOffset = Quaternion.identity;
        private Quaternion _smoothedChestSlashOffset = Quaternion.identity;
        private Vector3 _lastFramePosition;

        // Cached Animator Hashes (Zero GC Allocation)
        private static readonly int HashMoveSpeed       = Animator.StringToHash("MoveSpeed");
        private static readonly int HashSlashAngle      = Animator.StringToHash("SlashAngleNormalized");
        private static readonly int HashIsShieldBlock   = Animator.StringToHash("IsShieldBlocking");
        private static readonly int HashTrigSlashRight  = Animator.StringToHash("Trig_SlashRight");
        private static readonly int HashTrigSlashLeft   = Animator.StringToHash("Trig_SlashLeft");
        private static readonly int HashTrigHeavyCleave = Animator.StringToHash("Trig_HeavyCleave");
        private static readonly int HashTrigParry       = Animator.StringToHash("Trig_Parry");
        private static readonly int HashTrigCastSpell   = Animator.StringToHash("Trig_CastSpell");
        private static readonly int HashTrigHitReact    = Animator.StringToHash("Trig_HitReact");

        private void Awake()
        {
            if (characterAnimator == null) characterAnimator = GetComponent<Animator>();

            if (characterAnimator != null && characterAnimator.isHuman)
            {
                _spineBone     = characterAnimator.GetBoneTransform(HumanBodyBones.Spine);
                _chestBone     = characterAnimator.GetBoneTransform(HumanBodyBones.Chest) ?? _spineBone;
                _rightUpperArm = characterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                _leftUpperArm  = characterAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            }

            Ensure3DWeaponTrails();
            _lastFramePosition = transform.position;
        }

        private void Ensure3DWeaponTrails()
        {
            if (rightHandWeaponSocket != null && rightBladeTrail3D == null)
            {
                rightBladeTrail3D = CreateBladeTipTrail(rightHandWeaponSocket, "RightBladeTip_Trail", new Color(0.95f, 0.85f, 0.55f, 0.9f));
            }
            if (leftHandOffHandSocket != null && leftBladeTrail3D == null)
            {
                leftBladeTrail3D = CreateBladeTipTrail(leftHandOffHandSocket, "LeftBladeTip_Trail", new Color(0.45f, 0.85f, 1.0f, 0.9f));
            }
        }

        private TrailRenderer CreateBladeTipTrail(Transform socket, string trailName, Color baseColor)
        {
            GameObject tipObj = new GameObject(trailName);
            tipObj.transform.SetParent(socket, false);
            tipObj.transform.localPosition = new Vector3(0f, 0f, 0.85f); // 0.85m out at the blade tip

            TrailRenderer tr = tipObj.AddComponent<TrailRenderer>();
            tr.time = 0.18f;
            tr.minVertexDistance = 0.03f;
            tr.startWidth = 0.22f;
            tr.endWidth = 0.01f;
            tr.emitting = false;

            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (trailShader != null)
            {
                tr.material = new Material(trailShader) { color = baseColor };
            }
            return tr;
        }

        private void Update()
        {
            // 1. Update Locomotion BlendTree Speed
            float planarSpeed = Vector3.ProjectOnPlane(transform.position - _lastFramePosition, Vector3.up).magnitude / Mathf.Max(0.0001f, Time.deltaTime);
            _lastFramePosition = transform.position;

            float normalizedMove = Mathf.Clamp01(planarSpeed / 7.5f);
            if (characterAnimator != null)
            {
                characterAnimator.SetFloat(HashMoveSpeed, normalizedMove, 0.08f, Time.deltaTime);
            }

            // 2. Passive Shield Blocking Rule:
            // Whenever a Shield is equipped in the Off-Hand and the player is NOT mid-slash, automatically raise the shield!
            if (_activeSlashPoseTimer > 0f)
            {
                _activeSlashPoseTimer -= Time.deltaTime;
                isPassiveShieldBlocking = false;
            }
            else
            {
                isPassiveShieldBlocking = isPassiveShieldBlocking && hasShieldEquipped;
                if (rightBladeTrail3D != null) rightBladeTrail3D.emitting = false;
                if (leftBladeTrail3D != null)  leftBladeTrail3D.emitting = false;
            }

            if (characterAnimator != null)
            {
                characterAnimator.SetBool(HashIsShieldBlock, isPassiveShieldBlocking);
            }

            // 3. Ambient Blade Cooling
            currentWeaponThermalC = Mathf.MoveTowards(currentWeaponThermalC, 22f, Time.deltaTime * 12f);
        }

        private void LateUpdate()
        {
            // Apply 1:1 Swipe-Plane Roll to Chest Bone so the 3D swing matches the exact finger swipe vector
            Quaternion goal = _activeSlashPoseTimer > 0f ? _targetChestSlashOffset : Quaternion.identity;
            _smoothedChestSlashOffset = Quaternion.Slerp(_smoothedChestSlashOffset, goal, Time.deltaTime * spineRollResponsiveness);

            if (_chestBone != null && _activeSlashPoseTimer > 0f)
            {
                _chestBone.localRotation = _chestBone.localRotation * _smoothedChestSlashOffset;
            }
        }

        // =====================================================================
        // PUBLIC COMBAT TRIGGERS (CALLED BY TOUCH CANVAS & SPELL ENGINE)
        // =====================================================================

        /// <summary>
        /// Triggered when Finger 0 (Main Hand) or Finger 1 (Dual-Wield Off-Hand) executes a directional swipe.
        /// Aligns the 3D swing plane to swipeAngleDeg (-180 to +180) and fires the UpperBody Animator state.
        /// </summary>
        public void ExecuteDirectionalBladeSlash(int fingerIndex, float swipeAngleDeg, float swipeSpeedPxPerSec, bool isChargedWeaponArt)
        {
            currentSwipeAngleDeg = swipeAngleDeg;
            _activeSlashPoseTimer = isChargedWeaponArt ? 0.48f : 0.28f;

            // Heat up the blade slightly on every high-velocity slash
            currentWeaponThermalC = Mathf.Min(650f, currentWeaponThermalC + (isChargedWeaponArt ? 65f : 22f));
            UpdateThermalBladeTrailColor();

            // Convert 2D screen swipe angle into 3D torso roll/pitch alignment
            float rollAngle = Mathf.Clamp(swipeAngleDeg * 0.35f, -45f, 45f);
            _targetChestSlashOffset = Quaternion.Euler(0f, 0f, rollAngle);

            if (characterAnimator != null)
            {
                characterAnimator.SetFloat(HashSlashAngle, Mathf.Clamp(swipeAngleDeg / 180f, -1f, 1f));

                if (isChargedWeaponArt)
                {
                    characterAnimator.SetTrigger(HashTrigHeavyCleave);
                }
                else if (fingerIndex == 1 && isDualWielding)
                {
                    characterAnimator.SetTrigger(HashTrigSlashLeft);
                    if (leftBladeTrail3D != null) leftBladeTrail3D.emitting = true;
                }
                else
                {
                    characterAnimator.SetTrigger(HashTrigSlashRight);
                    if (rightBladeTrail3D != null) rightBladeTrail3D.emitting = true;
                }
            }
        }

        /// <summary>
        /// Triggered when two fingers tap simultaneously (< 0.12s hold, < 25px drift).
        /// </summary>
        public void ExecuteUniversalTwoFingerParry()
        {
            _activeSlashPoseTimer = 0.32f;
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(HashTrigParry);
            }
        }

        /// <summary>
        /// Triggered when the player flicks the [CAST] button or completes a Circle-First Rune.
        /// </summary>
        public void ExecuteRuneSpellCastAnimation()
        {
            _activeSlashPoseTimer = 0.38f;
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger(HashTrigCastSpell);
            }
        }

        /// <summary>
        /// Freezes the character's Animator for 65ms on heavy blade impact or Perfect Parry for tactile crunch!
        /// </summary>
        public void TriggerTactileHitStop()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HitStopRoutine(hitStopFreezeDuration));
            }
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            if (characterAnimator == null) yield break;
            float prevSpeed = characterAnimator.speed;
            characterAnimator.speed = 0.02f;
            yield return new WaitForSecondsRealtime(duration);
            characterAnimator.speed = prevSpeed;
        }

        private void UpdateThermalBladeTrailColor()
        {
            if (rightBladeTrail3D == null || rightBladeTrail3D.material == null) return;
            float t = Mathf.InverseLerp(80f, 550f, currentWeaponThermalC);
            Color coldSteel = new Color(0.92f, 0.95f, 1.0f, 0.85f);
            Color whiteHot  = new Color(1.0f, 0.38f, 0.08f, 0.98f);
            rightBladeTrail3D.material.color = Color.Lerp(coldSteel, whiteHot, t);
        }
    }
}


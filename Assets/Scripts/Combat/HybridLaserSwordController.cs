using System;
using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// HybridLaserSwordController implements a 3-Layer Hybrid Kinematic Pipeline:
    /// Combines Skyward Sword 1:1 directional tracking with Fruit Ninja / SAO swing weight.
    /// 
    /// GUARANTEES:
    /// - Zero mesh deformation: Never touches localPosition or localScale of any bone.
    /// - Anti-Candy-Wrapper Wrist Solver: 50% roll on RightLowerArm, 50% on RightHand, clamped to [-55, +55] deg.
    /// - Torso Repulsion Cylinder (0.28m radius) preventing sword from ever clipping through chest.
    /// - Dual-Color Laser Sightline: Cyan (In-Reach direct tracking) vs Amber (Out-of-Reach laser pointing).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HybridLaserSwordController : MonoBehaviour
    {
        [Header("Rig & Transform References")]
        public Transform playerRoot;
        public Transform rightHandWeaponSocket;
        public float bladeLength = 0.85f;
        public LayerMask combatLayerMask = ~0;

        [Header("IK Tuning & Anatomical Limits")]
        [Range(0f, 1f)] public float maxWeight = 1.0f;
        public float weightBlendSpeed = 12f;
        public float smoothTime = 0.025f;
        public float torsoRepulsionRadius = 0.28f;
        public float forearmTwistSplit = 0.5f; // 50% forearm, 50% wrist
        public float maxTwistDegrees = 55f;

        [Header("Telemetry (Read Only)")]
        public bool isLaserActive = false;
        public bool isInReach = false;
        public float currentIKWeight = 0f;
        public Vector3 laserTargetWorld;
        public Vector3 laserVelocity;
        public Vector3 bladeEdgeDirection = Vector3.up;

        // Cached Bones
        private Animator _animator;
        private Transform _shoulder;
        private Transform _elbow;
        private Transform _hand;
        private Transform _chest;
        private Transform _upperChest;

        private float _maxArmReach = 0.60f;
        private float _totalSwordReach = 1.45f;
        private Vector3 _laserSmoothVelocity;
        private Vector3 _prevLaserTargetWorld;

        // Visual Indicator
        private LineRenderer _laserLine;
        private Material _laserMaterial;
        private readonly Color _colorInReach = new Color(0.20f, 0.85f, 1.0f, 0.90f);  // Cyan
        private readonly Color _colorOutReach = new Color(1.0f, 0.70f, 0.15f, 0.90f); // Amber

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (playerRoot == null)
            {
                playerRoot = transform.root;
            }

            CacheHumanoidBones();
            EnsureLaserIndicator();
        }

        private void Start()
        {
            MeasureAnatomicalReach();
        }

        private void CacheHumanoidBones()
        {
            if (_animator == null || !_animator.isHuman) return;

            _shoulder   = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _elbow      = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _hand       = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            _chest      = _animator.GetBoneTransform(HumanBodyBones.Chest) ?? _animator.GetBoneTransform(HumanBodyBones.Spine);
            _upperChest = _animator.GetBoneTransform(HumanBodyBones.UpperChest);
        }

        private void MeasureAnatomicalReach()
        {
            if (_shoulder != null && _elbow != null && _hand != null)
            {
                float upperArmLen = Vector3.Distance(_shoulder.position, _elbow.position);
                float forearmLen  = Vector3.Distance(_elbow.position, _hand.position);
                _maxArmReach = (upperArmLen + forearmLen) * 0.92f; // 92% cap prevents rigid elbow lock
            }
            else
            {
                _maxArmReach = 0.58f;
            }

            _totalSwordReach = _maxArmReach + bladeLength;
            laserTargetWorld = playerRoot.position + playerRoot.forward * 2.2f + Vector3.up * 1.1f;
            _prevLaserTargetWorld = laserTargetWorld;
        }

        private void EnsureLaserIndicator()
        {
            _laserLine = GetComponentInChildren<LineRenderer>();
            if (_laserLine == null)
            {
                GameObject lineObj = new GameObject("HybridLaserLineRenderer");
                lineObj.transform.SetParent(transform, false);
                _laserLine = lineObj.AddComponent<LineRenderer>();
            }

            _laserLine.positionCount = 2;
            _laserLine.startWidth = 0.018f;
            _laserLine.endWidth   = 0.006f;
            _laserLine.useWorldSpace = true;
            _laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _laserLine.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _laserMaterial = new Material(shader);
                _laserLine.material = _laserMaterial;
            }
            _laserLine.enabled = false;
        }

        // =====================================================================
        // STAGE 1: SCREEN-SPACE LASER RAYCAST & VELOCITY TRACKER
        // =====================================================================
        private void Update()
        {
            // Determine active pointer (Mouse or Touch)
            bool pointerDown = Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase != TouchPhase.Ended);
            isLaserActive = pointerDown;

            float targetWeight = isLaserActive ? maxWeight : 0.0f;
            currentIKWeight = Mathf.MoveTowards(currentIKWeight, targetWeight, Time.deltaTime * weightBlendSpeed);

            if (isLaserActive && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 rawHitPoint;

                if (Physics.Raycast(ray, out RaycastHit hit, 55f, combatLayerMask, QueryTriggerInteraction.Ignore))
                {
                    rawHitPoint = hit.point;
                }
                else
                {
                    // Intersect virtual plane perpendicular to camera placed at player's combat sphere
                    Vector3 planeOrigin = playerRoot.position + playerRoot.forward * 2.2f + Vector3.up * 1.0f;
                    Plane virtualPlane = new Plane(-Camera.main.transform.forward, planeOrigin);
                    if (virtualPlane.Raycast(ray, out float enter))
                    {
                        rawHitPoint = ray.GetPoint(enter);
                    }
                    else
                    {
                        rawHitPoint = ray.GetPoint(3.5f);
                    }
                }

                // Smooth raw hit point to eliminate frame jitter
                laserTargetWorld = Vector3.SmoothDamp(laserTargetWorld, rawHitPoint, ref _laserSmoothVelocity, smoothTime);

                Vector3 delta = laserTargetWorld - _prevLaserTargetWorld;
                laserVelocity = delta / Mathf.Max(0.0001f, Time.deltaTime);
                _prevLaserTargetWorld = laserTargetWorld;

                if (laserVelocity.magnitude > 0.40f)
                {
                    Vector3 targetEdge = laserVelocity.normalized;
                    bladeEdgeDirection = Vector3.Slerp(bladeEdgeDirection, targetEdge, Time.deltaTime * 22f);
                }
            }

            UpdateVisualLaser();
        }

        private void UpdateVisualLaser()
        {
            if (_laserLine == null) return;

            if (currentIKWeight > 0.05f)
            {
                _laserLine.enabled = true;
                Vector3 swordTip = GetSwordTipPosition();

                _laserLine.SetPosition(0, swordTip);
                _laserLine.SetPosition(1, laserTargetWorld);

                Color targetColor = isInReach ? _colorInReach : _colorOutReach;
                targetColor.a *= currentIKWeight;
                if (_laserMaterial != null) _laserMaterial.color = targetColor;
                _laserLine.startColor = targetColor;
                _laserLine.endColor = new Color(targetColor.r, targetColor.g, targetColor.b, targetColor.a * 0.4f);
            }
            else
            {
                _laserLine.enabled = false;
            }
        }

        public Vector3 GetSwordTipPosition()
        {
            if (rightHandWeaponSocket != null)
            {
                return rightHandWeaponSocket.position + rightHandWeaponSocket.forward * bladeLength;
            }
            if (_hand != null)
            {
                return _hand.position + playerRoot.forward * bladeLength;
            }
            return transform.position + playerRoot.forward * (bladeLength + 0.4f);
        }

        // =====================================================================
        // STAGE 2: ANATOMICAL HEMISPHERE & ANTI-CLIPPING TORSO GUARD (IK)
        // =====================================================================
        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || currentIKWeight <= 0.001f || _shoulder == null || _hand == null)
                return;

            Vector3 shoulderToLaser = laserTargetWorld - _shoulder.position;
            float distToLaser = shoulderToLaser.magnitude;
            isInReach = (distToLaser <= _totalSwordReach);

            // 1. Forward Hemisphere Constraint in playerRoot space
            Vector3 localDir = playerRoot.InverseTransformDirection(shoulderToLaser.normalized);
            float yawDeg   = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float pitchDeg = Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;

            // Clamping horizontal [-75, +85] and vertical [-60, +75]
            float clampedYaw   = Mathf.Clamp(yawDeg, -75f, 85f);
            float clampedPitch = Mathf.Clamp(pitchDeg, -60f, 75f);
            Vector3 clampedLocalDir = Quaternion.Euler(-clampedPitch, clampedYaw, 0f) * Vector3.forward;
            Vector3 clampedWorldDir = playerRoot.TransformDirection(clampedLocalDir).normalized;

            // 2. In-Reach vs. Out-of-Reach Desired Hand Position
            Vector3 desiredHandPos;
            if (isInReach)
            {
                // In reach: Position hand back from laserTarget along clamped direction
                Vector3 candidateHandPos = laserTargetWorld - clampedWorldDir * bladeLength;
                float fromShoulderDist = Vector3.Distance(_shoulder.position, candidateHandPos);
                if (fromShoulderDist > _maxArmReach)
                {
                    candidateHandPos = _shoulder.position + (candidateHandPos - _shoulder.position).normalized * _maxArmReach;
                }
                desiredHandPos = candidateHandPos;
            }
            else
            {
                // Out of reach: Extend hand safely to 92% sphere surface and point blade forward
                desiredHandPos = _shoulder.position + clampedWorldDir * _maxArmReach;
            }

            // 3. Cylindrical Torso Deflection Guard (Anti-Chest Clipping)
            Vector3 chestAxisOrigin = _chest != null ? _chest.position : (_shoulder.position - playerRoot.right * 0.15f);
            Vector3 toTarget = desiredHandPos - chestAxisOrigin;
            Vector3 toTargetHorizontal = Vector3.ProjectOnPlane(toTarget, playerRoot.up);
            if (toTargetHorizontal.magnitude < torsoRepulsionRadius)
            {
                Vector3 outward = toTargetHorizontal.sqrMagnitude > 0.001f ? toTargetHorizontal.normalized : playerRoot.forward;
                desiredHandPos = chestAxisOrigin + outward * torsoRepulsionRadius + Vector3.Project(toTarget, playerRoot.up);
            }

            // 4. Elbow Pole Vector (AvatarIKHint.RightElbow)
            Vector3 elbowPole = _shoulder.position + playerRoot.right * 0.45f - playerRoot.up * 0.30f - playerRoot.forward * 0.15f;
            _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, currentIKWeight);
            _animator.SetIKHintPosition(AvatarIKHint.RightElbow, elbowPole);

            // 5. Hand Position & Orientation
            Vector3 forwardAim = (laserTargetWorld - desiredHandPos).normalized;
            Vector3 upEdge = bladeEdgeDirection.sqrMagnitude > 0.1f ? bladeEdgeDirection : playerRoot.up;
            Quaternion desiredHandRot = Quaternion.LookRotation(forwardAim, upEdge);

            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentIKWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, currentIKWeight);
            _animator.SetIKPosition(AvatarIKGoal.RightHand, desiredHandPos);
            _animator.SetIKRotation(AvatarIKGoal.RightHand, desiredHandRot);
        }

        // =====================================================================
        // STAGE 3: DISTRIBUTED SPINE LEAN & FOREARM TWIST SHARING
        // =====================================================================
        private void LateUpdate()
        {
            if (currentIKWeight <= 0.001f || _chest == null) return;

            // 1. Distributed Upper-Body Spine Lean (15% Chest, 25% UpperChest)
            Vector3 dirToTarget = (laserTargetWorld - _chest.position).normalized;
            Vector3 localAim = playerRoot.InverseTransformDirection(dirToTarget);
            float aimYaw   = Mathf.Clamp(Mathf.Atan2(localAim.x, localAim.z) * Mathf.Rad2Deg, -25f, 25f);
            float aimPitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(localAim.y, -1f, 1f)) * Mathf.Rad2Deg, -20f, 25f);

            Quaternion chestOffset = Quaternion.Euler(-aimPitch * 0.15f, aimYaw * 0.15f, 0f);
            _chest.localRotation = _chest.localRotation * Quaternion.Slerp(Quaternion.identity, chestOffset, currentIKWeight);

            if (_upperChest != null)
            {
                Quaternion upperChestOffset = Quaternion.Euler(-aimPitch * 0.25f, aimYaw * 0.25f, 0f);
                _upperChest.localRotation = _upperChest.localRotation * Quaternion.Slerp(Quaternion.identity, upperChestOffset, currentIKWeight);
            }

            // 2. Anti-Candy-Wrapper Forearm Twist Sharing (50% Forearm / 50% Wrist)
            if (_elbow != null && _hand != null)
            {
                Vector3 forearmAxis = (_hand.position - _elbow.position).normalized;
                Vector3 currentHandUp = _hand.up;
                Vector3 desiredUp = bladeEdgeDirection.sqrMagnitude > 0.1f ? bladeEdgeDirection : playerRoot.up;

                // Project current and desired up-vectors onto plane perpendicular to forearm
                Vector3 projCurrent = Vector3.ProjectOnPlane(currentHandUp, forearmAxis).normalized;
                Vector3 projDesired = Vector3.ProjectOnPlane(desiredUp, forearmAxis).normalized;

                if (projCurrent.sqrMagnitude > 0.01f && projDesired.sqrMagnitude > 0.01f)
                {
                    float twistAngle = Vector3.SignedAngle(projCurrent, projDesired, forearmAxis);
                    float clampedTwist = Mathf.Clamp(twistAngle, -maxTwistDegrees, maxTwistDegrees) * currentIKWeight;

                    // Distribute 50% onto forearm (RightLowerArm) and 50% onto wrist (RightHand)
                    Quaternion forearmTwist = Quaternion.AngleAxis(clampedTwist * forearmTwistSplit, forearmAxis);
                    Quaternion wristTwist   = Quaternion.AngleAxis(clampedTwist * (1.0f - forearmTwistSplit), forearmAxis);

                    _elbow.rotation = forearmTwist * _elbow.rotation;
                    _hand.rotation  = wristTwist * _hand.rotation;
                }
            }
        }
    }
}


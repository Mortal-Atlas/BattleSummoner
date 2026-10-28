using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// Blends full-body Universal Animation Library 2 animations while enforcing an
    /// absolute, non-deforming analytic 2-bone rotational override on the Right Sword Arm
    /// in LateUpdate() based on where the cursor/laser pointer lands in 3D world space.
    /// </summary>
    [DefaultExecutionOrder(100)] // Runs strictly AFTER Animator and PCModeDungeonTestingController
    public class WorldSpaceSwordHandOverrideSolver : MonoBehaviour
    {
        [Header("Animator & Rig References")]
        public Animator characterAnimator;
        public Transform rightHandWeaponSocket;
        public Transform swordTipTransform;
        public Camera activeCamera;

        [Header("Sword & Reach Geometry (Zero Deformation Guardrails)")]
        [Tooltip("Distance from RightHand wrist pivot to the tip of the sword blade.")]
        public float swordBladeLength = 1.05f;
        [Tooltip("Local axis of RightHand/WeaponSocket that points along the blade toward the tip.")]
        public Vector3 localBladeForwardAxis = Vector3.forward;
        [Tooltip("Local axis of RightHand/WeaponSocket aligned with the sharp cutting edge.")]
        public Vector3 localBladeEdgeAxis = Vector3.up;
        [Range(0.85f, 0.98f)]
        public float maxAnatomicalExtensionRatio = 0.94f; // Never locks elbow at 100% or stretches bones

        [Header("World-Space Laser Projection")]
        public LayerMask worldRaycastMask = ~0;
        public float fallbackCombatPlaneDistance = 2.4f;
        public float pointerSmoothTime = 0.02f;
        public bool alwaysOverrideSwordHand = true; // True = always tracks mouse; False = only while holding click

        [Header("Upper Torso Follow & Anti-Clipping")]
        [Range(0f, 0.5f)] public float chestLeanWeight = 0.22f;
        public float maxChestLeanDegrees = 24f;
        public float torsoExclusionRadius = 0.27f;
        public float maxWristForearmTwistDegrees = 55f;

        [Header("Live Telemetry (Read-Only)")]
        public Vector3 laserWorldPoint;
        public bool isTargetInSwordReach;
        public float currentOverrideWeight = 1f;

        private Transform _spine;
        private Transform _chest;
        private Transform _upperChest;
        private Transform _rightShoulder; // RightUpperArm
        private Transform _rightElbow;    // RightLowerArm
        private Transform _rightHand;     // RightHand

        private float _upperArmLength;
        private float _forearmLength;
        private float _maxArmReach;

        // Cached bone axis rotations relative to bone-to-child vector
        private Quaternion _upperArmAimCorrection = Quaternion.identity;
        private Quaternion _forearmAimCorrection = Quaternion.identity;

        private Vector3 _smoothedLaserPoint;
        private Vector3 _laserSmoothVelocity;
        private Vector3 _prevLaserPoint;
        private Vector3 _swipeEdgeDirection = Vector3.up;
        private LineRenderer _laserPointerLine;

        private void Start()
        {
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
            if (activeCamera == null) activeCamera = Camera.main;

            InitializeHumanoidBonesAndRestLengths();
            SetupLaserVisualIndicator();
        }

        public void InitializeHumanoidBonesAndRestLengths()
        {
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator == null || !characterAnimator.isHuman) return;

            _spine         = characterAnimator.GetBoneTransform(HumanBodyBones.Spine);
            _chest         = characterAnimator.GetBoneTransform(HumanBodyBones.Chest) ?? _spine;
            _upperChest    = characterAnimator.GetBoneTransform(HumanBodyBones.UpperChest) ?? _chest;
            _rightShoulder = characterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _rightElbow    = characterAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _rightHand     = characterAnimator.GetBoneTransform(HumanBodyBones.RightHand);

            if (_rightShoulder != null && _rightElbow != null && _rightHand != null)
            {
                // Measure exact skeletal bone lengths once so we NEVER stretch or deform the mesh
                _upperArmLength = Vector3.Distance(_rightShoulder.position, _rightElbow.position);
                _forearmLength  = Vector3.Distance(_rightElbow.position, _rightHand.position);
                _maxArmReach    = (_upperArmLength + _forearmLength) * maxAnatomicalExtensionRatio;

                // Record how each bone's native rotation aligns with the vector to its child joint
                Vector3 dirToElbow = (_rightElbow.position - _rightShoulder.position).normalized;
                if (dirToElbow.sqrMagnitude > 0.001f)
                    _upperArmAimCorrection = Quaternion.Inverse(Quaternion.LookRotation(dirToElbow, transform.up)) * _rightShoulder.rotation;

                Vector3 dirToHand = (_rightHand.position - _rightElbow.position).normalized;
                if (dirToHand.sqrMagnitude > 0.001f)
                    _forearmAimCorrection = Quaternion.Inverse(Quaternion.LookRotation(dirToHand, transform.up)) * _rightElbow.rotation;
            }

            if (rightHandWeaponSocket == null && _rightHand != null)
            {
                rightHandWeaponSocket = _rightHand.Find("WeaponSocket_R") ?? _rightHand;
            }
        }

        private void SetupLaserVisualIndicator()
        {
            GameObject lineObj = new GameObject("SwordLaserPointer_Line");
            lineObj.transform.SetParent(transform, false);
            _laserPointerLine = lineObj.AddComponent<LineRenderer>();
            _laserPointerLine.positionCount = 2;
            _laserPointerLine.startWidth = 0.025f;
            _laserPointerLine.endWidth = 0.008f;
            _laserPointerLine.useWorldSpace = true;

            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (sh != null) _laserPointerLine.material = new Material(sh);
        }

        private void Update()
        {
            if (activeCamera == null) activeCamera = Camera.main;
            if (activeCamera == null) return;

            bool isInputActive = alwaysOverrideSwordHand || Input.GetMouseButton(0);
            float targetWeight = isInputActive ? 1.0f : 0.0f;
            currentOverrideWeight = Mathf.MoveTowards(currentOverrideWeight, targetWeight, Time.deltaTime * 14f);

            // 1. Project screen cursor ray into the 3D world
            Ray screenRay = activeCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 rawHitPoint;

            Plane combatPlane = new Plane(-transform.forward, transform.position + transform.forward * fallbackCombatPlaneDistance + Vector3.up * 1.25f);
            if (Physics.Raycast(screenRay, out RaycastHit hit, 45f, worldRaycastMask) && hit.transform.root != transform.root)
            {
                rawHitPoint = hit.point;
            }
            else if (combatPlane.Raycast(screenRay, out float enterDist))
            {
                rawHitPoint = screenRay.GetPoint(enterDist);
            }
            else
            {
                rawHitPoint = screenRay.GetPoint(fallbackCombatPlaneDistance + 3f);
            }

            _smoothedLaserPoint = Vector3.SmoothDamp(_smoothedLaserPoint, rawHitPoint, ref _laserSmoothVelocity, pointerSmoothTime);
            laserWorldPoint = _smoothedLaserPoint;

            // Track drag velocity so the blade's cutting edge turns in the direction of motion
            Vector3 frameDelta = (laserWorldPoint - _prevLaserPoint) / Mathf.Max(0.0001f, Time.deltaTime);
            _prevLaserPoint = laserWorldPoint;
            if (frameDelta.sqrMagnitude > 0.25f)
            {
                _swipeEdgeDirection = Vector3.Slerp(_swipeEdgeDirection, frameDelta.normalized, Time.deltaTime * 22f).normalized;
            }
        }

        private void LateUpdate()
        {
            // Runs AFTER Animator updates Universal Animation Library 2!
            if (_rightShoulder == null || _rightElbow == null || _rightHand == null)
            {
                InitializeHumanoidBonesAndRestLengths();
                if (_rightShoulder == null) return;
            }

            if (currentOverrideWeight <= 0.001f)
            {
                if (_laserPointerLine != null) _laserPointerLine.enabled = false;
                return;
            }

            // Save animated rotations from Universal Animation Library 2 for smooth weight blending
            Quaternion animShoulderRot = _rightShoulder.rotation;
            Quaternion animElbowRot    = _rightElbow.rotation;
            Quaternion animHandRot     = _rightHand.rotation;

            // =================================================================
            // STAGE 1: SUBTLE UPPER-CHEST LEAN TOWARD WORLD TARGET (NON-DEFORMING)
            // =================================================================
            if (_upperChest != null && chestLeanWeight > 0.01f)
            {
                Vector3 toLaserFromChest = (laserWorldPoint - _upperChest.position).normalized;
                Vector3 clampedForward = Vector3.RotateTowards(transform.forward, toLaserFromChest, maxChestLeanDegrees * Mathf.Deg2Rad, 0f);
                Quaternion desiredChestDelta = Quaternion.FromToRotation(transform.forward, clampedForward);
                _upperChest.rotation = Quaternion.Slerp(
                    _upperChest.rotation,
                    desiredChestDelta * _upperChest.rotation,
                    chestLeanWeight * currentOverrideWeight);
            }

            // =================================================================
            // STAGE 2: SPHERICAL REACH CHECK & ANTI-CLIPPING TORSO GUARD
            // =================================================================
            Vector3 shoulderPos = _rightShoulder.position;
            Vector3 shoulderToLaser = laserWorldPoint - shoulderPos;

            // Constrain direction to front/side anatomical hemisphere so the arm never bends behind the back
            Vector3 localDir = transform.InverseTransformDirection(shoulderToLaser.normalized);
            localDir.z = Mathf.Max(-0.25f, localDir.z); // Prevent extreme backward hyper-extension
            Vector3 safeWorldDir = transform.TransformDirection(localDir.normalized);

            float distToLaser = shoulderToLaser.magnitude;
            float totalReachWithSword = _maxArmReach + swordBladeLength;

            Vector3 desiredWristWorldPos;
            if (distToLaser <= totalReachWithSword)
            {
                // IN REACH: Position the wrist so the sword tip lands directly on laserWorldPoint!
                isTargetInSwordReach = true;
                float wristDistFromShoulder = Mathf.Clamp(distToLaser - swordBladeLength, 0.18f, _maxArmReach);
                desiredWristWorldPos = shoulderPos + safeWorldDir * wristDistFromShoulder;
            }
            else
            {
                // OUT OF REACH: Clamp wrist strictly to safe reach sphere & point sword down the sightline!
                isTargetInSwordReach = false;
                desiredWristWorldPos = shoulderPos + safeWorldDir * _maxArmReach;
            }

            // Torso Repulsion Cylinder: prevent cross-body drags from clipping through the player's ribcage
            if (_chest != null)
            {
                Vector3 chestCenter = _chest.position;
                Vector3 planarOffset = Vector3.ProjectOnPlane(desiredWristWorldPos - chestCenter, transform.up);
                if (planarOffset.magnitude < torsoExclusionRadius)
                {
                    // Push outward toward the front-right around the chest cylinder
                    Vector3 pushDir = (planarOffset.sqrMagnitude > 0.001f) ? planarOffset.normalized : transform.right;
                    desiredWristWorldPos = new Vector3(
                        chestCenter.x + pushDir.x * torsoExclusionRadius,
                        desiredWristWorldPos.y,
                        chestCenter.z + pushDir.z * torsoExclusionRadius);
                }
            }

            // Re-clamp to [_minArmReach, _maxArmReach] so Triangle Inequality always holds (Zero Bone Stretching!)
            Vector3 shoulderToWrist = desiredWristWorldPos - shoulderPos;
            float minReach = Mathf.Abs(_upperArmLength - _forearmLength) + 0.04f;
            float targetArmDist = Mathf.Clamp(shoulderToWrist.magnitude, minReach, _maxArmReach);
            Vector3 armAxis = shoulderToWrist.sqrMagnitude > 0.0001f ? shoulderToWrist.normalized : transform.forward;

            // =================================================================
            // STAGE 3: ANALYTIC LAW-OF-COSINES 2-BONE ROTATIONAL IK SOLVER
            // =================================================================
            // Solve exact shoulder bend angle (alpha) for triangle (L1 = _upperArmLength, L2 = _forearmLength, D = targetArmDist)
            float cosAlpha = Mathf.Clamp(
                (_upperArmLength * _upperArmLength + targetArmDist * targetArmDist - _forearmLength * _forearmLength) /
                (2f * _upperArmLength * targetArmDist),
                -0.99f, 0.99f);
            float alphaRad = Mathf.Acos(cosAlpha);

            // Anatomical Down-Right-Back Elbow Pole Vector so elbow never flips upside down
            Vector3 elbowPoleTarget = shoulderPos + transform.right * 0.55f - transform.up * 0.45f - transform.forward * 0.20f;
            Vector3 poleDir = (elbowPoleTarget - shoulderPos).normalized;
            Vector3 bendPlaneNormal = Vector3.Cross(armAxis, poleDir).normalized;
            if (bendPlaneNormal.sqrMagnitude < 0.001f) bendPlaneNormal = transform.up;

            // Compute exact non-deforming Elbow and Wrist positions on the skeletal sphere
            Vector3 upperArmDir = (Quaternion.AngleAxis(alphaRad * Mathf.Rad2Deg, bendPlaneNormal) * armAxis).normalized;
            Vector3 solvedElbowPos = shoulderPos + upperArmDir * _upperArmLength;
            Vector3 solvedWristPos = shoulderPos + armAxis * targetArmDist;
            Vector3 forearmDir = (solvedWristPos - solvedElbowPos).normalized;

            // Apply pure rotational override to RightUpperArm and RightLowerArm
            Quaternion ikShoulderRot = Quaternion.LookRotation(upperArmDir, poleDir) * _upperArmAimCorrection;
            Quaternion ikElbowRot    = Quaternion.LookRotation(forearmDir, poleDir) * _forearmAimCorrection;

            _rightShoulder.rotation = Quaternion.Slerp(animShoulderRot, ikShoulderRot, currentOverrideWeight);
            _rightElbow.rotation    = Quaternion.Slerp(animElbowRot, ikElbowRot, currentOverrideWeight);

            // =================================================================
            // STAGE 4: SWORD TIP AIM OVERRIDE & 50/50 FOREARM-WRIST TWIST SPLIT
            // =================================================================
            Vector3 bladeAimVector = (laserWorldPoint - _rightHand.position).normalized;
            if (bladeAimVector.sqrMagnitude < 0.001f) bladeAimVector = safeWorldDir;

            // Align the blade's forward axis with bladeAimVector and its cutting edge with _swipeEdgeDirection
            Vector3 orthogonalUp = Vector3.ProjectOnPlane(_swipeEdgeDirection, bladeAimVector).normalized;
            if (orthogonalUp.sqrMagnitude < 0.001f) orthogonalUp = transform.up;

            // Convert from socket/blade space to RightHand rotation
            Quaternion targetSocketWorldRot = Quaternion.LookRotation(bladeAimVector, orthogonalUp);
            Quaternion handToSocketLocal = (rightHandWeaponSocket != null && rightHandWeaponSocket != _rightHand)
                ? rightHandWeaponSocket.localRotation
                : Quaternion.identity;
            Quaternion desiredHandWorldRot = targetSocketWorldRot * Quaternion.Inverse(handToSocketLocal);

            // Clamp wrist bend/twist relative to forearm so the wrist mesh never pinches ("candy-wrapper" guard)
            Quaternion forearmRefRot = _rightElbow.rotation;
            Quaternion relativeHandRot = Quaternion.Inverse(forearmRefRot) * desiredHandWorldRot;
            relativeHandRot.ToAngleAxis(out float relAngle, out Vector3 relAxis);
            if (relAngle > 180f) relAngle -= 360f;
            float clampedAngle = Mathf.Clamp(relAngle, -maxWristForearmTwistDegrees, maxWristForearmTwistDegrees);

            // Share 40% of the axial twist onto the forearm and 60% onto the wrist for smooth skinning
            Quaternion sharedForearmTwist = Quaternion.AngleAxis(clampedAngle * 0.40f, relAxis);
            Quaternion finalWristLocal    = Quaternion.AngleAxis(clampedAngle * 0.60f, relAxis);

            _rightElbow.rotation = Quaternion.Slerp(animElbowRot, forearmRefRot * sharedForearmTwist, currentOverrideWeight);
            _rightHand.rotation  = Quaternion.Slerp(animHandRot, _rightElbow.rotation * finalWristLocal, currentOverrideWeight);

            // =================================================================
            // STAGE 5: UPDATE VISUAL LASER SIGHTLINE (CYAN = IN REACH, AMBER = OUT OF REACH)
            // =================================================================
            if (_laserPointerLine != null)
            {
                _laserPointerLine.enabled = true;
                Vector3 tipOrigin = (rightHandWeaponSocket != null)
                    ? rightHandWeaponSocket.position + rightHandWeaponSocket.forward * swordBladeLength
                    : _rightHand.position + bladeAimVector * swordBladeLength;

                _laserPointerLine.SetPosition(0, tipOrigin);
                _laserPointerLine.SetPosition(1, laserWorldPoint);

                Color c = isTargetInSwordReach
                    ? new Color(0.15f, 0.95f, 1.0f, 0.85f)  // Cyan: Sword tip touches target
                    : new Color(1.0f, 0.60f, 0.10f, 0.85f); // Amber: Out of reach, pointing at target
                if (_laserPointerLine.material != null) _laserPointerLine.material.color = c;
            }
        }
    }
}


using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BattleSummoner.CameraSystem
{
    [DefaultExecutionOrder(-10)]
    public class PCThirdPersonCameraController : MonoBehaviour
    {
        public static PCThirdPersonCameraController Instance { get; private set; }

        [Header("Target Tracking")]
        public Transform target;
        public Vector3 targetOffset = new Vector3(0f, 1.45f, 0f);
        public float followSmoothTime = 0.05f;

        [Header("Orbit & Distance Tuning")]
        public float currentDistance = 5.2f;
        public float minDistance = 2.5f;
        public float maxDistance = 11.0f;
        public float zoomSpeed = 2.5f;

        public float yaw = 0f;
        public float pitch = 18f;
        public float minPitch = -12f;
        public float maxPitch = 70f;
        public float mouseSensitivityX = 2.2f;
        public float mouseSensitivityY = 1.8f;

        [Header("Obstacle Occlusion")]
        public bool checkCollision = true;
        public float collisionRadius = 0.22f;
        public LayerMask collisionLayers = ~0; // default everything except player

        private Vector3 _currentVelocity;
        private Vector3 _smoothTargetPos;
        private bool _isOrbitingWithMouse = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (target == null)
            {
                var player = GameObject.Find("Player_Hero") ?? GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                _smoothTargetPos = target.position + targetOffset;
                yaw = target.eulerAngles.y;
            }
            else
            {
                _smoothTargetPos = transform.position;
            }

            // Exclude Player and Ignore Raycast layers from camera collision
            collisionLayers &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) collisionLayers &= ~(1 << playerLayer);
        }

        private void OnEnable()
        {
            // Disable mobile portrait cameras to eliminate conflicts & shaking
            var mobileCam = GetComponent<PortraitARPGCamera>();
            if (mobileCam != null) mobileCam.enabled = false;

            var resEnforcer = GetComponent<HighFidelityMobileResolutionEnforcer>();
            if (resEnforcer != null) resEnforcer.enabled = false;
        }

        private void Update()
        {
            HandleMouseOrbitAndZoom();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredTargetPos = target.position + targetOffset;
            _smoothTargetPos = Vector3.SmoothDamp(_smoothTargetPos, desiredTargetPos, ref _currentVelocity, followSmoothTime);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 backDir = rot * Vector3.back;
            float targetDist = currentDistance;

            if (checkCollision)
            {
                RaycastHit[] hits = Physics.SphereCastAll(_smoothTargetPos, collisionRadius, backDir, currentDistance, collisionLayers, QueryTriggerInteraction.Ignore);
                float closestValid = currentDistance;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform == target || hits[i].transform.IsChildOf(target)) continue;
                    if (hits[i].collider.isTrigger) continue;
                    if (hits[i].distance < closestValid) closestValid = hits[i].distance;
                }
                if (closestValid < currentDistance)
                {
                    targetDist = Mathf.Clamp(closestValid - 0.15f, minDistance, currentDistance);
                }
            }

            transform.position = _smoothTargetPos + backDir * targetDist;
            transform.LookAt(_smoothTargetPos);
        }

        private void HandleMouseOrbitAndZoom()
        {
            // Right-Click drag to orbit camera, or free orbit if Right Click held
            bool rClickHeld = IsRightMouseHeld();
            bool mClickHeld = IsMiddleMouseHeld();

            if (rClickHeld || mClickHeld)
            {
                Vector2 delta = GetMouseDelta();
                yaw += delta.x * mouseSensitivityX;
                pitch -= delta.y * mouseSensitivityY;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            float scroll = GetMouseScroll();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);
            }
        }

        private bool IsRightMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.rightButton.isPressed;
#endif
            try { return Input.GetMouseButton(1); } catch { return false; }
        }

        private bool IsMiddleMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.middleButton.isPressed;
#endif
            try { return Input.GetMouseButton(2); } catch { return false; }
        }

        private Vector2 GetMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                var d = Mouse.current.delta.ReadValue();
                return new Vector2(d.x * 0.1f, d.y * 0.1f);
            }
#endif
            try { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); } catch { return Vector2.zero; }
        }

        private float GetMouseScroll()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                float y = Mouse.current.scroll.ReadValue().y;
                return Mathf.Clamp(y, -1f, 1f);
            }
#endif
            try { return Input.GetAxis("Mouse ScrollWheel"); } catch { return 0f; }
        }
    }
}

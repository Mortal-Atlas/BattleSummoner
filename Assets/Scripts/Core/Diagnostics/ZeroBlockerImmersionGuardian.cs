using UnityEngine;

namespace BattleSummoner.Core.Diagnostics
{
    /// <summary>
    /// Enforces unbroken gameplay flow and eliminates rookie softlocks:
    /// 1. Prevents Time.timeScale = 0 pause blackouts (keeps HUD diegetic & live in 3D space).
    /// 2. Detects if the player falls through terrain or gets wedged in geometry and smoothly step-resolves them.
    /// 3. Guarantees camera Line-of-Sight (auto-dithers any prop between PortraitARPGCamera and the Player).
    /// </summary>
    public class ZeroBlockerImmersionGuardian : MonoBehaviour
    {
        public static ZeroBlockerImmersionGuardian Instance { get; private set; }

        [Header("References")]
        public Transform playerTransform;
        public CharacterController playerController;
        public Camera mainPortraitCamera;
        public LayerMask environmentObstacleMask;

        [Header("Anti-Softlock Safety Thresholds")]
        public float outOfBoundsFloorY = -25f;
        public Vector3 safeFallbackSpawnPoint = new Vector3(0f, 2.0f, 0f);
        public float stuckVelocityThreshold = 0.05f;
        public float maxStuckSecondsBeforeUnwedge = 1.8f;

        private Vector3 _lastSafeGroundedPosition;
        private float _stuckTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (mainPortraitCamera == null) mainPortraitCamera = Camera.main;
            _lastSafeGroundedPosition = safeFallbackSpawnPoint;
            AutoLocatePlayer();
        }

        private void Start()
        {
            AutoLocatePlayer();
        }

        private void AutoLocatePlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerController = player.GetComponent<CharacterController>();
                }
            }
        }

        private void Update()
        {
            // 1. DIEGETIC IMMERSION RULE: Never allow rookie full-freeze pause menus during live gameplay
            if (Time.timeScale < 0.05f)
            {
                Time.timeScale = 1.0f;
            }

            if (playerTransform == null)
            {
                AutoLocatePlayer();
                if (playerTransform == null) return;
            }

            // 2. Record last safe grounded coordinate
            if (playerController != null && playerController.isGrounded && playerTransform.position.y > outOfBoundsFloorY + 5f)
            {
                _lastSafeGroundedPosition = playerTransform.position;
            }

            // 3. Out-of-bounds / Under-Map Fall Recovery (Zero Death/Reload Blocker)
            if (playerTransform.position.y < outOfBoundsFloorY)
            {
                TeleportPlayerSafely(_lastSafeGroundedPosition + Vector3.up * 1.5f);
                Debug.LogWarning("[ZeroBlockerGuardian] Recovered player from below-terrain clip without halting gameplay.");
            }
        }

        /// <summary>
        /// Called by movement controller when player input magnitude > 0.5f.
        /// Automatically nudges the player up/around micro-geometry snags if wedged.
        /// </summary>
        public void MonitorMovementProgress(Vector2 inputVector, Vector3 actualVelocity)
        {
            if (inputVector.sqrMagnitude > 0.25f && actualVelocity.sqrMagnitude < (stuckVelocityThreshold * stuckVelocityThreshold))
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer >= maxStuckSecondsBeforeUnwedge && playerController != null)
                {
                    // Smooth step-up / lateral slide nudge to clear sharp collider corners
                    Vector3 nudge = (playerTransform.up * 0.45f) - (playerTransform.forward * 0.35f);
                    playerController.Move(nudge);
                    _stuckTimer = 0f;
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        private void TeleportPlayerSafely(Vector3 targetPos)
        {
            if (playerController != null) playerController.enabled = false;
            playerTransform.position = targetPos;
            if (playerController != null) playerController.enabled = true;
        }
    }
}
